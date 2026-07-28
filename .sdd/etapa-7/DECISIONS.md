# Etapa 7 Decisions

Status: ACTIVE
Prompt: 7.1

## ADR-7.1-001 - Generated Materializer Complementa o Runtime Materializer

### Contexto

`QueryMapped*` ja suporta nested objects, constructor mapping, Value Objects e profiles por meio de `NestedMaterializationPlan`. Esse caminho e funcional e cobre configuracoes dinamicas, mas usa reflection e dynamic code.

### Decisao

Materializadores gerados serao uma otimizacao complementar. O runtime materializer permanece como fallback autoritativo.

### Alternativas

- Substituir completamente `NestedMaterializationPlan`.
- Criar uma nova API separada apenas para generated materialization.
- Manter runtime-only e nao evoluir geracao.

### Consequencias

- Compatibilidade com consumidores atuais e preservada.
- A implementacao pode evoluir por fases.
- O core tera dois caminhos de materializacao que precisam de testes de equivalencia.
- APIs que podem cair no fallback continuam precisando de annotations de trimming/dynamic code.

## ADR-7.1-002 - Localizacao por Entity, Profile e ColumnShape

### Contexto

O cache atual de materializacao usa tipo da entidade, profile opcional e nomes de colunas ordenados. O codigo gerado tende a usar ordinais fixos, portanto a ordem das colunas importa.

### Decisao

Materializers gerados devem ser localizados por:

```text
EntityType + ProfileType opcional + ColumnShape ordenado
```

O descriptor gerado tambem deve carregar uma assinatura do mapping estatico usado na geracao.

### Alternativas

- Localizar apenas por entidade/profile.
- Localizar por entidade/profile e conjunto de colunas sem ordem.
- Escolher materializer por nome do metodo gerado em chamada explicita do usuario.

### Consequencias

- Evita usar ordinais incorretos.
- Preserva isolamento de profiles.
- Permite fallback para shapes inesperados.
- Pode gerar multiplos descriptors por entidade/profile em fases futuras.

## ADR-7.1-003 - Evitar Acoplamento Excessivo ao Dapper

### Contexto

O FluentMap integra com Dapper por contratos publicos como `SqlMapper.ITypeMap`, `SqlMapper.IMemberMap`, `ExecuteReader` e TypeHandlers. A compatibilidade com TypeHandlers hoje usa um adapter isolado porque toca em uma area sensivel do Dapper.

### Decisao

O materializer gerado deve operar contra `IDataRecord` e contra contratos do FluentMap. Ele nao deve gerar chamadas para APIs internas do Dapper nem reimplementar o pipeline de consulta do Dapper.

### Alternativas

- Gerar codigo que chama diretamente detalhes internos do Dapper.
- Delegar toda conversao escalar ao Dapper.AOT.
- Reimplementar parser completo de linhas do Dapper.

### Consequencias

- O FluentMap permanece focado em object graph mapping.
- Upgrades do Dapper continuam menos arriscados.
- TypeHandler gerado fica como decisao especifica futura.
- Pode haver menor cobertura gerada ate existir uma boundary segura de conversao.

## ADR-7.1-004 - Fallback Transparente e Diagnosticavel

### Contexto

Muitos maps validos para FluentMap nao sao estaticamente geraveis: assembly scanning, conventions customizadas, nomes calculados e maps construidos com estado runtime.

### Decisao

Ausencia de materializer gerado nao deve ser erro. O sistema deve cair para `NestedMaterializationPlan` e, em fase futura, oferecer diagnostico explicando o motivo.

### Alternativas

- Falhar quando o generator nao conseguir cobrir um map.
- Exigir opt-in explicito para fallback.
- Fazer o generator interpretar codigo arbitrario.

### Consequencias

- Consumidores atuais nao quebram.
- Cobertura gerada pode crescer incrementalmente.
- Performance/AOT precisam ser comunicados como propriedade do caminho gerado, nao da API inteira.
- Diagnostics serao importantes para evitar surpresa.

## ADR-7.1-005 - Nao Replicar Dapper.AOT

### Contexto

Dapper.AOT tem escopo proprio para gerar materializacao e execucao de queries Dapper. O FluentMap tem escopo menor: traduzir metadata de FluentMap em object graphs quando o usuario usa `QueryMapped*`.

### Decisao

A Etapa 7 nao deve gerar SQL, commands, parametros, readers, handlers gerais de Dapper ou substitutos de `Query<T>()`. O generated materializer do FluentMap deve se limitar a `IDataRecord -> object graph`.

### Alternativas

- Construir um pipeline AOT completo concorrente ao Dapper.AOT.
- Integrar diretamente com Dapper.AOT como dependencia obrigatoria.
- Abandonar `QueryMapped*` e recomendar somente Dapper.AOT.

### Consequencias

- Mantem a biblioteca pequena e coerente.
- Reduz risco de manutencao.
- Permite interoperar com Dapper normal.
- Nao resolve todos os cenarios AOT de Dapper, apenas o trecho FluentMap-controlled.

## ADR-7.1-006 - Evolucao Sem Breaking Change

### Contexto

O FluentMap e uma biblioteca publica com APIs legadas preservadas, incluindo dicionarios mutaveis globais. O README atual documenta `QueryMapped*` como runtime reflection/dynamic-code based.

### Decisao

A evolucao de generated materialization deve ser aditiva. `QueryMapped*`, `AddGeneratedMappings()`, `FluentMapper.Initialize`, `Validate`, `Explain`, Dapper type maps e fallback runtime devem permanecer compativeis.

### Alternativas

- Fazer uma major version removendo fallback e APIs legadas.
- Tornar generator obrigatorio.
- Alterar semanticamente `QueryMapped*` para falhar fora do generated path.

### Consequencias

- A primeira entrega pode sair como melhoria incremental.
- O design precisa validar correspondencia entre descriptor gerado e configuracao efetiva.
- Mutable dictionaries legados continuam sendo risco arquitetural.
- Claims de AOT/performance precisam ser condicionais ate validacao.

## ADR-7.1-007 - Primeira Cobertura Gerada Deve Priorizar Explicit Maps Literais

### Contexto

O analyzer e o generator atual ja conseguem reconhecer classes de map, `IEntityMap<TEntity>`, `IProfileMap<TProfile>` e parte da DSL. Conventions customizadas e naming policies dinamicas executam codigo arbitrario.

### Decisao

A primeira fase de geracao deve focar explicit maps com `Map(...).ToColumn("literal")`, `Ignore`, `IncludeBase<TBase>` geravel e profiles tipados.

### Alternativas

- Incluir todas as conventions desde o inicio.
- Gerar apenas propriedades root sem profiles.
- Esperar uma interpretacao completa da DSL.

### Consequencias

- O escopo inicial fica testavel.
- Nested/value object pode evoluir sobre uma base confiavel.
- Naming policies built-in podem ser adicionadas depois com regras claras.
- Conventions customizadas continuam no fallback.

## ADR-7.3-001 - Contrato Publico por Descriptor e Delegate

### Contexto

O source generator emite codigo no assembly consumidor e, portanto, nao pode chamar contratos `internal` do core. Ao mesmo tempo, o runtime nao deve depender de uma classe especifica gerada por uma versao especifica do generator.

### Decisao

Adicionar contratos publicos pequenos em `Dapper.FluentMap.Materialization`:

```text
GeneratedRowMaterializer<TEntity>
GeneratedMaterializerColumn
GeneratedMaterializerDescriptor<TEntity>
```

O registro acontece por APIs publicas aditivas em `FluentMapConfiguration`. O runtime guarda os descriptors em registry interno e usa delegate direto por linha quando o descriptor corresponde ao mapping efetivo.

### Alternativas

- `IGeneratedMaterializer<T>`.
- Registro somente por delegate.
- Descriptor `internal` com `InternalsVisibleTo`.
- Descoberta por assembly scanning.

### Consequencias

- Generated code pode chamar o core sem permissao especial.
- O contrato preserva baixo overhead e separacao generator/runtime.
- A API publica nova e pequena, mas passa a ser contrato SemVer.
- O descriptor carrega metadata suficiente para validar fallback seguro.

## ADR-7.3-002 - Validar Descriptor Contra Mapping Efetivo Antes de Usar

### Contexto

Descritores gerados podem ficar incompativeis com a configuracao efetiva por mudancas em maps, conventions, profiles ou uso dos dicionarios mutaveis legados.

### Decisao

O lookup generated deve validar, por coluna, se o descriptor corresponde ao mapping efetivo atual:

- coluna materializada deve apontar para o mesmo `MemberPath`;
- coluna ignorada deve estar ignorada na configuracao efetiva;
- profile deve estar registrado;
- shape deve bater por entidade, profile e nomes ordenados.

Quando a validacao falha, o runtime usa `NestedMaterializationPlan`.

### Alternativas

- Confiar sempre no descriptor registrado.
- Validar apenas entity/profile/shape.
- Invalidar/remover descriptors quando maps mudarem.

### Consequencias

- Preserva comportamento existente como autoridade funcional.
- Evita ordinais gerados para configuracao divergente.
- Adiciona um pequeno custo por query no lookup generated, nao por linha.
- Diagnostics publicos de motivo de fallback continuam para etapa futura.

## ADR-7.3-003 - Nao Relaxar Annotations de QueryMapped

### Contexto

Mesmo com generated materializer registrado, `QueryMapped*` ainda pode cair no fallback runtime.

### Decisao

Manter `RequiresUnreferencedCode` e `RequiresDynamicCode` em `QueryMapped*`.

### Alternativas

- Remover annotations quando houver generated descriptor.
- Criar novas APIs AOT-only nesta etapa.

### Consequencias

- As APIs publicas continuam conservadoras para trimming/AOT.
- A reducao futura de warnings exige caminho dedicado ou garantia de generated-only ainda nao especificada.
