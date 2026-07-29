# Roadmap Summary

Resumo das Etapas 7-12. Este documento consolida resultado e limitacoes, sem
substituir os final reports de cada etapa.

## Etapa 7 - Generated Materialization & Performance

Objetivo: adicionar contratos e integracao para materializadores gerados,
preservando runtime fallback e medindo impacto de alocacao/performance.

Resultado: concluida. Foram adicionados descriptors de generated materializer,
registro gerado, dispatch por shape ordenado, cobertura para flat, immutable,
nested, value objects, profiles e ignored properties. Benchmarks mostraram
reducao de alocacao no hot path gerado.

Limitacoes restantes: generated cobre subconjunto estatico; `IncludeBase`,
conventions dinamicas, TypeHandler no generated path e full Native AOT foram
adiados.

## Etapa 8 - Persistence Semantics & Historical Compatibility

Objetivo: separar semantica de leitura/escrita sem transformar o core em CRUD e
resolver regressoes historicas ligadas a Dommel/persistence.

Resultado: concluida. O core ganhou metadata de persistencia, APIs fluent como
`ReadOnly()`, `Computed()` e `DatabaseDefaultOnInsert()`, diagnostics e
integracao Dommel para writes gerados.

Limitacoes restantes: write SQL continua fora do core; Dommel permanece global;
builders customizados precisam respeitar metadata; provider-specific coverage
alem de SQLite ficou para matriz futura.

## Etapa 9 - Advanced Query Materialization

Objetivo: ampliar os caminhos opt-in de materializacao com QueryMultiple,
streaming sincronico, streaming assincrono e cancellation.

Resultado: concluida. Foram adicionados `QueryMultipleMapped`, `MappedGridReader`,
`ReadMapped*`, `QueryMappedUnbuffered*` e async streaming por `DbConnection`.
Generated/runtime fallback compartilham o mesmo dispatch.

Limitacoes restantes: nao ha `QueryMultipleMappedAsync`, streaming por grid em
`MappedGridReader`, Dapper multi-mapping por `splitOn`, graph aggregation ou
identity map.

## Etapa 10 - Property Conversion & Extensibility

Objetivo: adicionar conversores por propriedade e por direcao, mantendo
interoperabilidade com Dapper TypeHandlers e sem prometer write conversion ainda.

Resultado: concluida. Foram adicionados contratos read/write/bidirecionais,
delegates, metadata publica, runtime read conversion, generated read conversion
quando suportado e diagnostics/analyzers correspondentes.

Limitacoes restantes: write converters sao metadata-only; converters por
instancia/delegate usam fallback generated; TypeHandler no generated path segue
adiado; nao ha factory/DI/scoped converter lifetime.

## Etapa 11 - Configuration Isolation & DI

Objetivo: permitir configuracoes isoladas para materializacao controlada pelo
FluentMap e adicionar integracao opcional com DI sem quebrar a API estatica.

Resultado: concluida. Foram adicionados `FluentMapConfigurationBuilder`,
`ImmutableFluentMapConfiguration`, `FluentMapRuntime`, caches por runtime,
bridge estatica compativel e pacote `Dapper.FluentMap.DependencyInjection`.

Limitacoes restantes: `Dapper.Query<T>()` e Dommel continuam process-wide;
colecoes legadas permanecem mutaveis por compatibilidade; named/keyed DI e
Dommel isolation foram adiados.

## Etapa 12 - Compatibility, Hardening & Release Readiness

Objetivo: auditar compatibilidade, providers, API publica, pacotes, CI,
documentacao, supply chain, trimming/AOT e prontidao de release.

Resultado: concluida com recomendacao de Release Candidate, nao stable. A
solution passou restore/build/test, packages foram gerados, SQLite foi validado,
NuGet metadata foi endurecida, CI/release workflow foram preparados,
vulnerability audit passou e documentacao publica foi consolidada.

Limitacoes restantes: nao publicar `2.0.0`; criar baseline API/binaria do fork;
validar SourceLink no remoto; revisar analyzer/generator release manifests;
decidir SBOM/signing; certificar SQL Server/PostgreSQL apenas com infraestrutura
real; full Native AOT nao e claim suportado.
