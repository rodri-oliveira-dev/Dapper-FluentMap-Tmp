# 04 - Validation, Pack and CI

## Specification

This delivery consolidates the .NET 10 migration completed by Deliveries 01, 02 and 03.

Expected final matrix:

```text
src/
|-- Dapper.FluentMap                 -> netstandard2.0
`-- Dapper.FluentMap.Dommel          -> netstandard2.0

test/
|-- Dapper.FluentMap.Tests           -> net10.0
`-- Dapper.FluentMap.Dommel.Tests    -> net10.0
```

The validation must prove:

- `src/` projects still compile for `netstandard2.0`.
- `test/` projects compile and execute for `net10.0`.
- `net10.0` test projects consume the `netstandard2.0` libraries through `ProjectReference`.
- dependencies restore without downgrade or target compatibility errors.
- Debug and Release builds work.
- NuGet packages can be generated and inspected.
- package contents include expected `lib/netstandard2.0` assemblies and exclude test/local artifacts.
- CI installs or selects a .NET 10 compatible SDK.
- CI does not publish NuGet packages.

Out of scope:

- xUnit 3 migration.
- functional library changes.
- public API changes.
- moving `src/` projects to `net10.0`.
- source multi-targeting.
- publishing packages.
- pushing the branch or opening a pull request.

## Discovery

### Recovered Context

- `AGENTS.md` was read before changes.
- Local skills under `.agents/skills/` were checked.
- Skills used:
  - `run-tests` for VSTest/xUnit 2 command selection.
  - `msbuild-modernization` for TargetFramework and SDK guardrails.
  - `msbuild-antipatterns` for project-file review.
- Required handoff files were read:
  - `docs/sdd/net10-migration/README.md`
  - `docs/sdd/net10-migration/status.md`
  - `docs/sdd/net10-migration/decisions.md`
  - `docs/sdd/net10-migration/dependency-matrix.md`
  - `docs/sdd/net10-migration/01-inventory-baseline.md`
  - `docs/sdd/net10-migration/02-test-projects-net10.md`
  - `docs/sdd/net10-migration/03-src-dependencies.md`
- Shared branch recorded in `README.md`: `chore/net10-migration`.
- Current branch: `chore/net10-migration`.
- Deliveries 01, 02 and 03 are concluded in `status.md`.

### Local State

- Active SDK: `10.0.302`.
- Host runtime: `10.0.10`.
- `global.json`: not found.
- `Directory.Build.props`: not found.
- `Directory.Build.targets`: not found.
- `Directory.Packages.props`: not found.
- `.editorconfig`: not found.
- Build scripts (`*.ps1`, `*.sh`, `*.cmd`, `*.bat`, `*.cake`): not found.
- `.gitignore` already excludes local restore/build/package outputs:
  - `.nuget/`
  - `bin/`
  - `obj/`
  - `TestResults/`
  - `artifacts/`

### Project Targets

| Project | Target element | Effective target |
|---|---|---|
| `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | `TargetFrameworks` | `netstandard2.0` |
| `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` | `TargetFrameworks` | `netstandard2.0` |
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | `TargetFramework` | `net10.0` |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | `TargetFramework` | `net10.0` |

The `src/` projects still use `TargetFrameworks` with a single TFM. This is existing shape from earlier deliveries and is preserved to avoid published-project churn.

### Package and Pack Configuration

- Published projects:
  - `src/Dapper.FluentMap`
  - `src/Dapper.FluentMap.Dommel`
- Test projects have `<IsPackable>false</IsPackable>`.
- Source package metadata is unchanged from earlier deliveries:
  - `VersionPrefix` is `2.0.0`.
  - authors and copyright remain Henk Mollema.
  - `PackageProjectUrl` points to the original repository.
  - `PackageLicenseUrl` is present; no metadata modernization is introduced here.
- No `.nuspec`, SourceLink, symbol package, package README, or repository metadata file was found.
- `NuGet.Config` uses only `https://api.nuget.org/v3/index.json` after clearing inherited sources.

### Test Consumption Evidence

- `test/Dapper.FluentMap.Tests` references `src/Dapper.FluentMap`.
- `test/Dapper.FluentMap.Dommel.Tests` references `src/Dapper.FluentMap.Dommel`.
- Existing tests exercise real library behavior through:
  - `FluentMapper.Initialize`
  - Dapper `SqlMapper` type-map resolution.
  - SQLite-backed Dapper integration tests.
  - Dommel resolver integration tests.
- No additional compatibility project is required because the `net10.0` test projects already consume the `netstandard2.0` source projects.

### CI State

Found CI files:

| File | Current state | Risk |
|---|---|---|
| `.appveyor.yml` | Visual Studio 2019 image, runs only `dotnet test`. | Does not explicitly install/select .NET 10 and does not validate pack. |
| `.travis.yml` | `dotnet: 3.1`, `dist: xenial`, runs only `dotnet test`. | Incompatible with `net10.0` test projects and obsolete distro/runtime. |

No `.github/workflows/` directory exists.

No CI file currently runs `dotnet nuget push`, publishes packages, uses NuGet tokens, uses `continue-on-error`, references `poc-arquitetura`, or creates a fake test framework matrix.

### Dependency State

- Direct production dependencies are already updated by Delivery 03:
  - `Dapper 2.1.79`
  - `Dommel 3.5.3`
- Test dependencies are already updated by Delivery 02:
  - `Microsoft.NET.Test.Sdk 18.8.1`
  - `Microsoft.Data.Sqlite 10.0.10`
  - `xunit 2.9.3`
  - `xunit.runner.visualstudio 3.1.5`
  - `coverlet.collector 10.0.1`
- Known deferred items:
  - xUnit 3 migration is Delivery 05.
  - `xunit.analyzers` remains transitive to xUnit 2 and is deferred with Delivery 05.
  - `SQLitePCLRaw.lib.e_sqlite3 2.1.11` remains a vulnerable test transitive reported by NuGet; handle as separate dependency-hardening work unless Delivery 05 changes it naturally.

## Decision

### Commands

Official local and CI commands for this delivery:

```bash
dotnet restore ./Dapper.FluentMap.sln
dotnet build ./Dapper.FluentMap.sln --no-restore
dotnet test ./Dapper.FluentMap.sln --no-build
dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build
dotnet test ./test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj --configuration Release
dotnet test ./test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj --configuration Release
dotnet pack ./Dapper.FluentMap.sln --configuration Release --no-build --output ./artifacts/packages
```

`dotnet pack --no-build` is retained because Release build runs first and test projects are marked non-packable.

### SDK and `global.json`

Do not create `global.json` in this delivery.

Reasoning:

- The repo currently has no `global.json`.
- Local validation already uses a stable .NET 10 SDK (`10.0.302`).
- CI can explicitly install the stable .NET 10 channel through setup steps.
- Pinning an exact SDK file now would add maintenance and could be more brittle than the current small-library setup.

CI should use the .NET 10 SDK channel with GA quality where supported.

### CI Plan

Add GitHub Actions CI because no GitHub workflow exists and the project is hosted as a GitHub repository.

Update legacy CI files so they no longer keep obsolete SDK assumptions:

- `.appveyor.yml`: use a newer Windows image and install .NET 10 explicitly before restore/build/test/pack.
- `.travis.yml`: move from `dotnet: 3.1`/`xenial` to a .NET 10 compatible configuration and run the same restore/build/test/pack sequence.

GitHub Actions choices:

- `ubuntu-latest`.
- `actions/checkout` current major from the official action README.
- `actions/setup-dotnet` current major from the official action README.
- `dotnet-version: 10.0.x`.
- `dotnet-quality: ga`.
- no NuGet cache because there is no lock file and setup-dotnet cache requires lock files.
- upload `artifacts/packages/*.nupkg` as a CI artifact.
- no matrix, because tests target only `net10.0`.
- no package publishing or NuGet token configuration.

### Validation Criteria

Migration is valid when:

- required local restore/build/test/pack commands pass, allowing documented NuGet vulnerability warnings.
- both test projects pass directly in Release.
- package inspection confirms only expected package contents and dependency groups.
- CI YAML parses as YAML and contains no publish/token/obsolete-framework commands.
- final git diff contains only CI/config/docs changes required by this delivery.

## Delivery

- Added `.github/workflows/ci.yml`:
  - installs .NET SDK `10.0.x` with GA quality.
  - runs `dotnet --info`.
  - restores `Dapper.FluentMap.sln`.
  - builds Release with `--no-restore`.
  - tests Release with `--no-build`.
  - packs Release with `--no-build`.
  - uploads generated `.nupkg` files as workflow artifacts.
  - uses no NuGet publish command, token, secret or package source mutation.
- Updated `.appveyor.yml`:
  - moved from `Visual Studio 2019` to `Visual Studio 2022`.
  - installs .NET SDK 10 GA through `dotnet-install.ps1`.
  - runs restore, Release build, Release tests and Release pack.
  - stores generated `.nupkg` files as AppVeyor artifacts.
  - keeps `test: off` because tests are executed explicitly in the build script.
- Updated `.travis.yml`:
  - moved from `dotnet: 3.1` and `dist: xenial` to `dotnet: 10.0` and `dist: jammy`.
  - runs restore, Release build, Release tests and Release pack.
- No `global.json` was created.
- No project file, C# source file, public API, package metadata, dependency version, or Dommel behavior was changed.
- No package was published.

## Validation

### Commands Executed

| Command | Result |
|---|---|
| `dotnet --info` | Passed. Active SDK `10.0.302`; host runtime `10.0.10`; no `global.json`. |
| `dotnet restore` | Passed with existing NU1903 warning for transitive `SQLitePCLRaw.lib.e_sqlite3 2.1.11` in `Dapper.FluentMap.Tests`. |
| `dotnet build --no-restore` | Passed. `src` outputs under `Debug/netstandard2.0`; tests under `Debug/net10.0`. |
| `dotnet test --no-build` | Passed. `Dapper.FluentMap.Tests`: 45 passed; `Dapper.FluentMap.Dommel.Tests`: 7 passed. |
| `dotnet build --configuration Release --no-restore` | Passed. `src` outputs under `Release/netstandard2.0`; tests under `Release/net10.0`. |
| `dotnet test --configuration Release --no-build` | Passed. `Dapper.FluentMap.Tests`: 45 passed; `Dapper.FluentMap.Dommel.Tests`: 7 passed. |
| `dotnet test test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj --configuration Release` | Passed. Restored/built the core library as `netstandard2.0`, ran 45 `net10.0` tests. |
| `dotnet test test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj --configuration Release` | Passed. Restored/built core and Dommel libraries as `netstandard2.0`, ran 7 `net10.0` tests. |
| `dotnet pack .\Dapper.FluentMap.sln --configuration Release --no-build --output .\artifacts\packages` | Passed. Generated both expected `.nupkg` files. Warnings: NU5125 for deprecated `licenseUrl`; package README recommendation. |
| `dotnet list .\Dapper.FluentMap.sln package --include-transitive --no-restore` | Passed. Confirmed final dependency graph and TFMs. |
| `dotnet list .\Dapper.FluentMap.sln package --outdated --include-transitive --no-restore` | Passed. No outdated direct packages; deferred transitives remain. |
| `dotnet list .\Dapper.FluentMap.sln package --vulnerable --include-transitive --no-restore` | Passed. Only known test transitive `SQLitePCLRaw.lib.e_sqlite3 2.1.11` is vulnerable. |
| `dotnet list .\Dapper.FluentMap.sln package --deprecated --no-restore` | Passed. Only `xunit 2.9.3` is reported as Legacy with `xunit.v3` alternative, deferred to Delivery 05. |
| PyYAML parse of `.github/workflows/ci.yml`, `.appveyor.yml`, `.travis.yml` | Passed. YAML syntax parsed locally. |
| `rg` for publish commands, secrets, `continue-on-error`, `poc-arquitetura`, `.NET Core 3.1`, and VS 2019 in CI files | Passed. No matches. |

### TargetFramework Confirmation

| Project | Confirmed target |
|---|---|
| `src/Dapper.FluentMap/Dapper.FluentMap.csproj` | `TargetFrameworks=netstandard2.0` |
| `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj` | `TargetFrameworks=netstandard2.0` |
| `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` | `TargetFramework=net10.0` |
| `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj` | `TargetFramework=net10.0` |

### Generated Packages

Generated under `artifacts/packages/`:

- `Dapper.FluentMap.2.0.0.nupkg`
- `Dapper.FluentMap.Dommel.2.0.0.nupkg`

Package contents inspected:

| Package | Expected contents | Dependency group |
|---|---|---|
| `Dapper.FluentMap.2.0.0.nupkg` | `lib/netstandard2.0/Dapper.FluentMap.dll`, `lib/netstandard2.0/Dapper.FluentMap.xml`, `.nuspec`, package metadata files. | `.NETStandard2.0`: `Dapper 2.1.79`. |
| `Dapper.FluentMap.Dommel.2.0.0.nupkg` | `lib/netstandard2.0/Dapper.FluentMap.Dommel.dll`, `lib/netstandard2.0/Dapper.FluentMap.Dommel.xml`, `.nuspec`, package metadata files. | `.NETStandard2.0`: `Dapper.FluentMap 2.0.0`, `Dapper 2.1.79`, `Dommel 3.5.3`. |

Package inspection confirmed:

- `lib/netstandard2.0` exists in both packages.
- expected assemblies and XML documentation files are present.
- no test assemblies are present.
- no `bin/`, `obj/`, local cache, local path, source tree artifact, or secret file is present.
- package version remains `2.0.0`.
- package metadata remains consistent with existing project files.
- no symbols or SourceLink files are included; none were configured before this delivery.
- license is represented by existing `licenseUrl`, which now produces NU5125 but was not modernized to avoid unrelated package metadata churn.
- package README is not included; NuGet reports a recommendation, not a packaging failure.

### CI Validation

Local validation of CI files:

- YAML syntax parses for GitHub Actions, AppVeyor and Travis files.
- paths reference the real solution and artifact directory.
- CI commands match the locally validated Release sequence.
- no CI file publishes to NuGet.
- no CI file adds tokens or secrets.
- no CI file uses `continue-on-error`.
- no CI file references `poc-arquitetura`.
- no CI file references `netcoreapp3.1`, `.NET Core 3.1`, or `Visual Studio 2019`.
- tests for both core and Dommel are run through the solution.
- package generation is controlled and artifacts are stored, not published.

GitHub Actions was not executed remotely in this delivery. AppVeyor and Travis were also not executed remotely. The validation here is local YAML parsing plus command equivalence to the local successful build/test/pack sequence.

### Dependency Review

No direct dependency changes were required in this delivery.

Confirmed:

- no direct package downgrade was reported.
- no vulnerable packages are reported in `src/`.
- `Dapper 2.1.79` and `Dommel 3.5.3` remain compatible with `netstandard2.0` package outputs.
- test packages restore and run on `net10.0`.
- xUnit remains on `2.9.3` for Delivery 05.

Deferred:

- `xunit` -> `xunit.v3` migration remains Delivery 05.
- `xunit.analyzers 1.18.0` remains a transitive package to xUnit 2 and is deferred with xUnit 3 migration.
- `SQLitePCLRaw.lib.e_sqlite3 2.1.11` remains a vulnerable test transitive from `Microsoft.Data.Sqlite 10.0.10`; this should be handled by a dedicated dependency-hardening task unless Delivery 05 changes the graph naturally.

### Limitations

- CI was not run on GitHub/AppVeyor/Travis from this environment.
- Travis availability and image contents were not proven remotely.
- AppVeyor installation of .NET 10 depends on network access to `https://dot.net/v1/dotnet-install.ps1`.
- NuGet package metadata modernization (`PackageLicenseExpression`, README, SourceLink/repository URL metadata) was intentionally not performed because it is outside the migration validation scope.
