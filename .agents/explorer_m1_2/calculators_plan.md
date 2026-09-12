# Comprehensive Domain Calculators Design: HPRebar.Core/BeamRebar/Calculators/

**Author**: `explorer_m1_2` (Role: M1 Domain Calculators Architect)  
**Target Subsystem**: `HPRebar.Core/BeamRebar/Calculators/` and `HPRebar.Core/BeamRebar/Tolerance.cs`  
**Target Framework**: `netstandard2.0` (with Polyfill 11.0.1)  
**Date**: 2026-09-07  
**Status**: Authoritative Architectural & Algorithmic Blueprint  

---

## 1. Architectural Tenets & Geometric Coordinate Foundation

### 1.1 Decoupling & Pure Mathematical Execution
All calculators in `HPRebar.Core/BeamRebar/Calculators/` are **pure, stateless static classes**.
- **Zero Revit References**: Absolutely no dependencies on `Autodesk.Revit.*`, `Nice3point.Revit.*`, or WPF UI libraries.
- **Unified Engineering Units**: Every geometric coordinate, dimension, length, width, cover, spacing, and diameter is strictly in **millimetres** (`double`).
- **Determinism**: Identical inputs produce identical outputs with zero side-effects.

### 1.2 Unified Continuous Beam Coordinate Datum
Continuous beams in Autodesk Revit are composed of individual `FamilyInstance` elements that may have been drawn in opposing or arbitrary directions. To guarantee continuous alignment, all calculations are executed within a normalized **Continuous Beam Local Coordinate Frame**:
1. **$X$-Axis (Longitudinal / Beam Axis)**:
   - Starts at $X = 0$ at the outer face/centerline of the first support node (Support 0).
   - Extends continuously across all spans $1 \dots N$ to the far end of the final span.
   - Vector $\vec{U}_X$ aligns from initial support towards terminating support.
2. **$Y$-Axis (Transverse Normal)**:
   - Horizontal normal perpendicular to beam axis: $\vec{U}_Y = \vec{U}_Z \times \vec{U}_X$.
   - Origin $Y = 0$ is aligned with the continuous beam centerline.
   - For a beam of width $b$: Left exterior face is at $Y = -b/2$; Right exterior face is at $Y = +b/2$.
3. **$Z$-Axis (Vertical Elevation)**:
   - World vertical vector $\vec{U}_Z = (0, 0, 1)$.
   - $Z_{top, i}$: Top elevation of span $i$ in millimetres.
   - $Z_{bot, i} = Z_{top, i} - h_i$: Bottom soffit elevation of span $i$.

```
 Elevation (X-Z plane):
 Z_top +--------------------+-----------------------+--------------------+
       |      Span 1        |        Span 2         |       Span 3       |
       | (b1 x h1)          | (b2 x h2)             | (b3 x h3)          |
 Z_bot +---------+----------+-----------+-----------+----------+---------+
                 |                      |                      |
            [Support 0]            [Support 1]            [Support 2]   [Support 3]
            Column c0              Column c1              Column c2     Column c3
       X = 0 -----------------------------------------------------> X_total
```

---

## 2. Core Calculators Specification

### 2.1 `Tolerance.cs`
**File Location**: `HPRebar.Core/BeamRebar/Tolerance.cs`  
**Namespace**: `HPRebar.Core.BeamRebar`  

#### 2.1.1 Purpose
Provides numerical precision thresholds and floating-point comparisons across all beam calculations. Matches the proven $1.0\times 10^{-9}$ threshold from `HPRebar.Core.ColumnRebar.Tolerance` while introducing geometric tolerances for segment simplification and collinearity checks.

#### 2.1.2 Constants & Mathematical Thresholds
- `Default = 1.0e-9`: Standard floating-point equality epsilon.
- `CollinearToleranceMm = 1.0e-6`: Permissible transverse deviation for merging intermediate collinear vertices.
- `MinimumSegmentMm = 1.0`: Revit API safety limit. Curve segments shorter than $1.0$ mm cause `ArgumentException: Curve is too short` during `Rebar.CreateFreeForm`.

#### 2.1.3 API Definition
```csharp
namespace HPRebar.Core.BeamRebar;

public static class Tolerance
{
    public const double Default = 1.0e-9;
    public const double CollinearToleranceMm = 1.0e-6;
    public const double MinimumSegmentMm = 1.0;

    public static bool AreEqual(double first, double second, double tolerance = Default) =>
        second - tolerance < first && first < second + tolerance;

    public static bool IsZero(double value, double tolerance = Default) =>
        Math.Abs(value) < tolerance;

    public static bool IsPositive(double value, double tolerance = Default) =>
        value > tolerance;

    public static bool IsNegative(double value, double tolerance = Default) =>
        value < -tolerance;

    public static bool IsGreaterOrEqual(double first, double second, double tolerance = Default) =>
        first > second - tolerance;

    public static bool IsLessOrEqual(double first, double second, double tolerance = Default) =>
        first < second + tolerance;
}
```

---

### 2.2 `BeamStirrupDistributionCalculator.cs`
**File Location**: `HPRebar.Core/BeamRebar/Calculators/BeamStirrupDistributionCalculator.cs`  
**Namespace**: `HPRebar.Core.BeamRebar.Calculators`  

#### 2.2.1 Purpose & Scope
Calculates closed stirrup distributions across clear beam spans ($L_n$), cantilevers, and optional interior support column nodes. Supports uniform layouts and Vietnamese/international 3-zone detailing ($L/4-L/2-L/4$ and $L/3-L/3-L/3$). Enforces the Revit API `MaxBarPositions = 1002` limit.

#### 2.2.2 Mathematical Formulations

##### A. Uniform Distribution (`StirrupDistributionType.Uniform = 0`)
- **Given**: Clear span $L_n$, spacing $S$, first bar start offset $o_{start} = 50$ mm.
- **Available distribution length**:
  $$L_{dist} = L_n - 2 \times o_{start}$$
- **Number of intervals & bar count**:
  If $L_{dist} \le 0$:
  - If $L_n > 0$, return 1 stirrup centered at $L_n / 2$.
  - Otherwise return 0 stirrups.
  If $L_{dist} > 0$:
  $$k = \left\lfloor \frac{L_{dist}}{S} \right\rfloor$$
  $$Count = k + 1$$
- **Centering Slack & Offset**:
  The residual slack $\delta$ is split equally between the two ends to keep the stirrup set perfectly symmetrical within the span:
  $$\delta = \frac{L_{dist} - k \times S}{2}$$
  $$StartOffset = o_{start} + \delta$$
- **Stirrup Run Span**: Extends from $StartOffset$ to $StartOffset + k \times S$.

##### B. 3-Zone Distribution ($L/4 - L/2 - L/4$, `TypeDis = 1`)
- **Zone Boundaries**:
  $$L_1 = \frac{L_n}{4} \quad (\text{Left support dense zone})$$
  $$L_2 = L_n - 2 \times L_1 = \frac{L_n}{2} \quad (\text{Midspan sparse zone})$$
  $$L_3 = L_1 = \frac{L_n}{4} \quad (\text{Right support dense zone})$$
- **Short Span Fallback**:
  If clear span $L_n < 600$ mm (or $L_1 \le o_{start}$), 3-zone layout collapses automatically to **Uniform** layout at dense spacing $S_1$ to avoid zero or negative zone lengths.
- **Zone 1 (Left Support Zone, dense spacing $S_1$)**:
  - Starts at $o_{start} = 50$ mm from left support face.
  - Available length: $L_{dist, 1} = L_1 - o_{start}$.
  - $k_1 = \lfloor L_{dist, 1} / S_1 \rfloor$.
  - $Count_1 = k_1 + 1$.
  - Slack: $\delta_1 = (L_{dist, 1} - k_1 \times S_1) / 2$.
  - Start offset from left support face: $StartOffset_1 = o_{start} + \delta_1$.
- **Zone 3 (Right Support Zone, dense spacing $S_1$)**:
  - Symmetrical to Zone 1: $Count_3 = Count_1$, $Spacing_3 = S_1$.
  - Distance from right support face: $StartOffset_{3, right} = StartOffset_1$.
  - Absolute start offset from left support face:
    $$StartOffset_3 = (L_n - L_3) + \delta_1$$
- **Zone 2 (Midspan Zone, sparse spacing $S_2$)**:
  - Span length: $L_2 = L_n - 2 \times L_1$.
  - $k_2 = \lfloor L_2 / S_2 \rfloor$.
  - $Count_2 = k_2 + 1$.
  - Slack: $\delta_2 = (L_2 - k_2 \times S_2) / 2$.
  - Absolute start offset from left support face:
    $$StartOffset_2 = L_1 + \delta_2$$

##### C. 3-Zone Distribution ($L/3 - L/3 - L/3$, `TypeDis = 2`)
Identical to the above equations, with zone lengths:
$$L_1 = \frac{L_n}{3}, \quad L_2 = \frac{L_n}{3}, \quad L_3 = \frac{L_n}{3}$$

##### D. Cantilever Stirrup Rules
For cantilever overhangs (exterior spans without an outer supporting column):
- Shear stress is critical near the support and remains elevated to the tip.
- Detailing rule: 100% dense spacing $S_1$ across the entire cantilever length $L_{cant}$.
- Start offset: $o_{start} = 50$ mm from interior support face.
- End clearance: $Cover$ from the cantilever free end tip.
- Available length: $L_{dist} = L_{cant} - o_{start} - Cover$.
- $Count = \lfloor L_{dist} / S_1 \rfloor + 1$.
- Slack: $\delta = (L_{dist} - (Count - 1) \times S_1) / 2$.
- $StartOffset = o_{start} + \delta$.

##### E. Column Node Stirrup Distribution
When `IsStirrupInNode` is enabled:
- Distributes closed ties across the column width $C$ through the beam-column joint.
- Available node length: $L_{node} = C - 2 \times Cover_{col}$.
- Spacing $S_{node}$.
- $Count_{node} = \lfloor L_{node} / S_{node} \rfloor + 1$.
- Centered inside the column core:
  $$\delta_{node} = \frac{L_{node} - (Count_{node} - 1) \times S_{node}}{2}$$
  $$StartOffset_{node} = Cover_{col} + \delta_{node}$$

#### 2.2.3 Guardrails & Failure Handling
- **Revit Maximum Positions Limit**:
  ```csharp
  public const int MaxBarPositions = 1002;
  ```
  If any calculated $Count > 1002$, throws `ArgumentOutOfRangeException` with explicit message detailing spacing and count.
- **Strictly Positive Spacing**:
  Throws `ArgumentOutOfRangeException` if $S, S_1, S_2, S_{node} \le 0$.
- **Negative Clear Span**:
  Throws `ArgumentOutOfRangeException` if clear span $L_n < 0$.

#### 2.2.4 API Method Blueprint
```csharp
public static class BeamStirrupDistributionCalculator
{
    public const int MaxBarPositions = 1002;
    public const double DefaultStartOffsetMm = 50.0;
    public const double MinimumThreeZoneSpanMm = 600.0;

    public static IReadOnlyList<StirrupRun> ComputeSpanRuns(
        double clearSpanMm,
        BeamStirrupSpec spec,
        bool isCantilever = false);

    public static StirrupRun ComputeNodeRun(
        double supportWidthMm,
        double coverMm,
        double spacingMm);

    public static (double L1, double L2, double L3) ComputeZoneLengths(
        double clearSpanMm,
        StirrupDistributionType type);
}
```

---

### 2.3 `BeamMainBarCalculator.cs`
**File Location**: `HPRebar.Core/BeamRebar/Calculators/BeamMainBarCalculator.cs`  
**Namespace**: `HPRebar.Core.BeamRebar.Calculators`  

#### 2.3.1 Purpose & Scope
Calculates continuous 3D polyline geometry for longitudinal top and bottom reinforcement traversing all spans of the continuous beam. Handles:
- Transverse bar placement across span width $b$.
- Standard 90° exterior anchorage hooks into exterior columns.
- Cantilever top bar wrap-down detailing.
- Commercial bar length division ($L_{stock} = 11700$ mm) and 50% staggered lap splices in code-compliant moment compression zones.
- Cross-section depth transition steps (anchoring bottom bars at soffit drops).
- Segment simplification culling segments $< 1.0$ mm.

#### 2.3.2 Transverse Bar Distribution Across Beam Width
For a beam of width $b$, cover $c$, and stirrup diameter $d_{stirrup}$:
- Inner clear width:
  $$W_{inner} = b - 2 \times c - 2 \times d_{stirrup}$$
- For $n$ bars of diameter $d_{bar}$:
  - If $n = 1$: $Y = 0$.
  - If $n \ge 2$:
    - First bar center: $Y_0 = -\frac{b}{2} + c + d_{stirrup} + \frac{d_{bar}}{2}$
    - Last bar center: $Y_{n-1} = +\frac{b}{2} - c - d_{stirrup} - \frac{d_{bar}}{2}$
    - Transverse pitch: $\Delta Y = \frac{W_{inner} - d_{bar}}{n - 1}$
    - Bar index $i \in [0, n-1]$:
      $$Y_i = Y_0 + i \times \Delta Y$$

#### 2.3.3 Longitudinal Geometry & Anchorage Detailing

##### A. Continuous Top Main Bars
- Vertical coordinate:
  $$Z_{top\_bar} = Z_{top} - c - d_{stirrup} - \frac{d_{top}}{2}$$
- **Exterior Left Support Anchorage (Column)**:
  - Outer support column has width $C_0$.
  - Left column face: $X_{face, 0} = X_{col, 0} + C_0 / 2$.
  - Bar penetrates column core to:
    $$X_{anchor, 0} = X_{col, 0} - \frac{C_0}{2} + Cover_{col}$$
  - Standard 90° downward hook:
    $$L_{hook, top} = \min(h_0 - 2c - 2d_{stirrup}, \max(30 \times d_{top}, 200.0))$$
  - Vertices for bar $i$:
    1. Hook tip: $(X_{anchor, 0}, Y_i, Z_{top\_bar} - L_{hook, top})$
    2. Bend corner: $(X_{anchor, 0}, Y_i, Z_{top\_bar})$
    3. Traverses continuously across spans to right exterior support.
- **Exterior Right Support Anchorage**:
  - Right column face: $X_{face, end} = X_{col, end} - C_{end} / 2$.
  - Bar penetrates to: $X_{anchor, end} = X_{col, end} + \frac{C_{end}}{2} - Cover_{col}$.
  - Bend corner: $(X_{anchor, end}, Y_i, Z_{top\_bar})$.
  - Hook tip: $(X_{anchor, end}, Y_i, Z_{top\_bar} - L_{hook, top})$.
- **Cantilever Exterior Ends**:
  If Span 0 is a left cantilever: tension top bars extend to the cantilever tip $X_{tip} + Cover$, then bend downward 90° into a full-depth end hook ($h - 2c$).

##### B. Continuous Bottom Main Bars
- Vertical coordinate:
  $$Z_{bot\_bar} = Z_{bot} + c + d_{stirrup} + \frac{d_{bot}}{2}$$
- **Exterior Anchorage**: 90° upward hook turning up into the column core by $L_{hook, bot} \ge 30d_{bot}$.
- **Vertical Step Transition ($h_1 \ne h_2$)**:
  - When adjacent spans have different depths ($|Z_{bot, j} - Z_{bot, j+1}| > 1.0$ mm), bottom bars cannot be bent across the corner.
  - DETECT: If $|h_j - h_{j+1}| > 1.0$ mm:
    - Span $j$ bottom bars terminate inside support node $j$ with an upward 90° hook.
    - Span $j+1$ bottom bars begin inside support node $j$ with an upward 90° hook.
    - This maintains structural integrity without cracking the concrete re-entrant corner.

#### 2.3.4 Lap Splicing & 50% Stagger Algorithm
When total length exceeds commercial stock length ($L_{stock} = 11700$ mm):
1. **Splice Zone Selection**:
   - **Top Bars**: Spliced strictly in the **midspan zone** ($0.35 L_n \le X \le 0.65 L_n$), where bending moment is positive and top fibers are in compression.
   - **Bottom Bars**: Spliced strictly at **intermediate supports** (within $0.20 L_n$ of support center), where bending moment is negative and bottom fibers are in compression.
2. **50% Alternating Stagger**:
   - The set of bars is split into:
     - Group A: even indices $i = 0, 2, 4 \dots$
     - Group B: odd indices $i = 1, 3, 5 \dots$
   - Splice Center for Group A: Station $X_{splice, A}$ in target span $k$.
   - Splice Center for Group B: Staggered by offset $\Delta_{stagger} \ge 1.3 \times L_{lap}$:
     $$X_{splice, B} = X_{splice, A} + 1.3 \times L_{lap}$$
     (If $X_{splice, B}$ exceeds the midspan allowable window, it shifts to the adjacent span's midspan).
3. **Lap Overlap Segments**:
   - Each splice splits the continuous line into Bar Segment 1 and Bar Segment 2:
     - Segment 1 ends at $X_{splice} + L_{lap} / 2$.
     - Segment 2 begins at $X_{splice} - L_{lap} / 2$.
   - Transverse placement: Segment 2 is offset laterally by $+d_{bar}$ (or paired alongside) to model physical non-intersecting bars.

#### 2.3.5 Polyline Simplification & Culling
Before returning polylines:
1. Cull consecutive points where Euclidean distance $< Tolerance.MinimumSegmentMm$ ($1.0$ mm).
2. Remove intermediate collinear vertices where transverse deviation $< Tolerance.CollinearToleranceMm$ ($10^{-6}$ mm).

#### 2.3.6 API Method Blueprint
```csharp
public static class BeamMainBarCalculator
{
    public const double CommercialStockLengthMm = 11700.0;
    public const double DefaultLapMultiplier = 40.0;

    public static IReadOnlyList<BeamBarPolyline> ComputeTopMainBars(
        BeamContinuousStack stack,
        BeamMainBarSpec spec,
        double stirrupDiameterMm);

    public static IReadOnlyList<BeamBarPolyline> ComputeBottomMainBars(
        BeamContinuousStack stack,
        BeamMainBarSpec spec,
        double stirrupDiameterMm);

    public static IReadOnlyList<double> ComputeTransverseYPositions(
        double widthMm,
        double coverMm,
        double stirrupDiameterMm,
        double barDiameterMm,
        int count);

    public static IReadOnlyList<Point3> SimplifyPolyline(IReadOnlyList<Point3> vertices);
}
```

---

### 2.4 `BeamAdditionalBarCalculator.cs`
**File Location**: `HPRebar.Core/BeamRebar/Calculators/BeamAdditionalBarCalculator.cs`  
**Namespace**: `HPRebar.Core.BeamRebar.Calculators`  

#### 2.4.1 Purpose & Scope
Calculates additional longitudinal reinforcement:
- **Top Negative Moment Additional Bars**: Centered over support columns, extending into adjacent clear spans by $L_n/3$ or $L_n/4$, in up to 2 vertical layers with staggered cutoffs.
- **Bottom Positive Moment Additional Bars**: Centered in midspans, starting and ending at $L_n/7$ or $L_n/8$ from support faces, in up to 2 vertical layers.

#### 2.4.2 Top Additional Bars Over Supports

##### A. Intermediate Support Geometry
For intermediate support $j$ between Span $j-1$ (left) and Span $j$ (right):
- Support width: $C_j$.
- Clear span left: $L_{n, left}$; Clear span right: $L_{n, right}$.
- Left support face: $X_{face, L} = X_{node, j} - C_j / 2$.
- Right support face: $X_{face, R} = X_{node, j} + C_j / 2$.
- **Layer 1 Extension Lengths** (default ratio $r_1 = 1/3$):
  $$L_{ext, L1} = r_1 \times L_{n, left}$$
  $$L_{ext, R1} = r_1 \times L_{n, right}$$
  $$X_{start, 1} = X_{face, L} - L_{ext, L1}, \quad X_{end, 1} = X_{face, R} + L_{ext, R1}$$
  $$L_{bar, 1} = L_{ext, L1} + C_j + L_{ext, R1}$$
- **Layer 2 Extension Lengths** (staggered cutoff, default ratio $r_2 = 1/4$):
  $$L_{ext, L2} = r_2 \times L_{n, left}$$
  $$L_{ext, R2} = r_2 \times L_{n, right}$$
  $$X_{start, 2} = X_{face, L} - L_{ext, L2}, \quad X_{end, 2} = X_{face, R} + L_{ext, R2}$$
  $$L_{bar, 2} = L_{ext, L2} + C_j + L_{ext, R2}$$

##### B. Exterior Support Geometry
At an exterior end support (e.g. Support 0):
- Negative top moment bar anchors into the exterior column with a 90° downward hook:
  $$L_{hook} = \min(h_0 - 2c - 2d_{stirrup}, 30d_{bar})$$
- Extends into the first span by $L_n / 3$ (Layer 1) or $L_n / 4$ (Layer 2).

##### C. Vertical Layer Stacking & Clearance
- **Layer 1**: Placed at identical elevation to Top Main Bars:
  $$Z_1 = Z_{top} - Cover - d_{stirrup} - \frac{d_{add}}{2}$$
  Arranged horizontally in the spaces between top main bars.
- **Layer 2**: Placed directly beneath Layer 1 with clear vertical gap:
  $$\Delta Z_{gap} = \max(30.0\text{ mm}, d_{add})$$
  $$Z_2 = Z_1 - \left(\frac{d_{add, 1}}{2} + \Delta Z_{gap} + \frac{d_{add, 2}}{2}\right)$$

#### 2.4.3 Bottom Additional Bars at Midspan

##### A. Span Clear Geometry
For Span $i$ with clear span $L_{n, i}$:
- Left support face: $X_{face, L}$; Right support face: $X_{face, R}$.
- Cutoff ratio: default $r_{cut} = 1/7 \approx 0.143$ (or $1/8 = 0.125$).
- Cutoff distance from faces:
  $$d_{cut} = r_{cut} \times L_{n, i}$$
- Bar boundaries:
  $$X_{start} = X_{face, L} + d_{cut}$$
  $$X_{end} = X_{face, R} - d_{cut}$$
  $$L_{bar} = L_{n, i} - 2 \times d_{cut} \quad (\approx 0.714 L_{n, i})$$
- Form: Straight horizontal bars (no end hooks needed because tensile stress at $L_n/7$ is negligible).

##### B. Vertical Layer Stacking
- **Layer 1**: In line with Bottom Main Bars:
  $$Z_1 = Z_{bot} + Cover + d_{stirrup} + \frac{d_{add}}{2}$$
- **Layer 2**: Stacked above Layer 1:
  $$\Delta Z_{gap} = \max(30.0\text{ mm}, d_{add})$$
  $$Z_2 = Z_1 + \left(\frac{d_{add, 1}}{2} + \Delta Z_{gap} + \frac{d_{add, 2}}{2}\right)$$

#### 2.4.4 API Method Blueprint
```csharp
public static class BeamAdditionalBarCalculator
{
    public const double DefaultTopCutoffRatioLayer1 = 1.0 / 3.0;
    public const double DefaultTopCutoffRatioLayer2 = 1.0 / 4.0;
    public const double DefaultBottomCutoffRatio = 1.0 / 7.0;
    public const double MinimumClearVerticalGapMm = 30.0;

    public static IReadOnlyList<BeamBarPolyline> ComputeSupportTopBars(
        BeamContinuousStack stack,
        BeamAdditionalTopBarSpec spec,
        double stirrupDiameterMm);

    public static IReadOnlyList<BeamBarPolyline> ComputeSpanBottomBars(
        BeamContinuousStack stack,
        BeamAdditionalBottomBarSpec spec,
        double stirrupDiameterMm);
}
```

---

### 2.5 `BeamSideBarCalculator.cs`
**File Location**: `HPRebar.Core/BeamRebar/Calculators/BeamSideBarCalculator.cs`  
**Namespace**: `HPRebar.Core.BeamRebar.Calculators`  

#### 2.5.1 Purpose & Scope
Calculates longitudinal skin / side reinforcement and transverse anti-buckling cross-ties for deep concrete beams.
- **Trigger Rule**: Enforced when beam height $h \ge 700$ mm (per TCVN 5574:2018 §10.3.2 and ACI 318 §9.7.2.3).
- Vertical spacing between side bar rows: $s_v \le 300$ mm.
- Transverse C-ties connecting opposite side bars with alternating 90°/135° hooks.

#### 2.5.2 Mathematical Formulation

##### A. Row Count & Vertical Distribution
- For a beam with height $h$, top cover $c_{top}$, bottom cover $c_{bot}$, stirrup diameter $d_{stirrup}$:
- Web depth between top and bottom main reinforcement layers:
  $$H_{web} = h - c_{top} - c_{bot} - 2 \times d_{stirrup} - d_{main}$$
- If $h < 700$ mm, returns 0 rows (empty list).
- If $h \ge 700$ mm:
  $$n_{rows} = \max\left(1, \left\lceil \frac{H_{web} - 200.0}{300.0} \right\rceil\right)$$
- Uniform vertical spacing:
  $$\Delta Z = \frac{H_{web}}{n_{rows} + 1}$$
- Elevation of row $k \in [1, n_{rows}]$:
  $$Z_k = (Z_{bot} + c_{bot} + d_{stirrup} + d_{main}/2) + k \times \Delta Z$$

##### B. Transverse Lateral Positions
Each row contains two symmetrical longitudinal bars:
- Left face bar:
  $$Y_{left} = -\frac{b}{2} + Cover + d_{stirrup} + \frac{d_{side}}{2}$$
- Right face bar:
  $$Y_{right} = +\frac{b}{2} - Cover - d_{stirrup} - \frac{d_{side}}{2}$$

##### C. Transverse Anti-Buckling Cross-Ties (C-Ties)
- Longitudinal spacing along beam clear span: $s_{tie} = \min(400.0\text{ mm}, 2 \times S_{stirrup})$.
- Tie runs across beam width from $Y_{left}$ to $Y_{right}$ at elevation $Z_k$.
- Count per row along clear span $L_n$:
  $$Count_{tie} = \left\lfloor \frac{L_n - 2 \times 50}{s_{tie}} \right\rfloor + 1$$
- **Seismic Hook Alternation**:
  At station $m$, tie has 135° hook on left and 90° hook on right. At station $m+1$, the hooks flip (90° left, 135° right). This alternation prevents joint unzipping under cyclic loading.

#### 2.5.3 API Method Blueprint
```csharp
public static class BeamSideBarCalculator
{
    public const double HeightThresholdMm = 700.0;
    public const double MaxVerticalSpacingMm = 300.0;
    public const double DefaultTieSpacingMm = 400.0;

    public static bool RequiresSideBars(double heightMm) => heightMm >= HeightThresholdMm;

    public static int ComputeRowCount(double heightMm, double coverMm, double stirrupDiameterMm, double mainDiameterMm);

    public static IReadOnlyList<BeamBarPolyline> ComputeLongitudinalSideBars(
        BeamContinuousStack stack,
        BeamSideBarSpec spec,
        double stirrupDiameterMm,
        double mainBarDiameterMm);

    public static IReadOnlyList<BeamBarPolyline> ComputeCrossTies(
        BeamContinuousStack stack,
        BeamSideBarSpec spec,
        double stirrupDiameterMm,
        double mainBarDiameterMm);
}
```

---

### 2.6 `BeamSpecialBarCalculator.cs`
**File Location**: `HPRebar.Core/BeamRebar/Calculators/BeamSpecialBarCalculator.cs`  
**Namespace**: `HPRebar.Core.BeamRebar.Calculators`  

#### 2.6.1 Purpose & Scope
Calculates special localized reinforcement at secondary framing intersections where incoming secondary beams introduce concentrated diagonal tension shear:
1. **Hanging Stirrups (Cốt treo)**: Concentrated closed stirrups flanking both sides of the secondary beam.
2. **Intersection Overlap Merging**: Merging hanging zones when multiple secondary beams are spaced closer than $200$ mm.
3. **45° Diagonal Ties (Thép vai bò)**: Bent bars positioned under the secondary beam soffit.

#### 2.6.2 Mathematical Formulation

##### A. Hanging Stirrups Layout
For a secondary beam intersecting at station $X_{sec}$ with width $b_s$:
- Left face of secondary beam: $X_{sec, L} = X_{sec} - b_s / 2$.
- Right face of secondary beam: $X_{sec, R} = X_{sec} + b_s / 2$.
- User specifies $n_{pairs}$ (e.g. 3 stirrups per side) at spacing $S_{hang} = 50$ mm.
- **Left Flanking Stirrups**:
  $$X_{k, L} = X_{sec, L} - (50.0 + k \times S_{hang}) \quad (k = 0 \dots n_{pairs}-1)$$
- **Right Flanking Stirrups**:
  $$X_{k, R} = X_{sec, R} + (50.0 + k \times S_{hang}) \quad (k = 0 \dots n_{pairs}-1)$$
- Cross-sectional dimensions match the primary beam stirrup:
  $$W = b_{prim} - 2 \times Cover, \quad H = h_{prim} - 2 \times Cover$$

##### B. Overlap Merging Algorithm
If two secondary beams are located at $X_{sec, 1}$ and $X_{sec, 2}$ with $|X_{sec, 2} - X_{sec, 1}| < b_s + 2 \times (50 + n_{pairs} \times S_{hang})$:
- The flanking zones collide.
- The algorithm computes the combined bounding envelope $[X_{min, zone}, X_{max, zone}]$, subtracts the secondary beam web footprints, and generates a unified, non-duplicated list of stirrup stations spaced evenly at $S_{hang}$.

##### C. 45° Diagonal Tie Geometry ("Thép vai bò")
Positioned directly under the secondary beam soffit:
- Inclination angle: $\theta = 45^\circ$ ($\tan 45^\circ = 1.0$).
- Height drop: $\Delta Z = (Z_{top} - Cover) - (Z_{bot} + Cover)$.
- Horizontal projection of diagonal leg: $\Delta X = \Delta Z / \tan 45^\circ = \Delta Z$.
- Bottom horizontal run: spans across secondary beam width $b_s + 2 \times 50$ mm.
- 6-point Polyline Vertices in longitudinal elevation:
  1. Top Left Anchor: $(X_{sec, L} - \Delta X - 30d, Y, Z_{top\_bar})$
  2. Top Left Bend: $(X_{sec, L} - \Delta X, Y, Z_{top\_bar})$
  3. Bottom Left Bend: $(X_{sec, L}, Y, Z_{bot\_bar})$
  4. Bottom Right Bend: $(X_{sec, R}, Y, Z_{bot\_bar})$
  5. Top Right Bend: $(X_{sec, R} + \Delta X, Y, Z_{top\_bar})$
  6. Top Right Anchor: $(X_{sec, R} + \Delta X + 30d, Y, Z_{top\_bar})$

#### 2.6.3 API Method Blueprint
```csharp
public static class BeamSpecialBarCalculator
{
    public const double DefaultHangingSpacingMm = 50.0;
    public const double DefaultHangingOffsetMm = 50.0;

    public static IReadOnlyList<double> ComputeHangingStirrupStations(
        double secondaryCenterXMm,
        double secondaryWidthMm,
        int countPerSide,
        double spacingMm = DefaultHangingSpacingMm);

    public static IReadOnlyList<double> MergeHangingStations(
        IReadOnlyList<double> stations,
        double minimumClearMm = 20.0);

    public static IReadOnlyList<Point3> ComputeDiagonalTiePolyline(
        double secondaryCenterXMm,
        double secondaryWidthMm,
        double primaryZTopMm,
        double primaryZBotMm,
        double coverMm,
        double barDiameterMm);
}
```

---

### 2.7 `BeamCanvasTransformCalculator.cs`
**File Location**: `HPRebar.Core/BeamRebar/Calculators/BeamCanvasTransformCalculator.cs`  
**Namespace**: `HPRebar.Core.BeamRebar.Calculators`  

#### 2.7.1 Purpose & Scope
Transforms continuous beam geometry from model space (millimetres) to WPF Canvas presentation coordinates (pixels) with:
1. **Aspect Ratio Preservation**: Uniform scaling preventing vertical or horizontal distortion.
2. **WPF Coordinate Inversion**: Inverting the $Z$-axis (upwards in structural space) to $Y$-axis (downwards in WPF Canvas).
3. **Centering & Margin Padding**: Automatic centering of beam within available canvas viewport.
4. **Bidirectional Mapping**: Forward transform (Model $\to$ Screen) for rendering, and Inverse transform (Screen $\to$ Model) for hit-testing, cursor coordinates, and tooltips.

#### 2.7.2 Mathematical Formulation

##### A. Elevation View Coordinate Transformation
- **Input Bounds**:
  - Model Longitudinal Range: $[X_{min}, X_{max}]$, length $L_{model} = X_{max} - X_{min}$.
  - Model Vertical Range: $[Z_{min}, Z_{max}]$, height $H_{model} = Z_{max} - Z_{min}$.
  - Canvas Dimensions: $W_{canvas} \times H_{canvas}$ (pixels).
  - Margin Padding: $M_x, M_y$ (pixels, default $40.0$ px).
- **Available Draw Box**:
  $$W_{draw} = W_{canvas} - 2 \times M_x$$
  $$H_{draw} = H_{canvas} - 2 \times M_y$$
- **Uniform Scale Factor** ($Scale$, pixels per millimetre):
  $$Scale = \min\left(\frac{W_{draw}}{L_{model}}, \frac{H_{draw}}{H_{model}}\right)$$
- **Content Dimensions on Canvas**:
  $$W_{content} = L_{model} \times Scale$$
  $$H_{content} = H_{model} \times Scale$$
- **Centering Offsets**:
  $$OffsetX = M_x + \frac{W_{draw} - W_{content}}{2}$$
  $$OffsetY = M_y + \frac{H_{draw} - H_{content}}{2}$$
  $$Baseline = H_{canvas} - OffsetY$$
- **Forward Mapping Equations**:
  For any model coordinate $(X, Z)$ in millimetres:
  $$X_{canvas} = OffsetX + (X - X_{min}) \times Scale$$
  $$Y_{canvas} = Baseline - (Z - Z_{min}) \times Scale$$
- **Inverse Mapping Equations (Hit-Testing)**:
  For any screen point $(X_{canvas}, Y_{canvas})$ in pixels:
  $$X_{model} = X_{min} + \frac{X_{canvas} - OffsetX}{Scale}$$
  $$Z_{model} = Z_{min} + \frac{Baseline - Y_{canvas}}{Scale}$$

##### B. Cross-Section View Coordinate Transformation
- Model Transverse Range: $[-b/2, +b/2]$, width $b$.
- Model Vertical Range: $[Z_{bot}, Z_{top}]$, height $h$.
- Canvas Dimensions: $W_{sec} \times H_{sec}$.
- Uniform Scale:
  $$Scale_{sec} = \min\left(\frac{W_{sec} - 2 M}{b}, \frac{H_{sec} - 2 M}{h}\right)$$
- Screen Coordinates:
  $$X_{canvas} = \frac{W_{sec}}{2} + Y \times Scale_{sec}$$
  $$Y_{canvas} = Baseline_{sec} - (Z - Z_{bot}) \times Scale_{sec}$$

#### 2.7.3 Immutable Layout Records & API Blueprint
```csharp
namespace HPRebar.Core.BeamRebar.Calculators;

public sealed record BeamCanvasTransform
{
    public double Scale { get; init; }
    public double OffsetX { get; init; }
    public double Baseline { get; init; }
    public double CanvasWidth { get; init; }
    public double CanvasHeight { get; init; }
    public double XMin { get; init; }
    public double ZMin { get; init; }

    public (double ScreenX, double ScreenY) ToScreen(double modelX, double modelZ) =>
        (OffsetX + (modelX - XMin) * Scale, Baseline - (modelZ - ZMin) * Scale);

    public (double ModelX, double ModelZ) ToModel(double screenX, double screenY) =>
        (XMin + (screenX - OffsetX) / Scale, ZMin + (Baseline - screenY) / Scale);
}

public static class BeamCanvasTransformCalculator
{
    public const double DefaultMarginPx = 40.0;

    public static BeamCanvasTransform ComputeElevationTransform(
        double xMinMm,
        double xMaxMm,
        double zMinMm,
        double zMaxMm,
        double canvasWidthPx,
        double canvasHeightPx,
        double marginPx = DefaultMarginPx);

    public static BeamCanvasTransform ComputeSectionTransform(
        double widthMm,
        double heightMm,
        double canvasWidthPx,
        double canvasHeightPx,
        double marginPx = DefaultMarginPx);
}
```

---

## 3. Data Contracts & Model Integration

The calculators operate on the immutable records designed in Milestone M1:

```
┌────────────────────────────────────────────────────────────────────────┐
│                   HPRebar.Core.BeamRebar.Models                        │
│                                                                        │
│  - Point3 (readonly struct: X, Y, Z)                                   │
│  - Vector3 (readonly struct: X, Y, Z)                                  │
│  - Polyline3 (record: IReadOnlyList<Point3> Points)                    │
│  - BeamSpan (record: Index, Name, LengthMm, WidthMm, HeightMm, TopZ)   │
│  - BeamSupportNode (record: Index, CenterX, WidthMm, SupportType)      │
│  - BeamContinuousStack (record: IReadOnlyList<BeamSpan>, SupportNodes) │
│  - BeamBarPolyline (record: BarId, Points, DiameterMm, Layer, Type)    │
│  - StirrupRun (record: Count, Spacing, StartOffset, Length)            │
│  - Enums (SupportType, StirrupDistributionType, BarPositionType)       │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ fed into
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│                HPRebar.Core.BeamRebar.Calculators                      │
│                                                                        │
│  1. Tolerance (1e-9 equality, 1.0mm segment safety, 1e-6 collinear)   │
│  2. BeamStirrupDistributionCalculator (Uniform, 3-zone, Max 1002)      │
│  3. BeamMainBarCalculator (Continuous top/bot polylines, 50% stagger)  │
│  4. BeamAdditionalBarCalculator (L/3, L/4, L/7 cutoffs, 2 layers)      │
│  5. BeamSideBarCalculator (h >= 700mm, s <= 300mm, cross-ties)        │
│  6. BeamSpecialBarCalculator (Cốt treo 50mm, vai bò 45 deg)            │
│  7. BeamCanvasTransformCalculator (Aspect ratio, WPF Y-inversion)      │
└────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Verification & Testing Strategy (xUnit v3 Blueprint for M2)

For Milestone M2, unit tests in `HPRebar.Core.Tests/BeamRebar/` will assert each calculator's behavior against rigorous test matrices:

| Calculator | Key Test Method | Tested Invariant | Assertion Pattern |
|---|---|---|---|
| `Tolerance` | `AreEqualWithinWindow()` | Floating point comparisons | `Assert.True(Tolerance.AreEqual(10.0, 10.0 + 1e-10))` |
| `BeamStirrupDistribution` | `UniformLayoutCentersSlack()` | Symmetrical start offset | `Assert.Equal(expectedOffset, run.StartOffset, 6)` |
| `BeamStirrupDistribution` | `ThreeZoneDenseSparseDense()` | $L/4 - L/2 - L/4$ counts | `Assert.Equal(3, runs.Count)` |
| `BeamStirrupDistribution` | `Exceeding1002Throws()` | `MaxBarPositions = 1002` guardrail | `Assert.Throws<ArgumentOutOfRangeException>(...)` |
| `BeamStirrupDistribution` | `CantileverUniformDense()` | Full dense layout on cantilever | `Assert.Single(runs); Assert.Equal(spec.S1, runs[0].Spacing)` |
| `BeamMainBar` | `PolylineContinuousAcrossSpans()` | Continuous X trajectory | `Assert.Equal(totalLength, polyline.Length, 3)` |
| `BeamMainBar` | `ExteriorHookTurnsDown90Degrees()` | Top bar anchor geometry | `Assert.Equal(Ztop - Lhook, points[0].Z, 3)` |
| `BeamMainBar` | `StaggeredLapSpliceFiftyPercent()` | Group A vs Group B stagger | `Assert.True(Math.Abs(spliceA - spliceB) >= 1.3 * lapLength)` |
| `BeamMainBar` | `CullSubMillimeterSegments()` | No segment $< 1.0$ mm | `Assert.All(segments, len => Assert.True(len >= 1.0))` |
| `BeamAdditionalBar` | `TopBarsExtendOneThirdClearSpan()` | $L_n / 3$ cutoff verification | `Assert.Equal(L1_n / 3, bar.LeftExtension, 3)` |
| `BeamAdditionalBar` | `LayerTwoVerticalClearance()` | $\Delta Z \ge 30$ mm | `Assert.True(Z1 - Z2 >= 30.0)` |
| `BeamAdditionalBar` | `BottomMidspanCutoffsOneSeventh()` | $L_n / 7$ start & end | `Assert.Equal(Xface + Ln / 7, bar.StartX, 3)` |
| `BeamSideBar` | `BeamsUnder700mmReturnZeroSideBars()` | Code trigger threshold | `Assert.Empty(sideBars)` |
| `BeamSideBar` | `DeepBeamSpacesSkinUnder300mm()` | Vertical spacing $\le 300$ mm | `Assert.All(spacings, s => Assert.True(s <= 300.0))` |
| `BeamSpecialBar` | `HangingStirrupsFlankSecondaryBeam()` | Symmetrical flanking | `Assert.Equal(countPerSide * 2, stations.Count)` |
| `BeamSpecialBar` | `DiagonalTieInclines45Degrees()` | $\Delta X == \Delta Z$ | `Assert.Equal(deltaZ, deltaX, 3)` |
| `BeamCanvasTransform` | `PreservesAspectRatioUniformly()` | Screen aspect ratio | `Assert.Equal(scaleX, scaleY, 6)` |
| `BeamCanvasTransform` | `InvertsZAxisToWpfY()` | Top of beam is low screen Y | `Assert.True(screenYTop < screenYBot)` |

---

## 5. Architectural Quality Checklist

- [x] Zero references to `Autodesk.Revit.*`.
- [x] Fully compliant with `netstandard2.0` and C# 9+ via `Polyfill 11.0.1`.
- [x] All coordinates and dimensions strictly in millimetres (`double`).
- [x] Deterministic, stateless static classes with immutable return records.
- [x] Guardrails against Revit crashes: `MaxBarPositions = 1002`, `MinimumSegmentMm = 1.0`.
- [x] Detailing compliant with Vietnamese standard TCVN 5574:2018 and ACI 318.
