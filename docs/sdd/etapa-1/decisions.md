# Decisoes Da Etapa 1

Registre aqui apenas decisoes que afetem entregas posteriores.

## ReflectionHelper

- Expressoes de propriedade devem ser resolvidas pelo `MemberExpression.Member` produzido pela expression tree, sem nova busca por nome via reflection.
- APIs que recebem `Expression<Func<TEntity, object>>` para mapeamento devem aceitar `Convert` gerado por boxing de value types.
- Expressoes que nao resolvem para propriedade devem falhar cedo com `ArgumentException`, em vez de produzir `null`, `InvalidCastException` ou depender de falhas indiretas.

## Composicao De Mappings

- A estrategia instalada pelo FluentMap deve resolver mappings explicitos antes de conventions e usar `DefaultTypeMap` do Dapper como fallback final.
- `AddMap(...)` e `AddConvention(...).ForEntity(...)` nao devem depender da ordem de registro para coexistirem no mesmo tipo.
- Mapping explicito para uma propriedade impede que conventions resolvam essa mesma propriedade, permitindo override explicito da convention.
- `FluentConventionTypeMap<TEntity>` permanece publico para compatibilidade, mas os fluxos internos de convention passam a instalar o type map composto.
- Registry e invalidacao completa de cache continuam deliberadamente adiados para a Entrega 4.

## Testes De Integracao Com Dapper

- A baseline de integracao usa SQLite in-memory via `Microsoft.Data.Sqlite` apenas no projeto de testes principal.
- Os testes de integracao devem validar materializacao observavel por `Dapper.Query<T>`, nao detalhes internos de `ITypeMap`.
- Enquanto o estado global nao for redesenhado, testes que alteram `FluentMapper` devem limpar `EntityMaps`, `TypeConventions` e os type maps do Dapper para os tipos tocados.
- O paralelismo da suite permanece desabilitado porque `FluentMapper`, `SqlMapper.SetTypeMap` e o cache estatico de `MultiTypeMap` compartilham estado global.
- A Entrega 4 deve considerar um mecanismo definido de registry/reset/invalidation para evitar dependencia de tipos unicos por teste e reduzir risco de cache stale.
