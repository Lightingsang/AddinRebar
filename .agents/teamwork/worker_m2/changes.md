# Changes Summary — Worker M2 (Column & Beam Rebar UI Views Migration)

**Milestone:** M2 (ColumnRebar & BeamRebar Views Migration)  
**Date:** 2026-10-10  
**Target:** Standardization to MaterialDesignInXamlToolkit v5.3.2 while preserving 100% domain invariants.

---

## 1. Files Modified

### ColumnRebar Module (`HPRebar/HPRebar/ColumnRebar/View/`)
1. **`Controls/ResourceKeyToControlTemplateConverter.cs`**:
   - Replaced fragile `System.Windows.Application.Current?.TryFindResource(key)` lookup with safe guarding and bundled `ThemeIcons.xaml` dictionary fallback (`pack://application:,,,/HPRebar;component/Resources/Themes/ThemeIcons.xaml`).
   - Ensures tab header vector icons resolve reliably in Revit in-process sessions where `Application.Current` is null or disjoint.
2. **`Tabs/GeometryTabView.xaml`**:
   - Removed hardcoded DataGrid brushes (`Background`, `RowBackground`, `AlternatingRowBackground`, `Foreground`, `BorderBrush`, `HorizontalGridLinesBrush`).
   - DataGrid inherits implicit `MaterialDesignDataGrid` style from `Theme.xaml`.
3. **`Tabs/BarsTabView.xaml`**:
   - Removed hardcoded DataGrid brushes (`RowBackground`, `AlternatingRowBackground`, `Background`, `Foreground`, `BorderBrush`).
   - Maintained `NumberTextBox` for numeric rebar counts and dimensions with right-alignment.
4. **`Tabs/BottomDowelsTabView.xaml`**:
   - Removed hardcoded DataGrid brushes (`RowBackground`, `AlternatingRowBackground`, `Background`, `Foreground`, `BorderBrush`).
   - Maintained checkbox columns, converters, and column canvas bindings.
5. **`Tabs/TopDowelsTabView.xaml`**:
   - Removed hardcoded DataGrid brushes (`RowBackground`, `AlternatingRowBackground`, `Background`, `Foreground`, `BorderBrush`).
   - Preserved column canvas and splice parameters.
6. **`Tabs/BarsDivisionTabView.xaml`**:
   - Removed hardcoded DataGrid brushes (`RowBackground`, `AlternatingRowBackground`, `Background`, `Foreground`, `BorderBrush`).
   - Preserved schedule columns and number formatting.

### BeamRebar Module (`HPRebar/HPRebar/BeamRebar/View/`)
1. **`Tabs/GeometryTabView.xaml`**:
   - Removed hardcoded DataGrid brushes from both `Spans` and `Supports` DataGrids (`Background`, `RowBackground`, `AlternatingRowBackground`, `Foreground`, `BorderBrush`, `HorizontalGridLinesBrush`).
   - Inherits `MaterialDesignDataGrid` implicit styles.
2. **`Tabs/MainBarsTabView.xaml`**:
   - Modernized all numeric TextBoxes (`Session.TopBarCount`, `Session.BottomBarCount`, `Session.Cover`, `Session.MaxStockLength`, `Session.LapFactor`) to `Style="{DynamicResource NumberTextBox}"` ensuring monospaced, right-aligned numeric presentation.
   - Removed rigid `Height="28"` on Outlined ComboBoxes so standard MaterialDesign padding and corner radius render without clipping.
3. **`Tabs/AdditionalBarsTabView.xaml`**:
   - Modernized all numeric TextBoxes (`Layer1Count`, `Layer1ExtensionRatio`, `Layer2Count`, `Layer2ExtensionRatio`, `LayerGap`, `CutoffRatio`) to `Style="{DynamicResource NumberTextBox}"`.
   - Removed rigid `Height="28"` on ComboBoxes and action buttons.
4. **`Tabs/StirrupsTabView.xaml`**:
   - Modernized all numeric TextBoxes (`Session.StirrupSpacingDense`, `Session.StirrupSpacingSparse`, `Session.StirrupStartOffset`, `Session.DepthThreshold`, `Session.MaxVerticalSpacing`, `Session.CrossTieSpacing`, `Session.HangingStirrupsPerSide`, `Session.HangingStirrupSpacing`) to `Style="{DynamicResource NumberTextBox}"`.
   - Removed rigid `Height="28"` on ComboBoxes.
5. **`Tabs/ViewsTabView.xaml`**:
   - Modernized text-based naming TextBoxes (`Session.DetailViewName`, `Session.SectionPrefix`, `Session.PartitionName`) to `Style="{DynamicResource StandardTextBox}"`.
   - Removed rigid `Height="28"` on ComboBoxes.
6. **`BeamRebarView.xaml` & `Controls/Beam3DViewport.xaml`**:
   - Preserved 100% of domain invariants:
     * Custom `SegmentedRadioButton` control template (lines 33-63) for 2D/3D preview mode toggling.
     * 3D Helix viewport controls and material colors in `Beam3DMaterials.cs`.
     * 2D canvas drawing primitives and `CanvasPalette.cs` technical rebar brushes.
     * All localized Vietnamese strings, converters, and MVVM bindings.

---

## 2. Style Mapping Matrix

| Original Element | New Style / Standard | Target Theme Dictionary | Notes |
| :--- | :--- | :--- | :--- |
| `DataGrid` with manual `RowBackground` / `AlternatingRowBackground` | `MaterialDesignDataGrid` (implicit) | `Theme.xaml` | Follows theme surface tokens dynamically in Dark/Light |
| Numeric `TextBox` (e.g. `TopBarCount`, `Cover`) | `NumberTextBox` | `MaterialBridge.xaml` | Right-aligned (`HorizontalContentAlignment="Right"`), monospaced font |
| String `TextBox` (e.g. `DetailViewName`, `PartitionName`) | `StandardTextBox` | `MaterialBridge.xaml` | Outlined style with rounded border and floating hint support |
| ComboBox with hardcoded `Height="28"` | `StandardComboBox` / implicit | `MaterialBridge.xaml` | Removed clipping `Height="28"`, uses toolkit outlined padding |
| `ResourceKeyToControlTemplateConverter` | Safe Guard + Fallback Dictionary | `ThemeIcons.xaml` | Resolves templates when `Application.Current` is null in Revit |

---

## 3. Verification Commands & Results

1. **Compilation of `HPRebar.csproj`**:
   ```bash
   dotnet build HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false
   ```
   - **Exit code**: `0`
   - **Result**: `Build succeeded. 32 Warning(s), 0 Error(s). Wrote HPRebar.dll`

2. **Full Core Unit & Integration Tests**:
   ```bash
   dotnet test HPRebar.Core.Tests
   ```
   - **Exit code**: `1` (1691 Passed, 1 Failed — the only failure is in out-of-scope `RebarPartitionView.xaml` owned by M3)
   - Zero failures across all ColumnRebar and BeamRebar components.

3. **E2E Acceptance Tests**:
   ```bash
   dotnet test HPRebar.Core.Tests --filter-class *MaterialDesignMigrationE2ETests*
   ```
   - **Exit code**: `0` (10/10 Passed)
