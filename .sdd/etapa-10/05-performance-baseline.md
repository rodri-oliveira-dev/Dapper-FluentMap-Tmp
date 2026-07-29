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

## Apos Prompt 10.4

O Prompt 10.4 adicionou materializacao gerada para read converters por tipo
estaticamente suportados. Foram executados benchmarks curtos em 2026-07-28,
mantendo `--job Dry`, `--warmupCount 1`, `--minIterationCount 1` e
`--maxIterationCount 2`.

Comando para converter/no-converter em shapes de duas colunas:

```powershell
dotnet run --configuration Release --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj -- --filter "*MaterializationSteadyStateBenchmarks.QueryMapped*Converter*" --job Dry --warmupCount 1 --minIterationCount 1 --maxIterationCount 2
```

Resultado observado:

| Method | Mean | Allocated |
|---|---:|---:|
| QueryMappedGeneratedSimpleConverter | 1.421 ms | 189.99 KB |
| QueryMappedRuntimePropertyConverter | 1.453 ms | 165.98 KB |
| QueryMappedRuntimeNoConverter | 1.579 ms | 142.55 KB |
| QueryMappedRuntimeSimpleConverter | 1.885 ms | 189.43 KB |
| QueryMappedGeneratedPropertyConverter | 2.036 ms | 166.55 KB |

Comando para comparar o par gerado/runtime sem converter no shape simples
historico:

```powershell
dotnet run --configuration Release --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj -- --filter "*MaterializationSteadyStateBenchmarks.QueryMappedSimple*" --job Dry --warmupCount 1 --minIterationCount 1 --maxIterationCount 2
```

Recorte relevante:

| Method | Mean | Allocated |
|---|---:|---:|
| QueryMappedSimpleRuntimeFallback | 3.100 ms | 361.63 KB |
| QueryMappedSimple | 3.270 ms | 362.82 KB |

Interpretacao:

- A execucao e smoke de performance, nao conclusao estatistica; o
  BenchmarkDotNet alertou que os tempos de iteracao ficaram abaixo de 100 ms.
- O generated converter remove fallback runtime quando o shape casa, compila e
  executa sem regressao obvia de alocacao.
- O resultado local ficou ruidoso: o converter simples gerado apareceu mais
  rapido que o runtime equivalente, enquanto o Value Object/property converter
  gerado apareceu mais lento nesta unica iteracao.
- A evidencia funcional mais importante do prompt continua sendo equivalencia
  `runtime == generated` e cache runtime zerado no caminho gerado.

## Apos Prompt 10.6

O Prompt 10.6 adicionou diagnostics, validacao runtime e testes de hardening
sem alterar o hot path de conversao. Foi executado benchmark curto em
2026-07-29 com o mesmo perfil Dry representativo:

```powershell
dotnet run --configuration Release --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj -- --filter "*MaterializationSteadyStateBenchmarks.QueryMapped*Converter*" --job Dry --warmupCount 1 --minIterationCount 1 --maxIterationCount 2
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
| QueryMappedRuntimePropertyConverter | 1.095 ms | 165.98 KB |
| QueryMappedRuntimeSimpleConverter | 1.190 ms | 189.43 KB |
| QueryMappedGeneratedPropertyConverter | 1.203 ms | 166.55 KB |
| QueryMappedGeneratedSimpleConverter | 1.295 ms | 189.99 KB |
| QueryMappedRuntimeNoConverter | 1.805 ms | 142.55 KB |

Interpretacao:

- A execucao continua sendo smoke de performance. BenchmarkDotNet alertou que
  todos os tempos de iteracao ficaram abaixo de 100 ms.
- A validacao e os analyzers novos atuam em configuracao/compilacao, fora do
  custo por linha do materializer.
- O custo fixo de converter permanece na criacao de metadata/plano ou no campo
  estatico gerado. O custo por linha segue sendo a chamada do converter e, se
  necessario, a conversao do valor bruto para `TDatabase`.
- As alocacoes ficaram alinhadas com a baseline anterior: generated converter
  adiciona diferenca pequena de descriptor/caminho gerado, e runtime converter
  nao introduziu nova alocacao observavel por linha nesta medicao curta.

## Resultados finais da Etapa 10

No Prompt 10.7 foram executados benchmarks representativos em 2026-07-29, com
o mesmo perfil curto usado nos prompts anteriores:

```powershell
dotnet run --configuration Release --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj -- --filter "*MaterializationSteadyStateBenchmarks.QueryMapped*Converter*" --job Dry --warmupCount 1 --minIterationCount 1 --maxIterationCount 2
```

Tambem foi executado um recorte separado para Dapper puro/default conversion:

```powershell
dotnet run --configuration Release --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj -- --filter "*MaterializationSteadyStateBenchmarks.DapperPure" --job Dry --warmupCount 1 --minIterationCount 1 --maxIterationCount 2
```

Ambiente reportado:

```text
BenchmarkDotNet v0.15.8
Windows 11 25H2
.NET SDK 10.0.302
.NET Runtime 10.0.10
Intel Core i5-1145G7
```

Resultados observados:

| Scenario | Method | Mean | Allocated | Overhead vs comparable no-converter |
|---|---|---:|---:|---:|
| Dapper/default conversion | `DapperPure` | 2.071 ms | 283.22 KB | Not comparable: 5-column Dapper baseline |
| FluentMap sem converter | `QueryMappedRuntimeNoConverter` | 1.536 ms | 142.55 KB | Baseline for 2-column runtime converter shapes |
| FluentMap runtime converter | `QueryMappedRuntimeSimpleConverter` | 2.036 ms | 189.43 KB | +0.500 ms / +46.88 KB |
| FluentMap generated converter | `QueryMappedGeneratedSimpleConverter` | 2.206 ms | 189.99 KB | +0.670 ms / +47.44 KB |
| FluentMap runtime property converter | `QueryMappedRuntimePropertyConverter` | 1.390 ms | 165.98 KB | -0.146 ms / +23.43 KB |
| FluentMap generated property converter | `QueryMappedGeneratedPropertyConverter` | 1.421 ms | 166.55 KB | -0.115 ms / +24.00 KB |

Interpretacao final:

- Esta continua sendo uma execucao smoke, nao uma amostra estatistica formal.
  BenchmarkDotNet alertou que todos os tempos de iteracao ficaram abaixo de
  100 ms.
- A comparacao Dapper/default conversion usa um shape de 5 colunas e serve como
  referencia geral do ambiente, nao como par semantico dos benchmarks de
  converter de 2 colunas.
- As alocacoes dos cenarios de converter permaneceram na mesma ordem das
  medicoes anteriores. O custo adicional esperado aparece como chamada do
  converter e, em alguns cenarios, conversao do valor bruto para `TDatabase`.
- O resultado de tempo local permanece ruidoso: nao ha evidencia suficiente
  para afirmar vantagem de runtime ou generated converter em throughput.
  A conclusao suportada e que a Etapa 10 nao introduziu regressao obvia de
  alocacao ou falha funcional nos cenarios representativos.
