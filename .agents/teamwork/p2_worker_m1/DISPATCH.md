# Dispatch: Worker 1 (Milestone 1 — VBA Extraction & Sync)

You are Worker 1 (`teamwork_preview_worker`).
Your working directory is: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_worker_m1\`

## Mandatory Inputs to Read First
- `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
- `SCOPE.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_27\SCOPE.md`
- Explorer handoffs:
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_1\handoff.md`
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_2\handoff.md`
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_3\handoff.md`

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Non-Negotiable Constraints
- `C:\kata_pro\` is 100% READ-ONLY! Never modify, overwrite, delete, or touch any file in `C:\kata_pro\`.
- All `dotnet` commands must be single-node sequential: `--disable-build-servers -m:1`.

## File Ownership
You exclusively own writing to:
- `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md`
- `C:\HPKata\Kata.xlsm`
- `HPKata/Distribution/Assets/Kata.xlsm`
- Temporary extraction scripts in your working directory or `%TEMP%`.

## Mission Objectives
1. Implement a complete Python extraction script using Python 3.14 (`python` or `C:\Python314\python.exe`) and `oletools.olevba.VBA_Parser` to decompress all 21 VBA modules from `C:\kata_pro\Kata.xlsm` (`xl/vbaProject.bin`).
2. Generate `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md` containing:
   - Header metadata, SHA-256 hashes, file lengths
   - Executive summary and module statistics table for all 21 modules
   - Worksheet ↔ Document Module mapping table
   - Complete plain-text VBA code listings for all 5 procedural modules (`Ve_dam`, `Tinh_dam`, `to_hop`, `save_info`, `Module1`) with detailed documentation of algorithms, engineering formulas, Subs, Functions, Types, and cell mappings
   - Document modules (`ThisWorkbook`, `Sheet1`..`Sheet15`)
   - Comprehensive cell & range reference dictionary (`B3`, `I10`, `I11`, `N10`, etc.)
   - Traceability ledger
3. Synchronize `C:\kata_pro\Kata.xlsm` to `C:\HPKata\Kata.xlsm` and `HPKata/Distribution/Assets/Kata.xlsm`. Verify SHA-256 is `89D1243CFC91A3DD2254942A41BD5EBABCAA43EBD0D8B758B016D066AB010B3D` and length is `474,272` bytes for all three files.
4. Verify `HPKata` solution builds and passes tests cleanly:
   `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`
   `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1 --no-build`
5. Write your complete handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_worker_m1\handoff.md` and report back via `send_message`.

## 2026-10-18T08:26:15Z
[Message] timestamp=2026-10-18T08:26:15Z sender=70f8ea72-c348-4512-8527-7930db5e95da priority=MESSAGE_PRIORITY_HIGH content=You are Worker 1 for Milestone 1 (VBA Extraction & Sync).
Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_worker_m1\

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Mandatory inputs to read:
- ORIGINAL_REQUEST.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- DISPATCH.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_worker_m1\DISPATCH.md
- SCOPE.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_27\SCOPE.md
- Explorer handoffs:
  - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_1\handoff.md
  - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_2\handoff.md
  - g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_explorer_3\handoff.md

Your mission:
1. Decompress all 21 VBA modules from unlocked C:\kata_pro\Kata.xlsm (remember C:\kata_pro\ is 100% READ-ONLY) using Python 3.14 and oletools.olevba.VBA_Parser.
2. Generate docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md with complete plain-text VBA source code, functions, types, algorithms, and cell references.
3. Synchronize C:\kata_pro\Kata.xlsm to C:\HPKata\Kata.xlsm and HPKata/Distribution/Assets/Kata.xlsm. Verify SHA-256 is 89D1243CFC91A3DD2254942A41BD5EBABCAA43EBD0D8B758B016D066AB010B3D and size is 474,272 bytes on all copies, while C:\kata_pro\ remains unmodified.
4. Run single-node verification tests for HPKata:
   dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
   dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1 --no-build
5. Document all commands, file hashes, and test outputs in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_worker_m1\handoff.md and report back via send_message.
