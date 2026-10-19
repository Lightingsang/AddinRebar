# DISPATCH — Worker M1 (Package Migration & Theme Infrastructure)

## Mission
Execute Milestone M1: Replace MaterialDesignThemes with MahApps.Metro 2.4.11 across HPKata projects, rebuild the theme subsystem (`MaterialBridge.xaml`, `Theme.xaml`, `ThemeManager.cs`), ensure zero MaterialDesign references remain in the theme subsystem, and verify that all existing tests compile and pass cleanly.

## Key Inputs
- Path to authoritative user request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (read entry dated `2026-10-17T14:27:21Z`).
- Path to project blueprint: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
- Explorer survey findings: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_1\handoff.md`.
- Working directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_1\`

## Exclusive Write Ownership
You own and may modify ONLY these files:
- `HPKata/HPKata.UI/HPKata.UI.csproj`
- `HPKata/HPKata.Revit/HPKata.Revit.csproj`
- `HPKata/HPKata.AutoCad/HPKata.AutoCad.csproj`
- `HPKata/HPKata.UI/Themes/MaterialBridge.xaml`
- `HPKata/HPKata.UI/Themes/Theme.xaml`
- `HPKata/HPKata.UI/Common/ThemeManager.cs`

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Step-by-Step Instructions
1. **Update Project References**:
   - In `HPKata/HPKata.UI/HPKata.UI.csproj`:
     Remove `<PackageReference Include="MaterialDesignThemes" ... />` and `<PackageReference Include="MaterialDesignColors" ... />`.
     Add `<PackageReference Include="MahApps.Metro" Version="2.4.11" />`.
   - In `HPKata/HPKata.Revit/HPKata.Revit.csproj`:
     Remove `<PackageReference Include="MaterialDesignThemes" ... />`.
     Add `<PackageReference Include="MahApps.Metro" Version="2.4.11" />`.
   - In `HPKata/HPKata.AutoCad/HPKata.AutoCad.csproj`:
     Remove `<PackageReference Include="MaterialDesignThemes" ... />`.
     Add `<PackageReference Include="MahApps.Metro" Version="2.4.11" />`.

2. **Update `MaterialBridge.xaml`**:
   - Remove MaterialDesign namespace and custom color theme.
   - Merge standard MahApps dictionaries:
     - `pack://application:,,,/MahApps.Metro;component/Styles/Controls.xaml`
     - `pack://application:,,,/MahApps.Metro;component/Styles/Fonts.xaml`
     - `pack://application:,,,/MahApps.Metro;component/Styles/Themes/Dark.Blue.xaml`
   - Define `<FontFamily x:Key="StandardFont">Segoe UI</FontFamily>`.

3. **Update `Theme.xaml`**:
   - Merge `MaterialBridge.xaml` (MahApps bootstrap) and base dictionaries.
   - Define ALL required style keys:
     - `StandardDataGrid` (`TargetType="DataGrid"`, `BasedOn="{StaticResource MahApps.Styles.DataGrid}"`) and implicit `<Style TargetType="DataGrid" BasedOn="{StaticResource StandardDataGrid}"/>`.
     - `StandardTextBox` (`TargetType="TextBox"`, `BasedOn="{StaticResource MahApps.Styles.TextBox}"`) and implicit `<Style TargetType="TextBox" BasedOn="{StaticResource StandardTextBox}"/>`.
     - `StandardComboBox` (`TargetType="ComboBox"`, `BasedOn="{StaticResource MahApps.Styles.ComboBox}"`) and implicit `<Style TargetType="ComboBox" BasedOn="{StaticResource StandardComboBox}"/>`.
     - `PrimaryButton` AND `Button.Primary` (`TargetType="Button"`, `BasedOn="{StaticResource MahApps.Styles.Button}"`).
     - `SecondaryButton` AND `Button.Secondary` (`TargetType="Button"`, `BasedOn="{StaticResource MahApps.Styles.Button}"`).
     - `Card.Panel` (`TargetType="Border"`).
     - `GroupBox.Header` (`TargetType="GroupBox"`, `BasedOn="{StaticResource MahApps.Styles.GroupBox}"`).
     - `Text.Title`, `Text.Section`, `Text.Muted`, `Text.Bold`, `Text.Label` (`TargetType="TextBlock"`), and implicit `TextBlock` style.
     - Define `Brush.Card` (alias to `{DynamicResource Brush.Surface}`).
   - Ensure ZERO references to `MaterialDesignThemes` or `http://materialdesigninxaml.net/winfx/xaml/themes` remain in `Theme.xaml`.

4. **Update `ThemeManager.cs`**:
   - Remove `using MaterialDesignThemes.Wpf;`.
   - Implement `EnsureAssembliesLoaded()`:
     ```csharp
     public static void EnsureAssembliesLoaded()
     {
         _ = typeof(MahApps.Metro.Controls.MetroWindow).Assembly;
         _ = typeof(ControlzEx.Theming.ThemeManager).Assembly;
     }
     ```
   - Implement `ApplyTheme(Window window, bool dark = true)`:
     Use `ControlzEx.Theming.ThemeManager.Current.ChangeTheme(window, dark ? "Dark.Blue" : "Light.Blue")` and merge `ThemeDark.xaml` / `ThemeLight.xaml` into `window.Resources.MergedDictionaries`.
   - Update `Apply(Window window, bool dark, Func<bool, ImageSource>? icon = null)` to delegate cleanly.
   - Ensure file is $\le 300$ lines, methods $\le 50$ lines, 0 warnings, 0 dead code.

5. **Build and Test Verification**:
   - Run: `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
   - Run: `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
   - Confirm: 0 warnings, 0 errors, 100% tests pass.

6. **Handoff**:
   - Write comprehensive report to `handoff.md` in your working directory.
   - Send completion message to `orchestrator_19`.


## 2026-10-17T14:42:20Z
You are worker_m1_1, the implementation worker for Milestone M1 (Package Migration & Theme Infrastructure).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_1\
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_1\DISPATCH.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-17T14:27:21Z).
Read the project plan at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Read Explorer 1's survey at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_1\handoff.md.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Your exclusive write ownership:
- HPKata/HPKata.UI/HPKata.UI.csproj
- HPKata/HPKata.Revit/HPKata.Revit.csproj
- HPKata/HPKata.AutoCad/HPKata.AutoCad.csproj
- HPKata/HPKata.UI/Themes/MaterialBridge.xaml
- HPKata/HPKata.UI/Themes/Theme.xaml
- HPKata/HPKata.UI/Common/ThemeManager.cs

Instructions:
1. Replace MaterialDesign packages with MahApps.Metro 2.4.11 in the 3 .csproj files.
2. Update MaterialBridge.xaml to bootstrap MahApps dictionaries (Controls.xaml, Fonts.xaml, Dark.Blue.xaml).
3. Update Theme.xaml defining all required styles (StandardDataGrid, StandardTextBox, StandardComboBox, PrimaryButton, Button.Primary, SecondaryButton, Button.Secondary, Card.Panel, GroupBox.Header, Text.Title, Text.Section, Text.Muted, Text.Bold, Text.Label, implicit styles, Brush.Card) with zero MaterialDesign residue.
4. Update ThemeManager.cs: add EnsureAssembliesLoaded(), implement ApplyTheme/Apply using ControlzEx/MahApps, remove MaterialDesign references. Keep class <= 300 lines, methods <= 50 lines.
5. Run build and test:
   dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
   dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
6. Write handoff.md in your working directory and notify orchestrator_19 when complete.
