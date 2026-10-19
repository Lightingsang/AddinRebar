# Handoff Report — Milestone M2 Hardening (Phase 3: Column & Shear Wall Detailing)

**Author**: `worker_m2_hardening` (Implementer / QA / Specialist)  
**Parent**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`  
**Date**: 2026-10-17  
**Status**: Task Complete (Hard Handoff)

---

## 1. Observation

1. **Defect 1 (`ColumnSpliceCalculator.cs`)**:
   - In `ColumnSpliceCalculator.BuildSegments` (lines 88–117), when $2 \cdot \text{storyHeight} \le L_{lap}$ (e.g. `storyHeight = 400.0`, `diameter = 25`, $L_{lap} = 875.0$), `targetSpan = 800.0`, `maxAllowed = 800.0`, `currentEnd = currentStart + 800.0`, and `currentStart = currentEnd - lap = currentStart - 75.0`.
   - Result: `currentStart` regressed backwards ($0 \to -75 \to -150 \to \dots \to -\infty$), resulting in process hang and timeout in `scripts/empirical_challenge_m2_column.ps1` Challenge 5.1.

2. **Defect 2 (`TcvnColumnRebarOptimizer.cs`)**:
   - In `CreateFallback` (lines 84–103), when `targetAst = 10000.0` was requested on a $200\times 200$ section, `neededCount = 32`, producing `SideCountB = 7`, `SideCountH = 7`.
   - Result: `CalculateMinClearSpacingMm` returned $s_{clear} = -7.0\text{ mm}$ with overlapping bars and reinforcement ratio $\rho = 25.13\%$, without marking non-compliance (`IsCompliant`).

3. **Defect 3 (`ColumnCrankBendCalculator.cs`)**:
   - Line 50 evaluated: `double effectiveCrankLen = Math.Min(crankLen, Math.Max(crankLen, beamDepthMm));`.
   - Result: Algebraic identity $\min(a, \max(a, b)) \equiv a$ evaluated to `crankLen` under all inputs, ignoring shallow beam depths (e.g., $h_{beam} = 200\text{ mm} < L_{crank} = 300\text{ mm}$).

4. **Defect 4 (`WallScheduleAggregator.cs`)**:
   - `AddWebMeshItems` (lines 67–120) omitted `mesh.UBarCount` / `mesh.UBarLengthMm`.
   - `AddOpeningItems` (lines 122–145) only scheduled `DiagonalBarCount`, omitting extra jamb vertical bars and extra header/sill horizontal bars.
   - Result: `run_m2_wall_stress_tests.ps1` PROBE-WPS-02 reported: `OpeningsJamb=False, Header=False, UBar=False`.

5. **Defect 5 (`WallOpeningModel.cs`)**:
   - Line 18 defaulted `public double DiagonalBarLengthMm { get; set; } = 900.0;`.
   - Result: `WallOpeningTrimmerCalculator.CalculateTrimmers` returned $900.0\text{ mm} < 2 \cdot L_{anc} = 1000.0\text{ mm}$ for standard $L_{anc} = 500\text{ mm}$ when using default constructor.

6. **Build & Test Outputs**:
   - `dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1` compiled with 0 warnings and 0 errors.
   - `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`:
     - 664 / 664 tests passed (100% green across all 5 test assemblies: 340 Core, 52 AutoCad, 152 E2E, 78 Revit, 42 UI).
   - Adversarial stress tests:
     - `scripts/empirical_challenge_m2_column.ps1`: 113 / 113 nominal tests passed, Challenge 5.1 loop bug eliminated, Challenge 5.2 unbuildable fallback fixed.
     - `HPKata/tools/run_m2_wall_stress_tests.ps1`: 16 / 16 probes passed (100%), PROBE-WPS-02 reported `OpeningsJamb=True, Header=True, UBar=True`.

---

## 2. Logic Chain

1. **Remediation for Item 1**:
   - Setting `minAdvancement = Math.Max(storyHeight > 0.0 ? storyHeight : 500.0, 500.0)` ensures a strictly positive floor for each bar segment.
   - Clamping `targetSpan = Math.Max(storyHeight * 2.0, lap + minAdvancement)` guarantees $currentEnd - lap \ge currentStart + minAdvancement > currentStart$.
   - The loop condition `while (currentStart < topZ)` strictly terminates in finite iterations $\le \lceil \text{totalHeight} / 500 \rceil$.

2. **Remediation for Item 2**:
   - Adding `IsCompliant` to `ColumnSectionModel` allows consumers to distinguish between fully compliant sections and fallback sections.
   - `CalculateMaxSideBars` computes the geometric threshold: $\lfloor (2 \cdot coreX) / (s_{\min} + d) \rfloor - 1$. For $200\times 200$, this yields 0 side bars.
   - Clamping `SideCountB` and `SideCountH` preserves $s_{clear} \ge 50\text{ mm}$ ($84.0\text{ mm}$ for $200\times 200$), preventing overlapping bars while explicitly signaling `IsCompliant = false`.

3. **Remediation for Item 3**:
   - Changing the expression to `beamDepthMm > 0.0 ? Math.Min(crankLen, beamDepthMm) : crankLen` ensures that when beam depth is shallow ($200\text{ mm} < 300\text{ mm}$), the vertical crank rise is bounded by $200\text{ mm}$.

4. **Remediation for Item 4**:
   - Refactored `WallScheduleAggregator` into modular helper methods ($\le 50$ lines each): `AddUBarItems` (generating U-bar edge closures with ShapeCode `"05"` and mark suffix `"-UBar"`), `AddJambExtraItem` (generating jamb extra bars with mark suffix `"-Jamb"`), and `AddHeaderExtraItem` (generating header extra bars with mark suffix `"-Header"`).
   - All 3 categories of reinforcement now appear in the bill of quantities and takeoffs.

5. **Remediation for Item 5**:
   - Setting `DiagonalBarLengthMm = 0.0` in `WallOpeningModel` allows `opening.DiagonalBarLengthMm > 0.0 ? ... : 2.0 * anchorageLengthMm` to dynamically evaluate to $2 \cdot L_{anc} = 1000\text{ mm}$ by default.

6. **Regression Verification**:
   - 5 dedicated unit tests were added across `ColumnSpliceCalculatorTests.cs`, `TcvnColumnRebarOptimizerTests.cs`, `ColumnCrankBendCalculatorTests.cs`, `ColumnWallPersistenceTests.cs`, and `WallOpeningTrimmerTests.cs` to permanently protect against regression.

---

## 3. Caveats

No caveats. All 5 defects/improvements were fully addressed directly in domain source code, verified with both standard unit test runners and adversarial PowerShell scripts, and adhere strictly to repository clean code rules.

---

## 4. Conclusion

Milestone M2 (Phase 3: Column & Shear Wall Shop Drawing) hardening is **COMPLETE** and verified 100% green.
- Critical infinite loop in `ColumnSpliceCalculator` is eliminated.
- Unbuildable fallback in `TcvnColumnRebarOptimizer` is eliminated and non-compliance is properly flagged.
- Tautological identity in `ColumnCrankBendCalculator` is corrected.
- Wall schedule aggregation in `WallScheduleAggregator` is fully populated with U-bars, jamb, and header bars.
- Dynamic diagonal trimmer calculation in `WallOpeningModel` operates correctly.
- Clean code governance is fully maintained ($\le 300$ lines/file, $\le 50$ lines/method, 0 dead code, 0 unannotated catches).

---

## 5. Verification Method

To independently verify the solution:

1. **Solution Compilation**:
   ```powershell
   dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1
   ```
   *Expected*: 0 Warning(s), 0 Error(s).

2. **Full Test Suite Execution**:
   ```powershell
   dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"
   ```
   *Expected*: 664 tests executed, 664 passed, 0 failed, 100% green.

3. **Wall Adversarial Stress Suite**:
   ```powershell
   powershell -ExecutionPolicy Bypass -File HPKata/tools/run_m2_wall_stress_tests.ps1
   ```
   *Expected*: Total: 16 | Pass: 16 | Fail: 0 (including PROBE-WPS-02: `OpeningsJamb=True, Header=True, UBar=True`).

4. **Column Empirical Challenge Suite**:
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts/empirical_challenge_m2_column.ps1
   ```
   *Expected*: Challenge 5.1 terminates cleanly without hang; Challenge 5.2 returns non-negative clear spacing ($s_{clear} \ge 50\text{ mm}$).

5. **Clean Code Verification**:
   Inspect line counts and method sizes of all modified files:
   - `HPKata/HPKata.Core/Columns/Calculators/ColumnSpliceCalculator.cs` (127 lines)
   - `HPKata/HPKata.Core/Columns/Calculators/TcvnColumnRebarOptimizer.cs` (136 lines)
   - `HPKata/HPKata.Core/Columns/Models/ColumnSectionModel.cs` (109 lines)
   - `HPKata/HPKata.Core/Columns/Calculators/ColumnCrankBendCalculator.cs` (57 lines)
   - `HPKata/HPKata.Core/Walls/Persistence/WallScheduleAggregator.cs` (213 lines)
   - `HPKata/HPKata.Core/Walls/Models/WallOpeningModel.cs` (26 lines)
