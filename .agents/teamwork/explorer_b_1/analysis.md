# Báo Cáo Khảo Sát SQLite Registry & Tool Lifecycle — HPDynamo MCP (Milestone B)

**Người thực hiện**: explorer_b_1  
**Ngày**: 2026-10-09  
**Mục tiêu**: Khảo sát hiện trạng SQLite Registry database, nguyên nhân 3 tools bị quarantined, và xác định quy trình chuẩn để phục hồi về `published` bảo đảm bề mặt 25 tools không làm mất dữ liệu người dùng.

---

## 1. Khảo Sát SQLite Registry Database của HPDynamo

### 1.1. Vị Trí Lưu Trữ Cơ Sở Dữ Liệu & Cấu Trúc Thư Mục
Dựa trên `DynamoHostProfile.cs` (`ProductFolder = "HPDynamo"`) và `RegistryOptions.cs` (`DefaultRootFor(ProductFolder)`):
- **Thư mục gốc MCP Server**:
  `C:\Users\STR-HP03\AppData\Roaming\HPDynamo\McpServer\`
- **Tệp cơ sở dữ liệu SQLite**:
  `C:\Users\STR-HP03\AppData\Roaming\HPDynamo\McpServer\registry.db`
  - Dung lượng hiện tại: `688,128` bytes (~688 KB).
  - Tệp nhật ký kèm theo: `registry.db-wal` (Write-Ahead Logging) và `registry.db-shm` (Shared Memory).
  - Cấu hình SQLite Connection (`ToolRegistryDb.cs`):
    `PRAGMA journal_mode = WAL; PRAGMA busy_timeout = 5000; PRAGMA foreign_keys = ON;` (cho phép nhiều phiên AI / tiến trình server đọc/ghi đồng thời mà không bị lock file).
- **Thư mục chứa mã nguồn các Tool (Tools Library)**:
  `C:\Users\STR-HP03\AppData\Roaming\HPDynamo\McpServer\tools-library\`
  - Lưu trữ theo cấu trúc: `<Category>/<ToolName>/{tool.json, code.cs, examples.json}`
  - Tệp manifest hạt giống: `_seeds.json` (ghi nhận SHA-256 checksum của các embedded seeds lúc cài đặt).

---

### 1.2. Cấu Trúc Schema Bảng trong SQLite
Cơ sở dữ liệu `registry.db` được khởi tạo bởi `ToolRegistryDb.Initialize()` với các bảng sau:

1. **Bảng `tools`** (Chỉ mục thư viện công cụ hiện hành):
   - `name TEXT PRIMARY KEY`: Tên định danh của công cụ (e.g. `batch_edit_graph`).
   - `category TEXT NOT NULL`: Nhóm phân loại (`Graph`, `Workspace`, `Library`, `Node`, `Wire`, `Data`, `Export`, `CodeBlock`...).
   - `title TEXT`: Tên hiển thị người dùng.
   - `description TEXT NOT NULL`: Mô tả công năng của tool.
   - `tags TEXT`: Chuỗi từ khóa phân tách bằng khoảng trắng.
   - `status TEXT NOT NULL`: Trạng thái vòng đời (`draft`, `tested`, `pending_approval`, `published`, `quarantined`, `deprecated`).
   - `version INTEGER NOT NULL`: Phiên bản công cụ (mặc định 1).
   - `checksum TEXT NOT NULL`: SHA-256 của toàn bộ nội dung tool (`tool.json` + `code.cs` + `examples.json`).
   - `folder TEXT`: Đường dẫn thư mục chứa trên đĩa.
   - `record_json TEXT NOT NULL`: Dữ liệu JSON tuần tự hóa toàn bộ đối tượng `ToolRecord` (chứa các trường mở rộng như `notes`, `inputSchema`, `transaction`, `timeoutSeconds`, `destructive`, `host`...).
   - `examples_text TEXT`: Văn bản tiêu đề các ví dụ phục vụ tìm kiếm.
   - `updated_at INTEGER NOT NULL`: Unix timestamp (milliseconds) thời điểm cập nhật cuối.

2. **Bảng `tool_versions`** (Lưu vết các phiên bản mã nguồn):
   - `name TEXT NOT NULL`
   - `version INTEGER NOT NULL`
   - `code_sha TEXT NOT NULL`
   - `input_schema TEXT`
   - `code TEXT NOT NULL`
   - `created_at INTEGER NOT NULL`
   - Khóa chính: `PRIMARY KEY (name, version)`

3. **Bảng `runs`** (Sổ cái lịch sử thực thi công cụ & ad-hoc runs):
   - `id INTEGER PRIMARY KEY AUTOINCREMENT`: Mã định danh lượt chạy.
   - `tool_name TEXT`: Tên công cụ (NULL nếu là execute ad-hoc).
   - `version INTEGER`: Phiên bản công cụ khi chạy.
   - `kind TEXT NOT NULL`: Loại lượt chạy (`tool`, `test`, `adhoc`).
   - `args_sha TEXT`: SHA-256 của bộ tham số đầu vào.
   - `code_sha TEXT`: SHA-256 của đoạn mã thực thi.
   - `dry_run INTEGER NOT NULL`: 1 nếu chạy thử nghiệm rollback, 0 nếu apply.
   - `success INTEGER NOT NULL`: 1 nếu thành công, 0 nếu thất bại.
   - `duration_ms INTEGER`: Thời gian chạy tính bằng mili-giây.
   - `error TEXT`: Thông báo lỗi chi tiết khi `success = 0`.
   - `revit_version TEXT`: Phiên bản Revit host (e.g. `2026`).
   - `doc_title TEXT`: Tên file Revit document tại thời điểm chạy.
   - `ts INTEGER NOT NULL`: Unix timestamp (milliseconds) lúc thực thi.
   - `code TEXT`: Nội dung mã nguồn lưu tạm cho ad-hoc runs.
   - `args_json TEXT`: Chuỗi JSON arguments đầu vào.
   - Chỉ mục: `idx_runs_tool_ts (tool_name, ts DESC)`, `idx_runs_kind_ts (kind, ts DESC)`.

4. **Bảng `registry_events`** (Nhật ký kiểm toán sự kiện vòng đời):
   - `id INTEGER PRIMARY KEY AUTOINCREMENT`: Mã sự kiện.
   - `ts INTEGER NOT NULL`: Unix timestamp (milliseconds).
   - `tool_name TEXT`: Tên công cụ liên quan.
   - `event TEXT NOT NULL`: Tên sự kiện (`proposed`, `tested`, `approved`, `published`, `quarantined`, `restore`, `status_changed`, `rejected`, `deprecate`...).
   - `actor TEXT`: Tác nhân kích hoạt (`ai`, `registry`, `file`, hoặc tên người dùng).
   - `detail TEXT`: Chi tiết ghi chú sự kiện.
   - Chỉ mục: `idx_events_tool_event_ts (tool_name, event, ts DESC)`.

5. **Bảng ảo `tools_fts`** (Full-Text Search FTS5):
   - Ảo hóa qua FTS5 với tokenizer `unicode61` trên các cột: `(name, title, description, tags, examples)`.

---

### 1.3. Hiện Trạng Các Tools Trong Registry DB & Bề Mặt MCP
Qua truy vấn thực tế `registry.db`:
- **Tổng số công cụ trong bảng `tools`**: 13 công cụ (tương ứng 13 seeds của HPDynamo).
- **Phân loại trạng thái**:
  - **10 tools Published**:
    1. `add_node_to_workspace` (Category: Node)
    2. `connect_node_ports` (Category: Wire)
    3. `create_code_block_node` (Category: CodeBlock)
    4. `disconnect_node_ports` (Category: Wire)
    5. `export_graph_to_image` (Category: Export)
    6. `get_node_output_values` (Category: Data)
    7. `inspect_node_type` (Category: Library)
    8. `run_dyn_file` (Category: Graph)
    9. `set_node_properties` (Category: Node)
    10. `validate_dyn_file` (Category: Graph)
  - **3 tools Quarantined**:
    1. `batch_edit_graph` (Category: Graph)
    2. `get_active_graph_info` (Category: Workspace)
    3. `search_nodes_library` (Category: Library)
- **Tác động lên bề mặt công cụ MCP (`tools/list`)**:
  - Theo thiết kế `DynamicToolRegistrar.Sync()`: chỉ công cụ có `record.IsPublished == true` mới được đăng ký vào `McpServerOptions.ToolCollection`.
  - Kết quả: Khi gọi `tools/list`, MCP Server chỉ trả về **22 tools** (4 Core + 8 Registry Meta + 10 Published Seeds), thiếu đúng 3 công cụ đang bị quarantined (mục tiêu chuẩn là **25 tools**).

---

## 2. Tìm Hiểu Lý Do 3 Tools Bị Quarantined

### 2.1. Dữ Liệu Ghi Nhận Trực Tiếp Trong Database
Khi kiểm tra nhật ký trong `registry.db`:
1. **Bảng `registry_events`**:
   - `id = 4 | ts = 1791539116954 | tool = get_active_graph_info | event = quarantined | actor = registry`
     - *Detail*: `"Quarantined 2026-10-09 09:45:16Z: 5/5 recent runs failed. Last error: The script does not compile. Fix the listed errors and retry."`
   - `id = 5 | ts = 1791539125068 | tool = search_nodes_library | event = quarantined | actor = registry`
     - *Detail*: `"Quarantined 2026-10-09 09:45:25Z: 5/5 recent runs failed. Last error: The script does not compile. Fix the listed errors and retry."`
   - `id = 6 | ts = 1791539135110 | tool = batch_edit_graph | event = quarantined | actor = registry`
     - *Detail*: `"Quarantined 2026-10-09 09:45:35Z: 5/5 recent runs failed. Last error: The script does not compile. Fix the listed errors and retry."`

2. **Bảng `runs`**:
   - Cả 3 công cụ đều có đúng **5 lần chạy liên tiếp** (`kind = 'tool'`, `success = 0`) trong khoảng thời gian 09:45 UTC ngày 2026-10-09:
     - `get_active_graph_info`: Runs ID 67, 69, 70, 71, 72 (tất cả duration ~765ms–1804ms, error: `"The script does not compile. Fix the listed errors and retry."`).
     - `search_nodes_library`: Runs ID 68, 73, 74, 75, 76 (tất cả duration ~755ms–934ms, error: `"The script does not compile. Fix the listed errors and retry."`).
     - `batch_edit_graph`: Runs ID 77, 78, 79, 80, 81 (tất cả duration ~750ms–859ms, error: `"The script does not compile. Fix the listed errors and retry."`).

3. **Cơ chế kích hoạt Quarantine tự động của `ToolManager`**:
   - Mã nguồn `ToolManager.RecordRun` (`ToolManager.cs`, dòng 212–220):
     ```csharp
     if (kind == RunRecord.KindTest || record.Status != ToolStatus.Published) return id;

     var stats = _db.Stats(record.Name, _options.RunWindow);
     if (!StabilityScorer.ShouldQuarantine(stats, _options.QuarantineMinRuns, _options.QuarantineMaxFailureRate)) return id;

     record.Status = ToolStatus.Quarantined;
     record.Notes = $"Quarantined {DateTimeOffset.UtcNow:u}: {stats.Failures}/{stats.Runs} recent runs failed. Last error: {stats.LastError}";
     Save(record, "quarantined", "registry", record.Notes);
     ```
   - Cấu hình chuẩn (`RegistryOptions.cs`):
     - `QuarantineMinRuns = 5`
     - `QuarantineMaxFailureRate = 0.4` (40%)
   - Khi cả 5 lần chạy thực tế đều thất bại liên tiếp (100% failure rate > 40%), `StabilityScorer.ShouldQuarantine` trả về `true`.
   - `ToolManager` lập tức chuyển công cụ sang `Quarantined`, ghi event vào `registry_events`, ghi đè `tool.json` trên ổ đĩa và kích hoạt `DynamicToolRegistrar.Sync()` gỡ công cụ khỏi danh sách active MCP tools.

---

### 2.2. Phân Tích Nguyên Nhân Kỹ Thuật (Compile Diagnostics & Compiler References)
Tại sao cả 3 công cụ trên đều bị lỗi `"The script does not compile. Fix the listed errors and retry."`?

1. **Trích xuất lỗi biên dịch thực tế**:
   - Xem tệp kết quả chạy thực nghiệm trước đó tại `scratch/run_dyn_out.json` cho các công cụ gặp lỗi biên dịch:
     ```json
     {
       "isError": true,
       "message": "The script does not compile. Fix the listed errors and retry.",
       "diagnostics": [
         {
           "line": 95,
           "column": 34,
           "id": "CS0234",
           "message": "The type or namespace name 'Applications' does not exist in the namespace 'Dynamo' (are you missing an assembly reference?)"
         }
       ]
     }
     ```
2. **Đối soát mã nguồn 3 công cụ bị quarantined**:
   - Trong `HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/batch_edit_graph/code.cs` (dòng 8):
     ```csharp
     var dynamoModel = dynamoViewModel?.Model ?? dynamo as DynamoModel ?? Dynamo.Applications.DynamoRevit.RevitDynamoModel;
     ```
   - Trong `HPDynamo.Mcp.Server/Registry/SeedLibrary/Library/search_nodes_library/code.cs` (dòng 11):
     ```csharp
     ?? Dynamo.Applications.DynamoRevit.RevitDynamoModel;
     ```
   - Trong `HPDynamo.Mcp.Server/Registry/SeedLibrary/Workspace/get_active_graph_info/code.cs` (dòng 7):
     ```csharp
     ?? Dynamo.Applications.DynamoRevit.RevitDynamoModel?.CurrentWorkspace;
     ```
3. **Mâu thuẫn giữa Mã Nguồn Seed và `DynamoCompilerReferences.All`**:
   - Trong `HPDynamo.McpBridge/Coordinator/DynamoCompilerReferences.cs` (dòng 28–48):
     ```csharp
     var list = new List<Assembly>
     {
         typeof(object).Assembly,
         typeof(Enumerable).Assembly,
         typeof(List<>).Assembly,
         typeof(ScriptArgs).Assembly,
         typeof(System.Text.Json.JsonElement).Assembly,
         typeof(DynamoScriptGlobals).Assembly
     };
     TryAdd(list, DynamoModelAssembly);       // DynamoCore.dll
     TryAdd(list, DynamoViewModelAssembly);   // DynamoCoreWpf.dll
     TryAdd(list, NodeModelAssembly);         // DynamoCore.dll
     TryAdd(list, RevitDocAssembly);          // RevitAPI.dll
     TryAdd(list, RevitUiAssembly);           // RevitAPIUI.dll
     TryAdd(list, ProtoCoreAssembly);         // ProtoCore.dll
     TryAdd(list, DesignScriptBuiltinAssembly);// DesignScriptBuiltin.dll
     ```
   - `DynamoCompilerReferences.All` **hoàn toàn không nạp assembly `DynamoApplications.dll`** vào Roslyn compiler `MetadataReference`. Do đó, bất kỳ đoạn script nào chứa cụm từ `Dynamo.Applications.` đều bị trình biên dịch Roslyn từ chối dứt khoát với lỗi `CS0234`!
4. **Tại sao tham chiếu `Dynamo.Applications` là lỗi thiết kế dư thừa?**
   - Trong `DynamoExecutionEngine.cs` (dòng 115–117):
     ```csharp
     object dynamoGlobal = _coordinator.IsLiveCanvasAttached && _coordinator.ActiveViewModel != null
         ? (object)_coordinator.ActiveViewModel
         : (object)Dynamo.Applications.DynamoRevit.RevitDynamoModel!;
     ```
     Bridge **đã tự động nạp** `RevitDynamoModel` vào biến toàn cục `dynamo` khi chạy headless.
   - Vì `RevitDynamoModel` kế thừa trực tiếp từ `Dynamo.Models.DynamoModel`, nên trong script chỉ cần gọi:
     ```csharp
     var dynamoViewModel = dynamo as Dynamo.ViewModels.DynamoViewModel;
     var dynamoModel = dynamoViewModel?.Model ?? dynamo as DynamoModel;
     ```
     là đã trích xuất được `DynamoModel` một cách sạch sẽ, an toàn 100%, hoàn toàn không cần gọi `Dynamo.Applications.DynamoRevit.RevitDynamoModel`!

---

## 3. Quy Trình Chuẩn Để Đưa Trạng Thái từ Quarantined Về Published Mà Không Làm Mất Dữ Liệu Người Dùng

### 3.1. Nguyên Tắc Bảo Toàn Dữ Liệu Tuyệt Đối (Zero Data Loss)
- **Tuyệt đối không xóa bảng `runs` hoặc các bản ghi trong `runs`**: Bảng `runs` chứa toàn bộ lịch sử 58 lượt chạy ad-hoc và các lần chạy công cụ.
- **Tuyệt đối không xóa file `registry.db`**: Cơ sở dữ liệu SQLite chứa các metadata, version và sự kiện kiểm toán.
- **Cơ chế Cutoff Timestamp sẵn có trong `ToolRegistryDb.Runs.cs`**:
  Hãy xem câu truy vấn lọc tính ổn định `StabilityRunFilter` (`ToolRegistryDb.Runs.cs`, dòng 61–64):
  ```sql
  kind <> 'test' AND (error IS NULL OR error NOT LIKE 'Argument%Exception:%')
  AND ts >= COALESCE((SELECT MAX(e.ts) FROM registry_events e
                      WHERE e.tool_name = runs.tool_name
                      AND e.event IN ('approved', 'published', 'restore', 'proposed_version', 'imported', 'status_changed')), 0)
  ```
  **Ý nghĩa kiến trúc cực kỳ quan trọng**:
  Khi một sự kiện hợp lệ trong danh sách (`approved`, `published`, `restore`, hoặc `status_changed`) được ghi vào bảng `registry_events`, mốc thời gian `MAX(e.ts)` sẽ được cập nhật lên thời điểm hiện tại.
  Toàn bộ 5 lần chạy thất bại trong quá khứ (vốn có `runs.ts < MAX(e.ts)`) **sẽ tự động bị loại khỏi cửa sổ tính toán `StabilityScorer.Stats`**.
  Do đó: Khi công cụ được khôi phục về `published`, các lần chạy tiếp theo sẽ không bị ảnh hưởng bởi 5 lỗi cũ trong quá khứ! Dữ liệu lịch sử chạy cũ vẫn được bảo toàn nguyên vẹn 100% cho việc audit và phân tích!

---

### 3.2. Quy Trình Chuẩn Đưa Trạng Thái Về Published

Hệ thống hỗ trợ 2 quy trình chuẩn để phục hồi:

#### Quy Trình 1: Phục hồi qua File-based Workflow (Được thiết kế chính thức trong `ToolManager.cs`)
Theo chú thích chính thức tại `ToolManager.cs` (dòng 92–98):
> *"A status edited by hand in tool.json (the documented second approval path) is a lifecycle event like an approve: without it a quarantined tool set back to published would be re-quarantined by the failures that got it quarantined."*

Các bước thực hiện:
1. **Sửa mã nguồn `code.cs`**:
   - Trong cả 3 thư mục hạt giống tại `HPDynamo.Mcp.Server/Registry/SeedLibrary/` và thư mục roaming `%AppData%\HPDynamo\McpServer\tools-library/`:
   - Thay thế tham chiếu lỗi `?? Dynamo.Applications.DynamoRevit.RevitDynamoModel` thành `?? dynamo as DynamoModel`.
   - Đảm bảo `batch_edit_graph/code.cs` sử dụng đúng `dryRun` và `RunAndWait` của `DynamoScriptGlobals`.
2. **Cập nhật `tool.json`**:
   - Mở `tool.json` của 3 tool:
     - `tools-library/Graph/batch_edit_graph/tool.json`
     - `tools-library/Library/search_nodes_library/tool.json`
     - `tools-library/Workspace/get_active_graph_info/tool.json`
   - Đặt lại `"status": "published"`.
   - Cập nhật `"notes": "Restored and published after compiler fix for CS0234 Dynamo.Applications."`.
   - Cập nhật `"updatedAt": "<UTC ISO timestamp>"`.
3. **Kích hoạt nạp lại thư viện (Library Reload)**:
   - Khi file `tool.json` thay đổi, file watcher của `ToolLibraryStore` (`_store.Changed`) tự động gọi `ToolManager.LoadAllAsync()`.
   - (Hoặc khi MCP Server khởi động lại, `RegistryStartup.StartAsync` sẽ gọi `LoadAllAsync()`).
4. **Xử lý tự động của `ToolManager`**:
   - `ToolManager.LoadAllAsync` nhận diện: `storedStatus` trong DB là `quarantined`, nhưng `record.Status` trong file vừa nạp là `published`.
   - Hàm tự động thực thi:
     ```csharp
     _db.UpsertTool(record);
     _db.InsertEvent(record.Name, "status_changed", "file", "quarantined -> published");
     ```
   - Sự kiện `"status_changed"` được ghi nhận vào `registry_events`, tự động thiết lập mốc thời gian mới cho `StabilityRunFilter`.
5. **Đồng bộ hóa bề mặt công cụ (`DynamicToolRegistrar.Sync()`)**:
   - `ToolManager` kích hoạt event `Changed`.
   - `DynamicToolRegistrar.Sync()` được gọi:
     - Quét toàn bộ `desired = _manager.Tools.Where(t => t.IsPublished)`.
     - Nhận diện 3 công cụ đã `IsPublished == true`.
     - Đóng gói vào `RegistryToolFunction` và thêm vào `McpServerOptions.ToolCollection`.
     - Số lượng tools đăng ký tăng từ 10 lên **13 seed tools**.
     - Tổng bề mặt công cụ MCP đạt chính xác **25 tools** (4 Core + 8 Registry + 13 Seeds).
     - Phát tín hiệu `notifications/tools/list_changed` tới AI client.

#### Quy Trình 2: Phục hồi qua API Quản Trị `ToolLifecycleService` (Programmatic Workflow)
Nếu thực hiện bằng code / test / CLI:
1. **Sửa mã nguồn `code.cs`** (loại bỏ `Dynamo.Applications`).
2. **Gọi `ToolLifecycleService.Manage(name, "restore", reason, actor)`**:
   - Đưa trạng thái từ `Quarantined` về `Draft`.
   - Ghi event `"restore"` vào `registry_events`.
3. **Gọi `ToolLifecycleService.TestAsync(name, cases, realRun: false)`** (`test_tool`):
   - Chạy kiểm thử ví dụ với `dryRun`.
   - Khi 100% case passed, trạng thái chuyển từ `Draft` sang `Tested`.
4. **Gọi `ToolLifecycleService.Approve(name, by: "admin", force: false)` hoặc `Publish(name)`**:
   - Chuyển trạng thái sang `Published`.
   - Ghi event `"approved"` / `"published"` vào `registry_events`.
   - `DynamicToolRegistrar.Sync()` tự động đưa tool lên bề mặt MCP, đạt **25 tools**.

---

### 3.3. Cơ Chế Nâng Cấp Tự Động của `SeedInstaller`
- Khi server phân phối bản cập nhật mới, `SeedInstaller.Install` đối chiếu SHA-256 checksum:
  `Checksum => Sha256(ToolJson + "\n" + Code + "\n" + ExamplesJson)`
- Trong `_seeds.json`:
  Nếu `tool.json` trên ổ đĩa từng bị sửa thành `quarantined`, checksum sẽ lệch so với giá trị trong `_seeds.json`.
  Vì vậy, khi cập nhật binary mới, cần đảm bảo cập nhật đồng bộ cả `_seeds.json` hoặc sử dụng Quy trình 1/2 ở trên để `tool.json` và `_seeds.json` đồng nhất, cho phép các bản nâng cấp tiếp theo được áp dụng tự động mà không bị chặn bởi điều kiện `onDisk != installed`.

---

## 4. Bảng Tổng Hợp So Sánh Trạng Thái Bề Mặt Công Cụ

| Thành Phần | Hiện Trạng (Lúc Quarantined) | Sau Khi Phục Hồi (Mục Tiêu) | Ghi Chú |
|---|:---:|:---:|---|
| **Core Tools** | 4 | 4 | `execute_dynamo_code`, `execute_designscript`, `get_dynamo_context`, `cancel_execution` |
| **Registry Meta Tools** | 8 | 8 | `search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool` |
| **Published Seed Tools** | 10 | **13** | Phục hồi 3 tools: `batch_edit_graph`, `search_nodes_library`, `get_active_graph_info` |
| **Quarantined Tools** | **3** | **0** | Đã xử lý dứt điểm lỗi biên dịch `CS0234` |
| **Tổng Bề Mặt Công Cụ MCP** | **22 tools** | **25 tools** | Khớp 100% yêu cầu R2 Đợt B và bài kiểm tra `ToolSurfaceTests.cs` |
| **Toàn vẹn Dữ liệu Người Dùng** | Giữ nguyên | **100% Bảo toàn** | Không xóa bản ghi `runs`, `registry_events` hay tệp `.db` nào |

---
*Báo cáo được tổng hợp đầy đủ từ mã nguồn `McpShared`, `HPDynamo` và dữ liệu thực nghiệm SQLite `registry.db`.*
