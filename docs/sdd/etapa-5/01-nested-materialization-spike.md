# 01 - Spike Nested/Value-Object Materialization

## Specification

Existe demanda historica para mappings como:

```csharp
Map(x => x.Address.City).ToColumn("city");
Map(x => x.Document.Number).ToColumn("cpf");
```

A Etapa 2 introduziu `MemberPath`, portanto o core ja consegue representar:

```text
Address.City
Document.Number
```

O problema desta entrega foi verificar se representar o caminho e suficiente para o Dapper materializar o grafo completo, ou se o FluentMap precisa controlar parte da materializacao.

Casos obrigatorios avaliados:

- nested mutable object: `Customer.Address.City`;
- dois paths com terminal igual: `Rank.Level` e `Seniority.Level`;
- Value Object imutavel: `Cpf.Number`;
- nested record: `Customer(int Id, Address Address)`.

## Discovery

Arquivos analisados no FluentMap:

- `src/Dapper.FluentMap/Mapping/MemberPath.cs`
- `src/Dapper.FluentMap/Mapping/EntityMap.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMap.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMapIdentity.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentMapTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentConstructorTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/ConstructorParameterMap.cs`
- `src/Dapper.FluentMap/TypeMaps/MultiTypeMap.cs`
- `src/Dapper.FluentMap.Generators/MappingRegistrationGenerator.cs`
- testes de integracao e constructor mapping do core.

Fontes do Dapper 2.1.79 analisadas:

- pacote local `Dapper` 2.1.79 referenciado pelo projeto;
- tag oficial `2.1.79` do repositorio `DapperLib/Dapper`, commit `72a54c475f75e18cb93cba0809d00a5e6e49efd9`;
- `SqlMapper.ITypeMap.cs`;
- `SqlMapper.IMemberMap.cs`;
- `DefaultTypeMap.cs`;
- `CustomPropertyTypeMap.cs`;
- `SqlMapper.cs`, especialmente `GenerateDeserializerFromMap`.

Links de referencia primaria:

- `ITypeMap`: https://github.com/DapperLib/Dapper/blob/72a54c475f75e18cb93cba0809d00a5e6e49efd9/Dapper/SqlMapper.ITypeMap.cs
- `IMemberMap`: https://github.com/DapperLib/Dapper/blob/72a54c475f75e18cb93cba0809d00a5e6e49efd9/Dapper/SqlMapper.IMemberMap.cs
- `DefaultTypeMap`: https://github.com/DapperLib/Dapper/blob/72a54c475f75e18cb93cba0809d00a5e6e49efd9/Dapper/DefaultTypeMap.cs
- `CustomPropertyTypeMap`: https://github.com/DapperLib/Dapper/blob/72a54c475f75e18cb93cba0809d00a5e6e49efd9/Dapper/CustomPropertyTypeMap.cs
- materializer IL: https://github.com/DapperLib/Dapper/blob/72a54c475f75e18cb93cba0809d00a5e6e49efd9/Dapper/SqlMapper.cs

### O que ITypeMap consegue fazer

`SqlMapper.ITypeMap` consegue:

- escolher construtor com `FindConstructor`;
- forcar construtor explicito com `FindExplicitConstructor`;
- mapear coluna para parametro de construtor com `GetConstructorParameter`;
- mapear coluna para um membro simples com `GetMember`.

Isso e suficiente para:

- propriedades simples;
- fields simples;
- parametros de construtor simples;
- aliases de coluna;
- constructor mapping de records/classes imutaveis quando os parametros correspondem a propriedades simples.

### O que ITypeMap nao consegue fazer

`ITypeMap` nao recebe nem retorna:

- um `MemberPath`;
- uma callback de atribuicao;
- uma factory de objetos intermediarios;
- uma estrategia de nullability;
- um plano de construcao de grafo;
- um contexto de objeto raiz + caminho.

`IMemberMap` possui apenas:

```text
ColumnName
MemberType
PropertyInfo
FieldInfo
ParameterInfo
```

Nao ha contrato publico para "atribua esta coluna a Address.City criando Address se necessario".

### Onde o setter e emitido

No `GenerateDeserializerFromMap`, o Dapper:

1. obtem o `ITypeMap` do tipo raiz;
2. resolve cada coluna para `IMemberMap`;
3. quando nao usa construtor especializado, emite IL para setter de propriedade ou field;
4. para propriedade, chama `DefaultTypeMap.GetPropertySetterOrThrow(item.Property, type)`;
5. para field, emite `Stfld`.

O `type` usado e o tipo raiz que esta sendo materializado. Quando o `PropertyInfo` pertence ao tipo aninhado, o Dapper nao conhece a cadeia intermediaria. O teste de caracterizacao mostrou que devolver o leaf `Address.City` pode fazer o valor escalar ser escrito no slot errado do objeto raiz, em vez de criar `Address`.

### Custom IMemberMap

Um `IMemberMap` customizado nao resolve nested assignment porque ele nao contem operacao de atribuicao. Mesmo com um `ITypeMap` puro retornando o `PropertyInfo` do leaf, o Dapper continua emitindo setter simples para o tipo raiz.

### Constructor mapping

O `FluentConstructorTypeMap` existente filtra `MemberPath.IsNested`, por decisao da Etapa 3. Isso esta correto: parametros de construtor do tipo raiz nao sao `MemberPath`.

Nested record falha porque o Dapper procura um construtor de `Customer` cujos parametros correspondam as colunas. A coluna `city` nao corresponde ao parametro `Address address`, nem fornece como criar `Address`.

### TypeHandlers

TypeHandler resolve Value Object escalar quando o destino do Dapper e o Value Object inteiro:

```csharp
Map(x => x.Cpf).ToColumn("cpf");
```

Com um `SqlMapper.TypeHandler<Cpf>`, o Dapper converte `varchar -> Cpf` e atribui `Cpf`.

TypeHandler nao resolve:

```csharp
Map(x => x.Cpf.Number).ToColumn("cpf");
```

Nesse caso o destino exposto ao Dapper e o membro terminal `Number`, cujo tipo e `string`. O handler de `Cpf` nao participa, e a cadeia `Customer.Cpf` nao e criada.

### Multi-mapping

Multi-mapping do Dapper (`Query<TFirst, TSecond, TReturn>`) materializa varios objetos em segmentos de coluna e delega composicao a uma callback do consumidor. Ele pode ser usado pelo usuario para compor `Customer` + `Address`, mas nao e uma boa base interna generica para nested mapping arbitrario porque:

- exige conhecimento de `splitOn`;
- segmenta por tipos, nao por `MemberPath`;
- nao resolve multiplos paths para o mesmo tipo ou mesmo terminal;
- nao cobre bem Value Objects escalares;
- mudaria demais a API para consultas simples.

### Source generation

O generator da Etapa 4 gera apenas registro:

```csharp
configuration.AddMap<CustomerMap>();
```

Ele nao le `DbDataReader`, nao gera assignment e nao materializa objetos. Porem, a infraestrutura pode ser evoluida no futuro para gerar materializers especializados, o que e interessante para:

- performance;
- Native AOT;
- trimming;
- records;
- grafos imutaveis.

## Experimentos

Arquivo adicionado:

- `test/Dapper.FluentMap.Tests/NestedMaterializationSpikeTests.cs`

Testes de caracterizacao:

| Teste | Evidencia |
|---|---|
| `NestedMutablePathShouldWriteLeafValueIntoRootSlotInsteadOfMaterializingGraph` | `Map(x => x.Address.City)` nao cria `Address`; o valor do leaf aparece no slot do root, evidenciando que Dapper recebeu apenas o terminal. |
| `NestedPathsWithSameTerminalShouldBeConfiguredButDapperStillReceivesOnlyTerminalMembers` | `Rank.Level` e `Seniority.Level` coexistem em `Explain`, mas a materializacao por Dapper nao preserva os caminhos. |
| `TypeHandlerShouldMaterializeScalarValueObjectProperty` | `TypeHandler<Cpf>` funciona quando o destino e `Customer.Cpf`. |
| `TypeHandlerShouldNotMaterializeNestedValueObjectPath` | `TypeHandler<Cpf>` nao participa quando o mapping e `Customer.Cpf.Number`. |
| `NestedRecordShouldNotMaterializeThroughConstructorMapping` | Nested record falha por ausencia de construtor correspondente a colunas planas. |
| `PureITypeMapReturningNestedLeafPropertyShouldWriteLeafValueIntoRootSlot` | Mesmo sem FluentMap, `ITypeMap` puro retornando leaf `PropertyInfo` nao representa nested assignment. |

Validacao localizada:

```text
dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~NestedMaterializationSpikeTests"
```

Resultado:

```text
6 testes aprovados
```

## Alternativas

### Alternativa A - ITypeMap puro

Vantagens:

- maxima compatibilidade com `Dapper.Query<T>`;
- pouca API nova;
- baixo custo inicial.

Limites comprovados:

- `IMemberMap` so possui `PropertyInfo`, `FieldInfo` ou `ParameterInfo`;
- nao ha callback de assignment;
- nao ha criacao de intermediarios;
- devolver o `PropertyInfo` terminal pode escrever no slot errado do objeto raiz;
- nao suporta nested records ou Value Objects aninhados.

Conclusao:

```text
Rejeitada como arquitetura principal.
```

### Alternativa B - Dapper TypeHandler

Vantagens:

- usa mecanismo publico do Dapper;
- bom para `varchar -> Cpf`, `int -> Money`, etc.;
- baixo custo;
- compoe com constructor mapping simples.

Limites:

- funciona por tipo de destino, nao por caminho;
- nao cria `Customer.Cpf`;
- nao resolve `Customer.Cpf.Number`;
- nao constroi grafos imutaveis.

Conclusao:

```text
Aceita como estrategia complementar para Value Objects escalares.
```

### Alternativa C - Wrapper de Query

Exemplo conceitual:

```csharp
connection.QueryMapped<Customer>(sql, param);
```

Vantagens:

- caminho opt-in, preservando `Dapper.Query<T>`;
- permite controlar `DbDataReader`, nullability, criacao de intermediarios e assignments por `MemberPath`;
- permite rejeitar cenarios nao suportados com diagnostico claro;
- nao exige fork do Dapper.

Custos:

- nova API paralela;
- precisa implementar plano de materializacao;
- precisa definir conversoes, TypeHandlers, cache e diagnostico;
- pode duplicar parte pequena da materializacao simples.

Conclusao:

```text
Direcao principal para Entrega 2.
```

### Alternativa D - Source-generated materializer

Vantagens:

- melhor potencial de performance;
- melhor caminho para trimming e Native AOT;
- pode gerar codigo direto para records, construtores e Value Objects;
- reduz reflection no hot path.

Custos:

- complexidade alta;
- exige projeto generator mais ambicioso;
- nao cobre configuracao dinamica;
- aumenta custo de manutencao.

Conclusao:

```text
Estrategia futura/complementar, especialmente para Entrega 3 e AOT.
```

### Alternativa E - Post-materialization/intermediario

Modelo:

```text
DbDataReader ou DapperRow
    -> valores por coluna
    -> plano FluentMap
    -> objeto final
```

Vantagens:

- evita depender de internals do Dapper;
- permite usar Dapper para executar comando e obter valores;
- controla nested paths de forma deterministica;
- pode cachear planos por tipo e shape de colunas.

Custos:

- alocacao de representacao intermediaria se usar `DapperRow`;
- conversoes precisam ser definidas;
- objetos imutaveis exigem fase de construcao distinta.

Conclusao:

```text
Provavel implementacao inicial do wrapper de Query.
```

## Tabela Comparativa

| Criterio | A - ITypeMap puro | B - TypeHandler | C - Query wrapper | D - Source-generated materializer | E - Post-materialization |
|---|---|---|---|---|---|
| Compatibilidade com API atual | Alta | Alta | Media, API nova opt-in | Media, exige generator | Media, API nova opt-in |
| Complexidade | Baixa | Baixa | Media | Alta | Media |
| Performance | Alta quando simples, invalida para nested | Alta | Media | Alta | Media |
| AOT/trimming | Limitado pelo Dapper | Igual Dapper | Reflection-sensitive se runtime | Melhor potencial | Reflection-sensitive se runtime |
| Records | Simples apenas | Escalar apenas | Possivel | Melhor opcao | Possivel com plano |
| Value Objects | Nao | Escalares | Possivel | Possivel | Possivel |
| Nested mutable objects | Nao seguro | Nao | Sim | Sim | Sim |
| Nested immutable objects | Nao | Nao | Possivel com construtores | Sim | Possivel com construtores |
| Debuggability | Baixa para nested | Alta | Alta se diagnostico proprio | Media | Alta |
| Manutenibilidade | Ruim para nested | Boa | Boa se escopo estreito | Mais cara | Boa se escopo estreito |
| Dependencia de internals do Dapper | Baixa, mas insuficiente | Baixa | Baixa | Baixa/media | Baixa |

## Decision

Direcao principal:

```text
Nested materialization deve ser implementada por caminho opt-in controlado pelo FluentMap, provavelmente `QueryMapped<T>`, usando um plano de materializacao baseado em MemberPath.
```

Estrategia complementar:

```text
Value Objects escalares devem continuar usando Dapper TypeHandlers quando o destino mapeado e o Value Object inteiro.
```

Estrategia futura:

```text
Source-generated materializers devem ser avaliados para nested immutable graphs, records e cenarios trimmed/AOT, mas nao sao pre-requisito para iniciar nested mutable objects.
```

### API publica provavel

Ainda nao definitiva:

```csharp
connection.QueryMapped<Customer>(sql, param);
connection.QueryMappedSingle<Customer>(sql, param);
```

Regras provaveis:

- API opt-in em namespace `Dapper.FluentMap`;
- nao substituir `Dapper.Query<T>`;
- usar mappings registrados no `MappingRegistry`;
- aceitar somente cenarios validados inicialmente;
- falhar com diagnostico claro quando path intermediario nao puder ser criado.

### O que continua usando Dapper normal

- mappings simples;
- conventions e naming policies simples;
- constructor mapping simples;
- records posicionais simples;
- fallback default do Dapper;
- TypeHandlers escalares.

### Quando FluentMap precisa controlar materializacao

FluentMap precisa controlar quando houver:

- `MemberPath.IsNested`;
- criacao de objetos intermediarios;
- nested Value Object;
- nested record;
- grafo imutavel;
- necessidade de preservar dois paths com mesmo terminal;
- nullability ou ausencia de intermediario.

## Impacto Em AOT E Performance

Runtime wrapper/reflection:

- menor custo de implementacao;
- bom para provar semantics da Entrega 2;
- precisa cachear planos por tipo e shape de colunas;
- sera trimming-sensitive se depender de reflection ampla.

Source-generated materializer:

- melhor caminho para AOT;
- pode remover reflection do hot path;
- deve reaproveitar metadata estatica do generator da Etapa 4;
- aumenta complexidade e deve ser entregue separadamente.

TypeHandler:

- performance boa e integrada ao Dapper;
- AOT depende do proprio handler e do Dapper;
- nao cobre nested path.

## Riscos

- Escrever nested paths no type map atual do Dapper pode produzir atribuicoes incorretas; a Entrega 2 deve neutralizar esse caminho.
- Criar `QueryMapped<T>` amplia superficie publica e precisa de nomes, overloads e comportamento compativeis.
- Conversoes devem respeitar TypeHandlers sem copiar internals do Dapper.
- Nullability de intermediarios precisa de regra explicita: criar, preservar null ou falhar.
- Grafos imutaveis exigem construtor/factory e nao devem ser misturados com a primeira entrega de mutable nested objects sem testes suficientes.
- Cache de planos deve incluir tipo, colunas, ordem e configuracao que altera resultado.

## Instrucoes Para Entrega 2

- Comecar por nested mutable object com construtor sem parametros e propriedades settable.
- Criar API opt-in em vez de prometer suporte via `Dapper.Query<T>`.
- Rejeitar paths cuja cadeia intermediaria nao tenha setter ou construtor suportado.
- Criar objetos intermediarios apenas quando a coluna do leaf tiver valor materializavel.
- Preservar dois paths com mesmo terminal (`Rank.Level` e `Seniority.Level`) usando `MemberPath` completo.
- Adicionar diagnostico claro para caminhos nao suportados.
- Alterar o type map atual para nao devolver leaf `PropertyInfo` aninhado ao Dapper como se fosse propriedade simples.

## Instrucoes Para Entrega 3

- Suportar Value Objects escalares primeiro via documentacao/testes de TypeHandler.
- Para Value Objects imutaveis aninhados, definir como construir o objeto: TypeHandler, construtor unico, factory explicita ou materializer gerado.
- Nao tratar `Cpf.Number` como equivalente automatico a `Cpf`.
- Records aninhados devem passar por plano de construtor, nao por setter terminal.
- Avaliar source generation quando o runtime reflection-based ficar complexo ou produzir warnings AOT relevantes.

## Delivery

Arquivos adicionados:

- `test/Dapper.FluentMap.Tests/NestedMaterializationSpikeTests.cs`
- `docs/sdd/etapa-5/README.md`
- `docs/sdd/etapa-5/status.md`
- `docs/sdd/etapa-5/decisions.md`
- `docs/sdd/etapa-5/01-nested-materialization-spike.md`

Nao foram alterados:

- codigo de producao do core;
- Dommel;
- TargetFrameworks;
- metadados de pacote;
- source generator.

## Validation

Validacao localizada executada durante o spike:

```text
dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~NestedMaterializationSpikeTests"
```

Resultado:

```text
Sucesso, 6 testes aprovados.
```

Validacao final deve executar:

```text
dotnet restore
dotnet build
dotnet test
dotnet build --configuration Release
dotnet test --configuration Release
```

## Semantic Commit

Mensagem planejada:

```text
test: characterize nested mapping constraints
```
