# Remote Qualification

## Branch

- Branch enviada: `release/3.0.0-rc.1`.
- Remote usado para push: `origin` com push URL HTTPS
  `https://github.com/rodri-oliveira-dev/Dapper-FluentMap.git`.
- O remote fetch local continua configurado como SSH, mas a autenticacao SSH
  falhou com `Permission denied (publickey)`. O push foi feito via HTTPS
  autenticado pelo GitHub CLI.
- Primeiro push controlado: `e6e462782c0151763679fc7802518b8026333d54`
  (`ci(release): qualify 3.0.0-rc.1 artifacts`), sem `--force`.
- Segundo push necessario: `44f690195f9a06703e04c051411047b993644186`
  (`fix(release): allow remote qualification on release branch`), sem
  `--force`, para permitir execucao remota do workflow de release por `push`.

## Commit

Commit qualificado remotamente:
`44f690195f9a06703e04c051411047b993644186`.

O commit esperado do RC.2 (`e6e462782c0151763679fc7802518b8026333d54`) foi
enviado primeiro. A qualificacao remota final usa `44f690195...` porque o
workflow `release.yml` nao era dispatchable enquanto inexistente na default
branch, exigindo uma correcao de workflow na propria branch de release.

## Workflow run

- Workflow: `Release`.
- Run ID: `30476842589`.
- Attempt: `1`.
- Evento: `push`.
- URL: `https://github.com/rodri-oliveira-dev/Dapper-FluentMap/actions/runs/30476842589`.
- Criado em: `2026-07-29T17:46:13Z`.
- Concluido em: `2026-07-29T17:47:52Z`.
- Resultado: `success`.
- Jobs:
  - `Validate release package`: `success`.
  - `Attest release package provenance`: `success`.

## Version

Versao qualificada: `3.0.0-rc.1`.

O workflow validou a versao fixa `3.0.0-rc.1`, rejeitou publish por padrao e
executou build, test, pack, validacao de artifacts, inventory de dependencias e
attestation sem publicacao NuGet.

## Artifacts

- Artifact remoto: `release-packages-3.0.0-rc.1`.
- Artifact ID: `8733989011`.
- Run ID: `30476842589`.
- Baixado localmente em:
  `artifacts/release-3.0.0-rc.1/remote/`.
- Conteudo baixado:
  - 5 `.nupkg`;
  - 3 `.snupkg`;
  - `artifact-manifest.json`;
  - `dependencies.json`.

## Package hashes

Os hashes SHA-256 abaixo foram recalculados nos artifacts remotos baixados e
comparados com `artifact-manifest.json`.

| Artifact | SHA-256 |
| --- | --- |
| `Dapper.FluentMap.3.0.0-rc.1.nupkg` | `55059c450db16a28d8e058460571950bcf967a88e4435a519a82612609f6407f` |
| `Dapper.FluentMap.3.0.0-rc.1.snupkg` | `9c1e96ba9f0760311280b4b6ffefce05a7f8d0dbe38844fefc4c9949711f90f7` |
| `Dapper.FluentMap.Analyzers.3.0.0-rc.1.nupkg` | `0b9e4c01bce2ef772b441124a03df554637a729c71912242f7ca28cfec8576fb` |
| `Dapper.FluentMap.DependencyInjection.3.0.0-rc.1.nupkg` | `339cabaea4399aa4d0387794a03910d4348c1ce46fa001aaa4a9ceb35f7a8785` |
| `Dapper.FluentMap.DependencyInjection.3.0.0-rc.1.snupkg` | `866400c82b10f9d4655bd03e0ae94ef99c70bb7da68f9ee9a675815ff6d11065` |
| `Dapper.FluentMap.Dommel.3.0.0-rc.1.nupkg` | `9c4b2157cf5f65b17e8914077921e1d23836adebb7f2fce2a6ff6f6673c8b680` |
| `Dapper.FluentMap.Dommel.3.0.0-rc.1.snupkg` | `b249b91f0764664a4e5f60161d082374ce1d22215c8d977cd886b0ced258a3bf` |
| `Dapper.FluentMap.Generators.3.0.0-rc.1.nupkg` | `9fcfc0c4586e35c408b49e964c1ed622cfc49192089da466809c721553920629` |

## SourceLink

SourceLink foi validado com `sourcelink` `3.1.1`.

- SourceLink URL template:
  `https://raw.githubusercontent.com/rodri-oliveira-dev/Dapper-FluentMap/44f690195f9a06703e04c051411047b993644186/*`.
- Commit remoto confirmado:
  `44f690195f9a06703e04c051411047b993644186`.
- `sourcelink test` passou para:
  - `Dapper.FluentMap.pdb`;
  - `Dapper.FluentMap.DependencyInjection.pdb`;
  - `Dapper.FluentMap.Dommel.pdb`;
  - `Dapper.FluentMap.Analyzers.pdb`;
  - `Dapper.FluentMap.Generators.pdb`.
- A ferramenta baixou os arquivos fonte via `raw.githubusercontent.com` e
  validou checksums SHA-256 dos documentos registrados nos PDBs.
- Os PDBs de core, DI e Dommel foram obtidos dos `.snupkg`; os PDBs de analyzer
  e generator foram obtidos dos respectivos `.nupkg`.

## Provenance

Provenance foi validada com GitHub artifact attestations, predicado
`https://slsa.dev/provenance/v1`.

- Verificacao executada com `gh attestation verify` para cada um dos 8
  artifacts.
- Repositorio exigido: `rodri-oliveira-dev/Dapper-FluentMap`.
- Source ref exigido: `refs/heads/release/3.0.0-rc.1`.
- Source digest exigido:
  `44f690195f9a06703e04c051411047b993644186`.
- Cert identity exigida:
  `https://github.com/rodri-oliveira-dev/Dapper-FluentMap/.github/workflows/release.yml@refs/heads/release/3.0.0-rc.1`.
- Issuer OIDC: `https://token.actions.githubusercontent.com`.
- Workflow: `Release`.
- Runner: `github-hosted`.
- Invocation:
  `https://github.com/rodri-oliveira-dev/Dapper-FluentMap/actions/runs/30476842589/attempts/1`.
- Timestamp verificado via Rekor: `2026-07-29T14:47:50-03:00`.
- A attestation contem 8 subjects e todos os artifacts baixados possuem subject
  correspondente com SHA-256 identico.

Os bundles de attestation foram baixados apenas como evidencia local
nao versionada em `artifacts/release-3.0.0-rc.1/remote/attestations/`.

## Security

- `publish` permaneceu desabilitado; o step `Guard disabled publish path` foi
  ignorado porque publish nao foi solicitado.
- Nenhum package foi publicado.
- Nenhuma tag foi criada.
- Nenhum GitHub Release foi criado.
- Nenhum merge em `master` foi executado.
- Os arquivos SDD versionados nao incluem tokens nem URLs assinadas temporarias.
- Permissoes do workflow:
  - job de validacao: `contents: read`;
  - job de provenance: `contents: read`, `id-token: write`,
    `attestations: write`.
- O run apresentou uma anotacao informativa de Actions sobre deprecacao de
  Node.js 20 para `actions/download-artifact`; nao bloqueou a qualificacao.

## Failures and retries

1. `gh workflow run .github/workflows/release.yml --ref release/3.0.0-rc.1`
   falhou com HTTP 404 porque o workflow `release.yml` nao existe na default
   branch.
2. Falha classificada como falha de acionamento remoto do workflow, nao falha de
   build, test, pack, SourceLink ou provenance.
3. Correcao aplicada no escopo de workflow:
   `fix(release): allow remote qualification on release branch`.
4. A branch foi reenviada sem `--force`; o evento `push` criou o run
   `30476842589`.
5. O run terminal passou sem retries adicionais.

## Result

Remote qualification: Passed with limitations.

Limitacao restante para RC.4: enquanto `release.yml` nao existir na default
branch ou nao houver outro caminho de promocao definido, `workflow_dispatch` do
workflow de release pelo GitHub CLI/UI continua indisponivel. A qualificacao
remota desta RC foi executada por `push` na branch de release.
