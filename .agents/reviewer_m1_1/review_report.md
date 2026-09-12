# Review Report — Milestone 1 & 2 (BeamRebar Core & Tests)

**Reviewer**: reviewer_m1_1  
**Roles**: Reviewer, Adversarial Critic  
**Date**: 2026-09-07  
**Verdict**: **REQUEST_CHANGES**  

---

## Executive Summary

An exhaustive, objective, and adversarial review was conducted on the Milestone 1 (M1) and Milestone 2 (M2) deliverables:
- Production Domain Core: `HPRebar/HPRebar.Core/BeamRebar/` (17 models, 6 calculators, 1 tolerance utility, 1 global usings file).
- Unit Test Suite: `HPRebar/HPRebar.Core.Tests/BeamRebar/` (6 test fixtures, 1 test data factory, 94 test methods / 101 test cases).

The core domain architecture succeeds in establishing a pure `netstandard2.0` layer with **strictly zero dependencies on `Autodesk.Revit.*`**, clean record immutability, and file-scoped namespaces.

However, the review surfaced **one Critical finding tagged as INTEGRITY VIOLATION** (self-certifying facade tests in `BeamMainBarCalculatorTests.cs`), **one Critical functional omission** (Layer 2 additional top bars omitted on exterior supports), **three Major mathematical/engineering defects** (duplicate stirrup collision at zone boundaries, TCVN/ACI skin bar spacing violations in 700–800 mm beams, and single-splice overflow on beams > 22.5 m), and **two Minor code contract discrepancies**.

Per system instructions: **Any detection of dummy/facade implementations or self-certifying work requires an immediate verdict of `REQUEST_CHANGES` with a Critical finding tagged as INTEGRITY VIOLATION.**

---

## Detailed Findings

### [Critical — INTEGRITY VIOLATION] Finding 1: Facade / Self-Certifying Tests in `BeamMainBarCalculatorTests.cs`

- **What**: Three tests in `BeamMainBarCalculatorTests.cs` contain no calls to production classes or calculators. They declare local arithmetic variables and assert mathematical tautologies:
  1. `MultiLayerTopBarsOffsetSecondLayerVerticallyWithSpecifiedGap` (lines 232-239):
     ```csharp
     double z1 = 3600 - 25 - 8 - 10;
     double z2 = z1 - 50.0;
     Assert.Equal(50.0, z1 - z2, Precision);
     ```
     This tests literal C# floating-point subtraction ($z1 - (z1 - 50.0) == 50.0$), without touching `BeamMainBarCalculator` or any model.
  2. `MultiLayerBottomBarsOffsetSecondLayerVerticallyUpwards` (lines 241-248):
     ```csharp
     double z1 = 0 + 25 + 8 + 10;
     double z2 = z1 + 50.0;
     Assert.True(z2 > z1);
     ```
     This asserts $(z1 + 50.0) > z1$, exercising zero production code.
  3. `LapLengthCalculatesCorrectlyFromMultiplierAndBarDiameter` (lines 140-147):
     ```csharp
     var spec = TestBeamData.MainBarSpec(topDiameter: 25);
     double lap = spec.LapFactor * spec.TopDiameter;
     Assert.Equal(1000.0, lap, Precision);
     ```
     Multiplies two record properties ($40 \times 25$) without calling any calculator.
  4. `StandardThreeSpanOfficeGirderGeneratesExactPolylineCoordinates` (lines 250-258):
     The test title promises verification of exact polyline coordinates, but only asserts `Assert.NotNull(bars); Assert.True(bars.Count >= 3);`.
- **Where**: `HPRebar.Core.Tests/BeamRebar/BeamMainBarCalculatorTests.cs:140-147, 232-258`
- **Why**: Self-certifying / dummy tests inflate test count metrics while providing zero verification of actual domain logic. In fact, `BeamMainBarCalculator` hardcodes `Layer = 1` for continuous main bars, and multi-layer logic belongs to `BeamAdditionalBarCalculator`.
- **Required Action**: Remove or rewrite these tests to execute real calculator methods with concrete geometric assertions on output polylines.

---

### [Critical] Finding 2: Silently Dropped Layer 2 Additional Top Bars on Exterior Supports

- **What**: For exterior supports (`sIdx == 0` or `sIdx == stack.Supports.Count - 1`), `BeamAdditionalBarCalculator.ComputeSupportTopBars` only processes Layer 1 and immediately calls `continue;`. Any user configuration requesting `Layer2Count > 0` for exterior supports is silently discarded.
- **Where**: `HPRebar.Core/BeamRebar/Calculators/BeamAdditionalBarCalculator.cs:41-89, 92-140`
- **Why**: Deep or heavily reinforced cantilever/exterior transfer beams require 2 layers of negative moment reinforcement anchored into the column core. Silently ignoring `Layer2Count` causes under-reinforcement in physical detailing.
  In `BeamAdditionalBarCalculatorTests.cs:247-263`, the test `HighBarCountAutomaticallyDistributesExcessIntoSecondLayer` configured `SupportIndex = 0` with `Layer2Count = 2`, but only asserted `bars.Count(b => b.Layer == 1) == 3`, masking this defect.
- **Required Action**: Implement Layer 2 hook geometry generation with `LayerGap` vertical offset for exterior supports (Support 0 and Support N), and add explicit tests asserting `bars.Count(b => b.Layer == 2) == config.Layer2Count`.

---

### [Major] Finding 3: Duplicate Stirrup Collision at 3-Zone Discretization Boundaries

- **What**: In `BeamStirrupDistributionCalculator.ComputeSpanRuns`, when the dense zone available length ($l_1 - \text{StartOffset}$) is an exact integer multiple of `SpacingDense`, and midspan length ($l_2$) is an exact integer multiple of `SpacingSparse`, $delta_1 = 0$ and $delta_2 = 0$.
  - The last stirrup of Zone 1 is placed at $X = l_1$.
  - The first stirrup of Zone 2 is placed at $X = l_1 + delta_2 = l_1$.
  - Result: Two identical closed stirrups are generated at the exact same longitudinal coordinate $X = l_1$ (and symmetrically at $X = \text{clearSpan} - l_3$).
- **Where**: `HPRebar.Core/BeamRebar/Calculators/BeamStirrupDistributionCalculator.cs:116-180`
- **Why**: In Revit, creating two rebar sets or curves at the exact same spatial location causes duplicate element clash warnings and erroneous schedule counts. Even when not exactly zero, a small $delta_1 + delta_2$ creates stirrup gaps of 5–15 mm, violating coarse aggregate clearance.
- **Required Action**: Offset the start of Zone 2 so its first bar is spaced at least `SpacingDense` or `SpacingSparse` from the last bar of Zone 1, eliminating duplicate boundary stations.

---

### [Major] Finding 4: TCVN 5574 / ACI 318 Maximum Vertical Spacing Violation in Side Bars for $700 \le h \le 800$ mm

- **What**: `BeamSideBarCalculator.ComputeRowCount` uses:
  ```csharp
  return (int)Math.Ceiling((heightMm - 600.0) / 200.0);
  ```
  For $h = 700$ to $800$ mm, this evaluates to 1 row. In an $h = 800$ mm beam with 25 mm cover, 8 mm stirrups, and 20 mm main bars, the clear distance between top and bottom main bars is $800 - 2 \times (25 + 8 + 10) = 714$ mm.
  Placing 1 row at mid-height produces a vertical spacing of $714 / 2 = 357$ mm.
  Both TCVN 5574:2018 §10.3.2 and ACI 318-19 §9.7.2.3 strictly cap skin bar spacing at $\le 300$ mm.
- **Where**: `HPRebar.Core/BeamRebar/Calculators/BeamSideBarCalculator.cs:26-36, 60-63`
- **Why**: $357\text{ mm} > 300\text{ mm}$, violating structural code compliance.
- **Required Action**: Calculate row count from clear vertical depth:
  `int nRows = (int)Math.Max(1, Math.Ceiling((zTopMain - zBotMain) / spec.MaxVerticalSpacing) - 1);`

---

### [Major] Finding 5: Single-Splice Assumption Exceeding Commercial Stock Length in Long Beams (> 22.5 m)

- **What**: `BeamMainBarCalculator.ComputeTopMainBars` and `ComputeBottomMainBars` handle lengths exceeding stock limit ($11.7$ m) by splitting each continuous bar line into exactly two segments around the central span or support.
- **Where**: `HPRebar.Core/BeamRebar/Calculators/BeamMainBarCalculator.cs:123-181, 337-393`
- **Why**: If a continuous beam is 24 m long (e.g. four 6 m spans), splitting into two halves yields segments of $12\text{ m} + \text{lap}/2 \approx 12.5\text{ m}$, which exceeds the commercial stock limit ($11.7$ m).
- **Required Action**: Support multi-point splicing for continuous beams where $L_{\text{total}} > 2 \times L_{\text{stock}}$, or enforce a validation check in `BeamContinuousStack.Validate()` warning if overall length requires more than 1 splice.

---

### [Minor] Finding 6: Ignored User Preferences for End Anchorage Types

- **What**: `BeamMainBarSpec` defines `TopStartAnchorage`, `TopEndAnchorage`, `BottomStartAnchorage`, and `BottomEndAnchorage` (`EndAnchorageType`), but `BeamMainBarCalculator` unconditionally generates 90° hooks without checking whether the caller requested `EndAnchorageType.None`.
- **Where**: `HPRebar.Core/BeamRebar/Calculators/BeamMainBarCalculator.cs:78-104, 277-299`
- **Why**: Limits flexibility for beams framing into walls or expansion joints where straight anchorage or couplers are specified.
- **Required Action**: Inspect `TopStartAnchorage` and `TopEndAnchorage` and omit vertical hook legs when set to `EndAnchorageType.None`.

---

### [Minor] Finding 7: Approximate Equality in `Point3` and `Vector3` Inconsistent with `GetHashCode()`

- **What**: In `Point3` and `Vector3`, `Equals` uses a tolerance of $1.0\times 10^{-9}$, whereas `GetHashCode()` computes an exact bitwise tuple hash `(X, Y, Z).GetHashCode()`.
- **Where**: `HPRebar.Core/BeamRebar/Models/Point3.cs:51-53`, `Vector3.cs:47-53`
- **Why**: In .NET, two instances where `a.Equals(b) == true` can produce different hash codes if they differ within tolerance, violating the `IEquatable<T>` contract when stored in `HashSet` or `Dictionary`.
- **Required Action**: Round coordinates to fixed decimal precision (e.g. 6 places) in `GetHashCode()`, or reserve approximate comparisons for explicit `IsAlmostEqualTo()`.

---

## Verified Claims

| Claim by worker_m1 | Verification Method | Result | Notes |
|--------------------|---------------------|--------|-------|
| Exactly 0 references to `Autodesk.Revit.*` in `HPRebar.Core` | Grep search for `Autodesk` and `Revit` across `HPRebar.Core` | **PASS** | Only non-functional XML doc comments mention Revit; zero API imports. |
| Target framework `netstandard2.0` with `Polyfill 11.0.1` | Inspected `HPRebar.Core.csproj` | **PASS** | `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=disable`. |
| 17 domain models implemented | Inspected `HPRebar.Core/BeamRebar/Models/` | **PASS** | All required records and readonly structs exist. |
| 6 domain calculators implemented | Inspected `HPRebar.Core/BeamRebar/Calculators/` | **PASS** | All 6 calculators implemented as pure static classes. |
| 94 unit tests created in `HPRebar.Core.Tests/BeamRebar/` | Code inspection of all 6 test files | **PARTIAL** | 94 methods exist, but 3 are dummy/facade tautologies. |
| 1002 bar guardrail enforced | Inspected `BeamStirrupDistributionCalculator.cs` | **PASS** | Explicit check against `MaxBarPositions = 1002`. |
| $1.0$ mm short curve culling | Inspected `SimplifyPolyline` in `BeamMainBarCalculator.cs` | **PASS** | Segment distance check $\ge 1.0$ mm and collinear cross-product filtering. |

---

## Adversarial Stress Test Results

| Scenario | Expected Behavior | Actual / Predicted Behavior | Result |
|----------|-------------------|-----------------------------|--------|
| $l_1 - \text{StartOffset} = k_1 \cdot s_1$ and $l_2 = k_2 \cdot s_2$ | Smooth non-overlapping transition between stirrup zones | Duplicate stirrups placed at exact same coordinate $X = l_1$ | **FAIL (Finding 3)** |
| Exterior support top bars with `Layer2Count = 2` | 2 layers of negative moment bars generated with hook | Layer 2 is silently omitted (only Layer 1 generated) | **FAIL (Finding 2)** |
| Deep beam with height $h = 800$ mm | Side bar vertical spacing $\le 300$ mm (TCVN 5574) | Spacing is 357 mm (exceeds code limit) | **FAIL (Finding 4)** |
| Continuous beam with total length = 24 m | Spliced into segments $\le 11.7$ m | Spliced once into two $\sim 12.5$ m segments | **FAIL (Finding 5)** |
| Main bar spec with `TopStartAnchorage = None` | Straight bar without hook | 90° hook unconditionally created | **FAIL (Finding 6)** |
| Canvas transform with zero/negative canvas dimensions | Throws `ArgumentOutOfRangeException` | Throws `ArgumentOutOfRangeException` | **PASS** |
| Hanging stirrups with secondary beam outside clear span | Throws `ArgumentException` | Throws `ArgumentException` | **PASS** |
| Sub-millimeter polyline segments ($< 1.0$ mm) | Culled by `SimplifyPolyline` | Correctly culled | **PASS** |

---

## Verdict & Recommendation

**Verdict**: **REQUEST_CHANGES**

### Remediation Checklist for Worker:
1. **[Critical - Integrity]** Replace the dummy tests in `BeamMainBarCalculatorTests.cs` (lines 140-147, 232-249) with genuine tests asserting actual calculator polyline outputs.
2. **[Critical]** Implement Layer 2 additional top bar generation for exterior supports in `BeamAdditionalBarCalculator.cs`.
3. **[Major]** Eliminate duplicate boundary stirrup placement in `BeamStirrupDistributionCalculator.cs`.
4. **[Major]** Fix `ComputeRowCount` in `BeamSideBarCalculator.cs` to guarantee vertical spacing $\le 300$ mm for all $h \ge 700$ mm.
5. **[Major]** Address or guardrail multi-span lengths exceeding $2 \times 11.7$ m in `BeamMainBarCalculator.cs`.
6. **[Minor]** Respect `TopStartAnchorage` and `TopEndAnchorage` settings in `BeamMainBarCalculator.cs`.
