# Dispatch: Explorer Survey 1 (Kata & Excel Domain / Spec Miner)

- **Role**: teamwork_preview_explorer
- **Task**: Map Kata spreadsheet contract, KataExport implementation, Excel COM / ClosedXML usage, and cell specifications for sheet 'Dam'.
- **Authoritative Request**: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (under ## 2026-09-27T15:57:37Z)
- **Scope**:
  1. Investigate `HPRebar/KataExport` and `HPRebar.Core/KataExport` (and any related plans/reports such as `plans/260926-2317-kata-export-hprebar/` and `reports/kata-cell-contract.md`).
  2. Search for any `.xlsm` / `.xlsx` sample files in the repo or references to Kata.
  3. Detail the exact cell mapping of sheet `Dam` according to user requirements:
     - Header: Name `B3`, count `B4`, dimensions b x h `B5:B6`, slab thickness `B7`, level `B10`, anchorage multipliers `G2:G3`, `H3`, `H5`.
     - Main bars: Continuous top `B11`, bottom `B12`.
     - Additional bars: Top extra layers rows 13-16, bottom extra layers rows 17-18.
     - Side bars: `E5`, `G4`, row 20.
     - Stirrups: Diameter `G6`, spacing near support `G7`, midspan spacing `G8`, types rows 25-27.
  4. Investigate COM reading via `oleaut32` and fallback to `ClosedXML` (check if ClosedXML is available or referenced in `HPExcel` or if package is needed).
- **Deliverable**: Write a comprehensive survey report to `report.md` in your working directory and deliver `handoff.md`.
