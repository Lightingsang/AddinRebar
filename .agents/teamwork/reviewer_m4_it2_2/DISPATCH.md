# Dispatch Instructions — reviewer_m4_it2_2

## Identity & Context
- **Agent**: `reviewer_m4_it2_2`
- **Archetype**: `teamwork_preview_reviewer`
- **Role**: `Robustness & Clean Code Reviewer (M4 It 2)`
- **Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_it2_2\`
- **Authoritative User Request**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (MANDATORY: read this first).
- **Project Scope Document**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
- **Worker Report & Handoff**:
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\report.md`
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\handoff.md`

## Mission
1. Independently measure and verify Clean Code compliance across all 24 files in Milestone M4:
   - Rule C2: Confirm all 24 files are strictly $\le 300$ physical lines.
   - Rule M3: Confirm every method across all 24 files is strictly $\le 50$ physical lines.
   - Verify that `ExtensibleStorageQueryScriptTemplate.cs` max method is $\le 28$ lines.
   - Verify that `BimPlanRunnerScriptTemplate.cs` max method is $\le 35$ lines.
2. Verify Robustness & Rule 0:
   - Verify defensive guard in `ExtensibleStorageReceiptReader.cs:119` against corrupted array items.
   - Verify zero guessing in `ReconcileService.cs` (OutcomeUnknown transitions only when receipt is confirmed or absent).
3. Run verification tools:
   - `python scratch/verify_m4_clean_code.py`
   - `dotnet run --project scratch/RoslynSyntaxAuditor/RoslynSyntaxAuditor.csproj`
   - `dotnet build HPBim/HPBim.slnx`
   - `dotnet test HPBim/HPBim.slnx`
4. Deliver `report.md` and explicit verdict (`APPROVE` or `REQUEST_CHANGES`) in `handoff.md`, then send a message back to parent.

## 2026-10-07T22:10:36Z
You are reviewer_m4_it2_2, a teamwork_preview_reviewer agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_it2_2\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Your detailed dispatch assignment is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_it2_2\DISPATCH.md.
The worker remediation report and handoff are at:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\report.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_r1\handoff.md

Your mission:
1. Independently measure and verify Clean Code compliance across all 24 files in Milestone M4:
   - Rule C2: Confirm all 24 files are strictly <= 300 physical lines.
   - Rule M3: Confirm every method across all 24 files is strictly <= 50 physical lines.
   - Verify that ExtensibleStorageQueryScriptTemplate.cs max method is <= 28 lines.
   - Verify that BimPlanRunnerScriptTemplate.cs max method is <= 35 lines.
2. Verify Robustness & Rule 0:
   - Verify defensive guard in ExtensibleStorageReceiptReader.cs:119 against corrupted array items.
   - Verify zero guessing in ReconcileService.cs (OutcomeUnknown transitions only when receipt is confirmed or absent).
3. Run verification tools:
   - python scratch/verify_m4_clean_code.py
   - dotnet run --project scratch/RoslynSyntaxAuditor/RoslynSyntaxAuditor.csproj
   - dotnet build HPBim/HPBim.slnx
   - dotnet test HPBim/HPBim.slnx
4. Deliver report.md and explicit verdict (APPROVE or REQUEST_CHANGES) in handoff.md, then send a message back to parent.
