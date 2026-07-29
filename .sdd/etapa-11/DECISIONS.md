# Etapa 11 Architectural Decisions

## ADR-1 - Builder vs mutable global configuration

### Contexto

Hoje `FluentMapConfiguration` escreve diretamente em `FluentMapper.Registry`, que e global. Isso impede configuracoes independentes e faz reset parecer solucao.

### Decisao

Introduzir um builder mutavel separado do runtime. A configuracao global vira apenas uma bridge para o builder/runtime default.

### Alternativas consideradas

- Manter `Initialize` como unica API.
- Criar apenas `Reset()` publico.
- Fazer `Initialize` limpar sempre a configuracao anterior.

### Consequencias

O modelo fica mais previsivel para DI e testes. A compatibilidade exige preservar o comportamento aditivo atual na camada estatica.

## ADR-2 - Immutable configuration

### Contexto

Maps, conventions e property maps sao mutaveis e expostos por `IList`.

### Decisao

`Build()` deve produzir snapshot imutavel de maps, profiles, conventions, generated descriptors, converters e persistence metadata.

### Alternativas consideradas

- Continuar usando `ConcurrentDictionary` como "imutabilidade suficiente".
- Clonar somente no momento do cache.

### Consequencias

Reduz races e elimina invalidacao por mutacao tardia. Pode exigir descritores internos imutaveis mesmo preservando interfaces publicas mutaveis.

## ADR-3 - Runtime/context abstraction

### Contexto

`MappedRowMaterializer` e type maps consultam `FluentMapper.Registry` diretamente.

### Decisao

Criar um runtime/context que contem configuracao imutavel e caches derivados.

### Alternativas consideradas

- Passar dicionarios avulsos para cada API.
- Usar `AsyncLocal` para escolher configuracao.

### Consequencias

APIs novas podem receber runtime explicitamente. Evita estado ambiente escondido.

## ADR-4 - Global compatibility layer

### Contexto

Consumidores existentes usam `FluentMapper.Initialize(...)` e `Dapper.Query<T>()`.

### Decisao

Manter `FluentMapper` como bridge para o runtime default e para `SqlMapper.SetTypeMap`.

### Alternativas consideradas

- Remover API estatica.
- Manter duas implementacoes independentes.

### Consequencias

Compatibilidade e preservada, mas a camada estatica continua com limites process-wide.

## ADR-5 - Configuration-specific caches

### Contexto

Caches atuais ficam no registry global e dependem da configuracao efetiva.

### Decisao

Mover caches para o runtime. As chaves continuam focadas em tipo, profile, coluna e shape porque o runtime ja identifica a configuracao.

### Alternativas consideradas

- Manter caches globais com generation/version id.
- Limpar caches em toda mutacao.

### Consequencias

Configuracoes simultaneas nao colidem. O custo de memoria cresce por runtime.

## ADR-6 - Multiple configurations

### Contexto

Issue #101 pede alternar mappings entre bancos diferentes.

### Decisao

Suportar multiplas configuracoes nas APIs FluentMap opt-in que possam receber runtime/context.

### Alternativas consideradas

- Reset global entre operacoes.
- Chavear configuracao por connection string.

### Consequencias

Uso concorrente fica possivel sem colisao nos caminhos controlados pelo FluentMap. Dapper puro e Dommel permanecem limitados por estado global.

## ADR-7 - DI lifetime

### Contexto

Issue #84 relaciona ASP.NET Core e inicializacao por instancia.

### Decisao

Configuration e runtime devem ser singleton. Wrappers scoped/transient so devem existir se carregarem recursos por request ou opcoes por chamada.

### Alternativas consideradas

- Runtime scoped por request.
- Builder registrado no container.

### Consequencias

Caches sao reaproveitados e a configuracao imutavel e compartilhada com seguranca.

## ADR-8 - Dommel interaction

### Contexto

Dommel usa resolvers/builders globais em `DommelMapper`.

### Decisao

Tratar Dommel como bridge process-wide inicialmente. Nao prometer multiplas configuracoes Dommel no mesmo processo ate existir design especifico.

### Alternativas consideradas

- Tentar esconder runtime por `AsyncLocal`.
- Reimplementar Dommel ou SQL generation no core.

### Consequencias

O escopo fica honesto. O core pode evoluir isolamento sem transformar Dommel em ORM proprio.

## ADR-9 - Generated materializer registration

### Contexto

Generated materializers sao registrados no registry global e validados contra mapping efetivo atual.

### Decisao

Descriptors gerados devem ser registrados no builder/configuracao e indexados no runtime.

### Alternativas consideradas

- Registry global separado por assembly.
- Gerar codigo que chama diretamente APIs estaticas.

### Consequencias

Generated e runtime usam o mesmo isolamento. A extensao `AddGeneratedMappings()` pode continuar retornando o builder/configuration para compatibilidade.

## ADR-10 - Backward compatibility

### Contexto

`Initialize` aditivo, dicionarios publicos e type maps Dapper globais sao comportamento historico.

### Decisao

A Etapa 11 deve ser aditiva. Mudancas em API estatica devem ser bridgeadas e testadas por compatibilidade.

### Alternativas consideradas

- Major breaking change imediata.
- Descontinuar dicionarios publicos sem adaptador.

### Consequencias

A migracao e mais longa, mas consumivel por biblioteca publica.

## ADR-11 - Reset semantics

### Contexto

#79, #84 e #101 mostram demanda por clear/reset, mas tambem os riscos de concorrencia.

### Decisao

Reset nao e solucao principal. Manter reset interno/teste e avaliar API publica somente como ferramenta de compatibilidade bem documentada.

### Alternativas consideradas

- Expor `FluentMapper.Reset()` publico como feature central.
- Fazer clear automatico em `Initialize`.

### Consequencias

A arquitetura ataca a causa do estado global. Testes antigos ainda podem usar reset ate migrarem.

## ADR-12 - Native AOT implications

### Contexto

Etapas 7 a 10 validaram trimming parcial e mantiveram warnings em `QueryMapped*`.

### Decisao

Isolamento de configuracao deve favorecer registro explicito/gerado e snapshots, mas nao remover warnings AOT enquanto houver fallback runtime reflection/dynamic code.

### Alternativas consideradas

- Declarar runtime isolado como AOT-safe.
- Remover fallback runtime para forcar generated-only.

### Consequencias

Compatibilidade e preservada. Um caminho generated-only/AOT-safe deve ser decisao futura separada.

## ADR-13 - Naming do modelo inicial

### Contexto

`FluentMapConfiguration` ja e uma API publica mutavel usada por
`FluentMapper.Initialize(...)` e por extensoes existentes. Trocar esse tipo por
uma configuracao imutavel nesta etapa quebraria compatibilidade de fonte e
provavelmente binaria.

### Decisao

Introduzir `FluentMapConfigurationBuilder` como builder publico novo e
`ImmutableFluentMapConfiguration` como snapshot imutavel publico. Manter
`FluentMapConfiguration` como fachada historica mutavel, mas desacopla-la do
singleton por um `MappingRegistry` injetado internamente.

### Alternativas consideradas

- Renomear ou transformar `FluentMapConfiguration` diretamente em imutavel.
- Criar um segundo builder com DSL propria independente.
- Exigir que o source generator conheca o singleton global.

### Consequencias

A etapa fica aditiva e preserva `Initialize`. O builder consegue reutilizar
extensoes existentes via `Configure(Action<FluentMapConfiguration>)`, enquanto
o snapshot evita expor maps/conventions mutaveis como configuracao efetiva.
