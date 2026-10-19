# Adversarial Empirical Challenge Report: Milestone M4 (TCVN 5574:2018 Beam Engine)

**Agent:** `challenger_m4_beam_1` (teamwork_preview_challenger)  
**Parent:** `orchestrator_3` (`4db7c650-146a-4a05-a1e1-c0fb57e2733b`)  
**Working Directory:** `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_beam_1\`  
**Date:** 2026-10-07T23:48:00Z  
**Type:** Hard Handoff (Challenge Complete)  
**Verdict:** **REJECT (Requires Remediation of 1 Safety-Critical Shear Flaw and 1 Geometry Guard)**

---

## 1. Observation

### 1.1 Test Execution Baseline & Suite Expansion
- Baseline test execution before challenge:
  ```text
  Test run summary: Passed!
    total: 1641
    failed: 0
    succeeded: 1641
    skipped: 0
    duration: 4s 554ms
  ```
- Executed co-located test harness in `HPRebar/HPRebar.Core.Tests/Tcvn5574/TcvnBeamStressChallengeTests.cs` (33 theory/fact cases):
  ```text
  Test run summary: Passed!
    total: 1674
    failed: 0
    succeeded: 1674
    skipped: 0
    duration: 6s 967ms
  ```

### 1.2 Extreme Bending Moment Observations
In `HPRebar/HPRebar.Core/Tcvn5574/Calculators/TcvnBeamFlexureCalculator.cs`:
- **Zero Moment**:
  Lines 39-42:
  ```csharp
  if (momentNmm < 1e-4)
  {
      return CreateZeroMomentResult(b, h0, momentKnm, muMaxPercent);
  }
  ```
  Lines 68-70:
  ```csharp
  double asMin = MinMu * b * h0;
  double muPercent = (asMin / (b * h0)) * 100.0;
  ```
  `MinMu` is `0.001` (0.1%). Empirical test `Flexure_ZeroMoment_EnforcesMinimumSteelRatioAcrossAllSections` across multiple beam sizes ($200\times 300, 300\times 600, 400\times 800, 500\times 1000$) confirms $A_s = 0.001 \cdot b \cdot h_0$, $\mu = 0.1\%$, $A_s' = 0$, `IsDoublyReinforced = false`, `IsCompliant = true`.
- **Negative & Infinitesimal Moments**:
  Line 36: `double momentNmm = Math.Abs(momentKnm) * 1e6;`.
  Moments from $10^{-12}\text{ kNm}$ to $10^{-3}\text{ kNm}$ smoothly yield $A_s = A_{s,min}$ without division-by-zero or NaN.
  Negative moments produce bitwise-identical results to positive moments.
- **Enormous Moments ($\alpha_m \gg \alpha_R$)**:
  Lines 46-51:
  ```csharp
  if (alphaM <= steel.AlphaR)
  {
      return CalculateSinglyReinforced(b, h0, momentKnm, momentNmm, alphaM, concrete, steel, muMaxPercent);
  }

  return CalculateDoublyReinforced(b, h0, momentKnm, momentNmm, alphaM, aPrime, concrete, steel, muMaxPercent);
  ```
  In `CalculateDoublyReinforced`:
  ```csharp
  double mb = steel.AlphaR * concrete.Rb * b * h0 * h0;
  double deltaM = momentNmm - mb;

  double asPrime = deltaM / (steel.Rsc * (h0 - aPrime));
  double asRequired = ((steel.XiR * concrete.Rb * b * h0) / steel.Rs) + ((steel.Rsc / steel.Rs) * asPrime);
  ```
  No `Math.Sqrt` is executed in the doubly reinforced branch. Tested up to $M = 1,000,000\text{ kNm}$. `isOverReinforced` is set to `true` and `IsCompliant = false` when $\mu > 3.0\%$. No crash occurs.
- **Section Depth Geometry Defect**:
  In `ValidateInputs` (lines 54-63):
  ```csharp
  if (a <= 0.0 || a >= h) throw new ArgumentOutOfRangeException(nameof(a), "Centroid distance a must be within (0, h).");
  if (aPrime <= 0.0 || aPrime >= h) throw new ArgumentOutOfRangeException(nameof(aPrime), "Centroid distance aPrime must be within (0, h).");
  ```
  The code omits validation for `h > a + aPrime`. When $h = 70, a = 40, a' = 40 \implies h_0 = 30 < a'$, then $(h_0 - a') = -10$, yielding a negative compression steel area $A_s' < 0$ or division by zero if $h_0 = a'$.

### 1.3 Shear Web Crush & Equilibrium Observations
In `HPRebar/HPRebar.Core/Tcvn5574/Calculators/TcvnBeamShearCalculator.cs`:
- **Web Crush Boundary**:
  Lines 48-53:
  ```csharp
  double qbMaxN = PhiB1 * concrete.Rb * b * h0;
  double qbMaxKn = qbMaxN / 1e3;
  if (qNewton > qbMaxN)
  {
      return CreateCrushExceededResult(shearKn, h0, qbMaxKn, stirrupDiameter, legs, asw);
  }
  ```
  For beam $300\times 600$, $h_0 = 560\text{ mm}$, B25 ($R_b = 14.5\text{ MPa}$), $Q_{b,max} = 730.8\text{ kN}$:
  - $Q = 730.799\text{ kN}$: `IsWebCrushExceeded = false`.
  - $Q = 730.800\text{ kN}$: `IsWebCrushExceeded = false`.
  - $Q = 730.801\text{ kN}$: `IsWebCrushExceeded = true`, `IsCompliant = false`.
- **CRITICAL DEFECT — High Shear Stirrup Deficiency Swallowed**:
  In `CalculateStirrupsRequired` (lines 170-199):
  ```csharp
  double sCalc = (stirrupSteel.Rsw * asw) / qswReq;
  ...
  double upperLimit = Math.Min(sMax, sDet);
  double recommendedSpacing = RoundToPracticalSpacing(Math.Min(sCalc, upperLimit));

  return new ShearDesignResult
  {
      ...
      CalculatedSpacing = sCalc,
      MaxSpacing = sMax,
      ConstructiveSpacing = sDet,
      RecommendedSpacing = recommendedSpacing,
      IsCompliant = true,
      Message = "Calculated shear reinforcement satisfies TCVN 5574:2018 inclined section equilibrium."
  };
  ```
  And in `RoundToPracticalSpacing` (lines 215-221):
  ```csharp
  private static double RoundToPracticalSpacing(double rawSpacing)
  {
      if (rawSpacing <= 50.0) return 50.0;
      if (rawSpacing < 100.0) return Math.Floor(rawSpacing / 25.0) * 25.0;
      if (rawSpacing <= 300.0) return Math.Floor(rawSpacing / 50.0) * 50.0;
      return Math.Min(300.0, Math.Floor(rawSpacing / 50.0) * 50.0);
  }
  ```
  **Direct Empirical Observation**:
  When $Q = 700\text{ kN}$ on $300\times 600$ beam with 2-leg d8 stirrups ($A_{sw} = 100.53\text{ mm}^2$):
  - Required spacing from equilibrium: $s_{calc} = 20.7\text{ mm}$.
  - Because $20.7 \le 50.0$, `RoundToPracticalSpacing` clamps **UP** to `50.0 mm`.
  - `RecommendedSpacing = 50.0 mm > CalculatedSpacing (20.7 mm)`.
  - With $s = 50\text{ mm}$, the provided shear capacity is:
    $$q_{sw,prov} = \frac{R_{sw} A_{sw}}{50} = \frac{170 \times 100.53}{50} = 341.8\text{ N/mm} \ll q_{sw,req} = 825.8\text{ N/mm}$$
    Provided capacity is only **41.4%** of required equilibrium shear force!
  - Yet `IsCompliant` is hardcoded to `true` and the message asserts compliance!

### 1.4 Crowded Rebar Spacing Observations
In `HPRebar/HPRebar.Core/Tcvn5574/Calculators/TcvnBeamRebarOptimizer.cs`:
- Clear spacing formula (line 26):
  ```csharp
  double requiredSpacing = isTop ? Math.Max(30.0, barDiameter) : Math.Max(25.0, barDiameter);
  ```
- Empirically verified:
  - For $d \le 25\text{ mm}$ bottom: threshold is $25\text{ mm}$.
  - For $d > 25\text{ mm}$ bottom (e.g. $d=28, 32$): threshold is $d$ ($28\text{ mm}, 32\text{ mm}$).
  - For $d \le 30\text{ mm}$ top: threshold is $30\text{ mm}$.
  - For $d = 32\text{ mm}$ top: threshold is $32\text{ mm}$.
  - In section $b=200$, 3 bars d25 produce clear spacing $t = 29.5\text{ mm}$: passes bottom ($29.5 \ge 25$) but fails top ($29.5 < 30$).
  - In section $b=200$, 3 bars d28 produce $t = 25.0\text{ mm}$: fails bottom because $25.0 < 28.0\text{ mm}$, confirming $d$ governs.
  - Overcrowded optimizer fallback (`CreateCrowdedFallback`): sets `IsValid = false`, `IsSpacingCompliant = false`, and outputs explanatory text.

---

## 2. Logic Chain

1. **Extreme Bending Moments Robustness**:
   - The TCVN formulation partitions singly reinforced ($\alpha_m \le \alpha_R$) and doubly reinforced ($\alpha_m > \alpha_R$) sections.
   - For enormous bending moments ($\alpha_m \gg \alpha_R$), the code strictly branches to `CalculateDoublyReinforced`. Since `CalculateDoublyReinforced` does not call `Math.Sqrt(1 - 2*alphaM)`, there is zero risk of negative square root exceptions or `NaN`.
   - Minimum reinforcement ratio $\mu_{min} = 0.1\%$ is unconditionally enforced at $M = 0$ and infinitesimal moments, ensuring structural integrity against sudden brittle rupture.

2. **Web Crush Limit Accuracy**:
   - Clause 8.1.3.2.1 specifies $Q \le 0.3 R_b b h_0$.
   - Tested directly at $\pm 0.001\text{ kN}$ around the exact boundary $730.800\text{ kN}$.
   - The threshold discriminator is exact and flags web crushing only when $Q > Q_{b,max}$.

3. **Analysis of Safety Flaw in Stirrup Design**:
   - In structural concrete mechanics, when shear force $Q$ requires $s_{calc} < s_{min}$ (where $s_{min} = 50\text{ mm}$ is the physical minimum spacing for aggregate vibration), the section or stirrup configuration is **insufficient**.
   - Simply rounding up to $50\text{ mm}$ without increasing the number of stirrup legs ($n_w \ge 4$) or bar diameter ($d_{sw} \ge 10\text{ mm}$) leaves the beam with insufficient shear reinforcement ($s > s_{calc}$).
   - Because `TcvnBeamShearCalculator.cs:198` hardcodes `IsCompliant = true`, downstream systems (or structural engineers using this report) are misled into believing a dangerous design satisfies equilibrium.
   - Therefore, `IsCompliant` MUST be evaluated as `bool isCompliant = sCalc >= recommendedSpacing;` (or `sCalc >= 50.0;`), and if false, set `IsCompliant = false` with a warning message requesting additional legs or section enlargement.

4. **Analysis of Geometry Validation Omission**:
   - In `TcvnBeamFlexureCalculator.ValidateInputs`, allowing $a + a' \ge h$ accepts degenerate beams where tension reinforcement is above compression reinforcement.
   - For doubly reinforced calculations, this causes division by zero or negative compression steel areas.

---

## 3. Caveats

1. **Rebar Optimizer Coverage**: The optimizer currently evaluates standard bar diameters 14 to 32 mm in 1 or 2 layers. Three-layer bundles or bundled bars ($>2$ layers) are not modeled.
2. **Dynamic / Seismic Shear Requirements**: TCVN 9386:2012 seismic capacity design shear increments ($Q_{sd} = \gamma_{Rd} \cdot Q$) are external multipliers and must be applied to the envelope forces prior to feeding into `TcvnBeamShearCalculator`.
3. **No Implementation Code Modified**: In strict compliance with the challenger role constraints, zero production code lines were altered. The findings are documented for the worker agent to remediate.

---

## 4. Conclusion & Verdict

### Verdict: **REJECT (Requires Worker Remediation)**

Milestone M4 has built a high-quality foundation (1641 passing tests, clean architecture, accurate formulas for normal ranges, and strong rebar spacing checks). However, adversarial challenge revealed **one safety-critical flaw** and **one geometry edge-case omission**:

1. **Defect 1 (CRITICAL — Structural Safety):**
   - **Location:** `HPRebar/HPRebar.Core/Tcvn5574/Calculators/TcvnBeamShearCalculator.cs:181-201`
   - **Issue:** When high shear requires $s_{calc} < 50\text{ mm}$, `RoundToPracticalSpacing` returns $50\text{ mm}$. `RecommendedSpacing` ($50\text{ mm}$) exceeds `CalculatedSpacing` ($20.7\text{ mm}$), providing only ~41% of required shear strength. `IsCompliant` is unconditionally set to `true`.
   - **Remediation:**
     ```csharp
     bool isSpacingAdequate = sCalc >= recommendedSpacing && sCalc >= 50.0;
     return new ShearDesignResult
     {
         ...
         IsCompliant = isSpacingAdequate,
         Message = isSpacingAdequate
             ? "Calculated shear reinforcement satisfies TCVN 5574:2018 inclined section equilibrium."
             : $"Stirrups inadequate: required spacing s_calc = {sCalc:F1}mm < 50mm minimum. Increase stirrup legs or diameter."
     };
     ```

2. **Defect 2 (MEDIUM — Section Geometry Validation):**
   - **Location:** `HPRebar/HPRebar.Core/Tcvn5574/Calculators/TcvnBeamFlexureCalculator.cs:54-63`
   - **Issue:** `ValidateInputs` does not check `if (h <= a + aPrime)`. Degenerate depths yield $(h_0 - a') \le 0$, resulting in negative $A_s'$ or division by zero.
   - **Remediation:** Add `if (h <= a + aPrime) throw new ArgumentOutOfRangeException(nameof(h), "Total height h must be greater than sum of centroid distances (a + aPrime).");`.

---

## 5. Verification Method

### 5.1 Verification Commands
1. Run all unit tests including the adversarial challenge suite:
   ```powershell
   dotnet test HPRebar/HPRebar.Core.Tests
   ```
   *Verified Result:* 1674 passed, 0 failed, 0 skipped.
2. Run only the adversarial stress test suite:
   ```powershell
   dotnet test HPRebar/HPRebar.Core.Tests -- --filter-method "*StressChallenge*"
   ```
   *Verified Result:* 33 passed, 0 failed.

### 5.2 Test File Added
- `HPRebar/HPRebar.Core.Tests/Tcvn5574/TcvnBeamStressChallengeTests.cs` (co-located in test project per layout compliance rules).

### 5.3 Invalidation Conditions
- Any test failure in `dotnet test HPRebar.Core.Tests`.
- Failure to flag non-compliance when $s_{calc} < s_{rec}$ in `TcvnBeamShearCalculator`.
- Acceptance of degenerate beam section where $h \le a + a'$.
