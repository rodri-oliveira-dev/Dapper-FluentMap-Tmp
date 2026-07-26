# Etapa 6 Handoff

## Etapa 6 Final State

Etapa 6 esta `COMPLETED`.

O objetivo foi endurecer contratos arquiteturais antes de qualquer mudanca grande no materializer. A etapa preservou API publica, manteve `Dapper.FluentMap` em `netstandard2.0` e nao alterou Dommel funcionalmente.

## Completed Deliveries

01 Configuration Lifecycle - `COMPLETED`

- Contrato formal: `Configuration Phase -> Operational Phase`.
- Runtime reconfiguration continua possivel por compatibilidade, mas apenas sob quiescencia externa.
- Direct public dictionary mutation permanece superficie legada.

02 Mapping State Encapsulation - `COMPLETED`

- Adicionados snapshots read-only:
  - `FluentMapper.GetEntityMaps()`;
  - `FluentMapper.GetTypeConventions()`.
- Campos publicos mutaveis foram preservados por compatibilidade.

03 Dapper Compatibility Adapters - `COMPLETED`

- Criada fronteira interna `Dapper.FluentMap.Compatibility`.
- `DapperTypeHandlerAdapter` concentra reflection residual de TypeHandlers.
- `IgnoredPropertyInfo` foi removido.
- Ignored/nested mappings usam `DapperIgnoredMemberMap`.

04 Generated Materializer Spike - `COMPLETED`

- Resultado: `GO WITH CONSTRAINTS`.
- Prototipo test-only validou materializer gerado conceitual para entidade simples, nested mutable object, immutable Value Object, profile e `DBNull`.
- Recomendacao: generated materializer com runtime fallback.

## Architecture After Etapa 6

`FluentMapper` permanece uma fachada global:

- static `MappingRegistry`;
- static `FluentMapConfiguration`;
- public mutable `EntityMaps`;
- public mutable `TypeConventions`;
- Dapper global type-map integration por `SqlMapper.SetTypeMap`.

O lifecycle suportado e:

```text
Configuration Phase
        |
        v
Operational Phase
```

Profiles permanecem query-scoped:

```text
QueryMapped<TEntity,TProfile>()
```

Eles nao trocam `SqlMapper.SetTypeMap` temporariamente.

`QueryMapped*` ainda usa `NestedMaterializationPlan` runtime/reflection-based. O futuro caminho recomendado e:

```text
QueryMapped
    |
    v
Generated materializer matches?
    | yes
    v
Generated path
    |
    no
    v
Runtime fallback
```

## Remaining Risks

- `FM-RISK-001`: global FluentMap/Dapper state permanece mitigado, nao resolvido.
- `FM-RISK-002`: public mutable dictionaries ainda podem bypassar registry/cache.
- `FM-RISK-004`: materializer gerado ainda nao existe em runtime de producao.
- `FM-RISK-005`: `QueryMapped*` ainda bufferiza todas as linhas.
- `FM-RISK-006`: factory methods/private constructors/private setters/fields/NRT continuam fora do contrato.
- `FM-RISK-007`: TypeHandler invocation ainda depende de reflection isolada.
- `FM-RISK-008`: conventions/naming policies por profile ainda nao existem.
- `FM-RISK-009`: profiles ainda nao se aplicam a `Dapper.Query<T>` ou multi-mapping.

## Resolved Risks

- `FM-RISK-012`: `IgnoredPropertyInfo` foi removido; ignored/nested mappings usam marker seguro.

## Mitigated Risks

- `FM-RISK-001`: lifecycle documentado e testado.
- `FM-RISK-003`: scanning marcado/documentado como trimming-sensitive; explicit/generated registration permanecem caminhos preferidos.
- `FM-RISK-004`: spike adicionou evidencia tecnica e arquitetura recomendada, mas nao resolveu o runtime.
- `FM-RISK-007`: reflection Dapper-specific isolada em `DapperTypeHandlerAdapter`.
- `FM-RISK-014`: analyzer/generator permanecem complementares a validacao runtime.

## Decisions That Future Work Must Preserve

- E6-D001 - Configuration lifecycle contract.
- E6-D002 - Documentation contract only for Delivery 01; sem freeze/seal API.
- E6-D003 - Profiles remain query-scoped.
- E6-D004 - Mapping state read-only snapshots.
- E6-D005 - Dapper compatibility boundary.
- E6-D006 - Residual TypeHandler reflection isolated and diagnostic.
- E6-D007 - Ignored mapping without throwing `PropertyInfo` sentinel.
- E6-D008 - Dapper upgrade checklist.
- E6-D009 - Generated materializer direction: generated + runtime fallback.
- E6-D010 - Static mapping eligibility.
- E6-D011 - AOT claims require runtime evidence.
- E6-D012 - Generated TypeHandler boundary.

## Recommended Next Stage

Etapa 7 - Generated Materialization

Suggested sequence:

1. Generated materializer contract and runtime lookup.
2. Static mapping DSL discovery in the generator.
3. Generated row materializer for explicit maps, nested mutable objects, immutable constructors and `DBNull`.
4. Generated profile support and diagnostics.
5. TypeHandler/conversion strategy.
6. Trim, Native AOT and performance validation.

## Preconditions

- Preserve source/binary compatibility.
- Keep runtime fallback.
- Keep `Dapper.Query<T>` default behavior unchanged.
- Keep profiles query-scoped and avoid `SqlMapper.SetTypeMap` mutation scopes.
- Do not require generator installation for existing consumers.
- Do not remove RUC/RDC annotations while runtime fallback remains possible.
- Validate with generator tests, integration tests, trimmed smoke and Native AOT runtime when environment supports it.

## Open Questions

- What minimal public or internal contract should connect generated materializers to the core lookup?
- How should generated descriptors prove they still match effective runtime configuration when public dictionaries can be mutated directly?
- Should first generated support include built-in naming policies or explicit maps only?
- What is the safest TypeHandler strategy without spreading Dapper-internal reflection?
- How should maps in referenced assemblies expose generated materializer manifests?
- What diagnostics should explain generated path vs fallback?
- What benchmark shape should become the baseline for startup, first query, throughput, allocation and memory?
- Which environment will validate Native AOT runtime with the required platform linker C++ installed?
