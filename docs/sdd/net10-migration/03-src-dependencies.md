# 03 - Source Project Dependencies

## Specification

Update direct production dependencies in `src/` to the newest stable versions that preserve:

- published source projects on `netstandard2.0`;
- `net10.0` test projects consuming the libraries through `ProjectReference`;
- public API and behavior unless a dependency incompatibility requires a minimal adjustment.

Do not migrate the source projects to multi-targeting, do not change package metadata, and do not update test-only packages in this delivery.

## Discovery

### Recovered Context

- `AGENTS.md` was read before changes.
- Required migration handoff files were read:
  - `docs/sdd/net10-migration/README.md`
  - `docs/sdd/net10-migration/status.md`
  - `docs/sdd/net10-migration/decisions.md`
  - `docs/sdd/net10-migration/dependency-matrix.md`
  - `docs/sdd/net10-migration/01-inventory-baseline.md`
  - `docs/sdd/net10-migration/02-test-projects-net10.md`
- Shared branch recorded in `README.md`: `chore/net10-migration`.
- Current branch: `chore/net10-migration`.
- Delivery 01 is concluded in `status.md`.
- Delivery 02 is concluded in `status.md`.

### Project Targets

| Project | Target element | Current TFM | Required final TFM |
|---|---|---|---|
| `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | `TargetFrameworks` | `netstandard2.0` | `netstandard2.0` |
| `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` | `TargetFrameworks` | `netstandard2.0` | `netstandard2.0` |
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | `TargetFramework` | `net10.0` | `net10.0` |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | `TargetFramework` | `net10.0` | `net10.0` |

The `src/` projects still use `TargetFrameworks` with a single target. This delivery preserves that shape to avoid unrelated published-project churn.

### Skills Used

- `msbuild-modernization`: selected for TFM and PackageReference guardrails.
- `msbuild-antipatterns`: selected for project-file dependency review.
- `run-tests`: selected to run the correct VSTest/xUnit 2 validation commands.

### Package Metadata Sources

Version discovery and compatibility were checked using:

- `dotnet list .\Dapper.FluentMap.sln package --outdated --include-transitive`
- `dotnet list .\Dapper.FluentMap.sln package --include-transitive --no-restore`
- `dotnet list .\Dapper.FluentMap.sln package --vulnerable --include-transitive --no-restore`
- NuGet flat container metadata:
  - `https://api.nuget.org/v3-flatcontainer/dapper/index.json`
  - `https://api.nuget.org/v3-flatcontainer/dapper/2.1.79/dapper.nuspec`
  - `https://api.nuget.org/v3-flatcontainer/dommel/index.json`
  - `https://api.nuget.org/v3-flatcontainer/dommel/3.5.3/dommel.nuspec`
- NuGet Gallery package pages:
  - `https://www.nuget.org/packages/Dapper`
  - `https://www.nuget.org/packages/Dommel`
- Official source/release pages where available:
  - `https://github.com/DapperLib/Dapper/releases`
  - `https://github.com/henkmollema/Dommel/releases`

### Direct Production Dependencies Before Update

| Project | Package | Current version |
|---|---|---:|
| `src/Dapper.FluentMap` | `Dapper` | `2.0.35` |
| `src/Dapper.FluentMap.Dommel` | `Dapper` | `2.0.35` |
| `src/Dapper.FluentMap.Dommel` | `Dommel` | `2.0.0` |

### Relevant API Usage

Core Dapper integration uses public Dapper APIs:

- `SqlMapper.ITypeMap`
- `SqlMapper.IMemberMap`
- `SqlMapper.SetTypeMap`
- `CustomPropertyTypeMap`
- `DefaultTypeMap`

Dommel integration uses public Dommel APIs:

- `DommelMapper.SetColumnNameResolver`
- `DommelMapper.SetKeyPropertyResolver`
- `DommelMapper.SetTableNameResolver`
- `DommelMapper.SetPropertyResolver`
- `IColumnNameResolver`
- `IKeyPropertyResolver`
- `ITableNameResolver`
- `IPropertyResolver`
- `Default*Resolver`
- `ColumnPropertyInfo`

### Relevant Transitives Before Update

| Area | Package | Resolved version | Finding |
|---|---|---:|---|
| `src` via Dapper/netstandard graph | `Microsoft.NETCore.Platforms` | `1.1.0` | Old transitive from the netstandard restore graph; not a direct dependency to force. |
| Dommel integration | `System.ComponentModel.Annotations` | `4.7.0` | Transitive dependency of `Dommel 2.0.0`; latest Dommel updates this to `5.0.0` for `netstandard2.0`. |
| Core tests | `SQLitePCLRaw.lib.e_sqlite3` | `2.1.11` | Known NU1903 high severity warning remains from Delivery 02; test-only transitive and outside this source-dependency delivery. |

## Decision

### Dependency Selection Table

| Project | Package | Previous version | Chosen version | Latest stable available | Reason for choice | `netstandard2.0` compatibility | Breaking changes evaluated | Code correction expected |
|---|---|---:|---:|---:|---|---|---|---|
| `src/Dapper.FluentMap` | `Dapper` | `2.0.35` | `2.1.79` | `2.1.79` | Latest stable from NuGet; package includes `netstandard2.0` assets and keeps the public type-map APIs used by FluentMap. | Compatible. NuGet metadata declares `.NETStandard2.0` with dependencies on `Microsoft.Bcl.AsyncInterfaces`, `System.Reflection.Emit.Lightweight`, and `System.Threading.Tasks.Extensions`. | Dapper release notes from the 2.1 line include TFM changes, async API normalization, DateOnly/TimeOnly support disablement after unlisted releases, type-handler fixes, and dependency updates. FluentMap does not use DateOnly/TimeOnly support or obsolete internal type-handler APIs. | None expected; compile and Dapper integration tests must confirm. |
| `src/Dapper.FluentMap.Dommel` | `Dapper` | `2.0.35` | `2.1.79` | `2.1.79` | Keep the integration aligned with the core Dapper version and avoid a lower direct version than Dommel transitively requires. | Compatible, same as core. | Same Dapper review as the core project. | None expected; compile and Dommel tests must confirm. |
| `src/Dapper.FluentMap.Dommel` | `Dommel` | `2.0.0` | `3.5.3` | `3.5.3` | Latest stable from NuGet; package includes `netstandard2.0` assets and preserves Dommel's resolver extension model according to package metadata and source surface to be validated by compile/tests. | Compatible. NuGet metadata declares `.NETStandard2.0` with dependencies on `Dapper 2.1.72`, `Microsoft.Bcl.HashCode 6.0.0`, and `System.ComponentModel.Annotations 5.0.0`. | Major-version update. Release page has tags through the 3.5 line but no detailed migration notes for 3.5.3 were found; resolver API compatibility will be verified by compilation and existing Dommel resolver tests. | Possible minimal resolver signature adjustment if the public Dommel interfaces changed. |

### Update Categories

Safe updates:

- `Dapper 2.0.35` -> `2.1.79` in both source projects, pending build/test confirmation.

Updates that require focused validation:

- `Dommel 2.0.0` -> `3.5.3` because it is a major-version jump and the integration implements Dommel resolver interfaces.

Blocked updates:

- None for direct production dependencies. No selected latest stable direct production package is blocked by `netstandard2.0`.

Deferred updates:

- `xunit` / `xunit.v3`: deferred to Delivery 05.
- `SQLitePCLRaw.*` test transitives: defer to Delivery 04 or a dedicated dependency-hardening task unless source dependency updates naturally change the graph.
- `xunit.analyzers`: transitive of `xunit`; do not force as a direct dependency in this delivery.
- `Microsoft.NETCore.Platforms`: transitive netstandard graph package; do not force as a direct dependency.

## Delivery

- Updated `src/Dapper.FluentMap/Dapper.FluentMap.csproj`:
  - `Dapper` `2.0.35` -> `2.1.79`
- Updated `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj`:
  - `Dapper` `2.0.35` -> `2.1.79`
  - `Dommel` `2.0.0` -> `3.5.3`
- Preserved `TargetFrameworks netstandard2.0` in both `src/` projects.
- Preserved `TargetFramework net10.0` in both test projects.
- No C# code changes were required.
- No public API, package metadata, CI, xUnit packages, or test code was changed.
- No tests were skipped, removed, or weakened.

### Direct Production Dependencies After Update

| Project | Package | Final version | Status |
|---|---|---:|---|
| `src/Dapper.FluentMap` | `Dapper` | `2.1.79` | Updated to latest stable. |
| `src/Dapper.FluentMap.Dommel` | `Dapper` | `2.1.79` | Updated to latest stable and aligned with core. |
| `src/Dapper.FluentMap.Dommel` | `Dommel` | `3.5.3` | Updated to latest stable. |

### Post-Update Dependency Findings

| Area | Package | Resolved version | Latest stable identified | Handling |
|---|---|---:|---:|---|
| `src` via Dapper `netstandard2.0` graph | `Microsoft.Bcl.AsyncInterfaces` | `10.0.8` | `10.0.10` | Do not force as direct dependency; Dapper declares `>= 10.0.8` and restore chose the dependency floor. |
| `src` via `NETStandard.Library` graph | `Microsoft.NETCore.Platforms` | `1.1.0` | `7.0.4` | Do not force as direct dependency. Existing netstandard graph behavior. |
| Test transitives | `SQLitePCLRaw.*` | `2.1.11` | `3.0.4` / `3.53.3` | Test-only SQLite transitives remain deferred to Delivery 04 or dependency hardening. |
| Test transitives | `xunit.analyzers` | `1.18.0` | `1.27.0` | Transitive of xUnit 2; defer to Delivery 05. |

## Validation

Environment:

- Active SDK: `10.0.302`
- Test runner: VSTest
- Test framework: xUnit 2

Commands executed:

| Command | Result |
|---|---|
| `dotnet restore` | Passed with the existing NU1903 warning for transitive `SQLitePCLRaw.lib.e_sqlite3 2.1.11` in `Dapper.FluentMap.Tests`. |
| `dotnet build` | Passed; `src` outputs built under `netstandard2.0`, test outputs under `net10.0`. |
| `dotnet test` | Passed; `Dapper.FluentMap.Tests`: 45 passed, 0 failed, 0 skipped; `Dapper.FluentMap.Dommel.Tests`: 7 passed, 0 failed, 0 skipped. |
| `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj` | Passed; 45 passed, 0 failed, 0 skipped. |
| `dotnet test .\test\Dapper.FluentMap.Dommel.Tests\Dapper.FluentMap.Dommel.Tests.csproj` | Passed; 7 passed, 0 failed, 0 skipped. |
| `dotnet build --configuration Release` | Passed; `src` outputs built under `netstandard2.0`, test outputs under `net10.0`. |
| `dotnet test --configuration Release` | Passed; `Dapper.FluentMap.Tests`: 45 passed, 0 failed, 0 skipped; `Dapper.FluentMap.Dommel.Tests`: 7 passed, 0 failed, 0 skipped. |
| `dotnet list .\Dapper.FluentMap.sln package --include-transitive --no-restore` | Passed; direct production dependencies resolve to `Dapper 2.1.79` and `Dommel 3.5.3`. |
| `dotnet list .\Dapper.FluentMap.sln package --outdated --include-transitive --no-restore` | Passed; no outdated direct production package remains. Only deferred transitives were reported. |
| `dotnet list .\Dapper.FluentMap.sln package --vulnerable --include-transitive --no-restore` | Passed; no vulnerable packages in `src/`; existing vulnerable test transitive `SQLitePCLRaw.lib.e_sqlite3 2.1.11` remains in `Dapper.FluentMap.Tests`. |
| `dotnet list .\Dapper.FluentMap.sln package --deprecated --no-restore` | Passed; no deprecated packages in `src/`; `xunit 2.9.3` remains legacy in test projects and is deferred to Delivery 05. |

Explicit confirmations:

- Shared branch is `chore/net10-migration`.
- Deliveries 01 and 02 are concluded in `status.md`.
- Test projects remain `net10.0`.
- Source projects remain `netstandard2.0`.
- All direct production package references restored.
- No package downgrade or conflict was reported by restore/build.
- Dapper integration behavior is covered by the existing core integration tests on `net10.0`.
- Dommel resolver behavior is covered by the existing Dommel tests on `net10.0`.
- No unnecessary public breaking change was introduced.

Residual risks and Delivery 04 handoff:

- `dotnet pack` and package content/dependency-group inspection remain for Delivery 04.
- `SQLitePCLRaw.lib.e_sqlite3 2.1.11` still reports NU1903 in the core test project and should be reviewed in Delivery 04 or separately.
- `Dommel 3.5.3` had no detailed 3.5.3 migration notes found on the release page; compile/tests validate the resolver surface used here, but Delivery 04 should keep package inspection focused.
