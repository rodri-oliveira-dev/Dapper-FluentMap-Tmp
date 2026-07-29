# Runtime Isolation Performance Impact

## Mudanca avaliada

O prompt 11.3 introduziu `FluentMapRuntime` como dono de caches derivados por
configuracao. O hot path de materializacao ficou assim:

```text
reader shape
    -> runtime generated lookup ou runtime plan cache
    -> delegate/plano reutilizado por linha
```

A indirecao de runtime acontece na criacao do materializer por reader. O lookup
pesado nao e repetido por linha.

## Caches e custo esperado

Cada runtime possui seus proprios caches:

- property map cache;
- materialization plan cache;
- generated materializer lookup.

Isso aumenta memoria proporcionalmente ao numero de runtimes/configuracoes
ativas, mas elimina colisao entre configuracoes. Para a configuracao usual
singleton, o custo permanece amortizado por runtime.

## Benchmark smoke

Comando executado:

```powershell
dotnet run --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release -- --filter "*MaterializationSteadyStateBenchmarks*QueryMappedSimple*" --job Dry
```

Observacao: por causa do `[ShortRunJob]` ja configurado no benchmark, o comando
executou cenarios `Dry` e `ShortRun`. Estes numeros sao smoke/guardrail, nao
baseline final de release.

Ambiente reportado pelo BenchmarkDotNet:

- Windows 11;
- .NET SDK 10.0.302;
- runtime .NET 10.0.10;
- BenchmarkDotNet 0.15.8.

## Resultados relevantes

ShortRun, 1000 linhas em SQLite in-memory:

| Metodo | Mean | Allocated |
| --- | ---: | ---: |
| `QueryMappedSimple` | 1.768 ms | 261.15 KB |
| `RuntimeQueryMappedSimple` | 1.861 ms | 261.16 KB |
| `QueryMappedSimpleUnbuffered` | 1.773 ms | 245.19 KB |
| `RuntimeQueryMappedSimpleUnbuffered` | 1.750 ms | 245.20 KB |
| `QueryMappedSimpleUnbufferedAsync` | 1.796 ms | 245.60 KB |
| `RuntimeQueryMappedSimpleUnbufferedAsync` | 1.928 ms | 245.61 KB |
| `QueryMappedSimpleRuntimeFallback` | 1.536 ms | 361.58 KB |
| `RuntimeQueryMappedSimpleRuntimeFallback` | 1.682 ms | 361.58 KB |

Dry, uma iteracao cold/smoke:

| Metodo | Mean | Allocated |
| --- | ---: | ---: |
| `QueryMappedSimple` | 3.030 ms | 362.82 KB |
| `RuntimeQueryMappedSimple` | 2.484 ms | 362.83 KB |
| `QueryMappedSimpleRuntimeFallback` | 2.391 ms | 361.63 KB |
| `RuntimeQueryMappedSimpleRuntimeFallback` | 2.575 ms | 361.63 KB |

## Leitura

O smoke nao indica alocacao extra relevante no steady-state. As diferencas de
tempo ficaram dentro de variacao esperada para `ShortRun` curto, e o caminho
runtime isolado manteve o mesmo perfil de alocacao dos helpers estaticos.

A alteracao importante para performance e estrutural: `FluentMapRuntime` e
resolvido antes da materializacao por linha, e o delegate/plano continua sendo
reutilizado para cada row.

## Riscos restantes

Benchmarks completos continuam recomendados antes de release, especialmente:

- todos os cenarios de `MaterializationSteadyStateBenchmarks`;
- cold start com configuracao grande;
- muitos runtimes ativos simultaneamente;
- generated materializers com converters.

O custo de memoria por runtime e intencional e deve ser documentado como troca
por isolamento correto entre configuracoes.
