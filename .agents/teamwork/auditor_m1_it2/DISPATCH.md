# Dispatch: auditor_m1_it2 (M1 Iteration 2 Forensic Auditor)

## Mission
You are `auditor_m1_it2`, a `teamwork_preview_auditor` agent.
Your working directory is: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m1_it2\`.
The authoritative user request is at: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`.
The master project plan is at: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
The worker remediation report is at:
- `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_it2_r1\report.md`
- `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_it2_r1\handoff.md`

## Your Forensic Integrity Audit Assignment:
1. Verify static integrity:
   - Check lines 379–385 of `BimModelSchemaValidator.cs`: verify that NO hardcoded fixture marks (`"M1_M2"`, `"GM1_GM2"`, `"BT1"`, `"DCT_DCN"`, etc.) remain anywhere in source code.
   - Verify zero fake mocks, zero empty catches without reason, zero commented-out code.
   - Verify zero heuristic `.FirstOrDefault()` guessing in `BimPlanRunnerScriptTemplate.cs`.
2. Verify Clean Code compliance:
   - Every file in `HPBim/src/` must be $\le 300$ lines (C2).
   - Every method must be $\le 50$ lines (M3).
3. Run verification commands:
   - `dotnet build HPBim/HPBim.slnx`
   - `dotnet test HPBim/HPBim.slnx`
4. Deliver forensic audit report in `report.md` and binary verdict (`CLEAN` or `INTEGRITY_VIOLATION`) in `handoff.md`, then send message to parent.

## 2026-10-07T18:10:34Z
You are auditor_m1_it2, a teamwork_preview_auditor agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m1_it2\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Your detailed dispatch assignment is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m1_it2\DISPATCH.md.
The worker remediation report is at:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_it2_r1\report.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_it2_r1\handoff.md

Your mission:
Perform forensic integrity verification of all code changes delivered by worker_m1_it2_r1:
1. Static analysis: Verify that lines 379–385 of BimModelSchemaValidator.cs contain NO hardcoded fixture marks ("M1_M2", "GM1_GM2", etc.). Verify zero fake mocks, zero empty catches without reason comments, zero commented-out code, and zero .FirstOrDefault() guessing in BimPlanRunnerScriptTemplate.cs.
2. Clean Code compliance: Verify that EVERY file in HPBim/src/ is <= 300 lines (C2) and every method is <= 50 lines (M3).
3. Runtime / build verification: Run `dotnet build HPBim/HPBim.slnx` and `dotnet test HPBim/HPBim.slnx`.
4. Deliver forensic audit report in report.md and binary verdict (CLEAN or INTEGRITY_VIOLATION) in handoff.md, then send message to orchestrator.
