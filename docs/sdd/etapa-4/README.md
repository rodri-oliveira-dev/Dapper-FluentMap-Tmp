# Etapa 4

## Objetivo

Evoluir o `Dapper.FluentMap` com tooling de build-time e compatibilidade de publicacao, preservando o contrato runtime consolidado nas Etapas 1, 2 e 3.

## Dependencia Das Etapas 1, 2 E 3

Esta etapa depende das decisoes anteriores sobre `MemberPath`, validacao runtime, heranca de mappings, naming policies, registro moderno, constructor mapping, `Validate()`, `Explain()` e provenance de mappings.

Antes de iniciar qualquer entrega desta etapa, leia:

- `docs/sdd/etapa-1/README.md`
- `docs/sdd/etapa-1/decisions.md`
- `docs/sdd/etapa-2/README.md`
- `docs/sdd/etapa-2/decisions.md`
- os relatorios relevantes da Etapa 2
- `docs/sdd/etapa-3/README.md`
- `docs/sdd/etapa-3/decisions.md`
- os relatorios relevantes da Etapa 3
- o relatorio anterior desta pasta, quando existir

## Escopo

Entregas:

1. 01 - Roslyn Analyzers
2. 02 - Trimming e Native AOT
3. 03 - Source Generator

## Compatibilidade

- O pacote principal `Dapper.FluentMap` deve continuar em `netstandard2.0`.
- O runtime do core nao deve ganhar dependencias Roslyn.
- APIs publicas existentes devem ser preservadas.
- `Dapper.FluentMap.Dommel` nao deve receber alteracao funcional nesta etapa salvo necessidade comprovada.
- Diagnostics e IDs publicados passam a ser contrato de tooling e nao devem ser renumerados ou reutilizados.

## Runtime Continua Autoridade

Analyzers nao substituem `Validate()` nem as validacoes fail-fast do runtime.

Motivos:

- o analyzer pode nao estar instalado;
- configuracao pode ser dinamica;
- assembly scanning depende de reflection;
- construtores de maps podem executar logica arbitraria;
- consumidores podem suprimir diagnostics;
- o runtime possui informacoes indisponiveis no compilador.

Regra principal:

```text
Se nao for possivel provar estaticamente, nao reporte como erro.
```

## Resultado da Etapa 4

A Etapa 4 adicionou tooling build-time e validacao de publicacao sem alterar o contrato runtime principal do `Dapper.FluentMap`.

Resumo:

- analyzers Roslyn em `Dapper.FluentMap.Analyzers`, com diagnostics `DFM001` a `DFM005`;
- generator incremental em `Dapper.FluentMap.Generators`, com registro gerado por `AddGeneratedMappings()`;
- diagnostics novos do generator: `DFM006` para mapping candidato ignorado e `DFM007` para duplicidade geravel de entidade;
- core preservado em `netstandard2.0` e sem dependencia Roslyn;
- registro manual e `AddMap<TMap>()` permanecem suportados;
- registro gerado complementa o caminho explicito para evitar assembly scanning;
- assembly scanning permanece suportado como conveniencia reflection-dependent e trimming-sensitive;
- caminho explicito e caminho gerado nao emitiram warnings FluentMap-owned nos smokes trimmed executados;
- Native AOT runtime nao foi validado no ambiente local porque faltou o platform linker C++ exigido pelo SDK;
- pacotes de analyzer/generator sao empacotados em `analyzers/dotnet/cs`, sem `lib/`.

Relatorios:

- `docs/sdd/etapa-4/01-roslyn-analyzers.md`
- `docs/sdd/etapa-4/02-trimming-aot.md`
- `docs/sdd/etapa-4/03-source-generator.md`
