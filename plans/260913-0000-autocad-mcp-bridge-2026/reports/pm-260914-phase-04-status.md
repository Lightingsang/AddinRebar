# PM status — AutoCAD MCP bridge, phase 4 closed (2026-09-14)

## Plan sync-back (all phase files swept)

| Phase | File | Todos | Status |
|---|---|---|---|
| 0 | phase-00 | 8/8 | done |
| 1 | phase-01 | 7/7 | done |
| 2 | phase-02 | 9/9 | done |
| 3 | phase-03 | 6/6 | done |
| 4 | phase-04 | 7/7 | **done** — registry engine per `IHostProfile` (validator/lifecycle/registrar/toolify/CLI, meta-tool wording host-neutral), 12 AutoCAD seeds embedded + compile-checked (46 tests, 0 skip), all 12 run live through stdio with AutoCAD 2026 (smoke 21/21); review 7.5/10 → 12 actionable findings fixed same day (`propose_tool` categories, exe name per profile via `IHostProfile.CliExecutable`, JSON escapes, seed hardening, CLI import host fence), re-verified live 22/22 |
| 5 | phase-05 | 0 | planned; inherits: `insert_block` with a real block, FTS name boost (`search_tools "layer"` ranks `list_layers` outside top 5), `get_run` field `revitVersion` (schema name kept; alias if the AI trips), manual checks from phase 2 (modal dialog while waiting, ESC-then-retry) |

`plan.md`: 5/6 phases done; `status: in-progress`.

## Evidence

- Build: `HPAutoCad.slnx` Debug/Release 0 warnings; `McpShared.slnx` 0 errors; `HPRebar.slnx Debug.R26` 0 errors.
- Tests: McpShared 95/95, HPRebar MCP 106/106, AutoCAD server 58/58 (259 total). Tester re-ran every gate independently (`phase-04-test-report.md`): 25/25, 0 skipped, smoke 21/21 with no `.NET Runtime 1026` event.
- Live: `tools/harness/run-server-smoke.ps1` 22/22 — `tools/list` 24, `search_tools` (FTS + category filter), 12 seeds by name (`create_layer MCP-TEST` → `draw_polyline` 15 000 mm → `draw_circle dryRun` → `add_text` MText → `add_linear_dimension` 3 000 mm → `insert_block` unknown-block error → `get_entities`); `%AppData%\HPAutoCad\McpServer\tools-library` 12 folders + `_seeds.json`; `registry stats` prints `host: autocad (AutoCAD)`.
- Revit: `tools/list` 34, names + input schemas + `required` identical to phase 0 (`phase-04-tools-list-revit.json` vs `phase-00-tools-list-after.json`); description text of 8 engine tools (`get_run`, `inspect_type`, `search_tools`, `cancel_execution`, `run_tool`, `propose_tool`, `test_tool`, `publish_tool`) + `inspect_type` title changed to host-neutral wording — intentional, called out; `%AppData%\HPRebar\McpServer\tools-library` untouched.
- Reports: `phase-04-seeds-registry.md`, `phase-04-test-report.md`, `phase-04-code-review.md`, `phase-04-tools-list-{autocad,revit}.json`.

## Decisions

- Meta-tool `[Description]` attributes are static → one host-neutral wording for both exes ("the host application", "the execute tool (execute_revit_code / execute_autocad_code)"); the profile-driven parts (`DynamicToolRegistrar` description, `ToolifyPrompts` rules, `search_tools` hint) use `manager.Profile`.
- `ToolValidator.Validate(..., IHostProfile)` is additive; the 4-arg overload stays the Revit behaviour (test-locked).
- Seed points are `{x, y}` objects in mm (not `[x, y]`): `ScriptArgs.List` yields `ScriptArgs` per element, `.Double("x")` reads directly.
- Seeds never create a missing layer — clear error "run create_layer first"; only the `autocad_modify_template` prompt auto-creates.
- No seed opens a transaction (`UsesTransaction == false` asserted); `tr` is the bridge's.
- `IHostProfile.CliExecutable` names the exe in every human-facing approve instruction (message, `_review/*.md`, CLI usage); Revit keeps `HPRebar.Mcp.Server.exe` verbatim.

## Open (not blocking)

- FTS ranking: single-word queries rank by description hits; name boost is an engine follow-up (both hosts).
- `insert_block` verified only for the unknown-block error path (blank drawing); real block → phase 5.
- `.mcp.json` entry `hprebar-autocad` is the user's to add; deployed Revit exe under `HPRebar/output/` predates phase 0 (republish optional).

## Next

`/bs:cook plans/260913-0000-autocad-mcp-bridge-2026/phase-05-verify-live-autocad-registry-loop-and-docs.md` — user confirmation required.
