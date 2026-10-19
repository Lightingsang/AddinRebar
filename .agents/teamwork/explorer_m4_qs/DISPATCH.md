# Dispatch: explorer_m4_qs

## Role & Mission
You are `explorer_m4_qs`, the Quantity Surveying (QS) Domain & Specification Miner for Milestone M4 (Phase 5: Quantity Surveying (QS) & Drawing Utilities).
Your working directory is: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_qs\`
Your parent conversation ID is: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

## Authoritative Reference Sources
1. Master user request at:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (headers ## 2026-10-16T23:55:10Z and ## 2026-10-17T06:46:41Z)
2. Master project scope at:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`
3. Reference KataPro files:
   - `C:\kata_pro\Kata_pro64_Cad2025.dll` (read-only decompilation via ilspycmd if helpful)
   - `C:\kata_pro\Goi lenh.lsp`
   - `C:\kata_pro\Kata.xlsm`
   - `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`

## Your Exploration Objectives
Mine exact specifications and design Clean Code architecture for the Quantity Surveying subsystem:
1. `HPKata.Core/QuantitySurveying/`:
   - Models: `QsElementModel`, `QsMemberType` (Footing, Column, Beam, Slab, Wall, Stair), `QsVolumeResult`, `QsFormworkResult`, `QsDeductionRule`.
   - Calculators: `QsTakeoffCalculator` (gross volume, gross surface area), `QsIntersectionDeductionEngine` (standard structural deduction priority: Column takes precedence over Beam/Slab; Footing takes precedence over Column dowel volume; Beam takes precedence over Slab). Formwork contact deduction at intersections.
   - Persistence: `QsScheduleAggregator`, `QsWorkbookGenerator` (ClosedXML export to `.xlsx` matching legacy `Kata.xlsm` QS sheets with summary and detail breakdown by member mark and story).
2. `HPKata.AutoCad/`:
   - Commands & Lisp aliases: `QS` (takeoff main), `QS_mbkc` (takeoff structural framing plan), `QS_tru` (deduction settings), `xuatQS` (export QS to Excel/Table).
   - Controllers: `QsTakeoffController.cs` (scans DWG blocks and bounding polylines, applies deduction matrix, generates DWG table or Excel file).
3. `HPKata.Revit/`:
   - `RevitQsTakeoffService.cs`: Queries Revit elements (`OST_StructuralColumns`, `OST_StructuralFraming`, `OST_Floors`, `OST_StructuralFoundation`, `OST_Walls`), extracts geometric volumes and solid intersection deductions.
4. Clean Code Architecture:
   - File length <= 300 lines (Rule C2).
   - Method length <= 50 lines (Rule M3).
   - Zero host references in `HPKata.Core`.

## Deliverable
Write your specification report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_qs\qs_spec_report.md`
and a concise `handoff.md` in your working directory.
When complete, notify parent via `send_message`.
