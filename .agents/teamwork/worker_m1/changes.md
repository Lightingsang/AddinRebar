# Changes Summary — Milestone M1: Theme Core Worker

**Worker:** Worker M1 (Theme Core Worker)  
**Date:** 2026-10-10  
**Target Solution:** `HPRebar` & `HPRebar.McpBridge`  

---

## 1. Files Modified

| File Path | Nature of Change | Summary |
|---|---|---|
| `HPRebar/HPRebar/Resources/Themes/MaterialBridge.xaml` | Modified | Added `StandardComboBox`, `StandardCheckBox`, `StandardRadioButton`, and `Card.Panel`. Preserved all 8 legacy keys, font override Segoe UI, and primary/secondary colors. |
| `HPRebar/HPRebar/Resources/Themes/Theme.xaml` | Modified | Added implicit styles for standard WPF controls (`TextBox`, `ComboBox`, `CheckBox`, `RadioButton`, `Button`, `DataGrid`, `TabControl`, `Expander`). |
| `HPRebar/HPRebar.McpBridge/Resources/Themes/Theme.xaml` | Modified | Synchronized with implicit styles referencing local component pack URIs. |
| `HPRebar/HPRebar/Resources/Themes/MaterialThemeBridge.cs` | Modified | Removed hardcoded surface/card/popup colors, allowing toolkit's `Theme.Create(dark ? BaseTheme.Dark : BaseTheme.Light, ...)` to naturally manage surface palettes across Dark/Light transitions. Preserved validation danger color, NO PaletteHelper, NO Application.Current. |
| `HPRebar/HPRebar/Resources/Themes/Controls.xaml` | Inspected & Verified | Confirmed compatibility of existing `Card`, `Separator`, `Badge`, `BadgeText`, `Tag` styles. |

---

## 2. Detailed Changes & Rationale

### 2.1. `HPRebar/HPRebar/Resources/Themes/MaterialBridge.xaml`
- **Rationale**:
  - `StandardComboBox`: 35+ ComboBoxes across the add-in lacked styling. Adding `StandardComboBox` based on `MaterialDesignOutlinedComboBox` with 4px corner radius standardizes all dropdowns to modern outlined style.
  - `StandardCheckBox` & `StandardRadioButton`: Rebased keys provide standardized typography, vertical alignment, and hand cursor.
  - `Card.Panel`: Provides standardized bordered section container with subtle DropShadowEffect per `revit-xaml-styles/SKILL.md`.
  - Invariants preserved:
    - Font override `<FontFamily x:Key="MaterialDesignFont">Segoe UI</FontFamily>` preserved (strictly 0 references to Roboto).
    - Brand Primary (`#0696D7`) and Secondary (`#E0641E`) in `CustomColorTheme` preserved.
    - 8 legacy keys required by `ThemeTokenCoverageTests` preserved: `PrimaryButton`, `SecondaryButton`, `DangerButton`, `IconButton`, `LinkButton`, `StandardTextBox`, `NumberTextBox`, `SearchTextBox`.

```xml
    <!-- ===== Combo boxes ===== -->
    <Style x:Key="StandardComboBox" TargetType="ComboBox" BasedOn="{StaticResource MaterialDesignOutlinedComboBox}">
        <Setter Property="FontFamily" Value="{DynamicResource Font.Family.Default}"/>
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Body}"/>
        <Setter Property="Padding" Value="8,6"/>
        <Setter Property="MinHeight" Value="32"/>
        <Setter Property="VerticalContentAlignment" Value="Center"/>
        <Setter Property="md:TextFieldAssist.TextFieldCornerRadius" Value="4"/>
    </Style>

    <!-- ===== Check boxes &amp; Radio buttons ===== -->
    <Style x:Key="StandardCheckBox" TargetType="CheckBox" BasedOn="{StaticResource MaterialDesignCheckBox}">
        <Setter Property="FontFamily" Value="{DynamicResource Font.Family.Default}"/>
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Body}"/>
        <Setter Property="VerticalContentAlignment" Value="Center"/>
        <Setter Property="Cursor" Value="Hand"/>
    </Style>

    <Style x:Key="StandardRadioButton" TargetType="RadioButton" BasedOn="{StaticResource MaterialDesignRadioButton}">
        <Setter Property="FontFamily" Value="{DynamicResource Font.Family.Default}"/>
        <Setter Property="FontSize" Value="{DynamicResource Font.Size.Body}"/>
        <Setter Property="VerticalContentAlignment" Value="Center"/>
        <Setter Property="Cursor" Value="Hand"/>
    </Style>

    <!-- ===== Card Panel (Content grouping) ===== -->
    <Style x:Key="Card.Panel" TargetType="Border">
        <Setter Property="Background" Value="{DynamicResource Brush.Surface}"/>
        <Setter Property="BorderBrush" Value="{DynamicResource Brush.Border}"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="CornerRadius" Value="4"/>
        <Setter Property="Padding" Value="12"/>
        <Setter Property="Effect">
            <Setter.Value>
                <DropShadowEffect BlurRadius="4" ShadowDepth="1" Opacity="0.15" Color="#000000"/>
            </Setter.Value>
        </Setter>
    </Style>
```

### 2.2. `HPRebar/HPRebar/Resources/Themes/Theme.xaml`
- **Rationale**:
  - Raw WPF controls in existing views (which previously lacked explicit style declarations) will automatically inherit MaterialDesign2 appearance without needing explicit key bindings across dozens of XAML files.
  - Controls mapped:
    - `TargetType="TextBox"` -> `BasedOn="{StaticResource StandardTextBox}"`
    - `TargetType="ComboBox"` -> `BasedOn="{StaticResource StandardComboBox}"`
    - `TargetType="CheckBox"` -> `BasedOn="{StaticResource MaterialDesignCheckBox}"`
    - `TargetType="RadioButton"` -> `BasedOn="{StaticResource MaterialDesignRadioButton}"`
    - `TargetType="Button"` -> `BasedOn="{StaticResource SecondaryButton}"`
    - `TargetType="DataGrid"` -> `BasedOn="{StaticResource MaterialDesignDataGrid}"`
    - `TargetType="TabControl"` -> `BasedOn="{StaticResource MaterialDesignTabControl}"`
    - `TargetType="Expander"` -> `BasedOn="{StaticResource MaterialDesignExpander}"`

### 2.3. `HPRebar/HPRebar.McpBridge/Resources/Themes/Theme.xaml`
- **Rationale**:
  - Synchronized `HPRebar.McpBridge`'s `Theme.xaml` with the same implicit styles, resolving resources through `pack://application:,,,/HPRebar.McpBridge;component/Resources/Themes/...`.
  - Ensures seamless theme parity between the main add-in and the MCP Bridge status window.

### 2.4. `HPRebar/HPRebar/Resources/Themes/MaterialThemeBridge.cs`
- **Rationale**:
  - Previously, lines 63–69 manually overwrote `theme.Background`, `theme.Cards.Background`, `theme.Cards.Border`, and `theme.ToolTips.Background` with custom HP palette tokens.
  - Per Requirement R3 ("Không dùng palette HP để ghi đè toàn bộ màu bề mặt của toolkit"), these hardcoded surface overrides were removed.
  - `Theme.Create(dark ? BaseTheme.Dark : BaseTheme.Light, seedTheme.PrimaryMid.Color, seedTheme.SecondaryMid.Color)` now naturally dictates the clean MaterialDesign surface, card, border, and popup palette.
  - Maintained `theme.ValidationError = Token(palette, "Color.Danger", theme.ValidationError)` to sync validation alerts with brand danger `#E5484D`.
  - Strictly adheres to NO `PaletteHelper` and NO `Application.Current`.
