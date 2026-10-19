# Technical Investigation Report: R2 (RV-02, RV-03, RV-05)
**Subsystem**: HPDynamo MCP (`HPDynamo.McpBridge`, `HPDynamo.Mcp.Server`, `McpShared`)  
**Date**: 2026-10-09  
**Investigator**: `explorer_dynamo_survey_2`  
**Scope**: Evaluation Synchronization, Token Propagation, Work Item Lifecycle & Revit Thread Ownership on Cancellation  

---

## Executive Summary

This report delivers a deep code-level investigation into findings **RV-02**, **RV-03**, and **RV-05** identified during the independent Codex review of the HPDynamo MCP subsystem. 

1. **RV-02 (Evaluation Synchronization Flaw)**: `DynamoEvaluationSync.RunAndWait` returns a boolean representing evaluation success, but caller methods in `CanvasGraphRunner` (`RunActiveGraph`, `RunDynFile`, `ExecuteDesignScript`) **completely discard** this return value. In `ExecuteDesignScript`, `Success = true` is unconditionally hardcoded. Consequently, when an evaluation fails, times out internally, or is cancelled, `RevitTransactionScope` is never notified of the error and proceeds to **assimilate and commit the Revit `TransactionGroup`**, persisting broken or incomplete geometry changes into the active Revit model. Furthermore, seeds `run_dyn_file` and `batch_edit_graph` bypass evaluation synchronization entirely, triggering asynchronous runs and reading outputs before the engine evaluates.
2. **RV-03 (`linkedToken` Discard & Routing Mismatch)**: In `DynamoExecutionEngine.cs` (lines 61 and 80), the execution callbacks for `RunDesignScriptAsync` and `RunDynFileAsync` discard the second parameter (`linkedToken`) emitted by `RevitTransactionScope.Execute` using discard `_`. They pass the outer work item token (`ct`), which is only linked to the external request cancellation source. Because `RevitTransactionScope`'s internal `timeoutSource` (derived from `request.TimeoutSeconds`) only cancels `linkedToken`, the runners and evaluation helpers remain completely unaware of the request timeout budget, executing up to 60 seconds even if the caller configured a 5-second or 10-second timeout. Additionally, because `ToolManager.RunAsync` tags requests with the tool's registered name (`"run_dyn_file"`), the label does not match `StartsWith("DynFile")`, forcing seeds into Roslyn compilation where `GuardProfile.Dynamo` blocks them from referencing `DynamoEvaluationSync`.
3. **RV-05 (Premature Completion & Thread Ownership Hijacking)**: In `DynamoBridgeCoordinator.cs` (line 270), `RunOnRevitThreadAsync` registers a cancellation callback that directly calls `request.Completion.TrySetCanceled(request.CancellationToken)` for all work requests. When cancellation is triggered (via `cancel_execution` or transport abort), `Completion.Task` is cancelled immediately on the calling thread. This causes `ExecuteAsync` to wake up, execute its `finally` block, and write `_busy = 0` **while the Revit main thread is actively executing `request.Work` or performing transactional rollback**. As a result, subsequent requests can be accepted while Revit is still dirty, causing corrupted states and concurrent execution hazards. Furthermore, `Dispose()` fails to drain `_pending`, causing queued context requests to hang forever.

---

## 1. Deep Dive: RV-02 — Evaluation Synchronization

### 1.1 Implementation Architecture of `DynamoEvaluationSync.RunAndWait`
Located at `HPDynamo/HPDynamo.McpBridge/Execution/DynamoEvaluationSync.cs`:

```csharp
14: public static bool RunAndWait(
15:     DynamoModel model,
16:     Action runAction,
17:     TimeSpan timeout,
18:     CancellationToken cancellationToken,
19:     Action<string?> log)
20: {
21:     ArgumentNullException.ThrowIfNull(model);
22:     ArgumentNullException.ThrowIfNull(runAction);
23: 
24:     var completed = false;
25:     var evaluationSuccess = true;
26:     var frame = new DispatcherFrame();
27: 
28:     void OnEvaluationCompleted(object? sender, EvaluationCompletedEventArgs e)
29:     {
30:         completed = true;
31:         evaluationSuccess = e.EvaluationSucceeded;
32:         frame.Continue = false;
33:     }
34: 
35:     model.EvaluationCompleted += OnEvaluationCompleted;
...
43:         runAction();
44: 
45:         // If evaluation did not finish synchronously inside runAction, pump dispatcher until finished or timed out
46:         if (!completed && !linkedCts.Token.IsCancellationRequested)
47:         {
48:             Dispatcher.PushFrame(frame);
49:         }
50: 
51:         if (linkedCts.Token.IsCancellationRequested)
52:         {
53:             if (cancellationToken.IsCancellationRequested)
54:                 log("Dynamo evaluation was cancelled.");
55:             else
56:                 log($"Dynamo evaluation timed out after {timeout.TotalSeconds:F1}s.");
57: 
58:             return false;
59:         }
60: 
61:         return evaluationSuccess;
62:     }
63:     catch (Exception ex)
64:     {
65:         Log.Warning(ex, "Exception encountered while waiting for Dynamo evaluation");
66:         return false;
67:     }
68:     finally
69:     {
70:         model.EvaluationCompleted -= OnEvaluationCompleted;
71:     }
```

#### Observations & Flaws in `DynamoEvaluationSync`:
- **Default value trap (Line 25)**: `var evaluationSuccess = true;`. If `runAction()` finishes and `Dispatcher.PushFrame` exits without `OnEvaluationCompleted` being triggered (e.g., if evaluation did not run because the workspace is empty or manual mode was not triggered), `completed` remains `false`, but the method returns `evaluationSuccess == true`.
- **No cancellation signal sent to Dynamo engine (Lines 51-59)**: When `linkedCts.Token.IsCancellationRequested` triggers, `RunAndWait` simply exits the dispatcher frame and returns `false`. It **does not notify or abort** the running evaluation on Dynamo's engine (`model.EngineController` / ProtoCore runtime).
- **Orphaned event hazard (Line 70)**: In `finally`, `model.EvaluationCompleted -= OnEvaluationCompleted` is detached. If the engine continues evaluating in the background and completes later, its events are unobserved, and it may trigger mutations during subsequent tool executions.

---

### 1.2 Return Value Discarding in `CanvasGraphRunner.cs`
Located at `HPDynamo/HPDynamo.McpBridge/Execution/CanvasGraphRunner.cs`:

#### A. In `RunActiveGraph` (Lines 35-52):
```csharp
35: DynamoEvaluationSync.RunAndWait(
36:     viewModel.Model,
37:     () => ws.Run(),
38:     TimeSpan.FromSeconds(60),
39:     ct,
40:     log);
41: 
42: int errorCount = ws.Nodes.Count(n => n.State == ElementState.Error);
43: log($"Active graph run finished. Nodes in error: {errorCount}");
44: 
45: return new
46: {
47:     Success = errorCount == 0,
48:     WorkspaceName = ws.Name,
49:     TotalNodes = ws.Nodes.Count(),
50:     ErrorNodesCount = errorCount,
51:     HasUnsavedChanges = ws.HasUnsavedChanges
52: };
```
- Line 35: The `bool` returned by `RunAndWait` is **completely ignored**.
- If evaluation timed out after 60s, `RunAndWait` returns `false`.
- The code proceeds to line 42: `ws.Nodes.Count(n => n.State == ElementState.Error)`. If nodes did not report error state before the timeout occurred, `errorCount == 0`, and the method returns `Success = true`.

#### B. In `RunDynFile` (Lines 78-94):
```csharp
78: DynamoEvaluationSync.RunAndWait(
79:     viewModel.Model,
80:     () => ws.Run(),
81:     TimeSpan.FromSeconds(60),
82:     ct,
83:     log);
84: 
85: int errorCount = ws.Nodes.Count(n => n.State == ElementState.Error);
86: return new
87: {
88:     Success = errorCount == 0,
89:     FilePath = filePath,
90:     NodesCount = ws.Nodes.Count(),
91:     ConnectorsCount = ws.Connectors.Count(),
92:     ErrorNodesCount = errorCount
93: };
```
- Line 78: The return value is **completely ignored**. If `RunAndWait` returns `false` due to timeout or cancellation, `Success = (errorCount == 0)` can still evaluate to `true`.

#### C. In `ExecuteDesignScript` (Lines 128-145):
```csharp
128: DynamoEvaluationSync.RunAndWait(
129:     viewModel.Model,
130:     () => ws.Run(),
131:     TimeSpan.FromSeconds(30),
132:     ct,
133:     log);
134: 
135: var outputs = ExtractOutputs(viewModel.Model.EngineController, cbn);
136: log($"DesignScript executed on canvas with {outputs.Count} outputs");
137: 
138: if (dryRun)
139: {
140:     log("dryRun=true: Removing ephemeral CodeBlock from canvas");
141:     viewModel.Model.ExecuteCommand(new DynamoModel.DeleteModelCommand(cbn.GUID));
142: }
143: 
144: return new { Success = true, NodeId = cbn.GUID, Outputs = outputs };
```
- Line 128: Return value is **discarded**.
- Line 144: `Success = true` is **hardcoded**.
- Even if evaluation encountered a critical ProtoCore compilation or runtime error, `ExtractOutputs` is invoked and `{ Success = true, ... }` is returned to `RevitTransactionScope`.

---

### 1.3 Catastrophic Interaction with `RevitTransactionScope`
Located at `HPDynamo/HPDynamo.McpBridge/Execution/RevitTransactionScope.cs` (lines 138-154, 247-256):

```csharp
138: try
139: {
140:     returnValue = action(linked.Token, Logger);
141: 
142:     if (linked.Token.IsCancellationRequested)
143:         throw new OperationCanceledException(linked.Token);
144: 
145:     rolledBack = FinalizeTransactions(txContext, request, mode, isDocModifiable, Logger);
146: }
...
247: if (request.DryRun)
248: {
249:     txContext.RollbackGroup();
250:     logger("dryRun=true: All Revit document modifications rolled back cleanly.");
251:     return true;
252: }
253: 
254: txContext.AssimilateGroup();
255: return false;
```

#### The Failure Chain:
1. An evaluation fails in Dynamo (e.g., DesignScript code contains a geometry failure or invalid syntax).
2. `DynamoEvaluationSync.RunAndWait` observes `e.EvaluationSucceeded == false` and returns `false`.
3. `CanvasGraphRunner.ExecuteDesignScript` discards the `false` return value and returns `{ Success = true, Outputs = { ... } }`.
4. `action` in `RevitTransactionScope` completes normally without throwing an exception.
5. `linked.Token.IsCancellationRequested` is `false`.
6. `FinalizeTransactions` executes line 254: `txContext.AssimilateGroup()`.
7. **The Revit `TransactionGroup` is committed permanently into the Revit document!**
8. `ExecuteResult` returns `IsError = false` and reports success to the caller.
9. **Outcome**: The AI agent believes the operation succeeded, while corrupt or partial elements are committed to the building model.

---

### 1.4 Seeds Bypass Synchronization Entirely

#### A. Seed `run_dyn_file/code.cs` (Lines 78-83, 95-102):
```csharp
78: activeCanvasWs.Run();
79: 
80: totalNodesEvaluated = activeCanvasWs.Nodes.Count();
81: errorNodesCount = activeCanvasWs.Nodes.Count(n => n.State == ElementState.Error);
82: 
83: ExtractTerminalOutputs(activeCanvasWs, dynamoViewModel);
```
- Line 78 calls `activeCanvasWs.Run()` asynchronously.
- Lines 80-83 immediately count errors and extract outputs.
- Because Dynamo evaluation takes multiple frames / milliseconds, `ExtractTerminalOutputs` queries node output values **before they are calculated**. Outputs are returned as `"null"`, and errors are reported as `0`.
- Line 108 reports `success = true` based on pre-evaluation state.
- In headless mode (lines 95-102), `appInstance.ExecuteDynamoCommand` is run with no completion tracking whatsoever.

#### B. Seed `batch_edit_graph/code.cs` (Lines 222-225, 244):
```csharp
222: if (runEvaluation && homeWs != null && !dryRun)
223: {
224:     homeWs.Run();
225: }
...
244: return new
245: {
246:     success = true,
...
```
- Line 224: Fires `homeWs.Run()` and does not wait.
- Line 244: Returns `success = true` unconditionally.

---

## 2. Deep Dive: RV-03 — `linkedToken` Preservation & Routing

### 2.1 The Token Disconnect in `DynamoExecutionEngine.cs`
Located at `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoExecutionEngine.cs`:

#### Code for `RunDesignScriptAsync` (Lines 60-66):
```csharp
60: var work = new McpBridgeWorkRequest("designscript", (uiapp, ct) =>
61:     RevitTransactionScope.Execute(uiapp, request, settings, progress, cancel, (_, _, log) =>
62:     {
63:         if (_coordinator.IsLiveCanvasAttached && _coordinator.ActiveViewModel != null)
64:             return _canvasRunner.ExecuteDesignScript(_coordinator.ActiveViewModel, request.Code, request.Args, request.DryRun, ct, log);
65:         return _headlessRunner.ExecuteDesignScript(uiapp, request.Code, request.Args, request.DryRun, ct, log);
66:     }, startAutoTransaction: false), cancel.Token);
```

#### Code for `RunDynFileAsync` (Lines 79-85):
```csharp
79: var work = new McpBridgeWorkRequest("dynfile", (uiapp, ct) =>
80:     RevitTransactionScope.Execute(uiapp, request, settings, progress, cancel, (_, _, log) =>
81:     {
82:         if (_coordinator.IsLiveCanvasAttached && _coordinator.ActiveViewModel != null)
83:             return _canvasRunner.RunDynFile(_coordinator.ActiveViewModel, dynPath, request.Args, request.DryRun, ct, log);
84:         return _headlessRunner.RunDynFile(uiapp, dynPath, request.Args, request.DryRun, ct, log);
85:     }, startAutoTransaction: false), cancel.Token);
```

#### Contrast with `RunRoslynCodeAsync` (Lines 109-122):
```csharp
109: var work = new McpBridgeWorkRequest("csharp", (uiapp, ct) =>
110:     RevitTransactionScope.Execute(uiapp, request, settings, progress, cancel, (doc, linkedToken, log) =>
111:     {
...
117:         uiapp.Application, uiapp, linkedToken, log,
...
121:         return compiled.Script!.RunAsync(globals, linkedToken).GetAwaiter().GetResult().ReturnValue;
122:     }, startAutoTransaction: false), cancel.Token);
```

#### Observations & Root Cause:
1. In `RevitTransactionScope.cs` (lines 122-123):
   ```csharp
   using var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Clamp(request.TimeoutSeconds, 5, 600)));
   using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancelSource.Token, timeoutSource.Token);
   ```
   `RevitTransactionScope` encapsulates the transaction timeout budget inside `timeoutSource`. It produces `linked.Token` and forwards it to the callback:
   `action(doc, linkedToken, log)`
2. In lines 61 and 80 of `DynamoExecutionEngine.cs`:
   The signature is `(_, _, log) =>`. The second parameter (`linkedToken`) is **discarded with `_`**.
3. Instead of `linkedToken`, the runners are passed `ct` — the token of `McpBridgeWorkRequest`, which is bound solely to `cancel.Token`!
4. **Impact**:
   - If the caller sets `timeoutSeconds = 5`, `timeoutSource` cancels `linked.Token` after 5 seconds.
   - However, `ct` is **not cancelled**!
   - `CanvasGraphRunner` receives `ct`. Inside `CanvasGraphRunner`, `DynamoEvaluationSync.RunAndWait` receives `ct` and its hardcoded 30s or 60s timeout.
   - `RunAndWait` continues to pump messages for up to 60 seconds. The 5-second deadline configured by the caller is completely ignored.
   - Once `action` finally finishes at 60s, control returns to `RevitTransactionScope`, which only then realizes that `linked.Token` was cancelled 55 seconds ago.

---

### 2.2 Tool Dispatch Routing Mismatch & ScriptGuard Barrier

#### The Dispatch Mismatch:
In `DynamoExecutionEngine.cs`:
```csharp
46: if (request.Label?.StartsWith("DynFile", StringComparison.OrdinalIgnoreCase) == true)
47: {
48:     return await RunDynFileAsync(request, settings, progress, cancel).ConfigureAwait(false);
49: }
50: 
51: return await RunRoslynCodeAsync(request, settings, progress, cancel).ConfigureAwait(false);
```
- In `McpShared/HPRebar.Mcp.Server.Core/Registry/ToolManager.cs` (line 174):
  `var request = new ExecuteRequest(record.Code, record.Transaction, dryRun, timeout, name, args);`
  For the seed tool `run_dyn_file`, `name` is `"run_dyn_file"`.
- `"run_dyn_file".StartsWith("DynFile")` is **`false`**.
- Therefore, calling the MCP tool `run_dyn_file` **never executes `RunDynFileAsync` or `CanvasGraphRunner.RunDynFile`**.
- It falls through to line 51: `RunRoslynCodeAsync`, compiling `Registry/SeedLibrary/Graph/run_dyn_file/code.cs` as a Roslyn script.

#### The ScriptGuard Barrier:
In `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs` (line 246):
```csharp
243: deniedNamespaces: new[]
244: {
245:     "System.Windows.Forms",
246:     "HPDynamo.McpBridge",
247:     "HPRebar.McpBridge.Core.Host",
248: }
```
- `HPDynamo.McpBridge` is an **explicitly denied namespace** in `GuardProfile.Dynamo`!
- Seed scripts cannot call `HPDynamo.McpBridge.Execution.DynamoEvaluationSync.RunAndWait`. Any attempt triggers AST rejection by `ScriptGuard`.
- Because `DynamoScriptGlobals` does not expose an evaluation synchronization helper, seed authors were unable to synchronize evaluations, leading to the raw `Run()` calls.

---

## 3. Deep Dive: RV-05 — Work Item State Machine & Revit Thread Ownership

### 3.1 Trace of Work Item Lifecycle & Premature Cancellation
Located at `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs`:

```csharp
123: public async Task<ExecuteResult> ExecuteAsync(
124:     ExecuteRequest request,
125:     IProgress<ScriptProgress>? progress,
126:     CancellationToken cancellationToken)
127: {
128:     if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
129:         return ExecuteResult.Failure("Another Dynamo script or graph is still running.");
130: 
131:     var sw = Stopwatch.StartNew();
132:     StateChanged?.Invoke();
133: 
134:     try
135:     {
136:         var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
137:         _currentCancel = cancel;
138: 
139:         try
140:         {
141:             var result = await DispatchExecutionAsync(request, progress, cancel).ConfigureAwait(false);
142:             return Finish(request, result, sw);
143:         }
144:         finally
145:         {
146:             _currentCancel = null;
147:             cancel.Dispose();
148:         }
149:     }
150:     catch (Exception ex)
151:     {
152:         Log.Error(ex, "Execution failed in DynamoBridgeCoordinator");
153:         var res = ExecuteResult.Failure(SafeText.StripPaths($"{ex.GetType().Name}: {ex.Message}"));
154:         return Finish(request, res, sw);
155:     }
156:     finally
157:     {
158:         Volatile.Write(ref _busy, 0);
159:         StateChanged?.Invoke();
160:     }
161: }
```

Now trace `RunOnRevitThreadAsync` (lines 255-281):
```csharp
255: internal Task<object?> RunOnRevitThreadAsync(McpBridgeWorkRequest request)
256: {
257:     if (_externalEvent == null)
258:     {
259:         request.Completion.TrySetException(new InvalidOperationException("ExternalEvent is not available."));
260:         return request.Completion.Task;
261:     }
262: 
263:     if (request.CancellationToken.IsCancellationRequested)
264:     {
265:         request.Completion.TrySetCanceled(request.CancellationToken);
266:         return request.Completion.Task;
267:     }
268: 
269:     _pending.Enqueue(request);
270:     var reg = request.CancellationToken.Register(() => request.Completion.TrySetCanceled(request.CancellationToken));
271:     request.Completion.Task.ContinueWith(_ => reg.Dispose(), TaskScheduler.Default);
272: 
273:     var accepted = _externalEvent.Raise();
274:     if (!accepted)
275:     {
276:         request.Completion.TrySetException(new InvalidOperationException(
277:             "Revit refused the request: modal dialog or busy state."));
278:     }
279: 
280:     return request.Completion.Task;
281: }
```

And trace `Execute(UIApplication application)` on Revit Main Thread (lines 208-231):
```csharp
208: public void Execute(UIApplication application)
209: {
210:     SetActiveDocumentTitle(application.ActiveUIDocument?.Document?.Title);
211: 
212:     while (_pending.TryDequeue(out var request))
213:     {
214:         if (request.Completion.Task.IsCompleted) continue;
215:         if (request.CancellationToken.IsCancellationRequested)
216:         {
217:             request.Completion.TrySetCanceled(request.CancellationToken);
218:             continue;
219:         }
220: 
221:         try
222:         {
223:             request.Completion.TrySetResult(request.Work(application, request.CancellationToken));
224:         }
225:         catch (Exception ex)
226:         {
227:             Log.Error(ex, "Dynamo bridge work '{Name}' threw on Revit thread", request.Name);
228:             request.Completion.TrySetException(ex);
229:         }
230:     }
231: }
```

---

### 3.2 Sequence of Events During Cancellation While Running on Revit Thread

```
Pipe Thread (ExecuteAsync)                 Caller/Cancel Tool               Revit Main UI Thread
-------------------------                  ------------------               ---------------------
_busy = 1
_pending.Enqueue(work)
await DispatchExecutionAsync()
   (awaiting work.Completion.Task)
                                                                            Dequeues work from _pending
                                                                            Begins work.Work(app, ct)
                                                                            -> RevitTransactionScope begins tx
                                                                            -> Dynamo nodes executing...
                                           Calls Cancel()
                                           -> cancel.Cancel()
                                           -> reg callback fires:
                                              work.Completion.
                                                TrySetCanceled()!
work.Completion.Task is CANCELED!
await throws TaskCanceledException!
Enters catch (Exception ex)
Enters finally:
  Volatile.Write(ref _busy, 0); <--- !!!                                    STILL RUNNING work.Work!
  _busy IS NOW 0!                                                           Revit thread is mutating model!
Finish() returns Failure result                                             Has NOT even begun SafeRollback!
---------------------------------------------------------------------------------------------------------
NEW REQUEST ARRIVES OVER PIPE:
ExecuteAsync(newReq)
Interlocked.CompareExchange(_busy, 1, 0)
SUCCEEDS! _busy = 1 for newReq!
newReq enqueued to _pending
_externalEvent.Raise()!
                                                                            Previous work catches OperationCanceledException
                                                                            Calls SafeRollback(txContext)...
                                                                            Previous work finishes rollback
                                                                            Loops to while (_pending.TryDequeue)
                                                                            DEQUEUES newReq ON SAME TICK!
                                                                            Races or conflicts with Revit state!
```

#### Detailed Failure Mechanics:
1. **Premature Task Completion (Line 270)**: `request.CancellationToken.Register(() => request.Completion.TrySetCanceled(...))` does not distinguish between a work item that is waiting in `_pending` (`Queued`) and a work item that is actively executing on the Revit UI thread (`Running`).
2. **Premature Release of `_busy` (Line 158)**: Because `Completion.Task` completes as canceled immediately, `await DispatchExecutionAsync` terminates on the pipe thread. The outer `finally` block runs and executes `Volatile.Write(ref _busy, 0)`.
3. **Loss of Thread Ownership**: The bridge reports to the outside world that it is idle (`_busy == 0`), while the Revit main thread is still executing inside `request.Work`, modifying the Revit document or executing `SafeRollback(txContext)`.
4. **Premature `RunCompleted` Emission**: `Finish(request, res, sw)` invokes `RunCompleted` before Revit has actually rolled back the document changes. The audit log records the run as finished while modifications are still taking place.
5. **Reentrancy Risk**: If `Dispatcher.PushFrame` is running inside `RunAndWait` when the new request arrives, message pumping can cause unpredictable reentrancy.

---

### 3.3 The Coordinator `Dispose()` Gap
Located at lines 235-239 of `DynamoBridgeCoordinator.cs`:
```csharp
235: public void Dispose()
236: {
237:     _currentCancel?.Cancel();
238:     _externalEvent?.Dispose();
239: }
```
- When `DynamoBridgeCoordinator` is disposed (e.g., during Revit shutdown or add-in unload), any work items currently queued in `_pending` (such as `GetContextAsync` calls) are **never completed or cancelled**.
- `_pending` is not drained.
- Any caller awaiting `RunOnRevitThreadAsync` on another thread hangs indefinitely.

---

## 4. Concrete Recommendations & Architecture Solutions

### 4.1 Recommendation for RV-02 (Evaluation Synchronization)

1. **Verify and Propagate `RunAndWait` Outcome in `CanvasGraphRunner`**:
   - Check the `bool` return value of `DynamoEvaluationSync.RunAndWait`.
   - If `RunAndWait` returns `false`:
     - Inspect whether `cancellationToken.IsCancellationRequested` is true. If so, throw `OperationCanceledException(ct)`.
     - Otherwise, throw `InvalidOperationException($"Dynamo evaluation failed or timed out: {failureDetails}")`.
   - By throwing an exception, control immediately enters `RevitTransactionScope`'s `catch` blocks, triggering `SafeRollback(txContext)` and returning `ExecuteResult.Failure`.
2. **Remove Hardcoded `Success = true` in `ExecuteDesignScript`**:
   - Only return `Success = true` if `RunAndWait` returned `true` AND no error nodes were generated.
3. **Abort Running Engine Evaluation on Timeout/Cancel**:
   - In `DynamoEvaluationSync.RunAndWait`, when cancellation/timeout is requested, signal Dynamo's `EngineController` or workspace execution to halt evaluation (or set workspace run settings to prevent further node execution).
4. **Expose Evaluation Sync to Seeds via `DynamoScriptGlobals`**:
   - Add a method to `DynamoScriptGlobals`:
     ```csharp
     public bool RunAndWait(TimeSpan? timeout = null)
     {
         var model = (dynamo as DynamoViewModel)?.Model ?? (dynamo as DynamoModel);
         if (model == null || workspace is not HomeWorkspaceModel homeWs) return false;
         return DynamoEvaluationSync.RunAndWait(model, () => homeWs.Run(), timeout ?? TimeSpan.FromSeconds(60), ct, log);
     }
     ```
   - This allows seeds `run_dyn_file` and `batch_edit_graph` to perform synchronous runs cleanly without violating `GuardProfile.Dynamo`.

---

### 4.2 Recommendation for RV-03 (`linkedToken` & Label Routing)

1. **Capture `linkedToken` in `DynamoExecutionEngine`**:
   - In `DynamoExecutionEngine.cs`:
     - Line 61: Change `(_, _, log) =>` to `(_, linkedToken, log) =>`.
     - Line 64: Pass `linkedToken` instead of `ct` to `_canvasRunner.ExecuteDesignScript` and `_headlessRunner.ExecuteDesignScript`.
     - Line 80: Change `(_, _, log) =>` to `(_, linkedToken, log) =>`.
     - Line 83: Pass `linkedToken` instead of `ct` to `_canvasRunner.RunDynFile` and `_headlessRunner.RunDynFile`.
2. **Align Tool Routing or Route via Label**:
   - In `DynamoExecutionEngine.DispatchAsync`:
     - Recognize `request.Label?.Equals("run_dyn_file", StringComparison.OrdinalIgnoreCase) == true` or provide the native helper on `DynamoScriptGlobals`.

---

### 4.3 Recommendation for RV-05 (Work Item State Machine & Queue Ownership)

1. **Implement Explicit 3-State Work Item State Machine**:
   ```csharp
   internal enum WorkItemState
   {
       Queued = 0,
       Running = 1,
       Completed = 2
   }
   ```
2. **Refactor `McpBridgeWorkRequest`**:
   ```csharp
   internal sealed class McpBridgeWorkRequest
   {
       public string Name { get; }
       public Func<UIApplication, CancellationToken, object?> Work { get; }
       public CancellationToken CancellationToken { get; }
       public TaskCompletionSource<object?> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

       private int _state = (int)WorkItemState.Queued;
       public WorkItemState State => (WorkItemState)Volatile.Read(ref _state);

       public bool TryTransitionToRunning() =>
           Interlocked.CompareExchange(ref _state, (int)WorkItemState.Running, (int)WorkItemState.Queued) == (int)WorkItemState.Queued;

       public bool TryTransitionToCompleted() =>
           Interlocked.Exchange(ref _state, (int)WorkItemState.Completed) != (int)WorkItemState.Completed;

       public bool TryCancelIfQueued()
       {
           if (Interlocked.CompareExchange(ref _state, (int)WorkItemState.Completed, (int)WorkItemState.Queued) == (int)WorkItemState.Queued)
           {
               Completion.TrySetCanceled(CancellationToken);
               return true;
           }
           return false;
       }
   }
   ```
3. **Refactor `RunOnRevitThreadAsync`**:
   - When cancellation triggers on `request.CancellationToken`:
     - Call `request.TryCancelIfQueued()`.
     - If the item was already in state `Running`, **do not call `TrySetCanceled`**. The caller must continue to await `request.Completion.Task` until the Revit UI thread completes execution and finishes rollback.
4. **Refactor `Execute(UIApplication application)` on Revit Thread**:
   ```csharp
   while (_pending.TryDequeue(out var request))
   {
       if (request.State == WorkItemState.Completed || request.Completion.Task.IsCompleted)
           continue;

       if (request.CancellationToken.IsCancellationRequested)
       {
           request.TryCancelIfQueued();
           continue;
       }

       if (!request.TryTransitionToRunning())
           continue;

       try
       {
           var result = request.Work(application, request.CancellationToken);
           request.TryTransitionToCompleted();
           request.Completion.TrySetResult(result);
       }
       catch (Exception ex)
       {
           request.TryTransitionToCompleted();
           request.Completion.TrySetException(ex);
       }
   }
   ```
5. **Drain `_pending` in `Dispose()`**:
   ```csharp
   public void Dispose()
   {
       _currentCancel?.Cancel();
       _externalEvent?.Dispose();
       while (_pending.TryDequeue(out var req))
       {
           req.Completion.TrySetException(new ObjectDisposedException(nameof(DynamoBridgeCoordinator), "Bridge coordinator has been disposed."));
       }
   }
   ```

---

## 5. Summary Table: Defects, Locations, Triggers, and Impacts

| Finding | File & Line | Root Defect | Trigger Condition | Impact |
|---|---|---|---|---|
| **RV-02** | `CanvasGraphRunner.cs:35,78,128` | Discards `RunAndWait` return value; hardcodes `Success = true` | DesignScript syntax error, node failure, or timeout $< 30$s | Commits broken elements via `txContext.AssimilateGroup()` to Revit; reports false success |
| **RV-02** | `run_dyn_file/code.cs:78-83` | Calls `activeCanvasWs.Run()` asynchronously without sync | Running `.dyn` file via MCP | Reads outputs before evaluation completes (`"null"`); reports success prematurely |
| **RV-02** | `batch_edit_graph/code.cs:224,244` | Calls `homeWs.Run()` asynchronously without sync | Running `batch_edit_graph` with `runEvaluation=true` | Unconditionally returns `success = true`, ignoring evaluation errors |
| **RV-03** | `DynamoExecutionEngine.cs:61,80` | Discards `linkedToken` with `_`, passes raw `ct` | Executing DesignScript or DynFile with custom `timeoutSeconds` | Request timeout is ignored; runner runs up to 60s hardcoded timeout |
| **RV-03** | `ToolManager.cs:174` & `DynamoExecutionEngine.cs:46` | Label is `"run_dyn_file"` (not `StartsWith("DynFile")`); `GuardProfile.Dynamo` denies `HPDynamo.McpBridge` | Running `run_dyn_file` seed | Bypasses `RunDynFileAsync`; cannot reference `DynamoEvaluationSync` |
| **RV-05** | `DynamoBridgeCoordinator.cs:270` | Registers `TrySetCanceled` for all work requests indiscriminately | Client aborts request or calls `cancel_execution` during run | `_busy = 0` set prematurely; new requests run while Revit is still executing/rolling back |
| **RV-05** | `DynamoBridgeCoordinator.cs:235-239` | `Dispose()` does not drain `_pending` queue | Bridge unloads or Revit shuts down | Queued context requests hang indefinitely |
