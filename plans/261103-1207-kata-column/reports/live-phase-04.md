# Live — Kata Column Rebar (merged button), 2026-11-03

Setup: 2nd Revit 2026.5 (pid 53952, killed after) on scratch copy of `XDMTHCSTH-HPC-LH_HC-ZZ-M3-ES-0001_sangtq6ANLE.rvt`,
opened **Detach from Central / preserve worksets** (`kata-col-rebar5_detached.rvt`). Scratch Kata copy
`kata-col-rebar.xlsm` active in Excel (checked before every write). Stack C24 = 6 columns
(10075038;9784601;10141454;10141800;10141944;10142052), Select by ID.

| Step | Result |
|---|---|
| Ribbon | one button `Kata Column Rebar` (Kata Column Export gone) ✅ |
| Step 1 page | 7 rows incl. top row TẦNG MÁI "hàng đỉnh", C3 = C24 (STR_ElementName), C4 = 1 (STR_ElementCount) ✅ |
| Write | A–D, K + header written; E–V typed earlier kept (E=3, G=4, I=20, J=500, L=d10a100/200) ✅ |
| Step 2 (re-reads Excel) | 10 starters, 70 main bars, Ø10 hoops 4 zones per storey, 0 ties (3×4 bars, I3 auto) ✅ |
| Create | 94 elements (70 bars + 24 hoop sets) in 4.2 s ✅ |
| Create again | 94 created, 94 of previous run deleted — no duplicates ✅ |
| Visual parity vs VC DWG | not done (3D view not opened in the harness) — CHƯA TEST |

Finding: on a workshared copy opened as local, creation failed — "Can't edit the element until sonlqP8PJ6 resaves the
element to central…" (an element the copy shares with central). Live tests on workshared models must open the copy
detached. Real users run on their own local file, where Revit's own borrowing rules apply (not a tool defect).

## After the review fixes (2026-11-03 17:00, Revit pid 32248, copy `kata-col-rebar7_detached.rvt`)

| Step | Result |
|---|---|
| Step 1 write + bars filled | ✅ |
| Step 2, then I15 = 22 in Excel without reading again, Create | refused "Sheet Cot đã đổi từ lần đọc trước…", page re-read (TẦNG KỸ THUẬT 10Ø22) ✅ |
| Create again | 94 elements ✅; warnings shown: cover type 'Rebar Cover 30mm (2)' created; Rebar Number 12 → 15, 11 → 16 refused |
| I15 back to 20, read, Create | 94 ✅; warnings: Rebar Number 10 → 13, 9 → 14 refused ⚠️ (defect logged in code-review-phase-04.md) |
| Corner bars in one plane (insets/feet/legs along b or crank), deploy 17:5x, copy 14 | 94 ✅, Rebar Number 12/12 = Kata, 0 warnings; rerun 94/94 ✅ |

## Untested branches (2026-11-03 18:20–18:40, copies 15–17 detached, stack C24)

| Variant (sheet) | Result |
|---|---|
| A: E=5 G=5 (I3 auto ties) | 148 elements, 12 tie sets (□ + C), Revit 15 numbers = Kata 15, 0 warnings ✅ |
| B: D15=D16=0 (two round storeys; also a > 75 mm step → cut + dowels) | first try: whole run rolled back, "Revit không tạo được thanh thép có móc '10'" (two hooked arcs refused) ❌ → `KataColumnRoundHoopFactory`: lapped-circle tie shape of the model + `ScaleToBox`, centre/radius checked ±5 mm, fallbacks arcs hooked / unhooked → 87 elements, 14 = 14, 0 warnings ✅ |
| C: E=4 F=2 G=5 H=3 (layer 2 + ties) | 148 elements; Revit 22 numbers for Kata 21: one □ tie of TẦNG 1 shortened by Revit on regenerate (last side 171.0 → 164.5 mm; planned centreline identical to storeys 2–5) ⚠️ open — warning shown on the page. **2026-11-04 copy 26, after the layer-2 rule fix (F = 2 = no row, as VC s5-e4f2g5h3):** 148 elements, 112 bars, 24 hoop sets, 12 tie sets, Revit 21 numbers = Kata 21, 0 warnings; 6 section views, 24 + 4 dims 0 refused, 132 tags ✅ — the shortened tie is gone with the old tie layout |
| Visual check of hook directions / positions in 3D | CHƯA TEST |
