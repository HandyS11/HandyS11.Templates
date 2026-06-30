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
- `Directory.Build.props` — shared MSBuild properties
- `Directory.Packages.props` — centralized package version management
- `global.json` — SDK version pinning
- `NuGet.config` — NuGet feed configuration
- `.editorconfig` — code style settings
- `.gitignore` — Git ignore rules
- `.gitattributes` — Git attribute settings
- `.vscode/` — VS Code workspace settings

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

An item template that scaffolds a `.github/` folder with GitHub Actions workflows and Dependabot configuration.

| Type | Platforms               |
|------|-------------------------|
| Item | Windows · Linux · macOS |

**Scaffolded files:**

- `.github/dependabot.yml` — Dependabot version updates (NuGet, .NET SDK, GitHub Actions)
- `.github/workflows/CI.yml` — build, format check & test on Ubuntu/Windows with coverage
- `.github/workflows/CD.yml` — tag-driven (`v*`) NuGet pack, publish & GitHub Release
- `.github/workflows/Sonar.yml` — SonarQube analysis (requires Sonar server + secrets)
- `.github/workflows/Mutation.yml` — Stryker mutation testing (manual + weekly)
- `.github/workflows/Documentation.yml` — DocFX build & GitHub Pages deploy

Pass `--name` to inject the solution name into the workflows (`<Name>.slnx`); it defaults to the
target folder name. Use the same name as `handys11-solution` for a matching pair.

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
