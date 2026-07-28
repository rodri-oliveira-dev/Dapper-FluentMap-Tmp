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

## Em andamento

Nenhuma feature produtiva em andamento. Esta passada e SDD/arquitetura.

## Proximos passos

1. Implementar metadata/contracts aditivos de conversao sem alterar
   comportamento.
2. Adicionar read conversion no runtime materializer com testes de regressao.
3. Evoluir generated read conversion ou fallback seguro quando houver converter.
4. Cobrir profile, inherited maps, nested leaves e Value Objects.
5. Investigar e implementar write conversion/Dommel somente apos definir hook
   de parametros por propriedade.
6. Evoluir diagnostics/analyzers.
7. Medir performance e documentar API publica.

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

## APIs propostas

APIs conceituais, ainda nao implementadas:

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

Contratos conceituais:

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

## Validacao do Prompt 10.1

- `dotnet restore ./Dapper.FluentMap.sln`: sucesso.
- `dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore`:
  sucesso, 0 warnings, 0 errors.
- `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build`:
  sucesso, 357 testes aprovados no total.
- `dotnet pack`: nao executado; este prompt alterou somente documentacao SDD e
  nao mudou empacotamento ou codigo produtivo.

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
- `.sdd/etapa-10/DECISIONS.md`
- `.sdd/etapa-10/STATUS.md`
- `src/Dapper.FluentMap/Materialization/NestedMaterializationPlan.cs`
- `src/Dapper.FluentMap/Materialization/MappedRowMaterializer.cs`
- `src/Dapper.FluentMap/Compatibility/DapperTypeHandlerAdapter.cs`
- `src/Dapper.FluentMap.Generators/MappingRegistrationGenerator.cs`
- `src/Dapper.FluentMap.Analyzers/FluentMapConfigurationAnalyzer.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMap.cs`
- `src/Dapper.FluentMap/Mapping/PropertyPersistenceMetadata.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap.Dommel/Resolvers/DommelPersistenceSqlBuilder.cs`
- `src/Dapper.FluentMap.Dommel/Resolvers/DommelPersistenceMetadata.cs`
- `src/Dapper.FluentMap.Dommel/Resolvers/DommelPropertyResolver.cs`
- `test/Dapper.FluentMap.Tests/ValueObjectMaterializationTests.cs`
- `test/Dapper.FluentMap.Tests/AdvancedQueryHardeningTests.cs`

## Ultimo prompt executado

Ultimo prompt executado: 10.1
