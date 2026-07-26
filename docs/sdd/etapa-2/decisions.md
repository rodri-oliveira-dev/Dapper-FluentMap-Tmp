# Decisoes Da Etapa 2

Registre aqui apenas decisoes que afetem entregas posteriores.

## MemberPath

- A identidade interna de uma propriedade mapeada deve usar o caminho completo de propriedades, nao apenas o `PropertyInfo.Name` terminal.
- `IPropertyMap.PropertyInfo` e `PropertyMap.PropertyInfo` permanecem publicos e continuam representando o membro terminal por compatibilidade.
- `MemberPath` nao implica materializacao de objetos aninhados; futuras entregas devem tratar validacao, diagnostico e naming policies sem declarar suporte a nested materialization.
- Comparacoes entre mapping explicito e convention devem usar a identidade de caminho quando disponivel, com fallback para caminho simples baseado no `PropertyInfo` terminal para implementacoes externas de `IPropertyMap`.
- Validacoes e diagnosticos futuros devem preferir `MemberPath.ToString()` para mensagens de caminho, preservando mensagens deterministicas sem depender apenas do nome terminal.
- Heranca de mappings deve comparar membros por caminho e identidade de membro, nao por string terminal.
- Naming policies futuras podem avaliar caminho completo, mas nao devem transformar isso em suporte implicito a materializacao aninhada.

## Validacao E Diagnosticos

- Erros inequivocos de configuracao devem falhar cedo durante construcao do map ou registro em `FluentMapper.Initialize`, sem depender de query ou materializacao pelo Dapper.
- `FluentMapConfigurationException`, derivada de `InvalidOperationException`, e a excecao publica para erros de configuracao estruturados do FluentMap.
- Nao foi adicionada API publica `Validate()` nesta entrega; futuras entregas so devem cria-la se houver diagnosticos agregaveis ou warnings com contrato claro.
- Mensagens de configuracao devem incluir entidade e, quando aplicavel, `MemberPath`, coluna, tipo do map/convention e causa.
- Conflitos de coluna dentro do mesmo entity map do core ou da mesma convention sao invalidos quando mais de uma propriedade pode responder pela mesma coluna, incluindo sobreposicao por case-insensitive.
- Implementacoes externas de `IPropertyMap` nao recebem validacao global de conflito de coluna, porque integracoes como Dommel podem reutilizar colunas com semantica adicional propria.
- Conflitos entre mapping explicito e convention para a mesma coluna continuam fora do escopo de erro imediato e seguem a precedencia explicito -> convention -> Dapper default.

## Heranca De Mappings

- Heranca de mappings e opt-in por `IncludeBase<TBase>()`; nao ha aplicacao automatica de maps de classes base por reflection.
- O map base deve estar registrado antes do map derivado; a ausencia do base map falha cedo com `FluentMapConfigurationException`.
- A composicao de mappings explicitos para um tipo derivado segue a ordem: mappings proprios do derivado, mappings herdados mais proximos, mappings herdados mais distantes.
- A precedencia final passa a ser: mapping explicito do derivado -> mapping explicito herdado -> convention do tipo consultado -> Dapper default.
- Overrides sao definidos por `MemberPath`: quando derivado e base mapeiam o mesmo path, o derivado vence e o mapping base sobrescrito nao participa da resolucao para o derivado.
- Conflitos de coluna entre mappings explicitos do derivado e mappings herdados de paths diferentes sao invalidos e diagnosticados durante o registro do map derivado.
- `IncludeBase<TBase>()` aceita apenas classe base real do tipo mapeado; tipos nao relacionados, o proprio tipo e interfaces ficam fora do contrato desta entrega.
- Naming policies futuras devem respeitar a composicao explicita efetiva antes de aplicar conventions ou fallback.

## Naming Policies

- Naming policy e integrada ao mecanismo existente de conventions; nao ha segundo pipeline de resolucao.
- A API publica usa `NamingPolicy`, uma abstracao leve baseada em delegate, em vez de uma interface publica prematura.
- `FluentMapConfiguration.UseNamingPolicy(...)` retorna `FluentConventionConfiguration`, preservando `.ForEntity<T>()`, `.ForEntitiesInAssembly(...)` e `.ForEntitiesInCurrentAssembly(...)`.
- Built-ins adicionados: `SnakeCase`, `Prefix`, `Suffix` e `Custom`, com composicao por `Then`, `WithPrefix` e `WithSuffix`.
- `SnakeCase` nao altera `DefaultTypeMap.MatchNamesWithUnderscores`; a policy gera `PropertyMap` dentro do FluentMap e evita efeito global silencioso no Dapper.
- A precedencia consolidada e: mapping explicito do derivado -> mapping explicito herdado mais proximo -> mapping explicito herdado mais distante -> convention/naming policy do tipo consultado -> Dapper default.
- Naming policy e convention compartilham o mesmo nivel de precedencia e seguem a ordem de registro entre conventions.
- Transformacao baseada em `MemberPath` completo continua fora do contrato publico, pois a etapa nao implementa materializacao aninhada.
- Invalid policy que produz coluna nula ou vazia falha cedo com `FluentMapConfigurationException` durante a configuracao da entidade.
