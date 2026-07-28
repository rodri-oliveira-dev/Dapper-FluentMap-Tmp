# Etapa 10 Status

## Objetivo

Definir discovery, boundaries e arquitetura inicial para Property Conversion &
Extensibility, preservando `TypeHandler<T>` do Dapper como mecanismo global por
tipo e abrindo espaco para conversao por propriedade, map e profile.

## Concluido

- Executado `git status` antes de alteracoes.
- Confirmada branch `feature/etapa-3`; nao estamos em `master`.
- Identificado item nao rastreado preexistente `src/Dapper.FluentMap/etapas/`,
  deixado intacto.
- Lido `README.md`.
- Examinada `Dapper.FluentMap.sln`.
- Examinados projetos core, Dommel, analyzers, generators, testes, smoke AOT e
  benchmarks por arquivos de projeto e pontos de implementacao relevantes.
- Examinado runtime materialization em `NestedMaterializationPlan` e
  `MappedRowMaterializer`.
- Examinado generated materialization em `GeneratedMaterializerDescriptor`,
  `GeneratedMaterializerColumn` e `MappingRegistrationGenerator`.
- Examinado source generator, incluindo fluent-chain parsing e helper `Read<T>`.
- Examinado analyzer, incluindo fluent-chain parsing e metadata de persistencia.
- Examinada persistence metadata em `PropertyPersistenceMetadata`.
- Examinada integracao Dommel em property/key/column/table resolvers e SQL
  builder de persistencia.
- Lidos `.sdd/etapa-9/FINAL-REPORT.md` e `.sdd/etapa-9/STATUS.md`.
- Consultado `.sdd/etapa-8/FINAL-REPORT.md`.
- Confirmado que `.sdd/etapa-10/` nao existia e criada a pasta.
- Consultadas fontes de ecossistema: Dapper TypeHandler, RepoDB PropertyHandler
  e EF Core ValueConverter.
- Criado `.sdd/etapa-10/01-conversion-landscape.md`.
- Criado `.sdd/etapa-10/02-property-conversion-spec.md`.
- Criado `.sdd/etapa-10/DECISIONS.md`.
- Criado `.sdd/etapa-10/STATUS.md`.
- Criado `.sdd/etapa-10/03-converter-contract-design.md`.
- Implementados contratos publicos de conversao por propriedade:
  `IReadPropertyConverter<TDatabase, TProperty>`,
  `IWritePropertyConverter<TProperty, TDatabase>`,
  `IPropertyConverter<TDatabase, TProperty>`,
  `ReadPropertyConverter<TDatabase, TProperty>` e
  `WritePropertyConverter<TProperty, TDatabase>`.
- Implementada fluent API em `PropertyMapBase<TPropertyMap>`:
  `ConvertFromDatabaseUsing`, `ConvertToDatabaseUsing` e `ConvertUsing`.
- Implementada metadata aditiva:
  `PropertyConversionMetadata`, `PropertyConverterMetadata`,
  `PropertyConversionDirection` e `IPropertyMapWithConversionMetadata`.
- `MemberMappingExplanation` passou a expor `Conversion`.
- Registry passa a rejeitar generated materializer manual quando o mapping
  efetivo possui read converter, evitando execucao generated sem conversao.
- Adicionados testes de metadata/fluent API/read-only/write-only/bidirecional,
  delegates, lifetime por property map, heranca, profile, invalid types,
  duplicidade, nullability, profile collision e generated fallback defensivo.

## Em andamento

Execucao real de converters em runtime materializer, generated materializer e
write/Dommel permanece adiada para incrementos seguintes.

## Proximos passos

1. Adicionar read conversion no runtime materializer com testes de regressao.
2. Evoluir generated read conversion ou fallback seguro quando houver converter.
3. Cobrir nested leaves e Value Objects com execucao real de converter.
4. Investigar e implementar write conversion/Dommel somente apos definir hook
   de parametros por propriedade.
5. Evoluir diagnostics/analyzers para reconhecer `Convert...`.
6. Medir performance e documentar API publica no README quando a execucao for
   ativada.

## Decisoes relevantes

- Property converter nao substitui `TypeHandler<T>`.
- Read e write conversion sao direcoes independentes.
- Converter local tem precedencia sobre `TypeHandler<T>` apenas na direcao e
  propriedade/profile configurados.
- `TypeHandler<T>` continua recomendado para Value Objects escalares com
  representacao global uniforme.
- Runtime read conversion deve vir antes de generated read conversion.
- Generated materializer deve cair para runtime fallback quando converter nao
  puder ser emitido com seguranca.
- Dommel/write conversion e incremento separado porque a integracao atual nao
  transforma valores de parametros por propriedade.
- Converters sao stateless/thread-safe por contrato e reutilizados.
- AOT exige caminho por instancia/delegate ou referencia estatica gerada; nao
  deve depender de ativacao reflection-only.
- Prompt 10.2 decidiu implementar somente contracts/metadata/fluent API e
  diagnostics, mantendo execucao de conversores para incremento posterior.

## APIs implementadas no Prompt 10.2

```csharp
Map(x => x.Status)
    .ToColumn("status")
    .ConvertFromDatabaseUsing<LegacyStatusReader, string>();
```

```csharp
Map(x => x.Status)
    .ToColumn("status")
    .ConvertToDatabaseUsing<LegacyStatusWriter, string>();
```

```csharp
Map(x => x.Status)
    .ToColumn("status")
    .ConvertUsing<LegacyStatusConverter, string>();
```

Contratos implementados:

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

## Riscos conhecidos

- Runtime e generated ja possuem conversao duplicada; TypeHandler funciona no
  runtime mapped, mas nao no helper gerado atual.
- `DapperTypeHandlerAdapter` consulta detalhe interno `SqlMapper.TypeHandlerCache<T>.Parse(object)`;
  isso e uma fronteira de compatibilidade sensivel.
- Write conversion por propriedade nao e trivial em Dommel porque o hook atual
  controla colunas/SQL, nao `DbParameter.Value`.
- Null conversion opt-in pode quebrar semantica de subarvores aninhadas se for
  introduzida cedo demais.
- Converter por reflection precisa de anotacoes de trimming e estrategia AOT.
- Caches atuais assumem configuracao efetivamente imutavel apos registro.
- Converter metadata ja existe, mas `QueryMapped*` ainda nao executa os
  conversores. Isso e intencional no Prompt 10.2 para evitar divergencia
  runtime/generated antes da proxima implementacao.
- `Convert...Using<TConverter, TDatabase>()` valida contrato por reflection de
  interfaces em configuration time; overloads por instancia/delegate oferecem
  caminho mais favoravel a AOT.

## Validacao do Prompt 10.1

- `dotnet restore ./Dapper.FluentMap.sln`: sucesso.
- `dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 357 testes aprovados no total.
- `dotnet pack`: nao executado; este prompt alterou somente documentacao SDD e
  nao mudou empacotamento ou codigo produtivo.

## Validacao do Prompt 10.2

- `dotnet test ./test/Dapper.FluentMap.Tests/Dapper.FluentMap.Tests.csproj --configuration Release --filter FullyQualifiedName~PropertyConversionMetadataTests`:
  sucesso, 17 testes aprovados.
- `dotnet restore ./Dapper.FluentMap.sln`: sucesso.
- `dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 374 testes aprovados no total.
- `dotnet pack ./src/Dapper.FluentMap/Dapper.FluentMap.csproj --configuration Release --no-build --output ./artifacts/packages`:
  sucesso, pacote criado em `artifacts/packages/Dapper.FluentMap.2.0.0.nupkg`;
  warning conhecido `NU5125` sobre `licenseUrl` depreciado.

## Interacao com Dapper TypeHandler

Precedencia proposta para `QueryMapped*`:

```text
property read converter
    -> Dapper TypeHandler<TProperty>
    -> FluentMap default conversion
```

Precedencia proposta para escrita futura:

```text
property write converter
    -> Dapper TypeHandler<TProperty>
    -> Dapper/provider parameter default
```

APIs normais do Dapper continuam fora do controle property-scoped do FluentMap:

```text
connection.Query<T>()
    -> Dapper type map para nomes/construtores
    -> Dapper/provider conversion
```

## Arquivos importantes

- `.sdd/etapa-10/01-conversion-landscape.md`
- `.sdd/etapa-10/02-property-conversion-spec.md`
- `.sdd/etapa-10/03-converter-contract-design.md`
- `.sdd/etapa-10/DECISIONS.md`
- `.sdd/etapa-10/STATUS.md`
- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`
- `src/Dapper.FluentMap/Materialization/MappedRowMaterializer.cs`
- `src/Dapper.FluentMap/Compatibility/DapperTypeHandlerAdapter.cs`
- `src/Dapper.FluentMap.Generators/MappingRegistrationGenerator.cs`
- `src/Dapper.FluentMap.Analyzers/FluentMapConfigurationAnalyzer.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMap.cs`
- `src/Dapper.FluentMap/Mapping/PropertyConversionMetadata.cs`
- `src/Dapper.FluentMap/Mapping/PropertyPersistenceMetadata.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap.Dommel/Resolvers/DommelPersistenceSqlBuilder.cs`
- `src/Dapper.FluentMap.Dommel/Resolvers/DommelPersistenceMetadata.cs`
- `src/Dapper.FluentMap.Dommel/Resolvers/DommelPropertyResolver.cs`
- `test/Dapper.FluentMap.Tests/ValueObjectMaterializationTests.cs`
- `test/Dapper.FluentMap.Tests/AdvancedQueryHardeningTests.cs`
- `test/Dapper.FluentMap.Tests/PropertyConversionMetadataTests.cs`

## Ultimo prompt executado

Ultimo prompt executado: 10.2
