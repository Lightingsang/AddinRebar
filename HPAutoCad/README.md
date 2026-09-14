# HPAutoCad — AutoCAD MCP bridge

Turns AutoCAD 2026 into a runtime for an AI agent, the way `HPRebar/` does for Revit: an MCP server exe
(stdio, launched by the host AI) talks over a named pipe to a plugin loaded inside `acad.exe` that
compiles and runs the C# the agent sends, inside one transaction, with opt-in, guard, timeout and audit.

Plan of record: [`../plans/260913-0000-autocad-mcp-bridge-2026/`](../plans/260913-0000-autocad-mcp-bridge-2026/plan.md)
(ADR-06 explains why this is a separate top-level folder and why the engine lives in `../McpShared/`).

## Status

Phase 3 done (2026-09-14): `HPAutoCad.Mcp.Server.exe` serves MCP over stdio — 12 tools (4 core + 8 registry),
`autocad://` resources, 2 prompts — and `execute_autocad_code` runs end-to-end through the bridge inside AutoCAD 2026
(verified with `tools/harness/run-server-smoke.ps1`, 7/7). Phase 2 gave the bridge itself: pipe listener, main-thread
executor (`Application.Idle` + `IsQuiescent`), two bridge-owned transactions (`tr` is the script's), change counting,
audit, status window (`tools/harness/run-bridge-unattended.ps1`, 21/21). No seed tools yet — phase 4.
AutoCAD commands: `HPMCPBRIDGE` (status window with the per-session "Allow AI code execution" opt-in), `HPMCPSTART`,
`HPMCPSTOP`, `HPMCPSTATUS`.

| Project | Phase | Purpose |
|---|---|---|
| `HPAutoCad.McpBridge.Loader` | 1 ✅ | The DLL AutoCAD loads: `IExtensionApplication`, the `HPMCP*` commands, an isolated `AssemblyLoadContext` for the real bridge |
| `HPAutoCad.McpBridge` | 1–2 ✅ | The bridge: Roslyn + self-check, `MainThreadExecutor`, `AutocadScriptRunner` (lock + outer/inner transaction, dryRun, timeout), context reader, serializer, XAML status window |
| `tools/harness/` | 2–3 ✅ | Unattended harnesses (Python + PowerShell): `run-bridge-unattended.ps1` (pipe, 21 scenarios), `run-server-smoke.ps1` (published exe over stdio, 7 steps), shared SECURELOAD/UIA/COM helpers |
| `HPAutoCad.Mcp.Server` | 3 ✅ | The MCP server exe (net10, stdio): `AutocadHostProfile`, `execute_autocad_code`, `get_autocad_context`, `autocad://` resources, prompts; seed tools arrive in phase 4 |
| `HPAutoCad.Mcp.Server.Tests` | 3 ✅ · 4 | xUnit v3: profile, tool surface, tools over a real pipe (8); seed compile checks against `AutoCAD.NET` 25.1.0 in phase 4 |

Shared engine (referenced, never copied): `../McpShared/HPRebar.Mcp.Contracts`, `../McpShared/HPRebar.McpBridge.Core`,
`../McpShared/HPRebar.Mcp.Server.Core`. This folder never references `../HPRebar/`.

## Target

AutoCAD 2026 base release (R25.1, .NET 8). `AutoCAD.NET` NuGet pinned to `[25.1.0]` — `25.1.1` and `26.0.0` are
the .NET 10 builds (2026 Update 1.2 / 2027) and do not load on the base release.

## Commands

```bash
dotnet build HPAutoCad/HPAutoCad.slnx -c Debug                     # deploys the bundle to %AppData%\Autodesk\ApplicationPlugins\
dotnet build HPAutoCad/HPAutoCad.slnx -c Debug -p:DeployBundle=false   # AutoCAD open (DLL locked)
cd HPAutoCad && dotnet test HPAutoCad.Mcp.Server.Tests            # 8 tests, no AutoCAD needed
pwsh HPAutoCad/tools/harness/run-bridge-unattended.ps1            # live: bridge over the pipe (AutoCAD must be closed)
pwsh HPAutoCad/tools/harness/run-server-smoke.ps1                 # live: published exe over stdio (publish first)
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
