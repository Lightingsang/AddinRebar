---
name: hp-mcp-tekla
description: "Kết nối và điều khiển Trimble Tekla Structures 2025 qua HPTekla MCP (server hprebar-tekla, tool mcp__hprebar-tekla__*): đọc model (thông tin mô hình, part, assembly, rebar, UDA, selection), sửa model (tạo dầm, cột, bản mã thép/bê tông, tạo nhóm cốt thép rebar group, thanh rebar đơn, cập nhật UDA), xuất dữ liệu (danh sách bản vẽ list_drawings, xuất IFC export_ifc), viết C# Roslyn Tekla Open API qua execute_tekla_code với 3-tier safety R/W/D + native savepoint rollback cho dryRun + auto snapshot .db1/.db2, registry tool (propose/test/publish). TRIGGER when: user nhắc 'Tekla', 'Tekla Structures', 'Tekla 2025', 'Tekla Open API', 'hprebar-tekla', 'hptekla-mcp-2025', 'bridge Tekla', 'cốt thép Tekla', 'rebar group', 'part tekla', 'dầm cột tekla', 'bản mã tekla', 'xuất IFC tekla', 'kết nối Tekla', hoặc lỗi -32001/-32002/-32003 từ tool Tekla. Keywords: tekla, tekla structures, tekla open api, rebar, beam, column, plate, ifc, drawing, mcp, bridge, snapshot, savepoint. Khi cần đọc/sửa/phân tích model Tekla đang mở qua MCP, viết script Tekla Open API, hoặc gỡ lỗi kết nối bridge Tekla."
metadata:
  author: hoang
  version: "1.0.0"
  mcp-server: hprebar-tekla
---

<!-- portable-host-contract:start -->
## Portable host contract

This skill is shared by Codex and Google Antigravity.

- Use the host's native task tracker. In Codex, use `update_plan`; in Antigravity, maintain the task-list artifact.
- Use native collaboration and user-input tools exposed by the host. Treat legacy tool names as capability descriptions, never literal calls.
- Resolve bundled resources from the project-local `.agents/skills/` tree. Do not create or write a user-global skill directory.
<!-- portable-host-contract:end -->

# HP MCP Tekla — Điều khiển Trimble Tekla Structures 2025 qua `hprebar-tekla`

## Overview

Dạy Claude dùng đúng 24 tool của MCP server `hprebar-tekla` (`mcp__hprebar-tekla__*`) để làm việc với mô hình Tekla Structures 2025 đang mở: chuỗi **Claude → HPTekla.Mcp.Server (stdio, .NET 10) → pipe `hptekla-mcp-2025` → HPTekla.McpBridge.dll (in-process plugin .NET Framework 4.8 chạy trong TeklaStructures.exe) → Tekla Open API 2025.0**.

**3-Tier Safety & Rollback:**
- **Tier R (ReadOnly):** Đọc mô hình, UDA, bản vẽ (`get_model_info`, `get_part_properties`, `select_objects`, `get_reinforcement_info`, `list_drawings`, `get_tekla_context`). Chạy an toàn với `transaction: "none"`.
- **Tier W (Write):** Tạo dầm, cột, bản mã, rebar, sửa UDA (`create_beam`, `create_column`, `create_contour_plate`, `create_rebar_group`, `create_single_rebar`, `modify_user_properties`). Tự động tạo snapshot sao lưu `.db1`/`.db2`. Khi `dryRun = true`, sử dụng cơ chế rollback nguyên bản `SetTestSavePoint()` / `RollbackToTestSavePoint()`, không commit vào database.
- **Tier D (Destructive):** Thao tác nặng / xuất file (`export_ifc`) hoặc xoá diện rộng. Yêu cầu bật quyền "Allow destructive operations".

## Bước 0 — Kết nối (checklist, làm theo thứ tự)

1. **Tekla Structures 2025.0** mở file model cục bộ (`.db1`).
2. **Bridge Plugin**: Khởi động plugin qua nút bấm trên Ribbon Tekla (hoặc tự động nạp từ thư mục extensions). Mở giao diện **HPTekla MCP Bridge** để kiểm tra trạng thái Pipe Server `hptekla-mcp-2025: Listening`. Đảm bảo checkbox **Allow AI code execution** đã được bật.
3. `.mcp.json` đã cấu hình server `hprebar-tekla` (trỏ đến `HPTekla/output/HPTekla.Mcp.Server/HPTekla.Mcp.Server.exe`).
4. Gọi `get_tekla_context` — **luôn là call đầu tiên** của phiên: kiểm tra `isAttached`, `modelPath`, `executionEnabled`, `destructiveOperationsEnabled`. Nếu chưa kết nối → yêu cầu mở Tekla và nạp Bridge.

## Workflow decision tree

```text
Yêu cầu của user
 ├─ Hỏi/đọc model, part, rebar    → seed R (get_*), thiếu → search_tools → thiếu nữa → execute_tekla_code transaction="none"
 ├─ Sửa model (dầm, cột, thép)   → seed W (dryRun=true trước) hoặc execute_tekla_code: dryRun=true → xem PREVIEW → user OK → dryRun=false, transaction="auto"
 ├─ Xuất IFC / Tác vụ xoá        → D: kiểm tra "Allow destructive operations" → export_ifc / execute_tekla_code
 ├─ Thao tác lặp lại nhiều lần   → toolify: propose_tool → test_tool → publish_tool
 └─ Lỗi kết nối / pipe           → kiểm tra TeklaStructures.exe và trạng thái Bridge UI
```

### 1. Đọc mô hình — Ưu tiên seed tool

| Cần làm | Tool | Ghi chú |
|---|---|---|
| Tổng quan model, path, project info | `get_model_info` | Gọi sau `get_tekla_context` |
| Tìm kiếm hoặc chọn đối tượng | `select_objects` (`kind`, `id`, `name`) | Lấy danh sách ID đối tượng |
| Chi tiết cấu kiện (profile, material, class, coords) | `get_part_properties` (`partId`) | Đọc chi tiết Part |
| Thống kê và thông số cốt thép | `get_reinforcement_info` (`rebarId` hoặc `partId`) | Trích xuất shape, size, spacing |
| Danh sách bản vẽ gia công/lắp dựng | `list_drawings` (`typeFilter`, `nameFilter`) | Assembly, Single-part, GA, Cast unit |

### 2. Mô hình hóa & Chỉnh sửa — Tier Write (Snapshot + Savepoint Rollback)

| Cần làm | Tool | Ghi chú |
|---|---|---|
| Tạo dầm thép hoặc bê tông | `create_beam` (`x1,y1,z1, x2,y2,z2`, `profile`, `material`) | Hỗ trợ dryRun |
| Tạo cột thép hoặc bê tông | `create_column` (`x,y,bottomZ,topZ`, `profile`, `material`) | Hỗ trợ dryRun |
| Tạo bản mã hoặc tấm phẳng | `create_contour_plate` (`points`, `profile`, `material`) | Poly-points |
| Rải nhóm cốt thép (rebar group/stirrups) | `create_rebar_group` (`shapePoints`, `spacing`, `size`, `grade`) | Hỗ trợ đai hoặc thép chủ |
| Tạo thanh cốt thép đơn lẻ | `create_single_rebar` (`shapePoints`, `size`, `grade`) | Rebar đơn |
| Cập nhật thuộc tính người dùng (UDA) | `modify_user_properties` (`objectId`, `attributes`) | Gán UDA string/int/double |

### 3. Tác vụ Destructive & Export

| Thao tác | Tool | Ghi chú |
|---|---|---|
| Xuất mô hình ra định dạng IFC | `export_ifc` (`outputPath`, `format`, `exportSelectedOnly`) | Yêu cầu quyền Destructive/Export |

## Scripting Guide (`execute_tekla_code`)

Globals có sẵn trong context Roslyn C#:
- `model`: `Tekla.Structures.Model.Model` — Đối tượng mô hình trung tâm Tekla Open API
- `selector`: `Tekla.Structures.Model.UI.ModelObjectSelector` — Bộ chọn đối tượng giao diện người dùng
- `args`: `ScriptArgs` — Tham số JSON đầu vào
- `log`: `Action<string>` — Ghi nhật ký vào Bridge UI
- `progress`: `Action<int, int?, string?>` — Cập nhật tiến độ
- `ct`: `CancellationToken` — Hủy bỏ tác vụ khi timeout

## Đề xuất tool — kiểm tra chất lượng code (ADR-0007)

`propose_tool` chạy bộ kiểm tra chất lượng của bridge trên `code` (quy tắc: `docs/clean-code/HP_CLEAN_CODE_CORE.md` §13, phụ lục host: `docs/clean-code/host-appendix/net48-inprocess.md`):

- **Bị từ chối (lỗi):** code bị comment lại (Q-B1), `catch` rỗng không có comment nói lý do (Q-B2), script > 300 dòng (Q-B3). Sửa code rồi `propose_tool` lại.
- **Chỉ cảnh báo:** khối / local function > 50 dòng (Q-W1), lồng > 3 cấp (Q-W2), tên mơ hồ `data`/`tmp`/`obj`/`res`/`val`… (Q-W3), tham số `bool` trên local function (Q-W4), `catch (Exception)` nuốt lỗi — không throw, không return, không dùng `ex` (Q-W5). Nên sửa trước khi publish.
- **`quality not analysed`:** bridge đang chạy là bản cũ, chưa có bộ kiểm tra — draft vẫn được lưu và publish vẫn được phép; ghi nhận trong báo cáo, redeploy bridge khi có thể.
