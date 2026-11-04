# Kata Column Rebar — CAD-style canvas drawing (elevation + sections) on the page

Requirement contract (user, 2026-11-04, grill):
- Step 2 of the Kata Column Rebar page draws the column's rebar from sheet `Cot` on one canvas, like Kata VC's
  AutoCAD drawing: **elevation** (mặt cắt dọc) left, **sections** (mặt cắt ngang 1-1, 2-2 …) right. Replaces the
  section grid.
- Elevation as VC: per storey the representative bars (group A left, group B right, layer 2 inside) with laps,
  1:6 cranks, cut + anchor legs, starters in the pile cap, hoop zones with "Ø10a100/a200" notes, levels (name + CT),
  lap / zone dims, bar labels "20Ø25" + numbers, section markers at each cut.
- Bars = the bars the tool will create in Revit (real faces), not the sheet sizes.
- Zoom / pan / fit; hover or click a bar → highlighted in the elevation and every section, chip with number, Ø,
  length, group. Colours follow the page theme. Bar schedule: later slice (out).
- Out: bar schedule, export/print, Revit-side changes, editing bars from the page.

Reuse: `src/kata/elevation/{shapes,palette,text,kataCad}.ts` + `kata/elevationGestures.ts` (generic, no beam types);
Core `KataColumnElevationDims`, `KataColumnSectionDrawings`, `KataColumnSectionDims`. No new dependency, no ADR
(web page = ADR-0014). Wire: `KataColumnRebarSummary` gains `Drawing` (additive, page ↔ host only).

| # | Phase | Type | Depends | Status |
|---|---|---|---|---|
| 1 | [Core drawing model + mapper](phase-01-core-drawing-model.md) | AFK | – | ✅ done (Core 1823) |
| 2 | [Canvas painter + layout + gestures](phase-02-canvas-painter.md) | AFK | 1 | ✅ done (vitest 40) |
| 3 | [Bar picking + highlight + chip](phase-03-picking.md) | AFK | 2 | ✅ done |
| 4 | [Live check vs VC + docs + review](phase-04-live-verify.md) | HITL (idle machine) | 3 | ✅ live render + pick + zoom OK (copies 28, 29); review 7/10 fixed; docs done; VC side-by-side done (j-all500) |

Gates: `dotnet test HPRebar.Core.Tests`, `npx vitest run src/kataColumn`, `dotnet build -c Debug.R26`; live on a
detached copy + scratch workbook; screenshot beside the VC drawing of the same sheet.

## Log
- 2026-11-04: phases 1–3 implemented. Rendered as SVG painter shapes (the beam page's engine, `ShapesSvg`), not
  `<canvas>`. Hoops closer than 4 px on screen drawn as a zone band. Old section grid (`ColumnSection.tsx`) deleted.
  Live copy 28 (C24, 6 storeys): drawing rendered, click on bar 1 → chip "Thanh 1 · Ø20 · dài 2100 mm · nhóm A · lớp 1 · PILECAPS".
- 2026-11-04: review 7/10 (reports/code-review-canvas.md) H1, H2, M1–M5, L1–L3, L7 fixed; live copy 29: 14 labels 5Ø20 (= 10 bars per storey), wheel zoom readable.
- 2026-11-04: side by side with VC j-all500 (same sheet as the fixture): levels/CT, section marks, cap + starter feet, labels per group + Ø, dims, sections match. Adopted from VC: hoops as the first two + last two of each zone, cut beams with break marks at each head, title "C1 (SL=1)". Left for a later slice: VC's exploded tie details beside each section ((13) Ø8a400 …), group labels on the sections, VC puts every label on the right.
