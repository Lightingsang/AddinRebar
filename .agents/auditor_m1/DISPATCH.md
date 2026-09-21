# Audit Assignment: Forensic Auditor (Milestone M1)

## Objective
Perform an exhaustive integrity and forensic audit on the code delivered for Milestone M1:
1. Scan `HPAutoCad.Core/SmartPlot/` and `HPAutoCad.Tests/SmartPlot/` for any evidence of cheating:
   - Hardcoded test returns or expected test results embedded in domain logic.
   - Dummy or facade implementations (empty stubs that return mock data).
   - Mocking or bypassing real algorithmic calculations.
2. Verify that `HPAutoCad.Core` contains ZERO references to `Autodesk.*`, Revit, AutoCAD, or third-party UI libraries.
3. Verify that `PlotOrderService`, `LayoutRangeParser`, `FileNameService`, and `PresetService` have genuine, robust implementations that fulfill all domain requirements.
4. Deliver verdict: `CLEAN` (zero integrity violations found) or `INTEGRITY VIOLATION` (with detailed evidence).

## Authoritative Reference
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (entry ## 2026-09-20T22:21:59Z)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1\handoff.md`

## Output
Write your forensic report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\auditor_m1\handoff.md`.

## 2026-09-20T22:35:00Z
You are the Forensic Auditor for Milestone M1.
Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\auditor_m1
Read your task: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\auditor_m1\DISPATCH.md
Read the authoritative requirements: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (entry ## 2026-09-20T22:21:59Z)
Read worker_m1 handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1\handoff.md
Audit HPAutoCad.Core/SmartPlot/ and HPAutoCad.Tests/SmartPlot/ for integrity violations, hardcoded cheating, facade implementations, or mock shortcuts. Verify zero host dependencies. Write your report with verdict CLEAN or INTEGRITY VIOLATION to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\auditor_m1\handoff.md.
Send a message when complete.
