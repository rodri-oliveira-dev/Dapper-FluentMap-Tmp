# Flat Generated Materializers

Status: SPECIFICATION + IMPLEMENTATION
Prompt: 7.4
Data: 2026-07-28

## Objetivo

Implementar a primeira cobertura produtiva de materializadores gerados para entidades flat simples, preservando o fallback runtime como autoridade funcional.

O generator continua emitindo `AddGeneratedMappings()`. A partir deste prompt, quando um map e estaticamente geravel, a mesma chamada tambem registra um `GeneratedRowMaterializer<TEntity>` pelo contrato publico criado no prompt 7.3.

## Casos Suportados

O caminho gerado cobre somente maps declarados na compilacao atual que ja eram elegiveis para registro gerado:

- map concreto, fechado, `public` ou `internal`;
- map com construtor publico sem parametros;
- map implementando exatamente um `IEntityMap<TEntity>`, com `TEntity` class;
- no maximo um `IProfileMap<TProfile>`, quando for profile;
- chamadas diretas no construtor `Map(entity => entity.Property).ToColumn("literal")`;
- `Ignore()` em propriedade root;
- propriedades root, flat, sem caminhos aninhados;
- propriedades escalares simples: primitivos numericos, `bool`, `char`, `string`, `decimal`, `DateTime`, `Guid`, enums e `Nullable<T>` desses tipos;
- entidade mutavel com construtor publico sem parametros e setters publicos para todas as propriedades materializadas;
- entidade imutavel simples com um unico construtor publico que corresponda a todas as propriedades materializadas por nome de parametro e tipo apos unwrap de `Nullable<T>`;
- profiles simples com os mesmos limites acima.

## Casos Nao Suportados

Estes casos continuam pelo fallback runtime:

- nested objects, nested value objects e member paths com mais de uma propriedade;
- collections, graph aggregation e factory methods;
- `IncludeBase<TBase>()`;
- conventions e naming policies como fonte gerada de colunas;
- nomes de coluna calculados ou nao literais;
- chamadas de mapping indiretas, helpers arbitrarios ou chains desconhecidas;
- propriedades sem setter que nao sejam vinculadas por um construtor simples;
- construtores ambiguos ou parcialmente vinculados;
- TypeHandlers do Dapper no caminho gerado;
- shapes de query que nao correspondam exatamente ao shape ordenado registrado;
- colunas extras, ausentes ou em ordem diferente do descriptor gerado.

Ausencia de suporte gerado nao e erro funcional. O runtime existente segue materializando quando a configuracao for valida para o FluentMap.

## Generated Code Shape

Para cada map suportado, o generator emite:

```csharp
configuration
    .AddMap<CustomerMap>()
    .AddGeneratedMaterializer<Customer>(
        new[]
        {
            GeneratedMaterializerColumn.Map("customer_id", "Id"),
            GeneratedMaterializerColumn.Map("full_name", "FullName")
        },
        DapperFluentMapGeneratedMaterializers.Read0);
```

Para profiles:

```csharp
configuration
    .AddProfile<LegacyCustomerMap>()
    .AddGeneratedMaterializer<Customer, LegacyProfile>(
        columns,
        DapperFluentMapGeneratedMaterializers.Read1);
```

O materializador emitido e uma classe estatica interna no assembly consumidor. Ele recebe `IDataRecord`, valida `record != null`, le valores por ordinal fixo e cria a entidade por:

- construtor publico sem parametros + atribuicoes diretas; ou
- construtor publico simples com argumentos locais lidos antes da chamada.

## Ordinal Handling

O descriptor usa a ordem das chamadas `Map(...)` reconhecidas no construtor do map. O ordinal do codigo gerado e a posicao do item nesse descriptor.

O runtime so usa o delegate gerado quando o shape ordenado do `IDataReader` bate com o descriptor registrado. Se a query retornar as mesmas colunas em outra ordem, colunas extras ou colunas ausentes, nao ha match e o fallback runtime e usado.

## Null Conversion

O helper gerado segue a semantica escalar do runtime para o subconjunto suportado:

- `DBNull` retorna `default(T)`;
- para reference types e `Nullable<T>`, isso resulta em `null`;
- para value types nao anulaveis, isso resulta em `default`;
- valores ja tipados sao retornados diretamente;
- enums aceitam string ou valor numerico;
- `Guid` aceita string;
- demais escalares usam `Convert.ChangeType(..., CultureInfo.InvariantCulture)`.

TypeHandlers do Dapper permanecem fora deste prompt e usam fallback.

## Constructor Selection

O generator prefere entidade mutavel quando existe construtor publico sem parametros e todos os membros materializados possuem setter publico.

Quando isso nao e possivel, ele aceita somente um construtor publico que:

- tenha exatamente a mesma quantidade de parametros que propriedades materializadas;
- vincule cada parametro a uma propriedade mapeada por nome case-insensitive;
- tenha tipo igual ao tipo da propriedade apos unwrap de `Nullable<T>`;
- use cada propriedade materializada exatamente uma vez.

Se nenhum construtor ou mais de um construtor for seguro, o generator nao emite materializer e o runtime fallback decide.

## Error Handling

Situacoes estaticamente invalidas ja cobertas pelo generator/analyzer continuam como diagnostics de erro existentes, por exemplo maps genericos invalidos e duplicidade de maps gerados.

Situacoes validas para runtime mas nao suportadas pelo caminho gerado produzem diagnostic informativo `DFM011` e continuam registradas via `AddMap<TMap>()` ou `AddProfile<TMap>()`.

Falhas de dominio ao executar construtor gerado sao encapsuladas em `FluentMapConfigurationException` com a excecao original como inner exception. Falhas de conversao escalar seguem o comportamento natural da conversao, como no runtime atual antes da chamada de construtor.

## Fallback

Fallback e integral:

- sem descriptor para entity/profile/shape: `NestedMaterializationPlan`;
- descriptor com shape diferente: `NestedMaterializationPlan`;
- descriptor incompativel com o mapping efetivo atual: `NestedMaterializationPlan`;
- map com feature nao suportada pelo generator: `NestedMaterializationPlan`;
- consumidor sem pacote generator: comportamento runtime existente.

O descriptor continua sendo validado contra o mapping efetivo no runtime antes de usar o delegate gerado.

## Limitacoes Restantes

- O generator ainda nao interpreta um metadata model compartilhado; ele reconhece um subconjunto estatico da DSL diretamente por Roslyn.
- A cobertura gerada e por shape canonico de map, nao por SQL real.
- `QueryMapped*` mantem annotations de trimming/dynamic-code porque qualquer chamada ainda pode cair no fallback.
- Nao ha diagnostico publico em runtime indicando qual caminho foi escolhido.
- Benchmarks locais sao evidencia de uma maquina/rodada, nao promessa publica de performance.
