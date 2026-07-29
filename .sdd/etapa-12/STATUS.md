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

## Em andamento

- Nenhuma implementacao em andamento. Este prompt foi limitado a auditoria,
  especificacao, decisoes e status.

## Proximos passos

1. Criar compatibility matrix formal por pacote, TFM, Dapper, Dommel e provider.
2. Adicionar baseline e tooling de API/binary compatibility.
3. Endurecer metadata de pacote, README de pacote, repository metadata,
   SourceLink, symbols e package validation.
4. Ajustar CI para matriz minima e validar pack/trimming/compatibility.
5. Documentar migration guide, support policy e provider certification.
6. Definir e validar release candidate antes de stable.
7. Fazer auditoria final de release blockers.

## Release blockers

- Critical: nao ha validacao formal de API/binary compatibility.
- Critical: `2.0.0` ja existe no NuGet.org para core e Dommel; a estrategia de
  versionamento do fork precisa mudar antes de publicar.
- High: core e Dommel geram `NU5125` e aviso de README ausente no pack.
- High: NuGet metadata ainda aponta para o repositorio upstream original.
- High: nao ha SourceLink, repository metadata, symbols ou deterministic CI
  policy.
- High: CI nao valida matriz de provider/Dapper/SDK nem smokes trimming/AOT.
- High: Dapper TypeHandler interoperability depende de internal shape por
  reflection.
- Medium: nao ha `global.json`, package lock ou Central Package Management.
- Medium: analyzer/generator release manifests precisam revisao para release.

## Compatibility decisions

- Manter `netstandard2.0` como TFM minimo dos pacotes publicos.
- Tratar `Dapper >= 2.1.79` como minimo atual e validar matriz antes de release.
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
- CI atual pode estar fragil por versoes de GitHub Actions que precisam
  confirmacao antes de release.

## Arquivos importantes

- `.sdd/etapa-12/01-release-readiness-audit.md`
- `.sdd/etapa-12/02-compatibility-spec.md`
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

## Ultimo prompt executado

Ultimo prompt executado: 12.1
