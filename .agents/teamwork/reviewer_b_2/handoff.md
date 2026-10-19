# Handoff Report: Reviewer #2 for Milestone B (Registry Recovery & 25 Tools Surface)

**Agent**: reviewer_b_2  
**Role**: reviewer, critic  
**Verdict**: **APPROVE**  
**Timestamp**: 2026-10-09T17:30:00Z  
**Target Milestone**: Milestone B (Surface Recovery 25 Tools & Gỡ Quarantined 3 Tools)

---

## Review Summary

**Verdict**: **APPROVE**

Milestone B đã đáp ứng 100% tất cả các yêu cầu kỹ thuật và tiêu chí nghiệm thu được giao trong `ORIGINAL_REQUEST.md` (mục header `## 2026-10-09T14:32:33Z`, yêu cầu R2) và `SCOPE.md`.
Không phát hiện bất kỳ vi phạm tính toàn vẹn (Integrity Violation), không có code giả lập (facade), không có kết quả test hardcoded hay bypass kiểm thử.

---

## 1. Observation

### 1.1. Trạng Thái `tool.json` trong Source Tree và `%AppData%`
Kiểm tra toàn bộ 13 file `tool.json` trong source tree (`HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/**/tool.json`) và 13 file `tool.json` trong `%AppData%\HPDynamo\McpServer\tools-library\`:
- Cả 13/13 source seeds đều có `"status": "published"`.
- Cả 13/13 AppData seeds đều có `"status": "published"`.
- Đặc biệt, 3 tools trước đây bị quarantined đã chuyển sang `"status": "published"`:
  * `batch_edit_graph`: `"status": "published"`
  * `search_nodes_library`: `"status": "published"`
  * `get_active_graph_info`: `"status": "published"`

### 1.2. Trạng Thái SQLite Database (`registry.db`)
Đường dẫn database: `C:\Users\STR-HP03\AppData\Roaming\HPDynamo\McpServer\registry.db`.
Kết quả truy vấn trực tiếp qua SQLite engine:
- `PRAGMA integrity_check`: `[('ok',)]`
- `PRAGMA foreign_key_check`: `[]` (0 vi phạm)
- Bảng `tools`: Tổng cộng **13 rows**, 100% đều có `status = 'published'`, `version = 1`. Thuộc tính `record_json` chứa đầy đủ schema metadata và `"status": "published"`.
- Bảng `registry_events`:
  * Chứa 3 sự kiện `status_changed` mới tại timestamp `1791563782950` cho cả 3 tools:
    - ID 7: `tool_name = 'batch_edit_graph'`, `event = 'status_changed'`, `actor = 'file'`, `detail = 'quarantined -> published'`
    - ID 8: `tool_name = 'search_nodes_library'`, `event = 'status_changed'`, `actor = 'file'`, `detail = 'quarantined -> published'`
    - ID 9: `tool_name = 'get_active_graph_info'`, `event = 'status_changed'`, `actor = 'file'`, `detail = 'quarantined -> published'`
- Bảng `runs`:
  * **Bảo toàn 100% dữ liệu lịch sử**: Đúng **116 / 116 rows** (không có bất kỳ row nào bị xóa).
  * Phân bổ runs: `get_node_output_values` (35), `batch_edit_graph` (5), `search_nodes_library` (5), `get_active_graph_info` (5), `validate_dyn_file` (6), `run_dyn_file` (2), `None` (58).

### 1.3. Bề Mặt Công Cụ MCP (Live MCP Tool Surface)
Thực thi lệnh kiểm tra trực tiếp tiến trình server thực tế:
`python McpShared/tools/mcp-call.py "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe" tools/list`
- Kết quả trả về chính xác **25 tools**:
  * **4 Core Tools**: `execute_dynamo_code`, `execute_designscript`, `get_dynamo_context`, `cancel_execution`.
  * **8 Registry Tools**: `search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`.
  * **13 Embedded Seeds**: `add_node_to_workspace`, `batch_edit_graph`, `connect_node_ports`, `create_code_block_node`, `disconnect_node_ports`, `export_graph_to_image`, `get_active_graph_info`, `get_node_output_values`, `inspect_node_type`, `run_dyn_file`, `search_nodes_library`, `set_node_properties`, `validate_dyn_file`.
- Kiểm tra gọi `tools/call get_tool` cho 3 tools phục hồi: Cả 3 tools đều trả về `status: published`.
- Kiểm tra gọi `tools/call search_tools`: Trả về kết quả khớp và xác nhận trạng thái `published`.

### 1.4. Kết Quả Kiểm Thử (100% Pass)
Đã thực thi độc lập toàn bộ các bộ test suites:
1. `dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj`:
   * **Passed: 139**, Failed: 0, Skipped: 0.
2. `dotnet test HPDynamo/HPDynamo.E2E.Tests/HPDynamo.E2E.Tests.csproj`:
   * **Passed: 209**, Failed: 0, Skipped: 0.
3. `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj`:
   * **Passed: 915**, Failed: 0, Skipped: 0.
4. `dotnet test HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj`:
   * **Passed: 223**, Failed: 0, Skipped: 0 (Bao gồm `ProductionSeedCompilationTests` biên dịch sạch cả 13 seeds bằng production `ScriptCompiler`).
5. `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj`:
   * **Passed: 115**, Failed: 0, Skipped: 0.
- **Tổng cộng**: **1,601 / 1,601 tests PASSED (0 failures, 0 regressions)**.

### 1.5. Clean Code Governance & Integrity Audit
- **Rule C2 (File length <= 300 dòng)**:
  * `batch_edit_graph/code.cs`: 279 dòng.
  * `get_node_output_values/code.cs`: 255 dòng.
  * `HostScriptContracts.cs`: 262 dòng.
  * `DynamoCompilerReferences.cs`: 213 dòng.
  * Toàn bộ 13 seed files đều <= 300 dòng.
- **Rule Q-B1 (Không có commented-out code)**: 0 vi phạm.
- **Rule Q-B2 (Không có empty catch block không giải thích)**: 0 vi phạm.

---

## 2. Logic Chain

1. **Khắc phục lỗi biên dịch seeds**:
   - Các seed trước đây gặp lỗi biên dịch do gọi các kiểu không tồn tại trong public runtime (như `DynamoRevitModel`).
   - Việc chuẩn hóa pattern sang `(dynamo as Dynamo.ViewModels.DynamoViewModel)?.Model ?? (dynamo as DynamoModel)` và nạp đầy đủ references trong `DynamoCompilerReferences.All` cho phép cả 13 seeds biên dịch sạch sẽ 100% thông qua chính production `ScriptCompiler`.
2. **Cơ chế phục hồi Registry chuẩn xác mà không mất dữ liệu**:
   - Trong `ToolRegistryDb.Runs.cs`, `StabilityRunFilter` định nghĩa:
     `ts >= COALESCE((SELECT MAX(e.ts) FROM registry_events e WHERE e.tool_name = runs.tool_name AND e.event IN ('approved', 'published', 'restore', 'proposed_version', 'imported', 'status_changed')), 0)`
   - Việc chèn sự kiện `status_changed` với timestamp `1791563782950` thiết lập một mốc cutoff mới. Bộ lọc tính độ ổn định của công cụ chỉ xét các lần chạy sau mốc này. Do đó, 5 lần chạy lỗi cũ trong quá khứ được bỏ qua mà không làm mất lịch sử telemetry (116 rows vẫn bảo toàn nguyên vẹn).
3. **Bề mặt công cụ MCP**:
   - Khi server khởi động, `SeedInstaller` đọc manifest seeds với status `published` và database registry ghi nhận trạng thái `published`, `tools/list` trả về chính xác 25 tools (4 Core + 8 Registry + 13 Seeds).
4. **Tính nhất quán đa môi trường**:
   - Cả source tree, AppData tools library, embedded manifest và SQLite database đều hoàn toàn đồng bộ trạng thái `published`.
   - Kết hợp với 1,601 test cases pass hoàn toàn, chứng minh tính đúng đắn, ổn định và không phát sinh hồi quy.

---

## 3. Adversarial Challenges & Stress Testing

### Challenge 1: Nguy cơ tái cách ly tức thì (Premature Re-quarantine)
- *Giả thuyết tấn công*: Nếu một tool vừa được un-quarantine nhưng trong DB vẫn còn 5 lần fail cũ, liệu lần chạy đầu tiên bị lỗi có khiến tool lập tức bị quarantine lại không?
- *Kiểm chứng thực tế*:
  - Đã chạy script kiểm tra `StabilityRunFilter` trên DB hiện tại:
    * `batch_edit_graph`: Tổng runs trong DB = 5, runs hợp lệ dưới StabilityRunFilter = 0.
    * `search_nodes_library`: Tổng runs trong DB = 5, runs hợp lệ dưới StabilityRunFilter = 0.
    * `get_active_graph_info`: Tổng runs trong DB = 5, runs hợp lệ dưới StabilityRunFilter = 0.
  - *Kết luận*: Nhờ mốc `status_changed`, cửa sổ tính stability được reset về 0 runs. Cần ít nhất 3/5 failures *mới* sau mốc này thì tool mới bị quarantine lại. Kịch bản lỗi không xảy ra.

### Challenge 2: Xung đột hoặc lệch pha giữa Source Tree, Embedded Resource và AppData
- *Giả thuyết tấn công*: `tool.json` trên ổ đĩa có thể đã sửa thành published nhưng embedded resource trong assembly hoặc trong `%AppData%` vẫn là quarantined.
- *Kiểm chứng thực tế*:
  - Đã kiểm tra trực tiếp: Cả 13 file source tree, 13 file AppData, và assembly resource (thông qua test `QuarantinedSeeds_RecoveredAndPublished` và `ToolSurface_ReturnsExactly25Tools_WithAllSeedsPublished`) đều đồng nhất `"status": "published"`.
  - Không có sự lệch pha nào giữa 3 vị trí lưu trữ.

### Challenge 3: Đánh giá tính toàn vẹn (Integrity Violation Check)
- *Kiểm tra các hình thức gian lận*:
  - Có hardcoded test results không? Không. `ProductionSeedCompilationTests` thực hiện Roslyn CSharp compilation thật trên từng file `code.cs` với tập assembly references đầy đủ.
  - Có dummy/facade implementations không? Không. Logic `batch_edit_graph` có đầy đủ 4 snapshot trackers (`createdNodeGuids`, `createdWiresBetweenExistingNodes`, `replacedWires`, `modifiedProperties`) và logic rollback đảo ngược.
  - Có xóa bớt dữ liệu để che giấu lỗi không? Không. Toàn bộ 116 records trong `runs` được bảo tồn nguyên vẹn 100%.

---

## 4. Caveats
- Phiên bản Revit cài đặt trên máy dev là Revit 2026 với `System.Runtime 10.0.0.0` (.NET 10); việc `HPDynamo.McpBridge` target `net10.0-windows7.0` là chuẩn xác theo ADR-DYN-09.
- Nguyên tắc Zero Touch được tuân thủ nghiêm ngặt: Không có can thiệp hay inject vào tiến trình Revit đang mở (PID 26264).

---

## 5. Conclusion
**VERDICT: APPROVE**

Milestone B đã hoàn thành xuất sắc, trung thực và toàn diện:
1. Gỡ bỏ trạng thái `quarantined` thành công cho cả 3 tools: `batch_edit_graph`, `search_nodes_library`, `get_active_graph_info`.
2. Đồng bộ hóa 100% trạng thái `"published"` trên source tree, AppData, SQLite DB và assembly embedded resources.
3. Bảo toàn nguyên vẹn 100% dữ liệu lịch sử bảng `runs` (116/116 rows).
4. Bề mặt công cụ MCP hiển thị đầy đủ và chính xác **25 tools** (4 Core + 8 Registry + 13 Seeds).
5. Toàn bộ 5 test suites chạy qua đạt **1,601 / 1,601 tests PASSED (100% green, 0 failures, 0 regressions)**.
6. Tuân thủ nghiêm ngặt Clean Code Governance (C2, M3, Q-B1, Q-B2).

---

## 6. Verification Method

Bất kỳ ai cũng có thể độc lập tái xác minh kết quả đánh giá trên bằng các lệnh sau:

1. **Kiểm tra SQLite DB và bảo toàn 116 rows**:
   ```bash
   python -c "import sqlite3, os; p = os.path.expandvars(r'%APPDATA%\HPDynamo\McpServer\registry.db'); c = sqlite3.connect(p); cur = c.cursor(); cur.execute('SELECT COUNT(*) FROM runs'); print('runs count:', cur.fetchone()[0]); cur.execute('SELECT COUNT(*) FROM tools WHERE status=\'published\''); print('published tools count:', cur.fetchone()[0]); c.close()"
   ```
   *Kết quả kỳ vọng*: `runs count: 116`, `published tools count: 13`.

2. **Kiểm tra bề mặt 25 tools MCP Server live**:
   ```bash
   python McpShared/tools/mcp-call.py "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\bin\Debug\net10.0\HPDynamo.Mcp.Server.exe" tools/list
   ```
   *Kết quả kỳ vọng*: Trả về đúng 25 tools.

3. **Chạy các bộ kiểm thử tự động**:
   ```bash
   dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj
   dotnet test HPDynamo/HPDynamo.E2E.Tests/HPDynamo.E2E.Tests.csproj
   (cd McpShared && dotnet test HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj)
   (cd HPDynamo && dotnet test HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj)
   (cd McpShared && dotnet test HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj)
   ```
   *Kết quả kỳ vọng*: 100% tests Passed.
