# Task Assignment: Forensic Auditor (Integrity Forensics & Clean Code Audit)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_auditor_1\`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (UTC header `## 2026-10-18T00:11:55Z`)
- Project Specification: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_22\PROJECT.md`
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\HPDcePro.slnx`
- Clean Code Standards: `AGENTS.md`, `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/CODE_REVIEW_CHECKLIST.md`

## Objectives
Conduct an exhaustive, independent Forensic Integrity Audit:

1. **Non-Tampering Verification of `C:\Dce Pro` (CRITICAL MANDATE)**:
   - Verify that directory `C:\Dce Pro\` has NOT been modified, created in, or deleted from.
   - Run git/file system checks to confirm 0 changes in `C:\Dce Pro\`.
   - Verify that all code in `HPDcePro/` treats `C:\Dce Pro` as strictly read-only or uses clean in-memory fallbacks when paths are missing in CI.

2. **Anti-Cheating & Integrity Verification (BINARY VETO)**:
   - Inspect all implementation files in `HPDcePro/src/` and tests in `HPDcePro/tests/`.
   - Verify that there are NO hardcoded expected test results, NO dummy/facade mocks returning fake data, NO shortcut mocks that bypass the real calculation engines.
   - Verify that `HPDcePro.Mcp` genuinely calls `HPDcePro.Core` and `HPDcePro.Excel` services.
   - Verify that `SteelFrameDraftingGenerator` genuinely computes 2D coordinates, geometry, and layers into `CadDraftingBuffer`.
   - Verify that `CalculationUdfBridge` genuinely invokes the TCVN calculation engines.
   - Verify that `MainControlCenterViewModel` genuinely binds to project models and commands.

3. **Clean Code & Line Limit Verification**:
   - Verify every C# file in `HPDcePro/`:
     - File length <= 300 lines (strictly enforced).
     - Method length <= 50 lines.
     - 0 commented-out code blocks.
     - 0 unannotated catch blocks (every catch block must have a `// Reason: ...` comment).
   - Verify `TreatWarningsAsErrors=true`:
     - `dotnet build HPDcePro/HPDcePro.slnx -c Debug` -> 0 errors, 0 warnings.
     - `dotnet test HPDcePro/HPDcePro.slnx -c Debug` -> 100% pass (>= 330 tests, 0 failed, 0 skipped).

4. **Verdict**:
   - Provide binary verdict: **CLEAN** or **INTEGRITY VIOLATION**.
   - If ANY integrity violation, cheating, or `C:\Dce Pro` tampering is detected, report **INTEGRITY VIOLATION** with full forensic evidence.
   - Write full audit report to `handoff.md` and notify orchestrator via send_message.


## 2026-10-18T01:18:06Z
You are the Forensic Auditor for HPDcePro Wave 3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_auditor_1\
Read your task assignment at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_auditor_1\DISPATCH.md
MANDATORY: Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-18T00:11:55Z).
Read PROJECT.md at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_22\PROJECT.md
Read Clean Code rules at: docs/clean-code/HP_CLEAN_CODE_CORE.md and AGENTS.md.

Conduct a strict, independent Forensic Integrity Audit:
1. NON-TAMPERING: Verify that C:\Dce Pro remains 100% untouched (0 files created, modified, or deleted).
2. ANTI-CHEATING: Verify NO hardcoded test results, NO dummy/facade implementations, NO fake shortcuts. Verify that all 332 tests execute authentic business logic.
3. CLEAN CODE COMPLIANCE:
   - Check every C# file in HPDcePro/: length <= 300 lines, methods <= 50 lines.
   - Check for 0 commented-out code.
   - Check that EVERY catch block contains a `// Reason: ...` comment explaining why the exception is handled.
4. BUILD & TEST VERIFICATION:
   - Verify `dotnet build HPDcePro/HPDcePro.slnx -c Debug` -> 0 errors, 0 warnings.
   - Verify `dotnet test HPDcePro/HPDcePro.slnx -c Debug` -> >= 330 tests pass (0 failed, 0 skipped).

Deliver a BINARY VETO verdict: CLEAN or INTEGRITY VIOLATION in handoff.md.
Notify orchestrator via send_message.
