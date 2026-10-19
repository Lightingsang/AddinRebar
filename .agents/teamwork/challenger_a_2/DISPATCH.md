# Dispatch: challenger_a_2

**Agent**: challenger_a_2
**Archetype**: teamwork_preview_challenger
**Role**: Empirical Adversarial Challenger #2 for Milestone A
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_a_2\`
**Parent Conversation ID**: `f5a54e1e-e38f-430b-b7e8-2a44388a5da1`

## Mandatory Documents (READ FIRST)
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Đọc kỹ mục header `## 2026-10-09T14:32:33Z` và yêu cầu R1)
2. `SCOPE.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8\SCOPE.md`
3. Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_a\handoff.md`

## Adversarial Challenge Objectives
Thực hiện kiểm thử thực nghiệm đối kháng độc lập nhằm tìm ra kẽ hở hoặc lỗi tiềm ẩn trong các tính năng đã sửa đổi:
1. **R2-04 (Coordinator Transaction Truth)**:
   - Thử thách tình huống cancel token kích hoạt muộn ngay sau khi action commit thành công vào Revit DB: Kiểm tra `ExecuteResult.RolledBack` có giữ đúng `false` hay không, có bị ghi đè sai sự thật không?
2. **R2-05 (Unified Evaluation Sync & Error Propagation)**:
   - Thử thách chạy đồ thị có node bị lỗi (`ElementState.Error`) hoặc evaluation runtime error: Kiểm tra xem hệ thống có bắt đúng lỗi và rollback toàn bộ transaction không, có báo `success = true` sai không?
3. **R2-06 (Scheduler Drain & Thread Ownership)**:
   - Thử thách timeout/cancel khi scheduler đang xử lý tác vụ: Kiểm tra xem coordinator có giữ `_busy = 1` cho tới khi scheduler xả xong không.
4. **R2-07 (Transaction AST Detector)**:
   - Thử thách các đoạn mã chứa comments hoặc string literals như `// TransactionManager.Instance`, `var s = "TransactionManager";`: Kiểm tra xem có nhận diện đúng và bật auto-tx không?
5. **R2-09 (Session-Scoped Cancellation)**:
   - Thử thách 2 session đồng thời có cùng request ID: Kiểm tra cancel từ session này có bị nhầm sang session khác không? Kiểm tra chống cancel-of-cancel.
6. **Thực thi các lệnh kiểm thử**:
   - Chạy `dotnet test HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj`
   - Chạy `dotnet test HPDynamo/HPDynamo.E2E.Tests/HPDynamo.E2E.Tests.csproj`

## Output Requirements
Viết báo cáo đánh giá thực nghiệm và kết luận (**APPROVE** hoặc **REQUEST_CHANGES**) vào:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_a_2\handoff.md`
Gửi tin nhắn hoàn tất về cho orchestrator (`f5a54e1e-e38f-430b-b7e8-2a44388a5da1`).

## 2026-10-09T15:47:58Z
Received dispatch message:
Bạn là challenger_a_2 phụ trách kiểm thử đối kháng thực nghiệm cho Milestone A thuộc HPDynamo MCP Vòng 2.
Thư mục làm việc: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_a_2\
Nhiệm vụ: Kiểm thử đối kháng thực nghiệm các tính năng đã sửa đổi (R2-04, R2-05, R2-06, R2-07, R2-09), chạy test suites, viết báo cáo handoff.md và gửi thông báo về orchestrator.
