# Dispatch: Worker M1 (Truth Matrix Generation)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1\`
- Parent Orchestrator: `orchestrator_25`
- Role: `teamwork_preview_worker`
- Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\PROJECT.md`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (`## 2026-10-18T05:27:06Z`)

## Mandatory Reference Inputs
- Explorer M1.1 Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_1\handoff.md`
- Explorer M1.1 Generator Script: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_1\build_truth_matrix_engine.py`
- Explorer M1.1 Reference Matrix: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_1\PROPOSED_TRUTH_MATRIX.md`
- Spec Miner M1.2 Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\spec_miner_m1_2\handoff.md`
- Spec Miner M1.3 Sections: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\spec_miner_m1_3\truth_matrix_sections.md`

## File Ownership
- **Exclusive Write Ownership**: `docs/rea-dce-pro/TRUTH_MATRIX.md` and metadata in `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1\`.
- **Strictly Read-Only**: `C:\Dce Pro\` (never modify any file in `C:\Dce Pro`).

## Mission
Generate and publish `docs/rea-dce-pro/TRUTH_MATRIX.md` combining all 602 endpoints (311 AutoCAD commands, 73 Excel Ribbon actions, 49 Steel functions, 29 MBKC functions, 69 native C++ exports, 24 Revit commands, 11 Jigs, 12 UI Forms, and 24 AutoStr steps) and static engineering assets cross-reference.
Every endpoint must have: Legacy Identifier, Subsystem, Token/RVA, Inspection Result, HPDcePro Target File, and Parity Status (`HOÀN THÀNH`, `ĐANG LÀ MOCK`, `CHƯA GIẢI MÃ ĐƯỢC`).


## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## 2026-10-18T06:04:00Z
You are Worker M1 for Milestone M1 (Master Truth Matrix Generation).
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1\
Read your dispatch specification at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1\DISPATCH.md
Read the authoritative user request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (`## 2026-10-18T05:27:06Z`).
Read the project specification at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_25\PROJECT.md.

Read input findings from:
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_1\handoff.md`
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_1\PROPOSED_TRUTH_MATRIX.md`
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m1_1\build_truth_matrix_engine.py`
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\spec_miner_m1_2\handoff.md`
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\spec_miner_m1_3\truth_matrix_sections.md`

Your exclusive file write ownership is: `docs/rea-dce-pro/TRUTH_MATRIX.md`.
Strictly read-only on `C:\Dce Pro`.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Mission:
Generate and write `docs/rea-dce-pro/TRUTH_MATRIX.md`.
Ensure it incorporates 100% of:
- 311 AutoCAD commands
- 73 Excel Ribbon actions
- 49 DceSteel functions
- 29 MBKC functions
- 69 native C++ exports
- 24 Revit commands (23 external commands + 1 application)
- 11 interactive CAD Jigs
- 12 specialized UI forms & palettes
- 24 AutoStr steps
- Section 11: Static engineering assets cross-reference (17 DWGs, 132 XLSMs, mauhoc.db 8.98 MB, 178 System files, 8 Language dicts, 133 Icons, 7 CUIX)
- Section 12: Technical blocker explanation for CHƯA GIẢI MÃ ĐƯỢC (Babel .NET JIT decryption hooks, out-of-bounds GUID heap, clean-room omission rules)
- Parity classification on every row (`HOÀN THÀNH`, `ĐANG LÀ MOCK` with exact code locations, `CHƯA GIẢI MÃ ĐƯỢC` with token/RVA and technical reason).

Verify markdown integrity, line counts, and tables. Write your handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m1\handoff.md`.
Communicate your completion back to parent via send_message.

