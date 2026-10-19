# DISPATCH — 2026-10-17T09:27:00Z

## Task Assignment for auditor_m2 (M2 Forensic Integrity Auditor)

You are **auditor_m2**, the Forensic Integrity Auditor for Milestone M2 (Phase 3: Column & Shear Wall Shop Drawing).
Your working directory is: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m2\`
Your parent conversation ID is: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

### CRITICAL AUDIT MANDATE:
You are the Forensic Integrity Auditor. Your verdict is a BINARY VETO.
If you find ANY evidence of cheating, dummy/facade implementations, hardcoded mock strings, fabricated test outputs, or circumvention of requirements, you MUST report **INTEGRITY VIOLATION**.

### MANDATORY REFERENCES:
1. `ORIGINAL_REQUEST.md` at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`.
2. Master Project Scope at `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`.
3. Worker's Changes & Handoff:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_column_wall\changes.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_column_wall\handoff.md`

### YOUR AUDIT CHECKS:
1. Static Integrity Analysis:
   - Grep scan `HPKata/` for hardcoded simulation strings, dummy returns, `TODO`, `FIXME`, `HACK`.
   - Inspect all 12 new controllers in `HPKata.AutoCad/Controllers/`: verify genuine ModelSpace interaction, real table generation, real DWG XRecord serialization (`KATA_COLUMN_DATA`, `KATA_WALL_DATA`), and real Excel workbook generation.
   - Inspect `ColumnRebarGenerator.cs` and `WallRebarGenerator.cs`: verify genuine Revit curves, crank geometry, and closed hoop generation.
2. Clean Code Rules:
   - Rule C2: Measure every `.cs` file in `HPKata/` — must be <= 300 lines.
   - Rule M3: Measure methods in Phase 3 code — must be <= 50 lines.
   - Rule Q-B1: Zero commented-out code.
   - Rule Q-B2: Zero unannotated empty catch blocks.
   - Rule R5: Constant 304.8 must appear strictly and exclusively in `RevitUnitConverter.cs`.
   - Host Neutrality: `HPKata.Core` has ZERO references to `Autodesk.*`.
3. Runtime Execution Validation:
   - Run: `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`
   - Confirm 100% tests pass (all 634 tests) without failures or file-lock collisions.
4. Audit Verdict:
   State clearly either: **CLEAN** or **INTEGRITY VIOLATION**.

DELIVERABLE:
Write your forensic audit report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m2\audit_report.md`
and a concise handoff.md in your working directory.
When complete, notify parent via send_message with your verdict and report path.
