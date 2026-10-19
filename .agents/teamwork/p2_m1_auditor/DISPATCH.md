# Dispatch: Forensic Auditor (Milestone 1 — VBA Extraction & Sync)

You are Forensic Auditor (`teamwork_preview_auditor`).
Your working directory is: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_auditor\`

## Mandatory Inputs to Read
- `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
- `SCOPE.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_27\SCOPE.md`
- Worker handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_worker_m1\handoff.md`
- Generated artifact: `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md`

## Mandatory Constraints
- `C:\kata_pro\` is 100% READ-ONLY! Never modify, delete, or touch any file in `C:\kata_pro\`.
- ZERO PARALLEL DOTNET EXECUTION: DO NOT invoke `dotnet build` or `dotnet test`. Read the build/test outputs documented in Worker 1's handoff.

## Audit Mission (Integrity Forensics)
Perform forensic integrity verification:
1. Anti-Cheat & Authenticity Check:
   - Check that `docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md` is genuinely extracted from `xl/vbaProject.bin`, not fabricated or filled with dummy/placeholder code.
   - Verify that all 21 modules are present with exact line counts matching the MS-OVBA decompression output.
2. Read-Only Invariant Audit:
   - Check the SHA-256 and modification timestamp of `C:\kata_pro\Kata.xlsm`. Confirm zero mutation.
   - Confirm that no files were written to `C:\kata_pro\`.
3. Synchronization Audit:
   - Verify that `C:\HPKata\Kata.xlsm` and `HPKata/Distribution/Assets/Kata.xlsm` are byte-identical to `C:\kata_pro\Kata.xlsm`.
4. Binary Veto Verdict:
   - Deliver verdict: `CLEAN` or `INTEGRITY VIOLATION` in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_auditor\handoff.md` and report back via `send_message`.

## 2026-10-18T08:35:10Z
[Message from 70f8ea72-c348-4512-8527-7930db5e95da]
You are Forensic Auditor for Milestone 1 (VBA Extraction & Sync).
Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_auditor\
Mandatory inputs to read:
- ORIGINAL_REQUEST.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- DISPATCH.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_auditor\DISPATCH.md
- SCOPE.md: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_27\SCOPE.md
- Worker handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_worker_m1\handoff.md
- Generated artifact: docs/rea-kata-pro/KATA_XLSM_VBA_MODULES.md

Non-negotiable constraints:
- C:\kata_pro\ is 100% READ-ONLY!
- DO NOT execute dotnet build or dotnet test. Read the test results documented in Worker 1's handoff.
Your mission:
1. Perform forensic integrity verification: ensure KATA_XLSM_VBA_MODULES.md contains genuine decompressed VBA code and not dummy/mock implementations.
2. Verify zero-mutation on C:\kata_pro\Kata.xlsm (SHA-256 matches baseline 89D1243CFC91A3DD2254942A41BD5EBABCAA43EBD0D8B758B016D066AB010B3D).
3. Verify synchronization integrity across all 3 copies.
4. Deliver binary verdict: CLEAN or INTEGRITY VIOLATION in g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\p2_m1_auditor\handoff.md and report back via send_message.
