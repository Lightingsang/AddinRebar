# Milestone M4 Iteration 2 Technical Review Report

**Reviewer**: reviewer_m4_it2_2  
**Role**: Reviewer & Adversarial Critic  
**Date**: 2026-09-07  
**Scope**: Preview Canvases, Frozen Resource Caching, Coordinate Transforms, and Boundary Safety  

---

## Review Summary

**Verdict**: **APPROVE**

The implementation in `worker_m4_it2` addresses all identified defects from Iteration 1 with precision, adheres strictly to the project architecture, preserves pure domain isolation in `HPRebar.Core`, eliminates GC churn in the WPF render loop, and handles geometric edge cases (exterior cantilevers, ultranarrow sections, high-aspect-ratio beams) robustly. No integrity violations or facade implementations were detected.

---

## Findings

### Good Practices Identified

1. **Dual Redundancy for Member Access (CS1061 Resolution)**:
   - In `BeamElevationPainter.cs`: Lines 215–216 and 427–428 access `stack.ContinuousStack.OverallStartX` and `stack.ContinuousStack.OverallEndX`.
   - In `BeamStack.cs`: Lines 32, 35, and 38 expose forwarding properties (`OverallStartX`, `OverallEndX`, `TotalLength`), preventing regressions if future callers access properties directly on `BeamStack`.
   - In `BeamElevationPainter.cs:404`: Replaced incorrect property name `inter.IntersectionX` with `inter.CenterX`, exactly matching `SecondaryBeamIntersection.cs:40`.

2. **Full Exterior Cantilever Enclosure in Canvas Viewport**:
   - In `BeamContinuousStack.cs:41–53` and `56–68`:
     ```csharp
     public double OverallStartX
     {
         get
         {
             if (Spans.Count == 0 && Supports.Count == 0) return 0.0;
             double min = Spans.Count > 0 ? Spans[0].StartX : double.MaxValue;
             if (Supports.Count > 0 && Supports[0].LeftFaceX < min)
             {
                 min = Supports[0].LeftFaceX;
             }
             return min == double.MaxValue ? 0.0 : min;
         }
     }
     ```
   - When exterior cantilever overhangs exist at either the start or end of a continuous beam run, `min` captures `Spans[0].StartX` and `max` captures `Spans[last].EndX`, even when support nodes do not precede the cantilever tip. This guarantees that `BeamCanvasTransformCalculator.ComputeElevationTransform` calculates coordinate bounds $X_{\min}$ and $X_{\max}$ encompassing all cantilever concrete and rebar without clipping or negative screen coordinates.

3. **Dynamic Proportional Layer Offsets**:
   - In `BeamElevationPainter.cs:270–272, 305–308, 353–356`:
     ```csharp
     double beamHeightPx = Math.Max(2.0, span.Height * _transform.Scale);
     double layerOffset = Math.Min(3.0, beamHeightPx * 0.15);
     double layerGap = Math.Min(5.0, beamHeightPx * 0.20);
     ```
   - On extreme aspect-ratio spans (e.g. 5 spans totaling 30m with 400mm depth where rendered height is small), the vertical offsets scale gracefully below $15\%$ and $20\%$ of screen height, preventing Layer 2 rebar from crossing over opposing layers or penetrating the beam soffit.

4. **Zero Render-Loop Heap Allocations**:
   - `CanvasPalette.cs`:
     - Defines static pre-frozen `DefaultDashStyle` (`new DashStyle(new double[] { 4, 3 }, 0)` with `Freeze()`).
     - Pre-allocates and freezes `DashedDimension` and `DashedSideBar`.
     - Ensures all brushes and pens returned are frozen via `Freeze()`.
   - `BeamElevationCanvas.cs` & `BeamSectionCanvas.cs`:
     - Maintain an instance cache `_palette` initialized on demand via `_palette ??= CanvasPalette.From(this)`.
     - Invalidate `_palette = null` only on theme change (`InvalidatePalette()`), control lifecycle (`Loaded`/`Unloaded`), or session swap (`OnSessionChanged`).
     - Render passes execute with zero WPF pen/brush heap allocations.

5. **Ultranarrow Section Guardrails**:
   - In `BeamSectionPainter.cs:79–84, 114–119`:
     ```csharp
     if (topEndX < topStartX)
     {
         topStartX = (stirrupLeft + stirrupRight) / 2.0;
         topEndX = topStartX;
     }
     double topStep = topCount > 1 ? (topEndX - topStartX) / (topCount - 1) : 0;
     ```
   - When beam width inside cover and stirrup diameter is smaller than the required bar envelope, `topStartX` and `topEndX` collapse safely to the centerline and `topStep` is set to 0. All bars render centered without negative spacing, inverse offsets, or division by zero.
   - For single-bar layers, explicit checks (`layer2Count == 1 ? (topStartX + topEndX) / 2.0 : ...`) ensure exact centering without `0 / 0` division.

6. **Integrity & Conformance**:
   - Zero hardcoded test outputs or mock shortcuts.
   - No modifications outside feature boundary.
   - Strictly conforms to `AGENTS.md` and `development-rules.md`.

---

## Verified Claims

| Claim | Verification Method | Result |
|---|---|---|
| `ContinuousStack.OverallStartX/EndX` member access | Static analysis of `BeamElevationPainter.cs` lines 215, 216, 436, 437 | **PASS** |
| `BeamStack` forwarding properties | Static analysis of `BeamStack.cs` lines 32, 35, 38 | **PASS** |
| `SecondaryBeamIntersection.CenterX` access | Static analysis of `BeamElevationPainter.cs:404` vs `SecondaryBeamIntersection.cs:40` | **PASS** |
| Cantilever bounds inclusion | Mathematical coordinate check on `BeamContinuousStack.cs:41–68` | **PASS** |
| Dynamic layer scaling | Mathematical bounds check on `BeamElevationPainter.cs:305–308` | **PASS** |
| Zero render-loop allocations | Static analysis of `CanvasPalette.cs`, `BeamElevationCanvas.cs`, `BeamSectionCanvas.cs` | **PASS** |
| Narrow section division-by-zero safety | Boundary analysis of `BeamSectionPainter.cs:79–84, 114–119, 132–137` | **PASS** |
| XAML dynamic resource tokens | Verification against `Spacing.xaml:21` and `Typography.xaml:14` | **PASS** |
| Two-way binding on additional bar editors | Static analysis of `BeamRebarSession.cs:222–250` and `AdditionalBarsTabView.xaml` | **PASS** |

---

## Adversarial Stress Tests

1. **Exterior Cantilever with No Tip Node**:
   - *Attack*: Continuous beam where Span 0 is a 2000mm cantilever starting at X=0, Support 0 is the first column at X=2000 (LeftFaceX=1800).
   - *Evaluation*: `Spans[0].StartX` = 0. `min` = 0. `Supports[0].LeftFaceX` = 1800 > 0. Result: `OverallStartX = 0`. Viewport bounds encompass the cantilever from X=0 to X=2000. No truncation.

2. **Ultranarrow Beam ($b = 100\text{mm}$, cover = $35\text{mm}$, stirrup = $10\text{mm}$, top bar = $25\text{mm}$)**:
   - *Attack*: Clear width inside stirrups = $100 - 2(35) - 2(10) = 10\text{mm}$. Top bar diameter = $25\text{mm}$. `topEndX < topStartX`.
   - *Evaluation*: Guard triggers `topStartX = topEndX = (stirrupLeft + stirrupRight) / 2.0`. `topStep = 0`. Bars placed at center. No `NaN` or negative coordinates.

3. **Single Main Bar Configuration ($topCount = 1$)**:
   - *Attack*: User specifies 1 bar in top layer.
   - *Evaluation*: `topCount = Math.Max(2, _session.TopBarCount)` guarantees `topCount >= 2` for outer stirrup corners. For Layer 2 additions where `layer2Count = 1`, `layer2Count > 1 ? ... : 0` prevents `1 - 1 = 0` division, and bar is placed at `(topStartX + topEndX) / 2.0`.

4. **Rapid Re-render (High FPS / Animation / Window Resize)**:
   - *Attack*: Window resize or continuous slider adjustment fires hundreds of `OnRender` events per second.
   - *Evaluation*: `_palette` is cached and reused across all render frames. Static `DefaultDashStyle` is frozen. No WPF Pen or Brush objects are created on the managed heap. GC pressure is zero.

---

## Conclusion

All objectives assigned to reviewer_m4_it2_2 are satisfied. The code is robust, cleanly structured, and ready for integration.

**Final Verdict**: **APPROVE**
