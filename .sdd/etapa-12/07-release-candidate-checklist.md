# Release Candidate Checklist

Checklist for promoting the current fork line to a release candidate. Do not create a tag, GitHub release or NuGet publish until the required gates are complete.

## Build

- [ ] `dotnet restore ./Dapper.FluentMap.sln` passes.
- [ ] `dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore` passes.
- [ ] Release build has zero warnings or every warning is explicitly accepted for RC.
- [ ] Local SDK matches `global.json`.

## Tests

- [ ] `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build` passes.
- [ ] Core tests pass.
- [ ] Dommel tests pass.
- [ ] DependencyInjection tests pass.
- [ ] Analyzer tests pass.
- [ ] Generator tests pass.
- [ ] Generated registration tests pass.
- [ ] Provider compatibility tests pass with SQLite.
- [ ] Skipped conditional provider tests are recorded with reason.

## Compatibility

- [ ] `netstandard2.0` remains the public package TFM.
- [ ] Test runtime remains documented separately from package TFM.
- [ ] Dapper range is reviewed and matches package metadata.
- [ ] Dommel range is reviewed and matches package metadata.
- [ ] Known Dapper TypeHandler reflection boundary is covered by tests.

## Providers

- [ ] SQLite provider tests pass.
- [ ] SQL Server harness status is recorded.
- [ ] PostgreSQL harness status is recorded.
- [ ] MySQL/MariaDB status remains documented as not validated unless real tests are added.
- [ ] SQL Server CE status remains documented as legacy/upstream-limited.

## Packages

- [ ] `dotnet pack ./Dapper.FluentMap.sln --configuration Release --no-build --output ./artifacts/packages-rc` passes.
- [ ] Exactly five `.nupkg` files are produced.
- [ ] Exactly three `.snupkg` files are produced for runtime packages.
- [ ] No test, benchmark, smoke, `.sdd`, temporary or secret files are included.
- [ ] Package README, license, repository URL and dependency ranges are inspected.
- [ ] Package version is not `2.0.0`.

## API Compatibility

- [ ] Public API surface is reviewed for core, Dommel, DependencyInjection, Analyzers and Generators.
- [ ] Fork-owned API/binary baseline strategy is decided before stable.
- [ ] Any intentional incompatibility is documented in migration notes.
- [ ] No accidental public API additions are left unreviewed.

## Documentation

- [ ] README describes FluentMap as an advanced mapping layer for Dapper, not an ORM.
- [ ] README includes installation, quick start and supported modern APIs.
- [ ] README links to migration, compatibility, support and changelog documents.
- [ ] Documentation examples are validated against real APIs.
- [ ] PT-BR documentation remains present.

## Migration

- [ ] `MIGRATION.md` covers original FluentMap to current fork.
- [ ] Initialization and registration compatibility are explained.
- [ ] Conventions, nested objects, value objects, profiles, `Ignore()`, persistence semantics, isolated configuration and DI are covered.
- [ ] The guide avoids telling users to migrate compatible APIs unnecessarily.

## CI

- [ ] CI restore/build/test/pack workflow passes.
- [ ] Release workflow validates packages without publishing.
- [ ] Action pins are reviewed.
- [ ] SourceLink URL/checksum is validated after the commit is available remotely.
- [ ] Artifact retention and provenance behavior are confirmed in GitHub Actions.

## Security

- [ ] `dotnet list ./Dapper.FluentMap.sln package --vulnerable --include-transitive` reports no untriaged vulnerabilities.
- [ ] NuGet Audit behavior is verified in CI.
- [ ] No secrets are present in source or packages.
- [ ] Package signing decision is recorded.
- [ ] SBOM decision is recorded.

## AOT / Trimming

- [ ] Trimmed smoke for explicit registration passes or blockers are recorded.
- [ ] Trimmed smoke for generated registration passes or blockers are recorded.
- [ ] Trimmed smoke for DI registration passes or blockers are recorded.
- [ ] Known IL warnings are documented and not hidden.
- [ ] Native AOT is not claimed unless publish and execution are validated with the native toolchain.

## Known Limitations

- [ ] Global `FluentMapper`/Dapper/Dommel state is documented.
- [ ] Dommel runtime isolation limitation is documented.
- [ ] Provider certification limits are documented.
- [ ] QueryMultiple/streaming limits are documented.
- [ ] Write converter metadata-only behavior is documented.
- [ ] Generated materializer fallback behavior is documented.

## Release Blockers

- [ ] Version strategy is finalized for the fork line.
- [ ] API/binary baseline is established for stable promotion.
- [ ] SourceLink remote validation passes after push.
- [ ] Analyzer/generator release manifests are reviewed.
- [ ] SQL Server/PostgreSQL provider certification decision is made.
- [ ] SBOM and package signing are either implemented or explicitly deferred.
- [ ] NuGet publish remains disabled until trusted publishing/OIDC and approval gates are configured.
