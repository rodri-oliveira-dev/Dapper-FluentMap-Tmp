# Etapa 5

## Objetivo

Investigar e evoluir o `Dapper.FluentMap` para suportar, de forma segura e opt-in, materializacao de objetos aninhados, Value Objects imutaveis e perfis de mapping, sem transformar a biblioteca em ORM, query builder ou camada de CRUD.

## Dependencia Das Etapas 1 A 4

Esta etapa depende das decisoes anteriores sobre:

- `MemberPath` como identidade interna de caminho;
- `MappingRegistry`, cache estruturado e precedencia efetiva;
- constructor mapping para propriedades simples;
- records e tipos imutaveis simples;
- API publica `Validate()` e `Explain<TEntity>()`;
- limites atuais do `ITypeMap` do Dapper;
- trimming, Native AOT e source generation.

Nenhuma decisao das etapas anteriores deve ser revertida sem evidencia tecnica registrada nesta pasta.

## Leitura Obrigatoria

Antes de iniciar qualquer entrega desta etapa, leia:

- `docs/sdd/etapa-1/README.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-2/README.md`
- `docs/sdd/etapa-2/decisions.md`
- `docs/sdd/etapa-2/01-member-path.md`
- `docs/sdd/etapa-2/03-inherited-mappings.md`
- `docs/sdd/etapa-3/README.md`
- `docs/sdd/etapa-3/decisions.md`
- `docs/sdd/etapa-3/02-constructor-immutable-mapping.md`
- `docs/sdd/etapa-3/03-diagnostics-api.md`
- `docs/sdd/etapa-4/README.md`
- `docs/sdd/etapa-4/decisions.md`
- `docs/sdd/etapa-4/02-trimming-aot.md`
- `docs/sdd/etapa-4/03-source-generator.md`
- `docs/sdd/etapa-5/decisions.md`
- relatorios ja concluidos em `docs/sdd/etapa-5/`.

## Escopo

O escopo padrao continua sendo o projeto principal `Dapper.FluentMap` e seus testes.

`Dapper.FluentMap.Dommel` nao deve receber alteracao funcional nesta etapa, salvo se uma mudanca comprovada no core exigir adaptacao explicita e documentada.

## Compatibilidade

- Preserve a API publica existente sempre que possivel.
- Preserve `netstandard2.0` nos projetos de `src/`.
- Nao altere os TargetFrameworks atuais sem decisao arquitetural futura e especifica.
- Nao mude o comportamento de `Dapper.Query<T>` para prometer nested materialization implicitamente.
- Qualquer nova capacidade de materializacao aninhada deve ser opt-in e testada com Dapper real.

## Fora Do Escopo

Esta etapa nao deve transformar o FluentMap em:

- ORM;
- query builder;
- gerador de SQL;
- camada de CRUD;
- change tracker;
- unit of work.

## Entregas

1. 01 - Spike de nested/value-object materialization
2. 02 - Nested object materialization
3. 03 - Value Objects imutaveis
4. 04 - Mapping profiles

## Resultado da Etapa 5

Capacidades entregues:

- nested mapping opt-in por `QueryMapped<T>()` e `QueryMappedSingle<T>()`, preservando `Dapper.Query<T>()` para o comportamento default;
- `MemberPath` preservado como identidade completa de paths como `Address.City`, `Rank.Level` e `Seniority.Level`;
- null semantics por subarvore: subarvore toda `NULL` resulta em intermediario/value object `null`; subarvore parcialmente preenchida cria o objeto;
- Value Objects imutaveis e nested immutable objects por construtores publicos compativeis, sem setters privados, fields ou bypass de invariantes;
- strategy de constructor/factory limitada a construtores publicos; factory methods permanecem fora do escopo;
- TypeHandler integration preservada para Value Objects escalares mapeados como propriedade inteira;
- mapping profiles query-scoped por `TProfile : IMappingProfile`, registrados por `AddProfile<TMap>()` e selecionados por `QueryMapped<TEntity,TProfile>()`;
- concorrencia validada para profiles distintos em queries sync e async simultaneas, sem troca de `SqlMapper.SetTypeMap`;
- `Explain<TEntity>()` para default e `Explain<TEntity,TProfile>()` para profile, incluindo `Materialization` e `ProfileType`;
- source generator atualizado para gerar `AddMap<TMap>()` ou `AddProfile<TMap>()` conforme o map;
- analyzer atualizado com diagnostics determinaveis para profile invalid/duplicado.

Compatibilidade:

- `Dapper.Query<T>()`, `AddMap(...)`, `AddMap<TMap>()`, conventions, naming policies, constructor mapping simples e fallback do Dapper continuam preservados;
- o core continua `netstandard2.0`;
- Dommel nao recebeu alteracao funcional nesta etapa.

AOT/trimming:

- `QueryMapped*` continua runtime/reflection-based e anotado com `RequiresUnreferencedCode` e `RequiresDynamicCode`;
- registro explicito e gerado sao os caminhos recomendados para consumidores trimmed;
- source generation ainda gera registro, nao materializer de `DbDataReader`;
- Native AOT runtime completo nao foi validado neste ambiente por ausencia do platform linker C++.

Limitacoes:

- `QueryMapped*` materializa em lista, sem streaming unbuffered;
- profiles nao se aplicam a `Dapper.Query<T>()` nem a multi-mapping do Dapper;
- conventions e naming policies ainda sao por entidade, nao por profile;
- factory methods, private constructors, private setters e field injection continuam fora do contrato;
- materializer gerado permanece futuro.

## Dividas e proximos passos

### P0

- Nenhum item P0 registrado ao encerrar a Etapa 5.

### P1

- Criar materializer gerado para `DbDataReader`, cobrindo nested mappings, Value Objects e profiles sem reflection no hot path.
- Definir suporte a per-profile conventions/naming policies antes de ampliar a composicao de policies.
- Avaliar streaming/unbuffered para `QueryMapped*` com lifetime claro de connection/reader.

### P2

- Adicionar benchmarks formais comparando Dapper default, `QueryMapped<T>()` e `QueryMapped<TEntity,TProfile>()`.
- Expandir overloads async/default de `QueryMapped*` de forma simetrica, se houver demanda publica.
- Melhorar diagnostics de profile inexistente em analyzer somente quando a ausencia puder ser comprovada sem falso positivo cross-assembly.
- Avaliar API publica de factory methods para Value Objects com regras de ambiguidade e validacao.

### Research

- Investigar Native AOT runtime completo em ambiente com platform linker C++ instalado.
- Avaliar integracao futura com APIs publicas novas do Dapper caso surja suporte a materializer/type map por operacao.
- Avaliar modelo de cache imutavel/snapshot para reduzir dependencia de estado global historico.
- Revisar Dommel em etapa propria para decidir se profiles devem ou nao ser visiveis em integrações CRUD externas.
