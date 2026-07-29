# Etapa 10 — Final Report

## Objetivo

Encerrar a Etapa 10 - Property Conversion & Extensibility com auditoria da
especificacao, revisao de API publica, documentacao, validacao runtime/generated,
fronteira Dommel/write, performance e smoke trimming/AOT, sem iniciar recursos
da Etapa 11.

## Implementado

- Contratos publicos direcionais:
  `IReadPropertyConverter<TDatabase, TProperty>`,
  `IWritePropertyConverter<TProperty, TDatabase>` e
  `IPropertyConverter<TDatabase, TProperty>`.
- Delegates publicos:
  `ReadPropertyConverter<TDatabase, TProperty>` e
  `WritePropertyConverter<TProperty, TDatabase>`.
- Fluent API por tipo, instancia e delegate:
  `ConvertFromDatabaseUsing`, `ConvertToDatabaseUsing` e `ConvertUsing`.
- Metadata publica aditiva:
  `PropertyConversionMetadata`, `PropertyConverterMetadata`,
  `PropertyConversionDirection` e `IPropertyMapWithConversionMetadata`.
- Read conversion no runtime materializer comum de `QueryMapped*`,
  `ReadMapped*`, `QueryMultipleMapped` e streaming unbuffered.
- Read conversion no generated materializer para converter types suportados
  estaticamente.
- Diagnostics em runtime validation, analyzer comum e source generator.
- Documentacao publica atualizada no `README.md`.

## Audit SDD

| Requirement | Implementation | Tests | Generated | AOT | Status |
| ----------- | -------------- | ----- | --------- | --- | ------ |
| Contratos read/write independentes | Interfaces direcionais, bidirecional e delegates em `Dapper.FluentMap.Mapping` | `PropertyConversionMetadataTests` | Metadata incluida em descriptors read gerados | Overloads por tipo anotados; instancia/delegate evitam construcao por tipo | Completed |
| Property-scoped conversion | Metadata fica no `PropertyMap` efetivo/member path | Testes com duas propriedades do mesmo tipo | Descriptor valida member path e converter por coluna | Sem estado global novo por tipo | Completed |
| Profile-scoped conversion | Profiles usam maps separados e nao herdam converter default automaticamente | Runtime e generated profile converter tests | Descriptor separado por entity/profile/shape | Registro gerado evita scanning para maps da compilacao atual | Completed |
| Read precedence | `null/DBNull -> property converter -> TypeHandler -> default conversion` no runtime | `RuntimeReadConversionTests`, `DapperCompatibilityAdapterTests` | Generated aplica `null/DBNull -> property converter -> default generated conversion` | `QueryMapped*` permanece anotado por fallback runtime | Completed |
| Dapper `Query<T>()` unchanged | Type map de nomes continua separado de converters property-scoped | Coberto por regressao existente e ausencia de mudanca nesse caminho | Not applicable | Not applicable | Completed |
| TypeHandler sem converter | Runtime consulta `TypeHandler<TProperty>` antes da conversao default | `QueryMappedShouldUseRegisteredDapperTypeHandler*` | TypeHandler no generated path segue fora da etapa | Fallback runtime documentado | Completed |
| Converter + TypeHandler | Converter local tem precedencia na propriedade configurada | `QueryMappedShouldUsePropertyConverterInsteadOfDapperTypeHandlerForThatProperty` | Generated converter nao encadeia TypeHandler | Sem reflexao de internals do Dapper no generated path | Completed |
| Null semantics | `null`/`DBNull` nao entram no converter; targets recebem null/default | Runtime e generated null converter tests | `ReadConverted<TDatabase,TProperty,TTarget>` replica regra | Preserva semantica de subarvore null | Completed |
| Nullable<T> | `T` e `Nullable<T>` tratados como compativeis em configuracao e execucao | Metadata/runtime/generated nullable tests | Generated usa `TTarget` real | Completed | Completed |
| Nested converter | Converter aplica somente a folha terminal do member path | Nested runtime/generated tests | Generated respeita null subtree | Completed | Completed |
| Immutable/value-object converter | Folhas convertidas antes de construtores; Value Object escalar pode usar property converter | Runtime/generated immutable e Value Object tests | Supported para converter type estatico | Generated evita reflection no hot path | Completed |
| Runtime/generated equivalence | Teste dedicado compara resultados runtime e generated | `GeneratedQueryMappedShouldMatchRuntimeFallbackForReadConverters` | Sim | Parcialmente AOT-friendly quando generated e shape casam | Completed |
| Write conversion execution | Core guarda metadata; Dommel nao executa por falta de hook publico de parametro | Dommel boundary test garante que `Insert`/`Update` nao chamam converter | Write-only nao afeta materializer read | Not applicable | Deferred |
| Persistence semantics Etapa 8 | Metadata de persistence continua governando insert/update/select | Dommel persistence tests, 22 testes isolados | Write metadata neutra para read generated | Not applicable | Completed |
| Diagnostics/analyzers | Runtime validation, `DFM014`, `DFM015`, generator `DFM012`, fallback `DFM011` | Analyzer/generator/configuration tests | Diagnostics gerados sem executar construtores | Not applicable | Completed |
| Performance baseline | Benchmarks representativos atualizados | BenchmarkDotNet Dry | Runtime e generated converter medidos | Not applicable | Completed |
| Trimming/AOT | `QueryMapped*` anotado; generated converter evita reflexao por linha | Trim smoke publicado e executado | Generated smoke retorna `generated:ok` | Native AOT bloqueado por linker ausente | Partial |

Divergencias justificadas:

- Write conversion nao foi executada em Dommel porque a API publica Dommel 3.5.3
  nao expoe hook para substituir `DbParameter.Value` por propriedade antes do
  Dapper parametrizar a entidade. O comportamento fica documentado como
  metadata-only.
- TypeHandler no generated path continua adiado desde a Etapa 7. O generated
  path nao usa internals do Dapper; cenarios que dependem de TypeHandler usam
  runtime fallback.
- `QueryMapped*` nao e declarado Native AOT-safe porque pode cair no
  materializer runtime baseado em reflection/dynamic code.

## Converter Model

O modelo final separa conversao por direcao e por escopo. Um converter pertence
ao member path efetivo do property map e, quando configurado em profile, ao
profile selecionado. `ConvertUsing` e atalho bidirecional para tipos que
implementam as duas interfaces compativeis.

Revisao de API:

- Naming esta consistente com as direcoes: `ConvertFromDatabaseUsing` para
  leitura e `ConvertToDatabaseUsing` para escrita.
- `IPropertyConverter<TDatabase, TProperty>` e util como contrato bidirecional,
  sem obrigar cenarios read-only/write-only.
- Overloads por tipo exigem `new()` e construtor publico parameterless; nao ha
  DI/factory nesta etapa.
- A instancia interna do converter nao e exposta na metadata publica.
- Nao foram feitas mudancas de API no prompt 10.7; dividas maiores foram
  documentadas.

## Read Conversion

Read conversion esta implementada para os caminhos controlados pelo FluentMap:
`QueryMapped*`, `ReadMapped*`, `QueryMultipleMapped`, unbuffered sincrono e
unbuffered assincrono. A precedencia real e:

```text
null/DBNull
    -> property read converter
    -> Dapper TypeHandler<TProperty>
    -> FluentMap default conversion
```

APIs Dapper puras (`Query<T>()`, `QuerySingle<T>()`) nao executam converters por
propriedade.

## Write Conversion

Write converter existe como contrato e metadata publica, mas nao e executado
por Dapper ou Dommel na Etapa 10. Dommel `Insert`/`Update` continuam passando a
entidade original ao Dapper, que aplica seu fluxo normal de parametrizacao.

## Dapper TypeHandler Interoperability

Valido explicitamente:

- property converter sem TypeHandler: runtime e generated aplicam converter de
  leitura nos cenarios suportados;
- TypeHandler sem converter: runtime `QueryMapped*` usa `TypeHandler<TProperty>`;
- converter + TypeHandler: property converter vence na propriedade configurada;
- converter por profile: profile selecionado usa seu converter proprio;
- nested converter: converter fica no member path terminal;
- immutable/value-object converter: conversao ocorre antes de construtor ou
  produz o Value Object escalar inteiro.

Regra conceitual documentada:

```text
TypeHandler -> comportamento por tipo
Property Converter -> comportamento por mapping/member/profile
```

## Profiles

Profiles permanecem query-scoped. Converters do map default nao vazam para
profiles. Reuso precisa ser explicito por configuracao do profile ou por
`IncludeBase<T>()` quando aplicavel.

## Nested and Value Objects

Converters se aplicam a folhas terminais de caminhos aninhados. Subarvores
totalmente `DBNull` continuam nao sendo criadas e seus converters nao rodam.
Value Objects escalares podem usar property converter; Value Objects por
componentes continuam usando construtores publicos compativeis.

## Runtime Materialization

`NestedMaterializationPlan` anexa conversion metadata a cada folha e cria um
delegate por folha no plano. A criacao/validacao fica fora do custo por linha;
o hot path executa leitura do `IDataRecord`, tratamento de null e chamada de
converter/default conversion.

## Generated Materialization

O source generator emite read converters quando o converter e por tipo,
acessivel, parameterless e implementa contrato compativel. O descriptor carrega
converter type, database type e property type para validar match com o mapping
efetivo. Instancias, delegates e padroes nao suportados usam runtime fallback.

## Diagnostics and Analyzers

- Runtime validation cobre metadata efetiva, direcoes inconsistentes,
  converter em propriedade ignorada e write converter em propriedade sem
  participacao de persistencia.
- Analyzer comum cobre `DFM014` para converter por tipo invalido e `DFM015`
  para converter direcional duplicado.
- Generator cobre `DFM012` para read converter gerado invalido e `DFM011` para
  fallback informativo.
- `Explain<T>()` expoe metadata estruturada em
  `MemberMappingExplanation.Conversion`.

## Dommel Integration

Dommel preserva persistence metadata da Etapa 8 para `SELECT`, `INSERT`,
`UPDATE`, generated, computed, identity, read-only e keys. Write converters sao
metadata-only e nao alteram `Insert`, `Update`, `InsertAll` ou variantes async.

## Performance

Benchmarks finais em 2026-07-29:

| Scenario | Mean | Allocated |
|---|---:|---:|
| Dapper/default conversion (`DapperPure`) | 2.071 ms | 283.22 KB |
| FluentMap sem converter (`QueryMappedRuntimeNoConverter`) | 1.536 ms | 142.55 KB |
| FluentMap runtime converter simples | 2.036 ms | 189.43 KB |
| FluentMap generated converter simples | 2.206 ms | 189.99 KB |
| FluentMap runtime property converter Value Object | 1.390 ms | 165.98 KB |
| FluentMap generated property converter Value Object | 1.421 ms | 166.55 KB |

BenchmarkDotNet alertou que as iteracoes ficaram abaixo de 100 ms. Use os
resultados como smoke representativo e evidencia de alocacao, nao como claim
formal de throughput.

## Native AOT / Trimming

Smoke trimming gerado:

- `dotnet publish ... -p:PublishTrimmed=true -p:DefineConstants=AOT_SMOKE_GENERATED`: sucesso;
- execucao do binario: `generated:ok`;
- warnings esperados: `IL2026` em `QueryMapped*` e `IL2104` em
  `Dapper.FluentMap`/`Dapper`.

Smoke Native AOT:

- `dotnet publish ... -p:PublishAot=true -p:DefineConstants=AOT_SMOKE_GENERATED`
  foi bloqueado pelo ambiente com `Platform linker not found`;
- antes do bloqueio, os warnings esperados `IL2026` e `IL3050` foram emitidos
  nas chamadas `QueryMapped*`.

Nao ha declaracao de compatibilidade Native AOT total.

## Backward Compatibility

- Nenhuma API publica existente foi removida.
- `IPropertyMap` permanece preservada; metadata vem por interface aditiva.
- Sem converter configurado, comportamento de Dapper puro, `QueryMapped*`,
  Dommel e generated materializers permanece compativel.
- Correcoes de validacao podem rejeitar configuracoes contraditorias que antes
  eram aceitas por acidente.

## Known Limitations

- Converters nao sao object mapper geral, serializer, SQL hook ou CRUD.
- Converter nao substitui `TypeHandler<T>` global do Dapper.
- Write converters nao executam em Dommel/Dapper nesta etapa.
- Nao ha DI, factory publica ou escopo por query para converters.
- Overloads por tipo exigem construtor publico parameterless.
- Generated converter cobre apenas converter type estaticamente visivel e
  suportado.
- TypeHandler no generated path permanece fora do escopo.
- `QueryMapped*` pode cair para runtime fallback e segue trimming/dynamic-code
  sensitive.
- Null conversion opt-in nao existe.
- Write profiles nao existem.

## Technical Debt

- Definir hook publico de parametro por propriedade antes de executar write
  conversion.
- Avaliar boundary publica para TypeHandlers no generated path sem depender de
  internals do Dapper.
- Formalizar compatibilidade binaria/API antes de release maior.
- Criar benchmarks mais longos para comparacao estatistica, se houver decisao
  de otimizacao.
- Investigar caminho generated-only/AOT-safe em etapa propria.

## Deferred Items

- Execucao de write converters em Dommel/Dapper.
- Parameter metadata (`DbType`, size, precision/scale, provider-specific).
- Factory/DI/scoped converter lifetime.
- Converter null opt-in.
- TypeHandler no generated path.
- Generated support para converter por instancia/delegate.
- Declaracao Native AOT alem dos smokes validados.

## Recommendations for Etapa 11

- Se a Etapa 11 tratar configuracao/DI, separar claramente de write conversion.
- Projetar configuration instances/scoped configuration antes de qualquer
  lifetime de converter por escopo.
- Para write conversion, decidir primeiro a API de parametro por propriedade e
  preservar a fronteira do core sem CRUD/SQL builder.
- Manter TypeHandler como mecanismo por tipo e evitar registry global paralelo.
- Adicionar API compatibility tooling antes de preparar pacote publico maior.

## Validation

Executado em 2026-07-29:

```bash
dotnet restore ./Dapper.FluentMap.sln
dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build
dotnet test ./test/Dapper.FluentMap.Dommel.Tests/Dapper.FluentMap.Dommel.Tests.csproj --configuration Release --no-build
```

Resultados:

- Restore: sucesso.
- Build Release: sucesso, 0 warnings, 0 errors.
- Solution tests: sucesso, 402 testes aprovados.
- Dommel tests isolados: sucesso, 22 testes aprovados.
- Benchmarks representativos: sucesso.
- Trimming smoke: sucesso com warnings esperados.
- Native AOT smoke: bloqueado pelo ambiente por ausencia do platform linker.
- Pack: nao executado no prompt 10.7; as alteracoes foram documentacao/SDD e
  nao alteraram empacotamento ou assemblies.
