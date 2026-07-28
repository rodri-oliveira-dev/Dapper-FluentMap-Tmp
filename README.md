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

Generated registration calls the existing `AddMap<TMap>()` / `AddProfile<TMap>()` paths. It does not generate database materializers, scan referenced assemblies or replace `FluentMapper.Validate()`.

The core runtime also exposes low-level generated materializer registration contracts for generator-emitted code. These contracts are additive infrastructure; current consumers do not need to register materializers manually, and missing generated materializers continue to use the existing runtime fallback.

## Trimming / Native AOT

FluentMap has different levels of support depending on the API:

| API area | Trimming / Native AOT status |
|---|---|
| Explicit `AddMap<TMap>()` registration | Preferred path for trimmed and Native AOT applications. |
| Generated registration | Useful alternative to assembly scanning for maps in the current compilation. |
| Assembly scanning APIs | Reflection-discovery based and annotated as trimming-sensitive. |
| `QueryMapped*` | Runtime reflection and dynamic-code based; annotated with trimming and dynamic-code warnings. |

Do not treat the package as fully Native AOT safe just because explicit registration works. Prefer explicit or generated registration and avoid reflection scanning in trimmed applications.

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
```

`QueryMapped*` returns buffered results and is the path that supports nested object materialization, constructor-built value objects and profile-specific mapping.

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

## Current Limitations

- FluentMap configuration is process-wide. Configure at startup and avoid changing mappings while queries are running.
- Assembly scanning depends on reflection discovery and is not the recommended path for trimmed or Native AOT applications.
- `QueryMapped*` uses runtime metadata and dynamic code; it is not the Native AOT-safe materialization path.
- Mapping profiles are selected only through `QueryMapped<TEntity, TProfile>()` APIs.
- `QueryMapped*` is buffered; it does not expose unbuffered streaming.
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

O registro gerado chama os caminhos existentes `AddMap<TMap>()` / `AddProfile<TMap>()`. Ele não gera materializadores de banco, não escaneia assemblies referenciados e não substitui `FluentMapper.Validate()`.

O runtime principal também expõe contratos de baixo nível para registro de materializadores gerados por código emitido por generator. Esses contratos são infraestrutura aditiva; consumidores atuais não precisam registrar materializadores manualmente, e a ausência de materializadores gerados continua usando o fallback runtime existente.

## Trimming / Native AOT

FluentMap tem níveis diferentes de suporte conforme a API:

| Área da API | Status para trimming / Native AOT |
|---|---|
| Registro explícito `AddMap<TMap>()` | Caminho preferencial para aplicações com trimming e Native AOT. |
| Registro gerado | Alternativa útil ao assembly scanning para maps da compilação atual. |
| APIs de assembly scanning | Baseadas em descoberta por reflection e anotadas como sensíveis a trimming. |
| `QueryMapped*` | Baseado em reflection e código dinâmico em runtime; anotado com warnings de trimming e dynamic code. |

Não trate o pacote como totalmente seguro para Native AOT apenas porque o registro explícito funciona. Prefira registro explícito ou gerado e evite scanning por reflection em aplicações com trimming.

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
```

`QueryMapped*` retorna resultados bufferizados e é o caminho que suporta materialização de objetos aninhados, Value Objects construídos por construtor e mapeamento específico por profile.

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

## Limitações Atuais

- A configuração do FluentMap é global no processo. Configure no startup e evite alterar mappings enquanto consultas estão em execução.
- Assembly scanning depende de descoberta por reflection e não é o caminho recomendado para aplicações com trimming ou Native AOT.
- `QueryMapped*` usa metadados de runtime e código dinâmico; ele não é o caminho de materialização seguro para Native AOT.
- Mapping profiles são selecionados apenas pelas APIs `QueryMapped<TEntity, TProfile>()`.
- `QueryMapped*` é bufferizado; ele não expõe streaming unbuffered.
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
