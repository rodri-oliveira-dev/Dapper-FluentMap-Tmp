# Etapa 11 Status

## Objetivo

Definir discovery e arquitetura para Configuration Isolation & Dependency Injection, preservando a API estatica historica como camada de compatibilidade e preparando configuracoes imutaveis com runtime isolado.

## Concluido

- Executado `git status` antes de alteracoes.
- Confirmada branch `feature/etapa-3`; nao estamos em `master`.
- Identificado item nao rastreado preexistente `src/Dapper.FluentMap/etapas/`, deixado intacto.
- Lido `README.md`.
- Examinada `Dapper.FluentMap.sln`.
- Examinados core, Dommel, analyzers, generators, testes, AOT smoke e benchmarks nos pontos relacionados a configuracao, registry, caches, materializacao, query APIs, generated materializers, converters, persistence metadata e diagnostics.
- Lidos `.sdd/etapa-10/FINAL-REPORT.md` e `.sdd/etapa-10/STATUS.md`.
- Consultados relatórios finais/status das Etapas 7, 8 e 9.
- Confirmado que `.sdd/etapa-11/` nao existia e criada a pasta.
- Pesquisadas issues historicas #101, #79 e #84 no projeto original.
- Criado `00-configuration-state-discovery.md`.
- Criado `01-historical-configuration-issues.md`.
- Criado `02-configuration-isolation-spec.md`.
- Criado `DECISIONS.md`.
- Criado este `STATUS.md`.

## Em andamento

- Revisao final de diff.
- Commit semantico do SDD.

## Proximos passos

1. Implementar modelo inicial de builder/configuracao imutavel sem mudar behavior publico.
2. Extrair runtime isolado a partir de `MappingRegistry`.
3. Adaptar `QueryMapped*`/`MappedGridReader` para runtime default e planejar overloads por runtime.
4. Reescrever `FluentMapper` como bridge de compatibilidade, preservando `Initialize` aditivo.
5. Projetar DI em incremento separado.
6. Migrar testes de isolamento/concurrencia para runtime instanciado.
7. Endurecer documentacao e limites de Dommel/Dapper process-wide.

## Decisoes relevantes

- Builder mutavel deve ser separado da configuracao imutavel.
- `Build()` e o limite apos o qual maps, conventions, profiles e converter metadata nao mudam.
- Caches derivados pertencem ao runtime, nao ao estado global.
- `FluentMapper` deve delegar ao runtime default.
- `Initialize` deve continuar aditivo inicialmente.
- `Reset` nao e solucao arquitetural principal.
- DI deve registrar configuracao e runtime como singleton.
- Dommel permanece bridge process-wide ate design especifico.
- Native AOT nao deve ser prometido alem do que os smokes validam.

## Estado global identificado

- `FluentMapper._registry`.
- `FluentMapper._configuration`.
- `FluentMapper.EntityMaps`.
- `FluentMapper.TypeConventions`.
- `MappingRegistry.ProfileMaps`.
- `MappingRegistry._propertyMapCache`.
- `MappingRegistry._materializationPlanCache`.
- `MappingRegistry._generatedMaterializers`.
- `SqlMapper.SetTypeMap` por entidade.
- `DommelMapper.SetColumnNameResolver`, `SetKeyPropertyResolver`, `SetTableNameResolver`, `SetPropertyResolver`.
- `DommelMapper.AddSqlBuilder`.
- Estado global de Dapper TypeHandlers consultado por `DapperTypeHandlerAdapter`.
- Campos estaticos generated por assembly consumidor.
- `MultiTypeMap.TypePropertyMapCache` legado sem uso encontrado.

## APIs afetadas

- `FluentMapper.Initialize`.
- `FluentMapper.Validate`.
- `FluentMapper.Explain`.
- `FluentMapper.GetEntityMaps`.
- `FluentMapper.GetTypeConventions`.
- `FluentMapper.EntityMaps`.
- `FluentMapper.TypeConventions`.
- `FluentMapConfiguration`.
- `FluentConventionConfiguration`.
- `QueryMapped*`.
- `QueryMultipleMapped`.
- `MappedGridReader.ReadMapped*`.
- `QueryMappedUnbuffered*`.
- `GeneratedMaterializerDescriptor` registration.
- `AddGeneratedMappings()` emitido pelo generator.
- `ForDommel()`.
- Resolvers Dommel.

## Backward compatibility

- Nenhuma API estatica deve ser removida na Etapa 11.
- `Initialize` aditivo deve ser preservado na bridge estatica.
- Dicionarios publicos mutaveis devem continuar existindo por compatibilidade, mas novas APIs devem expor snapshots/imutabilidade.
- `Dapper.Query<T>()` deve continuar funcionando para o runtime default por `SqlMapper.SetTypeMap`.
- APIs novas por runtime devem ser opt-in.

## Riscos conhecidos

- Dapper type maps sao globais por tipo e nao selecionam runtime por chamada.
- Dommel resolvers/builders sao globais.
- Mutacao direta de dicionarios publicos bypassa validacao, invalidacao e instalacao de type map.
- Interfaces publicas expõem `IList`, dificultando congelamento sem descritores internos.
- Caches atuais nao possuem generation id e dependem de invalidacao por tipo.
- Converter instances podem ser reutilizadas concorrentemente.
- Generated materializers por instancia/delegate ainda usam fallback runtime.
- Native AOT completo continua fora do contrato atual.

## Arquivos importantes

- `.sdd/etapa-11/00-configuration-state-discovery.md`
- `.sdd/etapa-11/01-historical-configuration-issues.md`
- `.sdd/etapa-11/02-configuration-isolation-spec.md`
- `.sdd/etapa-11/DECISIONS.md`
- `.sdd/etapa-11/STATUS.md`
- `src/Dapper.FluentMap/FluentMapper.cs`
- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `src/Dapper.FluentMap/Configuration/FluentConventionConfiguration.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/Materialization/MappedRowMaterializer.cs`
- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`
- `src/Dapper.FluentMap/QueryMappedExtensions.cs`
- `src/Dapper.FluentMap/MappedGridReader.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentMapTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentTypeMap.cs`
- `src/Dapper.FluentMap.Dommel/FluentMapConfigurationExtensions.cs`
- `src/Dapper.FluentMap.Dommel/Resolvers/*`
- `test/Dapper.FluentMap.Tests/ConfigurationLifecycleTests.cs`
- `test/Dapper.FluentMap.Tests/MappingStateEncapsulationTests.cs`
- `test/Dapper.FluentMap.Tests/MappingRegistryTests.cs`

## Validacao do Prompt 11.1

- `dotnet restore ./Dapper.FluentMap.sln`: sucesso.
- `dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore`: sucesso, 0 warnings, 0 errors.
- `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build`: sucesso, 402 testes aprovados no total.
- `dotnet pack`: nao executado; este prompt alterou somente documentacao SDD e nao mudou empacotamento ou assemblies.

## Último prompt executado

Último prompt executado: 11.1
