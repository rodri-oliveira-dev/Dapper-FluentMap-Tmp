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
