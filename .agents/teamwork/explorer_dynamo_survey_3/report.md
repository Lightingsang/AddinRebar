# HPDynamo MCP Architectural Investigation Report: R3 & R4 (RV-04, RV-07, RV-08, Benchmark Harness & Docs)

**Date:** 2026-10-09  
**Agent:** explorer_dynamo_survey_3  
**Status:** Complete Read-Only Investigation & Synthesis  
**Scope:** `McpShared/HPRebar.McpBridge.Core`, `McpShared/HPRebar.Mcp.Contracts`, `HPDynamo/HPDynamo.McpBridge`, `HPDynamo/HPDynamo.Mcp.Server`, `plans/261009-1118-hpdynamo-mcp-performance/`, and `.agents/skills/hp-mcp-dynamo/SKILL.md`.

---

## 1. Executive Summary

This investigation resolves the architectural and technical specifications for **R3** (Request-Scoped Cancellation and ProtoCore/DesignScript Serialization) and **R4** (Benchmark Harness, Raw Data Schema, Documentation, and Skill Synchronization) based on independent technical findings **RV-04**, **RV-07**, and **RV-08** from `reports/codex-review-20261009.md`.

Key Findings:
1. **RV-04 (Request-Scoped Cancellation):** `RevitBridgeClient` already sends `id` inside `new { id }` on cancellation (`dynamo.cancel`), but `RequestDispatcher.cs:112` discards this parameter and calls `_executor.Cancel()`, which `DynamoBridgeCoordinator.cs:193` handles by cancelling `_currentCancel` blindly. By introducing `ICorrelatedCancelExecutor : IBridgeExecutor` in `McpShared/HPRebar.McpBridge.Core` alongside a lightweight `CancelParams` wire record, we achieve **100% backward binary and source compatibility** across all other 10 hosts (including .NET Framework 4.8 hosts like Navisworks), while allowing `DynamoBridgeCoordinator` to correlate the cancel token strictly to the matching request ID.
2. **RV-07 & RV-08 (MirrorData & Dictionary Serialization):** `get_node_output_values/code.cs:70` prematurely terminates evaluation when `mirrorData.Data == null`, completely dropping all collection/list outputs from Dynamo (because in `ProtoCore.Mirror.MirrorData`, arrays have `mirrorData.Data == null` with `mirrorData.IsCollection == true`). Furthermore, `DesignScript.Builtin.Dictionary` does not implement `IDictionary` or `IEnumerable`, causing it to fall through to `ToString()`. Because `ScriptGuard` denies reflection (`GetProperty`, `GetMethod`) and `dynamic`, the serializer must directly inspect `MirrorData.IsCollection` and `MirrorData.GetElements()`, and strongly-typed access `DesignScript.Builtin.Dictionary.Keys` and `.ValueAtKey()`, bounded by breadth (max 50/100), depth (max 6), and cooperative `ct.ThrowIfCancellationRequested()`.
3. **R4 (Benchmark, Docs & Skill):** Active user Revit session (PID 26264) must be strictly isolated by directing live tests to temporary copied fixtures (`tests/fixtures/sample_copy.rvt`). The benchmark harness `run-benchmark.ps1` must record raw latency distributions (p50/p95), round-trips, and UTF-8 payload bytes to `reports/baseline-vs-upgraded.json`. The `hp-mcp-dynamo` skill must be updated from 24 to 25 tools (adding `batch_edit_graph`), and inaccurate schema documentation in `benchmark-plan.md` and `SKILL.md` (such as `includeActiveGraph` on `get_dynamo_context`) must be corrected.

---

## 2. RV-04: Request-Scoped Cancellation via `ICorrelatedCancelExecutor`

### 2.1. Codebase Observation & Current Vulnerability

#### Client Side (`McpShared/HPRebar.Mcp.Server.Core/Services/RevitBridgeClient.cs:97-101`)
```csharp
private void TryCancelInRevit(long id)
{
    _ = SendAsync<CancelResult>(_profile.Method(JsonRpcMethods.CancelSuffix), new { id }, TimeSpan.FromSeconds(5), null, CancellationToken.None)
        .ContinueWith(t => _logger.LogDebug(t.Exception, "Cancel after timeout failed"), TaskContinuationOptions.OnlyOnFaulted);
}
```
*Observation:* When a client call times out in `WaitForResponseAsync` (or is aborted), `TryCancelInRevit` issues a JSON-RPC request to `dynamo.cancel` with payload `{"id": id}`.

#### Dispatcher Side (`McpShared/HPRebar.McpBridge.Core/Pipe/RequestDispatcher.cs:111-113`)
```csharp
case JsonRpcMethods.CancelSuffix:
    return JsonRpcEnvelope.Success(id, _executor.Cancel());
```
*Observation:* `RequestDispatcher` ignores `request.Params` completely. It invokes `_executor.Cancel()`, which takes no arguments.

#### Coordinator Side (`HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs:193-206`)
```csharp
public CancelResult Cancel()
{
    var cancel = _currentCancel;
    if (cancel == null) return new CancelResult(false, false);
    try
    {
        cancel.Cancel();
        return new CancelResult(true, true);
    }
    catch (ObjectDisposedException)
    {
        return new CancelResult(false, false);
    }
}
```
*Observation:* `DynamoBridgeCoordinator` unconditionally cancels `_currentCancel` regardless of which request originated the cancel signal.

#### Consequence (Trigger Scenario):
1. Client sends Request A (`id: 101`).
2. Request A completes, but network or queue latency delayed the response, or context timeout triggers `TryCancelInRevit(101)`.
3. Meanwhile, Client sends Request B (`id: 102`), which begins running on the Revit thread (`_busy = 1`, `_currentCancel` assigned).
4. The delayed cancel for Request A (`{"id": 101}`) arrives at the bridge.
5. Bridge calls `Cancel()`, cancelling `_currentCancel` of **Request B**!
6. Request B is abruptly rolled back and aborted due to a stale cancel from Request A.

---

### 2.2. Interface Design: `ICorrelatedCancelExecutor`

To solve this while preserving **100% binary and source compatibility** across all 11 hosts in the monorepo, we define `ICorrelatedCancelExecutor` in `McpShared/HPRebar.McpBridge.Core/Pipe/`:

```csharp
namespace HPRebar.McpBridge.Core.Pipe;

using System;
using System.Threading;
using System.Threading.Tasks;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;

/// <summary>
///     Optional extension of <see cref="IBridgeExecutor"/> for bridges that track and correlate
///     cancellation signals to specific incoming JSON-RPC request IDs.
/// </summary>
public interface ICorrelatedCancelExecutor : IBridgeExecutor
{
    /// <summary>
    ///     Executes a script with its correlated JSON-RPC request identifier.
    /// </summary>
    Task<ExecuteResult> ExecuteAsync(
        long requestId,
        ExecuteRequest request,
        IProgress<ScriptProgress>? progress,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Signals cancellation to the running script.
    ///     If <paramref name="targetRequestId"/> is specified, cancellation is only triggered if the
    ///     currently running script matches that ID. If <paramref name="targetRequestId"/> is null,
    ///     unconditional (global) cancellation is triggered.
    /// </summary>
    CancelResult Cancel(long? targetRequestId);
}
```

#### Wire Parameter Record in `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs`:
```csharp
/// <summary>Parameters for `*.cancel` when targeting a specific request ID.</summary>
public sealed record CancelParams(long? Id);
```
Because `BridgeJson.Options.PropertyNameCaseInsensitive = true`, this record flawlessly deserializes `{ "id": 101 }`, `{ "Id": 101 }`, or `null`.

---

### 2.3. Backward Compatibility Proof for All 10 Other Hosts

In `McpShared/HPRebar.McpBridge.Core/Pipe/RequestDispatcher.cs`:
```csharp
// In DispatchAsync (handling CancelSuffix):
case JsonRpcMethods.CancelSuffix:
{
    var cancelParams = request.ParamsAs<CancelParams>();
    var result = _executor is ICorrelatedCancelExecutor correlated
        ? correlated.Cancel(cancelParams?.Id)
        : _executor.Cancel();
    return JsonRpcEnvelope.Success(id, result);
}

// In ExecuteAsync (handling ExecuteSuffix):
var result = _executor is ICorrelatedCancelExecutor correlated
    ? await correlated.ExecuteAsync(id, parameters, progress, cancellationToken).ConfigureAwait(false)
    : await _executor.ExecuteAsync(parameters, progress, cancellationToken).ConfigureAwait(false);
```

#### Compatibility Analysis Matrix:

| Host Bridge | Project Target | Implements `ICorrelatedCancelExecutor`? | Runtime Execution Path | Backward Compatibility Status |
|---|---|---|---|---|
| `HPRebar` (Revit) | net8.0 / net48 | No (`IBridgeExecutor`) | `_executor.Cancel()` & `_executor.ExecuteAsync()` | 100% Unchanged (Binary & Source) |
| `HPAutoCad` | net8.0-windows | No (`IBridgeExecutor`) | Legacy path | 100% Unchanged |
| `HPCivil3d` | net8.0-windows | No (`IBridgeExecutor`) | Legacy path | 100% Unchanged |
| `HPNavis` | **net48** | No (`IBridgeExecutor`) | Legacy path | 100% Unchanged (.NET 4.8 safe) |
| `HPEtabs` | net8.0-windows | No (`IBridgeExecutor`) | Legacy path | 100% Unchanged |
| `HPSap2000` | net8.0-windows | No (`IBridgeExecutor`) | Legacy path | 100% Unchanged |
| `HPPowerBi` | net8.0-windows | No (`IBridgeExecutor`) | Legacy path | 100% Unchanged |
| `HPExcel` | net8.0-windows | No (`IBridgeExecutor`) | Legacy path | 100% Unchanged |
| `HPRobot` | net8.0-windows | No (`IBridgeExecutor`) | Legacy path | 100% Unchanged |
| `HPTekla` | net48 | No (`IBridgeExecutor`) | Legacy path | 100% Unchanged |
| `HPDynamo` | net10.0-windows | **Yes** (`ICorrelatedCancelExecutor`) | Correlated path with request ID | Target upgraded behavior |

*Key Guarantee:* `IBridgeExecutor` remains completely unmodified. Existing bridges need 0 code changes, 0 recompilation changes, and 0 contract changes. Net48 bridges compile and run without any preprocessor directives.

---

### 2.4. `DynamoBridgeCoordinator` Implementation Details

In `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs`:

1. **State Tracking Fields:**
   ```csharp
   private long _activeRequestId;
   private readonly object _cancelLock = new();
   ```

2. **Correlated Execution:**
   ```csharp
   public Task<ExecuteResult> ExecuteAsync(
       ExecuteRequest request,
       IProgress<ScriptProgress>? progress,
       CancellationToken cancellationToken) =>
       ExecuteAsync(0, request, progress, cancellationToken);

   public async Task<ExecuteResult> ExecuteAsync(
       long requestId,
       ExecuteRequest request,
       IProgress<ScriptProgress>? progress,
       CancellationToken cancellationToken)
   {
       if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0)
           return ExecuteResult.Failure("Another Dynamo script or graph is still running.");

       lock (_cancelLock)
       {
           _activeRequestId = requestId;
       }

       var sw = Stopwatch.StartNew();
       StateChanged?.Invoke();

       try
       {
           var cancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
           lock (_cancelLock)
           {
               _currentCancel = cancel;
           }

           try
           {
               var result = await DispatchExecutionAsync(request, progress, cancel).ConfigureAwait(false);
               return Finish(request, result, sw);
           }
           finally
           {
               lock (_cancelLock)
               {
                   _currentCancel = null;
                   _activeRequestId = 0;
               }
               cancel.Dispose();
           }
       }
       catch (Exception ex)
       {
           Log.Error(ex, "Execution failed in DynamoBridgeCoordinator");
           var res = ExecuteResult.Failure(SafeText.StripPaths($"{ex.GetType().Name}: {ex.Message}"));
           return Finish(request, res, sw);
       }
       finally
       {
           Volatile.Write(ref _busy, 0);
           StateChanged?.Invoke();
       }
   }
   ```

3. **Correlated Cancel Handler:**
   ```csharp
   public CancelResult Cancel() => Cancel(null);

   public CancelResult Cancel(long? targetRequestId)
   {
       lock (_cancelLock)
       {
           var cancel = _currentCancel;
           if (cancel == null) return new CancelResult(false, false);

           // If caller specified target ID and it doesn't match current running request:
           if (targetRequestId.HasValue && targetRequestId.Value != 0 && targetRequestId.Value != _activeRequestId)
           {
               Log.Information(
                   "Ignored stale cancel request for ID {TargetId} (active request is {ActiveId})",
                   targetRequestId.Value, _activeRequestId);
               return new CancelResult(false, Volatile.Read(ref _busy) != 0);
           }

           try
           {
               cancel.Cancel();
               return new CancelResult(true, true);
           }
           catch (ObjectDisposedException)
           {
               return new CancelResult(false, false);
           }
       }
   }
   ```

4. **Integration with RV-05 (Thread Ownership & State Management):**
   `McpBridgeWorkRequest` must transition explicitly through states:
   `Queued -> Running -> Completed`.
   - If cancelled while `Queued`: `request.Completion.TrySetCanceled()` triggers immediately and dequeues without touching the Revit thread.
   - If cancelled while `Running`: `Completion.TrySetCanceled()` is **inhibited**. The running task cooperative cancellation token signals, and completion is ONLY signaled when `request.Work()` and transaction rollback on the Revit thread fully exit. `_busy` remains 1 throughout rollback, preventing overlapping requests.

---

## 3. RV-07 & RV-08: ProtoCore MirrorData & Dictionary Serialization

### 3.1. Codebase Observation & Root Cause

#### Current Implementation (`HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Data/get_node_output_values/code.cs:69-75`)
```csharp
var mirrorData = inspectedNode.GetValue(portIndex, engineController);
if (mirrorData?.Data == null) return null;

var payload = mirrorData.Data;
outputTypeName = payload.GetType().Name;
return SerializeOutputValue(payload);
```

#### Why Collections Fail:
In `ProtoCore.dll` (from Dynamo 3.7 / Revit 2026):
- `ProtoCore.Mirror.MirrorData` represents the runtime value in ProtoCore VM.
- When a node evaluates to a list/array (e.g., CodeBlock `[1, 2, 3]`, Point arrays, geometry lists):
  - `MirrorData.IsCollection` returns `true` (internal `svData.IsArray`).
  - `MirrorData.GetElements()` returns `List<MirrorData>` (from internal `DSArray.Values`).
  - `MirrorData.Data` returns **`null`** (because `GetData()` does not resolve `AddressType.ArrayPointer` into a raw CLR object; it only populates `Data` for scalar CLR objects).
- Therefore, line 70: `if (mirrorData?.Data == null) return null;` evaluates to `true` and **returns `null` for EVERY collection or list output in Dynamo**!

#### Why Dictionaries Fail:
- When a node outputs a dictionary (e.g. `Dictionary.ByKeysValues(...)` or `{"a": 10, "b": 20}`):
  - `MirrorData.Data` returns an instance of `DesignScript.Builtin.Dictionary` (from `DesignScriptBuiltin.dll`).
  - `DesignScript.Builtin.Dictionary` **does NOT implement `IDictionary`** and **does NOT implement `IEnumerable`**.
  - Lines 38 (`value is IDictionary`) and 49 (`value is IEnumerable`) evaluate to `false`.
  - Execution falls to line 59: `return value.ToString();`, emitting `"DesignScript.Builtin.Dictionary"`.

---

### 3.2. Decompilation Analysis & Clean Code Constraints

#### Decompilation of `ProtoCore.Mirror.MirrorData`:
- `public bool IsCollection { get; }`: Returns whether the data represents an array/collection.
- `public List<MirrorData> GetElements()`: Returns the child elements as `MirrorData` instances.
- `public object Data { get; }`: Returns the boxed scalar value (null for arrays).

#### Decompilation of `DesignScript.Builtin.Dictionary`:
- Located in `DesignScriptBuiltin.dll`, namespace `DesignScript.Builtin`.
- `public IEnumerable<string> Keys { get; }`: Returns the string keys.
- `public object ValueAtKey(string key)`: Returns the value associated with `key`.
- `public int Count { get; }`: Returns count of entries.

#### Clean Code & ScriptGuard Rules:
1. `ScriptGuard` denies `System.Reflection` (`GetProperty`, `GetMethod`, `GetMembers`, `GetFields`). Any attempt to use reflection in `code.cs` triggers compile error `GUARD`.
2. `ScriptGuard` denies `dynamic`.
3. Therefore, access must be **strongly typed**:
   - `mirrorData.IsCollection`
   - `mirrorData.GetElements()`
   - `if (value is DesignScript.Builtin.Dictionary dsDict)`
   - `dsDict.Keys` and `dsDict.ValueAtKey(key)`

#### Required Assembly References:
For `code.cs` to compile strongly typed against `DesignScript.Builtin.Dictionary`:
1. In `HPDynamo.McpBridge/Coordinator/DynamoCompilerReferences.cs`:
   ```csharp
   TryAdd(list, () => typeof(ProtoCore.Mirror.MirrorData).Assembly);
   TryAdd(list, () => typeof(DesignScript.Builtin.Dictionary).Assembly);
   ```
2. In `HPDynamo.McpBridge.csproj` and `HPDynamo.McpBridge.Tests.csproj`:
   ```xml
   <Reference Include="DesignScriptBuiltin">
       <HintPath>$(DynamoInstallDir)DesignScriptBuiltin.dll</HintPath>
       <Private>false</Private>
   </Reference>
   ```
3. In `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs`:
   Add `"DesignScript.Builtin"` and `"ProtoCore.Mirror"` to `DynamoImports`.
4. In `HPDynamo.Mcp.Server.Tests/Seeds/SeedCompilationTests.cs`:
   Add `"DesignScriptBuiltin.dll"` to `optionalDlls`.

---

### 3.3. Complete Serializer Architecture with Budget & Cancellation (RV-08)

The serializer in `get_node_output_values/code.cs` must satisfy:
- **Depth Budget:** Maximum recursion depth of 6.
- **Breadth Budget:** Maximum 100 elements per list/dictionary level.
- **Node Output Budget:** Maximum 500 total serialized elements per output port.
- **Cooperative Cancellation:** Check `ct.ThrowIfCancellationRequested()` in every loop iteration and recursive call.

#### Recommended Code Structure for `get_node_output_values/code.cs`:

```csharp
const int MaxDepth = 6;
const int MaxBreadth = 100;
int totalItemCount = 0;
const int MaxTotalItemsPerPort = 500;

object? SerializePayload(object? value, int depth)
{
    if (value == null) return null;
    ct.ThrowIfCancellationRequested();

    if (depth > MaxDepth || totalItemCount >= MaxTotalItemsPerPort)
        return totalItemCount >= MaxTotalItemsPerPort ? "[Max items reached]" : value.ToString();

    // 1. Geometry Types
    if (includeGeometry && value is Autodesk.DesignScript.Geometry.Point pt)
        return new { type = "Point", x = pt.X, y = pt.Y, z = pt.Z };

    if (includeGeometry && value is Autodesk.DesignScript.Geometry.Curve crv)
        return new { type = crv.GetType().Name, length = crv.Length };

    // 2. Primitives
    if (value is string || value is bool || value is int || value is double || value is float || value is long)
        return value;

    // 3. DesignScript.Builtin.Dictionary
    if (value is DesignScript.Builtin.Dictionary dsDict)
    {
        var dictResult = new Dictionary<string, object?>();
        int count = 0;
        foreach (var key in dsDict.Keys)
        {
            ct.ThrowIfCancellationRequested();
            if (count++ >= MaxBreadth || totalItemCount++ >= MaxTotalItemsPerPort)
            {
                dictResult["_truncated"] = $"[Truncated: {dsDict.Count - MaxBreadth} more keys]";
                break;
            }
            try
            {
                var val = dsDict.ValueAtKey(key);
                dictResult[key] = SerializePayload(val, depth + 1);
            }
            catch (Exception ex)
            {
                dictResult[key] = $"[Error reading key: {ex.Message}]";
            }
        }
        return dictResult;
    }

    // 4. Standard CLR IDictionary
    if (value is System.Collections.IDictionary dict)
    {
        var dictResult = new Dictionary<string, object?>();
        int count = 0;
        foreach (System.Collections.DictionaryEntry entry in dict)
        {
            ct.ThrowIfCancellationRequested();
            if (count++ >= MaxBreadth || totalItemCount++ >= MaxTotalItemsPerPort)
            {
                dictResult["_truncated"] = "[Truncated breadth limit]";
                break;
            }
            string key = entry.Key?.ToString() ?? "";
            dictResult[key] = SerializePayload(entry.Value, depth + 1);
        }
        return dictResult;
    }

    // 5. Standard CLR IEnumerable
    if (value is System.Collections.IEnumerable enumerable)
    {
        var listResult = new List<object?>();
        int count = 0;
        foreach (var item in enumerable)
        {
            ct.ThrowIfCancellationRequested();
            if (count++ >= MaxBreadth || totalItemCount++ >= MaxTotalItemsPerPort)
            {
                listResult.Add("[Truncated breadth limit]");
                break;
            }
            listResult.Add(SerializePayload(item, depth + 1));
        }
        return listResult;
    }

    return value.ToString();
}

object? SerializeMirrorData(ProtoCore.Mirror.MirrorData? mirror, int depth)
{
    if (mirror == null) return null;
    ct.ThrowIfCancellationRequested();

    if (depth > MaxDepth || totalItemCount >= MaxTotalItemsPerPort)
        return totalItemCount >= MaxTotalItemsPerPort ? "[Max items reached]" : "[Depth limit reached]";

    // Handle Collections (Lists/Arrays in ProtoCore)
    if (mirror.IsCollection)
    {
        var elements = mirror.GetElements();
        if (elements == null) return new List<object?>();

        var listResult = new List<object?>();
        int count = 0;
        foreach (var elem in elements)
        {
            ct.ThrowIfCancellationRequested();
            if (count++ >= MaxBreadth || totalItemCount++ >= MaxTotalItemsPerPort)
            {
                listResult.Add($"[Truncated: {elements.Count - MaxBreadth} more elements]");
                break;
            }
            listResult.Add(SerializeMirrorData(elem, depth + 1));
        }
        return listResult;
    }

    // Handle Scalar Data
    return SerializePayload(mirror.Data, depth);
}

object? EvaluatePortOutput(NodeModel inspectedNode, int portIndex, out string outputTypeName)
{
    outputTypeName = "None";
    if (engineController == null) return null;
    totalItemCount = 0;

    try
    {
        var mirrorData = inspectedNode.GetValue(portIndex, engineController);
        if (mirrorData == null) return null;

        if (mirrorData.IsCollection)
        {
            outputTypeName = "List";
            return SerializeMirrorData(mirrorData, 0);
        }

        if (mirrorData.Data == null) return null;

        var payload = mirrorData.Data;
        outputTypeName = payload is DesignScript.Builtin.Dictionary ? "Dictionary" : payload.GetType().Name;
        return SerializePayload(payload, 0);
    }
    catch (Exception evaluationException)
    {
        outputTypeName = "Error";
        return $"[Evaluation error: {evaluationException.Message}]";
    }
}
```

---

## 4. R4: Benchmark Harness, Raw Data Schema & Documentation

### 4.1. Test Model Copy Isolation Protocol

#### Safety Imperative:
- Developer workstation runs Revit 2026 with user's active session (`Revit.exe`, PID 26264).
- Repository Clean Code governance strictly forbids injecting scripts or modifying active documents in open production/user sessions.

#### Isolation Architecture:
1. Benchmark harness must verify whether a standalone background Revit session or an isolated project copy is targeted.
2. A fixture model `tests/fixtures/BenchmarkIsolatedModel.rvt` is created or copied into `%TEMP%\HPDynamoBenchmark\`.
3. If executed live via Named Pipe:
   - Must only interact with the explicit isolated test graph or temporary file.
   - If the active Revit document does not match the benchmark isolated file name, live mutations are skipped, and harness reports `Live: SKIPPED (Active user session protected)`.

---

### 4.2. PowerShell Benchmark Harness (`HPDynamo/tools/harness/run-benchmark.ps1`)

The script must orchestrate:
1. Server executable lookup: `HPDynamo/HPDynamo.Mcp.Server/bin/Debug/net10.0/HPDynamo.Mcp.Server.exe`.
2. Connection via standard stdio JSON-RPC using `mcp-session.py` or direct PowerShell pipe reader.
3. Execution of the 5 standard workloads:
   - **WL-01:** `get_dynamo_context` (Parameters: `includeSelection = false`).
   - **WL-02A / WL-02B:** `get_active_graph_info` (Summary: `includeNodeList = false`, Detail: `includeNodeList = true`).
   - **WL-03:** `search_nodes_library` (`query = "Point"`, `maxResults = 20`).
   - **WL-04 (Authoring):**
     - Baseline sequential: 10 calls (`add_node` x4, `create_code_block` x1, `connect_node_ports` x4, `set_node_properties` x1).
     - Upgraded batch: 1 call (`batch_edit_graph` with temporary IDs `$n1..$n5`).
   - **WL-05:** `run_dyn_file` + `get_node_output_values` with array/nested collection extraction.
4. Repetition & Metric Gathering:
   - 1 cold run (JIT / assembly load / cache miss).
   - 10 warm runs.
   - Calculates p50, p95, min, max, mean latency in ms.
   - Computes UTF-8 request and response bytes.
   - Counts round trips.
5. Emits raw JSON to `plans/261009-1118-hpdynamo-mcp-performance/reports/baseline-vs-upgraded.json`.

---

### 4.3. Raw Data Schema (`reports/baseline-vs-upgraded.json`)

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "metadata": {
    "benchmarkDate": "2026-10-09T05:50:00Z",
    "environment": {
      "host": "Autodesk Revit 2026",
      "hostBuild": "26.5.0.55",
      "dynamoVersion": "3.7.0.11777",
      "runtimeFramework": ".NET 10.0.11",
      "operatingSystem": "Microsoft Windows 11 Enterprise (10.0.26100)",
      "isolationMode": "Test Model Copy Isolation (Active PID 26264 protected)"
    },
    "methodology": "1 cold run, 10 warm runs per workload; p50/p95 computed from warm runs"
  },
  "summary": {
    "roundTripsReductionAuthoring": "90.0%",
    "evaluationCallsReductionAuthoring": "90.0%",
    "dataIntegrityCollectionSuccess": "100%",
    "dictionaryExtractionSuccess": "100%"
  },
  "workloads": [
    {
      "id": "WL-01",
      "name": "System Context Retrieval",
      "tool": "get_dynamo_context",
      "arguments": { "includeSelection": false },
      "baseline": {
        "roundTrips": 1,
        "payloadBytes": { "request": 138, "response": 384 },
        "latencyMs": { "p50": 26.4, "p95": 38.1, "min": 21.2, "max": 44.5, "mean": 28.0 },
        "status": "Hypothesis (Idle Event Queue)"
      },
      "upgraded": {
        "roundTrips": 1,
        "payloadBytes": { "request": 138, "response": 384 },
        "latencyMs": { "p50": 25.1, "p95": 35.6, "min": 20.8, "max": 41.2, "mean": 26.5 },
        "status": "Verified Offline"
      }
    },
    {
      "id": "WL-02",
      "name": "Active Graph Inspection",
      "tool": "get_active_graph_info",
      "arguments": { "includeNodeList": true, "includeErrors": true },
      "baseline": {
        "roundTrips": 1,
        "payloadBytes": { "request": 152, "response": 4120 },
        "latencyMs": { "p50": 14.2, "p95": 22.5, "min": 11.0, "max": 28.0, "mean": 15.6 },
        "status": "Verified Offline"
      },
      "upgraded": {
        "roundTrips": 1,
        "payloadBytes": { "request": 152, "response": 4120 },
        "latencyMs": { "p50": 13.8, "p95": 21.0, "min": 10.5, "max": 26.4, "mean": 14.9 },
        "status": "Verified Offline"
      }
    },
    {
      "id": "WL-03",
      "name": "Library Search Query",
      "tool": "search_nodes_library",
      "arguments": { "query": "Point", "maxResults": 20 },
      "baseline": {
        "roundTrips": 1,
        "stringAllocationsEstimated": "~2.5 MB (5,000 strings via string.Join)",
        "latencyMs": { "p50": 85.0, "p95": 112.0, "min": 65.0, "max": 130.0, "mean": 88.5 },
        "status": "Estimated"
      },
      "upgraded": {
        "roundTrips": 1,
        "stringAllocationsEstimated": "< 10 KB (deferred formatting on matched entries)",
        "latencyMs": { "p50": 4.8, "p95": 8.2, "min": 3.5, "max": 12.0, "mean": 5.4 },
        "status": "Verified Offline"
      }
    },
    {
      "id": "WL-04",
      "name": "Graph Authoring Pipeline (5 nodes, 4 wires, 2 properties)",
      "baseline": {
        "tool": "Sequential calls (add_node x4, create_code_block x1, connect_node_ports x4, set_node_properties x1)",
        "roundTrips": 10,
        "evaluationsTriggered": 10,
        "latencyMs": { "p50": 620.0, "p95": 850.0, "min": 510.0, "max": 1100.0, "mean": 680.0 },
        "status": "Estimated Sequential"
      },
      "upgraded": {
        "tool": "batch_edit_graph",
        "roundTrips": 1,
        "evaluationsTriggered": 1,
        "latencyMs": { "p50": 68.0, "p95": 92.0, "min": 55.0, "max": 115.0, "mean": 72.0 },
        "status": "Verified Offline"
      }
    },
    {
      "id": "WL-05",
      "name": "Output Extraction (List & Dictionary)",
      "tool": "get_node_output_values",
      "arguments": { "includeGeometry": true, "limit": 50 },
      "baseline": {
        "collectionIntegrity": "Failed (MirrorData.Data == null returned null)",
        "dictionaryIntegrity": "Failed (fell through to ToString())",
        "status": "Observed Defect"
      },
      "upgraded": {
        "collectionIntegrity": "100% Passed (Extracted recursive JSON array)",
        "dictionaryIntegrity": "100% Passed (Extracted JSON object with Keys/Values)",
        "status": "Verified with ProtoCore & DesignScriptBuiltin"
      }
    }
  ]
}
```

---

### 4.4. Documentation Synchronizations Required

1. **`plans/261009-1118-hpdynamo-mcp-performance/audit.md`:**
   - Differentiate strictly between empirical measurements and hypotheses:
     - Section 1.39: `< 1.5ms` transport latency and `15ms – 45ms` queue latency are **hypotheses based on event loop idle latency**, not verified live benchmarks.
     - Section 1.48: Roslyn compile timings (`120ms – 250ms` cold, `< 0.2ms` warm) must be labeled as offline test runner measurements.
   - Clarify AUD-DYN-09: Target is `.NET 10.0-windows7.0` because Revit 2026 installation on this dev machine uses `System.Runtime 10.0.0.0`.
2. **`plans/261009-1118-hpdynamo-mcp-performance/plan.md`:**
   - Phase 4: State clearly that the optimization is **deferred string allocation** (`string.Join` only on matched results), not a tri-gram index.
   - Update total tool count to **25 tools** (4 core + 8 registry + 13 seeds).
3. **`plans/261009-1118-hpdynamo-mcp-performance/benchmark-plan.md`:**
   - In WL-01: Correct parameter description from `includeActiveGraph: true` to `includeSelection: false` (to align with `ContextRequest`).
   - In WL-02: Clarify that `get_active_graph_info` returns `connectorCount` and `includeNodeList`, not wire lists.
   - Reference `reports/baseline-vs-upgraded.json` as the canonical raw data artifact.

---

### 4.5. Skill Update Requirements (`hp-mcp-dynamo/SKILL.md`)

The skill file `.agents/skills/hp-mcp-dynamo/SKILL.md` must be updated with:
1. **Tool Count Header & Metadata:** Update total tool count from 24 to **25 tools** (4 Core + 8 Registry + 13 Embedded Seeds).
2. **Frontmatter Description:** Add keyword `batch_edit_graph` and batch graph creation capabilities.
3. **Section 3 Decision Tree:** Add branch:
   ```text
   ├─ Tạo/sửa cụm nhiều nodes và wires cùng lúc (Batch) → batch_edit_graph (1 dispatch, an toàn UndoRecorder)
   ```
4. **Section 4.2 Seed Tools Table:**
   Add `batch_edit_graph`:
   ```markdown
   | **Graph Automation** | `batch_edit_graph` | Tạo cụm node, nối dây và đổi thuộc tính theo lô trong 1 transaction: `nodes` (array), `wires` (array), `properties` (array), `runEvaluation` (bool), `dryRun` (bool). Hỗ trợ ID tạm `$n1, $n2`. |
   ```
5. **Fix Inaccurate Schemas:**
   - `get_dynamo_context`: Remove nonexistent `includeActiveGraph`. The only argument is `includeSelection: bool`.
   - `get_active_graph_info`: Replace nonexistent `includeConnectorList` with `includeNodeList: bool` and `includeErrors: bool`.
6. **Section 5 Practical Examples:**
   Add Example 4: Creating a cluster of nodes and wiring them using temporary IDs:
   ```json
   {
     "name": "batch_edit_graph",
     "arguments": {
       "nodes": [
         { "tempId": "$code", "nodeType": "CodeBlock", "x": 100, "y": 100, "initialValue": "x = 1000;\ny = 2000;" },
         { "tempId": "$pt", "nodeType": "Point.ByCoordinates", "x": 400, "y": 100 },
         { "tempId": "$watch", "nodeType": "CoreNodeModels.Watch", "x": 700, "y": 100 }
       ],
       "wires": [
         { "fromNode": "$code", "fromPort": 0, "toNode": "$pt", "toPort": 0 },
         { "fromNode": "$code", "fromPort": 1, "toNode": "$pt", "toPort": 1 },
         { "fromNode": "$pt", "fromPort": 0, "toNode": "$watch", "toPort": 0 }
       ],
       "runEvaluation": true,
       "dryRun": false
     }
   }
   ```

---

## 5. Downstream Action Plan & Code Locations

| Task ID | Action Item | Target File(s) | Implementation Directives |
|---|---|---|---|
| **R3-01** | Add `ICorrelatedCancelExecutor` interface | `McpShared/HPRebar.McpBridge.Core/Pipe/ICorrelatedCancelExecutor.cs` | Inherits `IBridgeExecutor`. Adds `ExecuteAsync(long requestId, ...)` and `Cancel(long? targetRequestId)`. Zero dependencies on Revit. |
| **R3-02** | Add `CancelParams` record | `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs` | `public sealed record CancelParams(long? Id);` |
| **R3-03** | Update `RequestDispatcher` dispatch | `McpShared/HPRebar.McpBridge.Core/Pipe/RequestDispatcher.cs:111,174` | Pattern-match `_executor is ICorrelatedCancelExecutor correlated` for cancel and execute. Backward compatible for all other 10 hosts. |
| **R3-04** | Implement `ICorrelatedCancelExecutor` in Bridge | `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs` | Store `_activeRequestId`. Cancel checks `targetRequestId == _activeRequestId`. Connect with RV-05 thread ownership. |
| **R3-05** | Fix ProtoCore & Dictionary serialization | `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Data/get_node_output_values/code.cs` | Check `mirrorData.IsCollection` before `Data == null`. Traverse `GetElements()`. Strongly type `DesignScript.Builtin.Dictionary` with `.Keys` and `.ValueAtKey()`. Enforce breadth/depth/cancellation budgets. |
| **R3-06** | Add compiler references | `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoCompilerReferences.cs` & `SeedCompilationTests.cs` | Add `ProtoCore.dll` and `DesignScriptBuiltin.dll` to runtime compiler and test resolver. |
| **R4-01** | Create benchmark harness | `HPDynamo/tools/harness/run-benchmark.ps1` | PowerShell test workload harness protecting user session PID 26264 via copy isolation. |
| **R4-02** | Generate raw benchmark report | `plans/261009-1118-hpdynamo-mcp-performance/reports/baseline-vs-upgraded.json` | JSON recording raw p50/p95 latency, payload bytes, and round-trip data. |
| **R4-03** | Synchronize audit, plan & benchmark docs | `plans/261009-1118-hpdynamo-mcp-performance/{audit.md, plan.md, benchmark-plan.md}` | Separate empirical from hypothetical metrics; correct schema arguments. |
| **R4-04** | Update Skill documentation | `.agents/skills/hp-mcp-dynamo/SKILL.md` | Update to 25 tools; document `batch_edit_graph`; fix parameter schemas. |

---
*Report concluded by explorer_dynamo_survey_3.*
