# Configuration Isolation Matrix

## Escopo

Esta matriz registra o estado comprovado no prompt 11.6 para a arquitetura:

```text
FluentMapConfigurationBuilder
    -> ImmutableFluentMapConfiguration
    -> FluentMapRuntime
```

O foco e provar isolamento para APIs novas controladas pelo FluentMap. APIs
legadas que dependem de estado process-wide foram testadas separadamente e
documentadas como limites, nao como isolamento pleno.

## Matriz

| Scenario | Expected isolation | Test |
| --- | --- | --- |
| Same type, different mapping | Suportado por runtimes distintos. O mesmo tipo de entidade pode ter `Name` vindo de colunas diferentes quando a chamada usa `runtime.QueryMapped<T>()`. Caches de materializacao ficam em cada runtime. | `ConfigurationIsolationHardeningTests.IsolatedRuntimesShouldMaterializeSameEntityWithDifferentMappingsConcurrently` |
| Same profile, different config | Suportado por runtimes distintos. O mesmo tipo de profile pode mapear a mesma entidade de formas diferentes sem colisao quando usado por `runtime.QueryMapped<T, TProfile>()`. | `ConfigurationIsolationHardeningTests.ProfilesConvertersAndDiagnosticsShouldStayIsolatedAcrossConcurrentRuntimes`; cobertura previa em `IsolatedRuntimeTests.RuntimeShouldKeepSameProfileTypeIsolatedAcrossConfigurations` |
| Same converter, different config | Suportado quando converter metadata pertence ao snapshot do map de cada runtime. O mesmo tipo de converter pode ser usado por runtimes com colunas diferentes sem colisao. Instancias/delegates continuam assumidos stateless/thread-safe. | `ConfigurationIsolationHardeningTests.SameConverterTypeShouldRemainScopedToDifferentRuntimeMappings`; `ConfigurationIsolationHardeningTests.ProfilesConvertersAndDiagnosticsShouldStayIsolatedAcrossConcurrentRuntimes`; cobertura previa em `IsolatedRuntimeTests.RuntimeShouldScopeConvertersToConfiguration` |
| Generated materializer | Suportado. Descritores gerados pertencem ao snapshot e sao indexados no registry do runtime; dois runtimes podem registrar o mesmo shape com delegates diferentes. | `ConfigurationIsolationHardeningTests.GeneratedMaterializersShouldRemainConfigurationScopedUnderConcurrentMaterialization`; cobertura previa em `IsolatedRuntimeTests.RuntimeShouldScopeGeneratedMaterializersToConfiguration` |
| Dommel metadata | Nao isolado por runtime nesta etapa. Resolvers Dommel consultam `FluentMapper.EntityMaps`/`TypeConventions` e sao instalados globalmente em `DommelMapper`. Configuracoes isoladas do core nao dirigem Dommel. | `ManualMappingTests.DommelResolversShouldUseOnlyLegacyProcessWideConfiguration` |
| Diagnostics | Suportado por runtime. `runtime.Validate()` e `runtime.Explain<T>()` usam o registry do runtime e nao leem `FluentMapper.Runtime`. | `ConfigurationIsolationHardeningTests.ProfilesConvertersAndDiagnosticsShouldStayIsolatedAcrossConcurrentRuntimes`; cobertura previa em `IsolatedRuntimeTests.RuntimeDiagnosticsShouldUseItsConfigurationWithoutGlobalState` |
| Parallel tests | Suportado para novos APIs por testes que criam runtimes locais sem `FluentMapper.Reset()`. A suite principal ainda desabilita paralelismo no assembly por causa de testes legados globais. | `ConfigurationIsolationHardeningTests.IsolatedRuntimesShouldMaterializeSameEntityWithDifferentMappingsConcurrently`; `ConfigurationIsolationHardeningTests.SameRuntimeShouldMaterializeConcurrentReadersThroughOneScopedCache`; `FluentMapServiceCollectionExtensionsTests.IndependentServiceProvidersShouldResolveIndependentRuntimesConcurrently` |

## Limites globais comprovados

### Dapper

`Dapper.Query<T>()` usa o type map registrado em `SqlMapper.SetTypeMap`, que e
process-wide por tipo. Portanto, duas configuracoes FluentMap distintas nao
podem controlar simultaneamente `connection.Query<T>()` para o mesmo `T`.

Teste:

- `ConfigurationIsolationHardeningTests.DapperQueryShouldUseOnlyThePublishedGlobalTypeMapForSameEntity`

Conclusao:

- `runtime.QueryMapped<T>()`: isolado por runtime;
- `connection.Query<T>()`: limitado ao type map global publicado pela bridge
  estatica.

### Dommel

`ForDommel()` instala resolvers/builders no `DommelMapper` global. Os resolvers
atuais leem as colecoes legadas de `FluentMapper`, inclusive metadata Dommel
especifica. Configuracoes criadas por `FluentMapConfigurationBuilder` nao sao
observadas por Dommel.

Teste:

- `ManualMappingTests.DommelResolversShouldUseOnlyLegacyProcessWideConfiguration`

Conclusao:

- Dommel segue bridge process-wide;
- multiplas configuracoes Dommel simultaneas nao sao contrato suportado nesta
  etapa.

## Concorrencia

Cobertura adicionada usa `Barrier` para sincronizar inicio das operacoes e
exercitar pontos de corrida importantes sem depender apenas de loops longos:

- multiplos runtimes usando a mesma entidade;
- mesmo runtime em varias threads;
- generated materializers concorrentes;
- profiles/converters/diagnostics concorrentes;
- service providers DI independentes;
- inicializacao estatica concorrente serializada pela bridge.

## Estado da matriz

Estado final do prompt 11.6:

```text
Core isolated runtime: resolved structurally for QueryMapped/runtime APIs
Legacy static bridge: compatibility only, process-wide
Dapper.Query<T> integration: structurally limited by Dapper global type maps
Dommel integration: structurally limited by Dommel global resolvers/builders
```
