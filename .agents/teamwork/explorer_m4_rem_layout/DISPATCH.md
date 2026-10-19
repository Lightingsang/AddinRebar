# Task Dispatch: explorer_m4_rem_layout

## Identity
- **Agent Name**: explorer_m4_rem_layout
- **Role**: Sheet Arranger & Bin Packing Explorer
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_rem_layout\`
- **Parent Conversation ID**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

## Authoritative Context Documents (MANDATORY TO READ FIRST)
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Headers `## 2026-10-16T23:55:10Z` and `## 2026-10-17T06:46:41Z`)
2. Master Project Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`
3. Dead Ends Log: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\DEAD_ENDS.md`
4. Auditor M4 Full Evidence Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4\audit_report.md`
5. Layout Challenger Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_m4_2\challenge_report.md`

## Your Exploration Scope
Analyze the SheetArrangerCalculator defects causing the test suite failure in `DrawingLayoutStressChallengeTests.cs`:
1. **Vertical Bounds Clamping in `SheetArrangerCalculator.cs`**:
   - File: `HPKata/HPKata.Core/DrawingLayout/Calculators/SheetArrangerCalculator.cs`
   - In `TryPlaceOnShelf`, currently checks `cursorX + block.WidthMm > maxX`, but does NOT verify whether `cursorY + block.HeightMm > maxY`.
   - When a block is taller than the usable sheet printable height (e.g. `TooTall` block of 800 mm on an A2 sheet of 420 mm height), line 68 unconditionally places it on a new sheet protruding far outside the sheet boundary.
2. **Empty Phantom Sheet Elimination**:
   - In `ArrangeBlocks`, if an oversized block cannot fit on any sheet or shelf, the code spawns a new sheet and attempts placement, leaving behind empty sheets where `PlacedBlocks.Count == 0`.
   - The test assertion `OversizedBlocks_LargerThanPrintableArea_HandledWithoutInfiniteLoop`:
     ```csharp
     Assert.DoesNotContain("TooTall", placedBlockNames);
     ```
     fails because `TooTall` was placed on sheet 2 despite being taller than the sheet!
3. **Formulate Exact Mathematical & Algorithmic Fix**:
   - Determine how `SheetArrangerCalculator` should validate whether `block.WidthMm > printableWidth || block.HeightMm > printableHeight`.
   - Oversized blocks that physically cannot fit on the sheet must either be recorded in a collection of unplaced blocks or skipped, and never placed outside the printable boundary.
   - Any sheet with `PlacedBlocks.Count == 0` must never be added to `result.Sheets`.
4. **Clean Code & Boundary Constraints**:
   - Ensure proposed code keeps all files $\le 300$ physical lines (Rule C2) and methods $\le 50$ physical lines (Rule M3).
   - Zero Autodesk references in `HPKata.Core`.

## Deliverable
Write your detailed remediation specification report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_rem_layout\layout_remediation_plan.md`
and a concise `handoff.md` in your working directory.
When done, notify parent via `send_message`.
