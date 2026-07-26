## 📦 Archived
This repository is archived as I'm not using this library myself anymore and have no time maintaining it. Thanks for using it.

<hr>


# Dapper.FluentMap
Provides a simple API to fluently map POCO properties to database columns when using Dapper.

<hr>

| Windows | Linux/OSX | NuGet |
| --- | --- | --- |
| [![Windows Build status](https://ci.appveyor.com/api/projects/status/x6grw3cjuyud9c76?svg=true)](https://ci.appveyor.com/project/henkmollema/dapper-fluentmap) | [![Linux Build Status](https://travis-ci.org/henkmollema/Dapper-FluentMap.svg?branch=master)](https://travis-ci.org/henkmollema/Dapper-FluentMap) | [![NuGet Version](http://img.shields.io/nuget/v/Dapper.FluentMap.svg)](https://www.nuget.org/packages/Dapper.FluentMap/ "NuGet version") |

### Introduction

This [Dapper](https://github.com/StackExchange/dapper-dot-net) extension allows you to fluently configure the mapping between POCO properties and database columns. This keeps your POCO's clean of mapping attributes. The functionality is similar to [Entity Framework Fluent API](http://msdn.microsoft.com/nl-nl/data/jj591617.aspx). If you have any questions, suggestions or bugs, please don't hesitate to [contact me](mailto:henkmollema@gmail.com) or create an issue.

<hr>

### Download
[![Download Dapper.FluentMap on NuGet](http://i.imgur.com/Rs483do.png "Download Dapper.FluentMap on NuGet")](https://www.nuget.org/packages/Dapper.FluentMap)

<hr>

### Usage
#### Manual mapping
You can map property names manually using the [`EntityMap<TEntity>`](https://github.com/henkmollema/Dapper-FluentMap/blob/master/src/Dapper.FluentMap/Mapping/EntityMap.cs) class. When creating a derived class, the constructor gives you access to the `Map` method, allowing you to specify to which database column name a certain property of `TEntity` should map to.
```csharp
public class ProductMap : EntityMap<Product>
{
    public ProductMap()
    {
        // Map property 'Name' to column 'strName'.
        Map(p => p.Name)
            .ToColumn("strName");

        // Ignore the 'LastModified' property when mapping.
        Map(p => p.LastModified)
            .Ignore();
    }
}
```

Column names are mapped case sensitive by default. You can change this by specifying the `caseSensitive` parameter in the `ToColumn()` method: `Map(p => p.Name).ToColumn("strName", caseSensitive: false)`.

#### Nested object materialization
Nested paths can be configured with the same `Map(...)` API, but materializing the object graph is opt-in. Use `QueryMapped<T>()` or `QueryMappedSingle<T>()` when you want FluentMap to create supported mutable intermediate objects:

```csharp
public class CustomerMap : EntityMap<Customer>
{
    public CustomerMap()
    {
        Map(c => c.Address.City)
            .ToColumn("city");
    }
}

var customer = connection.QueryMappedSingle<Customer>(
    "SELECT 'Sao Paulo' AS city");
```

The regular `Dapper.Query<T>()` path continues to handle root properties, conventions, constructor mapping and Dapper fallback as before. The nested materializer supports mutable intermediate objects with public parameterless constructors and settable properties; immutable nested value objects and nested records are reserved for a generated or constructor-based materializer.

**Initialization:**
```csharp
FluentMapper.Initialize(config =>
    {
       config.AddMap(new ProductMap());
    });
```

You can also register map types directly when they have a public parameterless constructor:
```csharp
FluentMapper.Initialize(config =>
    {
       config
           .AddMap<ProductMap>()
           .AddMap<OrderMap>();
    });
```

Assembly scanning is available as a convenience, while explicit `AddMap<TMap>()` registration remains the path that does not require scanning:
```csharp
FluentMapper.Initialize(config =>
    {
       config.AddMapsFromAssemblyContaining<ProductMap>();
       config.AddMapsFromAssembly(typeof(ProductMap).Assembly, "App.Domain.Maps");
    });
```

#### Trimming and Native AOT
For applications published with IL trimming, single-file or Native AOT, prefer explicit registration:

```csharp
FluentMapper.Initialize(config =>
    {
       config.AddMap<ProductMap>();
       config.AddConvention<TypePrefixConvention>().ForEntity<Product>();
    });
```

Assembly scanning APIs such as `AddMapsFromAssembly(...)`, `AddMapsFromAssemblyContaining<TMarker>()`, `ForEntitiesInAssembly(...)`, `ForEntitiesInCurrentAssembly(...)` and the legacy `ApplyMapsFromAssemblies(...)` depend on reflection discovery and are annotated as trimming-sensitive. They remain supported for normal runtime usage, but they can warn or fail after trimming if discovered types or metadata are removed.

#### Generated mapping registration
Consumers can opt into `Dapper.FluentMap.Generators` to generate explicit registration for maps declared in the current project:

```csharp
using Dapper.FluentMap;

FluentMapper.Initialize(config =>
    {
       config.AddGeneratedMappings();
    });
```

The generated method calls `AddMap<TMap>()` for each eligible map in the current compilation. It does not scan referenced assemblies, instantiate maps during generation, or generate materializers.

#### Convention based mapping
When you have a lot of entity types, creating manual mapping classes can become plumbing. If your column names adhere to some kind of naming convention, you might be better off by configuring a mapping convention.

You can create a convention by creating a class which derives from the [`Convention`](https://github.com/henkmollema/Dapper-FluentMap/blob/master/src/Dapper.FluentMap/Conventions/Convention.cs) class. In the contructor you can configure the property conventions:
```csharp
public class TypePrefixConvention : Convention
{
    public TypePrefixConvention()
    {
        // Map all properties of type int and with the name 'id' to column 'autID'.
        Properties<int>()
            .Where(c => c.Name.ToLower() == "id")
            .Configure(c => c.HasColumnName("autID"));

        // Prefix all properties of type string with 'str' when mapping to column names.
        Properties<string>()
            .Configure(c => c.HasPrefix("str"));

        // Prefix all properties of type int with 'int' when mapping to column names.
        Properties<int>()
            .Configure(c => c.HasPrefix("int"));
    }
}
```

When initializing Dapper.FluentMap with conventions, the entities on which a convention applies must be configured. You can choose to either configure the entities explicitly or use assembly scanning.

```csharp
FluentMapper.Initialize(config =>
    {
        // Configure entities explicitly.
        config.AddConvention<TypePrefixConvention>()
              .ForEntity<Product>()
              .ForEntity<Order>;

        // Configure all entities in a certain assembly with an optional namespaces filter.
        config.AddConvention<TypePrefixConvention>()
              .ForEntitiesInAssembly(typeof(Product).Assembly, "App.Domain.Model");

        // Configure all entities in the current assembly with an optional namespaces filter.
        config.AddConvention<TypePrefixConvention>()
              .ForEntitiesInCurrentAssembly("App.Domain.Model.Catalog", "App.Domain.Model.Order");
    });
```

##### Transformations
The convention API allows you to configure transformation of property names to database column names. An implementation would look like this:
```csharp
public class PropertyTransformConvention : Convention
{
    public PropertyTransformConvention()
    {
        Properties()
            .Configure(c => c.Transform(s => Regex.Replace(input: s, pattern: "([A-Z])([A-Z][a-z])|([a-z0-9])([A-Z])", replacement: "$1$3_$2$4")));
    }
}
```

This configuration will map camel case property names to underscore seperated database column names (`UrlOptimizedName` -> `Url_Optimized_Name`).

<hr>

### [Dommel](https://github.com/henkmollema/Dommel)
Dommel contains a set of extensions methods providing easy CRUD operations using Dapper. One of the goals was to provide extension points for resolving table and column names. [Dapper.FluentMap.Dommel](https://github.com/henkmollema/Dapper-FluentMap/tree/master/src/Dapper.FluentMap.Dommel) implements certain interfaces of Dommel and uses the configured mapping. It also provides more mapping functionality.

#### [`PM> Install-Package Dapper.FluentMap.Dommel`](https://www.nuget.org/packages/Dapper.FluentMap.Dommel)

#### Usage
##### `DommelEntityMap<TEntity>`
This class derives from `EntityMap<TEntity>` and allows you to map an entity to a database table using the `ToTable()` method:

```csharp
public class ProductMap : DommelEntityMap<TEntity>
{
    public ProductMap()
    {
        ToTable("tblProduct");

        // ...
    }
}
```

##### `DommelPropertyMap<TEntity>`
This class derives `PropertyMap<TEntity>` and allows you to specify the key property of an entity using the `IsKey` method:

```csharp
public class ProductMap : DommelEntityMap<TEntity>
{
    public ProductMap()
    {
        Map(p => p.Id).IsKey();
    }
}
```

You can configure Dapper.FluentMap.Dommel in the `FluentMapper.Initialize()` method:

```csharp
FluentMapper.Initialize(config =>
    {
        config.AddMap(new ProductMap());
        config.ForDommel();
    });
```

## Resultado da Etapa 1

- Capacidades estabilizadas: parsing de expressoes por membro real, composicao mapping explicito/convention/fallback do Dapper, testes de integracao com materializacao real e cache interno estruturado.
- Principais decisoes: `FluentMapper` permanece como fachada publica; `MappingRegistry` e o dono interno de mappings/cache; `SqlMapper.SetTypeMap` continua como integracao global necessaria com o Dapper.
- Dividas transferidas: dicionarios publicos mutaveis preservados por compatibilidade, consumo direto pelo Dommel, paralelismo da suite ainda desabilitado, MemberPath/nested objects/Value Objects fora desta etapa.
- Relatorios: `docs/sdd/etapa-1/01-reflection-helper.md`, `docs/sdd/etapa-1/02-mapping-composition.md`, `docs/sdd/etapa-1/03-dapper-integration-tests.md`, `docs/sdd/etapa-1/04-mapping-registry-cache.md`.

## Resultado da Etapa 2

- Capacidades estabilizadas: `MemberPath` para identidade interna de propriedades, validacao fail-fast com `FluentMapConfigurationException`, heranca opt-in por `IncludeBase<TBase>()` e naming policies configuraveis via `UseNamingPolicy(...)`.
- Precedencia consolidada: mapping explicito do derivado, mapping explicito herdado mais proximo, mapping explicito herdado mais distante, convention/naming policy do tipo consultado e fallback do Dapper.
- APIs publicas adicionadas: `Dapper.FluentMap.FluentMapConfigurationException`, `EntityMap.IncludeBase<TBase>()`, `Dapper.FluentMap.Naming.NamingPolicy` e `FluentMapConfiguration.UseNamingPolicy(...)`.
- Naming policies implementadas: `SnakeCase`, `Prefix`, `Suffix`, `Custom` e composicao por `Then`, `WithPrefix` e `WithSuffix`, sem alterar `DefaultTypeMap.MatchNamesWithUnderscores`.
- Dividas adiadas: nested object materialization, Value Objects complexos, constructor/record mapping, multiple mapping profiles, Roslyn analyzers, source generators e AOT/trimming.
- Relatorios: `docs/sdd/etapa-2/01-member-path.md`, `docs/sdd/etapa-2/02-configuration-validation.md`, `docs/sdd/etapa-2/03-inherited-mappings.md`, `docs/sdd/etapa-2/04-naming-policies.md`.

## Resultado da Etapa 4

- Tooling disponivel: `Dapper.FluentMap.Analyzers` com diagnostics `DFM001` a `DFM005` e `Dapper.FluentMap.Generators` com `AddGeneratedMappings()`, `DFM006` e `DFM007`.
- Trimming: registro explicito e registro gerado foram validados em smoke trimmed sem warnings FluentMap-owned; assembly scanning permanece reflection-dependent e trimming-sensitive.
- Native AOT: publish continua bloqueado neste ambiente por ausencia do platform linker C++; nao ha declaracao de runtime AOT completo.
- Caminhos de registro: manual, gerado e assembly scanning coexistem; nenhum caminho antigo foi removido.
- Packaging: analyzer e generator ficam em `analyzers/dotnet/cs`; o core continua `netstandard2.0` sem dependencias Roslyn runtime.
- Limitacoes: o generator descobre apenas maps da compilacao atual e nao resolve nested object materialization, Value Objects complexos, multiple mapping profiles, query-specific mappings, custom materializer ou generated `DbDataReader` materializer.
- Relatorios: `docs/sdd/etapa-4/01-roslyn-analyzers.md`, `docs/sdd/etapa-4/02-trimming-aot.md`, `docs/sdd/etapa-4/03-source-generator.md`.
