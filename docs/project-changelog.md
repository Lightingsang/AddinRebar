# Changelog — HPRebar

Ghi lại thay đổi đáng kể. Mục mới nhất ở trên.

## 2026-09-14 — AutoCAD MCP bridge phase 1: loader, ALC, bundle, spike

Bổ sung: loader DLL với BridgeLoadContext (Roslyn 5.9 + Immutable 10 riêng, AutoCAD API shared), self-check startup, 4 core command (HPMCPBRIDGE/STATUS/START/STOP) + spike 2 command tạm thời, bundle 24 file / 14 MB → `%AppData%\Autodesk\ApplicationPlugins\`. Spike chạy 5/5 lần ×3 run liên tiếp, cuối cùng unattended (SECURELOAD auto-click, `Document.CloseAndDiscard()` + `Quit()` exit code 0).

**Xác minh:** Build zero warn/err, tests 70+106+334 xUnit pass, live in AutoCAD 2026 (2026-09-14).

**Các quyết định:**
- ADR-02 Accepted: Idle one-shot, timeout hủy subscribe (không để hang), executor rule: complete request trước unsubscribe
- ADR-05 Accepted: SECURELOAD prompt trên mỗi hash loader mới → "Always Load"; signing hoãn tới pack phase
- Spike gated `HPAUTOCAD_MCP_SPIKE=1` env var; xóa ngay khi phase 2 đâm ống listener

**Những chưa làm:**
- Pipe listener + executor (phase 2)
- MCP server exe + tools (phase 3)
- Modal dialog + Dynamo coexistence test
- Multi-version (R26/R27)

Plan: [`plans/260913-0000-autocad-mcp-bridge-2026/`](../plans/260913-0000-autocad-mcp-bridge-2026/plan.md) (phase 1/6 done).

## 2026-09-14 — Tách MCP engine host-neutral, scaffold AutoCAD

Refactor hạ tầng MCP để dùng chung cho Revit + AutoCAD. Tách engine (neutral với host) ra thư mục `McpShared/` cấp cao nhất; tạo scaffold `HPAutoCad/` để bắt đầu bridge AutoCAD.

Plan: [`plans/260913-0000-autocad-mcp-bridge-2026/phase-00-…`](../plans/260913-0000-autocad-mcp-bridge-2026/plan.md) (phase 0 xong; phase 1–5 tiếp theo).

### Thêm

- **`McpShared/`** (thư mục cấp cao nhất, slnx + global.json riêng):
  - `HPRebar.Mcp.Contracts/` (moved từ HPRebar) — netstandard2.0, hợp đồng dây JSON-RPC.
  - `HPRebar.McpBridge.Core/` (moved, refactor) — net8.0, pipe listener/dispatcher, guard/compiler, `BridgeSettingsStore`, `RequestDispatcher` dispatch theo method suffix (`revit.*` vs `autocad.*`), `IHostProfile` interface mới, `HostNeutralityTests`.
  - `HPRebar.Mcp.Server.Core/` (NEW) — net10.0 class lib, `McpServerHost` builder pattern, `HostProfile` abstraction, `ExecuteCodeService`/`ContextService`, Registry engine (`ToolRecord.Host/HostVersions`, SQLite WAL+FTS5), 4 core tool + registry CLI.
  - `HPRebar.Mcp.Server.Core.Tests/` (NEW) — net10.0 xUnit v3, 70 test: host-neutrality, guard/compiler/args/analyzer, HostProfile binding, registry store/db/manager, pipe round-trip fake executor.

- **`HPAutoCad/`** (thư mục cấp cao nhất, slnx + global.json, scaffold):
  - `HPAutoCad.slnx` reference `../McpShared/...` projects only.
  - `README.md` ghi layout, không có project class C# chưa.

### Sửa

| Vấn đề | Xử lý |
|---|---|
| HPRebar.Mcp.Server bị phồng (server + registry + pipe client) | Tách pipe client + registry → McpShared; server nay là thin exe (Program.cs một dòng) |
| Registry instance-per-app (Revit 2026 lúc này) | HostProfile dùng chung, registry root = `%AppData%\<Product>\McpServer` |
| Server build từ HPRebar cwd, khó test riêng | McpShared.slnx độc lập, test từ `cd McpShared && dotnet test` |
| Revit ↔ AutoCAD share pipes/registry mà không có cách select | RequestDispatcher routing: method prefix suffix xác định executor (`revit.execute` ≡ `autocad.execute`, ngoài các tool factory/impl riêng) |

### Bỏ

- HPRebar.Mcp.Server không còn phụ thuộc trực tiếp `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Contracts` (khác HPRebar folder).
- Hard-code "HPRebar"/"Revit" ở vài chỗ error string (engine chỉ biết host qua HostProfile).

### Hoãn

- AutoCAD bridge + server implementation (phase 01–02 chưa làm).

### Giới hạn

- McpShared test chạy từ McpShared cwd (global.json pin), HPRebar test chạy từ HPRebar cwd.
- Revit bridge DLL chưa rebuild → vẫn dùng được exe mới (backward compatible).
- Deploy builder auto-generate `Solutions.HPRebar` via Sourcy thay cho scan repo — phần này cần verify HPAutoCad tương tự.

## 2026-09-04 — Tích hợp NotebookLM (`notebooklm-py`)

Nối `teng-lin/notebooklm-py` 0.8.2 (MIT, unofficial) vào repo trên ba mặt, kèm pipeline nạp tài liệu Revit của repo lên notebook rồi kéo học liệu về.

Plan: [`plans/260904-1038-install-notebooklm-py-integration/`](../plans/260904-1038-install-notebooklm-py-integration/plan.md) · Docs: [`docs/notebooklm-integration.md`](notebooklm-integration.md)

### Thêm

- **Agent skill** `.claude/skills/notebooklm/` + mirror `.agents/skills/notebooklm/` — SKILL.md 60 → 61 ở cả hai cây.
- **MCP server** — `.mcp.json` đầu tiên của repo, 38 tool qua `uvx --from "notebooklm-py[mcp]"`.
- **Python surface** — venv riêng `scripts/.venv`, wrapper `scripts/notebooklm_client.py`, pipeline `scripts/build-notebooklm-course-assets.py` + manifest allow-list.
- **`tests/notebooklm/`** — 24 test `unittest`, chạy được bằng system Python (không cần `notebooklm`, không chạm mạng).
- **`docs/notebooklm-integration.md`** — cài lại từ máy trắng, version pin, bẫy đã dính.

### Sửa

| Vấn đề | Xử lý |
|---|---|
| `privacy-block` chỉ khớp `.env*` và `/credentials/i` — cookie Google đọc thoải mái | Thêm `storage_state.json` + `master_token.json` vào `privacy-checker.cjs` |
| `npx skills add` clone cả repo upstream (111 MB) kèm `CLAUDE.md`/`AGENTS.md` lồng trong `.claude/skills/` | Cắt còn `SKILL.md` + `LICENSE` (28K); ghi bước cắt vào plan |
| `generate_quiz`/`generate_flashcards` không nhận `language` → `TypeError` giữa chừng sau khi quota đã tiêu | `generate_and_wait` lọc kwarg theo chữ ký thật, log cảnh báo khi bỏ |

### Ghi chú

- Credential nằm ngoài worktree (`~/.notebooklm/`) — cố ý, để commit nhầm là bất khả thi về cấu trúc.
- Tài khoản là tier Pro (đo được: 300 source/notebook, 500 notebook), không phải free như plan giả định ban đầu.
- `AGENTS.md` đang lệch `CLAUDE.md`; re-sync là việc của plan `260823`.

## 2026-09-04 — Column Rebar (port từ `R01_ColumnsRebar`)

Feature đầu tiên của HPRebar. Port tool `R01_ColumnsRebar` (~19.5k dòng, .NET 4.8, Revit 2021, MVVM tự viết) sang Nice3point SDK + CommunityToolkit.Mvvm, đa version R23–R27.

Plan: [`plans/260903-2307-port-column-rebar-to-hprebar/`](../plans/260903-2307-port-column-rebar-to-hprebar/plan.md)

### Thêm

- **`HPRebar.Core`** (netstandard2.0) — toán thuần, không reference Revit. 25 file / ~1.5k dòng. 99 test xUnit.
- **`HPRebar.Core.Tests`** — xUnit v3 + Microsoft.Testing.Platform.
- **`HPRebar.Tests`** — TUnit chạy in-process trong Revit. 16 test, hiện skip hết vì thiếu model mẫu.
- **Feature `Column Rebar`** — 77 file / ~6.5k dòng: đọc hình học, 14 rule kiểm tra, tạo thép chủ + đai + đai phụ, tạo view/dimension/bảng thống kê, UI 8 tab, 4 control preview.
- **Theme** — `Resources/Themes/` 8 file, Dark + Light, theo `UIThemeManager` của Revit.
- **Song ngữ EN/VN** — đổi runtime, không đóng dialog.
- **Serilog file sink** → `%LocalAppData%\HPRebar\logs\`, xoay theo ngày, giữ 7 file.
- **`.gitattributes`** — git-lfs cho `*.rvt`/`*.rfa`/`*.rte`; ép LF cho fixture skill-sync.

### Sửa (bug có trong tool gốc)

| # | Bug | Ảnh hưởng |
|---|---|---|
| B1 | `GetErrorColumns` gán `error =` 15 lần không `return` | Báo lỗi **cuối cùng** thay vì lỗi đầu tiên user gặp |
| B2 | `IsnotVerticalColumns` `return` trong vòng `for` | Chỉ kiểm tra cột đầu tiên |
| B3 | 5 chỗ `OrderBy` không gán kết quả | `zb`/`hb` đọc từ mặt dầm bất kỳ, không phải mặt thấp nhất |
| B4 | `BarMainModel.cs:61` `Bar = Bar;` tự gán | Tham số `rebarBarModel` bị vứt |
| B5 | `GetWallBoudingBoxOneColumn` so `ElementId` với `Level` | Luôn true → không bao giờ tìm thấy tường đỡ cột |
| B6 | `double.Parse(UnitFormatUtils.Format(...))` ở 7 chỗ | Đổi đơn vị qua chuỗi, phụ thuộc culture |
| — | `CreateAddVerticalStirrupRectangleType1Item` dùng `BarH` thay `BarV` ở cả 4 chỗ | Đai phụ dọc dựng bằng shape của đai phụ ngang |

### Bỏ

- `WpfCustomControls` + `DSP` (dependency ngoài) — thay bằng CommunityToolkit.Mvvm + `RevitUnits`.
- `System.Windows.Forms.MessageBox` → `TaskDialog`.
- TaskBar (logo / YouTube / Account).
- Start/End section view (bản gốc đã comment out).
- Combobox chọn rebar shape ở tab Setting — bản gốc ép về `M_T1`/`M_T3` nên là UI chết.
- `ViewSchedule` cho detail component — bản gốc lọc field theo tên chuỗi tiếng Anh, hỏng trên Revit khác ngôn ngữ; chỉ phục vụ detail shop vốn chưa làm.

### Hoãn

- **Đường `IsRebar = false`** (thay thép thật bằng Detail Item `DT00..DT07A`) — cần family riêng của tác giả gốc mà repo không có.
- **Detail shop** (`DS*` family) — cùng lý do.
- **Thép gia cường (AddBar)** — chưa có ở Core.
- **Nav icon PathGeometry** — nav hiện chỉ có text.

### Giới hạn — đọc trước khi dùng

**Chưa có phiên bản Revit nào được verify runtime.** Build sạch 5/5 config (Debug + Release, 0 error, 0 warning CS) là bằng chứng duy nhất hiện có.

- Máy dev chỉ cài Revit 2025 + 2026 → R23/R24/R27 **build-only**.
- 16 TUnit test skip hết vì thiếu `HPRebar.Tests/Fixtures/column-stack-2-storey.rvt`.
- Rủi ro cụ thể chưa đo được: hack `SURFACE→LINEAR` trong `DimensionCreator` (undocumented, chưa chạy lần nào), `FormattedText` trên net48, Polyfill `MinBy` trên net48, .NET 10 runtime của R27.
