# BRIEFING — 2026-10-18T03:31:00Z

## Mission
Investigate HPKata.UI ViewModels and Views for Milestone M2 Feature 6 (Excel integration: Open, Read, Write, AutoSync), design commands, UI layout, tests, and verify constraints.

## 🔒 My Identity
- Archetype: explorer
- Roles: investigation, synthesis
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_2\
- Original parent: 3c323530-716e-4b3b-a14c-2a7fcee0c68a
- Milestone: M2 Feature 6 (UI Integration)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Strict UI Constraint: Retain System.Windows.Window + MaterialDesignThemes v5.2.1. DO NOT switch to MahApps.Metro. Retain all Theme.xaml StaticResource keys.
- Windows must instantiate without errors on STA thread under Dark and Light themes.
- Clean Code limits: max 300 lines per file (Rule C2), max 50 lines per method (Rule M3), 0 warnings.
- Output comprehensive handoff report to handoff.md.

## Current Parent
- Conversation ID: 3c323530-716e-4b3b-a14c-2a7fcee0c68a
- Updated: not yet

## Investigation State
- **Explored paths**:
  - `HPKata/HPKata.UI/ViewModels/BeamDetailingViewModel.cs`, `BeamSpanViewModel.cs`
  - `HPKata/HPKata.UI/Views/BeamDetailingWindow.xaml`, `BeamDetailingWindow.xaml.cs`
  - `HPKata/HPKata.UI/ViewModels/ColumnDetailingViewModel.cs`, `ColumnStoryRowViewModel.cs`
  - `HPKata/HPKata.UI/Views/ColumnDetailingWindow.xaml`, `ColumnDetailingWindow.xaml.cs`
  - `HPKata/HPKata.UI/ViewModels/WallDetailingViewModel.cs`, `WallOpeningViewModel.cs`
  - `HPKata/HPKata.UI/Views/WallDetailingWindow.xaml`, `WallDetailingWindow.xaml.cs`
  - `HPKata/HPKata.UI/Themes/Theme.xaml`, `MaterialBridge.xaml`, `ThemeDark.xaml`, `ThemeLight.xaml`, `Common/ThemeManager.cs`
  - `HPKata/HPKata.Core/Configuration/KataInstallEnvironment.cs`, `IKataInstallEnvironment.cs`
  - `HPKata/HPKata.UI.Tests/ViewModels/*`
  - `HPKata/HPKata.E2E.Tests/Tier1_FeatureCoverage/Tier1_F18_WpfWindowThemeInstantiationTests.cs`
- **Key findings**:
  - Baseline tests: 1019 tests passing (0 failed, 0 skipped).
  - Current line counts: BeamDetailingViewModel (163 lines), ColumnDetailingViewModel (186 lines), WallDetailingViewModel (172 lines).
  - Partial class pattern (`*.Excel.cs`) ensures each file stays <= 190 lines, well below 300-line Rule C2 limit.
  - Commands designed: `OpenExcelCommand`, `ReadExcelCommand`, `WriteExcelCommand`, `AutoSyncOnApply`.
  - UI toolbar designed using existing `Card.Panel`, `Button.Secondary`, `Text.Muted`, `Brush.Success`, `Brush.Text`.
  - Zero dependencies on MahApps.Metro; 100% MaterialDesignThemes v5.2.1 compliant.
- **Unexplored areas**: None for M2 Feature 6 UI scope.

## Key Decisions Made
- Use partial classes (`BeamDetailingViewModel.Excel.cs`, `ColumnDetailingViewModel.Excel.cs`, `WallDetailingViewModel.Excel.cs`) to cleanly isolate Excel sync from structural detailing logic, guaranteeing <= 190 lines per file.
- Provide constructor with optional `IKataExcelAdapter? = null` parameter so default constructors remain valid for STA tests (`Tier1_F18_WpfWindowThemeInstantiationTests`) and XAML previews.
- Use text-based Secondary Buttons with tooltips and clean colored status indicator (`Brush.Success` / `Brush.Warning`), avoiding any font-glyph lookup failures on headless test environments.

## Artifact Index
- DISPATCH.md — Task assignment and requirements
- BRIEFING.md — Persistent context
- progress.md — Liveness heartbeat
- handoff.md — Final handoff report
