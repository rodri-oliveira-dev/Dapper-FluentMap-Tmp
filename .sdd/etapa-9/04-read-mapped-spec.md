# ReadMapped and Profiles Specification

Prompt executado em 2026-07-28.

## Objetivo

`QueryMultipleMapped` expoe materializacao FluentMap por result set, usando o
mesmo caminho de selecao ja usado por `QueryMapped*`.

APIs publicas:

```csharp
using var multi = connection.QueryMultipleMapped(sql);

var customers = multi.ReadMapped<Customer>();
var legacyCustomers = multi.ReadMapped<Customer, LegacyProfile>();
var singleCustomer = multi.ReadMappedSingle<Customer>();
var singleLegacyCustomer = multi.ReadMappedSingle<Customer, LegacyProfile>();
```

Nao foi adicionada variante `ReadMappedSingleOrDefault`, porque a API existente
do projeto possui `QueryMappedSingle*`, mas nao possui
`QueryMappedSingleOrDefault*`. Essa decisao evita multiplicar superficie publica
sem contrato equivalente no restante do FluentMap.

## Mapping e profiles

Cada chamada de leitura resolve o mapping para o result set atual:

- `ReadMapped<TEntity>()` usa o mapping default da entidade;
- `ReadMapped<TEntity, TProfile>()` usa o profile registrado para a entidade;
- profiles nao alteram o type map global do Dapper;
- default map e profile map podem ser lidos para a mesma entidade em grids
  diferentes sem colisao de cache ou materializer.

Profile ausente falha com `FluentMapConfigurationException`, preservando o
comportamento de `QueryMapped<TEntity, TProfile>()`.

## Buffering

Todas as APIs `ReadMapped*` sao buffered.

Mesmo retornando `IEnumerable<TEntity>`, `ReadMapped<TEntity>()` le todo o grid
atual em memoria antes de retornar e chama `IDataReader.NextResult()` para
posicionar o wrapper no proximo result set.

Nao ha streaming/unbuffered neste incremento.

## Empty results

`ReadMapped<TEntity>()` e `ReadMapped<TEntity, TProfile>()` retornam uma colecao
vazia quando o grid atual nao tem linhas.

`ReadMappedSingle<TEntity>()` e
`ReadMappedSingle<TEntity, TProfile>()` seguem a semantica de LINQ `Single()`:

- zero linhas: `InvalidOperationException`;
- uma linha: retorna a entidade;
- mais de uma linha: `InvalidOperationException`.

Como o grid e buffered antes da aplicacao de `Single()`, o wrapper ja avancou
para o proximo result set quando a excecao de cardinalidade e observada.

## Null semantics

As regras sao as mesmas do runtime materializer usado por `QueryMapped*`:

- subtree nested fica `null` quando todas as colunas mapeadas da subtree sao
  `DBNull`;
- subtree e criada quando ao menos uma coluna da subtree tem valor;
- Value Object nullable recebe `null` quando suas colunas mapeadas sao todas
  `DBNull`;
- `DBNull` para tipo valor nao nullable segue a conversao/default atual;
- `Ignore()` exclui a coluna da materializacao;
- metadata de escrita (`ReadOnly`, `Computed`, `DatabaseDefaultOnInsert`,
  `ExcludeFromInsert`, `ExcludeFromUpdate`) nao muda leitura.

## Nested objects, Value Objects e constructors

`ReadMapped*` suporta os mesmos cenarios de `QueryMapped*`, porque ambos chamam
`MappedRowMaterializer`:

- mapeamentos explicitos root-level;
- convencoes e naming policies;
- objetos aninhados settable;
- objetos aninhados imutaveis construidos por construtor publico compativel;
- Value Objects por componentes;
- construtores publicos de entidades imutaveis;
- fallback Dapper default para membros root-level nao configurados.

Cenarios nao suportados, como caminho nested sem construtor publico compativel
ou construtores ambiguos, continuam falhando com `FluentMapConfigurationException`.

## Exception behavior

- `connection == null` em `QueryMultipleMapped` falha com
  `ArgumentNullException`;
- `sql == null` em overload conveniente falha com `ArgumentNullException`;
- leitura apos `Dispose()` falha com `ObjectDisposedException`;
- leitura apos o ultimo result set falha com `InvalidOperationException`;
- falhas de configuracao/materializacao mantem o tipo de excecao do
  materializer compartilhado;
- falhas durante materializacao ou `NextResult()` descartam o reader antes de
  propagar a excecao.

## Generated dispatch

Para cada result set, o wrapper captura o shape ordenado de colunas e chama o
dispatcher compartilhado:

```text
result set atual
    |
    v
column shape ordenado
    |
    v
TryGetGeneratedMaterializer(entity, profile, shape)
    |
 +--+--+
 |     |
 sim   nao
 |     |
generated
       runtime fallback
```

O lookup generated usa a chave:

```text
EntityType + ProfileType opcional + ordered ColumnShape
```

Isso isola:

```csharp
ReadMapped<Customer>()
ReadMapped<Customer, LegacyProfile>()
```

e tambem isola dois grids do mesmo tipo quando a ordem ou os nomes de colunas
mudam.

## Runtime fallback

Quando nao ha materializer gerado ou quando o descriptor gerado nao combina com
o mapping efetivo atual, o dispatcher chama
`MappingRegistry.GetMaterializationPlan(entity, profile, columns)`.

A cache de plano runtime tambem inclui entity, profile e shape ordenado de
colunas, portanto `QueryMultipleMapped` nao possui um cache separado nem uma
segunda regra de selecao.

## Historical regressions

O Prompt 9.3 adicionou regressao minima para:

- #22: convencoes aplicadas em multiplos result sets mapeados;
- #43: mapeamento explicito aplicado em um result set posterior, com colunas
  equivalentes ao cenario historico de `DynamicTable.Column`.

Esses testes cobrem o caminho opt-in `QueryMultipleMapped(...).ReadMapped*`.
O caminho Dapper puro `QueryMultiple(...).Read<T>()` permanece fora da nova API
e continua dependente do type map global instalado pelo FluentMap.
