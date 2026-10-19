# Dispatch: explorer_m4_cad_utils

## Role & Mission
You are `explorer_m4_cad_utils`, the CAD & Dimension Utilities Domain Explorer for Milestone M4 (Phase 5: Quantity Surveying (QS) & Drawing Utilities).
Your working directory is: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_cad_utils\`
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
Mine exact specifications and design Clean Code architecture for the CAD Dimension, Text & Layer Utilities subsystem:
1. Complete Enumeration of Remaining Legacy Commands in `Goi lenh.lsp`:
   - Dimension utilities:
     * `catdim` (`cat_chan_dim`): Trims dimension witness lines to a fixed offset from dimension line or picked boundary.
     * `ngatdim` (`ngat_dim`): Breaks dimension lines at intersection with other drawing entities.
     * `dnoi` (`dim_noi_tiep`): Continuous dimension chain generator.
     * `sl` (`ghi_do_doc`): Slope/gradient annotation with arrow and percentage text (e.g. `i = 2%`).
   - Text & Elevation utilities:
     * `dtext` (`can_chinh_text`): Vertical and horizontal alignment of text entities.
     * `mcat` (`cao_do_mat_cat`): Elevation level callout symbol with numeric elevation string (e.g. `+3.600`).
   - Layer utilities:
     * `anlayer` (`an_layer`): Freezes or turns off selected entity layer.
     * `hienlayer` (`hien_tat_ca_layer`): Unfreezes and turns on all drawing layers.
     * `doilayer` (`doi_layer`): Maps entities to standard Kata layer conventions (`KATA_BEAM`, `KATA_COL`, `KATA_REBAR`, `KATA_DIM`, etc.).
   - Miscellaneous utilities:
     * `xoa_xrecord` / `clean_kata`: Purges or resets Kata XRecords and temporary entity markers.
2. Architecture & Design:
   - `HPKata.Core/CadUtilities/`: Mathematical helpers for vector projection, line clipping, slope string formatting, elevation string formatting (`+0.000` / `-0.000`).
   - `HPKata.AutoCad/Controllers/`: `DimensionUtilityController.cs`, `TextAnnotationController.cs`, `LayerUtilityController.cs`.
   - Update `GoiLenh_HPKata.lsp` to ensure exact 238-line syntax, config offset at line 152, and full coverage of all 27 Phase 5 aliases.
3. Clean Code Architecture:
   - File length <= 300 lines (Rule C2).
   - Method length <= 50 lines (Rule M3).
   - Zero host references in `HPKata.Core`.

## Deliverable
Write your specification report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_cad_utils\cad_utils_spec_report.md`
and a concise `handoff.md` in your working directory.
When complete, notify parent via `send_message`.
