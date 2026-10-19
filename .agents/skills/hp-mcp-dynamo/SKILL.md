---
name: hp-mcp-dynamo
description: "Kết nối và điều khiển Dynamo trong Revit 2026 qua HPDynamo MCP (server hprebar-dynamo, tool mcp__hprebar-dynamo__*): chạy tự động file đồ thị .dyn với tham số đầu vào (Dynamo Player style) cả headless và live, đọc cấu trúc canvas đồ thị hiện hành (nodes, wires, status, errors), tạo/sửa node và dây nối (batch_edit_graph theo lô, add node, CodeBlock, connect wires, set properties, export canvas image), thực thi trực tiếp cú pháp DesignScript và C# Roslyn scripting trên Revit document với cơ chế an toàn 2 tầng (Revit TransactionGroup rollback cho dryRun=true + Dynamo UndoRecorder), registry tools (search/propose/test/publish). TRIGGER when: user nhắc 'Dynamo', 'dyn', 'đồ thị dynamo', 'dynamo revit', 'designscript', 'node dynamo', 'nối dây dynamo', 'batch edit dynamo', 'chạy file dyn', 'hprebar-dynamo', 'hpdynamo-mcp-2026', hoặc lỗi -32001/-32002/GUARD từ tool Dynamo. Keywords: dynamo, dyn, designscript, revit, mcp, bridge, graph, node, wire, codeblock, batch_edit_graph, dryrun. Khi cần đọc/sửa/chạy đồ thị Dynamo hoặc viết script DesignScript qua MCP."
metadata:
  author: hoang
  version: "1.0.0"
  mcp-server: hprebar-dynamo
---

<!-- portable-host-contract:start -->
## Portable host contract

This skill is shared by Codex and Google Antigravity.

- Use the host's native task tracker. In Codex, use `update_plan`; in Antigravity, maintain the task-list artifact.
- Use native collaboration and user-input tools exposed by the host. Treat legacy tool names as capability descriptions, never literal calls.
- Resolve bundled resources from the project-local `.agents/skills/` tree. Do not create or write a user-global skill directory.
<!-- portable-host-contract:end -->

# HPDynamo MCP — Điều khiển Dynamo Revit 2026 qua `hprebar-dynamo`

## 1. Tổng quan Kiến trúc (Overview)

Hệ thống **HPDynamo MCP** cho phép AI coding agent tương tác trực tiếp với **Dynamo 3.7** chạy bên trong **Autodesk Revit 2026**:
- **Chuỗi liên lạc:** AI Agent ──stdio──▶ `HPDynamo.Mcp.Server.exe` (.NET 10.0) ──Named Pipe `\\.\pipe\hpdynamo-mcp-2026`──▶ `HPDynamo.McpBridge.dll` (trong `Revit.exe`).
- **Dual-Host Lifecycle:**
  1. *Revit Add-in (`IExternalApplication`)*: Bật Named Pipe listener ngay khi mở Revit, cho phép chạy ngầm file `.dyn` qua `RevitDynamoModel` mà không cần mở giao diện Dynamo UI.
  2. *Dynamo ViewExtension (`IViewExtension`)*: Tự động móc vào `DynamoViewModel` khi người dùng mở cửa sổ Dynamo canvas, cho phép can thiệp trực quan (thêm node, nối dây, lấy dữ liệu output, xuất ảnh đồ thị).
- **An toàn 2 tầng:** Mọi thay đổi đối tượng trong Revit Document được bọc trong Revit `TransactionGroup("MCP Dynamo: <label>")`. Khi đặt `dryRun: true`, toàn bộ element sinh ra sẽ được **rollback sạch 100%**. Trạng thái canvas đồ thị được ghi nhận qua Dynamo `UndoRecorder`.
- **Bảo vệ Opt-In:** Cửa sổ trạng thái Bridge có nút gạt **"Allow AI execution in this Revit session"** (mặc định tắt mỗi khi bật Revit). Khi tắt, mọi thao tác ghi sẽ bị từ chối ngay lập tức với mã lỗi `-32001`.

---

## 2. Bước 0 — Kết nối & Thiết lập (Checklist theo thứ tự)

1. **Khởi động Revit 2026** và mở một dự án (.rvt). Add-in manifest `HPDynamo.McpBridge.addin` nằm tại `%AppData%\Autodesk\Revit\Addins\2026\`. Nếu Revit hỏi "Publisher could not be verified", chọn **Always Load**.
2. **Bật quyền thực thi AI (Opt-in):**
   - Trên thanh Ribbon Revit: bấm **HPDynamo ▸ MCP ▸ Status View**.
   - Tick chọn ô **"Allow AI execution in this Revit session"** (ô này tự tắt mỗi lần mở lại Revit để bảo vệ an toàn).
3. **Cấu hình `.mcp.json`:** Khai báo server `hprebar-dynamo` trỏ đến file exe đã build:
   ```json
   {
     "mcpServers": {
       "hprebar-dynamo": {
         "command": "G:\\09-PROJECT AI\\01_Revit\\02_CshapRevit\\01_AddinRebar\\HPDynamo\\HPDynamo.Mcp.Server\\bin\\Debug\\net10.0\\HPDynamo.Mcp.Server.exe"
       }
     }
   }
   ```
4. **Kiểm tra sức khỏe kết nối (Diagnostic script):**
   ```powershell
   pwsh .agents/skills/hp-mcp-dynamo/scripts/check-dynamo-mcp.ps1
   ```
5. **Gọi `get_dynamo_context` đầu tiên:** Kiểm tra trạng thái:
   - `executionEnabled`: Phải là `true` (nếu `false` -> nhắc người dùng tick checkbox).
   - `isDynamoUiOpen`: Cho biết cửa sổ canvas Dynamo có đang mở hay không.
   - `activeWorkspace`: Thông tin file `.dyn` đang mở (nếu có).

---

## 3. Sơ đồ Ra quyết định (Workflow Decision Tree)

```text
Yêu cầu của người dùng
 ├─ Kiểm tra kết nối / trạng thái Dynamo    → get_dynamo_context
 ├─ Chạy tự động file đồ thị .dyn           → validate_dyn_file (kiểm tra tính hợp lệ)
 │                                            → run_dyn_file với dryRun=true → xem kết quả
 │                                            → người dùng đồng ý → run_dyn_file với dryRun=false
 ├─ Thao tác trên Canvas đồ thị đang mở    → get_active_graph_info (đọc node, dây, trạng thái lỗi)
 │    ├─ Tạo/sửa cụm nhiều nodes và wires cùng lúc (Batch) → batch_edit_graph (1 dispatch, an toàn UndoRecorder, ID tạm $n1, $n2)
 │    ├─ Tìm kiếm node trong thư viện         → search_nodes_library (theo từ khóa/category)
 │    ├─ Tra cứu cổng in/out của node        → inspect_node_type
 │    ├─ Đặt node mới lên canvas             → add_node_to_workspace / create_code_block_node
 │    ├─ Nối dây giữa 2 node                 → connect_node_ports
 │    ├─ Ngắt dây nối                        → disconnect_node_ports
 │    ├─ Đổi tham số/slider/giá trị node     → set_node_properties
 │    ├─ Đọc dữ liệu output đã tính toán     → get_node_output_values
 │    └─ Xuất hình ảnh đồ thị                → export_graph_to_image
 ├─ Chạy mã linh hoạt (Ad-hoc Scripting)
 │    ├─ Chạy trực tiếp cú pháp DesignScript  → execute_designscript (hỗ trợ dryRun)
 │    └─ Viết C# Roslyn can thiệp sâu Dynamo  → execute_dynamo_code (biến globals: dynamo, workspace, doc)
 ├─ Đóng gói tác vụ lặp lại thành Tool mới  → propose_tool → test_tool → publish_tool
 └─ Gặp sự cố (-32001, -32002, lỗi kết nối)  → tra cứu references/troubleshooting.md
```

---

## 4. Danh mục 25 Công cụ Chi tiết (Tool Surface Reference: 4 Core + 8 Registry + 13 Seeds)

### 4.1. Core Tools (4 công cụ nền tảng)
| Tool | Mô tả | Tham số chính |
|---|---|---|
| `get_dynamo_context` | Lấy ngữ cảnh hiện tại của Dynamo & Revit | `includeSelection` (bool, default `false`) |
| `execute_designscript` | Thực thi đoạn mã DesignScript trên Revit doc | `code` (string, required), `transaction` ("auto"/"manual"/"none"), `dryRun` (bool), `timeoutSeconds` (int, 5–600), `label` (string), `args` (object) |
| `execute_dynamo_code` | Chạy C# Roslyn script với quyền truy cập Dynamo API | `code` (string, required), `transaction` ("auto"/"manual"/"none"), `dryRun` (bool), `timeoutSeconds` (int, 5–600), `label` (string), `args` (object) |
| `cancel_execution` | Hủy lệnh script hoặc graph đang chạy dở dang | *(không có tham số)* |

### 4.2. Embedded Seed Tools (13 công cụ nghiệp vụ hạt nhân)
| Nhóm | Tool | Mô tả & Tham số chính |
|---|---|---|
| **Graph Automation** | `batch_edit_graph` | Tạo cụm node, nối dây và đổi thuộc tính theo lô trong 1 transaction: `nodes` (array), `wires` (array), `properties` (array), `runEvaluation` (bool), `dryRun` (bool). Hỗ trợ ID tạm `$n1, $n2...`. |
| | `run_dyn_file` | Chạy file `.dyn` với tham số đè: `filePath` (string, required), `inputs` (object, dictionary tên/GUID node -> giá trị), `runHeadless` (bool), `extractOutputs` (bool), `dryRun` (bool). |
| | `validate_dyn_file` | Kiểm tra cú pháp, node thiếu, cyclic wire: `fileContent` (string, bắt buộc khi kiểm tra file offline chưa mở), `filePath` (string, khi kiểm tra workspace hiện hành), `checkPackages` (bool, default `true`). |
| | `get_active_graph_info` | Đọc thông tin canvas: `includeNodeList` (bool, default `false`), `includeErrors` (bool, default `true`). |
| **Node Authoring** | `search_nodes_library` | Tìm node: `query` (string, required), `category` (string), `maxResults` (int, default 50). |
| | `inspect_node_type` | Xem chi tiết node: `nodeType` (string, required). |
| | `add_node_to_workspace` | Đặt node mới: `nodeType` (string, required), `x` (number, required), `y` (number, required), `name` (string, optional), `initialValue` (string, optional). |
| | `create_code_block_node` | Tạo CodeBlock: `code` (string, required), `x` (number, default 100), `y` (number, default 100), `title` (string, optional). |
| | `set_node_properties` | Sửa giá trị/tọa độ node: `nodeId` (string, required), `name` (string), `value` (string), `isFrozen` (bool), `isVisible` (bool), `x` (number), `y` (number). |
| **Wiring & Data** | `connect_node_ports` | Nối dây: `sourceNodeId` (string, required), `sourcePortIndex` (int, required), `targetNodeId` (string, required), `targetPortIndex` (int, required). |
| | `disconnect_node_ports`| Ngắt dây: `nodeId` (string, required), `portIndex` (int, default 0), `portType` ("input"/"output", default "input"), `targetNodeId` (string, optional), `targetPortIndex` (int, optional). |
| | `get_node_output_values`| Lấy dữ liệu đã tính toán: `nodeIds` (array of string, optional), `includeGeometry` (bool, default `false`), `limit` (int, 1–200, default 50). |
| | `export_graph_to_image`| Chụp ảnh canvas: `outputPath` (string, optional), `format` ("PNG"/"JPEG", default "PNG"). |

### 4.3. Registry Meta Tools (8 công cụ quản lý thư viện mở rộng)
`search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`.

---

## 5. Các Ví dụ Thực hành Điển hình (Practical Examples)

### Ví dụ 1: Chạy tự động file `.dyn` với tham số đầu vào
```json
{
  "name": "run_dyn_file",
  "arguments": {
    "filePath": "C:\\DynamoScripts\\TaoCotTuCad.dyn",
    "inputs": {
      "ChieuCaoTang": 3600,
      "TietDienCot": "C400x400"
    },
    "dryRun": true
  }
}
```

### Ví dụ 2: Tạo node CodeBlock DesignScript và nối dây
1. Tạo node CodeBlock:
```json
{
  "name": "create_code_block_node",
  "arguments": {
    "code": "x = 5000;\ny = 3000;\nPoint.ByCoordinates(x, y, 0);",
    "x": 200,
    "y": 150
  }
}
```
2. Nối đầu ra của CodeBlock vào một node tạo đối tượng:
```json
{
  "name": "connect_node_ports",
  "arguments": {
    "sourceNodeId": "e3b0c442-98fc-1c14-9afb-4c8996fb9242",
    "sourcePortIndex": 0,
    "targetNodeId": "a1c2d3e4-5678-90ab-cdef-1234567890ab",
    "targetPortIndex": 0
  }
}
```

### Ví dụ 3: Thực thi trực tiếp đoạn mã DesignScript
```json
{
  "name": "execute_designscript",
  "arguments": {
    "code": "pts = Point.ByCoordinates(0..10000..#5, 0, 0);\nlines = Line.ByStartPointEndPoint(pts, Point.ByCoordinates(0..10000..#5, 5000, 0));",
    "dryRun": false
  }
}
```

### Ví dụ 4: Tạo cụm nodes và nối dây theo lô bằng `batch_edit_graph`
```json
{
  "name": "batch_edit_graph",
  "arguments": {
    "nodes": [
      { "tempId": "$code", "nodeType": "CodeBlock", "x": 100, "y": 100, "initialValue": "x = 1000;\ny = 2000;" },
      { "tempId": "$pt", "nodeType": "Point.ByCoordinates", "x": 400, "y": 100 },
      { "tempId": "$watch", "nodeType": "CoreNodeModels.Watch", "x": 700, "y": 100 }
    ],
    "wires": [
      { "fromNode": "$code", "fromPort": 0, "toNode": "$pt", "toPort": 0 },
      { "fromNode": "$code", "fromPort": 1, "toNode": "$pt", "toPort": 1 },
      { "fromNode": "$pt", "fromPort": 0, "toNode": "$watch", "toPort": 0 }
    ],
    "runEvaluation": true,
    "dryRun": false
  }
}
```

---

## 6. Gỡ lỗi Nhanh (Quick Troubleshooting)
- **Gặp lỗi `-32001`**: Người dùng chưa tick checkbox **"Allow AI execution in this Revit session"** trong cửa sổ Bridge WPF.
- **Gặp lỗi `-32002`**: Có lệnh hoặc đồ thị khác đang bận, đợi lệnh kết thúc hoặc gọi `cancel_execution`.
- Chi tiết các mã lỗi khác: đọc tệp [references/troubleshooting.md](file:///g:/09-PROJECT%20AI/01_Revit/02_CshapRevit/01_AddinRebar/.agents/skills/hp-mcp-dynamo/references/troubleshooting.md).
