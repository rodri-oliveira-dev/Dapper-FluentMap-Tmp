# Dependency Injection Integration Specification

## Objetivo

Adicionar uma integracao oficial pequena com
`Microsoft.Extensions.DependencyInjection` para ASP.NET Core, Worker Services,
generic host e aplicacoes modulares, sem tornar DI obrigatoria para o pacote
core.

## Package/project location

Decisao implementada:

```text
src/Dapper.FluentMap.DependencyInjection/
test/Dapper.FluentMap.DependencyInjection.Tests/
```

PackageId:

```text
Dapper.FluentMap.DependencyInjection
```

O pacote separado mantem `Dapper.FluentMap` livre de dependencia em
`Microsoft.Extensions.*` para consumidores que usam somente a API estatica,
runtime manual ou outros containers.

## Dependencies

O pacote de DI:

- referencia `Dapper.FluentMap`;
- depende de `Microsoft.Extensions.DependencyInjection.Abstractions`;
- nao depende de `Microsoft.Extensions.DependencyInjection` runtime;
- nao depende de ASP.NET Core, Hosting, Options, Logging ou Dommel.

`Microsoft.Extensions.DependencyInjection.Abstractions` foi escolhido porque
expoe `IServiceCollection` e os descriptors/lifetimes necessarios. A versao
10.0.10 suporta `netstandard2.0`, preservando a compatibilidade do pacote novo
com o target do core.

## Registration API

API publica:

```csharp
services.AddFluentMap(builder =>
{
    builder.AddMap<CustomerMap>();
    builder.Configure(config => config.AddGeneratedMappings());
});
```

Contrato:

- cria um `FluentMapConfigurationBuilder` local;
- executa o callback de configuracao uma vez;
- chama `Build()` uma vez;
- cria um `FluentMapRuntime` a partir do snapshot;
- chama `runtime.Validate()`;
- registra `ImmutableFluentMapConfiguration` e `FluentMapRuntime`;
- retorna o mesmo `IServiceCollection`.

A API nao recebe `IServiceProvider` no callback. Isso evita service locator e
mantem a configuracao como parte da composition root. Conversores que precisam
de estado externo continuam devendo ser configurados explicitamente por
instancia/delegate ou tratados por design futuro.

## Lifetime

Registros:

```text
ImmutableFluentMapConfiguration -> Singleton
FluentMapRuntime -> Singleton
```

Justificativa:

- o builder e mutavel e vive apenas durante o `AddFluentMap`;
- `Build()` produz snapshot imutavel e read-only;
- `FluentMapRuntime` possui caches derivados por configuracao, usa colecoes
  concorrentes no hot path e nao possui conexao, transacao, comando, reader ou
  estado por query;
- queries concorrentes usando o mesmo runtime ja sao suportadas pelos testes do
  runtime isolado e passam a ser cobertas no pacote DI.

Nao foram adicionados services scoped/transient porque o FluentMap nao possui
recurso por request. Aplicacoes podem registrar wrappers proprios quando
combinarem runtime com conexoes, tenants ou servicos scoped.

## Named/keyed/multiple configurations

O prompt 11.5 nao introduz named options nem keyed services.

Motivos:

- o pacote mira `netstandard2.0`; keyed services sao recurso moderno do
  ecossistema DI e exigiriam aumento de TFM ou dependencia condicional;
- nao ha ainda contrato publico de selecao nomeada de runtime no FluentMap;
- adicionar nomes agora criaria uma API dificil de versionar sem necessidade
  comprovada.

Suporte atual:

- uma configuracao default por `IServiceCollection`;
- multiplas configuracoes por multiplos `ServiceProvider`/composition roots;
- multiplas configuracoes manuais via
  `FluentMapConfigurationBuilder -> Build() -> CreateRuntime()`.

Evolucao futura permanece possivel por overloads adicionais, por exemplo
registro keyed/named em TFM moderno, sem quebrar `AddFluentMap(...)`.

## Startup validation

`AddFluentMap(...)` valida imediatamente durante composicao:

```text
configure(builder)
    -> builder.Build()
    -> configuration.CreateRuntime()
    -> runtime.Validate()
```

Essa escolha segue fail-fast sem depender da primeira resolucao do container.
Ela tambem evita que um `ServiceProvider` seja construido com metadata invalida
que so falharia na primeira query.

## Assembly scanning

Assembly scanning nao e obrigatorio no caminho DI.

O callback aceita toda a DSL do `FluentMapConfigurationBuilder`, entao scanning
continua possivel:

```csharp
services.AddFluentMap(builder =>
{
    builder.AddMapsFromAssemblyContaining<CustomerMap>();
});
```

Mas o caminho preferido para trimming/Native AOT e registro explicito ou
gerado. A API de DI nao adiciona scanning automatico por assembly de entrada,
marker type, AppDomain ou service collection.

## Generated registration

O source generator atual emite `AddGeneratedMappings()` como extensao sobre
`FluentMapConfiguration`. O builder preserva esse caminho por
`Configure(...)`:

```csharp
services.AddFluentMap(builder =>
{
    builder.Configure(config => config.AddGeneratedMappings());
});
```

Generated registration continua sendo build-time para descoberta dos maps da
compilacao atual. No pacote DI, ele e apenas mais uma entrada explicita para o
builder; os descriptors gerados sao congelados no snapshot e associados ao
runtime singleton.

## Trimming

O pacote DI em si nao faz scanning nem ativacao reflection-only. Ele delega ao
builder escolhido pelo usuario:

- `AddMap<TMap>()`: caminho preferido para trimmed apps;
- `Configure(config => config.AddGeneratedMappings())`: caminho preferido
  quando o source generator for adotado;
- `AddMapsFromAssembly*`: continua anotado como trimming-sensitive no core.

O pacote DI nao remove nem esconde os warnings do core. Quando o callback chama
uma API anotada, o aviso deve continuar aparecendo no call site do consumidor.

## Native AOT

Nao foi declarada compatibilidade Native AOT completa.

O pacote DI pode participar de composicao AOT-friendly quando o consumidor usa
registro explicito ou gerado. Ainda assim:

- `QueryMapped*` mantem anotacoes de trimming/dynamic-code porque pode cair no
  fallback runtime;
- assembly scanning nao e recomendado;
- o smoke AOT existente deve ser estendido futuramente para cobrir o pacote DI
  publicado, especialmente com generated registration.

## ASP.NET Core examples

Minimal hosting:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFluentMap(mapBuilder =>
{
    mapBuilder.AddMap<CustomerMap>();
});

var app = builder.Build();
```

Worker/generic host:

```csharp
Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddFluentMap(builder =>
        {
            builder.Configure(config => config.AddGeneratedMappings());
        });
    });
```

Consumer service:

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

O pacote nao registra `IDbConnection`, connection factory, repositories ou unit
of work. Esses lifetimes pertencem a aplicacao.

## Tests

Cobertura adicionada:

- registro de `ImmutableFluentMapConfiguration` e `FluentMapRuntime`;
- resolucao de services;
- identidade singleton;
- configuracao invalida com fail-fast;
- registro explicito por tipo;
- registro explicito por instancia;
- profiles;
- multiplos service providers com configuracoes independentes;
- queries concorrentes usando o runtime singleton;
- generated registration real via `AddGeneratedMappings()` no projeto de testes
  do source generator.

Nao foram criados testes ASP.NET completos porque `ServiceCollection` e
suficiente para validar contrato de registro, lifetimes e resolucao.
