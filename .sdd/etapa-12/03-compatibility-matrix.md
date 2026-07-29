# Compatibility Matrix

Matriz definida em 2026-07-29 para validar compatibilidade essencial sem
explodir combinacoes. O repositorio publica bibliotecas `netstandard2.0` e
executa testes em `net10.0`; portanto a matriz cruza a compilacao do TFM
publico com o runtime de testes moderno.

## Supported Frameworks

| TFM | Status | Notes |
| --- | ------ | ----- |
| `netstandard2.0` | Supported | TFM dos pacotes publicos: core, Dommel, DependencyInjection, Analyzers e Generators. |
| `net10.0` | Test runtime | TFM dos projetos de teste, AOT smoke e benchmarks. Nao eleva o TFM minimo dos pacotes. |

## Dapper

| Dapper | TFM | Build | Tests | Status |
| ------ | --- | ----- | ----- | ------ |
| `2.1.79` | `netstandard2.0` build + `net10.0` tests | Required | Required | Minimum supported and latest stable in NuGet.org on 2026-07-29. |

No Dapper preview atual foi selecionado. O feed NuGet.org so apresentou
pre-releases antigas da linha `1.x`, sem valor para a matriz de release atual.

## Dommel

| Dommel | Dapper | TFM | Build | Tests | Status |
| ------ | ------ | --- | ----- | ----- | ------ |
| `3.5.3` | `2.1.79` | `netstandard2.0` build + `net10.0` tests | Required | Required | Minimum supported and latest stable in NuGet.org on 2026-07-29. |

Dommel permanece pacote opcional e bridge process-wide. A matriz do core nao
promete isolamento Dommel por runtime.

## Analyzer And Generator Components

| Component | TFM | Compiler References | Build | Tests | Status |
| --------- | --- | ------------------- | ----- | ----- | ------ |
| `Dapper.FluentMap.Analyzers` | `netstandard2.0` | `Microsoft.CodeAnalysis.CSharp` `5.6.0`, `Microsoft.CodeAnalysis.Analyzers` `5.6.0` | Required | Required | Roslyn component; not a runtime TFM lane. |
| `Dapper.FluentMap.Generators` | `netstandard2.0` | `Microsoft.CodeAnalysis.CSharp` `5.6.0`, `Microsoft.CodeAnalysis.Analyzers` `5.6.0` | Required | Required | Roslyn component; not a runtime TFM lane. |

Analyzer/generator compatibility is validated separately from runtime Dapper
compatibility because Roslyn package references are compiler/load-context
inputs, not runtime framework support claims.

## Test Coverage Requirements

The essential Dapper matrix runs the runtime-facing projects that cover:

| Category | Evidence |
| -------- | -------- |
| Type maps | `Dapper.FluentMap.Tests` mapping registry, composition and compatibility bridge tests. |
| Constructors | `ConstructorMappingTests` and immutable/value-object materialization tests. |
| Nested mappings | `NestedMaterializationSpikeTests`, `ValueObjectMaterializationTests` and configuration validation tests. |
| Generated/runtime materialization | `Dapper.FluentMap.GeneratedRegistration.Tests`, generated materializer tests and runtime fallback tests. |
| Profiles | Profile tests in core, DI and configuration isolation suites. |
| QueryMultiple | `AdvancedQueryHardeningTests` and mapped grid reader tests. |
| Streaming | `QueryMappedUnbuffered*` sync/async tests. |
| Converters | `PropertyConversionMetadataTests`, runtime conversion tests and TypeHandler interoperability tests. |
| Configuration isolation | `IsolatedRuntimeTests`, `ConfigurationIsolationHardeningTests` and DI provider tests. |

Benchmarks are intentionally excluded from the compatibility matrix.

## Dependency Range

The declared Dapper dependency range is:

```xml
[2.1.79,3.0.0)
```

Rationale:

- `2.1.79` is both the minimum currently supported Dapper and the latest stable
  available on NuGet.org on 2026-07-29.
- The exclusive `3.0.0` upper bound prevents unvalidated future major versions
  from being selected by consumers.
- Future Dapper stable releases within the `2.x` line must be added to the CI
  matrix before a release claims them as latest validated.

The CI can override restore with `-p:DapperPackageVersion=<version>` so each
matrix lane restores a specific Dapper version while the package default keeps
the supported NuGet range.

## CI Policy

The essential CI lanes are:

| Job | Matrix | Scope |
| --- | ------ | ----- |
| `compatibility` | Dapper version lane | Restore, build and runtime tests for core, generated registration, DI and Dommel. |
| `roslyn-components` | None | Analyzer build/test and generator build/test. |
| `pack` | None | Pack once with the default dependency range after compatibility jobs pass. |

When a newer stable Dapper appears, add a second `compatibility` matrix lane:

```yaml
- dapper-lane: latest-stable
  dapper-version: <new-stable>
```

Do not add OS, provider or preview dimensions to this job until there is a
specific compatibility question. Provider certification and Native AOT remain
separate release gates.

## Compatibility Boundary

Allowed Dapper surface:

- `SqlMapper.SetTypeMap`
- `SqlMapper.GetTypeMap`
- `SqlMapper.ExecuteReader`
- `SqlMapper.ExecuteReaderAsync`
- `SqlMapper.HasTypeHandler`
- `SqlMapper.ITypeMap`
- `SqlMapper.IMemberMap`
- `SqlMapper.TypeHandler<T>`
- `DefaultTypeMap`
- `CommandDefinition`

Sensitive boundary:

- `Dapper.FluentMap.Compatibility.DapperTypeHandlerAdapter` resolves
  `SqlMapper.TypeHandlerCache<T>.Parse(object)` by reflection. This is the
  known highest-risk Dapper compatibility point and must stay covered by tests
  and diagnostics.

No new direct access to Dapper internals was introduced in this prompt.

## Known Incompatibilities

No incompatible Dapper or Dommel stable version was found during Prompt 12.2.
The only validated Dapper stable available in the current NuGet context is
`2.1.79`, so maximum validated Dapper is also `2.1.79`.
