# Implementation Report — Milestone M2 Hardening (Phase 3: Column & Shear Wall Detailing)

**Author**: `worker_m2_hardening` (Milestone M2 Hardening Worker)  
**Date**: 2026-10-17  
**Scope**: Resolution of 5 defect & improvement items identified by empirical challengers `challenger_m2_1` and `challenger_m2_2`, plus comprehensive regression test suite.

---

## 1. Summary of Changes

| Item | Target File | Nature | Description of Remediation |
|---|---|---|---|
| 1 | `HPKata.Core/Columns/Calculators/ColumnSpliceCalculator.cs` | **CRITICAL BUGFIX** | In `BuildSegments`, guaranteed that advancement step `minAdvancement = Math.Max(storyHeight, 500)` strictly exceeds lap length by clamping `targetSpan = Math.Max(storyHeight * 2.0, lap + minAdvancement)` and ensuring `nextStart > currentStart`. Completely eliminates infinite loop and OOM hang on short story heights ($2 \cdot H \le L_{lap}$). |
| 2 | `HPKata.Core/Columns/Calculators/TcvnColumnRebarOptimizer.cs` & `ColumnSectionModel.cs` | **MEDIUM BUGFIX** | Added `IsCompliant` property to `ColumnSectionModel` (default `true`). In `CreateFallback`, calculate maximum physically fittable side bars along B and H (`CalculateMaxSideBars`), clamp requested side bar counts to physical limits ($s_{clear} \ge 50\text{ mm}$), ensure non-negative clear spacing (`EnsureNonOverlapping`), and mark `IsCompliant = false`. |
| 3 | `HPKata.Core/Columns/Calculators/ColumnCrankBendCalculator.cs` | **LOW BUGFIX** | Replaced tautological expression `Math.Min(crankLen, Math.Max(crankLen, beamDepthMm))` (which identically reduced to `crankLen` under all inputs) with `beamDepthMm > 0.0 ? Math.Min(crankLen, beamDepthMm) : crankLen`, legitimately bounding the crank vertical rise by the joint beam depth. |
| 4 | `HPKata.Core/Walls/Persistence/WallScheduleAggregator.cs` | **IMPROVEMENT** | Added modular schedule helpers: `AddUBarItems` (generating U-bar edge closure items with ShapeCode `"05"` / `dang05`), `AddJambExtraItem` (generating extra opening vertical jamb bars), and `AddHeaderExtraItem` (generating extra opening header/sill horizontal bars). |
| 5 | `HPKata.Core/Walls/Models/WallOpeningModel.cs` | **IMPROVEMENT** | Changed default value of `DiagonalBarLengthMm` from `900.0` to `0.0`. Allows `WallOpeningTrimmerCalculator.CalculateTrimmers` to dynamically compute $2 \cdot L_{anc}$ unless explicitly overridden. |
| 6 | `HPKata.Core.Tests/` (4 test files) | **REGRESSION TESTS** | Added 5 unit tests validating: short story height loop termination, extreme $A_{st}$ on $200\times 200$ section fallback compliance and clear spacing, crank length shallow beam clamping, wall schedule inclusion of U-bars/jamb/header items, and default opening dynamic $2 \cdot L_{anc}$ calculation. |

---

## 2. File-by-File Detailed Diff Documentation

### 2.1 `HPKata.Core/Columns/Calculators/ColumnSpliceCalculator.cs`
- **Method Modified**: `BuildSegments` (lines 77–125)
- **Change**: Added `minAdvancement = Math.Max(storyHeight > 0.0 ? storyHeight : 500.0, 500.0)`. Clamped `targetSpan` to `Math.Max(storyHeight * 2.0, lap + minAdvancement)` and `staggerSpan` to `Math.Max(storyHeight + stagger, lap + minAdvancement)`. Added defensive check `if (nextStart <= currentStart) nextStart = currentStart + minAdvancement;`.
- **Clean Code**: 127 lines total ($\le 300$), method is 45 lines ($\le 50$).

### 2.2 `HPKata.Core/Columns/Models/ColumnSectionModel.cs`
- **Property Added**: `public bool IsCompliant { get; set; } = true;`
- **Clean Code**: 109 lines total ($\le 300$).

### 2.3 `HPKata.Core/Columns/Calculators/TcvnColumnRebarOptimizer.cs`
- **Methods Modified / Added**:
  - `Optimize`: Passes `coverMm` and `tieDiaMm` to `CreateFallback`.
  - `EvaluateCandidate`: Sets `candidate.IsCompliant = true`.
  - `CreateFallback`: Uses `CalculateMaxSideBars` to determine maximum side bars along B and H with $s_{clear} \ge 50\text{ mm}$, clamps requested side bars, sets `IsCompliant = false`, and invokes `EnsureNonOverlapping`.
  - `CalculateMaxSideBars`: Pure geometric calculation for side bar capacity.
  - `EnsureNonOverlapping`: Removes side bars if section core is too tight for positive clearance.
- **Clean Code**: 136 lines total ($\le 300$), largest method is 33 lines ($\le 50$).

### 2.4 `HPKata.Core/Columns/Calculators/ColumnCrankBendCalculator.cs`
- **Method Modified**: `CalculateCrankElevation` (lines 46–56)
- **Change**: `double effectiveCrankLen = beamDepthMm > 0.0 ? Math.Min(crankLen, beamDepthMm) : crankLen;`
- **Clean Code**: 57 lines total ($\le 300$), method is 10 lines ($\le 50$).

### 2.5 `HPKata.Core/Walls/Persistence/WallScheduleAggregator.cs`
- **Methods Modified / Added**:
  - `AddWebMeshItems`: Invokes `AddUBarItems`.
  - `AddUBarItems`: Appends U-bar edge closure (`ShapeCode = "05"`, `ComponentMark = $"{compMark}-UBar"`).
  - `AddOpeningItems`: Refactored to modularly dispatch diagonal, jamb, and header items.
  - `AddDiagonalTrimmerItem`: Appends diagonal bars.
  - `AddJambExtraItem`: Appends extra jamb vertical bars.
  - `AddHeaderExtraItem`: Appends extra header/sill horizontal bars.
- **Clean Code**: 213 lines total ($\le 300$), largest method is 35 lines ($\le 50$).

### 2.6 `HPKata.Core/Walls/Models/WallOpeningModel.cs`
- **Property Modified**: `public double DiagonalBarLengthMm { get; set; } = 0.0;`
- **Clean Code**: 26 lines total ($\le 300$).

### 2.7 `HPKata.Core.Tests/` (Test Additions)
- `ColumnSpliceCalculatorTests.cs`:
  - Added `SegmentCommercialColumnBars_ShortStoryHeight_TerminatesCleanlyWithoutInfiniteLoop`.
- `TcvnColumnRebarOptimizerTests.cs`:
  - Added `Optimize_NarrowSectionWithExtremeAst_SetsNonCompliantAndMaintainsPositiveClearSpacing`.
- `ColumnCrankBendCalculatorTests.cs`:
  - Added `CalculateCrankElevation_ShallowBeamDepth_ClampsCrankVerticalRiseToBeamDepth`.
- `ColumnWallPersistenceTests.cs`:
  - Added `WallScheduleAggregator_IncludesUBarsAndOpeningJambHeaderTrimmers`.
- `WallOpeningTrimmerTests.cs`:
  - Added `CalculateTrimmers_DefaultConstructor_DynamicallyCalculatesTwoTimesAnchorageLength`.

---

## 3. Verification & Test Execution Results

- **Build Command**: `dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1`
  - Result: **0 Warnings, 0 Errors** across all 10 projects.
- **Test Command**: `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`
  - Total Tests: **664 passed, 0 failed, 0 skipped (100% green)**:
    - `HPKata.Core.Tests`: 340 passed (expanded from 335)
    - `HPKata.AutoCad.Tests`: 52 passed
    - `HPKata.E2E.Tests`: 152 passed
    - `HPKata.Revit.Tests`: 78 passed
    - `HPKata.UI.Tests`: 42 passed
- **PowerShell Adversarial Stress Suites**:
  - `scripts/empirical_challenge_m2_column.ps1`:
    - Challenge 5.1 (Infinite loop on short story): **FIXED** (no hang, clean termination).
    - Challenge 5.2 (Unbuildable fallback on $200\times 200$): **FIXED** ($s_{clear} = 84\text{ mm} \ge 50\text{ mm}$, $\rho = 3.14\% \le 3.55\%$, `IsCompliant = false`).
    - Challenge 5.3 (Tautological identity): **FIXED** in source and covered by unit test.
  - `HPKata/tools/run_m2_wall_stress_tests.ps1`:
    - PROBE-WPS-02: **PASS** (`Items: 9. OpeningsJamb=True, Header=True, UBar=True`).
    - PROBE-WTC-02: **PASS** (`Auto(0mm)->1000mm, Default->1000mm`).
    - Overall: **16/16 Probes Passed (100%)**.
