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
`HPMCPSTOP`, `HPMCPSTATUS` — and, since bundle 0.2.0, the Ribbon tab **MCP AutoCAD** (see below).

| Project | Phase | Purpose |
|---|---|---|
| `HPAutoCad.McpBridge.Loader` | 1 ✅ | The DLL AutoCAD loads: `IExtensionApplication`, the `HPMCP*` commands, the Ribbon tab (`Ribbon/`, Autodesk.Windows), an isolated `AssemblyLoadContext` for the real bridge |
| `HPAutoCad.McpBridge` | 1–2 ✅ | The bridge: Roslyn + self-check, `MainThreadExecutor`, `AutocadScriptRunner` (lock + outer/inner transaction, dryRun, timeout), context reader, serializer, XAML status window |
| `tools/harness/` | 2–5 ✅ | Unattended harnesses (Python + PowerShell): `run-bridge-unattended.ps1` (pipe, 21 scenarios), `run-server-smoke.ps1` (published exe over stdio, 22 steps incl. every seed), `run-live-verify.ps1` (phase-5 proof on one stdio session: matrix, seeds, registry loops, Revit beside, isolation), shared SECURELOAD/UIA/COM helpers |
| `HPAutoCad.Mcp.Server` | 3–4 ✅ | The MCP server exe (net10, stdio): `AutocadHostProfile`, `execute_autocad_code`, `get_autocad_context`, `autocad://` resources, prompts, 12 embedded seed tools (`Registry/SeedLibrary/`) |
| `HPAutoCad.Mcp.Server.Tests` | 3–4 ✅ | xUnit v3 (46): profile, tool surface, tools over a real pipe, every seed compile-checked against `AutoCAD.NET` 25.1.0 from the NuGet cache (no AutoCAD needed) |

Shared engine (referenced, never copied): `../McpShared/HPRebar.Mcp.Contracts`, `../McpShared/HPRebar.McpBridge.Core`,
`../McpShared/HPRebar.Mcp.Server.Core`. This folder never references `../HPRebar/`.

## Ribbon tab "MCP AutoCAD"

Built by the loader with `Autodesk.Windows` (AdWindows.dll from the `AutoCAD.NET` package — compile-time only, never
copied). Tab id `HPAUTOCAD_MCP_TAB`; created once the Ribbon exists, re-created after a workspace switch, guarded by
`FindTab` so it never duplicates; removed on `Terminate`. Every button forwards to a bridge entry point — the same
delegates the `HPMCP*` commands call — so a click needs no drawing, never edits one, and works while a command is
waiting for input. Icons are vector drawings in code. No CUIx, no change to the user's `acad.cuix` or workspaces.

| Panel | Nút | Handler → bridge entry point | Command tương đương |
|---|---|---|---|
| Kết nối | Bảng điều khiển | `BridgeActions.Run("show")` → status window | `HPMCPBRIDGE` |
| Kết nối | Bật listener / Tắt listener | `Run("start")` / `Run("stop")` → `McpBridgeHost.Start/Stop` | `HPMCPSTART` / `HPMCPSTOP` |
| Kết nối | Trạng thái | `Run("status")` → command line, or an alert when no drawing is open | `HPMCPSTATUS` |
| Kết nối | (label) | `status.subscribe` → `McpBridgeHost.StateChanged`; text distinguishes bridge ready · listening · server connected (+ busy/error) and the opt-in | — |
| Công cụ | Sao chép script cuối | `copyLastScript` → clipboard ← `McpBridgeHost.LastRun.Source` | (window button) |
| Công cụ | Thư viện tool | `path("library")` → `%AppData%\HPAutoCad\McpServer\tools-library` in Explorer | — |
| Thiết lập | Mở nhật ký | `%LocalAppData%\HPAutoCad\McpBridge\logs` in Explorer | — |
| Thiết lập | Mở audit | `path("audit")` → `%AppData%\HPAutoCad\McpBridge\audit` | (window button) |
| Thiết lập | Tự khởi động listener (toggle) | `autoStart.get/set` → `settings.json` `AutoStartListener` | (window checkbox) |
| Thiết lập | Hướng dẫn | opens `Contents\README.md` shipped in the bundle | — |

Not on the Ribbon, on purpose: the "Allow AI code execution" opt-in (stays in the window, off on every start) and any
"run a tool" button (tools run through the MCP server, which Claude Code owns — the bridge never starts or stops it).
When the bridge failed to start, the bridge-backed buttons are disabled and their tooltip says to open the log.

Install / update / remove: `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug` with AutoCAD closed deploys the bundle
(`%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.McpBridge.bundle\`, autoloaded by `PackageContents.xml`,
`Platform="AutoCAD"`, R25.1); removing that folder uninstalls. Trusted location = the bundle folder (answer *Always
Load* once; SECURELOAD stays on). To try a build without the bundle: `NETLOAD` `Contents\HPAutoCad.McpBridge.Loader.dll`
from a copy of the bundle. Live check: `pwsh HPAutoCad/tools/harness/run-ribbon-check.ps1`.

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
pwsh HPAutoCad/tools/harness/run-ribbon-check.ps1                 # live: the Ribbon tab (UIA: one tab, workspace switch, buttons → pipe/window)
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
