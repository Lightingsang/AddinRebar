# Live verify — script-quality gate on Revit 2026 (2026-10-04)

Env: Revit 2026.4 (26.4.0.32) second instance on `HPRebar/output/golden/fixture-build/scratch.rte`; HPRebar.McpBridge Debug.R26 deployed 11:52 with the new Bridge.Core; server = Debug `HPRebar.Mcp.Server.exe` (same source); isolated registry `reports/live-registry/` (deleted after the run). Script: `live-verify-quality.py`.

| # | Check | Result |
|---|---|---|
| 1 | context: scratch doc under run folder, execution enabled | PASS |
| 2 | propose script with `// var old = …;` + `catch { }` → refused, errors `quality Q-B1 1:1`, `quality Q-B2 3:18` | PASS |
| 3 | propose `var data = …` → accepted, warning `quality Q-W3 1:5` | PASS |
| 4 | test_tool → 2/2 dryRun in Revit | PASS |
| 5 | publish_tool (manual) → pending_approval; review file `## Code quality` / `analysed: 0 error(s), 1 warning(s)` / `- Q-W3 1:5 …` | PASS |

5/5 PASS. Not run live: auto policy and the "quality not analysed" path (unit-tested in `ToolLifecycleTests`); the 9 other hosts (bridges not redeployed — CHƯA TEST).
