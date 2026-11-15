# `handys11-aspire` template — design

Date: 2026-11-15 · Status: draft, awaiting approval

## Intent

Add a `dotnet new` template that sets up .NET Aspire in a solution created with `handys11-solution`,
following the layout already used in practice (n2jsoft-technical-assessment): the Aspire projects
live under `tools/` as full projects, and package versions live in the root `Directory.Packages.props`.

Microsoft's own templates (`aspire-apphost`, `aspire-servicedefaults`, package `Aspire.ProjectTemplates`)
don't fit a handys11 solution:

- they put `Version="…"` on `PackageReference`, which fails restore under Central Package Management (NU1008);
- their code doesn't pass the solution's analyzers (`AnalysisLevel=latest-all`, `TreatWarningsAsErrors`);
- they create the project in a folder named after it, not under `tools/`;
- the ServiceDefaults package versions are frozen at the template's release.

## Goals

From the solution root, `dotnet new handys11-aspire --name <SolutionName>`:

1. creates `tools/<Name>.Aspire/` (AppHost) and `tools/<Name>.ServiceDefaults/`;
2. adds both projects to the `.slnx` under a `/tools/` solution folder;
3. records the ServiceDefaults package versions in the **root** `Directory.Packages.props`;
4. builds with `dotnet build -c Release` in a fresh `handys11-solution` with no manual edits.

## Non-goals

- No sample service/API project, no hosting integrations (PostgreSQL, Redis, …) — the user adds those.
- No changes to `handys11-solution` (no `--aspire` switch) or `handys11-github`.
- No Aspire test project (`Aspire.Hosting.Testing`).
- No automation keeping the template's pins up to date (`Aspire.AppHost.Sdk/<version>` and the two
  `Microsoft.Extensions.*` 10.x versions below) — bumped by hand.

## Anti-goals

- Output that fails to build, or fails the ReSharper cleanup pre-push hook, in a fresh `handys11-solution`.
- Downgrading a package version the solution already pins (accepted exception: the two pinned
  `Microsoft.Extensions.*` packages, see Post-actions).
- Snapshot tests that need the network or produce different output on each run.

## Constraints

- Target solution has `ManagePackageVersionsCentrally=true` and the analyzers from `Directory.Build.props`
  (NetAnalyzers, VS Threading, Roslynator, Sonar) with warnings as errors.
- `.editorconfig` already has a `[tools/**]` section turning off CS1591 (XML docs not required).
- `Directory.Build.props` supplies `TargetFramework=net10.0`, `ImplicitUsings`, `Nullable`.
- Repo SDK: .NET 10 (`global.json`); snapshot tests use `Microsoft.TemplateEngine.Authoring.TemplateVerifier`,
  which runs the real `dotnet new` — post-actions run during tests.

## Approaches considered

| # | Approach | Trade-off |
|---|----------|-----------|
| 1 | Ship `tools/Directory.Packages.props` importing the root one and adding the 7 versions | Works offline, deterministic; versions split across two files |
| 2 | **Chosen.** Post-actions run `dotnet add package`, which under CPM writes into the root `Directory.Packages.props` | Matches the n2jsoft layout, no manual step; needs network at creation, ~7 restores, partial failure possible |
| 3 | Print the 7 `<PackageVersion>` lines for the user to paste | Simplest; build fails (NU1010) until pasted |

Picked by the user: **2**.

## Spike findings (2026-11-15, SDK 10.0.403)

A throwaway template in an isolated hive confirmed:

- The "add reference" post-action (`actionId B17581D1-C5C9-4489-8F0A-004BE667B814`, `referenceType: package`)
  needs `"applyFileRenamesToArgs": ["targetFiles"]`, otherwise it fails with
  "Unable to determine which project file to add the reference to".
- Under CPM it writes `<PackageVersion>` into the root `Directory.Packages.props` (inserted alphabetically in
  the existing `ItemGroup`, so it can land under the `<!-- Analyzers -->` comment) and a version-less
  `<PackageReference>` into the csproj, then restores (~2 s per package, warm cache).
- **With a `version` arg it overwrites an existing pin** (1.18.0 was downgraded to 1.17.0).
  **Without `version`** it keeps an existing pin, otherwise takes the latest stable from the feed
  (1.21.0, vs 1.17.0 pinned by Microsoft's 13.6.5 template).
- Outside a CPM solution it falls back to `Version="…"` on the `PackageReference`.
- The "add to solution" post-action (`actionId D396686C-DE0E-4DE6-906D-291CD29FC5DE`) handles `.slnx` and
  `solutionFolder: "tools"` creates `/tools/`. With `continueOnError: true` and no solution found it prints
  "Post action failed." but `dotnet new` exits 0.

## Design

### Template manifest

- Folder `src/content/aspire/`, identity `HandyS11.Aspire`, short name `handys11-aspire`,
  name "Aspire AppHost & ServiceDefaults", `type: project`, language C#, icon `../icon.png`.
- `sourceName: "Project1"` (same convention as `handys11-github`), `preferNameDirectory: false`, so output
  is written relative to the current directory: run it from the solution root. `--name` defaults to the
  current folder name.
- `primaryOutputs`: the AppHost csproj (index 0) and the ServiceDefaults csproj (index 1).

### Generated files

```text
tools/<Name>.Aspire/
├── <Name>.Aspire.csproj
├── AppHost.cs
├── appsettings.json
├── aspire.config.json
└── Properties/launchSettings.json
tools/<Name>.ServiceDefaults/
├── <Name>.ServiceDefaults.csproj
└── Extensions.cs
```

**AppHost** (modelled on n2jsoft's `tools/N2JSoft.Aspire/`):

- csproj: `<Project Sdk="Aspire.AppHost.Sdk/13.6.5">`, `OutputType=Exe`, `UserSecretsId` (new GUID per
  instantiation via the manifest's `guids`), `NoWarn` `ASPIRE010` with the n2jsoft comment (DCP/Dashboard
  NuGet packages so `dotnet run` works without the Aspire CLI). No `TargetFramework`/`ImplicitUsings`/
  `Nullable` — inherited from `Directory.Build.props`. No `PackageReference`: the SDK brings
  `Aspire.Hosting.AppHost` itself, so nothing goes in `Directory.Packages.props` for the AppHost.
  No reference to ServiceDefaults (only service projects reference it).
- `AppHost.cs`: `DistributedApplication.CreateBuilder(args)` then
  `await builder.Build().RunAsync().ConfigureAwait(false);`.
- `appsettings.json`: same as n2jsoft (Default `Information`, `Microsoft.AspNetCore` and `Aspire.Hosting.Dcp`
  `Warning`). No `appsettings.Development.json`.
- `aspire.config.json`: `{ "appHost": { "path": "<Name>.Aspire.csproj" } }`.
- `launchSettings.json`: `https` and `http` profiles as in n2jsoft. Ports are generated per instantiation
  with Microsoft's pattern (hidden integer parameter + `port` generator fallback + `coalesce` replacer, same
  ranges as `aspire-apphost` 13.6.5) so two solutions on one machine don't clash.

**ServiceDefaults**:

- csproj: `Microsoft.NET.Sdk`, `IsAspireSharedProject=true`, `FrameworkReference Microsoft.AspNetCore.App`.
  No `PackageReference` in the template — the post-actions add them.
- `Extensions.cs`: Microsoft's 13.6.5 `Extensions.cs` (`AddServiceDefaults`, `ConfigureOpenTelemetry`,
  `AddDefaultHealthChecks`, `MapDefaultEndpoints`), kept in namespace `Microsoft.Extensions.Hosting` so
  services call `builder.AddServiceDefaults()` without a `using`, reworked until it builds clean under the
  solution's analyzers and passes the ReSharper cleanup profile (e.g. argument null checks, commented-out
  code removed).

### Post-actions

1. Seven "add reference" post-actions on the ServiceDefaults csproj, each with `continueOnError: true`,
   `condition: "(!filesOnly)"`, and manual instructions giving the exact
   `dotnet add tools/<Name>.ServiceDefaults package <Id> [--version <v>]` command:
   - **Pinned to `version: 10.11.0`** (user's decision, 2026-11-15): `Microsoft.Extensions.Http.Resilience`,
     `Microsoft.Extensions.ServiceDiscovery`. Unpinned, they would resolve to 11.0.0 (the .NET 11 line) and pull
     11.x `Microsoft.Extensions.*` assemblies into a net10.0 solution, risking NU1605 downgrade errors against
     any direct 10.x reference. 10.11.0 is the newest 10.x on nuget.org today (Microsoft's 13.6.5 template pins
     10.10.0). Accepted cost: a solution already pinning them higher gets downgraded; bump the pin by hand
     (e.g. when `handys11-solution` moves to .NET 11).
   - **Unpinned (no `version`)**: `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Extensions.Hosting`,
     `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`,
     `OpenTelemetry.Instrumentation.Runtime`. An existing pin is kept, never downgraded; a new pin gets the
     latest stable, never a stale one. Cost: whatever is latest at creation time, not a set tested together
     (low risk — the OpenTelemetry packages release in lockstep).
2. One "add to solution" post-action for both primary outputs, `solutionFolder: "tools"`,
   `continueOnError: true`, `condition: "(!filesOnly)"`. Note: `dotnet sln add` evaluates the projects, so it
   resolves `Aspire.AppHost.Sdk` from NuGet — it needs the network on a cold cache too.

### Parameters

- `--files-only` (bool, default false): writes the files and skips **all** post-actions (packages and solution).
  For offline use and the snapshot tests. The ServiceDefaults project then doesn't compile until the packages are
  added, and the projects must be added to the `.slnx` by hand. *(Builder-check finding: TemplateVerifier fails on
  any stderr, and the solution post-action writes "Post action failed" to stderr when no solution is found — so the
  tests must skip it too, not only the package post-actions.)*
- Hidden port parameters (as in Microsoft's template), used by the tests to get fixed ports.

### Tests

`tests/Templates/AspireTemplateTests.cs`, same shape as the existing classes: default instantiation and
`--name MyProject`, both with `--files-only` and fixed ports. The `UserSecretsId` GUID is scrubbed
(TemplateVerifier custom scrubber) so snapshots are stable. Snapshots committed under `Snapshots/`.

### Manual end-to-end check (before merge, not automated)

In a scratch folder: `dotnet new handys11-solution -n Demo`, `cd Demo`, `git init`,
`dotnet new handys11-aspire -n Demo` (template installed from the local pack), then
`dotnet build -c Release` is green, the `.slnx` lists both projects under `/tools/`,
`Directory.Packages.props` has the 7 versions, and `dotnet jb cleanupcode` with the solution's
profile leaves the generated files unchanged.

### Docs

- README: new `handys11-aspire` section (scaffolded files, usage, run from the solution root, needs network
  unless `--files-only`).
- CLAUDE.md: new row in the templates table.
- `src/HandyS11.Templates.csproj`: mention Aspire in `Description`, add `aspire` to `PackageTags`.

## Left to the builder

- The exact analyzer/ReSharper fixes in `Extensions.cs`, found by building.
- Template `classifications`, `description` text, and manual-instruction wording.
- How the GUID scrubber is wired in TemplateVerifier.
