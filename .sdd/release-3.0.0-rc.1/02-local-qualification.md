# Local Qualification

## Candidate version

Versao candidata unica: `3.0.0-rc.1`.

O gate local e o workflow de release devem rejeitar `2.0.0`, `3.0.0`,
versoes sem suffix pre-release e qualquer valor diferente de `3.0.0-rc.1`.

## Candidate branch

Branch local de qualificacao: `release/3.0.0-rc.1`.

O commit remoto a ser qualificado no Prompt RC.3 e o commit resultante do
Prompt RC.2, com mensagem `ci(release): qualify 3.0.0-rc.1 artifacts`.

## Build commands

```bash
dotnet restore ./Dapper.FluentMap.sln
dotnet build ./Dapper.FluentMap.sln --configuration Release --no-restore
```

## Test commands

```bash
dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build
dotnet test ./test/Dapper.FluentMap.ProviderCompatibility.Tests/Dapper.FluentMap.ProviderCompatibility.Tests.csproj --configuration Release --no-build
```

Os testes de provider executam SQLite localmente. SQL Server e PostgreSQL
permanecem condicionais a connection strings de ambiente.

## Package commands

```bash
dotnet pack ./Dapper.FluentMap.sln --configuration Release --no-build --output ./artifacts/release-3.0.0-rc.1/rc2 -p:Version=3.0.0-rc.1
```

## Expected artifacts

- 5 `.nupkg`:
  - `Dapper.FluentMap.3.0.0-rc.1.nupkg`
  - `Dapper.FluentMap.Dommel.3.0.0-rc.1.nupkg`
  - `Dapper.FluentMap.DependencyInjection.3.0.0-rc.1.nupkg`
  - `Dapper.FluentMap.Analyzers.3.0.0-rc.1.nupkg`
  - `Dapper.FluentMap.Generators.3.0.0-rc.1.nupkg`
- 3 `.snupkg`:
  - `Dapper.FluentMap.3.0.0-rc.1.snupkg`
  - `Dapper.FluentMap.Dommel.3.0.0-rc.1.snupkg`
  - `Dapper.FluentMap.DependencyInjection.3.0.0-rc.1.snupkg`
- `artifact-manifest.json`
- `dependencies.json`

## Version validation

O workflow recebe `package-version` explicitamente e valida que o valor e
exatamente `3.0.0-rc.1`. O pack usa somente `-p:Version=<package-version>`.

O script `eng/validate-release-artifacts.ps1` valida novamente a versao nos
nomes dos arquivos e nos nuspecs de todos os `.nupkg` e `.snupkg`.

## Package validation

O pack executa a validacao nativa de pacote nos projetos com
`EnablePackageValidation=true`.

O script de artefatos valida:

- contagem exata de 5 `.nupkg` e 3 `.snupkg`;
- ausencia de pacotes de tests, benchmarks e AOT smoke;
- PackageIds esperados;
- versao identica em todos os nuspecs;
- repository URL, branch e commit;
- dependency ranges esperados;
- README presente;
- license MIT;
- layout `lib/netstandard2.0` para runtime packages;
- layout `analyzers/dotnet/cs` para analyzers e generators.

## SourceLink preparation

O build preserva `RepositoryUrl`, `PublishRepositoryUrl`,
`EmbedUntrackedSources`, `Deterministic` e `ContinuousIntegrationBuild` em CI.

O script valida o repository commit gravado nos nuspecs. A validacao completa
de SourceLink contra URL remota permanece bloqueada ate o commit RC.2 existir no
remoto, no Prompt RC.3.

## Provenance preparation

O workflow mantem attestations em job separado, com permissoes limitadas a:

- `contents: read`;
- `id-token: write`;
- `attestations: write`.

O manifest inclui `version`, `repository`, `repositoryUrl`, `commit`, `branch`
e SHA-256 de todos os artefatos.

## Security gates

- Publish segue desabilitado.
- Nao existe `dotnet nuget push` ativo no workflow.
- Nenhum secret NuGet e necessario.
- CI nao usa `pull_request_target`.
- PRs de fork nao possuem caminho de publish.
- O input manual `publish` apenas falha com mensagem explicita.
- Dependencias sao auditadas com `dotnet list package --vulnerable
  --include-transitive`.

## Results

Gate local RC.2 executado em 2026-07-29:

- `dotnet restore ./Dapper.FluentMap.sln`: passou.
- `dotnet list ./Dapper.FluentMap.sln package --vulnerable
  --include-transitive`: passou sem vulnerabilidades reportadas.
- `dotnet build ./Dapper.FluentMap.sln --configuration Release
  --no-restore`: passou com 0 warnings e 0 erros.
- `dotnet test ./Dapper.FluentMap.sln --configuration Release --no-build`:
  passou.
- `dotnet test ./test/Dapper.FluentMap.ProviderCompatibility.Tests/Dapper.FluentMap.ProviderCompatibility.Tests.csproj
  --configuration Release --no-build`: passou com SQLite executado; SQL Server
  e PostgreSQL foram ignorados por falta de connection strings.
- `dotnet pack ./Dapper.FluentMap.sln --configuration Release --no-build
  --output ./artifacts/release-3.0.0-rc.1/rc2-local
  -p:Version=3.0.0-rc.1`: passou e gerou 5 `.nupkg` e 3 `.snupkg`.
- `eng/validate-release-artifacts.ps1`: passou e gerou
  `artifact-manifest.json`.
- Checagens negativas do gate rejeitaram `2.0.0`, `3.0.0` e
  `3.0.0-beta.1`.
- `.github/workflows/ci.yml`, `.github/workflows/release.yml` e
  `.github/dependabot.yml` foram parseados como YAML valido via PyYAML.
- Busca por `dotnet nuget push`, secrets NuGet, `pull_request_target` e
  ambiente de publish ativo nao encontrou matches.

SourceLink remoto e provenance continuam dependentes do push futuro do commit
RC.2, sem publicacao NuGet.
