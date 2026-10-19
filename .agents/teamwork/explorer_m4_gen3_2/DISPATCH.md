## 2026-10-18T09:48:31Z
You are explorer_m4_gen3_2.
Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_gen3_2\
Parent Conversation ID: 1e55a9de-0d52-405d-83fa-fa58bb05a293

MANDATORY READING BEFORE WORK:
- ORIGINAL_REQUEST.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- Master Scope: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25_gen3\PROJECT.md
- Truth Matrix: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\rea-dce-pro\TRUTH_MATRIX.md

MISSION: Milestone M4 Excel Ribbon & ClosedXML UDF Exploration
1. Examine HPDcePro/src/HPDcePro.Excel/:
   - Inspect existing implementation and interfaces.
   - Inspect TRUTH_MATRIX.md Subsystem 2 and C:\Dce Pro\System\danhba_dce_excel.json (or migrated assets).
   - Catalog all 73 Excel ribbon dispatch actions across Beam, Column, Slab, Foundation, Wall, Steel, and General calculations.
2. Design the Action Dispatcher:
   - Design `IDceExcelActionDispatcher` and `DceExcelActionDispatcher` to route each of the 73 action keys (e.g. `TinhDam`, `TinhCot`, `TinhMong`, `XuatBOQ`, etc.) to corresponding calculation handlers.
   - Wire calculation handlers directly to authentic calculation engines in `HPDcePro.Core.Calculations` (`BeamFlexureCalculator`, `ColumnPmmInteractionCalculator`, `PileCapCalculator`, `PunchingShearCalculator`, `QuantityTakeoffCalculator`, etc.).
3. Design TCVN UDF Bridge:
   - Provide standard engineering UDF implementations callable from Excel or ClosedXML formulas:
     `TCVN 5574:2018` concrete & rebar strength properties, `TCVN 10304:2025` pile capacity, `TCVN 5575:2024` steel structure checks, `TCVN 2737:2023` wind load coefficients.
4. Design Headless Testability:
   - Design ClosedXML integration tests opening migrated sample workbooks from `HPDcePro/assets/Excel/`, executing dispatch actions, and asserting cell values.
5. Detail the exact classes, method signatures, and file structure Worker M4 must implement.
6. Write your comprehensive report to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_gen3_2\handoff.md
   and notify the parent orchestrator via send_message.
