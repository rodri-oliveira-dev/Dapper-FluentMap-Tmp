# Etapa 3

## Objetivo

Modernizar pontos de configuracao avancada do `Dapper.FluentMap`, preservando a API publica historica e preparando evolucoes seguras em registro de mappings, constructor mapping, tipos imutaveis, validacao e diagnosticos.

## Dependencia Das Etapas 1 E 2

A Etapa 3 depende das decisoes das Etapas 1 e 2 sobre `MappingRegistry`, cache estruturado, precedencia entre mappings explicitos, mappings herdados, conventions, naming policies e fallback do Dapper.

Antes de alterar uma decisao registrada nas etapas anteriores, deve existir evidencia tecnica e a nova decisao deve ser documentada nesta pasta.

## Leitura Obrigatoria

Antes das proximas entregas desta etapa, leia:

- `docs/sdd/etapa-1/README.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-1/04-mapping-registry-cache.md`
- `docs/sdd/etapa-2/README.md`
- `docs/sdd/etapa-2/status.md`
- `docs/sdd/etapa-2/decisions.md`
- `docs/sdd/etapa-2/04-naming-policies.md`
- `docs/sdd/etapa-3/README.md`
- `docs/sdd/etapa-3/status.md`
- `docs/sdd/etapa-3/decisions.md`
- o relatorio da entrega anterior nesta pasta

## Compatibilidade Publica

A API publica existente deve ser preservada sempre que razoavelmente possivel. APIs novas devem ser aditivas e nao devem marcar membros historicos como obsoletos sem estrategia explicita de migracao.

## TargetFrameworks

Os projetos de `src/` devem continuar compativeis com `netstandard2.0`. Projetos de teste devem permanecer no framework ja consolidado pelas migracoes anteriores.

## Escopo

Entregas:

1. 01 - Registro e descoberta de mappings
2. 02 - Constructor mapping, records e tipos imutaveis
3. 03 - Validate e Explain

O escopo padrao continua sendo o projeto principal `Dapper.FluentMap`. `Dapper.FluentMap.Dommel` nao deve receber alteracao funcional nesta etapa, salvo adaptacao tecnica estritamente necessaria provocada por API compartilhada.
