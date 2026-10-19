# Handoff Report — M2 AutoCAD & Revit Commands Excel Sync (Features 7 & 8)

- **Agent**: `explorer_hpkata_m2_3` (M2 Host Commands Explorer)
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_3\`
- **Target Scope**: Milestone M2, Features 7 & 8 (`HPKata/PROJECT.md`)
- **Authoritative Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- **Target Solution**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`

---

## 1. Observation

### 1.1 Solution Baseline & Build Verification
- Running `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` succeeds with:
  ```
  Build succeeded.
      0 Warning(s)
      0 Error(s)
  Time Elapsed 00:00:13.53
  ```
- Running `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1 --no-build` passes 100% across all 5 test assemblies:
  - `HPKata.AutoCad.Tests`: 75 passed (0 failed, 0 skipped)
  - `HPKata.Core.Tests`: 561 passed (0 failed, 0 skipped)
  - `HPKata.E2E.Tests`: 232 passed (0 failed, 0 skipped)
  - `HPKata.Revit.Tests`: 92 passed (0 failed, 0 skipped)
  - `HPKata.UI.Tests`: 59 passed (0 failed, 0 skipped)
  - **Total**: 1,019 tests passing.

### 1.2 AutoCAD Detailing Commands Audit
Direct code inspection of `HPKata.AutoCad`:
1. **Continuous Beam Commands** (`HPKata.AutoCad/Commands/ContinuousBeamCommands.cs`, lines 12-35):
   - `[CommandMethod("ve_dam")]` / `[CommandMethod("vd")]` invokes `BeamDrawController.DrawContinuousBeamInteractive()`.
   - `[CommandMethod("ve_dam_shop")]` / `[CommandMethod("vds")]` invokes `BeamShopController.DrawBeamShopInteractive()`.
   - `[CommandMethod("ve_dam_mc")]` / `[CommandMethod("vdmc")]` invokes `BeamSectionController.DrawBeamSectionInteractive()`.
   - In `BeamDrawController.cs` (lines 29-39): currently instantiates a hardcoded `CreateDefaultLayout()`, bypassing `BeamTakeoffRegistry` and `Kata.xlsm`. Generated `Polyline` entities are created on the active layer without setting canonical layers (`kata_thep chu`, `kata_net thay`) and without attaching `KATA` XData.
   - In `BeamShopController.cs` (lines 31-42): segments commercial bars with `BeamCurtailmentCalculator`, but adds lines without proper layers or `KATA` XData.
   - `loadbeam` currently lives in `EtabsBeamCommands.cs` (line 15) and delegates to `BeamEtabsController.LoadFromExcelKataDam()`, which only instantiates `BeamDetailingWindow` without reading `Kata.xlsm` sheet `Dam`.

2. **Column Commands** (`HPKata.AutoCad/Commands/ColumnCommands.cs`, lines 12-36):
   - `[CommandMethod("ve_cot")]` / `[CommandMethod("vc")]` invokes `ColumnDrawController.DrawColumnInteractive()`.
   - `[CommandMethod("ve_cot_shop")]` / `[CommandMethod("vcs")]` invokes `ColumnShopController.DrawColumnShopInteractive()`.
   - `[CommandMethod("loadcolumn")]` invokes `ColumnEtabsController.LoadColumnInteractive()`.
   - In `ColumnEtabsController.cs` (lines 21-46): prompts for an ETABS tab-delimited text file instead of reading `Kata.xlsm` sheet `Cot`.
   - In `ColumnDrawController.cs` (lines 93-152): draws concrete profile and longitudinal bars/ties, but does not apply canonical layers or `KATA` XData.
   - In `ColumnShopController.cs` (lines 54-92): generates commercial splices (Tier A / Tier B) and starter dowels, but leaves layers and XData unassigned.

3. **Wall Commands** (`HPKata.AutoCad/Commands/WallCommands.cs`, lines 12-36):
   - `[CommandMethod("ve_vach")]` / `[CommandMethod("vv")]` invokes `WallDrawController.DrawWallInteractive()`.
   - `[CommandMethod("ve_vach_shop")]` / `[CommandMethod("vvs")]` invokes `WallShopController.DrawWallShopInteractive()`.
   - `[CommandMethod("loadwall")]` invokes `WallEtabsController.LoadWallInteractive()`.
   - In `WallEtabsController.cs` (lines 21-46): parses ETABS text table instead of reading `Kata.xlsm` sheet `Vach`.
   - In `WallDrawController.cs` (lines 92-170) & `WallShopController.cs` (lines 45-86): draws wall outlines, boundary elements, web mesh, and U-bars without canonical layers or `KATA` XData.

4. **Excel Export Commands** (`HPKata.AutoCad/Commands/ScheduleCommands.cs`, lines 68-78):
   - `[CommandMethod("toexcel")]` and `[CommandMethod("toexcelqh")]` invoke `ExportCommandRunner.ExecuteExcelExport`.
   - In `ExportCommandRunner.cs` (lines 28-30): writes to a default path `{DrawingName}_ThongKe.xlsx` via `ClosedXmlExporter.ExportScheduleToFile` instead of writing back into `KataInstallEnvironment.Current.KataWorkbookPath` (`C:\HPKata\Kata.xlsm`) and `IsoQhPlusPath` (`C:\HPKata\Iso_QHPlus.xls`) via `IKataExcelAdapter`.

5. **XData and Layer Infrastructure**:
   - `HPKata.AutoCad/Interop/XDataService.cs` is already fully implemented with RegApp `"KATA"`, `ToResultBuffer`, `FromResultBuffer`, `WriteXData`, and `ReadXData`.
   - `HPKata.Core/Serialization/KataXDataRecord.cs` defines structured properties: `Dang`, `STT`, `Phi`, `KhoangCach`, `SoLuong`, `SoLuongCauKien`, `ChieuDaiCacDoanMm`, `GocUon`, `TenCauKien`, `SttThepChinh`, `ChuThich`, `Z`.
   - `HPKata.Core/Configuration/KataConfig.cs` specifies canonical layer names:
     - `LayerThepChu`: `"kata_thep chu"` (longitudinal bars / `THEP_DOC`)
     - `LayerThepDai`: `"kata_thep dai"` (stirrups & ties / `THEP_DAI`)
     - `LayerNetThay`: `"kata_net thay"` (visible outline)
     - `LayerDim`: `"kata_dim"` (dimension lines / `DIM`)
     - `LayerText`: `"kata_text"` (text annotations / `TEXT`)
     - Standard wall layers: `"kata_thep - V"`, `"kata_thep - H"`

### 1.3 Revit Detailing Commands Audit
Direct code inspection of `HPKata.Revit`:
1. **Framing Detailing Command** (`HPKata.Revit/Commands/FramingRebarCommands.cs`, lines 18-65):
   - Selects a beam with `FramingSelectionFilter` (`OST_StructuralFraming`).
   - Opens `BeamDetailingWindow` with `BeamDetailingViewModel`.
   - Upon dialog confirmation, delegates to `FramingRebarGenerator.GenerateFramingRebar(doc, beam, vm.ResultLayout)`.
   - `FramingRebarGenerator.cs` (lines 69-90) implements idempotent rebar generation by purging rebars matching comment prefix `HPKata_Beam_{BeamName}`, resolving `RebarBarType` by diameter, and creating longitudinal bars and stirrup hoops.
2. **Column Detailing Command** (`HPKata.Revit/Commands/ColumnRebarCommands.cs`, lines 18-61):
   - Selects a column with `ColumnSelectionFilter` (`OST_StructuralColumns`).
   - Opens `ColumnDetailingWindow` with `ColumnDetailingViewModel`.
   - Delegates to `ColumnRebarGenerator.GenerateColumnRebar(doc, col, vm.ResultLayout)`.
   - Generates 3D vertical bars and confinement ties with dense/sparse spacing.
3. **Wall Detailing Command** (`HPKata.Revit/Commands/WallRebarCommands.cs`, lines 18-61):
   - Selects a wall with `WallSelectionFilter` (`OST_Walls`).
   - Opens `WallDetailingWindow` with `WallDetailingViewModel`.
   - Delegates to `WallRebarGenerator.GenerateWallRebar(doc, wall, vm.ResultModel)`.
   - Generates 3D boundary vertical bars and double curtain web mesh.
4. **Excel Export Command** (`HPKata.Revit/Commands/ExportExcelCommand.cs` & `RevitExportExcelRunner.cs`):
   - Collects all Revit `Rebar` elements, maps them via `RevitParameterBridge.ToRebarItem`, and exports to Excel.

---

## 2. Logic Chain

### 2.1 AutoCAD Detailing Pipeline Architecture
To achieve 100% operational parity with KataPro and satisfy Features 7 & 8:
```
[User Command]
   │
   ├─► loadbeam / loadcolumn / loadwall
   │     │
   │     ▼
   │   KataInstallEnvironment.Current.KataWorkbookPath ("C:\HPKata\Kata.xlsm")
   │     │
   │     ▼
   │   IKataExcelAdapter.ReadBeamSheet / ReadColumnSheet / ReadWallSheet
   │     │
   │     ▼
   │   KataBeamDataMapper / KataColumnDataMapper / KataWallDataMapper
   │     │
   │     ▼
   │   Store in Memory Registry:
   │   - BeamTakeoffRegistry
   │   - ColumnTakeoffRegistry
   │   - WallTakeoffRegistry
   │
   ├─► ve_dam (vd) / ve_cot (vc) / ve_vach (vv)  [Standard Detailing]
   │     │
   │     ▼
   │   Resolve layout from Registry (or prompt user/default fallback)
   │     │
   │     ▼
   │   KataEntityStyler & KataLayerHelper:
   │   - Ensure layers: kata_thep chu, kata_thep dai, kata_net thay, kata_text, kata_dim
   │   - Draw Concrete Outlines on kata_net thay
   │   - Draw Longitudinal Rebar on kata_thep chu + Attach KATA XData (Dang, Phi, STT, L)
   │   - Draw Stirrups / Ties on kata_thep dai + Attach KATA XData
   │   - Draw Callouts & Dimensions on kata_text & kata_dim
   │
   ├─► ve_dam_shop (vds) / ve_cot_shop (vcs) / ve_vach_shop (vvs)  [Shop Detailing]
   │     │
   │     ▼
   │   Calculate 11.7m commercial rebar curtailment & lap splices
   │   - Staggered lap positions (tension vs compression zones)
   │   - Crank bends (1:6 slope) & starter dowels
   │   - U-bar edge closures & hairpins for walls
   │   - Each segment tagged with L=... (Ø...) callout + KATA XData
   │   - Cross sections (vdmc) with rebar positions & dims
   │
   └─► toexcel / toexcelqh  [Schedule Export]
         │
         ▼
       Collect drawing entities with KATA XData
         │
         ▼
       Extract RebarItem list via ScheduleCommandRunner
         │
         ▼
       IKataExcelAdapter.ExportSchedule(KataWorkbookPath / IsoQhPlusPath, items)
       - toexcel   -> C:\HPKata\Kata.xlsm (Sheet Thong_ke)
       - toexcelqh -> C:\HPKata\Iso_QHPlus.xls
```

### 2.2 Revit Detailing Pipeline Architecture
```
[User Command in Revit 2026]
   │
   ├─► FramingRebarCommands (OST_StructuralFraming)
   │     │
   │     ├─► Check Kata.xlsm (Dam sheet) for matching BeamMark via IKataExcelAdapter
   │     ├─► Show BeamDetailingWindow (with Read/Write/Open Excel commands)
   │     └─► FramingRebarGenerator.GenerateFramingRebar:
   │           - Purge old rebars by comment "HPKata_Beam_{BeamName}"
   │           - Top continuous bars with downward hooks
   │           - Bottom continuous bars with upward hooks
   │           - Extra support curtailment bars (L/3, L/4) & midspan bars
   │           - Closed stirrups in dense/sparse zones (RebarStyle.StirrupTie)
   │           - Skin bars if depth >= 700mm
   │           - Tag with Partition = BeamName, Comments = HPKata_Beam_{BeamName}
   │
   ├─► ColumnRebarCommands (OST_StructuralColumns)
   │     │
   │     ├─► Check Kata.xlsm (Cot sheet) for matching ColumnMark via IKataExcelAdapter
   │     ├─► Show ColumnDetailingWindow (with Read/Write/Open Excel commands)
   │     └─► ColumnRebarGenerator.GenerateColumnRebar:
   │           - Purge old rebars by comment "HPKata_Column_{ColumnMark}"
   │           - 4 corner longitudinal bars + B-face/H-face side bars
   │           - Confinement ties at column ends (dense) and mid-height (sparse)
   │           - Starter dowels into foundation
   │           - Tag with Partition = ColumnMark, Comments = HPKata_Column_{ColumnMark}
   │
   ├─► WallRebarCommands (OST_Walls)
   │     │
   │     ├─► Check Kata.xlsm (Vach sheet) for matching WallMark via IKataExcelAdapter
   │     ├─► Show WallDetailingWindow (with Read/Write/Open Excel commands)
   │     └─► WallRebarGenerator.GenerateWallRebar:
   │           - Purge old rebars by comment "HPKata_Wall_{WallMark}"
   │           - Left & Right boundary element cages (BE-L, BE-R)
   │           - Double curtain vertical mesh & horizontal bars
   │           - End U-bars wrapping boundary zones
   │           - Tag with Partition = WallMark, Comments = HPKata_Wall_{WallMark}
   │
   └─► ExportExcelCommand
         │
         ▼
       Collect HPKata Rebar instances -> RevitParameterBridge.ToRebarItem
         │
         ▼
       IKataExcelAdapter.ExportSchedule(KataWorkbookPath / IsoQhPlusPath, items)
```

### 2.3 Modular File Architecture & Clean Code Compliance
To ensure every `.cs` file stays strictly `<= 300` lines (Rule C2) and methods `<= 50` lines (Rule M3):

| Target File | Scope / Responsibilities | Estimated Lines | Rule C2 Check |
|---|---|---|---|
| `HPKata.AutoCad/Interop/KataLayerHelper.cs` | Helper ensuring standard CAD layers (`kata_thep chu`, `kata_thep dai`, `kata_net thay`, `kata_dim`, `kata_text`, etc.) | ~80 lines | Pass (< 300) |
| `HPKata.AutoCad/Interop/KataEntityStyler.cs` | Centralized helper to set layer, color, and attach `KATA` XData | ~95 lines | Pass (< 300) |
| `HPKata.AutoCad/Controllers/BeamExcelController.cs` | Handles `loadbeam` from `Kata.xlsm` (`Dam` sheet) via `IKataExcelAdapter` | ~85 lines | Pass (< 300) |
| `HPKata.AutoCad/Controllers/ColumnExcelController.cs` | Handles `loadcolumn` from `Kata.xlsm` (`Cot` sheet) via `IKataExcelAdapter` | ~85 lines | Pass (< 300) |
| `HPKata.AutoCad/Controllers/WallExcelController.cs` | Handles `loadwall` from `Kata.xlsm` (`Vach` sheet) via `IKataExcelAdapter` | ~85 lines | Pass (< 300) |
| `HPKata.AutoCad/Controllers/BeamDrawController.cs` | Standard beam elevation with registry check, layers, XData, stirrups | ~190 lines | Pass (< 300) |
| `HPKata.AutoCad/Controllers/BeamShopController.cs` | Shop beam detailing with 11.7m curtailment, staggered laps, layers, XData | ~160 lines | Pass (< 300) |
| `HPKata.AutoCad/Controllers/ColumnDrawController.cs` | Standard column drawing with registry check, layers, XData, confinement ties | ~185 lines | Pass (< 300) |
| `HPKata.AutoCad/Controllers/ColumnShopController.cs` | Shop column detailing with story-by-story splices, dowels, layers, XData | ~140 lines | Pass (< 300) |
| `HPKata.AutoCad/Controllers/WallDrawController.cs` | Standard wall drawing with boundary cages, web mesh, layers, XData | ~195 lines | Pass (< 300) |
| `HPKata.AutoCad/Controllers/WallShopController.cs` | Shop wall detailing with 11.7m vertical curtailment, U-bars, cross-ties | ~145 lines | Pass (< 300) |
| `HPKata.AutoCad/Commands/ExportCommandRunner.cs` | `toexcel` and `toexcelqh` dispatching via `IKataExcelAdapter` | ~90 lines | Pass (< 300) |
| `HPKata.Core/Excel/Mappers/KataBeamDataMapper.cs` | Bi-directional mapper between `KataBeamSheetData` and `BeamRebarLayout` | ~110 lines | Pass (< 300) |
| `HPKata.Core/Excel/Mappers/KataColumnDataMapper.cs` | Bi-directional mapper between `KataColumnSheetData` and `ColumnRebarLayout` | ~110 lines | Pass (< 300) |
| `HPKata.Core/Excel/Mappers/KataWallDataMapper.cs` | Bi-directional mapper between `KataWallSheetData` and `ShearWallModel` | ~95 lines | Pass (< 300) |
| `HPKata.Revit/Commands/FramingRebarCommands.cs` | Beam external command with Excel pre-load and generator dispatch | ~85 lines | Pass (< 300) |
| `HPKata.Revit/Commands/ColumnRebarCommands.cs` | Column external command with Excel pre-load and generator dispatch | ~85 lines | Pass (< 300) |
| `HPKata.Revit/Commands/WallRebarCommands.cs` | Wall external command with Excel pre-load and generator dispatch | ~85 lines | Pass (< 300) |
| `HPKata.Revit/Services/FramingRebarGenerator.cs` | 3D framing rebar generation with hooks, stirrups, curtailment, side bars | ~230 lines | Pass (< 300) |
| `HPKata.Revit/Services/ColumnRebarGenerator.cs` | 3D column rebar generation with corner/side bars, ties, dowels | ~175 lines | Pass (< 300) |
| `HPKata.Revit/Services/WallRebarGenerator.cs` | 3D wall rebar generation with boundary cages, mesh, U-bars | ~145 lines | Pass (< 300) |

---

## 3. Caveats

1. **AutoCAD Runtime Dependency**:
   - AutoCAD native APIs (`Autodesk.AutoCAD.DatabaseServices`, `Autodesk.AutoCAD.Geometry`) require `.NET 8.0-windows` and `AutoCAD.NET 25.1.0`. While in-memory `Database` operations and command reflection can be tested headless, full interactive pick-point commands (`Editor.GetPoint`) execute inside `acad.exe`. We decouple drawing algorithms into pure layout/geometry functions and test command runners via `ICadDocumentContext` / `MockCadDocumentContext`.
2. **Revit In-Process Transactions**:
   - `Autodesk.Revit.DB.Document` is sealed and cannot be instantiated outside `Revit.exe`. Thus, `FramingRebarGenerator`, `ColumnRebarGenerator`, and `WallRebarGenerator` rely on pure calculators (`FramingRebarGeometryCalculator`, `RevitPoint3D`) for 100% headless testing in `HPKata.Revit.Tests`, while the command execution and transaction wrapping are verified via structural mocks and reflection.
3. **Excel Dual-Mode Availability**:
   - Desktop COM automation requires Microsoft Excel to be installed and running. If desktop Excel is not active, `IKataExcelAdapter` transparently falls back to headless ClosedXML file I/O on `KataInstallEnvironment.Current.KataWorkbookPath` (`C:\HPKata\Kata.xlsm`). All headless unit tests run safely without desktop Excel.

---

## 4. Conclusion & Concrete Design Specifications

### 4.1 AutoCAD Layer & Entity Styler Design

#### `HPKata.AutoCad/Interop/KataLayerHelper.cs`
```csharp
using System;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace HPKata.AutoCad.Interop
{
    public static class KataLayerHelper
    {
        public const string ThepDoc = "kata_thep chu";
        public const string ThepDai = "kata_thep dai";
        public const string NetThay = "kata_net thay";
        public const string NetKhung = "kata_net khung";
        public const string NetKhuat = "kata_net khuat";
        public const string Dim = "kata_dim";
        public const string Text = "kata_text";
        public const string ThepWallV = "kata_thep - V";
        public const string ThepWallH = "kata_thep - H";

        public static void EnsureStandardLayers(Transaction tr, Database db)
        {
            var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
            EnsureLayer(tr, lt, ThepDoc, Color.FromColorIndex(ColorMethod.ByAci, 2));   // Yellow
            EnsureLayer(tr, lt, ThepDai, Color.FromColorIndex(ColorMethod.ByAci, 4));   // Cyan
            EnsureLayer(tr, lt, NetThay, Color.FromColorIndex(ColorMethod.ByAci, 7));   // White
            EnsureLayer(tr, lt, Dim, Color.FromColorIndex(ColorMethod.ByAci, 1));       // Red
            EnsureLayer(tr, lt, Text, Color.FromColorIndex(ColorMethod.ByAci, 3));      // Green
        }

        public static ObjectId EnsureLayer(Transaction tr, LayerTable lt, string layerName, Color color)
        {
            if (lt.Has(layerName)) return lt[layerName];

            lt.UpgradeOpen();
            using var ltr = new LayerTableRecord
            {
                Name = layerName,
                Color = color
            };
            ObjectId id = lt.Add(ltr);
            tr.AddNewlyCreatedDBObject(ltr, true);
            return id;
        }
    }
}
```

#### `HPKata.AutoCad/Interop/KataEntityStyler.cs`
```csharp
using System;
using Autodesk.AutoCAD.DatabaseServices;
using HPKata.Core.Serialization;

namespace HPKata.AutoCad.Interop
{
    public static class KataEntityStyler
    {
        public static void StyleAndTag(
            Entity ent,
            BlockTableRecord ms,
            Transaction tr,
            Database db,
            string layerName,
            KataXDataRecord? xdata = null)
        {
            if (ent == null) throw new ArgumentNullException(nameof(ent));

            ent.Layer = layerName;
            ms.AppendEntity(ent);
            tr.AddNewlyCreatedDBObject(ent, true);

            if (xdata != null)
            {
                XDataService.EnsureRegApp(db, KataXDataRecord.RegAppName);
                XDataService.WriteXData(ent, xdata);
            }
        }
    }
}
```

### 4.2 Excel Load Controllers for AutoCAD

#### `HPKata.AutoCad/Controllers/BeamExcelController.cs`
```csharp
using System;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Mappers;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    public static class BeamExcelController
    {
        public static void LoadBeamInteractive(IKataExcelAdapter? adapter = null)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            string path = KataInstallEnvironment.Current.KataWorkbookPath;
            if (!File.Exists(path))
            {
                ed.WriteMessage($"\n[HPKata] Không tìm thấy workbook tại {path}\n");
                return;
            }

            var excel = adapter ?? KataExcelAdapterProvider.Current;
            if (!excel.ReadBeamSheet(path, out var sheetData))
            {
                ed.WriteMessage($"\n[HPKata] loadbeam: Không thể đọc sheet 'Dam' từ {path}\n");
                return;
            }

            var layout = KataBeamDataMapper.ToRebarLayout(sheetData);
            BeamTakeoffRegistry.Register(layout);
            ed.WriteMessage($"\n[HPKata] loadbeam: Đã tải dầm '{layout.BeamName}' ({layout.Spans.Count} nhịp) từ Kata.xlsm vào bộ nhớ.\n");
        }
    }
}
```

#### `HPKata.AutoCad/Controllers/ColumnExcelController.cs`
```csharp
using System;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Mappers;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    public static class ColumnExcelController
    {
        public static void LoadColumnInteractive(IKataExcelAdapter? adapter = null)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            string path = KataInstallEnvironment.Current.KataWorkbookPath;
            if (!File.Exists(path))
            {
                ed.WriteMessage($"\n[HPKata] Không tìm thấy workbook tại {path}\n");
                return;
            }

            var excel = adapter ?? KataExcelAdapterProvider.Current;
            if (!excel.ReadColumnSheet(path, out var sheetData))
            {
                ed.WriteMessage($"\n[HPKata] loadcolumn: Không thể đọc sheet 'Cot' từ {path}\n");
                return;
            }

            var layout = KataColumnDataMapper.ToRebarLayout(sheetData);
            ColumnTakeoffRegistry.Register(layout);
            ed.WriteMessage($"\n[HPKata] loadcolumn: Đã tải cột '{layout.ColumnMark}' ({layout.Stories.Count} tầng) từ Kata.xlsm vào bộ nhớ.\n");
        }
    }
}
```

#### `HPKata.AutoCad/Controllers/WallExcelController.cs`
```csharp
using System;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using HPKata.Core.Configuration;
using HPKata.Core.Excel;
using HPKata.Core.Excel.Mappers;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    public static class WallExcelController
    {
        public static void LoadWallInteractive(IKataExcelAdapter? adapter = null)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            string path = KataInstallEnvironment.Current.KataWorkbookPath;
            if (!File.Exists(path))
            {
                ed.WriteMessage($"\n[HPKata] Không tìm thấy workbook tại {path}\n");
                return;
            }

            var excel = adapter ?? KataExcelAdapterProvider.Current;
            if (!excel.ReadWallSheet(path, out var sheetData))
            {
                ed.WriteMessage($"\n[HPKata] loadwall: Không thể đọc sheet 'Vach' từ {path}\n");
                return;
            }

            var model = KataWallDataMapper.ToShearWallModel(sheetData);
            WallTakeoffRegistry.Register(model);
            ed.WriteMessage($"\n[HPKata] loadwall: Đã tải vách '{model.WallMark}' ({model.Stories.Count} tầng) từ Kata.xlsm vào bộ nhớ.\n");
        }
    }
}
```

### 4.3 Bi-Directional Mappers in `HPKata.Core.Excel.Mappers`

#### `HPKata.Core/Excel/Mappers/KataBeamDataMapper.cs`
- Maps between `KataBeamSheetData` (extracted from Excel cells) and `BeamRebarLayout`.
- Parses bar strings (e.g. `"6f25"` -> `BarCount = 6`, `DiameterMm = 25`, `"4f22"` -> `BarCount = 4`, `DiameterMm = 22`).
- Maps spans, clear lengths, column widths, beam depth, concrete cover, and stirrups.

#### `HPKata.Core/Excel/Mappers/KataColumnDataMapper.cs`
- Maps between `KataColumnSheetData` and `ColumnRebarLayout`.
- Maps stories (height, dimensions `b` and `h`, rebar diameter, tie distribution).

#### `HPKata.Core/Excel/Mappers/KataWallDataMapper.cs`
- Maps between `KataWallSheetData` and `ShearWallModel`.
- Maps wall mark, thickness, length, boundary elements, and web mesh.

### 4.4 AutoCAD Excel Export Integration (`toexcel`, `toexcelqh`)
In `HPKata.AutoCad/Commands/ExportCommandRunner.cs`:
```csharp
public static string ExecuteExcelExport(
    ICadDocumentContext context,
    bool isQhPlus = false,
    string? overridePath = null)
{
    if (context == null) throw new ArgumentNullException(nameof(context));

    IReadOnlyList<CadEntityDto> entities = context.GetAllRebarEntities();
    List<RebarItem> items = ScheduleCommandRunner.ExtractRebarItems(entities);

    string targetPath = overridePath ?? (isQhPlus
        ? KataInstallEnvironment.Current.IsoQhPlusPath
        : KataInstallEnvironment.Current.KataWorkbookPath);

    var adapter = KataExcelAdapterProvider.Current;
    bool success = adapter.ExportSchedule(targetPath, items);

    if (success)
    {
        context.WriteMessage($"\n[HPKata] Xuất bảng thống kê thành công vào: {targetPath} ({items.Count} thanh)\n");
    }
    else
    {
        // Safe fallback to independent workbook
        string fallbackPath = ResolveDefaultExportPath(context, isQhPlus);
        RebarScheduleReport report = RebarScheduleAggregator.GenerateReport(items);
        ClosedXmlExporter.ExportScheduleToFile(report, fallbackPath);
        context.WriteMessage($"\n[HPKata] Đã xuất bảng thống kê dự phòng tại: {fallbackPath}\n");
        return fallbackPath;
    }

    return targetPath;
}
```

### 4.5 Revit Detailing Commands Integration
In `HPKata.Revit`:
- Commands wire `BeamDetailingViewModel`, `ColumnDetailingViewModel`, `WallDetailingViewModel` to `KataInstallEnvironment.Current.KataWorkbookPath` via `IKataExcelAdapter`.
- When element is picked, the command attempts to pre-populate parameters from `Kata.xlsm` if matching mark exists.
- On Apply, native Revit 3D `Rebar` elements are generated via `FramingRebarGenerator`, `ColumnRebarGenerator`, `WallRebarGenerator` with:
  - Exact diameter mapping (`Ø{dia} (HPKata)`)
  - Hook angles (90 deg / 135 deg)
  - Stirrup/tie confinement zones
  - Partition and Comments metadata for schedule extraction and idempotent re-runs.

---

## 5. Verification Method

### 5.1 Verification Commands
To independently verify the implementation:
1. **Compilation & Warning Verification**:
   ```powershell
   dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
   ```
   *Expected*: `0 Warning(s), 0 Error(s)`.

2. **Automated Test Suite**:
   ```powershell
   dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1 --no-build
   ```
   *Expected*: `Passed! - Failed: 0, Skipped: 0` (100% pass across all test projects).

3. **Clean Code Compliance Audit**:
   Verify line limits (`<= 300` lines per `.cs` file, methods `<= 50` lines):
   ```powershell
   Get-ChildItem -Path "HPKata" -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch "\\(bin|obj)\\" } | ForEach-Object {
       $lines = (Get-Content $_.FullName | Measure-Object -Line).Lines
       if ($lines -gt 300) { Write-Error "$($_.FullName): $lines lines exceeds 300!" }
   }
   ```

### 5.2 Specific Test Cases Designed for Features 7 & 8
The implementing workers must add the following tests to `HPKata.AutoCad.Tests` and `HPKata.Revit.Tests`:

#### `HPKata.AutoCad.Tests/Controllers/CadDetailingCommandTests.cs`:
1. `ContinuousBeamCommands_DeclaresRequiredAliases`:
   Verifies `ve_dam`, `vd`, `ve_dam_shop`, `vds`, `ve_dam_mc`, `vdmc`, `loadbeam`.
2. `ColumnCommands_DeclaresRequiredAliases`:
   Verifies `ve_cot`, `vc`, `ve_cot_shop`, `vcs`, `loadcolumn`.
3. `WallCommands_DeclaresRequiredAliases`:
   Verifies `ve_vach`, `vv`, `ve_vach_shop`, `vvs`, `loadwall`.
4. `ScheduleCommands_DeclaresRequiredAliases`:
   Verifies `toexcel`, `toexcelqh`.
5. `LoadBeam_FromKataXlsm_PopulatesBeamTakeoffRegistry`:
   Reads a sample `Kata.xlsm` workbook via `IKataExcelAdapter`, verifies that `BeamTakeoffRegistry.TryGet("B01", out var layout)` succeeds with correct spans, depths, and bar diameters.
6. `LoadColumn_FromKataXlsm_PopulatesColumnTakeoffRegistry`:
   Reads sample `Kata.xlsm` sheet `Cot`, verifies `ColumnTakeoffRegistry.TryGet("C1", out var layout)` succeeds with story dimensions and corner diameter.
7. `LoadWall_FromKataXlsm_PopulatesWallTakeoffRegistry`:
   Reads sample `Kata.xlsm` sheet `Vach`, verifies `WallTakeoffRegistry.TryGet("V1", out var wall)` succeeds with thickness and length.
8. `ExportCommandRunner_ToExcel_CallsAdapterWithKataWorkbookPath`:
   Uses `MockCadDocumentContext` and verifies that `toexcel` targets `KataInstallEnvironment.Current.KataWorkbookPath`.
9. `ExportCommandRunner_ToExcelQh_CallsAdapterWithIsoQhPlusPath`:
   Verifies that `toexcelqh` targets `KataInstallEnvironment.Current.IsoQhPlusPath`.
10. `KataEntityStyler_AppliesLayerAndXData`:
    Verifies that styled entities have proper layer names and valid `KATA` ResultBuffer XData.

#### `HPKata.Revit.Tests/Commands/RevitDetailingCommandTests.cs`:
1. `FramingSelectionFilter_AllowsOnlyStructuralFraming`:
   Verifies category filter allows only `OST_StructuralFraming`.
2. `ColumnSelectionFilter_AllowsOnlyStructuralColumns`:
   Verifies category filter allows only `OST_StructuralColumns`.
3. `WallSelectionFilter_AllowsOnlyWalls`:
   Verifies category filter allows only `OST_Walls`.
4. `FramingRebarGeometry_WithExcelParameters_CalculatesCoordinates`:
   Verifies top/bottom bar endpoints, hooks, and stirrup hoops using parameters read from `Kata.xlsm` sheet `Dam`.
5. `ColumnRebarGeometry_WithExcelParameters_CalculatesCoordinates`:
   Verifies 4 corner bars and confinement tie coordinates using parameters from `Kata.xlsm` sheet `Cot`.
6. `WallRebarGeometry_WithExcelParameters_CalculatesCoordinates`:
   Verifies boundary cage bars and web mesh coordinates using parameters from `Kata.xlsm` sheet `Vach`.
7. `RevitExportExcelRunner_CallsAdapterWithKataWorkbookPath`:
   Verifies that Revit schedule export routes through `IKataExcelAdapter.ExportSchedule`.

---
*Report completed and self-verified by explorer_hpkata_m2_3. Ready for parent handoff.*
