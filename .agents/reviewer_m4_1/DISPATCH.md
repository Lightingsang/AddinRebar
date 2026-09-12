# Task Assignment: reviewer_m4_1

## Role
Milestone M4 Reviewer 1 (MVVM Architecture, Theming, and XAML Review)

## Working Directory
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m4_1`

## Reference Documents
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Project Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Architecture Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m4_1\ui_canvas_plan.md`
4. Worker Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m4\handoff.md`
5. Target Codebase: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Beam Rebar\`

## Task
Conduct an independent code and architecture review of Milestone M4:
- Verify `BeamRebarViewModel.cs`, `BeamRebarSession.cs`, and the 5 Tab ViewModels in `View Models/Tabs/` for strict `CommunityToolkit.Mvvm` patterns (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`).
- Verify `BeamRebarView.xaml` and 5 Tab UserControls in `View/Tabs/`: confirm 100% `{DynamicResource Brush.X}` and `{DynamicResource Spacing.X}` usage (zero hardcoded colors or StaticResources).
- Verify code-behind `BeamRebarView.xaml.cs` is strictly `InitializeComponent()`, `DataContext = vm`, `ThemeSwitcher.ApplyFromRevit(this)`, and close handler.
- Verify file-scoped namespaces throughout all new C# files (`namespace HPRebar.BeamRebar.ViewModels;`, etc.).

## Deliverables
- Review report: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m4_1\review_report.md`
- Handoff report: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m4_1\handoff.md`
- Notify orchestrator with binary verdict: `APPROVE` or `REQUEST_CHANGES`.

## 2026-09-07T09:36:56Z
You are reviewer_m4_1.
Your working directory is: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m4_1
Read your task assignment at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m4_1\DISPATCH.md
Read the authoritative user request at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md
Read the worker handoff at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m4\handoff.md
Read the target codebase at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Beam Rebar\

Conduct an independent code and architecture review of Milestone M4:
- Verify BeamRebarViewModel.cs, BeamRebarSession.cs, and the 5 Tab ViewModels in View Models/Tabs/ for CommunityToolkit.Mvvm patterns (ObservableObject, [ObservableProperty], [RelayCommand]).
- Verify BeamRebarView.xaml and 5 Tab UserControls in View/Tabs/: confirm 100% {DynamicResource Brush.X} and {DynamicResource Spacing.X} usage (zero hardcoded colors or StaticResources).
- Verify code-behind BeamRebarView.xaml.cs is strictly InitializeComponent(), DataContext = vm, ThemeSwitcher.ApplyFromRevit(this), and close handler.
- Verify file-scoped namespaces throughout all new C# files.

Write your review report to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m4_1\review_report.md
Write your handoff to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m4_1\handoff.md
Notify orchestrator via send_message with your verdict (APPROVE or REQUEST_CHANGES).
