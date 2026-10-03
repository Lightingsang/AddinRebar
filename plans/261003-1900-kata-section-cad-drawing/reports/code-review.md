# Code review — Kata section panel (2026-10-03)

Reviewer verdict 8/10, no Critical/High. Probe: 90 builds (DY7, 3 top layers, 3 bottom numbers, 1200 deep, 600 wide, mirrored) — no throw, no NaN, leaders inside bounds.

| # | Finding | Status |
|---|---|---|
| M1 | 3+ top layers: inner-layer tags pile at same insertion | fixed — row cursor across layers, steps past any tag in the way; test `Tags_of_a_third_top_layer_stand_clear_of_the_second_layer_s` |
| M2 | 2+ bottom middle numbers reach the width dim | fixed — `dimZ = min(Kata rule, lowest bottom row − 145)`; test `The_width_dimension_stays_under_every_tag_under_the_beam` |
| L1 | Empty panel swallows clicks | fixed — no panel when the plan has no cuts |
| L2 | Failed section build retried per repaint | fixed — `KataRebarDrawing.Section` caches null + logs once |
| L3 | Flags ≥ 10 partly clickable | fixed — reach from number length |
| L4 | Narrow canvas (< 600×240) shows no section in Kata mode | kept — contract: "narrow canvas → panel hidden" |
| L5 | Flag click leaves `SelectedColumnIndex` unchanged | kept — re-picking the same table row keeps the flag's section; minor |
| L6 | Dead `markers` parameter | removed |
| L7 | plan.md claimed E500 golden | fixed |

Side note from reviewer: McpShared settings change (AI execution off on start) belongs in its own commit.
