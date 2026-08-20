# FluentMap

[Português (Brasil)](#português-brasil)

FluentMap is an advanced mapping layer for Dapper. It lets you describe how .NET object properties map to database columns with fluent, strongly typed code, while keeping persistence attributes out of your POCOs.

FluentMap is not an ORM. It does not track entities, build arbitrary SQL, manage connections, run migrations, provide LINQ, or replace Dapper. Use it when Dapper's default name-based mapping is not enough and the mapping rules should live outside the model.

This repository originated from the archived `Dapper.FluentMap` project and is being evolved in this fork.

## Positioning

Use FluentMap for:

- explicit property-to-column maps;
- conventions and naming policies;
- ignored properties;
- immutable constructor mapping;
- opt-in nested object and value object materialization;
- mapping profiles for alternate SQL shapes;
- generated map registration/materialization where supported;
- persistence metadata consumed by integrations such as Dommel;
- isolated configuration and dependency injection for FluentMap-controlled materialization.

Do not use FluentMap as an ORM, CRUD framework, query builder, unit of work, or database abstraction.

## Installation

Install the package that matches the feature set you need:

| Package | Purpose |
| --- | --- |
| `Dapper.FluentMap` | Core mapping API and Dapper integration. |
| `Dapper.FluentMap.Dommel` | Optional Dommel integration for table, key and generated-column mapping. |
| `Dapper.FluentMap.DependencyInjection` | Optional `Microsoft.Extensions.DependencyInjection` integration. |
| `Dapper.FluentMap.Analyzers` | Roslyn analyzers for statically provable mapping mistakes. |
| `Dapper.FluentMap.Generators` | Source generator for build-time map registration and generated materializers. |

```bash
dotnet add package Dapper.FluentMap
```

The public packages target `netstandard2.0`. See [COMPATIBILITY.md](COMPATIBILITY.md) before adopting a release candidate.

## Quick Start

```csharp
using Dapper;
using Dapper.FluentMap;
using Dapper.FluentMap.Mapping;

public sealed class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(customer => customer.Id).ToColumn("customer_id");
    }
}

FluentMapper.Initialize(config =>
{
    config.AddMap<CustomerMap>();
});

var customer = connection.QuerySingle<Customer>(
    "SELECT 7 AS customer_id, 'Ada' AS Name;");
```

Call `FluentMapper.Initialize(...)` during application startup and treat the effective global configuration as read-only once queries begin.

## Mapping

Create maps by deriving from `EntityMap<TEntity>`:

```csharp
public sealed class ProductMap : EntityMap<Product>
{
    public ProductMap()
    {
        Map(product => product.Id).ToColumn("product_id");
        Map(product => product.Name).ToColumn("product_name", caseSensitive: false);
        Map(product => product.TransientValue).Ignore();
    }
}
```

Explicit mappings take precedence over conventions. Unmapped root members fall back to Dapper's normal behavior.

Conventions and naming policies cover repeated patterns:

```csharp
using Dapper.FluentMap.Conventions;
using Dapper.FluentMap.Naming;

public sealed class PrefixConvention : Convention
{
    public PrefixConvention()
    {
        Properties().Configure(property => property.HasPrefix("col"));
    }
}

FluentMapper.Initialize(config =>
{
    config.AddConvention<PrefixConvention>().ForEntity<Customer>();
    config.UseNamingPolicy(NamingPolicy.SnakeCase, caseSensitive: false)
        .ForEntity<Order>();
});
```

Available naming policies include `Identity`, `SnakeCase`, `Prefix(...)`, `Suffix(...)`, `Custom(...)`, `Then(...)`, `WithPrefix(...)` and `WithSuffix(...)`.

## Immutable Types

FluentMap participates in Dapper constructor mapping for root-level explicit mappings:

```csharp
public sealed class Customer
{
    public Customer(int id, string fullName)
    {
        Id = id;
        FullName = fullName;
    }

    public int Id { get; }
    public string FullName { get; }
}

public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(customer => customer.Id).ToColumn("customer_id");
        Map(customer => customer.FullName).ToColumn("full_name");
    }
}
```

Use `QueryMapped*` when FluentMap must construct nested immutable objects or value objects.

## Nested Objects

Nested member paths use the same `Map(...)` API:

```csharp
public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(customer => customer.Id).ToColumn("customer_id");
        Map(customer => customer.Address.City).ToColumn("city");
    }
}

var customer = connection.QueryMappedSingle<Customer>(
    "SELECT 7 AS customer_id, 'Sao Paulo' AS city;");
```

Nested object materialization is opt-in through `QueryMapped*`, `ReadMapped*`, `QueryMultipleMapped` and streaming helpers. Normal `Dapper.Query<T>()` remains root-level Dapper materialization.

## Value Objects

For scalar value objects mapped as one database value, prefer a Dapper `TypeHandler<T>`:

```csharp
Map(customer => customer.Cpf).ToColumn("cpf");
```

For value objects mapped through components, FluentMap-controlled materialization can call matching public constructors:

```csharp
public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(customer => customer.Id).ToColumn("customer_id");
        Map(customer => customer.Cpf.Number).ToColumn("cpf");
    }
}

var customer = connection.QueryMappedSingle<Customer>(
    "SELECT 1 AS customer_id, '12345678909' AS cpf;");
```

Factory methods are not used by the current materializer.

## Profiles

Profiles are opt-in mappings for the same entity under different SQL shapes:

```csharp
using Dapper.FluentMap.Mapping;

public sealed class LegacyProfile : IMappingProfile
{
}

public sealed class LegacyCustomerMap :
    EntityMap<Customer>,
    IProfileMap<LegacyProfile>
{
    public LegacyCustomerMap()
    {
        Map(customer => customer.Id).ToColumn("id");
        Map(customer => customer.Name).ToColumn("legal_name");
    }
}

FluentMapper.Initialize(config =>
{
    config.AddMap<CustomerMap>();
    config.AddProfile<LegacyCustomerMap>();
});

var legacy = connection.QueryMappedSingle<Customer, LegacyProfile>(
    "SELECT 7 AS id, 'Legacy Ltd.' AS legal_name;");
```

Profiles are selected per FluentMap-controlled query. They do not replace the global Dapper type map for the entity.

## Generated Materialization

Install `Dapper.FluentMap.Generators` when you want generated registration for maps in the current compilation:

```bash
dotnet add package Dapper.FluentMap.Generators
```

Then call the generated extension:

```csharp
FluentMapper.Initialize(config =>
{
    config.AddGeneratedMappings();
});
```

The generator emits `AddMap<TMap>()` and `AddProfile<TMap>()` calls for eligible maps. For supported explicit mappings it can also register generated row materializers for the ordered column shape, including flat properties, nested paths, constructor-built value objects and statically supported read converters.

Generated materialization is an optimization. Unsupported maps, dynamic shapes, shape mismatches, instance/delegate converters and some advanced patterns use the runtime fallback.

## Persistence Semantics

Persistence metadata describes write participation without changing read materialization:

```csharp
Map(product => product.CreatedAt)
    .ToColumn("created_at")
    .DatabaseDefaultOnInsert();

Map(product => product.UpdatedAt)
    .ToColumn("updated_at")
    .ReadOnly();

Map(product => product.Total)
    .ToColumn("total")
    .Computed();
```

`Ignore()` keeps its historical meaning: the property is not materialized by FluentMap and is not part of generated persistence metadata. For database values that should still be selected but not written, use `ReadOnly()`, `Computed()`, `DatabaseDefaultOnInsert()`, `ExcludeFromInsert()` or `ExcludeFromUpdate()`.

The core package stores metadata. Dommel is the current package that consumes it for generated `INSERT` and `UPDATE` behavior.

## QueryMultiple / Streaming

Use FluentMap query helpers when materialization must honor nested mappings, value objects, profiles, converters or generated materializers:

```csharp
var customers = connection.QueryMapped<Customer>(sql);
var customer = connection.QueryMappedSingle<Customer>(sql);
var legacy = connection.QueryMappedSingle<Customer, LegacyProfile>(legacySql);
```

For multiple result sets:

```csharp
using var multi = connection.QueryMultipleMapped(sql);

var customers = multi.ReadMapped<Customer>();
var orders = multi.ReadMapped<Order>();
```

`ReadMapped*` consumes result sets sequentially and buffers the current result set.

For incremental processing:

```csharp
foreach (var customer in connection.QueryMappedUnbuffered<Customer>(sql))
{
    Process(customer);
}
```

Async streaming is available on `DbConnection`:

```csharp
await foreach (var customer in connection.QueryMappedUnbufferedAsync<Customer>(
    sql,
    cancellationToken))
{
    await ProcessAsync(customer, cancellationToken);
}
```

Streaming keeps the underlying reader open until enumeration completes or the enumerator is disposed.

## Property Converters

Property converters are configured per mapped property and run only during FluentMap-controlled materialization:

```csharp
public sealed class ProductMap : EntityMap<Product>
{
    public ProductMap()
    {
        Map(product => product.Status)
            .ToColumn("status_code")
            .ConvertFromDatabaseUsing<ProductStatusConverter, string>();
    }
}

public sealed class ProductStatusConverter :
    IReadPropertyConverter<string, ProductStatus>
{
    public ProductStatus ConvertFromDatabase(string value)
    {
        return value == "A" ? ProductStatus.Active : ProductStatus.Inactive;
    }
}
```

Read conversion precedence in FluentMap-controlled materialization is:

```text
null/DBNull handling
    -> property read converter
    -> Dapper TypeHandler<TProperty>
    -> FluentMap default conversion
```

Write converter metadata can be configured, but it is not currently executed by Dapper or Dommel writes.

## Isolated Configuration / DI

The historical static API remains supported:

```csharp
FluentMapper.Initialize(config =>
{
    config.AddMap<CustomerMap>();
});
```

For multiple FluentMap-controlled configurations in the same process, build immutable configurations and use their runtimes:

```csharp
using Dapper.FluentMap.Configuration;

var runtime = new FluentMapConfigurationBuilder()
    .AddMap<CustomerMap>()
    .Build()
    .CreateRuntime();

var customer = runtime.QueryMappedSingle<Customer>(
    connection,
    "SELECT 7 AS customer_id, 'Ada' AS Name;");
```

Install `Dapper.FluentMap.DependencyInjection` for DI registration:

```csharp
using Microsoft.Extensions.DependencyInjection;

services.AddFluentMap(builder =>
{
    builder.AddMap<CustomerMap>();
    builder.Configure(config => config.AddGeneratedMappings());
});
```

The DI package registers `ImmutableFluentMapConfiguration` and `FluentMapRuntime` as singletons. It does not register database connections, repositories, Dommel bridges or global Dapper type maps.

## AOT / Trimming

FluentMap has partial trimming/AOT readiness, not full Native AOT compatibility:

| Area | Status |
| --- | --- |
| Explicit registration with `AddMap<TMap>()` | Preferred for trimming and Native AOT scenarios. |
| Generated registration with `AddGeneratedMappings()` | Preferred alternative to assembly scanning for maps in the current compilation. |
| Assembly scanning | Reflection-based and annotated as trimming-sensitive. |
| `QueryMapped*`, `ReadMapped*`, `QueryMultipleMapped`, streaming | Annotated as trimming/dynamic-code sensitive because runtime fallback can occur. |

Do not treat the package as fully Native AOT safe unless your application validates the exact query path and deployment mode.

## Compatibility

Current compatibility documentation lives in [COMPATIBILITY.md](COMPATIBILITY.md).

Short version:

- public packages target `netstandard2.0`;
- tests currently run on `net10.0`;
- Dapper range is `[2.1.79,3.0.0)`, with `2.1.79` validated in the current matrix;
- Dommel range is `[3.5.3,4.0.0)` for the optional Dommel package;
- SQLite is validated by automated provider tests;
- SQL Server and PostgreSQL have conditional harnesses but are not certified in CI yet;
- MySQL/MariaDB is not validated;
- SQL Server CE remains legacy/upstream-limited.

For users moving from the historical FluentMap package, see [MIGRATION.md](MIGRATION.md).

## Current Limitations

- `FluentMapper.Initialize(...)`, normal `Dapper.Query<T>()` and Dommel integrations use process-wide global state.
- Isolated runtimes apply to FluentMap-controlled materialization, not to normal Dapper queries or Dommel.
- Dommel uses global `DommelMapper` resolvers/builders.
- `QueryMultipleMapped` is sequential and buffered per result set; there is no `QueryMultipleMappedAsync`.
- `QueryMultipleMapped` is not Dapper multi-mapping with `splitOn`.
- FluentMap does not aggregate joined rows into graphs or maintain identity maps.
- Write converters are metadata-only in the current Dapper/Dommel write path.
- Generated materializers cover a supported subset and can fall back to runtime materialization.
- Assembly scanning and runtime fallback are trimming/AOT-sensitive.
- Value object construction uses compatible public constructors, not factory methods.

## More Documentation

- [MIGRATION.md](MIGRATION.md)
- [COMPATIBILITY.md](COMPATIBILITY.md)
- [SUPPORT.md](SUPPORT.md)
- [CHANGELOG.md](CHANGELOG.md)

## Contributing

Keep changes small, compatible with the public API and covered by focused tests. Typical local validation:

```bash
dotnet restore ./Dapper.FluentMap.sln
dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build
```

## License

FluentMap is licensed under the [MIT License](LICENSE).

# Português (Brasil)

FluentMap é uma camada avançada de mapeamento para Dapper. Ela permite descrever, com uma API fluente e fortemente tipada, como propriedades .NET se conectam a colunas de banco de dados, mantendo atributos de persistência fora dos POCOs.

FluentMap não é um ORM. Ele não faz tracking de entidades, não gera SQL arbitrário, não gerencia conexões, não executa migrations, não oferece LINQ e não substitui o Dapper.

Este repositório nasceu do projeto arquivado `Dapper.FluentMap` e está sendo evoluído neste fork.

## Posicionamento

Use FluentMap para:

- mappings explícitos entre propriedades e colunas;
- convenções e políticas de nomenclatura;
- propriedades ignoradas;
- constructor mapping para tipos imutáveis;
- materialização opt-in de objetos aninhados e value objects;
- profiles para formatos SQL alternativos;
- registro e materialização gerados quando suportados;
- metadata de persistência consumida por integrações como Dommel;
- configuração isolada e DI para materialização controlada pelo FluentMap.

Não use FluentMap como ORM, framework CRUD, query builder, unit of work ou abstração de banco.

## Instalação

Instale o pacote que corresponde ao recurso necessário:

| Pacote | Finalidade |
| --- | --- |
| `Dapper.FluentMap` | API core de mapping e integração com Dapper. |
| `Dapper.FluentMap.Dommel` | Integração opcional com Dommel para tabela, chave e colunas geradas. |
| `Dapper.FluentMap.DependencyInjection` | Integração opcional com `Microsoft.Extensions.DependencyInjection`. |
| `Dapper.FluentMap.Analyzers` | Analyzers Roslyn para erros de configuração prováveis em tempo de compilação. |
| `Dapper.FluentMap.Generators` | Source generator para registro de maps e materializadores gerados. |

```bash
dotnet add package Dapper.FluentMap
```

Os pacotes públicos targetam `netstandard2.0`. Consulte [COMPATIBILITY.md](COMPATIBILITY.md) antes de adotar um release candidate.

## Início Rápido

```csharp
using Dapper;
using Dapper.FluentMap;
using Dapper.FluentMap.Mapping;

public sealed class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(customer => customer.Id).ToColumn("customer_id");
    }
}

FluentMapper.Initialize(config =>
{
    config.AddMap<CustomerMap>();
});

var customer = connection.QuerySingle<Customer>(
    "SELECT 7 AS customer_id, 'Ada' AS Name;");
```

Chame `FluentMapper.Initialize(...)` no startup e trate a configuração global efetiva como somente leitura depois que as queries começarem.

## Mapeamento

Crie maps herdando de `EntityMap<TEntity>`:

```csharp
public sealed class ProductMap : EntityMap<Product>
{
    public ProductMap()
    {
        Map(product => product.Id).ToColumn("product_id");
        Map(product => product.Name).ToColumn("product_name", caseSensitive: false);
        Map(product => product.TransientValue).Ignore();
    }
}
```

Mappings explícitos têm precedência sobre convenções. Membros raiz não mapeados usam o comportamento normal do Dapper.

Convenções e políticas de nomenclatura cobrem padrões repetidos:

```csharp
using Dapper.FluentMap.Conventions;
using Dapper.FluentMap.Naming;

public sealed class PrefixConvention : Convention
{
    public PrefixConvention()
    {
        Properties().Configure(property => property.HasPrefix("col"));
    }
}

FluentMapper.Initialize(config =>
{
    config.AddConvention<PrefixConvention>().ForEntity<Customer>();
    config.UseNamingPolicy(NamingPolicy.SnakeCase, caseSensitive: false)
        .ForEntity<Order>();
});
```

As políticas disponíveis incluem `Identity`, `SnakeCase`, `Prefix(...)`, `Suffix(...)`, `Custom(...)`, `Then(...)`, `WithPrefix(...)` e `WithSuffix(...)`.

## Tipos Imutáveis

FluentMap participa do constructor mapping do Dapper para mappings explícitos no nível raiz:

```csharp
public sealed class Customer
{
    public Customer(int id, string fullName)
    {
        Id = id;
        FullName = fullName;
    }

    public int Id { get; }
    public string FullName { get; }
}

public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(customer => customer.Id).ToColumn("customer_id");
        Map(customer => customer.FullName).ToColumn("full_name");
    }
}
```

Use `QueryMapped*` quando o FluentMap precisar construir objetos aninhados imutáveis ou value objects.

## Objetos Aninhados

Caminhos aninhados usam a mesma API `Map(...)`:

```csharp
public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(customer => customer.Id).ToColumn("customer_id");
        Map(customer => customer.Address.City).ToColumn("city");
    }
}

var customer = connection.QueryMappedSingle<Customer>(
    "SELECT 7 AS customer_id, 'Sao Paulo' AS city;");
```

Materialização aninhada é opt-in via `QueryMapped*`, `ReadMapped*`, `QueryMultipleMapped` e helpers de streaming. `Dapper.Query<T>()` normal continua usando materialização raiz do Dapper.

## Value Objects

Para value objects escalares mapeados como um único valor de banco, prefira um `TypeHandler<T>` do Dapper:

```csharp
Map(customer => customer.Cpf).ToColumn("cpf");
```

Para value objects mapeados por componentes, a materialização controlada pelo FluentMap pode chamar construtores públicos compatíveis:

```csharp
public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(customer => customer.Id).ToColumn("customer_id");
        Map(customer => customer.Cpf.Number).ToColumn("cpf");
    }
}

var customer = connection.QueryMappedSingle<Customer>(
    "SELECT 1 AS customer_id, '12345678909' AS cpf;");
```

Factory methods não são usadas pelo materializador atual.

## Profiles

Profiles são mappings opt-in para a mesma entidade em formatos SQL diferentes:

```csharp
using Dapper.FluentMap.Mapping;

public sealed class LegacyProfile : IMappingProfile
{
}

public sealed class LegacyCustomerMap :
    EntityMap<Customer>,
    IProfileMap<LegacyProfile>
{
    public LegacyCustomerMap()
    {
        Map(customer => customer.Id).ToColumn("id");
        Map(customer => customer.Name).ToColumn("legal_name");
    }
}

FluentMapper.Initialize(config =>
{
    config.AddMap<CustomerMap>();
    config.AddProfile<LegacyCustomerMap>();
});

var legacy = connection.QueryMappedSingle<Customer, LegacyProfile>(
    "SELECT 7 AS id, 'Legacy Ltd.' AS legal_name;");
```

Profiles são selecionados por query controlada pelo FluentMap. Eles não substituem o type map global do Dapper para a entidade.

## Materialização Gerada

Instale `Dapper.FluentMap.Generators` para registro gerado de maps da compilação atual:

```bash
dotnet add package Dapper.FluentMap.Generators
```

Depois chame a extensão gerada:

```csharp
FluentMapper.Initialize(config =>
{
    config.AddGeneratedMappings();
});
```

O generator emite chamadas `AddMap<TMap>()` e `AddProfile<TMap>()` para maps elegíveis. Para mappings explícitos suportados, ele também pode registrar materializadores de linha gerados para o shape ordenado de colunas, incluindo propriedades simples, caminhos aninhados, value objects construídos por construtor e read converters suportados estaticamente.

Materialização gerada é otimização. Maps não suportados, shapes dinâmicos, divergências de shape, converters por instância/delegate e alguns padrões avançados usam fallback runtime.

## Semântica de Persistência

Metadata de persistência descreve participação em escrita sem mudar materialização de leitura:

```csharp
Map(product => product.CreatedAt)
    .ToColumn("created_at")
    .DatabaseDefaultOnInsert();

Map(product => product.UpdatedAt)
    .ToColumn("updated_at")
    .ReadOnly();

Map(product => product.Total)
    .ToColumn("total")
    .Computed();
```

`Ignore()` mantém o significado histórico: a propriedade não é materializada pelo FluentMap e não participa da metadata de persistência gerada. Para valores de banco que ainda devem ser selecionados, mas não escritos, use `ReadOnly()`, `Computed()`, `DatabaseDefaultOnInsert()`, `ExcludeFromInsert()` ou `ExcludeFromUpdate()`.

O pacote core armazena metadata. Dommel é o pacote atual que a consome para comportamento de `INSERT` e `UPDATE` gerados.

## QueryMultiple / Streaming

Use os helpers de query do FluentMap quando a materialização precisa honrar nested mappings, value objects, profiles, converters ou materializers gerados:

```csharp
var customers = connection.QueryMapped<Customer>(sql);
var customer = connection.QueryMappedSingle<Customer>(sql);
var legacy = connection.QueryMappedSingle<Customer, LegacyProfile>(legacySql);
```

Para múltiplos result sets:

```csharp
using var multi = connection.QueryMultipleMapped(sql);

var customers = multi.ReadMapped<Customer>();
var orders = multi.ReadMapped<Order>();
```

`ReadMapped*` consome result sets em sequência e bufferiza o result set atual.

Para processamento incremental:

```csharp
foreach (var customer in connection.QueryMappedUnbuffered<Customer>(sql))
{
    Process(customer);
}
```

Streaming assíncrono está disponível em `DbConnection`:

```csharp
await foreach (var customer in connection.QueryMappedUnbufferedAsync<Customer>(
    sql,
    cancellationToken))
{
    await ProcessAsync(customer, cancellationToken);
}
```

Streaming mantém o reader subjacente aberto até a enumeração terminar ou o enumerator ser descartado.

## Conversores de Propriedade

Conversores de propriedade são configurados por propriedade mapeada e executam somente na materialização controlada pelo FluentMap:

```csharp
public sealed class ProductMap : EntityMap<Product>
{
    public ProductMap()
    {
        Map(product => product.Status)
            .ToColumn("status_code")
            .ConvertFromDatabaseUsing<ProductStatusConverter, string>();
    }
}

public sealed class ProductStatusConverter :
    IReadPropertyConverter<string, ProductStatus>
{
    public ProductStatus ConvertFromDatabase(string value)
    {
        return value == "A" ? ProductStatus.Active : ProductStatus.Inactive;
    }
}
```

A precedência de conversão de leitura na materialização controlada pelo FluentMap é:

```text
tratamento de null/DBNull
    -> read converter da propriedade
    -> Dapper TypeHandler<TProperty>
    -> conversão default do FluentMap
```

Metadata de write converter pode ser configurada, mas não é executada atualmente por escritas Dapper ou Dommel.

## Configuração Isolada / DI

A API estática histórica continua suportada:

```csharp
FluentMapper.Initialize(config =>
{
    config.AddMap<CustomerMap>();
});
```

Para múltiplas configurações controladas pelo FluentMap no mesmo processo, crie configurações imutáveis e use seus runtimes:

```csharp
using Dapper.FluentMap.Configuration;

var runtime = new FluentMapConfigurationBuilder()
    .AddMap<CustomerMap>()
    .Build()
    .CreateRuntime();

var customer = runtime.QueryMappedSingle<Customer>(
    connection,
    "SELECT 7 AS customer_id, 'Ada' AS Name;");
```

Instale `Dapper.FluentMap.DependencyInjection` para registro em DI:

```csharp
using Microsoft.Extensions.DependencyInjection;

services.AddFluentMap(builder =>
{
    builder.AddMap<CustomerMap>();
    builder.Configure(config => config.AddGeneratedMappings());
});
```

O pacote de DI registra `ImmutableFluentMapConfiguration` e `FluentMapRuntime` como singletons. Ele não registra conexões de banco, repositories, bridges Dommel ou type maps globais do Dapper.

## AOT / Trimming

FluentMap tem prontidão parcial para trimming/AOT, não compatibilidade Native AOT completa:

| Área | Status |
| --- | --- |
| Registro explícito com `AddMap<TMap>()` | Preferencial para cenários com trimming e Native AOT. |
| Registro gerado com `AddGeneratedMappings()` | Alternativa preferencial ao assembly scanning para maps da compilação atual. |
| Assembly scanning | Baseado em reflection e anotado como sensível a trimming. |
| `QueryMapped*`, `ReadMapped*`, `QueryMultipleMapped`, streaming | Anotados como sensíveis a trimming/dynamic code porque fallback runtime pode ocorrer. |

Não trate o pacote como totalmente seguro para Native AOT sem validar o caminho de query e o modo de publicação exatos da sua aplicação.

## Compatibilidade

A documentação atual de compatibilidade está em [COMPATIBILITY.md](COMPATIBILITY.md).

Resumo:

- pacotes públicos targetam `netstandard2.0`;
- testes rodam atualmente em `net10.0`;
- a faixa de Dapper é `[2.1.79,3.0.0)`, com `2.1.79` validado na matriz atual;
- a faixa de Dommel é `[3.5.3,4.0.0)` no pacote opcional Dommel;
- SQLite é validado por testes automatizados de provider;
- SQL Server e PostgreSQL têm harness condicional, mas ainda não são certificados em CI;
- MySQL/MariaDB não está validado;
- SQL Server CE permanece legado/limitado por upstream.

Para migrar do FluentMap histórico, consulte [MIGRATION.md](MIGRATION.md).

## Limitações Atuais

- `FluentMapper.Initialize(...)`, `Dapper.Query<T>()` normal e integrações Dommel usam estado global process-wide.
- Runtimes isolados se aplicam à materialização controlada pelo FluentMap, não a queries Dapper normais nem Dommel.
- Dommel usa resolvers/builders globais do `DommelMapper`.
- `QueryMultipleMapped` é sequencial e bufferizado por result set; não há `QueryMultipleMappedAsync`.
- `QueryMultipleMapped` não é multi-mapping do Dapper com `splitOn`.
- FluentMap não agrega linhas de joins em grafos e não mantém identity map.
- Write converters são apenas metadata no caminho atual de escrita Dapper/Dommel.
- Materializers gerados cobrem um subconjunto suportado e podem cair para materialização runtime.
- Assembly scanning e fallback runtime são sensíveis a trimming/AOT.
- Construção de value objects usa construtores públicos compatíveis, não factory methods.

## Mais Documentação

- [MIGRATION.md](MIGRATION.md)
- [COMPATIBILITY.md](COMPATIBILITY.md)
- [SUPPORT.md](SUPPORT.md)
- [CHANGELOG.md](CHANGELOG.md)

## Contribuição

Mantenha mudanças pequenas, compatíveis com a API pública e cobertas por testes focados. Validação local típica:

```bash
dotnet restore ./Dapper.FluentMap.sln
dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build
```

## Licença

FluentMap é licenciado sob a [MIT License](LICENSE).
