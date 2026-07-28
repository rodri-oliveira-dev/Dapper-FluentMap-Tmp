# Etapa 9 Architectural Decisions

## ADR-1 - Wrapper de QueryMultiple

### Contexto

O FluentMap atual materializa consultas avancadas por `QueryMapped*`, que abre
um unico reader, bufferiza as linhas e fecha o reader antes de retornar. Dapper
2.1.79 oferece `QueryMultiple` e `GridReader`, mas nao expoe publicamente o
`DbDataReader` interno de forma adequada para materializacao customizada.

### Decisao

Projetar `QueryMultipleMapped(...)` como wrapper proprio do FluentMap, criado a
partir da execucao do comando, e nao como extensao que tenta extrair estado de
um `GridReader` existente.

### Alternativas consideradas

- Estender `SqlMapper.GridReader`: descartado porque exigiria internals ou
  reflection.
- Usar `GridReader.Read<T>()`: descartado para materializacao avancada porque
  nao aplica profiles/nested/value-object/generated do FluentMap.
- Criar wrapper ADO.NET proprio: escolhido como direcao arquitetural.

### Consequencias

O FluentMap passa a ter responsabilidade clara sobre reader/command no caminho
mapped. A implementacao precisa reproduzir cuidadosamente lifetime similar ao
Dapper sem virar uma abstracao geral de SQL.

## ADR-2 - Ownership do GridReader

### Contexto

`GridReader.Dispose()` fecha e descarta reader e command. A API publica expoe
`Command`, `IsConsumed` e metodos `Read*`, mas nao expoe o reader necessario
para materializacao customizada.

### Decisao

Nao assumir ownership nem inspecionar internals de `GridReader`. O ownership
principal da Etapa 9 sera de um wrapper FluentMap que controla recursos criados
por ele.

### Alternativas consideradas

- Aceitar `GridReader` em `ReadMapped`: inseguro porque nao ha reader publico.
- Duplicar logica interna do Dapper: descartado por manutencao e risco.
- Refletir membros protegidos/internos: descartado por compatibilidade.

### Consequencias

Usuarios que ja usam `QueryMultiple` continuam com Dapper normal. Usuarios que
precisam de materializacao avancada optam por `QueryMultipleMapped`.

## ADR-3 - Generated vs runtime em multiplos result sets

### Contexto

A Etapa 7 definiu lookup generated por entidade, profile opcional e shape
ordenado de colunas, com fallback runtime.

### Decisao

Cada result set deve executar o mesmo dispatch:

1. capturar column shape do grid atual;
2. tentar generated materializer;
3. cair para `NestedMaterializationPlan`.

### Alternativas consideradas

- Reusar um plano entre grids por entidade: descartado porque ordinais e nomes
  podem mudar.
- Desabilitar generated em `QueryMultipleMapped`: descartado por quebrar a
  equivalencia esperada da Etapa 7.

### Consequencias

Materializacao de cada grid fica independente e previsivel. A cache existente
continua adequada porque inclui tipo, profile e colunas ordenadas.

## ADR-4 - Buffered vs streaming API

### Contexto

`QueryMapped*` atual e buffered. Streaming muda lifetime observavel e aumenta
risco de vazamento de reader/connection.

### Decisao

Manter APIs buffered como primeiro incremento e criar nomes explicitos para
unbuffered/streaming.

### Alternativas consideradas

- Tornar `QueryMapped<T>` lazy: breaking behavioral change, descartado.
- Usar parametro `buffered` em toda API nova: familiar para Dapper, mas menos
  seguro para discoverability de lifetime.
- Criar APIs `Unbuffered`: preferido para deixar lifetime visivel.

### Consequencias

Menor risco de compatibilidade. Streaming entra como contrato opt-in separado,
com testes especificos de dispose.

## ADR-5 - Async streaming

### Contexto

Dapper 2.1.79 oferece `QueryUnbufferedAsync<T>` e
`GridReader.ReadUnbufferedAsync<T>()`, mas a materializacao avancada do
FluentMap precisa ler `IDataRecord`/`DbDataReader` e aplicar seu proprio plano.

### Decisao

Avaliar `IAsyncEnumerable<T>` como API separada, preferencialmente em overloads
baseados em `DbConnection`/`DbDataReader`, com cancellation explicita.

### Alternativas consideradas

- Retornar `Task<IEnumerable<T>>` para streaming: descartado, isso implica
  buffering.
- Usar diretamente `QueryUnbufferedAsync<T>` do Dapper: nao aplica
  materializacao avancada.

### Consequencias

A API pode exigir dependencia publica de async interfaces no target
`netstandard2.0`. Isso deve ser registrado como decisao de compatibilidade
antes da implementacao.

## ADR-6 - Cancellation

### Contexto

`CommandDefinition` possui `CancellationToken` em Dapper 2.1.79. O FluentMap
atual aceita `CommandDefinition`, mas as APIs convenientes nao recebem token.

### Decisao

APIs assincronas novas devem aceitar `CommandDefinition` e overloads com
`CancellationToken`. O token deve ser propagado para execucao e observado
durante loops de streaming.

### Alternativas consideradas

- Depender apenas de `CommandDefinition`: correto, mas pouco discoverable.
- Adicionar token em todos os overloads existentes: aditivo, mas deve ser feito
  com cuidado para evitar ambiguidade de overloads.

### Consequencias

Cancellation vira parte do contrato da Etapa 9. Excecao de cancelamento nao
deve ser wrapada como erro de mapping.

## ADR-7 - Connection lifetime

### Contexto

Dapper normalmente nao assume ownership de conexoes do usuario. Quando abre uma
conexao fechada para um comando, fecha ao concluir.

### Decisao

O FluentMap deve preservar essa regra. O wrapper e dono de command/reader, mas
nao da conexao recebida.

### Alternativas consideradas

- Exigir conexao aberta: simples, mas menos alinhado a Dapper.
- Sempre fechar conexao no dispose: breaking/hostil para usuarios.

### Consequencias

A implementacao precisa registrar se abriu a conexao. Testes devem validar
conexao inicialmente aberta e fechada.

## ADR-8 - Fallback

### Contexto

Generated materializers sao otimizacao, nao requisito funcional. O fork
preserva fallback runtime para maps dinamicos, conventions e shapes nao
gerados.

### Decisao

Todas as APIs normais da Etapa 9 devem manter fallback runtime. Uma API
generated-only so deve ser considerada em etapa futura.

### Alternativas consideradas

- Exigir generated para streaming: reduz reflection, mas quebra coverage de
  maps atuais.
- Usar apenas runtime em multiple result sets: perde beneficios da Etapa 7.

### Consequencias

As novas APIs herdam warnings de trimming/dynamic-code do caminho atual. AOT
full continua nao declarado.

## ADR-9 - API publica

### Contexto

Nomes conceituais avaliados:

```csharp
using var multi = connection.QueryMultipleMapped(sql);
var customers = multi.ReadMapped<Customer>();
var legacy = multi.ReadMapped<Customer, LegacyProfile>();
connection.QueryMappedUnbuffered<T>(...);
connection.QueryMappedUnbufferedAsync<T>(...);
```

### Decisao

Adotar como direcao de design:

- `QueryMultipleMapped` para criar o wrapper;
- `ReadMapped<T>` e `ReadMapped<T, TProfile>` no wrapper;
- nomes com `Unbuffered` para streaming sincrono/assincrono.

A assinatura final deve ser escolhida no prompt de implementacao depois de
verificar overload ambiguity, target framework e XML docs.

### Alternativas consideradas

- `QueryMappedMultiple`: menos alinhado ao nome Dapper `QueryMultiple`.
- Parametro `profile` runtime em vez de generic `TProfile`: menos consistente
  com API atual.
- Extensoes em `GridReader`: descartadas como caminho principal.

### Consequencias

A API fica discoverable para usuarios Dapper e preserva profiles por operacao.
Ainda sera necessario decidir se overloads async default sem profile entram no
mesmo incremento.

## ADR-10 - FluentMap nao abstrai SQL/query execution alem do necessario

### Contexto

O objetivo do core e mapear colunas para membros e materializar objetos quando
o usuario opta por isso. O projeto nao deve virar ORM, query builder ou CRUD.

### Decisao

A Etapa 9 pode criar wrappers minimos para executar comandos e controlar
reader/lifetime apenas quando isso for necessario para aplicar materializacao
avancada. Ela nao deve modelar SQL, joins, includes, repositories ou tracking.

### Alternativas consideradas

- Criar API de query rica: fora de escopo.
- Reaproveitar Dommel para execucao: fora de escopo do core e mistura leitura
  avancada com persistencia.

### Consequencias

O design permanece pequeno e compatibilidade com Dapper fica clara. Usuarios
continuam escrevendo SQL e escolhendo quando usar Dapper puro ou FluentMap
mapped.

## ADR-11 - API implementada no Prompt 9.2

### Contexto

O Prompt 9.2 precisava entregar infraestrutura produtiva de multiple result
sets sem antecipar streaming, async disposal ou dependencia publica nova para
`netstandard2.0`.

### Decisao

Implementar `QueryMultipleMapped(...)` retornando `MappedGridReader`, com
`ReadMapped<TEntity>()` e `ReadMapped<TEntity, TProfile>()` buffered.

Usar `SqlMapper.ExecuteReader(connection, CommandDefinition)` para execucao do
comando e encaminhamento de parametros, transacao, timeout e command type.

Extrair o dispatch de materializacao para `MappedRowMaterializer`, compartilhado
por `QueryMapped*` e `MappedGridReader`.

Nao adicionar `QueryMultipleMappedAsync`, `IAsyncDisposable` ou
`IAsyncEnumerable<T>` neste incremento.

### Alternativas consideradas

- Implementar command execution manual por ADO.NET: descartado para evitar
  duplicar binding de parametros e comportamento publico do Dapper.
- Adicionar async/streaming ja no Prompt 9.2: descartado porque muda lifetime e
  pode exigir dependencia publica adicional no target `netstandard2.0`.
- Usar `SqlMapper.GridReader`: descartado pelas ADRs anteriores, pois o reader
  necessario nao e superficie publica.

### Consequencias

A API publica nova e aditiva e alinhada ao nome `QueryMultiple` do Dapper. O
primeiro incremento e buffered e sequencial. Streaming e async continuam
decisoes separadas para prompts posteriores.

## ADR-12 - QueryMappedUnbuffered sincrono

### Contexto

`QueryMapped*` bufferiza resultados em `List<TEntity>` antes de retornar. Isso
mantem lifetime simples, mas penaliza datasets grandes. Dapper 2.1.79 oferece
leitura sincronica unbuffered pela convencao `Query<T>(..., buffered: false)`,
mas esse caminho nao aplica nested/value-object/profile/generated do FluentMap.

### Decisao

Adicionar APIs `QueryMappedUnbuffered<TEntity>` e
`QueryMappedUnbuffered<TEntity, TProfile>` como caminhos lazy e explicitos.

O FluentMap executa por `SqlMapper.ExecuteReader`, resolve o materializer uma
vez por shape e materializa uma linha por `MoveNext()`. O reader e descartado
quando a enumeracao termina, quando o enumerator e descartado ou quando uma
excecao interrompe o loop.

### Alternativas consideradas

- Tornar `QueryMapped<T>` lazy: descartado por breaking change.
- Adicionar parametro `buffered` a `QueryMapped<T>`: descartado por esconder
  mudanca forte de lifetime em um booleano opcional.
- Usar `Dapper.Query<T>(buffered: false)`: descartado porque nao aplica a
  materializacao avancada do FluentMap.

### Consequencias

O usuario ganha processamento incremental sincrono sem async streaming. O
contrato exige que conexao/transacao externas permanecam validas durante a
enumeracao. Async streaming permanece para prompt futuro.
