# Dispatch: Explorer 2 (Milestone 1 — VBA Extraction & Sync)

You are Explorer 2 (`teamwork_preview_explorer`).
Investigate the technical implementation for extracting complete plain-text VBA source code from the unlocked `C:\kata_pro\Kata.xlsm`:
1. Check the existing file `C:\kata_pro\Kata.xlsm` (100% READ-ONLY). Read its ZIP central directory and check `xl/vbaProject.bin`.
2. Analyze the MS-OVBA 2.4.1 compression algorithm in detail (State Machine, Compression Header, Compressed Chunk, Decompressed Chunk, token bytes, copy tokens, literal tokens).
3. Verify if `oletools` / `olevba` is available in Python environment or if a standalone Python script utilizing `olefile` or pure Python can perform standard OLE2 parsing and extract all 21 VBA modules cleanly.
4. Verify the destination paths:
   - `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md`
   - `C:\HPKata\Kata.xlsm`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\Distribution\Assets\Kata.xlsm`
5. Write your handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_2\handoff.md`.


## 2026-10-18T08:16:40Z
You are Explorer 2 for Milestone 1 (VBA Extraction & Sync).
Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_2\
Mandatory inputs to read:
- ORIGINAL_REQUEST.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- DISPATCH.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_2\DISPATCH.md
- SCOPE.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_27\SCOPE.md

Non-negotiable rule: C:\kata_pro\ is 100% READ-ONLY! Never modify any file in C:\kata_pro\.
Your mission:
1. Check Python environment and available packages (e.g. olefile, oletools).
2. Detail the MS-OVBA section 2.4.1 decompression algorithm logic (DecompressedCurrent, CompressedCurrent, ChunkHeader, FlagByte, CopyToken/LiteralToken).
3. Verify that all 21 VBA streams can be extracted cleanly without corruption.
4. Verify the exact copy target paths: C:\HPKata\Kata.xlsm and HPKata/Distribution/Assets/Kata.xlsm.
5. Write your findings to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_2\handoff.md and report back via send_message.
