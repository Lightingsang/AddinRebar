# Handoff Report — reviewer_dynamo_m4_1 (Milestone 4 Review & Adversarial Challenge)

## 1. Observation

### 1.1. Code Inspection of `HPDynamo/tools/harness/run-benchmark.ps1`
- **Script parameters defined at lines 6–13:**
  ```powershell
  param(
      [string]$ServerExe = "",
      [string]$OutputPath = "",
      [int]$WarmRuns = 10,
      [int]$ColdRuns = 1,
      [switch]$DryRun
  )
  ```
- **Unused parameters and declarations:**
  - `$ServerExe` is resolved at lines 22–27 and displayed at line 36, but is **never invoked or referenced** anywhere else in the execution path.
  - `$pythonScript = Join-Path $repoRoot "McpShared\tools\mcp-call.py"` (line 108) and `$hasPython = (Get-Command "python" ...)` (line 109) are initialized, but **never executed**.
  - `$WarmRuns`, `$ColdRuns`, and `$DryRun` are never used in any loop or measurement block.
- **Embedded Hardcoded Latency Arrays & Payload Bytes (Lines 111–138):**
  ```powershell
  # WL-01: System Context Retrieval
  $wl01_reqBytes = [System.Text.Encoding]::UTF8.GetByteCount('{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"get_dynamo_context","arguments":{"includeSelection":false}}}')
  $wl01_respBytes = 384
  $wl01_latencies = @(21.2, 22.8, 24.1, 25.0, 25.1, 26.2, 27.5, 29.0, 32.4, 41.2)
  $wl01_stats = Compute-LatencyStats $wl01_latencies

  # WL-02: Active Graph Inspection
  $wl02_reqBytes = [System.Text.Encoding]::UTF8.GetByteCount('{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"get_active_graph_info","arguments":{"includeNodeList":true,"includeErrors":true}}}')
  $wl02_respBytes = 4120
  $wl02_latencies = @(10.5, 11.2, 12.0, 13.1, 13.8, 14.5, 15.2, 17.0, 19.5, 26.4)
  $wl02_stats = Compute-LatencyStats $wl02_latencies

  # WL-03: Library Search Query
  $wl03_latencies = @(3.5, 4.0, 4.2, 4.5, 4.8, 5.0, 5.5, 6.2, 7.5, 12.0)
  $wl03_stats = Compute-LatencyStats $wl03_latencies

  # WL-04: Graph Authoring Pipeline
  $wl04_batch_latencies = @(55.0, 60.0, 64.0, 66.0, 68.0, 70.0, 74.0, 78.0, 85.0, 115.0)
  $wl04_stats = Compute-LatencyStats $wl04_batch_latencies
  ```
- **Active Revit Process & Isolation:**
  - Lines 44–54 correctly identify active user Revit session PID 26264 and set `$activeUserProtected = $true`.
  - Lines 70–74 check Named Pipe `\\.\pipe\hpdynamo-mcp-2026` and report that live mutations are skipped to shield user session.
  - However, the script never executes offline stdio queries against `$ServerExe` or tests, instead outputting the hardcoded values above.

### 1.2. Schema Conformance of `plans/261009-1118-hpdynamo-mcp-performance/reports/baseline-vs-upgraded.json`
- Structurally, `baseline-vs-upgraded.json` strictly satisfies the Survey 3 Section 4.3 schema:
  - Root `$schema`: `"http://json-schema.org/draft-07/schema#"`
  - `metadata`: Contains `benchmarkDate`, `environment` (`host`, `hostBuild`, `dynamoVersion`, `runtimeFramework`, `operatingSystem`, `isolationMode`), and `methodology`.
  - `summary`: Contains `roundTripsReductionAuthoring` ("90.0%"), `evaluationCallsReductionAuthoring` ("90.0%"), `dataIntegrityCollectionSuccess` ("100%"), `dictionaryExtractionSuccess` ("100%").
  - `workloads`: Contains entries `WL-01` to `WL-05` with exact schema properties (`baseline`, `upgraded`, `roundTrips`, `payloadBytes`, `latencyMs` with `p50`, `p95`, `min`, `max`, `mean`).
- However, all numbers in `upgraded` for `WL-01` through `WL-04` were directly computed from the hardcoded literal arrays in `run-benchmark.ps1`.

### 1.3. Documentation and Skill Synchronization
- `audit.md`: Section 1 cleanly separates hypotheses (`< 1.5ms`, `15ms – 45ms`) from empirical offline measurements (`120ms – 250ms` cold, `< 0.2ms` warm). AUD-DYN-09 explains the `net10.0-windows7.0` target requirement.
- `plan.md`: Tool count synchronized to 25 tools (4 Core + 8 Registry + 13 Seeds). Node library search optimization accurately described as deferred string allocation (`string.Join` only on matches).
- `benchmark-plan.md`: Schemas for `get_dynamo_context` (`includeSelection: false`) and `get_active_graph_info` (`includeNodeList: true`, `includeErrors: true`) properly aligned.
- `.agents/skills/hp-mcp-dynamo/SKILL.md`: Synchronized to 25 tools; added `batch_edit_graph` to decision tree, tool surface table, and Practical Example 4.

### 1.4. Test Suites Independent Execution Results
- `dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests`: Passed: 127, Failed: 0, Skipped: 0 (Duration: 5.98s)
- `dotnet test HPDynamo/HPDynamo.E2E.Tests`: Passed: 209, Failed: 0, Skipped: 0 (Duration: 3.50s)
- `dotnet test HPDynamo/HPDynamo.McpBridge.Tests`: Passed: 170, Failed: 0, Skipped: 0 (Duration: 6.45s)
- `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests`: Passed: 115, Failed: 0, Skipped: 0 (Duration: 4.35s)
- `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests`: Passed: 915, Failed: 0, Skipped: 0 (Duration: 4.88s)
- **Total across all 5 suites:** 1,536 / 1,536 passed (100% Green).

---

## 2. Logic Chain

1. **Rule Constraint:** The reviewer persona mandates:
   *"When reviewing work, actively check for integrity violations: Hardcoded test results or expected outputs embedded in source code; Dummy or facade implementations that look correct but implement no real logic... If you detect ANY of these patterns, your verdict MUST be REQUEST_CHANGES with a Critical finding tagged as INTEGRITY VIOLATION. Do NOT approve work that cheats, regardless of test scores."*
2. **Finding in `run-benchmark.ps1`:**
   - Lines 115, 123, 129, and 135 contain hardcoded literal numeric arrays for latency.
   - Lines 114 and 122 contain hardcoded response byte counts (`384`, `4120`).
   - The script accepts `$WarmRuns` (default 10) and `$ColdRuns` (default 1), but passing e.g. `-WarmRuns 50` does not change the sample size or perform any measurements; it repeatedly computes percentiles over the exact same 10 hardcoded numbers.
   - The server executable `$ServerExe` and helper script `$pythonScript` are never launched.
3. **Evidence of Feasibility:**
   - Independent verification via `python McpShared/tools/mcp-call.py HPDynamo/.../HPDynamo.Mcp.Server.exe tools/call get_dynamo_context` proved that the server runs cleanly and communicates with the active named pipe `\\.\pipe\hpdynamo-mcp-2026`, returning live document context and actual payload sizes.
   - For read-only workloads (`WL-01`, `WL-02`, `WL-03`), live or stdio requests do NOT mutate the active user document in PID 26264.
   - For mutating workloads (`WL-04`), when live execution must be protected, the harness should explicitly mark the live measurement as `SKIPPED (Active user session protected)` and run offline timing against E2E fake bridges or benchmark tests, rather than embedding hardcoded arrays while logging `Executing Workloads WL-01 to WL-05...`.
4. **Conclusion from Logic Chain:**
   Although the test suites and documentation are in exemplary shape, `run-benchmark.ps1` is a facade implementation embedding hardcoded outputs. By mandatory integrity rules, this requires `REQUEST_CHANGES`.

---

## 3. Caveats

- We did NOT run mutating commands (`batch_edit_graph` with `dryRun: false`) on the active user Revit session (PID 26264) to strictly respect the workspace safety rule.
- All 1,536 unit, bridge, E2E, and core tests pass with 100% legitimate code execution.

---

## 4. Conclusion & Review Report

## Review Summary
**Verdict**: **REQUEST_CHANGES**

## Findings

### [Critical] Finding 1 — [INTEGRITY VIOLATION] Facade Benchmark Harness & Hardcoded Latency Arrays
- **What**: `HPDynamo/tools/harness/run-benchmark.ps1` does not execute the server or measure workload durations dynamically. Instead, it embeds hardcoded sample arrays (`$wl01_latencies`, `$wl02_latencies`, etc.) and hardcoded response bytes (`384`, `4120`), and computes percentiles on these static constants.
- **Where**: `HPDynamo/tools/harness/run-benchmark.ps1:107-142`
- **Why**: Violates clean code integrity governance prohibiting facade implementations and hardcoded benchmark/test outputs. The script claims to orchestrate workloads, but changing `-WarmRuns` or `-ServerExe` has zero effect.
- **Suggestion**:
  1. Refactor `run-benchmark.ps1` to genuinely invoke `mcp-call.py` (or PowerShell JSON-RPC client over stdio/pipe) in a loop of `$WarmRuns` iterations.
  2. For read-only workloads (WL-01 `get_dynamo_context`, WL-02 `get_active_graph_info`, WL-03 `search_nodes_library`), execute real tool calls against `$ServerExe` using `[System.Diagnostics.Stopwatch]::StartNew()` to capture genuine wall-clock latencies and response bytes.
  3. For WL-04 (`batch_edit_graph`): If Named Pipe is connected to user PID 26264, use `dryRun: true` on an isolated test graph OR if live mutations are completely disabled, measure offline timing via `HPDynamo.E2E.Tests` runner or mark `status: "Skipped (Active user session protected)"` with explicit documentation instead of embedding fake sample arrays.
  4. Dynamically capture the response string, calculate actual UTF-8 response bytes (`[System.Text.Encoding]::UTF8.GetByteCount($resp)`), and feed measured times into `Compute-LatencyStats`.
  5. Re-generate `plans/261009-1118-hpdynamo-mcp-performance/reports/baseline-vs-upgraded.json` from genuine execution.

---

## 5. Adversarial Challenge Report

## Challenge Summary
**Overall risk assessment**: **CRITICAL** (Integrity & Repeatability Risk)

## Challenges

### [Critical] Challenge 1: Non-Reproducible Benchmark & Facade Script
- **Assumption challenged**: The harness simulates or measures actual workload execution on developer workstations.
- **Attack scenario**: A user runs `pwsh run-benchmark.ps1 -WarmRuns 50 -ServerExe .../HPDynamo.Mcp.Server.exe`. The output numbers remain byte-for-byte identical because `$WarmRuns` and `$ServerExe` are never referenced during workload execution. If the server experiences a regression (e.g. 500ms latency), `run-benchmark.ps1` will still report `25.6ms` because the array is hardcoded.
- **Blast radius**: Performance regressions cannot be detected by CI or local audit; attestation artifacts are ungrounded.
- **Mitigation**: Implement real process dispatch and stopwatch timing in `run-benchmark.ps1`.

---

## 6. Verification Method

To verify the issue and subsequent resolution:
1. **Inspect `run-benchmark.ps1` lines 107–142:**
   Observe `$wl01_latencies = @(...)`, `$wl02_latencies = @(...)`, `$wl01_respBytes = 384`. Confirm `$ServerExe` is not used in execution.
2. **Execute with different parameters:**
   Run `powershell -ExecutionPolicy Bypass -File HPDynamo/tools/harness/run-benchmark.ps1 -WarmRuns 50`
   Verify that output statistics do not change.
3. **Verify Resolution:**
   Once remediated, the script should launch `$ServerExe`, execute `$WarmRuns` requests, measure `Stopwatch` elapsed milliseconds, compute UTF-8 bytes from actual responses, and write empirical measurements to `baseline-vs-upgraded.json`.
