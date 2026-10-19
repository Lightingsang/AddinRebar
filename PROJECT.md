# Project: HPKata MahApps.Metro Migration & Revit Element Geometry Upgrade

## Architecture
- **Ecosystem**: `HPKata/HPKata.slnx` (.NET 8.0 SDK 10.0.300, `net8.0-windows` for UI and host add-ins).
- **HPKata.UI**: Shared WPF UI library used by both Revit 2026 (`HPKata.Revit`) and AutoCAD 2026 (`HPKata.AutoCad`). Migrating from `MaterialDesignThemes` to `MahApps.Metro` `2.4.11` using `MetroWindow` chrome, semantic tokens (`ThemeDark.xaml` / `ThemeLight.xaml`), and preloading via `ThemeManager.EnsureAssembliesLoaded()`.
- **HPKata.Revit**: Revit 2026 add-in (.NET 8.0). Bridges Revit structural elements (`OST_StructuralFraming`, `OST_StructuralColumns`, `OST_Walls`) to HPKata detailing dialogs, with automatic geometry extraction ($b \times h \times L/H$ in mm) via `RevitElementGeometryReader` and safe fallback to manual input on ESC (`OperationCanceledException`).
- **HPKata.AutoCad**: AutoCAD 2026 add-in (.NET 8.0). References `HPKata.UI` and `MahApps.Metro` 2.4.11.
- **HPKata.UI.Tests**: Test suite in `HPKata.UI.Tests` providing STA thread instantiation verification for all 13 `MetroWindow` dialogs under Dark and Light themes, plus 100% `{StaticResource}` resolution parity scanning.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Package Migration to MahApps.Metro 2.4.11 | Replace `MaterialDesignThemes` & `MaterialDesignColors` with `MahApps.Metro` 2.4.11 in `HPKata.UI.csproj`, `HPKata.Revit.csproj`, `HPKata.AutoCad.csproj` | M1 | Survey 1, R1 |
| 2 | Theme Bootstrap Migration | Migrate `MaterialBridge.xaml` to merge `MahApps.Metro` standard resource dictionaries (`Controls.xaml`, `Fonts.xaml`, `Dark.Blue.xaml`) | M1 | Survey 1, R2 |
| 3 | Style Keys in Theme.xaml | Define all required style keys in `Theme.xaml`: `StandardDataGrid`, `StandardTextBox`, `StandardComboBox`, `PrimaryButton`, `Button.Primary`, `SecondaryButton`, `Button.Secondary`, `Card.Panel`, `GroupBox.Header`, `Text.Title`, `Text.Section`, `Text.Muted`, `Text.Bold`, `Text.Label`, and implicit styles; define `Brush.Card` | M1 | Survey 1, 2, R2 |
| 4 | ThemeManager Assembly Preloading & Switching | Add `ThemeManager.EnsureAssembliesLoaded()` preloading `MetroWindow` & `ControlzEx` assemblies; update `Apply` and `ApplyTheme` for dynamic dark/light switching | M1 | Survey 1, R2 |
| 5 | Zero MaterialDesign Residue | Eliminate all remaining references to `MaterialDesignThemes` and `http://materialdesigninxaml.net/winfx/xaml/themes` in `HPKata/` | M1 | Survey 1, R1 |
| 6 | 13 MetroWindow XAML Migration | Convert all 13 dialogs in `HPKata/HPKata.UI/Views/` to inherit from `mah:MetroWindow` with `xmlns:mah="http://metro.mahapps.com/winfx/xaml/controls"` | M2 | Survey 2, R2 |
| 7 | Metro Chrome Attributes | Configure Metro chrome on all 13 views (`GlowBrush="{DynamicResource Brush.Accent}"`, `BorderThickness="1"`, `TitleCharacterCasing="Normal"`, Segoe UI font) | M2 | Survey 2, R2 |
| 8 | Parameterless Constructor Support | Add parameterless constructors to `FoundationDetailingWindow`, `SlabDetailingWindow`, `StairLintelDetailingWindow` chaining to default ViewModels | M2 | Survey 2, R2 |
| 9 | Window Constructor Lifecycle Standardization | Standardize constructors across all 13 views: call `ThemeManager.EnsureAssembliesLoaded()` before `InitializeComponent()`, `ThemeManager.Apply(this, dark: true)` after | M2 | Survey 2, R2 |
| 10 | Safe Modal DialogResult Handling | Implement safe `DialogResult = true` handling in `OnRequestClose()` using `ComponentDispatcher.IsThreadModal` or try/catch before `Close()` | M2 | Survey 2, R2 |
| 11 | Revit Element Geometry Reader | Create `HPKata/HPKata.Revit/Services/RevitElementGeometryReader.cs` to read $b \times h \times L/H$ in mm from Framing, Column, and Wall elements using pure testable methods and `RevitUnitConverter` (Rule R5) | M3 | Survey 3, R3 |
| 12 | Pre-Selection Element Detection | Update `FramingRebarCommands.cs`, `ColumnRebarCommands.cs`, `WallRebarCommands.cs` to detect pre-selected elements via `uidoc.Selection.GetElementIds()` | M3 | Survey 3, R3 |
| 13 | Interactive Pick with ESC Fallback | Wrap interactive picking with try/catch catching `Autodesk.Revit.Exceptions.OperationCanceledException` so ESC falls back to manual mode without aborting | M3 | Survey 3, R3 |
| 14 | Modal OK Transaction Execution | Execute Revit rebar transaction on `ShowDialog() == true` when target element is present, returning `Result.Succeeded` | M3 | Survey 3, R3 |
| 15 | STA MetroWindow Instantiation Unit Tests | Implement `HPKata.UI.Tests/Views/AllWindowsXamlInstantiationTests.cs` testing STA instantiation of all 13 `MetroWindow`s under both Dark and Light themes | M4 | Survey 3, R4 |
| 16 | Automated StaticResource Parity Scanner | Implement automated scanner in `AllWindowsXamlInstantiationTests.cs` verifying 100% of `{StaticResource}` keys in views resolve against `Theme.xaml` | M4 | Survey 2, 3, R4 |
| 17 | Solution Compilation (0 warnings, 0 errors) | `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` compiles cleanly with zero warnings and zero errors | M5 | AC1 |
| 18 | 100% Test Pass Rate | `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` passes 100% of tests across all test projects | M5 | AC6 |
| 19 | Clean Code Governance Compliance | Every C# file $\le 300$ lines (Rule C2), methods $\le 50$ lines (Rule M3), 0 dead code, 0 unannotated catch blocks, Rule R5 (`304.8` only in `RevitUnitConverter.cs`) | M5 | Governance |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Package Migration & Theme Infrastructure | Features 1, 2, 3, 4, 5 (Csproj, MaterialBridge.xaml, Theme.xaml, ThemeManager.cs) | Survey done | DONE (947/947 tests pass, 0 warnings) |
| M2 | 13 MetroWindow Views Migration & DialogResult Safety | Features 6, 7, 8, 9, 10 (All 13 Views in HPKata.UI/Views/) | M1 | DONE (982/982 tests pass, 0 warnings) |
| M3 | Revit Element Geometry Reader & Safe Picker Fallback | Features 11, 12, 13, 14 (RevitElementGeometryReader, Framing/Column/Wall commands) | M1 | DONE (994/994 tests pass, 0 warnings) |
| M4 | Comprehensive STA Unit Tests & StaticResource Parity | Features 15, 16 (AllWindowsXamlInstantiationTests in HPKata.UI.Tests) | M1, M2 | DONE (1,054/1,054 tests pass, 0 warnings) |
| M5 | Final Verification, Solution Build, 100% Test Pass & Audit | Features 17, 18, 19 (Solution build, full test run, forensic audit) | M1, M2, M3, M4 | DONE (1,054/1,054 tests pass, 0 warnings, CLEAN audit) |

## Interface Contracts

### Theme Subsystem ↔ WPF Views (`HPKata.UI.Themes` ↔ `HPKata.UI.Views`)
- **Assembly Preloading Contract**:
  ```csharp
  namespace HPKata.UI.Common;
  public static class ThemeManager
  {
      public static void EnsureAssembliesLoaded();
      public static void Apply(Window window, bool dark, Func<bool, ImageSource>? icon = null);
      public static void ApplyTheme(Window window, bool dark = true);
  }
  ```
- **XAML Style Keys Contract** in `Theme.xaml`:
  - DataGrid: `StandardDataGrid` (TargetType="DataGrid", BasedOn="{StaticResource MahApps.Styles.DataGrid}")
  - TextBox: `StandardTextBox` (TargetType="TextBox", BasedOn="{StaticResource MahApps.Styles.TextBox}")
  - ComboBox: `StandardComboBox` (TargetType="ComboBox", BasedOn="{StaticResource MahApps.Styles.ComboBox}")
  - Buttons: `PrimaryButton` and `Button.Primary` (TargetType="Button", BasedOn="{StaticResource MahApps.Styles.Button}")
  - Buttons: `SecondaryButton` and `Button.Secondary` (TargetType="Button", BasedOn="{StaticResource MahApps.Styles.Button}")
  - Borders: `Card.Panel` (TargetType="Border")
  - GroupBox: `GroupBox.Header` (TargetType="GroupBox", BasedOn="{StaticResource MahApps.Styles.GroupBox}")
  - TextBlocks: `Text.Title`, `Text.Section`, `Text.Muted`, `Text.Bold`, `Text.Label` (TargetType="TextBlock")
  - Brushes: `Brush.Card` (aliased to `Brush.Surface` / `Color.SurfaceElevated`)

### Geometry Reader ↔ Revit Commands (`HPKata.Revit.Services` ↔ `HPKata.Revit.Commands`)
- **RevitElementGeometryReader Contract**:
  ```csharp
  namespace HPKata.Revit.Services;
  public static class RevitElementGeometryReader
  {
      public static FramingDimensions ReadFramingDimensions(FamilyInstance beam);
      public static ColumnDimensions ReadColumnDimensions(FamilyInstance column);
      public static WallDimensions ReadWallDimensions(Wall wall);
      public static ElementDimensionData ResolveDimensions(
          double? primaryWidthFt, double? primaryHeightFt, double? lengthFt,
          double? bboxWidthFt, double? bboxHeightFt, double? bboxLengthFt,
          double defaultWidthMm, double defaultHeightMm, double defaultLengthMm);
  }
  public readonly record struct FramingDimensions(string Mark, double WidthMm, double HeightMm, double LengthMm);
  public readonly record struct ColumnDimensions(string Mark, double WidthMm, double DepthMm, double HeightMm);
  public readonly record struct WallDimensions(string Mark, double ThicknessMm, double LengthMm, double HeightMm);
  public readonly record struct ElementDimensionData(double WidthMm, double HeightMm, double LengthMm);
  ```
- **All unit conversions**: MUST route through `HPKata.Revit.Units.RevitUnitConverter.FeetToMm(...)` (Rule R5).

### Command Modal & Fallback Contract (`HPKata.Revit.Commands`)
- **Interactive Pick & Pre-Selection Pattern**:
  ```csharp
  Element? targetElement = null;
  // 1. Try pre-selection
  var preSelected = uidoc.Selection.GetElementIds()
      .Select(id => doc.GetElement(id))
      .FirstOrDefault(e => e is FamilyInstance fi && e.Category?.Id.Value == (long)BuiltInCategory.OST_StructuralFraming);
  if (preSelected != null) { targetElement = preSelected; }
  else {
      try {
          var pickedRef = uidoc.Selection.PickObject(ObjectType.Element, new FramingSelectionFilter(), "Select beam (ESC for manual):");
          if (pickedRef != null) targetElement = doc.GetElement(pickedRef);
      } catch (Autodesk.Revit.Exceptions.OperationCanceledException) {
          // ESC pressed: enter manual input mode
          targetElement = null;
      }
  }
  // 2. Open dialog modally
  bool? dialogResult = RevitWindowHelper.ShowModal(view, uiapp);
  if (dialogResult == true) {
      if (targetElement != null) {
          // Execute Revit transaction
          using var tr = new Transaction(doc, "Generate Rebar");
          tr.Start();
          FramingRebarGenerator.GenerateFramingRebar(doc, (FamilyInstance)targetElement, vm.ResultLayout);
          tr.Commit();
      }
      return Result.Succeeded;
  }
  return Result.Cancelled;
  ```

## Code Layout
- `HPKata/HPKata.UI/HPKata.UI.csproj`: MahApps.Metro 2.4.11 reference.
- `HPKata/HPKata.Revit/HPKata.Revit.csproj`: MahApps.Metro 2.4.11 reference.
- `HPKata/HPKata.AutoCad/HPKata.AutoCad.csproj`: MahApps.Metro 2.4.11 reference.
- `HPKata/HPKata.UI/Themes/MaterialBridge.xaml`: MahApps resource dictionaries bootstrap.
- `HPKata/HPKata.UI/Themes/Theme.xaml`: Required style keys and MahApps base styles.
- `HPKata/HPKata.UI/Common/ThemeManager.cs`: `EnsureAssembliesLoaded()`, `ApplyTheme()`, `Apply()`.
- `HPKata/HPKata.UI/Views/*.xaml` and `*.xaml.cs`: All 13 MetroWindow views.
- `HPKata/HPKata.Revit/Services/RevitElementGeometryReader.cs`: Dimension extraction and unit conversion.
- `HPKata/HPKata.Revit/Commands/FramingRebarCommands.cs`: Pre-selection, ESC fallback, modal transaction.
- `HPKata/HPKata.Revit/Commands/ColumnRebarCommands.cs`: Pre-selection, ESC fallback, modal transaction.
- `HPKata/HPKata.Revit/Commands/WallRebarCommands.cs`: Pre-selection, ESC fallback, modal transaction.
- `HPKata/HPKata.UI.Tests/Views/AllWindowsXamlInstantiationTests.cs`: STA tests and static resource scanner.
- `HPKata/HPKata.Revit.Tests/Services/RevitElementGeometryReaderTests.cs`: Pure unit tests for dimension calculation.

## Clean Code Governance
- **Rule C2**: Max 300 lines per `.cs` file.
- **Rule M3**: Max 50 lines per method.
- **Rule Q-B1**: 0 dead or commented-out code.
- **Rule Q-B2**: 0 empty catch blocks without explanatory comments.
- **Rule R5**: In `HPKata.Revit`, `304.8` must appear only inside `Units/RevitUnitConverter.cs`.
- **TreatWarningsAsErrors**: Build must succeed with 0 warnings and 0 errors.
