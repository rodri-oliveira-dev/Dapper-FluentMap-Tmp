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
