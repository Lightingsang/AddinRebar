# Dispatch Assignment: Milestone 4 Reviewer 2 — Live Verification Harness Audit

## Role: Reviewer (teamwork_preview_reviewer)
## Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_reviewer_m4_2`
## Authoritative Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)
## Project Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\orchestrator_8\PROJECT.md`
## Worker M4 Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m4\handoff.md`

---

## Review Scope
Audit the Python and PowerShell live verification harness in `HPTekla/tools/harness/`:
1. `live-verify.py`:
   - Valid import of `McpShared/tools/harness_common.py`.
   - Implementation of Stages A-F:
     - Stage A: Handshake & Stdio Pipe (`initialize`, `tools/list` 24 tools)
     - Stage B: Context & Resources (`get_tekla_context`, `tekla://model/info`, `tekla://selection`)
     - Stage C: Reflection & Metadata (`inspect_type` on `Beam`)
     - Stage D: Read-only Seeds (`get_model_info`, `select_objects`, `list_drawings`)
     - Stage E: Dry-Run Write Verification (`create_beam`, `create_column` with `dryRun = true`)
     - Stage F: Real Mutation & Reinforcement (`create_column`, `create_rebar_group`, `get_part_properties`, `get_reinforcement_info`)
   - Graceful skip behavior when bridge is not connected.
2. `run-live-verify.ps1`:
   - Process detection (`tekla.exe` / `TeklaStructures.exe`).
   - Pipe existence check (`\\.\pipe\hptekla-mcp-2025`).
   - Automated invocation of `live-verify.py`.
3. Run the harness:
   ```bash
   powershell -ExecutionPolicy Bypass -File HPTekla/tools/harness/run-live-verify.ps1
   ```
   Verify 13 passed, 4 skipped (detached), 0 failed, exit code 0.

## Verdict Requirement
Deliver an unambiguous verdict in your `handoff.md`:
- `APPROVE` if the harness adheres to repo standards and runs cleanly.
- `REQUEST_CHANGES` if defects are found.
Send a message to the orchestrator upon completion.

## 2026-09-21T19:04:42Z
You are teamwork_preview_reviewer_m4_2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_reviewer_m4_2
Your dispatch assignment is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_reviewer_m4_2\DISPATCH.md
The authoritative user request is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md under header ## 2026-09-21T17:20:33Z.
Worker M4 handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m4\handoff.md
Repo root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

Objective:
Audit Python and PowerShell live harness in HPTekla/tools/harness/ (live-verify.py and run-live-verify.ps1) per DISPATCH.md.
Run the harness and verify stage execution (A-F).
Write report to `.agents/teamwork_preview_reviewer_m4_2/report.md` and handoff to `.agents/teamwork_preview_reviewer_m4_2/handoff.md`.
Deliver an unambiguous APPROVE or REQUEST_CHANGES verdict.
Send a message to the orchestrator (caller) when done.

