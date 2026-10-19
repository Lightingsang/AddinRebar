# Dispatch: reviewer_a_2

**Agent**: reviewer_a_2
**Archetype**: teamwork_preview_reviewer
**Role**: Code Reviewer #2 for Milestone A
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_a_2\`
**Parent Conversation ID**: `f5a54e1e-e38f-430b-b7e8-2a44388a5da1`

## Mandatory Documents (READ FIRST)
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Đọc kỹ mục header `## 2026-10-09T14:32:33Z` và yêu cầu R1)
2. `SCOPE.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8\SCOPE.md`
3. Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_a\handoff.md`

## Review Focus
Rà soát khách quan và đối kháng toàn diện mã nguồn do worker_a thực hiện:
1. `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs`:
   - Kiểm tra R2-04: Xóa việc ném ngoại lệ sau boundary; trạng thái commit thực sự từ Revit DB được bảo toàn (RolledBack không bị ghi đè thành true sai sự thật).
   - Kiểm tra R2-06: Scheduler drain khi timeout/cancel và giữ `_busy = 1`.
   - Kiểm tra R2-09: Quản lý session cancellation `(SessionId, RequestId)` và re-entrancy guard `_isCancelling`.
2. `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs` & `DynamoTransactionDetector.cs`:
   - Kiểm tra R2-07: Roslyn AST Syntax Walker nhận diện transaction chân thực, bỏ qua comments và string literals.
3. `HPDynamo/HPDynamo.McpBridge/Execution/`:
   - Kiểm tra R2-05: Thống nhất evaluation sync, bắt node lỗi (`ElementState.Error`) và ném ngoại lệ rollback transaction.
4. Chạy kiểm thử xác minh:
   - `dotnet test HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj`
   - `dotnet test HPDynamo/HPDynamo.E2E.Tests/HPDynamo.E2E.Tests.csproj`

## Output Requirements
Viết báo cáo đánh giá và kết luận rõ ràng (**APPROVE** hoặc **REQUEST_CHANGES**) vào:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_a_2\handoff.md`
Gửi tin nhắn hoàn tất về cho orchestrator (`f5a54e1e-e38f-430b-b7e8-2a44388a5da1`).


## 2026-10-09T15:46:32Z
Bạn là reviewer_a_2 phụ trách Review mã nguồn cho Milestone A thuộc HPDynamo MCP Vòng 2.

Thư mục làm việc của bạn:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_a_2\

CÁC TÀI LIỆU BẮT BUỘC PHẢI ĐỌC:
1. g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (Mục header ## 2026-10-09T14:32:33Z và yêu cầu R1)
2. g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8\SCOPE.md
3. g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_a\handoff.md
4. g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_a_2\DISPATCH.md

NHIỆM VỤ:
Rà soát khách quan và đối kháng mã nguồn do worker_a thực hiện tại:
- HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs (R2-04 xóa ngoại lệ muộn, R2-06 scheduler drain, R2-09 session cancel & anti-recursion)
- HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs & DynamoTransactionDetector.cs (R2-07 Roslyn AST syntax walker)
- HPDynamo/HPDynamo.McpBridge/Execution/ (R2-05 unified evaluation sync, bắt lỗi ElementState.Error để rollback)
Chạy kiểm thử xác minh:
- dotnet test HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj
- dotnet test HPDynamo/HPDynamo.E2E.Tests/HPDynamo.E2E.Tests.csproj

Viết báo cáo đánh giá và kết luận rõ ràng (APPROVE hoặc REQUEST_CHANGES) vào:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_a_2\handoff.md
Gửi tin nhắn send_message thông báo về cho orchestrator (f5a54e1e-e38f-430b-b7e8-2a44388a5da1).
