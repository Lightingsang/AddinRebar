# Báo Cáo Khảo Sát: Production Compiler Diagnostics & Seeds (Milestone B)

**Người thực hiện**: `explorer_b_2` (Teamwork Explorer — Read-Only Investigation)  
**Ngày thực hiện**: 2026-10-09  
**Mục tiêu**: Thu thập chẩn đoán biên dịch thực tế của 3 tools bị `quarantined`, khảo sát toàn diện hạ tầng compiler production (`ScriptCompiler`, `DynamoCompilerReferences`, `HostScriptContracts.DynamoImports`, `DynamoScriptGlobals`), đối chiếu với test suite `SeedCompilationTests`, và lập kế hoạch giải quyết dứt điểm cho Worker.

---

## 1. Tóm Tắt Phát Hiện Cốt Lõi (Executive Summary)

1. **Lỗi biên dịch thực tế**: Cả 3 tools đang bị `quarantined` (`batch_edit_graph`, `search_nodes_library`, `get_active_graph_info`) đều gặp cùng một lỗi biên dịch Roslyn:  
   `CS0234: The type or namespace name 'Applications' does not exist in the namespace 'Dynamo' (are you missing an assembly reference?)`
2. **Nguyên nhân kép (Root Cause)**:
   - **Tầng Reference**: `DynamoCompilerReferences.All` **hoàn toàn thiếu** reference tới `DynamoApplications.dll` (cũng như `RevitNodes.dll`, `ProtoGeometry.dll`, `DynamoServices.dll`, `DynamoRevitDS.dll`, `RevitServices.dll`).
   - **Tầng Mã Nguồn Seed**: Cả 3 tools này (và 4 tools khác) đều cố chấp gọi fallback tĩnh `Dynamo.Applications.DynamoRevit.RevitDynamoModel`. Lệnh gọi tĩnh này vừa phá vỡ tính cô lập, vừa hoàn toàn thừa thãi vì bridge runtime (`DynamoExecutionEngine`) đã truyền `RevitDynamoModel` (vốn kế thừa `DynamoModel`) vào biến toàn cục `dynamo`.
3. **Phát hiện 4 hạt giống tiềm ẩn lỗi**: Không chỉ 3 tools bị quarantined, mà có thêm 4 hạt giống khác cũng chứa `Dynamo.Applications` (`run_dyn_file`, `inspect_node_type`, `add_node_to_workspace`, `connect_node_ports`). Chúng chưa bị quarantined chỉ vì chưa bị gọi thất bại 5 lần trong benchmark.
4. **Lỗ hổng trong bộ test hiện tại (`SeedCompilationTests.cs`)**: Test suite `SeedCompilationTests` dùng một phương thức compile riêng với wrapper class giả lập `SeedHost`, tự nạp `DynamoApplications.dll` và tự `using Dynamo.Applications;`, do đó **không hề phát hiện được** việc production `ScriptCompiler` và `DynamoCompilerReferences` thiếu assembly references.

---

## 2. Bằng Chứng Thực Nghiệm & Chẩn Đoán Chi Tiết 3 Tools Quarantined

### 2.1. Dữ liệu ghi nhận trong SQLite Registry (`registry.db`) và AppData `tool.json`
- **Vị trí file**: `C:\Users\STR-HP03\AppData\Roaming\HPDynamo\McpServer\registry.db`
- **Truy vấn bảng `runs`**:
  - `batch_edit_graph`: 5 lần chạy liên tiếp thất bại lúc `2026-10-09 09:45:35Z`. Lỗi: `"The script does not compile. Fix the listed errors and retry."`
  - `search_nodes_library`: 5 lần chạy liên tiếp thất bại lúc `2026-10-09 09:45:25Z`. Lỗi: `"The script does not compile. Fix the listed errors and retry."`
  - `get_active_graph_info`: 5 lần chạy liên tiếp thất bại lúc `2026-10-09 09:45:16Z`. Lỗi: `"The script does not compile. Fix the listed errors and retry."`
- **Truy vấn bảng `registry_events`**:
  - Cả 3 tools đều bị kích hoạt sự kiện `"quarantined"` tự động bởi `ToolManager.Record()` sau khi tỷ lệ thất bại đạt ngưỡng cách ly `5/5`.
- **Nội dung `tool.json` trong AppData**:
  - `batch_edit_graph/tool.json:152`: `"notes": "Quarantined 2026-10-09 09:45:35Z: 5/5 recent runs failed. Last error: The script does not compile. Fix the listed errors and retry."`
  - `search_nodes_library/tool.json:48`: `"notes": "Quarantined 2026-10-09 09:45:25Z: 5/5 recent runs failed. Last error: The script does not compile. Fix the listed errors and retry."`
  - `get_active_graph_info/tool.json:43`: `"notes": "Quarantined 2026-10-09 09:45:16Z: 5/5 recent runs failed. Last error: The script does not compile. Fix the listed errors and retry."`

### 2.2. Điểm lỗi chính xác trong mã nguồn 3 tools

#### A. `batch_edit_graph/code.cs` (Dòng 7-8)
```csharp
7: var dynamoViewModel = dynamo as Dynamo.ViewModels.DynamoViewModel;
8: var dynamoModel = dynamoViewModel?.Model ?? dynamo as DynamoModel ?? Dynamo.Applications.DynamoRevit.RevitDynamoModel;
```
- **Lỗi Roslyn**: `CS0234: The type or namespace name 'Applications' does not exist in the namespace 'Dynamo'`
- **Vị trí**: Cụm từ `Dynamo.Applications.DynamoRevit.RevitDynamoModel`.
- **Đánh giá các dòng còn lại**: Tất cả các cấu trúc LIFO rollback (`PerformRollback`), snapshot connectors, `UpdateValueParams`, `PortType`, `ElementState` đều hợp lệ và tương thích hoàn toàn với `DynamoCore.dll`.

#### B. `search_nodes_library/code.cs` (Dòng 8-11)
```csharp
8: var dynamoViewModel = dynamo as Dynamo.ViewModels.DynamoViewModel;
9: var dynamoModel = dynamoViewModel?.Model
10:     ?? dynamo as DynamoModel
11:     ?? Dynamo.Applications.DynamoRevit.RevitDynamoModel;
```
- **Lỗi Roslyn**: `CS0234: The type or namespace name 'Applications' does not exist in the namespace 'Dynamo'`
- **Vị trí**: Dòng 11 `Dynamo.Applications.DynamoRevit.RevitDynamoModel`.
- **Đánh giá các dòng còn lại**: Truy cập `dynamoModel.SearchModel.Entries`, `CreationName`, `FullName`, `Categories` đều hợp lệ trong `DynamoCore.dll`.

#### C. `get_active_graph_info/code.cs` (Dòng 4-7)
```csharp
4: var dynamoViewModel = dynamo as Dynamo.ViewModels.DynamoViewModel;
5: var currentWorkspaceModel = workspace
6:     ?? dynamoViewModel?.Model?.CurrentWorkspace
7:     ?? Dynamo.Applications.DynamoRevit.RevitDynamoModel?.CurrentWorkspace;
```
- **Lỗi Roslyn**: `CS0234: The type or namespace name 'Applications' does not exist in the namespace 'Dynamo'`
- **Vị trí**: Dòng 7 `Dynamo.Applications.DynamoRevit.RevitDynamoModel`.
- **Đánh giá các dòng còn lại**: Truy vấn `Nodes`, `Connectors`, `HasUnsavedChanges`, `NodeInfos`, `ElementState` đều hợp lệ trong `DynamoCore.dll`.

---

## 3. Khảo Sát Toàn Diện 13 Embedded Seeds

Khảo sát toàn bộ 13 file `code.cs` trong `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/`:

| # | Thư mục & Tên Tool | Dòng code | Sử dụng `Dynamo.Applications`? | Trạng thái hiện tại | Rủi ro biên dịch Production |
|---|---|---:|:---:|:---:|---|
| 1 | `CodeBlock/create_code_block_node` | 46 | ❌ KHÔNG | `published` | **AN TOÀN** (Dùng pattern chuẩn `(dynamo as DynamoModel)?.CurrentWorkspace`) |
| 2 | `Data/get_node_output_values` | 256 | ❌ KHÔNG | `published` | **AN TOÀN** (Dùng `ProtoCore` + `DesignScriptBuiltin` hợp lệ) |
| 3 | `Export/export_graph_to_image` | 40 | ❌ KHÔNG | `published` | **AN TOÀN** (Dùng `DynamoViewModel.OnRequestSaveImage` hợp lệ) |
| 4 | `Graph/batch_edit_graph` | 281 | ⚠️ **CÓ (Dòng 8)** | **`quarantined`** | **LỖI CS0234** khi biên dịch |
| 5 | `Graph/run_dyn_file` | 141 | ⚠️ **CÓ (Dòng 115, 118)** | `published` | **LỖI CS0234** khi chạy Headless (`new DynamoRevitApp()`) |
| 6 | `Graph/validate_dyn_file` | 147 | ❌ KHÔNG | `published` | **AN TOÀN** (Phân tích cú pháp JSON mộc) |
| 7 | `Library/inspect_node_type` | 82 | ⚠️ **CÓ (Dòng 8)** | `published` | **LỖI CS0234** nếu gọi live |
| 8 | `Library/search_nodes_library` | 76 | ⚠️ **CÓ (Dòng 11)** | **`quarantined`** | **LỖI CS0234** khi biên dịch |
| 9 | `Node/add_node_to_workspace` | 85 | ⚠️ **CÓ (Dòng 14)** | `published` | **LỖI CS0234** nếu gọi live |
| 10 | `Node/set_node_properties` | 92 | ❌ KHÔNG | `published` | **AN TOÀN** (Dùng pattern chuẩn `(dynamo as DynamoModel)?.CurrentWorkspace`) |
| 11 | `Wire/connect_node_ports` | 70 | ⚠️ **CÓ (Dòng 15)** | `published` | **LỖI CS0234** nếu gọi live |
| 12 | `Wire/disconnect_node_ports` | 104 | ❌ KHÔNG | `published` | **AN TOÀN** (Dùng pattern chuẩn) |
| 13 | `Workspace/get_active_graph_info` | 90 | ⚠️ **CÓ (Dòng 7)** | **`quarantined`** | **LỖI CS0234** khi biên dịch |

**Nhận định**: Có tới 7/13 seeds bị nhiễm token `Dynamo.Applications.DynamoRevit.RevitDynamoModel`. 3 seeds đã bị phát hiện và cách ly, trong khi 4 seeds còn lại là "bom nổ chậm" nếu được gọi live.

---

## 4. Khảo Sát Kiến Trúc Compiler Production & Chuỗi Phụ Thuộc

### 4.1. `ScriptCompiler.cs` (`McpShared/HPRebar.McpBridge.Core/Scripting/ScriptCompiler.cs`)
- Phương thức khởi tạo:
  ```csharp
  _options = ScriptOptions.Default
      .WithReferences(references)
      .WithImports(imports)
      .WithEmitDebugInformation(false)
      .WithOptimizationLevel(OptimizationLevel.Release);
  var script = CSharpScript.Create<object>(code, _options, _globalsType, _loader);
  ```
- Nhận `references` từ `DynamoCompilerReferences.All`, `imports` từ `DynamoCompilerReferences.ImportsFor(refs)`, và `_globalsType` là `typeof(DynamoScriptGlobals)`.

### 4.2. `DynamoCompilerReferences.cs` (`HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoCompilerReferences.cs`)
Hiện tại thuộc tính `All` chỉ nạp:
1. `typeof(object).Assembly` (`System.Private.CoreLib`)
2. `typeof(Enumerable).Assembly` (`System.Linq`)
3. `typeof(List<>).Assembly` (`System.Collections`)
4. `typeof(ScriptArgs).Assembly` (`HPRebar.McpBridge.Core`)
5. `typeof(JsonElement).Assembly` (`System.Text.Json`)
6. `typeof(DynamoScriptGlobals).Assembly` (`HPDynamo.McpBridge`)
7. `DynamoModelAssembly()` (`DynamoCore.dll`)
8. `DynamoViewModelAssembly()` (`DynamoCoreWpf.dll`)
9. `NodeModelAssembly()` (`DynamoCore.dll`)
10. `RevitDocAssembly()` (`RevitAPI.dll`)
11. `RevitUiAssembly()` (`RevitAPIUI.dll`)
12. `ProtoCoreAssembly()` (`ProtoCore.dll`)
13. `DesignScriptBuiltinAssembly()` (`DesignScriptBuiltin.dll`)

**Các assembly bị bỏ quên (Missing Assemblies)**:
- `DynamoApplications.dll` (nằm tại `C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\DynamoApplications.dll`, chứa `Dynamo.Applications.DynamoRevitApp` và `DynamoRevit`).
- `RevitNodes.dll` (nằm tại `...\DynamoForRevit\Revit\RevitNodes.dll`, chứa `Revit.Elements.*`).
- `ProtoGeometry.dll` (nằm tại `...\DynamoForRevit\ProtoGeometry.dll`, chứa `Autodesk.DesignScript.Geometry.*`).
- `DynamoServices.dll` (nằm tại `...\DynamoForRevit\DynamoServices.dll`).
- `DynamoRevitDS.dll` (nằm tại `...\DynamoForRevit\Revit\DynamoRevitDS.dll`).
- `RevitServices.dll` (nằm tại `...\DynamoForRevit\Revit\RevitServices.dll`).

**Hậu quả trên `ImportsFor`**:
Hàm `ImportsFor` lọc `HostScriptContracts.DynamoImports`:
```csharp
.Where(ns => ns != "Autodesk.DesignScript.Geometry" || names.Contains("ProtoGeometry"))
.Where(ns => ns != "Revit.Elements" || names.Contains("RevitNodes"))
```
Vì `ProtoGeometry` và `RevitNodes` không có trong `references`, cả hai namespace quan trọng này bị loại bỏ khỏi danh sách import mặc định của script!

### 4.3. `HostScriptContracts.DynamoImports` (`McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs`)
- Hiện chỉ có:
  `"System"`, `"System.Linq"`, `"System.Collections.Generic"`, `"Autodesk.DesignScript.Geometry"`, `"Dynamo.Graph.Nodes"`, `"Dynamo.Graph.Workspaces"`, `"Dynamo.Models"`, `"Revit.Elements"`, `"HPRebar.McpBridge.Core.Scripting"`.
- Thiếu các namespace hữu ích mà script hay dùng:
  - `"Dynamo.ViewModels"`
  - `"Dynamo.Applications"`
  - `"Autodesk.Revit.DB"`
  - `"Autodesk.Revit.UI"`

### 4.4. `DynamoScriptGlobals.cs` (`HPDynamo/HPDynamo.McpBridge/Support/DynamoScriptGlobals.cs`)
- Cung cấp: `dynamo` (object), `workspace` (WorkspaceModel?), `doc` (Document), `uidoc` (UIDocument), `app` (Application), `uiapp` (UIApplication), `ct` (CancellationToken), `log` (Action<string?>), `progress` (Action<int, int, string>), `args` (ScriptArgs), `dryRun` (bool), `globals` (DynamoScriptGlobals), `RunAndWait()`.
- Trong `DynamoExecutionEngine.cs:115-117`:
  ```csharp
  object dynamoGlobal = _coordinator.IsLiveCanvasAttached && _coordinator.ActiveViewModel != null
      ? (object)_coordinator.ActiveViewModel
      : (object)Dynamo.Applications.DynamoRevit.RevitDynamoModel!;
  ```
  Biến `dynamoGlobal` khi ở chế độ headless đã chính là đối tượng `RevitDynamoModel` (vốn là một thể hiện của `DynamoModel`). Do đó, trong script chỉ cần viết `dynamo as DynamoModel` là tự động bắt được đối tượng này, hoàn toàn không cần gọi tới namespace tĩnh `Dynamo.Applications.DynamoRevit`.

---

## 5. Đối Chiếu Với Bộ Test `SeedCompilationTests.cs`

Bộ test hiện hữu `HPDynamo.Mcp.Server.Tests/Seeds/SeedCompilationTests.cs` đã **đánh lừa** hệ thống CI/CD vì:
1. Nạp thẳng danh sách file DLL riêng trong `ResolveHostReferences()`:
   ```csharp
   string[] requiredDlls = [ "RevitAPI.dll", "RevitAPIUI.dll", "DynamoCore.dll", "DynamoCoreWpf.dll", "DynamoApplications.dll", "RevitNodes.dll" ];
   ```
2. Tự gói mã nguồn seed vào class giả lập `SeedHost` với hàng loạt `using` không có trong production:
   ```csharp
   using Dynamo.Applications;
   using Autodesk.Revit.DB;
   using Autodesk.Revit.UI;
   ```
3. Biên dịch bằng `CSharpCompilation.Create("dynamo_seed_check", ...)` thay vì gọi `ScriptCompiler.GetOrCompile()`.

Do đó, test chạy 100% PASS trên máy có cài Revit, nhưng khi server và bridge chạy thực tế bằng `ScriptCompiler` thì lỗi bung bét.

---

## 6. Kế Hoạch Đề Xuất Giải Quyết Cụ Thể Cho Worker (Actionable Plan)

Để bảo đảm 13 embedded seeds biên dịch sạch 100% bằng production `ScriptCompiler` và phục hồi bề mặt 25 tools:

### Bước 1: Nâng cấp `DynamoCompilerReferences.cs` (2 Tầng Phòng Vệ)
1. Thêm các hàm resolve động an toàn:
   - `DynamoApplicationsAssembly()`
   - `RevitNodesAssembly()`
   - `ProtoGeometryAssembly()`
   - `DynamoServicesAssembly()`
   - `DynamoRevitDSAssembly()`
   - `RevitServicesAssembly()`
2. Bổ sung `TryAdd` các assembly này vào `DynamoCompilerReferences.All`.
3. Cập nhật `ImportsFor()` để hỗ trợ thêm `DynamoApplications` và `DynamoCoreWpf`.

### Bước 2: Cập nhật `HostScriptContracts.cs` (Đồng Bộ Wire Contract)
Thêm các namespace cần thiết vào `HostScriptContracts.DynamoImports`:
- `"Dynamo.ViewModels"`
- `"Dynamo.Applications"`
- `"Autodesk.Revit.DB"`
- `"Autodesk.Revit.UI"`

### Bước 3: Chuẩn Hóa Cú Pháp Truy Cập Model Trong Tất Cả 13 Seeds
Loại bỏ hoàn toàn fallback tĩnh `Dynamo.Applications.DynamoRevit.RevitDynamoModel` trong 6 seeds:
- `Graph/batch_edit_graph/code.cs`
- `Library/search_nodes_library/code.cs`
- `Workspace/get_active_graph_info/code.cs`
- `Library/inspect_node_type/code.cs`
- `Node/add_node_to_workspace/code.cs`
- `Wire/connect_node_ports/code.cs`

Chuyển sang **Canonical Pattern** (đã chạy rất ổn định ở 6 seeds còn lại):
```csharp
var dynamoViewModel = dynamo as Dynamo.ViewModels.DynamoViewModel;
var dynamoModel = dynamoViewModel?.Model ?? dynamo as DynamoModel;
var currentWorkspace = workspace ?? dynamoModel?.CurrentWorkspace;
```
*(Riêng `Graph/run_dyn_file/code.cs` giữ nguyên `Dynamo.Applications.DynamoRevitApp` cho nhánh Headless, vì `DynamoApplications.dll` lúc này đã có trong References).*

### Bước 4: Viết Bộ Test Biên Dịch Trực Tiếp Bằng Production Compiler
Tạo test suite mới trong `HPDynamo.McpBridge.Tests`: `ProductionSeedCompilationTests.cs`:
- Khởi tạo trực tiếp `ScriptCompiler` với `DynamoCompilerReferences.All`, `DynamoCompilerReferences.ImportsFor(refs)`, và `typeof(DynamoScriptGlobals)`.
- Duyệt qua toàn bộ 13 hạt giống và gọi `compiler.GetOrCompile(seed.Code)`.
- Assert `outcome.Succeeded == true` và `outcome.Diagnostics.Count == 0`.

### Bước 5: Khôi Phục Trạng Thái Registry (Phối Hợp Explorer 1)
- Cập nhật file `tool.json` của 3 tools trong thư mục seed và trong `%AppData%\HPDynamo\McpServer\tools-library\`:
  - `"status": "published"`
  - Xóa chuỗi `notes` chứa thông báo lỗi thất bại lịch sử.
- Khởi động lại MCP Server để xác nhận `tools/list` trả về đầy đủ **25 tools** (4 Core + 8 Registry + 13 Seeds).
