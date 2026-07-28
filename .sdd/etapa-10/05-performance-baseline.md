# Etapa 10 - Performance Baseline

## Objetivo

Medir o custo inicial do runtime materializer apos adicionar read converters por
propriedade, comparando caminhos equivalentes que usam runtime fallback:

- sem converter;
- converter simples de propriedade;
- Dapper `TypeHandler<TProperty>`;
- property converter para Value Object escalar coexistindo com `TypeHandler`.

Os benchmarks usam 1000 linhas em SQLite in-memory e selecionam colunas em ordem
diferente do descriptor gerado para forcar o runtime fallback.

## Benchmark adicionado

Projeto:

```text
benchmarks/Dapper.FluentMap.Benchmarks/Dapper.FluentMap.Benchmarks.csproj
```

Metodos:

```text
MaterializationSteadyStateBenchmarks.QueryMappedRuntimeNoConverter
MaterializationSteadyStateBenchmarks.QueryMappedRuntimeSimpleConverter
MaterializationSteadyStateBenchmarks.QueryMappedRuntimeTypeHandler
MaterializationSteadyStateBenchmarks.QueryMappedRuntimePropertyConverter
```

## Execucao local

Comando executado em 2026-07-28:

```powershell
dotnet run --configuration Release --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj -- --filter "*MaterializationSteadyStateBenchmarks.QueryMappedRuntime*" --job Dry --warmupCount 1 --minIterationCount 1 --maxIterationCount 2
```

Ambiente reportado:

```text
BenchmarkDotNet v0.15.8
Windows 11 25H2
.NET SDK 10.0.302
.NET Runtime 10.0.10
Intel Core i5-1145G7
```

Resultado observado:

| Method | Mean | Allocated |
|---|---:|---:|
| QueryMappedRuntimePropertyConverter | 1.334 ms | 165.98 KB |
| QueryMappedRuntimeTypeHandler | 1.606 ms | 165.98 KB |
| QueryMappedRuntimeSimpleConverter | 1.676 ms | 189.43 KB |
| QueryMappedRuntimeNoConverter | 2.768 ms | 142.55 KB |

## Interpretacao

Esta execucao e uma baseline curta representativa, nao uma conclusao estatistica
final. O BenchmarkDotNet alertou que os tempos de iteracao ficaram abaixo de
100 ms, portanto uma comparacao formal deve aumentar operacoes/iteracoes.

O resultado confirma que o caminho novo compila e executa sem regressao obvia de
alocacao por linha. A diferenca favoravel dos converters nesta execucao parece
mais ligada ao shape simples e ao custo da conversao padrao sem converter do que
a uma otimizacao deliberada. Nao foi feita otimizacao prematura antes da medida.
