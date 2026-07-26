# Etapa 2

## Objetivo

Fortalecer a identidade interna de membros e propriedades para preparar evolucoes seguras em validacao, heranca de mappings e naming policies.

## Dependencia Conceitual Da Etapa 1

A Etapa 2 depende das decisoes da Etapa 1 sobre resolucao de expressions, composicao entre mappings explicitos e conventions, `MappingRegistry`, caches estruturados, estado global e testes de integracao.

Antes de alterar uma decisao registrada na Etapa 1, deve existir evidencia tecnica e uma nova decisao deve ser registrada nesta pasta.

## Escopo

Entregas:

1. 01 - MemberPath
2. 02 - Validacao e diagnosticos
3. 03 - Heranca de mappings
4. 04 - Naming policies

O escopo padrao continua sendo o projeto principal `Dapper.FluentMap`. `Dapper.FluentMap.Dommel` nao deve receber alteracao funcional nesta etapa, salvo se uma mudanca comprovada no core exigir adaptacao explicita.

## Leitura Obrigatoria

Antes das proximas entregas, leia:

- `docs/sdd/etapa-1/README.md`
- `docs/sdd/etapa-1/status.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-1/04-mapping-registry-cache.md`
- `docs/sdd/etapa-2/README.md`
- `docs/sdd/etapa-2/status.md`
- `docs/sdd/etapa-2/decisions.md`
- o relatorio da entrega anterior nesta pasta

## Compatibilidade Publica

A API publica existente deve ser preservada sempre que possivel. `PropertyInfo` exposto por `IPropertyMap.PropertyInfo` e `PropertyMap.PropertyInfo` permanece como membro terminal por compatibilidade.

## Fora Do Escopo

`MemberPath` representa identidade e diagnostico de caminho. Ele nao implementa materializacao de objetos aninhados, Value Objects, custom materializers, source generators, wrappers de query ou geracao de SQL.
