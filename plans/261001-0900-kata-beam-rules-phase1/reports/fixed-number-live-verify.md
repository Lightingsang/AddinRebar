# Live verify — longitudinal bars as Fixed Number sets, Revit 2026.4 (2026-10-02)

Contract (/grill-me 2026-10-02): every group of ≥ 2 identical, evenly spaced longitudinal bars of one cell (main B11/B12, rows 13–16, 17–18, side bars) = one Rebar with layout Fixed Number; a lone bar = Single; mixed Ø / left;right / uneven → split into evenly spaced runs, rest Single; positions unchanged ±0.5 mm.

Environment: the user's own Revit (pid 45464) stayed open. The locked `Addins\2026\HPRebar\HPRebar.dll` was renamed (`*.locked-<stamp>`) and the new build copied beside it; a second Revit (pid 32560) opened a fresh copy `thcphcs2-fixednumber-test3.rvt` of `Desktop\Kata\THCPHCS2-…_detached.rvt`. MCP pipe was served by the test instance (every script guarded on `doc.PathName`); UIA scripts pinned to the test pid. Workbook `kata-rebar-live.xlsm` (read only, no Xuất). Run: B_200x600 "Sảnh Đón" 9856329/9856352/9856371, cells of phase-06 + E14 `2f16;2f14`, D18 `2f16+1f14`.

## Found during the run
Revit snaps a new bar's plane (`RebarPlane` handle, `ToCover`, distance 0) to the host's cover: a Ø16 bar planned at y −29 landed at −33, single bars included (pre-existing, invisible until the set check measured it). Fix in `KataRebarCreationService`:
- single bar → `ElementTransformUtils.MoveElement` back to its planned offset (holds through regenerations, probed);
- set → after `SetLayoutAsFixedNumber`, the constraint of `RebarPlane` (bar 0) and `OutOfPlaneExtent` (bar n−1) gets its cover / host-face distance corrected by the measured error, tried one sign, re-measured, reversed if worse; then both ends are checked ±0.5 mm, otherwise roll back to singles.

## Result (final build)
| Check | Result |
|---|---|
| Message | 12 + 1 sets → final run: 13 Fixed Number sets, 4 singles, 0 fallback |
| Main top 1 / bottom 2 (2Ø18) | ✅ FixedNumber n=2, y −58.1 / 57.9, geometry as phase 06 (x 50→13350 legs 475; 137→13307) |
| C14 2Ø14, E14 `2f16;2f14`, G14 2Ø16 | ✅ 3.1.2 n2 ±60; 3.2.2T Ø16 n2 ±59, 3.2.2P Ø14 n2 ±19.6 (own sets); 3.3.2 n2 ±59 |
| Row 13 1Ø16 / 1Ø18 | ✅ Single at y 0 |
| D18 `2f16+1f14` | ✅ 4.1.1 Ø16 FixedNumber n2 ±29, Ø14 Single at 0 |
| Side bars (G4 12, G5 2) | ✅ 6 sets n2 ±61, z −383 / −217 |
| Longitudinal bars total | ✅ 30 (plan 4 + 11 + 3 + 12) in 17 elements |
| Re-generate | ✅ deleted 32, created 32 |
| Undo (UIA Undo button once) | ✅ back to the previous 32 elements (ids 10405049–10405091) |
| Revit warnings | 2 × "outside of its host" on the main-bar sets (host 9856352 is cut in this model, same cause as phase 06's 4 singles) |
| Variance (y −0.1 everywhere) | beam axis offset B9 0.1 mm |

Builds R26/R25/R24 0 errors; Core tests 826/826.
