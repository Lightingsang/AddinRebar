# Code review — floating section + delete bar groups (2026-10-04)

Reviewer 6.5/10 (probe on DY7: every line removed singly and all together → no exception, numbers contiguous).

| # | Finding | Status |
|---|---|---|
| H1 | Removing a mid-span zone merged the two dense zones into one run across the gap (canvas only) | fixed — `KataStirrupRuns` merges only zones ≤ 1.5 spacings apart; test `Striking_the_middle_zone_of_a_span_leaves_its_two_dense_zones_drawn_apart` |
| M1 | Inner U/C stirrups of a removed zone still generated | fixed — they go with their zone (span + zone name); C ties stay per contract + warning; test `Inner_stirrups_go_with_their_zone_and_the_C_ties_stay` |
| M2 | Ordinal `BarId` keys could name another bar after a re-plan | fixed — key adds layer / y / x0, generation refuses when `KataLayoutRemoval.Fingerprint` of the re-plan differs; test `The_fingerprint_changes_when_the_beam_plans_other_bars` |
| M3 | Esc always handled → window Esc-close lost | fixed — handled only when it closed the panel or dropped a selection |
| M4 | Delete / Ctrl+Z in a modeless window inside Revit | verified live (SendKeys into Revit 2026.4): Delete removed the group, Ctrl+Z restored it, Revit undo/delete untouched |
| M5 | Undrawable section → invisible panel eating input | fixed — panel + "Không vẽ được mặt cắt" + ✕ |
| M6 | Bar-type gate used the full plan | fixed — only diameters still drawn |
| L1 | Open section follows the number, not the place | fixed — remembers the flag's X |
| L2 | Bar lines (6 px) win over column pick | kept — bar pick is the new feature; column picks anywhere else |
| L3 | Settings re-preview drops removals silently | fixed — status says how many were restored |
| L4 | Floating panel can hide the right end of the run | kept — ✕ / Esc, pan |
| L5 | Test gaps | 3 tests added (10 removal tests total) |
