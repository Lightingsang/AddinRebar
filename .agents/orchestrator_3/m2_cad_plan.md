# Technical Specification & Implementation Plan: CAD Commands & CAD Services
**Milestone**: M2 (Add-In Layer & UI Feature) — CAD Commands & CAD Services Sub-track  
**Target Path**: `HPAutoCad/HPAutoCad/HPGeoLink/`  
**Source Baseline**: `HPGeo/HPGeo.AutoCad/` (`Commands/`, `Cad/`, `Imagery/`, root files)  
**Target Project**: `HPAutoCad/HPAutoCad/HPAutoCad.csproj` (`net8.0-windows`)  
**Domain Dependency**: `HPAutoCad/HPAutoCad.Core/HPAutoCad.Core.csproj` (`net8.0`)  
**Author**: `explorer_m2_cad`  
**Target Consumer**: Worker implementing CAD commands and CAD services in `HPAutoCad/HPAutoCad/`

---

## 1. Executive Summary & Objective

This specification provides the exact, file-by-file technical instructions to port and modernize the complete CAD command and service architecture from `HPGeo.AutoCad` into `HPAutoCad/HPAutoCad/HPGeoLink/`. 

The scope comprises **18 files** divided into 5 functional areas:
1. **AutoCAD Commands** (7 files): `HPGeoDialogCommand.cs`, `HPGeoKmzScriptCommand.cs`, `HPGeoImportCommand.cs`, `HPGeoImportScriptCommand.cs`, `HPGeoImageScriptCommand.cs`, `HPGeoInfoCommand.cs`, `ImageryConsole.cs`.
2. **CAD Services** (5 files): `DrawingContext.cs`, `DrawingReader.cs`, `DrawingWriter.cs`, `DocumentSettingsStore.cs`, `UserSettingsStore.cs`.
3. **Imagery Services** (4 files): `ImageryPipeline.cs`, `RasterInserter.cs`, `TileStitcher.cs`, `HelperTileFetcher.cs`.
4. **Support Utilities** (2 files): `GoogleEarthLauncher.cs`, `HPGeoLog.cs`.
5. **Entry Point** (1 file): `Entry.cs` (in `HPAutoCad` root).

All logic adheres strictly to the **isolated ALC pattern**: the add-in assembly contains **zero** AutoCAD command registration attributes (`[CommandMethod]`, `[ExtensionApplication]`). All command discovery and registration belongs to `HPAutoCad.Loader` in the Default ALC. `HPAutoCad.dll` exposes reflection delegates through `HPAutoCad.Entry.Start` returning an `IReadOnlyDictionary<string, Delegate>`.

---

## 2. Target File Layout & Project Structure

Inside `HPAutoCad/HPAutoCad/`:

```
HPAutoCad/
├── HPAutoCad.csproj                     ← Add-In UI & Commands Project (net8.0-windows)
├── Entry.cs                             ← Reflection entry point for HPAutoCad.Loader
└── HPGeoLink/
    ├── Commands/
    │   ├── HPGeoDialogCommand.cs        ← HPGEO: interactive entity selection & dialog launch
    │   ├── HPGeoKmzScriptCommand.cs     ← -HPGEOKMZ: CLI script export to KMZ
    │   ├── HPGeoImportCommand.cs        ← HPGEOIMPORT: interactive KML/KMZ import dialog
    │   ├── HPGeoImportScriptCommand.cs  ← -HPGEOIMPORT: CLI script import from KML/KMZ
    │   ├── HPGeoImageScriptCommand.cs   ← -HPGEOIMAGE: CLI script satellite imagery fetch & insert
    │   ├── HPGeoInfoCommand.cs          ← HPGEOINFO: diagnostic & drawing information inspection
    │   └── ImageryConsole.cs            ← Shared imagery CLI reporting, refusal, and settings persistence
    ├── Cad/
    │   ├── DrawingContext.cs            ← Document metadata snapshot (INSUNITS, chord tolerance, paths)
    │   ├── DrawingReader.cs             ← Point & polyline entity extraction, arc tessellation, WCS flattening
    │   ├── DrawingWriter.cs             ← Atomic entity generation on HPGEO-IMPORT (single undo transaction)
    │   ├── DocumentSettingsStore.cs     ← NOD Xrecord persistence under key "HPGEO"
    │   └── UserSettingsStore.cs         ← User settings persistence (%AppData%\HPGeo\settings.json)
    ├── Imagery/
    │   ├── ImageryPipeline.cs           ← Fetch, stitch, control-grid affine fit, bicubic warp, insert pipeline
    │   ├── RasterInserter.cs            ← RasterImage & RasterImageDef creation on HPGEO-IMAGE, draw order, IMAGEQUALITY
    │   ├── TileStitcher.cs              ← WPF WIC Bgra32 mosaic decode, assemble, and attribution stamp
    │   └── HelperTileFetcher.cs         ← Subprocess client for HPAutoCad.TileFetch.exe (firewall bypass)
    └── Support/
        ├── GoogleEarthLauncher.cs       ← Standard Google Earth Pro detection & execution
        └── HPGeoLog.cs                  ← Thread-safe file logger (%LocalAppData%\HPGeo\logs)
```

---

## 3. Detailed Analysis & Specification of AutoCAD Commands

### 3.1 `HPGeoDialogCommand.cs`
- **Namespace**: `HPAutoCad.HPGeoLink.Commands`
- **Command Name**: `HPGEO` (invoked via `"dialog"` delegate)
- **Execution Lifecycle**:
  1. Retrieves active document: `var doc = AcadApp.DocumentManager.MdiActiveDocument; if (doc is null) return; var ed = doc.Editor;`.
  2. Reads drawing context snapshot: `var ctx = DrawingContext.Read(doc);`.
  3. Executes `Pick(ed, tr, doc.Database)`:
     - First checks implied pick-first selection via `ed.SelectImplied()`. If objects exist, clears implied selection and filters for `DBPoint` or `Polyline`.
     - Otherwise prompts: `\nChọn POINT / LWPOLYLINE VN-2000 (Enter = toàn bộ model space)` with `SelectionFilter` on `"POINT,LWPOLYLINE"`.
     - If user cancels (`PromptStatus.Cancel` / Escape), cleanly aborts without error.
     - If user presses Enter without picking, automatically selects all model space IDs via `DrawingReader.ModelSpaceIds(tr, db, e => e is DBPoint or Polyline)`.
  4. Reads survey geometry via `DrawingReader.Read(tr, ids, ctx.ChordToleranceDrawingUnits)`.
  5. Validates non-empty geometry (`read.Points.Count == 0 && read.Boundaries.Count == 0`). Reports skipped entities if present.
  6. Loads persisted settings: `DocumentSettingsStore.Read(doc.Database)` (drawing-level) with fallback to `UserSettingsStore.Load()` (user-level).
  7. Instantiates `GeoExportViewModel` and `GeoExportWindow(viewModel) { MapEnabled = ... }`.
  8. Shows modal window using AutoCAD host window handle: `AcadApp.ShowModalWindow(AcadApp.MainWindow.Handle, window, false);` (`persistSizeAndPosition: false` prevents stale size hijacking).
  9. Upon export completion (`viewModel.LastExportPath is not null`):
     - Saves settings to drawing NOD via `DocumentSettingsStore.Write(doc.Database, settings)`.
     - Saves settings to user JSON via `UserSettingsStore.Save(settings with { MapEnabled = window.MapEnabled })`.
  10. If satellite imagery was requested (`viewModel.ImageChoice is { } choice`):
      - Executes `InsertImagery(doc, ed, ctx, viewModel, choice, drawingSettings, userSettings)`.
      - Verifies drawing unit factor > 0, checks layer lock via `RasterInserter.CheckLayer(doc.Database)`.
      - Computes bounding box, resolves image path (`RasterInserter.ImagePathFor`), creates `ImageryRequest`.
      - Executes `ImageryPipeline.Run(doc.Database, request, ...)` with user break polling `HostApplicationServices.Current.UserBreak()`.
      - Reports results via `ImageryConsole.Report` and persists settings via `ImageryConsole.Remember`.
  11. Contains nested `Shell : IGeoExportShell`:
      - `AskSavePath(string? initialDirectory, string suggestedFileName)` using `Microsoft.Win32.SaveFileDialog`.
      - `OpenPath(string path)` via `Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })`.
      - `OpenInGoogleEarth(string kmzPath)` via `GoogleEarthLauncher.Open(kmzPath)`.
      - `OpenUrl(string url)` via `ProcessStartInfo`.

### 3.2 `HPGeoKmzScriptCommand.cs`
- **Namespace**: `HPAutoCad.HPGeoLink.Commands`
- **Command Name**: `-HPGEOKMZ` (invoked via `"kmz-script"` delegate)
- **Execution Lifecycle**:
  1. Prompts for CLI argument string: `ed.GetString(new PromptStringOptions("\nHPGeo arguments (cm=<KTT> out=<file.kmz> [type=both|points|boundaries] [unit=m|mm] [layer=<wildcard>] [k0= fe= fn= name= pcolor= lcolor=]): ") { AllowSpaces = true })`.
  2. Parses string into `ExportArguments` record via `ExportArguments.Parse(input.StringResult)`.
  3. Reads drawing context `DrawingContext.Read(doc)`.
  4. Resolves unit: `args.UnitOverride ?? ctx.Unit`, gets `DrawingUnitFactor.MetersPerUnit(unit)`.
  5. Opens transaction: resolves layer wildcard filter (using `WildcardPattern`), enumerates model space IDs, reads geometry via `DrawingReader.Read(tr, ids, DrawingContext.ChordToleranceFor(metersPerUnit))`.
  6. Configures `ConversionOptions`, `KmlExportOptions`, and output path.
  7. Invokes `KmzExportPipeline.Run(new KmzExportRequest(...))`.
  8. Formats issues and summary to console via `Report(ed, outcome, args)` and logs to `HPGeoLog`.
  9. On success, persists zone parameters to drawing NOD via `DocumentSettingsStore.Write(doc.Database, existing with { CentralMeridianDeg = args.CentralMeridianDeg, ... })`.

### 3.3 `HPGeoImportCommand.cs`
- **Namespace**: `HPAutoCad.HPGeoLink.Commands`
- **Command Name**: `HPGEOIMPORT` (invoked via `"import"` delegate)
- **Execution Lifecycle**:
  1. Reads `DrawingContext.Read(doc)` and settings (`DocumentSettingsStore.Read` ?? `UserSettingsStore.Load`).
  2. Instantiates `GeoImportViewModel(ctx.Unit, new Shell(), stored)` and `GeoImportWindow(viewModel)`.
  3. Displays modal dialog: `AcadApp.ShowModalWindow(AcadApp.MainWindow.Handle, window, false);`.
  4. If user confirmed import (`viewModel.Result is not null`):
     - Executes `Write(doc, viewModel.Result)`:
       - Obtains document lock: `using var docLock = doc.LockDocument();`.
       - Calls `DrawingWriter.Write(doc.Database, plan)`.
       - Logs written counts to `HPGeoLog`.
     - Writes updated settings to both `DocumentSettingsStore` and `UserSettingsStore`.
  5. Implements `Shell : IGeoImportShell` with `Microsoft.Win32.OpenFileDialog` for `.kml`/`.kmz`.

### 3.4 `HPGeoImportScriptCommand.cs`
- **Namespace**: `HPAutoCad.HPGeoLink.Commands`
- **Command Name**: `-HPGEOIMPORT` (invoked via `"import-script"` delegate)
- **Execution Lifecycle**:
  1. Prompts for CLI string: `\nHPGeo import arguments (file=<file.kml|kmz> cm=<KTT> [unit=m|mm] [k0= fe= fn=]): `.
  2. Tokenizes parameters via `ExportArguments.Tokenize(input.StringResult)`.
  3. Validates existence of `file=` argument and tests `File.Exists(file)`.
  4. Parses remaining options through `ExportArguments.Parse(...) + " out=unused.kmz"`.
  5. Parses KML/KMZ features from file via `KmlReader.ReadFile(file)`.
  6. Plans conversion through `new ImportPlanner().FromKml(features, options)`.
  7. If planning succeeds, draws entities via `HPGeoImportCommand.Write(doc, plan)`.
  8. Prints status and writes log to `HPGeoLog`.

### 3.5 `HPGeoImageScriptCommand.cs`
- **Namespace**: `HPAutoCad.HPGeoLink.Commands`
- **Command Name**: `-HPGEOIMAGE` (invoked via `"image-script"` delegate)
- **Execution Lifecycle**:
  1. Prompts for CLI string: `\nHPGeo image arguments ([cm=<KTT>] [handle=<hex>|layer=<wildcard>] [res=0.3|zoom=19] [margin=30] [unit=m|mm] [out=<file.png>] [provider=esri] [k0= fe= fn=]): `.
  2. Parses string into `ImageArguments` record via `ImageArguments.Parse(input.StringResult)`.
  3. Executes `Execute(doc, args, ed)`:
     - Resolves projection parameters and drawing unit using `ImageZoneResolver.Resolve(args, stored, user, ctx.Unit)`. Throws `ImageZoneException` on invalid/missing CRS.
     - Checks whether target layer `HPGEO-IMAGE` is locked or frozen via `RasterInserter.CheckLayer(doc.Database)`.
     - Reads boundary polylines: either by specific hex handle (`args.Handle`), layer filter, or all model space. Filters for closed LWPOLYLINEs with >= 3 vertices. Computes `GridBoundingBox`.
     - Resolves destination image file path (adjacent to DWG or `%LocalAppData%\HPGeo\images`).
     - Computes margin: explicit `margin=` in metres wins; otherwise calculates margin from area multiplier `areaRatio` (default 10x) via `TileCoverage.MarginForAreaRatio`.
     - Constructs `ImageryRequest` with `FitToCaps: args.ResolutionMPerPx is null && args.Zoom is null`.
     - Executes `ImageryPipeline.Run(doc.Database, request, report, userBreak)`.
     - Remembers used parameters via `ImageryConsole.Remember`.
  4. Reports outcome via `ImageryConsole.Report(ed, Name, outcome)`. Handles `ImageZoneException`, `ArgumentException`, and general exceptions with clean error messages.

### 3.6 `HPGeoInfoCommand.cs`
- **Namespace**: `HPAutoCad.HPGeoLink.Commands`
- **Command Name**: `HPGEOINFO` (invoked via `"info"` delegate)
- **Execution Lifecycle**:
  1. Diagnostic inspection tool — completely read-only, runs with `CommandFlags.NoUndoMarker`.
  2. Prints product metadata: `HPGeo {Entry.Version} · {PRODUCT} {ACADVER}`.
  3. Prints drawing file and folder: `ctx.DocumentName`, `ctx.DirectoryPath ?? "(unsaved)"`.
  4. Prints unit details: `INSUNITS: {ctx.InsUnitsCode} = {ctx.UnitLabel} → metres per unit: {ctx.MetersPerUnit}`.
  5. Prints stored VN-2000 settings from drawing NOD: `DocumentSettingsStore.Read(doc.Database)`.
  6. Prints user settings from `%AppData%\HPGeo\settings.json`: `UserSettingsStore.Load()`.
  7. Prints province catalog statistics: count of current & legacy provinces, central meridians from `ProvinceCatalog.Default`.
  8. Queries model space entities: counts `POINT`s, `LWPOLYLINE`s (open and closed), and lists breakdown of skipped entity types.
  9. Counts `RasterImage` entities on layer `HPGEO-IMAGE` via `RasterInserter.CountImages`.
  10. Reports imagery provider, resolution, margin, tile cache size via `TileCache.SizeBytes()`, and tile fetch mechanism (`HelperTileFetcher.DefaultExePath() is null ? "in-process" : "helper HPAutoCad.TileFetch.exe"`).
  11. Performs trial conversion of point #1 using default Central Meridian (105.75°) to verify geodetic transform pipeline live.
  12. Prints active log folder path and writes informational entry to `HPGeoLog`.

### 3.7 `ImageryConsole.cs`
- **Namespace**: `HPAutoCad.HPGeoLink.Commands`
- **Shared Helpers**:
  - `Refuse(Editor ed, string commandName, string code, string message)`: standard refusal output format grepped by tests (`\nHPGeo Error: {message}\nHPGeo: FAILED — nothing inserted.\n`).
  - `Report(Editor ed, string commandName, ImageryOutcome outcome)`: standard success/failure summary printing RasterImage handle, layer, pixel dimensions, resolution, lower-left coordinate, affine residual RMS/max, image path, world file path, and attribution.
  - `Remember(Document doc, GeoSettings? stored, GeoSettings? user, TmParameters tm, DrawingUnit? chosenUnit, string providerId, double resolutionMPerPx, double marginM, bool useCurrentCatalog = true, string? provinceName = null, double? areaRatio = null)`: updates both `DocumentSettingsStore` and `UserSettingsStore` consistently.

---

## 4. Detailed Analysis & Specification of CAD Services

### 4.1 `DrawingContext.cs`
- **Namespace**: `HPAutoCad.HPGeoLink.Cad`
- **Type**: `internal sealed record DrawingContext(string DocumentName, string? DirectoryPath, int InsUnitsCode, DrawingUnit Unit)`
- **Key Properties**:
  - `MetersPerUnit`: `DrawingUnitFactor.MetersPerUnit(Unit)` (null when unknown).
  - `ChordToleranceDrawingUnits`: computes tolerance in current drawing units:
    ```csharp
    public double ChordToleranceDrawingUnits => ChordToleranceFor(MetersPerUnit);
    public static double ChordToleranceFor(double? metersPerUnit) =>
        metersPerUnit is { } f && f > 0 ? BulgeTessellator.DefaultToleranceM / f : BulgeTessellator.DefaultToleranceM;
    ```
  - `UnitLabel`: `DrawingUnitFactor.Label(Unit)`.
  - `BaseName`: `Path.GetFileNameWithoutExtension(DocumentName)`.
- **Factory Method**:
  - `public static DrawingContext Read(Document doc)`: safely checks `doc.IsNamedDrawing`, validates `db.Filename` exists as a rooted path, extracts `(int)db.Insunits`, and resolves unit via `DrawingUnitFactor.FromInsUnits(insUnits)`.

### 4.2 `DrawingReader.cs`
- **Namespace**: `HPAutoCad.HPGeoLink.Cad`
- **Type**: `internal static class DrawingReader`
- **Output Record**: `DrawingReadResult(IReadOnlyList<SurveyPoint> Points, IReadOnlyList<BoundaryPolyline> Boundaries, IReadOnlyDictionary<string, int> SkippedByType)`
- **Geometry Extraction Rules**:
  - All coordinates read strictly in **WCS** (World Coordinate System): vertices through `GetPoint3dAt(i)` and arcs through `GetArcSegmentAt(i)`. This guarantees mirrored polylines (normal $-Z$) or oblique OCS entities are never reflected or distorted.
  - `DBPoint`: creates `SurveyPoint` with 1-based index, `index.ToString()`, `new PlanePoint(point.Position.X, point.Position.Y)`, and entity handle string.
  - `Polyline`:
    - Checks for explicit `polyline.Closed` or implicit closure (where vertex $0$ and vertex $N-1$ are coincident within $10^{-6}$ m).
    - Flattens linear segments directly.
    - If segment has non-zero bulge (`Math.Abs(polyline.GetBulgeAt(i)) >= 1e-12`): calls `polyline.GetArcSegmentAt(i)`, computes chord count via `BulgeTessellator.ChordCount(arc.Radius, includedAngle, tolerance)`, samples interior points via `arc.GetSamplePoints(...)`, and appends to coordinate list.
  - Model space traversal: `ModelSpaceIds(Transaction tr, Database db, Func<Entity, bool>? filter = null)` yields all object IDs in `BlockTableRecord.ModelSpace`.
  - Skipped entities: counted in `SkippedByType` dictionary by entity type name for user awareness.

### 4.3 `DrawingWriter.cs`
- **Namespace**: `HPAutoCad.HPGeoLink.Cad`
- **Type**: `internal static class DrawingWriter`
- **Constants**:
  - `public const string LayerName = "HPGEO-IMPORT";`
  - `private const short LayerColorIndex = 3;` (ACI green)
- **Output Record**: `DrawingWriteResult(int PointCount, int PolylineCount, string Layer, IReadOnlyList<string> Handles)`
- **Transaction Safety**:
  - Executes entirely within one transaction (`using var tr = db.TransactionManager.StartTransaction()`).
  - Ensures layer exists via `EnsureLayer(tr, db)`. Throws `InvalidOperationException` if layer is locked or frozen.
  - Iterates `plan.Points`: appends `new DBPoint(new Point3d(x, y, 0)) { LayerId = layerId }`.
  - Iterates `plan.Polylines`: creates `Polyline(vertexCount) { LayerId = layerId }`, adds vertices with bulge 0, sets `polyline.Closed`, appends to model space.
  - Calls `tr.Commit()`. Single transaction ensures one `U` (Undo) removes all imported geometry cleanly.

### 4.4 `DocumentSettingsStore.cs`
- **Namespace**: `HPAutoCad.HPGeoLink.Cad`
- **Type**: `internal static class DocumentSettingsStore`
- **Persistence Target**: Named Objects Dictionary (`NOD`) under key `GeoSettings.Key` (`"HPGEO"`).
- **Format**: `Xrecord` containing a `ResultBuffer` of typed DXF pairs:
  - Header text tagged with `"k="`: e.g. `k=version`, `k=catalog`, `k=province`, `k=cm`, `k=k0`, `k=fe`, `k=fn`, `k=unit`, `k=output`, `k=pcolor`, `k=lcolor`, `k=savedBy`, `k=imgProvider`, `k=imgRes`, `k=imgMargin`, `k=imgArea`.
  - Data entries follow immediately: `DxfCode.Text`, `DxfCode.Int32`, or `DxfCode.Real`.
- **Fault Tolerance**:
  - `Read(Database db)` wraps `ReadCore` in try/catch. A corrupted or unrecognized Xrecord returns `null` and logs a warning to `HPGeoLog` instead of crashing the command.
  - `Write(Database db, GeoSettings settings)` opens NOD for write, constructs typed values (skipping NaN / non-finite doubles), and either updates existing Xrecord or appends a new one.

### 4.5 `UserSettingsStore.cs`
- **Namespace**: `HPAutoCad.HPGeoLink.Cad`
- **Type**: `internal static class UserSettingsStore`
- **Persistence Target**: `%AppData%\HPGeo\settings.json`
- **Serialization**:
  - Uses `System.Text.Json.JsonSerializer` with `WriteIndented = true`, `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`, and `JsonStringEnumConverter`.
  - `Load()`: returns `null` if file does not exist or cannot be deserialized (logs warning, never throws).
  - `Save(GeoSettings settings)`: ensures `%AppData%\HPGeo\` directory exists, sets `SavedBy = "HPAutoCad " + Entry.Version`, and writes UTF-8 JSON.

---

## 5. Detailed Analysis & Specification of Imagery Services

### 5.1 `ImageryPipeline.cs`
- **Namespace**: `HPAutoCad.HPGeoLink.Imagery`
- **Pipeline Architecture**:
  ```
  Boundary Bounding Box (VN-2000)
    ↓ TileCoverage.Plan (determines Zoom, TileAddress list, Web Mercator bounds)
    ↓ Fetch Tiles (HelperTileFetcher / TileFetcher via thread pool with deadline & Esc break)
    ↓ TileStitcher.Stitch (decodes tiles into uncompressed Bgra32 RasterBuffer mosaic)
    ↓ OutputRaster.BuildControlGrid (generates grid of VN-2000 ↔ Web Mercator control points)
    ↓ AffineFit.Fit (computes affine transformation and residual RMS error)
    ↓ RasterWarper.Warp (bicubic interpolation onto VN-2000 north-up raster)
    ↓ TileStitcher.StampAttribution (renders attribution watermark via DrawingVisual)
    ↓ TileStitcher.SavePng (writes final PNG to disk)
    ↓ RasterInserter.Insert (attaches RasterImage into DWG, writes .pgw world file, raises IMAGEQUALITY)
  ```
- **Error Handling & Cleanup**:
  - If `RasterInserter.Insert` fails or throws, the pipeline quietly deletes the generated `.png` and `.pgw` files so no orphaned images remain.
  - If tile fetch helper fails to start (e.g. executable blocked or missing runtime), falls back to in-process `TileFetcher` with a warning issue `HELPER_UNAVAILABLE`.
  - Cancellation via Escape (`Func<bool> userBreak`) yields clean `CANCELLED` outcome without inserting.

### 5.2 `RasterInserter.cs`
- **Namespace**: `HPAutoCad.HPGeoLink.Imagery`
- **Constants**:
  - `public const string LayerName = "HPGEO-IMAGE";`
  - `private const short LayerColorIndex = 8;` (ACI grey)
  - `public static readonly string UnsavedImageRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPGeo", "images");`
- **Insertion Logic**:
  1. Writes ESRI World File (`.pgw`) adjacent to the PNG with pixel size and origin coordinates.
  2. Resolves source path name relative to DWG if the PNG sits in the same directory as the drawing (`SourceNameFor`).
  3. Ensures image dictionary exists: `RasterImageDef.GetImageDictionary(db)` or `RasterImageDef.CreateImageDictionary(db)`.
  4. Generates unique definition name using `RasterImageDef.SuggestName(dict, fullPath)`.
  5. Creates `RasterImageDef`, sets `SourceFileName` and `ActiveFileName`, loads image data, appends to dictionary.
  6. Creates `RasterImage` entity: sets `ImageDefId`, anchors at lower-left coordinate in drawing units, sets width/height vectors ($U, V$).
  7. Associates raster definition: `RasterImage.EnableReactors(true); image.AssociateRasterDef(def);`.
  8. Moves image to bottom of draw order: `DrawOrderTable.MoveToBottom(new ObjectIdCollection { image.ObjectId })`.
  9. Inspects `ACAD_IMAGE_VARS` in NOD: if `IMAGEQUALITY` is currently `Draft`, automatically upgrades it to `High` within the same transaction so satellite imagery is rendered crisp.

### 5.3 `TileStitcher.cs`
- **Namespace**: `HPAutoCad.HPGeoLink.Imagery`
- **Key Functions**:
  - `Stitch(TilePlan plan, IReadOnlyDictionary<TileAddress, byte[]> tiles) -> RasterBuffer`: fills a pre-allocated Bgra32 pixel buffer with decoded tile frames at proper $(X, Y)$ pixel offsets.
  - `Decode(byte[] bytes) -> BitmapSource`: decodes image via `BitmapDecoder.Create` and converts format to `PixelFormats.Bgra32`.
  - `SavePng(BitmapSource bitmap, string path)`: writes PNG file using `PngBitmapEncoder`.
  - `StampAttribution(BitmapSource bitmap, string text) -> BitmapSource`: renders attribution string at bottom-left corner with semi-transparent black backing rectangle (`Color.FromArgb(150, 0, 0, 0)`) using `DrawingVisual` and `RenderTargetBitmap`.

### 5.4 `HelperTileFetcher.cs`
- **Namespace**: `HPAutoCad.HPGeoLink.Imagery`
- **Constants**:
  - `public const string HelperFolder = "TileFetch";`
  - `public const string HelperExe = "HPAutoCad.TileFetch.exe";`
- **Process Management**:
  - Locates helper: checks `App\TileFetch\HPAutoCad.TileFetch.exe` adjacent to add-in DLL (with fallback to `HPGeo.TileFetch.exe`).
  - Writes request file: `TileFetchProtocol.WriteRequest(...)` containing provider ID, cache root, user agent, and tile addresses.
  - Spawns subprocess with redirected stdout and stderr, reads stdout lines through `TileFetchProtocol.Parse(line)`.
  - Reports progress percentages to `IProgress<int>`.
  - On token cancellation, terminates process tree via `process.Kill(entireProcessTree: true)`.
  - Reads finished tiles directly from shared cache via `TileCache.ReadAsync`.

---

## 6. Detailed Analysis & Specification of Support Utilities

### 6.1 `GoogleEarthLauncher.cs`
- **Namespace**: `HPAutoCad.HPGeoLink.Support` (or `HPAutoCad.HPGeoLink`)
- **Key Methods**:
  - `CandidatePaths()`: checks standard installation paths for `googleearth.exe` in `ProgramFiles`, `ProgramFilesX86`, and `LocalApplicationData` under `Google\Google Earth Pro\client\` and `Google\Google Earth\client\`.
  - `Open(string kmzPath) -> string`: launches executable directly if found; otherwise launches via default Windows shell file association (`UseShellExecute = true`).

### 6.2 `HPGeoLog.cs`
- **Namespace**: `HPAutoCad.HPGeoLink.Support` (or `HPAutoCad.HPGeoLink`)
- **Log Path**: `%LocalAppData%\HPGeo\logs\hpgeo-YYYYMMDD.log`
- **Structure**: Thread-safe locking around file appending. Output format: `yyyy-MM-dd HH:mm:ss.fff [INF|WRN|ERR] message`.
- **Note**: Preserving this exact path and format is mandatory because acceptance harness scripts (`acceptance.ps1`, `dialog-check.ps1`) and unit tests grep this directory directly.

---

## 7. Reflection Entry Point (`Entry.cs`)

### 7.1 Location & Namespace
- **File Path**: `HPAutoCad/HPAutoCad/Entry.cs`
- **Namespace**: `HPAutoCad` (or `HPAutoCad.HPGeoLink`)
- **Class**: `public static class Entry`

### 7.2 Method Signatures & Delegate Dictionary
The reflection entry point must satisfy the loader contract defined in `PROJECT.md` line 77, while maintaining full backward compatibility:

```csharp
namespace HPAutoCad;

using System;
using System.Collections.Generic;
using System.Reflection;
using HPAutoCad.HPGeoLink;
using HPAutoCad.HPGeoLink.Commands;

public static class Entry
{
    public static string Version { get; } =
        typeof(Entry).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? typeof(Entry).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";

    /// <summary>
    /// Loader reflection entry point called by HPAutoCad.Loader (AppLoadContext).
    /// Returns dictionary of delegates invoked by AutoCAD commands registered in the loader.
    /// </summary>
    public static IReadOnlyDictionary<string, Delegate> Start(string appDirectory, Action<string>? log = null)
    {
        log?.Invoke($"HPAutoCad {Version} starting from {appDirectory}");
        HPGeoLog.Information($"HPAutoCad {Version} loaded from {appDirectory}");

        return new Dictionary<string, Delegate>(StringComparer.Ordinal)
        {
            ["dialog"] = new Action(HPGeoDialogCommand.Run),
            ["kmz-script"] = new Action(HPGeoKmzScriptCommand.Run),
            ["import"] = new Action(HPGeoImportCommand.Run),
            ["import-script"] = new Action(HPGeoImportScriptCommand.Run),
            ["image-script"] = new Action(HPGeoImageScriptCommand.Run),
            ["info"] = new Action(HPGeoInfoCommand.Run),
            ["stop"] = new Action(Stop),
        };
    }

    /// <summary>Backward-compatible 3-argument overload for legacy loaders.</summary>
    public static IReadOnlyDictionary<string, Delegate> Start(string appDirectory, string product, string acadVersion)
    {
        HPGeoLog.Information($"HPAutoCad {Version} loaded in {product} {acadVersion} from {appDirectory}");
        return Start(appDirectory, null);
    }

    private static void Stop() => HPGeoLog.Information($"HPAutoCad {Version} unloaded");
}
```

*Note for Worker*: If the loader uses `entry.GetMethod("Start", BindingFlags.Public | BindingFlags.Static)` without specifying parameter types, having two overloads will throw `AmbiguousMatchException`. Therefore, verify whether the loader passes `[appDir, log]` or `[appDir, product, acadVersion]`. The recommended approach is a single method with optional arguments:
```csharp
public static IReadOnlyDictionary<string, Delegate> Start(string appDirectory, Action<string>? log = null, string? product = null, string? acadVersion = null)
```
Or if the loader passes two arguments: `(string appDirectory, Action<string> log)`.

---

## 8. Complete Namespace & Dependency Migration Matrix

### 8.1 Namespace Mapping Table

| Old Namespace (`HPGeo`) | New Namespace (`HPAutoCad`) | Sub-Directory |
|---|---|---|
| `HPGeo.AutoCad` | `HPAutoCad` | Root of `HPAutoCad/` (`Entry.cs`) |
| `HPGeo.AutoCad` | `HPAutoCad.HPGeoLink` | `HPGeoLink/Support/` (`GoogleEarthLauncher`, `HPGeoLog`) |
| `HPGeo.AutoCad.Commands` | `HPAutoCad.HPGeoLink.Commands` | `HPGeoLink/Commands/` (all 7 command files) |
| `HPGeo.AutoCad.Cad` | `HPAutoCad.HPGeoLink.Cad` | `HPGeoLink/Cad/` (all 5 CAD service files) |
| `HPGeo.AutoCad.Imagery` | `HPAutoCad.HPGeoLink.Imagery` | `HPGeoLink/Imagery/` (all 4 imagery service files) |
| `HPGeo.AutoCad.UI` | `HPAutoCad.HPGeoLink.UI` | `HPGeoLink/UI/` (WPF views and view models) |

### 8.2 Domain Library Mapping Table (`HPGeo.Core` -> `HPAutoCad.Core`)

| Old Namespace (`HPGeo.Core`) | New Namespace (`HPAutoCad.Core`) | Key Types |
|---|---|---|
| `HPGeo.Core.Catalog` | `HPAutoCad.Core.HPGeoLink.Catalog` | `ProvinceCatalog`, `Province`, `CentralMeridian` |
| `HPGeo.Core.Conversion` | `HPAutoCad.Core.HPGeoLink.Conversion` | `Vn2000Converter`, `ConversionOptions`, `ExportArguments` |
| `HPGeo.Core.Geometry` | `HPAutoCad.Core.HPGeoLink.Geometry` | `BulgeTessellator` |
| `HPGeo.Core.Imagery` | `HPAutoCad.Core.HPGeoLink.Imagery` | `TileCoverage`, `TileFetcher`, `RasterWarper`, `AffineFit`, `TileCache`, `WebMercator`, `TileFetchProtocol` |
| `HPGeo.Core.Import` | `HPAutoCad.Core.HPGeoLink.Import` | `ImportPlanner`, `KmlReader`, `CoordinateTextParser` |
| `HPGeo.Core.Kml` | `HPAutoCad.Core.HPGeoLink.Kml` | `KmzExportPipeline`, `KmlExportOptions`, `KmlColor` |
| `HPGeo.Core.Model` | `HPAutoCad.Core.HPGeoLink.Model` | `PlanePoint`, `GeoPoint`, `SurveyPoint`, `BoundaryPolyline` |
| `HPGeo.Core.Projection` | `HPAutoCad.Core.HPGeoLink.Projection` | `TmParameters`, `Vn2000Wgs84Transform`, `TransverseMercator` |
| `HPGeo.Core.Settings` | `HPAutoCad.Core.HPGeoLink.Settings` | `GeoSettings` |
| `HPGeo.Core.Text` | `HPAutoCad.Core.HPGeoLink.Text` | `WildcardPattern` |
| `HPGeo.Core.Units` | `HPAutoCad.Core.HPGeoLink.Units` | `DrawingUnit`, `DrawingUnitFactor` |
| `HPGeo.Core.Validation` | `HPAutoCad.Core.HPGeoLink.Validation` | `CoordinateValidator`, `Vn2000Bounds` |

---

## 9. Concrete, Step-by-Step Implementation Instructions for Worker

### Phase 1: Create Project & Directory Scaffolding
1. Create directory `HPAutoCad/HPAutoCad/` with subdirectories:
   - `HPAutoCad/HPAutoCad/HPGeoLink/Commands/`
   - `HPAutoCad/HPAutoCad/HPGeoLink/Cad/`
   - `HPAutoCad/HPAutoCad/HPGeoLink/Imagery/`
   - `HPAutoCad/HPAutoCad/HPGeoLink/Support/`
   - `HPAutoCad/HPAutoCad/HPGeoLink/UI/`
   - `HPAutoCad/HPAutoCad/Resources/Themes/`
2. Create `HPAutoCad/HPAutoCad/HPAutoCad.csproj` with:
   - `<TargetFramework>net8.0-windows</TargetFramework>`
   - `<UseWPF>true</UseWPF>`
   - `<EnableDynamicLoading>true</EnableDynamicLoading>`
   - References: `AutoCAD.NET` `[25.1.0]`, `CommunityToolkit.Mvvm` `8.4.0`, `MaterialDesignThemes` `5.3.2`, `ILRepack` `2.0.46`, `Microsoft.Web.WebView2` `1.0.4191.47`.
   - ProjectReference: `..\HPAutoCad.Core\HPAutoCad.Core.csproj`.
   - Targets: `CopyTileFetchHelper` (copies `HPAutoCad.TileFetch` output) and `RepackMaterialDesign` (merges MaterialDesign into `HPAutoCad.dll`).
3. Add `HPAutoCad/HPAutoCad/HPAutoCad.csproj` to `HPAutoCad/HPAutoCad.slnx`.

### Phase 2: Port Support Utilities & CAD Services
1. Create `HPGeoLog.cs` and `GoogleEarthLauncher.cs` under `HPGeoLink/Support/` (or `HPGeoLink/`). Update namespaces to `HPAutoCad.HPGeoLink`.
2. Create `DrawingContext.cs`, `DrawingReader.cs`, `DrawingWriter.cs`, `DocumentSettingsStore.cs`, `UserSettingsStore.cs` under `HPGeoLink/Cad/`.
   - Update namespaces to `HPAutoCad.HPGeoLink.Cad`.
   - Update usings to `HPAutoCad.Core.HPGeoLink.*`.
   - Ensure `DocumentSettingsStore` uses `HPAutoCad.Entry.Version` in its `savedBy` text.

### Phase 3: Port Imagery Services
1. Create `TileStitcher.cs`, `RasterInserter.cs`, `HelperTileFetcher.cs`, `ImageryPipeline.cs` under `HPGeoLink/Imagery/`.
   - Update namespaces to `HPAutoCad.HPGeoLink.Imagery`.
   - In `HelperTileFetcher.cs`: update `HelperExe` constant to `"HPAutoCad.TileFetch.exe"`. Add fallback check for `HPGeo.TileFetch.exe`.
   - In `RasterInserter.cs`: preserve `LayerName = "HPGEO-IMAGE"` and `UnsavedImageRoot`.

### Phase 4: Port Commands & Entry Point
1. Create `ImageryConsole.cs`, `HPGeoInfoCommand.cs`, `HPGeoKmzScriptCommand.cs`, `HPGeoImportScriptCommand.cs`, `HPGeoImageScriptCommand.cs` under `HPGeoLink/Commands/`.
2. Create `HPGeoDialogCommand.cs` and `HPGeoImportCommand.cs`:
   - Note: If UI views/viewmodels are ported in parallel, ensure constructor signatures match `GeoExportViewModel` and `GeoImportViewModel`.
3. Create `Entry.cs` in `HPAutoCad/HPAutoCad/`:
   - Implement `Start` method returning the 7 delegates (`"dialog"`, `"kmz-script"`, `"import"`, `"import-script"`, `"image-script"`, `"info"`, `"stop"`).

### Phase 5: Verification & Compilation Gate
1. Build solution:
   ```bash
   dotnet build HPAutoCad/HPAutoCad.slnx -c Debug
   ```
   Verify 0 errors, 0 warnings treated as errors.
2. Run unit tests:
   ```bash
   dotnet test HPAutoCad/HPAutoCad.Tests
   ```
   Verify 161 tests pass.
3. Check mirror tests (Civil 3D parity):
   ```bash
   dotnet test HPCivil3d/HPCivil3d.McpBridge.Tests
   ```
   Verify 100% pass rate.

---

## 10. Verification Method & Quality Invariants

1. **Zero AutoCAD Attributes in Add-In**: Verify with `grep` that `[CommandMethod]` and `[ExtensionApplication]` exist **only** in loader projects (`HPAutoCad.Loader` / `HPAutoCad.McpBridge.Loader`), never inside `HPAutoCad/HPAutoCad/`.
2. **Strict Layer Names**:
   - Import entities: `HPGEO-IMPORT` (Color Index 3).
   - Raster images: `HPGEO-IMAGE` (Color Index 8).
3. **Atomic Undo Transactions**:
   - `HPGEO`: Dialog read-only; export creates KMZ and records settings in NOD; image insert is one undo step.
   - `-HPGEOKMZ`: Model space read-only; records settings in NOD.
   - `HPGEOIMPORT` / `-HPGEOIMPORT`: `DrawingWriter.Write` encapsulates all points and polylines in one `Transaction.Commit()`.
   - `-HPGEOIMAGE`: `RasterInserter.Insert` encapsulates dictionary, definition, entity, draw order, and `IMAGEQUALITY` in one `Transaction.Commit()`.
4. **Resilient Persistence**:
   - NOD key `"HPGEO"` uses lenient reading; unreadable or partially populated Xrecords never throw or crash commands.
   - User settings `%AppData%\HPGeo\settings.json` gracefully handles missing files or serialization errors.
