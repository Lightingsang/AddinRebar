# Changelog — HPRebar

Ghi lại thay đổi đáng kể. Mục mới nhất ở trên.

## 2026-09-14 — AutoCAD MCP bridge phase 5: live verification harness + stability-window fixes, plan complete

**Bổ sung:** Live verification harness (unattended) tại `HPAutoCad/tools/harness/` — `run-live-verify.ps1` + `live-verify.py` + `mcp-session.py` (≈560 + 120 + 150 lines) dùng một stdio MCP session để chạy 65 scenario (64 pass + 1 skip) trên registry riêng (isolated `output/live-verify/`) sinh động trong AutoCAD + Revit: execute matrix 18 (none/dryRun/commit, exception, none+modify, manual, guard trên `GetPoint`/`Commit`/`SendStringToExecute`, compile error, `cancel_execution` racing, timeout 5s, **busy → ESC posted → retry automated**, no drawing, audit), every seed 18 (real block, pickfirst set, dryRun), MISS → ad-hoc code + `propose_tool` → `test_tool` → `publish_tool` → CLI approve → `tools/list_changed` in 0.5s → call by name, fragile tool (unguarded `eKeyNotFound`) → 5× fail → quarantine → `manage_tool restore` + `propose_tool newVersion` (guarded, `ArgumentException`) → re-approve → stays published on 5× fail, Revit exe beside (34 tools, Revit lib hash unchanged, opt-in off → E1 skipped), isolation (second AutoCAD fails fast naming host, Civil 3D never loads).

**Stability window fixes (engine, both hosts):** Runs với lỗi `Argument…Exception:` không được tính (là lỗi caller, không tool), window restarts ở lifecycle event cuối (approved/published/restore/proposed_version/imported/status_changed), restored tool không bị re-quarantine bởi lỗi cũ. Hai defect tìm live: (1) seed `insert_block` bị quarantine bởi smoke tests của chính nó — fixed by excluding arg errors; (2) restored tool `mcp_verify_count_block_refs` bị re-quarantine ngay lần chạy đầu — fixed by restarting window at lifecycle event. Harness chạy registry isolated (không chạm `%AppData%` live) với flag `-IncludeIsolation` / `-OnlyIsolation` / `-SkipRevit` / `-UseLiveRegistry`.

**Xác minh:** Live run 2 (sau 2 engine fix) = 64 pass + 1 skip (Revit opt-in off) + isolation 4/4 + bridge regression 21/21; run 3 trên registry isolated: 64 pass + 1 skip. Build zero warn/err. Tests: McpShared 96/96, HPRebar MCP 109/109, AutoCAD 58/58 (263 total). Code review 7.5/10 → 16 actionable findings fixed; majors: the two engine fixes, harness registry isolation, pid-scoped cleanup. Open: Revit opt-in with new engine unverified at runtime (harness E skip); Revit 2025; AutoCAD 2026 U1.2 (.NET 10); modal dialog while waiting; per-run undo; `HPRebar/output/…exe` not republished (locked by running `hprebar-revit` MCP).

**Các quyết định:**
- Stability window logic: lỗi `Argument…Exception` do script tự từ chối đối số là lỗi caller, không phải tool failure → exclude khỏi quarantine check; window restart ở cuối lifecycle event mình = approved/published/restore/proposed_version/imported/status_changed từ bất cứ nguồn (manual edit `tool.json` triggered reload writes event, manual CLI action, etc.)
- Harness: one stdio session vì `notifications/tools/list_changed` chỉ reach running server; registry isolation = env var override LibraryPath + DbPath để server + CLI không chạm user's %AppData%; pid guard cleanup to avoid killing user-opened processes; `-UseLiveRegistry` opt để bypass isolation khi user muốn test on live registry
- Bridge `RequestDispatcher.HostVersion` + pipe-in-use message names host + version; Revit bridge log sink `shared: true` like AutoCAD
- `ToolRegistryDb` split: schema/search (cs) + runs/stability (Runs.cs partial)

**Commits:** d8507e8 feat (harness + engine fixes), 1b2ba8b fix (16 review findings). Plan: [`plans/260913-0000-autocad-mcp-bridge-2026/`](../plans/260913-0000-autocad-mcp-bridge-2026/plan.md) (phases 0–5 of 6 done, **plan complete**).

## 2026-09-14 — AutoCAD MCP bridge phase 4: registry per host profile + 12 seed tools

Bổ sung: Registry engine per `IHostProfile` (categories, reserved names from profile, host stamp); 12 embedded AutoCAD seed tools (6 read-only: list_layers, list_block_definitions, get_entities, list_layouts, get_drawing_info, get_selected_entities; 6 auto-transaction: draw_polyline, draw_circle, add_text, create_layer, insert_block, add_linear_dimension) installed into `%AppData%\HPAutoCad\McpServer\tools-library\` on first run across 7 categories (Drawing, Layer, Block, Annotation, Layout, Data, Generic); `SeedLibraryTests` 58 xUnit tests compile every seed against AutoCAD.NET 25.1.0 with bridge's exact imports/globals (0 skipped); meta-tool descriptions host-neutral (8 engine tools + inspect_type title worded for any host).

**Xác minh:** Live smoke harness `run-server-smoke.ps1` 22/22 (all 12 seeds by name, category filters, dryRun, layer round-trip), code review 7.5/10 with 12 actionable findings fixed the same day (no re-score), zero crashes, no .NET Runtime 1026 event. Tests: McpShared 95/95, HPRebar MCP 106/106, AutoCAD 58/58 (259 total). Build zero warn/err. Registry per host: `registry stats` prints `host: autocad (AutoCAD)` on AutoCAD server. Revit tools/list 34 names/schemas/annotations byte-identical to phase 0 (descriptions of 8 + title differ).

**Các quyết định:**
- `IHostProfile.CliExecutable` names the exe in every human instruction (publish_tool message, _review/*.md, CLI usage banner); defaults from assembly name if not overridden (Revit explicit `HPRebar.Mcp.Server.exe`, AutoCAD `HPAutoCad.Mcp.Server.exe`)
- Seed points as `{x, y}` objects not `[x, y]` arrays; mm at every edge via `ScriptUnits.ToDrawing/ToMm`; guard denies `StartTransaction/Commit/Abort/LockDocument`, `ed.Get*`
- No seed creates missing layer — clear error "run create_layer first"; only the `autocad_modify_template` prompt auto-creates
- Seed contract: no `tr` open/close (guard catches `UsesTransaction`), reads via `GetBlockModelSpaceId`, writes via `db.CurrentSpaceId` + `AppendEntity` + `AddNewlyCreatedDBObject`
- New `Validate(record, analysis, existing, newVersion, profile)` overload; the four-argument overload keeps the Revit behaviour locked (tests unchanged)

**Commits:** 69e3505 feat (engine + 12 seeds), f257071 fix (12 review findings). Plan: [`plans/260913-0000-autocad-mcp-bridge-2026/`](../plans/260913-0000-autocad-mcp-bridge-2026/plan.md) (phases 0–4 of 6 done).

## 2026-09-14 — AutoCAD MCP bridge phase 3: server exe over stdio

Bổ sung: `HPAutoCad.Mcp.Server` (net10 console exe) + `AutocadHostProfile` (12 tools: 4 core + 8 registry, `autocad://` resources, 2 prompts), registry root `%AppData%\HPAutoCad\McpServer\`, pipe `hpautocad-mcp-2026`, env prefix `HPAUTOCAD_MCP_`. Core: `ContextService.Shape` drops `revitVersion`/`isFamily` for non-Revit hosts (wire unchanged, Revit output byte-identical, regression test 89/89).

**Xác minh:** Publish exe 7.4 MB, stdio harness 7/7 ×2 (lead + tester): initialize, 12 tools/list, context without Revit fields, execute none/dryRun/real with runId + hint, get_run from registry.db. Revit exe 34 tools unchanged with Revit 2026 running side-by-side. Build zero warn/err. Tests: AutoCAD 8/8 + McpShared 89/89 + HPRebar MCP 106/106 (203 total). Harness files (`mcp-call.py`, `run-server-smoke.ps1`, `run-bridge-unattended.ps1`, `harness-common.ps1`) live in repo.

**Các quyết định:**
- `revitVersion`/`isFamily` hidden in `ContextService.Shape` (non-Revit hosts); Revit: wire unchanged, output byte-identical
- `IsModifiable` documented in Contracts XML comment + descriptions per host (Revit: transaction open; AutoCAD: writable + quiescent)
- Description length 1717 chars (AutoCAD contract needs `tr`/deny/units/semantics; plan budget ~1200, real budget 1800)
- Harnesses in repo (Python + PowerShell 7.3+, JSON quoting safe, depth guards)
- `.mcp.json` entry user adds (untracked); snippet in `HPAutoCad/README.md` + smoke report

**Những chưa làm:**
- Engine meta-tool descriptions still say "Revit"/`execute_revit_code` (phase 4: host-neutral wording or profile-driven text)
- Seed tools for AutoCAD (phase 4)
- R27 AutoCAD support (not planned)

**Các gaps được chấp nhận:**
- `runId`/`hint` branch exercised only by live smoke, not by xUnit (registry-less `ExecuteCodeService` in tests; phase 4 will add AutoCAD test with temp-dir `ToolManager`)
- Modal dialog while waiting + ESC-then-retry (phase 5 manual)
- Per-run undo (needs `ExecuteInCommandContextAsync`, phase 5)

Plan: [`plans/260913-0000-autocad-mcp-bridge-2026/`](../plans/260913-0000-autocad-mcp-bridge-2026/plan.md) (phases 1–3/6 done).

## 2026-09-14 — AutoCAD MCP bridge phase 2: bridge runtime

Bổ sung: `MainThreadExecutor` (Application.Idle + IsQuiescent, PostMessage WM_NULL wake, busy grace 8 s), `AutocadScriptRunner` (outer = group, inner = `tr`, commit inner trước quyết định outer, dryRun rollback), `DatabaseChangeCounter` (HANDSEED + ObjectOpenedForModify + IsErased, không giữ wrapper), `AutocadContextReader`/`AutocadResultSerializer` (entities, ObjectId handles, units mm ↔ drawing), XAML status window (theme dark/light override, per-session opt-in "Allow AI code execution"), Core `MainThreadQueue` + `BridgeRequestException` + `AutocadInsunits` + guard deny `StartTransaction`/`LockDocument`.

**Xác minh:** Build zero warn/err, tests McpShared 88/88 + HPRebar MCP 106/106, harness unattended 21/21 (lead + tester independent), AutoCAD 2026 R25.1, 234+ audit lines, zero crashes.

**Các quyết định:**
- ADR-03 Accepted (revised): outer transaction = TransactionGroup role, inner = `tr`, inner commit → event fire → count → outer decision; `transaction=manual` chạy như `auto` + cảnh báo, guard deny `StartTransaction` (vì finalizer crash ở GC thread)
- `transaction=none`: vẫn mở transaction (read cần tr ở AutoCAD), luôn abort, sửa như Revit nếu modified
- Undo merged per user command trong lock "HPMCP"; per-run undo out of MVP
- Idle one-shot, busy grace bị cắt bớt (tester #2), bỏ BusyGrace default 10s → enforce deadline

**Những chưa làm:**
- Modal dialog + ESC-then-retry (phase 5 manual)
- Per-run undo (needs `ExecuteInCommandContextAsync`)
- `IsModifiable` semantics document (phase 3)

Plan: [`plans/260913-0000-autocad-mcp-bridge-2026/`](../plans/260913-0000-autocad-mcp-bridge-2026/plan.md) (phases 1–2/6 done).

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
