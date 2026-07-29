# Release 3.0.0-rc.1 Status

## Estado

RC.2 qualificado localmente. O workflow de release agora atua como gate seguro
para gerar exatamente `3.0.0-rc.1`, validar artefatos, gerar manifest com
checksums e preparar provenance sem publicar.

## Commit candidato atual

Base inicial: `15e926c` (`chore(release): complete FluentMap readiness audit`).
RC.1 local: `a71d0213976c51437e1301bbaae699e4b4519c1d`
(`chore(release): prepare versioning for 3.0.0-rc.1`).
O commit que deve ser usado na qualificacao remota do Prompt RC.3 e o commit
final do Prompt RC.2, com mensagem
`ci(release): qualify 3.0.0-rc.1 artifacts`. O hash exato sera obtido apos a
criacao do commit, porque registra-lo dentro do proprio commit alteraria o hash.

## Branch

- Origem: `feature/etapa-3`.
- Atual/final esperada: `release/3.0.0-rc.1`.
- Nenhum push executado neste prompt.

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

## Em andamento

- Nenhuma tarefa em andamento.

## Proximos passos

1. Revisar `git diff` e `git diff --check`.
2. Criar o commit `chore(release): prepare versioning for 3.0.0-rc.1`.
3. Em prompt futuro, executar workflow remoto apos push autorizado.

## RC blockers

- SourceLink/provenance ainda precisam ser validados em SHA remoto apos push do
  commit RC.2.
- Consumer smoke externo com os cinco pacotes RC ainda nao foi executado.

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
- Bloqueios negativos confirmados para `2.0.0` e stable `3.0.0`.

## Workflow runs

- Nenhum workflow remoto executado neste prompt.

## Ultimo prompt executado

Ultimo prompt executado: RC.2
