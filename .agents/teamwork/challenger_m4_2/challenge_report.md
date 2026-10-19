# Empirical Challenge Report: Milestone M4 (Drawing Layout, Grids & CAD Utilities)

**Agent**: `challenger_m4_2`  
**Parent Agent**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`  
**Date**: 2026-10-17  
**Scope**: Drawing Layout, Grids & CAD Utilities domain calculators (`GridAxisGenerator.cs`, `SheetArrangerCalculator.cs`, `DrawingIndexAggregator.cs`, `SlopeCalculator.cs`, `ElevationFormatter.cs`, `VectorProjectionHelper.cs`)  
**Verdict**: **DEFECT_FOUND**

---

## Challenge Summary

**Overall risk assessment**: **HIGH**

While 3 out of 4 domain modules (`GridAxisGenerator`, `DrawingIndexAggregator`, and CAD utilities `SlopeCalculator`/`ElevationFormatter`/`VectorProjectionHelper`) proved robust and mathematically sound under intense stress-testing (505/507 tests passing), empirical stress testing of `SheetArrangerCalculator.cs` exposed **two critical geometric defects**:
1. **Missing Vertical Boundary Enforcement**: Detail blocks taller than usable sheet height (`block.HeightMm > maxY - MarginBottomMm`) are blindly placed on the sheet, protruding far past the paper border into unprintable negative space.
2. **Phantom Empty Sheet Generation**: When an unplaceable or oversized block fails placement on an overflow sheet, the empty sheet is not discarded, producing phantom empty drawings in the output pagination.

---

## Challenges

### [High] Challenge 1: `SheetArrangerCalculator` Violates Physical Sheet Boundary on Taller Blocks

- **Assumption Challenged**: The worker assumed `SheetArrangerCalculator.TryPlaceOnShelf` ensures placed blocks stay within the printable sheet boundary.
- **Attack Scenario**:
  A drawing block with `HeightMm = 800mm` is submitted for arrangement on an A2 landscape sheet (`594mm x 420mm`, margins: top 10mm, bottom 10mm, left 10mm, right 10mm; maximum usable height $Y = 410\text{ mm}$).
- **Actual Behavior & Trace**:
  In `HPKata.Core/DrawingLayout/Calculators/SheetArrangerCalculator.cs`:
  ```csharp
  private static bool TryPlaceOnShelf(
      DrawingBlockItem block, ArrangedSheetResult sheet, SheetSizeDefinition def, TitleBlockModel tb,
      ref double cursorX, double cursorY, ref double shelfHeight, double gutterX)
  {
      double maxX = def.WidthMm - def.MarginRightMm;
      if (cursorX + block.WidthMm > maxX) return false;

      if (IntersectsTitleBlock(cursorX, cursorY, block.WidthMm, block.HeightMm, def, tb))
          return false;

      block.X = cursorX;
      block.Y = cursorY;
      block.SheetIndex = sheet.SheetIndex;
      sheet.PlacedBlocks.Add(block);
      ...
      return true;
  }
  ```
  `TryPlaceOnShelf` only checks horizontal clearance: `cursorX + block.WidthMm > maxX`. It **completely omits vertical bounds validation** (`cursorY + block.HeightMm > def.HeightMm - def.MarginTopMm`).
  On the very first block of the first shelf (`cursorY = MarginBottomMm = 10`), `TryPlaceOnShelf` is invoked directly at line 39. Because horizontal width fits (`100mm <= 584mm`), it returns `true`. The block is placed at `Y = 10mm`, and extends up to $Y = 10 + 800 = 810\text{ mm}$, protruding **400 mm outside the physical sheet**.
- **Blast Radius**:
  In AutoCAD (`sbv` / `sap_xep_ban_ve`) and Revit (`RevitSheetArrangementService`), drawing viewports or blocks extend outside sheet frames and plot title borders, ruining drawing production layouts and causing corrupted PDF batch plots.
- **Mitigation**:
  In `TryPlaceOnShelf`, add vertical boundary validation:
  ```csharp
  double maxY = def.HeightMm - def.MarginTopMm;
  if (cursorY + block.HeightMm > maxY) return false;
  ```

---

### [Medium] Challenge 2: `SheetArrangerCalculator` Spawns Empty Phantom Sheets on Unplaceable Blocks

- **Assumption Challenged**: Multi-sheet overflow pagination only produces sheets containing placed drawing details.
- **Attack Scenario**:
  A drawing block whose dimensions exceed printable width (e.g. `WidthMm = 1000mm` on an A2 sheet of printable width $574\text{ mm}$) is passed to `ArrangeBlocks`.
- **Actual Behavior & Trace**:
  In `SheetArrangerCalculator.cs` lines 59–72:
  ```csharp
  if (!placed)
  {
      // Current sheet full, create next sheet
      FinalizeSheet(currentSheet);
      currentSheet = CreateNewSheet(sheets.Count, sheetDef, tb);
      sheets.Add(currentSheet);

      cursorX = sheetDef.MarginLeftMm;
      cursorY = sheetDef.MarginBottomMm;
      currentShelfHeight = block.HeightMm;

      TryPlaceOnShelf(
          block, currentSheet, sheetDef, tb,
          ref cursorX, cursorY, ref currentShelfHeight, gutterX);
  }
  ```
  When `TryPlaceOnShelf` fails on the new sheet, the new sheet has already been added to `sheets` (`sheets.Add(currentSheet)` at line 62).
  Line 75 then calls `FinalizeSheet(currentSheet)`.
  As a result, `sheets` contains an empty sheet (`PlacedBlocks.Count == 0`) with `UtilizationRatio = 0.0`.
- **Blast Radius**:
  In Revit and AutoCAD, running sheet arrangement creates blank, empty drawing sheets in the sheet index and project browser whenever an oversized detail cannot fit.
- **Mitigation**:
  Before attempting pagination or shelf placement, filter out or flag blocks exceeding `PrintableWidthMm` or `PrintableHeightMm`. Furthermore, if `currentSheet.PlacedBlocks.Count == 0` after attempting placement on a new sheet, discard the empty sheet or omit it from the final result.

---

## Stress Test Results

| Domain | Stress Test Scenario | Expected Behavior | Actual Behavior | Result |
|---|---|---|---|---|
| **GridAxisGenerator** | ISO 128 character skipping over 500 indices (`GetIsoLabel(0..500)`) | Strict omission of ambiguous characters `I`, `O`, `Q`, `Z` across all cycles (A..Y, A2..Y2, etc.) | 0 occurrences of I, O, Q, Z across all 500 generated labels | **PASS** |
| **GridAxisGenerator** | Radial grid ray calculation across 4 quadrants ($0^\circ, 30^\circ, 45^\circ, 90^\circ, 180^\circ, 270^\circ, 315^\circ$) | Ray start/end coordinates match exact trigonometric oracle ($x_0 + R\cos\theta, y_0 + R\sin\theta$) | Endpoints match oracle with tolerance $< 10^{-3}\text{ mm}$ | **PASS** |
| **GridAxisGenerator** | Orthogonal grids with 30 spans along Y and scale = 200 | Extension lengths, bubble circles, and dimension offsets scale proportionally ($15\times, 4.5\times, 10\times, 8\times \text{scale}$) | All offsets scaled accurately, multi-cycle labels A2..D2 generated cleanly | **PASS** |
| **SheetArrangerCalculator** | 50 randomized drawing blocks packed on A1 sheet with TitleBlock at bottom-right | Zero intersection between any placed block and TitleBlock rectangle | All 50 blocks checked: zero TitleBlock bounding box overlaps | **PASS** |
| **SheetArrangerCalculator** | Detail block taller than sheet height (`HeightMm = 800mm` on A2 height $420\text{ mm}$) | Block rejected or excluded from placement; must not exceed sheet boundary | **FAIL**: Block `TooTallBlock_800mm` placed at $Y=10$, top at $Y=810 > 410\text{ mm}$ | **DEFECT** |
| **SheetArrangerCalculator** | Unplaceable oversized block (`WidthMm = 1000mm` on A2 width $594\text{ mm}$) | Unplaceable block rejected without generating empty ghost sheet | **FAIL**: Empty sheet created and returned in `results` with `PlacedBlocks.Count == 0` | **DEFECT** |
| **SheetArrangerCalculator** | Multi-sheet pagination with 80 high-density blocks on A3 sheet | Paginates across multiple sheets, `SheetIndex` synchronized, utilization $\in [0, 100\%]$ | Paginated cleanly into multiple sheets with valid indices and utilization | **PASS** |
| **DrawingIndexAggregator** | Natural alphanumeric sorting (`KC-01`, `KC-02`, `KC-09`, `KC-10`, `KC-11`, `KC-100`) | Natural sorting order preserved: `KC-10` does NOT sort before `KC-02` | `KC-02` < `KC-09` < `KC-10` < `KC-11` < `KC-100` strictly verified | **PASS** |
| **DrawingIndexAggregator** | Duplicate sheet numbers (`KC-05`, `kc-05`) | Generates warning: `"Trùng số hiệu bản vẽ: ..."` | Case-insensitive duplicate detected and warning recorded | **PASS** |
| **DrawingIndexAggregator** | Sequence gap detection (`KC-01`, `KC-04`, `KC-08`) vs continuous sequence | Warnings for gaps (1 to 4, 4 to 8); zero warnings for continuous sequence 1..4 | Detected exact missing ranges; 0 false warnings on continuous sequences | **PASS** |
| **SlopeCalculator** | Text rotation angle across entire $360^\circ$ circle ($0^\circ$ to $359^\circ$ in $1^\circ$ steps) | Text angle normalized to $[-90^\circ, 90^\circ]$ (in radians $[0, \pi/2] \cup (3\pi/2, 2\pi)$) to prevent upside-down reading | 360/360 angles verified; zero angles in inverted range $(\pi/2, 3\pi/2]$ | **PASS** |
| **SlopeCalculator** | Percentage and ratio calculation for horizontal, 2%, 100%, vertical, and coincident points | 2% -> "1:50", "i = 2%"; vertical -> "Đứng", ratio "1:0"; 0% -> "Bằng" | Exact matches across all slope regimes; coincident points handled safely | **PASS** |
| **ElevationFormatter** | Vietnamese engineering formatting (TCVN 3986): $+3.600$, $\pm 0.000$, $-1.200$, `%%p0.000` | Sign prefix explicit, zero threshold $|Z| < 0.0005$ formatted as $\pm 0.000$ | Exact string formatting matched; boundary values at $0.00049$ and $0.00051$ respected | **PASS** |
| **ElevationFormatter** | Round-trip serialization/deserialization across 100 random elevations in $[-50, +250]$ | `Parse(Format(val)) == val` within 0.001m precision | 100/100 random samples parsed accurately | **PASS** |
| **VectorProjectionHelper** | Dimension witness line trimming for horizontal, vertical, and angled geometry | Witness lines trimmed by specified offset from dimension line along normal | Coordinates calculated accurately; perpendicular distance preserved | **PASS** |

---

## Unchallenged Areas

- **AutoCAD PaperSpace Viewport Creation (`taomv`, `khoav`)**: Real `Roamer`/`acad.exe` graphics viewport hardware drivers cannot be loaded in a headless CI/CLI environment without an active CAD session. Abstracted controllers tested via mock/headless runners.
- **Revit `ViewSheet.Create` live transaction**: Live Revit 2026 process was not attached in this pure .NET testing round; headless runner `RevitQsLayoutRunner` was verified.
