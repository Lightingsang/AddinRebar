# Live — settings combos + bar-end cut marks, 2026-10-08

Setup: test Revit 2026.5 on scratch copy `b03-marks-test.rvt` (copy of 8712357c `live\b01-test.rvt`), Debug.R26 DLL deployed by renaming the locked copy; B03 = 9795052/54/59/61 selected by Select by ID; Excel active = scratch copy `kataB03-cover-test.xlsm` (B3 B03, J9 30/25), KataB03.xlsm re-activated after. User's Revits (18036, 29880) untouched. Every test Revit closed answering "No" to save. No `KataSettings.json` before or after (dialog only cancelled).

| Check | Result |
|---|---|
| Kata Export window: "Cài đặt" button gone | ✅ |
| Generate B03, run 1 | log `long section B03 (id 9820630, created): 46 cut marks drawn, 0 previous marks deleted`; message "Mặt cắt dọc 'B03': 46 móc cắt kết thúc thép" — 46 = Core marks for B03 |
| First attempt orientation | ⚠️ mirrored (grid 6 + console on the left) with box BasisX = local X → fixed: BasisX = −local X, Min.Z = 0 (cut at centre) |
| After fix | grid 1 left, console right, 1:25, far clip 550 (= 250 + 300) |
| Marks (zoomed screenshots) | on bar ends: side bars up, layer-2 top bar down, span bottom bar up, leg tips toward the bar body |
| Run 2 first build | marks replaced (46 deleted / 46 drawn) but view recreated (id 9820630 → 9820741): crop transform ≠ creation box → compare via RightDirection/Origin |
| Run 2 after fix | `id 9820630, reused`, 46 drawn / 46 deleted, message "(thay 46 móc cũ)" |
| Ctrl+Z ×2 | section gone, Undo greyed → one undo entry per run |
| Kata Settings combos (UIA expand) | crank Ø `10…40`, slope `1/4 1/6 1/10 1/12`, coupler `16…50`; defaults 16 / 1/6 / 30; checkbox on |

Not run live: switch off (no section, old marks deleted) — code path only; beam moved/rotated (recreate path) — code path only; user annotation kept in the reused view — inferred from same view id + marks deleted by storage only.

Screens: scratchpad `live\run2-section.png`, `run2-left.png`, `settings-combos.png`.

## After the review fixes (cut plane in front, template cleared, non-fatal step)

| Check (pid 35612, same copy + workbook) | Result |
|---|---|
| Run 1 / run 2 | `id 9820630, created` 46 marks / `id 9820630, reused` 46 drawn, 46 deleted |
| Section properties | View Template `<None>`, 1:25, far clip 1100 (= 2 × (250 + 300)) |
| Marks | on bar ends, on the new cut plane (zoomed screenshot `liveix-left.png`) |
| Ctrl+Z ×2 | section gone, Undo greyed |
