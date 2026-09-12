## 2026-09-07T16:18:25Z
You are reviewer_m3_4_2, an independent WPF MVVM and UI theming reviewer.
Working directory: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_4_2\
Authoritative User Request: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\ORIGINAL_REQUEST.md (Refer to '## Follow-up — 2026-09-07T15:37:30Z')
Scope Document: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\orchestrator_2\SCOPE.md
Worker Handoff: F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m3_4\handoff.md

Your Mission:
Review the WPF MVVM UI implementation in `HPRebar/HPRebar/Foundation Rebar/View/` and `View Models/`:
1. Inspect ViewModels:
   - `FoundationRebarViewModel.cs`: `sealed partial class ... : ObservableObject` (`CommunityToolkit.Mvvm`), hosts child viewmodels, validation, Apply/Cancel commands.
   - `FoundationGeometryViewModel.cs`: Exposes observable geometry parameters.
   - `FoundationSettingViewModel.cs`: Exposes observable rebar parameters, covers, top mat toggle, hook types.
2. Inspect Views:
   - `FoundationRebarView.xaml`, `FoundationGeometryView.xaml`, `FoundationSettingView.xaml`.
   - 100% `{DynamicResource}` token theming via `Theme.xaml` (`Brush.*`, `Spacing.*`, `Font.*`). Zero hardcoded colors or fonts.
   - Code-behind is strictly `InitializeComponent(); DataContext = viewModel;` (zero business logic in code-behind).
3. Check runtime theme switcher (`ThemeSwitcher.cs`) synchronization for Revit Dark/Light modes.
4. Give explicit verdict: APPROVE or REQUEST_CHANGES.

Write your report to F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m3_4_2\handoff.md and message the parent orchestrator. Strictly read-only on source files.
