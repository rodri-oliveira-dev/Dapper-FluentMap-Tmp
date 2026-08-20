# Etapa 8 - Historical Regression Suite

Status: implementado no Prompt 8.6.

## Objetivo

Consolidar bugs historicos reais do projeto original
`henkmollema/Dapper-FluentMap` em uma suite permanente de regressao no fork,
sem criar um projeto de testes novo e sem ampliar comportamento publico alem do
que ja foi implementado na Etapa 8.

As paginas das issues arquivadas foram revalidadas no GitHub em 2026-07-28.
Todas as issues minimas estavam fechadas no projeto original. A verificacao
confirmou que os documentos do Prompt 8.1 ainda descreviam corretamente a
categoria historica de cada bug.

## Estrutura escolhida

Foram criadas areas `HistoricalRegression` dentro dos projetos existentes:

- `test/Dapper.FluentMap.Tests/HistoricalRegression/`
- `test/Dapper.FluentMap.Dommel.Tests/HistoricalRegression/`

Nao foi criado novo projeto porque os cenarios usam infraestrutura ja existente:
SQLite in-memory, Dapper, `QueryMapped*`, generated materializer registration e
Dommel.

## Categorias cobertas

- Core mapping regressions: expression parsing e propriedade ignorada.
- Materialization regressions: leitura Dapper, `QueryMapped*` runtime e generated
  materializer.
- Dommel regressions: resolvers, SQL gerado e materializacao por `Get`.
- Nested mapping regressions: caminhos aninhados com terminal repetido.
- Persistence behavior regressions: read-only, key nao identity, computed e
  database default on insert.

## Matriz

| Issue | Regression test | Projeto | Status |
| ----- | --------------- | ------- | ------ |
| #94 | `ReadOnlyPropertyShouldBeMaterializedButExcludedFromWrites` | `Dapper.FluentMap.Dommel.Tests` | Covered |
| #94 | `GeneratedAndRuntimeMaterializersShouldAgreeForHistoricalReadSemantics` | `Dapper.FluentMap.Tests` | Covered |
| #114 | `PropertyNamedLikeBclMemberShouldMapExpressionProperty` | `Dapper.FluentMap.Tests` | Covered |
| #122 | `NonIdentityKeyShouldBeInsertedAndOnlyUsedForUpdateWhereClause` | `Dapper.FluentMap.Dommel.Tests` | Covered |
| #123 | `ComputedPropertyShouldBeReadButExcludedFromInsertAndUpdate` | `Dapper.FluentMap.Dommel.Tests` | Covered |
| #123 | `GeneratedAndRuntimeMaterializersShouldAgreeForHistoricalReadSemantics` | `Dapper.FluentMap.Tests` | Covered |
| #126 | `NestedMemberPathsWithSameTerminalNameShouldMaterializeDistinctValues` | `Dapper.FluentMap.Tests` | Covered |
| #126 | `GeneratedAndRuntimeMaterializersShouldAgreeForHistoricalReadSemantics` | `Dapper.FluentMap.Tests` | Covered |
| #130 | `DatabaseDefaultOnInsertShouldOmitInsertColumnAndReadDatabaseValue` | `Dapper.FluentMap.Dommel.Tests` | Covered |
| #130 | `GeneratedAndRuntimeMaterializersShouldAgreeForHistoricalReadSemantics` | `Dapper.FluentMap.Tests` | Covered |
| #133 | `IgnoredPropertySelectedByDapperShouldRemainUnmappedWithoutThrowing` | `Dapper.FluentMap.Tests` | Covered |
| #133 | `GeneratedAndRuntimeMaterializersShouldAgreeForHistoricalReadSemantics` | `Dapper.FluentMap.Tests` | Covered |

## Prompt 8.1

As outras referencias diretamente relacionadas identificadas no Prompt 8.1 eram
os PRs historicos #129 e #131.

- PR #129 fica coberto pela regressao de key nao identity da issue #122.
- PR #131 fica coberto pelas regressoes de `Ignore()` das issues #130 e #133.

Nao foi identificada outra issue antiga, dentro do escopo de leitura versus
persistencia da Etapa 8, que justificasse novo caso alem dos cenarios minimos e
dos PRs relacionados.

## Differential test

`GeneratedAndRuntimeMaterializersShouldAgreeForHistoricalReadSemantics` registra
um materializer gerado via `AddGeneratedMaterializer(...)`, executa
`QueryMappedSingle<T>()` pelo caminho generated e repete a mesma consulta pelo
fallback runtime. O teste compara:

- read-only;
- computed;
- database-default-on-insert;
- nested paths `Rank.Level` e `Seniority.Level`;
- ignored column.

Assim, metadata de escrita continua neutra para leitura em ambos os caminhos, e
`Ignore()` permanece a unica semantica historica que remove materializacao.

## Bugs ainda reproduziveis

Nenhum bug historico minimo ficou reproduzivel nos cenarios adicionados.

O risco de compatibilidade ja documentado permanece: no Dommel,
`IsKey()` sem `SetGeneratedOption(DatabaseGeneratedOption.None)` continua
identity operacional por compatibilidade com maps antigos. A regressao historica
#122 protege explicitamente o caminho recomendado para key atribuida pela
aplicacao.
