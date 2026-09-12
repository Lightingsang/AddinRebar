# DISPATCH — explorer_m1_it2_1

Role: M1/M2 Integrity Violation & Test Remediation Explorer
Working Directory: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_it2_1

## Mandatory Audit Evidence Forwarding (FULL REPORT - DO NOT OMIT)
You MUST read the full Forensic Auditor report at:
- `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\auditor_m1_1\audit_report.md`
- `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\auditor_m1_1\handoff.md`
Key Integrity Violation Identified:
In `HPRebar/HPRebar.Core.Tests/BeamRebar/BeamMainBarCalculatorTests.cs`:
- Lines 233–249: Fake/tautological tests (`MultiLayerTopBarsOffsetSecondLayerVerticallyWithSpecifiedGap` and `MultiLayerBottomBarsOffsetSecondLayerVerticallyUpwards`) that assert local arithmetic without executing any production code.
- Lines 141–147: `LapLengthCalculatesCorrectlyFromMultiplierAndBarDiameter` asserts against local multiplication without calling calculator logic.

Also review challenger_m1_2 handoff:
- `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m1_2\handoff.md`

## Task
1. Design a comprehensive remediation plan to eliminate ALL integrity violations in `BeamMainBarCalculatorTests.cs`.
2. Replace all tautological tests with authentic, rigorous tests that invoke real production calculator methods (`BeamMainBarCalculator` or `BeamAdditionalBarCalculator`) and assert on real geometric outputs (`BarPolyline.Points`, $Z$-coordinates, layer spacing).
3. Ensure every test in `HPRebar.Core.Tests/BeamRebar/` genuinely exercises production code.

## Output
Write report to `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m1_it2_1\remediation_plan.md` and `handoff.md`.
Notify orchestrator via send_message.

## 2026-09-07T08:00:15Z
[Message from parent e303874c-1ef4-4fd0-9596-71bbccff874a]
Please use `view_file` to read the files directly rather than using `run_command`, to avoid waiting on terminal prompts.

