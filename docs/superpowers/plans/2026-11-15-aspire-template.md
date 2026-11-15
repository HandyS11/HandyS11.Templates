# handys11-aspire Template Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `handys11-aspire` project template that scaffolds `tools/<Name>.Aspire` (AppHost) and `tools/<Name>.ServiceDefaults` into a `handys11-solution`, adds them to the `.slnx`, and records package versions in the root `Directory.Packages.props`.

**Architecture:** New template folder `src/content/aspire/` (packed automatically by the existing `content\**\*` glob). Static files carry the projects; template-engine post-actions run `dotnet add package` (CPM-aware) and `dotnet sln add`. A `--files-only` switch disables all post-actions so TemplateVerifier snapshot tests stay offline and deterministic.

**Tech Stack:** `dotnet new` template engine (template.json, dotnetcli.host.json, post-actions), Aspire 13.6.5 (`Aspire.AppHost.Sdk`), xUnit + `Microsoft.TemplateEngine.Authoring.TemplateVerifier` 11.0.100.

**Spec:** `docs/superpowers/specs/2026-11-15-aspire-template-design.md`

## Global Constraints

- Short name `handys11-aspire`, identity `HandyS11.Aspire`, `sourceName` `Project1`, `preferNameDirectory: false`, icon `../icon.png`.
- AppHost SDK: `Aspire.AppHost.Sdk/13.6.5`.
- `Microsoft.Extensions.Http.Resilience` and `Microsoft.Extensions.ServiceDiscovery` pinned to `10.11.0`; the five `OpenTelemetry.*` packages get **no** `version` arg.
- Generated csproj files set no `TargetFramework` / `ImplicitUsings` / `Nullable` (inherited from `handys11-solution`'s `Directory.Build.props`) and no `Version=` on any `PackageReference`.
- Generated code must build with `dotnet build -c Release` (warnings as errors, `latest-all` analyzers) inside a fresh `handys11-solution`, and leave `dotnet jb cleanupcode --profile="ReformatAndReorder"` with no diff.
- Snapshot tests always pass `--files-only` and fixed ports; no network.
- Do not bump `<Version>` in `src/HandyS11.Templates.csproj`.
- Commits end with `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. **Offline or failing NuGet during creation:** each failed package post-action prints a manual instruction the user can copy-paste and run, and `dotnet new` still exits 0. → Task 2, Step 7.
2. **Existing pins in `Directory.Packages.props`:** a pre-existing `OpenTelemetry.*` pin is left unchanged. → Task 2, Step 7.
3. **Re-running the template on a solution that already has the projects:** `dotnet new` refuses to overwrite, and nothing changes on disk. → Task 2, Step 7.
4. **Running from a subfolder instead of the solution root:** the files would land in `<subfolder>/tools/`. The README must say to run it from the solution root. → Task 3, Step 1.
5. **Windows:** the post-actions are never exercised on Windows, because CI's Windows leg only runs `--files-only` snapshots. → Flag this to the user in the final summary as untested; no step.

## End-to-end check (used by Tasks 1 and 2)

Run from the repo root. It uses a throwaway template hive and folder, so nothing is installed for the user:

```bash
REPO=$(pwd); H=$(mktemp -d); W=$(mktemp -d)
dotnet new --debug:custom-hive "$H" install "$REPO/src/content/solution"
dotnet new --debug:custom-hive "$H" install "$REPO/src/content/aspire"
cd "$W" && dotnet new --debug:custom-hive "$H" handys11-solution -n Demo && cd Demo
git init -q && git add -A && git -c commit.gpgsign=false commit -qm init
dotnet new --debug:custom-hive "$H" handys11-aspire -n Demo
```

After a `dotnet new install` from a folder, edits to that folder are picked up without reinstalling. If they aren't, uninstall and install again.

---

### Task 1: AppHost project, manifest and snapshot tests

**Files:**
- Create: `src/content/aspire/.template.config/template.json`
- Create: `src/content/aspire/.template.config/dotnetcli.host.json`
- Create: `src/content/aspire/tools/Project1.Aspire/Project1.Aspire.csproj`
- Create: `src/content/aspire/tools/Project1.Aspire/AppHost.cs`
- Create: `src/content/aspire/tools/Project1.Aspire/appsettings.json`
- Create: `src/content/aspire/tools/Project1.Aspire/aspire.config.json`
- Create: `src/content/aspire/tools/Project1.Aspire/Properties/launchSettings.json`
- Test: `tests/Templates/AspireTemplateTests.cs`, plus snapshot directories under `tests/Templates/Snapshots/`

**Interfaces:**
- Produces:
  - template parameter `filesOnly` (bool, CLI `--files-only`), used by Task 2's post-action conditions;
  - hidden port parameters `appHostHttpPort`, `appHostHttpsPort`, `appHostOtlpHttpPort`, `appHostOtlpHttpsPort`, `appHostResourceHttpPort`, `appHostResourceHttpsPort`;
  - `primaryOutputs[0]` = `tools/Project1.Aspire/Project1.Aspire.csproj`;
  - the test class `AspireTemplateTests`, whose two tests Task 2 reuses unchanged.

- [ ] **Step 1: Write the failing tests** in `tests/Templates/AspireTemplateTests.cs`, in the same shape as `GitHubTemplateTests.cs`:

```csharp
private const string SourceUserSecretsId = "c7c079e4-191d-478f-8ed2-6559fc6cab16";
private static readonly string[] FixedArgs =
[
    "--files-only",
    "--appHostHttpPort", "15001", "--appHostHttpsPort", "17001",
    "--appHostOtlpHttpPort", "19001", "--appHostOtlpHttpsPort", "21001",
    "--appHostResourceHttpPort", "20001", "--appHostResourceHttpsPort", "22001",
];

[Fact] public async Task Aspire_DefaultInstantiationAsync()
// options: TemplateSpecificArgs = FixedArgs, DoNotAppendTemplateArgsToScenarioName = true, DisableDiffTool = true,
//          .WithCustomScrubbers(ScrubbersDefinition.Empty.AddScrubber(sb => { capture the UserSecretsId value,
//              replace it with "00000000-0000-0000-0000-000000000000" }, "csproj"))
// after Execute: Assert.True(Guid.TryParse(captured, out _)); Assert.NotEqual(SourceUserSecretsId, captured, ignoreCase: true);

[Fact] public async Task Aspire_WithCustomNameAsync()
// same, TemplateSpecificArgs = ["--name", "MyProject", ..FixedArgs], ScenarioName = "MyProject"
```

Keep the scrubber's capture-and-replace in one private helper that both tests use. If the extension filter `"csproj"` doesn't match (the GUID shows up unscrubbed in the received snapshot), try `".csproj"`.

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `dotnet test --filter "FullyQualifiedName~AspireTemplateTests"`
Expected: FAIL, because the template folder or short name `handys11-aspire` is not found.

- [ ] **Step 3: Create `template.json` and `dotnetcli.host.json`**

`template.json` contains:
- `author`/`owner` `HandyS11`, `classifications` `["Cloud", "Aspire"]`, `name` `"Aspire AppHost & ServiceDefaults"`, a one-line `description`;
- `tags`: `language` `C#`, `type` `project`, platforms Windows/Linux/macOS;
- `identity`, `shortName`, `sourceName`, `preferNameDirectory`, `icon` as in Global Constraints;
- `sources[0].exclude` `["**/.template.config/**"]`;
- `guids` `["c7c079e4-191d-478f-8ed2-6559fc6cab16"]`;
- `primaryOutputs` `[{ "path": "tools/Project1.Aspire/Project1.Aspire.csproj" }]`;
- `symbols`:
  - `filesOnly`: bool parameter, default `false`;
  - six port triplets copied from Microsoft's `aspire-apphost` 13.6.5, each made of an integer parameter `appHost<X>Port`, a generated `appHost<X>PortGenerated` (`port` generator) and a `appHost<X>PortReplacer` (`coalesce`). Ranges and the literal each one replaces:

| X | range | replaces |
|---|-------|----------|
| Http | 15000–15300 | `15000` |
| Https | 17000–17300 | `17000` |
| OtlpHttp | 19000–19300 | `19000` |
| OtlpHttps | 21000–21300 | `21000` |
| ResourceHttp | 20000–20300 | `20000` |
| ResourceHttps | 22000–22300 | `22000` |

`dotnetcli.host.json`: `filesOnly` → `longName` `files-only`, `shortName` `""`; the six `appHost*Port` symbols → `isHidden: true`.

- [ ] **Step 4: Create the AppHost files** (contents modelled on `n2jsoft-technical-assessment/tools/N2JSoft.Aspire/`)

- `Project1.Aspire.csproj`: `<Project Sdk="Aspire.AppHost.Sdk/13.6.5">`, with `OutputType` `Exe`, `UserSecretsId` `c7c079e4-191d-478f-8ed2-6559fc6cab16`, and `<NoWarn>$(NoWarn);ASPIRE010</NoWarn>`. Put this comment above the `NoWarn`: `<!-- Use the Dashboard/DCP NuGet packages so `dotnet run` works without the Aspire CLI installed. -->`. Nothing else.
- `AppHost.cs`: `var builder = DistributedApplication.CreateBuilder(args);` followed by `await builder.Build().RunAsync().ConfigureAwait(false);`.
- `appsettings.json`: `Logging.LogLevel` with `Default` `Information`, `Microsoft.AspNetCore` `Warning`, `Aspire.Hosting.Dcp` `Warning`.
- `aspire.config.json`: `{ "appHost": { "path": "Project1.Aspire.csproj" } }`.
- `Properties/launchSettings.json`: `https` and `http` profiles, the same as n2jsoft but with the literal ports from the table above:
  - `https`: `applicationUrl` `https://localhost:17000;http://localhost:15000`, OTLP `https://localhost:21000`, resource service `https://localhost:22000`;
  - `http`: `applicationUrl` `http://localhost:15000`, OTLP `http://localhost:19000`, resource service `http://localhost:20000`.

- [ ] **Step 5: Run the tests and accept the snapshots**

Run: `dotnet test --filter "FullyQualifiedName~AspireTemplateTests"`
Expected: FAIL with new `*.received/` directories under `tests/Templates/Snapshots/`. Check them:
- the files sit under `tools/handys11-aspire.Aspire/` (default test) and `tools/MyProject.Aspire/`;
- the ports read 15001/17001/19001/20001/21001/22001;
- `UserSecretsId` is all zeros.

Then rename each `.received` directory to `.verified`.

- [ ] **Step 6: Run the tests twice and confirm they pass and are deterministic**

Run: `dotnet test --filter "FullyQualifiedName~AspireTemplateTests"` twice.
Expected: PASS both times, with no new `.received` directories.

- [ ] **Step 7: End-to-end build of the AppHost**

Run the End-to-end check, then `dotnet build tools/Demo.Aspire -c Release`.
Expected: `0 Warning(s) 0 Error(s)`. The ServiceDefaults project doesn't exist yet, and the solution post-action isn't wired yet, so this step only checks the AppHost. Fix anything the analyzers flag in `AppHost.cs` or the csproj, then rerun Steps 5–6 if the snapshots changed.

- [ ] **Step 8: Run the full suite and the format check**

Run: `dotnet test` and `dotnet format --no-restore --verify-no-changes`
Expected: all tests pass, and format exits 0.

- [ ] **Step 9: Commit**

```bash
git add src/content/aspire tests/Templates/AspireTemplateTests.cs tests/Templates/Snapshots
git commit -m "feat(aspire): add handys11-aspire AppHost template"
```

---

### Task 2: ServiceDefaults project and post-actions

**Files:**
- Create: `src/content/aspire/tools/Project1.ServiceDefaults/Project1.ServiceDefaults.csproj`
- Create: `src/content/aspire/tools/Project1.ServiceDefaults/Extensions.cs`
- Modify: `src/content/aspire/.template.config/template.json` (`primaryOutputs`, `postActions`)
- Test: the existing `AspireTemplateTests` (snapshots gain the ServiceDefaults files)

**Interfaces:**
- Consumes: from Task 1, the `filesOnly` symbol, `primaryOutputs[0]`, and `AspireTemplateTests`.
- Produces:
  - `primaryOutputs[1]` = `tools/Project1.ServiceDefaults/Project1.ServiceDefaults.csproj`;
  - public extension methods in namespace `Microsoft.Extensions.Hosting`:
    - `TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder`
    - `TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder`
    - `TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder`
    - `WebApplication MapDefaultEndpoints(this WebApplication app)`

- [ ] **Step 1: Run the tests and confirm they still pass before the change**

Run: `dotnet test --filter "FullyQualifiedName~AspireTemplateTests"`
Expected: PASS. This is the baseline. In Step 4, the snapshot diff is the failing test.

- [ ] **Step 2: Create the ServiceDefaults project**

- `Project1.ServiceDefaults.csproj`: `Microsoft.NET.Sdk`, `<IsAspireSharedProject>true</IsAspireSharedProject>`, `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, and no `PackageReference`.
- `Extensions.cs`: start from Microsoft's 13.6.5 `aspire-servicedefaults/Extensions.cs`. Unpacked copy:
  `/tmp/claude-1000/-home-cloudcli-projects-HandyS11-Templates/dfc205a8-8e70-4856-aa16-322c29615780/scratchpad/aspire-templates-13.6.5/content/templates/aspire-servicedefaults/Extensions.cs`.
  If the scratchpad has been cleared, re-download `aspire.projecttemplates` 13.6.5 from `https://api.nuget.org/v3-flatcontainer/aspire.projecttemplates/13.6.5/aspire.projecttemplates.13.6.5.nupkg`. Keep the four public methods and their behaviour (the `/health` and `/alive` endpoints are mapped in Development only), and keep the namespace. The analyzer fixes come in Step 6.

- [ ] **Step 3: Add the post-actions to `template.json`**

Add `primaryOutputs[1]`. Then, in this order:
- seven post-actions, each with:
  - `actionId` `B17581D1-C5C9-4489-8F0A-004BE667B814`, `continueOnError: true`, `condition` `(!filesOnly)`;
  - `applyFileRenamesToArgs: ["targetFiles"]`;
  - `args`: `referenceType` `package`, `reference` `<Id>`, `projectFileExtensions` `.csproj`, `targetFiles` `["tools/Project1.ServiceDefaults/Project1.ServiceDefaults.csproj"]`, plus `version` `10.11.0` for the two `Microsoft.Extensions.*` packages only;
  - `manualInstructions`: `Run 'dotnet add tools/<Name>.ServiceDefaults package <Id> [--version 10.11.0]' from the solution root.`

  Package ids, in order: `Microsoft.Extensions.Http.Resilience`, `Microsoft.Extensions.ServiceDiscovery`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Instrumentation.Runtime`.
- one post-action with `actionId` `D396686C-DE0E-4DE6-906D-291CD29FC5DE`, `continueOnError: true`, `condition` `(!filesOnly)`, `args` `{ "primaryOutputIndexes": "0;1", "solutionFolder": "tools" }`, and the manual instruction `Run 'dotnet sln add tools/<Name>.Aspire tools/<Name>.ServiceDefaults --solution-folder tools'.`

- [ ] **Step 4: Run the tests, check the new snapshot files and accept them**

Run: `dotnet test --filter "FullyQualifiedName~AspireTemplateTests"`
Expected: FAIL, because the received snapshots now contain `tools/<name>.ServiceDefaults/` with both files. Check that the csproj has no `PackageReference`, which proves `--files-only` skipped the post-actions. Then replace the `.verified` directories with the `.received` ones.

- [ ] **Step 5: Run the tests and confirm they pass**

Run: `dotnet test --filter "FullyQualifiedName~AspireTemplateTests"`
Expected: PASS.

- [ ] **Step 6: End-to-end check: build, packages, solution, cleanup**

Run the End-to-end check. Then, in `Demo/`:
- `cat Demo.slnx` → a `/tools/` folder containing `tools/Demo.Aspire/Demo.Aspire.csproj` and `tools/Demo.ServiceDefaults/Demo.ServiceDefaults.csproj`.
- `grep -E 'Resilience|ServiceDiscovery|OpenTelemetry' Directory.Packages.props` → 7 `PackageVersion` lines, with both `Microsoft.Extensions.*` at `10.11.0`.
- `grep PackageReference tools/Demo.ServiceDefaults/Demo.ServiceDefaults.csproj` → 7 lines, none with `Version=`.
- `dotnet build Demo.slnx -c Release` → `0 Warning(s) 0 Error(s)`. Fix analyzer findings in `Extensions.cs` in the template source and rerun from the End-to-end check. Expected candidates:
  - CA1062: use `ArgumentNullException.ThrowIfNull` on `builder`/`app`;
  - CA1724: class `Extensions` clashes with namespace `Microsoft.Extensions`; rename it to `ServiceDefaultsExtensions`;
  - S125: drop the commented-out code blocks.
- `git add -A && dotnet tool restore && dotnet jb cleanupcode Demo.slnx --profile="ReformatAndReorder" --include="tools/**/*.cs" && git diff --exit-code -- tools` → exit 0. If cleanup changes the source, apply the same changes to the template source.

After any change to the template source, rerun Steps 4–5.

- [ ] **Step 7: End-to-end Review Focus checks**

Each check starts from a fresh End-to-end check run, stopping before its last line (the `handys11-aspire` command) unless stated otherwise:
- **Existing pin kept:** add `<PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.15.0" />` to `Directory.Packages.props`, run `handys11-aspire`, then `grep OpenTelemetry.Extensions.Hosting Directory.Packages.props` → still `1.15.0`.
- **Offline:** replace Demo's `NuGet.config` with one that has `<clear/>` and a single source pointing at an empty local folder, then run `handys11-aspire`. Expected:
  - exit code 0;
  - one "Manual instructions: Run 'dotnet add …'" line per package, with the real package id;
  - the files are still created.

  Record whether `<Name>` is printed literally or replaced. If literally, that's acceptable and it should be mentioned in the README.
- **Re-run:** after a successful run, run `handys11-aspire -n Demo` again. Expected: `dotnet new` refuses because the files exist (non-zero exit), and `git status --short` shows no change to `tools/`.

- [ ] **Step 8: Run the full suite and the format check**

Run: `dotnet test` and `dotnet format --no-restore --verify-no-changes`
Expected: all tests pass, and format exits 0.

- [ ] **Step 9: Commit**

```bash
git add src/content/aspire tests/Templates/Snapshots
git commit -m "feat(aspire): add ServiceDefaults project and package/solution post-actions"
```

---

### Task 3: Documentation and package metadata

**Files:**
- Modify: `README.md` (new section after `handys11-github`, before `## Uninstallation`)
- Modify: `CLAUDE.md` (Templates table)
- Modify: `src/HandyS11.Templates.csproj` (`Description`, `PackageTags`)

**Interfaces:**
- Consumes: the template behaviour from Tasks 1–2, plus the Step 7 finding about how manual instructions render.

- [ ] **Step 1: README section `### \`handys11-aspire\` — Aspire AppHost & ServiceDefaults`**

Use the same layout as the `handys11-github` section: a Project/C#/platforms table, "Scaffolded files", "Usage". It must say:
- run it **from the solution root** (where the `.slnx` is), with `--name` set to the solution name;
- what it creates under `tools/`;
- that it adds both projects to the `.slnx` under `/tools/`;
- that it runs `dotnet add package` for the 7 packages, writing the versions into the root `Directory.Packages.props`. The two `Microsoft.Extensions.*` packages are pinned to 10.11.0 and overwrite an existing pin; the OpenTelemetry packages take the latest stable and keep an existing pin;
- that it needs network access;
- that `--files-only` writes the files only, followed by the manual commands to finish the setup;
- that it is built for `handys11-solution`, since the projects inherit `TargetFramework` from its `Directory.Build.props`;
- that services opt in with a `ProjectReference` to ServiceDefaults plus `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`.

- [ ] **Step 2: Add a CLAUDE.md table row**

Add `handys11-aspire` | project | `tools/<Name>.Aspire` AppHost + `tools/<Name>.ServiceDefaults`. Mention that it uses `sourceName` `Project1`, runs `dotnet add package`/`dotnet sln add` post-actions (skipped with `--files-only`, which the snapshot tests use), and that the pins `Aspire.AppHost.Sdk/13.6.5` and `Microsoft.Extensions.*` `10.11.0` are bumped by hand.

- [ ] **Step 3: Update the csproj metadata**

Rewrite `Description` so it also lists the Aspire AppHost & ServiceDefaults template. Change `PackageTags` to `template;templates;aspire`.

- [ ] **Step 4: Verify the pack and the markdown**

Run: `dotnet pack src/HandyS11.Templates.csproj -c Release -o "$(mktemp -d)"` → succeeds. Then `unzip -l <nupkg> | grep content/aspire` lists the template files, including `.template.config/template.json`.

- [ ] **Step 5: Commit**

```bash
git add README.md CLAUDE.md src/HandyS11.Templates.csproj
git commit -m "docs: document handys11-aspire template"
```
