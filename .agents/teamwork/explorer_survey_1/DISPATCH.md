# Dispatch: Explorer Survey 1 (Legacy Baseline Binary Analysis & Tooling)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\`
- Parent Orchestrator: `orchestrator_25`
- Role: `teamwork_preview_explorer`

## Mandatory Reference Documents
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (Read header `## 2026-10-18T05:27:06Z` carefully)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\DISPATCH.md`

## Mission
Investigate the legacy baseline binaries at `C:\Dce Pro` (strictly read-only!), verify analysis tools (`rea@6.6.0`, `ilspycmd`), unpack `DBIM_RVT_26.exe` to a scratch workspace, and catalog all binary assemblies and native boundaries.

## Detailed Tasks
1. Verify `rea@6.6.0` (`node "C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs" --version` and CLI `C:\Users\STR-HP03\AppData\Roaming\npm\rea.cmd --version`) and `ilspycmd` (`C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe`).
2. Inspect `C:\Dce Pro` (STRICTLY READ-ONLY):
   - Catalog all managed DLLs (`DCE Pro.dll`, `DceSteel.dll`, `DbimSteelAutocad.dll`, `DCE Excel.dll`, etc.) with file sizes and architecture.
   - Catalog native DLLs (`DceAnalyssiss64.dll`, etc.) and their export tables (69 native C++ exports).
3. Unpack `C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe` into a scratch workspace outside `C:\Dce Pro` (e.g. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\dce_rvt_unpacked\`) using available tools (e.g. 7-Zip, innoextract, or rea). Inspect the extracted Revit 2026 add-in DLLs and manifests.
4. Run sample inspection via `rea inspect-managed-artifact` and `ilspycmd` to verify readability and determine if any assemblies/methods are obfuscated or uninspectable.
5. Record findings, evidence hashes, and exact metadata tokens in `.agents/teamwork/explorer_survey_1/handoff.md`.


## 2026-10-18T05:32:30Z
You are Explorer Survey 1 for HPDcePro 100% Standalone Reverse-Engineering.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\
Read your dispatch specification at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\DISPATCH.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (header ## 2026-10-18T05:27:06Z).

Your mission:
1. Verify `rea@6.6.0` (`node "C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs" --version` and CLI `C:\Users\STR-HP03\AppData\Roaming\npm\rea.cmd --version`) and `ilspycmd` (`C:\Users\STR-HP03\.dotnet\tools\ilspycmd.exe`).
2. Inspect `C:\Dce Pro` (STRICTLY READ-ONLY):
   - Catalog all managed DLLs (`DCE Pro.dll`, `DceSteel.dll`, `DbimSteelAutocad.dll`, `DCE Excel.dll`, etc.) with file sizes, versions, and architectures.
   - Catalog native DLLs (`DceAnalyssiss64.dll`, etc.) and their export tables (69 native C++ exports).
3. Unpack `C:\Dce Pro\AddinSetup_RVT\DBIM_RVT_26.exe` into a scratch workspace outside `C:\Dce Pro` (e.g. `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\scratch\dce_rvt_unpacked\`) using available tools (e.g. 7-Zip, innoextract, or rea). Inspect the extracted Revit 2026 add-in DLLs and manifests.
4. Run sample inspection via `rea inspect-managed-artifact` and `ilspycmd` to verify readability and determine if any assemblies/methods are obfuscated or uninspectable.
5. Record findings, evidence hashes, and exact metadata tokens in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\handoff.md`.
Communicate your completion back to parent via send_message.
