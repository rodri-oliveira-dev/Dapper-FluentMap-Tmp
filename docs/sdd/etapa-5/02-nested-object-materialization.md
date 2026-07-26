# 02 - Nested Object Materialization

## Specification

Esta entrega implementa suporte real e opt-in para materializar objetos aninhados mutaveis a partir de mappings baseados em `MemberPath`.

Exemplo suportado:

```csharp
Map(x => x.Address.City).ToColumn("city");

var customer = connection.QueryMappedSingle<Customer>(
    "SELECT 'Sao Paulo' AS city;");
```

Resultado:

```text
Customer
└── Address
    └── City = "Sao Paulo"
```

O caminho regular `Dapper.Query<T>` permanece preservado para mappings simples, constructor mapping simples, conventions, naming policies e fallback do Dapper. Nested materialization nao e prometida implicitamente por `Dapper.Query<T>`.

## Discovery

Arquivos analisados:

- `docs/sdd/etapa-5/01-nested-materialization-spike.md`
- `docs/sdd/etapa-2/01-member-path.md`
- `docs/sdd/etapa-3/02-constructor-immutable-mapping.md`
- `docs/sdd/etapa-4/02-trimming-aot.md`
- `docs/sdd/etapa-4/03-source-generator.md`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/MappingConfigurationValidator.cs`
- `src/Dapper.FluentMap/Mapping/MemberPath.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentConstructorTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentMapTypeMap.cs`
- `src/Dapper.FluentMap/Diagnostics/*`
- testes de integracao com SQLite.

Confirmacao obrigatoria:

```text
01 - Spike nested/value-object -> Concluido
```

## Decision

A arquitetura escolhida no spike foi mantida:

- nested materialization usa API opt-in controlada pelo FluentMap;
- `Dapper.Query<T>` nao tenta materializar grafos aninhados;
- `MemberPath` e a identidade do caminho;
- o plano de materializacao e criado a partir do shape de colunas;
- o plano usa a precedencia efetiva do `MappingRegistry`: explicito, herdado, convention/naming policy e fallback do Dapper para membros raiz.

API adicionada:

```csharp
connection.QueryMapped<TEntity>(sql, param, transaction, commandTimeout, commandType);
connection.QueryMappedSingle<TEntity>(sql, param, transaction, commandTimeout, commandType);
```

## Supported Scope

Suportado nesta entrega:

- objetos aninhados mutaveis;
- um nivel, por exemplo `Customer.Address.City`;
- multiplos niveis, por exemplo `Customer.Address.Country.Name`;
- paths com mesmo terminal, por exemplo `Rank.Level` e `Seniority.Level`;
- mappings explicitos e herdados;
- naming policies e conventions para propriedades raiz;
- fallback tradicional de POCO raiz settable;
- multiplas linhas;
- `Explain` indicando `Materialization = Nested`.

Fora do escopo:

- Value Objects imutaveis aninhados;
- nested records;
- construcao de grafos imutaveis por construtor;
- collections no meio do path;
- indexers, static members e paths readonly;
- materializer gerado.

## Null Semantics

As regras sao por subarvore nested:

- se todos os valores correspondentes a uma subarvore nested forem `NULL`, o objeto intermediario dessa subarvore fica `null`;
- se o root ou um intermediario ja criou esse objeto por construtor/inicializador, o materializer limpa a propriedade para `null` quando ela e settable;
- se pelo menos um valor da subarvore nested nao for `NULL`, o objeto intermediario e criado quando estiver `null`;
- valores leaf `NULL` dentro de uma subarvore criada sao atribuidos como `null` para reference/nullable types ou como default para value types nao anulaveis;
- nulabilidade C# por NRT nao e interpretada nesta entrega, porque o core permanece sem nullable annotations ponta a ponta.

Assim:

```text
city = NULL
```

mantem `Address = null` quando `Address.City` e o unico valor nested.

E:

```text
city = NULL, postal_code = '01000'
```

cria `Address`, define `City = null` e `PostalCode = '01000'`.

## Construction Semantics

Para nested materialization runtime:

- o tipo raiz consultado por `QueryMapped*` deve ter construtor publico sem parametros;
- cada propriedade intermediaria deve ter getter publico, setter publico e tipo com construtor publico sem parametros;
- cada leaf nested deve ser settable;
- objetos intermediarios existentes sao reutilizados quando a subarvore possui dados;
- falhas de construcao sao reportadas como `FluentMapConfigurationException` durante a criacao do plano `QueryMapped*`, antes da materializacao das linhas.

Nao ha reflection para construtores privados nesta entrega.

## Validation

A validacao de configuracao rejeita antecipadamente:

- membro intermediario sem getter/setter publico;
- leaf nested readonly;
- collection no meio do path;
- indexer;
- static member;
- prefix conflict, como mapear `Address` e `Address.City` ao mesmo tempo.

A validacao do plano `QueryMapped*` rejeita, antes de ler linhas, tipo raiz ou intermediario sem construtor publico sem parametros. Essa checagem fica no caminho opt-in anotado para trimming/dynamic-code para preservar o caminho de registro explicito sem warnings FluentMap-owned.

## Performance

O caminho opt-in cria e cacheia um plano por:

```text
tipo raiz + lista ordinal de colunas
```

O plano pre-computa:

- resolucao de mapping por coluna;
- arvore de `MemberPath`;
- delegates de getter/setter;
- factories de construtores sem parametros;
- indices de colunas por subarvore para decidir `NULL` total/parcial.

Por linha, o materializer executa leitura do valor, conversao leve e chamada dos delegates cacheados. Nao ha busca ampla de reflection por coluna por linha.

## AOT And Trimming

`QueryMapped*` e um caminho runtime/reflection-based e compila delegates em tempo de execucao. Por isso, as APIs foram anotadas com:

- `RequiresUnreferencedCode`;
- `RequiresDynamicCode`.

O source generator da Etapa 4 continua limitado a registro de mappings. Um materializer gerado permanece a estrategia futura preferencial para consumidores trimmed/Native AOT.

## Diagnostics

`MemberMappingExplanation` recebeu a propriedade:

```csharp
MappingMaterialization Materialization
```

Valores:

- `Dapper` para mappings raiz e fallback tradicional;
- `Nested` para paths aninhados materializados pelo wrapper opt-in.

Consumidores existentes nao quebram porque a API de diagnostico foi ampliada sem remover membros existentes.

## Delivery

Arquivos adicionados:

- `src/Dapper.FluentMap/Diagnostics/MappingMaterialization.cs`
- `src/Dapper.FluentMap/Materialization/MaterializationPlanCacheKey.cs`
- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`
- `src/Dapper.FluentMap/QueryMappedExtensions.cs`
- `test/Dapper.FluentMap.Tests/NestedObjectMaterializationTests.cs`
- `docs/sdd/etapa-5/02-nested-object-materialization.md`

Arquivos alterados:

- `README.md`
- `src/Dapper.FluentMap/Compatibility/CodeAnalysisAttributes.cs`
- `src/Dapper.FluentMap/Diagnostics/MemberMappingExplanation.cs`
- `src/Dapper.FluentMap/MappingConfigurationValidator.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `test/Dapper.FluentMap.Tests/NestedMaterializationSpikeTests.cs`
- `docs/sdd/etapa-5/decisions.md`
- `docs/sdd/etapa-5/status.md`

## Validation

Validacao localizada executada durante a implementacao:

```text
dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~NestedObjectMaterializationTests"
```

Resultado:

```text
16 testes aprovados
```

Validacao relacionada:

```text
dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~NestedMaterializationSpikeTests|FullyQualifiedName~NestedObjectMaterializationTests|FullyQualifiedName~DiagnosticsApiTests|FullyQualifiedName~ConstructorMappingTests"
```

Resultado:

```text
43 testes aprovados
```

Validacao final completa deve registrar:

```text
dotnet restore
dotnet build
dotnet test
dotnet build --configuration Release
dotnet test --configuration Release
```

Resultado final:

- `dotnet restore`: sucesso;
- `dotnet build`: sucesso, 0 warnings, 0 erros;
- `dotnet test`: sucesso, 150 testes do core, 7 Dommel, 7 analyzer, 12 generator e 1 generated-registration integration;
- `dotnet build --configuration Release`: sucesso, 0 warnings, 0 erros;
- `dotnet test --configuration Release`: sucesso com os mesmos 177 testes.

Smokes AOT/trimming:

- `dotnet run --project .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release`: `explicit:ok`;
- `dotnet run --project .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:DefineConstants=AOT_SMOKE_SCANNING`: `scanning:ok`;
- `dotnet run --project .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:DefineConstants=AOT_SMOKE_GENERATED`: `generated:ok`;
- publish trimmed explicit: sucesso, runtime `explicit:ok`, sem warnings FluentMap-owned; warnings restantes pertencem ao Dapper;
- publish trimmed generated: sucesso, runtime `generated:ok`, sem warnings FluentMap-owned; warnings restantes pertencem ao Dapper;
- publish Native AOT explicit: falhou no ambiente com `Platform linker not found`; runtime Native AOT nao foi validado.

## Limitations For Delivery 3

- Value Objects imutaveis aninhados devem definir construcao por TypeHandler, construtor, factory ou materializer gerado.
- Nested records exigem plano de construtor em vez de setters.
- NRT metadata nao foi usada para diferenciar intermediarios nullable/non-nullable.
- O caminho runtime nao e a estrategia ideal para Native AOT.
- Conversoes cobrem escalares comuns e fallback settable; conversoes complexas devem ser tratadas por entrega dedicada.

## Semantic Commit

Mensagem:

```text
feat: support nested object mappings
```
