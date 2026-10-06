# Kata B03 drawing parity + Revit check

Contract (grill-me 2026-10-06): B03 like B01 — DWG fixture + tests (bars, stirrups, tags, numbers of 10 sections + elevation),
U/C tag rule common to B01/B02/B03 (±2 mm), rules doc, Revit B03 on a model copy measured over MCP (user ticks opt-in).
Out: cut / flag positions (keep 0.1 L), B02-only gaps, B01 #18 / hanger numbers.

Sources (frozen): T2-DY7.dwg saved 2026-10-06 09:39 (B03 title at 23457,24910; origin = column C outer face at beam top
= 6732, 27210), KataB03.xlsm 09:40 (`reports/kataB03-dam-cells.txt`). Priority DWG > 04_quy_dinh > QUY_TRINH.
Base: HEAD 97c4b4e; the 08:25 partial revert of it is in `git stash@{0}` (user decision: build on HEAD).

| # | Phase | Status |
|---|---|---|
| 0 | Fixture `b03-dwg.json`, B03 sheet in `KataDwgBeam`, run DWG suites → gap list | ✅ |
| 1 | Sheet/geometry: left end 150 past column 1 (console depth was already right) | ✅ |
| 2 | Bars: side bars, row 17 at supports, inner C carry, layer-2 ties | ✅ |
| 3 | Numbering order + combined tags | ✅ |
| 4 | U/C + stirrup tags on B01/B02/B03 (±1, one hoop tag gap) | ✅ |
| 5 | Rules doc ✅, review fixed ✅, Revit copy generated + measured ✅ (≤7 mm, Revit cover snap) | ✅ |
