# Etapa 10 - Conversion Landscape

## Discovery local

O FluentMap atual possui duas rotas de leitura com responsabilidades diferentes:

- `connection.Query<T>()` e APIs normais do Dapper usam `SqlMapper.SetTypeMap(...)`
  instalado por `FluentMapTypeMap`. O FluentMap resolve nomes de colunas,
  propriedades e parametros de construtor raiz; a conversao de valores continua
  sendo do Dapper e do provider ADO.NET.
- `QueryMapped*`, `ReadMapped*`, `QueryMultipleMapped` e streaming usam
  `IDataRecord` diretamente via `MappedRowMaterializer`. Nesse caminho o
  FluentMap controla materializacao flat, nested, Value Objects e profiles.

Pontos de conversao existentes:

- Runtime materializer: `NestedMaterializationPlan.CreateConverter(...)` usa
  `DapperTypeHandlerAdapter` quando ha `SqlMapper.TypeHandler<T>` para o tipo
  de destino; caso contrario aplica null/default, cast direto, enum,
  `Guid` de string e `Convert.ChangeType(..., InvariantCulture)`.
- Generated materializer: `MappingRegistrationGenerator.AppendReadHelper(...)`
  emite null/default, cast direto, enum, `Guid` de string e
  `Convert.ChangeType(...)`. Ele nao consulta `TypeHandler<T>`.
- Constructor mapping runtime: valores de folhas sao convertidos antes de
  entrar nos argumentos do construtor.
- Nested objects: a arvore aninhada e criada somente quando algum valor da
  subarvore nao e `DBNull`; conversao ocorre nas folhas terminais.
- Value Objects por componentes: sao materializados por construtores publicos
  compativeis, com conversao nas folhas componentes.
- Value Objects escalares: hoje devem preferir `TypeHandler<T>` do Dapper, e ha
  teste cobrindo `QueryMappedShouldUseDapperTypeHandlerForScalarValueObjectProperty`.
- Persistence metadata: descreve participacao em leitura/insert/update, mas nao
  converte valores.
- Dommel: a integracao atual controla resolucao de propriedades, colunas,
  chaves e SQL de insert/update por metadata. Ela nao transforma valores de
  parametros por propriedade.

Responsabilidades atuais:

```text
ADO.NET provider
    entrega valores CLR em IDataRecord.GetValue e aplica DbParameter.Value

Dapper
    converte no caminho Query<T>, usa TypeHandler<T> global por tipo e cria
    parametros em Execute/operacoes usadas por Dommel

FluentMap
    resolve membros/colunas e, nos caminhos QueryMapped*, converte valores
    lidos para propriedades/construtores

Application converter
    ainda nao existe como contrato FluentMap por propriedade/profile
```

## Dapper TypeHandler

Fonte: https://github.com/DapperLib/Dapper/blob/main/Dapper/SqlMapper.TypeHandler.cs

### Escopo

`SqlMapper.TypeHandler<T>` e um mecanismo global por tipo CLR registrado no
Dapper. O contrato possui duas direcoes:

- `Parse(object value)` para transformar um valor vindo do banco em `T`;
- `SetValue(IDbDataParameter parameter, T value)` para configurar parametros.

Ele e excelente quando a representacao de um tipo e uniforme em toda a
aplicacao, por exemplo `Cpf` sempre vindo de uma coluna `VARCHAR`.

### Pontos fortes

- Integracao nativa com Dapper.
- Um unico registro atende leitura e parametros onde o Dapper invoca handlers.
- Bom para Value Objects escalares com representacao global.
- Nao exige que FluentMap replique conversao por tipo.

### Limitacoes para FluentMap

- O escopo e global por tipo, nao por propriedade, entity map ou profile.
- Duas propriedades do mesmo tipo nao conseguem usar representacoes diferentes.
- O contrato nao sabe qual member path, entidade, profile ou coluna esta sendo
  convertida.
- No FluentMap atual, o generated materializer nao consulta `TypeHandler<T>`;
  portanto runtime e generated podem divergir em Value Objects escalares.
- Para escrita via Dommel, `TypeHandler<T>` continua util, mas nao resolve o
  caso property-scoped quando duas propriedades do mesmo tipo exigem formatos
  diferentes.

## RepoDB PropertyHandler

Fontes:

- https://repodb.net/feature/propertyhandlers
- https://repodb.net/interface/ipropertyhandler
- https://repodb.net/reference/propertyhandlerpropertylevel
- https://repodb.net/reference/propertyhandlertypelevel

### Escopo

RepoDB possui `IPropertyHandler<TInput, TOutput>` para transformacao entre tipo
de coluna e tipo da propriedade. A documentacao separa uso property-level e
type-level. O handler recebe valor e contexto da propriedade, e possui duas
operacoes conceituais:

- `Get(...)` em leitura/hidratacao;
- `Set(...)` antes de escrita.

### Pontos fortes

- Resolve diretamente o problema de conversao por propriedade.
- Permite inbound/outbound e contexto da propriedade.
- Pode ser aplicado por atributo ou via fluent mapping.
- A semantica de propriedade tem menos surpresa que um handler global por tipo.

### Limitacoes para FluentMap

- RepoDB e uma biblioteca com CRUD/SQL generation mais amplo; FluentMap nao deve
  importar esse escopo.
- A API de contexto e lifecycle do RepoDB nao se transfere diretamente para
  Dapper/FluentMap.
- O FluentMap deve preservar modelos sem atributos como caminho principal.
- O uso type-level do RepoDB se aproxima de `TypeHandler<T>`; no FluentMap isso
  deve continuar pertencendo ao Dapper, salvo configuracoes explicitamente
  property/profile-scoped.

## EF Core ValueConverter

Fontes:

- https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions
- https://github.com/dotnet/efcore/blob/main/src/EFCore/Storage/ValueConversion/ValueConverter.cs

### Escopo

EF Core `ValueConverter` converte entre `ModelClrType` e `ProviderClrType`.
O modelo e definido por propriedade no metadata do EF. O contrato usa expressoes
para:

- converter do modelo para o provider em escrita;
- converter do provider para o modelo em leitura.

EF tambem modela hints, composicao e comportamento de nulls.

### Pontos fortes

- Direcoes explicitas.
- Tipos de modelo/provider claros.
- Integra bem com geracao/compilacao de delegates.
- A semantica de null e parte do contrato.
- O design e reconhecido por usuarios .NET para DDD Value Objects e enums.

### Limitacoes para FluentMap

- EF Core e um ORM completo; FluentMap nao deve adotar conceitos de tracking,
  model builder, migrations, comparers ou schema facets.
- Expression trees sao boas para composicao EF, mas podem aumentar superficie
  publica, trimming e complexidade no FluentMap.
- Converter bidirecional obrigatorio seria restritivo para cenarios read-only ou
  write-only.
- Converter de provider/model nao deve virar um serializer framework generico.

## Outros sinais relevantes

Dommel usa Dapper para execucao e mapping e expoe extensibility para nomes de
tabela, colunas, chaves, propriedades e SQL builder. A integracao FluentMap
atual aproveita esses pontos para metadata de persistencia, mas nao possui um
hook local por propriedade para alterar `DbParameter.Value`.

O historico de issues de Dapper/Dommel mostra que handlers globais podem ser
confusos em bibliotecas que geram SQL/parametros. A Etapa 10 deve evitar uma
promessa de escrita property-scoped antes de haver uma integracao testada com a
geracao de parametros.

## Conclusao para FluentMap

FluentMap deve adicionar property conversion como metadata do mapping, nao como
outro registry global por tipo. A fronteira recomendada e:

```text
Property/profile converter configurado explicitamente
    cobre a direcao configurada para aquele member path

Dapper TypeHandler<T>
    continua sendo o mecanismo global por tipo e fallback preferido para
    Value Objects escalares sem conversor por propriedade

Conversao padrao do FluentMap/Dapper/provider
    preserva comportamento atual quando nao ha converter
```

O primeiro incremento deve focar metadata/contracts e read conversion no
`QueryMapped*` runtime. Generated read conversion, profile hardening e write
conversion/Dommel devem entrar em incrementos separados para preservar
compatibilidade e testar equivalencia.
