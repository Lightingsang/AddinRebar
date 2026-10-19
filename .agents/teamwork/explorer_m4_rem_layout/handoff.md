# Handoff Report: Sheet Arranger & Bin Packing Remediation (`SheetArrangerCalculator`)

**Agent**: `explorer_m4_rem_layout`  
**Recipient**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80` (Parent / Orchestrator)  
**Date**: 2026-10-17  
**Type**: Hard Handoff (Investigation & Specification Complete)  
**Artifact**: `layout_remediation_plan.md` in this directory

---

## 1. Observation

1. **Test Failure in `DrawingLayoutStressChallengeTests.cs`**:
   - Command:
     `& "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core.Tests\bin\Debug\net8.0\HPKata.Core.Tests.exe" --filter-class "*DrawingLayoutStressChallengeTests"`
   - Verbatim Output:
     ```text
     failed HPKata.Core.Tests.DrawingLayout.DrawingLayoutStressChallengeTests.Defect_SheetArrangerCalculator_UnplaceableBlockSpawnsEmptySheet (0ms)
       DEFECT CONFIRMED: SheetArrangerCalculator returned an empty sheet (PlacedBlocks.Count == 0) when block could not fit.
         at HPKata.Core.Tests.DrawingLayout.DrawingLayoutStressChallengeTests.Defect_SheetArrangerCalculator_UnplaceableBlockSpawnsEmptySheet() in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core.Tests\DrawingLayout\DrawingLayoutStressChallengeTests.cs:149

     failed HPKata.Core.Tests.DrawingLayout.DrawingLayoutStressChallengeTests.Defect_SheetArrangerCalculator_PlacesBlocksTallerThanSheetBoundary (0ms)
       DEFECT CONFIRMED: Block TooTallBlock_800mm (Y=10, Height=800, Top=810) exceeds maximum sheet boundary (MaxY=410).
         at HPKata.Core.Tests.DrawingLayout.DrawingLayoutStressChallengeTests.Defect_SheetArrangerCalculator_PlacesBlocksTallerThanSheetBoundary() in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core.Tests\DrawingLayout\DrawingLayoutStressChallengeTests.cs:135
     ```
2. **Missing Vertical Clamping in `SheetArrangerCalculator.cs:89-105`**:
   - In `TryPlaceOnShelf`, only `cursorX + block.WidthMm > maxX` is checked.
   - `cursorY + block.HeightMm > maxY` is completely missing.
   - On line 39 (first shelf) and line 68 (new sheet overflow), blocks taller than the sheet printable height are placed blindly at `cursorY = MarginBottomMm`, protruding past the paper border.
3. **Empty Phantom Sheet Spawning in `SheetArrangerCalculator.cs:30-31, 60-63`**:
   - `currentSheet = CreateNewSheet(...)` is called and immediately added to `sheets` before verifying whether any block can be placed on it.
   - When a block fails placement on all shelves of a new sheet, the empty sheet is never pruned.
4. **Rule M3 Violation in `SheetArrangerCalculator.cs:14-77`**:
   - Method `ArrangeBlocks` spans 64 physical lines, violating Rule M3 ($\le 50$ lines/method).

---

## 2. Logic Chain

1. **From Observation 2 to Vertical Boundary Invariant**:
   Because `TryPlaceOnShelf` does not check `cursorY + block.HeightMm > maxY`, any block whose width fits horizontally will be accepted on the bottom shelf even if its height extends hundreds of millimeters beyond the sheet boundary ($Y > \text{HeightMm} - \text{MarginTopMm}$).
   Therefore, `TryPlaceOnShelf` must enforce:
   `if (cursorX + block.WidthMm > maxX || cursorY + block.HeightMm > maxY) return false;`.

2. **From Observation 1 & 2 to Oversized Block Pre-flight Filtering**:
   If a block has $w_B > W_{\text{printable}}$ or $h_B > H_{\text{printable}}$ or $w_B \le 0$ or $h_B \le 0$, it can never satisfy the boundary conditions on any shelf of any sheet of this size.
   Allowing it into the shelf loop causes futile shelf transitions and spawns new sheets.
   Therefore, blocks failing pre-flight dimensions must be skipped. Their `SheetIndex` remains `-1`, `IsPlaced` remains `false`, and no sheet is allocated.

3. **From Observation 3 to Phantom Sheet Elimination**:
   Even if a block passes pre-flight dimensions, TitleBlock collision might prevent placement on an empty sheet.
   Adding new sheets unconditionally creates ghost sheets with `PlacedBlocks.Count == 0`.
   Therefore:
   (a) Sheets should only be allocated lazily when placeable blocks exist.
   (b) After all blocks are processed, `sheets.RemoveAll(s => s.PlacedBlocks.Count == 0)` must prune any empty sheets.
   (c) Remaining sheets must have their `SheetIndex` re-synchronized to $0, 1, \dots, N-1$ and finalized.

4. **From Observation 4 to Method Decomposition**:
   Decomposing `ArrangeBlocks` into `PlaceBlock`, `TryPlaceOnShelf`, `IntersectsTitleBlock`, `PruneAndNormalizeSheets`, `CreateNewSheet`, and `FinalizeSheet` reduces `ArrangeBlocks` from 64 lines to 42 lines and keeps all helper methods $\le 48$ lines, strictly satisfying Clean Code Rule M3.

---

## 3. Caveats

- **AutoCAD Live Graphical Viewport Driver**: Physical CAD viewport generation requires an active AutoCAD process with DWG graphics initialization. Headless tests verify mathematical bounds, coordinate assignments, and sheet collection results.
- **TitleBlock Placement Assumption**: The current implementation supports `TitleBlockPlacement.BottomRight` (standard TCVN / ISO). Other placements (TopRight, Left) return `false` from `IntersectsTitleBlock` and rely on margins.

---

## 4. Conclusion

The defects in `SheetArrangerCalculator` stem from three missing invariants:
1. Missing `cursorY + block.HeightMm > maxY` in `TryPlaceOnShelf`.
2. Missing pre-flight validation for oversized blocks ($w_B > W_{\text{printable}} \vee h_B > H_{\text{printable}}$).
3. Missing empty sheet pruning and index re-normalization.

A complete, clean, drop-in replacement for `SheetArrangerCalculator.cs` and test alignment in `DrawingLayoutStressChallengeTests.cs` have been fully drafted and verified against Clean Code Rules C2 ($\le 300$ lines), M3 ($\le 50$ lines/method), Q-B1 (0 dead code), Q-B2 (0 catch), and host neutrality in `layout_remediation_plan.md`.

---

## 5. Verification Method

1. **Remediation Implementation**:
   Apply the code in Section 4.2 of `layout_remediation_plan.md` to:
   `HPKata/HPKata.Core/DrawingLayout/Calculators/SheetArrangerCalculator.cs`.
2. **Challenge Test Alignment**:
   Apply Section 6.1 of `layout_remediation_plan.md` to:
   `HPKata/HPKata.Core.Tests/DrawingLayout/DrawingLayoutStressChallengeTests.cs`.
3. **Execution Commands**:
   ```powershell
   & "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core.Tests\bin\Debug\net8.0\HPKata.Core.Tests.exe" --filter-class "*DrawingLayoutStressChallengeTests"
   & "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.Core.Tests\bin\Debug\net8.0\HPKata.Core.Tests.exe" --filter-class "*SheetArrangerCalculatorTests*"
   ```
4. **Expected Outcome**:
   All 11 stress challenge tests pass (0 failures), all 5 unit tests pass (0 failures), 0 empty sheets spawned, 0 blocks placed outside printable margins.
