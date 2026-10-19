# Task Assignment: Survey Spec Miner 3 (Specification Mining for Excel UDF Parity & Binary Decompilation)

- Subagent Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (UTC header `## 2026-10-18T00:11:55Z`)
- Project Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDcePro\HPDcePro.slnx`
- Documentation & Reference Specifications:
  - `docs/rea-dce-pro/03-steel-and-excel.md`
  - `docs/rea-dce-pro/04-command-mapping-and-roadmap.md`
  - Shipped binaries (STRICTLY READ-ONLY, NO MODIFICATION):
    - `C:\Dce Pro\AddinSteelCad\DbimSteelAutocad.dll`
    - `C:\Dce Pro\AddinSteel\DceSteel.dll`
    - `C:\Dce Pro\V.2020\DCE Excel.dll`

## Objectives
1. Read `ORIGINAL_REQUEST.md` at header `## 2026-10-18T00:11:55Z` and investigate **R3: Complete DCE Excel & DceSteel UDF Parity**:
   - Inspect existing `CalculationUdfBridge` in `HPDcePro.Excel`.
   - Mine exact mathematical definitions, argument lists, return types, and validation rules for all required UDFs:
     - RC & Geotechnical: `TinhCotNeUonXien_PMM`, `TinhVachCung`, `TinhSanHaiPhuong`, `TinhDaiCoc`, `TinhMongDon`, `SucChiuTaiCoc_MaSat`, `BeRongVetNutDam`.
     - Steel & Wind: `TinhDamThep_TCVN5575`, `TinhCotThep_TCVN5575`, `TinhLienKetMoment_TCVN5575`, `TinhBuLongNeo_TCVN5575`, `TinhTaiTrongGio_TCVN2737`.
   - Inspect `docs/rea-dce-pro/03-steel-and-excel.md` and read the decompiled metadata or signatures if needed.
2. Investigate how these UDFs connect to `HPDcePro.Core` calculators (or whether companion modular UDF classes in `HPDcePro.Excel` wrap Core calculators).
3. Check test requirements for UDF parity to ensure extensive automated test coverage.
4. Output your findings into `handoff.md` in your working directory.

## 2026-10-18T00:14:25Z
You are Survey Spec Miner 3 for HPDcePro Wave 3.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\
Read your task assignment at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\DISPATCH.md
MANDATORY: Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (entry dated 2026-10-18T00:11:55Z).
Read Clean Code rules at: docs/clean-code/HP_CLEAN_CODE_CORE.md and AGENTS.md.

Explore and mine authoritative specifications for R3: Complete DCE Excel & DceSteel UDF Parity:
1. Inspect existing HPDcePro.Excel/CalculationUdfBridge.cs and related files.
2. Inspect docs/rea-dce-pro/03-steel-and-excel.md and docs/rea-dce-pro/04-command-mapping-and-roadmap.md.
3. If necessary, inspect shipped binaries metadata (STRICTLY READ-ONLY, NEVER MODIFY C:\Dce Pro):
   - C:\Dce Pro\AddinSteelCad\DbimSteelAutocad.dll
   - C:\Dce Pro\AddinSteel\DceSteel.dll
   - C:\Dce Pro\V.2020\DCE Excel.dll
4. Extract exact signatures, parameters, return types, defaults, and calculation mappings for:
   - RC & Geotechnical: TinhCotNeUonXien_PMM, TinhVachCung, TinhSanHaiPhuong, TinhDaiCoc, TinhMongDon, SucChiuTaiCoc_MaSat, BeRongVetNutDam.
   - Steel & Wind: TinhDamThep_TCVN5575, TinhCotThep_TCVN5575, TinhLienKetMoment_TCVN5575, TinhBuLongNeo_TCVN5575, TinhTaiTrongGio_TCVN2737.
5. Detail how these UDFs connect to HPDcePro.Core calculation engines and how they should be tested in HPDcePro.Adapters.Tests.

Write your detailed findings into:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_spec_miner_survey_3\handoff.md
When finished, send a message to the orchestrator (caller) informing that you are done with the path to handoff.md.
