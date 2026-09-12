# Project: HPRebar — Continuous Beam Rebar (R02_BeamsRebar Migration)

## Architecture

The Continuous Beam Rebar feature is decomposed into three strictly decoupled layers, following the production architecture of `HPRebar`:

```
┌────────────────────────────────────────────────────────────────────────┐
│                        WPF MVVM Presentation                           │
│  HPRebar/HPRebar/Beam Rebar/View/ & View Models/                       │
│  - BeamRebarViewModel (CommunityToolkit.Mvvm, ObservableObject)        │
│  - BeamRebarView (Modal Dialog, DynamicResource Light/Dark Themes)     │
│  - BeamElevationCanvas & BeamSectionCanvas (WPF DrawingContext Preview)│
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ triggers
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                     Revit Add-In Integration Layer                     │
│  HPRebar/HPRebar/Beam Rebar/ (Revit API 2025/2026, .NET 8)             │
│  - BeamRebarCommand : ExternalCommand                                  │
│  - BeamRebarOrchestrator (Sole Owner of TransactionGroup("Beam Rebar"))│
│  - BeamStackReader, BeamSolidFaceReader, BeamSupportFinder             │
│  - BeamStackValidator (Collinear axis, cross-section, solids)          │
│  - BeamStirrupCreator, BeamMainBarCreator, BeamAdditionalBarCreator... │
│  - BeamDetailViewCreator, BeamSectionViewCreator, DimensionCreator     │
│  - RevitUnits (mm <-> ft), LocalizationService, ThemeSwitcher          │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ converts to/from domain models
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                      Pure Domain Logic & Geometry                      │
│  HPRebar.Core/BeamRebar/ (netstandard2.0, Zero Revit References)       │
│  - Immutable Models: BeamSpan, BeamSupportNode, StirrupSpec, BarSpec   │
│  - Math/Calculators: BeamStirrupDistributionCalculator,               │
│    BeamMainBarCalculator, BeamAdditionalBarCalculator,                │
│    BeamSideBarCalculator, BeamSpecialBarCalculator, CanvasTransform    │
│  - Unit Tests: HPRebar.Core.Tests/BeamRebar/ (xUnit v3, 100% Pass)     │
└────────────────────────────────────────────────────────────────────────┘
```

---

## Feature Inventory

Every feature identified in Phase 0 survey is mapped directly to a milestone:

| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Continuous Beam Geometry Representation | Immutable `BeamSpan` and `BeamSupportNode` in mm with cross-sections, levels, offsets | M1 | survey |
| 2 | Pure Geometry Primitives & Tolerance | `Point3`, `Vector3`, `Polyline3`, and numerical comparison `Tolerance` in Core | M1 | survey |
| 3 | Uniform Stirrup Distribution | Mathematical spacing, bar count rounding, and centering offset for uniform layouts | M1 | survey |
| 4 | 3-Zone Stirrup Distribution | Dense support zones (L/4 or L/3) and sparse midspan distribution with boundary rounding | M1 | survey |
| 5 | Stirrup Max Count Guardrail | Enforcement of maximum 1002 bar positions to prevent Revit API exceptions | M1 | survey |
| 6 | Continuous Main Top & Bottom Bars | Polyline calculation with 90° downward/upward anchorage hooks into end supports | M1 | survey |
| 7 | Staggered Lap Splicing | 50% staggered lap splice calculation for spans > 11.7m (top in midspan, bottom at support) | M1 | survey |
| 8 | Additional Top Bars Over Supports | Negative moment bars extending L/3 or L/4 into adjacent clear spans, 2 vertical layers | M1 | survey |
| 9 | Additional Bottom Bars in Midspan | Positive moment bars starting at L/7 or L/8 from support faces, 2 vertical layers | M1 | survey |
| 10 | Deep Beam Side (Skin) Bars | Longitudinal skin bars spaced <= 300mm vertically when beam height h >= 700mm | M1 | survey |
| 11 | Transverse Anti-Buckling Cross-Ties | C-shaped hook ties connecting opposite side bars in deep beams | M1 | survey |
| 12 | Secondary Framing Hanging Stirrups | Concentrated stirrups cage at intersections with secondary framing beams | M1 | survey |
| 13 | Secondary Intersection Diagonal Ties | 45° diagonal inclination tie bars at secondary beam load concentration nodes | M1 | survey |
| 14 | Preview Canvas Coordinate Scaling | Math transformation converting continuous beam spans to WPF Canvas screen coordinates | M1 | survey |
| 15 | Domain Unit Test Suite | Comprehensive xUnit v3 tests covering all calculators and edge cases | M2 | survey |
| 16 | External Command & Selection Filter | `BeamRebarCommand` with category-safe `StructuralFramingSelectionFilter` | M3 | survey |
| 17 | Beam Stack Geometry & Face Reader | Extraction of Solids, top/bottom/side PlanarFaces, and centerline alignment | M3 | survey |
| 18 | Beam Support Finder | Querying intersecting columns, walls, and perpendicular girders to derive clear spans | M3 | survey |
| 19 | Beam Stack Validator | Validation of collinearity, same level, valid cross-sections, and solid integrity | M3 | survey |
| 20 | Atomic Transaction Group | `TransactionGroup("Beam Rebar")` in `BeamRebarOrchestrator` with auto-rollback | M3 | survey |
| 21 | Warning Suppression | `SwallowWarnings` implementing `IFailuresPreprocessor` for non-fatal overlap warnings | M3 | survey |
| 22 | Stirrup & Tie Rebar Creator | Native `Rebar.CreateFromRebarShape` with `ScaleToBox` and `SetLayoutAsNumberWithSpacing` | M3 | survey |
| 23 | Shape-Driven Longitudinal Bar Creator | `Rebar.CreateFromCurves` for main, additional, and side bars with standard shape codes | M3 | survey |
| 24 | Detail & Section Views Generation | Automated elevation `ViewSection.CreateDetail` and transverse cross-sections | M3 | survey |
| 25 | Dimensions & Annotations Creation | Automated section dimensioning (`SURFACE` -> `LINEAR` reference) and rebar tags | M3 | survey |
| 26 | Unit Conversion Boundary | Decimal feet <-> millimeters via `RevitUnits` using `UnitTypeId.Millimeters` | M3 | survey |
| 27 | Localization Service | English and Vietnamese UI strings via `LocalizationService` and `UiStrings` | M3 | survey |
| 28 | WPF MVVM View Models | `BeamRebarViewModel` with CommunityToolkit.Mvvm `ObservableObject`, tabs, commands | M4 | survey |
| 29 | WPF UI View & Dynamic Theming | Modal `BeamRebarView` using `{DynamicResource Brush.X}` for Revit light/dark switch | M4 | survey |
| 30 | Interactive Preview Canvas | Direct `DrawingContext` elevation and cross-section preview with debounce | M4 | survey |
| 31 | Ribbon Integration | Push button "Beam Rebar" on "Rebar" panel in `Application.cs` with icons | M5 | survey |
| 32 | Multi-Version Build Verification | Zero errors across `Debug.R25`, `Debug.R26`, and 100% test pass in `HPRebar.Core.Tests` | M5 | survey |

---

## Milestones

| # | Name | Scope | Dependencies | Status |
|---|------|-------|--------------|--------|
| M1 | Domain Logic & Geometry Engine | `HPRebar.Core/BeamRebar/` pure models, records, and calculators (netstandard2.0) | none | DONE |
| M2 | Domain Unit Test Suite | `HPRebar.Core.Tests/BeamRebar/` xUnit v3 comprehensive tests | M1 | DONE |
| M3 | Revit Add-In Feature Implementation | `HPRebar/HPRebar/Beam Rebar/` readers, validators, creators, views, annotations, orchestrator | M1 | DONE |
| M4 | WPF MVVM UI & Preview Canvas | `HPRebar/HPRebar/Beam Rebar/View/` and `View Models/` MVVM, theming, interactive canvas | M1, M3 | DONE |
| M5 | Ribbon Integration & Verification | `Application.cs` button, build verification on Debug.R25 & Debug.R26, test verification | M2, M3, M4 | DONE |

---

## Interface Contracts

### 1. HPRebar.Core.BeamRebar ↔ HPRebar.BeamRebar
- **Location**: `HPRebar.Core/BeamRebar/Models/`
- **Contract Types**:
  - `BeamSpan`: `(int Index, string Name, double LengthMm, double WidthMm, double HeightMm, double TopOffsetMm)`
  - `BeamSupportNode`: `(int Index, double CenterX, double WidthMm, SupportType Type)`
  - `BeamStirrupSpec`: `(StirrupLayout Layout, double DiameterMm, double CoverMm, IReadOnlyList<StirrupZone> Zones)`
  - `StirrupZone`: `(double StartX, double Length, double Spacing, int Count)`
  - `BeamBarCurve`: `(IReadOnlyList<Point3> Points, double DiameterMm, BarType Type, int Layer)`
- **Rules**:
  - All measurements in `HPRebar.Core` MUST be `double` representing millimetres.
  - Zero Revit imports (`Autodesk.Revit.*`) in `HPRebar.Core`.
  - Immutable C# 9 records and readonly structs.

### 2. BeamStackReader ↔ BeamRebarOrchestrator
- `BeamStackReader.Read(IReadOnlyList<FamilyInstance> selectedBeams)` returns validated `BeamContinuousStack` containing ordered `BeamSpan`s, `BeamSupportNode`s, coordinate transformation frames, and reference faces.

### 3. BeamRebarOrchestrator ↔ Revit Document
- `BeamRebarOrchestrator.Execute(BeamRebarInput input)` wraps all creation operations inside a single `using var group = new TransactionGroup(doc, "Beam Rebar")`.
- On success: `group.Assimilate()`.
- On exception or cancel: `group.RollBack()`.
- Sub-transactions use `SwallowWarnings` to prevent UI freezing on non-fatal warnings.

### 4. BeamRebarViewModel ↔ View
- Uses `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`.
- Properties decorated with `[ObservableProperty]`.
- Commands decorated with `[RelayCommand]`.
- Implements `IBeamRebarRunner` to decouple ViewModel from Revit API execution context.

---

## Code Layout

Adhering strictly to `AGENTS.md` and feature-folder rules:

```
HPRebar/
├── HPRebar.Core/
│   └── BeamRebar/
│       ├── Models/
│       │   ├── BeamSpan.cs
│       │   ├── BeamSupportNode.cs
│       │   ├── BeamContinuousStack.cs
│       │   ├── BeamStirrupSpec.cs
│       │   ├── BeamMainBarSpec.cs
│       │   ├── BeamAdditionalBarSpec.cs
│       │   ├── BeamSideBarSpec.cs
│       │   ├── BeamSpecialBarSpec.cs
│       │   ├── Point3.cs
│       │   ├── Vector3.cs
│       │   ├── Polyline3.cs
│       │   └── Enums.cs
│       ├── Calculators/
│       │   ├── BeamStirrupDistributionCalculator.cs
│       │   ├── BeamMainBarCalculator.cs
│       │   ├── BeamAdditionalBarCalculator.cs
│       │   ├── BeamSideBarCalculator.cs
│       │   ├── BeamSpecialBarCalculator.cs
│       │   └── BeamCanvasTransformCalculator.cs
│       └── Tolerance.cs
│
├── HPRebar.Core.Tests/
│   └── BeamRebar/
│       ├── TestBeamData.cs
│       ├── BeamStirrupDistributionCalculatorTests.cs
│       ├── BeamMainBarCalculatorTests.cs
│       ├── BeamAdditionalBarCalculatorTests.cs
│       ├── BeamSideBarCalculatorTests.cs
│       ├── BeamSpecialBarCalculatorTests.cs
│       └── BeamCanvasTransformCalculatorTests.cs
│
└── HPRebar/
    ├── Beam Rebar/
    │   ├── BeamRebarCommand.cs
    │   ├── BeamRebarOrchestrator.cs
    │   ├── BeamStackReader.cs
    │   ├── BeamSolidFaceReader.cs
    │   ├── BeamSupportFinder.cs
    │   ├── BeamStackValidator.cs
    │   ├── BeamRebarCreationService.cs
    │   ├── BeamStirrupCreator.cs
    │   ├── BeamMainBarCreator.cs
    │   ├── BeamAdditionalBarCreator.cs
    │   ├── BeamSideBarCreator.cs
    │   ├── BeamSpecialBarCreator.cs
    │   ├── BeamDetailViewCreator.cs
    │   ├── BeamSectionViewCreator.cs
    │   ├── BeamDimensionCreator.cs
    │   ├── BeamTagCreator.cs
    │   ├── StructuralFramingSelectionFilter.cs
    │   ├── RevitUnits.cs
    │   ├── LocalizationService.cs
    │   ├── ThemeSwitcher.cs
    │   ├── RebarFailureHandling.cs
    │   ├── RebarShapeResolver.cs
    │   ├── RebarTypeCatalog.cs
    │   ├── Models/
    │   │   ├── BeamRebarInput.cs
    │   │   ├── UiStrings.cs
    │   │   └── RebarTypeOption.cs
    │   ├── View/
    │   │   ├── BeamRebarView.xaml
    │   │   ├── BeamRebarView.xaml.cs
    │   │   ├── Controls/
    │   │   │   ├── BeamElevationCanvas.cs
    │   │   │   └── BeamSectionCanvas.cs
    │   │   └── Tabs/
    │   │       ├── GeometryTab.xaml
    │   │       ├── StirrupsTab.xaml
    │   │       ├── MainBarsTab.xaml
    │   │       ├── AddTopBarsTab.xaml
    │   │       ├── AddBottomBarsTab.xaml
    │   │       ├── SideBarsTab.xaml
    │   │       ├── SpecialBarsTab.xaml
    │   │       └── SettingsTab.xaml
    │   └── View Models/
    │       ├── BeamRebarViewModel.cs
    │       ├── IBeamRebarRunner.cs
    │       └── Tabs/
    │           ├── GeometryTabViewModel.cs
    │           ├── StirrupsTabViewModel.cs
    │           ├── MainBarsTabViewModel.cs
    │           ├── AddTopBarsTabViewModel.cs
    │           ├── AddBottomBarsTabViewModel.cs
    │           ├── SideBarsTabViewModel.cs
    │           ├── SpecialBarsTabViewModel.cs
    │           └── SettingsTabViewModel.cs
    └── Application.cs (Ribbon registration)
```
