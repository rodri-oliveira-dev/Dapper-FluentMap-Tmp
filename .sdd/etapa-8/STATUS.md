# Etapa 8 Status

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

## Em andamento

Nenhum apos a validacao final deste prompt.

## Proximos passos

1. Adaptar FluentMap.Dommel para consumo operacional completo de metadata
   `Insert`/`Update` quando houver contrato seguro para nao confundir update com
   generated.
2. Criar suite de regressao historica para #94, #122, #123, #130, #114, #126 e
   #133.
3. Atualizar analyzers/source generator para reconhecer a nova DSL.
4. Fazer hardening de cache, profiles, generated materializers e Dommel SQL real.

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

## Issues historicas

- #94 ReadOnly Fields: resolver por nova arquitetura.
- #122 Insert issue when key column is not identity: parcialmente corrigida,
  manter regressao e separar key/identity.
- #123 Computed property used in insert/update: provavel correcao via resolvers
  atuais, ainda requer regressao de SQL real.
- #130 Default value do banco vs `Ignore()`: resolver por nova arquitetura.
- #114 conflito entre property e membros do tipo: ja resolvido, preservar.
- #126 nested properties com mesmo terminal: ja resolvido no core/generated,
  preservar.
- #133 `Ignore()` causando `NotImplementedException`: ja resolvido para bug
  original, preservar.
- Prompt 8.3 adicionou cobertura explicita para #114, #126 e #133 na semantica
  de leitura/materializacao.

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
  `SetGeneratedOption(None)` continua identity operacional nos resolvers, embora
  a metadata de core diferencie key de identity.
- `ExcludeFromInsert()` isolado e `DatabaseDefaultOnInsert()` com update ativo
  ainda nao podem ser traduzidos fielmente para `ColumnPropertyInfo.IsGenerated`.

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

## Ultimo prompt executado

Ultimo prompt executado: 8.3
