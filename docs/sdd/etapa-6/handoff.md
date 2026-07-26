# Etapa 6 Handoff

## Last Completed Delivery

01 - Configuration Lifecycle

## Current Architecture

`FluentMapper` remains a process-wide facade over static configuration state:

- static `MappingRegistry`;
- static `FluentMapConfiguration`;
- public mutable `EntityMaps`;
- public mutable `TypeConventions`;
- Dapper global type-map integration through `SqlMapper.SetTypeMap`.

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

## Files Changed

- `README.md`
- `docs/sdd/fluentmap-risk-assessment.md`
- `docs/sdd/etapa-6/README.md`
- `docs/sdd/etapa-6/decisions.md`
- `docs/sdd/etapa-6/handoff.md`
- `docs/sdd/etapa-6/01-configuration-lifecycle.md`
- `test/Dapper.FluentMap.Tests/ConfigurationLifecycleTests.cs`

## Public API Impact

No public API was added, removed, renamed or marked obsolete.

The public documentation now states:

- configure during startup;
- optionally call `FluentMapper.Validate()`;
- treat configuration as read-only once queries begin;
- runtime mutation after queries is compatibility-only and requires external quiescence;
- direct dictionary mutation is legacy and can bypass validation, cache invalidation and Dapper type-map installation.

## Remaining Risks

- FM-RISK-001 remains mitigated, not resolved: global FluentMap/Dapper state still exists.
- FM-RISK-002 remains open: public mutable dictionaries can still bypass registry validation/cache invalidation.
- Test assemblies still disable parallelization because of global state.
- There is still no immutable snapshot registry.
- There is still no runtime enforcement of the lifecycle boundary.

## Preconditions for Delivery 02

- Read `docs/sdd/etapa-6/01-configuration-lifecycle.md` and `docs/sdd/etapa-6/decisions.md`.
- Preserve source/binary compatibility unless a future major-version plan is explicit.
- Treat public dictionaries as compatibility debt, not as implementation detail that can be removed.
- Use existing tests in `ConfigurationLifecycleTests`, `MappingRegistryTests`, `DiagnosticsApiTests` and `MappingProfileTests` as lifecycle baseline.
- Keep Dommel out of scope unless a core change provably requires review.

## Things Delivery 02 Must Not Assume

- Do not assume `Initialize(...)` is currently one-shot.
- Do not assume runtime mutation can be forbidden in a minor-compatible change.
- Do not assume public dictionary mutation triggers registry validation, cache invalidation or `SqlMapper.SetTypeMap`.
- Do not assume profiles are visible to `Dapper.Query<T>()` or Dommel.
- Do not assume test parallelization can be re-enabled before global state is encapsulated or isolated.
- Do not add a freeze/seal API without a compatibility and migration decision.
