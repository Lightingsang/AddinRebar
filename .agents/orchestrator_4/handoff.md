# Final Project Handoff Report: Smart Plot Pro (HPAutoCad)

- **Feature**: Smart Plot Pro — Automated Batch Plotting & PDF Publishing for AutoCAD 2026 (.NET 8)
- **Author**: `orchestrator_4` (Project Orchestrator)
- **Target Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\`
- **Authoritative Reference**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (## 2026-09-20T22:21:59Z)
- **Status**: **Hard Handoff (Project Complete — All Acceptance Criteria Satisfied)**
- **Audit Verdict**: **`CLEAN` (Forensic Integrity Victory Audit Passed with Zero Violations)**

---

## 1. Observation

### 1.1 Complete Architecture & Deliverables

#### Layer 1: Pure Logic Engine (`HPAutoCad.Core/SmartPlot/`)
- **Host Independence**: Targets pure `net8.0` with zero dependencies on `Autodesk.*`, WPF, or native libraries.
- **Models (`Models/`)**:
  - `FrameSourceType.cs`: `enum FrameSourceType { Block, Layer, Layout }`
  - `OutputMode.cs`: `enum OutputMode { SingleFiles, MergedPdf }`
  - `OrientationMode.cs`: `enum OrientationMode { Auto, Portrait, Landscape }`
  - `PlotBounds.cs`: `readonly record struct PlotBounds(double MinX, double MinY, double MaxX, double MaxY)` with computed `Width`, `Height`, `CenterX`, `CenterY`, `IsLandscape`, `IsValid`, `VerticalOverlap()`, and `OverlapsVertically(threshold)`.
  - `PlotItem.cs`: `sealed record PlotItem` containing unique `Id`, `Bounds`, `LayoutName`, `DisplayName`, `AttributeValue`, `SheetNumber`, `SheetTitle`, `Order`, `Rotation`, `IsSelected`, `SourceHandle`, `OutputFileName`, and bounds delegation.
  - `PlotConfiguration.cs`: Production configuration defaults (`AutoCAD PDF (General Documentation).pc3`, `ISO_full_bleed_A1`, `monochrome.ctb`, `Auto`, `SingleFiles`, `ToleranceBandYRatio = 0.5`).
  - `PlotPreset.cs` & `PlotPresetCollection.cs`: Serialization root models.
  - `PlotResult.cs`: Immutable execution result with `Succeeded` and `Failed` factory methods.
- **Services (`Services/`)**:
  - `IPlotOrderService.cs` & `PlotOrderService.cs`: Spatial ordering service that clusters visual rows using a dynamic vertical overlap tolerance ratio ($\ge 0.5$) and sorts rows Top-to-Bottom by cluster `CenterY` descending, then frames Left-to-Right by `MinX` ascending, re-indexing `Order` 1..N.
  - `LayoutRangeParser.cs`: Zero-exception robust parser for sheet/layout ranges ("All", "*", "1-5", "1,3,5", "1-3,5,8-10", inverted "5-1"). Uses `int.TryParse`, clamps to `[1, maxCount]`, skips malformed tokens, and bounds large inputs with `HardCap = 10000`.
  - `IFileNameService.cs` & `FileNameService.cs`: Formats file names with token replacement (`{Prefix}`, `{Layout}`, `{SheetNo}`, `{Title}`, `{Order}`, `{DwgName}`, `{Date}`), sanitizes invalid characters via `Path.GetInvalidFileNameChars()`, collapses consecutive underscores, and safely prefixes Windows reserved device names (`CON`, `PRN`, `AUX`, `NUL`, `COM1-9`, `LPT1-9`) and their extensions (e.g. `aux.pdf` -> `_aux.pdf`).
  - `IPresetService.cs` & `PresetService.cs`: Thread-safe JSON persistence at `%AppData%\HPAutoCad\SmartPlot\presets.json` using `System.Text.Json`. Synchronized with `FileLock`, unique GUID temporary files for atomic replacement, and default fallbacks ("A1 Monochrome PDF", "A1 Monochrome Merged", "A3 Color PDF").

#### Layer 2: AutoCAD Plot Engine & Frame Providers (`HPAutoCad/SmartPlot/Cad/`)
- **Frame Providers (`Cad/Providers/`)**:
  - `IFrameProvider.cs` & `FrameScanOptions.cs`: Scanning contracts and options.
  - `BlockFrameProvider.cs`: Scans `BlockReference` entities. Evaluates `blkRef.DynamicBlockTableRecord` to resolve the true persistent `EffectiveName` (avoiding anonymous `*U...` names), extracts title and sheet numbers from attributes with candidate fallbacks, extracts bounding boxes from `blkRef.GeometricExtents`, and applies spatial ordering.
  - `LayerFrameProvider.cs`: Scans closed `Polyline`, `Polyline2d`, and `Polyline3d` entities (`poly.Closed == true`) on the target layer and extracts 2D bounding boxes.
  - `LayoutFrameProvider.cs`: Enumerates PaperSpace layouts from `db.LayoutDictionaryId` sorted by `layout.TabOrder`, filtered by `LayoutRangeParser`.
  - `AutoCadFrameProvider.cs`: Unified dispatcher routing by `FrameSourceType`.
- **Plot Engine (`Cad/Plot/`)**:
  - `IAutoCadPlotEngine.cs` & `PlotProgressUpdate.cs`: Contracts and progress records.
  - `AutoCadPlotEngine.cs`: Checks `PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting`. Wraps CAD execution in `using var docLock = doc.LockDocument()` on the AutoCAD main UI thread. Saves and restores `BACKGROUNDPLOT = 0` and `CMDECHO = 0` in `finally`. Sets non-destructive `plotInfo.OverrideSettings = plotSettings`. Auto-detects orientation based on aspect ratio. Encloses plot sessions in `try/finally` guaranteeing `engine.EndPlot(null)` and `engine.Destroy()` are executed. Manages native `PlotProgressDialog` with cooperative cancellation (`ct.IsCancellationRequested` and `progressDialog.IsPlotCancelled`), and routes to `IPdfMergeService` in merged mode.

#### Layer 3: In-Process PDF Merging (`HPAutoCad/SmartPlot/Pdf/`)
- **Package**: `PDFsharp` 6.1.1 referenced in `HPAutoCad.csproj` and `HPAutoCad.Tests.csproj`.
- **Implementation**:
  - `IPdfMergeService.cs` & `PdfMergeService.cs`: In-process multi-page PDF merging using `PdfSharp.Pdf.PdfDocument` and `PdfReader.Open(file, PdfDocumentOpenMode.Import)`.
  - **Resilience**: Source files are deleted strictly when `mergeSucceeded == true` after `outputDocument.Save()` succeeds; destination self-deletion is prevented via canonical path comparison; transient locks are handled via `TryDeleteFileWithRetry`. Zero external CLI calls.

#### Layer 4: Modeless WPF MVVM UI & Theme Synchronization (`HPAutoCad/SmartPlot/UI/`)
- **`SmartPlotWindow.xaml` & `SmartPlotWindow.xaml.cs`**:
  - Modeless window displayed via `Application.ShowModelessWindow(window)` with single-instance enforcement.
  - Contextual ALC reflection with `ThemeResources.Styles()` and dynamic dark/light synchronization via `MaterialThemeBridge.Attach(this, AutocadHostTheme.Instance)`.
  - Interactive "Pick Frame" minimizes window (`WindowState = Minimized`), acquires `using (doc.LockDocument())`, prompts user via `ed.GetEntity(peo)` to select a block or polyline, and restores/activates the window.
- **`SmartPlotViewModel.cs`**:
  - Built with `CommunityToolkit.Mvvm` (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`).
  - Manages reactive state, observable collections (`Items`, `Devices`, `MediaSizes`, `PlotStyles`, `AvailableBlocks`, `AvailableLayers`, `Presets`), commands (`ScanFramesCommand`, `PlotSelectedCommand`, `PlotAllCommand`, `PickFrameCommand`, preset commands), cooperative cancellation, and progress reporting.
- **`PlotItemViewModel.cs` & `SmartPlotConverters.cs`**:
  - DataGrid item model and value converters (`EnumToBooleanConverter`, `BoolToVisibilityConverter`, `EnumToVisibilityConverter`).

#### Layer 5: Commands & Ribbon Integration (`HPAutoCad.Loader/` & `Entry.cs`)
- **Commands**:
  - `Commands/SmartPlotCommands.cs`: Declares `[CommandMethod("HPSMARTPLOT", CommandFlags.Modal | CommandFlags.Session)]` and `[CommandMethod("HPLOT", CommandFlags.Modal | CommandFlags.Session)]`, delegating via `HPGeoCommands.Invoke("smartplot", "HPSMARTPLOT")`.
  - `Entry.cs`: Registers `["smartplot"] = new Action(SmartPlotCommand.Run)` in `HPAutoCad.Entry.Start`.
- **Ribbon**:
  - `Ribbon/SmartPlotRibbonPanel.cs`: Panel `HPPLOT_PANEL` ("Plot") with large button "Smart Plot Pro" (`HPSMARTPLOT_BUTTON`), mounted on shared tab `HPAUTOCAD_MCP_TAB` ("HPAutoCad").
  - `Ribbon/RibbonIcons.cs`: Resolution-independent 32×32 vector icon `Plot` (printer body, outbound paper fold, laser/plot accent line, and status LED) with dynamic ink adapting to `COLORTHEME`.
  - `HPAutoCadLoaderApplication.cs`: Hooks `SmartPlotRibbonPanel.Install()` on startup and `SmartPlotRibbonPanel.Uninstall()` on shutdown.

#### Layer 6: Packaging Isolation & Bundle Deployment
- **`RepackMaterialDesign` Target**:
  - Merges `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, and `Microsoft.Xaml.Behaviors.dll` directly into `HPAutoCad.dll` (size: 10,774,016 bytes ~10.77 MB).
  - Deletes loose `MaterialDesignThemes.Wpf.dll` to prevent cross-ALC BAML collisions in AutoCAD.
- **Bundle Directory**: `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\Contents\App\`:
  - `HPAutoCad.dll` contains merged MaterialDesignThemes.
  - `PdfSharp.dll` (761,856 bytes) is deployed as a clean loose assembly, loaded dynamically into `HPAutoCad.Loader.AppLoadContext`.
  - 0 loose `MaterialDesignThemes.Wpf.dll` files present.

---

## 2. Test Execution & Build Verification

### 2.1 Solution Compilation
Command:
```powershell
dotnet build HPAutoCad/HPAutoCad.slnx -c Debug
```
Output:
```
Build succeeded.
    1 Warning(s)  # ILRepack Swatch definition notice
    0 Error(s)
Time Elapsed 00:00:10.44
```

### 2.2 Primary Unit Test Suite (`HPAutoCad.Tests`)
Command:
```powershell
dotnet test HPAutoCad.Tests/HPAutoCad.Tests.csproj
```
Output:
```
Test run summary: Passed! - HPAutoCad.Tests.dll (net10.0|x64)
  total: 415
  failed: 0
  succeeded: 412
  skipped: 3  # Pre-existing live satellite imagery tests requiring HPGEO_LIVE_TILES=1
  duration: 4s 282ms
```
- Core SmartPlot tests: **174 unit and stress tests**, 100% passing.
  - `PlotBoundsAndModelTests`: 9 tests
  - `PlotOrderServiceTests`: 9 tests
  - `PlotOrderAdversarialStressTests`: 18 tests (1,000 frames in 3ms, jitter, precision boundary)
  - `LayoutRangeParserTests`: 18 tests
  - `FileNameServiceTests`: 18 tests
  - `PresetServiceTests`: 9 tests
  - `Challenger2StressTests`: 77 tests (concurrency, malicious ranges, null handling)
  - `PdfMergeServiceTests`: 10 tests
  - `PdfMergeServiceStressTests`: 6 tests (data-loss resilience, self-deletion prevention)

### 2.3 Sibling Regression Suites
1. `HPAutoCad.Mcp.Server.Tests`: 280 passed, 0 failed.
2. `HPAutoCad.Aec.Tests`: 225 passed, 0 failed.
3. `HPCivil3d.McpBridge.Tests`: 60 passed, 0 failed (zero mirror drift).

---

## 3. Forensic Integrity Audit Summary

The independent Forensic Integrity Auditor (`auditor_victory`, conversationId `bf406a6b-46a0-46d9-90ae-1c6bfbe47bee`) conducted an exhaustive audit across all 7 layers and delivered a final verdict of **`CLEAN`**:
- **0** hardcoded test outputs or lookup cheats.
- **0** dummy or facade implementations.
- **0** calls to `Process.Start` or external tools (PDF24, pdftk).
- **0** references to `Autodesk.*` in `HPAutoCad.Core`.
- **100%** authentic in-process PDF merging, AutoCAD plot pipeline orchestration, and WPF MVVM execution.

---

## 4. Conclusion & Handover
Smart Plot Pro is fully built, tested, packaged, and verified. All acceptance criteria from `ORIGINAL_REQUEST.md` (## 2026-09-20T22:21:59Z) have been completely satisfied.
