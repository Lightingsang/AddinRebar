# Codebase Summary — HPRebar + AutoCAD Bridge

Cập nhật: 2026-09-14

## Repository Layout

Gồm 4 unrelated deliverables (không cross-wire):

| Thư mục | Loại | Stack |
|---|---|---|
| `HPRebar/` | Revit add-in + server MCP | C# / Nice3point / WPF / xUnit / TUnit |
| `McpShared/` | Host-neutral MCP engine | netstandard2.0 · net8.0 · net10.0 |
| `HPAutoCad/` | AutoCAD add-in + server MCP (phases 1–3 ✅ verified) | C# / AutoCAD.NET / Roslyn (kế thừa từ McpShared) |
| `revit-market-research/` · `scripts/skill_sync/` · `course-website/` | Tooling | Node / Python / HTML |

## HPRebar Solution

`HPRebar/HPRebar.slnx` + global.json pin .NET SDK `10.0.300`, test runner `Microsoft.Testing.Platform`. Reference McpShared projects qua đường dẫn `../McpShared/...`.

| Project | TFM | Vai trò | Build? |
|---|---|---|---|
| `HPRebar/` | net48 (R23/R24) · net8.0-windows7.0 (R25/R26) · net10.0-windows7.0 (R27) | Add-in. Chạm Revit API | ✅ |
| `HPRebar.Core/` | netstandard2.0 | Toán thuần. **Không** reference Revit | ✅ |
| `HPRebar.Core.Tests/` | net8.0 | xUnit v3 — 334 test | ✅ |
| `HPRebar.Tests/` | R25/R26 | TUnit, load Revit in-process | ❌ |
| `HPRebar.Mcp.Server/` | net10.0 console | Thin exe (`Program.cs` một dòng) + 21 seed nhúng + CLI `registry …` | ✅ |
| `HPRebar.McpBridge/` | net8.0-windows7.0 (R25/R26) | Add-in thứ hai — bridge trong Revit (nút ExternalEvent + cửa sổ) | ✅ R25/R26 |
| `build/` | — | ModularPipelines | ❌ |
| `install/` | — | WixSharp installer | ❌ |

## McpShared Solution

`McpShared/McpShared.slnx` + global.json. Host-neutral engine dùng chung cho Revit + AutoCAD.

| Project | TFM | Vai trò |
|---|---|---|
| `HPRebar.Mcp.Contracts/` | netstandard2.0 | Envelope JSON-RPC + DTO chung server ↔ bridge. Không Revit, không MCP SDK |
| `HPRebar.McpBridge.Core/` | net8.0 | Pipe listener/dispatcher, Roslyn `ScriptGuard`/`ScriptCompiler`, `BridgeSettingsStore`, `RequestDispatcher`. Logic không chạm Revit; test xUnit + pipe thật |
| `HPRebar.Mcp.Server.Core/` | net10.0 | `McpServerHost` (builder pattern), `HostProfile` interface + Revit impl, `ExecuteCodeService`/`ContextService`, Registry engine (SQLite/FTS5 + FileSystemWatcher), 4 core tool + registry CLI |
| `HPRebar.Mcp.Server.Core.Tests/` | net10.0 | xUnit v3 — 96 test: host-neutrality, registry per profile, guard/compiler/args/analyzer, HostProfile binding, registry store/db/validator, pipe round-trip fake executor, both Revit + AutoCAD profiles, stability window |

## HPAutoCad Solution

`HPAutoCad/HPAutoCad.slnx` + global.json. Reference McpShared only; không dùng HPRebar. Phases 1–5 (2026-09-14): loader + ALC + bundle + bridge runtime + stdio server + registry per host profile + 12 embedded seed tools + live verification harness ✅ plan complete.

| Project | TFM | Vai trò | Build? |
|---|---|---|---|
| `HPAutoCad.McpBridge.Loader/` | net8.0-windows | IExtensionApplication + `HPMCP*` commands (HPMCPBRIDGE/STATUS/START/STOP); creates BridgeLoadContext, bootstraps bridge via reflection; **Ribbon (2026-09-14):** Ribbon tab "MCP AutoCAD" (Autodesk.Windows, three panels: Kết nối/Công cụ/Thiết lập, 9 buttons + 1 live label, `BridgeActions.cs` shared command/button runner, `Ribbon/{McpRibbonTab, RibbonStatusPresenter, RibbonCommandHandler, RibbonIcons}.cs` 398 total LOC), entry points (status.subscribe, copyLastScript, autoStart.get/set, path) stay BCL-only across ALC | ✅ |
| `HPAutoCad.McpBridge/` | net8.0-windows, UseWPF | Phases 1–2: Roslyn + self-check; `MainThreadExecutor` (Idle + IsQuiescent + PostMessage WM_NULL), `AutocadScriptRunner` (outer/inner transaction via `doc.TransactionManager`, dryRun, timeout), `DatabaseChangeCounter` (HANDSEED + events), `AutocadContextReader`/`AutocadResultSerializer`, XAML status window (theme-merged, opt-in "Allow AI code execution"); **Ribbon (2026-09-14):** new entry point file `BridgeEntry.Ribbon.cs` (4 additive entry points, 71 LOC) | ✅ |
| `HPAutoCad.Mcp.Server/` | net10.0 console | Phase 4 ✅: thin exe + `AutocadHostProfile` (registry per host profile) + 24 tools (4 core + 8 registry + 12 embedded seeds: 6 read-only data tools + 6 auto-transaction drawing tools in 7 categories). Registry root `%AppData%\HPAutoCad\McpServer\` (registry.db + tools-library, per-host). Pipe `hpautocad-mcp-2026`, env prefix `HPAUTOCAD_MCP_` | ✅ |
| `HPAutoCad.Mcp.Server.Tests/` | net10.0 | xUnit v3: 58 tests (profile, options, tool surface, context/resource/execute/refusal over pipe, 12 AutoCAD seeds compile-checked against AutoCAD.NET 25.1.0 with bridge's exact imports/globals, 0 skipped) | ✅ |

**Phase 2 additions:** `MainThreadQueue.cs`, `BridgeRequestException.cs`, `AutocadInsunits.cs`, `GuardProfile.AutoCAD` + deny `StartTransaction/StartOpenCloseTransaction/TopTransaction/LockDocument/ed.Get*`.

**Phase 4 additions:** Registry per host profile (`ToolValidator`, `ToolLifecycleService` on `IHostProfile`; categories, reserved names, host stamp from profile); 12 embedded seed tools (categories Drawing/Layer/Block/Annotation/Layout/Data/Generic); `RegistryToolText` host-neutral descriptions (8 engine tools); `SeedLibraryTests` 58 compile-checks + validate. Tests: McpShared 96/96, HPRebar MCP 109/109, AutoCAD 58/58.

**Phase 5 additions (harness & stability window):** `HPAutoCad/tools/harness/`: `run-live-verify.ps1`, `live-verify.py`, `mcp-session.py` (one stdio session, 65 scenarios isolated to ephemeral registry under `output/live-verify/`); matrix 18 (execute variants, guard, `cancel_execution`, busy → ESC → retry automated, timeout, audit), seeds 18 (real block, pickfirst), MISS → propose → test → publish → CLI approve (0.5 s `tools/list_changed`), fragile tool → quarantine → restore + newVersion (stays published on arg errors). Stability window: argument errors excluded, window restarts at lifecycle event so restored tool not re-quarantined. Live run 3: 64 pass + 1 skip + 4/4 isolation + 21/21 regression; xUnit 96/96 engine, 109/109 Revit server, 58/58 AutoCAD server (263 total).

**Ribbon tab additions (plan 260914-2204, 2026-09-14):** Ribbon UI entry via `run-ribbon-check.ps1` (UI Automation tab finder via `AutomationId`, COM workspace switch, PowerShell 5.1 object binding); 8/8 verification (tab created once, survives workspace round trip, all 9 buttons drive pipe/window/registry, status label updates live, no crashes in logs); regress bridge 21/21 + server 22/22 (smoke now accepts approved tools ≥ 24). Bundle version bumped to 0.2.0; README.md included in Contents for guide access. Loader harness helpers in `harness-common.ps1` (+3 functions: `Find-RibbonTabs`, `Select-RibbonTab`, `Invoke-RibbonButton`); `run-server-smoke.ps1` updated (+5 lines for assertion tweak).

**Phase 3–4 harness & verification:** `run-bridge-unattended.ps1` 21/21 bridge scenarios (context without Revit fields, 12 tools/list unchanged with Revit exe, none/dryRun/real execute through pipe, get_run from registry.db). `run-server-smoke.ps1` 22/22 (every seed by name through `tools/list` + `search_tools` category filter + dryRun + layer round-trip). Registry separate `%AppData%\HPAutoCad\` vs `%AppData%\HPRebar\`. SECURELOAD auto-accept, UI Automation opt-in. Zero crashes, no `.NET Runtime 1026` event.

**Publish:** `dotnet publish HPAutoCad/HPAutoCad.Mcp.Server -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false -p:IncludeNativeLibrariesForSelfExtract=true -o HPAutoCad/output/HPAutoCad.Mcp.Server` (7.4 MB). `.mcp.json` entry (user adds) → `hprebar-autocad` → that exe + env `HPAUTOCAD_MCP_Bridge__HostVersion=2026`.

**Runtime platform:** AutoCAD 2026 base release (R25.1, .NET 8) verified 2026-09-14. Harness in repo: `mcp-call.py`, `run-server-smoke.ps1`, `run-bridge-unattended.ps1`, `harness-common.ps1`.

`HPRebar.Tests` bị loại khỏi solution build có chủ đích: dưới config R23/R24 nó sẽ compile net8 rồi reference `HPRebar.dll` net48 → `CS0433 ReadOnlySpan<T> exists in both` (Polyfill nhúng span type vào assembly). Chạy riêng:

```bash
dotnet build HPRebar.Tests/HPRebar.Tests.csproj -c Debug.R26
```

## Ranh giới Core ↔ Revit

Nguyên tắc: **`HPRebar.Core` không bao giờ reference `Autodesk.Revit.*`**. `Document` là `sealed`, không mock được, nên toán phải tách ra mới test được bằng xUnit.

- Core làm việc bằng **millimét** (`double`).
- Chỉ 2 chỗ đổi đơn vị: [`RevitUnits`](../HPRebar/HPRebar/Column%20Rebar/RevitUnits.cs) (`MmToFt`/`FtToMm`).
- Đọc model → mm: `ColumnStackReader`. mm → Revit: `PointMapper`, `StirrupGeometry`.

ILRepack merge `HPRebar.Core.dll` vào `HPRebar.dll` khi deploy — thư mục add-in chỉ có 1 DLL.

## Feature: Column Rebar

77 file / ~6.5k dòng trong `HPRebar/HPRebar/Column Rebar/`, cộng 25 file / ~1.5k dòng Core.

Port từ `R01_ColumnsRebar` (~19.5k dòng, .NET 4.8, Revit 2021, MVVM tự viết). Plan: [`plans/260903-2307-port-column-rebar-to-hprebar/`](../plans/260903-2307-port-column-rebar-to-hprebar/plan.md).

### Luồng

```
ColumnRebarCommand.Execute
  PickObjects (StructuralColumnSelectionFilter)
  → sort theo cao độ mặt đáy
  → ColumnStackValidator.Validate        14 rule, trả lỗi ĐẦU TIÊN
  → ColumnStackReader.Read               → ColumnStack (mm)
  → DefaultRebarSpecBuilder.Build        spec mặc định
  → ColumnRebarSession + ViewModel + ColumnRebarView.ShowDialog()
       OK → RevitRebarRunner → ColumnRebarOrchestrator.Run
                                 ★ chủ sở hữu DUY NHẤT của TransactionGroup
                                 Tx "Create Detail View"
                                 Tx "Create Section View"
                                 Tx "Create Dimension View"
                                 Tx "Create Dimension Section"
                                 Tx "Create Stirrup Bars"
                                 Tx "Create Main Bars"
                                 Tx "Create Tag Bars"
                                 → Assimilate  (undo 1 bước)
       Cancel → không gọi Run → không có gì để rollback
```

### `HPRebar.Core/ColumnRebar/` — toán thuần

| File | Trách nhiệm |
|---|---|
| `BarLayoutCalculator` | vị trí thép chủ trên tiết diện (chữ nhật + tròn) |
| `BarSideClassifier` | số hiệu thanh → cạnh / phần tư |
| `DefaultOverlap` | nối chồng so le 35d / 70d |
| `SpliceCalculator` | tái phân bố thanh lên tiết diện trên |
| `DefaultUpperPositions` | lưới mặc định của tiết diện trên |
| `BarPolylineBuilder` | đường tim thanh (đáy → móc → bẻ xiên → neo) |
| `BarShapeClassifier` | đường tim → hình dạng uốn + chiều dài nhánh |
| `BarScheduleCalculator` | gom thanh giống nhau thành dòng bảng thống kê |
| `StirrupDistributionCalculator` | chia đai: đều, hoặc dày/thưa/dày |
| `CanvasScaleCalculator` | tỉ lệ vẽ preview |
| `Models/` (13 record/enum) | `ColumnSection`, `BarLayoutSpec`, `SpliceSpec`, `StirrupSpec`… |

### `Column Rebar/` — tầng Revit

| Nhóm | File |
|---|---|
| Đọc model | `ColumnSolidFaceReader`, `ColumnNeighbourFinder`, `ColumnStackReader` |
| Kiểm tra | `ColumnStackValidator` (14 rule), `ValidationMessages` |
| Tạo thép | `RebarTypeCatalog`, `RebarShapeResolver`, `PointMapper`, `MainBarCreator`, `StirrupGeometry`, `StirrupCreator`, `AdditionalTieCreator`, `RebarCreationService` |
| Tạo bản vẽ | `DetailViewCreator`, `SectionViewCreator`, `DimensionCreator`, `RebarTableTagCreator` |
| Điều phối | `ColumnRebarOrchestrator`, `RevitRebarRunner`, `ColumnRebarCommand` |
| UI | `View/` (9 XAML + code-behind ≤ 8 dòng), `View Models/` (13), `View/Controls/` (8 file vẽ canvas) |
| Khác | `RevitUnits`, `RevitDialogs`, `LocalizationService`, `ThemeSwitcher` |

### UI

MVVM Toolkit. `ColumnRebarSession` giữ state dùng chung, 8 tab đọc/ghi vào nó. Preview canvas gọi **đúng** calculator mà service tạo thép dùng — cái user thấy chính là cái sẽ dựng.

Theme: `Resources/Themes/` 8 file, mọi màu/spacing qua `{DynamicResource}`. `ThemeSwitcher` đọc `UIThemeManager` (R24+) để theo Dark/Light của Revit.

i18n: `UiStrings` record ~110 field, `UiStringsCatalog.English`/`.Vietnamese`, đổi cả record một lần → mọi nhãn refresh.

## Multi-version

2 block `#if` trong toàn bộ codebase, cả hai có comment `// Multi-version:`:

| Vị trí | Lý do |
|---|---|
| `MainBarCreator.cs` | `Rebar.CreateFreeForm`: overload `out RebarFreeFormValidationResult` bị xóa ở R27; overload `RebarStyle` chỉ có từ R26. Khác cả return type |
| `ThemeSwitcher.cs` | `UIThemeManager` chỉ có từ R24; R23 mặc định Dark |

Chi tiết đối chiếu API: [`plans/260903-2307-…/reports/api-surface-check.md`](../plans/260903-2307-port-column-rebar-to-hprebar/reports/api-surface-check.md).

## Lệnh

Mỗi thư mục có `global.json` pin runner, nên test chạy từ trong thư mục đó:

```bash
# HPRebar
cd HPRebar
dotnet build HPRebar.slnx -c Debug.R26          # chính (máy dev có Revit 2026)
dotnet build HPRebar.slnx -c Debug.R23          # net48, bắt lỗi TFM sớm
dotnet test HPRebar.Core.Tests                  # 334 test xUnit
dotnet test HPRebar.Mcp.Server.Tests            # 106 test xUnit — registry per profile, 21 Revit seed thực
dotnet build HPRebar.Tests/HPRebar.Tests.csproj -c Debug.R26   # TUnit, cần Revit

cd build && dotnet run                          # Release cả 5 config
cd build && dotnet run -- pack                  # Bundle → output/

# McpShared
cd McpShared
dotnet test HPRebar.Mcp.Server.Core.Tests       # 95 test xUnit — host-neutral engine, registry per profile

# HPAutoCad
cd HPAutoCad
dotnet build HPAutoCad.slnx -c Debug             # deploy bundle → %AppData%\Autodesk\ApplicationPlugins\
dotnet build HPAutoCad.slnx -c Debug -p:DeployBundle=false   # khi AutoCAD đang mở (DLL khóa)
dotnet test HPAutoCad.Mcp.Server.Tests          # 58 test xUnit — phase 4: 12 AutoCAD seeds compile-check, context/tools over pipe
```

**Revit đang mở sẽ khóa DLL đã deploy** → thêm `-p:DeployAddin=false` khi chỉ cần verify compile.

## Trạng thái verify

### Revit
| Version | Build | Runtime |
|---|---|---|
| 2023, 2024 | ✅ | ❌ không cài trên máy dev |
| 2025 | ✅ | ⚠️ chưa chạy |
| 2026 | ✅ | ⚠️ chưa chạy |
| 2027 | ✅ | ❌ không cài |

**Add-in rebar chưa có phiên bản nào được verify runtime.** 16 TUnit test đã viết nhưng skip hết vì thiếu model mẫu — xem `HPRebar/HPRebar.Tests/Fixtures/README.md`.

**MCP bridge** thì khác: đã chạy end-to-end trong Revit 2026 ngày 2026-09-12 (self-check Roslyn, `get_revit_context`, `inspect_type`, 15 kịch bản `execute_revit_code` gồm dryRun/commit/exception rollback/cancel/timeout).

### AutoCAD
| Version | Build | Runtime |
|---|---|---|
| 2026 | ✅ | ✅ 2026-09-14 (phases 1–4: 22/22 smoke harness all 12 seeds, 21/21 bridge scenarios, 58/58 tests, registry per profile, zero crashes) |
| 2027 .NET 10 | ❌ scaffold chưa test | ❌ |

## Ngoài add-in — tooling Python

Không thuộc solution `.slnx`, không ảnh hưởng build Revit.

| Đường dẫn | Vai trò | Chạy bằng |
|---|---|---|
| `scripts/skill_sync/` + `tests/skill-sync/` | Đồng bộ config agent `.claude` ↔ `.agents` ↔ `.codex` | system Python |
| `scripts/notebooklm_client.py` | Wrapper mỏng quanh `notebooklm-py` — import lười, nên hàm thuần chạy được ở mọi interpreter | `scripts/.venv` |
| `scripts/build-notebooklm-course-assets.py` + `notebooklm-sources.json` | Nạp docs Revit lên NotebookLM, sinh học liệu về `output/notebooklm/`. Idempotent theo hash | `scripts/.venv` |
| `tests/notebooklm/` | 24 test `unittest`, không chạm mạng | system Python (cố ý — chứng minh không kéo `notebooklm` ở top level) |
| `scripts/generate_revit_api_infographics.py` | Sinh infographic khoá học (one-off) | system Python |

`.mcp.json` khai báo MCP server `notebooklm` (38 tool). Chi tiết: [`notebooklm-integration.md`](notebooklm-integration.md).

## MCP bridge — Revit làm runtime cho AI

Thiết kế gốc: `plans/260912-1521-dynamic-revit-mcp-server-2026/architecture.md` + `adr/`. Hai tiến trình: host AI launch server exe làm child; server không reference Revit; bridge không reference MCP SDK; cầu nối chỉ qua `HPRebar.Mcp.Contracts`.

```
Claude Code ──stdio──▶ HPRebar.Mcp.Server (net10 console) ──named pipe hprebar-mcp-r2026 (JSON-RPC 2.0, 1 object/dòng)──▶ PipeListener (McpShared)
  (any AI host)         (thin exe: Program.cs một dòng)      guard/compile pipe thread                               ↓
                        + 21 seed nhúng                      ScriptGuard → ScriptCompiler (cache SHA-256)             ExternalEvent → Revit thread
                        + Registry engine (McpShared)        revit.analyze (no Revit needed)                           ↓
                                                                                                        McpBridgeExternalEventHandler → TransactionGroup → Revit API
```

| Thành phần | Ở đâu | Vai trò |
|---|---|---|
| **Hợp đồng dây** | | |
| `JsonRpcEnvelope`, `ExecuteRequest/Result`, `SafeText`, `SynchronousProgress` | `McpShared/HPRebar.Mcp.Contracts/` | Envelope strip path, forward progress đúng thứ tự |
| **Host-neutral engine** | | |
| `PipeListener`, `RequestDispatcher`, `IBridgeExecutor` | `McpShared/HPRebar.McpBridge.Core/Pipe/` | Listener 1 instance, ACL user, error code (`-32001`/`-32002`/`-32003`) |
| `ScriptGuard`, `ScriptCompiler`, `ScriptCache`, `TypeInspector`, `ScriptArgs`, `ScriptAnalyzer` | `McpShared/HPRebar.McpBridge.Core/Scripting/` | Deny-list walker, Roslyn cache LRU 50, reflection Revit API; `args` typed; literal/key walker cho `revit.analyze` |
| `BridgeSettingsStore`, `AuditLogger` | `McpShared/HPRebar.McpBridge.Core/Model/` | Settings per product (`%AppData%\<Product>\McpBridge`), audit JSON-lines |
| `IHostProfile`, `HostProfile`, `BridgeOptions`, `Host/*` | `McpShared/HPRebar.Mcp.Server.Core/` | Chọn executor (Revit/AutoCAD/khác), ProductFolder, HostVersion |
| `RegistryEngine`, `ToolRegistry`, `ToolRecord`, registry CLI | `McpShared/HPRebar.Mcp.Server.Core/Registry/` | SQLite WAL+FTS5, files = source, tool lifecycle, auto-quarantine |
| **Revit-cụ thể** | | |
| `RevitHostProfile`, `ExecuteRevitCodeTool`, `RevitContextTool` | `HPRebar/HPRebar.Mcp.Server/Hosts/Revit/` | Revit profile impl, 4 core tool + seed registry |
| `RevitBridgeClient`, `NdjsonPipeTransport`, `ResultFormatter` | `HPRebar.Mcp.Server/Services/` (mapped từ Core) | Ghép id↔response, timeout, reconnect backoff |
| `McpBridgeExternalEventHandler`, `ScriptRunner`, `RevitContextReader` | `HPRebar/HPRebar.McpBridge/` | Chỉ phần này chạm Revit API |
| `McpBridgeStatusView`, `McpBridgeStatusViewModel` | `HPRebar/HPRebar.McpBridge/View|ViewModel/` | Cửa sổ modeless, opt-in listener, last run |

Chính sách transaction: `auto` (bridge mở Transaction trong Group) · `manual` (script tự mở) · `none` (chỉ đọc) · `dryRun` luôn rollback. Timeout 5–120 s cooperative qua `ct`; hết hạn = thất bại + rollback kể cả khi script `return`. Không sandbox — 9 lớp phòng thủ (ADR-04).

Client: `.mcp.json` (untracked) entry `hprebar-revit` → `HPRebar/output/HPRebar.Mcp.Server/HPRebar.Mcp.Server.exe` (từ `dotnet publish … -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true`). Bridge cần bật listener (auto-start theo `settings.json`) và tick "Allow AI code execution" mỗi phiên Revit.

Registry (ADR-05/06): tool = `tool.json + code.cs + examples.json` ở `%AppData%\HPRebar\McpServer\tools-library\` (đổi qua `Registry:LibraryPath`); `registry.db` giữ lịch sử chạy + độ ổn định; policy `manual` — AI dừng ở `pending_approval` + `_review/<name>.md`, người duyệt bằng `HPRebar.Mcp.Server.exe registry approve <name> --by <ai>`; server đang chạy nhận thay đổi qua watcher (không restart). Đã verify live 3 kịch bản ngày 2026-09-12 (`plans/…/reports/phase-09-live-verify.md`).
