# DISPATCH — 2026-10-17T09:27:00Z

## Task Assignment for challenger_m2_1 (Column Domain Stress Tester)

You are **challenger_m2_1**, performing adversarial stress-testing and empirical verification on Milestone M2 (Phase 3: Column & Shear Wall Shop Drawing).
Your working directory is: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m2_1\`
Your parent conversation ID is: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

### MANDATORY REFERENCES:
1. `ORIGINAL_REQUEST.md` at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`.
2. Master Project Scope at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`.
3. Worker's Changes & Handoff:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_column_wall\changes.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_column_wall\handoff.md`

### YOUR MISSION:
Empirically stress-test the Column domain mathematics and detailing rules:
1. Stress-test `ColumnCrankBendCalculator.cs`:
   - Step-down boundaries: Delta = 0 (straight), Delta = 25mm, Delta = 50mm (crank slope 1:6, length = 6*Delta), Delta = 50.1mm (cutoff + starter dowel), Delta = 100mm.
   - Negative or zero step-down handling (eccentric transitions).
2. Stress-test `ColumnSpliceCalculator.cs`:
   - Staggering rules: Verify 50% staggered splices with delta Z >= max(1.3 * L_lap, 600mm).
   - Commercial 11.7m bar splitting on tall multi-story column stacks (e.g. 5-story 18m column run).
3. Stress-test `ColumnConfinementCalculator.cs`:
   - Boundary checks: l0 = max(b, h, H_n/6, 500mm).
   - Spacing checks: Dense zone tie spacing a1 <= 100mm, midspan sparse zone a2 <= 200mm.
4. Stress-test `TcvnColumnRebarOptimizer.cs`:
   - High axial load / Ast requirements: verify corner bars, side bar counts along b and h, clear spacing s_clear >= 50mm, steel ratio 0.6% <= rho <= 3.5%.
   - Narrow column sections (e.g. 200x200) ensuring clear spacing cannot become negative or unbuildable.
5. Build & Test mandate:
   If running tests, ALWAYS use `--disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`.
6. Verdict:
   Provide an empirical verification verdict: **CONFIRMED_CORRECT** or **DEFECT_FOUND**.

DELIVERABLE:
Write your empirical test report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m2_1\challenge_report.md`
and a concise handoff.md in your working directory.
When complete, notify parent via send_message with your verdict and report path.
