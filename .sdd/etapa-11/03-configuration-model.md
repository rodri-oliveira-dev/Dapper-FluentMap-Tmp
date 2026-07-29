# Configuration Model

## Objetivo

O modelo inicial da etapa 11 introduz a fronteira:

```text
FluentMapConfigurationBuilder mutavel
    -> Build()
ImmutableFluentMapConfiguration imutavel
```

Sem migrar ainda todos os entry points de runtime. A API estatica
`FluentMapper.Initialize(...)` continua sendo a bridge de compatibilidade global.

## Builder API

`FluentMapConfigurationBuilder` fica em `Dapper.FluentMap.Configuration` e
reusa a DSL historica de registro:

- `AddMap<TEntity>(IEntityMap<TEntity>)`;
- `AddMap<TMap>()`;
- `AddProfile<TMap>()`;
- `AddConvention<TConvention>().ForEntity<TEntity>()`;
- `UseNamingPolicy(...).ForEntity<TEntity>()`;
- `AddGeneratedMaterializer(...)`;
- `AddMapsFromAssembly(...)` e `AddMapsFromAssemblyContaining<TMarker>(...)`;
- `Configure(Action<FluentMapConfiguration>)`.

`Configure(...)` existe para reaproveitar extensoes existentes sobre
`FluentMapConfiguration`, incluindo o `AddGeneratedMappings()` emitido pelo
source generator. A extensao gerada nao precisa conhecer o singleton global: a
fachada passada pelo builder escreve no registry isolado do builder.

## Configuration API

`ImmutableFluentMapConfiguration` e o snapshot efetivo produzido por `Build()`.
Ele expoe somente colecoes read-only:

- `EntityMaps`: default maps por tipo de entidade;
- `ProfileMaps`: maps profile-scoped;
- `TypeConventions`: conventions e naming policies por entidade;
- `GeneratedMaterializers`: descriptors gerados por entidade/profile/shape.

O tipo historico `FluentMapConfiguration` permanece mutavel porque faz parte da
API publica existente e e usado por `FluentMapper.Initialize(...)`. Nesta etapa,
ele foi desacoplado do singleton por um registry injetado internamente.

## Lifecycle

1. O consumidor cria um `FluentMapConfigurationBuilder`.
2. O builder recebe registros mutaveis durante startup/composition root.
3. `Build()` executa validacao usando a mesma fonte de regras do runtime.
4. `Build()` cria um snapshot read-only.
5. Depois de `Build()`, o builder fica selado.
6. Chamadas posteriores a `Build()` retornam a mesma instancia imutavel.

O builder nao e thread-safe. A configuracao imutavel resultante e segura para
leituras concorrentes.

## Mutation Boundaries

O limite de mutacao e o primeiro `Build()`.

Depois disso:

- chamadas mutadoras no builder lancam `InvalidOperationException`;
- objetos `FluentConventionConfiguration` obtidos antes do build tambem rejeitam
  `ForEntity(...)`/scanning;
- o snapshot nao expoe `IEntityMap`, `Convention` nem listas mutaveis como
  configuracao efetiva;
- mutacao tardia de uma instancia de map usada no builder nao altera o snapshot.

`FluentMapper.EntityMaps`, `FluentMapper.TypeConventions` e
`IEntityMap.PropertyMaps` continuam mutaveis por compatibilidade, mas nao sao o
modelo recomendado para novas configuracoes imutaveis.

## Validation

`Build()` chama `MappingRegistry.ValidateConfiguration()`. Essa e a mesma fonte
usada por `FluentMapper.Validate()`.

Nao ha uma segunda arvore de regras para `configuration.Validate()`. A validacao
de invariants continua centralizada em:

- `MappingConfigurationValidator`;
- composicao/checagem de include base em `MappingRegistry`;
- validacao de generated materializer descriptor no registro.

## Mappings

Default maps sao capturados como `EntityMappingConfiguration`:

- entity type;
- concrete map type;
- property maps;
- included base types.

Property maps sao copiados para `PropertyMappingConfiguration`, preservando:

- member path;
- terminal `PropertyInfo`;
- column name;
- case sensitivity;
- ignored;
- persistence metadata;
- conversion metadata.

## Conventions

Conventions sao aplicadas no builder pela mesma logica existente em
`FluentConventionConfiguration`. O snapshot captura `ConventionType` e os
`PropertyMappingConfiguration` gerados para a entidade.

Conventions nao sao expostas como instancias mutaveis no snapshot.

## Naming

Naming policies continuam implementadas como `NamingPolicyConvention`.
`UseNamingPolicy(...)` retorna a mesma configuracao de convention historica, mas
apontando para o registry isolado do builder. O snapshot captura os property maps
resultantes e sua configuracao de case sensitivity.

## Profiles

Profiles sao capturados separadamente em `ProfileMappingConfiguration`:

- entity type;
- profile type;
- concrete map type;
- property maps;
- included base types.

Profiles continuam query-scoped conceitualmente. Esta etapa nao altera
`QueryMapped<TEntity, TProfile>()`.

## Converters

Property converter metadata faz parte do snapshot de cada property map.

O modelo preserva a regra da etapa 10:

```text
property converter -> metadata por mapping/member/profile
```

As instancias/delegates de converter continuam encapsuladas na metadata
existente. Consumidores devem tratar converters como stateless/thread-safe.

## Persistence Metadata

Persistence metadata tambem e copiada para cada `PropertyMappingConfiguration`.
O core continua metadata-only para writes. Dommel permanece o pacote que
interpreta essa metadata em SQL gerado.

## Generated Registrations

Generated materializers sao registrados no builder pelo mesmo contrato
`AddGeneratedMaterializer(...)`. O snapshot captura:

- entity type;
- profile type opcional;
- ordered column shape;
- delegate interno do materializer.

O generator atual pode ser usado com:

```csharp
var configuration = new FluentMapConfigurationBuilder()
    .Configure(config => config.AddGeneratedMappings())
    .Build();
```

Isso evita que o generated registration precise conhecer `FluentMapper` como
singleton global.

## Duplicate Detection

Duplicate maps, duplicate profiles e duplicate generated materializers continuam
rejeitados no momento de registro pelo `MappingRegistry`.

Duplicidades dentro de maps/conventions e conflitos de coluna continuam
validados por `MappingConfigurationValidator`, inclusive quando um map mutavel e
alterado depois do registro mas antes do `Build()`.

## Inheritance

`IncludeBase<TBase>()` continua validado durante registro/build pela mesma
composicao existente em `MappingRegistry`. O snapshot preserva os tipos base
incluidos para que o runtime isolado futuro consiga compor metadata sem reler
objetos mutaveis.

## Thread Safety

Modelo desta etapa:

- builder: mutavel, nao thread-safe, uso de startup;
- immutable configuration: read-only snapshot, seguro para leituras concorrentes;
- runtime atual: ainda usa `FluentMapper.Registry` para APIs existentes;
- runtime isolado futuro: deve consumir o snapshot e manter caches por runtime.

As colecoes sao `ReadOnlyDictionary`/`ReadOnlyCollection`, nao `FrozenDictionary`,
porque o pacote principal permanece em `netstandard2.0` e a etapa nao aumenta
TFMs nem adiciona dependencias.
