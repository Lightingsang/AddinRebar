# Dispatch: worker_a

**Agent**: worker_a
**Archetype**: teamwork_preview_worker
**Role**: Implementation Worker for Milestone A (R2-01..R2-07, R2-09, R2-10)
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_a\`
**Parent Conversation ID**: `f5a54e1e-e38f-430b-b7e8-2a44388a5da1`

## Mandatory Documents (READ FIRST)
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Đọc kỹ mục header `## 2026-10-09T14:32:33Z` và yêu cầu R1)
2. `SCOPE.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8\SCOPE.md`
3. Explorer Reports:
   - `explorer_a_1`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_a_1\analysis.md` và `handoff.md`
   - `explorer_a_2`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_a_2\analysis.md` và `handoff.md`
   - `explorer_a_3`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_a_3\analysis.md` và `handoff.md`

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Governance & Safety Constraints
- **Zero Touch Active Revit Session**: Tuyệt đối không can thiệp, không inject code, không mở hay đóng model người dùng đang mở trong PID 26264.
- **Clean Code Governance**: Mỗi file C# không vượt quá 300 dòng (Rule C2), mỗi phương thức không vượt quá 50 dòng (Rule M3), 0 dead code (Rule Q-B1), 0 unannotated empty catch (Rule Q-B2).
- **Đặc biệt lưu ý `batch_edit_graph/code.cs`**: Hiện tại đang là 297/300 dòng và có test kiểm tra `ScriptQuality.LineCount(code) <= 300`. BẮT BUỘC giữ nguyên tên 4 biến snapshot (`createdNodeGuids`, `createdWiresBetweenExistingNodes`, `replacedWires`, `modifiedProperties`). Refactor cô đọng các đoạn mã để bổ sung tính năng mà tổng dòng vẫn $\le 300$ dòng.
- **Bảo toàn McpShared Host-Neutrality**: Các thay đổi trong `McpShared/` phải giữ nguyên tính host-neutral, tương thích 100% cho cả 10 CAD/BIM hosts và .NET Framework 4.8.

## Implementation Tasks

### 1. R2-01, R2-02, R2-03 (`HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs`)
- **R2-01 (Node Values Rollback)**: Bổ sung hàm đọc giá trị tổng quát qua reflection `GetNodeValue(NodeModel node)` (đọc `Code`, `Value`, `InputValue`, `StringValue` với `CultureInfo.InvariantCulture`). Cập nhật `origVal` và `RestoreProperty` để phục hồi 100% giá trị cho Number, String, Boolean khi rollback.
- **R2-02 (Wire Replacement Ordering)**: Bổ sung `touchedTargetPorts` HashSet để chỉ ghi nhận `replacedWires` cho cổng mục tiêu trong lần chạm đầu tiên (first-seen). Trong `PerformRollback`, duyệt ngược LIFO danh sách `replacedWires`.
- **R2-03 (CodeBlock Wire Preservation)**: Snapshot toàn bộ connectors gắn với CodeBlock (cả InPorts và OutPorts) vào danh sách snapshot trước khi gọi `SetCodeContent`. Trong `PerformRollback`, tái kết nối các connectors này sau khi phục hồi code.

### 2. R2-04, R2-05, R2-06 (`HPDynamo/HPDynamo.McpBridge/` & seeds)
- **R2-04 (Coordinator Transaction Truth)**: Trong `DynamoBridgeCoordinator.cs`, xóa dòng 108 ném ngoại lệ muộn sau khi boundary đã commit. Transaction boundary trong `RevitTransactionScope` là nguồn sự thật duy nhất (`RolledBack` phản ánh đúng trạng thái thực tế). Cập nhật các test case kỳ vọng sai trong `Milestone3CorrelatedCancelStressTests.cs` (nếu có).
- **R2-05 (Unified Evaluation Sync & Error Policy)**:
  - Trong `batch_edit_graph/code.cs`: Khi `runEvaluation = true`, chờ `EvaluationCompleted`, kiểm tra nếu có node bị lỗi (`ElementState.Error`) thì ném ngoại lệ để rollback transaction.
  - Trong `HeadlessGraphRunner.cs` và `run_dyn_file/code.cs`: Nạp đầy đủ `inputs` cho headless workspace. Chờ `EvaluationCompleted`. Nếu có node lỗi hoặc evaluation fail, BẮT BUỘC ném ngoại lệ để rollback transaction, không trả `success = true`.
- **R2-06 (Scheduler Drain on Cancel/Timeout)**:
  - Trong `DynamoEvaluationSync.cs`: Kiểm tra `linkedCts.Token` trước `runAction()`. Khi cancel/timeout, thực hiện drain scheduler (`HasTaskInProgress == false` và `HasPendingTasks == false`).
  - Trong `DynamoBridgeCoordinator.cs`: Giữ `_busy = 1` cho tới khi scheduler hoàn tất dứt điểm trước khi giải phóng thread ownership (`_busy = 0`).

### 3. R2-07, R2-09, R2-10 (`DynamoExecutionEngine.cs`, `McpShared`, seeds, tests)
- **R2-07 (Transaction AST Syntax Policy)**:
  - Trong `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs`: Loại bỏ hoàn toàn substring checks tại lines 141-155. Xây dựng Roslyn Syntax Walker (`DynamoTransactionDetector`) phân tích AST để nhận diện chính xác các lời gọi Dynamo `TransactionManager.Instance...` hoặc `Revit.Elements...`, bỏ qua comment và string literal.
  - Cập nhật test case trong `Milestone1ChallengerTests.cs` (lines 35-41) để assert comment và string literal chứa từ khóa vẫn cho phép auto-tx (`startAutoTransaction: true`).
- **R2-09 (Session-Scoped Cancel & Anti-Recursion)**:
  - Trong `McpShared/HPRebar.McpBridge.Core` (hợp đồng `ICorrelatedCancelExecutor`, `CancelParams`, `ExecuteRequest`): Bổ sung `string? SessionId` (default `= null` để bảo toàn tương thích cho 10 hosts).
  - Trong `DynamoBridgeCoordinator.cs`: Định danh request theo cặp `(SessionId, RequestId)`. Bổ sung cờ re-entrancy `_isCancelling` chống cancel đệ quy và từ chối cancel đối với control/cancel methods.
- **R2-10 (Payload Budget & Async Tests)**:
  - Trong `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Data/get_node_output_values/code.cs`: Tách metadata `_truncated` ra ngoài dictionary người dùng (đặt ở metadata cấp port), áp dụng giới hạn `MaxStringLength` và UTF-8 payload byte budget.
  - Trong `HPDynamo/HPDynamo.McpBridge.Tests/Coordinator/Milestone2Tests.cs`: Đổi `public void Coordinator_Dispose_...` thành `public async Task`, thêm `await` cho cả 2 lời gọi `Assert.ThrowsAsync` (lines 128, 129).

## Verification Requirements
Worker phải tự chạy các lệnh build và test để kiểm chứng toàn bộ:
1. `dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj`
2. `dotnet test HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj`
3. `dotnet test HPDynamo/HPDynamo.E2E.Tests/HPDynamo.E2E.Tests.csproj`
4. `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj`
5. `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj`
Tất cả các bộ test phải PASS 100% (0 errors, 0 warnings).

## Output Requirements
Viết báo cáo bàn giao đầy đủ vào `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_a\handoff.md` và gửi tin nhắn hoàn tất về cho orchestrator (`f5a54e1e-e38f-430b-b7e8-2a44388a5da1`).
