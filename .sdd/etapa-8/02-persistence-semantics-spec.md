# Etapa 8 - Persistence Semantics Specification

Status: especificacao inicial, sem implementacao produtiva.

## Objetivo

Definir um modelo coerente para propriedades que participam de leitura e escrita
de formas diferentes, preservando o escopo do FluentMap:

- o core descreve mapping e metadata;
- o core nao gera SQL e nao executa CRUD;
- o pacote Dommel pode consumir metadata de persistencia para seus comandos.

## Discovery do comportamento atual

### Core FluentMap

`PropertyMap` contem hoje:

- `ColumnName`;
- `CaseSensitive`;
- `Ignored`;
- `PropertyInfo`;
- `MemberPath` interno.

`Ignore()` significa "nao mapear este membro para leitura/materializacao":

- `DapperFluentPropertyTypeMap.GetMember` retorna `DapperIgnoredMemberMap` para
  maps ignorados;
- `NestedMaterializationPlan.Create` pula maps ignorados;
- generated materializers registram `GeneratedMaterializerColumn.Ignore`;
- diagnostics expõem `MemberMappingExplanation.Ignored`.

Nao ha dimensoes separadas de `Insert`, `Update`, `Generated`, `Computed`,
`Default` ou `Identity` no core.

### Dommel integration

`DommelPropertyMap` adiciona metadata especifica:

- `Key`;
- `Identity`;
- `GeneratedOption`.

Os resolvers atuais passam `DatabaseGeneratedOption` para Dommel:

- `DommelPropertyResolver.ResolveProperties` exclui `Ignored` e cria
  `ColumnPropertyInfo(property, generatedOption)`;
- `DommelKeyPropertyResolver.ResolveKeyProperties` usa maps marcados como
  `Key`, mais fallback de key default do Dommel;
- `DommelColumnNameResolver` resolve column name por propriedade flat;
- `DommelTableNameResolver` resolve table name.

No Dommel 3.5.3, `Insert` e `Update` constroem SQL a partir de
`Resolvers.Properties(type).Where(x => !x.IsGenerated)`, e usam key properties
separadamente para identity/where. Portanto, a integracao atual depende de
`ColumnPropertyInfo.IsGenerated`.

### Materializacao, constructor mapping, nested mappings, generated path e profiles

Esses caminhos pertencem a leitura:

- `Dapper.Query<T>()` usa type map global para root-level mapping;
- `QueryMapped*` usa runtime materializer ou generated materializer;
- profiles sao query-scoped apenas para `QueryMapped<TEntity, TProfile>()`;
- nested e Value Object mapping sao materializacao de leitura;
- generated materializers descrevem shape de leitura `IDataRecord -> entity`.

Nenhum desses caminhos deve decidir se uma propriedade entra em `INSERT` ou
`UPDATE`.

## Modelo conceitual

A propriedade deve ser descrita por dimensoes independentes. Esta lista e
conceitual e nao implica API publica direta:

| Dimensao | Pergunta | Consumidor primario |
| --- | --- | --- |
| `Read` | A coluna pode materializar esse membro? | Dapper type maps, `QueryMapped*`, generated materializers |
| `Insert` | A propriedade pode ser enviada em `INSERT` gerado? | Dommel |
| `Update` | A propriedade pode ser enviada no `SET` de `UPDATE` gerado? | Dommel |
| `Generated` | O banco pode gerar o valor em alguma operacao? | Dommel, diagnostics |
| `Key` | A propriedade identifica a linha? | Dommel key resolver |
| `Identity` | A key/coluna e gerada como identity no insert? | Dommel insert result/key handling |
| `Computed` | O valor e computado pelo banco e nao deve ser escrito? | Dommel insert/update filtering |
| `DefaultOnInsert` | O banco aplica default quando a coluna e omitida no insert? | Dommel insert filtering |

Regras de independencia:

- `Ignored` equivale a `Read=no`, `Insert=no`, `Update=no`.
- `ReadOnly` nao equivale a `Ignored`.
- `Key` nao implica `Identity`.
- `Identity` implica `Generated` e normalmente `Insert=no`.
- `Computed` implica `Generated`, `Insert=no`, `Update=no`, `Read=yes`.
- `DefaultOnInsert` implica `Insert=no`, mas nao determina sozinho `Update`.
- `Generated` sozinho e insuficiente como API final se nao disser em qual
  operacao a escrita deve ser omitida.

## Casos semanticos obrigatorios

### Normal property

```text
Read      = yes
Insert    = yes
Update    = yes
Generated = no
Key       = no
Identity  = no
Computed  = no
```

### Ignored property

```text
Read   = no
Insert = no
Update = no
```

`Ignore()` deve preservar esse significado por compatibilidade.

### Read-only database value

```text
Read   = yes
Insert = no
Update = no
```

Representa campos como `CreatedAt`, row metadata, contador projetado ou valor
calculado que o modelo deve receber, mas o comando gerado nao deve escrever.

### Database default on insert

Semantica recomendada:

```text
Read            = yes
Insert          = no
Update          = yes
DefaultOnInsert = yes
Generated       = yes
```

Justificativa: o default do banco e aplicado quando a coluna e omitida no
`INSERT`; depois disso, a aplicacao pode ou nao atualizar o valor. Como existem
dominios em que o default e somente valor inicial e outros em que o valor tambem
deve permanecer read-only, a API futura deve permitir compor `ExcludeFromInsert`
e `ExcludeFromUpdate`.

### Computed property

```text
Read     = yes
Insert   = no
Update   = no
Generated = yes
Computed = yes
```

Computed nao deve ser alias para `Ignore()`, porque a coluna pode ser lida.

### Identity key

```text
Read      = yes
Insert    = no
Update    = no
Key       = yes
Identity  = yes
Generated = yes
```

A key identifica a linha. O valor identity e gerado pelo banco no insert.
O update da key deve ser proibido por default.

### Non-identity key

```text
Read      = yes
Insert    = yes
Update    = no
Key       = yes
Identity  = no
Generated = no
```

Decisao inicial: key nao identity participa de `INSERT`, mas nao entra no `SET`
de `UPDATE`; ela entra no `WHERE`. Isso combina com o comportamento usual do
Dommel e evita alterar identidade logica da linha por acidente.

### Generated non-key value

```text
Read      = yes
Insert    = no
Update    = conforme subtipo
Generated = yes
Key       = no
```

`Generated` deve ser tratado como categoria guarda-chuva. A API publica deve
preferir metodos mais especificos quando possivel.

## API design exploration

### Opcao A - `ReadOnly()`

```csharp
Map(x => x.CreatedAt)
    .ToColumn("created_at")
    .ReadOnly();
```

Vantagens:

- discoverability alta para #94;
- comunica "ler, nao escrever";
- facil de mapear para `Insert=no` e `Update=no`.

Custos:

- pode ser ambigua com propriedade C# sem setter;
- nao expressa default somente no insert;
- nome pode sugerir restricao de imutabilidade do objeto, nao persistencia.

### Opcao B - exclusoes por operacao

```csharp
Map(x => x.CreatedAt)
    .ToColumn("created_at")
    .ExcludeFromInsert()
    .ExcludeFromUpdate();
```

Vantagens:

- composavel;
- representa default on insert;
- evita sobrecarregar `Generated`;
- claro para Dommel.

Custos:

- mais verboso;
- usuarios precisam conhecer insert/update semantics.

### Opcao C - metodos de dominio de persistencia

```csharp
Map(x => x.RowVersion).Computed();
Map(x => x.CreatedAt).DatabaseDefaultOnInsert();
Map(x => x.Id).IsKey().IsIdentity();
Map(x => x.Code).IsKey().Assigned();
```

Vantagens:

- expressa intencao;
- melhora diagnostics/analyzers;
- pode traduzir para flags operacionais.

Custos:

- aumenta superficie publica;
- parte dos nomes e fortemente associada a CRUD/Dommel;
- risco de transformar o core em mini ORM se mal delimitado.

### Opcao D - metadata no core, API especializada no Dommel

Core adiciona um contrato de metadata de persistencia em `IPropertyMap` ou
interface opcional, e Dommel oferece metodos fluent que preenchem essa metadata.

Vantagens:

- core continua sem CRUD;
- Dommel pode consumir a metadata sem duplicar parsing de maps;
- generated/analyzers podem entender metadata quando ela existir;
- compatibilidade pode ser aditiva via interface opcional.

Custos:

- exige desenho cuidadoso para nao quebrar implementacoes customizadas de
  `IPropertyMap`;
- consumidores do core podem perguntar por metadata que nao usam.

## Decisao de localizacao

Semantica pertence a uma abstracao de metadata no core, consumida opcionalmente
pelo Dommel.

Racional:

- a propriedade ja e descrita no core por coluna, case sensitivity, ignore e
  member path;
- a distincao `Read` versus `Insert`/`Update` e conceitual, nao exclusivamente
  Dommel;
- o core nao deve gerar SQL, mas pode descrever metadata;
- Dommel deve continuar responsavel por traduzir metadata para comandos CRUD.

## Forma recomendada para API futura

Nao implementar neste prompt. Direcao preferida para prompts seguintes:

1. Introduzir metadata interna/aditiva de persistencia com defaults compativeis:
   `Read=yes`, `Insert=yes`, `Update=yes`, `Key=no`, `Generated=no`.
2. Preservar `Ignore()` como `Read=no`, `Insert=no`, `Update=no`.
3. Oferecer API composavel por operacao:
   `ExcludeFromInsert()`, `ExcludeFromUpdate()`.
4. Oferecer atalhos intencionais:
   `ReadOnly()` como `ExcludeFromInsert().ExcludeFromUpdate()`;
   `Computed()` como `ReadOnly()` + `Generated/Computed`;
   `DatabaseDefaultOnInsert()` como `ExcludeFromInsert()` + generated default.
5. Manter ou adaptar Dommel APIs existentes:
   `IsKey()`, `IsIdentity()`, `SetGeneratedOption(...)`.

## Compatibilidade

Mudancas futuras devem ser aditivas:

- nao remover `Ignore()`;
- nao alterar assinatura de `IPropertyMap` sem estrategia binaria;
- preferir interface opcional para metadata nova;
- manter `DommelPropertyMap.SetGeneratedOption(...)` funcionando;
- preservar `IsKey()` historico, mas diagnosticar default identity implicito
  quando houver risco;
- nao alterar `Dapper.Query<T>()` ou `QueryMapped*` para considerar insert/update.

## Diagnostics e analyzers

Novos diagnostics devem ser conservadores:

- alertar quando `Ignore()` parece usado para default/read-only em Dommel docs
  ou exemplos nao e viavel estaticamente sem contexto de comando;
- detectar combinacoes contraditorias, por exemplo `Ignore().ReadOnly()` se a API
  permitir cadeia desse tipo;
- detectar `Identity` sem `Key` se essa combinacao for invalida;
- detectar `Key` gerada e `ExcludeFromInsert(false)` se houver API explicita para
  reabilitar insert;
- explicar no `Explain<T>()` as semanticas de leitura e persistencia separadas.

## Interacao com generated materializers

Generated materializers devem consumir apenas `Read` e `Ignored`.

Metadata de `Insert`, `Update`, `Key`, `Identity`, `Computed` e
`DefaultOnInsert` nao deve alterar o delegate `IDataRecord -> entity`, exceto
quando tambem afetar `Read`.

## Plano da Etapa 8

1. Modelo de metadata:
   criar contrato aditivo e defaults compativeis.
2. APIs publicas:
   adicionar fluent methods pequenas e documentadas.
3. Dommel integration:
   traduzir metadata para `ColumnPropertyInfo` e key/property resolvers.
4. Historical regression suite:
   cobrir #94, #122, #123, #130, #114, #126, #133.
5. Diagnostics/analyzers:
   expor e validar combinacoes contraditorias.
6. Documentacao:
   atualizar README e XML docs com exemplos de leitura vs escrita.
7. Hardening:
   validar cache, profiles, generated materializers, Dommel SQL real e
   compatibilidade binaria.
