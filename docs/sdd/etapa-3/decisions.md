# Decisoes Da Etapa 3

Registre aqui apenas decisoes que afetem entregas posteriores.

## Registro E Descoberta De Mappings

- `AddMap<TEntity>(IEntityMap<TEntity>)` permanece como API historica de registro por instancia.
- `AddMap<TMap>()` e o caminho explicito moderno sem assembly scanning; `TMap` deve implementar exatamente uma interface fechada `IEntityMap<TEntity>` e possuir construtor publico sem parametros.
- `AddMap<TMap>()` infere `TEntity` por reflection limitada sobre as interfaces do map e registra pelo mesmo `MappingRegistry`.
- `AddMapsFromAssembly(...)` e `AddMapsFromAssemblyContaining<TMarker>(...)` sao conveniencias de discovery e nao substituem o caminho explicito.
- O scanning moderno considera apenas tipos exportados, concretos e fechados que implementam `IEntityMap`.
- O scanning aceita filtros opcionais de namespace, ordena candidatos de forma deterministica e registra maps base incluidos antes dos derivados quando ambos sao descobertos juntos.
- Duplicidade de entidade, seja por registro explicito, scanning ou combinacao dos dois, e erro de configuracao; nao ha comportamento "ultimo ganha".
- Reflection restante desta entrega: inferencia de entidade via `IEntityMap<TEntity>`, scanning por assembly, `Activator.CreateInstance` para criar maps descobertos e criacao interna de `FluentMapTypeMap<>` no `MappingRegistry`. AOT/trimming completo permanece fora do contrato atual.

## Constructor Mapping E Imutaveis

- Constructor mapping do FluentMap deve traduzir metadata para o Dapper, nao materializar objetos diretamente.
- Mappings explicitos, mappings herdados, conventions e naming policies influenciam constructor selection e `IMemberMap.Parameter` quando resolvem para propriedade simples e nao ignorada.
- A selecao de construtor continua delegada ao `DefaultTypeMap`; ambiguidades e parametros opcionais seguem o comportamento do Dapper.
- A precedencia consolidada tambem vale para parametros de construtor: explicit derivado -> explicit herdado -> convention/naming policy -> Dapper default.
- Constructor parameters nao sao `MemberPath`; mappings aninhados nao participam de constructor mapping nem implicam suporte a nested object materialization.
- Records posicionais e classes imutaveis passam a funcionar quando seus parametros correspondem a propriedades simples mapeadas ou resolvidas pelo fallback do Dapper.
