# Live verify — số hiệu Kata + tag mặt đứng (Revit 2026.4, 2026-10-02)

Env: second Revit on copies of the THCPHCS2 model (user's Revit untouched), workbook `kata-rebar-live.xlsm` (T2-DY7), path Kata Export (Đọc thép Excel → Toàn dải → Tạo thép Revit).

## Build before the review fixes (16:00)
- Canvas: checkbox "Hiện số hiệu" on; tags above (`1+4 2Ø18+1Ø18`, `5 3Ø18`, `1+6 …`, `7 3Ø18`, `1 2Ø18` mid-span) and below (`2+10 …`, `11 3Ø18`, `13 2Ø12`, `6 3Ø18`, `2+12 …`, `3 2Ø18`) — same numbers and texts as the Kata DWG; stirrup zones `(15)/(16)/(17) nØ8aS`. Screenshot `scratchpad/live/marks-read3.png`.
- Revit Schedule Mark after Tạo thép: 1..17 exactly as Kata (6 = E13 + F17, 2 elements / 4 bars; 14 = 9 tie sets / 58 bars; 15/16/17 hoops), partition `T2-DY7` on every element; Fixed Number grouping unchanged.

## After the review fixes (16:17 build: ±1 mm matching, mirror hooks, leader detour, side-bar tag per run)
First attempt: the user's Revit held the MCP pipe; the `doc.PathName` guard refused (changed 0). Re-run 16:45 once the pipe was free, T2-DY14 on a workbook copy:
- Canvas tags = Kata DWG: `1+5 2Ø18+1Ø18`, `6 3Ø18`, `1+7`, `8 3Ø18`, `2+11`, `12 3Ø18`, `3+13`, `14 3Ø18`, `15 2x2Ø12`, `4 2Ø18` ×2, zones (17)/(18)/(19); slid tags' leaders clear of their neighbours. Screenshot `scratchpad/live/marks-dy14.png`.
- Schedule Mark 1..19 = Kata (15 = 2 side-bar elements / 4 bars, 16 = 9 tie sets / 61 bars, 19 = spans 3 + 4, 5 elements), partition `T2-DY14` on all.

## Sections (2026-10-03)
Env: user's Revit (PID 9248, another project) still held the MCP pipe → no MCP. Second Revit on `thcphcs2-sections-test2.rvt` (copy), Structural Plan Tầng 2, beams picked by Manage › Select by ID driven by UIA/Win32 (`scratchpad/select-by-id.ps1`; ribbon buttons not in the UIA tree → click by window coordinates), workbook copies `kata-sections-dy14.xlsm` / `kata-sections-dy7.xlsm` checked active by COM before Đọc thép Excel.
- DY14 (9808496/499/502 + 9929059): elevation cut marks 1..9 with 9 twice (end of span 3 + span 4); card per span: 2-2 x 3125, 5-5 x 9775, 8-8 x 14900, 9-9 x 16425 = Kata DWG. 2-2 content: top 2Ø18 (1), bottom 2Ø18 (2) + 1Ø18 (11), layer 2 3Ø18 (12), C tie (16), hoops (17) Ø8a200 = elevation tags.
- DY7 (9808958/960/962): marks 1..9; 2-2 x 3050, 5-5 x 9775, 8-8 x 14900 = Kata DWG.
- Found + fixed: card fill alpha 235 let the bright CAD text show through → opaque; card covered the right of the run in Toàn dải → framing reserves the card width; side bars labelled twice (upper/lower half) → one label; first run the Kata window closed itself on the 2nd "Tiếp" (no log: the second Revit's sink had nothing to write) — not reproduced in 2 later runs × 9 steps; overlays now wrapped (exception → `Log.Error` once + overlay skipped) so a drawing fault cannot take the window down.
- Screenshots `scratchpad/live/sec3-dy14-*.png`, `sec3-dy7-*.png`. Builds R26/R25/R24 0 errors, Core 881/881.
