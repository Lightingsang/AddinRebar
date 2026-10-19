# DISPATCH — explorer_c_2

## Context
- Project: HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08)
- Orchestrator: orchestrator_8_gen2
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_2
- Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md

## Mission & Scope
Khảo sát cơ chế Persistent Session cho warm runs và đo lường metrics:
1. Khảo sát cách benchmark hiện tại đo đạc (startup/cold runs vs steady-state/warm runs).
2. Kiểm tra hạ tầng persistent session trong `McpShared/tools/` (ví dụ `mcp-session.py` vs `mcp-call.py`, `harness_common.py`) hoặc PowerShell/C# client:
   - Cách duy trì process/pipe MCP server mở xuyên suốt nhiều request để loại trừ chi phí process startup khi đo warm.
   - Cơ chế thu thập raw latency samples (từng lượt chạy, đơn vị ms).
   - Thuật toán tính toán percentiles p50 (median) và p95.
   - Phương thức đo dung lượng UTF-8 byte payload (request và response).
3. Khảo sát file báo cáo `reports/baseline-vs-upgraded.json` (hoặc các file tương tự trong `HPDynamo/reports/`):
   - Cấu trúc schema hiện có (hoặc schema dự kiến).
   - Đảm bảo lưu đầy đủ raw samples và metrics tính toán trung thực, không suy đoán tỷ lệ tăng tốc nếu chưa đo trên cùng fixture.
4. Đề xuất bản thiết kế và workflow đo lường chi tiết cho Worker.

## Output Requirements
Viết báo cáo khảo sát chi tiết vào `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_2\handoff.md`.
Cập nhật `progress.md` định kỳ.
Báo cáo lại cho Orchestrator qua `send_message`.


## 2026-10-09T18:19:21Z
Bạn là explorer_c_2 thuộc dự án HPDynamo MCP Vòng 2.
Thư mục làm việc của bạn: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_2
Tài liệu chỉ thị:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (BẮT BUỘC ĐỌC TRƯỚC TIÊN)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_2\DISPATCH.md
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\SCOPE.md

Nhiệm vụ: Khảo sát cơ chế Persistent Session cho warm runs và đo lường metrics:
1. Khảo sát cách đo đạc startup vs steady-state warm runs (so sánh mcp-call.py vs mcp-session.py hoặc PowerShell client).
2. Thiết kế cơ chế thu thập raw latency samples, percentiles p50/p95, dung lượng UTF-8 byte payload.
3. Khảo sát cấu trúc schema của reports/baseline-vs-upgraded.json.
4. Đề xuất bản thiết kế và workflow đo lường chi tiết cho Worker.
Ghi nhận kết quả vào handoff.md trong thư mục của bạn và gửi thông báo qua send_message.
