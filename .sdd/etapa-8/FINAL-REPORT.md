# Etapa 8 - Final Report

## Objetivo

Encerrar a Etapa 8 - Persistence Semantics & Historical Compatibility com
auditoria da especificacao, hardening de API, documentacao publica, suite de
regressao historica e validacao completa da solution.

## Implementado

- Metadata publica aditiva de persistencia no core:
  `PropertyPersistenceMetadata` e `IPropertyMapWithPersistenceMetadata`.
- APIs fluent em `PropertyMapBase<TPropertyMap>`:
  `ExcludeFromInsert()`, `ExcludeFromUpdate()`, `ReadOnly()`, `Computed()` e
  `DatabaseDefaultOnInsert()`.
- `Ignore()` preservado como semantica historica de nao materializar e nao
  persistir.
- `MemberMappingExplanation.Persistence` exposto em `Explain<TEntity>()` e
  `Explain<TEntity, TProfile>()`.
- Runtime validation de invariants de persistence metadata.
- Analyzer `DFM012` para combinacoes contraditorias em fluent chains diretas.
- Source generator atualizado para tratar write metadata como neutra para
  materializacao gerada.
- Integracao Dommel consumindo metadata para `INSERT`, `UPDATE`, keys,
  identities, computed columns e defaults de banco.
- Documentacao publica do README atualizada em ingles e portugues.
- Notas de compatibilidade historica em `08-compatibility-notes.md`.
- Hardening final do Prompt 8.7: `DatabaseDefaultOnInsert().Computed()` agora
  falha como combinacao contraditoria, igual a ordem inversa.

## Persistence Semantics

| Requirement | Implementation | Tests | Status | Notes |
| --- | --- | --- | --- | --- |
| Separar leitura, insert e update | `PropertyPersistenceMetadata` com `ParticipatesInMaterialization`, `ParticipatesInInsert` e `ParticipatesInUpdate` | `PropertyPersistenceMetadataTests`, `ConfigurationValidationTests` | Concluido | Core descreve metadata, nao gera SQL. |
| Preservar `Ignore()` historico | `Ignore()` seta `PropertyPersistenceMetadata.Ignored` e continua afetando materializacao | `IgnoredPropertySelectedByDapperShouldRemainUnmappedWithoutThrowing`, generated/runtime regressions | Concluido | `Ignore()` nao foi reaproveitado para read-only. |
| Read-only materializa mas nao escreve | `ReadOnly()` preserva read e exclui insert/update | Core metadata, Dommel SQLite, historical regressions | Concluido | Resolve a lacuna central de #94. |
| Excluir apenas insert | `ExcludeFromInsert()` preserva update e read | Metadata tests, Dommel integration | Concluido | Dommel recompoe colunas de insert via SQL builder wrapper. |
| Excluir apenas update | `ExcludeFromUpdate()` preserva insert e read | Metadata tests, Dommel integration | Concluido | Dommel traduz para `ColumnPropertyInfo.IsGenerated` no caminho de update. |
| Database default on insert | `DatabaseDefaultOnInsert()` marca generated/default, omite insert e preserva update | `DatabaseDefaultOnInsertShouldOmitInsertColumnAndReadDatabaseValue` | Concluido | Caso documentado com `created_at DEFAULT ...`, sem provider especifico no README. |
| Computed | `Computed()` marca generated/computed e omite insert/update | Dommel integration e historical regression #123 | Concluido | Hardening 8.7 cobre as duas ordens com database default. |
| Key vs identity | Dommel metadata diferencia `IsKey()`, `IsIdentity()` e `SetGeneratedOption(None)` | Non-identity key e composite key tests | Concluido com compatibilidade | `IsKey()` sem generated option preserva comportamento operacional legado. |
| Generated materializers ignoram write metadata | Generator aceita chamadas read-neutral e runtime valida apenas semantica de leitura | Generator tests e generated registration integration | Concluido | Apenas `Ignore()` altera read descriptor. |
| Diagnostics conservadores | `DFM012` e runtime validation | Analyzer tests e configuration validation tests | Concluido | Analyzer nao infere fluxo dinamico por design. |
| Dommel consome metadata | Resolvers e `DommelPersistenceSqlBuilder` | 21 testes Dommel, SQLite real | Concluido | Builders customizados posteriores a `ForDommel()` precisam honrar metadata. |
| Documentacao publica | README EN/PT e compatibility notes | Revisao manual | Concluido | Sem documentar CRUD no core. |

## API adicionada

Core:

- `Dapper.FluentMap.Mapping.PropertyPersistenceMetadata`;
- `Dapper.FluentMap.Mapping.IPropertyMapWithPersistenceMetadata`;
- `PropertyMapBase<TPropertyMap>.Persistence`;
- `PropertyMapBase<TPropertyMap>.ExcludeFromInsert()`;
- `PropertyMapBase<TPropertyMap>.ExcludeFromUpdate()`;
- `PropertyMapBase<TPropertyMap>.ReadOnly()`;
- `PropertyMapBase<TPropertyMap>.Computed()`;
- `PropertyMapBase<TPropertyMap>.DatabaseDefaultOnInsert()`;
- `MemberMappingExplanation.Persistence`.

Dommel existente preservado e conectado a metadata:

- `DommelPropertyMap.IsKey()`;
- `DommelPropertyMap.IsIdentity()`;
- `DommelPropertyMap.SetGeneratedOption(DatabaseGeneratedOption option)`.

Revisao de consistencia:

- `IPropertyMap` nao foi alterada.
- A API nova e aditiva.
- Nao ha API de CRUD, SQL generator, DI, converters ou configuration instances
  no core.
- Nao foi identificado vazamento de Dommel para o core; key/identity continuam
  APIs publicas do pacote Dommel.
- A combinacao contraditoria `DatabaseDefaultOnInsert().Computed()` foi corrigida
  no Prompt 8.7.

## Dommel Integration

Dommel honra os behaviors para comandos gerados:

| Behavior | SELECT | INSERT | UPDATE |
| --- | --- | --- | --- |
| Normal | Sim | Sim | Sim |
| Ignore | Nao | Nao | Nao |
| ReadOnly | Sim | Nao | Nao |
| ExcludeFromInsert | Sim | Nao | Sim |
| ExcludeFromUpdate | Sim | Sim | Nao |
| DatabaseDefaultOnInsert | Sim | Nao | Sim |
| DatabaseDefaultOnInsert + ExcludeFromUpdate | Sim | Nao | Nao |
| Computed | Sim | Nao | Nao |
| Identity key | Sim | Nao | WHERE only |
| Non-identity key | Sim | Sim | WHERE only |

`Dapper.FluentMap.Dommel` envolve os SQL builders padrao do Dommel para recompor
as colunas de insert com base em `ParticipatesInInsert`. Para update, o resolver
traduz `ParticipatesInUpdate=false` para o contrato publico de generated columns
do Dommel.

## Historical Issues

| Issue | Status | Evidencia |
| --- | --- | --- |
| #94 ReadOnly Fields | Resolved by implementation; Regression covered | `ReadOnly()` + `DommelHistoricalRegressionTests.ReadOnlyPropertyShouldBeMaterializedButExcludedFromWrites`. |
| #114 Conflict between property and type members | Already fixed upstream; Regression covered | `ReflectionHelper` usa o `MemberInfo` real; `PropertyNamedLikeBclMemberShouldMapExpressionProperty`. |
| #122 Insert issue when key column is not identity | Resolved by architecture; Regression covered | Key e identity separados; `NonIdentityKeyShouldBeInsertedAndOnlyUsedForUpdateWhereClause`. |
| #123 Computed property used in insert/update | Resolved by implementation; Regression covered | `Computed()` e `SetGeneratedOption(Computed)` omitidos de insert/update; computed SQLite real. |
| #126 Nested properties ending with same name | Resolved by architecture; Regression covered | `MemberPath` completo; nested/generated regressions com `Rank.Level` e `Seniority.Level`. |
| #130 Default value do banco vs `Ignore()` | Resolved by implementation; Regression covered | `DatabaseDefaultOnInsert()` com coluna `created_at DEFAULT ...`. |
| #133 `Ignore()` causing `NotImplementedException` | Already fixed upstream; Regression covered | `DapperIgnoredMemberMap`; Dapper query selecionando coluna ignorada sem throw. |

## Regression Coverage

- Core metadata defaults, read-only, computed, database default, ignore,
  exclusoes por operacao, inherited maps e profiles.
- Runtime validation de metadata efetiva e maps customizados invalidos.
- Analyzer `DFM012` para combinacoes invalidas nas duas ordens relevantes.
- Dapper normal mapping para `Ignore()` e propriedades com nomes conflitantes.
- `QueryMapped*` runtime e generated materializer para read semantics.
- Dommel SQLite real para insert, update, select, defaults, computed columns,
  identities, non-identity keys e composite keys.
- Historical regression suite dedicada para #94, #114, #122, #123, #126, #130 e
  #133.

## Backward Compatibility

- Sem remocao de APIs publicas existentes.
- `IPropertyMap` preservada, evitando breaking change binario para
  implementacoes customizadas.
- `Ignore()` preserva comportamento historico de leitura.
- `Dapper.Query<T>()`, `QueryMapped*`, profiles e generated materializers nao
  usam metadata de insert/update para decidir materializacao.
- `IsKey()` sem `SetGeneratedOption(DatabaseGeneratedOption.None)` continua
  identity operacional no Dommel para compatibilidade.
- O repositorio nao possui ferramenta dedicada de API/binary compatibility; a
  revisao foi feita por diff da superficie publica e build/pack Release.

## Breaking Changes

Nenhuma breaking change intencional foi introduzida.

Correcoes de bug podem alterar configuracoes contraditorias que antes eram
aceitas por acidente. No Prompt 8.7, `DatabaseDefaultOnInsert().Computed()` passa
a falhar com `FluentMapConfigurationException`, alinhado a especificacao e ao
analyzer.

## Known Limitations

- O core nao gera SQL e nao adiciona CRUD.
- Dommel custom SQL builders registrados depois de `ForDommel()` substituem o
  wrapper de insert e devem honrar `ParticipatesInInsert` por conta propria.
- Dommel trabalha com propriedades flat; nested materialization continua sendo
  responsabilidade de `QueryMapped*`.
- Analyzer `DFM012` cobre apenas fluent chains estaticamente visiveis; cenarios
  dinamicos dependem de runtime validation.
- `QueryMapped*` permanece buffered e nao oferece streaming.
- Native AOT completo nao foi declarado nesta etapa.

## Technical Debt

- Avaliar uma ferramenta formal de API/binary compatibility antes de release
  publica maior.
- Investigar uma estrategia menos global para caches/resolvers Dommel em etapa
  dedicada, sem quebrar o modelo atual de startup.
- Melhorar diagnostico amigavel para persistence metadata em logs se houver
  demanda de usuarios.
- Revisar provider-specific SQL builders Dommel alem de SQLite quando houver
  matriz de CI ou demanda real.

## Deferred Items

- `QueryMultipleMapped`.
- Streaming e `IAsyncEnumerable`.
- Property converters.
- DI e configuration instances.
- CRUD ou SQL generator no core.
- Provider-specific hardening amplo de Dommel.
- Compatibilidade Native AOT alem dos cenarios ja validados em etapas anteriores.

## Recommendations for Etapa 9

- Manter o escopo de Etapa 9 separado de persistence semantics.
- Se Etapa 9 tocar materializacao, preservar a regra: write metadata nao altera
  leitura, exceto `Ignore()`.
- Antes de ampliar Dommel, decidir se a limitacao de builders customizados deve
  virar API publica, documentacao adicional ou teste provider-specific.
- Considerar API compatibility tooling antes de preparar release NuGet.

## Validation

Executado em 2026-07-28:

```bash
dotnet restore ./Dapper.FluentMap.sln
dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build
dotnet test ./test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj --configuration Release --no-build
dotnet pack ./src/Dapper.FluentMap/Dapper.FluentMap.csproj --configuration Release --no-build --output ./artifacts/packages
```

Resultados:

- Restore: sucesso.
- Build Release: sucesso, 0 warnings, 0 errors.
- Solution tests: sucesso, 298 testes aprovados.
- Dommel tests isolados: sucesso, 21 testes aprovados.
- Pack core: sucesso; warning legado `NU5125` sobre `PackageLicenseUrl` /
  `licenseUrl`.
- Pacote inspecionado: `lib/netstandard2.0/Dapper.FluentMap.dll` e
  `lib/netstandard2.0/Dapper.FluentMap.xml` presentes.
