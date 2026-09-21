# Dispatch Assignment: Milestone 4 Challenger 2 — Live Harness Stress Testing

## Role: Challenger (teamwork_preview_challenger)
## Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m4_2`
## Authoritative Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)
## Project Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_8\PROJECT.md`
## Worker M4 Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m4\handoff.md`

---

## Challenge Scope
Adversarially challenge the Python and PowerShell live harness in `HPTekla/tools/harness/`:
1. Execute `python HPTekla/tools/harness/live-verify.py --help` and verify CLI options.
2. Execute individual stages:
   - `python HPTekla/tools/harness/live-verify.py --stages A`
   - `python HPTekla/tools/harness/live-verify.py --stages A,B,C,D`
   - `python HPTekla/tools/harness/live-verify.py --stages E`
3. Stress test error conditions:
   - Execute with invalid stages flag (e.g. `--stages Z`) -> assert clean exit code 2 or error message.
   - Verify that JSON output emitted by `--json` is strictly valid JSON conforming to `harness_common.py`.
4. Verify execution of `run-live-verify.ps1` with `-WhatIf` or detached bridge mode.

## Verdict Requirement
Deliver an unambiguous verdict in your `handoff.md`:
- `APPROVE` if the live harness handles all inputs cleanly and conforms to repo harness standards.
- `REQUEST_CHANGES` if defects or unhandled exceptions occur.
Send a message to the orchestrator upon completion.
