## 2026-09-07T09:21:42Z
# Task Assignment: worker_m4

## Role
Milestone M4 Implementation Worker (`teamwork_preview_worker`)

## Working Directory
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m4`

## Reference Documents
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Project Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Milestone M4 Architecture & Design Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m4_1\ui_canvas_plan.md`
4. Golden Standard Reference: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Column Rebar\View\` and `View Models\`
5. Dynamic Theme Palettes: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Resources\Themes\`
6. Core Canvas Math: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar.Core\BeamRebar\Calculators\BeamCanvasTransformCalculator.cs`

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Objective
Implement the complete presentation tier for Milestone M4 (WPF MVVM UI, Dynamic Theming, and Interactive Preview Canvases) in `HPRebar/HPRebar/Beam Rebar/`:

### 1. View Models (`HPRebar/HPRebar/Beam Rebar/View Models/`)
- `BeamRebarSession.cs`: Holds active `BeamStack` and `BeamRebarSpec`, properties for selected span index, parameter mutation methods, and validation.
- `Tabs/BeamRebarTabViewModel.cs`: Abstract base class with `ObservableObject` and localized `Title`.
- `Tabs/GeometryTabViewModel.cs`: Span overview, clear lengths, support widths, dimensions.
- `Tabs/MainBarsTabViewModel.cs`: Top & bottom bar counts, diameters, covers, hook angles (90°/180°).
- `Tabs/AdditionalBarsTabViewModel.cs`: Top negative support bars (Layer 1/2, L/3, L/4) and bottom positive midspan bars.
- `Tabs/StirrupsTabViewModel.cs`: Zone 1/2/3 spacings, diameters, anti-buckling cross ties, secondary hanging stirrup toggles.
- `Tabs/ViewsTabViewModel.cs`: Detail view scale, section cuts per span, dimensions, schedule table options.
- `BeamRebarViewModel.cs`: Multi-tab orchestrator with `[ObservableProperty]`, `[RelayCommand] Run`, `[RelayCommand] ToggleLanguage`, `IProgress<BeamProgressReport>` reporting, and `CloseRequested`.

### 2. Custom Drawing Canvases (`HPRebar/HPRebar/Beam Rebar/View/Controls/`)
- `CanvasPalette.cs`: Resolves and freezes Pens and Brushes from `{DynamicResource}` color tokens (`Color.Canvas.*`).
- `BeamElevationCanvas.cs`: Custom `FrameworkElement` with direct `OnRender(DrawingContext dc)` using `BeamCanvasTransformCalculator` from `HPRebar.Core`. Draws continuous concrete outlines, support columns, longitudinal bars, and stirrups with a 50ms keystroke debounce timer.
- `BeamSectionCanvas.cs`: Custom `FrameworkElement` with `OnRender(DrawingContext dc)` drawing transverse cross-section cut, stirrups, corner bars, layer 2 bars, and cross ties.

### 3. WPF View & Code-Behind (`HPRebar/HPRebar/Beam Rebar/View/`)
- `BeamRebarView.xaml`: 3-row dialog layout with Header, tabbed navigation + active tab + live canvas preview, and responsive Footer. Strictly all styling and colors bound via `{DynamicResource Brush.X}` and `{DynamicResource Spacing.X}` for 100% theme switching safety.
- `BeamRebarView.xaml.cs`: Code-behind containing strictly `InitializeComponent()`, `DataContext = vm`, `ThemeSwitcher.ApplyFromRevit(this)`, and `CloseRequested` handler.

### 4. Verification
- Verify that `HPRebar.Core` continues to have ZERO references to `Autodesk.Revit.*`.
- Verify compilation across configurations:
  `dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false`
  `dotnet build HPRebar/HPRebar.slnx -c Debug.R25 -p:DeployAddin=false`
- Verify tests pass:
  `dotnet test HPRebar/HPRebar.Core.Tests`

## Deliverables
- Handoff report: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m4\handoff.md`
- Notify orchestrator upon completion.
