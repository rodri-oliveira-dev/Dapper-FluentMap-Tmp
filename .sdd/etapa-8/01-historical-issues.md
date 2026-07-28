# Etapa 8 - Historical Issue Assessment

Este documento registra a leitura historica das issues do projeto original
`henkmollema/Dapper-FluentMap` e o estado observado no fork atual em
2026-07-28.

## Issue #94 - ReadOnly Fields

Fonte: https://github.com/henkmollema/Dapper-FluentMap/issues/94

### Problema original

O usuario pediu um equivalente a `Ignore()` para campos somente leitura. A
necessidade descrita nos comentarios foi popular uma propriedade por query, mas
nao tentar grava-la em `INSERT` ou `UPDATE`. Exemplos citados: campo computado,
contador e campo `Created` com default do banco.

### Causa provavel/historica

O FluentMap historico tinha `Ignore()` como unico vocabulario para excluir uma
propriedade. Isso misturava duas intencoes diferentes:

- nao materializar uma coluna de leitura;
- nao persistir uma propriedade em comandos de escrita gerados pelo Dommel.

Como `Ignore()` tambem impede leitura, ele nao representava "read-only database
value".

### Estado no fork atual

O core ainda expoe apenas `IPropertyMap.Ignored` para exclusao. Em
`DapperFluentPropertyTypeMap.GetMember`, maps ignorados retornam
`DapperIgnoredMemberMap`; em `NestedMaterializationPlan.Create`, maps ignorados
sao pulados; em generated materializers, `GeneratedMaterializerColumn.Ignore`
representa coluna que deve ser ignorada pela configuracao efetiva.

Dommel possui metadata propria em `DommelPropertyMap`:

- `Key`;
- `Identity`;
- `GeneratedOption`.

Nao ha API de core para "read yes, insert no, update no".

### Ainda reproduzivel?

Sim, como lacuna de modelo: nao existe API publica no core para "ler, mas nao
inserir/atualizar". O comportamento pode ser contornado no pacote Dommel com
`SetGeneratedOption(...)`, mas isso nao e semantica clara de read-only no core e
nao cobre expressivamente "exclude insert" versus "exclude update".

### Cobertura de testes existente

- `ManualMappingTests.PropertyShouldBeIgnored` cobre o flag `Ignored`.
- `GeneratedRegistrationIntegrationTests` cobre ignored no generated path.
- Testes Dommel cobrem key/generated basicos, mas nao o caso read-only como
  semantica independente.

### Relacao com a Etapa 8

E a issue que melhor expressa o objetivo central: separar leitura/materializacao
de persistencia escrita.

### Decisao

Resolver por nova arquitetura.

### Evidencia

- `src/Dapper.FluentMap/Mapping/PropertyMap.cs`
- `src/Dapper.FluentMap/Compatibility/DapperFluentPropertyTypeMap.cs`
- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`
- `src/Dapper.FluentMap/Materialization/GeneratedMaterializerColumn.cs`

## Issue #122 - Insert issue when key column is not identity

Fonte: https://github.com/henkmollema/Dapper-FluentMap/issues/122

PR relacionado: https://github.com/henkmollema/Dapper-FluentMap/pull/129

### Problema original

Depois do upgrade para v2, keys nao auto-geradas eram omitidas do `INSERT`.
Mesmo usando `SetGeneratedOption(DatabaseGeneratedOption.None)`, o SQL gerado
pelo Dommel omitia as colunas key e ainda tentava buscar identity.

### Causa provavel/historica

O modelo Dommel tratava `IsKey()` como implicando `DatabaseGeneratedOption.Identity`
por default. O contrato do Dommel tambem usa `ColumnPropertyInfo.IsGenerated` para
filtrar valores de insert/update. Quando "key" e "identity" se confundem, uma
key de negocio ou composite key deixa de ser inserida.

### Estado no fork atual

O fork contem a alteracao equivalente ao PR #129:

- `DommelPropertyMap.GeneratedOption`;
- `SetGeneratedOption(DatabaseGeneratedOption option)`;
- resolvers Dommel criando `ColumnPropertyInfo` com
  `GeneratedOption ?? (Key ? Identity : None)`.

Os testes `EntityMapsToMultipleKeys` e `PropertiesAreNotGenerated` validam
composite keys com `DatabaseGeneratedOption.None`.

### Ainda reproduzivel?

Parcialmente nao para o caso coberto por `SetGeneratedOption(None)` em composite
keys. A fragilidade arquitetural permanece: por default, `IsKey()` ainda implica
identity/generated, entao key e identity continuam conceitualmente acoplados
quando o usuario nao explicita `None`.

### Cobertura de testes existente

- `test/Dapper.FluentMap.Dommel.Tests/ManualMappingTests.cs`
  - `EntityMapsToMultipleKeys`;
  - `PropertiesAreNotGenerated`;
  - `KeyPropertyIsGenerated`.

### Relacao com a Etapa 8

Exige modelar `Key` e `Identity` como dimensoes independentes. Uma key nao
identity deve ser representavel como `Read=yes`, `Insert=yes`, `Key=yes`,
`Identity=no`.

### Decisao

Resolver por nova arquitetura e manter regression tests.

### Evidencia

- `src/Dapper.FluentMap.Dommel/Mapping/DommelPropertyMap.cs`
- `src/Dapper.FluentMap.Dommel/Resolvers/DommelKeyPropertyResolver.cs`
- `src/Dapper.FluentMap.Dommel/Resolvers/DommelPropertyResolver.cs`
- Dommel 3.5.3 `ColumnPropertyInfo.IsGenerated`:
  https://github.com/henkmollema/Dommel/blob/master/src/Dommel/ColumnPropertyInfo.cs

## Issue #123 - Computed property used in insert/update

Fonte: https://github.com/henkmollema/Dapper-FluentMap/issues/123

### Problema original

`SetGeneratedOption(DatabaseGeneratedOption.Computed)` em uma propriedade
continuava gerando coluna e parametro em `INSERT` e `UPDATE` pelo Dommel.
O usuario esperava que coluna computada fosse omitida dos comandos de escrita,
mas continuasse disponivel para leitura.

### Causa provavel/historica

O resolver de propriedades do FluentMap.Dommel nao entregava a opcao generated
para o `ColumnPropertyInfo` usado pelo Dommel, ou o Dommel efetivo nao consumia
essa metadata como esperado. A consequencia era que "computed" nao virava
"exclude insert/update" no SQL gerado.

### Estado no fork atual

O `DommelPropertyResolver` atual passa `dommelPropertyMap.GeneratedOption` para
`ColumnPropertyInfo`. No Dommel 3.5.3, `BuildInsertQuery` e `BuildUpdateQuery`
filtram `Resolvers.Properties(type).Where(x => !x.IsGenerated)`. Portanto, se
`Computed` chegar ao `ColumnPropertyInfo`, insert/update devem omitir a coluna.

O fork, porem, nao tem teste de integracao de SQL real para computed/default com
Dommel `Insert` e `Update`.

### Ainda reproduzivel?

Provavelmente nao para metadata `Computed` no resolver atual, mas ainda nao ha
regressao direta com SQL gerado/executado no fork. Deve ser tratado como risco
historico ate haver teste especifico.

### Cobertura de testes existente

- `PropertyIsGenerated` valida que alguma propriedade gerada aparece como
  `IsGenerated`.
- Nao ha teste especifico para `DatabaseGeneratedOption.Computed` em `Insert` e
  `Update`.

### Relacao com a Etapa 8

Computed deve ser uma semantica de escrita, nao sinonimo de ignore. A decisao
deve dizer que computed normalmente e `Read=yes`, `Insert=no`, `Update=no`,
`Generated=yes`, `Computed=yes`.

### Decisao

Resolver por nova arquitetura e adicionar regression test historico em prompt
posterior.

### Evidencia

- `src/Dapper.FluentMap.Dommel/Resolvers/DommelPropertyResolver.cs`
- Dommel `Insert.cs` e `Update.cs` filtram `IsGenerated`:
  https://github.com/henkmollema/Dommel/blob/master/src/Dommel/Insert.cs
  https://github.com/henkmollema/Dommel/blob/master/src/Dommel/Update.cs

## Issue #130 - Default value do banco vs Ignore()

Fonte: https://github.com/henkmollema/Dapper-FluentMap/issues/130

### Problema original

Em SQLite, uma coluna `datetime` com default do banco funcionava no insert quando
a propriedade era marcada com `Ignore()`, mas a leitura falhava com
`NotImplementedException` porque a propriedade era ignorada. Ao tentar
`DatabaseGeneratedOption.Identity` ou `Computed`, a propriedade voltava com
`0001-01-01 00:00:00`.

### Causa provavel/historica

O usuario queria "omit on insert, read on select". O unico mecanismo intuitivo
era `Ignore()`, que representa "nao mapear" no core. A alternativa via
`GeneratedOption` dependia do Dommel e nao comunicava claramente se o valor
deveria ser omitido so no insert, tambem no update, e como seria relido.

### Estado no fork atual

O bug especifico de `NotImplementedException` para `Ignore()` foi tratado pelo
PR #131 no projeto original e o fork atual usa `DapperIgnoredMemberMap` sem
`PropertyInfo` incompleto. O problema conceitual permanece: default de banco no
insert nao deve exigir `Ignore()` porque a propriedade ainda participa da
leitura.

### Ainda reproduzivel?

O `NotImplementedException` provavelmente nao. A lacuna "database default on
insert" ainda e reproduzivel como ausencia de semantica explicita no core.

### Cobertura de testes existente

- Generated ignored property tem regressao no fork.
- Nao ha teste Dommel para default on insert com leitura posterior.

### Relacao com a Etapa 8

Motiva uma semantica conceitual do tipo `Read=yes`, `Insert=no`, `Update=yes`
ou `Update=no`, conforme decisao explicita por API futura.

### Decisao

Resolver por nova arquitetura.

### Evidencia

- `src/Dapper.FluentMap/Compatibility/DapperIgnoredMemberMap.cs`
- `test/Dapper.FluentMap.GeneratedRegistration.Tests/GeneratedRegistrationIntegrationTests.cs`
- PR #131: https://github.com/henkmollema/Dapper-FluentMap/pull/131

## Issue #114 - Conflict between property and members of the type

Fonte: https://github.com/henkmollema/Dapper-FluentMap/issues/114

### Problema original

Mapear uma propriedade chamada `Format` causava `InvalidCastException` porque o
helper historico buscava membro por nome no tipo do parametro e pegava primeiro
um metodo do BCL em vez da propriedade correta. Comentario posterior citou
`TimeSpan.Duration`.

### Causa provavel/historica

Expression parsing por reflection procurava membro por nome em vez de usar o
`MemberInfo` real da expressao e validar que era propriedade.

### Estado no fork atual

`ReflectionHelper.GetMemberPath` caminha a expression tree, aceita conversoes de
`Expression<Func<TEntity, object>>`, usa o `MemberExpression.Member` real e
rejeita membros nao propriedade com `ArgumentException`.

### Ainda reproduzivel?

Nao para os cenarios cobertos: existem testes para `Format`, `Duration`, colisao
com membro de `string` e materializacao Dapper real usando outro nome de membro
de `string`, preservando a categoria sem depender de excecao especial para
`Format`.

### Cobertura de testes existente

- `ReflectionHelperTests.GetMemberInfo_ReturnsProperty_WhenPropertyNameMatchesSystemMember`
- `ReflectionHelperTests.GetMemberInfo_ReturnsValueTypeProperty_WhenPropertyNameMatchesSystemMember`
- `ReflectionHelperTests.GetMemberInfo_ReturnsValueTypeProperty_WithSystemTypeNames`
- `DapperIntegrationTests.ExpressionResolvedPropertyShouldMaterializeWhenNameCollidesWithStringMember`
- `DapperIntegrationTests.ExpressionResolvedPropertyShouldMaterializeWhenNameCollidesWithAnotherStringMember`

### Relacao com a Etapa 8

Regression historico a preservar. Qualquer nova metadata de persistencia deve
ser associada ao `PropertyInfo`/`MemberPath` real, nunca ao primeiro membro por
nome.

### Decisao

Ja resolvido; manter como regression boundary.

### Evidencia

- `src/Dapper.FluentMap/Utils/ReflectionHelper.cs`
- `test/Dapper.FluentMap.Tests/ReflectionHelperTests.cs`

## Issue #126 - Nested properties ending with same name

Fonte: https://github.com/henkmollema/Dapper-FluentMap/issues/126

### Problema original

Mapear varios caminhos aninhados terminando em `Level` era tratado como
duplicidade, por exemplo `Rank.Level`, `Seniority.Level`,
`CompletedProfile.Level`.

### Causa provavel/historica

O sistema historico identificava maps pelo `PropertyInfo` terminal ou pelo nome
terminal, nao pelo caminho completo.

### Estado no fork atual

O fork introduziu `MemberPath` e `PropertyMapIdentity`. Duplicidade e validada
por caminho completo. Materializacao runtime, generated materializers e analyzers
preservam o display completo (`Rank.Level`, `Seniority.Level`).

### Ainda reproduzivel?

Nao para os cenarios cobertos no core e no generated path. O resolver de colunas
do Dommel ainda usa `PropertyInfo.Name`, mas Dommel nao suporta nested
materialization e os resolvers Dommel so trabalham com propriedades flat
filtradas por `type.GetProperties()`.

### Cobertura de testes existente

- `ManualMappingTests.PropertyMapShouldDistinguishNestedPropertiesWithSameTerminalName`
- `NestedObjectMaterializationTests.QueryMappedShouldPreserveSameTerminalMemberPaths`
- `MappingRegistrationGeneratorTests.SameTerminalNestedPathsShouldUseFullMemberPathsInDescriptor`
- `GeneratedRegistrationIntegrationTests` para same terminal.
- `GeneratedRegistrationIntegrationTests.GeneratedQueryMappedShouldMatchRuntimeFallbackForEquivalentComplexShapes`
  valida equivalencia runtime/generated para `Rank.Level` e `Seniority.Level`.

### Relacao com a Etapa 8

Nova metadata de persistencia deve ser anexada ao caminho de membro completo
quando houver nested mapping, mesmo que Dommel inicialmente consuma apenas maps
flat.

### Decisao

Ja resolvido no core/generated; apenas regression test se a etapa tocar nessa
area.

### Evidencia

- `src/Dapper.FluentMap/Mapping/MemberPath.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMapIdentity.cs`
- `test/Dapper.FluentMap.Tests/NestedObjectMaterializationTests.cs`
- `test/Dapper.FluentMap.Generators.Tests/MappingRegistrationGeneratorTests.cs`

## Issue #133 - Ignore() causing NotImplementedException

Fonte: https://github.com/henkmollema/Dapper-FluentMap/issues/133

PR relacionado: https://github.com/henkmollema/Dapper-FluentMap/pull/131

### Problema original

Selecionar uma entidade com propriedade marcada como `Ignore()` causava
`NotImplementedException` no Dapper. O stack trace historico do PR #131 mostra
um `IgnoredPropertyInfo.PropertyType` nao implementado sendo acessado pelo
deserializador do Dapper.

### Causa provavel/historica

O ignore era representado por um `PropertyInfo` falso/incompleto. Dapper ainda
tentava consultar `MemberType` e acabava chamando membro nao implementado.

### Estado no fork atual

O core usa `DapperIgnoredMemberMap`, um `SqlMapper.IMemberMap` sentinela com
`MemberType = typeof(object)` e `Property`, `Field`, `Parameter` nulos. O
`MultiTypeMap` atual consegue tratar esse sentinela sem expor um `PropertyInfo`
incompleto.

### Ainda reproduzivel?

Nao para o core atual nos cenarios cobertos. O Prompt 8.3 adicionou regressao
com `Dapper.Query<T>()` selecionando uma coluna ignorada; a propriedade permanece
com valor inicial e nao ha `NotImplementedException`. Tambem ha cobertura para
ignored no generated path e equivalencia runtime/generated.

### Cobertura de testes existente

- `ManualMappingTests.PropertyShouldBeIgnored`
- `GeneratedRegistrationIntegrationTests` valida que `Secret` ignorado permanece
  com valor inicial.
- `DapperIntegrationTests.IgnoredExplicitMappingShouldNotMaterializeSelectedColumn`
  cobre a regressao historica equivalente ao PR #131.
- `GeneratedRegistrationIntegrationTests.GeneratedQueryMappedShouldMatchRuntimeFallbackForEquivalentComplexShapes`
  compara generated e runtime fallback para propriedade ignorada.

### Relacao com a Etapa 8

Mostra por que `Ignore()` nao deve ser reaproveitado para "read-only" ou
"default on insert". `Ignore()` deve continuar significando ausencia completa de
participacao em leitura e escrita.

### Decisao

Ja resolvido para o bug original; manter regression boundary e nao reutilizar
`Ignore()` para semantica de escrita.

### Evidencia

- `src/Dapper.FluentMap/Compatibility/DapperIgnoredMemberMap.cs`
- `src/Dapper.FluentMap/Compatibility/DapperFluentPropertyTypeMap.cs`
- PR #131: https://github.com/henkmollema/Dapper-FluentMap/pull/131
