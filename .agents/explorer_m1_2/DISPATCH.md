# DISPATCH — explorer_m1_2

Role: M1 Domain Calculators Architect
Working Directory: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_2

## Context & Inputs
- Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
- Project Master Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
- Survey Analysis: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_source_1\survey_source_analysis.md`
- Target Reference: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar.Core\ColumnRebar\StirrupDistributionCalculator.cs`

## Task
Investigate and design the exact calculation algorithms for `HPRebar.Core/BeamRebar/Calculators/`:
1. `BeamStirrupDistributionCalculator.cs`:
   - Uniform distribution calculation ($L_n$, spacing $S$, start offset 50mm, centering offset, count).
   - 3-Zone distribution ($L/4 - L/2 - L/4$ and $L/3 - L/3 - L/3$, dense spacing $S_1$, midspan spacing $S_2$, boundary rounding).
   - Guardrail: `MaxBarPositions = 1002` check.
   - Cantilever stirrup rules (uniform dense layout along cantilever).
2. `BeamMainBarCalculator.cs`:
   - Continuous top and bottom polyline generator across all spans.
   - Exterior anchorage hooks: 90° standard hook geometry calculation into exterior column.
   - Splicing: 50% staggered lap splice positions (top bar splices at midspan, bottom bar splices at supports) for total length exceeding standard bar stock (11700 mm).
   - Culled sub-millimeter segments (< 1.0 mm) to prevent polyline crashes.
3. `BeamAdditionalBarCalculator.cs`:
   - Top negative moment bars over supports: lengths extending $L_n/3$ or $L_n/4$ into adjacent spans, 2 vertical layers with $\Delta Z$ offset.
   - Bottom positive moment bars at midspan: cutoffs at $L_n/7$ or $L_n/8$ from support faces, 2 vertical layers.
4. `BeamSideBarCalculator.cs`:
   - Side/web skin bars when total height $h \ge 700$ mm, spacing $\le 300$ mm.
   - Transverse cross-ties (C-ties) with alternating 90°/135° or 180° hooks.
5. `BeamSpecialBarCalculator.cs`:
   - Hanging stirrup cages at secondary beam intersections ($n \times 2$ stirrups @ 50mm).
   - 45° diagonal tie bar geometry.
6. `BeamCanvasTransformCalculator.cs`:
   - Coordinate transformation from continuous beam world coordinates (mm) to WPF Canvas coordinates (pixels) with aspect ratio preservation and margin padding.
7. `Tolerance.cs`:
   - Numerical comparison with epsilon $1.0\times 10^{-9}$.

## Output
Write report to `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_2\calculators_plan.md` and `handoff.md`.
Notify orchestrator via send_message.

## 2026-09-07T07:35:36Z
User request:
You are explorer_m1_2.
Your working directory is: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_2
Read your task assignment at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_2\DISPATCH.md
Read the authoritative user request at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md
Read project master plan at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md

Investigate and design the exact calculation algorithms for HPRebar.Core/BeamRebar/Calculators/ (BeamStirrupDistributionCalculator, BeamMainBarCalculator, BeamAdditionalBarCalculator, BeamSideBarCalculator, BeamSpecialBarCalculator, BeamCanvasTransformCalculator, Tolerance).
Write your detailed plan to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_2\calculators_plan.md
Write your handoff to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_2\handoff.md
When finished, notify orchestrator via send_message.

