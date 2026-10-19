# Changes - Worker M4 (HPRebar.McpBridge UI & Packaging)

## Overview
Worker M4 standardized `HPRebar.McpBridge` UI (`McpBridgeStatusView.xaml` and `Theme.xaml`) to the MaterialDesignInXamlToolkit v5.3.2 design system and verified ILRepack packaging isolation and live theme switching under Milestone M4.

## Exclusive Write Scope
- `HPRebar/HPRebar.McpBridge/View/McpBridgeStatusView.xaml`
- `HPRebar/HPRebar.McpBridge/Resources/Themes/Theme.xaml`

## Detailed File Modifications

### 1. `HPRebar/HPRebar.McpBridge/View/McpBridgeStatusView.xaml`
- **MaterialDesign Namespace**: Added `xmlns:md="http://materialdesigninxaml.net/winfx/xaml/themes"` to the root `<Window>`.
- **CheckBox Controls (`IsExecutionEnabled`, `AutoStartListener`)**:
  - Explicitly assigned `Style="{StaticResource MaterialDesignCheckBox}"` for standard toolkit appearance, ripple effect, and check geometry.
  - Added `Cursor="Hand"` for clear interactive feedback.
  - Aligned the explanatory caption text under `IsExecutionEnabled` with `Margin="28,4,0,0"` to match the checkbox label offset.
  - Adjusted top margin for `AutoStartListener` to `Margin="0,12,0,0"` for clean spacing and visual separation between safety settings.
- **TextBox Control (`LastScriptPreview`)**:
  - Verified `Style="{DynamicResource StandardTextBox}"` (rebased on `MaterialDesignOutlinedTextBox` with Segoe UI / Monospace font).
  - Added `VerticalContentAlignment="Top"` so script preview lines start cleanly from the top of the box.
- **Button Controls**:
  - `ToggleListener`: `Style="{DynamicResource PrimaryButton}"` (rebased on `MaterialDesignRaisedButton`).
  - `CopyLastScript`: `Style="{DynamicResource LinkButton}"` (rebased on `MaterialDesignFlatButton`).
  - `OpenAuditFolder`: `Style="{DynamicResource LinkButton}"` (rebased on `MaterialDesignFlatButton`).
  - `RestartBridge`: `Style="{DynamicResource SecondaryButton}"` (rebased on `MaterialDesignOutlinedButton`).
  - `Close`: `Style="{DynamicResource PrimaryButton}"` (rebased on `MaterialDesignRaisedButton`).
- **Cards & Borders**:
  - Container borders use `Style="{DynamicResource Card}"` with `{DynamicResource Brush.SurfaceElevated}`, `{DynamicResource Brush.Border}`, `BorderThickness="1"`, and `CornerRadius="6"`.
- **Status Dot**:
  - Preserved `StatusDot` style and triggers for bridge lifecycle:
    - Default / Stopped: `Brush.Foreground.Tertiary` (Gray)
    - Listening: `Brush.Info` (Blue)
    - Connected: `Brush.Success` (Green for active)
    - Busy: `Brush.Warning` (Orange/Yellow)
    - Error: `Brush.Danger` (Red for error/inactive)

### 2. `HPRebar/HPRebar.McpBridge/Resources/Themes/Theme.xaml`
- Merged toolkit defaults and color dictionaries via pack URIs into `HPRebar.McpBridge`:
  - `pack://application:,,,/HPRebar.McpBridge;component/Resources/Themes/MaterialBridge.xaml`
  - `pack://application:,,,/HPRebar.McpBridge;component/Resources/Themes/ThemeDark.xaml`
  - `Typography.xaml`, `Spacing.xaml`, `Controls.xaml`, `ThemeIcons.xaml`
- Defined implicit styles for standard controls:
  - `TextBox` -> `StandardTextBox`
  - `ComboBox` -> `StandardComboBox`
  - `CheckBox` -> `MaterialDesignCheckBox`
  - `RadioButton` -> `MaterialDesignRadioButton`
  - `Button` -> `SecondaryButton`
  - `DataGrid` -> `MaterialDesignDataGrid`
  - `TabControl` -> `MaterialDesignTabControl`
  - `Expander` -> `MaterialDesignExpander`

### 3. Packaging & ILRepack Isolation Verification (`HPRebar.McpBridge.csproj`)
- Verified that target `RepackMaterialDesign` merges `MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, and `Microsoft.Xaml.Behaviors.dll` into `HPRebar.McpBridge.dll`.
- Confirmed post-build cleanup deletes all loose toolkit DLLs (`MaterialDesignThemes.Wpf.dll`, `MaterialDesignColors.dll`, `Microsoft.Xaml.Behaviors.dll`) from `bin/Debug.R26` and `bin/Debug.R25`.
- Verified Roslyn assemblies (`Microsoft.CodeAnalysis.*`) remain unmerged as intended for load-context isolation.

### 4. Theme Switching Verification (`MaterialThemeBridge.cs`)
- Verified `MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, dark => new RibbonIcons(dark).McpBridge)` in `McpBridgeStatusView.xaml.cs`.
- Dynamically swaps top-level overlay dictionary in `window.Resources.MergedDictionaries` on `RevitHostTheme.Instance.Changed`.
