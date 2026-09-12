# Scope: Foundation Rebar

## Architecture
- **Layer 1: Pure Domain Logic** (`HPRebar.Core/FoundationRebar/`)
  - Target: `netstandard2.0`
  - Zero dependencies on `Autodesk.Revit.*`
  - Immutable records, Cartesian vectors/points in mm (`Point3`, `Vector3`, `Polyline3`), stateless calculators (`FoundationBoundaryCalculator`, `FoundationMeshCalculator`, `FoundationGeometryCalculator`).
  - Strict 4-layer vertical stacking and coplanar 3D curve generation with anchorage hooks.
- **Layer 2: Pure Domain Unit Testing** (`HPRebar.Core.Tests/FoundationRebar/`)
  - Target: `net8.0` with xUnit v3 (`Microsoft.Testing.Platform` runner).
  - Exhaustive testing of spacing divisibility, centering margins, vertical elevations, rotated coordinate frames, and edge-case guardrails.
- **Layer 3: Revit Feature Services & Infrastructure** (`HPRebar/HPRebar/Foundation Rebar/`)
  - Multi-targeted for Revit 2023–2027 (.NET Framework 4.8 / .NET 8 / .NET 10).
  - Selection filter (`Floor`), SolidFace reader, Oriented Bounding Box (OBB) local axes.
  - Rebar creation via modern `Rebar.CreateFromCurves` with warning suppression.
  - Atomic `TransactionGroup("Foundation Rebar")` rollback/assimilate lifecycle.
  - Multi-version `ElementId.Value` vs `IntegerValue`.
- **Layer 4: User Interface (WPF MVVM)** (`HPRebar/HPRebar/Foundation Rebar/View/` & `View Models/`)
  - `CommunityToolkit.Mvvm` (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`).
  - 100% `{DynamicResource}` token theming via `Theme.xaml`, dark/light runtime switching.
  - Tabbed interface: Main/Overview, Geometry, Settings.
- **Layer 5: Application Ribbon Integration** (`HPRebar/HPRebar/Application.cs`)
  - PushButton "Foundation Rebar" on panel "Rebar" under tab "HPRebar".

---

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| F01 | `FoundationGeometrySnapshot` | Immutable record for length, width, thickness, top/bottom Z, origin, local orthonormal axes (Ux, Uy, Uz) | M1 | Survey |
| F02 | `FoundationRebarSpec` | Configuration record: diameters, spacings, covers (top, bottom, side), TopMat toggle, hook parameters | M1 | Survey |
| F03 | `FoundationBoundaryCalculator` | Calculates effective 2D placement boundary after subtracting side cover | M1 | Survey |
| F04 | `FoundationMeshCalculator` | Calculates bar counts, spacing intervals, and discrete distribution positions along distribution axes | M1 | Survey |
| F05 | Layer Stacking Elevations | Calculates exact 4-layer vertical elevations (Bottom X outer, Bottom Y inner, Top Y inner, Top X outer) | M1 | Survey |
| F06 | Anchorage Hooks | Generates 90° upward hooks for bottom bars and downward hooks for top bars with safety clamping | M1 | Survey |
| F07 | Rotated Plan Orientation | Computes affine transformation from local coordinates to world 3D space for arbitrary rotation angle | M1 | Survey |
| F08 | Pure Domain Guardrails | Validates spacing > 0, slab thickness >= minimum required clearance, boundary >= 2*cover | M1 | Survey |
| F09 | Unit Tests: Distribution | xUnit tests for spacing divisibility, count, and centering margins | M2 | Survey |
| F10 | Unit Tests: Layer Stacking | xUnit tests for vertical Z elevations, clearance gap, and no overlap | M2 | Survey |
| F11 | Unit Tests: Rotation | xUnit tests for arbitrary rotation angles, vector orthonormality, and coplanarity | M2 | Survey |
| F12 | Unit Tests: Guardrails | xUnit tests for negative spacing, insufficient thickness, and boundary limits | M2 | Survey |
| F13 | `FoundationSelectionFilter` | Selection filter restricting interactive picking strictly to `Floor` elements | M3 | Survey |
| F14 | `FoundationSolidFaceReader` | Extracts Solid, Top/Bottom PlanarFaces, thickness, OBB bounding box, and local axes from Revit Floor | M3 | Survey |
| F15 | `FoundationRebarValidator` | Validates floor horizontal orientation, valid geometry, and physical dimensions | M3 | Survey |
| F16 | `FoundationRebarCreationService` | Creates Revit `Rebar` elements from domain curves using `Rebar.CreateFromCurves` | M3 | Survey |
| F17 | `FoundationRebarOrchestrator` | Manages atomic `TransactionGroup("Foundation Rebar")` with rollback on cancel/error and assimilate on success | M3 | Survey |
| F18 | `FoundationSession` | Session model bridging Revit data, domain geometry, and UI state | M3 | Survey |
| F19 | Multi-Version Compatibility | Conditional compilation for `ElementId.Value` vs `IntegerValue` across R23–R27 | M3 | Survey |
| F20 | MVVM ViewModels | `FoundationRebarViewModel`, `FoundationGeometryViewModel`, `FoundationSettingViewModel` | M4 | Survey |
| F21 | WPF Themed Views | `FoundationRebarView.xaml`, `FoundationGeometryView.xaml`, `FoundationSettingView.xaml` with `{DynamicResource}` | M4 | Survey |
| F22 | Ribbon Registration | Registers "Foundation Rebar" button with 16px and 32px icons on panel "Rebar" in `Application.cs` | M5 | Survey |
| F23 | Multi-Version Build Verification | Verified clean build on `Debug.R25` and `Debug.R26` (.NET 8) with zero errors and zero warnings | M5 | Survey |

---

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Pure Domain Logic & Geometry Engine | `HPRebar.Core/FoundationRebar/` (Models & Calculators) | None | DONE |
| M2 | Pure Domain xUnit Test Suite | `HPRebar.Core.Tests/FoundationRebar/` (Test Suites) | M1 | DONE |
| M3 | Revit Feature Layer & Geometry Services | `HPRebar/HPRebar/Foundation Rebar/` (Readers, Services, Orchestrator) | M1 | DONE |
| M4 | WPF MVVM UI & ViewModels | `HPRebar/HPRebar/Foundation Rebar/View/` & `View Models/` | M1, M3 | DONE |
| M5 | Ribbon Integration & Verification | `HPRebar/HPRebar/Application.cs` & multi-version builds | M3, M4 | DONE |

---

## Interface Contracts

### `HPRebar.Core.FoundationRebar`
- Models:
  - `Point3(double X, double Y, double Z)` (or reuse existing `HPRebar.Core.FoundationRebar.Models.Point3` with vector operations)
  - `Vector3(double X, double Y, double Z)`
  - `Polyline3(IReadOnlyList<Point3> Points)`
  - `FoundationGeometrySnapshot`:
    ```csharp
    public sealed record FoundationGeometrySnapshot(
        double Length,
        double Width,
        double Thickness,
        double TopZ,
        double BottomZ,
        Point3 Origin,
        Vector3 LocalX,
        Vector3 LocalY,
        Vector3 LocalZ);
    ```
  - `FoundationRebarSpec`:
    ```csharp
    public sealed record FoundationRebarSpec(
        double DiameterBottomX,
        double DiameterBottomY,
        double DiameterTopX,
        double DiameterTopY,
        double SpacingBottomX,
        double SpacingBottomY,
        double SpacingTopX,
        double SpacingTopY,
        double CoverTop,
        double CoverBottom,
        double CoverSide,
        bool IsTopMatEnabled,
        FoundationHookType HookType,
        double HookLength);
    ```
  - `RebarMeshResult`:
    ```csharp
    public sealed record RebarMeshResult(
        IReadOnlyList<Polyline3> BottomBarsX,
        IReadOnlyList<Polyline3> BottomBarsY,
        IReadOnlyList<Polyline3> TopBarsX,
        IReadOnlyList<Polyline3> TopBarsY,
        FoundationMeshStatistics Statistics);
    ```
- Calculators:
  - `FoundationBoundaryCalculator`: `Calculate(double length, double width, double coverSide)`
  - `FoundationMeshCalculator`: `Calculate(FoundationGeometrySnapshot snapshot, FoundationRebarSpec spec)`
  - `FoundationValidationCalculator`: `Validate(FoundationGeometrySnapshot snapshot, FoundationRebarSpec spec)`

### `HPRebar.FoundationRebar` (Revit Layer)
- `FoundationSolidFaceReader`:
  `FoundationGeometrySnapshot Read(Element floor)`
- `FoundationRebarCreationService`:
  `void CreateRebars(Document doc, Element hostFloor, RebarMeshResult mesh, FoundationRebarBarTypes barTypes)`
- `FoundationRebarOrchestrator`:
  `bool Execute(UIApplication uiApp)`
