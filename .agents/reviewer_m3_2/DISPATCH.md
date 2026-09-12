# Dispatch: reviewer_m3_2 — Milestone M3 Reviewer 2

## Mission
Conduct an independent code and technical correctness review of the Rebar Creators, View Generators, and Dimensioning implementations in `HPRebar/HPRebar/Beam Rebar/`.

## Focus Areas
1. Rebar creation implementations:
   - `BeamStirrupCreator.cs`: `Rebar.CreateFromRebarShape`, `ScaleToBox`, `SetLayoutAsNumberWithSpacing` (1002 count limit).
   - `BeamMainBarCreator.cs`: `Rebar.CreateFromCurves`, vertical normal vector $\vec{Y}_{beam}$, 90° hooks and 50% staggered splices.
   - `BeamAdditionalBarCreator.cs`: 2 vertical layers, negative support bars ($L/3$, $L/4$), positive midspan bars ($L/7$).
   - `BeamSideBarCreator.cs`: skin longitudinal bars ($h \ge 700$ mm, spacing $\le 300$ mm), transverse cross-ties.
   - `BeamSpecialBarCreator.cs`: secondary beam hanging stirrups, 45° diagonal ties within host span.
   - `RebarCreationService.cs`: pre-flight checks (`CanCreate`), 5 staged transactions.
2. View and Annotation generators:
   - `DetailViewCreator.cs`: `ViewSection.CreateDetail` / `CreateSection`, transform, naming and crop box.
   - `SectionViewCreator.cs`: transverse sections, asymmetric crop (+2.5x margin).
   - `DimensionCreator.cs`: stable reference token rewriting (`SURFACE` -> `LINEAR`), exception guards.
   - `RebarTableTagCreator.cs`: detail curves and text notes schedule tables.
3. Universal Unit Conversion: `RevitUnits.cs` (`UnitTypeId.Millimeters`).

## Inputs
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Worker Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m3\handoff.md`
4. Target Codebase: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Beam Rebar\`

## Deliverables
- Detailed review report: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_2\review_report.md`
- Self-contained handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_2\handoff.md`


## 2026-09-07T08:42:50Z
You are reviewer_m3_2.
Your working directory is: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_2
Read your task assignment at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_2\DISPATCH.md
Read the authoritative user request at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md
Read the worker handoff at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m3\handoff.md
Read the target codebase at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Beam Rebar\

Conduct an independent technical correctness review:
- Verify BeamStirrupCreator (ScaleToBox, SetLayoutAsNumberWithSpacing, 1002 count limit).
- Verify BeamMainBarCreator (Rebar.CreateFromCurves with normal vector Y_beam, 90 hooks and staggered splices).
- Verify BeamAdditionalBarCreator (2 vertical layers with deltaZ offset, support L/3, L/4 top bars, midspan L/7 bottom bars).
- Verify BeamSideBarCreator (skin bars for h >= 700 mm, spacing <= 300 mm, anti-buckling cross-ties).
- Verify BeamSpecialBarCreator (secondary framing hanging stirrups, 45 degree diagonal ties clamped in span).
- Verify RebarCreationService staged transactions.
- Verify DetailViewCreator & SectionViewCreator.
- Verify DimensionCreator (SURFACE -> LINEAR stable reference rewriting).

Write your review report to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_2\review_report.md
Write your handoff to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_2\handoff.md
Notify orchestrator via send_message with your verdict (APPROVE or REQUEST_CHANGES).

