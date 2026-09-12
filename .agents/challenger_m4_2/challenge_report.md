# Challenge Report — Milestone M4 Canvas Geometry, Transforms & Memory Stress Test

**Agent**: `challenger_m4_2`  
**Role**: Milestone M4 Challenger 2 (Canvas Geometry, Transforms & Memory Stress Test)  
**Target Codebase**: `HPRebar/HPRebar/Beam Rebar/` (`View/Controls/`, `View Models/`, `HPRebar.Core/BeamRebar/`)  
**Overall Risk Assessment**: **CRITICAL**  
**Verdict**: **CHALLENGE_FAILED**  

---

## 1. Executive Summary

Milestone M4 delivered the interactive WPF presentation tier for Continuous Beam Reinforcement, featuring `BeamElevationCanvas`, `BeamSectionCanvas`, `BeamDrawPrimitives`, `CanvasPalette`, and debounced redraw mechanics.

While the MVVM architecture and 50ms `DispatcherTimer` debouncing properly prevent UI thread stalls during rapid text entry, empirical stress-testing across extreme aspect ratios, cross-sections, and memory lifecycle revealed **4 Critical and 1 High Severity defects**:
1. **Critical — Cantilever Overhangs Truncated & Projected Off-Screen**: `BeamContinuousStack.OverallStartX` and `OverallEndX` define beam extents solely from support faces, causing start cantilevers to map to negative screen coordinates ($X < 0$, off-canvas) with zero longitudinal reinforcement.
2. **Critical — Rebar Inversion & Soffit Penetration in Long Multi-Span Beams**: On 10-span beams ($L = 80\text{m}$, $h = 500\text{mm}$, aspect ratio 160:1), beam screen depth is only $4.5\text{px}$. Hardcoded $\pm 3.0\text{px}$ and $\pm 5.0\text{px}$ layer offsets cause top additional bars to penetrate through the soffit and bottom additional bars to float above the top flange, completely crossing over.
3. **Critical — Canvas Bottom Clipping on Deep Beams**: On deep transfer beams ($L=1200\text{mm}, h=3000\text{mm}$), beam height fills the drawing height. Bottom dimension lines ($Y = 202\text{px}$) and support captions ($Y = 198\text{px}-212\text{px}$) exceed canvas height ($200\text{px}$) and are clipped off-screen.
4. **Critical — Rebar Collision & Negative Distribution Step on Extreme Cross-Sections**: In tall thin beams ($b=120-150\text{mm}, h=1800\text{mm}$), rebar spacing $\Delta X$ collapses to $\le 2.2\text{px}$ or becomes negative ($-0.33\text{px}$ for $b=120\text{mm}$), reversing bar order outside stirrup hoops. In wide shallow beams ($b=2500\text{mm}, h=250\text{mm}$), top and bottom Layer 2 bars clash with 95% volume overlap.
5. **High — Violation of Zero-Allocation Frozen Pen Caching during `OnRender`**: `CanvasPalette.From(this)` is called inside `OnRender` on every frame, allocating 8 new frozen pens, 2 brushes, and 1 palette object per redraw. Furthermore, `BeamElevationPainter` instantiates new `Pen` and `DashStyle` objects inside support and skin bar render loops.

---

## 2. Detailed Challenges

### [Critical] Challenge 1: Cantilever Overhang Truncation and Negative Coordinate Projection

- **Assumption Challenged**: Continuous beam boundaries are fully defined by the first and last support bearing faces.
- **Root Cause Code**:
  `HPRebar.Core/BeamRebar/Models/BeamContinuousStack.cs` lines 41-44:
  ```csharp
  public double OverallStartX => Supports.Count > 0 ? Supports[0].LeftFaceX : (Spans.Count > 0 ? Spans[0].StartX : 0.0);
  public double OverallEndX => Supports.Count > 0 ? Supports[Supports.Count - 1].RightFaceX : (Spans.Count > 0 ? Spans[Spans.Count - 1].EndX : 0.0);
  ```
  `BeamCanvasTransformCalculator.cs` lines 111-112:
  ```csharp
  double xMin = stack.OverallStartX;
  double xMax = stack.OverallEndX;
  ```
  `BeamElevationPainter.cs` lines 219-220 & 298:
  ```csharp
  double xStart = _transform.ToScreenX(overallStartX + coverMm);
  ...
  double xLeft = i == 0 ? node.CenterX : node.LeftFaceX - extLeft;
  ```
- **Attack Scenario & Empirical Trace**:
  - Test setup: Beam with start cantilever of length $1500\text{mm}$ ($X \in [0, 1500]$), Support 0 at $CenterX = 1700\text{mm}$, $LeftFaceX = 1500\text{mm}$, Support 1 at $LeftFaceX = 7900\text{mm}$, $RightFaceX = 8300\text{mm}$.
  - `OverallStartX` returns `Supports[0].LeftFaceX = 1500.0` (ignores `Spans[0].StartX = 0.0`).
  - Model range: $xMin = 1500$, $xMax = 8300$, $L_{\text{model}} = 6800\text{mm}$.
  - For $W_{\text{draw}} = 720\text{px}$, $\text{Scale} = 720 / 6800 = 0.10588\text{ px/mm}$.
  - Cantilever tip screen position:
    $$\text{ToScreenX}(0) = 40.0 + (0 - 1500) \times 0.10588 = \mathbf{-118.8\text{ px}}$$
  - Main top rebar screen start position:
    $$\text{ToScreenX}(1500 + 25) = 40.0 + 2.6 = \mathbf{42.6\text{ px}}$$
- **Blast Radius**:
  - The cantilever overhang ($X \in [0, 1500]$) is rendered between $-118.8\text{px}$ and $40.0\text{px}$ — completely clipped off the left edge of the WPF canvas!
  - Main top and bottom reinforcement starts at $X = 42.6\text{px}$ (inside Support 0). Zero reinforcement is drawn inside the cantilever.
  - For Support 0, additional top bars terminate at `node.CenterX` because $i=0$ hardcodes `xLeft = node.CenterX` and `leftLn = 0.0`.
  - Overall length dimension prints $L = 6800$ instead of $L = 8300$.
- **Mitigation**:
  1. Update `BeamContinuousStack.OverallStartX` and `OverallEndX` to consider spans:
     `OverallStartX => Math.Min(Supports.Count > 0 ? Supports[0].LeftFaceX : double.MaxValue, Spans.Count > 0 ? Spans[0].StartX : 0.0);`
     `OverallEndX => Math.Max(Supports.Count > 0 ? Supports[^1].RightFaceX : double.MinValue, Spans.Count > 0 ? Spans[^1].EndX : 0.0);`
  2. In `BeamElevationPainter`, project main bars and additional bars using actual span geometric boundaries and support cantilever flags.

---

### [Critical] Challenge 2: Rebar Inversion and Soffit Penetration under Extreme Aspect Ratios (Long Multi-Span)

- **Assumption Challenged**: Fixed pixel offsets ($+3.0\text{px}, +5.0\text{px}$) for secondary reinforcement layers are universally valid across all zoom scales and aspect ratios.
- **Root Cause Code**:
  `BeamElevationPainter.cs` lines 302, 318, 347, 358:
  ```csharp
  double yLayer1 = _transform.ToScreenY(zTop) + 3.0; // Slightly offset below main bar
  ...
  double yLayer2 = yLayer1 + 5.0; // Gap
  ...
  double yLayer1 = _transform.ToScreenY(zBot) - 3.0; // Slightly above main bar
  ...
  double yLayer2 = yLayer1 - 5.0;
  ```
- **Attack Scenario & Empirical Trace**:
  - Test setup: 10-span continuous beam, total length $L = 80,000\text{mm}$, height $h = 500\text{mm}$ ($L/h = 160:1$).
  - Canvas available area: $W_{\text{draw}} = 720\text{px}, H_{\text{draw}} = 120\text{px}$.
  - $\text{Scale} = \min(720 / 80000, 120 / 500) = \min(0.009, 0.24) = 0.009\text{ px/mm}$.
  - Concrete screen height: $h_{\text{screen}} = 500 \times 0.009 = \mathbf{4.5\text{ px}}$.
  - Screen bounds of concrete beam: $y_{\text{top}} = 97.75\text{px}$, $y_{\text{bot}} = 102.25\text{px}$.
  - Top bar position: $y_{\text{topBar}} \approx 98.5\text{px}$.
  - Top additional Layer 2 position:
    $$y_{\text{topBar}} + 3.0 + 5.0 = 98.5 + 8.0 = \mathbf{106.5\text{ px}}$$
    ($106.5 > 102.25\text{px} \implies \mathbf{4.25\text{ px}}$ **below the bottom soffit of the beam**).
  - Bottom additional Layer 2 position:
    $$y_{\text{botBar}} - 3.0 - 5.0 = 101.5 - 8.0 = \mathbf{93.5\text{ px}}$$
    ($93.5 < 97.75\text{px} \implies \mathbf{4.25\text{ px}}$ **above the top flange of the beam**).
- **Blast Radius**:
  - The additional rebar layers cross over each other: negative moment top bars appear underneath the beam soffit, while positive moment bottom bars appear floating above the top edge.
  - Main bars ($2.0\text{px}$ thick each) separated by only $3.73\text{px}$ center-to-center merge into a single indistinct red blur.
- **Mitigation**:
  - Scale layer separation dynamically based on available screen beam depth:
    `double layerOffset = Math.Min(3.0, hScreen * 0.15);`
    `double layerGap = Math.Min(5.0, hScreen * 0.20);`
  - When $h_{\text{screen}} < 12\text{px}$, coalesce layers into a single line with double thickness or dash pattern.

---

### [Critical] Challenge 3: Annotation and Dimension Canvas Clipping on Deep Beams

- **Assumption Challenged**: A fixed margin of $40.0\text{px}$ provides sufficient headroom and footroom for support columns, centerlines, clear span dimensions, overall dimensions, and support labels.
- **Root Cause Code**:
  `BeamCanvasTransformCalculator.cs` lines 81-83:
  ```csharp
  double offsetY = marginPx + ((hDraw - hContent) / 2.0);
  double baseline = canvasHeightPx - offsetY;
  ```
  `BeamElevationPainter.cs` lines 76, 90, 410, 429:
  ```csharp
  BeamDrawPrimitives.FilledBox(dc, _palette.SupportFill, xLeft, yBot, width, 36.0);
  ...
  BeamDrawPrimitives.Caption(dc, _palette.Text, label, xCenter, yBot + 38.0, center: true);
  ...
  double yBaseDim = _transform.ToScreenY(zMin) + 20.0;
  ...
  double yTotalDim = yBaseDim + 22.0;
  ```
- **Attack Scenario & Empirical Trace**:
  - Test setup: 1-span transfer girder, $L = 1200\text{mm}, h = 3000\text{mm}$ ($L/h = 0.4:1$).
  - Canvas size: $W = 800\text{px}, H = 200\text{px}, \text{Margin} = 40.0\text{px}$.
  - $H_{\text{draw}} = 120\text{px}$. $H_{\text{model}} = 3000\text{mm}$.
  - $\text{Scale} = \min(720 / 1200, 120 / 3000) = 0.04\text{ px/mm}$.
  - $h_{\text{content}} = 3000 \times 0.04 = 120.0\text{ px}$ (fills $100\%$ of $H_{\text{draw}}$).
  - $OffsetY = 40.0\text{px}$. $Baseline = 200 - 40 = 160.0\text{px}$.
  - Beam soffit screen position: $y_{\text{bot}} = 160.0\text{px}$.
  - Clear span dimension: $y_{\text{baseDim}} = 160.0 + 20.0 = 180.0\text{px}$.
  - Overall length dimension line: $y_{\text{totalDim}} = 180.0 + 22.0 = \mathbf{202.0\text{ px}}$.
  - Canvas height is $200.0\text{px}$.
    $202.0\text{px} > 200.0\text{px} \implies$ **completely clipped past the bottom edge**!
  - Support caption label: $Y = y_{\text{bot}} + 38.0 = 198.0\text{px}$. With font height $14\text{px}$, label extends to $212.0\text{px}$, clipped by $12\text{px}$.
  - In addition, clear span length in pixels is $700 \times 0.04 = 28\text{px}$. The text `"Ln= 700"` has width $\approx 50\text{px}$, exceeding the span width by $78\%$ and colliding with both adjacent columns.
- **Blast Radius**:
  - Crucial engineering dimensions ($L_{\text{total}}$) and support identification labels are completely invisible to the user.
- **Mitigation**:
  - Dedicate asymmetrical vertical margins: e.g. `marginTop = 30px`, `marginBottom = 65px` to guarantee clearance for annotations.
  - Abbreviate or hide dimensions when $w_{\text{span}} < \text{textWidth} + 10\text{px}$.

---

### [Critical] Challenge 4: Cross-Section Rebar Collision and Negative Distribution Step on Extreme Cross-Sections

- **Assumption Challenged**: Cross-section width and height are always large enough relative to cover, bar diameters, and minimum radius ($2.5\text{px}$) to maintain positive inner clearances.
- **Root Cause Code**:
  `BeamSectionPainter.cs` lines 73-79:
  ```csharp
  double topRadiusPx = Math.Max(2.5, (topD / 2.0) * _transform.Scale);
  ...
  double topStartX = stirrupLeft + (stirrupDMm * _transform.Scale) + topRadiusPx;
  double topEndX = stirrupRight - (stirrupDMm * _transform.Scale) - topRadiusPx;
  double topStep = topCount > 1 ? (topEndX - topStartX) / (topCount - 1) : 0;
  ```
- **Attack Scenario & Empirical Trace**:
  - **Case A: Tall Thin Beam ($b = 120\text{mm}, h = 1800\text{mm}$)**:
    - Canvas $220 \times 220\text{px}$, Margin $32\text{px}$, $W_{\text{draw}} = H_{\text{draw}} = 156\text{px}$.
    - $\text{Scale} = 156 / 1800 = 0.08667\text{ px/mm}$.
    - $w_{\text{box}} = 120 \times 0.08667 = 10.40\text{ px}$.
    - Stirrup inner width: $sWidth = 10.40 - (2 \times 25 \times 0.08667) = 6.06\text{ px}$.
    - Top bar radius: clamped to `topRadiusPx = 2.5px`.
    - Stirrup diameter in pixels: $8 \times 0.08667 = 0.693\text{ px}$.
    - Distance available between bar centers:
      $$\Delta X = topEndX - topStartX = 6.06 - 2 \times (0.693 + 2.5) = 6.06 - 6.386 = \mathbf{-0.326\text{ px}}$$
    - For `topCount = 4`:
      $$\text{topStep} = \frac{-0.326}{3} = \mathbf{-0.109\text{ px}}$$
    - **Step is negative!** The bars are distributed backwards from right to left, protruding entirely outside the concrete outline!
  - **Case B: Wide Shallow Beam ($b = 2500\text{mm}, h = 250\text{mm}$)**:
    - $\text{Scale} = 156 / 2500 = 0.0624\text{ px/mm}$.
    - $h_{\text{box}} = 250 \times 0.0624 = 15.60\text{ px}$.
    - Top Layer 2 bar center: $Y = 109.88\text{px}$.
    - Bottom Layer 2 bar center: $Y = 110.12\text{px}$.
    - Gap between centers: $110.12 - 109.88 = \mathbf{0.24\text{ px}}$.
    - With bar diameter $5.0\text{px}$, top and bottom bars overlap by $4.76\text{px}$ (95% area intersection).
- **Blast Radius**:
  - The cross-section preview renders completely corrupted reinforcement graphics (inverted coordinates, bars drawn outside the concrete boundary, overlapping visual artifacts).
- **Mitigation**:
  - Clamp `topRadiusPx` dynamically so that $2 \cdot \text{topRadiusPx} \le sWidth / \text{count}$.
  - Guard against $\Delta X \le 0$: if $\Delta X \le 0$, clamp $\Delta X = 0$ and draw bars as a single clustered pack or warning indicator.

---

### [High] Challenge 5: Violation of Zero-Allocation Frozen Pen Caching during `OnRender`

- **Assumption Challenged**: All pens and brushes are frozen once and cached across rendering passes without heap allocations during `OnRender`.
- **Root Cause Code**:
  `BeamElevationCanvas.cs` line 77:
  ```csharp
  protected override void OnRender(DrawingContext drawingContext)
  {
      ...
      var palette = CanvasPalette.From(this); // Allocated every OnRender pass!
  ```
  `BeamSectionCanvas.cs` line 88:
  ```csharp
  protected override void OnRender(DrawingContext drawingContext)
  {
      ...
      var palette = CanvasPalette.From(this); // Allocated every OnRender pass!
  ```
  `BeamElevationPainter.cs` lines 85 & 381:
  ```csharp
  for (int i = 0; i < stack.Supports.Count; i++)
  {
      ...
      var dashedPen = CanvasPalette.Dashed(_palette.Dimension); // New Pen + DashStyle + double[] per support!
  }
  ...
  for (int i = 0; i < stack.Spans.Count; i++)
  {
      ...
      var dashedPen = CanvasPalette.Dashed(_palette.SideBar); // New Pen + DashStyle + double[] per span!
  }
  ```
- **Attack Scenario & Empirical Allocation Audit**:
  - In a standard session with 5 spans (6 supports):
    - `CanvasPalette.From(this)` allocates:
      - 8 `new Pen(...)` instances.
      - 2 `new SolidColorBrush(...)` instances (`supportFill`, `highlight`).
      - 1 `new CanvasPalette(...)` instance.
      - 6 `element.TryFindResource` lookups into the WPF visual tree dictionary.
    - Inside `PaintSupports`: 6 calls to `CanvasPalette.Dashed` allocate 6 `new Pen`, 6 `new DashStyle`, 6 `new double[]`.
    - Inside `PaintSideSkinBars`: 5 calls allocate 5 `new Pen`, 5 `new DashStyle`, 5 `new double[]`.
    - Total allocations per elevation redraw: **19 Pens, 11 DashStyles, 11 arrays, 2 Brushes, 1 Palette**!
  - During continuous resizing or rapid property changes, this creates substantial GC Gen 0 pressure.
- **Blast Radius**:
  - Unnecessary GC allocation churn on the WPF rendering thread.
  - Fails the explicit architectural contract of Milestone M4 and the golden standard set by `ColumnElevationCanvas` (which caches `_painter` and palette).
- **Mitigation**:
  1. Cache `CanvasPalette` as a field on `BeamElevationCanvas` and `BeamSectionCanvas`, invalidating it only when the active theme changes.
  2. Pre-create and freeze dashed pens (`DashedDimension`, `DashedSideBar`) inside `CanvasPalette` constructor instead of creating them dynamically in render loops.

---

## 3. Stress Test Results Summary

| Test Scenario | Input Dimensions | Expected Behavior | Actual Behavior | Verdict |
|---|---|---|---|---|
| **ST-01: Very Long Multi-Span** | 10 spans, $L=80\text{m}, h=500\text{mm}$ ($160:1$) | Layer 2 rebar rendered inside concrete outline | Top Layer 2 penetrates $4.25\text{px}$ below soffit; bottom Layer 2 floats $4.25\text{px}$ above top | **FAIL** |
| **ST-02: Very Short Deep Beam** | 1 span, $L=1200\text{mm}, h=3000\text{mm}$ ($0.4:1$) | Dimension lines and support labels visible on canvas | $L_{\text{total}}$ line at $Y=202\text{px}$ and labels at $Y=212\text{px}$ clipped off $200\text{px}$ canvas | **FAIL** |
| **ST-03: Cantilever Start Overhang** | $L_{\text{cant}}=1500\text{mm}, L_{\text{span}}=6000\text{mm}$ | Cantilever visible with continuous top rebar | Cantilever drawn at $X \in [-118.8, 40]\text{px}$ (off-screen); 0 rebar in overhang | **FAIL** |
| **ST-04: Cantilever Top Additional Rebar** | Cantilever at Support 0 | Top bars extend into cantilever | Top bars cut off at `node.CenterX` due to hardcoded $i=0$ guard | **FAIL** |
| **ST-05: Tall Thin Section** | $b=120\text{mm}, h=1800\text{mm}$ ($1:15$) | Rebar arranged with positive step inside stirrups | $\Delta X = -0.33\text{px}$, step negative, bars drawn backwards outside stirrups | **FAIL** |
| **ST-06: Wide Transfer Section** | $b=2500\text{mm}, h=250\text{mm}$ ($10:1$) | Top and bottom Layer 2 rebar maintain vertical gap | Gap is $0.24\text{px}$ vs $5.0\text{px}$ diameter $\implies 95\%$ clash / overlap | **FAIL** |
| **ST-07: Frozen Pen Allocation Audit** | Elevation OnRender with 6 supports | 0 Pen allocations during `OnRender` | 19 Pens, 11 DashStyles, 2 Brushes allocated on heap every frame | **FAIL** |
| **ST-08: Parameter Keystroke Debounce** | 10 keystrokes in 300ms | Single render pass 50ms after last key | Coalesced into single pass, no UI thread stalls | **PASS** |

---

## 4. Unchallenged Areas

- **WPF Dialog Theme Switching**: DynamicResource brush swaps between `ThemeDark.xaml` and `ThemeLight.xaml` conform to repository standard and were verified via resource key mapping.
- **WPF Tab Navigation & Session Data Binding**: Observable collection and two-way property bindings between tabs and `BeamRebarSession` were confirmed functional and reactive.

---

## 5. Conclusion and Recommendations

The preview canvases exhibit structural coordinate projection failures under non-standard but architecturally common beam geometries (cantilevers, deep transfer beams, thin perimeter beams, long multi-span runs), as well as heap allocation churn during rendering.

**Recommended Actions Before M4 Approval**:
1. Fix `BeamContinuousStack.OverallStartX` and `OverallEndX` to encapsulate cantilevers.
2. In `BeamElevationPainter`, dynamically scale rebar layer pixel offsets relative to $h_{\text{screen}}$.
3. In `BeamCanvasTransformCalculator`, increase bottom margin padding or allocate dynamic annotation margins to prevent dimension clipping.
4. In `BeamSectionPainter`, clamp minimum rebar radius and guard against negative distribution steps.
5. In `CanvasPalette`, pre-freeze dashed pens and cache the palette instance on the Canvas controls.
