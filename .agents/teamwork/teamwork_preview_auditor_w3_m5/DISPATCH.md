## 2026-10-09T19:00:48Z

You are Forensic Auditor for Milestone 5 (M5: Standardized Geometric Measurement & Acceptance Verification).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_auditor_w3_m5
Your parent conversation ID is: 8b5286ec-5528-473c-b9fe-c3867bdf1bf8

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under header ## 2026-10-09T14:54:56Z).
2. Read PROJECT.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md
3. Read Worker M5 handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_w3_m5_1\handoff.md

Forensic Audit Objectives:
- Perform strict integrity analysis on all files touched in M5:
  - HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.cs
  - HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.Structural.cs
  - HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.ProfileGrid.cs
  - HPBim/src/HPBim.Client/Verification/RevitGeometryQueryScriptTemplate.cs
  - HPBim/src/HPBim.Cli/Commands/VerifyCommand.cs
  - HPBim/tests/HPBim.Core.Tests/Verification/StandardizedGeometricMeasurementR5Tests.cs
  - HPBim/tests/HPBim.Client.Tests/Verification/StandardizedGeometricQueryR5Tests.cs
- Verify NO CHEATING:
  - Verify genuine centroid, polygon, and cutoff elevation calculations (no fake shortcuts or hardcoded pass booleans).
  - Verify genuine TargetUniqueId lookup prioritization.
  - Verify genuine Inconclusive return on unmeasured categories.
- Clean Code static analysis:
  - Every file <= 300 lines (Rule C2).
  - Every method <= 50 lines (Rule M3).
  - Zero code comments (Rule Q-B1).
  - Zero empty catches (Rule Q-B2).
  - Pure netstandard2.0 in HPBim.Core without Autodesk references.
- Run tests: dotnet test HPBim/HPBim.slnx

Deliverables:
- Write audit_report.md and handoff.md in your working directory.
- handoff.md must conclude with a clear binary verdict: CLEAN or INTEGRITY VIOLATION.
- Send completion message to parent (8b5286ec-5528-473c-b9fe-c3867bdf1bf8).
