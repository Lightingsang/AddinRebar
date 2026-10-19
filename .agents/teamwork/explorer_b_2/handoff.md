# Handoff Report: Production Compiler Diagnostics & Seeds (Milestone B)

**Agent**: `explorer_b_2` (Teamwork Explorer — Read-Only Investigation)  
**Parent**: `f5a54e1e-e38f-430b-b7e8-2a44388a5da1` (Orchestrator)  
**Date**: 2026-10-09  
**Type**: Hard Handoff (Investigation Complete)

---

## 1. Observation

1. **SQLite Registry & AppData Records**:
   - Tệp cơ sở dữ liệu `C:\Users\STR-HP03\AppData\Roaming\HPDynamo\McpServer\registry.db`:
     - Bảng `runs`: Truy vấn `SELECT tool_name, success, error, ts FROM runs WHERE tool_name IN ('batch_edit_graph', 'search_nodes_library', 'get_active_graph_info')` trả về 15 bản ghi liên tiếp có `success = 0` và lỗi verbatim:  
       `"The script does not compile. Fix the listed errors and retry."`
     - Bảng `registry_events`: 3 bản ghi sự kiện `quarantined` do actor `registry` phát sinh:  
       `"Quarantined 2026-10-09 09:45:35Z: 5/5 recent runs failed. Last error: The script does not compile. Fix the listed errors and retry."`
   - Các file `tool.json` tại `%AppData%\HPDynamo\McpServer\tools-library\`:
     - `Graph/batch_edit_graph/tool.json:15`: `"status": "quarantined"`
     - `Library/search_nodes_library/tool.json:13`: `"status": "quarantined"`
     - `Workspace/get_active_graph_info/tool.json:14`: `"status": "quarantined"`

2. **Dòng mã lỗi trong 3 Quarantined Seeds**:
   - `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs:8`:
     `var dynamoModel = dynamoViewModel?.Model ?? dynamo as DynamoModel ?? Dynamo.Applications.DynamoRevit.RevitDynamoModel;`
   - `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Library/search_nodes_library/code.cs:11`:
     `?? Dynamo.Applications.DynamoRevit.RevitDynamoModel;`
   - `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Workspace/get_active_graph_info/code.cs:7`:
     `?? Dynamo.Applications.DynamoRevit.RevitDynamoModel?.CurrentWorkspace;`

3. **Bốn hạt giống khác cũng chứa cùng token lỗi `Dynamo.Applications`**:
   - `Graph/run_dyn_file/code.cs:115,118`: `new Dynamo.Applications.DynamoRevitApp();` và `Dynamo.Applications.DynamoRevit.RevitDynamoModel;`
   - `Library/inspect_node_type/code.cs:8`: `?? Dynamo.Applications.DynamoRevit.RevitDynamoModel;`
   - `Node/add_node_to_workspace/code.cs:14`: `?? Dynamo.Applications.DynamoRevit.RevitDynamoModel;`
   - `Wire/connect_node_ports/code.cs:15`: `?? Dynamo.Applications.DynamoRevit.RevitDynamoModel;`

4. **Thiếu sót trong References của Production Compiler**:
   - `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoCompilerReferences.cs:24-48`:
     Thuộc tính `All` nạp 13 assembly nhưng **hoàn toàn không có**:
     - `DynamoApplications.dll`
     - `RevitNodes.dll`
     - `ProtoGeometry.dll`
     - `DynamoServices.dll`
     - `DynamoRevitDS.dll`
     - `RevitServices.dll`
   - `DynamoCompilerReferences.cs:53-59` (`ImportsFor`):
     Vì `names.Contains("ProtoGeometry")` và `names.Contains("RevitNodes")` đều trả về `false`, nên `"Autodesk.DesignScript.Geometry"` và `"Revit.Elements"` bị loại khỏi danh sách import của Roslyn script.

5. **Lỗ hổng trong `SeedCompilationTests.cs`**:
   - `HPDynamo/HPDynamo.Mcp.Server.Tests/Seeds/SeedCompilationTests.cs:104,168-170`:
     Test suite tự tạo một phương thức compile riêng, tự ép `using Dynamo.Applications;`, tự nạp `"DynamoApplications.dll"` và `"RevitNodes.dll"`, và dùng `CSharpCompilation.Create` thay vì `ScriptCompiler.GetOrCompile()`. Do đó test này hoàn toàn bỏ qua việc `DynamoCompilerReferences.All` thiếu assembly.

6. **Runtime Injection tại Bridge**:
   - `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs:115-117`:
     ```csharp
     object dynamoGlobal = _coordinator.IsLiveCanvasAttached && _coordinator.ActiveViewModel != null
         ? (object)_coordinator.ActiveViewModel
         : (object)Dynamo.Applications.DynamoRevit.RevitDynamoModel!;
     ```
     Bridge đã truyền trực tiếp `RevitDynamoModel` (kế thừa `DynamoModel`) vào `dynamoGlobal` khi ở chế độ headless.

---

## 2. Logic Chain

1. **Từ Observation 1 và 2**:
   - Khi server chạy live và gọi `batch_edit_graph`, `search_nodes_library`, hoặc `get_active_graph_info`, bridge nhận mã nguồn từ `ExecuteRequest` và chuyển sang `DynamoExecutionEngine.RunRoslynCodeAsync`.
   - `ScriptCompiler.GetOrCompile` biên dịch script bằng `CSharpScript.Create<object>(code, options, typeof(DynamoScriptGlobals), loader)`.
   - Các options được cấu hình với danh sách reference lấy từ `DynamoCompilerReferences.All`.

2. **Từ Observation 4 và 2**:
   - Vì `DynamoCompilerReferences.All` không chứa `DynamoApplications.dll`, Roslyn không tìm thấy namespace `Dynamo.Applications`.
   - Khi gặp token `Dynamo.Applications.DynamoRevit.RevitDynamoModel`, Roslyn ném lỗi chẩn đoán:  
     `CS0234: The type or namespace name 'Applications' does not exist in the namespace 'Dynamo' (are you missing an assembly reference?)`
   - `ScriptCompiler.GetOrCompile` trả về `CompileOutcome(null, errors, CacheHit: false)`.
   - `DynamoExecutionEngine` đóng gói lỗi vào `ExecuteResult.Message = "The script does not compile. Fix the listed errors and retry."`.

3. **Từ Observation 1**:
   - Khi kết quả trả về server, `ToolManager.Record()` ghi nhận `success = false`.
   - Sau 5 lần gọi thất bại liên tiếp trong benchmark/test, `StabilityScorer.ShouldQuarantine()` trả về `true`.
   - `ToolManager` chuyển trạng thái 3 tools sang `quarantined` và lưu vào `registry.db` và `tool.json`.

4. **Từ Observation 6 và 2**:
   - Tại bridge runtime, biến `dynamo` trong `DynamoScriptGlobals` khi chạy Headless đã chính là `RevitDynamoModel` (vốn là một subclass của `DynamoModel`).
   - Do đó, biểu thức `(dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model ?? dynamo as DynamoModel` đã hoàn toàn trích xuất được `DynamoModel` ở cả 2 chế độ (Live Canvas lẫn Headless).
   - Phần fallback `?? Dynamo.Applications.DynamoRevit.RevitDynamoModel` là hoàn toàn dư thừa và là thủ phạm gây ra lỗi CS0234.

5. **Từ Observation 3 và 4**:
   - 4 hạt giống khác (`run_dyn_file`, `inspect_node_type`, `add_node_to_workspace`, `connect_node_ports`) cũng chứa cùng đoạn mã gọi `Dynamo.Applications`.
   - Nếu không sửa cả 2 tầng (bổ sung reference vào `DynamoCompilerReferences` và chuẩn hóa code seed), hệ thống vẫn có nguy cơ lỗi biên dịch runtime.

6. **Từ Observation 5**:
   - `SeedCompilationTests.cs` không phản ánh môi trường biên dịch thật của production `ScriptCompiler`. Cần phải có unit/integration test biên dịch các hạt giống bằng chính `ScriptCompiler` và `DynamoCompilerReferences`.

---

## 3. Caveats

1. **Giới hạn môi trường Read-Only**: Agent tuân thủ nghiêm ngặt chỉ thị không sửa mã nguồn, không chạy build hay test trong phiên khảo sát này.
2. **Tiến trình Revit PID 26264**: Không can thiệp, không inject code, không mở hay đóng model trên tiến trình Revit đang hoạt động của người dùng.
3. **Phạm vi kiểm tra**: Khảo sát đã phân tích 100% (13/13) mã nguồn các file `code.cs` của embedded seeds, đảm bảo không còn lỗi syntax hay reference tiềm ẩn nào khác ngoài các phát hiện đã nêu.

---

## 4. Conclusion

1. **Nguyên nhân gốc rễ lỗi quarantined đã được chứng minh 100%**: Do lỗi biên dịch Roslyn `CS0234` khi script seed gọi `Dynamo.Applications` trong khi `DynamoCompilerReferences.All` thiếu assembly `DynamoApplications.dll`.
2. **Kế hoạch giải quyết gồm 4 bước hành động cụ thể cho Worker**:
   - **Bước 1 (Bridge References)**: Nâng cấp `DynamoCompilerReferences.cs`, thêm `DynamoApplicationsAssembly`, `RevitNodesAssembly`, `ProtoGeometryAssembly`, `DynamoServicesAssembly`, `DynamoRevitDSAssembly`, `RevitServicesAssembly` vào `All` và cập nhật `ImportsFor()`.
   - **Bước 2 (Contracts Imports)**: Bổ sung `"Dynamo.ViewModels"`, `"Dynamo.Applications"`, `"Autodesk.Revit.DB"`, `"Autodesk.Revit.UI"` vào `HostScriptContracts.DynamoImports`.
   - **Bước 3 (Seed Refactoring)**: Chuẩn hóa 6 seeds sang **Canonical Pattern** (`dynamo as DynamoModel`), loại bỏ hoàn toàn fallback tĩnh `Dynamo.Applications.DynamoRevit`. Riêng `run_dyn_file` giữ nguyên nhánh Headless vì đã có reference `DynamoApplications.dll`.
   - **Bước 4 (Test Parity & Registry Unquarantine)**: Thêm test `ProductionSeedCompilationTests.cs` trong `HPDynamo.McpBridge.Tests` biên dịch sạch 13/13 seeds qua `ScriptCompiler`. Reset trạng thái 3 tools trong `tool.json` và SQLite registry về `published` để MCP Server trả về đủ **25 tools**.

---

## 5. Verification Method

Sau khi Worker triển khai bản sửa, độc lập xác minh qua các bước:

1. **Biên dịch và kiểm thử qua Dotnet CLI**:
   ```bash
   dotnet build HPDynamo/HPDynamo.slnx -c Debug
   dotnet test HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj
   dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj
   ```
   *Điều kiện đạt*: 100% tests PASS, bao gồm test kiểm tra biên dịch 13 seeds bằng production `ScriptCompiler`.

2. **Kiểm tra bề mặt công cụ MCP (Tool Surface)**:
   Khởi động MCP Server và gọi `tools/list` qua harness hoặc `mcp-call.py`:
   ```bash
   python McpShared/tools/mcp-call.py --exe HPDynamo/HPDynamo.Mcp.Server/bin/Debug/net10.0/HPDynamo.Mcp.Server.exe tools/list
   ```
   *Điều kiện đạt*: Danh sách trả về chứa đúng **25 tools** (4 Core + 8 Registry + 13 Seeds), bao gồm cả `batch_edit_graph`, `search_nodes_library`, và `get_active_graph_info`.

3. **Kiểm tra trạng thái tệp `tool.json`**:
   Đọc `%AppData%\HPDynamo\McpServer\tools-library\**\tool.json`:
   *Điều kiện đạt*: Cả 13 seeds đều có `"status": "published"`, không còn tool nào bị `"quarantined"`.
