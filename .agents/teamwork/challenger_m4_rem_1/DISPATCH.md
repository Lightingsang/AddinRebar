# Task Dispatch: challenger_m4_rem_1

## Identity
- **Agent Name**: challenger_m4_rem_1
- **Role**: QS Domain & Mathematical Stress Challenger
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_rem_1\`
- **Parent Conversation ID**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

## Authoritative Context Documents (MANDATORY TO READ FIRST)
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Headers `## 2026-10-16T23:55:10Z` and `## 2026-10-17T06:46:41Z`)
2. Master Project Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`
3. Worker Changes and Handoff:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_remediation\changes.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_remediation\handoff.md`

## Your Empirical Challenge Mission
Stress-test the Quantity Surveying remediation fixes:
1. **Adversarial Dimension Testing (`QsTakeoffCalculator.cs`)**:
   - Negative dimensions: $W = -500$, $H = -600$, $L = -3000$, $T = -120$.
   - Confirm net and gross concrete volume are strictly $0.0$ and formwork is $0.0$.
   - Mixed signs: $W = -500, H = 600, L = 3000$. Confirm volume is $0.0$ (never positive).
   - Zero dimensions: $W = 0$, $L = 5000$. Confirm volume is $0.0$.
   - Extreme dimensions: $W = 10^6\text{ mm}$, $H = 10^6\text{ mm}$, $L = 10^7\text{ mm}$. Confirm no overflow crashes.
2. **Formwork Contact Deduction Testing (`QsFormworkCalculator.cs`)**:
   - Verify refactored helpers (`DeductColumnContact`, `DeductBeamContact`, `DeductFootingContact`).
   - Confirm contact deductions never exceed total perimeter or surface area.
3. **Execution Mandate**:
   If running test commands, always use: `--disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`.
4. **Verdict**:
   Provide an empirical verification verdict: CONFIRMED_CORRECT or DEFECT_FOUND.

## Deliverable
Write your report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_rem_1\challenge_report.md`
and a concise `handoff.md` in your working directory.
When done, notify parent via `send_message`.


## 2026-10-17T13:04:09Z
You are challenger_m4_rem_1, performing adversarial stress-testing and empirical verification on Milestone M4 (Iteration 2 Remediation), Quantity Surveying domain.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_rem_1\
Your parent conversation ID is: a7a2dbbb-6b7d-4279-98cd-1216d8280d80
