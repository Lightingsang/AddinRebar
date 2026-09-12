# BRIEFING — 2026-09-07T16:17:30Z

## Mission
Implement the complete Revit Feature Layer in `HPRebar/HPRebar/Foundation Rebar/` covering Milestones M3 and M4.

## 🔒 My Identity
- Archetype: implementer
- Roles: implementer, qa, specialist
- Working directory: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m3_4
- Original parent: 2ff0ff8b-87f5-4c14-be1c-b37017b7f55d
- Milestone: M3, M4

## 🔒 Key Constraints
- Exclusively own all files in HPRebar/HPRebar/Foundation Rebar/
- Do NOT modify files in Beam Rebar/, Column Rebar/, or outside this directory
- Maintain 100% genuine implementation (no dummy/facade/hardcoding)
- Both Debug.R26 and Debug.R25 must build with 0 errors
- All 334 tests in HPRebar.Core.Tests must pass

## Current Parent
- Conversation ID: 2ff0ff8b-87f5-4c14-be1c-b37017b7f55d
- Updated: 2026-09-07T16:17:30Z

## Task Summary
- **What to build**: Revit Feature Layer in HPRebar/HPRebar/Foundation Rebar/ (Command, Filter, FaceReader, Validator, CreationService, Orchestrator, Session, ViewModels, Views)
- **Success criteria**: Clean compilation on R25 & R26, all core tests passing, MVVM architecture, full feature integration
- **Interface contracts**: HPRebar.Core.FoundationRebar.Models, SCOPE.md
- **Code layout**: AGENTS.md Feature Folder Convention

## Key Decisions Made
- Followed strict Feature Folder Convention: Root services in `Foundation Rebar/`, models in `Models/`, viewmodels in `View Models/`, views in `View/`.
- Declared explicit PascalCase namespaces without underscores: `HPRebar.FoundationRebar`, `HPRebar.FoundationRebar.Models`, `HPRebar.FoundationRebar.ViewModels`, `HPRebar.FoundationRebar.Views`.
- Used `// Multi-version: ElementId` with `#if REVIT2024_OR_GREATER` (`.Value`) `#else` (`.IntegerValue`) in SelectionFilter and CreationService.
- Wrapped rebar creation in atomic `TransactionGroup("Foundation Rebar")` with rollback on cancel/error and assimilate on confirm.
- Suppressed benign Revit layout warnings via `RebarFailureHandling` IFailuresPreprocessor.
- Created fully responsive, themed WPF MVVM user interface with DynamicResource token binding matching Revit Light/Dark mode.

## Artifact Index
- `HPRebar/HPRebar/Foundation Rebar/FoundationRebarCommand.cs`
- `HPRebar/HPRebar/Foundation Rebar/FoundationSelectionFilter.cs`
- `HPRebar/HPRebar/Foundation Rebar/FoundationSolidFaceReader.cs`
- `HPRebar/HPRebar/Foundation Rebar/FoundationRebarValidator.cs`
- `HPRebar/HPRebar/Foundation Rebar/FoundationRebarCreationService.cs`
- `HPRebar/HPRebar/Foundation Rebar/FoundationRebarOrchestrator.cs`
- `HPRebar/HPRebar/Foundation Rebar/RebarFailureHandling.cs`
- `HPRebar/HPRebar/Foundation Rebar/RevitUnits.cs`
- `HPRebar/HPRebar/Foundation Rebar/RevitDialogs.cs`
- `HPRebar/HPRebar/Foundation Rebar/ThemeSwitcher.cs`
- `HPRebar/HPRebar/Foundation Rebar/Models/FoundationSession.cs`
- `HPRebar/HPRebar/Foundation Rebar/View Models/FoundationGeometryViewModel.cs`
- `HPRebar/HPRebar/Foundation Rebar/View Models/FoundationSettingViewModel.cs`
- `HPRebar/HPRebar/Foundation Rebar/View Models/FoundationRebarViewModel.cs`
- `HPRebar/HPRebar/Foundation Rebar/View/FoundationGeometryView.xaml` & `.xaml.cs`
- `HPRebar/HPRebar/Foundation Rebar/View/FoundationSettingView.xaml` & `.xaml.cs`
- `HPRebar/HPRebar/Foundation Rebar/View/FoundationRebarView.xaml` & `.xaml.cs`

## Change Tracker
- **Files modified**: None (all files are newly created under `HPRebar/HPRebar/Foundation Rebar/`)
- **Build status**: Ready for verification
- **Pending issues**: None

## Quality Status
- **Build/test result**: Ready for verification
- **Lint status**: 0 violations
- **Tests added/modified**: Covered by HPRebar.Core.Tests

## Loaded Skills
- Source: revit-addin (f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\skills\revit-addin\SKILL.md)
  Local copy: N/A
  Core methodology: Nice3point.Revit templates, multi-version R23-R27, DI container, ribbon setup
- Source: revit-wpf-mvvm (f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\skills\revit-wpf-mvvm\SKILL.md)
  Local copy: N/A
  Core methodology: CommunityToolkit.Mvvm, ObservableObject, [ObservableProperty], [RelayCommand], DataContext = vm
- Source: revit-xaml-styles (f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\skills\revit-xaml-styles\SKILL.md)
  Local copy: N/A
  Core methodology: Theme.xaml, DynamicResource Brush.* / Spacing.*, Revit Dark/Light sync
