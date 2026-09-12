# Comprehensive Unit Test Specifications: HPRebar.Core/BeamRebar/ (xUnit v3)

**Author**: `spec_miner_m1_3`  
**Role**: M1 Test Suite Specification Miner  
**Target Project**: `HPRebar.Core.Tests` (`net8.0`, xUnit v3 under `Microsoft.Testing.Platform`)  
**Subject Under Test (SUT)**: `HPRebar.Core/BeamRebar/` (`netstandard2.0`, Zero Revit dependencies)  
**Date**: 2026-09-07  
**Status**: Authoritative Test Specification Plan  

---

## 1. Executive Summary & Test Suite Architecture

This document defines the comprehensive unit test specification for the pure domain logic and geometry engine of the Continuous Beam Reinforcement module (`HPRebar.Core/BeamRebar/`).

Following the established gold standard of `HPRebar.Core.Tests/ColumnRebar/` (102 tests, 100% pass rate), the Beam Rebar test suite executes in pure .NET 8 CLI via `dotnet test HPRebar.Core.Tests` with **zero dependencies** on Autodesk Revit APIs, running in sub-second time without requiring Revit licenses or running processes.

### 1.1 Architecture & Directory Mapping

```
HPRebar/
├── HPRebar.Core/
│   └── BeamRebar/
│       ├── Models/
│       │   ├── Point3.cs                     (Immutable 3D coordinate struct in mm)
│       │   ├── Vector3.cs                    (Vector arithmetic & direction struct)
│       │   ├── Polyline3.cs                  (Ordered 3D vertex sequence with segment helpers)
│       │   ├── BeamSpan.cs                   (Continuous span geometry & dimensions)
│       │   ├── BeamSupportNode.cs            (Support bearing geometry & classification)
│       │   ├── BeamContinuousStack.cs        (Chained spans and supports container)
│       │   ├── BeamStirrupSpec.cs            (Stirrup configuration & zones)
│       │   ├── BeamMainBarSpec.cs            (Continuous longitudinal top/bottom bar spec)
│       │   ├── BeamAdditionalBarSpec.cs      (Top support and bottom midspan addition spec)
│       │   ├── BeamSideBarSpec.cs            (Deep beam web/skin reinforcement spec)
│       │   ├── BeamSpecialBarSpec.cs         (Secondary beam intersection spec)
│       │   └── Enums.cs                      (StirrupLayout, SupportType, BarPosition, etc.)
│       ├── Calculators/
│       │   ├── BeamStirrupDistributionCalculator.cs
│       │   ├── BeamMainBarCalculator.cs
│       │   ├── BeamAdditionalBarCalculator.cs
│       │   ├── BeamSideBarCalculator.cs
│       │   ├── BeamSpecialBarCalculator.cs
│       │   └── BeamCanvasTransformCalculator.cs
│       └── Tolerance.cs                      (Floating-point epsilon comparison 1e-9)
│
└── HPRebar.Core.Tests/
    └── BeamRebar/
        ├── TestBeamData.cs                   (Shared test fixtures & builder factory)
        ├── BeamStirrupDistributionCalculatorTests.cs
        ├── BeamMainBarCalculatorTests.cs
        ├── BeamAdditionalBarCalculatorTests.cs
        ├── BeamSideBarCalculatorTests.cs
        ├── BeamSpecialBarCalculatorTests.cs
        └── BeamCanvasTransformCalculatorTests.cs
```

### 1.2 Quantitative Target Metrics

| Metric | Target Value | Verification Gate |
|---|---|---|
| **Total Test Cases** | 94 unit tests across 6 calculator suites | `dotnet test HPRebar.Core.Tests` |
| **Pass Rate** | 100% (0 failures, 0 errors, 0 skipped) | Automated build & test pipeline |
| **Execution Duration** | < 1.5 seconds total | xUnit v3 parallel test runner |
| **Code Coverage Target** | > 95% line & branch coverage on `HPRebar.Core.BeamRebar` | Domain boundary verification |
| **Revit Dependencies** | Exactly 0 (no `Autodesk.Revit.*` in Core or Tests) | Compiler & assembly audit |

---

## 2. Shared Fixtures & Test Data Builder (`TestBeamData.cs`)

Following the pattern of `HPRebar.Core.Tests.ColumnRebar.TestSections`, `TestBeamData` is an `internal static class` providing standard parametric factory methods and defaults. Tests mutate these records cleanly using C# 9+ `with { ... }` syntax.

```csharp
using System.Collections.Generic;
using HPRebar.Core.BeamRebar.Models;

namespace HPRebar.Core.Tests.BeamRebar;

/// <summary>
///     Shared fixtures and fluent builder factory for BeamRebar unit tests.
///     All dimensions are expressed in millimetres (double).
/// </summary>
internal static class TestBeamData
{
    public const double DefaultCover = 25.0;
    public const double DefaultStirrupDiameter = 8.0;
    public const double DefaultMainTopDiameter = 20.0;
    public const double DefaultMainBottomDiameter = 20.0;
    public const double DefaultSideBarDiameter = 12.0;
    public const double DefaultColumnWidth = 400.0;
    public const int DefaultPrecision = 6;

    /// <summary>Creates a standard single-span beam stack between two exterior columns.</summary>
    public static BeamContinuousStack SingleSpan(
        double length = 6000,
        double width = 300,
        double height = 600,
        double leftCol = DefaultColumnWidth,
        double rightCol = DefaultColumnWidth)
    {
        var supports = new List<BeamSupportNode>
        {
            new(0, "Col-0", 0, leftCol, SupportType.ExteriorColumn),
            new(1, "Col-1", length, rightCol, SupportType.ExteriorColumn)
        };

        var spans = new List<BeamSpan>
        {
            new(0, "Span-1", length, width, height, TopOffsetMm: 0, CoverMm: DefaultCover,
                ClearLengthMm: length - (leftCol / 2.0) - (rightCol / 2.0))
        };

        return new BeamContinuousStack(spans, supports);
    }

    /// <summary>Creates a standard 2-span continuous beam stack.</summary>
    public static BeamContinuousStack TwoSpan(
        double l1 = 6000,
        double l2 = 6000,
        double width = 300,
        double height = 600,
        double colWidth = DefaultColumnWidth)
    {
        var supports = new List<BeamSupportNode>
        {
            new(0, "Col-0", 0, colWidth, SupportType.ExteriorColumn),
            new(1, "Col-1", l1, colWidth, SupportType.InteriorColumn),
            new(2, "Col-2", l1 + l2, colWidth, SupportType.ExteriorColumn)
        };

        var spans = new List<BeamSpan>
        {
            new(0, "Span-1", l1, width, height, 0, DefaultCover, l1 - colWidth),
            new(1, "Span-2", l2, width, height, 0, DefaultCover, l2 - colWidth)
        };

        return new BeamContinuousStack(spans, supports);
    }

    /// <summary>Creates a standard 3-span continuous beam stack (Tier 4 framing case).</summary>
    public static BeamContinuousStack ThreeSpan(
        double l1 = 6000,
        double l2 = 5000,
        double l3 = 6000,
        double width = 300,
        double height = 600,
        double colWidth = DefaultColumnWidth)
    {
        double x0 = 0;
        double x1 = l1;
        double x2 = l1 + l2;
        double x3 = l1 + l2 + l3;

        var supports = new List<BeamSupportNode>
        {
            new(0, "Col-0", x0, colWidth, SupportType.ExteriorColumn),
            new(1, "Col-1", x1, colWidth, SupportType.InteriorColumn),
            new(2, "Col-2", x2, colWidth, SupportType.InteriorColumn),
            new(3, "Col-3", x3, colWidth, SupportType.ExteriorColumn)
        };

        var spans = new List<BeamSpan>
        {
            new(0, "Span-1", l1, width, height, 0, DefaultCover, l1 - colWidth),
            new(1, "Span-2", l2, width, height, 0, DefaultCover, l2 - colWidth),
            new(2, "Span-3", l3, width, height, 0, DefaultCover, l3 - colWidth)
        };

        return new BeamContinuousStack(spans, supports);
    }

    /// <summary>Creates a cantilever beam stack (Cantilever Left + Interior Span).</summary>
    public static BeamContinuousStack CantileverLeft(
        double lCant = 2000,
        double lSpan = 6000,
        double width = 300,
        double height = 600,
        double colWidth = DefaultColumnWidth)
    {
        var supports = new List<BeamSupportNode>
        {
            new(0, "Tip-0", 0, 0, SupportType.CantileverEnd),
            new(1, "Col-1", lCant, colWidth, SupportType.InteriorColumn),
            new(2, "Col-2", lCant + lSpan, colWidth, SupportType.ExteriorColumn)
        };

        var spans = new List<BeamSpan>
        {
            new(0, "Cant-1", lCant, width, height, 0, DefaultCover, lCant - (colWidth / 2.0)),
            new(1, "Span-2", lSpan, width, height, 0, DefaultCover, lSpan - colWidth)
        };

        return new BeamContinuousStack(spans, supports);
    }

    /// <summary>Creates a beam stack with variable cross-sections across adjacent spans.</summary>
    public static BeamContinuousStack VariableDepth(
        double l1 = 6000, double h1 = 600,
        double l2 = 6000, double h2 = 400,
        double width = 300,
        double colWidth = DefaultColumnWidth)
    {
        var supports = new List<BeamSupportNode>
        {
            new(0, "Col-0", 0, colWidth, SupportType.ExteriorColumn),
            new(1, "Col-1", l1, colWidth, SupportType.InteriorColumn),
            new(2, "Col-2", l1 + l2, colWidth, SupportType.ExteriorColumn)
        };

        var spans = new List<BeamSpan>
        {
            new(0, "Span-1", l1, width, h1, 0, DefaultCover, l1 - colWidth),
            new(1, "Span-2", l2, width, h2, 0, DefaultCover, l2 - colWidth)
        };

        return new BeamContinuousStack(spans, supports);
    }

    /// <summary>Creates a deep beam stack (h >= 700 mm) triggering side/skin reinforcement.</summary>
    public static BeamContinuousStack DeepBeam(
        double length = 6000,
        double width = 400,
        double height = 800,
        double colWidth = DefaultColumnWidth)
    {
        return SingleSpan(length, width, height, colWidth, colWidth);
    }

    /// <summary>Factory for uniform stirrup specification.</summary>
    public static BeamStirrupSpec UniformStirrupSpec(double spacing = 150, double diameter = DefaultStirrupDiameter) =>
        new(StirrupLayout.Uniform, diameter, DefaultCover, S1: spacing, S2: spacing, StartOffsetMm: 50.0);

    /// <summary>Factory for 3-zone L/4 stirrup specification.</summary>
    public static BeamStirrupSpec ThreeZoneL4StirrupSpec(
        double s1 = 100,
        double s2 = 200,
        double diameter = DefaultStirrupDiameter) =>
        new(StirrupLayout.ThreeZoneL4, diameter, DefaultCover, S1: s1, S2: s2, StartOffsetMm: 50.0);

    /// <summary>Factory for 3-zone L/3 stirrup specification.</summary>
    public static BeamStirrupSpec ThreeZoneL3StirrupSpec(
        double s1 = 100,
        double s2 = 200,
        double diameter = DefaultStirrupDiameter) =>
        new(StirrupLayout.ThreeZoneL3, diameter, DefaultCover, S1: s1, S2: s2, StartOffsetMm: 50.0);

    /// <summary>Factory for continuous main longitudinal bar specification.</summary>
    public static BeamMainBarSpec MainBarSpec(
        int topCount = 3,
        double topDiameter = DefaultMainTopDiameter,
        int bottomCount = 3,
        double bottomDiameter = DefaultMainBottomDiameter,
        double hookLength = 350.0) =>
        new(topCount, topDiameter, bottomCount, bottomDiameter,
            LeftHookLengthMm: hookLength, RightHookLengthMm: hookLength,
            MaxStockLengthMm: 11700.0, LapLengthMultiplier: 40.0, StaggerSplice: true);
}
```

---

## 3. Four-Tier Unit Test Specifications

### 3.1 Tier 1: Core Feature Coverage (10 Feature Groups)

Tier 1 validates that every mathematical algorithm and calculation formula operates correctly under standard design parameters.

```
+-----------------------------------------------------------------------------------------+
|                                    TIER 1 COVERAGE                                      |
+------------------------------+------------------------------+---------------------------+
| 1. Uniform Stirrups          | 5. Continuous Bottom Bars    | 9. Secondary Hanging Ties |
| 2. 3-Zone L/4 Stirrups       | 6. Additional Top Bars (L/3) | 10. Canvas Transformation |
| 3. 3-Zone L/3 Stirrups       | 7. Additional Bot Bars (L/7) |                           |
| 4. Continuous Top Bars       | 8. Deep Beam Side Bars (Skin)|                           |
+------------------------------+------------------------------+---------------------------+
```

#### Group 1: Uniform Stirrup Layout (`StirrupLayout.Uniform`)
- **Formula**:
  - Available distance: $L_{dist} = L_n - 2 \times o_{start}$ (where $o_{start} = 50$ mm).
  - Bar count: $n = \lfloor L_{dist} / S \rfloor + 1$.
  - Centering margin: $\delta = (L_{dist} - (n - 1) \times S) / 2.0$.
  - Start offset from left support face: $X_{first} = o_{start} + \delta$.
- **Test Invariants**:
  - Returns exactly 1 `StirrupRun` in collection.
  - Spacing equals specified $S$.
  - Start offset and end offset are symmetric.
  - Total occupied run length $\le L_n$.

#### Group 2: 3-Zone L/4 Stirrup Layout (`StirrupLayout.ThreeZoneL4`)
- **Formula**:
  - Support zone length: $L_{zone} = L_n / 4.0$.
  - Midspan zone length: $L_{mid} = L_n - 2 \times L_{zone} = L_n / 2.0$.
  - Zone 1 (Left): $n_1 = \lfloor (L_{zone} - 50) / S_1 \rfloor + 1$, Spacing $S_1$, StartOffset $= 50$.
  - Zone 2 (Midspan): $n_2 = \lfloor L_{mid} / S_2 \rfloor + 1$, Spacing $S_2$, StartOffset $= L_{zone} + \delta_2$.
  - Zone 3 (Right): $n_3 = n_1$, Spacing $S_1$, StartOffset $= L_n - L_{zone} + \delta_1$.
- **Test Invariants**:
  - Returns exactly 3 `StirrupRun`s.
  - `runs[0].Count == runs[2].Count` and `runs[0].Spacing == runs[2].Spacing == S1`.
  - `runs[1].Spacing == S2`.

#### Group 3: 3-Zone L/3 Stirrup Layout (`StirrupLayout.ThreeZoneL3`)
- **Formula**:
  - All three zones have nominal length $L_n / 3.0$.
  - Zone 1 & Zone 3 dense with $S_1$, Zone 2 sparse with $S_2$.
- **Test Invariants**:
  - Returns exactly 3 `StirrupRun`s with symmetric counts and offsets.

#### Group 4: Continuous Top Main Longitudinal Bars
- **Formula**:
  - Polyline 3D coordinates:
    - Point 0 (Hook tip down in left column): $(X_{start}, Y_k, Z_{top} - Cover - d_{stirrup} - d_{bar}/2 - L_{hook})$
    - Point 1 (Hook corner): $(X_{start}, Y_k, Z_{top} - Cover - d_{stirrup} - d_{bar}/2)$
    - Point 2 (Right hook corner): $(X_{end}, Y_k, Z_{top} - Cover - d_{stirrup} - d_{bar}/2)$
    - Point 3 (Right hook tip down): $(X_{end}, Y_k, Z_{top} - Cover - d_{stirrup} - d_{bar}/2 - L_{hook})$
  - Transverse $Y$ positions:
    - Clear width: $W_{clear} = b - 2 \times Cover - 2 \times d_{stirrup} - d_{bar}$.
    - Spacing $\Delta Y = W_{clear} / (n_{bars} - 1)$.
    - $Y_k = -b/2 + Cover + d_{stirrup} + d_{bar}/2 + k \times \Delta Y$.
- **Test Invariants**:
  - Generates exactly $n_{top}$ polylines.
  - Every polyline has exactly 4 vertices (U-shape downward).
  - All vertices of bar $k$ share identical $Y$ coordinate (planar polyline).
  - Total length equals $(X_{end} - X_{start}) + 2 \times L_{hook}$.

#### Group 5: Continuous Bottom Main Longitudinal Bars
- **Formula**:
  - Point 0 (Hook tip up): $(X_{start}, Y_k, Z_{bot} + Cover + d_{stirrup} + d_{bar}/2 + L_{hook})$
  - Point 1 (Bottom left corner): $(X_{start}, Y_k, Z_{bot} + Cover + d_{stirrup} + d_{bar}/2)$
  - Point 2 (Bottom right corner): $(X_{end}, Y_k, Z_{bot} + Cover + d_{stirrup} + d_{bar}/2)$
  - Point 3 (Hook tip up): $(X_{end}, Y_k, Z_{bot} + Cover + d_{stirrup} + d_{bar}/2 + L_{hook})$
- **Test Invariants**:
  - Generates exactly $n_{bot}$ polylines.
  - Hook points extend vertically upwards ($+Z$).

#### Group 6: Additional Top Bars Over Supports (Negative Moment)
- **Formula**:
  - Intermediate Support $j$ (width $C_j$, left clear span $L_{n, left}$, right clear span $L_{n, right}$):
    - Left extension: $L_{ext, left} = L_{n, left} / 3.0$.
    - Right extension: $L_{ext, right} = L_{n, right} / 3.0$.
    - Bar starts at $X_{support, j} - C_j/2 - L_{ext, left}$.
    - Bar ends at $X_{support, j} + C_j/2 + L_{ext, right}$.
    - Total length $= L_{ext, left} + C_j + L_{ext, right}$.
  - Multi-layer offset:
    - Layer 1: Same $Z$ as top main bars ($Z_0$).
    - Layer 2: $Z_1 = Z_0 - (d_{bar} + 30)$ mm.
    - Layer 2 cutoff: Extends $L_n / 4.0$ (shorter than Layer 1).
- **Test Invariants**:
  - Bar is centered over support centerline when adjacent spans are equal.
  - Layer 2 is shorter than Layer 1 by exactly $(L_{n1} + L_{n2}) \times (1/3 - 1/4)$.

#### Group 7: Additional Bottom Bars at Midspan (Positive Moment)
- **Formula**:
  - Starts at $X_{face, left} + L_n / 7.0$ (or $L_n / 8.0$).
  - Ends at $X_{face, right} - L_n / 7.0$.
  - Total length $= L_n \times (1 - 2/7) \approx 0.714 L_n$.
  - Layer 2 offset: $Z_1 = Z_0 + (d_{bar} + 30)$ mm.
- **Test Invariants**:
  - Straight bar (2 vertices).
  - Perfectly centered in clear span: $X_{mid} = (X_{start} + X_{end}) / 2.0$.

#### Group 8: Deep Beam Side / Skin Reinforcement
- **Formula**:
  - Triggered if and only if $h \ge 700$ mm.
  - Vertical zone: from bottom stirrup corner to top stirrup corner.
  - Number of pairs $n_{pairs} = \lceil (h - 2 \times Cover - 200) / 300 \rceil$.
  - Spaced evenly along $Z$, mirrored on left and right faces:
    - $Y_{left} = -b/2 + Cover + d_{stirrup} + d_{side}/2$
    - $Y_{right} = +b/2 - Cover - d_{stirrup} - d_{side}/2$
- **Test Invariants**:
  - For $h = 600$ mm $\to 0$ side bars.
  - For $h = 800$ mm $\to \ge 1$ pair (2 bars) side bars.
  - For $h = 1200$ mm $\to 3$ pairs (6 bars) side bars.
  - Vertical spacing between adjacent side bars $\le 300$ mm.

#### Group 9: Secondary Beam Hanging Ties & Diagonal Bars
- **Formula**:
  - Secondary beam framing at $X_{sec}$ with width $b_{sec}$ and depth $h_{sec}$.
  - Hanging zone: $X_{sec} \pm (b_{sec}/2 + h_{sec}/2)$.
  - Hanging stirrups: $n_{pairs}$ closed stirrups spaced at 50 mm on both sides.
  - Diagonal bent bars: 45° inclined legs bridging under secondary beam soffit.
- **Test Invariants**:
  - Generates exactly $2 \times n$ hanging stirrups.
  - Diagonal bar bend angle is $45^\circ \pm 0.001^\circ$.

#### Group 10: Canvas Scaling & Coordinate Transformation
- **Formula**:
  - $W_{draw} = W_{canvas} - 2M$, $H_{draw} = H_{canvas} - 2M$.
  - Scale factor $S = \min(W_{draw} / L_{total}, H_{draw} / H_{max})$.
  - Screen $X_{canvas} = M + (X - X_{min}) \times S$.
  - Screen $Y_{canvas} = H_{canvas} - M - (Z - Z_{min}) \times S$.
- **Test Invariants**:
  - $X_{canvas}$ monotonically increases with domain $X$.
  - Higher elevation $Z$ results in smaller screen $Y$ (WPF coordinate convention).
  - All rendered points stay strictly within $[M, W_{canvas} - M]$ and $[M, H_{canvas} - M]$.

---

### 3.2 Tier 2: Boundary & Corner Cases

Tier 2 exercises degenerate conditions, extreme dimensions, boundary thresholds, and exception contracts.

| # | Calculator / Module | Boundary Condition | Input Parameter | Expected Assertion & Result |
|---|---|---|---|---|
| 1 | `StirrupDistribution` | Zero clear span | $L_n = 0$ mm | `Assert.Throws<ArgumentOutOfRangeException>` |
| 2 | `StirrupDistribution` | Negative clear span | $L_n = -500$ mm | `Assert.Throws<ArgumentOutOfRangeException>` |
| 3 | `StirrupDistribution` | Zero spacing | $S = 0$ mm | `Assert.Throws<ArgumentOutOfRangeException>` |
| 4 | `StirrupDistribution` | Negative spacing | $S = -100$ mm | `Assert.Throws<ArgumentOutOfRangeException>` |
| 5 | `StirrupDistribution` | Revit API set limit violation | $L_n = 10000$, $S = 5$ ($n > 1002$) | `Assert.Throws<ArgumentOutOfRangeException>` with "exceeds maximum 1002" |
| 6 | `StirrupDistribution` | Sizing just inside set limit | $n = 1001$ bars | Accepted without exception; `Assert.Equal(1001, run.Count)` |
| 7 | `StirrupDistribution` | Short link beam | $L_n = 500$ mm with 3-zone L4 | Auto-collapses to `Uniform`; returns single run |
| 8 | `StirrupDistribution` | Clear span < 2 * start offset | $L_n = 80$ mm ($< 100$ mm) | Returns 0 stirrups or throws `ArgumentOutOfRangeException` |
| 9 | `MainBarCalculator` | Single span beam | 1 span, 2 exterior columns | 2 anchor hooks, continuous polyline |
| 10 | `MainBarCalculator` | Left Cantilever | $Tip_0 \to Col_1 \to Col_2$ | Top tension bar runs to tip and turns down; Bottom bar stops at $Col_1$ |
| 11 | `MainBarCalculator` | Right Cantilever | $Col_0 \to Col_1 \to Tip_2$ | Top bar turns down at right tip; Bottom bar stops at $Col_1$ |
| 12 | `MainBarCalculator` | Both Ends Cantilever | $Tip_0 \to Col_1 \to Col_2 \to Tip_3$ | Top bar anchored at both tips; Bottom bar confined to interior span |
| 13 | `MainBarCalculator` | Depth step decrease | $h_1 = 600$, $h_2 = 400$ | Bottom bars terminate with 90° upward hooks at intermediate column |
| 14 | `MainBarCalculator` | Short polyline segment culling | Vertex gap $< 1.0$ mm | Points merged; no segment $< 1.0$ mm exists in output |
| 15 | `MainBarCalculator` | Out-of-plane planarity | Vertices with floating delta | All vertices have identical $Y$ within $1.0\times 10^{-9}$ |
| 16 | `AdditionalBarCalculator`| End exterior support addition | Exterior column node | Anchors into exterior column with 90° hook, extends $L_n/3$ into span |
| 17 | `SideBarCalculator` | Exact height threshold boundary | $h = 699.9$ mm vs $h = 700.0$ mm | $699.9$ mm $\to 0$ pairs; $700.0$ mm $\to 1$ pair |
| 18 | `CanvasTransform` | Empty beam stack | `new List<BeamSpan>()` | `Assert.Throws<ArgumentException>` |
| 19 | `CanvasTransform` | Zero canvas dimensions | $W_{canvas} = 0$ or $H_{canvas} = 0$ | `Assert.Throws<ArgumentOutOfRangeException>` |

---

### 3.3 Tier 3: Realistic Multi-Variable Combinations

Tier 3 verifies the interaction of multiple parameters simultaneously (varying cross-sections, multi-layer bars, long-span lap splicing).

#### Combination 1: 3 Spans with Variable Heights and Widths
- **Configuration**:
  - Span 1: $L_1 = 6000$, $b_1 = 300$, $h_1 = 600$
  - Span 2: $L_2 = 7000$, $b_2 = 400$, $h_2 = 750$ (Deep beam with side bars)
  - Span 3: $L_3 = 5000$, $b_3 = 300$, $h_3 = 500$
- **Verifications**:
  1. Top main bars run continuously across all 3 spans at flush top slab level.
  2. Bottom main bars of Span 2 cannot enter Span 1 or Span 3 straight; they anchor into Support 1 and Support 2 with upward hooks.
  3. Span 2 automatically generates 1 pair of side bars ($\Phi 12$); Spans 1 and 3 generate 0 side bars.
  4. Stirrup dimensions ($W \times H$) change appropriately across each span:
     - Span 1 stirrups: $(300 - 2c) \times (600 - 2c) = 250 \times 550$ mm.
     - Span 2 stirrups: $(400 - 2c) \times (750 - 2c) = 350 \times 700$ mm.
     - Span 3 stirrups: $(300 - 2c) \times (500 - 2c) = 250 \times 450$ mm.

#### Combination 2: Deep Beam with Multi-Layer Main Bars + Side Bars + 3-Zone Stirrups
- **Configuration**:
  - Span: $L_n = 7000$, $b = 400$, $h = 900$.
  - Main Top: 4 bars (2 in Layer 1, 2 in Layer 2 with $\Delta Z = -50$ mm).
  - Main Bottom: 4 bars (2 in Layer 1, 2 in Layer 2 with $\Delta Z = +50$ mm).
  - Side Bars: 2 pairs ($4$ bars total), $s_v \le 300$ mm.
  - Cross-Ties: C-ties @ 400 mm spacing.
  - Stirrups: 3-Zone L4 ($S_1 = 100$, $S_2 = 200$).
- **Verifications**:
  1. Layer 1 and Layer 2 vertical coordinates maintain clear gap $= 30$ mm ($50 - 20$).
  2. Side bars do not clash vertically with top or bottom Layer 2 bars.
  3. Total stirrup count matches zone equations for $L_n = 7000$.

#### Combination 3: Long Continuous Beam with 50% Staggered Lap Splices
- **Configuration**:
  - Continuous beam $L_{total} = 24.0$ m ($> 11.7$ m commercial stock limit).
  - Number of top bars: 4. Number of bottom bars: 4.
  - Lap length: $L_{lap} = 40d = 800$ mm.
- **Verifications**:
  1. Top bars are spliced strictly in the midspan zone (between $0.33 L_n$ and $0.67 L_n$).
  2. Bottom bars are spliced strictly at column support zones (within $0.25 L_n$ of support face).
  3. 50% stagger rule: Bars 0 & 2 splice at station $X_A$; Bars 1 & 3 splice at station $X_B = X_A + 1.3 \times L_{lap} = X_A + 1040$ mm.
  4. No bar polyline exceeds $11.7$ m maximum stock length.

#### Combination 4: Cantilever Left + Interior Span + Cantilever Right
- **Configuration**:
  - Cantilever Left: $L_{c1} = 1500$, $b = 300$, $h = 500$.
  - Interior Span: $L_{span} = 6000$, $b = 300$, $h = 600$.
  - Cantilever Right: $L_{c2} = 1800$, $b = 300$, $h = 500$.
- **Verifications**:
  1. Continuous top tension bars run from left tip to right tip with 90° downward hooks at both tips.
  2. Bottom bars in interior span stop at column faces and anchor with upward hooks.
  3. Cantilever bottom compression bars extend only into the support column by $L_a \ge 30d$.

---

### 3.4 Tier 4: Real-World Structural Framing Validation Cases

Tier 4 validates complete end-to-end framing layouts mirroring actual construction drawings.

```
Elevation: Standard 3-Span Office Girder (Framing Case A)
===================================================================================
+--------------------+-----------------------+--------------------+  Z_top = 0
|      Span 1        |        Span 2         |       Span 3       |
| 300 x 600, L=6000  | 300 x 600, L=5000     | 300 x 600, L=6000  |
+---------+----------+-----------+-----------+----------+---------+  Z_bot = -600
          |                      |                      |
     [Support 0]            [Support 1]            [Support 2]   [Support 3]
     Exterior Col           Interior Col           Interior Col   Exterior Col
     (400 x 400)            (400 x 400)            (400 x 400)    (400 x 400)
===================================================================================
```

#### Framing Case A: Standard 3-Span Continuous Office Girder
- **Clear Spans**:
  - Span 1: $L_{n1} = 6000 - 400/2 - 400/2 = 5600$ mm.
  - Span 2: $L_{n2} = 5000 - 400/2 - 400/2 = 4600$ mm.
  - Span 3: $L_{n3} = 6000 - 400/2 - 400/2 = 5600$ mm.
- **Transverse Stirrup Distributions (3-Zone L/4, $S_1 = 100$, $S_2 = 200$, $o_{start} = 50$)**:
  - **Span 1 ($L_n = 5600$)**:
    - $L_{zone} = 5600 / 4 = 1400$ mm. $L_{mid} = 2800$ mm.
    - Zone 1: $n_1 = \lfloor (1400 - 50)/100 \rfloor + 1 = 13 + 1 = 14$ stirrups @ 100 mm.
    - Zone 2: $n_2 = \lfloor 2800/200 \rfloor + 1 = 14 + 1 = 15$ stirrups @ 200 mm.
    - Zone 3: $n_3 = 14$ stirrups @ 100 mm.
    - Total Span 1 stirrups $= 14 + 15 + 14 = 43$ stirrups.
  - **Span 2 ($L_n = 4600$)**:
    - $L_{zone} = 4600 / 4 = 1150$ mm. $L_{mid} = 2300$ mm.
    - Zone 1: $n_1 = \lfloor (1150 - 50)/100 \rfloor + 1 = 11 + 1 = 12$ stirrups @ 100 mm.
    - Zone 2: $n_2 = \lfloor 2300/200 \rfloor + 1 = 11 + 1 = 12$ stirrups @ 200 mm.
    - Zone 3: $n_3 = 12$ stirrups @ 100 mm.
    - Total Span 2 stirrups $= 12 + 12 + 12 = 36$ stirrups.
  - **Span 3 ($L_n = 5600$)**: Identical to Span 1 $\to 43$ stirrups.
  - **Total Stirrups across all spans**: $43 + 36 + 43 = 122$ stirrups.
- **Longitudinal Reinforcement**:
  - Main Top: 3T20 continuous. Hook length into exterior columns $= 600 - 2 \times 25 = 550$ mm.
    - Horizontal length $= 6000 + 5000 + 6000 = 17000$ mm.
    - Total bar length $= 17000 + 2 \times 550 = 18100$ mm (requires midspan lap splice).
  - Additional Top Bars over Support 1 (Col-1, $C = 400$):
    - Left extension $= 5600 / 3 = 1866.67$ mm.
    - Right extension $= 4600 / 3 = 1533.33$ mm.
    - Total bar length $= 1866.67 + 400 + 1533.33 = 3800.0$ mm.
  - Additional Top Bars over Support 2 (Col-2, $C = 400$):
    - Left extension $= 4600 / 3 = 1533.33$ mm.
    - Right extension $= 5600 / 3 = 1866.67$ mm.
    - Total bar length $= 3800.0$ mm.
  - Additional Bottom Bars (Midspan):
    - Span 1: Starts at $X_{face} + 5600/7 = 200 + 800 = 1000$ mm. Ends at $5800 - 800 = 5000$ mm. Length $= 4000$ mm.
    - Span 2: Starts at $6200 + 4600/7 = 6200 + 657.14 = 6857.14$ mm. Length $= 4600 \times 5/7 = 3285.71$ mm.

#### Framing Case B: Secondary Beam Intersection with Hanging Stirrups & Diagonal Ties
- **Configuration**:
  - Primary girder: Single span $L = 8000$, $b = 400$, $h = 700$.
  - Secondary beam frames at $X = 3500$ mm with $b_{sec} = 250$ mm, $h_{sec} = 500$ mm.
  - Hanging stirrups: 3 pairs $\Phi 10$ @ 50 mm on each side of intersection ($6$ stirrups total).
  - Diagonal bars: 2T16 bent at 45° placed under secondary soffit.
- **Verifications**:
  1. Hanging stirrup positions:
     - Left group: $X = 3500 - 250/2 - 50 \times (1, 2, 3) = 3375 - (50, 100, 150) = 3325, 3275, 3225$ mm.
     - Right group: $X = 3500 + 250/2 + 50 \times (1, 2, 3) = 3625 + (50, 100, 150) = 3675, 3725, 3775$ mm.
  2. Diagonal bent bar polyline:
     - Soffit clearance and 45° angle verified to precision $10^{-6}$.
  3. No conflict with primary beam 3-zone stirrups.

---

## 4. Test Class Specifications & Assertion Precision

Every test class follows strict xUnit v3 conventions:
- Sealed class: `public sealed class <CalculatorName>Tests`.
- Constant precision: `private const int Precision = 6;`.
- Numerical assertions: `Assert.Equal(expectedDouble, actualDouble, Precision);`.
- Invariant assertions: `Assert.Single`, `Assert.InRange`, `Assert.Throws`.

### 4.1 `BeamStirrupDistributionCalculatorTests` (18 Tests)

```csharp
public sealed class BeamStirrupDistributionCalculatorTests
{
    private const int Precision = 6;

    // Feature Coverage (Tier 1)
    [Fact] public void UniformLayoutReturnsSingleRunEvenlySpaced();
    [Fact] public void UniformLayoutCentersLeftoverSlackBetweenFirstAndLastBar();
    [Theory]
    [InlineData(StirrupLayout.ThreeZoneL4, 1400d, 2800d)]
    [InlineData(StirrupLayout.ThreeZoneL3, 1866.666667d, 1866.666667d)]
    public void ZonedLayoutCalculatesExactZoneLengths(StirrupLayout layout, double expectedL1, double expectedL2);
    [Fact] public void ThreeZoneL4LayoutProducesSymmetricSupportRuns();
    [Fact] public void ThreeZoneL3LayoutDistributesDenseSparseDense();
    [Fact] public void SupportNodeStirrupToggleGeneratesRunThroughColumnWidth();
    [Fact] public void SupportNodeStirrupToggleDisabledProducesZeroNodeStirrups();

    // Boundary & Corner Cases (Tier 2)
    [Fact] public void ClearSpanBelowStartOffsetReturnsZeroStirrups();
    [Fact] public void ShortSpanLinkBeamCollapsesThreeZoneToUniform();
    [Fact] public void ZeroOrNegativeClearSpanThrowsArgumentOutOfRangeException();
    [Fact] public void ZeroOrNegativeSpacingThrowsArgumentOutOfRangeException();
    [Fact] public void SpacingExceedingRevitMaxBarPositionsThrowsArgumentOutOfRangeException();
    [Fact] public void SpacingJustInsideRevitLimitSucceeds();

    // Multi-Span & Realistic (Tier 3 & 4)
    [Fact] public void MultiSpanStackGeneratesIndependentStirrupRunsPerSpan();
    [Fact] public void CantileverSpanAppliesDenseUniformLayoutAlongCantileverLength();
    [Fact] public void VaryingSpansGenerateMatchingRunCountsForStandardThreeSpanGirder();
    [Fact] public void DenseSpacingInDeepBeamMaintainsClearDistanceRules();
    [Fact] public void TotalStirrupCountMatchesCalculatedDesignEquation();
}
```

### 4.2 `BeamMainBarCalculatorTests` (20 Tests)

```csharp
public sealed class BeamMainBarCalculatorTests
{
    private const int Precision = 6;

    // Feature Coverage (Tier 1)
    [Fact] public void TopMainBarsGenerateFourVertexUShapedPolylinesWithDownwardHooks();
    [Fact] public void BottomMainBarsGenerateFourVertexUShapedPolylinesWithUpwardHooks();
    [Fact] public void TransverseSpacingEvenlyDistributesBarsAcrossClearBeamWidth();
    [Fact] public void ExteriorAnchorageHooksClampToColumnDepthMinusCover();
    [Fact] public void BarPolylineTotalLengthMatchesSumOfSegments();
    [Fact] public void AllVerticesOfEachBarShareIdenticalTransverseYCoordinate();

    // Splicing & Staggering
    [Fact] public void TotalLengthUnderStockLimitGeneratesUnbrokenContinuousBars();
    [Fact] public void TotalLengthExceedingStockLimitSplicesTopBarsInMidspan();
    [Fact] public void TotalLengthExceedingStockLimitSplicesBottomBarsAtSupports();
    [Fact] public void StaggerToggleOffsetsAdjacentBarSplicesByOnePointThreeLapLength();
    [Fact] public void LapLengthCalculatesCorrectlyFromMultiplierAndBarDiameter();

    // Boundary & Step Changes (Tier 2 & 3)
    [Fact] public void DepthStepBetweenSpansTerminatesBottomBarsWithUpwardHooksAtSupport();
    [Fact] public void CantileverLeftExtendsTopTensionBarToTipAndTurnsDown();
    [Fact] public void CantileverLeftStopsBottomBarAtInteriorColumnFace();
    [Fact] public void CantileverRightAnchorsTopBarAtTip();
    [Fact] public void BothCantileversAnchorTopBarsAtBothTips();
    [Fact] public void SubMillimeterPolylineSegmentsAreCulledToPreventRevitGeometryCrash();
    [Fact] public void MultiLayerTopBarsOffsetSecondLayerVerticallyWithSpecifiedGap();
    [Fact] public void MultiLayerBottomBarsOffsetSecondLayerVerticallyUpwards();
    [Fact] public void StandardThreeSpanOfficeGirderGeneratesExactPolylineCoordinates();
}
```

### 4.3 `BeamAdditionalBarCalculatorTests` (16 Tests)

```csharp
public sealed class BeamAdditionalBarCalculatorTests
{
    private const int Precision = 6;

    // Support Top Bars (Tier 1 & 2)
    [Fact] public void SupportTopBarsCenterOverInteriorColumnBetweenEqualSpans();
    [Fact] public void SupportTopBarsExtendL3IntoAdjacentSpansForLayerOne();
    [Fact] public void SupportTopBarsExtendL4IntoAdjacentSpansForLayerTwo();
    [Fact] public void SupportTopBarsLayerTwoOffsetBelowLayerOneWithClearance();
    [Fact] public void ExteriorSupportTopBarsAnchorIntoEndColumnWithDownwardHook();
    [Fact] public void UnequalAdjacentSpansExtendAsymmetricLengthsIntoRespectiveSpans();

    // Midspan Bottom Bars (Tier 1 & 2)
    [Fact] public void MidspanBottomBarsStartAtOneSeventhClearSpanFromSupportFace();
    [Fact] public void MidspanBottomBarsEndAtOneSeventhClearSpanFromRightSupportFace();
    [Fact] public void MidspanBottomBarsLayerTwoOffsetAboveLayerOneWithClearance();
    [Fact] public void StraightMidspanBarsHaveExactlyTwoVertices();

    // Boundaries & Combinations (Tier 2 & 3)
    [Fact] public void CantileverInteriorSupportAdditionExtendsFromCantileverIntoSpan();
    [Fact] public void ZeroAdditionalBarsRequestedGeneratesEmptyCollection();
    [Fact] public void HighBarCountAutomaticallyDistributesExcessIntoSecondLayer();
    [Fact] public void FramingCaseASupportBarsMatchNominalCutLengths();
    [Fact] public void FramingCaseAMidspanBarsMatchNominalCutLengths();
    [Fact] public void TransversePositionsFitBetweenMainLongitudinalBars();
}
```

### 4.4 `BeamSideBarCalculatorTests` (14 Tests)

```csharp
public sealed class BeamSideBarCalculatorTests
{
    private const int Precision = 6;

    // Height Trigger & Counting (Tier 1 & 2)
    [Theory]
    [InlineData(500d, 0)]
    [InlineData(600d, 0)]
    [InlineData(699d, 0)]
    [InlineData(700d, 1)]
    [InlineData(800d, 1)]
    [InlineData(1000d, 2)]
    [InlineData(1200d, 3)]
    public void BeamHeightThresholdDeterminesNumberOfSideBarPairs(double height, int expectedPairs);

    // Geometry & Positions
    [Fact] public void SideBarsArePositionedInPairsAlongLeftAndRightLateralFaces();
    [Fact] public void SideBarTransverseOffsetsNestInsideStirrupLegs();
    [Fact] public void SideBarVerticalSpacingNeverExceedsThreeHundredMillimetres();
    [Fact] public void SideBarsRunContinuouslyAcrossEntireSpanLength();

    // Cross-Ties (C-Ties)
    [Fact] public void CrossTiesAreGeneratedWhenSideBarsArePresent();
    [Fact] public void CrossTiesAreOmittedWhenSideBarsAreAbsent();
    [Fact] public void CrossTiesLongitudinalSpacingMatchesSpecification();
    [Fact] public void CrossTiesHookShapesEncloseOppositeSideBars();
    [Fact] public void DeepBeamCombinationMaintainsClearDistanceToTopAndBottomLayers();
    [Fact] public void StepChangeInDepthOmitsSideBarsOnlyOnShallowerSpan();
}
```

### 4.5 `BeamSpecialBarCalculatorTests` (12 Tests)

```csharp
public sealed class BeamSpecialBarCalculatorTests
{
    private const int Precision = 6;

    // Hanging Stirrups (Tier 1 & 4)
    [Fact] public void SecondaryBeamIntersectionGeneratesSymmetricHangingStirrupsFlankingJoint();
    [Fact] public void HangingStirrupSpacingMatchesSpecifiedDistance();
    [Fact] public void HangingStirrupDimensionsMatchPrimaryBeamCrossSection();
    [Fact] public void NumberOfHangingStirrupPairsMatchesUserSpecification();

    // Overlapping Secondary Beams
    [Fact] public void AdjacentSecondaryBeamsMergeOverlappingHangingZones();
    [Fact] public void SecondaryBeamOutsideClearSpanThrowsArgumentException();

    // Diagonal Bent Bars (Thép vai bò)
    [Fact] public void DiagonalBentBarsGenerateFortyFiveDegreeInclinationLegs();
    [Fact] public void DiagonalBentBarsPositionDirectlyBeneathSecondaryBeamSoffit();
    [Fact] public void DiagonalBentBarsToggleDisabledProducesZeroBentBars();
    [Fact] public void SecondaryBeamDepthBelowThresholdOmitsDiagonalBentBars();
    [Fact] public void FramingCaseBSpecialBarGeometryMatchesNominalDrawingCoordinates();
    [Fact] public void HangingStirrupsDoNotConflictWithPrimaryStirrupRuns();
}
```

### 4.6 `BeamCanvasTransformCalculatorTests` (14 Tests)

```csharp
public sealed class BeamCanvasTransformCalculatorTests
{
    private const int Precision = 6;

    // Scaling & Transformation (Tier 1)
    [Fact] public void RealSizedContinuousBeamShrinksUniformlyToFitCanvasDimensions();
    [Fact] public void AspectRatioIsStrictlyPreservedBetweenLengthAndHeight();
    [Fact] public void MarginPaddingIsMaintainedOnAllFourCanvasBorders();
    [Fact] public void CanvasYCoordinatesAreInvertedRelativeDomainElevations();
    [Fact] public void XCoordinatesIncreaseMonotonicallyFromLeftToRight();

    // Boundary & degenerate cases (Tier 2)
    [Fact] public void EmptyBeamStackThrowsArgumentException();
    [Fact] public void ZeroOrNegativeCanvasDimensionsThrowArgumentOutOfRangeException();
    [Fact] public void SingleSpanCanvasScaleMatchesDirectDimensionRatio();
    [Fact] public void MultiSpanCanvasScaleIsDrivenByTotalLength();
    [Fact] public void ExtremelyDeepBeamCanvasScaleIsDrivenByMaxHeight();
    [Fact] public void DomainToCanvasRoundTripPreservesRelativeRatios();
    [Fact] public void CrossSectionCanvasScalesWidthAndHeightIndependently();
    [Fact] public void CrossSectionCanvasCentersBeamInTransverseViewport();
    [Fact] public void CanvasPointsNeverExceedViewportBoundingBox();
}
```

---

## 5. Verification Method & 100% Test Pass Strategy

To ensure 100% test pass rate under `dotnet test HPRebar.Core.Tests`:
1. **Isolated Pure Logic**: SUT classes depend only on standard .NET BCL and `Polyfill 11.0.1` in `HPRebar.Core`.
2. **Deterministic Inputs & Outputs**: All calculators are pure static functions with immutable inputs and outputs.
3. **Floating-Point Precision Standard**: Assertions use `Assert.Equal(expected, actual, precision: 6)` with floating-point delta $< 1.0\times 10^{-6}$ mm.
4. **Boundary Guardrails**: All edge conditions (0 spans, negative spacing, $>1002$ bars) are validated by explicit exception assertions.
5. **Execution Command**:
   ```powershell
   dotnet test HPRebar/HPRebar.Core.Tests
   ```
   Must pass 100% (196 passing tests = 102 existing ColumnRebar + 94 new BeamRebar).

---

## 6. Unresolved Questions & Forward Dependencies

- **Q1**: Will `BeamStackReader` in M3 provide support widths ($C$) as full bounding width or bearing centerline?
  - *Resolution*: Test fixtures model $C$ as total support bearing width along the beam axis, with clear span $L_n = L_{center} - C_{left}/2 - C_{right}/2$, perfectly matching both structural codes and `R02_BeamsRebar`.
- **Q2**: What is the default hook shape for cross-ties (C-ties)?
  - *Resolution*: Supported as 135° hook on one end and 90° hook on the other (Vietnamese standard detailing for installation ease over longitudinal bars).
