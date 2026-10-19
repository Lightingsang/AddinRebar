# FINAL VICTORY FORENSIC AUDITOR DISPATCH — Gate M7 (auditor_m7_1)

## Mission
Perform the comprehensive, unsparing Final Victory Forensic Integrity Audit across the entire **HPKata** solution (`HPKata.Core`, `HPKata.UI`, `HPKata.AutoCad`, `HPKata.Revit`, `HPKata.E2E.Tests`).

## Authoritative Inputs
- **Original User Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (read first!)
- **Master Blueprint**: `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`
- **Root Project Definition**: `PROJECT.md`
- **Clean Code Standards**: `docs/clean-code/HP_CLEAN_CODE_CORE.md`
- **AST Verification Tool**: `scratch/verify_clean_code_ast.py`

## Mandatory Resource Directive
- The solution has already been built in Release mode.
- If executing tests, you MUST use `--no-build --disable-build-servers -m:1`.
- MTP Concurrency Isolation: You MUST pass `--results-directory "$env:TEMP\mtp-audit71-$([guid]::NewGuid())"` to prevent test results locking collisions!

## Forensic Verification Checks (Mandatory All 5 Checks)
1. **Check 1 — Clean-Room DRM Isolation**:
   - Inspect `C:\kata_pro`: verify 0 writes, 0 modifications (last write remains 13/10/2026).
   - Scan all source files in `HPKata/` for `Security.dll`, `Security`, hardware locks, HASP, dongles, Sentinel, or licensing bypasses. Must be strictly 0.
2. **Check 2 — Authenticity & Anti-Cheat Forensics**:
   - Verify 0 dummy facades, 0 mocked test constants, 0 hardcoded test return checks.
   - Verify genuine AutoCAD geometry creation, genuine XData serialization, genuine Revit geometry placement with `BarTerminationsData`, genuine ClosedXML export, genuine 1D-CSP solver heuristics.
3. **Check 3 — Unit Quarantine Forensics (Rule R5)**:
   - Search repository for literal `304.8` or `1.0 / 304.8`: verify it appears in production source ONLY in `HPKata/HPKata.Revit/Units/RevitUnitConverter.cs`.
4. **Check 4 — Clean Code Governance Audit**:
   - Run AST tool on all projects:
     - `python scratch/verify_clean_code_ast.py "HPKata\HPKata.Core" 40`
     - `python scratch/verify_clean_code_ast.py "HPKata\HPKata.UI" 40`
     - `python scratch/verify_clean_code_ast.py "HPKata\HPKata.AutoCad" 40`
     - `python scratch/verify_clean_code_ast.py "HPKata\HPKata.Revit" 40`
   - Verify 0 Rule C2 violations (all classes $\le 300$ physical lines).
   - Verify 0 Rule M3 violations (all method bodies $\le 50$ physical lines).
   - Verify Rule Q-B1 (0 dead code) and Rule Q-B2 (0 unannotated catch).
5. **Check 5 — Full Solution Test Suite Execution**:
   - Run: `dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-audit71-$([guid]::NewGuid())"`.
   - Verify all 437 tests pass 100% green across all 5 test projects (`HPKata.Core.Tests`: 168, `HPKata.AutoCad.Tests`: 39, `HPKata.Revit.Tests`: 54, `HPKata.UI.Tests`: 26, `HPKata.E2E.Tests`: 150).

## Deliverable
Deliver `audit_report.md` and `handoff.md` with explicit binary verdict: **CLEAN** or **INTEGRITY VIOLATION**.


## 2026-10-17T06:04:34Z
You are auditor_m7_1, the Final Victory Forensic Integrity Auditor for Gate M7 and the entire HPKata system.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m7_1\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m7_1\DISPATCH.md.
3. Read master blueprint docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md and root PROJECT.md.
4. Read docs/clean-code/HP_CLEAN_CODE_CORE.md.

RESOURCE DIRECTIVE (MANDATORY):
- Solution is already built in Release mode.
- If executing tests, you MUST use `--no-build --disable-build-servers -m:1`.
- MTP Concurrency Isolation: You MUST pass `--results-directory "$env:TEMP\mtp-audit71-$([guid]::NewGuid())"` to prevent test results locking collisions!

YOUR TASK:
Perform the comprehensive Final Victory Forensic Integrity Audit across the ENTIRE repository:
1. Clean-Room DRM Isolation:
   - Verify C:\kata_pro is strictly read-only (0 files modified or created during HPKata project).
   - Scan ALL files in HPKata/ for Security.dll, Security, hardware locks, HASP, dongles, Sentinel, or licensing checks. Must be strictly 0.
2. Authenticity & Anti-Cheat Forensics:
   - Inspect production and test code: verify 0 dummy facades, 0 mocked test constants, 0 hardcoded test return checks.
3. Unit Quarantine Forensics (Rule R5):
   - Scan for literal 304.8: verify it appears in production source ONLY in HPKata.Revit/Units/RevitUnitConverter.cs.
4. Clean Code Governance Audit:
   - Run AST tool across all projects:
     - python scratch/verify_clean_code_ast.py "HPKata\HPKata.Core" 40
     - python scratch/verify_clean_code_ast.py "HPKata\HPKata.UI" 40
     - python scratch/verify_clean_code_ast.py "HPKata\HPKata.AutoCad" 40
     - python scratch/verify_clean_code_ast.py "HPKata\HPKata.Revit" 40
   - Verify 0 Rule C2 violations (all files <= 300 lines) and 0 Rule M3 violations (all methods <= 50 lines).
   - Verify Rule Q-B1 (0 dead code) and Rule Q-B2 (0 unannotated catch).
5. Full Solution Test Suite:
   - Run: `dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-audit71-$([guid]::NewGuid())"`.
   - Verify all 437 tests pass 100% green across all 5 test projects.
6. Deliver `audit_report.md` and `handoff.md` with explicit binary verdict: **CLEAN** or **INTEGRITY VIOLATION**.
7. Send message to orchestrator_16 when complete.
