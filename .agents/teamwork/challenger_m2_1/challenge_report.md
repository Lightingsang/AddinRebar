# Empirical Challenge Report — Milestone M2 (Phase 3: Column Detailing & Calculators)

**Date**: 2026-10-17  
**Challenger**: `challenger_m2_1`  
**Target Subsystem**: `HPKata.Core.Columns.Calculators`  
**Evaluated Target Binary**: `HPKata/HPKata.Core/bin/Debug/netstandard2.0/HPKata.Core.dll` & `net8.0`  
**Test Suite Executed**: `scripts/empirical_challenge_m2_column.ps1` (113 test cases executed, 113 nominal tests passed, 3 defects empirically confirmed)  
**Serialized Empirical Results**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m2_1\empirical_results.json`

---

## Challenge Summary

**Overall risk assessment**: **HIGH**  
**Empirical Verdict**: **DEFECT_FOUND**

While all standard boundary and nominal dimension tests pass (113/113, 100%), adversarial stress testing identified **three distinct defects** in the Column calculation domain:
1. **[CRITICAL] Infinite Loop / OOM in `ColumnSpliceCalculator.SegmentCommercialColumnBars`**: Occurs whenever `2 * storyHeight <= lapLength` on multi-story runs ($> 11.7\text{ m}$ or staggered). `currentStart` regresses backwards each iteration, causing process hang / OutOfMemoryException.
2. **[MEDIUM] Unphysical Fallback Layout in `TcvnColumnRebarOptimizer.CreateFallback`**: When required $A_{st}$ exceeds the code section limit ($3.5\% A_g$) on narrow sections (e.g., $200 \times 200$), the optimizer mechanically generates overlapping bars with negative clear spacing ($-7.0\text{ mm}$) and an unbuildable steel ratio ($\rho = 25.13\%$) without throwing or flagging unfeasibility.
3. **[LOW / CODE QUALITY] Tautological Min/Max Absorption in `ColumnCrankBendCalculator.CalculateCrankElevation`**: The expression `Math.Min(crankLen, Math.Max(crankLen, beamDepthMm))` is an algebraic identity $\min(a, \max(a, b)) \equiv a$, completely bypassing the intended beam-depth capping.

---

## Challenges & Confirmed Defects

### [Critical] Challenge 1: Infinite Loop & OOM in `ColumnSpliceCalculator.SegmentCommercialColumnBars`
- **Component**: `HPKata.Core.Columns.Calculators.ColumnSpliceCalculator.cs` (lines 88–117, `BuildSegments`)
- **Assumption challenged**: The worker assumed `currentStart` will always advance towards `topZ` because `currentEnd = currentStart + maxAllowed` and `currentStart = currentEnd - lap`.
- **Attack Scenario**:
  - Input: `baseZ = 0.0`, `topZ = 15000.0` (15m run), `diameter = 25mm` ($L_{lap} = 35 \times 25 = 875\text{ mm}$), `storyHeight = 400.0mm` (low-height mezzanine, ground transfer stub, or retaining column segment), `isStaggeredTier = false`.
  - Calculation:
    - `targetSpan = storyHeight * 2.0 = 800.0mm`.
    - `maxAllowed = Math.Min(11700.0, 800.0) = 800.0mm`.
    - `currentEnd = currentStart + 800.0mm`.
    - `currentStart = currentEnd - lap = currentStart + 800.0 - 875.0 = currentStart - 75.0mm`!
- **Observed Empirical Behavior**:
  - In each iteration $k \ge 1$, `currentStart` regresses backwards by $75\text{ mm}$: $0 \to -75 \to -150 \to -225 \dots \to -\infty$.
  - The condition `while (currentStart < topZ)` never terminates.
  - Test runner job timed out after 3,000 ms, allocating hundreds of thousands of `ColumnSpliceSegment` objects until execution abort.
- **Blast Radius**:
  - Application freeze / thread starvation / OutOfMemory crash in AutoCAD or Revit whenever a userDetails a multi-story column stack with short story segments ($2 \times \text{storyHeight} \le L_{lap}$).
- **Remediation**:
  - In `BuildSegments`: clamp `maxAllowed = Math.Max(maxAllowed, lap + MinSpliceSpan)` (e.g., `lap + 500.0`), or verify progression `if (currentEnd - lap <= currentStart) throw new InvalidOperationException(...)` or extend the span count so `targetSpan > lap`.

---

### [Medium] Challenge 2: Unbuildable Rebar Layout with Negative Clear Spacing in `TcvnColumnRebarOptimizer.CreateFallback`
- **Component**: `HPKata.Core.Columns.Calculators.TcvnColumnRebarOptimizer.cs` (lines 84–103, `CreateFallback`)
- **Assumption challenged**: The worker assumed `CreateFallback` reliably provides a fallback section when no candidate in the optimizer loop meets constraints.
- **Attack Scenario**:
  - Input: `width = 200.0`, `depth = 200.0`, `requiredAst = 10000.0 mm2`, `cover = 30.0mm`, `tieDia = 8.0mm`.
  - Theoretical limit: For a $200 \times 200$ section, $A_g = 40,000\text{ mm}^2$. Code limit $\mu_{max} = 3.5\%$ yields $A_{st,max} = 1,400\text{ mm}^2$. An input of $10,000\text{ mm}^2$ requires $\mu = 25\%$, which is physically impossible.
- **Observed Empirical Behavior**:
  - The optimizer loops through all standard diameters ($18\dots 32$) and side bar counts ($0\dots 3$), correctly rejecting them all.
  - It then calls `CreateFallback(200, 200, 10000)`, which mechanically calculates $\lceil 10000 / 314.16 \rceil = 32$ bars of $\phi 20$, setting `SideCountB = 7`, `SideCountH = 7`.
  - Evaluating the section via `CalculateMinClearSpacingMm`:
    - Core width = $200 - 2 \times (30 + 8 + 10) = 104\text{ mm}$.
    - Bar step = $104 / 8 = 13.0\text{ mm}$.
    - Center-to-center distance between bars = $13.0\text{ mm}$.
    - Bar diameter = $20.0\text{ mm}$.
    - Resulting clear spacing $s_{clear} = 13.0 - 20.0 = \mathbf{-7.0\text{ mm}}$!
  - Reinforcement ratio $\rho = 25.13\%$ ($\gg 3.5\%$).
- **Blast Radius**:
  - Produces invalid, intersecting 3D rebar geometry in Revit and overlapping CAD entities in AutoCAD without any warning to the structural engineer.
- **Remediation**:
  - `TcvnColumnRebarOptimizer.Optimize` must check if `targetAst > maxAst`. If no buildable candidate can satisfy the clear spacing ($s_{clear} \ge 50\text{ mm}$) and maximum steel ratio ($\rho \le 3.5\%$), it should throw an `InvalidOperationException("Required steel area exceeds section buildability limits; concrete cross-section must be enlarged per TCVN 5574:2018.")` or return a model flagged with `IsUnfeasible = true`.

---

### [Low] Challenge 3: Tautological Min/Max Absorption in `ColumnCrankBendCalculator.CalculateCrankElevation`
- **Component**: `HPKata.Core.Columns.Calculators.ColumnCrankBendCalculator.cs` (line 50)
- **Assumption challenged**: The worker assumed line 50 clamps the crank length within the beam depth.
- **Code under scrutiny**:
  ```csharp
  double crankLen = CalculateCrankLength(stepDownMm);
  double effectiveCrankLen = Math.Min(crankLen, Math.Max(crankLen, beamDepthMm));
  ```
- **Observed Empirical Behavior**:
  - For any two real numbers $a, b$, the algebraic identity holds:
    $$\min(a, \max(a, b)) \equiv a$$
  - When $a \le b$: $\max(a, b) = b \implies \min(a, b) = a$.
  - When $a > b$: $\max(a, b) = a \implies \min(a, a) = a$.
  - Under input `stepDownMm = 50.0` ($\implies crankLen = 300\text{ mm}$) and shallow `beamDepthMm = 200.0mm`:
    `effectiveCrankLen` evaluates to $300\text{ mm}$, completely ignoring the $200\text{ mm}$ beam depth constraint.
- **Blast Radius**:
  - Harmless under normal deep beams ($h_{beam} \ge 600\text{ mm} > 300\text{ mm}$), but fails to cap crank slope within shallow beams ($h_{beam} < 300\text{ mm}$).
- **Remediation**:
  - Replace `Math.Min(crankLen, Math.Max(crankLen, beamDepthMm))` with `Math.Min(crankLen, beamDepthMm)`.

---

## Empirical Test Suite Results

Executed via `scripts/empirical_challenge_m2_column.ps1` against `HPKata.Core.dll`:

| Category | Test Suite | Test Cases | Passed | Failed | Status |
|---|---|:---:|:---:|:---:|:---:|
| **1. Crank Bend** | Step-down strategy boundaries ($\Delta = 0, 0.001, 25, 50, 50.1, 100\text{mm}$) | 13 | 13 | 0 | **PASS** |
| | Negative step-down rejection (`ArgumentOutOfRangeException`) | 4 | 4 | 0 | **PASS** |
| | 1:6 crank slope calculation ($L = 6 \times \Delta$) | 7 | 7 | 0 | **PASS** |
| | Step-down calculation (concentric vs flush/eccentric) | 6 | 6 | 0 | **PASS** |
| | Crank elevation coordinate mapping | 3 | 3 | 0 | **PASS** |
| **2. Splice Engine** | Lap lengths (compression 35d, tension 45d, clamp $\ge 400\text{mm}$) | 28 | 28 | 0 | **PASS** |
| | Stagger offsets ($\Delta Z \ge \max(1.3 L_{lap}, 600\text{mm})$) | 7 | 7 | 0 | **PASS** |
| | Splice elevations above joint & $l_0$ | 3 | 3 | 0 | **PASS** |
| | Multi-story commercial 11.7m segmentation (1 to 10 stories) | 8 | 8 | 0 | **PASS** |
| | 5-story 18m stack 50% staggered differential verification | 1 | 1 | 0 | **PASS** |
| **3. Confinement** | Confinement height $l_0 = \max(b, h, H_n/6, 500\text{mm})$ | 7 | 7 | 0 | **PASS** |
| | Dense spacing $a_1 \le 100\text{mm}$ (clamped to $\ge 50\text{mm}$) | 5 | 5 | 0 | **PASS** |
| | Sparse spacing $a_2 \le 200\text{mm}$ (clamped to $\ge 100\text{mm}$) | 5 | 5 | 0 | **PASS** |
| | Full confinement tie sets & short-column overlap protection | 2 | 2 | 0 | **PASS** |
| **4. Optimizer** | Nominal column optimization ($400\times 400, 500\times 500, 600\times 800$) | 8 | 8 | 0 | **PASS** |
| | Narrow column optimization ($200\times 200$, 4 corner bars only, $s \ge 50\text{mm}$) | 4 | 4 | 0 | **PASS** |
| | Section dimension defensive rejection (zero/negative width) | 2 | 2 | 0 | **PASS** |
| **5. Adversarial** | Small story height commercial bar splitting ($2 \times H_n \le L_{lap}$) | 1 | 0 | 1 | **FAIL (HUNG)** |
| | Fallback clear spacing & $\mu$ under unfeasible required $A_{st}$ | 1 | 0 | 1 | **FAIL (INVALID)**|
| | Tautological min/max absorption in crank elevation | 1 | 0 | 1 | **FAIL (CODE DEFECT)**|
| **TOTAL** | | **116** | **113** | **3** | **DEFECT_FOUND** |

---

## Detailed Evaluation of Mission Inquiries

### 1. `ColumnCrankBendCalculator.cs`
- **Boundary $\Delta = 0\text{ mm}$**: Correctly returns `ContinuousStraight`.
- **Boundary $\Delta = 25\text{ mm}$**: Correctly returns `CrankBend` with length $150.0\text{ mm}$ ($1:6$ slope).
- **Boundary $\Delta = 50.0\text{ mm}$**: Exact threshold boundary returns `CrankBend` with length $300.0\text{ mm}$.
- **Boundary $\Delta = 50.1\text{ mm}$**: Correctly triggers transition to `CutoffAndStarterDowel`.
- **Boundary $\Delta = 100.0\text{ mm}$**: Correctly triggers `CutoffAndStarterDowel`.
- **Negative step-down**: Robustly throws `ArgumentOutOfRangeException`.
- **Eccentric / flush transitions**: `CalculateStepDown(500, 450, isConcentric: false)` returns $50\text{ mm}$ (`CrankBend`); `CalculateStepDown(500, 440, isConcentric: false)` returns $60\text{ mm}$ (`CutoffAndStarterDowel`).
- **Defect Note**: Line 50 contains tautological min/max expression (Challenge 3).

### 2. `ColumnSpliceCalculator.cs`
- **Staggering Rule**: Verified $50\%$ staggered splices enforce $\Delta Z \ge \max(1.3 L_{lap}, 600\text{ mm})$. For $\phi 20$ ($L_{lap} = 700\text{ mm}$), stagger offset is $910\text{ mm}$. On the 5-story $18\text{ m}$ stack, Tier 0 non-staggered splice elevation is $7,200\text{ mm}$ and staggered splice elevation is $4,510\text{ mm}$, yielding $\Delta Z = 2,690\text{ mm} \ge 910\text{ mm}$.
- **Commercial 11.7m bar splitting**: Verified on 1, 2, 3, 4, 5, and 10-story stacks. All individual segment lengths remain $\le 11,700\text{ mm}$ and consecutive segments maintain exact continuity overlap equal to $L_{lap}$.
- **Defect Note**: Infinite while-loop when $2 \times \text{storyHeight} \le L_{lap}$ (Challenge 1).

### 3. `ColumnConfinementCalculator.cs`
- **Boundary checks**: Verified $l_0 = \max(b, h, H_n/6, 500\text{ mm})$ across all aspect ratios and clear heights.
- **Spacing checks**: Dense zone spacing $a_1 \le 100\text{ mm}$ (and $\ge 50\text{ mm}$); sparse midspan spacing $a_2 \le 200\text{ mm}$ (and $\ge 100\text{ mm}$).
- **Short column protection**: When clear height is low ($H_n = 800\text{ mm}$), $l_0$ is clamped to $H_n / 2 = 400\text{ mm}$ to prevent top/bottom dense zone overlap, resulting in 0 mid-sparse ties and clean dense sets.
- **Joint ties**: $a_{joint} = 150\text{ mm}$, minimum 2 sets inside beam-column joint.

### 4. `TcvnColumnRebarOptimizer.cs`
- **Nominal columns ($400\times 400, 500\times 500, 600\times 800$)**: Optimizer successfully finds valid configurations with 4 corner bars + symmetric side bars along $B$ and $H$. All solutions satisfy $A_{st} \ge A_{st,req}$, $0.6\% \le \rho \le 3.5\%$, and $s_{clear} \ge 50\text{ mm}$.
- **Narrow columns ($200\times 200$)**: For buildable $A_{st} \le 1,250\text{ mm}^2$, optimizer correctly selects 4 corner bars only ($4\phi 18$ or $4\phi 20$) with zero side bars (`SideB = 0, SideH = 0`), preserving clear spacing $s_{clear} \ge 84.0\text{ mm} \ge 50.0\text{ mm}$.
- **Defect Note**: When $A_{st,req}$ exceeds section capacity, `CreateFallback` generates unbuildable overlapping bars (Challenge 2).

---

## Unchallenged Areas
- AutoCAD command dispatch controllers (`ColumnDrawController`, `ColumnTakeoffController`) and Revit native rebar generation (`ColumnRebarGenerator.cs`) require live AutoCAD 2026 / Revit 2026 process execution, which is out-of-scope for pure domain algorithmic verification.
- ClosedXML Excel sheet generation was validated structurally for schemas but not stressed for multi-megabyte workbook loads.
