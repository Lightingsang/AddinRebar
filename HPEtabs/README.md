# HPEtabs — ETABS 22 MCP

Turns a running **ETABS 22** into a runtime for an AI agent, with the same engine, tool registry and safety model as the
Revit, AutoCAD and Navisworks MCPs in this repo (`../McpShared/`). Two things are different about ETABS and shape everything here:

- **The OAPI is out-of-process COM.** Nothing loads into ETABS.exe. `HPEtabs.McpBridge.exe` is a small desktop app you run
  beside ETABS; it holds the one COM attachment (through the managed `ETABSv1.dll` wrapper CSI installs), the named pipe
  `hpetabs-mcp-22` and the two opt-in checkboxes. `HPEtabs.Mcp.Server.exe` is the stdio MCP server Claude Code launches;
  it never references the ETABS API.
- **ETABS has no transaction and no undo.** A script is classified before it runs: **R** read-only (runs under
  `transaction: none`), **W** write (`auto`: the bridge saves your model and copies a `.EDB` snapshot first), **D** destructive
  (unlock, `RunAnalysis`, file operations, deletes — needs the second checkbox *Allow destructive operations*, else refused
  with `-32001`). `dryRun` on a writing script is a static preview: nothing runs. Plan and ADRs:
  `../plans/260916-2152-etabs-mcp-2026/`.

Version numbers are CSI's (**22**, not a year): pipe `hpetabs-mcp-22`, env `HPETABS_MCP_Bridge__HostVersion=22`.

## Layout

```
HPEtabs.slnx · global.json · Directory.Build.props   (finds the ETABS install folder: -p:EtabsInstallDir / HPETABS_ETABS_DIR / COM registration / Program Files)
HPEtabs.McpBridge/          WPF desktop app, net8.0-windows — attachment, STA worker, tier gate, opt-in window. References ETABSv1.dll, never copies it.
                            Icon: a three-storey frame on its foundation + the MCP plug (Resources/HPEtabsMcpBridge.ico, ApplicationIcon + Window.Icon).
HPEtabs.McpBridge.Tests/    xUnit, needs ETABS 22 installed (it references the bridge)
HPEtabs.Mcp.Server/         net10 stdio MCP server — profile, 4 core tools, prompts, resources, seed library
HPEtabs.Mcp.Server.Tests/   xUnit, builds and runs on any machine (no ETABS needed)
tools/harness/              run-live-verify.ps1 + live-verify.py + spike-step.ps1 (UIA on our own window; never drives ETABS)
tools/icons/                render-app-icon.ps1 — the one vector glyph behind HPEtabs.McpBridge/Resources/HPEtabsMcpBridge.ico (exe + title bar)
output/                     publish targets and harness output (git-ignored)
```

## Build / test

```bash
dotnet build HPEtabs/HPEtabs.slnx -c Debug            # needs ETABS 22 installed for the two bridge projects
dotnet test HPEtabs/HPEtabs.Mcp.Server.Tests          # no ETABS needed
dotnet test HPEtabs/HPEtabs.McpBridge.Tests           # needs ETABS 22 installed
```

Without ETABS installed the bridge projects fail with one readable error; the server and its tests still build.

## Run

1. Start ETABS 22 and open a model (a **throw-away copy** while you try things: writing scripts save the model).
2. Start `HPEtabs.McpBridge.exe`, click **Start listener**, click **Attach**, tick **Allow AI code execution**.
   Tick **Allow destructive operations** only for a run that needs it; both boxes are off again every time the app starts.
3. Add the server to `.mcp.json` (not tracked; never touch the other hosts' entries):

```json
"hprebar-etabs": {
  "command": "F:/…/HPEtabs/output/HPEtabs.Mcp.Server/HPEtabs.Mcp.Server.exe",
  "env": { "HPETABS_MCP_Bridge__HostVersion": "22" }
}
```

Publish (server single-file; the bridge as a **folder** — Roslyn needs real file locations):

```bash
dotnet publish HPEtabs/HPEtabs.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o HPEtabs/output/HPEtabs.Mcp.Server
dotnet publish HPEtabs/HPEtabs.McpBridge -c Release -r win-x64 -p:SelfContained=false -o HPEtabs/output/HPEtabs.McpBridge
```

Registry root `%AppData%\HPEtabs\McpServer\` (`registry.db` + `tools-library`), bridge settings `%AppData%\HPEtabs\McpBridge\settings.json`,
audit `%AppData%\HPEtabs\McpBridge\audit\`, log `%LocalAppData%\HPEtabs\McpBridge\logs\`. CLI: `HPEtabs.Mcp.Server.exe registry list|pending|approve <name> --by <who>|…`.

## Live verification

```powershell
pwsh HPEtabs/tools/harness/run-live-verify.ps1 -Phase detached          # bridge only: opt-in, guard, preview, refusal codes — no ETABS needed
pwsh HPEtabs/tools/harness/run-live-verify.ps1 -Phase spike             # + Attach to the running ETABS: units, GetNameList, timeout
pwsh HPEtabs/tools/harness/run-live-verify.ps1 -Phase bridge            # + writes with snapshots, destructive on/off (writes the open model!)
pwsh HPEtabs/tools/harness/run-live-verify.ps1 -Phase seeds             # + the 12 seeds, the registry loop, a real run_analysis
pwsh HPEtabs/tools/harness/run-live-verify.ps1 -Phase full -Publish -Runs 3   # everything from the publish folders (3 × 102 checks, 2026-09-17)
pwsh HPEtabs/tools/harness/spike-step.ps1 -Action start|attach|phase <p>|destructive on|off|state|stop   # one step at a time while you change ETABS's state
```

The harness starts and stops only the bridge exe it launched; it never starts, closes or sends keys to ETABS. The `bridge` and
`seeds` phases save and modify the open model — a throw-away copy only. Details: `tools/harness/README.md`.

## Status

**Plan complete (phases 0–4, 2026-09-17), verified live on ETABS 22 v22.7.0.4095.** 24 tools (4 core + 8 registry + 12 seeds), the
three tiers with the save-and-snapshot path, the registry loop (propose → test → publish → CLI approve → quarantine → restore),
all driven by `run-live-verify.ps1 -Phase full -Publish` from the publish folders (3 × 102 checks). Reports:
`../plans/260916-2152-etabs-mcp-2026/reports/phase-0[1-4]-*.md`. Not done: kill ETABS in the middle of a call, a second licence seat,
restoring a snapshot in the ETABS GUI, `run_analysis` on a real project — see the known-gaps list in the root `CLAUDE.md`.

Gotchas: start ETABS from its shortcut and then File › Open the model — an instance started another way may not register its API
object (`GetObject` null; "not registered for the API in this session"); an elevated ETABS is invisible to the non-elevated bridge.
Rebuilding the bridge fails (MSB3027) while any `HPEtabs.McpBridge.exe` runs. `File.Save(path)` is a save-as; after any save
`GetModelFilename` reports the `.$et` working copy — the bridge and the seeds report the `.EDB`. Unlocking the model discards its results.
