# Dispatch: teamwork_preview_worker_m5_2 (Milestone 5 Iteration 2: Remediation Worker)

**Assigned Agent**: `teamwork_preview_worker_m5_2`
**Role**: M5 Remediation Worker
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m5_2\`
**Date**: 2026-10-08

## Mission Objective
Remediate all findings flagged by Reviewer 1 and Reviewer 2 in Milestone 5 Iteration 1:
1. Fix Critical Integrity Finding in `DesignScriptRunnerTests.cs`.
2. Fix Intermittent MSB3061 File Lock in `HPDynamo.McpBridge.csproj`.
3. Add Seed Tool Roslyn Compilation Verification in `HPDynamo.Mcp.Server.Tests` per R5.
4. Strengthen Concurrency Guard Tests in `CoordinatorTests.cs`.

---

## Authoritative Inputs to Read First
- `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
- `PROJECT.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md`
- Reviewer 1 Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_m5_1\handoff.md`
- Reviewer 2 Report: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_m5_2\handoff.md`

---

## Detailed Remediation Tasks

### Task 1: Remediate Dummy Test in `DesignScriptRunnerTests.cs` (Finding 1)
- **Target File**: `HPDynamo/HPDynamo.McpBridge.Tests/Execution/DesignScriptRunnerTests.cs`
- **Current Issue**: Lines 59–70 contain `StatementValidation_ValidDesignScriptSyntax_AcceptsAssignments` which performs trivial assertions on a local string literal and invokes 0 lines of production code.
- **Fix**: Replace this test with a genuine test that exercises `HeadlessGraphRunner.ExecuteDesignScript` or `DynamoBridgeCoordinator`. For example, verify that calling `runner.ExecuteDesignScript(null, script, null, false, CancellationToken.None, _ => {})` throws `ArgumentNullException` with paramName `"uiapp"`, proving that valid non-empty script code successfully passes argument validation and reaches the execution core, and that passing empty/whitespace script throws `ArgumentException` with paramName `"script"`.

### Task 2: Fix Intermittent MSB3061 in `HPDynamo.McpBridge.csproj` (Finding 2)
- **Target File**: `HPDynamo/HPDynamo.McpBridge/HPDynamo.McpBridge.csproj`
- **Current Issue**: Target `RepackMaterialDesign` (lines 94–102) fails intermittently during multi-core parallel builds (`MSB3061`) when `<Delete Files="..." />` runs before ILRepack releases its process file lock.
- **Fix**: Add retry and error tolerance attributes to the `<Delete>` task:
  ```xml
  <Delete Files="$(TargetDir)MaterialDesignThemes.Wpf.dll;$(TargetDir)MaterialDesignColors.dll;$(TargetDir)Microsoft.Xaml.Behaviors.dll;$(TargetDir)MaterialDesignThemes.Wpf.xml"
          Retries="5"
          RetryDelayMilliseconds="300"
          ContinueOnError="true" />
  ```

### Task 3: Add Seed Tool Roslyn Compilation Tests in `HPDynamo.Mcp.Server.Tests` (Finding 3)
- **Target Directory**: `HPDynamo/HPDynamo.Mcp.Server.Tests/Seeds/`
- **Requirement**: `ORIGINAL_REQUEST.md` R5: "Seed tool compilation tests against Dynamo and Revit reference assemblies".
- **Fix**: Create `HPDynamo/HPDynamo.Mcp.Server.Tests/Seeds/SeedCompilationTests.cs`:
  - Inspect all 12 embedded seeds from `DynamoHostProfile.Instance`.
  - Use Roslyn `CSharpCompilation` (via `Microsoft.CodeAnalysis.CSharp`) to compile each seed's C# code script.
  - Add standard references (`System.Runtime`, `System.Private.CoreLib`, `System.Linq`, `System.Text.Json`, `DynamoHostProfile`).
  - For Dynamo and Revit reference assemblies (`DynamoCore`, `RevitAPI`, etc.), check whether reference assemblies exist on the test machine (e.g. via environment path or installed location). If host reference assemblies are not found, use `Assert.SkipWhen` to cleanly skip or compile against available public contract stubs, exactly as done in sibling test suites (`HPEtabs.Mcp.Server.Tests`, `HPNavis.Mcp.Server.Tests`).
  - Verify that compilation diagnostics contain 0 blocking errors for each seed when references are resolved.

### Task 4: Enhance Concurrency Guard Stress Testing in `CoordinatorTests.cs` (Finding 4)
- **Target File**: `HPDynamo/HPDynamo.McpBridge.Tests/Coordinator/CoordinatorTests.cs`
- **Fix**: Add an asynchronous concurrent execution test (e.g., `ExecuteAsync_ConcurrentCalls_ExactlyOneSucceedsOthersRejected`) that uses `Task.WhenAll` across 10+ concurrent tasks calling `coordinator.ExecuteAsync`, asserting that atomic CAS `Interlocked.CompareExchange` allows exactly 1 to start execution while others are rejected with `"Another Dynamo script or graph is still running"`.

---

## Clean Code & Invariant Rules
- Classes <= 300 lines (Rule C2).
- Methods/blocks <= 50 lines (Rule M3).
- 0 empty catch blocks (Rule Q-B1).
- 0 commented-out code (Rule Q-B2).
- 0 host DLLs in test bins (<Private>false</Private>).
- Do NOT pass `--nologo` to MTP runners!

---

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

---

## Verification Commands to Execute & Report
```powershell
dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.slnx" -c Debug
dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server.Tests\HPDynamo.Mcp.Server.Tests.csproj"
dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge.Tests\HPDynamo.McpBridge.Tests.csproj"
powershell -ExecutionPolicy Bypass -File "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\run-e2e-dynamo-tests.ps1" -Tier All
dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj"
```

---

## Handoff Deliverable
Write your full 5-component report (Observation, Logic Chain, Caveats, Conclusion, Verification Method) to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m5_2\handoff.md`
Then send a message back to `orchestrator_4`.


## 2026-10-08T12:54:07Z
Message received from orchestrator_4:
Remediate all findings flagged by Reviewer 1 and Reviewer 2 in Milestone 5:
1. In HPDynamo/HPDynamo.McpBridge.Tests/Execution/DesignScriptRunnerTests.cs:
   Remove/replace dummy test StatementValidation_ValidDesignScriptSyntax_AcceptsAssignments with genuine test assertions exercising HeadlessGraphRunner.ExecuteDesignScript.
2. In HPDynamo/HPDynamo.McpBridge/HPDynamo.McpBridge.csproj:
   Fix intermittent race condition / file lock error (MSB3061) in target RepackMaterialDesign (lines 94–102):
   Add Retries="5" RetryDelayMilliseconds="300" ContinueOnError="true" to the <Delete Files="..." /> task.
3. In HPDynamo/HPDynamo.Mcp.Server.Tests/:
   Add Seeds/SeedCompilationTests.cs satisfying requirement R5 ("Seed tool compilation tests against Dynamo and Revit reference assemblies"). Follow pattern used in sibling server test projects (like HPRebar.Mcp.Server.Tests, HPEtabs.Mcp.Server.Tests).
4. In HPDynamo/HPDynamo.McpBridge.Tests/Coordinator/CoordinatorTests.cs:
   Add asynchronous concurrent execution test using Task.WhenAll to test true concurrency contention.
5. Clean Code governance compliance.
