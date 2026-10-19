# Dispatch: Spec Miner M1.3 (Excel, Steel, MBKC, Native C++ & Revit Specification)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\spec_miner_m1_3\`
- Parent Orchestrator: `orchestrator_25`
- Role: `teamwork_preview_spec_miner`
- Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\PROJECT.md`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (`## 2026-10-18T05:27:06Z`)

## Mission
Extract and structure the exact specifications for all 73 Excel commands (`danhba_dce_excel.json`), 49 DceSteel functions (`danhba_dce_thep.json`), 29 MBKC functions (`danhba_mbkc.json`), 69 native C++ exports (`DceAnalyssiss64.dll`), and 23 Revit commands (`DBimRevit.dll`).
Report findings to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\spec_miner_m1_3\handoff.md`.
## 2026-10-18T05:50:43Z
[Message] timestamp=2026-10-18T05:50:43Z sender=1b2bf217-fcc4-4416-87ff-86b0fe0ef59a priority=MESSAGE_PRIORITY_HIGH content=You are Spec Miner M1.3 for Milestone M1 (Excel, Steel, MBKC, Native C++ & Revit Specification).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\spec_miner_m1_3\
Read your dispatch specification at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\spec_miner_m1_3\DISPATCH.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (`## 2026-10-18T05:27:06Z`).
Read the project scope at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\PROJECT.md.
Inspect `C:\Dce Pro\AiAgent\DanhBa\danhba_dce_excel.json`, `danhba_dce_thep.json`, `danhba_mbkc.json`, and extracted Revit DLLs at `scratch\dce_rvt_unpacked\app\2026\AddinDbimDcepro\DBimRevit.dll`.

Your mission:
1. Extract and inventory all 73 Excel Ribbon actions from `danhba_dce_excel.json`.
2. Extract and inventory all 49 DceSteel functions from `danhba_dce_thep.json`.
3. Extract and inventory all 29 MBKC functions from `danhba_mbkc.json`.
4. Extract and inventory all 69 native C++ exports from `DceAnalyssiss64.dll` with ordinals and RVAs.
5. Extract and inventory all 23 Revit `IExternalCommand` classes + 1 `IExternalApplication` from `DBimRevit.dll`.
6. Format these entries into ready-to-insert markdown tables for `docs/rea-dce-pro/TRUTH_MATRIX.md`.
7. Write your report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\spec_miner_m1_3\handoff.md`.
Communicate your completion back to parent via send_message.
