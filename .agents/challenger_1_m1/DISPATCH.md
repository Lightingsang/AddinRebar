# Challenge Assignment: Challenger 1 (M1 - Spatial Algorithm & Boundary Adversarial Stress)

## Objective
Adversarially challenge and stress-test `PlotOrderService` and `PlotBounds` in `HPAutoCad.Core/SmartPlot/`:
1. Challenge the spatial ordering algorithm with extreme topologies:
   - 100+ randomized frames arranged in overlapping rows.
   - Degenerate frames: zero-width, zero-height, negative dimensions, infinite/NaN coordinates.
   - Precision boundary: frames with vertical overlap exactly at 49.999% vs 50.001%.
   - Massive coordinates (millions of units away from origin) to check floating point stability.
2. Formulate stress cases or tests to empirically verify correctness and stability.
3. Deliver verdict: `APPROVE` (robust and empirically confirmed) or `REJECT` / `REQUEST_CHANGES` (demonstrated failure mode).

## Authoritative Reference
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (entry ## 2026-09-20T22:21:59Z)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1\handoff.md`

## Output
Write your report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_1_m1\handoff.md`.

## 2026-09-20T22:34:50Z
You are Challenger 1 for Milestone M1 (Spatial Algorithm & Boundary Stress).
Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_1_m1
Read your task: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_1_m1\DISPATCH.md
Read the authoritative requirements: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (entry ## 2026-09-20T22:21:59Z)
Read worker_m1 handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\worker_m1\handoff.md
Stress test PlotOrderService and PlotBounds with extreme, degenerate, and massive coordinate cases. Write your report with verdict APPROVE or REJECT to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_1_m1\handoff.md.
Send a message when complete.
