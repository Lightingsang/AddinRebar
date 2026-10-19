# DISPATCH — 2026-10-17T09:27:00Z

## Task Assignment for reviewer_m2_2 (M2 Functional & Build/Test Reviewer)

You are **reviewer_m2_2**, reviewing Milestone M2 (Phase 3: Column & Shear Wall Shop Drawing).
Your working directory is: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m2_2\`
Your parent conversation ID is: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

### MANDATORY REFERENCES:
1. `ORIGINAL_REQUEST.md` at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`.
2. Master Project Scope at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`.
3. Worker's Changes & Handoff:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_column_wall\changes.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_column_wall\handoff.md`

### YOUR MISSION:
Review the Milestone M2 implementation focusing on Functional Completeness & Build/Test Verification:
1. Functional Verification:
   - Verify all 10 AutoCAD commands and aliases in `GoiLenh_HPKata.lsp` (`vc`, `vcs`, `raicot`, `dimcot`, `tkcot`, `loadcolumn`, `vv`, `vvs`, `vlv`, `loadwall`).
   - Verify genuine AutoCAD controllers without simulation stubs: ModelSpace block placement, dynamic takeoff table creation, DWG XRecord serialization (`KATA_COLUMN_DATA`, `KATA_WALL_DATA`).
   - Verify Revit column and wall rebar generators (`ColumnRebarGenerator.cs`, `WallRebarGenerator.cs`) with genuine vertical bar curves, crank bends, and closed confinement hoops.
   - Verify ClosedXML workbook generator for sheets `Cot` and `Vach`.
2. Build & Test Verification:
   - Run: `dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1` (must pass with 0 errors, 0 warnings).
   - Run: `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"` (all 634 tests must pass 100% green).
3. Verdict:
   Provide an explicit verdict: **APPROVE** or **REQUEST_CHANGES**.

DELIVERABLE:
Write your review report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m2_2\review.md`
and a concise handoff.md in your working directory.
When complete, notify parent via send_message with your verdict and report path.
