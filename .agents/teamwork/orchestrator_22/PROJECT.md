# Project: HPDcePro Wave 3

## Architecture
HPDcePro is a multi-project engineering automation solution in C# .NET (`HPDcePro/HPDcePro.slnx`):
- `HPDcePro.Core` (`netstandard2.0;net8.0`): Pure domain models, structural calculators (TCVN 5574:2018, TCVN 10304:2025, TCVN 5575:2024, TCVN 2737:2023, TCVN 1651:2018), 2D CAD drafting buffers (`CadDraftingBuffer`), 24-step `AutoStrOrchestrator`, ML tokenizers, and 1D cutting stock optimizers. Zero dependency on CAD/Revit/Office.
- `HPDcePro.Excel` (`net8.0`): ClosedXML (0.104.2) headless multi-sheet BOQ/report exporters and high-speed calculation UDF bridges. Zero COM reliance.
- `HPDcePro.Mcp` (`net8.0`): Stdio JSON-RPC 2.0 Model Context Protocol tool server exposing 8 domain tools over `HPDcePro.Core` and `HPDcePro.Excel` for coding agents.
- `HPDcePro.AutoCad` (`net8.0-windows`): AutoCAD 2025–2026 commands (`[CommandMethod]`), drafting buffer renderers, and AutoLISP aliases (`HPDcePro.lsp`).
- `HPDcePro.UI` (`net8.0-windows`): `MahApps.Metro` WPF MVVM windows, themes, and `MainControlCenterWindow` (`GETFORM` dashboard hub).
- `HPDcePro.Revit` (`net8.0-windows`): Revit 2026 rebar placement and data exchange adapters.
- `HPDcePro.Core.Tests` & `HPDcePro.Adapters.Tests`: xUnit v3 / MTP test suites (280 baseline passing tests -> expanding to >= 330 tests).

## Feature Inventory
Every feature from the Survey phase is mapped to an assigned milestone:
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | `HPDcePro.Mcp.csproj` | Net8.0 console executable project added to HPDcePro.slnx | M1 | Survey 1 |
| 2 | `DceProMcpToolCatalog` | Registry defining metadata & JSON Schemas for 8 MCP tools | M1 | Survey 1 |
| 3 | `DceProMcpToolExecutor` | Dispatcher routing JSON-RPC calls to dedicated tool handlers | M1 | Survey 1 |
| 4 | `dce_design_beam` | TCVN 5574:2018 single/continuous/foundation/curved beam design & drawing | M1 | Survey 1 |
| 5 | `dce_design_column_wall` | TCVN 5574:2018 biaxial P-Mx-My column & shear wall boundary elements | M1 | Survey 1 |
| 6 | `dce_design_foundation_pile` | TCVN 10304:2025 SPT pile capacity, pile cap & pad footing design | M1 | Survey 1 |
| 7 | `dce_design_slab_stair_lintel` | Marcus two-way slab, staircase (EngineBanThang/ZigZac), lintel | M1 | Survey 1 |
| 8 | `dce_design_steel_peb` | TCVN 5575:2024 PEB portal frame, moment connection, base plate, wind | M1 | Survey 1 |
| 9 | `dce_optimize_rebar_cutting` | 1D 11.7m commercial bar cutting optimizer & TCVN 1651 aggregator | M1 | Survey 1 |
| 10 | `dce_query_mayhoc_rebar` | ML mauhoc.db queries & Vietnamese rebar text tokenization | M1 | Survey 1 |
| 11 | `dce_run_autostr_pipeline` | 24-step AutoStrOrchestrator execution & ClosedXML BOQ export | M1 | Survey 1 |
| 12 | `SKILL.md` | Agent skill documentation at `.agents/skills/hp-dce-pro/SKILL.md` | M1 | Survey 1 |
| 13 | `SteelFrameDraftingGenerator` | 2D CAD drafting buffer generator for PEB portal frames | M2 | Survey 2 |
| 14 | `ElevationMiddleFrameBT` | Portal frame with concrete columns and steel rafters | M2 | Survey 2 |
| 15 | `ElevationMiddleFrame3` & `3BT` | 3-column, 2-bay steel and concrete-column portal frames | M2 | Survey 2 |
| 16 | `ElevationMiddleFrame5` & `5a` | 5-column, 4-bay portal frames with monitor/crane clearance | M2 | Survey 2 |
| 17 | `ElevationVDS1` | Triangular / trapezoidal steel roof truss VDS1 | M2 | Survey 2 |
| 18 | `PlanPurlinRoof` (`DrawPlanPulirnRoof`) | Roof purlin layout plan, sag rods, and cross bracings | M2 | Survey 2 |
| 19 | `PlanPurlinMonitor` | Roof monitor purlin & louver plan | M2 | Survey 2 |
| 20 | `DetailAnchorBolt` | Column base plate plan, section, grout bed & L-anchor bolts | M2 | Survey 2 |
| 21 | `SteelCadCommands.cs` | 10 AutoCAD `[CommandMethod]` registrations in `HPDcePro.AutoCad` | M2 | Survey 2 |
| 22 | `HPDcePro.lsp` Steel Aliases | 10 LISP aliases mapping to steel CAD drafting commands | M2 | Survey 2 |
| 23 | `RcGeotechUdfBridge.cs` | RC & Geotechnical UDF implementations in `HPDcePro.Excel` | M3 | Survey 3 |
| 24 | `TinhCotNeUonXien_PMM` | Biaxial P-M-M interaction check under TCVN 5574:2018 | M3 | Survey 3 |
| 25 | `TinhVachCung` | Core/shear wall boundary element & shear check | M3 | Survey 3 |
| 26 | `TinhSanHaiPhuong` | Marcus two-way slab elastic moments & required rebar | M3 | Survey 3 |
| 27 | `TinhDaiCoc` | Pile cap reactions, punching shear & flexural rebar | M3 | Survey 3 |
| 28 | `TinhMongDon` | Isolated pad footing soil pressure, uplift, punching & flexure | M3 | Survey 3 |
| 29 | `SucChiuTaiCoc_MaSat` | Pile skin friction capacity per TCVN 10304:2025 SPT | M3 | Survey 3 |
| 30 | `BeRongVetNutDam` | Beam crack width $a_{crc}$ check per TCVN 5574:2018 | M3 | Survey 3 |
| 31 | `SteelWindUdfBridge.cs` | Steel & Wind UDF implementations in `HPDcePro.Excel` | M3 | Survey 3 |
| 32 | `TinhDamThep_TCVN5575` | Steel I-beam flexure & lateral-torsional buckling $\varphi_b$ | M3 | Survey 3 |
| 33 | `TinhCotThep_TCVN5575` | Steel I-column in-plane & out-of-plane stability Ch. 7 | M3 | Survey 3 |
| 34 | `TinhLienKetMoment_TCVN5575` | Bolted extended end-plate moment connection | M3 | Survey 3 |
| 35 | `TinhBuLongNeo_TCVN5575` | Column base plate bearing & anchor bolt tension | M3 | Survey 3 |
| 36 | `TinhTaiTrongGio_TCVN2737` | TCVN 2737:2023 aerodynamic wind pressure ($q_0 \cdot k(z_e) \cdot c$) | M3 | Survey 3 |
| 37 | `CalculationUdfBridge.cs` update | Backward-compatible facade delegating all 12 UDFs | M3 | Survey 3 |
| 38 | `MainControlCenterViewModel.cs` | GETFORM central hub ViewModel with KPI metrics & navigation | M4 | Survey 2 |
| 39 | `MainControlCenterWindow.xaml` | MahApps.Metro dashboard window with 6 module cards | M4 | Survey 2 |
| 40 | `GETFORM` Command redirection | Redirects `GETFORM` in `AutoStrCommands.cs` to MainControlCenterWindow | M4 | Survey 2 |
| 41 | `PackageContents.xml` | AutoCAD 2025–2026 autoloader bundle manifest in `deploy/` | M4 | Survey 2 |
| 42 | Test Suite Expansion | Expand test suite to >= 330 passing automated tests | M5 | All |
| 43 | Forensic Clean Code Audit | 0 files > 300 lines, 0 methods > 50 lines, 0 comments, 0 unannotated catch | M5 | All |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | MCP Tool Catalog & Skill (R1) | `HPDcePro.Mcp`, `DceProMcpToolCatalog`, `DceProMcpToolExecutor`, 8 tools, `.agents/skills/hp-dce-pro/SKILL.md` | none | DONE |
| M2 | Steel CAD Drafting & Commands (R2) | `SteelFrameDraftingGenerator`, `SteelCadCommands`, `HPDcePro.lsp` aliases | none | DONE |
| M3 | Complete Excel UDF Parity (R3) | `RcGeotechUdfBridge`, `SteelWindUdfBridge`, `CalculationUdfBridge` updates | none | DONE |
| M4 | MainControlCenter Hub & Bundle (R4) | `MainControlCenterWindow`, `MainControlCenterViewModel`, `PackageContents.xml`, `GETFORM` redirection | none | DONE |
| M5 | Automated Verification & Audit | Test expansion across Core.Tests & Adapters.Tests to >= 330 tests (0 fail, 0 skip), Clean Code verification | M1, M2, M3, M4 | IN_PROGRESS |

## Interface Contracts
### `HPDcePro.Mcp` ↔ `HPDcePro.Core` & `HPDcePro.Excel`
- Tools invoke calculation classes:
  - `dce_design_beam` -> `BeamFlexureCalculator`, `BeamShearCalculator`, `BeamCrackCalculator`, `BeamShopDrawingGenerator`
  - `dce_design_column_wall` -> `ColumnPmmInteractionCalculator`, `ShearWallCalculator`, `ColumnDetailingGenerator`
  - `dce_design_foundation_pile` -> `PileCapacityCalculator`, `PileCapCalculator`, `PadFootingCalculator`
  - `dce_design_slab_stair_lintel` -> `TwoWaySlabCalculator`, `StaircaseCalculator`, `LintelCalculator`
  - `dce_design_steel_peb` -> `PreGableFrameCalculator`, `SteelMomentConnectionCalculator`, `AnchorBoltBasePlateCalculator`, `WindLoadCalculator`
  - `dce_optimize_rebar_cutting` -> `RebarCuttingOptimizer`, `RebarScheduleAggregator`
  - `dce_query_mayhoc_rebar` -> `RebarTextTokenizer`, `MauHocDatabaseReader`
  - `dce_run_autostr_pipeline` -> `AutoStrOrchestrator`, `AutoStrProjectSerializer`, `ClosedXmlBoqExporter`
- Input: JSON-RPC parameters parsed via `System.Text.Json` into typed DTOs.
- Output: `DceProToolResult` serialized to JSON-RPC result format with `isSuccess` and `data`.

### `SteelCadCommands` ↔ `SteelFrameDraftingGenerator`
- `SteelFrameDraftingGenerator` produces `CadDraftingBuffer` (collection of `CadEntityPrimitive`: `Line`, `Polyline`, `Circle`, `Text`, `Dimension`).
- `SteelCadCommands` invokes `CadDraftingBufferRenderer.Render(buffer, db, tr, btr)` inside an AutoCAD `Transaction`.
- Fallback: In headless/test mode where `MdiActiveDocument` is null, returns primitive count cleanly without exception.

### `CalculationUdfBridge` ↔ `RcGeotechUdfBridge` & `SteelWindUdfBridge`
- All methods are `public static double` or `public static int` returning numeric results with zero exception throwing on invalid input (returns `0.0` or default safe values).
- `CalculationUdfBridge` forwards calls to `RcGeotechUdfBridge` and `SteelWindUdfBridge`.

### `MainControlCenterViewModel` ↔ Child Windows
- Navigation commands (`OpenBeamDetailingCommand`, etc.) accept an optional navigation delegate `Action<Type>?` for headless unit testing.
- When clicked in interactive AutoCAD runtime, displays target Metro window modally via `AutoCadUiHelper.ShowModal(win)`.

## Code Layout & Write Boundaries
To prevent concurrency conflicts, each Milestone has exclusive write ownership:
- **Milestone M1 Ownership**:
  - `HPDcePro/HPDcePro.slnx` (adding `src/HPDcePro.Mcp/HPDcePro.Mcp.csproj`)
  - `HPDcePro/src/HPDcePro.Mcp/**` (all files in Mcp project)
  - `.agents/skills/hp-dce-pro/SKILL.md`
- **Milestone M2 Ownership**:
  - `HPDcePro/src/HPDcePro.Core/Drafting/SteelFrameDraftingModels.cs`
  - `HPDcePro/src/HPDcePro.Core/Drafting/SteelFrameDraftingGenerator*.cs`
  - `HPDcePro/src/HPDcePro.AutoCad/Commands/SteelCadCommands.cs`
  - `HPDcePro/src/HPDcePro.AutoCad/HPDcePro.lsp`
- **Milestone M3 Ownership**:
  - `HPDcePro/src/HPDcePro.Excel/RcGeotechUdfBridge.cs`
  - `HPDcePro/src/HPDcePro.Excel/SteelWindUdfBridge.cs`
  - `HPDcePro/src/HPDcePro.Excel/CalculationUdfBridge.cs`
- **Milestone M4 Ownership**:
  - `HPDcePro/src/HPDcePro.UI/ViewModels/MainControlCenterViewModel.cs`
  - `HPDcePro/src/HPDcePro.UI/Views/MainControlCenterWindow.xaml`
  - `HPDcePro/src/HPDcePro.UI/Views/MainControlCenterWindow.xaml.cs`
  - `HPDcePro/src/HPDcePro.AutoCad/Commands/AutoStrCommands.cs` (redirecting `GETFORM`)
  - `HPDcePro/deploy/HPDcePro.bundle/PackageContents.xml`
- **Milestone M5 Ownership**:
  - `HPDcePro/tests/HPDcePro.Core.Tests/**`
  - `HPDcePro/tests/HPDcePro.Adapters.Tests/**`
  - Verification & Forensic Integrity Audit
