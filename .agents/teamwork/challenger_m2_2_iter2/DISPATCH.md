# DISPATCH: challenger_m2_2_iter2 (Cutting Stock & Config Re-Challenger)

**Assigned Agent**: challenger_m2_2_iter2 (`teamwork_preview_challenger`)  
**Parent**: orchestrator_16 (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m2_2_iter2\`  
**Assigned Task**: Milestone M2 Iteration 2 Empirical Re-Challenge of 1D-CSP Cutting Stock & Config Parser  
**Timestamp**: 2026-10-17T01:48:00Z  

---

## Mandatory Reading List (Read in Order)
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`.
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m2_2\handoff.md` (previous defect report DEFECT-M2-01).
3. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m2_iter2\handoff.md` (remediation report).
4. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`.

---

## Detailed Re-Challenge Scope & Instructions

Empirically verify the remediation of `DEFECT-M2-01`:
1. **Re-Test Cutting Stock Optimization Harness**:
   - Execute the empirical harness:
     ```powershell
     powershell -ExecutionPolicy Bypass -File "HPKata/tools/run_m2_empirical_tests.ps1"
     ```
   - Verify that **Scenario 4 (DuBang0 Time Budget on 150 items of 2000 mm)** now PASSES with execution time $\le 1000$ ms (target: $\le 50$ ms).
   - Test an additional extreme stress test on 200 items of 2000 mm and 300 items of varying lengths without exact sum to verify `DuBang0` strictly respects the 1,000 ms budget under all conditions.
   - Verify Scenarios 1, 2, 3, and 5 continue to pass with 100% green metrics.
2. **Re-Test Config Parser (Scenarios C1–C6)**:
   - Verify all 86 parameters parse against real `C:\kata_pro\Goi lenh.lsp` with 0 mismatches, round-trip idempotence, missing file handling, and Windows-1258 fallback.
3. **Verdict Criteria**:
   - If all 11+ tests pass and Scenario 4 is $\le 1000$ ms, vote **APPROVE**.
   - If any test fails or execution exceeds 1000 ms, vote **REQUEST_CHANGES**.

---

## Output Requirements
1. Record empirical findings in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m2_2_iter2\challenge_report.md`.
2. Deliver `handoff.md` with explicit verdict: **APPROVE** or **REQUEST_CHANGES**.
3. Send a message to orchestrator_16 (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`).
