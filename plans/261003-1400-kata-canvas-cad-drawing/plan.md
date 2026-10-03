# Kata canvas = Kata elevation drawing (rebar on)

Contract agreed 2026-10-03 (grill-me): with Thép on, canvas draws like the Kata DWG; Thép off unchanged. Reference T2-DY7.dwg (DY7, DY14 E350, DY14 E500), read-only via hprebar-autocad (runs 717-725).

## Rules read from the DWG (x from outer face of first support, z from beam top)
| Item | Rule |
|---|---|
| Lineweights | bars + stirrups 0.35 → 3 px, outline 0.20 → 2 px, dim/grid/leader/hidden 0.05 → 1 px (fixed, any zoom) |
| Colours | outline cyan, hidden 9 grey dashed (HIDDEN ×18.75), grid 8 CENTER ×12.5, dims grey + green text, flags yellow, bubble grey + white text, blank zone dims magenta |
| Outline | span top/soffit; column supports: stubs +150 / lowest soffit −150, break zigzag k = min(50, w/6) at both ends; beam support: top + soffit lines across, no stubs; end face lines |
| Hidden | z = 0 across column tops; slab line z = −B7 split at grids, drops to soffit at the ends |
| Bars | drawn d/2 + ds/2 nearer their face than their centre (layer 1 on stirrup centre line); side bars as is |
| Stirrups | first + last of each zone, face ∓ 25; blank dim between them at z = −B7 (magenta, run ticks) |
| Top chain z +462 | support widths; span split at last stirrup of zone 1 and first of last zone |
| Stagger dims | layer-1 vs layer-2 free ends of extra bars, line at layer-1 drawn level + 125 |
| Depth dims | x −350: h of span 1; x −200: B7 + rest |
| Level | CT block at (−475, 0), text = B10 |
| Below (from stub bottom) | bottom chain −300 (ends, grids, support faces, innermost extra-bottom ends), axis chain −475, bubbles + bottom flags −675, grid line −500 |
| Flags | SECBAL at each cut, +587.5 and mirrored below; grid line top +662.5 |
| Title | (L/2, stub bottom −950): "B3 (SL=B4; L=total)" h125 underlined, "TL: 1/25" h62.5 |
| Dim style | text 62.5 above line (gap 15.6), arch tick 37.5 (width 5.6), ext fixed 93.75 + 37.5 past, line ext 30 |

GIẢ ĐỊNH CHƯA XÁC MINH: rows below fixed to stub bottom (both drawings have lowest soffit −600); interior beam support drawn like the end one; right end column mirrors the left; zone split with 2 zones = last of zone 1; CT text = B10; slab line fallback none when B7 = 0.

## Phases
| # | Work | Status |
|---|---|---|
| 1 | Core `KataElevationDrawing` + builder (outline, dims, stirrup runs, drawn bar levels incl. tag feet) + golden tests DY7/DY14 vs DWG | done |
| 2 | WPF `KataElevationCadPainter` / `KataCadDimPainter` / `KataCadText` (mm, lineweight px); px annotations + cut markers hidden in rebar mode; fallback to plain elevation if Kata paint fails; old rebar painter (incl. tie marks) removed | done |
| 3 | Build R26/R25/R24 ✓; Core 902/902 ✓; live DY7 (2nd Revit, model copy, scratch workbook) whole run / left end / right end / Thép off ✓; review ([report](reports/code-review.md)) M1–M4, L1–L4, L7 fixed | done |

| 4 | User OK 2026-10-03 ("có"): committed 8176285; stirrup zones as Kata — dense zone = ceil50(0.25 L0) from face, last hoop on it, middle from +s2 to −s2, evenly at ≤ nominal (Spacing placed / NominalSpacing label); zones closer than a dense spacing meet (250 span = 16350/16425/16500); Shift+left pan; review ([report](reports/code-review-stirrup-zones.md)) H1 short-span hoops, M1, M2 fixed; tests 905/905, builds ✓, live DY7 generate ✓ (500/2050/4550, 6500/8400/11650, 13900/14600/15500), canvas top chain 1400/2700/1400 ✓ | done |

Known gaps (user decision): bottom main bar hook leg drawn where the layout bends it (133) vs Kata 35; layer-2 bars 5 mm further in (clear gap 30 vs 25). Kata's own hoop count inside a zone is not on its drawing (only first/last): ours keeps spacing ≤ nominal (DY7 span 1: 15 @ 96.4). Open review lows: L5 per-paint allocations, L6 leader pen DPI, L8 tags/cuts/elevation built together.
