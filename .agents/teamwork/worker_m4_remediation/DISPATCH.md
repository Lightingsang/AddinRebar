# Task Dispatch: worker_m4_remediation

## Identity
- **Agent Name**: worker_m4_remediation
- **Role**: Milestone M4 Remediation Worker
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_remediation\`
- **Parent Conversation ID**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

## MANDATORY INTEGRITY WARNING
> DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Authoritative Context Documents (MANDATORY TO READ FIRST)
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Headers `## 2026-10-16T23:55:10Z` and `## 2026-10-17T06:46:41Z`)
2. Master Project Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`
3. Dead Ends Log: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\DEAD_ENDS.md`
4. Auditor M4 Full Evidence Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4\audit_report.md`
5. The 3 Authoritative Remediation Plans:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_rem_cad\cad_remediation_plan.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_rem_qs\qs_remediation_plan.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_rem_layout\layout_remediation_plan.md`

## Your Implementation Mission

Execute the remediation specifications from the 3 explorer plans across `HPKata/HPKata.Core`, `HPKata/HPKata.AutoCad`, and test suites:

### 1. CAD Commands & Aliases Remediation (`cad_remediation_plan.md`):
1. **Continuous Dimensioning (`dnoi` / `dim_noi_tiep`)**:
   - In `HPKata/HPKata.AutoCad/Controllers/DimensionUtilityController.cs`, implement genuine `ContinueDimensionInteractive()`:
     - Prompt user to pick base dimension (`AlignedDimension` or `RotatedDimension`).
     - Loop prompting continuation points, appending new chained dimensions in ModelSpace with active transaction.
     - Handle ESC/Enter cancellation gracefully.
   - In `HPKata/HPKata.AutoCad/Commands/DimensionUtilityCommands.cs`: wire `dnoi` and `dim_noi_tiep` to `DimensionUtilityController.ContinueDimensionInteractive()`. Eliminate the facade call to witness trimming.
2. **Missing Command Registration (`ve_grid`)**:
   - In `HPKata/HPKata.AutoCad/Commands/LayoutCommands.cs`: add `[CommandMethod("ve_grid")]` to `DrawGrid()` to satisfy `GoiLenh_HPKata.lsp:43`.

### 2. QS Controllers & Core Engine Remediation (`qs_remediation_plan.md`):
1. **Eliminate Simulation Stubs in `QsTakeoffController.cs`**:
   - In `ScanFramingElements`: Replace hardcoded `[C1, D1, S1]` with genuine acquisition querying ModelSpace entities / user selection (closed polylines, blocks, text tags on layers like `COT`, `DAM`, `SAN`, `MONG`, `VACH`), extracting real extents and calculating intersections.
   - In `FindElement` (`timqs`): Replace fake success print with genuine ModelSpace scan of `DBText`, `MText`, and `BlockReference` attributes matching `targetMark`. Highlight and zoom using `CadHighlightService`.
2. **Negative Dimension Validation in `QsTakeoffCalculator.cs` (Defect B-QS-01)**:
   - Add `SanitizeDimensions` with `Math.Max(0.0, ...)` clamping across volume and formwork calculations so negative dimensions never produce negative volume or false positive products.
3. **Clean Code Rule M3 Refactoring in `QsFormworkCalculator.cs` (Defect CC-QS-01)**:
   - Decompose 62-line `EvaluateContactPair` into helper methods (`DeductColumnContact`, `DeductBeamContact`, `DeductFootingContact`) so all methods are strictly $\le 50$ physical lines.

### 3. Drawing Layout Remediation (`layout_remediation_plan.md`):
1. **Vertical Bounds Clamping in `SheetArrangerCalculator.cs`**:
   - In `TryPlaceOnShelf`, add `cursorY + block.HeightMm > maxY` validation.
2. **Oversized Block Handling**:
   - Pre-flight filter blocks exceeding printable sheet dimensions (`WidthMm > PrintableWidthMm || HeightMm > PrintableHeightMm`), preventing them from protruding or spawning empty sheets.
3. **Empty Phantom Sheet Elimination**:
   - Ensure sheets where `PlacedBlocks.Count == 0` are pruned, and normalize contiguous `SheetIndex`.
4. **Clean Code Rule M3 Compliance**:
   - Decompose `ArrangeBlocks` into modular helper methods, each strictly $\le 50$ physical lines.
5. **Test Suite Verification**:
   - Ensure `DrawingLayoutStressChallengeTests.OversizedBlocks_LargerThanPrintableArea_HandledWithoutInfiniteLoop` passes cleanly.

### 4. Clean Code Rules Mandate (MANDATORY):
- Max 300 physical lines per file (Rule C2).
- Max 50 physical lines per method (Rule M3).
- 0 dead code / TODOs / NotImplementedException (Rule Q-B1).
- 0 unannotated empty catch blocks (Rule Q-B2).
- Constant 304.8 strictly and exclusively in `RevitUnitConverter.cs` (Rule R5).
- Zero `Autodesk.*` references in `HPKata.Core`.

### 5. Build & Test Mandate:
- Build: `dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1` (0 warnings, 0 errors).
- Test: `dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"` (all tests pass 100% green).

## Deliverable
Write your implementation report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_remediation\changes.md`
and a structured `handoff.md` in your working directory.
When complete, notify parent via `send_message` with build/test results and report path.
