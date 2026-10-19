# DISPATCH — reviewer_c_1

## Context
- Project: HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08)
- Orchestrator: orchestrator_8_gen2
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_c_1
- Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\SCOPE.md
- Worker Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c\handoff.md

## Mission & Focus
Review mã nguồn `HPDynamo/tools/harness/benchmark_session.py` và `test_benchmark_session.py`:
1. Kiểm tra tính đúng đắn của Persistent Session: class `Server`, handshake `initialize`, tái sử dụng process cho warm runs.
2. Kiểm tra bộ thẩm định 3 tầng lỗi (`evaluate_outcome`): JSON-RPC error, MCP `isError=true`, domain failure text.
3. Kiểm tra tính toán phân vị Type 7 chuẩn nội suy tuyến tính (p50, p95), đo lường thời gian $T_{\text{startup}}$, raw samples, và byte UTF-8 counts.
4. Kiểm tra Clean Code Governance: file <= 300 dòng, hàm <= 50 dòng, 0 dead code, 0 empty catch.
5. Chạy unit tests và kiểm tra xem có bất kỳ trường hợp nào bị false-positive hoặc nuốt lỗi không.

Đưa ra phán quyết rõ ràng: **APPROVE** hoặc **REQUEST_CHANGES**.
Viết báo cáo vào `handoff.md` và thông báo qua `send_message`.
## 2026-10-09T18:42:07Z
Sender: 0a4397e4-543d-4476-a901-af2f39506668
Content: Bạn là reviewer_c_1 thuộc dự án HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08).
Thư mục làm việc của bạn: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_c_1
Tài liệu chỉ thị:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (BẮT BUỘC ĐỌC)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_c_1\DISPATCH.md
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\SCOPE.md
- Báo cáo Worker: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c\handoff.md

Nhiệm vụ: Review Benchmark Engine (`HPDynamo/tools/harness/benchmark_session.py`), thẩm định 3 tầng lỗi, phân vị Type 7, persistent session, UTF-8 payload bytes và unit tests `test_benchmark_session.py`.
Đưa ra phán quyết rõ ràng: APPROVE hoặc REQUEST_CHANGES trong handoff.md và thông báo qua send_message.
