# Especificacao de Performance

Status: SPECIFICATION
Prompt: 7.2
Data: 2026-07-27

## Objetivo

Estabelecer uma baseline reproduzivel para materializacao no FluentMap antes de qualquer generated materializer. Esta baseline mede o comportamento atual e servira como referencia para validar as etapas 7.4, 7.5 e 7.6.

O foco da medicao e:

```text
Dapper ExecuteReader / Query<T> -> IDataReader -> object materialization
```

Nao ha objetivo de otimizar a implementacao produtiva neste prompt.

## Artefatos Validados

- `README.md` documenta `QueryMapped*` como caminho opt-in para nested objects, immutable types, Value Objects e profiles.
- `.sdd/etapa-7/STATUS.md` registra 7.1 como ultimo prompt executado.
- `.sdd/etapa-7/01-generated-materialization-architecture.md` define generated materializer como complemento futuro do runtime materializer.
- `.sdd/etapa-7/DECISIONS.md` registra fallback runtime obrigatorio, lookup por entity/profile/column shape e nao acoplamento a internals do Dapper.
- O codigo atual confirma que `QueryMapped*` ainda usa `SqlMapper.ExecuteReader`, coleta nomes de colunas e usa `NestedMaterializationPlan`.

Nao foi encontrada divergencia funcional entre a documentacao SDD lida e o codigo examinado neste prompt. O generated materializer ainda nao existe, como esperado.

## Cenarios

Os benchmarks devem cobrir pelo menos:

1. Dapper puro
   - `connection.Query<T>()` sem FluentMap registrado.
   - Entidade mutavel com nomes de colunas iguais aos membros.
2. Dapper + FluentMap root mapping
   - `connection.Query<T>()` com `FluentMapper.Initialize(...)` e explicit maps root-level.
   - Mede o custo do type map FluentMap instalado no Dapper sem `QueryMapped*`.
3. `QueryMapped<T>` simples
   - Entidade mutavel root-level usando explicit maps.
   - Mede o caminho FluentMap-controlled mesmo sem nested object.
4. Constructor mapping imutavel
   - Entidade imutavel com construtor publico e explicit maps root-level.
   - Comparar Dapper + FluentMap constructor mapping e `QueryMapped<T>`.
5. Nested object mapping
   - Entidade com objeto aninhado mutavel e ao menos um caminho `Map(x => x.Address.City)`.
   - Mede criacao de subarvore, null semantics e setters compilados.
6. Value object mapping
   - Entidade imutavel com Value Object construido por construtor publico a partir de coluna componente.
   - Mede binding bottom-up e custo de construtor/conversao.

## Dataset

Dataset inicial:

- SQLite em memoria, aberto por benchmark class.
- Uma tabela por familia de entidade para evitar SQL dinamico complexo.
- `RowCount = 1000` por operacao steady state.
- Colunas entre 2 e 6 por linha, suficientes para exercitar:
  - `int`;
  - `long`;
  - `string`;
  - `decimal`;
  - `DateTime`;
  - valores `NULL` em cenarios nested/value object dedicados quando necessario.

O numero de linhas deve ser grande o suficiente para reduzir ruido fixo de chamada e pequeno o suficiente para permitir repeticao local.

## Warmup

Para steady state:

- Inicializar FluentMap uma vez em `GlobalSetup`.
- Criar tabelas e popular dados em `GlobalSetup`.
- Executar uma consulta por cenario em `GlobalSetup` para aquecer:
  - cache do Dapper;
  - type map do Dapper;
  - cache de `NestedMaterializationPlan`;
  - delegates compilados com expression trees;
  - JIT do hot path.

Para cold start:

- Isolar em benchmark class separada.
- Medir uma primeira query em tipos dedicados para evitar cache anterior de Dapper/FluentMap.
- Quando o cenario exigir configuracao, resetar FluentMap e registrar o map dentro da operacao medida.
- Manter o mesmo SQL e dataset, mas interpretar o resultado como custo combinado de configuracao + primeira query.

Cold start nao deve ser comparado diretamente com steady state como se medisse throughput por linha.

## Metricas

Usar BenchmarkDotNet com:

- media e dispersao por operacao;
- Gen0/Gen1/Gen2 quando disponivel;
- allocated bytes por operacao;
- runtime e job registrados no relatorio;
- versoes de dependencias registradas no documento de baseline.

Metricas principais:

- tempo total para materializar `RowCount` linhas;
- bytes alocados por operacao;
- alocacao aproximada por linha;
- relacao entre Dapper puro, Dapper + FluentMap root e `QueryMapped*` runtime.

Metricas secundarias:

- custo de primeira consulta/configuracao;
- diferenca entre root simple, immutable, nested e Value Object;
- estabilidade entre rodadas.

## Comparacao Justa

Regras:

- Usar o mesmo provider SQLite em memoria para todos os cenarios.
- Materializar completamente o resultado em lista/array em todos os metodos.
- Evitar que deferred execution fique fora da medicao.
- Usar SQL equivalente e o mesmo numero de linhas por cenario.
- Separar Dapper normal de `QueryMapped*`, porque `QueryMapped*` e bufferizado por contrato atual.
- Nao usar APIs internas do Dapper.
- Nao alterar codigo produtivo para favorecer benchmark.
- Nao comparar nested/value object com Dapper puro como equivalentes funcionais quando Dapper puro nao materializa o mesmo grafo.

## Reflection, Expression Trees e Caches

Pontos esperados de custo no codigo atual:

- `DefaultTypeMap` criado durante criacao do plano runtime.
- `registry.GetProfilePropertyMap(...)` consultado por coluna.
- `MaterializationPlanCacheKey` por entity/profile/shape ordenado.
- `NestedMaterializationPlan.Create(...)` monta arvore de materializacao.
- `Expression.Compile()` cria factories, getters, setters, constructors e TypeHandler adapters.
- `Convert.ChangeType`, enum/Guid handling e `Activator.CreateInstance` participam da conversao escalar.

Steady state deve aquecer esses custos para medir o hot path atual. Cold start deve evidencia-los como custo inicial.

## Orcamento Inicial de Performance

Este prompt nao define numeros absolutos como requisito. O orcamento inicial e qualitativo e sera refinado com a baseline medida:

- `Dapper + FluentMap root mapping` deve permanecer proximo do Dapper puro para formas root simples, porque usa o pipeline normal do Dapper.
- `QueryMapped<T>` simples pode ser mais caro que Dapper puro, mas seu overhead deve ser entendido e tratado como teto para o caminho gerado futuro.
- Nested object e Value Object devem ser comparados principalmente contra o proprio `QueryMapped*` runtime atual, porque Dapper puro nao entrega o mesmo comportamento sem codigo adicional do usuario.
- Generated materializers futuros devem buscar reduzir alocacoes e tempo do hot path de `QueryMapped*`, especialmente nos cenarios nested, immutable e Value Object.
- Regressao futura relevante deve ser investigada quando ultrapassar o ruido estatistico da baseline e afetar um cenario funcionalmente equivalente.

Depois desta baseline, as etapas futuras devem transformar essas expectativas em limites revisaveis baseados em dados reais.

## Riscos de Benchmarks Artificiais

- SQLite em memoria mede tambem parser/executor SQL e provider, nao apenas materializacao.
- Queries `SELECT` simples podem favorecer cache interno do provider.
- Linhas muito pequenas escondem custo de conversao; linhas muito grandes escondem custo fixo de plano.
- Dados deterministicos podem nao expor todos os caminhos de null/default/conversao.
- Cold start em processo unico nao zera todos os caches estaticos do Dapper.
- BenchmarkDotNet pode produzir resultados diferentes conforme CPU scaling, antivirus, carga da maquina e modo de energia.
- Resultados de uma maquina local nao devem ser usados como promessa publica de performance.

## Repeticoes Futuras

Repetir a rodada representativa:

- apos 7.4, para comparar flat/simple generated materialization contra `QueryMapped<T>` simples;
- apos 7.5, para comparar nested, immutable e Value Object gerados contra runtime;
- apos 7.6, para validar lookup generated/fallback integrado e diagnosticos.
