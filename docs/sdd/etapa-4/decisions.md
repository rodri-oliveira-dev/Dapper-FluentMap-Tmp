# Decisoes Da Etapa 4

Registre aqui apenas decisoes que afetem entregas posteriores.

## Roslyn Analyzers

- Analyzers sao entregues em projeto e pacote isolado `Dapper.FluentMap.Analyzers`, sem referencia do core para Roslyn.
- A primeira versao dos diagnostics usa o prefixo `DFM` e IDs `DFM001` a `DFM005`.
- Todos os diagnostics iniciais sao `Error`, mas somente para situacoes provadas estaticamente com alto grau de confianca.
- Duplicidade de `MemberPath` e conflito de coluna sao analisados apenas para chamadas diretas de `Map(...).ToColumn(...)` em statements diretos do construtor do `EntityMap`.
- O analyzer nao executa codigo de usuario, nao instancia maps, nao faz reflection runtime e nao acessa banco.
- Regras dependentes de fluxo de execucao, chamadas auxiliares, scanning de assembly, construtores de maps, ordem real de registro ou estado global permanecem sob autoridade de `Validate()` e das validacoes runtime.
- Nao foi criado Code Fix Provider porque nenhuma correcao inicial e inequivoca sem risco de alterar semantica.

## Trimming E Native AOT

- A Entrega 02 deve considerar que o analyzer ja identifica alguns usos estaticamente invalidos de `AddMap<TMap>()`, mas isso nao remove a divida de reflection documentada na Etapa 3.
- Registro explicito por `AddMap<TMap>()` e o caminho recomendado para consumidores com IL trimming e Native AOT; ele nao emite warnings FluentMap-owned no smoke trimmed depois desta entrega.
- Assembly scanning permanece reflection-dependent por design e foi marcado com `RequiresUnreferencedCode` em `AddMapsFromAssembly(...)`, `AddMapsFromAssemblyContaining<TMarker>()`, `ForEntitiesInAssembly(...)`, `ForEntitiesInCurrentAssembly(...)` e `ApplyMapsFromAssemblies(...)`.
- O registry nao cria mais type maps por `Activator.CreateInstance(typeof(FluentMapTypeMap<>).MakeGenericType(type))`; um type map interno nao generico remove esse ponto de reflection dinamica sem remover a classe publica `FluentMapTypeMap<TEntity>`.
- `DynamicallyAccessedMembers` foi aplicado somente a fluxos verificaveis: interfaces do tipo de map em `AddMap<TMap>()`, propriedades publicas em `ForEntity<TEntity>()`, e propriedades/construtores publicos em `Explain<TEntity>()`.
- Warnings restantes no smoke trimmed explicito pertencem ao Dapper (`DefaultTypeMap`, `CustomPropertyTypeMap`, `DapperRow` e helpers internos); nao devem ser corrigidos copiando ou alterando codigo do Dapper dentro do FluentMap.
- Native AOT runtime nao foi validado nesta entrega porque o ambiente Windows nao possui o platform linker C++ exigido pelo SDK.
- Metadata candidata para Source Generator na Entrega 03: entidade alvo de `AddMap<TMap>()`, caminhos `Map(...)`, colunas `ToColumn(...)`, `Ignore()`, `IncludeBase<TBase>()`, naming policies estaticas e instalacao de type maps sem discovery por assembly.
- O Source Generator nao deve tentar tornar `AddMapsFromAssembly(...)` AOT-safe; ele deve oferecer um caminho gerado/explicito que substitua scanning quando o consumidor desejar publicacao trimmed/AOT.

## Source Generator

- A Entrega 03 pode reutilizar a leitura estatica de `Map(...)`, `ToColumn(...)`, `IncludeBase(...)` e `AddMap<TMap>()`, mas nao deve depender de diagnostics como unica fonte de verdade.
- O Source Generator foi entregue em projeto separado `Dapper.FluentMap.Generators`, empacotado em `analyzers/dotnet/cs`, sem referencia do core para Roslyn.
- A descoberta inicial e limitada a classes de mapping declaradas na compilacao atual; assemblies referenciados nao sao percorridos automaticamente.
- A API gerada e `Dapper.FluentMap.DapperFluentMapGeneratedRegistration.AddGeneratedMappings(...)`, exposta como extension method interno no assembly consumidor.
- O codigo gerado chama somente `FluentMapConfiguration.AddMap<TMap>()`, preservando o `MappingRegistry`, validacoes runtime, inheritance, conventions, naming policies e constructor mapping existentes.
- O generator e incremental, baseado em symbols, nao executa codigo do consumidor e nao instancia mappings durante a geracao.
- O caminho gerado nao usa assembly scanning, `GetTypes`, `GetExportedTypes` ou `Activator.CreateInstance(Type)`.
- `DFM005` foi reutilizado para tipos que nao satisfazem o contrato de exatamente uma interface fechada `IEntityMap<TEntity>` para entidade class.
- Novos diagnostics de generator: `DFM006` para mapping candidato ignorado no registro gerado e `DFM007` para duplicidade de entity maps geraveis na compilacao atual.
- Abstract maps, open generic maps, maps inacessiveis e maps sem construtor publico sem parametros sao reportados por `DFM006` e ignorados, evitando que o caminho gerado produza chamadas que nao compilam.
- O generator nao declara suporte a nested object materialization, Value Objects complexos, multiple mapping profiles, query-specific mappings, custom materializer ou generated `DbDataReader` materializer.
