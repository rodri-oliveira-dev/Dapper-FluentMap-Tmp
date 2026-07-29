# Release Plan - 3.0.0-rc.1

## Objetivo

Congelar o escopo da primeira Release Candidate da linha do fork e garantir que
nenhum pack normal produza a versao historica `2.0.0` ou a stable acidental
`3.0.0`.

## Escopo congelado

- Generated materialization e runtime fallback.
- Persistence semantics para metadata de escrita e integracao Dommel.
- Advanced query materialization com QueryMultiple, ReadMapped e streaming.
- Property converters para leitura controlada pelo FluentMap.
- Isolated configuration, `FluentMapRuntime` e DI.
- Compatibility, provider matrix, package metadata, CI/release hardening e
  documentacao publica das Etapas 7-12.

## Fora de escopo

- Publicacao NuGet.
- Criacao de tag ou GitHub Release.
- Stable `3.0.0`.
- Correcoes funcionais nao relacionadas ao release freeze.
- Baseline API/binaria definitiva para stable.
- SBOM formal, package signing, Native AOT completo e certificacao real de SQL
  Server/PostgreSQL.

## Packages

- `Dapper.FluentMap`
- `Dapper.FluentMap.Dommel`
- `Dapper.FluentMap.DependencyInjection`
- `Dapper.FluentMap.Analyzers`
- `Dapper.FluentMap.Generators`

## Versionamento

- Versao base da linha: `3.0.0`.
- Versao local padrao: `3.0.0-dev`.
- Versao da RC: `3.0.0-rc.1`.
- `Directory.Build.props` centraliza `FluentMapPackageVersionPrefix=3.0.0` e
  `VersionSuffix=dev` quando nenhuma versao explicita e informada.
- `Directory.Build.targets` bloqueia pack de `2.0.0` e `3.0.0` durante o freeze.
- Release workflow deve usar `-p:Version=<package-version>` e rejeitar stable
  durante esta RC.

## Branch strategy

- Branch de origem: `feature/etapa-3`.
- Branch de release local: `release/3.0.0-rc.1`.
- Commit base: `15e926c` (`chore(release): complete FluentMap readiness audit`).
- A branch antiga foi mantida intacta; nenhuma alteracao foi feita diretamente
  em `master`.

## Commit strategy

- Um unico commit semantico para o prompt RC.1:
  `chore(release): prepare versioning for 3.0.0-rc.1`.
- Nao fazer push neste prompt.
- Nao misturar bug fixes ou features com o freeze.

## Release gates

- `dotnet restore ./Dapper.FluentMap.sln`
- `dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore`
- `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build`
- Pack padrao deve gerar somente artefatos `3.0.0-dev`.
- Pack explicito da RC deve gerar exatamente artefatos `3.0.0-rc.1`.
- A pasta RC deve conter 5 `.nupkg` e 3 `.snupkg`.
- Nenhum artefato `2.0.0` ou stable `3.0.0` pode ser produzido.

## Rollback strategy

- Como nao ha push, tag ou publicacao, rollback local e remover/reverter o commit
  RC.1 antes de publicar a branch.
- Se o workflow remoto falhar apos push futuro, manter os artefatos sem publicar
  e corrigir em novo commit na mesma branch de release.

## Riscos conhecidos

- SourceLink/provenance dependem de execucao no SHA remoto apos push.
- SQL Server/PostgreSQL seguem com harness condicional, nao certificados.
- Native AOT completo nao foi validado.
- Package signing e SBOM formal permanecem decisoes futuras.
- A pasta nao rastreada `src/Dapper.FluentMap/etapas/` ja existia antes deste
  prompt e nao faz parte da RC.
