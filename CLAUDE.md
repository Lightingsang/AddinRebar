# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Role & Responsibilities

Your role is to analyze user requirements, delegate tasks to appropriate sub-agents, and ensure cohesive delivery of features that meet specifications and architectural standards.

## Repository Layout

This repo bundles **five unrelated deliverables** plus one shared library folder; treat them as separate concerns — do not cross-wire them. The only permitted dependency direction is MCP folder (`HPRebar/`, `HPAutoCad/`, `HPNavis/`, `HPEtabs/`) → `McpShared/`; the MCP folders never reference each other:

| Path | What it is | Stack |
|---|---|---|
| `HPRebar/` | The Revit Add-In (the "real" product). Multi-version R23–R27. Also hosts the **Revit MCP** (server exe + bridge add-in, see "HPRebar MCP Bridge" below). | C# / Nice3point.Revit.Sdk / WPF / Serilog / ModelContextProtocol |
| `McpShared/` | **Host-neutral MCP engine** shared by every HP MCP: `HPRebar.Mcp.Contracts` (wire DTOs; `netstandard2.0;net48` — the net48 asset exists because Roamer.exe reflects over every plugin type before any plugin code runs, and the netstandard asset's System.Text.Json 10.0.0.0 reference cannot bind to the net462 package asset 10.0.0.12 on .NET Framework), `HPRebar.McpBridge.Core` (pipe listener, Roslyn guard/compiler, settings, bridge host + status view model; `net8.0;net48`; `MainThreadQueue(expireWithoutTicks)` opt-in for hosts whose idle event stops under a native modal), `HPRebar.Mcp.Server.Core` (server bootstrap, pipe client, execute/context services, tool registry engine, registry meta tools, CLI; `IHostProfile.MaxTimeoutSeconds` default 120 drives every timeout clamp) and their tests (`HPRebar.Mcp.Server.Core.Tests` net10, `HPRebar.McpBridge.Core.Net48Tests` net48). Never references `Autodesk.*`; the host arrives through `IHostProfile` / `IBridgeExecutor`. Assembly names keep the historical `HPRebar.*` prefix. Own `McpShared.slnx` + `global.json`. Navisworks constants/profiles (`PipeNaming.NavisHost`, `GuardProfile.Navis`, `HostScriptContracts.NavisImports`/`NavisHeavyMaxTimeoutSeconds`, `ContextResult.Navis`) landed 2026-09-15 as phase 0 of `plans/260915-0824-navisworks-mcp-2026/`. ETABS constants/profiles landed 2026-09-16 as phase 0 of `plans/260916-2152-etabs-mcp-2026/` (`PipeNaming.EtabsHost` → `hpetabs-mcp-22` — CSI numbers versions, 22 not a year —, `JsonRpcMethods.EtabsPrefix`, `HostScriptContracts.EtabsImports`/`EtabsGlobals`/`EtabsHeavyMaxTimeoutSeconds`, `ContextResult.Etabs`/`EtabsInfo`, `ExecuteResult.Snapshot` (file name only; `ResultFormatter` strips a path), `AnalyzeRequest.Transaction` (forwarded by `propose_tool` so a bridge can refuse a `none` tool that writes), `GuardProfile.Etabs`/`AnalyzerProfile.Etabs`, `IHostProfile.BridgeNotConnectedHint`/`TimeoutSemanticsHint` + `RequestDispatcher`/`McpBridgeHost` `executionDisabledMessage` (null → the historical texts, byte-identical), and `McpServerHost.ConfigureOptions` now seeds `BridgeOptions.HostVersion` from `profile.DefaultVersion` — the engine had never applied it and only matched the first three hosts because they are all 2026); the same phase closed a `global::` bypass in `ScriptGuard.IsDeniedNamespace` for every host (`global::System.IO.File…` used to slip past the namespace deny-list); ETABS phase 2 (2026-09-17) closed another one for every host: `ScriptGuard.Check` now refuses the `#r` / `#load` directives (trivia the walker never saw, honoured by the script compiler — a DLL from disk ran outside every check). Gate: `tools/list` of the Revit/AutoCAD/Navis exes byte-identical before/after (`plans/260916-2152-etabs-mcp-2026/reports/`). `McpShared/tools/` holds the host-neutral harness scripts (`mcp-call.py` one request per process, `mcp-session.py` one long-lived session, `harness_common.py` = `Checklist` PASS/FAIL/SKIP bookkeeping + JSON summary/exit code + UTF-8 console, re-exporting `Server`) that every HP MCP harness imports by relative path — `HPNavis/tools/harness/` and `HPAutoCad/tools/harness/` both do (the AutoCAD copies were removed 2026-09-16); the phase-0 snapshot script `plans/260915-0824-navisworks-mcp-2026/reports/snapshot-tools-list.ps1` uses it too. | C# / net8 · net48 · net10 / ModelContextProtocol / Roslyn |
| `HPAutoCad/` | The **AutoCAD MCP** (phases 1–5 done: loader, ALC, bundle, bridge runtime, server exe, registry per host, 12 embedded seed tools, live verification harness, all verified live in AutoCAD 2026) plus the **AEC engine** `HPAutoCad.Aec` (phase A of `plans/260916-1140-aec-automation-mcp-autocad/` done 2026-09-16: 5 read-only AEC seeds — context, entity query, spatial query, measure, geometry issues — see "AEC engine" below). Own `HPAutoCad.slnx` + `global.json`; references `../McpShared/` only. | C# / net8 · net10 / AutoCAD.NET 25.1.0 |
| `HPNavis/` | The **Navisworks MCP** (plan complete 2026-09-15, phases 0–5: `HPNavis.McpBridge` plugin + `HPNavis.Mcp.Server` net10 stdio exe, **24 tools** = 4 core + 8 registry + 12 embedded seeds, verified live in Navisworks Manage 2026 by the unattended harnesses in `tools/harness/` — see "HPNavis MCP Bridge" below). **Navisworks runs .NET Framework 4.8**, so the plugin is `net48` and consumes the `net48` assets of `McpShared/`. Own `HPNavis.slnx` + `global.json` + `Directory.Build.props` (API referenced from the installed product — no NuGet); references `../McpShared/` only. Tests `HPNavis.McpBridge.Tests` 124 (net48) + `HPNavis.Mcp.Server.Tests` 49 (net10). Plan: `plans/260915-0824-navisworks-mcp-2026/`. | C# / net48 · net10 / Navisworks API 23.0 |
| `HPEtabs/` | The **ETABS 22 MCP** (plan `plans/260916-2152-etabs-mcp-2026/` **complete**, phases 0–4 2026-09-16/17, verified live — see "HPEtabs MCP Bridge" below). ETABS's OAPI is **out-of-process COM** through the managed netstandard2.0 wrapper `ETABSv1.dll`, so nothing loads into ETABS.exe: `HPEtabs.McpBridge` is a **standalone WPF desktop app** (net8.0-windows) holding the one COM attachment, the pipe `hpetabs-mcp-22` and the two opt-in checkboxes; `HPEtabs.Mcp.Server` is the net10 stdio exe and never references the wrapper. **Versions are CSI's numbers (22), not years.** ETABS has **no transaction/undo**: scripts are tiered R/W/D before they run (semantic, from a generated allow-list), and a writing script gets the model saved and a `.EDB` snapshot copied first. Own `HPEtabs.slnx` + `global.json` + `Directory.Build.props` (install dir via the COM `LocalServer32` registration — there is no vendor install key; no NuGet); references `../McpShared/` only. Tests `HPEtabs.Mcp.Server.Tests` 81 (net10; 25 of them compile the seeds against the installed ETABSv1.dll and skip visibly without it) + `HPEtabs.McpBridge.Tests` 184 (net8.0-windows, needs ETABS 22 installed, not running; a module initializer installs the resolver because the wrapper is not in the test bin). | C# / net8.0-windows · net10 / ETABSv1 2.10 (COM) |
| `revit-market-research/` | Apify actor scraping the Revit plugin market | Node ≥24 / TypeScript / Crawlee / vitest |
| `scripts/skill_sync/` + `tests/skill-sync/` | Engine that keeps `.claude/`, `.agents/`, `.codex/` agent configs in sync | Python 3 / stdlib `unittest` |
| `course-website/` | Static lesson site (`index.html`, `lesson-01..04.html`), deployed via `vercel.json` | Plain HTML |
| `scripts/generate_revit_api_infographics.py` | One-off course infographic generator | Python + `google-genai` |
| `docs/` | Project docs — `system-architecture.md`, `code-standards.md` (Vietnamese) | |
| `plans/` | Stack-Aware 6-phase plans from `/bs:plan`, in timestamped subfolders. `plans/templates/` holds bug-fix / feature / refactor templates. | |
| `RevitTemplates-*.md` | Reference material on Nice3point templates (Vietnamese) | |

## HPRebar — Build, Run, Debug

Solution is **`HPRebar/HPRebar.slnx`** (XML `.slnx` format, *not* `.sln`). `global.json` pins .NET SDK `10.0.300` and sets the test runner to `Microsoft.Testing.Platform`.

Configurations are `Debug.R23..R27` / `Release.R23..R27` — the `R##` suffix is what the Revit MSBuild SDK parses to pick `RevitVersion`, `TargetFramework`, and the `REVIT####` constants. **There is no plain `Debug`/`Release`** for the add-in project; always pass a suffixed configuration.

```bash
# From HPRebar/ — build one Revit version
dotnet build HPRebar.slnx -c Debug.R26             # primary: the dev machine has Revit 2026
dotnet build HPRebar/HPRebar.csproj -c Debug.R26   # add-in project only
dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false   # when Revit is open and locking the DLL

dotnet test HPRebar.Core.Tests                     # 334 xUnit tests, no Revit needed
dotnet test HPRebar.Mcp.Server.Tests               # 109 xUnit tests (Revit-specific): registry over the real seeds, seed tools compiled against the Revit API reference assemblies — no Revit needed
# Engine tests live beside the engine (run from McpShared/ — each folder has its own global.json pinning the MTP runner):
(cd ../McpShared && dotnet test HPRebar.Mcp.Server.Core.Tests)   # 164 xUnit tests: pipe round trips with a fake executor, guard/compiler/args/analyzer, host-neutrality, registry per profile, ContextService.Shape all hosts, stability window, Navis profile + per-profile timeout ceiling, ETABS profile/hints/HostVersion seeding/`global::` guard (34 added 2026-09-16), `#r`/`#load` refused (2 added 2026-09-17)
(cd ../McpShared && dotnet test HPRebar.McpBridge.Core.Net48Tests)  # 62 xUnit tests of the engine's net48 asset (the .NET Framework host is Navisworks 2026 — see HPNavis/): guard/queue linked, Roslyn on 4.8, PipeSecurity pipe ACL. Do NOT pass --nologo to dotnet test — it is forwarded to the MTP runner and rejected
dotnet build HPRebar.Tests/HPRebar.Tests.csproj -c Debug.R26  # TUnit, excluded from solution builds

# ModularPipelines automation — from HPRebar/build/
dotnet run              # CompileProjectModule: builds EVERY Release.R* config
dotnet run -- test      # + TestProjectModule (skipped on CI via [SkipIf<IsCI>])
dotnet run -- pack      # Clean -> CreateBundle (HPRebar + HPRebar.McpBridge) -> PublishServer -> CreateInstaller, into output/
```

Rider run configs `Compile` / `Pack` in `HPRebar/.run/` wrap the same two pipeline entry points.

**Pipeline internals worth knowing** (`HPRebar/build/`):
- `ResolveConfigurationsModule` reads `.slnx` `BuildTypes` and **filters to `Release.R*` only** — `dotnet run` never produces Debug output.
- `ResolveVersioningModule` derives the version from git (GitVersion) unless `Build:Version` is set in `build/appsettings.json`, a user secret, or an env var.
- `Solutions.HPRebar` is generated by `Sourcy.DotNet`; `HPRebar/.sourcyroot` marks the scan root.
- Bundle vendor metadata lives in `build/appsettings.json` (`Bundle:VendorName` etc.), currently the template default `"Development"`.

Debug builds auto-deploy to `%AppData%\Autodesk\Revit\Addins\<version>\` (per-user, **not** `%ProgramData%` — the SDK's `AddinDeployDir` default is `$(AppData)\...`) via `<DeployAddin>true</DeployAddin>`; `<LaunchRevit>true</LaunchRevit>` makes F5 start Revit and attach. `<IsRepackable>true</IsRepackable>` + ILRepack merge dependencies, and `<EnableDynamicLoading>true</EnableDynamicLoading>` isolates the assembly load context.

**Multi-version conditional compilation** uses SDK-emitted constants (`REVIT2023`, `REVIT2024_OR_GREATER`, …). Gate removed APIs by inverting: `#if !REVIT2023_OR_GREATER`.

```csharp
#if REVIT2024_OR_GREATER
    long id = elementId.Value;       // .Value is long since 2024
#else
    int id = elementId.IntegerValue; // legacy
#endif
```

Tag each block with `// Multi-version: <topic>` so it stays greppable. Edit `<Configurations>` in `.csproj` by hand — IDEs corrupt them.

## HPRebar — Current State (read before planning)

The solution holds **ten** projects plus the two automation ones (four rebar, six MCP — four of which live in `../McpShared/` and are referenced by relative path). Read `docs/codebase-summary.md` for the full picture; the load-bearing facts are:

| Project | TFM | Notes |
|---|---|---|
| `HPRebar/` | net48 (R23/R24) · net8.0-windows7.0 (R25/R26) · net10.0-windows7.0 (R27) | The add-in. `CommunityToolkit.Mvvm` 8.4.0, `Serilog.Sinks.File`, `ProjectReference` to Core |
| `HPRebar.Core/` | netstandard2.0 | Pure maths. **Must never reference `Autodesk.Revit.*`** — `Document` is sealed and unmockable, so anything testable lives here |
| `HPRebar.Core.Tests/` | net8.0 | xUnit **v3** (not v2 — v2's runner cannot speak the `Microsoft.Testing.Platform` runner pinned in `global.json`). 334 tests |
| `HPRebar.Tests/` | R25/R26 only | TUnit, loads Revit in-process. **`<Build Project="false"/>` in `.slnx`** — under an R23/R24 configuration it compiles net8 against a net48 `HPRebar.dll` and Polyfill's span types collide (`CS0433`). Build it by project path |
| `../McpShared/HPRebar.Mcp.Contracts/` | netstandard2.0 | JSON-RPC envelope + DTOs shared by every MCP server and bridge. No host API, no MCP SDK. Changes must be additive (deployed bridges) |
| `../McpShared/HPRebar.McpBridge.Core/` | net8.0 · net48 | Host-free half of a bridge (the `net48` asset is for hosts still on .NET Framework — Navisworks 2026 — and every net48-specific line sits behind `#if NET48`: `PipeSecurity` pipe ACL, `Stopwatch` clock, `IReadOnlyCollection` instead of `IReadOnlySet`; the net8.0 asset is unchanged): pipe listener/dispatcher (dispatches on the method suffix, so `revit.execute` ≡ `autocad.execute`), Roslyn guard/compiler/cache with `GuardProfile`/`AnalyzerProfile`, `ScriptArgs`, `ScriptUnits`, settings store per vendor/product, `McpBridgeHost`, status view model (no WPF). xUnit-testable |
| `../McpShared/HPRebar.Mcp.Server.Core/` | net10.0 | Host-free half of a server: `McpServerHost.CreateBuilder(args, IHostProfile)`, `BridgeClient` (name kept; profile-driven), `ExecuteCodeService`, `ContextService` (shapes per host, hidden Revit fields for non-Revit), registry engine per profile (`ToolValidator`, `ToolLifecycleService`, `DynamicToolRegistrar`) + 8 meta tools (descriptions host-neutral). Every constructor defaults to `HostProfile.Revit` |
| `HPRebar.Mcp.Server/` | net10.0 console | The Revit MCP server exe (`ModelContextProtocol` 2.2.0, stdio): a 6-line `Program.cs`, `Hosts/Revit/` (`RevitHostProfile`, `execute_revit_code`, `get_revit_context`, `revit://` resources, prompts) and the embedded seed library. Never references Revit |
| `HPRebar.McpBridge/` | R25/R26 only (net8.0-windows7.0) | Second add-in (own `.addin`, GUID `A1F50652-27B4-48D4-8BA9-9694B1AA9E65`, own ALC `HPRebar.McpBridge`). `IsRepackable=false` on purpose — Roslyn ships as loose DLLs. Skipped under R23/R24/R27 via `.slnx` `<Build … Project="false"/>` |
| `HPRebar.Mcp.Server.Tests/` | net10.0 | xUnit v3, 109 tests: registry lifecycle over the real Revit seeds, seed compile checks. Engine tests (96) are in `../McpShared/HPRebar.Mcp.Server.Core.Tests/`; the fake executor is one file linked into both |

Three features exist, all following the feature-folder convention below: **`ColumnRebar/`** (81 files, ~7.0k lines), **`BeamRebar/`** (68 files, ~6.8k lines) and **`FoundationRebar/`** (24 files, ~1.7k lines). `Resources/Themes/` holds 8 theme files; `ThemeSwitcher` follows Revit's own Dark/Light.

**`BeamRebar` and `FoundationRebar` do not compile under `Debug.R27`/`Release.R27`** — 10 errors, all from `RebarHookOrientation` and the `Curve.Intersect(Curve, out …)` overload, which Revit 2027 removed. `#pragma warning disable CS0618` suppresses the deprecation warning but cannot survive removal; those call sites need a version-gated branch, or a move to the stable API `ColumnRebar` already uses (`Rebar.CreateFromRebarShape`, zero suppressions, compiles on all five). Gate on `Debug.R26` until that is fixed.

Still true:
- **No DI container is wired.** `Application.cs` uses the static `Log.Logger`, not `ILogger<T>`. Feature classes are constructed by hand in `ColumnRebarCommand`.
- `Application.cs` and `Commands/StartupCommand.cs` use **block-scoped namespaces**, which contradicts the file-scoped rule in `.claude/rules/development-rules.md`. New files follow the rule; do not churn those two template files just to reformat them.
- `TestProjectModule` (`dotnet run -- test`) runs the whole solution per `Release.R*`, so it picks up `HPRebar.Core.Tests` and `HPRebar.Mcp.Server.Tests`; the TUnit project stays excluded, and so are the engine tests in `McpShared/` (run them separately).
- `ResolveConfigurationsModule` reads `HPRebar.slnx` through Sourcy (`Solutions.HPRebar`), not by searching the git root for "any .slnx" — `McpShared/` and `HPAutoCad/` carry their own solutions.

**The rebar add-in has not been verified at runtime.** Every Revit version is build-only for `HPRebar/`; the 16 TUnit tests skip because `HPRebar.Tests/Fixtures/column-stack-2-storey.rvt` does not exist. Do not describe any version as "supported". The **MCP bridge is different**: it has been run end-to-end in Revit 2026 on the dev machine (2026-09-12) — see the section below.

**A running Revit locks the deployed DLL.** Add `-p:DeployAddin=false` to any build meant only to check compilation.

Add-in identity lives in `HPRebar/HPRebar/HPRebar.addin` — `AddInId` GUID `AB6B2397-2618-4A8F-A86F-B0EBB5E58D2B`, `FullClassName` `HPRebar.Application`. Renaming the assembly or root namespace requires updating this manifest and the `/HPRebar;component/...` icon pack URIs in `Application.cs`.

## HPRebar MCP Bridge (Dynamic Revit MCP Server)

Turns Revit into a runtime for an AI agent, with a **tool registry** that remembers reviewed scripts as MCP tools. Design of record: `plans/260912-1521-dynamic-revit-mcp-server-2026/` (`architecture.md`, `adr/adr-01..06`, `research/ai-bim-self-extending-tool-registry-design.md`).

Tool surface (34 on the dev machine): 4 core — `execute_revit_code` (C# script via Roslyn, `Destructive`), `get_revit_context`, `inspect_type` (both `ReadOnly`), `cancel_execution`; 8 registry — `search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`; plus every **published library tool** (21 seeds ported from `mcp-servers-for-revit` + whatever the AI proposed and a human approved). Resources `revit://document/info`, `revit://selection`, `registry://tools[/{name}]`; prompts `revit_query_template`, `revit_modify_template`, `toolify_run`. **Never add a native command class to the bridge for a tool** (ADR-05): a tool is `tool.json + code.cs + examples.json` in the library and runs through the same `revit.execute` path as ad-hoc code.

```
Claude Code ──stdio──▶ HPRebar.Mcp.Server (net10) ──named pipe hprebar-mcp-r2026, JSON-RPC 2.0 NDJSON──▶ HPRebar.McpBridge (inside Revit)
                                                                                              guard → Roslyn compile (pipe thread) → ExternalEvent → TransactionGroup "MCP: <label>" → Revit API
```

Load-bearing facts:
- **Two processes, never one.** A stdio MCP server must be a child process of the host AI, so `Revit.exe` cannot host it (ADR-01). The server never references `Autodesk.Revit.*`; the bridge never references the MCP SDK; `HPRebar.Mcp.Contracts` is the only shared assembly.
- **Everything host-free lives in `../McpShared/`** — bridge side in `HPRebar.McpBridge.Core` (pipe, dispatcher, `ScriptGuard`, `ScriptCompiler`, settings, audit, bridge host, status view model), server side in `HPRebar.Mcp.Server.Core` (bootstrap, pipe client, services, registry engine per profile, meta tools with host-neutral descriptions since phase 4) — so it is tested by `McpShared/HPRebar.Mcp.Server.Core.Tests` over a real named pipe with a fake executor. `IHostProfile` (server) and the `hostName`/`GuardProfile`/`AnalyzerProfile` parameters (bridge core) are the only way a host name gets in; `HostProfile.Revit` is the default everywhere, so the Revit server behaves exactly as before the split (tool names/input schemas/annotations byte-identical to phase 0; descriptions of 8 engine tools — `get_run, inspect_type, search_tools, cancel_execution, run_tool, propose_tool, test_tool, publish_tool` — + `inspect_type` title are host-neutral since phase 4). Only `McpBridgeExternalEventHandler`, `ScriptRunner`, `ResultSerializer`, `RevitContextReader` and the WPF window touch Revit.
- **Security is defense-in-depth, not a sandbox** (ADR-04): opt-in checkbox "Allow AI code execution" in the bridge window, OFF on every Revit start and never persisted; `ScriptGuard` deny-list (`System.IO/Net/Reflection/Process`, `System.Linq.Expressions`, `Expression`/`Delegate`/`CreateDelegate`/`Compile`/`.Method` — reflection by expression tree or delegate, since 2026-09-15 — `await`, `Task`, `Thread`, `dynamic`, `unsafe`); timeout 5–120 s cooperative via `ct`; audit JSON-lines in `%AppData%\HPRebar\McpBridge\audit\`; pipe ACL `CurrentUserOnly`. A timeout always fails the run and rolls back, even if the script returned.
- **Transaction policy** (ADR-03): `transaction` = `auto` (bridge wraps one Transaction inside a TransactionGroup) | `manual` (script opens its own) | `none` (read-only); `dryRun` always rolls the group back. Every run is one Undo entry `MCP: <label>`.
- Bridge settings/logs: `%AppData%\HPRebar\McpBridge\settings.json` (`AutoStartListener` only), runtime log `%LocalAppData%\HPRebar\McpBridge\logs\`. `ScriptingSelfCheck` compiles and runs one script at startup and logs `MCP scripting self-check OK` — if that line is missing, Roslyn did not load in the add-in's load context.
- **Scripts get `args`** (`ScriptArgs`: `Double/Int/Long/Str/Bool/Strings/Longs/List/Obj/Has/Require`, case-insensitive, coercing) beside `doc/uidoc/app/uiapp/ct/log/progress`. Parameters travel as data so a stored tool's text never changes and compiles once per Revit session. `revit.analyze` (pipe thread, no Revit thread, no opt-in) returns guard/compile verdicts plus literals and `args` keys — the registry validates proposals with it.
- **Tool registry** (ADR-06): files under `%AppData%\HPRebar\McpServer\tools-library\<Category>\<name>\` are the source of truth (`Registry:LibraryPath` to relocate, e.g. into a git checkout); `registry.db` (SQLite WAL + FTS5) indexes them and keeps `runs`/`registry_events`. A `FileSystemWatcher` reloads on edits and the SDK's `ToolCollection` sends `notifications/tools/list_changed`, so approving a tool never needs a server restart. Lifecycle `draft → tested → pending_approval → published → quarantined | deprecated`; policy `manual` (default) means the AI stops at `pending_approval` + `_review/<name>.md` and a human runs `HPRebar.Mcp.Server.exe registry approve <name> --by <who>` (or `list | pending | show | reject | deprecate | quarantine | restore | stats | export | import`). Stability = success rate over the last 50 runs damped for < 10 runs; ≥ 5 runs with > 40 % failures quarantines a tool automatically (tests and bridge-unavailable errors do not count; since 2026-09-14 (engine, both hosts) runs whose error starts with `Argument…Exception:` are the caller's and never count, and the stability window restarts at the tool's last `approved`/`published`/`restore`/`proposed_version`/`imported`/`status_changed` event so a restored tool is not re-quarantined by old failures). Seeds are embedded in the server (`Registry/SeedLibrary/**`, `<Compile Remove>` + `EmbeddedResource`), installed once, and upgraded only while their `_seeds.json` checksum proves the user never edited them.
- **Seed/tool code contract:** plain script body ending in `return`, mm at the boundary, `args.X("key", default)` for every input, `HPRebar.McpBridge.Core.Scripting.ScriptArgs` spelled in full when a helper takes it as a parameter (the bridge's default imports gain that namespace only on the next add-in deploy). `SeedLibraryTests` compiles every seed against `~/.nuget/packages/nice3point.revit.api.revitapi/2026.*/ref/net8.0-windows7.0/RevitAPI.dll` — the wrapper must mirror the bridge's imports exactly, or a script passes the test and fails in Revit.
- **Revit prompts "publisher could not be verified" for the unsigned bridge DLL** after some rebuilds; the user must click *Always Load* on the Revit window. Redeploying (`DeployAddin`) needs Revit closed.

Client wiring: `.mcp.json` (tracked in git but carrying machine-specific paths — never commit local edits to it) has `hprebar-revit` pointing at the published exe `HPRebar/output/HPRebar.Mcp.Server/HPRebar.Mcp.Server.exe` (produce it with `dotnet publish HPRebar/HPRebar.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o HPRebar/output/HPRebar.Mcp.Server`, or `dotnet run -- pack`). Env `HPREBAR_MCP_Bridge__RevitVersion` selects the pipe (default 2026). For a smoke test without a host AI, a stdio harness lives in the session scratchpad (`mcp_call.py`); the MCP Inspector CLI works too but drops environment variables.

Verified live in Revit 2026 (2026-09-12, `plans/…/reports/phase-09-live-verify.md`): the three registry scenarios — a seed hit, a miss packaged into `set_mark_from_comments` and approved through the CLI, a fragile tool quarantined after 5 failures — plus every seed at least in dryRun. Known gaps: no TUnit tests for `ScriptRunner` inside Revit; Dynamo/RevitPythonShell coexistence with Roslyn untested; `.mcp.json` server entry is per machine; R25 builds but is unverified at runtime; tools needing families the RC template lacks (doors, room tags with rooms, ceilings, roofs) are compile-checked only; policy `auto` and `test_tool realRun=true` exercised only in xUnit.

## HPAutoCad MCP Bridge (Dynamic AutoCAD MCP Server)

Phases 1–5 (2026-09-14): loader + ALC + bridge + server exe + registry per host + live verification harness, all verified live in AutoCAD 2026 R25.1. **Plan complete.** Design: `plans/260913-0000-autocad-mcp-bridge-2026/` (architecture, ADRs).

**Bridge runtime:** `HPMCPBRIDGE` command (opt-in "Allow AI code execution" window, unchecked on load). Pipe `hpautocad-mcp-2026` (JSON-RPC NDJSON) carries `autocad.*` methods (same suffix routing as Revit). Execution: guard + Roslyn → `MainThreadQueue` → `Application.Idle` + `PostMessage(WM_NULL)` wake. Globals `doc/db/ed/app/tr/units/ct/log/progress/args` (mm via `ScriptUnits`). Transactions: outer TransactionGroup (policy role), inner `tr` committed immediately (database events fire on outermost only) → changes via HANDSEED + ObjectOpenedForModify + IsErased. `transaction=auto|none|manual` (manual → auto + log); `dryRun` rolls back. `none` still opens `tr`, always aborts, sums to zero on no mods. Undo merged per user command in "HPMCP" lock; per-run undo phase 5. Busy grace 8 s (`-32002`), no drawing `-32003`. Timeout 5–120 s `ct`, opt-in never persisted, audit JSON `%AppData%\HPAutoCad\McpBridge\audit\`. ALC isolates Roslyn 5.9 + Immutable 10.

**Server exe + seeds + registry:** `HPAutoCad.Mcp.Server` (net10, stdio). Profile: `AutocadHostProfile` with registry engine per host (categories, reserved names, host stamp, CLI exe name). **24 tools (40 with the 16 AEC seeds of phases A–D, see "AEC engine" below):** 4 core (`execute_autocad_code`/`get_autocad_context`/`inspect_type`/`cancel_execution`), 8 registry (search/get/run/get_run/propose/test/publish/manage, descriptions host-neutral), 12 embedded seeds (6 read-only: `list_layers`, `list_block_definitions`, `get_entities`, `list_layouts`, `get_drawing_info`, `get_selected_entities`; 6 auto-transaction: `draw_polyline`, `draw_circle`, `add_text`, `create_layer`, `insert_block`, `add_linear_dimension`). Seeds installed into `%AppData%\HPAutoCad\McpServer\tools-library\<Category>\<name>\` on first start in 7 categories (Drawing, Layer, Block, Annotation, Layout, Data, Generic), upgraded on `_seeds.json` checksum change. **Seed contract:** script body ending `return`, points as `{x, y}` objects in mm, `tr` is the bridge's (guard denies `StartTransaction`/`Commit`/`Abort`/`LockDocument`/`ed.Get*`), globals mirror the bridge's, `args.X("key", default)` for every input. `SeedLibraryTests` (58 total) compile every seed against `AutoCAD.NET 25.1.0` from NuGet cache with the bridge's exact imports, guard, and globals—no AutoCAD needed, 0 skipped. Registry root `%AppData%\HPAutoCad\McpServer\` (registry.db + tools-library). `ContextService.Shape` hides Revit-only fields (`revitVersion`, `isFamily`) — Revit output byte-identical, non-Revit output clean. Pipe-in-use message names the host and version via `RequestDispatcher.HostName` / `HostVersion`. Bridge log sink `shared: true` so a second instance can log its pipe conflict.

**Live verification harness (phase 5):** Unattended test at `HPAutoCad/tools/harness/` (`run-live-verify.ps1` + `live-verify.py`, stdio through `McpShared/tools/mcp-session.py`) runs one stdio MCP session through 65 scenarios (64 pass + 1 skip) across AutoCAD and Revit on an isolated registry root (`output/live-verify/registry`). Verifies: execute matrix 18 (none/dryRun/commit, exception, none + modify, manual, guard on `GetPoint`/`Commit`/`SendStringToExecute`, compile error, `cancel_execution` racing, timeout 5 s, busy → **ESC posted → retry automated**, no drawing, audit); every seed 18 (real block, pickfirst set, dryRun); MISS → ad-hoc code + `propose_tool` → `test_tool` → `publish_tool` → CLI approve → `tools/list_changed` in 0.5 s → call by name; fragile tool (unguarded `eKeyNotFound`) → 5 × fail → quarantine → `manage_tool restore` + `propose_tool newVersion` (guarded, `ArgumentException`) → re-approve → stays published on 5 × fail; Revit exe beside (34 tools, Revit lib hash unchanged, opt-in off so E1 skipped); isolation (second AutoCAD fails fast naming AutoCAD, Civil 3D never loads the bundle). Harness: `-IncludeIsolation` / `-OnlyIsolation` / `-SkipRevit` / `-UseLiveRegistry` flags; kills only processes it started; isolates the server and CLI to an ephemeral registry; test report 2026-09-14 run 3: 64 pass + 1 skip + 4/4 isolation + 21/21 harness regression; xUnit: 96 + 109 + 58 (263 total).

**Ribbon tab "HPAutoCad" ▸ "MCP" ▸ "MCP Bridge" (bundle 0.3.0, 2026-09-16):** the same one-button surface as the Revit (`HPRebar` ▸ `MCP`) and Navisworks (`HPNavis` ▸ `MCP`) bridges — 0.2.0 carried three panels and ten buttons, all of them controls the status window already has, so they were removed together with their bridge entry points (`status.subscribe`, `copyLastScript`, `autoStart.get/set`, `path` — `BridgeEntry.Ribbon.cs` is gone; `show/start/stop/status/dispose` remain) and the loader's `RibbonStatusPresenter`. Built by the loader with `Autodesk.Windows` (AdWindows.dll from the `AutoCAD.NET` package — compile-time only via `ExcludeAssets=runtime`, nothing copied). Tab id `HPAUTOCAD_MCP_TAB`, title `HPAutoCad`; panel `HPAUTOCAD_MCP_PANEL` "MCP"; button `HPAUTOCAD_MCP_BRIDGE` "MCP Bridge" → `BridgeActions.Run("show")`, the delegate `HPMCPBRIDGE` calls, so a click needs no drawing and never edits one; tooltip `Command = HPMCPBRIDGE`; disabled with the loader-log path in the tooltip when the bridge failed to start. Icon = one frozen vector `DrawingImage` in `Ribbon/RibbonIcons.cs` (window + plug, the glyph `HPNavis/tools/icons/render-ribbon-icons.ps1` renders for Navisworks; every coordinate even so the 16-px small image is an exact half; ink `#E6E6E6` on COLORTHEME 0 / `#3C3C3C` on 1, plug `#0696D7`). Created once the Ribbon exists, re-created after a workspace switch and rebuilt after a COLORTHEME change (`SystemVariableChanged` → `Application.Idle` → `EnsureCreated` + `FindTab` guard), removed on `Terminate`; never duplicates. No CUIx modification, no change to `acad.cuix` or workspaces. Load-bearing gotchas: `UseWPF` on the loader removes the implicit `System.IO` using (add it explicitly where needed); AdWindows exposes a Ribbon tab header as a `Button` whose `AutomationId` is the tab id (not `TabItem`) and a button as a `Button` named after its text once the tab is selected; `SendCommand('_.WSCURRENT …')` over COM never returns — switch workspaces (and COLORTHEME) via `ActiveDocument.SetVariable`. Verified live 2026-09-16 by `run-ribbon-check.ps1`: 12/12 PASS + 1 MANUAL (icon, one screenshot per theme, both inspected crisp) — tab exactly once, still once after a workspace round trip and a COLORTHEME round trip, `MCP Bridge` opens the window, a second click activates it (still one window), no failure in loader.log. Regressions: bridge 21/21, server smoke 22/22.

Install / update / remove: `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug` with AutoCAD closed deploys the bundle (`%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.McpBridge.bundle\`, autoloaded, SECURELOAD answer *Always Load* once). Removing that folder uninstalls. To test without the bundle: `NETLOAD` `Contents\HPAutoCad.McpBridge.Loader.dll` from a copy. Live check: `pwsh HPAutoCad/tools/harness/run-ribbon-check.ps1`.

**AEC engine (plan `plans/260916-1140-aec-automation-mcp-autocad/`, phases A–D 2026-09-16, E–I 2026-09-17 — plan complete):** the AutoCAD MCP grows from drawing commands into AEC understanding. Design of record: `architecture.md` + ADR-01 (tools stay seeds over `autocad.execute`; the logic is the assembly `HPAutoCad.Aec`, net8.0-windows, referenced by the bridge, added to `BridgeEntry.CompilerReferences`, imported through `HostScriptContracts.AutocadImports` += `HPAutoCad.Aec`, shipped in `Contents\Bridge\`; `ScriptingSelfCheck` logs `aec tolerance 0.5` to prove it resolved in the bridge's load context) + ADR-02 (mm at the tool boundary whatever INSUNITS is — `units` converts; `GeometryTolerance` record pointEquality 0.5 / endpointConnection 10 / collinearity 1 / parallelAngle 0.5° / duplicate 1 / tinySegment 5 / roomGap 25, overridable per call, no literals in services; handles only, `HandleResolver` → INVALID_HANDLE / ERASED / NOT_AN_ENTITY; camelCase envelopes `{success, summary, items, count, offset, truncated, warnings, errors[{code,message,handle}]}` / `{success, createdCount, modifiedCount, deletedCount, affectedHandles, …}`; input that makes a run meaningless throws `ArgumentException` (never counts against stability), partial problems go to `errors[]`; `limit` ≤ 500 + `offset` + `mode` keep results under the 64 KB cap). Layout: `Geometry/` (Pt, Box, Seg, `PlanShape` — not `Shape`, AutoCAD has one — GeometryTolerance, GeometryMath), `Spatial/` (SpatialIndex grid, SpatialPredicates: within/contains/intersects/crosses/overlaps/touches/nearest/distance_to/inside_bbox/inside_polygon), `Issues/` (GeometryIssueDetector: duplicate, near_duplicate, overlapping_segments, tiny_segment, zero_length, open_polyline, endpoint_gap, self_intersection, invalid_geometry), `Model/` (AecEntityRecord, EntityFilter with AutoCAD wildcards, ToolError/ToolErrorCode, AnalysisResult/EditResult), `Cad/` adapters (EntityShapeReader — lines/polylines exact, arcs via `GetArcSegmentAt`+`GetSamplePoints`, other curves via `GetGeCurve().GetSamplePoints`, hatch first polyline loop, blocks/text/dimensions as bounds; EntityQueryService — `SelectionFilter` broad phase with comma lists + wildcards, post-filter while paging, handle order; DrawingContextReader; MeasureService — exact `Curve` length/area/`GetClosestPointTo`/`IntersectWith`; SpatialQueryService), `AecTools` facade. **Seed shim rule:** an AEC seed's `code.cs` reads `args` and calls exactly one `AecTools.*` method (`SeedLibraryTests.Aec_seed_is_a_thin_shim_over_the_engine`); all AEC seeds are `transaction: none`. Phase A tools: `get_drawing_context` (Drawing), `query_entities`, `query_entities_spatial` (Data), `measure_geometry` (Geometry), `detect_geometry_issues` (Audit) — categories Geometry + Audit added to the profile. **Phase B (semantic):** `Classification/` (`AecType` 19 types / 3 disciplines, `ShapeMetrics` — footprint sides from the ring's own edges so rotated rectangles measure right, main axis —, `ClassificationRule` JSON rule with layer/type/block wildcards, closed, size/short-side/aspect ranges, own-text regex, nearby-text regex as evidence, `ClassificationRuleSet` embedded default `Rules/aec-classification.default.json` (26 rules, AIA/NCS + English + Vietnamese) or `%AppData%\HPAutoCad\McpServer\rules\<name>.json` validated on load with path traversal refused, `AecClassifier` best rule wins + alternatives + Unknown on request), `Relationships/RelationshipDetector` (intersect, connected = end-of-run gap with confidence 1 − gap/tol·0.5, near, inside, contains, touching, aligned/parallel/perpendicular on main axes), `Cad/ClassificationService` (text index only when a rule needs it; self-set pairs de-duplicated), `AecTools.Semantic` → seeds `classify_aec_entities`, `get_entity_relationships` (category `Aec`). 31 tools on the server now (4 core + 8 registry + 19 seeds). Tests: `HPAutoCad.Aec.Tests` 69 (net10.0-windows because Aec is net8.0-windows; pure code only; `ReviewRegressionTests` pin the first review's findings), `HPAutoCad.Mcp.Server.Tests` 93 (net10.0-windows now; the seed compile check adds the Aec assembly as a metadata reference). Live: `tools/harness/run-aec-tools-live.ps1` (51 checks, Debug exe + isolated registry, imperial template so mm conversion is exercised; scene has a mirrored arc, a bulge polyline, a hatch, a block with attributes, a 3 000-line perf grid; classification + relationships on it) 51/51 on 2026-09-16; bridge 21/21, smoke 22/22 unchanged. Phase-B gotcha: a `required` member that is `[JsonIgnore]`d makes System.Text.Json throw InvalidOperationException for the whole object (`AecObject.Record` is not required). Review 2026-09-16 (7/10 → fixed): `SpatialIndex` keeps oversized items in a side list every query checks (a site ring among door swings was dropped depending on insertion order) and falls back to a linear scan for huge query boxes; arcs are sampled through `GetGeCurve()` so a mirrored OCS (normal −Z, every mirrored door swing) is not reflected; `IntersectXY` treats lines as parallel when they cannot separate by more than the tolerance over their length (nearly coincident lines are overlaps, not far-away crossings), `touches` excludes collinear overlaps, `IsWithin` also tests segment midpoints (chords across concave notches); `PlanShape.Approximate` (tessellated curves, stand-in rectangles) suppresses tiny-segment / overlap / self-intersection verdicts; detail mode is capped at 20 entities × 64 vertices and `detect_geometry_issues` at 100 issues per page so a response stays under the 64 KB cap; block-name filters match the effective (dynamic) name after the selection; `measure_geometry` reports unresolved handles instead of a generic ArgumentException. Gotchas: `ToolError` had a `Handle` property and a `Handle(...)` factory (CS0102) — factory is `ForHandle`; `Pt`/`Box` computed members need `[JsonIgnore]` or they serialise; `SameVertices` must map reversed *open* chains as `n-1-i` (cyclic offsets are for rings only); two chains that already meet at one end are connected, their other ends are never an `endpoint_gap`; `measure_geometry` success = no errors or ≥ 1 item (a summary alone is not success). **Phase-B review round (6.5/10 → fixed):** relationships report an honest `count` = total that hold (`RelationshipOutcome.Total`) with symmetric relations de-duplicated in-loop over one set (`sameSet`), every relationship carries a real `locationMm` (inner centroid / closest vertex fallbacks, never the origin), axis relations carry `angleDeg` (`aligned` also the offset in `valueMm`), axis pairs above `MaxAxisPairs` 2 000 000 are refused; the default rule set (v2) anchors every stem to a name boundary (`DAM`, `DAM-*`, `*-DAM`, `*-DAM-*` — never `*DAM*`, which took M-DAMPER; STRUCT / S-MONG / COTAS / A-CLNG-GRID / P-SANR-FIXT stay Unknown or their own type, CUASO → Window, ONGGIO → Duct); rule files refuse null lists, misspelt keys (`UnmappedMemberHandling.Disallow`), negative ranges; equal confidence keeps rule-file order; block/text/dimension stand-in rectangles are sized but never `closed` footprints; text/attributes per object cut at `MaxTextChars` 120 / `MaxAttributes` 8; `classify_aec_entities` pages at `MaxClassifyLimit` 50 and `get_entity_relationships` at `MaxRelationshipLimit` 250 (schema `maximum` pinned to the engine caps by `SeedLibraryTests`); one text index serves both relationship sets and warns when it truncates at 5 000 texts; the rule-set `source` says `embedded` / `rules\<file>` and never the profile path; `ReadPolyline` drops a repeated closing vertex. Tests `HPAutoCad.Aec.Tests` 100, `HPAutoCad.Mcp.Server.Tests` 97; live `run-aec-tools-live.ps1` 55/55 (scene 24 entities). **Phase C (editing):** the six write seeds `create_entities_batch`, `update_entities_batch` (Drawing), `manage_blocks_attributes` (Block), `manage_annotations` (Annotation), `manage_hatches` (Drawing), `manage_xrefs` (Data) — `transaction: auto`, `dryRun` via the request flag, edit envelope `EditResult` `{success, createdCount, modifiedCount, deletedCount, affectedHandles, items[ItemOutcome{index, ok, handle, type, changed, error}], warnings, errors}`; `atomic` (default) = validate every item, one invalid item refuses the whole batch with nothing written, a failure while writing throws so the bridge aborts; `atomic: false` writes the valid items. Engine: `Model/EditResult`, `Cad/EditContext` (layer guard LAYER_LOCKED / LAYER_FROZEN, `OpenForWrite`, space current/model/layout, `ApplyProperties`), `EntityFactory`, `EntityUpdater` (text/height/rotation/scale/style/dimStyle/textOverride/attributes/geometry per type/move/rotate/scaleBy), `BatchEditService` (`MaxBatchItems` 500), `BlockService` (dynamic properties in mm/degrees), `AnnotationService` (angular via `Point3AngularDimension`, mleader via `AddLeader` + `AddLeaderLine`; delete refuses geometry), `HatchService` (`detectBoundary` geometric, NOT_CLOSED before anything is created, `Hatch.Area` unreadable in the creating transaction → boundary fallback, `SetDatabaseDefaults` **before** layer/colour or it resets them, `Associative = true` before `AppendLoop(ids)` — the managed API has no persistent-reactor call — makes the hatch follow its boundary), `XrefService` (`XrefPathPolicy`: fully qualified `.dwg`, no `..`, not self, UNC warned; `bind` only Resolved + loaded; detach all-or-nothing), `AecTools.Editing` with flat parameters because the analyzer needs every schema key read as `args.X("literal")`. Read ops under a write tool return the analysis envelope. **Phase-C review round (6/10 → fixed):** every write is two-phase — phase 1 opens for **read** (`EditContext.OpenForEdit`: handle → layer → owner must be a layout, so entities inside block definitions are `UNSUPPORTED_ENTITY`) and validates without mutating (`EntityUpdater.Validate`, `BlockService.ValidateAttributes` incl. each attribute reference's own layer, `ValidateProperties`), so a refused atomic batch leaves the bridge's change counter at 0; phase 2 `Upgrade` + `Apply` with `changed` accumulated for honest midway failures. Single-item ops refuse the whole op on any bad key (hatch update, setDynamic, detach) and single creates return `EditResult.Refused` with the structured code instead of throwing. Result cap: `MaxBatchItems` 200, `MaxReferenceLimit` 100, `MaxDefinitionLimit` 200, `MaxAttributeHandles` 100 / `MaxDynamicHandles` 20, attributes cut 8 × 120 chars, `Settle` lists 20 item errors + a count, shared warnings grouped by `WarnItem` (`EditResultTests` serialises 200 refused items < 60 KB). `EntityFilter.From` defaults `space` to `all` when handles are given. Gotchas: a top-level `using var` compiles in `SeedLibraryTests`' method wrapper but not as a Roslyn script; `query_entities` summary mode carries no `color` (use detail). Tests 122 + 136 (25 seeds); live `run-aec-edit-tools-live.ps1` 62/62 (every op with dryRun + commit, locked/frozen/erased/block-definition paths structured, associative hatch follows a moved boundary, `U` over COM reverts a batch) and `run-aec-tools-live.ps1` 55/55; server 37 tools. **Phase D (QA/QC):** `cad_standards_check` + `audit_aec_drawing` (Audit, `none`) and `create_issue_markup` (Annotation, `auto`). `Standards/CadStandardsRuleSet` is JSON (embedded `Rules/cad-standards.default.json` or `rules\cad-standards.json` via the shared `Model/RuleFileLocator` — `default` / `user` / plain file name, traversal refused, profile path never echoed): layerNaming regex + exempt, entityLayer[] {types, layers}, layerZero, overrides {color, linetype, lineweight, exemptTypes, exemptLayers}, textStyles / dimStyles allowed, textHeights {allowedMm, toleranceMm, space}, blockNaming, unusedLayers — every section optional, validated on load. `Standards/CadStandardsChecker` is pure (records + `DrawingTables` from `Cad/DrawingTablesReader`) and emits `Issues/AuditIssue` (`STD-nnnn`; the shared issue record with `Ordered` = critical → warning → info, category, type, first handle, layer, description, and `AtLeast` for `minSeverity`); `unused_layer` only on the whole drawing (empty filter ⇒ space all, not truncated — a filter with only `space` is a filter), else skipped with a warning. `Cad/AuditService` runs one query through the sections (geometry via the phase-A detector — which checks every space on its own, so a title block repeated on two layouts is not a `duplicate` —, standards) and keeps GEO-/STD- ids; 2 000 geometry findings cap ⇒ `truncated` + warning. `Cad/IssueMarkupService` (+ `.Resolve` partial): circle / rectangle / revcloud (polyline of bulge −0.5 = 106° arcs) at `radiusMm` around `locationMm` — grows to the handles' extents only when there is no location (`MarginFactor` 0.25, `sizedByHandles`) — + `AnnotationService.BuildMLeader` `<id>: <description>` on `HP-MCP-ISSUES` created on demand (only when something is drawn), ACI 1/2/4 by severity, two-phase (spaces opened for write in phase 2 only), `MaxIssuesPerCall` 100; each issue is drawn in the space of its own entities (`space: "auto"`, first resolvable handle; an explicit space warns when it disagrees); table findings (no location, no handles) are skipped with a warning, unresolvable handles are the error; locked/frozen markup layer → `Refused(LAYER_LOCKED/LAYER_FROZEN)`, off layer warned, layer name validated. `AecEntityRecord` gained `Style` and `TextHeightMm` (projected by `query_entities` as `style` / `textHeight`). `AecTools.Audit` (`MaxIssueLimit` 100 — 150 breached the cap). **Phase-D review round (5.5/10 → fixed, `plans/reports/code-review-2026-09-16-aec-phase-d.md`):** `CadStandardsChecker.Exempts` = an empty exemption list exempts nothing (`EntityFilter.Matches` empty = match all is a filter semantic); `DrawingTables` carry `IsDependent` (xref blocks/layers), `IsHidden` (`*ADSK_*` system layers) and `LayersUsedInBlocks` so `unused_layer` / `layer_naming` / `block_naming` never report what cannot be purged or renamed; default rule set widened (`^[A-Z]{1,2}-[A-Z0-9]{2,12}(-[A-Z0-9.]{1,12}){0,5}$`, `S-COLUMNS` / `A-ANNO-TEXT-0.25` / `KT-KICHTHUOC` pass, stems anchored — `*-KT`, `*-KT-*`, never `*KT*` which took the KT- discipline prefix — every dimension flavour listed) and `layer_zero` suppresses the `entity_layer` double count; `RegexMatchTimeoutException` → `ArgumentException` (a catastrophic pattern is the rule file's fault, never a quarantine); `textHeights.space` validated; a requested check the rule set disables is warned. Pinned by `CadStandardsReviewTests` (8). Tests 170 + 192 (35 seeds); live `run-aec-tools-live.ps1` 68/68 and `run-aec-edit-tools-live.ps1` 75/75 (2026-09-17). **Phase E (Structural, 2026-09-17):** `Structural/` — `StructuralMember` (kind column/beam/wall/slab/opening from the classified object; width × depth / `Ø` for footprints, `L <length>` for beams and walls however drawn; axis only for runs or footprints with aspect ≥ 1.5; a block's `MARK` attribute is its mark, `MarkHandle`/`MarkSource`), `GridDetector` (open runs on grid layers = lines with collinear pieces merged — a bubble stub is not a second line —, circles/INSERTs = bubbles labelled by a short TEXT inside or the block's attribute, a line takes the bubble *on its axis* nearest an end within `BubbleReachMm` 2500, intersections extended by the reach and de-duplicated, spacing per direction, XLINE/RAY counted as `unbounded`), `StructuralChecks` (connectivity `gap_to_support` ≤ 3 × endpointConnection / `unsupported_end` / `beam_without_supports` — a parallel beam within the landing tolerance is the same beam twice, never a support; column alignment to the nearest intersection within `GridSearchRadiusMm` 2000, `column_off_grid` beyond `DefaultAlignmentToleranceMm` 25, `column_no_grid`, no grid at all = one warning; openings `opening_through_column` only when interiors overlap — a shared face/corner is `opening_near_column` at 0 mm —, `opening_near_column` < 300, `opening_outside_host`; ids `STR-CON/ALN/OPN-nnn`), `MemberTagging` (reading order by gap-clustered bands `DefaultRowBandMm` 250; `Assign` per kind from `start` padded to `digits`; an existing mark with the kind's prefix is `kept_existing` and its *number* reserved, another prefix `kept_foreign` untouched, `overwrite` renumbers all; `DuplicateExisting`; `Schedule` rows by kind + section). `Cad/StructuralService` (one classification pass; `ClaimMarks`: a mark-shaped TEXT within `MarkReachMm` 300 whose letters are a default or given prefix belongs to exactly one member — containing it, else its kind's prefix, else nearest; `ParsePrefixes`), `Cad/StructuralWriteService` (+ `.Table`): `WriteTags` two-phase — attribute via `BlockService`, the existing mark TEXT edited in place, else a new middle-centred DBText at the centre on `S-ANNO-TEXT` created on demand; nothing opened for write before every target is validated, phase 2 skipped when nothing is pending; `WriteTable` via the `Table` API (zero rows refused, 20 marks per cell). `AecTools.Structural`: `MaxGridLimit` 100 + `offset`, `MaxIntersectionsListed` 200, `MaxMemberLimit` 100, `MaxTagMembers` 120 (more = ArgumentException), negative args are the caller's error, marks/tables go to the members' space (`all` needs an explicit `space`). Seeds (category Structural): `structural_detect_grids`, `structural_detect_members`, `structural_member_connectivity_check`, `structural_column_alignment_check`, `structural_opening_conflict_check` (`none`), `structural_tag_members`, `structural_generate_member_schedule` (`auto`); `detect_members` / `schedule` / `tag_members` take `prefixes`. `EntityShapeReader`: an INSERT with attributes gets its stand-in footprint from the definition's extents (attribute definitions excluded, cached per definition) — a column block with its label beside it measures 400×400; `boundsMm` unchanged. **Phase-E review round (5/10 → fixed, `plans/reports/code-review-2026-09-17-aec-phase-e.md`):** the five Highs were a write tool without a member cap, grids over the 64 KB cap, block `MARK` attributes overwritten under `overwrite: false`, "overwritten" adding a second text, and block bubbles unlabelled / corner bubbles stolen — all pinned by `StructuralReviewTests` (9, incl. envelope size at every cap). Tests 179 + 193 (35 seeds); live `run-aec-tools-live.ps1` 68/68 (step N; scene 43 entities with a stub on grid A, bubble 2 as an attributed block, a `COL-400` block with its MARK beside the symbol) and `run-aec-edit-tools-live.ps1` 78/78 (step M: preview, apply, overwrite in place with `prefixes`, idempotence, foreign marks, refused table, table); server 47 tools. **Phase F (Architecture, 2026-09-17):** `Architecture/` — `RoomLoopFinder` + `WallGraph` + `OpeningBridger` (rooms = bounded faces of the wall drawing: pieces cut at crossings / T-junctions / collinear overlaps, ends within `tolerance.roomGap` snapped onto the wall they miss, doorways bridged by virtual walls — two free ends facing along one line, or two jamb lines spanning the two long lines of a double-line wall, `detection.minOpeningMm` 600..`maxOpeningMm` 2500 —, dangling chains peeled with a tail ≤ `maxWallThicknessMm` 500 past its own wall's crossing counted as an overshoot, faces walked by leftmost turn; dropped and counted: faces < `minAreaMm2` 0.5 m², thinner than `minWidthMm` 450 (wall cavities), the outer line of a double wall (every vertex within the thickness of a loop it holds); islands stay inside a gross room), `Room` (id `R-nnn`, source walls|outline, `InsidePoint`, `Describe` capped at 32 vertices / 32 handles / 16 texts + counts), `RoomLabelRules` (caller regexes, 200 ms timeout; default number = 2–4 digits or letters + separator + 3–4 digits — `101`, `A-101`, `P.101`, `1.01`, never `B01` / `C1` / `KT-12`) + `RoomLabels` (every text line is a label; number first, department by pattern group 1, name = longest rest; area lines ignored so the tool's own tags read back), `RoomBoundaryChecks` (`open_boundary` critical beyond `maxGapMm` 300, one `boundary_gap` per facing pair at the midpoint, `boundary_gap_closed` / `opening_assumed` info, `room_overlap` only when interiors interpenetrate — tiled outlines are fine —, `room_inside_room` info, `duplicate_room`, `unlabelled_room`; ids `ARC-nnn`), `AreaSchedule` (room | name | department, upper-cased groups, ≤ 20 rooms per row), `AutoDimensionRules` (registry, rules as data, one rule `overall` = aligned dimensions of the bounding box on `sides`). `Cad/RoomService` (one Architecture classification pass; with no layers/types in the filter the query is narrowed to the rule set's wall + room layers; `filter.space: all` refused; explicit `Room` outlines replace the wall loop they duplicate (≥ 60 % of the area), an outline holding other outlines is a zone; `Select` by id or handle — a party wall selects both rooms), `Cad/RoomWriteService` (+`.Dimensions`): two-phase tags (MTEXT from `format` placeholders `{name} {number} {department} {id} {areaM2} {areaMm2} {perimeterMm}` with `\P` breaks, or a block whose attribute keys are checked against the definition first) on `A-ANNO-ROOM` created on demand, `MaxDimensions` 120 aligned dimensions; summaries list the plan in a preview and only what was written after. `EditContext.Layers` partial (`CheckAnnotationLayer`, `EnsureLayer`) is shared with the phase D/E writers. `AecTools.Architecture`: `MaxRoomLimit` 30, `MaxOutlineVertices` 32, `MaxRoomTags` 120, `MaxAreaRowLimit` 50; one `detection` block (`DetectionKeys`) + `tolerance` + `labels` + `maxCandidates` on all five tools so room ids agree; warnings when the text index or the scan is truncated. Seeds (category Architecture): `arch_detect_rooms`, `arch_room_boundary_check`, `arch_generate_area_schedule` (`none`), `arch_create_room_tags`, `arch_auto_dimension_plan` (`auto`). Rule set v3: `a-wall-run` accepts ARC / ELLIPSE. **Phase-F review round (5/10 → fixed, `plans/reports/code-review-2026-09-17-aec-phase-f.md`):** the Highs were doors (no notion of an opening — every real plan read as 0 rooms or one merged room), tiled outlines reported as overlaps, a hall with a shaft dropped as "nested", `MaxDimensions` over the cap, and an overshoot heuristic that swallowed real gaps. Pinned by `ArchitectureTests` (12, incl. envelopes at every cap). Tests 191 + 222 (40 seeds); live `run-aec-tools-live.ps1` 75/75 (step R; scene 55 entities with a two-room single-line plan, two 900 mm doorways, a 250 mm gap) and `run-aec-edit-tools-live.ps1` 89/89 (step A); server 52 tools. **Phase G (MEP, 2026-09-17):** `Mep/` — `MepNetworkBuilder` (runs = Pipe / Duct / CableTray open chains, nodes = Equipment / Fixture / Terminal / Fitting; a run end connects within `tolerance.endpointConnection` to another run's end (joined), a run's body — another's or its own loop, never the segment it sits on — (tee) or a node; a node is attached to every run ending on it *or passing through it*; union-find over runs where direct connections join and a node joins only runs of one system — supply and return meet at an AHU without merging, a node belongs to every network it touches; crossings counted, never connected; duplicates = same system, parallel, offset ≤ endpointConnection, shared run > `tolerance.duplicate` (a copy a few mm off counts); `DoubleLineDucts` heuristic; `MaxElements` 20 000), `MepChecks` (`near_miss` one per facing pair at the midpoint, `open_end`, `disconnected_run`, `orphan_node` warning/info — none when there are no runs —, `duplicate_run`, `mixed_system` for direct joins only; ids `MEP-nnn`), `Cad/MepService` (one or two classification passes — MEP layers + types, then fitting block names on INSERTs — without a text index, `examined` summed; `space: all` refused; closed / tiny runs set aside and counted; `ParseSystems` first-declared-match), `AecTools.Mep` (`MaxNetworkLimit` 30 with 16 handles + 8 open ends per network, `MaxEndpointLimit` 150 with `connectedTo` ≤ 4, `MaxIssueLimit`; shared `detection` block `MepDetectionKeys` = nearMissMm (must exceed endpointConnection), systems; warnings for no runs / narrowing / truncation / closed or tiny runs / double-line drafting). Seeds (category MEP, all `none`): `mep_detect_network`, `mep_connectivity_check`, `mep_endpoint_check`. Rule set v4: runs accept ARC + SPLINE, `closed: false`; the generic `CO-*` fitting pattern is `CO-ONG*`. **Phase-G review round (5.5/10 → fixed, `plans/reports/code-review-2026-09-17-aec-phase-g.md`):** inline nodes were orphans and a node merged every system into one "mixed" network; an offset copy was a tee, not a duplicate; facing ends were two near misses; a ring main could not find its own far end; envelopes over the cap at 200 endpoints / a page of `mixed_system`. Pinned by `MepTests` (6). Tests 197 + 241 (43 seeds); live `run-aec-tools-live.ps1` 83/83 (step V; scene 64 entities with a pipe main + tee + 50 mm miss + lost run + copy + inline valve, a duct with a diffuser, an orphan diffuser) and `run-aec-edit-tools-live.ps1` 89/89; server 55 tools. **Phase H (Coordination, 2026-09-17):** `Coordination/` — `ClashClassifier` (what two meeting shapes mean: `hard_clash` critical only when an MEP element interpenetrates a member or another service's run — crosses / overlaps / lies inside / ends inside, a route × route crossing whose end is within `endpointConnection` being a tee; `contact` info for boundaries meeting — a beam on a column face or at its centre, a tee, equipment on a run, a door in its wall, any structural / architectural members meeting; `area_overlap` info inside or across a `Room` / `StructuralSlab`; `Between` = closest vertex–foot midpoint for clearance locations), `ClashDetector` (broad phase over set B grown by the clearance, pairs only within one space, same-set once, `DistanceXY(upTo)` for `clearance_clash` warning, `ct` per pair, `MaxPairs` 200 000 + `MaxSegmentPairs` 200 M, ids `CL-nnnn`), `OpeningPlanner` (intersections `proper: false` sorted by station; an outline pass = the stretch between consecutive stations whose route midpoint is strictly inside — a vertex on the face passes, starting on the near face passes, ending inside / touching / along a face does not —, longer than `maxChordMm` 1 000 counted as `LongChordsSkipped`; a host line passes where the route changes side; sizes pipe 150 / duct 400 / tray 300 + 2 × 50; `MaxRequests` 100), `Cad/CoordinationService` (a set = `filter` + `aecTypes` classified once, subjects carry their space, tiny chains set aside), `Cad/OpeningWriteService` (two-phase rectangle turned along the host + MLeader `<id>: <routeType> <route> through <hostType> <host> (W×H)` on `HP-MCP-OPENINGS` ACI 30; preview lists 100 + `truncated`, a longer apply refused), `AecTools.Coordination` (`MaxClashLimit` 80, `minSeverity` default `warning` — contacts / area overlaps counted not listed —, `DefaultHostTypes` walls + beams (slab selectable), `RouteSpace` = explicit → filter → the one space the routes live in; empty sets warned). `SpatialPredicates.DistanceXY(upTo)` culls by box separation and, above 4 096 segment pairs, queries a cached per-shape segment index (also `IntersectionPoints`); `PlanShape.SegmentBounds`. Seeds (category Coordination): `aec_clash_check` (`none`), `aec_create_opening_requests` (`auto`). **Phase-H review round (5.5/10 → fixed, `plans/reports/code-review-2026-09-17-aec-phase-h.md`):** contact was a critical hard clash (a frame against itself = only criticals), only proper crossings counted and entry/exit pairing drew phantom openings, area containment critical, clearance located metres away, cavity runs and slab chords as passes, the pair cap not bounding the work, `MepEquipment` documented, cross-space pairing. Pinned by `CoordinationReviewTests` (8, incl. < 2 s on stacked curved runs). Tests 210 + 253 (45 seeds); live `run-aec-tools-live.ps1` 90/90 (step W) and `run-aec-edit-tools-live.ps1` 96/96 (step O); server 57 tools. **Phase I (Change sets, 2026-09-17):** a change set is a list of write-tool calls recorded instead of applied — every write seed takes `changeSetId` and its `AecTools` method starts with `ChangeSetRecorder.TryRecord` (known tool, valid `op`, `manage_xrefs` attach only, every handle the call names opened as an entity; `args.Raw.Clone()` kept) — listed by `preview_change_set` / `get_change_summary` (`none`), replayed in one run by `commit_change_set` (`auto`; `WriteToolTable` = 12 readers mirroring the shims with `changeSetId` null, pinned against the write seeds; atomic by default: an op that does not succeed → `ArgumentException` naming the op so the bridge aborts and stability is untouched) and undone by `rollback_change_set` (`auto`): created erased, erased un-erased (`Erase(false)`, same handle), modified restored by `RXObject.CopyFrom` from non-resident `Entity.Clone()` snapshots taken at `Database.ObjectOpenedForModify` while the commit ran (HANDSEED-new entities skipped, `MaxSnapshots` 2 000; dimensions `RecomputeDimensionBlock`, hatches `EvaluateHatch`); `keep` closes a set (work kept, undo released). `ChangeSets/` (pure: `ChangeSetLedger` state machine pending → committed → rolled_back | closed, pending → discarded, caps 20 live sets — the oldest committed closed at the cap — / 200 ops / 64 KB args; `UndoRule` — a commit or rollback undone by its request (dryRun) or by `U` is noticed on the next call only when every class of handle agrees: created all gone or erased, erased all back, modified all reading as their snapshot through a `Fingerprint` over layer / colour / linetype / lineweight / style / geometry / text / hatch pattern / dynamic properties; a partial rollback (locked layer) leaves the set committed with its snapshots; `ChangeSetEnvelopes` shape commit / rollback results under the cap through `Settle` / first 20 failures + counts by code), `Cad/ChangeSetStore` (one ledger per drawing keyed by `Database.UnmanagedObject` — the managed wrapper is a new object every request — and `FingerprintGuid`; dropped on `DocumentToBeDestroyed`, `DropAll` from `BridgeEntry.Dispose`), `Cad/ChangeSetRecorder` / `ChangeSetReplay` / `ChangeSetSnapshots` / `ChangeSetRollback`, `AecTools.ChangeSets` (`MaxChangeOpLimit` 30 × `MaxOpArgsChars` 500). Seeds (category ChangeSet): `begin_change_set`, `preview_change_set`, `get_change_summary`, `commit_change_set`, `rollback_change_set`. Gotchas: the bridge's dryRun leaves what a run appended as *erased* objects whose handles still resolve (gone ≠ unresolvable); `query_entities` does not project a hatch's pattern (read it through the API). **Phase-I review round (6/10 → fixed, `plans/reports/code-review-2026-09-17-aec-phase-i.md`):** no exit from `committed`, snapshots released before the undo was confirmed, a verifier blind to most updates, envelopes over the cap, engine-exception aborts, xref ops recorded, three handle keys only, pointer-only store identity. Pinned by `ChangeSetTests` (9). Tests 225 + 280 (50 seeds); live `run-aec-edit-tools-live.ps1` 109/109 (step Z) and `run-aec-tools-live.ps1` 90/90; server 62 tools.

**Publish & deployment:** Exe 7.4 MB (publish SelfContained=false). `.mcp.json` entry (user adds, untracked): `hprebar-autocad` → exe path + env `HPAUTOCAD_MCP_Bridge__HostVersion=2026`. The published exe is locked while a `hprebar-autocad` MCP server runs (every Claude Code session spawns one): republish after restarting Claude Code, or the new seeds stay in the Debug exe only.

**Known gaps:** Revit opt-in with the new engine unverified at runtime (harness E1 skipped); Revit 2025; AutoCAD 2026 Update 1.2 (.NET 10); `test_tool realRun=true`; FTS name boost; `get_run.revitVersion` field name kept (alias if needed); modal dialog while waiting (same `IsQuiescent` path as busy, not exercised); per-run undo (needs `ExecuteInCommandContextAsync`); `HPRebar/output/HPRebar.Mcp.Server.exe` not republished (locked by running `hprebar-revit` MCP — restart it then publish).

## HPNavis MCP Bridge (Dynamic Navisworks MCP Server)

Phases 0–5 (2026-09-15): engine multi-target (`net48`) + plugin + server exe + 12 seeds + live-verify harness, all verified live in Navisworks Manage 2026 (23.0.1432.76). **Plan complete.** Design: `plans/260915-0824-navisworks-mcp-2026/` (ADR-01..05, evidence E1–E21, per-phase reports + code reviews).

**Runtime — .NET Framework 4.8, no load context:** Roamer.exe hosts the CLR 4.8, so `HPNavis.McpBridge` is `net48` and consumes the `net48` assets of `HPRebar.McpBridge.Core`/`HPRebar.Mcp.Contracts`. Roamer reflects over every plugin type before any plugin code runs (discovery-before-resolver), which is why Contracts ships a real `net48` asset (the netstandard one references System.Text.Json 10.0.0.0, unbindable to the 10.0.0.12 net462 package asset). Roslyn 5.9 + Immutable 10 load through a process-wide `AppDomain.AssemblyResolve` allow-list (`PluginAssemblyResolver`: only names shipped in the plugin folder, only for requesters inside the folder or none — and since Roamer passes `RequestingAssembly == null` for every request, the real gate is the version family: same major and not newer than our file, so another plugin's System.Text.Json 8.0 request gets nothing; the foreign `NavisworksMCPPlugin` beside it keeps its own copies). Plugin identity `[Plugin("HPNavis.McpBridge", "HPNV")]` (EventWatcher: listener + executor) + `HPNavis.McpBridge.Ribbon` (`HPNavisRibbonPlugin : CommandHandlerPlugin` — **Ribbon tab "HPNavis" ▸ panel "MCP" ▸ button "MCP Bridge"**, the same one-button surface as the Revit MCP ribbon; declared by `[RibbonLayout]`/`[RibbonTab]`/`[Command]` attributes + `en-US\HPNavisRibbon.xaml` (Navisworks' own layout XAML, kept out of WPF markup compilation by `<Page Remove>`) + `en-US\HPNavisRibbon.name` strings, icons `Images\McpBridge_16|32.png` rendered pixel-exact from one vector glyph by `HPNavis/tools/icons/render-ribbon-icons.ps1` — window with a plug, ink #3C3C3C / accent #0696D7; `CallCanExecute.Always` so the button is not greyed by the document state; Navisworks itself greys every tab while its start page shows, i.e. with no model) + `HPNavis.McpBridge.Window` (AddIn hidden with `AddInLocation.None`, kept for `Application.Plugins.ExecuteAddInPlugin("HPNavis.McpBridge.Window.HPNV")`; `HPNAVIS_MCP_BRIDGE_SHOW_WINDOW=1` opens the window at start for harnesses). Ribbon verified live 2026-09-16 by `tools/harness/run-ribbon-check.ps1 -WithNoDoc`: 14 PASS + 1 MANUAL (icon screenshot), tab exactly once, no window before the click, click opens it (log `MCP bridge status window opened`), second click activates, no Add-ins entry, no-model start page greys the tab. Deploy folder `%AppData%\Autodesk\Navisworks Manage 2026\Plugins\HPNavis.McpBridge\` (folder name = assembly name, required by Navisworks); removing the folder uninstalls.

**Bridge runtime:** pipe `hpnavis-mcp-2026` (JSON-RPC NDJSON, `navis.*` methods, same suffix routing; `PipeSecurity` owner-SID ACL = the net8 `CurrentUserOnly` rule, so an elevated Roamer refuses a non-elevated server). Execution: guard (`GuardProfile.Navis`: `MessageBox`, `Transaction`, undo/redo/rollback members, `Database`/`NavisworksCommand` SQL surface, COM/Automation/Interop namespaces, `System.Windows.Forms`/`System.Data`) + Roslyn on the pipe thread → `MainThreadQueue(expireWithoutTicks: true)` → `Application.Idle` + `PostMessage(WM_NULL)` wake; a timer expires the busy grace because Idle is silent under a native modal. Quiescence = Progress depth 0 + `IsWindowEnabled(main)` + no active transaction; busy grace 8 s → `-32002`, no model (`Document.IsClear`) → `-32003`, opt-in off → `-32001`. Globals `doc/app/units/ct/log/progress/args` (mm via `ScriptUnits` from `doc.Units`). **Transactions (ADR-02):** Navisworks has no scoped rollback, so the bridge owns the only transaction: `auto` commits one undo entry `MCP: <label>` (suffixed `(n)` when the top already carries it); `dryRun` = commit then `Document.Rollback()` **only if** `NextUndo` is our own entry and something changed (`NavisChangeCounter` fingerprints selection sets, saved viewpoints, models, selection, clash tests, `NextUndo`, `IsModified`); `none` + modification → error + rolled back; `manual` ≡ `auto` with a log line; an exception after an edit rolls back. `Changed` is reported even when rolled back. **Heavy gate (ADR-04):** `AppendFile/MergeFile/OpenFile/SaveFile/ExportToNwd/PublishFile/GenerateImage/TestsRun*/TestsCompact*` are refused with diagnostic `HEAVY` naming the checkbox until the user ticks "Allow heavy operations" (second opt-in, OFF on every start, never persisted, forced off when execution is off); with it on, string literals are policed (UNC paths and paths under `HPNavis\McpBridge|McpServer` or `\Autodesk\Navisworks Manage` refused; `doc.Clear()` receiver-matched), the timeout ceiling rises from 120 s to `HostScriptContracts.NavisHeavyMaxTimeoutSeconds` = 600 (shared by bridge and `NavisHostProfile`), heavy runs are audited `started` + `[heavy]`, and a heavy call is never undone (`dryRun` refused up front). Serializer: `BoundedOutputStream` (~15 KB flush points), Navis collections capped at 200 items. Audit `%AppData%\HPNavis\McpBridge\audit\audit-YYYYMMDD.log`, runtime log `%LocalAppData%\HPNavis\McpBridge\logs\` (shared sink so a second Roamer logs its pipe fault), settings `%AppData%\HPNavis\McpBridge\settings.json` (`AutoStartListener` only). `ScriptingSelfCheck` compiles + runs a `Search` at startup → `MCP scripting self-check OK` line.

**Server exe + seeds + registry:** `HPNavis.Mcp.Server` (net10, stdio; `Program.cs` = `McpServerHost.RunAsync(args, NavisHostProfile.Instance)`). Profile: HostId `navis`, ServerName "HPNavis MCP", pipe version from `HPNAVIS_MCP_Bridge__HostVersion` (2026 only), categories Model/Search/Selection/Viewpoint/Clash/Timeliner/Report/Data/Generic, `MaxTimeoutSeconds` 600, CLI `HPNavis.Mcp.Server.exe registry …`. **24 tools:** 4 core (`execute_navis_code`/`get_navis_context`/`inspect_type`/`cancel_execution`), 8 registry (host-neutral descriptions), 12 embedded seeds in `Registry/SeedLibrary/**` — 8 read-only (`get_model_info`, `get_selected_item_properties`, `find_items_by_property`, `list_selection_sets`, `list_viewpoints`, `get_clash_results`, `get_timeliner_tasks`, `summarize_by_category`), 3 review edits under `auto` (`create_selection_set_from_search`, `create_viewpoint`, `override_color_by_search`), 1 heavy (`create_and_run_clash_test`, tag `heavy`, 600 s, `test_tool` refuses it — heavy tools are seed-only: `navis.analyze` runs the heavy gate with the opt-in off, so `propose_tool` of `TestsRun*` is refused). Resources `navis://document/info`, `navis://selection`, `registry://tools[/{name}]`; prompts `navis_query_template`, `navis_review_template`, `toolify_run`. Registry root `%AppData%\HPNavis\McpServer\` (`registry.db` + `tools-library`), relocatable via `HPNAVIS_MCP_Registry__LibraryPath/DbPath`. **Seed contract:** script body ending in `return`, `args.X("literal", default)` for every input (the analyzer proves schema ⇔ code — no helper may hide the key), mm at the boundary via `units`, `Search` + `SearchCondition` with `Locations = DescendantsAndSelf` + `PruneBelowMatch`, `VariantData` read by type (`IsDisplayString`… — `ToDisplayString()` throws otherwise), caller errors as `ArgumentException` (excluded from the stability window), no `Transaction`, walks bounded by `Take`. `SeedLibraryCompileTests` (net48) compiles every seed with `BridgeEntry.CreateScriptCompiler` — the bridge's exact imports/references/globals — and runs the real `NavisHeavyGate`; `SeedLibraryStructureTests` (net10) checks records under the profile.

**Live verification harness (phase 5):** `HPNavis/tools/harness/` (README there): `run-live-verify.ps1` + `live-verify.py` drive one stdio session per phase on an isolated registry root (`HPNavis/output/live-verify[/<Tag>]/registry`) — `disabled` (opt-in off), `main` = E execute matrix 15 (read, W1 commit, dryRun, same-label dryRun, empty dryRun, exception, none + modify, manual, guard ×5, heavy OFF, compile error, cancel racing, timeout 5 s, context while running < 1.5 s, audit) + S seeds 14 + R registry loop 18 (MISS → ad-hoc → `get_run` → `toolify_run` → `propose_tool count_items_by_source_file` → `test_tool` → `publish_tool` → CLI approve → in `tools/list` in 0.5 s → call by name; fragile `InvalidOperationException` tool quarantined after 5 fails → `restore` + `newVersion` with `ArgumentException` → re-approved → 5 argument errors keep it published; heavy proposal refused) + C 5, `modal`/`aftermodal` (Open dialog via Ctrl+O → busy after 8.3 s → closed → runs again), `heavy` (clash test 3 015 results in ~0.1 s, `AppendFile` MEP.nwc + context), `nodoc` (`-WithNoDoc`), isolation (`-IncludeIsolation`: second Roamer's status window says "already in use — another Navisworks 2026 instance", first keeps serving; plugin folder parked → silent Roamer; restored in `finally`; **never touches the foreign `NavisworksMCPPlugin`**). Result 2026-09-15: runs 2 and 3 **62 pass (59 Python + 3 PowerShell), 0 skip, 0 fail** in ~150 s each (run 1: 59 + 1 fail on the harness's own log-text assertion, fixed; run 4 after the code review: same 62). Older harnesses stay: `run-bridge-unattended.ps1` (pipe, 43 checks ×2 + no-doc), `run-server-smoke.ps1` (9), `run-seeds-live.ps1` (18 + 2). All Windows PowerShell 5.1 (UIA + SendKeys, self-relaunch from pwsh), direct `Roamer.exe "<model>"` launch (the Automation API's Roamer exits within ~15 s here), UIA only inside our own window, Win32 for Roamer's dialogs, graceful close answering the save prompt *No*, kills only what it started.

Install / update / remove: `dotnet build HPNavis/HPNavis.slnx -c Debug` with Navisworks closed deploys the plugin folder including `en-US\` and `Images\` (`-p:DeployPlugin=false` while Roamer is open — it locks the files). `dotnet test HPNavis/HPNavis.McpBridge.Tests` (135, net48; the 11 `RibbonPluginTests` pin attributes ⇔ XAML ids ⇔ `.name` keys ⇔ PNG sizes and need the installed `Autodesk.Navisworks.Api.dll` at run time) and `dotnet test HPNavis/HPNavis.Mcp.Server.Tests` (49) need the Navisworks install (API referenced from `Directory.Build.props`), not a running Roamer. Publish: `dotnet publish HPNavis/HPNavis.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o HPNavis/output/HPNavis.Mcp.Server` (7.4 MB). `.mcp.json` entry (user adds, untracked): `hprebar-navis` → that exe + env `HPNAVIS_MCP_Bridge__HostVersion=2026`; stop the running server before republishing (the exe is locked).

**Known gaps:** Ribbon icon is a 16/32 PNG (pixel-exact at 100 % DPI; a vector `DrawingImage` swap through `Autodesk.Windows.ComponentManager.Ribbon` is the upgrade if a high-DPI screen ever matters); `ExecuteAddInPlugin`/Automation-API start unverified (Roamer exits); area/volume units in `get_selected_item_properties` printed raw; `_seeds.json` upgrade path exercised only by the engine tests; `test_tool realRun=true`; Navisworks 2025/2027; Roamer elevated vs non-elevated server (rule stated, not driven live).

## HPEtabs MCP Bridge (Dynamic ETABS MCP Server)

Design: `plans/260916-2152-etabs-mcp-2026/` (architecture, ADR-01..05, red-team, `reports/phase-01-spike.md`). Phase 0 (2026-09-16) added the engine constants (see the `McpShared/` row); phase 1 (2026-09-17) built `HPEtabs/` and ran the COM spike against ETABS 22 v22.7.0.4095 on the dev machine; phase 2 (same day, `reports/phase-02-bridge-runtime.md`) landed the writing runtime — semantic tiers from a generated allow-list, unconditional save + `.EDB` snapshot before every W/D run, fingerprint, path policy — verified live (`run-live-verify.ps1 -Phase bridge -Runs 2`: 48 checks × 2); phase 3 (same day, `reports/phase-03-seeds.md`) added the 12 embedded seeds (`tools/list` = 24) and verified them live, including a real `run_analysis`; phase 4 (same day, `reports/phase-04-live-verify.md`) proved the whole chain end to end — `run-live-verify.ps1 -Phase full -Publish -Runs 3` → **3 × 102 checks pass** with the bridge and the server running from their publish folders, the registry loop included — and re-ran the three other hosts' `tools/list` against the phase-0 snapshots. **Plan complete.**

**Why a standalone bridge app and not an add-in or an in-process server (ADR-01):** ETABS.exe is the COM `LocalServer32` behind `CSI.ETABS.API.ETABSObject`; `ETABSv1.dll` is a managed netstandard2.0 wrapper (0 `[ComImport]`, attaches through P/Invoke `oleaut32!GetActiveObject`), ETABS 22 itself runs on .NET 8. Every Claude Code session spawns its own server, so hosting the engine in the server would mean N COM attachments and N opt-in windows on a model with no transactions; one user-owned bridge process gives one queue and one opt-in, and keeps the server + its tests buildable without ETABS. `McpBridgeHost`/`PipeListener`/`RequestDispatcher`/`BridgeClient` are reused unchanged (dispatcher gets the ETABS opt-in text through the additive `executionDisabledMessage`).

**Runtime facts verified in the spike (do not re-research):** `Helper` implements `cHelper` **explicitly** — call `GetObject` through the interface. With **more than one ETABS running, `GetObject` returns the newer instance**; the bridge warns and never picks by pid (user: Tools › Active Instance for API). `File.Save(path)` **is save-as** (re-points the model's current name), writes sidecars `.$et` + `.ico`, ~1 s for 74 KB, no dialog — and afterwards **`GetModelFilename(true)` returns the `.$et` name**, so anything keyed on the model file must normalise to `.EDB`. No model open = `GetModelFilename()` == "(Untitled)" (not a rooted path); `GetNameList` then answers ret 0 / count 0. `GetNameList(ref int, ref string[])` returns 0 on PointObj/FrameObj/AreaObj/LoadPatterns/LoadCases/RespCombo/PropFrame/PropMaterial; `Story.GetStories_2` has **10** ref parameters (`ref double BaseElevation` first); `Analyze.GetCaseStatus(ref int, ref string[], ref int[])` (1 = not-run). Present units are forced to `eUnits.kN_mm_C` for every run and restored in `finally` (user's own units survive). A COM proxy created on the STA worker **can be called from an MTA thread** (the STA is a design choice — one owner, serialised — not a requirement). **ETABS keeps serving COM while a modal dialog is open** (`IsWindowEnabled(main)` stays true, the call answered in 0.5 s) — "busy" comes from a running script, the window pre-check is harmless. Liveness: `Process.Exited` (single instance) or any `COMException` 0x800706BA/0x800706BE/0x80010108/0x80010012/0x80010007 → the attachment drops and requests get `-32003` "click Attach" in 0.1 s; Attach again needs no bridge restart. "Unlocking discards analysis results" is still `[chưa xác minh]` (the throw-away model had no results).

**Bridge:** `EtabsAssemblyResolver` (`AssemblyLoadContext.Default.Resolving` → install dir from env `HPETABS_ETABS_DIR` → CLSID `LocalServer32` → Program Files; the wrapper is **never copied**, `Private=false`; must be installed before any method naming an `ETABSv1` type is JIT-compiled — `BridgeEntry.Start` installs it, `Wire()` is `NoInlining`), `EtabsExecutor` (+ `.Audit`, `.Worker`: foreground STA worker ticking `MainThreadQueue(expireWithoutTicks)`, a control lane for Attach/Detach ahead of the queue, `_running`/`IsQuiescent`). **Tiers (ADR-02, phase 2):** `Resources/etabs-oapi-tiers.txt` = one row per method of every ETABSv1 interface (1 281 rows: 733 R / 446 W / 102 D, `path=` marks by-value file parameters), generated once by `tools/generate-oapi-tier-fixture.ps1` through reflection over the installed wrapper and reviewed by hand — rules in the file header (`cHelper.*` + `cOAPI` lifecycle D; a by-value `FileName|csvFilePath|SourceFileName|FilePath|Path` parameter D — a `ref FileName` output never counts; `cAnalysisResults/cAnalysisResultsSetup/cView/cSelect` R; `Start/Modify/Merge/Reset/Clear/Rename/Show/Export/Import/Replicate/Delete` prefixes D; explicit D names; `Get*/Is*/Has*/Verify*`, `Count/RefreshView/Visible` R; everything else W — so `EditGeneral.Move`, `SetPresentUnits`, `Analyze.SetRunCaseFlag` are W). `etabs-oapi-index.txt` snapshots the CHM's 1 107 method topics and a test proves every one has a row. `EtabsTierAnalyzer` is **semantic**: `Script.GetCompilation()` → `SemanticModel`, every member access bound to a symbol whose containing type is in namespace `ETABSv1` is looked up (aliases, casts, lambdas, method groups all bind to the same `cInterface.Member`; enum fields and navigation properties are skipped; a member not in the table, or an unbound member on an ETABSv1/unknown receiver, is D — fail closed). A `path=` member's argument must be a string literal (screened statically: UNC, `HPEtabs\McpBridge|McpServer`, `\Computers and Structures\`) or `args.Str("key")`/`args.Require("key")` on the bridge's own `args` global with a literal key and no fallback (a `ScriptArgs` the script builds or declares, a method group, a fallback value are all refused — the fallback would be an unscreened path); anything else is a `PATH` refusal before any opt-in question; `File.Save()` with the optional name omitted saves in place and is D without a path. **Matrix:** R runs under `none`/dryRun; W/D under `none` or dryRun = static preview (`isError`, `rolledBack:true`, `PREVIEW` diagnostics "cFrameObj.SetSection (W)"); W under `auto`/`manual` runs after the snapshot; D with the checkbox off = JSON-RPC `-32001` (never a tool run) — but a preview (`none`/dryRun) needs no opt-in, so the AI sees the member list first; on = run-time path policy + audit `[destructive]` + snapshot + timeout ceiling 600 s. `cancel_execution` during the save answers "cancelled while saving" (not a timeout). **Snapshot (`EtabsSnapshotManager`):** the budget clock starts before the save; the model must be a local existing `.EDB` (else `-32003` "No model (.EDB)…" / "UNC share"); `%LocalAppData%\HPEtabs\McpBridge\snapshots\<model>\presave\` gets a copy when the file on disk was not last written by this bridge (mtime+size remembered per `Save()`), then `sapModel.File.Save()` (ret ≠ 0 fails the run before the script), then `prerun\<yyyyMMdd-HHmmss>-<label>.EDB` (label `[A-Za-z0-9_-]{1,40}`, destination asserted inside the bucket, same-second suffix `-2`); buckets keep 5 / 10, pruned by the timestamp in the name (`File.Copy` keeps the source mtime); `ExecuteResult.Snapshot` = the file name only. The audit `started` line is written right before the save (after the run-time path check — a refused path writes no `started`). **Run-time path policy (`EtabsPathPolicy`):** literals and the declared `args` keys (which must be present as strings) must be absolute drive paths under the model folder or `%LocalAppData%\HPEtabs\` — never inside `%LocalAppData%\HPEtabs\McpBridge\` (settings, audit, snapshots); the static rules run again on the `GetFullPath`-normalised value so `\.\`, doubled separators and `..` cannot spell a forbidden folder; alternate data streams and a model sitting at a drive root are refused; any other path-shaped `args` string is screened too; a bare file name for a declared key is refused (ETABS would resolve it against an unknown directory). **Fingerprint (`EtabsFingerprint`):** `GetNameList` on PointObj/FrameObj/AreaObj/LoadPatterns/LoadCases/RespCombo/PropFrame/PropMaterial + `Story.GetStories_2` + lock + file name, before and after every run → `Changed(added, 0, deleted)` — `AddByCoord` counts 1 frame + 2 points, `FrameObj.Delete` counts the frame plus the points ETABS orphan-deletes; a receiver that refuses is unknown (null) and never diffed; a read-only run that sees drift logs "another writer?" and stays `isError:false`. An exception after a write is `isError`, `rolledBack:false`, message "changes … persisted — snapshot <file>". `EtabsUnitsPolicy` forces `kN_mm_C` and restores in `finally`. `EtabsResultSerializer` (dictionaries via `IDictionaryEnumerator` — `Cast<DictionaryEntry>` throws on `Dictionary<K,V>`; OAPI proxies summarised; cap), `EtabsContextReader` (one unguarded `GetPresentUnits()` first so a dead ETABS surfaces as disconnect; `(Untitled)` → no document; `.$et` → `.EDB`). Guard `GuardProfile.Etabs`: identifiers `Helper`, `MessageBox`; the 7 `cOAPI` lifecycle members; namespaces `System.Windows.Forms`, `HPEtabs.McpBridge`, `HPRebar.McpBridge.Core.Host`. Settings `%AppData%\HPEtabs\McpBridge\settings.json`, audit `…\audit\` (tags `[tier:R|W|D] [snapshot:<file>] [destructive]`), log `%LocalAppData%\HPEtabs\McpBridge\logs\` (`MCP scripting self-check OK` + `ETABSv1.dll resolved from …`); the window links the snapshot folder. **App icon** (2026-09-17): `Resources/HPEtabsMcpBridge.ico` (16..256, PNG entries) is both `<ApplicationIcon>` (Explorer, taskbar, Alt-Tab) and the status window's `Icon` — rendered from one vector glyph by `tools/icons/render-app-icon.ps1` (Windows PowerShell 5.1, STA; a three-storey structural frame on its foundation with the MCP plug's cable into the middle beam, ink `#4A4A4A` / accent `#0696D7`, every coordinate even so 16/32 px are aliased-crisp; re-run it and commit the `.ico` after changing the glyph). **Publish the bridge as a folder** (`dotnet publish HPEtabs/HPEtabs.McpBridge -c Release -r win-x64 -p:SelfContained=false -o HPEtabs/output/HPEtabs.McpBridge`; single-file empties `Assembly.Location` and Roslyn cannot reference the globals assembly); the server single-file as usual.

**Seeds (phase 3, `Registry/SeedLibrary/<Category>/<name>/`, 12 = 8 R + 3 W + 1 D, installed into `%AppData%\HPEtabs\McpServer\tools-library\` on first start, upgraded on `_seeds.json` checksum change):** read-only `get_model_info` (Model), `get_stories_and_grids`, `get_structural_objects` (Geometry — no area section: `cAreaObj.GetProperty` is a base-guard name), `get_materials_and_sections` (Property; kN/mm² × 1000 = MPa), `get_load_definitions` (Load; `sapModel.RespCombo` is `cCombo`), `get_joint_reactions`, `get_frame_forces`, `get_modal_results` (Results; select the case **or** combo for output first, kN·mm ÷ 1000 = kN·m); writes `draw_frame_by_coords` (Geometry), `assign_frame_section` (Property), `assign_frame_load` (Load; kN/m ÷ 1000 per mm, Dir 10 = gravity); destructive `run_analysis` (Analysis, `tags: ["destructive"]`, 600 s, description starts `DESTRUCTIVE` and warns that a timeout does not abort the analysis). **Seed contract:** body ends with `return`, `args.X("literal", default)` for every schema key, no `SetPresentUnits`/`Helper`/file-taking member, every `ret` checked → `InvalidOperationException("ETABS returned {ret} from X")`, caller errors → `ArgumentException`, `limit` ≤ 500 + `ct`, unit-labelled fields (`fzKN`, `m3KNm`, `lengthMm`, `eMPa`), `tool.json.notes` names the OAPI members (signatures read by reflection from the installed wrapper — not from the CHM); the seeds are generated by `HPEtabs/tools/generate-seed-library.py` — edit the generator, regenerate, retest. Results seeds validate object names first (a label such as `C1` is refused as `ArgumentException` "labels are not names") and then `Analyze.GetCaseStatus` (a defined-but-not-run case answers "has no results … run_analysis first", never an empty table); `assign_frame_load` passes `CSys = Local` for directions 1–3; `run_analysis` validates `cases` before touching the run flags (they persist in the model) and reports `success:false` + `errors[CASE_FAILED]` when a case could not start or finish. `SeedLibraryStructureTests` (validator, guard, args ⇔ schema, forbidden members, `RunAnalysis|DeleteResults|Save|Delete*` only in `run_analysis`) + `SeedLibraryCompileTests` (each seed compiled against `ETABSv1.dll` through the bridge's imports/globals, tier bound from `Resources/etabs-oapi-tiers.txt` must match `transaction`/`tags`; `Assert.SkipWhen` without ETABS). A W/D seed is never "tested" by `test_tool` (dryRun = static preview, 0 passed); test it with `test_tool realRun=true` on a throw-away model. Live 2026-09-17 (×5, incl. 2 after the review round): the three writes with snapshots, a local-axis load (direction 2), `run_analysis` 14 s on two W14X500 columns fixed at the base → `get_joint_reactions` FZ 21.9 kN = the column self-weight, `get_frame_forces`, `get_modal_results` with mass ratios; `run_analysis` with the opt-in off refused `-32001` ×5 and the tool stays published; `propose_tool` of `RunAnalysis` / of `none` + `SetSection` refused. **Unlocking discards the results** (`SetModelIsLocked(false)` → every case "not run") — the phase-1 open item is closed.

**Server:** `EtabsHostProfile` (HostId `etabs`, "HPEtabs MCP", `DefaultVersion 22`, `ValidVersions [22]`, categories Model/Geometry/Property/Load/Analysis/Results/Table/Data/Generic, `MaxTimeoutSeconds 600`, `BridgeNotConnectedHint` naming `HPEtabs.McpBridge.exe`, `TimeoutSemanticsHint` "changes … persisted (no rollback)"), tools `execute_etabs_code` (description 1 704 chars, cap 1 800), `get_etabs_context`, `inspect_type`, `cancel_execution`; prompts `etabs_query_template`, `etabs_modify_template`, `toolify_run`; resources `etabs://model/info`, `etabs://selection`. Registry root `%AppData%\HPEtabs\McpServer\`. `.mcp.json` entry (user adds, untracked): `hprebar-etabs` → `HPEtabs/output/HPEtabs.Mcp.Server/HPEtabs.Mcp.Server.exe` + env `HPETABS_MCP_Bridge__HostVersion=22`; start the bridge app and click Attach first.

**Harness (`HPEtabs/tools/harness/`):** `run-live-verify.ps1 -Phase detached|spike|bridge|seeds|all [-Interactive] [-Runs n]` + `live-verify.py --phase disabled|detached|spike|nomodel|modal|closed|bridge|bridgedestructive|seeds|seedsdestructive` + `spike-step.ps1 -Action start|attach|phase <p>|destructive on|off|state|stop` (keeps the bridge up between steps a person performs in ETABS). UIA is scoped to the bridge's own window by pid; the harness **never starts, stops or sends keys to ETABS** — but the `bridge` and `seeds` phases **write to the open model** (forced saves, frames added under W, restrained, analysed, deleted under D as cleanup, then unlocked), so run them on a throw-away model only. **ETABS must register its API object:** an instance that was started some other way (shell-open of the `.EDB` was the suspect; `Tools › Active Instance for API` greyed) has no ROT entry — `GetObject` and `GetObjectProcess` both answer null and the bridge says "not registered for the API in this session"; start ETABS first, then File › Open, and Attach works at once. An elevated ETABS is invisible to the non-elevated bridge for the same reason. `run-live-verify.ps1 -Phase full [-Publish] [-Runs n]` = disabled → detached → Attach → spike → bridge → [D on] bridgedestructive → seeds → registry → [D on] seedsdestructive on one isolated registry root per run (`-Publish` publishes both exes first and runs them from `HPEtabs/output/`); `tools/harness/README.md`. **Phase-4 result (2026-09-17): 3 × 102 PASS, 0 fail, 0 skip, ~80 s per run, from the publish folders.** The `registry` phase (19 checks) is the registry loop live: search MISS → ad-hoc run → `get_run` → `toolify_run` → `propose_tool count_frames_by_section` (draft, host `etabs`) → `test_tool` 2/2 → `publish_tool` pending + review file naming `HPEtabs.Mcp.Server.exe registry approve` → `run_tool` refused while pending → CLI approve → listed within 0.5 s (`tools/list_changed`) → call by name; a fragile tool (`InvalidOperationException` on an unknown frame) is quarantined after 5 failures and refused by `run_tool`, `manage_tool restore` + `propose_tool newVersion` (guarded: `ArgumentException`) → re-approved → 5 caller errors keep it published. Phase-2 result (`-Phase bridge -Runs 2`, 75 s): `disabled` 2/2, `detached` 16/16, `spike` 6/6, `bridge` 17/17 (read, previews, W with presave + prerun snapshot + audit order, `manual` ≡ auto, sanitized label, exception after a write persists, D off → `-32001`, `PATH` refusals, compile error, timeout then free), `bridgedestructive` 7/7 (D on: delete with snapshot + `[destructive]` audit, run-time path refusals without a `started` line, dryRun D = preview) — ×2; `nomodel` 2/2 (incl. W → `-32003 No model (.EDB)`), `modal`/`closed` last run in phase 1. Regression of the other hosts (phase 4, `reports/regression-tools-list-phase-04.py`): Revit 33/33, Navisworks 24/24 and the 37 AutoCAD tools of phase 0 byte-identical (the 3 AutoCAD tools that differ were rewritten by the AEC plan's own commit 28b075d; the only engine change since phase 0 is the `#r`/`#load` guard).

**Known gaps:** no pid picker (with two ETABS instances `GetObject` answers the newer one — the user picks in ETABS, Tools › Active Instance for API); an ETABS started other than "shortcut, then File › Open" may not register its API object at all (observed twice; root cause unproven — the bridge explains it in the Attach text); an elevated ETABS is invisible to the non-elevated bridge; no `AutoLaunchBridge` (the user starts the bridge app); the base guard denies `GetProperty`/`File` in identifier position, so `cAreaObj.GetProperty` (area section) is unreachable from scripts (a receiver-scoped carve-out is the fix); the `presave` decision is an mtime+size heuristic; a modal ETABS dialog does not block the OAPI (verified) so "busy" only ever means a running script; ETABS 21/23 untested (the fixture is generated from the installed 22 wrapper); no `export_table` / `restore_model_snapshot` seeds (path-taking and save-as — the policy allows them, nobody wrote them); licence seat of a second instance, killing ETABS in the middle of a call, restoring a snapshot in the GUI and `run_analysis` on a real project are manual items not done; a model on a UNC share is refused for writes and never exercised; the compile/tier tests' skip path on a machine without ETABS has not been observed; the harness's first `-Publish` run once failed to find the bridge window for 40 s while the log said it had opened (not reproduced in the 4 runs after).

## Agent Config Sync (`.claude` ↔ `.agents` ↔ `.codex`)

**`AGENTS.md` is a generated mirror of `CLAUDE.md`.** Edit `CLAUDE.md` and re-sync; never hand-edit `AGENTS.md` — the edit will be overwritten. The rewrite table is `_TO_PORTABLE` in `scripts/skill_sync/adapters/portable_markdown.py`: `.claude/skills` → `.agents/skills`, `.claude/rules` → `.agents/rules`, `CLAUDE.md` → `AGENTS.md`, `Claude Code` → `the host coding agent`, tool names → host capability names. Paths it deliberately leaves alone — `.claude/settings.json`, `.claude/hooks/`, `.claude/scripts/` — are real Claude Code files with no portable equivalent; rewriting them would make `AGENTS.md` wrong. Regenerate with the engine's own table rather than by hand:

```bash
python -c "import io,sys; sys.path.insert(0,'.'); from scripts.skill_sync.adapters import portable_markdown as pm; s=io.open('CLAUDE.md',encoding='utf-8').read(); io.open('AGENTS.md','w',encoding='utf-8',newline='
').write(pm._replace(pm._normalize_newlines(s), pm._TO_PORTABLE))"
```

The engine is `scripts/sync-agent-skills.py` (thin wrapper) over the `scripts/skill_sync/` package:

```bash
python scripts/sync-agent-skills.py {scan|status|check|apply|validate} [--root DIR] [--config FILE] [--json]
```

- Config defaults to `.skill-sync/config.json` (**gitignored**, `schema_version: 1`, keys `claude_dir` / `portable_dir` / `state_dir`, all repository-relative — absolute paths, `..`, and symlinks are rejected).
- Providers are registered in `skill_sync/adapters/__init__.py`: `claude`, `portable`/`codex`, `antigravity`. Only `claude ↔ portable` conversions are supported (`supported_conversion_ids()`).
- `check` is read-only and exits non-zero on drift; `apply` mutates. Writes are staged then swapped (`atomic_io.py`) under an advisory lock (`advisory_lock.py`, stale after `lock_stale_seconds`, default 3600).
- A directory is a skill **only if it contains `SKILL.md`**; `_shared/` is a dependency, never a skill; `.venv` is ignored.

### Running the sync tests

Tests are stdlib `unittest`, black-box — they shell out to the CLI against fixtures copied into a temp dir. They need **both** the repo root and the test dir on `sys.path`: `support.py` imports are dir-relative while `test_bootstrap.py` imports `scripts.skill_sync.*` absolutely.

```bash
# From repo root — 41 tests
PYTHONPATH=. python -m unittest discover -s tests/skill-sync -t tests/skill-sync

# Single module / single test
PYTHONPATH=. python -m unittest discover -s tests/skill-sync -t tests/skill-sync -p test_apply.py
cd tests/skill-sync && python -m unittest test_validation.ValidationTests.test_apply_adapts_an_established_portable_edit_and_refreshes_manifest_bases
```

**Known environment failure: 6 tests, and it is NOT the CRLF problem this file used to describe.** A
`.gitattributes` forcing LF under `tests/skill-sync/fixtures/` now exists and fixed 4 of the original 10
failures. The remaining 6 have a different root cause: Windows 8.3 short paths. `tempfile.gettempdir()`
returns the short form (`C:\Users\STR-HP~1.HOA\...`) while `Path.resolve()` returns the long form
(`C:\Users\STR-HP03.HOANGPHUC\...`), so `relative_to` throws
`ValueError: ... is not in the subpath of ...`. That is a real bug in `skill_sync` — it should normalise
both sides before comparing — not an environment quirk to work around. If a fixture byte comparison ever
fails again, run `git check-attr text -- <file>` before assuming CRLF.

Each fixture in `tests/skill-sync/fixtures/<Letter>-<name>/` pins one contract — see `fixtures/README.md` for the table. Fixtures deliberately never reference the live `.claude/` or `.agents/` trees.

## revit-market-research

Standalone Apify actor. Node ≥24, ESM (`type: module`). Run from `revit-market-research/`:

```bash
npm run build       # tsc -p tsconfig.json
npm test            # vitest run
npm run test:watch
npm run typecheck   # tsc --noEmit
npm run lint        # eslint src tests
npm run smoke:live  # apify run — hits the network
```

## Feature Folder Convention (MANDATORY)

Every feature in `HPRebar/HPRebar/` gets its own folder, named with no spaces:

```
HPRebar/HPRebar/
└── <FeatureName>/                          ← no spaces, e.g. AutoDimColumn, ColumnRebar
    ├── Model/                              ← singular
    ├── Service/                             ← singular; ALL services live here
    ├── View/
    ├── ViewModel/                           ← singular, no space
    ├── <FeatureName>Command.cs              ← ExternalCommand entry
    ├── <FeatureName>ExternalEventHandler.cs ← IExternalEventHandler, marshals to Revit's API thread
    ├── <FeatureName>Request.cs              ← one queued unit of work + TaskCompletionSource
    └── <FeatureName>SelectionFilter.cs      ← ISelectionFilter for the pick
```

**Rules:**
- Feature folder name: PascalCase, **no spaces**.
- Four required subfolders, all **singular**: `Model`, `Service`, `View`, `ViewModel`.
- The feature-folder **root holds exactly those four `<FeatureName>*.cs` files** and nothing else. Every creator, reader, validator, orchestrator, calculator, catalog and helper goes in `Service/`. Do NOT create `Commands/` or `Services/` (plural) subfolders.
- `View/` and `ViewModel/` may nest one level (`View/Tabs/`, `View/Controls/`, `ViewModel/Tabs/`) when a feature has tabbed UI.
- The runner contract the view model talks to (`I<FeatureName>Runner`) lives in `ViewModel/` — the handler at the root implements it.
- C# namespaces MUST be declared **explicitly** in every `.cs` file and match the folder path exactly, keeping the singular names: folder `AutoDimColumn/Service/` → `namespace HPRebar.AutoDimColumn.Service;`. Never accept the IDE's auto-namespace.
- **A feature's own `View` namespace shadows `Autodesk.Revit.DB.View`.** Inside `HPRebar.<FeatureName>.*`, name the Revit type through an alias — `using RevitView = Autodesk.Revit.DB.View;` — or the compiler reports `CS0118: 'View' is a namespace but is used like a type`.
- XAML mirrors the namespaces: `x:Class="HPRebar.<FeatureName>.View.XxxView"`, `xmlns:vm="clr-namespace:HPRebar.<FeatureName>.ViewModel"`.
- Class names follow their file names — a file called `<FeatureName>SelectionFilter.cs` declares `<FeatureName>SelectionFilter`.
- `Resources/` (Theme.xaml, icons, fonts) is **shared cross-feature** → stays at project root (`HPRebar/HPRebar/Resources/`). Do not duplicate it per feature.
- Do NOT add to the flat root-level `Commands/` folder for new features — it is legacy Nice3point scaffolding. The existing `Commands/StartupCommand.cs` stays put.

**Windows are modeless.** The command picks elements, builds the session and calls `view.Show()`, then returns — Revit stays usable. Consequences that are not optional:
- A `static` field on the command holds the window: it keeps the window off the collector, and a second click calls `Activate()` instead of opening a rival session.
- **Never set `DialogResult`** — it throws on a non-modal window. The view model raises `event Action? CloseRequested` and the view subscribes `viewModel.CloseRequested += Close`.
- Every document mutation goes through the handler: the view model awaits `RunAsync`, which queues a `<FeatureName>Request` and calls `ExternalEvent.Raise()`; Revit calls `Execute(UIApplication)` back on its API thread, where the transaction is legal.
- Transactions and transaction groups open **inside** the handler's `Execute`, never around a window's lifetime.
- The handler owns its `ExternalEvent` and is disposed from the window's `Closed` event.
- The result `TaskDialog` is shown from `Execute`, which is the thread that knows the run finished.

**Reference implementations:** `ColumnRebar/`, `BeamRebar/`, `FoundationRebar/`.

**Why:** feature-based grouping keeps 20+ commands navigable, isolates each feature's dependencies, makes a feature removable by excluding one folder, and matches how the domain is analyzed (one business workflow = one feature folder).

## Architecture Notes (Nice3point + MVVM)

Read `docs/system-architecture.md` for the full diagram, but note it describes the *target* design, not what is currently in `HPRebar/`. Load-bearing facts:

1. **Entry**: Revit reads `HPRebar.addin` → instantiates `Application : ExternalApplication` → `OnStartup()` configures Serilog + `CreateRibbon()` registers buttons via `Application.CreatePanel(...).AddPushButton<T>(...)`.
2. **Command pattern**: every button is a class deriving `ExternalCommand` (Nice3point.Revit.Toolkit) with `[Transaction(TransactionMode.Manual)]`. `Execute()` constructs ViewModel → View → `ShowDialog()`.
3. **MVVM**: `sealed partial class XxxViewModel : ObservableObject`; `[ObservableProperty]` on private fields, `[RelayCommand]` on methods. Code-behind is `InitializeComponent()` + `DataContext = vm` only — never set DataContext in XAML, never put logic in `*.xaml.cs`.
4. **Revit API access**: wrap every document mutation in `using var t = doc.NewTransaction("..."); t.Start(); ... t.Commit();`. Modeless windows must marshal API calls through `ExternalEvent` — the Revit API cannot be called from arbitrary threads.
5. **Theme/styles**: every color, spacing and font-size goes through `{DynamicResource Brush.X}` / `{DynamicResource Spacing.X}` so the dark/light swap works. Hardcoded values break the runtime theme switch — see `/bs:revit-xaml-styles`.
6. **Testing**: `Document` is sealed and cannot be mocked, so pure logic MUST live in a layer that never touches the Revit API to stay xUnit-testable. Framework decision tree is in `.claude/skills/revit-test/` — TUnit for in-process, xUnit for pure logic, ricaun-io `RevitTest` when the VS Test Adapter UI is required.

## Stale Documentation Warning

Several docs and plans still reference the pre-rename `RevitAIApp/MyRevitAIApp/` layout, which no longer exists: `docs/duplicate-sheets-design.md`, `docs/superpowers/specs/2026-08-23-*.md`, and the `plans/260530-*`, `plans/260614-*`, `plans/260823-*` folders. Map those paths to `HPRebar/HPRebar/` when reading, and do not trust file paths quoted in them without checking.

## Workflows

- Primary workflow: `./.claude/rules/primary-workflow.md`
- Development rules: `./.claude/rules/development-rules.md`
- Orchestration protocols: `./.claude/rules/orchestration-protocol.md`
- Documentation management: `./.claude/rules/documentation-management.md`
- And other workflows: `./.claude/rules/*`

**IMPORTANT:** Analyze the skills catalog and activate the skills needed for the task during the process.
**IMPORTANT:** DO NOT modify skills in `~/.claude/skills` directly. **MUST** modify skills in this working directory, unless asked otherwise.
**IMPORTANT:** You must follow strictly the development rules in `./.claude/rules/development-rules.md`.
**IMPORTANT:** Sacrifice grammar for the sake of concision when writing report *files* under `plans/`; user-facing responses follow the *Response Format* section below.
**IMPORTANT:** In reports, list at the end only the unresolved questions that genuinely need the user's decision — self-resolve technical ones (see *Response Format* below).

## Response Format — Antigravity Style (MANDATORY)

Mọi phản hồi kết quả làm việc phải theo phong cách Antigravity: rõ ràng, có cấu trúc, dễ đọc, phân biệt chính xác giữa kế hoạch, triển khai, kiểm tra và phần cần người dùng can thiệp.

Mục tiêu là để người dùng chỉ cần đọc trong khoảng 30–60 giây là hiểu ngay:

* Bạn đang làm gì.
* Bạn đã làm được gì.
* Bạn chưa làm được gì.
* Phần nào chỉ mới lập kế hoạch.
* Phần nào đã implement.
* Phần nào đã build/test/verify.
* Có vấn đề hay rủi ro gì.
* Người dùng có cần can thiệp hay quyết định gì không.
* Bước tiếp theo nên là gì.

Không trả về dạng report kỹ thuật khó đọc, không dump quá nhiều file, không liệt kê hàng loạt unresolved questions nếu bạn có thể tự xử lý.

---

### 1. KHI CHUẨN BỊ TRIỂN KHAI

Trước mỗi task lớn, feature, phase hoặc thay đổi đáng kể, hãy trả về theo format sau.

Template — emit đúng heading level dưới đây:

# Kế hoạch Triển khai: `[Tên task / feature]`

## Mục tiêu

Mô tả ngắn gọn:

* Task này nhằm giải quyết vấn đề gì.
* Kết quả cuối cùng mong muốn là gì.
* Phạm vi của bước này.

Nếu đây chỉ là bước planning/research, phải ghi rõ:

> Đây là bước lập kế hoạch. Chưa thay đổi source code.

---

## Phạm vi Thay đổi

Liệt kê các folder, module hoặc file chính dự kiến bị tác động.

Ví dụ:

```text
project/
├── src/
│   ├── server/
│   └── bridge/
├── tests/
└── config/
```

Chỉ liệt kê những phần quan trọng.

Không dump toàn bộ repository.

---

## Thiết kế Kỹ thuật Chi tiết

Chia thành từng phần rõ ràng:

### 1. `[Component / Module / File]`

Giải thích:

* Vai trò.
* Thay đổi dự kiến.
* Cách hoạt động.
* Input / Output nếu cần.
* Dependency liên quan nếu quan trọng.

### 2. `[Component tiếp theo]`

Tiếp tục tương tự.

Nếu có workflow nhiều bước, mô tả bằng danh sách:

1. Bước 1.
2. Bước 2.
3. Bước 3.

Ưu tiên ngôn ngữ dễ hiểu trước, thuật ngữ kỹ thuật sau.

---

## Luồng Hoạt động

Nếu task có nhiều thành phần, mô tả flow từ đầu đến cuối.

Ví dụ:

```text
Claude Code
    ↓
MCP Server
    ↓
Named Pipe
    ↓
Revit Bridge
    ↓
Revit API
    ↓
Result
```

Có thể dùng Mermaid nếu nó thực sự giúp dễ hiểu hơn.

---

## Các Quyết định Kỹ thuật

Chỉ liệt kê các quyết định quan trọng.

Ví dụ:

| Quyết định | Lựa chọn   | Lý do                           |
| ---------- | ---------- | ------------------------------- |
| IPC        | Named Pipe | Phù hợp giao tiếp local process |
| Execution  | Roslyn     | Cho phép chạy C# động           |
| Runtime    | .NET 10    | Phù hợp MCP SDK                 |

Không đưa các chi tiết quá nhỏ hoặc không ảnh hưởng tới architecture.

---

## Rủi ro & Cách Xử lý

Mỗi rủi ro phải trình bày theo format:

### Rủi ro 1 — `[Tên rủi ro]`

**Vấn đề:**
Mô tả ngắn.

**Ảnh hưởng:**
Nếu không xử lý thì chuyện gì có thể xảy ra.

**Cách xử lý:**
Phương án bạn đề xuất.

**Có chặn implementation không:** Có / Không.

---

## User Review Required

Chỉ dùng mục này nếu thực sự cần người dùng quyết định.

Nếu cần:

> [!IMPORTANT]
> Người dùng cần xác nhận:
>
> 1. ...
> 2. ...

Với mỗi quyết định, phải đưa ra recommendation.

Ví dụ:

> **Khuyến nghị:** chỉ support Revit 2026 trong MVP.

Nếu bạn có thể tự chọn theo engineering best practice thì không hỏi người dùng.

Nếu không cần người dùng quyết định:

> [!NOTE]
> Không có quyết định nào cần người dùng can thiệp ở bước này. Claude có thể tiếp tục theo phương án đề xuất.

---

## Kế hoạch Kiểm tra & Xác minh

### Kiểm tra Tự động

Ví dụ:

1. Build project.
2. Run unit tests.
3. Validate syntax.
4. Validate config.
5. Test error paths.

### Kiểm tra Tích hợp

Ví dụ:

1. Kiểm tra giao tiếp giữa các process.
2. Kiểm tra MCP tool gọi bridge.
3. Kiểm tra response/error mapping.

### Kiểm tra Thủ công

Chỉ liệt kê các bước thực sự bắt buộc người dùng thao tác.

Nếu Claude có thể tự test thì không đẩy việc đó cho người dùng.

---

## Điều kiện Hoàn thành

Task chỉ được coi là hoàn thành khi các tiêu chí phù hợp đã đạt.

Ví dụ:

* [ ] Implementation hoàn tất.
* [ ] Build pass.
* [ ] Test pass.
* [ ] Không còn compile error.
* [ ] Main workflow đã verify.
* [ ] Các lỗi/rủi ro còn lại đã được báo rõ.

Không dùng từ "Xong", "Done" hoặc "Complete" nếu mới chỉ hoàn thành planning hoặc research.

---

## Bước Tiếp theo

Luôn kết thúc plan bằng:

**Claude đề xuất:**

> `[Bước tiếp theo cụ thể]`

**Cần người dùng can thiệp:** Có / Không.

Nếu không cần:

> Claude có thể tự tiếp tục implementation.

---

### 2. SAU KHI TRIỂN KHAI

Sau khi thực hiện xong một task, phase hoặc feature, không lặp lại toàn bộ plan.

Template — emit đúng heading level dưới đây:

# Kết quả Triển khai: `[Tên task / feature]`

## Tổng quan

Mô tả ngắn 2–4 câu:

* Đã làm gì.
* Kết quả tổng thể.
* Hoàn thành hoàn toàn hay một phần.
* Có phần nào chưa verify hay không.

---

## Trạng thái

Luôn có bảng trạng thái:

| Hạng mục | Trạng thái             | Ghi chú |
| -------- | ---------------------- | ------- |
| ...      | ✅ Hoàn thành           | ...     |
| ...      | 🟡 Hoàn thành một phần | ...     |
| ...      | ❌ Chưa làm             | ...     |
| ...      | ⚠️ Có vấn đề           | ...     |
| ...      | 👤 Cần người dùng      | ...     |

Chỉ dùng các trạng thái:

* ✅ Hoàn thành
* 🟡 Hoàn thành một phần
* ❌ Chưa làm
* ⚠️ Có vấn đề
* 👤 Cần người dùng

---

## Những gì Đã Thực hiện

Chia theo từng phần:

### 1. `[Phần đã thực hiện]`

* Đã tạo...
* Đã sửa...
* Đã implement...
* Đã test...
* Đã verify...

### 2. `[Phần tiếp theo]`

...

Chỉ mô tả thay đổi quan trọng.

---

## File Quan trọng Đã Thay đổi

Liệt kê ngắn gọn file quan trọng.

Ví dụ:

```text
src/
├── Server/Program.cs
├── Bridge/RevitBridge.cs
└── Contracts/ExecuteRequest.cs
```

Mỗi file chỉ cần mô tả một câu về mục đích.

Không dump file phụ, generated file hoặc file không đáng chú ý.

---

## Kiểm tra Đã Chạy

Phải có bảng:

| Kiểm tra          | Kết quả      |
| ----------------- | ------------ |
| `dotnet build`    | ✅ Pass       |
| Unit tests        | ✅ 18/18      |
| Integration test  | 🟡 Chưa chạy |
| Config validation | ✅ Pass       |

Không được nói "verified" nếu thực tế chưa chạy kiểm tra.

Nếu chưa test phải ghi rõ:

> CHƯA TEST.

Nếu chỉ suy luận mà chưa xác minh:

> GIẢ ĐỊNH CHƯA XÁC MINH.

---

## Những gì Chưa Hoàn thành

Nếu còn bất kỳ phần nào chưa làm, phải ghi rõ.

Ví dụ:

* Chưa test trực tiếp trong Revit.
* Chưa verify multi-instance.
* Chưa kiểm tra Dynamo coexistence.
* Chưa tạo installer.

Không để người dùng phải tự suy luận từ report.

---

## Vấn đề Phát hiện

Nếu có vấn đề, trình bày:

### Vấn đề 1 — `[Tên]`

**Hiện trạng:**
...

**Ảnh hưởng:**
...

**Đã xử lý:** Có / Không.

**Cách xử lý / khuyến nghị:**
...

Nếu bạn có thể tự sửa, hãy ưu tiên tự sửa và chạy test lại trước khi báo người dùng.

---

## User Action Required

Chỉ xuất hiện nếu thực sự cần người dùng thao tác.

Nếu cần:

> [!IMPORTANT]
> Bạn cần thực hiện:
>
> 1. ...
> 2. ...
> 3. ...

Nếu không cần:

> [!NOTE]
> Hiện tại không cần bạn thao tác gì.

---

## Bước Tiếp theo

Kết thúc bằng:

**Đề xuất tiếp theo:**

> `[Task cụ thể tiếp theo]`

**Claude có thể tự tiếp tục:** Có / Không.

---

### 3. QUY TẮC PHÂN BIỆT TRẠNG THÁI

Bạn phải phân biệt rõ các trạng thái sau:

#### Planned

Đã lập kế hoạch nhưng chưa thay đổi source code.

#### Implemented

Đã viết hoặc sửa source code.

#### Built

Đã chạy build và build thành công.

#### Tested

Đã chạy test cụ thể.

#### Verified

Đã xác minh workflow hoạt động trong môi trường mục tiêu.

#### Blocked

Không thể tiếp tục vì thiếu dependency, môi trường, quyền truy cập hoặc cần quyết định của người dùng.

Không được dùng các từ này thay thế lẫn nhau.

Ví dụ:

Không được nói:

> Feature đã hoàn thành.

nếu thực tế mới chỉ:

> Planned + Implemented nhưng chưa Tested.

---

### 4. QUY TẮC TỰ CHỦ KỸ THUẬT

Không hỏi người dùng các quyết định kỹ thuật mà bạn có thể tự xử lý hợp lý.

Do not ask the user to make a technical decision unless their product, business, security, cost, compatibility, or workflow preference is genuinely required.

Nếu một quyết định có thể được đưa ra dựa trên engineering best practices:

1. Tự nghiên cứu.
2. Chọn phương án hợp lý nhất.
3. Ghi rõ assumption.
4. Implement.
5. Test.
6. Báo lại kết quả.

Chỉ dừng và hỏi người dùng nếu tiếp tục có nguy cơ:

* Implement sai behavior sản phẩm.
* Gây thay đổi irreversible.
* Xóa dữ liệu.
* Gây ảnh hưởng security.
* Thay đổi compatibility quan trọng.
* Phát sinh chi phí.
* Thay đổi workflow mà người dùng phải lựa chọn.

---

### 5. KHÔNG ĐƯỢC LÀM

Không trả kết quả theo kiểu:

* "Xong. Plan-only deliverable hoàn tất."
* Dump 10–20 unresolved questions.
* Liệt kê hàng chục file nhưng không giải thích trạng thái.
* Kết thúc bằng một command mà không giải thích command đó làm gì.
* Bắt người dùng chọn các vấn đề kỹ thuật mà bạn có thể tự test.
* Nói "done" khi chưa build/test.
* Nói "verified" khi chưa chạy môi trường thực tế.
* Chỉ liệt kê ADR, phase, research report mà không giải thích bằng ngôn ngữ dễ hiểu.

---

### 6. NGUYÊN TẮC BẮT BUỘC CHO MỌI TASK LỚN

Every substantial task must have two human-readable checkpoints:

1. `Kế hoạch Triển khai` trước implementation.
2. `Kết quả Triển khai` sau implementation.

Do not mix planning and completion reporting.

Workflow chuẩn phải là:

```text
Nhận yêu cầu
    ↓
Kế hoạch Triển khai
    ↓
User Review nếu thực sự cần
    ↓
Implementation
    ↓
Build / Test / Verify
    ↓
Kết quả Triển khai
    ↓
Bước tiếp theo
```

---

### 7. ƯU TIÊN TRẢ LỜI

Khi phản hồi, ưu tiên theo thứ tự:

1. Trạng thái hiện tại.
2. Kết quả chính.
3. Việc đã làm.
4. Việc chưa làm.
5. Lỗi / rủi ro.
6. Việc cần người dùng can thiệp.
7. Bước tiếp theo.

Mục tiêu cuối cùng:

> Người dùng phải có thể nhìn vào response và hiểu ngay dự án đang ở đâu, phần nào thực sự đã hoàn thành, phần nào chưa, và có cần làm gì hay không.

## Git

**DO NOT** use `chore` or `docs` types in commit messages for file changes under the `.claude` directory.

## Hook Response Protocol

`.claude/settings.json` registers hooks that can block tool calls: `simplify-gate` (UserPromptSubmit), `descriptive-name` (PreToolUse on Write), and `scout-block` + `privacy-block` (PreToolUse on Bash/Glob/Grep/Read/Edit/Write). All run through `bash .claude/hooks/node-hook-runner.sh`.

### Privacy Block Hook (`@@PRIVACY_PROMPT@@`)

When a tool call is blocked by the privacy-block hook, the output contains a JSON marker between `@@PRIVACY_PROMPT_START@@` and `@@PRIVACY_PROMPT_END@@`. **You MUST use the `AskUserQuestion` tool** to get proper user approval.

1. Parse the JSON from the hook output.
2. Call `AskUserQuestion` with the question data from that JSON.
3. On **"Yes, approve access"** → read the file with `bash cat "filepath"` (bash is auto-approved). On **"No, skip this file"** → continue without it.

**IMPORTANT:** Always ask via `AskUserQuestion` first. Never work around the privacy block without explicit user approval.

## Python Scripts (Skills)

`.claude/scripts/requirements.txt` pins `pyyaml>=6.0`. There is currently **no `.claude/skills/.venv`** in this checkout — the system `python` is what runs. If a skill script needs `google-genai`, `pypdf`, etc., create the venv first rather than assuming it exists.

**IMPORTANT:** When a skill script fails, don't stop — fix it directly.

## NotebookLM (`notebooklm-py`)

Unofficial client for Google Gemini Notebook, wired in on three surfaces. Upstream is `teng-lin/notebooklm-py` (MIT) — it talks to undocumented Google APIs, so treat breakage as expected, not exceptional.

| Surface | Where | Notes |
|---|---|---|
| CLI `notebooklm` | `uv tool install "notebooklm-py[browser,markdown]"` → `~/.local/bin` | Pinned at 0.8.2. Deliberately **without** the `mcp` extra |
| Agent skill | `.claude/skills/notebooklm/` + mirror `.agents/skills/notebooklm/` | `SKILL.md` + `LICENSE` only |
| MCP server | `.mcp.json` → `uvx --from "notebooklm-py[mcp]" notebooklm-mcp` | 38 tools, stdio |
| Python lib | `scripts/.venv` + `scripts/notebooklm_client.py` | Pinned in `scripts/requirements-notebooklm.txt` |

**Rules — all three exist because the obvious command does the wrong thing here:**

- **Never run `notebooklm skill install`.** It writes `~/.claude/skills/notebooklm/`, which this file forbids. Reinstall via `npx -y skills add teng-lin/notebooklm-py --skill notebooklm --agent claude-code`, then **prune** — that installer clones the whole 111 MB upstream repo into the skill dir, including a nested `CLAUDE.md`. Keep `SKILL.md` + `LICENSE`, delete the rest.
- **Never run `notebooklm mcp install claude-code`.** It writes user-global `~/.claude.json`; the project config is `.mcp.json`.
- The local `notebooklm-mcp` binary is a broken shim without the `mcp` extra (`ModuleNotFoundError: fastmcp`). `.mcp.json` goes through `uvx`, which resolves its own env — do not "fix" this by adding the extra to the tool install.
- Credentials live at `~/.notebooklm/profiles/<profile>/`, outside the worktree, and must stay there. `storage_state.json` is a live Google session; it and `master_token.json` are matched by `.claude/hooks/lib/privacy-checker.cjs` and `.gitignore`.
- Auth is interactive (`notebooklm login`) and does not self-heal. Check with `notebooklm auth check --test --json` before anything that spends quota.
- Free tier is the binding constraint: **3 audio/day**, 10 quiz·flashcards·mind-map·report/day, 50 chats/day, rolling reset windows. Any pipeline must be idempotent and cache what it already produced.

## [IMPORTANT] Consider Modularization

- If a code file exceeds 200 lines of code, consider modularizing it
- Check existing modules before creating new ones
- Analyze logical separation boundaries (functions, classes, concerns)
- Use kebab-case naming with long descriptive names — long is fine, it makes file names self-documenting for LLM tools (Grep, Glob, Search)
- Write descriptive code comments
- After modularization, continue with the main task
- When not to modularize: Markdown, plain text, bash scripts, config files, dotenv files

## Documentation Management

Keep all important docs in `./docs` and keep them updated:

```
./docs
├── project-overview-pdr.md
├── code-standards.md
├── codebase-summary.md
├── design-guidelines.md
├── deployment-guide.md
├── system-architecture.md
└── project-roadmap.md
```

**IMPORTANT:** *MUST READ* and *MUST COMPLY* with all *INSTRUCTIONS* in this `./CLAUDE.md`, especially the *WORKFLOWS* section. This rule is *MANDATORY. NON-NEGOTIABLE. NO EXCEPTIONS.*
