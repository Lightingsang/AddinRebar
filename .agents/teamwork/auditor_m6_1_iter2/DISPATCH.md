# FORENSIC RE-AUDITOR DISPATCH SPECIFICATION — Gate M6 Iteration 11 (auditor_m6_1_iter2)

## Mission
Perform a strict forensic integrity re-audit on Milestone M6 (`HPKata.Revit` and `HPKata.Revit.Tests`) to verify whether DEFECT-M6-01 (Rule M3 breach), false attestation, and test mock leakage have been genuinely remediated.

## Authoritative Inputs
- **Original User Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (read first!)
- **Previous Audit Report**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m6_1\audit_report.md`
- **Remediation Worker Handoff**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6_iter2\handoff.md`
- **Clean Code Standards**: `docs/clean-code/HP_CLEAN_CODE_CORE.md` and `docs/clean-code/REVITADDINAI_CLEAN_CODE_STANDARD.md`
- **AST Verification Tool**: `scratch/verify_clean_code_ast.py`

## Mandatory Resource Directive
- The worker has already compiled the solution in Release mode.
- If executing tests, you MUST use `--no-build --disable-build-servers -m:1`.
- MTP Concurrency Isolation: You MUST pass `--results-directory "$env:TEMP\mtp-audit6iter2-$([guid]::NewGuid())"` to prevent test results locking collisions!

## Audit Tasks
1. **Clean Code Governance Re-Audit**:
   - Run: `python scratch/verify_clean_code_ast.py "HPKata\HPKata.Revit" 40`.
   - Verify exit code is 0 and reports 0 strict violations (> 50 lines).
   - Inspect `HPKata/HPKata.Revit/Bridge/RevitParameterBridge.cs`:
     - Verify `ExtractFromRevitRebar()` method body is $\le 30$ physical lines (strictly $\le 50$ lines).
     - Verify `ResolveShapeAndParameters()` is $\le 50$ lines.
     - Verify file length $\le 300$ lines.
2. **Mock Relocation & Production Assembly Cleanliness**:
   - Verify that `HPKata/HPKata.Revit/Mocks/` no longer exists in production assembly.
   - Verify that `MockRevitDocumentContext.cs` is located strictly in `HPKata/HPKata.Revit.Tests/Mocks/`.
   - Verify that production assembly `HPKata.Revit` has 0 mock doubles.
3. **Clean-Room DRM Isolation**:
   - Verify `C:\kata_pro` is strictly read-only with 0 file modifications.
   - Verify 0 occurrences of `Security.dll`, dongles, or licensing routines.
4. **Unit Quarantine (Rule R5)**:
   - Verify literal `304.8` appears in production source ONLY in `RevitUnitConverter.cs`.
5. **Test Suite Verification**:
   - Run: `dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-audit6iter2-$([guid]::NewGuid())"`.
   - Verify all 287 tests pass 100% green (0 failures, 0 skipped, 0 warnings).
6. **Deliverable**:
   - Deliver `audit_report.md` and `handoff.md` in `.agents/teamwork/auditor_m6_1_iter2/` with an explicit binary verdict: **CLEAN** or **INTEGRITY VIOLATION**.


## 2026-10-17T05:23:51Z
You are auditor_m6_1_iter2, the Forensic Integrity Re-Auditor for Gate M6 Iteration 11 of HPKata.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m6_1_iter2\
Your parent is orchestrator_16 (Conversation ID: 4300a9ce-e3eb-419c-8493-f2001d2d04d6).

MANDATORY FIRST STEPS:
1. Read g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md.
2. Read your dispatch specification at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m6_1_iter2\DISPATCH.md.
3. Read your previous audit report at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m6_1\audit_report.md.
4. Read the remediation worker handoff at g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m6_iter2\handoff.md.
5. Read docs/clean-code/HP_CLEAN_CODE_CORE.md.

RESOURCE DIRECTIVE (MANDATORY):
- Worker has already built the solution in Release mode.
- If executing tests, you MUST use `--no-build --disable-build-servers -m:1`.
- MTP Concurrency Isolation: You MUST pass `--results-directory "$env:TEMP\mtp-audit6iter2-$([guid]::NewGuid())"` to prevent log locking collisions!

YOUR TASK:
Perform a strict forensic integrity re-audit on Gate M6 Iteration 11:
1. Clean Code Governance Re-Audit:
   - Run AST analysis: `python scratch/verify_clean_code_ast.py "HPKata\HPKata.Revit" 40`.
   - Verify exit code is 0 and reports 0 strict violations (> 50 lines).
   - Inspect `HPKata/HPKata.Revit/Bridge/RevitParameterBridge.cs`:
     - Verify `ExtractFromRevitRebar()` body length is <= 30 physical lines (strictly <= 50 lines).
     - Verify `ResolveShapeAndParameters()` is <= 50 lines.
     - Verify total file length <= 300 lines.
2. Mock Relocation & Production Cleanliness:
   - Verify that `HPKata/HPKata.Revit/Mocks/` does not exist.
   - Verify that `MockRevitDocumentContext.cs` is located strictly in `HPKata/HPKata.Revit.Tests/Mocks/`.
   - Verify that production assembly `HPKata.Revit` contains 25 source files with 0 test mock doubles.
3. Clean-Room DRM Isolation:
   - Verify `C:\kata_pro` is strictly read-only with 0 file modifications.
   - Scan codebase for `Security.dll`, dongles, or licensing checks. Must be strictly 0.
4. Unit Quarantine (Rule R5):
   - Scan for literal `304.8` in production code. Must appear ONLY in `RevitUnitConverter.cs`.
5. Test Suite Verification:
   - Run: `dotnet test HPKata/HPKata.slnx -c Release --no-build --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-audit6iter2-$([guid]::NewGuid())"`.
   - Verify all 287 tests pass 100% green (0 failures, 0 skipped, 0 warnings).
6. Deliver `audit_report.md` and `handoff.md` with explicit binary verdict: **CLEAN** or **INTEGRITY VIOLATION**.
7. Send message to orchestrator_16 when complete.
