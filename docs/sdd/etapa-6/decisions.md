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
