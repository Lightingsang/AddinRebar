# AGENTS.md

This file provides guidance to the host coding agent (claude.ai/code) when working with code in this repository.

## Role & Responsibilities

Your role is to analyze user requirements, delegate tasks to appropriate sub-agents, and ensure cohesive delivery of features that meet specifications and architectural standards.

## Repository Layout

This repo bundles **five unrelated deliverables** plus one shared library folder; treat them as separate concerns — do not cross-wire them. The only permitted dependency direction is MCP folder → `McpShared/`; `HPRebar/` and `HPAutoCad/` never reference each other:

| Path | What it is | Stack |
|---|---|---|
| `HPRebar/` | The Revit Add-In (the "real" product). Multi-version R23–R27. Also hosts the **Revit MCP** (server exe + bridge add-in, see "HPRebar MCP Bridge" below). | C# / Nice3point.Revit.Sdk / WPF / Serilog / ModelContextProtocol |
| `McpShared/` | **Host-neutral MCP engine** shared by every HP MCP: `HPRebar.Mcp.Contracts` (wire DTOs; `netstandard2.0;net48` — the net48 asset exists because Roamer.exe reflects over every plugin type before any plugin code runs, and the netstandard asset's System.Text.Json 10.0.0.0 reference cannot bind to the net462 package asset 10.0.0.12 on .NET Framework), `HPRebar.McpBridge.Core` (pipe listener, Roslyn guard/compiler, settings, bridge host + status view model; `net8.0;net48`; `MainThreadQueue(expireWithoutTicks)` opt-in for hosts whose idle event stops under a native modal), `HPRebar.Mcp.Server.Core` (server bootstrap, pipe client, execute/context services, tool registry engine, registry meta tools, CLI; `IHostProfile.MaxTimeoutSeconds` default 120 drives every timeout clamp) and their tests (`HPRebar.Mcp.Server.Core.Tests` net10, `HPRebar.McpBridge.Core.Net48Tests` net48). Never references `Autodesk.*`; the host arrives through `IHostProfile` / `IBridgeExecutor`. Assembly names keep the historical `HPRebar.*` prefix. Own `McpShared.slnx` + `global.json`. Navisworks constants/profiles (`PipeNaming.NavisHost`, `GuardProfile.Navis`, `HostScriptContracts.NavisImports`, `ContextResult.Navis`) landed 2026-09-15 as phase 0 of `plans/260915-0824-navisworks-mcp-2026/`; the `HPNavis/` folder itself does not exist yet. | C# / net8 · net48 · net10 / ModelContextProtocol / Roslyn |
| `HPAutoCad/` | The **AutoCAD MCP** (phases 1–5 done: loader, ALC, bundle, bridge runtime, server exe, registry per host, 12 embedded seed tools, live verification harness, all verified live in AutoCAD 2026). Own `HPAutoCad.slnx` + `global.json`; references `../McpShared/` only. | C# / net8 · net10 / AutoCAD.NET 25.1.0 |
| `HPNavis/` | The **Navisworks MCP** (in progress — phases 1–3 of 5 done: `HPNavis.McpBridge` plugin verified live in Navisworks Manage 2026 by the unattended harness `tools/harness/run-bridge-unattended.ps1` (43 checks per run ×2 + no-document run; Windows PowerShell 5.1, Navisworks closed) and `HPNavis.McpBridge.Tests` (62 net48 xUnit cases); `HPNavis.Mcp.Server` net10 stdio exe (`NavisHostProfile`, 600 s ceiling, `execute_navis_code`/`get_navis_context`, 12 tools, 12 tests) verified end-to-end against the live bridge by `run-server-smoke.ps1` 8/8 — the first net10 pipe client meeting a net48 `PipeSecurity` listener; `.mcp.json` entry `hprebar-navis`. Seeds and the registry loop follow). **Navisworks runs .NET Framework 4.8**, so the plugin is `net48` and consumes the `net48` assets of `McpShared/`. Own `HPNavis.slnx` + `global.json` + `Directory.Build.props` (API referenced from the installed product — no NuGet); references `../McpShared/` only. Plan: `plans/260915-0824-navisworks-mcp-2026/`. | C# / net48 · net10 / Navisworks API 23.0 |
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
(cd ../McpShared && dotnet test HPRebar.Mcp.Server.Core.Tests)   # 128 xUnit tests: pipe round trips with a fake executor, guard/compiler/args/analyzer, host-neutrality, registry per profile, ContextService.Shape all hosts, stability window, Navis profile + per-profile timeout ceiling
(cd ../McpShared && dotnet test HPRebar.McpBridge.Core.Net48Tests)  # 60 xUnit tests of the engine's net48 asset (for .NET Framework hosts such as Navisworks 2026): guard/queue linked, Roslyn on 4.8, PipeSecurity pipe ACL. Do NOT pass --nologo to dotnet test — it is forwarded to the MTP runner and rejected
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
- `Application.cs` and `Commands/StartupCommand.cs` use **block-scoped namespaces**, which contradicts the file-scoped rule in `.agents/rules/development-rules.md`. New files follow the rule; do not churn those two template files just to reformat them.
- `TestProjectModule` (`dotnet run -- test`) runs the whole solution per `Release.R*`, so it picks up `HPRebar.Core.Tests` and `HPRebar.Mcp.Server.Tests`; the TUnit project stays excluded, and so are the engine tests in `McpShared/` (run them separately).
- `ResolveConfigurationsModule` reads `HPRebar.slnx` through Sourcy (`Solutions.HPRebar`), not by searching the git root for "any .slnx" — `McpShared/` and `HPAutoCad/` carry their own solutions.

**The rebar add-in has not been verified at runtime.** Every Revit version is build-only for `HPRebar/`; the 16 TUnit tests skip because `HPRebar.Tests/Fixtures/column-stack-2-storey.rvt` does not exist. Do not describe any version as "supported". The **MCP bridge is different**: it has been run end-to-end in Revit 2026 on the dev machine (2026-09-12) — see the section below.

**A running Revit locks the deployed DLL.** Add `-p:DeployAddin=false` to any build meant only to check compilation.

Add-in identity lives in `HPRebar/HPRebar/HPRebar.addin` — `AddInId` GUID `AB6B2397-2618-4A8F-A86F-B0EBB5E58D2B`, `FullClassName` `HPRebar.Application`. Renaming the assembly or root namespace requires updating this manifest and the `/HPRebar;component/...` icon pack URIs in `Application.cs`.

## HPRebar MCP Bridge (Dynamic Revit MCP Server)

Turns Revit into a runtime for an AI agent, with a **tool registry** that remembers reviewed scripts as MCP tools. Design of record: `plans/260912-1521-dynamic-revit-mcp-server-2026/` (`architecture.md`, `adr/adr-01..06`, `research/ai-bim-self-extending-tool-registry-design.md`).

Tool surface (34 on the dev machine): 4 core — `execute_revit_code` (C# script via Roslyn, `Destructive`), `get_revit_context`, `inspect_type` (both `ReadOnly`), `cancel_execution`; 8 registry — `search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`; plus every **published library tool** (21 seeds ported from `mcp-servers-for-revit` + whatever the AI proposed and a human approved). Resources `revit://document/info`, `revit://selection`, `registry://tools[/{name}]`; prompts `revit_query_template`, `revit_modify_template`, `toolify_run`. **Never add a native command class to the bridge for a tool** (ADR-05): a tool is `tool.json + code.cs + examples.json` in the library and runs through the same `revit.execute` path as ad-hoc code.

```
the host coding agent ──stdio──▶ HPRebar.Mcp.Server (net10) ──named pipe hprebar-mcp-r2026, JSON-RPC 2.0 NDJSON──▶ HPRebar.McpBridge (inside Revit)
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

**Server exe + seeds + registry:** `HPAutoCad.Mcp.Server` (net10, stdio). Profile: `AutocadHostProfile` with registry engine per host (categories, reserved names, host stamp, CLI exe name). **24 tools:** 4 core (`execute_autocad_code`/`get_autocad_context`/`inspect_type`/`cancel_execution`), 8 registry (search/get/run/get_run/propose/test/publish/manage, descriptions host-neutral), 12 embedded seeds (6 read-only: `list_layers`, `list_block_definitions`, `get_entities`, `list_layouts`, `get_drawing_info`, `get_selected_entities`; 6 auto-transaction: `draw_polyline`, `draw_circle`, `add_text`, `create_layer`, `insert_block`, `add_linear_dimension`). Seeds installed into `%AppData%\HPAutoCad\McpServer\tools-library\<Category>\<name>\` on first start in 7 categories (Drawing, Layer, Block, Annotation, Layout, Data, Generic), upgraded on `_seeds.json` checksum change. **Seed contract:** script body ending `return`, points as `{x, y}` objects in mm, `tr` is the bridge's (guard denies `StartTransaction`/`Commit`/`Abort`/`LockDocument`/`ed.Get*`), globals mirror the bridge's, `args.X("key", default)` for every input. `SeedLibraryTests` (58 total) compile every seed against `AutoCAD.NET 25.1.0` from NuGet cache with the bridge's exact imports, guard, and globals—no AutoCAD needed, 0 skipped. Registry root `%AppData%\HPAutoCad\McpServer\` (registry.db + tools-library). `ContextService.Shape` hides Revit-only fields (`revitVersion`, `isFamily`) — Revit output byte-identical, non-Revit output clean. Pipe-in-use message names the host and version via `RequestDispatcher.HostName` / `HostVersion`. Bridge log sink `shared: true` so a second instance can log its pipe conflict.

**Live verification harness (phase 5):** Unattended test at `HPAutoCad/tools/harness/` (`run-live-verify.ps1` + `live-verify.py` + `mcp-session.py`) runs one stdio MCP session through 65 scenarios (64 pass + 1 skip) across AutoCAD and Revit on an isolated registry root (`output/live-verify/registry`). Verifies: execute matrix 18 (none/dryRun/commit, exception, none + modify, manual, guard on `GetPoint`/`Commit`/`SendStringToExecute`, compile error, `cancel_execution` racing, timeout 5 s, busy → **ESC posted → retry automated**, no drawing, audit); every seed 18 (real block, pickfirst set, dryRun); MISS → ad-hoc code + `propose_tool` → `test_tool` → `publish_tool` → CLI approve → `tools/list_changed` in 0.5 s → call by name; fragile tool (unguarded `eKeyNotFound`) → 5 × fail → quarantine → `manage_tool restore` + `propose_tool newVersion` (guarded, `ArgumentException`) → re-approve → stays published on 5 × fail; Revit exe beside (34 tools, Revit lib hash unchanged, opt-in off so E1 skipped); isolation (second AutoCAD fails fast naming AutoCAD, Civil 3D never loads the bundle). Harness: `-IncludeIsolation` / `-OnlyIsolation` / `-SkipRevit` / `-UseLiveRegistry` flags; kills only processes it started; isolates the server and CLI to an ephemeral registry; test report 2026-09-14 run 3: 64 pass + 1 skip + 4/4 isolation + 21/21 harness regression; xUnit: 96 + 109 + 58 (263 total).

**Ribbon tab "MCP AutoCAD" (bundle 0.2.0):** Built by the loader with `Autodesk.Windows` (AdWindows.dll from the `AutoCAD.NET` package — compile-time only via `ExcludeAssets=runtime`, nothing copied). Tab id `HPAUTOCAD_MCP_TAB`; created once the Ribbon exists, re-created after a workspace switch (via `SystemVariableChanged` → `Application.Idle` → `EnsureCreated` guard), removed on `Terminate`. Never duplicates. Every button forwards to a bridge entry point — the same delegates the `HPMCP*` commands call — so a click needs no drawing, never edits one, and works while a command is waiting for input. Icons are vector drawings in code. No CUIx modification, no change to `acad.cuix` or workspaces.

| Panel | Nút | Handler → bridge entry point | Command tương đương |
|---|---|---|---|
| Kết nối | Bảng điều khiển | `BridgeActions.Run("show")` → status window | `HPMCPBRIDGE` |
| Kết nối | Bật listener / Tắt listener | `Run("start")` / `Run("stop")` → `McpBridgeHost.Start/Stop` | `HPMCPSTART` / `HPMCPSTOP` |
| Kết nối | Trạng thái | `Run("status")` → command line, or an alert when no drawing | `HPMCPSTATUS` |
| Kết nối | (label) | `status.subscribe` → `McpBridgeHost.StateChanged`; live state: bridge ready · listening · server connected ± busy/error + opt-in | — |
| Công cụ | Sao chép script cuối | `copyLastScript` → clipboard ← `McpBridgeHost.LastRun.Source` | (window button) |
| Công cụ | Thư viện tool | `path("library")` → `%AppData%\HPAutoCad\McpServer\tools-library` in Explorer | — |
| Thiết lập | Mở nhật ký | `%LocalAppData%\HPAutoCad\McpBridge\logs` in Explorer | — |
| Thiết lập | Mở audit | `path("audit")` → `%AppData%\HPAutoCad\McpBridge\audit` | (window button) |
| Thiết lập | Tự khởi động listener | `autoStart.get/set` → `settings.json` `AutoStartListener` | (window checkbox) |
| Thiết lập | Hướng dẫn | opens `Contents\README.md` shipped in the bundle | — |

Not on the Ribbon, on purpose: the "Allow AI code execution" opt-in (stays in the window, OFF on every start, never persisted) and any "run a tool" button (tools run through the MCP server, which the host coding agent owns — the bridge never starts or stops it). When the bridge fails to start, Ribbon-backed buttons are disabled with a tooltip to open the log. Entry points stay **BCL-only across the ALC boundary** (`Func<Action<string,string>, Action>`, `Func<string>`, `Func<bool>`, `Action<bool>`, `Func<string,string>`). Load-bearing gotchas for future work: `UseWPF` on the loader removes the implicit `System.IO` using (add it explicitly); AdWindows exposes a Ribbon tab header as a `Button` whose `AutomationId` is the tab id (not `TabItem`) and buttons as `Button` named after their text once the tab is selected; `SendCommand('_.WSCURRENT …')` over COM never returns — switch workspaces via `ActiveDocument.SetVariable('WSCURRENT', …)`. Workspace switch drops code-added tabs; loader re-creates them. Verified live 2026-09-14 by `run-ribbon-check.ps1` 8/8: tab exactly once, still once after a workspace round trip, Bật listener → pipe up, Tắt → down, Bảng điều khiển → window, Trạng thái clean. Regressions: bridge 21/21, server smoke 22/22 (accepts approved tools ≥ 24).

Install / update / remove: `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug` with AutoCAD closed deploys the bundle (`%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.McpBridge.bundle\`, autoloaded, SECURELOAD answer *Always Load* once). Removing that folder uninstalls. To test without the bundle: `NETLOAD` `Contents\HPAutoCad.McpBridge.Loader.dll` from a copy. Live check: `pwsh HPAutoCad/tools/harness/run-ribbon-check.ps1`.

**Publish & deployment:** Exe 7.4 MB (publish SelfContained=false). `.mcp.json` entry (user adds, untracked): `hprebar-autocad` → exe path + env `HPAUTOCAD_MCP_Bridge__HostVersion=2026`.

**Known gaps:** Revit opt-in with the new engine unverified at runtime (harness E1 skipped); Revit 2025; AutoCAD 2026 Update 1.2 (.NET 10); `test_tool realRun=true`; FTS name boost; `get_run.revitVersion` field name kept (alias if needed); modal dialog while waiting (same `IsQuiescent` path as busy, not exercised); per-run undo (needs `ExecuteInCommandContextAsync`); `HPRebar/output/HPRebar.Mcp.Server.exe` not republished (locked by running `hprebar-revit` MCP — restart it then publish).

## Agent Config Sync (`.claude` ↔ `.agents` ↔ `.codex`)

**`AGENTS.md` is a generated mirror of `AGENTS.md`.** Edit `AGENTS.md` and re-sync; never hand-edit `AGENTS.md` — the edit will be overwritten. The rewrite table is `_TO_PORTABLE` in `scripts/skill_sync/adapters/portable_markdown.py`: `.agents/skills` → `.agents/skills`, `.agents/rules` → `.agents/rules`, `AGENTS.md` → `AGENTS.md`, `the host coding agent` → `the host coding agent`, tool names → host capability names. Paths it deliberately leaves alone — `.claude/settings.json`, `.claude/hooks/`, `.claude/scripts/` — are real the host coding agent files with no portable equivalent; rewriting them would make `AGENTS.md` wrong. Regenerate with the engine's own table rather than by hand:

```bash
python -c "import io,sys; sys.path.insert(0,'.'); from scripts.skill_sync.adapters import portable_markdown as pm; s=io.open('AGENTS.md',encoding='utf-8').read(); io.open('AGENTS.md','w',encoding='utf-8',newline='
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
6. **Testing**: `Document` is sealed and cannot be mocked, so pure logic MUST live in a layer that never touches the Revit API to stay xUnit-testable. Framework decision tree is in `.agents/skills/revit-test/` — TUnit for in-process, xUnit for pure logic, ricaun-io `RevitTest` when the VS Test Adapter UI is required.

## Stale Documentation Warning

Several docs and plans still reference the pre-rename `RevitAIApp/MyRevitAIApp/` layout, which no longer exists: `docs/duplicate-sheets-design.md`, `docs/superpowers/specs/2026-08-23-*.md`, and the `plans/260530-*`, `plans/260614-*`, `plans/260823-*` folders. Map those paths to `HPRebar/HPRebar/` when reading, and do not trust file paths quoted in them without checking.

## Workflows

- Primary workflow: `./.agents/rules/primary-workflow.md`
- Development rules: `./.agents/rules/development-rules.md`
- Orchestration protocols: `./.agents/rules/orchestration-protocol.md`
- Documentation management: `./.agents/rules/documentation-management.md`
- And other workflows: `./.agents/rules/*`

**IMPORTANT:** Analyze the skills catalog and activate the skills needed for the task during the process.
**IMPORTANT:** DO NOT modify skills in `.agents/skills` directly. **MUST** modify skills in this working directory, unless asked otherwise.
**IMPORTANT:** You must follow strictly the development rules in `./.agents/rules/development-rules.md`.
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
the host coding agent
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

When a tool call is blocked by the privacy-block hook, the output contains a JSON marker between `@@PRIVACY_PROMPT_START@@` and `@@PRIVACY_PROMPT_END@@`. **You MUST use the `host user-input capability` tool** to get proper user approval.

1. Parse the JSON from the hook output.
2. Call `host user-input capability` with the question data from that JSON.
3. On **"Yes, approve access"** → read the file with `bash cat "filepath"` (bash is auto-approved). On **"No, skip this file"** → continue without it.

**IMPORTANT:** Always ask via `host user-input capability` first. Never work around the privacy block without explicit user approval.

## Python Scripts (Skills)

`.claude/scripts/requirements.txt` pins `pyyaml>=6.0`. There is currently **no `.agents/skills/.venv`** in this checkout — the system `python` is what runs. If a skill script needs `google-genai`, `pypdf`, etc., create the venv first rather than assuming it exists.

**IMPORTANT:** When a skill script fails, don't stop — fix it directly.

## NotebookLM (`notebooklm-py`)

Unofficial client for Google Gemini Notebook, wired in on three surfaces. Upstream is `teng-lin/notebooklm-py` (MIT) — it talks to undocumented Google APIs, so treat breakage as expected, not exceptional.

| Surface | Where | Notes |
|---|---|---|
| CLI `notebooklm` | `uv tool install "notebooklm-py[browser,markdown]"` → `~/.local/bin` | Pinned at 0.8.2. Deliberately **without** the `mcp` extra |
| Agent skill | `.agents/skills/notebooklm/` + mirror `.agents/skills/notebooklm/` | `SKILL.md` + `LICENSE` only |
| MCP server | `.mcp.json` → `uvx --from "notebooklm-py[mcp]" notebooklm-mcp` | 38 tools, stdio |
| Python lib | `scripts/.venv` + `scripts/notebooklm_client.py` | Pinned in `scripts/requirements-notebooklm.txt` |

**Rules — all three exist because the obvious command does the wrong thing here:**

- **Never run `notebooklm skill install`.** It writes `.agents/skills/notebooklm/`, which this file forbids. Reinstall via `npx -y skills add teng-lin/notebooklm-py --skill notebooklm --agent claude-code`, then **prune** — that installer clones the whole 111 MB upstream repo into the skill dir, including a nested `AGENTS.md`. Keep `SKILL.md` + `LICENSE`, delete the rest.
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

**IMPORTANT:** *MUST READ* and *MUST COMPLY* with all *INSTRUCTIONS* in this `./AGENTS.md`, especially the *WORKFLOWS* section. This rule is *MANDATORY. NON-NEGOTIABLE. NO EXCEPTIONS.*
