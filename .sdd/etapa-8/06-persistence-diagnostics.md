# Etapa 8 - Persistence Diagnostics & Validation

Status: implementado no Prompt 8.5.

## Objetivo

Detectar cedo combinacoes contraditorias de persistence behavior sem transformar
combinacoes legitimas em erro. Diagnostics devem preservar a separacao de
concerns:

- analyzers reportam apenas cadeias fluentes estaticamente provaveis;
- runtime validation valida a metadata efetiva registrada;
- generated materializers observam somente leitura/materializacao;
- `Explain<TEntity>()` expoe a metadata efetiva para inspecao.

## Matriz

```text
Condition                                      Detectable at compile time? Detectable at startup? Severity Diagnostic
Default mapping                                No                         Yes                    None     None
ReadOnly                                       Yes, if direct chain        Yes                    None     None
ExcludeFromInsert                              Yes, if direct chain        Yes                    None     None
ExcludeFromUpdate                              Yes, if direct chain        Yes                    None     None
ExcludeFromInsert + ExcludeFromUpdate          Yes, if direct chain        Yes                    None     None
DatabaseDefaultOnInsert                        Yes, if direct chain        Yes                    None     None
DatabaseDefaultOnInsert + ExcludeFromUpdate    Yes, if direct chain        Yes                    None     None
Computed                                       Yes, if direct chain        Yes                    None     None
IsKey + SetGeneratedOption(None)               Yes, if direct chain        Yes                    None     None
IsIdentity                                     Yes, if direct chain        Yes                    None     None
Ignore last in the chain                       Yes, if direct chain        Yes                    None     None
Ignore + ReadOnly                              Yes, if direct chain        Yes                    Error    DFM012 / FluentMapConfigurationException
Ignore + ExcludeFromInsert                     Yes, if direct chain        Yes                    Error    DFM012 / FluentMapConfigurationException
Ignore + ExcludeFromUpdate                     Yes, if direct chain        Yes                    Error    DFM012 / FluentMapConfigurationException
Ignore + Computed                              Yes, if direct chain        Yes                    Error    DFM012 / FluentMapConfigurationException
Ignore + DatabaseDefaultOnInsert               Yes, if direct chain        Yes                    Error    DFM012 / FluentMapConfigurationException
Ignore + IsKey/IsIdentity                      Yes, if direct chain        Yes                    Error    DFM012 / FluentMapConfigurationException
Computed + DatabaseDefaultOnInsert             Yes, if direct chain        Yes                    Error    DFM012 / FluentMapConfigurationException
Computed + IsKey                               Yes, if direct chain        Yes                    Error    DFM012 / FluentMapConfigurationException
Computed + IsIdentity                          Yes, if direct chain        Yes                    Error    DFM012 / FluentMapConfigurationException
DatabaseDefaultOnInsert + IsIdentity           Yes, if direct chain        Yes                    Error    DFM012 / FluentMapConfigurationException
Computed + InsertEnabled                       No public enabling API      Yes, for custom metadata Error    FluentMapConfigurationException
Computed + UpdateEnabled                       No public enabling API      Yes, for custom metadata Error    FluentMapConfigurationException
Identity + explicit insert requirement         No public enabling API      Yes, for custom metadata Error    FluentMapConfigurationException
Key + UpdateEnabled                            No public enabling API      Yes, for custom metadata Error    FluentMapConfigurationException
Null persistence metadata                      No                         Yes                    Error    FluentMapConfigurationException
Ignored flag disagrees with metadata           No                         Yes                    Error    FluentMapConfigurationException
Write metadata on generated read materializer  Yes                        Yes                    None     None
```

## Runtime validation

`MappingConfigurationValidator` validates the effective metadata for explicit
maps, composed inherited maps, profiles and conventions. The validation rejects
custom `IPropertyMapWithPersistenceMetadata` implementations when their
metadata is null or contradicts the persistence invariants.

The validated invariants are:

- ignored properties do not participate in materialization, insert, update, key
  or generated behavior;
- non-ignored properties participate in materialization;
- computed properties are generated read-only values and are not key, identity
  or database-default-on-insert values;
- identity properties are generated keys and do not participate in insert,
  update or database-default-on-insert behavior;
- key properties do not participate in generated UPDATE SET behavior;
- database-default-on-insert properties are generated values omitted from insert
  and are not computed or identity values;
- `IPropertyMap.Ignored` and `Persistence.IgnoredByFluentMap` must agree.

## Analyzer

`DFM012` reports invalid persistence behavior when it is directly visible in a
map constructor fluent chain. The analyzer does not execute map constructors,
does not scan assemblies and does not infer behavior from variables or dynamic
control flow.

Examples reported:

```csharp
Map(e => e.Name).Ignore().ReadOnly();
Map(e => e.Total).Computed().DatabaseDefaultOnInsert();
Map(e => e.Code).Computed().IsKey();
```

Examples intentionally not reported:

```csharp
Map(e => e.Name).ReadOnly();
Map(e => e.CreatedAt).DatabaseDefaultOnInsert().ExcludeFromUpdate();
Map(e => e.Code).IsKey().SetGeneratedOption(DatabaseGeneratedOption.None);
```

## Generated source diagnostics

Generated materializers continue to treat persistence write metadata as neutral
for reads. `ExcludeFromInsert()`, `ExcludeFromUpdate()`, `ReadOnly()`,
`Computed()` and `DatabaseDefaultOnInsert()` must not produce generated
materializer fallback diagnostics by themselves.

Only `Ignore()` changes generated read behavior, because it disables
materialization.

## Explain API

No new public API was required in this prompt. `Explain<TEntity>()` and
`Explain<TEntity, TProfile>()` already expose `MemberMappingExplanation.Persistence`,
which provides:

```text
Read:        ParticipatesInMaterialization
Insert:      ParticipatesInInsert
Update:      ParticipatesInUpdate
Generated:   IsGenerated / IsComputed / IsIdentity / HasDatabaseDefaultOnInsert
```

Future work can add a formatted display helper if API usability research shows
that consumers need a textual summary, but the structured metadata is the
stable public contract.

