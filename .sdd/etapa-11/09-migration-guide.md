# Configuration Isolation Migration Guide

## Legacy

Codigo existente continua suportado:

```csharp
FluentMapper.Initialize(configuration =>
{
    configuration.AddMap(new CustomerMap());
});

using var connection = OpenConnection();
var customer = connection.Query<Customer>(
    "SELECT 1 AS customer_id, 'Ada' AS customer_name;")
    .Single();
```

Use este caminho quando a aplicacao possui uma unica configuracao global e
precisa que `Dapper.Query<T>()` use o type map instalado em `SqlMapper`.

## Isolated configuration

Para codigo novo que precisa de isolamento, crie um builder, congele a
configuracao e use um runtime explicito:

```csharp
var configuration = new FluentMapConfigurationBuilder()
    .AddMap(new CustomerMap())
    .Build();

var runtime = configuration.CreateRuntime();

using var connection = OpenConnection();
var customer = runtime.QueryMappedSingle<Customer>(
    connection,
    "SELECT 1 AS customer_id, 'Ada' AS customer_name;");
```

Esse caminho nao instala type maps globais do Dapper e nao exige
`FluentMapper.Reset()` em testes.

## Dependency Injection

No pacote `Dapper.FluentMap.DependencyInjection`:

```csharp
var services = new ServiceCollection();

services.AddFluentMap(builder =>
{
    builder.AddMap(new CustomerMap());
});

using var provider = services.BuildServiceProvider();
var runtime = provider.GetRequiredService<FluentMapRuntime>();
```

`ImmutableFluentMapConfiguration` e `FluentMapRuntime` sao registrados como
singletons. O FluentMap nao registra `IDbConnection`, repositories ou unidade
de trabalho.

## Test isolation

Para novos testes, prefira criar runtime local:

```csharp
var runtime = new FluentMapConfigurationBuilder()
    .AddMap(new CustomerMap())
    .Build()
    .CreateRuntime();

using var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();

var customer = runtime.QueryMappedSingle<Customer>(
    connection,
    "SELECT 42 AS customer_id, 'Grace' AS customer_name;");
```

Esse teste pode rodar junto de outro teste que cria outro runtime para o mesmo
tipo com colunas diferentes, porque caches e metadata derivados pertencem ao
runtime.

## Multiple configurations

Suportado para APIs controladas pelo runtime:

```csharp
var current = new FluentMapConfigurationBuilder()
    .AddMap(new CurrentCustomerMap())
    .Build()
    .CreateRuntime();

var legacy = new FluentMapConfigurationBuilder()
    .AddMap(new LegacyCustomerMap())
    .Build()
    .CreateRuntime();

var currentCustomer = current.QueryMappedSingle<Customer>(
    connection,
    "SELECT 1 AS customer_id, 'Ada' AS customer_name;");

var legacyCustomer = legacy.QueryMappedSingle<Customer>(
    connection,
    "SELECT 2 AS customer_id, 'Grace' AS legacy_name;");
```

Os dois runtimes podem coexistir no mesmo processo e ate serem usados
concorrentemente quando cada chamada passa o runtime correto.

## Generated registration

O source generator continua compativel com o builder por `Configure(...)`:

```csharp
var runtime = new FluentMapConfigurationBuilder()
    .Configure(configuration => configuration.AddGeneratedMappings())
    .Build()
    .CreateRuntime();
```

Os generated materializers registrados assim ficam associados ao snapshot e ao
runtime criados por esse builder.

## Known limitations

### Dapper.Query<T>()

`connection.Query<T>()` usa `SqlMapper.SetTypeMap`, que e global por tipo no
processo. Ele nao consegue escolher uma configuracao FluentMap por chamada.

Quando precisar de multiplas configuracoes para o mesmo tipo:

```csharp
var rows = runtime.QueryMapped<Customer>(
    connection,
    "SELECT 1 AS customer_id, 'Ada' AS customer_name;");
```

Nao use `connection.Query<Customer>()` esperando que ele selecione o runtime
isolado.

### Dommel

`configuration.ForDommel()` instala resolvers e SQL builders globais no
`DommelMapper`. Os resolvers atuais leem `FluentMapper.EntityMaps` e
`FluentMapper.TypeConventions`.

Consequencia:

- uma configuracao criada apenas por `FluentMapConfigurationBuilder` nao dirige
  Dommel;
- multiplas configuracoes Dommel simultaneas para o mesmo tipo nao sao
  suportadas nesta etapa;
- nao ha bridge por runtime para Dommel no prompt 11.6.

### Legacy mutable dictionaries

`FluentMapper.EntityMaps` e `FluentMapper.TypeConventions` continuam mutaveis
por compatibilidade. Mutacao direta pode bypassar validacao, invalidacao de
cache e instalacao de type map Dapper. Codigo novo deve usar builder ou
`FluentMapper.Initialize(...)`.
