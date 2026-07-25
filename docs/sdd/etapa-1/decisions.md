# Decisoes Da Etapa 1

Registre aqui apenas decisoes que afetem entregas posteriores.

## ReflectionHelper

- Expressoes de propriedade devem ser resolvidas pelo `MemberExpression.Member` produzido pela expression tree, sem nova busca por nome via reflection.
- APIs que recebem `Expression<Func<TEntity, object>>` para mapeamento devem aceitar `Convert` gerado por boxing de value types.
- Expressoes que nao resolvem para propriedade devem falhar cedo com `ArgumentException`, em vez de produzir `null`, `InvalidCastException` ou depender de falhas indiretas.
