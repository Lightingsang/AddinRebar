# DISPATCH — 2026-10-17T09:42:00Z

## Task Assignment for worker_m2_hardening (Milestone M2 Hardening Worker, Iteration 2)

You are **worker_m2_hardening**, the Hardening Worker for Milestone M2 (Phase 3: Column & Shear Wall Shop Drawing).
Your working directory is: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_hardening\`
Your parent conversation ID is: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

### MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

### MANDATORY REFERENCES:
1. `ORIGINAL_REQUEST.md` at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`.
2. Master Project Scope at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`.
3. Challenge Reports:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m2_1\challenge_report.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m2_2\challenge_report.md`
4. Gate Status at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\GATE_STATUS.md`.

### REMEDIATION DEFECTS TO RESOLVE:

1. **`HPKata.Core/Columns/Calculators/ColumnSpliceCalculator.cs` [CRITICAL]**:
   - Defect: In `SegmentCommercialColumnBars`, when $2 \times \text{storyHeight} \le L_{\text{lap}}$, `currentStart += 2 * storyHeight - L_lap` results in a negative or zero delta ($0 \to -75 \to -150\dots$), causing an infinite loop / OOM hang.
   - Fix: Ensure the advancing step is always strictly positive: `double step = Math.Max(storyHeight, 2.0 * storyHeight - lapLengthMm);` (or at least `Math.Max(500.0, ...)`). Verify that `currentStart` strictly increases on every iteration and loop is guaranteed to terminate.

2. **`HPKata.Core/Columns/Calculators/TcvnColumnRebarOptimizer.cs` [MEDIUM]**:
   - Defect: In `CreateFallback`, when required $A_{st}$ exceeds section capacity (e.g. $> 3.5\% A_g$) on narrow sections ($200\times 200$), fallback silently generates overlapping bars with negative clear spacing ($s_{\text{clear}} = -7.0\text{ mm}$) and false compliance.
   - Fix: In `CreateFallback`, check actual clear spacing. If bars cannot fit with $s_{\text{clear}} \ge 50\text{ mm}$ (or $s_{\text{clear}} < d$), clamp bar count to fit and set `IsCompliant = false`. Never emit overlapping bars with negative clear gap.

3. **`HPKata.Core/Columns/Calculators/ColumnCrankBendCalculator.cs` [LOW]**:
   - Defect: Line 50 `double verticalCrank = Math.Min(crankLen, Math.Max(crankLen, beamDepthMm));` is a mathematical identity $\min(a, \max(a, b)) \equiv a$, completely bypassing the beam depth cap.
   - Fix: Change to `double verticalCrank = beamDepthMm > 0.0 ? Math.Min(crankLen, beamDepthMm) : crankLen;` so crank vertical rise is legitimately bounded by the beam-column joint depth.

4. **`HPKata.Core/Walls/Persistence/WallScheduleAggregator.cs` [IMPROVEMENT]**:
   - Fix: In `AddWebMeshItems`, append U-bar edge closures (`mesh.UBarCount`, `mesh.UBarLengthMm`) using standard shape code (`dang05` / `dang25`). In `AddOpeningItems`, append extra jamb vertical bars and extra header/sill horizontal bars to the schedule.

5. **`HPKata.Core/Walls/Models/WallOpeningModel.cs` [IMPROVEMENT]**:
   - Fix: Set default `DiagonalBarLengthMm = 0.0;` so `WallOpeningTrimmerCalculator` automatically computes $2 \cdot L_{\text{anc}}$ dynamically unless explicitly overridden.

6. **Regression Tests**:
   - Add unit tests in `HPKata.Core.Tests` verifying:
     - Short story height ($2 \times H \le L_{\text{lap}}$) terminates cleanly without infinite loop.
     - Narrow column section ($200\times 200$) with extreme $A_{st}$ sets `IsCompliant = false` and has $s_{\text{clear}} \ge 0$.
     - Crank length with shallow beam depth ($h_{\text{beam}} < L_{\text{crank}}$) is properly clamped to $h_{\text{beam}}$.
     - U-bar, jamb, and header items appear in wall schedule items.

### BUILD & TEST MANDATE:
- Run: `dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1` (0 warnings, 0 errors).
- Run: `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"` (100% green).

### CLEAN CODE RULES:
- All files <= 300 lines (Rule C2).
- All methods <= 50 lines (Rule M3).
- 0 dead code/TODOs (Rule Q-B1).
- 0 unannotated empty catch blocks (Rule Q-B2).
- 304.8 strictly in `RevitUnitConverter.cs` (Rule R5).

DELIVERABLE:
Write your implementation report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_hardening\changes.md`
and a concise handoff.md in your working directory.
When complete, notify parent via send_message with build/test results and report path.
