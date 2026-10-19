# DISPATCH — auditor_c_1

## Context
- Project: HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08)
- Orchestrator: orchestrator_8_gen2
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_c_1
- Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\SCOPE.md
- Worker Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c\handoff.md

## Mission & Focus
Thực hiện Forensic Integrity Audit độc lập trên toàn bộ Milestone C:
1. **Kiểm tra gian lận (Anti-Cheating / Anti-Fabrication)**:
   - Rà soát mã nguồn xem có hardcode kết quả benchmark, gán nhãn "Passed" hoặc "Verified Live" giả tạo không.
   - Kiểm tra xem có facade/dummy logic hay bypass kiểm tra không.
   - Kiểm tra số liệu trong `reports/baseline-vs-upgraded.json` có đúng là kết quả đo lường thực tế hay không.
2. **Kiểm tra Clean Code Governance**:
   - File length <= 300 dòng (Rule C2).
   - Method length <= 50 dòng (Rule M3).
   - 0 dead code (Rule Q-B1).
   - 0 unannotated empty catch (Rule Q-B2).
3. **Kiểm tra Zero Touch Revit PID 26264**:
   - Xác minh mã nguồn hoàn toàn không có lệnh can thiệp, inject code, hoặc mở/đóng model của người dùng.
4. **Kiểm chứng độc lập các bộ test suites**:
   - Chạy độc lập `HPDynamo.Mcp.Server.Tests`, `HPDynamo.McpBridge.Tests`, `HPDynamo.E2E.Tests`, `HPRebar.McpBridge.Core.Net48Tests`, `HPRebar.Mcp.Server.Core.Tests`.
   - Xác minh 100% test suites pass.

Đưa ra phán quyết nhị phân: **CLEAN** hoặc **INTEGRITY VIOLATION**.
Viết báo cáo vào `handoff.md` và thông báo qua `send_message`.


## 2026-10-09T18:42:07Z
Bạn là auditor_c_1 thuộc dự án HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08).
Thư mục làm việc của bạn: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_c_1
Tài liệu chỉ thị:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (BẮT BUỘC ĐỌC)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_c_1\DISPATCH.md
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\SCOPE.md
- Báo cáo Worker: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c\handoff.md

Nhiệm vụ: Thực hiện Forensic Integrity Audit độc lập trên toàn bộ Milestone C:
- Kiểm tra anti-cheating, anti-fabrication, 0 hardcoded test/benchmark results.
- Kiểm tra Clean Code Governance: file <= 300 dòng, hàm <= 50 dòng, 0 dead code, 0 unannotated empty catch.
- Kiểm tra bảo vệ tuyệt đối Revit PID 26264 (Zero Touch).
- Kiểm tra độc lập 100% test suites pass trên cả 5 test projects.
Đưa ra phán quyết nhị phân: CLEAN hoặc INTEGRITY VIOLATION trong handoff.md và thông báo qua send_message.
