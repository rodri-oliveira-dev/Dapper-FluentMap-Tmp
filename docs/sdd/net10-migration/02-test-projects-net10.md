# 02 - Test Projects on net10.0

## Specification

Migrate all projects under `test/` from `netcoreapp3.1` to `net10.0`, preserving the published `src/` projects on `netstandard2.0`.

Expected consumption shape:

```text
test net10.0
  -> ProjectReference
src netstandard2.0
```

Scope limits for this delivery:

- Update only test project TFMs and test-only dependencies needed for `net10.0`.
- Keep xUnit 2; do not introduce `xunit.v3`.
- Do not update `Dapper`, `Dommel`, public APIs, production behavior, package metadata, or CI.
- Do not skip, remove, or weaken tests.

## Discovery

### Recovered Context

- `AGENTS.md` was read before changes.
- Required migration handoff files were read:
  - `docs/sdd/net10-migration/README.md`
  - `docs/sdd/net10-migration/status.md`
  - `docs/sdd/net10-migration/decisions.md`
  - `docs/sdd/net10-migration/dependency-matrix.md`
  - `docs/sdd/net10-migration/01-inventory-baseline.md`
- Shared branch recorded in `README.md`: `chore/net10-migration`.
- Current branch: `chore/net10-migration`.
- Delivery 01 is concluded in `status.md` and the latest commit is `docs: document .NET 10 migration baseline`.

### Skills Used

- `msbuild-modernization`: selected for TargetFramework/PackageReference migration guidance.
- `run-tests`: selected to detect test runner/platform and use the correct `dotnet test` commands.

### Projects Under `test/`

| Project | Current target element | Current TFM | Project reference |
|---|---|---|---|
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | `TargetFrameworks` | `netcoreapp3.1` | `src/Dapper.FluentMap/Dapper.FluentMap.csproj` |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | `TargetFrameworks` | `netcoreapp3.1` | `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` |

### Shared Build Configuration

No shared build/test configuration files were found:

- no `global.json`
- no `Directory.Build.props`
- no `Directory.Build.targets`
- no `Directory.Packages.props`
- no `packages.lock.json`
- no `*.runsettings`
- no `.editorconfig`

Test runner detection:

- VSTest through `Microsoft.NET.Test.Sdk`.
- xUnit 2 through `xunit` and `xunit.runner.visualstudio`.
- No Microsoft Testing Platform signal was found.

### Existing Test Dependencies

| Project | Package | Current version |
|---|---|---:|
| `Dapper.FluentMap.Tests` | `Microsoft.NET.Test.Sdk` | `16.7.1` |
| `Dapper.FluentMap.Tests` | `Microsoft.Data.Sqlite` | `3.1.32` |
| `Dapper.FluentMap.Tests` | `xunit` | `2.4.1` |
| `Dapper.FluentMap.Tests` | `xunit.runner.visualstudio` | `2.4.3` |
| `Dapper.FluentMap.Dommel.Tests` | `Microsoft.NET.Test.Sdk` | `16.7.1` |
| `Dapper.FluentMap.Dommel.Tests` | `xunit` | `2.4.1` |
| `Dapper.FluentMap.Dommel.Tests` | `xunit.runner.visualstudio` | `2.4.3` |
| `Dapper.FluentMap.Dommel.Tests` | `coverlet.collector` | `1.3.0` |

`dotnet list package --outdated --include-transitive --no-restore` and NuGet.org package pages confirmed the Delivery 01 matrix still matches the latest stable versions on 2026-07-25.

### Test Code Compatibility Scan

Searches under `test/` found:

- `[assembly: CollectionBehavior(DisableTestParallelization = true)]` in both test assemblies.
- Shared global state use through `FluentMapper`, `FluentMapper.Reset`, `FluentMapper.EntityMaps`, `FluentMapper.TypeConventions`, and Dapper type-map integration.
- xUnit 2 `[Fact]` and `[Trait]` usage.
- No `Thread.Sleep`, `Task.Delay`, remoting, `BinaryFormatter`, broad warning suppression, `async void`, blocking async waits, or obvious .NET 10 removed API usage in tests.
- Existing `Assert.Throws<Exception>` remains unchanged because it is unrelated to this runtime migration.

## Decision

### Final Test Targets

| Project | Final target element | Final TFM |
|---|---|---|
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | `TargetFramework` | `net10.0` |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | `TargetFramework` | `net10.0` |

Use singular `TargetFramework` because each test project has one target. Do not normalize the `src/` projects in this delivery even though they currently use `TargetFrameworks` with one target.

### Test Package Updates

| Package | Old version | New version | Justification |
|---|---:|---:|---|
| `Microsoft.NET.Test.Sdk` | `16.7.1` | `18.8.1` | Required to run tests reliably on modern SDK/VSTest and removes old vulnerable test-platform transitives from the test graph. |
| `Microsoft.Data.Sqlite` | `3.1.32` | `10.0.10` | Test-only SQLite provider used by integration tests; aligns native/runtime assets with `net10.0` while leaving production Dapper dependencies untouched. |
| `xunit` | `2.4.1` | `2.9.3` | Latest stable xUnit 2 line; preserves xUnit 2 API and defers `xunit.v3` to Delivery 05. |
| `xunit.runner.visualstudio` | `2.4.3` | `3.1.5` | Modern VSTest adapter that supports .NET 8+ and can run xUnit 2 tests. Keep `PrivateAssets="all"`. |
| `coverlet.collector` | `1.3.0` | `10.0.1` | Coverage collector version compatible with modern SDK/test SDK. Keep `PrivateAssets="all"`. |

### Dependencies Left Temporarily Old

- `Dapper 2.0.35` in `src/` remains for Delivery 03.
- `Dommel 2.0.0` in `src/Dapper.FluentMap.Dommel` remains for Delivery 03.
- Production `src/` targets remain `netstandard2.0`.
- xUnit 3 remains deferred to Delivery 05.

### Behavior and Risk Controls

- Preserve test source behavior; no test code changes are planned unless build/test exposes a direct `net10.0` incompatibility.
- Preserve disabled parallel execution because the suites share global FluentMapper/Dapper state.
- Keep VSTest runner model because no MTP signal exists.
- Do not alter public API, mapping behavior, package metadata, or CI in this delivery.

## Delivery

- Migrated `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` from `TargetFrameworks netcoreapp3.1` to `TargetFramework net10.0`.
- Migrated `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` from `TargetFrameworks netcoreapp3.1` to `TargetFramework net10.0`.
- Updated test-only packages:
  - `Microsoft.NET.Test.Sdk` `16.7.1` -> `18.8.1`
  - `Microsoft.Data.Sqlite` `3.1.32` -> `10.0.10`
  - `xunit` `2.4.1` -> `2.9.3`
  - `xunit.runner.visualstudio` `2.4.3` -> `3.1.5`
  - `coverlet.collector` `1.3.0` -> `10.0.1`
- No C# test code changes were required.
- No tests were skipped, removed, or weakened.
- No `src/` project files were changed; both published projects remain `netstandard2.0`.
- No production dependencies were updated.
- No CI, package metadata, or public API was changed.

## Validation

Environment:

- Active SDK: `10.0.302`
- Test runner: VSTest
- Test framework: xUnit 2

Commands executed:

| Command | Result |
|---|---|
| `dotnet restore` | Passed with NU1903 warning for transitive `SQLitePCLRaw.lib.e_sqlite3 2.1.11` in `Dapper.FluentMap.Tests`. |
| `dotnet build` | Passed; `src` outputs built under `netstandard2.0`, test outputs under `net10.0`. |
| `dotnet test` | Passed; `Dapper.FluentMap.Tests`: 45 passed, 0 failed, 0 skipped; `Dapper.FluentMap.Dommel.Tests`: 7 passed, 0 failed, 0 skipped. |
| `dotnet test test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | Passed; 45 passed, 0 failed, 0 skipped. |
| `dotnet test test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | Passed; 7 passed, 0 failed, 0 skipped. |
| `dotnet build --configuration Release` | Passed; `src` outputs built under `netstandard2.0`, test outputs under `net10.0`. |
| `dotnet test --configuration Release` | Passed; `Dapper.FluentMap.Tests`: 45 passed, 0 failed, 0 skipped; `Dapper.FluentMap.Dommel.Tests`: 7 passed, 0 failed, 0 skipped. |
| `dotnet list .\Dapper.FluentMap.sln package --include-transitive --no-restore` | Passed; confirmed test projects resolve as `net10.0` and `src` projects as `netstandard2.0`. |
| `dotnet list .\Dapper.FluentMap.sln package --outdated --include-transitive --no-restore` | Passed; direct test packages are current, while production `Dapper`/`Dommel`, xUnit analyzer transitives, and SQLitePCLRaw transitives remain visible. |
| `dotnet list .\Dapper.FluentMap.sln package --vulnerable --include-transitive --no-restore` | Passed; only `Dapper.FluentMap.Tests` reports transitive `SQLitePCLRaw.lib.e_sqlite3 2.1.11` high severity. |
| `dotnet list .\Dapper.FluentMap.sln package --deprecated --no-restore` | Passed; `xunit 2.9.3` remains marked legacy with `xunit.v3` alternative, intentionally deferred to Delivery 05. |

Explicit confirmations:

- Test projects compile and execute for `net10.0`.
- `src/Dapper.FluentMap` remains `netstandard2.0`.
- `src/Dapper.FluentMap.Dommel` remains `netstandard2.0`.
- `net10.0` test projects consume `netstandard2.0` production projects through `ProjectReference`.
- No test was ignored to hide a migration issue.
- No unrelated functional behavior was changed.
- Repeated Debug/Release test runs were deterministic in result counts.

Residual risks:

- `Microsoft.Data.Sqlite 10.0.10` still resolves vulnerable transitive `SQLitePCLRaw.lib.e_sqlite3 2.1.11`. This did not block restore/build/test, but should be reviewed in Delivery 04 or in a dedicated dependency-hardening task before release.
- `Dapper 2.0.35` and `Dommel 2.0.0` remain intentionally pending for Delivery 03.
- xUnit 3 remains intentionally pending for Delivery 05.
- `dotnet pack` was not run because this delivery did not alter published package projects or package metadata.
