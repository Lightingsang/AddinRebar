# Dispatch Assignment

## 2026-10-09T05:39:28Z

Bạn là Project Orchestrator (orchestrator_7).
Working directory của bạn: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_7\
Yêu cầu người dùng chính thức đã được ghi tại: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md

Nhiệm vụ:
Thực hiện kế hoạch hiệu chỉnh toàn diện phân hệ HPDynamo MCP nhằm khắc phục triệt để 8 phát hiện kỹ thuật (RV-01 đến RV-08) từ báo cáo đánh giá độc lập Codex Review 2026-10-09 (tại `plans/261009-1118-hpdynamo-mcp-performance/reports/codex-review-20261009.md`), bảo đảm tính đúng đắn giao dịch Revit/Dynamo, cơ chế rollback canvas toàn diện cho dryRun, an toàn hàng đợi và cách ly hủy bỏ theo request ID, đồng bộ hóa evaluation xuyên suốt core tools và seeds, trích xuất chuẩn xác dữ liệu ProtoCore MirrorData và DesignScript Dictionary, cùng harness benchmark thực nghiệm lưu raw data.

Tài liệu tham chiếu cốt lõi:
- `plans/261009-1118-hpdynamo-mcp-performance/reports/codex-review-20261009.md` (Chi tiết 8 phát hiện RV-01 -> RV-08)
- `plans/261009-1118-hpdynamo-mcp-performance/plan.md`
- `plans/261009-1118-hpdynamo-mcp-performance/audit.md`
- `plans/261009-1118-hpdynamo-mcp-performance/benchmark-plan.md`
- `docs/clean-code/HP_CLEAN_CODE_CORE.md`
- `AGENTS.md`

Chi tiết Yêu cầu (R1 - R4):
### R1. Tính Đúng Đắn Giao Dịch & Canvas Rollback Toàn Vẹn (RV-01, RV-06)
- Chuẩn hóa nguồn `dryRun` duy nhất tại execution boundary: Đảm bảo các script và seed (bao gồm `batch_edit_graph`) nhận biết chính xác cờ `dryRun` từ `ExecuteRequest` (đặc biệt khi `DynamicToolRegistrar` đã bóc tách cờ này khỏi `args`).
- Rollback canvas toàn diện cho `batch_edit_graph`: Kết hợp Dynamo `UndoRecorder` khi ở Canvas UI với danh sách programmatic rollback snapshot (ghi nhận trạng thái dây cũ bị thay thế, giá trị/thuộc tính cũ của nodes có sẵn). Khi `dryRun = true` hoặc phát sinh ngoại lệ/cancel ở bất kỳ thao tác nào, phải hoàn tác 100% thay đổi trên canvas Dynamo và rollback Revit `TransactionGroup`.
- Phân định quyền quản lý Transaction thông minh trong `DynamoExecutionEngine`:
  - Nếu script C# Roslyn thao tác trực tiếp Revit DB (gọi `doc.Create`, set parameter...) với `transaction = "auto"`, kích hoạt `startAutoTransaction: true`.
  - Nếu phát hiện script gọi wrapper `Revit.Elements.` hoặc tự quản lý qua Dynamo `TransactionManager`, đặt `startAutoTransaction: false` để nhường quyền mở child transaction cho Dynamo và chỉ bọc `TransactionGroup` ngoài cùng.

### R2. Đồng Bộ Hóa Evaluation & Queue Ownership Khi Cancel (RV-02, RV-03, RV-05)
- Xác thực kết quả `RunAndWait`: Trong `CanvasGraphRunner`, kiểm tra chặt chẽ giá trị trả về của `DynamoEvaluationSync.RunAndWait`. Nếu evaluation thất bại, timeout hoặc cancel thì không được trả `Success = true`, phải ghi log lỗi và kích hoạt rollback transaction.
- Thống nhất đường Evaluation Sync: Đảm bảo cả `run_dyn_file` và `batch_edit_graph` đều đi qua cơ chế chờ `EvaluationCompleted` trước khi trích xuất kết quả và trạng thái lỗi.
- Bảo toàn Linked Timeout Token: Truyền đúng `linkedToken` từ `RevitTransactionScope` vào runner execution callback thay vì dùng raw work item token.
- Quản lý trạng thái Work Item và Thread Ownership: Quản lý rõ ràng trạng thái (`Queued`, `Running`, `Completed`). Khi cancellation xảy ra trong lúc work đang chạy trên Revit thread, không được set `_busy = 0` sớm; giữ thread ownership cho tới khi toàn bộ thao tác thực thi và rollback trên Revit hoàn tất.

### R3. Request-Scoped Cancel & Trích Xuất Dữ Liệu ProtoCore MirrorData (RV-04, RV-07, RV-08)
- Cách ly lệnh Cancel theo Request ID: Bổ sung interface `ICorrelatedCancelExecutor` trong `McpShared/HPRebar.McpBridge.Core` kế thừa hoặc bổ trợ `IBridgeExecutor` (đảm bảo 100% tương thích nhị phân và mã nguồn cho 10 host khác, kể cả net48). `RequestDispatcher` kiểm tra và truyền `requestId` nếu executor hỗ trợ. `DynamoBridgeCoordinator` chỉ hủy request khớp với ID được yêu cầu.
- Sửa Serializer trích xuất MirrorData & Dictionary: Trong `get_node_output_values/code.cs`:
  - Kiểm tra `MirrorData.IsCollection` trước; nếu là collection, duyệt qua `MirrorData.GetElements()` thay vì bỏ qua khi `MirrorData.Data == null`.
  - Nhận diện và trích xuất cấu trúc `DesignScript.Builtin.Dictionary` thông qua các thuộc tính `.Keys` và phương thức `.ValueAtKey()`.
- Kiểm soát ngân sách Payload: Bổ sung giới hạn số lượng phần tử tối đa (breadth budget) và độ sâu đệ quy (depth budget), đồng thời kiểm tra cooperative `cancellationToken` trong quá trình duyệt dữ liệu lớn.

### R4. Benchmark Thực Nghiệm, Raw Data & Đồng Bộ Tài Liệu
- Harness Benchmark Thực Nghiệm: Tạo kịch bản PowerShell `run-benchmark.ps1` thực hiện đo đạc các workload chuẩn trên file model test copy cô lập; lưu file kết quả thô `reports/baseline-vs-upgraded.json` ghi nhận p50/p95, round-trips và UTF-8 payload bytes.
- Hiệu Chỉnh Tài Liệu & Phân Định Bằng Chứng: Cập nhật `audit.md`, `plan.md`, `benchmark-plan.md` làm rõ ranh giới giữa giả thuyết ước tính và số liệu đo đạc thực tế; ghi nhận thực nghiệm TFM .NET 10 của Revit 2026.
- Cập Nhật Skill `hp-mcp-dynamo`: Cập nhật số lượng công cụ lên 25 tools (4 core + 8 registry + 13 seeds) và bổ sung tài liệu hướng dẫn sử dụng `batch_edit_graph`.

Ràng buộc An toàn & Clean Code:
- Tuyệt đối không can thiệp phiên làm việc Revit hiện hành (PID 26264).
- Bảo toàn McpShared: host-neutral, tương thích .NET 10 và .NET Framework 4.8.
- 100% C# .NET Native (ĐIỀU 0: Không dùng Python cho production logic).
- Clean code governance: Lớp <= 300 dòng, method <= 50 dòng, 0 catch rỗng, 0 code comment, Q-W1..Q-W5.
- Duy trì liên tục file `progress.md` và `BRIEFING.md` trong thư mục của bạn.
- Khi hoàn tất mọi hạng mục và 100% test suites pass, hãy gửi báo cáo tổng kết hoàn tất về cho Sentinel để Sentinel kích hoạt Victory Auditor.
