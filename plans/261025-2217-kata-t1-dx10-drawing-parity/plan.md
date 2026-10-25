# Kata T1-DX10 drawing parity

Request (2026-10-25): T1-DX10 bars read from Kata Excel do not match Kata's drawing (T2-DY7.dwg). Test in the user's
open Revit file (Add-in Manager), no model copy.

Sources (read-only): T2-DY7.dwg block `T1-DX10 (SL=3; L=10350)` at (11907,26230), elevation origin = outer face of
column C at beam top = (6732, 27830); sections 1-1..5-5 at y 24680. KataB03.xlsm sheet Dam (cells in
`reports/t1-dx10-dam-cells.txt`). Revit: XDMTHCSTH…ES-0001_detached.rvt, beams 9789235 (300x500), 9789239 (300x350).

Beam: C 500 | D 6900 (300x500) | E 500 | F 2150 (300x350, soffit +150) | G 300. Top/bottom main 3Ø18.
C14 3Ø18, E14 3Ø18, G14 "-", D17 3Ø18, I1 True, G1 500, G2 40d, G3 30d, H3 0.2, H5 0.25.

## Gaps (Kata DWG vs HPRebar, sheet geometry, mm from C outer face, z from beam top)

| # | Bar | Kata | HPRebar | Rule |
|---|---|---|---|---|
| 1 | E14 top extra layer 2 | 5650 → through span 2 → leg down at G (x 10255, to −346) | 5650 → 8450, warning "G14 '-' … mới hỗ trợ ở hàng 13" | "-" in row 14 continues the neighbour's row (only row 13 today) |
| 2 | Bottom main span 1, ends | straight into support to outer face − 60, upturn = 30d − straight (440 + 100) both ends | left leg at x 141, up 200; right x 7870, up 75 | bottom anchorage split |
| 3 | D17 bottom extra layer 2 | 1750 … 6150 (1250 from each face) | 1650 … 6250 (1150) | cut rule unknown (B03 F17 also open) |
| 4 | Top main legs | left 250, right 300 (beam 350 deep at G) | left 275, right 270 | within ±30, check after #2 |
| 5 | Stirrups | zones 550–2250 / 2450–5450 / 5650–7350, 7950–8450 / 8650–9300 / 9500–10000 | same zones | ✅ |
| 6 | Bottom main span 2 | 7360 → 10270, up to −22 | 7360 → 10257, up to −40 | ✅ ±33 |

Hypothesis for #3: with I1 True ("Kéo thép gia cường", G1 500) the bottom extra overlaps the top extras by G1:
cut = top extra reach − G1 = 1750 − 500 = 1250 (both ends). Does not explain B01 (I1 not set, 1500) — needs user.

## Phases

| # | Phase | Status |
|---|---|---|
| 1 | `KataT1Dx10DrawingTests` (sheet + DWG numbers in asserts; JSON fixture not exported) | ✅ 13afc38 |
| 2 | Rows 14-16 "-" continuation (gap 1) — only over a "-" support without own lower rows | ✅ 13afc38 |
| 3 | Bottom main anchorage split (gap 2) — Kata schematic vs clash-free legs: user decision | 👤 |
| 4 | Bottom extra cut (gap 3): I1 "Cách điểm cắt thép gối = h" (user 2026-10-25) — also fixes B03 D17/F17 | ✅ 13afc38 |
| 5 | Build R26 ✅; user loads via Add-in Manager in the open file, reads T1-DX10 | 👤 |
