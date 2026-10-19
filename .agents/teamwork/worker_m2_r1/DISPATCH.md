# Dispatch Assignment: worker_m2_r1

- **Role**: Milestone M2 Implementation Worker (teamwork_preview_worker)
- **Working Directory**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_r1\`
- **Milestone**: M2 (P0 Verification: Rollback Honesty, Warning Capture & Measured Geometric Verification)
- **Authoritative User Request**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (inspect `## 2026-10-07T13:49:28Z`).
- **Master Plan**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.

## Mandatory Explorer Reports to Read
1. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_1\report.md` (Rollback Honesty & Warning Capture)
2. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_2\report.md` (QC Metric Cleanup, Step 8 Elimination, Tri-State Status)
3. `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m2_3\report.md` (Measured Geometric Verifier Architecture)

## Implementation Tasks

### Package 1: Rollback Honesty & Warning Capture (Feature F5)
1. In `McpShared/HPRebar.Mcp.Contracts/Messages/ExecuteResult.cs`:
   - Add property `public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();`.
2. In `HPRebar/HPRebar.McpBridge/Service/AutoDismissFailurePreprocessor.cs`:
   - Capture `failure.GetDescriptionText()` into a thread-safe list of warnings instead of discarding them. Expose captured warnings.
3. In `HPRebar/HPRebar.McpBridge/Service/ScriptRunner.cs` (or execution handler):
   - Populate `warnings` into `ExecuteResult`.
4. In `HPBim/src/HPBim.Client/Pipeline/BimPipelineResult.cs` and `StageResult.cs`:
   - Add property `public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();`.
5. In `HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs` & `BimPipelineEngine.cs`:
   - If `plan.Options.DryRun && !execRes.RolledBack`: set `success = false` and set explicit error message `"Dry-run failed: Revit bridge did not confirm transaction rollback (RolledBack == false)."`.
   - If `!plan.Options.DryRun && execRes.RolledBack`: set `success = false` and set explicit error message `"Apply execution failed: Transaction was unexpectedly rolled back by Revit bridge."`.
   - Update `ExecuteAtomicDryRunAsync` and `ExecuteSequentialStagesAsync` to enforce `RolledBack` verification.
   - Propagate `execRes.Warnings` into `BimPipelineResult.Warnings` and stage results.
6. In `HPBim/tests/HPBim.Client.Tests/Mocks/FakeRevitPipeClient.cs` & `HPBim/tests/HPBim.E2E.Tests/Fixtures/E2EFakeRevitPipeClient.cs`:
   - Support simulated `Warnings` and `RolledBack` flags in `SimulateResponse`.
7. Add 9-test suite in `HPBim/tests/HPBim.Client.Tests/Pipeline/DryRunRollbackHonestyTests.cs`.

### Package 2: QC Metric Cleanup, Step 8 Audit & Tri-State Status (Feature F6 Part 1)
1. In `HPBim/src/HPBim.Core/Qc/FoundationAuditModels.cs`:
   - Add `public enum FoundationStepStatus { Passed, Failed, Inconclusive }`.
   - Add `FoundationStepStatus Status` to `FoundationStepResult` with backward-compatible `bool Passed => Status == FoundationStepStatus.Passed;`.
   - Add parameter object `public sealed record FoundationAuditContext(...)` to keep parameter counts <= 4 (Rule M4).
2. Create `HPBim/src/HPBim.Core/Qc/FoundationLedgerAuditor.cs` (~95 lines, C2 <= 300, M3 <= 50):
   - Implement empirical Step 8 audit: check prior steps passed, check ledger exists and is loaded, check active phase is `Foundation`, check footing and pile element counts match ledger, check 0 unready technical groups, check 0 pending RFIs.
3. In `HPBim/src/HPBim.Core/Qc/FoundationQcEngine.cs`:
   - Eliminate hardcoded `true` at Step 8 (`line 259`); delegate to `FoundationLedgerAuditor.AuditStep8`.
   - In `AuditPositions`: if `groundTruthPiles == null && groundTruthFootings == null`, return `FoundationStepStatus.Inconclusive` (Rule 0) instead of false 100% PASS.
   - In `AuditGridsAndLevels`: if expected grids or levels are null/empty, return `FoundationStepStatus.Inconclusive` instead of false PASS.
   - Keep `FoundationQcEngine.cs` <= 260 lines (C2 limit <= 300 lines) and all methods <= 50 lines (M3 limit <= 50 lines).
4. Update `HPBim/tests/HPBim.Core.Tests/FoundationQcEngineTests.cs`:
   - Update tests to supply valid ground-truth and ledger fixtures for authentic 8-step passing tests.
   - Add tests proving that missing/null ground truth returns `Inconclusive` and blocks `AllPassed`.

### Package 3: Measured Geometric Verifier Architecture (Feature F6 Part 2)
1. In `HPBim/src/HPBim.Core/Verification/`:
   - `ActualElementGeometry.cs`: `SourceKey`, `Category`, `Centroid`, `TopElevationMm`, `BottomElevationMm`, `WidthMm`, `HeightMm`, `StartPoint`, `EndPoint`.
   - `GeometricVerificationStatus.cs`: `enum { Verified, Defect, Missing, Inconclusive }`.
   - `ElementGeometricAudit.cs`: `ElementPlanItem Planned`, `ActualElementGeometry? Actual`, `GeometricVerificationStatus Status`, `IReadOnlyList<string> Discrepancies`.
   - `GeometricAuditReport.cs`: summary counts, `IReadOnlyList<ElementGeometricAudit> Audits`, and `bool OverallPassed => TotalPlanned > 0 && VerifiedCount == TotalPlanned && DefectCount == 0 && MissingCount == 0 && InconclusiveCount == 0;`.
   - `MeasuredGeometricEngine.cs`: compares `BimExecutionPlan` against `IReadOnlyList<ActualElementGeometry>` within `CoordinationTolerances`. If any coordinate, elevation, or section dimension exceeds tolerance, marks element as `Defect` and `OverallPassed = false`, even if element count matches 100%.
   - Ensure all files in `HPBim.Core/Verification/` target `netstandard2.0` with ZERO `Autodesk.Revit.*` references.
2. In `HPBim/src/HPBim.Client/Verification/`:
   - `MeasuredGeometricVerifier.cs`: queries element geometry through Revit bridge and invokes `MeasuredGeometricEngine`.
   - `RevitGeometryQueryScriptTemplate.cs` & `MeasuredGeometryJsonParser.cs`.
3. Test suites:
   - Create `HPBim/tests/HPBim.Core.Tests/Verification/MeasuredGeometricEngineTests.cs` (9 unit tests).
   - Create `HPBim/tests/HPBim.Client.Tests/Verification/MeasuredGeometricVerifierTests.cs` (5 tests).

## Clean Code & Governance Rules
- Every method strictly <= 50 lines (M3). Recommended target <= 35 lines.
- Every class/file strictly <= 300 lines (C2). Recommended target <= 270 lines.
- No empty catch blocks without comment explanations.
- No commented-out code or unused debug code.
- Zero regressions on existing 473 tests.


## 2026-10-07T19:05:55Z
You are worker_m2_r1, a teamwork_preview_worker agent.
Your working directory is: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_r1\
The authoritative user request is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (MANDATORY: read this first).
The master project plan is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Your detailed dispatch assignment is at: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_r1\DISPATCH.md.
