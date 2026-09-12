# DISPATCH — worker_m4_it2

## 2026-09-07T09:53:35Z

## Mission
You are the Remediation Worker for Milestone M4 (WPF MVVM UI, Dynamic Theming, and Interactive Preview Canvases) for Continuous Beam Rebar.
Apply all 8 targeted fixes identified during Gate 1 review and stress testing, eliminate all compile errors and XAML token mismatches, ensure robust two-way data bindings and validation, and verify zero-allocation canvas rendering.

## Mandatory Reading
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Project Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Dead Ends Log: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\orchestrator_1\DEAD_ENDS.md`
4. Gate 1 Reviewer Reports:
   - `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m4_1\review_report.md`
   - `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m4_2\review_report.md`
5. Gate 1 Challenger Reports:
   - `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m4_1\challenge_report.md`
   - `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m4_2\challenge_report.md`

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Defect Inventory & Exact Fix Requirements

### 1. CS1061 in `BeamElevationPainter.cs` (lines 215, 216, 427, 428)
- Problem: Code references `stack.OverallStartX` and `stack.OverallEndX` on `BeamStack`. These exist on `stack.ContinuousStack.OverallStartX` and `stack.ContinuousStack.OverallEndX`.
- Fix: Access via `stack.ContinuousStack.OverallStartX` / `stack.ContinuousStack.OverallEndX` (or expose forwarding properties on `BeamStack` if helpful).

### 2. CS1061 in `BeamElevationPainter.cs` (line 395)
- Problem: Code references `inter.IntersectionX` on `SecondaryBeamIntersection`. The property in `HPRebar.Core/BeamRebar/Models/SecondaryBeamIntersection.cs` is `CenterX`.
- Fix: Change to `inter.CenterX`.

### 3. Missing/Undefined XAML Resource Tokens
- `HPRebar/HPRebar/Beam Rebar/View/BeamRebarView.xaml:87`:
  Replace `{DynamicResource Spacing.SmallRight}` with `{DynamicResource Spacing.SmallHorizontal}` (as defined in `HPRebar/HPRebar/Resources/Themes/Spacing.xaml`).
- `HPRebar/HPRebar/Beam Rebar/View/Tabs/GeometryTabView.xaml:27, 36, 45, 54`:
  Replace `{DynamicResource Font.Size.Subtitle}` with `{DynamicResource Font.Size.Subheading}` (as defined in `HPRebar/HPRebar/Resources/Themes/Typography.xaml`).

### 4. Two-Way Binding Breakdown on Support & Span Editors in `BeamRebarSession.cs` & `AdditionalBarsTabView.xaml`
- In `AdditionalBarsTabView.xaml:24, 116`: ComboBox binds `SelectedItem` to `Session.SelectedSupportEditor` and `Session.SelectedSpanEditor`. In `BeamRebarSession.cs`, these are get-only properties without setters.
- Fix: In `BeamRebarSession.cs`, add setters to `SelectedSupportEditor` and `SelectedSpanEditor` that locate the matching index and update `SelectedSupportIndex` / `SelectedSpanIndex` (and fire `OnPropertyChanged(nameof(SelectedSupportEditor))` / `OnPropertyChanged(nameof(SelectedSpanEditor))`), OR bind `SelectedIndex` of the ComboBox directly to `SelectedSupportIndex` and `SelectedSpanIndex`. Ensure dropdown selection properly switches the active editor.

### 5. Stirrup Set Limit Validation Asymmetry in `BeamRebarSession.cs`
- In `BeamRebarSession.Validate(out string error)`: Currently only validates `StirrupSpacingDense` for $> 1002$ bar count. If `StirrupSpacingSparse` has a typo (e.g. 5 mm or 0), it bypasses validation and can crash Revit API.
- Fix: In `BeamRebarSession.Validate`, validate both `StirrupSpacingDense` and `StirrupSpacingSparse`:
  - Must be $> 0$ (e.g. $\ge 25$ mm).
  - Calculated bar count per span for both dense and sparse zones must not exceed 1002 bars.

### 6. Cantilever Overhang Truncation & Off-Screen Coordinates ($X < 0$) in `BeamElevationCanvas`
- In `BeamElevationPainter.cs` and coordinate transform calculation: Ensure model X coordinates are bounded by `stack.ContinuousStack.Spans[0].StartX` to `stack.ContinuousStack.Spans[^1].EndX` (or the overall stack bounds) so that exterior cantilevers never map to negative X or clip off the left/right canvas margins.

### 7. Scaled Layer Offsets to Prevent Inversion on Long Multi-Span Beams
- In `BeamElevationPainter.cs`: Avoid hardcoded pixel offsets ($\pm 3.0$px, $\pm 5.0$px) that exceed the rendered beam height on long multi-span beams where the scale is small.
- Fix: Dynamically scale layer offsets proportional to the beam screen height: `double layerOffset = Math.Min(3.0, beamHeightPx * 0.15);` and similarly for layer 2.

### 8. Allocation-Free Frozen Pen Caching during `OnRender`
- In `BeamElevationCanvas.cs` and `BeamSectionCanvas.cs`: Do NOT call `CanvasPalette.From(this)` inside `OnRender`. Instead, cache a `CanvasPalette` field on the canvas instance (invalidated/recreated only when theme resources change or on initialization).
- In `BeamElevationPainter.cs` and `BeamSectionPainter.cs`: Ensure all pens, brushes, and dash styles are frozen (`pen.Freeze()`) and reused. Never instantiate new `Pen(...)` or `DashStyle(...)` inside drawing loops.

## Verification Commands
After implementing all fixes, run:
```bash
dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false
dotnet build HPRebar/HPRebar.slnx -c Debug.R25 -p:DeployAddin=false
dotnet test HPRebar/HPRebar.Core.Tests
```
Ensure zero warnings treated as errors, zero build errors, and 100% test pass.

## Output
Write your comprehensive handoff report to:
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m4_it2\handoff.md`
Notify orchestrator via `send_message` when done.
