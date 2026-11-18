# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Restore, build, test
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --verbosity normal

# Run a single test class
dotnet test --filter "FullyQualifiedName~SolutionTemplateTests"

# Check formatting (fails if changes needed)
dotnet format --no-restore --verify-no-changes

# Apply formatting
dotnet format --no-restore

# Pack the NuGet template package
dotnet pack --configuration Release --output ./artifacts
```

## Architecture

This repo is a **NuGet template package** for `dotnet new`. It contains no application code — the `src/` project is a packaging project only.

### Structure

- `src/` — packaging project (`PackageType=Template`). Contains no C# source. Its `content/` folder is what gets packed and installed.
- `src/content/<template-name>/` — one folder per template, each with a `.template.config/template.json` manifest.
- `tests/` — xUnit project using `Microsoft.TemplateEngine.Authoring.TemplateVerifier` to instantiate templates in isolation and verify their output via snapshot testing.

### Templates

| Short name | Type | Description |
|---|---|---|
| `handys11-solution` | project | Full solution scaffold (slnx + `.slnx.DotSettings` ReSharper cleanup profile, Directory.Build.props, Directory.Packages.props, global.json, NuGet.config, .editorconfig, .gitignore, .vscode/, `.config/dotnet-tools.json`, `.githooks/pre-push`, stryker-config.json, .markdownlint.json) |
| `handys11-editorconfig` | item | `.editorconfig` only |
| `handys11-gitignore` | item | `.gitignore` + `.gitattributes` |
| `handys11-github` | item | `.github/` — Dependabot config, release-notes categories (`release.yml`) + Actions workflows (CI, CD, Sonar, Mutation, Documentation, PR Labels). Uses `sourceName` (`Project1`) so `--name` injects the solution name into the workflows. The workflows rely on the tools manifest, `.DotSettings` and `stryker-config.json` shipped by `handys11-solution`. Tests run by kind (`tests/Project1.UnitTests`, `.IntegrationTests`, `.AcceptanceTests`, `.EndToEndTests`), each step guarded by `hashFiles`, with a solution-wide fallback when none of the four exists; Mutation runs Stryker once per project the unit tests reference. Every `uses:` is pinned to a commit SHA with a `# vX.Y.Z` comment: resolve a new pin with `gh api repos/<owner>/<action>/commits/<tag> --jq .sha` |
| `handys11-aspire` | project | `tools/<Name>.Aspire` AppHost + `tools/<Name>.ServiceDefaults`, run from a `handys11-solution` root (inherits its `Directory.Build.props`). Uses `sourceName` (`Project1`); post-actions run `dotnet add package` (CPM writes the versions into the root `Directory.Packages.props`) and `dotnet sln add` (`/tools/` folder). `--files-only` skips all post-actions — the snapshot tests use it, with fixed ports, to stay offline and deterministic. Pins bumped by hand: `Aspire.AppHost.Sdk/13.6.5` (AppHost csproj) and `Microsoft.Extensions.Http.Resilience`/`ServiceDiscovery` `10.11.0` (template.json); the `OpenTelemetry.*` packages are unpinned (latest stable) |

The `.editorconfig`, `.gitignore` and `.gitattributes` files exist twice (item template + solution template) and must be kept identical.

### Template verification tests

Each test uses `TemplateVerifierOptions` with a `TemplatePath` pointing directly into `src/content/<template>` (no install step needed). Tests generate snapshots under a `Snapshots/` directory; new or changed template output updates these snapshots on first run.

### Release flow

Publishing is tag-driven. Pushing a `v*` tag triggers the CD workflow, which extracts the version from the tag, injects it into the `.csproj`, packs, and pushes to both NuGet.org and GitHub Packages.

The version in `src/HandyS11.Templates.csproj` is a placeholder (`0.1.0`) — do not manually bump it; the CD pipeline sets it from the tag.
