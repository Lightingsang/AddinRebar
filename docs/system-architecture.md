# System Architecture — Revit Add-In (Nice3point Stack)

> **Phần 1–N dưới đây mô tả cấu trúc *mặc định* của template Nice3point (`dotnet new revit-addin`, DI mode `container`).
> Đó KHÔNG phải cấu trúc thật của HPRebar** — HPRebar chưa nối DI container, và tên file trong sơ đồ
> (`MyAddIn`, `WallReportView`) chỉ là ví dụ của template. Xem **mục cuối — "Column Rebar (thực tế)"** cho
> kiến trúc đang chạy, và `docs/codebase-summary.md` cho bản đồ file đầy đủ.

## 1. High-Level Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                       Revit Process                              │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │              Add-In: MyAddIn.dll (loaded by Revit)         │  │
│  │                                                            │  │
│  │  ┌──────────────────┐                                      │  │
│  │  │  Application.cs  │  ← Kế thừa ExternalApplication       │  │
│  │  │  OnStartupAsync()│    1. Setup Serilog                  │  │
│  │  │                  │    2. Build DI container             │  │
│  │  │                  │    3. CreateRibbon()                 │  │
│  │  └────┬─────────────┘                                      │  │
│  │       │                                                    │  │
│  │       ↓ (User click button)                                │  │
│  │  ┌────────────────────────────┐                            │  │
│  │  │  StartupCommand.cs         │  ← ExternalCommand         │  │
│  │  │  Execute()                 │    [Transaction(Manual)]   │  │
│  │  │  ├─ Resolve View qua DI    │                            │  │
│  │  │  └─ view.ShowDialog()      │                            │  │
│  │  └────┬───────────────────────┘                            │  │
│  │       │                                                    │  │
│  │       ↓                                                    │  │
│  │  ┌────────────────────────────────────────────────┐        │  │
│  │  │  Views/WallReportView.xaml                     │        │  │
│  │  │  ├─ Merge Theme.xaml (Dark + Light)            │        │  │
│  │  │  ├─ DataContext = ViewModel (DI inject)        │        │  │
│  │  │  └─ Bind {DynamicResource Brush.X}             │        │  │
│  │  └────┬───────────────────────────────────────────┘        │  │
│  │       │                                                    │  │
│  │       ↓                                                    │  │
│  │  ┌────────────────────────────────────────────────┐        │  │
│  │  │  ViewModels/WallReportViewModel.cs             │        │  │
│  │  │  sealed partial class : ObservableObject       │        │  │
│  │  │  ├─ [ObservableProperty] string _searchText    │        │  │
│  │  │  ├─ [RelayCommand] async Task RunAsync(token)  │        │  │
│  │  │  └─ ctor inject ILogger<T> + IWallService      │        │  │
│  │  └────┬───────────────────────────────────────────┘        │  │
│  │       │                                                    │  │
│  │       ↓                                                    │  │
│  │  ┌────────────────────────────────────────────────┐        │  │
│  │  │  Services/WallService.cs                       │        │  │
│  │  │  ├─ Truy cập Revit API (FilteredElementCollector)│       │  │
│  │  │  └─ Wrap Transaction nếu modify document       │        │  │
│  │  └────────────────────────────────────────────────┘        │  │
│  └────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────┘
```

## 2. Folder Structure (Standard)

```
MyAddIn/
├── Application.cs                      ← Entry point (Revit gọi đầu tiên)
├── MyAddIn.csproj                      ← Nice3point.Revit.Sdk + config
├── MyAddIn.addin                       ← Revit manifest XML
├── launchSettings.json                 ← F5 → Revit.exe path
│
├── Commands/                           ← External Commands (button handlers)
│   ├── StartupCommand.cs
│   └── ExportReportCommand.cs
│
├── Configuration/                      ← DI + Logger setup
│   ├── HostingConfiguration.cs         ← services.Add... registration
│   └── LoggerConfiguration.cs          ← Serilog setup
│
├── ViewModels/                         ← MVVM ViewModels (CommunityToolkit.Mvvm)
│   ├── WallReportViewModel.cs
│   └── SettingsViewModel.cs
│
├── Views/                              ← WPF Views (XAML + minimal code-behind)
│   ├── WallReportView.xaml
│   ├── WallReportView.xaml.cs
│   ├── SettingsView.xaml
│   └── SettingsView.xaml.cs
│
├── Models/                             ← POCO / DTO (no Revit dep, test-friendly)
│   ├── WallInfo.cs
│   └── ReportSettings.cs
│
├── Services/                           ← Business logic
│   ├── IWallService.cs                 ← Interface (for DI + testing)
│   ├── WallService.cs                  ← Revit API access
│   └── ReportExporter.cs               ← Xuất Excel/PDF (qua document-skills)
│
├── Helpers/                            ← Multi-version compat shims
│   ├── ElementIdHelper.cs
│   └── UnitConverter.cs
│
└── Resources/
    ├── Icons/
    │   ├── RibbonIcon16.png            ← Small icon (16x16)
    │   └── RibbonIcon32.png            ← Large icon (32x32)
    └── Themes/
        ├── Theme.xaml                  ← Master ResourceDictionary
        ├── ThemeDark.xaml              ← Dark color palette
        ├── ThemeLight.xaml             ← Light color palette
        ├── Typography.xaml             ← Font tokens
        ├── Spacing.xaml                ← Thickness tokens
        ├── Buttons.xaml                ← Button styles
        ├── TextBoxes.xaml              ← TextBox styles
        └── Controls.xaml               ← Card, Separator, Badge
```

## 3. DI Container (mode `container`)

`Configuration/HostingConfiguration.cs`:

```csharp
public static class HostingConfiguration
{
    private static IServiceProvider? _provider;

    public static IServiceProvider Provider => _provider
        ?? throw new InvalidOperationException("DI container not initialized");

    public static void Setup()
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(b => b.AddSerilog());

        // Services (singleton stateless, transient stateful)
        services.AddSingleton<IWallService, WallService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddTransient<IReportExporter, ReportExporter>();

        // ViewModels (transient — new instance per dialog open)
        services.AddTransient<WallReportViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Views (transient — inject ViewModel via constructor)
        services.AddTransient<WallReportView>();
        services.AddTransient<SettingsView>();

        _provider = services.BuildServiceProvider();
    }
}
```

## 4. Multi-version Strategy (Revit 2022–2027)

`.csproj` configs:
```xml
<Configurations>Debug.R22;Debug.R23;Debug.R24;Debug.R25;Debug.R26;Debug.R27</Configurations>
<Configurations>$(Configurations);Release.R22;Release.R23;Release.R24;Release.R25;Release.R26;Release.R27</Configurations>
```

Target framework auto-switch:
- R22–R24 → `net48`
- R25–R27 → `net8.0-windows`

Code branching:
```csharp
#if REVIT2024_OR_GREATER
    long id = elementId.Value;
#else
    int id = elementId.IntegerValue;
#endif
```

Chi tiết: `.claude/skills/revit-addin/references/multi-version-strategy.md`.

## 5. Modal vs Modeless

| Pattern | When | Implementation |
|---|---|---|
| Modal | Dialog ngắn (< 30s), block Revit UI | `view.ShowDialog()` + set `Owner = UiApplication.MainWindowHandle` |
| Modeless | Panel/picker, user vẫn tương tác Revit | `view.Show()` + `ExternalEvent.Create(handler)` cho Revit API call |

## 6. Theme Switch Runtime

`Services/ThemeService.cs` swap MergedDictionary:
```csharp
var uri = theme == AppTheme.Dark
    ? new Uri("pabs://application:,,,/MyAddIn;component/Resources/Themes/ThemeDark.xaml")
    : new Uri("pabs://application:,,,/MyAddIn;component/Resources/Themes/ThemeLight.xaml");
// Replace dict trong Application.Current.Resources.MergedDictionaries
```

Mọi binding `{DynamicResource Brush.X}` tự refresh khi swap.

## 7. Deploy Pipeline

| Stage | Tool | Output |
|---|---|---|
| Build Debug | `dotnet build -c Debug.R<XX>` (F5) | DLL auto-deploy vào `%ProgramData%\Autodesk\Revit\Addins\<version>\` |
| Build Release | `dotnet build -c Release.R<XX>` | DLL trong `bin/Release.R<XX>/` |
| ILRepack | Auto khi `<IsRepackable>true</IsRepackable>` | Single merged DLL |
| Installer | `revit-solution` template (WixSharp) | `.msi` |
| Autodesk Store | `revit-solution` template | Bundle folder + PackageContents.xml |

## 8. Logging Flow

```
Revit event / User click button
        ↓
Command.Execute() → Log.Information("...")
        ↓
ViewModel logic → _logger.LogDebug(...)
        ↓
Service Revit API → _logger.LogInformation(...)
        ↓
Serilog File sink
        ↓
%LocalAppData%\MyAddIn\logs\addin-YYYY-MM-DD.log
```

Setup: `Configuration/LoggerConfiguration.cs` (Nice3point template sinh sẵn).

## 9. Stack Reference

| Component | Version | Source |
|---|---|---|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com |
| Nice3point.Revit.Sdk | latest | NuGet |
| Nice3point.Revit.Toolkit | `$(RevitVersion).*` | NuGet |
| CommunityToolkit.Mvvm | 8.4+ | NuGet |
| Serilog | 4.3+ | NuGet |
| Microsoft.Extensions.DependencyInjection | latest stable | NuGet |
| TUnit (test) | latest | NuGet (Nice3point `revit-test` template) |
| xUnit (pure logic) | latest | NuGet |

## 10. Skill / Tool Map

| Khi cần | Skill |
|---|---|
| Scaffold mới | `/bs:revit-addin` |
| Sửa ViewModel/View | `/bs:revit-wpf-mvvm` |
| Sửa XAML style | `/bs:revit-xaml-styles` |
| Debug F5 / runtime issue | `/bs:revit-debug` |
| Setup / chạy test | `/bs:revit-test` |
| Plan feature mới | `/bs:plan` (Stack-Aware 6-phase) |
| Implement plan | `/bs:cook` (build verify gate) |


---

# Column Rebar — kiến trúc thực tế

Cập nhật 2026-09-04. Đây là feature duy nhất hiện có, và là bản mẫu cho convention feature-folder.

## Tách Core ↔ Revit

Ràng buộc gốc: `Autodesk.Revit.DB.Document` là `sealed` → không mock được → mọi thứ chạm Revit API đều không unit-test được. Nên toán tách hẳn ra project riêng.

```
┌─────────────────────────────────────────┐   ┌──────────────────────────────┐
│ HPRebar.Core  (netstandard2.0)          │   │ HPRebar  (net48/net8/net10)  │
│ KHÔNG reference Autodesk.Revit.*        │◄──┤ Toàn bộ code chạm Revit API  │
│ Đơn vị: millimét                        │   │ Đơn vị: feet (nội bộ Revit)  │
│                                         │   │                              │
│ BarLayoutCalculator                     │   │ ColumnStackReader   ──┐      │
│ SpliceCalculator                        │   │ ColumnStackValidator  │      │
│ BarPolylineBuilder                      │   │ RebarCreationService  │ mm ↔ ft
│ StirrupDistributionCalculator           │   │ DimensionCreator      │  qua  │
│ BarScheduleCalculator                   │   │ PointMapper         ──┘ RevitUnits
│ CanvasScaleCalculator                   │   │                              │
│                                         │   │ ColumnRebarOrchestrator      │
│ 99 test xUnit — chạy không cần Revit    │   │ 16 test TUnit — cần Revit    │
└─────────────────────────────────────────┘   └──────────────────────────────┘
                                                ILRepack merge Core vào 1 DLL
```

Chỉ **2 điểm** đổi đơn vị: `RevitUnits.MmToFt` / `FtToMm`. Đọc model quy về mm ngay tại `ColumnStackReader`; ghi ngược ra feet tại `PointMapper` và `StirrupGeometry`.

## Pipeline

```
ColumnRebarCommand.Execute                    (không mở transaction nào)
 │
 ├─ PickObjects + StructuralColumnSelectionFilter
 ├─ sort theo cao độ mặt đáy
 ├─ ColumnStackValidator.Validate ──► 14 rule, trả lỗi ĐẦU TIÊN gặp phải
 ├─ ColumnStackReader.Read        ──► ColumnStack { ColumnSection[] mm, ColumnFaces[] Revit }
 ├─ DefaultRebarSpecBuilder.Build ──► spec mặc định
 │
 └─ ColumnRebarView.ShowDialog()   (modal, Owner = Revit main window)
      │
      ├─ Cancel ──► không gọi Run ──► không có gì để rollback
      │
      └─ OK ──► RevitRebarRunner ──► ColumnRebarOrchestrator.Run
                                       │
                                       │  ★ NƠI DUY NHẤT mở TransactionGroup
                                       │
                                       ├─ Tx "Create Detail View"       2 mặt đứng
                                       ├─ Tx "Create Section View"      1 mặt cắt / đoạn cột
                                       ├─ Tx "Create Dimension View"    dim mặt đứng
                                       ├─ Tx "Create Dimension Section" dim mặt cắt
                                       ├─ Tx "Create Stirrup Bars"      đai + đai phụ
                                       ├─ Tx "Create Main Bars"         thép chủ
                                       ├─ Tx "Create Tag Bars"          bảng thống kê
                                       │
                                       ├─ thành công ──► Assimilate()  → undo 1 bước
                                       └─ lỗi        ──► RollBack()    → model nguyên vẹn
```

**Quyết định D8:** `new TransactionGroup` xuất hiện **đúng 1 lần** trong toàn feature (`ColumnRebarOrchestrator.cs`). Các service chỉ mở `Transaction`. Nhờ vậy orchestrator test được độc lập, undo gọn 1 bước, và Cancel không cần cơ chế rollback riêng.

## UI

```
ColumnRebarView (Window)
 ├─ ListBox nav ──────► 8 tab, DataTemplate theo type ViewModel
 ├─ ContentControl ───► SelectedTab
 ├─ ColumnElevationCanvas ──┐
 └─ footer: progress, OK/Cancel, EN⇄VN
                            │
        ┌───────────────────┴────────────────────┐
        │        ColumnRebarSession               │  state dùng chung
        │  ColumnStack + ColumnSpecEditor[]       │  mọi tab đọc/ghi vào đây
        │  SelectedColumnIndex                    │  tab không nói chuyện với nhau
        └───────────────────┬────────────────────┘
                            │ PropertyChanged
                            ▼
        Canvas gọi ĐÚNG calculator mà service tạo thép dùng
        ⇒ preview và kết quả không thể lệch nhau
```

Điểm này khác bản gốc có chủ đích: `R01_ColumnsRebar` có 2 đường code song song (`DrawStirrupItemRectangle0` tính lại số đai bằng tay, tách rời `CreateStirrupTypeItem1`), nên preview có thể lệch model.

Code-behind chỉ `InitializeComponent` + `DataContext` (+ theme và dialog-result ở shell). Không `x:Name`, không `DataContext` trong XAML, không `Canvas.Children` từ ViewModel.

## Theme + i18n

- `Resources/Themes/` 8 file. Mọi màu/spacing/font qua `{DynamicResource}`.
- `ThemeSwitcher.ApplyFromRevit` đọc `UIThemeManager` (R24+) rồi swap dictionary màu. R23 không có API → mặc định Dark.
- Canvas lấy màu qua `CanvasPalette.From(element)` → `TryFindResource`, thiếu key thì fallback + log warning, không throw.
- `UiStrings` là record ~110 field; đổi ngôn ngữ = thay cả record một lần → mọi binding refresh, dialog không đóng.

## Multi-version

Chỉ **2 block `#if`**, cả hai có comment `// Multi-version:`:

| Vị trí | Vấn đề |
|---|---|
| `MainBarCreator` | `Rebar.CreateFreeForm` — overload `out RebarFreeFormValidationResult` bị xóa ở R27; overload `RebarStyle` chỉ có từ R26; khác cả return type |
| `ThemeSwitcher` | `UIThemeManager` chỉ có từ R24 |

Đối chiếu đầy đủ 106 type API: `plans/260903-2307-port-column-rebar-to-hprebar/reports/api-surface-check.md`.

## Điểm giòn đã biết

`DimensionCreator.ToLinearReference` đổi token `SURFACE` → `LINEAR` trong stable representation của `PlanarFace.Reference`. Revit chỉ nhận reference LINEAR khi dimension trong section view, nhưng face trả về SURFACE. Cách này **undocumented**, có thể vỡ ở version mới. Cô lập trong 1 method; mọi caller `try/catch` → log warning và bỏ qua dimension, không làm hỏng cốt thép vừa dựng.

## MCP Bridge (thực tế, 2026-09-14)

Kiến trúc thứ hai, độc lập với add-in rebar: **hai tiến trình, một engine host-neutral**.

```mermaid
flowchart LR
    AI["AI agent<br/>(Claude Code)"] -- "stdio · JSON-RPC 2.0" --> S["HPRebar.Mcp.Server (net10)<br/>thin exe + 21 seed + Registry<br/>(McpShared projects)"]
    S -- "Named Pipe hprebar-mcp-r2026<br/>JSON-RPC 2.0, 1 object/dòng" --> L["PipeListener + RequestDispatcher<br/>(McpShared/HPRebar.McpBridge.Core)"]
    L --> G["ScriptGuard → ScriptCompiler<br/>(Roslyn, pipe thread, cache)"]
    G --> H["McpBridgeExternalEventHandler<br/>(HPRebar.McpBridge, ExternalEvent → Revit API thread)"]
    H --> R["ScriptRunner<br/>TransactionGroup 'MCP: label' → Revit 2026"]
    R --> A["AuditLogger · LastRun"]
    S --- REG["Registry Engine<br/>(McpShared/HPRebar.Mcp.Server.Core)<br/>files + SQLite/FTS5 + FileSystemWatcher"]
    REG -- "tool published → ToolCollection.Add → list_changed" --> S
```

Vì sao hai tiến trình (ADR-01): host AI phải launch MCP server stdio làm child — `Revit.exe` không thể là child đó. Server không reference Revit; bridge không reference MCP SDK. Cầu nối: `HPRebar.Mcp.Contracts` (netstandard2.0, hợp đồng dây).

Engine host-neutral ở `McpShared/`: `PipeListener`, `ScriptGuard`, `ScriptCompiler`, registry engine chạy không cần Revit. Test xUnit với fake executor qua pipe thật. Chỉ phần Revit-specific (`ExternalEventHandler`, `ScriptRunner`) nằm trong `HPRebar.McpBridge/`.

**Tầng registry (ADR-05/06):** tool cố định + tool AI tự sinh đều là *dữ liệu* (`tool.json + code.cs + examples.json`), chạy qua `revit.execute` với `args`. Không native command nào trong bridge. Vòng đời: `draft → tested → pending_approval → published → quarantined | deprecated`. Approve = CLI `registry approve` theo policy `manual`. Độ ổn định từ `runs`; tự quarantine nếu ≥ 5 run và > 40% thất bại. `FileSystemWatcher` reload files nên approve không cần restart.

Điểm giòn: Roslyn assembly không unload; timeout chỉ cooperative; Revit hỏi "publisher could not be verified" với DLL chưa ký. Chi tiết: `plans/260912-1521-dynamic-revit-mcp-server-2026/adr/`.

## AutoCAD MCP Bridge (Phase 1, 2026-09-14)

Cùng kiến trúc với Revit nhưng cho AutoCAD 2026 (R25.1, .NET 8). Đã verify phase 1 (loader, ALC, bundle, spike) sống động; phase 2–5 tiếp theo.

```
AI ──stdio──▶ HPAutoCad.Mcp.Server (phase 3)
              ↓
            Named pipe hpautocad-mcp-2026 (phase 2+)
              ↓
            HPAutoCad.McpBridge.Loader ──reflection──▶ BridgeLoadContext ("HPAutoCad.McpBridge")
                                                       │
                                                       └─ HPAutoCad.McpBridge (Roslyn + pipe listener, phase 2+)
                                                          ↓
                                                       acad.exe (main thread)
                                                       ↓
                                                       Idle tick → ExternalEvent → TransactionGroup → AutoCAD API
```

**Load context (ALC) tách riêng:** AutoCAD tải Roslyn 4.10 + Immutable 8.0 ở default context; bridge cần 5.9 + 10 → private deps.json qua `AssemblyDependencyResolver` (verified: Roslyn/Immutable ở "HPAutoCad.McpBridge", AcMgd/AcCoreMgd/AcDbMgd ở Default). Phase 1 nhanh compile (~1 s), main thread qua Idle + `LockDocument` + transaction commit (~0.9 s).

**Security mode:** env var `HPAUTOCAD_MCP_SPIKE=1` bật spike (phase 1 test); chưa có listener. Trên máy người dùng: SECURELOAD prompt "publisher could not be verified" → *Always Load* (same as Revit).

**Bundle:** `PackageContents.xml` SchemaVersion 1.0, platform AutoCAD, R25.1, 24 file / 14 MB. Deploy → `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.McpBridge.bundle\` (Debug build tự động).

**Mục tiêu phase 2–5:** pipe listener + main-thread executor (phase 2), server + tools (phase 3), tests (phase 4), multi-version R26/R27 (phase 5).
