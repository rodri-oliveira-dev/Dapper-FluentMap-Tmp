# Etapa 10 - Conversion Diagnostics & Hardening

## Objetivo do Prompt 10.6

Consolidar property converters sem adicionar uma segunda geracao de features.
O foco deste incremento e separar:

- erros comprovaveis em compile time;
- validacao runtime da configuracao efetiva;
- diagnosticos gerados pelo source generator;
- limites documentados de extensibilidade, concorrencia e Dommel/write.

Analyzers nao executam construtores de maps. `FluentMapper.Validate()` continua
sendo a validacao da composicao efetiva registrada em runtime.

## Diagnostic specification

| Condition | Compile time | Runtime validation | Severity | Diagnostic |
| --------- | ------------ | ------------------ | -------- | ---------- |
| incompatible converter por tipo, sem contrato direcional compativel | Sim, quando a fluent chain usa `Convert...Using<TConverter, TDatabase>()` diretamente | Sim, durante construcao do map e validacao da metadata efetiva | Error | `DFM014` no analyzer comum; `DFM012` no generator quando o materializer gerado consegue provar read converter invalido |
| duplicate read converter no mesmo property map | Sim, para fluent chain direta no construtor | Sim, pela fluent API/metadata | Error | `DFM015` |
| duplicate write converter no mesmo property map | Sim, para fluent chain direta no construtor | Sim, pela fluent API/metadata | Error | `DFM015` |
| inaccessible converter para generated materializer | Parcial. O generator emite fallback quando nao pode referenciar o converter de forma segura | Nao e erro runtime; runtime fallback permanece suportado | Info | `DFM011` quando o map usa fallback generated; sem erro porque o runtime suporta instancia/delegate/inacessivel |
| invalid generic arguments em `AddMap<TMap>()` | Sim | Sim, quando a configuracao registra o map | Error | `DFM005` |
| invalid generic arguments em `AddProfile<TMap>()` | Sim | Sim, quando a configuracao registra o profile | Error | `DFM009` |
| contradictory inherited configuration | Parcial. `IncludeBase<TBase>()` invalido e comprovavel e `DFM004`; conflitos efetivos dependem de registro/ordem | Sim, na composicao efetiva de base/derivado/profile | Error | `DFM004` quando estatico; `FluentMapConfigurationException` em runtime |
| invalid profile configuration ou profile duplicado | Sim para `AddProfile<TMap>()` invalido/duplicado no mesmo metodo de configuracao | Sim, no registro do profile | Error | `DFM009`, `DFM010` |
| impossible null conversion | Nao nesta etapa. Null/`DBNull` nao sao enviados ao converter por contrato, e NRT nao e contrato runtime em `netstandard2.0` | Parcial. Converter que retorna `null` para target value type nao nullable falha na materializacao com contexto | Error | `FluentMapConfigurationException` na materializacao; sem ID estatico confiavel |
| write converter em propriedade ignorada | Parcial, quando a chain direta contem `Ignore()` e write converter | Sim, porque a propriedade nunca participa de persistencia | Error | `DFM013` quando provado como comportamento de persistencia; `FluentMapConfigurationException` em runtime |
| write converter em propriedade read-only/computed nao chave e nunca persistida | Nao confiavel quando persistence metadata vem de map externo/base/profile | Sim, quando a metadata efetiva nao participa de insert, update nem key persistence | Error | `FluentMapConfigurationException` em runtime |
| write converter em Dommel `Insert`/`Update` esperando execucao property-scoped | Nao | Nao e erro por si; nesta etapa e metadata-only e a execucao Dommel preserva valores originais | Info/limite documentado | Sem diagnostic automatico; documentado em `07-write-conversion.md` |

## Analyzer IDs

Analyzer comum (`src/Dapper.FluentMap.Analyzers`):

- `DFM001`: expressao `Map(...)` invalida.
- `DFM002`: member path duplicado no construtor do map.
- `DFM003`: coluna duplicada no construtor do map.
- `DFM004`: `IncludeBase<TBase>()` nao aponta para base class valida.
- `DFM005`: `AddMap<TMap>()` generico invalido.
- `DFM009`: `AddProfile<TMap>()` generico invalido.
- `DFM010`: profile duplicado no mesmo metodo de configuracao.
- `DFM013`: comportamento de persistencia contraditorio.
- `DFM014`: property converter por tipo invalido.
- `DFM015`: property converter direcional duplicado na mesma fluent chain.

Source generator (`src/Dapper.FluentMap.Generators`):

- `DFM011`: fallback runtime para materializer gerado.
- `DFM012`: read converter gerado invalido.

`DFM012` fica reservado ao generator para evitar duas regras diferentes com o
mesmo ID quando analyzer comum e generator estiverem instalados juntos.

## Runtime validation

`FluentMapper.Validate()` e o registro de maps validam a metadata efetiva depois
de aplicar explicit maps, `IncludeBase<TBase>()`, profiles e conventions.

Validacao adicionada:

- conversion metadata nula em `IPropertyMapWithConversionMetadata` externo;
- descriptor de converter nulo ou com direcao inconsistente;
- read converter em propriedade ignorada;
- write converter em propriedade ignorada;
- write converter em propriedade que nao participa de insert, update nem key
  persistence.

As mensagens runtime seguem o padrao existente:
`FluentMapConfigurationException` com entity, member path, origem do map e uma
razao curta. O texto e diagnostico, nao contrato publico de mensagem exata.

## Explain

`Explain<TEntity>()` e `Explain<TEntity, TProfile>()` ja expõem o converter
efetivo de forma estruturada em `MemberMappingExplanation.Conversion`.

Exemplo de leitura estavel:

```csharp
var status = FluentMapper.Explain<Customer, LegacyProfile>()
    .Members.Single(member => member.MemberPath == nameof(Customer.Status));

var converterType = status.Conversion.ReadConverter.ConverterType;
var databaseType = status.Conversion.ReadConverter.DatabaseType;
var source = status.Source;
```

Nao foi alterado `MappingExplanation.ToString()` neste incremento. O formato de
texto e util para orientacao rapida, mas a superficie estavel para diagnostics e
ferramentas e a API estruturada.

## Extensibility review

Consumers conseguem criar converters proprios sem depender de internals:

- `IReadPropertyConverter<TDatabase, TProperty>`;
- `IWritePropertyConverter<TProperty, TDatabase>`;
- `IPropertyConverter<TDatabase, TProperty>`;
- overloads por tipo, instancia e delegate na fluent API;
- metadata publica read-only em `PropertyConversionMetadata` e
  `PropertyConverterMetadata`.

Nao foi exposta a instancia interna do converter nem detalhes de materializacao.
Isso preserva a fronteira publica: consumers implementam contratos, configuram
maps e inspecionam metadata; o runtime decide como executar.

## Concurrency

Converters continuam com contrato de reuso:

- converter por tipo: uma instancia por property map runtime; no generated path,
  campo estatico por binding gerado;
- converter por instancia/delegate: a instancia/delegate fornecida pelo usuario
  e reutilizada;
- nao ha escopo por query nesta etapa.

Documentacao XML agora declara que implementacoes devem ser stateless ou
thread-safe. Testes de hardening cobrem concorrencia em runtime materializer,
generated materializer e profiles.

## Regression hardening

Categorias cobertas ou reforcadas:

- mesmo tipo de propriedade com converters diferentes;
- mesma entidade com converters diferentes em default map/profile;
- nested properties com mesmo terminal member name;
- converter + nullable;
- converter + constructor;
- converter + `TypeHandler`;
- equivalencia runtime/generated;
- assimetria read/write;
- Dommel mantendo write converter como metadata-only.

## Performance interpretation

Benchmarks da etapa 10 continuam representativos, nao estatisticos formais. O
custo esperado de converter e:

- fixo: criacao/validacao de metadata, plano runtime ou campo estatico gerado;
- por linha: chamada de delegate/interface e conversao do valor bruto para o
  `TDatabase` declarado quando necessario.

Nenhuma otimizacao foi feita antes de medida. O objetivo de 10.6 e garantir que
o custo permanece visivel e que regressao funcional/concurrency seja detectada.
