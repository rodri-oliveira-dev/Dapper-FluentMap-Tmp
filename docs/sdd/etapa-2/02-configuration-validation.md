# 02 - Validacao E Diagnosticos De Configuracao

## Specification

Adicionar validacoes estruturadas para configuracoes invalidas e melhorar diagnosticos de erro, preservando configuracoes validas existentes e sem ampliar o escopo funcional do core para materializacao aninhada, ORM ou query builder.

Casos priorizados:

- mesma propriedade ou mesmo `MemberPath` mapeado mais de uma vez;
- caminhos distintos com o mesmo nome terminal;
- coluna duplicada quando a resolucao seria ambigua;
- expression invalida;
- convention ambigua;
- incoerencia de case sensitivity;
- metadata de propriedade incompativel com a entidade;
- pontos que lancavam `Exception` generica.

## Discovery

Arquivos analisados:

- `docs/sdd/etapa-1/README.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-2/README.md`
- `docs/sdd/etapa-2/status.md`
- `docs/sdd/etapa-2/decisions.md`
- `docs/sdd/etapa-2/01-member-path.md`
- `src/Dapper.FluentMap/Mapping/EntityMap.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMap.cs`
- `src/Dapper.FluentMap/Mapping/MemberPath.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMapIdentity.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `src/Dapper.FluentMap/Configuration/FluentConventionConfiguration.cs`
- `src/Dapper.FluentMap/Conventions/Convention.cs`
- `src/Dapper.FluentMap/Conventions/PropertyConventionConfiguration.cs`
- `src/Dapper.FluentMap/Conventions/ConventionPropertyConfiguration.cs`
- `src/Dapper.FluentMap/Utils/ReflectionHelper.cs`
- `src/Dapper.FluentMap/Utils/FluentMapConfigurationExtensions.cs`
- testes existentes do core diretamente relacionados.

Catalogo encontrado:

| Condicao | Excecao anterior | Momento anterior | Classificacao | Detectavel antes |
|---|---|---|---|---|
| Mesmo mapping chamado duas vezes para a mesma propriedade simples | `Exception` generica | construcao do `EntityMap` | erro de configuracao | sim |
| Mesmo `MemberPath` aninhado chamado duas vezes | `Exception` generica | construcao do `EntityMap` | erro de configuracao | sim |
| Dois paths distintos com mesmo terminal, como `Rank.Level` e `Seniority.Level` | falhava antes da Entrega 01; agora valido | configuracao | configuracao valida | sim |
| Dois maps explicitos do core para a mesma coluna | sem erro; primeiro match vencia | resolucao/materializacao | erro de configuracao | sim, no registro do map |
| Dois maps explicitos do core com colunas que colidem por case sensitivity | resultado dependia de ordem e coluna consultada | resolucao/materializacao | erro de configuracao | sim, no registro do map |
| Duas propriedades de uma convention resolvendo para a mesma coluna | `Exception` generica | `GetMember`/materializacao | erro de configuracao | sim, ao registrar a convention |
| Convention sem `Configure(...)` para regra aplicavel | `NullReferenceException` indireta | configuracao de convention | erro de configuracao | sim |
| `Map(...)` com expression que nao e caminho de propriedade | `ArgumentException` | construcao do `EntityMap` | erro imediato | sim |
| Expression nula | `ArgumentNullException` | helper de reflection | erro imediato | sim |
| Predicate/configure/transformer nulos em convention | falhas indiretas ou comportamento silencioso | configuracao | erro imediato de argumento | sim |
| `ToColumn(null)` ou `ToColumn("")` | mapeamento inutil/diagnostico tardio | configuracao | erro imediato de argumento | sim |
| `PropertyMap` sem `PropertyInfo` | `NullReferenceException` no construtor ou falha indireta | configuracao | erro imediato de argumento | sim |
| `IEntityMap` customizado com `PropertyInfo` de outro tipo | sem erro estruturado | resolucao/materializacao | erro de configuracao | sim, no registro do map |
| Registro duplicado de `EntityMap` para a mesma entidade | `InvalidOperationException` | `AddMap` | erro de configuracao | sim |
| `IgnoredPropertyInfo` com membros nao implementados | `NotImplementedException` | uso indevido do sentinel interno | erro de runtime fora do fluxo esperado | parcialmente; fora de escopo |
| Falhas de `FluentMapConfigurationExtensions` ao refletir maps de assemblies | `InvalidOperationException` | apply por assembly | diagnostico de discovery/reflection | parcialmente; fora de escopo funcional desta entrega |

## Decision

Nao foi adicionada API publica `Validate()`.

Motivo:

- os casos prioritarios encontrados sao deterministas e podem falhar cedo durante a configuracao;
- nao ha, nesta entrega, warnings agregaveis que justifiquem um contrato publico novo;
- uma API publica de diagnostico agregado exigiria definir modelo de resultado, estabilidade de mensagens, escopo de warning e interacao com estado global, o que pertence a uma evolucao posterior.

Foi adicionada uma excecao publica:

```text
FluentMapConfigurationException : InvalidOperationException
```

Motivo:

- diferencia erros de configuracao do FluentMap de falhas arbitrarias de runtime;
- preserva compatibilidade razoavel para fluxos que ja tratavam `InvalidOperationException`;
- substitui usos de `Exception` generica em erros de configuracao controlados;
- evita hierarquia extensa.

Classificacao das regras:

| Regra | Decisao |
|---|---|
| `MemberPath` duplicado no mesmo `EntityMap` | erro imediato |
| registro duplicado de `EntityMap` para a mesma entidade | erro imediato |
| coluna duplicada em mappings explicitos do core da mesma entidade | erro imediato no `AddMap` |
| conflito de coluna por case sensitivity em mappings explicitos do core | erro imediato no `AddMap` |
| convention ambigua para a mesma entidade | erro imediato no registro da convention |
| expression invalida | erro imediato com `ArgumentException` |
| argumentos nulos/coluna vazia | `ArgumentNullException` ou `ArgumentException` |
| metadata de propriedade incompativel com entidade | erro imediato no `AddMap` |
| conflitos entre explicit mapping e convention para mesma coluna | fora de escopo como erro; precedencia explicita continua preservada |
| materializacao aninhada | fora de escopo |

Formato de mensagens:

- incluir entidade sempre que o erro for por entidade;
- incluir `MemberPath.ToString()` para caminhos;
- incluir coluna quando o conflito envolver coluna;
- incluir tipo da convention ou entity map quando a origem ajudar;
- nao depender de mensagens internas de reflection ou Dapper para explicar erros do FluentMap.

## Delivery

Arquivos adicionados:

- `src/Dapper.FluentMap/FluentMapConfigurationException.cs`
- `src/Dapper.FluentMap/MappingConfigurationValidator.cs`
- `test/Dapper.FluentMap.Tests/ConfigurationValidationTests.cs`

Arquivos alterados:

- `src/Dapper.FluentMap/Mapping/EntityMap.cs`
- `src/Dapper.FluentMap/Mapping/PropertyMap.cs`
- `src/Dapper.FluentMap/MappingRegistry.cs`
- `src/Dapper.FluentMap/Configuration/FluentMapConfiguration.cs`
- `src/Dapper.FluentMap/Configuration/FluentConventionConfiguration.cs`
- `src/Dapper.FluentMap/Conventions/PropertyConventionConfiguration.cs`
- `src/Dapper.FluentMap/Conventions/ConventionPropertyConfiguration.cs`
- `test/Dapper.FluentMap.Tests/ManualMappingTests.cs`

Implementacao:

- `EntityMapBase.Map(...)` continua falhando cedo para `MemberPath` duplicado, agora com `FluentMapConfigurationException` e mensagem com entidade, path e colunas.
- `MappingConfigurationValidator` valida entity maps antes do registro global e conventions antes de instala-las no registry.
- conflitos de coluna sao detectados quando duas configuracoes do core podem responder pela mesma coluna, incluindo sobreposicao por case-insensitive.
- conventions continuam usando o mesmo criterio de pertencimento da resolucao existente: `ReflectedType` no target atual, com alternativa `DeclaringType` para `NETSTANDARD1_3`.
- argumentos nulos e colunas vazias em APIs fluentes agora falham com excecoes padrao de argumento.
- `ReflectionHelper` manteve `ArgumentException` para expressions invalidas; as mensagens existentes ja indicam que a expression deve resolver para um property path.

## Compatibility

API publica adicionada:

- `Dapper.FluentMap.FluentMapConfigurationException`.

API publica nao adicionada:

- nenhum `FluentMapper.Validate()`;
- nenhum `configuration.Validate()`;
- nenhum `Explain<T>()`.

Comportamento preservado:

- configuracoes validas continuam validas;
- paths distintos com mesmo nome terminal continuam coexistindo;
- extensoes de `IPropertyMap`, como Dommel, podem reutilizar coluna quando possuem semantica adicional propria;
- composicao explicit mapping -> convention -> Dapper default permanece;
- Dommel nao recebeu alteracao funcional;
- `PropertyInfo` publico segue sendo o membro terminal.

Comportamento alterado somente para configuracoes invalidas:

- duplicidades e ambiguidades passam a falhar cedo com diagnostico estruturado;
- alguns argumentos invalidos passam a falhar imediatamente, em vez de produzir erro indireto ou mapping inutil.

## Tests

Testes adicionados cobrem:

- configuracao valida;
- `MemberPath` duplicado;
- paths distintos com mesmo nome terminal;
- registro duplicado de map;
- conflito explicito de coluna;
- conflito de coluna por case sensitivity;
- reutilizacao de coluna por `IPropertyMap` externo, preservando compatibilidade com extensoes;
- convention ambigua;
- expression invalida;
- mensagem com contexto util;
- metadata incompativel;
- convention sem `Configure(...)`.

Como nao houve API `Validate()`, nao ha teste de validacao repetida.

## Validation

Comandos executados durante a entrega:

- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj --filter "FullyQualifiedName~ConfigurationValidationTests"`
  - resultado: sucesso, 10 testes aprovados.
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj`
  - resultado inicial: falha em `ConventionTests.ShouldMapEntitiesInAssembly` porque a validacao de convention usava compatibilidade por `DeclaringType` e classificava mapas herdados de outros tipos como duplicados.
- correcao: a validacao de convention passou a usar o mesmo filtro da resolucao (`ReflectedType == type` em `netstandard2.0`).
- `dotnet test .\test\Dapper.FluentMap.Tests\Dapper.FluentMap.Tests.csproj`
  - resultado: sucesso, 64 testes aprovados.
- `dotnet restore`
  - resultado: sucesso.
- `dotnet build`
  - resultado: sucesso, 0 warnings, 0 erros.
- `dotnet test`
  - resultado inicial: falha em 3 testes do Dommel porque a regra de coluna duplicada no core tambem atingia `DommelPropertyMap`, onde a reutilizacao de coluna possui semantica adicional valida.
- correcao: a validacao de conflito de coluna foi limitada a `PropertyMap` do core e foi adicionado teste de compatibilidade para `IPropertyMap` externo.
- `dotnet test`
  - resultado: sucesso, 65 testes aprovados no core e 7 testes aprovados no Dommel.
- `dotnet build --configuration Release`
  - resultado: sucesso, 0 warnings, 0 erros.
- `dotnet test --configuration Release`
  - resultado: sucesso, 65 testes aprovados no core e 7 testes aprovados no Dommel.

Pack nao foi executado porque nao houve mudanca de empacotamento, metadados NuGet ou targets.

## Limitacoes

- Nao ha diagnostico agregado ou warnings; erros sao fail-fast.
- Conflitos de coluna em implementacoes externas de `IPropertyMap` nao sao tratados como erro pelo core, pois extensoes como Dommel podem atribuir semantica adicional a maps com a mesma coluna.
- Conflitos entre mapping explicito e convention para a mesma coluna permanecem governados pela precedencia existente e nao sao tratados como erro nesta entrega.
- O sentinel interno `IgnoredPropertyInfo` continua fora do escopo.
- `FluentMapConfigurationExtensions.ApplyMapsFromAssemblies` ainda possui diagnosticos proprios de discovery/reflection e nao foi redesenhado.
- Nao foi implementado suporte a materializacao aninhada.
