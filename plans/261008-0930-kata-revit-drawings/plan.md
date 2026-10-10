# Kata drawings in Revit: long + cross sections, dims, tags, sheet

Status: phase 1 done (live B03), phase 2 next (2026-10-08). Contract: grill-me 2026-10-08 (user "implement").
Source of truth when sources differ: T2-DY7.dwg as measured (Core canvas builders, ±2 mm tests), not
`Q:\...\QUY_TRINH_THUAT_TOAN_VE_DIM_TAG_THEP_DAM.md` (reverse-engineered Kata notes — rules/API names only, no code).

## Decisions (user)
| Topic | Decision |
|---|---|
| Approach | hybrid: real Revit views + bars; native Dimension / Rebar Tag; 2D detail lines for what Revit lacks |
| Geometry source | `KataElevationDrawing` / `KataSectionDrawing` (canvas, DWG-matched) mapped to local frame |
| Tag family | ~~tool-made~~ blocked: Revit API cannot create labels (no FamilyItemFactory.NewLabel, RevitAPI.xml 2026) → **rebar tag already in the HP template** |
| Combined Kata tag (2Ø25+1Ø20) | one Revit tag per set, stacked on Kata's row; MultiReferenceAnnotation tried in the tag phase |
| Dims | real column/beam faces where possible, else detail lines (`<Invisible lines>`) at Kata's points |
| Cross sections | one per Kata flag (B01: 14), name "<B3> n-n", 1:25, same number when same bars |
| Sheet | "K-<B3>", title block most used on the model's sheets (first-by-name gave a cover sheet — changed 2026-10-08, to confirm with user), elevation on top, sections in a row (DWG step 1675 mm model) |
| Switch | Kata Settings checkbox "Tạo bản vẽ Kata (mặt cắt, dim, tag, sheet)", default on |
| Re-run | tool annotation replaced (storage-tagged); user annotation kept; a view that no longer fits is kept untagged |

## Phases
| # | Scope | Status |
|---|---|---|
| 1 | switch; cross-section views per flag; 2D breaks + layer-2 circles; dim chains (invisible-line ticks); sheet + viewports | built, tested, live B03 2026-10-08 |
| 2 | rebar tags (template family) on long + cross sections at Kata tag rows; MRA spike | waiting: tag family name check in template |
| 3 | dims bound to real column faces (support widths, spans) | todo |
| 4 | review, docs, live B01/B03 | todo |

## Phase 1 design
- Core `KataDimChains`: Kata's touching dims on one line (same axis, LineAt ±0.5) → one chain of stations; Run style skipped.
- Revit `KataDrawingDims`: per station a 10 mm tick detail line (invisible) → `NewDimension` over the chain; storage kind `Drafting`.
- Revit `KataCrossSectionViews`: box BasisX = local Y (screen right = −BasisX = −local Y, Kata draws +Y left), look +local X; reuse by storage kind `Cross:<i>` when station/direction unchanged, else forget + create.
- Revit `KataSheetComposer`: sheet storage kind `Sheet`; viewport centres = drawing coords / 25, sections at x = 250 + 1675 i, tops aligned at elevation bottom − 900 (B01 fit; Kata's exact sheet rule not derived).
- All inside the non-fatal drafting step of `KataRebarOrchestrator` (bars stay on failure), one Ctrl+Z.

Reports: `reports/`.
