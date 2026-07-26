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

## Records E Imutabilidade

- Records posicionais e classes imutaveis simples continuam sendo responsabilidade do constructor mapping existente quando todos os parametros sao simples.
- Nested records, nested immutable objects e construcao de Value Objects por construtor devem ser tratados por uma estrategia complementar ao `ITypeMap` do Dapper.

## Source Generation, Trimming E AOT

- O generator da Etapa 4 continua limitado a registro de mappings.
- Um materializer gerado pode ser uma estrategia futura para performance, trimming e Native AOT, mas nao deve ser acoplado a Entrega 2 como unico caminho.
- O caminho runtime/reflection-based de `QueryMapped*` e documentado como menos AOT-friendly e foi anotado com `RequiresUnreferencedCode` e `RequiresDynamicCode`; o caminho gerado deve ser a opcao preferencial para consumidores trimmed/AOT quando existir.
