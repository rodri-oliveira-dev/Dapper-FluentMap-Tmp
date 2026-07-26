# 01 - Configuration Lifecycle

## Current Behavior

`FluentMapper` e a fachada publica global do core. Ela possui:

- `_registry`: instancia estatica de `MappingRegistry`;
- `_configuration`: instancia estatica de `FluentMapConfiguration`;
- `EntityMaps`: campo publico `ConcurrentDictionary<Type, IEntityMap>` apontando para o storage do registry;
- `TypeConventions`: campo publico `ConcurrentDictionary<Type, IList<Convention>>` apontando para o storage do registry.

`FluentMapper.Initialize(Action<FluentMapConfiguration>)` nao cria snapshot e nao marca a configuracao como concluida. Ele apenas executa o callback recebido sobre a mesma instancia estatica de `FluentMapConfiguration`.

As APIs publicas que podem alterar configuracao sao:

- `FluentMapper.Initialize(...)`;
- `FluentMapConfiguration.AddMap<TEntity>(IEntityMap<TEntity>)`;
- `FluentMapConfiguration.AddMap<TMap>()`;
- `FluentMapConfiguration.AddProfile<TMap>()`;
- `FluentMapConfiguration.AddMapsFromAssembly(...)`;
- `FluentMapConfiguration.AddMapsFromAssemblyContaining<TMarker>()`;
- `FluentMapConfiguration.AddConvention<TConvention>()` combinado com `ForEntity(...)`, `ForEntitiesInAssembly(...)` ou `ForEntitiesInCurrentAssembly(...)`;
- `FluentMapConfiguration.UseNamingPolicy(...)` combinado com os mesmos destinos de convention;
- `FluentMapConfigurationExtensions.ApplyMapsFromAssemblies(...)`;
- mutacao direta de `FluentMapper.EntityMaps`;
- mutacao direta de `FluentMapper.TypeConventions`.

As estruturas static/global atuais sao:

- `FluentMapper._registry`;
- `FluentMapper._configuration`;
- `FluentMapper.EntityMaps`;
- `FluentMapper.TypeConventions`;
- caches internos de `MappingRegistry`;
- registro global de type maps do Dapper via `SqlMapper.SetTypeMap`;
- cache legado protegido `MultiTypeMap.TypePropertyMapCache`, preservado por compatibilidade mas nao usado pelo core atual.

`SqlMapper.SetTypeMap` e chamado em:

- `MappingRegistry.AddEntityMap(...)`, depois de validar e adicionar um default map;
- `MappingRegistry.AddConvention(...)`, depois de adicionar uma convention/naming policy;
- `MappingRegistry.Reset(...)`, para remover type maps dos tipos informados nos testes;
- testes de caracterizacao que instalam type maps customizados diretamente.

`SqlMapper.SetTypeMap` nao e chamado por `AddProfile<TMap>()` e nao e chamado por `QueryMapped<TEntity,TProfile>()`.

Invalidacao de cache atual:

- `AddEntityMap(...)` invalida entradas de property-map e materialization-plan cache do tipo e reinstala o type map do Dapper;
- `AddProfileMap(...)` invalida caches do tipo, mas nao troca o type map global do Dapper;
- `AddConvention(...)` invalida caches do tipo e reinstala o type map do Dapper;
- `Reset(...)` limpa maps, profiles, conventions, property-map cache, materialization-plan cache e remove type maps do Dapper para os tipos informados;
- mutacao direta de `EntityMaps` ou `TypeConventions` nao passa pelo registry e pode bypassar validacao, invalidacao e instalacao de type map.

Profiles evitam mutacao global porque sao armazenados em `MappingRegistry.ProfileMaps[(EntityType, ProfileType)]` e selecionados pelo caminho `QueryMapped<TEntity,TProfile>()`. A chave de cache inclui `ProfileType`, e o materializer resolve o profile antes do loop de leitura. O default type map do Dapper permanece representando apenas a configuracao default.

Os testes resetam estado por `FluentMapper.Reset(...)`, que e interno e visivel ao assembly de testes. A suite principal, Dommel e generated-registration desabilitam paralelismo porque FluentMap e Dapper compartilham estado global por processo.

Nao existe atualmente nenhum conceito publico ou interno de `configuration completed`, `freeze`, `sealed`, `initialized` ou equivalente. Chamadas repetidas de `Initialize(...)` sao permitidas quando adicionam configuracao valida e falham pelas regras existentes quando duplicam default maps ou profiles.

Nao foi encontrada documentacao historica prometendo runtime reconfiguration concorrente. A documentacao existente recomenda inicializacao por `FluentMapper.Initialize(...)`, valida estado global por `Validate()` e usa profiles query-scoped para evitar troca temporaria de `SqlMapper.SetTypeMap`.

## Problem

O FluentMap depende de estado global/static proprio e do registro global de `ITypeMap` do Dapper. Isso e compativel com o uso historico de configurar uma vez no startup e consultar depois, mas e ambiguo para consumidores que interpretam `Initialize(...)`, conventions ou dicionarios publicos como API de reconfiguracao dinamica durante a execucao.

O risco arquitetural e que queries concorrentes observem configuracoes diferentes para o mesmo tipo, ou que caches internos e o registro global do Dapper sejam alterados enquanto materializers estao em uso.

## Supported Lifecycle

O lifecycle suportado passa a ser:

```text
Configuration Phase
        |
        v
Operational Phase
```

### Configuration Phase

Fase esperada durante startup da aplicacao ou antes do primeiro uso dos tipos configurados.

Permitido:

- registrar default maps;
- registrar profiles;
- registrar conventions e naming policies;
- usar assembly scanning quando apropriado para runtime normal;
- chamar `Validate()`;
- chamar `Explain<TEntity>()` para diagnostico;
- chamar `Initialize(...)` mais de uma vez para configuracao aditiva, desde que cada chamada respeite as regras de duplicidade e validacao existentes.

### Operational Phase

Comeca quando a aplicacao passa a executar queries que podem usar FluentMap ou o type map global do Dapper para os tipos configurados.

Permitido como operacao normal:

- `Dapper.Query<T>()` usando o default type map ja instalado;
- `QueryMapped<T>()` usando o default registry snapshot efetivo no momento de criar o plano;
- `QueryMapped<TEntity,TProfile>()` selecionando profile por operacao;
- `Validate()` e `Explain<...>()` como leituras diagnosticas sem side effects intencionais.

Durante esta fase, consumidores devem tratar a configuracao efetiva como read-only.

### Compatibility Runtime Mutation

Por compatibilidade, as APIs publicas atuais continuam podendo registrar maps, profiles e conventions depois de queries ja terem ocorrido. Esse uso e suportado apenas quando o consumidor garante quiescencia externa para os tipos afetados: sem queries concorrentes, sem materializers em execucao e sem outro componente alterando `SqlMapper.SetTypeMap`.

Nao ha garantia de determinismo para reconfiguracao concorrente em runtime.

Mutacao direta de `EntityMaps` e `TypeConventions` permanece uma superficie legada de compatibilidade, mas nao faz parte do caminho suportado para configuracao deterministica. Ela pode bypassar validacao, invalidacao de cache e instalacao do type map do Dapper.

## Invariants

- Queries nao devem depender de configuracao sendo alterada simultaneamente.
- Configuracao estabelecida antes da fase operacional deve produzir comportamento deterministico.
- `AddMap(...)`, conventions e naming policies aplicadas pelo registry devem invalidar caches do tipo afetado.
- `AddProfile<TMap>()` deve invalidar planos do tipo, mas nao trocar o type map global do Dapper.
- Profiles devem permanecer query-scoped.
- Dapper global state nao deve ser trocado temporariamente para implementar profiles.
- `Validate()` e `Explain<...>()` devem permanecer leituras diagnosticas sem instalacao de type maps ou invalidacao de caches.
- Compatibilidade existente nao deve ser quebrada silenciosamente.
- Dicionarios publicos mutaveis nao devem ser tratados como caminho recomendado de configuracao nova.

## Goals

- Documentar o lifecycle oficial suportado.
- Distinguir configuracao normal, operacao read-only e mutacao legada compatibilizada.
- Registrar que runtime reconfiguration concorrente nao e contrato publico.
- Preservar source/binary compatibility.
- Preparar a Entrega 02 para encapsular estado sem assumir que mutabilidade publica pode ser removida imediatamente.
- Proteger o contrato com testes de caracterizacao focados.

## Non-Goals

- Eliminar todo estado global.
- Adicionar `Freeze()`, `Seal()`, `CompleteConfiguration()` ou API semelhante.
- Remover ou tornar obsoletos `EntityMaps` e `TypeConventions`.
- Tornar membros publicos apenas para teste.
- Reabilitar paralelismo de testes.
- Transformar FluentMap em container de DI.
- Mudar profiles para usar mutation scope de `SqlMapper.SetTypeMap`.
- Alterar Dommel.

## Compatibility Constraints

- `FluentMapper.Initialize(...)` deve manter sua assinatura e comportamento aditivo atual.
- `AddMap(new Map())`, `AddMap<TMap>()`, `AddProfile<TMap>()`, conventions, naming policies e scanning permanecem publicos.
- Duplicidades continuam falhando pelas regras ja existentes.
- `EntityMaps` e `TypeConventions` continuam publicos nesta entrega.
- `FluentMapper.Reset(...)` continua interno e voltado a testes.
- `Dapper.Query<T>()` continua usando o type map global default.
- `QueryMapped<TEntity,TProfile>()` continua selecionando profile por operacao.

## Proposed Contract

Consumidores devem configurar FluentMap durante startup, validar a configuracao e iniciar queries somente depois disso:

```csharp
FluentMapper.Initialize(config =>
{
    config.AddMap<CustomerMap>();
    config.AddProfile<LegacyCustomerMap>();
    config.UseNamingPolicy(NamingPolicy.SnakeCase).ForEntity<Order>();
});

FluentMapper.Validate();
```

Depois que queries comecarem, a configuracao deve ser tratada como read-only.

Quando uma aplicacao precisar alterar configuracao em runtime usando APIs existentes, ela deve serializar externamente essa transicao e garantir que nao ha queries concorrentes usando os tipos afetados. Esse uso existe por compatibilidade, mas nao e o modelo recomendado nem contrato de concorrencia.

Para SQL shapes alternativos da mesma entidade, o caminho suportado e profile query-scoped por `QueryMapped<TEntity,TProfile>()`, nao troca temporaria de `SqlMapper.SetTypeMap`.

## Alternatives Considered

### A - Documentation Contract Only

Aceita para esta entrega.

Motivos:

- menor risco de quebra;
- condiz com o historico de API publica mutavel;
- permite formalizar o contrato antes de encapsular estado;
- suficiente para preparar a Entrega 02.

### B - Soft Enforcement

Adiada.

Possibilidades futuras:

- diagnostics adicionais;
- API preferencial que exponha views read-only;
- avisos de documentacao XML;
- mecanismos internos de snapshot sem quebrar a fachada publica.

Motivo para adiar: ainda nao ha modelo de detecao confiavel de "primeira query" sem acoplar o core aos detalhes de uso do Dapper e `QueryMapped*`.

### C - Runtime Enforcement

Rejeitada nesta entrega.

Motivos:

- exigiria saber quando a aplicacao entrou na fase operacional;
- quebraria chamadas repetidas de `Initialize(...)` que hoje funcionam para configuracao aditiva;
- conflitaria com dicionarios publicos mutaveis preservados por compatibilidade;
- exigiria estrategia de versao/migracao para consumidores.

## Acceptance Criteria

- A estrutura `docs/sdd/etapa-6/` existe.
- O README da etapa lista as quatro entregas e status.
- O estado atual de APIs mutadoras, static/global state, `SetTypeMap`, cache, profiles, reset, paralelismo e ausencia de freeze esta documentado.
- A decisao de enforcement esta registrada em `decisions.md`.
- O README publico documenta o lifecycle de configuracao.
- Testes caracterizam `Initialize(...)` repetido aditivo.
- Testes caracterizam mutacao runtime compatibilizada sob acesso serializado.
- Testes caracterizam que mutacao direta de dicionario publico nao e caminho deterministico de configuracao porque bypassa instalacao de type map.
- `docs/sdd/fluentmap-risk-assessment.md` foi revisado para FM-RISK-001 sem marcar o risco como resolvido.
- Validacao obrigatoria foi executada: restore, build, tests e pack.
- `handoff.md` contem contexto suficiente para a Entrega 02.

## Risks / Residual Risks

- FM-RISK-001 permanece mitigado, nao resolvido: estado global e `SqlMapper.SetTypeMap` continuam existindo.
- FM-RISK-002 permanece aberto: dicionarios publicos mutaveis podem bypassar registry, validacao e cache.
- O contrato depende de disciplina do consumidor durante a fase operacional.
- A suite continua com paralelismo desabilitado.
- Entrega 02 nao deve assumir que os dicionarios publicos podem ser removidos em minor version.

## Validation Results

Environment:

- SDK: `10.0.302`
- test runner detected: VSTest with xUnit v3
- core target: `netstandard2.0`
- test target: `net10.0`

Localized validation:

```text
dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter "FullyQualifiedName~ConfigurationLifecycleTests"
```

Result:

- success;
- 3 tests passed.

Mandatory validation:

```text
dotnet restore .\Dapper.FluentMap.sln
dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build
dotnet pack .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-build --output .\artifacts\packages
```

Results:

- restore: success;
- build: success, 0 warnings, 0 errors;
- tests: success, 215 total tests passed:
  - core: 184;
  - Dommel: 7;
  - analyzers: 9;
  - generators: 14;
  - generated-registration integration: 1;
- pack: `Dapper.FluentMap.2.0.0.nupkg` created successfully.

Known pack warnings:

- `NU5125` for legacy `PackageLicenseUrl`;
- NuGet README recommendation.

These warnings are pre-existing package metadata debt tracked outside this delivery.
