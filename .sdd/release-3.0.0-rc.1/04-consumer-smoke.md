# Consumer Smoke

## Package source

Artifacts remotos registrados em `.sdd/release-3.0.0-rc.1/artifacts.json`.

- Workflow run: `30476842589`
- Artifact remoto: `release-packages-3.0.0-rc.1`
- Commit dos packages: `44f690195f9a06703e04c051411047b993644186`
- Pasta local validada: `artifacts/release-3.0.0-rc.1/remote/packages`
- Feed NuGet temporario criado pelo smoke:
  `.tmp/consumer-smoke/feed`
- Cache NuGet temporario criado pelo smoke:
  `.tmp/consumer-smoke/packages`

O script `eng/consumer-smoke/run-consumer-smoke.ps1` recria a pasta
`.tmp/consumer-smoke`, copia somente os `.nupkg` remotos validados para o feed
temporario, usa `NuGet.Config` temporario com `packageSourceMapping`, restaura
com `RestorePackagesPath` temporario e `RestoreNoCache=true`, e bloqueia
`ProjectReference`/referencias diretas a assemblies locais nos consumers.

Os artifacts locais ja existiam; nao foi necessario baixar novamente via
`gh run download`. O script possui fallback de download pelo workflow run
registrado quando os artifacts esperados nao estiverem presentes.

## Package hashes

Hashes SHA-256 recalculados e comparados com
`.sdd/release-3.0.0-rc.1/artifacts.json` antes do smoke:

| Artifact | SHA-256 |
| --- | --- |
| `Dapper.FluentMap.3.0.0-rc.1.nupkg` | `55059c450db16a28d8e058460571950bcf967a88e4435a519a82612609f6407f` |
| `Dapper.FluentMap.3.0.0-rc.1.snupkg` | `9c1e96ba9f0760311280b4b6ffefce05a7f8d0dbe38844fefc4c9949711f90f7` |
| `Dapper.FluentMap.Analyzers.3.0.0-rc.1.nupkg` | `0b9e4c01bce2ef772b441124a03df554637a729c71912242f7ca28cfec8576fb` |
| `Dapper.FluentMap.DependencyInjection.3.0.0-rc.1.nupkg` | `339cabaea4399aa4d0387794a03910d4348c1ce46fa001aaa4a9ceb35f7a8785` |
| `Dapper.FluentMap.DependencyInjection.3.0.0-rc.1.snupkg` | `866400c82b10f9d4655bd03e0ae94ef99c70bb7da68f9ee9a675815ff6d11065` |
| `Dapper.FluentMap.Dommel.3.0.0-rc.1.nupkg` | `9c4b2157cf5f65b17e8914077921e1d23836adebb7f2fce2a6ff6f6673c8b680` |
| `Dapper.FluentMap.Dommel.3.0.0-rc.1.snupkg` | `b249b91f0764664a4e5f60161d082374ce1d22215c8d977cd886b0ced258a3bf` |
| `Dapper.FluentMap.Generators.3.0.0-rc.1.nupkg` | `9fcfc0c4586e35c408b49e964c1ed622cfc49192089da466809c721553920629` |

## Scenarios

- Core console consumer: restore, build, run.
- Generator/analyzer valid console consumer: restore, build, generated source
  file check, run.
- Analyzer diagnostic console consumer: restore, build expected to fail with
  `DFM001`.
- Dependency Injection console consumer: restore, build, run.
- Dommel console consumer: restore, build, run.
- Trimming explicit consumer: restore for RID, `dotnet publish
  -p:PublishTrimmed=true`, run published binary.
- Trimming generated consumer: restore for RID, `dotnet publish
  -p:PublishTrimmed=true`, run published binary.
- Dependency inspection: assets file scan for exact RC versions, absence of
  project libraries, absence of direct source references, analyzer/generator
  packages without runtime assets, no `2.0.0` packages.
- CI mode check: script executed with `-PackageDirectory` to validate the path
  used by the release workflow.

## Core consumer

Result: Passed.

Validated with `eng/consumer-smoke/CoreConsumer`:

- install of `Dapper.FluentMap 3.0.0-rc.1`;
- explicit configuration with `AddMap<TMap>()`;
- legacy API compatibility through `FluentMapper.EntityMaps` and
  `FluentMapper.GetEntityMaps()`;
- Dapper root column mapping through `QuerySingle<T>()`;
- `QueryMapped<T>()`;
- constructor mapping;
- nested object materialization;
- immutable/value object materialization;
- profile query through `QueryMappedSingle<TEntity, TProfile>()`;
- read converter through `ConvertFromDatabaseUsing<TConverter, TDatabase>()`;
- isolated runtime through `FluentMapConfigurationBuilder`.

## Generator/analyzer consumer

Result: Passed.

Validated with `eng/consumer-smoke/GeneratorAnalyzerConsumer` and
`eng/consumer-smoke/AnalyzerDiagnosticConsumer`:

- `Dapper.FluentMap.Generators 3.0.0-rc.1` loads as a compiler analyzer from
  the package layout without manual DLL reference;
- generated extension `AddGeneratedMappings()` compiles;
- generated registration captures generated materializer metadata;
- generated materialization works for flat, nested, value object and read
  converter scenarios;
- `Dapper.FluentMap.Analyzers 3.0.0-rc.1` emits known diagnostic `DFM001`;
- analyzer/generator packages have no runtime assets in `project.assets.json`;
- restore/build succeeded without PDB/layout issues.

## DI consumer

Result: Passed.

Validated with `eng/consumer-smoke/DIConsumer`:

- `ServiceCollection`;
- `AddFluentMap`;
- explicit map registration;
- generated registration through `builder.Configure(c => c.AddGeneratedMappings())`;
- `ImmutableFluentMapConfiguration` and `FluentMapRuntime` resolution;
- real SQLite query through resolved runtime;
- two isolated DI configurations using separate runtime instances.

## Dommel consumer

Result: Passed.

Validated with `eng/consumer-smoke/DommelConsumer` using SQLite:

- `Insert`;
- `Update`;
- non-identity key with `IsKey().SetGeneratedOption(DatabaseGeneratedOption.None)`;
- identity key with `IsIdentity()`;
- database default via `DatabaseDefaultOnInsert()`;
- computed column via `Computed()`;
- read-only column via `ReadOnly()`;
- ignored column via `Ignore()`;
- read-after-write through raw Dapper query and `Dommel.Get<T>()`.

## Trimming consumer

Result: Passed.

Executed locally on `win-x64`:

```bash
dotnet publish ./eng/consumer-smoke/TrimExplicitConsumer/TrimExplicitConsumer.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishTrimmed=true
dotnet publish ./eng/consumer-smoke/TrimGeneratedConsumer/TrimGeneratedConsumer.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishTrimmed=true
```

Both published binaries were executed successfully.

Observed trimming warnings: none.

The release workflow will run the same script on `ubuntu-latest`, where the
script resolves the current RID dynamically, expected as `linux-x64` on the
current hosted runner.

## Results

Overall result: Passed.

Package classification:

| Package | Result |
| --- | --- |
| `Dapper.FluentMap 3.0.0-rc.1` | Passed |
| `Dapper.FluentMap.Dommel 3.0.0-rc.1` | Passed |
| `Dapper.FluentMap.DependencyInjection 3.0.0-rc.1` | Passed |
| `Dapper.FluentMap.Analyzers 3.0.0-rc.1` | Passed |
| `Dapper.FluentMap.Generators 3.0.0-rc.1` | Passed |

Resolved package versions observed in consumer `project.assets.json`:

- `Dapper.FluentMap`: `3.0.0-rc.1`
- `Dapper.FluentMap.Dommel`: `3.0.0-rc.1`
- `Dapper.FluentMap.DependencyInjection`: `3.0.0-rc.1`
- `Dapper.FluentMap.Analyzers`: `3.0.0-rc.1`
- `Dapper.FluentMap.Generators`: `3.0.0-rc.1`
- `Dapper`: `2.1.79`
- `Dommel`: `3.5.3`

No `ProjectReference` was present in public FluentMap package consumers. No
`2.0.0` package was restored. Roslyn analyzer/generator packages did not appear
as runtime assets.

CI integration:

- `.github/workflows/release.yml` now runs
  `./eng/consumer-smoke/run-consumer-smoke.ps1 -PackageDirectory './artifacts/packages'`
  after release artifact validation and before dependency inventory/upload
  completion.
- Release job timeout increased from 45 to 60 minutes to account for
  self-contained trimmed publish.

## Failures

No product/package failures.

Harness adjustments made during implementation:

- Valid consumers changed nested lambda expressions from `Address!.City` to
  `Address.City`, because the analyzer correctly reports `DFM001` for null
  forgiveness in map expressions.
- DI isolation types were separated because the generator correctly reports
  `DFM007` for multiple generated default maps targeting the same entity in one
  compilation.

## Blockers

No RC.5 blockers from consumer smoke.
