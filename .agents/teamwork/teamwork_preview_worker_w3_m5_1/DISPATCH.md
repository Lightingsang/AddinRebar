## 2026-10-09T18:32:52Z
You are Worker W3 M5 for HPBim Wave 3 Remediation Milestone 5: Standardized Geometric Measurement & Acceptance Verification (R5).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_w3_m5_1
Your parent conversation ID is: 8b5286ec-5528-473c-b9fe-c3867bdf1bf8

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under header ## 2026-10-09T14:54:56Z).
2. Read Explorer 3 findings:
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_w3_survey_3\survey_report.md (specifically Section 1: R5: Standardized Geometric Measurement & Acceptance Verification)
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_w3_survey_3\handoff.md
3. Read PROJECT.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

File Write Ownership:
You EXCLUSIVELY own:
- HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.cs
- HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.Structural.cs
- HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.ProfileGrid.cs
- HPBim/src/HPBim.Client/Verification/RevitGeometryQueryScriptTemplate.cs
- HPBim/src/HPBim.Cli/Commands/VerifyCommand.cs
- Tests in HPBim/tests/HPBim.Core.Tests/Verification/
- Tests in HPBim/tests/HPBim.Client.Tests/Verification/
Do NOT modify files outside this scope!

Task Requirements (R5):
1. TargetUniqueId Direct Geometry Querying:
   - In RevitGeometryQueryScriptTemplate.cs: Check item.Str("targetUniqueId", string.Empty). If non-empty, query element directly via doc.GetElement(targetUniqueId) before falling back to category collector search by mark.
   - In MeasuredGeometricEngine.cs (FindActual): Prioritize item.TargetUniqueId lookup in actuals dictionary (if item.TargetUniqueId is non-empty and actuals.TryGetValue(item.TargetUniqueId, out var actByUid), return actByUid).
   - In VerifyCommand.cs: Ensure TargetUniqueIds from ledger are populated on plan items when verifying.

2. Pile Cutoff Elevation Verification:
   - In MeasuredGeometricEngine.Structural.cs (HasRequiredPileMeasurements): Enforce actual.TopElevationMm.HasValue == true. If TopElevationMm is null, return false with missingReason explaining that Cutoff Elevation (TopElevationMm) is required.
   - When actual.TopElevationMm is missing, AuditItem must return GeometricVerificationStatus.Inconclusive.
   - In AuditPile: verify cutoff elevation against ptSpec.OffsetZMm within _tolerances.ElevationMm.
   - In RevitGeometryQueryScriptTemplate.cs: Extract pile cutoff elevation from bounding box Max.Z or location point Z into record["topElevationMm"].

3. Profile / Slab Outline & Thickness Verification:
   - In RevitGeometryQueryScriptTemplate.cs: For Floor / profile objects, export centroid and bounding box dimensions (widthMm, heightMm).
   - In MeasuredGeometricEngine.ProfileGrid.cs (AuditProfileStructure): Compare actual.Centroid against profile polygon centroid within _tolerances.PlanPositionMm, and compare actual.ThicknessMm against profSpec.ThicknessMm within _tolerances.SectionDimensionMm.
   - In HasRequiredProfileMeasurements: Enforce top elevation, thickness (when profSpec.ThicknessMm > 0), and centroid.

4. Elimination of Generic Auto-Verify Flaw:
   - In MeasuredGeometricEngine.cs (AuditGeneric): Change Status = GeometricVerificationStatus.Verified to Status = GeometricVerificationStatus.Inconclusive. Set DefectReason explaining that unmeasured categories cannot be auto-verified without empirical measurements under Rule 0.
   - In HasRequiredMeasurements: unhandled categories return false with missingReason.

5. Verification & Tests:
   - Create/update unit tests in HPBim.Core.Tests and HPBim.Client.Tests verifying:
     - Pile lacking cutoff elevation returns Inconclusive.
     - Pile with valid cutoff elevation returns Verified (or Defect if out of tolerance).
     - Generic / unmeasured category returns Inconclusive.
     - Slab / profile outline and thickness verification.
     - TargetUniqueId direct query.
   - Run tests: dotnet test HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj
   - Run tests: dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj
   - Run full HPBim test suite: dotnet test HPBim/HPBim.slnx
   - Ensure 100% tests PASS across the solution with 0 regressions.

6. Clean Code Governance (MANDATORY):
   - File length <= 300 lines (Rule C2). Watch out: MeasuredGeometricEngine.Structural.cs is already at ~248 lines. Keep it strictly <= 300 lines.
   - Method length <= 50 lines (Rule M3).
   - ZERO code comments (Rule Q-B1).
   - ZERO unannotated empty catches (Rule Q-B2).
   - Pure netstandard2.0 in HPBim.Core (zero Autodesk references).

Deliverables:
- Write report.md and handoff.md in your working directory.
- Send completion message to parent (8b5286ec-5528-473c-b9fe-c3867bdf1bf8).

## 2026-10-09T18:50:36Z
**Context**: HPBim Wave 3 Remediation Milestone 5 (R5)
**Content**: Yes, approved. Your File Write Ownership is hereby expanded to include:
1. `HPBim/tests/HPBim.Core.Tests/Adversarial/MeasuredGeometricVerifierAdversarialCoverageTests.cs`
2. `HPBim/tests/HPBim.E2E.Tests/Tiers/Tier2_BoundaryValueAnalysisTests.cs`

Specifically update the mock pile actuals in those two test cases to include `TopElevationMm = 0.0` (matching the planned `OffsetZMm = 0.0`) so that the pile satisfies `HasRequiredPileMeasurements` and properly tests the center boundary thresholds. Ensure all Clean Code governance rules (0 comments, files <= 300 lines, etc.) are strictly maintained.
**Action**: Apply the updates, complete remaining R5 implementation and verification, run full test suite `dotnet test HPBim/HPBim.slnx`, and submit your handoff report.
