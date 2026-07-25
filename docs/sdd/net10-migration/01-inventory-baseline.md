# 01 - Inventory and Baseline

## Specification

This delivery inventories the current state before any migration or dependency update.

Target final state:

```text
src/Dapper.FluentMap              -> netstandard2.0
src/Dapper.FluentMap.Dommel       -> netstandard2.0
test/Dapper.FluentMap.Tests       -> net10.0
test/Dapper.FluentMap.Dommel.Tests -> net10.0
```

Rules for this delivery:

- Do not change `TargetFramework` or `TargetFrameworks`.
- Do not update packages.
- Do not change C# code, solution files, CI, pack files, or workflows.
- Create only SDD handoff documentation under `docs/sdd/net10-migration/`.
- Preserve `netstandard2.0` for all `src/` projects.

## Discovery

### Branch

Current shared branch:

- `chore/net10-migration`

This branch was created locally for the migration because the starting branch was not `main`/`master` and `chore/net10-migration` did not exist locally.

### Skills Used

Local skills available under `.agents/skills/`:

- `assertion-quality`
- `coverage-analysis`
- `detect-static-dependencies`
- `dotnet-aot-compat`
- `migrate-nullable-references`
- `msbuild-antipatterns`
- `msbuild-modernization`
- `run-tests`
- `test-anti-patterns`
- `test-gap-analysis`

Skills used for this delivery:

- `msbuild-modernization`: identify project style and migration-relevant MSBuild concerns.
- `msbuild-antipatterns`: classify current project-file risks without changing them.
- `run-tests`: select and document the correct baseline test approach for the current VSTest + xUnit 2 setup.

### Repository Build Files

Found:

- `Dapper.FluentMap.sln`
- `NuGet.Config`
- `.appveyor.yml`
- `.travis.yml`
- Four SDK-style `.csproj` files.

Not found:

- `global.json`
- `Directory.Build.props`
- `Directory.Build.targets`
- `Directory.Packages.props`
- `.editorconfig`
- `.github/workflows/`
- `*.props`, `*.targets`, `*.ps1`, `*.sh`, `*.cake`, `*.cmd`, or `*.bat` build scripts beyond the files listed above.

### Solution Projects

| Project | Path | Current TFM | Desired TFM | Notes |
|---|---|---|---|---|
| Dapper.FluentMap | `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | `netstandard2.0` | `netstandard2.0` | Published core library. Uses `<TargetFrameworks>` with a single TFM. |
| Dapper.FluentMap.Dommel | `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` | `netstandard2.0` | `netstandard2.0` | Published Dommel integration. Uses `<TargetFrameworks>` with a single TFM. |
| Dapper.FluentMap.Tests | `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | `netcoreapp3.1` | `net10.0` | xUnit 2 / VSTest test project. Uses SQLite integration tests. |
| Dapper.FluentMap.Dommel.Tests | `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | `netcoreapp3.1` | `net10.0` | xUnit 2 / VSTest test project. Includes `coverlet.collector`. |

### Direct Dependencies

| Project | Direct packages |
|---|---|
| `src/Dapper.FluentMap` | `Dapper 2.0.35` |
| `src/Dapper.FluentMap.Dommel` | `Dapper 2.0.35`, `Dommel 2.0.0` |
| `test/Dapper.FluentMap.Tests` | `Microsoft.NET.Test.Sdk 16.7.1`, `Microsoft.Data.Sqlite 3.1.32`, `xunit 2.4.1`, `xunit.runner.visualstudio 2.4.3` |
| `test/Dapper.FluentMap.Dommel.Tests` | `Microsoft.NET.Test.Sdk 16.7.1`, `xunit 2.4.1`, `xunit.runner.visualstudio 2.4.3`, `coverlet.collector 1.3.0` |

See `dependency-matrix.md` for latest stable versions, compatibility notes, vulnerabilities, and planned actions.

### Tests

Detected test platform:

- VSTest through `Microsoft.NET.Test.Sdk`.
- xUnit 2 through `xunit` and `xunit.runner.visualstudio`.
- No Microsoft Testing Platform signal found in `global.json`, project files, or shared props.

Detected test count by source attributes:

- `test/Dapper.FluentMap.Tests`: 45 `[Fact]` tests.
- `test/Dapper.FluentMap.Dommel.Tests`: 7 `[Fact]` tests.
- Total: 52 `[Fact]` tests.

Coverage:

- `coverlet.collector 1.3.0` is referenced only by `test/Dapper.FluentMap.Dommel.Tests`.
- No `runsettings` or custom coverage configuration was found.

### CI

| File | Current behavior | Migration risk |
|---|---|---|
| `.appveyor.yml` | Visual Studio 2019 image, runs `dotnet test`. | Image may not contain .NET 10 SDK/runtime. Review in Delivery 04. |
| `.travis.yml` | `dotnet: 3.1`, runs `dotnet test`. | Explicitly obsolete for `net10.0`; review in Delivery 04. |

No GitHub Actions workflows were found.

### Environment Baseline

Sanitized `dotnet --info` summary:

- Active SDK: `10.0.302`
- MSBuild: `18.6.11`
- Host runtime: `10.0.10`
- OS: Windows x64
- Installed SDKs: `8.0.423`, `10.0.110`, `10.0.204`, `10.0.302`
- Installed `Microsoft.NETCore.App` runtimes: `8.0.29`, `10.0.8`, `10.0.10`
- `global.json`: not found
- .NET Core 3.1 runtime: not installed

### Baseline Commands

Commands requested by this delivery:

| Command | Result | Cause | Classification |
|---|---|---|---|
| `dotnet --info` | Succeeded | SDK available. | Environment info. |
| `dotnet --list-sdks` | Succeeded | SDK available. | Environment info. |
| `dotnet --list-runtimes` | Succeeded | SDK available. | Environment info. |
| `dotnet restore ./Dapper.FluentMap.sln` | Failed | Global NuGet cache metadata for `microsoft.netcore.targets/1.1.0` is corrupted: invalid JSON start byte in `.nupkg.metadata`. | Environmental; not a code failure. |
| `dotnet build ./Dapper.FluentMap.sln` | Failed | Build performs restore first and hit the same global NuGet cache corruption. | Environmental; not a code failure. |
| `dotnet test ./Dapper.FluentMap.sln` | Failed | Test command performs restore first and hit the same global NuGet cache corruption. | Environmental; not a code failure. |

Additional diagnostic commands using an isolated local package cache:

| Command | Result | Cause | Classification |
|---|---|---|---|
| `dotnet restore ./Dapper.FluentMap.sln --packages ./.nuget/packages` | Succeeded | Avoided corrupted global NuGet package cache. | Confirms restore is viable. |
| `dotnet build ./Dapper.FluentMap.sln --no-restore` | Succeeded | Used assets from isolated restore. | Code compiles in Debug: 0 warnings, 0 errors. |
| `dotnet test ./Dapper.FluentMap.sln --no-build` | Failed | Testhost requires `Microsoft.NETCore.App 3.1.0`, which is not installed. Installed runtimes start at 8.0 and 10.0. | Environmental/runtime baseline failure caused by current `netcoreapp3.1` test TFM. |

### Package Diagnostics

Commands:

- `dotnet list ./Dapper.FluentMap.sln package --include-transitive --no-restore`
- `dotnet list ./Dapper.FluentMap.sln package --outdated --include-transitive --no-restore`
- `dotnet list ./Dapper.FluentMap.sln package --deprecated --no-restore`
- `dotnet list ./Dapper.FluentMap.sln package --vulnerable --include-transitive --no-restore`

Findings:

- Direct production packages have no reported vulnerabilities or deprecation in the current graph.
- Current `xunit 2.4.1` is reported as deprecated/legacy with `xunit.v3` as suggested alternative.
- xUnit 3 migration is intentionally deferred to Delivery 05.
- Test graphs contain vulnerable transitives through old test/runtime packages:
  - `Newtonsoft.Json 9.0.1`
  - `System.Net.Http 4.3.0`
  - `System.Text.RegularExpressions 4.3.0`
  - `SQLitePCLRaw.lib.e_sqlite3 2.1.2` in `Dapper.FluentMap.Tests`

## Decision

### Safe Update Order

1. Delivery 02: migrate test projects from `netcoreapp3.1` to `net10.0` and update test-only packages required for a supported test runtime.
2. Delivery 03: update `src/` project dependencies while preserving `netstandard2.0`.
3. Delivery 04: run full validation, package inspection, and CI review.
4. Delivery 05: migrate from xUnit 2 to xUnit 3 as a separate compatibility and syntax change.

### Packages Planned for Delivery 02

Update only test project packages:

- `Microsoft.NET.Test.Sdk`: `16.7.1` -> latest stable identified `18.8.1`
- `Microsoft.Data.Sqlite`: `3.1.32` -> latest stable identified `10.0.10`
- `xunit`: `2.4.1` -> latest stable xUnit 2 identified `2.9.3`
- `xunit.runner.visualstudio`: `2.4.3` -> latest stable identified `3.1.5`
- `coverlet.collector`: `1.3.0` -> latest stable identified `10.0.1`, if coverage collector remains referenced

Do not introduce `xunit.v3` in Delivery 02.

### Packages Planned for Delivery 03

Update only direct `src/` dependencies after tests can run on `net10.0`:

- `Dapper`: `2.0.35` -> latest stable identified `2.1.79`
- `Dommel`: `2.0.0` -> latest stable identified `3.5.3`

The Dommel update is a major-version jump and must be validated against the existing Dommel resolver behavior.

### Packages Blocked by `netstandard2.0`

No direct production dependency planned for Delivery 03 is currently blocked by `netstandard2.0`:

- `Dapper 2.1.79` declares `netstandard2.0` compatibility.
- `Dommel 3.5.3` declares `netstandard2.0` compatibility.

Test-only packages that do not declare `netstandard2.0` are not blockers because they are not published dependencies of the `src/` projects.

### xUnit Strategy

Delivery 02 keeps xUnit 2:

- Keep test source syntax unchanged.
- Update `xunit` only to the latest stable xUnit 2 line.
- Use `xunit.runner.visualstudio` that can run xUnit 2 tests on modern VSTest.

Delivery 05 handles xUnit 3:

- Introduce `xunit.v3` packages only there.
- Re-check runner/platform syntax there.
- Treat xUnit 3 as an independent migration because package IDs, runner behavior, analyzers, and discovery can change.

### `netstandard2.0` Consumption Validation

Use the `net10.0` test projects as consumers of the `netstandard2.0` `src/` projects:

1. Restore the solution.
2. Build `src/Dapper.FluentMap` and `src/Dapper.FluentMap.Dommel` in Release.
3. Run both test projects on `net10.0`.
4. Keep Dapper integration tests active so materialization/type-map behavior is exercised by a real `net10.0` testhost.
5. In Delivery 04, run `dotnet pack` and inspect package dependency groups to confirm published outputs remain `netstandard2.0`.

### Known Risks

- Current default restore is blocked by a corrupted global NuGet cache entry. Later deliveries may need an isolated package cache or a user-performed cache cleanup.
- Current tests cannot run on this machine until the test TFM moves off `netcoreapp3.1` or the obsolete runtime is installed. Do not install .NET Core 3.1 automatically.
- `Dommel 2.0.0` -> `3.5.3` is a major update; validate integration behavior carefully.
- `coverlet.collector 10.0.1` requires modern SDK/test SDK support; update it together with `Microsoft.NET.Test.Sdk`.
- CI files are legacy and likely incompatible with `net10.0`; review after local migration succeeds.
- Project files use `<TargetFrameworks>` for a single target. This is an existing MSBuild style issue, but changing it should be done only in the delivery that edits project files.

## Delivery

Created SDD handoff files only:

- `docs/sdd/net10-migration/README.md`
- `docs/sdd/net10-migration/status.md`
- `docs/sdd/net10-migration/decisions.md`
- `docs/sdd/net10-migration/dependency-matrix.md`
- `docs/sdd/net10-migration/01-inventory-baseline.md`

No `.csproj`, C# source, solution, dependency, CI, or packaging files were changed.

No `.gitignore` change was needed because generated restore/build/test outputs are already ignored:

- `.nuget/`
- `artifacts/`
- `bin/`
- `obj/`
- `TestResults/`

## Validation

Validation checklist:

- All solution projects inventoried: yes.
- All direct dependencies registered: yes.
- Latest stable versions identified: yes, using NuGet.org source and `dotnet list package --outdated`.
- Baseline commands documented: yes.
- Shared branch documented: yes.
- Functional files unchanged: yes; documentation-only delivery.
- Absolute local paths omitted from documentation: yes.
- Sensitive data documented: no.
- Handoff folder sufficient for next chats: yes.

Commands to run before committing:

```bash
git diff
git status
```
