# Etapa 11 — Final Report

## Objetivo

Encerrar a Etapa 11 - Configuration Isolation & Dependency Injection com
auditoria de especificacao, API publica, imutabilidade, isolamento,
compatibilidade estatica, DI, performance, trimming/AOT e documentacao publica,
sem iniciar funcionalidades da Etapa 12.

## Implementado

| Requirement | Implementation | Tests | Compatibility | Status |
| ----------- | -------------- | ----- | ------------- | ------ |
| Separar configuracao mutavel de runtime | `FluentMapConfigurationBuilder -> Build() -> ImmutableFluentMapConfiguration -> FluentMapRuntime` | `ImmutableConfigurationModelTests`, `IsolatedRuntimeTests` | Aditivo; `FluentMapConfiguration` historico preservado | Completed |
| Builder com DSL existente | `FluentMapConfigurationBuilder` delega para uma `FluentMapConfiguration` sobre registry isolado; `Configure(...)` reusa extensoes existentes e generated registration | Builder/configuration tests e generated DI test | Sem remover DSL antiga | Completed |
| Snapshot imutavel | `ImmutableFluentMapConfiguration` expoe `IReadOnly*` e descritores de maps, profiles, conventions, property metadata e generated materializers | `BuildShouldNotExposeMutableEffectiveCollections`, `BuildShouldCaptureSnapshotIndependentFromLaterMapMutation` | Interfaces publicas mutaveis antigas continuam fora do modelo novo | Completed |
| Runtime isolado | `FluentMapRuntime` possui registry proprio reconstruido do snapshot | `RuntimeShouldMaterializeSameEntityWithIndependentConfigurations` e hardening concorrente | Entry points novos por runtime sao opt-in | Completed |
| Caches por configuracao/runtime | Property map cache, generated lookup e materialization plan cache vivem no `MappingRegistry` de cada runtime | cache isolation tests e benchmarks runtime/static | Caches globais legados permanecem apenas na bridge estatica | Completed |
| Query APIs por runtime | Metodos de instancia em `FluentMapRuntime` cobrem `QueryMapped`, profiles, single, unbuffered sync/async e `QueryMultipleMapped` | `IsolatedRuntimeTests`, `ConfigurationIsolationHardeningTests` | Helpers estaticos continuam usando runtime default | Completed |
| Profiles isolados | Profiles ficam no snapshot e sao resolvidos por runtime | testes com mesmo profile type em configuracoes diferentes | Sem alterar semantica query-scoped | Completed |
| Converters isolados | Conversion metadata e instancias/delegates sao capturados por snapshot e reconstruidos por runtime | converter isolation tests | Mantido contrato stateless/thread-safe | Completed |
| Persistence metadata imutavel | `PropertyPersistenceMetadata` e copiada para `PropertyMappingConfiguration` | snapshot tests e Dommel persistence suite | Core continua metadata-only para writes | Completed |
| Generated materializers por runtime | Descritores ficam no snapshot e sao registrados no registry do runtime | generated isolation/concurrency tests; generated registration DI test | Fallback runtime preservado | Completed |
| Bridge estatica compativel | `FluentMapper.Initialize(...)` publica `FluentMapper.Configuration` e `FluentMapper.Runtime` e reinstala type maps Dapper | `CompatibilityBridgeTests` | `Initialize` aditivo, campos publicos e `Dapper.Query<T>()` preservados | Completed |
| Dapper global type map | Documentado e testado como limite process-wide | `DapperQueryShouldUseOnlyThePublishedGlobalTypeMapForSameEntity` | Compatibilidade mantida; nao isolado por chamada | Partial |
| Dommel | Mantido como bridge process-wide sobre colecoes legadas | `DommelResolversShouldUseOnlyLegacyProcessWideConfiguration` | Sem promessa de isolamento Dommel nesta etapa | Partial |
| DI opcional | Pacote `Dapper.FluentMap.DependencyInjection` com `services.AddFluentMap(...)` | `FluentMapServiceCollectionExtensionsTests` | Core sem dependencia obrigatoria de DI | Completed |
| DI lifetimes | `ImmutableFluentMapConfiguration` e `FluentMapRuntime` registrados como singletons | singleton, providers independentes e concorrencia | Sem registrar conexao, repository ou Dommel | Completed |
| Trimming/AOT | Registro explicito/gerado e DI smoke; scanning continua anotado; `QueryMapped*` continua warning-sensitive | AOT smoke run/publish trimmed | Native AOT total nao declarado | Partial |
| Performance | Runtime resolvido por reader; hot path usa delegate/plano cacheado | `MaterializationSteadyStateBenchmarks*QueryMappedSimple*` smoke | Sem regressao de alocacao observavel | Completed |
| Test isolation | Novos testes usam runtime local sem `FluentMapper.Reset()` | isolation hardening e DI providers independentes | Testes legados globais continuam serializados | Completed |

Divergencias e itens parciais:

- `Dapper.Query<T>()` nao e isolado por configuracao porque o Dapper usa
  `SqlMapper.SetTypeMap` global por tipo. A solucao suportada para multiplas
  configuracoes e `runtime.QueryMapped<T>()`.
- Dommel nao e isolado por runtime porque `DommelMapper` usa resolvers/builders
  globais e os resolvers atuais dependem de metadata especifica de
  `DommelEntityMap`/`DommelPropertyMap` nas colecoes legadas.
- `QueryMapped*` nao foi declarado Native AOT-safe porque ainda pode cair no
  materializer runtime baseado em reflection/dynamic code.

## Configuration Builder

`FluentMapConfigurationBuilder` e o ponto mutavel novo. Ele aceita maps,
profiles, conventions, naming policies, generated materializers e
`Configure(Action<FluentMapConfiguration>)`. O builder usa um
`MappingRegistry(installDapperTypeMaps: false)`, portanto nao instala type maps
globais do Dapper durante a construcao de configuracoes isoladas.

`Build()` valida a configuracao, cria o snapshot imutavel e sela o builder.
Chamadas mutadoras posteriores lancam `InvalidOperationException`; chamadas
posteriores a `Build()` retornam a mesma configuracao.

## Immutable Configuration

`ImmutableFluentMapConfiguration` captura:

- maps default por entidade;
- profiles por entidade/profile;
- conventions e naming policies aplicadas por entidade;
- property maps com member path, coluna, case sensitivity e ignore;
- persistence metadata;
- conversion metadata;
- generated materializer descriptors.

Depois do build, mutacoes tardias nos maps/conventions originais nao alteram o
snapshot. As colecoes expostas sao read-only. O snapshot nao expoe as instancias
mutaveis de `IEntityMap`, `Convention` ou `PropertyMap` como configuracao
efetiva.

## Isolated Runtime

`ImmutableFluentMapConfiguration.CreateRuntime()` cria um `FluentMapRuntime`.
O runtime reconstrui um registry interno a partir dos descritores imutaveis e
usa esse registry para materializacao, generated lookup, profile lookup,
converters e diagnostics.

Os entry points de instancia cobrem os caminhos controlados pelo FluentMap:
`QueryMapped*`, `QueryMappedSingle*`, `QueryMappedUnbuffered*`,
`QueryMappedUnbufferedAsync*` e `QueryMultipleMapped`.

## Configuration-scoped Caches

Cada runtime possui seus proprios caches derivados:

- property map lookup;
- profile property map lookup;
- convention lookup;
- runtime materialization plan;
- generated materializer lookup/index.

As chaves continuam usando tipo, profile, coluna/shape e estrategia de lookup.
Como o cache vive dentro do runtime, a identidade da configuracao e implicita.
O benchmark smoke do prompt 11.7 manteve alocacao equivalente entre helpers
estaticos e runtime isolado nos pares comparaveis.

## Static Compatibility Layer

`FluentMapper.Initialize(...)` continua aditivo e serializado por lock. Depois
de cada inicializacao, a bridge publica um novo runtime default e reinstala
type maps Dapper para entidades com maps default ou conventions.

Preservado:

- `FluentMapper.Initialize(...)`;
- `FluentMapper.Validate()`;
- `FluentMapper.Explain<T>()` e profile;
- `FluentMapper.GetEntityMaps()`;
- `FluentMapper.GetTypeConventions()`;
- campos publicos `EntityMaps` e `TypeConventions`;
- comportamento aditivo de chamadas repetidas.

Nao houve nova marcacao `[Obsolete]`. As colecoes publicas mutaveis foram
documentadas como compatibilidade legada.

## Dependency Injection

O pacote novo `Dapper.FluentMap.DependencyInjection` adiciona:

```csharp
services.AddFluentMap(builder =>
{
    builder.AddMap<CustomerMap>();
});
```

O callback recebe o builder, nao `IServiceProvider`. A configuracao e
construida e validada imediatamente; depois `ImmutableFluentMapConfiguration` e
`FluentMapRuntime` sao registrados como singletons.

O pacote depende apenas de `Dapper.FluentMap` e
`Microsoft.Extensions.DependencyInjection.Abstractions`. O core nao passou a
ter dependencia obrigatoria de DI, Hosting, ASP.NET Core, Options, Logging ou
Dommel.

## Multiple Configurations

Suportado para materializacao controlada pelo FluentMap:

```text
Configuration A -> Runtime A -> runtime.QueryMapped<T>()
Configuration B -> Runtime B -> runtime.QueryMapped<T>()
```

Testes provam o mesmo entity type com mappings diferentes, mesmo profile type
com metadata diferente, converter metadata por runtime e generated
materializers concorrentes sem colisao.

Nao suportado como isolamento completo para:

- `Dapper.Query<T>()` tradicional;
- Dommel;
- mutacao direta das colecoes legadas sem nova publicacao pela bridge estatica.

## Test Isolation

Novos testes podem criar `FluentMapConfigurationBuilder`, chamar `Build()` e
usar `CreateRuntime()` sem `FluentMapper.Reset()`. Isso reduz dependencia de
estado global e permite testar duas configuracoes para o mesmo tipo no mesmo
processo.

A suite ainda possui testes legados que exercitam `FluentMapper.Initialize`,
Dapper type maps globais e Dommel. Esses testes continuam usando reset interno
e paralelismo desabilitado onde necessario.

## Concurrency

O modelo efetivo e:

- builder mutavel, usado em startup/composition root e nao thread-safe;
- configuracao imutavel read-only, segura para leitura concorrente;
- runtime thread-safe com caches `ConcurrentDictionary`;
- materializers e planos resolvidos por reader e reutilizados por linha;
- converters reutilizados concorrentemente conforme contrato stateless ou
  thread-safe.

Testes com `Barrier` cobrem runtimes distintos, mesmo runtime em varias
threads, generated materializers concorrentes, profiles/converters/diagnostics
concorrentes e providers DI independentes.

## Dapper Global Integration Limitations

`SqlMapper.SetTypeMap` e global por entity type. A bridge estatica instala type
maps para preservar `connection.Query<T>()`, mas esse caminho nao seleciona uma
configuracao FluentMap por chamada.

Conclusao:

- `runtime.QueryMapped<T>()`: isolado por runtime;
- `connection.Query<T>()`: usa o type map global publicado pelo
  `FluentMapper.Initialize(...)` mais recente para aquele tipo.

Essa limitacao e upstream/estrutural e esta documentada no README e na matriz
de isolamento.

## Dommel Integration Limitations

Dommel permanece process-wide. `ForDommel()` instala resolvers/builders globais
em `DommelMapper`. Os resolvers atuais leem `FluentMapper.EntityMaps` e
`FluentMapper.TypeConventions`, preservando metadata especifica de Dommel.

Configuracoes criadas apenas por `FluentMapConfigurationBuilder` nao dirigem
Dommel. Multiplas configuracoes Dommel simultaneas para o mesmo tipo nao sao
contrato suportado na Etapa 11.

## Performance

O hot path permanece:

```text
reader shape
    -> generated lookup ou runtime materialization plan cache
    -> delegate/plano reutilizado por row
```

Benchmark smoke do prompt 11.7:

| Comparacao | Metodo | Mean | Allocated |
| --- | --- | ---: | ---: |
| legacy default runtime | `QueryMappedSimple` | 1.976 ms | 261.16 KB |
| isolated runtime | `RuntimeQueryMappedSimple` | 2.070 ms | 261.16 KB |
| legacy default runtime | `QueryMappedSimpleUnbuffered` | 2.295 ms | 245.20 KB |
| isolated runtime | `RuntimeQueryMappedSimpleUnbuffered` | 2.667 ms | 245.20 KB |
| legacy default runtime | `QueryMappedSimpleRuntimeFallback` | 1.965 ms | 361.58 KB |
| isolated runtime | `RuntimeQueryMappedSimpleRuntimeFallback` | 1.922 ms | 361.58 KB |

O BenchmarkDotNet avisou que as iteracoes ficaram abaixo de 100 ms. Use estes
numeros como smoke de alocacao/guardrail, nao como benchmark formal de release.

## Native AOT / Trimming

Validado:

- `dotnet run` smoke explicito: `explicit:ok`;
- `dotnet run` smoke generated: `generated:ok`;
- `dotnet run` smoke DI explicito: `di-explicit:ok`;
- `dotnet run` smoke DI generated: `di-generated:ok`;
- `PublishTrimmed=true` DI explicito: sucesso; warning conhecido `IL2104` do
  Dapper; binario executou `di-explicit:ok`;
- `PublishTrimmed=true` DI generated: sucesso; warnings conhecidos `IL2104`
  de `Dapper.FluentMap`/Dapper; binario executou `di-generated:ok`.

Native AOT smoke:

- `PublishAot=true` DI explicito foi bloqueado pelo ambiente com
  `Platform linker not found`;
- o erro pede os prerequisitos Native AOT/Desktop Development for C++;
- nenhuma compatibilidade Native AOT total foi declarada.

Assembly scanning continua anotado como trimming-sensitive. `QueryMapped*`
continua anotado com `RequiresUnreferencedCode` e `RequiresDynamicCode` porque
pode cair no fallback runtime.

## Backward Compatibility

Compatibilidade de fonte:

- APIs estaticas existentes permanecem;
- campos publicos legados permanecem;
- `FluentMapConfiguration` continua tipo publico mutavel;
- `Initialize` continua aditivo;
- nenhuma nova `[Obsolete]` foi aplicada.

Compatibilidade comportamental:

- `Dapper.Query<T>()` continua usando type maps globais quando a bridge estatica
  e configurada;
- `QueryMapped*` estatico usa o runtime default publicado;
- profiles, converters, persistence metadata e generated materializers
  continuam funcionando na bridge estatica e nos runtimes isolados.

Compatibilidade binaria:

- nao ha ferramenta de API compatibility configurada no repositorio atual;
- a auditoria de superficie publica nao identificou remocoes ou alteracoes de
  assinatura nas APIs legadas;
- validacao binaria formal fica recomendada para a Etapa 12 antes de release.

## Historical Issue #101

Estado final:

```text
Issue #101: Partially resolved
```

Resolvida estruturalmente para APIs controladas pelo FluentMap:

- configuracoes independentes no mesmo processo;
- runtimes com caches proprios;
- queries concorrentes para o mesmo tipo com mappings diferentes;
- DI com service providers independentes.

Nao resolvida para caminhos globais legados:

- `Dapper.Query<T>()`;
- Dommel;
- troca global por reset/clear durante operacoes concorrentes.

## Known Limitations

- `Dapper.Query<T>()` tradicional nao seleciona runtime por chamada.
- Dommel continua bridge process-wide.
- Assembly scanning depende de reflection discovery e nao e recomendado para
  trimming/Native AOT.
- `QueryMapped*` ainda pode usar fallback runtime com reflection/dynamic code.
- Converter instances/delegates devem ser stateless ou thread-safe.
- Generated materializers por instancia/delegate ou shapes nao suportados ainda
  usam fallback runtime.
- Named/keyed DI configurations nao foram adicionadas.
- As colecoes legadas `FluentMapper.EntityMaps` e `TypeConventions` continuam
  mutaveis por compatibilidade e podem bypassar validacao/cache/type map.

## Technical Debt

- Adicionar ferramenta formal de API/binary compatibility antes de release.
- Planejar obsolescencia gradual das colecoes publicas mutaveis.
- Projetar bridge Dommel por runtime apenas se os extension points globais
  permitirem um contrato honesto.
- Avaliar caminho generated-only/AOT-safe sem fallback runtime.
- Executar benchmark completo de release para cold start, muitos runtimes e
  configuracoes grandes.
- Corrigir metadata NuGet legada do core (`licenseUrl`/README de pacote) em
  tarefa propria.

## Deferred Items

- Named/keyed DI configurations.
- Full Native AOT support.
- Dommel configuration isolation.
- Public reset/clear API.
- Compatibility matrix ampla de releases.
- Provider certification completa.
- Release automation.
- Write converter execution em Dapper/Dommel.
- Service-based converter factories/lifetimes.

## Recommendations for Etapa 12

1. Introduzir validacao formal de API/binary compatibility e baseline publica.
2. Decidir politica de obsolescencia para `FluentMapper.EntityMaps` e
   `FluentMapper.TypeConventions`.
3. Avaliar um caminho generated-only/AOT-safe separado do fallback runtime.
4. Desenhar Dommel isolation somente se houver extension point viavel sem
   `AsyncLocal` ou service locator.
5. Rodar benchmark completo de release antes de publicar pacote.

## Validation

Executado em 2026-07-29:

```bash
dotnet restore ./Dapper.FluentMap.sln
dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build
dotnet run --project ./benchmarks/Dapper.FluentMap.Benchmarks/Dapper.FluentMap.Benchmarks.csproj --configuration Release -- --filter "*MaterializationSteadyStateBenchmarks*QueryMappedSimple*" --job Dry
dotnet run --project ./test/Dapper.FluentMap.AotSmoke/Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:DefineConstants=AOT_SMOKE_EXPLICIT -p:UseSharedCompilation=false
dotnet run --project ./test/Dapper.FluentMap.AotSmoke/Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:DefineConstants=AOT_SMOKE_GENERATED -p:UseSharedCompilation=false
dotnet run --project ./test/Dapper.FluentMap.AotSmoke/Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:DefineConstants=AOT_SMOKE_DI_EXPLICIT -p:UseSharedCompilation=false
dotnet run --project ./test/Dapper.FluentMap.AotSmoke/Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:DefineConstants=AOT_SMOKE_DI_GENERATED -p:UseSharedCompilation=false
dotnet publish ./test/Dapper.FluentMap.AotSmoke/Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:PublishTrimmed=true -p:DefineConstants=AOT_SMOKE_DI_EXPLICIT -p:UseSharedCompilation=false --output ./.tmp/aot-smoke/di-explicit-trimmed
./.tmp/aot-smoke/di-explicit-trimmed/Dapper.FluentMap.AotSmoke.exe
dotnet publish ./test/Dapper.FluentMap.AotSmoke/Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:PublishTrimmed=true -p:DefineConstants=AOT_SMOKE_DI_GENERATED -p:UseSharedCompilation=false --output ./.tmp/aot-smoke/di-generated-trimmed
./.tmp/aot-smoke/di-generated-trimmed/Dapper.FluentMap.AotSmoke.exe
dotnet publish ./test/Dapper.FluentMap.AotSmoke/Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:PublishAot=true -p:DefineConstants=AOT_SMOKE_DI_EXPLICIT -p:UseSharedCompilation=false --output ./.tmp/aot-smoke/di-explicit-aot
dotnet pack ./src/Dapper.FluentMap/Dapper.FluentMap.csproj --configuration Release --no-build --output ./artifacts/packages
dotnet pack ./src/Dapper.FluentMap.DependencyInjection/Dapper.FluentMap.DependencyInjection.csproj --configuration Release --no-build --output ./artifacts/packages
tar -tf ./artifacts/packages/Dapper.FluentMap.2.0.0.nupkg
tar -tf ./artifacts/packages/Dapper.FluentMap.DependencyInjection.2.0.0.nupkg
```

Resultados:

- Restore: sucesso.
- Build Release: sucesso, 0 warnings, 0 errors.
- Solution tests: sucesso, 453 testes aprovados.
- Benchmark smoke: sucesso, 20 cenarios executados; warnings de iteracao curta
  esperados do BenchmarkDotNet.
- AOT smoke executavel: `explicit:ok`, `generated:ok`, `di-explicit:ok`,
  `di-generated:ok`.
- Trimmed DI explicit: publish e execucao com sucesso; warning conhecido
  `IL2104` do Dapper.
- Trimmed DI generated: publish e execucao com sucesso; warnings conhecidos
  `IL2104` de `Dapper.FluentMap`/Dapper.
- Native AOT: bloqueado pelo ambiente por ausencia de platform linker.
- Pack core: sucesso; warning legado `NU5125` sobre `licenseUrl` obsoleto e
  aviso de README ausente no pacote.
- Pack DI: sucesso.
- Inspecao dos pacotes: assemblies e XML documentation em `lib/netstandard2.0`;
  pacote DI inclui `README.md`; nenhum pacote contem projetos de teste.

Observacao: uma tentativa inicial de smokes AOT em paralelo falhou com `CS2012`
no assembly intermediario do generator por disputa de arquivo. Os mesmos
smokes foram reexecutados sequencialmente com sucesso.
