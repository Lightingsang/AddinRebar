# Task Assignment: challenger_m4_2

## Role
Milestone M4 Challenger 2 (Canvas Geometry, Transforms & Memory Stress Test)

## Working Directory
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m4_2`

## Reference Documents
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Project Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Architecture Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m4_1\ui_canvas_plan.md`
4. Worker Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m4\handoff.md`
5. Target Codebase: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Beam Rebar\`

## Task
Stress-test and challenge the interactive preview canvases:
- Challenge `BeamElevationCanvas` with extreme aspect ratios (very long multi-span beams, very short deep beams, cantilever overhangs).
- Challenge `BeamSectionCanvas` with extreme cross-sections (wide transfer beams, tall thin beams).
- Verify that `BeamDrawPrimitives` and `CanvasPalette` do NOT allocate new GDI/WPF pens or brushes during `OnRender` (frozen pen caching).
- Verify that rapid parameter adjustments trigger debounced invalidation without UI thread stalls.

## Deliverables
- Challenge report: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m4_2\challenge_report.md`
- Handoff report: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m4_2\handoff.md`
- Notify orchestrator with binary verdict: `APPROVE` or `CHALLENGE_FAILED`.
