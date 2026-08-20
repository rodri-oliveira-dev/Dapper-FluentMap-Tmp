# Complex Generated Materialization

Status: SPECIFICATION + IMPLEMENTATION
Prompt: 7.5
Data: 2026-07-28

## Objetivo

Expandir os materializers gerados alem dos mapas flat do Prompt 7.4 para cobrir object graphs que ja fazem parte do contrato funcional de `QueryMapped*`:

- nested object paths;
- nested mutable objects;
- nested immutable objects;
- immutable Value Objects por componentes;
- constructor composition;
- null subtree semantics;
- profiles com nested mapping.

O runtime materializer continua sendo o fallback autoritativo.

## Construção de Object Graph

O generator passou a montar uma arvore de metadata por map reconhecido:

```text
GeneratedMaterializationNode
  leaves: colunas escalares materializadas naquele tipo
  children: subobjetos por propriedade intermediaria
  constructor: plano de construtor quando necessario
  post-constructor leaves/children: atribuicoes restantes
  subtree ordinals: ordinais usados para null subtree
```

Essa representacao evita tratar apenas o nome terminal da propriedade. Paths como `Rank.Level` e `Seniority.Level` viram nodes distintos e descriptors distintos:

```text
rank_level      -> Rank.Level
seniority_level -> Seniority.Level
```

O descriptor gerado segue usando `GeneratedMaterializerColumn.Map(column, memberPath)` com o member path completo.

## Ordem de Criação

A criacao segue a mesma intencao do runtime:

1. ler valores escalares necessarios para construtores;
2. construir children usados por construtores, bottom-up;
3. construir o tipo atual por construtor ou construtor sem parametros;
4. aplicar children restantes;
5. aplicar leaves restantes por setters publicos.

Para objetos mutaveis, o codigo gerado cria ou reutiliza o objeto intermediario quando ha getter publico e construtor sem parametros. Quando nao ha getter publico, cria uma instancia nova e atribui pelo setter publico.

Para objetos imutaveis e Value Objects, o codigo gerado constroi o child antes de passá-lo ao construtor do parent.

## Constructor Matching

O generator seleciona construtores publicos quando a resolucao e deterministica:

- cada parametro precisa vincular por nome case-insensitive a uma leaf ou child do node atual;
- o tipo do parametro precisa ser compativel com o tipo da propriedade ou child, apos unwrap de `Nullable<T>`;
- membros sem setter publico precisam estar vinculados a construtor;
- children sem setter publico no parent precisam estar vinculados a construtor;
- quando mais de um construtor tem a mesma melhor pontuacao, o materializer gerado nao e emitido.

Falhas de dominio em construtores gerados sao encapsuladas em `FluentMapConfigurationException` com contexto de tipo, member path e colunas.

## Null Subtree

Para cada child node, o codigo gerado testa todos os ordinais da subarvore:

```text
if any subtree column is non-null:
    materialize child
else:
    assign null when the parent property has public setter and accepts null
```

Isso preserva a semantica do runtime: uma subarvore inteira `NULL` nao cria instancia vazia. Subarvore parcialmente preenchida cria o objeto e aplica `null`/default por leaf conforme a conversao escalar existente.

## Nullable Columns

O helper gerado de leitura escalar permanece alinhado ao Prompt 7.4:

- `DBNull` retorna `default(T)`;
- reference types e `Nullable<T>` recebem `null`;
- value types nao anulaveis recebem `default`;
- enums aceitam texto ou valor numerico;
- `Guid` aceita texto;
- os demais escalares suportados usam `Convert.ChangeType(..., InvariantCulture)`.

Nullable reference annotations nao mudam a semantica runtime atual; o comportamento continua baseado no tipo CLR observavel.

## Nested Mutable vs Immutable

Nested mutable e gerado quando:

- cada tipo intermediario e classe acessivel pelo codigo gerado;
- o tipo possui construtor publico sem parametros;
- leaves restantes tem setters publicos;
- o parent consegue receber o child por setter publico quando necessario.

Nested immutable e gerado quando:

- o child ou parent nao pode ser preenchido por setters;
- existe construtor publico deterministico que vincula leaves ou children por nome/tipo;
- children necessarios sao construidos bottom-up.

Casos sem construtor deterministico continuam usando fallback runtime.

## Value Objects

Value Objects por componentes usam a mesma regra de immutable nested objects. O caso:

```csharp
Map(customer => customer.Cpf.Number).ToColumn("cpf");
```

gera:

```text
cpf column -> Cpf(string number) -> Customer(..., Cpf cpf)
```

Factory methods continuam fora do contrato desta etapa. Value Object escalar mapeado como propriedade inteira continua sendo melhor atendido por TypeHandler do Dapper e pelo fallback runtime quando necessario.

## Unsupported Paths

O generator nao emite materializer quando encontra:

- `IncludeBase<TBase>()`;
- conventions ou naming policies como fonte de colunas geradas;
- nomes de coluna nao literais;
- chains de mapping nao reconhecidas;
- collections ou tipos intermediarios nao acessiveis;
- leaf com tipo escalar nao suportado;
- factory methods;
- TypeHandlers no caminho gerado;
- construtores ausentes, incompletos ou ambiguos.

Esses casos mantem `AddMap<TMap>()` / `AddProfile<TMap>()` e recebem diagnostic informativo `DFM011`; a materializacao funcional fica com `NestedMaterializationPlan`.

## Profiles

Profiles nested sao gerados com descriptor separado:

```text
EntityType + ProfileType + ordered ColumnShape
```

O profile continua query-scoped por `QueryMapped<TEntity, TProfile>()` e nao altera o type map global do Dapper.

## Diagnostics

`DFM011` continua informativo e indica por que um map registrado nao recebeu materializer gerado. O diagnostic nao transforma fallback em erro.

O runtime ainda nao expoe diagnostico publico de `Generated` vs `Runtime`; a integracao e testada observando que `MaterializationPlanCacheEntryCount` permanece `0` quando o materializer gerado e usado.

## Fallback

Fallback permanece obrigatorio:

- sem descriptor para entity/profile/shape;
- shape ordenado divergente;
- descriptor incompativel com mapping efetivo;
- feature nao suportada pelo generator;
- configuracao dinamica em runtime.

O generated path nao muda o comportamento de `Dapper.Query<T>()` nem remove annotations de trimming/dynamic-code de `QueryMapped*`.

## Testes

Cobertura adicionada:

- nested mutable gerado;
- null subtree gerado;
- Value Object por `Cpf.Number`;
- Value Object nullable quando todas as colunas do subtree sao `NULL`;
- dois paths terminando em `Level` (`Rank.Level` e `Seniority.Level`);
- constructor composition root + child;
- constructor incompatível com fallback `DFM011`;
- profile + nested mapping;
- fallback por shape sem descriptor.

## Benchmarks

O benchmark steady state existente usa `AddGeneratedMappings()` e, apos este prompt, passa a exercitar generated materializers nos cenarios nested e Value Object.

Resultados locais foram registrados em `.sdd/etapa-7/02-performance-baseline.md`, secao `Apos Prompt 7.5`.

## Limitacoes

- A metadata comum e interna ao generator; runtime e generated ainda nao compartilham uma biblioteca unica de plano.
- A semantica foi mantida alinhada por testes de equivalencia e pelos descriptors validados contra o mapping efetivo.
- TypeHandlers seguem no fallback.
- `IncludeBase<TBase>()` continua fora do generated materializer.
- Native AOT completo ainda exige validacao dedicada.
