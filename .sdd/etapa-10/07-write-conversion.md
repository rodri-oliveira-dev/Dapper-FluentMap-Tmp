# Etapa 10 - Write Conversion & Dommel Boundary

## Objetivo do Prompt 10.5

Investigar se a conversao:

```text
Property CLR value -> Database/provider CLR value
```

deve e pode ser executada na Etapa 10 pela integracao opcional com Dommel, sem
fazer o core executar CRUD e sem usar reflection privada ou copiar internals do
Dommel.

## Resultado arquitetural

A Etapa 10.5 nao implementa execucao de write converters no Dommel.

O motivo e tecnico e de fronteira publica: o Dommel 3.5.3 usado pelo projeto
expoe resolvers para tabela, coluna, chave, propriedades e SQL builders, mas os
metodos publicos `Insert` e `Update` continuam executando o comando passando a
propria entidade como objeto de parametros para o Dapper.

Na fonte publica mais proxima da versao atual, o fluxo e:

```text
Dommel Insert/Update
    -> resolve table/properties/keys/columns
    -> build SQL
    -> connection.ExecuteScalar/Execute(sql, entity, ...)
    -> Dapper cria parametros a partir das propriedades da entidade
```

Esses extension points permitem excluir ou incluir propriedades/colunas e
alterar SQL gerado, mas nao permitem trocar o valor de `DbParameter.Value` por
propriedade antes de o Dapper materializar os parametros.

## Write converter

O contrato publico de escrita ja existe no core:

```csharp
public interface IWritePropertyConverter<in TProperty, out TDatabase>
{
    TDatabase ConvertToDatabase(TProperty value);
}
```

Sua direcao e:

```text
Property CLR value -> Database/provider CLR value
```

Na Etapa 10.5, esse contrato permanece metadata descritiva. Ele nao e executado
por `Dapper.FluentMap.Dommel`.

## Persistence metadata

A decisao preserva integralmente a Etapa 8. A participacao de propriedades em
persistencia continua determinada por `PropertyPersistenceMetadata` e pela
integracao Dommel ja existente.

| Semantica | Insert | Update | Write converter na Etapa 10.5 |
| --- | --- | --- | --- |
| Normal | participa | participa | nao executado |
| ReadOnly | omitido | omitido | nao executado |
| Computed | omitido | omitido | nao executado |
| Generated default on insert | omitido | participa, salvo exclusao | nao executado |
| Identity key | omitido | WHERE only | nao executado |
| Non-identity key | participa | WHERE only | nao executado |
| ExcludeFromInsert | omitido | participa | nao executado |
| ExcludeFromUpdate | participa | omitido do SET | nao executado |
| Ignore | omitido | omitido | nao executado |

Mesmo quando write conversion vier a ser suportada, a regra deve continuar:
propriedades que nao participam da operacao nao podem chamar converter nessa
operacao.

## Insert

Para `INSERT`, a integracao atual com Dommel controla a lista efetiva de
propriedades por `DommelPropertyResolver` e `DommelPersistenceSqlBuilder`.

O SQL builder consegue recompor colunas e nomes de parametros, mas os valores
ainda vem da entidade original passada ao Dapper pelo Dommel. Alterar o SQL para
outro nome de parametro nao resolve a conversao, pois a entidade nao expoe uma
propriedade com o valor convertido.

Portanto, `ConvertToDatabaseUsing(...)` nao e chamado em `connection.Insert(...)`
na Etapa 10.5.

## Update

Para `UPDATE`, a integracao atual usa metadata de geracao para manter fora do
`SET` propriedades que nao participam de update. O valor de cada parametro do
`SET` e das chaves do `WHERE` ainda e lido pelo Dapper da entidade original.

Portanto, `ConvertToDatabaseUsing(...)` tambem nao e chamado em
`connection.Update(...)` na Etapa 10.5.

## Null

Como a escrita nao e executada nesta etapa, nao ha nova semantica de null em
Dommel.

A regra futura recomendada permanece alinhada com read conversion:

```text
null nao deve ser enviado ao converter por default
    -> parametro recebe null/DBNull conforme Dapper/provider
```

Isso evita converter null em valores sentinela sem opt-in explicito e preserva a
semantica de bancos relacionais.

## Parameters

Nao foi adicionado wrapper de parametros, `DynamicParameters`, parameter
metadata ou `DbType`.

Uma implementacao futura precisa de um dos caminhos abaixo:

- hook publico do Dommel para transformar valores por propriedade antes da
  execucao;
- API publica propria no pacote Dommel integration que delegue resolucao de SQL
  ao Dommel, mas receba parametros convertidos de forma explicita;
- mudanca upstream no Dommel para aceitar um parameter/value resolver.

A primeira opcao e preferivel porque preserva `connection.Insert(...)` e
`connection.Update(...)` como responsabilidade do Dommel.

## TypeHandler interaction

Sem write converter executado:

```text
Dommel -> Dapper parameterization -> Dapper TypeHandler<TProperty>/provider
```

continua sendo o comportamento efetivo para `Insert` e `Update`, igual ao
comportamento anterior.

Quando write conversion for implementada, a precedencia especificada deve ser:

```text
property write converter
    -> final database/provider CLR value
```

Nesse caminho, o FluentMap nao deve aplicar:

```text
PropertyConverter -> TypeHandler<TProperty>
```

em sequencia. Um converter por propriedade e uma decisao local explicita e deve
produzir o valor de parametro final no nivel CLR. Sem converter por propriedade,
`TypeHandler<TProperty>` continua sendo o mecanismo global recomendado.

Se o valor convertido tiver seu proprio tipo CLR com `TypeHandler<TDatabase>`,
qualquer uso por Dapper precisa ser tratado como extensao futura explicita e
testada; a Etapa 10.5 nao define essa composicao.

## DbType

Nao ha mapeamento generico de `DbType` nesta etapa.

Se um converter exigir `DbType` especifico para diferenciar, por exemplo,
`string` ANSI/Unicode, tamanho, precision/scale ou tipos provider-specific, isso
deve ser modelado como extensao futura de parameter metadata. Esse problema nao
deve ser escondido dentro de `IWritePropertyConverter<TProperty, TDatabase>`.

## Profiles

Profiles seguem sendo maps separados para materializacao/query shapes. A Etapa
10 nao deve forcar write profiles apenas por simetria.

Como Dommel `Insert` e `Update` atuais operam sobre a entidade/tipo, sem API
publica de profile para persistencia, write converters profile-specific nao sao
executados nem introduzidos nesta etapa.

Uma decisao futura de profiles de escrita precisa primeiro definir:

- como o consumidor escolhe um profile em uma operacao Dommel de escrita;
- se o profile altera somente conversao ou tambem participacao de colunas;
- como isso interage com cache de SQL e resolvers globais do Dommel.

## Test strategy

Os cenarios pedidos para write conversion continuam sendo requisitos para a
implementacao futura:

- insert normal;
- update normal;
- converter;
- null;
- read-only;
- computed;
- generated;
- identity;
- non-identity key;
- exclude insert;
- exclude update;
- coexistencia com `TypeHandler<TProperty>`;
- falha de converter.

Na Etapa 10.5, eles nao foram adicionados como testes de execucao porque seria
necessario implementar um caminho de escrita nao suportado pelos extension
points publicos atuais do Dommel. Os testes aplicaveis sao os de regressao de
persistence metadata Dommel, garantindo que a decisao nao alterou insert/update
existentes.

## Limitacao registrada

O suporte completo a write conversion fica bloqueado ate existir um hook publico
de valores de parametros por propriedade ou uma API publica propria e explicita
que nao se confunda com os metodos `Insert`/`Update` do Dommel.

Nao houve mudanca comportamental em `connection.Insert(...)`,
`connection.Update(...)`, `InsertAll(...)` ou variantes async.
