# Milestone 2 & 3 Implementation Report: Core Rebar Geometry & Distribution Calculator

- **Agent**: `worker_m2` (Core Rebar Geometry & Distribution Calculator Implementer)
- **Roles**: implementer, qa, specialist
- **Target Subsystem**: `HPRebar.Core/KataRebar/Calculators/KataRebarCalculator.cs` and `HPRebar.Core.Tests/KataRebar/KataRebarCalculatorTests.cs`
- **Target Framework**: `netstandard2.0` (Core) / `net8.0` (Tests) / `net8.0-windows7.0` (Add-In)
- **Date**: 2026-09-27

---

## 1. Overview & Objectives

This report documents the implementation and verification of **Milestone 2 (Core Rebar Geometry Calculator)** and **Milestone 3 (Pure Domain Unit Test Suite)** for the `KataRebar` feature in `HPRebar`.

The objective was to transform raw structural detailing data encapsulated in `KataBeamRebarSpec` into explicit 3D rebar curve geometry (`KataRebarLayoutResult`), strictly maintaining `netstandard2.0` purity with **zero references to `Autodesk.Revit.*`**, while enforcing standard Vietnamese and international reinforced concrete detailing practices (TCVN 5574:2018 / ACI 318).

---

## 2. Technical Implementation Details

### 2.1 Coordinate System & Beam Longitudinal Stations
All calculations operate in the continuous beam's local Cartesian coordinate system:
- **$X$ (Longitudinal Station)**: Station $X = 0.0$ starts at the exterior left face of Support 0. For each alternating support $k$ and span $s$:
  - Support $k$ has left face $X_{\text{suppLeft}}[k]$, right face $X_{\text{suppRight}}[k] = X_{\text{suppLeft}}[k] + C_k$, and center $X_{\text{suppCenter}}[k]$.
  - Clear span $s$ starts at $X_{\text{spanStart}}[s] = X_{\text{suppRight}}[s]$ and ends at $X_{\text{spanEnd}}[s] = X_{\text{spanStart}}[s] + L_{n,s}$.
  - Total beam length $L_{\text{total}} = \sum C_k + \sum L_{n,s}$.
- **$Y$ (Transverse)**: Centered at $Y = 0.0$ along the beam longitudinal axis. Lateral concrete faces sit at $Y = -b/2$ and $Y = +b/2$. Transverse bar positions are symmetrically centered via `ComputeTransverseYPositions(b, c, d_stirrup, d_bar, count)`.
- **$Z$ (Elevation)**: Top beam surface is $Z_{\text{top}} = 0.0$; soffit is $Z_{\text{bot}} = -h$.

### 2.2 Continuous Main Longitudinal Bars
1. **Top Continuous Main Bars**:
   - Run across the entire continuous beam length from $X = 0 + c_{\text{stirrup}}$ to $X = L_{\text{total}} - c_{\text{stirrup}}$.
   - Vertical elevation: $Z_{\text{top\_bar}} = Z_{\text{top}} - c_{\text{stirrup}} - d_{\text{stirrup}} - d_{\text{bar}} / 2$.
   - Exterior anchorage: 90° hook legs bent downwards at both ends ($L_{\text{hook}} = \min(h - 2c - 2d_{\text{stirrup}}, \max(30d, 200\text{ mm}))$).
   - In cantilever spans (support width $\le 0$), top continuous bars extend to the cantilever tip with 90° hook down.
2. **Bottom Continuous Main Bars**:
   - Standard exterior support: starts at $X = 0 + c_{\text{stirrup}}$ with 90° hook bent upwards into column.
   - Cantilever overhang: bottom bars stop at the interior column face ($X = X_{\text{suppLeft}}[1]$ for left cantilever, $X = X_{\text{suppRight}}[N-1]$ for right cantilever) with no hook.
   - Vertical elevation: $Z_{\text{bot\_bar}} = Z_{\text{bot}} + c_{\text{stirrup}} + d_{\text{stirrup}} + d_{\text{bar}} / 2$.

### 2.3 Support Top Additional Bars (Negative Moment)
- Multi-layer vertical offsets:
  - Top continuous bar elevation: $Z_{\text{top\_cont}}$.
  - Layer $m \in \{1, 2, 3, 4\}$ is offset downwards cumulatively by $\Delta Z = \max(d_{\text{layer}} + 30.0, 50.0)$, guaranteeing at least $50\text{ mm}$ clear vertical gap.
- Span cutoff extensions:
  - Exterior supports: $L_{\text{cutoff}} = L_{n} \times \text{ratio}$, with 90° hook down into column at exterior end, extending into span.
  - Interior supports: $L_{\text{cutoff}} = \max(L_{\text{left}}, L_{\text{right}}) \times \text{ratio}$, extending symmetrically into both adjacent spans as straight bars without hooks.
  - Cutoff origin is configurable via `KataCutoffOrigin` (`FromColumnFace` vs `FromColumnCenter`).

### 2.4 Span Bottom Additional Bars (Positive Moment)
- In each clear span $s$:
  - Clear span length $L_n = \text{Spans}[s].\text{Length}$.
  - Cutoffs: $L_n / 7 \approx 0.143 \times L_n$ inward from left and right clear span faces:
    $$X_{\text{start}} = X_{\text{spanStart}}[s] + \frac{L_n}{7}, \quad X_{\text{end}} = X_{\text{spanEnd}}[s] - \frac{L_n}{7}$$
  - Multi-layer vertical offsets: Layer 1 and Layer 2 are offset upwards cumulatively above bottom continuous bars by $\Delta Z \ge 50\text{ mm}$.

### 2.5 Side Bars / Web Skin Reinforcement
- Triggered when $h \ge 700\text{ mm}$ OR `spec.GlobalSideBars.Count > 0` OR `span.SideBars.Count > 0`.
- Supports span override (e.g. `0f12` in row 20 suppresses side bars in that span).
- Automatic row count calculation:
  $$H_{\text{clear}} = h - 2 \times (c + d_{\text{stirrup}} + d_{\text{main}} / 2)$$
  $$n_{\text{rows}} = \max\left(1, \, \left\lceil \frac{H_{\text{clear}}}{300\text{ mm}} \right\rceil - 1\right)$$
  guaranteeing vertical spacing $\le 300\text{ mm}$.
- Symmetrical pairs placed along left ($Y_{\text{left}}$) and right ($Y_{\text{right}}$) lateral faces.

### 2.6 3-Zone Stirrups Distribution
- For each span $s$:
  - Cantilever spans: uniform dense spacing across the cantilever length with $50\text{ mm}$ start offset.
  - Non-cantilever spans:
    - **Zone 1 (Left Support Zone)**: Length $L_n / 4$, dense spacing $s_1$, first stirrup at $50\text{ mm}$ offset from column face.
    - **Zone 3 (Right Support Zone)**: Length $L_n / 4$, dense spacing $s_3$ (or $s_1$), last stirrup at $50\text{ mm}$ offset from column face.
    - **Zone 2 (Midspan Sparse Zone)**: Symmetrically centered within the physical gap between Zone 1 and Zone 3 to eliminate clashes and duplicate stirrups.
  - Multi-branch support: generates Closed Hoop (□, 4 simplified corner vertices with `IsClosed = true`), Cap Stirrup (U, 4 open vertices), and Cross Tie (C, 2 horizontal vertices).
  - Out-to-out dimensions calculated as $b - 2c$ and $h - 2c$.
  - Structured output in `StirrupZones` and 3D physical curves in `IndividualStirrups`.

### 2.7 Polyline Simplification & Safety
- `Polyline3.Simplify(1.0)` is applied to all generated 3D polylines, eliminating redundant and sub-millimetre segments to prevent Revit API `Application.ShortCurveTolerance` (~0.78 mm) fatal exceptions.
- `TotalSteelWeightKg` computed using nominal bar diameters, cut lengths, and steel density $7850\text{ kg/m}^3$.

---

## 3. Test Suite Verification

15 comprehensive xUnit unit tests were implemented in `HPRebar.Core.Tests/KataRebar/KataRebarCalculatorTests.cs`:
1. `Calculate_NullSpec_ThrowsArgumentNullException`: Validates null defense.
2. `Calculate_EmptySpans_ReturnsInvalidLayoutWithWarning`: Validates empty span handling.
3. `Calculate_NonPositiveDimensions_ReturnsWarning`: Validates dimensions guard.
4. `Calculate_SingleSpanBeam_GeneratesContinuousBarsAndStirrupsCorrectly`: Tests 1-span beam, 3f20 top/bottom, 4-vertex polylines, 90° hooks, transverse Y centering, 3-zone stirrups.
5. `Calculate_MultiSpanContinuousBeam_B01KataSample_CalculatesAllReinforcements`: Tests full Kata sample B01 (2 spans, 18.1m total, 34 extra top bars across 3 supports, 10 extra bottom bars, span side bar override suppression, 6 stirrup zones).
6. `Calculate_CantileverOverhangBeam_AnchorsTopAndStopsBottomAtInteriorColumn`: Tests left cantilever overhang beam.
7. `Calculate_RightCantileverOverhangBeam_AnchorsTopAndStopsBottomAtInteriorColumn`: Tests right cantilever overhang beam.
8. `Calculate_BothSidesCantilever_HandlesDoubleOverhang`: Tests double cantilever beam.
9. `Calculate_MultiLayerTopAdditionalBars_OffsetsElevationsAndCalculatesRatios`: Tests 4 top extra bar layers, cumulative vertical gaps $\ge 50\text{ mm}$, decreasing elevations, and cutoff ratios.
10. `Calculate_DeepBeam_AutoGeneratesSideBarsWithMax300mmSpacing`: Tests $h = 1100\text{ mm}$ beam auto-generating 3 rows of symmetrical side bar pairs with spacing $\le 300\text{ mm}$.
11. `Calculate_MultiTypeStirrups_GeneratesClosedHoopCapAndCrossTie`: Tests Closed Hoop (□), Cap Stirrup (U), and Cross Tie (C).
12. `Calculate_AllPolylinesSimplified_ProtectsAgainstRevitShortCurveCrashes`: Asserts every polyline segment in the entire layout has length $\ge 1.0\text{ mm}$.
13. `Calculate_UnequalAdjacentSpans_InteriorSupportTopCutoffUsesMaxSpan`: Tests interior support cutoff using $\max(L_{\text{left}}, L_{\text{right}}) \times \text{ratio}$.
14. `Calculate_TransverseYCentering_GuaranteesSymmetryAcrossWidth`: Tests transverse Y centering for 1 to 6 bars.
15. `Calculate_ShortSpanStirrupDistribution_ValidAndNonOverlapping`: Tests short span stirrup non-overlapping stations.

---

## 4. Verification Results

1. **`HPRebar.Core` Build**:
   ```powershell
   dotnet build HPRebar/HPRebar.Core/HPRebar.Core.csproj
   # Output: Build succeeded. 0 Warning(s), 0 Error(s).
   ```

2. **Full Unit Test Suite (`HPRebar.Core.Tests`)**:
   ```powershell
   dotnet test HPRebar.Core.Tests
   # Output:
   # Test run summary: Passed!
   #   total: 521
   #   failed: 0
   #   succeeded: 521
   #   skipped: 0
   #   duration: 857ms
   ```

3. **Revit Add-In Solution Build**:
   ```powershell
   dotnet build HPRebar.slnx -c Debug.R26 -p:DeployAddin=false
   # Output: Build succeeded. 0 Error(s).
   ```
