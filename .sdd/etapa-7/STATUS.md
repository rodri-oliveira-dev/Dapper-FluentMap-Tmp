# Etapa 7 Status

## Objetivo

Definir a arquitetura e a especificacao inicial para materializacao gerada no FluentMap, preservando o escopo da biblioteca em `IDataReader / IDataRecord -> metadata de mapping -> object graph` e mantendo fallback runtime.

## Concluido

- Lido `README.md`.
- Examinada a solution `Dapper.FluentMap.sln`.
- Examinados projetos core, analyzers, generators, testes, AOT smoke e runtime de materializacao.
- Executado `git status`.
- Examinados commits recentes, incluindo:
  - `63effef chore(fluentmap): evaluate generated materializer architecture`;
  - `dde0ee0 refactor(fluentmap): isolate dapper compatibility internals`;
  - etapas anteriores de profiles, Value Objects, nested mapping e source generator.
- Confirmado que `.sdd/etapa-7/` nao existia no inicio deste prompt.
- Criada a especificacao `.sdd/etapa-7/01-generated-materialization-architecture.md`.
- Criado o registro de decisoes `.sdd/etapa-7/DECISIONS.md`.
- Executado `dotnet restore ./Dapper.FluentMap.sln`: sucesso.
- Executado `dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore`: sucesso, 0 warnings, 0 errors.
- Executado `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build`: sucesso, 231 testes aprovados.
- Revisado o diff final para limitar o commit aos documentos da Etapa 7.
- Criada a especificacao `.sdd/etapa-7/02-performance-spec.md`.
- Adicionado projeto `benchmarks/Dapper.FluentMap.Benchmarks/` com BenchmarkDotNet isolado dos testes normais.
- Adicionado benchmark steady state para:
  - Dapper puro;
  - Dapper + FluentMap root mapping;
  - `QueryMapped<T>` simples;
  - immutable constructor mapping;
  - nested object mapping;
  - Value Object mapping.
- Adicionado benchmark cold start para Dapper puro, FluentMap root mapping, nested e Value Object.
- Executada rodada steady state representativa com `MaterializationSteadyStateBenchmarks`: sucesso.
- Executada rodada cold start representativa com `MaterializationColdStartBenchmarks`: sucesso.
- Criado baseline `.sdd/etapa-7/02-performance-baseline.md`.
- Executado `dotnet restore ./Dapper.FluentMap.sln`: sucesso.
- Executado `dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore`: sucesso, 0 warnings, 0 errors.
- Executado `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build`: sucesso, 231 testes aprovados.
- Criada a especificacao `.sdd/etapa-7/03-generated-materializer-contracts.md`.
- Adicionados contratos publicos:
  - `GeneratedRowMaterializer<TEntity>`;
  - `GeneratedMaterializerColumn`;
  - `GeneratedMaterializerDescriptor<TEntity>`.
- Adicionadas APIs publicas em `FluentMapConfiguration` para registrar generated materializers default e por profile.
- Adicionado registry interno de generated materializers por entidade, profile e shape ordenado.
- Integrado lookup generated antes do fallback `NestedMaterializationPlan` em `QueryMapped*`.
- Mantido fallback runtime quando descriptor esta ausente ou incompativel com o mapping efetivo.
- Adicionados testes em `GeneratedMaterializerContractTests` cobrindo registro, lookup, missing materializer, fallback, profiles, duplicidade, contrato invalido e concorrencia.
- Executado `dotnet build .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release`: sucesso, 0 warnings, 0 errors.
- Executado `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter "FullyQualifiedName~GeneratedMaterializerContractTests"`: sucesso, 9 testes aprovados.
- Executado `dotnet restore .\Dapper.FluentMap.sln`: sucesso.
- Executado `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`: sucesso, 0 warnings, 0 errors.
- Executado `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`: sucesso, 240 testes aprovados.
- Executado benchmark smoke `dotnet run --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release --no-build -- --filter *MaterializationSteadyStateBenchmarks*`: sucesso.
- Benchmark smoke steady state resumido:
  - DapperPure: 1.338 ms, 283.17 KB;
  - DapperWithFluentMapRootMapping: 1.469 ms, 283.3 KB;
  - QueryMappedSimple: 1.739 ms, 361.42 KB;
  - QueryMappedImmutableConstructor: 1.670 ms, 423.92 KB;
  - QueryMappedNestedObject: 1.495 ms, 377 KB;
  - QueryMappedValueObject: 1.355 ms, 587.84 KB.
- Executado `dotnet pack .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-build --output .\artifacts\packages`: sucesso, gerou `Dapper.FluentMap.2.0.0.nupkg`.
- Warnings conhecidos no pack: `NU5125` por `PackageLicenseUrl` legado e recomendacao NuGet para README de pacote.
- Criada a especificacao `.sdd/etapa-7/04-flat-generated-materializers.md`.
- Evoluido `Dapper.FluentMap.Generators` para emitir materializers flat para explicit maps literais simples.
- `AddGeneratedMappings()` agora registra maps/profiles e, quando geravel, registra `AddGeneratedMaterializer(...)` para o shape ordenado canonico.
- Adicionado diagnostic informativo `DFM011` para maps validos que continuam no fallback runtime por feature ainda nao suportada.
- Mantido fallback integral para nested paths, value objects, conventions, `IncludeBase`, shapes ausentes/extras/reordenados e maps dinamicos.
- Adicionados testes do generator para:
  - entidade simples;
  - colunas renomeadas;
  - constructor mapping simples;
  - nullable values;
  - profiles;
  - determinismo existente;
  - fallback para nested nao suportado.
- Atualizado teste de integracao de generated registration para validar:
  - uso de generated materializer em profile flat;
  - constructor mapping gerado;
  - nullable values;
  - fallback quando o shape da query nao corresponde ao descriptor.
- Atualizados `README.md` e `src/Dapper.FluentMap.Generators/README.md` para documentar materializers flat gerados e fallback.
- Atualizado benchmark steady state para registrar maps via `AddGeneratedMappings()`, exercitando generated materializers nos cenarios flat suportados.
- Adicionada referencia do projeto de benchmarks ao generator como analyzer.
- Executado `dotnet restore .\Dapper.FluentMap.sln`: sucesso.
- Executado `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`: sucesso, 0 warnings, 0 errors.
- Executado `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`: sucesso, 244 testes aprovados.
- Executada rodada benchmark steady state `MaterializationSteadyStateBenchmarks`: sucesso.
- Executada rodada benchmark cold start `MaterializationColdStartBenchmarks`: sucesso.
- Atualizada a secao `Apos Prompt 7.4` em `.sdd/etapa-7/02-performance-baseline.md`.
- Criada a especificacao `.sdd/etapa-7/05-complex-generated-materialization.md`.
- Evoluido `Dapper.FluentMap.Generators` para montar uma arvore interna de materializacao gerada.
- Adicionado generated materializer para:
  - nested mutable objects;
  - nested immutable objects por construtor;
  - Value Objects por componentes;
  - constructor composition bottom-up;
  - null subtree semantics;
  - profiles com nested mapping.
- Preservado fallback runtime com `DFM011` para construtores incompatíveis, `IncludeBase`, conventions, TypeHandlers e paths nao determinísticos.
- Adicionados testes do generator para nested mutable, Value Object por constructor composition, paths com mesmo terminal e constructor incompatível com fallback.
- Atualizado teste de integracao de generated registration para validar nested, null subtree, Value Object nullable, `Rank.Level`/`Seniority.Level`, profile nested e fallback por shape sem descriptor.
- Atualizados `README.md` e `src/Dapper.FluentMap.Generators/README.md` para documentar materializers complexos gerados e fallback.
- Executado `dotnet restore .\Dapper.FluentMap.sln`: sucesso.
- Executado `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`: sucesso, 0 warnings, 0 errors.
- Executado `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`: sucesso, 247 testes aprovados.
- Executada rodada benchmark steady state `MaterializationSteadyStateBenchmarks`: sucesso.
- Executado `dotnet pack .\src\Dapper.FluentMap.Generators\Dapper.FluentMap.Generators.csproj --configuration Release --no-build --output .\artifacts\packages`: sucesso, gerou `Dapper.FluentMap.Generators.2.0.0.nupkg`.
- Atualizada a secao `Apos Prompt 7.5` em `.sdd/etapa-7/02-performance-baseline.md`.
- Benchmark steady state resumido apos 7.5:
  - DapperPure: 1.295 ms, 283.17 KB;
  - DapperWithFluentMapRootMapping: 1.580 ms, 283.3 KB;
  - QueryMappedSimple: 1.754 ms, 261.12 KB;
  - QueryMappedImmutableConstructor: 1.734 ms, 261.05 KB;
  - QueryMappedNestedObject: 1.670 ms, 292.44 KB;
  - QueryMappedValueObject: 1.392 ms, 276.47 KB.

## Em andamento

Nenhum no escopo deste prompt apos o commit local.

## Proximos passos

1. Adicionar diagnostics runtime de generated/fallback.
2. Repetir todos os benchmarks apos 7.6 para validar lookup generated/fallback integrado.
3. Avaliar uma forma segura de medir cold start generated sem expor reset publico desnecessario.
4. Validar trimming, Native AOT e performance antes de documentar ganhos.

## Decisoes relevantes

- Generated materializer complementa, nao substitui, `NestedMaterializationPlan`.
- Lookup deve considerar entity, profile e column shape ordenado.
- O generated path deve operar contra `IDataRecord` e evitar acoplamento a internals do Dapper.
- Fallback runtime e obrigatorio e deve ser diagnosticavel.
- O projeto nao deve replicar Dapper.AOT.
- Evolucao deve ser aditiva e sem breaking change.
- Primeira cobertura gerada deve priorizar explicit maps com colunas literais.
- Generated materializers usam contrato publico por descriptor e delegate.
- Descritores gerados devem ser validados contra o mapping efetivo antes de uso.
- `QueryMapped*` mantem annotations de trimming/dynamic-code enquanto houver fallback runtime.
- Prompt 7.4 nao alterou decisoes arquiteturais existentes; apenas implementou a primeira cobertura flat prevista.
- Prompt 7.5 usa uma metadata tree interna para preservar member paths completos, null subtree e constructor composition.
- Prompt 7.5 mantem fallback para construcao nao deterministica em vez de gerar codigo especulativo.

## Riscos conhecidos

- Divergencia entre runtime materializer e generated materializer.
- Public mutable dictionaries podem invalidar descriptors gerados.
- TypeHandlers do Dapper ainda exigem boundary segura.
- Conventions customizadas e naming policies dinamicas limitam cobertura gerada.
- Ganhos de performance ainda sao hipotese ate benchmarks.
- Compatibilidade Native AOT completa ainda depende de validacao real.

## Arquivos importantes

- `.sdd/etapa-7/01-generated-materialization-architecture.md`
- `.sdd/etapa-7/DECISIONS.md`
- `.sdd/etapa-7/STATUS.md`
- `README.md`
- `src/Dapper.FluentMap/QueryMappedExtensions.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`
- `benchmarks/Dapper.FluentMap.Benchmarks/Program.cs`
- `benchmarks/Dapper.FluentMap.Benchmarks/Dapper.FluentMap.Benchmarks.csproj`
- `src/Dapper.FluentMap.Generators/MappingRegistrationGenerator.cs`
- `src/Dapper.FluentMap.Analyzers/FluentMapConfigurationAnalyzer.cs`
- `test/Dapper.FluentMap.Tests/GeneratedMaterializerSpikeTests.cs`
- `test/Dapper.FluentMap.Tests/GeneratedMaterializerContractTests.cs`
- `docs/sdd/etapa-6/04-generated-materializer-spike.md`
- `.sdd/etapa-7/02-performance-spec.md`
- `.sdd/etapa-7/02-performance-baseline.md`
- `.sdd/etapa-7/03-generated-materializer-contracts.md`
- `.sdd/etapa-7/04-flat-generated-materializers.md`
- `.sdd/etapa-7/05-complex-generated-materialization.md`

## Ultimo prompt executado

7.5
