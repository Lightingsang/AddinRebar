# Live verify — Kata Export round trip, 2 nhịp, Revit 2026.4 (2026-09-29)

Môi trường: model nháp `kata-rebar-live.rvt` (+ cột 9641457, dầm 9641460 qua MCP → 400 | 6000 | 400 | 4500 | 400, trục 30°, y-offset 50), workbook nháp `kata-rebar-live.xlsm`. Build Debug.R26 deploy, *Load Once*, opt-in MCP bật bằng UIA rồi tắt. Mọi lần Xuất/Đọc chạy sau `kata-scratch-cells.ps1` (từ chối nếu workbook đang kích hoạt không phải bản nháp).

## Sự cố
Lần Xuất đầu (00:5x) ghi vào `C:\kata_pro\Kata.xlsm` gốc (đang mở + kích hoạt do phiên khác/user). Chưa lưu; user đóng Don't Save. Từ đó: kiểm tên workbook đang kích hoạt trước mỗi lần bấm.

## Sheet thử
B11 3f20, B12 2f20, G2 40, G3 30, G4 12, G5 2, G6 8, G7 a100, G8 a200, J9 43/25, H3 0.2 "L từ tâm cột", H5 0.25 "L từ mép cột"; C13 2f20, C14 2f18, **E14 `2f20;2f16`**, G14 2f18; D17 2f18, D18 2f20, F18 2f16; **C25 Đai U / D25 1-2**, **E25 Đai C / F25 2**.

## Kết quả
| Kịch bản | Kết quả |
|---|---|
| Xuất Excel | ✅ hàng 11 400·6000·400·4500·400 vào bản nháp |
| Đọc thép Excel → canvas | ✅ mặt đứng: vùng đai 15Ø8a100/200/100, 11Ø8a100/200/100, gia cường, cốt giá; mặt cắt nhịp 1 "Trên 3 · Dưới 6 · Giá 4"; cảnh báo thép chủ 12500 / 12100 > 11700 |
| Tạo thép Revit (lần 1, 2) | ✅ 5 thép chủ + 10 gia cường gối + 6 gia cường nhịp + 8 cốt giá + 10 bộ móc C/đai trong + 6 bộ đai; Revit warnings 0; lần 2 xoá đúng 45 thanh lần 1 |
| Thép chủ | ✅ trên 43 → 11657 (chân 443), dưới 131 → 11569; host dầm 1, chạy qua dầm 2 không cảnh báo |
| Gối giữa E14 | ✅ 2Ø20 (vế lớn) y ±107, 5400 → mép xa 6712, bẻ xuống tới −512 (cách thép dưới 25); 2Ø16 y ±36, thẳng 5760 (= 6400 − 40·16) → 7800 |
| Gia cường nhịp | ✅ 4.1.x 1250 → 5550 (L/7 = 857 → 850); 4.2.1 7400 → 10700 |
| Cốt giá | ✅ ±111, z −386 / −214; nhịp 1 280 → 6520, nhịp 2 6680 → 11420 (10d = 120 vào gối) |
| Móc C cốt giá | ✅ 15 @400 từ 466 (nhịp 1), 11 @400 từ 6866; 180°, sau sửa cả hai móc quay lên |
| Đai U 1-2 nhịp 1 | ✅ chân −121 / 14, móc 135° hai đầu quay vào lõi; theo 3 vùng đai +8 mm |
| Đai C 2 nhịp 2 | ✅ đứng tại y 14, móc 180° trên ôm tâm thanh 2 (0, −43) |

## Sửa trong lúc verify
- Móc đầu đầu thanh quay ngược: Revit 2026 xét "Right" theo hướng thanh ở **cả hai** đầu (tài liệu ghi "bar behind you") → `KataRebarCurveFactory.CreateHooked` dùng hướng thanh cho cả hai đầu.
- Kiểm hướng rải bộ thanh đọc `GetCenterlineCurves(index)` (trả hình thanh đầu) → đổi sang `GetBarPositionTransform`.

## Chưa làm / còn mở
- Móc C cốt giá: tâm móc cách tâm cốt giá 18 mm vào trong (móc không ôm trọn cốt giá; ôm trọn cần móc qua đai ngoài).
- Đai C tại thanh trên số a: móc dưới không có thanh dưới tương ứng khi thép dưới ít thanh hơn (theo công thức Kata dùng vị trí thanh trên).
- Hộp mặt cắt che phần phải mặt đứng khi cửa sổ hẹp.
- R25/R24 (overload `RebarHookOrientation`) chỉ build, hướng móc chưa kiểm.
- Undo một bước, case âm hình học lệch, IsReverse bật: chỉ unit test lần này.
