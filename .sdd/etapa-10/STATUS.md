# Etapa 10 Status

Status: Concluída

Último prompt executado: 10.7

## Objetivo

Definir discovery, boundaries e arquitetura inicial para Property Conversion &
Extensibility, preservando `TypeHandler<T>` do Dapper como mecanismo global por
tipo e abrindo espaco para conversao por propriedade, map e profile.

## Concluido

- Executado `git status` antes de alteracoes.
- Confirmada branch `feature/etapa-3`; nao estamos em `master`.
- Identificado item nao rastreado preexistente `src/Dapper.FluentMap/etapas/`,
  deixado intacto.
- Lido `README.md`.
- Examinada `Dapper.FluentMap.sln`.
- Examinados projetos core, Dommel, analyzers, generators, testes, smoke AOT e
  benchmarks por arquivos de projeto e pontos de implementacao relevantes.
- Examinado runtime materialization em `NestedMaterializationPlan` e
  `MappedRowMaterializer`.
- Examinado generated materialization em `GeneratedMaterializerDescriptor`,
  `GeneratedMaterializerColumn` e `MappingRegistrationGenerator`.
- Examinado source generator, incluindo fluent-chain parsing e helper `Read<T>`.
- Examinado analyzer, incluindo fluent-chain parsing e metadata de persistencia.
- Examinada persistence metadata em `PropertyPersistenceMetadata`.
- Examinada integracao Dommel em property/key/column/table resolvers e SQL
  builder de persistencia.
- Lidos `.sdd/etapa-9/FINAL-REPORT.md` e `.sdd/etapa-9/STATUS.md`.
- Consultado `.sdd/etapa-8/FINAL-REPORT.md`.
- Confirmado que `.sdd/etapa-10/` nao existia e criada a pasta.
- Consultadas fontes de ecossistema: Dapper TypeHandler, RepoDB PropertyHandler
  e EF Core ValueConverter.
- Criado `.sdd/etapa-10/01-conversion-landscape.md`.
- Criado `.sdd/etapa-10/02-property-conversion-spec.md`.
- Criado `.sdd/etapa-10/DECISIONS.md`.
- Criado `.sdd/etapa-10/STATUS.md`.
- Criado `.sdd/etapa-10/03-converter-contract-design.md`.
- Implementados contratos publicos de conversao por propriedade:
  `IReadPropertyConverter<TDatabase, TProperty>`,
  `IWritePropertyConverter<TProperty, TDatabase>`,
  `IPropertyConverter<TDatabase, TProperty>`,
  `ReadPropertyConverter<TDatabase, TProperty>` e
  `WritePropertyConverter<TProperty, TDatabase>`.
- Implementada fluent API em `PropertyMapBase<TPropertyMap>`:
  `ConvertFromDatabaseUsing`, `ConvertToDatabaseUsing` e `ConvertUsing`.
- Implementada metadata aditiva:
  `PropertyConversionMetadata`, `PropertyConverterMetadata`,
  `PropertyConversionDirection` e `IPropertyMapWithConversionMetadata`.
- `MemberMappingExplanation` passou a expor `Conversion`.
- Registry passa a rejeitar generated materializer manual quando o mapping
  efetivo possui read converter, evitando execucao generated sem conversao.
- Adicionados testes de metadata/fluent API/read-only/write-only/bidirecional,
  delegates, lifetime por property map, heranca, profile, invalid types,
  duplicidade, nullability, profile collision e generated fallback defensivo.
- Criado `.sdd/etapa-10/04-runtime-conversion.md`.
- Implementada execucao de read converters por propriedade no runtime
  materializer comum (`NestedMaterializationPlan`).
- Preservada precedencia:
  `null/DBNull -> property read converter -> Dapper TypeHandler<TProperty> -> default conversion`.
- Garantido que property converter configurado nao recebe valor ja convertido
  por `TypeHandler<TProperty>` e que `TypeHandler<TProperty>` nao roda depois
  do property converter da mesma folha.
- Mantida a selecao generated-then-runtime existente; generated descriptors
  com read converter continuam recusados e caem para runtime fallback.
- Adicionados testes de execucao para scalar conversion, nullable, null, enum,
  nested member path, constructor parameter, Value Object escalar, profile,
  exception wrapping, coexistencia com TypeHandler, unbuffered e async
  streaming.
- Atualizado README para documentar read conversion em runtime mapped.
- Adicionados benchmarks especificos para no converter, simple converter,
  TypeHandler e property converter.
- Criado `.sdd/etapa-10/05-performance-baseline.md`.
- Criado `.sdd/etapa-10/06-generated-conversion.md`.
- Criado `.sdd/etapa-10/07-write-conversion.md`.
- Generated materializers passam a emitir property read converters por tipo
  quando o converter e estaticamente suportado.
- `GeneratedMaterializerColumn` passou a declarar metadata opcional de read
  converter: converter type, database/provider type e converter property type.
- O registry valida descriptors gerados com converter contra o mapping efetivo,
  separando default map e profiles sem colisao.
- O source generator emite campos estaticos de converter e chamadas genericas
  fortemente tipadas para `ReadConverted<TDatabase, TProperty, TTarget>`.
- Preservada a semantica `null/DBNull` externa ao converter, incluindo o caso
  converter `T` aplicado a target `Nullable<T>`.
- Adicionado diagnostic `DFM012` para contrato read converter invalido
  comprovavel em compile-time.
- Converters por instancia/delegate e converters inacessiveis ao codigo gerado
  continuam usando runtime fallback.
- Smoke AOT generated atualizado para cobrir property read converter.
- Investigado Dommel 3.5.3 para write conversion e confirmado que os extension
  points publicos atuais nao expoem hook de valor de parametro por propriedade.
- Documentada a decisao de nao executar write converters em Dommel nesta etapa,
  preservando persistence semantics da Etapa 8 e comportamento atual de
  `Insert`/`Update`.
- Criado `.sdd/etapa-10/08-conversion-diagnostics.md`.
- Consolidada a matriz de diagnostics para converter configuration,
  runtime validation, analyzer comum, generator e Dommel/write boundary.
- Renumerado o diagnostic de persistencia do analyzer comum para `DFM013`,
  evitando colisao com `DFM012` do generator.
- Adicionados diagnostics do analyzer comum:
  `DFM014` para property converter por tipo invalido e `DFM015` para converter
  direcional duplicado na mesma fluent chain.
- `MappingConfigurationValidator` passou a validar conversion metadata efetiva,
  incluindo metadata nula em property map externo, direcao inconsistente,
  converter em propriedade ignorada e write converter em propriedade que nunca
  participa de insert, update ou key persistence.
- Documentado no XML docs dos contratos publicos que converters podem ser
  reutilizados por operacoes concorrentes e devem ser stateless/thread-safe.
- Reforcados testes de regressao para converters por propriedade com mesmo tipo,
  read/write asymmetry, profiles concorrentes, generated concurrent conversion,
  Dommel mantendo write conversion metadata-only e validacao runtime de
  metadata externa invalida.

## Itens adiados

Write/Dommel conversion permanece bloqueada ate existir um hook publico de
parametros por propriedade no Dommel ou uma API explicita no pacote de
integracao.

## Proximos passos

1. Definir hook publico de parametros por propriedade antes de implementar write
   conversion/Dommel.
2. Avaliar diagnostics futuros somente quando houver nova superficie de write
   conversion ou null conversion opt-in.
3. Aumentar benchmark formal quando houver decisao de otimizacao.

## Decisoes relevantes

- Property converter nao substitui `TypeHandler<T>`.
- Read e write conversion sao direcoes independentes.
- Converter local tem precedencia sobre `TypeHandler<T>` apenas na direcao e
  propriedade/profile configurados.
- `TypeHandler<T>` continua recomendado para Value Objects escalares com
  representacao global uniforme.
- Runtime read conversion deve vir antes de generated read conversion.
- Generated materializer deve cair para runtime fallback quando converter nao
  puder ser emitido com seguranca.
- Dommel/write conversion e incremento separado porque a integracao atual nao
  transforma valores de parametros por propriedade.
- Prompt 10.6 consolida diagnostics e hardening sem mudar a semantica de
  execucao: analyzer comum cobre somente conversores estaticamente provaveis,
  runtime validation cobre composicao efetiva e Dommel continua metadata-only
  para write converters.
- Converters sao stateless/thread-safe por contrato e reutilizados.
- AOT exige caminho por instancia/delegate ou referencia estatica gerada; nao
  deve depender de ativacao reflection-only.
- Prompt 10.2 decidiu implementar somente contracts/metadata/fluent API e
  diagnostics, mantendo execucao de conversores para incremento posterior.
- Prompt 10.3 executa read converters no runtime materializer comum e mantem
  generated/write conversion fora do escopo.
- Prompt 10.4 executa read converters no generated materializer somente para
  converters por tipo estaticamente suportados e mantem fallback runtime para
  instancia/delegate/inacessivel.
- Prompt 10.5 mantem write converters como metadata-only para Dommel porque
  `Insert`/`Update` passam a entidade original ao Dapper e a versao atual nao
  expoe hook publico para substituir valores de parametros por propriedade.

## APIs implementadas no Prompt 10.2

```csharp
Map(x => x.Status)
    .ToColumn("status")
    .ConvertFromDatabaseUsing<LegacyStatusReader, string>();
```

```csharp
Map(x => x.Status)
    .ToColumn("status")
    .ConvertToDatabaseUsing<LegacyStatusWriter, string>();
```

```csharp
Map(x => x.Status)
    .ToColumn("status")
    .ConvertUsing<LegacyStatusConverter, string>();
```

Contratos implementados:

```csharp
public interface IReadPropertyConverter<TDatabase, TProperty>
{
    TProperty ConvertFromDatabase(TDatabase value);
}

public interface IWritePropertyConverter<TProperty, TDatabase>
{
    TDatabase ConvertToDatabase(TProperty value);
}

public interface IPropertyConverter<TDatabase, TProperty> :
    IReadPropertyConverter<TDatabase, TProperty>,
    IWritePropertyConverter<TProperty, TDatabase>
{
}
```

## Riscos conhecidos

- Runtime e generated ja possuem conversao duplicada; TypeHandler funciona no
  runtime mapped, mas nao no helper gerado atual.
- `DapperTypeHandlerAdapter` consulta detalhe interno `SqlMapper.TypeHandlerCache<T>.Parse(object)`;
  isso e uma fronteira de compatibilidade sensivel.
- Write conversion por propriedade nao e trivial em Dommel porque o hook atual
  controla colunas/SQL, nao `DbParameter.Value`.
- Null conversion opt-in pode quebrar semantica de subarvores aninhadas se for
  introduzida cedo demais.
- Converter por reflection precisa de anotacoes de trimming e estrategia AOT.
- Caches atuais assumem configuracao efetivamente imutavel apos registro.
- Converter metadata ja existe, mas generated materializers ainda nao executam
  read converters por instancia/delegate; esses cenarios continuam no runtime
  fallback.
- `Convert...Using<TConverter, TDatabase>()` valida contrato por reflection de
  interfaces em configuration time; overloads por instancia/delegate oferecem
  caminho mais favoravel a AOT.
- TypeHandler no generated path permanece fora do escopo.
- Write converters em Dommel permanecem metadata-only: `Insert`/`Update` do
  Dommel passam a entidade original ao Dapper e nao expoem hook publico para
  substituir `DbParameter.Value` por propriedade.

## Validacao do Prompt 10.1

- `dotnet restore ./Dapper.FluentMap.sln`: sucesso.
- `dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 357 testes aprovados no total.
- `dotnet pack`: nao executado; este prompt alterou somente documentacao SDD e
  nao mudou empacotamento ou codigo produtivo.

## Validacao do Prompt 10.2

- `dotnet test ./test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj --configuration Release --filter FullyQualifiedName~PropertyConversionMetadataTests`:
  sucesso, 17 testes aprovados.
- `dotnet restore ./Dapper.FluentMap.sln`: sucesso.
- `dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 374 testes aprovados no total.
- `dotnet pack ./src/Dapper.FluentMap/Dapper.FluentMap.csproj --configuration Release --no-build --output ./artifacts/packages`:
  sucesso, pacote criado em `artifacts/packages/Dapper.FluentMap.2.0.0.nupkg`;
  warning conhecido `NU5125` sobre `licenseUrl` depreciado.

## Validacao do Prompt 10.3

- `dotnet test test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter FullyQualifiedName~RuntimeReadConversionTests`:
  sucesso, 11 testes aprovados.
- `dotnet restore .\Dapper.FluentMap.sln`: sucesso.
- `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 385 testes aprovados no total.
- `dotnet pack .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-build --output .\artifacts\packages`:
  sucesso, pacote criado em `artifacts/packages/Dapper.FluentMap.2.0.0.nupkg`;
  warning conhecido `NU5125` sobre `licenseUrl` depreciado.
- `dotnet run --configuration Release --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj -- --filter "*MaterializationSteadyStateBenchmarks.QueryMappedRuntime*" --job Dry --warmupCount 1 --minIterationCount 1 --maxIterationCount 2`:
  sucesso. Resultado observado: no converter 2.768 ms / 142.55 KB, simple
  converter 1.676 ms / 189.43 KB, TypeHandler 1.606 ms / 165.98 KB, property
  converter 1.334 ms / 165.98 KB. BenchmarkDotNet alertou que os tempos de
  iteracao ficaram abaixo de 100 ms; usar como baseline curta, nao como
  conclusao estatistica final.

## Validacao do Prompt 10.4

- `dotnet test .\test\Dapper.FluentMap.Generators.Tests\Dapper.FluentMap.Generators.Tests.csproj --configuration Release --filter FullyQualifiedName~MappingRegistrationGeneratorTests`:
  sucesso, 26 testes aprovados.
- `dotnet test .\test\Dapper.FluentMap.GeneratedRegistration.Tests\Dapper.FluentMap.GeneratedRegistration.Tests.csproj --configuration Release --filter FullyQualifiedName~GeneratedRegistrationIntegrationTests`:
  sucesso, 4 testes aprovados.
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter FullyQualifiedName~PropertyConversionMetadataTests`:
  sucesso, 18 testes aprovados.
- `dotnet restore .\Dapper.FluentMap.sln`: sucesso.
- `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 391 testes aprovados no total.
- `dotnet pack .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-build --output .\artifacts\packages`:
  sucesso, pacote criado em `artifacts/packages/Dapper.FluentMap.2.0.0.nupkg`;
  warning conhecido `NU5125` sobre `licenseUrl` depreciado.
- `dotnet run --configuration Release --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj -- --filter "*MaterializationSteadyStateBenchmarks.QueryMapped*Converter*" --job Dry --warmupCount 1 --minIterationCount 1 --maxIterationCount 2`:
  sucesso. Resultado observado: generated simple converter 1.421 ms / 189.99
  KB, runtime property converter 1.453 ms / 165.98 KB, runtime no converter
  1.579 ms / 142.55 KB, runtime simple converter 1.885 ms / 189.43 KB,
  generated property converter 2.036 ms / 166.55 KB.
- `dotnet run --configuration Release --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj -- --filter "*MaterializationSteadyStateBenchmarks.QueryMappedSimple*" --job Dry --warmupCount 1 --minIterationCount 1 --maxIterationCount 2`:
  sucesso. Recorte sem converter: runtime fallback 3.100 ms / 361.63 KB,
  generated 3.270 ms / 362.82 KB. BenchmarkDotNet alertou que os tempos de
  iteracao ficaram abaixo de 100 ms; usar como baseline curta.
- `dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:PublishTrimmed=true -p:DefineConstants=AOT_SMOKE_GENERATED --output .\.tmp\aot-smoke\generated-trimmed` seguido de execucao do binario:
  sucesso, executavel retornou `generated:ok`; warnings esperados `IL2026` em
  `QueryMapped*` e `IL2104` em `Dapper.FluentMap`/`Dapper`.
- `dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:PublishAot=true -p:DefineConstants=AOT_SMOKE_GENERATED --output .\.tmp\aot-smoke\generated-aot`:
  bloqueado pelo ambiente com `Platform linker not found`; antes do bloqueio
  foram emitidos warnings esperados `IL2026` e `IL3050` nas chamadas
  `QueryMapped*`.

## Validacao do Prompt 10.5

- `dotnet restore ./Dapper.FluentMap.sln`: sucesso.
- `dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 391 testes aprovados no total.
- `dotnet test ./test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj --configuration Release --no-build`:
  sucesso, 21 testes Dommel aprovados.
- `dotnet pack`: nao executado; este prompt alterou somente documentacao SDD e
  nao mudou empacotamento ou codigo produtivo.

## Validacao do Prompt 10.6

- `dotnet test .\test\Dapper.FluentMap.Analyzers.Tests\Dapper.FluentMap.Analyzers.Tests.csproj --configuration Release --filter FullyQualifiedName~FluentMapConfigurationAnalyzerTests`:
  sucesso, 19 testes aprovados.
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter "FullyQualifiedName~RuntimeReadConversionTests|FullyQualifiedName~ConfigurationValidationTests"`:
  sucesso, 29 testes aprovados. Uma tentativa paralela anterior falhou por lock
  temporario de build em `Dapper.FluentMap.dll`; rerun sequencial passou.
- `dotnet test .\test\Dapper.FluentMap.GeneratedRegistration.Tests\Dapper.FluentMap.GeneratedRegistration.Tests.csproj --configuration Release --filter FullyQualifiedName~GeneratedRegistrationIntegrationTests`:
  sucesso, 5 testes aprovados.
- `dotnet test .\test\Dapper.FluentMap.Dommel.Tests\Dapper.FluentMap.Dommel.Tests.csproj --configuration Release --filter FullyQualifiedName~DommelPersistenceIntegrationTests`:
  sucesso, 5 testes aprovados.
- `dotnet restore .\Dapper.FluentMap.sln`: sucesso.
- `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 402 testes aprovados no total.
- `dotnet run --configuration Release --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj -- --filter "*MaterializationSteadyStateBenchmarks.QueryMapped*Converter*" --job Dry --warmupCount 1 --minIterationCount 1 --maxIterationCount 2`:
  sucesso. Resultado observado: runtime property converter 1.095 ms /
  165.98 KB, runtime simple converter 1.190 ms / 189.43 KB, generated property
  converter 1.203 ms / 166.55 KB, generated simple converter 1.295 ms /
  189.99 KB, runtime no converter 1.805 ms / 142.55 KB. BenchmarkDotNet
  alertou que os tempos de iteracao ficaram abaixo de 100 ms; usar como smoke
  representativo, nao conclusao estatistica.
- `dotnet pack .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-build --output .\artifacts\packages`:
  sucesso, pacote criado em `artifacts/packages/Dapper.FluentMap.2.0.0.nupkg`;
  warning conhecido `NU5125` sobre `licenseUrl` depreciado.
- `dotnet pack .\src\Dapper.FluentMap.Analyzers\Dapper.FluentMap.Analyzers.csproj --configuration Release --no-build --output .\artifacts\packages`:
  sucesso, pacote criado em
  `artifacts/packages/Dapper.FluentMap.Analyzers.2.0.0.nupkg`.

## Interacao com Dapper TypeHandler

Precedencia proposta para `QueryMapped*`:

```text
property read converter
    -> Dapper TypeHandler<TProperty>
    -> FluentMap default conversion
```

Precedencia proposta para escrita futura:

```text
property write converter
    -> Dapper TypeHandler<TProperty>
    -> Dapper/provider parameter default
```

Apos Prompt 10.5, essa precedencia continua especificacao futura, nao
comportamento Dommel implementado. No Dommel atual, sem write converter
executado, o fluxo permanece:

```text
Dommel Insert/Update
    -> Dapper parameterization da entidade original
    -> Dapper TypeHandler<TProperty>/provider
```

Quando write conversion for implementada, nao deve haver composicao implicita
`PropertyConverter -> TypeHandler<TProperty>` na mesma propriedade.

APIs normais do Dapper continuam fora do controle property-scoped do FluentMap:

```text
connection.Query<T>()
    -> Dapper type map para nomes/construtores
    -> Dapper/provider conversion
```

## Arquivos importantes

- `.sdd/etapa-10/01-conversion-landscape.md`
- `.sdd/etapa-10/02-property-conversion-spec.md`
- `.sdd/etapa-10/03-converter-contract-design.md`
- `.sdd/etapa-10/04-runtime-conversion.md`
- `.sdd/etapa-10/05-performance-baseline.md`
- `.sdd/etapa-10/06-generated-conversion.md`
- `.sdd/etapa-10/07-write-conversion.md`
- `.sdd/etapa-10/08-conversion-diagnostics.md`
- `.sdd/etapa-10/DECISIONS.md`
- `.sdd/etapa-10/STATUS.md`
- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`
- `src/Dapper.FluentMap/Materialization/MappedRowMaterializer.cs`
- `src/Dapper.FluentMap/Compatibility/DapperTypeHandlerAdapter.cs`
- `src/Dapper.FluentMap.Generators/MappingRegistrationGenerator.cs`
- `src/Dapper.FluentMap.Generators/AnalyzerReleases.Unshipped.md`
- `src/Dapper.FluentMap.Analyzers/FluentMapConfigurationAnalyzer.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMap.cs`
- `src/Dapper.FluentMap/Mapping/PropertyConversionMetadata.cs`
- `src/Dapper.FluentMap/Mapping/PropertyPersistenceMetadata.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap.Dommel/Resolvers/DommelPersistenceSqlBuilder.cs`
- `src/Dapper.FluentMap.Dommel/Resolvers/DommelPersistenceMetadata.cs`
- `src/Dapper.FluentMap.Dommel/Resolvers/DommelPropertyResolver.cs`
- `test/Dapper.FluentMap.Tests/ValueObjectMaterializationTests.cs`
- `test/Dapper.FluentMap.Tests/AdvancedQueryHardeningTests.cs`
- `test/Dapper.FluentMap.Tests/PropertyConversionMetadataTests.cs`
- `test/Dapper.FluentMap.Tests/RuntimeReadConversionTests.cs`
- `test/Dapper.FluentMap.GeneratedRegistration.Tests/GeneratedRegistrationIntegrationTests.cs`
- `test/Dapper.FluentMap.AotSmoke/Program.cs`
- `benchmarks/Dapper.FluentMap.Benchmarks/Program.cs`

## Validacao do Prompt 10.7

- `dotnet restore .\Dapper.FluentMap.sln`: sucesso.
- `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 402 testes aprovados no total.
- `dotnet test .\test\Dapper.FluentMap.Dommel.Tests\Dapper.FluentMap.Dommel.Tests.csproj --configuration Release --no-build`:
  sucesso, 22 testes Dommel aprovados.
- `dotnet run --configuration Release --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj -- --filter "*MaterializationSteadyStateBenchmarks.QueryMapped*Converter*" --job Dry --warmupCount 1 --minIterationCount 1 --maxIterationCount 2`:
  sucesso. Resultado observado: runtime property converter 1.390 ms /
  165.98 KB, generated property converter 1.421 ms / 166.55 KB, runtime no
  converter 1.536 ms / 142.55 KB, runtime simple converter 2.036 ms /
  189.43 KB, generated simple converter 2.206 ms / 189.99 KB.
- `dotnet run --configuration Release --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj -- --filter "*MaterializationSteadyStateBenchmarks.DapperPure" --job Dry --warmupCount 1 --minIterationCount 1 --maxIterationCount 2`:
  sucesso. Resultado observado: DapperPure 2.071 ms / 283.22 KB.
- `dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:PublishTrimmed=true -p:DefineConstants=AOT_SMOKE_GENERATED --output .\.tmp\aot-smoke\generated-trimmed` seguido de execucao do binario:
  sucesso, executavel retornou `generated:ok`; warnings esperados `IL2026` em
  `QueryMapped*` e `IL2104` em `Dapper.FluentMap`/`Dapper`.
- `dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:PublishAot=true -p:DefineConstants=AOT_SMOKE_GENERATED --output .\.tmp\aot-smoke\generated-aot`:
  bloqueado pelo ambiente com `Platform linker not found`; antes do bloqueio
  foram emitidos warnings esperados `IL2026` e `IL3050` nas chamadas
  `QueryMapped*`.
- `dotnet pack`: nao executado no Prompt 10.7; as alteracoes foram
  documentacao/SDD e nao mudaram assemblies ou empacotamento.

## Ultimo prompt executado

Ultimo prompt executado: 10.7
