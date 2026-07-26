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

## Source Generator

- A Entrega 03 pode reutilizar a leitura estatica de `Map(...)`, `ToColumn(...)`, `IncludeBase(...)` e `AddMap<TMap>()`, mas nao deve depender de diagnostics como unica fonte de verdade.
