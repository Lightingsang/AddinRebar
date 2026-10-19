# Dispatch: reviewer_a_1

**Agent**: reviewer_a_1
**Archetype**: teamwork_preview_reviewer
**Role**: Code Reviewer #1 for Milestone A
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_a_1\`
**Parent Conversation ID**: `f5a54e1e-e38f-430b-b7e8-2a44388a5da1`

## Mandatory Documents (READ FIRST)
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Đọc kỹ mục header `## 2026-10-09T14:32:33Z` và yêu cầu R1)
2. `SCOPE.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8\SCOPE.md`
3. Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_a\handoff.md`

## Review Focus
Rà soát khách quan và đối kháng toàn diện mã nguồn do worker_a thực hiện:
1. `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs`:
   - Kiểm tra R2-01: Snapshot và phục hồi node input values (Number, String, Boolean, CodeBlock).
   - Kiểm tra R2-02: Thứ tự hoàn tác LIFO, bảo toàn wire đầu tiên (first-seen).
   - Kiểm tra R2-03: Snapshot & tái kết nối connectors của CodeBlock khi rollback.
   - Kiểm tra Clean Code & ScriptQuality: đếm tổng dòng file ($\le 300$), 4 biến snapshot có bị đổi tên không?
2. `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Data/get_node_output_values/code.cs`:
   - Kiểm tra R2-10: Tách biệt metadata `_truncated` ra ngoài dictionary người dùng, giới hạn string length (2000 chars) và byte budget (64KB).
3. `McpShared/`:
   - Kiểm tra R2-09: Tương thích ngược của `SessionId` đối với 10 hosts khác và .NET Framework 4.8.
4. Chạy kiểm thử xác minh:
   - `dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj`
   - `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj`
   - `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj`

## Output Requirements
Viết báo cáo đánh giá và kết luận rõ ràng (**APPROVE** hoặc **REQUEST_CHANGES**) vào:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_a_1\handoff.md`
Gửi tin nhắn hoàn tất về cho orchestrator (`f5a54e1e-e38f-430b-b7e8-2a44388a5da1`).


## 2026-10-09T15:46:32Z
Bạn là reviewer_a_1 phụ trách Review mã nguồn cho Milestone A thuộc HPDynamo MCP Vòng 2.

Thư mục làm việc của bạn:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_a_1\

CÁC TÀI LIỆU BẮT BUỘC PHẢI ĐỌC:
1. g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (Mục header ## 2026-10-09T14:32:33Z và yêu cầu R1)
2. g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8\SCOPE.md
3. g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_a\handoff.md
4. g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_a_1\DISPATCH.md

NHIỆM VỤ:
Rà soát khách quan và đối kháng mã nguồn do worker_a thực hiện tại:
- HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs (R2-01, R2-02, R2-03, Line budget <= 300, 4 tên biến snapshot giữ nguyên)
- HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Data/get_node_output_values/code.cs (R2-10, _truncated tách biệt, byte budget, string length limit)
- McpShared/ (R2-09, SessionId compatibility)
Chạy kiểm thử xác minh:
- dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj
- dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj
- dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj

Viết báo cáo đánh giá và kết luận rõ ràng (APPROVE hoặc REQUEST_CHANGES) vào:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_a_1\handoff.md
Gửi tin nhắn send_message thông báo về cho orchestrator (f5a54e1e-e38f-430b-b7e8-2a44388a5da1).
