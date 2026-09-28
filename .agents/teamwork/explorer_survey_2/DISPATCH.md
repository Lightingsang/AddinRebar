# Dispatch: Explorer Survey 2 (Beam Rebar Geometry & Math in HPRebar.Core)

- **Role**: teamwork_preview_explorer
- **Task**: Map existing beam rebar geometry calculations in `HPRebar.Core` and determine architecture for `KataRebar` geometric calculation engine.
- **Authoritative Request**: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (under ## 2026-09-27T15:57:37Z)
- **Scope**:
  1. Investigate `HPRebar.Core/BeamRebar/` and `HPRebar.Core.Tests/BeamRebar/`.
  2. Examine how 3D rebar curves, coordinate systems (beam local vs global), covers, stirrups, continuous bars, and cutoffs are modeled in `HPRebar.Core`.
  3. Determine how `KataBeamRebarSpec` translates into 3D curves:
     - Continuous top & bottom bars with anchorage into end supports (hook 90° or lap length).
     - Support top additional bar cutoffs (L/4, L/3, or sheet settings).
     - Mid-span bottom additional bar cutoffs relative to clear span faces.
     - Stirrup distribution (support dense L/4, mid-span sparse L/2, closed stirrup, U cap, C cross tie).
     - Side bars / torsion bars / web skin.
  4. Ensure zero references to `Autodesk.Revit.*` in `HPRebar.Core` (`netstandard2.0`).
- **Deliverable**: Write a comprehensive survey report to `report.md` in your working directory and deliver `handoff.md`.

## 2026-09-27T16:01:00Z

<USER_REQUEST>
You are explorer_survey_2 (Core Beam Rebar Math Explorer).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_2
Read your dispatch instructions at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_2\DISPATCH.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md under header ## 2026-09-27T15:57:37Z.

Your mission:
1. Investigate existing beam rebar models and algorithms in `HPRebar.Core/BeamRebar/` and tests in `HPRebar.Core.Tests/BeamRebar/`.
2. Check existing coordinate math, polyline/curve representations, beam spans and supports modeling, cover handling, stirrup distribution (3-zone support L/4 vs midspan L/2), anchorage hook (90° bend), top additional bar cutoffs (L/4, L/3, sheet parameters), bottom additional bar cutoffs, cantilever spans, and side bars.
3. Determine what domain models and calculators already exist that can be reused vs what dedicated classes are needed for `KataRebar` (e.g., `KataRebarCalculator`, `KataRebarLayoutResult`, `KataRebarCurve`, `KataStirrupZone`).
4. Ensure the design maintains 100% netstandard2.0 purity with ZERO references to `Autodesk.Revit.*`.
5. Propose a clear calculation pipeline from `KataBeamRebarSpec` + Beam geometry inputs -> 3D rebar curves and layout.
6. Write your comprehensive findings to `report.md` and handoff to `handoff.md`, then send a message when complete.
</USER_REQUEST>
