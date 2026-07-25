# Cross-Delivery Decisions

## Preserve `netstandard2.0` for Published Projects

`src/Dapper.FluentMap` and `src/Dapper.FluentMap.Dommel` must remain on `netstandard2.0` throughout the migration. Dependency updates in Delivery 03 must not force either published project to move to `net8.0`, `net10.0`, or multi-targeting.

## Keep xUnit 2 Until Delivery 05

Delivery 02 may update xUnit 2 packages to the latest stable xUnit 2 line, but must not migrate test code or project references to `xunit.v3`. The xUnit 3 migration is intentionally isolated in Delivery 05.

## Separate Test Runtime Migration From Source Dependency Updates

Delivery 02 should focus on test projects and test-only packages needed for `net10.0`. Delivery 03 should focus on direct dependencies of `src/` projects (`Dapper` and `Dommel`) after the tests can run on a supported runtime.

## Use `net10.0` Tests as Consumer Validation

The primary compatibility check for the published `netstandard2.0` projects is to run the `net10.0` test projects while they reference the `src/` projects. Delivery 04 should add package inspection with `dotnet pack` to confirm the published assemblies and dependency groups still target `netstandard2.0`.

## Delivery 02 Test Runtime Migration

Delivery 02 migrated only the test projects to `net10.0` and changed their single-target element from `TargetFrameworks` to `TargetFramework`. The `src/` projects were not normalized in this delivery and remain on `TargetFrameworks netstandard2.0` to avoid unrelated published-project churn.

Test execution remains on VSTest with xUnit 2:

- no `global.json` Microsoft Testing Platform runner was introduced;
- no `TestingPlatformDotnetTestSupport` property was introduced;
- `xunit` was updated only within the xUnit 2 package line;
- `xunit.runner.visualstudio 3.1.5` was selected because it is a modern VSTest adapter that supports .NET 8+ and can run xUnit 2 tests;
- `xunit.v3` remains deferred to Delivery 05.

`Microsoft.Data.Sqlite` was updated only in the core test project because it is a direct test-only dependency used by Dapper integration tests. A vulnerable transitive `SQLitePCLRaw.lib.e_sqlite3 2.1.11` remains after the update and should be reviewed in Delivery 04 or a dedicated dependency-hardening task rather than adding unrelated overrides in this migration step.
