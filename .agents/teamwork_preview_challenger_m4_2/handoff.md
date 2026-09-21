# Handoff Report: Milestone 4 Challenger 2 — Live Harness Stress Testing

**Challenger:** `teamwork_preview_challenger_m4_2`  
**Milestone:** Milestone 4 (Live Verification Harness Review & Adversarial Stress Testing)  
**Parent / Orchestrator:** `5d7560ee-5142-428f-a172-e73cf7738ac1` (`parent`)  
**Verdict:** **REQUEST_CHANGES**  

---

## 1. Observation

1. **Happy Path Execution (`live-verify.py`)**:
   - `python HPTekla/tools/harness/live-verify.py --exe HPTekla\HPTekla.Mcp.Server\bin\Debug\net10.0\HPTekla.Mcp.Server.exe --stages A`:
     ```
     HPTekla MCP Server initialized (PID: 37204, Info: {'name': 'HPTekla MCP', 'version': '1.0.0'})
     --- Running Stage A: Handshake & Stdio Pipe ---
     PASS A1 Server advertises exactly 24 tools total Tools count = 24
     PASS A2 All 4 core tools present Core: ['execute_tekla_code', 'get_tekla_context', 'inspect_type', 'cancel_execution']
     PASS A3 All 8 registry meta tools present Meta: ['search_tools', 'get_tool', 'run_tool', 'get_run', 'propose_tool', 'test_tool', 'publish_tool', 'manage_tool']
     PASS A4 All 12 embedded seed tools present Seeds: ['get_model_info', 'select_objects', 'get_part_properties', 'create_beam', 'create_column', 'create_contour_plate', 'create_rebar_group', 'create_single_rebar', 'modify_user_properties', 'get_reinforcement_info', 'list_drawings', 'export_ifc']
     {"stages": "A", "passed": 4, "skipped": 0, "total": 4, "failed": [], "skippedNames": []}
     ```
     Exit code: `0`.
   - `--stages A,B,C,D`: 11 passed, 0 failed, 0 skipped, exit code `0`.
   - `--stages E`: 2 passed (dry-run creation checks), 0 failed, 0 skipped, exit code `0`.
   - `--stages F`: 0 passed, 4 skipped (detached state identified), exit code `0`.
   - Full run (`A,B,C,D,E,F`): 13 passed, 4 skipped, 0 failed, exit code `0`.

2. **Observation of Invalid `--stages` Flag (`live-verify.py:206`)**:
   - Command: `python HPTekla/tools/harness/live-verify.py --exe HPTekla\HPTekla.Mcp.Server\bin\Debug\net10.0\HPTekla.Mcp.Server.exe --stages Z; echo "Exit code: $LASTEXITCODE"`
   - Output:
     ```
     HPTekla MCP Server initialized (PID: 32240, Info: {'name': 'HPTekla MCP', 'version': '1.0.0'})
     {"stages": "Z", "passed": 0, "skipped": 0, "total": 0, "failed": [], "skippedNames": []}
     Exit code: 0
     ```
   - In `HPTekla/tools/harness/live-verify.py` line 206:
     ```python
     stages = [s.strip().upper() for s in args.stages.split(",") if s.strip()]
     ```
     There is no validation against the allowed set `{'A', 'B', 'C', 'D', 'E', 'F'}`. Unrecognized stages are silently omitted, executing 0 tests, calling `CL.finish()`, which evaluates `not summary["failed"]` as True and returns exit code `0`.

3. **Observation of `--json` Flag (`live-verify.py:199-203`)**:
   - Command: `python HPTekla/tools/harness/live-verify.py --exe HPTekla\HPTekla.Mcp.Server\bin\Debug\net10.0\HPTekla.Mcp.Server.exe --stages A --json`
   - Output:
     ```
     usage: live-verify.py [-h] --exe EXE [--stages STAGES] [--out OUT]
     live-verify.py: error: unrecognized arguments: --json
     ```
     Exit code: `2`.
   - Note: JSON output is printed unconditionally by `harness_common.py` to stdout and saved via `--out <dir>`, but the explicit flag `--json` mandated in dispatch causes an `argparse` rejection.

4. **Observation of PowerShell Wrapper (`run-live-verify.ps1:2-7`)**:
   - Command: `powershell -ExecutionPolicy Bypass -Command "& 'HPTekla/tools/harness/run-live-verify.ps1' -Stages Z; echo 'Script exit code:' $LASTEXITCODE"`
   - Output:
     ```
     ==========================================================
     HPTekla MCP Live Verification Harness (Tekla Structures 2025)
     Server Exe: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPTekla\HPTekla.Mcp.Server\bin\Debug\net10.0\HPTekla.Mcp.Server.exe
     Stages:     Z
     ==========================================================
     ...
     {"stages": "Z", "passed": 0, "skipped": 0, "total": 0, "failed": [], "skippedNames": []}
     [SUCCESS] All requested stages passed!
     Script exit code: 0
     ```
   - Command with `-WhatIf`: `powershell -ExecutionPolicy Bypass -Command "& 'HPTekla/tools/harness/run-live-verify.ps1' -WhatIf"`
   - Output: Executes the full live verification harness instead of doing a WhatIf dry run because `run-live-verify.ps1` lacks `[CmdletBinding(SupportsShouldProcess)]`.

5. **Observation of Test ID Inconsistency in Stage F**:
   - In `HPTekla/tools/harness/live-verify.py`:
     - Lines 147-148 (detached mode skip):
       `skip("F2 create_rebar_group reinforcement creation", ...)`
       `skip("F3 get_part_properties verification", ...)`
     - Lines 167, 187 (live execution mode):
       `check("F2 get_part_properties reads created part attributes", ...)`
       `check("F3 create_rebar_group creates reinforcement group on host part", ...)`
     `F2` and `F3` test names and purposes are swapped.

---

## 2. Logic Chain

1. **Logic for Defect 1 (Invalid Stage False Positive)**:
   - The dispatch specification explicitly required: *"Execute with invalid stages flag (e.g. `--stages Z`) -> assert clean exit code 2 or error message."* (DISPATCH.md, line 19).
   - In Observation 2, `--stages Z` was executed against the real server. It initialized the server, did not run any test stages, printed `{"stages": "Z", "passed": 0, ...}`, and exited with code `0`.
   - In automated testing, an invalid stage parameter must fail fast with exit code 2 or an error message to prevent typos from silently skipping test coverage. Therefore, this is an empirical defect and violates the dispatch acceptance criteria.

2. **Logic for Defect 2 (`--json` Flag)**:
   - The dispatch required: *"Verify that JSON output emitted by `--json` is strictly valid JSON conforming to `harness_common.py`."* (DISPATCH.md, line 20).
   - In Observation 3, passing `--json` caused `argparse` to crash with exit code 2 (`unrecognized arguments: --json`).
   - While the harness always emits JSON at the end of the run and writes JSON when `--out` is specified, the missing CLI flag violates the interface expectation.

3. **Logic for Defect 3 (`run-live-verify.ps1` parameter handling)**:
   - In Observation 4, `run-live-verify.ps1` has neither parameter validation on `$Stages` nor `SupportsShouldProcess` for `-WhatIf`.
   - Passing `-Stages Z` results in the PowerShell script reporting `[SUCCESS] All requested stages passed!` with exit code 0.
   - Passing `-WhatIf` does not prevent execution; the script launches the real server process and executes live tests.

4. **Logic for Defect 4 (Stage F Test Numbering Inconsistency)**:
   - In Observation 5, tests F2 and F3 have divergent meanings depending on whether the bridge is connected or detached. This breaks automated test result aggregators that track test cases by identifier.

---

## 3. Caveats

- In detached mode, all 13 standard verification checks across stages A, B, C, D, and E execute cleanly and pass without errors.
- Stage F correctly detects that the bridge named pipe `hptekla-mcp-2025` is not active, cleanly skipping mutation tests.
- When valid stages are provided, `live-verify.py` and `run-live-verify.ps1` conform to the repository conventions for HP MCP live harnesses.
- These defects can be addressed with minimal surgical changes to `live-verify.py` and `run-live-verify.ps1` without modifying the core MCP server or bridge C# code.

---

## 4. Conclusion

**Verdict: `REQUEST_CHANGES`**

The live verification harness exhibits a critical validation defect where invalid stage arguments (`--stages Z`) silently succeed with exit code 0 (violating explicit dispatch criteria), lacks the requested `--json` CLI option, fails to handle PowerShell `-WhatIf`, and contains swapped test IDs in Stage F.

### Actionable Remediation Items for Worker:
1. **Validate `--stages` in `live-verify.py`**:
   Add stage validation rejecting unrecognized stage letters with `parser.error(...)` (which outputs error message and exits code 2):
   ```python
   VALID_STAGES = {"A", "B", "C", "D", "E", "F"}
   stages = [s.strip().upper() for s in args.stages.split(",") if s.strip()]
   invalid = [s for s in stages if s not in VALID_STAGES]
   if invalid:
       parser.error(f"Invalid stage(s): {', '.join(invalid)}. Allowed stages: A, B, C, D, E, F")
   if not stages:
       parser.error("No stages specified.")
   ```
2. **Add `--json` flag to `live-verify.py`**:
   ```python
   parser.add_argument("--json", action="store_true", help="Emit summary JSON to stdout")
   ```
3. **Enhance `run-live-verify.ps1`**:
   - Add `[CmdletBinding(SupportsShouldProcess)]` to support `-WhatIf`.
   - Validate `$Stages` with `[ValidatePattern('^[A-Fa-f,\s]+$')]` or throw an error if an invalid stage is passed.
4. **Fix Stage F Test Numbering in `live-verify.py`**:
   Ensure `F2` is `get_part_properties` and `F3` is `create_rebar_group` in both detached skip mode and connected execution mode.

---

## 5. Verification Method

To independently reproduce and verify the findings:

1. **Verify invalid stages failure (Defect 1)**:
   ```powershell
   python HPTekla/tools/harness/live-verify.py --exe HPTekla\HPTekla.Mcp.Server\bin\Debug\net10.0\HPTekla.Mcp.Server.exe --stages Z; echo "Exit: $LASTEXITCODE"
   ```
   *Current actual result: Prints `{"stages": "Z", "passed": 0, ...}` and exits with code 0.*  
   *Expected after fix: Prints `live-verify.py: error: Invalid stage(s): Z` and exits with code 2.*

2. **Verify `--json` flag rejection (Defect 2)**:
   ```powershell
   python HPTekla/tools/harness/live-verify.py --exe HPTekla\HPTekla.Mcp.Server\bin\Debug\net10.0\HPTekla.Mcp.Server.exe --stages A --json
   ```
   *Current actual result: `error: unrecognized arguments: --json`.*  
   *Expected after fix: Clean execution with exit code 0.*

3. **Verify PowerShell `-WhatIf` / `-Stages Z` (Defect 3)**:
   ```powershell
   powershell -ExecutionPolicy Bypass -File HPTekla/tools/harness/run-live-verify.ps1 -Stages Z
   ```
   *Current actual result: Reports `[SUCCESS] All requested stages passed!` with exit code 0.*  
   *Expected after fix: Rejects stage Z with an error and non-zero exit.*
