# Decisoes Da Etapa 6

Registre aqui apenas decisoes arquiteturais necessarias as proximas entregas.

## E6-D001 - Configuration Lifecycle Contract

O lifecycle publico suportado do FluentMap passa a ser descrito em duas fases:

```text
Configuration Phase
        |
        v
Operational Phase
```

Durante a `Configuration Phase`, consumidores devem registrar maps, profiles, conventions e naming policies, e podem chamar `Validate()` para falhar cedo. Chamadas repetidas de `FluentMapper.Initialize(...)` continuam permitidas para configuracao aditiva, sujeitas as validacoes e regras de duplicidade ja existentes.

Ao iniciar queries por `Dapper.Query<T>()`, `QueryMapped<T>()` ou APIs equivalentes, a aplicacao entra na `Operational Phase` para os tipos usados. Nessa fase, a configuracao efetiva deve ser tratada como read-only pelo consumidor.

Mutacoes depois do inicio das queries permanecem possiveis por compatibilidade binaria/fonte, mas so sao suportadas quando o consumidor garante quiescencia externa: sem queries concorrentes, sem leitores/materializers em execucao para os tipos afetados e com entendimento de que `SqlMapper.SetTypeMap` altera estado global do Dapper. O FluentMap nao garante determinismo para reconfiguracao concorrente em runtime.

## E6-D002 - Documentation Contract Only For Delivery 01

Esta entrega escolhe `A. Documentation Contract Only`.

Justificativa:

- `FluentMapper.Initialize(...)` historicamente executa mutacoes imediatas sobre uma instancia estatica de `FluentMapConfiguration`.
- `AddMap`, `AddProfile`, conventions e naming policies sao APIs publicas aditivas ou historicas.
- `FluentMapper.EntityMaps` e `FluentMapper.TypeConventions` continuam publicos e mutaveis por compatibilidade.
- `MappingRegistry.Reset(...)` e interno e usado para isolamento de testes, nao como contrato publico de runtime.
- Adicionar `Freeze()`, `Seal()` ou exceptions depois da primeira query quebraria comportamento atualmente possivel sem uma estrategia de migracao.

A entrega documenta o contrato, adiciona testes de caracterizacao e prepara a Entrega 02 para encapsulamento de estado. Nenhuma API publica foi removida, nenhuma API de freeze foi adicionada e nenhum enforcement de runtime foi introduzido.

## E6-D003 - Profiles Remain Query-Scoped

Profiles continuam sendo alternativa query-scoped para SQL shapes diferentes da mesma entidade.

O contrato preservado e:

- `Dapper.Query<T>()` usa apenas o default map instalado no type map global do Dapper.
- `QueryMapped<TEntity,TProfile>()` seleciona o profile por operacao.
- Profiles nao trocam `SqlMapper.SetTypeMap` temporariamente.
- Conventions e naming policies permanecem por entidade e sao lidas por profiles sem mutacao global por query.

Qualquer entrega futura que tente aplicar profiles ao caminho `Dapper.Query<T>()`, multi-mapping ou Dommel deve tratar isso como nova decisao arquitetural.

## E6-D004 - Mapping State Read-Only Snapshots

Entrega 02 escolhe encapsulamento incremental sem breaking change.

`FluentMapper.EntityMaps` e `FluentMapper.TypeConventions` permanecem campos publicos mutaveis do mesmo tipo para preservar compatibilidade de fonte e binaria. Eles nao foram marcados com `[Obsolete]` nesta entrega porque isso poderia quebrar consumidores que tratam warnings como erros.

Novas APIs publicas de leitura foram adicionadas:

```csharp
FluentMapper.GetEntityMaps()
FluentMapper.GetTypeConventions()
```

Elas retornam snapshots read-only, nao o `ConcurrentDictionary` vivo nem listas mutaveis de conventions. O objetivo e oferecer uma superficie oficial para inspecao e migracao sem permitir mutacao acidental pelo novo caminho.

Toda mutacao oficial continua passando conceitualmente por:

```text
Consumer API
     |
     v
FluentMapper / FluentMapConfiguration
     |
     v
MappingRegistry
     |
     v
Validation
     |
     v
Cache invalidation
     |
     v
Dapper integration
```

Mutacoes diretas nos campos legados continuam possiveis, podem ignorar invariantes e exigem migracao futura de major version para serem removidas ou substituidas por propriedades read-only.

## E6-D005 - Dapper Compatibility Boundary

Entrega 03 cria uma fronteira interna explicita para detalhes de compatibilidade com Dapper no namespace `Dapper.FluentMap.Compatibility`.

Essa fronteira concentra:

- invocacao de TypeHandlers registrados no Dapper por `DapperTypeHandlerAdapter`;
- exposicao de property mappings ao Dapper por `DapperFluentPropertyTypeMap`;
- `IMemberMap` seguro para propriedades simples por `DapperPropertyMemberMap`;
- marker seguro para ignored/nested por `DapperIgnoredMemberMap`.

Nenhuma API publica foi adicionada. O objetivo e manter detalhes Dapper-specific fora do materializer e reduzir o numero de pontos onde uma mudanca interna do Dapper pode afetar o FluentMap.

## E6-D006 - Residual TypeHandler Reflection

Nao foi encontrada no Dapper `2.1.79` uma API publica que converta um `object` usando o TypeHandler registrado para um tipo arbitrario.

Por isso, a reflection residual para `SqlMapper.TypeHandlerCache<T>.Parse(object)` permanece, mas fica isolada em `DapperTypeHandlerAdapter`. Se a shape esperada nao existir em uma versao futura do Dapper, o FluentMap deve falhar com `FluentMapConfigurationException` diagnosticavel em vez de cair silenciosamente para `Convert.ChangeType`.

Esse risco fica `MITIGATED`, nao `RESOLVED`, ate existir alternativa publica suportada pelo Dapper ou ate o FluentMap deixar de precisar invocar handlers no materializer runtime.

## E6-D007 - Ignored Mapping Without Throwing PropertyInfo Sentinel

Entrega 03 remove `IgnoredPropertyInfo`.

Mappings ignored e nested deixam de passar por `CustomPropertyTypeMap` para retornar um `PropertyInfo` falso. O caminho atual retorna um `DapperIgnoredMemberMap`, que implementa `SqlMapper.IMemberMap` com propriedades seguras e nulas. `MultiTypeMap` reconhece esse marker e retorna `null` sem continuar para `DefaultTypeMap`, preservando o bloqueio de fallback.

Com isso, `FM-RISK-012` fica `RESOLVED`: nao ha mais sentinel `PropertyInfo` interno com membros que lancam `NotImplementedException`.

## E6-D008 - Dapper Upgrade Checklist

Qualquer upgrade futuro de Dapper deve revisar explicitamente:

- `SqlMapper.ITypeMap`;
- `SqlMapper.IMemberMap`;
- `DefaultTypeMap` constructor/member behavior;
- `SqlMapper.SetTypeMap` global state;
- `SqlMapper.HasTypeHandler`;
- `SqlMapper.TypeHandlerCache<T>.Parse(object)`;
- comportamento de fallback quando um mapper retorna `null`;
- testes `DapperCompatibilityAdapterTests`, `ValueObjectMaterializationTests`, `ConstructorMappingTests`, `NestedMaterializationSpikeTests`, `DapperIntegrationTests` e Dommel.

## E6-D009 - Generated Materializer Direction

O spike da Entrega 04 conclui `GO WITH CONSTRAINTS` para materializacao gerada.

A arquitetura futura recomendada e:

```text
QueryMapped
    |
    v
Generated materializer available and matching?
    | yes
    v
Generated materializer
    |
    no
    v
Runtime NestedMaterializationPlan fallback
```

Um caminho generated-only foi rejeitado como arquitetura default porque quebraria configuracao dinamica, assembly scanning, conventions nao geraveis, maps em assemblies externos sem manifest e a superficie legada de mutacao publica ainda preservada por compatibilidade.

## E6-D010 - Static Mapping Eligibility

Materializers gerados devem ser usados apenas quando o generator conseguir provar estaticamente o mapping efetivo.

Primeiro subconjunto elegivel:

- maps declarados na compilacao atual;
- `Map(...).ToColumn("literal")`;
- `Ignore()`;
- `IncludeBase<TBase>()` quando o base map tambem for geravel;
- profiles por `IProfileMap<TProfile>`;
- construtores publicos e setters publicos representaveis pelo symbol model.

Devem cair para fallback runtime:

- column names dinamicos;
- helper methods arbitrarios;
- assembly scanning;
- public dictionary mutation;
- conventions customizadas nao geraveis;
- naming policies aplicadas dinamicamente;
- maps de assemblies externos sem descriptor gerado.

## E6-D011 - AOT Claims Require Runtime Evidence

Generated materializers podem reduzir dependencia de `Expression.Compile`, `Activator` e reflection no hot path para casos estaticos, mas isso nao basta para declarar compatibilidade Native AOT.

Qualquer etapa futura deve separar:

- `Proven`: validado por build/publish/run;
- `Likely`: inferido de codigo gerado e analyzers;
- `Unknown`: dependente de Dapper, TypeHandlers, ambiente Native AOT ou configuracao dinamica.

As annotations `RequiresUnreferencedCode` e `RequiresDynamicCode` das APIs que podem usar fallback runtime nao devem ser removidas ate haver um caminho publico que garanta generated-only sem fallback reflection-based.

## E6-D012 - Generated TypeHandler Boundary

TypeHandlers permanecem a area mais sensivel para materializer gerado.

Codigo gerado nao deve espalhar reflection para internals do Dapper nem chamar APIs version-sensitive sem uma decisao propria. Uma etapa futura deve escolher entre:

- uma pequena API/boundary publica no core para conversao gerada;
- chamada direta gerada a uma shape publica do Dapper, aceitando diagnostico/compile failure em upgrades;
- fallback runtime quando TypeHandler for necessario.

A decisao E6-D006 permanece vigente ate essa escolha ser implementada e validada.
