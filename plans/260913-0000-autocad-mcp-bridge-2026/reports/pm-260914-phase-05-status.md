# PM status — AutoCAD MCP bridge, phase 5 closed → plan complete (2026-09-14)

## Plan sync-back (all phase files swept)

| Phase | File | Todos | Status |
|---|---|---|---|
| 0 | phase-00 | 8/8 | done |
| 1 | phase-01 | 7/7 | done |
| 2 | phase-02 | 9/9 | done |
| 3 | phase-03 | 6/6 | done |
| 4 | phase-04 | 7/7 | done |
| 5 | phase-05 | 10/10 | **done** — live loop proven on one stdio session with AutoCAD 2026: matrix 18, seeds 18, MISS→approve→HIT 13, quarantine→restore 7, Revit beside 5 (+1 skip), isolation 4; two engine defects found live and fixed; review 7.5/10 → 16 findings fixed same day (hand-edited tool.json restarts the window, events index, harness on an isolated registry, pid-scoped cleanup, hard timeouts), live run 3 clean |

`plan.md`: 6/6 phases done; `status: completed`. ADR-02/03/04/05/06 Accepted (01 superseded), each with its "verified by" report.

## Evidence

- Build: `McpShared.slnx` 0 errors; `HPRebar.slnx Debug.R26` 0 errors; `HPAutoCad.slnx` Release 0 warnings.
- Tests: McpShared 96/96, HPRebar MCP 109/109, AutoCAD server 58/58 (263 total). Tester re-ran every gate independently incl. the live harness and isolation (`phase-05-test-report.md`): 65/65 + 4/4, only `mcp_verify_count_block_refs` v1 quarantined, no `.NET Runtime 1026`, no AutoCAD left.
- Live (`reports/phase-05-live-verify.md`, `run-live-verify.ps1`): run 2 **65/65** (1 skip: Revit opt-in off), isolation **4/4**, bridge harness regression 21/21; approve → `tools/list_changed` in 0.5 s; busy → ESC posted → retry works; second AutoCAD fails fast naming AutoCAD; Civil 3D never loads the bundle; both pipes coexist; Revit library hash unchanged.
- Registry state left on the dev machine: 14 published (12 seeds + `move_text_between_layers` + `mcp_verify_count_block_refs` v2), 0 quarantined.

## Decisions (phase 5)

- **Stability window (engine, both hosts):** runs whose error starts with `Argument…Exception:` are the caller's and never count; the window restarts at the last `approved`/`published`/`restore`/`proposed_version`/`imported`/`status_changed` event (the last one is written when a status edited by hand in tool.json is reloaded). Found live: the phase-4 smoke's deliberate `insert_block NO-SUCH-BLOCK` calls had quarantined the seed, and a restored tool was re-quarantined by its old failures. Behaviour change for Revit too (intentional, tested).
- Quarantine demo uses an unguarded tool (`eKeyNotFound`) — that is what "fragile" means now; the fix is the guard (`ArgumentException`) via `propose_tool newVersion=true`.
- Harness keeps one stdio session (`mcp-session.py`) because `notifications/tools/list_changed` only reaches a running server; it runs on an isolated registry root under `output/live-verify/` (the user's `%AppData%` registry is never written) and kills only the processes it started.
- Revit exe for E = Debug build: `HPRebar/output/HPRebar.Mcp.Server.exe` is locked by this session's running `hprebar-revit` servers.

## Open (not blocking)

- Revit seed + execute over the new engine with the opt-in on (E skip) — user ticks the box in Revit, then `live-verify.py --only e`.
- Republish `HPRebar/output/HPRebar.Mcp.Server.exe` after restarting the `hprebar-revit` MCP (Claude Code still runs the 2026-09-12 exe).
- `.mcp.json` entry `hprebar-autocad` (user).
- Not verified: modal dialog while waiting (same `IsQuiescent` branch as busy), Revit 2025, AutoCAD 2026 Update 1.2 (.NET 10), `test_tool realRun=true`, FTS name boost.

## Next

Plan complete. Follow-ups above are optional; a new plan is needed for AutoCAD 2025/.NET 10 support or an AutoCAD `pack` pipeline.
