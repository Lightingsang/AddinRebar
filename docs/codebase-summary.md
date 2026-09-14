# Codebase Summary — HPRebar + AutoCAD Bridge

Cập nhật: 2026-09-14

## Repository Layout

Gồm 4 unrelated deliverables (không cross-wire):

| Thư mục | Loại | Stack |
|---|---|---|
| `HPRebar/` | Revit add-in + server MCP | C# / Nice3point / WPF / xUnit / TUnit |
| `McpShared/` | Host-neutral MCP engine | netstandard2.0 · net8.0 · net10.0 |
| `HPAutoCad/` | AutoCAD add-in + server MCP (scaffold) | C# / AutoCAD.NET / Roslyn (kế thừa từ McpShared) |
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
| `HPRebar.Mcp.Server.Core.Tests/` | net10.0 | xUnit v3 — 70 test: host-neutrality, guard/compiler/args, HostProfile binding, registry store/db, pipe round-trip fake executor |

## HPAutoCad Solution

`HPAutoCad/HPAutoCad.slnx` + global.json. Reference McpShared only; không dùng HPRebar. Phases 1–2 (2026-09-14): loader + ALC + bundle + bridge runtime verified live.

| Project | TFM | Vai trò | Build? |
|---|---|---|---|
| `HPAutoCad.McpBridge.Loader/` | net8.0-windows | IExtensionApplication + HPMCP* commands (HPMCPBRIDGE/STATUS/START/STOP); tạo BridgeLoadContext, khởi động bridge bằng reflection | ✅ |
| `HPAutoCad.McpBridge/` | net8.0-windows, UseWPF | Phases 1–2: Roslyn + self-check; `MainThreadExecutor` (Idle + IsQuiescent + PostMessage WM_NULL), `AutocadScriptRunner` (outer/inner transaction via `doc.TransactionManager`, dryRun, timeout), `DatabaseChangeCounter` (HANDSEED + events), `AutocadContextReader`/`AutocadResultSerializer`, XAML status window (theme-merged, opt-in "Allow AI code execution") | ✅ |
| `HPAutoCad.Mcp.Server/` | net10.0 console | MCP server exe + AutocadHostProfile + tools/resources (phase 3) | ⏳ |
| `HPAutoCad.Mcp.Server.Tests/` | net10.0 | xUnit v3: profile, tools, seed compile check (phase 3–4) | ⏳ |

**Core additions (phase 2):** `MainThreadQueue.cs` (9 test), `BridgeRequestException.cs`, `AutocadInsunits.cs`, `GuardProfile.AutoCAD` + deny `StartTransaction/StartOpenCloseTransaction/TopTransaction/LockDocument`. Tests: McpShared 88/88, HPRebar MCP 106/106.

**Unattended harness & verification:** `HPAutoCad/tools/harness/` (Python + PowerShell) 21/21 scenarios: Idle wake, busy grace 8 s, timeout/cancel/none-document, dryRun, undo merging, SECURELOAD auto-accept, UI Automation opt-in, COM close/busy/REGEN. Run 2× independent (lead + tester) 21/21 ✅; zero `.NET Runtime 1026` crashes; audit 234+ lines.

**Runtime platform:** AutoCAD 2026 base release (R25.1, .NET 8) verified 2026-09-14.

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
dotnet test HPRebar.Mcp.Server.Tests            # 106 test xUnit — registry + 21 seed thực
dotnet build HPRebar.Tests/HPRebar.Tests.csproj -c Debug.R26   # TUnit, cần Revit

cd build && dotnet run                          # Release cả 5 config
cd build && dotnet run -- pack                  # Bundle → output/

# McpShared
cd McpShared
dotnet test HPRebar.Mcp.Server.Core.Tests       # 70 test xUnit — host-neutral engine

# HPAutoCad
cd HPAutoCad
dotnet build HPAutoCad.slnx -c Debug             # deploy bundle → %AppData%\Autodesk\ApplicationPlugins\
dotnet build HPAutoCad.slnx -c Debug -p:DeployBundle=false   # khi AutoCAD đang mở (DLL khóa)
dotnet test HPAutoCad.Mcp.Server.Tests          # (phase 3+)
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
| 2026 | ✅ | ✅ 2026-09-14 (phase 1 spike 5/5 ×3 run, unattended + SECURELOAD auto-click, exit 0) |
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
