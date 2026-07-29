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
- Criado `04-isolated-runtime.md`.
- Criado `05-performance-impact.md`.
- Implementado `FluentMapRuntime` associado a `ImmutableFluentMapConfiguration`.
- `MappedRowMaterializer` passou a receber runtime e deixou de consultar `FluentMapper.Registry` diretamente.
- `MappedGridReader` passou a carregar o runtime usado por `ReadMapped<T>()` e `ReadMapped<T, TProfile>()`.
- `FluentMapper` passou a delegar `Validate()` e `Explain<T>()` ao runtime global de compatibilidade.
- Caches de property lookup, generated lookup e materialization plan agora ficam escopados ao registry possuido por cada runtime isolado.
- Generated materializers registrados no builder/snapshot sao reconstruidos por runtime, sem registry global compartilhado.
- Adicionados entry points de instancia no runtime para `QueryMapped`, profile, unbuffered sync, async streaming e `QueryMultipleMapped`.
- Criados testes de runtime isolado para duas configuracoes da mesma entidade, mesmo profile type em configuracoes diferentes, generated materializers, converters, nested mappings, cache isolation, `ReadMapped`, unbuffered, async streaming, diagnostics e concorrencia.
- Benchmarks existentes foram estendidos com cenarios `RuntimeQueryMapped*` comparaveis aos helpers estaticos.
- `README.md` atualizado para documentar `FluentMapRuntime` e os limites restantes de `FluentMapper`, Dapper puro e Dommel.
- Criado `06-compatibility-bridge.md`.
- `ImmutableFluentMapConfiguration.CreateRuntime()` foi adicionado como entry point ergonomico para runtime isolado.
- `FluentMapper.Configuration` e `FluentMapper.Runtime` foram expostos para o runtime/configuracao default publicados pela bridge estatica.
- `FluentMapper.Initialize(...)` agora publica um runtime default criado de snapshot imutavel e serializa inicializacoes concorrentes.
- A bridge estatica preserva `Initialize` aditivo e reinstala type maps Dapper para maps/conventions default.
- `GetEntityMaps()` e `GetTypeConventions()` continuam retornando snapshots das colecoes historicas registradas, preservando instancias de maps/conventions.
- Dommel foi mantido como bridge process-wide sobre as colecoes legadas porque depende de metadata especifica de `DommelEntityMap` e `DommelPropertyMap`.
- Criados testes de compatibility bridge para runtime default, API configuration-aware, equivalencia legacy/new, repeated Initialize, Initialize concorrente, colecoes legadas, generated materializers, profiles e converters.
- Criado `07-dependency-injection-spec.md`.
- Criado projeto `Dapper.FluentMap.DependencyInjection` em pacote separado.
- Implementado `services.AddFluentMap(builder => ...)`.
- `AddFluentMap(...)` constroi e valida a configuracao imediatamente.
- `ImmutableFluentMapConfiguration` e `FluentMapRuntime` sao registrados como singletons.
- O pacote DI depende de `Microsoft.Extensions.DependencyInjection.Abstractions` e nao adiciona dependencias de Hosting, Options, ASP.NET Core, Dommel ou runtime DI concreto.
- Criados testes de registration, service resolution, singleton identity, invalid config, explicit registration, profiles, multiple service providers, independent configurations e concurrency.
- Adicionado teste de generated registration via DI no projeto do source generator.
- `README.md` atualizado com instalacao e uso de `Dapper.FluentMap.DependencyInjection`.
- Criado `08-isolation-matrix.md`.
- Criado `09-migration-guide.md`.
- Adicionados testes de hardening para runtimes isolados usando a mesma entidade com mappings diferentes, mesmo runtime em multiplas threads, generated materializers concorrentes, profiles/converters/diagnostics concorrentes, invalid configuration isolada, DI concorrente e inicializacao estatica controlada por `Barrier`.
- Adicionado teste que prova a limitacao estrutural de `Dapper.Query<T>()`: o caminho puro do Dapper usa somente o type map global publicado, enquanto `runtime.QueryMapped<T>()` usa a configuracao isolada.
- Adicionado teste que prova que Dommel resolve metadata pela configuracao legada process-wide e nao por runtimes isolados do core.
- Atualizada a classificacao final da issue #101 como `Partially resolved`.
- Repetido benchmark smoke de `MaterializationSteadyStateBenchmarks*QueryMappedSimple*` e registrado resultado no relatorio de performance.

## Em andamento

- Nenhum item em andamento para o prompt 11.6.

## Proximos passos

1. Endurecer Dommel em design proprio, mantendo honestos os limites process-wide de `DommelMapper`.
2. Avaliar full benchmark antes de release.
3. Migrar gradualmente testes antigos de isolamento/concurrencia para runtime instanciado quando isso reduzir dependencia de reset global.
4. Estender smoke Native AOT para cobrir o pacote DI publicado com registro gerado, se a matriz de release exigir esse contrato.

## Decisoes relevantes

- Builder mutavel deve ser separado da configuracao imutavel.
- `Build()` e o limite apos o qual maps, conventions, profiles e converter metadata nao mudam.
- `FluentMapConfigurationBuilder` e o builder publico inicial.
- `ImmutableFluentMapConfiguration` e o snapshot imutavel publico inicial.
- `FluentMapConfiguration` permanece a fachada mutavel historica por compatibilidade.
- Caches derivados pertencem ao runtime futuro, nao ao estado global.
- `FluentMapper` delega ao runtime default para diagnostics e query helpers estaticos.
- `Initialize` deve continuar aditivo inicialmente.
- `Reset` nao e solucao arquitetural principal.
- `FluentMapper.Configuration` e `FluentMapper.Runtime` representam a configuracao/runtime default publicados.
- `ImmutableFluentMapConfiguration.CreateRuntime()` e o caminho ergonomico para criar runtime isolado.
- DI deve registrar configuracao e runtime como singleton.
- DI fica em pacote separado `Dapper.FluentMap.DependencyInjection`.
- `AddFluentMap(...)` faz fail-fast em configuracao invalida.
- Named/keyed configurations nao foram adicionadas no prompt 11.5.
- Dommel permanece bridge process-wide ate design especifico.
- Native AOT nao deve ser prometido alem do que os smokes validam.

## Estado global identificado

- `FluentMapper._builderRegistry`.
- `FluentMapper._runtime`.
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
- Runtime isolado foi introduzido para os entry points controlados pelo FluentMap.
- Converter instances podem ser reutilizadas concorrentemente.
- Generated materializers por instancia/delegate ainda usam fallback runtime.
- Native AOT completo continua fora do contrato atual.

## Arquivos importantes

- `.sdd/etapa-11/00-configuration-state-discovery.md`
- `.sdd/etapa-11/01-historical-configuration-issues.md`
- `.sdd/etapa-11/02-configuration-isolation-spec.md`
- `.sdd/etapa-11/03-configuration-model.md`
- `.sdd/etapa-11/04-isolated-runtime.md`
- `.sdd/etapa-11/05-performance-impact.md`
- `.sdd/etapa-11/06-compatibility-bridge.md`
- `.sdd/etapa-11/08-isolation-matrix.md`
- `.sdd/etapa-11/09-migration-guide.md`
- `.sdd/etapa-11/DECISIONS.md`
- `.sdd/etapa-11/STATUS.md`
- `README.md`
- `src/Dapper.FluentMap/Configuration/FluentMapConfigurationBuilder.cs`
- `src/Dapper.FluentMap/Configuration/ImmutableFluentMapConfiguration.cs`
- `src/Dapper.FluentMap/Configuration/RuntimeConfigurationRegistryFactory.cs`
- `src/Dapper.FluentMap/FluentMapRuntime.cs`
- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `src/Dapper.FluentMap/Configuration/FluentConventionConfiguration.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/Materialization/MappedRowMaterializer.cs`
- `src/Dapper.FluentMap/MappedGridReader.cs`
- `src/Dapper.FluentMap/QueryMappedExtensions.cs`
- `test/Dapper.FluentMap.Tests/ImmutableConfigurationModelTests.cs`
- `test/Dapper.FluentMap.Tests/IsolatedRuntimeTests.cs`
- `test/Dapper.FluentMap.Tests/ConfigurationIsolationHardeningTests.cs`
- `test/Dapper.FluentMap.Tests/CompatibilityBridgeTests.cs`
- `.sdd/etapa-11/07-dependency-injection-spec.md`
- `src/Dapper.FluentMap.DependencyInjection/Dapper.FluentMap.DependencyInjection.csproj`
- `src/Dapper.FluentMap.DependencyInjection/FluentMapServiceCollectionExtensions.cs`
- `test/Dapper.FluentMap.DependencyInjection.Tests/Dapper.FluentMap.DependencyInjection.Tests.csproj`
- `test/Dapper.FluentMap.DependencyInjection.Tests/FluentMapServiceCollectionExtensionsTests.cs`
- `benchmarks/Dapper.FluentMap.Benchmarks/Program.cs`

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

## Validacao do Prompt 11.3

- `dotnet build .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release`: sucesso, 0 warnings, 0 errors.
- `dotnet build .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release`: sucesso, 0 warnings, 0 errors.
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~IsolatedRuntimeTests"`: sucesso, 10 testes aprovados.
- `dotnet build .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release`: sucesso, 0 warnings, 0 errors.
- `dotnet run --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release -- --filter "*MaterializationSteadyStateBenchmarks*QueryMappedSimple*" --job Dry`: sucesso; smoke executou cenarios `Dry` e `ShortRun`, registrado em `05-performance-impact.md`.
- `dotnet restore .\Dapper.FluentMap.sln`: sucesso.
- `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`: sucesso, 0 warnings, 0 errors.
- `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`: sucesso, 429 testes aprovados.
- `dotnet pack .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-build --output .\artifacts\packages`: sucesso; criou `artifacts\packages\Dapper.FluentMap.2.0.0.nupkg`; warning existente `NU5125` sobre `licenseUrl` obsoleto.

## Validacao do Prompt 11.4

- Detectado runner de testes como VSTest: SDK `10.0.302`, sem `global.json`, sem `Directory.Build.props`/`Directory.Packages.props` e projetos de teste com `Microsoft.NET.Test.Sdk` + xUnit runner.
- `dotnet build .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release`: sucesso, 0 warnings, 0 errors.
- `dotnet build .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release`: sucesso, 0 warnings, 0 errors.
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~CompatibilityBridgeTests"`: sucesso, 5 testes aprovados.
- `dotnet build .\test\Dapper.FluentMap.Dommel.Tests\Dapper.FluentMap.Dommel.Tests.csproj --configuration Release`: sucesso, 0 warnings, 0 errors.
- `dotnet test .\test\Dapper.FluentMap.Dommel.Tests\Dapper.FluentMap.Dommel.Tests.csproj --configuration Release --no-build`: sucesso, 22 testes aprovados.
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --no-build`: sucesso, 362 testes aprovados.
- `dotnet restore .\Dapper.FluentMap.sln`: sucesso.
- `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`: sucesso, 0 warnings, 0 errors.
- `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`: sucesso, 434 testes aprovados no total.
- `dotnet pack .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-build --output .\artifacts\packages`: sucesso; criou `artifacts\packages\Dapper.FluentMap.2.0.0.nupkg`; warning existente `NU5125` sobre `licenseUrl` obsoleto.
- Inspecionado `artifacts\packages\Dapper.FluentMap.2.0.0.nupkg`: contem nuspec/metadados e `lib/netstandard2.0/Dapper.FluentMap.dll` + XML; nao contem projetos de teste.
- Ferramenta dedicada de API compatibility nao foi encontrada no projeto atual; ha referencias planejadas para Etapa 12, mas sem `ApiCompat`, `PublicApiAnalyzers` ou package validation configurados nesta etapa.

## Validacao do Prompt 11.5

- Detectado runner de testes como VSTest: SDK `10.0.302`, sem `global.json`, sem `Directory.Build.props`/`Directory.Packages.props` e projetos de teste com `Microsoft.NET.Test.Sdk` + xUnit runner.
- `dotnet restore .\Dapper.FluentMap.sln`: sucesso.
- `dotnet build .\src\Dapper.FluentMap.DependencyInjection\Dapper.FluentMap.DependencyInjection.csproj --configuration Release --no-restore`: sucesso, 0 warnings, 0 errors.
- `dotnet build .\test\Dapper.FluentMap.DependencyInjection.Tests\Dapper.FluentMap.DependencyInjection.Tests.csproj --configuration Release --no-restore`: sucesso, 0 warnings, 0 errors.
- `dotnet build .\test\Dapper.FluentMap.GeneratedRegistration.Tests\Dapper.FluentMap.GeneratedRegistration.Tests.csproj --configuration Release --no-restore`: sucesso, 0 warnings, 0 errors.
- `dotnet test .\test\Dapper.FluentMap.DependencyInjection.Tests\Dapper.FluentMap.DependencyInjection.Tests.csproj --configuration Release --no-build`: sucesso, 8 testes aprovados.
- `dotnet test .\test\Dapper.FluentMap.GeneratedRegistration.Tests\Dapper.FluentMap.GeneratedRegistration.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~GeneratedRegistrationShouldWorkThroughDependencyInjection"`: sucesso, 1 teste aprovado.
- `dotnet run --project .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:DefineConstants=AOT_SMOKE_DI_EXPLICIT`: sucesso; binario retornou `di-explicit:ok`.
- `dotnet run --project .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:DefineConstants=AOT_SMOKE_DI_GENERATED`: sucesso; binario retornou `di-generated:ok`.
- `dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:PublishTrimmed=true -p:DefineConstants=AOT_SMOKE_DI_EXPLICIT --output .\.tmp\aot-smoke\di-explicit-trimmed`: sucesso; warning conhecido `IL2104` do Dapper.
- `.\.tmp\aot-smoke\di-explicit-trimmed\Dapper.FluentMap.AotSmoke.exe`: sucesso; binario retornou `di-explicit:ok`.
- `dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:PublishTrimmed=true -p:DefineConstants=AOT_SMOKE_DI_GENERATED --output .\.tmp\aot-smoke\di-generated-trimmed`: sucesso; warnings conhecidos `IL2104` de `Dapper.FluentMap`/Dapper.
- `.\.tmp\aot-smoke\di-generated-trimmed\Dapper.FluentMap.AotSmoke.exe`: sucesso; binario retornou `di-generated:ok`.
- `dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:PublishAot=true -p:DefineConstants=AOT_SMOKE_DI_EXPLICIT --output .\.tmp\aot-smoke\di-explicit-aot`: bloqueado pelo ambiente; erro `Platform linker not found`, exigindo prerequisites de Native AOT/Desktop Development for C++.
- `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`: sucesso, 0 warnings, 0 errors.
- `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`: sucesso, 443 testes aprovados no total.
- `dotnet pack .\src\Dapper.FluentMap.DependencyInjection\Dapper.FluentMap.DependencyInjection.csproj --configuration Release --no-build --output .\artifacts\packages`: sucesso; criou `artifacts\packages\Dapper.FluentMap.DependencyInjection.2.0.0.nupkg`.
- Inspecionado `artifacts\packages\Dapper.FluentMap.DependencyInjection.2.0.0.nupkg`: contem `README.md`, `lib/netstandard2.0/Dapper.FluentMap.DependencyInjection.dll`, XML documentation e nuspec; dependencias `Dapper.FluentMap` 2.0.0 e `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.10; nao contem projetos de teste.

## Validacao do Prompt 11.6

- Detectado runner de testes como VSTest: SDK `10.0.302`, sem `global.json`, sem `Directory.Build.props`/`Directory.Packages.props` e projetos de teste com `Microsoft.NET.Test.Sdk` + xUnit runner.
- Tentativa inicial de builds localizados em paralelo bloqueou no arquivo intermediario do core por `VBCSCompiler` (`CS2012`); repeticao sequencial passou. A falha foi de concorrencia entre comandos de build, nao de produto/teste.
- `dotnet build .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release`: sucesso, 0 warnings, 0 errors.
- `dotnet build .\test\Dapper.FluentMap.DependencyInjection.Tests\Dapper.FluentMap.DependencyInjection.Tests.csproj --configuration Release`: sucesso, 0 warnings, 0 errors.
- `dotnet build .\test\Dapper.FluentMap.Dommel.Tests\Dapper.FluentMap.Dommel.Tests.csproj --configuration Release`: sucesso, 0 warnings, 0 errors.
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~ConfigurationIsolationHardeningTests|FullyQualifiedName~CompatibilityBridgeTests"`: sucesso, 12 testes aprovados antes do reforco adicional de converter.
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~ConfigurationIsolationHardeningTests"`: sucesso, 8 testes aprovados apos adicionar a prova do mesmo converter em configuracoes diferentes.
- `dotnet test .\test\Dapper.FluentMap.DependencyInjection.Tests\Dapper.FluentMap.DependencyInjection.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~FluentMapServiceCollectionExtensionsTests"`: sucesso, 9 testes aprovados.
- `dotnet test .\test\Dapper.FluentMap.Dommel.Tests\Dapper.FluentMap.Dommel.Tests.csproj --configuration Release --no-build --filter "FullyQualifiedName~DommelResolversShouldUseOnlyLegacyProcessWideConfiguration"`: sucesso, 1 teste aprovado.
- `dotnet run --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release -- --filter "*MaterializationSteadyStateBenchmarks*QueryMappedSimple*" --job Dry`: sucesso; smoke executou 20 benchmarks `Dry`/`ShortRun`, registrado em `05-performance-impact.md`.
- `dotnet restore .\Dapper.FluentMap.sln`: sucesso.
- `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`: sucesso, 0 warnings, 0 errors.
- `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`: sucesso, 453 testes aprovados no total.
- `dotnet pack`: nao executado; o prompt 11.6 alterou testes e documentacao SDD, sem mudanca de empacotamento, metadata de pacote ou assemblies de producao.

## Ultimo prompt executado

Ultimo prompt executado: 11.6
