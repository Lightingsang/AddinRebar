# HP MCP Power BI — Reference Copy

<!-- Dumped from .agents/skills/hp-mcp-powerbi/SKILL.md -->

---
name: hp-mcp-powerbi
description: "Kết nối và điều khiển Microsoft Power BI qua HPPowerBi MCP (server hprebar-powerbi, tool mcp__hprebar-powerbi__*): đọc schema tabular (tables, columns, measures, relationships), đánh giá câu truy vấn DAX (EVALUATE, SUMMARIZECOLUMNS) qua ADOMD.NET, tạo/sửa/xoá measure qua AMO-TOM với snapshot TMSL JSON rollback, quản lý quan hệ bảng (active, cross-filtering), định dạng DAX, thao tác Power BI Service Cloud REST API (workspaces, datasets, refresh, DAX query) qua MSAL OAuth 2.0 / Service Principal, viết C# Roslyn scripting qua execute_powerbi_code, registry tool (propose/test/publish). TRIGGER when: user nhắc 'Power BI', 'powerbi', 'pbi', 'PBIDesktop', 'DAX', 'AMO-TOM', 'ADOMD', 'measure', 'relationship', 'TMSL', 'Power BI Service', 'workspaces', 'datasets', 'refresh', 'hppowerbi-mcp-2026', hoặc lỗi -32001/-32002/-32003 từ tool Power BI. Keywords: powerbi, pbi, pbidesktop, dax, tom, adomd, tmsl, measure, tabular, dataset, mcp, bridge, snapshot, cloud, refresh. Khi cần đọc/sửa mô hình Power BI Desktop hoặc tương tác Power BI Cloud qua MCP."
metadata:
  author: hoang
  version: "1.0.0"
  mcp-server: hprebar-powerbi
---

<!-- portable-host-contract:start -->
## Portable host contract

This skill is shared by Codex and Google Antigravity.

- Use the host's native task tracker. In Codex, use `update_plan`; in Antigravity, maintain the task-list artifact.
- Use native collaboration and user-input tools exposed by the host. Treat legacy tool names as capability descriptions, never literal calls.
- Resolve bundled resources from the project-local `.agents/skills/` tree. Do not create or write a user-global skill directory.
<!-- portable-host-contract:end -->

# HP MCP Power BI — điều khiển Power BI Desktop & Service qua `hprebar-powerbi`

## Overview & Architecture

Skill này hướng dẫn Claude và các coding agent sử dụng bộ công cụ của MCP server `hprebar-powerbi` (`mcp__hprebar-powerbi__*`) để làm việc với mô hình dữ liệu Tabular của **Power BI Desktop** đang mở cục bộ cũng như **Power BI Service (Cloud)**:
Chuỗi liên lạc: **Claude / Coding Agent → HPPowerBi.Mcp.Server (stdio, .NET 10) → Named Pipe `hppowerbi-mcp-2026` → HPPowerBi.McpBridge.exe (Standalone WPF Desktop App, .NET 8) → Local Analysis Services (AMO-TOM / ADOMD.NET) & Power BI Service Cloud REST API (MSAL)**.

```text
Claude / Coding Agent (Stdio MCP Client)
         │
         ▼ (stdio JSON-RPC 2.0)
HPPowerBi.Mcp.Server (.NET 10 Console)
         │
         ▼ (Named Pipe: \\.\pipe\hppowerbi-mcp-2026)
HPPowerBi.McpBridge.exe (.NET 8 WPF Desktop App, MaterialDesign 5.3.2)
         │
         ├─────────────────────────────────────────┐
         ▼ (Local Analysis Services)               ▼ (Cloud REST API)
Power BI Desktop (PBIDesktop.exe)          Power BI Service (api.powerbi.com)
msmdsrv.exe Tabular Engine                 MSAL OAuth 2.0 / Service Principal
(AMO-TOM & ADOMD.NET via TCP Port)         (Workspaces, Datasets, Refresh, DAX)
```

**Đặc điểm an toàn 3 lớp (3-Layer Safety):**
1. **Dual UI Opt-in Switches:** Hai checkbox độc lập trên giao diện Bridge ("Allow AI DAX / code execution" và "Allow AI model modification"), mặc định luôn TẮT khi khởi động.
2. **DAX & Roslyn AST Validation:** Chặn mọi lệnh độc hại (XMLA DDL DROP, Process Kill, System.IO, System.Net, Reflection, dynamic, unsafe, v.v.).
3. **Automatic Pre-mutation TMSL JSON Snapshots:** Tự động sao lưu toàn bộ schema database sang định dạng TMSL JSON tại `%LocalAppData%\HPPowerBi\Snapshots\` trước khi ghi bất kỳ thay đổi nào (`model.SaveChanges()`). Cho phép phục hồi / rollback tức thì khi xảy ra lỗi.

---

## Bước 0 — Kết nối (Checklist theo thứ tự)

1. **Khởi động Power BI Desktop (`PBIDesktop.exe`)** và mở tệp báo cáo `.pbix`. Power BI Desktop sẽ khởi động một instance engine tabular ngầm (`msmdsrv.exe`) lắng nghe trên một cổng TCP cục bộ ngẫu nhiên.
2. **Khởi động Bridge App** `HPPowerBi/output/HPPowerBi.McpBridge/HPPowerBi.McpBridge.exe` (hoặc build `HPPowerBi/HPPowerBi.McpBridge/bin/Debug/net8.0-windows/HPPowerBi.McpBridge.exe`):
   - Bridge tự động quét tiến trình `PBIDesktop.exe`, đọc tệp `msmdsrv.port.txt` trong thư mục `AnalysisServicesWorkspaces` và kết nối AMO-TOM / ADOMD.NET.
   - Nếu có nhiều instance Power BI Desktop đang mở, người dùng có thể chọn instance tương ứng trong dropdown trên thanh tiêu đề.
3. **Bật các checkbox an toàn (Human Opt-in)** trên cửa sổ Bridge:
   - Tick **"Allow AI DAX / code execution"**: Cho phép AI chạy truy vấn DAX hoặc kịch bản C# Roslyn đọc/phân tích dữ liệu.
   - Tick **"Allow AI model modification"**: Chỉ tick khi bạn muốn cho phép AI tạo/sửa/xoá Measure hoặc Relationship (ghi vào mô hình). Tùy chọn này tự động tắt khi tắt execution.
4. **Cấu hình `.mcp.json`**:
   Thêm server entry `hprebar-powerbi` trỏ tới `HPPowerBi.Mcp.Server.exe`:
   ```json
   {
     "mcpServers": {
       "hprebar-powerbi": {
         "command": "g:\\09-PROJECT AI\\01_Revit\\02_CshapRevit\\01_AddinRebar\\HPPowerBi\\HPPowerBi.Mcp.Server\\bin\\Debug\\net10.0\\HPPowerBi.Mcp.Server.exe",
         "args": []
       }
     }
   }
   ```
5. **Gọi `get_powerbi_context` — LUÔN là tool call đầu tiên**:
   Xác minh `isAttached`, `localPort`, `databaseName`, `tableCount`, `measureCount`, `relationshipCount`, `executionEnabled`, `mutationEnabled`.
   - Nếu `isAttached = false`: Yêu cầu người dùng mở Power BI Desktop và khởi động Bridge.
   - Nếu `executionEnabled = false`: Hướng dẫn người dùng tick "Allow AI DAX / code execution".
   - Nếu cần ghi mà `mutationEnabled = false`: Hướng dẫn người dùng tick "Allow AI model modification".

---

## Danh Mục Công Cụ (Tool Catalog)

HPPowerBi MCP Server cung cấp **12 công cụ chuyên dụng** (8 công cụ cục bộ + 4 công cụ cloud) cùng **8 công cụ Dynamic Registry** của hệ sinh thái HP MCP:

### 1. Local Tools (8 công cụ tương tác Power BI Desktop)

| Tên công cụ | Phân loại | Mô tả | Tham số chính |
|---|---|---|---|
| `get_powerbi_context` | Read-only | Trả về trạng thái phiên Power BI Desktop hiện tại: PID, port TCP, tên Database/Model, số lượng Table/Measure/Relationship, trạng thái 2 checkbox opt-in. | (không có) |
| `execute_powerbi_code` | Read/Write | Thực thi kịch bản C# Roslyn trực tiếp trên đối tượng TOM (`model`, `server`) và `adomd`. Cần opt-in tương ứng. | `code`, `transaction` ("none"\|"auto"), `dryRun`, `args` |
| `powerbi_get_schema` | Read-only | Trích xuất toàn bộ hoặc từng bảng trong schema tabular: danh sách cột, kiểu dữ liệu, measures, relationships. | `tableName`, `includeColumns`, `includeMeasures`, `includeRelationships` |
| `powerbi_evaluate_dax` | Read-only | Thực thi câu lệnh DAX (thường bắt đầu bằng `EVALUATE`) qua ADOMD.NET. Trả về kết quả dạng bảng Markdown hoặc JSON cùng thời gian chạy. | `query`, `maxRows` (mặc định 100, max 10000), `format` ("markdown"\|"json") |
| `powerbi_create_or_update_measure` | Mutating | Tạo mới hoặc cập nhật Measure trong bảng được chỉ định. Yêu cầu bật "Allow AI model modification". Tự động snapshot TMSL trước khi ghi. | `tableName`, `measureName`, `expression`, `formatString`, `displayFolder`, `description` |
| `powerbi_delete_measure` | Destructive | Xoá một Measure khỏi bảng. Yêu cầu bật "Allow AI model modification". Tự động snapshot TMSL trước khi xoá. | `tableName`, `measureName` |
| `powerbi_manage_relationship` | Mutating | Quản lý quan hệ giữa 2 bảng: tạo mới, xoá, bật/tắt Active, đổi CrossFilteringBehavior (OneDirection, BothDirections). Tự động snapshot TMSL. | `action` ("create"\|"delete"\|"toggle_active"\|"set_cross_filtering"), `fromTable`, `fromColumn`, `toTable`, `toColumn`, `isActive`, `crossFilteringBehavior` |
| `powerbi_format_dax` | Read-only | Định dạng câu lệnh DAX theo chuẩn chuẩn mực (viết hoa từ khóa, thụt lề thụt dòng, chia dòng VAR/RETURN và CALCULATE). | `dax` |

### 2. Cloud REST Tools (4 công cụ tương tác Power BI Service)

| Tên công cụ | Phân loại | Mô tả | Tham số chính |
|---|---|---|---|
| `powerbi_cloud_list_workspaces` | Cloud Read | Liệt kê các Workspace (nhóm làm việc) trên Power BI Service mà tài khoản / Service Principal có quyền truy cập. | (không có) |
| `powerbi_cloud_list_datasets` | Cloud Read | Liệt kê các Semantic Models (Datasets) trong "My Workspace" hoặc trong một Workspace cụ thể. | `workspaceId` (tuỳ chọn) |
| `powerbi_cloud_trigger_refresh` | Cloud Action | Kích hoạt tác vụ làm mới dữ liệu (Dataset Refresh) cho một Semantic Model trên Power BI Service. | `datasetId`, `workspaceId` (tuỳ chọn), `notifyOption` |
| `powerbi_cloud_execute_dax` | Cloud Read | Thực thi câu truy vấn DAX trực tiếp trên Semantic Model lưu trữ tại Power BI Service qua endpoint `executeQueries`. | `datasetId`, `query`, `workspaceId` (tuỳ chọn) |

### 3. Dynamic Tool Registry (8 công cụ meta kế thừa từ McpShared)

- `search_tools`: Tìm kiếm các công cụ trong thư viện bằng từ khoá.
- `get_tool`: Lấy chi tiết thông tin và mã nguồn của công cụ đã lưu.
- `run_tool`: Chạy công cụ đã publish trong thư viện với tham số JSON.
- `get_run`: Lấy lịch sử thực thi của một lần chạy script trước đó.
- `propose_tool`: Đề xuất đóng gói kịch bản thành công cụ mới.
- `test_tool`: Kiểm thử công cụ được đề xuất trong môi trường an toàn.
- `publish_tool`: Đưa công cụ vào trạng thái chờ phê duyệt.
- `manage_tool`: Quản lý vòng đời công cụ (approve, reject, quarantine, restore, deprecate).

---

## Xử Lý Sự Cố & Mã Lỗi (Troubleshooting)

| Mã lỗi / Hiện tượng | Nguyên nhân | Cách khắc phục |
|---|---|---|
| `-32001` (ExecutionDisabled) | Checkbox "Allow AI DAX / code execution" đang tắt. | Hướng dẫn người dùng tick chọn checkbox này trên cửa sổ HPPowerBi MCP Bridge. |
| `-32001` (MutationDisabled) | Thao tác ghi bị từ chối do checkbox "Allow AI model modification" đang tắt. | Hướng dẫn người dùng tick chọn checkbox "Allow AI model modification" trên cửa sổ Bridge. |
| `-32002` (Busy/Timeout) | Analysis Services đang bận xử lý truy vấn lớn hoặc đang refresh. | Chờ tác vụ hoàn thành hoặc tăng giá trị timeout (tối đa 600 giây). |
| `-32003` (NoDocument) | Không tìm thấy tiến trình `PBIDesktop.exe` hoặc chưa mở tệp `.pbix`. | Mở Power BI Desktop và tải tệp báo cáo cần thao tác. |
| `BridgeNotConnected` | Chưa bật ứng dụng `HPPowerBi.McpBridge.exe`. | Khởi động file `HPPowerBi.McpBridge.exe` từ thư mục output hoặc bin. |
| Lỗi Cloud `401 Unauthorized` | Token MSAL hết hạn hoặc thông tin xác thực Azure AD chưa cấu hình. | Thiết lập biến môi trường `HPPOWERBI_MCP_Cloud__ClientId`, `TenantId`, `ClientSecret`. |
