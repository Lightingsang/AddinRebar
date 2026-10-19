# DISPATCH — worker_c

## Context
- Project: HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08)
- Orchestrator: orchestrator_8_gen2
- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c
- Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (BẮT BUỘC ĐỌC)
- Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8_gen2\SCOPE.md
- Explorer Reports to read and implement:
  * g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_1\handoff.md
  * g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_2\handoff.md
  * g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_c_3\handoff.md

## Write Ownership
Worker owns exclusively:
- `HPDynamo/tools/harness/run-benchmark.ps1` (và `HPDynamo/tools/benchmark/` nếu có liên quan)
- `HPDynamo/tools/harness/benchmark_session.py` (tạo mới Persistent Engine)
- `HPDynamo/tools/harness/fixtures/` (các fixture files `StandardBenchmarkGraph.dyn`, `SampleRebarModel.rvt`)
- `HPDynamo/reports/baseline-vs-upgraded.json` & `plans/261009-1118-hpdynamo-mcp-performance/reports/baseline-vs-upgraded.json`

## Nhiệm Vụ Triển Khai
1. **Triển khai `benchmark_session.py`**:
   - Sử dụng `Server` từ `McpShared/tools/harness_common.py` (hoặc `mcp-session.py`) để duy trì persistent process cho warm runs.
   - Đo lường trung thực: $T_{\text{startup}}$ riêng biệt, $T_{\text{cold}}$, $T_{\text{warm}}$ (raw samples mảng, p50/p95 nội suy tuyến tính Type 7, min, max, mean), và UTF-8 payload bytes (request & response).
   - Thẩm định lỗi 3 tầng (JSON-RPC error, MCP `isError=true`, domain failure text như "bridge not connected" hay compile error).
   - Fail-fast: Dừng vòng lặp khi bridge offline hoặc lỗi, không tính latency giả mạo của thông báo lỗi, đánh dấu SKIP hoặc FAIL rõ ràng.
2. **Nâng cấp `run-benchmark.ps1`**:
   - Phát hiện 2 tầng tiến trình Revit: phát hiện PID 26264 cụ thể và bất kỳ tiến trình `Revit` nào đang chạy.
   - Thực thi 5 điều khoản Zero Touch Session Shield bảo vệ tuyệt đối model và canvas của user.
   - Cơ chế Sandbox cô lập trong `try...finally`: tạo `%TEMP%\HPDynamoBenchmark\Session_<GUID>\`, nhân bản fixture `.rvt` và `.dyn` thật, dọn dẹp an toàn khi kết thúc với 3 tầng ràng buộc bảo vệ.
   - Tích hợp gọi `benchmark_session.py` cho phiên đo lường.
   - Loại bỏ toàn bộ các chuỗi gán cứng ("Verified Live", "100%", reduction giả định). Ghi nhận động trạng thái thực tế vào `baseline-vs-upgraded.json`.
   - Exit code chuẩn: exit 1 nếu có FAIL, exit 0 nếu hoàn tất hoặc SKIP hợp lệ.
3. **Thêm Fixtures Chuẩn**:
   - Tạo file fixture `StandardBenchmarkGraph.dyn` (Dynamo 3.7 JSON độc lập, 5 nodes).
   - Đảm bảo tham chiếu đúng model `column-stack-2-storey.rvt` từ `HPRebar/HPRebar.Tests/Fixtures/`.
4. **Chạy Build, Tests & Benchmark**:
   - Chạy 100% test suites trên cả 5 projects, bảo đảm toàn bộ xanh:
     * `dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests`
     * `dotnet test HPDynamo/HPDynamo.McpBridge.Tests`
     * `dotnet test HPDynamo/HPDynamo.E2E.Tests`
     * `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests`
     * `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests`
   - Chạy `pwsh HPDynamo/tools/harness/run-benchmark.ps1` (hoặc python session) để sinh `baseline-vs-upgraded.json` thực tế.
   - Kiểm tra mã nguồn tuân thủ Clean Code Governance (file <= 300 dòng, method <= 50 dòng, 0 dead code, 0 unannotated empty catch).

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Output Requirements
Viết báo cáo chi tiết vào `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c\handoff.md`.
Báo cáo lại cho Orchestrator qua `send_message`.


## 2026-10-09T18:29:23Z
[Message] timestamp=2026-10-09T18:29:23Z sender=0a4397e4-543d-4476-a901-af2f39506668 priority=MESSAGE_PRIORITY_HIGH
Content: Bạn là worker_c thuộc dự án HPDynamo MCP Vòng 2 — Milestone C (Đợt C: Honest Benchmark & Model Isolation — R2-08).
Thư mục làm việc của bạn: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_c
