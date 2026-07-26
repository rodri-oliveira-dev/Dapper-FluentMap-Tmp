# 04 - Generated Materializer Spike

Status: COMPLETED

## Current Architecture

`QueryMapped*` e o caminho opt-in atual para materializacao controlada pelo FluentMap. Ele executa o comando pelo Dapper, abre um `IDataReader`, coleta os nomes das colunas e pede ao `MappingRegistry` um `NestedMaterializationPlan` cacheado por:

```text
EntityType + ProfileType + ordered column names
```

O plano runtime:

- resolve mappings efetivos por coluna, incluindo profile opcional;
- preserva `MemberPath` completo para paths como `Rank.Level` e `Seniority.Level`;
- aplica precedencia de mapping explicito, convention/naming policy e fallback default do Dapper;
- constroi objetos aninhados mutaveis por construtor publico sem parametros e setters publicos;
- constroi Value Objects e objetos imutaveis por construtores publicos compativeis;
- decide `DBNull`/null por subarvore;
- usa `DapperTypeHandlerAdapter` para TypeHandlers escalares;
- compila delegates com `Expression.Compile`.

As APIs publicas `QueryMapped*` permanecem anotadas com:

```text
RequiresUnreferencedCode
RequiresDynamicCode
```

O source generator atual (`Dapper.FluentMap.Generators`) gera apenas registro:

```csharp
configuration.AddGeneratedMappings();
```

Ele descobre maps na compilacao atual e emite `AddMap<TMap>()` ou `AddProfile<TMap>()`. Ele nao le `DbDataReader`, nao interpreta todo o corpo do map e nao gera materializers.

## Problem

`FM-RISK-004` permanece: `QueryMapped*` depende de reflection runtime e dynamic code para gerar accessors, factories, conversores e chamadas de construtor. Isso limita uso em trimming/Native AOT e cria custo de primeira query por plano.

O spike investiga se e tecnicamente viavel gerar materializers de `DbDataReader` em compile-time para o subconjunto de mappings do FluentMap que pode ser conhecido estaticamente, preservando fallback runtime para configuracao dinamica.

## Research Questions

1. Qual metadata o source generator consegue obter em compile-time?
2. Quais mappings sao estaticos e detectaveis?
3. Quais mappings podem ser construidos dinamicamente e portanto nao sao geraveis?
4. Como profiles poderiam ser representados?
5. Como `MemberPath` poderia virar codigo gerado?
6. Como nested mutable objects seriam materializados?
7. Como immutable Value Objects seriam materializados?
8. Como constructors seriam selecionados?
9. Como TypeHandlers seriam integrados?
10. Como `DBNull`/null seriam tratados?
11. Como conversoes seriam feitas?
12. Como naming policies/conventions afetariam geracao?
13. Como mappings registrados em assemblies externos seriam tratados?
14. Como caching mudaria?
15. Como generated e runtime materializer coexistiriam?

## Experiments Performed

Arquivos analisados:

- `docs/sdd/etapa-4/02-trimming-aot.md`;
- `docs/sdd/etapa-4/03-source-generator.md`;
- `docs/sdd/etapa-5/01-nested-materialization-spike.md`;
- `docs/sdd/etapa-5/02-nested-object-materialization.md`;
- `docs/sdd/etapa-5/03-value-objects.md`;
- `docs/sdd/etapa-5/04-mapping-profiles.md`;
- `docs/sdd/etapa-6/01-configuration-lifecycle.md`;
- `docs/sdd/etapa-6/02-mapping-state-encapsulation.md`;
- `docs/sdd/etapa-6/03-dapper-compatibility-adapters.md`;
- `docs/sdd/fluentmap-risk-assessment.md`;
- `src/Dapper.FluentMap.Generators/MappingRegistrationGenerator.cs`;
- `src/Dapper.FluentMap/QueryMappedExtensions.cs`;
- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`;
- `src/Dapper.FluentMap/MappingRegistry.cs`;
- `src/Dapper.FluentMap/Mapping/MemberPath.cs`;
- tests de generator, profiles, nested materialization, Value Objects e TypeHandler compatibility.

Prototipo adicionado:

- `test/Dapper.FluentMap.Tests/GeneratedMaterializerSpikeTests.cs`.

O prototipo e intencionalmente test-only. Ele simula codigo que um generator poderia emitir:

- usa ordinais fixos de `IDataRecord`;
- nao usa `Expression.Compile`;
- nao usa reflection para getter, setter ou construtor;
- materializa uma entidade simples com mapping explicito;
- materializa nested mutable object por subarvore;
- materializa Value Object imutavel por construtor;
- representa profile por metodo gerado separado;
- preserva `DBNull` como `null` em reference/Value Object nullable.

## Prototype

Forma conceitual validada pelo teste:

```csharp
internal static GeneratedCustomer ReadLegacyProfile(IDataRecord record)
{
    return new GeneratedCustomer(
        ReadInt32(record, 0),
        record.IsDBNull(1) ? null : new GeneratedCpf(ReadString(record, 1)),
        ReadString(record, 2));
}
```

Esse codigo prova que, quando coluna, path, construtor e profile sao conhecidos, o materializer pode ser codigo direto contra `IDataRecord`/`DbDataReader`, sem a combinacao atual de reflection + expression compilation no runtime.

O prototipo nao prova:

- discovery automatica de todas as chamadas fluent no corpo de mapas;
- TypeHandler gerado;
- Native AOT runtime real;
- convencoes complexas;
- performance.

## Findings

### 1. Metadata disponivel em compile-time

O generator atual ja consegue obter por Roslyn:

- classes de map na compilacao atual;
- `IEntityMap<TEntity>`;
- `IProfileMap<TProfile>`;
- abstracao, genericidade, visibilidade e construtor publico sem parametros do map;
- hierarquia de tipos e symbols de entidades/propriedades/construtores quando referenciados no codigo fonte.

Para materializer, o generator poderia obter mais metadata apenas se interpretar um subconjunto estatico da DSL:

- chamadas `Map(x => x.Property)` e `Map(x => x.Nested.Property)`;
- chamadas `ToColumn("literal")`;
- chamadas `Ignore()`;
- `IncludeBase<TBase>()`;
- `IProfileMap<TProfile>`.

Ele nao deve executar o construtor do map. Construtores de maps sao codigo arbitrario.

### 2. Mappings estaticos e detectaveis

Geraveis com boa confianca:

- maps declarados na compilacao atual;
- lambdas simples de member access;
- coluna literal em `ToColumn`;
- `Ignore`;
- profile por `IProfileMap<TProfile>`;
- `IncludeBase<TBase>` quando base map geravel no mesmo contexto;
- constructor binding por nomes de propriedades/parametros visiveis no symbol model.

### 3. Mappings dinamicos nao geraveis

Nao geraveis sem fallback:

- column names calculados por variavel, helper, config externa ou interpolacao nao constante;
- chamadas fluent escondidas em metodos arbitrarios;
- maps adicionados por `AddMap(new SomeMap(runtimeValue))`;
- assembly scanning;
- mutacao direta de `FluentMapper.EntityMaps` e `TypeConventions`;
- conventions customizadas que executam codigo no construtor;
- naming policies aplicadas dinamicamente;
- maps em assemblies referenciados sem um contrato de manifesto gerado;
- qualquer path que dependa de reflection runtime nao representada na compilacao atual.

### 4. Profiles

Profiles devem virar chaves geradas fortemente tipadas:

```text
EntityType + ProfileType + ColumnShape
```

Cada profile geravel pode produzir um materializer separado ou um descriptor gerado separado. Isso preserva a decisao E6-D003: profile e query-scoped e nao troca `SqlMapper.SetTypeMap`.

### 5. MemberPath como codigo gerado

`MemberPath` pode virar uma cadeia de symbols no codigo gerado:

```text
Customer.Cpf.Number -> constructor arg Cpf(number)
Customer.Address.City -> ensure Address then set City
```

Para o runtime gerado, a identidade precisa continuar sendo o path completo, nao apenas o terminal. Isso evita colisao entre `Rank.Level` e `Seniority.Level`.

### 6. Nested mutable objects

Codigo gerado pode emitir:

```text
if any subtree column is non-null:
    if parent.Address == null:
        parent.Address = new Address()
    parent.Address.City = value
else:
    parent.Address = null when assignable
```

Isso e equivalente a semantica atual de subarvore e nao exige reflection se os setters/construtores forem publicos e conhecidos.

### 7. Immutable Value Objects

Codigo gerado pode emitir construcao bottom-up:

```text
Cpf cpf = all cpf subtree columns are null ? null : new Cpf(number)
Customer customer = new Customer(id, cpf)
```

Construtores privados, setters privados, fields e `FormatterServices` continuam fora do contrato. Factory methods poderiam ser geradas futuramente apenas com API explicita.

### 8. Constructor selection

O generator pode usar symbols para selecionar construtores publicos por nome de parametro e tipo compativel, espelhando a regra atual. A parte sensivel e que o shape real de colunas vem do reader em runtime. Portanto a selecao gerada deve ser condicionada ao materializer gerado para aquele profile/map e ao conjunto de colunas suportado, com fallback se a query nao trouxer colunas esperadas.

### 9. TypeHandlers

Ha duas opcoes:

- chamar diretamente um caminho Dapper generico conhecido para `T`, quando possivel;
- criar uma pequena API publica ou boundary geravel no core para converter via TypeHandler sem reflection por tipo arbitrario.

O primeiro caminho reduz reflection, mas acopla codigo gerado a uma shape version-sensitive do Dapper. O segundo exige API publica nova e deve ser especificado antes de implementacao. A decisao da Entrega 03 permanece: nao espalhar reflection Dapper-specific.

### 10. DBNull/null

Codigo gerado deve preservar a regra atual:

- `DBNull` em reference/nullable vira `null`;
- `DBNull` em value type nao anulavel vira default quando esse for o contrato atual;
- subarvore toda `NULL` vira objeto nested/value object `null`;
- subarvore parcialmente preenchida cria o objeto e passa `null`/default para folhas correspondentes.

### 11. Conversoes

Geravel:

- typed getters quando o tipo da coluna for previsivel;
- `Convert.ToXxx`/`Convert.ChangeType` para fallback local;
- enum por string ou valor numerico;
- `Guid` por string;
- nullable wrappers.

Ainda precisa de decisao:

- cultura e provider;
- overflow/invalid cast diagnostics;
- TypeHandler sem reflection;
- conversoes customizadas publicas.

### 12. Naming policies/conventions

Mappings explicitos com colunas literais sao bons candidatos.

Naming policies e conventions sao mais dificeis:

- `NamingPolicy.SnakeCase` built-in pode ser geravel se registrado estaticamente;
- conventions customizadas sao objetos com codigo arbitrario e hoje populam `PropertyMaps` em runtime;
- per-profile conventions ainda nao existem.

Recomendacao: primeira etapa gerada deve cobrir explicit maps e talvez naming policies built-in comprovaveis; conventions dinamicas devem usar fallback.

### 13. Assemblies externos

O generator atual descobre apenas maps da compilacao atual. Para assemblies externos ha opcoes:

- cada assembly gera seu proprio manifesto/materializers e expõe um registro gerado local;
- o assembly consumidor referencia manifests de dependencies;
- fallback runtime para maps externos.

O caminho mais compativel e permitir coexistencia: materializers gerados por assembly quando disponiveis, fallback runtime quando nao.

### 14. Caching

O cache runtime mudaria de plano unico para duas camadas:

```text
GeneratedMaterializerRegistry
    key: EntityType + ProfileType + ColumnShape
    value: delegate/static descriptor gerado

Runtime MaterializationPlanCache
    key: EntityType + ProfileType + ColumnShape
    value: NestedMaterializationPlan
```

O fallback runtime permanece essencial para dynamic maps. A invalidacao do registry deve remover qualquer cache runtime afetado, mas materializers gerados sao estaticos e so devem ser usados se a configuracao efetiva ainda corresponder ao descriptor gerado.

### 15. Coexistencia generated/runtime

Arquitetura recomendada:

```text
QueryMapped
    |
    v
Resolve EntityType + ProfileType + ColumnShape
    |
    v
Generated materializer matches effective mapping?
    | yes
    v
Generated path
    |
    no
    v
Runtime NestedMaterializationPlan fallback
```

O fallback deve ser transparente e diagnosticavel. Ele preserva compatibilidade com maps dinamicos e evita transformar o generator em requisito de runtime.

## Architecture Comparison

| Dimension | A. Runtime-only | B. Generated registration + runtime materializer | C. Generated materializer with runtime fallback | D. Fully generated-only path |
| --- | --- | --- | --- | --- |
| Compatibility | Alta; e a arquitetura atual | Alta; ja existe | Alta se fallback for padrao | Baixa; quebra maps dinamicos/scanning |
| AOT | Limitada por `QueryMapped*` anotado | Registro melhora, materializer nao | Melhor para casos gerados; fallback segue anotado | Melhor potencial, mas perde cobertura |
| Performance | Custo de plano/delegates na primeira query | Igual A para materializacao | Hipotese de menor first-query/hot path nos casos gerados | Melhor potencial, sem fallback |
| Dynamic maps | Suportados | Suportados | Suportados via fallback | Nao suportados |
| Profiles | Suportados runtime | Suportados runtime | Geraveis por `TProfile` + fallback | Apenas profiles gerados |
| Complexity | Media, ja paga | Media | Alta, mas incremental | Muito alta |
| Diagnostics | Runtime authoritative | Runtime authoritative | Precisa explicar generated vs fallback | Compile-time forte, runtime restrito |
| Maintenance | Concentrada no core | Core + generator registro | Core + generator + registry de materializers | Alto risco de dois mundos ou breaking changes |

## AOT/Trimming Assessment

### Proven

- O runtime atual de `QueryMapped*` esta anotado com `RequiresUnreferencedCode` e `RequiresDynamicCode`.
- O generator atual nao gera materializers; ele gera registro e ja foi validado em smokes trimmed anteriores sem warnings FluentMap-owned no caminho gerado.
- A PoC test-only materializa simple/nested/value-object/profile/null sem `Expression.Compile`, sem reflection para members e sem `Activator` no hot path.
- Native AOT runtime completo nao foi validado neste ambiente nas etapas anteriores por ausencia do platform linker C++.

### Likely

- Um materializer gerado para mappings estaticos pode remover `Expression.Compile` do caminho gerado.
- Construtores publicos e setters publicos conhecidos podem ser chamados diretamente, reduzindo dependencia de reflection runtime.
- `MemberPath` gerado como cadeia de symbols reduz necessidade de preservar metadata de propriedades para o hot path.
- Fallback runtime ainda exigira manter as annotations atuais nas APIs que podem cair no caminho runtime.

### Unknown

- Se uma API publica AOT-safe para TypeHandlers arbitrarios pode ser oferecida sem depender de `SqlMapper.TypeHandlerCache<T>.Parse`.
- Se todos os warnings dependency-owned do Dapper seriam removidos em um consumidor real.
- Runtime Native AOT real, porque nao ha validacao local com platform linker C++.
- Como representar conventions customizadas geradas sem executar codigo arbitrario.
- Como validar, no runtime, que a configuracao efetiva ainda corresponde ao materializer gerado quando public mutable dictionaries foram alterados diretamente.

## Performance Hypotheses / Evidence

Evidence:

- A PoC elimina reflection/expression compilation no materializer test-only.
- O runtime atual cacheia planos, portanto o maior ganho esperado e em primeira query e hot path por row, nao em toda chamada igualmente.

Hypothesis:

- startup cost: pode aumentar ligeiramente por registrar manifests/materializers gerados;
- first query cost: deve cair para casos gerados porque nao ha criacao de `NestedMaterializationPlan` nem `Expression.Compile`;
- steady-state throughput: pode melhorar por chamadas diretas e menos indirection;
- allocation: pode reduzir objetos de plano/delegates e arrays de argumentos se construtores forem chamados diretamente;
- memory: pode trocar memoria runtime de cache por IL gerado no assembly consumidor.

Nao ha benchmark formal nesta entrega. Nenhuma afirmacao de performance deve ser tratada como fato ate existir benchmark com Dapper default, `QueryMapped*` runtime e generated path.

## Profile Implications

Profiles combinam bem com geracao porque ja possuem identidade forte por `TProfile`. A geracao deve preservar:

- `Dapper.Query<T>()` usando somente default map;
- `QueryMapped<TEntity,TProfile>()` selecionando profile por operacao;
- nenhum `SqlMapper.SetTypeMap` temporario;
- cache incluindo `ProfileType`;
- conventions/naming policies por entidade ate existir decisao de per-profile conventions.

## Value Object Implications

Geracao ajuda Value Objects por construtor porque o codigo pode ser bottom-up e direto:

```text
leaf scalar values -> Value Object constructor -> root constructor/setter
```

Factory methods tambem poderiam ficar melhores em codigo gerado, mas somente se houver API publica explicita para selecionar a factory. A geracao e positiva para essa feature futura, desde que nao tente inferir factories por nome.

## Streaming Implications

Generated materializer facilita streaming porque separa:

```text
reader lifecycle
    from
row materialization delegate
```

Um futuro streaming/unbuffered path poderia iterar `DbDataReader.Read()` e chamar um materializer gerado por row sem armazenar tudo em `List<TEntity>`.

Ainda assim, streaming exige uma entrega propria para:

- ownership de connection/reader;
- enumeracao lazy sem reader ja disposto;
- async streaming;
- cancellation;
- disposal deterministico;
- comportamento em excecoes durante enumeracao.

Este spike nao implementa streaming.

## Runtime Fallback Strategy

O fallback e obrigatorio.

Regras recomendadas:

- usar generated path apenas quando entity, profile e column shape forem reconhecidos;
- validar que a configuracao efetiva corresponde ao descriptor gerado;
- cair para runtime quando houver map dinamico, convention nao geravel, scanning, assembly externo sem manifest ou shape inesperado;
- expor diagnostico em `Explain` ou API futura para indicar se um shape usaria generated ou runtime;
- preservar annotations RUC/RDC nas APIs que ainda podem usar fallback runtime.

## Compatibility Impact

Uma futura implementacao pode ser minor-compatible se:

- nao remover `QueryMapped*` runtime;
- nao exigir generator para consumidores atuais;
- nao alterar `Dapper.Query<T>()`;
- nao remover public mutable dictionaries;
- nao tornar `Initialize(...)` one-shot;
- nao mudar TypeHandler semantics.

Possivel impacto publico futuro:

- pacote generator precisaria emitir materializer/manifest alem de registro;
- o core pode precisar de uma API publica pequena para registrar/descrever materializers gerados;
- diagnostics podem ganhar metadados de generated/fallback.

## Risks

- O generator pode aceitar apenas um subconjunto da DSL e surpreender consumidores se fallback nao for claro.
- Map constructors sao codigo arbitrario; tentar interpreta-los demais aumenta falso positivo/falso negativo.
- Public mutable dictionaries podem invalidar a correspondencia entre descriptor gerado e configuracao efetiva.
- TypeHandlers continuam sendo o ponto Dapper-specific mais delicado.
- Conventions customizadas e naming policies dinamicas podem limitar a cobertura gerada.
- Geracao por assembly exige desenho para dependencies e duplicidades.
- Sem benchmark, ganho de performance permanece hipotese.
- Sem Native AOT runtime, compatibilidade AOT completa permanece nao provada.

## Recommendation

`GO WITH CONSTRAINTS`

E tecnicamente viavel gerar materializers de `DbDataReader` para um subconjunto estatico dos mappings do FluentMap: explicit maps com colunas literais, profiles tipados, paths por `MemberPath`, nested mutable objects e Value Objects por construtores publicos. A PoC test-only prova a forma essencial do codigo sem reflection/dynamic code no hot path.

As restricoes sao obrigatorias:

- generated materializer deve complementar, nao substituir, o runtime;
- fallback runtime deve permanecer a politica default para maps dinamicos;
- a primeira implementacao deve focar explicit/profile maps geraveis;
- TypeHandler gerado precisa de decisao propria antes de virar contrato;
- AOT deve ser validado por publish/run real antes de remover ou relaxar annotations publicas;
- performance precisa de benchmark antes de claims.

## Proposed Next Stage

Etapa 7 - Generated Materialization

Sequencia sugerida derivada do spike:

1. `Generated Materializer Contract`
   - definir descriptor gerado, lookup por entity/profile/column shape e politica de fallback;
   - decidir impacto publico minimo.
2. `Static Mapping DSL Discovery`
   - estender generator para detectar somente `Map(...).ToColumn("literal")`, `Ignore`, `IncludeBase<TBase>` e `IProfileMap<TProfile>`;
   - emitir diagnostics informativos para maps nao geraveis.
3. `Generated Row Materializer Prototype`
   - gerar materializer para simple root properties, nested mutable objects, immutable constructors e `DBNull` semantics;
   - manter runtime fallback.
4. `Generated Profiles And Diagnostics`
   - cobrir `TProfile`, inherited profile maps, `Explain`/diagnostic de generated vs fallback.
5. `TypeHandler And Conversion Strategy`
   - escolher API/boundary para TypeHandlers sem espalhar reflection;
   - validar nullable handlers.
6. `AOT/Trim And Performance Validation`
   - publish trimmed;
   - Native AOT em ambiente com linker C++;
   - benchmark de startup, first query, throughput, allocation e memory.

Dependencies:

- manter Etapa 6 lifecycle;
- manter snapshots/read-only APIs;
- preservar compatibility boundary do Dapper;
- nao depender de Dommel;
- manter core `netstandard2.0`.

Migration approach:

- generator opt-in;
- runtime permanece autoritativo;
- fallback transparente;
- diagnostics para cobertura gerada.

Testing strategy:

- generator unit tests para discovery e codigo emitido;
- integration tests com SQLite para generated path;
- regression tests comparando runtime e generated para os mesmos SQL shapes;
- tests de fallback dinamico;
- trimmed smoke;
- Native AOT smoke quando ambiente permitir;
- benchmarks separados.

## Validation Results

Environment:

- SDK: `10.0.302`;
- test runner detected: VSTest with xUnit v3;
- core target: `netstandard2.0`;
- test target: `net10.0`.

Localized PoC validation:

```text
dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --configuration Debug --filter "FullyQualifiedName~GeneratedMaterializerSpikeTests"
```

Result:

- success;
- 2 tests passed.

Mandatory validation:

```text
dotnet restore .\Dapper.FluentMap.sln
dotnet build .\Dapper.FluentMap.sln --configuration Release --no-restore
dotnet test .\Dapper.FluentMap.sln --configuration Release --no-build
dotnet pack .\Dapper.FluentMap.sln --configuration Release --no-build --output .\artifacts\packages
```

Results:

- restore: success;
- build: success, 0 warnings, 0 errors;
- tests: success, 231 total tests passed:
  - core: 200;
  - Dommel: 7;
  - analyzers: 9;
  - generators: 14;
  - generated-registration integration: 1;
- pack: success:
  - `Dapper.FluentMap.2.0.0.nupkg`;
  - `Dapper.FluentMap.Dommel.2.0.0.nupkg`;
  - `Dapper.FluentMap.Analyzers.2.0.0.nupkg`;
  - `Dapper.FluentMap.Generators.2.0.0.nupkg`.

Known pack warnings:

- `NU5125` for legacy `PackageLicenseUrl` in core and Dommel;
- NuGet README recommendation for core and Dommel.

These warnings are pre-existing package metadata debt tracked outside this delivery.
