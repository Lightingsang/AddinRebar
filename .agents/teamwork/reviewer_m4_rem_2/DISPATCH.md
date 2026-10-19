# Task Dispatch: reviewer_m4_rem_2

## Identity
- **Agent Name**: reviewer_m4_rem_2
- **Role**: M4 Remediation Functional Reviewer
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_rem_2\`
- **Parent Conversation ID**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

## Authoritative Context Documents (MANDATORY TO READ FIRST)
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Headers `## 2026-10-16T23:55:10Z` and `## 2026-10-17T06:46:41Z`)
2. Master Project Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`
3. Worker Changes and Handoff:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_remediation\changes.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_remediation\handoff.md`

## Review Scope & Instructions
Review Milestone M4 Iteration 2 Remediation focusing on Functional Completeness & Build/Test Verification:
1. **Functional Verification of Remediation**:
   - Verify `dnoi` / `dim_noi_tiep` in `DimensionUtilityCommands.cs` now delegates to genuine `DimensionUtilityController.ContinueDimensionInteractive()`, with zero delegation to witness trimming.
   - Verify `LayoutCommands.cs` contains `[CommandMethod("ve_grid")]` on `DrawGrid()`, and that `(defun c:vg()(command "ve_grid"))` in `GoiLenh_HPKata.lsp:43` works without error.
   - Verify `QsTakeoffController.cs` has zero hardcoded simulation items `[C1, D1, S1]` and performs genuine ModelSpace/user entity acquisition. Verify `timqs` genuinely scans and highlights entities.
   - Verify `SheetArrangerCalculator.cs` vertical bounds clamping, oversized block rejection, and phantom sheet pruning.
   - Verify negative dimension clamping in `QsTakeoffCalculator.cs`.
2. **Build & Test Verification**:
   - Run: `dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1` (0 warnings, 0 errors).
   - Run: `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"` (all 889 tests pass 100% green).
3. **Verdict**:
   Provide an explicit verdict: APPROVE or REQUEST_CHANGES.

## Deliverable
Write your review report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_rem_2\review.md`
and a concise `handoff.md` in your working directory.
When done, notify parent via `send_message`.
