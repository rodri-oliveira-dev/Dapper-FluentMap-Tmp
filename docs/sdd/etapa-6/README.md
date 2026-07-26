# Etapa 6 - Architectural Hardening

## Objective

Formalizar contratos arquiteturais que reduzem ambiguidade sobre estado global, lifecycle de configuracao, integracao com Dapper e futuros caminhos de materializacao.

Esta etapa preserva a compatibilidade publica existente do core `Dapper.FluentMap` e usa Specification-Driven Development para separar contrato, decisao e implementacao.

## Deliveries

| Delivery | Title | Status | Notes |
|---|---|---|---|
| 01 | Configuration Lifecycle | COMPLETED | Lifecycle suportado e mutacoes de runtime formalizados. |
| 02 | Mapping State Encapsulation | COMPLETED | Snapshots read-only adicionados e superficie mutavel legada documentada. |
| 03 | Dapper Compatibility Adapters | COMPLETED | Compatibility boundary interno adicionado; TypeHandler reflection isolada; ignored sentinel removido. |
| 04 | Generated Materializer Spike | NEXT | Investigar materializer gerado para `DbDataReader`. |

## Delivery List

01 Configuration Lifecycle -> COMPLETED
02 Mapping State Encapsulation -> COMPLETED
03 Dapper Compatibility Adapters -> COMPLETED
04 Generated Materializer Spike -> NEXT

## Sources Of Truth

- `docs/sdd/fluentmap-risk-assessment.md`
- `docs/sdd/etapa-1/`
- `docs/sdd/etapa-2/`
- `docs/sdd/etapa-3/`
- `docs/sdd/etapa-4/`
- `docs/sdd/etapa-5/`
- `src/Dapper.FluentMap/FluentMapper.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`

## Current Focus

Delivery 03 isolated Dapper compatibility details behind internal adapters. Residual TypeHandler reflection remains, but only in `DapperTypeHandlerAdapter`; `IgnoredPropertyInfo` was removed.

Delivery 04 should investigate a generated `DbDataReader` materializer while preserving lifecycle, profiles, `MemberPath`, TypeHandler behavior and ignored mapping semantics.
