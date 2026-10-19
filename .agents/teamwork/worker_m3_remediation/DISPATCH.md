## 2026-10-17T11:21:49Z
You are worker_m3_remediation, the Remediation Worker for Milestone M3 (Phase 4: Foundation, Slab, Stair & Lintel Detailing), Iteration 2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_remediation\
Your parent conversation ID is: a7a2dbbb-6b7d-4279-98cd-1216d8280d80

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

MANDATORY FIRST STEPS:
1. Read the authoritative original request at:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under headers ## 2026-10-16T23:55:10Z and ## 2026-10-17T06:46:41Z)
2. Read the master project scope:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md
3. Read the review report:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_1\review.md
4. Read your task dispatch:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_remediation\DISPATCH.md

YOUR MISSION:
Resolve both Rule M3 method length violations identified by reviewer_m3_1:

1. `HPKata/HPKata.Core/Foundations/Calculators/PileCoordinateGenerator.cs`:
   - `GenerateLayout` currently spans lines 13–95 (83 physical lines).
   - Refactor `GenerateLayout` by extracting private helper methods:
     - e.g., `GeneratePredefinedLayout(...)` or `GenerateSmallGroupPiles(...)` for predefined pile counts (1 to 9).
     - e.g., `GenerateArbitraryGridPiles(...)` for pileCount >= 10.
   - Ensure `GenerateLayout` and each extracted helper method is strictly <= 50 physical lines.
   - Ensure the entire file `PileCoordinateGenerator.cs` remains strictly <= 300 physical lines (Rule C2).
   - Ensure all pile calculations, coordinates, recommended length/width, and centroid invariance remain identical.

2. `HPKata/HPKata.Core.Tests/Stairs/StairRebarCalculatorTests.cs`:
   - `CalculateStairRebar_StandardFlight_GeneratesExpectedItems` spans lines 12–64 (53 physical lines).
   - Refactor by extracting the instantiation of `StairFlightModel` into a private helper method: `private static StairFlightModel CreateStandardFlight()`.
   - Ensure the test method body and helper are each strictly <= 50 physical lines.

3. Build & Test Verification:
   - Build: dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1 (must pass with 0 errors, 0 warnings).
   - Test: dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())" (all 743 tests must pass 100% green).

4. Clean Code Rules Mandate:
   - Max 300 physical lines per file (Rule C2).
   - Max 50 physical lines per method (Rule M3).
   - 0 dead code/TODOs (Rule Q-B1).
   - 0 unannotated empty catch blocks (Rule Q-B2).
   - 304.8 strictly in RevitUnitConverter.cs (Rule R5).

DELIVERABLE:
Write your implementation report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_remediation\changes.md`
and a structured handoff.md in your working directory.
When complete, notify parent via send_message with build/test results and report path.
