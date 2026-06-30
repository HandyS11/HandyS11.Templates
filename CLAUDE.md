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
| `handys11-solution` | project | Full solution scaffold (slnx, Directory.Build.props, Directory.Packages.props, global.json, NuGet.config, .editorconfig, .gitignore, .vscode/) |
| `handys11-editorconfig` | item | `.editorconfig` only |
| `handys11-gitignore` | item | `.gitignore` + `.gitattributes` |
| `handys11-github` | item | `.github/` — Dependabot config + Actions workflows (CI, CD, Sonar, Mutation, Documentation). Uses `sourceName` (`Project1`) so `--name` injects the solution name into the workflows |

### Template verification tests

Each test uses `TemplateVerifierOptions` with a `TemplatePath` pointing directly into `src/content/<template>` (no install step needed). Tests generate snapshots under a `Snapshots/` directory; new or changed template output updates these snapshots on first run.

### Release flow

Publishing is tag-driven. Pushing a `v*` tag triggers the CD workflow, which extracts the version from the tag, injects it into the `.csproj`, packs, and pushes to both NuGet.org and GitHub Packages.

The version in `src/HandyS11.Templates.csproj` is a placeholder (`0.1.0`) — do not manually bump it; the CD pipeline sets it from the tag.
