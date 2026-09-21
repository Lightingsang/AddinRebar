# Project: Smart Plot Pro

## Architecture
Smart Plot Pro is an automated batch plotting and PDF publishing tool integrated into the HPAutoCad ecosystem (AutoCAD 2026, .NET 8).
The architecture strictly enforces clean separation between host-free logic, AutoCAD-dependent plotting, PDF merging, WPF MVVM UI, and ALC Loader isolation:

```
┌─────────────────────────────────────────────────────────────┐
│                       HPAutoCad.Loader                       │  (Default ALC)
│  - SmartPlotCommands.cs: [CommandMethod("HPSMARTPLOT", "HPLOT")]
│  - HPPlotRibbonTab.cs: Ribbon Panel "Plot", button "Smart Plot Pro"
│  - RibbonIcons.cs: Dynamic vector icon (Dark / Light ink aware)
│  - AppLoadContext: Isolated child ALC loading Contents\App\
└──────────────────────────────┬──────────────────────────────┘
                               │ delegates via Entry.Start
┌──────────────────────────────▼──────────────────────────────┐
│                          HPAutoCad                          │  (Child ALC: HPAutoCad.App)
│  - SmartPlot/UI/: SmartPlotWindow (Modeless), SmartPlotViewModel
│  - Theming: AutocadHostTheme, MaterialBridge.xaml, RepackMaterialDesign
│  - SmartPlot/Cad/: AutoCadPlotEngine, Block/Layer/Layout FrameProviders
│  - SmartPlot/Pdf/: PdfMergeService (PDFsharp 6.1.1 in-process merge)
└──────────────────────────────┬──────────────────────────────┘
                               │ references pure domain
┌──────────────────────────────▼──────────────────────────────┐
│                        HPAutoCad.Core                       │  (Pure .NET 8, Zero Host API)
│  - SmartPlot/Models/: PlotItem, PlotConfiguration, PlotPreset, PlotResult
│  - SmartPlot/Services/: PlotOrderService (overlap band), LayoutRangeParser,
│                         FileNameService, PresetService (%AppData%)
└─────────────────────────────────────────────────────────────┘
                               ▲
                               │ tested by
┌──────────────────────────────┴──────────────────────────────┐
│                        HPAutoCad.Tests                      │  (.NET 10 / xUnit v3 / MTP)
│  - SmartPlot/: PlotOrderServiceTests, LayoutRangeParserTests,
│                FileNameServiceTests, PresetServiceTests
└─────────────────────────────────────────────────────────────┘
```

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Domain Models & Enums | Pure records and enums: `FrameSourceType`, `OutputMode`, `OrientationMode`, `PlotBounds`, `PlotItem`, `PlotConfiguration`, `PlotPreset`, `PlotPresetCollection`, `PlotResult` | M1 | ORIGINAL_REQUEST §R1 |
| 2 | Spatial Order Service | `IPlotOrderService` / `PlotOrderService`: Zic-zac row clustering with dynamic vertical overlap band (default 50%) and left-to-right sorting | M1 | ORIGINAL_REQUEST §R1 |
| 3 | Layout Range Parser | `LayoutRangeParser`: Safe range string parsing ("All", "1-5", "1,3,5", "1-3,5,8-10") with zero exceptions on invalid inputs | M1 | ORIGINAL_REQUEST §R1 |
| 4 | File Name Service | `IFileNameService` / `FileNameService`: Path sanitization via `Path.GetInvalidFileNameChars()`, token replacements (`{Prefix}_{Layout}_{Title}_{SheetNo}`) | M1 | ORIGINAL_REQUEST §R1 |
| 5 | Preset Storage Service | `IPresetService` / `PresetService`: JSON serialization to `%AppData%\HPAutoCad\SmartPlot\presets.json` with fallback default presets | M1 | ORIGINAL_REQUEST §R1 |
| 6 | Core Unit Test Suite | Comprehensive xUnit v3 unit tests for order, range, filename, and preset services in `HPAutoCad.Tests/SmartPlot/` | M1 | ORIGINAL_REQUEST §Verification |
| 7 | PDFsharp Package & Merge Service | Package `PDFsharp` 6.1.1 in `HPAutoCad.csproj` and implement `IPdfMergeService` / `PdfMergeService` for in-process PDF merging and temp cleanup | M2 | ORIGINAL_REQUEST §R3 |
| 8 | Block Frame Provider | `BlockFrameProvider`: Scans `BlockReference` for `EffectiveName` (dynamic blocks) and extracts title block attributes (`Tag`, `TextString`) | M2 | ORIGINAL_REQUEST §R2 |
| 9 | Layer Frame Provider | `LayerFrameProvider`: Scans closed `Polyline` entities on specified layer and extracts bounding box | M2 | ORIGINAL_REQUEST §R2 |
| 10 | Layout Frame Provider | `LayoutFrameProvider`: Queries PaperSpace layouts by `TabOrder` filtered by `LayoutRangeParser` | M2 | ORIGINAL_REQUEST §R2 |
| 11 | AutoCAD Plot Engine | `AutoCadPlotEngine`: Executes pipeline (`PlotSettingsValidator`, `PlotInfoValidator`, `PlotFactory.CreatePublishEngine()`, `PlotEngine`), Window mode, auto orientation, fit/center, system variables (`BACKGROUNDPLOT=0`, `CMDECHO=0`) in try/finally, and `PlotProgressDialog` with cancellation | M2 | ORIGINAL_REQUEST §R2 |
| 12 | Modeless SmartPlotWindow | `SmartPlotWindow.xaml`: Modeless window via `Application.ShowModelessWindow()`, tabs, cards, progress bar, viewport pan/zoom support | M3 | ORIGINAL_REQUEST §R4 |
| 13 | Dynamic Theme Sync | Automatic synchronization with AutoCAD's `COLORTHEME` (55 vs 245) via `AutocadHostTheme.Instance` + `MaterialBridge.xaml` | M3 | ORIGINAL_REQUEST §R4 |
| 14 | SmartPlotViewModel | MVVM ViewModel with `CommunityToolkit.Mvvm`: state management, frame collection, plot commands, pick frame interaction, cancellation | M3 | ORIGINAL_REQUEST §R4 |
| 15 | Assembly Packaging Check | Ensure `RepackMaterialDesign` ILRepack target merges toolkit into `HPAutoCad.dll` with 0 loose `MaterialDesignThemes.Wpf.dll`, keeping `PdfSharp.dll` clean | M3 | ORIGINAL_REQUEST §R4 |
| 16 | Command Registration | Register `[CommandMethod("HPSMARTPLOT")]` and `[CommandMethod("HPLOT")]` in `HPAutoCad.Loader` and wire to `HPAutoCad.Entry.Start` | M4 | ORIGINAL_REQUEST §R5 |
| 17 | Ribbon Integration | Add "Plot" panel with "Smart Plot Pro" button to shared tab `HPAUTOCAD_MCP_TAB`, vector theme-aware icon in `RibbonIcons.cs` | M4 | ORIGINAL_REQUEST §R5 |
| 18 | Full Verification & Packaging Audit | Full solution compilation (`dotnet build HPAutoCad/HPAutoCad.slnx -c Debug`), unit test pass (`dotnet test HPAutoCad.Tests`), and bundle sanity checks | M4 | ORIGINAL_REQUEST §Acceptance Criteria |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Pure Logic Engine & Unit Tests | Models, Enums, `PlotOrderService`, `LayoutRangeParser`, `FileNameService`, `PresetService` in `HPAutoCad.Core/SmartPlot/`, and unit tests in `HPAutoCad.Tests/SmartPlot/` | none | DONE |
| M2 | AutoCAD Plot Engine & PDF Merge | `PDFsharp` package reference, `PdfMergeService`, `BlockFrameProvider`, `LayerFrameProvider`, `LayoutFrameProvider`, `AutoCadPlotEngine` in `HPAutoCad/SmartPlot/Cad/` & `Pdf/` | M1 | DONE |
| M3 | WPF MVVM UI & Theming | `SmartPlotWindow.xaml`, `SmartPlotViewModel`, theme sync via `AutocadHostTheme.Instance`, modeless lifecycle, pick frame interaction, repack verification | M1, M2 | DONE |
| M4 | Commands, Ribbon & Loader Integration | Command registration in `HPAutoCad.Loader`, ribbon panel in `HPAUTOCAD_MCP_TAB`, vector icon, full solution build & unit test execution | M1, M2, M3 | DONE |
| M5 | Final Verification & Victory Audit | Full solution compilation, test pass (415 tests), bundle verification, final forensic audit sign-off | M4 | DONE |

## Interface Contracts

### 1. `HPAutoCad.Core` ↔ `HPAutoCad`
- `PlotItem`:
  - `string Id`: Unique frame identifier.
  - `PlotBounds Bounds`: `(double MinX, double MinY, double MaxX, double MaxY)`.
  - `string LayoutName`: Layout name ("Model" or PaperSpace name).
  - `string DisplayName`: Human-friendly name.
  - `string SheetNumber`: Extracted attribute or index.
  - `string SheetTitle`: Extracted attribute or layout title.
  - `int Order`: Sequential order assigned by `IPlotOrderService`.
  - `double Rotation`: Frame rotation in degrees (0, 90, 180, 270).
  - `bool IsSelected`: Checked state in UI.
  - `string? OutputFileName`: Evaluated destination filename.
- `IPlotOrderService.Sort(IEnumerable<PlotItem> items, double toleranceRatio = 0.5) -> IReadOnlyList<PlotItem>`:
  - Clusters frames into visual rows using vertical overlap ratio $\ge 0.5$.
  - Sorts rows Top to Bottom by cluster CenterY descending.
  - Sorts frames within each row Left to Right by MinX ascending.
  - Re-indexes `Order` sequentially 1..N.
- `LayoutRangeParser.Parse(string? rangeText, int maxCount) -> IReadOnlyList<int>`:
  - Parses "All", "*", "1-5", "1,3,5", "1-3,5,8-10".
  - Clamps to `[1, maxCount]`. Never throws exceptions on invalid text.
- `IFileNameService.Format(string template, PlotItem item, string? prefix = null, string? dwgName = null) -> string`:
  - Replaces `{Prefix}`, `{Layout}`, `{Title}`, `{SheetNo}`, `{Order}`, `{DwgName}`, `{Date}`.
  - Sanitizes invalid characters via `Path.GetInvalidFileNameChars()`.
- `IPresetService.LoadPresetsAsync() -> Task<PlotPresetCollection>`, `SavePresetsAsync(PlotPresetCollection presets) -> Task`:
  - Reads and writes `%AppData%\HPAutoCad\SmartPlot\presets.json`.

### 2. `HPAutoCad/SmartPlot/Cad/` ↔ `HPAutoCad/SmartPlot/UI/`
- `IFrameProvider.ScanFramesAsync(FrameSourceType sourceType, FrameScanOptions options, CancellationToken ct) -> Task<IReadOnlyList<PlotItem>>`:
  - Extracted using `using (doc.LockDocument())`.
- `IAutoCadPlotEngine.PlotAsync(PlotConfiguration config, IReadOnlyList<PlotItem> items, IProgress<PlotProgressUpdate>? progress, CancellationToken ct) -> Task<PlotResult>`:
  - Executes AutoCAD plot pipeline using `PlotFactory.CreatePublishEngine()`.
  - Sets `OverrideSettings` on `PlotInfo` without dirtying drawing.
  - Restores `BACKGROUNDPLOT` and `CMDECHO` in `finally`.
  - Merges temp PDFs via `IPdfMergeService` if `config.OutputMode == OutputMode.MergedPdf`.

### 3. `HPAutoCad.Loader` ↔ `HPAutoCad`
- `HPAutoCad.Entry.Start(string appDir, Action<string> log)`:
  - Returns dictionary with `["smartplot"] = new Action(SmartPlotCommand.Run)`.
- `HPAutoCad.Loader/SmartPlotCommands.cs`:
  - Invokes `HPGeoCommands.Invoke("smartplot", "HPSMARTPLOT")`.

## Code Layout

```
HPAutoCad.Core/
└── SmartPlot/
    ├── Models/
    │   ├── FrameSourceType.cs
    │   ├── OutputMode.cs
    │   ├── OrientationMode.cs
    │   ├── PlotBounds.cs
    │   ├── PlotItem.cs
    │   ├── PlotConfiguration.cs
    │   ├── PlotPreset.cs
    │   ├── PlotPresetCollection.cs
    │   └── PlotResult.cs
    └── Services/
        ├── IPlotOrderService.cs
        ├── PlotOrderService.cs
        ├── LayoutRangeParser.cs
        ├── IFileNameService.cs
        ├── FileNameService.cs
        ├── IPresetService.cs
        └── PresetService.cs

HPAutoCad/
└── SmartPlot/
    ├── Cad/
    │   ├── Providers/
    │   │   ├── IFrameProvider.cs
    │   │   ├── FrameScanOptions.cs
    │   │   ├── BlockFrameProvider.cs
    │   │   ├── LayerFrameProvider.cs
    │   │   └── LayoutFrameProvider.cs
    │   └── Plot/
    │       ├── IAutoCadPlotEngine.cs
    │       ├── AutoCadPlotEngine.cs
    │       └── PlotProgressUpdate.cs
    ├── Pdf/
    │   ├── IPdfMergeService.cs
    │   └── PdfMergeService.cs
    ├── UI/
    │   ├── SmartPlotWindow.xaml
    │   ├── SmartPlotWindow.xaml.cs
    │   └── SmartPlotViewModel.cs
    └── Commands/
        └── SmartPlotCommand.cs

HPAutoCad.Loader/
├── Commands/
│   └── SmartPlotCommands.cs
└── Ribbon/
    └── HPPlotRibbonPanel.cs  (or integrated in Ribbon tabs)

HPAutoCad.Tests/
└── SmartPlot/
    ├── PlotOrderServiceTests.cs
    ├── LayoutRangeParserTests.cs
    ├── FileNameServiceTests.cs
    └── PresetServiceTests.cs
```
