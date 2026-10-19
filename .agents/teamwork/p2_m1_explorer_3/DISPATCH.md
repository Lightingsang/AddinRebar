# Dispatch: Explorer 3 (Milestone 1 — VBA Extraction & Sync)

You are Explorer 3 (`teamwork_preview_explorer`).
Investigate the technical implementation for extracting complete plain-text VBA source code from the unlocked `C:\kata_pro\Kata.xlsm`:
1. Check the 21 expected VBA modules: `Ve_dam`, `Tinh_dam`, `to_hop`, `save_info`, `Module1`, `ThisWorkbook`, `Sheet1`..`Sheet15`. Note the sheet names and how they correspond to workbook sheets (`Dam`, `Cot`, `Vach`, `save_info`, `Thong_ke`, etc.).
2. Detail the structure of `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md` ensuring it documents every Sub, Function, Type, cell coordinate, and algorithm extracted from the VBA code.
3. Check the exact file synchronization process: copying `C:\kata_pro\Kata.xlsm` to `C:\HPKata\Kata.xlsm` and `HPKata/Distribution/Assets/Kata.xlsm` without touching `C:\kata_pro\Kata.xlsm`.
4. Ensure verification commands and criteria are established.
5. Write your handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_3\handoff.md`.


## 2026-10-18T08:16:40Z
[Message] sender=70f8ea72-c348-4512-8527-7930db5e95da priority=MESSAGE_PRIORITY_HIGH
You are Explorer 3 for Milestone 1 (VBA Extraction & Sync).
Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_3\
Mandatory inputs to read:
- ORIGINAL_REQUEST.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- DISPATCH.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_3\DISPATCH.md
- SCOPE.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_27\SCOPE.md

Non-negotiable rule: C:\kata_pro\ is 100% READ-ONLY! Never modify any file in C:\kata_pro\.
Your mission:
1. Inventory the 21 modules in Kata.xlsm: procedural modules (Ve_dam, Tinh_dam, to_hop, save_info, Module1), document modules (ThisWorkbook, Sheet1..Sheet15).
2. Define the schema and documentation plan for docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md (cataloging Subs, Functions, Types, cell references, structural calculation logic).
3. Outline how the Worker should implement the end-to-end extraction script, write the markdown artifact, and copy the unlocked workbook to C:\HPKata\Kata.xlsm and HPKata/Distribution/Assets/Kata.xlsm.
4. Write your findings to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_3\handoff.md and report back via send_message.
