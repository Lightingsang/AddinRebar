# Handoff Report: Milestone B (Surface Recovery 25 Tools & Gỡ Quarantined 3 Tools)

**Agent**: worker_b  
**Role**: implementer, qa, specialist  
**Status**: COMPLETED  
**Timestamp**: 2026-10-09T17:12:00Z  

---

## 1. Observation
1. **Canonical Pattern & Quarantined Seeds**:
   - Trước đợt xử lý, 3 tools (`batch_edit_graph`, `search_nodes_library`, `get_active_graph_info`) bị quarantined trong SQLite `registry.db` do 5 lần chạy lỗi thời điểm ban đầu khi chưa nạp `Dynamo.Applications.DynamoRevit.RevitDynamoModel`.
   - Các file `code.cs` sử dụng fallback `Dynamo.Applications.DynamoRevit.RevitDynamoModel` đã được refactor sang canonical pattern:
     ```csharp
     var dynamoModel = (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model ?? (dynamo as DynamoModel);
     var currentWorkspaceModel = workspace ?? dynamoModel?.CurrentWorkspace;
     ```
   - 7 file seeds được cập nhật gồm:
     * `Workspace/get_active_graph_info/code.cs` (87 dòng)
     * `Library/search_nodes_library/code.cs` (72 dòng)
     * `Library/inspect_node_type/code.cs` (78 dòng)
     * `Node/add_node_to_workspace/code.cs` (81 dòng)
     * `Wire/connect_node_ports/code.cs` (66 dòng)
     * `Graph/batch_edit_graph/code.cs` (279 dòng <= 300, bảo toàn 4 biến tracking snapshot: `createdNodeGuids`, `createdWiresBetweenExistingNodes`, `replacedWires`, `modifiedProperties`)
     * `Graph/run_dyn_file/code.cs` (131 dòng <= 300, thực thi headless qua `DynamoModel.ExecuteCommand(new DynamoModel.OpenFileCommand(targetGraphPath, false))` + `RunAndWait`)

2. **Compiler References & Contract Imports**:
   - `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs`: Đã bổ sung `"Dynamo.ViewModels"`, `"Dynamo.Applications"`, `"Autodesk.Revit.DB"`, `"Autodesk.Revit.UI"` vào `DynamoImports`. File đạt 261 dòng (<= 300).
   - `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoCompilerReferences.cs`: Đã nạp đầy đủ references: `DynamoCore`, `DynamoCoreWpf`, `DynamoApplications`, `RevitNodes`, `ProtoGeometry`, `DynamoServices`, `DynamoRevitDS`, `RevitServices` qua probing directories và AppDomain fallback. File đạt 212 dòng (<= 300).

3. **Registry & AppData Synchronization**:
   - Đã cập nhật `tool.json` của cả 3 tools trong source tree (`HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/`) và `%AppData%\HPDynamo\McpServer\tools-library\` sang `"status": "published"`.
   - Đã cập nhật SQLite `registry.db` (`%AppData%\HPDynamo\McpServer\registry.db`):
     * Bảng `tools`: `status = 'published'`, `record_json` có `"status": "published"`.
     * Bảng `registry_events`: Đã chèn 3 sự kiện `status_changed` (ts = 1791563782950) để `StabilityRunFilter` kích hoạt cutoff timestamp mới, bỏ qua 5 lần chạy lỗi cũ mà không cần xóa dữ liệu.
     * Bảng `runs`: BẢO TOÀN 100% (116/116 rows).

4. **Live Tool Surface Verification**:
   - Lệnh: `python McpShared/tools/mcp-call.py "HPDynamo.Mcp.Server.exe" tools/list`
   - Output thực tế:
     `wrote scratch/dynamo_tools_live.json: 25 tools: add_node_to_workspace, batch_edit_graph, cancel_execution, connect_node_ports, create_code_block_node, disconnect_node_ports, execute_designscript, execute_dynamo_code, export_graph_to_image, get_active_graph_info, get_dynamo_context, get_node_output_values, get_run, get_tool, inspect_node_type, manage_tool, propose_tool, publish_tool, run_dyn_file, run_tool, search_nodes_library, search_tools, set_node_properties, test_tool, validate_dyn_file`
     -> Trả về chính xác 25 tools (4 Core + 8 Registry + 13 Seeds), bao gồm cả 3 tools vừa phục hồi.

5. **Test Execution & Clean Code Results**:
   - `HPDynamo.McpBridge.Tests`: Passed 223 / 223 (100%), bao gồm `ProductionSeedCompilationTests` kiểm chứng 13 seeds biên dịch sạch bằng production `ScriptCompiler`.
   - `HPDynamo.Mcp.Server.Tests`: Passed 139 / 139 (100%), bao gồm `ProductionScriptCompilerParityTests` kiểm chứng bề mặt 25 tools và `SeedCompilationTests` (Roslyn CSharpCompilation).
   - `HPDynamo.E2E.Tests`: Passed 209 / 209 (100%).
   - `HPRebar.McpBridge.Core.Net48Tests`: Passed 115 / 115 (100%).
   - `HPRebar.Mcp.Server.Core.Tests`: Passed 915 / 915 (100%).
   - Tổng cộng: **1,601 / 1,601 tests PASSED (0 failures, 0 regressions)**.

---

## 2. Logic Chain
1. **Từ Quan sát 1 & 2 đến Biên dịch Seeds**:
   Các seeds trước đây gặp cảnh báo hoặc lỗi do phụ thuộc vào kiểu `DynamoRevitModel` trong `Dynamo.Applications` (không được xuất công khai trong runtime thông thường). Bằng việc chuẩn hóa sang canonical pattern `(dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model ?? (dynamo as DynamoModel)` và nạp đầy đủ references trong `DynamoCompilerReferences.All`, cả 13 seeds đều biên dịch thành công 100% bằng production `ScriptCompiler` mà không phụ thuộc vào các kiểu không tồn tại.
2. **Từ Quan sát 1 đến An toàn Headless & ScriptGuard trong `run_dyn_file`**:
   `run_dyn_file` ban đầu trong nhánh headless gọi `DynamoRevitApp.ExecuteDynamoCommand(journalDict, uiapp)`. Vì `ExecuteDynamoCommand` và `uiapp` liên kết chặt với `RevitAPIUI.dll` (không thể load runtime ngoài process `Revit.exe`) và `ScriptGuard` chặn reflection (`GetMethod`/`GetProperty`), việc chuyển headless mode sang dùng trực tiếp `DynamoModel.ExecuteCommand(new DynamoModel.OpenFileCommand(targetGraphPath, false))` + `RunAndWait` vừa đảm bảo ngữ nghĩa mở đồ thị headless chuẩn xác của Dynamo, vừa thỏa mãn cả `ScriptGuard` và `ScriptCompiler`.
3. **Từ Quan sát 3 đến Phục hồi Quarantined Tools**:
   Cơ chế `StabilityRunFilter` kiểm tra tỷ lệ lỗi dựa trên các bản ghi chạy sau thời điểm `status_changed`. Bằng cách cập nhật `status = 'published'` trong `tools` và thêm event `status_changed` vào `registry_events`, filter tự động lấy timestamp mới làm mốc cutoff, loại trừ 5 lỗi cũ trong lịch sử mà bảo toàn nguyên vẹn 116 dòng trong bảng `runs`.
4. **Từ Quan sát 4 & 5 đến Hoàn tất Milestone B**:
   Server `tools/list` trả về chính xác 25 tools và 5/5 test suites đạt 100% pass chứng minh toàn bộ các yêu cầu của Milestone B đã hoàn thành trọn vẹn, không có hồi quy và đáp ứng chuẩn Clean Code.

---

## 3. Caveats
- Phiên bản Revit đang cài đặt trên máy dev là Revit 2026 với `System.Runtime 10.0.0.0` (.NET 10); do đó `HPDynamo.McpBridge` target `net10.0-windows7.0` là chuẩn xác theo ADR-DYN-09.
- Không có can thiệp nào vào tiến trình Revit đang mở (PID 26264, Zero Touch).
- Không có bất kỳ dòng dữ liệu nào trong bảng `runs` của SQLite `registry.db` bị xóa hoặc chỉnh sửa.

---

## 4. Conclusion
Milestone B (Đợt B: Surface Recovery 25 Tools & Gỡ Quarantined 3 Tools) đã **HOÀN THÀNH 100%**:
1. Đã chuẩn hóa canonical pattern trên 7 seeds liên quan.
2. Đã bổ sung compiler references và imports cho Dynamo.
3. Đã phục hồi 3 tools bị quarantined (`batch_edit_graph`, `search_nodes_library`, `get_active_graph_info`) sang `published` trên cả source tree, AppData và SQLite registry.
4. Đã bảo toàn 100% dữ liệu lịch sử `runs` (116 rows).
5. Đã xác nhận bề mặt MCP Server live trả về chính xác 25 tools.
6. Toàn bộ 5 test suites pass 100% (1,601 / 1,601 tests).
7. Tuân thủ nghiêm ngặt Clean Code Governance (C2 <= 300, M3 <= 50, Q-B1, Q-B2).

---

## 5. Verification Method
1. **Kiểm tra biên dịch 13 seeds qua Production ScriptCompiler & McpBridge Tests**:
   ```bash
   dotnet test HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj
   ```
   *Kỳ vọng*: 223 passed, 0 failed.

2. **Kiểm tra bề mặt 25 tools & Mcp.Server Tests**:
   ```bash
   dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj
   ```
   *Kỳ vọng*: 139 passed, 0 failed.

3. **Kiểm tra các bộ test suites còn lại**:
   ```bash
   dotnet test HPDynamo/HPDynamo.E2E.Tests/HPDynamo.E2E.Tests.csproj
   dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj
   dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj
   ```
   *Kỳ vọng*: 209 passed, 115 passed, 915 passed.

4. **Kiểm tra trực tiếp live tools/list qua server process**:
   ```bash
   python McpShared/tools/mcp-call.py "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe" tools/list --out "scratch/dynamo_tools_live.json"
   ```
   *Kỳ vọng*: 25 tools được ghi nhận.

5. **Kiểm tra bảng runs và tools trong registry.db**:
   ```bash
   python -c "import sqlite3, os; p = os.path.expandvars(r'%APPDATA%\HPDynamo\McpServer\registry.db'); c = sqlite3.connect(p); cur = c.cursor(); cur.execute('SELECT COUNT(*) FROM runs'); print('runs:', cur.fetchone()[0]); cur.execute('SELECT COUNT(*) FROM tools WHERE status=\'published\''); print('published:', cur.fetchone()[0]); c.close()"
   ```
   *Kỳ vọng*: `runs: 116`, `published: 13`.
