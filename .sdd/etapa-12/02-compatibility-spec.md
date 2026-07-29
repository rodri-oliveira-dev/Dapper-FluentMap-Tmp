# Compatibility Specification

Esta especificacao define a politica pretendida para uma release futura. Ela
nao muda contratos, versoes ou targets neste prompt.

## .NET TFMs

Politica proposta:

- Pacotes publicos continuam suportando `netstandard2.0` como TFM minimo ate
  decisao explicita de major/compatibilidade.
- Testes, smoke apps e benchmarks podem continuar em `net10.0` para validar em
  runtime moderno sem elevar o requisito dos consumidores.
- Multi-targeting de bibliotecas so deve ser adicionado se houver motivo
  concreto: analyzers de trimming/AOT, APIs condicionais, performance ou
  compatibilidade mensuravel.
- Se um TFM moderno for adicionado futuramente, ele deve ser aditivo
  (`netstandard2.0;net8.0` ou superior), nao substitutivo.

## Dapper

Politica proposta:

- Suporte minimo atual: `Dapper >= 2.1.79`.
- A matriz de release deve validar pelo menos:
  - a versao minima suportada;
  - a versao mais recente estavel de Dapper aprovada para a release;
  - cenarios de type map, constructor mapping, `CommandDefinition`,
    `ExecuteReader`, `ExecuteReaderAsync` e TypeHandler.
- Uso de APIs publicas do Dapper e permitido.
- Uso de internals por reflection, hoje `SqlMapper.TypeHandlerCache<T>.Parse`,
  deve ser tratado como risco de compatibilidade e coberto por teste dedicado.
- Se uma versao futura de Dapper quebrar esse boundary, a biblioteca deve falhar
  com diagnostico claro e documentar a faixa suportada.

## Dommel

Politica proposta:

- Suporte minimo atual: `Dommel >= 3.5.3`.
- Dommel e pacote opcional e nao faz parte do contrato do core.
- A integracao Dommel atual e process-wide porque usa extension points globais
  de `DommelMapper`.
- Provider builders registrados: SQL Server, SQL CE, SQLite, PostgreSQL e MySQL.
  Isso e provider support por integracao de builder, nao provider certification.
- Certification de provider exige teste real em CI ou harness documentado.

## Providers

Politica proposta:

- Provider-certified hoje: SQLite para testes automatizados locais/CI.
- Provider-independent: `DataTableReader` e interfaces ADO.NET para materializacao.
- Provider-supported-but-not-certified: SQL Server, SQL CE, PostgreSQL e MySQL
  via Dommel builders existentes.
- Documentacao publica deve diferenciar:
  - supported by design;
  - covered by automated tests;
  - manually smoke-tested;
  - not certified.

## Source compatibility

Politica proposta:

- Preservar nomes publicos, namespaces, generic constraints, overloads e
  comportamento observavel das APIs historicas sempre que possivel.
- Novas APIs devem ser aditivas.
- `FluentMapper.Initialize`, `FluentMapper.EntityMaps`,
  `FluentMapper.TypeConventions`, `EntityMap`, `PropertyMap`, conventions e
  type maps historicos permanecem compatibilidade legada.
- Marcacoes `[Obsolete]` futuras devem ser documentadas e nao remover API no
  mesmo release menor.
- Mudancas de validacao que rejeitam configuracoes contraditorias podem ser
  tratadas como bugfix, mas precisam teste de regressao e nota de migracao.

## Binary compatibility

Politica proposta:

- Release estavel exige baseline binaria formal para os pacotes publicos.
- Remover tipo/membro publico, alterar assinatura, alterar generic constraint,
  mudar tipo de retorno publico ou trocar tipo base/interface publica e breaking
  change.
- Adicionar membro abstrato ou alterar interface publica existente e breaking
  change.
- Interfaces novas devem ser aditivas; preferir interfaces auxiliares para
  metadata nova, como ja foi feito com persistence/conversion.
- O baseline deve cobrir core, Dommel, DI, analyzers e generators.

## Analyzer compatibility

Politica proposta:

- IDs `DFM001`-`DFM015` sao parte do contrato de usuario quando publicados.
- Severity padrao e categoria devem ser estaveis dentro de uma major, salvo bug
  claro.
- Novas regras devem ser registradas nos manifests Roslyn corretos.
- Regras que podem gerar falsos positivos relevantes devem iniciar como Info ou
  Warning, nao Error, salvo quando o erro for estaticamente provavel.
- Analyzer package nao deve expor dependencias Roslyn transitivas.

## Generator compatibility

Politica proposta:

- `AddGeneratedMappings()` emitido e contrato publico gerado e deve permanecer
  source-compatible dentro da major.
- Generated materialization e otimizacao. Fallback runtime deve continuar
  preservando comportamento quando um map/shape nao e suportado.
- Diagnostics de fallback devem ser informativos e nao quebrar builds por
  padrao.
- O generator nao deve executar construtores de maps, acessar banco, parsear SQL
  ou scanear assemblies referenciados.

## Native AOT

Politica proposta:

- Nao declarar suporte Native AOT completo no estado atual.
- Declaracoes permitidas:
  - registro explicito e gerado sao os caminhos preferenciais para apps Native
    AOT/trimming;
  - assembly scanning e APIs `QueryMapped*`/`ReadMapped*` podem ser sensiveis a
    trimming/dynamic code;
  - generated materializers reduzem reflection no hot path, mas nao eliminam o
    fallback.
- Claim Native AOT so sera permitido apos publish e execucao em CI com
  toolchain nativa instalada e sem warnings inexplicados.

## Trimming

Politica proposta:

- APIs conhecidamente sensiveis devem permanecer anotadas com
  `RequiresUnreferencedCode` e/ou `RequiresDynamicCode`.
- Smokes `PublishTrimmed=true` devem ser parte da matriz de release para
  registro explicito, registro gerado e DI.
- Warnings conhecidos de dependencias, como `IL2104` em Dapper, devem ser
  documentados por versao e nao escondidos.
- Nao usar suppressions para simular compatibilidade.

## Package compatibility

Politica proposta:

- Package IDs historicos devem ser tratados com cuidado especial:
  `Dapper.FluentMap` e `Dapper.FluentMap.Dommel` ja possuem historico publico.
- Pacotes novos (`DependencyInjection`, `Analyzers`, `Generators`) precisam de
  pre-release antes de estabilidade.
- Pacotes devem conter README quando aplicavel, license expression, repository
  metadata, SourceLink/symbols quando configurados e dependencias corretas.
- Analyzer/generator packages devem suprimir dependencias transitivas e conter
  assembly em `analyzers/dotnet/cs`.

## Semantic Versioning

Politica proposta:

- Patch: bugfix compativel, documentacao, validacao de pacote sem mudanca de
  contrato.
- Minor: API aditiva compativel.
- Major: breaking source/binary behavior ou mudanca relevante de contrato.
- Pre-release: obrigatorio para release candidate deste fork antes de qualquer
  stable, devido ao salto de superficie publica e divergencia do pacote
  original.

## Deprecation

Politica proposta:

- Deprecation deve ter:
  - alternativa documentada;
  - motivo claro;
  - janela minima de um minor/pre-release antes da remocao;
  - testes mantendo o comportamento antigo enquanto a API existir.
- Candidatos futuros:
  - mutacao direta de `FluentMapper.EntityMaps`;
  - mutacao direta de `FluentMapper.TypeConventions`;
  - assembly scanning em cenarios trimmed/AOT, com alternativa explicita/gerada.

## Breaking changes

Politica proposta:

- Qualquer breaking change exige ADR, teste, migration guide e major/pre-release.
- Correcoes de bug com mudanca comportamental devem ser chamadas pelo nome e
  vinculadas ao comportamento incorreto anterior.
- Breaking changes proibidas sem decisao explicita:
  - elevar TFM minimo do core;
  - remover APIs estaticas historicas;
  - remover Dommel process-wide sem alternativa;
  - trocar package IDs;
  - alterar semantics de `Ignore()`, persistence metadata, profiles ou
    precedence mapping sem teste e migracao.

## Release Criteria

Uma release so deve ser considerada pronta quando:

- `dotnet restore ./Dapper.FluentMap.sln` passa.
- `dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore`
  passa.
- `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build`
  passa.
- Pacotes packable corretos sao gerados e inspecionados.
- Warnings de build/pack sao zero ou explicitamente aceitos como conhecidos.
- API/binary compatibility passa contra baseline aprovado.
- NuGet metadata esta correta para o fork e para cada pacote.
- README e package READMEs correspondem ao comportamento real.
- Migration guide e compatibility matrix existem.
- Provider support/certification esta documentado.
- Smokes trimming rodam e warnings conhecidos estao documentados.
- Native AOT so aparece como suportado se houver publish/run validado.
- Vulnerability audit nao aponta vulnerabilidades conhecidas sem triagem.
- Release candidate passou por validacao antes de stable.

## Version Strategy Recommendation

Recomendacao tecnica:

- Nao publicar este estado como `2.0.0` estavel.
- Para package IDs historicos, usar uma nova linha pre-release do fork, por
  exemplo `3.0.0-rc.1` se a decisao for assumir que as evolucoes acumuladas
  exigem nova major, ou `2.1.0-rc.1` apenas se ApiCompat provar compatibilidade
  forte com `2.0.0`.
- Pela superficie publica acumulada e pela ausencia atual de ApiCompat, a opcao
  mais segura e `3.0.0-rc.1`.
- Para pacotes novos (`DependencyInjection`, `Analyzers`, `Generators`), tambem
  usar pre-release alinhado ao core antes de stable.
- Nao alterar versao neste prompt; a decisao final deve ocorrer apos
  compatibility baseline e pacote validado.

## Incremental Plan

1. Compatibility matrix: definir TFMs, Dapper, Dommel, providers e baselines de
   API/binario por pacote.
2. Provider validation: manter SQLite como certificado inicial e decidir se
   SQL Server/PostgreSQL/MySQL entram em CI ou permanecem support-by-design.
3. Public API/package hardening: adicionar ApiCompat/package validation,
   corrigir NuGet metadata, README de pacote, repository metadata, SourceLink e
   symbols.
4. CI/release engineering: ajustar workflow, adicionar matrix minima,
   vulnerability audit, pack validation, trimming smoke e artefatos.
5. Documentation/migration/support policies: publicar matriz de compatibilidade,
   migration guide, support policy e known limitations.
6. Release candidate validation: gerar RC, instalar em consumer smoke e validar
   Dapper/Dommel/provider matrix.
7. Final audit: repetir build/test/pack, revisar diff, atualizar STATUS e
   liberar apenas com blockers zerados ou aceitos.
