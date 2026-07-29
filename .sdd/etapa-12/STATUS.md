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

## Em andamento

- Nenhuma implementacao em andamento.

## Proximos passos

1. Adicionar baseline e tooling de API/binary compatibility.
2. Endurecer metadata de pacote, README de pacote, repository metadata,
   SourceLink, symbols e package validation.
3. Documentar migration guide, support policy e provider certification.
4. Definir e validar release candidate antes de stable.
5. Fazer auditoria final de release blockers.

## Release blockers

- Critical: nao ha validacao formal de API/binary compatibility.
- Critical: `2.0.0` ja existe no NuGet.org para core e Dommel; a estrategia de
  versionamento do fork precisa mudar antes de publicar.
- High: core e Dommel geram `NU5125` e aviso de README ausente no pack.
- High: NuGet metadata ainda aponta para o repositorio upstream original.
- High: nao ha SourceLink, repository metadata, symbols ou deterministic CI
  policy.
- High: CI ainda nao valida matriz de provider/SDK nem smokes trimming/AOT.
- High: Dapper TypeHandler interoperability depende de internal shape por
  reflection.
- Medium: nao ha `global.json`, package lock ou Central Package Management.
- Medium: analyzer/generator release manifests precisam revisao para release.

## Compatibility decisions

- Manter `netstandard2.0` como TFM minimo dos pacotes publicos.
- Tratar `Dapper [2.1.79,3.0.0)` como faixa suportada atual, com `2.1.79`
  validado como minimo e latest stable no Prompt 12.2.
- Tratar `Dommel >= 3.5.3` como minimo atual do pacote Dommel.
- Separar provider support de provider certification.
- Exigir API/binary compatibility formal antes de stable.
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
- Supported by Dommel builder registration but not certified in CI: SQL Server,
  SQL CE, PostgreSQL, MySQL.

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

Ultimo prompt executado: 12.2
