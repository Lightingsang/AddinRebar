# Live verify — script-quality gate on revit (2026-10-04)

**Result: 5/5 PASS** (harness `McpShared/tools/live-verify-quality.py --host revit`, started 2026-10-04 12:33:55).

| Item | Value |
|---|---|
| Host | Revit 2026.4 (26.4.0.32) |
| Document | agent's second Revit (pid 79908) on `HPRebar/output/golden/fixture-build/scratch.rte` |
| Build / deploy / launch | HPRebar.McpBridge Debug.R26 deployed 11:52 (phase 2); bridge window + opt-in by UIA (`revit-open-bridge-window.ps1`, `revit-toggle-optin.ps1`) |
| Server | Debug `HPRebar.Mcp.Server.exe` (same source as the bridge), isolated registry under `%TEMP%\hp-live-quality\revit` (deleted) |

| Check | Result | Detail |
|---|---|---|
| 1 context: execution enabled, new or scratch document | PASS | get_revit_context: doc='F:\\1-CONG VIEC\\05-AI\\01_Revit\\02_Csharp\\AddinRebar\\HPRebar\\output\\golden\\fixture-build\\scratch.rte' {"revitVersion": |
| 2 commented-out code + empty catch refused (Q-B1, Q-B2) | PASS | quality Q-B1 1:1 commented-out code — delete it (git keeps history) or turn it into a comment that explains why. / quality Q-B2 3:18 empty catch witho |
| 3 vague name accepted with a Q-W3 warning | PASS | quality Q-W3 1:5 vague name 'data' — say what the value is in the domain. |
| 4 test_tool 2/2 (dryRun) | PASS | {"name": "mcp_verify_quality_good", "status": "tested", "passed": 2, "failed": 0, "cases": [{"title": "default", "success": true, "dryRun": true, "dur |
| 5 manual publish: pending_approval + review quality record | PASS | ## Code quality analysed: 0 error(s), 1 warning(s) - Q-W3 1:5 vague name 'data' — say what the value is in the domain.  ## Code ```csharp var data = a |

Notes: same five checks as the phase-2 script — the shared harness reproduces them.

Raw result: `live-quality/live-verify-quality-revit.json`.
