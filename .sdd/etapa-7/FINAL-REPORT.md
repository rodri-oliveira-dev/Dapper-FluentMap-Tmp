# Etapa 7 - Final Report

## Objetivo

Finalizar a Etapa 7 - Generated Materialization & Performance com evidencia de implementacao, regressao, performance, trimming e Native AOT, sem iniciar funcionalidades da Etapa 8 e sem declarar compatibilidade maior do que a validacao sustenta.

## Implementado

- Contratos publicos aditivos para materializadores gerados:
  - `GeneratedRowMaterializer<TEntity>`;
  - `GeneratedMaterializerColumn`;
  - `GeneratedMaterializerDescriptor<TEntity>`;
  - overloads `AddGeneratedMaterializer(...)` em `FluentMapConfiguration`.
- Registry interno de generated materializers por entidade, profile e shape ordenado de colunas.
- Dispatch em `QueryMapped*` antes da iteracao das linhas, com fallback para `NestedMaterializationPlan`.
- Source generator emitindo `AddGeneratedMappings()` com `AddMap<TMap>()`, `AddProfile<TMap>()` e materializers gerados quando o map e estaticamente suportado.
- Generated materializers para:
  - propriedades flat;
  - construtores root simples;
  - nested mutable objects;
  - nested immutable objects;
  - Value Objects por componentes;
  - profiles;
  - ignored properties.
- Diagnostics conservadores:
  - `DFM011` informativo para maps registrados mas sem materializer gerado;
  - `Explain<T>()` indica presenca de descriptors generated sem prometer dispatch para uma query especifica.

## Arquitetura final

`QueryMapped*` continua executando SQL pelo Dapper, le o shape ordenado do `IDataReader` e tenta localizar um generated materializer por:

```text
EntityType + ProfileType opcional + ordered ColumnShape
```

Quando o descriptor existe e ainda corresponde ao mapping efetivo, o loop de linhas chama o delegate gerado. Quando nao existe match seguro, o runtime usa o `NestedMaterializationPlan` cacheado.

O generated path e uma otimizacao de materializacao `IDataRecord -> entidade`. Ele nao gera SQL, nao cria commands, nao substitui `Dapper.Query<T>()`, nao replica Dapper.AOT e nao remove fallback runtime.

## Audit matrix

| Requirement | Implementation | Tests | Benchmark | AOT/Trim impact | Status |
| --- | --- | --- | --- | --- | --- |
| Runtime fallback obrigatorio | `TryGetGeneratedMaterializer` cai para `NestedMaterializationPlan` quando descriptor falta ou diverge | `GeneratedMaterializerContractTests`, `GeneratedRegistrationIntegrationTests` | Runtime fallback comparado por shape reordenado | Mantem APIs `QueryMapped*` anotadas | Concluido |
| Lookup por entidade/profile/shape ordenado | `MaterializationPlanCacheKey` usado no registry generated | testes de default/profile/shape divergente | Benchmarks generated vs fallback por ordem diferente | Evita ordinais incorretos; shape real ainda e runtime | Concluido |
| Flat/simple generated materialization | Generator emite assignments diretos para propriedades root suportadas | generator e integracao | `QueryMappedSimple` generated vs fallback | Reduz reflection no hot path, mas lookup ainda valida metadata | Concluido |
| Constructor/immutable root | Generator seleciona construtor publico deterministico | generator, integracao e runtime fallback equivalence | `QueryMappedImmutableConstructor` | Sem `Expression.Compile` no delegate gerado | Concluido |
| Nested mutable objects | Metadata tree interna e null subtree por ordinais | generator, integracao, runtime tests | `QueryMappedNestedObject` generated vs fallback | Hot path gerado evita setters compilados | Concluido |
| Value Objects por componentes | Constructor composition bottom-up | generator, integracao, `ValueObjectMaterializationTests` | `QueryMappedValueObject` generated vs fallback | Hot path gerado evita constructor delegates runtime | Concluido |
| Profiles | Descriptor separado por `TProfile`; profile nao altera Dapper type map global | contract tests e integracao generated profile/nested profile | Coberto funcionalmente; sem benchmark separado | Registro gerado evita scanning para profiles na compilacao atual | Concluido |
| Ignored properties | Descriptor `GeneratedMaterializerColumn.Ignore(...)`; delegate nao atribui membro ignorado | generator test e integracao final 7.7 | Nao benchmarkado isoladamente | Preserva compatibilidade do mapping efetivo | Concluido |
| Duplicate member paths e nomes conflitantes | Analyzer/runtime detectam duplicidades; generator preserva member path completo | analyzer tests, same terminal tests, integracao `Rank.Level`/`Seniority.Level` | Nao benchmarkado isoladamente | Evita generated descriptor ambiguo | Concluido |
| Concurrency | Registry usa `ConcurrentDictionary`; generated delegates emitidos sem estado | contract tests de lookup/queries concorrentes | Nao benchmarkado | Sem estado mutavel por linha no generated code | Concluido |
| Diagnostics generated/fallback | `Explain<T>()` indica descriptors registrados; `DFM011` informa fallback estatico | diagnostics tests e generator tests | Nao aplicavel | Nao promete AOT-safe por query | Concluido |
| IncludeBase gerado | Generator registra maps com `IncludeBase`, mas nao emite materializer gerado | integracao valida fallback funcional de derived map | Nao benchmarkado | Fallback runtime preservado | Adiado intencionalmente |
| Conventions/naming policies geradas | Registro/conventions funcionam; materializer gerado nao cobre fonte dinamica | runtime/analyzer existentes | Nao benchmarkado | Assembly scanning/conventions dinamicas seguem trimming-sensitive | Adiado intencionalmente |
| TypeHandlers no generated path | Scalar Value Object por TypeHandler continua no fallback runtime | `ValueObjectMaterializationTests` | Nao benchmarkado | Boundary segura com Dapper permanece em aberto | Adiado intencionalmente |
| Trimming validation | Smoke explicit e generated publicados com `PublishTrimmed=true` e executados | `Dapper.FluentMap.AotSmoke` | Nao aplicavel | Explicit: warning Dapper `IL2104`; generated: `IL2026` esperado em `QueryMapped*` + `IL2104` | Parcialmente concluido |
| Native AOT validation | Smoke criado, mas publish local bloqueado por ausencia de linker nativo | comando de publish AOT executado | Nao aplicavel | `IL2026`/`IL3050` esperados em `QueryMapped*`; execucao AOT nao validada | Bloqueado por ambiente |

## Performance

Benchmark final steady state, `ShortRun`, 1000 linhas por operacao:

| Scenario | Generated final | Runtime final | Baseline 7.2 | Dapper puro |
| --- | ---: | ---: | ---: | ---: |
| Simple allocation | 261.12 KB | 361.48 KB | 361.28 KB | 283.17 KB |
| Immutable allocation | 261.05 KB | n/a final dedicado | 423.78 KB | 283.17 KB |
| Nested allocation | 292.44 KB | 377.06 KB | 376.86 KB | 283.17 KB |
| Value Object allocation | 276.47 KB | 587.9 KB | 587.7 KB | 283.17 KB |

Leitura:

- A evidencia mais forte e reducao de alocacao no hot path gerado.
- Tempo local permanece ruidoso; a Etapa 7 nao deve documentar promessa publica de latencia.
- `DapperWithFluentMapRootMapping` segue alinhado com Dapper puro em alocacao.

## Native AOT / Trimming

Validacoes executadas:

- `dotnet publish ... -p:PublishTrimmed=true` para smoke de registro explicito: sucesso; executavel retornou `explicit:ok`; warning conhecido de Dapper `IL2104`.
- `dotnet publish ... -p:PublishTrimmed=true -p:DefineConstants=AOT_SMOKE_GENERATED` para registro gerado + generated materializer via `QueryMappedSingle`: sucesso; executavel retornou `generated:ok`.
- `dotnet publish ... -p:PublishAot=true` para smoke explicito: bloqueado por ambiente, erro "Platform linker not found".
- `dotnet publish ... -p:PublishAot=true -p:DefineConstants=AOT_SMOKE_GENERATED`: bloqueado pelo mesmo erro de linker; antes do bloqueio foram emitidos `IL2026` e `IL3050` esperados nas chamadas de `QueryMappedSingle`.

Interpretacao:

- Registro explicito e registro gerado sao os caminhos preferenciais para trimmed apps.
- Generated materializers executaram em publish trimmed no smoke local.
- `QueryMapped*` permanece corretamente anotado com `RequiresUnreferencedCode` e `RequiresDynamicCode`, porque ainda pode cair para fallback runtime baseado em reflection/dynamic code.
- A biblioteca nao deve ser descrita como "fully Native AOT compatible" nesta etapa.

## Compatibilidade

- Nenhuma API publica existente foi removida.
- APIs novas sao aditivas.
- `Dapper.Query<T>()` e type maps globais permanecem compatíveis.
- Profiles continuam query-scoped por `QueryMapped<TEntity, TProfile>()`.
- Dommel nao foi alterado.
- Fallback runtime preserva maps dinamicos, conventions, `IncludeBase`, TypeHandlers e shapes nao gerados.

## Regression coverage

Cobertura confirmada para:

- simple mappings;
- constructors;
- nested mappings;
- immutable types;
- Value Objects;
- profiles;
- fallback por ausencia/divergencia de descriptor e por shape reordenado;
- ignored properties;
- duplicate member paths;
- member paths com mesmo terminal;
- concurrency de lookup e queries generated;
- analyzers para duplicidade e registros invalidos;
- smoke de trimming explicito e gerado.

## Limitações conhecidas

- `QueryMapped*` continua bufferizado.
- Generated materializers cobrem apenas o subconjunto estatico suportado da DSL.
- Query shapes extras, ausentes ou reordenados usam fallback runtime.
- `IncludeBase<TBase>()`, conventions customizadas, naming policies como fonte gerada e TypeHandlers permanecem no fallback.
- O lookup generated ainda valida descriptor contra mapping efetivo por query, usando metadata runtime.
- Native AOT publish/run nao foi validado localmente por ausencia do linker nativo.

## Dívidas técnicas

- Medir cold start generated sem criar API publica de reset apenas para benchmark.
- Melhorar diagnostico por query/shape sem acoplar `Explain<T>()` a SQL.
- Avaliar uma boundary publica segura para TypeHandlers no generated path.
- Reduzir dependencia de metadata runtime no lookup generated, se houver caminho compativel e seguro.

## Itens adiados

- Generated materializers para `IncludeBase<TBase>()`.
- Generated materializers para conventions/naming policies built-in configuradas estaticamente.
- Manifests de assemblies referenciados.
- API de diagnostico de dispatch por column shape.
- Suporte generated para TypeHandlers.
- Declaracao de compatibilidade Native AOT alem dos cenarios validados.

## Recomendações para Etapa 8

- Priorizar diagnostico publico por shape antes de ampliar cobertura gerada.
- Investigar generated cold start e isolamento de caches sem expor reset perigoso.
- Separar qualquer trabalho de Native AOT em matriz CI com toolchain nativa instalada.
- Manter TypeHandler/generated boundary como design dedicado, com testes de Dapper real.
