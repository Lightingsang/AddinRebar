# Dispatch: worker_m3 — Milestone M3 Implementation

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Objective
Implement Milestone M3 (Revit Add-In Feature Implementation for Continuous Beam Rebar) in `HPRebar/HPRebar/Beam Rebar/`.

## Authoritative Inputs & Specifications
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Project Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Readers & Models Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m3_1\readers_plan.md`
4. Creators & Shapes Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m3_2\creators_plan.md`
5. Views, Orchestrator & Command Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m3_3\views_orch_plan.md`
6. Golden Reference: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Column Rebar\`

## File Ownership & Scope Boundaries
You own all files under `HPRebar/HPRebar/Beam Rebar/`:
- `HPRebar/HPRebar/Beam Rebar/Models/`:
  - `BeamStack.cs`, `BeamFaces.cs`, `BeamRebarSpec.cs`, `BeamSectionStyle.cs`, `CreatedBeamRebar.cs`, `CreatedBeamViews.cs`, `BeamOrchestratorResult.cs`, `BeamAnnotationSettings.cs`, `RebarTypeInfo.cs`, `UiStrings.cs`, `UiStringsCatalog.cs`, `ValidationMessages.cs`, `ValidationResult.cs`
- `HPRebar/HPRebar/Beam Rebar/` (Root):
  - `StructuralFramingSelectionFilter.cs`
  - `BeamSolidFaceReader.cs`
  - `BeamStackReader.cs`
  - `BeamSupportFinder.cs`
  - `BeamStackValidator.cs`
  - `PointMapper.cs`
  - `RebarShapeResolver.cs`
   colonial-safe and robust shape matching
  - `RebarTypeCatalog.cs`
  - `BeamStirrupCreator.cs`
  - `BeamMainBarCreator.cs`
  - `BeamAdditionalBarCreator.cs`
  - `BeamSideBarCreator.cs`
  - `BeamSpecialBarCreator.cs`
  - `RebarCreationService.cs`
  - `DetailViewCreator.cs`
  - `SectionViewCreator.cs`
  - `DimensionCreator.cs`
  - `RebarTableTagCreator.cs`
  - `BeamRebarOrchestrator.cs`
  - `RevitRebarRunner.cs`
  - `IBeamRebarRunner.cs`
  - `BeamRebarCommand.cs`
  - `RebarFailureHandling.cs`
  - `RevitUnits.cs`
  - `LocalizationService.cs`
  - `ThemeSwitcher.cs`
  - `RevitDialogs.cs`
- `HPRebar/HPRebar/Beam Rebar/View Models/`:
  - `BeamRebarSession.cs`, `BeamRebarViewModel.cs`, and `IBeamRebarRunner.cs`
- `HPRebar/HPRebar/Beam Rebar/View/`:
  - `BeamRebarView.xaml`, `BeamRebarView.xaml.cs` (initial functional modal dialog with dynamic theming)

## Implementation Rules
1. Follow the feature folder convention in AGENTS.md strictly:
   - Root files at `Beam Rebar/` root.
   - Models only in `Models/`.
   - Views only in `View/`.
   - ViewModels only in `View Models/` (with space).
2. Use explicit file-scoped namespaces (`namespace HPRebar.BeamRebar;`, `namespace HPRebar.BeamRebar.Models;`, etc.).
3. Zero deprecated APIs: no `DisplayUnitType`, no `CreateFreeForm`.
4. Check multi-version compilation flags: `#if REVIT2024_OR_GREATER` for `elementId.Value`.
5. Atomic transactions: `BeamRebarOrchestrator` owns `TransactionGroup("Beam Rebar")` with auto-rollback on error/cancel and clean assimilation on success.
6. Attach `RebarFailureHandling.Apply(transaction)` to swallow non-fatal Revit warnings.

## Verification Requirements
Run builds and tests:
- `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false`
- `dotnet build HPRebar/HPRebar.slnx -c Debug.R25 -p:DeployAddin=false`
- `dotnet test HPRebar/HPRebar.Core.Tests`

Write your handoff report to: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m3\handoff.md`
When finished, notify orchestrator via `send_message`.

## 2026-09-07T08:28:30Z
You are worker_m3.
Your working directory is: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m3
Read your task assignment and mandatory integrity warning at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m3\DISPATCH.md
Read the authoritative user request at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md
Read the master project plan at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md
Read the 3 detailed implementation plans:
1. Readers & Models Plan: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m3_1\readers_plan.md
2. Creators & Shapes Plan: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m3_2\creators_plan.md
3. Views, Orchestration & Command Plan: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m3_3\views_orch_plan.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Implement Milestone M3 (Revit Add-In Feature for Continuous Beam Rebar) in `HPRebar/HPRebar/Beam Rebar/`:
1. Models in `HPRebar/HPRebar/Beam Rebar/Models/` (BeamStack.cs, BeamFaces.cs, BeamRebarSpec.cs, BeamSectionStyle.cs, CreatedBeamRebar.cs, CreatedBeamViews.cs, BeamOrchestratorResult.cs, BeamAnnotationSettings.cs, RebarTypeInfo.cs, UiStrings.cs, UiStringsCatalog.cs, ValidationMessages.cs, ValidationResult.cs)
2. Feature Root Classes in `HPRebar/HPRebar/Beam Rebar/`:
   - StructuralFramingSelectionFilter.cs
   - BeamSolidFaceReader.cs
   - BeamStackReader.cs
   - BeamSupportFinder.cs
   - BeamStackValidator.cs
   - PointMapper.cs
   - RebarShapeResolver.cs
   - RebarTypeCatalog.cs
   - BeamStirrupCreator.cs
   - BeamMainBarCreator.cs
   - BeamAdditionalBarCreator.cs
   - BeamSideBarCreator.cs
   - BeamSpecialBarCreator.cs
   - RebarCreationService.cs
   - DetailViewCreator.cs
   - SectionViewCreator.cs
   - DimensionCreator.cs
   - RebarTableTagCreator.cs
   - BeamRebarOrchestrator.cs
   - RevitRebarRunner.cs
   - IBeamRebarRunner.cs
   - BeamRebarCommand.cs
   - RebarFailureHandling.cs
   - RevitUnits.cs
   - LocalizationService.cs
   - ThemeSwitcher.cs
   - RevitDialogs.cs
3. View Models in `HPRebar/HPRebar/Beam Rebar/View Models/`:
   - BeamRebarSession.cs
   - BeamRebarViewModel.cs
4. Views in `HPRebar/HPRebar/Beam Rebar/View/`:
   - BeamRebarView.xaml
   - BeamRebarView.xaml.cs

Ensure:
- Strict file-scoped namespaces (`namespace HPRebar.BeamRebar;`, `namespace HPRebar.BeamRebar.Models;`, etc.).
- Zero deprecated APIs (use UnitTypeId.Millimeters).
- Correct `#if REVIT2024_OR_GREATER` for elementId.Value.
- Verify compilation:
  `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false`
  `dotnet build HPRebar/HPRebar.slnx -c Debug.R25 -p:DeployAddin=false`
  `dotnet test HPRebar/HPRebar.Core.Tests`

Write your handoff report to: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m3\handoff.md`
When finished, notify orchestrator via send_message.
