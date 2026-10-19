# DISPATCH — teamwork_preview_auditor_m3_1

## Context
You are Forensic Auditor for Milestone M3 (Revit Element Geometry Reader & Safe Picker Fallback).

## Paths to Context Files
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (entry dated 2026-10-17T14:27:21Z)
- Project Plan: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
- Worker M3 Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_1\handoff.md`

## Forensic Integrity Audit Mission
Perform a rigorous forensic integrity check on Milestone M3 deliverables:
1. **Authenticity Audit**:
   - Inspect `HPKata/HPKata.Revit/Services/RevitElementGeometryReader.cs`. Is it a genuine implementation with real parameter extraction and BoundingBox logic, or a dummy facade?
   - Verify that all calculations perform real math and conversion.
2. **Rule R5 Quarantine Audit**:
   - Run grep for `304.8` across the entire `HPKata.Revit` codebase.
   - Assert that the literal `304.8` appears ONLY in `Units/RevitUnitConverter.cs`. If found in `RevitElementGeometryReader.cs` or anywhere else, issue an immediate INTEGRITY VIOLATION.
3. **Clean Code & Governance Audit**:
   - Line limit: Verify all `.cs` files are $\le 300$ lines (Rule C2).
   - Method limit: Verify all methods are $\le 50$ lines (Rule M3).
   - Rule Q-B1: Zero commented-out or dead code.
   - Rule Q-B2: Zero empty catch blocks without explanatory comments.
   - TreatWarningsAsErrors: Zero compiler warnings.
4. **Build and Test Verification**:
```powershell
dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
```

## Binary Verdict
Output a binary verdict in `handoff.md`:
- `CLEAN` (all integrity checks pass)
- `INTEGRITY VIOLATION` (any fake code, mock bypass, or hardcoded violation)

Notify `orchestrator_19` when done.

## 2026-10-17T15:52:05Z
You are teamwork_preview_auditor_m3_1, Forensic Auditor for Milestone M3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_auditor_m3_1\
Read your dispatch file at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_auditor_m3_1\DISPATCH.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-17T14:27:21Z).
Read the project plan at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md.
Read Worker M3's handoff report at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_1\handoff.md.

Perform comprehensive forensic integrity audit on Milestone M3 deliverables:
1. Authenticity: verify authentic parameter extraction and BoundingBox logic (zero mocks, zero facades).
2. Rule R5 quarantine: verify literal 304.8 exists ONLY in Units/RevitUnitConverter.cs and nowhere else in HPKata.Revit.
3. Clean Code limits: all files <= 300 lines (Rule C2), methods <= 50 lines (Rule M3), 0 dead code, 0 unannotated catches.
4. Verify build and tests:
   dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
   dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
Output binary verdict: CLEAN or INTEGRITY VIOLATION in handoff.md. Notify orchestrator_19.
