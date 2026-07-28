# Etapa 10 - Property Conversion Specification

## Objetivos

- Permitir conversao configuravel por propriedade/member path.
- Permitir conversao especifica por mapping profile.
- Modelar leitura e escrita como direcoes independentes.
- Preservar `TypeHandler<T>` do Dapper como mecanismo global por tipo.
- Manter compatibilidade publica e comportamento atual quando nenhum converter
  estiver configurado.
- Suportar runtime materialization primeiro e generated materialization depois,
  com equivalencia testavel.
- Preparar metadata suficiente para Dommel/write sem prometer escrita antes do
  hook de parametros estar validado.

## Nao objetivos

- Serializer framework generico.
- Object mapper geral.
- Substituto de AutoMapper.
- SQL generator no core.
- Query builder.
- ORM.
- Repository.
- Unit of Work.
- Change tracking.
- Schema conversion.
- Migrations.
- DI container obrigatorio.
- Substituir `SqlMapper.TypeHandler<T>`.

## Read conversion

Read conversion transforma o valor do banco/provider para o tipo da propriedade
ou parametro de construtor:

```text
Database/provider CLR value -> Property CLR value
```

No `QueryMapped*`, o valor de entrada e o resultado de `IDataRecord.GetValue`.
O converter nao deve receber a linha inteira por padrao; isso preserva o foco em
property conversion e evita graph aggregation.

Precedencia proposta para `QueryMapped*`:

```text
DBNull/null handling padrao, salvo opt-in explicito de converter de null
    -> property read converter, se configurado
    -> Dapper TypeHandler<TProperty>, se existente e aplicavel
    -> conversao padrao atual do FluentMap
```

Para `connection.Query<T>()`, o FluentMap nao controla read conversion
property-scoped. Essa rota continua sendo Dapper + `TypeHandler<T>` + provider.

## Write conversion

Write conversion transforma o valor de propriedade para o valor de parametro:

```text
Property CLR value -> Database/provider CLR value
```

Ela deve ser independente de read conversion. Um mapping pode ter apenas read
converter, apenas write converter, ou ambos.

Primeira regra de design: o core pode guardar metadata de escrita, mas a
execucao inicial deve ser feita somente em integracoes com ponto real de
parametrizacao testado. Dommel exige investigacao/implementacao propria porque
a integracao atual controla colunas e SQL builders, mas nao transforma valores
de parametros por propriedade.

## Property-scoped conversion

Um converter configurado em `Map(x => x.Status)` pertence ao member path
efetivo daquele property map. Ele nao se aplica a outras propriedades do mesmo
tipo.

Exemplo conceitual:

```csharp
Map(x => x.LegacyStatus)
    .ToColumn("legacy_status")
    .ConvertFromDatabaseUsing<LegacyStatusReader, string>()
    .ConvertToDatabaseUsing<LegacyStatusWriter, string>();

Map(x => x.CurrentStatus)
    .ToColumn("status")
    .ConvertFromDatabaseUsing<CurrentStatusReader, int>();
```

O tipo de banco/provider (`string`, `int`, etc.) deve ser parte do contrato para
evitar conversores baseados em `object` em APIs principais.

## Profile-scoped conversion

Profiles ja sao maps separados para a mesma entidade sob shapes SQL diferentes.
Um converter configurado em um map que implementa `IProfileMap<TProfile>` deve
ser valido somente para aquele profile.

Precedencia por profile:

```text
profile property converter
    -> profile property mapping sem converter
    -> default entity map/convention somente quando a operacao nao seleciona profile
```

Nao deve haver vazamento automatico de converter do default map para profile.
Se reutilizacao for desejada, ela deve ser explicita via `IncludeBase<T>()` ou
API futura bem definida.

## Global TypeHandler interoperability

`TypeHandler<T>` permanece o mecanismo recomendado quando todo o tipo `T` tem a
mesma representacao no banco.

Um property converter configurado explicitamente deve ter precedencia sobre
`TypeHandler<T>` somente na direcao configurada e somente no caminho controlado
pelo FluentMap.

Sem converter:

- runtime `QueryMapped*` deve continuar consultando `TypeHandler<T>` antes da
  conversao padrao;
- generated materialization deve ganhar uma decisao explicita: ou emite chamada
  segura ao contrato publico de conversao do FluentMap, ou recusa generated
  materializer para tipos que dependem de `TypeHandler<T>`.

## Null semantics

Default recomendado:

- `DBNull` e `null` nao sao enviados ao converter.
- propriedades nullable/reference recebem `null`;
- propriedades value type nao nullable recebem `default(T)`, preservando o
  comportamento atual.

Deve existir uma decisao futura, talvez `ConvertsNulls`, somente se houver caso
real. Converter de null aumenta risco de comportamento divergente entre
runtime/generated e pode quebrar suposicoes de nested subtree null.

## Nullable<T>

O matching de tipos deve considerar `Nullable<T>` e `T` como compativeis para
selecao de converter, mas a semantica de null deve continuar externa ao
converter por default.

Um read converter para `TDatabase -> TProperty` pode alimentar `TProperty?`
quando `TProperty` for value type, desde que o resultado seja atribuivel.

## Value objects

Dois cenarios devem permanecer distintos:

- Value Object escalar mapeado como propriedade inteira:
  `Map(x => x.Cpf).ToColumn("cpf")`. Use `TypeHandler<Cpf>` quando a
  representacao for global; use property converter quando a representacao
  variar por propriedade/profile.
- Value Object por componentes:
  `Map(x => x.Cpf.Number).ToColumn("cpf")`. O converter se aplica ao componente
  terminal, nao ao Value Object inteiro, salvo API futura explicita para
  converter subarvore completa.

Factory methods continuam fora do escopo da Etapa 10. Conversores nao devem ser
usados para contornar uma materializacao de Value Object aninhado sem design.

## Nested mappings

Converters se aplicam a folhas terminais (`Address.City`, `Rank.Level`), nao a
objetos intermediarios. A criacao/null de subarvores aninhadas continua regida
por `HasNonNullValue` nos ordinais da subarvore.

Se todos os valores de uma subarvore sao `DBNull`, o converter de folha nao e
executado por default.

## Constructor mapping

Runtime constructor mapping deve converter folhas antes de montar os argumentos
do construtor.

Falhas de converter em argumentos de construtor devem ser encapsuladas com
contexto de entity type, member path, coluna e construtor, preservando a inner
exception.

Constructor matching nao deve depender do tipo de banco do converter. Ele deve
continuar usando o tipo da propriedade/member path para determinar se o
construtor e compativel.

## Generated materialization

Generated materializers so devem ser emitidos quando a cadeia de conversao for
deterministica e referenciavel em codigo gerado.

Requisitos:

- o descriptor gerado deve incluir metadata de conversao suficiente para validar
  que o materializer ainda corresponde ao mapping efetivo;
- conversores por tipo sem construtor publico ou instancia conhecida devem
  causar fallback runtime, nao codigo quebrado;
- runtime e generated devem compartilhar a mesma semantica de null, enum,
  `Guid`, `TypeHandler<T>` e property converter.

Direcao preferida para evitar duplicacao: extrair um runtime helper publico ou
internal-with-generator-contract para leitura de valor que o generated code
possa chamar.

## Runtime materialization

O runtime deve anexar `PropertyConversionMetadata` a cada leaf durante a criacao
do `NestedMaterializationPlan`.

A chave de cache de materializacao hoje e `entity + profile + ordered columns`.
Como os caches sao invalidados ao registrar maps/conventions, nao e necessario
incluir converter na chave se metadata e imutavel depois de registrado. Se a
API permitir mutacao posterior, o cache precisara incluir versao/configuracao
ou impedir mutacao depois do registro.

## Persistence / Dommel integration

Property write conversion deve ser exposta como metadata no core, mas executada
somente onde ha controle de parametro.

Para Dommel, a implementacao deve responder antes de codificar:

- Dommel 3.5.3 permite interceptar valor de parametro por propriedade via API
  publica?
- Se nao permite, o FluentMap deve criar wrapper de parametro, `DynamicParameters`
  ou caminho proprio de SQL?
- Como preservar builders customizados registrados depois de `ForDommel()`?
- Como garantir que `TypeHandler<T>` global ainda seja usado quando nao ha
  write converter?

Sem essas respostas, a Etapa 10 nao deve declarar write conversion completa.

## Error behavior

Erros devem ser diagnosticos, deterministas e preservar inner exception.

Read conversion deve falhar com `FluentMapConfigurationException` quando:

- converter nao implementa a direcao requerida;
- tipo de entrada/saida e incompativel com coluna/propriedade;
- instancia do converter nao pode ser criada por API configurada;
- conversao lanca excecao.

Mensagens devem incluir entidade, profile quando houver, member path, coluna,
direcao e converter type.

## Diagnostics

`Explain<TEntity>()` e `Explain<TEntity, TProfile>()` devem expor conversao de
forma aditiva, por exemplo:

- read converter type;
- write converter type;
- provider/database CLR type declarado;
- null handling;
- source: explicit/profile/inherited/convention quando aplicavel.

Analyzers devem evoluir gradualmente:

- reconhecer fluent chains com `Convert...`;
- detectar converter sem contrato esperado;
- detectar uso de API unsupported pelo generator;
- alertar quando generated materializer fara fallback por converter nao
  estaticamente suportado, se esse diagnostico for util.

## Trimming

APIs baseadas em `ConvertUsing<TConverter>()` com ativacao por reflection exigem
anotacoes de trimming para construtor publico. APIs por instancia ou delegate
sao mais amigaveis para trimming.

Direcao recomendada:

- oferecer overload por instancia/factory como caminho AOT-friendly;
- permitir overload generico com constraints e anotacoes claras;
- documentar que assembly scanning continua sensivel a trimming.

## Native AOT

Native AOT nao deve depender de `Expression.Compile()` ou reflection tardia para
conversao generated.

Estrategia:

- runtime fallback permanece anotado como trimming/dynamic-code sensitive;
- generated materializer pode ser AOT-friendly somente quando todos os
  conversores sao referenciaveis estaticamente e nao exigem ativacao dinamica;
- `TypeHandler<T>` interop no generated path precisa evitar reflexao sobre
  internals do Dapper, ou cair para runtime fallback.

## Thread safety

Converters devem ser tratados como stateless e thread-safe por contrato.
Instancias podem ser reutilizadas entre materializacoes concorrentes.

Se a API aceitar instancias stateful, a documentacao deve dizer que o usuario e
responsavel por thread safety. O FluentMap nao deve criar escopos por query na
primeira versao.

## Converter lifetime

Decisao proposta para o primeiro incremento:

- converter type com construtor publico parameterless: uma instancia por
  property map registrado;
- converter instance fornecida pelo usuario: a propria instancia e reutilizada;
- sem DI container no core;
- factory/DI fica adiado como extensibilidade futura.

Isso reduz allocations e mantem startup/configuration como ponto de validacao.

## Backward compatibility

Sem converter configurado:

- Dapper `Query<T>()` deve se comportar exatamente como hoje;
- `QueryMapped*` runtime deve preservar null/default, enum, `Guid`,
  `Convert.ChangeType` e `TypeHandler<T>`;
- generated materializers atuais devem continuar validos ou cair para runtime
  fallback quando nova metadata tornar o descriptor insuficiente;
- `IPropertyMap` nao deve ser quebrada. Metadata nova deve vir por interface
  aditiva.

## Performance

O custo por linha deve ser proximo de uma chamada de delegate apos o plano ser
criado. Resolucao de converter, validacao de tipos e criacao de instancias deve
acontecer no registro ou na criacao do plano, nao em cada valor.

Generated materialization deve conseguir inline/chamar helpers sem alocacoes por
coluna. Delegates por leaf sao aceitaveis no runtime fallback.

Benchmarks devem comparar:

- sem converter;
- converter simples read;
- converter simples write quando houver Dommel;
- TypeHandler global;
- generated vs runtime fallback;
- nested/value object/profile.
