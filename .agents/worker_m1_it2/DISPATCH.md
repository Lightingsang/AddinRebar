# DISPATCH — worker_m1_it2

Role: M1/M2 Remediation Implementation Worker
Working Directory: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m1_it2

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Context & Input Remediation Plans
- Test Integrity Remediation Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_it2_1\remediation_plan.md`
- Stirrup & Skin Spacing Remediation Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_it2_2\remediation_plan.md`
- Special Bar Bounds, Layer 2 & Polyline Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_it2_3\remediation_plan.md`
- Authoritative Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`

## Exclusive Write Ownership
You exclusively own and may edit files in:
- `HPRebar/HPRebar.Core/BeamRebar/`
- `HPRebar/HPRebar.Core.Tests/BeamRebar/`

## Tasks
1. **Fix Test Integrity Violations** in `HPRebar.Core.Tests/BeamRebar/BeamMainBarCalculatorTests.cs`:
   - Replace lines 141–147 (`LapLengthCalculatesCorrectlyFromMultiplierAndBarDiameter`) with genuine splice overlap test invoking `BeamMainBarCalculator.ComputeTopMainBars` and measuring real X-overlap ($1000.0$ mm).
   - Replace lines 233–249 (`MultiLayerTopBarsOffsetSecondLayerVerticallyWithSpecifiedGap` and `MultiLayerBottomBarsOffsetSecondLayerVerticallyUpwards`) with authentic tests invoking `BeamMainBarCalculator` and `BeamAdditionalBarCalculator` and asserting co-planar Layer 1 elevation and vertical $\Delta Z = 50.0$ mm offset.
2. **Fix Duplicate Stirrup Collision** in `HPRebar.Core/BeamRebar/Calculators/BeamStirrupDistributionCalculator.cs`:
   - Calculate Zone 2 symmetrically within the physical gap $L_{gap} = \text{startX}_3 - \text{lastX}_1$.
   - Intervals: $\text{intervals}_2 = \lceil (L_{gap} / s_2) - 2.0 - 10^{-9} \rceil$.
   - Update expected counts in `BeamStirrupDistributionCalculatorTests.cs` (42, 35, 119) and add anti-collision tests.
3. **Fix Skin Reinforcement Spacing** in `HPRebar.Core/BeamRebar/Calculators/BeamSideBarCalculator.cs`:
   - Calculate rows from clear depth: $n_{rows} = \max(1, \lceil H_{clear} / 300.0 \rceil - 1)$ for $H \ge 700$ mm.
   - Update expected row counts and parameterized vertical spacing tests in `BeamSideBarCalculatorTests.cs`.
4. **Fix Support Column Penetration** in `HPRebar.Core/BeamRebar/Calculators/BeamSpecialBarCalculator.cs`:
   - Filter hanging stirrups within $[hostSpan.StartX + Cover, hostSpan.EndX - Cover]$.
   - Clamp 45° diagonal bent ties within clear span bounds.
   - Update tests in `BeamSpecialBarCalculatorTests.cs`.
5. **Fix Exterior Support Layer 2 Top Bars** in `HPRebar.Core/BeamRebar/Calculators/BeamAdditionalBarCalculator.cs`:
   - Support both Layer 1 and Layer 2 for Support 0 and Support N.
   - Update tests in `BeamAdditionalBarCalculatorTests.cs`.
6. **Fix Hairpin 180° Hook Culling** in `HPRebar.Core/BeamRebar/Calculators/BeamMainBarCalculator.cs`:
   - In `SimplifyPolyline`, verify $\vec{v}_1 \cdot \vec{v}_2 > 0$ before culling collinear points, preserving 180° direction reversals.
   - Add test in `BeamMainBarCalculatorTests.cs`.
7. **Verification**:
   - Write your handoff report to `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m1_it2\handoff.md`.
   - Report completion to orchestrator via send_message.

## 2026-09-07T08:05:27Z
Received dispatch to execute M1/M2 remediation across 5 calculator classes and 5 test classes.
