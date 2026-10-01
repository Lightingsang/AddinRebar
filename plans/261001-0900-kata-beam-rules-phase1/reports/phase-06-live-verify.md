# Live verify — Kata beam rules phase 1, Revit 2026.4 (2026-10-01)

Môi trường: bản chép `scratchpad/live/thcphcs2-kata-test.rvt` của `Desktop\Kata\THCPHCS2-HPC-LH_BM_HC-ZZ-M3-ES-0001_detached.rvt` (gốc không mở). Build Debug.R26 deploy, *Load Once*. Workbook nháp `kata-rebar-live.xlsm` (kiểm workbook kích hoạt trước mỗi Xuất/Đọc). Settings mặc định (không có `KataSettings.json`).

Dải dầm: B_200x600 tầng "Sảnh Đón", 9856329 / 9856352 / 9856371, cột 300 → sheet 300 | 4400 | 300 | 3850 | 300 | 3950 | 300 (x = 0 tại mép ngoài cột đầu, trục Y). J9 50/25, G2 40, G3 30, G6 8, G7 a100, G8 a200, H5 0.25 mép, H3 0.2 tâm. B11 2f18, B12 2f18; C13 1f16, C14 2f14, E13 1f18, E14 2f16, G13 1f18, G14 2f16, D18 1f16.

| Kiểm | Kỳ vọng | Đo trong Revit |
|---|---|---|
| Xuất Excel | hàng 11 300·4400·300·3850·300·3950·300 | ✅ |
| Thép chủ trên Ø18 | x 50 → 13350, chân 720 − 250 = 470 → 475 cả 2 đầu | ✅ z −525 = −50 − 475 |
| Thép chủ dưới trái | lùi 87 (hàng 14 inset 46 + 16 + 25), chân 377 → 400 | ✅ x 137, z −150 |
| Thép chủ dưới phải | lùi 43, chân 333 → 350 | ✅ x 13307, z −200 |
| C13 Ø16 | 1400 → cắt lệch 750 + 500 = 1250 → 1550; chân 390 → 400 | ✅ 50..1550, z −450 |
| C14 Ø14 | z −50 − (9 + 30 + 7) = −96, x 96, cắt 1050, chân 356 → 375 | ✅ |
| E13 Ø18 | 3600 / 6100 → 3450 / 6250 | ✅ |
| E14 Ø16 | 3950 / 5750, z −97 | ✅ |
| G13 Ø18 | 7850 / 10150 → 7700 / 10300 | ✅ |
| G14 Ø16 | 8200 / 9800 | ✅ |
| D18 Ø16 | 0.15 × 4400 = 660 → 650: 950 → 4050 | ✅ |
| Đai nhịp 1 (2h = 1200 > 1100) | 350..1450 n12 · 1600..3400 n10 · 3550..4650 n12 | ✅ |
| Đai nhịp 2 | 5050..6150 n12 · 6325..7525 n7 · 7700..8800 n12 | ✅ |
| Đai nhịp 3 | 9200..10300 n12 · 10425..11825 n8 · 11950..13050 n12 | ✅ |
| G1 `-300;11700` | cảnh báo + dùng 500 | ✅ "G1 '-300;11700' không phải một số dương: … dùng thiết lập 500 mm" |
| G1 700, tạo lại | C13 1750 · E13 3250/6450 · G13 7500/10500; xoá 41 thanh cũ | ✅ 41 thanh mới (id ≥ 10405103) |

## Cảnh báo Revit
4 × "Rebar is placed completely outside of its host" — 4 thép chủ, host = dầm giữa 9856352. `Solid.IntersectWithCurve`: thanh trên nằm trong khối dầm 3850 mm, thanh dưới 0 mm; khối dầm 0.4235 m³ < 0.2 × 0.6 × 3.85 = 0.462 → đáy dầm bị cắt bởi phần tử join (điều kiện model). Không do luật giai đoạn 1 (vị trí thép chủ không đổi, chỉ chân móc). Không sửa join trong model của user.

## Chưa kiểm live
- Cảnh báo cốt giá h ≥ 700 (dải thử h 600; workbook có G4/G5 → cốt giá vẽ) — unit test.
- Ô `a;b` vế yếu chạy xuyên gối, hàng ngoài bị chặn — unit test.
- Hộp thiết lập mới (không mở trong lần live này).
- `executionEnabled = true` lúc khởi động Revit (bridge deploy 28/09, dòng `Application.cs:115`) — đã thấy, chờ user.
