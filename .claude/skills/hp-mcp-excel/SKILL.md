---
name: hp-mcp-excel
description: "Kết nối và điều khiển Microsoft Excel qua HPExcel MCP (server hprebar-excel, tool mcp__hprebar-excel__*): đọc dữ liệu (read_range, find_cells, read_table, read_worksheet_info), ghi và định dạng (write_range, format_range, create_table, manage_worksheet), biểu đồ và công thức (create_chart, evaluate_formula, export_worksheet, run_macro), viết C# Excel COM / ClosedXML qua execute_excel_code với 3-tier R/W/D và snapshot .xlsx tự động, registry tool (propose/test/publish). TRIGGER when: user nhắc 'Excel', 'xlsx', 'workbook', 'worksheet', 'sheet', 'cell', 'range', 'hprebar-excel', 'hpexcel-mcp-2026', 'ClosedXML', 'VBA', 'macro', hoặc lỗi -32001/-32002 từ tool Excel. Keywords: excel, xlsx, workbook, worksheet, range, cell, closedxml, vba, macro, mcp, bridge, snapshot, formula, chart."
metadata:
  author: hoang
  version: "1.0.0"
  mcp-server: hprebar-excel
---

<!-- portable-host-contract:start -->
## Portable host contract

This skill is shared by Codex and Google Antigravity.

- Use the host's native task tracker. In Codex, use `update_plan`; in Antigravity, maintain the task-list artifact.
- Use native collaboration and user-input tools exposed by the host. Treat legacy tool names as capability descriptions, never literal calls.
- Resolve bundled resources from the project-local `.agents/skills/` tree. Do not create or write a user-global skill directory.
<!-- portable-host-contract:end -->

# HP MCP Excel — Điều Khiển Microsoft Excel & Tệp .xlsx qua `hprebar-excel`

## Overview & Kiến Trúc Hệ Thống

Skill này hướng dẫn Claude và các coding agent khai thác toàn diện hệ sinh thái **HPExcel MCP** (`mcp__hprebar-excel__*`) để làm việc với các bảng tính **Microsoft Excel** đang mở cục bộ cũng như xử lý tệp `.xlsx` ở chế độ ngầm (**Headless ClosedXML**):

Chuỗi giao tiếp end-to-end:
**Claude / Coding Agent (Stdio MCP Client) → HPExcel.Mcp.Server (.NET 10 Console) → Named Pipe `hpexcel-mcp-2026` → HPExcel.McpBridge.exe (Standalone WPF Desktop App, .NET 8) → Microsoft Excel COM Interop & ClosedXML Engine**.

```text
Claude / Coding Agent (Stdio MCP Client)
         │
         ▼ (stdio JSON-RPC 2.0)
HPExcel.Mcp.Server (.NET 10 Console Executable)
         │
         ▼ (Named Pipe: \\.\pipe\hpexcel-mcp-2026)
HPExcel.McpBridge.exe (.NET 8 WPF Desktop App, MaterialDesign 5.3.2)
         │
         ├─────────────────────────────────────────┐
         ▼ (Live COM Automation)                   ▼ (Headless Engine)
Microsoft Excel (EXCEL.EXE)                        ClosedXML Engine
Excel.Application / ActiveWorkbook                 Direct .xlsx File I/O
(Running Object Table & STA Worker Thread)         (Fast, no Excel process needed)
```

### Điểm Sáng Kiến Trúc

1. **Khả Năng Vận Hành Kép (Hybrid Execution):**
   - **Live COM Mode:** Tương tác trực tiếp với phiên Microsoft Excel đang chạy: đọc cell người dùng đang chọn, xem đồ thị/bảng tính hiển thị trực quan, chạy VBA macro, render định dạng tức thì.
   - **Headless ClosedXML Mode:** Đọc và ghi tệp `.xlsx` độc lập tốc độ cao mà không cần cài đặt hoặc mở Microsoft Excel, lý tưởng cho xử lý dữ liệu lớn, server, batch jobs.
2. **STA Worker Thread & IOleMessageFilter:**
   - Toàn bộ lệnh COM được điều phối qua một Single-Threaded Apartment (STA) worker thread riêng biệt (`ExcelStaWorker`), loại trừ triệt để lỗi luồng `RPC_E_WRONG_THREAD` (0x8001010E).
   - Tự động đăng ký `IOleMessageFilter` bắt và retry các trạng thái bận `SERVERCALL_RETRYLATER` (0x8001010A) khi người dùng đang chỉnh sửa ô trong giao diện Excel.
3. **Kiến Trúc An Toàn 3 Tầng (3-Tier Safety Engine):**
   - Phân cấp rõ ràng: **Tier R (ReadOnly)**, **Tier W (Write)**, **Tier D (Destructive)**.
   - Dual/Triple Human UI Opt-in Toggles trên giao diện Bridge (mặc định luôn tắt khi khởi động).
   - Tự động tạo snapshot sao lưu `.xlsx` vào thư mục `.hpexcel_snapshots/` trước mọi thao tác ghi hoặc phá hủy.

---

## Bước 0 — Kết Nối (Checklist Theo Thứ Tự)

1. **Khởi động Microsoft Excel (`EXCEL.EXE`)** và mở workbook cần xử lý (nếu làm việc ở chế độ tương tác trực tiếp).
2. **Khởi động Bridge App** `HPExcel/output/HPExcel.McpBridge/HPExcel.McpBridge.exe`:
   - Bridge tự động phát hiện các tiến trình `EXCEL.EXE` đang mở qua `ExcelProcessDetector`.
   - Bấm **Attach** vào instance Excel mong muốn (nếu chưa tự động attach).
   - Nếu làm việc với tệp `.xlsx` trên đĩa chưa mở trong Excel, có thể sử dụng chế độ Headless ClosedXML.
3. **Bật các checkbox an toàn (Human UI Opt-in)** trên cửa sổ Bridge:
   - Tick **"Allow AI execution"**: Cho phép AI đọc dữ liệu và chạy kịch bản C# (Tier R).
   - Tick **"Allow write operations"**: Cho phép AI ghi ô, định dạng, tạo bảng, chèn đồ thị (Tier W). Tự động tắt nếu gate Execution tắt.
   - Tick **"Allow destructive operations"**: Cho phép AI xoá worksheet, clear nội dung diện rộng, hoặc chạy VBA Macro (Tier D). Chỉ tick khi người dùng đồng ý.
4. **Cấu hình `.mcp.json`**:
   Thêm entry `hprebar-excel` trỏ tới `HPExcel.Mcp.Server.exe`:
   ```json
   {
     "mcpServers": {
       "hprebar-excel": {
         "command": "g:\\09-PROJECT AI\\01_Revit\\02_CshapRevit\\01_AddinRebar\\HPExcel\\HPExcel.Mcp.Server\\bin\\Debug\\net10.0\\HPExcel.Mcp.Server.exe",
         "args": []
       }
     }
   }
   ```
5. **Gọi `get_excel_context` — LUÔN là tool call đầu tiên**:
   Xác minh các thông số quan trọng:
   - `isAttached`: Bridge đã kết nối với Excel hay chưa.
   - `docTitle`, `docPath`: Tên và đường dẫn workbook đang mở.
   - `activeSheetName`: Tên sheet đang kích hoạt.
   - `selectedRange`: Vùng ô người dùng đang bôi đen.
   - `executionEnabled`, `writeEnabled`, `destructiveEnabled`: Trạng thái 3 chốt kiểm soát an toàn.

---

## Workflow Decision Tree

```text
Yêu cầu của người dùng
 │
 ├─ Khám phá / Đọc dữ liệu (Tier R - ReadOnly)
 │   ├── Tổng quan phiên làm việc: get_excel_context
 │   ├── Thông tin cấu trúc sheets, tables, charts: read_worksheet_info
 │   ├── Đọc dữ liệu vùng ô / ma trận giá trị: read_range (outputFormat="values"|"formulas"|"formatted")
 │   ├── Tìm kiếm nội dung ô / công thức: find_cells (query="...", exactMatch=false)
 │   ├── Đọc bảng biểu có cấu trúc: read_table (tableName="...")
 │   └── Đánh giá giá trị công thức: evaluate_formula (formula="SUM(A1:A10)")
 │
 ├─ Chỉnh sửa & Định dạng (Tier W - Write, Tự động Snapshot)
 │   ├── Ghi dữ liệu mảng / bảng biểu: write_range (values=[[...]], autoFitColumns=true)
 │   ├── Định dạng phông, màu sắc, viền, số: format_range (numberFormat="...", bold=true, backgroundColor="#...")
 │   ├── Chuyển vùng dữ liệu thành bảng ListObject: create_table (tableName="...", tableStyle="...")
 │   ├── Vẽ biểu đồ trực quan: create_chart (chartType="ColumnClustered"|"Line"|"Pie", dataRange="...")
 │   ├── Xuất bản tính ra file: export_worksheet (format="pdf"|"csv")
 │   └── Quản lý Sheet (Tạo mới, đổi tên, sao chép, ẩn): manage_worksheet (action="add"|"rename"|"duplicate"|"hide"|"unhide")
 │
 ├─ Tác vụ Nguy hiểm / Phá hủy (Tier D - Destructive, Tự động Snapshot + UI Gating)
 │   ├── Xoá hoàn toàn một Worksheet: manage_worksheet (action="delete")
 │   └── Thực thi macro / thủ tục VBA: run_macro (macroName="...", args=[...])
 │
 ├─ Thao tác C# Nâng cao (COM Interop & ClosedXML)
 │   └── execute_excel_code (globals: excel, workbook, sheet, closedXml, ct, log, progress, args)
 │       ├── dryRun=true: Xem trước phân tích cú pháp AST và danh sách member ghi (PREVIEW)
 │       └── dryRun=false: Thực thi thật sự trên STA Worker với snapshot tự động
 │
 └─ Đóng gói thành công cụ tái sử dụng (Toolify)
     └── get_run → propose_tool → test_tool → publish_tool → CLI approve
```

---

## Danh Mục Chi Tiết 12 Seed Tools

Hệ sinh thái HPExcel MCP trang bị sẵn **12 Embedded Seed Tools** thuộc 7 nhóm danh mục chuẩn:

### 1. Data Tools (5 công cụ)

#### `read_range`
- **Mục đích:** Đọc giá trị, công thức hoặc văn bản hiển thị từ một vùng ô (`A1:D10`) hoặc named range sang JSON. Hỗ trợ đọc ma trận mảng 2D tốc độ cực cao (`Range.Value2` trong COM hoặc `IXLRange` trong ClosedXML).
- **Phân cấp:** Tier R (ReadOnly) | `transaction: "none"` | Timeout: 30s.
- **Tham số:**

| Tên tham số | Kiểu | Bắt buộc | Mặc định | Mô tả |
|---|---|---|---|---|
| `range` | string | Không | UsedRange | Địa chỉ vùng ô (ví dụ: `'A1:C10'`, `'Sheet2!B5:E20'`) hoặc named range. Nếu bỏ trống, đọc toàn bộ used range của sheet. |
| `sheet` | string | Không | Active Sheet | Tên sheet cần đọc. |
| `workbook` | string | Không | Active Workbook | Tên hoặc đường dẫn tệp workbook. |
| `outputFormat` | string | Không | `"values"` | Dạng dữ liệu trả về: `"values"` (giá trị thô), `"formulas"` (công thức tính toán), `"formatted"` (chuỗi hiển thị định dạng). |
| `hasHeaders` | boolean | Không | `false` | Nếu `true`, coi dòng đầu là tiêu đề và trả về danh sách JSON object theo tên cột. |
| `maxRows` | integer | Không | `1000` | Số lượng hàng tối đa được phép trả về. |

- **Ví dụ gọi:**
```json
{
  "range": "A1:D20",
  "hasHeaders": true,
  "outputFormat": "values"
}
```

---

#### `find_cells`
- **Mục đích:** Tìm kiếm chuỗi, số hoặc biểu thức công thức trên một worksheet hoặc trên toàn bộ workbook. Trả về danh sách địa chỉ ô và giá trị phù hợp.
- **Phân cấp:** Tier R (ReadOnly) | `transaction: "none"` | Timeout: 30s.
- **Tham số:**

| Tên tham số | Kiểu | Bắt buộc | Mặc định | Mô tả |
|---|---|---|---|---|
| `query` | string | **Có** | - | Chuỗi hoặc giá trị cần tìm kiếm. |
| `sheet` | string | Không | Active Sheet | Sheet cần tìm. Mặc định là active sheet. |
| `workbook` | string | Không | Active Workbook | Tên hoặc đường dẫn workbook. |
| `lookIn` | string | Không | `"values"` | Phạm vi tìm kiếm: `"values"` (tìm theo giá trị ô) hoặc `"formulas"` (tìm theo công thức). |
| `exactMatch` | boolean | Không | `false` | Tìm kiếm chính xác toàn bộ ô (`true`) hay tìm kiếm chuỗi con (`false`). |
| `searchAllSheets` | boolean | Không | `false` | Nếu `true`, quét qua tất cả các worksheet trong workbook. |
| `maxResults` | integer | Không | `100` | Giới hạn số lượng ô kết quả trả về. |

- **Ví dụ gọi:**
```json
{
  "query": "Total",
  "lookIn": "values",
  "exactMatch": false,
  "searchAllSheets": true
}
```

---

#### `read_table`
- **Mục đích:** Đọc cấu trúc bảng Excel (`ListObject`): danh sách tiêu đề (`HeaderRowRange`), các dòng dữ liệu dạng bản ghi JSON (`DataBodyRange`), và dòng tổng cộng (`TotalsRowRange`).
- **Phân cấp:** Tier R (ReadOnly) | `transaction: "none"` | Timeout: 30s.
- **Tham số:**

| Tên tham số | Kiểu | Bắt buộc | Mặc định | Mô tả |
|---|---|---|---|---|
| `tableName` | string | **Có** | - | Tên bảng Excel cần đọc (ví dụ: `'Table1'`, `'RebarSchedule'`). |
| `sheet` | string | Không | Tự động dò | Worksheet chứa bảng. Nếu bỏ qua, sẽ tìm kiếm trên mọi sheet. |
| `workbook` | string | Không | Active Workbook | Tên hoặc đường dẫn workbook. |
| `maxRows` | integer | Không | `1000` | Số lượng dòng dữ liệu tối đa trả về. |

- **Ví dụ gọi:**
```json
{
  "tableName": "RebarSchedule",
  "maxRows": 500
}
```

---

#### `write_range`
- **Mục đích:** Ghi hàng loạt mảng giá trị 2D hoặc công thức vào vùng ô bắt đầu từ `startCell`. Tự động tính toán kích thước vùng đích bằng `Range.Resize`. **Tự động sao lưu snapshot trước khi ghi.**
- **Phân cấp:** Tier W (Write) | `transaction: "auto"` | Timeout: 60s.
- **Tham số:**

| Tên tham số | Kiểu | Bắt buộc | Mặc định | Mô tả |
|---|---|---|---|---|
| `startCell` | string | **Có** | - | Ô bắt đầu ghi (ví dụ: `'A1'`, `'B5'`). |
| `values` | array | **Có** | - | Mảng 2D các hàng và cột cần ghi: `[[r1c1, r1c2], [r2c1, r2c2]]`. |
| `sheet` | string | Không | Active Sheet | Sheet đích. |
| `workbook` | string | Không | Active Workbook | Tên hoặc đường dẫn workbook. |
| `autoFitColumns` | boolean | Không | `false` | Tự động căn chỉnh độ rộng cột sau khi ghi. |
| `isFormula` | boolean | Không | `false` | Nếu `true`, các chuỗi bắt đầu bằng dấu `'='` được gán thành công thức tính toán. |

- **Ví dụ gọi:**
```json
{
  "startCell": "B2",
  "values": [
    ["Item", "Quantity", "Unit Price", "Total"],
    ["D20 Rebar", 120, 15.5, "=B3*C3"],
    ["D25 Rebar", 80, 22.0, "=B4*C4"]
  ],
  "isFormula": true,
  "autoFitColumns": true
}
```

---

#### `create_table`
- **Mục đích:** Chuyển đổi một vùng ô thành bảng Excel có cấu trúc (`ListObject`), áp dụng kiểu bảng (style) và tùy chọn hiển thị hàng tổng cộng (`TotalsRow`). **Tự động tạo snapshot.**
- **Phân cấp:** Tier W (Write) | `transaction: "auto"` | Timeout: 60s.
- **Tham số:**

| Tên tham số | Kiểu | Bắt buộc | Mặc định | Mô tả |
|---|---|---|---|---|
| `range` | string | **Có** | - | Vùng ô chuyển đổi thành bảng (ví dụ: `'A1:E50'`). |
| `tableName` | string | **Có** | - | Tên định danh duy nhất của bảng (ví dụ: `'MaterialTakeoff'`). |
| `sheet` | string | Không | Active Sheet | Sheet chứa bảng. |
| `workbook` | string | Không | Active Workbook | Tên hoặc đường dẫn workbook. |
| `hasHeaders` | boolean | Không | `true` | Hàng đầu tiên có chứa tiêu đề cột hay không. |
| `tableStyle` | string | Không | `"TableStyleMedium2"` | Tên kiểu bảng Excel (ví dụ: `TableStyleLight1`, `TableStyleMedium9`, `TableStyleDark1`). |
| `showTotalsRow` | boolean | Không | `false` | Bật/tắt hàng tính tổng ở chân bảng. |

- **Ví dụ gọi:**
```json
{
  "range": "A1:D10",
  "tableName": "CostEstimation",
  "tableStyle": "TableStyleMedium9",
  "showTotalsRow": true
}
```

---

### 2. Workbook Tools (2 công cụ)

#### `read_worksheet_info`
- **Mục đích:** Liệt kê thông tin chi tiết các sheet trong workbook: tên sheet, số thứ tự (index), trạng thái hiển thị (Visible, Hidden, VeryHidden), kích thước used range, danh sách bảng biểu (`ListObjects`) và biểu đồ (`ChartObjects`).
- **Phân cấp:** Tier R (ReadOnly) | `transaction: "none"` | Timeout: 30s.
- **Tham số:**

| Tên tham số | Kiểu | Bắt buộc | Mặc định | Mô tả |
|---|---|---|---|---|
| `sheet` | string | Không | Tất cả | Tên sheet cụ thể cần kiểm tra. Nếu bỏ trống, duyệt tất cả các sheet. |
| `workbook` | string | Không | Active Workbook | Tên hoặc đường dẫn workbook. |

- **Ví dụ gọi:**
```json
{
  "sheet": "Summary"
}
```

---

#### `manage_worksheet`
- **Mục đích:** Quản lý các sheet: tạo mới (`add`), đổi tên (`rename`), nhân bản (`duplicate`), ẩn (`hide`), hiện (`unhide`), hoặc xoá (`delete`). **Tác vụ `delete` thuộc Tier D (Destructive) yêu cầu bật checkbox Destructive.** Tự động snapshot trước khi thao tác.
- **Phân cấp:** Tier W (cho add/rename/duplicate/hide/unhide) hoặc Tier D (cho delete) | `transaction: "auto"` | Timeout: 60s.
- **Tham số:**

| Tên tham số | Kiểu | Bắt buộc | Mặc định | Mô tả |
|---|---|---|---|---|
| `action` | string | **Có** | - | Tác vụ: `"add"`, `"rename"`, `"duplicate"`, `"delete"`, `"hide"`, `"unhide"`. |
| `sheet` | string | **Có** | - | Sheet mục tiêu thực hiện tác vụ. |
| `newSheetName` | string | Không | - | Tên sheet mới (bắt buộc khi `action="rename"` và tùy chọn khi `add`/`duplicate`). |
| `workbook` | string | Không | Active Workbook | Tên hoặc đường dẫn workbook. |
| `visibility` | string | Không | `"visible"` | Mức độ hiển thị khi ẩn/hiện: `"visible"`, `"hidden"`, `"very_hidden"`. |

- **Ví dụ gọi:**
```json
{
  "action": "rename",
  "sheet": "Sheet1",
  "newSheetName": "RebarData"
}
```

---

### 3. Format Tools (1 công cụ)

#### `format_range`
- **Mục đích:** Định dạng toàn diện vùng ô: định dạng số (`NumberFormat`), phông chữ (in đậm, in nghiêng, cỡ chữ, màu sắc Hex), màu nền ô (Hex fill color), kiểu viền ô (`border`), và căn lề (`horizontalAlignment`). **Tự động snapshot trước khi áp dụng.**
- **Phân cấp:** Tier W (Write) | `transaction: "auto"` | Timeout: 60s.
- **Tham số:**

| Tên tham số | Kiểu | Bắt buộc | Mặc định | Mô tả |
|---|---|---|---|---|
| `range` | string | **Có** | - | Vùng ô định dạng (ví dụ: `'A1:E1'`, `'B2:B20'`). |
| `sheet` | string | Không | Active Sheet | Worksheet mục tiêu. |
| `workbook` | string | Không | Active Workbook | Tên hoặc đường dẫn workbook. |
| `numberFormat` | string | Không | - | Định dạng số (ví dụ: `"$#,##0.00"`, `"#,##0"`, `"0.0%"`, `"YYYY-MM-DD"`). |
| `bold` | boolean | Không | - | In đậm chữ (`true`/`false`). |
| `italic` | boolean | Không | - | In nghiêng chữ (`true`/`false`). |
| `fontSize` | number | Không | - | Kích thước phông chữ (pt). |
| `fontColor` | string | Không | - | Mã màu Hex cho chữ (ví dụ: `"#FFFFFF"`, `"#1A1A1A"`). |
| `backgroundColor` | string | Không | - | Mã màu Hex tô nền (ví dụ: `"#003366"`, `"#E2EFDA"`). |
| `horizontalAlignment` | string | Không | - | Căn lề: `"left"`, `"center"`, `"right"`, `"justify"`. |
| `wrapText` | boolean | Không | - | Tự động xuống dòng khi chữ dài. |
| `border` | string | Không | - | Kiểu viền ô: `"all_thin"`, `"outline_thick"`, `"bottom_double"`, `"none"`. |

- **Ví dụ gọi:**
```json
{
  "range": "A1:G1",
  "bold": true,
  "fontSize": 12,
  "fontColor": "#FFFFFF",
  "backgroundColor": "#0696D7",
  "horizontalAlignment": "center",
  "border": "all_thin"
}
```

---

### 4. Chart Tools (1 công cụ)

#### `create_chart`
- **Mục đích:** Tạo biểu đồ nhúng liên kết trực tiếp với vùng dữ liệu (`dataRange`): Cột, Đường, Tròn, Thanh ngang, Miền, Phân tán. Tuỳ biến tiêu đề, kích thước và vị trí đặt biểu đồ. **Tự động snapshot.**
- **Phân cấp:** Tier W (Write) | `transaction: "auto"` | Timeout: 60s.
- **Tham số:**

| Tên tham số | Kiểu | Bắt buộc | Mặc định | Mô tả |
|---|---|---|---|---|
| `dataRange` | string | **Có** | - | Vùng ô chứa dữ liệu vẽ biểu đồ (ví dụ: `'A1:D10'`). |
| `chartType` | string | **Có** | - | Loại biểu đồ: `"ColumnClustered"`, `"ColumnStacked"`, `"Line"`, `"LineMarkers"`, `"Pie"`, `"BarClustered"`, `"Area"`, `"Scatter"`. |
| `title` | string | Không | - | Tiêu đề của biểu đồ. |
| `sheet` | string | Không | Active Sheet | Sheet chứa biểu đồ. |
| `workbook` | string | Không | Active Workbook | Tên hoặc đường dẫn workbook. |
| `targetCell` | string | Không | `"E2"` | Ô góc trên bên trái nơi đặt biểu đồ. |
| `width` | number | Không | `400` | Chiều rộng biểu đồ (points). |
| `height` | number | Không | `250` | Chiều cao biểu đồ (points). |
| `hasLegend` | boolean | Không | `true` | Bật/tắt chú giải biểu đồ. |

- **Ví dụ gọi:**
```json
{
  "dataRange": "A1:B6",
  "chartType": "ColumnClustered",
  "title": "Rebar Weight Distribution",
  "targetCell": "D2",
  "width": 500,
  "height": 300
}
```

---

### 5. Calculation Tools (1 công cụ)

#### `evaluate_formula`
- **Mục đích:** Tính toán động một công thức Excel trong ngữ cảnh của worksheet/workbook đang mở mà không cần ghi vào ô nào. Trả về kết quả và kiểu dữ liệu.
- **Phân cấp:** Tier R (ReadOnly) | `transaction: "none"` | Timeout: 30s.
- **Tham số:**

| Tên tham số | Kiểu | Bắt buộc | Mặc định | Mô tả |
|---|---|---|---|---|
| `formula` | string | **Có** | - | Chuỗi công thức Excel (ví dụ: `'SUM(A1:A10)'`, `'VLOOKUP("D20", B2:F50, 3, FALSE)'`). Dấu `'='` ở đầu là tuỳ chọn. |
| `sheet` | string | Không | Active Sheet | Sheet ngữ cảnh để tính toán. |
| `workbook` | string | Không | Active Workbook | Tên hoặc đường dẫn workbook. |

- **Ví dụ gọi:**
```json
{
  "formula": "SUMIFS(C2:C100, A2:A100, \"Column\", B2:B100, \">20\")"
}
```

---

### 6. Export Tools (1 công cụ)

#### `export_worksheet`
- **Mục đích:** Xuất bản worksheet hoặc toàn bộ workbook ra định dạng PDF hoặc CSV trên đĩa cứng. Hỗ trợ xoay ngang trang (`landscape`) và co giãn vừa trang in (`fitToPage`). **Tự động snapshot.**
- **Phân cấp:** Tier W (Write) | `transaction: "auto"` | Timeout: 60s.
- **Tham số:**

| Tên tham số | Kiểu | Bắt buộc | Mặc định | Mô tả |
|---|---|---|---|---|
| `format` | string | **Có** | - | Định dạng xuất: `"pdf"` hoặc `"csv"`. |
| `outputPath` | string | Không | Cạnh file gốc | Đường dẫn tệp đích. Nếu bỏ qua, xuất ngay cạnh file workbook. |
| `sheet` | string | Không | Active Sheet | Worksheet cần xuất. Bắt buộc với CSV; nếu bỏ qua với PDF sẽ xuất toàn bộ workbook. |
| `workbook` | string | Không | Active Workbook | Tên hoặc đường dẫn workbook. |
| `landscape` | boolean | Không | `false` | Khổ giấy nằm ngang (chỉ áp dụng cho PDF). |
| `fitToPage` | boolean | Không | `true` | Co giãn nội dung vừa 1 trang in chiều ngang (PDF). |

- **Ví dụ gọi:**
```json
{
  "format": "pdf",
  "sheet": "BaoCaoKhoiLuong",
  "landscape": true,
  "fitToPage": true
}
```

---

### 7. Automation Tools (1 công cụ)

#### `run_macro`
- **Mục đích:** **DESTRUCTIVE:** Thực thi một Sub procedure hoặc Function VBA đã có trong workbook với danh sách tham số tuỳ chọn. **Bắt buộc phải bật checkbox "Allow destructive operations" trên giao diện Bridge.** Tự động snapshot trước khi chạy.
- **Phân cấp:** Tier D (Destructive) | `transaction: "auto"` | Timeout: 120s.
- **Tham số:**

| Tên tham số | Kiểu | Bắt buộc | Mặc định | Mô tả |
|---|---|---|---|---|
| `macroName` | string | **Có** | - | Tên thủ tục macro VBA cần chạy (ví dụ: `'CalculateAll'`, `'Module1.ExportData'`). |
| `args` | array[string] | Không | `[]` | Mảng các tham số truyền vào cho macro. |
| `workbook` | string | Không | Active Workbook | Tên hoặc đường dẫn workbook chứa macro. |

- **Ví dụ gọi:**
```json
{
  "macroName": "UpdateCalculations",
  "args": ["All", "Strict"]
}
```

---

## Các Công Cụ Cốt Lõi (Core Tools)

### 1. `get_excel_context`
Trả về ảnh chụp nhanh (snapshot) thông tin phiên làm việc Microsoft Excel hiện tại:
- `isAttached` (boolean): Đã kết nối với phiên Excel nào chưa.
- `attachedPid` (integer): Process ID của instance `EXCEL.EXE`.
- `excelVersion` (string): Phiên bản Excel (ví dụ: `16.0` cho Excel 2016/2019/2021/365).
- `docTitle` (string): Tên tệp workbook đang mở (`.xlsx`, `.xlsm`).
- `docPath` (string): Đường dẫn tuyệt đối của workbook trên ổ đĩa.
- `activeSheetName` (string): Tên worksheet đang được kích hoạt.
- `selectedRange` (string): Địa chỉ vùng ô hiện đang được bôi đen (selection).
- `openWorkbookCount` (int), `worksheetCount` (int).
- `executionEnabled`, `writeEnabled`, `destructiveEnabled` (boolean): Trạng thái 3 chốt kiểm soát an toàn trên giao diện Bridge.

### 2. `execute_excel_code`
Cung cấp môi trường thực thi kịch bản C# Roslyn trực tiếp trong không gian tiến trình của Excel hoặc ClosedXML.
- **Globals khả dụng trong mã script:**
  - `excel` (`Microsoft.Office.Interop.Excel.Application`): Đối tượng ứng dụng Excel cấp cao nhất (COM mode).
  - `workbook` (`Microsoft.Office.Interop.Excel.Workbook`): Workbook đang kích hoạt.
  - `sheet` (`Microsoft.Office.Interop.Excel.Worksheet`): Worksheet đang kích hoạt.
  - `closedXml` (`ClosedXmlWorkbookService`): Dịch vụ thao tác file `.xlsx` chế độ ngầm headless.
  - `ct` (`CancellationToken`): Token huỷ tác vụ.
  - `log(string)`: Ghi thông điệp ra audit log stream của Bridge.
  - `progress(int current, int total, string message)`: Báo tiến độ công việc về MCP client.
  - `args` (`ScriptArgs`): Nhận các tham số truyền vào từ client (`args.Str("key")`, `args.Int("key")`, `args.Double("key")`, `args.Bool("key")`).
- **Imports mặc định:** `System`, `System.Linq`, `System.Collections.Generic`, `Microsoft.Office.Interop.Excel`, `ClosedXML.Excel`, `HPRebar.McpBridge.Core.Scripting`.
- **Cơ chế an toàn:**
  - Script được phân tích cú pháp AST tĩnh: chặn `Process.Start`, `System.Net`, `System.IO` bất hợp pháp, reflection, `unsafe`, `dynamic`, `Application.Quit`.
  - Tự động nhận diện thành phần truy cập để xếp hạng Tier R, W hoặc D.
  - `dryRun=true` trả về kết quả `PREVIEW` mà không thực thi bất kỳ thay đổi nào.

---

## Kiến Trúc An Toàn 3 Tầng & Snapshot Tự Động

### 1. Phân Tầng An Toàn (3-Tier Safety Classification)

| Tier | Tên | Các công cụ tiêu biểu | Quyền Bridge yêu cầu | Cơ chế bảo vệ |
|---|---|---|---|---|
| **Tier R** | ReadOnly | `read_range`, `find_cells`, `read_table`, `read_worksheet_info`, `evaluate_formula`, `get_excel_context` | Allow AI execution | Không thay đổi dữ liệu; không cần sao lưu. |
| **Tier W** | Write / Mutating | `write_range`, `format_range`, `create_table`, `create_chart`, `export_worksheet`, sheet add/rename | Allow AI execution + Allow write operations | **Tự động snapshot** `.xlsx` trước khi ghi; hỗ trợ `dryRun` xem trước. |
| **Tier D** | Destructive | Sheet deletion (`manage_worksheet` delete), Clear contents/formats, `run_macro` | Allow AI execution + Allow write operations + **Allow destructive operations** | **Tự động snapshot**; yêu cầu xác nhận của con người qua checkbox; timeout tối đa 600s. |

### 2. Logic Cổng UI Cascading (Cascade UI Gating)
Trên cửa sổ `HPExcel.McpBridge`:
- Nếu tắt **"Allow AI execution"** → Cả 3 tầng R, W, D đều bị chặn (mã lỗi `-32001`).
- Nếu bật Execution nhưng tắt **"Allow write operations"** → Chỉ cho phép đọc Tier R; mọi lệnh Tier W và D bị chặn với thông báo yêu cầu bật chốt ghi.
- Nếu bật Write nhưng tắt **"Allow destructive operations"** → Cho phép Tier R và W; các thao tác xoá sheet hoặc macro Tier D bị chặn với mã lỗi `-32001`.

### 3. Động Cơ Sao Lưu Snapshot Tự Động (`ExcelSnapshotManager`)
- **Đường dẫn lưu trữ:**
  - Tệp đã lưu trên đĩa: Tự động lưu vào thư mục con `.hpexcel_snapshots/` nằm **ngay cạnh tệp Excel gốc**.
  - Tệp mới chưa lưu (Book1) hoặc thư mục mạng UNC: Lưu an toàn vào `%TEMP%\.hpexcel_snapshots\`.
- **Quy tắc đặt tên tệp snapshot:**
  `yyyyMMdd-HHmmss_<SanitizedWorkbookName>_<SanitizedLabel>.xlsx`
  *(Ví dụ: `20260921-183000_DuToanCongTrinh_write_range.xlsx`)*
- **Chính sách dọn dẹp (Retention & Pruning):**
  Hệ thống tự động lưu giữ **20 bản snapshot mới nhất** và xoá các bản cũ hơn để tiết kiệm dung lượng đĩa.
- **Khôi phục khi xảy ra sự cố:**
  Tên tệp snapshot luôn được trả về trong trường `Snapshot` của kết quả MCP. Người dùng chỉ cần mở bản snapshot tương ứng để phục hồi nguyên trạng bảng tính trước lúc AI can thiệp.

---

## Hướng Dẫn: Headless ClosedXML vs Live COM Interop

| Tiêu chí | Chế độ Live COM Interop | Chế độ Headless ClosedXML |
|---|---|---|
| **Cơ chế** | Tự động hoá qua `Excel.Application` COM | Thao tác tệp nhị phân OpenXML (`XLWorkbook`) |
| **Yêu cầu cài đặt** | Bắt buộc phải có Microsoft Excel đang chạy | Hoàn toàn không cần cài đặt Microsoft Excel |
| **Giao diện trực quan** | Người dùng nhìn thấy màn hình Excel cập nhật trực tiếp | Chạy ngầm hoàn toàn, ghi thẳng xuống tệp `.xlsx` |
| **Tốc độ xử lý** | Phù hợp tương tác vừa và nhỏ (~1.000 dòng) | Siêu nhanh cho hàng chục ngàn dòng dữ liệu |
| **Tính năng hỗ trợ** | Biểu đồ, VBA Macro, Selection, Dynamic Formatting | Bảng biểu, công thức, định dạng, cell data |
| **Cách sử dụng** | Bỏ trống `workbook` hoặc dùng workbook đang mở | Truyền đường dẫn tệp `.xlsx` trong tham số hoặc qua `closedXml` |

---

## Xử Lý Sự Cố & Bảng Mã Lỗi (Troubleshooting)

| Mã lỗi / Hiện tượng | Nguyên nhân | Biện pháp xử lý |
|---|---|---|
| `-32001` (ExecutionDisabled) | Checkbox "Allow AI execution" trong Bridge đang tắt. | Nhắc người dùng tick bật checkbox "Allow AI execution" trên cửa sổ HPExcel MCP Bridge. |
| `-32001` (WriteDisabled) | Thao tác ghi bị chặn vì "Allow write operations" đang tắt. | Nhắc người dùng tick bật "Allow write operations" trên giao diện Bridge. |
| `-32001` (DestructiveDisabled) | Thao tác xoá sheet hoặc chạy macro bị chặn do "Allow destructive operations" đang tắt. | Giải thích rõ thao tác phá hủy với người dùng và nhờ họ tick bật checkbox tương ứng. |
| `-32002` (Busy/Timeout) | Excel đang bận xử lý dữ liệu lớn hoặc người dùng đang chỉnh sửa ô (Cell Edit Mode / modal dialog). | Nhấn phím `Enter` hoặc `Esc` trong Excel để thoát chế độ sửa ô; kiểm tra tắt các hộp thoại modal đang mở. |
| `-32003` (NoDocument / NotAttached) | Chưa attach vào tiến trình Excel hoặc workbook chưa được mở. | Khởi động Excel, mở workbook cần làm việc, và bấm "Attach" trên cửa sổ Bridge. |
| Diagnostic `GUARD` | Script chứa namespace hoặc lời gọi bị cấm (`Process.Start`, `System.Net`, `Application.Quit`). | Viết lại script tuân thủ quy định bảo mật, sử dụng các API trong danh mục cho phép. |
| Diagnostic `PREVIEW` | Script ghi được chạy với `dryRun=true` hoặc `transaction="none"`. | Hoạt động bình thường theo thiết kế. Sau khi người dùng xác nhận bản xem trước, đặt `dryRun=false` để thực thi. |
| `BridgeNotConnected` | Ứng dụng `HPExcel.McpBridge.exe` chưa được khởi động. | Khởi chạy `HPExcel.McpBridge.exe` từ thư mục output hoặc bin để mở Named Pipe. |
