# Decisoes Da Etapa 5

Registre aqui apenas decisoes arquiteturais necessarias as proximas entregas.

## Nested Materialization

- `MemberPath` continua sendo identidade e diagnostico de caminho; ele nao deve ser entregue diretamente ao Dapper como `PropertyInfo` terminal para simular nested assignment.
- `Dapper.Query<T>` com o `ITypeMap` atual do Dapper permanece suportado para mappings simples, constructor mapping simples, conventions, naming policies e fallback.
- Nested object materialization e opt-in por uma API paralela de consulta/materializacao: `QueryMapped<T>` e `QueryMappedSingle<T>`.
- O caminho opt-in le valores do reader e aplica um plano de materializacao baseado em `MemberPath`.
- Nested paths nao sao tratados como propriedades simples pelo type map instalado no Dapper, porque isso pode escrever o valor do leaf no slot errado do objeto raiz.
- A Entrega 2 suporta objetos aninhados mutaveis com construtor publico sem parametros e propriedades publicamente settable.
- A semantica de `NULL` e por subarvore: quando todos os valores nested de uma subarvore sao `NULL`, o intermediario fica `null`; quando algum valor nao e `NULL`, o intermediario e criado ou reutilizado.
- `Explain<TEntity>()` representa nested mappings com `Materialization = Nested`.
- Prefix conflicts como `Address` e `Address.City` no mesmo plano sao rejeitados.

## Value Objects

- Value Objects escalares devem usar o mecanismo publico de TypeHandlers do Dapper quando o mapping aponta para a propriedade Value Object inteira, por exemplo `Map(x => x.Cpf).ToColumn("cpf")`.
- TypeHandler nao resolve nested path arbitrario como `Map(x => x.Cpf.Number).ToColumn("cpf")`, porque o Dapper passa a converter e atribuir o membro terminal (`Number`), nao o Value Object (`Cpf`).
- Value Objects imutaveis dentro de grafos aninhados exigem materializacao controlada pelo FluentMap ou geracao de materializer; nao devem ser declarados suportados por `ITypeMap` puro.
- `QueryMapped*` suporta nested Value Objects por construtores publicos quando todos os parametros exigidos correspondem a propriedades mapeadas ou objetos aninhados mapeados.
- Factory methods como `Cpf.Create(...)` nao foram implementados nesta etapa; qualquer suporte futuro deve ser API publica explicita, fortemente tipada e com regras de ambiguidade proprias.
- Nao ha suporte a private constructor, private setter, field injection, `FormatterServices` ou alteracao de backing field.
- A semantica de `NULL` para Value Object e por subarvore: se todas as colunas da subarvore sao `NULL`, o Value Object resultante e `null`; se alguma coluna possui valor, o construtor publico e usado.
- Excecoes de dominio lancadas por construtores sao preservadas como `InnerException` de `FluentMapConfigurationException` com contexto de entidade, `MemberPath`, tipo, construtor e colunas.

## Records E Imutabilidade

- Records posicionais e classes imutaveis simples continuam sendo responsabilidade do constructor mapping existente quando todos os parametros sao simples.
- Nested records, nested immutable objects e construcao de Value Objects por construtor devem ser tratados por uma estrategia complementar ao `ITypeMap` do Dapper.
- Nested records e nested immutable objects passam a ser suportados no caminho opt-in `QueryMapped*` quando a arvore completa pode ser construida por construtores publicos compativeis.
- Mappings simples de records/classes imutaveis via `Dapper.Query<T>` continuam preservados pelo constructor mapping da Etapa 3.

## Source Generation, Trimming E AOT

- O generator da Etapa 4 continua limitado a registro de mappings.
- Um materializer gerado pode ser uma estrategia futura para performance, trimming e Native AOT, mas nao deve ser acoplado a Entrega 2 como unico caminho.
- O caminho runtime/reflection-based de `QueryMapped*` e documentado como menos AOT-friendly e foi anotado com `RequiresUnreferencedCode` e `RequiresDynamicCode`; o caminho gerado deve ser a opcao preferencial para consumidores trimmed/AOT quando existir.
- A Entrega 3 nao amplia o generator para materializar `DbDataReader`; o smoke AOT valida registro/diagnostico de Value Object, nao runtime AOT completo de `QueryMapped*`.

## Mapping Profiles

- Multiple mapping profiles por tipo sao suportados apenas no caminho opt-in `QueryMapped*`; `Dapper.Query<T>` continua usando o mapping default registrado por `AddMap(...)`.
- A identidade de profile e fortemente tipada por marker `TProfile : IMappingProfile`; maps de profile implementam `IProfileMap<TProfile>`.
- A API de registro escolhida e `configuration.AddProfile<TMap>()`, inferindo a entidade por `IEntityMap<TEntity>` e o profile por `IProfileMap<TProfile>`.
- A API de consulta escolhida e query-scoped: `QueryMapped<TEntity,TProfile>(...)`, `QueryMappedSingle<TEntity,TProfile>(...)`, `QueryMappedAsync<TEntity,TProfile>(...)` e `QueryMappedSingleAsync<TEntity,TProfile>(...)`.
- `SqlMapper.SetTypeMap` nao e usado para profiles; o type map global do Dapper permanece representando apenas o default.
- O registry passa a modelar `EntityMaps[EntityType]` para default e `ProfileMaps[(EntityType, ProfileType)]` para profiles.
- `MappingCacheKey` e `MaterializationPlanCacheKey` incluem `ProfileType`, evitando reutilizacao de planos entre profiles.
- `IncludeBase<TBase>()` dentro de profile map procura a base no mesmo `TProfile`; nao ha heranca silenciosa do default dentro de profile alternativo.
- Conventions e naming policies continuam por entidade e sao aplicadas de forma read-only tambem em profiles; per-profile conventions ficam como divida futura.
- `Explain<TEntity>()` continua descrevendo o default; `Explain<TEntity,TProfile>()` descreve o profile e expoe `MappingExplanation.ProfileType`.
- O source generator distingue default maps de profile maps: default gera `AddMap<TMap>()`, profile gera `AddProfile<TMap>()`; duplicidade de profile gerada usa `DFM008`.
- O analyzer adiciona `DFM009` para `AddProfile<TMap>()` invalido e `DFM010` para duplicidade conhecida de entity/profile no mesmo metodo de configuracao.
- Profiles nao implementam multi-mapping, streaming unbuffered nem materializer gerado nesta entrega.
