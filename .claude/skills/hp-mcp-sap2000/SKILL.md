---
name: hp-mcp-sap2000
description: "Kết nối và điều khiển SAP2000 27 qua HPSap2000 MCP (server hprebar-sap2000, tool mcp__hprebar-sap2000__*): đọc model (hệ tọa độ, lưới, frame/area/point, vật liệu, tiết diện, tải, kết quả phản lực/nội lực), sửa model (vẽ frame theo tọa độ m, gán tiết diện, gán liên kết gối, gán tải), chạy phân tích, viết C# SAP2000v1 qua execute_sap2000_code với tier R/W/D + snapshot .SDB, registry tool (propose/test/publish). TRIGGER when: user nhắc 'SAP2000', 'sap2000', 'sapModel', 'SAP2000v1', 'OAPI SAP', 'hprebar-sap2000', 'bridge SAP2000', 'phản lực SAP', 'nội lực SAP', 'run analysis sap', 'load case/combo sap', 'frame section sap', 'kết nối SAP2000', 'attach SAP2000', hoặc lỗi -32001/-32003/PREVIEW/PATH từ tool SAP2000. Keywords: sap2000, csi, oapi, sap2000v1, sapmodel, mcp, bridge, snapshot, reactions, frame forces, run analysis, structural model. Khi cần đọc/sửa/phân tích model SAP2000 đang mở qua MCP, viết script SAP2000v1, hoặc gỡ lỗi kết nối bridge SAP2000."
metadata:
  author: hoang
  version: "1.0.0"
  mcp-server: hprebar-sap2000
---

<!-- portable-host-contract:start -->
## Portable host contract

This skill is shared by Codex and Google Antigravity.

- Use the host's native task tracker. In Codex, use `update_plan`; in Antigravity, maintain the task-list artifact.
- Use native collaboration and user-input tools exposed by the host. Treat legacy tool names as capability descriptions, never literal calls.
- Resolve bundled resources from the project-local `.agents/skills/` tree. Do not create or write a user-global skill directory.
<!-- portable-host-contract:end -->

# HP MCP SAP2000 — điều khiển SAP2000 27 qua `hprebar-sap2000`

## Overview

Dạy Claude dùng đúng 24 tool của MCP server `hprebar-sap2000` (`mcp__hprebar-sap2000__*`) để làm việc với model SAP2000 27 đang mở: chuỗi **Claude → HPSap2000.Mcp.Server (stdio) → pipe `hpsap2000-mcp-27` → HPSap2000.McpBridge.exe (app WPF riêng, giữ COM attach) → SAP2000v1 OAPI → SAP2000.exe**. SAP2000 **không có transaction/undo** — an toàn đến từ tier R/W/D quyết định trước khi chạy + save + snapshot `.SDB` trước mọi ghi + 2 checkbox opt-in trong bridge.

**Execution Units:** Internal execution được ép sang `kN_m_C` (Kilonewton, Metre, °C) và khôi phục sau khi chạy:
- Chiều dài: Metre (m)
- Lực: Kilonewton (kN)
- Moment: kN·m
- Ứng suất / Áp lực: kN/m² (kPa)

**Scope:** skill này xử lý *sử dụng* MCP SAP2000 (kết nối, chọn tool, viết script, đọc kết quả, gỡ lỗi). **Không** xử lý: sửa source `HPSap2000/` (xem `AGENTS.md` mục "HPSap2000 MCP Bridge" + `HPSap2000/README.md`), Revit/AutoCAD/Navisworks/ETABS MCP (server khác), thiết kế kết cấu (kết quả trả về là số liệu, kết luận kỹ thuật thuộc kỹ sư).

## Bước 0 — Kết nối (checklist, làm theo thứ tự)

1. **SAP2000 27** mở từ shortcut, **rồi** File › Open model đã lưu `.SDB` cục bộ (mở bằng double-click `.SDB` hoặc SAP2000 chạy elevated → không đăng ký API object → bridge báo "not registered for the API in this session").
2. **Bridge app** `HPSap2000/output/HPSap2000.McpBridge/HPSap2000.McpBridge.exe`: **Start listener** → **Attach** → tick **Allow AI code execution**. Tick **Allow destructive operations** chỉ khi cần và user đồng ý; cả 2 tắt lại mỗi lần mở app.
3. `.mcp.json` có entry `hprebar-sap2000` (exe `HPSap2000/output/HPSap2000.Mcp.Server/HPSap2000.Mcp.Server.exe`, env `HPSAP2000_MCP_Bridge__HostVersion=27`) — user thêm, không commit.
4. Kiểm tra nhanh không side-effect: `pwsh .agents/skills/hp-mcp-sap2000/scripts/check-sap2000-mcp.ps1` (SAP2000/bridge process, pipe, exe, `.mcp.json`).
5. Gọi `get_sap2000_context` — **luôn là call đầu tiên** của phiên: `sap2000.isAttached`, `docPath`, `isLocked`, `executionEnabled`, `destructiveOperationsEnabled`, counts. Không attached → hướng dẫn user bấm Attach; không có `docPath` → model chưa lưu, mọi ghi bị từ chối.

## Workflow decision tree

```text
Yêu cầu của user
 ├─ Hỏi/đọc model, kết quả      → seed R (get_*), thiếu → search_tools → thiếu nữa → execute_sap2000_code transaction="none"
 ├─ Sửa model (gán, vẽ, tải)     → seed W (dryRun=true trước) hoặc execute_sap2000_code: dryRun=true → xem PREVIEW → user OK → dryRun=false, transaction="auto", label ngắn
 ├─ Chạy phân tích / unlock / file → D: hỏi user tick "Allow destructive operations" → run_analysis / execute_sap2000_code
 ├─ Việc lặp lại nhiều lần        → toolify: get_run → propose_tool → test_tool → publish_tool → user approve CLI
 └─ Lỗi kết nối / -3200x          → references/troubleshooting.md
```

### 1. Đọc model — ưu tiên seed tool (đã review, ổn định)

| Cần | Tool | Ghi chú |
|---|---|---|
| Tổng quan file, lock, units, counts | `get_model_info` (`includeGroups`) | gọi sau `get_sap2000_context` |
| Hệ tọa độ + hệ lưới | `get_coordinate_systems_and_grids` (`includeGridLines`) | tọa độ và đường lưới |
| Frame/area/point, lọc group/tên | `get_structural_objects` (`kind`, `group`, `nameLike`, `limit` ≤ 500, `offset`) | trả **unique name** — dùng cho mọi tool khác |
| Vật liệu + tiết diện frame | `get_materials_and_sections` | E, G MPa; A m², I m⁴ |
| Pattern / case / combo | `get_load_definitions` (`includeComboCases`) | |
| Phản lực gối | `get_joint_reactions` (`caseOrCombo` bắt buộc, `pointNames`) | Fx, Fy, Fz kN, Mx, My, Mz kN·m |
| Nội lực frame | `get_frame_forces` (`caseOrCombo`, `frameNames`) | P/V2/V3 kN, T/M2/M3 kN·m, station m |
| Member API chưa rõ | `inspect_type` (`typeName` = `cSapModel`, `cFrameObj`…) | chữ ký C# thật từ wrapper |

Không có seed phù hợp → `search_tools` (query tiếng Việt/Anh) → vẫn không → viết script R (mục 3).

### 2. Ghi model — luôn preview trước

1. `get_sap2000_context`: `docPath` phải có, `isLocked=false` (model locked từ chối gán; unlock là D và **xoá kết quả phân tích**).
2. Seed W: `draw_frame_by_coords` (m, global), `assign_frame_section`, `assign_joint_restraint` (fixed, pinned, roller, free, custom), `assign_frame_load` (kN/m hoặc kN, `direction` 10 = gravity, 1–3 local, 4–6 global XYZ). Chạy `dryRun=true` trước trên model thật.
3. Script tự viết: `execute_sap2000_code` với `dryRun=true` → kết quả `isError` + diagnostic `PREVIEW` liệt kê member ghi (`cFrameObj.SetSection (W)`), **không chạy gì**. Trình danh sách này cho user.
4. User xác nhận → `dryRun=false`, `transaction="auto"`, `label` ngắn (≤ 64 ký tự, `[A-Za-z0-9_-]`). Bridge **save model + copy snapshot** `prerun\<timestamp>-<label>.SDB` vào `%LocalAppData%\HPSap2000\McpBridge\snapshots\<model>\` trước khi chạy; kết quả có `snapshot` = tên file, `changed.added/deleted` (chỉ đếm thêm/xoá object; `Set*` = 0).
5. Exception sau khi đã ghi → `rolledBack:false`, message "changes … persisted — snapshot <file>": báo user tên snapshot để khôi phục thủ công (File › Open snapshot trong SAP2000).

### 3. Script `execute_sap2000_code` — contract tối thiểu

- Globals: `sapModel` (cSapModel), `sap` (cOAPI), `units`, `ct`, `log(string)`, `progress(cur,total,msg)`, `args`. Usings sẵn: `System`, `System.Linq`, `System.Collections.Generic`, `SAP2000v1`, `HPRebar.McpBridge.Core.Scripting`.
- **Units ép `kN_m_C` mỗi run** (m, kN, kN·m, kN/m²), khôi phục sau.
- Mọi OAPI trả `int ret` → `if (ret != 0) throw new InvalidOperationException($"SAP2000 returned {ret} from X");`. Input sai của caller → `ArgumentException` (không tính vào stability của tool).
- Mảng qua `ref`: `int n = 0; string[] names = null; int ret = sapModel.FrameObj.GetNameList(ref n, ref names);`
- Tham số đi qua `args.Str/Int/Double/Bool/Strings/Require("key")` + `args` JSON trong call; kết thúc bằng `return <value>;` (object ẩn danh serialize được). `ct.ThrowIfCancellationRequested()` trong vòng lặp dài.
- Cấm (guard từ chối): `Helper`, `new Helper()`, `ApplicationExit`, `MessageBox`, `await`/`Task`/`Thread`, `dynamic`, `unsafe`, `System.IO`/`System.Net`/`Process`/reflection, `#r`/`#load`.
- Tier quyết định **tĩnh** từ member bind được: alias/cast/`?.`/lambda/method group vẫn bind đúng; member không có trong bảng = **D**. Path chỉ được là literal hoặc `args.Str("key")` (không fallback), dưới thư mục model hoặc `%LocalAppData%\HPSap2000\`, không UNC → khác = từ chối `PATH`.

### 4. Destructive (D) — hỏi user trước, không tự bật

`run_analysis`, `SetModelIsLocked`, `DeleteResults`, `File.Save/OpenFile/New*`, `Delete*`/`Rename*`/`Export*`/`Import*`/`Start*`/`Modify*`/`Clear*`/`Reset*`, member nhận path.

1. Nói rõ với user việc gì sẽ xảy ra và **không undo được**; bảo user tick **Allow destructive operations** trong cửa sổ bridge.
2. `run_analysis`: `cases` (mặc định mọi case), `deleteResultsFirst`, `timeoutSeconds` (≤ 600) — **timeout không dừng SAP2000**, các call sau trả busy tới khi xong.
3. Checkbox tắt → JSON-RPC `-32001`. Preview (`dryRun`/`none`) của script D **không cần** opt-in.

### 5. Toolify — biến script hay dùng thành tool

`get_run(runId)` → `propose_tool` (`name` snake_case, `category` ∈ Model/Geometry/Property/Load/Analysis/Results/Data/Generic, `inputSchema`, `code`, ≥ 2 `examples`, `transaction` none/auto) → `test_tool` → `publish_tool` → user chạy `HPSap2000.Mcp.Server.exe registry approve <name> --by <who>`.

## Bảng tier & tham số

| Tier | Ví dụ member | `transaction` | `dryRun` | Opt-in | Timeout |
|---|---|---|---|---|---|
| R | `Get*/Is*/Has*/Count`, `Results.*`, `Results.Setup.*`, `SelectObj.*`, `View.*`, `DatabaseTables.GetTableForDisplayArray` | `none` | chạy bình thường | execution | 5–120 s |
| W | `Set*/Add*` object/định nghĩa, `EditGeneral.Move`, `Analyze.SetRunCaseFlag`, `PointObj.SetRestraint` | `auto` (`manual` ≡ auto) | preview tĩnh | execution | 5–120 s |
| D | `RunAnalysis`, `DeleteResults`, `SetModelIsLocked`, `FrameObj.Delete`, `File.Save/OpenFile`, member path | `auto` | preview tĩnh, **không cần** opt-in D | execution **+ destructive** (chỉ khi chạy thật) | ≤ 600 s |
