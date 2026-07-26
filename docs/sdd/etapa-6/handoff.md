# Etapa 6 Handoff

## Last Completed Delivery

03 - Dapper Compatibility Adapters

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

The supported lifecycle is:

```text
Configuration Phase
        |
        v
Operational Phase
```

Configuration should happen during startup or before first use of the affected types. Once queries begin, effective configuration should be treated as read-only. Runtime mutation through public APIs remains possible only as a compatibility behavior under external quiescence.

Profiles remain query-scoped through `QueryMapped<TEntity,TProfile>()` and do not swap the Dapper global type map.

## Dapper Compatibility Boundary

Delivery 03 added an internal compatibility boundary in `Dapper.FluentMap.Compatibility`:

- `DapperTypeHandlerAdapter`: centralizes residual reflection into `SqlMapper.TypeHandlerCache<T>.Parse(object)`;
- `DapperFluentPropertyTypeMap`: exposes FluentMap property mappings to Dapper without using `CustomPropertyTypeMap`;
- `DapperPropertyMemberMap`: safe `SqlMapper.IMemberMap` for simple property mappings;
- `DapperIgnoredMemberMap`: safe `SqlMapper.IMemberMap` marker for ignored mappings and FluentMap-controlled nested paths.

The intended shape is:

```text
FluentMap materialization/type maps
        |
        v
internal Dapper compatibility boundary
        |
        v
Dapper-specific behavior
```

`NestedMaterializationPlan` should not grow new direct reflection into Dapper internals. Future Dapper-specific workarounds should go through this compatibility boundary or a similarly explicit adapter.

## Remaining Reflection Into Dapper Internals

Residual reflection remains only for TypeHandler invocation in `DapperTypeHandlerAdapter`:

```text
SqlMapper.TypeHandlerCache<T>.Parse(object)
```

Dapper `2.1.79` exposes `SqlMapper.HasTypeHandler(type)` publicly, but no public API was found to convert a single `object` through the registered handler for an arbitrary target type. Because of that, `FM-RISK-007` is `MITIGATED`, not `RESOLVED`.

If the expected Dapper shape is missing, the adapter throws `FluentMapConfigurationException` with an upgrade-oriented diagnostic instead of silently falling back to `Convert.ChangeType`.

## Ignored Mapping Strategy

`IgnoredPropertyInfo` was removed.

Ignored root mappings and FluentMap-controlled nested paths now flow as:

```text
DapperFluentPropertyTypeMap.GetMember(column)
        |
        v
DapperIgnoredMemberMap
        |
        v
MultiTypeMap returns null without consulting DefaultTypeMap
```

This preserves the existing behavior that ignored/nested FluentMap mappings block Dapper fallback for that column, while removing the previous `PropertyInfo` sentinel whose members threw `NotImplementedException`.

`FM-RISK-012` is `RESOLVED`.

## Dapper Upgrade Checklist

Before upgrading Dapper, review:

- `SqlMapper.ITypeMap`;
- `SqlMapper.IMemberMap`;
- `DefaultTypeMap` constructor and member behavior;
- `SqlMapper.SetTypeMap` global behavior;
- `SqlMapper.HasTypeHandler`;
- `SqlMapper.TypeHandlerCache<T>.Parse(object)`;
- fallback behavior when a mapper returns `null`;
- `DapperCompatibilityAdapterTests`;
- `ValueObjectMaterializationTests`;
- `NestedMaterializationSpikeTests`;
- `NestedObjectMaterializationTests`;
- `ConstructorMappingTests`;
- `DapperIntegrationTests`;
- Dommel tests.

Do not update Dapper merely to test the adapter. Treat dependency upgrade as its own specification.

## Materialization Architecture Relevant to Delivery 04

`QueryMapped*` still uses runtime `DbDataReader` materialization with cached `NestedMaterializationPlan`.

The plan remains responsible for:

- resolving the effective map by entity, optional profile and column shape;
- preserving full `MemberPath` identity;
- deciding nested/null subtree behavior;
- invoking constructors for immutable objects and Value Objects;
- applying Dapper TypeHandlers for scalar mapped properties through `DapperTypeHandlerAdapter`;
- falling back to local conversion when no handler is registered.

`Dapper.Query<T>` remains Dapper-owned and should continue to use the default type map installed by `SqlMapper.SetTypeMap`.

## Constraints the Generated Materializer Spike Must Preserve

Delivery 04 must preserve:

- explicit mapping before convention/naming policy before Dapper default;
- query-scoped profiles without temporary `SqlMapper.SetTypeMap` mutation;
- `MemberPath` identity for same-terminal nested paths;
- ignored mappings blocking Dapper/default fallback for their configured columns;
- TypeHandler behavior for scalar Value Object properties;
- nullable TypeHandler null semantics;
- diagnostic failure when Dapper compatibility internals are invalid;
- configuration lifecycle from Delivery 01;
- read-only snapshot behavior from Delivery 02;
- no public API additions unless the generated materializer specification explicitly justifies them;
- no Dommel redesign unless core changes require it.

## Decisions That Must Be Preserved

- E6-D001 - Configuration lifecycle is startup configuration followed by read-only operation.
- E6-D002 - Delivery 01 chose Documentation Contract Only; no `Freeze()`, no sealing API and no runtime enforcement.
- E6-D003 - Profiles remain query-scoped and must not be implemented by temporary `SqlMapper.SetTypeMap` mutation.
- E6-D004 - Mapping state read-only snapshots are the minor-compatible encapsulation path; mutable public fields remain legacy compatibility surface.
- E6-D005 - Dapper compatibility details are isolated behind internal adapters.
- E6-D006 - Residual TypeHandler reflection remains isolated and diagnostic.
- E6-D007 - Ignored mappings use safe `IMemberMap` markers, not throwing `PropertyInfo` sentinels.
- E6-D008 - Dapper upgrades require targeted compatibility review.

## Mapping State After Delivery 03

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

## Remaining Dapper-Specific Technical Debt

- `SqlMapper.SetTypeMap` remains process-global state.
- Dommel still reads public legacy mapping dictionaries directly.
- Profiles are not supported through `Dapper.Query<T>()` or Dapper multi-mapping.
- The runtime materializer still uses reflection/dynamic-code paths for `QueryMapped*`.
- Residual TypeHandler invocation still reflects into `SqlMapper.TypeHandlerCache<T>.Parse(object)`, isolated by `DapperTypeHandlerAdapter`.

## Files Changed In Delivery 03

- `README.md`
- `docs/sdd/fluentmap-risk-assessment.md`
- `docs/sdd/etapa-6/README.md`
- `docs/sdd/etapa-6/decisions.md`
- `docs/sdd/etapa-6/handoff.md`
- `docs/sdd/etapa-6/03-dapper-compatibility-adapters.md`
- `src/Dapper.FluentMap/Compatibility/DapperTypeHandlerAdapter.cs`
- `src/Dapper.FluentMap/Compatibility/DapperFluentPropertyTypeMap.cs`
- `src/Dapper.FluentMap/Compatibility/DapperPropertyMemberMap.cs`
- `src/Dapper.FluentMap/Compatibility/DapperIgnoredMemberMap.cs`
- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentMapTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentConventionTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/MultiTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/IgnoredPropertyInfo.cs` removed
- `test/Dapper.FluentMap.Tests/DapperCompatibilityAdapterTests.cs`

## Public API Impact

Delivery 03 added no public API and removed no public API.

The internal `IgnoredPropertyInfo` implementation detail was removed. FluentMap public behavior for ignored mappings, nested paths, Dapper fallback, TypeHandlers and profiles is preserved.

## Remaining Risks

- FM-RISK-001 remains mitigated, not resolved: global FluentMap/Dapper state still exists.
- FM-RISK-002 remains open with mitigation: read-only snapshots now exist, but public mutable dictionaries can still bypass registry validation/cache invalidation.
- FM-RISK-007 remains mitigated, not resolved: TypeHandler invocation still reflects into Dapper internals, but only through `DapperTypeHandlerAdapter`.
- FM-RISK-012 is resolved: `IgnoredPropertyInfo` no longer exists.
- Test assemblies still disable parallelization because of global state.
- There is still no immutable snapshot registry.
- There is still no runtime enforcement of the lifecycle boundary.

## Preconditions for Delivery 04

- Read `docs/sdd/etapa-6/01-configuration-lifecycle.md`, `docs/sdd/etapa-6/02-mapping-state-encapsulation.md`, `docs/sdd/etapa-6/03-dapper-compatibility-adapters.md` and `docs/sdd/etapa-6/decisions.md`.
- Preserve source/binary compatibility unless a future major-version plan is explicit.
- Treat public dictionaries as compatibility debt, not as implementation detail that can be removed.
- Use existing tests in `ConfigurationLifecycleTests`, `MappingRegistryTests`, `DiagnosticsApiTests`, `MappingProfileTests`, `DapperCompatibilityAdapterTests`, `ValueObjectMaterializationTests` and `DapperIntegrationTests` as baseline.
- Keep Dommel out of scope unless a core change provably requires review.

## Things Delivery 04 Must Not Assume

- Do not assume `Initialize(...)` is currently one-shot.
- Do not assume runtime mutation can be forbidden in a minor-compatible change.
- Do not assume public dictionary mutation triggers registry validation, cache invalidation or `SqlMapper.SetTypeMap`.
- Do not assume profiles are visible to `Dapper.Query<T>()` or Dommel.
- Do not assume test parallelization can be re-enabled before global state is encapsulated or isolated.
- Do not add a freeze/seal API without a compatibility and migration decision.
- Do not add new direct reflection into Dapper internals outside the compatibility boundary.
