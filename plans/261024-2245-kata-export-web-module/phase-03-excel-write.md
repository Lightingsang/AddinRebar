# Phase 3 — Ghi sheet Dam của Kata Pro

**Status:** Planned · **Type:** HITL (workbook nháp)

- Port `ExcelComAttach`, `ComLateBinding`, `KataExcelWriter` → `Services/Kata/Excel/`; COM chạy ngoài Revit API thread (không cần queue) nhưng tuần tự (một `SingleFlightRun`).
- Lệnh `kata.excel.probe` → tên workbook đang mở, có sheet `Dam`, đường dẫn; `kata.excel.write {sheet, workbookFullName}` — host **từ chối** nếu workbook đang mở khác `workbookFullName` trang đã xác nhận (chặn ghi nhầm file thật).
- Trang: hiện tên workbook, ô sẽ ghi (diff giá trị cũ → mới), nút Ghi; toast kết quả.
- Cổng: test thuần cho danh sách ô (từ `KataExcelCell`/hợp đồng ô), live với bản sao workbook Kata Pro nháp: đọc lại ô sau ghi == preview; kiểm workbook thật không đổi (LastWriteTime).
