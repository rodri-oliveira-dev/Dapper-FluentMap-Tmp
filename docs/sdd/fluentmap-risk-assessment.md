# FluentMap - Consolidated Risk Assessment

## 1. Executive Summary

This assessment reconstructs the FluentMap SDD history from `docs/sdd/etapa-1` through `docs/sdd/etapa-5`, plus the `.NET 10` migration and the SQLite dependency hardening. It consolidates only risks that still have current evidence as `OPEN`, `MITIGATED`, or `UNKNOWN`. Historical items that were later resolved or superseded are listed separately in section 10.

Development history reconstructed from repository evidence:

```text
Etapa 1
  - 01 ReflectionHelper
  - 02 Mapping composition
  - 03 Dapper integration tests
  - 04 MappingRegistry and cache

Etapa 2
  - 01 MemberPath
  - 02 Configuration validation and diagnostics
  - 03 Inherited mappings
  - 04 Naming policies

Etapa 3
  - 01 Mapping registration and discovery
  - 02 Constructor mapping and immutable types
  - 03 Validate and Explain

Etapa 4
  - 01 Roslyn analyzers
  - 02 Trimming and Native AOT
  - 03 Source generator

Etapa 5
  - 01 Nested/value-object materialization spike
  - 02 Nested object materialization
  - 03 Immutable value objects
  - 04 Mapping profiles

.NET 10 migration
  - 01 Inventory and baseline
  - 02 Test projects on net10.0
  - 03 Source project dependencies
  - 04 Validation, pack and CI
  - 05 xUnit 3 migration

Security hardening
  - SQLitePCLRaw vulnerability correction
```

Current risk count:

- Total current items: 18
- Critical: 0
- High: 3
- Medium: 10
- Low: 5
- Open: 9
- Mitigated: 6
- Resolved: 1
- Unknown: 2

Overall, FluentMap is not in a critical architectural state. The main runtime contract is well protected by SDD decisions, integration tests, fail-fast validation, `MemberPath`, cache keys, and query-scoped profiles. The largest remaining risks come from intentionally preserved global/static compatibility surfaces, trimming/AOT constraints, and the fact that the new materializer is runtime/reflection-based rather than generated.

## 2. Risk Distribution

| Severity | Count |
| -------- | ----: |
| Critical | 0 |
| High     | 3 |
| Medium   | 10 |
| Low      | 5 |

## 3. Priority Matrix

| ID | Problem | Severity | Probability | Area | Origin | Status |
| -- | ------- | -------- | ----------- | ---- | ------ | ------ |
| FM-RISK-001 | Global FluentMap/Dapper mapping state constrains thread safety and runtime reconfiguration | HIGH | Medium | Concurrency, Thread Safety | Etapa 1 / Entrega 03-04 | MITIGATED |
| FM-RISK-002 | Public mutable dictionaries can bypass registry validation and cache invalidation | HIGH | Medium | Architecture, Compatibility | Etapa 1 / Entrega 04 | OPEN |
| FM-RISK-003 | Assembly scanning can fail under trimming/AOT and produced a failing trimmed smoke | HIGH | Medium | Reflection, Compatibility | Etapa 3 / Entrega 01; Etapa 4 / Entrega 02 | MITIGATED |
| FM-RISK-004 | `QueryMapped*` remains runtime/reflection/dynamic-code based; no generated materializer exists | MEDIUM | Medium | AOT, Performance | Etapa 5 / Entrega 02-04 | MITIGATED |
| FM-RISK-005 | `QueryMapped*` buffers all rows and has no streaming/unbuffered mode | MEDIUM | Medium | Performance, Memory | Etapa 5 / Entrega 04 | OPEN |
| FM-RISK-006 | Value Object support excludes factories, private constructors/setters, fields and NRT semantics | MEDIUM | Medium | Value Objects, API Design | Etapa 5 / Entrega 03 | OPEN |
| FM-RISK-007 | Dapper TypeHandler integration in the runtime materializer depends on reflective access to `SqlMapper.TypeHandlerCache<T>` | MEDIUM | Low | Compatibility, Reflection | Etapa 5 / Entrega 03 | MITIGATED |
| FM-RISK-008 | Mapping profiles do not support per-profile conventions/naming policies | MEDIUM | Medium | Profiles, Extensibility | Etapa 5 / Entrega 04 | OPEN |
| FM-RISK-009 | Mapping profiles do not apply to `Dapper.Query<T>` or Dapper multi-mapping | MEDIUM | Medium | Profiles, API Design | Etapa 5 / Entrega 04 | OPEN |
| FM-RISK-010 | Legacy `ApplyMapsFromAssemblies` keeps older reflection/discovery behavior | MEDIUM | Low | Reflection, Maintainability | Etapa 2 / Entrega 02; Etapa 3 / Entrega 01 | MITIGATED |
| FM-RISK-011 | Constructor overload ambiguity and optional parameters remain delegated to Dapper | MEDIUM | Low | Materialization, Correctness | Etapa 3 / Entrega 02 | OPEN |
| FM-RISK-012 | Throwing `IgnoredPropertyInfo` sentinel was removed from ignored/nested mapping paths | MEDIUM | Low | Correctness, Maintainability | Etapa 2 / Entrega 02; Etapa 6 / Entrega 03 | RESOLVED |
| FM-RISK-013 | Dommel behavior for profiles/nested materialization is intentionally unreviewed | MEDIUM | Low | Dommel, Extensibility | Etapa 5 / Entrega 04 | UNKNOWN |
| FM-RISK-014 | Analyzer and generator coverage is intentionally partial | LOW | High | Developer Experience, Testing | Etapa 4 / Entrega 01-03 | MITIGATED |
| FM-RISK-015 | Async `QueryMapped*` overloads are asymmetric: profile async exists, default async does not | LOW | Medium | API Design | Etapa 5 / Entrega 04 | OPEN |
| FM-RISK-016 | NuGet package metadata remains legacy (`PackageLicenseUrl`, no package README/SourceLink metadata) | LOW | High | Documentation, Developer Experience, Packaging | .NET 10 / Entrega 04; Security hardening | OPEN |
| FM-RISK-017 | Remote CI execution remains unproven after CI modernization | LOW | Medium | Testing, Maintainability | .NET 10 / Entrega 04-05 | UNKNOWN |
| FM-RISK-018 | Documentation carries archived/legacy signals alongside new SDD features | LOW | Medium | Documentation | README and SDD summaries | OPEN |

## 4. Critical Risks

No critical risks were identified from the available evidence.

## 5. High Risks

## FM-RISK-001 - Global FluentMap/Dapper mapping state constrains thread safety and runtime reconfiguration

**Severidade:** HIGH
**Status:** MITIGATED
**Categoria:** Architecture, Concurrency, Thread Safety, Mapping, Compatibility
**Origem:** Etapa 1 / Entregas 03 and 04
**Detectado em:** planning, implementation and test review
**Componentes afetados:** `FluentMapper`, `MappingRegistry`, `SqlMapper.SetTypeMap`, tests using global reset

### Descricao

FluentMap still relies on process-wide mapping state and Dapper's global type-map registry. The code now uses `ConcurrentDictionary`, structured cache keys and invalidation, but the architecture remains global. Runtime reconfiguration while queries are executing is not proven safe as a public contract.

### Evidencias

- `docs/sdd/etapa-1/03-dapper-integration-tests.md`: identifies static `EntityMaps`, static `TypeConventions`, static `_configuration`, `SqlMapper.SetTypeMap`, cache interference and disabled parallelism.
- `docs/sdd/etapa-1/04-mapping-registry-cache.md`: resolves cache key and reset issues but explicitly keeps public mutable dictionaries, Dapper global type maps and disabled parallelism.
- `docs/sdd/net10-migration/05-xunit3-migration.md`: preserves `[assembly: CollectionBehavior(DisableTestParallelization = true)]` because tests use global FluentMapper/Dapper/Dommel state.
- `src/Dapper.FluentMap/FluentMapper.cs`: static `_registry`, static `_configuration`, public static `EntityMaps` and `TypeConventions`.
- `src/Dapper.FluentMap/MappingRegistry.cs`: `SetDapperTypeMap` calls `SqlMapper.SetTypeMap(type, instance)`.
- `docs/sdd/etapa-6/02-mapping-state-encapsulation.md`: adds read-only mapping snapshots while preserving mutable compatibility fields.
- `test/Dapper.FluentMap.Tests/MappingStateEncapsulationTests.cs`: validates official mutation cache/Dapper behavior, read-only snapshots, profile isolation and legacy bypass behavior.
- `test/Dapper.FluentMap.Tests/ManualMappingTests.cs` and `test/Dapper.FluentMap.Dommel.Tests/ManualMappingTests.cs`: assembly-level test parallelization disabled.
- `docs/sdd/etapa-6/01-configuration-lifecycle.md`: defines the supported lifecycle as startup configuration followed by read-only operation, with runtime mutation allowed only under external quiescence for compatibility.
- `test/Dapper.FluentMap.Tests/ConfigurationLifecycleTests.cs`: characterizes repeated additive `Initialize`, serialized runtime registration compatibility, and direct dictionary mutation bypassing Dapper type-map installation.

### Cenario de impacto

An application dynamically reinitializes mappings for the same entity while requests are still materializing rows. One request can observe old mappings, another can observe new mappings, and Dapper's global type-map registry can be replaced mid-flight.

### Impacto

Potential non-deterministic mapping behavior, hard-to-reproduce test failures, and incorrect materialization if consumers treat `Initialize` as a runtime mutation API instead of startup configuration.

### Probabilidade

Media. The normal startup-once usage is safe enough, but the public static shape makes runtime mutation possible and tests remain serialized because of it.

### Workaround atual

Configure FluentMap once during application startup. Avoid mutating mappings after queries begin. Use query-scoped profiles for alternate shapes instead of replacing Dapper type maps.

### Recomendacao

Use the Etapa 6 lifecycle contract as the public boundary: configure during startup, validate, and treat the effective configuration as read-only during operation. Investigate an immutable snapshot registry and reduced public mutability for future versions. Any stronger runtime enforcement must preserve source/binary compatibility or be planned as a major version.

### Relacoes

Related to FM-RISK-002, FM-RISK-005, FM-RISK-013 and the Etapa 5 research item "cache imutavel/snapshot".

## FM-RISK-002 - Public mutable dictionaries can bypass registry validation and cache invalidation

**Severidade:** HIGH
**Status:** OPEN
**Categoria:** Architecture, Correctness, API Design, Compatibility, Technical Debt
**Origem:** Etapa 1 / Entrega 04
**Detectado em:** implementation decision and compatibility review
**Componentes afetados:** `FluentMapper.EntityMaps`, `FluentMapper.TypeConventions`, `MappingRegistry`

### Descricao

`FluentMapper.EntityMaps` and `FluentMapper.TypeConventions` remain public mutable dictionaries for compatibility. Consumers can mutate them directly, bypassing `MappingRegistry.AddEntityMap`, `AddConvention`, validation, cache invalidation and Dapper type-map installation.

### Evidencias

- `docs/sdd/etapa-1/04-mapping-registry-cache.md`: explicitly lists direct public mutation as deliberately unresolved.
- `docs/sdd/etapa-1/decisions.md`: keeps public dictionaries and says reducing their mutability is a compatibility-planned change.
- `docs/sdd/etapa-3/03-diagnostics-api.md`: keeps the dictionaries public for compatibility.
- `src/Dapper.FluentMap/FluentMapper.cs`: exposes `public static readonly ConcurrentDictionary<Type, IEntityMap> EntityMaps` and `public static readonly ConcurrentDictionary<Type, IList<Convention>> TypeConventions`.
- `src/Dapper.FluentMap/FluentMapper.cs`: also exposes `GetEntityMaps()` and `GetTypeConventions()` read-only snapshots as the preferred inspection API.
- `src/Dapper.FluentMap/MappingRegistry.cs`: validation and invalidation happen only through registry methods, not through arbitrary dictionary mutation.
- `test/Dapper.FluentMap.Tests/MappingStateEncapsulationTests.cs`: characterizes direct map replacement leaving a cached mapping stale, proving the legacy bypass still exists.

### Cenario de impacto

A consumer directly assigns `FluentMapper.EntityMaps[typeof(Customer)] = new CustomerMap()` after a miss for a column was cached. The registry may not invalidate the existing cache entry or reinstall the Dapper type map for that type.

### Impacto

Mappings can be silently ignored or stale. Diagnostics may disagree with materialization, and failures can be hard to attribute to direct dictionary mutation.

### Probabilidade

Media. Direct dictionary access is public and historically available, but most documented examples use `Initialize`.

### Workaround atual

Use `FluentMapper.Initialize`, `AddMap`, `AddMap<TMap>`, `AddProfile<TMap>` and convention APIs only. Use `FluentMapper.GetEntityMaps()` and `FluentMapper.GetTypeConventions()` for read-only inspection. Do not mutate `EntityMaps` or `TypeConventions` directly.

### Recomendacao

Keep direct mutation documented as legacy compatibility surface. Use the new read-only snapshots as the preferred inspection API and plan a future major version that replaces public mutable fields with read-only properties or immutable effective mapping snapshots. Consider internal detection of dictionary replacement/mutation only if it can be done without breaking consumers.

### Relacoes

Related to FM-RISK-001 and FM-RISK-013. This is the main blocker for re-enabling test parallelism safely.

## FM-RISK-003 - Assembly scanning can fail under trimming/AOT and produced a failing trimmed smoke

**Severidade:** HIGH
**Status:** MITIGATED
**Categoria:** Reflection, Compatibility, Architecture, Developer Experience
**Origem:** Etapa 3 / Entrega 01; Etapa 4 / Entrega 02
**Detectado em:** planning, implementation and trimming smoke validation
**Componentes afetados:** `AddMapsFromAssembly`, `AddMapsFromAssemblyContaining`, `ForEntitiesInAssembly`, `ForEntitiesInCurrentAssembly`, `ApplyMapsFromAssemblies`

### Descricao

Assembly scanning remains supported for normal runtime usage, but it is reflection-dependent and trimming-sensitive. The trimmed scanning smoke published successfully with expected warnings and then failed at runtime because mapping metadata was removed.

### Evidencias

- `docs/sdd/etapa-3/01-mapping-registration.md`: documents scanning via reflection and `Activator.CreateInstance` as remaining AOT/trimming debt.
- `docs/sdd/etapa-4/02-trimming-aot.md`: classifies scanning APIs as reflection-dependent, marks them with `RequiresUnreferencedCode`, and records a trimmed scanning runtime failure.
- `docs/sdd/etapa-4/03-source-generator.md`: positions generated registration as the alternative to scanning, but not as a full materializer.
- `README.md`: tells trimmed/AOT consumers to prefer explicit registration and documents scanning as trimming-sensitive.
- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`: scanning uses `Assembly.GetExportedTypes()` and `Activator.CreateInstance(mapType)`.
- `src/Dapper.FluentMap/Configuration/FluentConventionConfiguration.cs`: convention scanning uses `GetExportedTypes()`.
- `src/Dapper.FluentMap/Utils/FluentMapConfigurationExtensions.cs`: legacy scanning uses `GetTypes()`, `MakeGenericMethod` and `Activator.CreateInstance`.

### Cenario de impacto

A Native AOT or trimmed application calls `AddMapsFromAssemblyContaining<CustomerMap>()`. The trimmer removes a map or interface metadata that scanning needs. The app starts with incomplete mappings, and a later query falls back to Dapper defaults or fails.

### Impacto

Potential missing mappings, incorrect column/property association, or startup/runtime failures in trimmed applications.

### Probabilidade

Media. The problem is proven in the repository smoke, but only affects consumers using scanning under trimming/AOT or ignoring analyzer/publish warnings.

### Workaround atual

Use `AddMap<TMap>()` or the source generator `AddGeneratedMappings()` for trimmed/AOT applications. Avoid assembly scanning in publish modes that remove metadata.

### Recomendacao

Keep scanning as documented convenience only. Make README examples more explicit about scanning not being an AOT-friendly path, and consider analyzer guidance that flags scanning in projects with trimming/AOT properties when static evidence is reliable.

### Relacoes

Related to FM-RISK-004, FM-RISK-010 and Etapa 4 decisions about runtime remaining authoritative.

## 6. Medium Risks

## FM-RISK-004 - `QueryMapped*` remains runtime/reflection/dynamic-code based; no generated materializer exists

**Severidade:** MEDIUM
**Status:** MITIGATED
**Categoria:** AOT, Trimming, Reflection, Performance, Materialization
**Origem:** Etapa 5 / Entregas 02, 03 and 04
**Detectado em:** architecture decision, implementation and validation
**Componentes afetados:** `QueryMappedExtensions`, `NestedMaterializationPlan`, `Dapper.FluentMap.Generators`

### Descricao

Nested materialization, Value Object construction and profiles are implemented through `QueryMapped*`, which reads a data reader and builds cached runtime plans using reflection and expression compilation. This path is annotated with `RequiresUnreferencedCode` and `RequiresDynamicCode`; the generator still only generates registration, not a `DbDataReader` materializer.

### Evidencias

- `docs/sdd/etapa-5/02-nested-object-materialization.md`: documents runtime reflection, expression compilation, plan cache and AOT/trimming annotations.
- `docs/sdd/etapa-5/03-value-objects.md`: states `QueryMapped*` remains annotated and that generated materializer remains future work.
- `docs/sdd/etapa-5/04-mapping-profiles.md`: says generated query/materializer is deferred.
- `docs/sdd/etapa-5/README.md`: P1 item to create a generated `DbDataReader` materializer.
- `src/Dapper.FluentMap/QueryMappedExtensions.cs`: all public `QueryMapped*` methods are annotated with `RequiresUnreferencedCode` and `RequiresDynamicCode`.
- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`: compiles delegates for constructors, getters, setters and converters.

### Cenario de impacto

A consumer wants nested immutable Value Objects in a Native AOT application. The only implemented path is `QueryMapped*`, which requires runtime code generation and reflection metadata that AOT/trimming may reject or remove.

### Impacto

Limits production use in Native AOT/trimming-heavy applications and leaves performance below the potential of generated row materializers.

### Probabilidade

Media. This affects a narrower but increasingly important deployment style. The APIs are annotated, reducing surprise.

### Workaround atual

Use explicit/generated registration for startup mapping and avoid `QueryMapped*` in Native AOT until a generated materializer exists. Use Dapper's normal `Query<T>` for simple root mappings.

### Recomendacao

Prioritize a generated materializer for `DbDataReader` that covers nested paths, Value Objects and profiles without expression compilation in the hot path. Keep runtime `QueryMapped*` as fallback for dynamic configurations.

### Relacoes

Related to FM-RISK-003, FM-RISK-005, FM-RISK-006 and FM-RISK-007.

## FM-RISK-005 - `QueryMapped*` buffers all rows and has no streaming/unbuffered mode

**Severidade:** MEDIUM
**Status:** OPEN
**Categoria:** Performance, Memory, Materialization, API Design
**Origem:** Etapa 5 / Entrega 04
**Detectado em:** implementation and roadmap
**Componentes afetados:** `QueryMappedExtensions.Materialize`

### Descricao

`QueryMapped*` reads the entire data reader into a `List<TEntity>` before returning. There is no unbuffered streaming equivalent, which can increase memory pressure for large result sets.

### Evidencias

- `docs/sdd/etapa-5/README.md`: lists no streaming/unbuffered support as a main limitation and a P1/P2 follow-up.
- `docs/sdd/etapa-5/04-mapping-profiles.md`: states `QueryMapped*` returns a materialized list and streaming was not implemented.
- `src/Dapper.FluentMap/QueryMappedExtensions.cs`: `Materialize` creates `var results = new List<TEntity>();` and returns it after the reader loop.

### Cenario de impacto

A reporting query returns hundreds of thousands of rows with nested Value Objects. `QueryMapped<T>()` stores all rows before the caller can start processing, causing high memory usage.

### Impacto

Memory growth, slower first-row availability, and inability to mirror Dapper's unbuffered query behavior for supported nested mappings.

### Probabilidade

Media. Large result sets are common, but nested/value-object mapping is opt-in and many use cases will be small projections.

### Workaround atual

Use Dapper `Query<T>` for simple mappings, page large result sets manually, or write a custom `DbDataReader` loop for heavy streaming scenarios.

### Recomendacao

Design streaming overloads with explicit connection/reader lifetime semantics. Do not expose lazy enumeration over a disposed reader; the API must define ownership clearly.

### Relacoes

Related to FM-RISK-004 and the Etapa 5 P1 item for streaming/unbuffered support.

## FM-RISK-006 - Value Object support excludes factories, private constructors/setters, fields and NRT semantics

**Severidade:** MEDIUM
**Status:** OPEN
**Categoria:** Value Objects, Materialization, API Design, Extensibility
**Origem:** Etapa 5 / Entrega 03
**Detectado em:** architecture decision and implementation
**Componentes afetados:** `NestedMaterializationPlan`, `MappingConfigurationValidator`, `QueryMappedExtensions`

### Descricao

The current Value Object contract supports public constructors whose parameters can be bound from mapped properties or nested objects. Factory methods, private constructors, private setters, field/backing-field injection, `FormatterServices`, and nullable reference type metadata are intentionally outside the contract.

### Evidencias

- `docs/sdd/etapa-5/03-value-objects.md`: explicitly lists factory methods, private constructor/setter, field injection and NRT metadata as out of scope.
- `docs/sdd/etapa-5/decisions.md`: states factory methods require an explicit future API and no private constructor/private setter/field injection support exists.
- `README.md`: says factory methods and generated materializers are not part of the runtime path.
- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`: uses public constructors and public setter delegates.
- `test/Dapper.FluentMap.Tests/ValueObjectMaterializationTests.cs`: covers public constructor support and rejection of incomplete/ambiguous constructor scenarios.

### Cenario de impacto

A domain model exposes `Cpf.Create(string)` and keeps constructors private to enforce invariants. `QueryMapped*` cannot construct it from `Cpf.Number`, even though the model is common in DDD-style codebases.

### Impacto

Consumers must change model visibility, map the whole Value Object through a Dapper `TypeHandler`, or avoid FluentMap-controlled materialization for that shape.

### Probabilidade

Media. Public-constructor Value Objects are supported, but private factories are common enough in domain models.

### Workaround atual

Use a public constructor, use a Dapper `TypeHandler<TValueObject>` when the whole Value Object maps to one column, or materialize manually.

### Recomendacao

Design a strongly typed factory API with deterministic ambiguity rules and validation. Do not infer factories by name or reflection convention.

### Relacoes

Related to FM-RISK-004, FM-RISK-007 and Etapa 5 P2 factory-method follow-up.

## FM-RISK-007 - Dapper TypeHandler integration in the runtime materializer depends on reflective access to `SqlMapper.TypeHandlerCache<T>`

**Severidade:** MEDIUM
**Status:** MITIGATED
**Categoria:** Compatibility, Reflection, Value Objects, Maintainability
**Origem:** Etapa 5 / Entrega 03
**Detectado em:** implementation review
**Componentes afetados:** `DapperTypeHandlerAdapter`, `NestedMaterializationPlan`

### Descricao

The runtime materializer detects a Dapper type handler with `SqlMapper.HasTypeHandler`. Dapper `2.1.79` does not expose a public API to convert one arbitrary `object` through the registered handler, so FluentMap still calls Dapper's nested `TypeHandlerCache<T>.Parse` using reflection. The reflection is now isolated behind an internal compatibility adapter instead of living in the materialization plan.

### Evidencias

- `docs/sdd/etapa-5/01-nested-materialization-spike.md`: records that conversions should respect TypeHandlers without copying Dapper internals.
- `docs/sdd/etapa-5/03-value-objects.md`: states TypeHandler support is preserved for scalar Value Object properties.
- `src/Dapper.FluentMap/Compatibility/DapperTypeHandlerAdapter.cs`: centralizes `TypeHandlerCache<T>.Parse(object)` reflection and fails with `FluentMapConfigurationException` if the expected Dapper shape is missing.
- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`: delegates TypeHandler detection/invocation to `DapperTypeHandlerAdapter`.
- `test/Dapper.FluentMap.Tests/DapperCompatibilityAdapterTests.cs`: verifies registered handler conversion, nullable handler null semantics, no-handler fallback and diagnostic failure when the compatibility boundary cannot resolve the Dapper cache shape.
- `test/Dapper.FluentMap.Tests/ValueObjectMaterializationTests.cs`: continues to verify `QueryMappedShouldUseDapperTypeHandlerForScalarValueObjectProperty`.
- `src/Dapper.FluentMap/Dapper.FluentMap.csproj`: Dapper is pinned to `2.1.79`, reducing immediate drift.

### Cenario de impacto

A future Dapper update removes, renames or changes `TypeHandlerCache<T>.Parse`. FluentMap still sees `HasTypeHandler == true`, but the adapter cannot call the handler through this reflective path and throws a diagnostic compatibility exception during plan creation.

### Impacto

Potential regression in scalar Value Object handling under `QueryMapped*` after Dapper upgrades.

### Probabilidade

Baixa. The current dependency is pinned and covered by tests, but the risk rises during dependency updates.

### Workaround atual

Keep Dapper upgrade tasks isolated and run `DapperCompatibilityAdapterTests` plus `ValueObjectMaterializationTests`. Consumers can use Dapper `Query<T>` for scalar TypeHandler paths outside `QueryMapped*`.

### Recomendacao

Investigate a public Dapper-supported handler invocation path during future Dapper upgrades. If none exists, keep the adapter small, fail diagnosticably and do not spread reflection into materialization code.

### Relacoes

Related to FM-RISK-004 and any future Dapper dependency update.

## FM-RISK-008 - Mapping profiles do not support per-profile conventions/naming policies

**Severidade:** MEDIUM
**Status:** OPEN
**Categoria:** Profiles, Extensibility, API Design, Mapping
**Origem:** Etapa 5 / Entrega 04
**Detectado em:** architecture decision and roadmap
**Componentes afetados:** `MappingRegistry.ProfileMaps`, `TypeConventions`, profile query path

### Descricao

Profiles are query-scoped and can define explicit maps, but conventions and naming policies are still registered by entity. The profile path applies entity-level conventions read-only; it cannot define conventions scoped only to one profile.

### Evidencias

- `docs/sdd/etapa-5/04-mapping-profiles.md`: explicitly defers per-profile conventions/naming policies.
- `docs/sdd/etapa-5/decisions.md`: says conventions/naming policies continue by entity and per-profile conventions are future debt.
- `docs/sdd/etapa-5/README.md`: P1 item to define per-profile conventions/naming policies before expanding policy composition.
- `src/Dapper.FluentMap/MappingRegistry.cs`: stores default/profile maps separately, but conventions remain `ConcurrentDictionary<Type, IList<Convention>> TypeConventions`.
- `test/Dapper.FluentMap.Tests/MappingProfileTests.cs`: validates entity naming policy applied to a profile, not per-profile policy registration.

### Cenario de impacto

The same `Customer` entity has one legacy profile using `legacy_customer_id` and a reporting profile using `report_customer_id`. The consumer wants a prefix policy per profile but must map each property explicitly.

### Impacto

More boilerplate and higher maintenance cost for profiles with broad naming differences.

### Probabilidade

Media. Profiles exist exactly to support different SQL shapes, and naming conventions often vary between systems.

### Workaround atual

Use explicit mappings inside each profile map.

### Recomendacao

Design profile-scoped convention storage and precedence rules before adding APIs. Ensure defaults do not leak into profiles silently except where explicitly documented.

### Relacoes

Related to FM-RISK-009 and the profile decisions in Etapa 5.

## FM-RISK-009 - Mapping profiles do not apply to `Dapper.Query<T>` or Dapper multi-mapping

**Severidade:** MEDIUM
**Status:** OPEN
**Categoria:** Profiles, Materialization, API Design, Compatibility
**Origem:** Etapa 5 / Entrega 04
**Detectado em:** architecture decision
**Componentes afetados:** `QueryMappedExtensions`, `MappingRegistry`, Dapper integration

### Descricao

Profiles are intentionally available only through `QueryMapped<TEntity,TProfile>()` and related opt-in APIs. `Dapper.Query<T>()` continues to use the default mapping, and Dapper multi-mapping has no profile overload.

### Evidencias

- `docs/sdd/etapa-5/04-mapping-profiles.md`: rejects mutation-scope profiles through `SqlMapper.SetTypeMap` and states profiles do not apply to `Dapper.Query<T>` or multi-mapping.
- `docs/sdd/etapa-5/decisions.md`: says multiple profiles per type are supported only in `QueryMapped*`.
- `README.md`: documents that `Dapper.Query<T>` and `QueryMapped<T>` use default mapping; profile selection is tied to `QueryMapped<TEntity,TProfile>()`.
- `test/Dapper.FluentMap.Tests/MappingProfileTests.cs`: verifies `DapperQueryShouldContinueUsingDefaultMapping`.

### Cenario de impacto

A consumer uses Dapper multi-mapping to compose aggregates and wants the second object to use a profile. FluentMap has no query-scoped hook for Dapper's multi-mapping API.

### Impacto

Profiles cannot cover some common Dapper query patterns. Consumers must choose between custom callbacks/manual mapping and `QueryMapped*` limitations.

### Probabilidade

Media. Dapper multi-mapping is a common advanced feature, but profile support is explicitly opt-in and new.

### Workaround atual

Use `QueryMapped<TEntity,TProfile>()` for single-entity materialization or manual Dapper multi-mapping callbacks for multi-entity composition.

### Recomendacao

Track demand before expanding the API. If implemented, avoid temporary `SqlMapper.SetTypeMap` swaps; use an operation-scoped materializer or wait for a public Dapper hook.

### Relacoes

Related to FM-RISK-001, FM-RISK-008 and Etapa 5 rejected alternative "Mutation scope".

## FM-RISK-010 - Legacy `ApplyMapsFromAssemblies` keeps older reflection/discovery behavior

**Severidade:** MEDIUM
**Status:** MITIGATED
**Categoria:** Reflection, Compatibility, Maintainability, Developer Experience
**Origem:** Etapa 2 / Entrega 02; Etapa 3 / Entrega 01
**Detectado em:** discovery/reflection review
**Componentes afetados:** `FluentMapConfigurationExtensions.ApplyMapsFromAssemblies`

### Descricao

The modern scanning APIs are deterministic and integrated into the registry, but the legacy `ApplyMapsFromAssemblies` remains for compatibility. It still uses `Assembly.GetTypes()`, reflection invocation and `Activator.CreateInstance`, and earlier SDD notes left its discovery diagnostics outside functional redesign.

### Evidencias

- `docs/sdd/etapa-2/02-configuration-validation.md`: keeps `FluentMapConfigurationExtensions.ApplyMapsFromAssemblies` discovery/reflection diagnostics outside scope.
- `docs/sdd/etapa-3/01-mapping-registration.md`: preserves `ApplyMapsFromAssemblies(...)` for compatibility and adds modern alternatives.
- `docs/sdd/etapa-4/02-trimming-aot.md`: marks the legacy API as trimming-sensitive.
- `src/Dapper.FluentMap/Utils/FluentMapConfigurationExtensions.cs`: uses `GetTypes()`, `MakeGenericMethod`, `Invoke`, `Activator.CreateInstance`, and throws `InvalidOperationException` for duplicate mappings.

### Cenario de impacto

A legacy consumer scans assemblies with maps that include base maps or contain problematic types. The legacy path can produce reflection-shaped errors and lacks the same documented deterministic preflight behavior as the modern API.

### Impacto

Diagnostics and ordering may be less predictable than modern registration APIs, and trimming/AOT behavior is fragile.

### Probabilidade

Baixa. Modern APIs and generator are documented, but legacy consumers can still call this public extension.

### Workaround atual

Use `AddMap<TMap>()`, `AddMapsFromAssembly(...)`, `AddMapsFromAssemblyContaining<TMarker>()`, or `AddGeneratedMappings()`.

### Recomendacao

Document `ApplyMapsFromAssemblies` as legacy. In a future major version, consider deprecation or routing it through the same modern scanning implementation if behavior can be preserved.

### Relacoes

Related to FM-RISK-003 and Etapa 3 registration decisions.

## FM-RISK-011 - Constructor overload ambiguity and optional parameters remain delegated to Dapper

**Severidade:** MEDIUM
**Status:** OPEN
**Categoria:** Materialization, Correctness, Compatibility
**Origem:** Etapa 3 / Entrega 02
**Detectado em:** architecture decision and tests
**Componentes afetados:** `FluentConstructorTypeMap`, `MultiTypeMap`, Dapper `DefaultTypeMap`

### Descricao

FluentMap translates configured column metadata to Dapper for simple constructor mapping, but it does not own root constructor selection ambiguity, optional parameter behavior or Dapper's underscore matching flag. These remain governed by Dapper.

### Evidencias

- `docs/sdd/etapa-3/02-constructor-immutable-mapping.md`: states constructor overload ambiguity remains Dapper responsibility and optional parameters receive no special handling.
- `docs/sdd/etapa-3/decisions.md`: says constructor selection remains delegated to `DefaultTypeMap`.
- `src/Dapper.FluentMap/TypeMaps/FluentConstructorTypeMap.cs`: delegates constructor matching to Dapper `DefaultTypeMap` after translating names/types.
- `test/Dapper.FluentMap.Tests/ConstructorMappingTests.cs`: covers supported constructor scenarios, including that nested `MemberPath` is not root constructor mapping.

### Cenario de impacto

A model has multiple public constructors that Dapper can interpret similarly after FluentMap translates column names. Dapper chooses according to its own rules, or fails, and FluentMap does not add a separate diagnostic layer for that root constructor ambiguity.

### Impacto

Behavior can surprise consumers who expect FluentMap's diagnostics to cover all immutable-constructor edge cases.

### Probabilidade

Baixa. Common single-constructor and record cases are tested, and ambiguous public constructors are less common.

### Workaround atual

Keep materialized entities constructor shapes simple, avoid ambiguous overloads, and use `QueryMapped*` for nested immutable graphs where FluentMap has its own constructor plan.

### Recomendacao

Do not reimplement Dapper constructor selection casually. If demand appears, add narrow diagnostics that explain Dapper-delegated ambiguity without changing behavior.

### Relacoes

Related to FM-RISK-004 and FM-RISK-006.

## FM-RISK-012 - Throwing `IgnoredPropertyInfo` sentinel was removed from ignored/nested mapping paths

**Severidade:** MEDIUM
**Status:** RESOLVED
**Categoria:** Correctness, Maintainability, Technical Debt
**Origem:** Etapa 2 / Entrega 02; Etapa 6 / Entrega 03
**Detectado em:** implementation review
**Componentes afetados:** `DapperIgnoredMemberMap`, `DapperFluentPropertyTypeMap`, `MultiTypeMap`

### Descricao

Ignored and nested mappings previously used an internal `IgnoredPropertyInfo` sentinel to prevent Dapper fallback. The sentinel overrode many `PropertyInfo` members by throwing `NotImplementedException`. Etapa 6 / Entrega 03 removed that sentinel and replaced it with an explicit internal `IMemberMap` marker that has safe null members.

### Evidencias

- `docs/sdd/etapa-2/02-configuration-validation.md`: catalogs `IgnoredPropertyInfo` throwing `NotImplementedException` as partially detectable and outside the delivery scope.
- `src/Dapper.FluentMap/TypeMaps/IgnoredPropertyInfo.cs`: removed.
- `src/Dapper.FluentMap/Compatibility/DapperFluentPropertyTypeMap.cs`: returns `DapperIgnoredMemberMap` for ignored mappings and FluentMap-controlled nested paths.
- `src/Dapper.FluentMap/Compatibility/DapperIgnoredMemberMap.cs`: implements `SqlMapper.IMemberMap` without throwing `PropertyInfo` members.
- `src/Dapper.FluentMap/TypeMaps/MultiTypeMap.cs`: recognizes `DapperIgnoredMemberMap` and returns `null` without falling back to `DefaultTypeMap`.
- `test/Dapper.FluentMap.Tests/DapperCompatibilityAdapterTests.cs`: verifies ignored root mapping, ignored nested path, Dapper fallback for unrelated members and direct type-map access without `NotImplementedException`.

### Cenario de impacto

Previously, a future Dapper version could inspect more of the returned `PropertyInfo` before FluentMap intercepted it. That path no longer exists because ignored/nested markers no longer expose a throwing `PropertyInfo`.

### Impacto

Resolved for the known sentinel path. Ignored/nested mappings now use an explicit internal `IMemberMap` marker.

### Probabilidade

Baixa for future Dapper fallback behavior, but the specific `NotImplementedException` sentinel risk is resolved.

### Workaround atual

None needed for this issue.

### Recomendacao

Keep `DapperCompatibilityAdapterTests` in the Dapper upgrade checklist to verify ignored mappings still block fallback.

### Relacoes

Related to FM-RISK-007 and future Dapper compatibility work, but no longer tracked as active technical debt.

## FM-RISK-013 - Dommel behavior for profiles/nested materialization is intentionally unreviewed

**Severidade:** MEDIUM
**Status:** UNKNOWN
**Categoria:** Extensibility, Profiles, Mapping, Documentation
**Origem:** Etapa 5 / Entrega 04
**Detectado em:** roadmap/research item
**Componentes afetados:** `Dapper.FluentMap.Dommel`, default/profile mapping registry

### Descricao

Etapa 5 intentionally did not change Dommel. Profiles and nested materialization are core/query-wrapper features, while Dommel still consumes the historical mapping surfaces. The SDD explicitly asks for a future Dommel review to decide whether profiles should be visible to external CRUD integrations.

### Evidencias

- `docs/sdd/etapa-5/README.md`: says Dommel received no functional changes and lists "Revisar Dommel em etapa propria" under research.
- `docs/sdd/etapa-5/decisions.md`: profiles affect `QueryMapped*`; Dapper global type map represents only default mapping.
- `src/Dapper.FluentMap.Dommel/Resolvers/*`: Dommel resolver implementation remains separate.
- `test/Dapper.FluentMap.Dommel.Tests/ManualMappingTests.cs`: Dommel tests cover legacy resolver behavior only.

### Cenario de impacto

A consumer expects a Dommel CRUD operation to use a profile map registered by `AddProfile<TMap>()`. Current evidence indicates profiles are query-scoped to `QueryMapped*`, but Dommel-specific behavior has not been formally reviewed for this new model.

### Impacto

Potential documentation/support confusion and extension limitations for Dommel consumers.

### Probabilidade

Baixa. Dommel profile integration is not documented as supported, but profile adoption can create expectations.

### Workaround atual

Use default maps for Dommel and reserve profiles for `QueryMapped<TEntity,TProfile>()`.

### Recomendacao

Run a dedicated Dommel design/review stage. Decide whether profiles should remain invisible to Dommel or receive explicit APIs, and document the outcome.

### Relacoes

Related to FM-RISK-001, FM-RISK-002, FM-RISK-008 and FM-RISK-009.

## 7. Low Risks

## FM-RISK-014 - Analyzer and generator coverage is intentionally partial

**Severidade:** LOW
**Status:** MITIGATED
**Categoria:** Developer Experience, Testing, Maintainability
**Origem:** Etapa 4 / Entregas 01 and 03; Etapa 5 / Entrega 04
**Detectado em:** analyzer/generator design decisions
**Componentes afetados:** `Dapper.FluentMap.Analyzers`, `Dapper.FluentMap.Generators`

### Descricao

The analyzer and generator detect only statically provable cases. They do not execute map constructors, follow helper methods, simulate scanning, reason about dynamic columns, or prove query-specific materialization validity.

### Evidencias

- `docs/sdd/etapa-4/README.md`: runtime remains authority; do not report what cannot be proven statically.
- `docs/sdd/etapa-4/01-roslyn-analyzers.md`: lists many analyzer limitations.
- `docs/sdd/etapa-4/03-source-generator.md`: generator discovers only maps declared in the current compilation.
- `src/Dapper.FluentMap.Analyzers/README.md`: analyzer complements runtime validation and does not replace it.
- `src/Dapper.FluentMap.Generators/README.md`: generator emits registration only for eligible maps in current compilation.

### Cenario de impacto

A consumer moves mapping calls into helper methods or uses dynamically computed column names. The analyzer stays silent, and invalid configuration is caught only by runtime validation or query execution.

### Impacto

Lower compile-time feedback coverage than consumers might assume.

### Probabilidade

Alta. Helper methods and dynamic configuration are common, but the README and SDD make runtime authority clear.

### Workaround atual

Call `FluentMapper.Validate()` during startup/tests and keep runtime fail-fast validations enabled.

### Recomendacao

Improve analyzer coverage only for patterns that can be proven without false positives. Add documentation examples that pair analyzer use with startup validation.

### Relacoes

Related to FM-RISK-003 and FM-RISK-010.

## FM-RISK-015 - Async `QueryMapped*` overloads are asymmetric: profile async exists, default async does not

**Severidade:** LOW
**Status:** OPEN
**Categoria:** API Design, Developer Experience
**Origem:** Etapa 5 / Entrega 04
**Detectado em:** implementation and roadmap
**Componentes afetados:** `QueryMappedExtensions`

### Descricao

The current public API includes async overloads for profile queries but not equivalent default `QueryMappedAsync<TEntity>()` and `QueryMappedSingleAsync<TEntity>()` overloads. The SDD lists symmetric async/default overload expansion as a future demand-driven item.

### Evidencias

- `docs/sdd/etapa-5/README.md`: P2 item to expand async/default overloads symmetrically if public demand appears.
- `src/Dapper.FluentMap/QueryMappedExtensions.cs`: async methods are present for `<TEntity,TProfile>` only.
- `test/Dapper.FluentMap.Tests/MappingProfileTests.cs`: validates async concurrent profile queries.

### Cenario de impacto

A consumer using default nested mappings in an async data-access layer cannot call a default `QueryMappedAsync<Customer>()` API.

### Impacto

Ergonomic limitation; not a correctness bug.

### Probabilidade

Media. Async data access is common, but profile async was prioritized for concurrency validation.

### Workaround atual

Use sync `QueryMapped<T>()` for default mappings or introduce an explicit profile when async profile APIs are acceptable.

### Recomendacao

Add symmetric default async overloads in a small API-only delivery with integration tests and cancellation-token coverage through `CommandDefinition`.

### Relacoes

Related to FM-RISK-005 and Etapa 5 API evolution.

## FM-RISK-016 - NuGet package metadata remains legacy

**Severidade:** LOW
**Status:** OPEN
**Categoria:** Documentation, Developer Experience, Packaging, Maintainability
**Origem:** .NET 10 / Entrega 04; Security hardening
**Detectado em:** package validation
**Componentes afetados:** `src/Dapper.FluentMap/*.csproj`, package output

### Descricao

Package metadata still uses `PackageLicenseUrl` and does not include a package README, SourceLink or repository metadata modernization. Pack succeeds, but NuGet emits NU5125 and README recommendations.

### Evidencias

- `docs/sdd/net10-migration/04-validation-pack-ci.md`: defers metadata modernization.
- `docs/sdd/net10-migration/README.md`: lists metadata modernization as out of scope.
- `docs/sdd/security-hardening/sqlitepclraw-vulnerability.md`: pack succeeds with existing NU5125 and README recommendation.
- `src/Dapper.FluentMap/Dapper.FluentMap.csproj`: contains `PackageLicenseUrl`.
- `src/Dapper.FluentMap.Dommel/Dapper.FluentMap.Dommel.csproj`: contains `PackageLicenseUrl`.

### Cenario de impacto

A package consumer or NuGet UI sees older metadata conventions and missing README even though the package builds correctly.

### Impacto

Lower package polish and possible future NuGet warning churn, but no runtime behavior impact.

### Probabilidade

Alta. The warning is repeatedly observed during pack.

### Workaround atual

None needed for runtime. Treat pack warnings as known until a metadata-only cleanup is scheduled.

### Recomendacao

Run a dedicated packaging modernization: replace `PackageLicenseUrl` with `PackageLicenseExpression`, add package README/repository metadata, inspect `.nupkg`, and keep it separate from functional changes.

### Relacoes

Related to `.NET 10` migration packaging decisions.

## FM-RISK-017 - Remote CI execution remains unproven after CI modernization

**Severidade:** LOW
**Status:** UNKNOWN
**Categoria:** Testing, Maintainability, Developer Experience
**Origem:** .NET 10 / Entregas 04 and 05
**Detectado em:** validation limitation
**Componentes afetados:** `.github/workflows/ci.yml`, `.appveyor.yml`, `.travis.yml`

### Descricao

CI files were updated and locally reviewed, but GitHub Actions, AppVeyor and Travis were not executed remotely from the development environment.

### Evidencias

- `docs/sdd/net10-migration/04-validation-pack-ci.md`: remote GitHub Actions, AppVeyor and Travis runs were not executed; Travis availability/image contents unproven.
- `docs/sdd/net10-migration/05-xunit3-migration.md`: CI files reviewed after xUnit 3 migration, but no remote execution evidence.
- `.github/workflows/ci.yml`, `.appveyor.yml`, `.travis.yml`: current CI definitions.

### Cenario de impacto

A push triggers CI and discovers that a hosted image, action version, Travis environment, or .NET 10 installation path behaves differently from local validation.

### Impacto

CI failure after merge/push, with no evidence of runtime library defect.

### Probabilidade

Media. Local command validation is strong, but remote infrastructure can drift.

### Workaround atual

Run remote CI before release decisions and treat local validation as necessary but not sufficient.

### Recomendacao

After the next push, record actual CI results in the SDD status or release notes. Revisit Travis if the service no longer supports the expected .NET 10 workflow.

### Relacoes

Related to `.NET 10` migration validation.

## FM-RISK-018 - Documentation carries archived/legacy signals alongside new SDD features

**Severidade:** LOW
**Status:** OPEN
**Categoria:** Documentation, Developer Experience, Maintainability
**Origem:** README and accumulated SDD updates
**Detectado em:** documentation review
**Componentes afetados:** `README.md`, CI badges, consumer-facing docs

### Descricao

The README still begins with an archived-project notice and historical CI badges, while later sections document new SDD-era capabilities such as nested materialization, profiles, analyzers, generators and .NET 10 validation. This can confuse readers about maintenance status and supported feature freshness.

### Evidencias

- `README.md`: starts with an "Archived" notice from the original project.
- `README.md`: later contains sections for nested object materialization, mapping profiles, trimming/Native AOT, generated registration and Etapa summaries.
- `.github/workflows/ci.yml`: new CI exists, while README badges still point to older AppVeyor/Travis-era links.
- `docs/sdd/etapa-5/README.md`: documents current supported capabilities and limitations.

### Cenario de impacto

A consumer reads the top of README, assumes the project is abandoned, then sees modern features and cannot tell which status is authoritative.

### Impacto

Documentation trust and adoption risk, not a code correctness risk.

### Probabilidade

Media. README is the primary entry point.

### Workaround atual

Use SDD reports and current tests as source of truth for recent work.

### Recomendacao

Create a documentation-only decision about project status. Either preserve the archived notice as historical context with a current-maintenance note, or move it to an archival/history section.

### Relacoes

Related to FM-RISK-016.

## 8. Cross-Cutting Architectural Concerns

- Global/static state: `FluentMapper`, Dapper type maps, test reset and Dommel resolver integration remain the main cross-cutting constraint.
- Reflection: expression parsing, assembly scanning, diagnostics, registration inference and runtime materialization all depend on metadata to different degrees.
- Caching: property-map and materialization-plan caches are structured and include profile/column shape where relevant, but cache invalidation still assumes registry-mediated mutation.
- Materialization: `Dapper.Query<T>` remains Dapper-owned for simple mappings; nested/value-object/profile behavior is opt-in through `QueryMapped*`.
- AOT/trimming: explicit/generated registration is the safer path; scanning and `QueryMapped*` remain annotated as sensitive.
- Profiles: query-scoped profiles avoid global `SetTypeMap` swaps, but do not yet cover Dapper multi-mapping, `Dapper.Query<T>` or per-profile conventions.
- Dommel: left stable by design, but not reviewed against the new profile/nested materialization model.

## 9. Technical Debt Register

| ID | Debt | Origin | Impact | Suggested Priority |
| -- | ---- | ------ | ------ | ------------------ |
| FM-RISK-001 | Global/static state and disabled test parallelism | Etapa 1 | Blocks stronger concurrency guarantees | P1 |
| FM-RISK-002 | Public mutable mapping dictionaries | Etapa 1 | Can bypass validation/cache invalidation | P1 |
| FM-RISK-004 | No generated `DbDataReader` materializer | Etapa 5 | AOT/performance limitation | P1 |
| FM-RISK-005 | No streaming/unbuffered `QueryMapped*` | Etapa 5 | Memory/performance limitation | P1 |
| FM-RISK-008 | No per-profile conventions/naming policies | Etapa 5 | Boilerplate and profile extensibility limit | P1 |
| FM-RISK-006 | No Value Object factory API | Etapa 5 | Common domain model limitation | P2 |
| FM-RISK-009 | No profile integration for Dapper multi-mapping | Etapa 5 | Advanced Dapper scenarios uncovered | P2 |
| FM-RISK-015 | Missing default async `QueryMapped*` overloads | Etapa 5 | API ergonomics | P2 |
| FM-RISK-016 | Legacy NuGet metadata | .NET 10 migration | Package polish/warnings | P2 |
| FM-RISK-018 | README maintenance-status inconsistency | README/SDD | Consumer confusion | P2 |
| FM-RISK-017 | Remote CI evidence missing | .NET 10 migration | Release confidence | P2 |
| FM-RISK-010 | Legacy assembly scanning API behavior | Etapa 3 | Maintenance/diagnostic debt | P3 |
| FM-RISK-014 | Partial analyzer/generator coverage | Etapa 4 | Compile-time feedback gaps | P3 |
| FM-RISK-011 | Dapper-delegated constructor edge cases | Etapa 3 | Edge-case diagnostics | P3 |
| FM-RISK-013 | Dommel profile/nested review missing | Etapa 5 | Extension clarity | P3 |
| FM-RISK-007 | Reflective TypeHandler adapter | Etapa 5 | Dapper upgrade fragility | P3 |
| FM-RISK-003 | Scanning unsafe under trimming/AOT | Etapa 4 | Compatibility risk if warnings ignored | P3, unless AOT-focused release |

## 10. Historical Issues Already Resolved

| Problem | Origin | Resolved In | Evidence |
| ------- | ------ | ----------- | -------- |
| ReflectionHelper could resolve a homonymous method/member instead of the expression property | Etapa 1 / Entrega 01 | Etapa 1 / Entrega 01 | `docs/sdd/etapa-1/01-reflection-helper.md`; `src/Dapper.FluentMap/Utils/ReflectionHelper.cs`; `test/Dapper.FluentMap.Tests/ReflectionHelperTests.cs` |
| Explicit mapping and convention order caused "last SetTypeMap wins" behavior | Etapa 1 / Entrega 02 | Etapa 1 / Entrega 02 and 04 | `docs/sdd/etapa-1/02-mapping-composition.md`; `src/Dapper.FluentMap/MappingRegistry.cs`; `test/Dapper.FluentMap.Tests/MappingCompositionTests.cs` |
| Old string-concatenated mapping cache could retain stale/mis-keyed hits/misses | Etapa 1 / Entrega 03 | Etapa 1 / Entrega 04 | `docs/sdd/etapa-1/04-mapping-registry-cache.md`; `src/Dapper.FluentMap/MappingCacheKey.cs`; `test/Dapper.FluentMap.Tests/MappingRegistryTests.cs` |
| No atomic internal reset for tests | Etapa 1 / Entrega 03 | Etapa 1 / Entrega 04 | `docs/sdd/etapa-1/04-mapping-registry-cache.md`; `src/Dapper.FluentMap/MappingRegistry.cs` |
| Nested paths with same terminal name, such as `Rank.Level` and `Seniority.Level`, were treated as duplicate | Etapa 2 / Entrega 01 | Etapa 2 / Entrega 01 | `docs/sdd/etapa-2/01-member-path.md`; `src/Dapper.FluentMap/Mapping/MemberPath.cs`; `test/Dapper.FluentMap.Tests/MemberPathTests.cs` |
| Configuration errors used generic/late exceptions for many invalid mappings | Etapa 2 / Entrega 02 | Etapa 2 / Entrega 02 and Etapa 3 / Entrega 03 | `docs/sdd/etapa-2/02-configuration-validation.md`; `src/Dapper.FluentMap/FluentMapConfigurationException.cs`; `src/Dapper.FluentMap/MappingConfigurationValidator.cs`; `FluentMapper.Validate()` |
| No inherited mapping composition | Etapa 2 / Entrega 03 | Etapa 2 / Entrega 03 | `docs/sdd/etapa-2/03-inherited-mappings.md`; `test/Dapper.FluentMap.Tests/InheritedMappingTests.cs` |
| No declarative naming policy API | Etapa 2 / Entrega 04 | Etapa 2 / Entrega 04 | `docs/sdd/etapa-2/04-naming-policies.md`; `src/Dapper.FluentMap/Naming/NamingPolicy.cs`; `test/Dapper.FluentMap.Tests/NamingPolicyTests.cs` |
| No explicit generic registration or deterministic modern assembly scanning | Etapa 3 / Entrega 01 | Etapa 3 / Entrega 01 | `docs/sdd/etapa-3/01-mapping-registration.md`; `test/Dapper.FluentMap.Tests/MappingRegistrationTests.cs` |
| Mapped columns did not influence Dapper constructor mapping for immutable simple models | Etapa 3 / Entrega 02 | Etapa 3 / Entrega 02 | `docs/sdd/etapa-3/02-constructor-immutable-mapping.md`; `src/Dapper.FluentMap/TypeMaps/FluentConstructorTypeMap.cs`; `test/Dapper.FluentMap.Tests/ConstructorMappingTests.cs` |
| No public aggregate validation/explain diagnostics | Etapa 2 and Etapa 3 | Etapa 3 / Entrega 03 | `docs/sdd/etapa-3/03-diagnostics-api.md`; `src/Dapper.FluentMap/Diagnostics/*`; `test/Dapper.FluentMap.Tests/DiagnosticsApiTests.cs` |
| Runtime registration created type maps via `MakeGenericType` and `Activator.CreateInstance` | Etapa 3 / Entrega 01 | Etapa 4 / Entrega 02 | `docs/sdd/etapa-4/02-trimming-aot.md`; `src/Dapper.FluentMap/MappingRegistry.cs` |
| No source generator for mapping registration | Etapa 4 planning | Etapa 4 / Entrega 03 | `docs/sdd/etapa-4/03-source-generator.md`; `src/Dapper.FluentMap.Generators/MappingRegistrationGenerator.cs` |
| Nested paths returned to Dapper could write leaf values into the root object slot | Etapa 5 / Entrega 01 | Etapa 5 / Entrega 02 | `docs/sdd/etapa-5/01-nested-materialization-spike.md`; `docs/sdd/etapa-5/02-nested-object-materialization.md`; `test/Dapper.FluentMap.Tests/NestedMaterializationSpikeTests.cs` |
| Mutable nested object materialization was unsupported | Etapa 5 / Entrega 01 | Etapa 5 / Entrega 02 | `docs/sdd/etapa-5/02-nested-object-materialization.md`; `test/Dapper.FluentMap.Tests/NestedObjectMaterializationTests.cs` |
| Immutable nested Value Objects were unsupported | Etapa 5 / Entrega 01-02 | Etapa 5 / Entrega 03 | `docs/sdd/etapa-5/03-value-objects.md`; `test/Dapper.FluentMap.Tests/ValueObjectMaterializationTests.cs` |
| Same entity could not have multiple query-scoped mapping profiles | Etapa 3 limitations | Etapa 5 / Entrega 04 | `docs/sdd/etapa-5/04-mapping-profiles.md`; `test/Dapper.FluentMap.Tests/MappingProfileTests.cs` |
| Tests targeted obsolete `netcoreapp3.1` and could not run on the local machine | .NET 10 / Entrega 01 | .NET 10 / Entrega 02 | `docs/sdd/net10-migration/01-inventory-baseline.md`; `docs/sdd/net10-migration/02-test-projects-net10.md` |
| xUnit 2 was deprecated/legacy in package diagnostics | .NET 10 / Entrega 01-04 | .NET 10 / Entrega 05 | `docs/sdd/net10-migration/05-xunit3-migration.md`; test `.csproj` files |
| Vulnerable test transitive `SQLitePCLRaw.lib.e_sqlite3 2.1.11` | .NET 10 migration | Security hardening | `docs/sdd/security-hardening/sqlitepclraw-vulnerability.md`; `test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj` |

## 11. Unknown / Requires Investigation

- FM-RISK-013: Dommel interaction with profiles/nested materialization requires a dedicated review. Current evidence only proves legacy/default Dommel behavior.
- FM-RISK-017: CI needs actual remote execution evidence after modernization.
- Native AOT full runtime behavior remains unproven locally because the platform linker was missing during Etapa 4 and Etapa 5 validation.
- Current external action/service versions were not verified during this audit; the report relies on repository evidence, not live CI execution.

## 12. Recommended Remediation Order

1. Document and constrain the configuration lifecycle around global/static state before changing implementation. This reduces ambiguity for FM-RISK-001 and FM-RISK-002.
2. Plan a compatibility-safe path away from public mutable dictionaries, likely with read-only views and a future-major migration note.
3. Implement a generated `DbDataReader` materializer, because it unlocks the biggest cluster: FM-RISK-004, FM-RISK-005 and part of FM-RISK-007.
4. Add streaming/unbuffered `QueryMapped*` only after reader lifetime semantics are designed.
5. Design per-profile conventions/naming policies before expanding profile APIs further.
6. Add default async `QueryMapped*` overloads with `CommandDefinition`/cancellation coverage.
7. Run a dedicated Dommel profile/nested review and document whether integration is intentionally unsupported.
8. Modernize NuGet metadata and README status in a documentation/packaging-only delivery.
9. Record remote CI outcomes after the next push.
10. Revisit lower-level compatibility debt: reflective TypeHandler adapter and legacy scanning API.

## 13. Architectural Health Assessment

### Strengths

- The project has unusually strong SDD traceability for a small library.
- Public behavior is protected by integration tests using real Dapper and SQLite.
- Precedence is explicit and repeatedly validated: explicit, inherited explicit, convention/naming policy, Dapper default.
- `MemberPath` removed a class of reflection/name-collision bugs and made nested/profiles possible.
- Runtime validation remains authoritative even after analyzers and generator were added.
- Profiles avoid unsafe temporary `SqlMapper.SetTypeMap` mutation by using query-scoped materialization.
- Published source projects remain `netstandard2.0`, preserving broad compatibility.

### Concerns

- Global mutable state and public mutable dictionaries remain the central architectural debt.
- AOT/trimming support is split: explicit/generated registration is good, scanning and `QueryMapped*` remain constrained.
- The runtime materializer has real scope but currently lacks generated and streaming variants.
- Dommel was intentionally not evolved with the core profile/nested model.
- README/package metadata still carry legacy signals.

### Evolution Risks

- Adding per-profile conventions, streaming, factory methods or generated materializers will touch shared registry/materialization/cache contracts.
- Removing or hiding public dictionaries would be a compatibility-sensitive major-version decision.
- Future Dapper updates need targeted review around `ITypeMap`, `IMemberMap`, constructor mapping and TypeHandler internals.
- Expanding Dommel support could reintroduce global state concerns if it tries to observe profiles through Dapper's global type-map path.

### Overall Assessment

**Moderate technical risk**

The core design is coherent and much healthier than the historical baseline: the main correctness bugs around expression resolution, mapping composition, cache keys, nested path identity and profile concurrency have been addressed. The remaining risk is moderate because the library still carries global mutable compatibility surfaces and the newest materialization capabilities depend on runtime reflection/dynamic code. There is no evidence of a current critical production-unsafety condition when the documented startup-once and opt-in APIs are used.
