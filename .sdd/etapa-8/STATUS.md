# Etapa 8 Status

## Objetivo

Definir a arquitetura e a especificacao inicial de semantica de persistencia de
propriedades, separando materializacao/leitura de insert/update, sem implementar
features produtivas significativas.

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

## Em andamento

Nenhum apos o commit local deste prompt.

## Proximos passos

1. Criar modelo de metadata aditivo no core.
2. Definir e implementar APIs publicas pequenas para semantica de escrita.
3. Adaptar FluentMap.Dommel para consumir a metadata sem gerar SQL no core.
4. Criar suite de regressao historica para #94, #122, #123, #130, #114, #126 e
   #133.
5. Atualizar diagnostics/analyzers.
6. Atualizar README e XML docs.
7. Fazer hardening de cache, profiles, generated materializers e Dommel SQL real.

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

## Arquivos importantes

- `.sdd/etapa-8/01-historical-issues.md`
- `.sdd/etapa-8/02-persistence-semantics-spec.md`
- `.sdd/etapa-8/DECISIONS.md`
- `.sdd/etapa-8/STATUS.md`
- `src/Dapper.FluentMap/Mapping/PropertyMap.cs`
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

## Ultimo prompt executado

Ultimo prompt executado: 8.1
