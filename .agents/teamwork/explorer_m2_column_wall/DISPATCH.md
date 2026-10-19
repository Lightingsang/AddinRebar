## 2026-10-17T08:46:44Z
You are explorer_m2_column_wall, the Domain & Architecture Explorer for Milestone M2 (Phase 3: Column & Shear Wall Shop Drawing).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_column_wall\
Your parent conversation ID is: a7a2dbbb-6b7d-4279-98cd-1216d8280d80

MANDATORY FIRST STEPS:
1. Read the authoritative original request at:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under header ## 2026-10-17T06:46:41Z)
2. Read the master project scope at:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md
3. Inspect completed Phase 2 patterns in HPKata to ensure design continuity:
   - HPKata/HPKata.Core/Beams/
   - HPKata/HPKata.UI/
   - HPKata/HPKata.AutoCad/
   - HPKata/HPKata.Revit/

YOUR MISSION:
Perform complete specification mining, architecture design, and test planning for Phase 3 (Column & Shear Wall Shop Drawing):
1. HPKata.Core (Column & Shear Wall Domain Models & Calculators):
   - Multi-story column elevation & cross-section detailing: ColumnStorySegment, ColumnSectionModel, ColumnRebarLayout.
   - Longitudinal bar splice zones (lap splice length, staggered couplers, crank/offset bends at story transitions and column step-downs).
   - Column ties & cross-ties (rectangular hoops, diamond hoops, cross-ties, seismic confinement zones l0 at floor/ceiling boundaries).
   - Shear wall & core wall detailing: ShearWallModel, WallBoundaryElementModel, boundary zone confinement ties (ve_loi_vach), double-curtain web reinforcement (horizontal & vertical bars), openings, and U-bar edge hooks.
   - ETABS column & pier/wall import service (loadcolumn, loadwall): reading story heights, column/wall dimensions, design forces, and required reinforcement areas.
2. HPKata.UI (Column & Wall WPF ViewModels & Views):
   - ColumnDetailingViewModel & ColumnDetailingWindow: Multi-story column table, section dimensions, longitudinal bars, tie patterns, splice options, Theme.xaml brush binding.
   - WallDetailingViewModel & WallDetailingWindow: Shear wall elevation, boundary elements, web mesh, opening definition.
3. HPKata.AutoCad & HPKata.Revit (Commands & Lisp Aliases):
   - AutoCAD commands & GoiLenh_HPKata.lsp aliases: ve_cot (vc), ve_cot_shop (vcs), raicot, dimcot, tkcot, loadcolumn, ve_vach (vv), ve_vach_shop (vvs), ve_loi_vach (vlv), loadwall.
   - Revit commands/services for Structural Columns (OST_StructuralColumns) and Walls (OST_Walls) rebar generation, boundary ties, and scheduling.
4. Clean Code Architecture:
   - Partition all planned classes so that every file is <= 300 lines and every method <= 50 lines.
   - Core has zero CAD/Revit dependencies.

DELIVERABLE:
Write your report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_column_wall\column_wall_spec_report.md`
and a concise handoff.md in your working directory.
When complete, notify parent via send_message with your report path and summary.
