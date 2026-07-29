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

## ADR-14 - Provider certification boundary

### Contexto

O core e desenhado sobre ADO.NET/Dapper, mas isso nao certifica automaticamente
SQL Server, PostgreSQL, SQLite, MySQL/MariaDB ou SQL CE. O Prompt 12.3 exigiu
separar design provider-agnostic de validacao real e criar uma matriz de
provider.

### Decisao

Criar um projeto dedicado `Dapper.FluentMap.ProviderCompatibility.Tests`.
SQLite e certificado automaticamente na lane rapida. SQL Server e PostgreSQL
ganham harness condicional por `DFM_SQLSERVER_CONNECTION_STRING` e
`DFM_POSTGRESQL_CONNECTION_STRING`, mas permanecem `Not validated` ate serem
executados contra servicos reais em ambiente controlado ou CI.

MySQL/MariaDB nao vira obrigatorio neste prompt porque nao havia dependencia,
imagem, service container ou cobertura previa suficiente. SQL CE e tratado como
limitacao upstream/legado, apesar do builder Dommel continuar registrado.

### Alternativas consideradas

- Declarar todos os builders Dommel como certificados: rejeitado por ausencia
  de teste real.
- Adicionar Testcontainers imediatamente: rejeitado porque o repositorio nao
  tinha infraestrutura existente e a instrucao pediu nao adicionar
  automaticamente se houver caminho mais simples.
- Rodar contra containers locais de outros projetos: rejeitado porque isso
  acoplaria a validacao a estado externo nao reprodutivel deste repositorio.

### Consequencias

A CI valida SQLite como provider real sem deixar o build principal muito lento.
SQL Server/PostgreSQL ficam prontos para validacao assim que connection strings
ou service containers dedicados forem configurados. A documentacao deve usar
`Validated`, `Partial`, `Not validated` e `Unsupported upstream` de forma
explicita.

## ADR-15 - Package metadata, SourceLink and symbols

### Contexto

Core e Dommel ainda usavam `PackageLicenseUrl`, nao incluiam README no pacote e
apontavam `PackageProjectUrl` para o repositorio upstream original. Nenhum
pacote gerava `.snupkg` e nao havia propriedades explicitas de repository
metadata, SourceLink, determinismo ou CI build.

### Decisao

Centralizar metadata moderna em `Directory.Build.props`: repository URL do
fork, project URL, license expression MIT, README, SourceLink/repository
metadata, determinismo e symbol packages. Analyzer e generator nao geram
`.snupkg`, porque o layout correto desses pacotes e `analyzers/dotnet/cs`, sem
`lib/`; seus PDBs sao empacotados ao lado das DLLs no pacote principal.

### Alternativas consideradas

- Manter metadata upstream original: rejeitado porque o fork precisa apontar
  para o repositorio que contem o codigo publicado.
- Referenciar pacote SourceLink explicitamente: rejeitado porque o SDK 10 ja
  inclui suporte SourceLink para GitHub; adicionar pacote seria redundante.
- Gerar `.snupkg` para analyzer/generator: rejeitado porque o SDK produziu
  symbol package vazio e falhou com `NU5017`.

### Consequencias

Pacotes runtime passam a ter `.snupkg`; todos os pacotes possuem README,
license expression e repository URL/commit. A validacao completa de URL/checksum
do SourceLink deve rodar apos o commit estar disponivel no remoto.

## ADR-16 - API/package validation baseline

### Contexto

O SDK oferece package validation nativa no `Pack`. O fork ja divergiu bastante
do pacote original `2.0.0`, com APIs novas e uma quebra confirmada em Dommel
(`GeneratedOption` nullable). Uma baseline obrigatoria contra o pacote original
nao representa um gate verde para a linha atual.

### Decisao

Habilitar `EnablePackageValidation=true` nos pacotes com `lib/`: core, Dommel e
DependencyInjection. Nao adicionar `PublicApiAnalyzers` neste prompt. A baseline
historica contra `2.0.0` original fica documentada no public API review e a
baseline obrigatoria do fork deve ser definida contra o primeiro RC/versao
aprovada do proprio fork.

### Alternativas consideradas

- Adicionar `PublicApiAnalyzers` agora: rejeitado por redundancia e por criar um
  baseline textual grande antes da decisao de versao.
- Bloquear pack contra `2.0.0` original: rejeitado porque registraria quebras
  ja acumuladas do fork, nao apenas mudancas acidentais futuras.
- Nao habilitar tooling: rejeitado porque package validation sem baseline ja
  protege consistencia de pacote e prepara o gate para uma baseline futura.

### Consequencias

`dotnet pack` passa a executar package validation nativa para pacotes runtime.
A release ainda precisa de baseline do fork antes de stable.

## ADR-17 - Strong naming and package signing

### Contexto

Assemblies atuais nao possuem public key token. O prompt pediu revisar a
necessidade historica de strong naming e validar pacotes. `dotnet nuget verify`
confirmou hashes, mas falhou com `NU3004` porque os pacotes nao estao assinados.

### Decisao

Nao adicionar strong naming e nao assinar pacotes neste prompt.

### Alternativas consideradas

- Assinar assemblies strong-name por tradicao: rejeitado porque muda identidade
  de assembly e pode ser breaking.
- Assinar `.nupkg` sem processo de release/certificado definido: rejeitado por
  risco operacional e segredo ausente.

### Consequencias

Strong naming e package signing permanecem decisoes de release engineering
separadas. Se forem adotados futuramente, devem ter chave/certificado,
validacao, ownership e estrategia de migracao.

## ADR-18 - CI hardening and release workflow

### Contexto

O workflow existente ja restaurava, compilava, testava e empacotava, mas nao
possuia release workflow dedicado, provenance, artifact retention explicito,
timeouts, checkout endurecido ou pin por SHA completo.

### Decisao

Endurecer o CI com permissoes minimas, actions pinadas por SHA, checkout sem
persistencia de credencial, timeouts, concurrency e artefatos previsiveis.
Adicionar workflow manual de release que valida e empacota uma versao informada,
gera metadata e provenance, mas falha explicitamente caso alguem tente publicar.

### Alternativas consideradas

- Publicar diretamente no NuGet por segredo `NUGET_API_KEY`: rejeitado por
  segredo longo e ausencia de gates.
- Fazer release automatico em tag neste prompt: rejeitado ate baseline de API,
  trusted publishing e ambiente de aprovacao existirem.
- Manter actions apenas por tag: rejeitado para hardening inicial, embora
  Dependabot passe a monitorar atualizacoes.

### Consequencias

O repositorio passa a ter caminho de release reproduzivel e auditavel, mas
publicacao segue bloqueada por design. A manutencao dos SHAs pinados deve vir
por PRs de Dependabot/revisao humana.

## ADR-19 - Dependency audit, SDK pin and SBOM boundary

### Contexto

Nao havia Dependabot/Renovate, lock files, `global.json` ou SBOM. O NuGet Audit
e as GitHub Artifact Attestations sao capacidades nativas/sustentaveis do
ecossistema atual.

### Decisao

Adicionar Dependabot para Actions/NuGet, habilitar NuGet Audit transitive com
severidade `low`, tratar `NU1901`-`NU1904` como erro em CI e fixar o SDK em
`global.json`. Gerar inventario JSON de dependencias e provenance no release
workflow. Nao adicionar SBOM formal neste prompt.

### Alternativas consideradas

- Adotar lock files agora: adiado para tarefa propria por impactar restore e
  renovacao de dependencias em toda a solution.
- Adotar Microsoft SBOM Tool agora: rejeitado por adicionar ferramenta externa
  antes da politica de SBOM estar definida.
- Chamar inventario `dotnet list package` de SBOM: rejeitado porque nao e SPDX
  nem CycloneDX.

### Consequencias

Dependencias vulneraveis passam a bloquear CI quando reportadas pelo NuGet
Audit. Builds usam SDK previsivel. SBOM permanece requisito futuro, sem claim
indevido neste release hardening.
