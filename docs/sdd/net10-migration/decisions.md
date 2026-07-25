# Cross-Delivery Decisions

## Preserve `netstandard2.0` for Published Projects

`src/Dapper.FluentMap` and `src/Dapper.FluentMap.Dommel` must remain on `netstandard2.0` throughout the migration. Dependency updates in Delivery 03 must not force either published project to move to `net8.0`, `net10.0`, or multi-targeting.

## Keep xUnit 2 Until Delivery 05

Delivery 02 may update xUnit 2 packages to the latest stable xUnit 2 line, but must not migrate test code or project references to `xunit.v3`. The xUnit 3 migration is intentionally isolated in Delivery 05.

## Separate Test Runtime Migration From Source Dependency Updates

Delivery 02 should focus on test projects and test-only packages needed for `net10.0`. Delivery 03 should focus on direct dependencies of `src/` projects (`Dapper` and `Dommel`) after the tests can run on a supported runtime.

## Use `net10.0` Tests as Consumer Validation

The primary compatibility check for the published `netstandard2.0` projects is to run the `net10.0` test projects while they reference the `src/` projects. Delivery 04 should add package inspection with `dotnet pack` to confirm the published assemblies and dependency groups still target `netstandard2.0`.
