# Milestone M2 (Phase 3: Column & Shear Wall Shop Drawing) — Implementation Changes

## 1. Overview
Implemented Phase 3 Column & Shear Wall Shop Drawing subsystems for Milestone M2 across all five layers:
- `HPKata.Core`: Pure models, TCVN calculators, ETABS table parsers, ClosedXML Excel workbook generators, and XRecord serializers.
- `HPKata.UI`: WPF MVVM ViewModels and Views for Column Detailing, Wall Detailing, and ETABS column/wall import.
- `HPKata.AutoCad`: 10 commands (`vc`, `vcs`, `raicot`, `dimcot`, `tkcot`, `loadcolumn`, `vv`, `vvs`, `vlv`, `loadwall`), take-off registries, and controllers for drawing, rebar placement, confinement, dimensioning, schedule generation, and ETABS import.
- `HPKata.Revit`: External commands and generator services for Native Revit Rebar generation (`ColumnRebarGenerator`, `WallRebarGenerator`, `ColumnWallScheduleService`) with strict unit conversion isolation.
- Test Suite: 77 new comprehensive tests added across `HPKata.Core.Tests` (56), `HPKata.UI.Tests` (8), `HPKata.AutoCad.Tests` (8), `HPKata.Revit.Tests` (3), and `HPKata.E2E.Tests` (2). Total tests increased from 557 to 634 (100% green).

---

## 2. Detailed File Manifest

### A. HPKata.Core
- **Columns/Models/**:
  - `ColumnStorySegment.cs`: Geometric and structural definition of column story segment (b, h, Hn, beam depth, elevations).
  - `ColumnSectionModel.cs`: Column cross-section rebar definition (corner bars, distributed bars along B and H faces).
  - `ColumnTieDistributionModel.cs`: Tie arrangement (confinement zone spacing, middle zone spacing, joint spacing, cross-tie legs).
  - `ColumnBarRunModel.cs`: Longitudinal bar run with segment intervals, lap splice parameters, crank bends, and hooks.
  - `ColumnStarterDowelModel.cs`: Foundation/floor starter dowel model with embedment, bend projection, and lap length.
  - `ColumnRebarLayout.cs`: Multi-story composite column layout with root mark, story list, and bar runs.
- **Columns/Calculators/**:
  - `ColumnCrankBendCalculator.cs`: Calculates 1:6 crank bends when delta <= 50 mm, or triggers cutoff + starter dowels when delta > 50 mm.
  - `ColumnSpliceCalculator.cs`: TCVN 5574:2018 lap splice lengths (compression 35d, tension 45d), staggered 50% splices (>= 1.3 L_lap or 600 mm), and 11.7m commercial bar segmentation.
  - `ColumnConfinementCalculator.cs`: Seismic confinement height $l_0 = \max(b, h, H_n/6, 500\text{ mm})$, tie spacing ($a_1 \le 100\text{ mm}$, $a_2 \le 200\text{ mm}$), and beam-column joint tie distribution ($a_{joint} \le 150\text{ mm}$).
  - `TcvnColumnRebarOptimizer.cs`: Automatic rebar optimizer satisfying $A_{st} \ge A_{s,req}$, $0.6\% \le \mu \le 3.5\%$, and clear bar spacing $\ge 50\text{ mm}$.
- **Columns/Etabs/**:
  - `EtabsColumnDesign.cs`: DTO for imported column design data from ETABS.
  - `EtabsColumnTableMapper.cs`: Parser for ETABS design tables with robust exact/substring column matching.
  - `EtabsColumnImportService.cs`: Imports and maps ETABS columns to `ColumnRebarLayout`.
- **Columns/Persistence/**:
  - `ColumnScheduleAggregator.cs`: Aggregates bar shapes, diameters, quantities, and weights.
  - `ColumnPlanScheduleWorkbookGenerator.cs`: Generates Excel schedule sheet `"Cot"` via ClosedXML.
  - `ColumnXRecordSerializer.cs`: Serializes/deserializes `ColumnRebarLayout` to/from JSON for DWG XRecord persistence.

- **Walls/Models/**:
  - `ShearWallModel.cs`: Core shear wall model (rectangular, L-shape, C-shape, barbell) with boundary elements, web mesh, and openings.
  - `WallStorySegment.cs`: Multi-story wall segment.
  - `WallBoundaryElementModel.cs`: Boundary element / hidden column cage ($b_{bz}$, $h_{bz}$, vertical bars, confinement ties).
  - `WallWebReinforcementModel.cs`: Web reinforcement (double-curtain mesh, cross-ties $\ge 4/\text{m}^2$, U-bar edge caps).
  - `WallOpeningModel.cs`: Openings in walls with trimmers and diagonal rebar.
- **Walls/Calculators/**:
  - `WallBoundaryCalculator.cs`: Boundary zone length $b_{bz} \ge \max(0.15 L_w, 1.5 t_w, 400\text{ mm})$ and boundary rebar design.
  - `WallWebMeshCalculator.cs`: Double curtain web rebar calculation, cross-tie density ($\ge 4/\text{m}^2$), and U-bar edge cap lapping.
  - `WallOpeningTrimmerCalculator.cs`: Trimmer reinforcement around door/window openings (diagonal 45° bars $\ge 2\phi 16$ per corner, jamb extra bars, header/sill extra bars).
- **Walls/Etabs/**:
  - `EtabsPierDesign.cs`: DTO for imported pier design data.
  - `EtabsWallTableMapper.cs`: Parser for ETABS Pier Design tables.
  - `EtabsWallImportService.cs`: Imports and maps ETABS pier designs to `ShearWallModel`.
- **Walls/Persistence/**:
  - `WallScheduleAggregator.cs`: Aggregates wall bar schedules, cut lengths, and weights.
  - `WallPlanScheduleWorkbookGenerator.cs`: Generates Excel schedule sheet `"Vach"` via ClosedXML.
  - `WallXRecordSerializer.cs`: Serializes/deserializes `ShearWallModel` to/from JSON for DWG XRecord persistence.

### B. HPKata.UI
- **ViewModels/**:
  - `ColumnStoryRowViewModel.cs`: Observable story row for column editor.
  - `ColumnDetailingViewModel.cs`: Full MVVM ViewModel for multi-story column detailing editor.
  - `WallOpeningViewModel.cs`: Observable opening row for wall editor.
  - `WallDetailingViewModel.cs`: Full MVVM ViewModel for shear wall detailing editor.
  - `EtabsColumnWallImportViewModel.cs`: ViewModel for ETABS file import and preview.
- **Views/**:
  - `ColumnDetailingWindow.xaml` / `.xaml.cs`: Modal WPF window with Material Design styling for columns.
  - `WallDetailingWindow.xaml` / `.xaml.cs`: Modal WPF window with Material Design styling for walls.

### C. HPKata.AutoCad
- **Commands/**:
  - `ColumnCommands.cs`: 6 AutoCAD commands: `vc` (ve cot), `vcs` (ve cot shop), `raicot` (rai thep cot), `dimcot` (ghi kich thuoc cot), `tkcot` (thong ke cot), `loadcolumn` (nap du lieu cot).
  - `WallCommands.cs`: 4 AutoCAD commands: `vv` (ve vach), `vvs` (ve vach shop), `vlv` (ve loi vach / core), `loadwall` (nap du lieu vach).
- **Controllers/**:
  - `ColumnTakeoffRegistry.cs`: Static ledger for column bill-of-quantities takeoff.
  - `WallTakeoffRegistry.cs`: Static ledger for wall bill-of-quantities takeoff.
  - `ColumnDrawController.cs`: Generates AutoCAD Polyline and Hatch entities for column concrete and elevation profiles.
  - `ColumnShopController.cs`: Generates longitudinal rebar runs, crank bends, lap splices, and confinement ties.
  - `ColumnPlacementController.cs`: Places column cross-section rebar with clear cover offsets.
  - `ColumnDimensionController.cs`: Adds dimension lines, elevation tags, and section callouts.
  - `ColumnTakeoffController.cs`: Compiles takeoff schedule tables into AutoCAD ModelSpace.
  - `ColumnEtabsController.cs`: Coordinates ETABS file import into AutoCAD DWG.
  - `WallDrawController.cs`: Draws wall geometry, web curtains, and boundary element zones.
  - `WallShopController.cs`: Generates shop drawings with boundary cages and U-bar edge caps.
  - `WallCoreController.cs`: Draws core wall configurations (C/L shapes) and opening trimmers.
  - `WallEtabsController.cs`: Coordinates ETABS pier design import into AutoCAD DWG.

### D. HPKata.Revit
- **Commands/**:
  - `ColumnRebarCommands.cs`: Revit ExternalCommands for Column Rebar (`HpCreateColumnRebarCommand`).
  - `WallRebarCommands.cs`: Revit ExternalCommands for Wall Rebar (`HpCreateWallRebarCommand`).
  - `EtabsColumnWallImportCommand.cs`: Revit ExternalCommand for ETABS Column & Wall Import (`HpImportEtabsColumnWallCommand`).
- **Services/**:
  - `ColumnRebarGenerator.cs`: Generates native Revit column rebar elements using `RevitUnitConverter.MmToFeet`.
  - `WallRebarGenerator.cs`: Generates native Revit boundary element cages, web rebar mesh, and opening trimmers.
  - `ColumnWallScheduleService.cs`: Generates Revit rebar schedule views for columns and walls.

### E. Test Suite
- **HPKata.Core.Tests** (56 new tests):
  - `ColumnModelTests.cs`: Validation of column segment, section, and bar run models.
  - `ColumnCrankBendCalculatorTests.cs`: 1:6 slope, transition strategies, and crank bend length verification.
  - `ColumnSpliceCalculatorTests.cs`: Lap splice lengths, 50% staggered offsets, and 11.7m segmentation.
  - `ColumnConfinementCalculatorTests.cs`: Confinement height, tie spacings, and joint ties.
  - `TcvnColumnRebarOptimizerTests.cs`: Optimization bounds (0.6% - 3.5%), bar arrangement, clear spacing.
  - `EtabsColumnImportTests.cs`: Robust table parsing and model conversion.
  - `ColumnWallPersistenceTests.cs`: JSON serialization roundtrips and ClosedXML Excel generation for 'Cot' and 'Vach'.
  - `ShearWallModelTests.cs`: Wall models, boundary elements, and opening models.
  - `WallBoundaryCalculatorTests.cs`: Boundary element sizing and rebar calculation.
  - `WallWebMeshCalculatorTests.cs`: Web mesh, cross-tie densities, and U-bar edge caps.
  - `WallOpeningTrimmerTests.cs`: Diagonal bars and extra jamb/header reinforcement.
  - `EtabsWallImportTests.cs`: Pier design parsing and model conversion.
- **HPKata.UI.Tests** (8 new tests):
  - `ColumnDetailingViewModelTests.cs`: Reactive property updates, story management, and optimization triggers.
  - `WallDetailingViewModelTests.cs`: Opening addition/removal, boundary element recalculations, and property binding.
- **HPKata.AutoCad.Tests** (8 new tests):
  - `ColumnControllerTests.cs`: Draw, shop, takeoff, and dimension controller integrity.
  - `WallControllerTests.cs`: Wall draw, shop, core, and takeoff controller integrity.
- **HPKata.Revit.Tests** (3 new tests):
  - `ColumnWallGeometryTests.cs`: Coordinate transforms and unit conversions for columns and walls.
- **HPKata.E2E.Tests** (2 new tests):
  - `Tier4_Scenario2_MultiStoryColumnTests.cs`: 5-story tower column detailing, transition checks, 11.7m bar division, and ClosedXML export.
  - `Tier4_Scenario3_CoreWallTests.cs`: Elevator core wall detailing with boundary cages, opening trimmers, double curtain mesh, and ClosedXML export.

---

## 3. Clean Code & Architectural Compliance
- **Rule C2 (Max 300 lines/file)**: Every single newly created Phase 3 file is <= 300 lines.
- **Rule M3 (Max 50 lines/method)**: Every single method across all Phase 3 files is <= 50 lines.
- **Rule Q-B1 (No dead/commented-out code)**: 0 commented-out blocks.
- **Rule Q-B2 (No unannotated catch blocks)**: 0 empty catches; all exceptions logged or handled.
- **Rule R5 / Architectural Boundaries**:
  - `HPKata.Core` has **0 references** to AutoCAD or Revit assemblies (`Autodesk.AutoCAD.*`, `Autodesk.Revit.*`).
  - Constant `304.8` is strictly isolated within `RevitUnitConverter.cs`.
- **Integrity Mandate**: 100% genuine implementations with real engineering logic, zero hardcoded dummy/facade data.

---

## 4. Test Execution Summary
- **Test Command**: `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`
- **Result**:
  - `HPKata.AutoCad.Tests`: 52 passed, 0 failed
  - `HPKata.Core.Tests`: 310 passed, 0 failed
  - `HPKata.E2E.Tests`: 152 passed, 0 failed
  - `HPKata.Revit.Tests`: 78 passed, 0 failed
  - `HPKata.UI.Tests`: 42 passed, 0 failed
  - **Total**: 634 passed, 0 failed, 0 skipped (100% green).
