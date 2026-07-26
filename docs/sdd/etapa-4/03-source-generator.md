# 03 - Source Generator

## Specification

Esta entrega avalia e implementa um Source Generator incremental para reduzir reflection no registro de mappings, sem substituir o runtime nem remover caminhos existentes.

Objetivos tratados:

- descobrir em compile-time classes de mapping declaradas na compilacao atual;
- gerar chamadas explicitas para `FluentMapConfiguration.AddMap<TMap>()`;
- evitar `Assembly.GetTypes`, `Assembly.GetExportedTypes` e `Activator.CreateInstance(Type)` no caminho gerado;
- preservar registro manual e assembly scanning;
- manter o core `Dapper.FluentMap` sem dependencia Roslyn;
- validar o caminho gerado com Dapper, naming policies, inheritance e constructor mapping;
- documentar limites reais de trimming e Native AOT sem declarar suporte nao validado.

Fora do objetivo:

- materializador gerado;
- leitura gerada de `DbDataReader`;
- SQL, CRUD, query wrappers ou ORM;
- nested object construction;
- converters;
- suporte automatico a todo o grafo de assemblies referenciados.

Experiencia desejada:

```csharp
using Dapper.FluentMap;

FluentMapper.Initialize(configuration =>
{
    configuration.AddGeneratedMappings();
});
```

Codigo gerado conceitual:

```csharp
configuration
    .AddMap<CustomerMap>()
    .AddMap<OrderMap>();
```

O generator e opcional. A biblioteca continua funcional sem ele.

## Discovery

Arquivos e contexto analisados:

- `AGENTS.md`
- `.agents/skills/run-tests/SKILL.md`
- `.agents/skills/msbuild-modernization/SKILL.md`
- `.agents/skills/msbuild-antipatterns/SKILL.md`
- `.agents/skills/dotnet-aot-compat/SKILL.md`
- `.agents/skills/msbuild-antipatterns/references/private-assets.md`
- `docs/sdd/etapa-1/README.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-2/README.md`
- `docs/sdd/etapa-2/decisions.md`
- `docs/sdd/etapa-3/README.md`
- `docs/sdd/etapa-3/decisions.md`
- `docs/sdd/etapa-3/01-mapping-registration.md`
- `docs/sdd/etapa-4/README.md`
- `docs/sdd/etapa-4/status.md`
- `docs/sdd/etapa-4/decisions.md`
- `docs/sdd/etapa-4/01-roslyn-analyzers.md`
- `docs/sdd/etapa-4/02-trimming-aot.md`
- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `src/Dapper.FluentMap/Mapping/EntityMap.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap.Analyzers/FluentMapConfigurationAnalyzer.cs`
- projetos e testes existentes.

Entregas anteriores da Etapa 4:

- Entrega 01 - Roslyn Analyzers: `Concluido`, commit local `9075f61`.
- Entrega 02 - Trimming e Native AOT: `Concluido`, commit local `d559d65`.

Respostas de discovery:

1. Como identificar um mapping em compile-time:
   - um `class` symbol declarado na compilacao atual que implemente uma interface fechada `Dapper.FluentMap.Mapping.IEntityMap<TEntity>`.
2. Tipo base/interface que representa mapping:
   - `IEntityMap<TEntity>` e a interface nao generica `IEntityMap`; `EntityMap<TEntity>` e `EntityMapBase<TEntity, TPropertyMap>` sao bases comuns, mas a decisao usa a interface fechada para nao depender da hierarquia concreta.
3. Classes de mapping:
   - abstratas: nao registraveis pelo caminho gerado; reportadas por `DFM006` e ignoradas;
   - genericas abertas: nao registraveis pelo caminho gerado; reportadas por `DFM006` e ignoradas;
   - nested: suportadas quando a classe e todos os containing types sao `public` ou `internal`;
   - internal: suportadas quando possuem construtor publico sem parametros;
   - private/protected/file-local: nao acessiveis pelo codigo gerado top-level; reportadas por `DFM006` e ignoradas;
   - sem construtor publico sem parametros: reportadas por `DFM006` e ignoradas.
4. Como `AddMap<TMap>()` funciona:
   - `TMap` deve implementar `IEntityMap`, possuir `new()`, e implementar exatamente uma interface fechada `IEntityMap<TEntity>`;
   - a entidade e inferida pelo runtime a partir das interfaces;
   - a instancia e criada por `new TMap()` e registrada pelo mesmo `MappingRegistry`.
5. Constraints:
   - `where TMap : IEntityMap, new()`;
   - entidade alvo deve ser `class`;
   - runtime ainda valida duplicidade, colunas, `IncludeBase<TBase>()`, composition e instalacao do type map do Dapper.
6. Duplicidades:
   - o generator detecta duas classes geraveis para a mesma entidade na compilacao atual e reporta `DFM007`;
   - o runtime continua autoridade para duplicidade causada por registro manual, scanning, ordem dinamica ou assemblies externos.
7. Ativacao:
   - instalacao/referencia do pacote/projeto `Dapper.FluentMap.Generators` como analyzer/source generator.
8. Assembly do codigo gerado:
   - o codigo e gerado dentro do assembly do consumidor onde o generator esta executando.
9. Assemblies referenciados:
   - nao sao descobertos nesta entrega.
10. Como evitar geracao duplicada:
   - partial declarations sao agrupadas por nome simbolico do map;
   - a saida possui hint name unico `DapperFluentMapGeneratedRegistration.g.cs`;
   - mapas sao ordenados deterministicamente por profundidade de heranca da entidade, nome da entidade e nome do map.

## Decision

Foi criado o projeto separado:

```text
src/Dapper.FluentMap.Generators/
```

Motivos:

- manter o core sem Roslyn;
- permitir opt-in separado do pacote runtime;
- empacotar o assembly em `analyzers/dotnet/cs`;
- evitar dependencia circular com `Dapper.FluentMap.Analyzers`.

Nao foi criado projeto comum entre analyzer e generator. A duplicacao atual e pequena: identificacao de `IEntityMap<TEntity>` e descriptor `DFM005`. Um projeto comum so seria justificado quando houver compartilhamento maior e estavel.

API gerada:

```csharp
namespace Dapper.FluentMap
{
    internal static class DapperFluentMapGeneratedRegistration
    {
        public static FluentMapConfiguration AddGeneratedMappings(
            this FluentMapConfiguration configuration);
    }
}
```

Caracteristicas:

- namespace `Dapper.FluentMap`, pois consumidores normalmente ja importam esse namespace para `FluentMapper`;
- classe `internal`, reduzindo superficie publica do assembly consumidor;
- metodo extension acessivel dentro do assembly consumidor;
- null check em `configuration`;
- retorno da propria configuracao para composicao fluente;
- chamadas fully-qualified a `AddMap<TMap>()`;
- nenhum `using` fragil;
- nenhum reflection, scanning ou estado capturado no codigo gerado.

Escopo de descoberta:

- somente maps declarados na compilacao atual;
- nenhum traversal automatico de references.

Motivos para nao atravessar references:

- custo;
- determinismo;
- duplicidade entre assemblies;
- regras de visibilidade;
- risco de surpresa para o consumidor;
- possivel ambiguidade de multiplos assemblies gerando o mesmo extension method.

Diagnostics:

| ID | Severidade | Situacao |
|---|---|---|
| DFM005 | Error | tipo candidato implementa zero/multiplas interfaces fechadas `IEntityMap<TEntity>` ou entidade alvo nao e class |
| DFM006 | Info | mapping candidato nao entra na geracao por ser abstrato, generico aberto, inacessivel ou sem construtor publico sem parametros |
| DFM007 | Error | mais de um mapping geravel para a mesma entidade na compilacao atual |

`DFM005` foi reutilizado porque a regra semantica e a mesma do analyzer de `AddMap<TMap>()`: o tipo nao satisfaz o contrato de registro generico.

Comparacao de estrategias:

| Estrategia | Reflection | Trimming | AOT | Manutencao manual |
|---|---|---|---|---|
| Registro manual | Nao usa scanning; `AddMap<TMap>()` ainda infere `IEntityMap<TEntity>` por metadata anotada | Smoke explicit trimmed publica e executa; 0 warnings FluentMap-owned | Publish bloqueado no ambiente por linker C++ ausente; runtime nao validado | Alta: cada map precisa ser listado |
| Registro gerado | Codigo gerado nao usa reflection; chama `AddMap<TMap>()` | Smoke generated trimmed publica e executa; 0 warnings FluentMap-owned | Mesmo bloqueio de linker do ambiente; runtime nao validado | Baixa dentro do assembly atual |
| Assembly scanning | Usa `Assembly.GetExportedTypes`/`GetTypes` e `Activator.CreateInstance(Type)` | Smoke scanning trimmed emite `IL2026` FluentMap-owned e falha em runtime | Nao validado; tratado como reflection-dependent | Baixa |

## Delivery

Arquivos adicionados:

- `src/Dapper.FluentMap.Generators/Dapper.FluentMap.Generators.csproj`
- `src/Dapper.FluentMap.Generators/MappingRegistrationGenerator.cs`
- `src/Dapper.FluentMap.Generators/README.md`
- `src/Dapper.FluentMap.Generators/AnalyzerReleases.Shipped.md`
- `src/Dapper.FluentMap.Generators/AnalyzerReleases.Unshipped.md`
- `test/Dapper.FluentMap.Generators.Tests/Dapper.FluentMap.Generators.Tests.csproj`
- `test/Dapper.FluentMap.Generators.Tests/MappingRegistrationGeneratorTests.cs`
- `test/Dapper.FluentMap.GeneratedRegistration.Tests/Dapper.FluentMap.GeneratedRegistration.Tests.csproj`
- `test/Dapper.FluentMap.GeneratedRegistration.Tests/AssemblyInfo.cs`
- `test/Dapper.FluentMap.GeneratedRegistration.Tests/GeneratedRegistrationIntegrationTests.cs`
- `docs/sdd/etapa-4/03-source-generator.md`

Arquivos alterados:

- `Dapper.FluentMap.sln`
- `README.md`
- `src/Dapper.FluentMap/Properties/AssemblyInfo.cs`
- `test/Dapper.FluentMap.AotSmoke/Dapper.FluentMap.AotSmoke.csproj`
- `test/Dapper.FluentMap.AotSmoke/Program.cs`
- `docs/sdd/etapa-4/README.md`
- `docs/sdd/etapa-4/status.md`
- `docs/sdd/etapa-4/decisions.md`

Implementacao:

- generator incremental por `IIncrementalGenerator`;
- `SyntaxProvider` filtra `class` com base list e valida por semantic model;
- descoberta usa symbols, nao executa codigo do consumidor;
- maps abstratos, genericos abertos, inacessiveis ou sem construtor publico sem parametros sao ignorados com diagnostic informativo;
- duplicidade de entidade no conjunto geravel falha com diagnostic `DFM007`;
- partial declarations sao deduplicadas;
- saida deterministica por ordenacao estavel;
- codigo gerado contem header `// <auto-generated/>` e `GeneratedCodeAttribute`;
- codigo gerado usa nomes fully-qualified;
- codigo gerado nao usa reflection.

Testes unitarios do generator cobrem:

- zero mappings;
- um mapping;
- varios mappings;
- mapping internal suportado;
- mapping abstrato;
- mapping generico aberto;
- duplicidade;
- namespaces distintos;
- mesmo nome de classe em namespaces diferentes;
- saida deterministica;
- recompilacao incremental em execucoes repetidas do driver;
- codigo gerado compila.

Teste de integracao cobre:

- `AddGeneratedMappings()` executado em projeto real com generator como analyzer;
- materializacao real via Dapper e SQLite in-memory;
- mapping internal;
- inheritance por `IncludeBase<TBase>()`;
- constructor mapping;
- naming policy `SnakeCase` coexistindo com registro gerado.

Smoke AOT:

- o projeto `Dapper.FluentMap.AotSmoke` recebeu caminho `AOT_SMOKE_GENERATED`;
- o generator e referenciado como analyzer somente quando `DefineConstants=AOT_SMOKE_GENERATED`.

Packaging:

- `Dapper.FluentMap.Generators` empacota apenas `analyzers/dotnet/cs/Dapper.FluentMap.Generators.dll` e `README.md`;
- `SuppressDependenciesWhenPacking=true`;
- pacotes Roslyn usam `PrivateAssets="all"`;
- core nao referencia Roslyn;
- nenhum pacote Roslyn vira dependencia runtime do core.

## Validation

Ambiente:

- SDK: `10.0.302`
- test runner detectado: VSTest com xUnit v3
- core: `netstandard2.0`
- testes: `net10.0`
- projeto generator: `netstandard2.0`

Validacao localizada executada:

```text
dotnet build .\src\Dapper.FluentMap.Generators\Dapper.FluentMap.Generators.csproj
dotnet build .\test\Dapper.FluentMap.Generators.Tests\Dapper.FluentMap.Generators.Tests.csproj
dotnet test .\test\Dapper.FluentMap.Generators.Tests\Dapper.FluentMap.Generators.Tests.csproj --no-build
dotnet test .\test\Dapper.FluentMap.GeneratedRegistration.Tests\Dapper.FluentMap.GeneratedRegistration.Tests.csproj
dotnet test .\test\Dapper.FluentMap.Analyzers.Tests\Dapper.FluentMap.Analyzers.Tests.csproj --no-build
```

Resultados locais ja observados:

- build do generator: sucesso, 0 warnings, 0 erros;
- build dos testes do generator: sucesso, 0 warnings, 0 erros;
- testes do generator: sucesso, 12 testes aprovados;
- testes de integracao do registro gerado: sucesso, 1 teste aprovado;
- testes do analyzer: sucesso, 7 testes aprovados.

Validacao final completa:

```text
dotnet restore
dotnet build
dotnet test
dotnet build --configuration Release
dotnet test --configuration Release
dotnet test --configuration Release --no-build
```

Resultado:

- `dotnet restore`: sucesso;
- `dotnet build`: sucesso, 0 warnings, 0 erros;
- `dotnet test`: sucesso, 128 testes do core, 7 Dommel, 7 analyzer, 12 generator e 1 generated-registration integration;
- `dotnet build --configuration Release`: sucesso, 0 warnings, 0 erros;
- `dotnet test --configuration Release`: sucesso com os mesmos 155 testes;
- `dotnet test --configuration Release --no-build`: sucesso com os mesmos 155 testes.

Smokes normais:

```text
dotnet run --project .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release
dotnet run --project .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:DefineConstants=AOT_SMOKE_SCANNING
dotnet run --project .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:DefineConstants=AOT_SMOKE_GENERATED
```

Resultado:

- `explicit:ok`;
- `scanning:ok`;
- `generated:ok`.

Publish trimmed:

```text
dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishTrimmed=true -p:TrimMode=full -p:PublishAot=false -p:TrimmerSingleWarn=false -p:ILLinkTreatWarningsAsErrors=false
.\test\Dapper.FluentMap.AotSmoke\bin\Release\net10.0\win-x64\publish\Dapper.FluentMap.AotSmoke.exe

dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishTrimmed=true -p:TrimMode=full -p:PublishAot=false -p:DefineConstants=AOT_SMOKE_GENERATED -p:TrimmerSingleWarn=false -p:ILLinkTreatWarningsAsErrors=false
.\test\Dapper.FluentMap.AotSmoke\bin\Release\net10.0\win-x64\publish\Dapper.FluentMap.AotSmoke.exe

dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishTrimmed=true -p:TrimMode=full -p:PublishAot=false -p:DefineConstants=AOT_SMOKE_SCANNING -p:TrimmerSingleWarn=false -p:ILLinkTreatWarningsAsErrors=false
.\test\Dapper.FluentMap.AotSmoke\bin\Release\net10.0\win-x64\publish\Dapper.FluentMap.AotSmoke.exe
```

Resultado:

- explicit trimmed: publish concluido, runtime `explicit:ok`, 0 warnings FluentMap-owned;
- generated trimmed: publish concluido, runtime `generated:ok`, 0 warnings FluentMap-owned;
- scanning trimmed: publish concluido com `IL2026` FluentMap-owned esperado em `AddMapsFromAssemblyContaining<TMarker>()`; runtime falhou com `Column 'customer_id' was not mapped to property 'Id'.`;
- warnings restantes nos caminhos explicit/generated pertencem ao Dapper (`DefaultTypeMap`, `CustomPropertyTypeMap`, `DapperRow` e helpers internos).

Native AOT:

```text
dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishAot=true -p:TrimmerSingleWarn=false -p:ILLinkTreatWarningsAsErrors=false -p:MSBuildWarningsAsMessages=
dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishAot=true -p:DefineConstants=AOT_SMOKE_GENERATED -p:TrimmerSingleWarn=false -p:ILLinkTreatWarningsAsErrors=false -p:MSBuildWarningsAsMessages=
```

Resultado:

- explicit AOT: falhou com `Platform linker not found`;
- generated AOT: falhou com `Platform linker not found`;
- runtime Native AOT nao foi validado neste ambiente.

Pack e inspecao:

```text
dotnet pack .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-build --output .\artifacts\packages
dotnet pack .\src\Dapper.FluentMap.Generators\Dapper.FluentMap.Generators.csproj --configuration Release --no-build --output .\artifacts\packages
dotnet pack .\src\Dapper.FluentMap.Analyzers\Dapper.FluentMap.Analyzers.csproj --configuration Release --no-build --output .\artifacts\packages
dotnet list .\src\Dapper.FluentMap\Dapper.FluentMap.csproj package --include-transitive
dotnet list .\src\Dapper.FluentMap.Generators\Dapper.FluentMap.Generators.csproj package --include-transitive
```

Resultado:

- `Dapper.FluentMap.2.0.0.nupkg` criado; warning existente `NU5125` sobre `PackageLicenseUrl`;
- `Dapper.FluentMap.Generators.2.0.0.nupkg` criado;
- `Dapper.FluentMap.Analyzers.2.0.0.nupkg` criado;
- pacote generator contem `README.md` e `analyzers/dotnet/cs/Dapper.FluentMap.Generators.dll`, sem `lib/`;
- nuspec do generator nao contem grupo de dependencias;
- pacote analyzer continua contendo apenas `README.md` e `analyzers/dotnet/cs/Dapper.FluentMap.Analyzers.dll`, sem `lib/`;
- pacote core contem apenas `lib/netstandard2.0/Dapper.FluentMap.dll` e XML docs;
- core continua com `Dapper` como unica dependencia direta e nenhuma dependencia Roslyn.

Limitacoes restantes:

- mappings em assemblies referenciados nao sao descobertos automaticamente;
- o extension method gerado e `internal`, portanto o assembly que declara os maps deve chamar seu proprio `AddGeneratedMappings()`;
- abstract maps e open generic maps sao ignorados em vez de registrados;
- ordem por inheritance depth cobre o caso esperado de base maps antes de derived maps, mas o runtime continua autoridade para `IncludeBase<TBase>()` dinamico;
- Native AOT runtime continua bloqueado neste ambiente pela ausencia do platform linker C++.

Dividas explicitamente fora desta etapa:

- Nested object materialization;
- Value Objects complexos;
- Multiple mapping profiles;
- Query-specific mappings;
- Custom materializer;
- Generated DbDataReader materializer.
