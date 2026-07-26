# 03 - Dapper Compatibility Adapters

Status: COMPLETED

## Specification

Esta entrega isola os pontos em que o FluentMap dependia de detalhes frageis do Dapper, sem alterar o comportamento publico de mappings e sem reimplementar o Dapper.

Os riscos tratados sao:

- `FM-RISK-007`: acesso reflexivo a `SqlMapper.TypeHandlerCache<T>.Parse`;
- `FM-RISK-012`: sentinel `IgnoredPropertyInfo` com membros que lancavam `NotImplementedException`.

## Current Dapper Integration Points

O core ainda integra com Dapper por superficies publicas:

- `SqlMapper.SetTypeMap(type, typeMap)` para instalar o type map default por entidade;
- `SqlMapper.GetTypeMap(type)` em testes e consumidores que inspecionam o estado do Dapper;
- `SqlMapper.ITypeMap` para constructor mapping, member mapping e fallback;
- `SqlMapper.IMemberMap` para expor property/field/parameter ao materializer do Dapper;
- `SqlMapper.HasTypeHandler(type)` para detectar handler registrado;
- `SqlMapper.TypeHandler<T>` para handlers de consumidores.

A versao fixada no core permanece:

```text
Dapper 2.1.79
```

Na versao atual nao foi encontrada API publica do Dapper que converta um `object` usando o TypeHandler registrado para um tipo arbitrario. As APIs publicas relacionadas sao registro/reset de handlers, `HasTypeHandler`, type maps, row parsers e parsers baseados em `IDataReader`.

## Version-Sensitive Areas

As areas sensiveis a upgrade de Dapper sao:

- assinatura e comportamento de `SqlMapper.ITypeMap`;
- assinatura e comportamento de `SqlMapper.IMemberMap`;
- comportamento de fallback quando um mapper retorna `null`;
- constructor mapping delegado a `DefaultTypeMap`;
- existencia do nested type `SqlMapper.TypeHandlerCache<T>`;
- existencia do metodo publico static `TypeHandlerCache<T>.Parse(object)`;
- semantica de `SqlMapper.SetTypeMap`, que continua global por processo.

## TypeHandler Problem

`QueryMapped*` controla seu proprio loop de `DbDataReader`, portanto nao passa pelo materializer interno do Dapper. Para preservar Value Objects escalares, o materializer precisa respeitar handlers registrados com Dapper.

O caminho anterior fazia isso dentro de `NestedMaterializationPlan`:

```text
SqlMapper.HasTypeHandler
typeof(SqlMapper).GetNestedType("TypeHandlerCache`1")
MakeGenericType
GetMethod("Parse")
Expression.Call(Parse)
```

Esse acoplamento estava concentrado em uma funcao, mas ainda fazia parte do materializer e podia falhar silenciosamente retornando ao conversor padrao quando a shape interna do Dapper mudasse.

## Ignored Member Problem

O caminho anterior usava `IgnoredPropertyInfo : PropertyInfo` para bloquear fallback do Dapper em duas situacoes:

- propriedade explicitamente ignorada;
- path nested que nao deve ser tratado como propriedade simples pelo `Dapper.Query<T>`.

O sentinel existia porque `CustomPropertyTypeMap` aceita apenas uma funcao que retorna `PropertyInfo`. Para impedir fallback, era necessario retornar algo nao nulo que o `MultiTypeMap` pudesse reconhecer.

O problema era a fragilidade: quase todos os membros de `IgnoredPropertyInfo` lancavam `NotImplementedException`. Se Dapper ou outro mapper inspecionasse o `PropertyInfo` antes da interceptacao do FluentMap, a falha escaparia de forma pouco diagnosticavel.

## Goals

- Criar uma fronteira interna explicita para detalhes de compatibilidade com Dapper.
- Centralizar reflection residual para TypeHandlers.
- Falhar com diagnostico claro se a shape interna esperada do Dapper deixar de existir.
- Remover o sentinel `PropertyInfo` com `NotImplementedException`.
- Preservar precedencia efetiva: explicito, convention/naming policy, fallback do Dapper.
- Preservar `Dapper.Query<T>` para mappings simples e `QueryMapped*` para materializacao controlada.
- Cobrir os caminhos com testes direcionados.

## Non-Goals

- Atualizar Dapper.
- Copiar internals do Dapper para o FluentMap.
- Criar interfaces publicas.
- Substituir `SqlMapper.SetTypeMap`.
- Fazer profiles funcionarem em `Dapper.Query<T>`.
- Implementar materializer gerado.
- Alterar Dommel.

## Compatibility Boundary

A fronteira escolhida fica no namespace interno `Dapper.FluentMap.Compatibility`:

```text
FluentMap materialization/type maps
        |
        v
internal Dapper compatibility boundary
        |
        v
Dapper-specific behavior
```

Componentes adicionados:

- `DapperTypeHandlerAdapter`: unico ponto de reflection para `SqlMapper.TypeHandlerCache<T>.Parse(object)`;
- `DapperFluentPropertyTypeMap`: `ITypeMap` interno que resolve `IPropertyMap` sem passar por `CustomPropertyTypeMap`;
- `DapperPropertyMemberMap`: `IMemberMap` seguro para propriedades simples;
- `DapperIgnoredMemberMap`: `IMemberMap` seguro para ignored/nested e reconhecido pelo `MultiTypeMap`.

## Proposed Design

### TypeHandler

`NestedMaterializationPlan` passa a delegar a decisao e a construcao do conversor para `DapperTypeHandlerAdapter`.

O adapter:

- usa `SqlMapper.HasTypeHandler` como superficie publica de deteccao;
- usa reflection residual apenas para localizar `TypeHandlerCache<T>.Parse(object)`;
- considera `Nullable<T>` separando tipo declarado e tipo do handler;
- retorna `null` para `DBNull` quando o destino declarado aceita null;
- lanca `FluentMapConfigurationException` quando a shape esperada do Dapper nao existe.

### Ignored Member

`FluentMapTypeMap`, `FluentConventionTypeMap` e o type map interno nao usam mais `CustomPropertyTypeMap` para propriedades FluentMap. Eles usam `DapperFluentPropertyTypeMap`, que pode retornar diretamente um `IMemberMap`.

Quando um mapping e ignored ou nested:

```text
DapperFluentPropertyTypeMap.GetMember(column)
        -> DapperIgnoredMemberMap
        -> MultiTypeMap reconhece o marker
        -> retorna null sem consultar DefaultTypeMap
```

Assim o fallback do Dapper continua bloqueado, mas nenhum `PropertyInfo` falso ou lancador e exposto.

## Alternatives Rejected

### Keep IgnoredPropertyInfo

Rejeitada. Preservaria comportamento, mas manteria o risco de `NotImplementedException` se o sentinel fosse inspecionado.

### Return null for ignored/nested directly

Rejeitada. Isso permitiria que `MultiTypeMap` continuasse para `DefaultTypeMap`, fazendo propriedades ignoradas ou paths nested com mesmo nome de coluna serem materializados pelo Dapper.

### Copy Dapper TypeHandler internals

Rejeitada. A entrega e sobre boundary de compatibilidade, nao fork ou copia de implementacao.

### Upgrade Dapper

Rejeitada nesta entrega. Nao ha specification de dependency upgrade, e a versao `2.1.79` permanece a referencia validada.

## Failure Behavior

Se o TypeHandler estiver registrado, mas o adapter nao conseguir resolver `SqlMapper.TypeHandlerCache<T>.Parse(object)`, o FluentMap deve lancar `FluentMapConfigurationException` com:

- tipo de destino;
- mencao explicita ao boundary de TypeHandler;
- orientacao para revisar compatibilidade antes de atualizar Dapper.

Falha diagnosticavel foi escolhida em vez de fallback silencioso, porque fallback para `Convert.ChangeType` pode materializar valor errado ou mascarar uma quebra de upgrade.

## Acceptance Criteria

- `DapperTypeHandlerAdapter` centraliza reflection para TypeHandlers.
- `NestedMaterializationPlan` nao chama `GetNestedType`, `MakeGenericType` ou `GetMethod` para TypeHandler.
- TypeHandler registrado e usado por `QueryMapped*`.
- `Nullable<T>` com handler registrado preserva `null` para `DBNull`.
- Sem handler, conversao padrao existente continua funcionando.
- Falha de shape interna do Dapper e diagnosticavel.
- `IgnoredPropertyInfo` e removido.
- Ignored root property bloqueia fallback do Dapper.
- Ignored nested path bloqueia fallback para propriedade raiz homonima.
- Fallback default do Dapper continua funcionando para colunas nao configuradas.
- Testes de Etapa 5 continuam passando.

## Residual Risks

- `FM-RISK-007` permanece `MITIGATED`, nao `RESOLVED`: ainda existe reflection para `SqlMapper.TypeHandlerCache<T>.Parse(object)`, mas ela esta isolada e coberta por testes.
- `SqlMapper.SetTypeMap` permanece estado global do Dapper.
- `QueryMapped*` continua runtime/reflection/dynamic-code based.
- Upgrades futuros de Dapper ainda exigem checklist especifico para `ITypeMap`, `IMemberMap`, constructor mapping e TypeHandlers.

## Implementation

Arquivos adicionados:

- `src/Dapper.FluentMap/Compatibility/DapperTypeHandlerAdapter.cs`;
- `src/Dapper.FluentMap/Compatibility/DapperFluentPropertyTypeMap.cs`;
- `src/Dapper.FluentMap/Compatibility/DapperPropertyMemberMap.cs`;
- `src/Dapper.FluentMap/Compatibility/DapperIgnoredMemberMap.cs`;
- `test/Dapper.FluentMap.Tests/DapperCompatibilityAdapterTests.cs`.

Arquivos alterados:

- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`;
- `src/Dapper.FluentMap/TypeMaps/FluentTypeMap.cs`;
- `src/Dapper.FluentMap/TypeMaps/FluentMapTypeMap.cs`;
- `src/Dapper.FluentMap/TypeMaps/FluentConventionTypeMap.cs`;
- `src/Dapper.FluentMap/TypeMaps/MultiTypeMap.cs`;
- `src/Dapper.FluentMap/MappingRegistry.cs`.

Arquivo removido:

- `src/Dapper.FluentMap/TypeMaps/IgnoredPropertyInfo.cs`.

## Validation Results

Environment:

- SDK: `10.0.302`
- test runner detected: VSTest with xUnit v3
- core target: `netstandard2.0`
- test target: `net10.0`
- Dapper: `2.1.79`

Localized validation:

```text
dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Debug --filter "FullyQualifiedName~DapperCompatibilityAdapterTests"
```

Result:

- success;
- 8 tests passed.

Related validation:

```text
dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Debug --filter "FullyQualifiedName~DapperCompatibilityAdapterTests|FullyQualifiedName~ValueObjectMaterializationTests|FullyQualifiedName~NestedMaterializationSpikeTests|FullyQualifiedName~NestedObjectMaterializationTests|FullyQualifiedName~ConstructorMappingTests|FullyQualifiedName~DapperIntegrationTests|FullyQualifiedName~MappingProfileTests"
dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~DapperCompatibilityAdapterTests|FullyQualifiedName~ValueObjectMaterializationTests|FullyQualifiedName~NestedMaterializationSpikeTests|FullyQualifiedName~NestedObjectMaterializationTests|FullyQualifiedName~ConstructorMappingTests|FullyQualifiedName~DapperIntegrationTests|FullyQualifiedName~MappingProfileTests"
```

Results:

- Debug related tests: success, 79 tests passed;
- Release related tests: success, 79 tests passed.

Mandatory validation:

```text
dotnet restore .\Dapper.FluentMap.sln
dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build
dotnet pack .\Dapper.FluentMap.sln --configuration Release --no-build --output .\artifacts\packages
```

Results:

- restore: success;
- build: success, 0 warnings, 0 errors;
- tests: success, 229 total tests passed:
  - core: 198;
  - Dommel: 7;
  - analyzers: 9;
  - generators: 14;
  - generated-registration integration: 1;
- pack: success:
  - `Dapper.FluentMap.2.0.0.nupkg`;
  - `Dapper.FluentMap.Dommel.2.0.0.nupkg`;
  - `Dapper.FluentMap.Analyzers.2.0.0.nupkg`;
  - `Dapper.FluentMap.Generators.2.0.0.nupkg`.

Known pack warnings:

- `NU5125` for legacy `PackageLicenseUrl` in core and Dommel;
- NuGet README recommendation for core and Dommel.

These warnings are pre-existing package metadata debt tracked outside this delivery.
