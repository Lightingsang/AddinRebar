# Dispatch: challenger_a_1

**Agent**: challenger_a_1
**Archetype**: teamwork_preview_challenger
**Role**: Empirical Adversarial Challenger #1 for Milestone A
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_a_1\`
**Parent Conversation ID**: `f5a54e1e-e38f-430b-b7e8-2a44388a5da1`

## Mandatory Documents (READ FIRST)
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Đọc kỹ mục header `## 2026-10-09T14:32:33Z` và yêu cầu R1)
2. `SCOPE.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8\SCOPE.md`
3. Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_a\handoff.md`

## Adversarial Challenge Objectives
Thực hiện kiểm thử thực nghiệm đối kháng độc lập nhằm tìm ra kẽ hở hoặc lỗi tiềm ẩn trong các tính năng đã sửa đổi:
1. **R2-01, R2-02, R2-03 (`batch_edit_graph/code.cs`)**:
   - Thử thách kiểm tra phục hồi giá trị các loại node input (Number, String, Boolean, CodeBlock) khi dryRun hoặc error.
   - Thử thách tình huống thay thế nhiều dây liên tiếp trên cùng 1 input port (A->T, B->T, C->T): Kiểm tra xem rollback có quay về đúng dây ban đầu Orig->T hay không, có bị dừng ở dây trung gian không?
   - Thử thách sửa CodeBlock có sẵn dây vào/ra: Rollback có phục hồi 100% dây nối không?
2. **R2-10 (`get_node_output_values/code.cs`)**:
   - Thử thách payload cực lớn (chuỗi > 2000 ký tự, mảng > 500 phần tử, dung lượng > 64KB): Kiểm tra xem `_truncated` có bị chèn đè vào user dictionary không? Có tuân thủ budget an toàn không?
3. **Thực thi các lệnh kiểm thử**:
   - Chạy `dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj`

## Output Requirements
Viết báo cáo đánh giá thực nghiệm và kết luận (**APPROVE** hoặc **REQUEST_CHANGES**) vào:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_a_1\handoff.md`
Gửi tin nhắn hoàn tất về cho orchestrator (`f5a54e1e-e38f-430b-b7e8-2a44388a5da1`).

## 2026-10-09T15:47:58Z
Received dispatch message:
Bạn là challenger_a_1 phụ trách kiểm thử đối kháng thực nghiệm cho Milestone A thuộc HPDynamo MCP Vòng 2.
Thư mục làm việc: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_a_1\
Nhiệm vụ: Kiểm thử đối kháng thực nghiệm các tính năng đã sửa đổi:
- R2-01, R2-02, R2-03: Thử thách rollback node values (Number/String/Boolean/CodeBlock), thay thế nhiều dây liên tiếp cùng 1 port (first-seen/LIFO), sửa CodeBlock bảo tồn dây nối.
- R2-10: Thử thách payload budget (chuỗi > 2000 ký tự, mảng > 500 phần tử, dung lượng > 64KB) và metadata _truncated tách biệt.
- Chạy test suite: dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj
Viết báo cáo đánh giá và kết luận rõ ràng (APPROVE hoặc REQUEST_CHANGES) vào:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_a_1\handoff.md
Gửi tin nhắn send_message thông báo về cho orchestrator (f5a54e1e-e38f-430b-b7e8-2a44388a5da1).
