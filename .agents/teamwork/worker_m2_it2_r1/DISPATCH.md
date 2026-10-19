# Dispatch Assignment: worker_m2_it2_r1

- **Role**: Milestone M2 Iteration 2 Remediation Worker (teamwork_preview_worker)
- **Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_it2_r1\`
- **Milestone**: M2 (P0 Verification Remediation)
- **Authoritative User Request**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (inspect `## 2026-10-07T13:49:28Z`).
- **Master Plan**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
- **Reviewer Feedback Report**:
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m2_2\report.md`
  - `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m2_2\handoff.md`

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Exclusive Write Ownership
- `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.cs`
- `HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.ProfileGrid.cs`
- `HPBim/tests/HPBim.Core.Tests/FoundationQcEngineTests.cs`
- `HPBim/tests/HPBim.Core.Tests/Verification/MeasuredGeometricEngineTests.cs`

## Remediation Tasks to Execute:
1. **[Rule M3 Violation] `BimPipelineEngine.ExecuteSequentialStagesAsync`** (spans lines 127–195 in `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.cs`, currently 69 lines):
   - Refactor and extract cohesive private helper methods (e.g. `ExecuteStageStepAsync`, `HandleStageExecutionResult`) so that `ExecuteSequentialStagesAsync` and all extracted helper methods are strictly <= 35 lines.
   - Ensure `BimPipelineEngine.cs` total file length stays comfortably <= 250 lines (Rule C2 <= 300).
2. **[Rule M3 Violation in Tests] `FoundationQcEngineTests.cs`** (spans lines 221–273, currently 53 lines):
   - In `FoundationQcEngine_FullAudit_WithValidGroundTruthAndLedger_PassesAll8Steps`, extract test setup helper `CreateValidFoundationAuditContext()` so the test method is strictly <= 35 lines.
3. **[Rule 0 / Adversarial Fix] `MeasuredGeometricEngine.ProfileGrid.cs:125` (`CheckDimension`)**:
   - Currently:
     ```csharp
     if (expectedMm <= 0 || !actualMm.HasValue) return 0.0;
     ```
   - When `expectedMm > 0` but `!actualMm.HasValue` (parameter unmeasurable or missing from actual Revit geometry), do NOT return 0.0 without discrepancy!
   - Log explicit discrepancy:
     ```csharp
     if (!actualMm.HasValue)
     {
         if (expectedMm > 0)
         {
             discrepancies.Add($"{dimName}: Expected {expectedMm:F1}mm, but actual parameter was unmeasurable (null)");
             return expectedMm;
         }
         return 0.0;
     }
     ```
   - This ensures that if actual dimensions cannot be measured, the element is flagged with a discrepancy rather than falsely passing as `Verified` (strictly enforcing Rule 0).
   - Add a unit test in `MeasuredGeometricEngineTests.cs` verifying this behavior.
4. **Automated Line-Count Verification & Truthful Attestation**:
   - Run an automated script/parser measuring EVERY method across touched files.
   - Confirm that 100% of methods across all touched files are strictly <= 50 lines (with exact measured line numbers).
   - Document exact measurements in `report.md` and `handoff.md`.
5. **Build and Test Verification**:
   - Run `dotnet build HPBim/HPBim.slnx` (0 warnings, 0 errors).
   - Run `dotnet test HPBim/HPBim.slnx` (100% green).
   - Run `dotnet test McpShared/McpShared.slnx` (100% green).

## 2026-10-07T19:48:47Z
You are worker_m2_it2_r1, a teamwork_preview_worker agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_it2_r1\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Your detailed dispatch assignment is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_it2_r1\DISPATCH.md.
The gate status and reviewer feedback are at:
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_1\GATE_STATUS.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m2_2\report.md
- G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m2_2\handoff.md
