# Dispatch: Explorer M5-2 (DWG Rebar Geometry, 2-Way XData & Handle Linking)

**Agent**: `explorer_m5_2` (`teamwork_preview_explorer`)
**Parent**: `orchestrator_16` (`4300a9ce-e3eb-419c-8493-f2001d2d04d6`)
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m5_2\`
**Date**: 2026-10-17

## Mandatory Reading
1. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\hpkata\HPKATA_REVERSE_ENGINEERING_BLUEPRINT.md` (Sections 4, 5, 6)
3. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md`
4. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\docs\clean-code\HP_CLEAN_CODE_CORE.md`

## Mission
Investigate and design the AutoCAD database interop for rebar geometry, XData, and handle linking:
1. Rebar Polyline Generation: Converting `HPKata.Core.Shapes.RebarShapeDefinition` and dimensions into 2D `Polyline` with fillet arc bulges and global width.
2. XData RegApp `"KATA"`: Reading and writing the structured 12-item XData stream (`Dang`, `STT`, `phi`, `kc`, `sl`, `sl_ck`, `L`, `goc_uon`, `ten_ck`, `stt_thep_chinh`, `chu_thich`, `Z`) using AutoCAD `ResultBuffer` and DxfCodes 1001, 1000, 1070.
3. Block Attribute Mapping: Reading and updating block attributes for `kata_block_KHT`, `kata_block_KHTCT`, `thep`, `Kata_Block_Beam_Rebar`, etc.
4. Dynamic Handle Linking Engine: Detailed implementation strategy for commands:
   - `link` (`tao_lien_ket`): Bidirectional handle cross-referencing between rebar polyline (1005) and tag block (1005).
   - `cl` (`copy_tao_lien_ket`): Clone interception and handle rebinding.
   - `ud` (`update_thep`): Polyline vertex reading -> segment extraction -> cutting length calculation -> tag attribute update.
   - `dl` (`xoa_lien_ket`), `tim` (`xem_lien_ket`), `xoa` (`xoa_highlight`).
   - `dimthep`: Automated dimension placement along rebar segments.
5. Clean Code decomposition: Ensure interop services and command helpers are partitioned so no class exceeds 300 lines (Rule C2) and no method exceeds 50 lines (Rule M3).
6. Deliver structured analysis in `analysis.md` and complete handoff report in `handoff.md`.

## Mandatory Constraints
- `C:\kata_pro` is strictly read-only.
- Never inspect, decompile, or touch `Security.dll` or licensing code.
- Report all file paths and recommended class structures.
