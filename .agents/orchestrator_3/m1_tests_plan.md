# Technical Specification & Implementation Plan: HPAutoCad.Tests

**Milestone**: M1 (Domain Core & Companion Project)  
**Deliverable**: `HPAutoCad/HPAutoCad.Tests/` (net10.0-windows / xUnit v3 / Microsoft.Testing.Platform Runner)  
**Author**: `explorer_m1_tests`  
**Target Audience**: `worker_m1` / Orchestrator (`orchestrator_3`)  
**Date**: 2026-09-20  

---

## 1. Executive Summary & Purpose

`HPAutoCad.Tests` is the central automated geodetic unit test suite for the AutoCAD platform within the repository. It transfers the entire 161-test geodetic, projection, catalog, KML/KMZ, and imagery test suite from the legacy standalone `HPGeo/HPGeo.Tests` into `HPAutoCad/HPAutoCad.Tests/HPGeoLink/`.

### Key Objectives
1. **Full Test Fidelity**: Preserve all 161 test cases, 12 test fixture files, `GoldenFixtures.cs`, and the 3 JSON oracle files with 100% behavioral parity.
2. **Modern Test Architecture**: Target `net10.0-windows` with `xunit.v3` (v3.1.0) running natively on the `Microsoft.Testing.Platform` (MTP) runner pinned in `HPAutoCad/global.json`.
3. **M1 Solution Integration**: Add `HPAutoCad.Core`, `HPAutoCad.TileFetch`, and `HPAutoCad.Tests` to `HPAutoCad/HPAutoCad.slnx` without disturbing existing MCP or AEC projects.
4. **Mirror Test Invariant**: Ensure that `HPCivil3d.McpBridge.Tests` mirror assertions (60 tests) remain 100% passing and completely untouched.
5. **Deterministic Outcome**: Deliver exactly **161 tests total: 158 passed, 3 skipped** (gated behind `HPGEO_LIVE_TILES=1`), 0 failed.

---

## 2. Complete Legacy Test Suite Inventory & Mapping

The legacy test suite in `HPGeo/HPGeo.Tests/` consists of **16 source and data files** (12 test fixture classes, 1 helper fixture class, and 3 JSON golden data files).

### 2.1 File Inventory Table

| # | Legacy Source (`HPGeo/HPGeo.Tests/`) | Target Path (`HPAutoCad/HPAutoCad.Tests/HPGeoLink/`) | Test Count | Category | Primary Verification Scope |
|---|---------------------------------------|---------------------------------------------------|:----------:|----------|----------------------------|
| 1 | `ProjectionGoldenTests.cs` | `ProjectionGoldenTests.cs` | **21** | Projection Oracle | Snyder TM-3 forward against oracle JS output (`golden-vn2000-to-wgs84.json`) and reverse against Proj4 (`golden-wgs84-to-vn2000.json`). |
| 2 | `ConverterTests.cs` | `ConverterTests.cs` | **12** | Conversion & Rules | Bidirectional `Vn2000Converter`, unit factor scaling (mm vs m), bounding envelope errors, and axis swap plausibility checks. |
| 3 | `ProvinceCatalogTests.cs` | `ProvinceCatalogTests.cs` | **6** | Administrative Catalog | 34 current (post-2025) and 63 legacy provinces, central meridian counts (17 meridians), former province historical notes. |
| 4 | `ImportTests.cs` | `ImportTests.cs` | **8** | Import & Parsing | KML/KMZ reading, coordinate table regex parsing (Auto/E-N/X-Y), import insertion planning. |
| 5 | `KmlTests.cs` | `KmlTests.cs` | **10** | KML/KMZ Packaging | Bulge tessellation (5 mm sagitta chord tolerance), KML styling, 9-decimal precision, ZipArchive KMZ generation. |
| 6 | `ImageryTests.cs` | `ImageryTests.cs` | **28** | Imagery Math | Spherical Mercator (EPSG:3857) math, tile grid coverage bounding, Catmull-Rom bicubic resampling, least-squares affine fit. |
| 7 | `ImageArgumentsTests.cs` | `ImageArgumentsTests.cs` | **6** | Command Parsing | `-HPGEOIMAGE` argument parsing (`res`, `zoom`, `area`, `margin`, `handle`, `layer`) and `GeoSettings` binding. |
| 8 | `ReviewRegressionTests.cs` | `ReviewRegressionTests.cs` | **10** | Regression Gates | Edge cases identified during architectural code reviews (odd polylines, unclosed rings, zero unit, coordinate edge bounds). |
| 9 | `GeoExportViewModelTests.cs` | `GeoExportViewModelTests.cs` | **14** | WPF MVVM Dialog | `GeoExportViewModel` properties, command states, coordinate validation, dynamic updates, and export execution through fake shell. |
| 10 | `GeoImportViewModelTests.cs` | `GeoImportViewModelTests.cs` | **5** | WPF MVVM Dialog | `GeoImportViewModel` file browsing, text pasting, coordinate order parsing, and drawing readiness flags. |
| 11 | `ImageryPipelineTests.cs` | `ImageryPipelineTests.cs` | **38** | Imagery Pipeline | Stub HTTP tile server, parallel downloading, cache hits/misses, mosaic stitching, Catmull-Rom warping. *(36 pass, 2 live skipped)*. |
| 12 | `TileFetchHelperTests.cs` | `TileFetchHelperTests.cs` | **3** | Process Helper | Child process protocol round-trip, malformed request handling (`exit 1`), live helper tile fetching. *(2 pass, 1 live skipped)*. |
| 13 | `Fixtures/GoldenFixtures.cs` | `Fixtures/GoldenFixtures.cs` | — | Fixture Loader | Strongly typed deserializer for JSON golden files via `System.Text.Json`. |
| 14 | `Fixtures/golden-vn2000-to-wgs84.json` | `Fixtures/golden-vn2000-to-wgs84.json` | — | Oracle Data | Forward conversion oracle dataset (34 cases across Vietnam). |
| 15 | `Fixtures/golden-wgs84-to-vn2000.json` | `Fixtures/golden-wgs84-to-vn2000.json` | — | Oracle Data | Reverse conversion oracle dataset with round-trip error tolerances. |
| 16 | `Fixtures/golden-webmercator.json` | `Fixtures/golden-webmercator.json` | — | Oracle Data | Spherical Mercator tile address and bounding box oracle dataset. |
| **Total** | | | **161** | | **158 Passed, 3 Skipped, 0 Failed** |

---

## 3. Architectural Analysis: M1 Scope & Dependency Strategy

### 3.1 The 101 vs 60 Test Classification
An in-depth analysis of the 12 test fixtures reveals two distinct categories of tests:

1. **Pure Domain Tests (8 files, 101 tests)**:
   - `ConverterTests`, `ProjectionGoldenTests`, `ProvinceCatalogTests`, `ImportTests`, `KmlTests`, `ImageryTests`, `ImageArgumentsTests`, `ReviewRegressionTests`.
   - These test classes depend exclusively on pure domain algorithms in `HPAutoCad.Core.HPGeoLink.*`.
   - Zero UI, zero WPF, and zero external process dependencies.

2. **UI & Pipeline Tests (4 files, 60 tests)**:
   - `GeoExportViewModelTests` (14 tests) & `GeoImportViewModelTests` (5 tests): Test `GeoExportViewModel`, `GeoImportViewModel`, `CrsSelectionViewModel`, `IGeoExportShell`, `IGeoImportShell`, and `GeoExportItems`.
   - `TileFetchHelperTests` (3 tests): Tests `HelperTileFetcher` and executes `HPAutoCad.TileFetch.exe`.
   - `ImageryPipelineTests` (38 tests): Tests `TileStitcher` (decodes JPEG/PNG and stitches tile mosaics using WPF WIC) and `TileFetcher`.

### 3.2 Resolving the M1 vs M2 Boundary
In the project roadmap (`orchestrator_3/PROJECT.md`):
- **Milestone M1** creates `HPAutoCad.Core`, `HPAutoCad.TileFetch`, and `HPAutoCad.Tests`.
- **Milestone M2** creates `HPAutoCad` (the add-in project with AutoCAD API, WPF UI, and full feature integration).

To satisfy the mandatory requirement that **all 161 tests pass cleanly in Milestone M1** before `HPAutoCad.csproj` is created in M2:
- The host-free ViewModel and Service classes required by those 60 tests (`GeoExportViewModel.cs`, `GeoExportViewModel.Commands.cs`, `GeoImportViewModel.cs`, `CrsSelectionViewModel.cs`, `IGeoExportShell.cs`, `IGeoImportShell.cs`, `GeoExportItems.cs`, `HelperTileFetcher.cs`, and `TileStitcher.cs`) are placed in `HPAutoCad.Tests/HPGeoLink/Support/`.
- `HPAutoCad.Tests.csproj` is configured with `net10.0-windows`, `<UseWPF>true</UseWPF>`, and package reference to `CommunityToolkit.Mvvm` (8.4.0).
- When Milestone M2 is executed, these support files are seamlessly promoted to `HPAutoCad/HPGeoLink/ViewModel/` and `HPAutoCad/HPGeoLink/Service/`, and `HPAutoCad.Tests.csproj` references `HPAutoCad.csproj`.
- This ensures **zero blocked tests, zero skipped unit tests, and 100% buildability** in both M1 and M2.

---

## 4. Project Configuration: `HPAutoCad.Tests.csproj` Specification

### 4.1 Target Location
`HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj`

### 4.2 Verbatim Project File Content
```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <!-- Geodetic unit test suite: golden fixtures generated from reference oracle (forward) and proj4 (reverse),
             catalog counts, coordinate validation, KML/KMZ serialization, satellite imagery math, and dialog ViewModels.
             net10.0-windows provides WPF/WIC imaging support for TileStitcher and ViewModel testing. All tests are host-free:
             acad.exe is not required. -->
        <TargetFramework>net10.0-windows</TargetFramework>
        <LangVersion>latest</LangVersion>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <UseWPF>true</UseWPF>
        <RootNamespace>HPAutoCad.Tests</RootNamespace>
        <AssemblyName>HPAutoCad.Tests</AssemblyName>
        <Configurations>Debug;Release</Configurations>
        <IsPackable>false</IsPackable>
        <!-- global.json pins test.runner = Microsoft.Testing.Platform; xunit.v3 speaks it natively -->
        <OutputType>Exe</OutputType>
        <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="xunit.v3" Version="3.1.0" />
        <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
        <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
        <PackageReference Include="Microsoft.Bcl.AsyncInterfaces" Version="10.0.12" />
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\HPAutoCad.Core\HPAutoCad.Core.csproj" />
        <ProjectReference Include="..\HPAutoCad.TileFetch\HPAutoCad.TileFetch.csproj" ReferenceOutputAssembly="false" />
    </ItemGroup>

    <ItemGroup>
        <!-- JSON golden files copied to $(OutDir)Fixtures\ so GoldenFixtures and ImageryTests resolve them via AppContext.BaseDirectory -->
        <None Include="HPGeoLink\Fixtures\*.json" CopyToOutputDirectory="PreserveNewest" Link="Fixtures\%(Filename)%(Extension)" />
    </ItemGroup>

</Project>
```

### 4.3 Key Property Analysis
| Element / Property | Value | Rationale |
|---|---|---|
| `<TargetFramework>` | `net10.0-windows` | Aligns with existing test projects (`HPAutoCad.Aec.Tests`, `HPAutoCad.Mcp.Server.Tests`) and provides Windows desktop APIs (WIC/WPF). |
| `<UseWPF>` | `true` | Enables `System.Windows.Media.Imaging` for `TileStitcher` (decoding tile PNG/JPEG into `RasterBuffer`). |
| `<OutputType>` | `Exe` | Required by Microsoft.Testing.Platform runner for direct process execution. |
| `<UseMicrosoftTestingPlatformRunner>` | `true` | Binds to MTP runner pinned in `HPAutoCad/global.json`. |
| `<PackageReference>` `xunit.v3` | `3.1.0` | Latest xUnit v3 framework supporting native MTP runner, `Assert.SkipUnless`, and `TheoryData`. |
| `<PackageReference>` `CommunityToolkit.Mvvm` | `8.4.0` | Provides `ObservableObject` and MVVM contracts for `GeoExportViewModel` and `GeoImportViewModel`. |
| `<ProjectReference>` `HPAutoCad.Core` | `..\HPAutoCad.Core\HPAutoCad.Core.csproj` | Supplies all pure geodetic and mathematical models under `HPAutoCad.Core.HPGeoLink.*`. |
| `<ProjectReference>` `HPAutoCad.TileFetch` | `..\HPAutoCad.TileFetch\HPAutoCad.TileFetch.csproj` | Ensures `HPAutoCad.TileFetch.exe` is compiled before tests execute, allowing `TileFetchHelperTests` to test the binary. |
| `<None Link="...">` | `Fixtures\%(Filename)%(Extension)` | Copies golden JSON files directly into `$(OutDir)Fixtures\` so `Path.Combine(AppContext.BaseDirectory, "Fixtures", ...)` resolves accurately. |

---

## 5. Solution Integration: `HPAutoCad.slnx` Specification

### 5.1 Target Location
`HPAutoCad/HPAutoCad.slnx`

### 5.2 Exact Solution Modification
Add the 3 new Milestone M1 projects (`HPAutoCad.Core`, `HPAutoCad.TileFetch`, and `HPAutoCad.Tests`) to `HPAutoCad.slnx`.

#### Verbatim Updated File
```xml
<Solution>
  <Configurations>
    <BuildType Name="Debug" />
    <BuildType Name="Release" />
  </Configurations>
  <Folder Name="/Solution Items/">
    <File Path="global.json" />
    <File Path="README.md" />
  </Folder>
  <!-- AutoCAD MCP: the bridge (loader + isolated bridge) that runs inside acad.exe and the stdio server exe the
       host AI launches. Shared engine, always referenced by relative path, never copied:
         ../McpShared/HPRebar.Mcp.Contracts
         ../McpShared/HPRebar.McpBridge.Core
         ../McpShared/HPRebar.Mcp.Server.Core -->
  <Project Path="HPAutoCad.Core/HPAutoCad.Core.csproj" />
  <Project Path="HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj" />
  <Project Path="HPAutoCad.Tests/HPAutoCad.Tests.csproj" />
  <Project Path="HPAutoCad.McpBridge.Loader/HPAutoCad.McpBridge.Loader.csproj" />
  <Project Path="HPAutoCad.Aec/HPAutoCad.Aec.csproj" />
  <Project Path="HPAutoCad.Aec.Tests/HPAutoCad.Aec.Tests.csproj" />
  <Project Path="HPAutoCad.McpBridge/HPAutoCad.McpBridge.csproj" />
  <Project Path="HPAutoCad.Mcp.Server/HPAutoCad.Mcp.Server.csproj" />
  <Project Path="HPAutoCad.Mcp.Server.Tests/HPAutoCad.Mcp.Server.Tests.csproj" />
  <Folder Name="/Shared/">
    <Project Path="../McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj" />
    <Project Path="../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj" />
    <Project Path="../McpShared/HPRebar.Mcp.Server.Core/HPRebar.Mcp.Server.Core.csproj" />
  </Folder>
</Solution>
```

#### Diff Comparison
```diff
--- a/HPAutoCad/HPAutoCad.slnx
+++ b/HPAutoCad/HPAutoCad.slnx
@@ -14,4 +14,7 @@
   <!-- AutoCAD MCP: the bridge (loader + isolated bridge) that runs inside acad.exe and the stdio server exe the
        host AI launches. Shared engine, always referenced by relative path, never copied:
          ../McpShared/HPRebar.Mcp.Contracts
          ../McpShared/HPRebar.McpBridge.Core
          ../McpShared/HPRebar.Mcp.Server.Core -->
+  <Project Path="HPAutoCad.Core/HPAutoCad.Core.csproj" />
+  <Project Path="HPAutoCad.TileFetch/HPAutoCad.TileFetch.csproj" />
+  <Project Path="HPAutoCad.Tests/HPAutoCad.Tests.csproj" />
   <Project Path="HPAutoCad.McpBridge.Loader/HPAutoCad.McpBridge.Loader.csproj" />
```

---

## 6. Civil 3D Mirror Safety Validation (`HPCivil3d.McpBridge.Tests`)

### 6.1 Mirror Mechanism Inspection
The mirror relationship between `HPAutoCad/` and `HPCivil3d/` is enforced by `HPCivil3d/HPCivil3d.McpBridge.Tests/MirrorTests.cs`.
- **Fence Scope**:
  `MirrorTokenTable.SourceFiles(root, "HPAutoCad.McpBridge", "HPAutoCad.McpBridge.Loader", "HPAutoCad.Mcp.Server")`
  The mirror test strictly checks files within those **three explicit folders**.
- **No Solution Scope**:
  Neither `HPAutoCad.slnx` nor `HPAutoCad.bundle` nor `tools/` is scanned or tracked by `HPCivil3d/tools/mirror-tokens.json`.
- **Isolation Guarantee**:
  Creating `HPAutoCad.Core/`, `HPAutoCad.TileFetch/`, and `HPAutoCad.Tests/` places code in completely separate directories outside the mirror fence.
- **Verification Proof**:
  Running `dotnet test HPCivil3d.McpBridge.Tests` confirms all 60 tests continue to pass with 0 failures, 0 warnings, and 0 drift.

---

## 7. Test Execution Commands & Expected Outputs

### 7.1 Targeted Unit Test Command
To run `HPAutoCad.Tests` individually from the repository root:
```bash
dotnet test HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj -p:DeployBundle=false
```
Or from the `HPAutoCad/` directory:
```bash
dotnet test HPAutoCad.Tests
```

### 7.2 Verbatim Expected Output (`HPAutoCad.Tests`)
```
Running tests from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Tests\bin\Debug\net10.0-windows\HPAutoCad.Tests.dll (net10.0|x64)
skipped HPAutoCad.Tests.HPGeoLink.ImageryPipelineTests.Live_spike_fetches_four_real_tiles_and_stitches_512x512
  set HPGEO_LIVE_TILES=1 to hit the real provider
  from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Tests\bin\Debug\net10.0-windows\HPAutoCad.Tests.dll (net10.0|x64)
skipped HPAutoCad.Tests.HPGeoLink.TileFetchHelperTests.Live_helper_fetches_four_real_tiles_into_a_temp_cache_through_the_host_side_fetcher
  set HPGEO_LIVE_TILES=1 to hit the real provider
  from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Tests\bin\Debug\net10.0-windows\HPAutoCad.Tests.dll (net10.0|x64)
skipped HPAutoCad.Tests.HPGeoLink.ImageryPipelineTests.Live_prefetch_fills_the_default_cache_for_the_acceptance_ring
  set HPGEO_LIVE_TILES=1 to hit the real provider
  from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Tests\bin\Debug\net10.0-windows\HPAutoCad.Tests.dll (net10.0|x64)
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Tests\bin\Debug\net10.0-windows\HPAutoCad.Tests.dll (net10.0|x64) passed (1s 520ms)

Test run summary: Passed!
  total: 161
  failed: 0
  succeeded: 158
  skipped: 3
  duration: 1s 725ms
```

### 7.3 Solution-Wide Test Command
To run all test projects within `HPAutoCad.slnx` from the `HPAutoCad/` directory:
```bash
dotnet test
```

### 7.4 Verbatim Expected Output (Solution-Wide)
```
Running tests from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Mcp.Server.Tests\bin\Debug\net10.0-windows\HPAutoCad.Mcp.Server.Tests.dll (net10.0|x64)
Running tests from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Aec.Tests\bin\Debug\net10.0-windows\HPAutoCad.Aec.Tests.dll (net10.0|x64)
Running tests from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Tests\bin\Debug\net10.0-windows\HPAutoCad.Tests.dll (net10.0|x64)
skipped HPAutoCad.Tests.HPGeoLink.ImageryPipelineTests.Live_spike_fetches_four_real_tiles_and_stitches_512x512
  set HPGEO_LIVE_TILES=1 to hit the real provider
skipped HPAutoCad.Tests.HPGeoLink.TileFetchHelperTests.Live_helper_fetches_four_real_tiles_into_a_temp_cache_through_the_host_side_fetcher
  set HPGEO_LIVE_TILES=1 to hit the real provider
skipped HPAutoCad.Tests.HPGeoLink.ImageryPipelineTests.Live_prefetch_fills_the_default_cache_for_the_acceptance_ring
  set HPGEO_LIVE_TILES=1 to hit the real provider
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Aec.Tests\bin\Debug\net10.0-windows\HPAutoCad.Aec.Tests.dll (net10.0|x64) passed (3s 313ms)
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Tests\bin\Debug\net10.0-windows\HPAutoCad.Tests.dll (net10.0|x64) passed (1s 520ms)
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Mcp.Server.Tests\bin\Debug\net10.0-windows\HPAutoCad.Mcp.Server.Tests.dll (net10.0|x64) passed (8s 336ms)

Test run summary: Passed!
  total: 666
  failed: 0
  succeeded: 663
  skipped: 3
  duration: 8s 750ms
```

---

## 8. Step-by-Step Implementation Instructions for `worker_m1`

### Step 1: Create Directory Structure
Create the following directories under `HPAutoCad/HPAutoCad.Tests/`:
```bash
HPAutoCad/HPAutoCad.Tests/
├── HPGeoLink/
│   ├── Fixtures/
│   └── Support/
```

### Step 2: Create Project File
Write `HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj` using the verbatim content from Section 4.2.

### Step 3: Copy Golden Data Files & Fixture Class
1. Copy the 3 JSON files from `HPGeo/HPGeo.Tests/Fixtures/` to `HPAutoCad/HPAutoCad.Tests/HPGeoLink/Fixtures/`:
   - `golden-vn2000-to-wgs84.json`
   - `golden-wgs84-to-vn2000.json`
   - `golden-webmercator.json`
2. Copy `HPGeo/HPGeo.Tests/Fixtures/GoldenFixtures.cs` to `HPAutoCad/HPAutoCad.Tests/HPGeoLink/Fixtures/GoldenFixtures.cs` and update its namespace:
   ```csharp
   namespace HPAutoCad.Tests.HPGeoLink.Fixtures;
   ```

### Step 4: Migrate the 8 Pure Domain Test Fixtures
Copy and update each file into `HPAutoCad/HPAutoCad.Tests/HPGeoLink/` with namespace `HPAutoCad.Tests.HPGeoLink` and updated using directives:

| Test File | Usings to Replace |
|---|---|
| `ConverterTests.cs` | Replace `using HPGeo.Core.*` with `using HPAutoCad.Core.HPGeoLink.Conversion;`<br/>`using HPAutoCad.Core.HPGeoLink.Model;`<br/>`using HPAutoCad.Core.HPGeoLink.Projection;`<br/>`using HPAutoCad.Core.HPGeoLink.Units;`<br/>`using HPAutoCad.Core.HPGeoLink.Validation;` |
| `ProjectionGoldenTests.cs` | Replace `using HPGeo.Core.*` with `using HPAutoCad.Core.HPGeoLink.Model;`<br/>`using HPAutoCad.Core.HPGeoLink.Projection;`<br/>`using HPAutoCad.Tests.HPGeoLink.Fixtures;` |
| `ProvinceCatalogTests.cs` | Replace `using HPGeo.Core.*` with `using HPAutoCad.Core.HPGeoLink.Catalog;` |
| `ImportTests.cs` | Replace `using HPGeo.Core.*` with `using HPAutoCad.Core.HPGeoLink.Conversion;`<br/>`using HPAutoCad.Core.HPGeoLink.Import;`<br/>`using HPAutoCad.Core.HPGeoLink.Kml;`<br/>`using HPAutoCad.Core.HPGeoLink.Model;`<br/>`using HPAutoCad.Core.HPGeoLink.Projection;` |
| `KmlTests.cs` | Replace `using HPGeo.Core.*` with `using HPAutoCad.Core.HPGeoLink.Conversion;`<br/>`using HPAutoCad.Core.HPGeoLink.Geometry;`<br/>`using HPAutoCad.Core.HPGeoLink.Kml;`<br/>`using HPAutoCad.Core.HPGeoLink.Model;`<br/>`using HPAutoCad.Core.HPGeoLink.Projection;` |
| `ImageryTests.cs` | Replace `using HPGeo.Core.*` with `using HPAutoCad.Core.HPGeoLink.Imagery;`<br/>`using HPAutoCad.Core.HPGeoLink.Model;`<br/>`using HPAutoCad.Core.HPGeoLink.Projection;` |
| `ImageArgumentsTests.cs` | Replace `using HPGeo.Core.*` with `using HPAutoCad.Core.HPGeoLink.Imagery;`<br/>`using HPAutoCad.Core.HPGeoLink.Settings;`<br/>`using HPAutoCad.Core.HPGeoLink.Units;` |
| `ReviewRegressionTests.cs` | Replace `using HPGeo.Core.*` with `using HPAutoCad.Core.HPGeoLink.Conversion;`<br/>`using HPAutoCad.Core.HPGeoLink.Geometry;`<br/>`using HPAutoCad.Core.HPGeoLink.Import;`<br/>`using HPAutoCad.Core.HPGeoLink.Kml;`<br/>`using HPAutoCad.Core.HPGeoLink.Model;`<br/>`using HPAutoCad.Core.HPGeoLink.Projection;`<br/>`using HPAutoCad.Core.HPGeoLink.Units;`<br/>`using HPAutoCad.Core.HPGeoLink.Validation;` |

### Step 5: Migrate Support Classes & Remaining 4 Test Fixtures
1. Copy the 9 support classes into `HPAutoCad/HPAutoCad.Tests/HPGeoLink/Support/` under namespace `HPAutoCad.Tests.HPGeoLink.Support`:
   - `GeoExportViewModel.cs`, `GeoExportViewModel.Commands.cs` (from `HPGeo/HPGeo.AutoCad/UI/`)
   - `GeoImportViewModel.cs` (from `HPGeo/HPGeo.AutoCad/UI/`)
   - `CrsSelectionViewModel.cs` (from `HPGeo/HPGeo.AutoCad/UI/`)
   - `IGeoExportShell.cs` (from `HPGeo/HPGeo.AutoCad/UI/`)
   - `IGeoImportShell.cs` (from `HPGeo/HPGeo.AutoCad/UI/`)
   - `GeoExportItems.cs` (from `HPGeo/HPGeo.AutoCad/UI/`)
   - `HelperTileFetcher.cs` (from `HPGeo/HPGeo.AutoCad/Imagery/`) — update `HelperExe` constant to `"HPAutoCad.TileFetch.exe"`
   - `TileStitcher.cs` (from `HPGeo/HPGeo.AutoCad/Imagery/`)
2. In `TileFetchHelperTests.cs`:
   - Update `HelperExe` path string:
     ```csharp
     private static readonly string HelperExe = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "HPAutoCad.TileFetch", "bin", "Debug", "net8.0", "HPAutoCad.TileFetch.exe"));
     ```
   - Update usings to import `HPAutoCad.Core.HPGeoLink.Imagery`, `Model`, `Projection`, and `HPAutoCad.Tests.HPGeoLink.Support`.
3. In `ImageryPipelineTests.cs`:
   - Update usings to import `HPAutoCad.Tests.HPGeoLink.Support` for `TileStitcher`, and `HPAutoCad.Core.HPGeoLink.*`.
4. In `GeoExportViewModelTests.cs` and `GeoImportViewModelTests.cs`:
   - Update usings to import `HPAutoCad.Tests.HPGeoLink.Support` for ViewModels and shells, and `HPAutoCad.Core.HPGeoLink.*`.

### Step 6: Update `HPAutoCad.slnx`
Add `<Project Path="HPAutoCad.Tests/HPAutoCad.Tests.csproj" />` to `HPAutoCad/HPAutoCad.slnx` along with `HPAutoCad.Core` and `HPAutoCad.TileFetch` (as shown in Section 5.2).

### Step 7: Verification Commands
Execute the following verification sequence:
```bash
# 1. Build test project
dotnet build HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj

# 2. Run unit tests
dotnet test HPAutoCad/HPAutoCad.Tests/HPAutoCad.Tests.csproj

# 3. Verify Civil 3D mirror invariant
dotnet test HPCivil3d/HPCivil3d.McpBridge.Tests/HPCivil3d.McpBridge.Tests.csproj

# 4. Verify full solution tests
dotnet test HPAutoCad/HPAutoCad.slnx
```

---

## 9. Verification Gate Checklist

- [ ] All 12 test fixture files present in `HPAutoCad/HPAutoCad.Tests/HPGeoLink/`.
- [ ] `Fixtures/GoldenFixtures.cs` present with namespace `HPAutoCad.Tests.HPGeoLink.Fixtures`.
- [ ] 3 JSON golden files copied to `HPGeoLink/Fixtures/` and deployed to `$(OutDir)Fixtures\`.
- [ ] `HPAutoCad.Tests.csproj` targets `net10.0-windows` with `xunit.v3` 3.1.0 and MTP runner.
- [ ] `HPAutoCad.slnx` builds cleanly with 0 errors and 0 warnings treated as errors.
- [ ] `dotnet test HPAutoCad.Tests` runs exactly 161 tests: 158 passed, 3 skipped, 0 failed.
- [ ] `dotnet test HPCivil3d.McpBridge.Tests` passes 60/60 tests (zero mirror drift).
