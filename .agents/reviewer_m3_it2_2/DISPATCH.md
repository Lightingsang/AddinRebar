# Task Assignment: reviewer_m3_it2_2

## Role
M3 Iteration 2 Reviewer 2 (Rebar Creators, Curves, View Sync)

## Working Directory
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_it2_2`

## Reference Documents
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Project Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Worker Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m3_it2\handoff.md`
4. Codebase: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Beam Rebar\`

## Task
Conduct an independent code and correctness review of the rebar creators and view creators:
- Verify `BeamMainBarCreator.cs`: `BuildCurves` closes the polyline with `Line.CreateBound(pLast, pFirst)` when `simplified.IsClosed && simplified.Points.Count > 2`.
- Verify `BeamStirrupCreator.cs`: branches on `run.Count == 1` to call `SetLayoutAsSingle()` and clamps `[2, 1002]` for `SetLayoutAsNumberWithSpacing`.
- Verify `BeamSupportFinder.cs`: preserving physical supports, generating `CantileverEnd` node (width 0), and checking `girderTopZ <= beamSoffitZ + 0.05` in `MeasureGirderSupport`.
- Verify `BeamSpecialBarCreator.cs` and `BeamSpecialBarCalculator.cs`: safe handling when secondary beam station falls in support zone without throwing unhandled exceptions.

## Deliverables
- Review report: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_it2_2\review_report.md`
- Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_it2_2\handoff.md`
- Notify orchestrator with binary verdict: `APPROVE` or `REQUEST_CHANGES`.
