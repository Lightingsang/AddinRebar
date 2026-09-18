# HPCivil3d — Civil 3D 2026 MCP

The fifth HP MCP host: an AI agent scripts **Autodesk Civil 3D 2026** through the shared HP MCP engine (`../McpShared/`).
Civil 3D is an AutoCAD vertical — the same `acad.exe`, R25.1, .NET 8 — so this folder is the AutoCAD bridge
(`../HPAutoCad/`) copied with Civil tokens plus the Civil API on top; it never references `HPAutoCad/`, and a mirror
test (phase 2) keeps the copied files in step. Plan: `../plans/260917-1633-civil3d-mcp-2026/`.

```
Claude Code ──stdio──▶ HPCivil3d.Mcp.Server.exe (net10)  ──pipe hpcivil3d-mcp-2026──▶ HPCivil3d.McpBridge (inside acad.exe /product C3D)
                                                                              guard (AutoCAD + Civil denials) → Roslyn 5.9 in its own load context → Idle → tr → Civil API
```

| Project | TFM | What |
|---|---|---|
| `HPCivil3d.McpBridge.Loader` | net8.0-windows | The assembly the autoloader NETLOADs (`Bundle/PackageContents.xml`, `Platform="Civil3D"`). Creates the isolated load context, starts the bridge by reflection, owns the commands `HPC3DMCPBRIDGE` / `HPC3DMCPSTART` / `HPC3DMCPSTOP` / `HPC3DMCPSTATUS` and the ribbon tab **HPCivil3d ▸ MCP ▸ MCP Bridge**. |
| `HPCivil3d.McpBridge` | net8.0-windows | The bridge: pipe host, script runner (outer/inner transaction, dryRun = abort), `civil` global (`CivilDocument`), Civil units, context, serializer, status window. References `AeccDbMgd`, `AeccPressurePipesMgd`, `AecBaseMgd` from the **installed** Civil 3D (`Directory.Build.props`, never copied). |
| `HPCivil3d.Mcp.Server` | net10 | The stdio MCP server exe: `Civil3dHostProfile`, `execute_civil3d_code`, `get_civil3d_context`, the engine's `inspect_type`/`cancel_execution` + 8 registry tools, seeds (phase 3). Never references the AutoCAD or Civil API — builds on any machine. |

## Build / deploy / remove

```bash
dotnet build HPCivil3d/HPCivil3d.slnx -c Debug                       # Civil 3D closed: deploys the bundle to %AppData%\Autodesk\ApplicationPlugins\HPCivil3d.McpBridge.bundle\
dotnet build HPCivil3d/HPCivil3d.slnx -c Debug -p:DeployBundle=false  # while Civil 3D is open (it locks the DLLs)
dotnet build HPCivil3d/HPCivil3d.slnx -c Debug -p:Civil3dInstallDir=X:\nowhere\   # machine without Civil 3D: bridge fails with one readable error, server still builds
```

- **The bridge needs Civil 3D 2026 installed to build** (`AeccDbMgd.dll` in `<AutoCAD 2026>\C3D\`, `AecBaseMgd.dll` in `<AutoCAD 2026>\ACA\`). `Directory.Build.props` finds the folder from `HKLM\SOFTWARE\Autodesk\AutoCAD\R25.1\ACAD-9100:409\Location` (9100 = Civil 3D); override with `-p:Civil3dInstallDir=…\C3D\` or env `HPCIVIL3D_C3D_DIR`.
- The bundle loads **only in Civil 3D** (`Platform="Civil3D"`, `SeriesMin/Max="R25.1"`): plain AutoCAD 2026 and Advance Steel 2026 share the exe but never load it, and the HPAutoCad bundle (`Platform="AutoCAD"`) never loads in Civil 3D — both can run at once on their own pipes.
- Start Civil 3D the way its shortcut does: `acad.exe /ld "<AutoCAD 2026>\AecBase.dbx" /p "<<C3D_Metric>>" /product C3D /language en-US` (profile names use underscores).
- Unsigned DLLs: Civil 3D 2026 asks *Security - Unsigned Executable File* once **per DLL hash** — the loader, the bridge and the two engine assemblies, so up to four prompts on the first start and again after every rebuild (the dialog does not name the file). Click **Always Load** for each; the spike harness answers **Load Once** for its own acad.exe only and grants no permanent trust.
- Remove: delete `%AppData%\Autodesk\ApplicationPlugins\HPCivil3d.McpBridge.bundle\`.
- Logs: `%LocalAppData%\HPCivil3d\McpBridge\logs\` (`loader.log` + `mcpbridge-*.log`; look for `MCP scripting self-check OK … civil Civil3D`), settings `%AppData%\HPCivil3d\McpBridge\settings.json`, audit `%AppData%\HPCivil3d\McpBridge\audit\`, registry `%AppData%\HPCivil3d\McpServer\`.

## Script contract (what the AI sees)

Globals `doc, db, ed, app, tr, units, civil, ct, log, progress, args` — the AutoCAD set plus `civil` (the active
`CivilDocument`, null when the drawing holds no Civil data). `units` converts mm ↔ the **Civil drawing unit**
(Meters or Feet from the drawing settings, not INSUNITS); plan geometry crosses the tool boundary in mm, stations and
elevations stay in drawing units and every envelope says which. `transaction=auto|none|manual` and `dryRun` behave as in
the AutoCAD MCP (outer transaction aborted = nothing kept). Blocked on top of the AutoCAD guard: `Rebuild`/`RebuildAll`/
`RebuildSnapshot`, data shortcuts, the survey database, file import/export members, `AeccUiMgd` dialogs, `Autodesk.AECC.Interop`.

## Client wiring (user adds, never committed)

`.mcp.json` entry `hprebar-civil3d` → `HPCivil3d/output/HPCivil3d.Mcp.Server/HPCivil3d.Mcp.Server.exe` (publish:
`dotnet publish HPCivil3d/HPCivil3d.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o HPCivil3d/output/HPCivil3d.Mcp.Server`)
with env `HPCIVIL3D_MCP_Bridge__HostVersion=2026`. Open Civil 3D, ribbon **HPCivil3d ▸ MCP ▸ MCP Bridge**, start the
listener, tick *Allow AI code execution* (off on every start, never persisted).

## Harness (`tools/harness/`)

`run-spike.ps1` (Windows PowerShell 5.1) is the phase-1 spike: builds and deploys, starts Civil 3D unattended, ticks the
opt-in through UI Automation, runs `spike.py` over stdio (copies of the Civil tutorial drawings from
`<AutoCAD 2026>\C3D\Help\Civil Tutorials\Drawings\` are the scene), then proves plain AutoCAD and Advance Steel do not
load the bundle and that AutoCAD + Civil 3D serve two pipes at once. It closes drawings without saving and kills only the
`acad.exe` processes it started — close your own AutoCAD/Civil 3D/Advance Steel first. Two older MCP bundles with
`Platform="AutoCAD*"` on the dev machine (`Civil3dMcp.bundle`, `AutoCadMcp.bundle`) load into every product; the harness
leaves them alone.

`tools/mirror-tokens.json` is the token table the copy from `HPAutoCad/` used; the phase-2 mirror tests apply it to the
AutoCAD file and require the Civil file to match (outside the `// civil-only: begin/end` blocks).
