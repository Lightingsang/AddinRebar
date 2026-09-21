---
name: hp-mcp-robot
description: "Kết nối và điều khiển Autodesk Robot Structural Analysis Professional 2026 qua HPRobot MCP (server hprebar-robot, tool mcp__hprebar-robot__*): đọc model (thông tin mô hình, hệ lưới trục, node/bar/panel, vật liệu, tiết diện, tải trọng/tổ hợp, nội lực/phản lực), sửa model (tạo node, vẽ thanh dầm/cột theo tọa độ, gán tiết diện, gán liên kết gối, gán tải phân bố/tập trung), chạy phân tích kết cấu (run_calculations), viết C# Roslyn RobotOM qua execute_robot_code với 3-tier safety R/W/D + auto snapshot .rtd, registry tool (propose/test/publish). TRIGGER when: user nhắc 'Robot Structural Analysis', 'Robot', 'RSA', 'RobotOM', 'hprebar-robot', 'hprobot-mcp-2026', 'bridge Robot', 'nội lực Robot', 'phản lực Robot', 'run calculations robot', 'thanh bar robot', 'tiết diện robot', 'kết nối Robot', hoặc lỗi -32001/-32002/-32003 từ tool Robot. Keywords: robot, rsa, robotom, autodesk robot, structural analysis, fea, bar, node, panel, reactions, bar forces, calculations, mcp, bridge, snapshot. Khi cần đọc/sửa/phân tích model Robot đang mở qua MCP, viết script RobotOM, hoặc gỡ lỗi kết nối bridge Robot."
metadata:
  author: hoang
  version: "1.0.0"
  mcp-server: hprebar-robot
---

<!-- portable-host-contract:start -->
## Portable host contract

This skill is shared by Codex and Google Antigravity.

- Use the host's native task tracker. In Codex, use `update_plan`; in Antigravity, maintain the task-list artifact.
- Use native collaboration and user-input tools exposed by the host. Treat legacy tool names as capability descriptions, never literal calls.
- Resolve bundled resources from the project-local `.agents/skills/` tree. Do not create or write a user-global skill directory.
<!-- portable-host-contract:end -->

# HP MCP Robot — điều khiển Robot Structural Analysis Professional 2026 qua `hprebar-robot`

## Overview

Dạy Claude dùng đúng 24 tool của MCP server `hprebar-robot` (`mcp__hprebar-robot__*`) để làm việc với model Autodesk Robot Structural Analysis Professional 2026 đang mở: chuỗi **Claude → HPRobot.Mcp.Server (stdio) → pipe `hprobot-mcp-2026` → HPRobot.McpBridge.exe (app WPF riêng, giữ COM attach) → RobotOM.dll API → Robot.exe**. Robot **không có transaction/undo native** — an toàn đến từ tier R/W/D quyết định trước khi chạy + save + auto snapshot `.rtd` trước mọi ghi + 2 checkbox opt-in trong bridge.

**Execution Units:** Internal execution được chuẩn hóa Metric (Meter, Kilonewton, °C, MPa) qua `RobotUnitsPolicy` và tự động khôi phục đơn vị gốc của người dùng sau khi chạy:
- Chiều dài: Metre (m)
- Lực: Kilonewton (kN)
- Moment: kN·m
- Ứng suất / Áp lực: Megapascal (MPa)

**Scope:** skill này xử lý *sử dụng* MCP Robot (kết nối, chọn tool, viết script, đọc kết quả, gỡ lỗi). **Không** xử lý: sửa source `HPRobot/` (xem `AGENTS.md` + `HPRobot/README.md`), Revit/AutoCAD/Navisworks/ETABS/SAP2000 MCP (server khác), thẩm định thiết kế kết cấu (kết quả trả về là số liệu, kết luận kỹ thuật thuộc kỹ sư).

## Bước 0 — Kết nối (checklist, làm theo thứ tự)

1. **Robot Structural Analysis Professional 2026** mở từ shortcut, **rồi** mở file model đã lưu `.rtd` cục bộ.
2. **Bridge app** `HPRobot/output/HPRobot.McpBridge/HPRobot.McpBridge.exe` (hoặc chạy từ bin Debug/Release): Bấm **Attach** → tick **Allow AI code execution**. Tick **Allow Heavy/Delete operations** chỉ khi cần xoá cấu kiện hoặc chạy `Calculate()` phân tích kết cấu và user đã đồng ý; cả 2 tự tắt lại mỗi lần mở app.
3. `.mcp.json` có entry `hprebar-robot` (exe `HPRobot/output/HPRobot.Mcp.Server/HPRobot.Mcp.Server.exe`, env `HPROBOT_MCP_Bridge__HostVersion=2026`).
4. Kiểm tra nhanh: `pwsh .agents/skills/hp-mcp-robot/scripts/check-robot-mcp.ps1` (Robot process, bridge process, pipe, exe).
5. Gọi `get_robot_context` — **luôn là call đầu tiên** của phiên: kiểm tra `isAttached`, `docPath`, `executionEnabled`, `heavyOperationsEnabled`, số lượng node/bar/panel. Nếu chưa attached → hướng dẫn user bấm Attach trên Bridge; nếu không có `docPath` → model chưa lưu, mọi thao tác ghi bị từ chối để bảo vệ dữ liệu.

## Workflow decision tree

```text
Yêu cầu của user
 ├─ Hỏi/đọc model, kết quả      → seed R (get_*), thiếu → search_tools → thiếu nữa → execute_robot_code transaction="none"
 ├─ Sửa model (gán, vẽ, tải)     → seed W (dryRun=true trước) hoặc execute_robot_code: dryRun=true → xem PREVIEW → user OK → dryRun=false, transaction="auto", label ngắn
 ├─ Chạy tính toán FEA / Xoá    → D: hỏi user tick "Allow Heavy/Delete operations" → run_calculations / execute_robot_code
 ├─ Việc lặp lại nhiều lần        → toolify: propose_tool → test_tool → publish_tool
 └─ Lỗi kết nối / -3200x          → references/troubleshooting.md
```

### 1. Đọc model — ưu tiên seed tool (đã review, ổn định)

| Cần | Tool | Ghi chú |
|---|---|---|
| Tổng quan file, type, units, counts | `get_model_info` | gọi sau `get_robot_context` |
| Hệ tọa độ + lưới trục | `get_coordinate_systems_and_grids` | structural axes và toạ độ |
| Node/bar/panel, lọc theo số/loại | `get_structural_objects` (`kind`, `number`, `limit`) | lấy số hiệu đối tượng |
| Vật liệu + đặc trưng tiết diện | `get_materials_and_sections` | E, G; diện tích A, mômen quán tính Ix, Iy, Iz |
| Danh sách tải trọng + tổ hợp | `get_load_definitions` | Dead, Live, Wind, Combo factors |
| Phản lực gối liên kết | `get_node_reactions` (`nodeNumber`, `caseNumber`) | FX, FY, FZ, MX, MY, MZ |
| Biểu đồ nội lực thanh | `get_bar_forces` (`barNumber`, `caseNumber`, `pointsCount`) | My, Mz, Fz, Fy, Fx tại các điểm |

### 2. Sửa model — Tier Write (tự động sao lưu Snapshot .rtd)

| Thao tác | Tool | Ghi chú |
|---|---|---|
| Vẽ thanh dầm/cột mới theo tọa độ | `draw_bar_by_coords` (`x1,y1,z1, x2,y2,z2`, `sectionName`) | Tự tạo hoặc tái sử dụng node |
| Gán liên kết gối / biên tựa nút | `assign_node_support` (`nodeNumber`, `supportName`) | Fixed, Pinned, Roller... |
| Gán/đổi tiết diện thanh | `assign_bar_section` (`barNumber`, `sectionName`) | Cập nhật nhãn tiết diện |
| Gán tải trọng phân bố/tập trung | `assign_bar_load` (`barNumber`, `caseNumber`, `pz`, `px`, `py`) | Lực kN/m hoặc kN |

### 3. Tác vụ nặng / Xoá (Tier Delete & Heavy)

- `run_calculations`: Chạy bộ giải FEA của Robot (`project.CalcEngine.Calculate()`). Yêu cầu bật `AllowHeavyOperations`.
- Xoá đối tượng (`structure.Bars.Delete`, `structure.Nodes.Delete`): Yêu cầu bật `AllowHeavyOperations`.

## Scripting Guide (`execute_robot_code`)

Globals có sẵn trong context Roslyn C#:
- `robot`: `IRobotApplication` — gốc COM Robot
- `structure`: `IRobotStructure` — truy cập Nodes, Bars, Panels, Cases, Results
- `units`: `IRobotUnitMngr` — quản lý đơn vị
- `args`: `ScriptArgs` — tham số JSON đầu vào
- `log`: `Action<string>` — ghi log vào Bridge
- `progress`: `Action<int, int?, string?>` — báo cáo tiến trình
- `ct`: `CancellationToken` — hủy tác vụ khi timeout
