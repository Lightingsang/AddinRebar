# DISPATCH — 2026-10-17T09:58:00Z

## Task Assignment for auditor_m2_final (Final M2 Forensic Integrity Auditor)

You are **auditor_m2_final**, the Final Forensic Integrity Auditor for Milestone M2 (Phase 3: Column & Shear Wall Shop Drawing).
Your working directory is: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m2_final\`
Your parent conversation ID is: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

### CRITICAL AUDIT MANDATE:
You are the Forensic Integrity Auditor. Your verdict is a BINARY VETO.
If you find ANY evidence of cheating, dummy/facade implementations, hardcoded mock strings, fabricated test outputs, or circumvention of requirements, you MUST report **INTEGRITY VIOLATION**.

### MANDATORY REFERENCES:
1. `ORIGINAL_REQUEST.md` at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`.
2. Master Project Scope at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`.
3. Challenge Reports:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m2_1\challenge_report.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m2_2\challenge_report.md`
4. Worker's Hardening Changes & Handoff:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_hardening\changes.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_hardening\handoff.md`

### YOUR AUDIT CHECKS:
1. Verify Resolution of all 5 Challenger Defects:
   - `ColumnSpliceCalculator.cs`: Verify `SegmentCommercialColumnBars` has strictly positive advancement (`Math.Max(storyHeight, 500.0)`) and cannot enter an infinite loop when $2 \cdot H \le L_{lap}$.
   - `TcvnColumnRebarOptimizer.cs` & `ColumnSectionModel.cs`: Verify `CreateFallback` checks clear spacing, clamps bar count, and marks `IsCompliant = false` on unbuildable sections. Verify no negative clear gap.
   - `ColumnCrankBendCalculator.cs`: Verify `verticalCrank` bounds crank length by beam depth `Math.Min(crankLen, beamDepthMm)` when `beamDepthMm > 0.0`.
   - `WallScheduleAggregator.cs`: Verify U-bar edge closures, extra jamb vertical bars, and extra header/sill horizontal bars are appended to schedule items.
   - `WallOpeningModel.cs`: Verify default `DiagonalBarLengthMm = 0.0`.
2. Static Integrity & Clean Code Governance:
   - Rule C2: Measure every `.cs` file in `HPKata/` touched by Phase 3 — must be <= 300 lines.
   - Rule M3: Measure methods in Phase 3 code — must be <= 50 lines.
   - Rule Q-B1: Zero commented-out code, zero TODOs/FIXMEs/HACKs.
   - Rule Q-B2: Zero unannotated empty catch blocks.
   - Rule R5: Constant 304.8 must appear strictly and exclusively in `RevitUnitConverter.cs`.
   - Host Neutrality: `HPKata.Core` has ZERO references to `Autodesk.*`.
3. Runtime Execution Validation:
   - Run: `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`
   - Confirm 100% tests pass (all 664 tests) without failures or file-lock collisions.
4. Audit Verdict:
   State clearly either: **CLEAN** or **INTEGRITY VIOLATION**.

DELIVERABLE:
Write your forensic audit report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m2_final\audit_report.md`
and a concise handoff.md in your working directory.
When complete, notify parent via send_message with your verdict and report path.
