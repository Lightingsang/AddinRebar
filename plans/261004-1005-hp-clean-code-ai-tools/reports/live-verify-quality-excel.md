# Live verify — script-quality gate on excel (2026-10-04)

**Result: 5/5 PASS** (harness `McpShared/tools/live-verify-quality.py --host excel`, started 2026-10-04 13:31:55).

| Item | Value |
|---|---|
| Host | Microsoft Excel 16.0 (16.0.20430.20118) |
| Document | new blank `Book1` (Excel started, Esc on the start screen; never saved — `docPath` is the bare name) |
| Build / deploy / launch | `dotnet build HPExcel/HPExcel.slnx -c Debug`; bridge app from `bin/Debug/net8.0-windows/`; opt-in + Attach by UIA (checkboxes have no accessible name — H-11) |
| Server | Debug `HPExcel.Mcp.Server.exe` (same source as the bridge), isolated registry under `%TEMP%\hp-live-quality\excel` (deleted) |
| Launcher | `reports/live-quality-launch/quality-excel.ps1` (Windows PowerShell 5.1) |

| Check | Result | Detail |
|---|---|---|
| 1 context: execution enabled, new or scratch document | PASS | get_excel_context: path='Book1' title='Book1' {"host": "excel", "hostVersion": "2026", "excel": {"isAttached": true, "attachedPid": 69628, "excelVersi |
| 2 commented-out code + empty catch refused (Q-B1, Q-B2) | PASS | quality Q-B1 1:1 commented-out code — delete it (git keeps history) or turn it into a comment that explains why. / quality Q-B2 3:18 empty catch witho |
| 3 vague name accepted with a Q-W3 warning | PASS | quality Q-W3 1:5 vague name 'data' — say what the value is in the domain. |
| 4 test_tool 2/2 (dryRun) | PASS | {"name": "mcp_verify_quality_good", "status": "tested", "passed": 2, "failed": 0, "cases": [{"title": "default", "success": true, "dryRun": true, "dur |
| 5 manual publish: pending_approval + review quality record | PASS | ## Code quality analysed: 0 error(s), 1 warning(s) - Q-W3 1:5 vague name 'data' — say what the value is in the domain.  ## Code ```csharp var data = a |
| no snapshot written by the read-only scripts | SKIP | %LOCALAPPDATA%/HPExcel/Snapshots does not exist yet |

Notes: the user's workbook had been closed but its Excel (pid 37248) stayed hidden with **0 workbooks** (checked over COM); `Quit` did not end it, so it was stopped — no document was open. `test_tool` dryRun is a static preview on this host ("Tier ReadOnly script compiled cleanly without execution"). First run FAIL: the harness did not treat Excel's bare `Book1` as unsaved — fixed in the harness. Snapshot folder absent → SKIP.

Raw result: `live-quality/live-verify-quality-excel.json`.
