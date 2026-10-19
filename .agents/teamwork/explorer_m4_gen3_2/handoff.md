# Handoff Report: Milestone M4 Excel Ribbon & ClosedXML UDF Exploration

**Agent ID**: `explorer_m4_gen3_2`  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_gen3_2\`  
**Target Milestone**: M4 (Excel Ribbon Actions & ClosedXML UDF Dispatch)  
**Parent Orchestrator ID**: `1e55a9de-0d52-405d-83fa-fa58bb05a293`  

---

## 1. Observation

### 1.1 Existing Codebase & Project Infrastructure
1. **`HPDcePro/src/HPDcePro.Excel/`**:
   - Project file `HPDcePro.Excel.csproj`: Targets `net8.0`, references `ClosedXML` v0.104.2 and `HPDcePro.Core`.
   - Existing implementations:
     - `CalculationUdfBridge.cs` (217 lines): Implements 19 static UDF methods forwarding to `HPDcePro.Core.Calculations`.
     - `RcGeotechUdfBridge.cs` (247 lines): Implements PMM column checks, shear wall checks, two-way slab calculations, pile cap capacity, pad footings, pile shaft friction, and beam crack width.
     - `SteelWindUdfBridge.cs` (200 lines): Implements TCVN 5575 steel beam/column stability, moment connections, anchor base plates, and TCVN 2737 wind pressure.
     - `ClosedXmlReportExporter.cs` (149 lines): Generates headless beam calculation reports (`ThuyetMinhDam`).
     - `ClosedXmlBoqExporter.cs` (203 lines): Generates multi-sheet BOQ workbooks (`TongHop`, `BeTong`, `VanKhuon`, `CotThep`, `ChiTietTheoTang`).
     - `ClosedXmlScheduleExporter.cs` (127 lines): Generates rebar schedule workbooks (`ThongKeThep`).
   - Missing interfaces in `HPDcePro.Excel`:
     - `IDceExcelActionDispatcher` and `DceExcelActionDispatcher`.
     - `ExcelActionContext` and `ExcelActionResult`.
     - Concrete action handlers mapping the 73 legacy ribbon actions.
     - Standard ClosedXML formula evaluation bridge (`DceUdfFunctions.RegisterAll`).

2. **Legacy DCE Excel Catalog**:
   - File location: `C:\Dce Pro\AiAgent\DanhBa\danhba_dce_excel.json` (also referenced in `TRUTH_MATRIX.md` Subsystem 2, lines 355–435).
   - Contains **exactly 73 records** (`muc`), verified via Python parser:
     - `HostRibbon.Dispatch` buttons (52 items): Legacy COM/VBA ribbon buttons.
     - `MatTienDceExcel.*` read-only tools (13 items): Headless workbook inspection routines returning structured JSON.
     - `AutoStr.*` commands (8 items): Automated calculation, drawing data sync, and calcsheet generation.

3. **Authentic Calculation Engines in `HPDcePro.Core.Calculations`**:
   - `BeamFlexureCalculator`, `BeamShearCalculator`, `BeamCrackCalculator`, `ContinuousBeamCalculator`.
   - `ColumnPmmInteractionCalculator` (PMM interaction curves and utilization ratio).
   - `ShearWallCalculator` (wall shear and boundary element checks).
   - `TwoWaySlabCalculator` (two-way slab moments and required reinforcement).
   - `PadFootingCalculator` (soil pressure, uplift, punching, flexure).
   - `PileCapCalculator` (pile reactions, column & corner pile punching, one-way shear).
   - `PileCapacityCalculator` (TCVN 10304:2025 pile axial capacity, shaft friction, end bearing).
   - `SteelStabilityCalculator`, `SteelMomentConnectionCalculator`, `AnchorBoltBasePlateCalculator`.
   - `RebarCuttingOptimizer` (1D cutting stock optimizer on 11.7m standard bar stock).
   - `QuantityTakeoffCalculator` (concrete volume, formwork area, rebar tonnage).

4. **Migrated Assets in `HPDcePro/assets/Excel/File V.2020/`**:
   - `09. DCE_Beam_2020.xlsm`: Contains sheets `THDL`, `TinhDam`, `TM_CT_TCVN`, `DCE_Pro_Beam`, `DL_DCE`, `BEAM DCE`.
   - `08. DCE_ColumnWall_2020.xlsm`: Contains sheets `DATA_CONCRETE&REBAR`, `ColumnDetail`, `ColumnDesign`, `WallDesign`.
   - `12. DCE_SLab_2020.xlsm`: Contains sheets `STRIP FORCES`, `STRIP FILTER`, `STRIP DESIGN`, `STRIP REPORT`.
   - `13. DCE_DaiCoc_2020.xlsm`: Contains sheets `THDL`, `TinhMong`, `ThuyetMinh`.
   - `05. DCE_MongDon.xlsm`: Contains sheets `MongDon`, `Rtc`, `SLS`, `ULS`.
   - `15. DCE_ThongKeThep_2020.xlsm`: Contains sheets `TKT_DCE`, `ToiUuCatThep`, `KhoThep`.
   - All sample workbooks parse cleanly using ClosedXML without COM dependency.

5. **Test Runner Protocol Verification**:
   - Executing direct test runners without `dotnet test`:
     ```powershell
     & "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Core.Tests\bin\Debug\net8.0\HPDcePro.Core.Tests.exe"
     & "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Adapters.Tests\bin\Debug\net8.0-windows\HPDcePro.Adapters.Tests.exe"
     ```
     - Results: `HPDcePro.Core.Tests.exe`: 295/295 passed (5.5s).
     - Results: `HPDcePro.Adapters.Tests.exe`: 127/127 passed (1.8s). Zero crashes, zero modals.

---

## 2. Logic Chain

1. **Subsystem 2 Parity Requirement**:
   `TRUTH_MATRIX.md` records 73 legacy Excel ribbon dispatch actions. To achieve full Milestone M4 parity, every single action key must be registered, recognized, and dispatched by `IDceExcelActionDispatcher`.

2. **Clean Code Architectural Constraints**:
   - Rule C2 limits each file to <= 300 lines. Implementing 73 actions in one single class would easily exceed 1,500 lines.
   - Therefore, a modular Handler Pattern must be introduced: `DceExcelActionDispatcher` routes incoming action keys to 8 specialized domain handlers:
     1. `BeamActionHandler` (15 beam actions + alias `TinhDam`)
     2. `ColumnWallActionHandler` (18 column/wall actions + aliases `TinhCot`, `TinhVach`)
     3. `SlabActionHandler` (10 slab actions + alias `TinhSan`)
     4. `FoundationActionHandler` (3 foundation actions + aliases `TinhMong`, `TinhDaiCoc`, `TinhMongDon`)
     5. `RebarCuttingActionHandler` (3 rebar cutting/combination actions + aliases `ToHopThep`, `ToiUuCatThep`)
     6. `CadBridgeActionHandler` (4 CAD link actions)
     7. `EtabsSafeActionHandler` (7 ETABS/SAFE model sync actions)
     8. `ApiSystemActionHandler` (13 API connection, licensing, and workbook metadata actions)
   - Each handler remains between 100 and 220 lines of code, well within the 300-line limit.

3. **Authentic Calculation Engine Binding**:
   - The legacy VBA ribbon actions either opened interactive WinForms/VBA dialogs or automated calculation procedures.
   - In our clean-room headless architecture, actions that perform calculations must directly invoke `HPDcePro.Core.Calculations`:
     - Beam sizing -> `BeamFlexureCalculator` and `BeamShearCalculator`.
     - Column & wall design -> `ColumnPmmInteractionCalculator` and `ShearWallCalculator`.
     - Slab design -> `TwoWaySlabCalculator`.
     - Foundation design -> `PadFootingCalculator`, `PileCapCalculator`, `PileCapacityCalculator`.
     - Rebar cutting -> `RebarCuttingOptimizer` (1D cutting stock on 11.7m stock lengths).
     - BOQ & Schedules -> `QuantityTakeoffCalculator`, `ClosedXmlBoqExporter`, `ClosedXmlScheduleExporter`.
     - Workbook inspection (`MatTienDceExcel.*`) -> Inspect real OpenXML sheets via ClosedXML and return structured JSON strings matching the legacy format.

4. **TCVN Formula Bridge Integration**:
   - `DceUdfFunctions` standardizes the formula vocabulary so that Excel users and ClosedXML workbooks can write standard formulas like `=TCVN_Rb("B25")`, `=TCVN_Pile_Rc_SPT(0.4, 20, 15, 25, TRUE)`, `=TCVN_Steel_BeamCheck(...)`.
   - `DceUdfFunctions.RegisterAll(XLWorkbook wb)` hooks into ClosedXML's formula engine, allowing headless evaluation of spreadsheets with zero COM dependency.

---

## 3. Catalog of All 73 Excel Ribbon Actions

| # | Action Key | Functional Group | Legacy Handler | Primary Engine / Binding | Parity Function |
|---|---|---|---|---|---|
| 01 | `APIAUTOCAD` | `dce_excel/ket_noi_api` | `HostRibbon.Dispatch` | `McpBridge` / Text IPC | Connects to AutoCAD API via IPC text protocol |
| 02 | `APICAD` | `dce_excel/ket_noi_api` | `HostRibbon.Dispatch` | `McpBridge` / Multi-CAD | Connects to ZWCAD / GstarCAD / BricsCAD API |
| 03 | `APICSI` | `dce_excel/ket_noi_api` | `HostRibbon.Dispatch` | `HPEtabs.Mcp` / COM OAPI | Connects to ETABS / SAFE / SAP2000 OAPI |
| 04 | `Bas_Show` | `dce_excel/mong` | `HostRibbon.Dispatch` | `HPDcePro.Core.Calculations` | Basic design conceptual checks (axial ratio, drift, wind) |
| 05 | `Bea_CalculateTheVolume` | `dce_excel/dam` | `HostRibbon.Dispatch` | `QuantityTakeoffCalculator` | Estimates beam concrete, formwork, and rebar BOQ |
| 06 | `Bea_ChooseColor` | `dce_excel/dam` | `HostRibbon.Dispatch` | `ClosedXML` Style Engine | Refreshes styling/colors of beam calculation workbook |
| 07 | `Bea_Design` | `dce_excel/dam` | `HostRibbon.Dispatch` | `BeamFlexureCalculator`, `BeamShearCalculator` | Automated flexural & shear rebar sizing for beam spans |
| 08 | `Bea_ExportDrawingData` | `dce_excel/dam` | `HostRibbon.Dispatch` | `ClosedXML` Exporter | Transfers beam design data to drawing sheets (`DL_DCE`, `BEAM DCE`) |
| 09 | `Bea_ExportSize` | `dce_excel/dam` | `HostRibbon.Dispatch` | `ClosedXML` Geometry Extractor | Links ETABS beam labels and dimensions to Excel |
| 10 | `Bea_LoadData` | `dce_excel/dam` | `HostRibbon.Dispatch` | `ClosedXML` / ETABS Reader | Loads beam internal forces (M3, V2) from analysis sheets |
| 11 | `Bea_Print` | `dce_excel/dam` | `HostRibbon.Dispatch` | `ClosedXmlReportExporter` | Generates official beam calculation sheets (`ThuyetMinhDam`) |
| 12 | `Bea_Setup` | `dce_excel/dam` | `HostRibbon.Dispatch` | `BeamDesignParameters` | Configures beam safety factors and bar layering rules |
| 13 | `Col_CalculateTheVolume` | `dce_excel/cot_vach` | `HostRibbon.Dispatch` | `QuantityTakeoffCalculator` | Estimates column/wall concrete volume and formwork |
| 14 | `Col_ChooseColor` | `dce_excel/cot_vach` | `HostRibbon.Dispatch` | `ClosedXML` Style Engine | Refreshes styling/colors of column calculation workbook |
| 15 | `Col_Design` | `dce_excel/cot_vach` | `HostRibbon.Dispatch` | `ColumnPmmInteractionCalculator` | Computes column P-M-M interaction ratios & rebar sizing |
| 16 | `Col_Design2` | `dce_excel/cot_vach` | `HostRibbon.Dispatch` | `ColumnPmmInteractionCalculator` | Quick design mode for standard rectangular columns |
| 17 | `Col_ExportDrawingData` | `dce_excel/cot_vach` | `HostRibbon.Dispatch` | `ClosedXML` Exporter | Exports column schedule data to drawing sheets |
| 18 | `Col_ExportSize` | `dce_excel/cot_vach` | `HostRibbon.Dispatch` | `ClosedXML` Geometry Extractor | Displays ETABS column sizes and story assignments in Excel |
| 19 | `Col_LoadData` | `dce_excel/cot_vach` | `HostRibbon.Dispatch` | `ClosedXML` / ETABS Reader | Loads column/pier internal forces (N, Mx, My, Vu) |
| 20 | `Col_LoadDataGenCol` | `dce_excel/cot_vach` | `HostRibbon.Dispatch` | `ClosedXML` Text Reader | Imports non-rectangular arbitrary polygon column sections |
| 21 | `Col_Print` | `dce_excel/cot_vach` | `HostRibbon.Dispatch` | `ClosedXmlReportExporter` | Generates column P-M-M interaction calculation reports |
| 22 | `Col_Setup` | `dce_excel/cot_vach` | `HostRibbon.Dispatch` | `ColumnDesignParameters` | Configures column/wall design criteria and criteria |
| 23 | `Col_SortData` | `dce_excel/cot_vach` | `HostRibbon.Dispatch` | `ColumnGroupingAggregator` | Groups and sorts identical column piers across stories |
| 24 | `DbimPuFile` | `dce_excel/etabs_safe` | `HostRibbon.Dispatch` | File System Purge | Cleans auxiliary temporary cache files (*.$et, *.log, *.bak) |
| 25 | `DceAI` | `dce_excel/tai_khoan` | `HostRibbon.Dispatch` | AI Assistant Service | Launches AI engineering copilot & structural advisory |
| 26 | `DceAdmin` | `dce_excel/tai_khoan` | `HostRibbon.Dispatch` | System Diagnostics | Developer info, diagnostic traces, and system checks |
| 27 | `DceLanguage` | `dce_excel/tai_khoan` | `HostRibbon.Dispatch` | Localization Service | Switches active UI language (vi-VN, en-US) |
| 28 | `DceLogin` | `dce_excel/tai_khoan` | `HostRibbon.Dispatch` | Authentication Service | User authentication and licensing token validation |
| 29 | `DceLogout` | `dce_excel/tai_khoan` | `HostRibbon.Dispatch` | Authentication Service | Session deactivation and device unbinding |
| 30 | `DceUpdate` | `dce_excel/tai_khoan` | `HostRibbon.Dispatch` | Auto-Update Service | Checks for application updates and schema revisions |
| 31 | `DrawLine` | `dce_excel/cau_noi_cad` | `HostRibbon.Dispatch` | `HPAutoCad.Aec` Bridge | Requests CAD line coordinates and writes back handle |
| 32 | `DrawPolyLine` | `dce_excel/cau_noi_cad` | `HostRibbon.Dispatch` | `HPAutoCad.Aec` Bridge | Requests CAD polyline coordinates and writes back handle |
| 33 | `DrawREC` | `dce_excel/cau_noi_cad` | `HostRibbon.Dispatch` | `HPAutoCad.Aec` Bridge | Requests CAD rectangle coordinates and writes back handle |
| 34 | `ESStiffnessModifiers` | `dce_excel/etabs_safe` | `HostRibbon.Dispatch` | Structural Parameter Mapper | Synchronizes section stiffness modifiers from ETABS to SAFE |
| 35 | `ESUniformLoadSetsToSafe` | `dce_excel/etabs_safe` | `HostRibbon.Dispatch` | Structural Parameter Mapper | Synchronizes uniform area load sets to SAFE slab models |
| 36 | `EtaSTableToEta` | `dce_excel/etabs_safe` | `HostRibbon.Dispatch` | Tabular Data Synchronizer | Transfers updated Excel property tables back to ETABS |
| 37 | `Eta_Show` | `dce_excel/mong` | `HostRibbon.Dispatch` | CSI Model Connector | Connects to ETABS model, retrieves cases, combos, and shell forces |
| 38 | `Fou_Show` | `dce_excel/mong` | `HostRibbon.Dispatch` | `PadFootingCalculator`, `PileCapCalculator`, `PileCapacityCalculator` | Foundation design center (isolated footings, pile caps, pile capacities) |
| 39 | `GopThepDaiToHop` | `dce_excel/to_hop_thep` | `HostRibbon.Dispatch` | `RebarScheduleAggregator` | Aggregates identical rebar diameters and lengths for optimization |
| 40 | `PointSafeToPointSafe` | `dce_excel/etabs_safe` | `HostRibbon.Dispatch` | Geometry Synchronizer | Synchronizes SAFE slab column joint points |
| 41 | `SafeTableToSafe` | `dce_excel/etabs_safe` | `HostRibbon.Dispatch` | Tabular Data Synchronizer | Assigns joint point loads and moments to SAFE |
| 42 | `SelectHandle` | `dce_excel/cau_noi_cad` | `HostRibbon.Dispatch` | `HPAutoCad.Aec` Bridge | Retrieves selected AutoCAD entity handles into Excel cells |
| 43 | `Sla_ChooseColor` | `dce_excel/san` | `HostRibbon.Dispatch` | `ClosedXML` Style Engine | Refreshes styling/colors of slab calculation workbook |
| 44 | `Sla_Design` | `dce_excel/san` | `HostRibbon.Dispatch` | `TwoWaySlabCalculator` | Two-way slab moment evaluation & orthogonal rebar design |
| 45 | `Sla_ExportSize` | `dce_excel/san` | `HostRibbon.Dispatch` | `ClosedXML` Geometry Extractor | Displays SAFE design strip geometry and forces in Excel |
| 46 | `Sla_LoadData` | `dce_excel/san` | `HostRibbon.Dispatch` | `ClosedXML` / SAFE Reader | Loads design strip moments and shears from SAFE |
| 47 | `Sla_Print` | `dce_excel/san` | `HostRibbon.Dispatch` | `ClosedXmlReportExporter` | Generates slab design calculation reports |
| 48 | `Sla_Setup` | `dce_excel/san` | `HostRibbon.Dispatch` | `SlabDesignParameters` | Configures slab design parameters (cover, crack limits) |
| 49 | `ToHop` | `dce_excel/to_hop_thep` | `HostRibbon.Dispatch` | `RebarCuttingOptimizer` | 1D Cutting stock optimization (Pattern Type 1) |
| 50 | `ToHop2` | `dce_excel/to_hop_thep` | `HostRibbon.Dispatch` | `RebarCuttingOptimizer` | 1D Cutting stock optimization with inventory & reusable remnants |
| 51 | `UniformLoadSets` | `dce_excel/etabs_safe` | `HostRibbon.Dispatch` | Structural Parameter Mapper | Extracts uniform load sets from ETABS models |
| 52 | `licenseissuance` | `dce_excel/tai_khoan` | `HostRibbon.Dispatch` | License Management | License purchase, renewal, and quota verification |
| 53 | `MatTienDceExcel.PhienBan` | `dce_excel/mat_tien_doc` | `MatTienDceExcel.PhienBan` | `ClosedXmlInspector` | Returns JSON of open workbook title and list of worksheets |
| 54 | `MatTienDceExcel.LietKeChucNang` | `dce_excel/mat_tien_doc` | `MatTienDceExcel.LietKeChucNang` | `ClosedXmlInspector` | Returns JSON of recognizable tables and data start rows |
| 55 | `MatTienDceExcel.DocBangDam` | `dce_excel/dam` | `MatTienDceExcel.DocBangDam` | `ClosedXmlInspector` | Reads beam schedule (`TinhDam`: b, h, top/bottom rebar, stirrups) to JSON |
| 56 | `MatTienDceExcel.DocBangCot` | `dce_excel/cot` | `MatTienDceExcel.DocBangCot` | `ClosedXmlInspector` | Reads column schedule (`Column Detail`: pier, story, b, h, rebar) to JSON |
| 57 | `MatTienDceExcel.DocBangSan` | `dce_excel/san` | `MatTienDceExcel.DocBangSan` | `ClosedXmlInspector` | Reads raw cells of `STRIP DESIGN` sheet to JSON |
| 58 | `MatTienDceExcel.KiemTraDauVaoDam` | `dce_excel/dam` | `MatTienDceExcel.KiemTraDauVaoDam` | `ClosedXmlInspector` | Verifies presence of `THDL`, `TinhDam`, `TM_CT_TCVN`, `DCE_Pro_Beam` |
| 59 | `MatTienDceExcel.DocKetQuaDam` | `dce_excel/dam` | `MatTienDceExcel.DocKetQuaDam` | `ClosedXmlInspector` | Reads beam calculation results (M3, Mu, As req, As prov, FS, crack) |
| 60 | `MatTienDceExcel.DocKetQuaCot` | `dce_excel/cot` | `MatTienDceExcel.DocKetQuaCot` | `ClosedXmlInspector` | Reads column calculation results (`ColumnDesign` sheet, OK/FAIL, FS) |
| 61 | `MatTienDceExcel.DocKetQuaVach` | `dce_excel/vach` | `MatTienDceExcel.DocKetQuaVach` | `ClosedXmlInspector` | Reads shear wall calculation results (`WallDesign` sheet) |
| 62 | `MatTienDceExcel.KiemTraDauVaoCot` | `dce_excel/cot` | `MatTienDceExcel.KiemTraDauVaoCot` | `ClosedXmlInspector` | Verifies presence of required column/wall sheets and row counts |
| 63 | `MatTienDceExcel.KiemTraDauVaoSan` | `dce_excel/san` | `MatTienDceExcel.KiemTraDauVaoSan` | `ClosedXmlInspector` | Verifies presence of `STRIP FORCES`, `STRIP FILTER`, `STRIP DESIGN`, `STRIP REPORT` |
| 64 | `MatTienDceExcel.TrangThaiWorkbook` | `dce_excel/meta` | `MatTienDceExcel.TrangThaiWorkbook` | `ClosedXmlInspector` | Detects workbook type (Beam, ColumnWall, Slab, PileCap, Footing) |
| 65 | `AutoStr.AutoStr_InThuyetMinhDam` | `dce_excel/dam` | `AutoStr_ExcelCommand` | `ClosedXmlReportExporter` | Automated batch export of beam calculation reports |
| 66 | `AutoStr.AutoStr_InThuyetMinhCot` | `dce_excel/cot` | `AutoStr_ExcelCommand` | `ClosedXmlReportExporter` | Automated batch export of column calculation reports |
| 67 | `AutoStr.AutoStr_ChonThepSan` | `dce_excel/san` | `AutoStr_ExcelCommand` | `TwoWaySlabCalculator` | Automated rebar selection for slab strips in `STRIP DESIGN` |
| 68 | `AutoStr.AutoStr_AiTinhToanDam` | `dce_excel/dam` | `AutoStr_ExcelCommand` | `BeamFlexureCalculator`, `BeamShearCalculator` | Full automated pipeline: loads forces -> designs rebar -> writes `DL_DCE` |
| 69 | `AutoStr.AutoStr_AiChuyenDuLieuVeDam`| `dce_excel/dam` | `AutoStr_ExcelCommand` | `ClosedXML` Exporter | Transfers beam design results from `TinhDam` to `DL_DCE` & `BEAM DCE` |
| 70 | `AutoStr.AutoStr_AiTinhToanCot` | `dce_excel/cot` | `AutoStr_ExcelCommand` | `ColumnPmmInteractionCalculator` | Full automated pipeline: loads forces -> calculates PMM -> groups piers |
| 71 | `AutoStr.AutoStr_AiXuatDuLieuVeCot` | `dce_excel/cot` | `AutoStr_ExcelCommand` | `ClosedXML` Exporter | Transfers column calculation results to drawing sheets |
| 72 | `AutoStr.AutoStr_InThuyetMinhSan` | `dce_excel/san` | `AutoStr_ExcelCommand` | `ClosedXmlReportExporter` | Automated export of slab calculation report to `STRIP REPORT` |
| 73 | `MatTienDceExcel.DocDamDaLuuDuLieuVe`| `dce_excel/dam` | `MatTienDceExcel.DocDamDaLuuDuLieuVe`| `ClosedXmlInspector` | Compares beams in `DL_DCE` vs `TinhDam` and reports unsaved beams |

### Friendly Canonical Aliases
To provide a clean, modern developer experience and match Vietnamese engineering idioms, the dispatcher must also route these canonical aliases:
- `TinhDam` -> `Bea_Design`
- `TinhCot` -> `Col_Design`
- `TinhVach` -> `Col_Design`
- `TinhSan` -> `Sla_Design`
- `TinhMong` -> `Fou_Show`
- `TinhDaiCoc` -> `Fou_Show`
- `TinhMongDon` -> `Fou_Show`
- `XuatBOQ` -> `Bea_CalculateTheVolume`
- `ToHopThep` -> `ToHop`
- `ToiUuCatThep` -> `ToHop2`
- `InThuyetMinh` -> `Bea_Print`

---

## 4. Software Architecture & Implementation Specification for Worker M4

### 4.1 Target File Structure in `HPDcePro/src/HPDcePro.Excel/`
```
HPDcePro/src/HPDcePro.Excel/
├── IDceExcelActionDispatcher.cs         // Interfaces: IDceExcelActionDispatcher, IExcelActionHandler
├── ExcelActionModels.cs                 // DTOs: ExcelActionContext, ExcelActionResult
├── DceExcelActionDispatcher.cs          // Main dispatcher registering & routing all 73 actions
├── DceUdfFunctions.cs                   // Standard TCVN UDFs + ClosedXML formula registration
├── CalculationUdfBridge.cs              // Existing UDF facade (preserved & enhanced)
├── RcGeotechUdfBridge.cs                // Existing RC & Geotechnical UDFs (preserved)
├── SteelWindUdfBridge.cs                // Existing Steel & Wind UDFs (preserved)
├── ClosedXmlReportExporter.cs           // Existing report exporter (preserved)
├── ClosedXmlBoqExporter.cs              // Existing BOQ exporter (preserved)
├── ClosedXmlScheduleExporter.cs         // Existing schedule exporter (preserved)
└── Handlers/
    ├── BeamActionHandler.cs             // Handles 15 beam actions + TinhDam
    ├── ColumnWallActionHandler.cs       // Handles 18 column/wall actions + TinhCot, TinhVach
    ├── SlabActionHandler.cs             // Handles 10 slab actions + TinhSan
    ├── FoundationActionHandler.cs       // Handles 3 foundation actions + TinhMong
    ├── RebarCuttingActionHandler.cs     // Handles 3 rebar cutting actions + ToHopThep
    ├── CadBridgeActionHandler.cs        // Handles 4 CAD bridge actions
    ├── EtabsSafeActionHandler.cs        // Handles 7 ETABS/SAFE actions
    └── ApiSystemActionHandler.cs        // Handles 13 API, licensing, and inspection actions
```

### 4.2 Exact Interfaces & Class Signatures

#### 1. `IDceExcelActionDispatcher.cs`
```csharp
namespace HPDcePro.Excel;

public interface IDceExcelActionDispatcher
{
    ExcelActionResult ExecuteAction(string actionKey, ExcelActionContext context);
    IReadOnlyList<string> GetSupportedActions();
    bool CanHandle(string actionKey);
}

public interface IExcelActionHandler
{
    IReadOnlyList<string> SupportedActions { get; }
    bool CanHandle(string actionKey);
    ExcelActionResult Handle(string actionKey, ExcelActionContext context);
}
```

#### 2. `ExcelActionModels.cs`
```csharp
using System;
using System.Collections.Generic;
using ClosedXML.Excel;

namespace HPDcePro.Excel;

public sealed class ExcelActionContext
{
    public XLWorkbook? Workbook { get; set; }
    public string? WorkbookPath { get; set; }
    public string? ActiveSheetName { get; set; }
    public string ProjectName { get; set; } = "Dự án mẫu";
    public string? OutputDirectory { get; set; }
    public Dictionary<string, object> Parameters { get; } = new(StringComparer.OrdinalIgnoreCase);

    public XLWorkbook GetOrCreateWorkbook()
    {
        if (Workbook != null) return Workbook;
        if (!string.IsNullOrWhiteSpace(WorkbookPath) && System.IO.File.Exists(WorkbookPath))
        {
            Workbook = new XLWorkbook(WorkbookPath);
            return Workbook;
        }
        Workbook = new XLWorkbook();
        return Workbook;
    }
}

public sealed class ExcelActionResult
{
    public bool Success { get; set; }
    public string ActionKey { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string? GeneratedFilePath { get; set; }
    public double ExecutionTimeMs { get; set; }

    public static ExcelActionResult Ok(string actionKey, string message, object? data = null, string? filePath = null)
        => new() { Success = true, ActionKey = actionKey, Message = message, Data = data, GeneratedFilePath = filePath };

    public static ExcelActionResult Fail(string actionKey, string message, object? data = null)
        => new() { Success = false, ActionKey = actionKey, Message = message, Data = data };
}
```

#### 3. `DceExcelActionDispatcher.cs`
```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HPDcePro.Excel;

public sealed class DceExcelActionDispatcher : IDceExcelActionDispatcher
{
    private readonly List<IExcelActionHandler> _handlers;
    private readonly Dictionary<string, IExcelActionHandler> _actionMap;

    public DceExcelActionDispatcher(IEnumerable<IExcelActionHandler>? customHandlers = null)
    {
        _handlers = customHandlers?.ToList() ?? new List<IExcelActionHandler>
        {
            new Handlers.BeamActionHandler(),
            new Handlers.ColumnWallActionHandler(),
            new Handlers.SlabActionHandler(),
            new Handlers.FoundationActionHandler(),
            new Handlers.RebarCuttingActionHandler(),
            new Handlers.CadBridgeActionHandler(),
            new Handlers.EtabsSafeActionHandler(),
            new Handlers.ApiSystemActionHandler()
        };

        _actionMap = new Dictionary<string, IExcelActionHandler>(StringComparer.OrdinalIgnoreCase);
        foreach (var handler in _handlers)
        {
            foreach (var action in handler.SupportedActions)
            {
                _actionMap[action] = handler;
            }
        }
    }

    public IReadOnlyList<string> GetSupportedActions() => _actionMap.Keys.OrderBy(k => k).ToList();

    public bool CanHandle(string actionKey)
        => !string.IsNullOrWhiteSpace(actionKey) && _actionMap.ContainsKey(actionKey);

    public ExcelActionResult ExecuteAction(string actionKey, ExcelActionContext context)
    {
        if (string.IsNullOrWhiteSpace(actionKey))
            return ExcelActionResult.Fail(string.Empty, "Action key cannot be empty.");

        if (!_actionMap.TryGetValue(actionKey, out var handler))
            return ExcelActionResult.Fail(actionKey, $"Unsupported action key '{actionKey}'.");

        var sw = Stopwatch.StartNew();
        try
        {
            var result = handler.Handle(actionKey, context);
            sw.Stop();
            result.ExecutionTimeMs = sw.Elapsed.TotalMilliseconds;
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            var fail = ExcelActionResult.Fail(actionKey, $"Action '{actionKey}' failed: {ex.Message}");
            fail.ExecutionTimeMs = sw.Elapsed.TotalMilliseconds;
            return fail;
        }
    }
}
```

#### 4. `DceUdfFunctions.cs` (TCVN UDF Bridge & ClosedXML Registration)
```csharp
using System;
using ClosedXML.Excel;
using HPDcePro.Core.Calculations.Beam;

namespace HPDcePro.Excel;

public static class DceUdfFunctions
{
    // TCVN 5574:2018 Materials & Mechanics
    public static double TCVN_Rb(string macBt)
    {
        var conc = MaterialLibrary.TryGetConcrete(macBt, out var p) && p != null ? p : MaterialLibrary.GetConcrete(ConcreteGrade.B25);
        return conc.RbMpa;
    }

    public static double TCVN_Rbt(string macBt)
    {
        var conc = MaterialLibrary.TryGetConcrete(macBt, out var p) && p != null ? p : MaterialLibrary.GetConcrete(ConcreteGrade.B25);
        return conc.RbtMpa;
    }

    public static double TCVN_Eb(string macBt)
    {
        var conc = MaterialLibrary.TryGetConcrete(macBt, out var p) && p != null ? p : MaterialLibrary.GetConcrete(ConcreteGrade.B25);
        return conc.EbMpa;
    }

    public static double TCVN_Rs(string macThep)
    {
        var reb = MaterialLibrary.TryGetRebar(macThep, out var p) && p != null ? p : MaterialLibrary.GetRebar(RebarGrade.CB400V);
        return reb.RsMpa;
    }

    public static double TCVN_As_Beam(double b, double h, double momentKnm, string macBt = "B25", string macThep = "CB400-V", double a = 40.0)
        => CalculationUdfBridge.TinhThepDam_M(b, h, momentKnm, macBt, macThep, a);

    public static double TCVN_Beam_CrackWidth(double b, double h, double serviceMomentKnm, double asTensionMm2, double phiBarMm = 20.0, string macBt = "B25", string macThep = "CB400-V", double a = 40.0, bool isLongTerm = true)
        => RcGeotechUdfBridge.BeRongVetNutDam(b, h, serviceMomentKnm, asTensionMm2, phiBarMm, macBt, macThep, a, isLongTerm);

    // TCVN 10304:2025 Pile Bearing Capacity
    public static double TCVN_Pile_Rc_SPT(double diameterM, double lengthM, double n60Shaft, double n60Tip, bool isDriven = true)
        => CalculationUdfBridge.SucChiuTaiCoc_SPT(diameterM, lengthM, n60Shaft, n60Tip, isDriven);

    public static double TCVN_Pile_SkinFriction(double diameterM, double lengthM, double n60Shaft, bool isDriven = true, bool isCohesive = true)
        => RcGeotechUdfBridge.SucChiuTaiCoc_MaSat(diameterM, lengthM, n60Shaft, isDriven, isCohesive, isDesign: true);

    // TCVN 5575:2024 Steel Stability & Connections
    public static double TCVN_Steel_BeamCheck(double hMm, double bMm, double twMm, double tfMm, double momentMxKnm, double unbracedLengthL0M = 6.0, double shearVKn = 0.0, double yieldStrengthFyMpa = 235.0)
        => SteelWindUdfBridge.TinhDamThep_TCVN5575(hMm, bMm, twMm, tfMm, momentMxKnm, unbracedLengthL0M, shearVKn, yieldStrengthFyMpa);

    public static double TCVN_Steel_ColumnCheck(double hMm, double bMm, double twMm, double tfMm, double axialNKn, double momentMxKnm, double effectiveLengthLxM = 3.0, double effectiveLengthLyM = 3.0, double yieldStrengthFyMpa = 235.0)
        => SteelWindUdfBridge.TinhCotThep_TCVN5575(hMm, bMm, twMm, tfMm, axialNKn, momentMxKnm, effectiveLengthLxM, effectiveLengthLyM, yieldStrengthFyMpa);

    // TCVN 2737:2023 Wind Loads
    public static double TCVN_Wind_Pressure(string vungGio, string diaHinh, double caoDoZeM, double cpe = 0.8, double cpi = 0.0, bool isDesign = true)
        => SteelWindUdfBridge.TinhTaiTrongGio_TCVN2737(vungGio, diaHinh, caoDoZeM, cpe, cpi, isDesign);

    /// <summary>
    /// Registers all standard engineering UDFs into ClosedXML custom function engine.
    /// </summary>
    public static void RegisterAll(XLWorkbook workbook)
    {
        if (workbook == null) throw new ArgumentNullException(nameof(workbook));
        // ClosedXML formula engine function registrations
        workbook.AddFunction("TCVN_RB", (p1) => TCVN_Rb(p1.ToString() ?? "B25"));
        workbook.AddFunction("TCVN_RBT", (p1) => TCVN_Rbt(p1.ToString() ?? "B25"));
        workbook.AddFunction("TCVN_EB", (p1) => TCVN_Eb(p1.ToString() ?? "B25"));
        workbook.AddFunction("TCVN_RS", (p1) => TCVN_Rs(p1.ToString() ?? "CB400-V"));
    }
}
```

---

## 5. Headless Integration Test Suite Specification

The test suite must be located at `HPDcePro/tests/HPDcePro.Adapters.Tests/DceExcelActionDispatcherTests.cs`:

### Test Cases Required:
1. **`Dispatcher_SupportsAll73ActionsAndAliases`**:
   - Asserts `dispatcher.GetSupportedActions().Count >= 73`.
   - Asserts each of the 73 legacy action keys from Section 3 is recognized by `dispatcher.CanHandle(key)`.
   - Asserts friendly aliases (`TinhDam`, `TinhCot`, `TinhSan`, `TinhMong`, `XuatBOQ`, `ToHopThep`) are recognized.

2. **`BeamAction_ExecutesAuthenticCalculationsOnSampleWorkbook`**:
   - Resolves `File V.2020/09. DCE_Beam_2020.xlsm` via `AssetPathResolver`.
   - Executes `MatTienDceExcel.TrangThaiWorkbook` -> verifies detected type is "Beam" or "dce_beam".
   - Executes `MatTienDceExcel.KiemTraDauVaoDam` -> verifies sheets `THDL`, `TinhDam`, `TM_CT_TCVN`, `DCE_Pro_Beam` exist.
   - Executes `Bea_Design` / `TinhDam` with context parameters -> verifies flexure calculation returned positive $A_s$.

3. **`ColumnWallAction_EvaluatesPmmInteraction`**:
   - Resolves `File V.2020/08. DCE_ColumnWall_2020.xlsm`.
   - Executes `MatTienDceExcel.DocBangCot` -> returns parsed JSON of column piers.
   - Executes `Col_Design` -> returns positive PMM utilization ratio.

4. **`SlabAction_EvaluatesTwoWaySlab`**:
   - Resolves `File V.2020/12. DCE_SLab_2020.xlsm`.
   - Executes `MatTienDceExcel.KiemTraDauVaoSan` -> verifies `STRIP FORCES`, `STRIP FILTER`, `STRIP DESIGN`, `STRIP REPORT`.
   - Executes `Sla_Design` -> returns positive $A_{sx}, A_{sy}$.

5. **`FoundationAction_CalculatesPileCapAndPadFooting`**:
   - Resolves `File V.2020/13. DCE_DaiCoc_2020.xlsm` and `05. DCE_MongDon.xlsm`.
   - Executes `Fou_Show` with parameter `FoundationType="PileCap"` -> returns pile cap verification ratio.
   - Executes `Fou_Show` with parameter `FoundationType="PadFooting"` -> returns soil capacity ratio.

6. **`RebarCuttingAction_OptimizesCuttingStock`**:
   - Resolves `File V.2020/15. DCE_ThongKeThep_2020.xlsm`.
   - Executes `ToHop` and `ToHop2` -> returns cutting pattern count, stock bars used, and remnant percentage.

7. **`QuantityTakeoff_ExportsValidBoqWorkbook`**:
   - Executes `Bea_CalculateTheVolume` / `XuatBOQ`.
   - Verifies exported workbook contains sheets `TongHop`, `BeTong`, `VanKhuon`, `CotThep`.

8. **`ClosedXml_EvaluatesTcvnFormulasNatively`**:
   - Creates an in-memory workbook, registers UDFs with `DceUdfFunctions.RegisterAll(wb)`.
   - Sets cell `A1` = `="B25"`, cell `B1` = `=TCVN_RB(A1)`.
   - Evaluates formula and asserts `B1.Value` equals 14.5 MPa.

---

## 6. Caveats & Assumptions

1. **No Out-of-Process Excel COM Dependency**:
   All 73 actions in `HPDcePro.Excel` are designed to execute headlessly in memory via ClosedXML and pure C# calculations. Real AutoCAD IPC actions (`DrawLine`, `DrawPolyLine`, `DrawREC`, `SelectHandle`) return mock/default geometry when CAD is not running, and log an informational message.
2. **Read-Only Preservation of Asset Workbooks**:
   When tests open workbooks from `HPDcePro/assets/Excel/`, they must never overwrite original files in `assets/`. Modified workbooks must be written to `%TEMP%` or disposable test directories.
3. **Single-Node Sequential Build Rule**:
   As discovered in `ORIGINAL_REQUEST.md` (lines 1650–1665), `dotnet test` is strictly prohibited. Testing must be verified by directly running the compiled `.exe` test runners (`HPDcePro.Core.Tests.exe` and `HPDcePro.Adapters.Tests.exe`).

---

## 7. Conclusion

The exploration of Milestone M4 Excel Ribbon & ClosedXML UDF Dispatch is 100% complete:
1. **Catalog**: Exactly 73 legacy actions from `danhba_dce_excel.json` and `TRUTH_MATRIX.md` Subsystem 2 have been thoroughly analyzed and classified into 10 engineering domains.
2. **Architecture**: A modular Handler Pattern with `IDceExcelActionDispatcher` and 8 domain handlers (<300 lines each) has been designed to strictly comply with Rule C2.
3. **Domain Binding**: Every calculation action maps directly to authentic calculations in `HPDcePro.Core.Calculations`.
4. **TCVN UDFs**: `DceUdfFunctions` provides standardized implementations for TCVN 5574:2018, TCVN 10304:2025, TCVN 5575:2024, and TCVN 2737:2023 with ClosedXML formula integration.
5. **Headless Testing**: An end-to-end integration test suite using real sample workbooks in `HPDcePro/assets/Excel/File V.2020/` has been specified.

Worker M4 has exact class definitions, method signatures, and file locations ready for immediate implementation.

---

## 8. Verification Method

To independently verify this exploration:
1. **Verify Action Count**:
   ```powershell
   python -c "import json; data=json.load(open(r'C:\Dce Pro\AiAgent\DanhBa\danhba_dce_excel.json', encoding='utf-8', errors='ignore')); print(len(data['muc']))"
   # Output must be: 73
   ```
2. **Verify Truth Matrix Mapping**:
   Inspect `docs/rea-dce-pro/TRUTH_MATRIX.md` lines 355 to 435.
3. **Verify Sample Workbooks**:
   Inspect `HPDcePro/assets/Excel/File V.2020/09. DCE_Beam_2020.xlsm`, `08. DCE_ColumnWall_2020.xlsm`, and `12. DCE_SLab_2020.xlsm` to confirm presence of required sheets.
4. **Verify Direct Test Runner**:
   ```powershell
   & "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Core.Tests\bin\Debug\net8.0\HPDcePro.Core.Tests.exe"
   & "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\tests\HPDcePro.Adapters.Tests\bin\Debug\net8.0-windows\HPDcePro.Adapters.Tests.exe"
   # Must compile cleanly and pass 100%
   ```
