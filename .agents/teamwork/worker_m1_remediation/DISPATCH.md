## 2026-10-17T07:49:57Z

You are worker_m1_remediation, the Remediation Worker for Milestone M1 (Phase 2: Beam Shop Drawing & ETABS Integration), Iteration 2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_remediation\
Your parent conversation ID is: a7a2dbbb-6b7d-4279-98cd-1216d8280d80

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

MANDATORY FIRST STEPS:
1. Read the authoritative original request at:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under header ## 2026-10-17T06:46:41Z)
2. Read the master project scope at:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md
3. Read the DEAD ENDS log at:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\DEAD_ENDS.md
4. Read the full evidence report from Reviewer 2:
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m1_2\review.md
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m1_2\handoff.md
5. Read the remediation strategy plan:
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_remediation\remediation_plan.md
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_remediation\handoff.md

YOUR MISSION:
Execute the remediation plan to eliminate all integrity violations and defects:

1. Revit Framing Rebar Geometry Fix:
   - In `HPKata/HPKata.Revit/Services/FramingRebarGenerator.cs` (and helper `FramingRebarGeometryCalculator.cs` if needed):
     - Correct bottom bar elevation to `startPt.Z - heightFt + coverFt` (so it resides inside the bottom cover of the beam, NOT above the top face).
     - Implement genuine multi-bar transverse distribution across clear width $(b - 2c)$.
     - Generate closed rectangular stirrup hoop curves along clear spans using `BeamStirrupZoneCalculator` at support ($L/4$) and midspan ($L/2$) spacings.
2. AutoCAD Controllers Remediation:
   - In `HPKata/HPKata.AutoCad/Controllers/` (`BeamTakeoffController.cs`, `BeamPersistenceController.cs`, `BeamEtabsController.cs`):
     - Remove ALL hardcoded literal strings/rows (e.g. `Cached 12 continuous beam runs`, `Identified 2 continuous beam axes`, `D1, 300x600, 6.0 m, 1`).
     - Wire real entity scanning in ModelSpace, dynamic table population via `BeamScheduleAggregator.cs`, genuine DWG extension dictionary (`KATA_BEAM_DATA`) serialization via `BeamXRecordSerializer.cs`, structured XData load assignment (`GanTai`/`HoatTai`), and Excel workbook generation (`BeamPlanScheduleWorkbookGenerator.cs`).
3. Revit Framing Analyzer Chaining Fix:
   - In `HPKata/HPKata.Revit/Services/RevitFramingAnalyzer.cs`:
     - Refactor `FindCollinearFramingChain` to search bidirectionally (both backward and forward from the seed framing element), handling interior seed beams and reversed curve orientations ($\vec{u}_1 \cdot \vec{u}_2 \approx -1$).
4. Host Adapter Test Coverage:
   - Add unit tests in `HPKata/HPKata.Revit.Tests/` (`FramingRebarGeometryTests.cs` and/or `RevitFramingAnalyzerTests.cs`) and `HPKata/HPKata.AutoCad.Tests/` (`BeamControllerTests.cs`) covering the new controllers and geometry calculators without blind spots.

BUILD & TEST MANDATE:
- Run: dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1
- Run: dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"
- Verify all tests pass 100% with 0 warnings and 0 errors.

CLEAN CODE RULES (MANDATORY):
- Max 300 physical lines per file (Rule C2).
- Max 50 physical lines per method (Rule M3).
- 0 dead/commented-out code (Rule Q-B1).
- 0 unannotated empty catch blocks (Rule Q-B2).
- 304.8 strictly in `RevitUnitConverter.cs` (Rule R5).

DELIVERABLE:
Write your implementation report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_remediation\changes.md`
and a structured handoff.md in your working directory.
When complete, notify parent via send_message with build/test results and report path.
