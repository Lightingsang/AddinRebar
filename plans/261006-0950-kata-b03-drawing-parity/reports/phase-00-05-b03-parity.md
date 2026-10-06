# B03 parity — result 2026-10-06

Fixture `HPRebar.Core.Tests/KataRebar/Fixtures/b03-dwg.json` (T2-DY7.dwg 09:39, origin 6732, 27210 = column C outer face at beam top; exporter = B01's with Ox/Oy). Sheet cells in `KataDwgBeam.B03Sheet` (= reports/kataB03-dam-cells.txt). B03 added to all DWG suites (sections bars/lines/tags, stirrup tags ±1, numbers, elevation).

Rules added (docs/specs/kata-beam-rebar-rules.md §12b, R-149…R-158): end overhang to crossing beam (Revit: dropped + warning), row 17 at supports ("-" run on / bars over), numbering of those with supports, mixed-layer spacer tie, side-bar levels from run's first span + run breaks at top change, inner-stirrup carry stops only at row-24 "*", U/C tag rule with ≥2 side layers, combined inner tags, tag offsets per stirrup Ø / width.

Checks: Core 1590/1590 (B01, B02, B03 DWG suites; DY7/DY14 goldens); Debug.R26 build OK.
Revit (test copy of Dam kata test.rvt, B03 ids 9795052/54/59/61, KataB03 active): generation OK twice — Rebar Number partition B03 = Kata 1..31, run 2 deleted 50 via storage; warning C21 overhang shown; 7 transient "outside of host" on top bars (#1,2,8,9,12,13,14 — slab-join, B01 had 18). Bar coordinates NOT measured: MCP bridge pipe is held by the user's Revit (read-only geometry read there: B03 beam starts at column 1 outer face, no 150 overhang).

Known gaps: F17/D17 own cut points (Kata 13650/1800 vs 13450/2150), hoop tag span 2 (−762.5 vs −687.5), end cover at columns (Kata 50 vs a), row-17 anchor leg at G (300 vs 450), Kata draws 2Ø20 of F17 on the 6Ø25 (HP spreads them into gaps ±83).

## Revit measurement 2026-10-06 (copy b01-test.rvt, user stopped own listener, opt-in ticked by user in the copy)

Generated with the 11:05 DLL; read-only MCP script (run 197): every Kata bar on the 4 B03 hosts, Rebar Number, positions 0 and last.
- 21 of 22 longitudinal numbers match the Core plan (Revit geometry, overhang dropped) within 8 mm; 9 of them within 1 mm. The 5-8 mm are bar ends at column faces / console tips pulled to Revit's cover (25 + d/2 = 37.5 vs a = 30).
- #19 (F17 2Ø20) at y −81 / 0: a Core defect (middle-gap pick took the 0 gap) — fixed after the run (±81 symmetric, test added), NOT regenerated in Revit yet.
- Stirrup zones, inner U/C and ties at the Core stations (+50 frame shift): 450/3200/8200, 11250/14750/21250, 25050/26750/29650, 31650.
- Rebar Number = Kata 1..31.
Decision pending: end cover at columns — Kata 50 (B01-B03) vs approved MVP rule A1 "a from the outer face" (J9 a = 30 here); switching breaks 14 MVP tests, reverted, asked user.

## Revit re-measurement 2026-10-06 11:43 (DLL with F17 fix + end cover max(a, 50), run 198)
- 22/22 longitudinal numbers present, Rebar Number = Kata 1..31; #19 now ±81 (fixed).
- 12/22 within 1 mm of Core; 10 within 5-7 mm: bar ends at an interior anchorage (G, E) or a console tip where Core keeps a = 30 and Revit pulls the bar to its host cover 25 + d/2 (37.5 / 35). Column-end starts (R-159, max(a, 50)) match exactly.
- Stirrups, inner U/C, ties at Core stations (previous run, unchanged code).
Test Revit closed after the measurement (no save).
