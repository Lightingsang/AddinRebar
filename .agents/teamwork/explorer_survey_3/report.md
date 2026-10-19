# HPDcePro Architecture, Build Infrastructure & Environment Survey Report

**Author**: `explorer_survey_3` (Teamwork Explorer Agent)  
**Date**: 2026-10-17  
**Working Directory**: `.agents/teamwork/explorer_survey_3/`  
**Target Solution**: `HPDcePro/HPDcePro.slnx`  
**Status**: COMPLETE (Survey & Architecture Plan)

---

## 1. Executive Summary

This survey report provides the architectural blueprint, infrastructure validation, clean-code governance analysis, and scaffolding plan for **HPDcePro** (`HPDcePro/HPDcePro.slnx`), the clean-room C# rewrite of the legacy structural engineering software suite located in `C:\Dce Pro`.

### Key Findings
1. **Build Infrastructure & Tooling**:
   - The repository strictly standardizes on `.slnx` solution files, pinned to .NET SDK `10.0.300` (with `rollForward: latestMinor`, running on installed SDK `10.0.400`), using `Microsoft.Testing.Platform` (MTP) and `xunit.v3`.
   - Multi-host deliverables (`HPRebar/`, `HPAutoCad/`, `HPKata/`, `HPBim/`, `HPExcel/`, etc.) maintain strict physical isolation: they depend only on `McpShared/` and never reference sibling product folders.
   - The verified fastest and cleanest sibling architecture is **`HPKata`**, which recently migrated its entire UI layer to `MahApps.Metro` `2.4.11`, achieving 0 warnings, 0 errors, and sub-3-second builds with `--disable-build-servers -m:1`.

2. **.NET Environment & NuGet Cache**:
   - All necessary dependencies are **already fully cached and verified** in `%USERPROFILE%\.nuget\packages`:
     - `MahApps.Metro` 2.4.11 & `ControlzEx` 4.4.0
     - `ClosedXML` 0.104.2
     - `Microsoft.Data.Sqlite` 10.0.12 & `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12
     - `CommunityToolkit.Mvvm` 8.4.0
     - `xunit.v3` 3.1.0 & `xunit.runner.visualstudio` 3.1.5
     - `Microsoft.Testing.Platform` 2.3.2
     - `AutoCAD.NET` 25.1.0
     - `Nice3point.Revit.Api.RevitAPI` / `RevitAPIUI` 2026.0.4
   - Zero external network downloads are needed for package restore.

3. **C:\Dce Pro Legacy Asset Verification**:
   - All legacy assets in `C:\Dce Pro` were verified as existing and accessible in read-only mode:
     - `DCE Pro.dll` (46.7 MB), `DceAnalyssiss64.dll` (346.6 KB), `DCE Pro.lsp` (25.8 KB)
     - `AutoStr\DuAnMau.autostr` (14.2 KB JSON project definition)
     - `MayHoc\mauhoc.db` (8.9 MB SQLite database with 45,020 training rows for rebar text token labeling)
     - `AddinSteel\DceSteel.dll` (21.3 MB), `AddinSteelCad\DbimSteelAutocad.dll` (4.4 MB)
     - `V.2020\DCE Excel.dll` (11.3 MB), `AddinSetup_RVT\DBIM_RVT_26.exe` (9.9 MB)
   - All legacy licensing, telemetry, serial checking, and updaters (`LicenseMoi`, `LicenseCheckAPI`, `DbimTaiKhoanApi`, `DceSerinumber`, `DceUpdate`) are strictly identified for total omission in `HPDcePro`.

---

## 2. Repository Architecture & Build Infrastructure

### 2.1 Repository Layout & Separation of Concerns

The repository enforces modular separation of concerns governed by `AGENTS.md` and `docs/architecture/ARCHITECTURE.md`:

```
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\
├── HPRebar/             # Revit Add-In (R23-R27) + Revit MCP Bridge
├── HPAutoCad/           # AutoCAD 2026 MCP Bridge + AEC Engine + HPGeoLink
├── HPKata/              # Dual-host Structural Rebar Suite (AutoCAD & Revit 2026)
├── HPBim/               # High-level BIM Deliverable automating 3D BIM models
├── HPEtabs/             # ETABS 22 Out-of-Process COM Bridge
├── HPExcel/             # Excel COM & ClosedXML MCP Bridge
├── McpShared/           # Shared host-neutral MCP contracts, Roslyn compiler, pipe engine
├── HPDcePro/            # [NEW] Structural engineering rewrite (Core, Tests, UI, CAD, Excel, Revit)
└── docs/                # Architectural, clean code, and REA documentation
```

**Key Isolation Rules**:
1. MCP and product folders depend **only** on `McpShared/` if MCP communication is required; product folders never cross-reference each other.
2. `HPDcePro.Core` is a pure C# engine with **zero references** to `Autodesk.AutoCAD.*`, `Autodesk.Revit.*`, or Microsoft Office COM assemblies.
3. Every deliverable owns its own `Directory.Build.props`, `global.json`, and `.slnx`.

### 2.2 Global.json and Build Properties

Each project directory specifies its own `global.json` pinning the MTP runner:
```json
{
  "sdk": {
    "version": "10.0.300",
    "rollForward": "latestMinor",
    "allowPrerelease": true
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

The recommended `HPDcePro/Directory.Build.props` incorporates lessons learned from `HPKata` and `HPBim`:
```xml
<Project>
  <!--
    Directory.Build.props for HPDcePro.
    Clean-room C# Structural Engineering Suite.
    Zero Autodesk API references in HPDcePro.Core.
  -->
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <NoWarn>$(NoWarn);xUnit1051;NU1701</NoWarn>
    <UseSharedCompilation>false</UseSharedCompilation>
    <BuildInParallel>false</BuildInParallel>
    <TestingPlatformCommandLineArguments>--results-directory "$([System.IO.Path]::Combine($([System.IO.Path]::GetTempPath()), 'hpdcepro-mtp', $([System.Guid]::NewGuid().ToString('N'))))"</TestingPlatformCommandLineArguments>
  </PropertyGroup>
</Project>
```

**Critical MTP Testing Discovery**:
When running `dotnet test` with flags like `--disable-build-servers -m:1`, the dotnet CLI can forward MSBuild flags to the test executable. To ensure seamless operation with `Microsoft.Testing.Platform` and `xunit.v3`, test projects must include:
```xml
<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
```
This enables native CLI parsing without exit code 5 help dumps.

---

## 3. Clean Code Governance & Review Checklist

All code in `HPDcePro` must comply with `docs/clean-code/HP_CLEAN_CODE_CORE.md` and `docs/clean-code/CODE_REVIEW_CHECKLIST.md`:

### 3.1 Non-Negotiable Hard Blocking Rules
- **Rule C2**: File $\le 300$ lines. ViewModel $\le 250$ lines.
- **Rule M3**: Method $\le 50$ lines. Methods $>20$ lines require review scrutiny; $>50$ lines must be split unless a cohesive, flat table/switch.
- **Rule Q-B1**: Zero dead or commented-out code.
- **Rule Q-B2**: Zero empty `catch` blocks without an explicit reason comment (e.g. `// Fallback for modeless display`).
- **Rule FM2**: Always brace blocks (including single-line `if`). Lines $\le 120$ characters.
- **Compiler Rules**: `TreatWarningsAsErrors=true`, 0 warnings, 0 errors. Build command: `dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1`.

### 3.2 Design & Architectural Rules
- **Rule N1–N10**: Types and fields are nouns; methods are verbs; Booleans read as positive questions (`IsValid`, `HasHooks`); units in names (`...Mm`, `...Ft`, `...Deg`, `...KN`); test naming `Method_Scenario_ExpectedResult`.
- **Rule M6**: Pure functions in `HPDcePro.Core`. Output depends only on inputs; no mutation of parameters; no `ref` accumulators or shared mutable warning lists.
- **Rule D2 & D3**: Constructors are trivial (assign and guard only, no I/O, no queries). Static is allowed only for pure stateless calculations and stateless adapters.
- **Rule R5**: In Revit-facing code (`HPDcePro.Revit`), `304.8` must appear only inside `RevitUnitConverter.cs`. Core uses millimeters ($mm$) exclusively.
- **Rule P5**: WPF dialogs use `MahApps.Metro` 2.4.11 (`mah:MetroWindow`), dynamic resource tokens, and Segoe UI product font.
- **Rule P7**: No unapproved package references or external network dependencies.

---

## 4. .NET Environment & Dependency Assessment

### 4.1 Installed SDKs and Runtimes
- **Installed .NET SDKs**:
  - `10.0.400` in `C:\Program Files\dotnet\sdk` (Active compiler via `rollForward: latestMinor`)
  - `9.0.317` in `C:\Program Files\dotnet\sdk`
- **Installed .NET Runtimes**:
  - .NET 8.0: `8.0.4`, `8.0.20`, `8.0.30` (Microsoft.NETCore.App and Microsoft.WindowsDesktop.App)
  - .NET 10.0: `10.0.9`, `10.0.11` (Microsoft.NETCore.App and Microsoft.WindowsDesktop.App)
  - .NET 9.0: `9.0.19`

### 4.2 Local NuGet Package Cache Verification

| Package | Version in Cache | Purpose in HPDcePro |
|---|---|---|
| `MahApps.Metro` | `2.4.11` | Primary WPF theme, `MetroWindow` chrome, Outlined styles |
| `ControlzEx` | `4.4.0` | Transitive theme dependency of MahApps.Metro |
| `CommunityToolkit.Mvvm` | `8.4.0` | MVVM pattern (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`) |
| `ClosedXML` | `0.104.2` | Headless OpenXML Excel rebar schedule & report generation |
| `Microsoft.Data.Sqlite` | `10.0.12` | SQLite inspection and querying of `MayHoc/mauhoc.db` |
| `SQLitePCLRaw.bundle_e_sqlite3` | `2.1.12` | Native SQLite library for Windows x64 |
| `xunit.v3` | `3.1.0` | Core unit & integration test framework |
| `xunit.runner.visualstudio` | `3.1.5` | Visual Studio & MTP test adapter |
| `Microsoft.Testing.Platform` | `2.3.2` | High-performance Microsoft test runner engine |
| `AutoCAD.NET` | `25.1.0` | AutoCAD 2026 API (`acdbmgd`, `acmgd`, `accoremgd`) |
| `Nice3point.Revit.Api.RevitAPI` | `2026.0.4` | Revit 2026 API reference assembly |
| `Nice3point.Revit.Api.RevitAPIUI` | `2026.0.4` | Revit 2026 UI API reference assembly |

All required dependencies are fully present on the local development machine.

---

## 5. Reverse-Engineering Analysis of `C:\Dce Pro`

A read-only inspection of the shipped binaries and data files revealed key structural details:

### 5.1 Project Configuration Schema (`DuAnMau.autostr`)
`C:\Dce Pro\AutoStr\DuAnMau.autostr` is a structured JSON file configuring the 24-step structural detailing pipeline:
- **Project Identity**: `TenDuAn`, `ProjectRootPath`, `ExcelGocPath`.
- **Material Specs**: `MacBtCotVach` (e.g. "B20"), `MacBtDamSan` ("B20"), `MacThep` ("CB400-V").
- **Typical Dimensions**: `BeRongDamDienHinh` (220.0), `ChieuCaoDamDienHinh` (400.0), `ChieuDaySanDienHinh` (120.0), `ChieuDaySanWC` (100.0).
- **Foundation Settings**: `ThepMongTrenX`, `ThepMongDuoiX`, `MongDayBTLot` (100.0), `MongNgamCoc` (100.0), `PrefixTenDai` ("ĐC.").
- **Layer Mappings**: `Layers` sub-object mapping architectural layers (`KT_TRUC`, `KT_COT`, `DAM`) to structural output layers (`DCE_Truc`, `DCE_Beam`, `DCE_BaoBeTong`, `DCE_San_Duoi`).
- **Folder & Prefix Mappings**: Output folders (`01.SheetSet`, `07.MatBangKetCau`, `10.ChiTietDam`) and prefixes (`MBKC`, `ChiTietDam`, `ChiTietCot`).

### 5.2 ML NLP Rebar Parser Database (`MayHoc/mauhoc.db`)
- `mauhoc.db` is an SQLite database containing **45,020 rows** in table `MauHoc`.
- Columns: `Id`, `TextGoc`, `ChiSoSo`, `GiaTri`, `Feature`, `Nhan`, `Nguon`, `ThoiGian`, `Hash`.
- **Domain Purpose**: It is a token-level NLP classifier for parsing Vietnamese structural engineering rebar callouts on drawings (e.g. `[1b] 3x∅20, L= 11700` or `2∅18 + 1∅20`).
- **Label Set (`Nhan`)**:
  - `SH`: Số hiệu thanh (Bar Mark, e.g. `1b`)
  - `SL`: Số lượng thanh (Quantity, e.g. `3`)
  - `DK`: Đường kính thanh (Diameter, e.g. `20`)
  - `KC`: Khoảng cách đai (Spacing, e.g. `a150`)
  - `CD`: Chiều dài (Length, e.g. `11700`)
- **Features (`Feature`)**: Contextual token feature vectors (e.g. `PREV_lbr NEXT_b AFTERL_0 INRING_1 DKCHUAN_0 NDIGIT_1 INSEQ_0 BEFORED_0 AFTERD_0 SHLET_1`).
- `HPDcePro.Core` can parse and evaluate these patterns in pure C# using both regex rule-based engines and SQLite feature lookup.

### 5.3 Command & LISP Inventory (`DCE Pro.lsp`)
`C:\Dce Pro\DCE Pro.lsp` exposes 40+ AutoCAD commands:
- **Beam Detailing**: `ShopDrawingBeam` (`sdb`), `VeDam_DCE` (`vd`), `VeDamAll_DCE` (`vdall`), `VeMCD_DCE` (`mcd`), `DatTenCauKienDam` (`ten`), `GanTrucBlockDam` (`GanTruc`), `VeBlockDam2` (`bb1`), `VeBlockSection` (`bb7`).
- **Rebar Schedule & Optimization**: `ToiUuCatThep` (`tuct`), `TKTSSOLUONGCAUKIEN` (`slck`), `TKTSDUONGKINHTHEP` (`sdkt`), `TongHopThongKe_dce` (`THTK_dce`), `tkt_dce`, `stk_dce`, `QuetThongKe_dce`, `MaTTThep`.
- **Column / Wall**: `VeCotDcePro` (`vc`), `VeVachDcePro` (`vv`), `ColumnREC1`, `ColumnREC2`, `ColumnCi`.
- **Slab Detailing**: `VeThepSanTopX` (`T1`), `VeThepSanTopY` (`T2`), `VeThepSanBotX` (`B1`), `VeThepSanBotY` (`B2`), `SoHieuThepSan` (`shts`), `ThongKeThepSan` (`tkts`), `ChiaKhoangCach` (`cf`).
- **Foundation & Piles**: `VeMongCoc_DCE`, `VeMongDon_DCE`, `VeBlockCoc` (`VBC`), `DanhTenCoc` (`DTC`), `XuatToaDoCoc` (`XTDC`), `StripMong`, `TextDaiMong`, `RaiDaiCoc` (`rdc`).
- **ETABS Integration**: `XuatDuLieuModelEtabs` (`mb1`), `UpdateModelEtabs`.

### 5.4 Native C++ Replacement (`DceAnalyssiss64.dll`)
`DceAnalyssiss64.dll` (`ClassLibrary1.DAnalysisss`) was a native C++ P/Invoke library performing 2D polygon clipping, point-in-polygon tests, and polyline offset calculations. `HPDcePro.Core` replaces this entirely with pure managed C# 2D geometry algorithms (`Polygon2D`, `Point2D`, `Vector2D`, `Segment2D`), providing zero P/Invoke dependencies and complete cross-platform testability.

### 5.5 Complete Omission of Legacy Licensing & DRM
The following legacy classes and endpoints are identified for **complete exclusion**:
- `LicenseMoi`, `LicenseCheckAPI`, `DceSerinumber`
- `DbimTaiKhoanApi`, `DceUpdate`
`HPDcePro` is clean, independent, open, and requires no phone-home or dongle checks.

---

## 6. Proposed Solution Architecture: `HPDcePro/HPDcePro.slnx`

### 6.1 Solution File Structure

```xml
<Solution>
  <Configurations>
    <BuildType Name="Debug" />
    <BuildType Name="Release" />
  </Configurations>
  <Folder Name="/Solution Items/">
    <File Path="global.json" />
    <File Path="Directory.Build.props" />
    <File Path="README.md" />
  </Folder>
  <Project Path="HPDcePro.Core/HPDcePro.Core.csproj" />
  <Project Path="HPDcePro.Core.Tests/HPDcePro.Core.Tests.csproj" />
  <Project Path="HPDcePro.UI/HPDcePro.UI.csproj" />
  <Project Path="HPDcePro.AutoCad/HPDcePro.AutoCad.csproj" />
  <Project Path="HPDcePro.Excel/HPDcePro.Excel.csproj" />
  <Project Path="HPDcePro.Revit/HPDcePro.Revit.csproj" />
</Solution>
```

### 6.2 Project Profiles & Responsibilities

#### 1. `HPDcePro.Core` (Pure C# Domain Engine)
- **Target Framework**: `<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>`
- **Dependencies**: `ClosedXML` (0.104.2), `Microsoft.Data.Sqlite` (10.0.12), `System.Text.Json` (8.0.5), `Polyfill` (11.0.1). Zero CAD/Revit references.
- **Key Modules**:
  - `Geometry/`: Pure 2D geometry (`Point2D`, `Vector2D`, `Segment2D`, `Polygon2D`, `Polyline2D`, `BoundingBox2D`, point-in-polygon, intersection, offset).
  - `AutoStr/`: `.autostr` JSON serialization/deserialization (`AutoStrProjectModel`, `AutoStrSettings`, `LayerMappingConfig`).
  - `XData/`: CAD XData packet encoding/decoding (`RebarXDataDce`, `DceRebarXDataTkt`, `LinksXDataObject`) with corrupt data recovery.
  - `Calculation/`:
    - `Tcvn5574BeamCalculator`: Rectangular & T-section flexure ($M_{Rd}$), balanced ratio $\xi_R$, minimum reinforcement ratio $\mu_{\min}$, shear capacity ($Q_{sw}$), crack width limit ($a_{crc}$), deflection check.
    - `Tcvn10304PileCalculator`: Pile axial bearing capacity based on soil SPT/CPT parameters.
    - `Tcvn5575SteelCalculator`: Steel section capacity (tension, compression, bending, lateral-torsional buckling).
    - `Tcvn2737WindCalculator`: Wind pressure distribution ($W_k$) by zone (I–V), terrain category (A–D), and height.
  - `Optimization/`:
    - `RebarCuttingStockOptimizer`: 1D cutting stock optimization for standard 11,700 mm commercial rebar lengths. Automatic lap splice calculation ($30d$–$40d$), scrap waste minimization (< 3–5%), cutting schedule generation.
  - `Schedule/`:
    - `RebarScheduleAggregator`: Weight summarization, classification by diameter groups ($\le 10$ mm, $10$–$18$ mm, $> 18$ mm), bar shape catalog.
  - `MayHoc/`:
    - `MayHocDbReader`: SQLite reader for `MayHoc/mauhoc.db` with connection pool clearing.
    - `RebarCalloutTokenParser`: Regex + SQLite token classifier extracting `SH`, `SL`, `DK`, `KC`, `CD` from Vietnamese rebar strings.

#### 2. `HPDcePro.Core.Tests` (Comprehensive Automated Test Suite)
- **Target Framework**: `net10.0`
- **Output Type**: `Exe` (speaks Microsoft.Testing.Platform natively)
- **Settings**: `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>`, `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>`
- **Dependencies**: `xunit.v3` (3.1.0), `xunit.runner.visualstudio` (3.1.5), `Microsoft.Data.Sqlite` (10.0.12), `ClosedXML` (0.104.2). References `HPDcePro.Core`.
- **Test Coverage**:
  - `Geometry2DTests.cs`: Polygon area, centroid, point-in-polygon, intersection, winding order.
  - `AutoStrSerializationTests.cs`: Parses real `C:\Dce Pro\AutoStr\DuAnMau.autostr` with full fidelity.
  - `MayHocDbTests.cs`: Queries real `C:\Dce Pro\MayHoc\mauhoc.db` and tests token parsing accuracy.
  - `XDataCodecTests.cs`: Round-trip encoding/decoding and error resilience against corrupt packets.
  - `Tcvn5574BeamTests.cs`: Sizing, minimum steel check, double reinforcement, stirrup spacing.
  - `RebarCuttingOptimizerTests.cs`: 11.7m commercial bar slicing, lap splice insertion, waste minimization.
  - `CivilEngineeringFormulaTests.cs`: Validates TCVN 10304 pile, TCVN 5575 steel, TCVN 2737 wind calculations.

#### 3. `HPDcePro.UI` (WPF MVVM Dialog Layer with MahApps.Metro)
- **Target Framework**: `net8.0-windows` (`UseWPF: true`)
- **Dependencies**: `MahApps.Metro` (2.4.11), `CommunityToolkit.Mvvm` (8.4.0). References `HPDcePro.Core`.
- **Theme Infrastructure**:
  - `Common/ThemeManager.cs`: Preloads `MahApps.Metro` and `ControlzEx` assemblies before XAML parsing; dynamic switching between `Dark.Blue` and `Light.Blue`.
  - `Themes/MaterialBridge.xaml`: Merges MahApps Controls, Fonts, Dark.Blue dictionaries.
  - `Themes/ThemeDark.xaml` & `Themes/ThemeLight.xaml`: Semantic color tokens (`Brush.Background`, `Brush.Surface`, `Brush.Accent`, `Brush.Text`, `Brush.Border`).
  - `Themes/Theme.xaml`: Consistent controls (`Text.Title`, `StandardTextBox`, `StandardComboBox`, `Card.Panel`, `GroupBox.Header`, `PrimaryButton`, `SecondaryButton`, `StandardDataGrid`).
- **Views & ViewModels**:
  - `Views/BeamDetailingWindow.xaml` / `ViewModels/BeamDetailingViewModel.cs`: Beam span geometry, rebar layers, stirrup spacing, shop 11.7m mode toggle.
  - `Views/CuttingOptimizationWindow.xaml` / `ViewModels/CuttingOptimizationViewModel.cs`: Bar cutting list input, stock length (11,700 mm), optimization run, visual cutting patterns & scrap percentage.
  - `Views/AutoStrConfigWindow.xaml` / `ViewModels/AutoStrConfigViewModel.cs`: `.autostr` project loader, material grade selectors, layer mapping preview.

#### 4. `HPDcePro.AutoCad` (AutoCAD 2026 Command Adapter & LISP Bridge)
- **Target Framework**: `net8.0-windows` (`UseWPF: true`)
- **Dependencies**: `AutoCAD.NET` (25.1.0, private/exclude runtime), `CommunityToolkit.Mvvm` (8.4.0), `MahApps.Metro` (2.4.11), `ClosedXML` (0.104.2). References `HPDcePro.Core` and `HPDcePro.UI`.
- **Packaging & Deployment**:
  - Auto-deploys `HPDcePro.bundle` (`PackageContents.xml`) to `%AppData%\Autodesk\ApplicationPlugins\HPDcePro.bundle\`.
  - Detects if AutoCAD locks the assembly and logs a clean notice without failing the build.
- **Commands & LISP Bridge**:
  - Preserves legacy command names (`ShopDrawingBeam`, `VeDam_DCE`, `ToiUuCatThep`, `TKTSSOLUONGCAUKIEN`, `AutoStr`, `XuatDuLieuModelEtabs`).
  - Provides clean `HPDcePro.lsp` with short aliases (`sdb`, `vdd`, `tuct`, `slck`, `sdkt`).

#### 5. `HPDcePro.Excel` (Headless ClosedXML & Calculation Export)
- **Target Framework**: `net8.0`
- **Dependencies**: `ClosedXML` (0.104.2). References `HPDcePro.Core`.
- **Key Adapters**:
  - `ExcelScheduleExporter`: Generates publication-ready `.xlsx` rebar schedules and BOQ sheets headlessly without Excel installed.
  - `ExcelCalculationReportExporter`: Generates TCVN 5574:2018 calculation verification sheets with formulas and checks.
  - `ExcelComInteropAdapter`: Safe COM interop with deterministic RCW release in `finally` blocks when interacting with active Excel workbooks.

#### 6. `HPDcePro.Revit` (Revit 2026 Structural Data Exchange Adapter)
- **Target Framework**: `net8.0-windows` (`UseWPF: true`)
- **Dependencies**: `Nice3point.Revit.Api.RevitAPI` (2026.0.4), `Nice3point.Revit.Api.RevitAPIUI` (2026.0.4), `CommunityToolkit.Mvvm` (8.4.0), `MahApps.Metro` (2.4.11). References `HPDcePro.Core` and `HPDcePro.UI`.
- **Packaging & Deployment**:
  - Auto-deploys `HPDcePro.Revit.addin` and binaries to `%AppData%\Autodesk\Revit\Addins\2026\`.
- **Services**:
  - `RevitElementGeometryReader`: Pure dimension extraction ($b, h, L$) from selected structural framing in mm.
  - `RebarCreationService`: Native Revit `Rebar` generator.
  - External commands: `BeamDetailingCommand`, `CuttingOptimizationCommand`.

---

## 7. Phased Implementation Roadmap & Risk Management

### 7.1 Multi-Phase Delivery Plan

| Phase | Scope | Deliverables |
|---|---|---|
| **Phase 1** | **Foundational Architecture, Geometry Core, .autostr I/O, TCVN 5574 RC Beam, 1D Cutting Stock, SQLite MauHoc, & Multi-Host Adapters** | Full `HPDcePro.slnx` scaffold, pure 2D geometry, .autostr JSON serializer, XData codec, TCVN 5574 beam calculator, 1D cutting optimizer (11.7m), `mauhoc.db` token parser, MahApps.Metro UI dialogs, AutoCAD/Excel/Revit project skeletons, and $\ge 80$ unit tests in `HPDcePro.Core.Tests`. |
| **Phase 2** | **Full Beam Ecosystem, Foundation Beams & ETABS Bidirectional Bridge** | Foundation beams (`VeDamMong`), inclined/curved beams, beam intersections (`dam_giao`), ETABS model extraction (`XuatDuLieuModelEtabs`) and update (`UpdateModelEtabs`). |
| **Phase 3** | **Column & Shear Wall Detailing** | Multi-story column detailing (`VeCotDcePro`), crank bends, lap splice zones, shear wall core detailing (`VeVachDcePro`), boundary elements. |
| **Phase 4** | **Foundation, Slab, Stair & Lintel Detailing** | Pile caps (`VeMongCoc_DCE`), pad footings (`VeMongDon_DCE`), two-way slabs (`VeThepSanTopX`/`BotX`), stair detailing (`VeThang_DCE`), lintels. |
| **Phase 5** | **Quantity Surveying (QS), Drawing Utilities & AutoStr 24-Step Automation** | Automated structural drawing production (`AutoStrStep01`..`Step24`), sheet index (`danhmuc`), batch print/export. |
| **Phase 6** | **Steel Structure Integration & Advanced AI Tuning** | Porting `DceSteel` & `DbimSteelAutocad` (TCVN 5575:2024), advanced ML rebar layout predictor from `mauhoc.db`. |

### 7.2 Risk Assessment & Mitigation

1. **Risk: Microsoft.Testing.Platform CLI Argument Mismatch**
   - *Detail*: Passing MSBuild options (`--disable-build-servers -m:1`) directly to `dotnet test` can cause MTP to dump help text (exit code 5).
   - *Mitigation*: Ensure `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>` is configured in `HPDcePro.Core.Tests.csproj` and `<TestingPlatformCommandLineArguments>` is configured in `Directory.Build.props`.

2. **Risk: WPF StaticResource / XamlParseException in Hosted Add-Ins**
   - *Detail*: In-process hosts (AutoCAD/Revit) load assemblies dynamically, causing WPF XAML parser to fail when looking up `MahApps.Metro` styles.
   - *Mitigation*: Replicate `HPKata`'s battle-tested `ThemeManager.EnsureAssembliesLoaded()` and contextual reflection scope before `InitializeComponent()`.

3. **Risk: SQLite File Locking on Windows**
   - *Detail*: Accessing `C:\Dce Pro\MayHoc\mauhoc.db` during unit tests could lock the file or crash if connections are not disposed.
   - *Mitigation*: Always open connections with read-only connection strings (`Data Source=...;Mode=ReadOnly;`), use `using` statements, and invoke `Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools()` in test cleanup/fixtures.

4. **Risk: Legacy Licensing & Obsolete Dependencies**
   - *Detail*: Accidental inclusion of legacy security/licensing logic (`LicenseMoi`, `DceSerinumber`).
   - *Mitigation*: Maintain a strict clean-room perimeter; no reference or porting of licensing files.

---

## 8. Conclusion

The repository environment and build infrastructure are in optimal health. All dependencies for `HPDcePro` are cached locally and verified. The proposed architecture for `HPDcePro/HPDcePro.slnx` faithfully addresses all user requirements, honors the repository's Clean Code Governance, and provides a clear, robust foundation for Phase 1 implementation.
