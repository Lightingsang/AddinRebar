# Task Assignment: reviewer_m4_2

## Role
Milestone M4 Reviewer 2 (Preview Canvases, Drawing Primitives, and Core Decoupling)

## Working Directory
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m4_2`

## Reference Documents
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Project Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Architecture Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m4_1\ui_canvas_plan.md`
4. Worker Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m4\handoff.md`
5. Target Codebase: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Beam Rebar\`

## Task
Conduct an independent technical review of the custom preview canvases:
- Verify `BeamElevationCanvas.cs`, `BeamElevationPainter.cs`, `BeamSectionCanvas.cs`, `BeamSectionPainter.cs`, `CanvasPalette.cs`, and `BeamDrawPrimitives.cs` in `View/Controls/`.
- Confirm that coordinate scaling strictly calls `BeamCanvasTransformCalculator` from `HPRebar.Core`.
- Confirm zero references to `Autodesk.Revit.*` in `HPRebar.Core`.
- Verify 50ms keystroke debounce logic via `DispatcherTimer` to prevent UI lag on rapid parameter entry.
- Verify that pens and brushes are frozen (`pen.Freeze()`) to prevent GC allocations per render frame.

## Deliverables
- Review report: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m4_2\review_report.md`
- Handoff report: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m4_2\handoff.md`

## 2026-09-07T09:36:56Z
You are reviewer_m4_2.
Your working directory is: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m4_2
Read your task assignment at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m4_2\DISPATCH.md
Read the authoritative user request at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md
Read the worker handoff at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m4\handoff.md
Read the target codebase at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Beam Rebar\

Conduct an independent technical review of the custom preview canvases:
- Verify BeamElevationCanvas.cs, BeamElevationPainter.cs, BeamSectionCanvas.cs, BeamSectionPainter.cs, CanvasPalette.cs, and BeamDrawPrimitives.cs in View/Controls/.
- Confirm coordinate scaling strictly calls BeamCanvasTransformCalculator from HPRebar.Core.
- Confirm zero references to Autodesk.Revit.* in HPRebar.Core.
- Verify 50ms keystroke debounce logic via DispatcherTimer.
- Verify that pens and brushes are frozen (pen.Freeze()) to prevent GC allocations per render frame.

Write your review report to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m4_2\review_report.md
Write your handoff to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m4_2\handoff.md
Notify orchestrator via send_message with your verdict (APPROVE or REQUEST_CHANGES).
