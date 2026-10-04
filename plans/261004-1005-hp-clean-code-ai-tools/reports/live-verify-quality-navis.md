# Live verify — script-quality gate on navis (2026-10-04)

**Result: 5/5 PASS** (harness `McpShared/tools/live-verify-quality.py --host navis`, started 2026-10-04 12:40:08).

| Item | Value |
|---|---|
| Host | Navisworks Manage 2026 (23.0.1432.76) |
| Document | copy of the sample `gatehouse_pub.nwd` under `HPNavis/output/live-quality/` |
| Build / deploy / launch | `dotnet build HPNavis/HPNavis.slnx -c Debug` with Roamer closed → plugin deployed 12:34; Roamer started directly with the model and `HPNAVIS_MCP_BRIDGE_SHOW_WINDOW=1`; opt-in `AllowExecution` by UIA (HPNavis harness helpers) |
| Server | Debug `HPNavis.Mcp.Server.exe` (same source as the bridge), isolated registry under `%TEMP%\hp-live-quality\navis` (deleted) |
| Launcher | `reports/live-quality-launch/quality-navis.ps1` (Windows PowerShell 5.1) |

| Check | Result | Detail |
|---|---|---|
| 1 context: execution enabled, new or scratch document | PASS | get_navis_context: doc='F:\\1-CONG VIEC\\05-AI\\01_Revit\\02_Csharp\\AddinRebar\\HPNavis\\output\\live-quality\\gatehouse_pub.nwd' {"host": "navis", " |
| 2 commented-out code + empty catch refused (Q-B1, Q-B2) | PASS | quality Q-B1 1:1 commented-out code — delete it (git keeps history) or turn it into a comment that explains why. / quality Q-B2 3:18 empty catch witho |
| 3 vague name accepted with a Q-W3 warning | PASS | quality Q-W3 1:5 vague name 'data' — say what the value is in the domain. |
| 4 test_tool 2/2 (dryRun) | PASS | {"name": "mcp_verify_quality_good", "status": "tested", "passed": 2, "failed": 0, "cases": [{"title": "default", "success": true, "dryRun": true, "dur |
| 5 manual publish: pending_approval + review quality record | PASS | ## Code quality analysed: 0 error(s), 1 warning(s) - Q-W3 1:5 vague name 'data' — say what the value is in the domain.  ## Code ```csharp var data = a |

Notes: first attempt FAIL (environment): the `Reload last file?` recovery prompt left by an earlier killed Roamer came back after every WM_CLOSE (the HPNavis harness's `Close-RoamerDialogs`), the model never loaded and the bridge answered busy; answering **No** fixed it (H-12).

Raw result: `live-quality/live-verify-quality-navis.json`.
