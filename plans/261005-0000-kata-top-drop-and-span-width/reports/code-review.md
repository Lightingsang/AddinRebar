# Code review — top drop (row 19) + span width (row 20), 2026-10-05: 5/10 → fixed

| # | Finding | Fix |
|---|---|---|
| C1 | width cut dropped main bars of a side shorter than 2·G3·d (short span / console) | "shorten only" just for the support's own extras (`HostSupportIndex == k`); main bars always cut — test `A_short_span_beside_a_width_change_*` |
| C2 | layout datum = level, Revit placement z0 = first piece top → offset beams shifted (regression) | `KataBeamPlacement` origin at `TopFt − ZOffsetMm` (level) — test `A_beam_offset_from_its_level_as_a_whole_*` |
| H1 | measuring noise (0 vs −0.4) cut / cranked top bars | measured tops/widths/depths snapped to mm, neighbours within 2 mm equalised, `LevelTolerance` 0.5 — test `A_millimetre_of_modelling_noise_*` |
| H2 | downward crank at a zero-width station ran in the lower part (bar above concrete) | crank on the higher side — test `A_downward_step_at_a_station_*` |
| H3 | width + top change at one support → duplicate stubs | blocked in scope ("đổi cả bề rộng và cao độ đỉnh") — test `A_support_changing_both_*` |
| H4 | B5 compared with soffit depth | B5 vs first span concrete depth; Height = B5; tops compared relative to first span |
| M1 | no spacing check after narrowing | `KataWidthProfile.SpacingWarnings` names row 20 — test `Narrow_spans_with_bars_too_close_*` |
| M2 | split zones share name → single stirrup numbers, inner-set removal | parts named "Giữa nhịp (2)"; singles numbered by their own zone |
| M3 | B6 / Width from Revit-first piece | first sheet-order span's piece |
| M4 | side-bar requirement on soffit depth | concrete depth |
| M5 | zone ending within offset past a step not cut | cut whenever step inside zone; stirrups within offset dropped — test `A_zone_ending_just_past_*` |
| M6 | bottom-extra clearance ignored drop | lowest top of the span |
| M7 | shortened legs silent, Hook90 with leg 0 | warning, hook cleared, DimA/B/C recomputed |
| L1 | ArgumentException not named | caught too |
| L3 | dead code | removed |
| open | L2 crossing-beam top line in elevation at 0; L4 KataBarSplit DimA metadata; L5 parser 369 lines / collector 304 lines | not done |

Tests 1319/1319. Live B01 re-generated with the fixes (same counts, no error). Uniformly offset beam: unit-tested only, CHƯA TEST live.
