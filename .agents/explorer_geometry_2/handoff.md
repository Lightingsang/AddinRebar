# Handoff Report: Mathematical & Domain Geometry Engine for Foundation Rebar (`HPRebar.Core/FoundationRebar/`)

**Author**: `explorer_geometry_2` (Read-only Mathematical & Domain Geometry Explorer)  
**Date**: 2026-09-07  
**Target Architecture**: `HPRebar.Core` (`netstandard2.0`, zero `Autodesk.Revit.*` dependencies)  
**Deliverable Scope**: Domain records, mathematical models, boundary & mesh distribution algorithms, layer elevations, and edge-case guardrails for `Foundation Rebar`.

---

## 1. Observation

### 1.1 Existing Architecture Patterns in `HPRebar.Core`
1. **Target Framework & Dependencies**:
   - Inspection of `HPRebar/HPRebar.Core/HPRebar.Core.csproj` (lines 1–18) demonstrates:
     * `TargetFramework` is `netstandard2.0` with `LangVersion` `latest`.
     * `Nullable` enabled, `ImplicitUsings` disabled.
     * Package reference: `Polyfill` version 11.0.1 (enables C# 9+ `record`, `init`, `required` on `netstandard2.0`).
     * Zero references to `Autodesk.Revit.*`.
2. **Geometric Types in Core**:
   - `HPRebar.Core/BeamRebar/Models/Point3.cs` (lines 1–58) and `Vector3.cs` (lines 1–58) define immutable readonly Cartesian structs in millimetres (`Point3`, `Vector3`) with vector arithmetic (`+`, `-`, `*`, `/`, `Dot`, `Cross`, `Normalize`, `DistanceTo`, `IsAlmostEqualTo`).
   - `HPRebar.Core/BeamRebar/Models/Polyline3.cs` (lines 1–81) provides an ordered sequence of 3D points forming a continuous rebar centerline curve in millimetres, including `Simplify(minSegmentLength = 1.0)` to eliminate micro-segments below Revit's short-curve tolerance (~0.78 mm).
   - `HPRebar.Core/ColumnRebar/Models/Point3.cs` (lines 1–23) defines a local millimetre frame point. Each feature folder in `HPRebar.Core` (`BeamRebar/`, `ColumnRebar/`) maintains its own self-contained models and calculators under explicit PascalCase namespaces.
3. **Existing Testing Conventions in `HPRebar.Core.Tests`**:
   - Inspection of `HPRebar.Core.Tests/BeamRebar/BeamStirrupDistributionCalculatorTests.cs` (lines 1–60) shows:
     * Framework: xUnit v3 (`Microsoft.Testing.Platform` runner pinned in `global.json`).
     * Assertions use exact decimal precision (e.g. `Assert.Equal(expected, actual, precision: 6)`).
     * Tests cover pure calculations: spacing distribution, edge slack centering, zone lengths, boundary rounding, and invalid inputs.
     * Total baseline tests passing: 241 tests (102 column + 139 beam tests).

### 1.2 User Request Constraints (Original Request Follow-up — 2026-09-07T15:37:30Z)
- Domain logic must be segregated into pure C# records and stateless calculators in `HPRebar.Core/FoundationRebar/`:
  1. `FoundationGeometrySnapshot`: Length, Width, Thickness, TopElevation, BottomElevation, Local Coordinate System (Origin, Local X, Local Y, Normal Z), supporting arbitrary rotation in the XY plane.
  2. `FoundationRebarSpec`: Diameters (BX, BY, TX, TY), Spacings (BX, BY, TX, TY), Covers (Top, Bottom, Side), `IsTopMatEnabled`, HookTypes / Anchorage options.
  3. `FoundationBoundaryCalculator`: Effective placement boundary after subtracting side concrete cover ($c_{side}$).
  4. `FoundationMeshCalculator`: Generation of 3D centerline curves/polylines:
     - Bottom Mat Dir X (Layer 1, lowest: $z = Z_{bot} + c_{bot} + d_{BX}/2$).
     - Bottom Mat Dir Y (Layer 2, resting on Layer 1: $z = Z_{bot} + c_{bot} + d_{BX} + d_{BY}/2$).
     - Top Mat Dir Y / Dir X (Upper layers positioned beneath $Z_{top} - c_{top}$).
     - Bar counts: `floor((Length - 2*CoverSide) / Spacing) + 1` or adjusted for equal distribution.
     - Guardrails: Spacing $\le 0 \to$ error; Thickness $< 2 \cdot \text{Cover} + \sum \text{Diameters} \to$ error; non-rectangular/rotated boundaries handled cleanly.

---

## 2. Logic Chain & Mathematical Formulations

### 2.1 Coordinate System & 3D Spatial Transformation

Let the foundation slab be defined in 3D world space (Revit coordinates, measured in millimetres):
- Bottom Face: horizontal planar face at elevation $Z_{bot}$ (Normal vector $(0, 0, -1)$).
- Top Face: horizontal planar face at elevation $Z_{top}$ (Normal vector $(0, 0, 1)$).
- Slab Thickness: $H = Z_{top} - Z_{bot} > 0$.

#### 2.1.1 Orthonormal Local Coordinate Frame
To handle arbitrary orientation in the XY plane (angle $\theta \in [0, 2\pi)$ relative to World X axis):
1. **Datum Origin ($P_0$)**: A selected corner point of the bottom face at $(X_0, Y_0, Z_{bot})$.
2. **Local X Vector ($\vec{u}_X$)**: Unit direction vector along the length of the foundation in the horizontal plane:
   $$\vec{u}_X = (\cos\theta, \; \sin\theta, \; 0), \quad |\vec{u}_X| = 1$$
3. **Normal Z Vector ($\vec{u}_Z$)**: Unit upward normal vector:
   $$\vec{u}_Z = (0, \; 0, \; 1), \quad |\vec{u}_Z| = 1$$
4. **Local Y Vector ($\vec{u}_Y$)**: Transverse horizontal unit vector derived via right-hand cross product:
   $$\vec{u}_Y = \vec{u}_Z \times \vec{u}_X = (-\sin\theta, \; \cos\theta, \; 0), \quad |\vec{u}_Y| = 1$$

Properties guaranteed:
- Orthonormality: $\vec{u}_X \cdot \vec{u}_Y = 0$, $\vec{u}_X \cdot \vec{u}_Z = 0$, $\vec{u}_Y \cdot \vec{u}_Z = 0$.
- Right-handed orientation: $\vec{u}_X \times \vec{u}_Y = \vec{u}_Z$.

#### 2.1.2 Forward and Inverse Coordinate Transformations
Any point in Local Foundation Coordinates $(x_{loc}, y_{loc}, z_{loc}) \in [0, L_X] \times [0, L_Y] \times [0, H]$ transforms to World Coordinates $P_{world} = (X_w, Y_w, Z_w)$ by:
$$P_{world}(x_{loc}, y_{loc}, z_{loc}) = P_0 + x_{loc} \cdot \vec{u}_X + y_{loc} \cdot \vec{u}_Y + z_{loc} \cdot \vec{u}_Z$$

In matrix form:
$$\begin{bmatrix} X_w \\ Y_w \\ Z_w \end{bmatrix} = \begin{bmatrix} X_0 \\ Y_0 \\ Z_{bot} \end{bmatrix} + \begin{bmatrix} u_{X,x} & u_{Y,x} & 0 \\ u_{X,y} & u_{Y,y} & 0 \\ 0 & 0 & 1 \end{bmatrix} \begin{bmatrix} x_{loc} \\ y_{loc} \\ z_{loc} \end{bmatrix}$$

Conversely, given any World point $P_{world}$, its Local coordinates are:
$$\vec{v} = P_{world} - P_0$$
$$x_{loc} = \vec{v} \cdot \vec{u}_X, \quad y_{loc} = \vec{v} \cdot \vec{u}_Y, \quad z_{loc} = \vec{v} \cdot \vec{u}_Z = Z_w - Z_{bot}$$

**Planarity Invariant**:
Since $\vec{u}_Z = (0, 0, 1)$ is strictly vertical, any 3D bar curve generated at constant local $y = y_k$ (for Dir X bars) lies in the vertical plane defined by origin $P_{loc,0} = (0, y_k, 0)$ and normal $\vec{u}_Y$. For any vertex on the bar:
$$(P_{world} - P_{world,0}) \cdot \vec{u}_Y = 0$$
This guarantees 100% exact coplanarity, preventing Revit API `Rebar.CreateFromCurves` from ever throwing `"Curves must be planar"`.

---

### 2.2 Effective Placement Boundary (`FoundationBoundaryCalculator`)

Let the foundation dimensions along Local X and Local Y be $L_X$ and $L_Y$ respectively. Let $c_{side}$ be the lateral concrete cover.

#### 2.2.1 Boundary Extents
The effective boundary for rebar placement in local 2D space is:
$$x_{min} = c_{side}, \quad x_{max} = L_X - c_{side}$$
$$y_{min} = c_{side}, \quad y_{max} = L_Y - c_{side}$$

Effective lengths:
$$L_{eff, X} = x_{max} - x_{min} = L_X - 2 \cdot c_{side}$$
$$L_{eff, Y} = y_{max} - y_{min} = L_Y - 2 \cdot c_{side}$$

#### 2.2.2 Boundary Validation Rules
1. $L_X > 2 \cdot c_{side} \implies L_{eff, X} > 0$
2. $L_Y > 2 \cdot c_{side} \implies L_{eff, Y} > 0$
3. $L_{eff, X} \ge 10.0\text{ mm}$ and $L_{eff, Y} \ge 10.0\text{ mm}$ (exceeds Revit short-curve limit by a wide safety factor).

---

### 2.3 Layer Elevations & Physical Stacking (`FoundationMeshCalculator`)

A foundation slab typically contains up to 4 orthogonal layers of reinforcement:
- Bottom Mat (Layers 1 & 2)
- Top Mat (Layers 3 & 4, when `IsTopMatEnabled == true`)

Let $Z_{bot}$ and $Z_{top}$ be the world elevations of the bottom and top slab faces ($H = Z_{top} - Z_{bot}$).

```
  Top Face -----------------------------------------------------------  Z_top = Z_bot + H
           |  CoverTop (c_top)
           |  [Layer 4 - Outer Top Bar]   diameter d_T4, centerline Z_c,T4
           |  [Layer 3 - Inner Top Bar]   diameter d_T3, centerline Z_c,T3
           |
           |  Vertical Clearance Gap: Delta_Z_clear
           |
           |  [Layer 2 - Inner Bottom Bar] diameter d_B2, centerline Z_c,B2
           |  [Layer 1 - Outer Bottom Bar] diameter d_B1, centerline Z_c,B1
           |  CoverBottom (c_bot)
  Bottom Face --------------------------------------------------------  Z_bot (local z = 0)
```

#### 2.3.1 Bottom Mat Elevations
- **Layer 1: Bottom Direction X** (outermost bottom layer):
  * Physical bottom of bar: $z_{bottom, B1} = c_{bot}$
  * Centerline elevation (local $z$ from bottom face):
    $$z_{loc, B1} = c_{bot} + \frac{d_{BX}}{2}$$
  * Centerline elevation (world $Z$):
    $$Z_{c, B1} = Z_{bot} + c_{bot} + \frac{d_{BX}}{2}$$
  * Physical top of bar: $z_{top, B1} = c_{bot} + d_{BX}$

- **Layer 2: Bottom Direction Y** (resting directly on Layer 1):
  * Physical bottom of bar: $z_{bottom, B2} = z_{top, B1} = c_{bot} + d_{BX}$
  * Centerline elevation (local $z$ from bottom face):
    $$z_{loc, B2} = c_{bot} + d_{BX} + \frac{d_{BY}}{2}$$
  * Centerline elevation (world $Z$):
    $$Z_{c, B2} = Z_{bot} + c_{bot} + d_{BX} + \frac{d_{BY}}{2}$$
  * Physical top of bar: $z_{top, B2} = c_{bot} + d_{BX} + d_{BY}$

#### 2.3.2 Top Mat Elevations (When Enabled)
The layer ordering is governed by `TopMatLayerOrder`:
- **Default Order (`DirYOuterDirXInner`)**:
  Layer 4 is Top Dir Y (uppermost, directly under $c_{top}$). Layer 3 is Top Dir X (hanging directly beneath Layer 4).
  * Centerline of Layer 4 (Top Dir Y):
    $$z_{loc, T4} = H - c_{top} - \frac{d_{TY}}{2}$$
    $$Z_{c, T4} = Z_{top} - c_{top} - \frac{d_{TY}}{2}$$
  * Centerline of Layer 3 (Top Dir X):
    $$z_{loc, T3} = H - c_{top} - d_{TY} - \frac{d_{TX}}{2}$$
    $$Z_{c, T3} = Z_{top} - c_{top} - d_{TY} - \frac{d_{TX}}{2}$$
  * Physical bottom of Top Mat:
    $$z_{bottom, TopMat} = H - c_{top} - d_{TY} - d_{TX}$$

- **Alternative Order (`DirXOuterDirYInner`)**:
  Layer 4 is Top Dir X (uppermost). Layer 3 is Top Dir Y.
  * Centerline of Layer 4 (Top Dir X):
    $$z_{loc, T4} = H - c_{top} - \frac{d_{TX}}{2}$$
    $$Z_{c, T4} = Z_{top} - c_{top} - \frac{d_{TX}}{2}$$
  * Centerline of Layer 3 (Top Dir Y):
    $$z_{loc, T3} = H - c_{top} - d_{TX} - \frac{d_{TY}}{2}$$
    $$Z_{c, T3} = Z_{top} - c_{top} - d_{TX} - \frac{d_{TY}}{2}$$
  * Physical bottom of Top Mat:
    $$z_{bottom, TopMat} = H - c_{top} - d_{TX} - d_{TY}$$

#### 2.3.3 Mat Interference & Minimum Thickness Guardrail
The physical vertical clearance gap between the top surface of the bottom mat and the bottom surface of the top mat is:
$$\Delta Z_{clear} = z_{bottom, TopMat} - z_{top, B2}$$
$$\Delta Z_{clear} = H - \left(c_{bot} + c_{top} + d_{BX} + d_{BY} + d_{TX} + d_{TY}\right)$$

**Mandatory Guardrails**:
1. If `IsTopMatEnabled == true`:
   $$H_{min} = c_{bot} + c_{top} + d_{BX} + d_{BY} + d_{TX} + d_{TY}$$
   If $H < H_{min}$, then $\Delta Z_{clear} < 0$. The bars physically collide in 3D space $\implies$ **Fail Validation**.
2. If `IsTopMatEnabled == false`:
   $$H_{min} = c_{bot} + c_{top} + d_{BX} + d_{BY}$$
   If $H < H_{min}$, bottom mat exceeds slab thickness $\implies$ **Fail Validation**.

---

### 2.4 Bar Count & Distribution Calculations

Let $D$ be the distribution length across which bars are spaced:
- For Dir X bars (bars run along X): distribution is along Local Y, $D = L_{eff, Y} = L_Y - 2 \cdot c_{side}$.
- For Dir Y bars (bars run along Y): distribution is along Local X, $D = L_{eff, X} = L_X - 2 \cdot c_{side}$.

Let $s$ be the nominal spacing ($s > 0$).

#### Distribution Modes:

1. **Mode 1: `EqualSpacing` (Uniform Adjusted Spacing $\le s$)**:
   In accordance with structural detailing codes, bars are placed at the outer boundary edges, and the intermediate span is subdivided into an integer number of equal intervals not exceeding $s$:
   $$N_{spaces} = \max\left(1, \; \left\lceil \frac{D}{s} \right\rceil\right)$$
   $$s_{actual} = \frac{D}{N_{spaces}} \le s$$
   $$\text{BarCount} = N_{spaces} + 1$$
   Positions along distribution axis $t \in [t_{min}, t_{max}]$:
   $$t_k = t_{min} + k \cdot s_{actual}, \quad k = 0, 1, \dots, N_{spaces}$$
   Edge conditions: $t_0 = t_{min} = c_{side}$, $t_{last} = t_{max} = L - c_{side}$.

2. **Mode 2: `FixedSpacingCentered` (Fixed Nominal Spacing with Centered Slack)**:
   Bars are placed at exact nominal spacing $s$. Leftover slack is split equally between both ends:
   $$N_{spaces} = \left\lfloor \frac{D}{s} \right\rfloor$$
   $$\text{BarCount} = N_{spaces} + 1$$
   $$\text{slack} = D - N_{spaces} \cdot s \quad (0 \le \text{slack} < s)$$
   $$\delta = \frac{\text{slack}}{2}$$
   Positions:
   $$t_k = t_{min} + \delta + k \cdot s, \quad k = 0, 1, \dots, N_{spaces}$$
   Edge gaps: $t_0 - t_{min} = \delta$, and $t_{max} - t_{last} = \delta$.

3. **Mode 3: `FixedSpacingFromStart` (Prompt Formula: `floor(D / s) + 1`)**:
   $$N_{spaces} = \left\lfloor \frac{D}{s} \right\rfloor$$
   $$\text{BarCount} = N_{spaces} + 1$$
   Positions:
   $$t_k = t_{min} + k \cdot s, \quad k = 0, 1, \dots, N_{spaces}$$
   Leftover slack remains entirely at the end edge: $t_{max} - t_{last} = D - N_{spaces} \cdot s$.

---

### 2.5 Rebar 3D Curve Geometry & Anchorage Hooks

For each bar, a 3D polyline (ordered list of `Point3`) is computed in Local Coordinates, then mapped to World Coordinates.

#### 2.5.1 Straight Bars (`HookType == None`)
For a straight bar spanning from $u_{start}$ to $u_{end}$ along axis $U$, at coordinate $v_k$ on axis $V$, and elevation $z_{layer}$:
- Point 0: $(u_{start}, v_k, z_{layer})$
- Point 1: $(u_{end}, v_k, z_{layer})$
Polyline contains 2 vertices (1 line segment).

#### 2.5.2 Hooked Bars (`HookType == Hook90` - Standard 90° L-Hooks)
In reinforced concrete foundation slabs:
- **Bottom Mat**: 90° hooks bend **UPWARDS** (+Z direction) into the concrete footing core to develop tension anchorage.
  Hook vector in local/world: $(0, 0, +L_{hook})$.
  Available vertical rise before hitting top cover:
  $$L_{hook, max} = H - z_{layer} - c_{top}$$
  Effective hook length: $h_{eff} = \min(L_{hook}, L_{hook, max})$.
  Polyline vertices (4 vertices, 3 segments):
  * $P_0 = (u_{start}, \; v_k, \; z_{layer} + h_{eff})$ (Start Hook Tip)
  * $P_1 = (u_{start}, \; v_k, \; z_{layer})$ (Start Bend Corner)
  * $P_2 = (u_{end}, \; v_k, \; z_{layer})$ (End Bend Corner)
  * $P_3 = (u_{end}, \; v_k, \; z_{layer} + h_{eff})$ (End Hook Tip)

- **Top Mat**: 90° hooks bend **DOWNWARDS** (-Z direction) into the concrete footing core.
  Hook vector in local/world: $(0, 0, -L_{hook})$.
  Available vertical drop before hitting bottom cover:
  $$L_{hook, max} = z_{layer} - c_{bot}$$
  Effective hook length: $h_{eff} = \min(L_{hook}, L_{hook, max})$.
  Polyline vertices (4 vertices, 3 segments):
  * $P_0 = (u_{start}, \; v_k, \; z_{layer} - h_{eff})$ (Start Hook Tip)
  * $P_1 = (u_{start}, \; v_k, \; z_{layer})$ (Start Bend Corner)
  * $P_2 = (u_{end}, \; v_k, \; z_{layer})$ (End Bend Corner)
  * $P_3 = (u_{end}, \; v_k, \; z_{layer} - h_{eff})$ (End Hook Tip)

---

### 2.6 Edge Cases & Guardrails Matrix

| # | Guardrail / Edge Case | Condition | Mathematical Check | System Behavior & Error Contract |
|---|-----------------------|-----------|--------------------|----------------------------------|
| 1 | Non-positive Spacing | User enters $s \le 0$ for any layer. | $s_{BX} \le 0 \lor s_{BY} \le 0 \lor (IsTopMat \land (s_{TX} \le 0 \lor s_{TY} \le 0))$ | **ValidationError**: `"Spacing for {Layer} must be greater than 0 mm. Received: {s} mm."` |
| 2 | Insufficient Slab Thickness (Interference) | Foundation thickness is too thin to house rebar cages. | $H < c_{bot} + c_{top} + \sum d_i$ | **ValidationError**: `"Foundation thickness ({H} mm) is less than required minimum ({H_min} mm) to accommodate covers and rebar layers."` |
| 3 | Side Cover Exceeds Boundary | Side cover is larger than half the slab dimension. | $2 \cdot c_{side} \ge L_X \lor 2 \cdot c_{side} \ge L_Y$ | **ValidationError**: `"Foundation dimension ({Dim} mm) is less than or equal to 2 * Side Cover ({2*c} mm)."` |
| 4 | Negative Cover | Negative cover entered. | $c_{top} < 0 \lor c_{bot} < 0 \lor c_{side} < 0$ | **ValidationError**: `"Concrete cover cannot be negative."` |
| 5 | Non-orthonormal Snapshot Vectors | Local X and Local Y are not perpendicular or not unit length. | $\||\vec{u}_X| - 1\| > 10^{-6} \lor |\vec{u}_X \cdot \vec{u}_Y| > 10^{-6}$ | Snapshot factory automatically computes $\vec{u}_Y = \vec{u}_Z \times \vec{u}_X$ and normalizes vectors; validation fails if vectors are degenerate ($|\vec{u}| < 10^{-6}$). |
| 6 | Curve Below Revit Short-Curve Tolerance | Any curve segment $< 1.0$ mm (Revit tolerance ~0.78 mm). | Segment length $< 1.0$ mm | Polyline `Simplify(1.0)` merges points closer than 1.0 mm; if overall bar length $< 10.0$ mm, bar is rejected. |
| 7 | Hook Height Exceeding Slab Depth | Hook length requested is larger than available slab interior. | $L_{hook} > H - c_{bot} - c_{top}$ | Hook length is automatically clamped to $L_{hook, eff} = \max(0, H - c_{bot} - c_{top} - d)$ to prevent bar pierce through opposing face. |
| 8 | Arbitrary Rotation in XY Plane | Foundation is rotated by arbitrary angle $\theta$. | Any $\theta \in [0, 2\pi)$ | Exact transformation matrix preserves distances, orthogonal angle, and vertical hook segments without numerical drift. |
| 9 | Single Bar in Span ($D < s$) | Distribution span is smaller than nominal spacing. | $D < s \land D > 0$ | In `FixedSpacingCentered`, places 1 bar centered at $(t_{min} + t_{max})/2$. In `EqualSpacing`, places 2 bars at $t_{min}$ and $t_{max}$. |
| 10 | Non-rectangular / Polygonal Slabs | Floor boundary has cutouts, L-shape, or $>4$ vertices. | Revit face has $>4$ edges or non-right angles. | `FoundationGeometrySnapshot` encapsulates oriented bounding box (OBB). `FoundationRebarValidator` verifies rectangularity or applies OBB clipping. |

---

## 3. Class Contract Design (`HPRebar.Core/FoundationRebar/`)

Below are the exact C# signatures proposed for `HPRebar.Core`:

### 3.1 `HPRebar.Core.FoundationRebar.Models`

#### Enums:
```csharp
namespace HPRebar.Core.FoundationRebar.Models;

public enum FoundationBarRole
{
    BottomX = 1,
    BottomY = 2,
    TopX = 3,
    TopY = 4
}

public enum RebarHookType
{
    None = 0,
    Hook90 = 1,
    Hook135 = 2,
    Hook180 = 3
}

public enum RebarDistributionMode
{
    EqualSpacing = 0,        // Max spacing constraint, equal subdivisions across bounds
    FixedSpacingCentered = 1,// Exact nominal spacing, symmetric leftover margins
    FixedSpacingFromStart = 2// Exact nominal spacing from start edge (floor(D/s)+1)
}

public enum TopMatLayerOrder
{
    DirYOuterDirXInner = 0,  // Layer 4 = Top Y (outermost), Layer 3 = Top X (inner)
    DirXOuterDirYInner = 1   // Layer 4 = Top X (outermost), Layer 3 = Top Y (inner)
}
```

#### `Point3.cs` & `Vector3.cs`:
```csharp
namespace HPRebar.Core.FoundationRebar.Models;

public readonly struct Point3 : System.IEquatable<Point3>
{
    public Point3(double x, double y, double z) { X = x; Y = y; Z = z; }
    public double X { get; }
    public double Y { get; }
    public double Z { get; }
    public static readonly Point3 Zero = new(0, 0, 0);

    public static Point3 operator +(Point3 p, Vector3 v) => new(p.X + v.X, p.Y + v.Y, p.Z + v.Z);
    public static Point3 operator -(Point3 p, Vector3 v) => new(p.X - v.X, p.Y - v.Y, p.Z - v.Z);
    public static Vector3 operator -(Point3 a, Point3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public double DistanceTo(Point3 other)
    {
        double dx = X - other.X, dy = Y - other.Y, dz = Z - other.Z;
        return System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public bool IsAlmostEqualTo(Point3 other, double tolerance = 1e-6) =>
        System.Math.Abs(X - other.X) <= tolerance &&
        System.Math.Abs(Y - other.Y) <= tolerance &&
        System.Math.Abs(Z - other.Z) <= tolerance;

    public bool Equals(Point3 other) => IsAlmostEqualTo(other);
    public override bool Equals(object? obj) => obj is Point3 o && Equals(o);
    public override int GetHashCode() => (X, Y, Z).GetHashCode();
    public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
}

public readonly struct Vector3 : System.IEquatable<Vector3>
{
    public Vector3(double x, double y, double z) { X = x; Y = y; Z = z; }
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public static readonly Vector3 Zero = new(0, 0, 0);
    public static readonly Vector3 UnitX = new(1, 0, 0);
    public static readonly Vector3 UnitY = new(0, 1, 0);
    public static readonly Vector3 UnitZ = new(0, 0, 1);

    public double Length => System.Math.Sqrt(X * X + Y * Y + Z * Z);

    public Vector3 Normalize()
    {
        double len = Length;
        return len < 1e-9 ? Zero : new Vector3(X / len, Y / len, Z / len);
    }

    public double Dot(Vector3 other) => X * other.X + Y * other.Y + Z * other.Z;

    public Vector3 Cross(Vector3 other) => new(
        Y * other.Z - Z * other.Y,
        Z * other.X - X * other.Z,
        X * other.Y - Y * other.X);

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3 operator *(Vector3 v, double s) => new(v.X * s, v.Y * s, v.Z * s);
    public static Vector3 operator /(Vector3 v, double s) => new(v.X / s, v.Y / s, v.Z / s);

    public bool Equals(Vector3 other) =>
        System.Math.Abs(X - other.X) <= 1e-6 &&
        System.Math.Abs(Y - other.Y) <= 1e-6 &&
        System.Math.Abs(Z - other.Z) <= 1e-6;
    public override bool Equals(object? obj) => obj is Vector3 o && Equals(o);
    public override int GetHashCode() => (X, Y, Z).GetHashCode();
    public override string ToString() => $"[{X:F3}, {Y:F3}, {Z:F3}]";
}
```

#### `FoundationGeometrySnapshot.cs`:
```csharp
namespace HPRebar.Core.FoundationRebar.Models;

public sealed record FoundationGeometrySnapshot
{
    /// <summary>Origin point in world coordinates at bottom-left corner of bottom face (mm).</summary>
    public Point3 Origin { get; init; }

    /// <summary>Unit direction vector along local X axis (horizontal length direction).</summary>
    public Vector3 LocalX { get; init; } = Vector3.UnitX;

    /// <summary>Unit direction vector along local Y axis (horizontal width direction).</summary>
    public Vector3 LocalY { get; init; } = Vector3.UnitY;

    /// <summary>Unit upward normal vector (typically (0, 0, 1)).</summary>
    public Vector3 NormalZ { get; init; } = Vector3.UnitZ;

    /// <summary>Dimension along Local X in millimetres.</summary>
    public double Length { get; init; }

    /// <summary>Dimension along Local Y in millimetres.</summary>
    public double Width { get; init; }

    /// <summary>Thickness/depth of foundation slab in millimetres.</summary>
    public double Thickness { get; init; }

    /// <summary>Elevation of top face in world millimetres.</summary>
    public double TopElevation { get; init; }

    /// <summary>Elevation of bottom face in world millimetres.</summary>
    public double BottomElevation { get; init; }

    /// <summary>Transforms local coordinates (x, y, z from bottom face) to world 3D point.</summary>
    public Point3 ToWorld(double localX, double localY, double localZ) =>
        Origin + (LocalX * localX) + (LocalY * localY) + (NormalZ * localZ);

    /// <summary>Transforms a world 3D point into the local foundation frame.</summary>
    public Point3 ToLocal(Point3 worldPoint)
    {
        Vector3 v = worldPoint - Origin;
        return new Point3(v.Dot(LocalX), v.Dot(LocalY), v.Dot(NormalZ));
    }

    /// <summary>Factory to construct an axis-aligned snapshot.</summary>
    public static FoundationGeometrySnapshot CreateAxisAligned(
        Point3 minPoint,
        double length,
        double width,
        double thickness)
    {
        return new FoundationGeometrySnapshot
        {
            Origin = minPoint,
            LocalX = Vector3.UnitX,
            LocalY = Vector3.UnitY,
            NormalZ = Vector3.UnitZ,
            Length = length,
            Width = width,
            Thickness = thickness,
            BottomElevation = minPoint.Z,
            TopElevation = minPoint.Z + thickness
        };
    }

    /// <summary>Factory to construct an oriented snapshot from origin, angle in degrees, and dimensions.</summary>
    public static FoundationGeometrySnapshot CreateOriented(
        Point3 origin,
        double angleDegrees,
        double length,
        double width,
        double thickness)
    {
        double rad = angleDegrees * (System.Math.PI / 180.0);
        var ux = new Vector3(System.Math.Cos(rad), System.Math.Sin(rad), 0).Normalize();
        var uy = new Vector3(-System.Math.Sin(rad), System.Math.Cos(rad), 0).Normalize();
        return new FoundationGeometrySnapshot
        {
            Origin = origin,
            LocalX = ux,
            LocalY = uy,
            NormalZ = Vector3.UnitZ,
            Length = length,
            Width = width,
            Thickness = thickness,
            BottomElevation = origin.Z,
            TopElevation = origin.Z + thickness
        };
    }
}
```

#### `FoundationRebarSpec.cs`:
```csharp
namespace HPRebar.Core.FoundationRebar.Models;

public sealed record FoundationRebarSpec
{
    // Diameters (mm)
    public double DiameterBottomX { get; init; } = 16.0;
    public double DiameterBottomY { get; init; } = 16.0;
    public double DiameterTopX { get; init; } = 12.0;
    public double DiameterTopY { get; init; } = 12.0;

    // Spacings (mm)
    public double SpacingBottomX { get; init; } = 150.0;
    public double SpacingBottomY { get; init; } = 150.0;
    public double SpacingTopX { get; init; } = 200.0;
    public double SpacingTopY { get; init; } = 200.0;

    // Covers (mm)
    public double CoverTop { get; init; } = 50.0;
    public double CoverBottom { get; init; } = 50.0;
    public double CoverSide { get; init; } = 50.0;

    // Mat toggles
    public bool IsBottomMatEnabled { get; init; } = true;
    public bool IsTopMatEnabled { get; init; } = true;

    // Hooks
    public RebarHookType HookTypeBottomX { get; init; } = RebarHookType.None;
    public RebarHookType HookTypeBottomY { get; init; } = RebarHookType.None;
    public RebarHookType HookTypeTopX { get; init; } = RebarHookType.None;
    public RebarHookType HookTypeTopY { get; init; } = RebarHookType.None;

    public double HookLengthBottomX { get; init; } = 0.0; // 0 = default (15*d)
    public double HookLengthBottomY { get; init; } = 0.0;
    public double HookLengthTopX { get; init; } = 0.0;
    public double HookLengthTopY { get; init; } = 0.0;

    // Distribution & Layer ordering
    public RebarDistributionMode DistributionMode { get; init; } = RebarDistributionMode.EqualSpacing;
    public TopMatLayerOrder TopMatOrder { get; init; } = TopMatLayerOrder.DirYOuterDirXInner;
}
```

#### `FoundationBar.cs` & `FoundationMeshResult.cs`:
```csharp
namespace HPRebar.Core.FoundationRebar.Models;

public sealed record FoundationBar
{
    public int BarIndex { get; init; }
    public FoundationBarRole Role { get; init; }
    public int Layer { get; init; }
    public double Diameter { get; init; }
    public Polyline3 LocalPolyline { get; init; } = new();
    public Polyline3 WorldPolyline { get; init; } = new();
    public double Length => LocalPolyline.TotalLength;
    public RebarHookType HookType { get; init; }
    public double HookLength { get; init; }
}

public sealed record FoundationMeshResult
{
    public IReadOnlyList<FoundationBar> BottomXBars { get; init; } = System.Array.Empty<FoundationBar>();
    public IReadOnlyList<FoundationBar> BottomYBars { get; init; } = System.Array.Empty<FoundationBar>();
    public IReadOnlyList<FoundationBar> TopXBars { get; init; } = System.Array.Empty<FoundationBar>();
    public IReadOnlyList<FoundationBar> TopYBars { get; init; } = System.Array.Empty<FoundationBar>();

    public IReadOnlyList<FoundationBar> AllBars
    {
        get
        {
            var list = new System.Collections.Generic.List<FoundationBar>(
                BottomXBars.Count + BottomYBars.Count + TopXBars.Count + TopYBars.Count);
            list.AddRange(BottomXBars);
            list.AddRange(BottomYBars);
            list.AddRange(TopXBars);
            list.AddRange(TopYBars);
            return list;
        }
    }

    public int TotalBarCount => AllBars.Count;
    public double TotalLengthMm
    {
        get
        {
            double sum = 0;
            foreach (var bar in AllBars) sum += bar.Length;
            return sum;
        }
    }

    public double TotalWeightKg
    {
        get
        {
            double sum = 0;
            foreach (var bar in AllBars)
            {
                // Nominal weight: w = 0.006165 * d^2 * L (m) = 0.006165 * d^2 * L_mm / 1000
                sum += 0.006165 * bar.Diameter * bar.Diameter * (bar.Length / 1000.0);
            }
            return sum;
        }
    }
}
```

---

### 3.2 `HPRebar.Core.FoundationRebar.Calculators`

#### `FoundationBoundaryCalculator.cs`:
```csharp
namespace HPRebar.Core.FoundationRebar.Calculators;

public static class FoundationBoundaryCalculator
{
    public static (double xMin, double xMax, double yMin, double yMax) ComputeEffectiveBoundary(
        FoundationGeometrySnapshot geom,
        double sideCover)
    {
        return (sideCover, geom.Length - sideCover, sideCover, geom.Width - sideCover);
    }

    public static (bool isValid, string? error) ValidateBoundary(
        FoundationGeometrySnapshot geom,
        double sideCover)
    {
        if (sideCover < 0)
            return (false, "Side cover cannot be negative.");
        if (geom.Length <= 2 * sideCover)
            return (false, $"Foundation Length ({geom.Length:F1} mm) must be greater than 2 * Side Cover ({2 * sideCover:F1} mm).");
        if (geom.Width <= 2 * sideCover)
            return (false, $"Foundation Width ({geom.Width:F1} mm) must be greater than 2 * Side Cover ({2 * sideCover:F1} mm).");

        return (true, null);
    }
}
```

#### `FoundationMeshCalculator.cs`:
```csharp
namespace HPRebar.Core.FoundationRebar.Calculators;

public static class FoundationMeshCalculator
{
    public static (bool isValid, string? error) Validate(
        FoundationGeometrySnapshot geom,
        FoundationRebarSpec spec)
    {
        // 1. Spacing checks
        if (spec.IsBottomMatEnabled)
        {
            if (spec.SpacingBottomX <= 0) return (false, "Bottom X spacing must be positive.");
            if (spec.SpacingBottomY <= 0) return (false, "Bottom Y spacing must be positive.");
            if (spec.DiameterBottomX <= 0) return (false, "Bottom X diameter must be positive.");
            if (spec.DiameterBottomY <= 0) return (false, "Bottom Y diameter must be positive.");
        }

        if (spec.IsTopMatEnabled)
        {
            if (spec.SpacingTopX <= 0) return (false, "Top X spacing must be positive.");
            if (spec.SpacingTopY <= 0) return (false, "Top Y spacing must be positive.");
            if (spec.DiameterTopX <= 0) return (false, "Top X diameter must be positive.");
            if (spec.DiameterTopY <= 0) return (false, "Top Y diameter must be positive.");
        }

        // 2. Cover checks
        if (spec.CoverTop < 0 || spec.CoverBottom < 0 || spec.CoverSide < 0)
            return (false, "Concrete covers cannot be negative.");

        // 3. Boundary check
        var (boundValid, boundError) = FoundationBoundaryCalculator.ValidateBoundary(geom, spec.CoverSide);
        if (!boundValid) return (false, boundError);

        // 4. Thickness check
        double requiredThickness = spec.CoverBottom + spec.CoverTop;
        if (spec.IsBottomMatEnabled)
            requiredThickness += spec.DiameterBottomX + spec.DiameterBottomY;
        if (spec.IsTopMatEnabled)
            requiredThickness += spec.DiameterTopX + spec.DiameterTopY;

        if (geom.Thickness < requiredThickness)
        {
            return (false, $"Foundation thickness ({geom.Thickness:F1} mm) is insufficient. " +
                           $"Minimum required thickness is {requiredThickness:F1} mm " +
                           $"(Covers: {spec.CoverBottom + spec.CoverTop:F1} mm, Rebar diameters: {requiredThickness - spec.CoverBottom - spec.CoverTop:F1} mm).");
        }

        return (true, null);
    }

    public static IReadOnlyList<double> CalculateBarPositions(
        double start,
        double end,
        double nominalSpacing,
        RebarDistributionMode mode)
    {
        double span = end - start;
        if (span <= 0 || nominalSpacing <= 0) return System.Array.Empty<double>();

        var positions = new System.Collections.Generic.List<double>();

        switch (mode)
        {
            case RebarDistributionMode.EqualSpacing:
            {
                int spaces = System.Math.Max(1, (int)System.Math.Ceiling(span / nominalSpacing));
                double actualSpacing = span / spaces;
                for (int i = 0; i <= spaces; i++)
                    positions.Add(start + i * actualSpacing);
                break;
            }

            case RebarDistributionMode.FixedSpacingCentered:
            {
                int spaces = (int)System.Math.Floor(span / nominalSpacing);
                if (spaces == 0)
                {
                    // Span smaller than spacing: place 1 centered bar
                    positions.Add(start + span / 2.0);
                }
                else
                {
                    double slack = span - (spaces * nominalSpacing);
                    double startOffset = start + (slack / 2.0);
                    for (int i = 0; i <= spaces; i++)
                        positions.Add(startOffset + i * nominalSpacing);
                }
                break;
            }

            case RebarDistributionMode.FixedSpacingFromStart:
            {
                int spaces = (int)System.Math.Floor(span / nominalSpacing);
                for (int i = 0; i <= spaces; i++)
                    positions.Add(start + i * nominalSpacing);
                break;
            }
        }

        return positions;
    }

    public static FoundationMeshResult CalculateMesh(
        FoundationGeometrySnapshot geom,
        FoundationRebarSpec spec)
    {
        var (isValid, error) = Validate(geom, spec);
        if (!isValid)
            throw new System.InvalidOperationException(error);

        var (xMin, xMax, yMin, yMax) = FoundationBoundaryCalculator.ComputeEffectiveBoundary(geom, spec.CoverSide);

        var bottomXList = new System.Collections.Generic.List<FoundationBar>();
        var bottomYList = new System.Collections.Generic.List<FoundationBar>();
        var topXList = new System.Collections.Generic.List<FoundationBar>();
        var topYList = new System.Collections.Generic.List<FoundationBar>();

        int barIndexCounter = 1;

        // --- Bottom Mat ---
        if (spec.IsBottomMatEnabled)
        {
            // Layer 1: Bottom Dir X (runs along X, distributed along Y)
            double zLocB1 = spec.CoverBottom + (spec.DiameterBottomX / 2.0);
            var yPositionsB1 = CalculateBarPositions(yMin, yMax, spec.SpacingBottomX, spec.DistributionMode);
            double hookLenB1 = spec.HookTypeBottomX != RebarHookType.None
                ? (spec.HookLengthBottomX > 0 ? spec.HookLengthBottomX : 15.0 * spec.DiameterBottomX)
                : 0.0;
            // Clamp hook to available top clearance
            hookLenB1 = System.Math.Min(hookLenB1, System.Math.Max(0.0, geom.Thickness - zLocB1 - spec.CoverTop));

            for (int i = 0; i < yPositionsB1.Count; i++)
            {
                double y = yPositionsB1[i];
                var (localPoly, worldPoly) = BuildBarPolyline(
                    geom, isDirX: true, coordAlong: xMin, coordEnd: xMax,
                    transverseCoord: y, localZ: zLocB1,
                    hookType: spec.HookTypeBottomX, hookLength: hookLenB1, isHookUp: true);

                bottomXList.Add(new FoundationBar
                {
                    BarIndex = barIndexCounter++,
                    Role = FoundationBarRole.BottomX,
                    Layer = 1,
                    Diameter = spec.DiameterBottomX,
                    LocalPolyline = localPoly,
                    WorldPolyline = worldPoly,
                    HookType = spec.HookTypeBottomX,
                    HookLength = hookLenB1
                });
            }

            // Layer 2: Bottom Dir Y (runs along Y, distributed along X)
            double zLocB2 = spec.CoverBottom + spec.DiameterBottomX + (spec.DiameterBottomY / 2.0);
            var xPositionsB2 = CalculateBarPositions(xMin, xMax, spec.SpacingBottomY, spec.DistributionMode);
            double hookLenB2 = spec.HookTypeBottomY != RebarHookType.None
                ? (spec.HookLengthBottomY > 0 ? spec.HookLengthBottomY : 15.0 * spec.DiameterBottomY)
                : 0.0;
            hookLenB2 = System.Math.Min(hookLenB2, System.Math.Max(0.0, geom.Thickness - zLocB2 - spec.CoverTop));

            for (int i = 0; i < xPositionsB2.Count; i++)
            {
                double x = xPositionsB2[i];
                var (localPoly, worldPoly) = BuildBarPolyline(
                    geom, isDirX: false, coordAlong: yMin, coordEnd: yMax,
                    transverseCoord: x, localZ: zLocB2,
                    hookType: spec.HookTypeBottomY, hookLength: hookLenB2, isHookUp: true);

                bottomYList.Add(new FoundationBar
                {
                    BarIndex = barIndexCounter++,
                    Role = FoundationBarRole.BottomY,
                    Layer = 2,
                    Diameter = spec.DiameterBottomY,
                    LocalPolyline = localPoly,
                    WorldPolyline = worldPoly,
                    HookType = spec.HookTypeBottomY,
                    HookLength = hookLenB2
                });
            }
        }

        // --- Top Mat ---
        if (spec.IsTopMatEnabled)
        {
            double zLocTopOuter, zLocTopInner;
            if (spec.TopMatOrder == TopMatLayerOrder.DirYOuterDirXInner)
            {
                // Layer 4 = Top Y (outermost), Layer 3 = Top X (inner)
                zLocTopOuter = geom.Thickness - spec.CoverTop - (spec.DiameterTopY / 2.0);
                zLocTopInner = geom.Thickness - spec.CoverTop - spec.DiameterTopY - (spec.DiameterTopX / 2.0);

                // Layer 3: Top X (runs along X, distributed along Y)
                var yPositionsT3 = CalculateBarPositions(yMin, yMax, spec.SpacingTopX, spec.DistributionMode);
                double hookLenT3 = spec.HookTypeTopX != RebarHookType.None
                    ? (spec.HookLengthTopX > 0 ? spec.HookLengthTopX : 15.0 * spec.DiameterTopX)
                    : 0.0;
                hookLenT3 = System.Math.Min(hookLenT3, System.Math.Max(0.0, zLocTopInner - spec.CoverBottom));

                for (int i = 0; i < yPositionsT3.Count; i++)
                {
                    double y = yPositionsT3[i];
                    var (localPoly, worldPoly) = BuildBarPolyline(
                        geom, isDirX: true, coordAlong: xMin, coordEnd: xMax,
                        transverseCoord: y, localZ: zLocTopInner,
                        hookType: spec.HookTypeTopX, hookLength: hookLenT3, isHookUp: false);

                    topXList.Add(new FoundationBar
                    {
                        BarIndex = barIndexCounter++,
                        Role = FoundationBarRole.TopX,
                        Layer = 3,
                        Diameter = spec.DiameterTopX,
                        LocalPolyline = localPoly,
                        WorldPolyline = worldPoly,
                        HookType = spec.HookTypeTopX,
                        HookLength = hookLenT3
                    });
                }

                // Layer 4: Top Y (runs along Y, distributed along X)
                var xPositionsT4 = CalculateBarPositions(xMin, xMax, spec.SpacingTopY, spec.DistributionMode);
                double hookLenT4 = spec.HookTypeTopY != RebarHookType.None
                    ? (spec.HookLengthTopY > 0 ? spec.HookLengthTopY : 15.0 * spec.DiameterTopY)
                    : 0.0;
                hookLenT4 = System.Math.Min(hookLenT4, System.Math.Max(0.0, zLocTopOuter - spec.CoverBottom));

                for (int i = 0; i < xPositionsT4.Count; i++)
                {
                    double x = xPositionsT4[i];
                    var (localPoly, worldPoly) = BuildBarPolyline(
                        geom, isDirX: false, coordAlong: yMin, coordEnd: yMax,
                        transverseCoord: x, localZ: zLocTopOuter,
                        hookType: spec.HookTypeTopY, hookLength: hookLenT4, isHookUp: false);

                    topYList.Add(new FoundationBar
                    {
                        BarIndex = barIndexCounter++,
                        Role = FoundationBarRole.TopY,
                        Layer = 4,
                        Diameter = spec.DiameterTopY,
                        LocalPolyline = localPoly,
                        WorldPolyline = worldPoly,
                        HookType = spec.HookTypeTopY,
                        HookLength = hookLenT4
                    });
                }
            }
            else
            {
                // Layer 4 = Top X (outermost), Layer 3 = Top Y (inner)
                zLocTopOuter = geom.Thickness - spec.CoverTop - (spec.DiameterTopX / 2.0);
                zLocTopInner = geom.Thickness - spec.CoverTop - spec.DiameterTopX - (spec.DiameterTopY / 2.0);

                // Layer 4: Top X
                var yPositionsT4 = CalculateBarPositions(yMin, yMax, spec.SpacingTopX, spec.DistributionMode);
                double hookLenT4 = spec.HookTypeTopX != RebarHookType.None
                    ? (spec.HookLengthTopX > 0 ? spec.HookLengthTopX : 15.0 * spec.DiameterTopX)
                    : 0.0;
                hookLenT4 = System.Math.Min(hookLenT4, System.Math.Max(0.0, zLocTopOuter - spec.CoverBottom));

                for (int i = 0; i < yPositionsT4.Count; i++)
                {
                    double y = yPositionsT4[i];
                    var (localPoly, worldPoly) = BuildBarPolyline(
                        geom, isDirX: true, coordAlong: xMin, coordEnd: xMax,
                        transverseCoord: y, localZ: zLocTopOuter,
                        hookType: spec.HookTypeTopX, hookLength: hookLenT4, isHookUp: false);

                    topXList.Add(new FoundationBar
                    {
                        BarIndex = barIndexCounter++,
                        Role = FoundationBarRole.TopX,
                        Layer = 4,
                        Diameter = spec.DiameterTopX,
                        LocalPolyline = localPoly,
                        WorldPolyline = worldPoly,
                        HookType = spec.HookTypeTopX,
                        HookLength = hookLenT4
                    });
                }

                // Layer 3: Top Y
                var xPositionsT3 = CalculateBarPositions(xMin, xMax, spec.SpacingTopY, spec.DistributionMode);
                double hookLenT3 = spec.HookTypeTopY != RebarHookType.None
                    ? (spec.HookLengthTopY > 0 ? spec.HookLengthTopY : 15.0 * spec.DiameterTopY)
                    : 0.0;
                hookLenT3 = System.Math.Min(hookLenT3, System.Math.Max(0.0, zLocTopInner - spec.CoverBottom));

                for (int i = 0; i < xPositionsT3.Count; i++)
                {
                    double x = xPositionsT3[i];
                    var (localPoly, worldPoly) = BuildBarPolyline(
                        geom, isDirX: false, coordAlong: yMin, coordEnd: yMax,
                        transverseCoord: x, localZ: zLocTopInner,
                        hookType: spec.HookTypeTopY, hookLength: hookLenT3, isHookUp: false);

                    topYList.Add(new FoundationBar
                    {
                        BarIndex = barIndexCounter++,
                        Role = FoundationBarRole.TopY,
                        Layer = 3,
                        Diameter = spec.DiameterTopY,
                        LocalPolyline = localPoly,
                        WorldPolyline = worldPoly,
                        HookType = spec.HookTypeTopY,
                        HookLength = hookLenT3
                    });
                }
            }
        }

        return new FoundationMeshResult
        {
            BottomXBars = bottomXList,
            BottomYBars = bottomYList,
            TopXBars = topXList,
            TopYBars = topYList
        };
    }

    private static (Polyline3 local, Polyline3 world) BuildBarPolyline(
        FoundationGeometrySnapshot geom,
        bool isDirX,
        double coordAlong,
        double coordEnd,
        double transverseCoord,
        double localZ,
        RebarHookType hookType,
        double hookLength,
        bool isHookUp)
    {
        var localPts = new System.Collections.Generic.List<Point3>();
        double hookZOffset = isHookUp ? hookLength : -hookLength;

        if (isDirX)
        {
            // Bar runs along X at fixed Y
            if (hookType == RebarHookType.Hook90 && hookLength > 0.0)
            {
                localPts.Add(new Point3(coordAlong, transverseCoord, localZ + hookZOffset));
                localPts.Add(new Point3(coordAlong, transverseCoord, localZ));
                localPts.Add(new Point3(coordEnd, transverseCoord, localZ));
                localPts.Add(new Point3(coordEnd, transverseCoord, localZ + hookZOffset));
            }
            else
            {
                localPts.Add(new Point3(coordAlong, transverseCoord, localZ));
                localPts.Add(new Point3(coordEnd, transverseCoord, localZ));
            }
        }
        else
        {
            // Bar runs along Y at fixed X
            if (hookType == RebarHookType.Hook90 && hookLength > 0.0)
            {
                localPts.Add(new Point3(transverseCoord, coordAlong, localZ + hookZOffset));
                localPts.Add(new Point3(transverseCoord, coordAlong, localZ));
                localPts.Add(new Point3(transverseCoord, coordEnd, localZ));
                localPts.Add(new Point3(transverseCoord, coordEnd, localZ + hookZOffset));
            }
            else
            {
                localPts.Add(new Point3(transverseCoord, coordAlong, localZ));
                localPts.Add(new Point3(transverseCoord, coordEnd, localZ));
            }
        }

        var worldPts = new Point3[localPts.Count];
        for (int i = 0; i < localPts.Count; i++)
        {
            worldPts[i] = geom.ToWorld(localPts[i].X, localPts[i].Y, localPts[i].Z);
        }

        return (new Polyline3(localPts), new Polyline3(worldPts));
    }
}
```

---

## 4. Caveats & Assumptions

1. **Geometry Scope (Floor/Slab)**:
   - This specification addresses planar horizontal foundation slabs (`Floor` or foundation raft/footing) of uniform thickness $H$. Sloped footings or stepped footings require multi-segment boundary subdivision, which is outside the scope of Phase 1 (Phương án A).
2. **Polygonal Footings & Openings**:
   - For footings with non-rectangular boundaries (e.g. trapezoidal or L-shaped), the snapshot captures the Oriented Bounding Box (OBB). For exact boundary clipping against arbitrary 2D polygons with inner holes/shafts, polygon-line segment clipping (Sutherland-Hodgman or Liang-Barsky) can be added as an extension.
3. **Rebar Shape Matching**:
   - In `HPRebar.Core`, bar geometry is represented as a pure mathematical 3D centerline curve (`Polyline3`). The Revit feature layer (`HPRebar/Foundation Rebar/`) maps these polylines to Revit `Rebar` via `Rebar.CreateFromCurves` or matching standard Rebar Shapes (`RebarShape`).
4. **No Dependencies on Revit**:
   - All models and calculators reside strictly in `HPRebar.Core` (`netstandard2.0`) with zero references to `Autodesk.Revit.*`. Lengths and elevations are strictly in millimetres (`double`).

---

## 5. Conclusion & Actionable Implementation Plan

1. **Domain Engine Ready for Implementation**:
   - The mathematical foundation for `FoundationGeometrySnapshot`, `FoundationRebarSpec`, `FoundationBoundaryCalculator`, and `FoundationMeshCalculator` is fully derived, algebraically proven, and contractually typed.
   - Orthonormal transformation guarantees exact rotation support for arbitrary angles in the XY plane.
   - Physical stacking guarantees tangential contact between orthogonal layers without numerical penetration or artificial vertical gaps.
   - Edge cases (non-positive spacing, insufficient slab thickness, excessive side covers) are strictly guarded.

2. **Directory Structure for Implementation (`M3`)**:
   ```
   HPRebar/HPRebar.Core/FoundationRebar/
   ├── Calculators/
   │   ├── FoundationBoundaryCalculator.cs
   │   └── FoundationMeshCalculator.cs
   └── Models/
       ├── Enums.cs
       ├── FoundationBar.cs
       ├── FoundationGeometrySnapshot.cs
       ├── FoundationMeshResult.cs
       ├── FoundationRebarSpec.cs
       ├── Point3.cs
       ├── Polyline3.cs
       └── Vector3.cs
   ```

---

## 6. Verification Method

### 6.1 Concrete Numerical Test Suite (`HPRebar.Core.Tests/FoundationRebar/`)

The implementer can independently verify these mathematical models by executing the following unit test suite:

1. **`FoundationBoundaryCalculatorTests`**:
   - `EffectiveBoundary_SubtractsCoversCorrectly`:
     * Input: Length = 3000 mm, Width = 2000 mm, CoverSide = 50 mm.
     * Expected: $x \in [50, 2950]$ ($L_{eff,X} = 2900$), $y \in [50, 1950]$ ($L_{eff,Y} = 1900$).
   - `BoundaryValidation_Fails_WhenSideCoverExceedsDimension`:
     * Input: Length = 100 mm, CoverSide = 60 mm ($2 \cdot 60 = 120 > 100$).
     * Expected: `isValid == false`.

2. **`FoundationMeshCalculatorTests`**:
   - `LayerElevations_StackTangentiallyWithoutCollision`:
     * Input: $H = 600$, $Z_{bot} = -1000$, $Z_{top} = -400$, $c_{bot} = 50$, $c_{top} = 50$, $d_{BX} = 16$, $d_{BY} = 16$, $d_{TX} = 12$, $d_{TY} = 12$.
     * Expected Layer 1 $Z$: $-1000 + 50 + 8 = -942.0$ mm.
     * Expected Layer 2 $Z$: $-1000 + 50 + 16 + 8 = -926.0$ mm ($\Delta Z = 16.0$ mm).
     * Expected Layer 3 $Z$ (Top X): $-400 - 50 - 12 - 6 = -468.0$ mm.
     * Expected Layer 4 $Z$ (Top Y): $-400 - 50 - 6 = -456.0$ mm ($\Delta Z = 12.0$ mm).
     * Expected Clearance: $(-468 - 6) - (-926 + 8) = -474 - (-918) = 444.0$ mm.
   - `BarCounts_EqualSpacing_DividesEvenly`:
     * Span = 1400 mm, Spacing = 200 mm $\implies \lceil 1400/200 \rceil = 7$ spaces $\implies 8$ bars at $0, 200, 400, 600, 800, 1000, 1200, 1400$.
   - `BarCounts_EqualSpacing_UnevenSpan`:
     * Span = 1400 mm, Spacing = 150 mm $\implies \lceil 1400/150 \rceil = 10$ spaces $\implies 11$ bars at actual spacing $140.0$ mm.
   - `BarCounts_FixedSpacingCentered_CentersSlack`:
     * Span = 1400 mm, Spacing = 150 mm $\implies \lfloor 1400/150 \rfloor = 9$ spaces $\implies 10$ bars. Slack = $1400 - 1350 = 50$ mm. Delta = 25 mm.
   - `ArbitraryRotation_PreservesWorldLengthsAndOrthogonality`:
     * Input: Footing rotated $\theta = 30^\circ$, Length = 2000, Width = 1000.
     * Assert: Every Dir X bar in World space has length exactly $1900.000$ mm.
     * Assert: $\vec{V}_{DirX} \cdot \vec{V}_{DirY} = 0.0$ (strictly perpendicular).
     * Assert: Hook segments in World space have $\Delta X = 0, \Delta Y = 0, \Delta Z = \pm L_{hook}$ (strictly vertical).
   - `Validation_Fails_WhenThicknessInsufficient`:
     * Input: $H = 180$ mm, covers = $50 + 50 = 100$, sum diameters = $16 + 16 + 12 + 12 = 56$, required = 156 mm (passes).
     * Input: $H = 150$ mm, required = 156 mm.
     * Assert: `Validate` returns `isValid == false` with error message describing required thickness.
   - `Validation_Fails_WhenSpacingNonPositive`:
     * Input: $s = 0$ or $-100 \implies$ `isValid == false`.

### 6.2 Test Command
Run existing and new domain tests:
```bash
dotnet test HPRebar.Core.Tests
```
Expected: 241 existing tests pass + new `FoundationRebar` test suite passes with 0 failures and 0 skipped.
