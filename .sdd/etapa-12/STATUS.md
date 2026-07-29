# Etapa 12 Status

## Objetivo

Transformar o estado atual da biblioteca em uma entrega buildable, testable,
compatible, packable, documented, reproducible e release-ready, sem adicionar
features.

## Estado geral

Etapa 12 iniciada com auditoria documental e baseline de build/test/pack. A
solution esta buildable e testable no ambiente local, mas ainda nao esta
release-ready por lacunas de API compatibility, versionamento, NuGet metadata,
SourceLink/reproducibilidade e CI de release.

## Concluido

- Executado `git status` antes de alteracoes.
- Lido `README.md`.
- Examinada `Dapper.FluentMap.sln`.
- Examinados projetos core, Dommel, DependencyInjection, Analyzers,
  Generators, tests, AOT smoke e benchmarks.
- Examinados `NuGet.Config`, `.csproj`, workflow GitHub e ausencia de
  `Directory.Build.props`, `Directory.Packages.props`, `global.json` e
  `.editorconfig`.
- Lidos `.sdd/etapa-11/FINAL-REPORT.md` e `.sdd/etapa-11/STATUS.md`.
- Lidos `FINAL-REPORT.md` das etapas 7, 8, 9 e 10.
- Confirmado que `.sdd/etapa-12/` nao existia e criada a pasta.
- Criado `01-release-readiness-audit.md`.
- Criado `02-compatibility-spec.md`.
- Criado `DECISIONS.md`.
- Criado este `STATUS.md`.
- Executada validacao inicial obrigatoria de restore/build/test/pack.
- Executada auditoria de vulnerabilidades via NuGet.
- Consultado historico NuGet dos package IDs relevantes.
- Criado `03-compatibility-matrix.md`.
- Adicionada matriz essencial de CI por versao Dapper.
- Separada validacao de analyzer/generator da matriz runtime.
- Declarada propriedade MSBuild `DapperPackageVersion` para restaurar versoes
  especificas na matriz.
- Confirmado por `git ls-remote` que as tags atuais de GitHub Actions usadas no
  workflow existem (`checkout@v7`, `setup-dotnet@v6`, `upload-artifact@v7`).
- Criado `04-provider-matrix.md`.
- Criado projeto `test/Dapper.FluentMap.ProviderCompatibility.Tests`.
- Adicionados testes reais de provider para SQLite cobrindo leitura basica,
  materializacao avancada, `QueryMultipleMapped`, streaming sync/async e
  persistencia Dommel.
- Adicionado harness condicional para SQL Server via
  `DFM_SQLSERVER_CONNECTION_STRING`.
- Adicionado harness condicional para PostgreSQL via
  `DFM_POSTGRESQL_CONNECTION_STRING`.
- Adicionada etapa de provider compatibility ao job `compatibility` da CI.
- Criado `05-public-api-review.md`.
- Examinada API publica atual dos cinco pacotes.
- Comparada API atual de core/Dommel com pacotes historicos NuGet.org `2.0.0`.
- Identificada quebra historica Dommel:
  `DommelPropertyMap.GeneratedOption` mudou de `DatabaseGeneratedOption` para
  `DatabaseGeneratedOption?`.
- Habilitada package validation nativa do SDK para pacotes runtime com `lib/`.
- Adicionada metadata NuGet moderna comum: repository URL do fork, project URL,
  license expression, README, repository commit e SourceLink metadata.
- Removido `PackageLicenseUrl` legado de core/Dommel.
- Adicionados README aos pacotes core/Dommel.
- Gerados `.snupkg` para core, Dommel e DependencyInjection.
- Empacotados PDBs em analyzer/generator no layout `analyzers/dotnet/cs`.
- Confirmado que analyzer/generator nao expoem dependencias Roslyn transitivas
  no nuspec.
- Revisada decisao de strong naming: nao adicionar neste prompt.
- Revisada package signing: nao assinar neste prompt.

## Em andamento

- Nenhuma implementacao em andamento.

## Proximos passos

1. Definir baseline de API do proprio fork apos primeiro RC/versao aprovada.
2. Documentar migration guide, support policy e provider certification.
3. Validar SourceLink URL/checksum em CI apos push.
4. Definir e validar release candidate antes de stable.
5. Fazer auditoria final de release blockers.

## Release blockers

- Critical: `2.0.0` ja existe no NuGet.org para core e Dommel; a estrategia de
  versionamento do fork precisa mudar antes de publicar.
- Critical: baseline de API do proprio fork ainda precisa ser estabelecida apos
  o primeiro RC/versao aprovada.
- High: SourceLink URL/checksum precisa ser validado em CI apos push do commit.
- High: CI ainda nao valida SQL Server/PostgreSQL com servicos reais nem smokes
  trimming/AOT.
- High: Dapper TypeHandler interoperability depende de internal shape por
  reflection.
- Medium: nao ha `global.json`, package lock ou Central Package Management.
- Medium: analyzer/generator release manifests precisam revisao para release.
- Medium: packages nao estao assinados; `dotnet nuget verify` falha com
  `NU3004` por ausencia de assinatura.

## Compatibility decisions

- Manter `netstandard2.0` como TFM minimo dos pacotes publicos.
- Tratar `Dapper [2.1.79,3.0.0)` como faixa suportada atual, com `2.1.79`
  validado como minimo e latest stable no Prompt 12.2.
- Tratar `Dommel [3.5.3,4.0.0)` como faixa atual do pacote Dommel.
- Separar provider support de provider certification.
- Exigir API/binary compatibility formal antes de stable.
- Usar package validation nativa do SDK nos pacotes runtime.
- Estabelecer baseline obrigatoria contra a linha do proprio fork, nao contra
  `2.0.0` original, salvo auditoria historica explicita.
- Nao declarar Native AOT completo no estado atual.
- Usar RC antes de stable; recomendacao inicial `3.0.0-rc.1`, salvo prova
  formal que permita `2.1.0-rc.1`.

## Packages

- `Dapper.FluentMap`
- `Dapper.FluentMap.Dommel`
- `Dapper.FluentMap.DependencyInjection`
- `Dapper.FluentMap.Analyzers`
- `Dapper.FluentMap.Generators`

## Providers

- Certified in automated tests: SQLite.
- Provider-independent coverage: `DataTableReader`/ADO.NET interfaces.
- Conditional harness but not certified in CI: SQL Server, PostgreSQL.
- Supported by Dommel builder registration but not certified in CI:
  SQL Server, SQL CE, PostgreSQL, MySQL.
- MySQL/MariaDB: not validated; no mandatory lane added in Prompt 12.3.
- SQL CE: unsupported upstream/legacy validation lane.

## Known risks

- Estado global permanece para `FluentMapper`, `SqlMapper.SetTypeMap` e
  `DommelMapper`.
- `QueryMapped*` pode cair para fallback runtime sensivel a trimming/dynamic
  code.
- Native AOT nao foi validado por execucao em ambiente com linker nativo.
- Pacotes novos ainda nao possuem historico publico no NuGet.org.

## Arquivos importantes

- `.sdd/etapa-12/01-release-readiness-audit.md`
- `.sdd/etapa-12/02-compatibility-spec.md`
- `.sdd/etapa-12/03-compatibility-matrix.md`
- `.sdd/etapa-12/04-provider-matrix.md`
- `.sdd/etapa-12/05-public-api-review.md`
- `.sdd/etapa-12/DECISIONS.md`
- `.sdd/etapa-12/STATUS.md`
- `README.md`
- `Dapper.FluentMap.sln`
- `.github/workflows/ci.yml`
- `src/Dapper.FluentMap/Dapper.FluentMap.csproj`
- `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj`
- `src/Dapper.FluentMap.DependencyInjection/Dapper.FluentMap.DependencyInjection.csproj`
- `src/Dapper.FluentMap.Analyzers/Dapper.FluentMap.Analyzers.csproj`
- `src/Dapper.FluentMap.Generators/Dapper.FluentMap.Generators.csproj`

## Validacao do Prompt 12.1

Executado em 2026-07-29:

```bash
dotnet restore ./Dapper.FluentMap.sln
dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build
dotnet pack ./Dapper.FluentMap.sln --configuration Release --no-build --output ./artifacts/packages
dotnet list ./Dapper.FluentMap.sln package --vulnerable --include-transitive
```

Resultados:

- Restore: sucesso.
- Build Release: sucesso, 0 warnings, 0 errors.
- Solution tests: sucesso, 453 testes aprovados, 0 falhas, 0 ignorados.
- Pack solution: sucesso; criou os 5 pacotes packable.
- Pack warnings: `NU5125` em core e Dommel por `licenseUrl` obsoleto; aviso de
  README ausente em core e Dommel.
- Vulnerability audit: nenhum pacote vulneravel encontrado nas fontes atuais.

## Validacao do Prompt 12.2

Executada localmente em 2026-07-29:

```bash
dotnet restore ./Dapper.FluentMap.sln -p:DapperPackageVersion=2.1.79
dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore -p:DapperPackageVersion=2.1.79
dotnet test ./test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj --configuration Release --no-build
dotnet test ./test/Dapper.FluentMap.GeneratedRegistration.Tests/Dapper.FluentMap.GeneratedRegistration.Tests.csproj --configuration Release --no-build
dotnet test ./test/Dapper.FluentMap.DependencyInjection.Tests/Dapper.FluentMap.DependencyInjection.Tests.csproj --configuration Release --no-build
dotnet test ./test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj --configuration Release --no-build
dotnet test ./test/Dapper.FluentMap.Analyzers.Tests/Dapper.FluentMap.Analyzers.Tests.csproj --configuration Release
dotnet test ./test/Dapper.FluentMap.Generators.Tests/Dapper.FluentMap.Generators.Tests.csproj --configuration Release
dotnet restore ./Dapper.FluentMap.sln
dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build
dotnet pack ./Dapper.FluentMap.sln --configuration Release --no-build --output ./artifacts/packages
```

Resultados:

- Restore da matriz Dapper `2.1.79`: sucesso.
- Build Release da matriz Dapper `2.1.79`: sucesso, 0 warnings, 0 errors.
- Runtime compatibility tests:
  - Core: 370 aprovados.
  - Generated registration: 6 aprovados.
  - DependencyInjection: 9 aprovados.
  - Dommel: 23 aprovados.
- Analyzer tests: 19 aprovados.
- Generator tests: 26 aprovados.
- Restore/build/test padrao da solution com range default: sucesso; 453 testes
  aprovados.
- Pack solution: sucesso; warnings conhecidos `NU5125` em core/Dommel por
  `licenseUrl` obsoleto e aviso de README ausente nesses pacotes.
- Inspecao do nuspec:
  - `Dapper.FluentMap`: `Dapper` `[2.1.79, 3.0.0)`.
  - `Dapper.FluentMap.Dommel`: `Dapper` `[2.1.79, 3.0.0)`, `Dommel` `3.5.3`.

## Ultimo prompt executado

Ultimo prompt executado: 12.4

## Validacao do Prompt 12.3

Executada localmente em 2026-07-29:

```bash
dotnet test ./test/Dapper.FluentMap.ProviderCompatibility.Tests/Dapper.FluentMap.ProviderCompatibility.Tests.csproj --configuration Release
dotnet restore ./Dapper.FluentMap.sln
dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build
dotnet pack ./Dapper.FluentMap.sln --configuration Release --no-build --output ./artifacts/packages
dotnet list ./Dapper.FluentMap.sln package --vulnerable --include-transitive
```

Resultados:

- Build/test do projeto provider compatibility: sucesso.
- SQLite: 7 cenarios aprovados.
- SQL Server: 7 cenarios skipped por ausencia de
  `DFM_SQLSERVER_CONNECTION_STRING`.
- PostgreSQL: 7 cenarios skipped por ausencia de
  `DFM_POSTGRESQL_CONNECTION_STRING`.
- Restore solution: sucesso.
- Build Release solution: sucesso, 0 warnings, 0 errors.
- Test solution: sucesso; 460 aprovados, 14 ignored/skipped, 0 falhas.
- Pack solution: sucesso; pacotes packable gerados.
- Pack warnings conhecidos: `NU5125` e README ausente em core/Dommel.
- Vulnerability audit: nenhum pacote vulneravel encontrado nas fontes atuais,
  incluindo as novas dependencias de teste `Microsoft.Data.SqlClient` e
  `Npgsql`.

Status por provider:

| Provider | Status | Observacao |
| --- | --- | --- |
| SQLite | `Validated` | Testes reais automatizados passaram localmente. |
| SQL Server | `Not validated` | Harness condicional existe, mas nao foi executado contra servico real. |
| PostgreSQL | `Not validated` | Harness condicional existe, mas nao foi executado contra servico real. |
| MySQL/MariaDB | `Not validated` | Nao ha harness obrigatorio neste prompt. |
| SQL Server CE | `Unsupported upstream` | Builder legado permanece, sem lane moderna de validacao. |

## Validacao do Prompt 12.4

Executada localmente em 2026-07-29:

```bash
dotnet restore ./Dapper.FluentMap.sln
dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build
dotnet pack ./Dapper.FluentMap.sln --configuration Release --no-build --output ./artifacts/packages-12.4-final
sourcelink print-json <pdbs dos cinco pacotes>
dotnet nuget verify artifacts/packages-12.4-final/*.nupkg
```

Resultados:

- Restore: sucesso.
- Build Release: sucesso, 0 warnings, 0 errors.
- Primeiro pack apos `IncludeSymbols=true` global falhou para Analyzer e
  Generator com `NU5017`, porque o SDK tentou criar `.snupkg` vazio para
  pacotes sem `lib/`. Corrigido desabilitando `.snupkg` nesses pacotes Roslyn e
  empacotando PDBs em `analyzers/dotnet/cs`.
- Test solution: sucesso; 460 aprovados, 14 ignored/skipped, 0 falhas.
- Pack final em `artifacts/packages-12.4-final`: sucesso, 0 warnings, 0 errors.
- Package validation nativa executou durante pack para core, Dommel e
  DependencyInjection.
- Pacotes gerados:
  - `Dapper.FluentMap.2.0.0.nupkg` e `.snupkg`;
  - `Dapper.FluentMap.Dommel.2.0.0.nupkg` e `.snupkg`;
  - `Dapper.FluentMap.DependencyInjection.2.0.0.nupkg` e `.snupkg`;
  - `Dapper.FluentMap.Analyzers.2.0.0.nupkg`;
  - `Dapper.FluentMap.Generators.2.0.0.nupkg`.
- Conteudo dos pacotes inspecionado:
  - runtime packages contem `README.md`, assembly `lib/netstandard2.0` e XML
    docs;
  - analyzer/generator contem `README.md`, DLL e PDB em `analyzers/dotnet/cs`;
  - sem binarios de teste, `.sdd`, artifacts internos, temporarios ou secrets.
- Nuspecs contem license expression MIT, project URL/repository URL do fork,
  branch/commit e dependencias esperadas.
- SourceLink JSON encontrado nos PDBs dos cinco pacotes apontando para
  `raw.githubusercontent.com/rodri-oliveira-dev/Dapper-FluentMap/<commit>/*`.
  Download/checksum nao foi usado como gate local porque o commit ainda nao
  estava publicado no remoto.
- `dotnet nuget verify` nos pacotes finais confirmou hashes, mas falhou com
  `NU3004` porque os pacotes nao estao assinados. Isso permanece decisao
  documentada, nao falha de build/pack.
