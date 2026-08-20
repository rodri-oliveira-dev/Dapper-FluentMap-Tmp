# Advanced Query Regressions

Prompt executado em 2026-07-28.

Esta suite consolida cobertura permanente para os caminhos avancados de
materializacao sem transformar a Etapa 9 em matriz infinita de providers ou
tipos. Os testes diferenciam materializacao provider-independent de
comportamento ADO.NET provider-specific.

| Scenario | Test | Status |
| -------- | ---- | ------ |
| Convencoes em multiplos result sets mapeados, regressao historica da issue #22 | `MappedConventionShouldApplyToTypedReadFromMultipleResults` | Covered |
| Mapeamento explicito aplicado em result set posterior, regressao historica da issue #43 | `ExplicitMapShouldApplyToLaterTypedReadFromMultipleResults` | Covered |
| Multiple result sets sequenciais com tipos diferentes | `ReadMappedShouldReadSequentialResultSets` | Covered |
| `ReadMappedSingle` avanca o result set apos materializar exatamente uma linha | `ReadMappedSingleShouldMaterializeExactlyOneRowAndAdvanceResultSet` | Covered |
| Profiles isolados entre result sets da mesma entidade | `ReadMappedShouldKeepDefaultAndProfileResultSetsIsolated` | Covered |
| Naming policy e convention no result set atual | `ReadMappedShouldApplyNamingPolicyAndConventionInCurrentResultSet` | Covered |
| Objetos aninhados imutaveis e Value Objects em `ReadMapped` | `ReadMappedShouldMaterializeImmutableNestedObjectsAndValueObjects` | Covered |
| Null semantics em subarvores aninhadas | `ReadMappedShouldPreserveNestedNullSemantics` | Covered |
| Generated materializer em `ReadMapped` | `ReadMappedShouldUseGeneratedMaterializerWhenRegistered` | Covered |
| Generated materializers default/profile sem colisao | `ReadMappedShouldUseGeneratedProfileMaterializersWithoutCollisions` | Covered |
| Equivalencia generated/runtime para mesmo comportamento observavel | `ReadMappedGeneratedAndRuntimeShouldReturnEquivalentResultsForSameShape` | Covered |
| Tipos representativos em reader provider-independent | `ReadMappedShouldMaterializeRepresentativeDataTypesFromProviderIndependentReader` | Covered |
| Tipos representativos em provider SQLite real | `QueryMappedShouldMaterializeRepresentativeDataTypesWithSqliteProvider` | Covered |
| Streaming sincrono flat, nested, Value Object, profile, generated e fallback | `QueryMappedUnbufferedTests` | Covered |
| Streaming assincrono flat, nested, Value Object, profile, generated, fallback e cancellation | `QueryMappedUnbufferedAsyncTests` | Covered |
| Runtime materialization cache sob queries paralelas e conexoes independentes | `QueryMappedRuntimeFallbackShouldRemainStableAcrossParallelConnections` | Covered |
| Profile cache sob async streams paralelos e conexoes independentes | `QueryMappedUnbufferedAsyncShouldRemainStableAcrossParallelProfileStreams` | Covered |
| Generated materializers + runtime fallback em `QueryMultipleMapped` com readers independentes paralelos | `QueryMultipleMappedShouldUseGeneratedAndRuntimeMaterializersOnIndependentParallelReaders` | Covered |
| Generated lookup concorrente direto no registry | `GeneratedLookupShouldRemainStableUnderConcurrentReads` | Covered |
| Generated materializer usado por queries concorrentes | `QueryMappedGeneratedMaterializerShouldRemainStableUnderConcurrentQueries` | Covered |
| Connection lifetime quando `QueryMultipleMapped` abre conexao fechada | `QueryMultipleMappedShouldCloseConnectionItOpened` | Covered |
| Connection lifetime quando `QueryMultipleMapped` recebe conexao aberta | `QueryMultipleMappedShouldKeepOpenConnectionOpenAfterDispose` | Covered |
| Early break no streaming sincrono | `QueryMappedUnbufferedShouldCloseConnectionItOpenedAfterEarlyBreak` | Covered |
| Partial consumption no streaming assincrono | `QueryMappedUnbufferedAsyncShouldCloseConnectionItOpenedAfterPartialConsumption` | Covered |
| Cancellation antes e durante streaming assincrono | `QueryMappedUnbufferedAsyncShouldPropagateCancellationBeforeExecution`, `QueryMappedUnbufferedAsyncShouldPropagateCancellationDuringEnumeration` | Covered |

## Provider coverage

Infraestrutura existente:

- SQLite: coberto por `Microsoft.Data.Sqlite` em testes e benchmarks.
- Provider-independent: coberto por `DataTableReader` para multiple result
  sets deterministico, sem depender de comportamento SQL de um provider real.

Nao havia infraestrutura instalada para SQL Server ou PostgreSQL no projeto no
Prompt 9.6. Nao foram adicionados `Microsoft.Data.SqlClient`, `Npgsql`,
containers, variaveis de ambiente ou harness externo apenas para inflar a
quantidade de providers. Essa decisao preserva a proporcao do escopo e evita
testes que dependam de servicos externos por default.

## Data types

A cobertura representativa exercita:

- integer;
- string;
- nullable com valor e `DBNull`;
- `DateTime`;
- `Guid`;
- `decimal`;
- enum por valor numerico;
- Value Object construido por construtor publico.

## Concurrency

Cobertura adicionada:

- queries paralelas em conexoes SQLite independentes;
- cache runtime compartilhado por shape;
- profile cache em async streams paralelos;
- generated materializer e runtime fallback em `QueryMultipleMapped` com
  readers independentes;
- generated lookup concorrente ja existente no registry.

Limite documentado: nao ha suporte declarado para uso concorrente do mesmo
`MappedGridReader` ou do mesmo `SqlMapper.GridReader`.
