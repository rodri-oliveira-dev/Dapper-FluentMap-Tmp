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
