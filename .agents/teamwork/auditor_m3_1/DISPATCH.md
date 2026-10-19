# DISPATCH: auditor_m3_1 (Forensic Integrity Auditor for Milestone M3)

**Assigned Agent**: auditor_m3_1 (`teamwork_preview_auditor`)  
**Parent**: orchestrator_16 (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m3_1\`  
**Assigned Task**: Milestone M3 (HPKata.Core.Tests) Forensic Integrity Audit  
**Timestamp**: 2026-10-17T02:25:00Z  

---

## Mandatory Reading List (Read in Order)
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`.
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`.
3. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
4. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3\handoff.md`.

---

## Detailed Audit Instructions & Integrity Forensics

Perform a strict forensic integrity audit on Milestone M3 (`HPKata.Core.Tests`):
1. **Clean-Room DRM / Licensing Isolation (MANDATORY)**:
   - Verify that `Security.dll`, hardware lock dongles, or licensing routines from `C:\kata_pro` have NEVER been referenced, copied, decompiled, or touched.
   - Verify that `C:\kata_pro` remains 100% read-only with 0 modifications and 0 new files.
2. **Authenticity & Anti-Cheating Verification (MANDATORY)**:
   - Inspect all 12 test files in `HPKata/HPKata.Core.Tests/`.
   - Verify that tests do NOT use dummy facades, mocked constant returns, or cheating assertions (e.g. asserting `true == true`, or asserting mock strings).
   - Verify tests actually execute logic in `HPKata.Core`.
3. **Clean Code Governance Audit**:
   - Rule C2: Verify all 12 test files are strictly $\le 300$ physical lines.
   - Rule M3: Verify all test method bodies are strictly $\le 50$ physical lines.
   - Rule Q-B1: Zero dead or commented-out code.
   - Rule Q-B2: Zero unannotated catch blocks.
   - Rule D1/D4: Zero references to `Autodesk.*`.
4. **Build & Test Verification**:
   - Run `dotnet test HPKata/HPKata.Core.Tests/HPKata.Core.Tests.csproj`.
   - Run `dotnet test HPKata/HPKata.slnx`.
   - Verify 0 failures, 0 skipped, 0 warnings.
5. **Verdict**:
   - Binary verdict: **CLEAN** or **INTEGRITY VIOLATION**.

---

## Output Requirements
1. Record full forensic evidence in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m3_1\audit_report.md`.
2. Deliver `handoff.md` with explicit binary verdict: **CLEAN** or **INTEGRITY VIOLATION**.
3. Send message to orchestrator_16 (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`).


## 2026-10-17T02:14:45Z
Received dispatch from orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6):
Perform a strict forensic integrity audit on Milestone M3 (HPKata.Core.Tests):
1. Clean-room DRM isolation: Verify Security.dll, hardware locks, or licensing code from C:\kata_pro have NEVER been referenced, decompiled, copied, or bypassed. Verify C:\kata_pro is 100% read-only with 0 modifications.
2. Authenticity verification: Inspect all 12 test files in HPKata/HPKata.Core.Tests/. Verify zero dummy facades, zero mocked test constants, and genuine assertions executing HPKata.Core logic.
3. Clean Code Governance: Check compliance with docs/clean-code/HP_CLEAN_CODE_CORE.md (Rule C2 <= 300 lines for all test files, Rule M3 <= 50 lines per method, Rule Q-B1 0 dead code, Rule Q-B2 0 unannotated catch, Rule D1/D4 0 Autodesk references).
4. Run `dotnet test HPKata/HPKata.Core.Tests/HPKata.Core.Tests.csproj` and `dotnet test HPKata/HPKata.slnx`. Verify 0 failures, 0 skipped, 0 warnings.
5. Record full forensic evidence in audit_report.md and deliver handoff.md with explicit binary verdict: CLEAN or INTEGRITY VIOLATION.
6. Send message to orchestrator_16 when complete.
