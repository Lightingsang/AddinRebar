# DISPATCH: Worker M4 Gen 2 (HPTekla Live Harness Remediation)

## Assignment
- **Agent**: `teamwork_preview_worker_m4_gen2`
- **Role**: Worker M4 Gen 2 (Live Harness Remediation)
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m4_gen2`
- **Parent / Orchestrator**: `5d7560ee-5142-428f-a172-e73cf7738ac1`
- **Original Request**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md` (header `## 2026-09-21T17:20:33Z`)

## Mandatory Integrity Warning
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Write Ownership
You have exclusive write access to:
- `HPTekla/tools/harness/live-verify.py`
- `HPTekla/tools/harness/run-live-verify.ps1`
- `.agents/teamwork_preview_worker_m4_gen2/**`

## Specific Remediation Tasks (Addressing Challenger 2 REQUEST_CHANGES)
Review `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m4_2\handoff.md`:

1. **Defect 1: False-Positive Success on Invalid Stages (`live-verify.py`)**:
   - Currently, invalid stages (e.g. `--stages Z`) are silently ignored, 0 checks execute, and the script exits with code `0`.
   - Implement strict stage validation in `live-verify.py`:
     ```python
     VALID_STAGES = {"A", "B", "C", "D", "E", "F"}
     stages = [s.strip().upper() for s in args.stages.split(",") if s.strip()]
     invalid = [s for s in stages if s not in VALID_STAGES]
     if invalid:
         parser.error(f"Invalid stage(s): {', '.join(invalid)}. Allowed stages: A, B, C, D, E, F")
     if not stages:
         parser.error("No stages specified.")
     ```
   - Ensure `parser.error(...)` is invoked when an invalid stage is provided, which outputs to stderr and exits with exit code `2`.

2. **Defect 2: Missing `--json` CLI Option in `live-verify.py`**:
   - Add `--json` argument to `argparse`:
     ```python
     parser.add_argument("--json", action="store_true", help="Emit summary JSON to stdout")
     ```
   - Ensure running `python live-verify.py ... --json` does not error out with `unrecognized arguments: --json`.

3. **Defect 3: PowerShell Parameter Validation & `-WhatIf` (`run-live-verify.ps1`)**:
   - Add `[CmdletBinding(SupportsShouldProcess)]` to `run-live-verify.ps1`.
   - If `$PSCmdlet.ShouldProcess(...)` is false (due to `-WhatIf`), print what would be run and return cleanly without invoking the python process.
   - Validate `$Stages` or check the exit code of `python live-verify.py` so that if `live-verify.py` fails (exit code 2 on invalid stage), `run-live-verify.ps1` catches it and exits with code 2 / non-zero error, rather than reporting `[SUCCESS]`.

4. **Defect 4: Stage F Test ID Inconsistency (`live-verify.py`)**:
   - In `live-verify.py`, ensure test IDs and names are identical in both detached skip mode and live execution mode:
     - `F1`: `create_beam`
     - `F2`: `get_part_properties`
     - `F3`: `create_rebar_group`
     - `F4`: `export_ifc`

5. **Empirical Verification**:
   - Test invalid stages: `python HPTekla/tools/harness/live-verify.py --exe HPTekla\HPTekla.Mcp.Server\bin\Debug\net10.0\HPTekla.Mcp.Server.exe --stages Z` -> assert exit code 2.
   - Test valid stage with `--json`: `python HPTekla/tools/harness/live-verify.py --exe HPTekla\HPTekla.Mcp.Server\bin\Debug\net10.0\HPTekla.Mcp.Server.exe --stages A --json` -> assert exit code 0.
   - Test PowerShell wrapper:
     - `powershell -ExecutionPolicy Bypass -File HPTekla/tools/harness/run-live-verify.ps1 -Stages A` -> [SUCCESS], exit 0.
     - `powershell -ExecutionPolicy Bypass -File HPTekla/tools/harness/run-live-verify.ps1 -Stages Z` -> [FAILURE], exit 2 or non-zero.
     - `powershell -ExecutionPolicy Bypass -File HPTekla/tools/harness/run-live-verify.ps1 -WhatIf` -> dry run, no process spawned.
   - Test full suite: `powershell -ExecutionPolicy Bypass -File HPTekla/tools/harness/run-live-verify.ps1` -> 13 passed, 4 skipped (detached), exit 0.

6. **Reporting & Handoff**:
   - Write full report to `.agents/teamwork_preview_worker_m4_gen2/report.md`.
   - Write 5-component handoff to `.agents/teamwork_preview_worker_m4_gen2/handoff.md`.
   - Send completion message to parent orchestrator.

## 2026-09-21T19:10:49Z
You are teamwork_preview_worker_m4_gen2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m4_gen2
Your dispatch assignment is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_worker_m4_gen2\DISPATCH.md
The authoritative user request is in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md under header ## 2026-09-21T17:20:33Z.
Challenger handoff report with defects: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m4_2\handoff.md
Repo root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

Objective:
Remediate the 4 defects in the HPTekla live harness identified by Challenger 2:
1. `live-verify.py`: Add strict validation for `--stages`. Unrecognized stages (e.g. `--stages Z`) must call `parser.error(...)` to print an error and exit with code 2.
2. `live-verify.py`: Add `--json` CLI argument to `argparse`.
3. `run-live-verify.ps1`: Add `[CmdletBinding(SupportsShouldProcess)]` to support `-WhatIf`. Ensure non-zero exit codes from `live-verify.py` propagate as errors and don't print `[SUCCESS]`.
4. `live-verify.py`: Align test IDs in Stage F between detached skip mode and connected execution mode (`F1` beam, `F2` get_part_properties, `F3` create_rebar_group, `F4` export_ifc).
5. Empirically verify all 4 defect fixes using PowerShell and python commands.
6. Write your report to `.agents/teamwork_preview_worker_m4_gen2/report.md` and handoff to `.agents/teamwork_preview_worker_m4_gen2/handoff.md`.
7. Send a message to the orchestrator (caller) with a concise summary and path to your handoff when done.
