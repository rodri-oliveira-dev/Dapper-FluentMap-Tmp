# Etapa 12 - Final Report

## Executive Summary

Auditoria final executada em 2026-07-29 no checkout local da branch
`feature/etapa-3`. O FluentMap esta buildable, testable, packable e
documentado para uma primeira validacao publica como Release Candidate do fork.

Conclusao objetiva: o projeto esta pronto para preparar uma RC, preferencialmente
`3.0.0-rc.1`, mas nao esta pronto para release stable. Tambem nao deve ser
publicado com a versao padrao atual `2.0.0`, pois esse numero ja existe nos
package IDs historicos `Dapper.FluentMap` e `Dapper.FluentMap.Dommel`.

## Release Recommendation

Recomendacao: `Release Candidate`.

Nao recomendado: `Stable`.

Racional:

- restore, build, test, pack, provider SQLite, vulnerability audit, trimmed
  smokes e benchmark smoke passaram localmente;
- a evolucao das Etapas 7-12 e grande demais para stable direto sem usuarios
  reais na nova arquitetura;
- ha uma quebra historica conhecida no pacote Dommel em relacao ao `2.0.0`
  original (`DommelPropertyMap.GeneratedOption` nullable);
- baseline API/binaria do proprio fork ainda deve ser estabelecida apos a
  primeira RC;
- Native AOT completo, SQL Server/PostgreSQL certificados, SourceLink checksum
  remoto, package signing e SBOM formal ainda nao estao fechados.

## Scope Reviewed

- `README.md`, `MIGRATION.md`, `COMPATIBILITY.md`, `SUPPORT.md` e `CHANGELOG.md`.
- `.sdd/etapa-12/01-07`, `DECISIONS.md` e `STATUS.md`.
- Final reports das etapas 7, 8, 9, 10 e 11.
- Solution, projetos publicos, testes, provider tests, AOT smoke, benchmarks,
  NuGet configuration e workflows GitHub.
- Public APIs documentadas dos cinco pacotes.
- Conteudo e metadata dos pacotes gerados.

## Full SDD Audit

| Requirement | Evidence | Status | Blocker? |
| ----------- | -------- | ------ | -------- |
| Preservar `netstandard2.0` nos pacotes publicos | Todos os projetos publicos targetam `netstandard2.0`; build Release passou | Passed | No |
| Testar em runtime moderno sem elevar TFM minimo | Projetos de teste/smoke/benchmark em `net10.0`; SDK `10.0.302` | Passed | No |
| Validar Dapper minimo e latest stable suportado | NuGet.org consultado: latest stable `Dapper 2.1.79`; solution passou com a faixa atual | Passed | No |
| Declarar range conservador de Dapper | Nuspec core/Dommel: `Dapper [2.1.79, 3.0.0)` | Passed | No |
| Cobrir boundary sensivel de TypeHandler | Testes de TypeHandler passam; codigo usa reflection sobre `TypeHandlerCache<T>.Parse` | Passed with limitation | No para RC; risco para stable |
| Validar Dommel atual | Dommel tests: 23 passed; range `Dommel [3.5.3,4.0.0)` no pacote | Passed | No |
| Separar support de provider certification | `COMPATIBILITY.md` e provider matrix diferenciam SQLite, SQL Server, PostgreSQL, MySQL e SQL CE | Passed | No |
| SQLite provider real | Provider tests: 7 passed em SQLite | Passed | No |
| SQL Server/PostgreSQL reais | Harness existe, mas env vars ausentes e 14 testes skipped | Passed with limitation | No para RC; blocker para claim certificado |
| MySQL/MariaDB | Sem harness obrigatorio; documentado como not validated | Deferred intentionally | No |
| SQL Server CE | Builder legado mantido, validacao moderna limitada por upstream | Not applicable | No |
| API publica revisada | `.sdd/etapa-12/05-public-api-review.md`; build/package validation | Passed with limitation | Stable blocker ate baseline do fork |
| Binary compatibility formal | Package validation habilitada; baseline do fork ainda inexistente | Passed with limitation | Stable blocker |
| Versionamento publicavel | Pack padrao gera `2.0.0`; pack override `3.0.0-rc.1` passou | Passed with limitation | Blocker se publicar `2.0.0` |
| NuGet metadata moderna | README, license expression, repository URL/commit e dependency ranges nos nuspecs | Passed | No |
| SourceLink/symbols | `.snupkg` runtime gerados; PDB contem raw GitHub URL do commit | Passed with limitation | Validar checksum no CI apos push |
| Analyzer/generator package layout | Assemblies/PDBs em `analyzers/dotnet/cs`; sem deps Roslyn transitivas no nuspec | Passed | No |
| Analyzer/generator release manifests | Arquivos existem, mas precisam promocao/revisao antes de stable | Passed with limitation | Stable blocker |
| CI hardening | Actions pinadas por SHA, permissoes minimas, release workflow sem publish | Passed | No |
| Publish NuGet | Intencionalmente desabilitado | Deferred intentionally | No |
| Vulnerability audit | `dotnet list ... --vulnerable --include-transitive`: sem vulnerabilidades | Passed | No |
| Secrets no repo/pacotes | Scan textual encontrou apenas docs/testes/workflow OIDC; pacotes inspecionados sem secrets | Passed | No |
| Package signing | `dotnet nuget verify` falha com `NU3004` por pacote nao assinado | Deferred intentionally | No para RC; decisao stable |
| SBOM formal | Dependency inventory/provenance preparados; SPDX/CycloneDX nao adotado | Deferred intentionally | No para RC |
| Trimming smoke | Publish trimmed DI explicit/generated passou e executou | Passed with limitation | No |
| Native AOT publish/run | Bloqueado por linker nativo ausente | Failed | Blocker para claim Native AOT |
| Performance guardrail | BenchmarkDotNet smoke de 20 cenarios passou; sem regressao severa observada | Passed with limitation | No |
| Documentacao publica | README/MIGRATION/COMPATIBILITY/SUPPORT revisados contra APIs/testes | Passed | No |

## Build

Executado:

```bash
dotnet restore ./Dapper.FluentMap.sln
dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore
```

Resultado: restore e build Release passaram com 0 warnings e 0 errors.

## Tests

Executado:

```bash
dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build
```

Resultado: 460 passed, 14 skipped, 0 failed.

Skips: 14 testes condicionais de SQL Server/PostgreSQL por ausencia de
`DFM_SQLSERVER_CONNECTION_STRING` e `DFM_POSTGRESQL_CONNECTION_STRING`.

## Dapper Compatibility

NuGet.org foi consultado em 2026-07-29:

| Package | Latest stable | Latest any |
| --- | ---: | ---: |
| Dapper | `2.1.79` | `2.1.79` |

Versoes validadas:

- minimum supported: `2.1.79`;
- latest supported stable: `2.1.79`.

Range nos pacotes:

```text
Dapper [2.1.79,3.0.0)
```

Risco restante: `DapperTypeHandlerAdapter` acessa
`SqlMapper.TypeHandlerCache<T>.Parse(object)` por reflection.

## .NET Compatibility

Validados:

- public packages: `netstandard2.0` build;
- tests/smoke/benchmarks: `net10.0`;
- SDK local: `10.0.302`, conforme `global.json`.

Nao foi alterado TFM minimo.

## Dommel Compatibility

Versao validada:

- Dommel `3.5.3`;
- range empacotado: `Dommel [3.5.3,4.0.0)`;
- `Dapper.FluentMap.Dommel.Tests`: 23 passed;
- provider tests cobrem persistencia Dommel em SQLite.

Limitacao: Dommel permanece process-wide via `DommelMapper`; nao e isolado por
`FluentMapRuntime`.

## Provider Validation

| Provider | Resultado |
| --- | --- |
| SQLite | Validated: 7 provider tests passed |
| SQL Server | Harness condicional presente; 7 skipped por env var ausente |
| PostgreSQL | Harness condicional presente; 7 skipped por env var ausente |
| MySQL/MariaDB | Not validated; apenas suporte por builder Dommel existente |
| SQL Server CE | Legacy/upstream-limited |

## Public API

Superficie revisada nos pacotes core, Dommel, DependencyInjection, Analyzers e
Generators. APIs historicas principais permanecem; APIs novas sao
majoritariamente aditivas; `DommelPropertyMap.GeneratedOption` e quebra
historica frente ao pacote original `2.0.0`.

## Binary Compatibility

Package validation nativa do SDK esta habilitada para core, Dommel e
DependencyInjection. Isso e suficiente como guardrail de RC, mas stable ainda
exige baseline formal do proprio fork.

## NuGet Packages

Executado:

```bash
dotnet pack ./Dapper.FluentMap.sln --configuration Release --no-build --output ./artifacts/packages-12.7-final
dotnet pack ./Dapper.FluentMap.sln --configuration Release --no-build --output ./artifacts/packages-12.7-rc -p:VersionPrefix=3.0.0-rc.1
```

Resultado:

- pack padrao: sucesso; 5 `.nupkg` e 3 `.snupkg`;
- pack RC override: sucesso; 5 `.nupkg` e 3 `.snupkg`;
- pack padrao ainda gera `2.0.0`, que nao deve ser publicado;
- conteudo dos `.nupkg`: README, assemblies/XML docs em `lib/netstandard2.0`
  para runtime packages; analyzers/generators em `analyzers/dotnet/cs`;
- sem test/benchmark/AOT smoke binaries nos pacotes inspecionados.

Metadata confirmada: license MIT, README, repository URL do fork, repository
branch/commit e ranges Dapper/Dommel esperados.

## SourceLink and Symbols

- `.snupkg` gerados para core, Dommel e DependencyInjection.
- Analyzer/generator incluem PDB no pacote principal.
- PDB do core contem SourceLink para o commit local
  `432705c118e697d4f51fecede1c1682d3d3f66fc`.
- `sourcelink` nao esta instalado localmente; checksum/download remoto deve
  ser validado em CI apos push.
- `dotnet nuget verify` confirmou hashes, mas falhou com `NU3004` porque os
  pacotes nao estao assinados.

## Generated Materialization

Etapa 7 nao foi regredida: generated registration tests passaram, os smokes
`generated:ok` e `di-generated:ok` passaram, e o benchmark smoke preserva o
perfil esperado de alocacao para generated vs runtime fallback.

## Persistence Semantics

Etapa 8 nao foi regredida: Dommel tests passaram, provider SQLite cobre
defaults/computed/read-only/non-identity key, e a documentacao preserva a
semantica historica de `Ignore()`.

## Advanced Query Materialization

Etapa 9 nao foi regredida: a suite cobre `QueryMultipleMapped`, `ReadMapped`,
streaming sync/async e cancellation; provider SQLite validou QueryMultiple e
streaming reais.

## Property Converters

Etapa 10 nao foi regredida: testes de converter/runtime/generated/TypeHandler
passam dentro da suite; write converters continuam documentados como
metadata-only.

## Configuration Isolation and DI

Etapa 11 nao foi regredida: DI tests passaram; runtime isolado segue valido para
caminhos controlados pelo FluentMap; Dapper global type maps e Dommel global
resolvers continuam limitacoes documentadas.

## Native AOT / Trimming

| Area | Classification |
| --- | --- |
| Explicit map registration | Trimming-safe preferred path |
| Generated registration | Trimming-safe preferred path for current compilation |
| Generated materializer hot path | AOT-friendlier generated path, subject to query API fallback boundary |
| Assembly scanning | Reflection fallback; trimming-sensitive |
| `QueryMapped*`, `ReadMapped*`, `QueryMultipleMapped`, streaming | Dynamic-code dependent when runtime fallback occurs |
| Full Native AOT | Not validated; not claimed |

Smokes executados:

- `explicit:ok`;
- `generated:ok`;
- `di-explicit:ok`;
- `di-generated:ok`;
- `PublishTrimmed=true` DI explicit/generated: publish e execucao passaram com
  warnings conhecidos `IL2104`;
- `PublishAot=true` DI explicit: falhou por ausencia de platform linker.

## Performance

Benchmark representativo executado com sucesso:

```bash
dotnet run --project ./benchmarks/Dapper.FluentMap.Benchmarks/Dapper.FluentMap.Benchmarks.csproj --configuration Release -- --filter "*MaterializationSteadyStateBenchmarks*QueryMappedSimple*" --job Dry
```

Alocacoes ShortRun relevantes:

| Scenario | Allocated |
| --- | ---: |
| `QueryMappedSimple` | 261.16 KB |
| `RuntimeQueryMappedSimple` | 261.16 KB |
| `QueryMappedSimpleRuntimeFallback` | 361.58 KB |
| `RuntimeQueryMappedSimpleRuntimeFallback` | 361.58 KB |
| `QueryMappedSimpleUnbuffered` | 245.20 KB |
| `QueryMappedSimpleUnbufferedRuntimeFallback` | 345.49 KB |
| `QueryMappedSimpleUnbufferedAsync` | 245.61 KB |

Nao foi observada regressao severa frente as baselines SDD. Tempos locais
seguem ruidosos e nao devem virar claim publico de throughput.

## Security / Supply Chain

- Vulnerability audit: sem vulnerabilidades reportadas.
- `NuGet.Config`: fonte unica NuGet.org.
- Dependabot configurado para GitHub Actions e NuGet.
- NuGet Audit transitive habilitado.
- CI usa permissoes minimas e actions pinadas por SHA.
- Release workflow gera metadata/provenance e bloqueia publish.
- Scan textual nao encontrou segredo real.
- SBOM formal e package signing permanecem adiados.

## Documentation

README, MIGRATION, COMPATIBILITY e SUPPORT correspondem a API real e documentam
os limites de providers, Dommel, AOT/trimming, write converters e global state.
Snippets representativos foram validados no Prompt 12.6 e a suite atual recompila
e testa as APIs correspondentes.

## Historical Regression Coverage

Etapas 7-11 continuam cobertas por generated registration/materialization tests,
historical core regressions, historical Dommel regressions,
QueryMultiple/streaming/cancellation tests, converter/TypeHandler tests,
configuration isolation/DI tests e provider SQLite tests.

## Breaking Changes

Nenhuma breaking change nova foi introduzida nesta auditoria final.

Known breaking/risky differences da linha atual:

- `DommelPropertyMap.GeneratedOption` diverge do pacote Dommel historico
  `2.0.0` por tipo nullable;
- validacoes novas podem rejeitar configuracoes contraditorias antes aceitas por
  acidente;
- pacote default `2.0.0` nao deve ser publicado pelo fork.

## Known Limitations

- Estado global permanece para `FluentMapper`, `SqlMapper.SetTypeMap` e
  `DommelMapper`.
- Dommel nao e isolado por `FluentMapRuntime`.
- SQL Server/PostgreSQL nao foram certificados nesta auditoria.
- MySQL/MariaDB nao foi validado.
- `QueryMultipleMappedAsync` nao existe.
- Write converters sao metadata-only no caminho Dapper/Dommel atual.
- Generated materializers possuem fallback runtime.
- Full Native AOT nao e suportado/declarado.

## Technical Debt

- Criar baseline API/binaria do fork apos primeira RC.
- Revisar manifests shipped/unshipped dos analyzers/generators antes de stable.
- Validar SourceLink por checksum em CI apos push.
- Decidir package signing e SBOM formal.
- Avaliar package lock ou Central Package Management em tarefa propria.
- Criar job provider real para SQL Server/PostgreSQL se esses providers forem
  promovidos a certificados.

## Release Blockers

Para RC:

- nao publicar artefatos `2.0.0`; usar versao pre-release do fork, recomendada
  `3.0.0-rc.1`;
- executar o release workflow no GitHub e validar SourceLink/provenance no SHA
  remoto;
- instalar os pacotes RC em um consumer smoke antes de qualquer promocao.

Para stable:

- baseline API/binaria do proprio fork;
- ciclo de feedback/adocao da RC;
- decisao de SBOM/package signing;
- manifests analyzer/generator fechados;
- SourceLink checksum validado;
- provider certification adicional, se houver claim alem de SQLite;
- Native AOT publish/run se houver claim AOT.

## Deferred Work

- Stable release.
- Publicacao NuGet.
- Git tag/GitHub Release.
- Full Native AOT.
- SQL Server/PostgreSQL CI service containers.
- MySQL/MariaDB harness.
- Runtime-isolated Dommel.
- Write converter execution.
- QueryMultiple async.
- SBOM SPDX/CycloneDX.

## Post-release Recommendations

1. Publicar primeiro `3.0.0-rc.1`, nao stable.
2. Validar instalacao dos cinco pacotes em um consumer smoke externo.
3. Rodar release workflow com provenance e artifact metadata no GitHub.
4. Criar baseline API/binaria a partir da RC aprovada.
5. Promover para stable somente apos feedback real e blockers zerados ou
   explicitamente aceitos.
