# Orchestrator Dispatch Specification — HPGrasshopper Re-audit Remediation (N1–N10)

## Mission
Khắc phục triệt để và toàn diện 10 phát hiện kiểm toán (N1–N10) của hệ thống HPGrasshopper MCP theo kế hoạch tại `plans/reports/261013-0600-hpgrasshopper-reaudit.md`, đảm bảo tính nguyên tử của giao dịch, báo cáo rollback trung thực, hủy bỏ an toàn trong batch solve, hoàn thiện ledger hoàn tác đối tượng baseline, bắt buộc danh tính tài liệu, bắt lỗi gán dữ liệu Player, bảo toàn output trùng tên, giữ nguyên diagnostics khi lỗi, chuẩn hóa schema container và cảnh báo stale context.

## Identity & Paths
- Role: Pure Orchestrator (`orchestrator_15`)
- Subagent Type: `teamwork_preview_orchestrator`
- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_15\`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Header `## 2026-10-12T23:09:55Z`)
- Audit Plan: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\plans\reports\261013-0600-hpgrasshopper-reaudit.md`
- Target Codebase: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\`

## Detailed Requirements

### R1. Khắc phục các phát hiện ưu tiên P1 (N1–N5)
- **N1 — Tính nguyên tử của Seed Rebar (`generate_rebar_curves`)**: Validate trước toàn bộ curve sets (coplanarity, endpoints, bar type...) trước khi gọi tạo rebar; nếu có bất kỳ set nào không hợp lệ sau khi bắt đầu mutation thì phải throw exception để Revit Transaction tự động rollback toàn bộ, ngăn chặn commit một phần khi batch trả failure.
- **N2 — Kết quả Rollback Trung thực (`RevitTransactionScope`)**: Kiểm tra và kết hợp kết quả `CanvasRollbackResult` từ `ICanvasUndoScope` vào `ExecutionOutcome`. Nếu canvas rollback thất bại hoặc chỉ phục hồi một phần, kết quả phải báo lỗi/partial với danh sách errors chi tiết, tuyệt đối không trả `RolledBack = true` hoặc `IsError = false` giả tạo.
- **N3 — Batch Solve trong Biên Giới An Toàn & Hỗ Trợ Cancellation (`CanvasEditor` & `GrasshopperExecutionEngine`)**: Truyền `CancellationToken` và scope vào `ExecuteBatch`; kiểm tra checkpoint token trước khi mutation và trước khi solve/commit; hoãn commit canvas đến khi có kết quả cuối cùng (align với Revit transaction boundary); không để batch tiếp tục commit khi đã có tín hiệu hủy/timeout.
- **N4 — Hoàn thiện Ledger Khôi Phục Đối Tượng Baseline (`CanvasUndoScope`)**: Xử lý chính xác chuỗi thao tác trên đối tượng baseline (đặc biệt là Remove rồi Add lại trong cùng một scope); phân biệt rõ giữa đối tượng gốc và đối tượng mới tạo để rollback không xóa mất đối tượng baseline; snapshot đầy đủ trạng thái trước khi thay đổi thuộc tính component hoặc liên kết dây.
- **N5 — Bắt Buộc Danh Tính Tài Liệu & Đồng Nhất Đường Dẫn (`McpBridgeWorkRequest` & Coordinator)**: Thiết lập `RequireTargetIdentity = true` trên các public route ghi dữ liệu khi có target identity; chuẩn hóa đường dẫn tài liệu nhất quán giữa coordinator cache, context extractor và validator (xử lý đồng bộ trường hợp tài liệu chưa lưu/Untitled hoặc null).

### R2. Khắc phục các phát hiện ưu tiên P2 (N6–N10)
- **N6 — Kiểm Tra Kết Quả Gán Dữ Liệu Player (`PlayerDataTreeHelper`)**: Kiểm tra giá trị Boolean trả về của từng lệnh `AddVolatileData`; stage/validate toàn bộ inputs trước khi gán; ghi nhận chi tiết các input bị từ chối vào `rejectedInputs`; không tự ý bỏ qua các phần tử null/unsupported làm lệch chỉ mục (index shift).
- **N7 — Bảo Toàn Dữ Liệu Output Trùng Tên (`PlayerOutputExtractor`)**: Khi có nhiều output trùng nickname trong danh sách requestedOutputs, bảo toàn toàn bộ giá trị tương ứng (dưới dạng list hoặc suffix GUID phân biệt) mà không ghi đè mất output trước đó.
- **N8 — Bảo Toàn Diagnostics Khi Lỗi & Chuẩn Hóa Casing JSON (`GrasshopperExecutionEngine`)**: Khi Player hoặc script ném lỗi dưới Revit, bảo tồn nguyên vẹn payload chẩn đoán gốc (errors, warnings, rejectedInputs, duration); sử dụng `BridgeJson.Options` (camelCase) khi serialize `JsonElement` trong `MapToExecuteResult` để đảm bảo tương thích client.
- **N9 — Kiểm Tra Schema Container Cho Batch Operations (`CanvasBatchValidator` & `CanvasEditor`)**: Phát hiện và báo lỗi preflight nếu các thuộc tính container danh sách (`componentsToAdd`, `wiresToConnect`, ...) được truyền vào sai định dạng (ví dụ: truyền JSON object/primitive thay vì JSON array), không âm thầm bỏ qua (silently no-op).
- **N10 — Đánh Dấu Stale / Không Khớp Context (`GrasshopperContextExtractor`)**: Đảm bảo snapshot context có tính nhất quán bất biến (immutable), bổ sung timestamp và đánh dấu rõ ràng trạng thái `isStale` hoặc `unavailable` khi không thể lấy snapshot mới từ UI thread; không trình bày các giá trị mặc định như dữ liệu thực tế.

### R3. Kiểm Thử Hồi Quy & Đảm Bảo Tính Toàn Vẹn Hệ Thống
- Mở rộng các bài kiểm thử unit và regression tests cho từng phát hiện N1–N10:
  - Test seed rebar với batch hợp lệ + không hợp lệ bảo đảm không commit một phần.
  - Test fake `ICanvasUndoScope` trả về failure bảo đảm `ExecuteResult` báo failure/partial trung thực.
  - Test cancellation token ngắt batch solve giữa chừng.
  - Test baseline remove → re-add giữ nguyên đối tượng sau rollback.
  - Test `AddVolatileData` thất bại được ghi nhận trong `rejectedInputs`.
  - Test requested outputs trùng nickname giữ trọn vẹn dữ liệu.
  - Test batch validator từ chối container sai kiểu (không phải array).
- Toàn bộ test suites hiện tại và mới phải PASS 100% filter-free.

### R4. Tuân Thủ Clean Code Governance & McpShared Host-Neutrality
- **Bảo toàn McpShared**: Tuyệt đối không chỉnh sửa bất kỳ file nào trong `McpShared/` (0 files modified).
- **Quy chuẩn mã nguồn**:
  - Rule C2: Mỗi file C# không vượt quá 300 dòng code.
  - Rule M3: Mỗi method/function không vượt quá 50 dòng code.
  - Rule Q-B1: 0 dòng mã chết (dead code / commented-out code).
  - Rule Q-B2: 0 catch block rỗng không có chú thích lý do.
  - ADR-0007: 12/12 embedded seeds vượt qua script-quality walker.

## Governance & Rules
1. Pure Orchestrator: Do NOT write code directly. Decompose milestones, spawn specialist subagents (explorers, workers, reviewers, challengers), monitor progress, synthesize results.
2. Maintain `plan.md`, `progress.md`, `context.md`, `BRIEFING.md` in `.agents/teamwork/orchestrator_15/`.
3. Never modify any file in `McpShared/`.
4. Run all tests via filter-free `dotnet test`.
5. When complete, provide structured handoff report in `handoff.md` and report completion back to Sentinel.


## 2026-10-12T23:12:02Z
You are the Project Orchestrator for the HPGrasshopper MCP Re-audit Remediation (N1–N10).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_15\
The authoritative user request is recorded in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (Header ## 2026-10-12T23:09:55Z).
Your full mission specification is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_15\DISPATCH.md.
The re-audit report detailing findings N1–N10 is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\plans\reports\261013-0600-hpgrasshopper-reaudit.md.
Target codebase: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPGrasshopper\.

Key instructions:
1. Maintain your own plan.md, progress.md, context.md, and BRIEFING.md in your working directory.
2. Decompose the remediation of N1–N10 into structured milestones across P1 (N1–N5) and P2 (N6–N10), followed by regression verification and governance check.
3. Spawn specialist subagents (explorers, workers, reviewers, challengers) to implement and test each finding. Do NOT write code directly.
4. Clean code governance:
   - 0 modifications to McpShared/ (host-neutral preservation).
   - Class <= 300 lines (Rule C2), Method <= 50 lines (Rule M3), 0 dead code (Rule Q-B1), 0 unannotated catch (Rule Q-B2).
   - 12/12 embedded seeds pass ADR-0007 script quality walker.
5. All test suites in HPGrasshopper and regression suites in McpShared must pass 100% filter-free.
6. When complete, write your final handoff report in handoff.md and report completion back to the Sentinel.


## 2026-10-13T05:15:08Z
[Sentinel Dispatch — Post-Restart Resume]
Server vừa khởi động lại (restart). Người dùng đã yêu cầu "tiếp tục".

1. Trạng thái trước khi restart:
   - Milestone M1: `worker_m1` đã hoàn tất triển khai mã nguồn và tests (938 tests pass).
   - Bạn đã dispatch các tác tử Reviewer (8925de38, 11bc6110), Challenger (d69c438b, 270fd754), và Auditor (04b19ffe).
2. Yêu cầu phục hồi:
   - Kiểm tra trạng thái các tác tử nhánh hoặc tái khởi động/chạy lại các tác tử đánh giá để hoàn thành Gate M1.
   - Tiếp tục thực hiện tự động toàn bộ kế hoạch khắc phục N1–N10 theo lộ trình Milestones M1 -> M2 (N6–N10) -> M3 (Full Regression & Clean Code Governance) -> M4 (Final Synthesis & Sentinel Handoff).
   - Đảm bảo 100% tests PASS filter-free, 0 file thay đổi trong `McpShared/`, tuân thủ Clean Code (C2, M3, Q-B1, Q-B2).
   - Khi hoàn tất toàn bộ dự án, viết `handoff.md` và gửi thông báo hoàn tất về cho Sentinel để kích hoạt Independent Victory Audit.

Hãy tiếp tục điều phối ngay lập tức.
