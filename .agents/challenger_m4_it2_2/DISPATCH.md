# DISPATCH — challenger_m4_it2_2

## Mission
You are Challenger 2 for Milestone M4 Iteration 2 (Preview Canvases, Extreme Geometry & Zero-Allocation Rendering).

## Mandatory Reading
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Project Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Worker Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m4_it2\handoff.md`
4. Codebase under test:
   - `HPRebar/HPRebar/Beam Rebar/View/Controls/BeamElevationPainter.cs`
   - `HPRebar/HPRebar/Beam Rebar/View/Controls/BeamElevationCanvas.cs`
   - `HPRebar/HPRebar/Beam Rebar/View/Controls/BeamSectionPainter.cs`
   - `HPRebar/HPRebar/Beam Rebar/View/Controls/BeamSectionCanvas.cs`
   - `HPRebar/HPRebar/Beam Rebar/View/Controls/CanvasPalette.cs`
   - `HPRebar.Core/BeamRebar/Models/BeamContinuousStack.cs`

## Stress-Test Challenges
1. **Cantilever Model Bounds & Screen Coordinates**:
   - Trace screen coordinates when a beam has an exterior start cantilever (e.g., span 0 overhang of 2000 mm before support 0).
   - Verify that `stack.ContinuousStack.OverallStartX` bounds start at the cantilever tip and that `ToScreenX(cantileverTipX) >= MarginLeft` ($X \ge 0$).
   - Verify that neither beam outlines nor rebar curves clip off-screen.
2. **Extreme Aspect Ratio Scaling & Layer Offsets**:
   - Challenge long multi-span beams (e.g. 5 spans of 10m, height 500mm, canvas height 250px -> rendered beam height ~6px):
     - Verify that dynamic layer offsets (`Math.Min(3.0, beamHeightPx * 0.15)`) prevent top bars and layer 2 bars from penetrating the bottom soffit.
3. **Ultra-Narrow Beam Section Challenge**:
   - Challenge `BeamSectionPainter` when beam width $b = 150$ mm and cover $= 50$ mm (clear width $\le 0$):
     - Verify that `BeamSectionPainter` does not throw `ArgumentException`, divide by zero, or draw reversed bars.
4. **Allocation-Free Render Loop Invariant**:
   - Verify that `OnRender` in both `BeamElevationCanvas` and `BeamSectionCanvas` does not instantiate `new CanvasPalette`, `new Pen`, or `new DashStyle`.
   - Verify that `CanvasPalette.DefaultDashStyle`, `DashedDimension`, and `DashedSideBar` are frozen (`IsFrozen == true`).

## Output
Write your challenge report to:
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m4_it2_2\challenge_report.md`
Write your handoff to:
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m4_it2_2\handoff.md`
Notify orchestrator via `send_message` with your verdict: **APPROVE** or **CHALLENGE_FAILED**.
