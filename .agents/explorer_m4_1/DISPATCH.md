# Task Assignment: explorer_m4_1

## Role
Milestone M4 UI & Preview Canvas Explorer (`teamwork_preview_explorer`)

## Working Directory
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m4_1`

## Reference Documents
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Project Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Legacy Source UI (read-only): `F:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\RebarAddin-master\RebarAddin-master\R02_BeamsRebar\View\` and `ViewModel\`
4. HPRebar Column Rebar Reference (golden standard): `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Column Rebar\View\` and `View Models\`
5. Target Codebase: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Beam Rebar\`
6. Core Models & Calculators: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar.Core\BeamRebar\`

## Mission
Investigate, design, and architect the complete Milestone M4 implementation (WPF MVVM UI, Dynamic Theming, and Interactive Preview Canvases) for Continuous Beam Rebar:

1. **WPF MVVM ViewModel (`BeamRebarViewModel.cs`)**:
   - Uses `CommunityToolkit.Mvvm.ComponentModel.ObservableObject`.
   - Properties decorated with `[ObservableProperty]`.
   - Commands decorated with `[RelayCommand]`.
   - Tabs:
     - Tab 1: Spans & Geometry (displays detected continuous spans, width, height, clear lengths, and support nodes).
     - Tab 2: Main Reinforcement (Top/Bottom main bar counts, diameter selection, hook angles 90°/180°, cover).
     - Tab 3: Additional Reinforcement (Support top negative bars Layer 1/2 lengths L/3 & L/4, Midspan bottom positive bars Layer 1/2 lengths, diameters).
     - Tab 4: Stirrups & Ties (Zone 1 / Zone 2 / Zone 3 spacings, diameters, anti-buckling cross ties, secondary framing hanging stirrup toggles).
     - Tab 5: Views & Annotations (Detail elevation scale, cross-section count per span, dimension styles, schedule table placement).
   - Dynamic binding to `RebarTypeCatalog` for available bar diameters.
   - Dynamic UI localization switching via `LocalizationService` (English & Vietnamese).
   - Execution progress reporting through `IProgress<BeamProgressReport>`.

2. **WPF View (`BeamRebarView.xaml` & `BeamRebarView.xaml.cs`)**:
   - Strict adherence to `revit-xaml-styles` and `revit-wpf-mvvm` skills.
   - All colors, borders, and backgrounds use `{DynamicResource Brush.X}` to ensure seamless runtime switching between Revit Light and Dark themes.
   - Code-behind is strictly `InitializeComponent()` + `DataContext = vm` (no business logic).
   - Dialog window sizing, responsive tab layouts, and clean button controls.

3. **Interactive Preview Canvases**:
   - `BeamElevationCanvas.cs`: Custom `FrameworkElement` or `Control` with direct `OnRender(DrawingContext dc)`. Renders continuous beam spans, support columns/walls, top/bottom continuous bars, support negative bars, midspan positive bars, and stirrup density zones with instant visual feedback upon parameter changes.
   - `BeamSectionCanvas.cs`: Custom `FrameworkElement` with `OnRender(DrawingContext dc)`. Renders beam concrete cross-section cut, main corner bars, layer 2 additional bars, side skin bars, and closed stirrups with cross ties.
   - Coordinate transformation from millimeters to canvas display coordinates.

## Deliverables
- Detailed Architecture & Design Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m4_1\ui_canvas_plan.md`
- Handoff Report: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m4_1\handoff.md`
- Send completion message to orchestrator.
