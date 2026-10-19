# Task Dispatch: reviewer_m4_rem_1

## Identity
- **Agent Name**: reviewer_m4_rem_1
- **Role**: M4 Remediation Code Architecture Reviewer
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_rem_1\`
- **Parent Conversation ID**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

## Authoritative Context Documents (MANDATORY TO READ FIRST)
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Headers `## 2026-10-16T23:55:10Z` and `## 2026-10-17T06:46:41Z`)
2. Master Project Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`
3. Worker Changes and Handoff:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_remediation\changes.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_remediation\handoff.md`

## Review Scope & Instructions
Review the Milestone M4 Iteration 2 Remediation focusing on Code Architecture & Clean Code Governance:
1. **Clean Code Rule C2 (File Length <= 300 lines)**:
   - Measure every file touched or added during remediation:
     - `DimensionUtilityCommands.cs`, `LayoutCommands.cs`, `DimensionUtilityController.cs`, `QsTakeoffController.cs`
     - `QsTakeoffCalculator.cs`, `QsFormworkCalculator.cs`, `SheetArrangerCalculator.cs`
     - All test files touched.
   - Confirm every file is strictly $\le 300$ physical lines.
2. **Clean Code Rule M3 (Method Length <= 50 lines)**:
   - Verify every method in touched code (especially `DimensionUtilityController`, `QsTakeoffController`, `EvaluateContactPair` in `QsFormworkCalculator`, `ArrangeBlocks` in `SheetArrangerCalculator`) is strictly $\le 50$ physical lines.
3. **Clean Code Rules Q-B1, Q-B2, R5**:
   - Zero commented-out dead code, zero TODOs/FIXMEs/HACKs/NotImplementedException.
   - Zero unannotated empty catch blocks.
   - Constant 304.8 strictly and exclusively in `RevitUnitConverter.cs`.
4. **Host Neutrality**:
   - Verify `HPKata.Core` has ZERO references to `Autodesk.AutoCAD.*` or `Autodesk.Revit.*`.
5. **Verdict**:
   Provide an explicit verdict: APPROVE or REQUEST_CHANGES.

## Deliverable
Write your review report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_rem_1\review.md`
and a concise `handoff.md` in your working directory.
When done, notify parent via `send_message`.

## 2026-10-17T13:04:09Z
You are reviewer_m4_rem_1, reviewing Milestone M4 (Phase 5: Quantity Surveying (QS) & Drawing Utilities) Iteration 2 Remediation.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_rem_1\
Your parent conversation ID is: a7a2dbbb-6b7d-4279-98cd-1216d8280d80
