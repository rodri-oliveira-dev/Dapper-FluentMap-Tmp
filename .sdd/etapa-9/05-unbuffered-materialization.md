# Synchronous Unbuffered Materialization

Prompt executado em 2026-07-28.

## Discovery do Dapper

Dependencia efetiva confirmada no core: `Dapper` 2.1.79.

Superficie publica relevante confirmada no pacote instalado:

- `SqlMapper.Query<T>(IDbConnection, string, ..., bool buffered = true, ...)`;
- `SqlMapper.Query<T>(IDbConnection, CommandDefinition)`;
- `CommandDefinition` possui `CommandFlags`, com `Buffered = 1` e `None = 0`;
- `SqlMapper.ExecuteReader(IDbConnection, CommandDefinition)`;
- `SqlMapper.ExecuteReader(IDbConnection, CommandDefinition, CommandBehavior)`;
- `SqlMapper.QueryUnbufferedAsync(...)` existe para caminho async;
- `GridReader.ReadUnbufferedAsync*` existe para multiple result sets async.

Nao ha API publica sincronica chamada `QueryUnbuffered<T>` no Dapper 2.1.79. O
caminho sincronico unbuffered do Dapper e exposto pela convencao
`Query<T>(..., buffered: false)` ou por `CommandDefinition` sem
`CommandFlags.Buffered`.

O FluentMap nao usa `Dapper.Query<T>` para a nova API porque precisa aplicar seu
proprio materializador por `IDataRecord`, incluindo nested objects, Value
Objects, profiles e generated materializers. A execucao continua baseada em
API publica do Dapper: `SqlMapper.ExecuteReader`.

## API

APIs publicas adicionadas:

```csharp
IEnumerable<TEntity> QueryMappedUnbuffered<TEntity>(
    this IDbConnection connection,
    string sql,
    object param = null,
    IDbTransaction transaction = null,
    int? commandTimeout = null,
    CommandType? commandType = null)
    where TEntity : class;

IEnumerable<TEntity> QueryMappedUnbuffered<TEntity, TProfile>(
    this IDbConnection connection,
    string sql,
    object param = null,
    IDbTransaction transaction = null,
    int? commandTimeout = null,
    CommandType? commandType = null)
    where TEntity : class
    where TProfile : IMappingProfile;

IEnumerable<TEntity> QueryMappedUnbuffered<TEntity>(
    this IDbConnection connection,
    CommandDefinition command)
    where TEntity : class;

IEnumerable<TEntity> QueryMappedUnbuffered<TEntity, TProfile>(
    this IDbConnection connection,
    CommandDefinition command)
    where TEntity : class
    where TProfile : IMappingProfile;
```

O nome `Unbuffered` foi escolhido para nao alterar a semantica buffered de
`QueryMapped<T>()` e para manter o lifetime perigoso visivel no call site.

Os overloads convenientes criam `CommandDefinition` com `CommandFlags.None`.
Quando o usuario passa um `CommandDefinition`, o FluentMap preserva o comando
recebido.

## Lazy execution

`QueryMappedUnbuffered*` retorna uma sequencia lazy.

Regras:

- argumentos nulos sao validados na chamada publica;
- o comando nao e executado na chamada publica;
- o comando nao e executado por `GetEnumerator()` isolado;
- o comando e executado no primeiro `MoveNext()`;
- cada nova enumeracao executa um novo comando.

Essa semantica e diferente de `QueryMapped<T>()`, que executa e bufferiza antes
de retornar.

## Reader lifetime

O `IDataReader` fica aberto durante a enumeracao.

Fluxo:

1. primeiro `MoveNext()` chama `SqlMapper.ExecuteReader`;
2. o FluentMap captura o shape ordenado de colunas;
3. o FluentMap escolhe o materializer uma vez;
4. cada `MoveNext()` materializa uma linha;
5. fim da enumeracao, early break, dispose explicito ou excecao descartam o
   reader.

O enumerator deve ser descartado quando a enumeracao parar cedo. `foreach`
cumpre esse contrato automaticamente.

## Connection lifetime

O FluentMap nao assume ownership da conexao recebida.

Regra preservada:

- conexao inicialmente fechada: Dapper/ADO.NET abre no primeiro `MoveNext()` e
  o dispose do reader fecha ao final, early break, dispose explicito ou excecao;
- conexao inicialmente aberta: permanece aberta apos a enumeracao ou apos o
  dispose do enumerator.

O usuario e responsavel por manter uma conexao externa aberta e valida durante
toda a enumeracao unbuffered.

## Early enumeration termination

Parar a enumeracao antes do fim descarta o reader quando o enumerator e
descartado.

Exemplos seguros:

```csharp
foreach (var row in connection.QueryMappedUnbuffered<Customer>(sql))
{
    break;
}
```

```csharp
using var enumerator = connection.QueryMappedUnbuffered<Customer>(sql).GetEnumerator();
if (enumerator.MoveNext())
{
    // process one row
}
```

## Exception behavior

Excecoes de materializacao preservam a semantica existente:

- profile ausente falha com `FluentMapConfigurationException`;
- construtor ou regra de dominio que falha durante materializacao continua
  sendo encapsulado pelo runtime materializer quando esse e o comportamento
  atual;
- excecoes de ADO.NET/Dapper durante execucao/leitura nao sao convertidas para
  excecoes de configuracao.

Quando uma excecao ocorre no meio da enumeracao, o iterator descarta o reader
antes de propagar a excecao.

## Generated materializer

O lookup generated continua por:

```text
EntityType + ProfileType opcional + ordered ColumnShape
```

No caminho unbuffered, o lookup ocorre uma vez por enumeracao, depois que o
reader esta aberto e antes do loop de linhas. Linhas subsequentes chamam apenas
o delegate escolhido.

Generated materializers continuam sendo otimizacao. Eles nao executam SQL, nao
abrem conexao, nao avancam reader e nao possuem recursos.

## Runtime fallback

Quando nao ha descriptor generated compativel, o FluentMap usa o mesmo
`NestedMaterializationPlan` de `QueryMapped*`.

O plano tambem e resolvido uma vez antes do loop de linhas. A cache existente
continua indexada por tipo, profile e shape ordenado de colunas.

## Allocation expectations

O caminho unbuffered evita a alocacao da `List<TEntity>` usada por
`QueryMapped<T>()`.

Ainda ha alocacoes esperadas para:

- o objeto enumerator;
- o `IDataReader`/command do provider;
- o array de nomes de colunas por enumeracao;
- o delegate/wrapper de materializer por enumeracao;
- as entidades e objetos aninhados/value objects materializados;
- caches runtime no primeiro fallback por shape.

Nao ha promessa publica de throughput ou latencia. Resultados locais ficam em
`.sdd/etapa-9/06-performance-results.md`.

## Profiles

Profiles sao selecionados por operacao:

```csharp
connection.QueryMappedUnbuffered<Customer, LegacyProfile>(sql);
```

Isso nao altera type maps globais do Dapper. Profile default e profile
especifico usam chaves de cache separadas.

## Transaction

O parametro `transaction` dos overloads convenientes e encaminhado para
`CommandDefinition`.

Como a enumeracao e lazy, a transacao precisa continuar ativa ate a enumeracao
terminar. Descartar ou concluir a transacao antes de enumerar e erro de uso do
chamador/provider.

## Ownership

O FluentMap e dono do reader que abre por `SqlMapper.ExecuteReader` durante a
enumeracao.

O FluentMap nao e dono:

- da conexao recebida;
- da transacao recebida;
- dos parametros;
- do SQL;
- do ciclo de vida externo que envolve a enumeracao.

## Escopo excluido

Este prompt nao implementa:

- async streaming;
- `IAsyncEnumerable<T>`;
- `ReadMappedUnbuffered<T>()` em `MappedGridReader`;
- suporte a `SqlMapper.GridReader` internals;
- multi-mapping por `splitOn`;
- SQL generation ou query builder.
