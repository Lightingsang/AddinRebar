# Project: Kata Rebar

## Architecture
The Kata Rebar feature automatically generates 3D concrete beam reinforcement in Revit 2026 based on structural calculation and detailing data read from sheet `Dam` of `Kata.xlsm` (via active Excel COM or file fallback).

```
                      +---------------------------------------+
                      |         Excel Sheet 'Dam'             |
                      | (Active COM or ClosedXML .xlsm file)  |
                      +---------------------------------------+
                                          |
                                          v
+---------------------------------------------------------------------------------+
| HPRebar.Core (netstandard2.0 — Zero Revit References)                           |
|                                                                                 |
|  +---------------------------+         +-------------------------------------+  |
|  |   KataDamSheetParser      | ------> |      KataBeamRebarSpec (DTOs)       |  |
|  | (IKataDamCellAccessor)    |         | (spans, supports, bars, stirrups)   |  |
|  +---------------------------+         +-------------------------------------+  |
|                                                           |                     |
|                                                           v                     |
|                                        +-------------------------------------+  |
|                                        |         KataRebarCalculator         |  |
|                                        | (3D Polyline3 curves, cutoffs,      |  |
|                                        |  anchorage, 3-zone stirrups □,U,C)  |  |
|                                        +-------------------------------------+  |
|                                                           |                     |
|                                                           v                     |
|                                        +-------------------------------------+  |
|                                        |        KataRebarLayoutResult        |  |
+---------------------------------------------------------------------------------+
                                                            |
                                                            v
+---------------------------------------------------------------------------------+
| HPRebar (net8.0-windows7.0 — Revit 2026 Add-In)                                 |
|                                                                                 |
|  +------------------------------+       +------------------------------------+  |
|  |     KataRebarViewModel       | <===> |      KataRebarView (Modeless)      |  |
|  | (preview spans, bars, types) |       | (MaterialDesign + ThemeDark/Light) |  |
|  +------------------------------+       +------------------------------------+  |
|                 |                                                               |
|                 v                                                               |
|  +------------------------------+       +------------------------------------+  |
|  |  KataRebarExternalHandler    | ----> |       KataRebarOrchestrator        |  |
|  | (Revit Main Thread Sync)     |       | (TransactionGroup atomic Undo)     |  |
|  +------------------------------+       +------------------------------------+  |
|                                                           |                     |
|                                                           v                     |
|  +------------------------------+       +------------------------------------+  |
|  |   KataRebarCleanupService    | ----> |      KataRebarCreationService      |  |
|  | (Idempotency: delete prior   |       | (Rebar.CreateFromCurves &          |  |
|  |  "HPRebar_Kata_{BeamName}")  |       |  Rebar.CreateFromRebarShape)       |  |
|  +------------------------------+       +------------------------------------+  |
+---------------------------------------------------------------------------------+
```

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| F1 | Sheet Dam Parser & DTOs | Parse sheet Dam header, continuous bars, extra top/bottom bars, side bars, stirrup definitions into KataBeamRebarSpec | M1 | Survey 1 |
| F2 | Notation String Parser | Parse complex bar strings ('2f18', '3f20', 'a150', '2f20;2f16', '-50;5f20') | M1 | Survey 1, 2 |
| F3 | Dual-Source Excel Reader | Active Excel COM (oleaut32 Value2 array) + ClosedXML .xlsm disk file fallback via IKataDamCellAccessor | M1 | Survey 1 |
| F4 | Continuous Main Bar Calculator | Calculate top & bottom continuous bar curves with 90° hooks or lap anchorage | M2 | Survey 2 |
| F5 | Additional Bar Calculator | Calculate support top bar cutoffs (L/3, L/4, sheet factors) & midspan bottom bar cutoffs | M2 | Survey 2 |
| F6 | Side Bar & Skin Calculator | Calculate side bars for h >= 700mm with spacing <= 300mm | M2 | Survey 2 |
| F7 | 3-Zone Stirrup Distribution | Calculate support dense zone (L/4) and midspan sparse zone (L/2) for shapes □, U, C | M2 | Survey 2 |
| F8 | Pure Domain Unit Tests | 100% green xUnit tests in HPRebar.Core.Tests covering parser, notation, geometry, edge cases | M3 | Survey 1, 2 |
| F9 | Beam Matching & Geometry Extraction | Match Revit framing selection with Kata spec by name and span count | M4 | Survey 3 |
| F10 | Smart RebarBarType & Hook Resolution | Map bar notation to RebarBarType by diameter (±0.5mm) and RebarHookType | M4 | Survey 3 |
| F11 | Idempotent Cleanup Service | Query and delete prior Kata rebars tagged Comments = "HPRebar_Kata_{BeamName}" | M4 | Survey 3 |
| F12 | Revit Rebar Generation Service | Create 3D Rebar elements inside TransactionGroup using CreateFromCurves and CreateFromRebarShape | M4 | Survey 3 |
| F13 | Modeless WPF MVVM Dialog | Interactive preview dialog with dynamic theming and ExternalEvent execution | M5 | Survey 3 |
| F14 | Ribbon Button Integration | Register "Kata Rebar" push button on "Rebar" panel adjacent to "Kata Export" | M5 | Survey 3 |
| F15 | Build & Integration Verification | Verify clean compilation (Debug.R26) and 100% test pass with zero regressions | M6 | Survey 1, 2, 3 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Core DTOs & Sheet Parser | Models (KataBeamRebarSpec, sub-models), KataBarNotationParser, IKataDamCellAccessor, KataDamSheetParser, Excel COM & ClosedXML readers | none | DONE |
| M2 | Core Rebar Geometry Calculator | KataRebarCalculator, Polyline3 generation, anchorage hooks, cutoffs, side bars, 3-zone stirrup loops | M1 | DONE |
| M3 | Pure Domain xUnit Test Suite | Comprehensive tests in HPRebar.Core.Tests for parser, notation, calculator, single/multi-span/cantilever | M1, M2 | DONE |
| M4 | Revit 3D Rebar Generation & Idempotency | BeamMatcher, RebarTypeCatalog resolver, KataRebarCleanupService, KataRebarCreationService, KataRebarOrchestrator | M1, M2, M3 | DONE |
| M5 | WPF MVVM UI & Ribbon Integration | KataRebarViewModel, KataRebarView, ExternalEventHandler, Ribbon button in Application.cs | M4 | DONE |
| M6 | End-to-End Build & Verification | Solution build on Debug.R26, all tests green, forensic audit, victory claim | M1-M5 | DONE |

## Interface Contracts

### 1. `IKataDamCellAccessor` (`HPRebar.Core.KataRebar.Parsers`)
```csharp
public interface IKataDamCellAccessor
{
    string? GetText(int row, int col);
    double? GetDouble(int row, int col);
    int? GetInt(int row, int col);
}
```

### 2. `KataBeamRebarSpec` (`HPRebar.Core.KataRebar.Models`)
```csharp
public sealed record KataBeamRebarSpec
{
    public string BeamName { get; init; } = "";
    public int BeamCount { get; init; } = 1;
    public double Width { get; init; } // mm
    public double Height { get; init; } // mm
    public double SlabThickness { get; init; } // mm
    public double TensionLapMultiplier { get; init; } = 40.0;
    public double CompressionLapMultiplier { get; init; } = 30.0;
    public double TopCutoffRatioLayer1 { get; init; } = 0.25;
    public double TopCutoffRatioLayer2 { get; init; } = 0.20;
    public double CoverMain { get; init; } = 30.0;
    public double CoverStirrup { get; init; } = 25.0;
    public KataBarItem TopContinuous { get; init; } = new(0, 0);
    public KataBarItem BottomContinuous { get; init; } = new(0, 0);
    public IReadOnlyList<KataSupportRebarSpec> Supports { get; init; } = Array.Empty<KataSupportRebarSpec>();
    public IReadOnlyList<KataSpanRebarSpec> Spans { get; init; } = Array.Empty<KataSpanRebarSpec>();
}
```

### 3. `KataRebarLayoutResult` (`HPRebar.Core.KataRebar.Models`)
```csharp
public sealed record KataRebarLayoutResult
{
    public IReadOnlyList<KataRebarCurve> MainTopBars { get; init; } = Array.Empty<KataRebarCurve>();
    public IReadOnlyList<KataRebarCurve> MainBottomBars { get; init; } = Array.Empty<KataRebarCurve>();
    public IReadOnlyList<KataRebarCurve> ExtraTopBars { get; init; } = Array.Empty<KataRebarCurve>();
    public IReadOnlyList<KataRebarCurve> ExtraBottomBars { get; init; } = Array.Empty<KataRebarCurve>();
    public IReadOnlyList<KataRebarCurve> SideBars { get; init; } = Array.Empty<KataRebarCurve>();
    public IReadOnlyList<KataStirrupZoneResult> StirrupZones { get; init; } = Array.Empty<KataStirrupZoneResult>();
}
```

## Code Layout
- `HPRebar/HPRebar.Core/KataRebar/`:
  - `Models/`:
    - `KataBeamRebarSpec.cs`
    - `KataSpanRebarSpec.cs`
    - `KataSupportRebarSpec.cs`
    - `KataBarItem.cs`
    - `KataStirrupSpec.cs`
    - `KataRebarCurve.cs`
    - `KataStirrupZoneResult.cs`
    - `KataRebarLayoutResult.cs`
  - `Parsers/`:
    - `KataBarNotationParser.cs`
    - `IKataDamCellAccessor.cs`
    - `KataCellTable.cs`
    - `KataDamSheetParser.cs`
  - `Calculators/`:
    - `KataRebarCalculator.cs`
- `HPRebar/HPRebar/KataRebar/`:
  - `Excel/`:
    - `ComKataDamReader.cs`
    - `ClosedXmlKataDamReader.cs`
  - `Service/`:
    - `KataBeamMatcher.cs`
    - `KataRebarTypeResolver.cs`
    - `KataRebarCleanupService.cs`
    - `KataRebarCreationService.cs`
    - `KataRebarOrchestrator.cs`
    - `KataRebarExternalEventHandler.cs`
  - `View/`:
    - `KataRebarView.xaml`
    - `KataRebarView.xaml.cs`
  - `ViewModel/`:
    - `KataRebarViewModel.cs`
  - `KataRebarCommand.cs`
- `HPRebar/HPRebar.Core.Tests/KataRebar/`:
  - `KataDamSheetParserTests.cs`
  - `KataBarNotationParserTests.cs`
  - `KataRebarCalculatorTests.cs`
