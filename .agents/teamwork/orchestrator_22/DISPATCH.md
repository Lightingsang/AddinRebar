# Dispatch Specification — Project Orchestrator (orchestrator_22)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_22\`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (UTC header `## 2026-10-18T00:11:55Z`)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\HPDcePro.slnx`
- Existing Baseline: 280 automated tests passing 100%, 0 warnings, 0 errors.

## Mission: HPDcePro Wave 3
Implement all requirements of Wave 3 end-to-end:

### R1. MCP Tool Catalog & Agent Skill (`HPDcePro.Mcp` & `.agents/skills/hp-dce-pro/SKILL.md`)
- Create `HPDcePro/src/HPDcePro.Mcp/HPDcePro.Mcp.csproj` (`net8.0`) and add it to `HPDcePro/HPDcePro.slnx`.
- Provide structured JSON-RPC / MCP tool dispatcher (`DceProMcpToolCatalog` & `DceProMcpToolExecutor`) exposing 8 comprehensive tools over `HPDcePro.Core` and `HPDcePro.Excel`:
  1. `dce_design_beam` (TCVN 5574:2018 single/continuous/foundation/curved beam design + shop drawing primitive summary)
  2. `dce_design_column_wall` (TCVN 5574:2018 biaxial P-Mx-My column & shear wall boundary element check)
  3. `dce_design_foundation_pile` (TCVN 10304:2025 SPT/friction pile capacity, pile cap & pad footing design)
  4. `dce_design_slab_stair_lintel` (Two-way Marcus slab, EngineBanThang/EngineZigZac staircase & opening lintel design)
  5. `dce_design_steel_peb` (TCVN 5575:2024 PEB portal frame, bolted moment connection, anchor bolt base plate & TCVN 2737:2023 wind load)
  6. `dce_optimize_rebar_cutting` (1D 11.7m commercial bar cutting stock optimizer ToiUuCatThep & TCVN 1651 schedule aggregator)
  7. `dce_query_mayhoc_rebar` (Query MayHoc/mauhoc.db ML patterns & tokenize Vietnamese rebar callout strings)
  8. `dce_run_autostr_pipeline` (Execute the 24-step AutoStrOrchestrator on a .autostr project with auto-resizing & multi-sheet ClosedXML BOQ export)
- Create `.agents/skills/hp-dce-pro/SKILL.md` documenting tool usage for coding agents.

### R2. `DbimSteelAutocad` Steel CAD Drafting Generator & Commands (`HPDcePro.Core` & `HPDcePro.AutoCad`)
- Implement `SteelFrameDraftingGenerator` in `HPDcePro.Core/Drafting/` generating 2D CAD primitives (`CadDraftingBuffer`) for PEB portal frame elevations (`MiddleFrameBT`, `MiddleFrame3`, `MiddleFrame3BT`, `MiddleFrame5`, `MiddleFrame5a`, `VDS1`), roof purlin & monitor plans (`PlanPurlinRoof`, `PlanPurlinMonitor`), and anchor bolt details (`DetailAnchorBolt`).
- Implement `SteelCadCommands` in `HPDcePro.AutoCad/Commands/` registering `[CommandMethod]` handlers for `DrawElevationMiddleFrameBT`, `DrawElevationMiddleFrame3`, `DrawElevationMiddleFrame3BT`, `DrawElevationMiddleFrame5`, `DrawElevationMiddleFrame5a`, `DrawElevationVDS1`, `DrawPlanPulirnRoof`, `DrawPlanPurlinMonitor`, `DetailAnchorBolt`, and `SteelConnectionDialog`, and add their LISP aliases to `HPDcePro.lsp`.

### R3. Complete `DCE Excel` & `DceSteel` UDF Parity (`HPDcePro.Excel`)
- Expand `CalculationUdfBridge` (or companion modular UDF classes in `HPDcePro.Excel`) to expose backward-compatible static functions for all remaining calculations in `V.2020/DCE Excel.dll` and `AddinSteel/DceSteel.dll`:
  - RC & Geotechnical: `TinhCotNeUonXien_PMM`, `TinhVachCung`, `TinhSanHaiPhuong`, `TinhDaiCoc`, `TinhMongDon`, `SucChiuTaiCoc_MaSat`, `BeRongVetNutDam`.
  - Steel & Wind: `TinhDamThep_TCVN5575`, `TinhCotThep_TCVN5575`, `TinhLienKetMoment_TCVN5575`, `TinhBuLongNeo_TCVN5575`, `TinhTaiTrongGio_TCVN2737`.

### R4. Unified `MainControlCenterWindow` (`GETFORM` Hub) & AutoCAD `.bundle` Manifest
- Add `MainControlCenterWindow` and `MainControlCenterViewModel` (`MahApps.Metro` `MetroWindow`) in `HPDcePro.UI` serving as the central dashboard launched by `GETFORM` (with navigation commands to open `BeamDetailingWindow`, `ColumnWallDetailingWindow`, `FoundationSlabWindow`, `CuttingOptimizationWindow`, `SteelConnectionWindow`, and `AutoStrConfigWindow`, plus live project summary stats).
- Create `HPDcePro/deploy/HPDcePro.bundle/PackageContents.xml` for AutoCAD 2025–2026 ApplicationPlugins deployment.

## Acceptance Criteria
- `C:\Dce Pro` remains 100% untouched (0 files modified, created, or deleted).
- `dotnet build HPDcePro/HPDcePro.slnx -c Debug` succeeds with 0 errors and 0 warnings (`TreatWarningsAsErrors=true`).
- Every C# file in `HPDcePro/` strictly obeys `HP_CLEAN_CODE_CORE.md` (<= 300 lines per file, methods <= 50 lines, 0 commented-out code, 0 unannotated empty catch blocks).
- `dotnet test HPDcePro/HPDcePro.slnx -c Debug` passes 100% with **at least 330 automated tests** (0 failed, 0 skipped).
