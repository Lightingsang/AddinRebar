---
name: hp-mcp-powerbi
description: "Kết nối và điều khiển Microsoft Power BI qua HPPowerBi MCP (server hprebar-powerbi, tool mcp__hprebar-powerbi__*): đọc schema tabular (tables, columns, measures, relationships), đánh giá câu truy vấn DAX (EVALUATE, SUMMARIZECOLUMNS) qua ADOMD.NET, tạo/sửa/xoá measure qua AMO-TOM với snapshot TMSL JSON rollback, quản lý quan hệ bảng (active, cross-filtering), định dạng DAX, thao tác Power BI Service Cloud REST API (workspaces, datasets, refresh, DAX query) qua MSAL OAuth 2.0 / Service Principal, viết C# Roslyn scripting qua execute_powerbi_code, registry tool (propose/test/publish). TRIGGER when: user nhắc 'Power BI', 'powerbi', 'pbi', 'PBIDesktop', 'DAX', 'AMO-TOM', 'ADOMD', 'measure', 'relationship', 'TMSL', 'Power BI Service', 'workspaces', 'datasets', 'refresh', 'hppowerbi-mcp-2026', hoặc lỗi -32001/-32002/-32003 từ tool Power BI. Keywords: powerbi, pbi, pbidesktop, dax, tom, adomd, tmsl, measure, tabular, dataset, mcp, bridge, snapshot, cloud, refresh. Khi cần đọc/sửa mô hình Power BI Desktop hoặc tương tác Power BI Cloud qua MCP."
metadata:
  author: hoang
  version: "1.0.0"
  mcp-server: hprebar-powerbi
---


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
         ▼ (Named Pipe: <host-path>)
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
         "command": "<host-path>",
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

## Workflow Decision Tree

```text
Yêu cầu của người dùng
 │
 ├─ Khám phá cấu trúc bảng / cột / quan hệ
 │   └── powerbi_get_schema (hoặc đọc resource powerbi://schema)
 │
 ├─ Truy vấn dữ liệu / kiểm tra kết quả tính toán
 │   ├── Định dạng câu truy vấn: powerbi_format_dax
 │   └── Thực thi: powerbi_evaluate_dax (format="markdown" hoặc "json", maxRows ≤ 10000)
 │
 ├─ Thêm mới hoặc sửa đổi Measure tính toán
 │   ├── (Khuyến nghị) Nhờ prompt tối ưu DAX: powerbi_dax_optimize
 │   └── Thực thi: powerbi_create_or_update_measure (tự động snapshot TMSL JSON)
 │
 ├─ Xoá Measure không còn sử dụng
 │   └── powerbi_delete_measure (tự động snapshot TMSL JSON)
 │
 ├─ Quản lý Relationship giữa các bảng
 │   └── powerbi_manage_relationship (Action="create"|"delete"|"toggle_active"|"set_cross_filtering")
 │
 ├─ Tác vụ Power BI Service (Cloud)
 │   ├── Liệt kê không gian làm việc: powerbi_cloud_list_workspaces
 │   ├── Liệt kê semantic models/datasets: powerbi_cloud_list_datasets
 │   ├── Kích hoạt làm mới dữ liệu: powerbi_cloud_trigger_refresh
 │   └── Chạy truy vấn DAX trên Cloud: powerbi_cloud_execute_dax
 │
 ├─ Tác vụ C# Tabular Object Model (TOM) nâng cao
 │   └── execute_powerbi_code (globals: model, server, adomd, ct, log, progress, args)
 │
 └─ Đóng gói thành công cụ tái sử dụng (Toolify)
     └── get_run → propose_tool → test_tool → publish_tool → CLI approve
```

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

## Kiến Trúc An Toàn 3 Lớp (3-Layer Safety Architecture)

### 1. Lớp 1 — Human UI Opt-in Toggles (Dual Switches)
Trên cửa sổ Bridge, người dùng nắm toàn quyền kiểm soát thông qua 2 checkbox:
- **"Allow AI DAX / code execution"**: Cổng chặn mọi hoạt động thực thi. Nếu tắt, mọi yêu cầu chạy DAX hoặc kịch bản C# đều bị từ chối ngay lập tức với lỗi JSON-RPC `-32001`.
- **"Allow AI model modification"**: Cổng chặn các tác vụ sửa đổi mô hình dữ liệu (tạo/sửa/xoá measure, sửa relationship). Nếu tắt, chỉ các lệnh đọc (Read) được phép hoạt động; các lệnh ghi (Write/Delete) bị từ chối với lỗi `-32001`.

### 2. Lớp 2 — DAX Syntax & Script AST Security Guard
- **DAX Safety Guard:** Quét và từ chối các câu lệnh DAX hoặc câu lệnh XMLA có dấu hiệu can thiệp trái phép (`<Drop`, `<Alter`, `DISCOVER_SESSIONS`, `KILL`).
- **Roslyn Script Guard:** Phân tích cú pháp AST của mã C# trước khi biên dịch:
  - Chặn các namespace nguy hiểm: `System.IO`, `System.Net`, `System.Diagnostics.Process`, `System.Reflection`, `System.Threading`.
  - Chặn từ khóa nguy hiểm: `unsafe`, `dynamic`, `await`, `Task`, `Thread`, `#r`, `#load`.

### 3. Lớp 3 — Automatic Pre-mutation TMSL JSON Snapshots
- Mỗi khi một thao tác ghi chuẩn bị diễn ra (`model.SaveChanges()` hoặc `powerbi_create_or_update_measure`, `powerbi_delete_measure`, `powerbi_manage_relationship`):
  - `PbiSnapshotManager` tự động trích xuất toàn bộ schema hiện tại của Database sang tệp định dạng TMSL JSON chuẩn:
    `%LocalAppData%\HPPowerBi\Snapshots\Snapshot_<DatabaseName>_<Timestamp>_<Label>.json`
  - Tự động duy trì tối đa 50 bản sao lưu gần nhất (pruning).
  - Tên tệp snapshot được trả về trong trường `Snapshot` của kết quả thực thi.
  - Người dùng hoặc AI có thể khôi phục (restore/rollback) lại mô hình bất kỳ lúc nào nếu measure mới gây lỗi tính toán hoặc hỏng mô hình.

---

## Đề xuất tool — kiểm tra chất lượng code (ADR-0007)

`propose_tool` chạy bộ kiểm tra chất lượng của bridge trên `code` (quy tắc: `docs/clean-code/HP_CLEAN_CODE_CORE.md` §13, phụ lục host: `docs/clean-code/host-appendix/powerbi.md`):

- **Bị từ chối (lỗi):** code bị comment lại (Q-B1), `catch` rỗng không có comment nói lý do (Q-B2), script > 300 dòng (Q-B3). Sửa code rồi `propose_tool` lại.
- **Chỉ cảnh báo:** khối / local function > 50 dòng (Q-W1), lồng > 3 cấp (Q-W2), tên mơ hồ `data`/`tmp`/`obj`/`res`/`val`… (Q-W3), tham số `bool` trên local function (Q-W4), `catch (Exception)` nuốt lỗi — không throw, không return, không dùng `ex` (Q-W5). Nên sửa trước khi publish.
- **`quality not analysed`:** bridge đang chạy là bản cũ, chưa có bộ kiểm tra — draft vẫn được lưu và publish vẫn được phép; ghi nhận trong báo cáo, redeploy bridge khi có thể.

## Resources & Prompts

### Resources
- `powerbi://schema` (`powerbi_schema`): Trả về toàn bộ schema tabular hiện hành dạng JSON (danh sách bảng, cột, kiểu dữ liệu, measures, relationships).
- `powerbi://document/info` (`powerbi_document_info`): Trả về thông tin tóm tắt phiên làm việc: phiên bản, tiêu đề báo cáo, cổng TCP msmdsrv, tên database, số lượng bảng và measures.

### Prompts
- `powerbi_dax_optimize`: Cung cấp persona kỹ sư tối ưu hóa DAX chuyên nghiệp, hướng dẫn AI:
  * Sử dụng hàm `DIVIDE(n, d, alt)` thay thế cho phép chia `/` để tránh lỗi chia cho 0.
  * Tận dụng cú pháp `VAR ... RETURN ...` để lưu trữ kết quả tạm thời, tránh việc tính toán lặp lại.
  * Không sử dụng `FILTER` trên fact table lớn; ưu tiên lọc trên dimension table hoặc dùng boolean filter trong `CALCULATE`.
  * Khai thác quan hệ Star Schema với `RELATED` / `RELATEDTABLE`.
  * Viết câu lệnh định dạng rõ ràng, viết hoa từ khoá DAX.

---

## Tích Hợp External Tools Trong Power BI Desktop

`HPPowerBi.McpBridge` hỗ trợ tự động đăng ký vào thanh công cụ mở rộng của Power BI Desktop (**External Tools Ribbon**):
- Tệp đăng ký: `HPPowerBi.pbitool.json`.
- Đường dẫn đăng ký:
  * Hệ thống (ưu tiên): `%CommonProgramFiles%\Microsoft Shared\Power BI Desktop\External Tools\HPPowerBi.pbitool.json`
  * Cục bộ người dùng (fallback): `%LocalAppData%\Microsoft\Power BI Desktop\External Tools\HPPowerBi.pbitool.json`
- Khi mở Power BI Desktop, nút **"HP Power BI MCP"** sẽ tự động xuất hiện trên tab **External Tools**. Người dùng chỉ cần nhấp vào nút này để khởi chạy Bridge với tham số cổng và tên database của instance đang mở!

---

## Kịch Bản Viết Code C# (`execute_powerbi_code`) Contract

Khi các tool dựng sẵn không đáp ứng đủ nhu cầu phức tạp, bạn có thể viết script C# Roslyn gửi qua `execute_powerbi_code`:
- **Globals có sẵn:**
  * `model` (`Microsoft.AnalysisServices.Tabular.Model`): Đối tượng mô hình Tabular cấp cao của AMO-TOM.
  * `server` (`Microsoft.AnalysisServices.Tabular.Server`): Đối tượng kết nối server Analysis Services cục bộ.
  * `adomd` (`Microsoft.AnalysisServices.AdomdClient.AdomdConnection`): Kết nối ADOMD.NET dùng để mở DataReader chạy DAX/MDX.
  * `ct` (`CancellationToken`): Token hủy tác vụ cooperative.
  * `log(string)`: Ghi log ra audit stream của Bridge.
  * `progress(int current, int total, string message)`: Báo tiến độ.
  * `args` (`ScriptArgs`): Nhận các tham số truyền vào từ client.
- **Imports mặc định:** `System`, `System.Linq`, `System.Collections.Generic`, `Microsoft.AnalysisServices.Tabular`, `Microsoft.AnalysisServices.AdomdClient`, `HPRebar.McpBridge.Core.Scripting`.
- **Nguyên tắc:** Script kết thúc bằng `return <anonymous object hoặc data>;`. Nếu thực hiện sửa đổi qua TOM, bắt buộc phải bật "Allow AI model modification" và nên gọi `model.SaveChanges()`.

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
