# Handoff Report: Milestone M4 Revit Structural Commands & Ribbon Exploration

- **Agent**: `explorer_m4_gen3_1`
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_gen3_1\`
- **Parent Conversation ID**: `1e55a9de-0d52-405d-83fa-fa58bb05a293`
- **Date**: 2026-10-18T09:58:00Z
- **Target Deliverables**: `HPDcePro/src/HPDcePro.Revit/`, `HPDcePro/tests/HPDcePro.Adapters.Tests/`

---

## 1. Observation

### 1.1 Existing State in `HPDcePro/src/HPDcePro.Revit/`
Inspection of `HPDcePro/src/HPDcePro.Revit/` identified five existing source files and one project file:
1. **`HPDcePro.Revit.csproj`**:
   - `TargetFramework`: `net8.0-windows`
   - PackageReferences: `Nice3point.Revit.Api.RevitAPI` (2026.0.4) and `Nice3point.Revit.Api.RevitAPIUI` (2026.0.4) with `PrivateAssets="all"` and `ExcludeAssets="runtime"`.
   - ProjectReferences: `HPDcePro.Core.csproj`, `HPDcePro.UI.csproj`.
2. **`DceProRevitApplication.cs`** (lines 8–73):
   - Implements `Autodesk.Revit.UI.IExternalApplication` (`OnStartup`, `OnShutdown`).
   - Hardcoded tab name `TabName = "HPDcePro"` and single panel `PanelName = "Thiết Kế & Cốt Thép TCVN"`.
   - Registers only two buttons: `HPDcePro_ControlCenter` (`DceProControlCenterCommand`) and `HPDcePro_ExportFraming` (`ExportFramingCommand`).
3. **`DceProControlCenterCommand.cs`** (lines 12–31):
   - Implements `IExternalCommand` (`[Transaction(TransactionMode.Manual)]`).
   - Launches `MainControlCenterWindow` from `HPDcePro.UI.Views`.
4. **`ExportFramingCommand.cs`** (lines 13–47):
   - Implements `IExternalCommand`. Calls `RevitDataExchangeService.CollectAllFramingElements` and `CollectAllColumnElements` and reports element counts in a TaskDialog.
5. **`RevitDataExchangeService.cs`** (lines 9–173):
   - Defines DTOs `RevitFramingElement` (lines 9–20) and `RevitColumnElement` (lines 22–32).
   - Provides pure conversion utilities: `FeetToMm(double feet)` ($feet \times 304.8$) and `MmToFeet(double mm)` ($mm / 304.8$).
   - Provides extraction methods from Revit `FamilyInstance` (`ExtractFramingElement`, `ExtractColumnElement`).
6. **`RevitRebarPlacementService.cs`** (lines 11–261):
   - Defines enums and descriptors: `RevitShapeType`, `RevitHookDescriptor`, `RevitRebarDescriptor`, and `RevitRebarPlacementResult`.
   - Implements pure mappings: `MapBeamRebar(ContinuousBeamResult, Point3D)`, `MapColumnRebar(ColumnDetailingOptions, Point3D)`, `MapPileCapRebar(PileCapResult, PileCapInput, Point3D)`.
   - Implements idempotent tag prefix: `BuildIdempotentTagPrefix(string elementMark) => $"HPDcePro_Rebar_{elementMark}_"`.
   - **Critical Architecture Observation**: This service contains 0 references to `Autodesk.Revit.*` namespaces; it operates entirely on domain calculation results and returns pure geometric rebar descriptors.

### 1.2 Master Truth Matrix & Legacy Binary Survey
1. **`docs/rea-dce-pro/TRUTH_MATRIX.md` (Subsystem 6: lines 603–631)**:
   - Tracks exactly 24 endpoints for Revit:
     - `DBimRevit.AdskApplication` (`0x0200000A`)
     - `DBimRevit.CommandLogin` (`0x02000011`)
     - `DBimRevit.CommandLogout` (`0x02000012`)
     - `DBimRevit.CommandAddBeam` (`0x0200001B`)
     - `DBimRevit.CommandAddColumn` (`0x0200001F`)
     - `DBimRevit.CommandAddGird` (`0x02000021`)
     - `DBimRevit.CommandAddSlab` (`0x02000022`)
     - `DBimRevit.CommandAddWall` (`0x02000025`)
     - `DBimRevit.CommandAutoCut` (`0x02000028`)
     - `DBimRevit.CommandAutoJoin` (`0x0200002A`)
     - `DBimRevit.CommandBatchUnjoin` (`0x0200002C`)
     - `DBimRevit.CommandBeamDisallowJoin` (`0x0200002E`)
     - `DBimRevit.CommandCreateCope` (`0x02000031`)
     - `DBimRevit.CommandJoinByOrder` (`0x02000034`)
     - `DBimRevit.DceBeamBreaks` (`0x0200003A`)
     - `DBimRevit.CommandDatTenDam` (`0x0200004A`)
     - `DBimRevit.CommandDceSetup` (`0x0200004D`)
     - `DBimRevit.CommandExportBeamToExcel` (`0x0200004F`)
     - `DBimRevit.CommandExportToAutocad` (`0x02000051`)
     - `DBimRevit.CommandCreateAnalytical` (`0x02000053`)
     - `DBimRevit.CommandEtabsToRevit` (`0x0200005B`)
     - `DBimRevit.CommandExportToEtabs` (`0x0200005D`)
     - `DBimRevit.CommandXuatKhoiLuong` (`0x02000064`)
     - `DBimRevit.CommandAddRebarBeam` (`0x02000072`)
2. **`scratch/dce_rvt_unpacked/app/2026/AddinDbimDcepro/DBimRevit.dll`**:
   - Binary string & type inspection confirmed all matching command entrypoints and internal worker classes: `DBimRevit.DceAutoJoin`, `DBimRevit.DceBeamCadToRevit`, `DBimRevit.DceBreakBeams`, `DBimRevit.DceCadToRevit`, `DBimRevit.DceExportBeamToExcel`, `DBimRevit.DceExportMbToAutocad`, `DBimRevit.DceExportToEtabs`, `DBimRevit.DceProcess`, `DBimRevit.DceSetName`, `DBimRevit.DceSetup`, `DBimRevit.DceSlabCadToRevit`, `DBimRevit.DceWallCadToRevit`, `DBimRevit.EtabsToRevit`, `DBimRevit.FormSetJoinOrder`, `DBimRevit.RevitCreateAnalytical`, `DBimRevit.RevitToEtabs`, `DBimRevit.RevitXuatKhoiLuong`.
3. **Master Scope `PROJECT.md` & Dispatch Requirements**:
   - Explicitly designates the 23 core structural commands to be migrated:
     `CommandAddBeam`, `CommandAddColumn`, `CommandAddGrid`, `CommandAddSlab`, `CommandAddWall`, `CommandAutoCut`, `CommandAutoJoin`, `CommandAddRebarBeam`, `CommandAddRebarColumn`, `CommandAddRebarWall`, `CommandAddRebarSlab`, `CommandAddRebarFooting`, and all companion commands (`CommandBatchUnjoin`, `CommandBeamDisallowJoin`, `CommandCreateCope`, `CommandJoinByOrder`, `DceBeamBreaks`, `CommandDatTenDam`, `CommandCreateAnalytical`, `CommandXuatKhoiLuong`, `CommandExportBeamToExcel`, `CommandExportToAutocad`, `CommandEtabsToRevit`, `CommandExportToEtabs`, `CommandDceSetup`, `CommandLogin`, `CommandLogout`).

### 1.3 Available Visual Assets in `HPDcePro/assets/Icon/`
Direct filesystem search revealed 138 total icon assets, specifically:
- **`HPDcePro/assets/Icon/IconRevit/`** (13 dedicated icons):
  - `Autocad.png`
  - `Beam.png`
  - `Column.png`
  - `DeleteLink.png`
  - `Etabs.png`
  - `ExportToExcel.png`
  - `Floor.png`
  - `Gird.png` (matches legacy `CommandAddGird` / `CommandAddGrid`)
  - `Join.png`
  - `Name.png`
  - `Rebar.png`
  - `ThongKe.png`
  - `Wall.png`
- **`HPDcePro/assets/Icon/IconCad/`** (supplementary icons for complete coverage):
  - `Cut.png` (for `CommandAutoCut` and `DceBeamBreaks`)
  - `Setup.png` (for `CommandDceSetup`)
  - `Rebarlayout.png` (for `CommandAddRebarColumn`)
  - `HouSe.png` (for `CommandAddRebarFooting`)
  - `Spread.png` (for `CommandAddRebarWall`)
  - `Drawing.png` (for `CommandAddRebarSlab`)
  - `Repair1.png` (for `CommandJoinByOrder`)
  - `Repair2.png` (for `CommandBeamDisallowJoin`)
  - `Repair3.png` (for `CommandCreateCope`)
  - `Calculator.png` (for `CommandCreateAnalytical`)
  - `ETABS.png` (for `CommandExportToEtabs`)
  - `Login.png` / `Logout.png` (for `CommandLogin` / `CommandLogout`)
  - `AutoStr_32.png` (for `DceProControlCenterCommand`)
- **Asset Resolver**:
  `AssetPathResolver.ResolveIcon("IconRevit/Beam.png")` resolves cleanly via `HPDcePro.Core.Common.AssetPathResolver`.

### 1.4 Test Infrastructure in `HPDcePro/tests/HPDcePro.Adapters.Tests/`
- Target: `net8.0-windows`
- References: `xunit.v3` (3.1.0), `HPDcePro.Core`, `HPDcePro.UI`, `HPDcePro.AutoCad`, `HPDcePro.Excel`, `HPDcePro.Mcp`, `HPDcePro.Revit`, `HPDcePro.Desktop`.
- Existing tests (`RevitDataExchangeTests.cs`, `RevitRebarPlacementTests.cs`) pass headlessly in `HPDcePro.Adapters.Tests.exe` without starting Revit.

---

## 2. Logic Chain

1. **Host Boundary & Testability Constraint**:
   - Revit external commands implement `IExternalCommand` and are invoked by Revit's CLR host on the UI thread passing `ExternalCommandData`.
   - Outside `Revit.exe`, instantiating or executing methods that directly call Revit API members (e.g. `doc.Create.NewFamilyInstance`, `FilteredElementCollector`, `JoinGeometryUtils.JoinGeometry`) causes immediate runtime exceptions (`Autodesk.Revit.Exceptions.InvalidOperationException` or `TypeInitializationException`).
   - Therefore, to achieve 100% automated test coverage in `HPDcePro.Adapters.Tests` without needing an interactive Revit session, all geometric generation, structural sizing, validation, spatial indexing, rebar curves, and BOQ arithmetic **must be completely decoupled** from the `IExternalCommand` wrappers.

2. **Decoupled Architecture Pattern (Generator vs Wrapper)**:
   - **Layer A (Pure Generators / Planning Engines)**:
     - Pure C# classes located in `HPDcePro.Revit.Generators`.
     - Zero references to `Autodesk.Revit.DB.*` or `Autodesk.Revit.UI.*`.
     - Accept pure domain models and configuration primitives (`Point3D`, coordinates, millimeter dimensions).
     - Perform all geometric translations (mm $\leftrightarrow$ feet, angle conversions, clearance offsets, polygon area, rebar bending curves, bar counts, weights, idempotent comments).
     - Return strongly-typed immutable Execution Plans (`GridCreationPlan`, `ColumnCreationPlan`, `BeamCreationPlan`, `SlabCreationPlan`, `WallCreationPlan`, `JoinCutPlan`, `BeamBreakPlan`, `RevitRebarPlacementResult`, `TakeoffReportResult`).
     - **100% headlessly testable** in `HPDcePro.Adapters.Tests`.
   - **Layer B (Revit API Execution Wrappers)**:
     - Thin command classes located in `HPDcePro.Revit.Commands` implementing `IExternalCommand`.
     - Decorated with `[Transaction(TransactionMode.Manual)]` and `[Regeneration(RegenerationOption.Manual)]`.
     - Responsible solely for: (1) reading active Revit document elements or prompts; (2) passing data to Layer A Generator; (3) opening a Revit `Transaction`; (4) creating/modifying native Revit elements (`Grid`, `FamilyInstance`, `Floor`, `Wall`, `Rebar`, `JoinGeometryUtils`) according to the plan; (5) committing the transaction.

3. **Ribbon & Application Specification**:
   - RibbonTab must be named `"DCE PRO"`.
   - Must be divided into 4 cohesive engineering panels:
     1. `"Modeling"` (Grids, Columns, Beams, Slabs, Walls, Auto-Name, Analytical).
     2. `"Join/Cut"` (AutoJoin, AutoCut, BatchUnjoin, JoinByOrder, DisallowJoin, CreateCope, BeamBreaks).
     3. `"Reinforcement"` (RebarBeam, RebarColumn, RebarWall, RebarSlab, RebarFooting, BOQ Takeoff).
     4. `"Settings"` (DCE Hub, ExportToExcel, ExportToAutocad, EtabsToRevit, ExportToEtabs, Setup, Login/Logout).
   - Icon loading helper `RevitRibbonHelper.GetRibbonIcon(string relativePath)` dynamically loads images via `AssetPathResolver.ResolveIcon(...)` into frozen `BitmapImage` instances, with defensive error handling to prevent UI crashes if assets are missing.

4. **Clean Code & Build Rule Compliance**:
   - Every file must adhere to Rule C2 ($\le 300$ lines) and Rule M3 ($\le 50$ lines/method).
   - Zero commented-out code (Rule Q-B1) and zero unannotated empty catch blocks (Rule Q-B2).
   - Build must succeed with `TreatWarningsAsErrors=true` using sequential single-node commands (`--disable-build-servers -m:1`).
   - Test execution MUST bypass `dotnet test` and invoke the compiled test runner executable `tests/HPDcePro.Adapters.Tests/bin/Debug/net8.0-windows/HPDcePro.Adapters.Tests.exe` directly to eliminate `FailFast` / `Unknown Hard Error` crashes.

---

## 3. Caveats

1. **Revit Native Element Creation in Production**:
   - While Layer A (Generators) is 100% verified headlessly, actual creation of native elements inside Revit (`FamilyInstance`, `Rebar`) at runtime inside `Revit.exe` depends on the active document having the appropriate loaded families (e.g., Structural Framing and Column families with parameters `b` and `h`, and standard `RebarBarType` symbols).
   - Worker M4's wrappers must include defensive checks: if a family symbol is missing, fallback to the first available symbol or log an informative warning message rather than throwing an unhandled exception.
2. **Revit 2026 Analytical Model API**:
   - In Revit 2026, the analytical model uses `AnalyticalMember` and `AnalyticalPanel` (replacing the deprecated pre-2023 `AnalyticalModel` class). Layer B `CommandCreateAnalytical` must use modern Revit 2026 API methods or wrap calls in conditional safety checks.
3. **No Direct `dotnet test`**:
   - Under no circumstances should `dotnet test` be executed on this repository due to the known `Microsoft.Testing.Platform` named pipe collision. Direct invocation of the compiled test `.exe` must always be used.

---

## 4. Conclusion & Concrete Specification for Worker M4

### 4.1 Master Catalog of 23+ Structural Commands & Ribbon Layout

| # | Command Class | Legacy Token | Ribbon Panel | Ribbon Button Text | Assigned Icon Asset | Headless Generator Class |
|---|---|---|---|---|---|---|
| 1 | `CommandAddGrid` | `0x02000021` | **Modeling** | Lưới Trục | `IconRevit/Gird.png` | `GridGenerator` |
| 2 | `CommandAddColumn` | `0x0200001F` | **Modeling** | Cột BTCT | `IconRevit/Column.png` | `ColumnGenerator` |
| 3 | `CommandAddBeam` | `0x0200001B` | **Modeling** | Dầm BTCT | `IconRevit/Beam.png` | `BeamGenerator` |
| 4 | `CommandAddSlab` | `0x02000022` | **Modeling** | Sàn BTCT | `IconRevit/Floor.png` | `SlabGenerator` |
| 5 | `CommandAddWall` | `0x02000025` | **Modeling** | Vách BTCT | `IconRevit/Wall.png` | `WallGenerator` |
| 6 | `CommandDatTenDam` | `0x0200004A` | **Modeling** | Đặt Tên Dầm | `IconRevit/Name.png` | `BeamNamingGenerator` |
| 7 | `CommandCreateAnalytical`| `0x02000053` | **Modeling** | Mô Hình Giải Tích| `IconCad/Calculator.png` | `AnalyticalModelGenerator` |
| 8 | `CommandAutoJoin` | `0x0200002A` | **Join/Cut** | Auto Join | `IconRevit/Join.png` | `JoinCutPlanGenerator` |
| 9 | `CommandAutoCut` | `0x02000028` | **Join/Cut** | Auto Cut | `IconCad/Cut.png` | `CutPlanGenerator` |
| 10 | `CommandBatchUnjoin` | `0x0200002C` | **Join/Cut** | Tách Join | `IconRevit/DeleteLink.png` | `UnjoinPlanGenerator` |
| 11 | `CommandJoinByOrder` | `0x02000034` | **Join/Cut** | Thứ Tự Join | `IconCad/Repair1.png` | `JoinOrderGenerator` |
| 12 | `CommandBeamDisallowJoin`| `0x0200002E` | **Join/Cut** | Khóa Đầu Dầm | `IconCad/Repair2.png` | `BeamJoinDisallowGenerator` |
| 13 | `CommandCreateCope` | `0x02000031` | **Join/Cut** | Vát Mộng Dầm | `IconCad/Repair3.png` | `CopingPlanGenerator` |
| 14 | `DceBeamBreaks` | `0x0200003A` | **Join/Cut** | Tách Dầm Nhịp | `IconCad/Cut.png` | `BeamBreakGenerator` |
| 15 | `CommandAddRebarBeam` | `0x02000072` | **Reinforcement**| Thép Dầm 3D | `IconRevit/Rebar.png` | `RevitRebarPlacementService` |
| 16 | `CommandAddRebarColumn`| Prompt Spec | **Reinforcement**| Thép Cột 3D | `IconCad/Rebarlayout.png` | `RevitRebarPlacementService` |
| 17 | `CommandAddRebarWall` | Prompt Spec | **Reinforcement**| Thép Vách 3D | `IconCad/Spread.png` | `WallRebarGenerator` |
| 18 | `CommandAddRebarSlab` | Prompt Spec | **Reinforcement**| Thép Sàn 3D | `IconCad/Drawing.png` | `SlabRebarGenerator` |
| 19 | `CommandAddRebarFooting`| Prompt Spec | **Reinforcement**| Thép Đài Móng | `IconCad/HouSe.png` | `RevitRebarPlacementService` |
| 20 | `CommandXuatKhoiLuong` | `0x02000064` | **Reinforcement**| Thống Kê BOQ | `IconRevit/ThongKe.png` | `TakeoffReportGenerator` |
| 21 | `DceProControlCenterCommand`| Hub Entry | **Settings** | DCE Pro Hub | `IconCad/AutoStr_32.png` | `DceRevitSetupManager` |
| 22 | `CommandExportBeamToExcel`| `0x0200004F` | **Settings** | Xuất Excel | `IconRevit/ExportToExcel.png` | `BeamExcelDataExporter` |
| 23 | `CommandExportToAutocad`| `0x02000051` | **Settings** | Xuất CAD | `IconRevit/Autocad.png` | `CadExportPayloadGenerator` |
| 24 | `CommandEtabsToRevit` | `0x0200005B` | **Settings** | ETABS → RVT | `IconRevit/Etabs.png` | `EtabsRevitExchangeGenerator` |
| 25 | `CommandExportToEtabs` | `0x0200005D` | **Settings** | RVT → ETABS | `IconCad/ETABS.png` | `EtabsRevitExchangeGenerator` |
| 26 | `CommandDceSetup` | `0x0200004D` | **Settings** | Cài Đặt | `IconCad/Setup.png` | `DceRevitSetupManager` |
| 27 | `CommandLogin` / `Logout`| `0x02000011` | **Settings** | Đăng Nhập | `IconCad/Login.png` | `DceRevitSetupManager` |

---

### 4.2 File Structure to Implement by Worker M4

```
HPDcePro/src/HPDcePro.Revit/
├── Common/
│   └── RevitRibbonHelper.cs                   <-- Icon loading & ButtonData factory
├── DceProRevitApplication.cs                  <-- Updated 4-panel Ribbon (Tab "DCE PRO")
├── DceProControlCenterCommand.cs              <-- Control Center launcher (existing)
├── ExportFramingCommand.cs                    <-- Framing export (existing)
├── RevitDataExchangeService.cs                <-- Conversion utils (existing)
├── RevitRebarPlacementService.cs              <-- Rebar placement engine (existing)
├── Generators/                                <-- Pure Headless Calculation Engines
│   ├── GridGenerator.cs
│   ├── ColumnGenerator.cs
│   ├── BeamGenerator.cs
│   ├── SlabGenerator.cs
│   ├── WallGenerator.cs
│   ├── BeamNamingGenerator.cs
│   ├── AnalyticalModelGenerator.cs
│   ├── JoinCutPlanGenerator.cs
│   ├── BeamBreakGenerator.cs
│   ├── WallRebarGenerator.cs
│   ├── SlabRebarGenerator.cs
│   ├── TakeoffReportGenerator.cs
│   ├── CadExportPayloadGenerator.cs
│   ├── EtabsRevitExchangeGenerator.cs
│   └── DceRevitSetupManager.cs
└── Commands/                                  <-- IExternalCommand Wrappers
    ├── CommandAddGrid.cs
    ├── CommandAddColumn.cs
    ├── CommandAddBeam.cs
    ├── CommandAddSlab.cs
    ├── CommandAddWall.cs
    ├── CommandDatTenDam.cs
    ├── CommandCreateAnalytical.cs
    ├── CommandAutoJoin.cs
    ├── CommandAutoCut.cs
    ├── CommandBatchUnjoin.cs
    ├── CommandJoinByOrder.cs
    ├── CommandBeamDisallowJoin.cs
    ├── CommandCreateCope.cs
    ├── DceBeamBreaks.cs
    ├── CommandAddRebarBeam.cs
    ├── CommandAddRebarColumn.cs
    ├── CommandAddRebarWall.cs
    ├── CommandAddRebarSlab.cs
    ├── CommandAddRebarFooting.cs
    ├── CommandXuatKhoiLuong.cs
    ├── CommandExportBeamToExcel.cs
    ├── CommandExportToAutocad.cs
    ├── CommandEtabsToRevit.cs
    ├── CommandExportToEtabs.cs
    ├── CommandDceSetup.cs
    ├── CommandLogin.cs
    └── CommandLogout.cs
```

---

### 4.3 Detailed Class & Method Signatures for Worker M4

#### 1. Common Ribbon Helper (`Common/RevitRibbonHelper.cs`)
```csharp
namespace HPDcePro.Revit.Common;

public static class RevitRibbonHelper
{
    public static System.Windows.Media.Imaging.BitmapImage? LoadRibbonIcon(string relativeIconPath);
    public static Autodesk.Revit.UI.PushButtonData CreatePushButton(
        string name, string text, string commandClassName, string iconRelativePath, string tooltip);
}
```

#### 2. Generators (`Generators/*.cs`)

##### A. `GridGenerator.cs`
```csharp
namespace HPDcePro.Revit.Generators;

public sealed record GridItemDef(string Name, Point3D StartMm, Point3D EndMm, bool IsCurved = false, Point3D? ArcCenterMm = null);
public sealed record GridPlanItem(string Name, Point3D StartFeet, Point3D EndFeet, bool IsCurved, Point3D? ArcCenterFeet);
public sealed record GridCreationPlan(IReadOnlyList<GridPlanItem> Items, double TotalLengthM);

public static class GridGenerator
{
    public static GridCreationPlan CreatePlan(IEnumerable<GridItemDef> gridDefs);
}
```

##### B. `ColumnGenerator.cs`
```csharp
namespace HPDcePro.Revit.Generators;

public sealed record ColumnItemDef(string Mark, Point3D LocationMm, double WidthMm, double HeightMm, double RotationDeg, string BaseLevel, string TopLevel);
public sealed record ColumnPlanItem(string Mark, Point3D LocationFeet, double WidthMm, double HeightMm, double RotationRad, string BaseLevel, string TopLevel, string FamilySymbolName);
public sealed record ColumnCreationPlan(IReadOnlyList<ColumnPlanItem> Items, int TotalCount);

public static class ColumnGenerator
{
    public static ColumnCreationPlan CreatePlan(IEnumerable<ColumnItemDef> colDefs);
}
```

##### C. `BeamGenerator.cs`
```csharp
namespace HPDcePro.Revit.Generators;

public sealed record BeamItemDef(string Mark, Point3D StartMm, Point3D EndMm, double WidthMm, double HeightMm, string LevelName, double ZOffsetMm = 0.0);
public sealed record BeamPlanItem(string Mark, Point3D StartFeet, Point3D EndFeet, double WidthMm, double HeightMm, string LevelName, double ZOffsetFeet, double LengthMm, string FamilySymbolName);
public sealed record BeamCreationPlan(IReadOnlyList<BeamPlanItem> Items, double TotalLengthM, double TotalConcreteVolumeM3);

public static class BeamGenerator
{
    public static BeamCreationPlan CreatePlan(IEnumerable<BeamItemDef> beamDefs);
}
```

##### D. `SlabGenerator.cs`
```csharp
namespace HPDcePro.Revit.Generators;

public sealed record SlabItemDef(string Mark, IReadOnlyList<Point3D> OuterBoundaryMm, IReadOnlyList<IReadOnlyList<Point3D>>? OpeningsMm, double ThicknessMm, string LevelName, double ElevationOffsetMm = 0.0);
public sealed record SlabPlanItem(string Mark, IReadOnlyList<Point3D> OuterBoundaryFeet, IReadOnlyList<IReadOnlyList<Point3D>> OpeningsFeet, double ThicknessMm, string LevelName, double ElevationOffsetFeet, double AreaM2, double VolumeM3);
public sealed record SlabCreationPlan(IReadOnlyList<SlabPlanItem> Items, double TotalAreaM2, double TotalVolumeM3);

public static class SlabGenerator
{
    public static SlabCreationPlan CreatePlan(IEnumerable<SlabItemDef> slabDefs);
}
```

##### E. `WallGenerator.cs`
```csharp
namespace HPDcePro.Revit.Generators;

public sealed record WallItemDef(string Mark, Point3D StartMm, Point3D EndMm, double ThicknessMm, double HeightMm, string BaseLevel, string TopLevel);
public sealed record WallPlanItem(string Mark, Point3D StartFeet, Point3D EndFeet, double ThicknessMm, double HeightFeet, string BaseLevel, string TopLevel, double AreaM2, double VolumeM3);
public sealed record WallCreationPlan(IReadOnlyList<WallPlanItem> Items, double TotalLengthM, double TotalVolumeM3);

public static class WallGenerator
{
    public static WallCreationPlan CreatePlan(IEnumerable<WallItemDef> wallDefs);
}
```

##### F. `BeamNamingGenerator.cs`
```csharp
namespace HPDcePro.Revit.Generators;

public sealed record BeamNamingItem(string UniqueId, string CurrentMark, Point3D StartMm, Point3D EndMm, string LevelName);
public sealed record BeamNamingResultItem(string UniqueId, string CurrentMark, string ProposedMark, string StoryPrefix, int SequenceNumber);

public static class BeamNamingGenerator
{
    public static IReadOnlyList<BeamNamingResultItem> GenerateNames(IEnumerable<BeamNamingItem> beams, string prefix = "D");
}
```

##### G. `JoinCutPlanGenerator.cs`
```csharp
namespace HPDcePro.Revit.Generators;

public enum StructuralCategoryRank { Column = 1, Wall = 2, Beam = 3, Slab = 4 }
public sealed record BoundingBox3D(Point3D Min, Point3D Max);
public sealed record StructuralElementProxy(string Id, StructuralCategoryRank Rank, BoundingBox3D BBox);
public sealed record JoinInstruction(string PrimaryId, string SecondaryId, bool ShouldJoin);

public static class JoinCutPlanGenerator
{
    public static IReadOnlyList<JoinInstruction> DetermineJoins(IEnumerable<StructuralElementProxy> elements);
}
```

##### H. `BeamBreakGenerator.cs`
```csharp
namespace HPDcePro.Revit.Generators;

public sealed record ContinuousBeamDef(string Mark, Point3D StartMm, Point3D EndMm, double WidthMm, double HeightMm, string LevelName);
public sealed record BeamBreakResultItem(string SubMark, Point3D StartMm, Point3D EndMm, double SpanLengthMm);
public sealed record BeamBreakPlan(string OriginalMark, IReadOnlyList<BeamBreakResultItem> Spans);

public static class BeamBreakGenerator
{
    public static BeamBreakPlan BreakBeamAtSupports(ContinuousBeamDef beam, IEnumerable<Point3D> supportPointsMm);
}
```

##### I. `WallRebarGenerator.cs`
```csharp
namespace HPDcePro.Revit.Generators;

public sealed record WallRebarInput(string WallMark, double ThicknessMm, double LengthMm, double HeightMm, double VertDiaMm = 12.0, double HorizDiaMm = 10.0, double SpacingMm = 200.0, double CoverMm = 25.0);

public static class WallRebarGenerator
{
    public static RevitRebarPlacementResult GenerateWallRebar(WallRebarInput input, Point3D wallOrigin);
}
```

##### J. `SlabRebarGenerator.cs`
```csharp
namespace HPDcePro.Revit.Generators;

public sealed record SlabRebarInput(string SlabMark, IReadOnlyList<Point3D> BoundaryMm, double ThicknessMm, double BottomDiaMm = 10.0, double BottomSpacingMm = 150.0, double TopDiaMm = 10.0, double TopSpacingMm = 150.0, double CoverMm = 20.0);

public static class SlabRebarGenerator
{
    public static RevitRebarPlacementResult GenerateSlabRebar(SlabRebarInput input, Point3D slabOrigin);
}
```

##### K. `TakeoffReportGenerator.cs`
```csharp
namespace HPDcePro.Revit.Generators;

public sealed record TakeoffItemSummary(string Category, int ElementCount, double ConcreteVolumeM3, double FormworkAreaM2, double SteelWeightKg);
public sealed record TakeoffReportResult(
    double TotalConcreteM3,
    double TotalFormworkM2,
    double TotalSteelKg,
    double SteelDiaSmallKg,   // <= 10mm
    double SteelDiaMediumKg,  // 12mm - 18mm
    double SteelDiaLargeKg,   // > 18mm
    IReadOnlyList<TakeoffItemSummary> Summaries,
    string FormattedTextTable);

public static class TakeoffReportGenerator
{
    public static TakeoffReportResult GenerateReport(
        IEnumerable<RevitFramingElement> framings,
        IEnumerable<RevitColumnElement> columns,
        IEnumerable<SlabPlanItem> slabs,
        IEnumerable<WallPlanItem> walls,
        IEnumerable<RevitRebarDescriptor> rebars);
}
```

#### 3. Command Wrapper Pattern (`Commands/Command*.cs`)
Every command class adheres to this clean, uniform pattern:
```csharp
using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPDcePro.Revit.Generators;

namespace HPDcePro.Revit.Commands;

[Transaction(TransactionMode.Manual)]
[Regeneration(RegenerationOption.Manual)]
public sealed class CommandAddBeam : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var uidoc = commandData?.Application?.ActiveUIDocument;
        if (uidoc == null)
        {
            message = "No active Revit document.";
            return Result.Cancelled;
        }

        var doc = uidoc.Document;

        try
        {
            using var trans = new Transaction(doc, "HPDcePro: Add Beams");
            trans.Start();
            // 1. Gather inputs or selections
            // 2. Invoke BeamGenerator.CreatePlan(...)
            // 3. Create elements via doc.Create.NewFamilyInstance(...)
            trans.Commit();
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            // Reason: Surface error message cleanly to user and abort transaction safely
            message = $"HPDcePro Error: {ex.Message}";
            return Result.Failed;
        }
    }
}
```

---

## 5. Verification Method

### 5.1 Independent Headless Test Suite in `HPDcePro.Adapters.Tests`
Worker M4 must implement comprehensive unit tests in `HPDcePro.Adapters.Tests`:
1. **`RevitCommandRegistrationTests.cs`**:
   - Asserts that all 27 command classes exist, implement `IExternalCommand`, and have `[Transaction(TransactionMode.Manual)]` and `[Regeneration(RegenerationOption.Manual)]` attributes.
2. **`RevitRibbonTests.cs`**:
   - Asserts that `DceProRevitApplication.TabName == "DCE PRO"`.
   - Asserts that all 4 panels ("Modeling", "Join/Cut", "Reinforcement", "Settings") exist and contain the correct push button IDs and icon assets.
   - Verifies that `AssetPathResolver.ResolveIcon` succeeds for all mapped icons.
3. **`RevitGeneratorsModelingTests.cs`**:
   - Tests `GridGenerator`: verifies coordinate conversion, spacing, ordering, curved grids.
   - Tests `ColumnGenerator`: verifies point conversion, width/height, rotation, family symbol resolution.
   - Tests `BeamGenerator`: verifies centerline, length, concrete volume, offsets.
   - Tests `SlabGenerator`: verifies area calculation via shoelace formula, boundary winding, thickness, volume.
   - Tests `WallGenerator`: verifies wall height, thickness, centerline coordinates.
   - Tests `BeamNamingGenerator`: verifies directional sorting and sequential numbering (`T2-D1`, `T2-D2`).
4. **`RevitGeneratorsJoinCutTests.cs`**:
   - Tests `JoinCutPlanGenerator`: verifies AABB collision detection, hierarchy precedence (`Column > Wall > Beam > Slab`).
   - Tests `BeamBreakGenerator`: verifies multi-span splitting, support projection, tolerance filtering.
5. **`RevitGeneratorsRebarTests.cs`**:
   - Tests `WallRebarGenerator`: verifies 2 layers, vertical/horizontal bar counts, boundary element confinement cages.
   - Tests `SlabRebarGenerator`: verifies bottom mesh X/Y and top negative moment strips with 90° hooks.
   - Tests `RevitRebarPlacementService.MapBeamRebar`, `MapColumnRebar`, `MapPileCapRebar` with idempotent tags.
6. **`RevitGeneratorsTakeoffTests.cs`**:
   - Tests `TakeoffReportGenerator`: verifies concrete volume aggregation, formwork surface area, and steel weight grouping across the 3 TCVN diameter tiers ($\le 10$, $12-18$, $> 18$).

### 5.2 Build & Test Verification Commands
Execute sequentially in single-node mode:
```powershell
# 1. Compile solution
dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1

# 2. Run Core domain test suite directly (bypass dotnet test)
& "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Core.Tests\bin\Debug\net8.0\HPDcePro.Core.Tests.exe"

# 3. Run Adapters & Revit test suite directly (bypass dotnet test)
& "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Adapters.Tests\bin\Debug\net8.0-windows\HPDcePro.Adapters.Tests.exe"
```

### 5.3 Invalidation Conditions
This investigation and design would be invalidated if:
1. Any command requires a native Revit API call inside Layer A (Generators) that prevents out-of-process test execution.
2. An icon asset mapped in Section 4.1 is missing from `HPDcePro/assets/Icon/`. (Verified: 100% of mapped icons are present).
3. Any `.cs` file exceeds 300 lines (Rule C2) or any method exceeds 50 lines (Rule M3).
