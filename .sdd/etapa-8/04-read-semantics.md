# Etapa 8 - Read Semantics & Materialization Compatibility

Status: implementado no Prompt 8.3.

## Objetivo

Definir a semantica de leitura apos a introducao de metadata de persistencia,
preservando compatibilidade do FluentMap como biblioteca publica.

A regra central e:

```text
Ignore altera leitura/materializacao.
ReadOnly, ExcludeFromInsert, ExcludeFromUpdate, Computed e DatabaseDefaultOnInsert
alteram escrita/metadata de persistencia, nao leitura.
```

## Estados de propriedade

| Estado | Materializa? | Insert | Update | Generated | Observacao |
| --- | --- | --- | --- | --- | --- |
| normal | sim | sim | sim | nao | Mapping padrao. |
| ignored | nao | nao | nao | nao | `Ignore()` preserva o significado historico. |
| read-only | sim | nao | nao | nao | `ReadOnly()` nao e alias de `Ignore()`. |
| insert-excluded | sim | nao | sim | nao | `ExcludeFromInsert()` preserva update e leitura. |
| update-excluded | sim | sim | nao | nao | `ExcludeFromUpdate()` preserva insert e leitura. |
| computed | sim | nao | nao | sim | `Computed()` representa valor lido, gerado pelo banco e nao escrito. |
| generated/default | sim | nao | sim por default | sim | `DatabaseDefaultOnInsert()` omite insert, mas preserva update ate que `ExcludeFromUpdate()` seja composto. |

`PropertyPersistenceMetadata.ParticipatesInMaterialization` e a dimensao de
leitura. As dimensoes `ParticipatesInInsert`, `ParticipatesInUpdate`,
`IsGenerated`, `IsComputed` e `HasDatabaseDefaultOnInsert` nao devem ser usadas
por materializadores para decidir se uma propriedade sera preenchida.

## Dapper normal mapping

`Dapper.Query<T>()` usa o type map global instalado pelo FluentMap para
propriedades no nivel raiz e constructor mapping root-level.

- normal: coluna configurada preenche a propriedade ou parametro de construtor.
- ignored: coluna configurada retorna sentinela interna e nao cai para o mapping
  default do Dapper; a propriedade permanece com valor inicial/default.
- read-only: coluna configurada preenche normalmente.
- insert-excluded: coluna configurada preenche normalmente.
- update-excluded: coluna configurada preenche normalmente.
- computed: coluna configurada preenche normalmente.
- generated/default: coluna configurada preenche normalmente.

Nested paths nao sao materializados por `Dapper.Query<T>()`; eles sao protegidos
por sentinela para evitar fallback incorreto para o membro terminal.

## QueryMapped runtime

`QueryMapped*` usa `NestedMaterializationPlan` quando nao ha generated
materializer compativel.

- normal: entra no plano de materializacao.
- ignored: e pulado durante a criacao do plano.
- read-only: entra no plano.
- insert-excluded: entra no plano.
- update-excluded: entra no plano.
- computed: entra no plano.
- generated/default: entra no plano.

Essa regra vale para propriedades flat, nested mappings, Value Objects
construidos por componentes, tipos imutaveis e profiles.

## Generated materialization

Generated materializers sao uma otimizacao de leitura `IDataRecord -> entidade`.
Eles observam apenas o shape de colunas e a semantica de leitura.

- normal: descriptor usa `GeneratedMaterializerColumn.Map(column, memberPath)`.
- ignored: descriptor usa `GeneratedMaterializerColumn.Ignore(column)` e o
  delegate nao atribui o membro.
- read-only: descriptor usa `Map`, nao `Ignore`.
- insert-excluded: descriptor usa `Map`, nao `Ignore`.
- update-excluded: descriptor usa `Map`, nao `Ignore`.
- computed: descriptor usa `Map`, nao `Ignore`.
- generated/default: descriptor usa `Map`, nao `Ignore`.

O source generator deve aceitar chamadas fluent de escrita conhecidas como
neutras para leitura. A presenca dessas chamadas nao deve impedir a emissao de
materializer gerado quando o restante do map e estaticamente suportado.

O runtime valida descriptors gerados contra o mapping efetivo antes do dispatch.
Essa validacao deve rejeitar divergencias de `Ignore()` e de member path, mas
nao deve rejeitar apenas porque metadata de insert/update mudou.

## Constructor mapping

Para `Dapper.Query<T>()`, o constructor type map do FluentMap aplica apenas
mapeamentos root-level simples:

- ignored: nao participa da escolha de construtor nem do binding de parametro.
- read-only/computed/generated/excluded: participam como propriedade normal.

Para `QueryMapped*`, construtores root, nested e de Value Objects seguem a mesma
regra de leitura do plano runtime/generated:

- ignored nao fornece argumento;
- write exclusions continuam fornecendo argumento quando a coluna esta presente.

## Nested mapping

Nested member paths sao identificados por caminho completo, por exemplo:

```text
Rank.Level
Seniority.Level
```

O terminal `Level` nao e suficiente para identidade de mapping. Essa regra vale
para runtime materializer, generated descriptor, diagnostics e validacao de
duplicidade.

Write metadata anexada a um nested path nao altera a criacao de objetos
intermediarios nem a regra de null subtree. Apenas `Ignore()` remove o path da
materializacao configurada.

## Value Objects

Value Objects mapeados por componentes continuam sendo materializados por
construtores publicos compativeis no `QueryMapped*`.

- ignored: componente nao entra no plano e nao participa do construtor.
- read-only/computed/generated/excluded: componente entra no plano como qualquer
  outro componente de leitura.

Value Objects escalares por TypeHandler continuam no boundary do Dapper e nao
sao transformados por metadata de escrita.

## Profiles

Profiles sao query-scoped em `QueryMapped<TEntity, TProfile>()`.

Cada profile possui mapping e generated descriptors proprios. A semantica de
leitura e a mesma:

- `Ignore()` no profile remove leitura naquele profile.
- `ReadOnly()` e demais write semantics no profile continuam materializando.
- O profile nao altera o type map global usado por `Dapper.Query<T>()`.

## Regressions historicas protegidas

- #114: expression parsing usa o `MemberInfo` real da expression tree e valida
  `PropertyInfo`, evitando confusao com metodos como membros de `string` ou
  `TimeSpan`.
- #126: member paths aninhados com mesmo terminal continuam distintos no runtime
  e no generated path.
- #133: `Ignore()` nao usa `PropertyInfo` falso/incompleto; Dapper normal mapping
  pode ver a coluna ignorada sem `NotImplementedException`, e a propriedade nao e
  preenchida.

