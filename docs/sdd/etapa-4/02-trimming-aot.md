# 02 - Trimming E Native AOT

## Specification

Esta entrega mediu e melhorou a compatibilidade do core `Dapper.FluentMap` com IL trimming, single-file e Native AOT, preservando o target `netstandard2.0`.

Objetivos tratados:

- medir baseline de publish trimmed e Native AOT em um consumidor pequeno;
- separar o caminho explicito de registro do caminho por assembly scanning;
- remover reflection redundante no registro do type map interno;
- adicionar annotations de trimming quando o contrato e verificavel;
- marcar APIs de scanning como dependentes de reflection;
- documentar warnings de propriedade do FluentMap e do Dapper.

Fora do objetivo:

- declarar compatibilidade Native AOT completa;
- alterar o target do core para `net10.0`;
- criar source generator;
- corrigir warnings internos do Dapper;
- tornar assembly scanning seguro a qualquer custo.

## Discovery

Arquivos e contexto analisados:

- `AGENTS.md`
- `.agents/skills/dotnet-aot-compat/SKILL.md`
- `.agents/skills/dotnet-aot-compat/references/polyfills.md`
- `.agents/skills/run-tests/SKILL.md`
- `.agents/skills/msbuild-antipatterns/SKILL.md`
- `docs/sdd/etapa-1/README.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-2/README.md`
- `docs/sdd/etapa-2/decisions.md`
- `docs/sdd/etapa-3/README.md`
- `docs/sdd/etapa-3/decisions.md`
- `docs/sdd/etapa-3/01-mapping-registration.md`
- `docs/sdd/etapa-4/README.md`
- `docs/sdd/etapa-4/status.md`
- `docs/sdd/etapa-4/decisions.md`
- `docs/sdd/etapa-4/01-roslyn-analyzers.md`
- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `src/Dapper.FluentMap/Configuration/FluentConventionConfiguration.cs`
- `src/Dapper.FluentMap/Mapping/EntityMap.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/TypeMaps/*`
- `src/Dapper.FluentMap/Utils/FluentMapConfigurationExtensions.cs`
- `src/Dapper.FluentMap/Utils/ReflectionHelper.cs`

Entrega 1 confirmada:

- `docs/sdd/etapa-4/status.md` registrava `01 - Roslyn Analyzers` como `Concluido`;
- `docs/sdd/etapa-4/01-roslyn-analyzers.md` registrava validacao completa e pacote de analyzer inspecionado.

Busca executada:

```text
rg -n "Assembly|GetTypes|GetExportedTypes|GetMember|GetProperty|GetConstructor|Activator\.CreateInstance|MakeGenericMethod|\.Invoke\(|CreateDelegate|Dynamic|Expression\.Compile|Type\.GetType|RuntimeTypeHandle|MakeGenericType|PropertyInfo|MemberInfo" src test docs\sdd\etapa-4 -g "*.cs" -g "*.csproj" -g "*.md"
```

Classificacao dos usos relevantes no core:

| Uso | Local | Classificacao | Decisao |
|---|---|---|---|
| `Assembly.GetExportedTypes()` | `FluentMapConfiguration.AddMapsFromAssembly(...)` | Reflection-dependent por design | API marcada com `RequiresUnreferencedCode`; scanning documentado como trimming-sensitive |
| `Assembly.GetCallingAssembly().GetExportedTypes()` | `FluentConventionConfiguration.ForEntitiesInCurrentAssembly(...)` | Reflection-dependent por design | API marcada com `RequiresUnreferencedCode` |
| `Assembly.GetExportedTypes()` para conventions | `FluentConventionConfiguration.ForEntitiesInAssembly(...)` | Reflection-dependent por design | API marcada com `RequiresUnreferencedCode` |
| `Assembly.GetTypes()` | `FluentMapConfigurationExtensions.ApplyMapsFromAssemblies(...)` | Reflection-dependent por design | API legada marcada com `RequiresUnreferencedCode` |
| `Activator.CreateInstance(mapType)` | Scanning de maps | Trimming-sensitive | Mantido apenas no caminho de scanning e coberto pelo aviso da API |
| `Activator.CreateInstance(typeof(FluentMapTypeMap<>).MakeGenericType(type))` | `MappingRegistry.SetDapperTypeMap(...)` | Pode ser removido | Substituido por type map interno nao generico |
| `MakeGenericMethod(...).Invoke(...)` | `ApplyMapsFromAssemblies(...)` legado | Reflection-dependent por design | Mantido por compatibilidade e marcado como trimming-sensitive |
| `Type.GetInterfaces()` | `AddMap<TMap>()` | Trimming-sensitive, anotavel | `TMap` anotado com preservacao de `Interfaces` |
| `Type.GetProperties(...)` | `ForEntity<T>()` e `Explain<TEntity>()` | Trimming-sensitive, anotavel | `ForEntity<T>()`, `MapProperties(...)`, `Explain<TEntity>()` e helpers anotados |
| `Type.GetConstructors(...)` | `Explain<TEntity>()` | Trimming-sensitive, anotavel | `Explain<TEntity>()` e helpers anotados |
| `PropertyInfo` / `MemberInfo` via expression tree | `ReflectionHelper`, `MemberPath`, `PropertyMap` | Reflection metadata por contrato | Mantido; nao faz scanning nem busca ampla por nome |
| `ConstructorInfo` / `ParameterInfo` recebidos do Dapper | `FluentConstructorTypeMap` | AOT-safe no FluentMap; depende do Dapper para discovery | Mantido; warnings restantes classificados como dependency-owned |

Areas especiais:

- Assembly scanning: permanece convenience reflection-dependent.
- Registro generico: `AddMap<TMap>()` e o caminho recomendado; usa reflection limitada para inferir `IEntityMap<TEntity>`, agora anotada e sem warning do FluentMap no smoke trimmed explicito.
- Constructor mapping: nao materializa objetos; traduz metadata para o Dapper. Warnings de discovery de construtor no smoke trimmed vem de `Dapper.DefaultTypeMap`.
- MemberPath: usa `PropertyInfo` obtido da expression ou de convention ja configurada; nao adiciona scanning.
- Convention discovery: `ForEntity<T>()` e o caminho explicito anotado; `ForEntitiesInAssembly(...)` e `ForEntitiesInCurrentAssembly(...)` continuam dependentes de scanning.
- Analyzer: fica isolado em `Dapper.FluentMap.Analyzers` e nao altera runtime do core.
- `Explain<TEntity>()`: faz diagnostico por reflection sobre propriedades/construtores publicos e foi anotado.

## Baseline

Ambiente:

- SDK: `10.0.302`
- Runtime alvo do consumidor smoke: `net10.0`
- RID usado: `win-x64`
- Core: `netstandard2.0`

Baseline por `ProjectReference`:

```text
dotnet publish ... -p:PublishTrimmed=true
```

Resultado:

- falhou antes da analise do consumidor com `NETSDK1124`, porque `PublishTrimmed` foi propagado ao projeto `src/Dapper.FluentMap` `netstandard2.0`;
- `dotnet publish ... -p:PublishAot=true` falhou com `NETSDK1207` pelo mesmo motivo conceitual.

Baseline usando referencia direta ao assembly compilado do core:

```text
dotnet build .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release
dotnet publish <consumidor temporario> --configuration Release --runtime win-x64 --self-contained true -p:PublishTrimmed=true -p:TrimMode=full -p:PublishAot=false -p:TrimmerSingleWarn=false -p:ILLinkTreatWarningsAsErrors=false
```

Resultado trimmed antes das mudancas:

- publish concluido;
- runtime executou `explicit:Id` e `scanning:Id` no consumidor combinado;
- warnings FluentMap-owned:
  - `IL2067` em `FluentMapConfiguration.CreateEntityMap(Type)` por `Activator.CreateInstance(Type)`;
  - `IL2026` em `FluentMapConfiguration.GetExportedTypes(Assembly)` por `Assembly.GetExportedTypes()`;
  - `IL2070` em `FluentMapConfiguration.GetMappedEntityType(Type)` por `Type.GetInterfaces()`.
- warnings dependency-owned:
  - `IL2046`, `IL2092`, `IL2075`, `IL2070` em fontes do Dapper.

Baseline Native AOT antes das mudancas:

```text
dotnet publish <consumidor temporario> --configuration Release --runtime win-x64 --self-contained true -p:PublishAot=true
```

Resultado:

- build gerou o DLL intermediario do consumidor;
- publish falhou no ambiente com `Platform linker not found`;
- runtime Native AOT nao foi validado porque faltam os pre-requisitos de toolchain C++ para Native AOT no Windows.

## Decision

Registro explicito:

- `AddMap<TMap>()` permanece a API explicita moderna;
- `TMap` recebeu annotation para preservar interfaces, permitindo a inferencia de `IEntityMap<TEntity>` sem warning do FluentMap no smoke trimmed explicito;
- a instancia do map passou a ser criada por `new TMap()`, removendo `Activator.CreateInstance` do caminho explicito.

Assembly scanning:

- `AddMapsFromAssembly(...)`, `AddMapsFromAssemblyContaining<TMarker>()`, `ForEntitiesInAssembly(...)`, `ForEntitiesInCurrentAssembly(...)` e `ApplyMapsFromAssemblies(...)` foram marcados com `RequiresUnreferencedCode`;
- scanning continua suportado em runtime normal;
- scanning trimmed pode falhar em runtime quando o trimmer remove tipos, interfaces ou construtores que so seriam descobertos por reflection;
- nao foi usado `UnconditionalSuppressMessage`, `NoWarn`, `SuppressTrimAnalysisWarnings` ou `#pragma`.

Type map interno:

- o registry deixou de criar `FluentMapTypeMap<TEntity>` por `MakeGenericType` + `Activator.CreateInstance`;
- foi adicionado um type map interno nao generico para registro no Dapper;
- a classe publica `FluentMapTypeMap<TEntity>` permanece para compatibilidade.

Annotations:

- polyfills internos foram adicionados para manter `netstandard2.0`;
- `DynamicallyAccessedMembers` foi usado apenas onde o fluxo e verificavel;
- `RequiresUnreferencedCode` foi usado em APIs de scanning e helpers privados exclusivos desse caminho;
- `Explain<TEntity>()` foi anotado porque enumera propriedades e construtores publicos.

Impacto publico:

- nenhuma API publica foi removida;
- annotations em APIs publicas passam a expor warnings corretos para consumidores trimmed/AOT;
- scanning agora avisa o consumidor em publish trimmed/AOT em vez de esconder a fragilidade.

## Delivery

Arquivos adicionados:

- `src/Dapper.FluentMap/Compatibility/CodeAnalysisAttributes.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentMapTypeMap.cs`
- `test/Dapper.FluentMap.AotSmoke/Dapper.FluentMap.AotSmoke.csproj`
- `test/Dapper.FluentMap.AotSmoke/Program.cs`
- `docs/sdd/etapa-4/02-trimming-aot.md`

Arquivos alterados:

- `Dapper.FluentMap.sln`
- `README.md`
- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `src/Dapper.FluentMap/Configuration/FluentConventionConfiguration.cs`
- `src/Dapper.FluentMap/FluentMapper.cs`
- `src/Dapper.FluentMap/Mapping/EntityMap.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/Utils/FluentMapConfigurationExtensions.cs`
- `docs/sdd/etapa-4/decisions.md`
- `docs/sdd/etapa-4/status.md`

Comportamento preservado:

- core continua `netstandard2.0`;
- `AddMap(new CustomerMap())` permanece;
- `AddMap<CustomerMap>()` permanece;
- assembly scanning continua funcionando em runtime normal;
- conventions explicitas e naming policies continuam funcionando;
- constructor mapping continua delegado ao Dapper.

Comportamento/documentacao alterados:

- APIs de scanning emitem warning de trimming/AOT via `RequiresUnreferencedCode`;
- registro explicito nao emite warnings FluentMap-owned no smoke trimmed;
- registry nao depende mais de `MakeGenericType` + `Activator.CreateInstance` para instalar o type map interno.

## Validation

Validacao localizada executada durante a entrega:

```text
dotnet build .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-restore
```

Resultado:

- sucesso, 0 warnings, 0 erros.

Smoke normal:

```text
dotnet run --project .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release
dotnet run --project .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release -p:DefineConstants=AOT_SMOKE_SCANNING
```

Resultado:

- `explicit:ok`;
- `scanning:ok`.

Publish trimmed explicito:

```text
dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishTrimmed=true -p:TrimMode=full -p:PublishAot=false -p:TrimmerSingleWarn=false -p:ILLinkTreatWarningsAsErrors=false
.\test\Dapper.FluentMap.AotSmoke\bin\Release\net10.0\win-x64\publish\Dapper.FluentMap.AotSmoke.exe
```

Resultado:

- publish concluido;
- runtime: `explicit:ok`;
- 0 warnings FluentMap-owned;
- warnings restantes dependency-owned no Dapper: `IL2080`, `IL2046`, `IL2092`, `IL2075`, `IL2070`.

Publish trimmed scanning:

```text
dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishTrimmed=true -p:TrimMode=full -p:PublishAot=false -p:DefineConstants=AOT_SMOKE_SCANNING -p:TrimmerSingleWarn=false -p:ILLinkTreatWarningsAsErrors=false
.\test\Dapper.FluentMap.AotSmoke\bin\Release\net10.0\win-x64\publish\Dapper.FluentMap.AotSmoke.exe
```

Resultado:

- publish concluido;
- warning FluentMap-owned esperado: `IL2026` na chamada de `AddMapsFromAssemblyContaining<TMarker>()`;
- runtime falhou: `Column 'customer_id' was not mapped to property 'Id'.`;
- falha confirma que scanning depende de metadata que pode ser removida pelo trimmer.

Publish Native AOT explicito:

```text
dotnet publish .\test\Dapper.FluentMap.AotSmoke\Dapper.FluentMap.AotSmoke.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishAot=true -p:TrimmerSingleWarn=false -p:ILLinkTreatWarningsAsErrors=false -p:MSBuildWarningsAsMessages=
```

Resultado:

- build gerou `Dapper.FluentMap.AotSmoke.dll`;
- publish falhou com `Platform linker not found`;
- runtime Native AOT nao foi validado neste ambiente.

Validacao final completa:

```text
dotnet restore
dotnet build
dotnet test
dotnet build --configuration Release
dotnet test --configuration Release
dotnet pack .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-build --output .\artifacts\packages
```

Resultado:

- `dotnet restore`: sucesso;
- `dotnet build`: sucesso, 0 warnings, 0 erros;
- `dotnet test`: sucesso, 128 testes do core, 7 testes Dommel e 7 testes do analyzer;
- `dotnet build --configuration Release`: sucesso, 0 warnings, 0 erros;
- `dotnet test --configuration Release`: sucesso, 128 testes do core, 7 testes Dommel e 7 testes do analyzer.
- `dotnet pack`: pacote `Dapper.FluentMap.2.0.0.nupkg` criado; warning existente `NU5125` sobre `PackageLicenseUrl` legado.

Inspecao do pacote:

- contem `lib/netstandard2.0/Dapper.FluentMap.dll`;
- contem `lib/netstandard2.0/Dapper.FluentMap.xml`;
- nuspec mantem dependencia `Dapper` `2.1.79` para `.NETStandard2.0`;
- nao contem projetos de teste nem o smoke app.

Confirmacoes:

- `src/Dapper.FluentMap/Dapper.FluentMap.csproj` continua com `TargetFrameworks` igual a `netstandard2.0`;
- registro explicito nao ganhou assembly scanning;
- nenhum warning foi silenciado por `NoWarn`, `SuppressTrimAnalysisWarnings`, `UnconditionalSuppressMessage` ou `#pragma`;
- APIs reflection-heavy estao anotadas e documentadas;
- Dommel nao recebeu alteracao funcional.

## Matriz

| Funcionalidade | Normal | Trimmed | Native AOT | Observacao |
|---|---|---|---|---|
| Registro explicito | Suportado | Publica e executa no smoke; sem warnings FluentMap-owned | Publish bloqueado pelo linker ausente; sem runtime validado | Caminho recomendado para trimmed/AOT |
| Assembly scanning | Suportado | Publica com `IL2026` e falha no smoke scanning trimmed | Nao validado em runtime; tratado como reflection-dependent | Mantido como convenience, nao como caminho AOT-friendly |
| Naming policies | Suportado via `ForEntity<TEntity>()` | Validado no smoke explicito trimmed | Nao validado em runtime | `ForEntitiesInAssembly(...)` segue trimming-sensitive |
| Constructor mapping | Suportado | Validado no smoke explicito trimmed; warnings restantes vem do Dapper | Nao validado em runtime | FluentMap traduz metadata; Dapper faz discovery final |

## Limitacoes Restantes

- Native AOT runtime nao foi executado porque o ambiente nao possui o linker C++ exigido pelo SDK.
- Dapper ainda emite warnings de trimming/AOT no smoke; esses warnings nao pertencem ao FluentMap e nao foram corrigidos internamente.
- Assembly scanning nao e seguro sob trimming por contrato; a Entrega 3 pode substituir esse caminho por metadata gerada.
- `PropertyInfo` e `MemberInfo` continuam parte do contrato publico e da integracao com Dapper.
- O projeto smoke usa referencia direta ao assembly compilado do core durante publish trimmed/AOT para evitar propagacao de `PublishTrimmed`/`PublishAot` ao projeto `netstandard2.0`; os comandos de publish devem ser precedidos por build do core em `Release`.
