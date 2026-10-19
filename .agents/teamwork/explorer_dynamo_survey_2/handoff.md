# Handoff Report: R2 Investigation (RV-02, RV-03, RV-05)

## 1. Observation
1. **`DynamoEvaluationSync.cs:14,24-25,58,61,66`**:
   - `RunAndWait` returns `bool`.
   - `evaluationSuccess` defaults to `true` (line 25: `var evaluationSuccess = true;`).
   - Returns `false` on cancellation/timeout (line 58: `return false;`) and exception (line 66: `return false;`).
   - Detaches `model.EvaluationCompleted` in finally without stopping running Dynamo engine tasks.
2. **`CanvasGraphRunner.cs:35-40,78-83,128-144`**:
   - Line 35: `DynamoEvaluationSync.RunAndWait(...)` return value is discarded.
   - Line 78: `DynamoEvaluationSync.RunAndWait(...)` return value is discarded.
   - Line 128: `DynamoEvaluationSync.RunAndWait(...)` return value is discarded.
   - Line 144: Returns `new { Success = true, NodeId = cbn.GUID, Outputs = outputs }` unconditionally.
3. **`RevitTransactionScope.cs:140-146,247-254`**:
   - Line 140: `returnValue = action(linked.Token, Logger);`
   - Line 145: `rolledBack = FinalizeTransactions(txContext, request, mode, isDocModifiable, Logger);`
   - Line 254: `txContext.AssimilateGroup();` commits the `TransactionGroup` whenever `request.DryRun == false` and `action` does not throw an exception.
4. **`DynamoExecutionEngine.cs:60-66,79-85,109-122`**:
   - Lines 60-66: `RevitTransactionScope.Execute(uiapp, request, settings, progress, cancel, (_, _, log) => ... _canvasRunner.ExecuteDesignScript(..., ct, log) ...)`
   - Lines 79-85: `RevitTransactionScope.Execute(uiapp, request, settings, progress, cancel, (_, _, log) => ... _canvasRunner.RunDynFile(..., ct, log) ...)`
   - The second parameter of `ExecutionAction` (`linkedToken`) is discarded with `_`.
   - In contrast, lines 109-122 for `RunRoslynCodeAsync` correctly bind `(doc, linkedToken, log)` and pass `linkedToken`.
5. **`run_dyn_file/code.cs:78-83` and `batch_edit_graph/code.cs:224,244`**:
   - `run_dyn_file/code.cs:78`: calls `activeCanvasWs.Run();` directly; line 83 calls `ExtractTerminalOutputs` immediately without awaiting evaluation.
   - `batch_edit_graph/code.cs:224`: calls `homeWs.Run();` directly; line 244 returns `success = true` unconditionally.
6. **`GuardProfile.cs:246`**:
   - `GuardProfile.Dynamo` denies namespace `"HPDynamo.McpBridge"`, preventing seed scripts from calling `DynamoEvaluationSync.RunAndWait` directly.
7. **`DynamoBridgeCoordinator.cs:128,158,208-230,235-239,269-272`**:
   - Line 270: `var reg = request.CancellationToken.Register(() => request.Completion.TrySetCanceled(request.CancellationToken));`
   - Cancels `Completion.Task` immediately upon cancellation token trigger, regardless of whether work is `Queued` or `Running` on Revit thread.
   - Line 141: `await DispatchExecutionAsync` terminates with `TaskCanceledException`.
   - Line 158: `Volatile.Write(ref _busy, 0);` resets `_busy` to 0 while Revit thread is still executing `request.Work(application, request.CancellationToken)` or running `SafeRollback`.
   - Lines 235-239: `Dispose()` does not drain `_pending`, stranding any queued requests.

## 2. Logic Chain
1. **From Obs 1, 2, and 3**:
   When DesignScript or a Dynamo graph evaluation fails or times out internally ($< 30$s), `DynamoEvaluationSync.RunAndWait` returns `false`. However, `CanvasGraphRunner` ignores this return value and returns an object (`Success = true`). Because no exception is thrown, `RevitTransactionScope` executes `txContext.AssimilateGroup()` and commits all partial modifications to the active Revit document. Therefore, failed evaluations report success and commit broken changes.
2. **From Obs 4 and 1**:
   `RevitTransactionScope` creates a linked token combining the external cancellation token and its internal request timeout timer (`timeoutSource`). Because `DynamoExecutionEngine` discards `linkedToken` and forwards the raw work item token `ct`, the runners only observe `ct`. If the caller sets a short timeout (e.g., 5s), `RevitTransactionScope` cancels `linkedToken`, but `ct` remains active, causing `DynamoEvaluationSync` to pump the dispatcher until its hardcoded 30s/60s timeout expires.
3. **From Obs 5, 6, and 4**:
   `ToolManager.RunAsync` tags requests with tool names (`"run_dyn_file"`), which fails the check `request.Label?.StartsWith("DynFile")`. As a result, `run_dyn_file` is dispatched as a Roslyn script. In the script, `GuardProfile.Dynamo` denies access to `HPDynamo.McpBridge`, preventing access to `DynamoEvaluationSync`. Consequently, seeds invoke `Run()` asynchronously and read stale outputs before evaluation occurs.
4. **From Obs 7**:
   When cancellation occurs while a work item is executing on the Revit thread, `request.Completion.TrySetCanceled` completes the task immediately. The pipe thread's `ExecuteAsync` wakes up, catches the exception, and resets `_busy = 0`. At that moment, the Revit main thread is still inside `request.Work` or rolling back transactions. This releases concurrency protection prematurely, allowing subsequent requests to overlap with uncompleted Revit operations.

## 3. Caveats
- Direct live testing in a running `Revit.exe` process was not conducted during this read-only investigation (per constraints). All findings are derived from rigorous static code analysis, decompiler verification of Dynamo assemblies, and existing test suites.
- Dynamo's `EngineController` does not provide an explicit public `CancelEvaluation()` method; halting a running execution requires stopping the execution thread via Dynamo's `WorkspaceModel.RunSettings` or letting the dispatcher frame exit while verifying that downstream tasks do not mutate the document.

## 4. Conclusion
Findings RV-02, RV-03, and RV-05 represent critical correctness, transactional safety, and concurrency vulnerabilities in HPDynamo:
- **RV-02**: Must be remediated by propagating `RunAndWait` failure/timeout as exceptions to trigger `RevitTransactionScope.SafeRollback`, and providing a synchronous runner helper on `DynamoScriptGlobals`.
- **RV-03**: Must be remediated by binding `linkedToken` in `DynamoExecutionEngine` callbacks and aligning label routing.
- **RV-05**: Must be remediated by implementing a 3-state state machine (`Queued`, `Running`, `Completed`), ensuring that cancellation only cancels completion immediately if the item is `Queued`. Running work must retain thread ownership and keep `_busy = 1` until Revit completes execution and rollback. `Dispose()` must drain `_pending`.

## 5. Verification Method
1. **Code Review & Inspection**:
   - Inspect `HPDynamo/HPDynamo.McpBridge/Execution/CanvasGraphRunner.cs` lines 35, 78, 128, 144.
   - Inspect `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs` lines 61, 80.
   - Inspect `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs` lines 158, 270.
   - Inspect `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/run_dyn_file/code.cs` lines 78-83.
2. **Unit Test Execution**:
   - Run existing test suites from repository root:
     ```bash
     dotnet test HPDynamo/HPDynamo.McpBridge.Tests/HPDynamo.McpBridge.Tests.csproj
     dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests/HPDynamo.Mcp.Server.Tests.csproj
     dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj
     dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj
     ```
3. **Invalidation Conditions**:
   - The conclusion regarding RV-02 would be invalidated if `RevitTransactionScope` had an alternative mechanism to detect `Success = false` from return values without exceptions (verified absent: lines 145, 254).
   - The conclusion regarding RV-05 would be invalidated if `Completion.TrySetCanceled` did not wake `ExecuteAsync` (verified present: lines 141, 270).
