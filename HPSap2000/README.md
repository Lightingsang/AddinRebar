# HPSap2000 — SAP2000 27 MCP

Turns a running **SAP2000 27** (or v24–v26) into a runtime for an AI agent, with the same engine, tool registry and safety model as the
Revit, AutoCAD, Navisworks and ETABS MCPs in this repo (`../McpShared/`). Modeled directly after `HPEtabs`, with SAP2000-specific adaptations:

- **The OAPI is out-of-process COM.** Nothing loads into SAP2000.exe. `HPSap2000.McpBridge.exe` is a small desktop app you run
  beside SAP2000; it holds the one COM attachment (through the managed `SAP2000v1.dll` wrapper CSI installs), the named pipe
  `hpsap2000-mcp-27` and the two opt-in checkboxes. `HPSap2000.Mcp.Server.exe` is the stdio MCP server Claude Code / Antigravity launches;
  it never references the SAP2000 API.
- **SAP2000 has no transaction and no undo.** A script is classified before it runs: **R** read-only (runs under
  `transaction: none`), **W** write (`auto`: the bridge saves your model and copies a `.SDB` snapshot first), **D** destructive
  (unlock, `RunAnalysis`, file operations, deletes — needs the second checkbox *Allow destructive operations*, else refused
  with `-32001`). `dryRun` on a writing script is a static preview: nothing runs.
- **Consistent Execution Units:** Internal execution is forced to `kN_m_C` (Kilonewton, Metre, °C) and restored in `finally`. Lengths are in Metres (m), forces in Kilonewtons (kN), moments in kN·m, stresses in kN/m² (kPa).

Version numbers are CSI's (**27**, not a year): pipe `hpsap2000-mcp-27`, env `HPSAP2000_MCP_Bridge__HostVersion=27`.

## Layout

```
HPSap2000.slnx · global.json · Directory.Build.props   (finds the SAP2000 install folder: -p:Sap2000InstallDir / HPSAP2000_SAP2000_DIR / COM registration / Program Files)
HPSap2000.McpBridge/          WPF desktop app, net8.0-windows — attachment, STA worker, tier gate, opt-in window. References SAP2000v1.dll, never copies it.
HPSap2000.McpBridge.Tests/    xUnit, needs SAP2000 installed (it references the bridge)
HPSap2000.Mcp.Server/         net10 stdio MCP server — profile, 4 core tools, prompts, resources, seed library (12 seeds)
HPSap2000.Mcp.Server.Tests/   xUnit, builds and runs on any machine (tests compile seeds against installed SAP2000v1.dll)
tools/harness/                run-live-verify.ps1 + live-verify.py + spike-step.ps1 (UIA on our own window; never drives SAP2000)
output/                       publish targets and harness output (git-ignored)
```

## Build / test

```bash
dotnet build HPSap2000/HPSap2000.slnx -c Debug        # builds bridge, server, and test suites
dotnet test HPSap2000/HPSap2000.Mcp.Server.Tests      # 79 tests: seeds, profile, registry
dotnet test HPSap2000/HPSap2000.McpBridge.Tests       # 165 tests: unit policies, tiers, snapshot, serializer, guard
```

Without SAP2000 installed the bridge projects fail with one readable error; the server and its tests still build.

## Run

1. Start SAP2000 27 and open a model (a **throw-away copy** while you try things: writing scripts save the model).
2. Start `HPSap2000.McpBridge.exe`, click **Start listener**, click **Attach**, tick **Allow AI code execution**.
   Tick **Allow destructive operations** only for a run that needs it; both boxes are off again every time the app starts.
3. Add the server to `.mcp.json`:

```json
"hprebar-sap2000": {
  "command": "G:/…/HPSap2000/output/HPSap2000.Mcp.Server/HPSap2000.Mcp.Server.exe",
  "env": { "HPSAP2000_MCP_Bridge__HostVersion": "27" }
}
```

Publish (server single-file; the bridge as a **folder** — Roslyn needs real file locations):

```bash
dotnet publish HPSap2000/HPSap2000.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o HPSap2000/output/HPSap2000.Mcp.Server
dotnet publish HPSap2000/HPSap2000.McpBridge -c Release -r win-x64 -p:SelfContained=false -o HPSap2000/output/HPSap2000.McpBridge
```

Registry root `%AppData%\HPSap2000\McpServer\` (`registry.db` + `tools-library`), bridge settings `%AppData%\HPSap2000\McpBridge\settings.json`,
audit `%AppData%\HPSap2000\McpBridge\audit\`, log `%LocalAppData%\HPSap2000\McpBridge\logs\`. CLI: `HPSap2000.Mcp.Server.exe registry list|pending|approve <name> --by <who>|…`.

## Live verification

```powershell
pwsh HPSap2000/tools/harness/run-live-verify.ps1 -Phase detached          # bridge only: opt-in, guard, preview, refusal codes — no SAP2000 needed
pwsh HPSap2000/tools/harness/run-live-verify.ps1 -Phase spike             # + Attach to running SAP2000: units, GetNameList, timeout
pwsh HPSap2000/tools/harness/run-live-verify.ps1 -Phase bridge            # + writes with snapshots, destructive on/off (writes open model!)
pwsh HPSap2000/tools/harness/run-live-verify.ps1 -Phase seeds             # + the 12 seeds, the registry loop, a real run_analysis
pwsh HPSap2000/tools/harness/run-live-verify.ps1 -Phase full -Publish     # everything from publish folders
pwsh HPSap2000/tools/harness/spike-step.ps1 -Action start|attach|phase <p>|destructive on|off|state|stop
```

The harness starts and stops only the bridge exe it launched; it never starts, closes or sends keys to SAP2000. The `bridge` and
`seeds` phases save and modify the open model — a throw-away copy only. Details: `tools/harness/README.md`.
