# DISPATCH: Reviewer M4 Gen 2 (HPTekla Live Harness Remediation Review)

## Assignment
- **Agent**: `teamwork_preview_reviewer_m4_gen2`
- **Role**: Reviewer M4 Gen 2 (HPTekla Live Harness Remediation)
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_reviewer_m4_gen2`
- **Parent / Orchestrator**: `5d7560ee-5142-428f-a172-e73cf7738ac1`
- **Original Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)
- **Worker Handoff**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m4_gen2\handoff.md`
- **Challenger Defect Report**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m4_2\handoff.md`

## Objective
Review the 4 defect fixes in `HPTekla/tools/harness/live-verify.py` and `HPTekla/tools/harness/run-live-verify.ps1`:
1. Verify `--stages` validation in `live-verify.py` (checks `VALID_STAGES`, calls `parser.error(...)` on invalid or empty stages).
2. Verify `--json` CLI option is present in `argparse`.
3. Verify `run-live-verify.ps1` supports `-WhatIf` via `[CmdletBinding(SupportsShouldProcess)]`, validates `$Stages`, and correctly catches non-zero exit codes from python.
4. Verify Stage F test identifiers are synchronized between detached skip mode and connected execution mode (`F1` beam, `F2` get_part_properties, `F3` create_rebar_group, `F4` export_ifc).
5. Verify that existing test suites continue to pass without regression.
6. Write your report to `.agents/teamwork_preview_reviewer_m4_gen2/report.md` and handoff with unambiguous verdict (`APPROVE` or `REQUEST_CHANGES`) to `.agents/teamwork_preview_reviewer_m4_gen2/handoff.md`.
7. Send a message to the orchestrator (caller) with a concise summary and path to your handoff when done.
## 2026-09-21T19:16:21Z
<USER_REQUEST>
You are teamwork_preview_reviewer_m4_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_reviewer_m4_gen2
Your dispatch assignment is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_reviewer_m4_gen2\DISPATCH.md
The authoritative user request is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md under header ## 2026-09-21T17:20:33Z.
Worker handoff report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m4_gen2\handoff.md
Challenger defect report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m4_2\handoff.md
Repo root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

Objective:
Review the 4 defect fixes in `HPTekla/tools/harness/live-verify.py` and `HPTekla/tools/harness/run-live-verify.ps1`:
1. Verify `--stages` validation in `live-verify.py` (checks `VALID_STAGES`, calls `parser.error(...)` on invalid or empty stages).
2. Verify `--json` CLI option is present in `argparse`.
3. Verify `run-live-verify.ps1` supports `-WhatIf` via `[CmdletBinding(SupportsShouldProcess)]`, validates `$Stages`, and correctly catches non-zero exit codes from python.
4. Verify Stage F test identifiers are synchronized between detached skip mode and connected execution mode (`F1` beam, `F2` get_part_properties, `F3` create_rebar_group, `F4` export_ifc).
5. Verify that existing test suites continue to pass without regression.
6. Write your report to `.agents/teamwork_preview_reviewer_m4_gen2/report.md` and handoff with unambiguous verdict (`APPROVE` or `REQUEST_CHANGES`) to `.agents/teamwork_preview_reviewer_m4_gen2/handoff.md`.
7. Send a message to the orchestrator (caller) with a concise summary and path to your handoff when done.
</USER_REQUEST>
