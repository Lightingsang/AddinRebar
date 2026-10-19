---
name: hp-mcp-grasshopper
description: "Kết nối và điều khiển Grasshopper trong Revit 2026 qua HPGrasshopper MCP (server hprebar-grasshopper, tool mcp__hprebar-grasshopper__*): đọc cấu trúc canvas đồ thị hiện hành (.gh / .ghx, components, wires, status, errors), giải và thực thi định nghĩa Grasshopper Player với tham số đầu vào inputs, tạo/sửa/xoá component và dây nối (batch_edit_grasshopper_canvas với GH_UndoRecord), xuất ảnh canvas đồ thị, thực thi trực tiếp C# Roslyn scripting trên Revit Document và RhinoDoc/GH_Document với cơ chế an toàn 2 tầng (Revit TransactionGroup rollback cho dryRun=true + Grasshopper GH_UndoRecord), 12 embedded seeds chuyên biệt Grasshopper ↔ Revit (Levels/Grids, cột/dầm parametric, curve-to-rebar, đai stirrups, sàn floors, curtain walls, facade panels, solar radiation...), registry meta tools (search/propose/test/publish). TRIGGER when: user nhắc 'Grasshopper', 'GH', 'ghx', 'Rhino.Inside', 'Rhino.Inside.Revit', 'Rhino 8', 'canvas grasshopper', 'component grasshopper', 'nối dây grasshopper', 'batch edit grasshopper', 'chạy file gh', 'hprebar-grasshopper', 'hpgrasshopper-mcp-2026', hoặc lỗi -32001/-32002/GUARD từ tool Grasshopper. Keywords: grasshopper, gh, rhino, rhinoinside, revit, mcp, bridge, canvas, component, wire, player, batch_edit, dryrun. Khi cần đọc/sửa/chạy đồ thị Grasshopper hoặc viết script Grasshopper/Revit qua MCP."
metadata:
  author: hoang
  version: "1.0.0"
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
- **An toàn 2 tầng (Dual-layer Safety):** Mọi thay đổi trong Revit Document được bọc trong Revit `TransactionGroup("MCP Grasshopper: <label>")`. Khi đặt `dryRun: true`, toàn bộ element sinh ra sẽ được **rollback sạch 100%**. Trạng thái canvas Grasshopper được bọc trong `CanvasUndoScope` và ghi nhận `GH_UndoRecord` đầy đủ để người dùng có thể Ctrl+Z hoàn tác.
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

## 4. Danh mục 24 Công cụ Chi tiết (Tool Catalog)

### 4.1. Core Tools (6 công cụ điều khiển lõi)
| Tool | Mục đích | Tham số chính |
|---|---|---|
| `get_grasshopper_context` | Đọc trạng thái bridge, phiên bản Rhino/GH, opt-in status, document đang mở | *(không có tham số)* |
| `read_grasshopper_canvas` | Trích xuất toàn bộ cấu trúc node, wires, parameters, solver errors/warnings | `includeParams` (bool), `includeErrors` (bool) |
| `run_grasshopper_player` | Mở & giải file định nghĩa `.gh`/`.ghx` với tham số đầu vào inputs (GH Player style) | `filePath` (string, required), `inputs` (object), `extractOutputs` (bool), `dryRun` (bool) |
| `batch_edit_grasshopper_canvas` | Thêm, xoá, nối dây và cấu hình components theo batch với UndoRecord | `components` (array), `wires` (array), `parameters` (array), `dryRun` (bool) |
| `execute_grasshopper_code` | Chạy C# Roslyn script đa môi trường (Revit API + RhinoCommon + GH API) | `code` (string, required), `transaction` ("auto"/"manual"/"none"), `dryRun` (bool), `timeoutSeconds` (int, 5–600) |
| `export_grasshopper_canvas_image` | Xuất ảnh chụp canvas Grasshopper chất lượng cao (PNG/base64) | `outputPath` (string), `zoomToExtents` (bool) |

### 4.2. Embedded Seed Tools (12 công cụ chuyên biệt Grasshopper ↔ Revit)
| Nhóm | Tool | Mô tả & Chức năng |
|---|---|---|
| **Revit Model Sync** | `create_levels_grids` | Tạo hệ lưới trục và cao độ tầng tự động trong Revit từ đường line Grasshopper |
| | `generate_columns_beams` | Sinh dầm và cột kết cấu Revit theo đường tâm hình học parametric |
| | `generate_rebar_curves` | Chuyển đổi đường curve 3D thành cốt thép Rebar thực tế trong Revit |
| | `generate_stirrup_distribution` | Tự động tính toán và rải cốt đai gia cường theo khoảng cách thay đổi |
| | `create_floors_slabs` | Tạo sàn bê tông Revit từ đường biên kín (boundary curves) |
| | `create_wall_openings` | Đục lỗ mở cửa sổ / kỹ thuật trên tường Revit từ profile hình học Grasshopper |
| **Geometry & Facade**| `curtain_grid_layout` | Tạo hệ khung lưới và đố kính Curtain Wall theo phân vị phức tạp |
| | `directshape_from_geometry` | Chuyển nhanh hình khối phức tạp từ Rhino/GH sang Revit DirectShape |
| | `evaluate_solar_radiation` | Đánh giá bức xạ mặt trời trên bề mặt vỏ bao che công trình |
| | `facade_panelization` | Phân chia và tối ưu hóa panel mặt đứng theo kích thước tiêu chuẩn |
| | `query_revit_geometry` | Trích xuất hình học phần tử Revit sang định dạng Brep/Mesh của Rhino |
| **Model Exchange** | `export_model_to_rhino` | Xuất toàn bộ mô hình hoặc cấu kiện chọn lọc từ Revit sang file `.3dm` |

### 4.3. Registry Meta Tools (8 công cụ quản lý thư viện mở rộng)
`search_tools`, `propose_tool`, `test_tool`, `publish_tool`, `list_tools`, `get_tool_source`, `retire_tool`, `view_tool_docs`.

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
    "extractOutputs": true,
    "dryRun": false
  }
}
```

### Ví dụ 2: Thêm component và kết nối dây trên canvas Grasshopper
```json
{
  "name": "batch_edit_grasshopper_canvas",
  "arguments": {
    "components": [
      { "guid": "44be1b89-b22e-4f0e-b3f4-3d07e60a373a", "name": "Number Slider", "x": 100, "y": 200, "tempId": "$s1" },
      { "guid": "6a388b14-0466-419b-a7e8-4688fa553e1a", "name": "Circle", "x": 350, "y": 200, "tempId": "$c1" }
    ],
    "wires": [
      { "fromComponent": "$s1", "fromParam": 0, "toComponent": "$c1", "toParam": 1 }
    ],
    "dryRun": false
  }
}
```

### Ví dụ 3: Thực thi C# Roslyn Script tương tác Revit và RhinoCommon
```csharp
// Biến môi trường có sẵn: doc (GH_Document), canvas (GH_Canvas), rhinoDoc (RhinoDoc), revitDoc (Autodesk.Revit.DB.Document)
using System.Linq;
using Autodesk.Revit.DB;
using Rhino.Geometry;

// Lấy danh sách cột trong Revit
var columns = new FilteredElementCollector(revitDoc)
    .OfCategory(BuiltInCategory.OST_StructuralColumns)
    .WhereElementIsNotElementType()
    .ToElements();

// Sinh điểm tâm toạ độ trong Rhino
var points = columns.Select(c => {
    var loc = (LocationPoint)c.Location;
    return new Point3d(loc.Point.X * 304.8, loc.Point.Y * 304.8, loc.Point.Z * 304.8); // convert feet -> mm
}).ToList();

return $"Đã trích xuất {points.Count} toạ độ cột từ Revit sang Rhino!";
```

---

## 6. Quy tắc An toàn, Giới hạn & Best Practices (Q-Rules)

1. **Luôn kiểm tra `executionEnabled` trước khi chạy script ghi:** Nếu `-32001`, hướng dẫn người dùng bật checkbox trong Revit.
2. **Khuyên dùng `dryRun: true` cho lần chạy đầu tiên:** Đặc biệt khi tạo mới đối tượng Revit hoặc can thiệp canvas phức tạp.
3. **Đơn vị hình học:** Lưu ý Revit sử dụng đơn vị nội bộ là `Feet`, trong khi Grasshopper/Rhino thường dùng `Millimeters` hoặc `Meters`. Chú ý hệ số chuyển đổi khi đồng bộ dữ liệu.
4. **Không viết mã chết hoặc nuốt exception:** Tuân thủ chuẩn Clean Code ADR-0007 khi dùng `propose_tool`.
