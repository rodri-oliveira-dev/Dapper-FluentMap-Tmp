# Etapa 9 Status

Status: Concluída

## Objetivo

Definir e evoluir a arquitetura de Advanced Query Materialization para
multiplos result sets, `QueryMultiple`, `ReadMapped<T>`, profiles, buffering,
streaming, `IAsyncEnumerable<T>`, cancellation, lifetime de recursos e
equivalencia entre materializacao generated e runtime.

## Concluido

- Executado `git status` antes de alteracoes.
- Confirmada branch `feature/etapa-3`; nao estamos em `master`.
- Identificado item nao rastreado preexistente `src/Dapper.FluentMap/etapas/`,
  deixado intacto.
- Lido `README.md`.
- Examinada `Dapper.FluentMap.sln`.
- Examinados projetos core, Dommel, analyzers, generators, testes, smoke AOT e
  benchmarks.
- Lidos `.sdd/etapa-8/FINAL-REPORT.md` e `.sdd/etapa-8/STATUS.md`.
- Lido `.sdd/etapa-7/FINAL-REPORT.md` para contexto de generated
  materialization.
- Confirmado que `.sdd/etapa-9/` nao existia e criada a pasta.
- Investigadas as APIs atuais `QueryMapped<T>`,
  `QueryMapped<T, TProfile>`, `QueryMappedSingle<T>`,
  `QueryMappedSingle<T, TProfile>`, `QueryMappedAsync<TEntity, TProfile>` e
  `QueryMappedSingleAsync<TEntity, TProfile>`.
- Investigado runtime materializer (`NestedMaterializationPlan`), cache de
  planos, generated descriptors e registry.
- Confirmada dependencia efetiva `Dapper` 2.1.79 no core.
- Inspecionada superficie publica do Dapper 2.1.79 para `QueryMultiple`,
  `GridReader`, `ExecuteReader`, `QueryUnbufferedAsync`,
  `CommandDefinition`, `CommandFlags.Buffered` e cancellation.
- Lidas issues historicas #22 e #43, comentarios e eventos via GitHub/API.
- Consultadas issues relacionadas #42 e #62.
- Criado `.sdd/etapa-9/01-historical-query-issues.md`.
- Criado `.sdd/etapa-9/02-advanced-query-materialization-spec.md`.
- Criado `.sdd/etapa-9/DECISIONS.md`.
- Criado `.sdd/etapa-9/STATUS.md`.
- Executado `dotnet restore ./Dapper.FluentMap.sln`: sucesso.
- Executado `dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- Executado `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 298 testes aprovados.
- Prompt 9.2: criada especificacao refinada
  `.sdd/etapa-9/03-query-multiple-design.md`.
- Prompt 9.2: implementado `QueryMultipleMapped(...)` com retorno
  `MappedGridReader`.
- Prompt 9.2: implementados `MappedGridReader.ReadMapped<TEntity>()` e
  `MappedGridReader.ReadMapped<TEntity, TProfile>()`.
- Prompt 9.2: extraido `MappedRowMaterializer` para compartilhar dispatch
  generated-then-runtime entre `QueryMapped*` e `QueryMultipleMapped`.
- Prompt 9.2: cobertos testes de criacao, primeiro result set, segundo result
  set, profiles, empty result, invalid state, dispose antes/depois de consumo,
  excecao durante materializacao, generated materializer, parametros,
  transacao e lifetime de conexao.
- Prompt 9.3: criada especificacao
  `.sdd/etapa-9/04-read-mapped-spec.md`.
- Prompt 9.3: adicionadas APIs `ReadMappedSingle<TEntity>()` e
  `ReadMappedSingle<TEntity, TProfile>()`, alinhadas a `QueryMappedSingle*`.
- Prompt 9.3: nao adicionada API `ReadMappedSingleOrDefault*`, pois nao ha
  equivalente `QueryMappedSingleOrDefault*` na superficie atual do projeto.
- Prompt 9.3: reforcada cobertura de `ReadMapped*` para profiles isolados,
  naming policy, convention, immutable objects, nested objects, Value Objects,
  generated materializers, runtime fallback, equivalencia com `QueryMapped*` e
  multiplos result sets de tipos diferentes.
- Prompt 9.3: adicionadas regressoes minimas para as issues historicas #22 e
  #43 no caminho opt-in `QueryMultipleMapped(...).ReadMapped*`.
- Prompt 9.4: criada especificacao
  `.sdd/etapa-9/05-unbuffered-materialization.md`.
- Prompt 9.4: adicionada API `QueryMappedUnbuffered<TEntity>()`.
- Prompt 9.4: adicionada API `QueryMappedUnbuffered<TEntity, TProfile>()`.
- Prompt 9.4: adicionados overloads por `CommandDefinition`.
- Prompt 9.4: refatorado `MappedRowMaterializer` para resolver o delegate de
  materializacao uma vez por shape e reutilizar no caminho buffered e
  unbuffered.
- Prompt 9.4: adicionada cobertura para flat entity, nested object, Value
  Object, profile, generated materializer, runtime fallback, empty result,
  large sequence, lazy execution, early break, dispose explicito, excecao no
  meio da enumeracao, connection lifetime e transaction externa.
- Prompt 9.4: adicionados benchmarks de Dapper unbuffered e FluentMap
  unbuffered generated/runtime fallback.
- Prompt 9.4: criada
  `.sdd/etapa-9/06-performance-results.md` para baseline e resultados.
- Prompt 9.5: criada especificacao
  `.sdd/etapa-9/07-async-streaming-spec.md`.
- Prompt 9.5: adicionada API `QueryMappedUnbufferedAsync<TEntity>()` baseada
  em `DbConnection` e `IAsyncEnumerable<TEntity>`.
- Prompt 9.5: adicionada API
  `QueryMappedUnbufferedAsync<TEntity, TProfile>()`.
- Prompt 9.5: adicionados overloads por `CommandDefinition`.
- Prompt 9.5: cancellation propagada para `CommandDefinition`,
  `DbDataReader.ReadAsync(...)` e loop de enumeracao com
  `[EnumeratorCancellation]`.
- Prompt 9.5: reader descartado em `finally`, usando `DisposeAsync()` quando o
  provider implementa `IAsyncDisposable` e `Dispose()` como fallback.
- Prompt 9.5: materializers generated e runtime continuam sincronos por linha;
  async fica concentrado em execute/read.
- Prompt 9.5: core fixado em `LangVersion` `8.0` e dependencia publica direta
  de `Microsoft.Bcl.AsyncInterfaces` 10.0.8 adicionada para
  `IAsyncEnumerable<T>` em `netstandard2.0`.
- Prompt 9.5: adicionada cobertura para async streaming normal, empty result,
  nested, Value Object, profile, generated, fallback, cancellation antes da
  execucao, cancellation durante enumeracao, cancellation apos consumo parcial,
  partial consumption, excecoes, disposal, transaction e connection state.
- Prompt 9.5: adicionados benchmarks de Dapper async unbuffered e FluentMap
  async unbuffered generated/runtime fallback.
- Prompt 9.6: criada matriz
  `.sdd/etapa-9/08-resource-lifetime-matrix.md`.
- Prompt 9.6: criada suite documental
  `.sdd/etapa-9/09-advanced-query-regressions.md`.
- Prompt 9.6: renomeadas regressoes historicas para nomes orientados a
  comportamento:
  `MappedConventionShouldApplyToTypedReadFromMultipleResults` e
  `ExplicitMapShouldApplyToLaterTypedReadFromMultipleResults`.
- Prompt 9.6: adicionada cobertura provider-independent de tipos
  representativos com `DataTableReader`.
- Prompt 9.6: adicionada cobertura provider-specific SQLite para conversoes
  ADO.NET representativas.
- Prompt 9.6: adicionada cobertura de concorrencia para runtime fallback,
  profile cache em async streaming, generated materializers e
  `QueryMultipleMapped` em readers/conexoes independentes.
- Prompt 9.6: adicionados benchmarks de `QueryMultiple` para Dapper buffered,
  FluentMap generated e FluentMap runtime fallback.
- Prompt 9.7: auditada a implementacao real contra
  `.sdd/etapa-9/02-advanced-query-materialization-spec.md`.
- Prompt 9.7: revisadas APIs publicas introduzidas na Etapa 9; nenhuma
  correcao pequena de API foi necessaria no fechamento.
- Prompt 9.7: README atualizado com exemplos reais de `QueryMultipleMapped`,
  `ReadMapped`, profiles, streaming unbuffered, async streaming, cancellation,
  generated/runtime dispatch e limitacoes.
- Prompt 9.7: `.sdd/etapa-9/01-historical-query-issues.md` atualizado com
  classificacao final das issues #22 e #43.
- Prompt 9.7: `.sdd/etapa-9/06-performance-results.md` revisado com auditoria
  final dos benchmarks representativos.
- Prompt 9.7: criado `.sdd/etapa-9/FINAL-REPORT.md`.

## Em andamento

Nenhuma feature produtiva em andamento.

## Validacao do Prompt 9.2

- `dotnet test test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter FullyQualifiedName~QueryMultipleMappedTests`:
  sucesso, 13 testes aprovados.
- `dotnet build Dapper.FluentMap.sln --configuration Release`: sucesso,
  0 warnings, 0 errors.
- `dotnet test Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 311 testes aprovados.
- `dotnet restore Dapper.FluentMap.sln`: sucesso.
- `dotnet pack src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-build --output artifacts\packages`:
  sucesso; warning legado `NU5125` sobre `licenseUrl`.
- Pacote inspecionado:
  `lib/netstandard2.0/Dapper.FluentMap.dll` e
  `lib/netstandard2.0/Dapper.FluentMap.xml` presentes.

## Validacao do Prompt 9.3

- `dotnet test test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter FullyQualifiedName~QueryMultipleMappedTests`:
  sucesso, 25 testes aprovados.
- `dotnet restore .\Dapper.FluentMap.sln`: sucesso.
- `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 323 testes aprovados no total.
- `dotnet pack .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-build --output .\artifacts\packages`:
  sucesso; warning legado `NU5125` sobre `licenseUrl`.
- Pacote inspecionado:
  `lib/netstandard2.0/Dapper.FluentMap.dll` e
  `lib/netstandard2.0/Dapper.FluentMap.xml` presentes.
- Benchmarks de smoke nao executados: o Prompt 9.3 nao alterou a regra de
  dispatch, apenas reutilizou `MappedRowMaterializer` e adicionou wrappers
  single sobre o caminho buffered existente.

## Validacao do Prompt 9.4

- `dotnet test test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter FullyQualifiedName~QueryMappedUnbufferedTests`:
  sucesso, 14 testes aprovados.
- `dotnet build .\Dapper.FluentMap.sln --configuration Release`:
  sucesso, 0 warnings, 0 errors.
- `dotnet run --project benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release --no-build -- --filter *MaterializationSteadyStateBenchmarks*`:
  sucesso, 12 benchmarks executados; resultados registrados em
  `.sdd/etapa-9/06-performance-results.md`.
- `dotnet restore .\Dapper.FluentMap.sln`:
  sucesso.
- `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 337 testes aprovados no total.
- `dotnet pack .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-build --output .\artifacts\packages`:
  sucesso; warning legado `NU5125` sobre `licenseUrl`.
- Pacote inspecionado:
  `lib/netstandard2.0/Dapper.FluentMap.dll` e
  `lib/netstandard2.0/Dapper.FluentMap.xml` presentes.

## Validacao do Prompt 9.5

- `dotnet test test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter FullyQualifiedName~QueryMappedUnbufferedAsyncTests`:
  sucesso, 15 testes aprovados.
- `dotnet restore .\Dapper.FluentMap.sln`:
  sucesso.
- `dotnet build benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release`:
  sucesso, 0 warnings, 0 errors.
- `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 352 testes aprovados no total.
- `dotnet run --project benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release --no-build -- --filter *MaterializationSteadyStateBenchmarks*`:
  sucesso, 15 benchmarks executados; resultados registrados em
  `.sdd/etapa-9/06-performance-results.md`.
- `dotnet pack .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-build --output .\artifacts\packages`:
  sucesso; warning legado `NU5125` sobre `licenseUrl`.
- Pacote inspecionado:
  `lib/netstandard2.0/Dapper.FluentMap.dll` e
  `lib/netstandard2.0/Dapper.FluentMap.xml` presentes; nuspec inclui
  dependencias `Dapper` 2.1.79 e `Microsoft.Bcl.AsyncInterfaces` 10.0.8.

## Validacao do Prompt 9.6

- `dotnet test test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter FullyQualifiedName~AdvancedQueryHardeningTests`:
  sucesso, 5 testes aprovados.
- `dotnet test test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter FullyQualifiedName~QueryMultipleMappedTests`:
  sucesso, 25 testes aprovados.
- `dotnet build benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release`:
  sucesso, 0 warnings, 0 errors.
- `dotnet run --project benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release --no-build -- --filter *MaterializationSteadyStateBenchmarks*`:
  sucesso, 18 benchmarks executados; resultados registrados em
  `.sdd/etapa-9/06-performance-results.md`.
- `dotnet restore .\Dapper.FluentMap.sln`:
  sucesso.
- `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 357 testes aprovados no total.

## Validacao do Prompt 9.7

- `dotnet --version`:
  `10.0.302`.
- Plataforma de testes detectada: VSTest com `Microsoft.NET.Test.Sdk` e xUnit
  v3; nao ha `global.json`, `Directory.Build.props` ou
  `Directory.Packages.props` com sinal de Microsoft.Testing.Platform.
- `dotnet test test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter FullyQualifiedName~QueryMappedUnbufferedTests`:
  sucesso, 14 testes aprovados.
- `dotnet test test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter FullyQualifiedName~QueryMappedUnbufferedAsyncTests`:
  sucesso, 15 testes aprovados.
- Tentativas paralelas iniciais de
  `QueryMultipleMappedTests` e `AdvancedQueryHardeningTests` falharam com
  `CS2012` por lock concorrente do mesmo assembly em `obj\Release`; nao foi
  falha de teste.
- `dotnet test test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter FullyQualifiedName~QueryMultipleMappedTests`:
  sucesso apos reexecucao sequencial, 25 testes aprovados.
- `dotnet test test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter FullyQualifiedName~AdvancedQueryHardeningTests`:
  sucesso apos reexecucao sequencial, 5 testes aprovados.
- `dotnet restore .\Dapper.FluentMap.sln`:
  sucesso.
- `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 357 testes aprovados no total.
- `dotnet run --project benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release --no-build -- --filter *MaterializationSteadyStateBenchmarks*`:
  sucesso, 18 benchmarks executados.

## Proximos passos

1. Avaliar `QueryMultipleMappedAsync`/`ReadMappedUnbufferedAsync` em prompt
   proprio, se houver demanda.
2. Avaliar caminho generated-only/AOT-safe em prompt proprio.
3. Criar provider certification opt-in somente quando houver infraestrutura de
   CI ou demanda real para SQL Server/PostgreSQL.

## Decisoes relevantes

- Nao alterar internals de `SqlMapper.GridReader`.
- Criar wrapper proprio `QueryMultipleMapped` como direcao principal.
- `ReadMapped<T>` deve usar o mesmo dispatch generated-then-runtime de
  `QueryMapped*`.
- Prompt 9.2 implementou o caminho buffered sincronico antes de streaming.
- Buffered deve ser entregue antes de streaming.
- Streaming deve ter nomes explicitos com `Unbuffered`.
- Prompt 9.4 implementou `QueryMappedUnbuffered*` sincrono sem misturar com
  async streaming.
- Prompt 9.5 implementou `QueryMappedUnbufferedAsync*` com `IAsyncEnumerable<T>`
  como mudanca aditiva de API/dependencia para `netstandard2.0`.
- Cancellation usa `CommandDefinition.CancellationToken`, overloads
  discoverable e token efetivo do async enumerator.
- FluentMap nao deve abstrair SQL alem do necessario para aplicar
  materializacao avancada.
- Prompt 9.6 confirmou que concorrencia suportada e entre operacoes/readers/
  conexoes independentes. Uso concorrente do mesmo `MappedGridReader` ou do
  mesmo `SqlMapper.GridReader` nao e contrato suportado.

## Issues historicas

- #22: conventions em multiple results; historico inconclusivo, mas sem
  regressao coberta por
  `MappedConventionShouldApplyToTypedReadFromMultipleResults`.
- #43: `QueryMultiple().Read<T>()` nao aplicava mappings em 1.5.x; reporter
  confirmou que 1.4.1 funcionava; mantenedor marcou corrigida em 1.7.0;
  regressao coberta por
  `ExplicitMapShouldApplyToLaterTypedReadFromMultipleResults`.
- #42: multi-mapping por `splitOn` em unico result set, relacionado
  historicamente mas fora do escopo automatico da Etapa 9.
- #62: plano v2 cita #42 e #43 como relacionadas a melhorias de type mapping.

## APIs propostas

APIs implementadas e conceituais para prompts futuros:

```csharp
using var multi = connection.QueryMultipleMapped(sql);

var customers = multi.ReadMapped<Customer>();
var legacyCustomers = multi.ReadMapped<Customer, LegacyProfile>();
```

```csharp
foreach (var customer in connection.QueryMappedUnbuffered<Customer>(sql))
{
}
```

```csharp
await foreach (var customer in connection.QueryMappedUnbufferedAsync<Customer>(
    command,
    cancellationToken))
{
}
```

`QueryMappedUnbuffered*` sincrono foi implementado no Prompt 9.4.
`QueryMappedUnbufferedAsync*` foi implementado no Prompt 9.5 com receiver
`DbConnection`.

## Riscos conhecidos

- `GridReader` nao expoe reader publico suficiente para `ReadMapped`.
- Implementar wrapper proprio exige lifetime correto de connection, command e
  reader.
- Streaming pode manter recursos abertos por mais tempo se enumeradores nao
  forem descartados.
- Cancellation depende do suporte real do provider.
- `IAsyncEnumerable<T>` em API publica `netstandard2.0` alterou a dependencia
  publica ao adicionar `Microsoft.Bcl.AsyncInterfaces` 10.0.8.
- SQLite cobre provider-specific ADO behavior disponivel na infraestrutura
  atual, mas nao substitui certificacao SQL Server/PostgreSQL.
- Generated-only para AOT ainda nao existe; fallback runtime preserva warnings
  de trimming/dynamic code.
- Tests de estado global precisam resetar FluentMapper e type maps por tipo.

## Arquivos importantes

- `.sdd/etapa-9/01-historical-query-issues.md`
- `.sdd/etapa-9/02-advanced-query-materialization-spec.md`
- `.sdd/etapa-9/03-query-multiple-design.md`
- `.sdd/etapa-9/04-read-mapped-spec.md`
- `.sdd/etapa-9/05-unbuffered-materialization.md`
- `.sdd/etapa-9/06-performance-results.md`
- `.sdd/etapa-9/07-async-streaming-spec.md`
- `.sdd/etapa-9/08-resource-lifetime-matrix.md`
- `.sdd/etapa-9/09-advanced-query-regressions.md`
- `.sdd/etapa-9/DECISIONS.md`
- `.sdd/etapa-9/STATUS.md`
- `src/Dapper.FluentMap/MappedGridReader.cs`
- `src/Dapper.FluentMap/Materialization/MappedRowMaterializer.cs`
- `src/Dapper.FluentMap/QueryMappedExtensions.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`
- `src/Dapper.FluentMap/Materialization/GeneratedMaterializerDescriptor.cs`
- `src/Dapper.FluentMap/Materialization/GeneratedMaterializerColumn.cs`
- `src/Dapper.FluentMap/Materialization/GeneratedRowMaterializer.cs`
- `src/Dapper.FluentMap.Generators/MappingRegistrationGenerator.cs`
- `test/Dapper.FluentMap.Tests/MappingProfileTests.cs`
- `test/Dapper.FluentMap.Tests/GeneratedMaterializerContractTests.cs`
- `test/Dapper.FluentMap.Tests/QueryMultipleMappedTests.cs`
- `test/Dapper.FluentMap.Tests/QueryMappedUnbufferedTests.cs`
- `test/Dapper.FluentMap.Tests/QueryMappedUnbufferedAsyncTests.cs`
- `test/Dapper.FluentMap.Tests/AdvancedQueryHardeningTests.cs`
- `benchmarks/Dapper.FluentMap.Benchmarks/Program.cs`
- `.sdd/etapa-9/FINAL-REPORT.md`

## Último prompt executado

Último prompt executado: 9.7
