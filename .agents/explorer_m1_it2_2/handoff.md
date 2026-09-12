# Handoff Report — explorer_m1_it2_2

**Task**: Milestone 1 Remediation Planning for Beam Stirrup Collision & Skin Bar Vertical Spacing  
**Working Directory**: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_it2_2`  
**Handoff Type**: Hard (Investigation & Planning Complete)  

---

## 1. Observation

### Observation 1: Duplicate Boundary Stirrup Collision in `BeamStirrupDistributionCalculator.cs`
- **Location**: `HPRebar/HPRebar.Core/BeamRebar/Calculators/BeamStirrupDistributionCalculator.cs:115-179`
- **Code Quote**:
  ```csharp
  116: double lDist1 = l1 - spec.StartOffset;
  117: int intervals1 = (int)Math.Floor(lDist1 / spec.SpacingDense);
  ...
  122: double delta1 = (lDist1 - (intervals1 * spec.SpacingDense)) / 2.0;
  123: double startX1 = spec.StartOffset + delta1;
  ...
  140: int intervals2 = (int)Math.Floor(l2 / spec.SpacingSparse);
  ...
  145: double delta2 = (l2 - (intervals2 * spec.SpacingSparse)) / 2.0;
  146: double startX2 = l1 + delta2;
  ...
  164: double startX3 = (clearSpanMm - l3) + delta1;
  ```
- **Observed Behavior**:
  - The last bar of Zone 1 is at $X = \text{startX1} + \text{intervals1} \cdot s_1 = L_1 - \delta_1$.
  - The first bar of Zone 2 is at $X = L_1 + \delta_2$.
  - Distance across the boundary is $\Delta X = (L_1 + \delta_2) - (L_1 - \delta_1) = \delta_1 + \delta_2$.
  - When $L_{dist1} \pmod{s_1} = 0$ and $L_2 \pmod{s_2} = 0$, $\delta_1 = 0$ and $\delta_2 = 0 \implies \Delta X = 0.0$ mm (exact spatial collision).
  - Symmetrically between Zone 2 and Zone 3: distance is $\delta_1 + \delta_2 = 0.0$ mm.
  - Confirmed by `challenger_m1_2/challenge_report.md:14-30` for $L_n = 6200$ mm ($L_1 = 1550$ mm, $s_1 = 100$ mm, $s_2 = 100$ mm or $310$ mm).

### Observation 2: Existing Test Artifacts Asserting Buggy Stirrup Counts
- **Location**: `HPRebar/HPRebar.Core.Tests/BeamRebar/BeamStirrupDistributionCalculatorTests.cs:176-205`
- **Code Quote**:
  ```csharp
  182: Assert.Equal(43, runs1.Sum(r => r.Count));
  183: Assert.Equal(36, runs2.Sum(r => r.Count));
  ...
  204: // Span 1 (43) + Span 2 (36) + Span 3 (43) = 122
  205: Assert.Equal(122, totalCount);
  ```
- **Observed Behavior**:
  - In $L_n = 5600$ mm: Zone 1 had 14 bars (ending at 1375 mm), Zone 2 had 15 bars (starting at 1400 mm), Zone 3 had 14 bars (starting at 4225 mm). Total = 43. The spacing between Zone 1 and Zone 2 was $1400 - 1375 = 25$ mm ($< s_1 = 100$ mm).
  - In $L_n = 4600$ mm: Zone 1 had 12 bars (ending at 1150 mm), Zone 2 had 12 bars (starting at 1200 mm), Zone 3 had 12 bars (starting at 3450 mm). Total = 36. Spacing was $1200 - 1150 = 50$ mm ($< s_1 = 100$ mm).

### Observation 3: Skin Reinforcement Spacing Violation in `BeamSideBarCalculator.cs`
- **Location**: `HPRebar/HPRebar.Core/BeamRebar/Calculators/BeamSideBarCalculator.cs:26-36` and `lines 60-63`
- **Code Quote**:
  ```csharp
  26: public static int ComputeRowCount(
  27:     double heightMm,
  28:     double coverMm = 25.0,
  29:     double stirrupDiameterMm = 8.0,
  30:     double mainDiameterMm = 20.0)
  31: {
  32:     if (heightMm < HeightThresholdMm)
  33:         return 0;
  34: 
  35:     return (int)Math.Ceiling((heightMm - 600.0) / 200.0);
  36: }
  ...
  60: double zBotMain = span.BottomElevation + span.Cover + stirrupDiameterMm + (mainBarDiameterMm / 2.0);
  61: double zTopMain = span.TopElevation - span.Cover - stirrupDiameterMm - (mainBarDiameterMm / 2.0);
  62: double deltaZ = (zTopMain - zBotMain) / (nRows + 1);
  ```
- **Observed Behavior**:
  - For $H = 700$ mm: `ComputeRowCount` returns $\lceil (700 - 600)/200 \rceil = 1$ row. With $z_{offset} = 43$ mm ($25 + 8 + 10$), $H_{clear} = 700 - 86 = 614$ mm. Vertical spacing $\Delta Z = 614 / 2 = 307.0$ mm $> 300.0$ mm (`MaxVerticalSpacingMm`).
  - For $H = 800$ mm: `ComputeRowCount` returns 1 row. $H_{clear} = 714$ mm. Vertical spacing $\Delta Z = 714 / 2 = 357.0$ mm $> 300.0$ mm.
  - For $H = 1000$ mm: `ComputeRowCount` returns 2 rows. $H_{clear} = 914$ mm. Vertical spacing $\Delta Z = 914 / 3 = 304.67$ mm $> 300.0$ mm.
  - Violates TCVN 5574:2018 §10.3.2 and ACI 318-19 §9.7.2.3.

### Observation 4: Masking Unit Test in `BeamSideBarCalculatorTests.cs`
- **Location**: `HPRebar/HPRebar.Core.Tests/BeamRebar/BeamSideBarCalculatorTests.cs:18-21` and `lines 52-63`
- **Code Quote**:
  ```csharp
  18: [InlineData(700d, 1)]
  19: [InlineData(800d, 1)]
  20: [InlineData(1000d, 2)]
  ...
  52: public void SideBarVerticalSpacingNeverExceedsThreeHundredMillimetres()
  53: {
  54:     var stack = TestBeamData.DeepBeam(height: 1200);
  ...
  58:     for (int i = 0; i < leftBars.Count - 1; i++)
  59:     {
  60:         double diff = leftBars[i + 1].Points[0].Z - leftBars[i].Points[0].Z;
  61:         Assert.True(diff <= 300.0);
  62:     }
  63: }
  ```
- **Observed Behavior**:
  - Test only evaluated $H = 1200$ mm and only tested spacing between adjacent side bars (ignoring distance to top and bottom main bars).
  - For $H = 800$ mm with 1 row, `leftBars.Count = 1`, loop executed 0 times, completely masking the 357 mm violation.

---

## 2. Logic Chain

1. **Stirrup Clashing Mechanics**:
   - Zone 1 rightmost stirrup coordinate is $X_{1, last} = L_1 - \delta_1$ (Observation 1).
   - Zone 3 leftmost stirrup coordinate is $X_{3, first} = L_n - L_3 + \delta_1 = L_n - L_1 + \delta_1$ (Observation 1).
   - The available gap between the physical end of Zone 1 and the physical start of Zone 3 is $L_{gap} = X_{3, first} - X_{1, last} = L_2 + 2\delta_1$.
   - If Zone 2 is distributed independently on $L_2$, its leftmost stirrup coordinate is $X_{2, first} = L_1 + \delta_2$. The resulting transition gap $\Delta X = \delta_1 + \delta_2$ is bounded by $[0, (s_1 + s_2)/2]$, which collapses to 0.0 mm whenever intervals align with boundaries.
   - Therefore, Zone 2 must be formulated directly inside $L_{gap}$ rather than independently on $L_2$.
   - Within $L_{gap}$, setting $\text{intervals}_2 = \lceil (L_{gap} / s_2) - 2.0 - 10^{-9} \rceil$ mathematically guarantees:
     $$\frac{s_2}{2.0} < d_{boundary} \le s_2$$
   - This strictly eliminates coincident stirrups ($\Delta X \ge 50$ mm) and satisfies the code upper bound ($d_{boundary} \le s_2$).

2. **Test Count Alignment**:
   - Eliminating the redundant boundary stirrups that were placed 25 mm from Zone 1 in $L_n = 5600$ mm and 50 mm from Zone 1 in $L_n = 4600$ mm reduces Zone 2 bar count by 1 in each span.
   - Total count for $L_n = 5600$ mm changes from $43 \to 42$ ($14 + 14 + 14$).
   - Total count for $L_n = 4600$ mm changes from $36 \to 35$ ($12 + 11 + 12$).
   - Total 3-span count changes from $122 \to 119$ ($42 + 35 + 42$).
   - These updates must be reflected in `BeamStirrupDistributionCalculatorTests.cs` (Observation 2).

3. **Skin Reinforcement Spacing Mechanics**:
   - By TCVN 5574:2018 §10.3.2 and ACI 318-19 §9.7.2.3, vertical pitch $\Delta Z \le 300.0$ mm is legally required for beams with $h \ge 700$ mm.
   - Clear vertical span between top and bottom main bars is $H_{clear} = H - 2 \cdot z_{offset} = H - 86$ mm for standard detailing (Observation 3).
   - For $H = 700$ mm, $H_{clear} = 614$ mm. One row produces $\Delta Z = 614 / 2 = 307.0$ mm $> 300.0$ mm.
   - For $H = 800$ mm, $H_{clear} = 714$ mm. One row produces $\Delta Z = 714 / 2 = 357.0$ mm $> 300.0$ mm.
   - Mathematically, to achieve $\Delta Z \le 300.0$ mm, the required number of spaces is $N_{spaces} = \lceil H_{clear} / 300.0 \rceil$, requiring $n_{rows} = N_{spaces} - 1$ rows.
   - For $H = 700$ mm: $N_{spaces} = \lceil 614 / 300 \rceil = 3 \implies n_{rows} = 2$ rows ($\Delta Z = 204.7$ mm $\le 300.0$ mm).
   - For $H = 800$ mm: $N_{spaces} = \lceil 714 / 300 \rceil = 3 \implies n_{rows} = 2$ rows ($\Delta Z = 238.0$ mm $\le 300.0$ mm).
   - For $H = 1000$ mm: $N_{spaces} = \lceil 914 / 300 \rceil = 4 \implies n_{rows} = 3$ rows ($\Delta Z = 228.5$ mm $\le 300.0$ mm).
   - Updating `ComputeRowCount` to calculate rows dynamically from $H_{clear}$ and `MaxVerticalSpacingMm` guarantees code compliance across all beam depths.

---

## 3. Caveats

- **Scope Boundary**: This investigation and remediation plan covers the pure domain calculators in `HPRebar.Core` (`BeamStirrupDistributionCalculator` and `BeamSideBarCalculator`) and their respective test suites in `HPRebar.Core.Tests`. It does not modify Revit API creation classes (M3 scope).
- **Execution Environment**: As noted in `auditor_m1_1/audit_report.md`, interactive terminal execution via `run_command` timed out on permission prompt. In accordance with system instructions, `run_command` was not re-executed; all mathematical deductions and code changes are verified via static analytical proofs.
- **Other Audit Findings**: Additional findings from reviewer and auditor reports (such as exterior support top bar Layer 2 omission and tautological test cleanup in `BeamMainBarCalculatorTests.cs`) are documented in the respective reports and can be sequenced alongside or after this remediation.

---

## 4. Conclusion

1. **Stirrup Clashing Remediation**:
   - Position Zone 2 symmetrically in the physical gap $L_{gap} = \text{startX}_3 - \text{lastX}_1$.
   - Set $\text{intervals}_2 = \lceil (L_{gap} / s_2) - 2.0 - 10^{-9} \rceil$.
   - Proven guarantee: $\frac{s_2}{2.0} < d_{boundary} \le s_2$, eliminating coincident stirrups ($\Delta X = 0.0$ mm) and preventing illegal boundary crowding while preserving code shear limits.

2. **Skin Reinforcement Remediation**:
   - Replace empirical heuristic `(heightMm - 600) / 200` with direct derivation from clear vertical depth $H_{clear} = H - 2 \cdot (\text{cover} + d_{stirrup} + d_{main}/2)$.
   - $n_{rows} = \max(1, \lceil H_{clear} / s_{max} \rceil - 1)$ for $H \ge 700$ mm.
   - For $H = 700$ mm: 2 rows ($\Delta Z = 204.7$ mm $\le 300.0$ mm).
   - For $H = 800$ mm: 2 rows ($\Delta Z = 238.0$ mm $\le 300.0$ mm).
   - For $H = 1000$ mm: 3 rows ($\Delta Z = 228.5$ mm $\le 300.0$ mm).
   - Strictly compliant with TCVN 5574:2018 §10.3.2 and ACI 318-19 §9.7.2.3.

3. **Complete Plan**:
   - Detailed diffs and test specifications are fully articulated in `remediation_plan.md`.

---

## 5. Verification Method

### 5.1 Verification Commands
When executed by an implementing agent with terminal permissions:
```bash
# Build core project
dotnet build HPRebar/HPRebar.Core/HPRebar.Core.csproj

# Run pure domain unit tests (102 tests)
dotnet test HPRebar/HPRebar.Core.Tests/HPRebar.Core.Tests.csproj
```

### 5.2 Files to Inspect Post-Implementation
1. `HPRebar/HPRebar.Core/BeamRebar/Calculators/BeamStirrupDistributionCalculator.cs`:
   - Inspect lines 140–180: verify `gap = startX3 - lastX1` and ceiling intervals formula.
2. `HPRebar/HPRebar.Core/BeamRebar/Calculators/BeamSideBarCalculator.cs`:
   - Inspect lines 26–36: verify `ComputeRowCount` calculates `clearVerticalSpanMm` and returns 2 for $H \in [700, 800]$.
3. `HPRebar/HPRebar.Core.Tests/BeamRebar/BeamStirrupDistributionCalculatorTests.cs`:
   - Inspect updated counts (42, 35, 119) and new collision regression tests.
4. `HPRebar/HPRebar.Core.Tests/BeamRebar/BeamSideBarCalculatorTests.cs`:
   - Inspect parameterized spacing verification across $H \in \{700, 750, 800, 900, 1000, 1200\}$.

### 5.3 Invalidation Conditions
This remediation plan is invalidated if:
- A structural code requires stirrups across zone boundaries to share a single bar coordinate ($X = L_1$) without offsetting.
- An application requirement allows skin bar vertical spacing to exceed 300.0 mm for beams with $H \ge 700$ mm.
