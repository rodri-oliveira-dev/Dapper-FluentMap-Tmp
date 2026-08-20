# Etapa 10 - Architectural Decisions

## ADR-1 - Property converter vs Dapper TypeHandler

### Contexto

Dapper ja possui `SqlMapper.TypeHandler<T>` com leitura e escrita globais por
tipo. A lacuna do FluentMap e permitir representacoes diferentes para
propriedades/profile do mesmo tipo.

### Decisao

Property converters serao metadata de mapping por member path/profile. Eles nao
substituem `TypeHandler<T>` e nao serao registrados globalmente por tipo.

### Alternativas consideradas

- Criar um registry global `Status -> converter`: rejeitado por duplicar
  `TypeHandler<T>`.
- Usar apenas `TypeHandler<T>`: insuficiente para propriedades do mesmo tipo com
  representacoes diferentes.
- Adotar modelo amplo de ORM: fora do escopo do FluentMap.

### Consequencias

Consumidores continuam usando `TypeHandler<T>` para representacao uniforme.
Property converters ficam reservados para variacao local e podem ser explicados
em diagnostics por propriedade/profile.

## ADR-2 - Read vs write conversion

### Contexto

Nem todo converter e bidirecional. Um sistema pode ler legado sem escrever no
mesmo formato, ou escrever representacao customizada sem participar de
materializacao avancada.

### Decisao

Leitura e escrita serao modeladas como direcoes independentes. Um mapping pode
configurar somente read, somente write ou ambos.

### Alternativas consideradas

- Converter bidirecional obrigatorio: simples, mas forca implementacoes falsas.
- Delegates livres sem direcao: pouco discoverable e fraco para diagnostics.

### Consequencias

A API precisa ter nomes claros para cada direcao. Validacao deve falhar somente
quando uma operacao tenta usar uma direcao ausente.

## ADR-3 - Converter contract

### Contexto

O contrato precisa ser type-safe, eficiente, diagnosticavel e viavel para
source generation/AOT.

### Decisao

Modelo preferido:

```csharp
public interface IReadPropertyConverter<TDatabase, TProperty>
{
    TProperty ConvertFromDatabase(TDatabase value);
}

public interface IWritePropertyConverter<TProperty, TDatabase>
{
    TDatabase ConvertToDatabase(TProperty value);
}

public interface IPropertyConverter<TDatabase, TProperty> :
    IReadPropertyConverter<TDatabase, TProperty>,
    IWritePropertyConverter<TProperty, TDatabase>
{
}
```

Delegates podem existir como overload ergonomico, mas o metadata canonico deve
ser descritor tipado com direcao.

### Alternativas consideradas

- `IPropertyConverter<TSource, TDestination>` unico: ambiguo em APIs
  bidirecionais.
- `object Convert(object value, context)`: flexivel, mas perde type safety e
  piora generated/AOT.
- Expression trees estilo EF: poderosas, mas mais pesadas que o FluentMap
  precisa nesta etapa.

### Consequencias

APIs ficam mais verbosas, porem claras. O generator consegue identificar
contratos e tipos. Contexto extra fica adiado para uma versao futura.

## ADR-4 - Converter lifetime

### Contexto

FluentMap e configurado globalmente no startup e depois deve ser tratado como
read-only. Converters podem ser usados em consultas concorrentes.

### Decisao

Converters sao stateless/thread-safe por contrato. Instancias genericas sao
criadas no registro/plano e reutilizadas. Overloads por instancia reutilizam a
instancia fornecida pelo usuario.

### Alternativas consideradas

- Nova instancia por linha: rejeitado por custo.
- DI scoped por query: adiado; introduz escopo/configuration instances.
- Singleton global por tipo de converter: possivel, mas uma instancia por
  property map simplifica futuras configuracoes locais.

### Consequencias

Conversores stateful sao responsabilidade do consumidor. O core nao ganha
dependencia de DI.

## ADR-5 - Precedence

### Contexto

O comportamento atual ja possui explicit mapping, convention e Dapper default
para nomes; e TypeHandler/global/default para valores no runtime mapped.

### Decisao

Para `QueryMapped*`:

```text
property read converter
    -> Dapper TypeHandler<TProperty>
    -> FluentMap default conversion
```

Null/`DBNull` continuam tratados antes do converter por default.

Para escrita futura:

```text
property write converter
    -> Dapper TypeHandler<TProperty>
    -> Dapper/provider parameter default
```

Somente a direcao configurada ganha precedencia.

### Alternativas consideradas

- TypeHandler antes do property converter: surpreendente, pois um mapping
  explicito local nao teria efeito.
- Property converter global por tipo antes de TypeHandler: rejeitado por duplicar
  Dapper.

### Consequencias

Converter local tem prioridade previsivel, mas nao altera Dapper puro nem outras
propriedades do mesmo tipo.

## ADR-6 - Profile-scoped converters

### Contexto

Profiles ja representam shapes SQL alternativos para a mesma entidade.

### Decisao

Conversores configurados em `IProfileMap<TProfile>` aplicam somente aquele
profile. Nao ha heranca automatica de converters do default map para profiles.

### Alternativas consideradas

- Converter default herdado por profile: reduz repeticao, mas cria surpresa em
  profiles legados.
- Registry separado por profile: adiado ate haver necessidade real alem dos
  property maps de profile.

### Consequencias

Cada profile declara sua representacao explicitamente. Reuso depende de base
maps/inclusao ou API futura.

## ADR-7 - Runtime vs generated execution

### Contexto

Etapa 9 consolidou dispatch generated-then-runtime por shape. O generated
materializer atual replica conversao default e nao usa TypeHandler.

### Decisao

Runtime sera o primeiro executor suportado para read converters. Generated
materialization so deve emitir converter quando conseguir validar metadata e
referenciar o conversor de forma deterministica; caso contrario deve cair para
runtime fallback.

### Alternativas consideradas

- Implementar runtime e generated juntos: alto risco de divergencia.
- Sempre desabilitar generated quando houver converter: seguro, mas perde
  beneficio em casos simples e AOT-friendly.
- Fazer generator instanciar qualquer converter por reflection: rejeitado para
  AOT/trimming.

### Consequencias

O plano incremental precisa de testes de equivalencia antes de declarar suporte
generated completo.

## ADR-8 - Dommel/write integration

### Contexto

Dommel gera SQL e parametros usando Dapper. A integracao atual do FluentMap
filtra propriedades/colunas, mas nao transforma valores por propriedade.

### Decisao

Write conversion fica especificada e descrita em metadata no core, mas a
execucao Dommel sera incremento separado. Nao declarar suporte completo ate
validar um hook de parametro por propriedade.

### Alternativas consideradas

- Confiar em `TypeHandler<T>` para escrita: resolve apenas global por tipo.
- Gerar SQL proprio no core: fora do escopo.
- Reescrever Dommel: fora do escopo.

### Consequencias

Etapa 10 deve separar read conversion de write conversion. O risco de Dommel
fica visivel e testavel.

## ADR-9 - Error semantics

### Contexto

Conversores de aplicacao podem falhar por dados invalidos, configuracao
incompativel ou regras de dominio.

### Decisao

Falhas de configuracao e execucao de converter no caminho FluentMap devem virar
`FluentMapConfigurationException` com inner exception preservada e contexto de
entity/profile/member/column/direction/converter.

### Alternativas consideradas

- Propagar excecao original sem contexto: diagnostico ruim.
- Criar nova hierarquia publica de excecoes: adiado; aumenta superficie.

### Consequencias

Mensagens devem ser uteis, mas testes nao devem depender de texto completo sem
necessidade.

## ADR-10 - Native AOT strategy

### Contexto

O projeto ja diferencia registro explicito/generated de reflection scanning e
mantem `QueryMapped*` anotado por fallback runtime.

### Decisao

Property converters nao tornam `QueryMapped*` AOT-safe por si. O caminho
AOT-friendly deve exigir converter instance/delegate ou tipo estaticamente
referenciavel no generated materializer. Ativacao por reflection deve ser
anotada ou causar fallback.

### Alternativas consideradas

- Declarar AOT-safe apos compilar: rejeitado.
- Exigir somente delegates: bom para AOT, mas pior discoverability e analyzer.
- Exigir somente converter type: simples, mas mais trimming-sensitive.

### Consequencias

Documentacao deve separar "supported at runtime" de "generated/AOT-friendly".
Smoke AOT deve entrar somente quando houver implementacao generated.

## ADR-11 - Prompt 10.2 converter metadata increment

### Contexto

O Prompt 10.2 pediu infraestrutura minima para representar conversoes por
propriedade sem espalhar execucao por toda a biblioteca.

### Decisao

Foram implementados contratos publicos tipados, overloads fluent por tipo,
instancia e delegate, metadata aditiva em `PropertyMap` e diagnostics via
`Explain`. A execucao de conversores em runtime materializer, generated
materializer e escrita Dommel ficou fora deste incremento.

Generated materializers manuais nao sao selecionados quando o effective mapping
da coluna possui read converter, porque o descriptor atual nao declara nem
aplica conversao de leitura. Write-only converter nao bloqueia materializer de
leitura.

### Consequencias

O projeto passa a conseguir validar e inspecionar converters por propriedade,
profile e heranca, mantendo comportamento de valor inalterado ate a etapa que
implementar execucao. A API e aditiva e preserva `IPropertyMap`.

## ADR-12 - Prompt 10.3 runtime read conversion

### Contexto

O Prompt 10.3 pediu que o runtime materializer aplicasse read converters
configurados por propriedade sem duplicar indevidamente `TypeHandler<T>` do
Dapper e sem espalhar logica pelas APIs `QueryMapped*`, `ReadMapped*` e
streaming.

### Decisao

Read converters por propriedade executam em `NestedMaterializationPlan`, na
folha terminal que le `IDataRecord.GetValue`. A precedencia efetiva e:

```text
null/DBNull handling
    -> property read converter
    -> Dapper TypeHandler<TProperty>
    -> FluentMap default conversion
```

Quando um property converter existe para a folha, o `TypeHandler<TProperty>` nao
e chamado para aquela propriedade. Sem converter, o caminho antigo com
`TypeHandler<TProperty>` e conversao padrao permanece.

### Consequencias

Todas as APIs que usam `MappedRowMaterializer` compartilham a mesma semantica:
`QueryMapped`, `QueryMultipleMapped`/`ReadMapped`, unbuffered sincrono e
streaming assincrono. Generated materializers continuam caindo para runtime
fallback quando o mapping efetivo possui read converter. Escrita/Dommel e
execucao generated de converters permanecem incrementos separados.

## ADR-13 - Prompt 10.4 generated read conversion

### Contexto

O runtime materializer ja executava read converters por propriedade, mas o
generated materializer recusava qualquer mapping efetivo com read converter e
caia para runtime fallback. Isso preservava corretude, mas impedia o beneficio
generated em cenarios simples e AOT-friendly.

### Decisao

Generated materializers passam a emitir read conversion somente quando o
converter e por tipo, acessivel ao codigo gerado, possui construtor publico
parameterless e implementa um contrato `IReadPropertyConverter<TDatabase,
TProperty>` compativel com o member path.

O codigo gerado usa um campo estatico por binding de coluna/converter e chama
um helper generico fortemente tipado. O descriptor de coluna gerado declara
tipo do converter, tipo de banco/provider e tipo de propriedade retornado pelo
converter. O registry so seleciona o descriptor quando essa metadata coincide
com o mapping efetivo do default map ou profile selecionado.

Converters por instancia/delegate e converters inacessiveis para o codigo
gerado continuam usando runtime fallback.

### Consequencias

O caminho gerado passa a cobrir scalar, nullable, nested, immutable constructor,
Value Object escalar e profiles com property read converter, sem reflection por
linha nem `Activator.CreateInstance` no hot path. A semantica de null usa o tipo
alvo real da propriedade/parametro, preservando equivalencia para casos como
`IReadPropertyConverter<string, int>` aplicado a `int?`.

O novo diagnostic `DFM012` reporta contrato read invalido quando isso pode ser
provado em compile-time. Fallback continua sendo uma limitacao de otimizacao,
nao breaking change para cenarios suportados pelo runtime.

## ADR-14 - Prompt 10.5 Dommel write conversion boundary

### Contexto

O Prompt 10.5 pediu conversao de escrita `Property -> Database value` quando a
arquitetura da Etapa 10 determinasse que isso e responsabilidade do FluentMap,
com Dommel como consumidor inicial. A restricao principal era usar extension
points suportados pela versao atual do Dommel, sem reflection privada e sem
copiar internals.

Dommel 3.5.3 expoe resolvers de tabela, coluna, chave, propriedades e
`ISqlBuilder`. Esses pontos permitem alterar metadata e SQL, mas `Insert` e
`Update` executam passando a entidade original ao Dapper como objeto de
parametros.

### Decisao

Write conversion permanece especificada como metadata no core, mas nao e
executada pela integracao Dommel na Etapa 10.5.

Nao sera criado wrapper implicito de parametros nem nova API de CRUD nesta etapa.
Tambem nao sera feita composicao implicita `PropertyConverter -> TypeHandler`.

### Alternativas consideradas

- Usar `ISqlBuilder` para trocar nomes de parametros: rejeitado porque o Dapper
  ainda le os valores da entidade original e nao ha propriedades convertidas.
- Copiar a montagem de `Insert`/`Update` do Dommel: rejeitado por duplicar
  responsabilidade de CRUD/SQL generation e aumentar risco de divergencia.
- Reflection privada sobre caches/internals do Dommel: rejeitada por fragilidade
  e incompatibilidade com o requisito.
- Mutar a entidade antes da chamada e restaurar depois: rejeitado por side
  effects, thread safety e excecoes intermediarias.
- Confiar em `TypeHandler<TProperty>`: preservado como fallback global, mas nao
  resolve conversao property-scoped.

### Consequencias

`connection.Insert(...)` e `connection.Update(...)` continuam respeitando
persistence metadata da Etapa 8, mas nao chamam write converters. Sem converter
executado, `TypeHandler<TProperty>` e provider continuam com o mesmo papel que
tinham antes.

O suporte futuro depende de um hook publico de parametros por propriedade no
Dommel ou de uma API explicita no pacote de integracao que deixe claro que usa
parametros convertidos e nao os metodos Dommel existentes diretamente.

## ADR-15 - Prompt 10.6 conversion diagnostics hardening

### Contexto

Converters agora possuem metadata, execucao runtime, suporte generated parcial
e uma fronteira Dommel/write documentada. Faltava consolidar diagnostics sem
transformar analyzers em executor de configuracao nem criar nova feature de
conversao.

### Decisao

O analyzer comum reporta somente configuracoes estaticamente provaveis:

- `DFM014` para converter por tipo sem contrato read/write compativel;
- `DFM015` para duplicidade direcional na mesma fluent chain.

O diagnostic de persistencia do analyzer comum foi renumerado para `DFM013`,
mantendo `DFM012` como diagnostic do generator para read converter gerado
invalido.

`FluentMapper.Validate()` e o registro runtime continuam responsaveis pela
composicao efetiva: maps externos, base maps, profiles, conventions,
persistencia efetiva e converter em propriedade que nunca sera materializada ou
persistida.

### Consequencias

Nao ha execucao de construtores pelo analyzer. Consumers recebem feedback cedo
quando o codigo fonte permite prova estatica, e ainda precisam de
`FluentMapper.Validate()`/testes para configuracoes dinamicas.

`Explain<T>()` permanece com metadata estruturada em
`MemberMappingExplanation.Conversion`; o `ToString()` nao virou formato
diagnostico de contrato.

Converters sao explicitamente documentados como stateless/thread-safe por
contrato, pois instancias podem ser reutilizadas em consultas concorrentes e no
caminho generated.
