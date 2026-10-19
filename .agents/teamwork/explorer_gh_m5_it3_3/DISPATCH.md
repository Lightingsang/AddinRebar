## 2026-10-10T14:25:43Z

You are explorer_gh_m5_it3_3 (Tier 5 Pseudo-Tests Remediation Explorer).
Your working directory is:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it3_3

MANDATORY FIRST STEP:
Read the user requirements from:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Specifically section ## 2026-10-10T06:56:11Z.

Also read:
- Master project index: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md
- Test Infra specification: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_INFRA.md
- Full Reviewer 1 Evidence Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_gh_m5_it2_1\handoff.md
- Worker M5 Iteration 2 handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_gh_m5_it2\handoff.md

CONTEXT:
Reviewer reviewer_gh_m5_it2_1 rejected Gate M5 Iteration 2 (REQUEST_CHANGES, tagged INTEGRITY VIOLATION) due to residual self-certifying pseudo-tests in Tier 5:
- `Tier5_SeedParameterFuzzingTests.cs:25-130`:
  * `TC_T5_FUZZ_02`: local HashSet comparison.
  * `TC_T5_FUZZ_06`: local static function `SanitizeQuery` inside test.
  * `TC_T5_FUZZ_07`: local `Math.Clamp(l, 1, 200)`.
  * `TC_T5_FUZZ_09`: local static function `ValidateFramingLength` inside test.
  * `TC_T5_FUZZ_10`: local static function `IsClosedBoundary` inside test.
- `Tier5_SecurityOptInAndDenyListTests.cs`:
  * `TC_T5_SEC_08`: BCL `JsonDocument.Parse("{ invalid_json }")`.

OBJECTIVE:
1. Inspect all flagged methods in `Tier5_SeedParameterFuzzingTests.cs` and `Tier5_SecurityOptInAndDenyListTests.cs`.
2. Design authentic replacements:
   - For fuzzing: pass fuzz inputs (extreme parameters, boundary values, NaN/infinity) into real seed script code via `ScriptArgs` and validate against `ScriptGuard.Check`, `ScriptQuality.Find`, and `bridge.ExecuteAsync(request)`.
   - For security: pass malformed JSON-RPC payloads into `PipeListener` / `RequestDispatcher` or `BridgeClient` and verify protocol error response (`JsonRpcError` with `-32700 Parse error`).
3. Ensure all proposed methods strictly comply with Rule C2 (<= 300 lines) and Rule M3 (<= 50 lines).
4. Save report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it3_3\tier5_remediation.md` and write `handoff.md`.
5. Call send_message when done.
