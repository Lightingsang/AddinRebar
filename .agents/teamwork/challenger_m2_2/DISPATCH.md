# DISPATCH — 2026-10-17T09:27:00Z

## Task Assignment for challenger_m2_2 (Shear Wall & Core Stress Tester)

You are **challenger_m2_2**, performing adversarial stress-testing and empirical verification on Milestone M2 (Phase 3: Column & Shear Wall Shop Drawing).
Your working directory is: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m2_2\`
Your parent conversation ID is: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

### MANDATORY REFERENCES:
1. `ORIGINAL_REQUEST.md` at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`.
2. Master Project Scope at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`.
3. Worker's Changes & Handoff:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_column_wall\changes.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_column_wall\handoff.md`

### YOUR MISSION:
Empirically stress-test the Shear Wall and Core Wall domain mathematics, ETABS pier tables, and Revit wall services:
1. Stress-test `WallBoundaryCalculator.cs`:
   - Boundary zone length: verify b_bz >= max(0.15 * Lw, 1.5 * tw, 400mm).
   - Boundary vertical steel ratio rho_bz >= 1.0%, confinement tie spacing s_bz <= 100mm.
2. Stress-test `WallWebMeshCalculator.cs`:
   - Double-curtain vertical and horizontal bar arrays across wall thickness tw >= 150mm.
   - Cross-tie density (>= 4 ties/m^2) and U-bar end cap lap lengths (>= 30d).
3. Stress-test `WallOpeningTrimmerCalculator.cs`:
   - 45 deg diagonal crack-control trimmer bars at 4 opening corners (length >= 2 * L_anc).
   - Extra jamb vertical bars and extra header/sill horizontal bars.
4. Stress-test `EtabsWallImportService.cs` and `EtabsWallTableMapper.cs`:
   - Malformed/messy pier tables, missing columns, comma/tab separators, zero/negative values.
5. Stress-test `WallPlanScheduleWorkbookGenerator.cs`:
   - ClosedXML workbook generation for sheet 'Vach', verifying table formatting and bar schedule aggregations.
6. Build & Test mandate:
   If running tests, ALWAYS use `--disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`.
7. Verdict:
   Provide an empirical verification verdict: **CONFIRMED_CORRECT** or **DEFECT_FOUND**.

DELIVERABLE:
Write your empirical test report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m2_2\challenge_report.md`
and a concise handoff.md in your working directory.
When complete, notify parent via send_message with your verdict and report path.
