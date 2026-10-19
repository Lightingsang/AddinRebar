---
name: hp-mcp-grasshopper
description: "Kết nối và điều khiển Grasshopper trong Revit 2026 qua HPGrasshopper MCP (server hprebar-grasshopper, tool mcp__hprebar-grasshopper__*): đọc cấu trúc canvas đồ thị hiện hành (.gh / .ghx, components, wires, status, errors), giải và thực thi định nghĩa Grasshopper Player với tham số đầu vào inputs, tạo/sửa/xoá component và dây nối (batch_edit_grasshopper_canvas với CanvasUndoScope), xuất ảnh canvas đồ thị, thực thi trực tiếp C# Roslyn scripting trên Revit Document và RhinoDoc/GH_Document với cơ chế an toàn 2 tầng (Revit TransactionGroup rollback cho dryRun=true + CanvasUndoScope), 12 embedded seeds chuyên biệt Grasshopper ↔ Revit (Levels/Grids, cột/dầm parametric, curve-to-rebar với diameterToleranceMm, đai stirrups, sàn floors, curtain walls, facade panels, solar radiation, directshape_from_geometry cuboid bounding volume, query_revit_geometry scalar summaries...), 8 registry meta tools (search_tools, get_tool, run_tool, get_run, propose_tool, test_tool, publish_tool, manage_tool), cancel_execution và context tool get_grasshopper_context (tổng 27 tools). TRIGGER when: user nhắc 'Grasshopper', 'GH', 'ghx', 'Rhino.Inside', 'Rhino.Inside.Revit', 'Rhino 8', 'canvas grasshopper', 'component grasshopper', 'nối dây grasshopper', 'batch edit grasshopper', 'chạy file gh', 'hprebar-grasshopper', 'hpgrasshopper-mcp-2026', hoặc lỗi -32001/-32002/GUARD từ tool Grasshopper. Keywords: grasshopper, gh, rhino, rhinoinside, revit, mcp, bridge, canvas, component, wire, player, batch_edit, dryrun. Khi cần đọc/sửa/chạy đồ thị Grasshopper hoặc viết script Grasshopper/Revit qua MCP."
metadata:
  author: hoang
  version: "1.1.0"
  mcp-server: hprebar-grasshopper
---

<!-- portable-host-contract:start -->
## Portable host contract

This skill is shared by Codex and Google Antigravity.

- Use the host's native task tracker. In Codex, use `update_plan`; in Antigravity, maintain the task-list artifact.
- Use native collaboration and user-input tools exposed by the host. Treat legacy tool names as capability descriptions, never literal calls.
- Resolve bundled resources from the project-local `.agents/skills/` tree. Do not create or write a user-global skill directory.
<!-- portable-host-contract:end -->

# HPGrasshopper MCP — Điều khiển Grasshopper Rhino.Inside Revit qua `hprebar-grasshopper`

## 1. Tổng quan Kiến trúc (Overview)

Hệ thống **HPGrasshopper MCP** cho phép AI coding agent tương tác trực tiếp với **Grasshopper (Rhino 8)** chạy bên trong tiến trình **Autodesk Revit 2026** thông qua **Rhino.Inside.Revit**:
- **Chuỗi liên lạc:** AI Agent ──stdio──▶ `HPGrasshopper.Mcp.Server.exe` (.NET 10.0) ──Named Pipe `\\.\pipe\hpgrasshopper-mcp-2026`──▶ `HPGrasshopper.McpBridge.dll` (trong `Revit.exe`).
- **In-Process Dual-Lifecycle Bridge:**
  1. *Revit Add-in (`IExternalApplication`)*: Lắng nghe Named Pipe ngay khi mở Revit, quản lý luồng Main/UI Thread qua Revit `ExternalEvent` và nạp động assembly Rhino 8 / Grasshopper an toàn.
  2. *Rhino.Inside Integration*: Cho phép đồng bộ dữ liệu hình học, tham số tính toán và sinh đối tượng Revit hai chiều (Revit ↔ Grasshopper) mà không cần xuất tệp trung gian.
- **An toàn 2 tầng (2-Tier Atomic Rollback):**
  - **Tầng 1 (Revit Host):** Mọi thay đổi trong Revit Document được bọc trong Revit `TransactionGroup("MCP Grasshopper: <label>")`. Khi đặt `dryRun: true` hoặc khi phát sinh ngoại lệ, toàn bộ element sinh ra sẽ được **rollback sạch 100%**.
  - **Tầng 2 (Grasshopper Canvas):** Mọi thay đổi đồ thị được bọc trong `CanvasUndoScope` với sổ cái đối tượng (`_addedGuids`, `_removedSnapshots`, `_connectedWires`, `_disconnectedWires`). Khi `dryRun: true` hoặc khi mutation thất bại, canvas được khôi phục nguyên tử về trạng thái ban đầu mà không làm sống lại các đối tượng tạm (temporary objects). Canvas undo scope chỉ commit sau khi Revit transaction đạt kết quả cuối cùng (`final outcome`).
- **Xác thực Danh tính Tài liệu (Document Identity Validation):**
  - Ở tầng Bridge RPC (hoặc truyền qua từ điển `args` của `execute_grasshopper_code`), hệ thống hỗ trợ kiểm tra đối chiếu `targetDocTitle`, `targetDocPath` (tài liệu Revit) và `targetDefId` (GUID của định nghĩa Grasshopper).
  - Nếu tài liệu active không khớp với target được chỉ định, bridge lập tức từ chối thực thi (`fail-closed`) để ngăn chặn việc ghi nhầm vào tài liệu khác.
- **Chuẩn Ánh xạ Lỗi (Error Mapping Standard):**
  - Khi một tool native gặp lỗi (tệp không tồn tại, preflight thất bại, solver error, missing outputs, document mismatch), MCP server luôn ánh xạ về `CallToolResult.IsError = true` (`isError: true`), đồng thời bảo toàn trọn vẹn thông tin chẩn đoán JSON trong Content blocks thay vì nuốt lỗi.
- **Bảo vệ Opt-In:** Cửa sổ trạng thái Bridge có nút gạt **"Allow AI execution in this Revit session"** (mặc định tắt mỗi khi bật Revit để bảo vệ an toàn). Khi tắt, mọi thao tác ghi sẽ bị từ chối ngay lập tức với mã lỗi `-32001`.

---

## 2. Bước 0 — Kết nối & Thiết lập (Checklist theo thứ tự)

1. **Khởi động Revit 2026** và mở một dự án (.rvt). Đảm bảo tab **Rhino.Inside** đã được nạp.
2. **Bật quyền thực thi AI (Opt-in):**
   - Trên thanh Ribbon Revit: bấm tab **HPGrasshopper ▸ MCP ▸ Status View**.
   - Tick chọn ô **"Allow AI execution in this Revit session"** (ô này tự tắt mỗi lần mở lại Revit để bảo vệ an toàn).
3. **Cấu hình `.mcp.json`:** Khai báo server `hprebar-grasshopper` trỏ đến file exe đã build:
   ```json
   {
     "mcpServers": {
       "hprebar-grasshopper": {
         "command": "G:\\09-PROJECT AI\\01_Revit\\02_CshapRevit\\01_AddinRebar\\HPGrasshopper\\HPGrasshopper.Mcp.Server\\bin\\Release\\net10.0\\HPGrasshopper.Mcp.Server.exe"
       }
     }
   }
   ```
4. **Kiểm tra sức khỏe kết nối (Diagnostic script):**
   ```powershell
   pwsh .agents/skills/hp-mcp-grasshopper/scripts/check-grasshopper-mcp.ps1
   ```
5. **Gọi `get_grasshopper_context` đầu tiên:** Kiểm tra trạng thái:
   - `executionEnabled`: Phải là `true` (nếu `false` -> nhắc người dùng tick checkbox).
   - `isGrasshopperLoaded`: Cho biết Grasshopper / Rhino.Inside đã nạp thành công hay chưa.
   - `activeDocument`: Tên file `.gh` / `.ghx` đang mở trên canvas (nếu có).

---

## 3. Khảo sát & Tra cứu Nhanh (Discovery)

- **Đọc toàn bộ canvas hiện hành:** Gọi `read_grasshopper_canvas` để lấy danh sách components, GUID, toạ độ, dây kết nối wires và solver status.
- **Kiểm tra lỗi và cảnh báo tính toán:** `read_grasshopper_canvas` trả về danh sách runtime warnings và runtime errors kèm GUID của component bị lỗi.

---

## 4. Danh mục 27 Công cụ Chi tiết (Tool Catalog)

Hệ thống HPGrasshopper cung cấp tổng cộng **27 công cụ** phân thành 4 nhóm rõ ràng:

### 4.1. Core Native & Execution Tools (6 công cụ điều khiển lõi & huỷ thực thi)
| Tool | Mục đích | Tham số chính |
|---|---|---|
| `read_grasshopper_canvas` | Trích xuất toàn bộ cấu trúc node, wires, parameters, solver errors/warnings | `includeValues` (bool, default false), `selectedOnly` (bool, default false), `categoryFilter` (string?) |
| `batch_edit_grasshopper_canvas` | Thêm, xoá, nối dây và cấu hình components theo batch với atomic rollback và kiểm tra preflight | `componentsToAdd` (array: `{type, x, y, nickName, tempId}`), `componentsToRemove` (string[]: instance Guids), `wiresToConnect` (array: `{source, sourceOutput, target, targetInput}`), `wiresToDisconnect` (array: `{source, sourceOutput, target, targetInput}`), `dryRun` (bool, default false) |
| `run_grasshopper_player` | Mở & giải file định nghĩa `.gh`/`.ghx` với tham số đầu vào inputs (GH Player style), bắt lỗi solver thực chất | `filePath` (string, required), `inputs` (object?), `outputs` (string[]?), `headless` (bool, default true) |
| `export_grasshopper_canvas_image` | Xuất ảnh chụp canvas Grasshopper chất lượng cao (PNG/base64) | `mode` ("FullCanvas"/"Viewport"/"Selection", default "FullCanvas"), `zoom` (number: 0.1–5.0, default 1.0), `outputPath` (string?), `returnBase64` (bool, default true) |
| `execute_grasshopper_code` | Chạy C# Roslyn script đa môi trường (Revit API + RhinoCommon + GH API) với 2-tier safety | `code` (string, required), `transaction` ("auto"/"manual"/"none", default "auto"), `dryRun` (bool, default false), `timeoutSeconds` (int: 5–600, default 60), `label` (string?), `args` (object?) |
| `cancel_execution` | Huỷ bỏ script, tool hoặc player run đang chạy, hoặc xóa cờ busy nếu bị kẹt | *(không có tham số)* |

> **Lưu ý về Document Identity (`targetDocTitle`, `targetDocPath`, `targetDefId`):**
> Các tham số danh tính tài liệu được kiểm tra ở tầng Bridge RPC để bảo đảm cơ chế `fail-closed` chống ghi nhầm tài liệu. Khi sử dụng qua MCP, caller có thể truyền các giá trị này vào từ điển `args` của `execute_grasshopper_code` (ví dụ `args: { "targetDocTitle": "...", "targetDefId": "..." }`) hoặc truyền trực tiếp trong payload JSON-RPC gửi đến Bridge.

### 4.2. Context Tool (1 công cụ trạng thái môi trường)
| Tool | Mục đích | Tham số chính |
|---|---|---|
| `get_grasshopper_context` | Đọc trạng thái bridge, phiên bản Rhino/GH/Revit, opt-in status, tài liệu RVT và định nghĩa GH đang mở | `includeSelection` (bool, default false) |

### 4.3. Embedded Seed Tools (12 công cụ chuyên biệt Grasshopper ↔ Revit)
| Nhóm | Tool | Transaction | Mô tả & Chức năng | Tham số chính (khớp verbatim manifest `tool.json`) |
|---|---|---|---|---|
| **Revit Model Sync** | `create_levels_grids` | auto | Tạo hệ lưới trục và cao độ tầng tự động trong Revit từ mảng cao độ và khoảng cách trục | `elevations` (number[], required), `gridSpacings` (number[], required), `gridLength` (number, default 30.0), `dryRun` (bool, default false) |
| | `generate_columns_beams` | auto | Sinh dầm và cột kết cấu Revit theo toạ độ điểm cột và đường tim dầm parametric | `columnPoints` (object[], required), `beamLines` (object[], required), `columnHeight` (number, default 3.6), `dryRun` (bool, default false) |
| | `generate_rebar_curves` | auto | Chuyển đổi mạng lưới đường curve 3D thành cốt thép Rebar thực tế trong Revit với dung sai đường kính | `hostElementId` (integer, required), `rebarCurves` (array[], required), `barDiameterMm` (number, default 16.0), `diameterToleranceMm` (number, default 2.0), `dryRun` (bool, default false) |
| | `generate_stirrup_distribution` | none | Tự động tính toán và phân bổ cốt đai gia cường theo khoảng cách thay đổi giữa vùng gối và giữa nhịp | `beamElementId` (integer, required), `denseSpacingMm` (number, default 100.0), `sparseSpacingMm` (number, default 200.0), `supportZoneRatio` (number, default 0.25), `stirrupDiameterMm` (number, default 8.0), `dryRun` (bool, default false) |
| | `create_floors_slabs` | auto | Tạo sàn bê tông Revit từ các đường biên khép kín (boundary loops) kèm cờ kết cấu `structural` | `boundaryLoops` (array[], required), `elevation` (number, default 0.0), `structural` (boolean, default true), `dryRun` (bool, default false) |
| | `create_wall_openings` | auto | Đục lỗ mở hình chữ nhật trên tường Revit từ danh sách toạ độ và kích thước | `wallElementId` (integer, required), `openings` (object[], required), `dryRun` (bool, default false) |
| **Geometry & Facade** | `curtain_grid_layout` | none | Lập kế hoạch phân vị hệ khung lưới đố kính Curtain Wall theo tham số khoảng cách trục U/V | `curtainWallElementId` (integer, required), `uSpacingMeters` (number, default 1.5), `vSpacingMeters` (number, default 3.0), `layoutRule` (string, default "FixedDistance"), `dryRun` (bool, default false) |
| | `directshape_from_geometry` | auto | Tạo phần tử DirectShape dạng khối bounding volume (Solid extrusion hoặc Mesh box) từ kích thước hộp bao | `shapeType` ("Solid"/"Mesh", default "Solid"), `category` (string, default "GenericModel"), `boundingDimensions` ({width, length, height}, required), `name` (string, default "Grasshopper_DirectShape"), `dryRun` (bool, default false) |
| | `evaluate_solar_radiation` | none | Đánh giá bức xạ mặt trời trên bề mặt panel vỏ bao che theo vector hướng nắng và pháp tuyến bề mặt | `sunVector` (object, required), `targetSurfaceNormals` (object[], required), `directRadiationWatts` (number, default 1000.0) |
| | `facade_panelization` | none | Phân chia lưới mặt đứng facade envelope thành hệ panel adaptive theo số lượng phân vị U/V | `surfaceWidthMeters` (number, default 30.0), `surfaceHeightMeters` (number, default 15.0), `uDivisions` (integer, required), `vDivisions` (integer, required), `panelFamilyName` (string, default "Adaptive-Quad-Panel"), `dryRun` (bool, default false) |
| | `query_revit_geometry` | none | Truy vấn trích xuất tóm tắt hình học phần tử Revit (solid, thể tích m³, diện tích m², bbox) không sửa mô hình | `elementIds` (integer[], required), `detailLevel` ("Coarse"/"Medium"/"Fine", default "Fine"), `includeFaces` (boolean, default true) |
| **Model Exchange** | `export_model_to_rhino` | none | Lập kế hoạch cấu trúc phân lớp hình học xuất từ Revit sang Rhino theo danh mục category và tầng | `categories` (string[], required), `targetRhinoUnits` (string, default "Meters"), `groupByLevel` (boolean, default true) |

### 4.4. Registry Meta Tools (8 công cụ quản lý thư viện mở rộng)
| Tool | Mục đích | Tham số chính |
|---|---|---|
| `search_tools` | Tìm kiếm công cụ trong registry theo từ khoá, category hoặc tag (để trống query để liệt kê tất cả) | `query` (string?), `category` (string?), `limit` (int, default 5), `includeUnpublished` (bool, default false) |
| `get_tool` | Xem chi tiết metadata, JSON schema và mã nguồn C# của tool trong registry | `name` (string, required), `includeCode` (bool, default true), `recentRuns` (int, default 10) |
| `run_tool` | Thực thi một tool đã phát hành trong registry với bộ tham số đầu vào | `name` (string, required), `args` (object?), `dryRun` (bool, default false), `allowUnpublished` (bool, default false) |
| `get_run` | Xem chi tiết kết quả, log và phân tích mã nguồn của một lần chạy trong lịch sử (để trích xuất tham số) | `runId` (int, required), `analyze` (bool, default true) |
| `propose_tool` | Đề xuất mã nguồn C# tool mới (chạy bộ kiểm tra Clean Code ADR-0007 tự động) | `name` (string, required), `description` (string, required), `category` (string, required), `inputSchema` (object, required), `code` (string, required), `examples` (array, required), `title` (string?), `tags` (string[]?), `transaction` ("auto"/"manual"/"none", default "auto"), `timeoutSeconds` (int, default 60), `sourceRunId` (int?), `newVersion` (bool, default false) |
| `test_tool` | Chạy thử nghiệm tool trong sandbox với bộ tham số kiểm thử mẫu | `name` (string, required), `cases` (array?), `realRun` (bool, default false) |
| `publish_tool` | Phát hành tool đã test đạt chuẩn vào thư viện hoạt động chính thức | `name` (string, required) |
| `manage_tool` | Quản lý vòng đời tool (deprecate, retire, reactivate, xóa) khỏi registry | `name` (string, required), `action` ("deprecate"/"retire"/"reactivate"/"delete", required), `reason` (string?) |

---

## 5. Các Ví dụ Thực hành Điển hình (Practical Examples)

### Ví dụ 1: Chạy tự động file `.gh` với tham số đầu vào (GH Player)
```json
{
  "name": "run_grasshopper_player",
  "arguments": {
    "filePath": "C:\\GrasshopperScripts\\FacadeOptimization.gh",
    "inputs": {
      "PanelWidth": 1500,
      "PanelHeight": 3300,
      "CurvatureFactor": 1.25
    },
    "outputs": ["OptimizedPanels", "PanelCount"],
    "headless": true
  }
}
```

### Ví dụ 2: Thêm component và kết nối dây trên canvas Grasshopper
```json
{
  "name": "batch_edit_grasshopper_canvas",
  "arguments": {
    "componentsToAdd": [
      { "type": "Addition", "x": 100.0, "y": 200.0, "nickName": "+", "tempId": "add_1" },
      { "type": "NumberSlider", "x": 350.0, "y": 200.0, "nickName": "Val", "tempId": "slider_1" }
    ],
    "componentsToRemove": [],
    "wiresToConnect": [
      { "source": "slider_1", "sourceOutput": 0, "target": "add_1", "targetInput": 0 }
    ],
    "wiresToDisconnect": [],
    "dryRun": false
  }
}
```

### Ví dụ 3: Tạo cốt thép từ đường Curve với dung sai đường kính (Seed Tool)
```json
{
  "name": "generate_rebar_curves",
  "arguments": {
    "hostElementId": 128450,
    "rebarCurves": [
      [
        { "startX": 0.0, "startY": 0.0, "startZ": 0.0, "endX": 4.5, "endY": 0.0, "endZ": 0.0 }
      ]
    ],
    "barDiameterMm": 18.0,
    "diameterToleranceMm": 2.0
  }
}
```

### Ví dụ 4: Tạo DirectShape bounding volume trong Revit (Seed Tool)
```json
{
  "name": "directshape_from_geometry",
  "arguments": {
    "shapeType": "Solid",
    "category": "GenericModel",
    "boundingDimensions": {
      "width": 2.5,
      "length": 5.0,
      "height": 3.2
    },
    "name": "Facade_Pod_Block"
  }
}
```

### Ví dụ 5: Truy vấn tóm tắt hình học phần tử Revit (Seed Tool)
```json
{
  "name": "query_revit_geometry",
  "arguments": {
    "elementIds": [128450, 128451],
    "detailLevel": "Fine",
    "includeFaces": true
  }
}
```

### Ví dụ 6: Thực thi C# Roslyn Script tương tác Revit và RhinoCommon
```csharp
// Biến môi trường có sẵn: doc (GH_Document), canvas (GH_Canvas), rhinoDoc (RhinoDoc), revitDoc (Autodesk.Revit.DB.Document)
using System.Linq;
using Autodesk.Revit.DB;
using Rhino.Geometry;

var columns = new FilteredElementCollector(revitDoc)
    .OfCategory(BuiltInCategory.OST_StructuralColumns)
    .WhereElementIsNotElementType()
    .ToElements();

var points = columns.Select(c => {
    var loc = (LocationPoint)c.Location;
    return new Point3d(loc.Point.X * 304.8, loc.Point.Y * 304.8, loc.Point.Z * 304.8); // convert feet -> mm
}).ToList();

return $"Đã trích xuất {points.Count} toạ độ cột từ Revit sang Rhino!";
```

---

## 6. Quy tắc An toàn, Giới hạn & Best Practices (Q-Rules)

1. **Luôn kiểm tra `executionEnabled` trước khi chạy script ghi:** Nếu gặp mã lỗi `-32001`, hướng dẫn người dùng bật checkbox "Allow AI execution" trong status window của Revit.
2. **Khuyên dùng `dryRun: true` cho lần chạy đầu tiên:** Đặc biệt khi can thiệp cấu trúc đồ thị canvas hoặc tạo đối tượng mới trên Revit, để đảm bảo kiểm tra tính hợp lệ trước khi cam kết thay đổi thật.
3. **Chỉ định danh tính tài liệu (`targetDocTitle` / `targetDefId`):** Khi làm việc trong môi trường đa dự án, truyền `targetDocTitle` hoặc `targetDefId` (qua từ điển `args` của `execute_grasshopper_code` hoặc raw Bridge JSON-RPC) để bridge thực hiện kiểm tra `fail-closed`, bảo đảm không sửa đổi nhầm mô hình.
4. **Đơn vị hình học:** Lưu ý Revit sử dụng đơn vị nội bộ là `Feet`, trong khi Grasshopper/Rhino thường dùng `Millimeters` hoặc `Meters`. Chú ý hệ số chuyển đổi khi đồng bộ dữ liệu.
5. **Tiêu chuẩn Ánh xạ Lỗi (`isError: true`):** Khi kiểm tra kết quả từ tool, kiểm tra cả `isError: true` và `success: false` trong payload JSON để phát hiện kịp thời các lỗi runtime solver hoặc lỗi preflight.
6. **Không viết mã chết hoặc nuốt exception:** Tuân thủ chuẩn Clean Code ADR-0007 khi dùng `propose_tool` (mỗi phương thức $\le 50$ dòng, lớp $\le 300$ dòng, không dead code, không catch rỗng không chú thích).
