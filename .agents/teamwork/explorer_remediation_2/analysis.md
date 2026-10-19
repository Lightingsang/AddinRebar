# Deep-Dive UI Analysis: Remediation of JoinElementView.xaml

**Document**: Analysis Report — Explorer 2 (JoinElements UI Specialist)  
**Date**: 2026-10-10  
**Target File**: `HPRebar/HPRebar/JoinElements/View/JoinElementView.xaml`  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_remediation_2\`  
**Context**: Remediation for Victory Audit Rejection (Phase B Integrity Check — R2: Elimination of redundant HP styles)

---

## 1. Executive Summary

During the final Victory Audit for the HPRebar MaterialDesign Migration project, Phase B Integrity Check failed with the following finding:
> **R2: Elimination of redundant HP styles: FAIL.**  
> Target modules (Column, Beam, Foundation, Kata, Bridge) are cleanly migrated, but unmerged code in the compilation tree (`HPRebar/HPRebar/JoinElements/View/JoinElementView.xaml`, lines 30-60) retains deprecated custom styles (`SectionHeaderStyle`, `CardBorderStyle`, `BadgeBorderStyle`, `DataGridHeaderStyle`).

A comprehensive repository scan confirmed that **`JoinElementView.xaml` is the only file remaining across the entire `HPRebar` codebase** that defines and references these deprecated styles.

This investigation provides:
1. Complete catalog of the 5 redundant custom styles defined in `Window.Resources` (lines 30-70).
2. Trace of every single usage (15 occurrences) across `JoinElementView.xaml`.
3. Concrete mapping to standard `MaterialDesignInXamlToolkit` v5.3.2 and `Theme.xaml` tokens (`Card`, `Tag`, `Subheading`, implicit `MaterialDesignDataGrid`, `PrimaryButton`, `SecondaryButton`, `LinkButton`).
4. Full audit verifying that 100% of MVVM bindings, commands, converters, event handlers, and Vietnamese text are preserved.
5. Exact unified diff and actionable replacement plan for the remediation coder.

---

## 2. Inventory of Redundant Custom Styles in `Window.Resources`

All 5 redundant styles are defined in `JoinElementView.xaml` between lines 30 and 70:

| # | Style Key | TargetType | Defined Lines | Original Definition | Reason for Deprecation |
|---|---|---|---|---|---|
| 1 | `SectionHeaderStyle` | `TextBlock` | 30–35 | `FontWeight="SemiBold"`, `FontSize="{DynamicResource Font.Size.Body}"`, `Foreground="{DynamicResource Brush.Foreground.Primary}"`, `Margin="0,0,0,6"` | Redundant with standard typography token `{DynamicResource Subheading}` defined in `Typography.xaml` (merged via `Theme.xaml`). |
| 2 | `CardBorderStyle` | `Border` | 37–44 | `Background="{DynamicResource Brush.Surface}"`, `BorderBrush="{DynamicResource Brush.Border}"`, `BorderThickness="1"`, `CornerRadius="6"`, `Padding="14"`, `Margin="0,0,0,10"` | Redundant with standard container style `{DynamicResource Card}` defined in `Controls.xaml` (merged via `Theme.xaml`). |
| 3 | `BadgeBorderStyle` | `Border` | 46–51 | `Background="{DynamicResource Brush.GridLine}"`, `CornerRadius="4"`, `Padding="8,4"`, `Margin="0,0,8,0"` | Redundant with standard chip/tag style `{DynamicResource Tag}` defined in `Controls.xaml` (merged via `Theme.xaml`). |
| 4 | `DataGridHeaderStyle` | `DataGridColumnHeader` | 53–60 | `FontWeight="SemiBold"`, `Foreground="{DynamicResource Brush.Foreground.Primary}"`, `Background="{DynamicResource Brush.Surface}"`, `Padding="8,6"`, `BorderBrush="{DynamicResource Brush.GridLine}"`, `BorderThickness="0,0,1,1"` | Redundant with implicit `MaterialDesignDataGrid` header styling automatically provided by `Theme.xaml` (Line 25: `<Style TargetType="DataGrid" BasedOn="{StaticResource MaterialDesignDataGrid}"/>`). |
| 5 | `DataGridRowStyle` | `DataGridRow` | 62–70 | `Background="Transparent"`, `Foreground="{DynamicResource Brush.Foreground.Primary}"`, MouseOver trigger with `Background="{DynamicResource Brush.GridLine}"` | Redundant with implicit `MaterialDesignDataGrid` row hover and selection styling provided by `MaterialDesign2.Defaults.xaml`. |

---

## 3. Usage Trace in `JoinElementView.xaml`

The 5 redundant styles are used across 15 locations in `JoinElementView.xaml`:

### 3.1. Usages of `CardBorderStyle` (6 locations)
1. **Line 82**: `<Border Grid.Row="0" Style="{StaticResource CardBorderStyle}" Padding="12,10" Margin="0,0,0,12">`  
   *Container*: Header Information Bar.  
   *Replacement*: `Style="{DynamicResource Card}" Padding="12,10" Margin="0,0,0,12"`
2. **Line 140**: `<Border Style="{StaticResource CardBorderStyle}">`  
   *Container*: Left panel section 1 (Scope selection).  
   *Replacement*: `Style="{DynamicResource Card}" Margin="0,0,0,10"`
3. **Line 158**: `<Border Style="{StaticResource CardBorderStyle}">`  
   *Container*: Left panel section 2 (Execution options).  
   *Replacement*: `Style="{DynamicResource Card}" Margin="0,0,0,10"`
4. **Line 189**: `<Border Style="{StaticResource CardBorderStyle}" Background="#0A2196F3" BorderBrush="#332196F3">`  
   *Container*: Left panel section 3 (Feature Tip info card).  
   *Replacement*: `Style="{DynamicResource Card}" Margin="0,0,0,10" Background="#0A2196F3" BorderBrush="#332196F3"`
5. **Line 206**: `<Border Grid.Column="1" Style="{StaticResource CardBorderStyle}" Padding="0">`  
   *Container*: Right panel wrapper for Priority Matrix DataGrid.  
   *Replacement*: `Style="{DynamicResource Card}" Padding="0"`
6. **Line 280**: `<Border Grid.Row="2" Style="{StaticResource CardBorderStyle}" Padding="12,10" Margin="0,8,0,0">`  
   *Container*: Footer Bar (Status, ProgressBar, Action Buttons).  
   *Replacement*: `Style="{DynamicResource Card}" Padding="12,10" Margin="0,8,0,0"`

### 3.2. Usages of `BadgeBorderStyle` (5 locations)
In Header Information Bar WrapPanel (Lines 95–125):
1. **Line 95**: `<Border Style="{StaticResource BadgeBorderStyle}">`  
   *Content*: "Cặp va chạm: {TotalCandidatePairs}"  
   *Replacement*: `<Border Style="{DynamicResource Tag}" Margin="0,0,8,0">`
2. **Line 101**: `<Border Style="{StaticResource BadgeBorderStyle}">`  
   *Content*: "Đã Join: {TotalJoined}" (Green text)  
   *Replacement*: `<Border Style="{DynamicResource Tag}" Margin="0,0,8,0">`
3. **Line 107**: `<Border Style="{StaticResource BadgeBorderStyle}">`  
   *Content*: "Đảo cắt: {TotalSwitched}" (Blue text)  
   *Replacement*: `<Border Style="{DynamicResource Tag}" Margin="0,0,8,0">`
4. **Line 113**: `<Border Style="{StaticResource BadgeBorderStyle}">`  
   *Content*: "Đã chuẩn: {TotalSkipped}" (Orange text)  
   *Replacement*: `<Border Style="{DynamicResource Tag}" Margin="0,0,8,0">`
5. **Line 119**: `<Border Style="{StaticResource BadgeBorderStyle}">`  
   *Content*: "Thời gian: {ExecutionTimeText}"  
   *Replacement*: `<Border Style="{DynamicResource Tag}">`

### 3.3. Usages of `SectionHeaderStyle` (2 locations)
1. **Line 144**: `<TextBlock Text="1. Phạm vi cấu kiện (Scope)" Style="{StaticResource SectionHeaderStyle}" Margin="6,0,0,0"/>`  
   *Replacement*: `Style="{DynamicResource Subheading}" Margin="6,0,0,0"`
2. **Line 162**: `<TextBlock Text="2. Tùy chọn xử lý hình học" Style="{StaticResource SectionHeaderStyle}" Margin="6,0,0,0"/>`  
   *Replacement*: `Style="{DynamicResource Subheading}" Margin="6,0,0,0"`

### 3.4. Usages of `DataGridHeaderStyle` and `DataGridRowStyle` (2 locations on DataGrid)
On DataGrid (Lines 235–246):
- **Line 244**: `ColumnHeaderStyle="{StaticResource DataGridHeaderStyle}"`
- **Line 245**: `RowStyle="{StaticResource DataGridRowStyle}"`  
*Replacement*: **Omit both explicit style attributes**. In `Theme.xaml`, Line 25 defines `<Style TargetType="DataGrid" BasedOn="{StaticResource MaterialDesignDataGrid}"/>`, which automatically governs column headers, rows, hovers, alternating lines, and selection states.

### 3.5. Secondary Standardization Opportunities (Buttons in Toolbar and Footer)
Consistent with `RebarPartitionView.xaml`, `ColumnRebarView.xaml`, and `MaterialBridge.xaml`:
- **Lines 221–230**: Toolbar buttons (`SelectAllPairsCommand`, `DeselectAllPairsCommand`, `ResetDefaultPriorityCommand`) currently use `Style="{StaticResource MaterialDesignFlatButton}"`.  
  *Recommended*: Standardize to `Style="{DynamicResource LinkButton}"` for hand cursor, dynamic accent foreground, and repo consistency.
- **Line 302**: Close button (`CloseCommand`) currently uses `Style="{StaticResource MaterialDesignOutlinedButton}"`.  
  *Recommended*: Standardize to `Style="{DynamicResource SecondaryButton}"` (which is based on `MaterialDesignOutlinedButton`).
- **Line 307**: Run button (`RunAutoJoinCommand`) currently uses `Style="{StaticResource MaterialDesignRaisedButton}" Background="{DynamicResource Brush.Accent}"`.  
  *Recommended*: Standardize to `Style="{DynamicResource PrimaryButton}"` (which is based on `MaterialDesignRaisedButton` with primary blue accent and hand cursor).

---

## 4. Replacement Mapping & Theme Token Parity

| Legacy / Deprecated Token | Standard Replacement | Source Definition | Visual / Behavioral Parity Analysis |
|---|---|---|---|
| `CardBorderStyle` | `{DynamicResource Card}` | `HPRebar/Resources/Themes/Controls.xaml` | `Card` defines `Background="{DynamicResource Brush.SurfaceElevated}"`, `BorderBrush="{DynamicResource Brush.Border}"`, `BorderThickness="1"`, `CornerRadius="6"`, `Padding="{DynamicResource Spacing.Medium}"`. Fully compatible with dynamic Dark/Light runtime swapping. |
| `BadgeBorderStyle` | `{DynamicResource Tag}` | `HPRebar/Resources/Themes/Controls.xaml` | `Tag` defines `Background="{DynamicResource Brush.Surface}"`, `BorderBrush="{DynamicResource Brush.Border}"`, `BorderThickness="1"`, `CornerRadius="3"`, `Padding="6,2"`. Provides clean outlined chip appearance, preserving colored status numbers (`#FF388E3C`, `#FF1976D2`, `#FFFB8C00`). Exactly mirrors `RebarPartitionView.xaml` lines 53–71. |
| `SectionHeaderStyle` | `{DynamicResource Subheading}` | `HPRebar/Resources/Themes/Typography.xaml` | `Subheading` defines `FontFamily="Segoe UI"`, `FontSize="16"`, `FontWeight="SemiBold"`, `Foreground="{DynamicResource Brush.Foreground.Primary}"`. Perfectly matches section title hierarchy. |
| `DataGridHeaderStyle` | *Omitted (Implicit)* | `HPRebar/Resources/Themes/Theme.xaml` & `MaterialDesign2.Defaults.xaml` | `MaterialDesignDataGrid` provides clean Material column header styling with sorting indicators and theme borders. |
| `DataGridRowStyle` | *Omitted (Implicit)* | `HPRebar/Resources/Themes/Theme.xaml` & `MaterialDesign2.Defaults.xaml` | `MaterialDesignDataGrid` provides accessible row hover (`SurfaceHover`), row selection (`Accent`), and text contrast. |
| `MaterialDesignFlatButton` | `{DynamicResource LinkButton}` | `HPRebar/Resources/Themes/MaterialBridge.xaml` | Hand cursor, `Brush.Accent` foreground, clean flat button padding. |
| `MaterialDesignOutlinedButton` | `{DynamicResource SecondaryButton}` | `HPRebar/Resources/Themes/MaterialBridge.xaml` | Outlined button with `MinWidth="96"`, `Brush.Border`, `Hand` cursor. |
| `MaterialDesignRaisedButton` | `{DynamicResource PrimaryButton}` | `HPRebar/Resources/Themes/MaterialBridge.xaml` | Raised button with `MinWidth="96"`, `Hand` cursor, primary color styling. |

---

## 5. Functional & MVVM Integrity Audit

A comprehensive check of all logic and data bindings confirms:

1. **Window Definition & Lifecyle**:
   - `x:Class="HPRebar.JoinElements.View.JoinElementView"` is unchanged.
   - `Title="HPRebar — Tự động Join Hình học (Auto Join Elements)"` is preserved.
   - Dynamic window resizing in code-behind `JoinElementView.xaml.cs` (`WorkArea` height/width clamping) is untouched.
   - `MaterialThemeBridge.Attach(this, RevitHostTheme.Instance, dark => new RibbonIcons(dark).JoinElements)` is already wired and untouched.
   - `viewModel.RequestClose += Close` and `KeyBinding Key="Escape" Command="{Binding CloseCommand}"` are preserved.

2. **MVVM Bindings Preserved**:
   - `TotalCandidatePairs`: `{Binding TotalCandidatePairs}`
   - `TotalJoined`: `{Binding TotalJoined}`
   - `TotalSwitched`: `{Binding TotalSwitched}`
   - `TotalSkipped`: `{Binding TotalSkipped}`
   - `ExecutionTimeText`: `{Binding ExecutionTimeText}`
   - `SelectedScope`: `{Binding SelectedScope}`
   - `SmartSkip`: `{Binding SmartSkip}`
   - `CleanJoinedNonIntersecting`: `{Binding CleanJoinedNonIntersecting}`
   - `SwitchOrderToPriority`: `{Binding SwitchOrderToPriority}`
   - `UnjoinFirst`: `{Binding UnjoinFirst}`
   - `CategoryPairs`: `{Binding CategoryPairs}`
   - `IsSelected`: `{Binding IsSelected, UpdateSourceTrigger=PropertyChanged}`
   - `DisplayPair`: `{Binding DisplayPair}`
   - `PrimaryCutsSecondary`: `{Binding PrimaryCutsSecondary, UpdateSourceTrigger=PropertyChanged}`
   - `CuttingOrderText`: `{Binding CuttingOrderText}`
   - `IsBusy`: `{Binding IsBusy}`
   - `StatusMessage`: `{Binding StatusMessage}`

3. **MVVM Commands Preserved**:
   - `CloseCommand`
   - `SelectAllPairsCommand`
   - `DeselectAllPairsCommand`
   - `ResetDefaultPriorityCommand`
   - `RunAutoJoinCommand`

4. **Resource Converters**:
   - `<BooleanToVisibilityConverter x:Key="BoolToVis"/>` is preserved in `Window.Resources`.
   - Used for `ProgressBar` visibility and `RunAutoJoinCommand` enablement (`ConverterParameter=Inverse`).

5. **Vietnamese Text & ToolTips**:
   - All Vietnamese UI strings, labels, and rich tooltip explanations are 100% preserved.

6. **Font Rule Compliance**:
   - Font family remains `{DynamicResource Font.Family.Default}` (`Segoe UI`).
   - Zero references to Roboto or `{md:MaterialDesignFont}`.

---

## 6. Actionable Implementation Plan & Recommended Diff

### Target File
`HPRebar/HPRebar/JoinElements/View/JoinElementView.xaml`

### Recommended Unified Diff
```diff
--- a/HPRebar/HPRebar/JoinElements/View/JoinElementView.xaml
+++ b/HPRebar/HPRebar/JoinElements/View/JoinElementView.xaml
@@ -27,47 +27,6 @@
             </ResourceDictionary.MergedDictionaries>
             <BooleanToVisibilityConverter x:Key="BoolToVis"/>
-
-            <Style x:Key="SectionHeaderStyle" TargetType="TextBlock">
-                <Setter Property="FontWeight" Value="SemiBold"/>
-                <Setter Property="FontSize" Value="{DynamicResource Font.Size.Body}"/>
-                <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.Primary}"/>
-                <Setter Property="Margin" Value="0,0,0,6"/>
-            </Style>
-
-            <Style x:Key="CardBorderStyle" TargetType="Border">
-                <Setter Property="Background" Value="{DynamicResource Brush.Surface}"/>
-                <Setter Property="BorderBrush" Value="{DynamicResource Brush.Border}"/>
-                <Setter Property="BorderThickness" Value="1"/>
-                <Setter Property="CornerRadius" Value="6"/>
-                <Setter Property="Padding" Value="14"/>
-                <Setter Property="Margin" Value="0,0,0,10"/>
-            </Style>
-
-            <Style x:Key="BadgeBorderStyle" TargetType="Border">
-                <Setter Property="Background" Value="{DynamicResource Brush.GridLine}"/>
-                <Setter Property="CornerRadius" Value="4"/>
-                <Setter Property="Padding" Value="8,4"/>
-                <Setter Property="Margin" Value="0,0,8,0"/>
-            </Style>
-
-            <Style x:Key="DataGridHeaderStyle" TargetType="DataGridColumnHeader">
-                <Setter Property="FontWeight" Value="SemiBold"/>
-                <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.Primary}"/>
-                <Setter Property="Background" Value="{DynamicResource Brush.Surface}"/>
-                <Setter Property="Padding" Value="8,6"/>
-                <Setter Property="BorderBrush" Value="{DynamicResource Brush.GridLine}"/>
-                <Setter Property="BorderThickness" Value="0,0,1,1"/>
-            </Style>
-
-            <Style x:Key="DataGridRowStyle" TargetType="DataGridRow">
-                <Setter Property="Background" Value="Transparent"/>
-                <Setter Property="Foreground" Value="{DynamicResource Brush.Foreground.Primary}"/>
-                <Style.Triggers>
-                    <Trigger Property="IsMouseOver" Value="True">
-                        <Setter Property="Background" Value="{DynamicResource Brush.GridLine}"/>
-                    </Trigger>
-                </Style.Triggers>
-            </Style>
         </ResourceDictionary>
     </Window.Resources>
 
@@ -79,7 +38,7 @@
         </Grid.RowDefinitions>
 
         <!-- 1. Header Information Bar -->
-        <Border Grid.Row="0" Style="{StaticResource CardBorderStyle}" Padding="12,10" Margin="0,0,0,12">
+        <Border Grid.Row="0" Style="{DynamicResource Card}" Padding="12,10" Margin="0,0,0,12">
             <Grid>
                 <Grid.ColumnDefinitions>
                     <ColumnDefinition Width="Auto"/>
@@ -92,31 +51,31 @@
                 </StackPanel>
 
                 <WrapPanel Grid.Column="1" HorizontalAlignment="Right" VerticalAlignment="Center">
-                    <Border Style="{StaticResource BadgeBorderStyle}">
+                    <Border Style="{DynamicResource Tag}" Margin="0,0,8,0">
                         <StackPanel Orientation="Horizontal">
                             <TextBlock Text="Cặp va chạm: " Foreground="{DynamicResource Brush.Foreground.Secondary}"/>
                             <TextBlock Text="{Binding TotalCandidatePairs}" FontWeight="Bold"/>
                         </StackPanel>
                     </Border>
-                    <Border Style="{StaticResource BadgeBorderStyle}">
+                    <Border Style="{DynamicResource Tag}" Margin="0,0,8,0">
                         <StackPanel Orientation="Horizontal">
                             <TextBlock Text="Đã Join: " Foreground="{DynamicResource Brush.Foreground.Secondary}"/>
                             <TextBlock Text="{Binding TotalJoined}" FontWeight="Bold" Foreground="#FF388E3C"/>
                         </StackPanel>
                     </Border>
-                    <Border Style="{StaticResource BadgeBorderStyle}">
+                    <Border Style="{DynamicResource Tag}" Margin="0,0,8,0">
                         <StackPanel Orientation="Horizontal">
                             <TextBlock Text="Đảo cắt: " Foreground="{DynamicResource Brush.Foreground.Secondary}"/>
                             <TextBlock Text="{Binding TotalSwitched}" FontWeight="Bold" Foreground="#FF1976D2"/>
                         </StackPanel>
                     </Border>
-                    <Border Style="{StaticResource BadgeBorderStyle}">
+                    <Border Style="{DynamicResource Tag}" Margin="0,0,8,0">
                         <StackPanel Orientation="Horizontal">
                             <TextBlock Text="Đã chuẩn: " Foreground="{DynamicResource Brush.Foreground.Secondary}"/>
                             <TextBlock Text="{Binding TotalSkipped}" FontWeight="Bold" Foreground="#FFFB8C00"/>
                         </StackPanel>
                     </Border>
-                    <Border Style="{StaticResource BadgeBorderStyle}">
+                    <Border Style="{DynamicResource Tag}">
                         <StackPanel Orientation="Horizontal">
                             <TextBlock Text="Thời gian: " Foreground="{DynamicResource Brush.Foreground.Secondary}"/>
                             <TextBlock Text="{Binding ExecutionTimeText}" FontWeight="Bold"/>
@@ -137,11 +96,11 @@
             <!-- Left Panel: Scope & Options -->
             <ScrollViewer Grid.Column="0" VerticalScrollBarVisibility="Auto" Margin="0,0,12,0">
                 <StackPanel>
                     <!-- Scope Selection -->
-                    <Border Style="{StaticResource CardBorderStyle}">
+                    <Border Style="{DynamicResource Card}" Margin="0,0,0,10">
                         <StackPanel>
                             <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
                                 <md:PackIcon Kind="FilterOutline" Width="18" Height="18" Foreground="{DynamicResource Brush.Accent}"/>
-                                <TextBlock Text="1. Phạm vi cấu kiện (Scope)" Style="{StaticResource SectionHeaderStyle}" Margin="6,0,0,0"/>
+                                <TextBlock Text="1. Phạm vi cấu kiện (Scope)" Style="{DynamicResource Subheading}" Margin="6,0,0,0"/>
                             </StackPanel>
 
                             <ComboBox SelectedValue="{Binding SelectedScope}"
@@ -155,11 +114,11 @@
                     </Border>
 
                     <!-- Execution Options -->
-                    <Border Style="{StaticResource CardBorderStyle}">
+                    <Border Style="{DynamicResource Card}" Margin="0,0,0,10">
                         <StackPanel>
                             <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
                                 <md:PackIcon Kind="CogOutline" Width="18" Height="18" Foreground="{DynamicResource Brush.Accent}"/>
-                                <TextBlock Text="2. Tùy chọn xử lý hình học" Style="{StaticResource SectionHeaderStyle}" Margin="6,0,0,0"/>
+                                <TextBlock Text="2. Tùy chọn xử lý hình học" Style="{DynamicResource Subheading}" Margin="6,0,0,0"/>
                             </StackPanel>
 
                             <CheckBox IsChecked="{Binding SmartSkip}"
@@ -186,7 +145,7 @@
                     </Border>
 
                     <!-- Feature Tip -->
-                    <Border Style="{StaticResource CardBorderStyle}" Background="#0A2196F3" BorderBrush="#332196F3">
+                    <Border Style="{DynamicResource Card}" Margin="0,0,0,10" Background="#0A2196F3" BorderBrush="#332196F3">
                         <StackPanel>
                             <StackPanel Orientation="Horizontal" Margin="0,0,0,4">
                                 <md:PackIcon Kind="InformationOutline" Width="16" Height="16" Foreground="#FF1976D2"/>
@@ -203,7 +162,7 @@
             </ScrollViewer>
 
             <!-- Right Panel: Priority Matrix DataGrid -->
-            <Border Grid.Column="1" Style="{StaticResource CardBorderStyle}" Padding="0">
+            <Border Grid.Column="1" Style="{DynamicResource Card}" Padding="0">
                 <Grid>
                     <Grid.RowDefinitions>
                         <RowDefinition Height="Auto"/>
@@ -219,13 +178,13 @@
                             <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
                                 <Button Command="{Binding SelectAllPairsCommand}"
-                                        Style="{StaticResource MaterialDesignFlatButton}"
+                                        Style="{DynamicResource LinkButton}"
                                         Content="Chọn tất cả" Height="28" Padding="8,0" Margin="0,0,4,0"/>
                                 <Button Command="{Binding DeselectAllPairsCommand}"
-                                        Style="{StaticResource MaterialDesignFlatButton}"
+                                        Style="{DynamicResource LinkButton}"
                                         Content="Bỏ chọn" Height="28" Padding="8,0" Margin="0,0,4,0"/>
                                 <Button Command="{Binding ResetDefaultPriorityCommand}"
-                                        Style="{StaticResource MaterialDesignFlatButton}"
+                                        Style="{DynamicResource LinkButton}"
                                         Content="Chuẩn BIM" Height="28" Padding="8,0"/>
                             </StackPanel>
                         </Grid>
@@ -240,9 +199,7 @@
                               CanUserDeleteRows="False"
                               GridLinesVisibility="Horizontal"
                               HorizontalGridLinesBrush="{DynamicResource Brush.GridLine}"
-                              Background="Transparent"
-                              RowHeaderWidth="0"
-                              ColumnHeaderStyle="{StaticResource DataGridHeaderStyle}"
-                              RowStyle="{StaticResource DataGridRowStyle}">
+                              Background="Transparent"
+                              RowHeaderWidth="0">
                         <DataGrid.Columns>
                             <DataGridCheckBoxColumn Header="Áp dụng"
@@ -277,7 +234,7 @@
         </Grid>
 
         <!-- 3. Footer Bar -->
-        <Border Grid.Row="2" Style="{StaticResource CardBorderStyle}" Padding="12,10" Margin="0,8,0,0">
+        <Border Grid.Row="2" Style="{DynamicResource Card}" Padding="12,10" Margin="0,8,0,0">
             <Grid>
                 <Grid.ColumnDefinitions>
                     <ColumnDefinition Width="*"/>
@@ -299,13 +256,12 @@
                 <!-- Actions -->
                 <StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center">
                     <Button Command="{Binding CloseCommand}"
-                            Style="{StaticResource MaterialDesignOutlinedButton}"
+                            Style="{DynamicResource SecondaryButton}"
                             Content="ĐÓNG"
                             Width="90" Height="36" Margin="0,0,8,0"/>
 
                     <Button Command="{Binding RunAutoJoinCommand}"
-                            Style="{StaticResource MaterialDesignRaisedButton}"
-                            Background="{DynamicResource Brush.Accent}"
+                            Style="{DynamicResource PrimaryButton}"
                             Width="170" Height="36"
                             IsEnabled="{Binding IsBusy, Converter={StaticResource BoolToVis}, ConverterParameter=Inverse}">
                         <StackPanel Orientation="Horizontal">
```

---

## 7. Verification Method

Once the remediation coder applies this change, independent verification will consist of:

1. **Static Analysis & Zero Deprecated Styles Check**:
   ```powershell
   # Must return 0 matches across the repository:
   rg "CardBorderStyle|SectionHeaderStyle|BadgeBorderStyle|DataGridHeaderStyle|DataGridRowStyle" "HPRebar"
   ```

2. **Core Theme Token Coverage Tests**:
   ```powershell
   dotnet test HPRebar/HPRebar.Core.Tests --filter "FullyQualifiedName~ThemeTokenCoverageTests"
   dotnet test HPRebar/HPRebar.Core.Tests --filter "FullyQualifiedName~MaterialDesignMigrationE2ETests"
   ```

3. **Multi-Version Compilation**:
   ```powershell
   dotnet build HPRebar/HPRebar/HPRebar.csproj -c Debug.R26 -p:DeployAddin=false
   dotnet build HPRebar/HPRebar/HPRebar.csproj -c Debug.R25 -p:DeployAddin=false
   dotnet build HPRebar/HPRebar/HPRebar.csproj -c Debug.R27 -p:DeployAddin=false
   ```
