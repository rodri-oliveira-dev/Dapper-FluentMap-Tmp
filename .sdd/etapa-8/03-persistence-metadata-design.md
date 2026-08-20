# Etapa 8 - Persistence Metadata Design

Status: implementado no Prompt 8.2.

## Objetivo

Representar, no core, metadata de persistencia separada da metadata de leitura
sem adicionar CRUD ao FluentMap. O modelo deve ser consumivel por diagnostics,
Dommel e extensoes futuras.

## Classes e interfaces

### `PropertyPersistenceMetadata`

Tipo publico imutavel em `Dapper.FluentMap.Mapping`.

Propriedades:

- `ParticipatesInMaterialization`;
- `ParticipatesInInsert`;
- `ParticipatesInUpdate`;
- `IgnoredByFluentMap`;
- `IsKey`;
- `IsIdentity`;
- `IsGenerated`;
- `IsComputed`;
- `HasDatabaseDefaultOnInsert`.

Instancias estaticas:

- `PropertyPersistenceMetadata.Default`;
- `PropertyPersistenceMetadata.Ignored`.

O tipo nao expoe setters. As alteracoes durante configuracao fluente criam uma
nova instancia e substituem a referencia mantida pelo `PropertyMapBase`.

### `IPropertyMapWithPersistenceMetadata`

Interface publica aditiva:

```csharp
public interface IPropertyMapWithPersistenceMetadata
{
    PropertyPersistenceMetadata Persistence { get; }
}
```

Ela evita alterar `IPropertyMap`, preservando compatibilidade binaria com
implementacoes customizadas.

### `PropertyMapBase<TPropertyMap>`

Agora implementa `IPropertyMapWithPersistenceMetadata` e possui:

```csharp
public PropertyPersistenceMetadata Persistence { get; }
```

APIs fluent publicas adicionadas:

```csharp
ExcludeFromInsert();
ExcludeFromUpdate();
ReadOnly();
Computed();
DatabaseDefaultOnInsert();
```

APIs protegidas para maps derivados:

```csharp
UsePersistence(...);
MarkAsKey();
MarkAsIdentity();
MarkAsNotGenerated();
MarkAsComputed();
```

### Diagnostics

`MemberMappingExplanation` agora expoe:

```csharp
public PropertyPersistenceMetadata Persistence { get; }
```

`FluentMapper.Explain<T>()` e `Explain<T, TProfile>()` passam a carregar a
metadata efetiva para mappings explicitos, herdados, convencoes e fallback
Dapper.

## Defaults

Propriedade mapeada normal:

```text
Materialization = yes
Insert          = yes
Update          = yes
Ignored         = no
Key             = no
Identity        = no
Generated       = no
Computed        = no
DefaultOnInsert = no
```

Fallback Dapper e maps customizados sem a interface opcional usam o mesmo
default quando `Ignored=false`.

## Invariants

### Ignore

`Ignore()` continua sendo historico e total:

```text
Ignore
=> Materialization=no
=> Insert=no
=> Update=no
=> Key=no
=> Identity=no
=> Generated=no
=> Computed=no
=> DefaultOnInsert=no
```

APIs de escrita chamadas depois de `Ignore()` falham com
`FluentMapConfigurationException`.

### Read-only

```text
ReadOnly()
=> Materialization=yes
=> Insert=no
=> Update=no
=> Ignored=no
```

Read-only nao implica generated.

### Exclude por operacao

```text
ExcludeFromInsert() => Insert=no, Update preservado
ExcludeFromUpdate() => Update=no, Insert preservado
```

Chamadas repetidas e combinacoes equivalentes sao idempotentes.

### Computed

```text
Computed()
=> Materialization=yes
=> Insert=no
=> Update=no
=> Generated=yes
=> Computed=yes
=> DefaultOnInsert=no
```

Computed nao pode ser combinado com `DatabaseDefaultOnInsert()` nem com key.

### Database default on insert

```text
DatabaseDefaultOnInsert()
=> Materialization=yes
=> Insert=no
=> Update=yes
=> Generated=yes
=> DefaultOnInsert=yes
```

Pode ser combinado com `ExcludeFromUpdate()` quando o valor tambem deve ser
read-only depois do insert.

### Key

```text
IsKey()
=> Key=yes
=> Identity=no
=> Insert=yes
=> Update=no
```

Key nao identity continua insertable no modelo de metadata.

### Identity

```text
IsIdentity()
=> Key=yes
=> Identity=yes
=> Generated=yes
=> Insert=no
=> Update=no
```

Identity e tratado como key gerada pelo banco.

## Combinacoes validas

- default mapping;
- `ReadOnly()`;
- `ExcludeFromInsert()`;
- `ExcludeFromUpdate()`;
- `ExcludeFromInsert().ExcludeFromUpdate()`;
- `DatabaseDefaultOnInsert()`;
- `DatabaseDefaultOnInsert().ExcludeFromUpdate()`;
- `Computed()`;
- `IsKey()`;
- `IsKey().SetGeneratedOption(DatabaseGeneratedOption.None)`;
- `IsIdentity().IsKey()` e `IsKey().IsIdentity()`;
- mappings herdados com qualquer metadata valida;
- profiles com metadata propria.

## Combinacoes invalidas

- `Ignore().ReadOnly()`;
- `Ignore().ExcludeFromInsert()`;
- `Ignore().ExcludeFromUpdate()`;
- `Ignore().Computed()`;
- `Ignore().DatabaseDefaultOnInsert()`;
- `Computed().DatabaseDefaultOnInsert()`;
- `Computed().IsKey()`;
- `Computed().IsIdentity()`;
- `DatabaseDefaultOnInsert().IsIdentity()`.

`Ignore()` chamado no fim da cadeia continua permitido e domina a metadata,
preservando o significado historico de "nao mapear".

## Backward compatibility

- `IPropertyMap` nao foi alterada.
- `Ignore()` nao mudou de significado.
- `PropertyMap` e `DommelPropertyMap` continuam existindo.
- APIs Dommel existentes foram preservadas.
- `IsKey()` no modelo de metadata nao implica identity, mas o resolver Dommel
  ainda preserva o comportamento operacional historico quando `GeneratedOption`
  nao e especificado.
- `SetGeneratedOption(DatabaseGeneratedOption.None)` continua sendo a forma
  compativel de declarar key nao gerada no Dommel atual.
- `Dapper.Query<T>()`, `QueryMapped*` e generated materializers continuam
  consumindo apenas semantica de leitura/ignore.

## Relacao com mappings existentes

Mappings explicitos, convencoes, naming policies, inherited maps e profiles
recebem metadata default automaticamente porque todos usam `PropertyMapBase`.

Implementacoes customizadas que implementam apenas `IPropertyMap` continuam
validas. Internamente elas sao adaptadas para:

```text
Ignored=true  => PropertyPersistenceMetadata.Ignored
Ignored=false => PropertyPersistenceMetadata.Default
```

## Relacao com Dommel

`DommelPropertyMap` escreve na metadata ao chamar:

- `IsKey()`;
- `IsIdentity()`;
- `SetGeneratedOption(...)`.

O resolver Dommel tambem consegue consumir `IPropertyMapWithPersistenceMetadata`
em maps do core ou customizados quando o estado pode ser representado pelo
contrato atual do Dommel.

Para `DommelPropertyMap`, o resolver calcula uma `EffectiveGeneratedOption`:

- `GeneratedOption` explicito vence;
- identity vira `DatabaseGeneratedOption.Identity`;
- metadata sem insert/update vira `DatabaseGeneratedOption.Computed`;
- key sem opcao explicita preserva o legado como identity operacional;
- demais propriedades usam `None`.

Nao ha geracao de SQL no core. Separacao fina `Insert=no`, `Update=yes` ainda
nao cabe no contrato atual `ColumnPropertyInfo.IsGenerated` sem alterar
comportamento de update, entao fica como metadata para prompt posterior.

## Relacao com source generator

Generated materializers continuam observando apenas:

- `ParticipatesInMaterialization`;
- `IgnoredByFluentMap` via o flag historico `Ignored`.

Metadata de insert/update/key/identity/computed/default nao altera delegates
`IDataRecord -> entity`.

Prompts futuros podem atualizar analyzers/generator para reconhecer chamadas da
DSL e emitir diagnostics, mas nao devem alterar materializacao por causa de
semantica de escrita.

## Relacao com diagnostics

`Explain<T>()` ja expoe a metadata em `MemberMappingExplanation.Persistence`.
Isso permite:

- inspecionar defaults;
- distinguir `Ignore()` de `ReadOnly()`;
- ver metadata herdada;
- ver metadata especifica de profile;
- sustentar diagnostics futuros sem depender de SQL/CRUD.

Diagnostics mais fortes para combinacoes contraditorias podem evoluir sobre o
mesmo modelo.
