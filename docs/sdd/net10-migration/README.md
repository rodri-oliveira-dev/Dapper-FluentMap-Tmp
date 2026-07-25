# .NET 10 test migration

## Objective

Document and coordinate the migration of the test projects from `netcoreapp3.1` to `net10.0`, with controlled dependency updates for `src/` and `test/` projects.

The migration must preserve the published library targets:

- `src/Dapper.FluentMap` stays on `netstandard2.0`.
- `src/Dapper.FluentMap.Dommel` stays on `netstandard2.0`.
- The `src/` projects must remain consumable by `net10.0` applications and tests.

This folder is the persistent handoff source for the five independent chats. Future deliveries must read these files instead of relying on memory from previous conversations.

## Shared Branch

Branch: `chore/net10-migration`

Do not push this branch unless a later prompt explicitly asks for it.

## Expected Final Structure

```text
src/
|-- Dapper.FluentMap                 -> netstandard2.0
`-- Dapper.FluentMap.Dommel          -> netstandard2.0

test/
|-- Dapper.FluentMap.Tests           -> net10.0
`-- Dapper.FluentMap.Dommel.Tests    -> net10.0
```

## Delivery Order

1. Inventory and baseline.
2. Migrate test projects to `net10.0`.
3. Update `src/` project dependencies.
4. Complete validation, package inspection, and CI review.
5. Separate migration to xUnit 3.

## Identified Solution and Projects

Solution:

- `Dapper.FluentMap.sln`

Projects:

- `src/Dapper.FluentMap/Dapper.FluentMap.csproj`
- `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj`
- `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj`
- `test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj`

## Repository Validation Commands

Commands documented in `AGENTS.md` for the main library:

```bash
dotnet restore ./Dapper.FluentMap.sln
dotnet build ./src/Dapper.FluentMap/Dapper.FluentMap.csproj --configuration Release --no-restore
dotnet test ./test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj --configuration Release
```

Commands documented in `AGENTS.md` for the full solution:

```bash
dotnet restore ./Dapper.FluentMap.sln
dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build
```

Commands documented in `AGENTS.md` for packaging:

```bash
dotnet pack ./src/Dapper.FluentMap/Dapper.FluentMap.csproj --configuration Release --no-build --output ./artifacts/packages
```

CI files after Delivery 04:

- `.github/workflows/ci.yml`: installs .NET SDK `10.0.x` GA, runs restore, Release build, Release tests, Release pack, and uploads `.nupkg` artifacts.
- `.appveyor.yml`: installs .NET SDK 10 GA on Visual Studio 2022 image, runs restore, Release build, Release tests, Release pack, and stores `.nupkg` artifacts.
- `.travis.yml`: uses `dotnet: 10.0` on `jammy`, runs restore, Release build, Release tests, and Release pack.

No `global.json`, `Directory.Build.props`, `Directory.Packages.props`, or `.editorconfig` files are present after Delivery 04.
