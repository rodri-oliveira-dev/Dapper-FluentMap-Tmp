# Dependency Matrix

Latest stable versions were identified with `dotnet list package --outdated --include-transitive --no-restore` after an isolated restore, and cross-checked against NuGet.org package pages on 2026-07-25.

## Projects and Direct Dependencies

| Project | Type | Current TFM | Desired TFM | Project references | Direct package | Current version | Latest stable identified | Declared `netstandard2.0` compatibility | `net10.0` consumer compatibility | Planned action | Notes / blocks |
|---|---|---|---|---|---|---:|---:|---|---|---|---|
| `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | Published library | `netstandard2.0` | `netstandard2.0` | - | `Dapper` | `2.1.79` | `2.1.79` | Yes; latest includes `netstandard2.0` assets. | Yes; latest declares `net10.0` compatibility. | Completed in Delivery 03. | Updated from `2.0.35`; no code changes required; Dapper type-map integration tests passed. |
| `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` | Published library / integration | `netstandard2.0` | `netstandard2.0` | `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | `Dapper` | `2.1.79` | `2.1.79` | Yes; latest includes `netstandard2.0` assets. | Yes; latest declares `net10.0` compatibility. | Completed in Delivery 03. | Updated from `2.0.35`; kept aligned with core and Dommel dependency floor. |
| `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` | Published library / integration | `netstandard2.0` | `netstandard2.0` | `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | `Dommel` | `3.5.3` | `3.5.3` | Yes; latest includes `netstandard2.0` assets. | Yes; latest declares `net10.0` compatibility. | Completed in Delivery 03. | Updated from `2.0.0`; major update compiled without resolver code changes and Dommel tests passed. |
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | Test project | `net10.0` | `net10.0` | `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | `Microsoft.NET.Test.Sdk` | `18.8.1` | `18.8.1` | Test-only; latest declares `netstandard2.0`, `net8.0`, and computed `net10.0` compatibility. | Yes. | Completed in Delivery 02. | Updated from `16.7.1`; VSTest execution passed on `net10.0`. |
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | Test project | `net10.0` | `net10.0` | `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | `Microsoft.Data.Sqlite` | `10.0.10` | `10.0.10` | Yes; current includes `netstandard2.0` assets. | Yes; current has computed `net10.0` compatibility. | Completed in Delivery 02. | Updated from `3.1.32`; still resolves vulnerable transitive `SQLitePCLRaw.lib.e_sqlite3 2.1.11`, pending review. |
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | Test project | `net10.0` | `net10.0` | `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | `xunit.v3` | `3.2.2` | `3.2.2` | Test-only framework package; xUnit v3 supports modern .NET test projects. | Yes. | Completed in Delivery 05. | Replaced `xunit 2.9.3`; no test code changes required. |
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | Test project | `net10.0` | `net10.0` | `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | `xunit.runner.visualstudio` | `3.1.5` | `3.1.5` | Test adapter; latest does not declare `netstandard2.0`, but that is not required for published `src` packages. | Yes; latest supports .NET 8+ and computed `net10.0`; can run xUnit v1/v2/v3 tests. | Completed in Delivery 05. | Kept as VSTest runner for `dotnet test` and Test Explorer compatibility. |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | Test project | `net10.0` | `net10.0` | `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` | `Microsoft.NET.Test.Sdk` | `18.8.1` | `18.8.1` | Test-only; latest declares `netstandard2.0`, `net8.0`, and computed `net10.0` compatibility. | Yes. | Completed in Delivery 02. | Updated from `16.7.1`; VSTest execution passed on `net10.0`. |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | Test project | `net10.0` | `net10.0` | `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` | `xunit.v3` | `3.2.2` | `3.2.2` | Test-only framework package; xUnit v3 supports modern .NET test projects. | Yes. | Completed in Delivery 05. | Replaced `xunit 2.9.3`; no test code changes required. |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | Test project | `net10.0` | `net10.0` | `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` | `xunit.runner.visualstudio` | `3.1.5` | `3.1.5` | Test adapter; latest does not declare `netstandard2.0`, but that is not required for published `src` packages. | Yes; latest supports .NET 8+ and computed `net10.0`; can run xUnit v1/v2/v3 tests. | Completed in Delivery 05. | Kept as VSTest runner for `dotnet test` and Test Explorer compatibility. |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | Test project | `net10.0` | `net10.0` | `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` | `coverlet.collector` | `10.0.1` | `10.0.1` | Test/coverage collector; latest does not declare `netstandard2.0`, but it is not a published dependency. | Yes; latest supports .NET 8+ / .NET Framework 4.7.2+ and declares `net10.0` compatibility. | Completed in Delivery 02. | Updated from `1.3.0`; repo still has no runsettings. |

## Relevant Transitive Findings

| Area | Package | Resolved version | Latest stable identified | Finding | Planned handling |
|---|---|---:|---:|---|---|
| Test transitives | `Newtonsoft.Json` | Not resolved after Delivery 02 | `13.0.4` | Was vulnerable in old test platform graph. | Resolved by Delivery 02 test package updates. |
| Test transitives | `System.Net.Http` | Not resolved after Delivery 02 | `4.3.4` | Was vulnerable in old `netcoreapp3.1` graph. | Resolved by Delivery 02 test package updates. |
| Test transitives | `System.Text.RegularExpressions` | Not resolved after Delivery 02 | `4.3.1` | Was vulnerable in old `netcoreapp3.1` graph. | Resolved by Delivery 02 test package updates. |
| Core/test transitives | `Microsoft.NETCore.Targets` | `1.1.0` | `5.0.0` | Old transitive package involved in the corrupted global cache error. | Do not edit directly; should disappear from modern test graph where possible. |
| Core/test transitives | `Microsoft.NETCore.Platforms` | `1.1.0` | `7.0.4` | Old transitive from `NETStandard.Library` graph. | Do not edit directly. |
| Source transitives | `Microsoft.Bcl.AsyncInterfaces` | `10.0.8` | `10.0.10` | Transitive dependency resolved through `Dapper 2.1.79` for `netstandard2.0`. | Do not force as a direct source dependency; restore selected Dapper's dependency floor. |
| Dommel transitives | `System.ComponentModel.Annotations` | `5.0.0` | `5.0.0` | Updated naturally through `Dommel 3.5.3`. | Completed by Delivery 03 without direct override. |
| Dommel transitives | `Microsoft.Bcl.HashCode` | `6.0.0` | `6.0.0` | New `Dommel 3.5.3` transitive dependency for `netstandard2.0`. | Accept as package metadata dependency; no direct override. |
| Core tests | `SQLitePCLRaw.lib.e_sqlite3` | `2.1.11` | `3.53.3` | Still vulnerable after `Microsoft.Data.Sqlite 10.0.10`; reported as NU1903 high severity in `Dapper.FluentMap.Tests`. | Deferred to a dedicated dependency-hardening task; do not add unrelated overrides in the xUnit 3 migration. |
| Test transitives | `xunit.analyzers` | `1.27.0` | `1.27.0` | Updated transitively by xUnit 3 in Delivery 05. | No direct override needed. |
| Test transitives | `Microsoft.Testing.Platform` | `1.9.1` | `2.3.2` | Introduced transitively by the default xUnit v3 `3.x` MTP v1 package graph. | Do not force directly; repository uses VSTest for `dotnet test`. |

## Delivery 02 Applied Updates

| Project | Package | Previous version | New version | Status |
|---|---|---:|---:|---|
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | `TargetFramework` | `netcoreapp3.1` | `net10.0` | Completed. |
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | `Microsoft.NET.Test.Sdk` | `16.7.1` | `18.8.1` | Completed. |
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | `Microsoft.Data.Sqlite` | `3.1.32` | `10.0.10` | Completed. |
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | `xunit` | `2.4.1` | `2.9.3` | Completed; xUnit 3 deferred. |
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | `xunit.runner.visualstudio` | `2.4.3` | `3.1.5` | Completed. |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | `TargetFramework` | `netcoreapp3.1` | `net10.0` | Completed. |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | `Microsoft.NET.Test.Sdk` | `16.7.1` | `18.8.1` | Completed. |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | `xunit` | `2.4.1` | `2.9.3` | Completed; xUnit 3 deferred. |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | `xunit.runner.visualstudio` | `2.4.3` | `3.1.5` | Completed. |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | `coverlet.collector` | `1.3.0` | `10.0.1` | Completed. |

## Delivery 03 Applied Updates

| Project | Package | Previous version | New version | Status |
|---|---|---:|---:|---|
| `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | `Dapper` | `2.0.35` | `2.1.79` | Completed. |
| `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` | `Dapper` | `2.0.35` | `2.1.79` | Completed. |
| `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` | `Dommel` | `2.0.0` | `3.5.3` | Completed. |

## Delivery 05 Applied Updates

| Project | Package | Previous version | New version | Status |
|---|---|---:|---:|---|
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | `xunit` -> `xunit.v3` | `2.9.3` | `3.2.2` | Completed. |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | `xunit` -> `xunit.v3` | `2.9.3` | `3.2.2` | Completed. |
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | `xunit.runner.visualstudio` | `3.1.5` | `3.1.5` | Kept for VSTest and Test Explorer compatibility. |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | `xunit.runner.visualstudio` | `3.1.5` | `3.1.5` | Kept for VSTest and Test Explorer compatibility. |

## Pending Direct Package Work

| Delivery | Package | Current version | Latest stable identified | Reason deferred |
|---|---|---:|---:|---|
| - | - | - | - | No pending direct package work remains in this .NET 10 migration initiative. |

## Delivery 04 Validation Findings

No direct package version changed in Delivery 04.

Validation commands confirmed:

- restore succeeds with the current package graph.
- Debug and Release builds succeed.
- Release package generation succeeds.
- no direct package downgrade is reported.
- no vulnerable packages are reported in `src/`.
- both `src` packages are emitted under `lib/netstandard2.0`.
- both `net10.0` test projects consume the `netstandard2.0` source projects through `ProjectReference`.

NuGet still reports only deferred items already known from earlier deliveries:

- `SQLitePCLRaw.lib.e_sqlite3 2.1.11` is a vulnerable transitive in `Dapper.FluentMap.Tests`.
- xUnit 2 legacy packages are removed by Delivery 05.
- xUnit v3 introduces Microsoft Testing Platform v1 transitives through its default `3.x` package graph. They are not direct dependencies and are not forced because the repository continues to use VSTest for `dotnet test`.
- `Microsoft.Bcl.AsyncInterfaces 10.0.8` remains Dapper's resolved `netstandard2.0` dependency floor.
- `Microsoft.NETCore.Platforms 1.1.0` remains part of the `NETStandard.Library` restore graph.

Delivery 04 did not force transitive overrides because none are required to validate the .NET 10 migration and package contents.

## Packages Whose Latest Version Does Not Support `netstandard2.0`

No direct production dependency updated in Delivery 03 was blocked by `netstandard2.0`.

Test-only packages that do not need to support the published `netstandard2.0` libraries:

- `xunit.v3` latest supports the repository's `net10.0` test projects.
- `xunit.runner.visualstudio` latest targets .NET 8+ and .NET Framework 4.7.2+.
- `coverlet.collector` latest supports .NET Core 8+ and .NET Framework 4.7.2+.

## Package Source References

- NuGet package source: `https://api.nuget.org/v3/index.json`
- `Dapper`: https://www.nuget.org/packages/Dapper
- `Dommel`: https://www.nuget.org/packages/Dommel/3.5.3
- `Microsoft.Data.Sqlite`: https://www.nuget.org/packages/Microsoft.Data.Sqlite/10.0.10
- `Microsoft.NET.Test.Sdk`: https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.8.1
- `xunit.v3`: https://www.nuget.org/packages/xunit.v3
- `xunit.runner.visualstudio`: https://www.nuget.org/packages/xunit.runner.visualstudio
- `coverlet.collector`: https://www.nuget.org/packages/coverlet.collector
