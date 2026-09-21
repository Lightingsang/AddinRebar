# Adversarial Challenge Report: HPTekla.Mcp.Server.Tests

**Agent:** `teamwork_preview_challenger_m4_1`  
**Milestone:** Milestone 4 (Automated Test Suites & Live Verification Harness)  
**Parent / Caller:** `5d7560ee-5142-428f-a172-e73cf7738ac1` (`parent`)  
**Date:** 2026-09-21T19:10:00Z  

---

## 1. Executive Summary

- **Overall Risk Assessment:** **LOW**
- **Verdict:** **APPROVE**
- **Test Suite Status:** 96/96 PASSED, 0 FAILED, 0 SKIPPED across all test runs.
- **Empirical Reproducibility:** 100% deterministic, zero test flakiness observed across 6 consecutive executions.
- **Tekla Structures 2025 Open API Assembly Resolution:** Confirmed. 39 managed Tekla assemblies at `C:\Program Files\Tekla Structures\2025.0\bin` are dynamically discovered and loaded into Roslyn with zero compilation errors and zero skips.

---

## 2. Adversarial Challenge Dimensions & Empirical Results

### Challenge 1: Non-Flakiness, Concurrency & Determinism
- **Target:** `HPTekla.Mcp.Server.Tests.exe` (Target `net10.0`, Microsoft.Testing.Platform runner, xUnit v3).
- **Hypothesis:** Tests interacting over Named Pipes (`SeedExecutionTests`) or Roslyn compilation caches might suffer from race conditions, pipe name collisions, or memory exhaustion when executed repeatedly in rapid succession.
- **Empirical Stress Test:** Executed 6 independent test runs:
  - Run 1 (Standalone): 96 total, 0 failed, 96 succeeded, 0 skipped (3,054 ms)
  - Run 2 (Batch loop 1): 96 total, 0 failed, 96 succeeded, 0 skipped (3,399 ms)
  - Run 3 (Batch loop 2): 96 total, 0 failed, 96 succeeded, 0 skipped (4,054 ms)
  - Run 4 (Batch loop 3): 96 total, 0 failed, 96 succeeded, 0 skipped (3,400 ms)
  - Run 5 (Batch loop 4): 96 total, 0 failed, 96 succeeded, 0 skipped (3,600 ms)
  - Run 6 (Batch loop 5): 96 total, 0 failed, 96 succeeded, 0 skipped (3,398 ms)
- **Observations:**
  - `SeedExecutionTests` uses `Guid.NewGuid().ToString("N")` per test fixture, isolating pipes from concurrent or repeated runs (`hptekla-mcp-test-<guid>`).
  - Pipe cleanup is guaranteed via `IAsyncLifetime` (`_client.DisposeAsync()`, `_listener.StopAsync()`, `_listener.Dispose()`).
  - **Result: PASS. Zero flakiness.**

---

### Challenge 2: TeklaHostProfile Naming Invariants & Cross-Host Contamination
- **Target:** `TeklaHostProfileTests.cs` (8 tests).
- **Hypotheses Tested:**
  1. The profile might allow foreign host version configurations (e.g. 2020).
  2. Foreign host tools, prompts, or resource URI schemes might leak into the Tekla server registration.
  3. The profile might leak local file paths (`C:\`) or Windows usernames into bridge connection hints.
- **Empirical Results:**
  - `InvalidHostVersion_IsRefusedByValidation`: Configured `Bridge:HostVersion = "2020"`; correctly throws `OptionsValidationException`.
  - `Profile_names_the_tekla_pipe_prefix_tools_registry_root_ceiling_and_hints`:
    - `HostId == "tekla"`
    - `DisplayName == "Tekla Structures"`
    - `ServerName == "HPTekla MCP"`
    - `PipeName(2025) == "hptekla-mcp-2025"`
    - `Methods: tekla.execute, tekla.ping, tekla.context, tekla.cancel`
    - `MaxTimeoutSeconds == 600`
    - `Categories`: Includes `Model`, `Geometry`, `Property`, `Rebar`, `Drawing`, `Export`, `Generic`. Explicitly excludes Revit/AutoCAD/Navis concepts: `Layer`, `Block`, `Wall`, `Clash`, `Viewpoint`.
    - Sanitization: Excludes local disk roots (`C:\`) and `Environment.UserName` from hints.
  - `ToolSurface_ContainsFourCoreTools_EightRegistryTools_NoForeignHostTools`:
    - Validates exactly 12 static tools (4 core + 8 registry).
    - Asserts absence of any name matching `revit`, `autocad`, `etabs`, `sap2000`, `navis`, `excel`, `powerbi`, `robot`.
  - `ResourcesAndPrompts_UseTeklaSchemeAndNames`:
    - Resources: strictly `tekla://model/info`, `tekla://selection`, `registry://tools`. Zero foreign schemes.
    - Prompts: strictly `tekla_query_template`, `tekla_modify_template`, `tekla_rebar_template`, `toolify_run`. Zero foreign prefixes.
- **Result: PASS. Strict isolation verified.**

---

### Challenge 3: SeedCatalog AST Strictness & Bidirectional Argument Validation
- **Target:** `SeedCatalogTests.cs` (49 tests).
- **Hypotheses Tested:**
  1. Embedded seeds might read script arguments via `args.Str()` or `args.Double()` that are not declared in `tool.json` `inputSchema`.
  2. Seeds might declare inputSchema properties that are dead code / never consumed by the script.
  3. Seeds might evade safety guards or open forbidden transactions.
  4. Non-destructive seeds might inadvertently perform destructive actions.
- **Empirical Results:**
  - Discovers exactly 12 seeds across 6 categories (`Drawing`, `Export`, `Geometry`, `Model`, `Property`, `Rebar`).
  - Combined catalog equals exactly 24 tools (4 core + 8 registry + 12 seeds).
  - Bidirectional arg validation:
    - `read.IsSubsetOf(declared)`: Confirms NO seed reads undeclared arguments.
    - `declared.IsSubsetOf(read)`: Confirms NO seed declares unused arguments.
  - Guard check: `ScriptGuard.Check(seed.Code, GuardProfile.Tekla)` produces 0 diagnostics across all 12 seeds.
  - AST check: `ScriptAnalyzer.Analyze(seed.Code, AnalyzerProfile.Tekla)`: `facts.UsesTransaction == false`.
  - Return check: `EndsWithReturn(seed.Code)` passes for all 12 seeds.
  - Destructive check: Only `export_ifc` declares `destructive: true` (with `transaction: auto`, `timeout: 600s`). All other 11 seeds declare `destructive: false` (timeout <= 60s).
  - Validation engine: `ToolValidator.Validate(record, ...)` returns `IsValid == true` for all 12 seeds.
- **Result: PASS. Airtight AST and schema validation.**

---

### Challenge 4: SeedCompilation Assembly Resolution & Skip Behavior
- **Target:** `SeedCompilationTests.cs` (27 tests).
- **Hypotheses Tested:**
  1. The test might silently skip on this machine if path discovery fails.
  2. Dynamic assembly loading might load unmanaged C++ DLLs (such as `Tekla.Structures.Native.*`) causing crash/exceptions.
  3. Invalid Tekla API code might be accepted by mistake.
- **Empirical Results:**
  - Machine Status: Trimble Tekla Structures 2025.0 is installed at `C:\Program Files\Tekla Structures\2025.0\bin`.
  - Assembly Resolution:
    - `FindTeklaBinDir()` successfully resolves `C:\Program Files\Tekla Structures\2025.0\bin`.
    - Dynamic filter excludes `.resources.dll` and `*Native*` binaries.
    - `System.Reflection.AssemblyName.GetAssemblyName` filters verified that all 39 loaded assemblies are valid managed .NET libraries.
  - Test Execution:
    - `skipped: 0` in all test runs. No tests were skipped.
    - All 12 seeds compile cleanly against real Tekla 2025 assemblies with 0 errors.
  - API Misuse Verification:
    - `Compile_check_rejects_api_misuse_and_accepts_the_real_api`: Code calling `model.NoSuchMember(args.Int("n"))` fails compilation with CS1061. Valid code calling `model.GetInfo()` compiles with 0 errors.
  - Guard Misuse Verification:
    - `Guard_check_rejects_tekla_dialogs_quit_and_process_spawn`: Rejects `MessageBox.Show`, `Picker.PickObject`, `Process.Start`.
- **Result: PASS. Real assemblies compiled, zero silent skips.**

---

### Challenge 5: Cancellation & Timeout Handling (up to 600s)
- **Target:** `SeedExecutionTests.cs` (12 tests).
- **Hypotheses Tested:**
  1. The server might reject timeouts over 120s or fail to clamp values above 600s.
  2. Client-side cancellation or timeout might fail to notify the bridge executor.
  3. Refusals for execution or heavy operations might return misleading error messages.
- **Empirical Results:**
  - Timeout Clamping:
    - Requesting `timeoutSeconds: 600` passes `600` to the executor.
    - Requesting `timeoutSeconds: 1200` clamps to `600` (`Math.Clamp(timeoutSeconds, MinTimeoutSeconds, bridge.Profile.MaxTimeoutSeconds)`).
  - Cancellation Dispatch:
    - `Cancel_DispatchesToBridgeExecutor`: `tekla.cancel` RPC dispatches cleanly to `_executor.CancelCalls == 1`.
    - `Timeout_InformsModelThatChangesMayHavePersistedOrDiscarded`: When a client times out waiting for execution, `RevitBridgeClient` automatically issues a background `tekla.cancel` request to the executor (`_executor.CancelCalls > 0`).
  - Refusal Checks:
    - `ExecutionDisabled`: Returns error naming "HPTekla MCP Bridge" and "Allow AI execution".
    - `HeavyOperationsDisabled`: Returns error naming "Allow heavy operations".
    - Disconnected bridge: Error names `hptekla-mcp-2025` and does NOT leak user directories or local paths.
- **Result: PASS. Robust cancellation and timeout lifecycle.**

---

## 3. Additional External Stress Testing

### Adversarial Python Harness (`HPTekla/tools/harness/adversarial_challenge.py`)
Executed directly against `HPTekla.Mcp.Server.exe`:
- **Protocol Handshake:** `initialize` + `notifications/initialized` round-trip in 0.648s.
- **Tool Surface:** Exactly 24 tools (4 core + 8 registry + 12 seeds).
- **Resources & Prompts:** 3 resources, 4 prompts verified.
- **Bridge Disconnected Behavior:** Tools `execute_tekla_code`, `get_tekla_context`, `inspect_type`, `cancel_execution`, and seeds all respond with formatted errors without server crash.
- **Malformed Payloads:**
  - Unknown RPC method: Returns standard JSON-RPC -32601 error.
  - Unknown tool name: Returns -32602 error.
  - Missing required arguments: Returns standard error result.
  - Corrupted raw JSON syntax on stdin: Process survives without crash.
  - Whitespace / empty lines: Process survives without crash.
- **Stress & Liveness:**
  - 25 rapid-burst `tools/list` requests: 25/25 succeeded.
  - 100KB oversized query argument: Process survives and handles response cleanly.
  - Post-adversarial recovery: Final `tools/list` check succeeds with 24 tools.
  - Clean shutdown: Stdin closure exits cleanly within 5s with exit code 0.
- **Summary:** **45/45 PASSED (0 FAILED)**.

### Live Verification Script (`run-live-verify.ps1`)
- **Status:** 13 passed, 4 skipped (detached bridge mode), 0 failed. Exit code 0.

### Zero Regression Across McpShared
- `HPRebar.Mcp.Server.Core.Tests`: 742/742 passed (0 failed, 0 skipped).
- `HPRebar.McpBridge.Core.Net48Tests`: 113/113 passed (0 failed, 0 skipped).
- `HPTekla.McpBridge.Tests`: 24/24 passed (0 failed, 0 skipped).

---

## 4. Final Verdict

**Verdict:** **APPROVE**  
`HPTekla.Mcp.Server.Tests` is robust, deterministic, non-flaky, and strictly enforces all architectural, security, and protocol invariants for the Tekla Structures 2025 MCP ecosystem.
