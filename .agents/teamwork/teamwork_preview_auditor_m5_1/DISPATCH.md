# DISPATCH — teamwork_preview_auditor_m5_1

## Context
You are the Victory Forensic Auditor for Milestone M5 (Final Solution Build, Full Test Pass & Victory Audit).

## Paths to Context Files
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (entry dated 2026-10-17T14:27:21Z)
- Project Plan: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`

## Forensic Victory Audit Mission
Perform final, project-wide forensic integrity verification across the entire repository for HPKata:
1. **Zero Cheating / Authenticity Audit**:
   - Verify all implementations across M1, M2, M3, M4 are genuine, authentic production code.
   - Zero hardcoded test outputs, zero fake passes, zero mock bypasses.
2. **Package & Resource Purge Audit**:
   - Run grep for `MaterialDesignThemes`, `MaterialDesignColors`, and `materialdesigninxaml.net` across all `HPKata/` projects.
   - Assert exactly 0 matches.
3. **Rule R5 Quarantine Audit**:
   - Run grep for `304.8` across `HPKata/HPKata.Revit/`.
   - Assert literal `304.8` appears ONLY inside `Units/RevitUnitConverter.cs`.
4. **Clean Code Governance Audit**:
   - Check line counts of all C# files in `HPKata/` (Rule C2 <= 300 lines).
   - Check method counts of all methods (Rule M3 <= 50 lines).
   - Check for dead/commented code (Rule Q-B1) and empty catch blocks without comments (Rule Q-B2).
   - Check compiler warnings under `TreatWarningsAsErrors=true` (must be 0).
5. **Solution Build & Test Execution Audit**:
   - Execute:
     `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
     `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
   - Verify 100% pass rate across all 5 test projects (`HPKata.AutoCad.Tests`, `HPKata.Core.Tests`, `HPKata.E2E.Tests`, `HPKata.Revit.Tests`, `HPKata.UI.Tests`).

## Binary Verdict
Output a binary verdict in `handoff.md`:
- `CLEAN` (all integrity checks pass, ready for victory completion report to Sentinel)
- `INTEGRITY VIOLATION` (any fake code, mock bypass, or hardcoded violation)

Notify `orchestrator_19` when done.

## 2026-10-17T16:18:00Z
You are teamwork_preview_auditor_m5_1, Victory Forensic Auditor for Milestone M5.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_auditor_m5_1\
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_auditor_m5_1\DISPATCH.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-17T14:27:21Z).
Read the project plan at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.

Perform final project-wide Victory Forensic Audit:
1. Zero cheating / authenticity: genuine production code, zero hardcoded test outputs, zero fake passes.
2. Zero MaterialDesign residue: grep across all HPKata projects.
3. Rule R5 quarantine: literal 304.8 ONLY in Units/RevitUnitConverter.cs.
4. Clean Code limits: all C# files <= 300 lines (Rule C2), methods <= 50 lines (Rule M3), 0 warnings under TreatWarningsAsErrors, 0 dead code, 0 unannotated catches.
5. Independent build and full test execution:
   dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
   dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
Output binary verdict: CLEAN or INTEGRITY VIOLATION in handoff.md. Notify orchestrator_19.
