# 02 - Mapping State Encapsulation

## Current Exposure

`FluentMapper.EntityMaps` exposes:

```csharp
public static readonly ConcurrentDictionary<Type, IEntityMap> EntityMaps
```

`FluentMapper.TypeConventions` exposes:

```csharp
public static readonly ConcurrentDictionary<Type, IList<Convention>> TypeConventions
```

Both fields are `readonly` only at the field-reference level. Consumers cannot assign a different dictionary to the field, but they can mutate the exposed `ConcurrentDictionary` instance and the mutable `IList<Convention>` values.

Available mutation operations include `TryAdd`, index assignment, `Remove`, `Clear` and explicit interface `Add` through `IDictionary<TKey,TValue>` / `ICollection<KeyValuePair<TKey,TValue>>`.

Delivery 01 is confirmed as `COMPLETED` in `docs/sdd/etapa-6/README.md`. Its lifecycle decision is authoritative for this delivery:

```text
Configuration Phase
        |
        v
Operational Phase
```

Configuration after the operational phase remains compatibility-only and requires external quiescence. Direct dictionary mutation is legacy compatibility debt, not a supported deterministic configuration path.

## Mutation Paths

| Mutation path | Validates | Invalidates cache | Updates Dapper | Supported |
| ------------- | --------- | ----------------- | -------------- | --------- |
| `Initialize(c => c.AddMap<TEntity>(map))` | Yes | Yes | Yes | Yes, during configuration phase |
| `Initialize(c => c.AddMap<TMap>())` | Yes | Yes | Yes | Yes, during configuration phase |
| `Initialize(c => c.AddMapsFromAssembly(...))` | Yes | Yes | Yes | Yes, trimming-sensitive |
| `Initialize(c => c.AddProfile<TMap>())` | Yes | Yes | No | Yes, query-scoped profiles only |
| `Initialize(c => c.AddConvention<T>().ForEntity<TEntity>())` | Yes | Yes | Yes | Yes, during configuration phase |
| `Initialize(c => c.UseNamingPolicy(...).ForEntity<TEntity>())` | Yes | Yes | Yes | Yes, during configuration phase |
| `((IDictionary<Type, IEntityMap>)FluentMapper.EntityMaps).Add(...)` | No | No | No | Legacy compatibility only |
| `FluentMapper.EntityMaps.TryAdd(...)` | No | No | No | Legacy compatibility only |
| `FluentMapper.EntityMaps[type] = map` | No | No | No | Legacy compatibility only |
| `FluentMapper.EntityMaps.Remove(...)` | No | No | No | Legacy compatibility only |
| `FluentMapper.EntityMaps.Clear()` | No | No | No | Legacy compatibility only |
| `((IDictionary<Type, IList<Convention>>)FluentMapper.TypeConventions).Add(...)` | No | No | No | Legacy compatibility only |
| `FluentMapper.TypeConventions.TryAdd(...)` | No | No | No | Legacy compatibility only |
| `FluentMapper.TypeConventions[type] = list` | No | No | No | Legacy compatibility only |
| `FluentMapper.TypeConventions[type].Add(...)` | No | No | No | Legacy compatibility only |
| `FluentMapper.TypeConventions.Remove(...)` | No | No | No | Legacy compatibility only |
| `FluentMapper.TypeConventions.Clear()` | No | No | No | Legacy compatibility only |
| `FluentMapper.Reset(...)` | No public validation | Clears all caches | Removes requested type maps | Internal test isolation only |

## Problem

`MappingRegistry` is the intended mutation boundary. It validates entity maps, validates conventions, invalidates property-map/materialization-plan caches, and installs the default Dapper type map through `SqlMapper.SetTypeMap`.

The public dictionaries expose the registry storage directly. As a result, consumers can add, replace or remove maps and conventions without the registry observing the mutation. This can produce stale cache entries, missing Dapper type-map installation, or diagnostics that disagree with query behavior.

## Compatibility Constraints

- The public fields cannot be removed in this delivery without source and binary breakage.
- Changing their declared type from `ConcurrentDictionary<...>` to a read-only interface would be source and binary breaking.
- Replacing the instances with non-mutable wrappers is impossible without changing the field type.
- Marking the fields with `[Obsolete]` is source-compatible and binary-compatible, but can break consumers that treat warnings as errors.
- Blocking runtime mutation would contradict Delivery 01 unless a major-version migration is planned.
- `Dommel` currently reads these fields directly; this delivery must not redesign Dommel.

Compatibility impact matrix:

| Possible change | Source breaking | Binary breaking | Behavior breaking | Decision |
| --------------- | --------------- | --------------- | ----------------- | -------- |
| Remove public fields | Yes | Yes | Yes | Rejected |
| Change field types to read-only interfaces | Yes | Yes | Yes | Rejected |
| Replace fields with read-only properties of same names | Yes | Yes | Yes | Rejected |
| Keep fields and add read-only APIs | No | No | No | Accepted |
| Mark fields `[Obsolete]` | Warning-only, but can fail warnings-as-errors builds | No | No | Deferred |
| Throw on runtime mutation through official APIs | No | No | Yes | Rejected for this delivery |
| Detect all external direct mutations | No reliable path with current field types | No reliable path with current field types | Could be partial/inconsistent | Rejected |

## Goals

- Provide official read-only accessors for mapping state inspection.
- Keep new read-only access snapshot-based so consumers cannot mutate registry collections through the new API.
- Keep all official mutations conceptually behind `FluentMapper -> MappingRegistry`.
- Preserve existing public fields for source and binary compatibility.
- Document direct dictionary mutation as a legacy compatibility surface.
- Preserve lifecycle, precedence, profiles, naming policies, inherited maps, cache invalidation and Dapper integration.

## Non-Goals

- Remove `EntityMaps` or `TypeConventions`.
- Change the declared type of existing public fields.
- Add runtime freezing/sealing.
- Detect every possible direct mutation of legacy dictionaries.
- Make `IEntityMap`, `Convention` or `PropertyMap` immutable.
- Redesign Dommel.
- Implement a generated materializer.

## Proposed Encapsulation Strategy

Add snapshot-based read-only APIs:

```csharp
FluentMapper.GetEntityMaps()
FluentMapper.GetTypeConventions()
```

These APIs return read-only snapshots of the current default entity maps and type conventions. The snapshots do not expose `ConcurrentDictionary` or mutable convention lists. They are intended for diagnostics, inspection and migration away from direct dictionary reads.

The existing public fields remain as legacy compatibility surface. Their XML documentation is updated to tell consumers to use fluent registration APIs for mutation and read-only snapshots for inspection.

The registry remains the mutation owner for official APIs:

```text
Consumer API
     |
     v
FluentMapper / FluentMapConfiguration
     |
     v
MappingRegistry
     |
     v
Validation
     |
     v
Cache invalidation
     |
     v
Dapper integration
```

No cache invalidation or validation logic is duplicated outside `MappingRegistry`.

## Migration Strategy

Minor-compatible migration:

- New code should use `Initialize(...)`, `AddMap(...)`, `AddProfile(...)`, convention APIs and naming policies for mutation.
- New code that only needs to inspect mappings should use `GetEntityMaps()` and `GetTypeConventions()`.
- Existing code that mutates `EntityMaps` or `TypeConventions` continues to compile and run, but remains legacy and can bypass invariants.

Future major-version migration:

- Replace public mutable fields with read-only properties.
- Move mutable state behind registry-owned methods only.
- Consider immutable snapshots for effective mapping state after configuration.
- Consider an explicit compatibility adapter for Dommel instead of direct dictionary reads.

## Public API Impact

Added public APIs:

```csharp
public static IReadOnlyDictionary<Type, IEntityMap> GetEntityMaps()
public static IReadOnlyDictionary<Type, IReadOnlyList<Convention>> GetTypeConventions()
```

Preserved public APIs:

```csharp
public static readonly ConcurrentDictionary<Type, IEntityMap> EntityMaps
public static readonly ConcurrentDictionary<Type, IList<Convention>> TypeConventions
```

The legacy fields are not marked `[Obsolete]` in this delivery. The reason is compatibility risk for consumers that compile with warnings as errors.

## Internal API Impact

`MappingRegistry` adds snapshot builders for entity maps and type conventions. They copy the dictionary contents and copy convention lists into read-only collections.

No registry mutation rule is moved out of `MappingRegistry`.

## Implementation

Implemented:

- `FluentMapper.GetEntityMaps()`;
- `FluentMapper.GetTypeConventions()`;
- `MappingRegistry.GetEntityMapsSnapshot()`;
- `MappingRegistry.GetTypeConventionsSnapshot()`;
- XML documentation remarks on `EntityMaps` and `TypeConventions`;
- README guidance to prefer snapshots for read-only inspection.

Not implemented:

- `[Obsolete]` attributes on legacy fields;
- freeze/seal lifecycle enforcement;
- automatic direct-mutation detection;
- immutable effective mapping state;
- Dommel redesign.

## Acceptance Criteria

- Delivery 02 SDD document exists.
- Stage README marks Delivery 02 progress and final completion.
- `EntityMaps` and `TypeConventions` public signatures are documented.
- Mutation paths and bypass behavior are documented.
- Official read-only snapshot APIs are implemented and tested.
- Official map/convention registration still validates, invalidates cache and updates Dapper as before.
- Profile behavior remains query-scoped and does not update Dapper type maps.
- Legacy direct mutation remains possible but is characterized as bypassing registry invariants.
- FM-RISK-001 and FM-RISK-002 are reviewed.
- Restore, build, tests and pack are executed and recorded.
- A single semantic commit is created.

## Residual Risk

Direct mutation remains possible through `EntityMaps`, `TypeConventions`, mutable `IEntityMap.PropertyMaps`, mutable `Convention.PropertyMaps` and mutable `Convention.ConventionConfigurations`. Because the public field signatures expose concrete mutable collections, full prevention requires a major-version compatibility break.

The new read-only APIs reduce risk for inspection and migration, but they do not make the legacy surface safe.

## Validation Results

Environment:

- SDK: `10.0.302`
- test runner detected: VSTest with xUnit v3
- core target: `netstandard2.0`
- test target: `net10.0`

Localized validation:

```text
dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter "FullyQualifiedName~MappingStateEncapsulationTests"
```

Result:

- success;
- 6 tests passed.

Mandatory validation:

```text
dotnet restore .\Dapper.FluentMap.sln
dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build
dotnet pack .\Dapper.FluentMap.sln --configuration Release --no-build --output .\artifacts\packages
```

Results:

- restore: success;
- build: success, 0 warnings, 0 errors;
- tests: success, 221 total tests passed:
  - core: 190;
  - Dommel: 7;
  - analyzers: 9;
  - generators: 14;
  - generated-registration integration: 1;
- pack: success:
  - `Dapper.FluentMap.2.0.0.nupkg`;
  - `Dapper.FluentMap.Dommel.2.0.0.nupkg`;
  - `Dapper.FluentMap.Analyzers.2.0.0.nupkg`;
  - `Dapper.FluentMap.Generators.2.0.0.nupkg`.

Known pack warnings:

- `NU5125` for legacy `PackageLicenseUrl` in core and Dommel;
- NuGet README recommendation for core and Dommel.

These warnings are pre-existing package metadata debt tracked outside this delivery.
