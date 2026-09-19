---
phase: 3
title: "Piles — detect, name in reading order, coordinate table, create from a coordinate list"
status: planned
priority: P2
effort: "10h"
dependencies: []
---

# Phase 3: Pile tools (cọc)

## Context Links
- [plan.md](plan.md) · [research](research/vinacad-express-tools-analysis.md) (rows `TDC` / `VC` / `DTC` / `XLDTC`)
- Reuse: `Cad/StructuralService` (`ClaimMarks`: a mark-shaped TEXT within `MarkReachMm` 300 belongs to exactly one member), `Structural/MemberTagging`
  (`Assign`, band ordering, `kept_existing` / `overwrite`), `Cad/StructuralWriteService` (`WriteTags` two-phase: attribute via `BlockService`, TEXT edited in
  place, else new middle-centred DBText), `Cad/StructuralWriteService.Table`, `Cad/EntityFactory` (circle / insert), `ChangeSetRecorder`, `WriteToolTable`

## Overview
Foundation plans: hundreds of piles drawn as one block (or a circle) each, named `P-001…`, and a coordinate table (`Số hiệu | X | Y`) that
the site engineer sets out from. Four tools close the loop both ways: detect → name → table, and list → draw. Fits the office's
`FoundationRebar` work.

Tools (category Structural): `detect_piles` (`none`) · `name_piles` (`auto`) · `pile_coordinate_table` (`auto`; `write: false` → data only)
· `create_piles_from_table` (`auto`).

## Key Insights
- A pile is whatever the caller says it is: `blockNames` (wildcards; default `*COC*,*PILE*`) on `layers` (default any), plus circles
  with `diameterMm` in `[min, max]` on the given layers when `includeCircles`. **No attribute / "contains a circle" heuristics** — the
  source's fallbacks silently took any attributed block; here the selection is explicit and the summary says how many candidates each
  rule matched.
- The mark is the block's `MARK` / `SOHIEU` attribute (tag list configurable) or the nearest mark-shaped TEXT within `markReachMm`
  (phase-E claim rule: one text belongs to one pile).
- Coordinates are the INSERT position / circle centre in **WCS**, reported in mm and in the table unit (`m` with `decimals` 3 by default);
  optional `originMm` shifts to a site grid origin; optional `swapXY` for cadastral paperwork (X = Northing) — same caveat as HPGeo, the
  drawing itself is never swapped.
- Reading orders: `rowsThenColumns` (top→bottom, left→right), `columnsThenRows`, `rowsThenColumnsReversed`, `columnsThenRowsReversed`,
  `alongPolyline` (order by station along a caller polyline handle — pile rows along an alignment).

## Requirements
### Functional
- `detect_piles`: `selection {blockNames, layers, includeCircles, circleDiameterMm{min,max}, space, handles}`, `marks {attributeTags
  ["MARK","SOHIEU"], markReachMm 300, prefixes ["P"]}`, `order`, `rowBandMm` (default 600 — pile spacing scale), `limit` ≤ `MaxPileLimit`
  300 + `offset` → `{success, summary{piles, byRule{block, circle}, unmarked, duplicateMarks}, items[{index, handle, kind, blockName,
  xMm, yMm, diameterMm?, mark, markSource attribute|text|none, markHandle}], warnings}`.
- `name_piles`: same `selection` + `marks` + `order` + `prefix` (default `P-`), `start`, `digits` (3), `mode keep|overwrite`
  (`keep` = existing marks with the prefix keep their number and are reserved; foreign marks untouched — phase-E semantics),
  `textHeightMm` (default 250 → **must be given when a new TEXT is created and no text style height applies**), `textStyle` (must exist),
  `labelOffsetMm {x, y}` (default `{diameter/2 + 100, 0}`), `layer` for new texts (`S-ANNO-TEXT` created on demand), `dryRun`, `changeSetId`.
  Output `EditResult` with `items[{handle, oldMark, newMark, action attribute|text_updated|text_created|kept_existing|kept_foreign}]`; cap `MaxTagPiles` 300.
- `pile_coordinate_table`: rows from `detect_piles` (re-collected), `unit m|mm`, `decimals`, `originMm`, `swapXY`, `sort mark|order`,
  `write` (default true) with `locationMm`, `space`, `tableStyle`, `textHeightMm`, `rowsPerTable`, `columnGapMm`, `title`; `write:false`
  returns `rows[]` only (`none`-like behaviour under an `auto` tool is allowed — the run simply modifies nothing).
- `create_piles_from_table`: `rows[{mark, xMm, yMm}]` (≤ 300; from the AI reading an Excel / CSV), `blockName` (must exist → else circle
  with `diameterMm`), `layer`, `label` (attribute or TEXT as in `name_piles`), `skipExistingMarks` (a pile with that mark already in the
  drawing is skipped + warned), `dryRun`, `changeSetId`, `atomic`.
### Non-functional
- Every pile-shaped input validated before anything is opened for write (two-phase); `rows` with a non-numeric coordinate → `ArgumentException`.

## Architecture
```
Cad/PileService.Collect(db, tr, selection, marks, order) → PileRecord[]   (query via EntityQueryService, marks via ClaimMarks, order via MemberTagging)
Piles/PileNaming.Plan(records, opts)  → NamingPlan[]     (pure; reuses MemberTagging.Assign semantics)
Piles/PileTable.Build(records, opts)  → PileTableRow[]   (pure; unit, origin, swap, natural sort)
Cad/PileWriteService.{WriteMarks (StructuralWriteService.WriteTags generalised), WriteTable (TableWriter), CreatePiles (EntityFactory)}
AecTools.Piles.{Detect, Name, Table, CreateFromTable}
```

## Related Code Files
- Create: `HPAutoCad.Aec/Piles/{PileRecord, PileNaming, PileTable}.cs`, `Cad/PileService.cs`, `Cad/PileWriteService.cs`, `AecTools.Piles.cs`,
  4 seed folders under `Structural/`, `HPAutoCad.Aec.Tests/PileTests.cs`.
- Modify: `StructuralWriteService.WriteTags` → shared `MarkWriter` (parameterised by prefix / layer / offset), `WriteToolTable`, harness scene (a 5 × 4 pile grid as a `PILE-D300` block with `MARK`, three marked by TEXT, two circles, one duplicate mark).

## Implementation Steps
1. `PileRecord` + `PileTable.Build` + `PileNaming.Plan` with tests (orders × 5, keep vs overwrite, duplicate marks reported, `swapXY`, `originMm`).
2. `PileService.Collect` (explicit rules only, counts per rule) + `detect_piles` seed.
3. `MarkWriter` extraction + `name_piles` seed (attribute first, text in place, else new text) + change set.
4. `TableWriter` reuse (phase 2 extracts it — if phase 2 is not done first, extract here) + `pile_coordinate_table`.
5. `create_piles_from_table` (block or circle + label) + change set.
6. Live: detect 23, name 20 in `rowsThenColumns` keeping 3 existing, table in m with 3 decimals equals the block positions, create 4 from rows, `U`, unknown block → circle path, non-numeric row → refused.
7. Review + docs.

## Todo List
- [ ] 1 · [ ] 2 · [ ] 3 · [ ] 4 · [ ] 5 · [ ] 6 live · [ ] 7 docs

## Success Criteria
- Table X/Y equals INSERT positions to 1 mm; renaming is idempotent (second `name_piles` in `keep` mode changes 0); `create_piles_from_table` followed by `pile_coordinate_table` reproduces the input rows.

## Risk Assessment
- Piles drawn as dynamic blocks with a visibility state per diameter → effective name matching (phase B rule) + `diameterMm` read from a dynamic property when present.
- Rotated UCS drawings: WCS only in v1; a warning when the current UCS is not world.

## Security Considerations
- Writes: attributes / TEXT / INSERT / circle / one table; no deletion (`create_piles_from_table` never erases); change-set aware.

## Next Steps
- A `pile_cap_detect` (đài cọc from closed polylines around pile groups) is a natural follow-up over `arch_detect_rooms`-style loops — not in this plan.
