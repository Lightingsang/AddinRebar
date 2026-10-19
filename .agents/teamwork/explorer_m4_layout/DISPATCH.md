# Dispatch: explorer_m4_layout

## Role & Mission
You are `explorer_m4_layout`, the Drawing Layout & Viewport Domain Explorer for Milestone M4 (Phase 5: Quantity Surveying (QS) & Drawing Utilities).
Your working directory is: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_layout\`
Your parent conversation ID is: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

## Authoritative Reference Sources
1. Master user request at:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (headers ## 2026-10-16T23:55:10Z and ## 2026-10-17T06:46:41Z)
2. Master project scope at:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`
3. Reference KataPro files:
   - `C:\kata_pro\Kata_pro64_Cad2025.dll`
   - `C:\kata_pro\Goi lenh.lsp`
   - `docs/hpkata/HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md`

## Your Exploration Objectives
Mine exact specifications and design Clean Code architecture for the Drawing Layout, Grid & Viewport subsystem:
1. `HPKata.Core/DrawingLayout/`:
   - Grid Generator models & calculators: `GridLineModel`, `GridSystemModel`, `GridAxisGenerator` (orthogonal X/Y grids, skewed/radial grids, bubble circle radius, extension offset, text labels: 1, 2, 3... and A, B, C...).
   - Sheet & Title Block models: `SheetSizeDefinition` (A0, A1, A2, A3, A4), `TitleBlockModel`, `SheetArrangerCalculator` (auto-packing drawing blocks into sheet grids with margin and gutter spacing).
   - Drawing Register / Index: `DrawingIndexItem`, `DrawingIndexAggregator` (drawing number, drawing title, revision, date, scale).
2. `HPKata.AutoCad/`:
   - Commands & Lisp aliases:
     * `vg` (`ve_luoi_truc`): Generates grid lines, bubbles, labels, and overall dimension strings.
     * `sbv` (`sap_xep_ban_ve`): Arranges multiple drawing details into a grid within border boundaries.
     * `ibv` (`chen_khung_ten`): Inserts standard title block frame at picked location.
     * `taomv` (`tao_viewport`): Creates a PaperSpace layout with MView viewport scaled to selected detail.
     * `khoav` (`khoa_viewport`) & `mokhoav` (`mo_khoa_viewport`): Toggles viewport display locked property.
     * `danhmuc` (`tao_danh_muc_ban_ve`): Scans title blocks across PaperSpace layouts and creates a summary Table in ModelSpace or Excel.
   - Controllers: `GridDrawController.cs`, `SheetLayoutController.cs`, `ViewportController.cs`, `DrawingIndexController.cs`.
3. `HPKata.Revit/`:
   - `RevitSheetArrangementService.cs`: Automates `ViewSheet` creation, title block family placement, and view placement via `Viewport.Create`.
4. Clean Code Architecture:
   - File length <= 300 lines (Rule C2).
   - Method length <= 50 lines (Rule M3).
   - Zero host references in `HPKata.Core`.

## Deliverable
Write your specification report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_layout\layout_spec_report.md`
and a concise `handoff.md` in your working directory.
When complete, notify parent via `send_message`.
