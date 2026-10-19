# HANDOFF — Milestone M2 Review Report: Evaluation Sync & Queue Safety (HPDynamo MCP)

**Agent**: reviewer_dynamo_m2_1 (Roles: reviewer, critic)  
**Recipient**: Parent Orchestrator (`f448d195-c008-4b83-983a-30d0f3ab6338`)  
**Verdict**: **APPROVE**  
**Date**: 2026-10-09  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_dynamo_m2_1\`  

---

## 1. Observation

### Codebase Inspections
1. **`HPDynamo/HPDynamo.McpBridge/Execution/DynamoEvaluationSync.cs`** (86 lines):
   - Line 25: `var evaluationSuccess = false;` defaults to false.
   - Line 35: `model.EvaluationCompleted += OnEvaluationCompleted;`
   - Lines 55–58:
     ```csharp
     finally
     {
         model.EvaluationCompleted -= OnEvaluationCompleted;
     }
     ```
     Unhooks the event handler reliably in the finally block.
   - Lines 42–46: Pushes a nested `DispatcherFrame` on the UI thread to pump messages while awaiting evaluation without freezing Revit.
   - Lines 61–85: `CheckCompletion` helper method keeps `RunAndWait` at 46 lines (strictly $\le 50$ lines, Rule M3).

2. **`HPDynamo/HPDynamo.McpBridge/Execution/CanvasGraphRunner.cs`** (228 lines):
   - Lines 35–47, 90–102, 152–165: Checks return value of `DynamoEvaluationSync.RunAndWait`.
   - On `!evalOk`: throws `OperationCanceledException` (if `ct.IsCancellationRequested`) or `InvalidOperationException` ("... evaluation failed or timed out").
   - Lines 49–55, 104–109, 166–169: Checks `ws.Nodes.Count(n => n.State == ElementState.Error)` and throws `InvalidOperationException` if error count $> 0$.
   - Lines 182–189: Catches any failure during `ExecuteDesignScript`, cleans up the ephemeral `CodeBlockNodeModel` from canvas, and re-throws to trigger `RevitTransactionScope.SafeRollback`.
   - Genuine result tracking without hardcoded facade values.

3. **`HPDynamo/HPDynamo.McpBridge/Support/DynamoScriptGlobals.cs`** (80 lines):
   - Lines 58–79: Public method `RunAndWait(Action? runAction = null, TimeSpan? timeout = null)` resolves active `DynamoModel` and calls `DynamoEvaluationSync.RunAndWait`.

4. **`HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/run_dyn_file/code.cs`** (121 lines):
   - Line 78: Synchronously awaits evaluation:
     ```csharp
     bool evalSuccess = RunAndWait(() => activeCanvasWs.Run());
     if (!evalSuccess)
     {
         if (ct.IsCancellationRequested)
             throw new OperationCanceledException("Dynamo evaluation was cancelled.", ct);
         throw new InvalidOperationException($"Dynamo evaluation failed or timed out for graph '{targetGraphPath}'.");
     }
     ```
   - Only reads terminal outputs after evaluation completes successfully.

5. **`HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs`** (182 lines):
   - Lines 62, 81, 113: Captures `linkedToken` from `RevitTransactionScope.Execute`'s delegate and forwards it into `_canvasRunner`, `_headlessRunner`, and `compiled.Script!.RunAsync(globals, linkedToken)`.

6. **`HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs`** (296 lines):
   - Lines 280–296: Implements 3-state machine `WorkItemTracker` (`Queued`, `Running`, `Completed`) with atomic `Interlocked.CompareExchange` transitions.
   - Lines 289–294:
     ```csharp
     public bool TryCancelIfQueued(TaskCompletionSource<object?> completion, CancellationToken ct = default)
     {
         if (Interlocked.CompareExchange(ref _state, (int)WorkItemState.Completed, (int)WorkItemState.Queued) != (int)WorkItemState.Queued) return false;
         completion.TrySetCanceled(ct);
         return true;
     }
     ```
   - Once transitioning to `Running`, cancellation does not cancel the `TaskCompletionSource` prematurely; the Revit thread continues through execution, rollback, and cleanup.
   - Lines 113–145: `_busy` is set to `1` via `CompareExchange` and only cleared to `0` in the `finally` block of `ExecuteAsync` after the Revit thread finishes awaiting the work item.
   - Lines 218–228: `Dispose()` drains all remaining pending requests from `_pending` queue, faulting them with `OperationCanceledException` without leaking hanging tasks.

7. **Clean Code Rule Compliance**:
   - Every file strictly $\le 300$ lines (`DynamoBridgeCoordinator.cs` is at 296 lines, Rule C2).
   - Every method strictly $\le 50$ lines (Rule M3).
   - Zero commented-out dead code (Rule Q-B1).
   - Zero empty `catch` blocks without justification (Rule Q-B2).

### Test Suite Execution
Direct execution of all project test suites:
- `dotnet test HPDynamo\HPDynamo.McpBridge.Tests`: Passed: 125, Failed: 0, Skipped: 0 (5.9s)
- `dotnet test HPDynamo\HPDynamo.Mcp.Server.Tests`: Passed: 112, Failed: 0, Skipped: 0 (6.1s)
- `dotnet test HPDynamo\HPDynamo.E2E.Tests`: Passed: 201, Failed: 0, Skipped: 0 (3.5s)
- `dotnet test McpShared\HPRebar.Mcp.Server.Core.Tests`: Passed: 899, Failed: 0, Skipped: 0 (4.6s)
- `dotnet test McpShared\HPRebar.McpBridge.Core.Net48Tests`: Passed: 115, Failed: 0, Skipped: 0 (4.7s)
**Total: 1,452 passed, 0 failed, 0 skipped.**

---

## 2. Logic Chain

1. **RV-02A (Evaluation Failure & Node Error Rollback)**:
   - When Dynamo evaluation fails, times out, or leaves nodes in `ElementState.Error`, `CanvasGraphRunner` throws `InvalidOperationException` or `OperationCanceledException`.
   - In `RevitTransactionScope.ExecuteCore`, the thrown exception is caught in the `catch` block (lines 147–160).
   - `SafeRollback` rolls back both the auto transaction and the outer `TransactionGroup`.
   - The returned `ExecuteResult` has `RolledBack = true` and `IsError = true`.
   - *Conclusion*: Corrupt or partial graph evaluations are never left committed in Revit.

2. **RV-02B (Output Synchronization in Seeds)**:
   - In `run_dyn_file/code.cs`, outputs were previously queried immediately after calling `Run()`, yielding null or stale values from previous evaluations.
   - Calling `RunAndWait(() => activeCanvasWs.Run())` blocks execution until `model.EvaluationCompleted` fires via the WPF dispatcher frame pump.
   - If evaluation does not succeed, an exception is thrown before reading output nodes.
   - *Conclusion*: Seeds never return stale or partial outputs from incomplete evaluations.

3. **RV-03 (linkedToken Preservation)**:
   - `RevitTransactionScope` creates `linked` combining the user cancellation token and the request timeout source.
   - `DynamoExecutionEngine` passes this `linkedToken` into all runners and Roslyn script globals.
   - *Conclusion*: Timeout budgets and user cancellations propagate cooperatively to Dynamo execution.

4. **RV-05 (3-State Queue Machine & Safe Cancel Handling)**:
   - `WorkItemTracker` coordinates state transitions between `Queued`, `Running`, and `Completed`.
   - If a cancellation token fires while an item is in `Queued`, `TryCancelIfQueued` transitions it to `Completed` and cancels the TCS. When Revit dequeues the item, it sees `IsCompleted == true` and skips execution.
   - If a cancellation token fires while an item is in `Running`, `TryCancelIfQueued` returns `false` and does not cancel the TCS prematurely. The Revit thread continues executing, rolls back transactions safely, and sets the TCS upon completion.
   - `_busy` remains `1` until the Revit thread finishes its rollback, preventing subsequent requests from corrupting Revit document state.
   - *Conclusion*: Thread ownership and transaction safety are strictly preserved during cancellations.

---

## 3. Caveats

1. **Revit In-Process Runtime**: `dotnet test` tests the state machine, compiler, and contract boundaries without loading Revit native C++ binaries (`RevitAPIUI.dll`). Live in-process validation within Revit 2026 / Dynamo UI will be performed in Milestone 4 / E2E.
2. **DynamoBridgeCoordinator Line Count**: `DynamoBridgeCoordinator.cs` currently has 296 lines, leaving a 4-line margin before hitting the 300-line ceiling (Rule C2). Implementers of Milestone 3 (`ICorrelatedCancelExecutor`) must ensure additions do not exceed this limit by extracting helper classes if necessary.
3. **Optional Evaluation in batch_edit_graph**: In `batch_edit_graph/code.cs` (delivered in M1), `runEvaluation` defaults to `false`. When set to `true`, it still invokes `homeWs.Run()`. Since `batch_edit_graph` focuses on canvas modification and programmatic rollback, this does not compromise M2, but future iterations can route through `RunAndWait` as well.

---

## 4. Conclusion

Milestone 2 implementation is **APPROVED**.
- Integrity check: **PASS** (Zero hardcoded outputs, zero facade implementations, zero shortcuts).
- Clean Code check: **PASS** (All files $\le 300$ lines, methods $\le 50$ lines, zero empty catches, zero commented code).
- Functionality check: **PASS** (RV-02A, RV-02B, RV-03, and RV-05 fully implemented and verified).
- Test execution: **PASS** (1,452 / 1,452 tests green across all 5 test suites).

---

## 5. Verification Method

To independently verify this verdict:

1. **Run McpBridge Unit Tests**:
   ```powershell
   dotnet test HPDynamo\HPDynamo.McpBridge.Tests
   ```
   *Expected*: 125 passed, 0 failed, 0 skipped.

2. **Run Mcp Server Unit Tests**:
   ```powershell
   dotnet test HPDynamo\HPDynamo.Mcp.Server.Tests
   ```
   *Expected*: 112 passed, 0 failed, 0 skipped.

3. **Run E2E Tests**:
   ```powershell
   dotnet test HPDynamo\HPDynamo.E2E.Tests
   ```
   *Expected*: 201 passed, 0 failed, 0 skipped.

4. **Run McpShared Engine Tests**:
   ```powershell
   cd McpShared
   dotnet test HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj
   dotnet test HPRebar.McpBridge.Core.Net48Tests\HPRebar.McpBridge.Core.Net48Tests.csproj
   cd ..
   ```
   *Expected*: 899 passed and 115 passed, 0 failed, 0 skipped.

5. **Verify Clean Code Line Limits**:
   ```powershell
   powershell -Command "(Get-Content HPDynamo\HPDynamo.McpBridge\Coordinator\DynamoBridgeCoordinator.cs).Count"
   ```
   *Expected*: $\le 300$ (currently 296).
