# Etapa 10 - Runtime Read Conversion

## Ponto de execucao

Read converters por propriedade executam somente no materializer comum de
`QueryMapped*`, `ReadMapped*`, `QueryMultipleMapped`, streaming unbuffered
sincrono e streaming unbuffered assincrono.

O ponto exato e a folha terminal de `NestedMaterializationPlan`:

```text
IDataRecord.GetValue(columnOrdinal)
    -> NestedLeaf.GetValue
    -> null/DBNull handling
    -> property read converter, se configurado para o member path efetivo
    -> Dapper TypeHandler<TProperty>, se nao houver property converter
    -> conversao padrao do FluentMap
    -> setter ou parametro de construtor
```

`MappedRowMaterializer` continua sendo o unico dispatch das APIs mapeadas:
generated materializer quando valido, senao runtime fallback. Descriptors
generated ainda sao rejeitados quando o mapping efetivo possui read converter,
evitando materializer gerado sem conversao.

## Precedencia

Para runtime `QueryMapped*`:

```text
null/DBNull handling
    -> property read converter
    -> Dapper TypeHandler<TProperty>
    -> default FluentMap conversion
```

O property converter recebe o valor CLR vindo de `IDataRecord.GetValue`,
normalizado apenas para o `TDatabase` declarado quando a conversao CLR padrao
e necessaria. `TypeHandler<TProperty>` nao e aplicado antes nem depois do
property converter da mesma folha.

Sem read converter configurado, o comportamento anterior permanece: o runtime
consulta `TypeHandler<TProperty>` e, se nao houver handler, aplica cast direto,
enum, `Guid` de string e `Convert.ChangeType(..., InvariantCulture)`.

## Null semantics

`null` e `DBNull.Value` nao sao enviados ao converter por default.

- `Nullable<T>` recebe `null`.
- reference types recebem `null`.
- value types nao nullable preservam o comportamento historico e recebem
  `default(T)`.
- subarvores aninhadas continuam usando `HasNonNullValue`: se todos os valores
  da subarvore sao `DBNull`, nenhuma folha nem converter e executado e a
  subarvore fica `null` quando atribuivel.

Se um converter retornar `null` para target value type nao nullable, a
materializacao falha com `FluentMapConfigurationException` e inner exception
preservando a causa local.

## Primitive conversion e enums

Sem property converter, a conversao padrao existente continua:

- valor ja atribuivel: retorno direto;
- enum: parse de string ou `Enum.ToObject`;
- `Guid`: parse de string;
- demais casos: `Convert.ChangeType` com cultura invariante.

Com property converter, essa conversao padrao so pode ser usada para ajustar o
valor bruto ao `TDatabase` declarado do converter. O resultado do converter e
tratado como o valor da propriedade e nao passa por `TypeHandler<TProperty>`.

## Value Objects

Value Objects escalares podem usar property converter quando a representacao e
local a uma propriedade/profile:

```csharp
Map(x => x.Cpf)
    .ToColumn("cpf")
    .ConvertFromDatabaseUsing<CpfConverter, string>();
```

Sem property converter, `TypeHandler<TValueObject>` segue sendo o mecanismo
global recomendado para Value Objects escalares.

Value Objects por componentes continuam convertendo folhas terminais antes de
invocar construtores publicos compativeis:

```csharp
Map(x => x.Cpf.Number)
    .ToColumn("cpf")
    .ConvertFromDatabaseUsing<DigitsOnlyConverter, string>();
```

## Constructor parameters

Folhas ligadas a parametros de construtor sao convertidas antes da montagem do
array de argumentos. A selecao do construtor continua baseada no tipo do member
path/propriedade, nao no `TDatabase` do converter.

Falhas de converter em parametros de construtor sao encapsuladas antes da
invocacao do construtor. Falhas de dominio do proprio construtor continuam
encapsuladas pelo bloco de materializacao de construtor existente.

## Nested properties

Converters sao associados ao member path efetivo do property map. Um converter
configurado em:

```csharp
Map(x => x.BillingAddress.ZipCode)
```

nao se aplica a:

```csharp
Map(x => x.ShippingAddress.ZipCode)
```

mesmo quando o tipo terminal e o nome da propriedade sao iguais.

## Profiles

Profiles usam o property map efetivo do profile selecionado:

```text
QueryMapped<TEntity>()
    -> default entity map

QueryMapped<TEntity, LegacyProfile>()
    -> profile map LegacyProfile
```

Conversores do map default nao vazam para profiles. Reuso deve ser explicito
por `IncludeBase<T>()` ou por configuracao direta no profile.

## Exception wrapping

Falhas de read converter geram `FluentMapConfigurationException` com inner
exception preservada. A mensagem inclui, quando disponivel:

- entity type;
- profile type;
- member path;
- column;
- converter type;
- source CLR type;
- converter database type;
- converter property type;
- target CLR type.

O texto exato e diagnostico, nao contrato publico. O tipo da excecao e a inner
exception preservada sao parte do comportamento esperado.

## Fallback

APIs Dapper puras (`Query<T>()`, `QuerySingle<T>()` etc.) continuam fora do
escopo property-scoped do FluentMap. Elas usam Dapper type map para nomes e os
mecanismos de conversao do Dapper/provider.

Generated materializers ainda nao executam property converters neste incremento.
Quando um generated descriptor nao declara essa metadata, o registry nao o
seleciona e o runtime materializer assume.
