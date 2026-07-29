# Etapa 11 Status

## Objetivo

Definir discovery e arquitetura para Configuration Isolation & Dependency
Injection, preservando a API estatica historica como camada de compatibilidade
e preparando configuracoes imutaveis com runtime isolado.

## Concluido

- Executado `git status` antes de alteracoes.
- Confirmada branch `feature/etapa-3`; nao estamos em `master`.
- Identificado item nao rastreado preexistente `src/Dapper.FluentMap/etapas/`, deixado intacto.
- Lido `README.md`.
- Examinada `Dapper.FluentMap.sln`.
- Examinados core, Dommel, analyzers, generators, testes, AOT smoke e benchmarks nos pontos relacionados a configuracao, registry, caches, materializacao, query APIs, generated materializers, converters, persistence metadata e diagnostics.
- Lidos `.sdd/etapa-10/FINAL-REPORT.md` e `.sdd/etapa-10/STATUS.md`.
- Consultados relatorios finais/status das Etapas 7, 8 e 9.
- Criado `00-configuration-state-discovery.md`.
- Criado `01-historical-configuration-issues.md`.
- Criado `02-configuration-isolation-spec.md`.
- Criado `03-configuration-model.md`.
- Criado `DECISIONS.md`.
- Criado este `STATUS.md`.
- Validado que as ADRs existentes continuam compativeis com o incremento 11.2.
- Adicionada ADR-13 para o naming do modelo inicial.
- Implementado `FluentMapConfigurationBuilder`.
- Implementado `ImmutableFluentMapConfiguration` com snapshots read-only de maps, profiles, conventions/naming, generated materializers, persistence metadata e converter metadata.
- `FluentMapConfiguration` e `FluentConventionConfiguration` foram desacopladas do singleton global por registry injetado internamente, preservando os construtores/APIs publicas existentes.
- `MappingRegistry` agora pode operar sem instalar type maps globais do Dapper, permitindo builders independentes sem colisao process-wide.
- Criados testes de empty configuration, single map, multiple maps, convention, naming, inheritance, profiles, converters, generated registrations, duplicate maps, invalid map, Build, immutability, independent configurations e concurrent reads.

## Em andamento

- Revisao final de diff.
- Commit semantico.

## Proximos passos

1. Extrair runtime isolado a partir de `MappingRegistry`.
2. Adaptar `QueryMapped*`/`MappedGridReader` para runtime default e planejar overloads por runtime.
3. Reescrever `FluentMapper` como bridge de compatibilidade, preservando `Initialize` aditivo.
4. Projetar DI em incremento separado.
5. Migrar testes de isolamento/concurrencia para runtime instanciado.
6. Endurecer documentacao e limites de Dommel/Dapper process-wide.

## Decisoes relevantes

- Builder mutavel deve ser separado da configuracao imutavel.
- `Build()` e o limite apos o qual maps, conventions, profiles e converter metadata nao mudam.
- `FluentMapConfigurationBuilder` e o builder publico inicial.
- `ImmutableFluentMapConfiguration` e o snapshot imutavel publico inicial.
- `FluentMapConfiguration` permanece a fachada mutavel historica por compatibilidade.
- Caches derivados pertencem ao runtime futuro, nao ao estado global.
- `FluentMapper` deve delegar ao runtime default em incremento futuro.
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
- `FluentMapConfigurationBuilder`.
- `ImmutableFluentMapConfiguration`.
- `EntityMappingConfiguration`.
- `ProfileMappingConfiguration`.
- `ConventionMappingConfiguration`.
- `PropertyMappingConfiguration`.
- `GeneratedMaterializerConfiguration`.
- `FluentConventionConfiguration`.
- `GeneratedMaterializerDescriptor` registration.
- `AddGeneratedMappings()` emitido pelo generator.

## Backward compatibility

- Nenhuma API estatica foi removida na Etapa 11.
- `Initialize` aditivo foi preservado.
- Dicionarios publicos mutaveis continuam existindo por compatibilidade, mas novas APIs expõem snapshots/imutabilidade.
- `Dapper.Query<T>()` continua funcionando para o runtime default por `SqlMapper.SetTypeMap`.
- O builder novo nao instala type maps globais do Dapper.
- APIs novas por runtime continuam futuras e opt-in.

## Riscos conhecidos

- Dapper type maps sao globais por tipo e nao selecionam runtime por chamada.
- Dommel resolvers/builders sao globais.
- Mutacao direta de dicionarios publicos bypassa validacao, invalidacao e instalacao de type map.
- Interfaces publicas expõem `IList`, dificultando congelamento sem descritores internos.
- Runtime isolado ainda nao foi extraido; APIs `QueryMapped*` atuais continuam no registry default.
- Converter instances podem ser reutilizadas concorrentemente.
- Generated materializers por instancia/delegate ainda usam fallback runtime.
- Native AOT completo continua fora do contrato atual.

## Arquivos importantes

- `.sdd/etapa-11/00-configuration-state-discovery.md`
- `.sdd/etapa-11/01-historical-configuration-issues.md`
- `.sdd/etapa-11/02-configuration-isolation-spec.md`
- `.sdd/etapa-11/03-configuration-model.md`
- `.sdd/etapa-11/DECISIONS.md`
- `.sdd/etapa-11/STATUS.md`
- `README.md`
- `src/Dapper.FluentMap/Configuration/FluentMapConfigurationBuilder.cs`
- `src/Dapper.FluentMap/Configuration/ImmutableFluentMapConfiguration.cs`
- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `src/Dapper.FluentMap/Configuration/FluentConventionConfiguration.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `test/Dapper.FluentMap.Tests/ImmutableConfigurationModelTests.cs`

## Validacao do Prompt 11.1

- `dotnet restore ./Dapper.FluentMap.sln`: sucesso.
- `dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore`: sucesso, 0 warnings, 0 errors.
- `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build`: sucesso, 402 testes aprovados no total.
- `dotnet pack`: nao executado; o prompt 11.1 alterou somente documentacao SDD e nao mudou empacotamento ou assemblies.

## Validacao do Prompt 11.2

- `dotnet build ./src/Dapper.FluentMap/Dapper.FluentMap.csproj --configuration Release`: sucesso, 0 warnings, 0 errors.
- `dotnet test ./test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj --configuration Release`: sucesso, 347 testes aprovados.
- `dotnet restore ./Dapper.FluentMap.sln`: sucesso.
- `dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore`: sucesso, 0 warnings, 0 errors.
- `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build`: sucesso, 419 testes aprovados no total.
- `dotnet pack`: nao executado; o prompt 11.2 nao alterou empacotamento nem metadata de pacote.

## Ultimo prompt executado

Ultimo prompt executado: 11.2
