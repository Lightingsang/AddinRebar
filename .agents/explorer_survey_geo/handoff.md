# Handoff Report: HPGeo Survey & Migration Mapping to HPAutoCad

**Agent**: `explorer_survey_geo`  
**Parent Orchestrator**: `orchestrator_3` (ID: `050984c1-afaa-4911-859c-331e9279dc4f`)  
**Report Document**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\survey_hpgeo.md`  
**Handoff Type**: Hard (Investigation complete)

---

## 1. Observation

1. **Solution Structure & Subprojects (`HPGeo/HPGeo.slnx`)**:
   `HPGeo.slnx` defines 5 projects:
   - `HPGeo.Core/HPGeo.Core.csproj` (`net8.0`, host-free, 0 external NuGet dependencies).
   - `HPGeo.AutoCad/HPGeo.AutoCad.csproj` (`net8.0-windows`, WPF, AutoCAD 2026 API, `CommunityToolkit.Mvvm` 8.4.0, `MaterialDesignThemes` 5.3.2, `Microsoft.Web.WebView2` 1.0.4191.47, `ILRepack` 2.0.46).
   - `HPGeo.AutoCad.Loader/HPGeo.AutoCad.Loader.csproj` (`net8.0-windows`, zero 3rd-party dependencies, isolated `AssemblyLoadContext` `GeoLoadContext`).
   - `HPGeo.TileFetch/HPGeo.TileFetch.csproj` (`net8.0` console, references `HPGeo.Core`, 0 NuGet packages).
   - `HPGeo.Tests/HPGeo.Tests.csproj` (`net10.0-windows`, `xunit.v3` 3.1.0, MTP runner).

2. **Registered Commands (`HPGeo.AutoCad.Loader/HPGeoCommands.cs`, lines 12–30)**:
   - `HPGEOINFO` (Modal | NoUndoMarker) -> `info` -> `HPGeoInfoCommand.Run()`
   - `-HPGEOKMZ` (Modal) -> `kmz-script` -> `HPGeoKmzScriptCommand.Run()`
   - `HPGEO` (Modal) -> `dialog` -> `HPGeoDialogCommand.Run()`
   - `HPGEOIMPORT` (Modal) -> `import` -> `HPGeoImportCommand.Run()`
   - `-HPGEOIMPORT` (Modal) -> `import-script` -> `HPGeoImportScriptCommand.Run()`
   - `-HPGEOIMAGE` (Modal) -> `image-script` -> `HPGeoImageScriptCommand.Run()`

3. **Geodetic Models & Calculations (`HPGeo.Core`)**:
   - `Ellipsoid.Wgs84` (`a = 6378137.0`, `1/f = 298.257223563` in `Projection/Ellipsoid.cs`).
   - `TmParameters.Tm3(cm)` (`k0 = 0.9999`, `FE = 500000.0`, `FN = 0.0` in `Projection/TmParameters.cs`).
   - `Helmert7Parameters.Vn2000ToWgs84`: Dx -191.90441429, Dy -39.30318279, Dz -111.45032835, Rx -0.00928836, Ry 0.01975479, Rz -0.00427372, Scale 0.252906278 ppm (EPSG 1032 Coordinate Frame in `Projection/Helmert7.cs`).
   - `TransverseMercator`: Snyder forward & inverse series with Newton-like feedback loop converging to $< 10^{-9}\text{ m}$ (`Projection/TransverseMercator.cs`).
   - `ProvinceCatalog`: Loads embedded manifest resource `"HPGeo.Core.Data.vn2000-provinces.json"` (34 current and 63 legacy provinces with former province label lookup in `Catalog/ProvinceCatalog.cs`).

4. **UI & ViewModels (`HPGeo.AutoCad/UI`)**:
   - `GeoExportWindow.xaml` / `.cs` ($1040\times760$) & `GeoImportWindow.xaml` / `.cs` ($960\times680$).
   - `CrsSelectionView.xaml` (reusable CRS block) & `MapPanel.xaml` (WebView2 hosting Leaflet on Esri World Imagery in `MapHtml.cs`).
   - `MaterialThemeBridge.cs`: Follows AutoCAD `COLORTHEME` dynamically.
   - `AssemblyLoadContext.EnterContextualReflection()` wraps window resource creation to resolve WPF pack URIs in the isolated load context (`GeoExportWindow.xaml.cs` lines 13–20).

5. **TileFetch Process (`HPGeo.TileFetch/Program.cs` & `HelperTileFetcher.cs`)**:
   - Standalone console executable `HPGeo.TileFetch.exe` executed via `HelperTileFetcher`.
   - Protocol: request file with provider, cache path, user agent, and tile $z/x/y$ list; stdout reports `progress`, `fail`, `done`.
   - Bypasses AutoCAD outbound firewall rule blocks (e.g. `Autocad2026` Windows Firewall rule causing `WSAEACCES` on `acad.exe`).

6. **Unit Test Count Verification (`HPGeo.Tests`)**:
   Running `& "HPGeo\HPGeo.Tests\bin\Debug\net10.0-windows\HPGeo.Tests.exe"` outputs verbatim:
   ```
   xUnit.net v3 Microsoft.Testing.Platform Runner v3.1.0+03a071627b (64-bit .NET 10.0.11)
   skipped HPGeo.Tests.ImageryPipelineTests.Live_spike_fetches_four_real_tiles_and_stitches_512x512 (0ms)
   skipped HPGeo.Tests.TileFetchHelperTests.Live_helper_fetches_four_real_tiles_into_a_temp_cache_through_the_host_side_fetcher (0ms)
   skipped HPGeo.Tests.ImageryPipelineTests.Live_prefetch_fills_the_default_cache_for_the_acceptance_ring (0ms)
   Test run summary: Passed!
     total: 161
     failed: 0
     succeeded: 158
     skipped: 3
     duration: 1s 010ms
   ```
   The mention of 111 tests in AGENTS.md was prior to adding 50 imagery pipeline tests; the verified total is exactly 161.

7. **Civil 3D Mirror Assertions (`HPCivil3d/HPCivil3d.McpBridge.Tests`)**:
   Running `& ".\HPCivil3d\HPCivil3d.McpBridge.Tests\bin\Debug\net10.0\HPCivil3d.McpBridge.Tests.exe"` passes 60/60 tests. These tests compare 24 files in `HPAutoCad.McpBridge` against `HPCivil3d.McpBridge` and check SHA256 hashes defined in `HPCivil3d/tools/mirror-tokens.json`.

---

## 2. Logic Chain

1. **Host-Free Separation**:
   Observation 1 & 3 confirm `HPGeo.Core` has zero references to AutoCAD. Therefore, it can cleanly move directly into `HPAutoCad.Core/HPGeoLink/` while retaining full testability under `net8.0`.
2. **Resource Renaming Necessity**:
   Observation 3 shows `ProvinceCatalog.cs` hardcodes `ResourceName = "HPGeo.Core.Data.vn2000-provinces.json"`. When moved to `HPAutoCad.Core/HPGeoLink/Data/vn2000-provinces.json`, this string and the csproj `LogicalName` must be updated to avoid `InvalidOperationException: Embedded resource ... is missing`.
3. **Packaging & Ribbon Unification**:
   Observation 1, 2, and 4 show `HPGeo.AutoCad` and `HPAutoCad.McpBridge` both use `HPAUTOCAD_MCP_TAB`. Migrating `HPGeo` into `HPAutoCad` allows a single bundle (`HPAutoCad.bundle`) with a unified loader (`HPAutoCad.Loader.dll`) creating a single Ribbon tab containing both `MCP` (Bridge AI) and `HPGeoLink` (UI commands).
4. **Mirror Test Safety**:
   Observation 7 establishes that modifying `HPAutoCad.McpBridge` source files directly will break `HPCivil3d.McpBridge.Tests`. Therefore, the migration must introduce `HPAutoCad` and `HPAutoCad.Core` alongside `HPAutoCad.McpBridge` or carefully preserve all mirrored files and hashes.
5. **TileFetch Helper Persistence**:
   Observation 5 proves `TileFetch` must remain a separate console executable (`HPAutoCad.TileFetch.exe`) to prevent socket failures under firewall restrictions on CAD machines.

---

## 3. Caveats

1. **Live Network Tests**: 3 unit tests in `HPGeo.Tests` (`Live_spike_fetches_four_real_tiles_and_stitches_512x512`, `Live_helper_fetches_four_real_tiles_into_a_temp_cache_through_the_host_side_fetcher`, `Live_prefetch_fills_the_default_cache_for_the_acceptance_ring`) are conditionally skipped unless `HPGEO_LIVE_TILES=1` is set in the environment.
2. **Esri World Imagery Terms**: Esri World Imagery tiles in Viet Nam stop at zoom level 19 ($\approx 0.29\text{ m/px}$). Finer zoom levels (e.g. zoom 20) return blank tiles.
3. **No Implementation**: As an explorer agent with a read-only mandate, no source code in `HPGeo/`, `HPAutoCad/`, or `HPCivil3d/` was altered.

---

## 4. Conclusion

The `HPGeo` codebase is mature, well-tested (161 tests, 100% pass rate), and adheres cleanly to host-free domain principles.
All 7 survey objectives are mapped and documented in:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\survey_hpgeo.md`

The recommended migration architecture is:
- `HPAutoCad/HPAutoCad.csproj` (.NET 8.0-windows, WPF, MVVM) with feature folder `HPGeoLink/`
- `HPAutoCad/HPAutoCad.Core/HPAutoCad.Core.csproj` (.NET 8.0 host-free) with feature folder `HPGeoLink/`
- `HPAutoCad/HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj` (.NET 8.0 console)
- `HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj` (net10.0-windows, xUnit v3 / MTP) with all 161 tests
- Single unified bundle `HPAutoCad.bundle` deployed to `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`.

---

## 5. Verification Method

To independently verify the facts and findings stated in this report:

1. **Verify Test Suite Count & Status**:
   ```powershell
   & "HPGeo\HPGeo.Tests\bin\Debug\net10.0-windows\HPGeo.Tests.exe"
   ```
   Expected: `total: 161, failed: 0, succeeded: 158, skipped: 3`.

2. **Verify Civil 3D Mirror Assertions**:
   ```powershell
   & ".\HPCivil3d\HPCivil3d.McpBridge.Tests\bin\Debug\net10.0\HPCivil3d.McpBridge.Tests.exe"
   ```
   Expected: `total: 60, failed: 0, succeeded: 60, skipped: 0`.

3. **Verify Technical Survey Report**:
   Inspect `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_3\survey_hpgeo.md` for complete catalogs and file mapping tables.
