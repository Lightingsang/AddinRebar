# Remediation Plan: BeamSpecialBar, BeamAdditionalBar (Layer 2), and BeamMainBar (Hairpin & Splicing)

**Author**: `explorer_m1_it2_3`  
**Role**: M1 Special Bar, Layer 2 & Polyline Remediation Explorer  
**Date**: 2026-09-07  
**Target Repository**: `HPRebar/HPRebar.Core/BeamRebar/`  
**Referenced Audits**:
- Forensic Audit Report: `.agents/auditor_m1_1/audit_report.md`
- Challenge Report: `.agents/challenger_m1_2/challenge_report.md`
- Review Report: `.agents/reviewer_m1_1/review_report.md`

---

## 1. Executive Overview

This remediation plan provides complete, mathematically verified architectural designs, precise algorithms, and code diff specifications for three critical issues identified during the adversarial audit and review of Milestone 1 (Domain Models & Calculators):

1. **`BeamSpecialBarCalculator.cs`**:
   - Prevention of support column penetration and coordinates outside the beam envelope.
   - Hanging stirrups and 45° diagonal bent ties ("thép vai bò") clamped or bounded within `[hostSpan.StartX, hostSpan.EndX]` (with concrete cover offsets).
2. **`BeamAdditionalBarCalculator.cs`**:
   - Implementation of Layer 2 negative-moment top bars for exterior supports (Support 0 and Support N).
   - Resolves the silent dropping of Layer 2 bars caused by early `continue;` statements.
3. **`BeamMainBarCalculator.cs`**:
   - Fix for 180° hairpin turn culling in `SimplifyPolyline` by requiring codirectional vector alignment ($v_1 \cdot v_2 > 0$) for collinear culling, preserving turnaround apexes.
   - Architectural strategy and defensive validation guardrails for continuous beams exceeding double commercial stock length ($L_{total} > 2 \times 11.7\text{ m} \approx 22.5\text{ m}$).

---

## 2. Issue 1: `BeamSpecialBarCalculator.cs` Clamping & Support Penetration Fix

### 2.1 Root Cause Analysis
In `HPRebar.Core/BeamRebar/Calculators/BeamSpecialBarCalculator.cs`:
- **Hanging Stirrups** (`ComputeHangingStirrups`, lines 128–149):
  Stations are computed purely from secondary beam centerline and width via `ComputeHangingStirrupStations(sec.CenterX, sec.Width, countPerSide, spacing)`.
  When a secondary beam frames near a column (e.g. $X_{center} = 350\text{ mm}$, $b_s = 250\text{ mm}$ in a span with $StartX = 200\text{ mm}$), flanking stations at $X = 75, 125, 175\text{ mm}$ fall inside the column or outside the building bounding box.
- **45° Diagonal Bent Ties ("Thép vai bò")** (`ComputeDiagonalTiePolyline`, lines 77–105):
  The diagonal incline length is $\Delta X = \Delta Z$ and the horizontal anchor leg is $L_{anchor} = 30 d_b$. The total horizontal projection from the secondary beam face is $\Delta X + L_{anchor} \approx 500\text{ mm} + 420\text{ mm} = 920\text{ mm}$.
  If the secondary beam face is within $920\text{ mm}$ of the span start face, the horizontal leg extends into negative coordinate space (e.g. $X = -281\text{ mm}$), producing illegal freeform rebar models in Revit.

### 2.2 Mathematical & Engineering Requirements
1. **Clear Span Boundaries with Cover**:
   Rebar cannot penetrate support nodes or violate concrete cover:
   $$X_{min} = hostSpan.StartX + hostSpan.Cover$$
   $$X_{max} = hostSpan.EndX - hostSpan.Cover$$
2. **Hanging Stirrup Station Filtering**:
   Hanging stirrups that fall outside $[X_{min}, X_{max}]$ must be **culled (filtered out)** rather than clamped. Clamping multiple stations to $X_{min}$ would collapse multiple stirrups into the exact same coordinate, violating coarse aggregate clearance and creating duplicate Revit elements.
   *Structural rationale*: Shear transfer between a closely spaced secondary beam and column occurs directly via compressive strut action into the column joint; hanging stirrups inside the column are physically impossible to place and structurally redundant.
3. **Diagonal Bent Tie Geometry Clamping & Validity Check**:
   - Soffit check: The secondary beam joint faces must satisfy $xSecL \ge X_{min}$ and $xSecR \le X_{max}$.
   - Bend clearance check: The 45° inclined leg requires $xSecL - \Delta X \ge X_{min}$ and $xSecR + \Delta X \le X_{max}$. If the distance to the column face is less than $\Delta X$ (roughly beam height $h$), a 45° bent bar cannot physically be formed within the span clear bounds. In this case, the diagonal tie must be omitted for this joint.
   - Anchor tip clamping: If the 45° bend clears ($xSecL - \Delta X \ge X_{min}$), the horizontal top anchorage leg is clamped to $X_{min}$ on the left and $X_{max}$ on the right:
     $$x_0 = \max(X_{min}, xSecL - \Delta X - L_{anchor})$$
     $$x_5 = \min(X_{max}, xSecR + \Delta X + L_{anchor})$$
   - Redundant segment culling: If $x_0 == x_1$ (anchor leg clamped to 0 mm length), `SimplifyPolyline` culls the zero-length segment, yielding a valid 5-point polyline.

### 2.3 Proposed Code Specification for `BeamSpecialBarCalculator.cs`

#### Method 1: `ComputeHangingStirrupStations`
```csharp
/// <summary>
/// Computes longitudinal stations X for hanging stirrups flanking a secondary beam joint.
/// Optionally filters stations to remain strictly within [minXMm, maxXMm].
/// </summary>
public static IReadOnlyList<double> ComputeHangingStirrupStations(
    double secondaryCenterXMm,
    double secondaryWidthMm,
    int countPerSide,
    double spacingMm = DefaultHangingSpacingMm,
    double minXMm = double.NegativeInfinity,
    double maxXMm = double.PositiveInfinity)
{
    if (countPerSide <= 0)
        return Array.Empty<double>();

    double xSecL = secondaryCenterXMm - (secondaryWidthMm / 2.0);
    double xSecR = secondaryCenterXMm + (secondaryWidthMm / 2.0);

    var stations = new List<double>(countPerSide * 2);

    // Left flanking stations (ordered from left to right)
    for (int k = countPerSide; k >= 1; k--)
    {
        double x = xSecL - (k * spacingMm);
        if (x >= minXMm && x <= maxXMm)
        {
            stations.Add(x);
        }
    }

    // Right flanking stations
    for (int k = 1; k <= countPerSide; k++)
    {
        double x = xSecR + (k * spacingMm);
        if (x >= minXMm && x <= maxXMm)
        {
            stations.Add(x);
        }
    }

    return stations;
}
```

#### Method 2: `ComputeDiagonalTiePolyline`
```csharp
/// <summary>
/// Computes the 3D polyline defining a 45° diagonal bent tie ("thép vai bò") under the secondary beam soffit,
/// clamping anchor tips strictly within [minXMm, maxXMm].
/// Returns empty collection if 45° bend cannot develop within the specified bounds.
/// </summary>
public static IReadOnlyList<Point3> ComputeDiagonalTiePolyline(
    double secondaryCenterXMm,
    double secondaryWidthMm,
    double primaryZTopMm,
    double primaryZBotMm,
    double coverMm,
    double barDiameterMm,
    double minXMm = double.NegativeInfinity,
    double maxXMm = double.PositiveInfinity)
{
    double xSecL = secondaryCenterXMm - (secondaryWidthMm / 2.0);
    double xSecR = secondaryCenterXMm + (secondaryWidthMm / 2.0);

    double zTopBar = primaryZTopMm - coverMm - (barDiameterMm / 2.0);
    double zBotBar = primaryZBotMm + coverMm + (barDiameterMm / 2.0);

    double deltaZ = zTopBar - zBotBar;
    if (deltaZ <= 0.0)
        return Array.Empty<Point3>();

    // For 45 degrees, tan(45°) = 1.0 => deltaX = deltaZ
    double deltaX = deltaZ;
    double anchorLength = 30.0 * barDiameterMm;

    // Check if secondary beam soffit or 45° incline points violate bounds
    double xBendL = xSecL - deltaX;
    double xBendR = xSecR + deltaX;

    if (xSecL < minXMm || xSecR > maxXMm || xBendL < minXMm || xBendR > maxXMm)
    {
        // Joint too close to support to develop 45° inclined bar within span bounds
        return Array.Empty<Point3>();
    }

    // Clamp horizontal anchor legs to span bounds
    double x0 = Math.Max(minXMm, xBendL - anchorLength);
    double x5 = Math.Min(maxXMm, xBendR + anchorLength);

    var rawPts = new List<Point3>
    {
        new(x0, 0.0, zTopBar),
        new(xBendL, 0.0, zTopBar),
        new(xSecL, 0.0, zBotBar),
        new(xSecR, 0.0, zBotBar),
        new(xBendR, 0.0, zTopBar),
        new(x5, 0.0, zTopBar)
    };

    return BeamMainBarCalculator.SimplifyPolyline(rawPts);
}
```

#### Method 3: `ComputeHangingStirrups` Integration
In `ComputeHangingStirrups(BeamContinuousStack stack, BeamSpecialBarSpec spec)`:
```csharp
    // Derived bounds for host span
    double minX = hostSpan.StartX + hostSpan.Cover;
    double maxX = hostSpan.EndX - hostSpan.Cover;

    var stations = ComputeHangingStirrupStations(
        sec.CenterX, sec.Width, spec.HangingStirrupsPerSide, spec.HangingStirrupSpacing, minX, maxX);
```

#### Method 4: `ComputeDiagonalTies` Integration
In `ComputeDiagonalTies(BeamContinuousStack stack, BeamSpecialBarSpec spec)`:
```csharp
    double minX = hostSpan.StartX + hostSpan.Cover;
    double maxX = hostSpan.EndX - hostSpan.Cover;

    var rawPts = ComputeDiagonalTiePolyline(
        sec.CenterX, sec.Width, hostSpan.TopElevation, hostSpan.BottomElevation, hostSpan.Cover, spec.DiagonalTieDiameter, minX, maxX);

    if (rawPts.Count < 2)
        continue;
```

---

## 3. Issue 2: `BeamAdditionalBarCalculator.cs` Exterior Support Layer 2 Top Bars

### 3.1 Root Cause Analysis
In `HPRebar.Core/BeamRebar/Calculators/BeamAdditionalBarCalculator.cs`:
- Exterior Support 0 (`lines 41–89`) and Exterior Support N (`lines 92–140`) only construct `Layer = 1` bars.
- At line 88 and line 139, the loop immediately executes `continue;`.
- Any user configuration with `config.Layer2Count > 0` on an exterior support is silently dropped.
- In heavy structural designs (e.g. transfer girders, cantilevers, or large moment-frame exterior joints), negative moment capacity requires 2 layers of reinforcement anchored into column cores. Dropping Layer 2 creates severe under-reinforcement.

### 3.2 Mathematical & Detailing Specification
For an exterior support (`sIdx == 0` or `sIdx == stack.Supports.Count - 1`):

#### A. Layer 2 Geometry for Exterior Support 0 (Left End):
1. **Vertical Elevation ($Z_2$)**:
   $$z_1 = z_{top} - cover - stirrupDiameter - (d_1 / 2.0)$$
   $$gap = \text{config.LayerGap} > 0 \text{ ? } \text{config.LayerGap} : (d_1 + \text{MinimumClearVerticalGapMm})$$
   $$z_2 = z_1 - gap$$
2. **Clear Span Extension ($X_{end2}$)**:
   $$r_2 = \text{config.Layer2ExtensionRatio} > 0 \text{ ? } \text{config.Layer2ExtensionRatio} : \text{DefaultTopCutoffRatioLayer2}\ (0.25)$$
   $$x_{end2} = support.RightFaceX + (r_2 \times L_{n,0})$$
3. **Exterior Column Anchorage ($X_{start2}$)**:
   $$x_{start2} = support.LeftFaceX + cover$$
4. **Vertical Downward Hook Length ($hookLen_2$)**:
   The hook drops vertically from $z_2$. It must not violate the bottom cover of the beam/column:
   $$z_{botFloor} = span.BottomElevation + cover + stirrupDiameter$$
   $$availDrop_2 = \max(0.0, z_2 - z_{botFloor})$$
   $$nominalHook_2 = \text{config.ExteriorHookLength} > 0 \text{ ? } \text{config.ExteriorHookLength} : \max(30.0 \times d_2, 200.0)$$
   $$hookLen_2 = \min(availDrop_2, nominalHook_2)$$
5. **3D Polyline Vertices (3 Points)**:
   $$\text{Pt}_0 = (x_{start2}, y, z_2 - hookLen_2)$$
   $$\text{Pt}_1 = (x_{start2}, y, z_2)$$
   $$\text{Pt}_2 = (x_{end2}, y, z_2)$$
   Where $y$ coordinates are computed from `BeamMainBarCalculator.ComputeTransverseYPositions(span.Width, cover, stirrupDiameterMm, config.Layer2Diameter, config.Layer2Count)`.
6. **Rebar Properties**:
   - `Layer = 2`
   - `HostSupportIndex = 0`
   - `HostSpanIndex = 0`
   - `StartHookAngle = HookAngle.Hook90`
   - `EndHookAngle = HookAngle.None`
   - `StartHookLength = hookLen2`
   - `LeftExtension = support.Width`
   - `RightExtension = r2 * ln`

#### B. Layer 2 Geometry for Exterior Support N (Right End):
1. **Vertical Elevation ($Z_2$)**: Same as above ($z_2 = z_1 - gap$).
2. **Clear Span Extension ($X_{start2}$)**:
   $$x_{start2} = support.LeftFaceX - (r_2 \times L_{n,N-1})$$
3. **Exterior Column Anchorage ($X_{end2}$)**:
   $$x_{end2} = support.RightFaceX - cover$$
4. **Vertical Downward Hook Length ($hookLen_2$)**: Same bounded calculation.
5. **3D Polyline Vertices (3 Points)**:
   $$\text{Pt}_0 = (x_{start2}, y, z_2)$$
   $$\text{Pt}_1 = (x_{end2}, y, z_2)$$
   $$\text{Pt}_2 = (x_{end2}, y, z_2 - hookLen_2)$$
6. **Rebar Properties**:
   - `Layer = 2`
   - `HostSupportIndex = sIdx`
   - `HostSpanIndex = stack.Spans.Count - 1`
   - `StartHookAngle = HookAngle.None`
   - `EndHookAngle = HookAngle.Hook90`
   - `EndHookLength = hookLen2`
   - `LeftExtension = r2 * ln`
   - `RightExtension = support.Width`

### 2.3 Proposed Code Specification for `BeamAdditionalBarCalculator.cs`
Replace lines 40–140 in `BeamAdditionalBarCalculator.cs`:

```csharp
            // Exterior Support 0 (Left End)
            if (isExteriorStart)
            {
                if (stack.Spans.Count == 0) continue;
                var span = stack.Spans[0];
                double ln = span.LengthClear;
                double zTop = span.TopElevation;
                double cover = span.Cover;
                double zBotFloor = span.BottomElevation + cover + stirrupDiameterMm;

                // Layer 1
                if (config.Layer1Count > 0)
                {
                    double r1 = config.Layer1ExtensionRatio > 0 ? config.Layer1ExtensionRatio : DefaultTopCutoffRatioLayer1;
                    double xStart = support.LeftFaceX + cover;
                    double xEnd = support.RightFaceX + (r1 * ln);
                    double z1 = zTop - cover - stirrupDiameterMm - (config.Layer1Diameter / 2.0);
                    double availDrop1 = Math.Max(0.0, z1 - zBotFloor);
                    double hookLen1 = config.ExteriorHookLength > 0.0
                        ? Math.Min(availDrop1, config.ExteriorHookLength)
                        : Math.Min(availDrop1, Math.Max(30.0 * config.Layer1Diameter, 200.0));

                    var yPositions1 = BeamMainBarCalculator.ComputeTransverseYPositions(
                        span.Width, cover, stirrupDiameterMm, config.Layer1Diameter, config.Layer1Count);

                    for (int i = 0; i < yPositions1.Count; i++)
                    {
                        double y = yPositions1[i];
                        var pts = new List<Point3>
                        {
                            new(xStart, y, z1 - hookLen1),
                            new(xStart, y, z1),
                            new(xEnd, y, z1)
                        };

                        result.Add(new BarPolyline
                        {
                            BarIndex = barId++,
                            Type = BarType.AdditionalTop,
                            Diameter = config.Layer1Diameter,
                            Layer = 1,
                            HostSupportIndex = sIdx,
                            HostSpanIndex = 0,
                            Polyline = new Polyline3(pts),
                            StartHookAngle = HookAngle.Hook90,
                            EndHookAngle = HookAngle.None,
                            StartHookLength = hookLen1,
                            TransverseY = y,
                            LeftExtension = support.Width,
                            RightExtension = r1 * ln,
                            BarTypeName = config.BarTypeName
                        });
                    }
                }

                // Layer 2
                if (config.Layer2Count > 0)
                {
                    double r2 = config.Layer2ExtensionRatio > 0 ? config.Layer2ExtensionRatio : DefaultTopCutoffRatioLayer2;
                    double xStart2 = support.LeftFaceX + cover;
                    double xEnd2 = support.RightFaceX + (r2 * ln);

                    double gap = config.LayerGap > 0 ? config.LayerGap : (config.Layer1Diameter + MinimumClearVerticalGapMm);
                    double z1 = zTop - cover - stirrupDiameterMm - (config.Layer1Diameter / 2.0);
                    double z2 = z1 - gap;

                    double availDrop2 = Math.Max(0.0, z2 - zBotFloor);
                    double hookLen2 = config.ExteriorHookLength > 0.0
                        ? Math.Min(availDrop2, config.ExteriorHookLength)
                        : Math.Min(availDrop2, Math.Max(30.0 * config.Layer2Diameter, 200.0));

                    var yPositions2 = BeamMainBarCalculator.ComputeTransverseYPositions(
                        span.Width, cover, stirrupDiameterMm, config.Layer2Diameter, config.Layer2Count);

                    for (int i = 0; i < yPositions2.Count; i++)
                    {
                        double y = yPositions2[i];
                        var pts = new List<Point3>
                        {
                            new(xStart2, y, z2 - hookLen2),
                            new(xStart2, y, z2),
                            new(xEnd2, y, z2)
                        };

                        result.Add(new BarPolyline
                        {
                            BarIndex = barId++,
                            Type = BarType.AdditionalTop,
                            Diameter = config.Layer2Diameter,
                            Layer = 2,
                            HostSupportIndex = sIdx,
                            HostSpanIndex = 0,
                            Polyline = new Polyline3(pts),
                            StartHookAngle = HookAngle.Hook90,
                            EndHookAngle = HookAngle.None,
                            StartHookLength = hookLen2,
                            TransverseY = y,
                            LeftExtension = support.Width,
                            RightExtension = r2 * ln,
                            BarTypeName = config.BarTypeName
                        });
                    }
                }

                continue;
            }

            // Exterior Support N (Right End)
            if (isExteriorEnd)
            {
                if (stack.Spans.Count == 0) continue;
                var span = stack.Spans[stack.Spans.Count - 1];
                double ln = span.LengthClear;
                double zTop = span.TopElevation;
                double cover = span.Cover;
                double zBotFloor = span.BottomElevation + cover + stirrupDiameterMm;

                // Layer 1
                if (config.Layer1Count > 0)
                {
                    double r1 = config.Layer1ExtensionRatio > 0 ? config.Layer1ExtensionRatio : DefaultTopCutoffRatioLayer1;
                    double xStart = support.LeftFaceX - (r1 * ln);
                    double xEnd = support.RightFaceX - cover;
                    double z1 = zTop - cover - stirrupDiameterMm - (config.Layer1Diameter / 2.0);
                    double availDrop1 = Math.Max(0.0, z1 - zBotFloor);
                    double hookLen1 = config.ExteriorHookLength > 0.0
                        ? Math.Min(availDrop1, config.ExteriorHookLength)
                        : Math.Min(availDrop1, Math.Max(30.0 * config.Layer1Diameter, 200.0));

                    var yPositions1 = BeamMainBarCalculator.ComputeTransverseYPositions(
                        span.Width, cover, stirrupDiameterMm, config.Layer1Diameter, config.Layer1Count);

                    for (int i = 0; i < yPositions1.Count; i++)
                    {
                        double y = yPositions1[i];
                        var pts = new List<Point3>
                        {
                            new(xStart, y, z1),
                            new(xEnd, y, z1),
                            new(xEnd, y, z1 - hookLen1)
                        };

                        result.Add(new BarPolyline
                        {
                            BarIndex = barId++,
                            Type = BarType.AdditionalTop,
                            Diameter = config.Layer1Diameter,
                            Layer = 1,
                            HostSupportIndex = sIdx,
                            HostSpanIndex = stack.Spans.Count - 1,
                            Polyline = new Polyline3(pts),
                            StartHookAngle = HookAngle.None,
                            EndHookAngle = HookAngle.Hook90,
                            EndHookLength = hookLen1,
                            TransverseY = y,
                            LeftExtension = r1 * ln,
                            RightExtension = support.Width,
                            BarTypeName = config.BarTypeName
                        });
                    }
                }

                // Layer 2
                if (config.Layer2Count > 0)
                {
                    double r2 = config.Layer2ExtensionRatio > 0 ? config.Layer2ExtensionRatio : DefaultTopCutoffRatioLayer2;
                    double xStart2 = support.LeftFaceX - (r2 * ln);
                    double xEnd2 = support.RightFaceX - cover;

                    double gap = config.LayerGap > 0 ? config.LayerGap : (config.Layer1Diameter + MinimumClearVerticalGapMm);
                    double z1 = zTop - cover - stirrupDiameterMm - (config.Layer1Diameter / 2.0);
                    double z2 = z1 - gap;

                    double availDrop2 = Math.Max(0.0, z2 - zBotFloor);
                    double hookLen2 = config.ExteriorHookLength > 0.0
                        ? Math.Min(availDrop2, config.ExteriorHookLength)
                        : Math.Min(availDrop2, Math.Max(30.0 * config.Layer2Diameter, 200.0));

                    var yPositions2 = BeamMainBarCalculator.ComputeTransverseYPositions(
                        span.Width, cover, stirrupDiameterMm, config.Layer2Diameter, config.Layer2Count);

                    for (int i = 0; i < yPositions2.Count; i++)
                    {
                        double y = yPositions2[i];
                        var pts = new List<Point3>
                        {
                            new(xStart2, y, z2),
                            new(xEnd2, y, z2),
                            new(xEnd2, y, z2 - hookLen2)
                        };

                        result.Add(new BarPolyline
                        {
                            BarIndex = barId++,
                            Type = BarType.AdditionalTop,
                            Diameter = config.Layer2Diameter,
                            Layer = 2,
                            HostSupportIndex = sIdx,
                            HostSpanIndex = stack.Spans.Count - 1,
                            Polyline = new Polyline3(pts),
                            StartHookAngle = HookAngle.None,
                            EndHookAngle = HookAngle.Hook90,
                            EndHookLength = hookLen2,
                            TransverseY = y,
                            LeftExtension = r2 * ln,
                            RightExtension = support.Width,
                            BarTypeName = config.BarTypeName
                        });
                    }
                }

                continue;
            }
```

---

## 4. Issue 3: `BeamMainBarCalculator.cs` Hairpin 180° Hook Culling Fix & Multi-Splice Handling

### 4.1 Hairpin 180° Turn Culling in `SimplifyPolyline`

#### Root Cause:
In `BeamMainBarCalculator.cs:424–433`:
```csharp
var v1 = (pCurr - pPrev).Normalize();
var v2 = (pNext - pCurr).Normalize();
var cross = v1.Cross(v2);
if (cross.Length > Tolerance.CollinearToleranceMm)
{
    simplified.Add(pCurr);
}
```
For two unit vectors $\vec{v}_1$ and $\vec{v}_2$, $\|\vec{v}_1 \times \vec{v}_2\| = \sin \theta$.
- Straight ahead: $\theta = 0 \implies \sin 0 = 0$.
- Direction reversal (180° hairpin turn): $\theta = \pi \implies \sin \pi = 0$.

Because $\sin \pi = 0 \le \text{CollinearToleranceMm}$, the vertex `pCurr` at the turnaround apex (e.g. $(0,0,0) \to (100,0,0) \to (50,0,0)$) is culled! The polyline is collapsed to $(0,0,0) \to (50,0,0)$, destroying the 180° hook entirely.

#### Mathematical Fix:
An intermediate vertex is redundant and should only be culled if the two vectors are **collinear AND codirectional**:
$$\|\vec{v}_1 \times \vec{v}_2\| \le \text{Tolerance.CollinearToleranceMm} \quad \text{AND} \quad \vec{v}_1 \cdot \vec{v}_2 > 0$$
When $\vec{v}_1 \cdot \vec{v}_2 \le 0$ (such as $\vec{v}_1 \cdot \vec{v}_2 \approx -1.0$ for a 180° return hook), the vertex `pCurr` is an essential reversal apex and MUST be retained in `simplified`.

#### Proposed Code Specification for `SimplifyPolyline`:
```csharp
    /// <summary>
    /// Simplifies polyline vertices: culls segments &lt; 1.0 mm and removes collinear intermediate vertices.
    /// Correctly preserves 180° hairpin turnaround vertices where vectors are anti-parallel.
    /// </summary>
    public static IReadOnlyList<Point3> SimplifyPolyline(IReadOnlyList<Point3> vertices)
    {
        if (vertices.Count < 2)
            return vertices;

        var culled = new List<Point3>(vertices.Count) { vertices[0] };
        for (int i = 1; i < vertices.Count; i++)
        {
            if (vertices[i].DistanceTo(culled[culled.Count - 1]) >= Tolerance.MinimumSegmentMm)
            {
                culled.Add(vertices[i]);
            }
        }

        if (culled.Count < 3)
            return culled;

        // Remove intermediate collinear vertices
        var simplified = new List<Point3>(culled.Count) { culled[0] };
        for (int i = 1; i < culled.Count - 1; i++)
        {
            var pPrev = simplified[simplified.Count - 1];
            var pCurr = culled[i];
            var pNext = culled[i + 1];

            var v1 = (pCurr - pPrev).Normalize();
            var v2 = (pNext - pCurr).Normalize();

            var cross = v1.Cross(v2);
            double dot = v1.Dot(v2);

            // An intermediate vertex is redundant only if vectors are collinear AND point in the same direction.
            // Hairpin turnaround vertices (dot <= 0.0) must be preserved.
            bool isCodirectionalCollinear = cross.Length <= Tolerance.CollinearToleranceMm && dot > 0.0;
            if (!isCodirectionalCollinear)
            {
                simplified.Add(pCurr);
            }
        }
        simplified.Add(culled[culled.Count - 1]);

        return simplified;
    }
```

---

### 4.2 Multi-Splice Handling for Long Beams (> 22.5 m)

#### Root Cause:
Currently, `BeamMainBarCalculator.ComputeTopMainBars` and `ComputeBottomMainBars` divide continuous bars exceeding `spec.MaxStockLength` (11.7 m) into **exactly two segments** with one lap splice at midspan (top bars) or over an intermediate support (bottom bars).
If the continuous beam total length $L_{total} > 2 \times 11.7\text{ m} - L_{lap} \approx 22.5\text{ m}$ (e.g. four 6 m spans = 24 m), splitting into two halves yields segments $> 12\text{ m}$, still violating commercial rebar stock limits (11.7 m).

#### Remediation Architecture:
1. **Defensive Validation Guardrail in `BeamContinuousStack.Validate()`**:
   Add an explicit validation check:
   ```csharp
   const double CommercialStockLengthMm = 11700.0;
   double maxSingleSpliceLength = (2.0 * CommercialStockLengthMm) - (40.0 * 25.0); // ~ 22400 mm
   if (TotalLength > maxSingleSpliceLength)
   {
       // Flag long beams requiring multi-point splicing (> 1 splice)
       return ValidationResult.Fail(
           $"Continuous beam total length ({TotalLength:0.#} mm) exceeds double stock capacity ({maxSingleSpliceLength:0.#} mm). Multi-point splicing (> 1 splice joint) required.");
   }
   ```
2. **Multi-Splice Extension Algorithm (Phase 2 Roadmap)**:
   For continuous beams with $N_{spans} \ge 3$ exceeding $22.5\text{ m}$:
   - Number of splices required: $K = \lfloor L_{total} / (L_{stock} - L_{lap}) \rfloor$.
   - **Top bars**: Spliced in midspan zones ($L/3$ to $2L/3$ of intermediate spans), never over supports. Splice locations are assigned to Spans 1, 3, etc., alternating staggered offsets ($1.3 L_{lap}$) between even and odd bar indexes.
   - **Bottom bars**: Spliced over intermediate column supports (within column width or $L/4$ from support face), never in midspan positive moment zones. Splice locations are assigned to Supports 1, 3, etc.

---

## 5. Comprehensive Unit Test Plan & Test Assertions

To eliminate tautological assertions and verify these fixes with 100% empirical rigor, the following tests must be implemented:

### 5.1 Test Suite 1: `BeamSpecialBarCalculatorTests.cs` (Clamping Verification)
1. **`SecondaryBeamNearLeftColumnClampsHangingStirrupsInsideHostSpan`**:
   - Secondary beam at $X_{center} = 350\text{ mm}$, $b_s = 250\text{ mm}$ in a span with $StartX = 200\text{ mm}$, $Cover = 25\text{ mm}$.
   - Flanking stirrups generated at $X \ge 225\text{ mm}$.
   - Assert: Zero stirrups exist with $X < 225\text{ mm}$.
2. **`SecondaryBeamNearRightColumnClampsHangingStirrupsInsideHostSpan`**:
   - Secondary beam at $X_{center} = 5650\text{ mm}$, $b_s = 250\text{ mm}$ in a span ending at $EndX = 5800\text{ mm}$, $Cover = 25\text{ mm}$.
   - Assert: Zero stirrups exist with $X > 5775\text{ mm}$.
3. **`SecondaryBeamNearColumnOmitsDiagonalTieWhenBendCannotClearSpan`**:
   - Secondary beam at $X_{center} = 500\text{ mm}$, beam height $H = 600\text{ mm}$ ($\Delta X \approx 536\text{ mm}$), span $StartX = 200\text{ mm}$.
   - Bend point $xSecL - \Delta X = (500 - 125) - 536 = -161\text{ mm} < 200\text{ mm}$.
   - Assert: `ComputeDiagonalTies` produces 0 ties for this intersection.
4. **`DiagonalBentTieAnchorLegsClampToSpanBounds`**:
   - Secondary beam at $X_{center} = 900\text{ mm}$ ($\Delta X = 536\text{ mm}$, bend point $= 239\text{ mm} \ge 225\text{ mm}$, anchor $= 420\text{ mm} \implies$ unclamped $X = -181\text{ mm}$).
   - Assert: Generated polyline tip has $X \ge 225\text{ mm}$ ($StartX + Cover$).

### 5.2 Test Suite 2: `BeamAdditionalBarCalculatorTests.cs` (Exterior Support Layer 2 Verification)
1. **`ExteriorSupportZeroGeneratesBothLayerOneAndLayerTwoBars`**:
   - Config: `SupportIndex = 0`, `Layer1Count = 3`, `Layer2Count = 2`, `LayerGap = 50.0`.
   - Assert:
     `bars.Count(b => b.Layer == 1) == 3`
     `bars.Count(b => b.Layer == 2) == 2`
     `Assert.Equal(50.0, bars.First(b => b.Layer == 1).Points[1].Z - bars.First(b => b.Layer == 2).Points[1].Z, Precision)`
     `Assert.Equal(HookAngle.Hook90, bars.First(b => b.Layer == 2).StartHookAngle)`
2. **`ExteriorSupportNGreatestSupportIndexGeneratesLayerTwoBars`**:
   - Config on Support $N$: `Layer1Count = 2`, `Layer2Count = 2`, `LayerGap = 60.0`.
   - Assert:
     `bars.Count(b => b.Layer == 2) == 2`
     `Assert.Equal(HookAngle.Hook90, bars.First(b => b.Layer == 2).EndHookAngle)`
3. **`LayerTwoHookLengthClampedToAvailableClearHeight`**:
   - Shallow beam $H = 350\text{ mm}$ with large layer gap.
   - Assert: Hook tip $Z \ge BottomElevation + Cover + StirrupDiameter$.

### 5.3 Test Suite 3: `BeamMainBarCalculatorTests.cs` (Hairpin & Polyline Verification)
1. **`SimplifyPolylinePreservesOneHundredEightyDegreeHairpinApex`**:
   - Input: `[(0, 0, 0), (100, 0, 0), (50, 0, 0)]`.
   - Assert: Output has 3 vertices; vertex 1 is exactly `(100, 0, 0)`.
2. **`SimplifyPolylinePreservesIntermediatePointsOnHairpinStraightLegs`**:
   - Input: `[(0, 0, 0), (50, 0, 0), (100, 0, 0), (75, 0, 0), (50, 0, 0)]`.
   - Straight forward segment $(50, 0, 0)$ is culled; apex $(100, 0, 0)$ is kept; straight return segment $(75, 0, 0)$ is culled.
   - Assert: Output is exactly `[(0, 0, 0), (100, 0, 0), (50, 0, 0)]`.
3. **`ReplacementForTautologicalMultiLayerTests`**:
   - Replace lines 233–248 with authentic calls to `BeamAdditionalBarCalculator.ComputeSupportTopBars` and `ComputeSpanBottomBars`, verifying that production calculators correctly place Layer 2 below/above Layer 1 with the specified `LayerGap`.
4. **`LongBeamExceedingDoubleStockLengthFailsValidation`**:
   - Stack with 4 spans of 6 m each ($L_{total} = 24.4\text{ m}$).
   - Assert: `stack.Validate().IsValid == false`.

---

## 6. Execution Roadmap & Impact Assessment

| Task | Component | Risk Level | Blast Radius | Downstream Impact |
|---|---|---|---|---|
| **Task 1** | `BeamSpecialBarCalculator.cs` | Medium | Hanging & diagonal tie generation | Eliminates clash in Revit 3D model & prevents outside coords |
| **Task 2** | `BeamAdditionalBarCalculator.cs` | Low | Support 0 & N top bars | Enables full negative moment capacity on exterior supports |
| **Task 3** | `BeamMainBarCalculator.cs` | Low | Polyline vertex simplification | Enables 180° hairpin hooks, U-caps, and return bends |
| **Task 4** | `BeamMainBarCalculatorTests.cs` | Low | Test suite only | Restores audit integrity from VIOLATION to CLEAN |

All proposed changes maintain **100% pure standard C# (`netstandard2.0`)**, with **zero dependencies on `Autodesk.Revit.*`**, preserving unit testability in the headless xUnit runner.
