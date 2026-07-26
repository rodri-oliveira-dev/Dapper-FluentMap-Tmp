# 01 - ReflectionHelper

## Specification

Corrigir a resolucao de propriedades a partir de `Expression<Func<TEntity, object>>` para usar o membro representado pela propria expression tree, preservando API publica e rejeitando expressoes nao suportadas com erro claro.

Fora do escopo: MemberPath completo, objetos aninhados, Value Objects, composicao de conventions, MappingRegistry, redesign de cache, records, constructor mapping, mudancas em Dommel e atualizacoes de frameworks ou dependencias.

## Discovery

Arquivos analisados:

- `src/Dapper.FluentMap/Utils/ReflectionHelper.cs`
- `src/Dapper.FluentMap/Mapping/EntityMap.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMap.cs`
- `test/Dapper.FluentMap.Tests/ReflectionHelperTests.cs`
- `test/Dapper.FluentMap.Tests/ManualMappingTests.cs`
- `test/Dapper.FluentMap.Tests/TestEntity.cs`
- `README.md`

Consumidores de `ReflectionHelper.GetMemberInfo`:

- `EntityMapBase<TEntity, TPropertyMap>.Map(Expression<Func<TEntity, object>> expression)`, que converte o retorno para `PropertyInfo`.
- Testes unitarios em `ReflectionHelperTests`.

Formatos aceitos atualmente:

- `LambdaExpression` cujo corpo seja `MemberExpression`.
- `UnaryExpression` com `ExpressionType.Convert`, usado por propriedades value type em `Expression<Func<TEntity, object>>`.
- Acesso aninhado simples, como `x => x.Email.Address`, retornando a propriedade final.

Comportamentos ja cobertos:

- propriedade comum (`Id`);
- propriedade herdada em entidade derivada;
- nullable/value type com `Convert`;
- propriedade aninhada em value object;
- propriedade aninhada cujo nome coincide com membro de tipo do sistema (`String.Length`).

Lacunas encontradas:

- propriedade final cujo nome coincide com outro membro publico do tipo da propria propriedade, como `string.Format` ou `TimeSpan.Duration`;
- expression invalida sem `MemberExpression`, que atualmente retorna `null` e tende a falhar depois com erro indireto.

Causa raiz: no caminho de `MemberAccess`, o helper obtem `memberExpression.Member`, mas depois procura novamente membros por nome em tipos relacionados (`GetMembers().FirstOrDefault(...)` e `GetMember(member.Name)[0]`). Essa nova busca depende da ordem de reflection e pode retornar `MethodInfo` ou outro membro homonimo em vez do `PropertyInfo` que a expression tree ja identificou.

## Decision

Causa raiz confirmada: a resolucao por nome e por primeiro resultado de reflection e ambigua.

Estrategia escolhida:

- Desembrulhar `Lambda` e `Convert`.
- Em `MemberAccess`, retornar diretamente o `PropertyInfo` presente em `MemberExpression.Member`.
- Rejeitar `MemberExpression` que nao represente propriedade com `ArgumentException`.
- Rejeitar expressoes nao suportadas com `ArgumentException` clara.
- Manter a assinatura publica de `ReflectionHelper.GetMemberInfo(LambdaExpression)`.

Alternativas descartadas:

- Filtrar `GetMember(...)` por `PropertyInfo`: ainda reexecuta uma busca desnecessaria por nome e pode introduzir ambiguidades futuras.
- Criar uma nova abstracao de parsing ou MemberPath: fora do escopo desta entrega.
- Alterar `EntityMap.Map(...)` para nova API publica: desnecessario para corrigir a falha e aumentaria a superficie publica.

Impacto esperado:

- Expressoes validas passam a resolver exatamente a propriedade representada pela expression tree.
- Colisoes de nome com membros de `string`, `TimeSpan` ou outros tipos deixam de produzir `InvalidCastException` indireta.
- Expressoes invalidas passam a falhar mais cedo com erro explicito.

Compatibilidade preservada:

- API publica e assinaturas existentes.
- Suporte a propriedade simples, propriedade herdada, value types com `Convert` e acesso aninhado ja existente.
- Sem mudancas em Dommel, build, targets ou dependencias.

## Delivery

- `ReflectionHelper.GetMemberInfo` passou a:
  - validar `lambda == null` com `ArgumentNullException`;
  - desembrulhar `Lambda` e `Convert`;
  - retornar diretamente o `PropertyInfo` de `MemberExpression.Member`;
  - rejeitar membros que nao sejam propriedades com `ArgumentException`;
  - rejeitar expressoes nao suportadas com `ArgumentException`.
- Testes de regressao adicionados em `ReflectionHelperTests` para:
  - propriedade comum com nome que colide com membro de `string` (`Format`);
  - propriedade value type com `Convert` e nome que colide com membro de `TimeSpan` (`Duration`);
  - expression invalida (`e.Id.ToString()`).

Arquivos alterados:

- `src/Dapper.FluentMap/Utils/ReflectionHelper.cs`
- `test/Dapper.FluentMap.Tests/ReflectionHelperTests.cs`
- `docs/sdd/etapa-1/README.md`
- `docs/sdd/etapa-1/status.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-1/01-reflection-helper.md`

## Validation

- `dotnet test test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Release --filter "FullyQualifiedName~ReflectionHelperTests"`
  - Resultado inicial: falhou antes de executar por metadado corrompido no cache NuGet global (`microsoft.netcore.targets`).
- Reexecutado com `NUGET_PACKAGES` temporario:
  - restore e build dos testes concluiram;
  - execucao abortou porque o runtime `Microsoft.NETCore.App 3.1.0` nao esta instalado na maquina.
- `dotnet build src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release`
  - Resultado: sucesso, 0 warnings, 0 erros.
- Harness temporario `net8.0` referenciando o projeto atual:
  - Resultado: sucesso; propriedade comum, value type com `Convert`, colisoes `Format`/`Duration` e expression invalida se comportaram como esperado.
- Harness temporario `net8.0` compilando `ReflectionHelper.cs` de `HEAD` antes da alteracao:
  - Resultado: falhou como esperado em `Format`, retornando `RuntimeMethodInfo` em vez de `PropertyInfo`.
- `dotnet restore .\Dapper.FluentMap.sln`
  - Resultado: sucesso com cache NuGet temporario.
- `dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore`
  - Resultado: sucesso, 0 warnings, 0 erros.
- `dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build`
  - Resultado: abortou porque os projetos de teste miram `netcoreapp3.1` e o runtime `Microsoft.NETCore.App 3.1.0` nao esta instalado.

Riscos e limitacoes:

- A suite oficial nao foi executada ate o fim neste ambiente por ausencia do runtime `netcoreapp3.1`.
- A mudanca torna expressoes invalidas mais explicitas via `ArgumentException`; isso substitui falhas indiretas anteriores como `null` ou `InvalidCastException`.
- Dommel nao recebeu alteracao funcional.
