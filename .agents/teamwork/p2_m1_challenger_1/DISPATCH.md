# Dispatch: Challenger 1 (Milestone 1 — VBA Extraction & Sync)

You are Challenger 1 (`teamwork_preview_challenger`).
Your working directory is: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_challenger_1\`

## Mandatory Inputs to Read
- `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
- `SCOPE.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_27\SCOPE.md`
- Worker handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_worker_m1\handoff.md`
- Generated artifact: `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md`

## Mandatory Constraints
- `C:\kata_pro\` is 100% READ-ONLY! Never modify, delete, or touch any file in `C:\kata_pro\`.
- ZERO PARALLEL DOTNET EXECUTION: DO NOT invoke `dotnet build` or `dotnet test`.

## Challenge Mission
1. Empirically verify the VBA extraction:
   - Run a Python test script reading `xl/vbaProject.bin` from `C:\kata_pro\Kata.xlsm` (in memory) to verify that every single sub and function name declared in `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md` matches the actual decompressed bytes byte-for-byte.
   - Verify that there are zero missing procedures, truncated lines, or hallucinated code blocks.
2. Stress test the cell dictionary: verify coordinates like `B3`, `I10`, `I11`, `N10`, `A11`, `A12`, `D10`, `D11` against the actual code statements in `Ve_dam`, `Tinh_dam`, and `save_info`.
3. Verify that `C:\kata_pro\Kata.xlsm` has not been altered (check SHA-256 matches baseline `89D1243CFC91A3DD2254942A41BD5EBABCAA43EBD0D8B758B016D066AB010B3D`).
4. Deliver verdict: `APPROVE` or `REQUEST_CHANGES` in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_challenger_1\handoff.md` and report back via `send_message`.

## 2026-10-18T08:35:10Z
[Message] timestamp=2026-10-18T08:35:10Z sender=70f8ea72-c348-4512-8527-7930db5e95da priority=MESSAGE_PRIORITY_HIGH content=You are Challenger 1 for Milestone 1 (VBA Extraction & Sync).
Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_challenger_1\
Mandatory inputs to read:
- ORIGINAL_REQUEST.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- DISPATCH.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_challenger_1\DISPATCH.md
- SCOPE.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_27\SCOPE.md
- Worker handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_worker_m1\handoff.md
- Generated artifact: docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md

Non-negotiable constraints:
- C:\kata_pro\ is 100% READ-ONLY!
- DO NOT execute dotnet build or dotnet test.
Your mission:
1. Empirically verify with a Python script that all subs, functions, and lines in docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md match actual decompressed bytes from xl/vbaProject.bin in memory.
2. Stress test cell references and verify C:\kata_pro\Kata.xlsm hash has not changed.
3. Document verdict (APPROVE or REQUEST_CHANGES) in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_challenger_1\handoff.md and report back via send_message.
