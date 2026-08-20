# Resource Lifetime Matrix

Prompt executado em 2026-07-28.

Esta matriz consolida o contrato de lifetime das APIs de materializacao
avancada da Etapa 9. Ela descreve ownership de recursos criados pelo
FluentMap/Dapper e a obrigacao do consumidor quando escolhe caminhos
unbuffered/streaming.

| API | Owns command | Owns reader | Connection requirement | Early termination | Cancellation |
| --- | ------------ | ----------- | ---------------------- | ----------------- | ------------ |
| `QueryMapped<TEntity>()` buffered | Dapper cria e gerencia o comando usado por `SqlMapper.ExecuteReader`; FluentMap nao expoe o command. | FluentMap descarta o `IDataReader` dentro do metodo antes de retornar a lista bufferizada. | Conexao pode estar aberta ou fechada; se o provider/Dapper abrir uma conexao fechada, o reader fecha ao ser descartado. | Nao aplicavel ao consumidor; todas as linhas sao lidas antes do retorno. | Sem cancellation especifica no caminho sincronico buffered; overload por `CommandDefinition` preserva o contrato recebido pelo Dapper. |
| `QueryMappedUnbuffered<TEntity>()` | Dapper cria o comando quando a enumeracao comeca; FluentMap nao expoe o command. | FluentMap e dono do `IDataReader` durante a enumeracao e o descarta ao fim, early break, dispose do enumerator ou excecao. | A conexao/transacao externas precisam permanecer validas ate a enumeracao terminar; conexao ja aberta permanece aberta. | Seguro quando o enumerator e descartado; `foreach` faz isso automaticamente em `break`/excecao. | Sem cancellation assincrona; o consumidor pode parar a enumeracao e descartar o enumerator. |
| `QueryMappedUnbufferedAsync<TEntity>()` async streaming | Dapper cria o comando em `ExecuteReaderAsync`; o token efetivo e copiado para o `CommandDefinition`. | FluentMap e dono do `DbDataReader` durante o async iterator e usa `DisposeAsync()` quando disponivel, com fallback para `Dispose()`. | Requer `DbConnection`; conexao/transacao externas precisam permanecer validas durante todo o `await foreach`. | Seguro quando o async enumerator e descartado; `await foreach` faz isso automaticamente em `break`/excecao. | Token propagado para `CommandDefinition`, para `ReadAsync(token)` e verificado entre linhas; `OperationCanceledException` nao e convertida. |
| `QueryMultipleMapped(...)` | Dapper cria o comando usado por `SqlMapper.ExecuteReader`; `MappedGridReader` controla o reader retornado, mas nao expoe command. | `MappedGridReader` e dono do `IDataReader` ate `Dispose()` ou ate excecao durante leitura/materializacao. | Consumo sequencial; a conexao deve continuar valida ate todos os result sets necessarios serem lidos ou o wrapper ser descartado. | Dispose do wrapper apos consumo parcial fecha o reader e impede leituras posteriores. | API sincronica sem cancellation propria; overload por `CommandDefinition` preserva configuracao suportada pelo Dapper. |
| `ReadMapped<TEntity>()` / `ReadMappedSingle<TEntity>()` | Usa o command/reader ja possuido pelo `MappedGridReader`; nao cria novo comando. | Le o result set atual de forma buffered e avanca com `NextResult()`; o reader permanece vivo entre grids ate o wrapper ser consumido ou descartado. | Deve ser chamado em ordem. Nao ha suporte a leitura concorrente nem fora de ordem dentro do mesmo `MappedGridReader`. | Nao ha enumerador ativo apos o retorno, porque o grid e bufferizado; parar entre result sets exige descartar o wrapper. | Nao ha cancellation propria; falhas de provider em `Read`/`NextResult` propagam sem wrapping como erro de mapping. |

## Limitacoes explicitas

- `MappedGridReader` nao suporta duas leituras concorrentes no mesmo wrapper.
- O projeto nao tenta suportar uso concorrente do mesmo `SqlMapper.GridReader`,
  porque esse contrato pertence ao Dapper e o reader interno nao e superficie
  publica para o FluentMap.
- Streaming de multiple result sets (`ReadMappedUnbuffered*`) ainda nao foi
  implementado; `QueryMultipleMapped` permanece buffered por result set.
- Cancellation real depende do provider ADO.NET. O FluentMap propaga tokens nos
  pontos async que aceitam token, mas nao transforma provider sincronico em I/O
  cancelavel.
