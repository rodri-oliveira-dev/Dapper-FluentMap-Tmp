# 01 - Roslyn Analyzers

## Specification

Criar analyzers Roslyn para antecipar problemas de configuracao do `Dapper.FluentMap` em compile-time, sem duplicar toda a logica runtime.

Objetivos tratados nesta primeira versao:

- expression invalida passada para `Map(...)`;
- uso de membro nao suportado em `Map(...)`;
- mapping duplicado evidente para o mesmo `MemberPath`;
- conflito inequivoco de coluna por `ToColumn(...)` literal;
- `IncludeBase<TBase>()` com tipo que nao e base class real da entidade;
- `AddMap<TMap>()` com tipo que nao implementa exatamente uma interface fechada `IEntityMap<TEntity>` para entidade class.

Fora do objetivo:

- executar construtores de maps;
- instanciar mappings;
- simular assembly scanning;
- substituir `Validate()` ou as validacoes fail-fast;
- diagnosticar preferencias de estilo;
- criar Code Fix Provider;
- alterar comportamento runtime do core;
- alterar Dommel funcionalmente.

## Discovery

Arquivos e decisoes analisados:

- `AGENTS.md`
- `.agents/skills/msbuild-modernization/SKILL.md`
- `.agents/skills/msbuild-antipatterns/SKILL.md`
- `.agents/skills/run-tests/SKILL.md`
- `docs/sdd/etapa-1/README.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-2/README.md`
- `docs/sdd/etapa-2/decisions.md`
- `docs/sdd/etapa-2/01-member-path.md`
- `docs/sdd/etapa-2/02-configuration-validation.md`
- `docs/sdd/etapa-3/README.md`
- `docs/sdd/etapa-3/decisions.md`
- `docs/sdd/etapa-3/01-mapping-registration.md`
- `docs/sdd/etapa-3/02-constructor-immutable-mapping.md`
- `docs/sdd/etapa-3/03-diagnostics-api.md`
- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `src/Dapper.FluentMap/Configuration/FluentConventionConfiguration.cs`
- `src/Dapper.FluentMap/Mapping/EntityMap.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMap.cs`
- `src/Dapper.FluentMap/Mapping/MemberPath.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMapIdentity.cs`
- `src/Dapper.FluentMap/MappingConfigurationValidator.cs`
- `src/Dapper.FluentMap/FluentMapper.cs`
- projetos `.csproj`, `Dapper.FluentMap.sln` e `NuGet.Config`.

Catalogo de erros e classificacao:

| Condicao | Classificacao |
|---|---|
| `Map(...)` com lambda que nao resolve para property path | Pode ser detectado estaticamente quando a lambda e literal |
| `Map(...)` usando metodo, campo, indexer ou expressao composta | Pode ser detectado estaticamente quando a lambda e literal |
| Mesmo `MemberPath` mapeado duas vezes no mesmo construtor de map | Pode ser detectado parcialmente |
| Dois paths distintos com mesmo terminal, como `Rank.Level` e `Seniority.Level` | Configuracao valida; nao diagnosticar |
| Dois mappings explicitos para a mesma coluna literal no mesmo construtor | Pode ser detectado parcialmente |
| Conflito de coluna calculada dinamicamente | Somente runtime |
| Conflito entre explicit mapping e convention | Somente runtime/predecencia existente |
| Convention ambigua ou sem `Configure(...)` | Somente runtime, pois depende de predicates e transformers |
| `ToColumn(null)` ou `ToColumn("")` literal | Detectavel, mas nao implementado nesta primeira versao para manter conjunto pequeno |
| `IncludeBase<TBase>()` com interface, mesmo tipo ou tipo nao relacionado | Pode ser detectado estaticamente |
| `IncludeBase<TBase>()` com base map nao registrado | Somente runtime, pois depende da ordem real de registro |
| `AddMap<TMap>()` com map nao generico ou multiplas entidades | Pode ser detectado estaticamente |
| `AddMap<TMap>()` com map abstrato ou sem construtor publico sem parametros | Ja e coberto pelo compilador via constraint `new()` quando aplicavel |
| `AddMapsFromAssembly(...)` com tipos descobertos invalidos | Somente runtime/reflection |
| Constructor mapping impossivel por overloads/parametros opcionais | Somente Dapper/runtime |
| Nested object materialization | Fora do contrato; nao diagnosticar apenas por `MemberPath` aninhado |

Infraestrutura encontrada:

- nao havia projeto de analyzer;
- nao havia Central Package Management;
- versoes de pacotes sao declaradas em cada `.csproj`;
- testes usam `net10.0`, `Microsoft.NET.Test.Sdk` e `xunit.v3`;
- solution possui folders `src` e `test`;
- `NuGet.Config` usa `nuget.org`.

Pacotes escolhidos:

- `Microsoft.CodeAnalysis.CSharp` `5.6.0` para o analyzer e a harness de testes;
- `Microsoft.CodeAnalysis.Analyzers` `5.6.0` no projeto de analyzer, com `PrivateAssets="all"`;
- os pacotes de teste seguem as versoes ja usadas nos testes existentes.

Impacto futuro sobre Source Generator:

- a leitura estatica de lambdas e cadeias `Map(...).ToColumn(...)` pode ser reaproveitada;
- o source generator nao deve assumir que todo mapping valido esta disponivel estaticamente;
- chamadas auxiliares, configuracao dinamica e assembly scanning continuam exigindo fallback runtime.

## Decision

Diagnostics iniciais:

| ID | Severidade | Situacao | Detectavel estaticamente? |
|---|---|---|---|
| DFM001 | Error | `Map(...)` recebe lambda literal que nao resolve para property path de propriedades suportadas | Sim |
| DFM002 | Error | mesmo `MemberPath` aparece em duas chamadas diretas de `Map(...)` no mesmo construtor de `EntityMap` | Parcialmente, somente padrao direto |
| DFM003 | Error | dois `MemberPath`s distintos no mesmo construtor resolvem a mesma coluna literal por `ToColumn(...)` | Parcialmente, somente constantes |
| DFM004 | Error | `IncludeBase<TBase>()` usa tipo que nao e base class real da entidade do map | Sim |
| DFM005 | Error | `AddMap<TMap>()` usa tipo que nao implementa exatamente um `IEntityMap<TEntity>` fechado para entidade class | Sim |

Severidade:

- todos sao `Error` porque representam configuracoes que o runtime ja rejeita ou que o compilador consegue provar como invalidas;
- nenhum diagnostic de estilo foi criado.

Code fixes:

- nenhum Code Fix Provider foi entregue;
- corrigir expression, escolher coluna, escolher base type ou substituir map type pode alterar intencao de dominio.

## Delivery

Arquivos adicionados:

- `src/Dapper.FluentMap.Analyzers/Dapper.FluentMap.Analyzers.csproj`
- `src/Dapper.FluentMap.Analyzers/FluentMapConfigurationAnalyzer.cs`
- `src/Dapper.FluentMap.Analyzers/AnalyzerReleases.Shipped.md`
- `src/Dapper.FluentMap.Analyzers/AnalyzerReleases.Unshipped.md`
- `test/Dapper.FluentMap.Analyzers.Tests/Dapper.FluentMap.Analyzers.Tests.csproj`
- `test/Dapper.FluentMap.Analyzers.Tests/FluentMapConfigurationAnalyzerTests.cs`
- `docs/sdd/etapa-4/README.md`
- `docs/sdd/etapa-4/status.md`
- `docs/sdd/etapa-4/decisions.md`
- `docs/sdd/etapa-4/01-roslyn-analyzers.md`

Arquivos alterados:

- `Dapper.FluentMap.sln`

Estrutura:

```text
src/Dapper.FluentMap.Analyzers/
test/Dapper.FluentMap.Analyzers.Tests/
```

Implementacao:

- analyzer baseado em `DiagnosticAnalyzer(LanguageNames.CSharp)`;
- usa `SyntaxNodeAction` para invocacoes e `CompilationEndAction` apenas para agregacoes locais de duplicidade/conflito;
- compara symbols para identificar APIs do FluentMap;
- interpreta lambdas literais de `Map(...)` sem executar codigo;
- aceita caminhos simples, paths aninhados e casts explicitos em torno da expressao;
- considera duplicidade/conflito apenas em statements diretos do construtor, evitando fluxo arbitrario;
- conflito de coluna exige coluna conhecida estaticamente e respeita `caseSensitive` literal;
- chamadas com coluna dinamica, bool dinamico ou `Ignore()` ficam sem diagnostic de coluna;
- `AddMap<TMap>()` valida o contrato de exatamente um `IEntityMap<TEntity>` fechado e entidade class.

Packaging:

- o projeto analyzer e `netstandard2.0`;
- `IncludeBuildOutput=false`;
- o assembly do analyzer e empacotado em `analyzers/dotnet/cs`;
- `SuppressDependenciesWhenPacking=true`, evitando dependencia runtime para Roslyn no `.nuspec`;
- `PackageLicenseExpression=MIT`;
- readme minimo incluido no pacote;
- o core nao referencia o analyzer;
- o core nao recebeu dependencias Roslyn.

## Validation

Validacao localizada executada durante a entrega:

- `dotnet restore .\Dapper.FluentMap.sln`
  - resultado: sucesso.
- `dotnet build .\test\Dapper.FluentMap.Analyzers.Tests\Dapper.FluentMap.Analyzers.Tests.csproj --no-restore`
  - resultado inicial: sucesso com warnings RS1036, RS1037, RS2008 e xUnit2031.
- Ajustes:
  - `EnforceExtendedAnalyzerRules=true`;
  - release tracking em `AnalyzerReleases.Shipped.md` e `AnalyzerReleases.Unshipped.md`;
  - `WellKnownDiagnosticTags.CompilationEnd` em `DFM002` e `DFM003`;
  - uso do overload de `Assert.Single` com predicate.
- `dotnet build .\test\Dapper.FluentMap.Analyzers.Tests\Dapper.FluentMap.Analyzers.Tests.csproj --no-restore`
  - resultado: sucesso, 0 warnings, 0 erros.
- `dotnet test .\test\Dapper.FluentMap.Analyzers.Tests\Dapper.FluentMap.Analyzers.Tests.csproj --no-build`
  - resultado: sucesso, 7 testes aprovados.

Testes do analyzer cobrem:

- positivo, mensagem, severidade e localizacao para `DFM001`;
- positivo, mensagem, severidade e localizacao para `DFM002`;
- positivo, mensagem, severidade e localizacao para `DFM003`;
- positivo, mensagem, severidade e localizacao para `DFM004`;
- positivo, mensagem, severidade e localizacao para `DFM005`;
- mapping valido sem diagnostics;
- expression valida;
- `MemberPath` aninhado valido;
- inheritance valido;
- registration valido;
- record/constructor mapping valido;
- ausencia de falso positivo em colunas com casing diferente quando ambas sao case-sensitive.

Validacao final completa:

- `dotnet restore`
  - resultado: sucesso.
- `dotnet build`
  - resultado: sucesso, 0 warnings, 0 erros.
- `dotnet test`
  - resultado: sucesso, 128 testes aprovados no core, 7 no Dommel e 7 no analyzer.
- `dotnet build --configuration Release`
  - resultado: sucesso, 0 warnings, 0 erros.
- `dotnet test --configuration Release`
  - resultado: sucesso, 128 testes aprovados no core, 7 no Dommel e 7 no analyzer.
- `dotnet test .\test\Dapper.FluentMap.Analyzers.Tests\Dapper.FluentMap.Analyzers.Tests.csproj --no-build`
  - resultado: sucesso, 7 testes aprovados.
- `dotnet pack .\src\Dapper.FluentMap.Analyzers\Dapper.FluentMap.Analyzers.csproj --configuration Release --no-build --output .\artifacts\packages`
  - resultado: sucesso, pacote `Dapper.FluentMap.Analyzers.2.0.0.nupkg` criado sem warnings.
- inspecao do `.nupkg`
  - resultado: contem `README.md` e `analyzers/dotnet/cs/Dapper.FluentMap.Analyzers.dll`; nao contem `lib/`.
- `dotnet list .\src\Dapper.FluentMap\Dapper.FluentMap.csproj package --include-transitive`
  - resultado: o projeto principal continua com `Dapper` como unica dependencia direta; nenhuma dependencia Roslyn foi adicionada ao core.

Confirmacoes:

- core nao referencia Roslyn;
- pacote principal nao recebe dependencias Roslyn;
- analyzer nao muda runtime;
- diagnostics aparecem apenas nos cenarios cobertos;
- suite anterior continua verde.

## Limitacoes

- Nao analisa chamadas `Map(...)` indiretas por helper method.
- Nao analisa duplicidade em fluxos condicionais, loops ou chamadas fora de statements diretos do construtor.
- Nao executa constructor mapping nem simula o Dapper.
- Nao diagnostica `AddMapsFromAssembly(...)`, pois discovery depende de reflection e ambiente runtime.
- Nao diagnostica base map ausente em `IncludeBase<TBase>()`, pois depende de registro real.
- Nao diagnostica transformers de naming policy ou convention.
- Nao diagnostica materializacao aninhada.
- Nao ha Code Fix Provider.
