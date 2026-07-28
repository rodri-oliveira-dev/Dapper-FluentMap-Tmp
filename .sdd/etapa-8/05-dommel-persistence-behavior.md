# Etapa 8 - Dommel Persistence Behavior

Status: implementado no Prompt 8.4.

## Objetivo

Fazer `Dapper.FluentMap.Dommel` consumir a metadata de persistencia do core para
os comandos gerados pelo Dommel, mantendo o core fora da geracao de SQL.

Dommel 3.5.3 expoe `ColumnPropertyInfo.IsGenerated` como filtro unico usado por
`INSERT` e `UPDATE`. Para preservar a semantica separada de `Insert` e `Update`,
a integracao usa duas traducoes:

- `DommelPropertyResolver` traduz `ParticipatesInUpdate=false` para
  `ColumnPropertyInfo.IsGenerated=true`, porque o `UPDATE` do Dommel filtra por
  esse contrato.
- `DommelPersistenceSqlBuilder` envolve os SQL builders padrao e recompõe as
  colunas de `INSERT` a partir de `ParticipatesInInsert`, ignorando o filtro
  unico recebido do Dommel quando existe map FluentMap registrado.

O core continua sem SQL provider-specific. A diferenca de SQL fica no pacote
Dommel e delega quoting, parametros e formato de insert aos builders do Dommel.

## Matriz de comportamento

| Behavior | SELECT | INSERT | UPDATE |
| --- | --- | --- | --- |
| Normal | Sim | Sim | Sim |
| Ignore | Nao | Nao | Nao |
| ReadOnly | Sim | Nao | Nao |
| InsertExcluded | Sim | Nao | Sim |
| UpdateExcluded | Sim | Sim | Nao |
| Generated | Sim | Conforme subtipo gerado | Conforme subtipo gerado |
| Computed | Sim | Nao | Nao |
| NonIdentityKey | Sim | Sim | WHERE only; nao entra no SET |

`DatabaseDefaultOnInsert()` e tratado como `InsertExcluded` com metadata
`Generated` e `DefaultOnInsert`: a coluna e omitida do `INSERT`, continua sendo
lida em `SELECT` e participa do `UPDATE` por default. Quando combinado com
`ExcludeFromUpdate()`, passa a ser read-only depois do insert.

`Identity` e uma key gerada pelo banco: participa de leitura e do `WHERE` quando
usada como key, mas nao entra em `INSERT` nem no `SET` de `UPDATE`.

## Dommel 3.5.3

O contrato efetivamente usado nesta etapa foi o pacote `Dommel` 3.5.3 referenciado
por `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj`.

Observacoes relevantes:

- `IPropertyResolver.ResolveProperties(Type)` retorna `ColumnPropertyInfo`.
- `ColumnPropertyInfo.IsGenerated` e derivado de `GeneratedOption != None`.
- `BuildInsertQuery` usa propriedades nao geradas e depois chama
  `ISqlBuilder.BuildInsert(...)`.
- `BuildUpdateQuery` usa propriedades nao geradas no `SET` e key properties no
  `WHERE`.
- Nao ha hook publico separado para `INSERT` versus `UPDATE`.

## Decisoes implementadas

- `ReadOnly()` e `Computed()` continuam materializaveis em SELECT e sao omitidos
  de INSERT/UPDATE.
- `ExcludeFromInsert()` e `DatabaseDefaultOnInsert()` sao omitidos de INSERT e
  continuam no UPDATE.
- `ExcludeFromUpdate()` continua no INSERT e e omitido do UPDATE.
- Key nao identity declarada com `IsKey().SetGeneratedOption(None)` participa do
  INSERT e nao entra no SET do UPDATE.
- Composite key nao identity segue a mesma regra para todos os componentes.
- `Ignore()` continua fora de SELECT/INSERT/UPDATE para os caminhos Dommel.
- Mappings herdados via `IncludeBase<TBase>()` sao considerados pelos resolvers
  Dommel para nome de coluna, insert e update.

## Compatibilidade

Mappings sem as novas opcoes preservam o comportamento historico esperado.
`IsKey()` sem `SetGeneratedOption(None)` continua sendo tratado como identity
operacional no key resolver Dommel para compatibilidade com maps antigos.

Consumidores que registrarem SQL builders customizados depois de `ForDommel()`
substituem o wrapper instalado pela integracao e ficam responsaveis por honrar a
metadata de insert. Os builders padrao de SQL Server, SQL Server CE, SQLite,
PostgreSQL e MySQL sao envolvidos automaticamente.

## Cobertura

`DommelPersistenceIntegrationTests` executa operacoes reais com SQLite
in-memory cobrindo:

- propriedade normal em INSERT e UPDATE;
- ignored;
- read-only;
- exclude insert;
- exclude update;
- computed/generated;
- identity;
- database default equivalente a `created_at DEFAULT CURRENT_TIMESTAMP`;
- mapping herdado;
- key nao identity;
- composite key nao identity;
- operacoes repetidas em entidades diferentes.

SQLite foi usado para validar semantica geral de omissao de colunas e defaults
de banco. Nenhuma conclusao provider-specific de SQL Server ou PostgreSQL foi
inferida alem do contrato de builders do Dommel.
