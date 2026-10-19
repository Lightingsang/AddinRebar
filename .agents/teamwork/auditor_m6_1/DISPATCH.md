# DISPATCH — Gate M6: Forensic Integrity Auditor M6

**Assigned Agent**: `auditor_m6_1` (`teamwork_preview_auditor`)  
**Parent Orchestrator**: `orchestrator_16` (Conversation ID: `4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m6_1\`  
**Target Projects**: `HPKata/HPKata.Revit/`, `HPKata/HPKata.Revit.Tests/`  
**Date**: 2026-10-17  

---

## 1. Context & Mission
Milestone M6 (`HPKata.Revit` and `HPKata.Revit.Tests`) has been implemented by `worker_m6`.
Your task is to conduct an independent, unsparing forensic integrity audit of Milestone M6:
1. Clean-Room DRM Isolation:
   - Verify `C:\kata_pro` is strictly read-only with 0 file modifications.
   - Scan entire repository for `Security.dll`, dongles, hardware locks, or licensing bypasses. Must be strictly 0.
2. Authenticity & Anti-Cheat Forensics:
   - Inspect all C# files in `HPKata.Revit/` and `HPKata.Revit.Tests/`.
   - Verify 0 dummy facades, 0 mocked test constants, 0 hardcoded test return checks.
   - Verify genuine parameter bridge, genuine shape mapping, genuine curve analysis, and genuine test assertions.
3. Unit Quarantine Forensics (Rule R5):
   - Scan `HPKata/` production source code for literal `304.8` or `1.0 / 304.8`.
   - Verify it appears ONLY in `HPKata/HPKata.Revit/Units/RevitUnitConverter.cs`. Zero tolerance for conversion factors outside quarantine.
4. Clean Code Governance Audit:
   - Run AST analysis: `python scratch/verify_clean_code_ast.py "HPKata\HPKata.Revit" 40`.
   - Rule C2: verify all files $\le 300$ lines.
   - Rule M3: verify all method bodies $\le 50$ lines (with warning check $\le 40$).
   - Rule Q-B1: verify 0 dead or commented-out code.
   - Rule Q-B2: verify 0 unannotated catch blocks.
5. Test Suite Execution & Output Forensics:
   - Run:
     ```powershell
     dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-audit6-$([guid]::NewGuid())"
     ```
   - Verify 287/287 tests pass (100% green, 0 failures, 0 skipped, 0 warnings).
6. Deliver full forensic evidence in `audit_report.md` and explicit binary verdict in `handoff.md`: `CLEAN` or `INTEGRITY VIOLATION`.

## 2026-10-17T05:04:39Z
You are auditor_m6_1, the Forensic Integrity Auditor for Milestone M6 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m6_1\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m6_1\DISPATCH.md.
3. Read the master specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md (Sections 1, 9, 10).
4. Read worker handoff report at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6\handoff.md.
5. Read docs/clean-code/HP_CLEAN_CODE_CORE.md.

RESOURCE DIRECTIVE (MANDATORY):
- Worker has already built and verified the solution in Release mode.
- If executing tests, you MUST include `--no-build --disable-build-servers -m:1`.
- MTP Concurrency Isolation: You MUST pass `--results-directory "$env:TEMP\mtp-audit6-$([guid]::NewGuid())"` to prevent log locking collisions!

YOUR TASK:
Perform a strict forensic integrity audit on Milestone M6 (HPKata.Revit and HPKata.Revit.Tests):
1. Clean-Room DRM Isolation:
   - Verify C:\kata_pro is strictly read-only with 0 file modifications.
   - Scan codebase for Security.dll, Security, hardware locks, dongles, or licensing checks. Must be strictly 0.
2. Authenticity & Anti-Cheat Forensics:
   - Inspect all C# files in HPKata.Revit and HPKata.Revit.Tests.
   - Verify 0 dummy facades, 0 mocked test constants, 0 hardcoded test return checks.
   - Verify genuine Revit geometry creation, genuine parameter bridge, genuine shape mapping, genuine ClosedXML export.
3. Unit Quarantine Forensics (Rule R5):
   - Scan HPKata production source for literal 304.8 or 1.0 / 304.8.
   - Verify it appears ONLY in HPKata.Revit/Units/RevitUnitConverter.cs.
4. Clean Code Governance Audit:
   - Run AST analysis: python scratch/verify_clean_code_ast.py "HPKata\HPKata.Revit" 40.
   - Rule C2: verify all files <= 300 lines.
   - Rule M3: verify all methods <= 50 lines.
   - Rule Q-B1: verify 0 dead or commented-out code.
   - Rule Q-B2: verify 0 unannotated catch blocks.
5. Test Suite Execution:
   - Run: dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-audit6-$([guid]::NewGuid())".
   - Verify 287/287 tests pass (100% green, 0 failures, 0 skipped, 0 warnings).
6. Record full forensic evidence in audit_report.md and deliver handoff.md with explicit binary verdict: CLEAN or INTEGRITY VIOLATION.

MANDATORY CONSTRAINTS:
- C:\kata_pro is strictly read-only.
- Never inspect, decompile, or touch Security.dll or licensing code.

Send a message to orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6) when complete.
