# Live — beam Rebar Cover from J9 b (Pick Elements), 2026-10-06

Contract (user 2026-10-06): J9 `a/b` → beam cover = b like Revit Cover tool "Pick Elements" (Top/Bottom/Other + every exposed face, faces set by Pick Faces overwritten); missing type → create `Rebar Cover <b>mm`; one Undo with the run.

Code: [KataHostCoverService.cs](../../../HPRebar/HPRebar/KataRebar/Service/KataHostCoverService.cs) (called by `KataRebarOrchestrator` with `plan.Rules.StirrupCover`). Rule R-138 in [kata-beam-rebar-rules.md](../../../docs/specs/kata-beam-rebar-rules.md).

Setup: Revit 2026 pid 45160 on copy `b01-test.rvt` (from `Dam kata test.rvt`), B03 = 9795052/54/59/61, MCP bridge opt-in ticked by user in the copy. Excel run 2 on scratch copy `kataB03-cover-test.xlsm` (closed unsaved, KataB03.xlsm re-activated).

| Step | Before | After | Result |
|---|---|---|---|
| Run 1, KataB03 J9 30/25 | 052: one face pre-set to 40mm by MCP; 061: Bottom/Other 30, faces 30×4 + 25×1 | all 4 beams: params + all faces `Rebar Cover 25mm` (052 3 faces, 061 5 faces) | ✅ |
| Run 2, copy J9 30/28 | no 28 mm type | log `created rebar cover type Rebar Cover 28mm`; all params + faces 28 | ✅ |
| Ctrl+Z ×1 | — | all back to 25, type 28 gone, 112 rebar | ✅ |

Note: J9 30/30 of the contract would not create a type here — the copy already had `Rebar Cover 30mm` (id 9061845) — so 30/28 exercised the creation path.

Fix after live: log "N faces changed" said 3 for 7 differing places (faces counted after the parameters had already carried them) → faces counted first. Log only; build R26 OK, not re-run live.

Top parameter reads empty on 052/054/059: top face not exposed (slab joined), Revit keeps no Top cover there.
