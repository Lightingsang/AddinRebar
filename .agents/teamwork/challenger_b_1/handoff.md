# Empirical Adversarial Challenge Report: Milestone B (Production Compiler Parity)

**Agent**: challenger_b_1  
**Role**: critic, specialist (Empirical Adversarial Challenger)  
**Parent Conversation ID**: `f5a54e1e-e38f-430b-b7e8-2a44388a5da1`  
**Assessment Conclusion**: **APPROVE**  
**Timestamp**: 2026-10-09T17:23:00Z  

---

## 1. Observation

### 1.1. Production ScriptCompiler Direct Compilation of 13 Embedded Seeds
Chạy thực nghiệm kiểm thử trực tiếp trên assembly test bằng lệnh:
`.\HPDynamo\HPDynamo.McpBridge.Tests\bin\Debug\net10.0-windows7.0\HPDynamo.McpBridge.Tests.exe --filter-class "*ProductionSeedCompilationTests*" --output Detailed`

Kết quả thực nghiệm:
```text
xUnit.net v3 Microsoft.Testing.Platform Runner v3.1.0+03a071627b (64-bit .NET 10.0.11)
passed HPDynamo.McpBridge.Tests.Coordinator.ProductionSeedCompilationTests.QuarantinedSeeds_RecoveredAndCompileCleanly (6s 913ms)
passed HPDynamo.McpBridge.Tests.Coordinator.ProductionSeedCompilationTests.Seed_CompilesCleanly_UsingProductionScriptCompiler(name: "create_code_block_node", ...) (1s 177ms)
passed HPDynamo.McpBridge.Tests.Coordinator.ProductionSeedCompilationTests.Seed_CompilesCleanly_UsingProductionScriptCompiler(name: "get_node_output_values", ...) (1s 202ms)
passed HPDynamo.McpBridge.Tests.Coordinator.ProductionSeedCompilationTests.Seed_CompilesCleanly_UsingProductionScriptCompiler(name: "export_graph_to_image", ...) (1s 060ms)
passed HPDynamo.McpBridge.Tests.Coordinator.ProductionSeedCompilationTests.Seed_CompilesCleanly_UsingProductionScriptCompiler(name: "batch_edit_graph", ...) (1s 344ms)
passed HPDynamo.McpBridge.Tests.Coordinator.ProductionSeedCompilationTests.Seed_CompilesCleanly_UsingProductionScriptCompiler(name: "run_dyn_file", ...) (1s 726ms)
passed HPDynamo.McpBridge.Tests.Coordinator.ProductionSeedCompilationTests.Seed_CompilesCleanly_UsingProductionScriptCompiler(name: "validate_dyn_file", ...) (1s 357ms)
passed HPDynamo.McpBridge.Tests.Coordinator.ProductionSeedCompilationTests.Seed_CompilesCleanly_UsingProductionScriptCompiler(name: "inspect_node_type", ...) (1s 101ms)
passed HPDynamo.McpBridge.Tests.Coordinator.ProductionSeedCompilationTests.Seed_CompilesCleanly_UsingProductionScriptCompiler(name: "search_nodes_library", ...) (1s 007ms)
passed HPDynamo.McpBridge.Tests.Coordinator.ProductionSeedCompilationTests.Seed_CompilesCleanly_UsingProductionScriptCompiler(name: "add_node_to_workspace", ...) (977ms)
passed HPDynamo.McpBridge.Tests.Coordinator.ProductionSeedCompilationTests.Seed_CompilesCleanly_UsingProductionScriptCompiler(name: "set_node_properties", ...) (946ms)
passed HPDynamo.McpBridge.Tests.Coordinator.ProductionSeedCompilationTests.Seed_CompilesCleanly_UsingProductionScriptCompiler(name: "connect_node_ports", ...) (911ms)
passed HPDynamo.McpBridge.Tests.Coordinator.ProductionSeedCompilationTests.Seed_CompilesCleanly_UsingProductionScriptCompiler(name: "disconnect_node_ports", ...) (1s 108ms)
passed HPDynamo.McpBridge.Tests.Coordinator.ProductionSeedCompilationTests.Seed_CompilesCleanly_UsingProductionScriptCompiler(name: "get_active_graph_info", ...) (1s 231ms)

Test run summary: Passed!
  total: 14, failed: 0, succeeded: 14, skipped: 0, duration: 22s 368ms
```
Toàn bộ 13/13 embedded seeds được biên dịch trực tiếp qua instance của production `HPRebar.McpBridge.Core.Scripting.ScriptCompiler` khởi tạo với:
- `DynamoCompilerReferences.All` (bao gồm `DynamoCore`, `DynamoCoreWpf`, `DynamoApplications`, `RevitNodes`, `ProtoCore`, `DynamoServices`, `RevitAPI`, `RevitAPIUI`)
- `DynamoCompilerReferences.ImportsFor(refs)`
- `typeof(DynamoScriptGlobals)`
Kết quả biên dịch: `outcome.Succeeded == true`, `outcome.Script != null`, và `outcome.Diagnostics.Count == 0` (0 compilation errors, 0 compilation warnings).

### 1.2. Thẩm tra Kỹ thuật 3 Seeds Từng Bị Quarantined
1. `batch_edit_graph` (`HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs`):
   - Đạt 279 dòng (tuân thủ Rule C2 $\le 300$ dòng).
   - Dùng canonical cast: `var dynamoModel = (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model ?? (dynamo as DynamoModel);` (dòng 7).
   - Không chứa bất kỳ tham chiếu nào đến kiểu unexposed `RevitDynamoModel`.
   - Cơ chế snapshot và rollback: `createdNodeGuids`, `createdWiresBetweenExistingNodes`, `replacedWires` (LIFO restoration), `modifiedProperties` (LIFO restoration), `codeBlockWires`.
   - Khối `catch (Exception) { PerformRollback(); throw; }` bọc an toàn và re-throw trung thực.
   - Biên dịch độc lập: PASSED (1s 344ms).
2. `search_nodes_library` (`HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Library/search_nodes_library/code.cs`):
   - Đạt 72 dòng (tuân thủ Rule C2 $\le 300$ dòng).
   - Dùng canonical cast: `var dynamoModel = (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model ?? (dynamo as DynamoModel);` (dòng 8).
   - Kiểm tra `dynamoModel?.SearchModel == null` trả về kết quả rỗng an toàn, không ném NullReferenceException (dòng 12-24).
   - Bọc `ct.ThrowIfCancellationRequested()` trong vòng lặp tìm kiếm (dòng 28).
   - Biên dịch độc lập: PASSED (1s 007ms).
3. `get_active_graph_info` (`HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Workspace/get_active_graph_info/code.cs`):
   - Đạt 87 dòng (tuân thủ Rule C2 $\le 300$ dòng).
   - Dùng canonical cast: `var dynamoModel = (dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model ?? (dynamo as DynamoModel);` (dòng 4) và `workspace ?? dynamoModel?.CurrentWorkspace;` (dòng 5).
   - Phân loại lỗi chính xác từ `currentWorkspaceModel.Nodes.Where(n => n.State == ElementState.Error || n.State == ElementState.Warning)`.
   - Biên dịch độc lập: PASSED (1s 231ms).

### 1.3. Kiểm Tra ScriptQuality & Clean Code Governance
Chạy thực nghiệm:
`.\HPDynamo\HPDynamo.Mcp.Server.Tests\bin\Debug\net10.0\HPDynamo.Mcp.Server.Tests.exe --filter-class "*ScriptQualityTests*" --output Detailed`

Kết quả thực nghiệm:
```text
passed HPDynamo.Mcp.Server.Tests.Seeds.ScriptQualityTests.AllSeeds_PassScriptQualityWithZeroWarnings (233ms)
passed HPDynamo.Mcp.Server.Tests.Seeds.ScriptQualityTests.Baseline_IsEmpty (4ms)
passed HPDynamo.Mcp.Server.Tests.Seeds.ScriptQualityTests.SeedLibrary_IsEmbedded (3ms)
passed HPDynamo.Mcp.Server.Tests.Seeds.ScriptQualityTests.AllSeeds_PassScriptQualityWithZeroErrors (28ms)
total: 4, failed: 0, succeeded: 4, skipped: 0, duration: 504ms
```
Kiểm tra cấu trúc mã nguồn đối kháng qua Python AST scanner:
- 13/13 file `code.cs` đều có số dòng từ 39 đến 279 dòng $\le 300$ (Rule C2 PASS).
- 0 khối empty catch không chú thích (Rule Q-B2 PASS).
- 0 khối code comment (Rule Q-B1 PASS).
- `Baseline` allowlist trong `ScriptQualityTests` rỗng 100% (không có ngoại lệ/đặc ân nào được cấp).

### 1.4. Kiểm Tra Toàn Bộ Test Suite HPDynamo.McpBridge.Tests
Chạy lệnh: `dotnet test HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj`
Kết quả:
```text
Run tests: 'HPDynamo.McpBridge.Tests.dll' [net10.0-windows7.0|x64]
Passed! - Failed: 0, Passed: 223, Skipped: 0, Total: 223, Duration: 23s 479ms
```

### 1.5. Kiểm Tra Đối Soát Toàn Diện Các Test Suites Liền Kề
- `HPDynamo.Mcp.Server.Tests`: Passed 139 / 139 (5s 721ms).
- `HPDynamo.E2E.Tests`: Passed 209 / 209 (3s 535ms).
- `HPRebar.McpBridge.Core.Net48Tests`: Passed 115 / 115 (3s 709ms).
- `HPRebar.Mcp.Server.Core.Tests`: Passed 915 / 915 (4s 305ms).
- Tổng cộng: **1,601 / 1,601 tests PASSED** (0 failures, 0 skipped, 0 regressions).

### 1.6. Kiểm Tra Trực Tiếp Trạng Thái SQLite registry.db & Live Tools Surface
1. Truy vấn SQLite `registry.db` (`%APPDATA%\HPDynamo\McpServer\registry.db`):
   - `SELECT COUNT(*) FROM runs`: **116** (bảo toàn 100% dữ liệu lịch sử).
   - `SELECT COUNT(*) FROM tools WHERE status='published'`: **13** (toàn bộ 13 seeds published).
   - Cả 3 tools `batch_edit_graph`, `search_nodes_library`, `get_active_graph_info` đều mang `status = 'published'`.
   - Bảng `registry_events` ghi nhận 3 sự kiện `status_changed` tại timestamp `1791563782950` (`detail = 'quarantined -> published'`), cung cấp cutoff mới cho `StabilityRunFilter`.
2. Kiểm tra live MCP Server `tools/list`:
   - Lệnh: `python McpShared/tools/mcp-call.py "HPDynamo.Mcp.Server.exe" tools/list --out "scratch/challenger_dynamo_tools_live.json"`
   - Output: Trả về chính xác **25 tools** (4 Core + 8 Registry + 13 Seeds).
   - Cả 3 tools phục hồi đều có đầy đủ `title`, `description`, và `inputSchema` chuẩn JSON Schema.

---

## 2. Logic Chain

1. **Từ Quan sát 1.1 & 1.2 đến Tính đúng đắn của việc phục hồi Quarantined Tools**:
   Lý do gốc khiến 3 tools bị quarantined ở Vòng 1 là do lỗi biên dịch và phụ thuộc vào `DynamoRevitModel` (một kiểu không được public trong các assembly tiêu chuẩn). Bằng việc chuyển sang canonical pattern `(dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model ?? (dynamo as DynamoModel)` và bổ sung các tham chiếu chuẩn vào `DynamoCompilerReferences.All`, cả 3 tools đã biên dịch sạch sẽ mà không gặp bất kỳ lỗi nào.
2. **Từ Quan sát 1.1 đến Sự tương thích với Production ScriptCompiler**:
   Kiểm thử thực nghiệm không sử dụng mock hay compiler giả lập, mà sử dụng chính `HPRebar.McpBridge.Core.Scripting.ScriptCompiler` (chính là compiler được `DynamoBridgeCoordinator` dùng khi nhận lệnh runtime qua Named Pipe). Thực tế 14/14 test cases trong `ProductionSeedCompilationTests` biên dịch thành công chứng minh 100% các script seeds sẵn sàng chạy runtime mà không phát sinh lỗi biên dịch Roslyn.
3. **Từ Quan sát 1.3 đến Tuân thủ Chuẩn Clean Code Governance**:
   Không có seed nào vượt quá 279 dòng, không có empty catch vô căn cứ, không có dead code, và `Baseline` allowlist hoàn toàn rỗng. Điều này khẳng định mã nguồn đáp ứng đầy đủ các tiêu chí khắt khe của HP Clean Code Governance.
4. **Từ Quan sát 1.4 & 1.5 đến Độ ổn định hệ thống (Zero Regressions)**:
   1,601 / 1,601 tests trên 5 test suites (bao gồm cả .NET 10 và .NET Framework 4.8) đều pass 100%, chứng minh không có bất kỳ hồi quy nào xảy ra đối với các phân hệ khác hoặc các hosts khác trong repo.
5. **Từ Quan sát 1.6 đến Tính toàn vẹn của Dữ liệu Lịch sử & Bề mặt Server**:
   SQLite registry bảo toàn nguyên vẹn 116 lượt chạy và live server process xuất đúng 25 tools, thỏa mãn trọn vẹn yêu cầu R2 của ORIGINAL_REQUEST.

---

## 3. Caveats

1. **Zero Touch Active Revit Session**:
   Theo quy định an toàn bất di bất dịch của dự án, tiến trình Revit 2026 đang mở (PID 26264) của người dùng không bị tác động. Toàn bộ các thử thách biên dịch Roslyn và unit/integration tests được thực hiện độc lập trong môi trường in-process runner với các reference assemblies thực tế của Revit 2026 và Dynamo 3.7.
2. **Offline Assembly Probing**:
   `DynamoCompilerReferences.All` giải quyết các assembly dựa trên probing path trỏ tới thư mục cài đặt Revit 2026 (`C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit`). Các assembly này đã có sẵn trên máy dev và được nạp đầy đủ.

---

## 4. Conclusion

**KẾT LUẬN: APPROVE**

Milestone B (Production Compiler Parity & Surface Recovery 25 Tools) thuộc HPDynamo MCP Vòng 2 đạt đầy đủ và vượt mức các tiêu chí chấp nhận:
1. 100% (13/13) embedded seeds biên dịch thành công bằng chính production `ScriptCompiler` với 0 errors, 0 warnings.
2. 3 công cụ (`batch_edit_graph`, `search_nodes_library`, `get_active_graph_info`) đã được phục hồi hoàn toàn sang trạng thái `published`.
3. `ScriptQualityTests` đạt 0 errors, 0 warnings với `Baseline` allowlist rỗng.
4. Test suite `HPDynamo.McpBridge.Tests` pass 223 / 223 (100%).
5. Bề mặt công cụ MCP Server live trả về chính xác 25 tools.
6. Toàn bộ 1,601 tests trên 5 test suites liên quan đều PASS 100%, zero regressions.

---

## 5. Verification Method

Người nhận handoff có thể tái tạo độc lập và xác minh kết quả thẩm tra bằng các lệnh sau:

1. **Kiểm tra biên dịch 13 seeds bằng Production ScriptCompiler**:
   ```powershell
   .\HPDynamo\HPDynamo.McpBridge.Tests\bin\Debug\net10.0-windows7.0\HPDynamo.McpBridge.Tests.exe --filter-class "*ProductionSeedCompilationTests*" --output Detailed
   ```
   *Kỳ vọng*: 14 passed, 0 failed, 0 skipped.

2. **Kiểm tra ScriptQualityTests**:
   ```powershell
   .\HPDynamo\HPDynamo.Mcp.Server.Tests\bin\Debug\net10.0\HPDynamo.Mcp.Server.Tests.exe --filter-class "*ScriptQualityTests*" --output Detailed
   ```
   *Kỳ vọng*: 4 passed, 0 failed, 0 skipped.

3. **Chạy toàn bộ HPDynamo.McpBridge.Tests**:
   ```powershell
   dotnet test HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj
   ```
   *Kỳ vọng*: 223 passed, 0 failed.

4. **Kiểm tra trạng thái SQLite registry.db**:
   ```powershell
   python -c "import sqlite3, os; p = os.path.expandvars(r'%APPDATA%\HPDynamo\McpServer\registry.db'); c = sqlite3.connect(p); cur = c.cursor(); cur.execute('SELECT COUNT(*) FROM runs'); print('runs:', cur.fetchone()[0]); cur.execute('SELECT COUNT(*) FROM tools WHERE status=\'published\''); print('published:', cur.fetchone()[0]); c.close()"
   ```
   *Kỳ vọng*: `runs: 116`, `published: 13`.

5. **Kiểm tra bề mặt 25 tools trên live server**:
   ```powershell
   python McpShared/tools/mcp-call.py "HPDynamo/HPDynamo.Mcp.Server/bin/Debug/net10.0/HPDynamo.Mcp.Server.exe" tools/list --out "scratch/verify_tools.json"
   ```
   *Kỳ vọng*: 25 tools được ghi nhận trong output.
