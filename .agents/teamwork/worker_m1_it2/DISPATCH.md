## 2026-10-08T16:09:56Z
You are worker_m1_it2 for Milestone M1 Iteration 2 Implementation in HPBim Wave 2 Remediation.
Working directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_it2\
Parent Conversation ID: e0aba3da-bb9f-4e02-be57-11f1cc948914
Original Request: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (READ under header ## 2026-10-08T15:03:54Z)
Forensic Audit Report: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m1_rem_1\handoff.md
Explorer 1 Report: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_it2_1\report.md
Explorer 2 Report: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_it2_2\report.md
Explorer 3 Report: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_it2_3\report.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Scope & Exclusively Owned Files:
1. `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs`
2. `HPBim/tests/HPBim.Client.Tests/Pipeline/BimPipelineEngineRecordReconciliationTests.cs` (and partial class if split)
3. `HPBim/tests/HPBim.Client.Tests/Pipeline/EmpiricalChallengerM1Tests_Stress.cs`

Tasks to Implement:
1. Refactor `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs`:
   - Fix line 167 condition: change `rec.TargetUniqueId != null && string.IsNullOrWhiteSpace(rec.TargetUniqueId)` to `string.IsNullOrWhiteSpace(rec.TargetUniqueId)`. When a record has status `Created` or `Updated`, a null or whitespace `TargetUniqueId` MUST trigger `ERR_MISSING_TARGET_UNIQUE_ID`.
   - Decompose `ReconcileSingleItem` (currently 72 lines) into smaller helper methods (following the design in explorer_m1_it2_1/report.md and explorer_m1_it2_3/report.md) such that EVERY method in the file is strictly <= 35 lines (comfortably below the 50-line Rule M3 ceiling).
   - Ensure `BimPipelineEngine.ElementRecords.cs` total length is <= 280 lines (comfortably below the 300-line Rule C2 ceiling).
   - Ensure 0 commented-out code (Rule Q-B1) and 0 unannotated catch blocks (Rule Q-B2).

2. Test updates:
   - In `BimPipelineEngineRecordReconciliationTests.cs`, add a unit test `Reconciliation_CreatedRecordWithOmittedTargetUniqueIdProperty_FailsIntegrityCheck` (where `targetUniqueId` property is completely omitted from the JSON payload, yielding null `TargetUniqueId`).
   - If `BimPipelineEngineRecordReconciliationTests.cs` threatens to exceed 300 lines, split into partial classes (e.g. `BimPipelineEngineRecordReconciliationTests.cs` and `BimPipelineEngineRecordReconciliationTests.SkipAndRollback.cs`) so that each file is <= 250 lines.
   - In `EmpiricalChallengerM1Tests_Stress.cs` lines 96-97 (`Stress4`), add `"targetUniqueId": "uid-c1"` and `"targetUniqueId": "uid-c2"` to the mock C1 and C2 records so they do not falsely fail the null check.

3. Verification:
   - Run `dotnet build HPBim/HPBim.slnx` -> 0 errors, 0 warnings.
   - Run `dotnet test HPBim/HPBim.slnx` -> 100% tests pass (all 877+ tests).
   - Measure line counts and method lengths with PowerShell.
   - Document results in `handoff.md` and send completion message to Parent.
