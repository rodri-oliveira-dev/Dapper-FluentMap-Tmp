# Release 3.0.0-rc.1 Status

## Estado

RC.1 freeze preparado localmente. Escopo congelado, versionamento seguro e
validacao local concluidos neste prompt.

## Commit candidato atual

Base inicial: `15e926c` (`chore(release): complete FluentMap readiness audit`).
O commit RC.1 local passa a ser o candidato apos o commit semantico deste
prompt.

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

## Em andamento

- Nenhuma tarefa em andamento.

## Proximos passos

1. Revisar `git diff` e `git diff --check`.
2. Criar o commit `chore(release): prepare versioning for 3.0.0-rc.1`.
3. Em prompt futuro, executar workflow remoto apos push autorizado.

## RC blockers

- SourceLink/provenance ainda precisam ser validados em SHA remoto apos push.
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
- Bloqueios negativos confirmados para `2.0.0` e stable `3.0.0`.

## Workflow runs

- Nenhum workflow remoto executado neste prompt.

## Ultimo prompt executado

Ultimo prompt executado: RC.1
