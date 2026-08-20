# Etapa 8 - Compatibility Notes

## Objetivo

Orientar usuarios do FluentMap historico na migracao de usos ambigues de
`Ignore()` e metadata Dommel para persistence behaviors explicitos, sem exigir
redesign de maps existentes.

## Regra principal

`Ignore()` continua sendo uma semantica de leitura e escrita:

```text
Read   = no
Insert = no
Update = no
```

Use `Ignore()` somente quando a propriedade nao deve ser materializada pelo
FluentMap.

## Migracoes comuns

### Valor read-only do banco

Antes:

```csharp
Map(entity => entity.ServerValue)
    .ToColumn("server_value")
    .Ignore();
```

Agora:

```csharp
Map(entity => entity.ServerValue)
    .ToColumn("server_value")
    .ReadOnly();
```

Resultado:

```text
SELECT: participa
INSERT: excluido
UPDATE: excluido
```

### Default aplicado no insert

Antes:

```csharp
Map(entity => entity.CreatedAt)
    .ToColumn("created_at")
    .Ignore();
```

Agora:

```csharp
Map(entity => entity.CreatedAt)
    .ToColumn("created_at")
    .DatabaseDefaultOnInsert();
```

Use `.ExcludeFromUpdate()` junto se o valor tambem nao deve ser alterado depois
do insert.

### Coluna computed

Antes:

```csharp
Map(entity => entity.Total)
    .ToColumn("total")
    .SetGeneratedOption(DatabaseGeneratedOption.Computed);
```

Agora, quando estiver usando a API nova:

```csharp
Map(entity => entity.Total)
    .ToColumn("total")
    .Computed();
```

A API historica `SetGeneratedOption(DatabaseGeneratedOption.Computed)` continua
suportada no pacote Dommel e passa a alimentar a mesma metadata de persistencia.

### Key atribuida pela aplicacao

Antes, usar apenas `IsKey()` podia ser interpretado como identity operacional no
Dommel historico.

Agora:

```csharp
Map(entity => entity.Code)
    .ToColumn("code")
    .IsKey()
    .SetGeneratedOption(DatabaseGeneratedOption.None);
```

Esse e o caminho compativel para key non-identity: participa do `INSERT`, nao
entra no `SET` de `UPDATE` e e usada no `WHERE`.

## Compatibilidade preservada

- `IPropertyMap` nao foi alterada.
- `Ignore()` nao mudou de significado.
- APIs Dommel historicas `IsKey()`, `IsIdentity()` e `SetGeneratedOption(...)`
  continuam disponiveis.
- `IsKey()` sem `SetGeneratedOption(DatabaseGeneratedOption.None)` preserva o
  comportamento operacional legado de identity no resolver Dommel.

## Limites

O core descreve metadata, mas nao gera SQL e nao adiciona CRUD. A traducao para
`INSERT` e `UPDATE` acontece no pacote `Dapper.FluentMap.Dommel`.
