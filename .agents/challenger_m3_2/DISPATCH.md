# Dispatch: challenger_m3_2 — Milestone M3 Challenger 2

## Mission
Stress-test and empirically challenge the Rebar Instantiation and Transformation subsystems in `HPRebar/HPRebar/Beam Rebar/`.

## Challenge Scenarios & Invariant Verification
1. `PointMapper.cs` & Coordinate Transformations:
   - Verify coordinate mapping from local millimetres $(X, Y, Z)$ to Revit world feet $XYZ$.
   - Test invariance under rotated continuous beams in world coordinates (e.g. beam at 37° angle in plan).
2. `Rebar.CreateFromCurves` Constraints:
   - Check normal vector $\vec{Y}_{beam} = \vec{Z} \times \vec{X}_{beam}$ orthogonality to all curve segments.
   - Verify that `Polyline3.Simplify(1.0)` eliminates curve segments shorter than Revit's short curve tolerance (~0.78 mm).
   - Check hook orientation and planar curve closure.
3. `Rebar.CreateFromRebarShape` Constraints:
   - Check orthogonal vectors `xVec = stack.NormalDirection` and `yVec = XYZ.BasisZ`.
   - Verify that `ScaleToBox` parameters (width, height) are strictly positive and account for concrete cover.
   - Verify that `SetLayoutAsNumberWithSpacing` bar count is strictly clamped $\le 1002$ to avoid Revit API exceptions.
4. Section Dimensioning Reference Rewriting:
   - Verify that `DimensionCreator` successfully rewrites `"SURFACE"` to `"LINEAR"` in stable representation strings.

## Inputs
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Worker Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m3\handoff.md`
4. Codebase: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Beam Rebar\`

## Deliverables
- Detailed challenge report: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m3_2\challenge_report.md`
- Self-contained handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m3_2\handoff.md`
- Notify orchestrator via `send_message` with your verdict (APPROVE or CHALLENGE_FAILED).

## 2026-09-07T08:42:50Z
User Request received:
Stress-test and challenge rebar instantiation and geometric transformations:
- Challenge PointMapper coordinate mapping under rotated beam orientations in plan (e.g. 30, 45, 60 degrees).
- Challenge Rebar.CreateFromCurves normal vector Y_beam orthogonality and short curve culling via Polyline3.Simplify(1.0).
- Challenge Rebar.CreateFromRebarShape vector orthogonality (xVec = Y_beam, yVec = BasisZ) and max bar count limit (1002).
- Challenge DimensionCreator stable representation token replacement (SURFACE -> LINEAR).

Write your challenge report to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m3_2\challenge_report.md
Write your handoff to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m3_2\handoff.md
Notify orchestrator via send_message with your verdict (APPROVE or CHALLENGE_FAILED).

