# DISPATCH — teamwork_preview_auditor_m4_1

## Context
You are Forensic Auditor for Milestone M4 (Comprehensive STA Unit Tests & StaticResource Parity).

## Paths to Context Files
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (entry dated 2026-10-17T14:27:21Z)
- Project Plan: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
- Worker M4 Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_1\handoff.md`

## Forensic Integrity Audit Mission
Perform a rigorous forensic integrity check on Milestone M4 deliverables:
1. **Authenticity Audit**:
   - Inspect `HPKata/HPKata.UI.Tests/Views/AllWindowsXamlInstantiationTests.cs` and `StaticResourceParityScannerTests.cs`.
   - Verify that tests actually instantiate the real WPF `MetroWindow` views and do not use dummy mock windows or facade types.
   - Verify that the static resource scanner actually parses and resolves real XAML tokens on disk.
2. **Clean Code & Governance Audit**:
   - Line limit: Verify all `.cs` files are $\le 300$ lines (Rule C2).
   - Method limit: Verify all methods are $\le 50$ lines (Rule M3).
   - Rule Q-B1: Zero commented-out or dead code.
   - Rule Q-B2: Zero empty catch blocks without explanatory comments.
   - TreatWarningsAsErrors: Zero compiler warnings.
3. **Build and Test Verification**:
```powershell
dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
```

## Binary Verdict
Output a binary verdict in `handoff.md`:
- `CLEAN` (all integrity checks pass)
- `INTEGRITY VIOLATION` (any fake code, mock bypass, or hardcoded violation)

Notify `orchestrator_19` when done.

## 2026-10-17T16:06:53Z
You are teamwork_preview_auditor_m4_1, Forensic Auditor for Milestone M4.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_auditor_m4_1\
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_auditor_m4_1\DISPATCH.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-17T14:27:21Z).
Read the project plan at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Read Worker M4's handoff report at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_1\handoff.md.

Perform comprehensive forensic integrity audit on Milestone M4 deliverables:
- Authenticity: Verify authentic instantiation of real MetroWindow views and real XAML token scanning (zero fake tests, zero mock bypasses).
- Clean Code: <= 300 lines per file (Rule C2), <= 50 lines per method (Rule M3), 0 dead code, 0 unannotated catches.
- Verify build and tests:
  dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
  dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
Output binary verdict: CLEAN or INTEGRITY VIOLATION in handoff.md. Notify orchestrator_19.
