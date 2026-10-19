# HPRebar MaterialDesign Migration — Module Views Survey & Analysis Report

**Date**: 2026-10-10  
**Investigator**: Explorer 2 (Module Views Explorer)  
**Scope**: All UI Views and UserControls across modules (`ColumnRebar`, `BeamRebar`, `FoundationRebar`, `KataExport`, `KataRebar`, `RebarPartition`, `McpBridge`) in `HPRebar/HPRebar/` and `HPRebar/HPRebar.McpBridge/`.  
**Baseline Verification**: `dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` PASS (0 errors, 32 ILRepack warnings); `dotnet test HPRebar.Core.Tests` PASS (1,682/1,682 green tests).

---

## 1. Executive Summary

This investigation surveys all XAML views, UserControls, and custom graphics canvases in the HPRebar add-in ecosystem. The goal is to prepare for the migration of all UI components to `MaterialDesignInXamlToolkit` (v5.3.2) while ensuring zero regressions in business logic, technical graphics, layout stability, or localization.

### Key Findings
1. **Total Views Identified**: 24 distinct `.xaml` view files across 7 modules (plus 8 theme dictionary files).
2. **Current Styling Architecture**:
   - `Resources/Themes/Theme.xaml` already merges `MaterialBridge.xaml` first, defining re-based legacy button styles (`PrimaryButton`, `SecondaryButton`, `DangerButton`, `IconButton`, `LinkButton`) and text box styles (`StandardTextBox`, `NumberTextBox`).
   - Dynamic dark/light runtime synchronization is handled via `MaterialThemeBridge.cs`, which creates a top-level overlay dictionary modifying MaterialDesign palette tokens (`theme.Background`, `theme.Foreground`, `theme.ValidationError`, `theme.Cards.Background`, etc.) based on `ThemeDark.xaml` and `ThemeLight.xaml`.
3. **Primary Inconsistencies & Legacy Redundancies**:
   - Several newer views (`KataRebarView`, `RebarPartitionView`, `KataExportView`) defined redundant local styles (such as `CardBorderStyle`, `DataGridHeaderStyle`, `DataGridRowStyle`, `PropertyCellBorder`, `ParamPickerComboBoxStyle`) rather than consuming toolkit standard styles or `Theme.xaml` tokens.
   - Some controls (e.g. Buttons in `KataRebarView`, input TextBoxes in `MainBarsTabView`, `AdditionalBarsTabView`, `StirrupsTabView`) omit explicit style keys, falling back to default WPF or implicit styles.
   - `ColumnRebarView` tab icons rely on vector `ControlTemplate`s via `ResourceKeyToControlTemplateConverter`, which accesses `Application.Current?.TryFindResource(key)` (problematic in in-process Revit add-in environments where `Application.Current` can be null or disjoint).
4. **Non-Negotiable Preservations**:
   - **Numeric Right-Alignment**: Every numerical field (lengths, spacings, diameters, ratios, counts, covers, angles) must retain right-alignment and monospace font (`Cascadia Mono` / `Consolas`).
   - **Technical Colors**: 2D canvas and 3D Helix rebar rendering colors (`Beam3DMaterials.cs`, `CanvasPalette.cs`, `KataCanvasPalette.cs`) represent structural engineering conventions (e.g. red for top rebar, blue for bottom rebar, green for stirrups, amber for ties) and must never be altered or overridden by theme colors.
   - **Data Validation & Error Banners**: Explicit validation error banners, `md:ValidationAssist` integration, and ViewModel validation status messaging must remain fully functional.
   - **Localization**: 100% of Vietnamese text strings and MVVM bindings must remain identical.

---

## 2. Comprehensive Inventory of Views and UI Controls

### 2.1 View Inventory Matrix

| Module | View File (`.xaml`) | Code-Behind (`.cs`) | Window / Control Type | Dimensions / Layout | Primary Controls Present | Current Styling Mechanism | Proposed MaterialDesign Target |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **BeamRebar** | `BeamRebarView.xaml` | `BeamRebarView.xaml.cs` | Window (Modeless) | 1100×800 (Min 960×640) | ListBox, ContentControl, RadioButton (3), GridSplitter (2), ProgressBar, Button (3), PackIcon, Custom Canvases | Local `SegmentedRadioButton` ControlTemplate; `PrimaryButton`, `SecondaryButton`, `LinkButton` | Standard MaterialDesign Segmented Toggle/RadioButton or re-based toolkit style; retain window layout |
| **BeamRebar** | `Controls/Beam3DViewport.xaml` | `Beam3DViewport.xaml.cs` | UserControl | Responsive (Dock/Grid) | `helix:HelixViewport3D`, Border (Card), Button, PackIcon, TextBlock | `MaterialDesignPaper`, `MaterialDesignCardBackground`, `MaterialDesignFlatButton` | Already partially uses toolkit tokens; retain 3D viewport unchanged |
| **BeamRebar** | `Tabs/GeometryTabView.xaml` | `GeometryTabView.xaml.cs` | UserControl | ScrollViewer | Border (`Card`), TextBlock, DataGrid (2: Spans, Supports) | `Card`, `Heading`, `BodyStrong`, `Caption`, inline DataGrid brushes | Replace inline DataGrid brushes with `MaterialDesignDataGrid` defaults; `md:Card` |
| **BeamRebar** | `Tabs/MainBarsTabView.xaml` | `MainBarsTabView.xaml.cs` | UserControl | ScrollViewer | Border (`Card` ×3), TextBox (5), ComboBox (6), CheckBox (1) | `Card`, default TextBox/ComboBox (no explicit key), `Spacing.*` | `md:Card`, `NumberTextBox` (for numbers), `MaterialDesignOutlinedComboBox`, `MaterialDesignCheckBox` |
| **BeamRebar** | `Tabs/AdditionalBarsTabView.xaml` | `AdditionalBarsTabView.xaml.cs` | UserControl | ScrollViewer | Border (`Card` ×2), Button (2), ComboBox (4), CheckBox (2), TextBox (8) | `Card`, `SecondaryButton`, default TextBox/ComboBox | `md:Card`, `NumberTextBox`, `MaterialDesignOutlinedComboBox`, `SecondaryButton` |
| **BeamRebar** | `Tabs/StirrupsTabView.xaml` | `StirrupsTabView.xaml.cs` | UserControl | ScrollViewer | Border (`Card` ×3), ComboBox (4), CheckBox (4), TextBox (8) | `Card`, default TextBox/ComboBox | `md:Card`, `NumberTextBox`, `MaterialDesignOutlinedComboBox`, `MaterialDesignCheckBox` |
| **BeamRebar** | `Tabs/ViewsTabView.xaml` | `ViewsTabView.xaml.cs` | UserControl | ScrollViewer | Border (`Card` ×3), CheckBox (4), TextBox (3), ComboBox (2) | `Card`, default TextBox/ComboBox | `md:Card`, `StandardTextBox`, `MaterialDesignOutlinedComboBox` |
| **ColumnRebar** | `ColumnRebarView.xaml` | `ColumnRebarView.xaml.cs` | Window (Modeless) | 1040×720 (Min 880×600) | ListBox, ContentControl, ScrollViewer, ColumnElevationCanvas, ProgressBar, Button (3), Control (Vector icons) | `ResourceKeyToControlTemplateConverter`, `LinkButton`, `SecondaryButton`, `PrimaryButton` | Retain `MaterialThemeBridge.Attach`; maintain elevation canvas; evaluate icon template loading |
| **ColumnRebar** | `Tabs/GeometryTabView.xaml` | `GeometryTabView.xaml.cs` | UserControl | Grid | Border (`Card`), TextBlock, DataGrid (1: Column stack) | `Card`, inline DataGrid brushes | `md:Card`, `MaterialDesignDataGrid` |
| **ColumnRebar** | `Tabs/BarsTabView.xaml` | `BarsTabView.xaml.cs` | UserControl | Grid | Border (`Card` ×3), ComboBox (2), TextBox (4), Button (1), ColumnSectionCanvas, DataGrid (1) | `Card`, `NumberTextBox`, `SecondaryButton`, inline DataGrid brushes | `md:Card`, `NumberTextBox`, `MaterialDesignOutlinedComboBox`, `MaterialDesignDataGrid` |
| **ColumnRebar** | `Tabs/BottomDowelsTabView.xaml` | `BottomDowelsTabView.xaml.cs` | UserControl | Grid | Border (`Card` ×2), ComboBox (1), Button (1), ColumnSectionCanvas, DataGrid (1 with CheckBoxColumn) | `Card`, `SecondaryButton`, inline DataGrid brushes | `md:Card`, `MaterialDesignDataGrid` |
| **ColumnRebar** | `Tabs/TopDowelsTabView.xaml` | `TopDowelsTabView.xaml.cs` | UserControl | Grid | Border (`Card` ×2), ComboBox (1), Button (1), ColumnSectionCanvas, DataGrid (1 with CheckBoxColumn) | `Card`, `SecondaryButton`, inline DataGrid brushes | `md:Card`, `MaterialDesignDataGrid` |
| **ColumnRebar** | `Tabs/StirrupsTabView.xaml` | `StirrupsTabView.xaml.cs` | UserControl | ScrollViewer | Border (`Card` ×2), ComboBox (2), TextBox (4), CheckBox (1), Button (1), DistributionDiagram | `Card`, `NumberTextBox`, `SecondaryButton` | `md:Card`, `NumberTextBox`, `MaterialDesignOutlinedComboBox`, `MaterialDesignCheckBox` |
| **ColumnRebar** | `Tabs/AdditionalStirrupsTabView.xaml` | `AdditionalStirrupsTabView.xaml.cs` | UserControl | ScrollViewer | Border (`Card` ×3), ComboBox (3), TextBox (4), CheckBox (2), Button (1), ColumnSectionCanvas | `Card`, `NumberTextBox`, `SecondaryButton` | `md:Card`, `NumberTextBox`, `MaterialDesignOutlinedComboBox`, `MaterialDesignCheckBox` |
| **ColumnRebar** | `Tabs/BarsDivisionTabView.xaml` | `BarsDivisionTabView.xaml.cs` | UserControl | Grid | Border (`Card`), TextBox (1), Button (1), DataGrid (1: Schedule) | `Card`, `NumberTextBox`, `SecondaryButton`, inline DataGrid brushes | `md:Card`, `NumberTextBox`, `MaterialDesignDataGrid` |
| **ColumnRebar** | `Tabs/SettingTabView.xaml` | `SettingTabView.xaml.cs` | UserControl | ScrollViewer | Border (`Card` ×3), TextBox (4), CheckBox (1) | `Card`, `StandardTextBox`, `NumberTextBox` | `md:Card`, `StandardTextBox`, `NumberTextBox`, `MaterialDesignCheckBox` |
| **FoundationRebar** | `FoundationRebarView.xaml` | `FoundationRebarView.xaml.cs` | Window (Modal) | 750×680 (Min 650×550) | TabControl (2 tabs), Border (`Badge`), Border (Error banner), Button (2) | `Badge`, `BadgeText`, `SecondaryButton`, `PrimaryButton` | `md:Card`, `MaterialDesignTabControl`, `MaterialDesignRaisedButton`, `MaterialDesignOutlinedButton` |
| **FoundationRebar** | `FoundationGeometryView.xaml` | `FoundationGeometryView.xaml.cs` | UserControl | ScrollViewer | Border (`Card` ×2), TextBox (5: ReadOnly) | `Card`, `NumberTextBox` (ReadOnly) | `md:Card`, `NumberTextBox` (with `IsReadOnly="True"`) |
| **FoundationRebar** | `FoundationSettingView.xaml` | `FoundationSettingView.xaml.cs` | UserControl | ScrollViewer | Border (`Card` ×3), TextBox (8), ComboBox (5), CheckBox (1) | `Card`, `NumberTextBox`, default ComboBox | `md:Card`, `NumberTextBox`, `MaterialDesignOutlinedComboBox`, `MaterialDesignCheckBox` |
| **KataExport** | `KataExportView.xaml` | `KataExportView.xaml.cs` | Window (Modeless) | 1180×900 (Min 960×680) | `md:Card` (5), Button (12), CheckBox (4), ListBox (PreviewColumns), ComboBox (ParamPicker), Canvas (KataElevationCanvas), PackIcon (20+) | Local `ParamPickerComboBoxStyle`, `KataCardTitle`, `PropertyLabel`, `PropertyCellBorder`, ListBoxItem template | Standardize `ParamPicker` popup with MaterialDesign; retain canvas & custom Excel listbox layout |
| **KataExport** | `KataSettingsView.xaml` | `KataSettingsView.xaml.cs` | Window (Modal) | 460 × SizeToContent (NoResize) | PackIcon, TextBox (14), ComboBox (2), Button (3) | Local `SettingBox` (BasedOn `NumberTextBox`), `SettingLabel`, `SettingUnit`, `GroupHeader` | Clean up local styles; use `NumberTextBox` / `MaterialDesignOutlinedTextBox` with HintAssist or suffix |
| **KataRebar** | `KataRebarView.xaml` | `KataRebarView.xaml.cs` | Window (Modeless) | 1120×780 (Min 920×620) | Border (`CardBorderStyle`), TabControl (4 tabs), DataGrid (4), Button (4), ListBox, ProgressBar | Local `CardBorderStyle`, `BadgeBorderStyle`, `DataGridHeaderStyle`, `DataGridRowStyle`, Buttons lack style keys | Replace local styles with `md:Card`, `MaterialDesignTabControl`, `MaterialDesignDataGrid`, `PrimaryButton`, `SecondaryButton` |
| **RebarPartition** | `RebarPartitionView.xaml` | `RebarPartitionView.xaml.cs` | Window (Modeless) | 1100×720 (Min 900×580) | Border (`CardBorderStyle`), ComboBox (2), TextBox (3), CheckBox (multi), Button (4), DataGrid (1), ProgressBar | Already uses `MaterialDesignOutlinedComboBox`, `MaterialDesignOutlinedTextBox`, `MaterialDesignOutlinedButton`, `MaterialDesignRaisedButton`; local card/grid styles | Remove local `CardBorderStyle`, `DataGridHeaderStyle`, `DataGridRowStyle` in favor of toolkit defaults |
| **McpBridge** | `McpBridgeStatusView.xaml` | `McpBridgeStatusView.xaml.cs` | Window (Modeless) | 560×640 (Min 480×520) | Border (`Card` ×3), Ellipse (`StatusDot`), Button (4), CheckBox (2), TextBox (1) | `Card`, `StatusDot`, `PrimaryButton`, `SecondaryButton`, `LinkButton`, `StandardTextBox` | Keep existing clean design; `MaterialDesignCard` / `Card` |

---

## 3. Deep-Dive Current Styling Architecture & Discrepancies

### 3.1 Master Theme Architecture (`Resources/Themes/`)
The repository's XAML styling pipeline is structured as follows:
```
Resources/Themes/
├── Theme.xaml              ← Master ResourceDictionary
│   ├── MaterialBridge.xaml ← Toolkit CustomColorTheme + MaterialDesign2.Defaults.xaml + Segoe UI override + Re-based button/textbox styles
│   ├── ThemeDark.xaml      ← Hand-tuned dark color palette tokens (Brush.*, Color.*, Brush.Canvas.*)
│   ├── Typography.xaml     ← Font sizes (Caption, Body, BodyStrong, Subheading, Heading, Title) & TextBlock styles
│   ├── Spacing.xaml        ← Standard spacing tokens (multiples of 4; Spacing.Small, Medium, Large, etc.)
│   ├── Controls.xaml       ← Legacy Border styles (Card, Separator, Badge, BadgeText, Tag)
│   └── ThemeIcons.xaml     ← Vector rebar ControlTemplates (TabIcon.Geometry, TabIcon.Bars, etc.)
└── MaterialThemeBridge.cs  ← C# runtime theme bridge (attaches to RevitHostTheme, creates overlay dictionary swapping ThemeDark/ThemeLight)
```

### 3.2 Redundant & Ad-Hoc Styles Identified for Elimination or Refactoring

1. **`KataRebarView.xaml` (Lines 29–69)**:
   - `CardBorderStyle`: Custom `Border` with `Background="{DynamicResource Brush.Surface}"`, `CornerRadius="6"`, `BorderBrush="{DynamicResource Brush.Border}"`.  
     *Action*: Replace with `md:Card` or `{StaticResource Card}` from `Controls.xaml`.
   - `BadgeBorderStyle`: Custom `Border` with `Background="{DynamicResource Brush.GridLine}"`, `CornerRadius="4"`.  
     *Action*: Replace with `{StaticResource Badge}` from `Controls.xaml`.
   - `DataGridHeaderStyle` & `DataGridRowStyle`: Hand-written DataGrid header and mouseover triggers.  
     *Action*: Remove; let `MaterialDesign2.Defaults.xaml` style the DataGrid headers and alternating/hover states natively, ensuring Dark/Light dynamic tokens apply.
   - Unstyled Buttons (Lines 146, 157, 320, 328): Currently use raw `<Button Content="..."/>` without style keys.  
     *Action*: Apply `Style="{DynamicResource PrimaryButton}"` to "Tạo cốt thép 3D" and `Style="{DynamicResource SecondaryButton}"` to "Đọc lại Excel", "Chọn dầm", "Đóng".

2. **`RebarPartitionView.xaml` (Lines 37–70)**:
   - Contains identical duplicated copies of `CardBorderStyle`, `BadgeBorderStyle`, `DataGridHeaderStyle`, `DataGridRowStyle`.  
     *Action*: Remove local definitions; adopt `md:Card` and default MaterialDesign DataGrid styles.

3. **`KataExportView.xaml` (Lines 31–141)**:
   - `KataCardTitle`, `PropertyLabel`, `PropertyCellBorder`: Custom styles for the B3:B10 property grid.  
     *Action*: Retain necessary structural alignments for the compact CAD property table, but ensure all foregrounds/borders bind to `{DynamicResource Brush.*}`.
   - `ParamPickerComboBoxStyle`: A 90-line custom `ControlTemplate` for a mini 25×22 cog-icon popup.  
     *Action*: Retain or refine to use toolkit popup brushes (`MaterialDesignPaper` / `Brush.SurfaceElevated`), ensuring no hardcoded background leaks.

4. **`BeamRebarView.xaml` (Lines 33–63)**:
   - `SegmentedRadioButton`: Custom `ControlTemplate` for the 2D / 3D / Split preview mode switch.  
     *Action*: Keep the functional segmented behavior, but re-base or bind colors to `Brush.Accent`, `Brush.Foreground.OnAccent`, and `Brush.SurfaceHover`.

5. **`BeamRebar` Tab Views (`MainBarsTabView`, `AdditionalBarsTabView`, `StirrupsTabView`, `ViewsTabView`)**:
   - Numerical and selection controls specify hardcoded `Height="28"` without explicit styles:  
     `<TextBox Text="{Binding Session.TopBarCount...}" Height="28" Margin="..."/>`  
     `<ComboBox ItemsSource="{Binding Session.BarTypes...}" Height="28" Margin="..."/>`  
     *Action*: Assign `Style="{DynamicResource NumberTextBox}"` to all numerical inputs, and standard `MaterialDesignOutlinedComboBox` or `StandardComboBox` to ComboBoxes.

---

## 4. 100% Functional Requirements Preservation Mapping

### 4.1 Numeric Inputs with Right-Alignment (`TextAlignment="Right"`)
All dimensional, count, and ratio inputs must be right-aligned with monospace font to ensure technical clarity for structural engineers.

**Catalog of Numeric Fields by Module**:

| Module | Tab / View | Property / Binding | Recommended Style | Key Settings |
| :--- | :--- | :--- | :--- | :--- |
| **BeamRebar** | MainBarsTab | `Session.TopBarCount` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **BeamRebar** | MainBarsTab | `Session.BottomBarCount` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **BeamRebar** | MainBarsTab | `Session.Cover` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **BeamRebar** | MainBarsTab | `Session.MaxStockLength` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **BeamRebar** | MainBarsTab | `Session.LapFactor` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **BeamRebar** | AdditionalBarsTab | `Layer1Count`, `Layer1ExtensionRatio` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **BeamRebar** | AdditionalBarsTab | `Layer2Count`, `Layer2ExtensionRatio`, `LayerGap` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **BeamRebar** | AdditionalBarsTab | `CutoffRatio` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **BeamRebar** | StirrupsTab | `Session.StirrupSpacingDense` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **BeamRebar** | StirrupsTab | `Session.StirrupSpacingSparse` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **BeamRebar** | StirrupsTab | `Session.StirrupStartOffset` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **BeamRebar** | StirrupsTab | `Session.DepthThreshold` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **BeamRebar** | StirrupsTab | `Session.MaxVerticalSpacing` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **BeamRebar** | StirrupsTab | `Session.CrossTieSpacing` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **BeamRebar** | StirrupsTab | `Session.HangingStirrupsPerSide` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **BeamRebar** | StirrupsTab | `Session.HangingStirrupSpacing` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **ColumnRebar** | BarsTab | `BarsAlongWidth`, `BarsAlongDepth`, `BarsAround` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **ColumnRebar** | BarsTab | `OverlapFactor` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **ColumnRebar** | StirrupsTab | `Cover`, `Spacing`, `SpacingDense`, `SpacingSparse` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **ColumnRebar** | AdditionalStirrupsTab | `HorizontalTieLeg`, `HorizontalTieCount` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **ColumnRebar** | AdditionalStirrupsTab | `VerticalTieLeg`, `VerticalTieCount` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **ColumnRebar** | BarsDivisionTab | `Session.IdenticalColumns` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **ColumnRebar** | SettingTab | `Session.IdenticalColumns` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **FoundationRebar** | FoundationGeometryView | `Length`, `Width`, `Thickness`, `TopElevation`, `BottomElevation` | `NumberTextBox` (ReadOnly) | `HorizontalContentAlignment="Right"`, Mono Font |
| **FoundationRebar** | FoundationSettingView | `SpacingBottomX`, `SpacingBottomY`, `SpacingTopX`, `SpacingTopY` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **FoundationRebar** | FoundationSettingView | `CoverTop`, `CoverBottom`, `CoverSide`, `HookLength` | `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |
| **KataSettings** | KataSettingsView | All 14 numeric fields (cutoffs, hooks, clear gaps, offsets) | `SettingBox` / `NumberTextBox` | `HorizontalContentAlignment="Right"`, Mono Font |

### 4.2 Data Validation Rules, Error Templates & Validation Triggers
- **ViewModel Validation Banners**:
  - `FoundationRebarView`: Grid Row 2 displays an error banner bounded by `Visibility="{Binding HasValidationError, Converter={StaticResource BoolToVis}}"`, styled with `Background="{DynamicResource Brush.Danger}"`. Must not be truncated.
  - `KataExportView`: Collapsible warnings card (`Visibility="{Binding HasWarnings, Converter={StaticResource BoolToVis}}"`) with warning color styling and ItemsControl bullet items.
  - `KataRebarView`: Status text with `DataTrigger Binding="{Binding HasError}" Value="True"` switching foreground to `Brush.Danger`.
  - `McpBridgeStatusView`: StatusDot ellipse reflecting status kind (`Listening` → `Brush.Info`, `Connected` → `Brush.Success`, `Busy` → `Brush.Warning`, `Error` → `Brush.Danger`).
- **MaterialDesign Input Validation**:
  - When migrating inputs to `MaterialDesignOutlinedTextBox`, ensure `md:ValidationAssist.Suppress="False"` is configured appropriately so field-level validation errors (if bound via `IDataErrorInfo` / `INotifyDataErrorInfo`) render without collapsing the grid row or distorting neighboring columns.

### 4.3 2D/3D Rebar Preview Geometry Canvases / Helix Viewports & Technical Colors

**Strict Rule**: Technical rebar display colors are engineering semantic tokens. They MUST NEVER be overwritten with brand colors or muted theme surface tones.

1. **Helix 3D Viewport (`Beam3DViewport.xaml` & `Beam3DMaterials.cs`)**:
   - `Concrete`: Semi-transparent LightSteelBlue (`Alpha ~35%`, `#59B0C4DE`).
   - `Support`: Semi-opaque Grey (`Alpha ~50%`, `#809E9E9E`).
   - `MainTopBar`: Crimson Red (`#E5484D`).
   - `MainBottomBar`: Dodger Blue (`#1E88E5`).
   - `AdditionalTopBar`: Coral / Salmon (`#FF7043`).
   - `AdditionalBottomBar`: Cyan (`#29B6F6`).
   - `SideBar`: Forest Green (`#43A047`).
   - `CrossTie`: Teal (`#26A69A`).
   - `Stirrup`: Amber Gold (`#FFB300`).
   - `SpecialBar`: Purple Orchid (`#AB47BC`).
   - *Requirement*: All materials are frozen WPF `MaterialGroup` instances with diffuse and specular components. Viewport background binds to `{DynamicResource MaterialDesignPaper}`.

2. **2D Canvases (`BeamElevationCanvas`, `BeamSectionCanvas`, `ColumnElevationCanvas`, `ColumnSectionCanvas`)**:
   - Managed via `CanvasPalette.cs` resolving:
     - `Brush.Canvas.Fill` (`#252526` Dark / `#FAFAFA` Light)
     - `Brush.Canvas.Bound` (`#CCCCCC` Dark / `#666666` Light)
     - `Brush.Canvas.MainBar` (`#E5484D`)
     - `Brush.Canvas.MainBar.Selected` (`#F5A623`)
     - `Brush.Canvas.Stirrup` (`#16C172`)
     - `Brush.Canvas.Tag` (`#8E8E93`)
   - *Requirement*: `ThemeDark.xaml` and `ThemeLight.xaml` must continue to define all `Brush.Canvas.*` and `Color.Canvas.*` tokens.

3. **Kata 2D Elevation & Detailing Canvas (`KataElevationCanvas.cs`, `KataCanvasPalette.cs`)**:
   - Renders multi-span continuous beams, column faces, section cuts, bar tags, and dimension lines.
   - Interactive features: Zoom, Pan, Drag-to-Edit, Bar deletion, Tooltips.
   - Background canvas surface bound to `{DynamicResource Brush.Canvas.Fill}`.

### 4.4 Layout Dimensions, Splitters, Margins, and Paddings
- **Window Minimum Boundaries**:
  - `BeamRebarView`: MinWidth=960, MinHeight=640 (Height of preview container Row 1 is 280, MinHeight=160; Navigation column Width=230).
  - `ColumnRebarView`: MinWidth=880, MinHeight=600 (Elevation preview Row 1 Height=240; Navigation column Width=220).
  - `KataExportView`: MinWidth=960, MinHeight=680 (B3:B10 column Width=240, Row 2 MinHeight=260).
  - `KataRebarView`: MinWidth=920, MinHeight=620 (Left pane Width=380).
  - `FoundationRebarView`: MinWidth=650, MinHeight=550.
- **Splitters**:
  - `BeamRebarView`: Horizontal GridSplitter (Row 1, Height=6) and Vertical GridSplitter in split mode (Column 1, Width=6).
- **Responsive Scrolling**:
  - All tab bodies wrapped in `ScrollViewer` with `VerticalScrollBarVisibility="Auto"` and `HorizontalScrollBarVisibility="Disabled"` (except canvas areas which support horizontal scrolling).

### 4.5 MVVM Bindings, Commands, Converters, and Vietnamese Localization
- **Bindings & Commands**:
  - 100% of existing bindings (e.g. `Session.*`, `Geometry.*`, `Setting.*`, `PreviewColumns`, `SelectedTab`) and RelayCommands (`RunCommand`, `CancelCommand`, `ApplyAllColumnsCommand`, `GenerateRebarCommand`, `ExportCommand`, `RepickCommand`, etc.) must remain unchanged.
- **Converters**:
  - `<BooleanToVisibilityConverter x:Key="BoolToVis"/>`
  - `<controls:ResourceKeyToControlTemplateConverter x:Key="ResourceKeyToControlTemplateConverter"/>`
  - `<MultiBinding StringFormat="{}{0:0} × {1:0} mm">`
- **Vietnamese Text Strings**:
  - All localized titles, section labels, tooltips, and status prompts must be preserved verbatim.

---

## 5. Module-by-Module Migration Checklist and Plan

### Phase 1: Theme & Infrastructure Consolidation (`Resources/Themes/`)
- [ ] Verify `MaterialBridge.xaml` is merged first in `Theme.xaml`.
- [ ] Add explicit implicit styles for `TabControl`, `DataGrid`, `CheckBox`, and `RadioButton` in `MaterialBridge.xaml` (or verified toolkit defaults) so tab views receive modern styling automatically without manual attributes.
- [ ] Confirm `MaterialDesignFont` remains strictly overridden with `Segoe UI`.
- [ ] Ensure all `Brush.Canvas.*` tokens remain intact in both `ThemeDark.xaml` and `ThemeLight.xaml`.
- [ ] Run `dotnet test HPRebar.Core.Tests` to verify `ThemeTokenCoverageTests` passes after every theme dictionary modification.

### Phase 2: BeamRebar Module Migration
- [ ] **`BeamRebarView.xaml`**:
  - Refactor `SegmentedRadioButton` to adopt MaterialDesign styling while preserving the segmented selection UX and triggers.
  - Standardize GridSplitters to use `MaterialDesignDivider` or `Brush.Border`.
  - Maintain 2D/3D/Split layout switching and bindings.
- [ ] **`Tabs/GeometryTabView.xaml`**:
  - Remove inline DataGrid row/cell brushes in favor of toolkit DataGrid style.
  - Retain `Border Style="{DynamicResource Card}"` or switch to `md:Card`.
- [ ] **`Tabs/MainBarsTabView.xaml`**, **`AdditionalBarsTabView.xaml`**, **`StirrupsTabView.xaml`**, **`ViewsTabView.xaml`**:
  - Upgrade all numeric TextBoxes to `Style="{DynamicResource NumberTextBox}"` with right-alignment.
  - Standardize ComboBoxes to `Style="{StaticResource MaterialDesignOutlinedComboBox}"` or `StandardComboBox`.
  - Standardize CheckBoxes to `MaterialDesignCheckBox`.

### Phase 3: ColumnRebar Module Migration
- [ ] **`ColumnRebarView.xaml`**:
  - Inspect tab list icon rendering in `ResourceKeyToControlTemplateConverter` to ensure robust template resolution in all Revit contexts.
  - Preserve `ColumnElevationCanvas` viewport and scrolling behavior.
- [ ] **`Tabs/GeometryTabView.xaml`** & **`BarsDivisionTabView.xaml`**:
  - Remove manual DataGrid color setters; adopt toolkit DataGrid styling.
- [ ] **`Tabs/BarsTabView.xaml`**, **`BottomDowelsTabView.xaml`**, **`TopDowelsTabView.xaml`**, **`StirrupsTabView.xaml`**, **`AdditionalStirrupsTabView.xaml`**, **`SettingTabView.xaml`**:
  - Standardize all inputs with `NumberTextBox` / `StandardTextBox`.
  - Preserve `ColumnSectionCanvas` (including `GhostColumn` overlap visualization) and `DistributionDiagram`.

### Phase 4: FoundationRebar Module Migration
- [ ] **`FoundationRebarView.xaml`**:
  - Remove inline `TabControl` colors; adopt toolkit `MaterialDesignTabControl`.
  - Preserve error banner with `Brush.Danger`.
  - Retain header badge and footer button hierarchy.
- [ ] **`FoundationGeometryView.xaml`** & **`FoundationSettingView.xaml`**:
  - Retain `NumberTextBox` on all numeric fields (with `IsReadOnly="True"` for geometry dimensions).
  - Modernize section headers with `md:PackIcon`.

### Phase 5: KataExport Module Migration
- [ ] **`KataExportView.xaml`**:
  - Replace local `KataCardTitle`, `PropertyLabel`, `PropertyCellBorder` with clean toolkit-aligned styles.
  - Refine `ParamPickerComboBoxStyle` popup to ensure background and hover colors track dynamic themes.
  - Preserve `KataElevationCanvas` with all interactive capabilities (zoom, pan, drag-to-edit, tag/section toggles).
  - Retain horizontal virtualization for Excel columns C..BZ in `PreviewColumns` ListBox.
- [ ] **`KataSettingsView.xaml`**:
  - Clean up `SettingBox`, `SettingLabel`, `SettingUnit`, `GroupHeader`.
  - Ensure all 14 numeric inputs retain right-alignment and mono font.

### Phase 6: KataRebar & RebarPartition Modules Migration
- [ ] **`KataRebarView.xaml`**:
  - Remove local `CardBorderStyle`, `BadgeBorderStyle`, `DataGridHeaderStyle`, `DataGridRowStyle`.
  - Replace cards with `md:Card` or `{StaticResource Card}`.
  - Apply `PrimaryButton` and `SecondaryButton` to all buttons currently lacking style keys.
  - Adopt toolkit DataGrid styling across all 4 preview tables.
- [ ] **`RebarPartitionView.xaml`**:
  - Remove redundant local style copies.
  - Connect preview DataGrid directly to toolkit styles.

### Phase 7: Verification & Build Quality Gate
- [ ] Execute `dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false` (must compile with 0 errors).
- [ ] Execute `dotnet test HPRebar.Core.Tests` (must pass 100%, including `ThemeTokenCoverageTests`).
- [ ] Verify that dark/light runtime theme switching updates instantly via `MaterialThemeBridge`.
- [ ] Verify all 5 Revit configurations (`Debug.R23` through `Debug.R27`).

---

## 6. Summary of Architectural Decisions & Risk Mitigations

1. **Font Safety**: Never reference the Roboto font or `{md:MaterialDesignFont}`. `Segoe UI` is strictly mandated due to ILRepack limitations in Revit add-ins.
2. **DynamicResource Enforcement**: Never hardcode colors (`#1E1E1E`) or use `{StaticResource ...}` on brushes. Every visual state must use `{DynamicResource Brush.*}` so `MaterialThemeBridge.Apply()` succeeds during runtime theme changes.
3. **Canvas Isolation**: Technical rebar colors in `CanvasPalette.cs` and `Beam3DMaterials.cs` must remain completely separate from application chrome colors.
4. **DataGrid Compatibility**: Avoid overriding `ControlTemplate` on DataGrid cells directly; use column styles or cell templates that bind to `DynamicResource` to prevent performance degradation on large tables.
