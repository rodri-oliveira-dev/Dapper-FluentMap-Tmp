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
