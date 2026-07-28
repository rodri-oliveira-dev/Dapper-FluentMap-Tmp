# QueryMultipleMapped Design

Prompt executado em 2026-07-28.

## Objetivo

Criar a primeira infraestrutura produtiva para multiplos result sets no caminho
opt-in de materializacao avancada do FluentMap.

API implementada:

```csharp
using var multi = connection.QueryMultipleMapped(sql, param, transaction);

var customers = multi.ReadMapped<Customer>();
var orders = multi.ReadMapped<Order>();
var legacy = multi.ReadMapped<Customer, LegacyProfile>();
```

## Wrapper escolhido

O wrapper publico escolhido foi `MappedGridReader`.

`QueryMultipleMapped(...)` abre um `IDataReader` por
`SqlMapper.ExecuteReader(connection, CommandDefinition)` e retorna
`MappedGridReader`. O FluentMap nao herda, encapsula nem inspeciona
`SqlMapper.GridReader`.

Essa escolha preserva o encaminhamento publico do Dapper para parametros,
transacao, timeout e command type, enquanto mantem o FluentMap responsavel
apenas pela materializacao dos grids.

## Ownership

`MappedGridReader` e dono do `IDataReader` que recebe.

O FluentMap nao e dono da `IDbConnection` recebida pelo usuario. A conexao segue
a regra operacional do Dapper/ADO.NET:

- se Dapper abriu uma conexao que estava fechada, o dispose do reader fecha a
  conexao;
- se a conexao ja estava aberta, o dispose do wrapper nao fecha a conexao.

`SqlMapper.ExecuteReader` continua responsavel por criar e configurar o comando
com os parametros do `CommandDefinition`.

## Disposal

`MappedGridReader.Dispose()` descarta o reader subjacente e marca o wrapper como
consumido.

Regras testadas:

- dispose antes de consumir impede leituras posteriores com
  `ObjectDisposedException`;
- dispose depois de consumo parcial fecha o reader e impede leitura dos grids
  restantes;
- dispose depois do consumo completo e idempotente;
- excecao durante materializacao descarta o reader antes de propagar a excecao.

## Semantica sincronica

Este incremento implementa apenas o caminho buffered sincronico:

- `ReadMapped<TEntity>()`;
- `ReadMapped<TEntity, TProfile>()`.

Cada chamada materializa todo o result set atual em memoria e so entao chama
`IDataReader.NextResult()` para posicionar o wrapper no proximo grid.

Mesmo retornando `IEnumerable<TEntity>`, o resultado ja esta bufferizado, igual
ao contrato atual de `QueryMapped*`.

## Semantica assincrona

Nenhuma API publica assincrona de `QueryMultipleMapped` foi adicionada neste
prompt.

Motivos:

- o core publica `netstandard2.0`;
- async disposal publico exigiria uma decisao explicita sobre
  `IAsyncDisposable`/`Microsoft.Bcl.AsyncInterfaces`;
- async streaming com `IAsyncEnumerable<T>` tambem altera a superficie publica
  e a matriz de compatibilidade.

Essa decisao preserva o escopo do Prompt 9.2 como infraestrutura buffered. Os
prompts posteriores de streaming/async devem decidir dependencias e assinaturas
publicas separadamente.

## Estado interno

`MappedGridReader` mantem:

- `_reader`: reader ADO.NET subjacente;
- `_disposed`: indica que o wrapper foi descartado;
- `_isConsumed`: indica que `NextResult()` retornou `false` ou que o wrapper
  foi descartado.

A propriedade publica `IsConsumed` retorna `true` quando todos os grids foram
consumidos ou quando o wrapper foi descartado, seguindo o padrao de descoberta
do `GridReader` do Dapper sem tentar replicar seus internals.

## Consumo sequencial dos result sets

O consumo e estritamente sequencial.

Fluxo de cada `ReadMapped*`:

1. valida que o wrapper nao foi descartado;
2. valida que ainda ha result set disponivel;
3. captura o shape de colunas do grid atual;
4. tenta materializador gerado registrado;
5. cai para `NestedMaterializationPlan` runtime quando necessario;
6. le todas as linhas do grid atual;
7. chama `NextResult()` uma vez para avancar o reader.

Nao ha suporte a leitura concorrente ou leitura fora de ordem neste incremento.
Como o caminho atual e buffered, nao existe enumerador ativo entre chamadas.

## Erros apos disposal

Chamadas a `ReadMapped*` depois de `Dispose()` falham com
`ObjectDisposedException`.

## Erros apos o ultimo result set

Chamadas a `ReadMapped*` depois de `NextResult()` retornar `false` falham com
`InvalidOperationException` informando que nao ha result sets restantes.

## Excecoes durante materializacao

Excecoes do materializador runtime/generated sao propagadas sem troca de tipo.
Quando o runtime materializer encapsula falha de dominio em
`FluentMapConfigurationException`, esse comportamento e preservado.

Ao capturar qualquer excecao durante materializacao ou `NextResult()`, o wrapper
descarta o reader antes de relancar a excecao para evitar vazamento de recurso.

## Connection lifetime

`QueryMultipleMapped` aceita `IDbConnection` e nao assume ownership da conexao.

Testes cobrem:

- conexao inicialmente fechada: aberta por Dapper e fechada no dispose do
  wrapper;
- conexao inicialmente aberta: permanece aberta apos dispose do wrapper.

## Transaction propagation

O parametro `transaction` do overload conveniente e encaminhado para
`CommandDefinition`.

O teste de infraestrutura usa SQLite em memoria com transacao ativa para
confirmar que a consulta e executada dentro da transacao recebida.

## Command type

O parametro `commandType` do overload conveniente e encaminhado para
`CommandDefinition`.

Nao foi adicionado teste provider-specific de stored procedure porque o core
permanece provider-independent e SQLite nao oferece esse contrato.

## Timeout

O parametro `commandTimeout` do overload conveniente e encaminhado para
`CommandDefinition`.

Nao ha teste deterministico de timeout neste prompt, para evitar flakiness e
dependencia de timing.

## Parameters

O parametro `param` e encaminhado para `CommandDefinition` e validado por teste
com query parametrizada em SQLite.

## Cancellation

Nao ha cancellation nova neste incremento porque a API implementada e
sincronica.

`CommandDefinition` ja permanece disponivel no overload principal; quando APIs
assincronas forem adicionadas, elas devem propagar `CancellationToken` pelo
`CommandDefinition` e observar cancellation nos loops de leitura aplicaveis.

## Materialization dispatch

O dispatch foi extraido para `MappedRowMaterializer`, compartilhado por
`QueryMapped*` e `MappedGridReader`.

Ordem preservada por grid:

1. `FluentMapper.Registry.TryGetGeneratedMaterializer(...)`;
2. `FluentMapper.Registry.GetMaterializationPlan(...)` como fallback runtime.

Cada result set recalcula seu proprio shape ordenado de colunas antes de
materializar linhas.

## Limitacoes deste incremento

- Sem unbuffered/streaming.
- Sem `IAsyncEnumerable<T>`.
- Sem `QueryMultipleMappedAsync`.
- Sem leitura de grids dinamicos ou escalares pelo wrapper FluentMap.
- Sem extensao sobre `SqlMapper.GridReader` existente.
- Sem suporte a Dapper multi-mapping por `splitOn`.
