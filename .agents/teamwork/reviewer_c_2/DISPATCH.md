# DISPATCH — reviewer_c_2

## Context
- Project: HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08)
- Orchestrator: orchestrator_8_gen2
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_c_2
- Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\SCOPE.md
- Worker Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c\handoff.md

## Mission & Focus
Review mã nguồn `HPDynamo/tools/harness/run-benchmark.ps1`, fixtures, và báo cáo JSON:
1. Kiểm tra cơ chế nhận diện 2 tầng Revit: PID 26264 cụ thể và quét mọi tiến trình `Revit`.
2. Kiểm tra việc tuân thủ 5 điều khoản Zero-Touch Session Shield bảo vệ model và canvas người dùng.
3. Kiểm tra Sandbox Isolation trong khối `try...finally`: tạo `%TEMP%\HPDynamoBenchmark\Session_<GUID>\`, copy model thật `SampleRebarModel.rvt` và graph `StandardBenchmarkGraph.dyn`.
4. Kiểm tra thuật toán dọn dẹp an toàn (`Remove-BenchmarkSandbox`): bảo đảm không bao giờ xóa ra ngoài thư mục `%TEMP%\HPDynamoBenchmark`, giải phóng GC, retry logic.
5. Kiểm tra file báo cáo JSON (`baseline-vs-upgraded.json`): loại bỏ hoàn toàn các chuỗi gán cứng thành công hay số liệu giả định.

Đưa ra phán quyết rõ ràng: **APPROVE** hoặc **REQUEST_CHANGES**.
Viết báo cáo vào `handoff.md` và thông báo qua `send_message`.
## 2026-10-09T18:42:07Z
Bạn là reviewer_c_2 thuộc dự án HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08).
Thư mục làm việc của bạn: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_c_2
Tài liệu chỉ thị:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (BẮT BUỘC ĐỌC)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_c_2\DISPATCH.md
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\SCOPE.md
- Báo cáo Worker: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c\handoff.md

Nhiệm vụ: Review Harness & Model Isolation (`HPDynamo/tools/harness/run-benchmark.ps1`), 2-tier Revit PID 26264 detection, 5 Zero-Touch shield rules, Sandbox clone & safe cleanup trong try...finally, fixtures, và JSON report schema.
Đưa ra phán quyết rõ ràng: APPROVE hoặc REQUEST_CHANGES trong handoff.md và thông báo qua send_message.

## 2026-10-09T18:47:48Z
**Context**: HPDynamo MCP Vòng 2 — Review Milestone C (Harness & Model Isolation)
**Content**: Orchestrator đang chờ báo cáo handoff của bạn. Bạn đang ở bước nào trong việc kiểm tra run-benchmark.ps1 và sandbox isolation?
**Action**: Vui lòng cập nhật trạng thái hoặc hoàn tất handoff.md và gửi báo cáo cho Orchestrator.
