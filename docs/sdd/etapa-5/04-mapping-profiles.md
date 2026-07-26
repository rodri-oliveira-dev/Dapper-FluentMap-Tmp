# 04 - Mapping Profiles

## Specification

O problema desta entrega e permitir que a mesma entidade seja materializada a partir de shapes SQL distintos sem trocar o `ITypeMap` global do Dapper durante a operacao.

Exemplo:

```sql
SELECT customer_id, customer_name
SELECT id, legal_name
```

Ambas podem materializar `Customer`, mas exigem mappings diferentes.

Requisitos preservados:

- `connection.Query<Customer>(sql)` continua usando o mapping default registrado por `AddMap(...)`;
- profiles sao opt-in por operacao;
- nenhuma query troca `SqlMapper.SetTypeMap(...)` temporariamente;
- queries simultaneas com profiles diferentes nao vazam mappings;
- nested mappings e Value Objects imutaveis continuam usando o caminho `QueryMapped*`;
- analyzer, source generator e `Explain` distinguem default de profiles.

## Discovery

Arquivos analisados:

- `AGENTS.md`
- `.agents/skills/run-tests/SKILL.md`
- `.agents/skills/dotnet-aot-compat/SKILL.md`
- `docs/sdd/etapa-5/README.md`
- `docs/sdd/etapa-5/status.md`
- `docs/sdd/etapa-5/decisions.md`
- `docs/sdd/etapa-5/01-nested-materialization-spike.md`
- `docs/sdd/etapa-5/02-nested-object-materialization.md`
- `docs/sdd/etapa-5/03-value-objects.md`
- `docs/sdd/etapa-1/04-mapping-registry-cache.md`
- `docs/sdd/etapa-3/01-mapping-registration.md`
- `docs/sdd/etapa-3/03-diagnostics-api.md`
- `docs/sdd/etapa-4/02-trimming-aot.md`
- `docs/sdd/etapa-4/03-source-generator.md`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/QueryMappedExtensions.cs`
- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`
- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `src/Dapper.FluentMap.Analyzers/FluentMapConfigurationAnalyzer.cs`
- `src/Dapper.FluentMap.Generators/MappingRegistrationGenerator.cs`

Entregas anteriores confirmadas:

```text
01 - Spike nested/value-object -> Concluido, commit ff64f96
02 - Nested object materialization -> Concluido, commit 2ed4af5
03 - Value Objects imutaveis -> Concluido, commit 68c9959
```

Fontes primarias do Dapper 2.1.79 analisadas:

- `SqlMapper.ITypeMap.cs`: https://github.com/DapperLib/Dapper/blob/72a54c475f75e18cb93cba0809d00a5e6e49efd9/Dapper/SqlMapper.ITypeMap.cs
- `SqlMapper.cs`: https://github.com/DapperLib/Dapper/blob/72a54c475f75e18cb93cba0809d00a5e6e49efd9/Dapper/SqlMapper.cs
- `SqlMapper.Async.cs`: https://github.com/DapperLib/Dapper/blob/72a54c475f75e18cb93cba0809d00a5e6e49efd9/Dapper/SqlMapper.Async.cs

Conclusoes sobre o Dapper:

- o `ITypeMap` e resolvido por `Type`, nao por operacao;
- `CommandDefinition` transporta SQL, parametros, transaction, timeout, command type, flags e cancellation token, mas nao um type map por query;
- as APIs async do Dapper tambem materializam por `Type` e cache interno do Dapper;
- multi-mapping usa `splitOn` e callbacks de composicao, mas nao representa `MemberPath` nem profile identity;
- nao ha API publica no Dapper 2.1.79 para fornecer `ITypeMap` ou materializer customizado por operacao.

## Alternatives

### A - Mutation scope

Modelo:

```text
SetTypeMap(profile A)
Query()
SetTypeMap(profile B)
```

Rejeitada.

Motivos:

- `SqlMapper.SetTypeMap` altera estado global por `Type`;
- duas queries simultaneas poderiam observar o profile errado;
- async pode suspender e retomar em outro momento enquanto outro profile foi instalado;
- caches internos do Dapper podem ser aquecidos com uma identidade de type map que nao representa a operacao seguinte;
- exigiria lock global por entidade e reduziria concorrencia, alem de continuar vulneravel a consumidores externos chamando Dapper diretamente.

### B - Query wrapper

Aceita como superficie publica.

`QueryMapped*` ja era a API opt-in da Etapa 5 para materializacao controlada pelo FluentMap. Esta entrega a estende com overloads tipados por profile.

### C - Custom materializer da Etapa 5

Aceita como implementacao.

`NestedMaterializationPlan` ja controla `DbDataReader`, `MemberPath`, null semantics, construtores e Value Objects. Profiles passam a selecionar outro conjunto de mappings antes de criar/cachear o plano.

### D - Generated query/materializer

Adiada.

O source generator atual gera registro, nao leitura de `DbDataReader`. Materializers gerados continuam sendo o caminho futuro preferencial para performance e Native AOT, mas nao sao necessarios para entregar selecao query-scoped segura.

### E - Dapper API publica existente

Nao encontrada no Dapper 2.1.79.

As APIs publicas de `Query`, `QueryAsync`, `ExecuteReader`, `ExecuteReaderAsync`, `CommandDefinition` e multi-mapping nao aceitam type map/materializer por operacao.

## Decision

API escolhida:

```csharp
public interface IMappingProfile
{
}

public interface IProfileMap<TProfile>
    where TProfile : IMappingProfile
{
}

configuration.AddProfile<LegacyCustomerMap>();

connection.QueryMapped<Customer, LegacyProfile>(sql);
connection.QueryMappedSingle<Customer, LegacyProfile>(sql);
connection.QueryMappedAsync<Customer, LegacyProfile>(sql);
connection.QueryMappedSingleAsync<Customer, LegacyProfile>(sql);

FluentMapper.Explain<Customer, LegacyProfile>();
```

Exemplo:

```csharp
public sealed class LegacyProfile : IMappingProfile
{
}

public sealed class LegacyCustomerMap :
    EntityMap<Customer>,
    IProfileMap<LegacyProfile>
{
    public LegacyCustomerMap()
    {
        Map(customer => customer.Id).ToColumn("id");
        Map(customer => customer.Name).ToColumn("legal_name");
    }
}
```

Identidade do profile:

- fortemente tipada por marker `TProfile`;
- um map de profile implementa exatamente um `IProfileMap<TProfile>`;
- a entidade continua sendo inferida por `IEntityMap<TEntity>`;
- strings nao foram usadas para evitar typos silenciosos.

Modelo de registry:

```text
EntityType
  Default map: EntityMaps[EntityType]
  Profile maps: ProfileMaps[(EntityType, ProfileType)]
  Conventions/naming policies: TypeConventions[EntityType]
```

Precedencia efetiva no caminho de profile:

```text
Profile explicit
Profile inherited no mesmo TProfile
Entity conventions/naming policies atuais
Dapper/default behavior
```

Conventions e naming policies continuam registradas por entidade, nao por profile, nesta entrega. Elas sao aplicadas de forma read-only tambem em profiles e podem ser sobrescritas por mappings explicitos do profile. Per-profile conventions ficam como divida futura, porque exigem uma API adicional e regras proprias de composicao.

Inheritance:

- `IncludeBase<TBase>()` em um default map continua procurando o default map da base;
- `IncludeBase<TBase>()` em um profile map procura a base no mesmo `TProfile`;
- nao ha mistura silenciosa de default base map dentro de um profile alternativo.

Cache:

- `MappingCacheKey` agora inclui `ProfileType`;
- `MaterializationPlanCacheKey` agora inclui `ProfileType`;
- o profile e resolvido antes do loop de leitura;
- nao ha lookup textual por row;
- registro/reset invalidam planos por entidade.

Thread/async safety:

- a selecao de profile esta nos generics da operacao;
- nenhum `AsyncLocal`, thread-static ou mutacao global e usado para selecionar profile;
- o Dapper type map global continua representando apenas o default;
- `QueryMappedAsync*` usa `CommandDefinition` e `ExecuteReaderAsync`, preservando cancellation token quando o consumidor passa o command overload.

Compatibilidade:

- `AddMap(...)`, `AddMap<TMap>()`, `Dapper.Query<T>()`, `QueryMapped<T>()` e `Explain<T>()` foram preservados;
- `Dapper.Query<T>()` nao ve profiles;
- profiles nao sao registrados por `SqlMapper.SetTypeMap`.

Limitacoes:

- profiles sao suportados no caminho `QueryMapped*`, nao em `Dapper.Query<T>()`;
- multi-mapping do Dapper nao recebeu overload de profile;
- unbuffered streaming nao foi implementado; os overloads retornam lista materializada como o `QueryMapped<T>()` existente;
- per-profile conventions/naming policies ficam para etapa futura;
- generator continua sendo registration generator, nao materializer generator.

## Delivery

Arquivos adicionados:

- `src/Dapper.FluentMap/MappingProfileKey.cs`
- `test/Dapper.FluentMap.Tests/MappingProfileTests.cs`
- `docs/sdd/etapa-5/04-mapping-profiles.md`

Arquivos alterados:

- `README.md`
- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `src/Dapper.FluentMap/Diagnostics/MappingExplanation.cs`
- `src/Dapper.FluentMap/FluentMapper.cs`
- `src/Dapper.FluentMap/Mapping/EntityMap.cs`
- `src/Dapper.FluentMap/MappingCacheKey.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/Materialization/MaterializationPlanCacheKey.cs`
- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`
- `src/Dapper.FluentMap/QueryMappedExtensions.cs`
- `src/Dapper.FluentMap.Analyzers/AnalyzerReleases.Unshipped.md`
- `src/Dapper.FluentMap.Analyzers/FluentMapConfigurationAnalyzer.cs`
- `src/Dapper.FluentMap.Generators/AnalyzerReleases.Unshipped.md`
- `src/Dapper.FluentMap.Generators/MappingRegistrationGenerator.cs`
- `test/Dapper.FluentMap.Analyzers.Tests/FluentMapConfigurationAnalyzerTests.cs`
- `test/Dapper.FluentMap.AotSmoke/Program.cs`
- `test/Dapper.FluentMap.GeneratedRegistration.Tests/GeneratedRegistrationIntegrationTests.cs`
- `test/Dapper.FluentMap.Generators.Tests/MappingRegistrationGeneratorTests.cs`
- `docs/sdd/etapa-5/README.md`
- `docs/sdd/etapa-5/status.md`
- `docs/sdd/etapa-5/decisions.md`

Analyzer:

- `DFM009`: `AddProfile<TMap>()` com map sem exatamente um `IEntityMap<TEntity>` e um `IProfileMap<TProfile>`;
- `DFM010`: duas chamadas conhecidas ao mesmo entity/profile no mesmo metodo de configuracao;
- IDs existentes `DFM001` a `DFM005` foram preservados.

Source generator:

- default maps continuam gerando `.AddMap<TMap>()`;
- profile maps geram `.AddProfile<TMap>()`;
- `DFM007` continua valendo apenas para mais de um default map da mesma entidade;
- `DFM008` detecta mais de um generated profile map para a mesma entidade e o mesmo profile.

## Tests

Testes novos cobrem:

- default mapping via `Dapper.Query<T>()`;
- profile alternativo via `QueryMapped<T,TProfile>()`;
- duas queries sequenciais com profiles diferentes;
- default depois de profile;
- queries paralelas sync com profiles distintos;
- queries async concorrentes com profiles distintos;
- nested mappings em profiles;
- Value Objects em profiles;
- inheritance no mesmo profile marker;
- naming policy de entidade aplicada no profile;
- constructor mapping em profile;
- profile inexistente;
- duplicidade de profile;
- `Explain<TEntity,TProfile>()`;
- generator com profile;
- generator rejeitando profile duplicado;
- analyzer validando `AddProfile<TMap>()`;
- analyzer rejeitando registro duplicado conhecido.

## Performance

Comparacao arquitetural:

| Caminho | Resolucao por operacao | Hot path por row |
|---|---|---|
| Dapper/FluentMap default | Dapper resolve type map/cache por `Type` e shape | IL/materializer do Dapper |
| QueryMapped default | plano cacheado por entidade + colunas | delegates precomputados |
| QueryMapped profile | plano cacheado por entidade + profile + colunas | delegates precomputados |

Overhead esperado do profile:

- uma chave de cache maior;
- um lookup de `ProfileMaps[(EntityType, ProfileType)]` ao criar o plano;
- nenhum lookup extra por row em relacao ao `QueryMapped<T>()` default.

Nao foi adicionado benchmark formal nesta entrega. O criterio de aceite foi garantir ausencia de vazamento em concorrencia e evitar resolucao textual por row.

## AOT And Trimming

- `QueryMapped*` continua anotado com `RequiresUnreferencedCode` e `RequiresDynamicCode`, porque usa reflection e expression compilation;
- `AddProfile<TMap>()` segue o mesmo modelo de `AddMap<TMap>()`, com inferencia por interfaces anotada;
- source generation de registro suporta profiles e evita assembly scanning;
- smoke AOT/trimming valida registro/explain de profile nos caminhos explicit e generated;
- Native AOT runtime completo permanece nao validado no ambiente por ausencia do platform linker C++.

## Validation

Validacoes localizadas ja executadas durante a implementacao:

```text
dotnet build .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Debug
dotnet build .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Debug
dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~MappingProfileTests"
dotnet test .\test\Dapper.FluentMap.Analyzers.Tests\Dapper.FluentMap.Analyzers.Tests.csproj
dotnet test .\test\Dapper.FluentMap.Generators.Tests\Dapper.FluentMap.Generators.Tests.csproj
dotnet test .\test\Dapper.FluentMap.GeneratedRegistration.Tests\Dapper.FluentMap.GeneratedRegistration.Tests.csproj
```

Resultados:

- core: sucesso, 0 warnings, 0 erros;
- testes do core: sucesso, 0 warnings, 0 erros;
- `MappingProfileTests`: sucesso, 15 testes aprovados;
- analyzer: sucesso, 9 testes aprovados;
- generator: sucesso, 14 testes aprovados;
- generated-registration integration: sucesso, 1 teste aprovado.

Validacao final completa deve registrar:

```text
dotnet restore
dotnet build
dotnet test
dotnet build --configuration Release
dotnet test --configuration Release
```

Resultado final:

- `dotnet restore`: sucesso;
- `dotnet build`: sucesso, 0 warnings, 0 erros;
- `dotnet test`: sucesso, 181 testes do core, 7 Dommel, 9 analyzer, 14 generator e 1 generated-registration integration;
- `dotnet build --configuration Release`: sucesso, 0 warnings, 0 erros;
- `dotnet test --configuration Release`: sucesso com os mesmos 212 testes totais;
- `dotnet pack` do core: pacote `Dapper.FluentMap.2.0.0.nupkg` criado; warning legado `NU5125` sobre `PackageLicenseUrl`;
- `dotnet pack` do analyzer e generator: pacotes criados com sucesso.

Inspecao de pacotes:

- core contem `lib/netstandard2.0/Dapper.FluentMap.dll` e XML docs;
- generator contem `README.md` e `analyzers/dotnet/cs/Dapper.FluentMap.Generators.dll`, sem `lib/`;
- analyzer contem `README.md` e `analyzers/dotnet/cs/Dapper.FluentMap.Analyzers.dll`, sem `lib/`;
- nenhum pacote contem projetos de teste ou artefatos indevidos.

Smokes AOT/trimming:

```text
dotnet run --project .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release
dotnet run --project .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:DefineConstants=AOT_SMOKE_GENERATED
dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishTrimmed=true -p:TrimMode=full -p:PublishAot=false -p:TrimmerSingleWarn=false -p:ILLinkTreatWarningsAsErrors=false
dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishTrimmed=true -p:TrimMode=full -p:PublishAot=false -p:DefineConstants=AOT_SMOKE_GENERATED -p:TrimmerSingleWarn=false -p:ILLinkTreatWarningsAsErrors=false
dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishAot=true -p:TrimmerSingleWarn=false -p:ILLinkTreatWarningsAsErrors=false -p:MSBuildWarningsAsMessages=
```

Resultados:

- AOT smoke explicit: `explicit:ok`;
- AOT smoke generated: `generated:ok`;
- publish trimmed explicit: sucesso, runtime `explicit:ok`, sem warnings FluentMap-owned; warnings restantes pertencem ao Dapper;
- publish trimmed generated: sucesso, runtime `generated:ok`, sem warnings FluentMap-owned; warnings restantes pertencem ao Dapper;
- publish Native AOT explicit: falhou no ambiente com `Platform linker not found`; runtime Native AOT nao foi validado.

## Semantic Commit

Mensagem planejada:

```text
feat: add query-scoped mapping profiles
```
