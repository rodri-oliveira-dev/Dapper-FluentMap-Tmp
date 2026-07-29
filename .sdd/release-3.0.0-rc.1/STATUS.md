# Release 3.0.0-rc.1 Status

## Estado

RC.5 gate local concluido sem blockers Critical ou High de RC. Os packages
`3.0.0-rc.1` foram validados por restore, build, tests, provider SQLite,
artifact validation, consumer smoke, trimming smoke, vulnerability audit e
benchmark guardrail representativo.

Candidate state: Ready for publication qualification
Candidate commit: registrado apos a criacao do commit unico RC.5, sem segundo
commit de evidencia, para evitar ciclo autorreferencial de hash.

## Commit candidato atual

Base inicial: `15e926c` (`chore(release): complete FluentMap readiness audit`).
RC.1 local: `a71d0213976c51437e1301bbaae699e4b4519c1d`
(`chore(release): prepare versioning for 3.0.0-rc.1`).
RC.2 local: `e6e462782c0151763679fc7802518b8026333d54`
(`ci(release): qualify 3.0.0-rc.1 artifacts`).
RC.3 remoto qualificado: `44f690195f9a06703e04c051411047b993644186`
(`fix(release): allow remote qualification on release branch`).

O commit RC.3 contem somente correcao de workflow necessaria porque
`release.yml` nao podia ser executado por `workflow_dispatch` enquanto nao
existente na default branch.

## Branch

- Origem: `feature/etapa-3`.
- Atual/final esperada: `release/3.0.0-rc.1`.
- Branch remota: `origin/release/3.0.0-rc.1`.
- Push executado neste prompt somente para a branch de release, sem `--force`.

## Concluido

- Discovery inicial de Git, historico, SDD, documentacao publica, projetos,
  workflows e versionamento.
- Pasta `.sdd/release-3.0.0-rc.1/` criada.
- Plano de release e ADRs da RC criados.
- Versionamento central seguro definido para `3.0.0-dev` local.
- Pack de `2.0.0` e stable `3.0.0` bloqueado durante o freeze.
- Workflow de release ajustado para usar `Version` explicito e prerelease
  validada.
- Changelog preparado com `3.0.0-rc.1 - Unreleased`.
- Restore, build, test, pack default, pack RC e bloqueios negativos de versao
  validados localmente.
- Especificacao `.sdd/release-3.0.0-rc.1/02-local-qualification.md` criada.
- Script `eng/validate-release-artifacts.ps1` criado para validacao local/CI dos
  artefatos da RC.
- Workflow de release endurecido para aceitar somente `3.0.0-rc.1` na branch
  `refs/heads/release/3.0.0-rc.1`.
- Manifest `artifact-manifest.json` gerado com repository, commit, branch,
  PackageIds, versoes e SHA-256.
- Validacao de artefatos cobre 5 `.nupkg`, 3 `.snupkg`, ausencia de test,
  benchmark e AOT smoke packages, nuspecs, repository commit, dependency ranges,
  README, license MIT e layouts analyzer/generator.
- Gate local RC.2 executado: restore, audit, build, test da solution, provider
  SQLite, pack, manifest, YAML parse e `git diff --check`.
- Push controlado da branch `release/3.0.0-rc.1`.
- Workflow remoto `Release` executado no run `30476842589`.
- Artifacts remotos baixados em `artifacts/release-3.0.0-rc.1/remote/`.
- Script `eng/validate-release-artifacts.ps1` passou contra os artifacts
  remotos.
- Manifest remoto comparado com manifest recalculado localmente e SHA-256
  confirmados.
- SourceLink validado nos PDBs de core, DI, Dommel, analyzers e generators.
- GitHub artifact attestations validadas com predicado SLSA provenance v1,
  cert identity, repository, source ref, source digest e subjects/hashes.
- Relatorio `.sdd/release-3.0.0-rc.1/03-remote-qualification.md` criado.
- Manifest versionado `.sdd/release-3.0.0-rc.1/artifacts.json` criado.
- Especificacao e evidencia
  `.sdd/release-3.0.0-rc.1/04-consumer-smoke.md` criada.
- Infraestrutura `eng/consumer-smoke/` criada para consumers externos por
  pacote, sem `ProjectReference`.
- Consumer smoke validou core, analyzer, generator, Dependency Injection,
  Dommel e trimming usando os packages RC restaurados em versao exata.
- Dependency inspection confirmou ausencia de referencias ao codigo-fonte
  local, ausencia de dependencias Roslyn runtime indevidas, resolucao esperada
  de `Dapper 2.1.79` e `Dommel 3.5.3`, e nenhum package `2.0.0`.
- Workflow `Release` passou a executar consumer smoke como gate apos validacao
  de artifacts.
- Prompt RC.5 executou inventario de gates, classificacao de problemas e
  validacao local final antes do commit candidato.
- `.sdd/release-3.0.0-rc.1/05-rc-gate-report.md` criado.
- `CHANGELOG.md` finalizado para a secao `3.0.0-rc.1 - Unreleased`.

## Em andamento

- Nenhuma tarefa em andamento.

## Proximos passos

1. Revisar o commit candidato RC.5.
2. Regenerar/qualificar artifacts remotos apos push autorizado da branch de
   release.
3. Antes do Prompt RC.6, criar autorizacao manual
   `.sdd/release-3.0.0-rc.1/PUBLISH-AUTHORIZATION.md`.

## RC blockers

- Critical blockers: 0.
- High RC blockers: 0.
- Remote qualification infrastructure: Ready with documented limitation.
- `workflow_dispatch` de `release.yml` continua indisponivel ate o workflow
  existir na default branch ou outro caminho de promocao ser definido. A
  qualificacao RC.3 foi executada por evento `push` na branch de release.
- Consumer smoke externo com os cinco pacotes RC: Passed.

## Package consumer classification

- `Dapper.FluentMap`: Passed.
- `Dapper.FluentMap.Dommel`: Passed.
- `Dapper.FluentMap.DependencyInjection`: Passed.
- `Dapper.FluentMap.Analyzers`: Passed.
- `Dapper.FluentMap.Generators`: Passed.

## Stable-only blockers

- Baseline API/binaria do fork.
- Feedback de adocao da RC.
- Decisao de package signing e SBOM formal.
- Revisao final de analyzer/generator release manifests.
- Certificacao adicional de providers se houver claim alem de SQLite.
- Native AOT publish/run se houver claim AOT.

## Artifacts

- Default: `artifacts/release-3.0.0-rc.1/default`, com 5 `.nupkg` e 3
  `.snupkg` em `3.0.0-dev`.
- RC: `artifacts/release-3.0.0-rc.1/rc`, com 5 `.nupkg` e 3 `.snupkg` em
  `3.0.0-rc.1`.
- RC.2 local: `artifacts/release-3.0.0-rc.1/rc2-local`, com 5 `.nupkg`, 3
  `.snupkg`, `artifact-manifest.json` e `dependencies.json`.
- RC.3 remoto: `artifacts/release-3.0.0-rc.1/remote`, com 5 `.nupkg`, 3
  `.snupkg`, `artifact-manifest.json`, `dependencies.json`, PDBs extraidos para
  SourceLink e bundles de attestation locais nao versionados.
- Bloqueios negativos confirmados para `2.0.0` e stable `3.0.0`.

## Workflow runs

- `30476842589`: `Release`, branch `release/3.0.0-rc.1`, commit
  `44f690195f9a06703e04c051411047b993644186`, evento `push`, resultado
  `success`.

## Ultimo prompt executado

Ultimo prompt executado: RC.5
