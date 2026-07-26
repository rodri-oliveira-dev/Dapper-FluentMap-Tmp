# 01 - MemberPath

## Specification

Introduzir uma representacao interna robusta de caminho de membro para diferenciar propriedades que compartilham o mesmo nome terminal, como:

```csharp
x => x.Rank.Level
x => x.Seniority.Level
```

Requisitos:

- representar todos os membros do caminho;
- preservar ordem;
- fornecer igualdade e hashing consistentes;
- suportar caminhos simples e aninhados;
- aceitar `Convert` produzido por `Expression<Func<TEntity, object>>`;
- preservar a API publica baseada em `PropertyInfo` terminal;
- nao implementar materializacao de objetos aninhados.

## Discovery

Arquivos analisados:

- `docs/sdd/etapa-1/README.md`
- `docs/sdd/etapa-1/status.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-1/04-mapping-registry-cache.md`
- `src/Dapper.FluentMap/Mapping/EntityMap.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMap.cs`
- `src/Dapper.FluentMap/Utils/ReflectionHelper.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/Configuration/FluentConventionConfiguration.cs`
- `test/Dapper.FluentMap.Tests/ManualMappingTests.cs`
- `test/Dapper.FluentMap.Tests/ReflectionHelperTests.cs`
- `test/Dapper.FluentMap.Tests/MappingCompositionTests.cs`
- `test/Dapper.FluentMap.Tests/MappingRegistryTests.cs`
- `test/Dapper.FluentMap.Tests/DapperIntegrationTests.cs`

Achados:

- `ReflectionHelper.GetMemberInfo` ja usa o `MemberExpression.Member` real da expression tree e aceita `Convert`, conforme decisao da Etapa 1.
- `ReflectionHelper.GetMemberInfo` retorna somente o membro terminal, perdendo a cadeia de acesso.
- `EntityMapBase.ThrowIfDuplicateMapping` detecta duplicidade por `p.PropertyInfo.Name == map.PropertyInfo.Name`.
- `MappingRegistry.IsExplicitlyMapped` tambem usa somente `PropertyInfo.Name` para impedir que conventions resolvam propriedades explicitamente mapeadas.
- `PropertyMap` preserva apenas `PropertyInfo`, `ColumnName`, `CaseSensitive` e `Ignored`; nao existe identidade interna de caminho.
- O cache atual (`MappingCacheKey`) usa tipo, coluna e estrategia; ele nao colide por caminho de propriedade, mas armazena apenas o `PropertyInfo` terminal resolvido.
- Conventions escaneiam propriedades publicas de instancia do tipo raiz e produzem mapas simples.

Comportamento atual confirmado por leitura do fluxo e por teste de regressao executado antes da implementacao:

- `Map(x => x.Rank.Level)` cria um `PropertyMap` cujo `PropertyInfo.Name` e `Level`.
- `Map(x => x.Seniority.Level)` cria outro `PropertyMap` cujo `PropertyInfo.Name` tambem e `Level`.
- A segunda chamada falha durante configuracao em `EntityMapBase.ThrowIfDuplicateMapping`, antes de cache ou materializacao.
- Comando de confirmacao:
  - `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~PropertyMapShouldDistinguishNestedPropertiesWithSameTerminalName"`
  - resultado antes da correcao: falha com `Duplicate mapping detected. Property 'Level' is already mapped to column 'Level'.`

## Decision

Representacao escolhida:

```text
internal sealed class MemberPath
```

Semantica:

- armazena uma sequencia ordenada de `PropertyInfo`;
- o membro terminal continua disponivel como `PropertyInfo`;
- caminho simples: uma propriedade, por exemplo `Name`;
- caminho aninhado: duas ou mais propriedades, por exemplo `Address.City`;
- `ToString()` retorna a string de diagnostico formada por nomes unidos por `.`;
- igualdade compara a sequencia completa de propriedades por identidade de membro;
- hashing combina todos os membros na mesma ordem;
- `Convert` e removido durante o parsing da expression;
- expressoes que nao resolvem para cadeia de propriedades continuam falhando com `ArgumentException`;
- indexers e chamadas de metodo permanecem invalidos.

Compatibilidade:

- `IPropertyMap.PropertyInfo` nao sera removido nem alterado;
- `PropertyMap.PropertyInfo` continua sendo o terminal;
- `MemberPath` sera interno e associado aos mapas produzidos pelo core;
- implementacoes externas de `IPropertyMap` que nao conhecem `MemberPath` recebem fallback para caminho simples baseado em `PropertyInfo`.

Limite arquitetural:

- esta entrega nao cria materializador aninhado;
- retornar o `PropertyInfo` terminal para o Dapper continua sendo o limite do `ITypeMap` atual.

## Delivery

Arquivos alterados:

- `src/Dapper.FluentMap/Mapping/MemberPath.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMapIdentity.cs`
- `src/Dapper.FluentMap/Utils/ReflectionHelper.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMap.cs`
- `src/Dapper.FluentMap/Mapping/EntityMap.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `test/Dapper.FluentMap.Tests/MemberPathTests.cs`
- `test/Dapper.FluentMap.Tests/ManualMappingTests.cs`
- `test/Dapper.FluentMap.Tests/MappingCompositionTests.cs`

Modelo anterior:

- `ReflectionHelper` resolvia o membro correto, mas devolvia apenas o `PropertyInfo` terminal.
- `PropertyMap` armazenava apenas o terminal.
- Duplicidade e override de convention eram decididos por `PropertyInfo.Name`.

Modelo novo:

- `MemberPath` e uma representacao interna imutavel baseada em uma sequencia ordenada de `PropertyInfo`.
- `ReflectionHelper.GetMemberPath` percorre a cadeia da expression, remove `Convert`/`ConvertChecked` e valida que cada elo e propriedade.
- `ReflectionHelper.GetMemberInfo` continua publico e passa a devolver o terminal do `MemberPath`, preservando contrato.
- `PropertyMapBase` guarda `MemberPath` internamente, mantendo `PropertyInfo` publico como terminal.
- `PropertyMapIdentity` centraliza leitura/escrita da identidade interna e fornece fallback para caminho simples quando uma implementacao externa de `IPropertyMap` nao carrega `MemberPath`.
- `EntityMapBase.ThrowIfDuplicateMapping` compara caminhos completos.
- `MappingRegistry.IsExplicitlyMapped` compara caminhos completos para evitar que `Rank.Level` bloqueie uma convention para `Level` no tipo raiz.

Igualdade:

- dois `MemberPath` sao iguais quando possuem a mesma quantidade de propriedades e cada posicao representa o mesmo membro.
- a comparacao usa `Module`, `MetadataToken` e `DeclaringType` quando disponiveis, com fallback para `PropertyInfo.Equals`.
- a igualdade considera ordem, entao `Rank.Level` e diferente de `Seniority.Level`.

Hashing:

- o hash combina todos os membros do caminho em ordem.
- cada membro usa a mesma identidade por metadados usada na igualdade quando disponivel, com fallback para `PropertyInfo.GetHashCode`.

Impacto nos caches:

- `MappingCacheKey` nao mudou: continua usando tipo, nome de coluna ordinal e estrategia.
- o cache ainda retorna `PropertyInfo` terminal porque este e o contrato exigido pelo `CustomPropertyTypeMap` do Dapper.
- a correcao de identidade ocorre antes da entrada no cache, na configuracao e na composicao explicito/convention.

Testes adicionados:

- caminho simples (`Name`);
- caminho aninhado (`Address.City`);
- caminhos distintos com terminal igual (`Rank.Level` e `Seniority.Level`);
- igualdade/hash para o mesmo caminho;
- `Convert` em value type;
- expression invalida;
- dois nested mappings com terminal `Level` devem coexistir;
- duplicidade real do mesmo nested path deve continuar falhando;
- explicit mapping aninhado nao deve bloquear convention de propriedade raiz com mesmo terminal.

Nao suportado nesta entrega:

- materializacao de objetos aninhados;
- criacao automatica de objetos intermediarios;
- Value Objects ponta a ponta;
- custom materializer;
- source generator;
- query wrapper;
- naming policy baseada em caminho completo.

## Validation

Ambiente:

- SDK: `10.0.302`
- test runner: VSTest com xUnit v3
- projeto principal: `netstandard2.0`
- projetos de teste: `net10.0`
- `NUGET_PACKAGES=%TEMP%\dfm-nuget-packages-memberpath` usado para isolar o cache NuGet.

Comandos executados:

- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~PropertyMapShouldDistinguishNestedPropertiesWithSameTerminalName"`
  - resultado antes da correcao: falhou reproduzindo a duplicidade por `Level`.
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~MemberPathTests|FullyQualifiedName~PropertyMapShouldDistinguishNestedPropertiesWithSameTerminalName|FullyQualifiedName~DuplicateNestedPropertyPathShouldThrow|FullyQualifiedName~ExplicitNestedMappingShouldNotOverrideConvention"`
  - resultado: sucesso, 9 testes aprovados.
- `dotnet restore .\Dapper.FluentMap.sln`
  - resultado: sucesso.
- `dotnet build .\Dapper.FluentMap.sln --no-restore`
  - resultado: sucesso, 0 warnings, 0 erros.
- `dotnet test .\Dapper.FluentMap.sln --no-build`
  - resultado: sucesso, 54 testes aprovados no core e 7 testes aprovados no Dommel.
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --no-build`
  - resultado: sucesso, 54 testes aprovados.
- `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`
  - resultado: sucesso, 0 warnings, 0 erros.
- `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`
  - resultado: sucesso, 54 testes aprovados no core e 7 testes aprovados no Dommel.
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --no-build`
  - resultado: sucesso, 54 testes aprovados.

Confirmacoes:

- testes da Etapa 1 continuam passando;
- `MemberPath` diferencia `Rank.Level` de `Seniority.Level`;
- duplicidade real do mesmo caminho continua sendo detectada;
- nao houve implementacao de nested materialization;
- Dommel nao recebeu alteracao funcional;
- API publica existente foi preservada.

Pack nao foi executado porque nao houve mudanca de empacotamento, metadados NuGet ou targets.
