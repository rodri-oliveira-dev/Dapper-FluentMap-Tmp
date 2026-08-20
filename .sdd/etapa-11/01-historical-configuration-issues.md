# Historical Configuration Issues

## Issue #101

Fonte: https://github.com/henkmollema/Dapper-FluentMap/issues/101

### Problema original

Em 2019-11-10, o usuario relatou leitura de dados de um banco para outro com nomes de colunas diferentes e pediu uma forma de resetar os mappings para o comportamento default. A issue foi fechada em 2020-07-24; o mantenedor respondeu que nao havia forma de fazer isso e que nao planejava adicionar a feature.

### Causa arquitetural

O problema e causado por configuracao process-wide. Uma vez que `FluentMapper.Initialize(...)` registra maps no estado global e instala type maps do Dapper por tipo, nao ha conceito de "configuracao A" para uma leitura e "configuracao B" para outra leitura no mesmo processo. Resetar o estado global resolveria apenas a troca serializada, nao consultas concorrentes, multi-tenant ou composicao de bibliotecas.

### Estado atual no fork

O fork possui um `FluentMapper.Reset(params Type[])` interno usado pelos testes. Esse reset limpa `EntityMaps`, `ProfileMaps`, `TypeConventions`, caches de property map, runtime materialization plan, generated materializers e remove type maps Dapper para os tipos informados. A API publica continua expondo `FluentMapper.EntityMaps` e `FluentMapper.TypeConventions`, entao consumidores ainda conseguem limpar dicionarios manualmente por compatibilidade.

O estado atual melhora o isolamento de testes internos, mas nao resolve a causa estrutural. Caches e resolvers ainda dependem do registry global, e `QueryMapped*`, type maps Dapper e Dommel ainda leem esse estado global.

Atualizacao do prompt 11.4: `QueryMapped*`, `ReadMapped*`, generated
materializers e diagnostics `Explain` agora usam o `FluentMapRuntime` default
publicado pela bridge estatica. `FluentMapper.Initialize(...)` reconstroi esse
runtime a partir de uma configuracao imutavel. `Dapper.Query<T>()` continua
process-wide por causa de `SqlMapper.SetTypeMap`, e Dommel continua bridge
process-wide por depender de metadata especifica de `DommelEntityMap` e
`DommelPropertyMap`.

### Solução simples possível

Uma API publica como `FluentMapper.Reset()` ou `ClearConfiguration()` poderia:

- limpar maps, profiles, conventions e generated materializers;
- limpar caches derivados;
- opcionalmente remover type maps Dapper para tipos conhecidos.

Essa solucao e simples de descobrir, mas continuaria process-wide. Ela tambem seria perigosa com queries concorrentes e dificil de tornar correta sem controlar todos os tipos ja instalados em `SqlMapper`.

### Solução estrutural recomendada

Introduzir um modelo isolado:

```text
builder mutavel
    -> configuracao imutavel
    -> runtime/context com caches por configuracao
    -> APIs QueryMapped/runtime que recebem ou pertencem a esse runtime
```

A API estatica deve virar camada de compatibilidade que possui um runtime default, em vez de continuar sendo a implementacao principal. O reset deve permanecer ferramenta de compatibilidade/teste, nao solucao arquitetural para multiplas configuracoes.

### Decisão para Etapa 11

Na Etapa 11, a direcao e especificar e implementar incrementalmente configuracoes imutaveis e runtime isolado. `FluentMapper.Initialize(...)` deve continuar funcionando, mas como bridge para o runtime default. Nao remover a API estatica e nao promover `Reset()` como API principal.

Decisao do prompt 11.4: nao criar `Reset()` publico. A solucao para novos
consumidores e `FluentMapConfigurationBuilder -> Build() ->
configuration.CreateRuntime()`. O reset interno permanece para testes e
compatibilidade.

Atualizacao do prompt 11.6: estado final da #101 e **Partially resolved**.

Por que nao "Resolved structurally" integral:

- a causa arquitetural foi resolvida para APIs controladas pelo FluentMap:
  `runtime.QueryMapped<T>()`, profiles, converters, generated materializers,
  diagnostics e DI podem usar configuracoes independentes no mesmo processo;
- testes novos provam uso concorrente de multiplos runtimes para o mesmo tipo
  sem `FluentMapper.Reset()`;
- a bridge estatica legado continua process-wide por compatibilidade;
- `Dapper.Query<T>()` continua limitado pelo `SqlMapper.SetTypeMap` global por
  tipo e nao pode selecionar configuracao por chamada;
- Dommel continua limitado por resolvers/builders globais de `DommelMapper` e
  por metadata especifica mantida nas colecoes legadas.

Estado final:

```text
Issue #101: Partially resolved
```

Resolvido estruturalmente para novos entry points isolados. Nao resolvido para
o uso legado direto de `Dapper.Query<T>()` ou Dommel com multiplas configuracoes
simultaneas.

## Issue #79

Fonte: https://github.com/henkmollema/Dapper-FluentMap/issues/79

### Problema original

Em 2018-11-08, um usuario executava `FluentMapper.Initialize(...)` dentro de um `UnitOfWork` registrado como transient em ASP.NET Core e recebia erro de mapa duplicado. Ele perguntou se haveria forma de "dispose mappings".

### Comentarios relevantes

O mantenedor explicou que `Initialize` deveria rodar uma vez no startup. Tambem sugeriu limpar `FluentMapper.EntityMaps` e `FluentMapper.TypeConventions` como workaround e reconheceu que limpar em cada `Initialize` seria perturbador porque usuarios poderiam depender do comportamento aditivo. Uma possivel API `ClearConfiguration` foi citada como mais descobrivel.

### Leitura arquitetural

#79 mostra dois contratos historicos importantes:

- `Initialize` aditivo e chamado em startup virou comportamento esperado;
- limpar colecoes globais era workaround aceito, mas nao seguro para concorrencia nem suficiente para Dapper/Dommel/caches.

## Issue #84

Fonte: https://github.com/henkmollema/Dapper-FluentMap/issues/84

### Problema original

Em 2019-03-07, um usuario de testes de integracao ASP.NET Core relatou falhas quando mais de um teste iniciava a aplicacao em memoria e cada startup tentava registrar os mesmos maps. Ele comparou com a API por instancia/DI do AutoMapper e pediu inicializacao por instancia.

### Comentarios relevantes

Foi sugerido limpar mappings antes de inicializar, mas o reporter observou falhas aleatorias com mais testes paralelos. O mantenedor respondeu que FluentMap configura type maps do Dapper, que nao sao consumidos por DI como `IMapper`, e que nao via forma de usar FluentMap concorrentemente; normalmente desabilitava execucao paralela nesses testes.

### Leitura arquitetural

#84 e a evidencia historica mais direta para a Etapa 11: reset/clear nao resolve paralelismo. A arquitetura precisa aceitar configuracao isolada para APIs controladas pelo FluentMap, e documentar que a integracao process-wide do Dapper puro e do Dommel requer bridge separada.
