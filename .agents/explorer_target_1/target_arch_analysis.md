# Target Architecture Analysis: HPRebar

**Author**: `explorer_target_1`  
**Date**: 2026-09-07  
**Scope**: HPRebar repository (`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar`)  
**Purpose**: Architectural exploration and specification of standards, conventions, and reference patterns in `HPRebar` to guide the migration of `R02_BeamsRebar` into `HPRebar.Core/BeamRebar` and `HPRebar/Beam Rebar`.

---

## 1. Executive Summary

`HPRebar` is a multi-version Autodesk Revit Add-in targeting Revit 2023 through 2027 (.NET Framework 4.8 for R23–R24, .NET 8.0-windows7.0 for R25–R26, and .NET 10.0-windows7.0 for R27). It utilizes `Nice3point.Revit.Sdk` (v6.2.3), `CommunityToolkit.Mvvm` (v8.4.0), `Serilog` (v4.4.0), and `xunit.v3` (v3.1.0) under `Microsoft.Testing.Platform`.

The solution follows a strict multi-tier architecture designed around four fundamental tenets:
1. **Zero-Revit Domain Core (`HPRebar.Core`)**: All structural calculations, bar scheduling, rebar polylines, tie distributions, and canvas scaling are implemented as pure, deterministic C# records and stateless static calculator methods in `netstandard2.0` with zero dependencies on `Autodesk.Revit.*` or UI assemblies.
2. **xUnit v3 Pure Domain Test Suite (`HPRebar.Core.Tests`)**: 100% unit test coverage of domain logic using xUnit v3 running independently of Revit processes or licenses.
3. **Isolated Feature-Folder Convention (`HPRebar/HPRebar/<Feature Name>/`)**: Each workflow (e.g. `Column Rebar`, and the upcoming `Beam Rebar`) is self-contained in a dedicated directory with three required subfolders: `Models/`, `View/`, `View Models/`, alongside flat root orchestrators, readers, creators, and validators.
4. **Atomic Transaction Grouping and Resilient Failure Handling**: High-level commands wrap operations in a single `TransactionGroup` that assimilates on success into one undo operation and cleanly rolls back on any error. Warning preprocessors swallow non-fatal Revit warnings to prevent UI-blocking dialogs.

---

## 2. Project & Solution Build Structure

### 2.1 Solution File (`HPRebar.slnx`)
The repository uses the XML-based solution format (`HPRebar.slnx`):
- Pinned .NET SDK in `global.json`: `10.0.300` with test runner `Microsoft.Testing.Platform`.
- Configurations: `Debug.R23`, `Debug.R24`, `Debug.R25`, `Debug.R26`, `Debug.R27` (and corresponding `Release.R*` variants).
- Projects in solution:
  - `HPRebar/HPRebar.csproj`: Main Revit Add-In project.
  - `HPRebar.Core/HPRebar.Core.csproj`: Pure domain class library (`netstandard2.0`).
  - `HPRebar.Core.Tests/HPRebar.Core.Tests.csproj`: Pure unit tests (`net8.0`, xUnit v3).
  - `HPRebar.Tests/HPRebar.Tests.csproj`: TUnit in-process integration tests (`<Build Project="false"/>` in `.slnx`).
  - Automation projects under `/Automation/`: `build/Build.csproj` and `install/Installer.csproj` (`<Build Project="false"/>`).

### 2.2 Compilation & Execution Flags
- Target frameworks dynamically selected by SDK suffix:
  - `R23` / `R24`: `net48`
  - `R25` / `R26`: `net8.0-windows7.0`
  - `R27`: `net10.0-windows7.0`
- Essential build command:
  ```powershell
  dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false
  dotnet build HPRebar.slnx -c Debug.R25 -p:DeployAddin=false
  ```
  *Note*: `-p:DeployAddin=false` is mandatory during build validation when Revit is active to avoid file lock contention in `%AppData%\Autodesk\Revit\Addins\<version>\`.
- Unit test execution:
  ```powershell
  dotnet test HPRebar/HPRebar.Core.Tests
  ```

---

## 3. HPRebar.Core Architecture (Pure Domain & Geometry)

### 3.1 Project Configuration (`HPRebar.Core.csproj`)
```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>netstandard2.0</TargetFramework>
        <LangVersion>latest</LangVersion>
        <Nullable>enable</Nullable>
        <ImplicitUsings>disable</ImplicitUsings>
        <RootNamespace>HPRebar.Core</RootNamespace>
        <Configurations>Debug;Release</Configurations>
    </PropertyGroup>
    <ItemGroup>
        <!-- Enables record, init, and required on netstandard2.0 without runtime dependencies -->
        <PackageReference Include="Polyfill" Version="11.0.1" PrivateAssets="all"/>
    </ItemGroup>
</Project>
```

### 3.2 Domain Conventions & Characteristics
1. **Strict Zero-Revit Reference**:
   - Zero `using Autodesk.Revit.*` or `Nice3point.Revit.*`.
   - Never reference Revit `XYZ`, `Curve`, `Document`, `ElementId`, `Parameter`, or `Units`.
2. **Local Coordinate Frames & Units**:
   - Every coordinate, length, width, cover, spacing, and diameter is in **millimetres** (`double`).
   - Coordinate representations:
     - `public readonly struct Point3`: Holds `(double X, double Y, double Z)` in local member coordinate space.
     - `public readonly struct PlanPoint`: Holds 2D coordinates `(double X, double Y)`.
3. **Data Immutability**:
   - Models are declared as `public sealed record <Name>` with `{ get; init; }`.
   - Default initialization for nested collections: `= new List<T>();` or `= new();`.
4. **Stateless Calculators & Pure Functions**:
   - Functional calculation classes are `public static class <Name>Calculator` (or `Builder`).
   - Static methods receive pure records / primitive values and return immutable results (`IReadOnlyList<T>`, tuples, records).
5. **Numerical Precision & Floating-point Comparisons**:
   - `HPRebar.Core.ColumnRebar.Tolerance`:
     ```csharp
     public static class Tolerance
     {
         public const double Default = 1.0e-9;
         public static bool AreEqual(double first, double second, double tolerance = Default) =>
             second - tolerance < first && first < second + tolerance;
     }
     ```
6. **Pre-condition Validation & Error Handling**:
   - Methods validate inputs using standard exceptions: `ArgumentNullException` and `ArgumentOutOfRangeException`.
   - Early boundary checks guard against downstream Revit limitations before any Revit transaction opens (e.g. `StirrupDistributionCalculator.MaxBarPositions = 1002`).

---

## 4. HPRebar.Core.Tests Conventions (xUnit v3)

### 4.1 Project Configuration (`HPRebar.Core.Tests.csproj`)
```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <LangVersion>latest</LangVersion>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <RootNamespace>HPRebar.Core.Tests</RootNamespace>
        <Configurations>Debug;Release</Configurations>
        <IsPackable>false</IsPackable>
        <OutputType>Exe</OutputType>
        <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="xunit.v3" Version="3.1.0"/>
        <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5"/>
    </ItemGroup>
    <ItemGroup>
        <ProjectReference Include="..\HPRebar.Core\HPRebar.Core.csproj"/>
    </ItemGroup>
</Project>
```

### 4.2 Test Patterns & Best Practices
- **Class Structure**: `public sealed class <CalculatorName>Tests`.
- **Precision Constant**: `private const int Precision = 6;` for floating point comparisons.
- **Fixture Builders**:
  - Encapsulated in `internal static class Test<Entity>` (e.g. `TestSections.cs`).
  - Provides factory methods creating baseline records (e.g. `TestSections.Rectangle(...)`).
  - Tests customize fixtures cleanly using C# 9+ record non-destructive mutations: `TestSections.Rectangle() with { Hc = 3000, Hb = 500, Zb = 100 }`.
- **Behavior-Driven Test Method Naming**:
  - Descriptive PascalCase names explaining expected behavior:
    - `TiesNormallyStopUnderTheBeam()`
    - `AZonedLayoutIsDenseThenSparseThenDense()`
    - `ZeroOrNegativeSpacingIsRejectedRatherThanLoopingForever()`
- **Assertions**:
  - Floating point comparison: `Assert.Equal(expectedDouble, actualDouble, Precision)`.
  - Single item validation: `var item = Assert.Single(collection);`.
  - Exception verification: `Assert.Throws<ArgumentOutOfRangeException>(() => ...);`.
  - Range validation: `Assert.InRange(value, min, max);`.
  - Theory / Data-driven tests: `[Theory]` + `[InlineData(...)]`.

---

## 5. Golden Reference Feature Architecture (`Column Rebar`)

The `HPRebar/HPRebar/Column Rebar/` folder serves as the strict architectural blueprint.

### 5.1 Mandatory Folder Layout
```
HPRebar/HPRebar/<Feature Name With Spaces>/
├── <FeatureName>Command.cs            ← ExternalCommand entry point
├── <FeatureName>Orchestrator.cs       ← TransactionGroup coordinator
├── <FeatureName>StackReader.cs        ← Revit element & geometry extraction
├── <FeatureName>SolidFaceReader.cs    ← Solid, face, and bounding box inspection
├── <FeatureName>StackValidator.cs     ← Geometric & topological validation rules
├── <FeatureName>CreationService.cs    ← Rebar batch coordinator
├── <FeatureName>RebarRunner.cs        ← UI runner adapter (implements runner interface)
├── <Entity>Creator.cs                 ← Specific rebar/shape creators
├── DetailViewCreator.cs               ← Elevation view creation
├── SectionViewCreator.cs              ← Cross-section view creation
├── DimensionCreator.cs                ← Dimension generation
├── <Entity>TableTagCreator.cs         ← Annotations / schedule tables
├── RevitUnits.cs                      ← Pure boundary mm <-> internal ft
├── LocalizationService.cs             ← Observable bilingual UI string service
├── ThemeSwitcher.cs                   ← Revit dark/light theme adapter
├── <Entity>SelectionFilter.cs         ← ISelectionFilter implementation
├── Models/                            ← Feature-specific Revit-aware data records
├── View/                              ← Window and view user controls
│   ├── <FeatureName>View.xaml
│   ├── <FeatureName>View.xaml.cs
│   ├── Controls/                      ← Custom canvases, painters, visual components
│   └── Tabs/                          ← Tab user controls (XAML + codebehind)
└── View Models/                       ← CommunityToolkit.Mvvm view models
    ├── <FeatureName>ViewModel.cs      ← Root window view model
    ├── <FeatureName>Session.cs        ← Working state / editing session
    └── Tabs/                          ← Tab view models
```

### 5.2 ExternalCommand Entry Point (`ColumnRebarCommand.cs`)
- Decorators: `[UsedImplicitly]`, `[Transaction(TransactionMode.Manual)]`.
- Inherits `Nice3point.Revit.Toolkit.External.ExternalCommand`.
- Lifecycle steps:
  1. Access `Application.ActiveUIDocument` and `Document`.
  2. Prompt user selection via `uiDocument.Selection.PickObjects(...)` using `ISelectionFilter`.
  3. Catch `Autodesk.Revit.Exceptions.OperationCanceledException` on user cancel (Escape).
  4. Validate selection with `<Feature>Validator.Validate(...)`. Check `validation.IsOk`. If failed, display error dialog and abort.
  5. Extract geometry with `<Feature>Reader.Read(...)` into domain stack.
  6. Pre-flight check via `<Feature>CreationService.CanCreate(...)` to ensure required rebar shapes and families exist before opening any dialog or transaction.
  7. Construct Orchestrator, Session, and ViewModel.
  8. Attach Revit main window handle:
     ```csharp
     var view = new ColumnRebarView(viewModel);
     new WindowInteropHelper(view).Owner = Application.MainWindowHandle;
     if (view.ShowDialog() != true) return;
     ```
  9. Display summary results using `RevitDialogs.Info(...)`.

### 5.3 Transaction Architecture & Warning Handling
- **Sole TransactionGroup Owner**: The Orchestrator class owns the `TransactionGroup`:
  ```csharp
  using var group = new TransactionGroup(_document, "Column Rebar");
  group.Start();
  try
  {
      // Sub-transactions:
      // 1. Create Views
      // 2. Create Dimensions
      // 3. Create Reinforcement
      // 4. Create Tables / Annotations
      group.Assimilate(); // Consolidates into single undo item
      return new OrchestratorResult { ... };
  }
  catch (Exception ex)
  {
      Log.Error(ex, "Failed; rolling back");
      group.RollBack();
      throw;
  }
  ```
- **Warning Preprocessor (`RebarFailureHandling.cs`)**:
  Each sub-transaction attaches an `IFailuresPreprocessor`:
  ```csharp
  var options = transaction.GetFailureHandlingOptions();
  options = options.SetFailuresPreprocessor(new SwallowWarnings());
  options = options.SetClearAfterRollback(true);
  transaction.SetFailureHandlingOptions(options);
  ```
  `SwallowWarnings` deletes warnings (severity == Warning) and writes them to Serilog. Genuine errors remain untouched, causing an exception that triggers `group.RollBack()`.

### 5.4 Revit Rebar Creation APIs & Multi-Version Gates
1. **Shape-Driven Rebar (Stirrups / Ties)**:
   - Method: `Rebar.CreateFromRebarShape(doc, shape, barType, host, origin, xVec, yVec)`.
   - Accessor:
     ```csharp
     var accessor = rebar.GetShapeDrivenAccessor();
     accessor.ScaleToBox(origin, width, height);
     accessor.SetLayoutAsNumberWithSpacing(count, RevitUnits.MmToFt(spacing), true, true, true);
     ```
2. **Free-Form Polyline Rebar (Main / Longitudinal Bars)**:
   - Points are simplified: segments `< 1.0 mm` (`MinimumSegmentMm = 1.0`) are culled to prevent Revit geometry rejection.
   - Points converted to lines: `Line.CreateBound(mapper.ToXyz(p1), mapper.ToXyz(p2))`.
   - Wrapped in `CurveLoop.Create(curves)`.
   - **Multi-Version Gate**:
     ```csharp
     #if REVIT2026_OR_GREATER
         var result = Rebar.CreateFreeForm(document, barType, host, loops, RebarStyle.Standard);
         if (result.Error != RebarFreeFormValidationResult.Success || result.Rebar is null)
             throw new InvalidOperationException($"Revit could not create bar: {result.Error}");
         var rebar = result.Rebar;
     #else
         var rebar = Rebar.CreateFreeForm(document, barType, host, loops, out var validation);
         if (validation != RebarFreeFormValidationResult.Success)
             throw new InvalidOperationException($"Revit could not create bar: {validation}");
     #endif
     ```
3. **Partition Assignment**:
   - `rebar.LookupParameter("Partition")?.Set(partitionName);`

### 5.5 Unit Conversion Boundary (`RevitUnits.cs`)
```csharp
internal static class RevitUnits
{
    public static double MmToFt(double mm) => UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters);
    public static double FtToMm(double ft) => UnitUtils.ConvertFromInternalUnits(ft, UnitTypeId.Millimeters);
    public static string Display(Document doc, double ft) =>
        UnitFormatUtils.Format(doc.GetUnits(), SpecTypeId.Length, ft, false);
}
```
*Rule*: Zero deprecated `DisplayUnitType`. Always use `UnitTypeId` and `SpecTypeId`.

### 5.6 MVVM & WPF UI Implementation
1. **MVVM Framework**:
   - `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`.
   - `[ObservableProperty]` generates property with INotifyPropertyChanged.
   - `[RelayCommand(CanExecute = nameof(CanRun))]` generates ICommand.
   - `[NotifyCanExecuteChangedFor(nameof(RunCommand))]` refreshes command states.
2. **Clean UI Decoupling**:
   - ViewModel communicates with Revit via a runner interface (`IColumnRebarRunner`) receiving domain specs and reporting progress via `IProgress<int>`.
   - ViewModel contains zero references to `Autodesk.Revit.DB.*`.
3. **Modal Window Lifecycle (`ColumnRebarView.xaml.cs`)**:
   - Strictly minimal code-behind:
     ```csharp
     public ColumnRebarView(ColumnRebarViewModel viewModel)
     {
         InitializeComponent();
         DataContext = viewModel;
         ThemeSwitcher.ApplyFromRevit(this);
         viewModel.CloseRequested += result =>
         {
             DialogResult = result;
             Close();
         };
     }
     ```
4. **Theming & DynamicResources**:
   - `Theme.xaml` in `HPRebar/HPRebar/Resources/Themes/` merges `ThemeDark.xaml` (or `ThemeLight.xaml`), `Typography.xaml`, `Spacing.xaml`, `Buttons.xaml`, `TextBoxes.xaml`, `Controls.xaml`.
   - In XAML:
     - `Background="{DynamicResource Brush.Background}"`
     - `Foreground="{DynamicResource Brush.Foreground.Primary}"`
     - `FontFamily="{DynamicResource Font.Family.Default}"`
     - `Margin="{DynamicResource Spacing.Large}"`
   - `ThemeSwitcher.ApplyFromRevit(FrameworkElement target)` swaps the color dictionary dynamically:
     ```csharp
     #if REVIT2024_OR_GREATER
         return Autodesk.Revit.UI.UIThemeManager.CurrentTheme == Autodesk.Revit.UI.UITheme.Dark;
     #else
         return true;
     #endif
     ```
5. **Dynamic Localization**:
   - `LocalizationService : ObservableObject` holds `[ObservableProperty] private UiStrings _strings`.
   - XAML binds via `{Binding Localization.Strings.<PropertyKey>}`.
   - Calling `LocalizationService.Toggle()` swaps the entire `UiStrings` record between English and Vietnamese, updating all UI text immediately.
6. **High-Performance Preview Canvas**:
   - Built as custom `FrameworkElement` (`ColumnElevationCanvas`, `ColumnSectionCanvas`).
   - Directly renders via `DrawingContext` in `OnRender(...)`.
   - Coalesces rapid UI updates using a 50ms `DispatcherTimer` debounce.

---

## 6. Application Ribbon Integration (`Application.cs`)

Located in `HPRebar/HPRebar/Application.cs`:
- Class: `public class Application : ExternalApplication` (from `Nice3point.Revit.Toolkit.External`).
- `OnStartup()`:
  - Calls `CreateLogger()`: sets up Serilog daily rolling file sink at `%LocalAppData%\HPRebar\logs\hprebar-.log`.
  - Calls `CreateRibbon()`:
    ```csharp
    var rebarPanel = Application.CreatePanel("Rebar", "HPRebar");

    rebarPanel.AddPushButton<ColumnRebarCommand>("Column Rebar")
        .SetImage("/HPRebar;component/Resources/Icons/RibbonIcon16.png")
        .SetLargeImage("/HPRebar;component/Resources/Icons/RibbonIcon32.png");
    ```
- Adding Beam Rebar follows the exact same panel registration:
  ```csharp
  rebarPanel.AddPushButton<BeamRebarCommand>("Beam Rebar")
      .SetImage("/HPRebar;component/Resources/Icons/RibbonIcon16.png")
      .SetLargeImage("/HPRebar;component/Resources/Icons/RibbonIcon32.png");
  ```

---

## 7. Direct Mapping Table for Beam Rebar (`R02_BeamsRebar`)

| Column Rebar Reference | Target Beam Rebar Component | Responsibility |
|---|---|---|
| `HPRebar.Core/ColumnRebar/Models/ColumnSection.cs` | `HPRebar.Core/BeamRebar/Models/BeamSpan.cs`, `BeamSupport.cs` | Pure geometric description (span length, width b, depth h, z-levels, supports) in mm. |
| `HPRebar.Core/ColumnRebar/Models/BarPolyline.cs` | `HPRebar.Core/BeamRebar/Models/BeamBarPolyline.cs` | Calculated bar polyline coordinates, diameters, hook angles, splice specs. |
| `HPRebar.Core/ColumnRebar/StirrupDistributionCalculator.cs` | `HPRebar.Core/BeamRebar/BeamStirrupDistributionCalculator.cs` | Uniform & 3-zone (gối-nhịp-gối: L/4, 2L/4, L/4) stirrup count and spacing calculation. |
| `HPRebar.Core/ColumnRebar/BarPolylineBuilder.cs` | `HPRebar.Core/BeamRebar/BeamMainBarCalculator.cs`, `BeamAdditionalBarCalculator.cs` | Continuous longitudinal bars, anchorage hooks (90°/180°), lap splices, top/bottom additional bars. |
| `HPRebar.Core.Tests/ColumnRebar/TestSections.cs` | `HPRebar.Core.Tests/BeamRebar/TestBeamSpans.cs` | Standard beam test fixtures (single-span, multi-span, cantilever, varying cross-sections). |
| `HPRebar/HPRebar/Column Rebar/ColumnRebarCommand.cs` | `HPRebar/HPRebar/Beam Rebar/BeamRebarCommand.cs` | ExternalCommand picking structural framing beams (`OST_StructuralFraming`). |
| `HPRebar/HPRebar/Column Rebar/ColumnRebarOrchestrator.cs` | `HPRebar/HPRebar/Beam Rebar/BeamRebarOrchestrator.cs` | Owner of `TransactionGroup("Beam Rebar")`, orchestrates views, rebar, dimensions. |
| `HPRebar/HPRebar/Column Rebar/ColumnStackReader.cs` | `HPRebar/HPRebar/Beam Rebar/BeamStackReader.cs`, `BeamSupportFinder.cs` | Reads collinear framing beams and column/girder supports; converts ft to mm. |
| `HPRebar/HPRebar/Column Rebar/ColumnStackValidator.cs` | `HPRebar/HPRebar/Beam Rebar/BeamStackValidator.cs` | Collinear check, continuous level check, valid solids check, rectangular profiles. |
| `HPRebar/HPRebar/Column Rebar/RebarCreationService.cs` | `HPRebar/HPRebar/Beam Rebar/BeamRebarCreationService.cs` | Coordinates batch creation of stirrups, main bars, additional bars, side bars. |
| `HPRebar/HPRebar/Column Rebar/StirrupCreator.cs` | `HPRebar/HPRebar/Beam Rebar/BeamStirrupCreator.cs` | Shape-driven stirrup creation (`CreateFromRebarShape`). |
| `HPRebar/HPRebar/Column Rebar/MainBarCreator.cs` | `HPRebar/HPRebar/Beam Rebar/BeamMainBarCreator.cs` | Free-form longitudinal bar creation (`CreateFreeForm` with multi-version R25/R26 gate). |
| `HPRebar/HPRebar/Column Rebar/DetailViewCreator.cs` | `HPRebar/HPRebar/Beam Rebar/BeamDetailViewCreator.cs`, `BeamSectionViewCreator.cs` | Elevation section and cross-sections along beam spans. |
| `HPRebar/HPRebar/Column Rebar/View Models/ColumnRebarViewModel.cs` | `HPRebar/HPRebar/Beam Rebar/View Models/BeamRebarViewModel.cs` | Tabbed CommunityToolkit.Mvvm ViewModel with `IBeamRebarRunner`. |
| `HPRebar/HPRebar/Column Rebar/View/ColumnRebarView.xaml` | `HPRebar/HPRebar/Beam Rebar/View/BeamRebarView.xaml` | Modal WPF Window styled with `DynamicResource` matching Revit Dark/Light themes. |

---

## 8. Critical Guardrails for Implementation

1. **Never import `Autodesk.Revit.*` into `HPRebar.Core`**. Keep `HPRebar.Core` strictly testable via `dotnet test HPRebar/HPRebar.Core.Tests`.
2. **Always tag multi-version conditional blocks** with `// Multi-version: <topic>`.
3. **Use file-scoped namespaces with explicit naming** in all new files (e.g. `namespace HPRebar.BeamRebar.Models;` - no spaces or auto-generated underscores).
4. **Never create `Commands/` or `Services/` subfolders** inside `HPRebar/HPRebar/Beam Rebar/`. Keep commands, services, orchestrators, and creators at the root of `Beam Rebar/`.
5. **Always use modern Revit Unit APIs** (`UnitTypeId`, `SpecTypeId`). Never use `DisplayUnitType`.
6. **Ensure single-undo atomicity**: Only `BeamRebarOrchestrator` creates and manages `TransactionGroup("Beam Rebar")`.
7. **Ensure non-modal warning resilience**: Apply `RebarFailureHandling.Apply(transaction)` to every child transaction to prevent silent UI hangs.
