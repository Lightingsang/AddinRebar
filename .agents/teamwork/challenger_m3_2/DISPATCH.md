# Dispatch: challenger_m3_2

## Role & Mission
You are `challenger_m3_2`, performing adversarial stress-testing and empirical verification on Milestone M3 (Phase 4: Foundation, Slab, Stair & Lintel Detailing), specifically the Stair, Lintel, Cross-Section, and Persistence subsystems.

## Mission Scope
1. Empirically stress-test `StairGeometryCalculator.cs` & `StairRebarCalculator.cs`:
   - Blondel ergonomic formula: 2 * h_r + b_t in [580, 660] mm. Test extreme values (steep flights, shallow treads).
   - Scissor-crank crossing at re-entrant corners: tension bars must not bend inward. Extension into compression zone L_ext >= max(35 * d, 500mm). Test diameters 10mm to 32mm.
   - Multi-flight and landing junctions.
2. Empirically stress-test `LintelRebarCalculator.cs`:
   - Bearing length checks: L_bearing >= max(200mm, h_lintel / 2).
   - Short spans vs long spans.
3. Empirically stress-test `GenericSectionCalculator.cs`:
   - Section cut planes through beams, columns, walls, stairs.
4. Empirically stress-test ClosedXML Workbook Generators:
   - FoundationPlanScheduleWorkbookGenerator (sheet "Mong").
   - SlabPlanScheduleWorkbookGenerator (sheet "San").
   - StairPlanScheduleWorkbookGenerator (sheet "Thang").
   - Confirm openable, valid XML structures, correct column formulas, non-zero bar weights.
5. Build & Test mandate:
   If running tests, ALWAYS use `--disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`.
6. Verdict:
   Provide an empirical verification verdict: CONFIRMED_CORRECT or DEFECT_FOUND.

## 2026-10-17T11:28:52Z
You are challenger_m3_2, performing adversarial stress-testing and empirical verification on Milestone M3 (Phase 4: Foundation, Slab, Stair & Lintel Detailing), Stair, Lintel, Cross-Section, and Persistence domains.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_2\
Your parent conversation ID is: a7a2dbbb-6b7d-4279-98cd-1216d8280d80

MANDATORY FIRST STEPS:
1. Read the authoritative original request at:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under headers ## 2026-10-16T23:55:10Z and ## 2026-10-17T06:46:41Z)
2. Read the master project scope:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md
3. Read the worker's changes and handoff:
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_remediation\changes.md
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_remediation\handoff.md
4. Read your task dispatch:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_2\DISPATCH.md

YOUR MISSION:
Empirically stress-test the Stair, Lintel, Generic Section, and ClosedXML persistence subsystems:
1. Stress-test `StairGeometryCalculator.cs` & `StairRebarCalculator.cs`:
   - Blondel ergonomic check: 2 * h_r + b_t in [580, 660] mm. Test steep risers (220mm), flat treads (240mm).
   - Scissor-crank crossing at re-entrant corner: tension bars must not bend inward; extension into compression zone L_ext >= max(35 * d, 500mm). Test bar diameters from 10mm to 32mm.
2. Stress-test `LintelRebarCalculator.cs`:
   - Bearing length checks: L_bearing >= max(200mm, h_lintel / 2). Test short spans vs long spans.
3. Stress-test `GenericSectionCalculator.cs`:
   - Section cut geometry and callout tags.
4. Stress-test ClosedXML Workbook Generators:
   - Verify sheets 'Mong', 'San', 'Thang' generate genuine OpenXML spreadsheets with valid tables and formulas.
5. Build & Test mandate:
   If running tests, ALWAYS use `--disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`.
6. Verdict:
   Provide an empirical verification verdict: CONFIRMED_CORRECT or DEFECT_FOUND.

DELIVERABLE:
Write your empirical test report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_2\challenge_report.md`
and a concise handoff.md in your working directory.
When complete, notify parent via send_message with your verdict and report path.
