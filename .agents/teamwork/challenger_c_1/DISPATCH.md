# DISPATCH — challenger_c_1

## Context
- Project: HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08)
- Orchestrator: orchestrator_8_gen2
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_c_1
- Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\SCOPE.md
- Worker Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c\handoff.md

## Mission & Focus
Thực hiện kiểm thử đối kháng (Adversarial Stress Testing) trên Benchmark Engine (`benchmark_session.py`):
1. Thử nghiệm các trường hợp biên và dị dạng dữ liệu:
   - Phản hồi JSON-RPC bị lỗi cú pháp / cụt.
   - MCP `isError = true` với payload rỗng hoặc chứa text lạ.
   - Domain failure ("Dynamo bridge not connected", script compilation error).
   - Danh sách raw samples rỗng hoặc chỉ có 1 sample (kiểm tra tính toán p50/p95 không crash / chia cho 0).
2. Kiểm tra xem engine có bao giờ trả về "Passed" hoặc "Verified Live" trong bất kỳ kịch bản lỗi nào không.
3. Chạy các kịch bản kiểm thử thực tế và viết test cases đối kháng nếu cần.

Đưa ra phán quyết rõ ràng: **APPROVE** hoặc **REQUEST_CHANGES**.
Viết báo cáo vào `handoff.md` và thông báo qua `send_message`.

## 2026-10-09T18:42:07Z
Bạn là challenger_c_1 thuộc dự án HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08).
Thư mục làm việc của bạn: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_c_1
Tài liệu chỉ thị:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (BẮT BUỘC ĐỌC)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_c_1\DISPATCH.md
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\SCOPE.md
- Báo cáo Worker: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c\handoff.md

Nhiệm vụ: Thực hiện kiểm thử đối kháng (Adversarial Testing) trên Benchmark Engine (`benchmark_session.py`): test các ca biên (malformed response, empty response, missing fields, bridge offline, single sample percentile), chứng minh không bao giờ xảy ra false positive "Passed" hay "Verified Live".
Đưa ra phán quyết rõ ràng: APPROVE hoặc REQUEST_CHANGES trong handoff.md và thông báo qua send_message.

