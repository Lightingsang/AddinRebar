# Dispatch: challenger_m3_1

## Role & Mission
You are `challenger_m3_1`, performing adversarial stress-testing and empirical verification on Milestone M3 (Phase 4: Foundation, Slab, Stair & Lintel Detailing), specifically the Foundation and Slab domain calculators.

## Mission Scope
1. Empirically stress-test `PileCoordinateGenerator.cs` & `PileCapRebarCalculator.cs`:
   - Centroid invariance: verify center of mass (X_bar, Y_bar) == (0, 0) for 1 to 9+ piles.
   - Spacing boundaries: custom spacing vs default 3D, edge distances ce >= 1.0D or 250mm.
   - Embedment checks: net cap volume = gross volume - n * (pi * D^2 / 4) * h_emb.
2. Empirically stress-test `FootingGeometryCalculator.cs`:
   - Trapezoidal frustum volume: V = h/3 * (A1 + A2 + sqrt(A1 * A2)).
   - Sloped vs stepped footings.
3. Empirically stress-test `SlabRebarCalculator.cs` & `SlabChairCalculator.cs`:
   - Chair stool height clamping: h_stool = h_s - c_top - c_bot - sum(d_top) - sum(d_bot,bearing), clamped >= 30mm.
   - Thin slabs (h_s <= 70mm) where rebar layers exceed slab thickness.
   - Polyline stepping quantization Delta L = 50mm, sub-mark clustering (1a, 1b...).
   - 4-way diagonal rebar length and spacing checks.
4. Build & Test mandate:
   If running tests, ALWAYS use `--disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`.
5. Verdict:
   Provide an empirical verification verdict: CONFIRMED_CORRECT or DEFECT_FOUND.


## 2026-10-17T11:28:52Z
You are challenger_m3_1, performing adversarial stress-testing and empirical verification on Milestone M3 (Phase 4: Foundation, Slab, Stair & Lintel Detailing), Foundation and Slab domains.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_1\
Your parent conversation ID is: a7a2dbbb-6b7d-4279-98cd-1216d8280d80

MANDATORY FIRST STEPS:
1. Read the authoritative original request at:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (under headers ## 2026-10-16T23:55:10Z and ## 2026-10-17T06:46:41Z)
2. Read the master project scope:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md
3. Read the worker's changes and handoff:
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_remediation\changes.md
   - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_remediation\handoff.md
4. Read your task dispatch:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_1\DISPATCH.md

YOUR MISSION:
Empirically stress-test the Foundation and Slab domain calculators:
1. Stress-test `PileCoordinateGenerator.cs` & `PileCapRebarCalculator.cs`:
   - Centroid invariance: verify center-of-mass (X_bar, Y_bar) == (0, 0) for 1 to 9+ piles within floating-point tolerance (< 1e-4).
   - Spacing boundaries: default 3D vs custom spacing, edge distance ce >= 1.0D or 250mm.
   - Net concrete volume deduction: gross volume - sum(pile embedment volume).
2. Stress-test `FootingGeometryCalculator.cs`:
   - Pad footing frustum volume formula: V = h/3 * (A1 + A2 + sqrt(A1 * A2)).
3. Stress-test `SlabRebarCalculator.cs` & `SlabChairCalculator.cs`:
   - Stool height clamping: h_stool = h_s - c_top - c_bot - sum(d_top) - sum(d_bot,bearing), clamped >= 30mm.
   - Degenerate/thin slabs (h_s <= 70mm) where rebar layers exceed slab thickness.
   - Polyline stepping quantization Delta L = 50mm, sub-mark clustering (1a, 1b...).
   - 4-way diagonal rebar calculation.
4. Build & Test mandate:
   If running tests, ALWAYS use `--disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`.
5. Verdict:
   Provide an empirical verification verdict: CONFIRMED_CORRECT or DEFECT_FOUND.

DELIVERABLE:
Write your empirical test report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m3_1\challenge_report.md`
and a concise handoff.md in your working directory.
When complete, notify parent via send_message with your verdict and report path.
