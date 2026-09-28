# Phase 03 — Review + live verify Revit 2026

Status: done 2026-09-28 — kết quả: [reports/phase-live-verify.md](reports/phase-live-verify.md).

## Chuẩn bị
1. Revit đóng → `dotnet build HPRebar/HPRebar/HPRebar.csproj -c Debug.R26` (deploy).
2. Revit 2026 mở template `F:\3-SOFT\OneDrive\BIM\02-Hoang Phuc\01-Template\03-Revit\Template_Structural_HoangPhuc_RV2026_V3.rvt`, SaveAs scratch; MCP bridge opt-in bật.
3. `execute_revit_code`: 2 cột 400×400 xoay 30°, tâm cách 6400 trên trục 30°; dầm 300×600 y-offset +50; có RebarBarType Ø8/Ø20 + shape đai kín.
4. Chép `C:\kata_pro\Kata.xlsm` → scratch, mở bản chép. KataExport → C11:E11 = 400·6000·400. Ghi B11 `3f20`, B12 `4f20`, G2 40, G3 30, G6 8, G7 `a100`, G8 `a200`, J9 `43/25`.

## Kiểm (execute_revit_code, transaction none; hệ trục tự tính từ solid dầm + tâm cột)
- 7 thanh thường + 3 bộ đai, host = dầm, Comments = tag.
- Trên y −107/0/107, z −43, x 43→6757, chân 443. Dưới y ±107/±35.7, z −557, chân x 88/6712 dài 288.
- Đai hộp 250×550 tại (−125, −575): 15@100 từ 450; 15@200 từ 2000; 15@100 từ 4950 → 6350. Sai số ±1 mm; song song trục (dot ≥ 0.9999).
- Lần 2 J9 `40` → cover 22, z −40, y ±110; log "deleted 10"; thanh vẽ tay có Partition = B3 còn nguyên; Ctrl+Z một lần xoá hết.
- Âm: D13 `2f18` → skipped; C11 460 → chặn; chọn 2 dầm → chặn; thiếu Ø20 → báo tên, không tạo gì. Log Serilog đủ dòng.
