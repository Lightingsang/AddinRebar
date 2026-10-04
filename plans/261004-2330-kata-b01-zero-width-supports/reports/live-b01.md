# Live B01 after N1–N3 (2026-10-04)

Setup: second Revit 2026.4 (test pid, killed after) on a copy of `Dam kata test.rvt` (scratchpad `live/b01-test.rvt`),
Debug.R26 DLL; user's Revit untouched (old DLL renamed in Addins, still loaded there). B01 ids 9790831…9790841 read
from the user's Revit selection via `get_revit_context` (read-only). Active workbook checked = KataB1.xlsm; only
"Đọc thép Excel" pressed. Screenshot `scratchpad/live/b01-t5-read.png`.

| Check | Before | After |
|---|---|---|
| Row 11 vs Revit | blocked: 13 columns vs 10 | ✅ matches: C 400 · 10400 · 400 · 6500 · 400 · 6500 (H+J) · 400 · 6200 · 400 · 2000 · O console |
| Supports of no width | blocked (R-05) | ✅ I merged, O = console warning |
| Remaining [Chặn] | — | N21: console h sheet 1100, Revit 900 |
| Tạo thép | disabled | still disabled (correctly) |

## What Revit shows that the sheet rules still miss
Revit beams of B01: 500x1100 · 500x700 · **500x650** (H+J) · 500x700 · **300x700** (L) · **300x900** (console).
- Depth = B5 − row 21 **+ row 19** (top drop): H 1100 − 400 − 50 = 650; console 1100 − 0 − 200 = 900. R-03 lacks the row 19 term.
- Row 20 plain number = width change: L20 = 300 → L and console 300 wide. HPRebar measures one width per run.
- Generating now would put top bars 50 mm above the concrete in H, 200 mm in the console, and bars outside a 300 beam
  in L/console → the block is right; its message names the depth only.

Next rules needed for B01: N4 top drop (crank 1:6 / split), width change per span (row 20 number).
