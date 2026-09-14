# PM status — AutoCAD MCP bridge, phase 3 closed (2026-09-14)

## Plan sync-back (all phase files swept)

| Phase | File | Todos | Status |
|---|---|---|---|
| 0 | phase-00 | 8/8 | done |
| 1 | phase-01 | 7/7 | done |
| 2 | phase-02 | 9/9 | done |
| 3 | phase-03 | 6/6 | **done** — exe over stdio verified with AutoCAD 2026 (7/7 ×2 lead, ×1 tester), two exes 34/12; review 8/10, all findings resolved (d9818f4, 6df7cca, a70feee) |
| 4 | phase-04 | 0 | planned — ready; inherits: engine meta-tool descriptions still say "Revit"/`execute_revit_code` (host-neutral wording or profile-driven text), `get_run` record field `revitVersion` |
| 5 | phase-05 | 0 | planned; manual checks carried from phase 2 (modal dialog while waiting, ESC-then-retry) |

`plan.md`: 4/6 phases done; `status: in-progress`.

## Evidence

- Build: `HPAutoCad.slnx` (4 projects + shared) Debug/Release 0 warnings; `HPRebar.slnx Debug.R26` 0 errors.
- Tests: AutoCAD server 8/8, McpShared 89/89, HPRebar MCP 106/106 (203 total).
- Live: `tools/harness/run-server-smoke.ps1` 7/7 (initialize `HPAutoCad MCP`, 12 tools, context without Revit fields, none read, dryRun, real run with runId + hint, get_run); Revit exe 34 tools unchanged with Revit 2026 running.
- Reports: `phase-03-server-smoke-two-exes.md`, `phase-03-test-report.md`, `phase-03-code-review.md` (+ Resolution), `phase-03-tools-list-{autocad,revit}.json`.

## Decisions

- `revitVersion`/`isFamily` hidden for non-Revit hosts in `ContextService` (wire unchanged; Revit output byte-identical, regression test).
- `IsModifiable` meaning documented per host (Contracts XML comment + descriptions).
- Description budget: 1 717 chars (test cap 1 800) — the AutoCAD contract is longer than Revit's by design.
- Harnesses (`harness-common.ps1`, `run-server-smoke.ps1`, `mcp-call.py`) live in the repo; they refuse to run while another AutoCAD is open.

## Open (not blocking)

- `.mcp.json` entry `hprebar-autocad` is the user's to add (untracked file); snippet in `HPAutoCad/README.md` and the smoke report.
- Deployed Revit exe under `HPRebar/output/` predates phase 0 (server name `HPRebar.Mcp.Server`); republishing is optional.

## Next

`/bs:cook plans/260913-0000-autocad-mcp-bridge-2026/phase-04-*.md` — user confirmation required.
