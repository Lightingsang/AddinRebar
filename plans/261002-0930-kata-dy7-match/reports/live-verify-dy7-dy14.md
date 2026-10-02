# Live verify — T2-DY7 vs Kata drawing, T2-DY14 run (Revit 2026.4, 2026-10-02)

Env: user's Revit (pid 51676) untouched; locked `Addins\2026\HPRebar\HPRebar.dll` renamed `*.locked-<stamp>`, new build copied; second Revit on a fresh copy `thcphcs2-dy7-test2.rvt` of `Desktop\Kata\THCPHCS2-…_detached.rvt`, MCP scripts guarded on `doc.PathName`, UIA pinned to the test pid. Kata drawing `Documents\T2-DY7.dwg` read through MCP AutoCAD (read-only). Workbook `kata-rebar-live.xlsm` (backup `kata-rebar-live-dy7-20261002-102603.xlsm` before DY14 export).

## T2-DY7 (x mm from column C outer face; Kata = DWG x − 6732; z from beam top)
| Bar | Kata drawing | Revit | Δ |
|---|---|---|---|
| Top main 2Ø18 | 30 → 16215, legs 300 | 42 → 16208, legs 325 / 266 (I clamped) | ✅ ≤ 12 / leg I −34 (clamped, warned) |
| Bottom main spans 1–2 | 35 (leg 125) → crank 5950→6550, −100 → 13815 leg 125 | 133 (leg 225, inset) → 5950→6550 → 13808 leg 150 | ⚠️ start: inset 98 (bottom leg kept clear of top legs; agreed) |
| Bottom main span 3 | 13310 → 16190 leg 400 (above top) | 13310 → 16165, leg 266 (clamped to depth, warned) | ✅ x; leg clamped |
| C13 / C14 | 2350 / 1850 | 2350 / 1850 | ✅ 0 |
| E13 / E14 | 4050–8700 / 4550–8200 | same | ✅ 0 |
| G13 / G14 | 11150–16215 (anchored at I, "-") / 11650–14400 | 11150–16208 anchored / 11650–14400 | ✅ ≤ 7 |
| D18 / D17 | 850–5550 / 1350–5050 | same | ✅ 0 |
| F18 / F17 | 7100–12750 / 7600–12250 | same | ✅ 0 |
| Side bars 2Ø12 | 330–13520, z −250 | same | ✅ 0 |
| Dense zones | 1400 / 1750 / 550 from faces | last dense stirrup 1350 / 1650 / 500 (a100 from 50) | ✅ |
| Stirrup heights | 500 / 600 / 350 | 500 / 600 / 350 | ✅ |

Result message: 6 main, 12 support, 8 span, 2 side, 9 stirrup sets; all longitudinal multi-bar groups Fixed Number. Revit warnings: 7 × "outside of its host" on top bars — the floors (50/100/120) cut the beam top (known model join issue, not a rule).

## T2-DY14 (no Kata drawing — run report)
Sheet after export: C 450 | 5650 | E 350 | 6950 | G 450 | 2200 | I 250 | J 250 | K beam 100×350; rows 21: 0, −100, 150, 150; rebar cells kept from DY7 (I13 "-").
- Bottom: crank at E (6100→6700), cut at G (deep end 13808), spans 3–4 one bar 13310 → 16565 (K beam, inset).
- G13 "-" at I (now interior): runs through I, cut H5 × 250 → 16400.
- Side bars: spans 1–2 continuous 330–13520; span 4 (250 long) gets its own 2Ø12 from G4/G5 (J20 empty).
- 10 Fixed Number sets, 5 singles, 0 fallback (earlier build: 1 fallback "RebarConstraint is not FixedDistanceToHostFace" → fixed: only cover / host-face constraints are adjusted).

Builds R26/R25/R24 0 errors; Core tests 841/841.
