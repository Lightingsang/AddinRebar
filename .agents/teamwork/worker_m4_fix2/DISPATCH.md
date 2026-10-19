# DISPATCH — worker_m4_fix2

## Task Objective
Remediate the Critical Finding (Integrity / Facade Code) in `PublicRouteRegressionTests.cs` and implement genuine, non-mocked verification for `PlayerRunner.CollectRuntimeMessages` when `GH_SolutionPhase.Failed`.

## Context & Inputs
- Original Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
- Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_14\plan.md`
- Reviewer Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_r2_1\handoff.md`
- Target Files:
  - `HPGrasshopper/HPGrasshopper.McpBridge.Tests/Host/PublicRouteRegressionTests.cs`
  - `HPGrasshopper/HPGrasshopper.McpBridge/Player/PlayerRunner.cs`
  - `HPGrasshopper/HPGrasshopper.McpBridge.Tests/Player/PlayerIntegrationTests.cs` (or relevant Player test file)

## Specific Remediation Instructions
1. In `HPGrasshopper/HPGrasshopper.McpBridge.Tests/Host/PublicRouteRegressionTests.cs`:
   - Remove lines 130–135 (the dead facade setup `comp`, `proxy`, `collectMethod`).
   - Rename the test to `SolveError_DispatcherPropagatesErrorPayload` to truthfully describe that it verifies dispatcher translation of execution failure payloads into JSON-RPC response properties (`success: false`, `isError: true`, error message, and errors array).
   - In Test 8 (`DryRun_BatchEditCanvas_ReportsRolledBackTrue`), clarify comments/naming to reflect dispatcher parameter parsing.
   - Verify that `PublicRouteRegressionTests.cs` remains <= 300 lines (Rule C2).
2. Genuine Component Solver Error Verification:
   - In `PlayerRunner.cs`: ensure `CollectRuntimeMessages` can accept `IEnumerable<IGH_DocumentObject> objects` (e.g. `internal static List<string> CollectRuntimeMessages(IEnumerable<IGH_DocumentObject> objects)`), following the exact same clean design pattern as `PlayerOutputExtractor.ExtractOutputs(IEnumerable<IGH_DocumentObject> objects, ...)`.
   - In `HPGrasshopper.McpBridge.Tests` (e.g. `PlayerIntegrationTests.cs` or `PlayerRunnerTests.cs`):
     Add a genuine, non-mocked test `CollectRuntimeMessages_ComponentPhaseFailed_CapturesPhaseFailure`:
     Create a component with `proxy.Phase = GH_SolutionPhase.Failed`, pass it to `PlayerRunner.CollectRuntimeMessages(new[] { comp })`, and assert that the returned error list contains the expected error message (`"Component execution failed (SolutionPhase.Failed)"`).
   - Ensure method length <= 50 lines (Rule M3) and file length <= 300 lines (Rule C2).
3. Test Suites & Governance:
   - Run all test suites: `HPGrasshopper.McpBridge.Tests`, `HPGrasshopper.Mcp.Server.Tests`, `HPGrasshopper.E2E.Tests`.
   - Verify `git status McpShared` remains strictly untouched.
   - Verify Rule C2 (<= 300 lines), Rule M3 (<= 50 lines), Rule Q-B1 (0 dead code), Rule Q-B2 (0 unannotated catch).

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Write your report to `handoff.md` in your directory and notify orchestrator (`be638180-e3a9-46c4-834a-a5beaaf11a17`) via `send_message`.


## 2026-10-12T19:06:45Z
You are worker_m4_fix2.
Your working directory is:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_fix2\

Read your dispatch instructions at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m4_fix2\DISPATCH.md
Read the original request at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read the scope document at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_14\plan.md
Read the reviewer handoff at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m4_r2_1\handoff.md

Remediate the Critical Finding in PublicRouteRegressionTests.cs and implement genuine verification for PlayerRunner.CollectRuntimeMessages:
1. In `PublicRouteRegressionTests.cs`:
   - Remove lines 130–135 (unused comp/proxy/collectMethod).
   - Rename Test 5 to `SolveError_DispatcherPropagatesErrorPayload` to truthfully describe what it verifies.
   - Clarify Test 8 naming/comments regarding dispatcher parameter parsing.
   - Keep file <= 300 lines (Rule C2).
2. Genuine Component Solver Error Verification:
   - Expose `internal static List<string> CollectRuntimeMessages(IEnumerable<IGH_DocumentObject> objects)` in `PlayerRunner.cs`.
   - In `PlayerIntegrationTests.cs` (or relevant Player test suite), add genuine test `CollectRuntimeMessages_ComponentPhaseFailed_CapturesPhaseFailure` that passes `comp` with `proxy.Phase = GH_SolutionPhase.Failed` and asserts the error list contains the phase failure message.
3. Tests & Governance:
   - Run all test suites: `HPGrasshopper.McpBridge.Tests`, `HPGrasshopper.Mcp.Server.Tests`, `HPGrasshopper.E2E.Tests`.
   - Check Rule C2 (<= 300 lines), Rule M3 (<= 50 lines), Rule Q-B1 (0 dead code), Rule Q-B2 (0 unannotated catch).
   - ZERO edits to `McpShared/`.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Write your report to `handoff.md` and notify orchestrator (be638180-e3a9-46c4-834a-a5beaaf11a17) via send_message.
