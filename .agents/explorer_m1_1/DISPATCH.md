# DISPATCH — explorer_m1_1

Role: M1 Domain Models Architect
Working Directory: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_1

## Context & Inputs
- Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
- Project Master Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
- Survey Analysis: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_source_1\survey_source_analysis.md`
- Target Reference: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar.Core\ColumnRebar\`

## Task
Investigate and design the exact C# data models for `HPRebar.Core/BeamRebar/Models/`:
1. `Point3.cs`, `Vector3.cs`, `Polyline3.cs` (pure math, readonly structs / records, mm units).
2. `BeamSpan.cs`: Span index, span name, clear span $L_n$, center-to-center span $L_c$, width $b$, height $h$, top elevation/offset $Z$, cover $c$.
3. `BeamSupportNode.cs`: Node index, coordinate $X$, width $C$, SupportType (Column, Wall, Girder, CantileverLeft, CantileverRight).
4. `BeamContinuousStack.cs`: Ordered list of spans and support nodes, validation checks.
5. Reinforcement specification records:
   - `BeamStirrupSpec.cs`: Layout type (Uniform, ThreeZoneL4, ThreeZoneL3), diameter, spacing dense/sparse, hook angles, zone list.
   - `BeamMainBarSpec.cs`: Top & bottom continuous bars, diameter, count, hook lengths, lap splice parameters.
   - `BeamAdditionalBarSpec.cs`: Support top bars (L/3, L/4, layer 1 & 2), midspan bottom bars (L/7, L/8, layer 1 & 2).
   - `BeamSideBarSpec.cs`: Skin bars for $h \ge 700$ mm, diameter, count/spacing, cross-tie parameters.
   - `BeamSpecialBarSpec.cs`: Secondary beam intersection hanging stirrups & diagonal ties.
   - `Enums.cs`: All necessary enums.
6. Verify strict compliance with `netstandard2.0`, `Polyfill 11.0.1`, immutability, zero `Autodesk.Revit.*` references.

## Output
Write report to `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_1\models_plan.md` and `handoff.md`.
Notify orchestrator via send_message.

## 2026-09-07T07:35:36Z
User request:
You are explorer_m1_1.
Your working directory is: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_1
Read your task assignment at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_1\DISPATCH.md
Read the authoritative user request at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md
Read project master plan at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md

Investigate and design the complete domain models for HPRebar.Core/BeamRebar/Models/ (BeamSpan, BeamSupportNode, BeamContinuousStack, StirrupSpec, MainBarSpec, AdditionalBarSpec, SideBarSpec, SpecialBarSpec, Point3, Vector3, Polyline3, Enums).
Write your detailed plan to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_1\models_plan.md
Write your handoff to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_1\handoff.md
When finished, notify orchestrator via send_message.

