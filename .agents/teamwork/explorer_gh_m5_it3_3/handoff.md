# Handoff Report: Tier 5 Pseudo-Tests Remediation & Architectural Design

**Agent**: `explorer_gh_m5_it3_3` (Tier 5 Pseudo-Tests Remediation Explorer)  
**Parent Agent**: `65736fe1-0fe9-4743-9fff-fd7f010af55d`  
**Date**: 2026-10-10T14:42:00Z  
**Scope**: Remediation designs for residual Tier 5 pseudo-tests flagged during Gate M5 Iteration 2  
**Handoff Type**: Hard (Task complete)

---

## 1. Observation

1. **Reviewer 1 Rejection Findings**:
   Reviewer `reviewer_gh_m5_it2_1` rejected Gate M5 Iteration 2 (REQUEST_CHANGES, tagged INTEGRITY VIOLATION) citing residual self-certifying pseudo-tests in Tier 5:
   - `Tier5_SeedParameterFuzzingTests.cs:25-130`:
     * `TC_T5_FUZZ_02`: Local `HashSet` comparison against string literals.
     * `TC_T5_FUZZ_06`: Local static function `SanitizeQuery` declared inside test body.
     * `TC_T5_FUZZ_07`: Local arithmetic evaluation `Math.Clamp(l, 1, 200)`.
     * `TC_T5_FUZZ_09`: Local static function `ValidateFramingLength` inside test body.
     * `TC_T5_FUZZ_10`: Local static function `IsClosedBoundary` inside test body.
   - `Tier5_SecurityOptInAndDenyListTests.cs:90-95`:
     * `TC_T5_SEC_08`: BCL primitive parser `JsonDocument.Parse("{ invalid_json }")`.

2. **Codebase Exploration**:
   - `SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly)` dynamically provides all 12 embedded seeds from `HPGrasshopper.Mcp.Server`, including `facade_panelization`, `create_levels_grids`, `generate_columns_beams`, `create_floors_slabs`, and `generate_stirrup_distribution`.
   - `GrasshopperE2EBridge` provides real Roslyn compilation and execution with `ScriptGuard.Check`, `ScriptQuality.Find`, and `ScriptArgs` argument binding.
   - `GrasshopperPipeSimulator` provides an in-memory Named Pipe server binding production `PipeListener` and `RequestDispatcher`.
   - `RequestDispatcher.HandleLineAsync` catches `JsonException` on invalid lines and sends `JsonRpcEnvelope.Failure(0, BridgeErrorCode.ParseError, "Line is not valid JSON-RPC.")` with error code `-32700`.
   - `ToolRegistryDb.Search` in `McpShared/HPRebar.Mcp.Server.Core` implements SQLite FTS5 query tokenization and safely handles arbitrary query lengths.

3. **AST Clean Code Verification**:
   Running `python scratch/verify_clean_code_ast.py ".agents/teamwork/explorer_gh_m5_it3_3" 40`:
   - Rule C2 (File Length $\le 300$ lines): 0 violations.
     * `proposed_Tier5_SeedParameterFuzzingTests.cs`: 240 lines.
     * `proposed_Tier5_SecurityOptInAndDenyListTests.cs`: 114 lines.
   - Rule M3 (Method Length $\le 50$ lines): 0 violations (all methods $\le 28$ lines).
   - Rule Q-B1 (Commented-out code): 0 violations.
   - Rule Q-B2 (Unannotated empty catches): 0 violations.

---

## 2. Logic Chain

1. **Defect Causality**: The flagged tests were written by evaluating local C# helpers and constants rather than integrating with the production deliverable and harness. As a result, regressions in tool execution, parameter parsing, or protocol dispatch would never be caught.
2. **Remediation Strategy for Fuzzing**:
   - For `TC_T5_FUZZ_02`: Execute a Roslyn script via `GrasshopperE2EBridge` reading `args.Require("filePath")` to validate extension acceptance (`.gh`, `.ghx`) and rejection of invalid formats (`.txt`, `.exe`, `.dyn`, `.dwg`, `.rvt`, `.dll`).
   - For `TC_T5_FUZZ_06`: Call real `ToolRegistryDb.Search` with a 10,000-character fuzzed string and whitespace, asserting graceful SQLite handling without crashes.
   - For `TC_T5_FUZZ_07`: Execute production seed `facade_panelization` via `GrasshopperE2EBridge` with fuzzed division limits (`-50`, `0`, `500`), proving runtime stability and non-negative outputs.
   - For `TC_T5_FUZZ_09`: Validate production seed `generate_columns_beams` against `ScriptGuard.Check` and `ScriptQuality.Find`, and execute framing length validation on zero-length beam lines through `bridge.ExecuteAsync`, proving rejection below 0.001m.
   - For `TC_T5_FUZZ_10`: Validate production seed `create_floors_slabs` against `ScriptGuard.Check` and `ScriptQuality.Find`, and execute boundary loop validation on $< 3$ vertices through `bridge.ExecuteAsync`, proving rejection of degenerate loops.
   - Also hardened `TC_T5_FUZZ_01`, `TC_T5_FUZZ_08`, `TC_T5_FUZZ_11`, and `TC_T5_FUZZ_12` to ensure the entire file is 100% genuine and free of pseudo-tests.
3. **Remediation Strategy for Security Protocol**:
   - For `TC_T5_SEC_08`: Start a live Named Pipe simulator via `GrasshopperPipeSimulator`, connect via `NamedPipeClientStream`, send raw invalid JSON string `"{ invalid_json }"`, and assert that the protocol response is a JSON-RPC error with `BridgeErrorCode.ParseError` (`-32700`).

---

## 3. Caveats

- **Read-Only Explorer Scope**: As an explorer subagent, changes were not committed directly to `HPGrasshopper.E2E.Tests/Tiers/`. Instead, complete replacement files were authored and validated in the explorer folder:
  * `proposed_Tier5_SeedParameterFuzzingTests.cs`
  * `proposed_Tier5_SecurityOptInAndDenyListTests.cs`
- The downstream worker agent must copy these proposed files to `HPGrasshopper/HPGrasshopper.E2E.Tests/Tiers/` and run the build and test verification pipeline.

---

## 4. Conclusion

All 6 flagged Tier 5 pseudo-tests (plus 4 adjacent fuzzing tests) have been authentically redesigned. The replacement tests directly exercise `GrasshopperE2EBridge`, `ScriptGuard`, `ScriptQuality`, `ScriptArgs`, `SeedInstaller`, `ToolRegistryDb`, `PipeListener`, and `RequestDispatcher`. Zero local dummy functions, zero constant arithmetic, and zero local collection comparisons remain. Clean Code rules C2 and M3 are 100% satisfied.

---

## 5. Verification Method

To independently verify the proposed designs:

1. **Clean Code AST Audit**:
   ```powershell
   python scratch/verify_clean_code_ast.py ".agents/teamwork/explorer_gh_m5_it3_3" 40
   ```
   *Expected result*: 0 violations for Rule C2 ($\le 300$ lines), 0 methods $> 40$ lines.

2. **Inspect Detailed Remediation Report**:
   Inspect `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it3_3\tier5_remediation.md`.

3. **Inspect Proposed Replacement Files**:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it3_3\proposed_Tier5_SeedParameterFuzzingTests.cs`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it3_3\proposed_Tier5_SecurityOptInAndDenyListTests.cs`
