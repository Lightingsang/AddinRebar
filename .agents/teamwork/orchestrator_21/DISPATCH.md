# DISPATCH — 2026-10-17T22:07:16Z

## Task Assignment for orchestrator_21 (Project Orchestrator)

You are **orchestrator_21**, the Project Orchestrator responsible for Wave 2: Implementing Phases 2 through 6 of `HPDcePro` (`HPDcePro.slnx`) end-to-end on top of the completed Phase 1 baseline.

Working directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_21\`
Parent Sentinel conversation ID: `0a049d24-8203-4e76-b372-e203b1b67e80`
Authoritative user request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (see timestamp `## 2026-10-17T22:07:16Z`)

## Verification Resources (Read-Only)
- Existing Phase 1 baseline (118 passing tests, 0 warnings, 0 errors):
  - `HPDcePro/HPDcePro.slnx` (`HPDcePro.Core`, `HPDcePro.UI`, `HPDcePro.AutoCad`, `HPDcePro.Excel`, `HPDcePro.Revit`, `HPDcePro.Core.Tests`, `HPDcePro.Adapters.Tests`)
  - REA Reverse-Engineering Dossier in `docs/rea-dce-pro/` (`README.md`, `01-domain-architecture.md`, `02-autostr-and-mayhoc.md`, `03-steel-and-excel.md`, `04-command-mapping-and-roadmap.md`)
- Shipped target binaries and reference data (strictly read-only):
  - `C:\Dce Pro\DCE Pro.dll`, `C:\Dce Pro\DceAnalyssiss64.dll`, `C:\Dce Pro\DCE Pro.lsp`
  - `C:\Dce Pro\AutoStr\DuAnMau.autostr`
  - `C:\Dce Pro\MayHoc\mauhoc.db`
  - `C:\Dce Pro\AddinSteel\DceSteel.dll`, `C:\Dce Pro\AddinSteelCad\DbimSteelAutocad.dll`, `C:\Dce Pro\V.2020\DCE Excel.dll`
- Clean Code Governance:
  - `AGENTS.md`, `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/CODE_REVIEW_CHECKLIST.md`

## Requirements Summary

### R1. Phase 2 — Continuous Beam Detailing, Drafting Primitives & ETABS Bridge (`HPDcePro.Core` & `HPDcePro.AutoCad`)
- Implement `ContinuousBeamCalculator`, `FoundationBeamCalculator`, and `CurvedBeamCalculator` in `HPDcePro.Core` supporting multi-span moment redistribution, top/bottom bar curtailment, anchorage/lap zones, and concentrated stirrup zones at primary-secondary beam intersections (`BeamTrimmerV2` / `dam_giao`).
- Implement a host-independent 2D CAD Drafting Primitive Engine (`CadDraftingBuffer`, `CadEntityPrimitive` for lines, polylines, circles, hatches, dimensions, block inserts, text tags, and attached `XDataPacket`s) and `BeamShopDrawingGenerator` producing complete longitudinal elevations and cross-sections (1-1, 2-2) with `RebarXdataDCE` metadata.
- Implement `EtabsModelSynchronizer` in `HPDcePro.Core` for bidirectional story/grid/frame section/force and required reinforcement ($A_s$) exchange.

### R2. Phase 3 — Column & Shear Wall Detailing + Biaxial P-M-M Engine (`HPDcePro.Core` & `HPDcePro.UI`)
- Implement `ColumnPmmInteractionCalculator` under TCVN 5574:2018 (axial-biaxial bending $N$-$M_x$-$M_y$ capacity, slenderness buckling $\eta$, and utilization ratio) and `ColumnDetailingGenerator` (multi-story column splice zones $H_n/3..2H_n/3$, 1:6 cranked bars, rectangular single/multi-tie `ColumnREC1`/`ColumnREC2`, and circular `ColumnCi` cross-sections).
- Implement `ShearWallCalculator` (`VeVachDcePro`) for core wall boundary elements, web vertical/horizontal reinforcement, and shear check under TCVN 5574:2018.
- Add `ColumnWallDetailingWindow` and `ColumnWallDetailingViewModel` (`MahApps.Metro` `MetroWindow`) in `HPDcePro.UI`.

### R3. Phase 4 — Substructure (Pile Cap & Pad Footing), Two-Way Slab, Stairs & Lintel (`HPDcePro.Core` & `HPDcePro.UI`)
- Implement `PileCapCalculator` (`VeMongCoc_DCE` — 1 to N pile layouts, individual pile reactions under axial load + biaxial moment, punching shear along pile cap pyramid, one-way shear, flexural rebar, and pile coordinate schedule `XuatToaDoCoc`) and `PadFootingCalculator` (`VeMongDon_DCE` — base pressure check, uplift check, punching shear, and two-way bending).
- Implement `TwoWaySlabCalculator` (`VeThepSanTopX/Y`, `VeThepSanBotX/Y`, `ThongKeThepSan` — two-way slab strip moments, crack/deflection checks, and top/bottom bar layout).
- Implement `StaircaseCalculator` (`VeThang_DCE` — flight slab `EngineBanThang` and zig-zag `EngineZigZac` rebar geometry & schedule) and `LintelCalculator` (`VeBlockLanhTo`, `VeMatCatLanhTo`).
- Add `FoundationSlabWindow` and `FoundationSlabViewModel` (`MahApps.Metro` `MetroWindow`) in `HPDcePro.UI`.

### R4. Phase 5 — Full 24-Step `AutoStr` Pipeline Orchestrator & Quantity Surveying BOQ (`HPDcePro.Core` & `HPDcePro.Excel`)
- Implement `AutoStrOrchestrator` and all 24 step processors (`AutoStrStep01`..`AutoStrStep24`) in `HPDcePro.Core/AutoStr/` driven by `AutoStrConfig` (`.autostr`), including input validation, step progress/result tracking, and the automated iterative resizing loop (auto-increasing column section / pile count when capacity ratio > 1.0 as configured by `CotChoTangTietDien` and `CocTuTangKhiKhongDat`).
- Implement `QuantityTakeoffCalculator` computing Concrete volume ($m^3$), Formwork area ($m^2$), and Rebar tonnage ($kg$) by element type and story, and expand `ClosedXmlReportExporter` in `HPDcePro.Excel` to generate full multi-sheet calculation & BOQ workbooks.

### R5. Phase 6 — TCVN 5575:2024 Steel Connections, Full CAD Command Execution & Revit Rebar Integration
- Implement `PreGableFrameCalculator` (portal frame & purlin layout), `SteelMomentConnectionCalculator` (TCVN 5575:2024 bolted end-plate moment connection, flange/web welds, bolt shear/tension interaction, stiffeners), and `AnchorBoltBasePlateCalculator` (`DetailAnchorBolt`), plus `SteelConnectionWindow` and `SteelConnectionViewModel` (`MahApps.Metro` `MetroWindow`) in `HPDcePro.UI`.
- Upgrade all command handlers in `HPDcePro.AutoCad/Commands/` (`BeamCommands`, `ColumnCommands`, `SlabCommands`, `FootingCommands`, `RebarCommands`, `AutoStrCommands`) so they execute real domain calculation, drafting primitive generation, and UI workflows instead of placeholder messages.
- Expand `HPDcePro.Revit` with `RevitRebarPlacementService` mapping `HPDcePro.Core` beam/column/footing detailing models to Revit structural framing and rebar descriptors.

## Acceptance Criteria
- [ ] `C:\Dce Pro` remains 100% untouched (0 files modified, created, or deleted).
- [ ] `dotnet build HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` succeeds with `0 errors` and `0 warnings` (`TreatWarningsAsErrors=true`).
- [ ] Every C# file in `HPDcePro/` strictly obeys `HP_CLEAN_CODE_CORE.md` ($\le 300$ lines per file, methods $\le 50$ lines, 0 commented-out code, 0 unannotated empty catch blocks).
- [ ] `HPDcePro.Core` contains working calculators and drafting generators for Continuous/Curved/Foundation Beams, Column P-M-M & Shear Wall, Pile Cap & Pad Footing, Two-Way Slab, Staircase & Lintel, the 24-Step `AutoStrOrchestrator` with iterative auto-sizing, Quantity Takeoff BOQ, and TCVN 5575:2024 Steel Moment Connections & Base Plates.
- [ ] `HPDcePro.UI` provides `MahApps.Metro` windows and viewmodels for `BeamDetailingWindow`, `CuttingOptimizationWindow`, `AutoStrConfigWindow`, `ColumnWallDetailingWindow`, `FoundationSlabWindow`, and `SteelConnectionWindow`.
- [ ] All 70 AutoCAD commands in `HPDcePro.AutoCad` execute real domain services, drafting generators, or `MahApps.Metro` dialogs.
- [ ] `dotnet test HPDcePro/HPDcePro.slnx -c Debug --disable-build-servers -m:1` passes 100% with **at least 200 automated tests** (`0 failed, 0 skipped`) covering all Phases 1–6 calculators, 2D drafting primitive generation, the 24-step `AutoStrOrchestrator` end-to-end execution on `DuAnMau.autostr`, Excel multi-sheet BOQ/report export, `MahApps.Metro` ViewModels/Windows, Revit rebar mapping, and AutoCAD command execution.
