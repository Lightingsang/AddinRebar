# Migration Plan: Port R02_BeamsRebar to HPRebar

- Target Solution: HPRebar/HPRebar.slnx
- Target Frameworks: .NET 8 (Revit 2025/2026), .NET Framework 4.8 (Revit 2023/2024), .NET 10 (Revit 2027)
- Source Module: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\RebarAddin-master\RebarAddin-master\R02_BeamsRebar
- Target Feature: HPRebar/HPRebar/Beam Rebar/ & HPRebar.Core/BeamRebar/

## Phase 1: Pure Logic Domain in HPRebar.Core
- Models: BeamSpan, BeamSupportNode, BeamStirrupSpec, BeamStirrupDistribution, BeamMainBarSpec, BeamAdditionalBarSpec, BeamSideBarSpec, BeamSpecialBarSpec, BeamPolyline.
- Calculators: BeamStirrupCalculator, BeamMainBarCalculator, BeamAdditionalBarCalculator, BeamSideBarCalculator, BeamSpecialBarCalculator, BeamBarPolylineBuilder, BeamCanvasScaleCalculator.

## Phase 2: Unit Testing in HPRebar.Core.Tests
- Edge cases from bs:scenario: single span, multi-span, cantilevers left/right, varying beam depths, high beams needing side bars, secondary beam hanging ties, stirrup distribution evenly vs 3 zones.

## Phase 3: Revit Geometry Extraction & Validation
- StructuralFramingSelectionFilter.cs
- BeamSolidFaceReader.cs
- BeamSupportFinder.cs
- BeamStackReader.cs
- BeamStackValidator.cs

## Phase 4: Revit Rebar Creators & Views
- RevitUnits.cs
- BeamStirrupCreator.cs
- BeamMainBarCreator.cs
- BeamAdditionalBarCreator.cs
- BeamSideBarCreator.cs
- BeamSpecialBarCreator.cs
- BeamDetailViewCreator.cs
- BeamSectionViewCreator.cs
- BeamDimensionCreator.cs
- BeamTagCreator.cs
- BeamRebarCreationService.cs

## Phase 5: WPF UI & MVVM Layer
- LocalizationService.cs & UiStrings.cs
- BeamRebarSession.cs
- BeamRebarViewModel.cs & TabViewModels
- BeamRebarView.xaml & TabViews
- Canvas preview controls: BeamElevationCanvas.cs, BeamSectionCanvas.cs

## Phase 6: Orchestration, Command & Ribbon
- BeamRebarOrchestrator.cs (Atomic TransactionGroup owner)
- BeamRebarCommand.cs
- Ribbon button registration in Application.cs

## Phase 7: Verification & Review
- dotnet build HPRebar/HPRebar.slnx -c Debug.R25 -p:DeployAddin=false
- dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false
- dotnet test HPRebar/HPRebar.Core.Tests
- Adversarial Code Review via bs:code-review
