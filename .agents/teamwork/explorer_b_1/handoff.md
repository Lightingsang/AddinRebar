# Handoff Report: Khảo Sát SQLite Registry & Tool Lifecycle (Milestone B)

**Agent**: explorer_b_1  
**Milestone**: Milestone B (Đợt B) — HPDynamo MCP Vòng 2  
**Thư mục làm việc**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_b_1\`  
**Người nhận**: orchestrator_8 (`f5a54e1e-e38f-430b-b7e8-2a44388a5da1`)

---

## 1. Observation (Quan Sát Trực Tiếp)

1. **Vị trí và Tình trạng Database**:
   - Tệp cơ sở dữ liệu SQLite: `C:\Users\STR-HP03\AppData\Roaming\HPDynamo\McpServer\registry.db` (Dung lượng: `688,128` bytes, kèm theo `registry.db-wal` và `registry.db-shm`).
   - Thư mục thư viện tool: `C:\Users\STR-HP03\AppData\Roaming\HPDynamo\McpServer\tools-library\`.
   - Cấu hình SQLite connection (`McpShared/HPRebar.Mcp.Server.Core/Registry/ToolRegistryDb.cs:24,208`):
     `Mode = SqliteOpenMode.ReadWriteCreate, Cache = SqliteCacheMode.Shared`
     `PRAGMA journal_mode = WAL; PRAGMA busy_timeout = 5000; PRAGMA foreign_keys = ON;`

2. **Cấu trúc Bảng SQLite**:
   - `ToolRegistryDb.cs:36–53`: Các bảng gồm:
     - `tools`: `name TEXT PRIMARY KEY, category TEXT, title TEXT, description TEXT, tags TEXT, status TEXT, version INTEGER, checksum TEXT, folder TEXT, record_json TEXT, examples_text TEXT, updated_at INTEGER`
     - `tool_versions`: `name TEXT, version INTEGER, code_sha TEXT, input_schema TEXT, code TEXT, created_at INTEGER, PRIMARY KEY (name, version)`
     - `runs`: `id INTEGER PRIMARY KEY, tool_name TEXT, version INTEGER, kind TEXT, args_sha TEXT, code_sha TEXT, dry_run INTEGER, success INTEGER, duration_ms INTEGER, error TEXT, revit_version TEXT, doc_title TEXT, ts INTEGER, code TEXT, args_json TEXT`
     - `registry_events`: `id INTEGER PRIMARY KEY, ts INTEGER, tool_name TEXT, event TEXT, actor TEXT, detail TEXT`
     - `tools_fts`: FTS5 virtual table on `(name, title, description, tags, examples)`.

3. **Dữ liệu Thực Tế trong Database**:
   - Bảng `tools`: Có tổng cộng 13 bản ghi.
     - 10 tools có `status = 'published'` (`add_node_to_workspace`, `connect_node_ports`, `create_code_block_node`, `disconnect_node_ports`, `export_graph_to_image`, `get_node_output_values`, `inspect_node_type`, `run_dyn_file`, `set_node_properties`, `validate_dyn_file`).
     - 3 tools có `status = 'quarantined'` (`batch_edit_graph`, `search_nodes_library`, `get_active_graph_info`).
   - Bảng `registry_events`:
     - Event ID 4 (`ts = 1791539116954`): `tool_name = 'get_active_graph_info'`, `event = 'quarantined'`, `actor = 'registry'`, `detail = 'Quarantined 2026-10-09 09:45:16Z: 5/5 recent runs failed. Last error: The script does not compile. Fix the listed errors and retry.'`
     - Event ID 5 (`ts = 1791539125068`): `tool_name = 'search_nodes_library'`, `event = 'quarantined'`, `actor = 'registry'`, `detail = 'Quarantined 2026-10-09 09:45:25Z: 5/5 recent runs failed. Last error: The script does not compile. Fix the listed errors and retry.'`
     - Event ID 6 (`ts = 1791539135110`): `tool_name = 'batch_edit_graph'`, `event = 'quarantined'`, `actor = 'registry'`, `detail = 'Quarantined 2026-10-09 09:45:35Z: 5/5 recent runs failed. Last error: The script does not compile. Fix the listed errors and retry.'`
   - Bảng `runs`:
     - `batch_edit_graph`: 5 runs (ID 77–81), tất cả `kind = 'tool'`, `success = 0`, `error = 'The script does not compile. Fix the listed errors and retry.'`
     - `search_nodes_library`: 5 runs (ID 68, 73–76), tất cả `kind = 'tool'`, `success = 0`, `error = 'The script does not compile. Fix the listed errors and retry.'`
     - `get_active_graph_info`: 5 runs (ID 67, 69–72), tất cả `kind = 'tool'`, `success = 0`, `error = 'The script does not compile. Fix the listed errors and retry.'`

4. **Lỗi Biên Dịch Thực Tế (Verbatim Error)**:
   - Trong `scratch/run_dyn_out.json`:
     `"id": "CS0234", "message": "The type or namespace name 'Applications' does not exist in the namespace 'Dynamo' (are you missing an assembly reference?)"`
   - Trong mã nguồn seed tại `HPDynamo.Mcp.Server/Registry/SeedLibrary/`:
     - `batch_edit_graph/code.cs:8`: `var dynamoModel = dynamoViewModel?.Model ?? dynamo as DynamoModel ?? Dynamo.Applications.DynamoRevit.RevitDynamoModel;`
     - `search_nodes_library/code.cs:11`: `?? Dynamo.Applications.DynamoRevit.RevitDynamoModel;`
     - `get_active_graph_info/code.cs:7`: `?? Dynamo.Applications.DynamoRevit.RevitDynamoModel?.CurrentWorkspace;`
   - Trong `HPDynamo.McpBridge/Coordinator/DynamoCompilerReferences.cs:28–48`:
     `DynamoCompilerReferences.All` không đưa `DynamoApplications.dll` vào danh sách `MetadataReference` của Roslyn.

5. **Logic Đăng Ký Công Cụ MCP**:
   - `McpShared/HPRebar.Mcp.Server.Core/Registry/DynamicToolRegistrar.cs:146`:
     `var desired = _manager.Tools.Where(t => t.IsPublished).ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);`
   - Khi 3 công cụ bị `quarantined`, `t.IsPublished == false`, nên chỉ có 10 seeds được đưa vào `McpServerOptions.ToolCollection`.
   - Kết quả: `tools/list` chỉ trả về **22 tools** (4 Core + 8 Registry + 10 Seeds), thiếu 3 công cụ để đạt đủ **25 tools**.

6. **Logic Lọc Tính Ổn Định và Bảo Toàn Lịch Sử Chạy**:
   - `McpShared/HPRebar.Mcp.Server.Core/Registry/ToolRegistryDb.Runs.cs:61–64`:
     ```sql
     kind <> 'test' AND (error IS NULL OR error NOT LIKE 'Argument%Exception:%')
     AND ts >= COALESCE((SELECT MAX(e.ts) FROM registry_events e
                         WHERE e.tool_name = runs.tool_name
                         AND e.event IN ('approved', 'published', 'restore', 'proposed_version', 'imported', 'status_changed')), 0)
     ```
   - `McpShared/HPRebar.Mcp.Server.Core/Registry/ToolManager.cs:92–98`:
     `// A status edited by hand in tool.json (the documented second approval path) is a lifecycle event like an approve: without it a quarantined tool set back to published would be re-quarantined by the failures that got it quarantined.`
     `_db.InsertEvent(record.Name, "status_changed", "file", $"{ToolRegistryDb.StatusText(was)} -> {ToolRegistryDb.StatusText(record.Status)}");`

---

## 2. Logic Chain (Chuỗi Lập Luận)

1. Từ **Observation 3 và 4**:
   - Ba công cụ `batch_edit_graph`, `search_nodes_library`, `get_active_graph_info` bị gọi 5 lần liên tiếp và đều thất bại do lỗi biên dịch `CS0234` vì tham chiếu tới namespace `Dynamo.Applications` (vốn không có trong `DynamoCompilerReferences.All`).
2. Từ **Observation 1 và 3**:
   - Theo quy tắc `StabilityScorer.ShouldQuarantine` với `QuarantineMinRuns = 5` và `QuarantineMaxFailureRate = 0.4`, khi 5/5 lượt chạy gần nhất thất bại (100% > 40%), `ToolManager.RecordRun` tự động chuyển trạng thái của 3 công cụ sang `Quarantined` và ghi đè `tool.json` trên ổ đĩa.
3. Từ **Observation 5**:
   - `DynamicToolRegistrar.Sync()` chỉ đăng ký công cụ có `record.IsPublished == true`. Khi 3 công cụ bị `Quarantined`, chúng bị loại khỏi `ToolCollection`, khiến bề mặt MCP chỉ còn 22 công cụ (thiếu 3 công cụ).
4. Từ **Observation 6**:
   - Hệ thống được trang bị cơ chế cutoff timestamp trong `StabilityRunFilter`: Khi một sự kiện hợp lệ (`status_changed`, `approved`, `published`, hoặc `restore`) được ghi vào `registry_events`, mốc thời gian kiểm tra tính ổn định được nâng lên thời điểm mới.
   - Do đó, các lượt chạy thất bại trước đó trong bảng `runs` tự động nằm ngoài phạm vi tính toán của `StabilityScorer`, giúp công cụ không bị quarantine lại khi phục hồi mà KHÔNG CẦN XÓA bất kỳ bản ghi nào trong bảng `runs`.
5. **Quy trình phục hồi chuẩn**:
   - Bước 1: Sửa mã nguồn `code.cs` loại bỏ dependency `Dynamo.Applications` (thay bằng `?? dynamo as DynamoModel`).
   - Bước 2: Cập nhật `tool.json` chuyển trạng thái về `"status": "published"`.
   - Bước 3: File watcher hoặc server startup gọi `ToolManager.LoadAllAsync()`, phát hiện sự thay đổi trạng thái từ `quarantined` sang `published`, tự động ghi event `"status_changed"` vào `registry_events`, thiết lập mốc cutoff mới cho `StabilityRunFilter`.
   - Bước 4: `DynamicToolRegistrar.Sync()` tự động nạp lại 13 seeds, đưa tổng bề mặt công cụ MCP lên chính xác **25 tools** (4 Core + 8 Registry + 13 Seeds).

---

## 3. Caveats (Các Điểm Giới Hạn)

1. **Phạm vi Explorer**:
   - Nhiệm vụ là khảo sát đọc-chỉ (Read-Only). Explorer TUYỆT ĐỐI không sửa code nguồn C#, không sửa các file `tool.json` hay `code.cs` trong AppData, và không chỉnh sửa database thực tế. Việc sửa code và cập nhật metadata sẽ do implementer thực hiện ở bước tiếp theo của Milestone B.
2. **Phiên Revit Đang Chạy**:
   - Tiến trình Revit PID 26264 đang mở không bị tác động hay can thiệp.
3. **Các Tool Chưa Được Chạy Thực Tế**:
   - Các seed tools khác (`inspect_node_type`, `add_node_to_workspace`, `connect_node_ports`...) có chứa chuỗi `Dynamo.Applications` nhưng chưa bị quarantined chỉ vì chúng chưa chạy đủ 5 lần thực tế. Implementer khi sửa code cần rà soát và loại bỏ `Dynamo.Applications` trên toàn bộ 13 seed tools để ngăn ngừa nguy cơ lỗi tương tự trong tương lai.

---

## 4. Conclusion (Kết Luận)

1. **Vị trí Database**: `C:\Users\STR-HP03\AppData\Roaming\HPDynamo\McpServer\registry.db`.
2. **Nguyên nhân Quarantined**: 3 tools chạy thất bại 5/5 lần liên tiếp với lỗi biên dịch Roslyn `CS0234` (do mã nguồn gọi `Dynamo.Applications.DynamoRevit.RevitDynamoModel` trong khi assembly `DynamoApplications.dll` không được đưa vào `DynamoCompilerReferences.All`). `StabilityScorer` tự động cách ly công cụ theo ngưỡng `QuarantineMinRuns = 5` và `QuarantineMaxFailureRate = 0.4`.
3. **Quy trình Phục Hồi Chuẩn Bảo Toàn Dữ Liệu**:
   - Sửa mã nguồn seed `code.cs` (loại bỏ `Dynamo.Applications`).
   - Cập nhật `tool.json` sang `"status": "published"`.
   - `ToolManager.LoadAllAsync()` ghi event `"status_changed"` vào `registry_events` để reset mốc tính lỗi cho `StabilityRunFilter`, bảo toàn 100% dữ liệu lịch sử trong bảng `runs`.
   - `DynamicToolRegistrar.Sync()` tự động khôi phục bề mặt đầy đủ **25 tools** (4 Core + 8 Registry + 13 Seeds).

---

## 5. Verification Method (Phương Pháp Xác Minh Độc Lập)

1. **Kiểm tra trạng thái SQLite hiện hành (Read-Only SQL Query)**:
   ```powershell
   python -c "import sqlite3; con = sqlite3.connect(r'C:\Users\STR-HP03\AppData\Roaming\HPDynamo\McpServer\registry.db'); cur = con.cursor(); print(cur.execute('SELECT name, status FROM tools WHERE status=\"quarantined\";').fetchall())"
   ```
   *Kết quả hiện tại*: Trả về `[('batch_edit_graph', 'quarantined'), ('get_active_graph_info', 'quarantined'), ('search_nodes_library', 'quarantined')]`.

2. **Kiểm tra Unit Test Bề Mặt Công Cụ (Tool Surface Parity)**:
   ```powershell
   dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj -p:DeployAddin=false
   ```
   *Mục tiêu sau khi sửa*: Tất cả các bài test trong `HPDynamo.Mcp.Server.Tests/Surface/ToolSurfaceTests.cs` (kiểm tra 4 core + 8 registry + 13 seeds = 25 tools) và `SeedManifestTests.cs` đều đạt PASS 100%.

3. **Kiểm tra Bề Mặt MCP Live qua `tools/list`**:
   ```powershell
   python McpShared/tools/mcp-call.py --exe HPDynamo/HPDynamo.Mcp.Server/bin/Debug/net10.0/HPDynamo.Mcp.Server.exe --method tools/list
   ```
   *Điều kiện thành công*: Danh sách trả về chứa đầy đủ chính xác **25 công cụ** (không còn bị thiếu 3 công cụ).
