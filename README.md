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

## Uninstallation

```bash
dotnet new uninstall HandyS11.Templates
```

## License

See [LICENSE](LICENSE) for details.
