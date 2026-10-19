# Project: HPRebar & HPRebar.McpBridge MaterialDesign Migration

## Architecture
- **In-process Revit Add-In (`HPRebar.dll`)**: Multi-version R23–R27, .NET 8 (R25/R26), .NET 10 (R27), .NET Framework 4.8 (R23/R24). Built-in ILRepack via Nice3point.Revit.Sdk.
- **In-process Named Pipe Bridge (`HPRebar.McpBridge.dll`)**: Revit 2025/2026 (.NET 8.0-windows7.0). Custom target `RepackMaterialDesign` merges MaterialDesign toolkit assemblies while preserving Roslyn compiler independence.
- **Theming Subsystem (`HPRebar/HPRebar/Resources/Themes/`)**:
  - `MaterialBridge.xaml`: Bridges MaterialDesignInXamlToolkit v5.3.2, merges `MaterialDesign2.Defaults.xaml`, enforces Segoe UI (`MaterialDesignFont`), defines primary `#0696D7` / secondary `#E0641E` colors, and provides standardized control styles.
  - `Theme.xaml`: Master dictionary merging child dictionaries and providing implicit styles for standard controls (`TextBox`, `ComboBox`, `CheckBox`, `RadioButton`, `Button`, `DataGrid`, `TabControl`, `Expander`).
  - `ThemeDark.xaml` & `ThemeLight.xaml`: Palette tokens (`Brush.Background`, `Brush.Surface`, `Brush.Border`, etc.) and technical rebar brushes (`Brush.Canvas.*`).
  - `MaterialThemeBridge.cs`: Dynamic runtime dark/light theme switching via window-level `MergedDictionaries` overlay swap (no `Application.Current`, no `PaletteHelper`).
  - `HPRebar.McpBridge`: Links all theme XAML and C# files directly from `HPRebar/Resources/Themes/`, with local `Theme.xaml` remapping component pack URIs.

## Code Layout
```
HPRebar/
├── HPRebar/
│   ├── Resources/Themes/
│   │   ├── MaterialBridge.xaml       # MaterialDesign 5.3.2 defaults, font override, rebased keys
│   │   ├── Theme.xaml                # Master dictionary + implicit control styles
│   │   ├── ThemeDark.xaml            # Dark palette tokens + Brush.Canvas.*
│   │   ├── ThemeLight.xaml           # Light palette tokens + Brush.Canvas.*
│   │   ├── Controls.xaml             # Card, Badge, Tag, Separator
│   │   ├── Typography.xaml           # TextBlock typography hierarchy
│   │   ├── Spacing.xaml              # Grid spacing tokens
│   │   ├── ThemeIcons.xaml           # Vector tab icon templates
│   │   ├── MaterialThemeBridge.cs    # Runtime window overlay swap
│   │   └── RevitHostTheme.cs         # Revit Application.ThemeChanged event hook
│   ├── BeamRebar/View/               # BeamRebarView, Beam3DViewport, 5 Tabs
│   ├── ColumnRebar/View/             # ColumnRebarView, 8 Tabs
│   ├── FoundationRebar/View/         # FoundationRebarView, Geometry & Setting views
│   ├── KataExport/View/              # KataExportView, KataSettingsView
│   ├── KataRebar/View/               # KataRebarView
│   └── RebarPartition/View/          # RebarPartitionView
├── HPRebar.McpBridge/
│   ├── Resources/Themes/Theme.xaml   # Pack URI proxy to linked theme files
│   ├── View/McpBridgeStatusView.xaml # Bridge status window
│   └── HPRebar.McpBridge.csproj      # RepackMaterialDesign custom ILRepack target
├── HPRebar.Core.Tests/
│   └── Themes/
│       ├── ThemeTokenCoverageTests.cs          # Token parity & pack URI tests
│       └── MaterialDesignMigrationE2ETests.cs  # Acceptance test suite (Tiers 1-4)
└── tools/theme-gallery/              # Off-Revit visual inspection tool
```

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | MaterialBridge & Master Theme Expansion | Add `StandardComboBox` and standard implicit styles (`TextBox`, `ComboBox`, `CheckBox`, `RadioButton`, `Button`, `DataGrid`, `TabControl`, `Expander`) to `Theme.xaml` | M1 | DONE |
| 2 | Runtime Theme Swapping & Font Enforcement | Ensure `MaterialThemeBridge.cs` swaps BaseTheme and surfaces cleanly without `PaletteHelper`, maintaining Segoe UI font | M1 | DONE |
| 3 | ColumnRebar Views Standardization | Standardize `ColumnRebarView.xaml` and 8 tab views to toolkit styles while preserving right-aligned number inputs and canvas brushes | M2 | DONE |
| 4 | BeamRebar Views Standardization | Standardize `BeamRebarView.xaml`, `Beam3DViewport.xaml`, and 5 tab views; preserve 2D/3D Helix materials and `SegmentedRadioButton` | M2 | DONE |
| 5 | FoundationRebar Views Standardization | Standardize `FoundationRebarView.xaml` and geometry/setting tab views | M3 | DONE |
| 6 | Kata & Partition Views Cleanup | Clean redundant styles (`CardBorderStyle`, `DataGridHeaderStyle`, etc.) in `KataExportView`, `KataRebarView`, `RebarPartitionView`; preserve `ParamPickerComboBoxStyle` | M3 | DONE |
| 7 | HPRebar.McpBridge UI & ILRepack Isolation | Migrate `McpBridgeStatusView.xaml` to standard toolkit styles and verify `RepackMaterialDesign` ILRepack isolation | M4 | DONE |
| 8 | ThemeGallery Tool Upgrade | Fix `KataSupport` constructor parameter count, replace `CopyFromScreen` with `RenderTargetBitmap`, add McpBridge window | M5 | DONE |
| 9 | ThemeTokenCoverageTests Expansion | Update tests to assert new rebased keys and scan both `HPRebar` and `HPRebar.McpBridge` XAML files | M5 | DONE |
| 10 | Cross-Configuration Build & Test Verification | Build `Debug.R25`, `Debug.R26`, `Debug.R27` with `-p:DeployAddin=false` and run all 1,692 core tests | M5 | DONE |
| 11 | Victory Audit Remediation | Resolve CS7036 ElementId comparison on R25 in `JoinElementService.cs` and eradicate residual custom styles in `JoinElementView.xaml` | M6 | DONE |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Theme Core Architecture & Master Dictionaries | `MaterialBridge.xaml`, `Theme.xaml`, `Controls.xaml`, `MaterialThemeBridge.cs` | none | DONE |
| M2 | ColumnRebar & BeamRebar Module Views Migration | `ColumnRebar/View/` (9 files) and `BeamRebar/View/` (7 files) | M1 | DONE |
| M3 | FoundationRebar, Kata & Partition Views Migration | `FoundationRebar/View/` (3 files), `KataExport/View/` (2 files), `KataRebar/View/` (1 file), `RebarPartition/View/` (1 file) | M1 | DONE |
| M4 | HPRebar.McpBridge UI & Packaging Isolation | `McpBridgeStatusView.xaml`, `HPRebar.McpBridge.csproj` packaging targets | M1 | DONE |
| M5 | Theme Gallery, Token Coverage Tests & Cross-Config Verification | `ThemeTokenCoverageTests.cs`, `ThemeGallery/`, and full multi-version verification | M2, M3, M4 | DONE |
| M6 | Victory Audit Remediation | `JoinElementService.cs` (multi-version fix) & `JoinElementView.xaml` (style standardization) | M5 | DONE |

## Final Audit Sign-Off
- **Remediation Gate Verdict**: **PASS** (Reviewer 1 APPROVE, Reviewer 2 APPROVE, Challenger 1 APPROVE, Challenger 2 APPROVE, Auditor CLEAN)
- **Forensic Auditor Verdict**: **CLEAN** (Zero Integrity Violations)
- **Acceptance Tests**: 10/10 PASS (`MaterialDesignMigrationE2ETests.cs`)
- **Theme Tests**: 13/13 PASS (`ThemeTokenCoverageTests.cs` + E2E)
- **Core Tests**: 1,692/1,692 PASS (Zero Regressions)
- **Mcp Server Tests**: 118/118 PASS
- **Multi-Version Builds**: `Debug.R25`, `Debug.R26`, `Debug.R27` (0 Errors), `McpBridge Debug.R25`, `Debug.R26` (0 Errors, ILRepack clean)
- **Deprecated Style Sweep**: 0 occurrences of deprecated custom styles across the entire codebase
- **Clean Code Compliance**: 100% compliant with Rules C2, M3, Q-B1, Q-B2.
