# 03 - Validate E Explain

## Specification

Consolidar diagnosticos de configuracao para o `Dapper.FluentMap` apos a introducao de registro moderno, mappings herdados, naming policies e constructor mapping.

Objetivos tratados:

- expor validacao publica para o estado global atual;
- agregar erros quando o estado configurado contem mais de uma falha;
- explicar mappings efetivos por entidade sem retornar apenas texto;
- representar origem do mapping de forma estruturada;
- incluir explicit mapping, inherited mapping, convention, naming policy, fallback do Dapper e constructor parameter;
- preservar caches, registry e type maps do Dapper sem side effects de diagnostico.

Fora do objetivo:

- logging;
- acesso a banco;
- I/O;
- alteracao de comportamento de materializacao;
- sistema de profiles;
- diagnostico query-specific.

## Discovery

Arquivos analisados:

- `AGENTS.md`
- `.agents/skills/run-tests/SKILL.md`
- `docs/sdd/etapa-1/README.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-2/README.md`
- `docs/sdd/etapa-2/decisions.md`
- `docs/sdd/etapa-2/02-configuration-validation.md`
- `docs/sdd/etapa-3/README.md`
- `docs/sdd/etapa-3/status.md`
- `docs/sdd/etapa-3/decisions.md`
- `docs/sdd/etapa-3/01-mapping-registration.md`
- `docs/sdd/etapa-3/02-constructor-immutable-mapping.md`
- `src/Dapper.FluentMap/FluentMapper.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/MappingConfigurationValidator.cs`
- `src/Dapper.FluentMap/Mapping/EntityMap.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMap.cs`
- `src/Dapper.FluentMap/Mapping/MemberPath.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMapIdentity.cs`
- `src/Dapper.FluentMap/Conventions/*`
- `src/Dapper.FluentMap/TypeMaps/*`
- testes existentes de validacao, composicao, inheritance, naming policies e constructor mapping.

Estado anterior da validacao:

- a Etapa 2 criou `FluentMapConfigurationException` e `MappingConfigurationValidator`;
- a validacao era fail-fast durante construcao ou registro;
- a decisao documentada na Etapa 2 foi nao expor `Validate()`, porque ainda nao havia modelo agregado ou API de diagnostico;
- nenhuma API `Explain<T>()` existia.

Pipeline mapeado:

```text
FluentMapper.Initialize
↓
FluentMapConfiguration
↓
MappingRegistry
↓
MappingConfigurationValidator
↓
entity maps + included base maps + conventions/naming policies
↓
FluentMapTypeMap
↓
FluentConstructorTypeMap + CustomPropertyTypeMap + DefaultTypeMap
↓
constructor parameter/property member/fallback
```

Informacoes disponiveis:

- `EntityMaps` guarda entity maps registrados por entidade;
- `IEntityMapWithIncludedBaseTypes` preserva bases incluidas;
- `PropertyMapIdentity` preserva `MemberPath` completo;
- conventions e naming policies produzem `PropertyMap` no registro;
- `NamingPolicyConvention` permite distinguir naming policy de convention comum;
- constructor mapping ja possui algoritmo interno para associar propriedade simples a parametro;
- fallback do Dapper pode ser explicado por propriedades publicas simples que nao possuem mapping efetivo do FluentMap.

Informacao descartada:

- a resolucao hot path retornava apenas `IPropertyMap`/`PropertyInfo`, sem provenance;
- a provenance pode ser reconstruida sem duplicar estado usando a composicao existente do registry.

## Decision

### Validate

`FluentMapper.Validate()` foi exposto como API publica.

Contrato:

- valida o estado global atual de `FluentMapper`;
- retorna `void`;
- lanca `FluentMapConfigurationException` quando encontra erros;
- agrega mensagens de mais de uma falha quando o estado atual contem multiplos problemas;
- e idempotente;
- nao altera registry, caches, conventions, entity maps ou type maps do Dapper;
- nao faz I/O, logging ou acesso a banco.

Motivo para mudar a decisao da Etapa 2:

- apos as entregas de registro moderno, inheritance, naming policies e constructor mapping, ha mais fontes de configuracao coexistindo;
- o diagnostico agregado agora tem utilidade observavel para tooling, testes de startup e auditoria de configuracao;
- a API permanece pequena e reaproveita as regras ja existentes de validacao.

### Explain

`FluentMapper.Explain<TEntity>()` foi exposto como API publica.

Contrato:

- retorna `MappingExplanation`;
- funciona antes ou depois de `Initialize`;
- para entidade sem FluentMap registrado, retorna fallback do Dapper para propriedades publicas simples e diagnostico textual auxiliar;
- nao cria mappings;
- nao instala type maps;
- nao invalida caches;
- nao consulta banco;
- produz snapshots read-only.

Modelo publico:

```csharp
MappingExplanation
{
    EntityType,
    EntityMapType,
    ConventionTypes,
    Members,
    Diagnostics
}

MemberMappingExplanation
{
    MemberPath,
    PropertyInfo,
    ColumnName,
    Source,
    CaseSensitive,
    Ignored,
    InheritedFrom,
    ConventionType,
    ConstructorParameters
}

ConstructorParameterExplanation
{
    Constructor,
    Name,
    ParameterType
}
```

Provenance publica:

```text
Explicit
Inherited
Convention
NamingPolicy
DapperDefault
```

Constructor parameters nao foram modelados como source. Eles sao destino adicional associado a um mapping simples, preservando a distincao entre origem do mapping e destino de materializacao.

## Delivery

Arquivos adicionados:

- `src/Dapper.FluentMap/Diagnostics/MappingSource.cs`
- `src/Dapper.FluentMap/Diagnostics/MappingExplanation.cs`
- `src/Dapper.FluentMap/Diagnostics/MemberMappingExplanation.cs`
- `src/Dapper.FluentMap/Diagnostics/ConstructorParameterExplanation.cs`
- `test/Dapper.FluentMap.Tests/DiagnosticsApiTests.cs`
- `docs/sdd/etapa-3/03-diagnostics-api.md`

Arquivos alterados:

- `src/Dapper.FluentMap/FluentMapper.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentConstructorTypeMap.cs`
- `docs/sdd/etapa-3/README.md`
- `docs/sdd/etapa-3/status.md`
- `docs/sdd/etapa-3/decisions.md`

APIs publicas adicionadas:

```csharp
FluentMapper.Validate();
FluentMapper.Explain<TEntity>();
```

Tipos publicos adicionados:

```text
Dapper.FluentMap.Diagnostics.MappingSource
Dapper.FluentMap.Diagnostics.MappingExplanation
Dapper.FluentMap.Diagnostics.MemberMappingExplanation
Dapper.FluentMap.Diagnostics.ConstructorParameterExplanation
```

Exemplo conceitual:

```text
Id
  Column: customer_id
  Source: Explicit

Name
  Column: customer_name
  Source: NamingPolicy
  Constructor parameter: name

CreatedAt
  Column: CreatedAt
  Source: DapperDefault
```

Implementacao:

- `Validate()` chama o registry e reexecuta as validacoes existentes sobre maps, bases incluidas, composicao efetiva e conventions;
- erros encontrados em estado global ja corrompido por mutabilidade legada dos dicionarios sao agregados;
- `Explain<TEntity>()` deriva provenance a partir de entity maps, included base maps, conventions e naming policies ja registrados;
- mappings herdados preservam o tipo base que declarou o mapping;
- naming policies sao distinguidas pela convention interna `NamingPolicyConvention`;
- fallback do Dapper e representado por propriedades publicas simples sem mapping efetivo no snapshot;
- constructor parameters sao detectados apenas para mappings simples e nao ignorados;
- nested `MemberPath` aparece no diagnostico, mas nao e tratado como constructor parameter.

## Compatibility

Compatibilidade preservada:

- nenhuma API publica existente foi removida;
- `FluentMapper.Initialize` manteve comportamento;
- `EntityMaps` e `TypeConventions` permanecem publicos por compatibilidade;
- validacoes fail-fast existentes continuam ocorrendo no registro;
- constructor mapping da Entrega 02 nao mudou o contrato de materializacao;
- Dommel nao recebeu alteracao funcional.

Comportamento publico novo:

- consumidores podem chamar `FluentMapper.Validate()` para validar o estado global atual;
- consumidores podem chamar `FluentMapper.Explain<TEntity>()` para obter diagnostico estruturado.

## Tests

Testes adicionados cobrem:

- `Validate()` com configuracao valida;
- configuracao invalida;
- multiplos erros agregados;
- chamada repetida;
- ausencia de side effects em cache/registry;
- explicit mapping;
- inherited mapping;
- convention;
- naming policy;
- Dapper default fallback;
- constructor parameter;
- entidade sem mapping;
- paths distintos com mesmo terminal;
- metadata read-only;
- chamada repetida de `Explain()` consistente.

## Validation

Validacao localizada executada:

- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~DiagnosticsApiTests"`
  - resultado: sucesso, 11 testes aprovados.
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~DiagnosticsApiTests|FullyQualifiedName~ConfigurationValidationTests|FullyQualifiedName~MappingCompositionTests|FullyQualifiedName~InheritedMappingTests|FullyQualifiedName~NamingPolicyTests|FullyQualifiedName~ConstructorMappingTests"`
  - resultado: sucesso, 68 testes aprovados.

Validacao final:

- `dotnet restore`
  - resultado: sucesso.
- `dotnet build`
  - resultado: sucesso, 0 warnings, 0 erros.
- `dotnet test`
  - resultado: sucesso, 128 testes aprovados no core e 7 testes aprovados no Dommel.
- `dotnet build --configuration Release`
  - resultado: sucesso, 0 warnings, 0 erros.
- `dotnet test --configuration Release`
  - resultado: sucesso, 128 testes aprovados no core e 7 testes aprovados no Dommel.

`dotnet pack` nao foi executado porque nao houve mudanca de empacotamento, metadados NuGet ou targets.

## Limitacoes

- `Explain<TEntity>()` e um snapshot por entidade, nao por consulta SQL especifica.
- Fallback do Dapper e representado de forma conservadora por propriedades publicas simples sem mapping efetivo.
- Ambiguidade de constructor overload continua sendo responsabilidade do Dapper.
- Mensagens agregadas de `Validate()` sao diagnosticas; o contrato estavel e o tipo de excecao e a agregacao, nao texto exato.
- Nao ha cache adicional para diagnosticos.

## Dividas Fora Do Escopo

- Roslyn analyzers
- Source generator
- AOT/trimming completo
- Nested object materialization
- Value Objects complexos
- Multiple mapping profiles por tipo
- Query-specific mapping

## Semantic Commit

Mensagem planejada:

```text
feat: add mapping diagnostics API
```
