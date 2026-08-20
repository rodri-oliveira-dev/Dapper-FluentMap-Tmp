# Etapa 9 - Final Report

## Objetivo

Encerrar a Etapa 9 - Advanced Query Materialization com auditoria final da
implementacao, documentacao publica, validacao de testes, evidencia de
performance e registro claro de limites. A etapa adicionou materializacao
avancada para multiple result sets, streaming sincronico, async streaming,
cancellation e integracao com materializadores gerados, sem iniciar recursos da
Etapa 10.

## Implementado

- `QueryMultipleMapped(...)` como wrapper proprio para multiple result sets no
  caminho opt-in do FluentMap.
- `MappedGridReader` com `ReadMapped<TEntity>()`,
  `ReadMapped<TEntity, TProfile>()`, `ReadMappedSingle<TEntity>()` e
  `ReadMappedSingle<TEntity, TProfile>()`.
- Dispatch compartilhado por `MappedRowMaterializer`, tentando generated
  materializer antes do fallback runtime.
- `QueryMappedUnbuffered<TEntity>()` e
  `QueryMappedUnbuffered<TEntity, TProfile>()` para streaming sincronico lazy.
- `QueryMappedUnbufferedAsync<TEntity>()` e
  `QueryMappedUnbufferedAsync<TEntity, TProfile>()` para streaming assincrono
  lazy via `DbConnection` e `IAsyncEnumerable<TEntity>`.
- Cancellation em async streaming por `CommandDefinition`, token efetivo do
  enumerator e `DbDataReader.ReadAsync(...)`.
- Cobertura de regressao historica para issues #22 e #43.
- Benchmarks steady state para buffered, unbuffered, async unbuffered,
  generated/runtime fallback e `QueryMultiple`.

## Audit SDD

| Requirement | Implementation | Tests | Performance | Status |
| ----------- | -------------- | ----- | ----------- | ------ |
| Multiple result sets no caminho opt-in | `QueryMultipleMapped(...)` retorna `MappedGridReader` e usa `IDataReader.NextResult()` | `QueryMultipleMappedTests` | `DapperQueryMultipleBuffered`, `QueryMultipleMappedSimple`, `QueryMultipleMappedSimpleRuntimeFallback` | Completed |
| Leitura por entidade e profile em grids distintos | `ReadMapped<T>()` e `ReadMapped<T, TProfile>()` por result set | `ReadMappedShouldKeepDefaultAndProfileResultSetsIsolated`, profile generated tests | Coberto funcionalmente; sem benchmark de profile separado | Completed |
| Precedencia explicit mapping, convention, Dapper default | Runtime plan usa registry antes de `DefaultTypeMap`; conventions/naming policies cobertas | naming policy, convention e issue #22 regressions | Nao benchmarkado isoladamente | Completed |
| Generated antes de runtime fallback por shape ordenado | `MappedRowMaterializer.CreateMaterializer` chama `TryGetGeneratedMaterializer` antes de `GetMaterializationPlan` | generated/default/profile/fallback/equivalence tests | generated vs runtime fallback em buffered, unbuffered e QueryMultiple | Completed |
| Buffered materialization preservada | `QueryMapped*` e `ReadMapped*` retornam resultados ja materializados | `ReadMappedShouldReturnEmptyCollectionForEmptyResultSet`, equivalence tests | Buffered comparado contra Dapper | Completed |
| Streaming sincronico explicito | `QueryMappedUnbuffered*` retorna `IEnumerable<T>` lazy | `QueryMappedUnbufferedTests` | Dapper unbuffered vs FluentMap unbuffered generated/runtime | Completed |
| `ReadMappedUnbuffered<T>()` em multiple results | Nao implementado; `QueryMultipleMapped` permanece buffered por grid | Limitacao documentada em lifetime/final report | Nao aplicavel | Deferred |
| Async streaming por `IAsyncEnumerable<T>` | `QueryMappedUnbufferedAsync*` em `DbConnection` | `QueryMappedUnbufferedAsyncTests` | Dapper async unbuffered vs FluentMap async unbuffered | Completed |
| `QueryMultipleMappedAsync` | Nao implementado; requer design proprio de async multiple result lifetime | Registrado como item adiado | Nao aplicavel | Deferred |
| Cancellation async | Token propagado para command, read async e loop | cancellation before/during/partial tests | Nao benchmarkado; provider-dependent | Completed |
| Connection lifetime | Dispose de reader preserva conexao inicialmente aberta/fechada conforme Dapper/provider | connection state tests em QueryMultiple, sync/async streaming | Nao benchmarkado diretamente | Completed |
| Reader lifetime e early termination | Readers descartados ao fim, early break, dispose, cancellation e excecao | disposal/early break/exception tests | Allocations de unbuffered indicam ausencia de `List<T>` do FluentMap | Completed |
| Command lifetime | Comando e criado pelo Dapper; FluentMap possui o reader retornado e libera recursos por dispose do reader | Coberto indiretamente por lifetime/transaction tests | Nao aplicavel | Partial |
| Exception semantics | Argument null, disposed, no remaining result sets e mapping exceptions preservados | exception/dispose/profile/mapping tests | Nao aplicavel | Completed |
| Null semantics | Nested subtree e Value Object nullable preservam semantica runtime existente | nested null tests, representative data type tests | Nao benchmarkado isoladamente | Completed |
| Provider independence | Producao usa `IDbConnection`, `DbConnection`, `IDataReader`, `DbDataReader`, `CommandDefinition` e Dapper publico | `DataTableReader` + SQLite provider tests | SQLite em memoria | Completed |
| SQL Server/PostgreSQL provider certification | Nao havia infraestrutura instalada; nao foram adicionados servicos externos | Documentado como limite | Nao aplicavel | Deferred |
| Dapper multi-mapping por `splitOn` | Explicitamente fora de escopo | Limitacoes e README | Nao aplicavel | Not applicable |
| Graph aggregation/identity map | Explicitamente fora de escopo | Limitacoes e README | Nao aplicavel | Not applicable |
| Native AOT seguro para `QueryMapped*` | APIs continuam anotadas porque fallback runtime pode ocorrer | Build/trim context herdado da Etapa 7 | Nao aplicavel | Partial |

Nao foram encontradas divergencias produtivas que exigissem redesign no
fechamento. A diferenca mais importante e a de command lifetime: a
implementacao usa `SqlMapper.ExecuteReader` e o FluentMap controla o reader
retornado, enquanto o command fica encapsulado pelo comportamento publico do
Dapper/provider. Isso e aceitavel para a etapa, mas deve permanecer documentado
como ownership indireto.

## API Review

APIs publicas adicionadas:

- `MappedGridReader`;
- `MappedGridReader.IsConsumed`;
- `MappedGridReader.ReadMapped<TEntity>()`;
- `MappedGridReader.ReadMapped<TEntity, TProfile>()`;
- `MappedGridReader.ReadMappedSingle<TEntity>()`;
- `MappedGridReader.ReadMappedSingle<TEntity, TProfile>()`;
- `QueryMappedExtensions.QueryMultipleMapped(...)`;
- `QueryMappedExtensions.QueryMappedUnbuffered<TEntity>(...)`;
- `QueryMappedExtensions.QueryMappedUnbuffered<TEntity, TProfile>(...)`;
- `QueryMappedExtensions.QueryMappedUnbufferedAsync<TEntity>(...)`;
- `QueryMappedExtensions.QueryMappedUnbufferedAsync<TEntity, TProfile>(...)`.

Revisao final:

- Naming esta alinhado ao vocabulario Dapper (`QueryMultiple`) e ao contrato de
  lifetime (`Unbuffered`).
- Overloads por `CommandDefinition` sao justificaveis para parametros,
  transacao, timeout, command type, flags e cancellation.
- `DbConnection` no async e consistente com a necessidade real de
  `DbDataReader.ReadAsync(...)`.
- `ReadMappedSingleOrDefault*` nao foi adicionado porque nao ha equivalente
  `QueryMappedSingleOrDefault*` no projeto.
- Nao ha streaming de grids em `MappedGridReader`; esse e um item adiado, nao
  um comportamento escondido.

Nenhuma correcao pequena de API foi necessaria no prompt final.

## QueryMultiple

`QueryMultipleMapped(...)` cria um `MappedGridReader` usando API publica do
Dapper e consome grids sequencialmente. Cada chamada `ReadMapped*` captura o
shape do grid atual, resolve o materializador e bufferiza o grid antes de
avancar para o proximo result set.

Coberto por testes:

- multiplos result sets;
- tipos diferentes por grid;
- profiles;
- generated materializer;
- runtime fallback;
- nested objects;
- immutable objects;
- Value Objects;
- empty result sets;
- dispose antes/depois de consumo;
- leitura apos dispose;
- leitura apos ultimo result set;
- excecoes de materializacao;
- parametros, transacao e lifetime de conexao.

## ReadMapped

`ReadMapped<T>()` e `ReadMapped<T, TProfile>()` sao buffered, apesar de
retornarem `IEnumerable<T>`. `ReadMappedSingle*` aplica semantica de
`Single()` depois de o grid atual ser materializado e avancado.

Nao ha suporte a leitura concorrente ou fora de ordem dentro do mesmo
`MappedGridReader`.

## Mapping Profiles

Profiles sao selecionados por operacao:

- `QueryMapped<TEntity, TProfile>()`;
- `ReadMapped<TEntity, TProfile>()`;
- `QueryMappedUnbuffered<TEntity, TProfile>()`;
- `QueryMappedUnbufferedAsync<TEntity, TProfile>()`.

Eles nao substituem o type map global do Dapper e possuem chaves separadas de
cache/materializer por entity + profile + shape.

## Unbuffered Materialization

`QueryMappedUnbuffered*` e lazy: a chamada publica valida argumentos, mas o
comando e executado apenas quando a enumeracao comeca. O reader permanece
aberto durante a enumeracao e e descartado ao final, em early break, em dispose
explicito do enumerator ou em excecao.

As allocations medidas mostram a reducao esperada pela ausencia de uma
`List<TEntity>` interna do FluentMap no shape simple:

- FluentMap buffered generated: `261.15 KB`;
- FluentMap unbuffered generated: `245.17 KB`;
- FluentMap buffered runtime fallback: `361.58 KB`;
- FluentMap unbuffered runtime fallback: `345.48 KB`.

## Async Streaming

`QueryMappedUnbufferedAsync*` retorna `IAsyncEnumerable<TEntity>` e exige
`DbConnection`. A execucao ocorre no primeiro `MoveNextAsync()`.

O caminho async:

- usa `SqlMapper.ExecuteReaderAsync`;
- resolve o materializer uma vez por shape;
- chama `DbDataReader.ReadAsync(cancellationToken)` por linha;
- materializa a linha de forma sincrona apos a leitura;
- descarta o reader em `finally`, usando `DisposeAsync()` quando disponivel.

## Generated Materialization Integration

Todos os caminhos adicionados usam o mesmo dispatch:

```text
entity + profile opcional + ordered column shape
    -> generated materializer compativel
    -> runtime NestedMaterializationPlan fallback
```

Generated materializers sao otimizacao, nao requisito funcional. Shapes
ausentes, reordenados, dinamicos ou nao suportados continuam usando fallback
runtime.

## Resource Lifetime

O FluentMap nao assume ownership da conexao recebida. Se o provider/Dapper abre
uma conexao fechada para o reader, o dispose do reader fecha essa conexao; se a
conexao ja estava aberta, ela permanece aberta.

Streaming mantem reader/command/provider resources vivos durante a enumeracao.
O consumidor deve descartar enumeradores quando interromper consumo parcial;
`foreach` e `await foreach` fazem isso nos casos normais.

## Cancellation

Cancellation e parte do contrato apenas nas APIs async streaming. O token e
propagado para:

- `CommandDefinition`;
- `DbDataReader.ReadAsync(token)`;
- verificacoes explicitas entre linhas.

`OperationCanceledException` nao e convertida em erro de mapping. Suporte real
a cancelamento continua dependente do provider.

## Historical Issues

| Issue | Final status | Evidencia |
| --- | --- | --- |
| #22 conventions em multiple results | Regression covered; Resolved by implementation no caminho opt-in | `MappedConventionShouldApplyToTypedReadFromMultipleResults` cobre convencoes por entidade em multiplos result sets via `QueryMultipleMapped(...).ReadMapped*`. |
| #43 explicit map em result set posterior | Regression covered; Resolved by implementation no caminho opt-in; Already resolved previously no caminho Dapper historico conforme upstream | `ExplicitMapShouldApplyToLaterTypedReadFromMultipleResults` cobre terceiro grid com colunas equivalentes ao relato historico. |

O caminho Dapper puro `connection.QueryMultiple(...).Read<T>()` permanece
responsabilidade do Dapper/type map global. A Etapa 9 resolveu a lacuna para
materializacao avancada opt-in do FluentMap.

## Performance

Benchmark final local (`ShortRun`, 1000 linhas por operacao, SQLite em memoria,
.NET 10.0.10, Dapper 2.1.79):

| Scenario | Allocated |
| --- | ---: |
| Dapper buffered | 283.17 KB |
| FluentMap buffered generated | 261.15 KB |
| FluentMap buffered runtime fallback | 361.58 KB |
| Dapper unbuffered | 266.96 KB |
| FluentMap unbuffered generated | 245.17 KB |
| FluentMap unbuffered runtime fallback | 345.48 KB |
| Dapper async unbuffered | 267.27 KB |
| FluentMap async unbuffered generated | 245.59 KB |
| FluentMap async unbuffered runtime fallback | 345.89 KB |
| Dapper QueryMultiple buffered | 284.18 KB |
| FluentMap QueryMultiple generated | 263.77 KB |
| FluentMap QueryMultiple runtime fallback | 363.07 KB |

Interpretacao:

- A evidencia mais estavel e de allocation, nao de throughput.
- Streaming evita a lista interna do FluentMap e materializa linha a linha.
- Generated path preserva a reducao de alocacao observada na Etapa 7.
- Tempo local em `ShortRun` e ruidoso demais para claims publicos.

## Provider Coverage

Cobertura executada:

- Provider-independent: `DataTableReader` para grids deterministicos e multiple
  result sets.
- SQLite: `Microsoft.Data.Sqlite` para ADO.NET real, connection lifetime,
  transactions, sync/async readers e benchmarks.

Nao houve infraestrutura proporcional para SQL Server ou PostgreSQL. Nao foram
adicionados pacotes, containers, variaveis de ambiente ou testes dependentes de
servicos externos no fechamento.

## Backward Compatibility

- Nenhuma API publica existente foi removida.
- `QueryMapped<T>()` permaneceu buffered.
- APIs unbuffered receberam nomes explicitos em vez de alterar comportamento
  existente.
- `QueryMultipleMapped` e aditivo e nao altera `SqlMapper.GridReader`.
- Profiles continuam query-scoped e nao alteram type maps globais.
- Dommel nao foi alterado nesta etapa.

## Native AOT / Trimming Considerations

As APIs `QueryMapped*`, `ReadMapped*`, `QueryMultipleMapped` e
`QueryMappedUnbuffered*` continuam anotadas com warnings de trimming/dynamic
code porque podem cair para fallback runtime baseado em reflection/dynamic code.

Registro explicito e registro gerado seguem sendo os caminhos preferenciais
para apps com trimming. A Etapa 9 nao declara `QueryMapped*` como fully Native
AOT-safe.

## Known Limitations

- Result sets em `MappedGridReader` sao consumidos sequencialmente.
- Nao ha leitura concorrente no mesmo `MappedGridReader`.
- Streaming mantem reader aberto ate fim, early termination ou dispose.
- A mesma conexao nao deve ser usada concorrentemente enquanto reader estiver
  ativo, salvo suporte explicito do provider.
- `QueryMultipleMapped` nao e Dapper multi-mapping por `splitOn`.
- Graph aggregation, identity map e automatic join grouping nao fazem parte do
  FluentMap.
- APIs podem continuar usando runtime fallback mesmo quando generated
  materializers existem para outros shapes.
- `ReadMappedUnbuffered*` e `QueryMultipleMappedAsync` foram adiados.
- SQL Server/PostgreSQL nao foram certificados nesta etapa.

## Technical Debt

- Formalizar API/binary compatibility checks antes de release maior.
- Avaliar diagnostics por query/column shape sem acoplar `Explain<T>()` a SQL.
- Reavaliar ownership visivel de command se uma API async multiple result set
  for criada.
- Criar matriz provider-specific apenas quando houver infraestrutura de CI ou
  demanda real.
- Investigar caminho generated-only/AOT-safe em etapa propria.

## Deferred Items

- `QueryMultipleMappedAsync`.
- `ReadMappedUnbuffered<T>()` / streaming por result set dentro de
  `MappedGridReader`.
- Dapper multi-mapping por `splitOn`.
- Graph aggregation.
- Property converters.
- DI/configuration instances/scoped configuration.
- SQL generation, CRUD, LINQ e repository.
- Provider certification ampla para SQL Server/PostgreSQL.

## Recommendations for Etapa 10

- Nao misturar converters/DI/configuration instances com hardening adicional de
  QueryMultiple.
- Se houver demanda por `QueryMultipleMappedAsync`, projetar lifetime,
  cancellation e async disposal antes de codificar.
- Se houver demanda por provider certification, criar harness opt-in e CI
  antes de adicionar dependencias permanentes.
- Antes de ampliar generated-only/AOT, definir contrato que evite fallback
  runtime ou documente explicitamente quando ele pode ocorrer.
- Manter o core focado em materializacao/mapping; nao iniciar CRUD, SQL
  generation ou graph aggregation por acidente.

## Validation

Executado em 2026-07-28:

```bash
dotnet restore ./Dapper.FluentMap.sln
dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build
dotnet test ./test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj --configuration Release --filter FullyQualifiedName~QueryMultipleMappedTests
dotnet test ./test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj --configuration Release --filter FullyQualifiedName~QueryMappedUnbufferedTests
dotnet test ./test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj --configuration Release --filter FullyQualifiedName~QueryMappedUnbufferedAsyncTests
dotnet test ./test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj --configuration Release --filter FullyQualifiedName~AdvancedQueryHardeningTests
dotnet run --project ./benchmarks/Dapper.FluentMap.Benchmarks/Dapper.FluentMap.Benchmarks.csproj --configuration Release --no-build -- --filter *MaterializationSteadyStateBenchmarks*
```

Resultados:

- Restore: sucesso.
- Build Release: sucesso, 0 warnings, 0 errors.
- Solution tests: sucesso, 357 testes aprovados.
- `QueryMultipleMappedTests`: sucesso, 25 testes aprovados.
- `QueryMappedUnbufferedTests`: sucesso, 14 testes aprovados.
- `QueryMappedUnbufferedAsyncTests`: sucesso, 15 testes aprovados.
- `AdvancedQueryHardeningTests`: sucesso, 5 testes aprovados.
- Benchmarks steady state: sucesso, 18 cenarios executados.

Observacao: duas primeiras tentativas de testes filtrados em paralelo falharam
com `CS2012` porque processos de build competiram pelo mesmo assembly em
`obj/Release`. As mesmas suites foram reexecutadas sequencialmente e passaram;
nao houve falha de teste.
