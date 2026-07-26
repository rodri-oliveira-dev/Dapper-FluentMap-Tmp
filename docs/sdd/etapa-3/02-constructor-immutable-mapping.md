# 02 - Constructor Mapping E Imutaveis

## Specification

Esta entrega melhora a integracao do FluentMap com constructor mapping do Dapper para modelos com construtores parametrizados, propriedades somente leitura, propriedades `init`, records e classes imutaveis.

Objetivos tratados:

- permitir que `Map(e => e.Name).ToColumn("full_name")` influencie parametros de construtor correspondentes;
- aplicar mappings explicitos, mappings herdados, conventions e naming policies tambem na selecao de construtor;
- preservar fallback do `DefaultTypeMap` quando nao houver configuracao relevante do FluentMap;
- manter a precedencia consolidada: mapping explicito do derivado -> mapping explicito herdado -> convention/naming policy -> Dapper default;
- validar materializacao real por `Dapper.QuerySingle<T>` com SQLite in-memory.

Fora do objetivo:

- criar materializador concorrente ao Dapper;
- gerar IL proprio;
- criar object factory;
- adicionar DSL publica `MapConstructor(...)`;
- implementar nested object materialization ou Value Objects.

## Discovery

Arquivos analisados:

- `AGENTS.md`
- `.agents/skills/run-tests/SKILL.md`
- `docs/sdd/etapa-1/README.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-2/README.md`
- `docs/sdd/etapa-2/decisions.md`
- `docs/sdd/etapa-3/README.md`
- `docs/sdd/etapa-3/status.md`
- `docs/sdd/etapa-3/decisions.md`
- `docs/sdd/etapa-3/01-mapping-registration.md`
- `src/Dapper.FluentMap/TypeMaps/MultiTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentConventionTypeMap.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/Mapping/MemberPath.cs`
- testes de integracao, composition, inheritance e naming policies.

Contratos do Dapper 2.1.79 analisados:

- `SqlMapper.ITypeMap.FindConstructor(string[] names, Type[] types)`;
- `SqlMapper.ITypeMap.GetConstructorParameter(ConstructorInfo constructor, string columnName)`;
- `SqlMapper.IMemberMap.Parameter`;
- `DefaultTypeMap`;
- `CustomPropertyTypeMap`.

Comportamento encontrado:

- `FluentMapTypeMap<TEntity>` e `FluentConventionTypeMap<TEntity>` eram compostos por `CustomPropertyTypeMap` e `DefaultTypeMap`.
- `CustomPropertyTypeMap` resolve propriedades, mas nao fornece constructor mapping.
- `MultiTypeMap.FindConstructor` acabava delegando ao `DefaultTypeMap`.
- `MultiTypeMap.GetConstructorParameter` tambem dependia do `DefaultTypeMap`, mas o `CustomPropertyTypeMap` do Dapper 2.1.79 pode lancar `NotSupportedException` nesse metodo.
- `DefaultTypeMap.FindConstructor` seleciona construtor por nomes e tipos de colunas na ordem recebida do reader; construtor sem parametros vence cedo; construtores parametrizados precisam ter a mesma quantidade de parametros que as colunas consideradas.
- `DefaultTypeMap.GetConstructorParameter` associa coluna a parametro por nome do parametro, com matching case-insensitive e suporte ao flag global `MatchNamesWithUnderscores`.
- Mappings explicitos, herdados, conventions e naming policies do FluentMap ja influenciavam `GetMember`, mas nao os nomes usados por `FindConstructor`.
- Records posicionais e classes imutaveis falhavam quando a coluna configurada nao tinha o mesmo nome do parametro do construtor.
- Com SQLite, colunas inteiras sao expostas como `Int64`; para colunas mapeadas pelo FluentMap, a entrega usa o tipo da propriedade mapeada na chamada ao `DefaultTypeMap.FindConstructor`, preservando a conversao final do Dapper.

Caracterizacao antes da alteracao:

- `TraditionalPocoShouldContinueMaterializingConfiguredColumn` passava.
- `ParameterlessConstructorShouldContinueUsingSettableProperties` passava.
- `NestedMemberPathMappingShouldNotActAsConstructorParameterMapping` passava como falha esperada de materializacao.
- Falhavam records, classes imutaveis, explicit mappings para parametros, naming policy, convention, multiplos construtores, casing diferente, fallback parcial e inheritance, sempre porque o Dapper via nomes crus como `person_id` e `full_name`.

## Decision

A lacuna pertence ao FluentMap apenas na traducao de metadata:

- coluna recebida do reader;
- propriedade simples configurada pelo FluentMap;
- nome e tipo que o `DefaultTypeMap` deve enxergar para escolher o construtor;
- `ParameterInfo` que o Dapper deve receber por `IMemberMap.Parameter`.

A materializacao continua pertencendo ao Dapper.

Estrategia:

- adicionar um type map interno `FluentConstructorTypeMap`;
- inserir esse mapper antes de `CustomPropertyTypeMap` e antes de `DefaultTypeMap`;
- quando uma coluna resolve para um `IPropertyMap` simples e nao ignorado, chamar `DefaultTypeMap.FindConstructor` com nome e tipo da propriedade;
- quando uma coluna nao possui mapping simples, manter nome e tipo originais e deixar o `DefaultTypeMap` atuar como fallback;
- implementar um `IMemberMap` interno apenas para expor `ParameterInfo`;
- preservar `GetMember` existente para propriedades settable.

Precedencia:

1. mapping explicito do derivado;
2. mapping explicito herdado mais proximo;
3. demais mappings herdados;
4. convention/naming policy;
5. fallback do `DefaultTypeMap`.

Inheritance:

- constructor mapping usa a composicao efetiva ja existente no `MappingRegistry`;
- `IncludeBase<TBase>()` continua opt-in;
- mappings herdados podem traduzir colunas para parametros do construtor do tipo derivado quando o parametro corresponde a propriedade simples herdada.

Conflitos e ambiguidades:

- conflitos de coluna dentro de entity map e convention continuam falhando cedo pelas validacoes da Etapa 2;
- ambiguidades de constructor overload continuam sob responsabilidade do algoritmo do Dapper;
- nenhum erro novo de ambiguidade de construtor foi criado nesta entrega.

MemberPath:

- constructor parameter nao e representado como `MemberPath`;
- somente mappings cujo `MemberPath` nao e aninhado participam do constructor mapping;
- mapping como `Map(e => e.Rank.Level).ToColumn("rank_level")` nao e usado para preencher parametro `level` do construtor raiz;
- nested object materialization e Value Objects permanecem fora do contrato.

Records e `init`:

- records posicionais funcionam porque seus parametros correspondem a propriedades simples;
- propriedades `init` continuam sendo tratadas pelo Dapper conforme seu proprio suporte a setter/constructor;
- esta entrega nao adiciona API nem regra especial para `init`.

Parametros opcionais:

- nao foi criada regra especial para parametros opcionais;
- a selecao segue `DefaultTypeMap`: construtor parametrizado precisa corresponder a assinatura esperada pelo Dapper.

## Delivery

Arquivos alterados:

- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/TypeMaps/ConstructorParameterMap.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentConstructorTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentConventionTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/MultiTypeMap.cs`
- `test/Dapper.FluentMap.Tests/ConstructorMappingTests.cs`
- `docs/sdd/etapa-3/02-constructor-immutable-mapping.md`
- `docs/sdd/etapa-3/decisions.md`
- `docs/sdd/etapa-3/status.md`

Implementacao:

- `MappingRegistry` passou a expor resolucao interna de `IPropertyMap`, reaproveitando o cache estruturado existente.
- `FluentConstructorTypeMap` traduz colunas mapeadas para nomes/tipos de propriedades simples e delega a selecao ao `DefaultTypeMap`.
- `ConstructorParameterMap` implementa `SqlMapper.IMemberMap` para fornecer `ParameterInfo` ao Dapper.
- `MultiTypeMap.GetConstructorParameter` passou a ignorar `NotSupportedException` de mappers que nao suportam constructor parameter mapping, permitindo fallback real.
- `FluentMapTypeMap<TEntity>` e `FluentConventionTypeMap<TEntity>` passaram a compor o mapper de construtor antes do mapper de propriedades.

Testes adicionados cobrem:

- POCO tradicional;
- record posicional;
- classe imutavel;
- mapping explicito para parametro;
- naming policy para parametro;
- convention para parametro;
- construtor unico;
- multiplos construtores;
- construtor sem parametros;
- casing diferente;
- parameter mapping com fallback Dapper;
- mapping herdado;
- nested `MemberPath` nao usado como parametro de construtor;
- materializacao real via SQLite in-memory.

## Validation

Ambiente:

- SDK: `10.0.302`
- test runner detectado: VSTest com xUnit v3
- projeto principal: `netstandard2.0`
- projeto de testes do core: `net10.0`

Validacao localizada:

- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~ConstructorMappingTests"`
  - antes da implementacao: 8 falhas e 3 sucessos, reproduzindo a lacuna.
  - depois da implementacao: sucesso, 11 testes aprovados.

Validacao final:

- `dotnet restore`
  - resultado: sucesso.
- `dotnet build`
  - resultado: sucesso, 0 warnings, 0 erros.
- `dotnet test`
  - resultado: sucesso, 117 testes aprovados no core e 7 testes aprovados no Dommel.
- `dotnet build --configuration Release`
  - resultado: sucesso, 0 warnings, 0 erros.
- `dotnet test --configuration Release`
  - resultado: sucesso, 117 testes aprovados no core e 7 testes aprovados no Dommel.
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter "Category=Integration"`
  - resultado: sucesso, 21 testes de integracao aprovados.

`dotnet pack` nao foi executado porque nao houve mudanca de empacotamento, metadados NuGet ou targets.

## Limitacoes

- Constructor overload ambiguity continua seguindo o Dapper.
- Parametros opcionais nao recebem tratamento especial.
- Nested object materialization e Value Objects continuam fora do contrato.
- O suporte AOT/trimming nao foi ampliado.
- `DefaultTypeMap.MatchNamesWithUnderscores` continua sendo flag global do Dapper e nao e alterado por naming policies do FluentMap.
