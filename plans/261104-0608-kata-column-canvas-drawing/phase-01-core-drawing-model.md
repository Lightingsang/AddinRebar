# Phase 1 — Core drawing model + mapper

Overview: pure `HPRebar.Core/KataRebar/ColumnStack/KataColumnDrawing*.cs`; everything the canvas paints, in mm of the
column frame (x along b from the stack's left face, z absolute). Status: pending.

## VC layout (reports/variants/v00-base.json, read 2026-11-04)
- Elevation along b: concrete outline per storey + pile cap, level line + name + `kata_block_CT` (±0.000 m) per storey.
- `kata_thep chu` polylines: per storey 1 bar left (group A) + 1 right (group B) on the b faces; layer 2 drawn one
  slot inside when present (FLOOR 2: x 347 / 633). Starters bend out in the cap (foot F5·d).
- `kata_block_KHT` leaders: number + "20Ø25" per group per storey; hoop "Ø10a100".
- `kata_block_SECBAL` "1" both sides at the cut → section "1-1" (title `kata_block_TD` "1-1, TL 1/20").
- Dims: laps (30d), hoop zones, storey heights.

## Model
- `KataColumnDrawing(Elevation, Sections)`; `Sections` = existing `KataColumnSectionDrawing` + `Name` ("1-1" …).
- `KataColumnElevationDrawing(LeftMm, RightMm, BottomMm, TopMm, Outlines[], Levels[], Bars[], HoopZones[], Labels[],
  Dims = KataColumnElevationDims.Chains, Cuts[])`.
- Bar: `Number, Storey, Group A|B, Layer, DiameterMm, Kind (starter/main), Points[(x,z)], LengthMm`.
  Representative = per storey/group/layer the outermost bar on the left (A) / right (B), as the Revit elevation tags.
- `Cuts`: storey, z = `CutElevationMm`, name.

## Steps
1. Records + `KataColumnDrawingMapper.Map(spec, storeys, design)` (reuse section build, elevation dims).
2. `KataColumnRebarSummary.Drawing` (null on errors); mapper call in `KataColumnRebarSummaryMapper`.
3. Tests: sample (5 storeys) → 2 bars × storeys + layer-2 bars on FLOOR 2, starters in cap, labels "20Ø25",
   cuts inside storeys, one storey, round stack (no layer offset), refused sheet → null.
4. Fixture `kata-column-rebar-sample.json` regenerated (`HPREBAR_UPDATE_KATA_FIXTURES=1`), `columnContracts.ts` +
   contract test.

## Success
Core tests green; fixture carries `drawing`; contract test green.
