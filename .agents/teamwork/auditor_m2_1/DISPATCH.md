# DISPATCH: auditor_m2_1 (Forensic Integrity Auditor M2)

**Assigned Agent**: auditor_m2_1 (`teamwork_preview_auditor`)  
**Parent**: orchestrator_16 (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m2_1\`  
**Assigned Task**: Milestone M2 Forensic Integrity Audit of HPKata.Core  
**Timestamp**: 2026-10-17T01:24:00Z  

---

## Mandatory Reading List (Read in Order)
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (verbatim user request).
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`.
3. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
4. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2\handoff.md`.

---

## Detailed Forensic Audit Scope & Instructions

Perform a forensic integrity audit on `HPKata/HPKata.Core/`:
1. **DRM & Clean-Room Isolation Verification**:
   - Verify that `Security.dll`, hardware locks, or licensing routines from `C:\kata_pro` have NEVER been decompiled, referenced, copied, or bypassed.
   - Verify that `C:\kata_pro` remains completely unmodified (check git status and file timestamps).
2. **Authenticity & Genuine Implementation**:
   - Inspect all 27 C# files in `HPKata/HPKata.Core/`.
   - Verify there are ZERO hardcoded dummy outputs, ZERO test-facade patterns, ZERO empty stub methods returning mock constants.
   - Verify all 85 shape formulas, 86 configuration parameters, 5 cutting stock heuristics, XData serializers, and QRCoder generators contain authentic algorithmic logic.
3. **Repository Clean Code Governance**:
   - Check compliance with `docs/clean-code/HP_CLEAN_CODE_CORE.md`:
     - Rule C2: class $\le 300$ lines.
     - Rule M3: method $\le 50$ lines.
     - Rule Q-B1: zero dead / commented-out code.
     - Rule Q-B2: zero unannotated catch blocks.
     - Rule D1/D4: pure host-neutrality (`netstandard2.0;net8.0`, 0 references to `Autodesk.AutoCAD.*` or `Autodesk.Revit.*`).
4. **Compilation & Artifact Integrity**:
   - Verify `dotnet build HPKata/HPKata.slnx -c Release` produces genuine DLLs with 0 warnings, 0 errors.

---

## Output Requirements
1. Record full forensic evidence in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m2_1\audit_report.md`.
2. Deliver `handoff.md` with an explicit binary verdict: **CLEAN** or **INTEGRITY VIOLATION**.
   *(Note: As defined in repository rules, INTEGRITY VIOLATION is a non-negotiable binary veto).*
3. Send a message to orchestrator_16 (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`).

## 2026-10-17T01:24:37Z
You are auditor_m2_1, the Forensic Auditor for Milestone M2 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m2_1\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m2_1\DISPATCH.md.
3. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md.
4. Read worker handoff report at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2\handoff.md.

YOUR TASK:
Perform a strict forensic integrity audit on Milestone M2 (HPKata.Core):
1. Clean-room DRM isolation: Verify Security.dll, hardware locks, and licensing code from C:\kata_pro have NEVER been decompiled, referenced, copied, or bypassed. Verify C:\kata_pro is 100% read-only and untouched.
2. Authenticity verification: Inspect all 27 files in HPKata/HPKata.Core/. Verify zero hardcoded dummy outputs, zero test facades, zero empty stub methods returning mock constants.
3. Clean Code Governance: Check compliance with docs/clean-code/HP_CLEAN_CODE_CORE.md (Rule C2 <= 300 lines, Rule M3 <= 50 lines, Rule Q-B1 0 dead code, Rule Q-B2 0 unannotated catch, Rule D1/D4 0 Autodesk references).
4. Build verification: Verify `dotnet build HPKata/HPKata.slnx -c Release` compiles cleanly with 0 warnings and 0 errors.
5. Record full forensic evidence in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m2_1\audit_report.md.
6. Deliver handoff.md in your working directory with an explicit binary verdict: CLEAN or INTEGRITY VIOLATION.
7. Send a message to orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6) when complete.
