# RC Gate Report - 3.0.0-rc.1

## Scope

Prompt RC.5 consolidates the final local release-candidate gate for
`3.0.0-rc.1`. No product code blocker was found and no feature work was added.

Candidate package artifacts must be regenerated after the RC.5 candidate commit
so the package repository commit can match the final SHA. The pre-commit package
gate below validated the package shape and consumption path before creating that
single candidate commit.

## Gate Inventory

| Gate | Evidence | Status | Blocker |
| ---- | -------- | ------ | ------- |
| branch | `git status --short --branch`: `release/3.0.0-rc.1...origin/release/3.0.0-rc.1 [ahead 2]` before RC.5 edits | Passed | No |
| versionamento | `Directory.Build.props`, `Directory.Build.targets`, release workflow and artifact validator lock `3.0.0-rc.1`; pack gate generated only `3.0.0-rc.1` packages | Passed | No |
| restore | `dotnet restore ./Dapper.FluentMap.sln` | Passed | No |
| build | `dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore -p:Version=3.0.0-rc.1` | Passed, 0 warnings, 0 errors | No |
| tests | `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build` | Passed: core, Dommel, analyzer, generator, generated registration, DI and provider test projects | No |
| SQLite | `dotnet test ./test/Dapper.FluentMap.ProviderCompatibility.Tests/Dapper.FluentMap.ProviderCompatibility.Tests.csproj --configuration Release --no-build` | Passed: 7 SQLite tests; SQL Server/PostgreSQL skipped by missing connection strings | No |
| package validation | `dotnet pack ./Dapper.FluentMap.sln --configuration Release --no-build --output <rc5-precommit> -p:Version=3.0.0-rc.1`; `eng/validate-release-artifacts.ps1` | Passed: 5 `.nupkg`, 3 `.snupkg`, nuspecs, layouts, dependency ranges and metadata valid | No |
| artifacts | Pre-commit RC.5 artifact directory contained the expected 8 artifacts plus generated `artifact-manifest.json` | Passed | No |
| remote workflow | `.github/workflows/release.yml` read; RC.3 run `30476842589` passed validate and provenance jobs on `release/3.0.0-rc.1`; workflow now includes consumer smoke | Ready with known limitation | No |
| SourceLink | RC.3 validated SourceLink for core, DI, Dommel, analyzer and generator PDBs against commit `44f690195f9a06703e04c051411047b993644186` | Ready for final remote SHA validation after push | No |
| provenance | RC.3 validated GitHub artifact attestations for 8 package artifacts with SLSA provenance v1 | Ready for final remote SHA validation after push | No |
| consumer core | `eng/consumer-smoke/run-consumer-smoke.ps1 -PackageDirectory <rc5-precommit>` | Passed | No |
| analyzer | Consumer smoke restored analyzer package as compiler analyzer and diagnostic consumer emitted `DFM001` | Passed | No |
| generator | Consumer smoke restored generator package, generated `AddGeneratedMappings()` and materialized generated scenarios | Passed | No |
| DI | Consumer smoke validated `AddFluentMap`, isolated runtimes and generated DI registration | Passed | No |
| Dommel | Consumer smoke validated SQLite insert/update/default/computed/read-only/ignored metadata scenarios | Passed | No |
| trimming | Consumer smoke published and executed `TrimExplicitConsumer` and `TrimGeneratedConsumer` on `win-x64` with no trimming warnings | Passed | No |
| vulnerability audit | `dotnet list ./Dapper.FluentMap.sln package --vulnerable --include-transitive` | Passed: no vulnerable packages reported | No |
| documentation | `CHANGELOG.md`, README/adoption docs from prior gates and SDD release docs reviewed | Passed after RC.5 release-note finalization | No |
| changelog | `CHANGELOG.md` section `3.0.0-rc.1 - Unreleased` finalized for RC scope, risks, provider status, AOT/trimming limits, migration and RC status | Passed | No |

## Issue Classification

| Classification | Issue | Disposition |
| -------------- | ----- | ----------- |
| Critical | None found | No RC change required |
| High | None found | No RC change required |
| Medium | `workflow_dispatch` for `release.yml` is unavailable until the workflow exists on the default branch; RC.3 qualification used the `push` trigger successfully | Not an RC package blocker; keep operational limitation documented |
| Low | GitHub CLI attestation download is public preview | Documented; no product or package impact |
| Stable-only | SQL Server and PostgreSQL harnesses are conditional and not certified in CI | Do not claim provider certification beyond SQLite for RC.1 |
| Stable-only | Package signing and formal SBOM are undecided | Keep out of RC.1 publication gate |
| Stable-only | Fork-owned API/binary baseline and RC adoption feedback are still needed for stable | Stable release blocker, not RC blocker |
| Post-3.0 | Full Native AOT support and write-converter execution in Dapper/Dommel write paths | Backlog; no RC.1 feature change |

## Exit Criteria

| Criterion | Result |
| --------- | ------ |
| Critical blockers | 0 |
| High RC blockers | 0 |
| Remote qualification infrastructure | Ready |
| Consumer smoke | Passed |
| Package version | `3.0.0-rc.1` |

## RC.5 Result

Candidate state: Ready for publication qualification, pending regeneration of
candidate packages from the final RC.5 commit SHA.
