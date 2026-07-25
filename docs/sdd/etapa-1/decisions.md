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
- Testes que alteram `FluentMapper` devem usar o reset interno definido na Entrega 4 para limpar registry, cache e type maps do Dapper dos tipos tocados.
- O paralelismo da suite permanece desabilitado porque `FluentMapper`, `SqlMapper.SetTypeMap` e os dicionarios publicos mutaveis ainda compartilham estado global.

## MappingRegistry E Cache

- `MappingRegistry` passa a ser o dono interno de entity maps, conventions, cache de propriedades e instalacao de type maps no Dapper.
- `FluentMapper.EntityMaps` e `FluentMapper.TypeConventions` permanecem publicos por compatibilidade, mas apontam para o storage do registry.
- O cache ativo de resolucao usa chave estruturada com tipo, nome de coluna ordinal e opcoes de estrategia (`FluentMap` ou `ConventionOnly`), substituindo as chaves por concatenacao de strings.
- Case sensitivity continua sendo propriedade de cada `IPropertyMap`; a chave diferencia o nome de coluna recebido e a invalidacao por tipo cobre mudancas de configuracao.
- Reconfiguracoes feitas pela API do FluentMap invalidam o cache do tipo afetado e reinstalam o type map composto no Dapper.
- O reset interno de testes limpa entity maps, conventions, cache e type maps do Dapper para os tipos informados.
- `SqlMapper.SetTypeMap` continua como estado global necessario porque e o contrato publico de extensibilidade do Dapper.
- O membro protegido legado `MultiTypeMap.TypePropertyMapCache` nao e mais usado pelo core, mas foi preservado para evitar quebra de compatibilidade.
- Etapa 2 deve tratar qualquer tentativa de reduzir a mutabilidade publica dos dicionarios como mudanca de compatibilidade planejada.
