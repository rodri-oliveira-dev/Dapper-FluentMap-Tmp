# Etapa 6 Handoff

## Last Completed Delivery

02 — Mapping State Encapsulation

## Current Architecture

`FluentMapper` remains a process-wide facade over static configuration state:

- static `MappingRegistry`;
- static `FluentMapConfiguration`;
- public mutable `EntityMaps`;
- public mutable `TypeConventions`;
- Dapper global type-map integration through `SqlMapper.SetTypeMap`.

Delivery 02 added read-only snapshot APIs:

- `FluentMapper.GetEntityMaps()`;
- `FluentMapper.GetTypeConventions()`.

These APIs return snapshot collections for inspection and do not expose the live `ConcurrentDictionary` instances or mutable convention lists.

The supported lifecycle is now documented as:

```text
Configuration Phase
        |
        v
Operational Phase
```

Configuration should happen during startup or before first use of the affected types. Once queries begin, effective configuration should be treated as read-only. Runtime mutation through public APIs remains possible only as a compatibility behavior under external quiescence.

Profiles remain query-scoped through `QueryMapped<TEntity,TProfile>()` and do not swap the Dapper global type map.

## Decisions That Must Be Preserved

- E6-D001 - Configuration lifecycle is startup configuration followed by read-only operation.
- E6-D002 - Delivery 01 chose Documentation Contract Only; no `Freeze()`, no sealing API and no runtime enforcement.
- E6-D003 - Profiles remain query-scoped and must not be implemented by temporary `SqlMapper.SetTypeMap` mutation.
- E6-D004 - Mapping state read-only snapshots are the minor-compatible encapsulation path; mutable public fields remain legacy compatibility surface.

## Mapping State After Delivery 02

Official mutation paths still go through `FluentMapConfiguration` and `MappingRegistry`:

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

Read-only inspection should use `GetEntityMaps()` and `GetTypeConventions()`. These are snapshots, so later registrations do not mutate a previously returned view.

## Public Compatibility Surfaces Still Present

- `FluentMapper.EntityMaps` remains `public static readonly ConcurrentDictionary<Type, IEntityMap>`.
- `FluentMapper.TypeConventions` remains `public static readonly ConcurrentDictionary<Type, IList<Convention>>`.
- `IEntityMap.PropertyMaps` remains mutable.
- `Convention.PropertyMaps` and `Convention.ConventionConfigurations` remain mutable.
- Direct mutation through these surfaces remains possible and can bypass registry invariants.

## Registry Invariants

- Official `AddMap(...)` validates before storage.
- Official `AddProfile<TMap>()` validates before storage.
- Official convention/naming-policy registration validates before storage.
- Duplicate default maps and duplicate profiles remain rejected by the registry.
- Profiles remain stored in `ProfileMaps[(EntityType, ProfileType)]` and are not exposed by `GetEntityMaps()`.

## Cache Invariants

- Official default map registration invalidates property-map and materialization-plan cache entries for the entity type and reinstalls the Dapper type map.
- Official profile registration invalidates caches for the entity type and does not call `SqlMapper.SetTypeMap`.
- Official convention/naming-policy registration invalidates caches for the entity type and reinstalls the Dapper type map.
- Direct mutation of legacy public dictionaries does not invalidate caches.

## Decisions Delivery 03 Must Preserve

- Do not make profiles visible to `Dapper.Query<T>()` by mutating global Dapper type maps per operation.
- Do not duplicate validation or cache invalidation outside `MappingRegistry`.
- Prefer adapters around Dapper compatibility boundaries over widening public mutable state.
- Keep `GetEntityMaps()` and `GetTypeConventions()` as read-only inspection snapshots.
- Treat removal or type changes of `EntityMaps`/`TypeConventions` as future major-version work.

## Remaining Dapper-Specific Technical Debt

- `SqlMapper.SetTypeMap` remains process-global state.
- Dommel still reads public legacy mapping dictionaries directly.
- Profiles are not supported through `Dapper.Query<T>()` or Dapper multi-mapping.
- The runtime materializer still uses reflection/dynamic-code paths for `QueryMapped*`.
- Dapper compatibility details around type maps, constructor mapping and handlers remain candidates for Delivery 03.

## Files Changed In Delivery 02

- `README.md`
- `docs/sdd/fluentmap-risk-assessment.md`
- `docs/sdd/etapa-6/README.md`
- `docs/sdd/etapa-6/decisions.md`
- `docs/sdd/etapa-6/handoff.md`
- `docs/sdd/etapa-6/02-mapping-state-encapsulation.md`
- `src/Dapper.FluentMap/FluentMapper.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `test/Dapper.FluentMap.Tests/MappingStateEncapsulationTests.cs`

## Public API Impact

New public API was added:

- `FluentMapper.GetEntityMaps()`;
- `FluentMapper.GetTypeConventions()`.

No public API was removed, renamed or marked obsolete.

The public documentation now states:

- configure during startup;
- optionally call `FluentMapper.Validate()`;
- treat configuration as read-only once queries begin;
- runtime mutation after queries is compatibility-only and requires external quiescence;
- direct dictionary mutation is legacy and can bypass validation, cache invalidation and Dapper type-map installation.
- read-only inspection should use snapshot APIs.

## Remaining Risks

- FM-RISK-001 remains mitigated, not resolved: global FluentMap/Dapper state still exists.
- FM-RISK-002 remains open with mitigation: read-only snapshots now exist, but public mutable dictionaries can still bypass registry validation/cache invalidation.
- Test assemblies still disable parallelization because of global state.
- There is still no immutable snapshot registry.
- There is still no runtime enforcement of the lifecycle boundary.

## Preconditions for Delivery 03

- Read `docs/sdd/etapa-6/01-configuration-lifecycle.md`, `docs/sdd/etapa-6/02-mapping-state-encapsulation.md` and `docs/sdd/etapa-6/decisions.md`.
- Preserve source/binary compatibility unless a future major-version plan is explicit.
- Treat public dictionaries as compatibility debt, not as implementation detail that can be removed.
- Use existing tests in `ConfigurationLifecycleTests`, `MappingRegistryTests`, `DiagnosticsApiTests` and `MappingProfileTests` as lifecycle baseline.
- Keep Dommel out of scope unless a core change provably requires review.

## Things Delivery 03 Must Not Assume

- Do not assume `Initialize(...)` is currently one-shot.
- Do not assume runtime mutation can be forbidden in a minor-compatible change.
- Do not assume public dictionary mutation triggers registry validation, cache invalidation or `SqlMapper.SetTypeMap`.
- Do not assume profiles are visible to `Dapper.Query<T>()` or Dommel.
- Do not assume test parallelization can be re-enabled before global state is encapsulated or isolated.
- Do not add a freeze/seal API without a compatibility and migration decision.
