# Live verify — Kata Rebar MVP, Revit 2026.4 (2026-09-28)

Môi trường: bản chép `Template_Structural_HoangPhuc_RV2026_V3.rvt` + bản chép `C:\kata_pro\Kata.xlsm` trong scratchpad (bản gốc không mở). Build Debug.R26 deploy. Revit mở bằng Claude, add-in chưa ký trả lời *Load Once*, opt-in MCP bật bằng UIA trong phiên kiểm, tắt sau khi xong. Điều khiển cửa sổ: [revit-kata-ui.ps1](revit-kata-ui.ps1) (UIA theo pid Revit). Đo: `execute_revit_code` transaction none, hệ trục tự tính từ tâm 2 cột + solid instance của dầm (x = 0 mép ngoài cột trái, y = 0 tâm bê tông, z = 0 đỉnh dầm).

Model thử: 2 cột 400×400 xoay 30°, tâm cách 6400 trên trục 30°, cột TẦNG 1 → TẦNG 3; dầm B_300x600 (id 9641060) ở TẦNG 2, **y Offset +50** (đo lại: tâm bê tông lệch location line 50.0 mm). KataExport đo 400 · 6000 · 400 và ghi sheet; ghi thêm B3 DT1, B11 3f20, B12 4f20, G2 40, G3 30, G6 8, G7 a100, G8 a200, J9 43/25. Sheet còn nội dung cũ của template (C14, E14, E15, D17, G4/G5, C25–C27, M25) → đúng là test "Bỏ qua".

## Kết quả

| Lần | Kịch bản | Kết quả |
|---|---|---|
| 1 | J9 43/25 | 7 thép chủ ✅ trên y −107/0/107 z −43 x 43→6757 chân 443; dưới y ±107/±35.7 z −557 x 88→6712 chân 288; song song trục (dot 1.000000). Đai rơi về 45 đai lẻ ⚠️ (phép kiểm hộp đo sai: bỏ bend+hook làm đoạn móc vượt góc) → sửa phép đo |
| — | Parser | ⚠️ lần đầu đếm 16 nhịp: nhãn hàng 10 của template chạy tới AI → dừng ở ô hàng 11 trống đầu tiên (như VBA `save_info`) + test hồi quy |
| 2 | J9 `40` + 1 thanh vẽ tay Partition DT1 | xoá đúng 52 thanh cũ, thanh vẽ tay còn ✅; thép chủ đúng; đai lại rơi về đai lẻ ⚠️: Revit ép bộ đai shape-driven về cover của host (25) khi regenerate |
| 3 | sau `KataStirrupCoverFit` | ✅ 7 thép chủ + 3 bộ đai; y ±110 z −40/−560; chân 440 / 285 (lùi 45 → x 85); đai 15@100 450→1850, 15@200 2000→4800, 15@100 4950→6350, hộp 256×556 tại (−128, −578) |
| Undo | 1 lần `Undo` | ✅ trả đúng trạng thái trước lần 3 (52 thanh cũ + thanh vẽ tay); `Redo` lại |
| Âm | C11 = 460 | ✅ [Chặn] "C11: bề rộng gối 1 trong sheet 460 mm, Revit đo 400 mm (lệch 60 mm)", nút Tạo bị khoá. Phát hiện chọn chiều ngược do nhiễu → sửa (chỉ đảo khi tốt hơn > 2 mm) + test |
| Âm | B11 = 3f21 | ✅ "Chưa có RebarBarType cho: Thép chủ trên Ø21…", không tạo gì (vẫn 11 phần tử) |
| Âm | chọn 2 dầm không thẳng hàng | ✅ [Chặn] "Beam 9641060 is not in line with the first beam…" |
| 4 | bản build cuối, J9 43/25 | ✅ xoá 10, tạo 7 thép chủ + 3 bộ đai M_T1, 0 cảnh báo Revit; mọi số khớp golden (bảng rule-table) ±0.1 mm; log `reversed=false` |

Log Serilog (`%LocalAppData%\HPRebar\logs\hprebar-20260928.log`): mỗi lần có dòng beam/ids/reversed/b×h/segments, rules, từng nhóm thép, từng vùng đai, từng mục skipped, dòng kết quả; lỗi bộ đai ghi kèm lý do.

## Sửa trong lúc verify
- `KataDamSheetParser`: kết thúc danh sách cột ở ô hàng 11 trống đầu tiên (`KataDamSheetColumnListTests`).
- `KataStirrupSetCreator.CheckFirstStirrup`: đo đường tim thật (bend + hook), không bỏ.
- `KataStirrupCoverFit` (mới): mỗi cạnh bộ đai ràng buộc tới cover được đặt khoảng cách = cover host của mặt đó − lớp đai sheet (`// Multi-version: preferred rebar constraint`, R24 dùng `SetPreferredConstraintForHandle`).
- `KataSheetGeometryCheck`: đảo chiều chỉ khi tốt hơn > 2 mm.
- Log lỗi kèm `{Reason}`.

## Chưa làm
- Revit 2025 / 2024 chạy thật (chỉ compile).
- Dầm có cover host khác nhau trên/dưới/bên (code xử lý theo từng mặt, chưa có model thử).
- Chiều "ngược" thật (sheet ghi từ đầu kia, dầm không đối xứng) chỉ có unit test.
