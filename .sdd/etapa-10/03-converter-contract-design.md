# Etapa 10 - Converter Contract Design

## Escopo do Prompt 10.2

Este incremento adiciona contratos, fluent API e metadata por propriedade para
conversores. Ele nao executa conversao em `QueryMapped*`, Dapper puro ou Dommel.
A decisao evita espalhar logica de execucao antes de haver equivalencia
runtime/generated e hook de escrita validado.

## Contratos publicos

Contratos adicionados em `Dapper.FluentMap.Mapping`:

```csharp
public interface IReadPropertyConverter<in TDatabase, out TProperty>
{
    TProperty ConvertFromDatabase(TDatabase value);
}

public interface IWritePropertyConverter<in TProperty, out TDatabase>
{
    TDatabase ConvertToDatabase(TProperty value);
}

public interface IPropertyConverter<TDatabase, TProperty> :
    IReadPropertyConverter<TDatabase, TProperty>,
    IWritePropertyConverter<TProperty, TDatabase>
{
}
```

Delegates direcionais tambem existem para configuracao leve:

```csharp
public delegate TProperty ReadPropertyConverter<in TDatabase, out TProperty>(
    TDatabase value);

public delegate TDatabase WritePropertyConverter<in TProperty, out TDatabase>(
    TProperty value);
```

`IPropertyConverter<TDatabase, TProperty>` e invariante porque os dois tipos
aparecem em posicoes de entrada e saida quando as duas direcoes sao combinadas.

## Fluent API escolhida

APIs por tipo de converter:

```csharp
Map(x => x.Status)
    .ConvertFromDatabaseUsing<StatusReadConverter, string>();

Map(x => x.Status)
    .ConvertToDatabaseUsing<StatusWriteConverter, string>();

Map(x => x.Status)
    .ConvertUsing<StatusTextConverter, string>();
```

APIs por instancia:

```csharp
Map(x => x.Status)
    .ConvertFromDatabaseUsing(new StatusReadConverter());

Map(x => x.Status)
    .ConvertToDatabaseUsing(new StatusWriteConverter());

Map(x => x.Status)
    .ConvertUsing(new StatusTextConverter());
```

APIs por delegate:

```csharp
Map(x => x.Status)
    .ConvertFromDatabaseUsing<string, Status>(value => Status.Parse(value))
    .ConvertToDatabaseUsing<Status, string>(value => value.Code);
```

A API existente `Map(Expression<Func<TEntity, object>>)` nao carrega
`TProperty`, portanto os overloads por tipo detectam incompatibilidade em
configuration time. Os overloads por instancia/delegate preservam maior
type-safety em build time quando o compilador infere os tipos do contrato.

## Direcoes

Read direction:

```text
Database/provider CLR value -> Property CLR value
```

Write direction:

```text
Property CLR value -> Database/provider CLR value
```

As direcoes sao independentes. Um property map pode configurar apenas leitura,
apenas escrita ou ambas. `ConvertUsing` exige que o converter implemente as duas
direcoes compativeis.

## Null handling

O contrato documentado para execucao futura e:

- `null` e `DBNull.Value` nao serao enviados ao converter por default;
- nullable/reference recebem `null`;
- value type nao nullable recebe `default(T)`;
- `Nullable<T>` e `T` sao aceitos como compativeis na configuracao.

O Prompt 10.2 apenas guarda metadata e valida tipos; nao altera a execucao de
null em materializers.

## Lifetime

Estrategia implementada:

- `Convert...Using<TConverter, TDatabase>()`: cria uma instancia por property
  map no momento de construcao do map;
- overload por instancia: reutiliza a instancia fornecida pelo usuario;
- overload por delegate: reutiliza o delegate fornecido pelo usuario;
- sem DI, factory ou escopo por query nesta etapa.

Conversores sao tratados como stateless/thread-safe por contrato. Se o usuario
fornecer uma instancia stateful, a thread safety e responsabilidade dele.

## Metadata

Metadata publica aditiva:

- `PropertyConversionMetadata`;
- `PropertyConverterMetadata`;
- `PropertyConversionDirection`;
- `IPropertyMapWithConversionMetadata`.

`PropertyConversionMetadata` responde:

- `HasReadConverter`;
- `HasWriteConverter`;
- `ReadConverter`;
- `WriteConverter`.

Cada `PropertyConverterMetadata` responde:

- `Direction`;
- `ConverterType`;
- `DatabaseType`;
- `PropertyType`.

A instancia real do converter fica armazenada internamente no descriptor. Isso
evita expor estado mutavel como contrato publico e preserva um ponto de
execucao futuro.

`MemberMappingExplanation.Conversion` expoe um snapshot read-only da metadata
efetiva. O profile scope nao e duplicado dentro da property metadata; ele vem do
`MappingExplanation.ProfileType` e da origem do map efetivo.

## Profile behavior

Profiles continuam maps separados. Conversores configurados em um
`IProfileMap<TProfile>` valem somente para aquele profile.

```text
QueryMapped<TEntity, TProfile>()
    usa metadata do profile registrado

QueryMapped<TEntity>()
    usa metadata do map default
```

Nao ha vazamento automatico de converter do map default para profile.

## Inheritance

`IncludeBase<TBase>()` preserva conversion metadata dos property maps herdados.
Quando o map derivado declara explicitamente o mesmo member path, a precedencia
existente continua valendo:

```text
derived explicit mapping
    -> inherited base explicit mapping
```

Isso evita merge silencioso entre converters contraditorios. O property map
efetivo e unico.

## Duplicate configuration

Configuracoes duplicadas sao invalidas:

- segundo read converter no mesmo property map: erro;
- segundo write converter no mesmo property map: erro;
- `ConvertUsing` falha se read ou write ja existir;
- profile duplicate continua sendo rejeitado por entity/profile key.

## Precedence futura de execucao

Sem alterar execucao neste prompt, a metadata foi desenhada para a seguinte
precedencia futura no caminho `QueryMapped*`:

```text
property read converter
    -> Dapper TypeHandler<TProperty>
    -> FluentMap default conversion
```

Para escrita futura:

```text
property write converter
    -> Dapper TypeHandler<TProperty>
    -> Dapper/provider parameter default
```

## Generated materializers

O source generator ja deixa de emitir generated materializer quando encontra
metodo fluent nao suportado na chain. Como `Convert...` e novo, maps com
converter caem para runtime fallback no caminho gerado.

O registry tambem rejeita descriptors generated manuais quando o effective
mapping para a coluna possui read converter. Isso evita escolher um materializer
que nao declarou nem aplicou a conversao de leitura.

Write-only converters nao bloqueiam materializer de leitura.

## Invalid configuration

Erros cobertos em configuration time:

- converter sem contrato direcional requerido;
- database/source type declarado incompatibil com o converter;
- property/destination type retornado incompatibil com a propriedade;
- read converter duplicado;
- write converter duplicado;
- profile collision;
- override derivado preservando precedencia explicita.

As excecoes usam `FluentMapConfigurationException` para manter o padrao publico
existente do projeto.

## Compatibilidade

Sem converter configurado:

- `IPropertyMap` permanece inalterada;
- maps existentes continuam compilando;
- `Query<T>()`, `QueryMapped*`, generated materializers e Dommel nao mudam
  comportamento de valor;
- diagnostics ganham metadata aditiva em `MemberMappingExplanation.Conversion`.

