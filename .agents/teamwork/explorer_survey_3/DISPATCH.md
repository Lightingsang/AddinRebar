## 2026-10-18T05:32:30Z

# Dispatch: Explorer Survey 3 (Legacy Catalogs & Static Assets Mapping)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\`
- Parent Orchestrator: `orchestrator_25`
- Role: `teamwork_preview_explorer`

## Mandatory Reference Documents
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Read header `## 2026-10-18T05:27:06Z` carefully)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\DISPATCH.md`

## Mission
Investigate machine-readable catalogs in `C:\Dce Pro\AiAgent\DanhBa\*.json` and inventory all static engineering data assets in `C:\Dce Pro` vs `HPDcePro/assets/`.

## Detailed Tasks
1. Inspect all JSON catalog files in `C:\Dce Pro\AiAgent\DanhBa\*.json`:
   - Enumerate all 311 AutoCAD `<CommandMethod>` entries.
   - Enumerate all 73 Excel Ribbon commands and dispatch actions.
   - Enumerate 49 `DceSteel` functions and 29 `MBKC` functions.
   - Enumerate 69 native C++ exports from `DceAnalyssiss64.dll`.
   - Enumerate all Revit external commands.
2. Inventory static engineering assets in `C:\Dce Pro` (STRICTLY READ-ONLY):
   - 17 `.dwg` block/titleblock templates (`Cad\*.dwg`, `AutoStr\00.FileDungChung\*.dwg`).
   - 132 `.xlsm` calculation workbooks (`Excel\**\*.xlsm`).
   - `MayHoc\mauhoc.db` + `model.zip`.
   - 178 `System\*.txt` config files.
   - `Language\**\*.txt` localization dictionaries.
   - `Icon\**` and `.cuix` menus.
3. Compare with `HPDcePro/assets/` to determine what assets are already migrated, what are missing, and check relative path handling in `.lsp`, `.xlsm`, and config files.
4. Provide structured inventory counts and paths for the truth matrix and migration plan.
5. Write comprehensive report to `.agents/teamwork/explorer_survey_3/handoff.md`.
