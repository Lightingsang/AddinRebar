# Detailed Remediation Plan: Stirrup Zone Boundary Clashing & Side Bar Spacing Violations

**Author**: `explorer_m1_it2_2`  
**Target Milestone**: Milestone 1 Remediation (Pure Domain Logic in `HPRebar.Core`)  
**Target Files**:
- `HPRebar/HPRebar.Core/BeamRebar/Calculators/BeamStirrupDistributionCalculator.cs`
- `HPRebar/HPRebar.Core/BeamRebar/Calculators/BeamSideBarCalculator.cs`
- `HPRebar/HPRebar.Core.Tests/BeamRebar/BeamStirrupDistributionCalculatorTests.cs`
- `HPRebar/HPRebar.Core.Tests/BeamRebar/BeamSideBarCalculatorTests.cs`

---

## 1. Executive Overview

This remediation plan provides an exact, mathematically proven, and engineering-compliant resolution for the two defects identified in the Forensic Audit (`auditor_m1_1`), Adversarial Review (`reviewer_m1_1`), and Challenger (`challenger_m1_2`) reports:
1. **Duplicate Stirrup Clashing (0.0 mm distance)** at 3-zone boundaries in `BeamStirrupDistributionCalculator.cs`.
2. **Skin Reinforcement Vertical Spacing Violations ($\Delta Z > 300.0$ mm)** for beams with $H \in [700, 800]$ mm (and $H = 1000$ mm) in `BeamSideBarCalculator.cs`.

Both fixes preserve pure `netstandard2.0` architecture, zero `Autodesk.Revit.*` dependencies, full API backwards compatibility, and strict adherence to structural codes (TCVN 5574:2018 §10.3.2 and ACI 318-19 §9.7.2.3).

---

## 2. Issue 1: Duplicate Stirrup Collision at 3-Zone Boundaries

### 2.1 Problem Statement & Root Cause Analysis
In `BeamStirrupDistributionCalculator.ComputeSpanRuns`:
- For 3-zone layouts (`ThreeZoneL4` and `ThreeZoneL3`), the clear span $L_n$ is partitioned into theoretical zone lengths $L_1$, $L_2$, and $L_3$ ($L_1 = L_3$).
- Zone 1 distributes dense stirrups with spacing $s_1 = \text{SpacingDense}$ over $L_{dist1} = L_1 - \text{StartOffset}$:
  $$\text{intervals}_1 = \lfloor L_{dist1} / s_1 \rfloor$$
  $$\delta_1 = (L_{dist1} - \text{intervals}_1 \cdot s_1) / 2.0$$
  $$\text{startX}_1 = \text{StartOffset} + \delta_1$$
  $$\text{lastX}_1 = \text{startX}_1 + \text{intervals}_1 \cdot s_1 = L_1 - \delta_1$$
- Zone 2 distributes sparse stirrups with spacing $s_2 = \text{SpacingSparse}$ independently over theoretical length $L_2$:
  $$\text{intervals}_2 = \lfloor L_2 / s_2 \rfloor$$
  $$\delta_2 = (L_2 - \text{intervals}_2 \cdot s_2) / 2.0$$
  $$\text{startX}_2 = L_1 + \delta_2$$
- Zone 3 symmetrically distributes dense stirrups starting at:
  $$\text{startX}_3 = (L_n - L_3) + \delta_1$$

**The Geometric Breakdown**:
The boundary distance between the last stirrup of Zone 1 and the first stirrup of Zone 2 is:
$$\Delta X_{1\to 2} = \text{startX}_2 - \text{lastX}_1 = (L_1 + \delta_2) - (L_1 - \delta_1) = \delta_1 + \delta_2$$
And between Zone 2 and Zone 3:
$$\Delta X_{2\to 3} = \text{startX}_3 - \text{lastX}_2 = ((L_n - L_3) + \delta_1) - ((L_1 + \text{intervals}_2 \cdot s_2)) = \delta_1 + \delta_2$$

When $L_{dist1}$ is an exact integer multiple of $s_1$ and $L_2$ is an exact integer multiple of $s_2$:
$$\delta_1 = 0.0\text{ mm} \quad \text{and} \quad \delta_2 = 0.0\text{ mm} \implies \Delta X_{1\to 2} = 0.0\text{ mm}!$$

**Empirical Proof (Challenger Attack 1)**:
- $L_n = 6200.0$ mm, `ThreeZoneL4` ($L_1 = 1550$ mm, $L_2 = 3100$ mm, $L_3 = 1550$ mm), $s_1 = 100$ mm, $s_2 = 100$ mm (or $310$ mm), $\text{StartOffset} = 50$ mm.
- $L_{dist1} = 1550 - 50 = 1500$ mm. $1500 / 100 = 15 \implies \delta_1 = 0.0$. Last bar of Zone 1: $X = 1550.0$ mm.
- $L_2 = 3100$ mm. $3100 / 100 = 31 \implies \delta_2 = 0.0$. First bar of Zone 2: $X = 1550.0$ mm.
- **Distance $\Delta X = 1550.0 - 1550.0 = 0.0$ mm! Two stirrups occupy the identical spatial coordinate.**
- Even when $\delta_1 + \delta_2 > 0$ (e.g., $L_n = 5600$ mm where $\delta_1 = 25$ mm, $\delta_2 = 0$), the spacing is only 25 mm, violating coarse aggregate clearance ($d_{agg} \approx 20\text{--}25$ mm).

### 2.2 Mathematical Remediation
In structural detailing, Zone 1 is anchored by the support face and ends with a stirrup at $\text{lastX}_1 \le L_1$. Zone 3 is anchored by the opposing support face and begins with a stirrup at $\text{startX}_3 \ge L_n - L_3$.

The interior gap available for Zone 2 between these physical boundary stirrups is:
$$L_{gap} = \text{startX}_3 - \text{lastX}_1$$
Note that because $\text{lastX}_1 = L_1 - \delta_1$ and $\text{startX}_3 = L_n - L_3 + \delta_1 = L_n - L_1 + \delta_1$, this gap is centered at the exact beam midpoint:
$$\frac{\text{lastX}_1 + \text{startX}_3}{2} = \frac{L_n}{2}$$

Within this gap, Zone 2 contains $\text{count}_2$ stirrups spaced internally at $s_2 = \text{SpacingSparse}$.
The number of internal intervals in Zone 2 is $\text{intervals}_2 = \text{count}_2 - 1$.
The transition distance from $\text{lastX}_1$ to the first bar of Zone 2 ($\text{startX}_2$) and from the last bar of Zone 2 ($\text{lastX}_2$) to $\text{startX}_3$ is:
$$d_{boundary} = \frac{L_{gap} - (\text{intervals}_2 \cdot s_2)}{2.0}$$

**Constraint Formulation**:
1. **Upper bound**: Shear capacity requires that the spacing between any two adjacent stirrups in Zone 2 must not exceed the design sparse spacing $s_2$:
   $$d_{boundary} \le s_2 \iff \frac{L_{gap} - \text{intervals}_2 \cdot s_2}{2} \le s_2 \iff \text{intervals}_2 \ge \frac{L_{gap}}{s_2} - 2$$
2. **Lower bound**: To prevent clashing, $d_{boundary}$ must not be excessively small.
   Choosing the minimal integer satisfying the upper bound:
   $$\text{intervals}_2 = \left\lceil \frac{L_{gap}}{s_2} - 2.0 - 10^{-9} \right\rceil$$

**Theorem (Guaranteed Clearance Bounds)**:
Let $\text{intervals}_2 = \lceil y \rceil$ where $y = (L_{gap} / s_2) - 2.0$. By definition of ceiling, $y \le \text{intervals}_2 < y + 1$.
1. Upper bound: $\text{intervals}_2 \ge y \implies d_{boundary} \le s_2$ (no shear capacity shortfall).
2. Lower bound: $\text{intervals}_2 < y + 1 \implies \text{intervals}_2 \cdot s_2 < L_{gap} - s_2 \implies d_{boundary} > \frac{s_2}{2.0}$.

Therefore, for any clear span and any spacing $s_2$:
$$\mathbf{\frac{s_2}{2.0} < d_{boundary} \le s_2}$$
- When $s_2 = 200$ mm: $100.0\text{ mm} < d_{boundary} \le 200.0\text{ mm}$.
- When $s_2 = 100$ mm: $50.0\text{ mm} < d_{boundary} \le 100.0\text{ mm}$.
- When $s_2 = 300$ mm: $150.0\text{ mm} < d_{boundary} \le 300.0\text{ mm}$.

**Result**:
- $\Delta X \ge 50.0$ mm always (far above zero collision, strictly exceeds aggregate clearance).
- $\Delta X \le s_2$ always (strictly complies with structural code shear spacing limits).
- Complete spatial symmetry about the beam centerline is preserved.

### 2.3 Proposed Source Diff for `BeamStirrupDistributionCalculator.cs`

```csharp
<<<<
        // Zone 2 (Midspan Sparse Zone)
        int intervals2 = (int)Math.Floor(l2 / spec.SpacingSparse);
        int count2 = intervals2 + 1;
        if (count2 > MaxBarPositions)
            throw new ArgumentOutOfRangeException(nameof(spec), $"Zone 2 stirrup count {count2} exceeds maximum {MaxBarPositions}.");

        double delta2 = (l2 - (intervals2 * spec.SpacingSparse)) / 2.0;
        double startX2 = l1 + delta2;
        var positions2 = new List<double>(count2);
        for (int i = 0; i < count2; i++)
            positions2.Add(startX2 + (i * spec.SpacingSparse));

        var run2 = new StirrupRun
        {
            Count = count2,
            Spacing = spec.SpacingSparse,
            StartOffset = startX2,
            Length = intervals2 * spec.SpacingSparse,
            StartX = startX2,
            EndX = startX2 + (intervals2 * spec.SpacingSparse),
            Positions = positions2
        };

        // Zone 3 (Right Support Zone)
        int count3 = count1;
        double startX3 = (clearSpanMm - l3) + delta1;
        var positions3 = new List<double>(count3);
        for (int i = 0; i < count3; i++)
            positions3.Add(startX3 + (i * spec.SpacingDense));
====
        // Zone 3 (Right Support Zone) computed first to establish exact right boundary
        int count3 = count1;
        double startX3 = (clearSpanMm - l3) + delta1;
        var positions3 = new List<double>(count3);
        for (int i = 0; i < count3; i++)
            positions3.Add(startX3 + (i * spec.SpacingDense));

        var run3 = new StirrupRun
        {
            Count = count3,
            Spacing = spec.SpacingDense,
            StartOffset = startX3,
            Length = intervals1 * spec.SpacingDense,
            StartX = startX3,
            EndX = startX3 + (intervals1 * spec.SpacingDense),
            Positions = positions3
        };

        // Zone 2 (Midspan Sparse Zone)
        // Positioned symmetrically within the physical gap between Zone 1 and Zone 3:
        // gap = startX3 - lastX1.
        // Guarantees boundary transition spacing dBoundary satisfies: s2/2 < dBoundary <= s2,
        // eliminating duplicate/clashing stirrups at zone transitions.
        double lastX1 = startX1 + (intervals1 * spec.SpacingDense);
        double gap = startX3 - lastX1;

        int count2;
        int intervals2;
        double startX2;
        var positions2 = new List<double>();

        if (gap <= 0.0)
        {
            count2 = 0;
            intervals2 = 0;
            startX2 = lastX1;
        }
        else
        {
            double y2 = (gap / spec.SpacingSparse) - 2.0;
            intervals2 = (int)Math.Ceiling(y2 - 1e-9);
            if (intervals2 < 0)
                intervals2 = 0;

            if (gap < 2.0 * Math.Min(spec.SpacingDense, spec.SpacingSparse))
            {
                if (gap >= 2.0 * DefaultStartOffsetMm)
                {
                    count2 = 1;
                    intervals2 = 0;
                    startX2 = (lastX1 + startX3) / 2.0;
                    positions2.Add(startX2);
                }
                else
                {
                    count2 = 0;
                    intervals2 = 0;
                    startX2 = lastX1;
                }
            }
            else
            {
                count2 = intervals2 + 1;
                if (count2 > MaxBarPositions)
                    throw new ArgumentOutOfRangeException(nameof(spec), $"Zone 2 stirrup count {count2} exceeds maximum {MaxBarPositions}.");

                double delta2 = (gap - (intervals2 * spec.SpacingSparse)) / 2.0;
                startX2 = lastX1 + delta2;
                for (int i = 0; i < count2; i++)
                    positions2.Add(startX2 + (i * spec.SpacingSparse));
            }
        }

        var run2 = new StirrupRun
        {
            Count = count2,
            Spacing = spec.SpacingSparse,
            StartOffset = startX2,
            Length = count2 > 0 ? intervals2 * spec.SpacingSparse : 0.0,
            StartX = startX2,
            EndX = count2 > 0 ? startX2 + (intervals2 * spec.SpacingSparse) : startX2,
            Positions = positions2
        };
>>>>
```

### 2.4 Test Suite Updates in `BeamStirrupDistributionCalculatorTests.cs`
1. **Update Test Counts**:
   - `VaryingSpansGenerateMatchingRunCountsForStandardThreeSpanGirder`:
     - $L_n = 5600$ mm: `runs1.Sum(r => r.Count)` updates from $43 \to 42$ ($14 + 14 + 14 = 42$).
       - *Why*: Redundant duplicate stirrup at 25 mm from Zone 1 ($X = 1400$) is eliminated; Zone 2 centers 14 bars at $X = 1500 \dots 4100$ with $125.0$ mm boundary clearance.
     - $L_n = 4600$ mm: `runs2.Sum(r => r.Count)` updates from $36 \to 35$ ($12 + 11 + 12 = 35$).
       - *Why*: Redundant boundary bar at $X = 1200$ (50 mm from Zone 1) is eliminated; Zone 2 centers 11 bars at $X = 1300 \dots 3300$ with $150.0$ mm boundary clearance.
   - `TotalStirrupCountMatchesCalculatedDesignEquation`:
     - Updates from $122 \to 119$ ($42 + 35 + 42 = 119$).
2. **Add Dedicated Adversarial Regression Tests**:
   - `ThreeZoneBoundaryTransitionsNeverProduceCoincidentOrSubAggregateSpacing`:
     - Tests $L_n \in \{4600, 5200, 5600, 6200, 7500\}$.
     - Asserts `runs[1].StartX - runs[0].EndX >= 50.0`.
     - Asserts `runs[2].StartX - runs[1].EndX >= 50.0`.
     - Asserts `runs[1].StartX - runs[0].EndX <= runs[1].Spacing`.
     - Asserts `runs[2].StartX - runs[1].EndX <= runs[1].Spacing`.
   - `ChallengerAttackScenarioSixtyTwoHundredMillimetresHasZeroClash`:
     - Configures $L_n = 6200$ mm, $s_1 = 100$, $s_2 = 100$.
     - Asserts spacing between Zone 1 and Zone 2 is exactly 100.0 mm (not 0.0 mm).
     - Configures $L_n = 6200$ mm, $s_1 = 100$, $s_2 = 310$.
     - Asserts spacing between Zone 1 and Zone 2 is strictly $> 0$ and $\le 310.0$ mm.

---

## 3. Issue 2: Skin Reinforcement Vertical Spacing Violations for $H \in [700, 800]$ mm

### 3.1 Problem Statement & Root Cause Analysis
In `BeamSideBarCalculator.cs`:
- `ComputeRowCount` currently contains:
  ```csharp
  public static int ComputeRowCount(
      double heightMm,
      double coverMm = 25.0,
      double stirrupDiameterMm = 8.0,
      double mainDiameterMm = 20.0)
  {
      if (heightMm < HeightThresholdMm)
          return 0;

      return (int)Math.Ceiling((heightMm - 600.0) / 200.0);
  }
  ```
- Although `coverMm`, `stirrupDiameterMm`, and `mainDiameterMm` were defined in the method signature, they were completely ignored!
- The empirical formula `(heightMm - 600.0) / 200.0` produces:
  - $H = 700$ mm: $\lceil 100 / 200 \rceil = 1$ row.
  - $H = 800$ mm: $\lceil 200 / 200 \rceil = 1$ row.
  - $H = 1000$ mm: $\lceil 400 / 200 \rceil = 2$ rows.

**Physical Calculation of Vertical Spacing**:
In `ComputeLongitudinalSideBars` and `ComputeCrossTies`:
$$z_{botMain} = \text{BottomElevation} + \text{Cover} + d_{stirrup} + (d_{main} / 2.0)$$
$$z_{topMain} = \text{TopElevation} - \text{Cover} - d_{stirrup} - (d_{main} / 2.0)$$
$$\Delta Z = \frac{z_{topMain} - z_{botMain}}{n_{rows} + 1}$$

With standard detailing parameters ($\text{Cover} = 25$, $d_{stirrup} = 8$, $d_{main} = 20$):
$$z_{offset} = 25 + 8 + 10 = 43\text{ mm} \implies H_{clear} = z_{topMain} - z_{botMain} = H - 86\text{ mm}$$

- For $H = 700$ mm: $H_{clear} = 614$ mm. With $n_{rows} = 1$:
  $$\Delta Z = \frac{614}{1 + 1} = \mathbf{307.0\text{ mm}} > 300.0\text{ mm}!$$
- For $H = 800$ mm: $H_{clear} = 714$ mm. With $n_{rows} = 1$:
  $$\Delta Z = \frac{714}{1 + 1} = \mathbf{357.0\text{ mm}} > 300.0\text{ mm}!$$
- For $H = 1000$ mm: $H_{clear} = 914$ mm. With $n_{rows} = 2$:
  $$\Delta Z = \frac{914}{2 + 1} = \mathbf{304.67\text{ mm}} > 300.0\text{ mm}!$$

**Legal & Code Blast Radius**:
- **TCVN 5574:2018 §10.3.2**: Specifically mandates that for $h \ge 700$ mm, the vertical spacing of skin reinforcement must not exceed 300 mm.
- **ACI 318-19 §9.7.2.3**: Explicitly caps skin reinforcement spacing at $s \le 300$ mm (12 in.).
- Detailed reinforcement designs with $\Delta Z \in \{307, 357\}$ mm will fail structural verification.

### 3.2 Mathematical Remediation
To strictly satisfy $\Delta Z \le s_{max}$ (where $s_{max} = \text{MaxVerticalSpacingMm} = 300.0$ mm):
$$\frac{H_{clear}}{n_{rows} + 1} \le s_{max} \iff n_{rows} + 1 \ge \frac{H_{clear}}{s_{max}} \iff n_{rows} \ge \left\lceil \frac{H_{clear}}{s_{max}} \right\rceil - 1$$

For any deep beam where side bars are mandated ($H \ge 700$ mm), $n_{rows} \ge 1$.
Thus:
$$N_{spaces} = \left\lceil \frac{H_{clear}}{s_{max}} \right\rceil$$
$$n_{rows} = \max(1, N_{spaces} - 1)$$

**Evaluation Across All Standard Beam Depths**:
| Height $H$ (mm) | $H_{clear}$ (mm) | Required Spaces $N_{spaces} = \lceil H_{clear}/300 \rceil$ | Row Count $n_{rows}$ | Vertical Pitch $\Delta Z = H_{clear} / (n_{rows} + 1)$ (mm) | Compliance ($\le 300.0$ mm) |
|---|---|---|---|---|---|
| 600 | 514 | N/A ($< 700$) | 0 | N/A | **N/A** |
| 699 | 613 | N/A ($< 700$) | 0 | N/A | **N/A** |
| **700** | 614 | $\lceil 614/300 \rceil = 3$ | **2** | $614 / 3 = \mathbf{204.7}$ | **PASS** |
| **800** | 714 | $\lceil 714/300 \rceil = 3$ | **2** | $714 / 3 = \mathbf{238.0}$ | **PASS** |
| 900 | 814 | $\lceil 814/300 \rceil = 3$ | **2** | $814 / 3 = \mathbf{271.3}$ | **PASS** |
| **1000** | 914 | $\lceil 914/300 \rceil = 4$ | **3** | $914 / 4 = \mathbf{228.5}$ | **PASS** |
| 1100 | 1014 | $\lceil 1014/300 \rceil = 4$ | **3** | $1014 / 4 = \mathbf{253.5}$ | **PASS** |
| 1200 | 1114 | $\lceil 1114/300 \rceil = 4$ | **3** | $1114 / 4 = \mathbf{278.5}$ | **PASS** |
| 1400 | 1314 | $\lceil 1314/300 \rceil = 5$ | **4** | $1314 / 5 = \mathbf{262.8}$ | **PASS** |

### 3.3 Proposed Source Diff for `BeamSideBarCalculator.cs`

```csharp
<<<<
    /// <summary>
    /// Computes the number of side bar pairs (rows) based on beam height.
    /// Returns 0 for h < 700 mm; 1 for 700-800 mm; 2 for 900-1000 mm; 3 for 1100-1200 mm.
    /// </summary>
    public static int ComputeRowCount(
        double heightMm,
        double coverMm = 25.0,
        double stirrupDiameterMm = 8.0,
        double mainDiameterMm = 20.0)
    {
        if (heightMm < HeightThresholdMm)
            return 0;

        return (int)Math.Ceiling((heightMm - 600.0) / 200.0);
    }
====
    /// <summary>
    /// Computes the number of side bar pairs (rows) based on clear vertical depth between main bars.
    /// Enforces TCVN 5574:2018 §10.3.2 and ACI 318 §9.7.2.3 vertical spacing limit (<= 300 mm).
    /// </summary>
    public static int ComputeRowCount(
        double heightMm,
        double coverMm = 25.0,
        double stirrupDiameterMm = 8.0,
        double mainDiameterMm = 20.0,
        double maxVerticalSpacingMm = MaxVerticalSpacingMm)
    {
        if (heightMm < HeightThresholdMm)
            return 0;

        double spacing = maxVerticalSpacingMm > 0.0 ? maxVerticalSpacingMm : MaxVerticalSpacingMm;
        double zOffset = coverMm + stirrupDiameterMm + (mainDiameterMm / 2.0);
        double clearVerticalSpanMm = heightMm - (2.0 * zOffset);
        if (clearVerticalSpanMm <= 0.0)
            return 1;

        int spaces = (int)Math.Ceiling(clearVerticalSpanMm / spacing);
        int rows = spaces - 1;

        return Math.Max(1, rows);
    }
>>>>
```

In `ComputeLongitudinalSideBars` (line 56) and `ComputeCrossTies` (line 136):
```csharp
<<<<
            int nRows = ComputeRowCount(span.Height, span.Cover, stirrupDiameterMm, mainBarDiameterMm);
====
            int nRows = ComputeRowCount(span.Height, span.Cover, stirrupDiameterMm, mainBarDiameterMm, spec.MaxVerticalSpacing);
>>>>
```

### 3.4 Test Suite Updates in `BeamSideBarCalculatorTests.cs`
1. **Update `BeamHeightThresholdDeterminesNumberOfSideBarPairs`**:
   ```csharp
   [Theory]
   [InlineData(500d, 0)]
   [InlineData(600d, 0)]
   [InlineData(699d, 0)]
   [InlineData(700d, 2)] // 2 rows guarantee deltaZ = 204.7 mm <= 300 mm
   [InlineData(800d, 2)] // 2 rows guarantee deltaZ = 238.0 mm <= 300 mm
   [InlineData(900d, 2)] // 2 rows guarantee deltaZ = 271.3 mm <= 300 mm
   [InlineData(1000d, 3)] // 3 rows guarantee deltaZ = 228.5 mm <= 300 mm
   [InlineData(1200d, 3)] // 3 rows guarantee deltaZ = 278.5 mm <= 300 mm
   public void BeamHeightThresholdDeterminesNumberOfSideBarPairs(double height, int expectedPairs)
   {
       int actual = BeamSideBarCalculator.ComputeRowCount(height);
       Assert.Equal(expectedPairs, actual);
   }
   ```
2. **Update `SideBarsArePositionedInPairsAlongLeftAndRightLateralFaces`**:
   - Beam height is 800 mm. For $H = 800$ mm, 2 rows are generated (4 bars total).
   - Assert `Assert.Equal(4, bars.Count); // 2 rows * 2 sides = 4 bars`.
3. **Update `SingleSpanDeepBeamGeneratesExpectedSideBarCount`**:
   - Beam height is 1000 mm. For $H = 1000$ mm, 3 rows are generated (6 bars total).
   - Assert `Assert.Equal(6, bars.Count); // 3 rows * 2 sides = 6 bars`.
4. **Expand `SideBarVerticalSpacingNeverExceedsThreeHundredMillimetres` (Unmasking Test)**:
   - Convert to parameterized `[Theory]`:
     ```csharp
     [Theory]
     [InlineData(700)]
     [InlineData(750)]
     [InlineData(800)]
     [InlineData(900)]
     [InlineData(1000)]
     [InlineData(1200)]
     public void SideBarVerticalSpacingNeverExceedsThreeHundredMillimetres(double height)
     {
         var stack = TestBeamData.DeepBeam(height: height);
         var bars = BeamSideBarCalculator.ComputeLongitudinalSideBars(stack, new BeamSideBarSpec(), 8.0, 20.0);

         double zBotMain = stack.Spans[0].BottomElevation + 25.0 + 8.0 + 10.0;
         double zTopMain = stack.Spans[0].TopElevation - 25.0 - 8.0 - 10.0;

         var leftBars = bars.Where(b => b.TransverseY < 0.0).OrderBy(b => b.Points[0].Z).ToList();
         Assert.NotEmpty(leftBars);

         // Distance from bottom main bar to first skin bar
         Assert.True(leftBars[0].Points[0].Z - zBotMain <= 300.0);

         // Spacing between adjacent skin bars
         for (int i = 0; i < leftBars.Count - 1; i++)
         {
             double diff = leftBars[i + 1].Points[0].Z - leftBars[i].Points[0].Z;
             Assert.True(diff <= 300.0);
         }

         // Distance from last skin bar to top main bar
         Assert.True(zTopMain - leftBars.Last().Points[0].Z <= 300.0);
     }
     ```

---

## 4. Implementation Steps & Acceptance Criteria

### Step 1: `BeamStirrupDistributionCalculator.cs`
- Modify Zone 2 and Zone 3 calculation block in `ComputeSpanRuns`.
- Ensure Zone 3 is evaluated before Zone 2 or `startX3` is computed to establish `gap = startX3 - lastX1`.
- Center Zone 2 intervals within `gap` using $y_2 = (gap / s_2) - 2.0$ and $\text{intervals}_2 = \lceil y_2 - 10^{-9} \rceil$.

### Step 2: `BeamSideBarCalculator.cs`
- Update `ComputeRowCount` with formula based on `clearVerticalSpanMm` and `MaxVerticalSpacingMm`.
- Pass `spec.MaxVerticalSpacing` in `ComputeLongitudinalSideBars` and `ComputeCrossTies`.

### Step 3: Test Fixture Refactoring
- Update expected assertions in `BeamStirrupDistributionCalculatorTests.cs` (counts 43->42, 36->35, 122->119).
- Add boundary transition clearance tests for 3-zone stirrups.
- Update expected row counts in `BeamSideBarCalculatorTests.cs` (700->2, 800->2, 1000->3).
- Expand unmasking tests verifying $\Delta Z \le 300$ mm across all intervals from bottom main to top main bar.

### Acceptance Criteria:
1. Zero coincident stirrups ($\Delta X > 0$ for all adjacent bars in all spans).
2. Boundary stirrup clearance $\Delta X \in [s_2 / 2, s_2]$ for all 3-zone distributions.
3. For $H \in [700, 1400]$ mm, vertical spacing between any adjacent longitudinal reinforcement (bottom main, skin bars, top main) is $\le 300.0$ mm.
4. All unit tests in `HPRebar.Core.Tests` compile cleanly and pass.
