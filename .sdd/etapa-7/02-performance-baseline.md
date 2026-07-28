# Baseline de Performance

Status: BASELINE
Prompt: 7.2
Data: 2026-07-27

## Escopo

Baseline inicial antes de generated materializers. Os benchmarks medem o comportamento atual usando:

- Dapper puro com `Query<T>`;
- Dapper + FluentMap root mapping via APIs normais do Dapper;
- `QueryMapped<T>` simples;
- `QueryMapped<T>` com constructor mapping imutavel;
- `QueryMapped<T>` com nested object;
- `QueryMapped<T>` com Value Object.

Nenhuma implementacao produtiva foi alterada para melhorar resultados.

## Ambiente

- OS: Windows 11 `10.0.26200.8875/25H2/2025Update/HudsonValley2`
- CPU: 11th Gen Intel Core i5-1145G7 2.60GHz, 1 CPU, 8 logical cores, 4 physical cores
- .NET SDK: `10.0.302`
- Runtime: `.NET 10.0.10`, X64 RyuJIT
- GC: Concurrent Workstation
- BenchmarkDotNet: `0.15.8`
- Dapper: `2.1.79`
- Microsoft.Data.Sqlite: `10.0.10`
- SQLitePCLRaw.bundle_e_sqlite3: `2.1.12`

## Dataset

- SQLite em memoria.
- `BenchmarkRows` com 1000 linhas por operacao.
- Colunas: `Id`, `Name`, `Age`, `Balance`, `CreatedAt`, `City`, `PostalCode`, `Country`, `Cpf`, `Currency`.
- Tipos exercitados: `int`, `string`, `decimal`, `DateTime` e objetos compostos por construtor.
- Todos os benchmarks materializam completamente os 1000 resultados.

## Comandos Executados

Rodada steady state:

```bash
dotnet run --project benchmarks/Dapper.FluentMap.Benchmarks/Dapper.FluentMap.Benchmarks.csproj --configuration Release -- --filter *MaterializationSteadyStateBenchmarks*
```

Rodada cold start:

```bash
dotnet run --project benchmarks/Dapper.FluentMap.Benchmarks/Dapper.FluentMap.Benchmarks.csproj --configuration Release -- --filter *MaterializationColdStartBenchmarks*
```

Os artefatos do BenchmarkDotNet foram gravados em `.tmp/benchmarks/BenchmarkDotNet.Artifacts/`, caminho ignorado pelo repositorio.

## Resultados - Steady State

Job: `ShortRun`, `LaunchCount=1`, `WarmupCount=3`, `IterationCount=3`.

| Method | Mean | StdDev | Ratio | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| QueryMappedValueObject | 1.337 ms | 0.0216 ms | 0.90 | 140.6250 | 37.1094 | 587.7 KB | 2.08 |
| DapperWithFluentMapRootMapping | 1.478 ms | 0.1041 ms | 0.99 | 68.3594 | - | 283.3 KB | 1.00 |
| DapperPure | 1.503 ms | 0.1435 ms | 1.01 | 68.3594 | - | 283.17 KB | 1.00 |
| QueryMappedSimple | 1.547 ms | 0.1191 ms | 1.04 | 87.8906 | 19.5313 | 361.28 KB | 1.28 |
| QueryMappedNestedObject | 1.573 ms | 0.1261 ms | 1.05 | 91.7969 | 29.2969 | 376.86 KB | 1.33 |
| QueryMappedImmutableConstructor | 1.651 ms | 0.1207 ms | 1.11 | 103.5156 | 9.7656 | 423.78 KB | 1.50 |

### Leitura

- As diferencas de tempo nesta rodada curta nao devem ser tratadas como ranking forte. Os intervalos de erro sao grandes em relacao as medias.
- `DapperWithFluentMapRootMapping` ficou essencialmente alinhado com Dapper puro em tempo e alocacao para root mapping.
- `QueryMapped*` mostrou alocacao maior mesmo quando o tempo ficou proximo:
  - simple: cerca de 1.28x Dapper puro;
  - nested: cerca de 1.33x Dapper puro;
  - immutable constructor: cerca de 1.50x Dapper puro;
  - Value Object: cerca de 2.08x Dapper puro.
- Em termos aproximados por linha, Dapper puro/root ficaram perto de 290 B/linha, enquanto `QueryMappedValueObject` ficou perto de 602 B/linha.

## Resultados - Cold Start

Job: `RunStrategy=ColdStart`, `LaunchCount=8`, `WarmupCount=0`, `IterationCount=1`.

| Method | Mean | StdDev | Ratio | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: |
| QueryMappedNestedColdStart | 184.4 ms | 26.52 ms | 0.75 | 442.46 KB | 1.55 |
| FluentMapRootMappingColdStart | 203.5 ms | 46.85 ms | 0.82 | 364.78 KB | 1.28 |
| QueryMappedValueObjectColdStart | 226.1 ms | 48.94 ms | 0.92 | 644.65 KB | 2.25 |
| DapperPureColdStart | 250.9 ms | 37.64 ms | 1.02 | 285.95 KB | 1.00 |

### Leitura

- Cold start apresentou variancia alta e outliers, incluindo Dapper puro. Os tempos nao devem ser usados para afirmar que um caminho e mais rapido que outro.
- As allocations cold start reforcam a mesma direcao do steady state: `QueryMapped*` aloca mais, especialmente Value Object.
- O custo medido inclui inicializacao do provider SQLite, criacao/populacao da tabela em memoria, configuracao FluentMap quando aplicavel, primeira query Dapper e primeira criacao de plano runtime quando aplicavel.

## Hotspots e Hipoteses

Hotspots esperados para investigar nas etapas futuras:

- `NestedMaterializationPlan.Create(...)` criando `DefaultTypeMap`, resolvendo maps por coluna e selando arvore de materializacao.
- `Expression.Compile()` para factories, getters, setters e construtores.
- `object[]` por chamada de construtor em `ConstructorPlan.Create(...)`.
- Conversao escalar em `ConvertValue(...)`, incluindo `Convert.ChangeType` e defaults de value types.
- Alocacao adicional de subobjetos em nested/value object, que e funcionalmente necessaria mas pode ser reduzida no caminho gerado.
- Buffering de `QueryMapped*`, que hoje retorna uma lista materializada.

## Limitacoes

- SQLite em memoria mede tambem provider, SQL e reader; nao isola materializacao pura.
- `ShortRun` e adequado para baseline local rapida, mas nao substitui uma rodada longa para decisao final de performance.
- Cold start em BenchmarkDotNet mede processo frio e mostrou alta variancia nesta maquina.
- Resultados locais nao sao promessa publica de performance.
- O baseline nao inclui profiles, TypeHandlers escalares ou null-heavy datasets.
- O baseline nao compara com generated materializers, porque eles ainda nao foram implementados.

## Orcamento Inicial Refinado

Com base nesta rodada:

- Manter `Dapper + FluentMap root mapping` proximo de Dapper puro em tempo e alocacao para cenarios root simples.
- Tratar `QueryMappedSimple` steady state como teto inicial do fallback runtime para materializacao simples: aproximadamente `361 KB` por 1000 linhas nesta maquina.
- Para generated materializers futuros, buscar reducao mensuravel de alocacao em relacao ao fallback runtime:
  - nested: baseline `376.86 KB` por 1000 linhas;
  - immutable constructor: baseline `423.78 KB` por 1000 linhas;
  - Value Object: baseline `587.7 KB` por 1000 linhas.
- So transformar diferenca de tempo em requisito apos rodada mais longa e estatisticamente estavel.

## Repetir Apos Etapas Futuras

Repetir estes benchmarks:

- apos 7.4: `DapperPure`, `DapperWithFluentMapRootMapping`, `QueryMappedSimple` e cold root/simple;
- apos 7.5: `QueryMappedImmutableConstructor`, `QueryMappedNestedObject`, `QueryMappedValueObject` e seus equivalentes generated;
- apos 7.6: todos os steady state e cold start para validar lookup generated/fallback integrado.

## Apos Prompt 7.4

Prompt 7.4 adicionou materializers gerados para maps flat simples e alterou o benchmark steady state para registrar maps por `AddGeneratedMappings()`. Com isso, `QueryMappedSimple` e `QueryMappedImmutableConstructor` usam generated materializer quando a query retorna o shape canonico gerado. `QueryMappedNestedObject` e `QueryMappedValueObject` continuam no fallback runtime.

### Comandos Executados

Rodada steady state:

```bash
dotnet run --project ./benchmarks/Dapper.FluentMap.Benchmarks/Dapper.FluentMap.Benchmarks.csproj --configuration Release --no-build -- --filter *MaterializationSteadyStateBenchmarks*
```

Rodada cold start existente:

```bash
dotnet run --project ./benchmarks/Dapper.FluentMap.Benchmarks/Dapper.FluentMap.Benchmarks.csproj --configuration Release --no-build -- --filter *MaterializationColdStartBenchmarks*
```

Tambem foi tentado adicionar um benchmark cold dedicado para `QueryMapped` flat gerado. A tentativa nao foi mantida porque BenchmarkDotNet invoca o metodo mais de uma vez no mesmo processo para estatisticas extras, e o contrato publico atual nao expoe reset de generated materializers; a segunda chamada a `AddGeneratedMappings()` duplicava o descriptor. Nao foi criada API publica nova apenas para o benchmark.

### Resultados - Steady State

Job: `ShortRun`, `LaunchCount=1`, `WarmupCount=3`, `IterationCount=3`.

| Method | Mean | StdDev | Ratio | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| QueryMappedValueObject | 1.202 ms | 0.0732 ms | 0.89 | 140.6250 | 41.0156 | 587.84 KB | 2.08 |
| DapperPure | 1.355 ms | 0.0485 ms | 1.00 | 68.3594 | - | 283.17 KB | 1.00 |
| QueryMappedNestedObject | 1.422 ms | 0.1847 ms | 1.05 | 91.7969 | 29.2969 | 377 KB | 1.33 |
| DapperWithFluentMapRootMapping | 1.456 ms | 0.0464 ms | 1.08 | 68.3594 | - | 283.3 KB | 1.00 |
| QueryMappedImmutableConstructor | 1.610 ms | 0.0769 ms | 1.19 | 62.5000 | 11.7188 | 261.05 KB | 0.92 |
| QueryMappedSimple | 1.617 ms | 0.1008 ms | 1.19 | 62.5000 | 11.7188 | 261.12 KB | 0.92 |

### Leitura - Steady State

- `QueryMappedSimple` reduziu alocacao de aproximadamente `361 KB` para `261 KB` por 1000 linhas quando o generated materializer foi usado.
- `QueryMappedImmutableConstructor` reduziu alocacao de aproximadamente `424 KB` para `261 KB` por 1000 linhas.
- O tempo continua ruidoso em `ShortRun`; nao ha base estatistica para prometer ganho de tempo.
- `QueryMappedNestedObject` e `QueryMappedValueObject` ainda usam fallback runtime. As variacoes de tempo nesses cenarios devem ser tratadas como ruido da rodada, nao como efeito do prompt 7.4.

### Resultados - Cold Start

Job: `RunStrategy=ColdStart`, `LaunchCount=8`, `WarmupCount=0`, `IterationCount=1`.

| Method | Mean | StdDev | Ratio | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: |
| DapperPureColdStart | 183.5 ms | 27.79 ms | 1.02 | 285.95 KB | 1.00 |
| QueryMappedValueObjectColdStart | 251.8 ms | 25.75 ms | 1.40 | 645.09 KB | 2.26 |
| FluentMapRootMappingColdStart | 271.1 ms | 59.49 ms | 1.51 | 353.05 KB | 1.23 |
| QueryMappedNestedColdStart | 276.7 ms | 55.80 ms | 1.54 | 442.84 KB | 1.55 |

### Leitura - Cold Start

- Cold start continuou com variancia alta e outliers.
- A rodada cold valida que os cenarios existentes continuam executando apos a integracao do generator no projeto de benchmarks.
- Nao ha numero cold dedicado para generated flat neste prompt por causa da limitacao de reset publico descrita acima.

## Apos Prompt 7.5

Prompt 7.5 expandiu os materializers gerados para nested mutable objects, constructor-composed nested immutable objects e Value Objects por componentes. O benchmark steady state existente ja registra maps por `AddGeneratedMappings()`, entao `QueryMappedNestedObject` e `QueryMappedValueObject` passaram a usar generated materializer quando a query retorna o shape canonico gerado.

### Comando Executado

Rodada steady state:

```bash
dotnet run --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release --no-build -- --filter *MaterializationSteadyStateBenchmarks*
```

### Resultados - Steady State

Job: `ShortRun`, `LaunchCount=1`, `WarmupCount=3`, `IterationCount=3`.

| Method | Mean | StdDev | Ratio | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| DapperPure | 1.295 ms | 0.0535 ms | 1.00 | 68.3594 | - | 283.17 KB | 1.00 |
| QueryMappedValueObject | 1.392 ms | 0.0130 ms | 1.08 | 62.5000 | 19.5313 | 276.47 KB | 0.98 |
| DapperWithFluentMapRootMapping | 1.580 ms | 0.3355 ms | 1.22 | 68.3594 | - | 283.3 KB | 1.00 |
| QueryMappedNestedObject | 1.670 ms | 0.0571 ms | 1.29 | 70.3125 | 21.4844 | 292.44 KB | 1.03 |
| QueryMappedImmutableConstructor | 1.734 ms | 0.0812 ms | 1.34 | 62.5000 | 5.8594 | 261.05 KB | 0.92 |
| QueryMappedSimple | 1.754 ms | 0.1335 ms | 1.36 | 62.5000 | 5.8594 | 261.12 KB | 0.92 |

### Leitura - Steady State

- `QueryMappedNestedObject` reduziu alocacao de aproximadamente `377 KB` no Prompt 7.4 para `292.44 KB` por 1000 linhas quando o generated materializer foi usado.
- `QueryMappedValueObject` reduziu alocacao de aproximadamente `587.84 KB` no Prompt 7.4 para `276.47 KB` por 1000 linhas.
- O tempo continua ruidoso em `ShortRun`, especialmente nos cenarios com intervalos amplos; nao ha promessa publica de ganho de tempo.
- A rodada valida que nested e Value Object agora entram no caminho gerado para o shape canonico do benchmark.

### Limitacoes

- Nao foi executada rodada cold dedicada para generated complex. O benchmark cold existente registra maps manualmente por `AddMap(...)` e continua representando o fallback/runtime.
- Os resultados continuam locais e devem ser usados como indicio de alocacao, nao como contrato de performance.
