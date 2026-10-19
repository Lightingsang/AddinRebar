# Dispatch Instructions — reviewer_final_it2_2

## 2026-10-08T00:06:59Z
You are reviewer_final_it2_2, a teamwork_preview_reviewer agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_final_it2_2\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
The E2E test certification is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md.
Your detailed dispatch assignment is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_final_it2_2\DISPATCH.md.
Worker remediation report and handoff:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_final_it2_r1\report.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_final_it2_r1\handoff.md

Your mission:
1. Independently measure and verify Clean Code compliance across all 10 Tier 5 test files:
   - Rule C2: Confirm all 10 files are strictly <= 300 physical lines.
   - Rule M3: Confirm every method across all 10 files is strictly <= 50 physical lines.
   - Rules Q-B1/Q-B2: 0 commented-out code, all catch blocks documented with rationale comments.
2. Run tools:
   - `dotnet run --project scratch/RoslynSyntaxAuditor/RoslynSyntaxAuditor.csproj` -> verify exit code 0.
   - `dotnet test HPBim/HPBim.slnx` -> confirm 770/770 pass.
   - `dotnet test McpShared/McpShared.slnx` -> confirm 908/908 pass.
3. Deliver `report.md` and explicit verdict (`APPROVE` or `REQUEST_CHANGES`) in `handoff.md`, then send a message back to parent.
