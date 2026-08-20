# Async Streaming Materialization

Prompt executado em 2026-07-28.

## API

APIs publicas adicionadas ao core:

```csharp
IAsyncEnumerable<TEntity> QueryMappedUnbufferedAsync<TEntity>(
    this DbConnection connection,
    string sql,
    CancellationToken cancellationToken)
    where TEntity : class;

IAsyncEnumerable<TEntity> QueryMappedUnbufferedAsync<TEntity>(
    this DbConnection connection,
    string sql,
    object param = null,
    IDbTransaction transaction = null,
    int? commandTimeout = null,
    CommandType? commandType = null,
    CancellationToken cancellationToken = default)
    where TEntity : class;

IAsyncEnumerable<TEntity> QueryMappedUnbufferedAsync<TEntity, TProfile>(
    this DbConnection connection,
    string sql,
    CancellationToken cancellationToken)
    where TEntity : class
    where TProfile : IMappingProfile;

IAsyncEnumerable<TEntity> QueryMappedUnbufferedAsync<TEntity, TProfile>(
    this DbConnection connection,
    string sql,
    object param = null,
    IDbTransaction transaction = null,
    int? commandTimeout = null,
    CommandType? commandType = null,
    CancellationToken cancellationToken = default)
    where TEntity : class
    where TProfile : IMappingProfile;

IAsyncEnumerable<TEntity> QueryMappedUnbufferedAsync<TEntity>(
    this DbConnection connection,
    CommandDefinition command)
    where TEntity : class;

IAsyncEnumerable<TEntity> QueryMappedUnbufferedAsync<TEntity, TProfile>(
    this DbConnection connection,
    CommandDefinition command)
    where TEntity : class
    where TProfile : IMappingProfile;
```

O receiver e `DbConnection`, nao `IDbConnection`, porque o contrato real de
streaming assincrono precisa de `DbDataReader.ReadAsync(...)`. O caminho
sincrono `QueryMappedUnbuffered*` permanece em `IDbConnection`.

O pacote principal continua em `netstandard2.0`, mas agora fixa
`LangVersion` em `8.0` e declara dependencia direta de
`Microsoft.Bcl.AsyncInterfaces` porque `IAsyncEnumerable<T>` passou a fazer
parte da superficie publica.

## IAsyncEnumerable

`QueryMappedUnbufferedAsync*` retorna uma sequencia lazy. O comando nao e
executado quando o metodo publico e chamado nem quando `GetAsyncEnumerator()` e
obtido. A execucao ocorre no primeiro `MoveNextAsync()`.

Cada nova enumeracao cria um novo reader e executa o comando novamente.

## Cancellation

Cancellation e propagada por tres pontos:

- o `CancellationToken` dos overloads convenientes e gravado no
  `CommandDefinition`;
- o token efetivo do async enumerator e aplicado ao `CommandDefinition` antes
  de chamar `SqlMapper.ExecuteReaderAsync`;
- o loop chama `ThrowIfCancellationRequested()` e passa o token a
  `DbDataReader.ReadAsync(cancellationToken)`.

O parametro interno do async iterator usa `[EnumeratorCancellation]`. Assim,
quando o usuario combina um token no metodo e outro via `await foreach` /
`WithCancellation`, o compilador pode produzir o token efetivo esperado para a
enumeracao.

`OperationCanceledException` nao e capturada nem convertida em erro de
configuracao.

Testes cobrem:

- cancellation antes da execucao;
- cancellation durante a enumeracao;
- cancellation apos enumeracao parcial, seguida de dispose do enumerator.

## Reader lifetime

O `DbDataReader` fica aberto durante a enumeracao assincrona e e descartado em
`finally` quando:

- o result set termina;
- o consumidor para cedo;
- o enumerator e descartado explicitamente;
- cancellation interrompe o loop;
- materializacao ou leitura falha.

Quando o reader implementa `IAsyncDisposable`, o FluentMap chama
`DisposeAsync()`. Caso contrario, chama `Dispose()` como fallback compativel com
`netstandard2.0`.

## Connection lifetime

O FluentMap nao assume ownership da conexao recebida.

Regra preservada:

- conexao inicialmente fechada: Dapper/provider abre no primeiro
  `MoveNextAsync()` e o dispose do reader fecha ao final, early break,
  cancellation, dispose explicito ou excecao;
- conexao inicialmente aberta: permanece aberta depois da enumeracao ou dispose
  do enumerator.

O usuario precisa manter a conexao e a transacao externas validas durante toda
a enumeracao.

## Command lifetime

O comando e criado e gerenciado pelo caminho publico do Dapper usado por
`SqlMapper.ExecuteReaderAsync`. O FluentMap e dono do reader retornado e fecha o
reader para liberar command e recursos auxiliares do provider.

Os overloads por `CommandDefinition` preservam command text, parametros,
transacao, timeout, command type e flags. O token efetivo da enumeracao e
copiado para um novo `CommandDefinition` antes da execucao, para que o provider
receba o token correto.

## Async disposal

Nao ha `.Result`, `.Wait()` ou bloqueio equivalente no caminho produtivo.

O dispose do reader ocorre por `await DisposeReaderAsync(reader)`. A rotina usa
`IAsyncDisposable.DisposeAsync()` quando tecnicamente disponivel e recai para
`Dispose()` apenas para readers que nao expoem async disposal.

## Exception semantics

- `connection == null` falha com `ArgumentNullException` na chamada publica.
- `sql == null` falha com `ArgumentNullException` na chamada publica.
- Profile ausente preserva `FluentMapConfigurationException`.
- Excecoes de dominio/construtor seguem o comportamento atual do materializer
  runtime, incluindo wrapping em `FluentMapConfigurationException` quando
  aplicavel.
- Excecoes de ADO.NET/Dapper durante execute/read/dispose nao sao convertidas
  para excecoes de configuracao.
- Cancellation propaga `OperationCanceledException`.

Em todos os casos apos a abertura do reader, o `finally` descarta o reader antes
de a excecao sair para o consumidor.

## Generated materializer

Generated materializers continuam sendo sincronos por linha. A operacao async
fica concentrada em I/O:

```text
ExecuteReaderAsync
    -> capturar shape de colunas
    -> resolver materializer gerado ou runtime uma vez
    -> ReadAsync por linha
    -> materializer(record) sincrono
```

Nao foi criado contrato de materializer async porque a leitura do valor ja
ocorreu quando o delegate e chamado.

## Runtime materializer

O fallback runtime usa o mesmo `NestedMaterializationPlan` dos caminhos
buffered e unbuffered sincrono. O plano e resolvido uma vez por enumeracao,
depois que o reader esta aberto e o shape ordenado de colunas e conhecido.

A cache existente continua por:

```text
EntityType + ProfileType opcional + ordered ColumnShape
```

## Profiles

Profiles sao suportados pelas variantes:

```csharp
connection.QueryMappedUnbufferedAsync<Customer, LegacyProfile>(sql, cancellationToken);
```

Isso preserva a semantica dos caminhos buffered e unbuffered sincrono:
selecionar profile por operacao nao altera type maps globais do Dapper e usa
cache/materializers separados do mapping default.

## Providers

O contrato produtivo depende de:

- `DbConnection`;
- `DbDataReader`;
- `IDbTransaction`;
- `CommandDefinition`;
- contratos publicos de Dapper.

Nao ha dependencia de SQLite, SQL Server, stored procedures ou parsing de SQL.
Os testes usam SQLite porque ele fornece provider real para connection lifetime,
async reader e transacao em memoria.

Providers podem implementar async de forma internamente sincronica. O FluentMap
nao tenta mascarar isso; ele apenas usa os contratos async disponiveis e
propaga cancellation aos pontos que aceitam token.

## Performance expectations

O caminho async streaming evita a `List<TEntity>` do buffered e materializa uma
linha por `ReadAsync`. Ainda ha alocacoes esperadas para:

- async enumerator/state machine;
- reader/command do provider;
- shape de colunas por enumeracao;
- delegate/wrapper de materializer por enumeracao;
- entidades, nested objects e Value Objects;
- possivel linked cancellation token quando multiplos tokens sao combinados.

Benchmarks foram adicionados para steady state async unbuffered, focando em
overhead e alocacao por item. Resultados locais ficam em
`.sdd/etapa-9/06-performance-results.md`.
