# 04 - MappingRegistry E Cache

## Specification

Introduzir uma estrutura interna de registry para centralizar a configuracao de mappings do core e substituir o cache ativo de propriedades, antes baseado em chaves de string concatenadas, por chaves estruturadas.

Objetivos:

- preservar a API publica existente;
- manter a composicao definida na Entrega 2: mapping explicito, convention e fallback do Dapper;
- manter a baseline de integracao da Entrega 3;
- reduzir o espalhamento de estado entre `FluentMapper`, type maps e caches;
- definir invalidacao explicita do cache nas reconfiguracoes feitas pela API;
- preparar terreno para melhorias futuras sem redesenhar a API publica.

Fora do escopo:

- MemberPath;
- nested object materialization;
- Value Objects;
- inheritance mappings;
- records;
- constructor mapping;
- Roslyn analyzers;
- source generators;
- AOT;
- multiplos profiles;
- redesign completo da API publica.

## Discovery

Entregas anteriores confirmadas em `status.md`:

- `01 - ReflectionHelper`: Concluído.
- `02 - Composicao de mappings`: Concluído.
- `03 - Testes de integracao`: Concluído.

Arquivos analisados:

- `AGENTS.md`
- `docs/sdd/etapa-1/README.md`
- `docs/sdd/etapa-1/status.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-1/01-reflection-helper.md`
- `docs/sdd/etapa-1/02-mapping-composition.md`
- `docs/sdd/etapa-1/03-dapper-integration-tests.md`
- `src/Dapper.FluentMap/FluentMapper.cs`
- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `src/Dapper.FluentMap/Configuration/FluentConventionConfiguration.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentConventionTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/MultiTypeMap.cs`
- `test/Dapper.FluentMap.Tests/ManualMappingTests.cs`
- `test/Dapper.FluentMap.Tests/ConventionTests.cs`
- `test/Dapper.FluentMap.Tests/MappingCompositionTests.cs`
- `test/Dapper.FluentMap.Tests/DapperIntegrationTests.cs`

Estado relacionado a mapping antes da mudanca:

| Estado | Escrita | Leitura | Lifetime | Thread safety | Invalidacao |
|---|---|---|---|---|---|
| `FluentMapper.EntityMaps` | `FluentMapConfiguration.AddMap` e testes por `Clear()` | `FluentTypeMap`, Dommel e testes | estatico por processo | `ConcurrentDictionary`, mas valores continuam mutaveis | manual em testes; sem cache reset |
| `FluentMapper.TypeConventions` | `FluentConventionConfiguration` e testes por `Clear()` | `FluentTypeMap`, `FluentConventionTypeMap`, Dommel e testes | estatico por processo | `ConcurrentDictionary`, mas lista era atualizada por helper nao atomico | manual em testes; sem cache reset |
| `_configuration` | `FluentMapper.Initialize` reutiliza a mesma instancia | callbacks de configuracao | estatico por processo | sem sincronizacao propria | nao aplicavel |
| `SqlMapper.SetTypeMap` | `FluentMapper.AddTypeMap` e `AddConventionTypeMap` | Dapper durante materializacao | global no Dapper por tipo | responsabilidade do Dapper | testes removiam por tipo |
| `MultiTypeMap.TypePropertyMapCache` | `FluentTypeMap` e `FluentConventionTypeMap` | `FluentTypeMap` e `FluentConventionTypeMap` | estatico por processo | `ConcurrentDictionary` | sem reset definido |

Problemas resolviveis nesta entrega:

- remover o cache ativo baseado em strings como `FluentMapTypeMap;{type.FullName};{columnName}`;
- centralizar escrita, leitura, instalacao de type map e invalidacao em um componente interno;
- tornar o reset de testes atomico para dicionarios, cache e type maps do Dapper dos tipos tocados;
- atualizar conventions com `ConcurrentDictionary.AddOrUpdate` e copia de lista, evitando mutacao in-place do valor compartilhado;
- manter os campos publicos existentes como visoes do storage interno por compatibilidade.

Problemas deliberadamente nao resolvidos:

- consumidores ainda podem mutar diretamente `EntityMaps` e `TypeConventions`, pois esses campos publicos fazem parte da compatibilidade existente;
- o registro global do Dapper continua necessario porque a extensibilidade de materializacao passa por `SqlMapper.SetTypeMap`;
- o paralelismo da suite continua desabilitado por causa de estado global historico e por Dommel ainda consumir os dicionarios publicos diretamente;
- `MultiTypeMap.TypePropertyMapCache` permanece como membro protegido para evitar quebra de compatibilidade, mas deixou de ser usado pelo core.

## Decision

Design adotado:

```text
FluentMapper public facade
           |
           v
internal MappingRegistry
           |
           +-- EntityMaps / TypeConventions public-compatible storage
           +-- structured mapping cache
           +-- SqlMapper.SetTypeMap integration
           |
           v
FluentMapTypeMap / FluentConventionTypeMap
           |
           v
Dapper DefaultTypeMap fallback
```

Dono do estado:

- `MappingRegistry` e o dono interno do storage de `EntityMaps`, `TypeConventions` e cache.
- `FluentMapper.EntityMaps` e `FluentMapper.TypeConventions` continuam publicos e apontam para os mesmos dicionarios do registry.
- `FluentMapConfiguration` e `FluentConventionConfiguration` passam a escrever via `FluentMapper.Registry`.
- `FluentMapTypeMap<TEntity>` e `FluentConventionTypeMap<TEntity>` passam a delegar resolucao ao registry.

Chave estruturada:

```csharp
MappingCacheKey
{
    Type Type;
    string ColumnName;
    MappingCacheOptions Options;
}
```

`MappingCacheOptions` diferencia:

- `FluentMap`: mapping explicito, convention e fallback posterior do Dapper;
- `ConventionOnly`: compatibilidade de `FluentConventionTypeMap<TEntity>`.

Comparacao de coluna e case sensitivity:

- a chave usa `ColumnName` com igualdade ordinal para diferenciar chamadas como `case_id` e `CASE_ID`;
- a decisao de match continua por `IPropertyMap.CaseSensitive`, preservando o comportamento atual;
- mudancas de configuracao invalidam as entradas do tipo afetado, entao alteracoes de case sensitivity por API nao reaproveitam resultados antigos.

Invalidacao:

- `AddEntityMap<TEntity>` invalida todas as entradas de cache do tipo e reinstala o type map composto no Dapper;
- `AddConvention(Type, Convention)` atualiza a lista de conventions, invalida o tipo e reinstala o type map composto;
- `Reset(params Type[])` limpa entity maps, conventions, cache e remove os type maps do Dapper para os tipos informados.

Thread safety:

- dicionarios globais continuam `ConcurrentDictionary`;
- o cache estruturado usa `ConcurrentDictionary<MappingCacheKey, MappingCacheEntry>`;
- misses sao cacheados como `MappingCacheEntry` com `PropertyInfo` nulo, evitando valor nulo direto no dicionario;
- conventions sao adicionadas por `AddOrUpdate` com copia da lista atual.

Compatibilidade:

- nenhuma API publica foi removida ou renomeada;
- `EntityMaps` e `TypeConventions` continuam campos publicos do mesmo tipo;
- `FluentConventionTypeMap<TEntity>` continua publico;
- `SqlMapper.GetTypeMap(typeof(T))` continua recebendo `FluentMapTypeMap<T>` nos fluxos internos de configuracao;
- foi adicionado `InternalsVisibleTo("Dapper.FluentMap.Tests")` para validar registry e cache sem tornar membros publicos.

## Delivery

Implementacao:

- adicionado `MappingRegistry` interno;
- adicionados `MappingCacheKey`, `MappingCacheOptions` e `MappingCacheStrategy`;
- `FluentMapper` passou a manter um registry interno e expor os dicionarios publicos como storage compatibilizado;
- `FluentMapConfiguration.AddMap` passou a registrar mappings pelo registry;
- `FluentConventionConfiguration` passou a registrar conventions pelo registry;
- `FluentMapTypeMap<TEntity>` passou a delegar resolucao composta ao registry;
- `FluentConventionTypeMap<TEntity>` passou a delegar resolucao convention-only ao registry;
- testes do core passaram a usar `FluentMapper.Reset(...)` interno;
- adicionado acesso interno ao assembly de testes.

Testes adicionados em `MappingRegistryTests`:

- cache hit para mesma chave estruturada;
- chaves distintas para tipos distintos;
- chaves distintas para nomes de coluna distintos;
- comportamento case-sensitive atual;
- reset/invalidacao de mapping cacheado;
- invalidacao de miss cacheado quando um mapping e registrado depois;
- leitura concorrente basica via type map do Dapper.

## Validation

Ambiente:

- SDK: `10.0.302`
- test runner: VSTest com xUnit v2
- projetos de teste: `netcoreapp3.1`
- `DOTNET_ROLL_FORWARD=Major` usado para executar testes `netcoreapp3.1` neste ambiente.

Comandos executados:

- `dotnet restore .\Dapper.FluentMap.sln`
  - resultado: falhou por metadado corrompido no cache NuGet global (`microsoft.netcore.targets` / `.nupkg.metadata` com byte `0x00`).
- `NUGET_PACKAGES=%TEMP%\dfm-nuget-packages-registry dotnet restore .\Dapper.FluentMap.sln`
  - resultado: sucesso.
- `NUGET_PACKAGES=%TEMP%\dfm-nuget-packages-registry dotnet build .\Dapper.FluentMap.sln --no-restore`
  - resultado: sucesso, 0 warnings, 0 erros.
- `NUGET_PACKAGES=%TEMP%\dfm-nuget-packages-registry DOTNET_ROLL_FORWARD=Major dotnet test .\Dapper.FluentMap.sln --no-build`
  - resultado: sucesso, 45 testes aprovados no core e 7 testes aprovados no Dommel.

- `dotnet build .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release`
  - resultado: sucesso, 0 warnings, 0 erros.
- `NUGET_PACKAGES=%TEMP%\dfm-nuget-packages-registry dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`
  - resultado: sucesso, 0 warnings, 0 erros.
- `NUGET_PACKAGES=%TEMP%\dfm-nuget-packages-registry DOTNET_ROLL_FORWARD=Major dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~MappingRegistryTests"`
  - resultado: sucesso, 7 testes aprovados.
- `NUGET_PACKAGES=%TEMP%\dfm-nuget-packages-registry DOTNET_ROLL_FORWARD=Major dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~MappingCompositionTests|FullyQualifiedName~DapperIntegrationTests"`
  - resultado: sucesso, 15 testes aprovados.
- `NUGET_PACKAGES=%TEMP%\dfm-nuget-packages-registry DOTNET_ROLL_FORWARD=Major dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`
  - resultado: sucesso, 45 testes aprovados no core e 7 testes aprovados no Dommel.

Pack nao foi executado porque nao houve mudanca de empacotamento, metadados NuGet ou targets.

## Encerramento Da Etapa 1

Capacidades estabilizadas:

- parsing de expressoes por membro real da expression tree;
- composicao deterministica entre mappings explicitos, conventions e fallback do Dapper;
- baseline de integracao com materializacao real via Dapper;
- dono interno de mappings e cache estruturado com invalidacao definida.

Dividas transferidas:

- os campos publicos mutaveis permanecem por compatibilidade;
- Dommel ainda consome os dicionarios publicos diretamente;
- paralelismo da suite continua desabilitado;
- suporte a MemberPath, objetos aninhados e Value Objects permanece fora do escopo.
