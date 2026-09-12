# Codebase Summary — HPRebar

Cập nhật: 2026-09-12 (tối)

## Solution

`HPRebar/HPRebar.slnx` (định dạng XML `.slnx`, **không phải** `.sln`). `global.json` pin .NET SDK `10.0.300`, test runner `Microsoft.Testing.Platform`.

| Project | TFM | Vai trò | Trong solution build? |
|---|---|---|---|
| `HPRebar/` | net48 (R23/R24) · net8.0-windows7.0 (R25/R26) · net10.0-windows7.0 (R27) | Add-in. Mọi thứ chạm Revit API | ✅ |
| `HPRebar.Core/` | netstandard2.0 | Toán thuần. **Không** reference Revit | ✅ |
| `HPRebar.Core.Tests/` | net8.0 | xUnit v3 trên `HPRebar.Core` — 102 test | ✅ |
| `HPRebar.Tests/` | theo config R25/R26 | TUnit, load Revit in-process | ❌ `<Build Project="false"/>` |
| `HPRebar.Mcp.Contracts/` | netstandard2.0 | Envelope JSON-RPC + DTO dùng chung server ↔ bridge | ✅ |
| `HPRebar.Mcp.Server/` | net10.0 console | MCP server stdio (`ModelContextProtocol` 2.2.0) + **tool registry** (`Registry/`, SQLite/FTS5, 21 seed nhúng). Không reference Revit. Cũng là CLI `registry …` | ✅ |
| `HPRebar.McpBridge.Core/` | net8.0 | Nửa bridge không đụng Revit: pipe listener/dispatcher, Roslyn guard/compiler/cache, settings, audit | ✅ |
| `HPRebar.McpBridge/` | net8.0-windows7.0 (R25/R26) | Add-in thứ hai — bridge trong Revit: ExternalEvent, ScriptRunner, cửa sổ trạng thái | ✅ R25/R26, ❌ R23/R24/R27 (`<Build … Project="false"/>`) |
| `HPRebar.Mcp.Server.Tests/` | net10.0 | xUnit v3 — 159 test: pipe round-trip thật với fake executor, ScriptGuard/Analyzer/Args, registry (store, db, manager, registrar, validator, lifecycle, CLI), 21 seed compile với RevitAPI ref assemblies | ✅ |
| `build/` | — | ModularPipelines | ❌ |
| `install/` | — | WixSharp installer | ❌ |

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

```bash
cd HPRebar

dotnet build HPRebar.slnx -c Debug.R26          # chính (máy dev có Revit 2026)
dotnet build HPRebar.slnx -c Debug.R23          # net48, bắt lỗi TFM sớm
dotnet test HPRebar.Core.Tests                  # 334 test xUnit
dotnet test HPRebar.Mcp.Server.Tests            # 159 test xUnit — MCP server + registry + bridge Core, không cần Revit

dotnet build HPRebar.Tests/HPRebar.Tests.csproj -c Debug.R26   # TUnit, cần Revit

cd build && dotnet run                          # Release cả 5 config
cd build && dotnet run -- pack                  # 2 bundle (HPRebar, HPRebar.McpBridge) + HPRebar.Mcp.Server.zip + installer → output/
```

**Revit đang mở sẽ khóa DLL đã deploy** → thêm `-p:DeployAddin=false` khi chỉ cần verify compile.

## Trạng thái verify

| Revit | Build | Runtime |
|---|---|---|
| 2023, 2024 | ✅ | ❌ không cài trên máy dev |
| 2025 | ✅ | ⚠️ chưa chạy |
| 2026 | ✅ | ⚠️ chưa chạy |
| 2027 | ✅ | ❌ không cài |

**Add-in rebar chưa có phiên bản nào được verify runtime.** 16 TUnit test đã viết nhưng skip hết vì thiếu model mẫu — xem `HPRebar/HPRebar.Tests/Fixtures/README.md`.

**MCP bridge** thì khác: đã chạy end-to-end trong Revit 2026 ngày 2026-09-12 (self-check Roslyn, `get_revit_context`, `inspect_type`, 15 kịch bản `execute_revit_code` gồm dryRun/commit/exception rollback/cancel/timeout). Xem mục dưới.

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

Thiết kế gốc: `plans/260912-1521-dynamic-revit-mcp-server-2026/architecture.md` + `adr/`. Tóm tắt luồng:

```
Claude Code ──stdio──▶ HPRebar.Mcp.Server ──named pipe hprebar-mcp-r2026 (JSON-RPC 2.0, 1 object/dòng)──▶ HPRebar.McpBridge (trong Revit)
                        4 core + 8 registry + N tool thư viện   ScriptGuard → ScriptCompiler (pipe thread, cache SHA-256)
                        3 resource · 3 prompt                   revit.analyze (guard+compile+literal, pipe thread)
                                                                → McpBridgeExternalEventHandler → Revit thread
                                                                → ScriptRunner: TransactionGroup "MCP: <label>" → Revit API
```

| Thành phần | Ở đâu | Việc |
|---|---|---|
| `JsonRpcEnvelope`, `ExecuteRequest/Result`, `SafeText`, `SynchronousProgress` | `HPRebar.Mcp.Contracts/` | Hợp đồng dây, strip path, forward progress đúng thứ tự |
| `RevitBridgeClient`, `NdjsonPipeTransport`, `ResultFormatter` | `HPRebar.Mcp.Server/Services/` | Ghép id↔response, timeout+`revit.cancel`, reconnect backoff, map lỗi → `isError` / `McpException` |
| `ExecuteRevitCodeTool` (+3 tool), `RevitDocumentResources`, `RevitScriptPrompts` | `HPRebar.Mcp.Server/Tools|Resources|Prompts/` | Bề mặt MCP; validation kích thước/shape |
| `PipeListener`, `RequestDispatcher`, `IRevitExecutor` | `HPRebar.McpBridge.Core/Pipe/` | Listener 1 instance, ACL user, `-32001` disabled / `-32002` busy / `-32003` no doc |
| `ScriptGuard`, `ScriptCompiler`, `ScriptCache`, `TypeInspector`, `ScriptArgs`, `ScriptAnalyzer` | `HPRebar.McpBridge.Core/Scripting/` | Deny-list syntax walker, Roslyn + `InteractiveAssemblyLoader.RegisterDependency`, LRU 50, reflection Revit API; `args` global typed; literal/args-key walker cho `revit.analyze` |
| `ToolLibraryStore`, `ToolRegistryDb`, `ToolManager`, `DynamicToolRegistrar`, `ToolValidator`, `ToolLifecycleService`, `SeedInstaller`, `RegistryCli` | `HPRebar.Mcp.Server/Registry/` | Files (`tools-library/<Category>/<name>/`) = sự thật; SQLite WAL+FTS5 = chỉ mục + `runs`; tool published → `McpServerTool` từ `AIFunction` trong `ToolCollection` (`list_changed`); validate → propose → test (dryRun) → publish gate → approve (CLI) → quarantine tự động |
| `ToolRegistryQueryTools`, `RunToolTool`, `ToolLifecycleTools`, `RunHistoryTools`, `ToolifyPrompts`, `ToolRegistryResources` | `HPRebar.Mcp.Server/Tools/Registry|Prompts|Resources/` | `search_tools` · `get_tool` · `run_tool` · `get_run` · `propose_tool` · `test_tool` · `publish_tool` · `manage_tool`; prompt `toolify_run`; `registry://tools[/{name}]` |
| `BridgeSettings`, `BridgeSettingsStore`, `AuditLogger` | `HPRebar.McpBridge.Core/Model/` | Opt-in không persist; audit JSON-lines `%AppData%\HPRebar\McpBridge\audit\` |
| `McpBridgeExternalEventHandler`, `Service/ScriptRunner.cs`, `ResultSerializer`, `RevitContextReader` | `HPRebar.McpBridge/` | Chỉ phần này chạm Revit API |
| `McpBridgeHost`, `McpBridgeCommand`, `View/McpBridgeStatusView`, `ViewModel/McpBridgeStatusViewModel` | `HPRebar.McpBridge/` | State machine + cửa sổ modeless (opt-in, listener, last run) |

Chính sách transaction: `auto` (bridge mở Transaction trong Group) · `manual` (script tự mở) · `none` (chỉ đọc) · `dryRun` luôn rollback. Timeout 5–120 s cooperative qua `ct`; hết hạn = thất bại + rollback kể cả khi script `return`. Không sandbox — 9 lớp phòng thủ (ADR-04).

Client: `.mcp.json` (untracked) entry `hprebar-revit` → `HPRebar/output/HPRebar.Mcp.Server/HPRebar.Mcp.Server.exe` (từ `dotnet publish … -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true`). Bridge cần bật listener (auto-start theo `settings.json`) và tick "Allow AI code execution" mỗi phiên Revit.

Registry (ADR-05/06): tool = `tool.json + code.cs + examples.json` ở `%AppData%\HPRebar\McpServer\tools-library\` (đổi qua `Registry:LibraryPath`); `registry.db` giữ lịch sử chạy + độ ổn định; policy `manual` — AI dừng ở `pending_approval` + `_review/<name>.md`, người duyệt bằng `HPRebar.Mcp.Server.exe registry approve <name> --by <ai>`; server đang chạy nhận thay đổi qua watcher (không restart). Đã verify live 3 kịch bản ngày 2026-09-12 (`plans/…/reports/phase-09-live-verify.md`).
