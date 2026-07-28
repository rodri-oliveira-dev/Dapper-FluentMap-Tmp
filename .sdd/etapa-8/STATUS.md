# Etapa 8 Status

Status: Concluída

Último prompt executado: 8.7

## Objetivo

Definir e implementar o modelo inicial de metadata de persistencia de
propriedades, separando materializacao/leitura de insert/update, sem adicionar
execucao de CRUD ao core.

## Concluido

- Executado `git status` antes das alteracoes.
- Lido `README.md`.
- Examinada `Dapper.FluentMap.sln`.
- Examinados projetos core, Dommel, analyzers, generators, testes,
  materializacao runtime, generated materialization e profiles.
- Lidos `.sdd/etapa-7/FINAL-REPORT.md` e `.sdd/etapa-7/STATUS.md`.
- Confirmado que `.sdd/etapa-8/` nao existia e criada a pasta.
- Lidas issues historicas #94, #122, #123, #130, #114, #126 e #133.
- Lidos PRs relacionados #129 e #131.
- Investigado Dommel 3.5.3 efetivo e seu uso de `ColumnPropertyInfo.IsGenerated`
  em `Insert` e `Update`.
- Criado `.sdd/etapa-8/01-historical-issues.md`.
- Criado `.sdd/etapa-8/02-persistence-semantics-spec.md`.
- Criado `.sdd/etapa-8/DECISIONS.md`.
- Executado `dotnet restore ./Dapper.FluentMap.sln`: sucesso.
- Executado `dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore`: sucesso, 0 warnings, 0 errors.
- Executado `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build`: sucesso, 254 testes aprovados.
- Confirmado que o Prompt 8.1 estava aplicado nos documentos da Etapa 8 e no
  estado do README/codigo ja existente.
- Nao encontrada divergencia entre a documentacao da Etapa 8.1 e a
  implementacao atual que exigisse registro corretivo separado.
- Implementado `PropertyPersistenceMetadata`.
- Implementado `IPropertyMapWithPersistenceMetadata`.
- Adicionada metadata `Persistence` em `PropertyMapBase<TPropertyMap>`.
- Adicionadas APIs fluent:
  - `ExcludeFromInsert()`;
  - `ExcludeFromUpdate()`;
  - `ReadOnly()`;
  - `Computed()`;
  - `DatabaseDefaultOnInsert()`.
- Preservado `Ignore()` como `Read=no`, `Insert=no`, `Update=no`.
- Conectado `DommelPropertyMap.IsKey()`, `IsIdentity()` e
  `SetGeneratedOption(...)` a metadata de persistencia.
- Adicionada ponte conservadora `EffectiveGeneratedOption` para os resolvers
  Dommel.
- Adicionada metadata em `MemberMappingExplanation.Persistence` para
  `Explain<T>()` e `Explain<T, TProfile>()`.
- Criado `.sdd/etapa-8/03-persistence-metadata-design.md`.
- Atualizado `.sdd/etapa-8/02-persistence-semantics-spec.md` com o modelo
  efetivo.
- Atualizado `README.md` com a API publica nova.
- Adicionados testes de metadata no core e em Dommel.
- Executado `dotnet build .\Dapper.FluentMap.sln --configuration Release`:
  sucesso, 0 warnings, 0 errors.
- Executado `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --no-build`:
  sucesso, 228 testes aprovados.
- Executado `dotnet test .\test\Dapper.FluentMap.Dommel.Tests\Dapper.FluentMap.Dommel.Tests.csproj --configuration Release --no-build`:
  sucesso, 13 testes aprovados.
- Executado `dotnet restore .\Dapper.FluentMap.sln`: sucesso.
- Executado `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- Executado novamente `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- Executado novamente `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 274 testes aprovados.
- Executado `dotnet pack .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-build --output .\artifacts\packages`:
  sucesso; warning legado `NU5125` sobre `PackageLicenseUrl`/`licenseUrl`.
- Inspecionado `artifacts/packages/Dapper.FluentMap.2.0.0.nupkg`:
  contem `lib/netstandard2.0/Dapper.FluentMap.dll`,
  `lib/netstandard2.0/Dapper.FluentMap.xml`, nuspec e metadados NuGet; dependencia
  `Dapper` 2.1.79 preservada.
- Criado `.sdd/etapa-8/04-read-semantics.md`.
- Ajustado source generator para tratar `ExcludeFromInsert()`,
  `ExcludeFromUpdate()`, `ReadOnly()`, `Computed()` e
  `DatabaseDefaultOnInsert()` como neutros para materializacao gerada.
- Preservado `Ignore()` como unica chamada da DSL atual que transforma coluna
  configurada em `GeneratedMaterializerColumn.Ignore(...)`.
- Adicionada regressao de `Dapper.Query<T>()` para coluna ignorada selecionada,
  cobrindo a categoria historica da issue #133.
- Adicionada cobertura de colisao de nome de propriedade com membro de `string`
  sem depender apenas de `Format`, cobrindo a categoria historica da issue #114.
- Ampliada equivalencia runtime/generated para propriedade normal, ignored,
  read-only, computed, database-default-on-insert, insert-excluded e
  update-excluded.
- Mantida cobertura runtime/generated existente para nested, immutable,
  Value Objects, profiles e member paths `Rank.Level`/`Seniority.Level`.
- Executado `dotnet test .\test\Dapper.FluentMap.Generators.Tests\Dapper.FluentMap.Generators.Tests.csproj --configuration Release --filter "FullyQualifiedName~MappingRegistrationGeneratorTests"`:
  sucesso, 23 testes aprovados.
- Executado `dotnet test .\test\Dapper.FluentMap.GeneratedRegistration.Tests\Dapper.FluentMap.GeneratedRegistration.Tests.csproj --configuration Release --filter "FullyQualifiedName~GeneratedRegistrationIntegrationTests"`:
  sucesso, 2 testes aprovados.
- Executado `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter "FullyQualifiedName~DapperIntegrationTests"`:
  sucesso, 9 testes aprovados.
- Executado `dotnet restore .\Dapper.FluentMap.sln`: sucesso.
- Executado `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- Executado `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 277 testes aprovados.
- Executado smoke de benchmark:
  `dotnet run --configuration Release --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj -- --filter "*MaterializationSteadyStateBenchmarks.QueryMappedSimple*" --job Dry --warmupCount 1 --minIterationCount 1 --maxIterationCount 2`.
  Resultado observado: `QueryMappedSimple` generated 3.716 ms / 362.73 KB e
  `QueryMappedSimpleRuntimeFallback` 4.435 ms / 361.53 KB. BenchmarkDotNet
  alertou que a iteracao unica e curta demais para conclusao estatistica; como
  smoke, nao indicou regressao evidente do hot path.
- Criado `.sdd/etapa-8/05-dommel-persistence-behavior.md`.
- Adaptado `Dapper.FluentMap.Dommel` para consumir metadata de persistencia em
  INSERT e UPDATE.
- Adicionado wrapper de `ISqlBuilder` para recompor colunas de INSERT com base
  em `ParticipatesInInsert`.
- Alterado resolver de propriedades Dommel para usar `ParticipatesInUpdate` no
  filtro operacional de UPDATE.
- Resolvido consumo de mappings herdados por resolvers Dommel.
- Adicionados testes reais SQLite para normal, ignored, read-only,
  insert-excluded, update-excluded, computed, identity, database default,
  mapping herdado, key nao identity, composite key nao identity e operacoes
  repetidas em entidades diferentes.
- Executado `dotnet build .\test\Dapper.FluentMap.Dommel.Tests\Dapper.FluentMap.Dommel.Tests.csproj --configuration Release`:
  sucesso, 0 warnings, 0 errors.
- Executado `dotnet test .\test\Dapper.FluentMap.Dommel.Tests\Dapper.FluentMap.Dommel.Tests.csproj --configuration Release --no-build`:
  sucesso, 17 testes aprovados.
- Executado `dotnet restore .\Dapper.FluentMap.sln`: sucesso.
- Executado `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- Executado `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 281 testes aprovados.
- Criado `.sdd/etapa-8/06-persistence-diagnostics.md` com matriz de
  diagnostics e validacao.
- Implementada validacao runtime dos invariants de persistence metadata em
  `MappingConfigurationValidator`.
- Adicionado analyzer `DFM012` para combinacoes contraditorias em fluent chains
  estaticamente visiveis.
- Confirmado que metadata de escrita continua neutra para generated
  materializers; `ExcludeFromInsert()` e equivalentes nao geram fallback warning
  por si so.
- Mantida a API `Explain<TEntity>()` sem nova superficie publica: a metadata
  estruturada existente em `MemberMappingExplanation.Persistence` ja expoe read,
  insert, update e generated/computed/default/identity.
- Adicionados testes para combinacoes validas, invalidas, diagnostics do
  analyzer, diagnostics runtime e preservacao dos casos inherited/profile ja
  cobertos pela suite de persistence metadata.
- Executado `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter "FullyQualifiedName~ConfigurationValidationTests"`:
  sucesso, 13 testes aprovados.
- Executado `dotnet test .\test\Dapper.FluentMap.Analyzers.Tests\Dapper.FluentMap.Analyzers.Tests.csproj --configuration Release --filter "FullyQualifiedName~FluentMapConfigurationAnalyzerTests"`:
  sucesso, 14 testes aprovados.
- Executado `dotnet restore .\Dapper.FluentMap.sln`: sucesso.
- Executado `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- Executado `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 288 testes aprovados.
- Executado `dotnet pack .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-build --output .\artifacts\packages`:
  sucesso; warning legado `NU5125` sobre `PackageLicenseUrl`/`licenseUrl`.
- Executado `dotnet pack .\src\Dapper.FluentMap.Analyzers\Dapper.FluentMap.Analyzers.csproj --configuration Release --no-build --output .\artifacts\packages`:
  sucesso.
- Inspecionados `Dapper.FluentMap.2.0.0.nupkg` e
  `Dapper.FluentMap.Analyzers.2.0.0.nupkg`; conteudos esperados preservados.
- Revalidadas no GitHub arquivado as issues historicas #94, #114, #122, #123,
  #126, #130 e #133; todas continuam fechadas no projeto original.
- Criado `.sdd/etapa-8/07-historical-regression-suite.md` com matriz
  issue/teste/projeto/status.
- Criada suite explicita em
  `test/Dapper.FluentMap.Tests/HistoricalRegression/HistoricalCoreRegressionTests.cs`.
- Criada suite explicita em
  `test/Dapper.FluentMap.Dommel.Tests/HistoricalRegression/DommelHistoricalRegressionTests.cs`.
- Cobertas regressions historicas de core mapping, materializacao, nested
  mapping, Dommel e persistence behavior.
- Adicionado teste diferencial runtime/generated para semanticas historicas de
  leitura.
- Atualizado `.sdd/etapa-8/01-historical-issues.md` com evidencia precisa dos
  testes do Prompt 8.6.
- Executado `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter "Category=HistoricalRegression"`:
  sucesso, 4 testes aprovados.
- Executado `dotnet test .\test\Dapper.FluentMap.Dommel.Tests\Dapper.FluentMap.Dommel.Tests.csproj --configuration Release --filter "Category=HistoricalRegression"`:
  sucesso, 4 testes aprovados apos repetir isoladamente uma tentativa paralela
  que encontrou lock de build.
- Executado `dotnet restore .\Dapper.FluentMap.sln`: sucesso.
- Executado `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- Executado `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 296 testes aprovados.
- Executado Prompt 8.7 de hardening e fechamento.
- Corrigida combinacao contraditoria `DatabaseDefaultOnInsert().Computed()` para
  falhar com `FluentMapConfigurationException`.
- Adicionada regressao runtime para `DatabaseDefaultOnInsert().Computed()`.
- Adicionada regressao do analyzer `DFM012` para
  `DatabaseDefaultOnInsert().Computed()`.
- Atualizado `README.md` com documentacao publica detalhando `Ignore`,
  read-only, database defaults, computed, keys e integracao Dommel em ingles e
  portugues.
- Criado `.sdd/etapa-8/08-compatibility-notes.md`.
- Criado `.sdd/etapa-8/FINAL-REPORT.md`.
- Executado `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter "FullyQualifiedName~PropertyPersistenceMetadataTests"`:
  sucesso, 15 testes aprovados.
- Executado `dotnet test .\test\Dapper.FluentMap.Analyzers.Tests\Dapper.FluentMap.Analyzers.Tests.csproj --configuration Release --filter "FullyQualifiedName~FluentMapConfigurationAnalyzerTests"`:
  sucesso, 15 testes aprovados.
- Executado `dotnet restore .\Dapper.FluentMap.sln`: sucesso.
- Executado `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- Executado `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 298 testes aprovados.
- Executado `dotnet test .\test\Dapper.FluentMap.Dommel.Tests\Dapper.FluentMap.Dommel.Tests.csproj --configuration Release --no-build`:
  sucesso, 21 testes aprovados.
- Executado `dotnet pack .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-build --output .\artifacts\packages`:
  sucesso; warning legado `NU5125` sobre `PackageLicenseUrl`/`licenseUrl`.
- Inspecionado `artifacts/packages/Dapper.FluentMap.2.0.0.nupkg`:
  contem `lib/netstandard2.0/Dapper.FluentMap.dll` e
  `lib/netstandard2.0/Dapper.FluentMap.xml`.

## Em andamento

Nenhum. Etapa 8 concluida.

## Proximos passos

1. Avaliar helper textual opcional para `Explain` se usuarios pedirem uma
   representacao pronta para logs.
2. Fazer hardening de cache e cenarios Dommel provider-specific quando houver
   demanda real.

## Decisoes relevantes

- `Read`, `Insert` e `Update` sao dimensoes independentes.
- `Ignore()` continua significando `Read=no`, `Insert=no`, `Update=no`.
- Read-only significa `Read=yes`, `Insert=no`, `Update=no`.
- Metadata de persistencia deve existir no core como contrato aditivo/opcional,
  mas CRUD continua fora do core.
- `Computed` e `DatabaseDefaultOnInsert` sao semanticas diferentes.
- `Key` nao implica `Identity`.
- Dommel traduz metadata para `ColumnPropertyInfo` e seus resolvers.
- Generated materializers observam apenas semantica de leitura.
- `IPropertyMap` nao foi alterada; metadata nova fica em interface opcional.
- `Explain<T>()` ja expoe metadata de persistencia.
- `DFM012` reporta persistence behavior contraditorio apenas quando provado
  estaticamente em uma fluent chain direta.
- Runtime validation valida a metadata efetiva e protege maps customizados.

## Issues historicas

- #94 ReadOnly Fields: resolved no Dommel, com regressao SQLite de INSERT,
  UPDATE e SELECT.
- #122 Insert issue when key column is not identity: regression covered para key
  nao identity explicita e composite key.
- #123 Computed property used in insert/update: resolved no Dommel, com coluna
  generated real em SQLite.
- #130 Default value do banco vs `Ignore()`: resolved com
  `DatabaseDefaultOnInsert()` e `created_at DEFAULT CURRENT_TIMESTAMP`.
- #114 conflito entre property e membros do tipo: ja resolvido, preservar.
- #126 nested properties com mesmo terminal: ja resolvido no core/generated,
  preservar.
- #133 `Ignore()` causando `NotImplementedException`: ja resolvido para bug
  original, preservar.
- Prompt 8.3 adicionou cobertura explicita para #114, #126 e #133 na semantica
  de leitura/materializacao.
- Prompt 8.6 consolidou a suite historica permanente com testes explicitos para
  #94, #114, #122, #123, #126, #130 e #133.

## Riscos conhecidos

- Compatibilidade binaria se `IPropertyMap` for alterada diretamente.
- `IsKey()` historico ainda implica identity por default no DommelPropertyMap
  quando `GeneratedOption` nao e especificado.
- Dommel cacheia resolvers/properties; mudancas de metadata devem considerar
  inicializacao global e invalidacao.
- Profiles sao leitura query-scoped e nao devem contaminar metadata global de
  escrita sem decisao especifica.
- Nested paths usam `MemberPath`, mas Dommel trabalha com propriedades flat.
- `Generated` e amplo demais para representar sozinho default, computed e
  identity.
- Dommel ainda tem uma ponte de compatibilidade: `IsKey()` sem
  `SetGeneratedOption(None)` continua identity operacional no key resolver,
  embora a metadata de core diferencie key de identity.
- SQL builders customizados registrados depois de `ForDommel()` substituem o
  wrapper padrao e precisam honrar `ParticipatesInInsert` por conta propria.
- O analyzer nao infere combinacoes construidas por variaveis, helpers ou fluxo
  condicional; esses casos dependem da validacao runtime.

## Arquivos importantes

- `.sdd/etapa-8/01-historical-issues.md`
- `.sdd/etapa-8/02-persistence-semantics-spec.md`
- `.sdd/etapa-8/DECISIONS.md`
- `.sdd/etapa-8/STATUS.md`
- `src/Dapper.FluentMap/Mapping/PropertyMap.cs`
- `src/Dapper.FluentMap/Mapping/PropertyPersistenceMetadata.cs`
- `src/Dapper.FluentMap/Mapping/EntityMap.cs`
- `src/Dapper.FluentMap/Mapping/MemberPath.cs`
- `src/Dapper.FluentMap/Compatibility/DapperFluentPropertyTypeMap.cs`
- `src/Dapper.FluentMap/Compatibility/DapperIgnoredMemberMap.cs`
- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`
- `src/Dapper.FluentMap/Materialization/GeneratedMaterializerColumn.cs`
- `src/Dapper.FluentMap.Dommel/Mapping/DommelPropertyMap.cs`
- `src/Dapper.FluentMap.Dommel/Resolvers/DommelPropertyResolver.cs`
- `src/Dapper.FluentMap.Dommel/Resolvers/DommelKeyPropertyResolver.cs`
- `src/Dapper.FluentMap.Dommel/Resolvers/DommelColumnNameResolver.cs`
- `src/Dapper.FluentMap.Generators/MappingRegistrationGenerator.cs`
- `src/Dapper.FluentMap.Analyzers/FluentMapConfigurationAnalyzer.cs`
- `.sdd/etapa-8/04-read-semantics.md`
- `.sdd/etapa-8/05-dommel-persistence-behavior.md`
- `.sdd/etapa-8/06-persistence-diagnostics.md`
- `.sdd/etapa-8/07-historical-regression-suite.md`
- `test/Dapper.FluentMap.Tests/HistoricalRegression/HistoricalCoreRegressionTests.cs`
- `test/Dapper.FluentMap.Dommel.Tests/HistoricalRegression/DommelHistoricalRegressionTests.cs`

## Último prompt executado

Último prompt executado: 8.7
