## 2026-10-05T23:40:37Z
[Message] timestamp=2026-10-05T23:40:37Z sender=f21aade0-effc-4aa5-a071-ae17d49dabe3 priority=MESSAGE_PRIORITY_HIGH content=You are teamwork_preview_explorer assigned to Milestone 1 (M1: TCVN Detailing Rules, QC Engine & Serialization) of the HPBim deliverable.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m1_3_hpbim

Read the authoritative user request at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under ## 2026-10-05T23:21:03Z)

Read the project specification at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10\PROJECT.md

Consult prior survey findings in:
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_vncad\handoff.md

Your task:
Analyze and formulate the exact implementation blueprint for:
1. TCVN 5574:2018 detailing rules under `src/HPBim.Core/Tcvn/`:
   - `TcvnStandards.cs`: Standard wall thicknesses (110, 220), column sections (220x220), concrete grades.
   - `ConcreteCoverRules.cs`: Minimum cover (Footing: 50mm, Beam/Column: 25mm, Stair: 20mm, Slab: 15mm).
   - `TcvnRebarCalculator.cs`:
     * Rebar weight formula: W = (D^2 / 162.2) * L (kg).
     * Anchorage length: 30D. Hook 90 deg: 12D.
     * Lap splice length: 40D (tension) / 30D (compression). Max 50% staggered.
     * Commercial bar max length: 11.7m (11700mm), auto-calculates lap splices if bar > 11.7m.
     * Stirrup hooks: 135 deg with straight extension >= 10D and >= 75mm.
     * Stirrup zones: Dense zone a100 in 2h / L/4; sparse zone a200 mid-span.
2. Offline Pre-flight QC Engine under `src/HPBim.Core/Qc/`:
   - `QcReport.cs`: IsValid, Errors, Warnings, Clashes.
   - `QcClashDetector.cs`: Detects column vs opening clashes, secondary beam intersecting slabs without sub-division.
   - `ClearanceChecker.cs`: Opening to column clearance >= 100mm, door swing clearance.
   - `SpatialGridIndex.cs`: Grid bucketing (2000mm cell size) for O(1) candidate lookup.
   - `QcEngine.cs`: Orchestrates all pre-flight checks on `BimModel`.
3. Serialization under `src/HPBim.Core/Serialization/`:
   - `BimModelSerializer.cs`: Clean JSON serialization & deserialization using System.Text.Json without external dependencies.
4. Clean code compliance: Methods <= 50 lines, Classes <= 300 lines, 0 empty catches.

Deliverables:
- Write your implementation plan to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m1_3_hpbim\handoff.md.
- Update g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m1_3_hpbim\progress.md.
- Send a message to parent with your summary and handoff path.
