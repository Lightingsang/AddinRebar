## 2026-10-10T10:58:34Z
You are explorer_m4_it2_2 (Facade Tests Remediation Explorer).
Your working directory is:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_it2_2

MANDATORY FIRST STEP:
Read the user requirements from:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Specifically section ## 2026-10-10T06:56:11Z and ## 2026-10-10T10:29:34Z.

Also read:
- Master project index: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md
- Full Reviewer Evidence Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_1\handoff.md
- Worker M4 Handoff: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4\handoff.md

CRITICAL PROBLEM:
Reviewer reviewer_m4_1 reported a Critical finding (INTEGRITY VIOLATION):
13 unit tests across `PlayerRunnerTests.cs` (10 tests) and `CanvasReaderTests.cs` (3 tests) masquerade as testing component features (parameter binding, type conversion, output filtering, volatile data formatting) but execute zero code from the target classes, testing only .NET BCL primitives (`JsonDocument`, `HashSet`, `Dictionary`, `string.ToUpperInvariant`, local anonymous objects).
Specific tests flagged:
- `PlayerRunnerTests.cs`: Lines 74, 86, 96, 105, 119, 129, 138, 149, 161, 238.
- `CanvasReaderTests.cs`: Lines 168, 193, 211.

OBJECTIVE:
Design a concrete, authentic refactoring plan for all 13 flagged tests:
1. For `PlayerRunner`:
   - How to test genuine parameter binding (`FindInputParam`, `SetParamValue`), type conversions (Number, Boolean, String), and output extraction (`ExtractOutputs`) by directly invoking the methods in `PlayerRunner` (using reflection if private static, or public API/managed archives).
2. For `CanvasReader`:
   - How to test genuine `ExtractParam`, `ExtractComponentWires`, `Read`, and category filtering by invoking `CanvasReader` methods directly rather than creating local anonymous records.
3. Guarantee that if the target logic in `PlayerRunner.cs` or `CanvasReader.cs` is modified or broken, these tests will genuinely fail.
4. Ensure all refactored test methods are <= 50 lines (Rule M3) and files <= 300 lines (Rule C2).

SCOPE:
- Read-only exploration. DO NOT modify production code directly.
- Write your findings to: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_it2_2\facade_tests_remediation.md`.
- Produce a self-contained `handoff.md` in your working directory.
- When done, call send_message to report completion back to parent orchestrator.
