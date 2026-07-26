## Specification

Corrigir a composicao entre mappings explicitos, conventions e fallback padrao do Dapper para que a resolucao de membros siga uma cadeia previsivel:

1. mapping explicito;
2. convention;
3. `DefaultTypeMap` do Dapper.

Objetivos:

- permitir coexistencia de `AddMap(...)` e `AddConvention(...).ForEntity(...)` para o mesmo tipo;
- permitir que mapping explicito sobrescreva convention para a mesma propriedade;
- preservar o fallback do Dapper quando nem mapping explicito nem convention resolvem a coluna;
- eliminar o comportamento em que a ultima chamada a `SqlMapper.SetTypeMap(...)` determina sozinha a estrategia ativa;
- preservar a API publica e evitar o `MappingRegistry` completo previsto para entrega posterior.

Fora do escopo:

- MappingRegistry definitivo;
- redesign amplo de cache;
- MemberPath;
- materializacao aninhada;
- Value Objects;
- profiles por tipo;
- constructor mapping;
- alteracoes funcionais no Dommel.

## Discovery

Arquivos analisados:

- `src/Dapper.FluentMap/FluentMapper.cs`
- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `src/Dapper.FluentMap/Configuration/FluentConventionConfiguration.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentConventionTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/MultiTypeMap.cs`
- `src/Dapper.FluentMap/Utils/FluentMapConfigurationExtensions.cs`
- `src/Dapper.FluentMap/Mapping/EntityMap.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMap.cs`
- `test/Dapper.FluentMap.Tests/ManualMappingTests.cs`
- `test/Dapper.FluentMap.Tests/ConventionTests.cs`
- `docs/sdd/etapa-1/01-reflection-helper.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-1/status.md`

`status.md` confirmou que `01 - ReflectionHelper` esta `Concluido`.

Pontos que chamavam `SqlMapper.SetTypeMap(...)`:

- `FluentMapper.AddTypeMap<TEntity>()`
  - instalava `new FluentMapTypeMap<TEntity>()`;
  - chamado por `FluentMapConfiguration.AddMap<TEntity>(...)`.
- `FluentMapper.AddTypeMap(Type entityType)`
  - instalava `FluentMapTypeMap<>` via reflection;
  - chamado por assembly scanning de maps via `ApplyMapsFromAssemblies(...)`.
- `FluentMapper.AddConventionTypeMap<TEntity>()`
  - instalava `new FluentConventionTypeMap<TEntity>()`;
  - chamado por `FluentConventionConfiguration.ForEntity<T>()`.
- `FluentMapper.AddConventionTypeMap(Type entityType)`
  - instalava `FluentConventionTypeMap<>` via reflection;
  - chamado por `ForEntitiesInCurrentAssembly(...)` e `ForEntitiesInAssembly(...)`.

Fluxo atual antes da mudanca:

- `AddMap(...)` registra o `IEntityMap` em `FluentMapper.EntityMaps` e instala `FluentMapTypeMap<TEntity>`.
- `ApplyMapsFromAssemblies(...)` encontra classes que implementam `IEntityMap<>` e chama `AddMap(...)` por reflection.
- `AddConvention<TConvention>()` cria um `FluentConventionConfiguration`.
- `ForEntity<T>()` materializa `PropertyMap`s da convention, registra a convention em `FluentMapper.TypeConventions` e instala `FluentConventionTypeMap<TEntity>`.
- `ForEntitiesInCurrentAssembly(...)` e `ForEntitiesInAssembly(...)` repetem o mesmo processo para cada tipo exportado filtrado.
- `FluentMapTypeMap<TEntity>` consultava mappings explicitos e depois caia para `DefaultTypeMap`.
- `FluentConventionTypeMap<TEntity>` consultava conventions e depois caia para `DefaultTypeMap`.

Causa raiz:

- mappings explicitos e conventions eram estrategias separadas instaladas diretamente no registro global do Dapper;
- para o mesmo tipo de entidade, a chamada mais recente a `SqlMapper.SetTypeMap(...)` substituia a anterior;
- portanto `AddMap(...); AddConvention(...).ForEntity<T>()` deixava apenas convention + default ativa;
- e `AddConvention(...).ForEntity<T>(); AddMap(...)` deixava apenas explicito + default ativo;
- cada type map ja tinha fallback proprio para `DefaultTypeMap`, mas nao havia um type map unico que compusesse explicito e convention antes do fallback.

Observacoes sobre cache:

- `MultiTypeMap.TypePropertyMapCache` e compartilhado entre type maps;
- a chave antiga usava apenas `type.FullName` e `columnName`;
- em uma composicao ingênua com dois `CustomPropertyTypeMap`s, um miss do resolver explicito poderia ser cacheado e impedir a convention de ser consultada para a mesma coluna;
- a Entrega 4 continua sendo o local apropriado para redesenhar registry/cache de forma completa.

## Decision

Design escolhido:

- usar `FluentMapTypeMap<TEntity>` como estrategia composta instalada tanto por mappings explicitos quanto por conventions;
- alterar `FluentMapTypeMap<TEntity>` para resolver em uma unica funcao:
  - primeiro mappings explicitos em `FluentMapper.EntityMaps`;
  - depois conventions em `FluentMapper.TypeConventions`;
  - por fim o `DefaultTypeMap` ja presente no `MultiTypeMap`;
- manter `FluentConventionTypeMap<TEntity>` publico e funcional para compatibilidade, mas deixar de instala-lo nos fluxos internos de `AddConventionTypeMap(...)`;
- mover a comparacao de coluna para `MultiTypeMap.MatchColumnNames(...)`, evitando duplicar a regra case-sensitive/case-insensitive entre os type maps.

Precedencia final:

1. se a coluna casa com um mapping explicito, ele vence;
2. se o mapping explicito e `Ignore()`, a resolucao para aquela coluna para sem cair no default;
3. se a coluna nao casa com mapping explicito, conventions podem resolver;
4. conventions nao resolvem propriedades que tenham mapping explicito, permitindo override explicito da convention para a mesma propriedade;
5. se nenhuma regra especial resolver, `DefaultTypeMap` permanece disponivel.

Comportamento em conflito:

- mapping explicito para a mesma coluna vence por ser consultado antes;
- mapping explicito para a mesma propriedade remove essa propriedade dos candidatos por convention;
- ambiguidades dentro de uma convention continuam usando a excecao existente quando mais de um `PropertyMap` casa com a mesma coluna.

Compatibilidade:

- nenhuma API publica foi removida ou alterada;
- `FluentConventionTypeMap<TEntity>` permanece publico;
- `AddConventionTypeMap(...)` passa a instalar `FluentMapTypeMap<TEntity>` para obter a composicao;
- consumidores que observam diretamente `SqlMapper.GetTypeMap(typeof(T))` apos configurar apenas convention podem notar o tipo concreto diferente, mas o comportamento funcional esperado de convention + default permanece.

Alternativas descartadas:

- apenas trocar a ordem de chamadas de `SetTypeMap`: manteria o comportamento dependente de ordem e nao comporia as estrategias;
- criar agora um `MappingRegistry`: resolveria parte do problema, mas antecipa a Entrega 4 e ampliaria o escopo;
- empilhar dois `CustomPropertyTypeMap`s independentes: conflitaria com o cache compartilhado quando o primeiro resolver cacheasse misses antes do segundo ser consultado;
- remover `FluentConventionTypeMap<TEntity>`: seria uma quebra desnecessaria de superficie publica.

## Delivery

Implementacao:

- `FluentMapper.AddConventionTypeMap(...)` agora delega para `AddTypeMap(...)`, instalando o type map composto.
- `FluentMapTypeMap<TEntity>` agora consulta mappings explicitos e conventions antes do fallback default.
- `FluentMapTypeMap<TEntity>` ignora candidates de convention para propriedades ja mapeadas explicitamente.
- `MultiTypeMap` recebeu `MatchColumnNames(...)` protegido para compartilhar a regra de comparacao.
- `FluentConventionTypeMap<TEntity>` passou a usar uma chave de cache distinta da chave do type map composto.

Testes adicionados em `test/Dapper.FluentMap.Tests/MappingCompositionTests.cs`:

- somente mapping explicito resolve coluna explicita;
- somente convention resolve coluna por prefixo;
- `DefaultTypeMap` resolve coluna quando nenhuma regra especial casa;
- mapping explicito e convention resolvem propriedades diferentes no mesmo tipo;
- mapping explicito sobrescreve convention para a mesma propriedade;
- ordem `AddMap(...)` antes de `AddConvention(...)` nao impede composicao;
- ordem `AddConvention(...)` antes de `AddMap(...)` nao impede composicao;
- case sensitivity de mapping explicito e convention case-insensitive permanecem independentes.

Implicacoes para MappingRegistry:

- a entrega cria uma cadeia de resolucao observavel, mas ainda consulta os dicionarios globais existentes;
- a Entrega 4 deve substituir essa consulta direta por descritores/registry mais explicitos e lidar com invalidacao de cache;
- a chave de cache continua deliberadamente simples e nao resolve colisoes por assembly, reinicializacao tardia ou todas as dimensoes de configuracao.

## Validation

Ambiente:

- SDK: `10.0.302`
- test runner detectado: VSTest com xUnit v2 (`Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`)
- projetos de teste: `netcoreapp3.1`

Comandos executados:

- `dotnet test test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter "FullyQualifiedName~MappingCompositionTests"`
  - resultado: falhou antes de executar por metadado corrompido no cache NuGet global (`microsoft.netcore.targets`).
- Com `NUGET_PACKAGES` temporario no workspace:
  - `dotnet test test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter "FullyQualifiedName~MappingCompositionTests"`
  - resultado: restore e build passaram; execucao abortou porque `Microsoft.NETCore.App 3.1.0` nao esta instalado.
- Harness temporario `net8.0` referenciando o projeto atual:
  - resultado: passou todos os cenarios de composicao, override, ordem, fallback e case sensitivity.
- Com `NUGET_PACKAGES` temporario:
  - `dotnet build src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release`
  - resultado: sucesso, 0 warnings, 0 erros.
- Com `NUGET_PACKAGES` temporario:
  - `dotnet build Dapper.FluentMap.sln --configuration Release`
  - resultado: sucesso, 0 warnings, 0 erros.
- `dotnet restore`
  - resultado: falhou por metadado corrompido no cache NuGet global (`microsoft.netcore.targets`).
- Com `NUGET_PACKAGES=%TEMP%\dfm-nuget-packages-composition`:
  - `dotnet restore`
  - resultado: sucesso.
- Com `NUGET_PACKAGES=%TEMP%\dfm-nuget-packages-composition`:
  - `dotnet build --configuration Release`
  - resultado: sucesso, 0 warnings, 0 erros.
- Com `NUGET_PACKAGES=%TEMP%\dfm-nuget-packages-composition`:
  - `dotnet test --configuration Release --no-build`
  - resultado: abortou porque `Microsoft.NETCore.App 3.1.0` nao esta instalado para os projetos de teste core e Dommel.
- Com `NUGET_PACKAGES=%TEMP%\dfm-nuget-packages-composition`:
  - `dotnet test test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --no-build`
  - resultado: abortou porque `Microsoft.NETCore.App 3.1.0` nao esta instalado.

Limitacoes:

- a suite oficial compilou, mas nao executou ate o fim neste ambiente por ausencia do runtime `netcoreapp3.1`;
- Dommel nao recebeu alteracao funcional; a solution completa compilou em Release;
- pack nao foi executado porque a entrega nao altera metadados ou empacotamento NuGet.
