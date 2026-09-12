# DISPATCH — spec_miner_revit_1

Role: Revit API & Integration Spec Miner
Working Directory: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_revit_1

## Objective
Read ORIGINAL_REQUEST.md at `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md` and repository guidelines in `AGENTS.md`.
Spec out the Revit API requirements, contracts, unit conversions, and anti-patterns for continuous beam rebar in modern Revit (2025/2026, .NET 8).

## Tasks
1. Revit Rebar API specifications for Revit 2025/2026:
   - `Rebar.CreateFromCurves` signatures and parameters (RebarBarType, RebarHookType, hooks, vector normal, curves, hook orientation).
   - `Rebar.CreateFromRebarShape` signatures for stirrups.
   - Array distribution methods: `Rebar.GetShapeDrivenAccessor()` / `SetLayoutAsFixedNumberWithSpacing` / `SetLayoutAsMaximumSpacing` / etc.
   - Zero deprecated APIs: no `DisplayUnitType` (use `UnitTypeId` / `ForgeTypeId`), no `ElementId.IntegerValue` in 2024+ (use `ElementId.Value` as long), check multi-version conditional compilation in `AGENTS.md`.
2. Structural Framing Revit Geometry & Element Querying:
   - Extracting beam solids, faces (top, bottom, side faces), centerlines, bounding boxes.
   - Identifying continuous beams: collinear axis check, adjacent beams, level, cross-section validation.
   - Identifying support columns and structural walls beneath the beam stack (`BeamSupportFinder.cs`).
3. Transaction & Safety specifications:
   - `TransactionGroup` ("Beam Rebar") in `BeamRebarOrchestrator.cs`: start, assimilate on success, rollback on exception or user cancel.
4. Drawing & Annotation creation specifications:
   - Creating detail elevation view (`ViewSection.CreateDetail`), cross-section views (`ViewSection.CreateSection`).
   - Dimensioning (`Document.Create.NewDimension`).
   - Rebar tagging (`IndependentTag.Create`).
5. Unit conversions:
   - Millimetres to feet boundary (`RevitUnits.cs` / `UnitUtils`).

## Output

## 2026-09-07T07:25:27Z

Investigate and specify Revit 2025/2026 API requirements for continuous beam rebar generation:
- Rebar creation methods (CreateFromCurves, CreateFromRebarShape, modern overloads, hook types, layout rules)
- Deprecated API avoidance (no DisplayUnitType, ElementId.Value vs IntegerValue, UnitUtils)
- Structural Framing geometry extraction (solid faces, continuous beam alignment, column/beam support detection)
- Atomic TransactionGroup management (start, assimilate, rollback)
- View & Annotation creation (ViewSection detail/section views, dimensioning, rebar tags)
Write your report to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_revit_1\revit_api_spec.md
Write your handoff report to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\spec_miner_revit_1\handoff.md
When finished, notify the orchestrator via send_message.
