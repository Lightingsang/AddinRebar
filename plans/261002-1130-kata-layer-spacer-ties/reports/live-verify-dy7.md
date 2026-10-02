# Live verify — thanh C kê lớp 2 + bước J7/I8, T2-DY7 (Revit 2026.4, 2026-10-02)

Env: user's Revit (pid 51676) untouched; deployed `Addins\2026\HPRebar\HPRebar.dll` replaced (old copy unlocked once the previous test Revit closed); second Revit on fresh copies `thcphcs2-ties-test.rvt` / `-test2.rvt`, MCP scripts guarded on `doc.PathName`, UIA pinned to the test pid. Workbook `kata-rebar-live.xlsm` (scratch): J7 `a500`, I8 2. Path = **Kata Export** (Xuất Excel → Đọc thép Excel → Tạo thép Revit), same `KataRebarWorkflow` as Kata Rebar.

## Run 1 (before fix, Kata Rebar window) — stations OK, levels off
- 9 sets / 58 bars = 7 layer tie sets (33) + 2 side-bar tie sets (25), as computed by hand.
- Stations (local, world Y + 350): 3.1.2C 516·1016·1516; 3.2.2C 4616–5616 + 6516–8016; 3.3.2C 11716–13216 + 13916; 4.1.2C 1416 ×8; 4.2.2C 7666 ×10; 5.x.1C 516 ×11 / 6516 ×14 — all step 500 ✅.
- ⚠️ Top tie sets in beams 958/960 sat +13 mm (Revit `Edge → ToCover` constraint pulled them; MoveElement back holds — probed in dryRun). Layer-2 top bars also off (+4 / +12, pre-existing, longitudinal sets only fitted across the beam).

## Fix
`KataRebarSectionFit` (Revit side): after creation, the rebar's longest straight segment is compared with the planned one and the element moved back — ties in local Y+Z, longitudinal bars/sets in Z (a set left > 0.5 mm off falls back to single bars). Tie hook bend radius < (Ø bar + Ø tie)/2 → warning.

## Run 2 (after fix, Kata Export)
| Group | Bars level (z, mm) | Tie wraps at | Δ |
|---|---|---|---|
| Top layer 2 C / E / G | 3460 / 3459 / 3459 | 3460 (all 5 sets) | ≤ 1 ✅ |
| Bottom row 17 span 1 / 2 | 3140 / 3040 | 3140 / 3040 | 0 ✅ |
| Side bars | 3300 | 3300 | 0 ✅ |
Design: top layer 2 = 3550 − 90 = 3460 ✅; tie straight part = bar − 14 (Ø8 stirrup bend radius in this template), hook top = bar + 14.

Status message: 6 main, 12 support, 8 span, 2 side bars (9 Fixed Number, 5 single), 9 flat-bar sets, 9 stirrup sets. Revit "outside of its host": 11 (7 before) — the 4 extra are the top tie sets in beams 958/960, whose concrete top the floors cut down (beam 960 solid top 3430 < 3460): model join issue, same as the 7 top-bar warnings.

## Not live-checked
I8 = 1 (Giống đai ngoài), J7 typo fallback, left;right cell, bars ≥ Ø22 warning — unit tests only (852/852).
