# HandyS11.Templates

A .NET template package providing project and item templates for use with the `dotnet new` CLI.

## Installation

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

## Uninstallation

```bash
dotnet new uninstall HandyS11.Templates
```

## License

See [LICENSE](LICENSE) for details.
