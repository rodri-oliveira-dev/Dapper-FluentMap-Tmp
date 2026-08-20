# Etapa 10 - Generated Property Conversion

## Regra central

Generated materialization e runtime materialization devem preservar:

```text
runtime result == generated result
```

para toda conversao de leitura suportada pelos dois caminhos. O generated
materializer nao define uma semantica paralela; ele emite uma forma estatica da
mesma ordem efetiva do runtime:

```text
null/DBNull handling
    -> property read converter
    -> default generated conversion
```

Quando nao ha property converter, o comportamento generated anterior permanece:
cast direto, enum, `Guid` de string e `Convert.ChangeType(...,
InvariantCulture)`. `TypeHandler<T>` no generated path segue fora desta etapa e
continua dependendo do runtime fallback nos cenarios em que for necessario.

## Converter discovery

O source generator reconhece read converters somente quando a fluent chain usa
um converter por tipo estaticamente referenciavel:

```csharp
Map(x => x.Status)
    .ToColumn("status")
    .ConvertFromDatabaseUsing<StatusConverter, string>();
```

Tambem e reconhecido:

```csharp
Map(x => x.Status)
    .ConvertUsing<StatusTextConverter, string>();
```

porque `ConvertUsing` inclui a direcao de leitura.

O generator valida o contrato `IReadPropertyConverter<TDatabase, TProperty>` no
tipo do converter, com `TDatabase` compativel com o tipo declarado na chamada e
`TProperty` atribuivel ao tipo terminal do member path. `Nullable<T>` e `T`
continuam equivalentes para matching de contrato, preservando a regra da etapa
10.2/10.3.

## Converter construction

Converters gerados sao materializados em campos estaticos privados da classe
gerada, um por binding de coluna/converter:

```csharp
private static readonly StatusConverter Read0Converter1 =
    new StatusConverter();
```

Isso evita:

- `Activator.CreateInstance` no hot path;
- reflection por linha;
- `dynamic`;
- criacao por linha.

O contrato de lifetime continua sendo converter stateless/thread-safe. A
instancia gerada e separada da instancia criada pelo map runtime, mas preserva o
mesmo modelo operacional para converters por tipo: reuso e nenhuma alocacao por
linha. Converters por instancia/delegate fornecidos pelo usuario nao sao
duplicados pelo generator e usam runtime fallback.

## Generated invocation

Quando suportado, o hot path gerado chama um helper generico fortemente tipado:

```csharp
entity.Status =
    ReadConverted<string, Status, Status>(
        record,
        0,
        Read0Converter0,
        ...contexto diagnostico...);
```

O helper:

- checa `DBNull` antes de chamar o converter;
- converte o valor bruto para `TDatabase` com a mesma conversao primitiva do
  helper `Read<T>`;
- chama `IReadPropertyConverter<TDatabase, TProperty>.ConvertFromDatabase`;
- retorna o tipo alvo real da propriedade/parametro (`TTarget`);
- encapsula falhas em `FluentMapConfigurationException` com inner exception.

## Nullable handling

`DBNull` e `null` nao sao enviados ao converter.

- propriedades/reference targets nullable recebem `null`;
- value types nao nullable recebem `default(TTarget)`;
- conversores que retornam `T` podem alimentar propriedades `Nullable<T>`;
- se um converter retorna `null` para target nao nullable, o generated path
  falha com `FluentMapConfigurationException`, preservando inner exception.

Esta regra corrige a diferenca sutil entre `TProperty` do converter e `TTarget`
do destino gerado, por exemplo `IReadPropertyConverter<string, int>` aplicado a
`int?`.

## Profile

Descriptors generated agora carregam metadata de read converter por coluna:

```text
column name
member path
read converter type
database/provider CLR type
converter property CLR type
```

O registry so seleciona o materializer gerado quando essa metadata coincide com
o map efetivo do profile selecionado. Isso separa corretamente:

```text
default mapping
profile A
profile B
```

e evita colisao quando dois profiles usam a mesma entidade/coluna com
converters diferentes.

## Nested

Converters continuam associados a folhas terminais do member path:

```text
BillingAddress.ZipCode
ShippingAddress.ZipCode
```

O generated path usa a mesma regra de subarvore do runtime: se todos os ordinais
de uma subarvore sao `DBNull`, a subarvore nao e criada e nenhum converter da
subarvore e chamado.

## Immutable constructor

Folhas com read converter sao convertidas antes de montar argumentos de
construtor. A selecao do construtor continua baseada no tipo terminal do member
path/propriedade, nao no `TDatabase` do converter.

Generated materializers tambem suportam property converter em Value Objects
escalares quando o converter produz o objeto inteiro:

```csharp
Map(x => x.Cpf)
    .ToColumn("cpf")
    .ConvertFromDatabaseUsing<CpfConverter, string>();
```

## Diagnostics

O generator emite `DFM012` como erro quando consegue provar que um converter por
tipo possui contrato read invalido para o member path, por exemplo:

- nao implementa `IReadPropertyConverter<TDatabase, TProperty>` para o
  `TDatabase` declarado;
- retorna `TProperty` nao atribuivel ao tipo terminal;
- possui mais de um contrato read compativel e ambiguo.

O diagnostic `DFM011` permanece informativo para fallback de materializer
gerado quando a chain nao e suportada estaticamente.

## Fallback

O generator cai para runtime fallback, preservando comportamento suportado, em
casos como:

- read converter por instancia;
- read converter por delegate;
- converter por tipo nao acessivel ao codigo gerado;
- chain fluent nao analisavel estaticamente;
- `IncludeBase`, conventions dinamicas e demais limites ja existentes do
  generated materializer.

Write-only converters sao neutros para generated read materialization.

## Trimming

O caminho gerado para converter por tipo e mais amigavel a trimming que runtime
fallback porque referencia o converter de forma estatica no codigo gerado e nao
usa ativacao dinamica no hot path. Ainda assim, `QueryMapped*` permanece
anotado como trimming/dynamic-code sensitive porque pode cair para runtime
fallback.

Converters por instancia/delegate continuam suportados pelo runtime, mas nao
sao transformados em codigo gerado nesta etapa.

## Native AOT

Generated property conversion evita `Expression.Compile`, reflection por linha
e ativacao dinamica no materializer gerado. O impacto AOT e portanto
incrementalmente positivo nos cenarios em que:

- o map e registrado por `AddGeneratedMappings()`;
- o converter e por tipo, acessivel e parameterless;
- o shape da query casa com o descriptor gerado;
- nao ha outro motivo para fallback runtime.

A biblioteca continua nao devendo ser descrita como totalmente Native AOT
compatible. O smoke de trimming/AOT deve ser interpretado junto com os avisos
esperados de `QueryMapped*` e Dapper documentados nas etapas anteriores.
