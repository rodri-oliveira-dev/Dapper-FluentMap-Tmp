# Public API Review

Revisao executada em 2026-07-29 para o Prompt 12.4, no checkout local
`feature/etapa-3`.

## Inputs

- Assemblies locais Release em `src/*/bin/Release/netstandard2.0`.
- Pacotes locais em `artifacts/packages-12.4-final`.
- Pacotes historicos NuGet.org `Dapper.FluentMap` `2.0.0` e
  `Dapper.FluentMap.Dommel` `2.0.0`.
- NuGet.org mostrou `Dapper.FluentMap` e `Dapper.FluentMap.Dommel` `2.0.0`
  como pacotes historicos/deprecated. Os pacotes `DependencyInjection`,
  `Analyzers` e `Generators` nao tinham historico nesses IDs.

## Tooling Decision

Tooling adotado agora:

- `EnablePackageValidation=true` nos pacotes com `lib/`:
  `Dapper.FluentMap`, `Dapper.FluentMap.Dommel` e
  `Dapper.FluentMap.DependencyInjection`.
- Package validation nativo do SDK roda durante `dotnet pack`.
- Baseline historico contra `2.0.0` original ficou documentado e opt-in para
  core/Dommel via `EnableFluentMapHistoricalApiCompatValidation`, porque a API
  do fork ja divergiu e uma baseline obrigatoria contra o pacote original nao
  representa um gate verde realista para esta linha.

Tooling nao adotado agora:

- `PublicApiAnalyzers`: redundante neste momento com package validation nativo
  do SDK e exigiria baseline textual grande antes da decisao de versao do fork.
- ApiCompat global tool permanente: nao necessario enquanto o SDK ja executa
  package validation no pack. Pode ser usado pontualmente em auditorias.

Decisao para release futura:

- Depois do primeiro RC do fork, definir `PackageValidationBaselineVersion`
  para a ultima versao aprovada do proprio fork e commitar suppressions apenas
  quando uma quebra for intencional e documentada.

## Dapper.FluentMap

Resumo da superficie publica atual:

- 51 tipos publicos e 298 membros publicos/protected observados.
- TFM: `netstandard2.0`.
- Assembly nao strong-named.
- Package validation nativo: habilitado.

Public surface:

- Facade global: `FluentMapper`, `FluentMapConfigurationException`.
- Runtime isolado: `FluentMapRuntime`, `FluentMapConfigurationBuilder`,
  `ImmutableFluentMapConfiguration`.
- Configuracao: `FluentMapConfiguration`, `FluentConventionConfiguration` e
  snapshots `EntityMappingConfiguration`, `ProfileMappingConfiguration`,
  `ConventionMappingConfiguration`, `PropertyMappingConfiguration`,
  `GeneratedMaterializerConfiguration`.
- Mapeamento: `EntityMap<TEntity>`, `EntityMapBase<TEntity,TPropertyMap>`,
  `PropertyMap`, `PropertyMapBase<TPropertyMap>`, `IEntityMap`,
  `IEntityMap<TEntity>`, `IPropertyMap`, `IMappingProfile`,
  `IProfileMap<TProfile>`.
- Conversao/persistencia: `IPropertyConverter<>`,
  `IReadPropertyConverter<>`, `IWritePropertyConverter<>`,
  `PropertyConversionMetadata`, `PropertyConverterMetadata`,
  `PropertyPersistenceMetadata`, delegates de conversao.
- Materializacao gerada: `GeneratedMaterializerColumn`,
  `GeneratedMaterializerDescriptor<TEntity>`, `GeneratedRowMaterializer<TEntity>`.
- Diagnosticos: `MappingExplanation`, `MemberMappingExplanation`,
  `ConstructorParameterExplanation`, `MappingSource`,
  `MappingMaterialization`.
- Convencoes/naming: `Convention`, `ConventionPropertyConfiguration`,
  `PropertyConventionConfiguration`, `NamingPolicy`.
- Dapper integration/query helpers: `QueryMappedExtensions`,
  `MappedGridReader`, `FluentMapTypeMap<TEntity>`,
  `FluentConventionTypeMap<TEntity>`, `MultiTypeMap`.
- Utilities publicas historicas: `ReflectionHelper` e
  `FluentMapConfigurationExtensions`.

Newly introduced APIs versus `Dapper.FluentMap` `2.0.0` original:

- Runtime/configuracao isolada (`FluentMapRuntime`,
  `FluentMapConfigurationBuilder`, `ImmutableFluentMapConfiguration`).
- Profiles (`IMappingProfile`, `IProfileMap<TProfile>`, `AddProfile`).
- Naming policies (`NamingPolicy`, `UseNamingPolicy`).
- Query/materialization APIs (`QueryMapped*`, `QueryMultipleMapped`,
  `MappedGridReader`, streaming sync/async).
- Generated materializer contract and registration APIs.
- Conversion metadata and converter interfaces/delegates.
- Persistence metadata (`ReadOnly`, `Computed`, database default, generated
  and identity semantics).
- Diagnostics/explanation API.

Obsolete APIs:

- Nenhum `[Obsolete]` encontrado.

Accidental API candidates:

- `Dapper.FluentMap.Utils.ReflectionHelper` era publico no pacote original e
  deve ser tratado como legado publico, mesmo parecendo utilitario interno.
- `Dapper.FluentMap.Utils.FluentMapConfigurationExtensions` e nova API publica
  em namespace `Utils`; revisar antes de stable se deve permanecer contrato
  publico.
- Type maps em `Dapper.FluentMap.TypeMaps` sao publicos historicos e servem
  como extension points avancados; nao remover sem major.

Extension points:

- `IEntityMap<TEntity>`, `EntityMap<TEntity>`,
  `EntityMapBase<TEntity,TPropertyMap>`.
- `Convention` e configuracoes de convencao.
- `IPropertyConverter<>`, `IReadPropertyConverter<>`,
  `IWritePropertyConverter<>` e delegates.
- `FluentMapTypeMap<TEntity>`, `FluentConventionTypeMap<TEntity>` e
  `MultiTypeMap` para integracao avancada com Dapper.

Historical compatibility classification:

- Compatible: tipos historicos principais continuam presentes.
- Compatible/additive: a maioria da superficie nova e aditiva.
- Behaviorally changed: validacao, materializacao aninhada, converters,
  persistence metadata e query helpers alteram capacidades observaveis.
- Source breaking: nenhum membro historico removido foi confirmado no core; os
  headers de `PropertyMap`/`PropertyMapBase` mudaram por interfaces adicionais.
- Binary breaking: nao prometer compatibilidade absoluta; interfaces publicas
  adicionais em tipos historicos geralmente sao aditivas, mas a linha do fork
  ainda exige ApiCompat formal contra uma baseline aprovada do proprio fork.
- Intentionally replaced: assembly scanning continua existindo, mas registro
  explicito/gerado e runtime isolado sao caminhos preferenciais novos.

## Dapper.FluentMap.Dommel

Resumo da superficie publica atual:

- 8 tipos publicos e 23 membros publicos/protected observados.
- TFM: `netstandard2.0`.
- Assembly nao strong-named.
- Package validation nativo: habilitado.

Public surface:

- `FluentMapConfigurationExtensions.ForDommel()`.
- `DommelEntityMap<TEntity>`, `DommelPropertyMap`, `IDommelEntityMap`.
- Resolvers publicos: `DommelColumnNameResolver`,
  `DommelKeyPropertyResolver`, `DommelPropertyResolver`,
  `DommelTableNameResolver`.

Newly introduced APIs versus `Dapper.FluentMap.Dommel` `2.0.0` original:

- `DommelPropertyMap` agora tambem implementa metadata de conversion/persistence
  herdada do core.
- Metodos de persistencia Dommel/core ampliados por `PropertyMapBase`.

Obsolete APIs:

- Nenhum `[Obsolete]` encontrado.

Accidental API candidates:

- Resolvers sao publicos desde a linha historica e devem ser tratados como
  extension points publicos, mesmo quando usados principalmente pelo bridge.

Extension points:

- `DommelEntityMap<TEntity>` para mapas Dommel.
- `DommelPropertyMap` para key/identity/generated options.
- Resolvers Dommel publicos para integracao com extension points globais do
  Dommel.

Historical compatibility classification:

- Compatible: tipos principais e `ForDommel()` continuam presentes.
- Behaviorally changed: persistence metadata agora interage com key/identity,
  computed/default/read-only semantics.
- Source breaking: `DommelPropertyMap.GeneratedOption` mudou de
  `DatabaseGeneratedOption` para `DatabaseGeneratedOption?`; consumidores que
  assumem tipo nao-nullable podem precisar ajuste.
- Binary breaking: a mudanca de tipo de `GeneratedOption` altera assinatura de
  getter/setter e e quebra binaria frente ao pacote original `2.0.0`.
- Intentionally replaced: nenhum pacote substituto; Dommel continua opcional e
  process-wide.

## Dapper.FluentMap.DependencyInjection

Resumo da superficie publica atual:

- 1 tipo publico e 1 metodo publico observado.
- TFM: `netstandard2.0`.
- Assembly nao strong-named.
- Package validation nativo: habilitado.
- Nao ha pacote historico NuGet.org nesse ID.

Public surface:

- `Microsoft.Extensions.DependencyInjection.FluentMapServiceCollectionExtensions`.
- `IServiceCollection AddFluentMap(Action<FluentMapConfigurationBuilder>)`.

Newly introduced APIs:

- Pacote novo do fork; toda a superficie e nova.

Obsolete APIs:

- Nenhum `[Obsolete]` encontrado.

Accidental API candidates:

- Nenhum candidato atual. O namespace `Microsoft.Extensions.DependencyInjection`
  e intencional para extension method discovery.

Extension points:

- Callback de configuracao via `FluentMapConfigurationBuilder`.

Historical compatibility classification:

- Intentionally replaced/new: pacote novo, sem baseline historica.
- Binary compatibility futura deve ser medida contra o primeiro RC/estavel do
  fork.

## Dapper.FluentMap.Analyzers

Resumo da superficie publica atual:

- 1 tipo publico e 13 membros publicos/protected observados.
- TFM: `netstandard2.0`.
- Assembly nao strong-named.
- Layout NuGet: `analyzers/dotnet/cs`.
- Nao ha dependencias runtime no nuspec por causa de
  `SuppressDependenciesWhenPacking=true` e `PrivateAssets=all`.
- Nao ha pacote historico NuGet.org nesse ID.

Public surface:

- `FluentMapConfigurationAnalyzer : DiagnosticAnalyzer`.
- `SupportedDiagnostics`, `Initialize`.
- Diagnostic IDs publicos: `DFM001`-`DFM015` conforme manifests/regras atuais.

Newly introduced APIs:

- Pacote novo do fork; toda a superficie e nova.

Obsolete APIs:

- Nenhum `[Obsolete]` encontrado.

Accidental API candidates:

- Campos publicos de diagnostic IDs sao contrato de usuario depois de publicado.
  Manter estabilidade de ID/severity/categoria dentro da major.

Extension points:

- O analyzer class e carregado pelo compilador. Nao ha extensibilidade publica
  planejada para consumidores alem de configurar severities no projeto
  consumidor.

Historical compatibility classification:

- Intentionally replaced/new: pacote novo, sem baseline historica.
- Binary compatibility futura deve ser medida contra o primeiro RC/estavel do
  fork.

## Dapper.FluentMap.Generators

Resumo da superficie publica atual:

- 1 tipo publico intencional observado no fonte/assembly:
  `MappingRegistrationGenerator : IIncrementalGenerator`.
- TFM: `netstandard2.0`.
- Assembly nao strong-named.
- Layout NuGet: `analyzers/dotnet/cs`.
- Nao ha dependencias runtime no nuspec por causa de
  `SuppressDependenciesWhenPacking=true` e `PrivateAssets=all`.
- Nao ha pacote historico NuGet.org nesse ID.

Public surface:

- `MappingRegistrationGenerator : IIncrementalGenerator`.
- Metodo `Initialize(IncrementalGeneratorInitializationContext)`.
- API gerada para consumidores: `AddGeneratedMappings()` em codigo fonte
  emitido durante compilacao.

Newly introduced APIs:

- Pacote novo do fork; toda a superficie e nova.
- O contrato mais importante para consumidores e o codigo gerado
  `AddGeneratedMappings()`, nao o tipo do generator em si.

Obsolete APIs:

- Nenhum `[Obsolete]` encontrado.

Accidental API candidates:

- Nenhum candidato alem do proprio tipo generator, que precisa ser publico para
  carregamento Roslyn.

Extension points:

- Nao ha extensibilidade publica planejada; consumidores influenciam o generator
  declarando maps elegiveis no proprio projeto.

Historical compatibility classification:

- Intentionally replaced/new: pacote novo, sem baseline historica.
- Source compatibility futura deve incluir o shape de `AddGeneratedMappings()`.

## NuGet Metadata Review

Estado apos Prompt 12.4:

- `PackageId`: preservado para os cinco pacotes.
- `VersionPrefix`: mantido em `2.0.0`; nao publicar essa versao porque core e
  Dommel ja possuem `2.0.0` historico no NuGet.org.
- `Authors`: mantido como `Henk Mollema` para preservar atribuicao historica.
- `PackageProjectUrl`/`RepositoryUrl`: apontam para
  `https://github.com/rodri-oliveira-dev/Dapper-FluentMap`.
- License: `PackageLicenseExpression=MIT`; `PackageLicenseUrl` removido de
  core/Dommel.
- README: presente nos cinco `.nupkg`.
- Tags: preservadas por pacote.
- Release notes: nao adicionadas enquanto a versao/RC final nao estiver
  decidida; release notes devem ser preenchidas junto do RC.
- Icon: nao adicionado porque nao ha ativo apropriado aprovado.
- Dependency ranges:
  - Dapper: `[2.1.79,3.0.0)`.
  - Dommel: `[3.5.3,4.0.0)`.
  - Microsoft dependencies permanecem com minima atual, sem upper bound, por
    serem contratos de plataforma/abstractions e nao haver evidencia de quebra
    major especifica neste prompt.

## Symbols, SourceLink And Determinism

Estado apos Prompt 12.4:

- `PublishRepositoryUrl=true`, `RepositoryType=git`,
  `RepositoryUrl=https://github.com/rodri-oliveira-dev/Dapper-FluentMap`.
- `EmbedUntrackedSources=true`.
- `Deterministic=true`.
- `ContinuousIntegrationBuild=true` apenas quando `CI=true`.
- `IncludeSymbols=true` e `SymbolPackageFormat=snupkg` para pacotes runtime com
  `lib/`.
- Analyzer/generator nao geram `.snupkg` porque esse layout nao possui `lib/`;
  seus PDBs sao empacotados em `analyzers/dotnet/cs`.

Validacao observada:

- `.snupkg` gerado para core, Dommel e DependencyInjection.
- PDBs presentes em `.snupkg` para pacotes runtime.
- PDBs presentes no `.nupkg` de Analyzer e Generator ao lado das DLLs Roslyn.
- `sourcelink print-json` encontrou mapeamento GitHub em todos os PDBs.
- `sourcelink test` completo nao foi usado como gate porque o commit local ainda
  nao estava publicado no remoto; a validacao completa de download/checksum deve
  rodar em CI apos push.

## Package Contents

Pacotes inspecionados em `artifacts/packages-12.4-final`:

- `Dapper.FluentMap.2.0.0.nupkg`: `README.md`,
  `lib/netstandard2.0/Dapper.FluentMap.dll`, XML docs.
- `Dapper.FluentMap.Dommel.2.0.0.nupkg`: `README.md`,
  `lib/netstandard2.0/Dapper.FluentMap.Dommel.dll`, XML docs.
- `Dapper.FluentMap.DependencyInjection.2.0.0.nupkg`: `README.md`,
  `lib/netstandard2.0/Dapper.FluentMap.DependencyInjection.dll`, XML docs.
- `Dapper.FluentMap.Analyzers.2.0.0.nupkg`: `README.md`,
  `analyzers/dotnet/cs/Dapper.FluentMap.Analyzers.dll` e PDB.
- `Dapper.FluentMap.Generators.2.0.0.nupkg`: `README.md`,
  `analyzers/dotnet/cs/Dapper.FluentMap.Generators.dll` e PDB.

Ausencias confirmadas:

- Sem binarios de teste.
- Sem `.sdd`.
- Sem artifacts internos.
- Sem arquivos temporarios.
- Sem secrets observados no conteudo do pacote.

## Analyzer And Generator Layout

- Assemblies Roslyn ficam em `analyzers/dotnet/cs`.
- `IncludeBuildOutput=false` impede `lib/` acidental.
- `SuppressDependenciesWhenPacking=true` impede dependencias Roslyn transitivas
  no nuspec.
- `Microsoft.CodeAnalysis.CSharp` e `Microsoft.CodeAnalysis.Analyzers` seguem
  com `PrivateAssets=all`.
- Consumers nao recebem Roslyn como dependencia runtime dos pacotes Analyzer e
  Generator.

## Strong Naming

Estado atual:

- Assemblies nao possuem public key token.
- Pacotes historicos locais tambem devem ser tratados como linha sem strong-name
  ate prova contraria por baseline formal.

Decisao:

- Nao adicionar strong naming neste prompt.

Racional:

- Strong naming altera identidade de assembly e pode ser breaking para
  consumidores.
- Nao ha requisito de GAC, binding policy ou ecossistema corporativo concreto
  neste prompt.
- Se necessario futuramente, deve ser decisao de release/major separada, com
  chave, assinatura, verificacao e estrategia de migracao.

## Package Signing

Estado atual:

- `dotnet nuget verify artifacts/packages-12.4-final/*.nupkg` confirma hashes, mas
  falha com `NU3004` porque os pacotes nao estao assinados.

Decisao:

- Nao assinar pacotes neste prompt.
- Package signing deve ser tratado como release engineering separado, com
  certificado, owner do segredo, rotação e CI seguro.

## Release Blockers After This Review

- Critical: estrategia de versao ainda precisa mudar antes de publicar, porque
  `2.0.0` ja existe para core/Dommel.
- Critical: baseline de API do fork ainda precisa ser estabelecida apos o
  primeiro RC/versao aprovada.
- High: SourceLink URL/checksum precisa ser testado em CI apos push do commit.
- Medium: package signing segue ausente por decisao consciente.
- Medium: analyzer/generator release manifests ainda precisam revisao antes de
  stable.
