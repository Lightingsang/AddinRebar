## 2026-10-09T19:13:13Z
You are Worker W3 M5 Iteration 2 for HPBim Wave 3 Remediation Milestone 5 Remediation.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_w3_m5_it2
Your parent conversation ID is: 8b5286ec-5528-473c-b9fe-c3867bdf1bf8

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under header ## 2026-10-09T14:54:56Z).
2. Read Challenger 2 findings:
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_challenger_w3_m5_2\challenge_report.md
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_challenger_w3_m5_2\handoff.md
3. Read Worker M5 handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_w3_m5_1\handoff.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

File Write Ownership:
You EXCLUSIVELY own:
- HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.cs
- HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.Structural.cs
- HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.ProfileGrid.cs
- HPBim/tests/HPBim.Core.Tests/Verification/StandardizedGeometricMeasurementR5Tests.cs
Do NOT modify files outside this scope!

Remediation Tasks (Fix 4 boundary defects found by Challenger 2):
1. Planned Geometry Null Check:
   - In AuditItem or HasRequiredMeasurements, verify that item.Geometry != null. If item.Geometry is null, return GeometricVerificationStatus.Inconclusive with reason: $"Rule 0: ElementPlanItem '{item.Mark}' lacks planned geometry specification."
2. Mismatched Geometry Spec Type Check:
   - In HasRequiredMeasurements or category audit dispatch, verify that item.Geometry matches the expected specification type:
     - Column, Pile -> PointGeometrySpec
     - Beam, GradeBeam, Wall -> LineGeometrySpec
     - Footing, Slab -> ProfileGeometrySpec
     - Level -> LevelGeometrySpec
     - Grid -> GridGeometrySpec
     If mismatched, return Inconclusive with a clear Rule 0 reason (do NOT default deltas to 0 and falsely mark Verified).
3. IEEE 754 NaN and Infinite Value Safeguard:
   - In HasRequiredMeasurements and tolerance evaluation, verify that measurements are finite numbers (not double.IsNaN and not double.IsInfinity). If actual values contain NaN or Infinity (e.g. Centroid, TopElevationMm, WidthMm), reject as invalid measurement returning Inconclusive.
4. Degenerate 3-Point Profile Closed Loop Crash:
   - In ComputeProfileCentroid (MeasuredGeometricEngine.ProfileGrid.cs), before constructing new Polygon2D(pts), verify that the distinct non-closing vertices count is >= 3. If distinct count < 3 (such as [P0, P1, P0]), do NOT throw an unhandled ArgumentException crash; compute the simple average centroid of the points or handle gracefully returning Inconclusive.

Clean Code Governance (MANDATORY):
- File length <= 300 lines (Rule C2). Ensure MeasuredGeometricEngine.Structural.cs remains strictly <= 300 lines (was 254 lines).
- Method length <= 50 lines (Rule M3).
- ZERO code comments (Rule Q-B1).
- ZERO unannotated empty catches (Rule Q-B2).
- Pure netstandard2.0 in HPBim.Core (zero Autodesk references).

Verification:
- Run tests: dotnet test HPBim/tests/HPBim.Core.Tests/HPBim.Core.Tests.csproj
- Run tests: dotnet test HPBim/HPBim.slnx
- Ensure 100% tests PASS across the solution with 0 regressions.

Deliverables:
- Write report.md and handoff.md in your working directory.
- Send completion message to parent (8b5286ec-5528-473c-b9fe-c3867bdf1bf8).
