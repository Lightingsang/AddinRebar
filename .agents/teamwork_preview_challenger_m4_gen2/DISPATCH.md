# DISPATCH: Challenger M4 Gen 2 (HPTekla Live Harness Adversarial Verification)

## 2026-09-21T19:16:21Z
## Assignment
- **Agent**: `teamwork_preview_challenger_m4_gen2`
- **Role**: Challenger M4 Gen 2 (Adversarial Verification of Harness Remediation)
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m4_gen2`
- **Parent / Orchestrator**: `5d7560ee-5142-428f-a172-e73cf7738ac1`
- **Original Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)
- **Worker Handoff**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m4_gen2\handoff.md`
- **Challenger Defect Report**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m4_2\handoff.md`

## Objective
Adversarially challenge the remediated live harness:
1. Challenge invalid stage inputs:
   - `python HPTekla/tools/harness/live-verify.py --exe HPTekla\HPTekla.Mcp.Server\bin\Debug\net10.0\HPTekla.Mcp.Server.exe --stages Z` -> must print error and exit with code 2.
   - Test empty stages `--stages " "` -> must exit code 2.
   - Test mixed invalid `--stages A,Z` -> must exit code 2.
   - Test lowercase `--stages a,b` -> must normalize and succeed (exit code 0).
2. Challenge `--json` flag:
   - Run with `--json` -> must succeed and emit valid JSON output without `argparse` rejection.
3. Challenge PowerShell `-WhatIf` and error propagation:
   - Run `run-live-verify.ps1 -WhatIf` -> must print dry-run message and exit 0 without spawning python or server.
   - Run `run-live-verify.ps1 -Stages Z` -> must fail validation and NOT print `[SUCCESS]`.
4. Challenge Stage F test IDs:
   - Run `--stages F` in detached mode -> verify test IDs are `F1` beam, `F2` get_part_properties, `F3` create_rebar_group, `F4` export_ifc.
5. Challenge full suite:
   - Run `run-live-verify.ps1` -> 13 passed, 4 skipped (detached), exit 0.
6. Output report to `.agents/teamwork_preview_challenger_m4_gen2/report.md` and handoff with unambiguous verdict (`APPROVE` or `REQUEST_CHANGES`) to `.agents/teamwork_preview_challenger_m4_gen2/handoff.md`.
7. Send a message to the orchestrator (caller) with a concise summary and path to your handoff when done.
