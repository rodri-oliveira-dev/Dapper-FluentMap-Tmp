# Release Readiness Audit

Auditoria executada em 2026-07-29 no checkout local
`feature/etapa-3`, sem usar memoria de chats anteriores como fonte de verdade.

## Repository State

- Branch: `feature/etapa-3`, a frente de `origin/feature/etapa-3` por 14 commits.
- Worktree antes das alteracoes da Etapa 12: item nao rastreado preexistente
  `src/Dapper.FluentMap/etapas/`.
- `.sdd/etapa-12/` nao existia e foi criada neste prompt.
- SDK local usado na auditoria: `10.0.302`.
- Nao ha `global.json`, `Directory.Build.props`, `Directory.Packages.props` ou
  `.editorconfig` na raiz.
- `NuGet.Config` usa apenas `https://api.nuget.org/v3/index.json`.

## Projects

Projetos na solution:

| Projeto | Tipo | TFM | Packable | Observacao |
| --- | --- | --- | --- | --- |
| `src/Dapper.FluentMap` | Core library | `netstandard2.0` | Sim | API principal e integracao Dapper. |
| `src/Dapper.FluentMap.Dommel` | Dommel integration | `netstandard2.0` | Sim | Bridge global com Dommel. |
| `src/Dapper.FluentMap.DependencyInjection` | DI integration | `netstandard2.0` | Sim | Pacote opcional para `IServiceCollection`. |
| `src/Dapper.FluentMap.Analyzers` | Roslyn analyzer | `netstandard2.0` | Sim | Empacotado em `analyzers/dotnet/cs`. |
| `src/Dapper.FluentMap.Generators` | Source generator | `netstandard2.0` | Sim | Empacotado em `analyzers/dotnet/cs`. |
| `test/Dapper.FluentMap.Tests` | Tests | `net10.0` | Nao | Core unit/integration/regression. |
| `test/Dapper.FluentMap.Dommel.Tests` | Tests | `net10.0` | Nao | Dommel integration/regression. |
| `test/Dapper.FluentMap.DependencyInjection.Tests` | Tests | `net10.0` | Nao | DI integration. |
| `test/Dapper.FluentMap.Analyzers.Tests` | Tests | `net10.0` | Nao | Analyzer tests. |
| `test/Dapper.FluentMap.Generators.Tests` | Tests | `net10.0` | Nao | Generator tests. |
| `test/Dapper.FluentMap.GeneratedRegistration.Tests` | Tests | `net10.0` | Nao | Generated registration integration. |
| `test/Dapper.FluentMap.AotSmoke` | Smoke app | `net10.0` | Nao | Trim/AOT smoke harness. |
| `benchmarks/Dapper.FluentMap.Benchmarks` | Benchmark app | `net10.0` | Nao | BenchmarkDotNet harness. |

## Packages

`dotnet pack ./Dapper.FluentMap.sln --configuration Release --no-build --output ./artifacts/packages`
produz atualmente:

- `Dapper.FluentMap.2.0.0.nupkg`
- `Dapper.FluentMap.Dommel.2.0.0.nupkg`
- `Dapper.FluentMap.DependencyInjection.2.0.0.nupkg`
- `Dapper.FluentMap.Analyzers.2.0.0.nupkg`
- `Dapper.FluentMap.Generators.2.0.0.nupkg`

Conteudo observado:

- Core: `lib/netstandard2.0/Dapper.FluentMap.dll` e XML documentation.
- Dommel: `lib/netstandard2.0/Dapper.FluentMap.Dommel.dll` e XML documentation.
- DI: `README.md`, `lib/netstandard2.0/Dapper.FluentMap.DependencyInjection.dll`
  e XML documentation.
- Analyzers: `README.md`, `analyzers/dotnet/cs/Dapper.FluentMap.Analyzers.dll`.
- Generators: `README.md`, `analyzers/dotnet/cs/Dapper.FluentMap.Generators.dll`.

## Target Frameworks

TFMs encontrados:

- Public libraries and analyzer/generator packages: `netstandard2.0`.
- Tests, AOT smoke and benchmarks: `net10.0`.

Nao ha multi-targeting real apesar de alguns projetos usarem
`TargetFrameworks` com somente `netstandard2.0`.

## Dependency Matrix

Dependencias diretas principais:

| Projeto | Dependencias diretas |
| --- | --- |
| Core | `Dapper` `2.1.79`, `Microsoft.Bcl.AsyncInterfaces` `10.0.8` |
| Dommel | Core project reference, `Dapper` `2.1.79`, `Dommel` `3.5.3` |
| DI | Core project reference, `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.10` |
| Analyzers | `Microsoft.CodeAnalysis.CSharp` `5.6.0`, `Microsoft.CodeAnalysis.Analyzers` `5.6.0`, both `PrivateAssets=all` |
| Generators | `Microsoft.CodeAnalysis.CSharp` `5.6.0`, `Microsoft.CodeAnalysis.Analyzers` `5.6.0`, both `PrivateAssets=all` |
| Tests | `Microsoft.NET.Test.Sdk` `18.8.1`, `xunit.v3` `3.2.2`, `xunit.runner.visualstudio` `3.1.5`, SQLite packages |
| Benchmarks | `BenchmarkDotNet` `0.15.8`, SQLite packages |

Dapper:

- Versao minima atualmente permitida pelo pacote: `2.1.79`, porque a referencia
  direta sem upper bound empacota dependencia NuGet minima `>= 2.1.79`.
- Versao usada nos testes: `2.1.79`.
- APIs publicas diretamente usadas:
  - `SqlMapper.SetTypeMap`
  - `SqlMapper.GetTypeMap`
  - `SqlMapper.ExecuteReader`
  - `SqlMapper.ExecuteReaderAsync`
  - `SqlMapper.HasTypeHandler`
  - `SqlMapper.ITypeMap`
  - `SqlMapper.IMemberMap`
  - `SqlMapper.TypeHandler<T>` nos testes
  - `DefaultTypeMap`
  - `CommandDefinition`
- API sensivel: `SqlMapper.TypeHandlerCache<T>.Parse(object)` e resolvida por
  reflection interna em `DapperTypeHandlerAdapter`. Este e o maior risco de
  compatibilidade com novas versoes do Dapper.

Dommel:

- Versao usada e minima atual: `3.5.3`.
- Pontos de integracao:
  - `DommelMapper.SetColumnNameResolver`
  - `DommelMapper.SetKeyPropertyResolver`
  - `DommelMapper.SetTableNameResolver`
  - `DommelMapper.SetPropertyResolver`
  - `DommelMapper.AddSqlBuilder`
  - `IColumnNameResolver`, `IKeyPropertyResolver`, `ITableNameResolver`,
    `IPropertyResolver`, `ISqlBuilder`
  - SQL builders padrao para SQL Server, SQL CE, SQLite, PostgreSQL e MySQL.
- Estado efetivo: bridge process-wide; runtimes isolados do core nao isolam
  Dommel.

Historico NuGet consultado via flat-container em 2026-07-29:

- `Dapper.FluentMap` ja publicou ate `2.0.0` no pacote original.
- `Dapper.FluentMap.Dommel` ja publicou ate `2.0.0` no pacote original.
- `Dapper.FluentMap.DependencyInjection`, `Dapper.FluentMap.Analyzers` e
  `Dapper.FluentMap.Generators` nao existem no NuGet.org nesse nome.

## Test Coverage Categories

| Categoria | Evidencia atual |
| --- | --- |
| Unit | Tests de mapping metadata, naming policies, member paths, validation, diagnostics, immutable configuration. |
| Integration | Traits `Category=Integration` em core, DI, generated registration, Dommel e SQLite real. |
| Generator | `Dapper.FluentMap.Generators.Tests` e `Dapper.FluentMap.GeneratedRegistration.Tests`. |
| Analyzer | `Dapper.FluentMap.Analyzers.Tests`; release manifest de regras existe, mas precisa limpeza. |
| Historical regression | `test/*/HistoricalRegression/*` cobre issues historicas das etapas 8 e 9. |
| AOT | Smoke project existe; Native AOT publish foi historicamente bloqueado no ambiente por ausencia de linker. |
| Trimming | Smokes trimmed registrados nas etapas 7, 10 e 11; warnings conhecidos `IL2104`/`IL2026` documentados. |
| Package | `dotnet pack` executado e conteudo de `.nupkg` inspecionado superficialmente. |
| Provider | SQLite em memoria e `DataTableReader`; SQL Server/PostgreSQL/MySQL/SQL CE tem builders Dommel configurados, mas nao certificados em CI. |
| Performance | BenchmarkDotNet harness existe; resultados anteriores sao smoke/guardrail, nao claims publicos. |

Suite baseline neste prompt:

- `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build`
- Resultado: 453 testes aprovados, 0 falhas, 0 ignorados.

Observacao: alguns assemblies desabilitam paralelismo devido a estado global de
`FluentMapper`, Dapper e Dommel.

## CI

Workflows atuais:

- `.github/workflows/ci.yml`
  - triggers: push em `master` e `chore/net10-migration`, pull request.
  - runner: `ubuntu-latest`.
  - SDK: `10.0.x`, quality `ga`.
  - passos: checkout, setup .NET, `dotnet --info`, restore, build Release,
    test Release, pack Release, upload de `.nupkg`.

Outros arquivos legados:

- `.appveyor.yml`
- `.travis.yml`

Lacunas de CI:

- O workflow usa action versions `actions/checkout@v7`, `actions/setup-dotnet@v6`
  e `actions/upload-artifact@v7`; estas versoes devem ser confirmadas antes de
  tratar CI como operacional.
- Nao ha matriz de OS/SDK/provider.
- Nao ha etapa de API compatibility, package validation formal, SourceLink,
  determinism check, signature verification, SBOM ou provenance.
- Nao ha workflow de release com gates separados de publish.

## Packaging

Estado atual:

- Todos os pacotes usam `VersionPrefix=2.0.0`.
- Core e Dommel ainda usam `PackageLicenseUrl`, gerando `NU5125`.
- Core e Dommel nao incluem package README, e o pack emite aviso de best
  practices.
- DI, Analyzers e Generators usam `PackageLicenseExpression=MIT` e README.
- Project URLs ainda apontam para o repositorio original
  `https://github.com/henkmollema/Dapper-FluentMap`.
- Nao ha `RepositoryUrl`, `RepositoryType`, `PackageIcon`, symbol package,
  SourceLink configurado ou metadata explicita de commit.
- Nao ha `ContinuousIntegrationBuild`, `Deterministic`, `EmbedUntrackedSources`
  ou `PublishRepositoryUrl` configurados.

## Public API

Projetos que expoem API publica:

- `Dapper.FluentMap`
- `Dapper.FluentMap.Dommel`
- `Dapper.FluentMap.DependencyInjection`
- `Dapper.FluentMap.Analyzers`
- `Dapper.FluentMap.Generators`

Superficies publicas relevantes:

- Core static/global: `FluentMapper`, `FluentMapConfiguration`, conventions,
  mappings, type maps e helpers `QueryMapped*`.
- Core isolated runtime: `FluentMapConfigurationBuilder`,
  `ImmutableFluentMapConfiguration`, `FluentMapRuntime`.
- Mapping metadata: persistence, conversion, generated materializer contracts,
  diagnostics/explanations.
- Dommel: `DommelEntityMap`, `DommelPropertyMap`, `ForDommel()` e resolvers
  publicos.
- DI: `IServiceCollection.AddFluentMap(...)`.
- Analyzer/generator: tipos publicos `DiagnosticAnalyzer` e
  `IIncrementalGenerator`, diagnostic IDs `DFM001`-`DFM015`.

Lacuna: nao ha baseline formal de API publica nem API/binary compatibility
tooling configurado.

## Documentation

Documentacao existente:

- `README.md` cobre core, Dommel, analyzers, generators, DI, QueryMapped,
  QueryMultipleMapped, streaming, converters, limitations e trimming/AOT.
- README dos pacotes DI, analyzers e generators existe.
- SDD das etapas 7-11 contem final reports e decisoes.

Lacunas:

- Core e Dommel nao incluem README no pacote.
- Nao ha matriz publica consolidada de compatibilidade por package/provider/TFM.
- Nao ha migration guide final para uma release maior do fork.
- Nao ha release checklist publico ou support policy.
- Metadados NuGet ainda apontam para o repositorio original.

## AOT / Trimming

Estado atual:

- APIs de assembly scanning estao anotadas como sensiveis a trimming.
- `QueryMapped*`, `ReadMapped*`, `QueryMultipleMapped` e streaming permanecem
  anotados com `RequiresUnreferencedCode` e `RequiresDynamicCode`, porque podem
  cair no fallback runtime baseado em reflection/dynamic code.
- `Dapper.FluentMap.AotSmoke` cobre cenarios explicitos, gerados e DI.
- Smokes trimmed das etapas anteriores passaram com warnings conhecidos.
- Native AOT publish/run nao foi validado localmente por falta de toolchain
  nativa.
- Nenhum projeto publico declara `IsAotCompatible`.

Conclusao: a biblioteca possui caminhos preferenciais para trimmed apps, mas
nao esta release-ready para claim de Native AOT completo.

## Security / Supply Chain

Estado observado:

- `dotnet list ./Dapper.FluentMap.sln package --vulnerable --include-transitive`
  nao encontrou pacotes vulneraveis nas fontes atuais.
- `NuGet.Config` limpa feeds e usa apenas NuGet.org.
- Nao ha lock file de pacotes.
- Nao ha signing, SBOM, SLSA/provenance, checksum validation, dependency review
  workflow ou scan de segredos configurado.
- Nao ha SourceLink nem deterministic build/provenance configurados.

## Release Process

Estado atual:

- CI constroi, testa, empacota e publica artefatos, mas nao publica no NuGet.
- Nao ha processo documentado de release candidate, assinatura, validacao de
  pacote, changelog, migration guide, tags ou rollback.
- Nao ha criterio documentado para quando warnings conhecidos sao aceitaveis.

Release criteria propostos:

- Restore, build Release e tests da solution passam.
- Pacotes packable corretos sao gerados e inspecionados.
- Warnings de pack/build sao zero ou todos explicados e aceitos em `STATUS.md`.
- API publica e compatibilidade binaria sao comparadas contra baseline aprovado.
- NuGet metadata esta atualizada para o fork, license/readme validos.
- README e package READMEs refletem comportamento real.
- Matriz de compatibilidade e provider certification/limitation estao
  documentadas.
- Smokes trimming rodam em CI; Native AOT so e declarado quando houver publish
  e execucao em ambiente com toolchain nativa.
- Nenhum pacote vulneravel conhecido no lock atual.
- Release candidate e validado antes de release estavel.

## Identified Gaps

### Critical

- Nao ha validacao formal de API/binary compatibility. Para uma biblioteca
  publica com muitas APIs novas desde a linha historica, isso bloqueia release
  estavel.
- Versao `2.0.0` ja existe no NuGet.org para core e Dommel; publicar este fork
  como `2.0.0` estavel nesses package IDs seria tecnicamente e operacionalmente
  inseguro.

### High

- Metadata NuGet do core e Dommel esta incompleta/legada: `PackageLicenseUrl`
  obsoleto, sem package README, project URL apontando para upstream original.
- Nao ha SourceLink, repository metadata, deterministic CI policy ou symbol
  package para depurabilidade/reprodutibilidade.
- CI nao possui matriz minima de SDK/OS nem valida smokes trimming/AOT.
- Dapper compatibility depende de internals por reflection para
  `TypeHandlerCache<T>.Parse(object)`, sem matriz contra versoes futuras.
- Provider support nao esta separado de provider certification; SQLite e
  provider-independent sao validados, outros providers nao.

### Medium

- Nao ha `global.json`; builds locais podem variar com SDK instalado.
- Nao ha Central Package Management ou lock file; aceitavel para repo pequeno,
  mas piora auditabilidade de dependencias.
- Analyzer release manifests precisam revisao antes de publicar; ha regras
  unshipped que parecem pertencer ao release planejado.
- Nao ha package validation formal (`dotnet package validation`/ApiCompat),
  NuGet verify, assinatura ou SBOM.
- Native AOT permanece parcialmente validado e nao deve ser usado como claim de
  release.

### Low

- Alguns projetos usam `TargetFrameworks` com um unico TFM.
- `.appveyor.yml` e `.travis.yml` parecem legados e podem confundir leitores.
- Testes desabilitam paralelismo em alguns assemblies por estado global; isso e
  conhecido, mas deve permanecer documentado.
- Benchmarks existentes sao uteis como smoke, mas nao como promessa publica de
  throughput.
