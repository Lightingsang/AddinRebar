# Dispatch: worker_m4_qs_layout_utils

## Role & Mission
You are `worker_m4_qs_layout_utils`, the Implementation Worker for Milestone M4 (Phase 5: Quantity Surveying (QS) & Drawing Utilities).
Your working directory is: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_qs_layout_utils\`
Your parent conversation ID is: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

## Authoritative Reference Reports
Read these three master specification reports:
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_qs\qs_spec_report.md`
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_layout\layout_spec_report.md`
3. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_cad_utils\cad_utils_spec_report.md`

## Implementation Tasks

### 1. `HPKata.Core` (Zero Autodesk Dependencies)
- `HPKata/HPKata.Core/QuantitySurveying/`:
  - `Models/`: `QsMemberType.cs`, `QsElementModel.cs`, `QsVolumeResult.cs`, `QsFormworkResult.cs`, `QsDeductionRecord.cs`.
  - `Calculators/`:
    * `QsTakeoffCalculator.cs`: Gross volume and surface area calculations for Footings, Columns, Beams, Slabs, Walls, Stairs.
    * `QsIntersectionDeductionEngine.cs`: Priority deduction matrix: Footing > Column/Wall > Primary Beam > Secondary Beam > Slab. Volume overlap deduction logic.
    * `QsFormworkCalculator.cs`: Contact area deduction: beam ends deducted from column side formwork ($b \times h$), slab thickness deducted from column/beam perimeter.
  - `Persistence/`:
    * `QsScheduleAggregator.cs`: Aggregates takeoffs by member type, story, and mark.
    * `QsWorkbookGenerator.cs`: Headless ClosedXML export to `.xlsx` matching `Kata.xlsm` sheet `Kata_QS` (columns A–J) and summary sheet `Thong_ke_QS`.
- `HPKata/HPKata.Core/DrawingLayout/`:
  - `Models/`: `GridLineModel.cs`, `GridSystemModel.cs`, `GridBubbleOrientation.cs`, `SheetSizeDefinition.cs` (A0–A4), `TitleBlockModel.cs`, `DrawingBlockItem.cs`, `ArrangedSheetResult.cs`, `DrawingIndexItem.cs`, `DrawingIndexTableDto.cs`.
  - `Calculators/`:
    * `GridAxisGenerator.cs`: Orthogonal X/Y and skewed/radial grid line geometry, bubble coordinates, alphanumeric label generation (ISO 128 skipping I, O, Q, Z), continuous dimension strings.
    * `SheetArrangerCalculator.cs`: 2D shelf-packing algorithm with title block collision avoidance in bottom-right corner and multi-sheet overflow pagination.
    * `DrawingIndexAggregator.cs`: Natural alphanumeric sorting (`KC-01`..`KC-10`), gap and duplicate detection, table formatting.
- `HPKata/HPKata.Core/CadUtilities/`:
  - `Models/`: `Point2D.cs`, `Vector2D.cs`, `ElevationCalloutModel.cs`, `SlopeAnnotationModel.cs`.
  - `Calculators/`:
    * `VectorProjectionHelper.cs`: Witness line trimming and normal vector projection.
    * `SlopeCalculator.cs`: Gradient calculation, quadrant angle normalization (anti-upside-down text), slope formatting (e.g. `i = 2%`).
    * `ElevationFormatter.cs`: Formats Vietnamese architectural elevation signs (`+3.600`, `±0.000`, `-1.200`).
    * `TextAlignmentCalculator.cs`: Horizontal and vertical text alignment and equal distribution.

### 2. `HPKata.UI`
- `HPKata/HPKata.UI/ViewModels/`:
  - `QsTakeoffViewModel.cs`: Reactive MVVM ViewModel for QS element table, deduction toggles, and Excel export command.
  - `DrawingUtilityViewModel.cs`: Reactive MVVM ViewModel for grid generation preview, sheet size & margin selection, and drawing register tables.
- `HPKata/HPKata.UI/Views/`:
  - `QsTakeoffWindow.xaml` / `.xaml.cs`: Bound to `Theme.xaml` (`DynamicResource Brush.*`).
  - `DrawingUtilityWindow.xaml` / `.xaml.cs`: Bound to `Theme.xaml`.

### 3. `HPKata.AutoCad` (AutoCAD 2026 .NET 8)
- `HPKata/HPKata.AutoCad/Controllers/`:
  - `QsTakeoffController.cs`: Scans ModelSpace entities, runs QS calculations and deductions, places dynamic AutoCAD Tables or exports Excel.
  - `GridDrawController.cs`: Draws grid lines, circle bubbles, attribute tags, and dimension chains in ModelSpace.
  - `SheetLayoutController.cs`: Places arranged drawing detail blocks within sheet border polylines.
  - `ViewportController.cs`: Creates and scales PaperSpace Viewports (`taomv`), toggles lock state (`khoav`/`mokhoav`).
  - `DrawingIndexController.cs`: Scans TitleBlock attributes across PaperSpace layouts and builds drawing index Table.
  - `DimensionUtilityController.cs`: Trims witness lines (`catdim`), breaks dims (`ngatdim`), continues dims (`dnoi`), annotates slopes (`sl`).
  - `TextAnnotationController.cs`: Aligns text (`dtext`), creates elevation callout symbols (`mcat`).
  - `LayerUtilityController.cs`: Controls layer freeze/on/off (`anlayer`, `hienlayer`, `doilayer`).
  - `KataCleanController.cs`: Purges temporary XRecords / objects (`clean_kata` / `xoa_xrecord`).
- `HPKata/HPKata.AutoCad/Commands/`:
  - `QsCommands.cs`: `QS`, `QS_mbkc`, `QS_tru`, `xuatQS`, `timqs`, `tkqs`.
  - `LayoutCommands.cs`: `vg`, `ve_luoi_truc`, `sbv`, `sap_xep_ban_ve`, `ibv`, `chen_khung_ten`, `taomv`, `taomv1`, `khoav`, `mokhoav`, `danhmuc`.
  - `DimensionUtilityCommands.cs`: `catdim`, `cat_chan_dim`, `ngatdim`, `ngat_dim`, `dnoi`, `dim_noi_tiep`, `sl`, `ghi_do_doc`.
  - `TextAnnotationCommands.cs`: `dtext`, `can_chinh_text`, `mcat`, `cao_do_mat_cat`.
  - `LayerUtilityCommands.cs`: `anlayer`, `an_layer`, `hienlayer`, `hien_tat_ca_layer`, `doilayer`, `doi_layer`.
  - `KataCleanCommands.cs`: `clean_kata`, `xoa_xrecord`.
- Preserve `GoiLenh_HPKata.lsp`: Ensure exact 238-line syntax, config offset at line 152 intact, and all 27 Phase 5 command aliases work smoothly.

### 4. `HPKata.Revit` (Revit 2026 .NET 8)
- `HPKata/HPKata.Revit/Services/`:
  - `RevitQsTakeoffService.cs`: Queries Revit elements, calculates solid volumes and contact deductions.
  - `RevitSheetArrangementService.cs`: Automates `ViewSheet.Create`, title block placement, and `Viewport.Create`.
- `HPKata/HPKata.Revit/Commands/`:
  - `RevitQsCommands.cs`, `RevitLayoutCommands.cs`.
- Strictly enforce Rule R5: all unit conversions strictly in `RevitUnitConverter.cs`.

### 5. Unit & Integration Tests
- Add tests in `HPKata.Core.Tests`, `HPKata.UI.Tests`, `HPKata.AutoCad.Tests`, `HPKata.Revit.Tests`, and `HPKata.E2E.Tests`.
- Target: expand solution test suite to $\ge 840+$ tests (all passing 100% green).

## Non-Negotiable Rules
- Build & Test command: Always use `--disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"`.
- Rule C2: Every C# file $\le 300$ physical lines.
- Rule M3: Every method $\le 50$ physical lines.
- Rule Q-B1: Zero commented-out code, zero TODOs/FIXMEs/HACKs/NotImplementedException.
- Rule Q-B2: Zero unannotated empty catch blocks.
- Rule R5: Constant `304.8` strictly in `RevitUnitConverter.cs`.
- Pure `HPKata.Core`: Zero `Autodesk.*` references.
