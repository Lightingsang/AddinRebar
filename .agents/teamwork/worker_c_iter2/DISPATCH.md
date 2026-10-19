# DISPATCH — worker_c_iter2

## Context
- Project: HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08) — Iteration 2 (Remediation)
- Orchestrator: orchestrator_8_gen2
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c_iter2
- Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (BẮT BUỘC ĐỌC)
- Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\SCOPE.md
- Báo cáo Kế hoạch Sửa đổi từ Explorer:
  `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_iter2\handoff.md`
- Bản vá và code mẫu đã kiểm chứng:
  * `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_iter2\benchmark_remediation.patch`
  * `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_iter2\proposed_benchmark_session.py`
  * `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_iter2\proposed_run-benchmark.ps1`

## Write Ownership
Worker owns exclusively:
- `HPDynamo/tools/harness/benchmark_session.py` và `HPDynamo/tools/benchmark/benchmark_session.py`
- `HPDynamo/tools/harness/run-benchmark.ps1` và `HPDynamo/tools/benchmark/run-benchmark.ps1`
- `HPDynamo/reports/baseline-vs-upgraded.json` và `plans/261009-1118-hpdynamo-mcp-performance/reports/baseline-vs-upgraded.json`

## Nhiệm Vụ Triển Khai
1. **Áp dụng bản vá cho `benchmark_session.py` (đồng bộ cả 2 thư mục `tools/harness` và `tools/benchmark`)**:
   - Thay thế `evaluate_outcome`: phòng thủ kiểu dữ liệu toàn diện (chống crash trên `None`/`str`), bắt buộc `result` có `content` dạng list không rỗng, kiểm tra `joined.strip()` không rỗng, phân loại đúng `is_offline`.
   - Cập nhật `build_workload_record`: tham số hóa `failure_count` lấy tổng số lỗi thực tế (`cold_fail + warm_failures`).
   - Cập nhật `execute_workload`: theo dõi `warm_failures`, dừng ngay khi bridge offline trong warm loop và trả về `SKIP`, bắt buộc trả về `FAIL` nếu toàn bộ warm runs thất bại (cấm trả về `PASS` hoặc `Verified Live`), ghi nhận degraded trung thực nếu có partial warm failures.
2. **Áp dụng bản vá cho `run-benchmark.ps1` (đồng bộ cả 2 thư mục `tools/harness` và `tools/benchmark`)**:
   - Thay thế điều kiện ranh giới trong `Remove-BenchmarkSandbox`: bắt buộc dùng tiền tố chuẩn xác `$requiredPrefix = $sandboxRootCanonical + [System.IO.Path]::DirectorySeparatorChar`, từ chối xóa bất kỳ thư mục nào không nằm strictly bên trong.
3. **Chạy kiểm chứng độc lập**:
   - Chạy `python HPDynamo/tools/harness/test_adversary_benchmark.py`: bắt buộc **17/17 tests Passed (100%)**!
   - Chạy `python HPDynamo/tools/harness/test_benchmark_session.py`: bắt buộc **11/11 tests Passed (100%)**!
   - Chạy `powershell -NoProfile -File HPDynamo/tools/harness/run-benchmark.ps1 -WarmRuns 5 -ColdRuns 1`: xác nhận harness chạy thành công, sandbox được dọn dẹp an toàn, báo cáo JSON trung thực.
   - Chạy toàn bộ 5 test suites .NET để đảm bảo 0 regression:
     * `dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests`
     * `dotnet test HPDynamo/HPDynamo.McpBridge.Tests`
     * `dotnet test HPDynamo/HPDynamo.E2E.Tests`
     * `(cd McpShared && dotnet test HPRebar.McpBridge.Core.Net48Tests)`
     * `(cd McpShared && dotnet test HPRebar.Mcp.Server.Core.Tests)`
4. **Tuân thủ Clean Code Governance**:
   - `benchmark_session.py`: <= 300 dòng (thực tế 271 dòng).
   - `run-benchmark.ps1`: <= 300 dòng (thực tế 219 dòng).
   - Mọi hàm <= 50 dòng, 0 dead code, 0 unannotated empty catch.

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Output Requirements
Viết báo cáo chi tiết vào `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c_iter2\handoff.md`.
Báo cáo lại cho Orchestrator qua `send_message`.


## 2026-10-09T19:03:45Z
Bạn là worker_c_iter2 thuộc dự án HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08) — Iteration 2 (Remediation).
Thư mục làm việc của bạn: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c_iter2

Tài liệu chỉ thị:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (BẮT BUỘC ĐỌC)
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c_iter2\DISPATCH.md
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\SCOPE.md
- Báo cáo và giải pháp chi tiết từ Explorer:
  * g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_iter2\handoff.md
  * g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_iter2\benchmark_remediation.patch
  * g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_iter2\proposed_benchmark_session.py
  * g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_iter2\proposed_run-benchmark.ps1
