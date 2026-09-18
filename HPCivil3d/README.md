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
`CivilDocument`; every drawing opened in Civil 3D has one, Civil settings are created lazily, so null is only the
defensive path). `units` converts mm ↔ the **Civil drawing unit** (Meters or Feet from the drawing settings, not
INSUNITS); plan geometry crosses the tool boundary in mm, stations and elevations stay in drawing units and every
envelope says which. A drawing without Civil settings (a plain `acad.dwt` drawing opened in Civil 3D) reports
`DrawingUnits = Feet` whatever INSUNITS says — `get_civil3d_context` flags that as `insunitsMismatch: true`, the
scripts follow the Civil unit, and a seed must warn before writing coordinates into such a drawing. `transaction=auto|none|manual` and `dryRun` behave as in
the AutoCAD MCP (outer transaction aborted = nothing kept). Blocked on top of the AutoCAD guard: `Rebuild`/`RebuildAll`/
`RebuildSnapshot`, data shortcuts, the survey database, file import/export members, `AeccUiMgd` dialogs, `Autodesk.AECC.Interop`.

## Client wiring (user adds, never committed)

`.mcp.json` entry `hprebar-civil3d` → `HPCivil3d/output/HPCivil3d.Mcp.Server/HPCivil3d.Mcp.Server.exe` (publish:
`dotnet publish HPCivil3d/HPCivil3d.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -o HPCivil3d/output/HPCivil3d.Mcp.Server`)
with env `HPCIVIL3D_MCP_Bridge__HostVersion=2026`. Open Civil 3D, ribbon **HPCivil3d ▸ MCP ▸ MCP Bridge**, start the
listener, tick *Allow AI code execution* (off on every start, never persisted).

## Tests (`HPCivil3d.McpBridge.Tests`, net10, xunit v3 — no AutoCAD or Civil reference, builds anywhere)

`dotnet test HPCivil3d/HPCivil3d.McpBridge.Tests` (41): **MirrorTests** read `tools/mirror-tokens.json` and require every
mirrored file to equal its `HPAutoCad/` source after the tokens are applied (in order) and the `// civil-only: begin/end`
(`<!-- civil-only: begin/end -->`) blocks are stripped — a block may only add lines; `civilOwnedFiles` (BridgeEntry, the
self-check, the csproj, the bundle manifest, the units readers) are not compared; every source file under the two bridge
projects must be in one list or the other; versions are neutralised so a bump on either side is not drift. Changing one
character outside a block fails exactly one test naming the file and line. `Civil3dUnitTableTests` pin the unit rule
(`Civil3dUnitTable.cs` is linked into the test project): Meters 1000 / Feet 304.8, INSUNITS only without a Civil unit,
`insunitsMismatch` when they disagree, US survey feet = feet, `"."` = no zone.

## Harness (`tools/harness/`)

`run-bridge-unattended.ps1` (Windows PowerShell 5.1) is the pipe harness: starts Civil 3D with `bridge.scr`
(`HPC3DMCPBRIDGE` + `HPC3DMCPSTART`), answers SECURELOAD (*Load Once*, its own acad.exe only), ticks the opt-in through
UI Automation, opens a copy of `Align-7C.dwg` and runs `pipe-scenarios.py` straight against the pipe (no MCP server):
the AutoCAD set — read, dryRun, commit, exception, `none` + modify, `manual`, modify + erase, guard ×3, compile error,
cancel, timeout, progress, serializer, `U` — plus the Civil set C1–C9 (context `civil3d` block, alignments and
alignment entities serialised, COGO point dryRun / commit + `U`, guard `RebuildAll` and data shortcuts, a Civil exception
as `PointNotOnEntityException: …`, a style with its name), then opt-in off → `-32001`, no drawing → `-32003`, busy →
`-32002` → ESC → retry. 31 checks. `run-ribbon-check.ps1` (pwsh 7) checks the `HPCivil3d ▸ MCP ▸ MCP Bridge` tab the way
the AutoCAD one does (exactly one tab, workspace and COLORTHEME round trips, the button opens one window; icon = MANUAL
with screenshots under `output/ribbon-check/`).

`run-spike.ps1` (Windows PowerShell 5.1) is the phase-1 spike: builds and deploys, starts Civil 3D unattended, ticks the
opt-in through UI Automation, runs `spike.py` over stdio (copies of the Civil tutorial drawings from
`<AutoCAD 2026>\C3D\Help\Civil Tutorials\Drawings\` are the scene), then proves plain AutoCAD and Advance Steel do not
load the bundle and that AutoCAD + Civil 3D serve two pipes at once. It closes drawings without saving and kills only the
`acad.exe` processes it started — close your own AutoCAD/Civil 3D/Advance Steel first. Two older MCP bundles with
`Platform="AutoCAD*"` on the dev machine (`Civil3dMcp.bundle`, `AutoCadMcp.bundle`) load into every product; the harness
leaves them alone.

`tools/mirror-tokens.json` is the mirror contract (tokens, version patterns, mirrored and Civil-owned files) — its `note`
states the rules; the MirrorTests above enforce them.
