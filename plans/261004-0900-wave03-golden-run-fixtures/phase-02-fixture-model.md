# Phase 2 — Fixture model generator

## Context
- `HPRebar/HPRebar.Tests/Fixtures/README.md` (TUnit spec: levels, 6 marked columns, foundation, beam, shapes `M_T1`/`M_T3`).
- `ColumnStackFixture.cs:14` hard-codes `column-stack-2-storey.rvt` → keep the name (renaming = code change).
- Template: `C:\ProgramData\Autodesk\RVT 2026\Templates\English\Structural Analysis-DefaultMetric.rte` (7.3 MB, OOTB — no company template in git).
- Families: `...\Libraries\English\US\Structural Columns\Concrete\M_Concrete-Rectangular-Column.rfa`, `Structural Framing\Concrete\M_Concrete-Rectangular Beam.rfa`, `Structural Rebar Shapes\M_T1|M_T3|M_10.rfa` (no `M_T10` anywhere → beam falls back to `M_10` per `BeamRebar/Service/RebarShapeResolver.cs:17`; column cross-ties stay uncovered — default spec has none).

## Overview
P1 · planned · additive files only. Generated, not hand-built, so a fixture can be rebuilt and reviewed.

## Model content (all marks = `ALL_MODEL_MARK`)
| Area (origin) | Content | Golden use |
|---|---|---|
| A (0,0) | README exactly: levels Foundation 0 / Level 1 3000 / Level 2 6000; `C1-LOWER`, `C1-UPPER`, `C-SLANTED`, `C2-DETACHED`, `C3-LOWER`, `C3-WIDER`; structural foundation under C1/C-SLANTED/C3 joined; beam `B-C1` 300×500 into C1-LOWER head, top flush | Column: `C1-LOWER`+`C1-UPPER`; TUnit |
| B (x +20 m) | columns `BR-C1..BR-C3` 400×400 Foundation→Level 1 at 0/6000/11000; beams on Level 1 300×600 `BR-B1` (C1→C2), `BR-B2` (C2→C3), `BR-CANT` C3 → +1800 free end; secondary `BR-SEC` 250×450 into mid-span of BR-B1, perpendicular, 3000 long, own end column `BR-C4` | Beam: `BR-B1,BR-B2,BR-CANT` |
| C (y +20 m) | Floors-category structural slab `FND-SLAB` 6000×4000×600 at Foundation level (Foundation feature needs `OST_Floors`) | Foundation |

Bar-type catalog fixed by the script: delete template bar types, create `D16, D10, D25, D12, D20, D8` **in that order** (element-id order ≠ diameter order, so id/index confusions in Wave 4 show up). Cover types left as template.

## Generator
`HPRebar/tools/golden-run/scripts/build-fixture.csx` (`.csx` so no csproj picks it up), run once through `execute_revit_code` `transaction: manual`:
1. `app.NewProjectDocument(args.Str("template"))` (background doc; active doc = any scratch doc in the test Revit).
2. One `Transaction` on the new doc: levels, load families (`LoadFamily(path)`), types sized by instance/type params, elements, joins (`JoinGeometryUtils`, column cuts beam), bar types.
3. Assert every mark exists, the 3 shapes load, 6 bar types — else throw (`InvalidOperationException` with list).
4. `SaveAs(args.Str("out"), Compact=true, MaximumBackups=1)` into the run dir, `Close(false)`.
5. Copy only the `.rvt` into `HPRebar/HPRebar.Tests/Fixtures/`; record sha256 + Revit build in `Fixtures/golden/fixture-meta.json`.

## Related files
Create: `tools/golden-run/scripts/build-fixture.csx`, `Fixtures/column-stack-2-storey.rvt` (LFS), `Fixtures/golden/fixture-meta.json`. Modify: `Fixtures/README.md` (generated-by note + areas B/C).

## Steps
1. Write csx; dry iterate in the test Revit (phase 3 launch steps reused manually).
2. Open the saved model read-only via snapshot script → marks, levels, catalog as expected.
3. Optional proof: `dotnet run --project HPRebar.Tests -c Debug.R26` → 21 tests run not skip (Nice3point.TUnit.Revit runner behaviour GIẢ ĐỊNH CHƯA XÁC MINH; not a gate).
4. Commit `test(fixtures): add generated column/beam/foundation fixture model` (LFS, ~8-10 MB, no push).

## Success criteria
Model opens in Revit 2026 without warnings dialog; all marks present; each of the 3 features opens its window on its selection without a validation error (checked in phase 3).

## Risks
- Feature refuses a fixture shape (e.g. cantilever, offset upper column) → adjust geometry, or if the add-in is wrong log a B-xx (never fix in this wave).
- README geometry subtleties (C1-UPPER 50 mm offset, beam top flush) → assert in script by bounding boxes.
- Fixture tied to Revit 2026.x build; `fixture-meta.json` records it. R25 TUnit config cannot open it (accepted).
- Rollback: delete the files; nothing else references them except TUnit (which skips again).
