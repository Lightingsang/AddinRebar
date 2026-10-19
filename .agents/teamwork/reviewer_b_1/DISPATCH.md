# Dispatch: reviewer_b_1

**Agent**: reviewer_b_1
**Archetype**: teamwork_preview_reviewer
**Role**: Code Reviewer #1 for Milestone B (Seeds & Compiler References)
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_b_1\`
**Parent Conversation ID**: `f5a54e1e-e38f-430b-b7e8-2a44388a5da1`

## Mandatory Documents (READ FIRST)
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Đọc kỹ mục header `## 2026-10-09T14:32:33Z` và yêu cầu R2)
2. `SCOPE.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8\SCOPE.md`
3. Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_b\handoff.md`

## Review Focus
Rà soát khách quan và đối kháng toàn diện mã nguồn do worker_b thực hiện cho Milestone B:
1. **Chuẩn hóa mã nguồn seeds**:
   - Kiểm tra 7 seeds (`batch_edit_graph`, `search_nodes_library`, `get_active_graph_info`, `run_dyn_file`, `inspect_node_type`, `add_node_to_workspace`, `connect_node_ports`):
   - Xác nhận đã loại bỏ `Dynamo.Applications.DynamoRevit.RevitDynamoModel` và thay bằng canonical pattern `(dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model ?? (dynamo as DynamoModel)`.
   - Xác nhận `batch_edit_graph/code.cs` đạt $\le 300$ dòng và bảo toàn 4 biến tracking snapshot.
2. **Compiler References & Imports**:
   - Kiểm tra `DynamoCompilerReferences.cs` và `HostScriptContracts.DynamoImports`.
3. **Thực thi kiểm thử xác minh**:
   - `dotnet test HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj`
   - `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj`

## Output Requirements
Viết báo cáo đánh giá và kết luận rõ ràng (**APPROVE** hoặc **REQUEST_CHANGES**) vào:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_b_1\handoff.md`
Gửi tin nhắn hoàn tất về cho orchestrator (`f5a54e1e-e38f-430b-b7e8-2a44388a5da1`).

## 2026-10-09T17:13:46Z
Bạn là reviewer_b_1 phụ trách Review mã nguồn và compiler references cho Milestone B thuộc HPDynamo MCP Vòng 2.

Thư mục làm việc của bạn:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_b_1\

CÁC TÀI LIỆU BẮT BUỘC PHẢI ĐỌC:
1. g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (Mục header ## 2026-10-09T14:32:33Z và yêu cầu R2)
2. g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8\SCOPE.md
3. g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_b\handoff.md
4. g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_b_1\DISPATCH.md

NHIỆM VỤ:
Rà soát khách quan và đối kháng toàn diện mã nguồn do worker_b thực hiện:
- Kiểm tra 7 seeds đã loại bỏ Dynamo.Applications và dùng pattern canonical chuẩn.
- Kiểm tra batch_edit_graph/code.cs đạt <= 300 dòng và giữ 4 biến snapshot.
- Kiểm tra DynamoCompilerReferences.cs và HostScriptContracts.DynamoImports.
- Chạy kiểm thử:
  * dotnet test HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj
  * dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj

Viết báo cáo đánh giá và kết luận rõ ràng (APPROVE hoặc REQUEST_CHANGES) vào:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_b_1\handoff.md
Gửi tin nhắn send_message thông báo về cho orchestrator (f5a54e1e-e38f-430b-b7e8-2a44388a5da1).
