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

## Repeticao no prompt 11.6

Comando executado:

```powershell
dotnet run --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release -- --filter "*MaterializationSteadyStateBenchmarks*QueryMappedSimple*" --job Dry
```

Ambiente reportado:

- Windows 11;
- .NET SDK 10.0.302;
- runtime .NET 10.0.10;
- BenchmarkDotNet 0.15.8.

Observacao: o benchmark atual ja contem os cenarios "after isolated runtime",
"legacy default runtime" e "isolated runtime". Um baseline executavel "before
isolated runtime" nao existe no estado atual do workspace sem voltar o codigo
historico; portanto a comparacao antes/depois usa os resultados registrados
no prompt 11.3 como referencia historica.

ShortRun, 1000 linhas em SQLite in-memory:

| Comparacao | Metodo | Mean | Allocated |
| --- | --- | ---: | ---: |
| legacy default runtime | `QueryMappedSimple` | 1.759 ms | 261.16 KB |
| isolated runtime | `RuntimeQueryMappedSimple` | 1.731 ms | 261.16 KB |
| legacy default runtime | `QueryMappedSimpleUnbuffered` | 1.786 ms | 245.20 KB |
| isolated runtime | `RuntimeQueryMappedSimpleUnbuffered` | 1.786 ms | 245.20 KB |
| legacy default runtime | `QueryMappedSimpleUnbufferedAsync` | 2.801 ms | 245.61 KB |
| isolated runtime | `RuntimeQueryMappedSimpleUnbufferedAsync` | 2.222 ms | 245.61 KB |
| legacy default runtime | `QueryMappedSimpleRuntimeFallback` | 1.859 ms | 361.58 KB |
| isolated runtime | `RuntimeQueryMappedSimpleRuntimeFallback` | 1.839 ms | 361.58 KB |

Dry, uma iteracao cold/smoke:

| Comparacao | Metodo | Mean | Allocated |
| --- | --- | ---: | ---: |
| legacy default runtime | `QueryMappedSimple` | 3.109 ms | 362.83 KB |
| isolated runtime | `RuntimeQueryMappedSimple` | 2.722 ms | 362.83 KB |
| legacy default runtime | `QueryMappedSimpleUnbuffered` | 2.677 ms | 346.80 KB |
| isolated runtime | `RuntimeQueryMappedSimpleUnbuffered` | 2.600 ms | 346.80 KB |
| legacy default runtime | `QueryMappedSimpleUnbufferedAsync` | 2.777 ms | 347.30 KB |
| isolated runtime | `RuntimeQueryMappedSimpleUnbufferedAsync` | 3.356 ms | 347.30 KB |
| legacy default runtime | `QueryMappedSimpleRuntimeFallback` | 2.567 ms | 361.63 KB |
| isolated runtime | `RuntimeQueryMappedSimpleRuntimeFallback` | 2.695 ms | 361.63 KB |

Leitura do prompt 11.6:

- alocacao segue equivalente nos pares comparaveis;
- runtime isolado nao introduziu alocacao extra observavel no steady-state;
- diferencas de tempo em `ShortRun`/`Dry` continuam pequenas e com margem alta,
  entao devem ser tratadas como smoke/guardrail;
- benchmark completo segue recomendado antes de release para cold start,
  muitos runtimes ativos e cenarios com converters/generated materializers.

## Repeticao no prompt 11.7

Comando executado:

```powershell
dotnet run --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release -- --filter "*MaterializationSteadyStateBenchmarks*QueryMappedSimple*" --job Dry
```

Ambiente reportado:

- Windows 11;
- .NET SDK 10.0.302;
- runtime .NET 10.0.10;
- BenchmarkDotNet 0.15.8.

ShortRun, 1000 linhas em SQLite in-memory:

| Comparacao | Metodo | Mean | Allocated |
| --- | --- | ---: | ---: |
| legacy default runtime | `QueryMappedSimple` | 1.976 ms | 261.16 KB |
| isolated runtime | `RuntimeQueryMappedSimple` | 2.070 ms | 261.16 KB |
| legacy default runtime | `QueryMappedSimpleUnbuffered` | 2.295 ms | 245.20 KB |
| isolated runtime | `RuntimeQueryMappedSimpleUnbuffered` | 2.667 ms | 245.20 KB |
| legacy default runtime | `QueryMappedSimpleUnbufferedAsync` | 2.002 ms | 245.61 KB |
| isolated runtime | `RuntimeQueryMappedSimpleUnbufferedAsync` | 2.155 ms | 245.61 KB |
| legacy default runtime | `QueryMappedSimpleRuntimeFallback` | 1.965 ms | 361.58 KB |
| isolated runtime | `RuntimeQueryMappedSimpleRuntimeFallback` | 1.922 ms | 361.58 KB |

Leitura do prompt 11.7:

- alocacao permaneceu equivalente nos pares comparaveis entre bridge estatica
  e runtime isolado;
- nao ha evidencia de lookup significativo por linha introduzido pelo runtime,
  porque a resolucao segue ocorrendo na criacao do materializer por reader;
- os tempos continuam ruidosos por `ShortRun`/`Dry`, com aviso de iteracoes
  abaixo de 100 ms, portanto nao devem ser usados como claim formal de
  throughput.
