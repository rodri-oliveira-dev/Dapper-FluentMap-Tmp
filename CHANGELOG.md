# Changelog

This project follows the spirit of [Keep a Changelog](https://keepachangelog.com/) and Semantic Versioning for the fork line.

The historical archived package history is not reconstructed here. This changelog records the new maintained fork line.

## [Unreleased]

### Added

- Public adoption documentation for README, migration, compatibility and support policy.
- Release candidate readiness checklist under `.sdd/etapa-12/`.

### Changed

- README is now a concise bilingual entrypoint and delegates detailed release/adoption policy to dedicated documents.

## [3.0.0-rc.1] - Unreleased

### Added

- Generated materialization support for eligible explicit mappings, including runtime fallback for unsupported shapes.
- Persistence semantics for read-only, computed, insert-excluded, update-excluded and database-default columns.
- Advanced FluentMap-controlled query materialization, including `QueryMultipleMapped`, `ReadMapped*` and sync/async streaming helpers.
- Property converter metadata and read conversion in FluentMap-controlled materialization.
- Isolated immutable configuration, `FluentMapRuntime` and optional dependency injection integration.

### Changed

- Release versioning is hardened so default local pack produces `3.0.0-dev`, not historical `2.0.0` or accidental stable `3.0.0`.
- Release workflow uses an explicit validated package version for RC artifacts.

### Known Limitations

- Normal `Dapper.Query<T>()`, `SqlMapper.SetTypeMap` and Dommel integration remain process-wide.
- Write converters are metadata-only in the current Dapper/Dommel write path.
- SQL Server and PostgreSQL have conditional harnesses but are not certified in CI.
- Full Native AOT support is not claimed.
- Stable release remains blocked on fork-owned API/binary baseline, SourceLink validation on remote SHA, package signing/SBOM decision and RC feedback.

## Fork Release Candidate Line

The first fork release candidate is expected to use a prerelease version such as `3.0.0-rc.1`, unless API compatibility review proves a different versioning path is safer.

Do not reuse `2.0.0` for the fork line because `Dapper.FluentMap` and `Dapper.FluentMap.Dommel` already have historical `2.0.0` packages.
