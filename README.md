# FluentMap

[Português (Brasil)](#português-brasil)

FluentMap provides a fluent API for mapping .NET object properties to database columns used by [Dapper](https://github.com/DapperLib/Dapper), keeping persistence attributes out of your POCOs.

> This repository originated from the archived `Dapper.FluentMap` project and is being evolved in this fork. Some legacy project metadata still reflects the original package history.

## Why FluentMap?

Dapper maps columns to members by name. FluentMap is useful when your database shape does not match your domain model, or when you want the mapping rules to live outside the model classes.

Use FluentMap to:

- map properties to columns explicitly;
- ignore mapped properties;
- apply naming conventions or naming policies;
- compose explicit maps, inherited maps and conventions;
- inspect and validate configuration;
- opt into FluentMap-controlled materialization for nested objects, immutable types, value objects and mapping profiles.

## Installation

Install the package that matches the functionality you need:

| Package | Purpose |
|---|---|
| `Dapper.FluentMap` | Core mapping API and Dapper integration. |
| `Dapper.FluentMap.DependencyInjection` | Optional Microsoft.Extensions.DependencyInjection integration. |
| `Dapper.FluentMap.Dommel` | Optional Dommel integration for table, key and generated-column mapping. |
| `Dapper.FluentMap.Analyzers` | Roslyn analyzers for statically provable configuration mistakes. |
| `Dapper.FluentMap.Generators` | Source generator for build-time map registration. |

```powershell
Install-Package Dapper.FluentMap
```

or:

```bash
dotnet add package Dapper.FluentMap
```

The core package targets `netstandard2.0` and depends on Dapper.

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
    config.AddMap(new CustomerMap());
});

var customer = connection.QuerySingle<Customer>(
    "SELECT 7 AS customer_id, 'Ada' AS Name;");
```

Call `FluentMapper.Initialize(...)` during application startup and treat the effective configuration as read-only once queries begin.

## Mapping

Create a map by deriving from `EntityMap<TEntity>`:

```csharp
public sealed class ProductMap : EntityMap<Product>
{
    public ProductMap()
    {
        Map(product => product.Id).ToColumn("product_id");
        Map(product => product.Name).ToColumn("product_name", caseSensitive: false);
        Map(product => product.LastModified).Ignore();
    }
}
```

Explicit mappings take precedence over convention mappings. Unmapped members fall back to Dapper's normal behavior.

Persistence metadata can describe write participation without changing read materialization:

```csharp
Map(product => product.CreatedAt)
    .ToColumn("created_at")
    .DatabaseDefaultOnInsert();

Map(product => product.UpdatedAt)
    .ToColumn("updated_at")
    .ReadOnly();

Map(product => product.Total)
    .Computed();
```

### Ignore

`Ignore()` keeps its historical meaning: the property does not participate in FluentMap materialization or generated persistence metadata.

```csharp
Map(product => product.TransientValue)
    .Ignore();
```

Do not use `Ignore()` for database values that should still be selected. It is not the same as read-only persistence metadata.

### Read-only

Use `ReadOnly()` for database values that are selected but not written by generated persistence operations:

```csharp
Map(product => product.UpdatedAt)
    .ToColumn("updated_at")
    .ReadOnly();
```

```text
SELECT: participates
INSERT: excluded
UPDATE: excluded
```

### Database Defaults

Use `DatabaseDefaultOnInsert()` when the database supplies the initial value if the column is omitted from `INSERT`, for example a `created_at DEFAULT ...` column:

```csharp
Map(product => product.CreatedAt)
    .ToColumn("created_at")
    .DatabaseDefaultOnInsert();
```

This excludes the property from generated `INSERT` metadata, keeps it readable, and keeps it updateable by default. Compose `.ExcludeFromUpdate()` when the value should remain database-controlled after insert.

### Computed

Use `Computed()` for values calculated by the database:

```csharp
Map(product => product.Total)
    .ToColumn("total")
    .Computed();
```

Computed properties participate in reads and are excluded from generated `INSERT` and `UPDATE` metadata.

## Property Converters

Property converters are configured on a specific mapping when a column value
needs member-specific conversion. They are useful when two properties of the
same CLR type need different database representations, or when a mapping
profile reads a legacy SQL shape differently from the default map.

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

Converter instances may be reused by concurrent materialization operations.
Implementations should be stateless or otherwise thread-safe.

### Read Conversion

Read conversion runs only in FluentMap-controlled materialization:
`QueryMapped*`, `ReadMapped*`, `QueryMultipleMapped`, synchronous unbuffered
streaming and asynchronous unbuffered streaming.

For those APIs the effective precedence is:

```text
null/DBNull handling
    -> property read converter
    -> Dapper TypeHandler<TProperty>
    -> FluentMap default conversion
```

`null` and `DBNull.Value` are not passed to read converters by default.
Nullable/reference targets receive `null`; non-nullable value types receive
`default(T)`. Normal Dapper queries such as `Query<T>()` are unchanged and do
not execute FluentMap property converters.

Generated materializers can emit read converter calls for converter types that
are statically supported, accessible and parameterless:

```csharp
Map(product => product.Status)
    .ToColumn("status_code")
    .ConvertFromDatabaseUsing<ProductStatusConverter, string>();
```

Converters supplied by instance or delegate continue to use the runtime
materializer fallback.

### Converter Metadata For Writes

The core package can store write converter metadata:

```csharp
Map(product => product.Status)
    .ToColumn("status_code")
    .ConvertToDatabaseUsing<ProductStatusWriteConverter, string>();
```

This does not currently convert parameters for Dapper or Dommel operations.
`Insert`, `Update` and other Dommel writes keep using the original entity values
and Dapper/provider parameter handling. Write converter execution is deferred
until there is a supported parameter-value hook.

### Profiles

Converters configured in a profile map apply only when that profile is selected:

```csharp
public sealed class LegacyProductMap :
    EntityMap<Product>,
    IProfileMap<LegacyProfile>
{
    public LegacyProductMap()
    {
        Map(product => product.Status)
            .ToColumn("legacy_status")
            .ConvertFromDatabaseUsing<LegacyProductStatusConverter, string>();
    }
}

var product = connection.QueryMappedSingle<Product, LegacyProfile>(
    "SELECT '1' AS legacy_status;");
```

Default-map converters do not automatically leak into profiles. Reuse must be
explicit, for example through `IncludeBase<T>()` or by configuring the converter
again in the profile map.

### Dapper TypeHandlers

Use a Dapper `TypeHandler<T>` when a type has one database representation across
the application. Use a FluentMap property converter when the conversion belongs
to one mapping, member path or profile.

```text
TypeHandler -> behavior by type
Property Converter -> behavior by mapping/member/profile
```

When both are present on a FluentMap-controlled read, the property read
converter wins for that mapped property. Without a property converter,
`QueryMapped*` uses the registered `TypeHandler<TProperty>` before FluentMap's
default conversion. Generated materializers do not call Dapper TypeHandlers in
this stage; scenarios that depend on TypeHandlers use the runtime fallback.

Inherited explicit mappings can be included when the derived entity should reuse a base entity map:

```csharp
public sealed class PreferredCustomerMap : EntityMap<PreferredCustomer>
{
    public PreferredCustomerMap()
    {
        IncludeBase<Customer>();
        Map(customer => customer.Tier).ToColumn("tier");
    }
}
```

Register the base map before the derived map.

## Configuration

Register maps explicitly:

```csharp
FluentMapper.Initialize(config =>
{
    config.AddMap<CustomerMap>();
    config.AddMap<OrderMap>();
});
```

Assembly scanning is available for normal runtime scenarios:

```csharp
FluentMapper.Initialize(config =>
{
    config.AddMapsFromAssemblyContaining<CustomerMap>();
    config.AddMapsFromAssembly(typeof(CustomerMap).Assembly, "App.Domain.Maps");
});
```

Use explicit registration for trimmed or Native AOT applications.

You can validate the current configuration after registration:

```csharp
FluentMapper.Initialize(config => config.AddMap<CustomerMap>());
FluentMapper.Validate();
```

For read-only inspection, use `FluentMapper.GetEntityMaps()` and `FluentMapper.GetTypeConventions()`. The public mutable dictionaries `FluentMapper.EntityMaps` and `FluentMapper.TypeConventions` remain for compatibility, but new code should prefer the registration APIs.

### Static configuration - compatibility

The historical static configuration remains supported:

```csharp
FluentMapper.Initialize(config =>
{
    config.AddMap<CustomerMap>();
});

FluentMapper.Validate();
```

This path publishes the default `ImmutableFluentMapConfiguration` and
`FluentMapRuntime` exposed by `FluentMapper.Configuration` and
`FluentMapper.Runtime`, and installs global Dapper type maps for default maps
and conventions. Use it when your process has one effective FluentMap
configuration and you want normal `connection.Query<T>()` calls to use the
global Dapper type map bridge.

### Isolated configuration

You can build an immutable configuration snapshot without mutating the global
`FluentMapper` state:

```csharp
using Dapper.FluentMap.Configuration;

var configuration = new FluentMapConfigurationBuilder()
    .AddMap<CustomerMap>()
    .Configure(config => config.AddGeneratedMappings())
    .Build();

var runtime = configuration.CreateRuntime();
```

`Build()` validates the same invariants used by `FluentMapper.Validate()` and
returns an `ImmutableFluentMapConfiguration` with read-only metadata for maps,
profiles, conventions, naming policies, persistence metadata, converters and
generated materializer registrations. The builder is sealed after `Build()`.
Create a `FluentMapRuntime` from the immutable configuration when multiple
configuration-specific `QueryMapped*` pipelines must coexist in the same
process. `FluentMapper.Initialize(...)` remains the global compatibility layer,
with `FluentMapper.Configuration` and `FluentMapper.Runtime` exposing the
currently published default configuration and runtime.

### Multiple configurations

Multiple configurations are supported for FluentMap-controlled materialization
when each operation uses the intended `FluentMapRuntime`:

```csharp
var current = new FluentMapConfigurationBuilder()
    .AddMap<CurrentCustomerMap>()
    .Build()
    .CreateRuntime();

var legacy = new FluentMapConfigurationBuilder()
    .AddMap<LegacyCustomerMap>()
    .Build()
    .CreateRuntime();

var currentCustomer = current.QueryMappedSingle<Customer>(
    connection,
    "SELECT 1 AS customer_id, 'Ada' AS customer_name;");

var legacyCustomer = legacy.QueryMappedSingle<Customer>(
    connection,
    "SELECT 2 AS customer_id, 'Grace' AS legacy_name;");
```

This isolation applies to `QueryMapped*`, `ReadMapped*`,
`QueryMultipleMapped`, profiles, converters, generated materializers and
runtime diagnostics. It does not make normal `Dapper.Query<T>()` or Dommel
select a FluentMap runtime per call.

### Test isolation

Tests can create a local builder and runtime instead of resetting global
FluentMap state:

```csharp
var runtime = new FluentMapConfigurationBuilder()
    .AddMap<CustomerMap>()
    .Build()
    .CreateRuntime();

var customer = runtime.QueryMappedSingle<Customer>(
    connection,
    "SELECT 42 AS customer_id, 'Grace' AS Name;");
```

This lets tests use different mappings for the same entity type in the same
process. Tests that exercise `FluentMapper.Initialize(...)`, direct
`FluentMapper.EntityMaps` mutation, normal Dapper type maps or Dommel still
touch process-wide state and should remain isolated accordingly.

## Dependency Injection

Install `Dapper.FluentMap.DependencyInjection` when using ASP.NET Core, Worker
Services or the generic host:

```bash
dotnet add package Dapper.FluentMap.DependencyInjection
```

Register FluentMap during service composition:

```csharp
using Microsoft.Extensions.DependencyInjection;

services.AddFluentMap(builder =>
{
    builder.AddMap<CustomerMap>();
    builder.Configure(config => config.AddGeneratedMappings());
});
```

`AddFluentMap(...)` builds and validates the immutable configuration
immediately, then registers both `ImmutableFluentMapConfiguration` and
`FluentMapRuntime` as singletons. Use the resolved runtime with the
configuration-aware query APIs:

```csharp
public sealed class CustomerReader
{
    private readonly FluentMapRuntime _runtime;

    public CustomerReader(FluentMapRuntime runtime)
    {
        _runtime = runtime;
    }

    public Customer Read(IDbConnection connection)
    {
        return _runtime.QueryMappedSingle<Customer>(
            connection,
            "SELECT 7 AS customer_id, 'Ada' AS name;");
    }
}
```

The DI package does not register database connections, repositories, Dommel
bridges or global Dapper type maps. Use explicit or generated registration for
trimmed and Native AOT applications; assembly scanning remains available but is
not the recommended DI path for those deployments.

## Conventions and Naming Policies

Conventions let you map repeated column patterns:

```csharp
using Dapper.FluentMap.Conventions;

public sealed class PrefixConvention : Convention
{
    public PrefixConvention()
    {
        Properties()
            .Configure(property => property.HasPrefix("col"));
    }
}

FluentMapper.Initialize(config =>
{
    config.AddConvention<PrefixConvention>()
        .ForEntity<Customer>();
});
```

Naming policies cover common name transformations:

```csharp
using Dapper.FluentMap.Naming;

FluentMapper.Initialize(config =>
{
    config.UseNamingPolicy(NamingPolicy.SnakeCase, caseSensitive: false)
        .ForEntity<Customer>();
});
```

Available policies include `Identity`, `SnakeCase`, `Prefix(...)`, `Suffix(...)`, `Custom(...)` and composition with `Then(...)`, `WithPrefix(...)` and `WithSuffix(...)`.

## Immutable Types and Constructor Mapping

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

When you need FluentMap to build nested immutable objects or value objects, use `QueryMapped*`.

## Nested Object Mapping

Nested member paths can be configured with the same `Map(...)` API:

```csharp
public sealed class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(customer => customer.Id).ToColumn("customer_id");
        Map(customer => customer.Address.City).ToColumn("city");
    }
}
```

Use FluentMap's opt-in query helpers to materialize nested object graphs:

```csharp
var customer = connection.QueryMappedSingle<Customer>(
    "SELECT 7 AS customer_id, 'Sao Paulo' AS city;");
```

`QueryMapped*` creates supported intermediate objects, preserves null semantics for nested subtrees and rejects unsupported paths with `FluentMapConfigurationException`.

## Value Objects

For scalar value objects mapped as a whole property, prefer a Dapper `TypeHandler<T>`:

```csharp
Map(customer => customer.Cpf).ToColumn("cpf");
```

For value objects mapped through their components, `QueryMapped*` can construct them through matching public constructors:

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

Factory methods are not used by the current runtime materializer.

## Mapping Profiles

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

Profiles are selected per `QueryMapped<TEntity, TProfile>()` operation. They do not replace the global Dapper type map for the entity.

Profiles can also be selected per result set when using mapped multiple results:

```csharp
using var multi = connection.QueryMultipleMapped(sql);

var currentCustomers = multi.ReadMapped<Customer>();
var legacyCustomers = multi.ReadMapped<Customer, LegacyProfile>();
```

Use `ReadMappedSingle<T>()` or `ReadMappedSingle<T, TProfile>()` when the current result set must contain exactly one row.

## Diagnostics

Use runtime validation to fail fast after configuration:

```csharp
FluentMapper.Validate();
```

Use `Explain<TEntity>()` or `Explain<TEntity, TProfile>()` to inspect the effective mapping:

```csharp
var explanation = FluentMapper.Explain<Customer>();

foreach (var member in explanation.Members)
{
    Console.WriteLine($"{member.MemberPath} -> {member.ColumnName} ({member.Source})");
}
```

## Source Generator and Analyzers

`Dapper.FluentMap.Analyzers` reports configuration mistakes that can be proven at compile time, such as invalid map expressions, duplicate member paths, duplicate columns and invalid profile registrations. It complements runtime validation and does not execute map constructors or scan assemblies.

`Dapper.FluentMap.Generators` discovers eligible `IEntityMap<TEntity>` implementations in the current compilation and emits `AddGeneratedMappings()`:

```csharp
FluentMapper.Initialize(config =>
{
    config.AddGeneratedMappings();
});
```

Generated registration calls the existing `AddMap<TMap>()` / `AddProfile<TMap>()` paths. For explicit maps with literal columns and supported deterministic construction, it also registers generated row materializers for the matching ordered column shape, including flat properties, nested object paths, constructor-built Value Objects and statically supported property read converters. Unsupported maps and unexpected shapes continue to use the runtime fallback. It does not scan referenced assemblies, execute map constructors during generation or replace `FluentMapper.Validate()`.

The core runtime also exposes low-level generated materializer registration contracts for generator-emitted code. These contracts are additive infrastructure; current consumers do not need to register materializers manually, and missing generated materializers continue to use the existing runtime fallback.

## Trimming / Native AOT

FluentMap has different levels of support depending on the API:

| API area | Trimming / Native AOT status |
|---|---|
| Explicit `AddMap<TMap>()` registration | Preferred path for trimmed and Native AOT applications. |
| Generated registration | Useful alternative to assembly scanning for maps in the current compilation. |
| Assembly scanning APIs | Reflection-discovery based and annotated as trimming-sensitive. |
| `QueryMapped*` | Runtime reflection and dynamic-code based; annotated with trimming and dynamic-code warnings. |

Do not treat the package as fully Native AOT safe just because explicit registration works. Prefer explicit or generated registration and avoid reflection scanning in trimmed applications. `QueryMapped*` keeps its trimming and dynamic-code annotations even when a generated materializer is available, because unsupported shapes can still fall back to the runtime materializer.

## Dapper Integration

FluentMap installs Dapper type maps for configured entities. The normal Dapper APIs continue to be the default path for root-level mapping:

```csharp
connection.Query<Customer>(sql);
connection.QuerySingle<Customer>(sql);
```

Use FluentMap query helpers when you need FluentMap-controlled advanced materialization:

```csharp
connection.QueryMapped<Customer>(sql);
connection.QueryMappedSingle<Customer>(sql);
connection.QueryMappedSingle<Customer, LegacyProfile>(sql);
connection.QueryMappedUnbuffered<Customer>(sql);
connection.QueryMappedUnbufferedAsync<Customer>(sql, cancellationToken);

using var multi = connection.QueryMultipleMapped(sql);
var customers = multi.ReadMapped<Customer>();
var orders = multi.ReadMapped<Order>();
```

`QueryMapped*` and `ReadMapped*` return buffered results and are the paths that support nested object materialization, constructor-built value objects and profile-specific mapping. When a generated materializer is registered for the entity, profile and ordered column shape, these APIs use it; otherwise they use the runtime materializer fallback.

For independent configurations in the same process, create a runtime from an
immutable configuration and use its query entry points:

```csharp
var configuration = new FluentMapConfigurationBuilder()
    .AddMap<CustomerMap>()
    .Build();

var runtime = configuration.CreateRuntime();
var customers = runtime.QueryMapped<Customer>(
    connection,
    "SELECT 7 AS customer_id, 'Ada' AS Name;");
```

The runtime owns configuration-scoped caches for mapping lookup, generated
materializer lookup and runtime materialization plans. It is safe to share across
concurrent queries when the configuration is immutable.

Use `QueryMultipleMapped(...)` when one command returns multiple result sets that all need FluentMap-controlled materialization:

```csharp
var sql = @"
    SELECT 1 AS customer_id, 'Ada' AS customer_name;
    SELECT 10 AS order_id, 42.50 AS total;";

using var multi = connection.QueryMultipleMapped(sql);

var customers = multi.ReadMapped<Customer>().ToList();
var orders = multi.ReadMapped<Order>().ToList();
```

Result sets are consumed sequentially. `ReadMapped<T>()` and `ReadMapped<T, TProfile>()` buffer the current result set, advance to the next one and keep the underlying reader open until all result sets are consumed or the `MappedGridReader` is disposed.

Profiles can be selected per result set:

```csharp
using var multi = connection.QueryMultipleMapped(sql);

var currentCustomers = multi.ReadMapped<Customer>();
var legacyCustomers = multi.ReadMapped<Customer, LegacyProfile>();
```

Use `QueryMappedUnbuffered<T>()` or `QueryMappedUnbuffered<T, TProfile>()` when you need to process a large result set incrementally:

```csharp
foreach (var customer in connection.QueryMappedUnbuffered<Customer>(sql))
{
    Process(customer);
}
```

Unbuffered queries are lazy: the command is executed when enumeration starts, not when the method is called. The underlying reader stays open until enumeration finishes or the enumerator is disposed. If FluentMap opens a closed connection for the enumeration, disposing the reader closes it again; if the connection was already open, it remains open and must stay usable for the whole enumeration. Dispose the enumerator, for example by using `foreach`, when stopping early.

Use `QueryMappedUnbufferedAsync<T>()` or `QueryMappedUnbufferedAsync<T, TProfile>()` on `DbConnection` when the provider supports asynchronous readers:

```csharp
using var cancellation = new CancellationTokenSource();

await foreach (var customer in connection.QueryMappedUnbufferedAsync<Customer>(
    sql,
    cancellation.Token))
{
    await ProcessAsync(customer, cancellation.Token);
}
```

Async unbuffered queries are also lazy and incremental. FluentMap awaits command execution and `DbDataReader.ReadAsync(...)`, propagates cancellation to supported async operations, and disposes the reader when enumeration completes, stops early, is canceled or throws. Row materialization remains synchronous after the row has been read; generated materializers and runtime fallback use the same dispatch as buffered and synchronous unbuffered queries.

`QueryMultipleMapped` is about multiple result sets, not Dapper multi-mapping with `splitOn`. FluentMap does not perform graph aggregation, identity maps or automatic join grouping; write the SQL shape you need and choose the mapped helper only when FluentMap should materialize each row.

## Dommel

Install `Dapper.FluentMap.Dommel` when using [Dommel](https://github.com/henkmollema/Dommel):

```bash
dotnet add package Dapper.FluentMap.Dommel
```

Create maps with `DommelEntityMap<TEntity>` when you need Dommel-specific table and key metadata:

```csharp
using Dapper.FluentMap.Dommel.Mapping;
using Dapper.FluentMap.Dommel;

public sealed class ProductMap : DommelEntityMap<Product>
{
    public ProductMap()
    {
        ToTable("products");
        Map(product => product.Id).ToColumn("product_id").IsKey().IsIdentity();
    }
}
```

Enable Dommel integration during FluentMap configuration:

```csharp
FluentMapper.Initialize(config =>
{
    config.AddMap(new ProductMap());
    config.ForDommel();
});
```

Dommel honors FluentMap persistence metadata for generated `INSERT` and `UPDATE`
commands. `ReadOnly()` and `Computed()` are selected but not written,
`DatabaseDefaultOnInsert()` and `ExcludeFromInsert()` are omitted from `INSERT`
while remaining updateable, and `ExcludeFromUpdate()` remains insertable but is
not written by `UPDATE`. These behaviors are metadata in the core package; Dommel
is the package that turns them into generated SQL behavior.

Key metadata is Dommel-specific:

```csharp
Map(product => product.Id)
    .ToColumn("product_id")
    .IsKey()
    .IsIdentity();

Map(product => product.Code)
    .ToColumn("product_code")
    .IsKey()
    .SetGeneratedOption(DatabaseGeneratedOption.None);
```

`IsKey()` identifies the row. `IsIdentity()` marks a database-generated identity
key, excluded from `INSERT` and from `UPDATE SET`. A non-identity key is assigned
by the application, participates in `INSERT`, and is used by Dommel in the
`UPDATE WHERE` clause rather than in `UPDATE SET`.

### Compatibility Notes

Historical FluentMap code sometimes used `Ignore()` to keep a property out of
Dommel `INSERT` or `UPDATE`. Keep `Ignore()` only for values that should not be
materialized. For database-generated values that must still be read, use the
persistence behavior that matches the intent: `ReadOnly()`, `Computed()`,
`DatabaseDefaultOnInsert()`, `ExcludeFromInsert()` or `ExcludeFromUpdate()`.

## Current Limitations

- `FluentMapper.Initialize(...)`, `Dapper.Query<T>()` and Dommel still use process-wide compatibility bridges. Use `ImmutableFluentMapConfiguration` + `FluentMapRuntime` with `QueryMapped*`/`ReadMapped*` when multiple configurations must coexist in the same process.
- Multiple configurations are isolated only for FluentMap-controlled materialization. Normal `Dapper.Query<T>()` uses the global `SqlMapper.SetTypeMap` registered for the entity type.
- Dommel integration uses global `DommelMapper` resolvers/builders and reads the legacy process-wide FluentMap collections; isolated core runtimes do not configure Dommel.
- Assembly scanning depends on reflection discovery and is not the recommended path for trimmed or Native AOT applications.
- `QueryMapped*` may use generated materializers for supported flat, nested and Value Object shapes, but it can still fall back to runtime metadata and dynamic code; it is not yet a guaranteed Native AOT-safe materialization path.
- Property converters are not a general object mapper, serializer, SQL hook or replacement for Dapper `TypeHandler<T>`.
- Write converters are metadata-only in the current Dommel integration and are not executed by `Insert` or `Update`.
- Converter type overloads require a public parameterless constructor; instance and delegate overloads are the preferred runtime configuration forms when a converter needs explicit construction.
- Generated read conversion supports statically visible converter types; converter instances, delegates, inaccessible converter types and unsupported fluent patterns use runtime fallback.
- Mapping profiles are selected through `QueryMapped<TEntity, TProfile>()` and `ReadMapped<TEntity, TProfile>()` APIs.
- `QueryMapped*` and `ReadMapped*` are buffered. Use `QueryMappedUnbuffered*` for explicit synchronous or asynchronous unbuffered streaming.
- `QueryMultipleMapped` consumes result sets sequentially and does not support concurrent reads from the same `MappedGridReader`.
- Streaming keeps the underlying reader open. Do not use the same connection concurrently while a reader is active unless the provider explicitly supports that usage.
- Multiple result sets are not Dapper multi-mapping by `splitOn`; FluentMap does not perform graph aggregation or automatic join grouping.
- Value object construction uses matching public constructors, not factory methods.

## Contributing

Keep changes small, compatible with the public API and covered by focused tests. The core library should remain a FluentMap layer for Dapper, not an ORM, SQL generator or CRUD abstraction.

Typical validation:

```bash
dotnet restore ./Dapper.FluentMap.sln
dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build
```

## License

FluentMap is licensed under the [MIT License](LICENSE).

---

# Português (Brasil)

[Back to English](#fluentmap)

FluentMap fornece uma API fluente para mapear propriedades de objetos .NET para colunas de banco de dados usadas pelo [Dapper](https://github.com/DapperLib/Dapper), mantendo atributos de persistência fora dos seus POCOs.

> Este repositório se originou do projeto arquivado `Dapper.FluentMap` e está sendo evoluído neste fork. Alguns metadados legados ainda refletem o histórico do pacote original.

## Por Que FluentMap?

O Dapper mapeia colunas para membros pelo nome. FluentMap é útil quando o formato do banco não combina com o modelo de domínio, ou quando você quer manter as regras de mapeamento fora das classes do modelo.

Use FluentMap para:

- mapear propriedades para colunas explicitamente;
- ignorar propriedades mapeadas;
- aplicar convenções ou políticas de nomenclatura;
- compor mapas explícitos, mapas herdados e convenções;
- inspecionar e validar a configuração;
- optar por materialização controlada pelo FluentMap para objetos aninhados, tipos imutáveis, Value Objects e profiles de mapeamento.

## Instalação

Instale o pacote conforme a funcionalidade necessária:

| Pacote | Finalidade |
|---|---|
| `Dapper.FluentMap` | API principal de mapeamento e integração com Dapper. |
| `Dapper.FluentMap.DependencyInjection` | Integração opcional com Microsoft.Extensions.DependencyInjection. |
| `Dapper.FluentMap.Dommel` | Integração opcional com Dommel para tabela, chave e colunas geradas. |
| `Dapper.FluentMap.Analyzers` | Analyzers Roslyn para erros de configuração detectáveis estaticamente. |
| `Dapper.FluentMap.Generators` | Source generator para registro de maps em tempo de build. |

```powershell
Install-Package Dapper.FluentMap
```

ou:

```bash
dotnet add package Dapper.FluentMap
```

O pacote principal tem target `netstandard2.0` e depende do Dapper.

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
    config.AddMap(new CustomerMap());
});

var customer = connection.QuerySingle<Customer>(
    "SELECT 7 AS customer_id, 'Ada' AS Name;");
```

Chame `FluentMapper.Initialize(...)` durante o startup da aplicação e trate a configuração efetiva como somente leitura depois que as consultas começarem.

## Mapeamento

Crie um map herdando de `EntityMap<TEntity>`:

```csharp
public sealed class ProductMap : EntityMap<Product>
{
    public ProductMap()
    {
        Map(product => product.Id).ToColumn("product_id");
        Map(product => product.Name).ToColumn("product_name", caseSensitive: false);
        Map(product => product.LastModified).Ignore();
    }
}
```

Mapeamentos explícitos têm precedência sobre convenções. Membros não mapeados usam o comportamento normal do Dapper.

Metadata de persistência pode descrever participação em escrita sem alterar a materialização de leitura:

```csharp
Map(product => product.CreatedAt)
    .ToColumn("created_at")
    .DatabaseDefaultOnInsert();

Map(product => product.UpdatedAt)
    .ToColumn("updated_at")
    .ReadOnly();

Map(product => product.Total)
    .Computed();
```

### Ignore

`Ignore()` mantém seu significado histórico: a propriedade não participa da materialização do FluentMap nem da metadata de persistência gerada.

```csharp
Map(product => product.TransientValue)
    .Ignore();
```

Não use `Ignore()` para valores do banco que ainda devem ser selecionados. Ele não é o mesmo que metadata de persistência read-only.

### Read-only

Use `ReadOnly()` para valores do banco que são selecionados, mas não escritos por operações de persistência geradas:

```csharp
Map(product => product.UpdatedAt)
    .ToColumn("updated_at")
    .ReadOnly();
```

```text
SELECT: participa
INSERT: excluido
UPDATE: excluido
```

### Defaults de Banco

Use `DatabaseDefaultOnInsert()` quando o banco fornece o valor inicial se a coluna for omitida do `INSERT`, por exemplo uma coluna `created_at DEFAULT ...`:

```csharp
Map(product => product.CreatedAt)
    .ToColumn("created_at")
    .DatabaseDefaultOnInsert();
```

Isso exclui a propriedade da metadata de `INSERT` gerado, mantém a leitura e preserva `UPDATE` por default. Componha `.ExcludeFromUpdate()` quando o valor também deve permanecer controlado pelo banco depois do insert.

### Computed

Use `Computed()` para valores calculados pelo banco:

```csharp
Map(product => product.Total)
    .ToColumn("total")
    .Computed();
```

Propriedades computed participam de leituras e são excluídas da metadata de `INSERT` e `UPDATE` gerados.

## Conversores de Propriedade

Conversores de propriedade sao configurados em um mapping especifico quando um
valor de coluna precisa de conversao local ao membro. Eles sao uteis quando duas
propriedades do mesmo tipo CLR precisam de representacoes de banco diferentes,
ou quando um mapping profile le um shape SQL legado de forma diferente do map
default.

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

Instancias de converter podem ser reutilizadas por materializacoes concorrentes.
Implementacoes devem ser stateless ou thread-safe.

### Conversao de Leitura

Conversao de leitura executa somente na materializacao controlada pelo
FluentMap: `QueryMapped*`, `ReadMapped*`, `QueryMultipleMapped`, streaming
unbuffered sincrono e streaming unbuffered assincrono.

Para essas APIs, a precedencia efetiva e:

```text
tratamento de null/DBNull
    -> property read converter
    -> Dapper TypeHandler<TProperty>
    -> conversao padrao do FluentMap
```

`null` e `DBNull.Value` nao sao enviados aos read converters por default.
Targets nullable/reference recebem `null`; value types nao nullable recebem
`default(T)`. Consultas Dapper normais, como `Query<T>()`, nao mudam e nao
executam converters de propriedade do FluentMap.

Materializadores gerados podem emitir chamadas de read converter para converter
types suportados estaticamente, acessiveis e parameterless:

```csharp
Map(product => product.Status)
    .ToColumn("status_code")
    .ConvertFromDatabaseUsing<ProductStatusConverter, string>();
```

Converters fornecidos por instancia ou delegate continuam usando fallback do
materializador de runtime.

### Metadata de Conversao para Escrita

O pacote core consegue armazenar metadata de write converter:

```csharp
Map(product => product.Status)
    .ToColumn("status_code")
    .ConvertToDatabaseUsing<ProductStatusWriteConverter, string>();
```

Isso ainda nao converte parametros em operacoes Dapper ou Dommel. `Insert`,
`Update` e outras escritas Dommel continuam usando os valores originais da
entidade e a parametrizacao do Dapper/provider. A execucao de write converters
fica adiada ate existir um hook suportado para valores de parametros.

### Profiles

Converters configurados em um profile map valem somente quando aquele profile e
selecionado:

```csharp
public sealed class LegacyProductMap :
    EntityMap<Product>,
    IProfileMap<LegacyProfile>
{
    public LegacyProductMap()
    {
        Map(product => product.Status)
            .ToColumn("legacy_status")
            .ConvertFromDatabaseUsing<LegacyProductStatusConverter, string>();
    }
}

var product = connection.QueryMappedSingle<Product, LegacyProfile>(
    "SELECT '1' AS legacy_status;");
```

Converters do map default nao vazam automaticamente para profiles. Reuso deve
ser explicito, por exemplo com `IncludeBase<T>()` ou configurando o converter
novamente no profile map.

### Dapper TypeHandlers

Use um `TypeHandler<T>` do Dapper quando um tipo tem uma representacao de banco
unica na aplicacao. Use um property converter do FluentMap quando a conversao
pertence a um mapping, member path ou profile especifico.

```text
TypeHandler -> comportamento por tipo
Property Converter -> comportamento por mapping/member/profile
```

Quando ambos existem em uma leitura controlada pelo FluentMap, o property read
converter tem precedencia naquela propriedade mapeada. Sem property converter,
`QueryMapped*` usa o `TypeHandler<TProperty>` registrado antes da conversao
padrao do FluentMap. Materializadores gerados nao chamam TypeHandlers do Dapper
nesta etapa; cenarios que dependem de TypeHandlers usam fallback runtime.

Mapeamentos explícitos herdados podem ser incluídos quando a entidade derivada deve reutilizar um map da entidade base:

```csharp
public sealed class PreferredCustomerMap : EntityMap<PreferredCustomer>
{
    public PreferredCustomerMap()
    {
        IncludeBase<Customer>();
        Map(customer => customer.Tier).ToColumn("tier");
    }
}
```

Registre o map da base antes do map derivado.

## Configuração

Registre maps explicitamente:

```csharp
FluentMapper.Initialize(config =>
{
    config.AddMap<CustomerMap>();
    config.AddMap<OrderMap>();
});
```

Assembly scanning está disponível para cenários normais de runtime:

```csharp
FluentMapper.Initialize(config =>
{
    config.AddMapsFromAssemblyContaining<CustomerMap>();
    config.AddMapsFromAssembly(typeof(CustomerMap).Assembly, "App.Domain.Maps");
});
```

Use registro explícito em aplicações com trimming ou Native AOT.

Você pode validar a configuração atual depois do registro:

```csharp
FluentMapper.Initialize(config => config.AddMap<CustomerMap>());
FluentMapper.Validate();
```

Para inspeção somente leitura, use `FluentMapper.GetEntityMaps()` e `FluentMapper.GetTypeConventions()`. Os dicionários públicos mutáveis `FluentMapper.EntityMaps` e `FluentMapper.TypeConventions` permanecem por compatibilidade, mas código novo deve preferir as APIs de registro.

### Configuração Estática - Compatibilidade

A configuração estática histórica continua suportada:

```csharp
FluentMapper.Initialize(config =>
{
    config.AddMap<CustomerMap>();
});

FluentMapper.Validate();
```

Esse caminho publica o `ImmutableFluentMapConfiguration` e o
`FluentMapRuntime` default expostos por `FluentMapper.Configuration` e
`FluentMapper.Runtime`, e instala type maps globais do Dapper para maps default
e conventions. Use esse caminho quando o processo possui uma única configuração
FluentMap efetiva e você quer que chamadas normais a `connection.Query<T>()`
usem a bridge global de type map do Dapper.

### Configuração Isolada

Tambem e possivel construir um snapshot imutavel sem alterar o estado global do
`FluentMapper`:

```csharp
using Dapper.FluentMap.Configuration;

var configuration = new FluentMapConfigurationBuilder()
    .AddMap<CustomerMap>()
    .Configure(config => config.AddGeneratedMappings())
    .Build();

var runtime = configuration.CreateRuntime();
```

`Build()` valida os mesmos invariants usados por `FluentMapper.Validate()` e
retorna um `ImmutableFluentMapConfiguration` com metadata read-only para maps,
profiles, conventions, naming policies, persistence metadata, converters e
generated materializer registrations. O builder fica selado depois de `Build()`.
Crie um `FluentMapRuntime` a partir da configuracao imutavel quando multiplos
pipelines `QueryMapped*` especificos por configuracao precisarem coexistir no
mesmo processo. `FluentMapper.Initialize(...)` continua sendo a camada global de
compatibilidade, com `FluentMapper.Configuration` e `FluentMapper.Runtime`
expondo a configuracao e o runtime default publicados.

### Múltiplas Configurações

Múltiplas configurações são suportadas para materialização controlada pelo
FluentMap quando cada operação usa o `FluentMapRuntime` correto:

```csharp
var current = new FluentMapConfigurationBuilder()
    .AddMap<CurrentCustomerMap>()
    .Build()
    .CreateRuntime();

var legacy = new FluentMapConfigurationBuilder()
    .AddMap<LegacyCustomerMap>()
    .Build()
    .CreateRuntime();

var currentCustomer = current.QueryMappedSingle<Customer>(
    connection,
    "SELECT 1 AS customer_id, 'Ada' AS customer_name;");

var legacyCustomer = legacy.QueryMappedSingle<Customer>(
    connection,
    "SELECT 2 AS customer_id, 'Grace' AS legacy_name;");
```

Esse isolamento vale para `QueryMapped*`, `ReadMapped*`,
`QueryMultipleMapped`, profiles, converters, materializadores gerados e
diagnósticos do runtime. Ele não faz `Dapper.Query<T>()` normal nem Dommel
selecionarem um runtime FluentMap por chamada.

### Isolamento de Testes

Testes podem criar um builder e runtime locais em vez de resetar o estado
global do FluentMap:

```csharp
var runtime = new FluentMapConfigurationBuilder()
    .AddMap<CustomerMap>()
    .Build()
    .CreateRuntime();

var customer = runtime.QueryMappedSingle<Customer>(
    connection,
    "SELECT 42 AS customer_id, 'Grace' AS Name;");
```

Isso permite que testes usem mappings diferentes para o mesmo tipo de entidade
no mesmo processo. Testes que exercitam `FluentMapper.Initialize(...)`,
mutação direta de `FluentMapper.EntityMaps`, type maps normais do Dapper ou
Dommel ainda tocam estado process-wide e devem continuar isolados de acordo.

## Dependency Injection

Instale `Dapper.FluentMap.DependencyInjection` ao usar ASP.NET Core, Worker
Services ou generic host:

```bash
dotnet add package Dapper.FluentMap.DependencyInjection
```

Registre o FluentMap na composição de serviços:

```csharp
using Microsoft.Extensions.DependencyInjection;

services.AddFluentMap(builder =>
{
    builder.AddMap<CustomerMap>();
    builder.Configure(config => config.AddGeneratedMappings());
});
```

`AddFluentMap(...)` constrói e valida a configuração imutável imediatamente, e
registra `ImmutableFluentMapConfiguration` e `FluentMapRuntime` como singletons.
Use o runtime resolvido com as APIs de query por configuração:

```csharp
public sealed class CustomerReader
{
    private readonly FluentMapRuntime _runtime;

    public CustomerReader(FluentMapRuntime runtime)
    {
        _runtime = runtime;
    }

    public Customer Read(IDbConnection connection)
    {
        return _runtime.QueryMappedSingle<Customer>(
            connection,
            "SELECT 7 AS customer_id, 'Ada' AS name;");
    }
}
```

O pacote de DI não registra conexões, repositories, bridges Dommel ou type maps
globais do Dapper. Use registro explícito ou gerado para aplicações com
trimming e Native AOT; assembly scanning continua disponível, mas não é o
caminho recomendado em DI para esses deployments.

## Convenções e Políticas de Nomenclatura

Convenções permitem mapear padrões repetidos de colunas:

```csharp
using Dapper.FluentMap.Conventions;

public sealed class PrefixConvention : Convention
{
    public PrefixConvention()
    {
        Properties()
            .Configure(property => property.HasPrefix("col"));
    }
}

FluentMapper.Initialize(config =>
{
    config.AddConvention<PrefixConvention>()
        .ForEntity<Customer>();
});
```

Políticas de nomenclatura cobrem transformações comuns:

```csharp
using Dapper.FluentMap.Naming;

FluentMapper.Initialize(config =>
{
    config.UseNamingPolicy(NamingPolicy.SnakeCase, caseSensitive: false)
        .ForEntity<Customer>();
});
```

As políticas disponíveis incluem `Identity`, `SnakeCase`, `Prefix(...)`, `Suffix(...)`, `Custom(...)` e composição com `Then(...)`, `WithPrefix(...)` e `WithSuffix(...)`.

## Tipos Imutáveis e Constructor Mapping

FluentMap participa do constructor mapping do Dapper para mapeamentos explícitos no nível raiz:

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

Quando você precisa que o FluentMap construa objetos aninhados imutáveis ou Value Objects, use `QueryMapped*`.

## Mapeamento de Objetos Aninhados

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
```

Use os helpers opt-in do FluentMap para materializar o grafo de objetos:

```csharp
var customer = connection.QueryMappedSingle<Customer>(
    "SELECT 7 AS customer_id, 'Sao Paulo' AS city;");
```

`QueryMapped*` cria objetos intermediários suportados, preserva semântica de null em subárvores aninhadas e rejeita caminhos não suportados com `FluentMapConfigurationException`.

## Value Objects

Para Value Objects escalares mapeados como uma propriedade inteira, prefira um `TypeHandler<T>` do Dapper:

```csharp
Map(customer => customer.Cpf).ToColumn("cpf");
```

Para Value Objects mapeados pelos seus componentes, `QueryMapped*` pode construí-los por construtores públicos compatíveis:

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

Factory methods não são usadas pelo materializador de runtime atual.

## Mapping Profiles

Profiles são mapeamentos opt-in para a mesma entidade em formatos SQL diferentes:

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

Profiles são selecionados por operação com `QueryMapped<TEntity, TProfile>()`. Eles não substituem o type map global do Dapper para a entidade.

Profiles também podem ser selecionados por result set em multiplos resultados mapeados:

```csharp
using var multi = connection.QueryMultipleMapped(sql);

var currentCustomers = multi.ReadMapped<Customer>();
var legacyCustomers = multi.ReadMapped<Customer, LegacyProfile>();
```

Use `ReadMappedSingle<T>()` ou `ReadMappedSingle<T, TProfile>()` quando o result set atual deve conter exatamente uma linha.

## Diagnósticos

Use validação em runtime para falhar cedo depois da configuração:

```csharp
FluentMapper.Validate();
```

Use `Explain<TEntity>()` ou `Explain<TEntity, TProfile>()` para inspecionar o mapeamento efetivo:

```csharp
var explanation = FluentMapper.Explain<Customer>();

foreach (var member in explanation.Members)
{
    Console.WriteLine($"{member.MemberPath} -> {member.ColumnName} ({member.Source})");
}
```

## Source Generator e Analyzers

`Dapper.FluentMap.Analyzers` reporta erros de configuração que podem ser provados em tempo de compilação, como expressões de map inválidas, caminhos de membros duplicados, colunas duplicadas e registros de profile inválidos. Ele complementa a validação de runtime e não executa construtores de maps nem faz scan de assemblies.

`Dapper.FluentMap.Generators` descobre implementações elegíveis de `IEntityMap<TEntity>` na compilação atual e emite `AddGeneratedMappings()`:

```csharp
FluentMapper.Initialize(config =>
{
    config.AddGeneratedMappings();
});
```

O registro gerado chama os caminhos existentes `AddMap<TMap>()` / `AddProfile<TMap>()`. Para maps explícitos com colunas literais e construção determinística suportada, ele também registra materializadores de linha gerados para o shape ordenado de colunas correspondente, incluindo propriedades flat, caminhos aninhados, Value Objects construídos por construtor e property read converters suportados estaticamente. Maps não suportados e shapes inesperados continuam usando o fallback runtime. Ele não escaneia assemblies referenciados, não executa construtores de maps durante a geração e não substitui `FluentMapper.Validate()`.

O runtime principal também expõe contratos de baixo nível para registro de materializadores gerados por código emitido por generator. Esses contratos são infraestrutura aditiva; consumidores atuais não precisam registrar materializadores manualmente, e a ausência de materializadores gerados continua usando o fallback runtime existente.

## Trimming / Native AOT

FluentMap tem níveis diferentes de suporte conforme a API:

| Área da API | Status para trimming / Native AOT |
|---|---|
| Registro explícito `AddMap<TMap>()` | Caminho preferencial para aplicações com trimming e Native AOT. |
| Registro gerado | Alternativa útil ao assembly scanning para maps da compilação atual. |
| APIs de assembly scanning | Baseadas em descoberta por reflection e anotadas como sensíveis a trimming. |
| `QueryMapped*` | Baseado em reflection e código dinâmico em runtime; anotado com warnings de trimming e dynamic code. |

Não trate o pacote como totalmente seguro para Native AOT apenas porque o registro explícito funciona. Prefira registro explícito ou gerado e evite scanning por reflection em aplicações com trimming. `QueryMapped*` mantém suas anotações de trimming e dynamic code mesmo quando um materializador gerado existe, porque shapes não suportados ainda podem cair para o materializador runtime.

## Integração com Dapper

FluentMap instala type maps do Dapper para entidades configuradas. As APIs normais do Dapper continuam sendo o caminho padrão para mapeamento no nível raiz:

```csharp
connection.Query<Customer>(sql);
connection.QuerySingle<Customer>(sql);
```

Use os helpers de consulta do FluentMap quando precisar de materialização avançada controlada pelo FluentMap:

```csharp
connection.QueryMapped<Customer>(sql);
connection.QueryMappedSingle<Customer>(sql);
connection.QueryMappedSingle<Customer, LegacyProfile>(sql);
connection.QueryMappedUnbuffered<Customer>(sql);
connection.QueryMappedUnbufferedAsync<Customer>(sql, cancellationToken);

using var multi = connection.QueryMultipleMapped(sql);
var customers = multi.ReadMapped<Customer>();
var orders = multi.ReadMapped<Order>();
```

`QueryMapped*` e `ReadMapped*` retornam resultados bufferizados e são os caminhos que suportam materialização de objetos aninhados, Value Objects construídos por construtor e mapeamento específico por profile. Quando existe materializador gerado para entidade, profile e shape ordenado de colunas, essas APIs o utilizam; caso contrário, usam o fallback de materialização em runtime.

Para configuracoes independentes no mesmo processo, crie um runtime a partir de
uma configuracao imutavel e use seus entry points de consulta:

```csharp
var configuration = new FluentMapConfigurationBuilder()
    .AddMap<CustomerMap>()
    .Build();

var runtime = configuration.CreateRuntime();
var customers = runtime.QueryMapped<Customer>(
    connection,
    "SELECT 7 AS customer_id, 'Ada' AS Name;");
```

O runtime possui caches escopados por configuracao para lookup de mapping,
lookup de materializador gerado e planos de materializacao runtime. Ele e seguro
para compartilhamento entre queries concorrentes quando a configuracao e
imutavel.

Use `QueryMultipleMapped(...)` quando um comando retorna múltiplos result sets que precisam de materialização controlada pelo FluentMap:

```csharp
var sql = @"
    SELECT 1 AS customer_id, 'Ada' AS customer_name;
    SELECT 10 AS order_id, 42.50 AS total;";

using var multi = connection.QueryMultipleMapped(sql);

var customers = multi.ReadMapped<Customer>().ToList();
var orders = multi.ReadMapped<Order>().ToList();
```

Os result sets são consumidos sequencialmente. `ReadMapped<T>()` e `ReadMapped<T, TProfile>()` bufferizam o result set atual, avançam para o próximo e mantêm o reader subjacente aberto até todos os result sets serem consumidos ou até o `MappedGridReader` ser descartado.

Profiles podem ser selecionados por result set:

```csharp
using var multi = connection.QueryMultipleMapped(sql);

var currentCustomers = multi.ReadMapped<Customer>();
var legacyCustomers = multi.ReadMapped<Customer, LegacyProfile>();
```

Use `QueryMappedUnbuffered<T>()` ou `QueryMappedUnbuffered<T, TProfile>()` quando precisar processar um result set grande de forma incremental:

```csharp
foreach (var customer in connection.QueryMappedUnbuffered<Customer>(sql))
{
    Process(customer);
}
```

Consultas unbuffered são lazy: o comando é executado quando a enumeração começa, não quando o método é chamado. O reader subjacente permanece aberto até a enumeração terminar ou o enumerator ser descartado. Se o FluentMap abrir uma conexão fechada para a enumeração, o dispose do reader fecha a conexão novamente; se a conexão já estava aberta, ela permanece aberta e precisa continuar válida durante toda a enumeração. Descarte o enumerator, por exemplo usando `foreach`, ao parar cedo.

Use `QueryMappedUnbufferedAsync<T>()` ou `QueryMappedUnbufferedAsync<T, TProfile>()` em `DbConnection` quando o provider suportar readers assíncronos:

```csharp
using var cancellation = new CancellationTokenSource();

await foreach (var customer in connection.QueryMappedUnbufferedAsync<Customer>(
    sql,
    cancellation.Token))
{
    await ProcessAsync(customer, cancellation.Token);
}
```

Consultas async unbuffered também são lazy e incrementais. O FluentMap aguarda a execução do comando e `DbDataReader.ReadAsync(...)`, propaga cancellation para operações async suportadas e descarta o reader quando a enumeração termina, para cedo, é cancelada ou falha. A materialização da linha continua síncrona depois que a linha foi lida; materializers gerados e fallback runtime usam o mesmo dispatch dos caminhos buffered e unbuffered síncrono.

`QueryMultipleMapped` trata de múltiplos result sets, não de Dapper multi-mapping com `splitOn`. O FluentMap não faz agregação de grafo, identity map nem agrupamento automático de joins; escreva o shape SQL necessário e use o helper mapeado apenas quando o FluentMap deve materializar cada linha.

## Dommel

Instale `Dapper.FluentMap.Dommel` ao usar [Dommel](https://github.com/henkmollema/Dommel):

```bash
dotnet add package Dapper.FluentMap.Dommel
```

Crie maps com `DommelEntityMap<TEntity>` quando precisar de metadados específicos do Dommel para tabela e chave:

```csharp
using Dapper.FluentMap.Dommel.Mapping;
using Dapper.FluentMap.Dommel;

public sealed class ProductMap : DommelEntityMap<Product>
{
    public ProductMap()
    {
        ToTable("products");
        Map(product => product.Id).ToColumn("product_id").IsKey().IsIdentity();
    }
}
```

Ative a integração com Dommel durante a configuração do FluentMap:

```csharp
FluentMapper.Initialize(config =>
{
    config.AddMap(new ProductMap());
    config.ForDommel();
});
```

A integração Dommel respeita a metadata de persistência em comandos `INSERT` e
`UPDATE` gerados. `ReadOnly()` e `Computed()` são selecionados, mas não escritos;
`DatabaseDefaultOnInsert()` e `ExcludeFromInsert()` são omitidos do `INSERT` e
continuam atualizáveis; `ExcludeFromUpdate()` continua inserível, mas não é
escrito pelo `UPDATE`. Esses comportamentos são metadata no pacote core; o
Dommel é o pacote que os transforma em comportamento de SQL gerado.

Metadata de chave é específica do Dommel:

```csharp
Map(product => product.Id)
    .ToColumn("product_id")
    .IsKey()
    .IsIdentity();

Map(product => product.Code)
    .ToColumn("product_code")
    .IsKey()
    .SetGeneratedOption(DatabaseGeneratedOption.None);
```

`IsKey()` identifica a linha. `IsIdentity()` marca uma identity gerada pelo banco,
excluída de `INSERT` e do `SET` de `UPDATE`. Uma key non-identity é atribuída
pela aplicação, participa do `INSERT` e é usada pelo Dommel no `WHERE` do
`UPDATE`, não no `SET`.

### Notas de Compatibilidade

Código FluentMap histórico às vezes usava `Ignore()` para remover uma
propriedade do `INSERT` ou `UPDATE` do Dommel. Mantenha `Ignore()` apenas para
valores que não devem ser materializados. Para valores gerados pelo banco que
ainda devem ser lidos, use o persistence behavior correspondente:
`ReadOnly()`, `Computed()`, `DatabaseDefaultOnInsert()`, `ExcludeFromInsert()` ou
`ExcludeFromUpdate()`.

## Limitações Atuais

- `FluentMapper.Initialize(...)`, `Dapper.Query<T>()` e Dommel continuam usando bridges globais/process-wide. Para multiplas configuracoes simultaneas no mesmo processo, use `ImmutableFluentMapConfiguration` + `FluentMapRuntime` com os entry points `QueryMapped*`/`ReadMapped*`.
- Múltiplas configurações são isoladas somente para materialização controlada pelo FluentMap. `Dapper.Query<T>()` normal usa o `SqlMapper.SetTypeMap` global registrado para o tipo de entidade.
- A integração Dommel usa resolvers/builders globais do `DommelMapper` e lê as coleções legadas process-wide do FluentMap; runtimes isolados do core não configuram o Dommel.
- Assembly scanning depende de descoberta por reflection e não é o caminho recomendado para aplicações com trimming ou Native AOT.
- Property converters nao sao object mapper geral, serializer, hook de SQL nem substituto para `TypeHandler<T>` do Dapper.
- Write converters sao metadata-only na integracao Dommel atual e nao sao executados por `Insert` ou `Update`.
- Overloads por tipo de converter exigem construtor publico parameterless; overloads por instancia e delegate sao as formas preferidas de configuracao runtime quando o converter precisa de construcao explicita.
- Conversao de leitura gerada suporta converter types visiveis estaticamente; instancias, delegates, converter types inacessiveis e padroes fluent nao suportados usam fallback runtime.
- `QueryMapped*` pode usar materializadores gerados para shapes flat, aninhados e Value Object suportados, mas ainda pode cair para metadados de runtime e código dinâmico; ele ainda não é um caminho de materialização garantidamente seguro para Native AOT.
- Mapping profiles são selecionados pelas APIs `QueryMapped<TEntity, TProfile>()` e `ReadMapped<TEntity, TProfile>()`.
- `QueryMapped*` e `ReadMapped*` são bufferizados. Use `QueryMappedUnbuffered*` para streaming unbuffered síncrono ou assíncrono explícito.
- `QueryMultipleMapped` consome result sets sequencialmente e não suporta leituras concorrentes do mesmo `MappedGridReader`.
- Streaming mantém o reader subjacente aberto. Não use a mesma conexão concorrentemente enquanto um reader estiver ativo, salvo quando o provider suportar explicitamente esse uso.
- Múltiplos result sets não são Dapper multi-mapping por `splitOn`; o FluentMap não faz agregação de grafo nem agrupamento automático de joins.
- A construção de Value Objects usa construtores públicos compatíveis, não factory methods.

## Contribuição

Mantenha mudanças pequenas, compatíveis com a API pública e cobertas por testes focados. A biblioteca principal deve continuar sendo uma camada de FluentMap para Dapper, não um ORM, gerador de SQL ou abstração de CRUD.

Validação típica:

```bash
dotnet restore ./Dapper.FluentMap.sln
dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build
```

## Licença

FluentMap é licenciado sob a [MIT License](LICENSE).
