# Kata canvas: tag + bars like the Kata DWG, AutoCAD zoom/pan

Contract agreed 2026-10-03 (grill-me). Reference: `C:\Users\...\Documents\T2-DY7.dwg` (DY7, DY14, DY14 E=500), read-only via hprebar-autocad MCP.

## Rules read from the DWG (verified, runs 704-715)
| Item | Rule |
|---|---|
| Tag stations | section cut stations (`KataSectionCuts`); end cut of a span points left (vis P), others right (T) |
| Short span L < 3 m, 3 cuts | start cut full, mid cut top only, end cut none (GIẢ ĐỊNH CHƯA XÁC MINH, fits DY7+DY14) |
| Levels at a cut | one tag per bar level; top: deepest level → row A; bottom: highest level → row A |
| Rows (mm from span face) | top +100, +237.5; bottom −137.5, −275; stirrup tag +387.5 |
| Leader | arrow (37.5) at bar, vertical to row, horizontal max(200, 48·chars) |
| KHT | circles r 62.5 centred ±62.5/±187.5 beyond insertion; text h 62.5, right-aligned ins−21 baseline +22 (T) / left ins+12.5 (P) |
| Side bars | tag at start cut + (mid − start)/3, bottom row A, one leader per level, "2Ø12" / "2x2Ø12" |
| Stirrup tag | insertion = zone centre (first..last stirrup) + 125, "Ø8a100", text centred on row, no leader |
| Bar lines | per bar at its level; tick 75 back / 25 inward at both ends (top bars down, others up; legs toward bar body); 90° bends filleted r 20 |
| Colours | bars red, stirrups magenta 255,0,191, text green, circles/numbers white (dark text in light theme), leaders gray 128 |
| Zoom/pan | one scale X=Y; wheel 1.25×/notch at cursor; middle drag pan (hand); middle double-click = extents; left click selects |

## Phases
| # | Work | Status |
|---|---|---|
| 1 | Core: `KataElevationViewport` uniform scale; `KataBarTagBuilder` rewrite; `KataBarDrafting` (ticks, fillets) + golden tests DY7/DY14/E500 — 892/892; DY14 stirrup tags 50 mm off (layout zones), span-4 zones merged like Kata | done |
| 2 | WPF: tag painter (mm, text squeezed to Kata 0.68 h/char), rebar painter (Kata colours, drafting), scene rows pushed past tag band, input like AutoCAD, every layer guarded | done |
| 3 | Build R26/R25/R24 ✓, tests 894/894; live DY14 + DY7 (extents, wheel, middle pan, middle double-click, DY7 cut 2 = user's CAD image) ✓; review ([report](reports/code-review.md)) M1–M5 + L2–L5, L7, L8 fixed and re-checked live; open: L1 left-cantilever side tag (no Kata drawing), L6 touchpad pan (user) | done |
