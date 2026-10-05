# Đợt 2 + 3 — cờ cắt, nội dung 14 mặt cắt (2026-10-05)

## Đợt 2 — cờ cắt (user: giữ 0.1 L, sửa phần rõ)
- Console: 1 cắt tại mặt gối + L/3 (B01 32266.7 = DWG, tuyệt đối). Trước: 2 cắt.
- 7/14 cờ khớp DWG ±2 (5450, 11850, 14300, 18750, 23950, 27950, 32266.7). 7 cờ còn lệch 25–300 mm: quy tắc Kata
  không suy ra được chung với DY7/DY14 (DY7 525/700/150, DY14 550, B01 850/650/600…). Nhịp gộp H+J: Kata cắt 2 đầu như
  HP, giữa khác (Kata 20250 + 22650, HP 21200).
- Test: `KataB01DwgElevationTests.The_cuts_HPRebar_places_*`, `KataSectionCutsTests.A_cantilever_has_one_cut_*`.

## Đợt 3 — nội dung mặt cắt: 14/14 khớp DWG
Dựng mặt cắt canvas tại đúng 14 vị trí cắt của Kata, so fixture: mọi thanh (tâm ±2, Ø) và mọi nét đai (đỉnh ±2, bulge).
| Sửa | Loại | File |
|---|---|---|
| Móc đai ngoài dài 40 cố định (DY7 Ø8 và B01 Ø10 đều 40) | vẽ | `KataSectionStyle.HoopHook` |
| Lớp trong cách lớp ngoài `d_ngoài + 25` mỗi lớp (50 Ø25, 45 Ø20) | vẽ | `KataSectionBars.Lay` |
| Mọi lớp chung lưới x góc (6Ø20 dưới 6Ø25 ±207.5; 2Ø16 ở góc) | vẽ | `KataSectionBars.Lay` |
| U/C ô trống kế thừa nhịp trước khi thép chủ trên giống (F, H, J có U 3-4, C 2, C 5) | **thép 3D** | `KataInnerStirrupLayout.Entries` (R-68) |
| C kê lớp 2 chỉ khi lớp 3–4 thanh (B01 6 thanh: không) | **thép 3D** | `KataLayerSpacerTieLayout.MaxBarsHeld` (R-76, ngưỡng 4 giả định) |
| Gối `trái;phải`, lớp ≥ 2, hai nhịp khác bề rộng: mỗi phía rải riêng (K 2Ø16 ở góc nhịp L) | **thép 3D** | `KataSupportTopBarLayout` (R-46) |
- Test mới: `KataB01DwgSectionTests` (28 = 14 × thép + đai). Sửa test cũ: `KataShapeCodeAndBarMarkTests` (nhịp 2 kế thừa U/C).
- Tổng 1399/1399, golden DY7/DY14 xanh, build R26 OK.

## Còn lại
- Đợt 4: tag/dim mặt cắt (Kata 1-1 chỉ 3 tag: [1], [4], [đai]; HP có thêm U/C, cốt giá…), tag mặt đứng.
- Đợt 5: số hiệu thanh (1…35).
- Đợt 6: Revit — Rebar Number, Extensible Storage; kiểm thép 3D mới (U/C ở F/H/J, C kê bớt, 2Ø16 nhịp L).
