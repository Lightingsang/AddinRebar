# Technical Review Report: M4 Custom Preview Canvases, Drawing Primitives, and Core Decoupling

## Review Summary

**Verdict**: REQUEST_CHANGES

The implementation of the custom preview canvases (`BeamElevationCanvas`, `BeamSectionCanvas`), drawing primitives (`BeamDrawPrimitives`), and the frozen theme palette (`CanvasPalette`) demonstrates sound architecture, robust WPF performance optimizations (50ms debounce, frozen pens/brushes, DPI awareness), and strict architectural decoupling (zero `Autodesk.Revit.*` references in `HPRebar.Core`).

However, **critical compilation errors** exist in `BeamElevationPainter.cs` where properties are referenced that do not exist on the underlying models (`stack.OverallStartX`, `stack.OverallEndX`, and `inter.IntersectionX`). These prevent the add-in project from compiling. Once these model reference mismatches are resolved, the canvas rendering pipeline meets all architectural requirements.

---

## Findings

### [Critical] Finding 1: Unresolved Properties `stack.OverallStartX` and `stack.OverallEndX` on `BeamStack`

- **What**: C# compilation error CS1061: `'BeamStack' does not contain a definition for 'OverallStartX'` and `'OverallEndX'`.
- **Where**: `HPRebar/HPRebar/Beam Rebar/View/Controls/BeamElevationPainter.cs`:
  - Line 215: `double overallStartX = stack.OverallStartX;`
  - Line 216: `double overallEndX = stack.OverallEndX;`
  - Line 427: `double sTotalStart = _transform.ToScreenX(stack.OverallStartX);`
  - Line 428: `double sTotalEnd = _transform.ToScreenX(stack.OverallEndX);`
- **Why**: The variable `stack` is of type `HPRebar.BeamRebar.Models.BeamStack`. In `BeamStack.cs`, the pure domain representation is encapsulated as `public BeamContinuousStack ContinuousStack { get; init; }`. `OverallStartX` and `OverallEndX` are defined on `BeamContinuousStack`, but `BeamStack` does not expose forwarding properties for them.
- **Suggestion**: Either:
  1. Update `BeamElevationPainter.cs` to access `stack.ContinuousStack.OverallStartX` and `stack.ContinuousStack.OverallEndX` (consistent with line 264 and line 431 which already use `stack.ContinuousStack.TotalLength`); OR
  2. Add forwarding properties to `BeamStack.cs`:
     ```csharp
     public double OverallStartX => ContinuousStack.OverallStartX;
     public double OverallEndX => ContinuousStack.OverallEndX;
     public double TotalLength => ContinuousStack.TotalLength;
     ```

### [Critical] Finding 2: Unresolved Property `inter.IntersectionX` on `SecondaryBeamIntersection`

- **What**: C# compilation error CS1061: `'SecondaryBeamIntersection' does not contain a definition for 'IntersectionX'`.
- **Where**: `HPRebar/HPRebar/Beam Rebar/View/Controls/BeamElevationPainter.cs`:
  - Line 395: `double sX = _transform.ToScreenX(inter.IntersectionX);`
- **Why**: `SecondaryBeamIntersection` in `HPRebar.Core.BeamRebar.Models.SecondaryBeamIntersection.cs` defines the longitudinal coordinate property as `CenterX` (line 40), not `IntersectionX`.
- **Suggestion**:
  Update `BeamElevationPainter.cs` line 395 from `inter.IntersectionX` to `inter.CenterX`.

---

## Verified Claims

1. **Zero References to `Autodesk.Revit.*` in `HPRebar.Core`**
   - *Claim*: `HPRebar.Core` has zero dependencies on the Revit API.
   - *Verification*: Full ripgrep across `HPRebar/HPRebar.Core/` for `Autodesk.Revit` and `Autodesk` returned 0 matches. Inspected `HPRebar.Core.csproj`: targets `netstandard2.0` with only `Polyfill` NuGet package.
   - *Status*: **PASS**.

2. **Strict Coordinate Scaling via `BeamCanvasTransformCalculator`**
   - *Claim*: Coordinate scaling strictly delegates to `BeamCanvasTransformCalculator` from `HPRebar.Core`.
   - *Verification*:
     - `BeamElevationCanvas.cs` line 82 calls `BeamCanvasTransformCalculator.ComputeElevationTransform(session.Stack.ContinuousStack, width, height, 40.0)`.
     - `BeamSectionCanvas.cs` line 93 calls `BeamCanvasTransformCalculator.ComputeSectionTransform(span.Width, span.Height, width, height, 32.0)`.
     - `BeamElevationPainter.cs` and `BeamSectionPainter.cs` strictly use `_transform.ToScreenX`, `_transform.ToScreenY`, and `_transform.Scale`.
     - Tested comprehensively via 14 unit tests in `BeamCanvasTransformCalculatorTests.cs`.
   - *Status*: **PASS**.

3. **50ms Keystroke Debounce Logic via `DispatcherTimer`**
   - *Claim*: Canvas invalidation is throttled using a 50ms `DispatcherTimer` to prevent UI stutter during rapid parameter typing.
   - *Verification*:
     - `BeamElevationCanvas.cs` lines 24–40 and 116–120 initialize `DispatcherTimer(DispatcherPriority.Background)` with `Interval = TimeSpan.FromMilliseconds(50)`. On `OnSourceChanged`, `_redraw.Stop(); _redraw.Start();` coalesces rapid changes. On tick, `_redraw.Stop(); InvalidateMeasure(); InvalidateVisual();` fires.
     - `BeamSectionCanvas.cs` lines 24–44 and 127–131 implement the identical pattern.
     - Both canvases clean up on `Unloaded` via `_redraw.Stop(); Detach(Session);`.
   - *Status*: **PASS**.

4. **Frozen Pens and Brushes (`pen.Freeze()`) to Prevent GC Allocations**
   - *Claim*: Pens and brushes are frozen to eliminate GC pressure per render frame.
   - *Verification*:
     - `CanvasPalette.cs` lines 77, 95, 99, 119, 128, 134 call `.Freeze()` on all instantiated `Pen` and `Brush` instances (`outline`, `mainBar`, `selected`, `stirrup`, `tag`, `supportFill`, `highlight`, `dashedPen`).
     - Dynamic theme resolution checks `if (brush.CanFreeze) brush.Freeze();`.
   - *Status*: **PASS**.

5. **DPI Awareness and Per-Monitor Rendering**
   - *Claim*: Rendering correctly scales with display DPI.
   - *Verification*: `BeamElevationCanvas.cs` line 76 and `BeamSectionCanvas.cs` line 87 assign `BeamDrawPrimitives.PixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;` which is passed into `FormattedText` creation in `BeamDrawPrimitives.cs` line 102.
   - *Status*: **PASS**.

---

## Adversarial Review & Stress-Testing

### Challenge 1: Infinite Loop / Divide-by-Zero in Stirrup Advance
- **Hypothesis**: Could an extremely dense stirrup spacing or very high canvas scale cause `xCur += advanceMm` in `PaintStirrups` to produce an infinite loop or hang the UI thread?
- **Inspection**: `BeamElevationPainter.cs` line 201: `double advanceMm = Math.Max(spacing, 12.0 / _transform.Scale);`. Since `spacing = s1 > 0` (validated in `BeamRebarSession.Validate` to be $> 0$) and `12.0 / _transform.Scale > 0`, `advanceMm` is strictly positive and bounded away from zero.
- **Stress Result**: **PASS**.

### Challenge 2: Viewport Boundary Violation on Degenerate Dimensions
- **Hypothesis**: Could zero or negative canvas sizes passed from layout cause `ComputeElevationTransform` or `ComputeSectionTransform` to throw `ArgumentOutOfRangeException` during window resize?
- **Inspection**:
  - `BeamElevationCanvas.cs` lines 73–74: `double width = Math.Max(200.0, ActualWidth > 0 ? ActualWidth : 800.0);` and `double height = Math.Max(120.0, ActualHeight > 0 ? ActualHeight : 200.0);`.
  - `BeamSectionCanvas.cs` lines 84–85: `double width = Math.Max(120.0, ActualWidth > 0 ? ActualWidth : 220.0);` and `double height = Math.Max(120.0, ActualHeight > 0 ? ActualHeight : 220.0);`.
  - The margins (40px and 32px) satisfy $2 \cdot \text{margin} < \min(\text{width}, \text{height})$ under all conditions.
- **Stress Result**: **PASS**.

### Challenge 3: Missing Theme Resources Fallback
- **Hypothesis**: If Revit theme dictionaries are missing expected `Brush.Canvas.*` keys, does canvas painting crash with `NullReferenceException`?
- **Inspection**: `CanvasPalette.Resolve` (lines 123–136) catches null results from `element.TryFindResource(key)`, logs a warning via `Log.Warning`, and returns a frozen fallback `SolidColorBrush`.
- **Stress Result**: **PASS**.

---

## Coverage Gaps
- Interactive in-process Revit execution has not been verified (depends on Revit 2025/2026 runtime launch). Risk level: Low for canvas rendering, as WPF canvases run entirely in the presentation tier.

## Unverified Items
- None within the scope of M4 preview canvases and drawing primitives.
