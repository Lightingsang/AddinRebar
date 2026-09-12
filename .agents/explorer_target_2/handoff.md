# Handoff Report: Target Architecture & Foundation Rebar Blueprint

## 1. Observation

Direct inspection of `HPRebar` codebase revealed the following structural, architectural, and multi-version conventions:

### 1.1 Project Structure and Frameworks
- **`HPRebar.Core/HPRebar.Core.csproj`** (Lines 4-15):
  ```xml
  <TargetFramework>netstandard2.0</TargetFramework>
  <LangVersion>latest</LangVersion>
  <Nullable>enable</Nullable>
  <ImplicitUsings>disable</ImplicitUsings>
  <RootNamespace>HPRebar.Core</RootNamespace>
  <ItemGroup>
      <PackageReference Include="Polyfill" Version="11.0.1" PrivateAssets="all"/>
  </ItemGroup>
  ```
  *Rule*: `HPRebar.Core` contains zero references to `Autodesk.Revit.*` or UI assemblies. It compiles against `netstandard2.0` with `Polyfill` for modern C# features (`record`, `init`, `required`). All dimensions are in millimetres (`double`).

- **`HPRebar.Core.Tests/HPRebar.Core.Tests.csproj`** (Lines 4-23):
  ```xml
  <TargetFramework>net8.0</TargetFramework>
  <LangVersion>latest</LangVersion>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <RootNamespace>HPRebar.Core.Tests</RootNamespace>
  <OutputType>Exe</OutputType>
  <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
  <ItemGroup>
      <PackageReference Include="xunit.v3" Version="3.1.0"/>
      <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5"/>
  </ItemGroup>
  <ItemGroup>
      <ProjectReference Include="..\HPRebar.Core\HPRebar.Core.csproj"/>
  </ItemGroup>
  ```
  *Rule*: Test runner uses `xunit.v3` (v3.1.0) on .NET 8 via `Microsoft.Testing.Platform`. Executes pure domain logic without needing Revit.

- **`HPRebar/HPRebar/HPRebar.csproj`**:
  Uses `Nice3point.Revit.Sdk`, `CommunityToolkit.Mvvm` 8.4.0, `Serilog.Sinks.File`. Multi-targeted for `net48` (R23/R24), `net8.0-windows7.0` (R25/R26), and `net10.0-windows7.0` (R27).

### 1.2 Feature Folder Convention
From `AGENTS.md` and observed in `HPRebar/HPRebar/Beam Rebar/` and `HPRebar/HPRebar/Column Rebar/`:
- Folder name has spaces: `Foundation Rebar`
- Three mandatory subfolders:
  - `Models/`
  - `View/`
  - `View Models/` (with space)
- Service and ExternalCommand entry points reside at the feature root:
  - `FoundationRebarCommand.cs`
  - `FoundationSelectionFilter.cs`
  - `FoundationSolidFaceReader.cs`
  - `FoundationRebarValidator.cs`
  - `FoundationRebarCreationService.cs`
  - `FoundationRebarOrchestrator.cs`
- Namespaces are explicitly declared (file-scoped), spaces stripped, PascalCased:
  - Feature root: `namespace HPRebar.FoundationRebar;`
  - Models: `namespace HPRebar.FoundationRebar.Models;`
  - View Models: `namespace HPRebar.FoundationRebar.ViewModels;`
  - Views: `namespace HPRebar.FoundationRebar.Views;`
  - Core domain: `namespace HPRebar.Core.FoundationRebar.Models;` and `namespace HPRebar.Core.FoundationRebar.Calculators;`
  - Core tests: `namespace HPRebar.Core.Tests.FoundationRebar;`

### 1.3 Transaction Management Pattern
Observed in `BeamRebarOrchestrator.cs` (lines 59-94):
```csharp
using var group = new TransactionGroup(_document, "Beam Rebar");
group.Start();
try
{
    // Sub-transactions for operations
    using (var t = new Transaction(_document, "Create Reinforcement"))
    {
        t.Start();
        RebarFailureHandling.Apply(t);
        // mutate Revit DB
        t.Commit();
    }
    group.Assimilate(); // Flattens all sub-transactions into one Undo step
    return result;
}
catch (Exception ex)
{
    Log.Error(ex, "Failed; rolling back.");
    group.RollBack(); // Clean atomic rollback
    throw;
}
```
In `RebarFailureHandling.cs`:
`Apply(Transaction transaction)` attaches an `IFailuresPreprocessor` (`SwallowWarnings`) to delete harmless warnings (like rebar extending slightly outside host volume or minor overlaps), preventing commit abortion.

### 1.4 Ribbon Registration Details
Observed in `HPRebar/HPRebar/Application.cs` (lines 43-59):
- Tab Name: `"HPRebar"`
- Panel Name: `"Rebar"`
- Method: `rebarPanel.AddPushButton<T>("Text").SetImage("...").SetLargeImage("...")`
- Small Icon: `"/HPRebar;component/Resources/Icons/RibbonIcon16.png"`
- Large Icon: `"/HPRebar;component/Resources/Icons/RibbonIcon32.png"`
- Existing buttons: `ColumnRebarCommand` ("Column Rebar") and `BeamRebarCommand` ("Beam Rebar").

### 1.5 Multi-Version Compilation Directives
Observed in `Column Rebar/MainBarCreator.cs` (line 32), `Column Rebar/ThemeSwitcher.cs` (line 72), and `Beam Rebar/ThemeSwitcher.cs` (line 51):
- Conditional symbol: `#if REVIT2024_OR_GREATER`
- Tag comment: `// Multi-version: <Topic>`
- Pattern for `ElementId`:
  ```csharp
  // Multi-version: ElementId
  #if REVIT2024_OR_GREATER
      long id = element.Id.Value;
  #else
      int id = element.Id.IntegerValue;
  #endif
  ```
- Pattern for `UIThemeManager`:
  ```csharp
  // Multi-version: Theme
  #if REVIT2024_OR_GREATER
      return Autodesk.Revit.UI.UIThemeManager.CurrentTheme == Autodesk.Revit.UI.UITheme.Dark;
  #else
      return true;
  #endif
  ```

### 1.6 WPF Theming & MVVM
Observed in `Theme.xaml`, `BeamRebarView.xaml`, and `ColumnRebarView.xaml`:
- Window resource merged dictionary:
  `<ResourceDictionary Source="pack://application:,,,/HPRebar;component/Resources/Themes/Theme.xaml"/>`
- Dynamic brushes & tokens:
  `Background="{DynamicResource Brush.Background}"`
  `Foreground="{DynamicResource Brush.Foreground.Primary}"`
  `FontFamily="{DynamicResource Font.Family.Default}"`
  `FontSize="{DynamicResource Font.Size.Body}"`
  `Margin="{DynamicResource Spacing.Large}"`
  `Style="{DynamicResource Button.Primary}"`
  `Style="{DynamicResource TextBox.Number}"`
- MVVM Toolkit:
  - ViewModel: `sealed partial class ... : ObservableObject`
  - Field: `[ObservableProperty] private double _spacing;` -> emits `public double Spacing { get; set; }`
  - Command: `[RelayCommand] private void Run()` -> emits `public IRelayCommand RunCommand { get; }`
- Code-behind: strictly `InitializeComponent();` + `DataContext = vm;`. Zero business logic.
- Runtime theme switch: `ThemeSwitcher.ApplyFromRevit(view);` before `view.ShowDialog();`.

---

## 2. Logic Chain

1. **Separation of Concerns**: Because Revit's `Document` and `Element` are sealed and unmockable, testing rebar geometry inside Revit process is slow and complex. Placing geometry calculations in `HPRebar.Core` (as pure records and stateless static methods in mm) allows 100% unit test coverage in `HPRebar.Core.Tests` via `dotnet test` in < 2 seconds.
2. **Revit Feature Layer Decoupling**: The UI layer (`FoundationRebarViewModel`) should not hold direct references to `Document` or Revit transaction objects. Following `IBeamRebarRunner` / `IColumnRebarRunner`, the ViewModel interacts via `FoundationSession` and an execution delegate or runner, which invokes `FoundationRebarOrchestrator`.
3. **Selection & Geometry Extraction**: The user selects a foundation slab (`Floor` or `OST_StructuralFoundation`). `FoundationSelectionFilter` ensures only valid elements are picked. `FoundationSolidFaceReader` extracts the geometry (Solid, top face normal $(0,0,1)$, bottom face normal $(0,0,-1)$, thickness $T$, boundary dimensions $L, W$, and local axes $X, Y$).
4. **Coordinate Boundary via `PointMapper`**: Local coordinates are defined in millimetres:
   - Origin: Minimum corner of bounding box on bottom face.
   - Vector $X$: Along length edge.
   - Vector $Y$: Transverse orthogonal axis ($Z \times X$).
   - Vector $Z$: $(0, 0, 1)$ upwards.
   `PointMapper` translates local `(x, y, z)` mm directly into Revit world `XYZ` (decimal feet) using `UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters)`.
5. **Pure Domain Calculations**:
   - `FoundationBoundaryCalculator`: Determines effective placement area by subtracting `SideCover` from edges.
   - `FoundationMeshCalculator`:
     - Direction X bars run along length $X$, distributed along $Y$ with spacing $s_X$.
     - Direction Y bars run along width $Y$, distributed along $X$ with spacing $s_Y$.
     - Bottom Mat:
       - Layer 1 (Dir X): $Z = c_{bottom} + d_X / 2$.
       - Layer 2 (Dir Y, resting on top of Layer 1): $Z = c_{bottom} + d_X + d_Y / 2$.
       - Hooks (Hook90Up): End points bend up by hook length $H_L$.
     - Top Mat (if enabled):
       - Layer 2 (Dir Y, top-most): $Z = T - c_{top} - d_Y / 2$.
       - Layer 1 (Dir X, under Dir Y): $Z = T - c_{top} - d_Y - d_X / 2$.
       - Hooks (Hook90Down): End points bend down by hook length $H_L$.
6. **Rebar Instantiation**:
   `FoundationRebarCreationService` converts `FoundationBarCurve` point sequences into Revit curves (`Line.CreateBound(p0, p1)`), selects `RebarBarType` from `RebarTypeCatalog`, and calls `Rebar.CreateFromCurves` within the sub-transaction.
7. **Transaction Group Integrity**: `FoundationRebarOrchestrator` starts `TransactionGroup("Foundation Rebar")`. If creation succeeds, `group.Assimilate()`; on exception, `group.RollBack()`.

---

## 3. Caveats

1. **Target Element Scope**: Per user requirement (Phương án A), `Foundation Rebar` targets planar horizontal slab foundations modeled as `Floor` (or `OST_StructuralFoundation` slab). Stepped, warped, or sloped slabs are excluded by `FoundationRebarValidator`.
2. **Rebar Creation API**: `Rebar.CreateFromCurves` is used. For straight or hooked bars created from curves, Revit generates standard shape-driven or free-form bars. `RebarFailureHandling` must be applied to suppress harmless clearance warnings.
3. **No DI Container in Host**: Like `ColumnRebarCommand` and `BeamRebarCommand`, `FoundationRebarCommand` constructs services and ViewModels directly (manual composition). Do not introduce an external DI container into this feature.
4. **Revit Lock During Compilation**: When Revit is running, the deployed DLL is locked. Always test compilation with `-p:DeployAddin=false`.

---

## 4. Conclusion & Concrete Specification

### 4.1 Target Contracts for `HPRebar.Core/FoundationRebar/`

#### Models (`HPRebar.Core.FoundationRebar.Models`)
1. **`Point3` & `Vector3`**:
   Structs representing 3D coordinates and vectors in millimetres.
2. **`FoundationGeometrySnapshot`** (`sealed record`):
   ```csharp
   public sealed record FoundationGeometrySnapshot
   {
       public double LengthMm { get; init; }
       public double WidthMm { get; init; }
       public double ThicknessMm { get; init; }
       public double TopElevationMm { get; init; }
       public double BottomElevationMm { get; init; }
       public Point3 OriginMm { get; init; }
       public Vector3 DirectionX { get; init; }
       public Vector3 DirectionY { get; init; }
       public Vector3 DirectionZ { get; init; }
   }
   ```
3. **`FoundationRebarSpec`** (`sealed record`):
   ```csharp
   public enum FoundationHookType { None = 0, Hook90Up = 1, Hook90Down = 2 }

   public sealed record FoundationRebarSpec
   {
       // Bottom Mat (Lưới dưới)
       public double BottomDiameterDirX { get; init; } = 12.0;
       public double BottomSpacingDirX { get; init; } = 150.0;
       public FoundationHookType BottomHookDirX { get; init; } = FoundationHookType.Hook90Up;
       public double BottomHookLengthDirX { get; init; } = 200.0;
       public string BottomBarTypeNameDirX { get; init; } = string.Empty;

       public double BottomDiameterDirY { get; init; } = 12.0;
       public double BottomSpacingDirY { get; init; } = 150.0;
       public FoundationHookType BottomHookDirY { get; init; } = FoundationHookType.Hook90Up;
       public double BottomHookLengthDirY { get; init; } = 200.0;
       public string BottomBarTypeNameDirY { get; init; } = string.Empty;

       // Top Mat (Lưới trên)
       public bool EnableTopMat { get; init; } = false;
       public double TopDiameterDirX { get; init; } = 10.0;
       public double TopSpacingDirX { get; init; } = 200.0;
       public FoundationHookType TopHookDirX { get; init; } = FoundationHookType.Hook90Down;
       public double TopHookLengthDirX { get; init; } = 200.0;
       public string TopBarTypeNameDirX { get; init; } = string.Empty;

       public double TopDiameterDirY { get; init; } = 10.0;
       public double TopSpacingDirY { get; init; } = 200.0;
       public FoundationHookType TopHookDirY { get; init; } = FoundationHookType.Hook90Down;
       public double TopHookLengthDirY { get; init; } = 200.0;
       public string TopBarTypeNameDirY { get; init; } = string.Empty;

       // Concrete Covers (Lớp bảo vệ bê tông)
       public double SideCoverMm { get; init; } = 50.0;
       public double BottomCoverMm { get; init; } = 50.0;
       public double TopCoverMm { get; init; } = 50.0;

       public string PartitionName { get; init; } = "Foundation";
   }
   ```
4. **`FoundationBarCurve`** (`sealed record`):
   Contains `BarIndex`, `Mat` (Bottom/Top), `Direction` (DirX/DirY), `LayerIndex` (1/2), `DiameterMm`, `Points` (`IReadOnlyList<Point3>`), and `TotalLengthMm`.
5. **`FoundationMeshResult`** (`sealed record`):
   Contains `IReadOnlyList<FoundationBarCurve> Bars`, with helper properties `BottomBars`, `TopBars`, `TotalCount`.
6. **`ValidationResult`** (`sealed record`):
   `IsSuccess`, `ErrorMessage`, `Warnings`, static `Ok()`, `Fail(string message)`.

#### Calculators (`HPRebar.Core.FoundationRebar.Calculators`)
1. **`FoundationBoundaryCalculator`**:
   Computes usable layout dimensions:
   - $X_{range} = \text{LengthMm} - 2 \times \text{SideCoverMm}$
   - $Y_{range} = \text{WidthMm} - 2 \times \text{SideCoverMm}$
   Throws `ArgumentOutOfRangeException` if cover exceeds half of length/width.
2. **`FoundationMeshCalculator`**:
   Computes all bar polylines for Bottom Mat (Dir X, Dir Y) and Top Mat (Dir X, Dir Y):
   - Handles exact spacing vs centered leftover slack.
   - Enforces correct non-clashing Z-stacking.
   - Appends 90-degree hooks if specified.
   - Validates thickness vs covers + bar diameters.
   - Enforces Revit maximum count $\le 1000$ bars.

---

### 4.2 Target Contracts for `HPRebar.Core.Tests/FoundationRebar/`

- **`HPRebar.Core.Tests/FoundationRebar/TestFoundationData.cs`**:
  Fixtures generating standard rectangular foundation snapshots (e.g. 3000x2000x500 mm, 6000x4000x800 mm, rotated 45°).
- **`HPRebar.Core.Tests/FoundationRebar/FoundationMeshCalculatorTests.cs`**:
  - `SpacingDivision_DivisibleAndNonDivisible_DistributesCorrectCountAndCentersSlack`
  - `OrthogonalLayers_HaveCorrectZOffsets_NoClashing`
  - `TopMat_DisabledByDefault_ProducesOnlyBottomBars`
  - `TopMat_Enabled_ProducesBothBottomAndTopMats`
  - `Hooks_Hook90UpAndDown_GenerateCorrectVerticalLegCoordinates`
  - `RotatedFoundation_LocalFrameCalculationsAreInvariant`
  - `Validation_ThrowsWhenThicknessTooSmallForCoversAndBars`
  - `Validation_ThrowsWhenSpacingZeroOrNegative`
  - `Validation_ThrowsWhenCoverZeroOrNegative`

---

### 4.3 Target Contracts for `HPRebar/HPRebar/Foundation Rebar/`

#### Directory Layout
```
HPRebar/HPRebar/Foundation Rebar/
├── FoundationRebarCommand.cs          <-- ExternalCommand entry
├── FoundationSelectionFilter.cs       <-- Category filter (Floor / OST_Floors)
├── FoundationSolidFaceReader.cs       <-- Solid and PlanarFace extractor
├── FoundationRebarValidator.cs        <-- Horizontal & thickness validation
├── FoundationRebarCreationService.cs  <-- Rebar.CreateFromCurves
├── FoundationRebarOrchestrator.cs     <-- TransactionGroup owner
├── PointMapper.cs                     <-- mm Point3 <-> Revit XYZ feet
├── RevitUnits.cs                      <-- MmToFt, FtToMm
├── RebarFailureHandling.cs            <-- Swallows non-fatal Revit warnings
├── ThemeSwitcher.cs                   <-- Runtime Dark/Light theme sync
├── RevitDialogs.cs                    <-- TaskDialog wrappers
├── Models/
│   ├── FoundationSession.cs           <-- Observable UI session
│   ├── RebarTypeInfo.cs               <-- RebarBarType wrapper
│   └── ValidationResult.cs            <-- Feature-level validation result
├── View/
│   ├── FoundationRebarView.xaml (+ .cs)     <-- Main Dialog Window
│   ├── FoundationGeometryView.xaml (+ .cs)  <-- Geometry Tab
│   └── FoundationSettingView.xaml (+ .cs)   <-- Reinforcement Settings Tab
└── View Models/
    ├── FoundationRebarViewModel.cs    <-- Main ViewModel with RunCommand
    ├── FoundationGeometryViewModel.cs <-- Geometry Tab ViewModel
    └── FoundationSettingViewModel.cs  <-- Settings Tab ViewModel
```

#### Key Class Blueprints

1. **`FoundationRebarCommand`**:
   - Attribute: `[Transaction(TransactionMode.Manual)]`, `[UsedImplicitly]`.
   - Derives: `ExternalCommand`.
   - `Execute()`:
     - `uiDocument.Selection.PickObject(ObjectType.Element, new FoundationSelectionFilter(), "Select a foundation floor")`
     - Validates with `FoundationRebarValidator.Validate(doc, element)`
     - Reads geometry: `var snapshot = FoundationSolidFaceReader.Read(element)`
     - Loads bar types: `RebarTypeCatalog.LoadBarTypes(doc)`
     - Builds `FoundationSession`
     - Instantiates `FoundationRebarViewModel` & `FoundationRebarView`
     - `new WindowInteropHelper(view).Owner = Application.MainWindowHandle;`
     - `ThemeSwitcher.ApplyFromRevit(view);`
     - `view.ShowDialog();`

2. **`FoundationSolidFaceReader`**:
   - Inspects `element.get_Geometry(new Options { ComputeReferences = true, DetailLevel = ViewDetailLevel.Fine })`.
   - Extracts `Solid` (Volume $> 10^{-6}$).
   - Finds Top Face (`f.FaceNormal.Z > 1.0 - 1e-6`) and Bottom Face (`f.FaceNormal.Z < -1.0 + 1e-6`).
   - Thickness $T = \text{TopFace.Origin.Z} - \text{BottomFace.Origin.Z}$.
   - Evaluates outer planar loop edges to establish principal axes:
     - Axis X: direction along the longest edge.
     - Axis Y: $Z \times X$.
     - Dimensions $L$ and $W$ from projection onto Axis X and Axis Y.
   - Instantiates `PointMapper(minCorner, axisX, axisY, XYZ.BasisZ)` and `FoundationGeometrySnapshot`.

3. **`FoundationRebarCreationService`**:
   - Iterates through `FoundationMeshResult.Bars`.
   - Converts `bar.Points` to `Curve` list via `Line.CreateBound(mapper.ToXyz(p0), mapper.ToXyz(p1))`.
   - Normal vector: for Dir X bars with vertical hooks, normal is Axis Y; for Dir Y bars, normal is Axis X. For straight bars, normal is perpendicular to the line.
   - Calls `Rebar.CreateFromCurves(doc, RebarStyle.Standard, barType, null, null, host, normal, curves, ...)`
   - Sets `"Partition"` parameter to `spec.PartitionName`.

4. **`FoundationRebarOrchestrator`**:
   - Manages `TransactionGroup("Foundation Rebar")`.
   - Runs `FoundationRebarCreationService.Create(...)` inside an inner `Transaction("Create Foundation Rebar")`.
   - Applies `RebarFailureHandling.Apply(t)`.
   - Assimilates on success; rolls back on exception.

5. **`Application.cs` Registration**:
   ```csharp
   var rebarPanel = Application.CreatePanel("Rebar", "HPRebar");

   rebarPanel.AddPushButton<FoundationRebarCommand>("Foundation Rebar")
       .SetImage("/HPRebar;component/Resources/Icons/RibbonIcon16.png")
       .SetLargeImage("/HPRebar;component/Resources/Icons/RibbonIcon32.png");
   ```

---

## 5. Verification Method

### 5.1 Automated Compilation
Run from `HPRebar/`:
```bash
dotnet build HPRebar/HPRebar.csproj -c Debug.R25 -p:DeployAddin=false
dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false
```
**Pass Criterion**: 0 errors, 0 warnings related to missing types.

### 5.2 Automated Domain Unit Tests
Run from repository root:
```bash
dotnet test HPRebar/HPRebar.Core.Tests
```
**Pass Criterion**: 100% pass rate (241 baseline tests + all new Foundation Rebar tests, 0 failed, 0 skipped).

### 5.3 Architectural Invariants Verification
1. `HPRebar.Core/FoundationRebar/` must contain zero `using Autodesk.Revit.*`.
2. All namespaces in `HPRebar.Core/FoundationRebar/` match `HPRebar.Core.FoundationRebar.*`.
3. All namespaces in `HPRebar/HPRebar/Foundation Rebar/` match `HPRebar.FoundationRebar.*`.
4. Transaction handling strictly uses `TransactionGroup` with rollback in `catch` and `Assimilate()` in success path.
5. All UI bindings utilize `{DynamicResource ...}` matching `Theme.xaml`.

### 5.4 Invalidation Conditions
- Introduction of any `Autodesk.Revit.*` reference in `HPRebar.Core`.
- Underscores in C# namespaces (e.g. `HPRebar.Foundation_Rebar`).
- Hardcoded colors, margins, or fonts in XAML files instead of `{DynamicResource ...}`.
- Deprecated Revit API calls (`DisplayUnitType`, old `CreateFromCurves` signatures).
