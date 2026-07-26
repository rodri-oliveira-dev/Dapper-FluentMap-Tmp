# 03 - Testes De Integracao Com Dapper

## Specification

Criar uma baseline pequena e deterministica de testes de integracao que exercite o comportamento publico do `Dapper.FluentMap` atraves do proprio Dapper materializando objetos a partir de SQL.

O fluxo protegido e:

```text
SQL
|
Dapper Query
|
ITypeMap do FluentMap
|
Objeto materializado
|
Assert sobre comportamento observavel
```

Fora do escopo:

- Docker, Testcontainers, PostgreSQL, SQL Server ou servicos externos;
- redesign de estado global, registry ou cache;
- alteracoes funcionais no core;
- cobertura exaustiva de todos os testes unitarios existentes;
- mudanca de target dos projetos de teste.

## Discovery

Arquivos analisados:

- `AGENTS.md`
- `docs/sdd/etapa-1/README.md`
- `docs/sdd/etapa-1/status.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-1/01-reflection-helper.md`
- `docs/sdd/etapa-1/02-mapping-composition.md`
- `Dapper.FluentMap.sln`
- `src/Dapper.FluentMap/Dapper.FluentMap.csproj`
- `src/Dapper.FluentMap/FluentMapper.cs`
- `src/Dapper.FluentMap/TypeMaps/MultiTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentConventionTypeMap.cs`
- `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj`
- `test/Dapper.FluentMap.Tests/ManualMappingTests.cs`
- `test/Dapper.FluentMap.Tests/ConventionTests.cs`
- `test/Dapper.FluentMap.Tests/MappingCompositionTests.cs`
- `test/Dapper.FluentMap.Tests/TestEntity.cs`

Entregas anteriores:

- `01 - ReflectionHelper` esta marcada como `Concluido` em `status.md`.
- `02 - Composicao de mappings` esta marcada como `Concluido` em `status.md`.

Suite atual:

- framework de testes: xUnit v2;
- runner: VSTest (`Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio`);
- target real dos projetos de teste: `netcoreapp3.1`;
- SDK local: `10.0.302`;
- nao ha `global.json`, `Directory.Build.props` ou `Directory.Packages.props` relevantes;
- a suite principal nao tinha testes com conexao real ou provider SQL;
- os testes existentes exercitavam `SqlMapper.GetTypeMap(...).GetMember(...)`, mas nao `Query<T>`.

Dependencias existentes:

- `Dapper.FluentMap` referencia `Dapper` `2.0.35`;
- `Dapper.FluentMap.Tests` referenciava apenas o projeto core, xUnit e VSTest.

Estado global e isolamento:

- `FluentMapper.EntityMaps` e `FluentMapper.TypeConventions` sao dicionarios estaticos globais;
- `FluentMapper.Initialize(...)` reutiliza uma instancia estatica de `FluentMapConfiguration`;
- `SqlMapper.SetTypeMap(...)` altera o registro global de type maps do Dapper por tipo;
- `MultiTypeMap.TypePropertyMapCache` e um cache estatico compartilhado, sem reset publico;
- `ManualMappingTests.cs` desabilita paralelismo no assembly com `CollectionBehavior(DisableTestParallelization = true)`;
- testes existentes limpam `EntityMaps` e `TypeConventions`; `MappingCompositionTests` tambem chama `SqlMapper.SetTypeMap(type, null)` para os tipos afetados.

Riscos identificados:

- testes que reutilizam o mesmo tipo com configuracoes diferentes podem sofrer interferencia por `SqlMapper.SetTypeMap`;
- o cache estatico pode reter misses ou hits por chave `type.FullName + columnName`;
- nao ha mecanismo publico ou interno dedicado para reset atomico do estado global;
- reabilitar paralelismo sem resolver o estado global seria inseguro.

## Decision

Provider escolhido: `Microsoft.Data.Sqlite` com SQLite in-memory.

Motivos:

- roda localmente e sem rede;
- nao exige Docker, servico externo ou processo separado;
- permite exercitar `IDbConnection`, SQL real e `Dapper.QuerySingle<T>`;
- adiciona somente uma dependencia de teste;
- a versao `3.1.32` e compativel com o target atual `netcoreapp3.1`, evitando misturar modernizacao de runtime nesta entrega.

Estrategia de banco:

- cada teste abre uma nova `SqliteConnection` com `Data Source=:memory:`;
- os testes usam `SELECT` direto para projetar uma linha deterministica;
- a conexao e descartada ao final do teste;
- nenhum arquivo temporario de banco e criado.

Estrategia de isolamento:

- cada teste usa um tipo de entidade especifico, evitando colisao no cache por tipo e coluna;
- antes e depois de cada teste, `EntityMaps` e `TypeConventions` sao limpos;
- antes e depois de cada teste, `SqlMapper.SetTypeMap(type, null)` remove o type map do Dapper para os tipos tocados;
- o cache interno de `MultiTypeMap` nao e limpo porque nao ha API para isso e a Entrega 4 deve tratar registry/cache.

Estrategia de paralelismo:

- o assembly ja tem paralelismo desabilitado;
- isso continua necessario por causa do estado global do FluentMap e do registro global do Dapper;
- a entrega nao tenta resolver essa restricao arquitetural.

Cenarios selecionados:

- mapping padrao do Dapper;
- mapping explicito de nome de coluna;
- convention por prefixo;
- composicao explicit + convention;
- override explicito sobre convention;
- correcao da Entrega 1 exercitada via materializacao real com propriedade `Format`;
- mapping explicito case-insensitive.

## Delivery

Implementacao:

- adicionada dependencia `Microsoft.Data.Sqlite` `3.1.32` ao projeto `Dapper.FluentMap.Tests`;
- adicionada a classe `DapperIntegrationTests`;
- cada teste usa `Dapper.QuerySingle<T>` contra SQLite in-memory;
- os asserts validam propriedades materializadas, nao detalhes internos de `ITypeMap`;
- nenhum codigo de producao foi alterado.

Testes adicionados:

- `DefaultDapperMappingShouldMaterializeProperties`
- `ExplicitMappingShouldMaterializeConfiguredColumn`
- `ConventionShouldMaterializeConfiguredColumns`
- `ExplicitMappingAndConventionShouldMaterializeTogether`
- `ExplicitMappingShouldOverrideConventionDuringMaterialization`
- `ExpressionResolvedPropertyShouldMaterializeWhenNameCollidesWithStringMember`
- `CaseInsensitiveExplicitMappingShouldMaterializeColumnWithDifferentCase`

Arquivos alterados:

- `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj`
- `test/Dapper.FluentMap.Tests/DapperIntegrationTests.cs`
- `docs/sdd/etapa-1/status.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-1/03-dapper-integration-tests.md`

## Validation

Comandos executados:

- `dotnet restore .\Dapper.FluentMap.sln`
  - resultado: falhou por metadado corrompido no cache NuGet global (`microsoft.netcore.targets`).
- `NUGET_PACKAGES=.\.nuget-temp dotnet restore .\Dapper.FluentMap.sln`
  - resultado: sucesso.
- `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`
  - resultado: sucesso antes da alteracao.
- `DOTNET_ROLL_FORWARD=Major dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~MappingCompositionTests"`
  - resultado: sucesso antes da alteracao, 8 testes aprovados.
- `NUGET_PACKAGES=.\.nuget-temp dotnet restore`
  - resultado: sucesso.
- `NUGET_PACKAGES=.\.nuget-temp dotnet build --configuration Release --no-restore`
  - resultado: sucesso, 0 warnings, 0 erros.
- `NUGET_PACKAGES=.\.nuget-temp DOTNET_ROLL_FORWARD=Major dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~DapperIntegrationTests"`
  - resultado: sucesso, 7 testes aprovados.
- `NUGET_PACKAGES=.\.nuget-temp DOTNET_ROLL_FORWARD=Major dotnet test --configuration Release --no-build`
  - resultado: sucesso, 38 testes aprovados no projeto core e 7 testes aprovados no projeto Dommel.
- `NUGET_PACKAGES=.\.nuget-temp DOTNET_ROLL_FORWARD=Major dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --no-build`
  - resultado: sucesso, 38 testes aprovados.
- `NUGET_PACKAGES=.\.nuget-temp DOTNET_ROLL_FORWARD=Major dotnet test --configuration Release`
  - resultado: sucesso, 38 testes aprovados no projeto core e 7 testes aprovados no projeto Dommel.
- `NUGET_PACKAGES=.\.nuget-temp DOTNET_ROLL_FORWARD=Major dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release`
  - resultado: sucesso, 38 testes aprovados.

Observacoes:

- `DOTNET_ROLL_FORWARD=Major` foi necessario apenas para executar os testes `netcoreapp3.1` neste ambiente, que possui runtimes 8.0 e 10.0, mas nao o runtime 3.1.
- Os testes de integracao usam apenas SQLite in-memory e nao persistem arquivos temporarios de banco.
- A primeira tentativa de build apos adicionar a dependencia falhou porque restore e build foram executados em paralelo; apos restore sequencial, o build passou.

## Follow-Up Para Entrega 4

- Criar uma estrategia explicita para reset ou substituicao segura do estado global em testes.
- Definir invalidacao do cache estatico quando mapas ou conventions forem alterados.
- Avaliar chave de cache estruturada que considere tipo, coluna, comparacao e estrategia instalada.
- Avaliar encapsulamento dos dicionarios publicos globais antes de qualquer tentativa de reabilitar paralelismo.
- Considerar se `FluentMapper.Initialize(...)` deve continuar reutilizando uma configuracao estatica mutavel.

## Achado De Baseline

- A execucao completa da suite revelou que `ReflectionHelperTests.GetMemberInfo_ReturnsProperty_OfDerivedType` ainda esperava o `PropertyInfo` retornado por `typeof(DerivedTestEntity).GetProperty("Id")`.
- Essa expectativa conflitava com a decisao da Entrega 1 de retornar diretamente o `MemberExpression.Member`, que para propriedade herdada aponta para `TestEntity.Id`.
- O teste foi ajustado para validar a decisao ja documentada; nenhum codigo de producao foi alterado.
