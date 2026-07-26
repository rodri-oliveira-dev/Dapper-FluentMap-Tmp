# 03 - Heranca De Mappings

## Specification

Adicionar suporte explicito para reutilizar mappings configurados em uma classe base quando um mapping de tipo derivado optar por essa composicao.

Problema historico:

- um `EntityMap<User>` configurado para `User.Id -> user_id` nao era aplicado a `AdminUser : User`;
- consumidores precisavam copiar mappings herdados para cada tipo derivado;
- inferir heranca automaticamente poderia alterar comportamento existente de forma silenciosa.

Requisitos tratados:

- inclusao deliberada de base mapping;
- ordem de composicao;
- precedencia entre derivado, base, convention e Dapper default;
- override de membro herdado;
- conflito de coluna entre base e derivado;
- interacao com conventions;
- preservacao de `MemberPath` herdado;
- hierarquia invalida;
- multiplos niveis de heranca;
- ordem de registro diagnostica.

## Discovery

Arquivos analisados:

- `AGENTS.md`
- `docs/sdd/etapa-1/README.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-1/04-mapping-registry-cache.md`
- `docs/sdd/etapa-2/README.md`
- `docs/sdd/etapa-2/status.md`
- `docs/sdd/etapa-2/decisions.md`
- `docs/sdd/etapa-2/01-member-path.md`
- `docs/sdd/etapa-2/02-configuration-validation.md`
- `src/Dapper.FluentMap/Mapping/EntityMap.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMap.cs`
- `src/Dapper.FluentMap/Mapping/MemberPath.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMapIdentity.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/MappingConfigurationValidator.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentTypeMap.cs`
- `src/Dapper.FluentMap/TypeMaps/FluentConventionTypeMap.cs`
- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `src/Dapper.FluentMap/Configuration/FluentConventionConfiguration.cs`
- testes do core relacionados a composicao, validacao, registry e Dapper.

Achados:

- `MappingRegistry.ResolveFluentPropertyInfo` ja centralizava a precedencia explicito -> convention -> Dapper default.
- `GetExplicitPropertyMaps(type)` retornava apenas o `EntityMap` registrado para o tipo exato.
- `MemberPath` ja permitia comparar propriedades herdadas por identidade de membro, nao apenas nome terminal.
- `MappingConfigurationValidator` ja validava compatibilidade de paths cujo primeiro membro vem de classe base.
- conventions para um tipo derivado ja enxergavam propriedades herdadas via `type.GetProperties(...)`.
- nao existia metadado no `EntityMap` para declarar que um map derivado depende de um map base.

Reproducao inicial:

- foi adicionado um teste expressando `IncludeBase<BaseUser>()`;
- antes da implementacao, a suite falhava na compilacao com `CS0103`, pois a API nao existia;
- isso confirmou que o suporte precisava de contrato publico/protegido novo, nao apenas ajuste de registry.

## Decision

API escolhida:

```csharp
protected void IncludeBase<TBase>()
    where TBase : class
```

Uso:

```csharp
public class UserMap : EntityMap<User>
{
    public UserMap()
    {
        Map(e => e.Id).ToColumn("user_id");
    }
}

public class AdminUserMap : EntityMap<AdminUser>
{
    public AdminUserMap()
    {
        IncludeBase<User>();
        Map(e => e.Permission).ToColumn("admin_permission");
    }
}
```

Motivos:

- inclusao deliberada, sem heranca magica por reflection;
- baixa complexidade;
- preserva API publica existente e adiciona apenas uma API protegida para autores de maps;
- evita profiles, modos de heranca ou scanning amplo;
- permite diagnostico claro quando a base nao foi registrada.

Resolucao do mapping base:

- `IncludeBase<TBase>()` armazena internamente o tipo base no `EntityMap` derivado;
- o `MappingRegistry` resolve o `IEntityMap` base ja registrado para esse tipo;
- o base map deve ser registrado antes do derived map;
- se o base map nao existir, `AddMap(derived)` falha com `FluentMapConfigurationException`.

Modelo de composicao:

```text
maps proprios do derivado
maps explicitos da base incluida, ja compostos recursivamente
```

Para multiplos niveis:

```text
Derived
  IncludeBase<Intermediate>()

Intermediate
  IncludeBase<Base>()
```

O resultado efetivo para `Derived` e:

```text
Derived explicit maps
Intermediate explicit maps
Base explicit maps
```

Precedencia final:

```text
Mapping explicito do derivado
        ↓
Mapping explicito herdado mais proximo
        ↓
Mapping explicito herdado mais distante
        ↓
Convention do tipo consultado
        ↓
Dapper Default
```

Overrides:

- se derivado e base configurarem o mesmo `MemberPath`, o mapping do derivado vence;
- o mapping base sobrescrito nao participa da resolucao de coluna para o tipo derivado;
- a comparacao de override usa `MemberPath`, preservando membros herdados e caminhos aninhados.

Conflitos:

- se derivado e base configurarem a mesma coluna para `MemberPath` diferentes, a configuracao do derivado falha cedo;
- conflito respeita case sensitivity pelas regras da Entrega 02;
- conflito real entre maps do core continua sendo `FluentMapConfigurationException`.

Conventions:

- conventions continuam registradas por tipo;
- mappings explicitos compostos, incluindo herdados, bloqueiam convention para o mesmo `MemberPath`;
- convention ainda pode resolver propriedades distintas do derivado.

Validacoes:

- `TBase` deve ser uma classe base real de `TEntity`;
- incluir o mesmo base type mais de uma vez e invalido;
- base map ausente e invalido no registro do map derivado;
- derived antes de base e invalido, mas pode ser tentado novamente depois que a base for registrada;
- coluna duplicada entre derivado e base e invalida;
- `MemberPath` incompativel continua invalido pela validacao existente.

## Delivery

Arquivos alterados:

- `src/Dapper.FluentMap/Mapping/EntityMap.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/MappingConfigurationValidator.cs`
- `test/Dapper.FluentMap.Tests/MappingCompositionTests.cs`
- `test/Dapper.FluentMap.Tests/InheritedMappingTests.cs`
- `docs/sdd/etapa-2/status.md`
- `docs/sdd/etapa-2/decisions.md`
- `docs/sdd/etapa-2/03-inherited-mappings.md`

Implementacao:

- adicionado metadado interno `IEntityMapWithIncludedBaseTypes`;
- `EntityMapBase<TEntity, TPropertyMap>` passou a registrar bases incluidas;
- `IncludeBase<TBase>()` valida relacao de heranca e duplicidade;
- `MappingRegistry` passou a compor explicit maps do tipo consultado com mapas base incluidos;
- composicao recursiva ignora paths ja definidos pelo tipo mais derivado, implementando override;
- validacao composta detecta conflitos de coluna depois da aplicacao dos overrides.

Nao implementado:

- heranca automatica sem `IncludeBase<TBase>()`;
- multiplos modos de heranca;
- profiles;
- compartilhamento entre tipos nao relacionados;
- suporte novo a materializacao aninhada;
- alteracao funcional no Dommel.

## Tests

Testes adicionados cobrem:

- base mapping simples;
- derived adicionando propriedade propria;
- derived sobrescrevendo mapping base;
- mapping base com convention no derived;
- `MemberPath` herdado e aninhado;
- multiplos niveis de heranca;
- base map inexistente;
- tipo informado que nao e base valido;
- conflito de coluna entre derived e base;
- ordem de registro base antes de derived;
- materializacao real com Dapper e SQLite in-memory.

## Validation

Comandos executados durante a entrega:

- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~IncludedBaseMappingShouldResolveColumnForDerivedEntity"`
  - antes da implementacao: falha de compilacao `CS0103` porque `IncludeBase` nao existia;
  - depois da implementacao: sucesso, 1 teste aprovado.
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~InheritedMappingTests"`
  - resultado: sucesso, 11 testes aprovados.
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj`
  - resultado: sucesso, 77 testes aprovados.

- `dotnet restore`
  - resultado: sucesso.
- `dotnet build`
  - resultado: sucesso, 0 warnings, 0 erros.
- `dotnet test`
  - resultado: sucesso, 77 testes aprovados no core e 7 testes aprovados no Dommel.
- `dotnet build --configuration Release`
  - resultado: sucesso, 0 warnings, 0 erros.
- `dotnet test --configuration Release`
  - resultado: sucesso, 77 testes aprovados no core e 7 testes aprovados no Dommel.

Pack nao e esperado porque nao houve mudanca de empacotamento, metadados NuGet ou targets.

## Limitacoes

- a base deve ser registrada antes do derivado;
- mutacoes diretas nos dicionarios publicos legados continuam fora do modelo de invalidacao segura;
- o suporte inclui apenas explicit mappings de base, nao conventions registradas para o tipo base;
- `IncludeBase<TBase>()` aceita apenas classe base real, nao interface;
- materializacao aninhada permanece fora do escopo.
