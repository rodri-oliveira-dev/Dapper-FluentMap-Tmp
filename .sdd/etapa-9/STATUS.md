# Etapa 9 Status

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

## Proximos passos

1. Unbuffered synchronous path.
2. Async streaming.
3. Lifetime/cancellation hardening para caminhos async/streaming.
4. Regression/performance.
5. Documentacao final.

## Decisoes relevantes

- Nao alterar internals de `SqlMapper.GridReader`.
- Criar wrapper proprio `QueryMultipleMapped` como direcao principal.
- `ReadMapped<T>` deve usar o mesmo dispatch generated-then-runtime de
  `QueryMapped*`.
- Prompt 9.2 implementou o caminho buffered sincronico antes de streaming.
- Buffered deve ser entregue antes de streaming.
- Streaming deve ter nomes explicitos com `Unbuffered`.
- `IAsyncEnumerable<T>` deve ser avaliado como mudanca de API/dependencia para
  `netstandard2.0`.
- Cancellation deve usar `CommandDefinition.CancellationToken` e overloads
  discoverable quando aprovados.
- FluentMap nao deve abstrair SQL alem do necessario para aplicar
  materializacao avancada.

## Issues historicas

- #22: conventions em multiple results; historico inconclusivo, mas sem
  regressao dedicada atual.
- #43: `QueryMultiple().Read<T>()` nao aplicava mappings em 1.5.x; reporter
  confirmou que 1.4.1 funcionava; mantenedor marcou corrigida em 1.7.0.
- #42: multi-mapping por `splitOn` em unico result set, relacionado
  historicamente mas fora do escopo automatico da Etapa 9.
- #62: plano v2 cita #42 e #43 como relacionadas a melhorias de type mapping.

## APIs propostas

APIs conceituais a avaliar nos prompts de implementacao:

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

Nomes finais ainda dependem de revisao de overloads, target framework e
compatibilidade.

## Riscos conhecidos

- `GridReader` nao expoe reader publico suficiente para `ReadMapped`.
- Implementar wrapper proprio exige lifetime correto de connection, command e
  reader.
- Streaming pode vazar recursos se enumeradores nao forem descartados.
- Cancellation depende do suporte real do provider.
- `IAsyncEnumerable<T>` em API publica `netstandard2.0` pode alterar
  dependencias/compatibilidade.
- SQLite pode nao cobrir todos os cenarios reais de multiple result sets.
- Generated-only para AOT ainda nao existe; fallback runtime preserva warnings
  de trimming/dynamic code.
- Tests de estado global precisam resetar FluentMapper e type maps por tipo.

## Arquivos importantes

- `.sdd/etapa-9/01-historical-query-issues.md`
- `.sdd/etapa-9/02-advanced-query-materialization-spec.md`
- `.sdd/etapa-9/03-query-multiple-design.md`
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
- `benchmarks/Dapper.FluentMap.Benchmarks/Program.cs`

## Ultimo prompt executado

Ultimo prompt executado: 9.3
