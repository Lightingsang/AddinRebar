# DISPATCH — challenger_c_2

## Context
- Project: HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08)
- Orchestrator: orchestrator_8_gen2
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_c_2
- Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\SCOPE.md
- Worker Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c\handoff.md

## Mission & Focus
Thực hiện kiểm thử đối kháng (Adversarial Stress Testing) trên cơ chế Sandbox Isolation & Regression Testing:
1. Thử thách cơ chế phát hiện Revit PID 26264 và Zero-Touch Shield trong `run-benchmark.ps1`:
   - Kiểm tra hành vi khi có process Revit chạy hoặc không chạy.
   - Thử nghiệm xem script có bao giờ cố gắng tắt, kill, hoặc can thiệp vào tiến trình Revit không.
2. Thử thách cơ chế Sandbox Isolation và dọn dẹp:
   - Thử nghiệm xem hàm dọn dẹp có khả năng xóa nhầm file ngoài sandbox `%TEMP%\HPDynamoBenchmark` không (kiểm tra boundary enforcement).
   - Kiểm tra việc nhân bản fixture nhị phân `SampleRebarModel.rvt` và `StandardBenchmarkGraph.dyn`.
3. Kiểm tra toàn bộ 5 test suites .NET để đảm bảo 0 regression.

Đưa ra phán quyết rõ ràng: **APPROVE** hoặc **REQUEST_CHANGES**.
Viết báo cáo vào `handoff.md` và thông báo qua `send_message`.

## 2026-10-09T18:42:07Z
Bạn là challenger_c_2 thuộc dự án HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08).
Thư mục làm việc của bạn: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_c_2
Tài liệu chỉ thị:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (BẮT BUỘC ĐỌC)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_c_2\DISPATCH.md
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\SCOPE.md
- Báo cáo Worker: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c\handoff.md

Nhiệm vụ: Thực hiện kiểm thử đối kháng trên Sandbox Isolation, Zero-Touch Shield (Revit PID 26264), và chạy độc lập toàn bộ 5 test suites .NET để đảm bảo 0 regression.
Đưa ra phán quyết rõ ràng: APPROVE hoặc REQUEST_CHANGES trong handoff.md và thông báo qua send_message.
