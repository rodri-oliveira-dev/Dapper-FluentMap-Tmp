# CI and Release Design

Documento criado em 2026-07-29 para o Prompt 12.5.

## Pipeline Model

```text
Pull Request
    ↓
Restore
Build
Tests
Compatibility
Package validation

Main
    ↓
Full validation
Provider tests
Pack artifacts

Release
    ↓
Validation
Packages
Provenance
Publish
```

Publish permanece desabilitado neste prompt. O workflow de release prepara
validacao, packages, metadata e provenance; a publicacao NuGet deve ser
habilitada somente depois de configurar trusted publishing/OIDC no NuGet.org e
um ambiente de aprovacao no GitHub.

## Workflows

### Pull Request / Main: `.github/workflows/ci.yml`

Triggers:

- `pull_request`;
- `push` para `master`;
- `push` para `feature/etapa-3`, enquanto a etapa de release engineering roda
  nessa branch de trabalho.

Jobs:

- `compatibility`: restore, audit, build Release e testes runtime contra a lane
  Dapper `2.1.79`, que hoje e minimo e latest stable aprovados.
- `roslyn-components`: build/test separado para analyzers e generators.
- `pack`: pack unico da solution, validacao do conjunto de artefatos e upload
  de `.nupkg`, `.snupkg` e metadata.

### Release: `.github/workflows/release.yml`

Trigger:

- `workflow_dispatch` manual com `package-version`, default
  `3.0.0-rc.1`.

Fluxo:

1. valida SemVer simples;
2. rejeita `2.0.0`, porque esse numero ja existe nos package IDs historicos;
3. restaura;
4. audita dependencias;
5. compila Release;
6. executa testes da solution;
7. empacota com `-p:VersionPrefix=<package-version>`;
8. valida o conjunto esperado de artefatos;
9. gera metadata de release;
10. faz upload dos artefatos;
11. gera artifact attestations de provenance para `.nupkg` e `.snupkg`.

O input `publish` existe apenas como guarda explicita: se marcado, o workflow
falha com mensagem indicando que publish ainda nao esta habilitado. Isso evita
publicacao acidental antes dos gates de release.

## CI Hardening

Medidas aplicadas:

- `permissions: contents: read` como base.
- Elevacao de permissao somente no job de provenance:
  `id-token: write` e `attestations: write`.
- Actions pinadas por SHA completo, com comentario da tag revisada:
  - `actions/checkout` `v7`;
  - `actions/setup-dotnet` `v6`;
  - `actions/upload-artifact` `v7`;
  - `actions/download-artifact` `v6`;
  - `actions/attest-build-provenance` `v3`.
- `actions/checkout` usa `persist-credentials: false`, `fetch-depth: 1` e
  `show-progress: false`.
- Workflows usam `concurrency` para evitar runs redundantes no CI e evitar dois
  releases simultaneos da mesma versao.
- Jobs possuem `timeout-minutes`.
- Artifact retention explicito:
  - CI: 14 dias;
  - release: 90 dias.

Fork PR safety:

- O CI nao usa `pull_request_target`.
- O CI nao le secrets.
- O CI nao publica pacotes.
- O CI roda com token de leitura apenas.
- Provenance e release rodam somente em `workflow_dispatch`, nao em PR.

Cache:

- Nenhum cache de NuGet foi adicionado neste prompt.
- Motivo: o repositorio ainda nao usa lock files; cache sem lock aumenta a
  chance de comportamento menos auditavel. Cache pode ser adotado junto com
  `packages.lock.json` ou politica equivalente.

## Dependency Security

Estado observado em codigo:

- Nao havia Dependabot ou Renovate configurado.
- `NuGet.Config` limpa feeds e usa apenas NuGet.org.
- Nao ha lock files.
- Versoes ainda ficam nos projetos, mas a versao dos pacotes FluentMap foi
  centralizada por propriedade compartilhada.

Medidas aplicadas:

- Adicionado `.github/dependabot.yml` para GitHub Actions e NuGet.
- Adicionado NuGet Audit em `Directory.Build.props`:
  - `NuGetAudit=true`;
  - `NuGetAuditMode=all`;
  - `NuGetAuditLevel=low`.
- Em CI, warnings `NU1901` a `NU1904` viram erro via `WarningsAsErrors`.
- Workflows executam
  `dotnet list ./Dapper.FluentMap.sln package --vulnerable --include-transitive`.

Politica:

- Nao fazer upgrades major automaticamente ou junto de release hardening.
- Atualizacoes major devem ter PR proprio, revisao de compatibilidade e matriz
  de testes.
- Lock files permanecem decisao futura. Para a matriz Dapper atual, o range
  `[2.1.79,3.0.0)` e a lane override por `DapperPackageVersion` sao mais
  importantes do que travar tudo sem estrategia de renovacao.

## Reproducibility

Medidas aplicadas:

- Adicionado `global.json` com SDK `10.0.302` e roll-forward limitado ao feature
  band.
- Workflows usam `actions/setup-dotnet` apontando para `global.json`.
- O mesmo fluxo semantico e usado localmente e em CI:
  `restore` -> `build Release --no-restore` -> `test Release --no-build` ->
  `pack Release --no-build`.
- `Directory.Build.props` ja define:
  - `Deterministic=true`;
  - `ContinuousIntegrationBuild=true` quando `CI=true`;
  - SourceLink/repository metadata;
  - symbol packages para pacotes runtime.

Versionamento:

- A versao `2.0.0` nao foi trocada para RC neste prompt porque a ADR-6 ainda
  condiciona a decisao final a baseline de compatibilidade.
- O valor foi centralizado em `FluentMapPackageVersionPrefix` para evitar edicao
  manual duplicada em cinco `.csproj`.
- O workflow de release permite validar RC com
  `-p:VersionPrefix=<package-version>` sem alterar arquivos de projeto.

## Artifacts

Conjunto esperado:

- 5 `.nupkg`:
  - `Dapper.FluentMap`;
  - `Dapper.FluentMap.Dommel`;
  - `Dapper.FluentMap.DependencyInjection`;
  - `Dapper.FluentMap.Analyzers`;
  - `Dapper.FluentMap.Generators`.
- 3 `.snupkg`:
  - `Dapper.FluentMap`;
  - `Dapper.FluentMap.Dommel`;
  - `Dapper.FluentMap.DependencyInjection`.
- release metadata:
  - `release-metadata.json` com repo, ref, SHA, run, SDK e SHA-256 dos
    pacotes;
  - `dependencies.json` com inventario `dotnet list package --include-transitive
    --format json`.

O workflow falha se aparecer pacote de test/benchmark/smoke ou se a contagem
esperada nao bater.

## SBOM and Provenance

Capacidades avaliadas:

- GitHub Artifact Attestations geram provenance com OIDC e nao exigem segredo
  persistente.
- NuGet trusted publishing permite trocar OIDC de GitHub Actions por credencial
  curta no NuGet.org, evitando API key longa.
- `actions/attest-sbom` atesta SBOM existente, mas nao gera o SBOM.
- Microsoft SBOM Tool e sustentavel, mas adiciona ferramenta externa e politica
  operacional propria.

Decisao deste prompt:

- Adicionar provenance nativo do GitHub para os pacotes no release workflow.
- Nao adicionar SBOM formal ainda.
- Incluir inventario JSON de dependencias como release metadata, sem chamar isso
  de SBOM SPDX/CycloneDX.
- Futuro SBOM deve usar ferramenta aprovada, gerar SPDX ou CycloneDX e entao
  opcionalmente usar `actions/attest-sbom`.

Referencias oficiais consultadas:

- NuGet trusted publishing:
  `https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing`
- NuGet audit:
  `https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages`
- GitHub artifact attestations:
  `https://docs.github.com/actions/security-for-github-actions/using-artifact-attestations/using-artifact-attestations-to-establish-provenance-for-builds`
- GitHub SBOM attestation action:
  `https://github.com/actions/attest-sbom`

## Release History

Historico acessivel no checkout local:

- Tags locais/remotas historicas vao de `v1.0.2` a `v2.0.0`.
- Tag mais recente observada: `v2.0.0`, em 2020-08-23.
- `gh release list --repo rodri-oliveira-dev/Dapper-FluentMap --limit 20`
  nao retornou releases publicadas no fork.

Implicacao:

- Tags historicas existem, mas o fork ainda precisa de primeiro RC proprio.
- `2.0.0` nao deve ser reutilizada nos package IDs historicos.

## Publish Requirements

Antes de habilitar publish:

1. criar baseline de API do proprio fork;
2. configurar trusted publisher no NuGet.org para o repositorio/workflow;
3. criar ambiente GitHub protegido para aprovacao manual;
4. substituir a guarda `publish` por fluxo OIDC aprovado;
5. validar install/consumer smoke dos pacotes gerados;
6. documentar rollback e criterios de promocao RC -> stable.

Nenhum segredo NuGet foi adicionado e nenhum publish foi executado.
