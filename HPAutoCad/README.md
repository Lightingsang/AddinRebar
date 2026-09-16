# HPAutoCad — AutoCAD MCP bridge

Turns AutoCAD 2026 into a runtime for an AI agent, the way `HPRebar/` does for Revit: an MCP server exe
(stdio, launched by the host AI) talks over a named pipe to a plugin loaded inside `acad.exe` that
compiles and runs the C# the agent sends, inside one transaction, with opt-in, guard, timeout and audit.

Plan of record: [`../plans/260913-0000-autocad-mcp-bridge-2026/`](../plans/260913-0000-autocad-mcp-bridge-2026/plan.md)
(ADR-06 explains why this is a separate top-level folder and why the engine lives in `../McpShared/`).

## Status

Phase 5 done (2026-09-14) — the plan is complete: `HPAutoCad.Mcp.Server.exe` serves MCP over stdio — 24 tools (4 core + 8 registry + 12 seed
tools installed into `%AppData%\HPAutoCad\McpServer\tools-library\` on first start), `autocad://` resources, 2 prompts —
and the whole loop is verified live in AutoCAD 2026: execute matrix, every seed, MISS → propose → test → publish →
CLI approve → `tools/list_changed` → call by name, quarantine → restore, the Revit exe beside it, a second AutoCAD
failing fast on the pipe and Civil 3D not loading the bundle (`tools/harness/run-live-verify.ps1`, 65/65 + 4/4;
`reports/phase-05-live-verify.md`). Phase 2 gave the bridge itself: pipe listener, main-thread
executor (`Application.Idle` + `IsQuiescent`), two bridge-owned transactions (`tr` is the script's), change counting,
audit, status window (`tools/harness/run-bridge-unattended.ps1`, 21/21).
AutoCAD commands: `HPMCPBRIDGE` (status window with the per-session "Allow AI code execution" opt-in), `HPMCPSTART`,
`HPMCPSTOP`, `HPMCPSTATUS` — and the Ribbon tab **HPAutoCad** ▸ **MCP** ▸ **MCP Bridge** (see below).

| Project | Phase | Purpose |
|---|---|---|
| `HPAutoCad.McpBridge.Loader` | 1 ✅ | The DLL AutoCAD loads: `IExtensionApplication`, the `HPMCP*` commands, the Ribbon tab (`Ribbon/`, Autodesk.Windows), an isolated `AssemblyLoadContext` for the real bridge |
| `HPAutoCad.McpBridge` | 1–2 ✅ | The bridge: Roslyn + self-check, `MainThreadExecutor`, `AutocadScriptRunner` (lock + outer/inner transaction, dryRun, timeout), context reader, serializer, XAML status window |
| `tools/harness/` | 2–5 ✅ | Unattended harnesses (Python + PowerShell): `run-bridge-unattended.ps1` (pipe, 21 scenarios), `run-server-smoke.ps1` (published exe over stdio, 22 steps incl. every seed), `run-live-verify.ps1` (phase-5 proof on one stdio session: matrix, seeds, registry loops, Revit beside, isolation), shared SECURELOAD/UIA/COM helpers |
| `HPAutoCad.Mcp.Server` | 3–4 ✅ | The MCP server exe (net10, stdio): `AutocadHostProfile`, `execute_autocad_code`, `get_autocad_context`, `autocad://` resources, prompts, 12 embedded seed tools (`Registry/SeedLibrary/`) |
| `HPAutoCad.Mcp.Server.Tests` | 3–4 ✅ | xUnit v3 (46): profile, tool surface, tools over a real pipe, every seed compile-checked against `AutoCAD.NET` 25.1.0 from the NuGet cache (no AutoCAD needed) |

Shared engine (referenced, never copied): `../McpShared/HPRebar.Mcp.Contracts`, `../McpShared/HPRebar.McpBridge.Core`,
`../McpShared/HPRebar.Mcp.Server.Core`. This folder never references `../HPRebar/`.

## Ribbon tab "HPAutoCad" ▸ "MCP" ▸ "MCP Bridge"

The same one-button surface as the Revit and Navisworks bridges (bundle 0.3.0; 0.2.0 carried three panels and ten
buttons — every one of them is a control of the status window, so the tab only opens that window now). Built by the
loader with `Autodesk.Windows` (AdWindows.dll from the `AutoCAD.NET` package — compile-time only, never copied). Tab
id `HPAUTOCAD_MCP_TAB`, title `HPAutoCad`; panel `MCP`; button `MCP Bridge` → `BridgeActions.Run("show")`, the same
delegate `HPMCPBRIDGE` calls, so a click needs no drawing and never edits one. The icon is a vector `DrawingImage`
drawn in code (window + plug, the glyph shared with the Navisworks bridge): every coordinate even, so the 16-px
small image is an exact half of the 32-px one; the frame ink follows `COLORTHEME` (light ink on the dark theme,
`#3C3C3C` on the light one), the plug is the HP MCP blue `#0696D7`. The tab is created once the Ribbon exists,
re-created after a workspace switch (`WSCURRENT`) and after a theme change (`COLORTHEME` → rebuilt with the other
ink), guarded by `FindTab` so it never duplicates, removed on `Terminate`. When the bridge failed to start, the
button is disabled and its tooltip names the loader log. No CUIx, no change to the user's `acad.cuix` or workspaces.

Install / update / remove: `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug` with AutoCAD closed deploys the bundle
(`%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.McpBridge.bundle\`, autoloaded by `PackageContents.xml`,
`Platform="AutoCAD"`, R25.1); removing that folder uninstalls. Trusted location = the bundle folder (answer *Always
Load* once; SECURELOAD stays on). To try a build without the bundle: `NETLOAD` `Contents\HPAutoCad.McpBridge.Loader.dll`
from a copy of the bundle. Live check: `pwsh HPAutoCad/tools/harness/run-ribbon-check.ps1` (12 checks + the icon as a
MANUAL item with a screenshot per theme).

## Target

AutoCAD 2026 base release (R25.1, .NET 8). `AutoCAD.NET` NuGet pinned to `[25.1.0]` — `25.1.1` and `26.0.0` are
the .NET 10 builds (2026 Update 1.2 / 2027) and do not load on the base release.

## Commands

```bash
dotnet build HPAutoCad/HPAutoCad.slnx -c Debug                     # deploys the bundle to %AppData%\Autodesk\ApplicationPlugins\
dotnet build HPAutoCad/HPAutoCad.slnx -c Debug -p:DeployBundle=false   # AutoCAD open (DLL locked)
cd HPAutoCad && dotnet test HPAutoCad.Mcp.Server.Tests            # 58 tests, no AutoCAD needed
pwsh HPAutoCad/tools/harness/run-bridge-unattended.ps1            # live: bridge over the pipe (AutoCAD must be closed)
pwsh HPAutoCad/tools/harness/run-server-smoke.ps1                 # live: published exe over stdio (publish first)
pwsh HPAutoCad/tools/harness/run-live-verify.ps1 -IncludeIsolation   # live: the phase-5 proof (registry loops, Revit beside, isolation)
pwsh HPAutoCad/tools/harness/run-ribbon-check.ps1                 # live: the Ribbon tab (UIA: one tab, workspace + theme round trips, button → window)
dotnet publish HPAutoCad/HPAutoCad.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -p:IncludeNativeLibrariesForSelfExtract=true -o HPAutoCad/output/HPAutoCad.Mcp.Server
HPAutoCad/output/HPAutoCad.Mcp.Server/HPAutoCad.Mcp.Server.exe registry approve <tool> --by <who>
```

`.mcp.json` entry (machine-specific, untracked):

```json
"hprebar-autocad": {
  "command": "<repo>\\HPAutoCad\\output\\HPAutoCad.Mcp.Server\\HPAutoCad.Mcp.Server.exe",
  "args": [],
  "env": { "HPAUTOCAD_MCP_Bridge__HostVersion": "2026" }
}
```

Runtime folders: registry `%AppData%\HPAutoCad\McpServer\` (tools-library, registry.db), bridge settings and
audit `%AppData%\HPAutoCad\McpBridge\`, bridge logs `%LocalAppData%\HPAutoCad\McpBridge\logs\`, pipe `hpautocad-mcp-2026`.
