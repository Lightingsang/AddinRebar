# Handoff Report — AutoCAD Plot Engine & PdfSharp Integration for Smart Plot Pro

## 1. Observation

### 1.1 HPAutoCad Project Dependencies & AutoCAD Assemblies
- File inspected: `HPAutoCad/HPAutoCad/HPAutoCad.csproj` (lines 20–32):
  - Target Framework: `net8.0-windows`
  - Current NuGet references:
    - `<PackageReference Include="AutoCAD.NET" Version="$(AutocadPackageVersion)" ExcludeAssets="runtime" PrivateAssets="all" />` where `$(AutocadPackageVersion)` defaults to `[25.1.0]`.
    - `<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />`
    - `<PackageReference Include="MaterialDesignThemes" Version="5.3.2" />`
    - `<PackageReference Include="ILRepack" Version="2.0.46" PrivateAssets="all" ExcludeAssets="all" GeneratePathProperty="true" />`
    - `<PackageReference Include="Microsoft.Web.WebView2" Version="1.0.4191.47" />`
- AutoCAD 2026 installation path inspected: `C:\Program Files\Autodesk\AutoCAD 2026`:
  - `Get-ChildItem -Filter '*mgd*.dll'` results:
    - `accoremgd.dll` (1,010,968 bytes)
    - `acdbmgd.dll` (6,754,592 bytes)
    - `acdbmgdbrep.dll` (181,016 bytes)
    - `acmgd.dll` (3,576,096 bytes)
    - `AcMPolygonMGD.dll`, `AcShareViewPropsMgd.dll`, `AcTcMgd.dll`, `AdskConnectionPointMgd.dll`, `AdUIMgd.dll`, `AeciMgdApi64.dll`
  - **`AcPublishMgd.dll` does NOT exist** anywhere in AutoCAD 2026.
- Assembly reflection of Plot types via `AutoCAD.NET` [25.1.0]:
  - `Autodesk.AutoCAD.DatabaseServices.PlotSettings` -> in `Acdbmgd.dll`
  - `Autodesk.AutoCAD.DatabaseServices.PlotSettingsValidator` -> in `Acdbmgd.dll`
  - `Autodesk.AutoCAD.DatabaseServices.PlotType` -> in `Acdbmgd.dll`
  - `Autodesk.AutoCAD.DatabaseServices.PlotRotation` -> in `Acdbmgd.dll`
  - `Autodesk.AutoCAD.DatabaseServices.StdScaleType` -> in `Acdbmgd.dll`
  - `Autodesk.AutoCAD.DatabaseServices.PlotPaperUnit` -> in `Acdbmgd.dll`
  - `Autodesk.AutoCAD.PlottingServices.PlotInfo` -> in `accoremgd.dll`
  - `Autodesk.AutoCAD.PlottingServices.PlotInfoValidator` -> in `accoremgd.dll`
  - `Autodesk.AutoCAD.PlottingServices.PlotEngine` -> in `accoremgd.dll`
  - `Autodesk.AutoCAD.PlottingServices.PlotFactory` -> in `accoremgd.dll`
  - `Autodesk.AutoCAD.PlottingServices.PlotProgressDialog` -> in `accoremgd.dll`
  - `Autodesk.AutoCAD.PlottingServices.PlotPageInfo` -> in `accoremgd.dll`
  - `Autodesk.AutoCAD.PlottingServices.ProcessPlotState` -> in `accoremgd.dll`
- Assembly isolation inspected: `HPAutoCad/HPAutoCad.Loader/AppLoadContext.cs`:
  - `AppLoadContext` inherits `AssemblyLoadContext` (`name: "HPAutoCad.App", isCollectible: false`).
  - Host prefixes (`Ac`, `Ad`, `Autodesk.`) fall through to `AssemblyLoadContext.Default` (provided by `acad.exe`).
  - Dependencies are resolved via `AssemblyDependencyResolver` reading `HPAutoCad.deps.json` from `Contents\App\`.
  - Non-host DLLs (`CommunityToolkit.Mvvm.dll`, `HPAutoCad.Core.dll`, `Microsoft.Web.WebView2.*.dll`) reside as loose DLLs in `Contents\App\` and load into `HPAutoCad.App` without collision.
- `RepackMaterialDesign` target in `HPAutoCad.csproj` (lines 61–69):
  - Merges only `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, and `Microsoft.Xaml.Behaviors.dll` into `HPAutoCad.dll` to prevent BAML naming collision.
  - Other libraries are ignored by the repack command and remain loose in output.

### 1.2 PdfSharp v6.x Verification
- NuGet package search for `PdfSharp`:
  - Official package ID is `PDFsharp`, latest stable version `6.1.1` (or `6.2.4`), vendor `PDFsharp-Team`, targeting .NET 6.0/8.0.
  - Dependencies: `Microsoft.Extensions.Logging` (6.0.0), `Microsoft.Extensions.Logging.Abstractions` (6.0.0), `Microsoft.Extensions.Options` (6.0.0), `Microsoft.Extensions.Primitives` (6.0.0), `Microsoft.Extensions.DependencyInjection.Abstractions` (6.0.0).
- In-process merge experiment executed in .NET 8 test harness:
  - Created two single-page test PDFs (`page1.pdf` with A4 portrait 595x842 pt, `page2.pdf` with A4 landscape 842x595 pt).
  - Merged using `PdfDocument` and `PdfReader.Open(path, PdfDocumentOpenMode.Import)`.
  - Added pages to output document: `outputDoc.AddPage(page)`.
  - Output document saved to `merged.pdf`, size: 2,328 bytes.
  - Verified merged page count: 2 pages.
  - Temporary files deleted cleanly with zero file-locking exceptions.

### 1.3 AutoCAD Plot Pipeline API Details
- `PlotFactory` member signatures:
  - `public static ProcessPlotState ProcessPlotState { get; }`
    - Enum values: `ProcessPlotState.NotPlotting` (0), `ProcessPlotState.ForegroundPlotting` (1), `ProcessPlotState.BackgroundPlotting` (2).
  - `public static PlotEngine CreatePublishEngine()`
  - Note: Prompt mentioned `PlotFactory.ProcessPlotState.CreatePlotEngine()`, but the actual method in AutoCAD.NET API is `PlotFactory.CreatePublishEngine()`.
- `PlotEngine` member signatures:
  - `BeginPlot(PlotProgress plotProgress, object parameters)`
  - `BeginDocument(PlotInfo plotInfo, string documentName, object parameters, int copies, bool plotToFile, string fileName)`
  - `BeginPage(PlotPageInfo pageInfo, PlotInfo plotInfo, bool lastPage, object parameters)`
  - `BeginGenerateGraphics(object parameters)`
  - `EndGenerateGraphics(object parameters)`
  - `EndPage(object parameters)`
  - `EndDocument(object parameters)`
  - `EndPlot(object parameters)`
  - `Dispose()` / `Destroy()`
- `PlotProgressDialog` member signatures:
  - Constructor: `public PlotProgressDialog(bool isPreview, int sheetCount, bool showCancelSheetButton)`
  - Status properties: `PlotMsgString`, `StatusMsgString`, `PlotProgressPos`, `UpperPlotProgressRange`, `LowerPlotProgressRange`, `SheetProgressPos`, `UpperSheetProgressRange`, `LowerSheetProgressRange`, `IsVisible`.
  - Cancellation properties: `IsPlotCancelled` (`bool`), `IsSheetCancelled` (`bool`), `PlotCancelStatus`, `SheetCancelStatus`.
  - Lifecycle methods: `OnBeginPlot()`, `OnBeginSheet()`, `OnEndSheet()`, `OnEndPlot()`.
- `PlotSettingsValidator` methods on `PlotSettingsValidator.Current`:
  - `SetPlotConfigurationName(PlotSettings plotSet, string plotDeviceName, string mediaName)`
  - `SetPlotType(PlotSettings plotSet, PlotType plotAreaType)` — `PlotType.Window`
  - `SetPlotWindowArea(PlotSettings plotSet, Extents2d windowArea)`
  - `SetPlotCentered(PlotSettings plotSet, bool isCentered)`
  - `SetUseStandardScale(PlotSettings plotSet, bool useStandard)`
  - `SetStdScaleType(PlotSettings plotSet, StdScaleType scaleType)` — `StdScaleType.ScaleToFit`
  - `SetPlotRotation(PlotSettings plotSet, PlotRotation rotationType)` — `Degrees000`, `Degrees090`, `Degrees180`, `Degrees270`
  - `SetCurrentStyleSheet(PlotSettings plotSet, string styleSheetName)`
  - `RefreshLists(PlotSettings plotSet)`
- `PlotInfo` and `PlotInfoValidator`:
  - `plotInfo.Layout = layout.ObjectId;`
  - `plotInfo.OverrideSettings = plotSettings;`
  - `plotInfoValidator.MediaMatchingPolicy = MatchingPolicy.MatchNextClose;`
  - `plotInfoValidator.Validate(plotInfo);`

### 1.4 Frame Provider Entity Observations
- `BlockReference` in `Acdbmgd.dll`:
  - `IsDynamicBlock`: `bool` indicating dynamic block definition.
  - `DynamicBlockTableRecord`: `ObjectId` pointing to base definition. When dynamic block is edited/stretched, `BlockTableRecord` points to anonymous block `*U...`, whereas `DynamicBlockTableRecord` returns the persistent definition whose `Name` is the user-visible `EffectiveName`.
  - `AttributeCollection`: Collection of `ObjectId` items resolving to `AttributeReference`.
  - `AttributeReference`: Has `.Tag` (string) and `.TextString` (string).
  - `GeometricExtents`: `Extents3d` with `MinPoint` and `MaxPoint`.
- `Polyline`, `Polyline2d`, `Polyline3d` in `Acdbmgd.dll`:
  - `Closed`: `bool` property indicating closed polygon.
  - `GeometricExtents`: `Extents3d` bounding box.
- `LayoutManager` & `Layout` in `Acdbmgd.dll`:
  - `db.LayoutDictionaryId`: Dictionary of all `Layout` objects.
  - `layout.ModelType`: `false` for paper space layouts, `true` for Model Space (`"Model"`).
  - `layout.TabOrder`: `int` indicating display sequence of tabs in AutoCAD UI.
  - `layout.LayoutName`: `string` name of the layout.

---

## 2. Logic Chain

### 2.1 Package & Assembly Architecture
1. **AutoCAD Plot API Availability**:
   - Because `AutoCAD.NET` [25.1.0] references `AutoCAD.NET.Core` (`accoremgd.dll`) and `AutoCAD.NET.Model` (`acdbmgd.dll`), all required plot types (`PlotFactory`, `PlotEngine`, `PlotInfo`, `PlotSettings`, `PlotSettingsValidator`, etc.) are already fully exposed.
   - `AcPublishMgd.dll` was a legacy assembly from ancient AutoCAD versions; in modern AutoCAD (.NET 8), publish and plot engine types reside directly in `accoremgd.dll`.
   - Therefore, no additional AutoCAD assemblies or reference packages are required in `HPAutoCad.csproj`.
2. **PdfSharp v6 Integration**:
   - Adding `<PackageReference Include="PDFsharp" Version="6.1.1" />` to `HPAutoCad/HPAutoCad.csproj` supplies `PdfSharp.dll` and its `Microsoft.Extensions.Logging` dependencies.
   - The add-in is loaded via `HPAutoCad.Loader.AppLoadContext`. This isolated ALC loads dependencies from `Contents\App\` via `HPAutoCad.deps.json`, preventing any conflict with host assemblies or other plugins.
   - `RepackMaterialDesign` only merges MaterialDesign assemblies and explicitly leaves other DLLs intact; `PdfSharp.dll` will be deployed beside `HPAutoCad.dll` and resolved cleanly.

### 2.2 Plot Pipeline Execution Architecture
1. **Concurrency and State Check**:
   - `PlotFactory.ProcessPlotState` must equal `ProcessPlotState.NotPlotting`. If another plot is active, Smart Plot Pro must abort with a friendly notification.
2. **System Variables & Document Safety**:
   - Plotting must occur under `using (doc.LockDocument())`.
   - `BACKGROUNDPLOT`: If non-zero, AutoCAD delegates plotting to a background process, making progress tracking, file output synchronization, and in-memory overrides impossible. Must be forced to `0`.
   - `CMDECHO`: Must be forced to `0` to prevent flooding the AutoCAD text window.
   - Both system variables must be captured and restored in a `finally` block to guarantee AutoCAD returns to its original state even on cancellation or error.
3. **Window Plot Calculation**:
   - For `BlockFrameProvider` and `LayerFrameProvider`, plotting operates in Window mode:
     - Min point: `(MinX, MinY)`, Max point: `(MaxX, MaxY)` converted to `Extents2d`.
     - `psv.SetPlotType(plotSettings, PlotType.Window)`
     - `psv.SetPlotWindowArea(plotSettings, extents2d)`
   - Center & Scale:
     - `psv.SetPlotCentered(plotSettings, true)`
     - `psv.SetUseStandardScale(plotSettings, false)` followed by `psv.SetStdScaleType(plotSettings, StdScaleType.ScaleToFit)`
4. **Orientation Detection**:
   - Frame dimension: `width = MaxX - MinX`, `height = MaxY - MinY`.
   - In `OrientationMode.Auto`:
     - If `width >= height`: Landscape (`PlotRotation.Degrees000` or matched to paper canonical aspect ratio).
     - If `height > width`: Portrait (`PlotRotation.Degrees090` when using standard landscape-oriented PC3 media names).
   - In `OrientationMode.Portrait` or `OrientationMode.Landscape`: Apply explicit `PlotRotation`.
5. **PlotInfo Override Pattern**:
   - Modifying the permanent `Layout` object requires write transactions and dirties the drawing.
   - Assigning `plotInfo.OverrideSettings = plotSettings` completely decouples the plot operation from drawing database persistence, keeping drawings clean.
   - `plotInfoValidator.Validate(plotInfo)` confirms the overridden settings against the device driver prior to calling `engine.BeginDocument`.
6. **Dual Cancellation Handling**:
   - Smart Plot Pro uses `CancellationTokenSource` from the WPF ViewModel.
   - Concurrently, AutoCAD's `PlotProgressDialog` displays a native "Cancel Plot" button.
   - At every sheet iteration and inside the plot loop:
     `if (cts.Token.IsCancellationRequested || progressDialog.IsPlotCancelled) { ... abort ... }`
   - This provides responsive cancellation from both the WPF UI and AutoCAD's native progress dialog.

### 2.3 Frame Providers Architecture
1. **`IFrameProvider` Abstraction**:
   - Sits in `HPAutoCad/SmartPlot/Cad/Providers/IFrameProvider.cs`.
   - Receives `Database db` and `PlotConfiguration config`, returns `IReadOnlyList<PlotItem>`.
2. **`BlockFrameProvider`**:
   - Scans entities in active space (`db.CurrentSpaceId`).
   - Checks `ent is BlockReference blkRef`.
   - Resolves effective name:
     ```csharp
     string blockName = blkRef.IsDynamicBlock 
         ? ((BlockTableRecord)tr.GetObject(blkRef.DynamicBlockTableRecord, OpenMode.ForRead)).Name 
         : blkRef.Name;
     ```
   - If `blockName` matches configured block filter (or all blocks if blank):
     - Extracts attributes: searches `blkRef.AttributeCollection` for tag names matching title/sheet tokens (e.g. "TEN_BV", "SO_BV", "TITLE", "SHEET_NO").
     - Extracts bounds: `blkRef.GeometricExtents`.
     - Creates `PlotItem`.
3. **`LayerFrameProvider`**:
   - Scans entities on configured layer in active space.
   - Checks `ent is Polyline pl && pl.Closed` (or `Polyline2d`/`Polyline3d`).
   - Rejects degenerate polylines (`width <= 0 || height <= 0`).
   - Extracts bounds: `pl.GeometricExtents`.
   - Creates `PlotItem`.
4. **`LayoutFrameProvider`**:
   - Reads `db.LayoutDictionaryId`.
   - Excludes Model space (`!layout.ModelType`).
   - Sorts layouts by `layout.TabOrder`.
   - Uses `LayoutRangeParser` to filter matching 1-based indices.
   - Creates `PlotItem` per layout.

### 2.4 In-Process PDF Merging Architecture
1. **Per-Sheet Temporary Files**:
   - Each sheet plots to a temporary PDF: `Path.Combine(Path.GetTempPath(), $"HPPlot_{Guid.NewGuid():N}.pdf")`.
   - In `OutputMode.SingleFiles`: File is plotted directly to user's sanitized destination filename.
   - In `OutputMode.MergedPdf`:
     1. Plot each sheet to temporary file path.
     2. Collect list of generated temporary paths: `List<string> tempPdfs`.
     3. Call `IPdfMergeService.MergeAsync(tempPdfs, finalMergedPdfPath, cancellationToken)`.
     4. `PdfMergeService` opens each temp file via `PdfReader.Open(file, PdfDocumentOpenMode.Import)`.
     5. Appends all pages to single `PdfDocument`.
     6. Saves to `finalMergedPdfPath`.
     7. In a `finally` block, iterates `tempPdfs` and deletes each temporary file.

---

## 3. Caveats

1. **AutoCAD Runtime Context Required for Engine Tests**:
   - `PlotSettingsValidator.Current`, `PlotFactory.CreatePublishEngine()`, and native graphics plotting require an active `acad.exe` process with an initialized graphics device. Running them in a standalone test runner throws `InvalidProgramException` or `NullReferenceException`.
   - All domain logic (`PlotOrderService`, `LayoutRangeParser`, `FileNameService`, `PresetService`) resides in `HPAutoCad.Core` and is 100% unit-testable in `HPAutoCad.Tests` without AutoCAD.
   - The CAD plot engine and providers must be verified live inside AutoCAD via the MCP test harness (`run-bridge-unattended.ps1` or MCP execute).
2. **PC3 Driver Dependency**:
   - Plotting to PDF relies on an installed PDF PC3 driver, such as `"AutoCAD PDF (General Documentation).pc3"` or `"DWG To PDF.pc3"`. If the PC3 driver is not present on the user's machine, the engine should gracefully fall back to available system PDF printers (e.g. `"Microsoft Print to PDF"`).
3. **Paper Margins on Window Plots**:
   - Standard paper sizes often have printable area margins (e.g. 3–5 mm border). For zero-margin plots (e.g. title blocks that already include borders), canonical media names with `"full_bleed"` or `"expand"` provide optimal results without clipping.

---

## 4. Conclusion

1. **Zero New AutoCAD Packages Needed**:
   `AutoCAD.NET` 25.1.0 already provides every plotting and publishing type in `accoremgd.dll` and `Acdbmgd.dll`. `AcPublishMgd.dll` does not exist in AutoCAD 2026.
2. **PdfSharp v6.1.1 is Ready & Fully Verified**:
   `PDFsharp` package 6.1.1 restores cleanly on .NET 8, merges multiple PDFs with mixed portrait/landscape dimensions in-process in milliseconds, and operates safely within `AppLoadContext` without conflicting with `RepackMaterialDesign`.
3. **Robust Plot Pipeline Architecture**:
   - Pre-check: `PlotFactory.ProcessPlotState == ProcessPlotState.NotPlotting`.
   - Safety: `DocumentLock` and strict `BACKGROUNDPLOT=0` / `CMDECHO=0` restoration in `finally`.
   - Settings: `PlotSettings` with `PlotInfo.OverrideSettings` prevents dirtying drawing databases.
   - Progress: Native `PlotProgressDialog` updated in sync with cooperative `CancellationToken`.
4. **Three Distinct Frame Providers**:
   - `BlockFrameProvider`: Supports Dynamic Blocks via `DynamicBlockTableRecord` -> `EffectiveName`, reading attributes for title and sheet number.
   - `LayerFrameProvider`: Scans closed `Polyline` entities on a designated layer.
   - `LayoutFrameProvider`: Queries paper space layouts in `TabOrder` sequence, filtered via `LayoutRangeParser`.

---

## 5. Verification Method

### 5.1 Project & Dependency Verification
```bash
# Verify HPAutoCad builds cleanly with PDFsharp package reference
dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.slnx" -c Debug
```
- Expected result: 0 errors, 0 warnings treated as errors.
- Verification files to inspect:
  - `HPAutoCad/HPAutoCad/bin/Debug/net8.0-windows/PdfSharp.dll` exists in output.
  - `HPAutoCad/HPAutoCad/bin/Debug/net8.0-windows/MaterialDesignThemes.Wpf.dll` does NOT exist (correctly repack-merged into `HPAutoCad.dll`).

### 5.2 Unit Tests Verification (Host-Free)
```bash
# Run unit tests across HPAutoCad test suites
dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Tests"
```
- Invalidation condition: Any failure in `HPAutoCad.Tests` indicates a regression in domain logic or build configuration.

### 5.3 Live AutoCAD Verification via MCP
1. Launch AutoCAD 2026 with HPAutoCad bundle loaded.
2. Verify MCP named pipe `hpautocad-mcp-2026` is active.
3. Test `HPSMARTPLOT` command opens modeless dialog.
4. Execute test plot on a test DWG containing:
   - Dynamic block frames with attributes.
   - Closed polylines on a specific layer.
   - Multiple layout tabs.
5. Invalidation conditions:
   - If `BACKGROUNDPLOT` remains `0` after plotting fails/aborts (indicates `finally` restoration failure).
   - If temporary files remain in `%TEMP%` after merged PDF generation.
