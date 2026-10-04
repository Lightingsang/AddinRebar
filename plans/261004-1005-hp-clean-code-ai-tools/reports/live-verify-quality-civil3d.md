# Live verify — script-quality gate on civil3d (2026-10-04)

**Result: 5/5 PASS** (harness `McpShared/tools/live-verify-quality.py --host civil3d`, started 2026-10-04 13:22:48).

| Item | Value |
|---|---|
| Host | Civil 3D 2026 (acad R25.1.74, `<<C3D_Metric>>`) |
| Document | new `Drawing1.dwg` (Meters, no path) |
| Build / deploy / launch | `dotnet build HPCivil3d/HPCivil3d.slnx -c Debug` → bundle deployed; started with the Civil 3D shortcut command line (no `/b`), SECURELOAD answered Load Once ×7 for this pid only (HPCivil3d harness helper), ribbon HPCivil3d ▸ MCP ▸ MCP Bridge, opt-in by UIA; closed by COM `Quit` |
| Server | Debug `HPCivil3d.Mcp.Server.exe` (same source as the bridge), isolated registry under `%TEMP%\hp-live-quality\civil3d` (deleted) |
| Launcher | `reports/live-quality-launch/quality-acad.ps1 -Product civil3d` (Windows PowerShell 5.1) |

| Check | Result | Detail |
|---|---|---|
| 1 context: execution enabled, new or scratch document | PASS | get_civil3d_context: path='' title='Drawing1.dwg' {"host": "civil3d", "hostVersion": "2026", "autocad": {"insunits": "Meters", "measurement": "Metric" |
| 2 commented-out code + empty catch refused (Q-B1, Q-B2) | PASS | quality Q-B1 1:1 commented-out code — delete it (git keeps history) or turn it into a comment that explains why. / quality Q-B2 3:18 empty catch witho |
| 3 vague name accepted with a Q-W3 warning | PASS | quality Q-W3 1:5 vague name 'data' — say what the value is in the domain. |
| 4 test_tool 2/2 (dryRun) | PASS | {"name": "mcp_verify_quality_good", "status": "tested", "passed": 2, "failed": 0, "cases": [{"title": "default", "success": true, "dryRun": true, "dur |
| 5 manual publish: pending_approval + review quality record | PASS | ## Code quality analysed: 0 error(s), 1 warning(s) - Q-W3 1:5 vague name 'data' — say what the value is in the domain.  ## Code ```csharp var data = a |

Notes: first attempt with `/b` failed like AutoCAD (H-10).

Raw result: `live-quality/live-verify-quality-civil3d.json`.
