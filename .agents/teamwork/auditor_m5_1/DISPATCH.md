# Dispatch: Forensic Auditor M5 (Milestone M5 Integrity Audit)

**Agent**: `auditor_m5_1` (`teamwork_preview_auditor`)
**Parent**: `orchestrator_16` (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`)
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m5_1\`
**Date**: 2026-10-17

## Mandatory Reading
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` (Sections 1, 2, 3, 10)
3. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5\handoff.md`
4. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
5. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\clean-code\HP_CLEAN_CODE_CORE.md`

## Resource Directive (MANDATORY)
- Worker has already built and verified the solution.
- If executing tests, you MUST include `--no-build --disable-build-servers -m:1`.
- **MTP Concurrency Isolation**: You MUST pass `--results-directory "$env:TEMP\mtp-audit5-$([guid]::NewGuid())"` to prevent Microsoft.Testing.Platform log locking collisions!

## Mission
Perform a strict forensic integrity audit on Milestone M5 (`HPKata.AutoCad` and `HPKata.AutoCad.Tests`):
1. **Clean-Room DRM Isolation**:
   - Verify `C:\kata_pro` is strictly read-only with 0 file modifications.
   - Scan codebase for `Security.dll`, `Security`, hardware locks, dongles, or licensing checks. Must be strictly 0.
2. **Authenticity Verification**:
   - Inspect all C# files in `HPKata.AutoCad` and `HPKata.AutoCad.Tests`.
   - Verify 0 dummy facades, 0 mocked test constants, 0 hardcoded test return checks.
   - Verify genuine AutoCAD geometry, genuine XData conversions, genuine Lisp parsing.
3. **Clean Code Governance Audit**:
   - Run AST analysis: `python scratch/verify_clean_code_ast.py "HPKata\HPKata.AutoCad" 40`.
   - Rule C2: verify all files $\le 300$ lines.
   - Rule M3: verify all methods $\le 50$ lines.
   - Rule Q-B1: verify 0 dead or commented-out code.
   - Rule Q-B2: verify 0 unannotated catch blocks.
4. **Test Suite Execution**:
   - Run: `dotnet test HPKata/HPKata.slnx --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-audit5-$([guid]::NewGuid())"`.
   - Verify 225/225 tests pass (100% green, 0 failures, 0 skipped, 0 warnings).
5. Record full forensic evidence in `audit_report.md` and deliver handoff report with explicit binary verdict: **CLEAN** or **INTEGRITY VIOLATION**.

## Mandatory Constraints
- `C:\kata_pro` is strictly read-only.
- Never inspect, decompile, or touch `Security.dll` or licensing code.


## 2026-10-17T04:04:23Z
You are auditor_m5_1, the Forensic Auditor for Milestone M5 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m5_1\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m5_1\DISPATCH.md.
3. Read the master specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md (Sections 1, 2, 3, 10).
4. Read worker handoff report at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m5\handoff.md.
5. Read docs/clean-code/HP_CLEAN_CODE_CORE.md.

RESOURCE DIRECTIVE (MANDATORY):
- Worker has already built and verified the solution.
- If executing tests, you MUST include `--no-build --disable-build-servers -m:1`.
- MTP Concurrency Isolation: You MUST pass `--results-directory "$env:TEMP\mtp-audit5-$([guid]::NewGuid())"` to prevent log locking collisions!

YOUR TASK:
Perform a strict forensic integrity audit on Milestone M5 (HPKata.AutoCad and HPKata.AutoCad.Tests):
1. Clean-Room DRM Isolation:
   - Verify C:\kata_pro is strictly read-only with 0 file modifications.
   - Scan codebase for Security.dll, Security, hardware locks, dongles, or licensing checks. Must be strictly 0.
2. Authenticity Verification:
   - Inspect all C# files in HPKata.AutoCad and HPKata.AutoCad.Tests.
   - Verify 0 dummy facades, 0 mocked test constants, 0 hardcoded test return checks.
   - Verify genuine AutoCAD geometry, genuine XData conversions, genuine Lisp parsing.
3. Clean Code Governance Audit:
   - Run AST analysis: python scratch/verify_clean_code_ast.py "HPKata\HPKata.AutoCad" 40.
   - Rule C2: verify all files <= 300 lines.
   - Rule M3: verify all methods <= 50 lines.
   - Rule Q-B1: verify 0 dead or commented-out code.
   - Rule Q-B2: verify 0 unannotated catch blocks.
4. Test Suite Execution:
   - Run: dotnet test HPKata/HPKata.slnx --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-audit5-$([guid]::NewGuid())".
   - Verify 225/225 tests pass (100% green, 0 failures, 0 skipped, 0 warnings).
5. Record full forensic evidence in audit_report.md and deliver handoff.md with explicit binary verdict: CLEAN or INTEGRITY VIOLATION.

MANDATORY CONSTRAINTS:
- C:\kata_pro is strictly read-only.
- Never inspect, decompile, or touch Security.dll or licensing code.

Send a message to orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6) when complete.
