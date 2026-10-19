# DISPATCH: auditor_m4_1 (Forensic Integrity Auditor for Milestone M4)

**Assigned Agent**: auditor_m4_1 (`teamwork_preview_auditor`)  
**Parent**: orchestrator_16 (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4_1\`  
**Assigned Task**: Milestone M4 (HPKata.UI) Forensic Integrity Audit  
**Timestamp**: 2026-10-17T02:55:00Z  

---

## Mandatory Reading List (Read in Order)
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`.
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`.
3. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`.
4. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4\handoff.md`.

---

## RESOURCE DIRECTIVE (MANDATORY)
- Worker has already built the solution. If running any test commands, you MUST pass `--no-build --disable-build-servers -m:1`.
- DO NOT run concurrent full builds or spawn background build servers.
- Perform static code inspection and lightweight checks.

---

## Detailed Audit Instructions & Integrity Forensics
Perform a strict forensic integrity audit on Milestone M4 (`HPKata.UI`):
1. **Clean-Room DRM / Licensing Isolation (MANDATORY)**:
   - Verify that `Security.dll`, hardware lock dongles, or licensing routines from `C:\kata_pro` have NEVER been referenced, copied, decompiled, or touched.
   - Verify that `C:\kata_pro` remains 100% read-only with 0 modifications.
2. **Authenticity & Anti-Cheating Verification (MANDATORY)**:
   - Inspect all C# and XAML files in `HPKata/HPKata.UI/` and `HPKata/HPKata.UI.Tests/`.
   - Verify zero dummy facades, zero mocked test constants, and genuine MVVM bindings interacting with `HPKata.Core`.
3. **Clean Code Governance Audit**:
   - Rule C2: Verify all 22 `.cs` files are strictly $\le 300$ physical lines.
   - Rule M3: Verify all method bodies are strictly $\le 50$ physical lines.
   - Rule Q-B1: Zero dead or commented-out code.
   - Rule Q-B2: Zero unannotated catch blocks.
   - Rule D1/D4: Zero references to `Autodesk.*`.
4. **Execution Verification**:
   - Run `dotnet test HPKata/HPKata.slnx --no-build --disable-build-servers -m:1`.
   - Verify 190 tests pass (167 in Core.Tests, 23 in UI.Tests), 0 failures, 0 skipped, 0 warnings.
5. **Verdict**:
   - Binary verdict: **CLEAN** or **INTEGRITY VIOLATION**.

---

## Output Requirements
1. Record full forensic evidence in `audit_report.md`.
2. Deliver `handoff.md` with explicit binary verdict: **CLEAN** or **INTEGRITY VIOLATION**.
3. Send message to orchestrator_16 (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`).

## 2026-10-17T02:53:47Z
[Message] timestamp=2026-10-17T02:53:47Z sender=4300a9ce-e3eb-419c-8493-f2001d2d04d6 priority=MESSAGE_PRIORITY_HIGH
Content:
You are auditor_m4_1, the Forensic Auditor for Milestone M4 (HPKata.UI) of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4_1\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4_1\DISPATCH.md.
3. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md and PROJECT.md.
4. Read worker handoff report at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4\handoff.md.

RESOURCE DIRECTIVE (MANDATORY):
- Worker has built the solution. If executing tests, you MUST use `dotnet test HPKata/HPKata.slnx --no-build --disable-build-servers -m:1`.
- Do NOT spawn full builds or background build servers.

YOUR TASK:
Perform a strict forensic integrity audit on Milestone M4 (HPKata.UI):
1. Clean-room DRM isolation: Verify Security.dll or licensing code from C:\kata_pro have NEVER been referenced, decompiled, or touched. C:\kata_pro remains 100% read-only with 0 modifications.
2. Authenticity verification: Inspect all 22 .cs files in HPKata.UI and HPKata.UI.Tests. Verify 0 dummy facades, 0 mocked constants, genuine MVVM interactions.
3. Clean Code Governance: Check compliance with Rule C2 (<= 300 lines), Rule M3 (<= 50 lines), Rule Q-B1 (0 dead code), Rule Q-B2 (0 unannotated catch), and Rule D1/D4 (0 Autodesk references).
4. Run `dotnet test HPKata/HPKata.slnx --no-build --disable-build-servers -m:1` (verify 190 tests pass, 0 failures, 0 skipped, 0 warnings).
5. Record full forensic evidence in audit_report.md and deliver handoff.md with explicit binary verdict: CLEAN or INTEGRITY VIOLATION.
6. Send message to orchestrator_16 when complete.
