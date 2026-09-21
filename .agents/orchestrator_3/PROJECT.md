# Project: HPAutoCad HPGeoLink Migration & Closed-Loop MCP Verification

## Architecture
- **Multi-Project Structure**:
  - `HPAutoCad/HPAutoCad.Core/`: Pure domain geodetic algorithms, projections, catalogs, KML/KMZ serialization, imagery math (.NET 8.0, zero host dependencies).
  - `HPAutoCad/HPAutoCad/`: AutoCAD Add-In UI, commands, drawing readers/writers, WPF MVVM views/viewmodels, MaterialDesignThemes theme bridge (.NET 8.0-windows).
  - `HPAutoCad/HPAutoCad.TileFetch/`: Companion out-of-process console utility for map tile downloads (.NET 8.0).
  - `HPAutoCad/HPAutoCad.Tests/`: Comprehensive geodetic unit test suite (net10.0-windows / xUnit v3 / Microsoft.Testing.Platform runner).
  - `HPAutoCad/HPAutoCad.Loader/`: Isolated ALC loader in AutoCAD Default ALC, commands registration, shared Ribbon tab builder.
  - `HPAutoCad/HPAutoCad.McpBridge/`: MCP bridge runtime, Roslyn script compiler, pipe listener (`hpautocad-mcp-2026`), status window.
  - `HPAutoCad/HPAutoCad.Aec/`: AEC tools engine (layers, blocks, hatches, xrefs, change sets).
  - `HPAutoCad/HPAutoCad.Mcp.Server/`: Stdio MCP server executable (net10.0 console).
- **Single Bundle Packaging**:
  - Deployed to `%AppData%\Autodesk\ApplicationPlugins\HPAutoCad.bundle\`.
  - Manifest `PackageContents.xml` with `Platform="AutoCAD"` and `SeriesMin="R25.1"` / `SeriesMax="R25.1"`.
- **ALC Isolation**:
  - `BridgeLoadContext` isolates Roslyn 5.9, Serilog, and MCP bridge runtime in `Contents\Bridge\`.
  - `AppLoadContext` isolates WebView2, CommunityToolkit.Mvvm, and HPGeoLink UI in `Contents\App\`.
  - MaterialDesignThemes 5.3.2 ILRepacked into respective assemblies to eliminate BAML resource collisions.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Geodetic Projections Engine | Snyder TM-3 forward/inverse, WGS84 ellipsoid, 7-parameter Helmert, Bowring geocentric | M1 | survey_hpgeo |
| 2 | Administrative Province Catalog | 34 current & 63 legacy provinces, 17 central meridians, embedded JSON resource | M1 | survey_hpgeo |
| 3 | Geodetic Validation & Coordinate Parsing | Vietnam envelope bounds, TM-3 plausibility checks, coordinate table regex parser | M1 | survey_hpgeo |
| 4 | Bulge Tessellator & KML/KMZ Pipeline | Polyline arc tessellation (5mm tolerance), Placemarks, Point/Line/Polygon styling, KMZ zip writer | M1 | survey_hpgeo |
| 5 | Satellite Imagery Domain Math | Web Mercator tile math, tile coverage bounding, Catmull-Rom bicubic warper, affine fit, tile cache | M1 | survey_hpgeo |
| 6 | Companion TileFetch Utility | Out-of-process tile downloader, child process protocol, firewall (`WSAEACCES`) bypass | M1 | survey_hpgeo |
| 7 | Geodetic Unit Test Suite Port | Transfer all 161 unit tests, golden fixtures, and test runners into HPAutoCad.Tests | M1 | survey_hpgeo |
| 8 | CAD Readers & Writers | Model space entity filtering (DBPoint, Polyline), DrawingContext, DrawingWriter, single undo transaction | M2 | survey_hpgeo |
| 9 | CAD Metadata Persistence | NOD Xrecord store (`HPGEO`), user settings JSON store (`%AppData%\HPGeo\settings.json`) | M2 | survey_hpgeo |
| 10 | CAD Satellite Imagery Pipeline | ImageryPipeline, RasterInserter, TileStitcher, HelperTileFetcher integration | M2 | survey_hpgeo |
| 11 | WPF MVVM User Interface | GeoExportWindow, GeoImportWindow, CrsSelectionView, MapPanel (WebView2 + Leaflet), ViewModels | M2 | survey_hpgeo |
| 12 | Theming & Dynamic Switcher | Theme.xaml, MaterialBridge.xaml, ThemeDark/Light, MaterialThemeBridge, AutocadHostTheme on COLORTHEME | M2 | survey_hpgeo |
| 13 | MaterialDesignThemes Repacking | RepackMaterialDesign target in csproj merging toolkit into HPAutoCad.dll via ILRepack | M2 | survey_hpautocad |
| 14 | Native WebView2 Bundling | Package WebView2Loader.dll in `runtimes\win-x64\native\` with deps.json unmanaged resolution | M2 | survey_hpautocad |
| 15 | AutoCAD Commands Implementation | HPGEO, -HPGEOKMZ, HPGEOIMPORT, -HPGEOIMPORT, -HPGEOIMAGE, HPGEOINFO command handlers | M2 | survey_hpgeo |
| 16 | Single Bundle Deployment | Unified `HPAutoCad.bundle` deployment under `%AppData%\Autodesk\ApplicationPlugins\` | M3 | survey_hpautocad |
| 17 | Isolated ALC Loader | HPAutoCad.Loader in AutoCAD Default ALC, host assembly fallthrough, reflection delegates | M3 | survey_hpautocad |
| 18 | Shared Ribbon Tab Integration | Shared tab `HPAUTOCAD_MCP_TAB`, MCP panel + HPGeoLink panel, workspace & theme resilience | M3 | survey_hpautocad |
| 19 | Civil 3D Mirror Invariant | Preserve HPAutoCad.McpBridge mirror compatibility; HPCivil3d.McpBridge.Tests 100% pass | M3 | survey_hpautocad |
| 20 | E2E Test Suite (Tiers 1-4) | Comprehensive opaque-box test suite covering commands, boundary cases, cross-features, real workflows | E2E Track | survey_specs |
| 21 | Unattended AutoCAD 2026 MCP Harness | Automated acad.exe launch with bridge.scr, SECURELOAD auto-answering, pipe communication | M4 | survey_specs |
| 22 | Unattended Modal Dialog Validation | Background PowerShell jobs, off-screen Win32 PrintWindow capture, clean WM_CLOSE teardown | M4 | survey_specs |
| 23 | Scriptable CLI & Service Validation | Automated verification of -HPGEOKMZ, -HPGEOIMPORT, -HPGEOIMAGE, HPGEOINFO, and execute_autocad_code | M4 | survey_specs |
| 24 | AEC & Seed Tools Regression Gate | 100% pass rate across 12 standard seeds and 40 AEC tools with zero regressions | M4 | survey_specs |
| 25 | Standalone HPGeo/ Retirement | Safe deletion of legacy HPGeo/ folder after full live verification | M5 | original_request |
| 26 | Documentation Standardization | Update AGENTS.md, docs/code-standards.md, docs/system-architecture.md, docs/codebase-summary.md | M5 | original_request |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Domain Core & Companion Project | Create HPAutoCad.Core, HPAutoCad.TileFetch, and HPAutoCad.Tests; port 161 tests; build cleanly | None | DONE |
| M2 | Add-In Layer & UI Feature | Create HPAutoCad (WPF/MVVM, HPGeoLink, theming, repacking, commands); build cleanly | M1 | DONE |
| M3 | Single Bundle, Loader & Shared Ribbon | Single bundle packaging, HPAutoCad.Loader, shared Ribbon tab, Civil 3D mirror verification | M2 | DONE |
| E2E | E2E Testing Suite | Comprehensive test infra & test cases (Tiers 1-4); publish TEST_READY.md | Parallel to M1-M3 | DONE |
| M4 | Closed-Loop Live Verification | Live AutoCAD 2026 execution via MCP, unattended dialog check, Tier 1-4 pass, Tier 5 hardening | M3, E2E | DONE |
| M5 | Repo Cleanup & Documentation | Delete standalone HPGeo/, update AGENTS.md, docs/ (standards, architecture, summary) | M4 | DONE |

## Interface Contracts

### HPAutoCad.Core ↔ HPAutoCad
- **Projections & Conversion**:
  - `Vn2000Converter.ToWgs84(PlanePoint, ConversionOptions)` -> `ConversionResult<GeoPoint>`
  - `Vn2000Converter.ToVn2000(GeoPoint, ConversionOptions)` -> `ConversionResult<PlanePoint>`
- **Catalog**:
  - `ProvinceCatalog.FindProvince(string name, int catalogYear = 2025)` -> `Province?`
  - `ProvinceCatalog.Provinces` -> `IReadOnlyList<Province>`
- **KML / KMZ**:
  - `KmzExportPipeline.Run(KmzExportRequest)` -> `KmzExportResult`
- **Tile Fetch Protocol**:
  - `TileFetchProtocol.ParseStdout(string line)` -> `TileFetchProgressEvent`

### HPAutoCad.Loader ↔ HPAutoCad (AppLoadContext)
- **Entry Method**:
  - `HPAutoCad.Entry.Start(string appDir, Action<string> log)` -> `IReadOnlyDictionary<string, Delegate>`
  - Returned delegates: `"dialog"`, `"kmz-script"`, `"import"`, `"import-script"`, `"image-script"`, `"info"`.

### Ribbon Contract
- **Tab Identifier**: `HPAUTOCAD_MCP_TAB` ("HPAutoCad")
- **Panels**:
  - `HPAUTOCAD_MCP_PANEL` ("MCP") -> Bridge Status Button (`HPMCPBRIDGE`)
  - `HPGEOLINK_PANEL` ("HPGeoLink") -> Main Dialog Button (`HPGEO`), SplitButton (`-HPGEOKMZ`, `HPGEOIMPORT`, `HPGEOINFO`)

## Code Layout
```
HPAutoCad/
├── HPAutoCad/                            ← Add-In UI Project (net8.0-windows)
│   ├── HPGeoLink/
│   │   ├── Commands/
│   │   ├── Model/
│   │   ├── Service/
│   │   ├── View/
│   │   └── ViewModel/
│   ├── Resources/Themes/
│   └── HPAutoCad.csproj
├── HPAutoCad.Core/                       ← Pure Domain Algorithm Library (.NET 8.0)
│   ├── HPGeoLink/
│   │   ├── Catalog/
│   │   ├── Conversion/
│   │   ├── Geometry/
│   │   ├── Imagery/
│   │   ├── Import/
│   │   ├── Kml/
│   │   ├── Model/
│   │   ├── Projection/
│   │   ├── Settings/
│   │   ├── Text/
│   │   ├── Units/
│   │   └── Validation/
│   └── HPAutoCad.Core.csproj
├── HPAutoCad.TileFetch/                  ← Companion Console Helper (.NET 8.0)
│   └── HPAutoCad.TileFetch.csproj
├── HPAutoCad.Tests/                      ← Pure Unit Tests (net10.0-windows / MTP)
│   ├── HPGeoLink/
│   │   ├── Fixtures/
│   │   └── *Tests.cs
│   └── HPAutoCad.Tests.csproj
├── HPAutoCad.Loader/                     ← Isolated ALC Loader (net8.0-windows)
│   ├── Ribbon/
│   └── HPAutoCad.Loader.csproj
├── HPAutoCad.McpBridge.Loader/           ← MCP Bridge Loader (Preserved for Mirror Parity)
├── HPAutoCad.McpBridge/                  ← MCP Bridge Runtime
├── HPAutoCad.Aec/                        ← AEC Tool Engine
├── HPAutoCad.Mcp.Server/                 ← Stdio MCP Server
├── HPAutoCad.Aec.Tests/                  ← AEC Unit Tests
├── HPAutoCad.Mcp.Server.Tests/           ← MCP Server Tests
└── tools/
    └── harness/                          ← Unattended Verification Harnesses
```
