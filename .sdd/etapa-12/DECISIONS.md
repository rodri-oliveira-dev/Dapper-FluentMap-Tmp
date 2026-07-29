# Etapa 12 Decisions

## ADR-1 - TFMs oficialmente suportados

### Contexto

Os pacotes publicos atuais targetam `netstandard2.0`; testes, AOT smoke e
benchmarks targetam `net10.0`. O core deve preservar compatibilidade ampla.

### Decisao

Manter `netstandard2.0` como TFM oficial minimo dos pacotes publicos ate nova
ADR. Runtimes modernos continuam sendo usados em testes.

### Alternativas consideradas

- Elevar tudo para `net10.0`: rejeitado por breaking change desnecessaria.
- Multi-targeting imediato: adiado ate haver beneficio validado.

### Consequencias

Consumidores existentes continuam cobertos. Claims de AOT/trimming modernos
exigem smokes separados porque `netstandard2.0` nao habilita tudo sozinho.

## ADR-2 - Politica de Dapper compatibility

### Contexto

O pacote referencia Dapper `2.1.79` e usa APIs publicas de type map/reader, mas
tambem acessa `SqlMapper.TypeHandlerCache<T>.Parse(object)` por reflection.

### Decisao

Declarar `Dapper >= 2.1.79` como minimo atual e exigir matriz de release contra
a versao minima e uma versao estavel atual aprovada. O boundary de TypeHandler
deve ter teste dedicado e diagnostico claro.

### Alternativas consideradas

- Assumir qualquer Dapper 2.x como compativel: rejeitado sem matriz.
- Remover TypeHandler interoperability: breaking change e fora do escopo.

### Consequencias

Dapper vira parte central da matriz de release. Internals usados por reflection
sao risco conhecido, nao promessa irrestrita.

## ADR-3 - Provider support vs provider certification

### Contexto

SQLite e `DataTableReader` sao validados. Dommel registra builders para varios
providers, mas CI nao executa bancos externos.

### Decisao

Separar "supported by design" de "certified by automated tests". SQLite e
provider-independent sao certificados inicialmente; demais providers ficam
documentados como nao certificados ate haver harness real.

### Alternativas consideradas

- Declarar todos os providers como certificados: rejeitado por falta de teste.
- Remover builders Dommel: breaking change sem necessidade.

### Consequencias

A documentacao fica honesta e evita claims maiores do que a evidencia.

## ADR-4 - Binary compatibility

### Contexto

As etapas anteriores adicionaram muitas APIs publicas. Nao ha ferramenta formal
de API/binary compatibility configurada.

### Decisao

Release estavel exige baseline formal de API/binario para core, Dommel, DI,
analyzers e generators.

### Alternativas consideradas

- Confiar em revisao manual: insuficiente para biblioteca publica.
- Aceitar compatibilidade apenas source: insuficiente para consumidores NuGet.

### Consequencias

ApiCompat/package validation entram como blocker antes de stable.

## ADR-5 - Package validation

### Contexto

`dotnet pack` passa, mas core/Dommel geram `NU5125` e aviso de README ausente.
Nao ha SourceLink, symbols ou repository metadata.

### Decisao

Pacotes devem passar pack validation sem warnings inexplicados, conter metadata
NuGet moderna e ter conteudo inspecionado antes de release.

### Alternativas consideradas

- Aceitar warnings legados para release inicial: permitido apenas para RC
  interna, nao para stable publica.

### Consequencias

Core/Dommel precisam de hardening de metadata antes da release.

## ADR-6 - Version strategy

### Contexto

NuGet.org ja possui `Dapper.FluentMap` e `Dapper.FluentMap.Dommel` `2.0.0`.
O fork adicionou grande superficie publica e novos pacotes.

### Decisao

Nao publicar como `2.0.0` estavel. A recomendacao inicial e `3.0.0-rc.1` para
uma linha do fork, salvo se ApiCompat provar que `2.1.0-rc.1` e seguro.

### Alternativas consideradas

- Continuar `2.0.0`: rejeitado porque a versao ja existe publicamente.
- Publicar `2.1.0` stable imediatamente: rejeitado sem compatibilidade formal.

### Consequencias

A estrategia final depende de baseline, mas o caminho seguro inicial e RC.

## ADR-7 - Release candidate strategy

### Contexto

Ha muitas capacidades novas, pacotes novos e claims parciais de trimming/AOT.

### Decisao

Toda release publica deve passar primeiro por RC com pacotes instalados em
consumer smoke externo ou amostra de consumo.

### Alternativas consideradas

- Stable direto apos suite verde: rejeitado por risco de package/API.

### Consequencias

Release engineering deve incluir validacao de instalacao e consumo, nao so pack.

## ADR-8 - Warning policy

### Contexto

Build Release esta limpo. Pack emite warnings NuGet para core/Dommel. Trimming
historico emite warnings conhecidos.

### Decisao

Build/test/pack de release devem ter zero warnings ou lista explicita de
warnings aceitos com motivo e escopo. Warnings de pacote metadata nao devem
permanecer em stable.

### Alternativas consideradas

- Tratar todo warning como fatal agora: adiado ate os warnings conhecidos serem
  classificados.
- Ignorar warnings de pack: rejeitado para release publica.

### Consequencias

Warnings conhecidos podem existir em RC apenas se documentados; stable deve
reduzir o maximo possivel.

## ADR-9 - AOT/trimming claims

### Contexto

Smokes trimmed passaram em etapas anteriores, mas Native AOT publish/run foi
bloqueado por falta de linker. APIs importantes seguem anotadas com
`RequiresUnreferencedCode` e `RequiresDynamicCode`.

### Decisao

Nao declarar Native AOT completo. Declarar apenas caminhos preferenciais e
limitacoes: registro explicito/gerado para trimming, scanning sensivel e
`QueryMapped*` com fallback runtime sensivel.

### Alternativas consideradas

- Declarar AOT-safe por causa de generated materializers: rejeitado por fallback.
- Remover fallback para obter claim AOT: breaking change e fora do escopo.

### Consequencias

Documentacao e NuGet metadata devem evitar claims amplos de AOT.

## ADR-10 - Dependency versioning

### Contexto

Versoes estao nos `.csproj`, sem CPM ou lock file. O repo e pequeno, mas a
release precisa auditabilidade.

### Decisao

Nao migrar para Central Package Management neste prompt. Antes da release,
decidir entre manter versoes locais com auditoria simples ou adotar CPM/lock em
tarefa propria.

### Alternativas consideradas

- Adotar CPM imediatamente: fora do escopo documental.
- Atualizar dependencias junto com release readiness: rejeitado por misturar
  riscos.

### Consequencias

Matriz de dependencias deve ser documentada e auditada a cada RC.

## ADR-11 - Release automation

### Contexto

CI atual restaura, compila, testa, empacota e faz upload de artefatos. Nao ha
publish NuGet automatico.

### Decisao

Manter publish manual/ausente ate gates de release estarem definidos. Automatizar
primeiro build/test/pack/validation/provenance; publish so depois de aprovacao
explicita.

### Alternativas consideradas

- Adicionar publish ao workflow atual: rejeitado por falta de gates.

### Consequencias

Nao ha risco de publicacao acidental nesta etapa, mas release final requer
workflow dedicado.

## ADR-12 - Support policy

### Contexto

O fork evoluiu uma biblioteca publica arquivada, com APIs legadas e novas.

### Decisao

Documentar suporte por package/TFM/provider e manter escopo do core como
mapping/materializacao para Dapper. Recursos fora do escopo, como ORM/CRUD/SQL
generator/provider universal, permanecem nao suportados.

### Alternativas consideradas

- Prometer suporte amplo por implicacao do README: rejeitado.

### Consequencias

Known limitations e compatibility matrix viram artefatos obrigatorios da
release.

## ADR-13 - Runtime and Dapper compatibility matrix

### Contexto

Os pacotes publicos targetam `netstandard2.0`, enquanto a suite executavel roda
em `net10.0`. A dependencia direta em Dapper era declarada como versao exata no
`.csproj`, mas no pacote NuGet isso virava minimo aberto `>= 2.1.79`. O prompt
12.2 exigiu matriz pragmatica entre TFMs suportados e versoes Dapper
suportadas.

### Decisao

Declarar a faixa `Dapper [2.1.79,3.0.0)` e validar em CI a lane essencial de
Dapper por propriedade MSBuild sobregravavel `DapperPackageVersion`. Como
`2.1.79` e simultaneamente a minima suportada e a latest stable em NuGet.org em
2026-07-29, a matriz atual possui uma unica lane
`minimum-and-latest-stable`.

Separar analyzer/generator em job proprio de CI, porque esses pacotes dependem
de Roslyn/compiler references e nao representam runtime TFM support.

### Alternativas consideradas

- Manter dependencia aberta sem upper bound: rejeitado porque permitiria Dapper
  major futuro sem validacao.
- Fixar exatamente `2.1.79` no pacote: rejeitado porque bloquearia consumidores
  sem evidencia de incompatibilidade dentro da major atual.
- Criar matriz combinatoria de OS/provider/preview: rejeitado ate haver pergunta
  concreta de compatibilidade.

### Consequencias

O pacote fica mais conservador contra Dapper `3.x`. Quando surgir nova stable
de Dapper `2.x`, o workflow deve ganhar lane `latest-stable` antes da release
reivindicar essa versao como validada. Dommel continua validado apenas como
integracao opcional process-wide.
