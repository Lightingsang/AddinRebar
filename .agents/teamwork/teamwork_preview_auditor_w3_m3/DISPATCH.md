## 2026-10-09T16:53:49Z
You are Forensic Auditor for Milestone 3 (M3: Tách biệt Persistent Document Identity khỏi Dynamic DocumentVersion GUID).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_auditor_w3_m3
Your parent conversation ID is: 8b5286ec-5528-473c-b9fe-c3867bdf1bf8

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under header ## 2026-10-09T14:54:56Z).
2. Read PROJECT.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md
3. Read Worker M3 handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_w3_m3_1\handoff.md

Forensic Audit Objectives:
- Perform strict integrity analysis on all files touched in M3:
  - HPBim/src/HPBim.Core/Models/DocSnapshot.cs
  - HPBim/src/HPBim.Client/Recovery/ReconcileService.cs
  - HPBim/src/HPBim.Client/Recovery/ReconcileService.DocSnapshot.cs
  - HPBim/tests/HPBim.Client.Tests/Recovery/DocSnapshotReconcileTests.cs
- Verify NO CHEATING:
  - No dummy/facade implementations or fake boolean returns.
  - Verify genuine static identity comparison and dynamic VersionGuid decoupling.
- Clean Code static analysis:
  - Every file <= 300 lines (Rule C2).
  - Every method <= 50 lines (Rule M3).
  - Zero code comments (Rule Q-B1).
  - Zero empty catches (Rule Q-B2).
  - Pure netstandard2.0 without Autodesk references.
- Run tests: dotnet test HPBim/tests/HPBim.Client.Tests/HPBim.Client.Tests.csproj

Deliverables:
- Write audit_report.md and handoff.md in your working directory.
- handoff.md must conclude with a clear binary verdict: CLEAN or INTEGRITY VIOLATION.
- Send completion message to parent (8b5286ec-5528-473c-b9fe-c3867bdf1bf8).
