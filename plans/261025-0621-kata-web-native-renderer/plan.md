# Kata Export — native web renderer (Revit 2026)

Status: **In progress** — user approved 2026-10-25 (three.js OK); phases 1–5 done. Follows `plans/261024-2345-kata-export-web-ui-hprebar/` (committed 17caa8d, 7b23d43).

## Goal
Drop the hidden WPF `KataElevationCanvas` → SVG pipe on Revit 2026. The page draws the elevation, Kata's rebar elevation, bar tags and sections itself from model-space data (mm), with client-side zoom/pan and web-only features. Other Revit versions: not touched, not tested.

## Key insight (verified by scout)
All drawing geometry is already pure records in `HPRebar.Core` (mm, no WPF): `KataElevation` (`KataExport/Models/KataElevationModels.cs`), `KataElevationDrawing` / `KataDrawingLine(Pen, Points, Keys)` / dims / flags / bubbles / level / title (`KataRebar/Models/KataElevationDrawing.cs`), `KataSectionDrawing` (bulge polylines, bars, leaders, tags), `KataBarTag`, `KataSectionCut`. The WPF side (`View/Controls`, ~2.5k lines) only does mm→px layout + painting. → send records once per plan, port layout + paint to TS.

## Architecture
```
KataExportViewModel ──(plan/elevation changed)──▶ KataWebSceneMapper (Core, pure) ──▶ event kata.scene {elevation, rebar{lines,dims,flags,…,tags,cuts}}
page: scene store → Viewport (mm↔px, one scale) → SVG layers (model <g transform>, pixel-fixed annotation layer) → pointer/wheel/pinch local, no round trip
page gestures that change the model → commands: kata.update {selectedColumn}, kata.removeBars {keys}, kata.run undoRemoveBars, kata.section {number, mirror} (on demand, cached)
```

## User decisions (2026-10-25)
- **Delete the WPF Kata Export window in every version**: Kata Export = Revit 2026 only, web only (ribbon button gated `#if REVIT2026`). `KataSettings` (own WPF dialog, `View/Tabs`) is a separate command — out of scope, kept.
- MVP web features: **3D view of beam + rebar**, **2D longitudinal + cross sections**, hover + tooltip, smooth zoom, minimap + find bar, export SVG/PNG/print PDF, measure + layer panel.

## Phases
| # | Phase | Type | Status |
|---|---|---|---|
| 1 | [Scene contract + mapper in Core](phase-01-scene-contract.md) — [report](reports/phase-01-scene-contract.md); 3D DTO moved to phase 7 | AFK | ✅ Done (live CHƯA TEST) |
| 2 | [TS viewport + beam elevation layer](phase-02-viewport-elevation.md) — [report](reports/phase-02-viewport-elevation.md) | AFK | ✅ Done (live 7/7) |
| 3 | [Rebar layers: Kata elevation (longitudinal section), tags, cuts](phase-03-rebar-layers.md) — [report](reports/phase-03-rebar-layers.md) | AFK | ✅ Done (live 9/9; bars live CHƯA TEST) |
| 4 | [Interaction + cross sections](phase-04-interaction-sections.md) — [report](reports/phase-04-interaction-sections.md) | AFK | ✅ Done (live 11/11 + plain card live; bar pick live CHƯA TEST — needs a workbook matching the run) |
| 5 | [Web features: minimap, find, export, measure, layers](phase-05-web-features.md) — [report](reports/phase-05-web-features.md) | AFK | ✅ Done (live 11/11 + 7/7; PNG/print live CHƯA TEST) |
| 7 | [3D view (three.js)](phase-07-3d-view.md) | AFK | Planned |
| 6 | [Cut-over: delete WPF window everywhere, live, review, docs](phase-06-cutover-verify.md) | HITL (Revit) | Planned |

Order: 1 → 2 → 3 → 4 → 5 → 7 → 6.

## Decisions (proposed)
- Renderer = **SVG** (vector text crisp at any zoom, DOM hit test, CSS theming, export for free). Canvas2D only if a measured scene > ~20k elements is slow (not expected: one beam run).
- Model layer in mm under one `transform`; annotations whose size is fixed in px (letters, chains, bubbles, tags rows) recomputed in TS from `KataElevationScene` rules — ported 1:1, same constants.
- Pens/colours → CSS variables from `KataCanvasPalette` (dark/light follow `theme.changed`); `vector-effect: non-scaling-stroke` for lineweights.
- Section bulge vertices → SVG arc commands (exact, no flattening).
- Wire contract hand-mirrored in TS + extended drift test (as today), JSON fixture of a real plan exported by a Core test for vitest goldens.
- Delete `DrawingSvgWriter` (+ tests), the hidden canvas, `KataExportView.xaml`, `View/Controls/*` painters (~2.5k lines) once the web port reaches parity.
- 3D = **three.js** (new npm dependency, MIT, tree-shaken ~150 KB gz) fed by `KataRebarLayoutResult` 3D centerlines (`KataRebarCurve.Polyline`, mm, beam local) + stirrup zones + concrete extruded from the beam outline/width profile; orbit controls, bar picking shares the 2D selection store.

## Risks
- Parity of ~2.5k lines of layout rules (stacking of tags, chain rows, label lanes) — mitigated by porting file by file, vitest on fixture, side-by-side screenshots vs the WPF window (`HPREBAR_KATA_UI=wpf` kept until phase 6).
- Kata elevation label collision rules (`KataLabelLane`, `KataCadTag`) subtle → port as pure TS functions with unit tests from C# behaviour.

- 3D stirrups: zones carry spacing, not per-stirrup curves for every bar → expand zones into instanced shapes in TS (InstancedMesh), verify count vs Revit.
- No WPF fallback once deleted: a web regression blocks Kata Export → keep the WPF window until phase 6 live parity passes, delete in the last commit.


