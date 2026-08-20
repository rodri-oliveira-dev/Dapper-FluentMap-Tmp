# Configuration State Discovery

## Escopo examinado

Arquivos principais examinados no prompt 11.1:

- `README.md`
- `Dapper.FluentMap.sln`
- `src/Dapper.FluentMap/FluentMapper.cs`
- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `src/Dapper.FluentMap/Configuration/FluentConventionConfiguration.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/Mapping/EntityMap.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMap.cs`
- `src/Dapper.FluentMap/Mapping/PropertyPersistenceMetadata.cs`
- `src/Dapper.FluentMap/Mapping/PropertyConversionMetadata.cs`
- `src/Dapper.FluentMap/Materialization/*`
- `src/Dapper.FluentMap/QueryMappedExtensions.cs`
- `src/Dapper.FluentMap/MappedGridReader.cs`
- `src/Dapper.FluentMap/TypeMaps/*`
- `src/Dapper.FluentMap.Dommel/*`
- projetos de testes, analyzers, generators, AOT smoke e benchmarks por arquivos de projeto e pontos de integracao
- `.sdd/etapa-7/FINAL-REPORT.md`, `.sdd/etapa-8/FINAL-REPORT.md`, `.sdd/etapa-9/FINAL-REPORT.md`
- `.sdd/etapa-10/FINAL-REPORT.md` e `.sdd/etapa-10/STATUS.md`

## Estado global identificado

| Estado | Local | Classificacao | Mutacao atual | Thread safety atual | Observacoes |
| --- | --- | --- | --- | --- | --- |
| Registry default | `FluentMapper._registry` | Global compatibility state | Criado uma vez no processo | Instancia unica; containers internos sao concorrentes | Fonte efetiva de toda configuracao e cache do core. |
| Configuration default | `FluentMapper._configuration` | Global compatibility state | Reusado por todas as chamadas `Initialize` | Sem lock proprio | Fachada mutavel que escreve no registry global. |
| Default entity maps | `MappingRegistry.EntityMaps`, exposto como `FluentMapper.EntityMaps` | Configuration state + global compatibility state | `AddMap`, `TryAdd`, indexer, `Clear`, `Reset` | `ConcurrentDictionary`, mas valores sao mutaveis | API publica permite bypass de validacao, invalidacao de cache e instalacao de type map Dapper. |
| Profile maps | `MappingRegistry.ProfileMaps` | Configuration state | `AddProfileMap`, `Reset` | `ConcurrentDictionary`, valores mutaveis | Interno, mas ainda process-wide via registry global. |
| Type conventions | `MappingRegistry.TypeConventions`, exposto como `FluentMapper.TypeConventions` | Configuration state + global compatibility state | `AddConvention`, `Clear`, mutacao de `IList<Convention>` | Dicionario concorrente; listas e conventions nao sao imutaveis | `AddConvention` substitui listas por copia, mas API publica expoe listas mutaveis. |
| Property map cache | `MappingRegistry._propertyMapCache` | Cache derived from configuration | Lazy `GetOrAdd`, clear/reset, invalidacao por tipo | `ConcurrentDictionary` | Chave contem tipo, profile, coluna e estrategia, mas nao versiona configuracao. |
| Runtime materialization plan cache | `MappingRegistry._materializationPlanCache` | Cache derived from configuration | Lazy `GetOrAdd`, clear/reset, invalidacao por tipo | `ConcurrentDictionary` | Chave contem tipo, profile e shape ordenado de colunas. Depende de maps, conventions, converters e metadata de persistence para ignore/read. |
| Generated materializer registry | `MappingRegistry._generatedMaterializers` | Runtime immutable state + cache derived from configuration | `AddGeneratedMaterializer`, clear/reset | `ConcurrentDictionary` | Descritores sao registrados durante configuracao, mas ficam no mesmo registry global. Validacao de match consulta mapping efetivo atual. |
| Dapper type maps | `SqlMapper.SetTypeMap` em `MappingRegistry.SetDapperTypeMap` e `Reset` | Process-wide integration state | Instalado ao registrar map/convention; removido no reset interno de testes | Estado global do Dapper | Limite central: `Query<T>()` do Dapper escolhe type map por tipo, nao por configuracao FluentMap. |
| Dommel resolvers | `DommelMapper.Set*` em `ForDommel` | Process-wide integration state | Instalado por chamada a `ForDommel` | Estado global do Dommel | Resolvers atuais leem `FluentMapper.EntityMaps` e `TypeConventions` diretamente. |
| Dommel SQL builders | `DommelMapper.AddSqlBuilder` em `DommelPersistenceSqlBuilder.RegisterDefaults` | Process-wide integration state | Registrado por chamada a `ForDommel` | Estado global do Dommel | Substitui builders por chave de provider no processo. |
| Dommel default resolvers | `static readonly DefaultResolver` nos resolvers | Runtime immutable state | Nenhuma apos criacao | Seguro se os resolvers Dommel forem thread-safe | Estado estatico imutavel de fallback. |
| Dapper TypeHandler adapter | `DapperTypeHandlerAdapter` | Process-wide integration state | Consulta `SqlMapper.HasTypeHandler` e `TypeHandlerCache<T>` | Depende de Dapper | Nao cria registry proprio, mas depende de estado global de TypeHandlers do Dapper. |
| Generated source converter fields | codigo emitido pelo generator | Runtime immutable state | Campos `static readonly` por materializer gerado | Thread-safe se converter for stateless | Pertencem ao assembly consumidor e sao registrados no registry global via `AddGeneratedMappings()`. |
| Static metadata defaults | `PropertyPersistenceMetadata.Default/Ignored`, `PropertyConversionMetadata.Default`, `NamingPolicy.Identity/SnakeCase` | Runtime immutable state | Nenhuma | Imutavel | Nao sao problema de isolamento. |
| Legacy `MultiTypeMap.TypePropertyMapCache` | `MultiTypeMap` | Cache derived from configuration, residual | Nao ha usos encontrados | `ConcurrentDictionary` | Cache legado aparentemente morto; deve ser revisado em hardening futuro. |

## Mutações depois de Initialize

O modelo atual nao possui transicao formal para configuracao imutavel. Depois de `Initialize` ainda e possivel:

- chamar `Initialize` novamente e adicionar mapas, profiles, conventions e materializers gerados;
- mutar `FluentMapper.EntityMaps` diretamente com `TryAdd`, indexer, `Clear` ou operacoes de `ConcurrentDictionary`;
- mutar `FluentMapper.TypeConventions` diretamente e mutar as listas internas;
- mutar `IEntityMap.PropertyMaps`, porque o contrato publico expoe `IList<IPropertyMap>`;
- mutar `Convention.PropertyMaps` e `Convention.ConventionConfigurations`, ambos `IList`;
- mutar instancias de `PropertyMapBase` durante a construcao do map via fluent API; se uma instancia vazar, nao ha congelamento;
- chamar `FluentMapper.Reset(...)` internamente nos testes, limpando registry e type maps do Dapper para os tipos informados;
- chamar `ForDommel()` novamente, reinstalando resolvers/builders globais.

## Dependencia entre caches e configuracao

- `_propertyMapCache` depende de maps explicitos, profiles, conventions, naming policies, `Ignored`, case sensitivity, inheritance e member path.
- `_materializationPlanCache` depende de maps, profiles, conventions, ordered column shape, persistence read semantics, converters, constructors, setters e Dapper TypeHandlers.
- `_generatedMaterializers` depende de descriptors registrados e do mapping efetivo no momento da consulta; a validacao faz fallback se o descriptor divergir.
- `SqlMapper.SetTypeMap` instala objetos que consultam `FluentMapper.Registry` em tempo de resolucao. Isso evita copiar toda configuracao no type map, mas prende `Query<T>()` ao registry global.
- Dommel resolvers consultam os dicionarios publicos globais em tempo de resolucao, inclusive para persistence metadata.

## Acessos diretos ao estado global

### Core

- `FluentMapConfiguration` escreve em `FluentMapper.Registry`.
- `FluentConventionConfiguration` escreve em `FluentMapper.Registry`.
- `MappedRowMaterializer` consulta `FluentMapper.Registry` para generated materializer e runtime plan.
- `FluentMapTypeMap<TEntity>`, `FluentMapTypeMap` interno e `FluentConventionTypeMap<TEntity>` resolvem propriedades via `FluentMapper.Registry`.
- `MappingRegistry` instala/remova type maps em `SqlMapper.SetTypeMap`.

### Dommel

- `DommelColumnNameResolver` consulta `FluentMapper.EntityMaps` e `FluentMapper.TypeConventions`.
- `DommelKeyPropertyResolver` consulta `FluentMapper.EntityMaps`.
- `DommelPropertyResolver` consulta `FluentMapper.EntityMaps`.
- `DommelTableNameResolver` consulta `FluentMapper.EntityMaps`.
- `DommelPersistenceMetadata` consulta `FluentMapper.EntityMaps`.
- `ForDommel()` instala resolvers/builders no `DommelMapper` global.

### Testes

- Os testes usam `FluentMapper.Reset(...)`, `EntityMaps.Clear()` e `TypeConventions.Clear()` para isolamento.
- Projetos de teste que tocam estado global desabilitam paralelismo com `CollectionBehavior(DisableTestParallelization = true)`.
- Varios testes inspecionam contadores internos de cache via `FluentMapper.Registry`.

## APIs publicas que expoem colecoes mutaveis

- `FluentMapper.EntityMaps`: `ConcurrentDictionary<Type, IEntityMap>`.
- `FluentMapper.TypeConventions`: `ConcurrentDictionary<Type, IList<Convention>>`.
- `IEntityMap.PropertyMaps`: `IList<IPropertyMap>`.
- `EntityMapBase<TEntity, TPropertyMap>.PropertyMaps`: `IList<IPropertyMap>`.
- `Convention.ConventionConfigurations`: `IList<PropertyConventionConfiguration>`.
- `Convention.PropertyMaps`: `IList<PropertyMap>`.

## Riscos atuais

- `ConcurrentDictionary` protege a estrutura do dicionario, mas nao congela os objetos armazenados.
- `ContainsKey` seguido de `TryAdd` evita duplicidade funcional, mas nao e uma transicao atomica de configuracao completa.
- A invalidacao por tipo nao cobre mutacao direta dos objetos de map/convention depois que caches foram preenchidos.
- Direct mutation em `EntityMaps` bypassa `SetDapperTypeMap`, comprovado por teste de compatibilidade.
- Dapper e Dommel mantem integracoes process-wide por tipo/provider; configuracoes multiplas nao conseguem dirigir `Query<T>()` ou Dommel sem novos entry points ou bridges explicitas.
- Generated descriptors podem ficar registrados globalmente para uma configuracao que depois foi alterada; ha validacao de match, mas nao isolamento por instancia de configuracao.
