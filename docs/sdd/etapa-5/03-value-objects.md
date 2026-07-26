# 03 - Value Objects Imutaveis

## Specification

Esta entrega adiciona suporte opt-in para materializar Value Objects imutaveis e nested immutable objects pelo caminho `QueryMapped<T>` / `QueryMappedSingle<T>`, sem exigir setters publicos e sem contornar invariantes do dominio.

Exemplo suportado:

```csharp
Map(customer => customer.Id).ToColumn("id");
Map(customer => customer.Cpf.Number).ToColumn("cpf");

var customer = connection.QueryMappedSingle<Customer>(
    "SELECT 1 AS id, '12345678909' AS cpf;");
```

O modelo pode expor apenas getters e construtores publicos:

```csharp
public sealed class Customer
{
    public Customer(int id, Cpf cpf)
    {
        Id = id;
        Cpf = cpf;
    }

    public int Id { get; }
    public Cpf Cpf { get; }
}

public sealed class Cpf
{
    public Cpf(string number)
    {
        Number = number;
    }

    public string Number { get; }
}
```

## Discovery

Arquivos analisados:

- `docs/sdd/etapa-5/README.md`
- `docs/sdd/etapa-5/status.md`
- `docs/sdd/etapa-5/decisions.md`
- `docs/sdd/etapa-5/01-nested-materialization-spike.md`
- `docs/sdd/etapa-5/02-nested-object-materialization.md`
- `docs/sdd/etapa-3/02-constructor-immutable-mapping.md`
- `docs/sdd/etapa-4/02-trimming-aot.md`
- `docs/sdd/etapa-4/03-source-generator.md`
- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`
- `src/Dapper.FluentMap/MappingConfigurationValidator.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/Diagnostics/*`
- `test/Dapper.FluentMap.Tests/NestedObjectMaterializationTests.cs`
- `test/Dapper.FluentMap.Tests/NestedMaterializationSpikeTests.cs`
- `test/Dapper.FluentMap.AotSmoke/Program.cs`

Confirmacao obrigatoria:

```text
01 - Spike nested/value-object -> Concluido
02 - Nested object materialization -> Concluido
```

## Decision

### TypeHandler Boundary

Value Objects escalares mapeados como propriedade inteira continuam sendo responsabilidade do Dapper TypeHandler:

```csharp
Map(customer => customer.Cpf).ToColumn("cpf");
SqlMapper.AddTypeHandler(new CpfTypeHandler());
```

Esse caminho permanece ideal para conversoes simples como `string -> Cpf`, inclusive com `Dapper.Query<T>`.

Para paths aninhados, o TypeHandler nao e suficiente:

```csharp
Map(customer => customer.Cpf.Number).ToColumn("cpf");
```

Nesse caso o destino conceitual e o grafo `Customer.Cpf`, nao apenas o membro terminal `Number`. O suporte foi implementado no materializer opt-in do FluentMap.

### Constructor Strategy

O plano runtime agora constroi uma arvore por `MemberPath` e seleciona construtores publicos por nome de parametro:

- parametros sao associados a propriedades mapeadas por nome, case-insensitive;
- `Cpf(string number)` recebe `Cpf.Number`;
- `Money(decimal amount, string currency)` recebe `Money.Amount` e `Money.Currency`;
- `Customer(int id, Cpf cpf)` recebe o valor simples `Id` e o objeto `Cpf` ja materializado;
- objetos mutaveis com construtor publico sem parametros e setters continuam usando o caminho anterior;
- construtores sao pre-computados no plano por tipo + shape de colunas;
- o hot path por linha usa delegates compilados e bindings ja resolvidos.

Factory methods como `Cpf.Create(...)` foram avaliados, mas nao implementados. Uma DSL de factories exigiria uma API publica explicita, forte em tipos e com regras de ambiguidade proprias. Esta entrega manteve somente construtores publicos.

## Supported Scope

Suportado nesta entrega:

- Value Object de um valor, como `Cpf(string number)`;
- record de um valor, como `record Email(string Value)`;
- Value Object com varios componentes, como `Money(decimal amount, string currency)`;
- nested immutable object, como `Customer(Address address)` e `Address(string city)`;
- Value Object nullable por semantica runtime de referencia: subarvore toda `NULL` resulta em `null`;
- dois Value Objects no mesmo tipo;
- paths com mesmo terminal em objetos distintos;
- mappings herdados por `IncludeBase<TBase>()`;
- naming policy para propriedades raiz combinada com nested value object explicito;
- root immutable constructor mapping no caminho `QueryMapped*`;
- TypeHandler quando o destino mapeado e o Value Object inteiro.

Fora do escopo:

- factory methods;
- private constructor;
- private setter via reflection;
- field/backing field injection;
- `FormatterServices`;
- colecoes no meio do path;
- nullability NRT como contrato runtime;
- generated DbDataReader materializer.

## Validation

Configuracoes impossiveis sao rejeitadas com `FluentMapConfigurationException`:

- path com indexer, static member, sem getter publico ou colecao intermediaria;
- propriedade sem setter que nao possa ser associada a parametro de construtor publico;
- tipo sem construtor publico compativel com os membros mapeados;
- parametro de construtor sem coluna/membro correspondente;
- multiplos construtores publicos igualmente validos;
- prefix conflict como `Address` e `Address.City`;
- Value Object que nao pode ser criado pelo conjunto de colunas consultado.

As mensagens incluem o tipo de entidade, `MemberPath`, tipo do Value Object, construtor quando aplicavel e colunas problemáticas.

## Null Semantics

A regra de `NULL` continua por subarvore:

- se todos os valores de uma subarvore Value Object sao `NULL`, o Value Object fica `null`;
- se pelo menos um valor da subarvore nao e `NULL`, o Value Object e construido;
- valores `NULL` para parametros escalares reference/nullable chegam como `null`;
- valores `NULL` para value types nao anulaveis seguem o comportamento ja existente do materializer: default do tipo;
- NRT (`Cpf?`) nao e interpretado como metadata runtime nesta entrega.

## Exceptions

Construtores de dominio continuam sendo a autoridade para invariantes.

Quando um construtor rejeita um valor, a excecao original e preservada como `InnerException` de `FluentMapConfigurationException`, com contexto adicional:

- entidade;
- `MemberPath`;
- tipo do Value Object;
- construtor usado;
- coluna ou colunas envolvidas.

Nao ha silencio de excecao nem criacao sem construtor.

## Diagnostics

`MappingMaterialization` recebeu:

```csharp
ValueObject
```

`Explain<TEntity>()` agora distingue:

- `Dapper` para root mapping regular;
- `Nested` para nested mutable materialization;
- `ValueObject` para paths aninhados que exigem construtor.

Exemplo validado:

```text
Cpf.Number
  Column: cpf
  Source: Explicit
  Materialization: ValueObject
```

## Analyzer And Generator

Analyzer:

- nenhuma regra nova foi adicionada, porque construtores/factories e cobertura de colunas dependem do shape runtime da query;
- os testes existentes de analyzer foram preservados para garantir que nested/value-object member expressions continuam validas;
- regras estaticamente comprovaveis existentes (`DFM001` a `DFM005`) permanecem.

Source generator:

- nao foi transformado em materializer;
- o generator continua limitado a registro de maps;
- o smoke AOT recebeu um mapping de Value Object e valida `Explain` com `Materialization = ValueObject`;
- materializer gerado permanece estrategia futura para consumidores Native AOT/trimming que nao queiram usar `QueryMapped*` reflection-based.

## AOT And Performance

`QueryMapped*` continua anotado com:

- `RequiresUnreferencedCode`;
- `RequiresDynamicCode`.

Motivo: o caminho runtime usa reflection metadata e expression compilation para getters, setters, construtores e TypeHandler binding.

Mitigacoes implementadas:

- plano cacheado por tipo raiz + lista ordinal de colunas;
- selecao de construtor feita uma vez por plano;
- delegates de construtor/getter/setter/conversor pre-computados;
- sem lookup de construtor por row;
- sem expression tree criada por row.

## Delivery

Arquivos adicionados:

- `test/Dapper.FluentMap.Tests/ValueObjectMaterializationTests.cs`
- `docs/sdd/etapa-5/03-value-objects.md`

Arquivos alterados:

- `README.md`
- `src/Dapper.FluentMap/Diagnostics/MappingMaterialization.cs`
- `src/Dapper.FluentMap/MappingConfigurationValidator.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`
- `src/Dapper.FluentMap/QueryMappedExtensions.cs`
- `test/Dapper.FluentMap.Tests/NestedMaterializationSpikeTests.cs`
- `test/Dapper.FluentMap.Tests/NestedObjectMaterializationTests.cs`
- `test/Dapper.FluentMap.AotSmoke/Program.cs`
- `docs/sdd/etapa-5/decisions.md`
- `docs/sdd/etapa-5/status.md`

Nao foram alterados:

- Dommel;
- targets;
- metadados NuGet;
- pacote do analyzer;
- source generator runtime.

## Validation

Validacao localizada executada durante a implementacao:

```text
dotnet build .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Debug
dotnet build .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Debug
dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~ValueObjectMaterializationTests"
dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~ValueObjectMaterializationTests|FullyQualifiedName~NestedObjectMaterializationTests|FullyQualifiedName~NestedMaterializationSpikeTests|FullyQualifiedName~ConstructorMappingTests|FullyQualifiedName~DiagnosticsApiTests"
```

Resultados locais:

- build do core: sucesso, 0 warnings, 0 erros;
- build dos testes do core: sucesso, 0 warnings, 0 erros;
- `ValueObjectMaterializationTests`: sucesso, 16 testes aprovados;
- conjunto relacionado: sucesso, 60 testes aprovados.

Validacao final completa registrada ao concluir a entrega:

```text
dotnet restore
dotnet build
dotnet test
dotnet build --configuration Release
dotnet test --configuration Release
dotnet pack .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-build --output .\artifacts\packages
```

Resultado final:

- `dotnet restore`: sucesso;
- `dotnet build`: sucesso, 0 warnings, 0 erros;
- `dotnet test`: sucesso, 166 testes do core, 7 Dommel, 7 analyzer, 12 generator e 1 generated-registration integration;
- `dotnet build --configuration Release`: sucesso, 0 warnings, 0 erros;
- `dotnet test --configuration Release`: sucesso com os mesmos 193 testes totais;
- `dotnet pack`: pacote `Dapper.FluentMap.2.0.0.nupkg` criado; warning legado `NU5125` sobre `PackageLicenseUrl`.

Inspecao do pacote:

- contem `lib/netstandard2.0/Dapper.FluentMap.dll`;
- contem `lib/netstandard2.0/Dapper.FluentMap.xml`;
- nao contem projetos de teste nem artefatos indevidos.

Smokes especificos registrados ao concluir a entrega:

```text
dotnet test .\test\Dapper.FluentMap.Analyzers.Tests\Dapper.FluentMap.Analyzers.Tests.csproj
dotnet test .\test\Dapper.FluentMap.Generators.Tests\Dapper.FluentMap.Generators.Tests.csproj
dotnet test .\test\Dapper.FluentMap.GeneratedRegistration.Tests\Dapper.FluentMap.GeneratedRegistration.Tests.csproj
dotnet run --project .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release
dotnet run --project .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:DefineConstants=AOT_SMOKE_GENERATED
dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishTrimmed=true -p:TrimMode=full -p:PublishAot=false -p:TrimmerSingleWarn=false -p:ILLinkTreatWarningsAsErrors=false
dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishTrimmed=true -p:TrimMode=full -p:PublishAot=false -p:DefineConstants=AOT_SMOKE_GENERATED -p:TrimmerSingleWarn=false -p:ILLinkTreatWarningsAsErrors=false
dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishAot=true -p:TrimmerSingleWarn=false -p:ILLinkTreatWarningsAsErrors=false -p:MSBuildWarningsAsMessages=
```

Resultados:

- testes de analyzer: sucesso, 7 testes aprovados;
- testes de generator: sucesso, 12 testes aprovados;
- generated-registration integration: sucesso, 1 teste aprovado;
- AOT smoke explicit: `explicit:ok`;
- AOT smoke generated: `generated:ok`;
- publish trimmed explicit: sucesso, runtime `explicit:ok`, sem warnings FluentMap-owned; warnings restantes pertencem ao Dapper;
- publish trimmed generated: sucesso, runtime `generated:ok`, sem warnings FluentMap-owned; warnings restantes pertencem ao Dapper;
- publish Native AOT explicit: falhou no ambiente com `Platform linker not found`; runtime Native AOT nao foi validado.

## Semantic Commit

Mensagem:

```text
feat: support immutable value object mappings
```
