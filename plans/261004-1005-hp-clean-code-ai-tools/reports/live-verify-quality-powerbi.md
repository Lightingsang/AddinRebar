# Live verify — script-quality gate on powerbi (2026-10-04)

**Result: 5/5 PASS** (harness `McpShared/tools/live-verify-quality.py --host powerbi`, started 2026-10-04 12:49:53).

| Item | Value |
|---|---|
| Host | Power BI Desktop 2.144.1378.0 (25.06) |
| Document | new blank report (`Untitled`) |
| Build / deploy / launch | `dotnet build HPPowerBi/HPPowerBi.slnx -c Debug`; bridge app from `bin/Debug/net8.0-windows/`; opt-in by UIA; `Connect` pressed without selecting the listed instance, so the bridge stayed **not connected** to the model |
| Server | Debug `HPPowerBi.Mcp.Server.exe` (same source as the bridge), isolated registry under `%TEMP%\hp-live-quality\powerbi` (deleted) |
| Launcher | `reports/live-quality-launch/quality-powerbi.ps1` (Windows PowerShell 5.1) |

| Check | Result | Detail |
|---|---|---|
| 1 context: execution enabled, new or scratch document | PASS | get_powerbi_context: doc='' {"host": "Power BI", "hostVersion": "2026", "powerBi": {"isConnected": false, "mutationEnabled": false, "tableCount": 0, " |
| 2 commented-out code + empty catch refused (Q-B1, Q-B2) | PASS | quality Q-B1 1:1 commented-out code — delete it (git keeps history) or turn it into a comment that explains why. / quality Q-B2 3:18 empty catch witho |
| 3 vague name accepted with a Q-W3 warning | PASS | quality Q-W3 1:5 vague name 'data' — say what the value is in the domain. |
| 4 test_tool 2/2 (dryRun) | PASS | {"name": "mcp_verify_quality_good", "status": "tested", "passed": 2, "failed": 0, "cases": [{"title": "default", "success": true, "dryRun": true, "dur |
| 5 manual publish: pending_approval + review quality record | PASS | ## Code quality analysed: 0 error(s), 1 warning(s) - Q-W3 1:5 vague name 'data' — say what the value is in the domain.  ## Code ```csharp var data = a |
| no snapshot written by the read-only scripts | SKIP | %LOCALAPPDATA%/HPPowerBi/Snapshots does not exist yet |

Notes: the gate runs on the bridge's analyze path, which needs no model connection; `test_tool` dryRun is a static preview (script compiled, not executed). First attempt aborted in the wrapper (msmdsrv StartTime unreadable) before the harness ran — the agent's PBIDesktop was closed and the run repeated. Snapshot folder `%LOCALAPPDATA%/HPPowerBi/Snapshots` absent → SKIP.

Raw result: `live-quality/live-verify-quality-powerbi.json`.
