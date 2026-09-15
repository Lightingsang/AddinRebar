# HPNavis — MCP bridge for Autodesk Navisworks Manage 2026

Turns Navisworks into a runtime for an AI agent, the way `HPRebar/` does for Revit and `HPAutoCad/` for AutoCAD:
a plugin inside `Roamer.exe` compiles and runs reviewed C# against the open model over a named pipe, and (from
phase 3) a stdio MCP server exe the host AI launches. Shared engine: `../McpShared/` only — this folder never
references `HPRebar/` or `HPAutoCad/`.

**Navisworks runs on .NET Framework 4.8**, unlike Revit/AutoCAD 2026 (.NET 8). The plugin is `net48` and consumes the
`net48` asset of `HPRebar.McpBridge.Core`. There is no isolated load context on .NET Framework; instead
`PluginAssemblyResolver` binds Roslyn and its System.* companions to the copies beside the plugin (allow-list,
only for requests from this plugin), and the start-up self-check verifies they came from there.

| Project | TFM | What |
|---|---|---|
| `HPNavis.McpBridge` | net48 | The plugin: `EventWatcherPlugin` (listener, executor) + `AddInPlugin` "HPNavis MCP" in the Add-ins menu (status window). |
| `HPNavis.Mcp.Server` | net10 | The stdio MCP server exe the host AI launches: `NavisHostProfile` (pipe `hpnavis-mcp-2026`, prefix `navis.`, registry root `%AppData%\HPNavis\McpServer\`, 600 s ceiling), `execute_navis_code`, `get_navis_context`, prompts `navis_query_template` / `navis_review_template`, resources `navis://document/info` and `navis://selection`, plus the engine's `inspect_type`, `cancel_execution` and 8 registry tools. Never references the Navisworks API. |
| `HPNavis.McpBridge.Tests` | net48 | xUnit v3 over the plugin's pure layers: undo rules, heavy gate, fingerprint delta, serializer bounds, resolver folder test — 62 cases, no Navisworks running (needs the API installed to build the plugin). |
| `HPNavis.Mcp.Server.Tests` | net10 | xUnit v3: the profile and tool surface this exe registers, the tools over a real pipe with a fake executor (600 s pass-through, refusal codes naming Navisworks, no-bridge error without machine paths) — 12 tests. |

## Build, deploy, remove

The API is referenced from the installed product (no NuGet exists). `Directory.Build.props` finds it through
`-p:NavisworksInstallDir=…`, `HPNAVIS_NAVISWORKS_DIR`, the installer's registry key, or
`%ProgramW6432%\Autodesk\Navisworks Manage 2026\`. Without it, the plugin and its tests do not build (one readable error);
the server and its tests do.

```bash
dotnet build HPNavis/HPNavis.slnx -c Debug                      # Navisworks closed: deploys the plugin too
dotnet build HPNavis/HPNavis.slnx -c Debug -p:DeployPlugin=false  # Navisworks open (it locks the files)
```

Deploy target: `%AppData%\Autodesk\Navisworks Manage 2026\Plugins\HPNavis.McpBridge\` — Navisworks requires the folder
name to equal the assembly name. Remove the plugin by deleting that folder; disable it temporarily by renaming the folder.

## Server exe and client wiring

```bash
dotnet publish HPNavis/HPNavis.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o HPNavis/output/HPNavis.Mcp.Server
```

`.mcp.json` entry (machine-specific, never commit it): `hprebar-navis` → that exe, env `HPNAVIS_MCP_Bridge__HostVersion=2026`. The exe
is locked while the server runs inside the host AI; stop it before republishing. `HPNavis.Mcp.Server.exe registry <command>` is the human
side of the tool registry (approve/reject/…), same CLI as the other hosts.

Smoke without a host AI (Navisworks closed, plugin deployed, exe published — Windows PowerShell 5.1):

```powershell
powershell.exe -ExecutionPolicy Bypass -File HPNavis/tools/harness/run-server-smoke.ps1
```

Starts Roamer with `gatehouse_pub.nwd`, ticks the opt-in, then runs one stdio session (`server-smoke.py`): initialize, tools/list,
get_navis_context, read-only execute, dry-run edit, heavy refusal, inspect_type, search_tools — 8/8 on 2026-09-15.

## Using it

1. Start Navisworks, open a model. Add-ins ▸ **HPNavis MCP** opens the bridge window.
2. **Start listener** (or tick auto-start). Pipe: `hpnavis-mcp-2026`.
3. Tick **Allow AI code execution** — per session, never persisted. Tick **Allow heavy operations** only when the AI needs
   to append/merge files, save/export, or run a clash test: those cannot be undone or interrupted.
4. Every run is one Undo entry `MCP: <label>`; dry runs are undone right after they commit; a run whose transaction
   produced no undo entry never touches your own undo history.

Logs: `%LocalAppData%\HPNavis\McpBridge\logs\` (look for `MCP scripting self-check OK`). Audit: `%AppData%\HPNavis\McpBridge\audit\`.
Settings (`AutoStartListener` only): `%AppData%\HPNavis\McpBridge\settings.json`.

## Unattended check (phase-1 spike harness)

```powershell
# Windows PowerShell 5.1 (not pwsh); Navisworks closed; plugin deployed
powershell.exe -ExecutionPolicy Bypass -File HPNavis/tools/harness/run-bridge-unattended.ps1 -Runs 2 -WithModal -WithNoDoc
```

Starts `Roamer.exe` with `Samples\gatehouse\gatehouse_pub.nwd` and the process-scoped variable
`HPNAVIS_MCP_BRIDGE_SHOW_WINDOW=1` (the bridge opens its status window once the GUI is up — only the window; the
execution opt-in still starts OFF), ticks the opt-ins through UI Automation, drives `pipe-scenarios.py` over the pipe
(reads, W1 edits, dryRun, same-label dry run, empty transaction, `none` violation, compile error, exception, guard, heavy
gate, big result bound, busy while running, timeout, cancel, clash + file append with heavy ON, a modal dialog, and with
`-WithNoDoc` a Roamer without a model), closes Navisworks without saving and writes `output/spike/run-N.{log,json}`.
43 checks per run, verified 2/2 on 2026-09-15 — `plans/260915-0824-navisworks-mcp-2026/reports/phase-02-bridge-runtime.md`.
Dialogs and focus are handled through Win32 (`EnumWindows`, `SetForegroundWindow`, `WM_CLOSE`); UI Automation is used only
inside the bridge's own window. The Automation API (`NavisworksApplication`) is not
used: a Roamer started that way exits within seconds on the dev machine.

Plan and evidence: `plans/260915-0824-navisworks-mcp-2026/`.
