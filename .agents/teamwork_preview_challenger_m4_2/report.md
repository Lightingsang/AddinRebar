# Adversarial Challenge Report: HPTekla Live Verification Harness

**Target Under Review:** `HPTekla/tools/harness/live-verify.py` and `HPTekla/tools/harness/run-live-verify.ps1`  
**Challenger Agent:** `teamwork_preview_challenger_m4_2`  
**Date:** 2026-09-22  
**Overall Risk Assessment:** **MEDIUM**  

---

## Challenge Summary

The HPTekla live harness (`live-verify.py` and `run-live-verify.ps1`) was subjected to empirical adversarial stress testing across CLI parsing, stage selection, error conditions, invalid arguments, JSON output schema conformity, and PowerShell wrapper execution.

While the happy path and detached bridge mode are well implemented (passing 13 checks and cleanly skipping 4 checks without false failures), **two critical edge-case flaws** and **two quality/usability defects** were discovered through direct test execution:

1. **Defect 1 (Dispatch Requirement Violation / False Positive on Invalid Stage):** Passing an invalid stage parameter (e.g., `--stages Z`, `--stages UNKNOWN`, or `--stages "   "`) is completely unvalidated. It executes 0 tests and returns **exit code 0** with a JSON summary claiming success (`{"stages": "Z", "passed": 0, "skipped": 0, "total": 0, "failed": [], "skippedNames": []}`). The dispatch requirement explicitly mandated: *`Execute with invalid stages flag (e.g. --stages Z) -> assert clean exit code 2 or error message.`*
2. **Defect 2 (Missing CLI Argument):** Passing `--json` as requested in dispatch causes argparse to crash with `unrecognized arguments: --json` (exit code 2), as the flag was omitted from `live-verify.py`'s argument definitions.
3. **Defect 3 (PowerShell `-WhatIf` / `-Stages` Passthrough Flaw):** `run-live-verify.ps1` lacks `[CmdletBinding(SupportsShouldProcess)]` and stage validation. Passing `-WhatIf` executes the full live harness rather than performing a dry run, and passing `-Stages Z` outputs `[SUCCESS] All requested stages passed!` with exit code 0.
4. **Defect 4 (Stage F Test Numbering Inconsistency):** Test numbering for Stage F swaps `F2` and `F3` between detached skip mode and connected execution mode.

Because Defect 1 and Defect 2 directly violate explicit dispatch assertions and introduce dangerous false positives in CI/CD, the recommendation is **`REQUEST_CHANGES`**.

---

## Challenges

### [High] Challenge 1: Silent False-Positive Success on Invalid Stages Flag

- **Assumption challenged:** Harness assumes any user input in `--stages` is valid and that unhandled stage letters require no validation or error reporting.
- **Attack scenario:**
  A developer or automated CI job specifies a typo or invalid stage:
  ```powershell
  python HPTekla/tools/harness/live-verify.py --exe HPTekla\HPTekla.Mcp.Server\bin\Debug\net10.0\HPTekla.Mcp.Server.exe --stages Z
  ```
  Or:
  ```powershell
  powershell -ExecutionPolicy Bypass -File HPTekla/tools/harness/run-live-verify.ps1 -Stages Z
  ```
- **Blast radius:**
  The command initializes the MCP server, runs 0 test checks, prints:
  `{"stages": "Z", "passed": 0, "skipped": 0, "total": 0, "failed": [], "skippedNames": []}`
  And exits with **code 0**!
  The PowerShell wrapper prints `[SUCCESS] All requested stages passed!`.
  If a CI pipeline intends to run `--stages A,B,C,D,E,F` but has a typo like `--stages A,B,C,D,E,G`, stage G is silently ignored without warning or error, and the suite passes with exit code 0.
- **Mitigation:**
  In `HPTekla/tools/harness/live-verify.py`, validate parsed stages against an explicit allowed set:
  ```python
  VALID_STAGES = {"A", "B", "C", "D", "E", "F"}
  stages = [s.strip().upper() for s in args.stages.split(",") if s.strip()]
  invalid_stages = [s for s in stages if s not in VALID_STAGES]
  if invalid_stages:
      parser.error(f"Invalid stage(s): {', '.join(invalid_stages)}. Allowed stages: A, B, C, D, E, F")
  if not stages:
      parser.error("No valid stages specified.")
  ```
  In `HPTekla/tools/harness/run-live-verify.ps1`:
  Add validation pattern or check:
  ```powershell
  [ValidatePattern('^[A-Fa-f,\s]+$')][string]$Stages = 'A,B,C,D,E,F',
  ```

---

### [Medium] Challenge 2: Missing `--json` Flag in CLI Parser

- **Assumption challenged:** Harness assumes JSON output only needs to be printed to stdout via `Checklist.finish()` or saved via `--out <dir>`.
- **Attack scenario:**
  Invoking per dispatch specification:
  ```powershell
  python HPTekla/tools/harness/live-verify.py --exe HPTekla\HPTekla.Mcp.Server\bin\Debug\net10.0\HPTekla.Mcp.Server.exe --json
  ```
- **Blast radius:**
  Argparse terminates execution immediately:
  `live-verify.py: error: unrecognized arguments: --json` (exit code 2).
- **Mitigation:**
  Add `--json` flag to `argparse`:
  ```python
  parser.add_argument("--json", action="store_true", help="Emit summary JSON to stdout (default behavior)")
  ```

---

### [Medium] Challenge 3: Lack of `[CmdletBinding()]` and `-WhatIf` Support in PowerShell Wrapper

- **Assumption challenged:** `run-live-verify.ps1` supports PowerShell standard `-WhatIf` dry-run switch.
- **Attack scenario:**
  Invoking:
  ```powershell
  powershell -ExecutionPolicy Bypass -Command "& 'HPTekla/tools/harness/run-live-verify.ps1' -WhatIf"
  ```
- **Blast radius:**
  Because the script lacks `[CmdletBinding(SupportsShouldProcess)]`, `-WhatIf` is placed into uninspected `$args`. The script ignores it and runs the actual test suite against the live executable.
- **Mitigation:**
  Add `[CmdletBinding(SupportsShouldProcess)]` to `run-live-verify.ps1`, and if `$PSCmdlet.ShouldProcess("Server: $Exe", "Execute live verification")` is false, print the execution plan and exit 0 without launching Python.

---

### [Low] Challenge 4: Inconsistent Test Identifier Numbering in Stage F

- **Assumption challenged:** Test IDs in `live-verify.py` uniquely and consistently identify the same test scenario regardless of whether the bridge is connected or detached.
- **Attack scenario:**
  In detached mode (lines 146-149):
  - `F1`: `create_column real mutation`
  - `F2`: `create_rebar_group reinforcement creation`
  - `F3`: `get_part_properties verification`
  - `F4`: `get_reinforcement_info verification`
  In connected mode (lines 160-191):
  - `F1`: `create_column real mutation succeeds`
  - `F2`: `get_part_properties reads created part attributes`
  - `F3`: `create_rebar_group creates reinforcement group on host part`
  - `F4`: `get_reinforcement_info lists reinforcement on host part`
- **Blast radius:**
  Test reporting tooling comparing test outcomes between detached and live runs will see `F2` and `F3` swapped.
- **Mitigation:**
  Align test numbering so `F2` is `get_part_properties` and `F3` is `create_rebar_group` across both detached skips and live checks.

---

## Stress Test Results

| Test Scenario | Command Line | Expected Behavior | Actual Behavior | Result |
|---|---|---|---|---|
| **CLI Help** | `python live-verify.py --help` | Exit code 0, displays usage options | Exit code 0, displays `--exe`, `--stages`, `--out` | **PASS** |
| **Missing Exe Arg** | `python live-verify.py` | Exit code 2, error: `--exe` required | Exit code 2, error: the following arguments are required: --exe | **PASS** |
| **Non-existent Exe Path** | `python live-verify.py --exe non_existent.exe --stages A` | Non-zero exit code, error message | Exit code 1, `FileNotFoundError` | **PASS** |
| **Stage A Run** | `python live-verify.py --exe ... --stages A` | 4 tests pass, exit code 0 | 4 passed, 0 failed, exit code 0 | **PASS** |
| **Stages A,B,C,D Run** | `python live-verify.py --exe ... --stages A,B,C,D` | 11 tests pass, exit code 0 (detached mode) | 11 passed, 0 failed, exit code 0 | **PASS** |
| **Stage E Run** | `python live-verify.py --exe ... --stages E` | 2 dryRun tests pass, exit code 0 | 2 passed, 0 failed, exit code 0 | **PASS** |
| **Stage F Run (Detached)** | `python live-verify.py --exe ... --stages F` | 4 tests skipped, exit code 0 | 4 skipped, 0 failed, exit code 0 | **PASS** |
| **Case Insensitive Stages** | `python live-verify.py --exe ... --stages a,b` | Parses `A,B`, 7 tests pass, exit code 0 | 7 passed, exit code 0 | **PASS** |
| **JSON Output to File** | `python live-verify.py --exe ... --stages A --out <dir>` | Writes valid JSON summary matching `harness_common.py` | Created `summary-tekla.json` strictly adhering to schema | **PASS** |
| **Invalid Stage Flag** | `python live-verify.py --exe ... --stages Z` | **Exit code 2 or error message** (dispatch requirement) | **Exit code 0, 0 tests run, reports success** | **FAIL** |
| **Whitespace Stage Flag** | `python live-verify.py --exe ... --stages "   "` | Exit code 2 or error message | Exit code 0, 0 tests run | **FAIL** |
| **Dispatch `--json` Flag** | `python live-verify.py --exe ... --stages A --json` | Valid flag or JSON summary | Exit code 2 (`unrecognized arguments: --json`) | **FAIL** |
| **PowerShell Detached Mode** | `run-live-verify.ps1` | Detects detached state, 13 passed, 4 skipped, exit code 0 | 13 passed, 4 skipped, exit code 0, `[SUCCESS]` | **PASS** |
| **PowerShell Invalid Exe** | `run-live-verify.ps1 -Exe nonexistent.exe -SkipBuild` | Script throws error, exit code 1 | Script throws error, exit code 1 | **PASS** |
| **PowerShell `-WhatIf`** | `run-live-verify.ps1 -WhatIf` | Dry run without execution | Unhandled, executes live test run | **FAIL** |
| **PowerShell `-Stages Z`** | `run-live-verify.ps1 -Stages Z` | Validation error, non-zero exit | Prints `[SUCCESS]`, exit code 0 | **FAIL** |

---

## Unchallenged Areas

- **Attached Live Model Mutation (Stage F active mutation):** Tekla Structures 2025 GUI was detected running (PID 30316) but the `hptekla-mcp-2025` bridge plugin was not actively loaded in the editor session. Therefore Stage F real entity creation could not be executed live against the Tekla database. The harness's skip logic was verified empirically instead.
- **Resource Exhaustion / OOM / Process Kill during Pipe Wait:** Not simulated as `mcp-session.py` pumps in a separate thread with queue timeouts, which is shared across all HP MCP harnesses.
