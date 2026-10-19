# Dispatch: Explorer 1 (Milestone 1 — VBA Extraction & Sync)

You are Explorer 1 (`teamwork_preview_explorer`).
Investigate the technical implementation for extracting complete plain-text VBA source code from the unlocked `C:\kata_pro\Kata.xlsm`:
1. Inspect `C:\kata_pro\Kata.xlsm` (remember `C:\kata_pro\` is 100% READ-ONLY). It is a ZIP container containing `xl/vbaProject.bin`.
2. Inspect the structure of `xl/vbaProject.bin` (OLE2 compound document, `dir` stream, module stream offsets, MS-OVBA 2.4.1 Compression and decompression algorithm).
3. Investigate available Python libraries or standard scripts (e.g. `oletools`, `olevba`, or custom Python MS-OVBA decompression script) to extract all 21 VBA modules (`Ve_dam`, `Tinh_dam`, `to_hop`, `save_info`, `Module1`, `ThisWorkbook`, `Sheet1`..`Sheet15`) without modifying `C:\kata_pro\Kata.xlsm`.
4. Document the exact steps, module names, stream offsets, and output requirements for `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md` and synchronization targets `C:\HPKata\Kata.xlsm` and `HPKata/Distribution/Assets/Kata.xlsm`.
5. Write your handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_1\handoff.md`.

## 2026-10-18T08:16:40Z
[Message] sender=70f8ea72-c348-4512-8527-7930db5e95da priority=MESSAGE_PRIORITY_HIGH
You are Explorer 1 for Milestone 1 (VBA Extraction & Sync).
Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_1\
Mandatory inputs to read:
- ORIGINAL_REQUEST.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- DISPATCH.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_1\DISPATCH.md
- SCOPE.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_27\SCOPE.md

Non-negotiable rule: C:\kata_pro\ is 100% READ-ONLY! Never modify any file in C:\kata_pro\.
Your mission:
1. Inspect C:\kata_pro\Kata.xlsm, extract xl/vbaProject.bin in memory/temp.
2. Analyze the OLE2 compound document and MS-OVBA 2.4.1 compression algorithm.
3. Formulate the precise Python script/tool (using olefile, olevba, or pure MS-OVBA RLE decompression) to extract all 21 VBA modules (Ve_dam, Tinh_dam, to_hop, save_info, Module1, ThisWorkbook, Sheet1..Sheet15).
4. Outline the exact structure of docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md and the sync copy steps.
5. Write your findings to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_1\handoff.md and report back via send_message.
