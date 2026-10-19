# DISPATCH: auditor_m4_1_iter2 (Forensic Integrity Re-Auditor M4)

**Assigned Agent**: auditor_m4_1_iter2 (`teamwork_preview_auditor`)  
**Parent**: orchestrator_16 (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4_1_iter2\`  
**Assigned Task**: Gate M4 Iteration 2 Forensic Integrity Re-Audit on HPKata.UI & HPKata.Core  
**Timestamp**: 2026-10-17T03:15:00Z  

---

## Mandatory Reading List (Read in Order)
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`
3. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
4. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\clean-code\HP_CLEAN_CODE_CORE.md`
5. Previous Audit Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4_1\handoff.md`
6. Worker Remediation Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_iter2\handoff.md`

---

## RESOURCE DIRECTIVE (MANDATORY)
- Worker has built the solution. If executing tests, you MUST use `dotnet test HPKata/HPKata.slnx --no-build --disable-build-servers -m:1`.
- Do NOT spawn background build servers or run parallel compilation storms.

---

## Forensic Audit Scope
1. **DEFECT-M4-01 Re-Verification (Rule M3)**:
   - Run AST method span analysis:
     `python scratch/verify_clean_code_ast.py "HPKata\HPKata.UI" 40`
   - Empirically verify that `SetupViewModel.RebarDetails.cs` has 0 methods exceeding 50 lines (and specifically that `LoadFromConfig` and `ToConfig` decompositions keep all method bodies <= 40 lines).
   - Verify that worker handoff claims match actual physical file line counts.
2. **Clean-Room DRM Isolation**:
   - Verify `C:\kata_pro` is 100% read-only with 0 modifications.
   - Verify `Security.dll` or licensing code from KataPro has NEVER been decompiled, referenced, copied, or circumvented.
3. **Authenticity & Clean Code Governance**:
   - Verify zero dummy facades, zero mocked test constants, and genuine MVVM interactions.
   - Verify Rule C2 (all files <= 300 lines), Rule Q-B1 (0 dead code), Rule Q-B2 (all catch blocks annotated), and Rules D1/D4 (0 Autodesk references).
4. **Test Suite Verification**:
   - Run `dotnet test HPKata/HPKata.slnx --no-build --disable-build-servers -m:1` (verify all 194 tests pass, 0 failures, 0 skipped, 0 warnings).

---

## Deliverables
- Write `audit_report.md` with full forensic evidence.
- Deliver `handoff.md` with explicit binary verdict: **`CLEAN`** or **`INTEGRITY VIOLATION`**.
- Send coordination message to orchestrator_16 (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`).
