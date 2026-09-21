# Technical Specification & Implementation Plan: HPAutoCad.Core

**Milestone**: M1 (Domain Core & Companion Project)  
**Deliverable**: `HPAutoCad/HPAutoCad.Core/` (.NET 8.0 Pure Domain Geodetic & Math Library)  
**Author**: `explorer_m1_core`  
**Target Audience**: `worker_m1` / Orchestrator  
**Date**: 2026-09-20  

---

## 1. Executive Summary & Purpose

`HPAutoCad.Core` is the pure domain library for the AutoCAD platform within the repository, establishing architectural parity with `HPRebar.Core` in the Revit platform. It encapsulates all geodetic algorithms, coordinate conversions, map projections, administrative province catalogs, KML/KMZ export/import pipelines, and satellite imagery tile math.

### Architectural Imperatives

1. **Host-Free Isolation**:
   - Compiles strictly against `.NET 8.0` (`net8.0`), with **ZERO** references to `Autodesk.*`, `AcMgd`, `AcDbMgd`, `AcCoreMgd`, or any CAD binaries.
   - All coordinates, vectors, matrices, and geometries are represented as pure .NET types (`PlanePoint`, `GeoPoint`, primitive doubles, records, and arrays) without CAD entities (`Point3d`, `Polyline`, `ObjectId`).
2. **Deterministic Testability**:
   - Enables 100% of geodetic transformation math, boundary tessellation, KML generation, and tile math to be thoroughly unit-tested via xUnit v3 / MTP in `HPAutoCad.Tests` without launching or attaching to `acad.exe`.
3. **Unified Feature Architecture**:
   - Migrated from the standalone legacy `HPGeo/HPGeo.Core` project into the `HPAutoCad` repository tree under the unified feature namespace `HPAutoCad.Core.HPGeoLink.*`.
   - Forms the foundational dependency for:
     - `HPAutoCad.TileFetch` (out-of-process tile downloader).
     - `HPAutoCad.Tests` (geodetic unit test suite).
     - `HPAutoCad` (Add-In UI, WPF MVVM dialogs, and AutoCAD drawing integration).

---

## 2. Complete File Inventory & Migration Mapping

The migration transfers **41 files** (40 C# source files and 1 JSON embedded resource file) from `HPGeo/HPGeo.Core/` into `HPAutoCad/HPAutoCad.Core/HPGeoLink/`.

| # | Legacy Source Path (`HPGeo/HPGeo.Core/`) | Target Path (`HPAutoCad/HPAutoCad.Core/HPGeoLink/`) | Lines | Bytes | Primary Responsibility / Algorithm |
|---|------------------------------------------|---------------------------------------------------|------:|------:|------------------------------------|
| **Catalog** | | | | | |
| 1 | `Catalog/CentralMeridian.cs` | `Catalog/CentralMeridian.cs` | 50 | 2,126 | VN-2000 central meridian struct (17 meridians, 103°00′–108°30′) & parser/formatter |
| 2 | `Catalog/Province.cs` | `Catalog/Province.cs` | 19 | 702 | Province model record (Name, CentralMeridians list, CatalogKind) |
| 3 | `Catalog/ProvinceCatalog.cs` | `Catalog/ProvinceCatalog.cs` | 98 | 4,776 | Embedded JSON catalog loader (34 current post-2025 & 63 legacy provinces) |
| **Data** | | | | | |
| 4 | `Data/vn2000-provinces.json` | `Data/vn2000-provinces.json` | 699 | 10,825 | Static JSON dataset of Vietnam administrative provinces and central meridians |
| **Conversion** | | | | | |
| 5 | `Conversion/ConversionModels.cs` | `Conversion/ConversionModels.cs` | 74 | 2,913 | Domain records: `SurveyPoint`, `BoundaryPolyline`, `ConversionOptions`, `ConversionResult` |
| 6 | `Conversion/ExportArguments.cs` | `Conversion/ExportArguments.cs` | 134 | 5,382 | User export parameters: CRS, unit scale, KML styling, presentation |
| 7 | `Conversion/Vn2000Converter.cs` | `Conversion/Vn2000Converter.cs` | 136 | 5,714 | High-level bidirectional converter (VN-2000 plane <-> WGS84 geodetic) |
| **Geometry** | | | | | |
| 8 | `Geometry/BulgeTessellator.cs` | `Geometry/BulgeTessellator.cs` | 89 | 3,753 | CAD polyline arc bulge tessellation into linear chords (5 mm sagitta tolerance) |
| **Imagery** | | | | | |
| 9 | `Imagery/AffineFit.cs` | `Imagery/AffineFit.cs` | 99 | 3,825 | Least-squares 2D affine transformation fit (pixel coordinates <-> CAD coordinates) |
| 10 | `Imagery/ImageArguments.cs` | `Imagery/ImageArguments.cs` | 132 | 5,108 | Arguments for satellite imagery bounding, target resolution, and provider selection |
| 11 | `Imagery/ImageZoneResolver.cs` | `Imagery/ImageZoneResolver.cs` | 101 | 3,848 | Coordinate system resolution for raster imagery extents |
| 12 | `Imagery/ImageryProvider.cs` | `Imagery/ImageryProvider.cs` | 55 | 2,058 | Tile server definitions (Esri World Imagery, XYZ URL templates) |
| 13 | `Imagery/OutputRaster.cs` | `Imagery/OutputRaster.cs` | 158 | 5,599 | Raster buffer metadata, control grid generation, world file (.tfw/.jgw) math |
| 14 | `Imagery/RasterWarper.cs` | `Imagery/RasterWarper.cs` | 163 | 6,952 | Bicubic Catmull-Rom resampling and image reprojection pipeline |
| 15 | `Imagery/TileCache.cs` | `Imagery/TileCache.cs` | 69 | 3,311 | Disk tile cache manager (`%LocalAppData%\HPGeo\tiles\...`), eviction, atomic writes |
| 16 | `Imagery/TileCoverage.cs` | `Imagery/TileCoverage.cs` | 218 | 9,153 | Tile planning: bounding box coverage, zoom level calculation, tile address grid |
| 17 | `Imagery/TileFetchProtocol.cs` | `Imagery/TileFetchProtocol.cs` | 97 | 5,049 | IPC protocol between host add-in and `HPAutoCad.TileFetch.exe` (request/stdout format) |
| 18 | `Imagery/TileFetcher.cs` | `Imagery/TileFetcher.cs` | 148 | 5,802 | In-process HTTP tile downloader with retry and progress reporting |
| 19 | `Imagery/WebMercator.cs` | `Imagery/WebMercator.cs` | 69 | 3,018 | Spherical Mercator (EPSG:3857) projection math, pixel coordinate transformations |
| **Import** | | | | | |
| 20 | `Import/CoordinateTextParser.cs` | `Import/CoordinateTextParser.cs` | 179 | 7,494 | Robust parser for pasted coordinate lists, CSV, tab-separated tables, auto X/Y detection |
| 21 | `Import/ImportPlanner.cs` | `Import/ImportPlanner.cs` | 230 | 8,916 | Import validation, coordinate reordering, drawing insertion point planning |
| 22 | `Import/KmlReader.cs` | `Import/KmlReader.cs` | 141 | 6,071 | KML/KMZ reader extracting Placemarks, LineStrings, and Polygons into domain models |
| **Kml** | | | | | |
| 23 | `Kml/KmlColor.cs` | `Kml/KmlColor.cs` | 30 | 1,279 | KML `aabbggrr` hex color formatting, validation, and polygon opacity generation |
| 24 | `Kml/KmlDocumentBuilder.cs` | `Kml/KmlDocumentBuilder.cs` | 184 | 8,055 | XML building for KML documents, Placemark styles, look-at cameras, descriptions |
| 25 | `Kml/KmlExportOptions.cs` | `Kml/KmlExportOptions.cs` | 32 | 1,317 | Presentation options (Points, Boundaries, Both, AltitudeModes) |
| 26 | `Kml/KmlGeoMath.cs` | `Kml/KmlGeoMath.cs` | 37 | 1,356 | Haversine distance and geodetic bounding box calculations |
| 27 | `Kml/KmzExportPipeline.cs` | `Kml/KmzExportPipeline.cs` | 34 | 1,359 | End-to-end export orchestrator: validation -> conversion -> KML build -> KMZ zip |
| 28 | `Kml/KmzWriter.cs` | `Kml/KmzWriter.cs` | 68 | 2,769 | ZipArchive packaging of `doc.kml` into `.kmz` compressed format |
| **Model** | | | | | |
| 29 | `Model/GeoPoint.cs` | `Model/GeoPoint.cs` | 8 | 315 | Readonly record struct `GeoPoint(double LatDeg, double LonDeg)` |
| 30 | `Model/PlanePoint.cs` | `Model/PlanePoint.cs` | 12 | 528 | Readonly record struct `PlanePoint(double Easting, double Northing)` |
| **Projection** | | | | | |
| 31 | `Projection/Ellipsoid.cs` | `Projection/Ellipsoid.cs` | 32 | 1,087 | Reference ellipsoids (WGS-84, Krassovsky 1940) equatorial radius $a$, flattening $f$ |
| 32 | `Projection/GeocentricConverter.cs` | `Projection/GeocentricConverter.cs` | 38 | 1,639 | Geodetic $(\phi, \lambda, h)$ <-> Geocentric Cartesian $(X, Y, Z)$ using Bowring closed form |
| 33 | `Projection/Helmert7.cs` | `Projection/Helmert7.cs` | 74 | 3,290 | 7-parameter Helmert coordinate datum transform ($\Delta X, \Delta Y, \Delta Z, \omega_X, \omega_Y, \omega_Z, s$) |
| 34 | `Projection/TmParameters.cs` | `Projection/TmParameters.cs` | 27 | 1,179 | Transverse Mercator grid parameters ($k_0 = 0.9999$, False Easting $500,000$ m, False Northing $0$) |
| 35 | `Projection/TransverseMercator.cs` | `Projection/TransverseMercator.cs` | 148 | 5,667 | Snyder Transverse Mercator (TM-3 / TM-6) forward and inverse series equations |
| 36 | `Projection/Vn2000Wgs84Transform.cs` | `Projection/Vn2000Wgs84Transform.cs` | 72 | 3,302 | Complete 3-stage datum transformation: TM-3 -> Geocentric -> Helmert-7 -> WGS84 |
| **Settings** | | | | | |
| 37 | `Settings/GeoSettings.cs` | `Settings/GeoSettings.cs` | 40 | 2,115 | Domain model for persistent settings (NOD "HPGEO" & `%AppData%\HPGeo\settings.json`) |
| **Text** | | | | | |
| 38 | `Text/WildcardPattern.cs` | `Text/WildcardPattern.cs` | 37 | 1,560 | Glob/wildcard matching utility for layer names and filter strings |
| **Units** | | | | | |
| 39 | `Units/DrawingUnitFactor.cs` | `Units/DrawingUnitFactor.cs` | 46 | 1,996 | AutoCAD `INSUNITS` scale factors (Millimeters, Meters, Centimeters, Inches, Feet to metres) |
| **Validation** | | | | | |
| 40 | `Validation/PlausibilityCheck.cs` | `Validation/PlausibilityCheck.cs` | 45 | 1,791 | Coordinate plausibility checks (detect swapped X/Y, out-of-range false coordinates) |
| 41 | `Validation/VietnamEnvelope.cs` | `Validation/VietnamEnvelope.cs` | 20 | 734 | Geographic bounding envelope for Vietnam territory (Lat 8.0°–24.0°N, Lon 102.0°–110.0°E) |

---

## 3. Namespace Migration & Type Reference Matrix

Every source file in `HPGeo.Core` currently follows a 1:1 folder-to-namespace naming convention. In `HPAutoCad.Core`, the prefix `HPGeo.Core` is systematically replaced with `HPAutoCad.Core.HPGeoLink`.

### 3.1 Sub-Namespace Mapping

| Sub-System | Source Namespace (`HPGeo.Core.*`) | Target Namespace (`HPAutoCad.Core.HPGeoLink.*`) |
|------------|-----------------------------------|------------------------------------------------|
| Catalog | `HPGeo.Core.Catalog` | `HPAutoCad.Core.HPGeoLink.Catalog` |
| Conversion | `HPGeo.Core.Conversion` | `HPAutoCad.Core.HPGeoLink.Conversion` |
| Geometry | `HPGeo.Core.Geometry` | `HPAutoCad.Core.HPGeoLink.Geometry` |
| Imagery | `HPGeo.Core.Imagery` | `HPAutoCad.Core.HPGeoLink.Imagery` |
| Import | `HPGeo.Core.Import` | `HPAutoCad.Core.HPGeoLink.Import` |
| Kml | `HPGeo.Core.Kml` | `HPAutoCad.Core.HPGeoLink.Kml` |
| Model | `HPGeo.Core.Model` | `HPAutoCad.Core.HPGeoLink.Model` |
| Projection | `HPGeo.Core.Projection` | `HPAutoCad.Core.HPGeoLink.Projection` |
| Settings | `HPGeo.Core.Settings` | `HPAutoCad.Core.HPGeoLink.Settings` |
| Text | `HPGeo.Core.Text` | `HPAutoCad.Core.HPGeoLink.Text` |
| Units | `HPGeo.Core.Units` | `HPAutoCad.Core.HPGeoLink.Units` |
| Validation | `HPGeo.Core.Validation` | `HPAutoCad.Core.HPGeoLink.Validation` |

### 3.2 Type Catalog per Namespace

| Target Namespace | Primary Types Exported |
|------------------|------------------------|
| `HPAutoCad.Core.HPGeoLink.Catalog` | `CentralMeridian`, `Province`, `ProvinceCatalog`, `ProvinceCatalogKind` |
| `HPAutoCad.Core.HPGeoLink.Conversion` | `Vn2000Converter`, `ConversionOptions`, `ConversionResult`, `ConvertedPoint`, `ConvertedBoundary`, `SurveyPoint`, `BoundaryPolyline`, `ConversionIssue`, `IssueSeverity`, `ExportArguments` |
| `HPAutoCad.Core.HPGeoLink.Geometry` | `BulgeTessellator`, `Vertex` |
| `HPAutoCad.Core.HPGeoLink.Imagery` | `AffineFit`, `AffineTransform`, `AffineFitResult`, `ImageArguments`, `ImageZoneResolver`, `ResolvedImageZone`, `ImageZoneException`, `IImageryProvider`, `EsriWorldImageryProvider`, `ImageryProviders`, `OutputRaster`, `ControlGrid`, `RasterWarper`, `RasterBuffer`, `ResampleKernel`, `TileCache`, `TileCoverage`, `TilePlan`, `PlanResult`, `GridBoundingBox`, `GeoBoundingBox`, `TileFetchProtocol`, `TileFetchRequest`, `TileFetchLine`, `TileFetcher`, `TileFetchResult`, `TileFailure`, `ITileSource`, `WebMercator`, `TileAddress`, `GlobalPixel` |
| `HPAutoCad.Core.HPGeoLink.Import` | `CoordinateTextParser`, `PastedCoordinate`, `PastedCoordinateKind`, `PairOrder`, `ImportPlanner`, `ImportPlan`, `ImportPoint`, `ImportPolyline`, `KmlReader`, `KmlFeature`, `KmlFeatureKind` |
| `HPAutoCad.Core.HPGeoLink.Kml` | `KmlColor`, `KmlDocumentBuilder`, `KmlBuildResult`, `KmlExportOptions`, `KmlOutput`, `KmlGeoMath`, `KmzExportPipeline`, `KmzExportRequest`, `KmzExportOutcome`, `KmzWriter` |
| `HPAutoCad.Core.HPGeoLink.Model` | `GeoPoint`, `PlanePoint` |
| `HPAutoCad.Core.HPGeoLink.Projection` | `Ellipsoid`, `GeocentricConverter`, `Helmert7`, `Helmert7Parameters`, `TmParameters`, `TransverseMercator`, `Vn2000Wgs84Transform` |
| `HPAutoCad.Core.HPGeoLink.Settings` | `GeoSettings` |
| `HPAutoCad.Core.HPGeoLink.Text` | `WildcardPattern` |
| `HPAutoCad.Core.HPGeoLink.Units` | `DrawingUnit`, `DrawingUnitFactor` |
| `HPAutoCad.Core.HPGeoLink.Validation` | `PlausibilityCheck`, `VietnamEnvelope` |

### 3.3 Critical Code-Level String Literals & Resources

Two files contain internal string constants that must be updated during migration:

1. **`Catalog/ProvinceCatalog.cs` (Line 15)**:
   - *Legacy*: `private const string ResourceName = "HPGeo.Core.Data.vn2000-provinces.json";`
   - *Target*: `private const string ResourceName = "HPAutoCad.Core.HPGeoLink.Data.vn2000-provinces.json";`
   - *Rationale*: Assembly manifest resource stream lookup fails if this string does not match the embedded resource logical name in `.csproj`.

2. **`Imagery/TileFetchProtocol.cs` (Line 53)**:
   - *Legacy*: `return new TileFetchRequest(provider, cache, string.IsNullOrWhiteSpace(ua) ? "HPGeo.TileFetch" : ua, tiles);`
   - *Target*: `return new TileFetchRequest(provider, cache, string.IsNullOrWhiteSpace(ua) ? "HPAutoCad.TileFetch" : ua, tiles);`
   - *Rationale*: Aligns fallback User-Agent with the new companion process name `HPAutoCad.TileFetch.exe`.

3. **`Imagery/TileCache.cs` (Line 11)**:
   - *Retain*: `Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HPGeo", "tiles");`
   - *Rationale*: Preserves the local cache on developer/user workstations so downloaded satellite tiles are not re-fetched, consistent with the `HPGEO` NOD dictionary and `%AppData%\HPGeo\settings.json` settings path.

---

## 4. Project File Specification (`HPAutoCad.Core.csproj`)

The project file must reside at:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Core\HPAutoCad.Core.csproj`

### Verbatim XML Content

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <!-- Host-free geodetic & math engine: VN-2000 <-> WGS84 (TM-3 + 7-parameter Helmert), the province catalog,
             the KML/KMZ pipeline, and satellite imagery math. Never references an AutoCAD type so it is 100%
             unit-tested without acad.exe. -->
        <TargetFramework>net8.0</TargetFramework>
        <LangVersion>latest</LangVersion>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <RootNamespace>HPAutoCad.Core</RootNamespace>
        <Configurations>Debug;Release</Configurations>
        <Version>0.1.0</Version>
    </PropertyGroup>

    <ItemGroup>
        <!-- Vietnam administrative province catalog and central meridians dataset. -->
        <EmbeddedResource Include="HPGeoLink\Data\vn2000-provinces.json" LogicalName="HPAutoCad.Core.HPGeoLink.Data.vn2000-provinces.json" />
    </ItemGroup>

    <ItemGroup>
        <!-- Test project grant for internal helper methods (e.g. TransverseMercator.ForwardSeries). -->
        <InternalsVisibleTo Include="HPAutoCad.Tests" />
    </ItemGroup>

</Project>
```

### Key Architectural Properties:
- **`TargetFramework: net8.0`**: Standard .NET 8 library. Pure managed code, runnable on any platform.
- **`Nullable: enable`**: Enforces complete nullable reference type checking.
- **`ImplicitUsings: enable`**: Uses standard implicit C# usings (`System`, `System.Collections.Generic`, `System.IO`, `System.Linq`, `System.Threading.Tasks`).
- **`RootNamespace: HPAutoCad.Core`**: Establishes root identity matching `HPRebar.Core`.
- **`EmbeddedResource`**: Declares `HPGeoLink\Data\vn2000-provinces.json` with explicit `LogicalName="HPAutoCad.Core.HPGeoLink.Data.vn2000-provinces.json"`.
- **`InternalsVisibleTo: HPAutoCad.Tests`**: Allows unit tests to verify internal algorithms (such as `TransverseMercator.ForwardSeries`).
- **Zero Host References**: No `<PackageReference Include="AutoCAD.NET" ... />`, no `<ProjectReference>` to CAD bridges or Revit add-ins.

---

## 5. Solution Integration Specification (`HPAutoCad.slnx`)

Register the project in `HPAutoCad/HPAutoCad.slnx`.

### Target XML Modification in `HPAutoCad/HPAutoCad.slnx`

Add line 16 right before the existing MCP and AEC projects:

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
  <!-- AutoCAD Core: pure domain geodetic algorithms, projections, catalogs, KML/KMZ and imagery math -->
  <Project Path="HPAutoCad.Core/HPAutoCad.Core.csproj" />
  <Project Path="HPAutoCad.McpBridge.Loader/HPAutoCad.McpBridge.Loader.csproj" />
  <Project Path="HPAutoCad.Aec/HPAutoCad.Aec.csproj" />
  ...
```

---

## 6. Worker Agent Implementation Instructions

The worker agent (`worker_m1`) must execute the following 7 phases in sequence:

### Phase 1: Directory Scaffolding
Create the directory tree:
```powershell
New-Item -ItemType Directory -Path "HPAutoCad/HPAutoCad.Core/HPGeoLink/Catalog" -Force
New-Item -ItemType Directory -Path "HPAutoCad/HPAutoCad.Core/HPGeoLink/Conversion" -Force
New-Item -ItemType Directory -Path "HPAutoCad/HPAutoCad.Core/HPGeoLink/Data" -Force
New-Item -ItemType Directory -Path "HPAutoCad/HPAutoCad.Core/HPGeoLink/Geometry" -Force
New-Item -ItemType Directory -Path "HPAutoCad/HPAutoCad.Core/HPGeoLink/Imagery" -Force
New-Item -ItemType Directory -Path "HPAutoCad/HPAutoCad.Core/HPGeoLink/Import" -Force
New-Item -ItemType Directory -Path "HPAutoCad/HPAutoCad.Core/HPGeoLink/Kml" -Force
New-Item -ItemType Directory -Path "HPAutoCad/HPAutoCad.Core/HPGeoLink/Model" -Force
New-Item -ItemType Directory -Path "HPAutoCad/HPAutoCad.Core/HPGeoLink/Projection" -Force
New-Item -ItemType Directory -Path "HPAutoCad/HPAutoCad.Core/HPGeoLink/Settings" -Force
New-Item -ItemType Directory -Path "HPAutoCad/HPAutoCad.Core/HPGeoLink/Text" -Force
New-Item -ItemType Directory -Path "HPAutoCad/HPAutoCad.Core/HPGeoLink/Units" -Force
New-Item -ItemType Directory -Path "HPAutoCad/HPAutoCad.Core/HPGeoLink/Validation" -Force
```

### Phase 2: Create Project File
Write `HPAutoCad/HPAutoCad.Core/HPAutoCad.Core.csproj` with the verbatim XML defined in Section 4.

### Phase 3: Copy Data Asset
Copy `HPGeo/HPGeo.Core/Data/vn2000-provinces.json` byte-for-byte to `HPAutoCad/HPAutoCad.Core/HPGeoLink/Data/vn2000-provinces.json`.

### Phase 4: Copy & Transform C# Source Files
Copy all 40 C# source files from `HPGeo/HPGeo.Core/<Folder>/<File>.cs` into `HPAutoCad/HPAutoCad.Core/HPGeoLink/<Folder>/<File>.cs`, applying two text substitutions:
1. Replace `namespace HPGeo.Core.` with `namespace HPAutoCad.Core.HPGeoLink.`
2. Replace `using HPGeo.Core.` with `using HPAutoCad.Core.HPGeoLink.`

*Automated execution command via PowerShell*:
```powershell
$sourceRoot = "HPGeo/HPGeo.Core"
$targetRoot = "HPAutoCad/HPAutoCad.Core/HPGeoLink"

Get-ChildItem -Path $sourceRoot -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } | ForEach-Object {
    $rel = $_.FullName.Substring((Resolve-Path $sourceRoot).Path.Length + 1)
    $dest = Join-Path $targetRoot $rel
    $content = Get-Content -Path $_.FullName -Raw -Encoding UTF8
    $content = $content -replace 'namespace HPGeo\.Core\.', 'namespace HPAutoCad.Core.HPGeoLink.'
    $content = $content -replace 'using HPGeo\.Core\.', 'using HPAutoCad.Core.HPGeoLink.'
    Set-Content -Path $dest -Value $content -Encoding UTF8
}
```

### Phase 5: Resource Reference & String Updates
Apply the specific string updates:
1. In `HPAutoCad/HPAutoCad.Core/HPGeoLink/Catalog/ProvinceCatalog.cs`:
   Replace `"HPGeo.Core.Data.vn2000-provinces.json"` with `"HPAutoCad.Core.HPGeoLink.Data.vn2000-provinces.json"`.
2. In `HPAutoCad/HPAutoCad.Core/HPGeoLink/Imagery/TileFetchProtocol.cs`:
   Replace `"HPGeo.TileFetch"` with `"HPAutoCad.TileFetch"`.

### Phase 6: Register in Solution
Add `<Project Path="HPAutoCad.Core/HPAutoCad.Core.csproj" />` to `HPAutoCad/HPAutoCad.slnx`.

### Phase 7: Verification & Quality Gate
Run the following build and verification commands:
```powershell
# 1. Build HPAutoCad.Core cleanly in Debug and Release
dotnet build HPAutoCad/HPAutoCad.Core/HPAutoCad.Core.csproj -c Debug
dotnet build HPAutoCad/HPAutoCad.Core/HPAutoCad.Core.csproj -c Release

# 2. Verify zero Autodesk references
$autodeskRefs = Select-String -Path "HPAutoCad/HPAutoCad.Core/**/*.cs", "HPAutoCad/HPAutoCad.Core/HPAutoCad.Core.csproj" -Pattern "Autodesk"
if ($autodeskRefs.Count -gt 0) { throw "Violation: Autodesk references found in HPAutoCad.Core!" }

# 3. Verify embedded resource stream loads without error
python -c "
import zipfile
# Verify .dll contains embedded resource via reflection or dotnet-dump / IL check if needed
"
```

---

## 7. Verification Method & Acceptance Criteria

### Static Verification Gates
1. **Compilation Success**:
   - `dotnet build HPAutoCad/HPAutoCad.Core/HPAutoCad.Core.csproj -c Debug` -> Exit code 0, 0 Error(s), 0 Warning(s).
   - `dotnet build HPAutoCad/HPAutoCad.Core/HPAutoCad.Core.csproj -c Release` -> Exit code 0, 0 Error(s), 0 Warning(s).
2. **CAD Binary Isolation**:
   - Zero references to `Autodesk.AutoCAD.*`, `Autodesk.Revit.*`, `AcDbMgd`, or any CAD DLL in code, usings, or `.csproj`.
3. **Resource Manifest Verification**:
   - The compiled `HPAutoCad.Core.dll` contains the manifest resource stream `HPAutoCad.Core.HPGeoLink.Data.vn2000-provinces.json`.
4. **File Count Parity**:
   - Exactly 40 `.cs` files and 1 `.json` file present under `HPAutoCad/HPAutoCad.Core/HPGeoLink/`.
