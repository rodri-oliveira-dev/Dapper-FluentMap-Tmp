# 04 - Naming Policies

## Specification

Adicionar uma API clara e reutilizavel para transformar nomes de membros em nomes de colunas sem criar um segundo pipeline de conventions.

Casos tratados:

- `CustomerId -> customer_id`;
- `FirstName -> first_name`;
- `Id -> customer_id`;
- `Name -> usr_name`;
- prefix;
- suffix;
- transformacao customizada;
- composicao com mappings explicitos, mappings herdados, conventions e fallback do Dapper.

Fora do objetivo:

- reproduzir apenas `DefaultTypeMap.MatchNamesWithUnderscores`;
- criar dezenas de estilos de nomes;
- introduzir profiles;
- alterar estado global do Dapper como efeito colateral;
- declarar suporte a materializacao aninhada.

## Discovery

Arquivos analisados:

- `AGENTS.md`
- `docs/sdd/etapa-1/README.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-1/04-mapping-registry-cache.md`
- `docs/sdd/etapa-2/README.md`
- `docs/sdd/etapa-2/status.md`
- `docs/sdd/etapa-2/decisions.md`
- `docs/sdd/etapa-2/01-member-path.md`
- `docs/sdd/etapa-2/02-configuration-validation.md`
- `docs/sdd/etapa-2/03-inherited-mappings.md`
- `README.md`
- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `src/Dapper.FluentMap/Configuration/FluentConventionConfiguration.cs`
- `src/Dapper.FluentMap/Conventions/Convention.cs`
- `src/Dapper.FluentMap/Conventions/PropertyConventionConfiguration.cs`
- `src/Dapper.FluentMap/Conventions/ConventionPropertyConfiguration.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/MappingConfigurationValidator.cs`
- testes de composition, inherited mappings, registry e Dapper integration.

Recursos existentes:

- `Convention` ja registra `PropertyMap` por entidade.
- `ConventionPropertyConfiguration.HasPrefix(...)` ja permite prefix simples.
- `ConventionPropertyConfiguration.Transform(Func<string, string>)` ja permite transformacao customizada.
- `ConventionPropertyConfiguration.IsCaseInsensitive()` ja governa comparacao de coluna.
- `FluentConventionConfiguration` ja faz scanning das propriedades, cria `PropertyMap` e chama a validacao.
- `MappingRegistry` ja centraliza explicit mappings, conventions, fallback, cache e instalacao de type maps no Dapper.

Limitacoes encontradas:

- para usar uma transformacao simples, o consumidor precisava criar uma classe `Convention` dedicada;
- nao havia built-in para snake_case;
- nao havia built-in direto para suffix;
- prefix e transformacao existiam, mas nao havia um modelo declarativo e composavel de policy;
- o suporte nativo `DefaultTypeMap.MatchNamesWithUnderscores` e um flag estatico global do Dapper e cobre apenas matching underscore, sem prefix, suffix ou custom transform.

Comportamento nativo do Dapper verificado:

- `DefaultTypeMap.MatchNamesWithUnderscores = false` nao mapeia `customer_id` para `CustomerId` pelo `DefaultTypeMap`;
- `DefaultTypeMap.MatchNamesWithUnderscores = true` passa a mapear `customer_id` para `CustomerId`;
- o flag e global e foi restaurado no teste;
- a nova API nao altera esse flag.

## Decision

Modelo escolhido:

```csharp
public sealed class NamingPolicy
```

com API:

```csharp
NamingPolicy.Identity
NamingPolicy.SnakeCase
NamingPolicy.Prefix(string prefix)
NamingPolicy.Suffix(string suffix)
NamingPolicy.Custom(Func<string, string> transformer)

policy.Then(...)
policy.WithPrefix(...)
policy.WithSuffix(...)
policy.GetColumnName(...)
```

Registro:

```csharp
FluentMapper.Initialize(c =>
{
    c.UseNamingPolicy(NamingPolicy.SnakeCase)
     .ForEntity<Customer>();

    c.UseNamingPolicy(NamingPolicy.SnakeCase.WithPrefix("usr_"))
     .ForEntity<User>();
});
```

Custom:

```csharp
c.UseNamingPolicy(name => "x_" + name.ToLowerInvariant())
 .ForEntity<MyEntity>();
```

Motivos:

- um delegate e suficiente para a execucao;
- uma classe pequena permite built-ins e composicao sem introduzir interface publica prematura;
- `Func<string, string>` preserva o mesmo nivel funcional que a convention atual;
- `MemberPath` nao foi exposto na API porque conventions atuais operam sobre propriedades simples do tipo consultado e a etapa nao implementa materializacao aninhada;
- futuras etapas podem adicionar overload baseado em caminho se houver suporte real ponta a ponta.

Integracao com conventions:

- `UseNamingPolicy(...)` cria uma convention interna (`NamingPolicyConvention`);
- a convention interna usa `Properties().Configure(c => c.Transform(...))`;
- `UseNamingPolicy(...)` retorna `FluentConventionConfiguration`, portanto usa os mesmos `.ForEntity<T>()`, `.ForEntitiesInAssembly(...)` e `.ForEntitiesInCurrentAssembly(...)`;
- nao ha storage global novo fora do `MappingRegistry`;
- nao ha alteracao silenciosa em `DefaultTypeMap.MatchNamesWithUnderscores`.

Built-ins implementados:

- `SnakeCase`;
- `Prefix`;
- `Suffix`;
- `Custom`;
- composicao via `Then`, `WithPrefix` e `WithSuffix`.

Precedencia consolidada:

```text
Mapping explicito do derivado
        |
        v
Mapping explicito herdado mais proximo
        |
        v
Mapping explicito herdado mais distante
        |
        v
Convention / Naming Policy do tipo consultado
        |
        v
Dapper Default
```

Consequencia:

- explicit mapping sempre vence naming policy;
- inherited explicit mapping vence naming policy;
- naming policy e convention ficam no mesmo nivel e seguem a ordem de registro entre conventions;
- fallback do Dapper permanece disponivel quando nada no FluentMap resolve a coluna.

## Delivery

Arquivos adicionados:

- `src/Dapper.FluentMap/Naming/NamingPolicy.cs`
- `src/Dapper.FluentMap/Conventions/NamingPolicyConvention.cs`
- `test/Dapper.FluentMap.Tests/NamingPolicyTests.cs`
- `docs/sdd/etapa-2/04-naming-policies.md`

Arquivos alterados:

- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `docs/sdd/etapa-2/decisions.md`
- `docs/sdd/etapa-2/status.md`
- `README.md`

Implementacao:

- `NamingPolicy` encapsula uma funcao de transformacao imutavel;
- `SnakeCase` transforma PascalCase/camelCase em snake_case com tratamento basico de siglas;
- `Prefix` e `Suffix` adicionam texto antes/depois do nome gerado;
- `Custom` aceita `Func<string, string>`;
- `Then`, `WithPrefix` e `WithSuffix` permitem compor policies;
- `UseNamingPolicy(NamingPolicy, bool caseSensitive = true)` registra a policy;
- `UseNamingPolicy(Func<string, string>, bool caseSensitive = true)` e atalho para custom;
- a convention interna produz `PropertyMap` e passa pelas validacoes existentes.

Cache e performance:

- a transformacao ocorre no momento de `ForEntity(...)`, junto com o mapeamento de convention existente;
- os `PropertyMap` resultantes armazenam o nome de coluna ja transformado;
- a resolucao em runtime continua usando o cache estruturado do `MappingRegistry`;
- a chave de cache nao mudou: tipo, nome de coluna ordinal e estrategia (`FluentMap` ou `ConventionOnly`);
- mudancas feitas por `UseNamingPolicy(...).ForEntity(...)` invalidam o cache do tipo pelo mesmo caminho de `AddConvention`.

## Tests

Testes adicionados cobrem:

- sem policy, preservando fallback do Dapper;
- snake_case;
- prefix composavel;
- suffix composavel;
- transformer customizado;
- explicit mapping maior que policy;
- inherited mapping maior que policy;
- policy junto com convention;
- case sensitivity;
- mesma policy aplicada em tipos diferentes;
- policy invalida retornando coluna nula;
- materializacao real com Dapper e SQLite in-memory;
- confirmacao de que `UseNamingPolicy` nao altera `DefaultTypeMap.MatchNamesWithUnderscores`;
- caracterizacao do comportamento nativo de `DefaultTypeMap.MatchNamesWithUnderscores`.

## Validation

Comandos executados durante a entrega:

- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~NamingPolicyTests"`
  - resultado: sucesso, 14 testes aprovados.
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~MappingCompositionTests|FullyQualifiedName~InheritedMappingTests|FullyQualifiedName~DapperIntegrationTests|FullyQualifiedName~NamingPolicyTests"`
  - resultado: sucesso, 42 testes aprovados.

Validacao final completa registrada apos execucao:

- `dotnet restore`
  - resultado: sucesso.
- `dotnet build`
  - resultado: sucesso, 0 warnings, 0 erros.
- `dotnet test`
  - resultado: sucesso, 91 testes aprovados no core e 7 testes aprovados no Dommel.
- `dotnet build --configuration Release`
  - resultado: sucesso, 0 warnings, 0 erros.
- `dotnet test --configuration Release`
  - resultado: sucesso, 91 testes aprovados no core e 7 testes aprovados no Dommel.

## Encerramento Da Etapa 2

Capacidades estabilizadas:

- identidade interna de membros com `MemberPath`;
- validacao fail-fast e diagnosticos estruturados;
- heranca opt-in de mappings por `IncludeBase<TBase>()`;
- naming policies declarativas e composaveis;
- precedencia consolidada entre mapping explicito, mapping herdado, convention/naming policy e fallback do Dapper.

Dividas transferidas:

- nested object materialization;
- Value Objects complexos;
- constructor/record mapping;
- multiple mapping profiles;
- Roslyn analyzers;
- source generators;
- AOT/trimming.

Pack nao e esperado porque nao houve mudanca de empacotamento, metadados NuGet ou targets.
