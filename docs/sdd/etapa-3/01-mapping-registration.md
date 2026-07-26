# 01 - Registro E Descoberta De Mappings

## Specification

Modernizar o registro de mappings sem abandonar a proposta central do FluentMap: mapping externo, fortemente tipado e sem atributos no modelo.

Objetivos tratados:

- preservar `AddMap(new CustomerMap())`;
- adicionar registro explicito sem scanning por tipo de map, como `AddMap<CustomerMap>()`;
- adicionar descoberta por assembly como conveniencia;
- adicionar marker type para escolher o assembly sem depender de `Assembly.GetCallingAssembly()`;
- integrar todos os caminhos ao `MappingRegistry`;
- preservar validacoes, inheritance mappings, conventions, naming policies e precedencia consolidada;
- tornar duplicidades deterministicas e diagnosticas;
- documentar reflection restante sem declarar suporte AOT/trimming completo.

Fora do objetivo:

- remover APIs antigas;
- criar DI container ou integrar `IServiceCollection`;
- transformar scanning em mecanismo principal de startup;
- implementar source generator, analyzer ou AOT completo;
- alterar funcionalmente Dommel.

## Discovery

Arquivos analisados:

- `AGENTS.md`
- `.agents/skills/run-tests/SKILL.md`
- `docs/sdd/etapa-1/README.md`
- `docs/sdd/etapa-1/status.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-1/04-mapping-registry-cache.md`
- `docs/sdd/etapa-2/README.md`
- `docs/sdd/etapa-2/status.md`
- `docs/sdd/etapa-2/decisions.md`
- `docs/sdd/etapa-2/01-member-path.md`
- `docs/sdd/etapa-2/02-configuration-validation.md`
- `docs/sdd/etapa-2/03-inherited-mappings.md`
- `docs/sdd/etapa-2/04-naming-policies.md`
- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `src/Dapper.FluentMap/Utils/FluentMapConfigurationExtensions.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/Mapping/EntityMap.cs`
- `src/Dapper.FluentMap/MappingConfigurationValidator.cs`
- `src/Dapper.FluentMap/Configuration/FluentConventionConfiguration.cs`
- testes de manual mapping, conventions e registry.

Formas atuais de registro:

- `FluentMapper.Initialize(Action<FluentMapConfiguration>)` reutiliza uma instancia estatica de `FluentMapConfiguration`.
- `FluentMapConfiguration.AddMap<TEntity>(IEntityMap<TEntity> mapper)` registra uma instancia ja criada.
- `FluentMapConfiguration.AddConvention<TConvention>()` cria a convention por `new()` e retorna `FluentConventionConfiguration`.
- `FluentConventionConfiguration.ForEntity<T>()` aplica convention para uma entidade explicita.
- `FluentConventionConfiguration.ForEntitiesInAssembly(...)` usa `Assembly.GetExportedTypes()` para entidades e registra conventions por tipo.
- `FluentMapConfigurationExtensions.ApplyMapsFromAssemblies(...)` e a API historica de discovery de entity maps por assembly.

Como `EntityMap<TEntity>` e registrado hoje:

- o consumidor instancia manualmente o map;
- `AddMap<TEntity>(IEntityMap<TEntity>)` valida nulo e chama `FluentMapper.Registry.AddEntityMap(mapper)`;
- `MappingRegistry.AddEntityMap<TEntity>` valida duplicidade de entidade, valida o map, valida bases incluidas, valida composicao efetiva, grava em `EntityMaps`, invalida cache do tipo e instala `FluentMapTypeMap<TEntity>` no Dapper.

Mappings por tipo:

- nao havia API direta `AddMap<CustomerMap>()`;
- o caminho por tipo existia apenas indiretamente em `ApplyMapsFromAssemblies(...)`, via reflection.

Instanciacao:

- registro explicito exigia instancia manual;
- conventions usam constraint `new()`;
- discovery historico usa `Activator.CreateInstance(type)`.

Assembly scanning historico:

- `ApplyMapsFromAssemblies(...)` chama `Assembly.GetTypes()`;
- ignora tipos abstratos e interfaces;
- encontra tipos com interface fechada `IEntityMap<>`;
- detecta mais de um map para a mesma entidade antes do registro;
- usa `GetMethod(nameof(AddMap))`, `MakeGenericMethod(...)`, `Invoke(...)` e `Activator.CreateInstance(...)`;
- a ordem de registro vem da ordem retornada por reflection.

Constraints de construtor:

- instancia manual nao exige construtor especifico da API;
- discovery historico exige construtor sem parametros em runtime;
- quando construtor falta ou lanca, a falha vem de reflection e pode chegar embrulhada por `TargetInvocationException`.

Duplicidades:

- duplicidade de entidade no registry falha com `FluentMapConfigurationException`;
- duplicidade dentro do mesmo assembly scan historico falha com `InvalidOperationException`;
- o modelo validado na Etapa 2 nao permite conflito silencioso de coluna em `PropertyMap` do core.

Validacao:

- entity maps sao validados no registro global, antes de gravar no registry;
- inheritance por `IncludeBase<TBase>()` exige base map ja registrado;
- conventions e naming policies compartilham o pipeline de convention e validacao existente.

Scanning de tipos invalidos:

- abstratos e interfaces ja sao ignorados pelo discovery historico;
- genericos abertos nao sao tratados explicitamente no discovery historico;
- tipos concretos sem construtor publico sem parametros falham durante `Activator.CreateInstance`.

## Decision

APIs adicionadas:

```csharp
configuration.AddMap<CustomerMap>();

configuration
    .AddMap<CustomerMap>()
    .AddMap<OrderMap>();

configuration.AddMapsFromAssembly(typeof(CustomerMap).Assembly);

configuration.AddMapsFromAssemblyContaining<CustomerMap>();
```

Tambem serao aceitos filtros opcionais de namespace nos metodos de scanning, seguindo o estilo de `ForEntitiesInAssembly(...)`:

```csharp
configuration.AddMapsFromAssembly(assembly, "App.Domain.Maps");
configuration.AddMapsFromAssemblyContaining<CustomerMap>("App.Domain.Maps");
```

Registro de instancia:

- `AddMap<TEntity>(IEntityMap<TEntity> mapper)` permanece como API historica;
- assinatura e comportamento sao preservados.

Registro generico:

- `AddMap<TMap>()` representa o caminho explicito sem assembly scanning;
- `TMap` deve implementar `IEntityMap` e possuir construtor publico sem parametros;
- o tipo deve implementar exatamente uma interface fechada `IEntityMap<TEntity>`;
- a entidade e inferida dessa interface e o registro passa pelo mesmo `MappingRegistry`.

Assembly scanning:

- scanning moderno usa apenas tipos exportados da assembly;
- tipos abstratos, interfaces e genericos abertos sao ignorados;
- candidatos sao ordenados deterministamente por nome completo antes de qualquer decisao;
- duplicidade de entidade dentro do conjunto descoberto falha antes da instanciacao;
- maps sao instanciados antes do registro para permitir ordenacao por `IncludeBase<TBase>()`;
- quando um map inclui base map tambem descoberto no mesmo conjunto, o registro e ordenado para registrar a base primeiro;
- ciclos ou dependencias impossiveis falham com diagnostico.

Duplicidades:

- mesmo mapping registrado duas vezes: falha pelo registry porque a entidade ja possui map;
- mesma entidade com mappings diferentes: falha pelo registry ou pelo preflight do scanning;
- registro explicito seguido de scanning da mesma entidade: falha pelo registry durante o scanning;
- nao ha "ultimo ganha".

Reflection:

- `AddMap<TMap>()` nao faz assembly scanning, mas usa reflection limitada para inferir `TEntity` de `IEntityMap<TEntity>`;
- `AddMapsFromAssembly(...)` depende de reflection e `Activator.CreateInstance`;
- `MappingRegistry` ainda usa `Activator.CreateInstance(typeof(FluentMapTypeMap<>).MakeGenericType(type))` para instalar type map no Dapper;
- AOT/trimming completo permanece divida futura.

## Delivery

Arquivos alterados:

- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/Utils/FluentMapConfigurationExtensions.cs`
- `test/Dapper.FluentMap.Tests/MappingRegistrationTests.cs`
- `README.md`
- `docs/sdd/etapa-3/README.md`
- `docs/sdd/etapa-3/status.md`
- `docs/sdd/etapa-3/decisions.md`
- `docs/sdd/etapa-3/01-mapping-registration.md`

API anterior preservada:

```csharp
configuration.AddMap(new CustomerMap());
```

APIs novas:

```csharp
configuration.AddMap<CustomerMap>();

configuration
    .AddMap<CustomerMap>()
    .AddMap<OrderMap>();

configuration.AddMapsFromAssembly(typeof(CustomerMap).Assembly);
configuration.AddMapsFromAssembly(typeof(CustomerMap).Assembly, "App.Domain.Maps");

configuration.AddMapsFromAssemblyContaining<CustomerMap>();
configuration.AddMapsFromAssemblyContaining<CustomerMap>("App.Domain.Maps");
```

Implementacao:

- `FluentMapConfiguration.AddMap<TMap>()` cria o map e retorna a propria configuracao para permitir chaining;
- o tipo de entidade e inferido pela interface fechada `IEntityMap<TEntity>`;
- `MappingRegistry` recebeu overload interno `AddEntityMap(Type, IEntityMap)` para registrar o tipo inferido sem `MakeGenericMethod`/`Invoke`;
- o overload generico antigo do registry foi preservado e delega para o novo overload interno;
- `AddMapsFromAssembly(...)` usa tipos exportados da assembly, filtra namespace opcional, ignora abstratos/interfaces/genericos abertos, detecta duplicidades antes de instanciar e registra de forma deterministica;
- `AddMapsFromAssemblyContaining<TMarker>(...)` usa a assembly do marker type e compartilha o mesmo fluxo;
- scanning instancia todos os maps descobertos antes do registro e ordena por `IncludeBase<TBase>()` quando base e derivado aparecem no mesmo conjunto descoberto;
- `ApplyMapsFromAssemblies(...)` foi mantido por compatibilidade e recebeu apenas ajuste de comentario XML para evitar ambiguidade com a nova overload.

Duplicidades:

- mesmo mapping registrado duas vezes: `FluentMapConfigurationException` pelo registry;
- mesma entidade com mappings diferentes por registro explicito: `FluentMapConfigurationException` pelo registry;
- mesma entidade duplicada dentro do scanning: `FluentMapConfigurationException` antes de qualquer registro;
- registro explicito seguido de scanning da mesma entidade: `FluentMapConfigurationException` durante o registro descoberto;
- nenhum fluxo novo usa "ultimo ganha".

Reflection restante:

- `AddMap<TMap>()` nao faz assembly scanning, mas usa reflection para identificar a unica interface `IEntityMap<TEntity>`;
- `AddMapsFromAssembly(...)` usa `Assembly.GetExportedTypes()` e `Activator.CreateInstance`;
- `MappingRegistry` continua usando `Activator.CreateInstance(typeof(FluentMapTypeMap<>).MakeGenericType(type))` para instalar type maps no Dapper;
- essas dependencias ficam registradas como divida futura para AOT/trimming.

Compatibilidade:

- nenhuma API publica foi removida ou marcada como obsoleta;
- `AddMap(new CustomerMap())` continua funcionando;
- target `netstandard2.0` do core foi preservado;
- Dommel nao recebeu alteracao funcional;
- conventions, naming policies, inheritance mappings e validacao continuam passando pelo `MappingRegistry`.

Testes adicionados em `MappingRegistrationTests` cobrem:

- registro por instancia existente;
- registro generico;
- materializacao real com Dapper via registro generico;
- chaining de multiplos mappings explicitos;
- scanning por assembly;
- marker type;
- map abstrato ignorado;
- tipo invalido que nao implementa `IEntityMap<TEntity>`;
- mesmo mapping registrado duas vezes;
- entidade duplicada com maps diferentes;
- duplicidade detectada dentro do scanning antes de registro parcial;
- scanning apos registro explicito;
- erro de construtor;
- validacao integrada;
- ordenacao de scanning para inheritance mappings.

## Validation

Ambiente:

- SDK: `10.0.302`
- test runner detectado: VSTest com xUnit v3
- projeto principal: `netstandard2.0`
- projeto de testes do core: `net10.0`

Comandos de validacao localizada:

- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~MappingRegistrationTests"`
  - resultado: sucesso, 15 testes aprovados.
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~MappingRegistrationTests|FullyQualifiedName~MappingRegistryTests|FullyQualifiedName~MappingCompositionTests|FullyQualifiedName~InheritedMappingTests|FullyQualifiedName~ConventionTests|FullyQualifiedName~NamingPolicyTests|FullyQualifiedName~DapperIntegrationTests"`
  - resultado: sucesso, 69 testes aprovados.

Validacao final:

- `dotnet restore`
  - resultado: sucesso.
- `dotnet build`
  - resultado: sucesso, 0 warnings, 0 erros.
- `dotnet test`
  - resultado: sucesso, 106 testes aprovados no core e 7 testes aprovados no Dommel.
- `dotnet build --configuration Release`
  - resultado: sucesso, 0 warnings, 0 erros.
- `dotnet test --configuration Release`
  - resultado: sucesso, 106 testes aprovados no core e 7 testes aprovados no Dommel.
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release`
  - resultado: sucesso, 106 testes aprovados.
- `dotnet build .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-restore`
  - resultado: sucesso, 0 warnings, 0 erros.

`dotnet pack` nao foi executado porque nao houve mudanca de empacotamento, metadados NuGet ou targets.
