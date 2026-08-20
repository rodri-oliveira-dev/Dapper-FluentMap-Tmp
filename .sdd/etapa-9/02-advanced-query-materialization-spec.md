# Advanced Query Materialization Specification

Discovery executado para a Etapa 9 em 2026-07-28.

## Objetivos

- Suportar multiplos result sets no caminho opt-in de materializacao avancada do
  FluentMap.
- Permitir leitura por entidade e por mapping profile em result sets distintos.
- Preservar a ordem de precedencia atual: mapping explicito, convencao,
  comportamento default do Dapper.
- Preservar equivalencia entre generated materializer e runtime fallback por
  entidade, profile e shape ordenado de colunas.
- Introduzir caminhos unbuffered/streaming com ownership e lifetime explicitos.
- Suportar cancellation em APIs assincronas sem esconder o contrato real do
  provider.
- Continuar provider-independent e baseado em APIs publicas de Dapper/ADO.NET.

## Nao objetivos

A Etapa 9 nao deve implementar:

- SQL parsing;
- geracao de SQL;
- query builder;
- LINQ provider;
- CRUD;
- graph aggregation automatica;
- identity map;
- change tracking;
- eager loading;
- `Include`;
- repository abstractions;
- materializacao automatica por `splitOn`;
- abstracao ampla de conexao ou transacao.

Multiple Result Sets e Dapper Multi-Mapping sao conceitos diferentes.

- Multiple Result Sets: um comando retorna varios grids sequenciais e o
  consumidor chama `Read*` para cada grid.
- Dapper Multi-Mapping: uma unica linha de um unico result set e dividida em
  varios objetos por `splitOn`.

Multi-mapping por `splitOn` nao entra automaticamente nesta etapa.

## QueryMultiple

Dapper 2.1.79 oferece publicamente:

- `SqlMapper.QueryMultiple(IDbConnection, string, object, IDbTransaction, int?, CommandType?)`;
- `SqlMapper.QueryMultiple(IDbConnection, CommandDefinition)`;
- `SqlMapper.QueryMultipleAsync(...)`;
- `SqlMapper.GridReader.Read<T>(bool buffered = true)`;
- `SqlMapper.GridReader.ReadAsync<T>(bool buffered = true)`;
- `SqlMapper.GridReader.ReadUnbufferedAsync<T>()`;
- `SqlMapper.GridReader.Dispose()` e `DisposeAsync()`;
- propriedades publicas `IsConsumed` e `Command`.

`GridReader.Reader`, `ResultIndex`, `CancellationToken`, `OnBeforeGrid` e
`OnAfterGrid` existem no tipo, mas nao sao superficie publica consumivel pelo
FluentMap. O design nao deve depender desses membros por reflection.

A API preferencial para FluentMap e criar um wrapper proprio desde a execucao do
comando:

```csharp
using var multi = connection.QueryMultipleMapped(sql);

var customers = multi.ReadMapped<Customer>();
var orders = multi.ReadMapped<Order>();
```

O wrapper deve controlar `IDataReader.NextResult()` / `DbDataReader.NextResultAsync`
e usar o mesmo materializador de linhas de `QueryMapped*`.

## Multiple result sets

O wrapper deve consumir result sets sequencialmente. Cada chamada `ReadMapped*`
opera no result set atual e avanca para o proximo apenas depois que o grid atual
for consumido ou descartado.

Regras:

- leitura fora de ordem nao e suportada;
- leitura concorrente de dois grids no mesmo wrapper nao e suportada;
- tentar ler apos dispose deve falhar com `ObjectDisposedException`;
- tentar iniciar um novo grid enquanto um grid unbuffered esta em andamento
  deve falhar com excecao de uso invalido;
- grids escalares e dinamicos permanecem no caminho Dapper normal ou em APIs
  separadas, se explicitamente aprovadas.

## Profiles

Profiles continuam query-scoped. A selecao deve estar na chamada de leitura:

```csharp
var customers = multi.ReadMapped<Customer, LegacyProfile>();
```

Essa chamada nao deve alterar o type map global do Dapper. Cada result set pode
usar um profile diferente. Profile ausente deve continuar falhando com
`FluentMapConfigurationException`, como `QueryMapped<TEntity, TProfile>()`.

## Buffered materialization

O caminho buffered deve ser a primeira entrega funcional porque preserva o
contrato simples atual: o reader fica aberto apenas durante a leitura interna e
o metodo retorna uma colecao ja materializada.

Opcoes conceituais:

```csharp
IEnumerable<T> ReadMapped<T>();
IEnumerable<T> ReadMapped<T, TProfile>();
```

Apesar do retorno `IEnumerable<T>`, o comportamento inicial deve ser buffered
para alinhar com `QueryMapped*` atual e reduzir risco de lifetime.

## Unbuffered materialization

O caminho unbuffered sincrono deve ser separado e explicito:

```csharp
IEnumerable<T> QueryMappedUnbuffered<T>(...);
IEnumerable<T> ReadMappedUnbuffered<T>();
```

O enumerador deve manter reader/command/conexao em uso ate a enumeracao
terminar ou o enumerador ser descartado. Isso precisa ser documentado e coberto
por testes de dispose antecipado.

Para evitar armadilhas, uma API unbuffered nao deve ser confundida com o
`QueryMapped<T>` buffered atual.

## Async streaming

O caminho assincrono deve expor `IAsyncEnumerable<T>` apenas onde o target e as
dependencias permitirem assinatura publica estavel.

Dapper 2.1.79 tem `QueryUnbufferedAsync<T>` em `DbConnection` e
`GridReader.ReadUnbufferedAsync<T>()`, mas o FluentMap precisa materializar via
`IDataRecord`/`DbDataReader` proprio para aplicar nested/value-object/profile.

API conceitual:

```csharp
IAsyncEnumerable<T> QueryMappedUnbufferedAsync<T>(
    this DbConnection connection,
    CommandDefinition command);
```

Para `netstandard2.0`, introduzir `IAsyncEnumerable<T>` em API publica implica
dependencia e compatibilidade binaria com `Microsoft.Bcl.AsyncInterfaces`. Essa
decisao deve ser feita explicitamente antes da implementacao.

## Cancellation

`CommandDefinition` em Dapper 2.1.79 possui `CancellationToken`. APIs
assincronas novas devem aceitar `CommandDefinition` e overloads convenientes com
`CancellationToken`.

Regras:

- cancellation deve ser observada na abertura/execucao do comando quando o
  provider suportar;
- no loop de streaming assincrono, verificar cancellation entre linhas;
- cancellation deve propagar `OperationCanceledException` ou excecao do provider
  sem wrapping como erro de mapping;
- excecoes de mapping continuam seguindo a semantica atual do materializador.

## Connection lifetime

FluentMap nao deve assumir ownership de conexoes recebidas do usuario. O wrapper
deve preservar a regra do Dapper: se a conexao estava fechada e o FluentMap a
abriu para executar o comando, ela deve ser fechada ao encerrar o reader; se ja
estava aberta, permanece aberta.

Essa regra precisa de teste com conexao inicialmente aberta e inicialmente
fechada, se o provider de teste permitir.

## Reader lifetime

Buffered:

- o reader e lido ate o fim do grid dentro do metodo;
- o reader permanece vivo entre grids no wrapper;
- dispose do wrapper fecha o reader.

Unbuffered:

- o reader permanece vivo durante a enumeracao;
- dispose do enumerador deve consumir/descartar o grid atual conforme necessario
  para liberar recursos;
- o wrapper nao pode avancar para o proximo grid enquanto o enumerador atual
  estiver ativo.

## Command lifetime

Quando o FluentMap cria command/reader, o wrapper e dono do command. Dispose do
wrapper deve descartar command e reader, inclusive apos excecoes.

Quando uma API apenas compoe `SqlMapper.QueryMultiple`, o `GridReader` do
Dapper e dono do command/reader. Como essa alternativa nao permite acesso
publico adequado ao reader, ela nao e o caminho principal para `ReadMapped`.

## Exception semantics

- Argumentos nulos devem falhar com `ArgumentNullException`, seguindo o padrao
  atual.
- Profile ausente ou mapping invalido deve falhar com
  `FluentMapConfigurationException`.
- Dominio/construtor que falha durante materializacao continua sendo wrapped em
  `FluentMapConfigurationException` com inner exception, como hoje.
- Excecoes de ADO.NET/Dapper durante execucao, leitura, `NextResult` ou dispose
  nao devem ser convertidas para excecoes de configuracao.
- `Single`/`First` semantics, se adicionadas para multiple result sets, devem
  alinhar com os nomes do Dapper.

## Generated materializer interaction

Generated materializer deve continuar sendo lookup por:

```text
EntityType + ProfileType opcional + ordered ColumnShape
```

Cada result set tem seu proprio column shape. O wrapper deve calcular as colunas
do grid atual antes de iterar linhas e usar:

1. `TryGetGeneratedMaterializer`;
2. fallback runtime por `GetMaterializationPlan`.

Generated materializers nao devem executar SQL, abrir conexao, avancar grids ou
possuir recursos.

## Runtime fallback interaction

O runtime fallback deve ser o mesmo `NestedMaterializationPlan` usado por
`QueryMapped*`. A cache key ja inclui tipo, profile e colunas ordenadas, entao
multiplos result sets naturalmente produzem planos separados quando os shapes
diferem.

Se a Etapa 9 introduzir streaming, o plano deve ser criado uma vez por grid e
reutilizado por linha.

## Null semantics

Preservar as regras atuais:

- subarvore nested fica `null` quando todas as colunas do subtree sao `DBNull`;
- subarvore e criada quando qualquer coluna do subtree tem valor;
- Value Object nullable recebe `null` quando aplicavel;
- valores `DBNull` em tipos valor nao nullable seguem default/conversao atual;
- `Ignore()` exclui a coluna da materializacao;
- metadata de escrita (`ReadOnly`, `Computed`, `DatabaseDefaultOnInsert`,
  `ExcludeFromInsert`, `ExcludeFromUpdate`) e neutra para leitura.

## Provider independence

O design deve depender de `IDbConnection`, `DbConnection`, `IDataReader`,
`DbDataReader`, `IDataRecord`, `IDbCommand`, `CommandDefinition` e contratos
publicos do Dapper. Nao deve depender de SQLite, SQL Server, stored procedure
specifics ou parsing de SQL.

Testes podem usar SQLite quando ele suportar o comportamento necessario. Se o
provider nao suportar multiplos result sets em um unico comando, usar um reader
fake/ADO.NET controlado para testes unitarios de materializacao e um provider
real para lifetime quando disponivel.

## Performance expectations

- Buffered deve ter overhead pequeno sobre `QueryMapped*` por grid.
- Unbuffered deve evitar alocacao de `List<T>` e materializar por linha.
- Generated path deve manter a reducao de alocacao da Etapa 7.
- O primeiro grid pode pagar custo de shape/plan lookup; linhas seguintes nao
  devem recomputar plano.
- Benchmarks devem separar steady state, cold start, generated, runtime
  fallback, buffered e streaming.

Nao documentar promessa publica de latencia sem benchmark estavel.

## Backward compatibility

- Nenhuma API existente deve ser removida.
- `QueryMapped<T>` e `QueryMappedSingle<T>` continuam buffered.
- As variantes assincronas existentes hoje cobrem apenas profiles
  (`QueryMappedAsync<TEntity, TProfile>` e
  `QueryMappedSingleAsync<TEntity, TProfile>`). A Etapa 9 pode adicionar
  overloads default async como API aditiva, mas isso deve ser decidido
  separadamente.
- `connection.QueryMultiple(...).Read<T>()` continua sendo Dapper normal.
- Profiles continuam nao alterando type map global.
- Dommel nao entra no escopo salvo impacto comprovado no core.

## Native AOT / trimming considerations

`QueryMapped*` atual permanece anotado com `RequiresUnreferencedCode` e
`RequiresDynamicCode`, mesmo com generated materializers, porque pode cair para
runtime fallback.

As novas APIs de materializacao avancada devem herdar essa postura ate existir
um contrato que garanta "generated-only" sem fallback. Opcoes futuras:

- APIs normais anotadas como trimming/dynamic-code sensitive;
- API generated-only que falha se nao houver descriptor gerado para o shape;
- diagnostico por shape antes da execucao, sem prometer AOT full.

Introduzir `IAsyncEnumerable<T>` em API publica `netstandard2.0` tambem deve
ser avaliado como mudanca de dependencia/compatibilidade.
