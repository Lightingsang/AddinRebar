# Kata Rebar Core Geometry & Mathematical Engine Survey Report

- **Author**: `explorer_survey_2` (Core Beam Rebar Math Explorer)
- **Target Subsystem**: `HPRebar.Core` (Target Framework: `netstandard2.0`, Zero `Autodesk.Revit.*` dependencies)
- **Authoritative Directive**: `ORIGINAL_REQUEST.md` (Header `## 2026-09-27T15:57:37Z`)
- **Date**: 2026-09-27

---

## 1. Executive Summary

This report establishes the mathematical, geometric, and architectural foundation for the **Kata Rebar** generation engine in `HPRebar.Core`. By conducting a comprehensive audit of existing beam reinforcement models in `HPRebar.Core/BeamRebar/` (452 passing unit tests), inspecting real cell contracts from `C:\kata_pro\Kata.xlsm` (sheet `Dam`), and cross-analyzing Revit add-in geometric adapters (`KataAxisFrame`, `PointMapper`), this survey details how to translate raw structural detailing specifications (`KataBeamRebarSpec`) into explicit 3D rebar curve geometry ready for native Revit generation.

### Key Conclusions:
1. **High Code Reuse (70% Core Math)**: Foundational 3D vector math (`Point3`, `Vector3`, `Polyline3`), tolerance logic (`Tolerance`), continuous beam representations (`BeamSpan`, `BeamSupportNode`, `BeamContinuousStack`), and the core 3-zone stirrup distribution algorithm (`BeamStirrupDistributionCalculator`) are completely host-free and 100% reusable directly.
2. **Dedicated KataRebar Domain Layer**: A clean, dedicated namespace `HPRebar.Core.KataRebar` (`Models` and `Calculators`) is recommended to model sheet `Dam` structural data (`KataBeamRebarSpec`), parse Vietnamese structural bar notations (e.g., `6f25`, `2f20;2f16`, `a100/200/50`), and encapsulate the calculation output (`KataRebarLayoutResult`).
3. **100% netstandard2.0 Purity**: All calculations operate strictly in standard millimetres within the continuous beam's local coordinate frame $(X_{\text{axis}}, Y_{\text{transverse}}, Z_{\text{elevation}})$, maintaining zero dependencies on `Autodesk.Revit.*`. Revit world coordinate mapping $(X, Y, Z)_{\text{feet}}$ remains strictly quarantined in the Revit add-in boundary (`HPRebar/KataRebar/` via `PointMapper`).
4. **Comprehensive Detailing Support**: The proposed calculation pipeline covers all Kata detailing rules: 90° exterior anchorage hooks, staggered lap splices for bars $> 11.7\text{ m}$, support top bar cutoffs ($L/3$, $L/4$, sheet parameters), midspan bottom bar cutoffs ($L/7$), cantilever overhangs, deep beam side bars ($h \ge 700\text{ mm}$), and 3-zone multi-type stirrup layouts (closed outer hoop □, open cap U, and cross-tie C).

---

## 2. Inventory & Analysis of Existing Core Math in `HPRebar.Core/BeamRebar/`

### 2.1 3D Coordinate System & Geometry Primitives

All geometry in `HPRebar.Core` is defined in a continuous beam local Cartesian coordinate system:
- **$X$ (Longitudinal)**: Station along the beam axis from start to end (mm). $X = 0$ is aligned with the reference start datum (exterior face of Support 0 or clear span face).
- **$Y$ (Transverse)**: Centered at $Y = 0$ along the beam longitudinal centerline. Left lateral face is at $Y = -b/2$; right lateral face is at $Y = +b/2$.
- **$Z$ (Elevation)**: Vertical elevation (mm). $Z_{\text{top}}$ is the top surface elevation; bottom soffit is at $Z_{\text{bot}} = Z_{\text{top}} - h$.

| Existing Type | Location | Responsibilities | Reusability for `KataRebar` |
|---|---|---|---|
| `Point3` | `HPRebar.Core/BeamRebar/Models/Point3.cs` | Readonly immutable struct $(X, Y, Z)$ in millimetres. Vector arithmetic, Euclidean distance, tolerance equality. | **100% Direct Reuse** |
| `Vector3` | `HPRebar.Core/BeamRebar/Models/Vector3.cs` | Readonly struct for 3D vectors. Dot product, cross product, normalization, unit vectors. | **100% Direct Reuse** |
| `Polyline3` | `HPRebar.Core/BeamRebar/Models/Polyline3.cs` | Ordered sequence of `Point3`. Implements cumulative `TotalLength` and **`Simplify(minSegmentLength = 1.0)`**, which is vital to prevent Revit exceptions on `Application.ShortCurveTolerance` (~0.78 mm / 1/32"). | **100% Direct Reuse** |
| `BarPolyline` | `HPRebar.Core/BeamRebar/Models/BarPolyline.cs` | Master centerline curve container with physical metadata: `BarIndex`, `Type` (`BarType`), `Diameter`, `Layer`, `Polyline`, `StartHookAngle`, `EndHookAngle`, `StartHookLength`, `EndHookLength`, `TransverseY`, `HostSpanIndex`, `HostSupportIndex`, `TotalLength`. | **100% Direct Reuse** |
| `Tolerance` | `HPRebar.Core/BeamRebar/Tolerance.cs` | Precision constants: `Default = 1.0e-9`, `CollinearToleranceMm = 1.0e-6`, `MinimumSegmentMm = 1.0`. Epsilon comparison helpers. | **100% Direct Reuse** |

### 2.2 Continuous Beam Hierarchy

The structural multi-span continuous beam is modeled by three decoupled records:
1. **`BeamSpan`** (`HPRebar.Core/BeamRebar/Models/BeamSpan.cs`):
   - `Index`: 0-based span index ($0 \dots N-1$).
   - `LengthCenter` ($L_c$): Center-to-center support distance (mm).
   - `LengthClear` ($L_n$): Clear distance between support inner faces (mm).
   - `Width` ($b$) & `Height` ($h$): Cross-section dimensions (mm).
   - `TopElevation` ($Z_{\text{top}}$) & `BottomElevation` ($Z_{\text{bot}} = Z_{\text{top}} - h$).
   - `StartX` & `EndX = StartX + LengthClear`: Stations of the clear span in local coordinates.
   - `Cover`: Clear concrete cover (mm).
   - `Cantilever`: Enum `CantileverPosition` (`None`, `Left`, `Right`, `Both`).
2. **`BeamSupportNode`** (`HPRebar.Core/BeamRebar/Models/BeamSupportNode.cs`):
   - For $N$ spans, there are exactly $N+1$ supports (Index $0 \dots N$).
   - `CenterX`: Longitudinal station of support centerline (mm).
   - `Width`: Dimension along the beam axis (mm).
   - `LeftFaceX = CenterX - Width/2` & `RightFaceX = CenterX + Width/2`.
   - `Type`: `SupportType` (`Column`, `Girder`, `Wall`, `CantileverEnd`).
   - `IsExterior`: True for Support 0 and Support $N$ (or cantilever tips where $\text{Width} = 0$).
3. **`BeamContinuousStack`** (`HPRebar.Core/BeamRebar/Models/BeamContinuousStack.cs`):
   - Immutable master assembly containing `Spans`, `Supports`, and optional `SecondaryIntersections`.
   - `TotalLength = OverallEndX - OverallStartX`.
   - `Validate()`: Ensures span count $\ge 1$, support count $= \text{spans} + 1$, all dimensions $> 0$.

### 2.3 Concrete Cover Offsets & Transverse Bar Layout

Cover rules are universally applied inward from concrete outer faces:
- **Top Bar Elevation**:
  $$Z_{\text{top\_bar}} = Z_{\text{top}} - c_{\text{top}} - d_{\text{stirrup}} - \frac{d_{\text{bar}}}{2}$$
- **Bottom Bar Elevation**:
  $$Z_{\text{bot\_bar}} = Z_{\text{bot}} + c_{\text{bot}} + d_{\text{stirrup}} + \frac{d_{\text{bar}}}{2}$$
- **Transverse Placement ($Y$-coordinates)**:
  Implemented in `BeamMainBarCalculator.ComputeTransverseYPositions(widthMm, coverMm, stirrupDiameterMm, barDiameterMm, count)`:
  $$Y_0 = -\frac{b}{2} + c + d_{\text{stirrup}} + \frac{d_{\text{bar}}}{2}$$
  $$Y_{n-1} = +\frac{b}{2} - c - d_{\text{stirrup}} - \frac{d_{\text{bar}}}{2}$$
  $$\Delta Y = \frac{Y_{n-1} - Y_0}{\text{count} - 1}$$
  Centering bars perfectly across the width, guaranteeing equal concrete cover on both sides.

### 2.4 Main Continuous Bars & Anchorage Hooks

`BeamMainBarCalculator` (`HPRebar.Core/BeamRebar/Calculators/BeamMainBarCalculator.cs`) implements:
1. **Exterior Anchorage Hooks (90° bends)**:
   - Top bars: 90° hook downward (`Hook90Down`).
   - Bottom bars: 90° hook upward (`Hook90Up`).
   - Default calculated hook length:
     $$L_{\text{hook}} = \min\left(h - 2c - 2d_{\text{stirrup}}, \, \max(30 d_{\text{bar}}, \, 200\text{ mm})\right)$$
   - Geometry constructed as a 4-point 3D polyline:
     $$(X_{\text{start}}, Y, Z_{\text{bar}} - L_{\text{hook}}) \longrightarrow (X_{\text{start}}, Y, Z_{\text{bar}}) \longrightarrow (X_{\text{end}}, Y, Z_{\text{bar}}) \longrightarrow (X_{\text{end}}, Y, Z_{\text{bar}} - L_{\text{hook}})$$
2. **Commercial Stock Length & Splicing**:
   - Commercial bar limit: $L_{\text{stock}} = 11,700\text{ mm}$.
   - When total length $> 11,700\text{ mm}$, bars are automatically split with 50% staggered lap splices:
     - Lap length: $L_{\text{lap}} = \text{LapFactor} \times d_{\text{bar}}$ (default $40 d$).
     - Stagger offset: $1.3 \times L_{\text{lap}}$.
     - Top bars splice in the **midspan** (zone of low tension / positive moment).
     - Bottom bars splice at **intermediate supports** (zone of zero positive moment).
3. **Cantilever Support Termination**:
   - At a cantilever end, bottom main bars stop at the interior support face (they do not extend into the cantilever soffit).

### 2.5 Additional Reinforcement Calculators

`BeamAdditionalBarCalculator` (`HPRebar.Core/BeamRebar/Calculators/BeamAdditionalBarCalculator.cs`) implements:
1. **Top Negative-Moment Bars Over Supports**:
   - **Exterior Supports (Support 0 / Support $N$)**:
     - Anchored with 90° hook down into column face: $L_{\text{hook}} = \min(h - 2c - 2d_{\text{stirrup}}, 30 d)$.
     - Layer 1 extends into clear span by $L_{\text{ext},1} = r_1 \times L_n$ (default $r_1 = 1/3 = L/3$).
     - Layer 2 extends by $L_{\text{ext},2} = r_2 \times L_n$ (default $r_2 = 1/4 = L/4$).
     - Layer 2 placed below Layer 1 with vertical gap $\Delta Z = \max(50\text{ mm}, d_{\text{bar}} + 30\text{ mm})$.
   - **Interior Supports**:
     - Symmetrical straight bars continuous across the column width $C$:
       $$X_{\text{start}} = X_{\text{leftFace}} - (r \times L_{n,\text{left}}), \quad X_{\text{end}} = X_{\text{rightFace}} + (r \times L_{n,\text{right}})$$
       $$\text{Total Length} = (r \times L_{n,\text{left}}) + C + (r \times L_{n,\text{right}})$$
2. **Bottom Positive-Moment Bars in Midspans**:
   - Cutoff ratio from clear span face: default $r_{\text{cut}} = 1/7 \approx 0.143$.
   - Curtailment points:
     $$X_{\text{start}} = X_{\text{leftFace}} + (r_{\text{cut}} \times L_n), \quad X_{\text{end}} = X_{\text{rightFace}} - (r_{\text{cut}} \times L_n)$$
   - Layer 2 placed above Layer 1 with vertical gap $\Delta Z = 50\text{ mm}$.

### 2.6 Stirrup Distribution Calculator (3-Zone Support L/4 vs Midspan L/2)

`BeamStirrupDistributionCalculator` (`HPRebar.Core/BeamRebar/Calculators/BeamStirrupDistributionCalculator.cs`) provides the exact mathematical engine needed:
- **Cantilever Spans**: Uniform dense spacing $s_1$ across entire length ($L_{\text{cant}} - s_0 - c$).
- **3-Zone Layout (`ThreeZoneL4`)**:
  - Dense zone length at support start: $L_1 = L_n / 4$. First stirrup at $s_0 = 50\text{ mm}$. Spacing $s_1$.
  - Dense zone length at support end: $L_3 = L_n / 4$. Spacing $s_1$.
  - Midspan sparse zone: $L_2 = L_n / 2$. Spacing $s_2$.
  - **Clash Prevention**: Symmetrically centers midspan stirrups in the physical gap between Zone 1 and Zone 3 such that transition spacing $d_{\text{boundary}}$ satisfies:
    $$\frac{s_2}{2} < d_{\text{boundary}} \le s_2$$
  - Prevents Revit bar clashes and eliminates duplicate stirrups at zone boundaries.
  - Collapses to uniform dense layout if clear span $< 600\text{ mm}$.
- **Support Node Ties (`ComputeNodeRun`)**: Distributes stirrups across column width $C$ if enabled.

### 2.7 Side (Skin / Web) Reinforcement & Cross-Ties

`BeamSideBarCalculator` (`HPRebar.Core/BeamRebar/Calculators/BeamSideBarCalculator.cs`) enforces TCVN 5574:2018 §10.3.2 and ACI 318 §9.7.2.3:
- Triggered when beam height $h \ge 700\text{ mm}$.
- Row count:
  $$n_{\text{rows}} = \max\left(1, \, \left\lceil \frac{h - 2(c + d_{\text{stirrup}} + d_{\text{main}}/2)}{300\text{ mm}} \right\rceil - 1\right)$$
- Symmetrically placed on lateral faces ($Y_{\text{left}}, Y_{\text{right}}$).
- Transverse cross-ties connecting opposing side bars with alternating 90° and 135° hooks at spacing $s_{\text{tie}} = 400\text{ mm}$.

---

## 3. Sheet `Dam` (Kata.xlsm) Detailing Rules vs Existing Core Capabilities

Based on the live inspection of `C:\kata_pro\Kata.xlsm` (sheet `Dam`), here is the direct mapping between Kata's Excel rows/cells and our mathematical domain:

| Sheet `Dam` Cell / Row | Content in Kata | Format / Sample Value | Handling in Core Math Engine |
|---|---|---|---|
| **B3** | Tên dầm (Beam Name) | `"B01"`, `"D1"` | Stored in `KataBeamRebarSpec.BeamName`; used for naming, logging, and idempotency tag. |
| **B4** | Số cấu kiện (Member Count) | `1` | Stored in `KataBeamRebarSpec.MemberCount`. |
| **B5 / B6** | $h$ dầm / $b$ dầm (mm) | `1100` / `500` | Section dimensions; sets default bounding box for stirrup and main bar elevations. |
| **B7** | $h$ sàn (mm) | `150` | Slab thickness. Used for composite beam cap stirrups (U) or elevation references. |
| **B10** | Cao trình dầm (Level) | `"+3.300"` or `3.3` | Reference top elevation $Z_{\text{top}}$. |
| **G2 / G3** | Neo kéo / Neo nén ($d$) | `40` / `30` | Tension lap/anchorage factor ($40d$) and compression anchorage factor ($30d$). Used to calculate $L_{\text{lap}} = 40d$ and minimum hook lengths. |
| **H3 / H5** | Tỷ lệ cắt thép gia cường | `0.2` ($L/5$) / `0.25` ($L/4$) | Cutoff ratios for top additional bars ($r_1, r_2$). Overrides default $L/3$ and $L/4$. |
| **I3 / I5** | Mốc tính chiều dài cắt | `"L từ mép cột"` or `"L từ tâm cột"` | Enum `KataCutoffReference`: determines whether $L_{\text{ext}} = r \times L_{\text{clear}}$ (from column face) or $r \times L_{\text{center}}$ (from column center). |
| **J9** | Lớp bảo vệ thép chủ / đai | `"50/25"` or `"30/20"` | Clear covers: `CoverMain = 30 mm`, `CoverStirrup = 25 mm`. |
| **B11** | Thép chịu lực trên (Top Main) | `"6f25"` | Parsed to Count = 6, Diameter = 25 mm. Generated by continuous bar calculator with 90° hooks down. |
| **B12** | Thép chịu lực dưới (Bot Main) | `"6f25"` | Parsed to Count = 6, Diameter = 25 mm. Generated by continuous bar calculator with 90° hooks up. |
| **Row 10 (C$\to$)** | Phân loại cột / nhịp | `Cột \| Nhịp \| Cột \| Nhịp...` | Defines the sequence of alternating supports and spans. $0$ width represents cantilever joints or free ends. |
| **Row 11 (C$\to$)** | Kích thước gối & chiều dài nhịp | `Cột: 400, Nhịp: 10400...` | Defines support widths $C_k$ and span clear lengths $L_{n,i}$. |
| **Rows 13–16** | Thép gia cường trên (Top Add) | `C: 6f25, E: 6f20, K: 2f20;2f16` | Additional top bars over support columns. Supports compound notation (e.g. `2f20;2f16`). Layer 1 (rows 13-14), Layer 2 (row 15), Layer 3 (row 16). |
| **Rows 17–18** | Thép gia cường dưới (Bot Add) | `D: 6f25, F: 2f20, L: 2f20` | Additional bottom bars in spans. Layer 1 (row 18), Layer 2 (row 17). Placed with $L/7$ cutoffs from support faces. |
| **Row 19** | Giật mép trên / Thép | `H: -50, L: 3f20, N: -200` | Elevation drop of top surface ($\Delta Z$) or top bar modification. |
| **Row 20 / E5 / G4** | Bề rộng dầm giao / Cốt giá | `F: 0f12, L: 300, N: 1f12` | Web skin / side bars. Explicit count and diameter take precedence over the $h \ge 700\text{ mm}$ rule. |
| **Row 21** | Giật mép dưới / Lệch dầm giao | `F: 400, L: 3f20, N: 0` | Bottom soffit drop / bottom step across spans. |
| **Row 22 (Nhịp)** | Đai chịu lực | `"a100/200/50"` or `"a150"` | Stirrup spacing along span: dense start (100), sparse mid (200), dense end (50). |
| **G6 / G7 / G8** | Đai mặc định (D / S1 / S2) | `G6: 10, G7: a150, G8: a200` | Default stirrup diameter (10 mm), dense spacing (150 mm), sparse spacing (200 mm). |
| **Rows 25–27** | Phân loại đai (Stirrup Types) | `Row 25: Đai □, Row 26: Đai U, Row 27: Đai C` | Multiple stirrup branch configurations: closed hoop, open cap, cross tie. |

---

## 4. Domain Model Architecture for `KataRebar`

To keep `HPRebar.Core` modular, clean, and decoupled from both Revit API and Excel COM, we establish a dedicated namespace: **`HPRebar.Core.KataRebar`**.

```
HPRebar.Core/
└── KataRebar/
    ├── Models/
    │   ├── KataBarItem.cs
    │   ├── KataStirrupSpec.cs
    │   ├── KataSpanRebarSpec.cs
    │   ├── KataSupportRebarSpec.cs
    │   ├── KataBeamRebarSpec.cs
    │   ├── KataRebarCurve.cs
    │   ├── KataStirrupZoneResult.cs
    │   ├── KataRebarLayoutResult.cs
    │   └── Enums.cs
    └── Calculators/
        ├── KataBarNotationParser.cs
        └── KataRebarCalculator.cs
```

### 4.1 Class Designs & Data Structures

#### `KataBarItem` (Parsed bar notation)
```csharp
namespace HPRebar.Core.KataRebar.Models;

/// <summary>
/// A single group of reinforcing bars (e.g., 2f20, 3d18).
/// </summary>
public sealed record KataBarItem(int Count, double DiameterMm, string RawNotation = "")
{
    public double TotalAreaMm2 => Count * Math.PI * (DiameterMm / 2.0) * (DiameterMm / 2.0);
}
```

#### `KataBarNotationParser` (Text parser for Vietnamese structural notations)
```csharp
namespace HPRebar.Core.KataRebar.Calculators;

public static class KataBarNotationParser
{
    // Regex matching "2f18", "3d20", "2phi22", "6d25"
    private static readonly Regex BarPattern = new(@"(\d+)\s*(?:f|d|phi|Ø|Φ)\s*(\d+)", RegexOptions.IgnoreCase);

    // Regex matching stirrup spacing: "a150", "a100/200", "a100/200/50", "150"
    private static readonly Regex StirrupPattern = new(@"a?(\d+)(?:/(\d+))?(?:/(\d+))?", RegexOptions.IgnoreCase);

    public static IReadOnlyList<KataBarItem> ParseBarList(string text);
    public static (double DenseStart, double SparseMid, double DenseEnd) ParseStirrupSpacing(string text, double defaultDense = 150, double defaultSparse = 200);
    public static (double DropMm, IReadOnlyList<KataBarItem> Bars) ParseOffsetAndBars(string text);
    public static (double CoverMain, double CoverStirrup) ParseCover(string text, double defaultMain = 30, double defaultStirrup = 25);
}
```

#### `KataBeamRebarSpec` (Master input DTO from sheet `Dam`)
```csharp
namespace HPRebar.Core.KataRebar.Models;

public sealed record KataBeamRebarSpec
{
    public string BeamName { get; init; } = string.Empty;
    public int MemberCount { get; init; } = 1;
    public double SectionHeightMm { get; init; } = 600.0;
    public double SectionWidthMm { get; init; } = 300.0;
    public double SlabThicknessMm { get; init; } = 120.0;
    public double LevelElevationMm { get; init; }
    
    // Detailing parameters & multipliers
    public double TensionLapMultiplier { get; init; } = 40.0;
    public double CompressionLapMultiplier { get; init; } = 30.0;
    public double TopCutoffRatioLayer1 { get; init; } = 0.25; // or 0.333
    public double TopCutoffRatioLayer2 { get; init; } = 0.20; // or 0.25
    public double BottomCutoffRatio { get; init; } = 1.0 / 7.0;
    public KataCutoffReference CutoffReference { get; init; } = KataCutoffReference.FromSupportFace;
    public double ConcreteCoverMainMm { get; init; } = 30.0;
    public double ConcreteCoverStirrupMm { get; init; } = 25.0;
    public double MaxStockLengthMm { get; init; } = 11700.0;

    // Continuous longitudinal bars
    public IReadOnlyList<KataBarItem> ContinuousTopBars { get; init; } = Array.Empty<KataBarItem>();
    public IReadOnlyList<KataBarItem> ContinuousBottomBars { get; init; } = Array.Empty<KataBarItem>();

    // Spans and supports
    public IReadOnlyList<KataSpanRebarSpec> Spans { get; init; } = Array.Empty<KataSpanRebarSpec>();
    public IReadOnlyList<KataSupportRebarSpec> Supports { get; init; } = Array.Empty<KataSupportRebarSpec>();
}
```

#### `KataRebarCurve` (3D Physical Rebar Output)
```csharp
namespace HPRebar.Core.KataRebar.Models;

public sealed record KataRebarCurve
{
    public int BarId { get; init; }
    public KataBarRole Role { get; init; }
    public double DiameterMm { get; init; }
    public int Layer { get; init; } = 1;
    public Polyline3 Polyline { get; init; } = new();
    public HookAngle StartHookAngle { get; init; } = HookAngle.None;
    public HookAngle EndHookAngle { get; init; } = HookAngle.None;
    public double StartHookLengthMm { get; init; }
    public double EndHookLengthMm { get; init; }
    public double TransverseYMm { get; init; }
    public int HostSpanIndex { get; init; } = -1;
    public int HostSupportIndex { get; init; } = -1;
    public double TotalLengthMm => Polyline.TotalLength + StartHookLengthMm + EndHookLengthMm;
}
```

#### `KataStirrupZoneResult` (Structured Stirrup Layout)
```csharp
namespace HPRebar.Core.KataRebar.Models;

public sealed record KataStirrupZoneResult
{
    public int SpanIndex { get; init; }
    public int ZoneIndex { get; init; } // 0 = Dense Start, 1 = Sparse Mid, 2 = Dense End
    public string ZoneName { get; init; } = string.Empty;
    public double StartStationX { get; init; }
    public double EndStationX { get; init; }
    public double SpacingMm { get; init; }
    public int Count { get; init; }
    public IReadOnlyList<double> Stations { get; init; } = Array.Empty<double>();
    public double OutToOutWidthMm { get; init; }
    public double OutToOutHeightMm { get; init; }
    public KataStirrupType StirrupType { get; init; } = KataStirrupType.ClosedHoop;
}
```

#### `KataRebarLayoutResult` (Complete Calculation Output)
```csharp
namespace HPRebar.Core.KataRebar.Models;

public sealed record KataRebarLayoutResult
{
    public string BeamName { get; init; } = string.Empty;
    public double TotalLengthMm { get; init; }
    public IReadOnlyList<KataRebarCurve> LongitudinalBars { get; init; } = Array.Empty<KataRebarCurve>();
    public IReadOnlyList<KataStirrupZoneResult> StirrupZones { get; init; } = Array.Empty<KataStirrupZoneResult>();
    public IReadOnlyList<KataRebarCurve> IndividualStirrups { get; init; } = Array.Empty<KataRebarCurve>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public bool IsValid => Warnings.Count == 0;
    public int TotalBarCount => LongitudinalBars.Count + IndividualStirrups.Count;
    public double TotalSteelWeightKg { get; init; }
}
```

---

## 5. Master Calculation Pipeline (`KataRebarCalculator`)

The calculation pipeline transforms input specification and beam geometry through seven distinct phases:

```
[KataBeamRebarSpec] + [Revit Beam Geometry / ContinuousStack]
                         │
                         ▼
        ┌───────────────────────────────────┐
        │ Step 1: Input Validation & Stack  │
        │ Synthesis (Lengths, Spans, Nodes) │
        └───────────────────────────────────┘
                         │
                         ▼
        ┌───────────────────────────────────┐
        │ Step 2: Continuous Main Top &     │
        │ Bottom Bars (Hooks 90°, Splices)  │
        └───────────────────────────────────┘
                         │
                         ▼
        ┌───────────────────────────────────┐
        │ Step 3: Support Top Additional    │
        │ Bars (Negative Moment L/3, L/4)   │
        └───────────────────────────────────┘
                         │
                         ▼
        ┌───────────────────────────────────┐
        │ Step 4: Span Bottom Additional    │
        │ Bars (Positive Moment L/7 Cutoff) │
        └───────────────────────────────────┘
                         │
                         ▼
        ┌───────────────────────────────────┐
        │ Step 5: Web Skin / Side Bars &    │
        │ Transverse Cross-Ties (h >= 700)  │
        └───────────────────────────────────┘
                         │
                         ▼
        ┌───────────────────────────────────┐
        │ Step 6: 3-Zone Stirrup Run        │
        │ Generation (□ Outer, U Cap, C Tie)│
        └───────────────────────────────────┘
                         │
                         ▼
        ┌───────────────────────────────────┐
        │ Step 7: Assembly of 3D Curves,    │
        │ Schedule Totals & Result Packaging│
        └───────────────────────────────────┘
                         │
                         ▼
             [KataRebarLayoutResult]
                         │
        (Handed to Revit Add-In Layer via PointMapper)
                         │
                         ▼
      [Rebar.CreateFromCurves / CreateFromShape]
```

### Pipeline Step Breakdown:

#### Step 1: Stack Synthesis & Span Validation
- Synthesize an internal `BeamContinuousStack` from `KataBeamRebarSpec.Spans` and `Supports`.
- If an existing measured `BeamContinuousStack` is passed from Revit:
  - Assert `spec.Spans.Count == measuredStack.Spans.Count`.
  - Validate clear span lengths within physical tolerance ($\pm 50\text{ mm}$). If differences exist, log informative warnings in `KataRebarLayoutResult.Warnings`.

#### Step 2: Continuous Top & Bottom Main Bars
- Extract top bar items (e.g., `6f25` $\to 6$ bars of $d = 25\text{ mm}$) and bottom bar items.
- Calculate transverse $Y$ coordinates across width $b$ centered at $0$.
- Formulate 3D polylines:
  - **Top bars**: Continuous across all spans. At exterior ends, 90° hook down into support column ($L_{\text{hook}} = \min(h - 2c - 2d_{\text{stirrup}}, 30 d)$).
  - **Bottom bars**: Continuous across interior spans. At exterior supports, 90° hook up. At cantilever ends, stop at interior column face.
  - If length $> 11,700\text{ mm}$: Split with 50% staggered lap splices ($L_{\text{lap}} = \text{TensionLapMultiplier} \times d$; top bars in midspan, bottom bars at supports).

#### Step 3: Support Top Additional Bars (Negative Moment)
- For each support $k = 0 \dots N$:
  - Layer 1 bars (rows 13–14):
    - Extension into adjacent clear spans: $L_{\text{ext},1} = r_1 \times L_n$ ($r_1$ from $H5$ or default $0.25$ / $0.333$).
    - At exterior supports: 90° hook down.
    - At interior supports: Straight bar through column width ($L_{\text{total}} = L_{\text{ext,left}} + C + L_{\text{ext,right}}$).
  - Layer 2 bars (row 15):
    - Extension: $L_{\text{ext},2} = r_2 \times L_n$ ($r_2 = 0.20$ or $0.25$, shorter than Layer 1).
    - Elevation: $Z_2 = Z_1 - \text{LayerGap}$ ($\Delta Z = \max(50\text{ mm}, d + 30\text{ mm})$).
  - Layer 3 bars (row 16, if specified):
    - Elevation: $Z_3 = Z_2 - \text{LayerGap}$.

#### Step 4: Span Bottom Additional Bars (Positive Moment)
- For each span $i = 0 \dots M-1$:
  - Layer 1 bars (row 18):
    - Cutoff distance from support face: $d_{\text{cut}} = r_{\text{cut}} \times L_n$ (default $r_{\text{cut}} = 1/7$).
    - $X_{\text{start}} = X_{\text{leftFace}} + d_{\text{cut}}$, $X_{\text{end}} = X_{\text{rightFace}} - d_{\text{cut}}$.
    - Elevation: $Z_1 = Z_{\text{bot}} + c + d_{\text{stirrup}} + d_{\text{bar}}/2$.
  - Layer 2 bars (row 17):
    - Elevation: $Z_2 = Z_1 + \text{LayerGap}$.

#### Step 5: Web Skin / Side Bars & Cross-Ties
- Check if specified in `spec.Spans[i].SideBars` (from row 20 / E5 / G4), or automatically triggered if $h \ge 700\text{ mm}$.
- Calculate number of rows with vertical spacing $\le 300\text{ mm}$.
- Generate straight longitudinal bars on left and right lateral faces.
- Generate transverse cross-ties connecting pairs if enabled.

#### Step 6: 3-Zone Stirrup Run Generation
- For each span $i$:
  - Read $s_1$ (dense near supports), $s_2$ (sparse midspan), $s_3$ (dense end).
  - If cantilever span: uniform dense spacing across entire span.
  - If normal span:
    - Zone 1 (support start): length $L_n / 4$, first stirrup at $s_0 = 50\text{ mm}$, spacing $s_1$.
    - Zone 3 (support end): length $L_n / 4$, spacing $s_3$ (or $s_1$).
    - Zone 2 (midspan): symmetrically placed in the remaining gap with spacing $s_2$.
  - Output structured `KataStirrupZoneResult` runs (for Revit `ScaleToBox` + `SetLayoutAsNumberWithSpacing`).
  - Output explicit 3D `Polyline3` loops for each individual stirrup position (for unit tests and UI elevation/3D canvas rendering).
  - Assign stirrup type: closed outer hoop (□), open cap (U), or cross-tie (C) based on rows 25–27.

#### Step 7: Packaging & Scheduling
- Compile all curves into `KataRebarLayoutResult`.
- Compute total steel length and estimated weight per bar diameter ($W = \frac{d^2}{162} \times L$).
- Validate that all curves satisfy `minSegmentLength >= 1.0 mm` via `Polyline3.Simplify()`.

---

## 6. Zero-Revit Purity & `netstandard2.0` Compliance

To uphold the core architectural mandate of `HPRebar.Core`:
1. **No External Assemblies**: `HPRebar.Core.csproj` targets `netstandard2.0` with only the `Polyfill` NuGet package. It references zero Autodesk Revit DLLs (`RevitAPI.dll`, `RevitAPIUI.dll`).
2. **Deterministic Mathematical Types**: Coordinates use `double`, `Point3`, `Vector3`, `Polyline3`, and standard C# records.
3. **Short Curve Tolerance Safety**: Revit throws an uncatchable fatal exception when creating curves shorter than `Application.ShortCurveTolerance` (~0.78 mm / 1/32"). All generated polylines pass through `Polyline3.Simplify(minSegmentLength: 1.0)`, merging micro-segments and removing redundant collinear vertices before export.
4. **Isolated Coordinate Mapping**: The mapping from local millimetres to Revit world coordinates (feet) is strictly performed in `HPRebar` (the add-in layer) using `PointMapper`:
   ```csharp
   XYZ worldPoint = pointMapper.ToXyz(localPoint3);
   ```
   The Core math engine has zero awareness of Revit decimal feet.

---

## 7. Proposed Unit Testing Strategy (`HPRebar.Core.Tests/KataRebar/`)

Unit tests can run 100% autonomously without opening Revit or Excel using `xunit.v3` under `dotnet test HPRebar.Core.Tests`.

### Test Suite Structure:
1. **`KataBarNotationParserTests`**:
   - Single bar notation: `"2f18"` $\to (2, 18)$, `"3d20"` $\to (3, 20)$, `"6f25"` $\to (6, 25)$.
   - Compound bar notation: `"2f20;2f16"` $\to [(2, 20), (2, 16)]$, `"2f20+1f18"` $\to [(2, 20), (1, 18)]$.
   - Empty / zero inputs: `""`, `"0"`, `"-"` $\to$ Empty.
   - Stirrup notation: `"a150"` $\to (150, 150, 150)$, `"a100/200"` $\to (100, 200, 100)$, `"a100/200/50"` $\to (100, 200, 50)$.
   - Drop and cover parsing: `"100;5f25"`, `"50/25"`.
2. **`KataRebarCalculatorTests`**:
   - **Single-Span Beam**: Standard beam ($6\text{ m}$, $300 \times 600$), verifies 90° hooks at both ends, 3-zone stirrup count, and correct bottom bar cutoff.
   - **Multi-Span Continuous Beam (Golden Case from Kata.xlsm)**: 6 spans, spans ranging from $2\text{ m}$ to $10.4\text{ m}$, $6f25$ main bars, variable support top bars (`6f25`, `6f20`, `2f20;2f16`), cantilever end span. Verifies staggered lap splices when span $> 11.7\text{ m}$.
   - **Cantilever Left & Right**: Verifies bottom bars terminate at interior support face and stirrups are 100% dense along the cantilever.
   - **Deep Beam Skin Bars**: $h = 800\text{ mm}$ and $1100\text{ mm}$, verifies correct row count ($\le 300\text{ mm}$ spacing) and placement of cross-ties.
   - **Variable Depth / Step Beam**: Spans with differing $h$ or top drops ($\Delta Z$), verifying step hooks.
   - **Boundary & Robustness Checks**: Spans shorter than $600\text{ mm}$, zero-width joints, and very large bars.

---

## 8. Summary Comparison Table: Existing vs New Components

| Requirement / Feature | Existing in `HPRebar.Core/BeamRebar` | New in `HPRebar.Core/KataRebar` | Integration Note |
|---|---|---|---|
| **3D Vector & Point Math** | `Point3`, `Vector3`, `Polyline3` | None needed | Reused directly from `HPRebar.Core.BeamRebar.Models`. |
| **Numerical Tolerance** | `Tolerance` | None needed | Reused directly. |
| **Sheet Dam Specification** | None | `KataBeamRebarSpec`, `KataSpanRebarSpec`, `KataSupportRebarSpec` | New strongly-typed DTOs representing parsed Excel data. |
| **Notation Parser** | None | `KataBarNotationParser` | New regex parser for `2f18`, `2f20;2f16`, `a100/200`. |
| **3-Zone Stirrups** | `BeamStirrupDistributionCalculator` | `KataStirrupSpec`, `KataStirrupZoneResult` | Reuses core algorithm, adds multi-shape tagging (□, U, C). |
| **Continuous Bars** | `BeamMainBarCalculator` | Adapted in `KataRebarCalculator` | Enforces 90° hooks, stock length splices, cantilever rules. |
| **Top Additional Bars** | `BeamAdditionalBarCalculator` | Adapted in `KataRebarCalculator` | Incorporates sheet cutoff ratios ($H3, H5$) and compound layers. |
| **Bottom Additional Bars** | `BeamAdditionalBarCalculator` | Adapted in `KataRebarCalculator` | Incorporates $L/7$ clear face cutoffs and multi-layer gaps. |
| **Side / Skin Bars** | `BeamSideBarCalculator` | Adapted in `KataRebarCalculator` | Honors explicit sheet row 20 specification or $h \ge 700\text{ mm}$ trigger. |
| **Output Container** | `BarPolyline`, `StirrupRun` | `KataRebarLayoutResult`, `KataRebarCurve` | Comprehensive layout packaging for downstream Revit generation. |
