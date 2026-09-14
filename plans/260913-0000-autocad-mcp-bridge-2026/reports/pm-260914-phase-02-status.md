# PM status — AutoCAD MCP bridge, phase 2 closed (2026-09-14)

## Plan sync-back (all phase files swept)

| Phase | File | Todos | Status |
|---|---|---|---|
| 0 | phase-00 | 8/8 | done (598da4c) |
| 1 | phase-01 | 7/7 | done (cc1868b, bae2fd2) |
| 2 | phase-02 | 9/9 | **done** — built, tested, verified live twice (lead run 10/11, tester independent run); review 7.5/10 → 16/17 resolved, #11 carried to phase 3 (150a95a + 4322411) |
| 3 | phase-03 | 0 | planned — ready; description text updated (no StartTransaction, undo merged, `isModifiable` meaning) |
| 4–5 | phase-04/05 | 0 | planned; phase 5 inherits manual checks: modal dialog while a request waits, ESC-then-retry |

`plan.md`: 3/6 phases done; `status: in-progress`.

## Evidence

- Build: `HPAutoCad.slnx` Debug/Release 0 warnings; `HPRebar.slnx Debug.R26` 0 errors (Core additive).
- Tests: McpShared 88/88 (70 → 88: queue 10, insunits 2, dispatcher code 1, guard transactions/lock 1 + existing), HPRebar MCP 106/106.
- Live: harness `HPAutoCad/tools/harness/` 21/21 (opt-in off → -32001, 18 execute/context scenarios, no drawing → -32003, busy → -32002 after 8 s); 0 `.NET Runtime 1026` events on the final runs; audit 234+ lines; status window opened in the isolated ALC.
- Reports: `phase-02-bridge-runtime.md`, `phase-02-test-report.md`, `phase-02-code-review.md` (+ Resolution).

## Decisions recorded (ADR-03 Accepted, revised by live evidence)

- Two bridge-owned transactions through `doc.TransactionManager` (outer = group, inner = `tr`, inner committed first because database events only fire on the outermost commit).
- Scripts may not start transactions or locks (guard) — an undisposed wrapper crashes acad.exe at GC time; `manual` runs like `auto`.
- Change counts via HANDSEED + ObjectOpenedForModify + ObjectId.IsErased; approximate, documented.
- Undo merges consecutive application-context runs into one step per user command (named lock "HPMCP").
- Idle subscribed once + WM_NULL wake; busy grace 8 s bounds every wait, cancelled requests never start.

## Open (not blocking)

- Modal dialog while a request waits; ESC-then-retry — manual, phase 5.
- `IsModifiable` per-host meaning — phase 3 descriptions/Contracts comment.
- Per-run undo would need `ExecuteInCommandContextAsync` + `UNDO _BE/_E` — out of MVP.

## Next

`/bs:cook plans/260913-0000-autocad-mcp-bridge-2026/phase-03-server-autocad-host-tools-prompts-resources.md` — user confirmation required.
