# Compatibility Bridge

## Objetivo

O prompt 11.4 torna a arquitetura configuration-aware consumivel e transforma
`FluentMapper` em uma bridge sobre o modelo:

```text
builder default mutavel
    -> ImmutableFluentMapConfiguration
    -> FluentMapRuntime default
```

Nao ha um segundo runtime global para `QueryMapped*`, diagnostics estruturados
ou type maps Dapper. O caminho estatico publica um runtime default novo sempre
que `Initialize(...)` conclui, ou quando uma chamada parcialmente aplicada
falha depois de registrar algum estado valido.

## New configuration-aware entry points

A menor superficie publica adicionada e:

```csharp
var configuration = new FluentMapConfigurationBuilder()
    .AddMap<CustomerMap>()
    .Build();

var runtime = configuration.CreateRuntime();

var customers = runtime.QueryMapped<Customer>(
    connection,
    "SELECT 1 AS customer_id, 'Ada' AS name;");
```

`FluentMapRuntime` ja possui entry points de instancia para:

- `QueryMapped<T>()`;
- `QueryMapped<T, TProfile>()`;
- `QueryMappedSingle<T>()`;
- `QueryMappedSingle<T, TProfile>()`;
- `QueryMappedUnbuffered<T>()`;
- `QueryMappedUnbuffered<T, TProfile>()`;
- `QueryMappedUnbufferedAsync<T>()`;
- `QueryMultipleMapped(...)`.

Nao foi introduzido `AsyncLocal`. Configuracoes especificas continuam sendo
explicitas: o caller cria ou recebe um `FluentMapRuntime`.

## Default configuration

`FluentMapper.Configuration` expoe o `ImmutableFluentMapConfiguration`
atualmente publicado pela bridge estatica.

`FluentMapper.Runtime` expoe o `FluentMapRuntime` default atualmente usado por:

- `QueryMappedExtensions`;
- `MappedGridReader` criado pelos helpers estaticos;
- `FluentMapper.Explain<T>()`;
- type maps Dapper instalados pela bridge.

Essas propriedades sao snapshots efetivos do estado publicado. Uma nova chamada
de `Initialize(...)` pode trocar a instancia de runtime default.

## Static initialization

`FluentMapper.Initialize(...)` agora executa sob lock:

```text
configure(default FluentMapConfiguration)
    -> snapshot imutavel
    -> runtime default
    -> instalacao de SqlMapper.SetTypeMap para maps/conventions default
```

O `FluentMapConfiguration` historico continua sendo a fachada mutavel aceita
por `Initialize(...)`, mas escreve no builder default, nao no runtime publicado.
Depois da publicacao, consultas estaticas usam o runtime default.

## Lifecycle

Lifecycle recomendado para codigo novo:

```text
startup/composition root:
    builder mutavel
    -> Build()
    -> configuration.CreateRuntime()

runtime:
    usar runtime explicitamente em QueryMapped*
```

Lifecycle legado:

```text
startup:
    FluentMapper.Initialize(...)

runtime:
    Dapper.Query<T>() para mapping raiz global
    QueryMapped* para materializacao FluentMap controlada pelo runtime default
```

## Repeated Initialize

O comportamento aditivo historico foi preservado. Chamadas repetidas a
`FluentMapper.Initialize(...)` acumulam registros no builder default e publicam
um novo runtime default apos cada chamada bem-sucedida.

Inicializacoes concorrentes sao serializadas por lock. Isso evita corridas entre
duas chamadas de `Initialize(...)`; nao transforma a bridge global em uma API
multi-tenant para trocar configuracao durante queries em andamento.

Se uma chamada falhar depois de registrar parte do estado, a bridge tenta
publicar um runtime a partir do estado valido restante antes de relancar a
excecao. Isso preserva o comportamento historico em que registros realizados
antes do erro ficavam observaveis.

## Mutation attempts

`FluentMapConfigurationBuilder.Build()` continua sendo o limite de imutabilidade
para configuracoes novas. Mutacoes posteriores no builder sao rejeitadas.

Na bridge estatica, as colecoes legadas continuam mutaveis por compatibilidade.
Mutar essas colecoes diretamente altera o builder default legado, mas nao
reescreve automaticamente o runtime default ja publicado. Uma chamada posterior
a `Initialize(_ => { })` publica um novo runtime a partir do estado atual do
builder default.

## Legacy dictionaries

As APIs abaixo foram mantidas como campos publicos por compatibilidade de fonte
e binaria:

- `FluentMapper.EntityMaps`;
- `FluentMapper.TypeConventions`.

Estrategia escolhida: manter como colecoes mutaveis legadas ligadas ao builder
default, nao como runtime configuration-aware.

Consequencias:

- `GetEntityMaps()` e `GetTypeConventions()` preservam snapshots das instancias
  historicas registradas;
- mutacao direta continua podendo bypassar validacao, cache invalidation e
  instalacao de type maps Dapper;
- consultas ja publicadas usam `FluentMapper.Runtime`;
- codigo novo deve usar `FluentMapConfigurationBuilder` ou `Initialize(...)`.

Nenhuma API foi marcada como obsolete neste prompt para evitar ruído de upgrade.
A documentacao desencoraja uso novo das colecoes mutaveis.

## Deprecated APIs

Nao houve remocao nem nova marcacao `[Obsolete]`.

Possiveis obsoletions futuras:

- `FluentMapper.EntityMaps`;
- `FluentMapper.TypeConventions`;
- construcao direta de `FluentMapConfiguration` fora de `Initialize(...)`.

Essas mudancas exigem revisao propria de compatibilidade.

## Diagnostics

`FluentMapper.Explain<T>()` usa o runtime default publicado.

`FluentMapper.Validate()` valida o builder default legado e o runtime default.
Isso preserva diagnostics para consumidores/testes que ainda inserem maps
diretamente nas colecoes legadas, ao mesmo tempo em que mantem o runtime
configuration-aware como fonte das consultas.

`FluentMapRuntime.Validate()` e `FluentMapRuntime.Explain<T>()` continuam
isolados e nao acessam estado global.

## Query APIs

Os helpers estaticos continuam usando `FluentMapper.Runtime`:

- `connection.QueryMapped<T>(...)`;
- `connection.QueryMappedSingle<T>(...)`;
- `connection.QueryMappedUnbuffered<T>(...)`;
- `connection.QueryMappedUnbufferedAsync<T>(...)`;
- `connection.QueryMultipleMapped(...)`.

O runtime default e substituido por publicacao atomica de referencia. Runtimes
isolados criados por `configuration.CreateRuntime()` possuem caches proprios e
podem coexistir no mesmo processo.

## Dapper type maps

`FluentMapper.Initialize(...)` continua instalando `SqlMapper.SetTypeMap` para
entidades com maps default e conventions default. Esses type maps consultam o
runtime default atual quando Dapper resolve membros.

Limite preservado: `Dapper.Query<T>()` e global por tipo. Ele nao consegue
selecionar configuracao por chamada. Para multiplas configuracoes simultaneas,
usar os entry points de `FluentMapRuntime`.

## Dommel

Dommel permanece bridge process-wide. Os resolvers de Dommel continuam lendo as
colecoes legadas porque precisam preservar metadata especifica de
`DommelEntityMap` e `DommelPropertyMap`, que nao pertence ao snapshot imutavel
do core.

Nao foi prometido isolamento completo de Dommel neste prompt. Isso exige design
proprio dos extension points globais de `DommelMapper`.

## Reset

Nao foi introduzido `Reset()` publico.

Decisao: reset global continua ferramenta interna de teste/compatibilidade. Ele
nao resolve a causa da Issue #101, pois:

- queries em andamento poderiam observar troca global;
- `SqlMapper.SetTypeMap` e process-wide;
- Dommel tambem e process-wide;
- caches e generated materializers precisam pertencer a runtimes especificos.

Para novos consumidores, a resposta arquitetural e criar configuracoes e
runtimes isolados.

## Migration strategy

Migracao recomendada:

1. Manter `FluentMapper.Initialize(...)` para codigo existente.
2. Para novos cenarios com uma unica configuracao, continuar usando a bridge
   estatica se `Dapper.Query<T>()` global for desejado.
3. Para testes, multi-tenant ou bancos com schemas diferentes, criar
   `FluentMapConfigurationBuilder`, chamar `Build()`, depois
   `configuration.CreateRuntime()`.
4. Substituir `connection.QueryMapped<T>(...)` por
   `runtime.QueryMapped<T>(connection, ...)` nos pontos que precisam de
   configuracao especifica.
5. Evitar mutacao direta de `EntityMaps` e `TypeConventions`.

## Compatibility

Compatibilidade preservada:

- nenhuma API estatica removida;
- campos publicos legados mantidos;
- `Initialize` segue aditivo;
- `GetEntityMaps()` preserva instancias historicas registradas;
- generated registrations continuam funcionando pela DSL historica;
- profiles, converters e generated materializers funcionam no runtime default e
  em runtimes isolados.

Mudanca comportamental intencional:

- `FluentMapper.Runtime`/`Configuration` representam o runtime/configuracao
  publicados; mutacao direta das colecoes legadas nao altera queries ja
  publicadas ate nova publicacao via `Initialize(...)`.
