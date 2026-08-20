# Isolated FluentMap Runtime

## Objetivo

Introduzir o runtime associado a uma configuracao especifica:

```text
ImmutableFluentMapConfiguration
    -> FluentMapRuntime
    -> materialization / diagnostics / query integration
```

O nome concreto segue a ADR-3 e a ADR-13: `FluentMapRuntime` e o runtime
publico; `ImmutableFluentMapConfiguration` continua sendo o snapshot produzido
por `FluentMapConfigurationBuilder`.

## Responsibilities

`FluentMapRuntime` e responsavel por:

- manter uma referencia para a configuracao imutavel;
- possuir caches derivados daquela configuracao;
- resolver maps explicitos, profiles, conventions e fallback default do Dapper;
- selecionar materializers gerados quando o shape e o mapping efetivo batem;
- criar planos de materializacao de runtime quando nao ha generated match;
- executar diagnostics `Validate()` e `Explain<T>()` sem acessar estado global;
- alimentar `QueryMapped`, `ReadMapped`, unbuffered sync e async streaming.

O runtime nao possui conexao, transacao, comando, SQL nem reader. Esses recursos
continuam pertencendo ao caller e aos helpers de query.

## Configuration Ownership

O ownership e:

```text
FluentMapConfigurationBuilder mutavel
    -> Build()
ImmutableFluentMapConfiguration imutavel
    -> new FluentMapRuntime(configuration)
```

O runtime publico e criado a partir do snapshot, nao a partir das instancias
mutaveis de `EntityMap`, `PropertyMap` ou `Convention` usadas no builder.

Para isso, `RuntimeConfigurationRegistryFactory` reconstrui um registry interno
com adaptadores de snapshot:

- entity/profile maps usam property maps de snapshot;
- member paths completos sao preservados internamente;
- persistence e converter metadata sao preservados;
- generated materializer delegates sao registrados no registry do runtime;
- convention maps sao reconstruidos como maps efetivos de convention.

Mutacoes tardias no builder ou nos maps originais nao alteram runtime ja criado.

## Caches

Os caches derivados agora sao runtime-scoped porque vivem no
`MappingRegistry` possuido por cada `FluentMapRuntime`:

- property map cache;
- profile property map cache;
- convention lookup cache;
- materialization plan cache;
- generated materializer lookup/index.

A camada estatica `FluentMapper` preserva um `MappingRegistry` global por
compatibilidade, mas tambem o envolve em um `FluentMapRuntime` default. Assim,
os entry points estaticos delegam para a mesma abstracao usada por runtimes
isolados.

## Cache Keys

As chaves de cache continuam contendo:

- entity type;
- profile type, quando aplicavel;
- column name ou ordered column shape;
- estrategia de lookup.

Elas nao incluem um id explicito de configuracao porque cada runtime possui seu
proprio conjunto de caches. Portanto:

```text
Runtime A cache: Type + Profile + Shape
Runtime B cache: Type + Profile + Shape
```

Mesmo que `Type`, `Profile` e `Shape` sejam iguais, os resultados nao colidem
porque as entradas estao em instancias diferentes.

## Materializers

`MappedRowMaterializer` nao consulta mais `FluentMapper.Registry` diretamente.
Ele recebe um `FluentMapRuntime` e faz:

```text
runtime.Registry.TryGetGeneratedMaterializer(...)
    -> generated delegate
runtime.Registry.GetMaterializationPlan(...)
    -> runtime fallback plan
```

O plano fallback ainda e cacheado por runtime e nao por linha. A indirecao de
runtime acontece ao criar o materializer para o reader; a materializacao linha a
linha executa o delegate/plano ja resolvido.

## Profiles

Profiles continuam query-scoped por `TProfile`.

No runtime isolado:

- profile maps pertencem ao snapshot da configuracao;
- o cache de profile lookup fica no runtime;
- o mesmo tipo de profile pode existir em duas configuracoes diferentes com
  mappings diferentes;
- default maps nao vazam para profiles, preservando o comportamento existente.

## Converters

Property converter metadata e copiada para o snapshot e reconstruida no runtime.
Converters por instancia/delegate continuam sendo reutilizados pelo runtime, com
o mesmo contrato de thread safety ja documentado.

O runtime nao introduz factory/DI de converter. Isso permanece item futuro para
nao misturar isolamento de configuracao com ciclo de vida externo.

## Generated Registrations

Generated materializers ficam associados a configuracao que os registrou.

O codigo gerado continua podendo registrar descriptors pelo builder via:

```csharp
new FluentMapConfigurationBuilder()
    .Configure(config => config.AddGeneratedMappings())
    .Build();
```

O delegate gerado pode ser estruturalmente global por tipo no assembly
consumidor, desde que ele seja tratado como factory/metadata reutilizavel. Ele
nao deve possuir mapping state runtime. O mapping state efetivo fica no runtime
que validou o descriptor contra sua propria configuracao.

## Diagnostics

`FluentMapRuntime` expoe:

```csharp
runtime.Validate();
runtime.Explain<Customer>();
runtime.Explain<Customer, LegacyProfile>();
```

Essas chamadas usam o registry do runtime. `FluentMapper.Validate()` e
`FluentMapper.Explain<T>()` continuam existindo e delegam ao runtime global de
compatibilidade.

## Query Integration

O caminho estatico existente usa `FluentMapper.Runtime`.

O runtime isolado tambem oferece entry points de instancia para:

- `QueryMapped<T>()`;
- `QueryMapped<T, TProfile>()`;
- `QueryMappedSingle<T>()`;
- `QueryMappedSingle<T, TProfile>()`;
- `QueryMappedUnbuffered<T>()`;
- `QueryMappedUnbuffered<T, TProfile>()`;
- `QueryMappedUnbufferedAsync<T>()`;
- `QueryMultipleMapped(...)`, cujo `MappedGridReader` carrega o runtime.

`MappedGridReader` agora possui um runtime. `ReadMapped<T>()` e
`ReadMapped<T, TProfile>()` usam o runtime carregado pelo reader.

## Thread Safety

Modelo efetivo:

- builder: mutavel, uso de startup, nao thread-safe;
- immutable configuration: read-only e compartilhavel;
- runtime: thread-safe para consultas concorrentes;
- caches: `ConcurrentDictionary`;
- materialization delegates/plans: criados uma vez por shape e reutilizados;
- converters: podem ser chamados concorrentemente e devem ser stateless ou
  thread-safe.

O runtime nao possui dispose porque nao possui recursos descartaveis.

## Lifetime

Lifetime recomendado:

- `ImmutableFluentMapConfiguration`: singleton;
- `FluentMapRuntime`: singleton;
- wrappers de aplicacao: scoped/transient somente se carregarem recursos scoped
  que nao pertencem ao FluentMap.

## Limites

`Dapper.Query<T>()` puro continua dependendo de `SqlMapper.SetTypeMap`, que e
process-wide por tipo. Multiplas configuracoes simultaneas devem usar os entry
points controlados pelo `FluentMapRuntime`.

Dommel continua bridge process-wide nesta etapa. O isolamento completo de Dommel
exige design proprio por causa dos resolvers globais de `DommelMapper`.
