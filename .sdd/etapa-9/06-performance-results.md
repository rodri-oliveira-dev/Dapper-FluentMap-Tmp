# Etapa 9 Performance Results

Prompt executado em 2026-07-28.

## Baseline herdada da Etapa 7

Rodada final da Etapa 7, `ShortRun`, 1000 linhas por operacao:

| Scenario | Allocated |
| --- | ---: |
| Dapper puro buffered | 283.17 KB |
| Dapper + FluentMap root mapping buffered | 283.3 KB |
| QueryMapped simple generated buffered | 261.12 KB |
| QueryMapped simple runtime fallback buffered | 361.48 KB |
| QueryMapped nested generated buffered | 292.44 KB |
| QueryMapped nested runtime fallback buffered | 377.06 KB |
| QueryMapped Value Object generated buffered | 276.47 KB |
| QueryMapped Value Object runtime fallback buffered | 587.9 KB |

Leitura da baseline: o ganho mais estavel da Etapa 7 estava em alocacao no
caminho generated. O tempo local era ruidoso e nao foi tratado como promessa de
latencia.

## Benchmarks adicionados no Prompt 9.4

O benchmark steady state passou a comparar no mesmo dataset:

- Dapper buffered;
- Dapper unbuffered (`buffered: false`);
- FluentMap `QueryMapped` buffered;
- FluentMap `QueryMappedUnbuffered` generated;
- FluentMap `QueryMapped` runtime fallback;
- FluentMap `QueryMappedUnbuffered` runtime fallback.

Comando representativo:

```bash
dotnet run --project benchmarks/Dapper.FluentMap.Benchmarks/Dapper.FluentMap.Benchmarks.csproj --configuration Release -- --filter *MaterializationSteadyStateBenchmarks*
```

## Resultados do Prompt 9.4

Comando executado:

```bash
dotnet run --project benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release --no-build -- --filter *MaterializationSteadyStateBenchmarks*
```

Ambiente reportado pelo BenchmarkDotNet:

- Windows 11 `10.0.26200.8875/25H2/2025Update/HudsonValley2`;
- CPU: 11th Gen Intel Core i5-1145G7;
- .NET SDK: `10.0.302`;
- Runtime: `.NET 10.0.10`;
- BenchmarkDotNet: `0.15.8`;
- Job: `ShortRun`, 1000 linhas por operacao.

| Method | Mean | StdDev | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| DapperWithFluentMapRootMapping | 1.741 ms | 0.1655 ms | 66.4063 | - | 283.3 KB | 1.00 |
| QueryMappedNestedObjectRuntimeFallback | 1.775 ms | 0.1504 ms | 89.8438 | 27.3438 | 377.16 KB | 1.33 |
| QueryMappedSimpleUnbufferedRuntimeFallback | 1.796 ms | 0.0716 ms | 82.0313 | - | 345.48 KB | 1.22 |
| QueryMappedValueObject | 1.818 ms | 0.1290 ms | 62.5000 | 19.5313 | 276.5 KB | 0.98 |
| QueryMappedValueObjectRuntimeFallback | 1.865 ms | 0.2101 ms | 136.7188 | 25.3906 | 587.99 KB | 2.08 |
| QueryMappedSimpleRuntimeFallback | 1.911 ms | 0.1662 ms | 87.8906 | 19.5313 | 361.58 KB | 1.28 |
| QueryMappedNestedObject | 2.056 ms | 0.1918 ms | 70.3125 | 23.4375 | 292.47 KB | 1.03 |
| QueryMappedSimple | 2.098 ms | 0.1705 ms | 62.5000 | 11.7188 | 261.15 KB | 0.92 |
| DapperPureUnbuffered | 2.241 ms | 0.6324 ms | 62.5000 | - | 266.96 KB | 0.94 |
| QueryMappedImmutableConstructor | 2.432 ms | 0.2152 ms | 62.5000 | 11.7188 | 261.09 KB | 0.92 |
| DapperPure | 2.563 ms | 0.0961 ms | 62.5000 | - | 283.17 KB | 1.00 |
| QueryMappedSimpleUnbuffered | 2.633 ms | 0.2799 ms | 58.5938 | - | 245.17 KB | 0.87 |

## Leitura

- O tempo continua ruidoso no `ShortRun`; nao ha base para promessa publica de
  throughput.
- A diferenca de alocacao confirma o efeito esperado do caminho unbuffered:
  - Dapper puro: `266.96 KB` unbuffered vs `283.17 KB` buffered;
  - FluentMap simple generated: `245.17 KB` unbuffered vs `261.15 KB`
    buffered;
  - FluentMap simple runtime fallback: `345.48 KB` unbuffered vs `361.58 KB`
    buffered.
- A reducao e coerente com a remocao da `List<TEntity>` interna. O custo de
  entidades, provider, reader, shape de colunas e materializacao por linha
  permanece.
- Nested e Value Object unbuffered nao foram adicionados como metodos
  separados nesta rodada para evitar crescimento excessivo da matriz; o
  mecanismo e o mesmo, e a cobertura funcional exercita esses shapes.

## Benchmarks adicionados no Prompt 9.5

O benchmark steady state passou a incluir:

- Dapper `QueryUnbufferedAsync`;
- FluentMap `QueryMappedUnbufferedAsync` generated;
- FluentMap `QueryMappedUnbufferedAsync` runtime fallback.

Esses cenarios medem overhead e alocacao por item no streaming assincrono. Eles
nao sao tratados como substitutos diretos dos cenarios sincronos porque o valor
de async depende do provider e de I/O real; SQLite em memoria pode completar de
forma essencialmente local.

Comando executado:

```bash
dotnet run --project benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release --no-build -- --filter *MaterializationSteadyStateBenchmarks*
```

Ambiente reportado pelo BenchmarkDotNet:

- Windows 11 `10.0.26200.8875/25H2/2025Update/HudsonValley2`;
- CPU: 11th Gen Intel Core i5-1145G7;
- .NET SDK: `10.0.302`;
- Runtime: `.NET 10.0.10`;
- BenchmarkDotNet: `0.15.8`;
- Job: `ShortRun`, 1000 linhas por operacao.

| Method | Mean | StdDev | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| QueryMappedValueObject | 1.824 ms | 0.1105 ms | 62.5000 | 19.5313 | 276.5 KB | 0.98 |
| DapperPureUnbufferedAsync | 1.857 ms | 0.2207 ms | 62.5000 | - | 267.27 KB | 0.94 |
| QueryMappedValueObjectRuntimeFallback | 1.872 ms | 0.1797 ms | 136.7188 | 31.2500 | 587.99 KB | 2.08 |
| DapperPureUnbuffered | 1.953 ms | 0.1212 ms | 64.4531 | - | 266.96 KB | 0.94 |
| QueryMappedSimpleRuntimeFallback | 2.004 ms | 0.1523 ms | 85.9375 | 19.5313 | 361.58 KB | 1.28 |
| QueryMappedSimpleUnbufferedAsync | 2.013 ms | 0.2180 ms | 58.5938 | - | 245.59 KB | 0.87 |
| DapperPure | 2.168 ms | 0.1259 ms | 66.4063 | - | 283.17 KB | 1.00 |
| QueryMappedSimpleUnbufferedAsyncRuntimeFallback | 2.170 ms | 0.3495 ms | 82.0313 | - | 345.89 KB | 1.22 |
| DapperWithFluentMapRootMapping | 2.215 ms | 0.1494 ms | 62.5000 | - | 283.3 KB | 1.00 |
| QueryMappedImmutableConstructor | 2.319 ms | 0.1303 ms | 62.5000 | 11.7188 | 261.09 KB | 0.92 |
| QueryMappedNestedObject | 2.329 ms | 0.0504 ms | 70.3125 | 23.4375 | 292.47 KB | 1.03 |
| QueryMappedSimpleUnbufferedRuntimeFallback | 2.346 ms | 0.4538 ms | 83.9844 | - | 345.48 KB | 1.22 |
| QueryMappedSimpleUnbuffered | 2.434 ms | 0.1912 ms | 58.5938 | - | 245.17 KB | 0.87 |
| QueryMappedSimple | 2.873 ms | 0.5289 ms | 62.5000 | 11.7188 | 261.15 KB | 0.92 |
| QueryMappedNestedObjectRuntimeFallback | 3.010 ms | 0.1693 ms | 89.8438 | 27.3438 | 377.16 KB | 1.33 |

## Leitura do Prompt 9.5

- O tempo local continuou ruidoso no `ShortRun`; as margens de erro sao grandes
  demais para conclusoes de throughput.
- As alocacoes do FluentMap async streaming ficaram alinhadas ao unbuffered
  sincrono:
  - generated simple: `245.59 KB` async vs `245.17 KB` sincrono;
  - runtime fallback simple: `345.89 KB` async vs `345.48 KB` sincrono.
- Dapper async unbuffered ficou em `267.27 KB`, proximo do Dapper unbuffered
  sincrono em `266.96 KB`.
- A diferenca esperada e pequena porque o custo dominante permanece em rows,
  provider/reader, column shape e objetos materializados; o async enumerator
  adiciona pouco no cenario medido.

## Benchmarks finais do Prompt 9.6

O benchmark steady state passou a incluir cenarios representativos de
`QueryMultiple`:

- Dapper `QueryMultiple` buffered;
- FluentMap `QueryMultipleMapped` generated;
- FluentMap `QueryMultipleMapped` runtime fallback.

Comando executado:

```bash
dotnet run --project benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release --no-build -- --filter *MaterializationSteadyStateBenchmarks*
```

Ambiente reportado pelo BenchmarkDotNet:

- Windows 11 `10.0.26200.8875/25H2/2025Update/HudsonValley2`;
- CPU: 11th Gen Intel Core i5-1145G7;
- .NET SDK: `10.0.302`;
- Runtime: `.NET 10.0.10`;
- Dapper: `2.1.79`;
- BenchmarkDotNet: `0.15.8`;
- Job: `ShortRun`, 1000 linhas por operacao.

| Method | Mean | StdDev | Gen0 | Gen1 | Allocated | Alloc Ratio |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| DapperWithFluentMapRootMapping | 1.617 ms | 0.1288 ms | 66.4063 | - | 283.3 KB | 1.00 |
| QueryMappedValueObject | 1.661 ms | 0.2101 ms | 62.5000 | 19.5313 | 276.5 KB | 0.98 |
| QueryMappedValueObjectRuntimeFallback | 1.788 ms | 0.3112 ms | 136.7188 | 25.3906 | 587.99 KB | 2.08 |
| DapperPure | 1.863 ms | 0.2177 ms | 68.3594 | - | 283.17 KB | 1.00 |
| DapperQueryMultipleBuffered | 1.865 ms | 0.1218 ms | 66.4063 | 7.8125 | 284.18 KB | 1.00 |
| QueryMappedSimpleRuntimeFallback | 1.885 ms | 0.2440 ms | 85.9375 | 19.5313 | 361.58 KB | 1.28 |
| DapperPureUnbufferedAsync | 1.900 ms | 0.0383 ms | 62.5000 | - | 267.27 KB | 0.94 |
| QueryMappedNestedObject | 1.916 ms | 0.0789 ms | 70.3125 | 23.4375 | 292.47 KB | 1.03 |
| DapperPureUnbuffered | 1.933 ms | 0.1777 ms | 62.5000 | - | 266.96 KB | 0.94 |
| QueryMappedNestedObjectRuntimeFallback | 2.032 ms | 0.2998 ms | 89.8438 | 27.3438 | 377.16 KB | 1.33 |
| QueryMappedSimpleUnbufferedRuntimeFallback | 2.057 ms | 0.2421 ms | 82.0313 | - | 345.48 KB | 1.22 |
| QueryMappedSimpleUnbuffered | 2.156 ms | 0.3421 ms | 58.5938 | - | 245.17 KB | 0.87 |
| QueryMappedSimple | 2.283 ms | 0.0523 ms | 62.5000 | 11.7188 | 261.15 KB | 0.92 |
| QueryMultipleMappedSimple | 2.345 ms | 0.2091 ms | 62.5000 | 7.8125 | 263.77 KB | 0.93 |
| QueryMappedSimpleUnbufferedAsync | 2.372 ms | 0.1782 ms | 58.5938 | - | 245.59 KB | 0.87 |
| QueryMappedSimpleUnbufferedAsyncRuntimeFallback | 2.375 ms | 0.4317 ms | 82.0313 | - | 345.89 KB | 1.22 |
| QueryMultipleMappedSimpleRuntimeFallback | 2.382 ms | 0.1981 ms | 87.8906 | - | 363.07 KB | 1.28 |
| QueryMappedImmutableConstructor | 2.869 ms | 0.3252 ms | 62.5000 | 11.7188 | 261.09 KB | 0.92 |

## Leitura final do Prompt 9.6

- A rodada foi bem-sucedida, mas continua sendo `ShortRun` local; tempo nao
  deve ser tratado como promessa publica.
- As alocacoes confirmam o comportamento esperado:
  - Dapper buffered: `283.17 KB`;
  - FluentMap buffered generated: `261.15 KB`;
  - FluentMap buffered runtime fallback: `361.58 KB`;
  - Dapper unbuffered: `266.96 KB`;
  - FluentMap unbuffered generated: `245.17 KB`;
  - FluentMap unbuffered runtime fallback: `345.48 KB`.
- `QueryMultipleMappedSimple` alocou `263.77 KB`, proximo ao
  `QueryMappedSimple` generated buffered (`261.15 KB`) e abaixo do Dapper
  `QueryMultiple` buffered (`284.18 KB`) nesta rodada.
- `QueryMultipleMappedSimpleRuntimeFallback` alocou `363.07 KB`, alinhado ao
  custo esperado do runtime fallback buffered (`361.58 KB`).
- Streaming sincrono e assincrono nao apresentam crescimento proporcional ao
  tamanho total por uma `List<TEntity>` criada pelo FluentMap; as allocations
  ficam abaixo dos caminhos buffered equivalentes para o shape simple.
- Nao foi identificada regressao de allocation em relacao aos resultados dos
  Prompts 9.4 e 9.5. A variacao de tempo permaneceu alta demais para comparar
  throughput com seguranca.

## Limitacoes

- SQLite em memoria mede pouco I/O real; async aqui mede principalmente
  overhead do contrato e do provider local.
- `ShortRun` usa poucas iteracoes e e adequado como smoke de regressao, nao
  como publicacao de performance.
- SQL Server e PostgreSQL nao foram medidos porque nao existe infraestrutura
  provider-specific instalada neste repositorio.

## Auditoria final do Prompt 9.7

Comando reexecutado:

```bash
dotnet run --project benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release --no-build -- --filter *MaterializationSteadyStateBenchmarks*
```

Resultado: sucesso, 18 cenarios executados.

A rodada confirmou a mesma leitura de allocations registrada no Prompt 9.6:

- Dapper buffered, unbuffered e async unbuffered permanecem como baseline.
- FluentMap generated e runtime fallback estao separados em buffered,
  unbuffered, async unbuffered e QueryMultiple.
- As allocations do streaming continuam abaixo dos equivalentes buffered para o
  shape simple, coerentes com a ausencia de `List<TEntity>` interna do
  FluentMap.
- Tempo local continua ruidoso em `ShortRun`; a documentacao publica nao deve
  fazer claims promocionais de throughput.
