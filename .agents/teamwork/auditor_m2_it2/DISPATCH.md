# Dispatch Assignment: auditor_m2_it2

- **Role**: Forensic Integrity Auditor (teamwork_preview_auditor)
- **Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m2_it2\`
- **Milestone**: M2 Iteration 2 (P0 Verification Forensic Integrity Audit)
- **Authoritative User Request**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (MANDATORY: read this first).
- **Master Plan**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
- **Worker Remediation Report & Handoff**:
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_it2_r1\report.md`
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_it2_r1\handoff.md`

## Your Mission:
Perform forensic integrity verification of all code changes delivered by `worker_m2_it2_r1`:
1. Clean Code Compliance:
   - Rule C2: Measure every modified file to confirm length <= 300 lines (e.g. `BimPipelineEngine.cs` at 212 lines, `FoundationQcEngineTests.cs` at 260 lines).
   - Rule M3: Measure every method in modified files to confirm length strictly <= 50 lines (specifically `ExecuteSequentialStagesAsync` and its sub-helpers, and `FoundationQcEngine_FullAudit_WithValidGroundTruthAndLedger_PassesAll8Steps`).
   - Attestation Veracity: Verify that worker's line-count attestation in `report.md` matches disk reality.
2. Static Analysis:
   - Verify zero hardcoded test results, zero dummy facades, zero empty catches without comments, zero commented-out code.
   - Verify `CheckDimension` in `MeasuredGeometricEngine.ProfileGrid.cs:125` records explicit discrepancy on null actual dimension (Rule 0).
3. Build & Test Verification:
   - Run `dotnet build HPBim/HPBim.slnx` -> 0 errors, 0 warnings.
   - Run `dotnet test HPBim/HPBim.slnx` -> 100% tests pass (521/521).
   - Run `dotnet test McpShared/McpShared.slnx` -> 100% tests pass.
4. Deliver forensic audit report in `report.md` and binary verdict (`CLEAN` or `INTEGRITY_VIOLATION`) in `handoff.md`, then send a message back to parent.

## 2026-10-07T19:59:49Z
[Message] timestamp=2026-10-07T19:59:49Z sender=44938268-52fa-4afa-af1c-06dc2c4f2512 priority=MESSAGE_PRIORITY_HIGH content=You are auditor_m2_it2, a teamwork_preview_auditor agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m2_it2\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Your detailed dispatch assignment is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m2_it2\DISPATCH.md.
The worker remediation report and handoff are at:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_it2_r1\report.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_it2_r1\handoff.md
