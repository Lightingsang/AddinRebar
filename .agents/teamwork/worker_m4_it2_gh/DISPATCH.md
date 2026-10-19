## 2026-10-10T11:15:32Z
You are worker_m4_it2_gh (HPGrasshopper Bridge Tests & Headless Remediation Worker).
Your working directory is:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_gh

MANDATORY FIRST STEP:
Read the user requirements from:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Specifically section ## 2026-10-10T06:56:11Z and ## 2026-10-10T10:29:34Z.

Also read:
- Master project index: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md
- Full Reviewer Finding Report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_1\handoff.md
- Modal Error Remediation Specification from Explorer 1: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_it2_1\modal_error_remediation.md
- Facade Tests Remediation Specification from Explorer 2: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_it2_2\facade_tests_remediation.md
- Rule Q-B2 Empty Catch Remediation from Explorer 3: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_it2_3\qb2_empty_catch_remediation.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

EXCLUSIVE WRITE OWNERSHIP:
You exclusively own:
- `HPGrasshopper/HPGrasshopper.McpBridge.Tests/**`
- `HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerRunner.cs`
- `HPGrasshopper/tools/harness/**`
- `PROJECT.md` (Update Milestone M4 status upon successful verification)

TASK OBJECTIVE:
Implement Milestone M4 Iteration 2 Remediation:

1. OS-Level Modal Hard Error Elimination (`HPGrasshopper.McpBridge.Tests/TestInitializer.cs`):
   - In `TestInitializer.cs`, add P/Invoke to `SetErrorMode` and `SetThreadErrorMode` in `[ModuleInitializer]` with flags:
     `SEM_FAILCRITICALERRORS (0x0001) | SEM_NOGPFAULTERRORBOX (0x0002) | SEM_NOOPENFILEERRORBOX (0x8000)`
     to guarantee Windows never presents a desktop modal popup dialog during test runs.

2. Eliminate `new GH_Document()` and Rule Q-B2 Empty Catches (`DualSafetyRollbackTests.cs` & `CanvasUndoScopeTests.cs`):
   - In `DualSafetyRollbackTests.cs` and `CanvasUndoScopeTests.cs`, replace `new GH_Document()` with safe headless creation via:
     `FormatterServices.GetUninitializedObject(typeof(GH_Document))` or pure managed proxy/reflection per Explorer 1 & 3 specifications.
   - Delete all 12 unannotated empty catch blocks (or provide full explanatory comments per Rule Q-B2). With safe headless instantiation, tests run assertions directly without throwing `TypeInitializationException` or `DllNotFoundException`.

3. Lazy Document Allocation in `PlayerRunner.cs` (`HPGrasshopper/HPGrasshopper.McpBridge/Tools/PlayerRunner.cs`):
   - In `PlayerRunner.Run`, read `GH_Archive` before allocating `GH_Document`. Return early error if file is missing, invalid XML, or corrupt, without calling `new GH_Document()`.
   - Ensure `PlayerRunner.cs` complies with Rule C2 (<= 300 lines) and Rule M3 (<= 50 lines).

4. Authentic Refactoring of 13 Facade Tests:
   - Add `HPGrasshopper/HPGrasshopper.McpBridge.Tests/Fakes/GrasshopperTestDoubles.cs` providing `DispatchProxy` test doubles for `IGH_Param` and `IGH_Component` per Explorer 2 specification.
   - Refactor the 10 facade tests in `PlayerRunnerTests.cs` (lines 74, 86, 96, 105, 119, 129, 138, 149, 161, 238) so they directly invoke `PlayerRunner` methods (`SetParamValue`, `FindInputParam`, `ExtractOutputs`, `DisposeDocument`).
   - Refactor the 3 facade tests in `CanvasReaderTests.cs` (lines 168, 193, 211) so they directly invoke `CanvasReader` methods (`ExtractParam`, `ExtractComponentWires`, `ExtractComponent`). Also update line 228 to test `_reader.Read(null)`.

5. Clean Code Rules:
   - Rule C2: All .cs files <= 300 lines.
   - Rule M3: All methods <= 50 lines (target <= 40 lines).
   - Rule Q-B1: Zero dead/commented-out code.
   - Rule Q-B2: Zero unannotated empty catches.

VERIFICATION COMMANDS (YOU MUST RUN):
1. `dotnet build HPGrasshopper/HPGrasshopper.slnx -c Release` (Confirm 0 errors, 0 warnings).
2. `dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests -c Release` (Confirm 100% tests PASS, ZERO modal popups!).
3. `dotnet test HPGrasshopper/HPGrasshopper.Mcp.Server.Tests -c Release` (Confirm 77/77 tests PASS).
4. `powershell -ExecutionPolicy Bypass -File HPGrasshopper/tools/harness/run-live-verify.ps1 -Mock` (Confirm 100% stages pass).

OUTPUT:
Write your handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_it2_gh\handoff.md` and call `send_message` when done.
