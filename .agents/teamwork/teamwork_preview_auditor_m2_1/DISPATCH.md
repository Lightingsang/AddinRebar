# DISPATCH — Forensic Auditor (Milestone M2)

## Mission
Perform comprehensive forensic integrity audit on Milestone M2 deliverables:
- Check for authentic vs fake implementations across all 13 view pairs (26 files).
- Verify zero hardcoded test outputs or mock bypasses.
- Verify zero dead code (Rule Q-B1) and zero unannotated empty catch blocks (Rule Q-B2).
- Verify clean code limits: <= 300 lines per file (Rule C2), <= 50 lines per method (Rule M3).
- Verify git diff and modified files strictly match task requirements.
- Output a binary verdict: **CLEAN** or **INTEGRITY VIOLATION**.

## Inputs
- Authoritative user request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (entry dated `2026-10-17T14:27:21Z`).
- Project scope document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
- Worker report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_1\handoff.md`.
- Working directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_auditor_m2_1\`

## Tasks
1. Audit modified files:
   All 13 view pairs in `HPKata/HPKata.UI/Views/`.
2. Run build and test:
   `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
   `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
3. Check for any cheating, dummy implementations, or fake assertions.
4. Output verdict: **CLEAN** or **INTEGRITY VIOLATION** with full evidence in `handoff.md`. Notify orchestrator_19 when complete.


## 2026-10-17T15:21:23Z
You are teamwork_preview_auditor_m2_1.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_auditor_m2_1\
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_auditor_m2_1\DISPATCH.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-17T14:27:21Z).
Read the project plan at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Read Worker M2's handoff report at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_1\handoff.md.

Perform comprehensive forensic integrity audit on Milestone M2 deliverables:
- Check for authentic vs fake implementations across all 13 view pairs (26 files).
- Verify zero hardcoded test outputs or mock bypasses.
- Verify zero dead code (Rule Q-B1) and zero unannotated empty catch blocks (Rule Q-B2).
- Verify clean code limits: <= 300 lines per file (Rule C2), <= 50 lines per method (Rule M3).
- Verify git diff and modified files strictly match task requirements.
- Run build and test:
  dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
  dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
Output a binary verdict: CLEAN or INTEGRITY VIOLATION in handoff.md. Notify orchestrator_19.
