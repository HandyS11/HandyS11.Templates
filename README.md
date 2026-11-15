# HandyS11.Templates

A .NET template package providing project and item templates for use with the `dotnet new` CLI.

## Installation

```bash
dotnet new install HandyS11.Templates
```

## Update

`dotnet new update` checks **all** installed template packages for updates — it takes no package argument:

```bash
dotnet new update
```

To force a specific package to its latest version, reinstall it:

```bash
dotnet new install HandyS11.Templates
```

## Templates

### `handys11-solution` — Empty Solution

A starter kit that scaffolds a new solution folder pre-configured with common project-level files.

| Type    | Language | Platforms               |
|---------|----------|-------------------------|
| Project | C#       | Windows · Linux · macOS |

**Scaffolded files:**

- `*.slnx` — solution file
- `*.slnx.DotSettings` — ReSharper settings, including the `ReformatAndReorder` cleanup profile
- `Directory.Build.props` — shared MSBuild properties (analyzers, warnings as errors, XML docs)
- `Directory.Packages.props` — centralized package version management
- `global.json` — minimum .NET SDK version
- `NuGet.config` — NuGet feed configuration
- `.config/dotnet-tools.json` — local tools: ReSharper CLI (`jb`), Stryker, DocFX
- `.githooks/pre-push` — blocks a push when ReSharper cleanup would change the pushed files
- `stryker-config.json` — Stryker mutation testing settings
- `.markdownlint.json` — Markdown lint rules
- `.editorconfig` — code style settings
- `.gitignore` — Git ignore rules
- `.gitattributes` — Git attribute settings
- `.vscode/` — VS Code workspace settings

The first local build runs `git config core.hooksPath .githooks`, so the pre-push hook is active
without any manual step (skipped on CI).

**Usage:**

```bash
dotnet new handys11-solution --name <SolutionName>
```

---

### `handys11-editorconfig` — .editorconfig File

An `.editorconfig` item template with opinionated code style settings.

| Type | Platforms               |
|------|-------------------------|
| Item | Windows · Linux · macOS |

**Usage:**

```bash
dotnet new handys11-editorconfig
```

---

### `handys11-gitignore` — .gitignore File

A `.gitignore` item template tailored for .NET projects.

| Type | Platforms               |
|------|-------------------------|
| Item | Windows · Linux · macOS |

**Usage:**

```bash
dotnet new handys11-gitignore
```

---

### `handys11-github` — GitHub Workflows

An item template that scaffolds a `.github/` folder with GitHub Actions workflows, release-notes
categories and Dependabot configuration.

| Type | Platforms               |
|------|-------------------------|
| Item | Windows · Linux · macOS |

**Scaffolded files:**

- `.github/dependabot.yml` — weekly Dependabot updates, one grouped PR each for NuGet and GitHub Actions
- `.github/release.yml` — groups generated release notes by PR label (Features, Fixes, Documentation, Dependencies, Internal)
- `.github/workflows/CI.yml` — build, ReSharper format check & test on Ubuntu/Windows with coverage
- `.github/workflows/CD.yml` — tag-driven (`v*`) NuGet pack, publish & GitHub Release
- `.github/workflows/Sonar.yml` — SonarQube analysis (requires Sonar server + secrets)
- `.github/workflows/Mutation.yml` — Stryker mutation testing (manual + weekly)
- `.github/workflows/Documentation.yml` — DocFX build & GitHub Pages deploy
- `.github/workflows/PrLabels.yml` — labels each PR from its Conventional Commits title (`feat`, `fix`, `docs`)

Pass `--name` to inject the solution name into the workflows (`<Name>.slnx`); it defaults to the
target folder name. Use the same name as `handys11-solution` for a matching pair: the workflows run
the local tools and the ReSharper cleanup profile that `handys11-solution` ships.

**Usage:**

```bash
dotnet new handys11-github --name <SolutionName>
```

---

### `handys11-aspire` — Aspire AppHost & ServiceDefaults

A project template that adds .NET Aspire to a solution created with `handys11-solution`: an AppHost
and a ServiceDefaults project, both under `tools/`.

| Type    | Language | Platforms               |
|---------|----------|-------------------------|
| Project | C#       | Windows · Linux · macOS |

**Scaffolded files:**

- `tools/<Name>.Aspire/` — the AppHost (`Aspire.AppHost.Sdk/13.6.5`): `AppHost.cs`, `appsettings.json`,
  `aspire.config.json` (for the Aspire CLI) and `Properties/launchSettings.json` (`https` and `http` profiles,
  ports picked at random per solution). `dotnet run` works without the Aspire CLI installed.
- `tools/<Name>.ServiceDefaults/` — `AddServiceDefaults()` (OpenTelemetry, health checks, service discovery,
  resilient HTTP clients) and `MapDefaultEndpoints()` (`/health` and `/alive`, in Development only)

**After creating the files, the template:**

- adds both projects to the `.slnx`, under a `/tools/` solution folder;
- runs `dotnet add package` for the 7 ServiceDefaults packages, which writes their versions into the root
  `Directory.Packages.props`:
  - `Microsoft.Extensions.Http.Resilience` and `Microsoft.Extensions.ServiceDiscovery` are pinned to `10.11.0`
    (the .NET 10 line), replacing any version already pinned;
  - the 5 `OpenTelemetry.*` packages take the latest stable version, and keep any version already pinned.

These steps need network access. If one fails, `dotnet new` prints the command to run yourself — replace
`<Name>` in it with your solution name.

The projects inherit `TargetFramework`, `ImplicitUsings` and `Nullable` from the `Directory.Build.props` of
`handys11-solution`, so use this template in a solution created with it.

**Usage:**

Run it **from the solution root** (the folder with the `.slnx`), with the solution name:

```bash
dotnet new handys11-aspire --name <SolutionName>
```

`--name` defaults to the current folder name. Add `--files-only` to only write the files (offline use), then
finish the setup by hand:

```bash
dotnet sln add tools/<SolutionName>.Aspire tools/<SolutionName>.ServiceDefaults --solution-folder tools
dotnet add tools/<SolutionName>.ServiceDefaults package Microsoft.Extensions.Http.Resilience --version 10.11.0
dotnet add tools/<SolutionName>.ServiceDefaults package Microsoft.Extensions.ServiceDiscovery --version 10.11.0
dotnet add tools/<SolutionName>.ServiceDefaults package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add tools/<SolutionName>.ServiceDefaults package OpenTelemetry.Extensions.Hosting
dotnet add tools/<SolutionName>.ServiceDefaults package OpenTelemetry.Instrumentation.AspNetCore
dotnet add tools/<SolutionName>.ServiceDefaults package OpenTelemetry.Instrumentation.Http
dotnet add tools/<SolutionName>.ServiceDefaults package OpenTelemetry.Instrumentation.Runtime
```

To wire a service in, reference `tools/<SolutionName>.ServiceDefaults` from it, call
`builder.AddServiceDefaults()` on its builder and `app.MapDefaultEndpoints()` on its app, then add it to
`AppHost.cs` with `builder.AddProject<Projects.YourService>("yourservice")` (the AppHost also needs a
`ProjectReference` to the service).

---

## Uninstallation

```bash
dotnet new uninstall HandyS11.Templates
```

## License

See [LICENSE](LICENSE) for details.
