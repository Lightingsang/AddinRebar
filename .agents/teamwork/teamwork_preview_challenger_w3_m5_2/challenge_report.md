# Milestone 5 (M5) Adversarial Challenge Report
## Standardized Geometric Measurement & Acceptance Verification

**Agent:** Challenger 2 (`teamwork_preview_challenger_w3_m5_2`)  
**Date:** 2026-10-10  
**Overall Risk Assessment:** **HIGH**  
**Empirical Verdict:** **REQUEST_CHANGES**

---

## 1. Executive Summary

Milestone 5 introduced standardized geometric measurement and verification against Revit models, implementing direct `TargetUniqueId` lookups, pile cutoff elevation auditing, profile outline/thickness auditing, and inconclusive handling for unmeasured categories.

While all 1,090 HPBim tests and 3,920 repository-wide tests pass, adversarial empirical testing of boundary and degenerate conditions using custom test harness `tests/m5_w3_challenger_harness/` revealed **13 empirical test failures** across **4 critical attack surfaces**:
1. **Null Planned Geometry Bypass**: Elements with `item.Geometry == null` falsely pass as `Verified` with 0 discrepancies because delta calculation is silently skipped.
2. **Mismatched Geometry Spec Bypass**: Elements with incorrect geometry types (e.g. Column with `LineGeometrySpec`) bypass positional auditing and pass as `Verified`.
3. **IEEE 754 NaN Tolerance Bypass**: `double.NaN` in actual measurements (Centroid, TopElevationMm, WidthMm) evaluates `delta > tolerance` to `false`, causing corrupted elements to be stamped `Verified`.
4. **Unhandled ArgumentException Crash**: A 3-point closed profile loop `[P0, P1, P0]` triggers `Polygon2D` vertex deduplication down to 2 vertices, throwing an unhandled `ArgumentException` that crashes the verification pipeline.

---

## 2. Empirical Challenge Findings

### Challenge 1 (CRITICAL): Null Planned Geometry Silently Verified (Rule 0 Violation)
- **Assumption Challenged**: The engine assumes `HasRequiredMeasurements` guarantees sufficient data to verify an element.
- **Attack Scenario**: An `ElementPlanItem` is constructed with `Geometry = null` (e.g., from partial CAD extraction, corrupt JSON, or incomplete plan item).
- **Observed Behavior**:
  - Column with `Geometry == null` -> `AuditItem` returns `Status = Verified` (`DeltaCenterLineMm = 0.0`).
  - Pile with `Geometry == null` -> `AuditItem` returns `Status = Verified` (`DeltaCenterLineMm = 0.0`, `DeltaElevationMm = 0.0`).
  - Beam with `Geometry == null` -> `AuditItem` returns `Status = Verified` (`DeltaCurveStartMm = 0.0`, `DeltaElevationMm = 0.0`).
  - Slab with `Geometry == null` -> `AuditItem` returns `Status = Verified` (`DeltaCenterLineMm = 0.0`, `DeltaThickness = 0.0`).
  - Wall with `Geometry == null` -> `AuditItem` returns `Status = Verified`.
  - Level with `Geometry == null` -> `AuditItem` returns `Status = Verified`.
  - Grid with `Geometry == null` -> `AuditItem` returns `Status = Verified`.
- **Root Cause**:
  `HasRequiredMeasurements` checks `actual` properties only (e.g. `actual.Centroid.HasValue`) and never verifies `item.Geometry != null`. Each audit method checks `if (item.Geometry is SpecificSpec spec)`, silently defaulting deltas to `0.0` when false. With 0 discrepancies, `Status` evaluates to `Verified`.
- **Blast Radius**: Elements lacking planned geometry specifications falsely pass QA/QC with zero defects.
- **Mitigation**:
  In `HasRequiredMeasurements` (or before dispatch in `AuditItem`), verify that `item.Geometry != null`. If null, return `GeometricVerificationStatus.Inconclusive` with:
  `"Rule 0: ElementPlanItem '{item.Mark}' lacks planned geometry specification."`

---

### Challenge 2 (HIGH): Mismatched Geometry Spec Bypasses Verification
- **Assumption Challenged**: The engine assumes `item.Geometry` matches the element's `CoordinationCategory`.
- **Attack Scenario**: An element plan item has an incompatible geometry type (e.g., Column with `LineGeometrySpec` or Beam with `PointGeometrySpec`).
- **Observed Behavior**:
  - Column located at actual coordinates `(9999, 9999, 0)` with a `LineGeometrySpec` returned `Status = Verified` because `item.Geometry is not PointGeometrySpec` returned `0.0`.
  - Beam with `PointGeometrySpec` bypassed endpoints and elevation checks, returning `Status = Verified`.
- **Blast Radius**: Elements with corrupt geometry spec types bypass spatial auditing and are falsely verified.
- **Mitigation**:
  Validate `item.Geometry` against expected specification type in `HasRequiredMeasurements`. If mismatched, return `Inconclusive`.

---

### Challenge 3 (HIGH): IEEE 754 NaN Bypasses Tolerance Checks
- **Assumption Challenged**: Floating-point values in `ActualElementGeometry` are finite real numbers.
- **Attack Scenario**: Revit API or geometry calculation returns `double.NaN` for `Centroid.X`, `TopElevationMm`, or `WidthMm`.
- **Observed Behavior**:
  - In C# IEEE 754, `delta > tolerance` evaluates to `false` when `delta` is `NaN`.
  - Pile with `actual.Centroid = (NaN, 2000, -2500)` produced `deltaPos = NaN`, but `deltaPos > tolerance` was `false`. No discrepancy was logged, and the pile was marked `Status = Verified`!
  - Pile with `TopElevationMm = NaN` was marked `Status = Verified`.
  - Column with `WidthMm = NaN` was marked `Status = Verified`.
- **Blast Radius**: Corrupted or NaN geometric data from Revit API silently passes verification.
- **Mitigation**:
  In `HasRequiredMeasurements` and tolerance check helpers, check `double.IsNaN(...)` or `!double.IsFinite(...)`. If non-finite, treat as missing measurement (`Inconclusive`) or add defect discrepancy.

---

### Challenge 4 (HIGH): Degenerate 3-Point Closed Loop Unhandled Crash
- **Assumption Challenged**: Any `OuterLoop` with `Count >= 3` can be safely passed to `new Polygon2D(pts)`.
- **Attack Scenario**: A slab profile has a closed loop with 3 points where the last point equals the first point (`[P0, P1, P0]`), representing a collapsed 2-vertex polygon.
- **Observed Behavior**:
  `Polygon2D` constructor removes the duplicate closing vertex (`list.Count` becomes 2), then line 33 throws:
  `ArgumentException: A non-empty polygon must contain at least 3 distinct vertices. (Parameter 'vertices')`
  This exception is unhandled and crashes the caller (`AuditProfileStructure` -> `AuditItem` -> `AuditPlan`).
- **Blast Radius**: A single degenerate profile polygon crashes the entire `verify` CLI command with an unhandled exception.
- **Mitigation**:
  In `ComputeProfileCentroid`, check if distinct non-closing vertices are `>= 3` before calling `new Polygon2D(pts)`. If `< 3`, fall back to vertex average or catch `ArgumentException`.

---

## 3. Stress Test Results Summary

| Test Case | Description | Expected | Actual | Result |
|---|---|---|---|---|
| 1.1 | Column with `Geometry == null` | Inconclusive | Verified (delta=0) | **FAIL** |
| 1.2 | Pile with `Geometry == null` | Inconclusive | Verified (delta=0) | **FAIL** |
| 1.3 | Beam with `Geometry == null` | Inconclusive | Verified (delta=0) | **FAIL** |
| 1.4 | Slab with `Geometry == null` | Inconclusive | Verified (delta=0) | **FAIL** |
| 1.5 | Wall with `Geometry == null` | Inconclusive | Verified (delta=0) | **FAIL** |
| 1.6 | Level with `Geometry == null` | Inconclusive | Verified (delta=0) | **FAIL** |
| 1.7 | Grid with `Geometry == null` | Inconclusive | Verified (delta=0) | **FAIL** |
| 2.1 | Column with LineGeometrySpec | Inconclusive/Defect | Verified | **FAIL** |
| 2.2 | Beam with PointGeometrySpec | Inconclusive/Defect | Verified | **FAIL** |
| 3.1 | Pile with NaN Centroid | Inconclusive/Defect | Verified (delta=NaN) | **FAIL** |
| 3.2 | Pile with NaN TopElevationMm | Inconclusive/Defect | Verified (delta=NaN) | **FAIL** |
| 3.3 | Column with NaN WidthMm | Inconclusive/Defect | Verified (delta=NaN) | **FAIL** |
| 3.4 | Beam with Infinity TopElevationMm | Defect | Defect | **PASS** |
| 4.1 | Slab 3-point loop `[P0, P1, P0]` | Graceful handle | ArgumentException crash | **FAIL** |
| 4.2 | Collinear 3-point profile loop | Graceful handle | Handled | **PASS** |
| 5.1 | Pile without TopElevationMm | Inconclusive | Inconclusive | **PASS** |
| 5.2 | Column without Centroid | Inconclusive | Inconclusive | **PASS** |
| 5.3 | Beam without CurveStart/End | Inconclusive | Inconclusive | **PASS** |
| 5.4 | Beam without TopElevationMm | Inconclusive | Inconclusive | **PASS** |
| 5.5 | Wall without CurveStart/End | Inconclusive | Inconclusive | **PASS** |
| 5.6 | Slab missing ThicknessMm | Inconclusive | Inconclusive | **PASS** |
| 5.7 | Unmeasured Opening Category | Inconclusive | Inconclusive | **PASS** |
| 6.1 | TargetUniqueId Priority | Verified (correct UID) | Verified (correct UID) | **PASS** |
| 6.2 | Non-existent TargetUniqueId Fallback | Defect | Defect | **PASS** |
| 7.1 | OverallPassed with Inconclusive | False | False | **PASS** |
| 7.2 | InconclusiveCount Aggregation | 1 | 1 | **PASS** |
| 7.3 | VerifiedCount Aggregation | 1 | 1 | **PASS** |

**Harness Totals:** 27 Tests | 14 Passed | 13 Failed.

---

## 4. Repository Regression Verification

Empirically verified across the repository:
1. `HPBim/tests/HPBim.Core.Tests`: 482 / 482 passed
2. `HPBim/tests/HPBim.Client.Tests`: 566 / 566 passed
3. `HPBim/tests/HPBim.E2E.Tests`: 42 / 42 passed
4. `HPRebar/HPRebar.Core.Tests`: 1,682 / 1,682 passed
5. `HPRebar/HPRebar.Mcp.Server.Tests`: 118 / 118 passed
6. `McpShared/HPRebar.Mcp.Server.Core.Tests`: 915 / 915 passed
7. `McpShared/HPRebar.McpBridge.Core.Net48Tests`: 115 / 115 passed
**Total Repository Tests:** 3,920 passed (100% green, zero regressions).

---

## 5. Conclusion & Verdict

While the worker successfully implemented the core R5 deliverables (`TargetUniqueId` querying, pile cutoff elevation check, profile slab measurements, and Rule 0 generic inconclusive handling), the implementation is vulnerable to:
1. False verification on null/mismatched planned geometries.
2. False verification on NaN measurement values.
3. Unhandled crash on closed 3-point profile loops.

**Verdict:** **REQUEST_CHANGES**  
Remediation is required before Milestone 5 can be approved.
