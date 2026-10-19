## 2026-10-10T14:25:43Z
You are explorer_gh_m5_it3_2 (Tier 3 Pseudo-Tests Remediation Explorer).
Your working directory is:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it3_2

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
Reviewer reviewer_gh_m5_it2_1 rejected Gate M5 Iteration 2 (REQUEST_CHANGES, tagged INTEGRITY VIOLATION) due to residual self-certifying pseudo-tests in Tier 3 (`Tier3_CrossFeatureInteractionTests.cs`):
1. `TC_T3_INT_02_BatchEditParameterAndRunPlayerSolve` (lines 33-45): Advertised as "Batch Edit Parameter And Run Player Solve", but only mutates a local dictionary and computes `floors * 3.8 = 95.0` in local arithmetic. Must execute edited graph via Roslyn script through `bridge.ExecuteAsync(request)` and verify output reflects updated parameter!
2. `TC_T3_INT_04_ToolRegistryDynamicLifecycle` (lines 59-67): Advertised as dynamic lifecycle, but only calls `bridge.ValidateScript(code)`. Must use `ToolRecord` and `ToolValidator.Validate` to simulate full lifecycle transitions (draft -> test -> publish).
3. `TC_T3_INT_05_DynamicRegistryToolRetirementRejection` (lines 70-77): Asserts that random string `"custom_tool"` is not in `BuildToolSurfaceList()`. Must exercise `ToolLifecycleService` or `ToolRecord.Status = "retired"` and verify rejection of retired tools.
4. `TC_T3_INT_09_ExportCanvasImagePostBatchEdit` (lines 119-129): Asserts 4-byte mock PNG header from its own test helper. Must test actual canvas image bounding box or disk output writing for canvas post-batch-edit.
5. `TC_T3_INT_14_WireReplacementLifoOrderingRollback` (lines 200-210): Connects 1 wire and asserts 1 wire exists. Zero wire replacement, zero LIFO ordering, zero rollback. Must connect wire 1 (A->T), replace with wire 2 (B->T), trigger rollback, and assert wire 1 (A->T) is restored!

OBJECTIVE:
1. Design concrete, authentic C# drop-in code replacements for all 5 flagged methods in `Tier3_CrossFeatureInteractionTests.cs`.
2. Ensure each test authentically exercises end-to-end interactions across canvas editing, player solving, dynamic tool lifecycle, and wire rollback.
3. Ensure all proposed methods strictly comply with Rule C2 (<= 300 lines) and Rule M3 (<= 50 lines).
4. Save report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_gh_m5_it3_2\tier3_remediation.md` and write `handoff.md`.
5. Call send_message when done.
