# Dispatch Log

## 2026-10-09T14:34:39Z

Chào bạn, bạn là Project Orchestrator (orchestrator_8) chịu trách nhiệm dàn xếp và điều phối toàn diện dự án HPDynamo MCP Vòng 2.

Thư mục làm việc của bạn:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8

Tài liệu yêu cầu gốc:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (Mục header ## 2026-10-09T14:32:33Z)

Nhiệm vụ trọng tâm:
Thực hiện kế hoạch hiệu chỉnh kỹ thuật toàn diện phân hệ HPDynamo MCP Vòng 2 nhằm xử lý triệt để 10 phát hiện (R2-01 đến R2-10) từ báo cáo Codex Review Round 2 (2026-10-09). Dự án được triển khai theo 3 đợt tuần tự nghiêm ngặt:

1. Đợt A — Tính Đúng Đắn Giao Dịch, Canvas Rollback & Evaluation (R2-01..R2-07, R2-09, R2-10):
   - R2-01: Snapshot & Rollback Node Input Values (Number, String, Boolean, CodeBlock...). Phục hồi 100% giá trị gốc khi dryRun = true hoặc exception/cancel.
   - R2-02: Thứ tự phục hồi dây nối khi thay thế nhiều dây trên cùng một input port trong một batch (LIFO/snapshot gốc ban đầu duy nhất).
   - R2-03: Bảo tồn dây nối khi sửa CodeBlock: snapshot ports/connectors, phục hồi cả code, port layout và connectors bị ảnh hưởng khi rollback.
   - R2-04: Bảo toàn kết quả transaction thật sự tại Coordinator: transaction boundary là nguồn sự thật duy nhất, không ghi đè RolledBack = true khi cancel đến muộn sau khi commit thành công.
   - R2-05: Thống nhất evaluation sync & error handling xuyên suốt mọi mode (batch_edit_graph runEvaluation=true, HeadlessGraphRunner, run_dyn_file). Headless mode phải áp dụng inputs đầy đủ và không bao giờ báo success = true khi graph fail.
   - R2-06: Đảm bảo Dynamo scheduler đã dừng hoàn toàn khi timeout/cancel trước khi nhả thread ownership (_busy = 0) hoặc finalize transaction.
   - R2-07: Phân định quyền quản lý transaction bằng cú pháp AST/policy, loại bỏ kiểm tra substring/string match "TransactionManager".
   - R2-09: Cô lập cancel theo session (session, request) và chống cancel-of-cancel đệ quy.
   - R2-10: Kiểm soát ngân sách payload byte & string length, tách biệt metadata _truncated không ghi đè user data. Thêm await cho Assert.ThrowsAsync trong test suite.

2. Đợt B — Khôi Phục Vận Hành Thực Tế & Gỡ Quarantined 3 Tools:
   - Thu thập compile diagnostics từ SQLite registry và compiler cho 3 tools bị quarantined (batch_edit_graph, search_nodes_library, get_active_graph_info).
   - Biên dịch sạch bằng production ScriptCompiler cùng DynamoCompilerReferences, imports và globals.
   - Khôi phục trạng thái registry từ quarantined về published theo đúng workflow chuẩn, khôi phục bề mặt công cụ đầy đủ 25 tools (4 Core + 8 Registry + 13 Seeds).

3. Đợt C — Nâng Cấp Harness Benchmark Đo Lường Trung Thực & Model Isolation (R2-08):
   - R2-08: Harness benchmark run-benchmark.ps1 phân tích đầy đủ JSON-RPC error, MCP isError = true, domain failure; dừng vòng lặp và ghi nhận FAIL/SKIP rõ ràng, tuyệt đối không đánh dấu Success giả tạo.
   - Persistent session đo lường warm runs, lưu lại raw samples, percentiles p50/p95 và byte payload.
   - Bảo vệ tuyệt đối tiến trình Revit đang mở (PID 26264), cô lập fixture thử nghiệm trên file bản sao riêng biệt.

Safety & Governance Constraints:
- Zero Touch Active Revit Session (PID 26264).
- Clean Code Governance: file <= 300 dòng, method <= 50 dòng, 0 dead code, 0 unannotated empty catch.
- Bảo toàn McpShared host-neutrality cho 10 hosts và .NET Framework 4.8.
- 100% test suites pass:
  * HPDynamo.Mcp.Server.Tests (.NET 10.0)
  * HPDynamo.McpBridge.Tests (.NET 10.0-windows7.0)
  * HPDynamo.E2E.Tests (.NET 10.0)
  * HPRebar.McpBridge.Core.Net48Tests (.NET Framework 4.8)
  * HPRebar.Mcp.Server.Core.Tests (.NET 10.0)

Quy trình quản lý:
- Duy trì BRIEFING.md và progress.md trong thư mục làm việc của bạn (.agents/teamwork/orchestrator_8/).
- Phối hợp với các specialist agents (workers, reviewers, challengers, test writers...) theo từng đợt A, B, C và gates tương ứng.
- Khi hoàn tất toàn bộ yêu cầu và tất cả test suites đều pass xanh, gửi báo cáo hoàn thành cho Sentinel để chuẩn bị quy trình Victory Audit độc lập.
