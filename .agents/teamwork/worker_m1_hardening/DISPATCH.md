## 2026-10-17T08:25:02Z
You are worker_m1_hardening, the Hardening Worker for Milestone M1 (Phase 2: Beam Shop Drawing & ETABS Integration), Iteration 3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_hardening\
Your parent conversation ID is: a7a2dbbb-6b7d-4279-98cd-1216d8280d80

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

MANDATORY FIRST STEPS:
1. Read the authoritative original request at:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under header ## 2026-10-17T06:46:41Z)
2. Read the master project scope at:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md
3. Read the DEAD ENDS log at:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\DEAD_ENDS.md
4. Read the challenge reports from Challenger 1 and Challenger 2:
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m1_1\challenge_report.md
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m1_2\challenge_report.md

YOUR MISSION:
Resolve all 6 defects from Challenger 1 and all 5 defects from Challenger 2:

1. `TcvnBeamRebarOptimizer.cs`:
   - In fallback, calculate genuine layer counts and diameters; never emit overlapping bars with negative clear gap. If usable width cannot fit bars, mark `IsCompliant = false`.
   - On narrow beams (e.g. b = 100mm), verify actual clear spacing for default bars; if clear gap < 25mm, mark `IsCompliant = false`.
   - Enforce minimum reinforcement ratio As_min >= 0.001 * b * h0 (TCVN 5574:2018).
2. `EtabsBeamImportService.cs`:
   - In `PopulateLongitudinalBars`, when the optimizer selects dual-diameter combinations (`SecondaryCount > 0` and `SecondaryDiameterMm > 0`), genuinely add secondary bar items to `BeamRebarLayout.TopBars` and `BottomBars` so dual-diameter layouts retain 100% of required reinforcement without steel deficit.
3. `BeamStirrupZoneCalculator.cs`:
   - Fix rounding overflow: ensure `2 * denseZoneLength <= clearSpanLength`. If rounding up exceeds half span, clamp down so dense zones never overlap or exceed clear span.
   - On cantilever overhangs, do not place a dense stirrup zone at the free tip (dense zone at support only).
4. `BeamCurtailmentCalculator.cs`:
   - On cantilever overhangs, top tension bars must run 100% of the cantilever length plus hook at the free end.
5. `EtabsTextTableParser.cs`:
   - In `ParseDoubleInvariant`, properly normalize comma decimal separators so `"850,5"` parses to `850.5` without 10x inflation.
6. `EtabsFrameChainBuilder.cs`:
   - Update collinear frame chaining to support bidirectional expansion from interior seeds and handle reversed beam vectors (candStart to candEnd reversed: u1 . u2 ~ -1).
7. `FramingRebarGeometryCalculator.cs`:
   - In `CalculateTransverseOffsets`, when `barCount == 0`, return an empty list `[]`. Never return `[0.0]`.
8. Tests:
   - Ensure all tests in `BeamCoreStressTests.cs` and throughout `HPKata.slnx` pass 100%.

BUILD & TEST MANDATE:
- Run: dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1
- Run: dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"
- Verify 100% pass rate, 0 warnings, 0 errors.

CLEAN CODE MANDATE:
- All files <= 300 lines (Rule C2).
- All methods <= 50 lines (Rule M3).
- 0 dead code/TODOs (Rule Q-B1).
- 0 unannotated empty catches (Rule Q-B2).
- 304.8 strictly in `RevitUnitConverter.cs` (Rule R5).

DELIVERABLE:
Write your implementation report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1_hardening\changes.md`
and a structured handoff.md in your working directory.
When complete, notify parent via send_message with build/test results and report path.
