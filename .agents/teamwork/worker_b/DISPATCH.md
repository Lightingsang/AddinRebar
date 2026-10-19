# Dispatch: worker_b

**Agent**: worker_b
**Archetype**: teamwork_preview_worker
**Role**: Implementation Worker for Milestone B (Surface Recovery 25 Tools & Gỡ Quarantined 3 Tools)
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_b\`
**Parent Conversation ID**: `f5a54e1e-e38f-430b-b7e8-2a44388a5da1`

## Mandatory Documents (READ FIRST)
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Đọc kỹ mục header `## 2026-10-09T14:32:33Z` và yêu cầu R2)
2. `SCOPE.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_8\SCOPE.md`
3. Explorer Reports:
   - `explorer_b_1`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_b_1\handoff.md` (Registry & Tool Lifecycle)
   - `explorer_b_2`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_b_2\handoff.md` (Compiler Diagnostics & Seeds)

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Governance & Safety Constraints
- **Zero Touch Active Revit Session**: Tuyệt đối không can thiệp, không inject code, không mở hay đóng model người dùng đang mở trong PID 26264.
- **Clean Code Governance**: Mỗi file C# không vượt quá 300 dòng (Rule C2), mỗi phương thức không vượt quá 50 dòng (Rule M3), 0 dead code (Rule Q-B1), 0 unannotated empty catch (Rule Q-B2).
- **Đặc biệt lưu ý `batch_edit_graph/code.cs`**: Hiện tại file đang dưới 300 dòng (khoảng 247-280 dòng), BẮT BUỘC giữ nguyên tổng số dòng $\le 300$ và 4 biến snapshot.

## Implementation Tasks

### 1. Chuẩn Hóa Mã Nguồn Seeds
- Trong các seed bị ảnh hưởng (`batch_edit_graph`, `search_nodes_library`, `get_active_graph_info`, và nếu cần: `run_dyn_file`, `inspect_node_type`, `add_node_to_workspace`, `connect_node_ports`):
  * Loại bỏ hoàn toàn fallback tĩnh `?? Dynamo.Applications.DynamoRevit.RevitDynamoModel`.
  * Thay thế bằng pattern chuẩn tắc an toàn:
    `var dynamoModel = (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model ?? (dynamo as DynamoModel);`
    và `var currentWorkspace = dynamoModel?.CurrentWorkspace;`

### 2. Cập Nhật Production Compiler References & Imports
- Kiểm tra `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoCompilerReferences.cs` và `HostScriptContracts.DynamoImports`:
  * Bổ sung các references cần thiết từ Revit/Dynamo install path nếu có (`DynamoApplications.dll`, `RevitNodes.dll`...) vào `All`.
  * Đảm bảo `ImportsFor` nạp đúng các namespace.

### 3. Khôi Phục Trạng Thái Registry & Unquarantine
- Cập nhật các file `tool.json` của 3 tools trong source tree và trong `%AppData%\HPDynamo\McpServer\tools-library\` sang `"status": "published"`.
- Cập nhật cơ sở dữ liệu registry SQLite tại `%AppData%\HPDynamo\McpServer\registry.db`:
  * Cập nhật `tools` table cho 3 tools về `status = 'published'`.
  * Ghi nhận sự kiện `status_changed` vào `registry_events` để `StabilityRunFilter` thiết lập cutoff timestamp mới, loại bỏ 5 lần chạy lỗi cũ khỏi cửa sổ tính toán stability.
  * BẢO TOÀN 100% dữ liệu lịch sử trong bảng `runs`, không xóa bất kỳ dòng nào của người dùng.
- Khôi phục bề mặt công cụ MCP (`tools/list`) hiển thị đầy đủ và chính xác **25 tools** (4 Core + 8 Registry + 13 Seeds).

### 4. Bổ Sung Kiểm Thử Production Compiler Parity
- Trong `HPDynamo.Mcp.Server.Tests/Seeds/`:
  * Thêm test case xác nhận toàn bộ 13 seeds biên dịch thành công 100% bằng chính production `ScriptCompiler` với 0 errors và 0 warnings.
  * Kiểm tra bề mặt tool list đủ 25 tools.

## Verification Requirements
Worker phải tự chạy và kiểm chứng toàn bộ 5 test suites:
1. `dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj`
2. `dotnet test HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj`
3. `dotnet test HPDynamo/HPDynamo.E2E.Tests/HPDynamo.E2E.Tests.csproj`
4. `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj`
5. `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj`

## Output Requirements
Viết báo cáo bàn giao vào:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_b\handoff.md`
Gửi tin nhắn thông báo về cho orchestrator (`f5a54e1e-e38f-430b-b7e8-2a44388a5da1`).
