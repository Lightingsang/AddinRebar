## 2026-10-09T19:37:42Z
You are Worker E2E Final Certification for HPBim Wave 3 Remediation.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_w3_final
Your parent conversation ID is: 8b5286ec-5528-473c-b9fe-c3867bdf1bf8

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under header ## 2026-10-09T14:54:56Z).
2. Read PROJECT.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Certification Tasks:
1. Full Repository Test Suite Execution:
   - Run tests for HPBim: dotnet test HPBim/HPBim.slnx
   - Run tests for HPRebar: dotnet test HPRebar/HPRebar.Core.Tests/HPRebar.Core.Tests.csproj and dotnet test HPRebar/HPRebar.Mcp.Server.Tests/HPRebar.Mcp.Server.Tests.csproj
   - Run tests for McpShared: dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj and dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj
   - Run Challenger harnesses:
     - dotnet run --project tests/m5_w3_challenger_harness/M5W3ChallengerHarness.csproj
     - dotnet run --project tests/m5_w3_stress_harness/M5W3StressHarness.csproj
   - Document all test counts, run durations, and verify 0 failures and 0 regressions. Total must be >= 3,779 tests.

2. Clean Code Static Metrics Across All Wave 3 Modified Files:
   - Check line counts (Rule C2 <= 300) for all touched files:
     - HPBim/src/HPBim.Client/Connection/CrossProcessFileLock.cs
     - HPBim/src/HPBim.Client/Connection/IntentJournal.cs
     - HPBim/src/HPBim.Client/Recovery/ReconcileService.cs
     - HPBim/src/HPBim.Client/Recovery/ReconcileService.ApplyReceipt.cs
     - HPBim/src/HPBim.Client/Recovery/ReconcileService.DocSnapshot.cs
     - HPBim/src/HPBim.Core/Models/DocSnapshot.cs
     - HPBim/src/HPBim.Core/Coordination/CompositeSourceKeyJsonConverter.cs
     - HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.cs
     - HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.ElementRecords.Validation.cs
     - HPBim/src/HPBim.Client/Pipeline/BimPipelineEngine.PlanExecution.cs
     - HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.cs
     - HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.Structural.cs
     - HPBim/src/HPBim.Core/Verification/MeasuredGeometricEngine.ProfileGrid.cs
     - HPBim/tests/HPBim.Core.Tests/Verification/StandardizedGeometricMeasurementR5Tests.cs
   - Check method lengths (Rule M3 <= 50).
   - Check commented-out code (Rule Q-B1: 0).
   - Check catch blocks (Rule Q-B2: 0 unannotated empty catches).
   - Check project purity: HPBim.Core targets netstandard2.0 with zero Autodesk references.

Deliverables:
- Write e2e_verification_report.md and handoff.md in your working directory.
- Send completion message to parent (8b5286ec-5528-473c-b9fe-c3867bdf1bf8).
