# Contratos para Generated Materializers

Status: SPECIFICATION + IMPLEMENTATION
Prompt: 7.3
Data: 2026-07-28

## Objetivo

Criar a infraestrutura minima para que codigo gerado possa fornecer materializadores de linhas ao runtime do FluentMap sem acoplar o core a uma implementacao especifica do generator.

Esta entrega nao implementa nested generated materialization nem altera o generator existente para emitir materializers.

## Discovery

### Entrada esperada

O materializer gerado recebe um `IDataRecord` ja posicionado na linha corrente.

O runtime continua responsavel por executar a query via Dapper, obter o `IDataReader`, ler os nomes das colunas, iterar as linhas e escolher generated materializer ou fallback runtime.

O materializer gerado nao abre conexao, nao cria command, nao executa SQL e nao acessa APIs internas do Dapper.

### Retorno

O retorno e a entidade materializada:

```csharp
public delegate TEntity GeneratedRowMaterializer<out TEntity>(IDataRecord record)
    where TEntity : class;
```

O registry interno armazena o delegate como `Func<IDataRecord, object>` apenas para lookup uniforme.

### Acesso as colunas e ordinal lookup

O contrato publico representa o shape ordenado com `GeneratedMaterializerColumn`.

Cada coluna contem:

- `ColumnName`: nome esperado no ordinal;
- `MemberPath`: caminho de membro esperado para colunas materializadas;
- `Ignored`: indica que a configuracao efetiva deve ignorar a coluna.

O ordinal e implicito pela posicao da coluna no descriptor. Isso evita lookup por nome no hot path gerado e preserva a decisao de localizar por shape ordenado.

### Null handling

Null semantics ainda pertencem ao materializer que sera gerado em etapas futuras. O contrato atual apenas transporta o delegate.

O generated code futuro deve preservar:

- `DBNull` em reference/nullable vira `null`;
- `DBNull` em value type nao anulavel segue o default runtime atual;
- subarvore nested toda `NULL` nao cria objeto;
- subarvore parcialmente preenchida cria objeto;
- falhas de construtor devem ser encapsuladas com contexto quando o generator implementar esse caminho.

### Profile identification

Profiles sao identificados por `Type` no descriptor e por generic helper no registro:

```csharp
configuration.AddGeneratedMaterializer<TEntity, TProfile>(columns, materializer);
```

O lookup usa:

```text
EntityType + ProfileType opcional + ordered ColumnShape
```

Para profiles, o runtime exige que o profile map esteja registrado. Um generated descriptor nao pode criar um profile implicito nem vazar para `SqlMapper.SetTypeMap`.

### Constructors

Esta entrega nao escolhe construtores gerados. O contrato deixa essa responsabilidade para o generator futuro. O runtime apenas valida se o descriptor registrado continua compativel com a configuracao efetiva por coluna/member path antes de usar o delegate.

### Exceptions

O contrato de registro rejeita:

- descriptor nulo;
- delegate nulo;
- lista de colunas nula ou vazia;
- coluna nula;
- nome de coluna nulo/vazio/whitespace;
- member path nulo/vazio/whitespace para coluna materializada;
- profile type que nao implementa `IMappingProfile`;
- registro duplicado para mesma entidade/profile/shape.

Durante lookup, profile ausente continua gerando `FluentMapConfigurationException`, como o fallback runtime ja fazia.

### Caches e registry

Foi adicionada uma camada interna de registry:

```text
Generated materializer registry
  key: EntityType + ProfileType + ordered ColumnShape
  value: descriptor + delegate

Runtime materialization plan cache
  key: EntityType + ProfileType + ordered ColumnShape
  value: NestedMaterializationPlan
```

O lookup generated acontece antes de criar `NestedMaterializationPlan`.

O registry de generated materializers e limpo por `FluentMapper.Reset(...)`. Registro de maps/conventions invalida caches runtime existentes, mas nao remove descriptors gerados; o descriptor so e usado se ainda corresponder a configuracao efetiva no momento do lookup.

### Fallback runtime

Fallback e obrigatorio.

O runtime cai para `NestedMaterializationPlan` quando:

- nao ha descriptor para entity/profile/shape;
- o descriptor existe, mas seus member paths nao correspondem a configuracao efetiva;
- o descriptor espera coluna ignorada e a configuracao efetiva nao ignora;
- o descriptor espera coluna materializada e a configuracao efetiva ignora;
- a coluna nao corresponde nem a FluentMap/convention/profile nem ao fallback default do Dapper.

## Contrato Escolhido

Foram adicionados estes contratos publicos no namespace `Dapper.FluentMap.Materialization`:

- `GeneratedRowMaterializer<TEntity>`;
- `GeneratedMaterializerColumn`;
- `GeneratedMaterializerDescriptor<TEntity>`.

Foram adicionadas APIs publicas em `FluentMapConfiguration`:

- `AddGeneratedMaterializer<TEntity>(IEnumerable<GeneratedMaterializerColumn>, GeneratedRowMaterializer<TEntity>)`;
- `AddGeneratedMaterializer<TEntity, TProfile>(IEnumerable<GeneratedMaterializerColumn>, GeneratedRowMaterializer<TEntity>)`;
- `AddGeneratedMaterializer<TEntity>(GeneratedMaterializerDescriptor<TEntity>)`.

A escolha favorece baixo overhead, compatibilidade, testabilidade, AOT e separacao entre generator e runtime.

## Alternativas Avaliadas

### `IGeneratedMaterializer<T>`

Descartada para esta etapa. Embora seja nominalmente simples, obrigaria instancias geradas ou singletons, misturaria metadata e execucao no mesmo objeto, adicionaria dispatch virtual/interface no hot path e encorajaria o runtime a depender de uma forma especifica de classe gerada.

### Registro apenas por delegate

Descartado. Delegate sozinho nao descreve entity/profile/shape/member paths, nao permite validar se a configuracao efetiva ainda corresponde ao codigo gerado e e pior para diagnostico futuro.

### Descriptor internal com `InternalsVisibleTo`

Descartado. O generator emite codigo no assembly consumidor, que nao tem acesso aos internals do core. `InternalsVisibleTo` nao e viavel para assemblies arbitrarios de consumidores.

### Assembly scanning de materializers

Descartado. Conflita com trimming/AOT e com a decisao de que registro explicito ou gerado deve bastar sem scanning.

### Manifesto por assembly

Adiado. Pode ser util para assemblies referenciados, mas amplia o escopo e nao e necessario para o contrato minimo desta etapa.

## Compatibilidade

APIs existentes continuam funcionando.

O generator atual de registration continua valido porque `AddGeneratedMappings()` ainda chama apenas `AddMap<TMap>()` e `AddProfile<TMap>()`.

Consumidores sem generated materializer seguem usando `QueryMapped*` com o fallback runtime.

As annotations `RequiresUnreferencedCode` e `RequiresDynamicCode` em `QueryMapped*` permanecem corretas, pois qualquer chamada ainda pode cair no fallback runtime.

Dommel nao foi alterado.

## Testes Criados

`test/Dapper.FluentMap.Tests/GeneratedMaterializerContractTests.cs` cobre:

- registro e lookup default;
- missing materializer;
- profile default vs profile especifico;
- duplicate registration;
- descriptor/contract invalido;
- descriptor incompativel com configuracao efetiva;
- `QueryMapped*` usando generated quando registrado;
- fallback runtime quando generated esta ausente;
- concorrencia em lookup.

## Fora do Escopo

- Geracao Roslyn de materializers.
- Nested generated materialization.
- Constructor/value object generated path.
- TypeHandler generated boundary.
- Diagnostics publicos de generated vs runtime.
- Relaxar annotations de trimming/AOT.
- Benchmarks comparando generated real, pois o hot path gerado produtivo ainda nao existe.

## Validacao Executada

```text
dotnet restore .\Dapper.FluentMap.sln
dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build
dotnet run --project .\benchmarks\Dapper.FluentMap.Benchmarks\Dapper.FluentMap.Benchmarks.csproj --configuration Release --no-build -- --filter *MaterializationSteadyStateBenchmarks*
dotnet pack .\src\Dapper.FluentMap\Dapper.FluentMap.csproj --configuration Release --no-build --output .\artifacts\packages
```

Resultados:

- restore: sucesso;
- build: sucesso, 0 warnings, 0 errors;
- testes: sucesso, 240 testes aprovados;
- benchmark smoke steady state: sucesso.
- pack do core: sucesso, gerou `artifacts/packages/Dapper.FluentMap.2.0.0.nupkg`.

Warnings conhecidos no pack:

- `NU5125` por `PackageLicenseUrl` legado;
- recomendacao NuGet para README de pacote.

Resumo do benchmark smoke:

| Method | Mean | Allocated |
| --- | ---: | ---: |
| DapperPure | 1.338 ms | 283.17 KB |
| DapperWithFluentMapRootMapping | 1.469 ms | 283.3 KB |
| QueryMappedSimple | 1.739 ms | 361.42 KB |
| QueryMappedImmutableConstructor | 1.670 ms | 423.92 KB |
| QueryMappedNestedObject | 1.495 ms | 377 KB |
| QueryMappedValueObject | 1.355 ms | 587.84 KB |

Leitura: a rodada `ShortRun` e ruidosa, mas as alocacoes permaneceram alinhadas ao baseline de 7.2. Nao ha generated materializer produtivo para comparacao de ganho nesta etapa.
