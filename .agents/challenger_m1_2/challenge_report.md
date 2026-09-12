# Challenge Report — challenger_m1_2

## Challenge Summary

**Overall risk assessment**: HIGH  
**Verdict**: `CHALLENGE_FAILED` (Critical geometric defects, boundary penetration, and code violation bugs discovered)

The pure domain logic of `HPRebar.Core/BeamRebar/` exhibits clean architectural modularity and zero Revit runtime dependencies. However, empirical stress-testing and mathematical analysis revealed critical invariant breakdowns in stirrup zone transitions, secondary framing intersections, skin reinforcement spacing, and polyline simplification.

---

## Challenges

### [High] Challenge 1: Duplicate Stirrups (0.0 mm Distance) at 3-Zone Boundaries in `BeamStirrupDistributionCalculator`

- **Location**: `HPRebar.Core/BeamRebar/Calculators/BeamStirrupDistributionCalculator.cs:122-179`
- **Assumption Challenged**: Zone 1 (dense), Zone 2 (sparse), and Zone 3 (dense) can be computed independently using centering slack $\Delta = (L_{dist} - n \cdot s) / 2$ without coordinating boundary coordinates between adjacent zones.
- **Attack Scenario / Empirical Proof**:
  - Let clear span $L_n = 6200.0\text{ mm}$, layout `ThreeZoneL4` ($L_1 = 1550\text{ mm}$, $L_2 = 3100\text{ mm}$, $L_3 = 1550\text{ mm}$), $s_{dense} = 100\text{ mm}$, $s_{sparse} = 100\text{ mm}$ (or $310\text{ mm}$), start offset $= 50\text{ mm}$.
  - For Zone 1: $L_{dist1} = 1550 - 50 = 1500\text{ mm}$. $\lfloor 1500 / 100 \rfloor = 15$ intervals, $\Delta_1 = (1500 - 1500)/2 = 0.0$.
    - Zone 1 last position: $X = 50.0 + \Delta_1 + 15 \times 100 = 1550.0\text{ mm}$.
  - For Zone 2: $L_2 = 3100\text{ mm}$. $\lfloor 3100 / 100 \rfloor = 31$ intervals, $\Delta_2 = (3100 - 3100)/2 = 0.0$.
    - Zone 2 first position: $X = L_1 + \Delta_2 = 1550.0 + 0.0 = 1550.0\text{ mm}$.
  - **Distance between Zone 1 last bar and Zone 2 first bar is exactly $1550.0 - 1550.0 = 0.0\text{ mm}$!**
  - Similarly between Zone 2 and Zone 3:
    - Zone 2 last position: $X = 1550 + 31 \times 100 = 4650.0\text{ mm}$.
    - Zone 3 first position: $X = (6200 - 1550) + \Delta_1 = 4650.0 + 0.0 = 4650.0\text{ mm}$.
    - **Distance between Zone 2 last bar and Zone 3 first bar is exactly $0.0\text{ mm}$!**
- **Blast Radius**: Two stirrups occupy the exact identical spatial coordinates. In Revit API, creating overlapping rebars corrupts the schedule/BOM count (double-counting stirrups at support-midspan boundaries) and fails physical clash-detection. Even when $\Delta_1 + \Delta_2 > 0$, the boundary spacing is $\Delta_1 + \Delta_2$, which can be arbitrarily small (e.g. 5–15 mm), creating an unphysical stirrup pair.
- **Mitigation**: Calculate continuous stations across zone boundaries or ensure that Zone 2 starts at $X_{first} \ge X_{zone1\_last} + s_{sparse}$ (or $s_{dense}$), avoiding redundant boundary stirrups.

---

### [High] Challenge 2: Secondary Framing Hanging Stirrups and 45° Diagonal Bent Ties Penetrate Support Nodes

- **Location**: `HPRebar.Core/BeamRebar/Calculators/BeamSpecialBarCalculator.cs:122-162` and `lines 77-105`
- **Assumption Challenged**: Secondary framing intersections only need to validate that `sec.CenterX` is inside `hostSpan`.
- **Attack Scenario / Empirical Proof**:
  - Consider `hostSpan` with $\text{StartX} = 200.0\text{ mm}$, $\text{EndX} = 5800.0\text{ mm}$.
  - Secondary beam with width $b_s = 250\text{ mm}$ intersects near support at $X_{center} = 350.0\text{ mm}$ (inside clear span $[200, 5800]$).
  - Left joint face: $x_{secL} = 350 - 125 = 225.0\text{ mm}$.
  - With `HangingStirrupsPerSide = 3`, `spacing = 50 mm`:
    - Station 1: $225 - 150 = 75.0\text{ mm}$ ($< \text{StartX} = 200.0\text{ mm}$).
    - Station 2: $225 - 100 = 125.0\text{ mm}$ ($< \text{StartX} = 200.0\text{ mm}$).
    - Station 3: $225 - 50 = 175.0\text{ mm}$ ($< \text{StartX} = 200.0\text{ mm}$).
    - **All 3 left hanging stirrups are placed inside the support column (or outside the beam)!**
  - For a 45° diagonal bent tie:
    - $\Delta Z = 536\text{ mm} \implies \Delta X = 536\text{ mm}$. Anchorage length $= 30 \times 14 = 420\text{ mm}$.
    - Leftward extension from secondary beam face $= \Delta X + L_{anchor} = 956\text{ mm}$ (~1 meter).
    - If $X_{center} = 800\text{ mm}$, $x_{secL} = 675\text{ mm}$:
    - Tie start coordinate $X = 675 - 956 = -281.0\text{ mm}$!
    - **The diagonal tie extends 281 mm past the outer exterior boundary of the entire continuous beam run into air!**
- **Blast Radius**: Rebars are generated outside the structural host volume, clashing with vertical column reinforcement or protruding into external space. Downstream Revit `Rebar.CreateFromCurves` will either fail host validation or produce illegal freeform rebar models.
- **Mitigation**: Check if flanking stations $X \ge \text{hostSpan.StartX} + \text{cover}$ and $X \le \text{hostSpan.EndX} - \text{cover}$. Cull or clamp stations that violate span bounds. For diagonal bent ties, clamp anchor horizontal legs to `hostSpan.StartX + cover` or throw a geometric validation warning if the secondary joint is too close to a column support to develop a 45° bent bar.

---

### [Medium] Challenge 3: Side Bar Vertical Spacing Exceeds 300 mm Code Limit for $H \in [700, 800]$ mm

- **Location**: `HPRebar.Core/BeamRebar/Calculators/BeamSideBarCalculator.cs:35` and `lines 60-63`
- **Assumption Challenged**: `ComputeRowCount = (int)Math.Ceiling((heightMm - 600.0) / 200.0)` satisfies the code requirement $s \le 300\text{ mm}$ (`MaxVerticalSpacingMm`).
- **Attack Scenario / Empirical Proof**:
  - For $H = 800\text{ mm}$, cover $= 25\text{ mm}$, stirrup $= 8\text{ mm}$, main bar $= 20\text{ mm}$:
    - $z_{botMain} = 25 + 8 + 10 = 43\text{ mm}$.
    - $z_{topMain} = 800 - 43 = 757\text{ mm}$.
    - Distance between top and bottom main bars $= 757 - 43 = 714\text{ mm}$.
    - `ComputeRowCount(800)` returns $\lceil (800 - 600)/200 \rceil = 1$ row.
    - Vertical pitch $\Delta Z = 714 / (1 + 1) = \mathbf{357.0\text{ mm}}$!
    - **$357.0\text{ mm} > 300.0\text{ mm}$ (`MaxVerticalSpacingMm = 300.0`)!**
  - For $H = 700\text{ mm}$:
    - $z_{topMain} - z_{botMain} = 614\text{ mm}$.
    - `ComputeRowCount(700)` returns 1 row.
    - $\Delta Z = 614 / 2 = \mathbf{307.0\text{ mm}} > 300.0\text{ mm}$!
- **Blast Radius**: Non-compliance with TCVN 5574:2018 §10.3.2 and ACI 318 §9.7.2.3 which legally require skin reinforcement pitch $s \le 300\text{ mm}$. Structural engineering sign-off would reject the reinforcement detailing.
- **Masking Test Discovery**: In `BeamSideBarCalculatorTests.cs:52-63`, the test `SideBarVerticalSpacingNeverExceedsThreeHundredMillimetres` only checked $H = 1200\text{ mm}$ and only checked spacing between adjacent side bars (`leftBars[i+1] - leftBars[i]`). For $H = 800\text{ mm}$, with only 1 row, `leftBars.Count = 1`, so the loop executed zero times, masking this 357 mm spacing violation!
- **Mitigation**: Derive row count directly from clear vertical span between main bars:
  `int nRows = (int)Math.Max(1, Math.Ceiling((zTopMain - zBotMain) / MaxVerticalSpacingMm) - 1);`

---

### [Medium] Challenge 4: Hairpin 180° Direction Reversal Point Culled by `SimplifyPolyline`

- **Location**: `HPRebar.Core/BeamRebar/Calculators/BeamMainBarCalculator.cs:424-432`
- **Assumption Challenged**: Collinear vertices can be identified solely by `v1.Cross(v2).Length <= Tolerance.CollinearToleranceMm`.
- **Attack Scenario / Empirical Proof**:
  - Consider a hairpin hook or 180° return tie: $A = (0, 0, 0)$, $B = (100, 0, 0)$, $C = (50, 0, 0)$.
  - $v_1 = (1, 0, 0)$, $v_2 = (-1, 0, 0)$.
  - $v_1 \times v_2 = (0, 0, 0) \implies \text{cross.Length} = 0 \le \text{Tolerance.CollinearToleranceMm}$.
  - The turn vertex $B = (100, 0, 0)$ is culled as "intermediate collinear".
  - The simplified polyline becomes $(0, 0, 0) \to (50, 0, 0)$, collapsing the 180° hook entirely!
- **Blast Radius**: Latent defect for 90° hooks, but critical failure mode if 180° hooks, seismic hoops, or return bends are simplified through this method.
- **Mitigation**: Require positive dot product for collinear culling:
  `if (cross.Length > Tolerance.CollinearToleranceMm || v1.Dot(v2) < 0.0) simplified.Add(pCurr);`

---

### [Low] Challenge 5: Tautological Dummy Tests in `BeamMainBarCalculatorTests`

- **Location**: `HPRebar.Core.Tests/BeamRebar/BeamMainBarCalculatorTests.cs:233-249`
- **Observation**:
  ```csharp
  [Fact]
  public void MultiLayerTopBarsOffsetSecondLayerVerticallyWithSpecifiedGap()
  {
      double z1 = 3600 - 25 - 8 - 10;
      double z2 = z1 - 50.0;
      Assert.Equal(50.0, z1 - z2, Precision);
  }

  [Fact]
  public void MultiLayerBottomBarsOffsetSecondLayerVerticallyUpwards()
  {
      double z1 = 0 + 25 + 8 + 10;
      double z2 = z1 + 50.0;
      Assert.True(z2 > z1);
  }
  ```
- **Vulnerability**: These tests assert tautological local arithmetic (`50.0 == 50.0` and `z2 > z1`) without invoking any domain calculator or model.
- **Blast Radius**: Zero functional impact on production, but represents degraded test integrity / illusory code coverage.
- **Mitigation**: Replace with authentic assertions invoking `BeamAdditionalBarCalculator` or `BeamMainBarCalculator` testing actual multi-layer offsets.

---

### [Low] Challenge 6: NaN and PositiveInfinity Bypass Input Validation in `BeamCanvasTransformCalculator`

- **Location**: `HPRebar.Core/BeamRebar/Calculators/BeamCanvasTransformCalculator.cs:61-68`
- **Vulnerability**: Checks `if (canvasWidthPx <= 0.0)` evaluate to `false` when input is `double.NaN` or `double.PositiveInfinity` (standard IEEE 754 behavior). WPF Canvases report `NaN` before initial arrange, or `PositiveInfinity` during unconstrained measure passes.
- **Blast Radius**: Transform matrices produce `Scale = NaN` or `Infinity`, triggering downstream WPF layout rendering crashes.
- **Mitigation**: Add `if (double.IsNaN(canvasWidthPx) || double.IsInfinity(canvasWidthPx) || canvasWidthPx <= 0.0)`.

---

## Stress Test Results

| Scenario | Expected Behavior | Actual Behavior | Result |
|---|---|---|---|
| 3-Zone stirrups with $L_{dist1} = 1500$, $s = 100$ | Zone 2 spaced $\ge 100$ mm from Zone 1 | Duplicate stirrup at $X = 1550.0$ mm (0.0 mm gap) | **FAIL** |
| Secondary beam $X_{center} = 350$ mm near column face | Hanging stirrups clamped within $[200, 5800]$ | 3 stirrups generated at $X \in \{75, 125, 175\}$ inside column | **FAIL** |
| 45° diagonal bent tie with $X_{center} = 800$ mm | Tie clamped within beam bounds | Tie tip extends to $X = -281$ mm (outside beam) | **FAIL** |
| Deep beam $H = 800$ mm skin reinforcement | Vertical spacing $\le 300$ mm | 1 row generated, spacing $= 357.0$ mm | **FAIL** |
| Deep beam $H = 700$ mm skin reinforcement | Vertical spacing $\le 300$ mm | 1 row generated, spacing $= 307.0$ mm | **FAIL** |
| 180° hairpin polyline $(0,0,0) \to (100,0,0) \to (50,0,0)$ | Preserve apex at $(100,0,0)$ | Apex culled, line becomes $(0,0,0) \to (50,0,0)$ | **FAIL** |
| Canvas transform elevation round-trip | Inverse recovers exact model coords | Exact round-trip within $10^{-10}$ mm | **PASS** |
| Aspect ratio preservation on canvas | Uniform isotropic scale factor | Verified isotropic scale preservation | **PASS** |
| Revit API $N > 1002$ stirrups guard | Throws `ArgumentOutOfRangeException` | Throws `ArgumentOutOfRangeException` | **PASS** |
| Short span link beam ($< 600$ mm) | 3-Zone collapses to Uniform | Verified fallback to Uniform | **PASS** |

---

## Unchallenged Areas

- **Revit TransactionGroup & API creation**: M3 scope (`HPRebar/Beam Rebar/`), not part of M1/M2 domain core.
- **Revit In-Process TUnit runner**: Excluded from CLI solution build per AGENTS.md, requires active Revit 2026 process.
