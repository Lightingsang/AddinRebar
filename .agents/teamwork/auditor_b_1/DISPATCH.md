# Dispatch: auditor_b_1

**Agent**: auditor_b_1
**Archetype**: teamwork_preview_auditor
**Role**: Forensic Integrity Auditor for Milestone B
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_b_1\`
**Parent Conversation ID**: `f5a54e1e-e38f-430b-b7e8-2a44388a5da1`

## Mandatory Documents (READ FIRST)
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Đọc kỹ mục header `## 2026-10-09T14:32:33Z` và yêu cầu R2)
2. `SCOPE.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8\SCOPE.md`
3. Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_b\handoff.md`

## Forensic Audit Protocol & Verification
Thực hiện kiểm toán toàn vẹn độc lập (Forensic Integrity Audit) đối với Milestone B:
1. **Kiểm tra tính chân thực (Authenticity Verification)**:
   - Tuyệt đối không gian lận: Kiểm tra xem danh sách 25 tools có bị hardcode không?
   - Kiểm tra bảng `runs` trong SQLite `registry.db` (`C:\Users\STR-HP03\AppData\Roaming\HPDynamo\McpServer\registry.db`): Có bị xóa hàng để "làm đẹp" số liệu không? (Toàn bộ 116 rows ban đầu phải còn nguyên vẹn).
   - Kiểm tra mã nguồn các seed: Có logic thực tế hay dùng dummy facade?
2. **Kiểm tra Clean Code Governance**:
   - Kiểm tra giới hạn dòng file (Rule C2 $\le 300$ dòng) cho tất cả các file bị chỉnh sửa:
     * `batch_edit_graph/code.cs` ($\le 300$ dòng)
     * `run_dyn_file/code.cs` ($\le 300$ dòng)
     * `DynamoCompilerReferences.cs` ($\le 300$ dòng)
     * `HostScriptContracts.cs` ($\le 300$ dòng)
   - Kiểm tra không có dead code (Rule Q-B1), không có empty catch block không giải thích (Rule Q-B2).
3. **Kiểm tra An Toàn Hạ Tầng**:
   - Zero touch đối với active Revit PID 26264.
   - Không vi phạm tính host-neutral của `McpShared/` và khả năng tương thích .NET Framework 4.8.
4. **Kiểm tra Test Suite Thực Tế**:
   - Tự chạy và ghi nhận kết quả thực tế của cả 5 bộ test suites:
     * `dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj`
     * `dotnet test HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj`
     * `dotnet test HPDynamo/HPDynamo.E2E.Tests/HPDynamo.E2E.Tests.csproj`
     * `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj`
     * `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj`

## Output Requirements
Viết báo cáo kiểm toán đầy đủ và kết luận (**CLEAN** hoặc **INTEGRITY VIOLATION**) vào:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_b_1\handoff.md`
Gửi tin nhắn hoàn tất về cho orchestrator (`f5a54e1e-e38f-430b-b7e8-2a44388a5da1`).

## 2026-10-09T17:13:47Z
Bạn là auditor_b_1 phụ trách Forensic Integrity Audit cho Milestone B thuộc HPDynamo MCP Vòng 2.

Thư mục làm việc của bạn:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_b_1\

CÁC TÀI LIỆU BẮT BUỘC PHẢI ĐỌC:
1. g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (Mục header ## 2026-10-09T14:32:33Z và yêu cầu R2)
2. g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8\SCOPE.md
3. g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_b\handoff.md
4. g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_b_1\DISPATCH.md

NHIỆM VỤ KIỂM TOÁN ĐỘC LẬP:
- Kiểm tra tính chân thực (Authenticity Verification): Không hardcode tool list, không dummy facade.
- Kiểm tra bảng runs trong C:\Users\STR-HP03\AppData\Roaming\HPDynamo\McpServer\registry.db có bị xóa dòng nào không (phải bảo toàn 100% rows).
- Kiểm tra Clean Code Governance: C2 <= 300 dòng/file, M3 <= 50 dòng/method, 0 dead code, 0 unannotated empty catch.
- Zero Touch Active Revit Session (PID 26264).
- Bảo toàn McpShared host-neutrality và .NET Framework 4.8.
- Tự chạy kiểm chứng thực tế 5 bộ test suites:
  * dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj
  * dotnet test HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj
  * dotnet test HPDynamo/HPDynamo.E2E.Tests/HPDynamo.E2E.Tests.csproj
  * dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj
  * dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj

Viết báo cáo kiểm toán đầy đủ và kết luận (CLEAN hoặc INTEGRITY VIOLATION) vào:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_b_1\handoff.md
Gửi tin nhắn send_message thông báo về cho orchestrator (f5a54e1e-e38f-430b-b7e8-2a44388a5da1).
