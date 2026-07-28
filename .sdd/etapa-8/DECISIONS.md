# Etapa 8 - Architectural Decisions

## ADR-1 - Read semantics vs write semantics

### Contexto

Historicamente, `Ignore()` foi usado para excluir propriedades de mapping. As
issues #94, #123 e #130 mostram que usuarios tambem precisam excluir
propriedades de escrita sem perder materializacao de leitura.

### Decisao

Separar conceitualmente `Read`, `Insert` e `Update`. Leitura pertence aos type
maps/materializers; escrita pertence aos consumidores de persistencia, hoje
principalmente Dommel.

### Alternativas consideradas

- Manter apenas `Ignore()`.
- Usar apenas `DatabaseGeneratedOption`.
- Criar APIs Dommel-only sem metadata no core.

### Consequencias

O core pode descrever metadata, mas nao gera SQL. Dommel traduz metadata para
insert/update.

## ADR-2 - `Ignore()` vs read-only

### Contexto

`Ignore()` impede materializacao. Read-only precisa continuar lendo a coluna,
mas omitir escrita.

### Decisao

`Ignore()` continua significando `Read=no`, `Insert=no`, `Update=no`.
Read-only sera uma semantica diferente: `Read=yes`, `Insert=no`, `Update=no`.

### Alternativas consideradas

- Alterar `Ignore()` para significar apenas "ignore on write".
- Fazer `Ignore()` depender do pacote consumidor.

### Consequencias

Preserva compatibilidade e evita regressao de #133. Usuarios terao API separada
para read-only em prompts futuros.

## ADR-3 - Localizacao da metadata de persistencia

### Contexto

Dommel e o consumidor que gera CRUD, mas a decisao de uma propriedade ser
read-only/default/computed e metadata do mapping.

### Decisao

Colocar a metadata conceitual no core por contrato aditivo/opcional, consumido
pelo Dommel. O core nao executa CRUD.

### Alternativas consideradas

- Metadata exclusivamente no Dommel.
- Metadata apenas externa em resolvers customizados.
- Expandir `IPropertyMap` diretamente.

### Consequencias

Evita duplicacao e permite diagnostics/analyzers. A implementacao deve preservar
compatibilidade binaria, preferindo interface opcional.

## ADR-4 - Computed vs generated/default

### Contexto

Dommel usa `IsGenerated` como filtro operacional, mas generated e uma categoria
ampla. Computed e default-on-insert possuem escrita diferente.

### Decisao

`Computed` representa `Read=yes`, `Insert=no`, `Update=no`.
`DatabaseDefaultOnInsert` representa `Read=yes`, `Insert=no` e `Update=yes` por
default, com composicao possivel para tambem excluir update.

### Alternativas consideradas

- Tratar todo `Generated` como `Insert=no`, `Update=no`.
- Mapear tudo para `DatabaseGeneratedOption.Computed`.

### Consequencias

API futura deve expor intencao mais precisa que apenas `Generated`.

## ADR-5 - Key vs identity

### Contexto

#122 mostrou que key nao identity foi omitida do insert porque key e identity
foram acopladas.

### Decisao

`Key` e `Identity` sao dimensoes independentes. Key nao identity entra em
`INSERT`; identity key nao entra em `INSERT`. Keys nao entram no `SET` de
`UPDATE`, mas participam do `WHERE`.

### Alternativas consideradas

- Manter `IsKey()` implicando identity sempre.
- Obrigar toda key nao identity a chamar `SetGeneratedOption(None)`.

### Consequencias

Preserva o comportamento atual quando necessario, mas a arquitetura futura deve
tornar a intencao explicita e testavel.

## ADR-6 - Backward compatibility

### Contexto

FluentMap e biblioteca publica. `IPropertyMap`, `PropertyMap`,
`DommelPropertyMap` e `FluentMapper.Initialize` sao superficie sensivel.

### Decisao

Evolucao deve ser aditiva. Nao remover APIs, nao mudar significado de
`Ignore()`, nao introduzir breaking change sem versao major e justificativa.

### Alternativas consideradas

- Redesenhar `IPropertyMap` diretamente.
- Trocar `GeneratedOption` por enum propria removendo API antiga.

### Consequencias

Prompts futuros devem preferir interfaces opcionais, defaults compativeis e
adapters internos.

## ADR-7 - Interacao com Dommel

### Contexto

Dommel 3.5.3 constroi `Insert`/`Update` filtrando `ColumnPropertyInfo.IsGenerated`.
O pacote FluentMap.Dommel ja instala resolvers customizados em `ForDommel()`.

### Decisao

Dommel continua sendo o unico responsavel por SQL CRUD. FluentMap.Dommel deve
traduzir metadata de persistencia para os contratos publicos do Dommel,
especialmente `ColumnPropertyInfo` e resolvers de key/property.

### Alternativas consideradas

- Gerar SQL no core.
- Criar SQL builder proprio para substituir Dommel.
- Duplicar todo o modelo de Dommel no core.

### Consequencias

Nao adicionar CRUD ao core. Regression tests de insert/update pertencem ao
projeto Dommel integration.

## ADR-8 - Interacao com generated materializers

### Contexto

Generated materializers da etapa 7 sao otimizacao de leitura
`IDataRecord -> entity`. Eles ja conhecem ignored columns.

### Decisao

Generated materializers devem observar apenas semantica de leitura. Metadata de
insert/update/key/identity/computed nao altera materializacao, salvo se tambem
alterar `Read`.

### Alternativas consideradas

- Incluir metadata de persistencia nos descriptors de generated materializer.
- Fazer generated materializers validarem toda metadata de persistencia.

### Consequencias

Evita acoplamento entre leitura e escrita. O generator/analyzer pode reconhecer
a nova API para diagnostics, mas nao deve mudar delegates de leitura por causa
de insert/update.
