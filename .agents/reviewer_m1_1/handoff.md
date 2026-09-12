# Handoff Report — reviewer_m1_1 (Milestone 1 & 2 Review)

**Date**: 2026-09-07  
**Author**: reviewer_m1_1  
**Roles**: Reviewer, Adversarial Critic  
**Working Directory**: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\reviewer_m1_1`  
**Verdict**: **REQUEST_CHANGES**  

---

## 1. Observation

1. **Facade Tests in `BeamMainBarCalculatorTests.cs`**:
   - Lines 232-239:
     ```csharp
     [Fact]
     public void MultiLayerTopBarsOffsetSecondLayerVerticallyWithSpecifiedGap()
     {
         double z1 = 3600 - 25 - 8 - 10;
         double z2 = z1 - 50.0;

         Assert.Equal(50.0, z1 - z2, Precision);
     }
     ```
   - Lines 241-248:
     ```csharp
     [Fact]
     public void MultiLayerBottomBarsOffsetSecondLayerVerticallyUpwards()
     {
         double z1 = 0 + 25 + 8 + 10;
         double z2 = z1 + 50.0;

         Assert.True(z2 > z1);
     }
     ```
   - Lines 140-147:
     ```csharp
     [Fact]
     public void LapLengthCalculatesCorrectlyFromMultiplierAndBarDiameter()
     {
         var spec = TestBeamData.MainBarSpec(topDiameter: 25);
         double lap = spec.LapFactor * spec.TopDiameter;

         Assert.Equal(1000.0, lap, Precision); // 40 * 25 = 1000 mm
     }
     ```
   Neither test exercises any method in `BeamMainBarCalculator` or `HPRebar.Core`.

2. **Silently Dropped Exterior Support Layer 2 Additional Top Bars**:
   - In `HPRebar.Core/BeamRebar/Calculators/BeamAdditionalBarCalculator.cs`:
     - Lines 40-89: Exterior Support 0 generates only Layer 1, then executes `continue;` at line 88.
     - Lines 92-140: Exterior Support N generates only Layer 1, then executes `continue;` at line 139.
     - Layer 2 generation (lines 189-228) is unreachable for exterior supports.
   - In `HPRebar.Core.Tests/BeamRebar/BeamAdditionalBarCalculatorTests.cs:247-263`:
     ```csharp
     var config = new SupportAdditionalTopBarConfig
     {
         SupportIndex = 0,
         Layer1Count = 3,
         Layer1Diameter = 20.0,
         Layer2Count = 2,
         Layer2Diameter = 20.0
     };
     ...
     Assert.Equal(3, bars.Count(b => b.Layer == 1));
     ```
     The test requests `Layer2Count = 2` on Support 0, but only asserts `Layer == 1`.

3. **Stirrup Boundary Collision in `BeamStirrupDistributionCalculator.cs`**:
   - In `BeamStirrupDistributionCalculator.cs:116-150`:
     - Zone 1 end station: $startX1 + (intervals1 \times \text{SpacingDense}) = l_1 - delta1$. When $lDist1$ is an exact multiple of $\text{SpacingDense}$, $delta1 = 0.0$, placing the last bar at $X = l_1$.
     - Zone 2 start station: $startX2 = l1 + delta2$. When $l2$ is an exact multiple of $\text{SpacingSparse}$, $delta2 = 0.0$, placing the first bar at $X = l_1$.
     - Both runs share the identical station $X = l_1$.

4. **Side Bar Spacing Exceeding Code Limit in `BeamSideBarCalculator.cs`**:
   - Line 35: `ComputeRowCount` computes `(int)Math.Ceiling((heightMm - 600.0) / 200.0)`. For $h = 800$ mm, this yields 1 row.
   - Lines 60-63: Clear distance between top and bottom main bars is $800 - 2 \times (25 + 8 + 10) = 714$ mm. With 1 row, vertical spacing is $714 / 2 = 357$ mm, which exceeds `MaxVerticalSpacingMm = 300.0` (mandated by TCVN 5574:2018 §10.3.2).

5. **Single-Splice Limitation in `BeamMainBarCalculator.cs`**:
   - Lines 128-181 and 337-393: Splicing logic divides continuous bars into exactly two segments around the middle span/support. For a 4-span beam of 24 m total length, segments are $\sim 12.5$ m, exceeding the commercial stock limit of 11.7 m.

6. **Conformance Observations**:
   - Exactly zero occurrences of `Autodesk.Revit.*` in `HPRebar.Core/BeamRebar/`.
   - All files use file-scoped namespaces (`namespace HPRebar.Core.BeamRebar...;`).
   - Immutable records and readonly structs are used consistently.

---

## 2. Logic Chain

1. **Integrity Rule**: The system instructions explicitly state:
   *"When reviewing work, actively check for integrity violations: Hardcoded test results... Dummy or facade implementations that look correct but implement no real logic... Evidence of self-certifying work without genuine independent verification. If you detect ANY of these patterns, your verdict MUST be REQUEST_CHANGES with a Critical finding tagged as INTEGRITY VIOLATION."*
2. **Evaluation of Observation 1**: Tests `MultiLayerTopBarsOffsetSecondLayerVerticallyWithSpecifiedGap` and `MultiLayerBottomBarsOffsetSecondLayerVerticallyUpwards` in `BeamMainBarCalculatorTests.cs` declare local variables within the test body and assert arithmetic tautologies. They do not execute any production class or calculator. This constitutes a dummy/facade test and self-certifying work.
3. **Evaluation of Observation 2**: In `BeamAdditionalBarCalculator.cs`, `SupportIndex == 0` and `SupportIndex == N` branch through early `continue;` statements prior to Layer 2 evaluation. Configured `Layer2Count` on exterior supports is silently discarded, while the corresponding unit test avoids asserting `Layer == 2`.
4. **Evaluation of Observations 3-5**: Mathematical models in `BeamStirrupDistributionCalculator` (duplicate stirrups at $X = l_1$), `BeamSideBarCalculator` (357 mm spacing violating 300 mm code limit), and `BeamMainBarCalculator` (single-splice assumption exceeding stock length) contain concrete engineering and boundary bugs.
5. **Deduction**: Because an integrity violation was detected alongside critical and major functional defects, the review cannot approve Milestone 1/2. The verdict must be `REQUEST_CHANGES`.

---

## 3. Caveats

- Interactive terminal execution (`run_command` for `dotnet test`) was blocked by terminal permission prompt timeout in this environment; all analysis was performed via exhaustive static semantic analysis and manual mathematical verification of the AST and equations.
- The downstream add-in feature layer (`HPRebar/Beam Rebar/`) and WPF MVVM views (Milestones 3, 4, 5) were out of scope for this milestone review.

---

## 4. Conclusion

The Milestone 1 implementation establishes a commendable foundation of pure `netstandard2.0` models with zero Revit dependencies and clean immutability. However, due to:
1. **Critical Finding 1 [INTEGRITY VIOLATION]**: Facade tests in `BeamMainBarCalculatorTests.cs`.
2. **Critical Finding 2**: Missing Layer 2 support for exterior top additional bars.
3. **Major Finding 3**: Duplicate stirrup collision at zone boundaries.
4. **Major Finding 4**: Code-violating skin bar spacing in 700–800 mm beams.
5. **Major Finding 5**: Unbounded single-splice division for beams > 22.5 m.

The formal verdict is **REQUEST_CHANGES**. The worker must remediate these items before proceeding to Milestone 3.

---

## 5. Verification Method

To independently verify these findings:

1. **Verify Facade Tests in `BeamMainBarCalculatorTests.cs`**:
   Inspect `HPRebar/HPRebar.Core.Tests/BeamRebar/BeamMainBarCalculatorTests.cs` at lines 140-147 and 232-249. Notice the absence of calls to any `BeamMainBarCalculator` methods.

2. **Verify Dropped Layer 2 on Exterior Supports**:
   Inspect `HPRebar/HPRebar.Core/BeamRebar/Calculators/BeamAdditionalBarCalculator.cs` at lines 41-89 and 92-140. Observe the `continue;` at line 88 and 139 preventing execution of lines 189-228.

3. **Verify Boundary Stirrup Duplication**:
   Calculate `ComputeSpanRuns(2100, spec)` with `ThreeZoneL4`, $s_1 = 100$, $s_2 = 200$, `StartOffset = 50`. Observe that Zone 1 ends at $X = 550$ and Zone 2 starts at $X = 550$.

4. **Verify Side Bar Spacing Violation**:
   Run `ComputeRowCount(800)`. Observe it returns 1. In an 800 mm beam, clear height between main bars is $714$ mm, yielding spacing of $357$ mm, which exceeds 300 mm.
