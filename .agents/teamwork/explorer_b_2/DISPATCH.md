# Dispatch: explorer_b_2

**Agent**: explorer_b_2
**Archetype**: teamwork_preview_explorer
**Role**: Explorer #2 for Milestone B (Production Compiler Diagnostics & Seeds)
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_b_2\`
**Parent Conversation ID**: `f5a54e1e-e38f-430b-b7e8-2a44388a5da1`

## Mandatory Documents (READ FIRST)
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Đọc kỹ mục header `## 2026-10-09T14:32:33Z` và yêu cầu R2)
2. `SCOPE.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8\SCOPE.md`

## Objectives
Khảo sát hiện trạng biên dịch và chẩn đoán lỗi (compile diagnostics) cho Milestone B:
1. **Thu thập Compile Diagnostics của 3 Tools Quarantined**:
   - `batch_edit_graph/code.cs`
   - `search_nodes_library/code.cs`
   - `get_active_graph_info/code.cs`
   - Kiểm tra mã nguồn 3 file này: Có phụ thuộc reference nào bị thiếu không? Có syntax hay symbol nào chưa được import?
2. **Khảo sát Production ScriptCompiler & References**:
   - Trong `HPDynamo/HPDynamo.McpBridge/` và `HPDynamo/HPDynamo.Mcp.Server/`:
     * Các reference assemblies nào được nạp qua `DynamoCompilerReferences` (hoặc tương đương)?
     * Các namespaces nào được tự động import qua `HostScriptContracts.DynamoImports`?
     * Global parameters nào được định nghĩa trong `DynamoScriptGlobals`?
   - Khi production `ScriptCompiler` biên dịch 13 embedded seeds:
     * Bộ test `HPDynamo.Mcp.Server.Tests/Seeds/SeedCompilationTests.cs` (hoặc tương tự) đang biên dịch hạt giống ra sao?
     * Điều kiện để 3 tools này biên dịch sạch 100% không còn warning/error là gì?
3. **Đề xuất Fix Strategy**:
   - Các sửa đổi cần thiết về imports, references, hoặc code trong 3 seeds để bảo đảm biên dịch sạch bằng production compiler.

## Scope Boundaries
- BẠN LÀ READ-ONLY EXPLORER: TUYỆT ĐỐI KHÔNG SỬA CODE.
- KHÔNG CHẠY BUILD/TEST.

## Output Requirements
Viết phân tích vào:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_b_2\analysis.md`
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_b_2\handoff.md`
Gửi tin nhắn hoàn tất về cho orchestrator (`f5a54e1e-e38f-430b-b7e8-2a44388a5da1`).
