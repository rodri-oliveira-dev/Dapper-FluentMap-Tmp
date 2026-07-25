# Dependency Matrix

Latest stable versions were identified with `dotnet list package --outdated --include-transitive --no-restore` after an isolated restore, and cross-checked against NuGet.org package pages on 2026-07-25.

## Projects and Direct Dependencies

| Project | Type | Current TFM | Desired TFM | Project references | Direct package | Current version | Latest stable identified | Declared `netstandard2.0` compatibility | `net10.0` consumer compatibility | Planned action | Notes / blocks |
|---|---|---|---|---|---|---:|---:|---|---|---|---|
| `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | Published library | `netstandard2.0` | `netstandard2.0` | - | `Dapper` | `2.0.35` | `2.1.79` | Yes; current and latest include `netstandard2.0` assets. | Yes; current via `netstandard2.0`, latest also declares `net10.0` compatibility. | Delivery 03: update after tests run on `net10.0`. | Preserve public behavior and Dapper type-map integration. |
| `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` | Published library / integration | `netstandard2.0` | `netstandard2.0` | `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | `Dapper` | `2.0.35` | `2.1.79` | Yes; current and latest include `netstandard2.0` assets. | Yes; current via `netstandard2.0`, latest also declares `net10.0` compatibility. | Delivery 03: keep aligned with core Dapper version. | Dommel integration should change only as needed for dependency compatibility. |
| `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` | Published library / integration | `netstandard2.0` | `netstandard2.0` | `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | `Dommel` | `2.0.0` | `3.5.3` | Yes; current and latest include `netstandard2.0` assets. | Yes; latest declares `net10.0` compatibility. | Delivery 03: evaluate update after core tests are on `net10.0`. | Major-version update; review Dommel resolver API behavior before changing. |
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | Test project | `netcoreapp3.1` | `net10.0` | `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | `Microsoft.NET.Test.Sdk` | `16.7.1` | `18.8.1` | Test-only; latest declares `netstandard2.0`, `net8.0`, and computed `net10.0` compatibility. | Yes. | Delivery 02: update for supported VSTest execution on `net10.0`. | Current test platform is old and pulls vulnerable transitives. |
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | Test project | `netcoreapp3.1` | `net10.0` | `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | `Microsoft.Data.Sqlite` | `3.1.32` | `10.0.10` | Yes; current and latest include `netstandard2.0` assets. | Yes; latest has computed `net10.0` compatibility. | Delivery 02: update with test runtime migration. | Needed by Dapper integration tests. |
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | Test project | `netcoreapp3.1` | `net10.0` | `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | `xunit` | `2.4.1` | `2.9.3` | Metapackage; depends on xUnit 2 packages that support .NET Standard-era test projects. | Yes with compatible runner/test SDK. | Delivery 02: update only within xUnit 2 line. | Current package is reported deprecated/legacy with `xunit.v3` alternative; defer v3 to Delivery 05. |
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | Test project | `netcoreapp3.1` | `net10.0` | `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | `xunit.runner.visualstudio` | `2.4.3` | `3.1.5` | Test adapter; latest does not declare `netstandard2.0`, but that is not required for published `src/` packages. | Yes; latest supports .NET 8+ and computed `net10.0`; can run xUnit v1/v2/v3 tests. | Delivery 02: update runner while keeping xUnit 2 framework. | Keep `PrivateAssets="all"`. |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | Test project | `netcoreapp3.1` | `net10.0` | `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` | `Microsoft.NET.Test.Sdk` | `16.7.1` | `18.8.1` | Test-only; latest declares `netstandard2.0`, `net8.0`, and computed `net10.0` compatibility. | Yes. | Delivery 02: update for supported VSTest execution on `net10.0`. | Current test platform is old and pulls vulnerable transitives. |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | Test project | `netcoreapp3.1` | `net10.0` | `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` | `xunit` | `2.4.1` | `2.9.3` | Metapackage; depends on xUnit 2 packages that support .NET Standard-era test projects. | Yes with compatible runner/test SDK. | Delivery 02: update only within xUnit 2 line. | Current package is reported deprecated/legacy with `xunit.v3` alternative; defer v3 to Delivery 05. |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | Test project | `netcoreapp3.1` | `net10.0` | `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` | `xunit.runner.visualstudio` | `2.4.3` | `3.1.5` | Test adapter; latest does not declare `netstandard2.0`, but that is not required for published `src/` packages. | Yes; latest supports .NET 8+ and computed `net10.0`; can run xUnit v1/v2/v3 tests. | Delivery 02: update runner while keeping xUnit 2 framework. | Keep `PrivateAssets="all"`. |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | Test project | `netcoreapp3.1` | `net10.0` | `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` | `coverlet.collector` | `1.3.0` | `10.0.1` | Test/coverage collector; latest does not declare `netstandard2.0`, but it is not a published dependency. | Yes; latest supports .NET 8+ / .NET Framework 4.7.2+ and declares `net10.0` compatibility. | Delivery 02: update with test runtime migration if coverage remains enabled. | Latest requires SDK 8.0.414+ and compatible `Microsoft.NET.Test.Sdk`; current repo has no runsettings. |

## Relevant Transitive Findings

| Area | Package | Resolved version | Latest stable identified | Finding | Planned handling |
|---|---|---:|---:|---|---|
| Test transitives | `Newtonsoft.Json` | `9.0.1` | `13.0.4` | Vulnerable, pulled by old test platform packages. | Expected to be removed or updated by Delivery 02 test package updates. |
| Test transitives | `System.Net.Http` | `4.3.0` | `4.3.4` | Vulnerable, pulled transitively in current `netcoreapp3.1` graph. | Expected to be removed or updated by Delivery 02 test package updates. |
| Test transitives | `System.Text.RegularExpressions` | `4.3.0` | `4.3.1` | Vulnerable, pulled transitively in current `netcoreapp3.1` graph. | Expected to be removed or updated by Delivery 02 test package updates. |
| Core/test transitives | `Microsoft.NETCore.Targets` | `1.1.0` | `5.0.0` | Old transitive package involved in the corrupted global cache error. | Do not edit directly; should disappear from modern test graph where possible. |
| Core/test transitives | `Microsoft.NETCore.Platforms` | `1.1.0` | `7.0.4` | Old transitive from `NETStandard.Library` graph. | Do not edit directly. |
| Core tests | `SQLitePCLRaw.lib.e_sqlite3` | `2.1.2` | `3.53.3` | Vulnerable in core test project graph. | Expected to be updated through `Microsoft.Data.Sqlite` in Delivery 02. |

## Packages Whose Latest Version Does Not Support `netstandard2.0`

No direct production dependency planned for Delivery 03 was found to be blocked by `netstandard2.0`.

Test-only packages that do not need to support the published `netstandard2.0` libraries:

- `xunit.runner.visualstudio` latest targets .NET 8+ and .NET Framework 4.7.2+.
- `coverlet.collector` latest supports .NET Core 8+ and .NET Framework 4.7.2+.

## Package Source References

- NuGet package source: `https://api.nuget.org/v3/index.json`
- `Dapper`: https://www.nuget.org/packages/Dapper
- `Dommel`: https://www.nuget.org/packages/Dommel/3.5.3
- `Microsoft.Data.Sqlite`: https://www.nuget.org/packages/Microsoft.Data.Sqlite/10.0.10
- `Microsoft.NET.Test.Sdk`: https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.8.1
- `xunit`: https://www.nuget.org/packages/xunit
- `xunit.runner.visualstudio`: https://www.nuget.org/packages/xunit.runner.visualstudio
- `coverlet.collector`: https://www.nuget.org/packages/coverlet.collector
