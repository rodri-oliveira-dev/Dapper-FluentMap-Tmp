# Runtime Integration de Generated Materializers

Status: SPECIFICATION + IMPLEMENTATION
Prompt: 7.6
Data: 2026-07-28

## Objetivo

Integrar `QueryMapped*` ao caminho de materializacao gerada quando existir um descriptor compativel, preservando `NestedMaterializationPlan` como fallback autoritativo.

O consumidor continua usando a mesma API:

```csharp
connection.QueryMapped<Customer>(sql);
connection.QueryMappedSingle<Customer, LegacyProfile>(sql);
```

Nenhum detalhe de dispatch, descriptor ou fallback e exigido do usuario.

## Lookup

O lookup acontece uma vez por execucao de query, depois que o `IDataReader` esta aberto e antes da iteracao das linhas.

Chave:

```text
EntityType + ProfileType opcional + ordered ColumnShape
```

O shape e a sequencia exata de nomes retornados por `IDataRecord.GetName(ordinal)`. A ordem importa porque o materializer gerado usa ordinais fixos.

## Dispatch

Fluxo:

```text
QueryMapped<T>
  -> Dapper ExecuteReader
  -> ler column names
  -> TryGetGeneratedMaterializer(entity, profile, ordered shape)
       -> found e valido: usar delegate gerado por linha
       -> missing/invalido: usar GetMaterializationPlan runtime
  -> List<T> bufferizada
```

O dispatch fica antes do loop `reader.Read()`. Assim:

- nao ha lookup por linha;
- o hot path gerado chama apenas o delegate direto;
- reflection e validacao de descriptor ficam fora da iteracao;
- fallback runtime preserva os caches existentes.

## Caching

Generated materializers sao armazenados no registry interno por `MaterializationPlanCacheKey`.

Propriedades da chave:

- tipo da entidade;
- tipo de profile ou `null`;
- nomes de colunas em ordem ordinal;
- comparacao ordinal de strings;
- hash estruturado, sem concatenacao textual.

O registry generated cresce apenas por descriptors registrados, nao por cada query executada. Queries com shapes desconhecidos nao criam entradas generated. O cache runtime de `NestedMaterializationPlan` continua separado e cresce por shapes efetivamente usados no fallback, como antes desta etapa.

## Profiles

Profiles possuem descriptors separados do map default:

```text
Customer + null + shape
Customer + LegacyProfile + shape
```

`QueryMapped<TEntity>()` procura somente o descriptor default. `QueryMapped<TEntity, TProfile>()` exige que o profile esteja registrado e procura somente o descriptor daquele profile.

Um generated descriptor nao registra profile implicitamente e nao altera o type map global do Dapper.

## Validacao de Descriptor

Um descriptor encontrado pelo shape ainda precisa bater com o mapping efetivo atual.

Para cada coluna:

- coluna materializada deve resolver para o mesmo `MemberPath`;
- coluna ignorada deve continuar ignorada;
- coluna materializada nao pode resolver para mapping ignorado;
- quando nao houver FluentMap/convention/profile, o fallback default do Dapper precisa apontar para o mesmo membro root;
- profile ausente continua falhando com `FluentMapConfigurationException`.

Se qualquer verificacao falhar, o descriptor e tratado como invalido e o runtime fallback materializa a query.

## Invalid Generated Materializer

Descriptor invalido nao e erro de consulta por si so. Ele e uma otimizacao rejeitada.

Exemplos:

- descriptor gerado a partir de metadata antiga;
- column shape com mesma coluna em ordem diferente;
- member path divergente;
- coluna esperada como ignorada mas configuracao efetiva materializa;
- coluna esperada como materializada mas configuracao efetiva ignora.

O fallback evita que um delegate gerado com ordinais incorretos produza objeto incorreto.

## Diagnostics

`Explain<T>()` nao conhece o SQL nem o shape real do reader. Por isso ele nao deve prometer que uma query especifica usara generated materializer.

Nesta etapa, `MappingExplanation.Diagnostics` passa a indicar somente a presenca de descriptors generated registrados para a entidade/profile e deixa claro que a selecao real depende de:

- ordem de colunas do reader;
- compatibilidade com o mapping efetivo;
- fallback runtime quando nao houver match seguro.

Nao foi adicionado enum publico `Generated` a `MappingMaterialization`, porque esse enum descreve como cada membro e materializado semanticamente (`Dapper`, `Nested`, `ValueObject`), nao qual delegate foi escolhido para uma query especifica.

Fallback reason por query permanece fora da API publica nesta etapa para evitar contrato fragil baseado em SQL/reader shape. Uma API futura pode receber explicitamente um shape de colunas e retornar uma explicacao de dispatch.

## Thread Safety

O registry usa `ConcurrentDictionary` para:

- maps default;
- profile maps;
- conventions;
- materialization plans runtime;
- generated materializers.

Lookup generated e lock-free sobre snapshot de descriptor. O delegate gerado precisa ser thread-safe por contrato pratico: ele nao deve guardar estado mutavel por linha. O generator atual emite metodos estaticos sem estado.

Registros duplicados para a mesma entidade/profile/shape sao rejeitados de forma deterministica.

## Startup Behavior

O fluxo recomendado continua:

```csharp
FluentMapper.Initialize(config =>
{
    config.AddGeneratedMappings();
});
```

`AddGeneratedMappings()` registra maps/profiles pelos caminhos existentes e, para mapas geraveis, registra descriptors de materializer.

Sem pacote generator, sem descriptor gerado ou com map nao geravel, `QueryMapped*` usa o fallback runtime existente.

## Configuration Lifecycle

O FluentMap continua sendo configuracao global de processo e deve ser inicializado no startup.

Registros feitos por APIs de configuracao invalidam caches runtime de mapping/plano do tipo afetado. Descriptors gerados nao sao removidos nessas invalidacoes; eles continuam registrados, mas so sao usados se passarem pela validacao contra o mapping efetivo no lookup.

`FluentMapper.Reset(...)` limpa maps, profiles, conventions, caches runtime e generated descriptors. Os dicionarios publicos mutaveis legados permanecem por compatibilidade, mas alteracoes diretas neles fora do lifecycle recomendado podem contornar invalidacoes, como ja ocorria com caches existentes.

## Backward Compatibility

Compatibilidade preservada:

- `QueryMapped*` continua aceitando os mesmos parametros e retornando resultados bufferizados;
- `Dapper.Query<T>()` e type maps globais nao mudam;
- profiles continuam query-scoped;
- fallback runtime permanece obrigatorio;
- annotations `RequiresUnreferencedCode` e `RequiresDynamicCode` permanecem porque qualquer query ainda pode cair no fallback;
- consumidores sem generator nao precisam mudar codigo.

## Testes

Cobertura esperada nesta etapa:

- generated selected antes da iteracao;
- fallback selected quando descriptor esta ausente;
- fallback selected quando metadata generated e invalida;
- profiles generated;
- nested generated via source generator;
- immutable constructor generated via source generator;
- Value Object generated via source generator;
- concorrencia de lookup e de queries;
- queries repetidas sem crescimento de cache runtime no generated path;
- equivalencia funcional entre resultado runtime e generated para shapes equivalentes.

## Benchmarks

A suite principal deve comparar explicitamente:

- Dapper puro;
- Dapper + FluentMap root mapping;
- `QueryMapped*` com shape canonico gerado;
- `QueryMapped*` runtime fallback por shape equivalente em ordem diferente.

Resultados locais devem ser registrados em `.sdd/etapa-7/02-performance-baseline.md` como evidencia, nao como promessa publica.
