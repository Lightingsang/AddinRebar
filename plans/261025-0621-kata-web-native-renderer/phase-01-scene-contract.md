# Phase 1 — Scene contract + mapper (Core)

**Context:** `HPRebar.Core/KataExport/Models/KataElevationModels.cs`, `HPRebar.Core/KataRebar/Models/{KataElevationDrawing,KataSectionDrawing}.cs`, `KataRebar/Calculators/{KataBarTagBuilder,KataSectionCuts}.cs`, `HPRebar/KataExport/View/Controls/KataRebarDrawing.cs`, `View/Web/KataWebContracts.cs`.

## Requirements
- Wire records (camelCase, numbers in mm, tuples → `{x,z}`), built by a pure `KataWebSceneMapper` in `HPRebar.Core/KataExport/Web/` (netstandard2.0, xUnit-testable).
- Event `kata.scene` (revision, elevation, rebar? {lines[pen, points, keys], dims, flags, bubbles, level, title, extents, tags, cuts}, stationMap) sent when elevation / plan / station map change — not on zoom.
- Command `kata.section {number, mirror}` → `KataSectionDrawing` DTO (bulge vertices, bars, leaders, marks, tags) or null.
- Commands `kata.removeBars {keys[]}` (VM removal entry point already used by canvas Delete) and existing `undoRemoveBars` action.
- Size guard: log scene bytes; fail loudly if > 2 MB (real beam run expected < 200 KB).

## Steps
1. Read `KataExportViewModel.Removal.cs` + `KataElevationCanvas.Edit.cs` to find the VM call the canvas Delete uses; expose it as a command.
2. DTO records + mapper + tests (pens/enums as strings, rounding to 0.1 mm).
3. Core test writes `HPBimToolkit.Frontend/src/kata/__fixtures__/scene-*.json` from a real plan fixture already used by KataRebar tests (opt-in env like `HPBIM_UPDATE_CONTRACTS`).
4. Bridge: emit `kata.scene` (coalesced), keep `kata.drawing` alive until phase 6.
5. Extend `kataContracts.test.ts` drift guard to the new records.

## Success
Core tests green; fixture JSON committed; bridge builds R26; drift test green.
