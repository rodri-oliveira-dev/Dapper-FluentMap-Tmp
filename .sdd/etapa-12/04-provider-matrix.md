# Provider Compatibility Matrix

Documento criado em 2026-07-29 para o Prompt 12.3.

## Provider-Agnostic Design vs Provider Validated/Certified

`Provider-agnostic design` significa que o core usa contratos comuns de
ADO.NET, Dapper e `DbDataReader`, sem codificar comportamento especifico de
banco no materializador. Esse desenho e necessario para portabilidade, mas nao
prova que um provider concreto preserva os mesmos tipos CLR, lifetime de reader,
multiple result sets, cancellation, identity retrieval ou SQL gerado pelo
Dommel.

`Provider validated/certified` significa que existe teste de integracao real
contra aquele provider e banco, executado localmente ou em CI, cobrindo os
cenarios declarados. Mocks de ADO.NET, `DataTableReader` e apenas SQL builders
registrados contam como cobertura provider-independent ou support-by-design, nao
como certificacao de provider.

## Status Vocabulary

| Status | Meaning |
| ------ | ------- |
| `Validated` | Teste real automatizado passou no ambiente registrado. |
| `Partial` | Ha harness, builder ou cobertura parcial, mas falta execucao real completa ou CI. |
| `Not validated` | Nao ha evidencia de teste real executado para este provider neste prompt. |
| `Unsupported upstream` | Limitacao do provider/banco ou dependencia upstream impede tratar como bug do FluentMap. |

## Matrix

| Provider | Basic Read | Nested | Constructor | QueryMultiple | Streaming | Persistence | Status |
| -------- | ---------- | ------ | ----------- | ------------- | --------- | ----------- | ------ |
| SQLite (`Microsoft.Data.Sqlite`) | `Validated`: column rename, null, Guid, DateTime, decimal | `Validated`: nested object and value object | `Validated`: immutable constructor | `Validated`: sequential result sets through `QueryMultipleMapped` | `Validated`: sync early termination, async cancellation, reader release | `Validated`: Dommel identity, non-identity key, computed, database default, read-only | `Validated` |
| SQL Server (`Microsoft.Data.SqlClient`) | Conditional harness via `DFM_SQLSERVER_CONNECTION_STRING`; not executed locally/CI in this prompt | Conditional harness | Conditional harness | Conditional harness; subject to provider multiple-result behavior | Conditional harness | Conditional Dommel harness; identity/default/computed SQL Server DDL defined | `Not validated` |
| PostgreSQL (`Npgsql`) | Conditional harness via `DFM_POSTGRESQL_CONNECTION_STRING`; not executed locally/CI in this prompt | Conditional harness | Conditional harness | Conditional harness; subject to Npgsql multiple-result behavior | Conditional harness | Conditional Dommel harness; identity/default/generated column PostgreSQL DDL defined | `Not validated` |
| MySQL/MariaDB | No harness added; Dommel builder remains registered support-by-design only | No harness | No harness | Not evaluated | Not evaluated | Not evaluated | `Not validated` |
| SQL Server CE | Dommel builder remains registered for compatibility, but no modern provider lane exists | Not evaluated | Not evaluated | Not evaluated | Not evaluated | Not evaluated | `Unsupported upstream` |

## Test Strategy

Foi criado `test/Dapper.FluentMap.ProviderCompatibility.Tests` para concentrar
testes reais de provider sem poluir a suite core. O projeto:

- roda SQLite sempre, usando `Microsoft.Data.Sqlite` in-memory;
- inclui SQL Server e PostgreSQL como lanes condicionais por connection string;
- usa providers reais de ADO.NET, nao mocks;
- cobre leitura basica, materializacao avancada, `QueryMultipleMapped`,
  streaming sync/async e persistencia Dommel;
- desabilita paralelismo no assembly porque `FluentMapper`, `SqlMapper` e
  `DommelMapper` usam estado global process-wide.

Connection strings opcionais:

```text
DFM_SQLSERVER_CONNECTION_STRING
DFM_POSTGRESQL_CONNECTION_STRING
```

Sem essas variaveis, as lanes SQL Server/PostgreSQL sao marcadas como skipped
com diagnostico explicito. Isso nao e certificacao; apenas preserva um harness
executavel quando a infraestrutura real existir.

## CI

A CI passa a executar o projeto de provider compatibility no job
`compatibility`. No estado atual, isso certifica SQLite na lane rapida e registra
SQL Server/PostgreSQL como nao executados quando as connection strings nao
existem.

Nao foram adicionados Testcontainers nem service containers neste prompt porque:

- o repositorio nao possuia infraestrutura existente para containers de banco;
- SQL Server container aumenta tempo e custo do build principal;
- PostgreSQL local existia na maquina, mas pertencia a outro stack em execucao e
  nao foi usado como fonte de verdade;
- MySQL/MariaDB nao tinha dependencia, imagem ou demanda suficiente para virar
  obrigatorio agora.

Proximo passo recomendado: criar um job separado `provider-infrastructure` com
service containers para SQL Server e PostgreSQL, timeouts proprios e artefatos
de log, antes de mudar esses providers para `Validated`.

## Provider Differences Documented

- Identity retrieval pertence ao banco/provider e ao SQL builder do Dommel; o
  core nao normaliza esse comportamento.
- Boolean representation nao recebeu claim provider-certified neste prompt; os
  testes novos focam em rename/null/Guid/DateTime/decimal e materializacao
  avancada.
- `DateTime` pode variar em precision, timezone e kind por provider. Os testes
  usam valores sem fracao sub-millisecond e sem timezone para evitar prometer
  semantica acima do provider.
- Case sensitivity e quoted identifiers nao foram normalizados. Os nomes de
  tabelas/colunas dos testes evitam quoting para validar o caminho comum.
- Multiple result sets sao tratados como provider capability. Se um provider
  falhar por limitacao upstream, deve ser registrado como provider limitation,
  nao como bug automatico do FluentMap.
- Async streaming propaga cancellation aos pontos ADO.NET que aceitam token, mas
  providers podem implementar async internamente de modo sincrono. O FluentMap
  nao promete cancelamento mais forte que o contrato do provider.
