# Historical Query Issues

Discovery executado para a Etapa 9 em 2026-07-28.

Fontes historicas:

- Issue #22: https://github.com/henkmollema/Dapper-FluentMap/issues/22
- Issue #43: https://github.com/henkmollema/Dapper-FluentMap/issues/43
- Issue #42 relacionada a multi-mapping: https://github.com/henkmollema/Dapper-FluentMap/issues/42
- Issue #62, plano v2 que relaciona #42, #43 e #56: https://github.com/henkmollema/Dapper-FluentMap/issues/62

## Issue #22

### Problema original

Issue "Conventions not working on 'Multiple Results'", aberta em 2015-01-30.
O reporter partiu do exemplo de `QueryMultiple` do README do Dapper:

```csharp
using (var multi = connection.QueryMultiple(sql, new { id = selectedId }))
{
    var customer = multi.Read<Customer>().Single();
    var orders = multi.Read<Order>().ToList();
    var returns = multi.Read<Return>().ToList();
}
```

O comportamento relatado era que convencoes configuradas no FluentMap nao eram
aplicadas aos POCOs retornados pelos result sets.

### Causa historica

A causa nao foi isolada no historico publico. Nos comentarios, o mantenedor
testou um cenario com `QueryMultiple`, `multi.Read<Customer>()` e
`multi.Read<Order>()` usando uma convencao de transformacao para colunas com
underscore, e relatou que funcionava. A issue foi fechada por falta de
informacao adicional.

A interpretacao arquitetural para a Etapa 9 e que #22 nao prova uma falha
remanescente de Dapper `GridReader`, mas prova que multiplos result sets sempre
foram uma area sem regressao clara no FluentMap.

### Estado atual do fork

O fork atual registra type maps globais do Dapper para mapas e convencoes por
entidade. Portanto `connection.QueryMultiple(...).Read<T>()` deve conseguir
usar root-level explicit mappings e conventions quando o caminho normal do
Dapper consulta o type map global da entidade.

Entretanto, as APIs opt-in `QueryMapped*` do fork nao participam de
`QueryMultiple`: elas executam um unico reader via `SqlMapper.ExecuteReader`,
bufferizam todas as linhas em `List<TEntity>` e fecham o reader antes de
retornar. Nao ha `QueryMultipleMapped` nem `ReadMapped<T>()`.

Nao foi localizado teste de regressao dedicado para #22 no fork atual.

### Ainda reproduzivel?

Nao foi reproduzido nesta discovery porque este prompt nao deve implementar
features produtivas nem criar regressao executavel ainda.

Estado de risco:

- para root-level Dapper mapping, provavelmente nao reproduzivel se o type map
  global estiver instalado corretamente;
- para materializacao avancada do fork, ainda nao suportado em
  `QueryMultiple`, porque nao existe API `ReadMapped<T>()`;
- para profiles, geracao runtime/generated e nested/value-object
  materialization em multiplos result sets, ainda nao ha cobertura.

### Solucao arquitetural proposta

Implementar infraestrutura propria de multiple result sets para o caminho
opt-in do FluentMap, sem alterar internals do Dapper:

- `QueryMultipleMapped(...)` deve executar o comando por APIs publicas do
  Dapper ou ADO.NET e retornar um wrapper disposable controlado pelo FluentMap.
- O wrapper deve expor `ReadMapped<TEntity>()` e
  `ReadMapped<TEntity, TProfile>()`.
- O wrapper deve materializar cada result set pelo mesmo dispatch
  generated-then-runtime usado por `QueryMapped*`.
- Para root-level Dapper behavior sem materializacao avancada, manter
  `connection.QueryMultiple(...).Read<T>()` como caminho Dapper normal.

Nao alterar `SqlMapper.GridReader` por reflection, heranca ou acesso a membros
nao publicos. A API publica do `GridReader` em Dapper 2.1.79 nao fornece um
reader publico suficiente para reutilizar diretamente o materializador atual.

### Regression coverage necessaria

- `QueryMultipleMapped` com tres result sets e chamadas sequenciais
  `ReadMapped<Customer>()`, `ReadMapped<Order>()`, `ReadMapped<Return>()`.
- Convencoes aplicadas por entidade em result sets diferentes.
- Mapeamento explicito por entidade em result sets diferentes.
- Result set dinamico/escalares continuam sendo responsabilidade de Dapper ou
  de uma API explicitamente nao mapeada, se criada.
- Ordem de consumo obrigatoria: tentar ler fora de ordem ou ler apos dispose
  deve falhar de forma previsivel.
- Fechamento do reader/command quando o wrapper e descartado.

### Estado apos Prompt 9.3

Implementada regressao minima para o caminho opt-in atual:

- `HistoricalIssue22ReadMappedShouldApplyConventionsAcrossMultipleResultSets`
  configura a mesma convencao por entidade e le dois result sets sequenciais
  com `ReadMappedSingle<T>()`;
- a convencao e aplicada de forma independente para `ConventionCustomer` e
  `ConventionOrder`;
- o teste usa `DataTableReader` para tornar os grids deterministicos e
  provider-independent.

O teste cobre `QueryMultipleMapped(...).ReadMapped*`, nao altera nem substitui
o comportamento Dapper puro de `QueryMultiple(...).Read<T>()`.

### Estado apos Prompt 9.6

A regressao passou a ter nome orientado a comportamento:

- `MappedConventionShouldApplyToTypedReadFromMultipleResults`.

Ela permanece ligada a issue #22 nesta documentacao SDD, mas o teste em si
expressa o contrato permanente: convencoes configuradas por entidade devem ser
aplicadas a leituras tipadas de multiplos result sets no caminho
`QueryMultipleMapped(...).ReadMapped*`.

## Issue #43

### Problema original

Issue "Does not appear to work with QueryMultiple and .Read<>", aberta em
2016-08-10. O reporter usava uma stored procedure com tres tabelas:

```csharp
var ds = con.QueryMultiple("uspSELNodes", p, commandType: CommandType.StoredProcedure);
var rows = ds.Read().ToDictionary(v => v.RowNo);
var total = ds.Read<int>().First();
var columns = ds.Read<DynamicTable.Column>();
```

As colunas que batiam pelo nome eram preenchidas pelo Dapper, mas as colunas
mapeadas por `EntityMap<DynamicTable.Column>` ficavam com valores default.

### Causa historica

Os comentarios ligam #43 a uma regressao entre Dapper.FluentMap 1.4.1 e 1.5.x,
possivelmente relacionada a #42. O reporter confirmou que 1.4.1 funcionava.
Em 2018-11-16, o mantenedor marcou #43 como corrigida na versao 1.7.0 para full
.NET e .NET Core 2.0/2.1.

A issue #42, embora seja sobre Dapper multi-mapping em um unico result set,
registrou uma excecao de convencoes ambiguas em 1.5.x. A issue #62 citou #42 e
#43 como relacionadas a melhorias de v2, incluindo remocao de dependencia de
`ReflectedType`/`DeclaringType`.

### Estado atual do fork

O fork atual ainda possui caminhos que comparam propriedades por
`PropertyInfo.ReflectedType` em convencoes no target nao `NETSTANDARD1_3`, mas
tambem possui melhorias posteriores de `MemberPath`, inherited mapping,
validation, profiles e generated materialization.

O problema especifico de #43 no caminho Dapper normal pode estar resolvido pelo
type map global atual, mas nao existe cobertura dedicada localizada para
`QueryMultiple().Read<T>()`.

O problema para materializacao avancada continua fora do escopo implementado:
`QueryMapped*` nao oferece `QueryMultiple` nem `ReadMapped`.

### Ainda reproduzivel?

Nao determinado por execucao nesta discovery. A reproducao exata exigiria um
teste de integracao com SQLite ou provider equivalente usando multiplos
statements/result sets, quando o provider suportar `NextResult`.

Para a Etapa 9, tratar como lacuna de cobertura e design, nao como permissao
para mexer em internals do Dapper.

### Solucao arquitetural proposta

Separar dois cenarios:

- `connection.QueryMultiple(...).Read<T>()`: comportamento Dapper normal,
  protegido por regressao historica para root-level explicit mappings e
  conventions.
- `connection.QueryMultipleMapped(...).ReadMapped<T>()`: novo caminho opt-in
  para materializacao avancada, profiles e equivalencia runtime/generated.

O wrapper FluentMap deve ter ownership claro do reader/command que criou. Se a
API escolher aceitar um `GridReader` existente, ela deve ser limitada a
operacoes que a API publica do Dapper permite; como `GridReader.Reader` nao e
publico, essa alternativa nao deve ser o caminho principal.

### Regression coverage necessaria

- Regressao historica #43 com terceiro result set mapeando colunas
  `column_prefix`, `column_name`, `display_order`, `can_be_ordered`,
  `can_be_filtered` e `column_width_in_pixels`.
- Variacao por convention para simular #22.
- Variacao com `ReadMapped<Column>()` e, se decidido, teste separado para
  `QueryMultiple().Read<Column>()` no caminho Dapper normal.
- Equivalencia generated/runtime para o mesmo result set, incluindo shape
  ordenado que aciona generated e shape alternativo que cai para runtime.
- Falha de configuracao em profile ausente preservando
  `FluentMapConfigurationException`.
- Materializacao de scalar result set deve permanecer via Dapper ou API
  explicitamente fora do FluentMap advanced materialization.

### Estado apos Prompt 9.3

Implementada regressao minima para o caminho opt-in atual:

- `HistoricalIssue43ReadMappedShouldApplyExplicitMapOnLaterResultSet` le tres
  result sets sequenciais;
- o terceiro result set usa colunas equivalentes ao relato historico:
  `column_prefix`, `column_name`, `display_order`, `can_be_ordered`,
  `can_be_filtered` e `column_width_in_pixels`;
- o resultado prova que um grid posterior nao perde o `EntityMap` explicito.

O teste modela os grids escalares/dinamicos historicos como pequenas entidades
mapeadas, porque `MappedGridReader` e deliberadamente uma API de
materializacao de entidades e nao uma substituicao geral para `GridReader`.

### Estado apos Prompt 9.6

A regressao passou a ter nome orientado a comportamento:

- `ExplicitMapShouldApplyToLaterTypedReadFromMultipleResults`.

Ela permanece ligada a issue #43 nesta documentacao SDD, mas o teste em si
expressa o contrato permanente: mapeamentos explicitos devem continuar sendo
aplicados em result sets posteriores, inclusive quando os grids anteriores ja
foram consumidos.
