# Configuration Isolation Specification

## Objetivos

- Separar configuracao mutavel de runtime de consulta.
- Permitir construir uma configuracao imutavel reutilizavel.
- Permitir mais de uma configuracao FluentMap no mesmo processo para APIs que possam receber runtime/context.
- Preservar `FluentMapper.Initialize(...)` como camada de compatibilidade.
- Reduzir dependencia de reset global para testes.
- Tornar caches derivados escopados por runtime/configuracao.
- Preparar integracao futura com DI e ASP.NET Core sem Service Locator e sem `AsyncLocal` ambient.

## Nao objetivos

- Remover a API estatica nesta etapa.
- Transformar o core em ORM, repository, Unit of Work, connection factory ou SQL generator.
- Resolver multi-tenancy de aplicacao como framework completo.
- Tornar `Dapper.Query<T>()` capaz de escolher configuracao por chamada sem novo contrato do Dapper.
- Tornar Dommel completamente isolado sem avaliar seus extension points globais.
- Declarar Native AOT completo para `QueryMapped*`.
- Executar write converters em Dommel/Dapper.

## Configuration Builder

Conceito recomendado: um builder mutavel com a DSL atual de registro.

Nome conceitual preferido apos leitura da API: `FluentMapConfigurationBuilder`.

Responsabilidades:

- receber `AddMap`, `AddProfile`, `AddConvention`, naming policies e generated materializers;
- executar validacoes de configuracao;
- ordenar includes de base;
- guardar descritores mutaveis somente ate `Build()`;
- produzir uma configuracao imutavel.

`FluentMapConfiguration` hoje e uma fachada mutavel. Para compatibilidade, ela pode ser mantida como tipo historico e gradualmente redirecionada para o builder, ou virar wrapper temporario sobre o builder default.

## Immutable Configuration

Nome conceitual recomendado: `FluentMapConfiguration`.

Responsabilidades:

- conter snapshots imutaveis de default maps, profile maps, conventions/naming policies, generated descriptors, persistence metadata e converter metadata;
- nao expor colecoes mutaveis;
- ser segura para compartilhamento entre threads;
- ser registravel como singleton em DI;
- ser independente de caches lazy de materializacao.

Regra:

```text
mutable builder
    -> Build()
immutable configuration
```

Apos `Build()`, maps, profiles, conventions e converter metadata nao mudam. Caches derivados podem ser lazy, desde que estejam em runtime/context thread-safe.

## Runtime Context

Nome conceitual recomendado: `FluentMapRuntime`.

Responsabilidades:

- possuir uma `FluentMapConfiguration`;
- possuir caches derivados por configuracao;
- resolver property maps, profile maps, conventions e Dapper default fallback;
- criar materializers runtime;
- localizar generated materializers;
- produzir diagnostics/explain;
- oferecer APIs de consulta opt-in ou ser passado a elas.

O runtime nao deve possuir conexao, transacao, comando ou SQL. Ele deve ser singleton quando sua configuracao for imutavel.

## Global Compatibility Layer

`FluentMapper` deve continuar existindo. A direcao e:

```text
FluentMapper
    -> default builder/configuration bridge
    -> default FluentMapRuntime
```

A camada estatica nao deve duplicar a implementacao. Ela deve delegar ao mesmo runtime usado por APIs instanciadas.

Compatibilidade a preservar:

- `Initialize(Action<FluentMapConfiguration>)` continua aditivo por padrao, porque #79 registrou essa expectativa;
- `EntityMaps` e `TypeConventions` continuam existindo por compatibilidade, mas devem ser desencorajados e eventualmente tratados como view/adapter legado;
- `Validate`, `Explain`, `GetEntityMaps` e `GetTypeConventions` continuam funcionando sobre o runtime default;
- `Reset` interno pode permanecer para testes e bridge de compatibilidade, sem virar solucao principal.

## Query Integration

APIs atuais `QueryMapped*`, `ReadMapped*`, `QueryMultipleMapped`, streaming sync e async hoje usam `FluentMapper.Registry`.

Evolucao proposta:

- manter overloads atuais usando runtime default;
- adicionar novos entry points que recebam `FluentMapRuntime` explicitamente ou sejam metodos de extensao sobre um query context;
- evitar `AsyncLocal` para selecionar configuracao implicitamente;
- nao alterar semantica de buffering/streaming existente;
- manter generated-then-runtime fallback por runtime.

Exemplo conceitual:

```csharp
var runtime = configuration.CreateRuntime();
var rows = connection.QueryMapped(runtime, sql);
```

## Profiles

Profiles continuam query-scoped e escolhidos por `TProfile` nos metodos existentes.

No modelo isolado:

- profile maps ficam na configuracao imutavel;
- caches usam chave `runtime/configuration + entity + profile + column shape`;
- profile nao instala type map Dapper global;
- maps default nao vazam para profiles salvo composicao explicita, como hoje.

## Generated Materializers

Generated materializers devem pertencer a configuracao ou ao runtime criado dela.

Direcao:

- generator continua emitindo `AddGeneratedMappings()` para compatibilidade;
- em API nova, o codigo gerado deve registrar descriptors no builder;
- descriptors devem ser congelados em `Build()`;
- lookup generated deve ser por runtime/configuracao, entity, profile e ordered column shape;
- validacao contra mapping efetivo deve usar a configuracao do runtime, nao `FluentMapper.Registry`.

Campos estaticos gerados para converter types continuam aceitaveis quando stateless/thread-safe.

## Property Converters

Converter metadata deve ser parte da configuracao imutavel.

Direcao:

- overloads por tipo continuam criando instancia durante configuracao, como hoje;
- instancias/delegates configurados continuam pertencendo a metadata do map;
- contrato deve manter exigencia de thread-safety/stateless porque runtime singleton pode reutilizar instancias;
- DI/factory de converter e item futuro e so deve ser adicionado apos existir runtime/configuration isolation.

## Persistence Metadata

Persistence metadata pertence ao snapshot imutavel de property maps.

Direcao:

- core continua metadata-only para write;
- Dommel continua pacote que interpreta persistence metadata;
- `Ignore()` continua unica semantica que remove materializacao;
- write converters continuam metadata-only ate haver hook de parametros por propriedade.

## Diagnostics

Diagnostics devem ser expostos pelo runtime/configuracao:

- `Validate()` em configuracao ou runtime;
- `Explain<TEntity>()` usando a configuracao do runtime;
- diagnostics generated devem contar descriptors do runtime selecionado;
- camada estatica delega ao runtime default.

## Dommel

Dommel e o ponto mais sensivel para isolamento porque seus resolvers sao instalados globalmente em `DommelMapper`.

Direcao para Etapa 11:

- nao prometer isolamento completo para APIs Dommel existentes sem novos extension points;
- manter `ForDommel()` como bridge de compatibilidade global;
- extrair resolvers para dependerem de um provider de runtime/configuracao quando possivel;
- avaliar uma API futura que instale Dommel contra o runtime default explicitamente;
- documentar que multiplas configuracoes Dommel no mesmo processo nao sao plenamente suportadas enquanto `DommelMapper` for global.

## Caching

Caches derivados devem sair do estado global e morar no runtime.

Caches candidatos:

- property map cache;
- materialization plan cache;
- generated materializer lookup/index;
- diagnostics/explain cache, se for criado no futuro.

Chaves devem incluir todos os fatores que alteram resultado: entity type, profile type, column name, ordered column shape, estrategia de resolucao e a identidade implicita do runtime/configuracao. Se o cache mora dentro do runtime, nao precisa incluir id de configuracao na chave.

## Thread Safety

Modelo desejado:

- builder nao e thread-safe e deve ser usado na inicializacao;
- configuracao imutavel e thread-safe;
- runtime e thread-safe;
- caches lazy usam `ConcurrentDictionary` ou outra primitiva equivalente;
- maps/conventions congelados nao podem ser alterados apos build;
- chamadas de query podem rodar em paralelo quando usam runtimes distintos ou o mesmo runtime imutavel.

## Multiple Configurations

A arquitetura deve permitir:

```text
Configuration A -> Runtime A -> Database A
Configuration B -> Runtime B -> Database B
```

Sem colisao de mapping state para APIs controladas pelo FluentMap.

Limite importante: `Dapper.Query<T>()` puro e Dommel existente seguem por integracao process-wide. Para multiplas configuracoes, sera necessario usar entry points FluentMap que recebam runtime ou criar bridges especificas.

## Test Isolation

Novos testes devem poder criar `FluentMapConfigurationBuilder`, chamar `Build()`, criar `FluentMapRuntime` e consultar sem tocar `FluentMapper.Reset`.

Testes de compatibilidade estatica podem continuar serializados e usando reset interno. A meta e reduzir, nao apagar em um unico passo, a dependencia global.

## Dependency Injection

Pacote futuro de DI deve registrar configuracao e runtime como singletons quando a configuracao for imutavel.

Lifetimes recomendados:

- singleton: `FluentMapConfiguration`;
- singleton: `FluentMapRuntime`;
- scoped: wrappers que agreguem runtime + recursos scoped da aplicacao, se houver necessidade real;
- transient: query context leve quando ele apenas carrega runtime e opcoes por chamada.

Nao usar `Scoped` por reflexo de ASP.NET Core. Mapping metadata imutavel e cache de materializacao sao recursos naturalmente singleton.

## ASP.NET Core

Direcao futura:

```csharp
services.AddFluentMap(builder =>
{
    builder.AddMap<CustomerMap>();
});
```

Essa API deve:

- construir configuracao uma vez durante composition root;
- registrar runtime singleton;
- nao registrar connection factory propria;
- nao depender de service locator;
- permitir que testes de integracao criem service providers independentes sem colisao no runtime FluentMap;
- deixar claro que Dapper type maps globais e Dommel global exigem opt-in separado.

## Trimming / Native AOT

Direcao:

- preservar anotacoes de APIs que usam scanning, reflection e runtime fallback;
- manter registro explicito e gerado como caminhos preferidos;
- favorecer descriptors imutaveis e generated registration por builder;
- evitar reflection scanning no caminho DI por default;
- nao declarar `QueryMapped*` AOT-safe enquanto houver fallback runtime possivel;
- nao introduzir ativacao reflection-only para converter factories.

## Backward Compatibility

Compromissos:

- nenhuma API estatica removida na Etapa 11;
- `Initialize` continua funcionando;
- comportamento aditivo de `Initialize` deve ser preservado inicialmente;
- colecoes publicas mutaveis continuam por compatibilidade, mas novas APIs devem evitar esse padrao;
- `SqlMapper.SetTypeMap` continua sendo usado pela bridge estatica para compatibilidade com `Dapper.Query<T>()`;
- alteracoes de comportamento publico exigem teste e nota de migracao.

## Migration Strategy

Sequencia recomendada:

1. Introduzir builder/configuration/runtime internos ou publicos aditivos.
2. Mover logica do `MappingRegistry` para runtime isolado.
3. Adaptar `QueryMapped*` para usar runtime default e adicionar overloads por runtime.
4. Reescrever `FluentMapper` como bridge de compatibilidade.
5. Adicionar DI em pacote/namespace separado.
6. Migrar testes para runtime isolado onde possivel.
7. Endurecer Dommel e documentar limites.

## Performance

Expectativa:

- Build pode ter custo maior por congelar snapshots e validar composicao.
- Query hot path deve manter caches lazy e generated dispatch existentes.
- Runtime singleton permite amortizar caches.
- Configuracoes multiplas duplicam caches por runtime, como esperado.
- Evitar copiar grandes estruturas por query.
