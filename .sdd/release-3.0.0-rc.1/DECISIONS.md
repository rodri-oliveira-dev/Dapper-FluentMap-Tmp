# Release 3.0.0-rc.1 Decisions

## ADR-RC-1 - Versao 3.0.0-rc.1

### Contexto

As Etapas 7-12 adicionaram uma superficie publica relevante e a auditoria final
recomendou Release Candidate, nao stable. Os package IDs historicos de core e
Dommel ja possuem `2.0.0` publicado.

### Decisao

Usar `3.0.0-rc.1` como versao pretendida da primeira RC do fork.

### Alternativas consideradas

- `2.0.0`: rejeitada porque ja existe nos IDs historicos.
- `2.1.0-rc.1`: rejeitada para esta RC porque a mudanca acumulada e grande e
  ainda nao ha baseline API/binaria do fork.
- `3.0.0` stable: rejeitada ate feedback de RC e gates stable.

### Consequencias

A linha assume SemVer major nova para reduzir ambiguidade com o pacote original
e permitir validacao publica antes de stable.

## ADR-RC-2 - Versao padrao segura de desenvolvimento

### Contexto

Antes deste freeze, `dotnet pack` padrao produzia `2.0.0`.

### Decisao

O pack local padrao passa a produzir `3.0.0-dev`. Versoes de release precisam
ser informadas explicitamente por `Version`.

### Alternativas consideradas

- Deixar `2.0.0` como padrao e depender de disciplina: rejeitada por risco de
  artefato errado.
- Usar `3.0.0` como padrao: rejeitada por risco de stable acidental.

### Consequencias

Builds locais continuam simples, mas seus pacotes sao claramente prerelease de
desenvolvimento.

## ADR-RC-3 - Branch de release

### Contexto

O checkout estava em `feature/etapa-3`, nome inadequado para release freeze.

### Decisao

Criar a branch local `release/3.0.0-rc.1` a partir de `15e926c`.

### Alternativas consideradas

- Continuar em `feature/etapa-3`: rejeitada por higiene de release.
- Criar a partir de `master`: rejeitada porque o trabalho das Etapas 7-12 esta
  na branch atual.

### Consequencias

O historico de release fica separado sem merge, rebase destrutivo ou force push.

## ADR-RC-4 - Proibicao de publicacao de 2.0.0

### Contexto

`Dapper.FluentMap` e `Dapper.FluentMap.Dommel` ja possuem `2.0.0` historico no
NuGet.org.

### Decisao

Bloquear pack de `2.0.0` via MSBuild e rejeitar `2.0.0` no workflow de release.

### Alternativas consideradas

- Documentar apenas a proibicao: rejeitada porque nao impede erro operacional.

### Consequencias

Um comando normal de pack ou release nao pode produzir o numero historico por
acidente.

## ADR-RC-5 - Politica de correcoes durante o freeze

### Contexto

O objetivo da RC.1 e congelar escopo, nao continuar feature work.

### Decisao

Aceitar somente correcoes diretamente ligadas a release blockers, validacao,
versionamento, packaging ou documentacao critica de release.

### Alternativas consideradas

- Corrigir bugs oportunistas durante o freeze: rejeitada por aumentar risco.

### Consequencias

Correcoes funcionais novas devem esperar nova etapa ou novo prompt explicito.

## ADR-RC-6 - Politica de push

### Contexto

Este prompt proibe push, tag, GitHub Release e publicacao.

### Decisao

Nao fazer push neste prompt. Push futuro deve ocorrer somente apos revisao local
do diff, commit validado e decisao explicita.

### Alternativas consideradas

- Push imediato da branch de release: rejeitada pelas restricoes do prompt.

### Consequencias

Todos os artefatos deste prompt permanecem locais.

## ADR-RC-7 - Tag pretendida

### Contexto

A RC precisa de uma tag futura previsivel, mas este prompt proibe cria-la.

### Decisao

A tag pretendida, quando autorizada, e `v3.0.0-rc.1`.

### Alternativas consideradas

- `3.0.0-rc.1` sem prefixo `v`: rejeitada para manter compatibilidade com tags
  historicas do repositorio.

### Consequencias

Nenhuma tag e criada agora; o nome fica reservado no plano.

## ADR-RC-8 - Packages participantes

### Contexto

A solution possui cinco projetos packable publicos.

### Decisao

Os cinco packages participam da RC e devem compartilhar exatamente a mesma
versao.

### Alternativas consideradas

- Publicar apenas core/Dommel: rejeitada porque DI, analyzers e generators fazem
  parte da linha validada.

### Consequencias

Pack default e pack RC devem gerar cinco `.nupkg`; core, Dommel e DI tambem
devem gerar `.snupkg`.

## ADR-RC-9 - Manifest e validacao de artefatos

### Contexto

O workflow RC.1 validava a contagem basica de artefatos diretamente no YAML e
gerava metadata simples. Para a qualificacao final da RC, o gate precisa
inspecionar nuspecs, dependency ranges, repository commit, layouts de pacote e
checksums sem duplicar regras complexas no workflow.

### Decisao

Criar `eng/validate-release-artifacts.ps1` como contrato reutilizavel local e
CI para validar os artefatos da versao `3.0.0-rc.1` e gerar
`artifact-manifest.json`.

### Alternativas consideradas

- Manter toda a logica em YAML: rejeitada por baixa reutilizacao local e maior
  risco de divergencia.
- Adicionar ferramenta externa de manifest/SBOM neste prompt: rejeitada porque
  SBOM formal permanece decisao futura e o gate atual nao deve introduzir nova
  dependencia operacional.

### Consequencias

O workflow de release passa a orquestrar restore, audit, build, test, pack,
validacao, manifest e provenance. A semantica de validacao dos artefatos fica
concentrada no script sem exigir secrets nem publicar pacotes.
