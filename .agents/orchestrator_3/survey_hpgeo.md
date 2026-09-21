# Technical Survey: HPGeo Codebase & Migration Mapping to HPAutoCad (HPGeoLink)

**Date**: 2026-09-20  
**Author**: `explorer_survey_geo` (Teamwork Explorer Agent)  
**Target Recipient**: `orchestrator_3` (Parent Orchestrator)  
**Scope**: Exploration and cataloging of `HPGeo/` (`HPGeo.Core`, `HPGeo.AutoCad`, `HPGeo.AutoCad.Loader`, `HPGeo.TileFetch`, `HPGeo.Tests`) for consolidation into `HPAutoCad` as feature module `HPGeoLink`.

---

## 1. Executive Summary & Architectural Overview

The `HPGeo` deliverable is an AutoCAD 2026 add-in toolkit providing bi-directional transformation between the Vietnamese national coordinate system **VN-2000** (Transverse Mercator TM-3 with provincial central meridians) and **WGS84**, exporting survey geometry to Google Earth KMZ, importing KML/KMZ or raw cadastral coordinate lists back into AutoCAD, and fetching satellite imagery from Esri World Imagery to warp and insert directly into AutoCAD drawings.

### Current Solution Structure (`HPGeo.slnx`)
```
HPGeo/
├── HPGeo.Core/               (net8.0 - host-free geodetic engine, zero AutoCAD dependencies)
├── HPGeo.AutoCad/            (net8.0-windows - AutoCAD WPF UI, commands, drawing readers/writers)
├── HPGeo.AutoCad.Loader/     (net8.0-windows - isolated AssemblyLoadContext loader, bundle deployer)
├── HPGeo.TileFetch/          (net8.0 console - companion tile downloading process)
├── HPGeo.Tests/              (net10.0-windows - xUnit v3 / Microsoft.Testing.Platform runner)
└── tools/                    (node / powershell - golden fixtures, unattended acceptance scripts)
```

### Key Architectural Characteristics
1. **Host-Free Core**: `HPGeo.Core` contains 100% of mathematical projections (Snyder TM forward/inverse, 7-parameter Helmert coordinate frame, geocentric Cartesian conversion), administrative province catalogs, KML/KMZ serialization, bulge tessellation, coordinate parsing, and tile math without referencing `Autodesk.*`.
2. **Two-Stage ALC Loading**: AutoCAD loads `HPGeo.AutoCad.Loader.dll` into the default ALC (zero 3rd-party NuGet dependencies). The loader spins up `GeoLoadContext` (`AssemblyLoadContext`) pointing to `Contents\App\HPGeo.AutoCad.dll`. This prevents version collisions with AutoCAD or other add-ins (e.g. `CommunityToolkit.Mvvm`, `MaterialDesignThemes`).
3. **ILRepack BAML Isolation**: `MaterialDesignThemes` 5.3.2 is ILRepack-merged into `HPGeo.AutoCad.dll` so no loose `MaterialDesignThemes.Wpf.dll` exists in `Contents\App\`.
4. **Out-of-Process Tile Fetching**: `HPGeo.TileFetch.exe` executes as a child process. Because `acad.exe` is frequently blocked from outbound network access by corporate/firewall rules (e.g. Windows Firewall outbound block on `acad.exe` causing `WSAEACCES`), fetching tiles out-of-process circumvents socket blocking and prevents AutoCAD UI freezing.
5. **Shared Ribbon Standard**: HP add-ins share a single Ribbon tab `HPAUTOCAD_MCP_TAB` (`HPAutoCad`). `HPGeo` adds a single panel `VN2000` (`HPGEO_VN2000_PANEL`).

---

## 2. AutoCAD Commands Catalog

All commands are registered through AutoCAD runtime attributes in `HPGeo.AutoCad.Loader/HPGeoCommands.cs`, which forwards calls via reflection delegates returned by `HPGeo.AutoCad.Entry.Start(...)`.

| Command | Entry Key | CommandFlags | Class & Method | Interaction with AutoCAD | Description |
|---|---|---|---|---|---|
| `HPGEO` | `"dialog"` | `Modal` | `HPGeoDialogCommand.Run()` | Reads selection (`DBPoint`, `Polyline`), opens WPF modal dialog, writes settings to DWG Xrecord `HPGEO`. If satellite imagery button is clicked, runs `ImageryPipeline` and inserts `RasterImage`. | Interactive export workflow: pick points/lines in model space -> configure CRS & options in WPF UI -> export KMZ / open in Google Earth / insert satellite raster into CAD. |
| `-HPGEOKMZ` | `"kmz-script"` | `Modal` | `HPGeoKmzScriptCommand.Run()` | Reads model space entities (with optional layer wildcard), converts coordinates, writes KMZ file, saves CRS to DWG Xrecord `HPGEO`. | Command-line scriptable KMZ export for automation/harness: `cm=<KTT> out=<file.kmz> [type=both\|points\|boundaries] [unit=m\|mm] [layer=<wildcard>] [k0= fe= fn= name= pcolor= lcolor=]`. |
| `HPGEOIMPORT` | `"import"` | `Modal` | `HPGeoImportCommand.Run()` | Opens WPF modal dialog for KML/KMZ or text input, locks document, creates layer `HPGEO-IMPORT` (green ACI 3), draws `DBPoint` & `Polyline`, saves settings. | Interactive import workflow: loads Google Earth KML/KMZ or pasted coordinates (`lat, lon` or VN-2000 `E,N`/`X,Y`) -> transforms to drawing coordinates -> draws in 1 transaction. |
| `-HPGEOIMPORT` | `"import-script"` | `Modal` | `HPGeoImportScriptCommand.Run()` | Reads KML/KMZ from disk, plans import via `ImportPlanner`, draws geometry via `DrawingWriter.Write`. | Command-line scriptable import: `file=<file.kml\|kmz> cm=<KTT> [unit=m\|mm] [k0= fe= fn=]`. |
| `-HPGEOIMAGE` | `"image-script"` | `Modal` | `HPGeoImageScriptCommand.Run()` | Reads closed `Polyline` boundary (via handle or layer), downloads tiles via `HelperTileFetcher`, stitches/warps, writes PNG+PGW, creates `RasterImageDef` and `RasterImage` on layer `HPGEO-IMAGE`. | Command-line satellite image insertion: `[cm=<KTT>] [handle=<hex>\|layer=<wildcard>] [res=0.3\|zoom=19] [margin=30] [unit=m\|mm] [out=<file.png>] [provider=esri] [k0= fe= fn=]`. |
| `HPGEOINFO` | `"info"` | `Modal \| NoUndoMarker` | `HPGeoInfoCommand.Run()` | Read-only. Queries system variables (`PRODUCT`, `ACADVER`, `INSUNITS`), inspects drawing entities, DWG Xrecord, user settings, tile cache size, test conversion. | Diagnostics & acceptance assertion readout printed to AutoCAD command line. |

### CAD Interaction Details
- **Selection & Filtering**: `DrawingReader.ModelSpaceIds` iterates model space block table record. Filters entities: `DBPoint` becomes `SurveyPoint`, `Polyline` (LWPOLYLINE) becomes `BoundaryPolyline`. Arcs in polylines are sampled based on sagitta chord tolerance (`BulgeTessellator.ChordCount`), ensuring curved road boundaries adhere to ~5 mm precision.
- **Transactions & Undo Safety**: Every modifying command executes within an explicit transaction. All changes (entities, layers, Xrecord dictionaries) collapse into a single undo group; pressing `U` in AutoCAD cleanly reverts all changes made by the command.
- **Metadata Persistence (`DocumentSettingsStore`)**: Stored in the Named Objects Dictionary (NOD) under key `HPGEO` as an `Xrecord` with typed DXF pairs (`DxfCode.Text` and `DxfCode.Real`). Contains: `version`, `catalog`, `province`, `cm` (central meridian), `k0`, `fe`, `fn`, `unit`, `output`, `pcolor`, `lcolor`, `savedBy`, `imgProvider`, `imgRes`, `imgMargin`, `imgArea`.
- **User Settings (`UserSettingsStore`)**: Persisted as JSON in `%AppData%\HPGeo\settings.json`.

---

## 3. Geodetic Domain Architecture & Logic (`HPGeo.Core`)

All domain logic resides in `HPGeo.Core` (targeting `net8.0`, zero external dependencies).

### 3.1 Mathematical Projection Pipeline
- **Reference Ellipsoid (`Ellipsoid.cs`)**:
  - VN-2000 is defined on the **WGS84 ellipsoid**:
    - Semi-major axis $a = 6\,378\,137.0\text{ m}$
    - Inverse flattening $1/f = 298.257223563$
    - Derived: $e^2 = f(2 - f)$, $e'^2 = e^2 / (1 - e^2)$, Prime vertical radius of curvature $N(\varphi) = a / \sqrt{1 - e^2 \sin^2\varphi}$.
- **Transverse Mercator (`TransverseMercator.cs` & `TmParameters.cs`)**:
  - Implements Snyder's USGS Bulletin 1395 equations:
    - Standard TM-3 parameters: $k_0 = 0.9999$, False Easting $\text{FE} = 500\,000\text{ m}$, False Northing $\text{FN} = 0.0\text{ m}$, Latitude of Origin $\varphi_0 = 0.0^\circ$.
    - Inverse series (`Inverse`): Exact analytical term-by-term implementation from the national reference tool. Converts grid $(E, N) \to (\varphi, \lambda)$.
    - Forward series (`Forward`): Snyder forward series with Newton-Raphson-like iterative feedback refinement (`ForwardConvergenceM = 1e-9`). Refines $(E, N)$ until inverse calculation round-trips to floating-point precision ($< 10^{-9}\text{ m}$).
- **Helmert 7-Parameter Datum Shift (`Helmert7.cs` & `Helmert7Parameters.cs`)**:
  - Transformation between VN-2000 and WGS84 in geocentric Cartesian $(X, Y, Z)$ using **Coordinate Frame Rotation (EPSG method 1032)**:
    - $\Delta X = -191.90441429\text{ m}$
    - $\Delta Y = -39.30318279\text{ m}$
    - $\Delta Z = -111.45032835\text{ m}$
    - $R_x = -0.00928836\text{ arcsec}$
    - $R_y = 0.01975479\text{ arcsec}$
    - $R_z = -0.00427372\text{ arcsec}$
    - $\Delta s = 0.252906278\text{ ppm}$
  - Matrix formulation: $X' = T + M \cdot R \cdot X$.
  - Exact analytical inverse via cofactor matrix adjugate: inverts the $3\times3$ system without parameter negation, eliminating second-order residual errors.
- **Geocentric Conversion (`GeocentricConverter.cs`)**:
  - Bowring iteration algorithm converting geodetic coordinates $(\varphi, \lambda, h) \leftrightarrow (X, Y, Z)$.
- **Coordinated Transformation (`Vn2000Wgs84Transform.cs`)**:
  - `ToWgs84(PlanePoint, TmParameters)`: Grid $(E, N) \to$ TM Inverse $\to$ Geocentric $(X,Y,Z) \to$ Helmert Forward $\to$ WGS84 Geodetic $(\text{lat}, \text{lon})$. (Assumes $h=0$ on VN-2000 side).
  - `ToVn2000(GeoPoint, TmParameters)`: Solves iteratively for the ellipsoidal height $h_{\text{WGS84}}$ that yields $h_{\text{VN2000}} = 0$ (preventing $\sim 1\text{ mm}$ shift per $30\text{ m}$ height offset due to datum normal divergence), then applies Helmert Inverse $\to$ TM Forward.

### 3.2 Administrative Province Catalog (`ProvinceCatalog.cs`)
- Stores 2 sets of administrative divisions:
  - **Current Catalog (34 provinces/cities)**: Post-2025 merged administrative units. Supports multiple central meridians per merged province with former province attribution labels (e.g. TP. Hồ Chí Minh · $107^\circ 45'$ $\to$ "Bà Rịa - Vũng Tàu cũ").
  - **Legacy Catalog (63 provinces/cities)**: Pre-2025 administrative units.
- Serialized in embedded JSON: `Data/vn2000-provinces.json`.
- Central Meridians: 17 distinct values from $103^\circ 00'$ to $108^\circ 30'$ in $0^\circ 15'$ ($0.25^\circ$) steps.

### 3.3 Domain Validation & Envelopes
- **Vietnam Envelope (`VietnamEnvelope.cs`)**: Bounding box $\text{Lat } [7.5, 24.0]^\circ$, $\text{Lon } [101.5, 110.5]^\circ$. Any coordinate falling outside triggers a blocking `OUTSIDE_VIETNAM` error.
- **Plausibility Check (`PlausibilityCheck.cs`)**: Diagnostic checks for VN-2000 TM-3 bounds:
  - Plausible Easting: $100\,000 \le E \le 900\,000\text{ m}$
  - Plausible Northing: $800\,000 \le N \le 2\,600\,000\text{ m}$
  - Diagnoses swapped X/Y (cadastral format where $X=\text{Northing}, Y=\text{Easting}$), millimeter scaling ($/1000$), or local coordinates.

### 3.4 Geometry & Serialization
- **Bulge Arc Tessellation (`BulgeTessellator.cs`)**: Converts polyline arc segments ($\text{bulge} = \tan(\theta/4)$) into straight chords. Clamps chords between 4 and 512 segments per arc based on sagitta $s = r(1 - \cos(\theta/2n)) \le \text{tolerance}$ (default $5\text{ mm}$).
- **KML/KMZ Pipeline (`KmzExportPipeline.cs`, `KmlDocumentBuilder.cs`, `KmzWriter.cs`)**:
  - Builds valid KML 2.2 schemas with `<Placemark>`, `<Point>`, `<LineString>`, `<Polygon>`, `<Style>`, `<BalloonStyle>`.
  - Points carry 9-decimal coordinate precision ($\sim 0.1\text{ mm}$).
  - Compresses KML into KMZ (ZIP with `doc.kml` entry).
- **Coordinate Parser (`CoordinateTextParser.cs`)**:
  - Uses compiled regular expressions to parse pasted coordinate tables.
  - Supports WGS84 (`lat, lon` or `lon, lat`) and VN-2000 (`E, N` or cadastral `X, Y`).
  - Auto-detects pair order or enforces user selection.

### 3.5 Satellite Imagery Math (`HPGeo.Core/Imagery/`)
- `WebMercator.cs`: Spherical Mercator tile math ($z/x/y$ calculations, pixel bounds, resolution meters/pixel).
- `TileCoverage.cs`: Computes required tiles, bounding box coverage, margin calculation, and area ratios ($10\times$ default).
- `RasterWarper.cs`: Warps Web Mercator tiles to VN-2000 Cartesian grid using a $64\text{-px}$ control grid with Catmull-Rom bicubic interpolation.
- `AffineFit.cs`: Computes affine residual RMS and maximum deviation to verify distortion ($< 0.5\text{ m}$).
- `TileCache.cs`: Manages disk tile cache at `%LocalAppData%\HPGeo\tiles`.

---

## 4. UI, ViewModels, and Controls (`HPGeo.AutoCad/UI`)

The UI is built with WPF (.NET 8.0-windows) and `CommunityToolkit.Mvvm`, utilizing MaterialDesignThemes with custom dark/light theme switching.

### 4.1 Views & Controls
1. **`GeoExportWindow.xaml` / `.cs`**:
   - Primary dialog for `HPGEO`. Size $1040\times760$.
   - Components: Source summary, `CrsSelectionView`, KMZ output options (Point/Line color, output filters), DataGrid coordinate preview table, `MapPanel` (WebView2), collapsible KML preview, status/issues log, satellite imagery insertion controls, action buttons ("Xuất KMZ", "Mở Google Earth", "Google Maps", "Xem KML", "Chèn ảnh vệ tinh vào CAD").
2. **`GeoImportWindow.xaml` / `.cs`**:
   - Primary dialog for `HPGEOIMPORT`. Size $960\times680$.
   - Components: Source selector (File KML/KMZ vs. Pasted Text), coordinate format selector (WGS84 vs VN-2000), pair order options (Auto, E/N, X/Y), polyline join/close toggles, `CrsSelectionView`, DataGrid preview table, action buttons ("Vẽ vào bản vẽ", "Đóng").
3. **`CrsSelectionView.xaml` / `.cs`**:
   - Reusable UserControl for coordinate system configuration.
   - Radio buttons for 34 vs 63 province catalog; editable ComboBox for province with type-ahead search; central meridian ComboBox; manual KTT text box; Expander for advanced geodetic parameters ($k_0, \text{FE}, \text{FN}$, drawing unit).
4. **`MapPanel.xaml` / `.cs`**:
   - Custom UserControl embedding `Microsoft.Web.WebView2.Wpf.WebView2`.
   - Initializes WebView2 with dedicated user data directory (`%LocalAppData%\HPGeo\webview2`).
   - Renders Leaflet map over Esri World Imagery tiles via embedded HTML (`MapHtml.cs`). Communicates bi-directionally via `chrome.webview.postMessage` (pushes JSON geometry, receives `tile-loaded`, `tile-error`, `leaflet-missing`).
   - Graceful fallback: If WebView2 runtime is missing or internet is disconnected, displays user-friendly fallback text without crashing.

### 4.2 ViewModels (`CommunityToolkit.Mvvm`)
- **`GeoExportViewModel.cs` & `GeoExportViewModel.Commands.cs`**:
  - Properties: `FileName`, `PointColor`, `LineColor`, `OutputBoth`, `OutputPoints`, `OutputBoundaries`, `Preview`, `MapDataJson`, `KmlPreview`, `IssuesText`, `Status`, `CanExport`, `ImageResolutionText`, `ImageAreaRatioText`.
  - Commands: `ExportKmzCommand`, `OpenGoogleEarthCommand`, `OpenGoogleMapsCommand`, `ToggleKmlPreviewCommand`, `InsertImageCommand`, `CloseCommand`.
- **`GeoImportViewModel.cs`**:
  - Properties: `SourceIsFile`, `SourceIsText`, `FilePath`, `PastedText`, `PastedIsWgs84`, `PairOrderIndex`, `JoinAsPolyline`, `CloseRing`, `Preview`, `CanDraw`.
  - Commands: `BrowseFileCommand`, `DrawCommand`, `CloseCommand`.
- **`CrsSelectionViewModel.cs`**:
  - Reusable VM bound to `CrsSelectionView`. Handles province selection, meridian resolution, former province hints, and manual overrides.

### 4.3 Styling & MaterialDesign Integration
- **`Theme.xaml`**: Merges `MaterialBridge.xaml`, defining base colors (`Primary #0696D7`, `Secondary #E0641E`) and re-basing standard WPF controls on MaterialDesign2 styles.
- **`MaterialThemeBridge.cs`**: Follows host theme (`HPGeoHostTheme`) via `Application.SystemVariableChanged` on AutoCAD variable `COLORTHEME` (Dark: 0/1; Light: 245). Rebuilds top-level overlay dictionary dynamically at runtime.
- **ALC Contextual Reflection**: `GeoExportWindow` and `GeoImportWindow` wrap their initialization in `using (AssemblyLoadContext.GetLoadContext(assembly).EnterContextualReflection())` to ensure WPF BAML URI lookups resolve within the isolated load context.

---

## 5. `HPGeo.TileFetch` Console Tool

### 5.1 Architecture & Purpose
`HPGeo.TileFetch` is a standalone console application (`HPGeo.TileFetch.exe`) targeting `net8.0` with `OutputType = Exe`.
- **Primary Purpose**: Download map tiles outside the `acad.exe` process.
- **Firewall Isolation**: On developer and corporate CAD workstations, `acad.exe` is frequently restricted by outbound firewall rules (e.g. Windows Firewall rule `Autocad2026` blocking outbound traffic for `acad.exe`). Calling HTTP/HTTPS APIs directly from within `acad.exe` fails with `SocketError.AccessDenied` (`WSAEACCES` 10013). Running a child console process bypasses `acad.exe` firewall rules.
- **Stability**: Prevents AutoCAD main thread and UI message pumping from freezing during large multi-tile downloads.

### 5.2 Execution Protocol (`TileFetchProtocol.cs`)
- **Invocation**:
  ```bash
  HPGeo.TileFetch.exe <request-file.txt>
  ```
- **Request File Format**:
  - Line 1: `provider=<providerId>` (e.g. `esri`)
  - Line 2: `cache=<cacheDirectory>` (e.g. `%LocalAppData%\HPGeo\tiles`)
  - Line 3: `ua=<userAgentString>`
  - Subsequent lines: `<z>/<x>/<y>` (tile coordinates)
- **Standard Output Reporting Protocol**:
  - Progress updates: `progress <count>`
  - Failures: `fail <z>/<x>/<y> <reason>`
  - Completion summary: `done <succeeded_count> <failed_count> <cached_count>`
- **Exit Codes**:
  - `0`: Success (all requested tiles downloaded or cached).
  - `1`: Bad request / invalid arguments / missing request file.
  - `2`: Partial failure (some tiles could not be fetched).
- **Process Management (`HelperTileFetcher.cs`)**:
  - Writes temporary request file in `%TEMP%`.
  - Spawns `HPGeo.TileFetch.exe` with redirected stdout and stderr.
  - Monitors stdout for progress and parses results into `TileFetchResult`.
  - Supports cancellation via `CancellationToken` (kills child process cleanly; partial tiles written with temp names prevent cache corruption).

---

## 6. Test Suite Catalog & Discrepancy Analysis (`HPGeo.Tests`)

### 6.1 Test Count Verification
Running the test suite via the MTP test runner (`HPGeo.Tests.exe`):
```
Test run summary: Passed!
  total: 161
  failed: 0
  succeeded: 158
  skipped: 3 (behind HPGEO_LIVE_TILES=1)
  duration: 1.010s
```

### 6.2 Explanation of the "111 vs 161" Discrepancy
- **111 tests**: Documented in `AGENTS.md` at the conclusion of milestone phase 7 (covering geodetic projections, catalog, basic KML, and view models).
- **+50 tests**: Added during the imagery milestone (phases A-C: `ImageryTests.cs`, `ImageryPipelineTests.cs`, `ImageArgumentsTests.cs`, `TileFetchHelperTests.cs`, `golden-webmercator.json`).
- **Total = 161 tests**: Exactly 161 tests exist in the codebase today.

### 6.3 Test Classes Breakdown
All tests are implemented in `HPGeo.Tests` (targeting `net10.0-windows` with `xunit.v3` 3.1.0):

| Test Class | Category | Test Methods | Description |
|---|---|:---:|---|
| `ProjectionGoldenTests.cs` | Projection Oracle | ~21 | Compares forward Snyder TM against oracle JS output (`golden-vn2000-to-wgs84.json`) and reverse against Proj4 (`golden-wgs84-to-vn2000.json`). |
| `ConverterTests.cs` | Converter & Validation | ~12 | Tests `Vn2000Converter`: valid conversions, unit scaling, envelope errors, plausibility hints. |
| `ProvinceCatalogTests.cs` | Catalog & Division | ~6 | Verifies 34 current and 63 legacy provinces, central meridian counts, former province notes. |
| `ImportTests.cs` | Import Engine | ~8 | Tests KML/KMZ reading, coordinate text regex parsing (Auto/E-N/X-Y), import planning. |
| `KmlTests.cs` | KML / KMZ Generation | ~10 | Tests KML structure, vector markers, 9-decimal coordinates, zip compression. |
| `GeoExportViewModelTests.cs` | WPF ViewModel | ~14 | Tests `GeoExportViewModel`: commands, property changes, validation state, KMZ export path. |
| `GeoImportViewModelTests.cs` | WPF ViewModel | ~5 | Tests `GeoImportViewModel`: file browsing, text pasting, pair order parsing, draw readiness. |
| `ImageryTests.cs` | Imagery Math | ~28 | Tests Web Mercator tile math, bounding box intersections, bicubic warping, affine fit. |
| `ImageArgumentsTests.cs` | Command Parsing | ~6 | Tests `-HPGEOIMAGE` argument parsing (`res`, `zoom`, `area`, `margin`, `handle`, `layer`). |
| `ImageryPipelineTests.cs` | Imagery Pipeline | ~38 | Tests mock tile fetching, mosaic stitching, warping, raster output. (Includes 2 skipped live tests). |
| `TileFetchHelperTests.cs` | Process Helper | ~3 | Tests child process invocation, protocol stdout parsing, exit codes. (Includes 1 skipped live test). |
| `ReviewRegressionTests.cs` | Regression Gates | ~10 | Covers edge cases discovered during code reviews (odd polylines, unclosed rings, zero unit). |

---

## 7. Dependencies, Project Configurations & Packaging

### 7.1 TargetFrameworks & Package Dependencies

| Project | TargetFramework | Output Type | Project References | Package References |
|---|---|---|---|---|
| `HPGeo.Core` | `net8.0` | Library | None | None (100% pure .NET) |
| `HPGeo.AutoCad` | `net8.0-windows` | Library (`EnableDynamicLoading`) | `HPGeo.Core`, `HPGeo.TileFetch` (build-order) | `AutoCAD.NET` [25.1.0] (private/no-runtime)<br/>`CommunityToolkit.Mvvm` 8.4.0<br/>`MaterialDesignThemes` 5.3.2<br/>`ILRepack` 2.0.46<br/>`Microsoft.Web.WebView2` 1.0.4191.47 |
| `HPGeo.AutoCad.Loader` | `net8.0-windows` | Library | `HPGeo.AutoCad` (build-order) | `AutoCAD.NET` [25.1.0] (private/no-runtime) |
| `HPGeo.TileFetch` | `net8.0` | Exe (`UseAppHost`) | `HPGeo.Core` | None |
| `HPGeo.Tests` | `net10.0-windows` | Exe (MTP Runner) | `HPGeo.Core`, `HPGeo.AutoCad` | `xunit.v3` 3.1.0<br/>`xunit.runner.visualstudio` 3.1.5 |

### 7.2 Custom MSBuild Targets
- **`RepackMaterialDesign` (`HPGeo.AutoCad.csproj`)**:
  Executes `ILRepack.exe /union /parallel /noRepackRes` merging `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, and `Microsoft.Xaml.Behaviors.dll` directly into `HPGeo.AutoCad.dll`. Deletes loose toolkit DLLs from the output directory.
- **`CopyTileFetchHelper` (`HPGeo.AutoCad.csproj`)**:
  Copies `HPGeo.TileFetch/bin/.../net8.0/*` to `$(OutDir)TileFetch/` so the bundle deployer packages the companion executable.
- **`DeployBundle` (`HPGeo.AutoCad.Loader.csproj`)**:
  Deploys `Contents\HPGeo.AutoCad.Loader.dll`, `Contents\App\*` (the add-in, dependencies, and `TileFetch`), and `PackageContents.xml` into `%AppData%\Autodesk\ApplicationPlugins\HPGeo.bundle\`.

---

## 8. Migration & Mapping Plan (`HPAutoCad/HPGeoLink` & `HPAutoCad.Core/HPGeoLink`)

Per requirements R1 & R2 from `ORIGINAL_REQUEST.md`:
1. Establish the standard HPRebar multi-project layout in `HPAutoCad`:
   - `HPAutoCad/HPAutoCad.csproj` (The main Add-In project, .NET 8.0-windows, WPF, MVVM).
   - `HPAutoCad/HPAutoCad.Core/HPAutoCad.Core.csproj` (Host-free algorithm engine, .NET 8.0).
   - `HPAutoCad/HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj` (Tile download helper console app).
   - `HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj` (net10.0-windows / xUnit v3 / MTP runner).
2. Package everything into a single unified bundle:
   `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`.

### 8.1 Detailed File-to-File Mapping Table

#### Domain Logic: `HPGeo.Core/` $\longrightarrow$ `HPAutoCad.Core/HPGeoLink/`
| Existing HPGeo File | New Path in HPAutoCad.Core | Namespace Migration |
|---|---|---|
| `Catalog/CentralMeridian.cs` | `HPAutoCad.Core/HPGeoLink/Catalog/CentralMeridian.cs` | `HPAutoCad.Core.HPGeoLink.Catalog` |
| `Catalog/Province.cs` | `HPAutoCad.Core/HPGeoLink/Catalog/Province.cs` | `HPAutoCad.Core.HPGeoLink.Catalog` |
| `Catalog/ProvinceCatalog.cs` | `HPAutoCad.Core/HPGeoLink/Catalog/ProvinceCatalog.cs` | `HPAutoCad.Core.HPGeoLink.Catalog` |
| `Data/vn2000-provinces.json` | `HPAutoCad.Core/HPGeoLink/Data/vn2000-provinces.json` | *EmbeddedResource* (LogicalName updated) |
| `Conversion/ConversionModels.cs` | `HPAutoCad.Core/HPGeoLink/Conversion/ConversionModels.cs` | `HPAutoCad.Core.HPGeoLink.Conversion` |
| `Conversion/ExportArguments.cs` | `HPAutoCad.Core/HPGeoLink/Conversion/ExportArguments.cs` | `HPAutoCad.Core.HPGeoLink.Conversion` |
| `Conversion/Vn2000Converter.cs` | `HPAutoCad.Core/HPGeoLink/Conversion/Vn2000Converter.cs` | `HPAutoCad.Core.HPGeoLink.Conversion` |
| `Geometry/BulgeTessellator.cs` | `HPAutoCad.Core/HPGeoLink/Geometry/BulgeTessellator.cs` | `HPAutoCad.Core.HPGeoLink.Geometry` |
| `Imagery/AffineFit.cs` | `HPAutoCad.Core/HPGeoLink/Imagery/AffineFit.cs` | `HPAutoCad.Core.HPGeoLink.Imagery` |
| `Imagery/ImageArguments.cs` | `HPAutoCad.Core/HPGeoLink/Imagery/ImageArguments.cs` | `HPAutoCad.Core.HPGeoLink.Imagery` |
| `Imagery/ImageZoneResolver.cs` | `HPAutoCad.Core/HPGeoLink/Imagery/ImageZoneResolver.cs` | `HPAutoCad.Core.HPGeoLink.Imagery` |
| `Imagery/ImageryProvider.cs` | `HPAutoCad.Core/HPGeoLink/Imagery/ImageryProvider.cs` | `HPAutoCad.Core.HPGeoLink.Imagery` |
| `Imagery/OutputRaster.cs` | `HPAutoCad.Core/HPGeoLink/Imagery/OutputRaster.cs` | `HPAutoCad.Core.HPGeoLink.Imagery` |
| `Imagery/RasterWarper.cs` | `HPAutoCad.Core/HPGeoLink/Imagery/RasterWarper.cs` | `HPAutoCad.Core.HPGeoLink.Imagery` |
| `Imagery/TileCache.cs` | `HPAutoCad.Core/HPGeoLink/Imagery/TileCache.cs` | `HPAutoCad.Core.HPGeoLink.Imagery` |
| `Imagery/TileCoverage.cs` | `HPAutoCad.Core/HPGeoLink/Imagery/TileCoverage.cs` | `HPAutoCad.Core.HPGeoLink.Imagery` |
| `Imagery/TileFetchProtocol.cs` | `HPAutoCad.Core/HPGeoLink/Imagery/TileFetchProtocol.cs` | `HPAutoCad.Core.HPGeoLink.Imagery` |
| `Imagery/TileFetcher.cs` | `HPAutoCad.Core/HPGeoLink/Imagery/TileFetcher.cs` | `HPAutoCad.Core.HPGeoLink.Imagery` |
| `Imagery/WebMercator.cs` | `HPAutoCad.Core/HPGeoLink/Imagery/WebMercator.cs` | `HPAutoCad.Core.HPGeoLink.Imagery` |
| `Import/CoordinateTextParser.cs` | `HPAutoCad.Core/HPGeoLink/Import/CoordinateTextParser.cs` | `HPAutoCad.Core.HPGeoLink.Import` |
| `Import/ImportPlanner.cs` | `HPAutoCad.Core/HPGeoLink/Import/ImportPlanner.cs` | `HPAutoCad.Core.HPGeoLink.Import` |
| `Import/KmlReader.cs` | `HPAutoCad.Core/HPGeoLink/Import/KmlReader.cs` | `HPAutoCad.Core.HPGeoLink.Import` |
| `Kml/KmlColor.cs` | `HPAutoCad.Core/HPGeoLink/Kml/KmlColor.cs` | `HPAutoCad.Core.HPGeoLink.Kml` |
| `Kml/KmlDocumentBuilder.cs` | `HPAutoCad.Core/HPGeoLink/Kml/KmlDocumentBuilder.cs` | `HPAutoCad.Core.HPGeoLink.Kml` |
| `Kml/KmlExportOptions.cs` | `HPAutoCad.Core/HPGeoLink/Kml/KmlExportOptions.cs` | `HPAutoCad.Core.HPGeoLink.Kml` |
| `Kml/KmlGeoMath.cs` | `HPAutoCad.Core/HPGeoLink/Kml/KmlGeoMath.cs` | `HPAutoCad.Core.HPGeoLink.Kml` |
| `Kml/KmzExportPipeline.cs` | `HPAutoCad.Core/HPGeoLink/Kml/KmzExportPipeline.cs` | `HPAutoCad.Core.HPGeoLink.Kml` |
| `Kml/KmzWriter.cs` | `HPAutoCad.Core/HPGeoLink/Kml/KmzWriter.cs` | `HPAutoCad.Core.HPGeoLink.Kml` |
| `Model/GeoPoint.cs` | `HPAutoCad.Core/HPGeoLink/Model/GeoPoint.cs` | `HPAutoCad.Core.HPGeoLink.Model` |
| `Model/PlanePoint.cs` | `HPAutoCad.Core/HPGeoLink/Model/PlanePoint.cs` | `HPAutoCad.Core.HPGeoLink.Model` |
| `Projection/Ellipsoid.cs` | `HPAutoCad.Core/HPGeoLink/Projection/Ellipsoid.cs` | `HPAutoCad.Core.HPGeoLink.Projection` |
| `Projection/GeocentricConverter.cs`| `HPAutoCad.Core/HPGeoLink/Projection/GeocentricConverter.cs`| `HPAutoCad.Core.HPGeoLink.Projection` |
| `Projection/Helmert7.cs` | `HPAutoCad.Core/HPGeoLink/Projection/Helmert7.cs` | `HPAutoCad.Core.HPGeoLink.Projection` |
| `Projection/TmParameters.cs` | `HPAutoCad.Core/HPGeoLink/Projection/TmParameters.cs` | `HPAutoCad.Core.HPGeoLink.Projection` |
| `Projection/TransverseMercator.cs`| `HPAutoCad.Core/HPGeoLink/Projection/TransverseMercator.cs`| `HPAutoCad.Core.HPGeoLink.Projection` |
| `Projection/Vn2000Wgs84Transform.cs`| `HPAutoCad.Core/HPGeoLink/Projection/Vn2000Wgs84Transform.cs`| `HPAutoCad.Core.HPGeoLink.Projection` |
| `Settings/GeoSettings.cs` | `HPAutoCad.Core/HPGeoLink/Settings/GeoSettings.cs` | `HPAutoCad.Core.HPGeoLink.Settings` |
| `Text/WildcardPattern.cs` | `HPAutoCad.Core/HPGeoLink/Text/WildcardPattern.cs` | `HPAutoCad.Core.HPGeoLink.Text` |
| `Units/DrawingUnitFactor.cs` | `HPAutoCad.Core/HPGeoLink/Units/DrawingUnitFactor.cs` | `HPAutoCad.Core.HPGeoLink.Units` |
| `Validation/PlausibilityCheck.cs` | `HPAutoCad.Core/HPGeoLink/Validation/PlausibilityCheck.cs` | `HPAutoCad.Core.HPGeoLink.Validation` |
| `Validation/VietnamEnvelope.cs` | `HPAutoCad.Core/HPGeoLink/Validation/VietnamEnvelope.cs` | `HPAutoCad.Core.HPGeoLink.Validation` |

#### AutoCAD UI & CAD Feature: `HPGeo.AutoCad/` $\longrightarrow$ `HPAutoCad/HPGeoLink/`
| Existing HPGeo File | New Path in HPAutoCad | Category / Role |
|---|---|---|
| `Commands/HPGeoDialogCommand.cs` | `HPAutoCad/HPGeoLink/Commands/HPGeoDialogCommand.cs` | Command |
| `Commands/HPGeoKmzScriptCommand.cs` | `HPAutoCad/HPGeoLink/Commands/HPGeoKmzScriptCommand.cs` | Command |
| `Commands/HPGeoImportCommand.cs` | `HPAutoCad/HPGeoLink/Commands/HPGeoImportCommand.cs` | Command |
| `Commands/HPGeoImportScriptCommand.cs` | `HPAutoCad/HPGeoLink/Commands/HPGeoImportScriptCommand.cs`| Command |
| `Commands/HPGeoImageScriptCommand.cs` | `HPAutoCad/HPGeoLink/Commands/HPGeoImageScriptCommand.cs` | Command |
| `Commands/HPGeoInfoCommand.cs` | `HPAutoCad/HPGeoLink/Commands/HPGeoInfoCommand.cs` | Command |
| `Commands/ImageryConsole.cs` | `HPAutoCad/HPGeoLink/Commands/ImageryConsole.cs` | Command helper |
| `Cad/DrawingContext.cs` | `HPAutoCad/HPGeoLink/Service/DrawingContext.cs` | CAD Service |
| `Cad/DrawingReader.cs` | `HPAutoCad/HPGeoLink/Service/DrawingReader.cs` | CAD Service |
| `Cad/DrawingWriter.cs` | `HPAutoCad/HPGeoLink/Service/DrawingWriter.cs` | CAD Service |
| `Cad/DocumentSettingsStore.cs` | `HPAutoCad/HPGeoLink/Service/DocumentSettingsStore.cs` | CAD Service (Xrecord) |
| `Cad/UserSettingsStore.cs` | `HPAutoCad/HPGeoLink/Service/UserSettingsStore.cs` | CAD Service (JSON) |
| `Imagery/HelperTileFetcher.cs` | `HPAutoCad/HPGeoLink/Service/HelperTileFetcher.cs` | Imagery Service |
| `Imagery/ImageryPipeline.cs` | `HPAutoCad/HPGeoLink/Service/ImageryPipeline.cs` | Imagery Service |
| `Imagery/RasterInserter.cs` | `HPAutoCad/HPGeoLink/Service/RasterInserter.cs` | Imagery Service |
| `Imagery/TileStitcher.cs` | `HPAutoCad/HPGeoLink/Service/TileStitcher.cs` | Imagery Service |
| `GoogleEarthLauncher.cs` | `HPAutoCad/HPGeoLink/Service/GoogleEarthLauncher.cs` | OS Service |
| `HPGeoLog.cs` | `HPAutoCad/HPGeoLink/Service/GeoLog.cs` | Logging Service |
| `UI/GeoExportViewModel.cs` | `HPAutoCad/HPGeoLink/ViewModel/GeoExportViewModel.cs` | ViewModel |
| `UI/GeoExportViewModel.Commands.cs` | `HPAutoCad/HPGeoLink/ViewModel/GeoExportViewModel.Commands.cs`| ViewModel |
| `UI/GeoImportViewModel.cs` | `HPAutoCad/HPGeoLink/ViewModel/GeoImportViewModel.cs` | ViewModel |
| `UI/CrsSelectionViewModel.cs` | `HPAutoCad/HPGeoLink/ViewModel/CrsSelectionViewModel.cs` | ViewModel |
| `UI/GeoExportItems.cs` | `HPAutoCad/HPGeoLink/Model/GeoExportItems.cs` | View Item Model |
| `UI/IGeoExportShell.cs` | `HPAutoCad/HPGeoLink/ViewModel/IGeoExportShell.cs` | Interface |
| `UI/GeoExportWindow.xaml` / `.cs` | `HPAutoCad/HPGeoLink/View/GeoExportWindow.xaml` / `.cs` | View (Window) |
| `UI/GeoImportWindow.xaml` / `.cs` | `HPAutoCad/HPGeoLink/View/GeoImportWindow.xaml` / `.cs` | View (Window) |
| `UI/CrsSelectionView.xaml` / `.cs` | `HPAutoCad/HPGeoLink/View/CrsSelectionView.xaml` / `.cs` | View (UserControl) |
| `UI/MapPanel.xaml` / `.cs` | `HPAutoCad/HPGeoLink/View/MapPanel.xaml` / `.cs` | View (UserControl) |
| `UI/MapHtml.cs` | `HPAutoCad/HPGeoLink/View/MapHtml.cs` | View Helper |
| `UI/ValueConverters.cs` | `HPAutoCad/HPGeoLink/View/ValueConverters.cs` | View Helper |

#### Companion Tile Tool: `HPGeo.TileFetch/` $\longrightarrow$ `HPAutoCad.TileFetch/`
- All source files migrate to `HPAutoCad/HPAutoCad.TileFetch/` (`Program.cs`, `HPAutoCad.TileFetch.csproj`).
- Project references `HPAutoCad.Core.csproj`.
- Emits `HPAutoCad.TileFetch.exe` copied into bundle `Contents\App\TileFetch\`.

#### Unit Tests: `HPGeo.Tests/` $\longrightarrow$ `HPAutoCad.Tests/`
- All 12 test fixture files + `GoldenFixtures.cs` + 3 JSON files migrate to `HPAutoCad/HPAutoCad.Tests/` (`net10.0-windows`).
- All 161 test cases preserved and passing 100%.

---

## 9. Key Technical Invariants, Risks & Recommendations

### 9.1 Technical Invariants
1. **Embedded Resource Naming**:
   In `ProvinceCatalog.cs`, the manifest resource is currently loaded via string:
   `"HPGeo.Core.Data.vn2000-provinces.json"`.
   Upon migration to `HPAutoCad.Core`, this must be updated in both `ProvinceCatalog.cs` and `HPAutoCad.Core.csproj` (`LogicalName="HPAutoCad.Core.HPGeoLink.Data.vn2000-provinces.json"`).
2. **ILRepack and BAML Collision Prevention**:
   Because WPF resolves BAML types by simple assembly name and takes the last-loaded copy across `AssemblyLoadContexts`, `MaterialDesignThemes` MUST be merged directly into `HPAutoCad.dll` via `ILRepack`. Loose `MaterialDesignThemes.Wpf.dll` must be deleted before deployment.
3. **`WebView2Loader.dll` Native Dependency**:
   Ensure `runtimes\win-x64\native\WebView2Loader.dll` is bundled into `Contents\App\runtimes\win-x64\native\` (or directly in `Contents\App\`) so WebView2 can initialize correctly on workstations that do not have the native loader in their system PATH.
4. **Backward Compatibility with Civil 3D Mirror Tests**:
   `HPCivil3d.McpBridge.Tests` verifies 24 files in `HPAutoCad.McpBridge` against `HPCivil3d.McpBridge` via `HPCivil3d/tools/mirror-tokens.json` and SHA256 hashes.
   **Crucial Rule**: The migration of `HPGeo` into `HPAutoCad` must NOT modify the mirrored files of `HPAutoCad.McpBridge` or `HPAutoCad.McpBridge.Loader` without updating `mirror-tokens.json`, or the 60 Civil 3D mirror tests will fail. The loader consolidation should either preserve `HPAutoCad.McpBridge.Loader` intact or carefully account for mirror assertions.
5. **TileFetch Helper Executable Naming**:
   `HelperTileFetcher.cs` searches for `TileFetch\HPGeo.TileFetch.exe`. When renamed to `HPAutoCad.TileFetch.exe`, update `HelperTileFetcher.HelperExe` and the MSBuild copy target accordingly.
6. **Cadastral Coordinate Orientation Rule**:
   Surveyors often write $X$ for Northing and $Y$ for Easting on paper. In AutoCAD drawings, $X$ is ALWAYS Easting and $Y$ is ALWAYS Northing. The add-in must maintain this strict convention and only diagnose plausible axis swaps rather than altering coordinate geometry.

---

## 10. Verification Method for Downstream Implementation
1. **Static Build**:
   ```bash
   dotnet build HPAutoCad/HPAutoCad.slnx -c Debug -p:DeployBundle=false
   ```
   Must succeed with 0 errors and 0 warnings treated as errors across all targets.
2. **Local Unit Tests**:
   ```bash
   dotnet test HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj -p:DeployBundle=false
   ```
   Must execute all 161 tests (158 passed, 3 skipped for live network) with 0 failures.
3. **Mirror Test Verification**:
   ```bash
   dotnet test HPCivil3d/HPCivil3d.McpBridge.Tests/HPCivil3d.McpBridge.Tests.csproj -p:DeployBundle=false
   ```
   Must pass 60/60 tests to guarantee zero drift.
4. **Live Verification Loop**:
   Deploy bundle to `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`. Launch AutoCAD 2026 via unattended harness (`HPAutoCad/tools/harness/run-bridge-unattended.ps1`), verify Ribbon tab `HPAutoCad` with both panels (`MCP` and `HPGeoLink`), and execute test commands (`HPGEOINFO`, `-HPGEOKMZ`, `-HPGEOIMPORT`).
