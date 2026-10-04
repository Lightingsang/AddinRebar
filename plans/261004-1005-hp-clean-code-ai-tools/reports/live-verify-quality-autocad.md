# Live verify — script-quality gate on autocad (2026-10-04)

**Result: 5/5 PASS** (harness `McpShared/tools/live-verify-quality.py --host autocad`, started 2026-10-04 13:15:54).

| Item | Value |
|---|---|
| Host | AutoCAD 2026 (R25.1.74) |
| Document | new `Drawing1.dwg` from the default template (no path) |
| Build / deploy / launch | `dotnet build HPAutoCad/HPAutoCad.slnx -c Debug` with AutoCAD closed → unified bundle deployed; AutoCAD started with the **shortcut command line** (`/product ACAD /language en-US`), ribbon HPAutoCad ▸ MCP ▸ MCP Bridge by UIA, opt-in `AllowExecution` by UIA; closed by COM `Quit` (drawing discarded) |
| Server | Debug `HPAutoCad.Mcp.Server.exe` (same source as the bridge), isolated registry under `%TEMP%\hp-live-quality\autocad` (deleted) |
| Launcher | `reports/live-quality-launch/quality-acad.ps1` (Windows PowerShell 5.1) |

| Check | Result | Detail |
|---|---|---|
| 1 context: execution enabled, new or scratch document | PASS | get_autocad_context: path='' title='Drawing1.dwg' {"host": "autocad", "hostVersion": "2026", "autocad": {"insunits": "Inches", "measurement": "English |
| 2 commented-out code + empty catch refused (Q-B1, Q-B2) | PASS | quality Q-B1 1:1 commented-out code — delete it (git keeps history) or turn it into a comment that explains why. / quality Q-B2 3:18 empty catch witho |
| 3 vague name accepted with a Q-W3 warning | PASS | quality Q-W3 1:5 vague name 'data' — say what the value is in the domain. |
| 4 test_tool 2/2 (dryRun) | PASS | {"name": "mcp_verify_quality_good", "status": "tested", "passed": 2, "failed": 0, "cases": [{"title": "default", "success": true, "dryRun": true, "dur |
| 5 manual publish: pending_approval + review quality record | PASS | ## Code quality analysed: 0 error(s), 1 warning(s) - Q-W3 1:5 vague name 'data' — say what the value is in the domain.  ## Code ```csharp var data = a |

Notes: 4 attempts FAIL (environment) with the harness's `/nologo /b bridge.scr` start: the loader never added the MCP ribbon panel, `HPMCPBRIDGE` from the script/typed opened nothing and COM `GetActiveObject` answered MK_E_UNAVAILABLE (CadAddinManager is installed) — H-10. The HPAutoCad harness answers SECURELOAD with **Always Load**; the wrapper overrode it to Load Once (H-09). One more FAIL was the harness reading the title `Drawing1.dwg` as a path — fixed in the harness (an unsaved document has no `docPath`).

Raw result: `live-quality/live-verify-quality-autocad.json`.
