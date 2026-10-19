## 2026-10-06T00:00:05Z
You are teamwork_preview_reviewer assigned to Milestone 1 (M1: Domain Algorithms, TCVN Rules & QC Review).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m1_2_hpbim

Read the authoritative request at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under ## 2026-10-05T23:21:03Z)

Read the project specification at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_10\PROJECT.md

Read Worker handoff at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1_hpbim\handoff.md

Your task:
1. Examine code in `HPBim/src/HPBim.Core/Algorithms/`, `HPBim/src/HPBim.Core/Tcvn/`, `HPBim/src/HPBim.Core/Qc/`, and `HPBim/src/HPBim.Core/Serialization/`.
2. Build the full solution:
   `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\HPBim.slnx" -c Release`
3. Verify correctness of:
   - `WallSegmenter`: wall span subdivision around door/window cutouts.
   - `RoomAreaCalculator`: Shoelace formula area computation.
   - `TcvnRebarCalculator`: Weight formula W = (D^2 / 162.2) * L, anchorage 30D, lap 40D, hook 12D, commercial splices.
   - `QcEngine`: Clash detection and clearance checking.
   - `BimModelSerializer`: System.Text.Json serialization without external dependencies.
4. Verify Clean Code rules: methods <= 50 lines, classes <= 300 lines, 0 empty catches.
5. Provide a clear verdict: APPROVE or REQUEST_CHANGES.
6. Write your review report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m1_2_hpbim\handoff.md`, update `progress.md`, and send message to parent.
