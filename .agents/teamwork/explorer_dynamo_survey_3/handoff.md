# Handoff Report — explorer_dynamo_survey_3

**Date:** 2026-10-09  
**From:** explorer_dynamo_survey_3  
**To:** parent / orchestrator_7 (`f448d195-c008-4b83-983a-30d0f3ab6338`)  
**Assignment:** Investigation and Architectural Design for R3 & R4 (RV-04, RV-07, RV-08, Benchmark Harness & Docs)

---

## 1. Observation

1. **RV-04 Cancellation Wire & Dispatch Mismatch:**
   - In `McpShared/HPRebar.Mcp.Server.Core/Services/RevitBridgeClient.cs:99`:
     `SendAsync<CancelResult>(_profile.Method(JsonRpcMethods.CancelSuffix), new { id }, TimeSpan.FromSeconds(5), null, CancellationToken.None)`
     The client sends the target request ID inside `params: { id }`.
   - In `McpShared/HPRebar.McpBridge.Core/Pipe/RequestDispatcher.cs:112`:
     `case JsonRpcMethods.CancelSuffix: return JsonRpcEnvelope.Success(id, _executor.Cancel());`
     The dispatcher ignores `request.Params` and calls `_executor.Cancel()` with no arguments.
   - In `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoBridgeCoordinator.cs:193-206`:
     `Cancel()` unconditionally signals `_currentCancel?.Cancel()`, aborting whatever execution is currently running even if the cancel signal belonged to an earlier completed or timed-out request.
   - In `McpShared/HPRebar.McpBridge.Core/Pipe/IBridgeExecutor.cs:10-38`:
     `IBridgeExecutor` defines `CancelResult Cancel();` with 0 parameters. It is implemented across 10 other hosts (`HPRebar`, `HPAutoCad`, `HPCivil3d`, `HPNavis` [.NET 4.8], `HPEtabs`, `HPSap2000`, `HPPowerBi`, `HPExcel`, `HPRobot`, `HPTekla`).

2. **RV-07 & RV-08 ProtoCore MirrorData & Dictionary Output Defect:**
   - In `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Data/get_node_output_values/code.cs:70`:
     `var mirrorData = inspectedNode.GetValue(portIndex, engineController);`
     `if (mirrorData?.Data == null) return null;`
   - In `ProtoCore.Mirror.MirrorData`:
     For collections/arrays (e.g. CodeBlock `[1, 2, 3]` or geometry lists), `mirrorData.IsCollection` is `true`, `mirrorData.GetElements()` yields `List<MirrorData>`, and `mirrorData.Data` is **`null`**. Line 70 immediately returns `null` for all collection outputs.
   - In `DesignScriptBuiltin.dll`:
     `DesignScript.Builtin.Dictionary` does not implement `IDictionary` or `IEnumerable`. In `code.cs`, it skips line 38 and line 49, falling through to `value.ToString()` at line 59.
   - In `McpShared/HPRebar.McpBridge.Core/Scripting/ScriptGuard.cs:22,46,166`:
     `ScriptGuard` statically denies `System.Reflection` (`GetProperty`, `GetMethod`), and `dynamic`. Access to `MirrorData` and `DesignScript.Builtin.Dictionary` must be direct and strongly typed.
   - In `HPDynamo/HPDynamo.McpBridge/Coordinator/DynamoCompilerReferences.cs:21-45`:
     Neither `ProtoCore.dll` nor `DesignScriptBuiltin.dll` is included in `DynamoCompilerReferences.All`.
   - In `get_node_output_values/code.cs:49`:
     The loop materializes all elements of an `IEnumerable` without breadth limits, and recursion does not check `ct.ThrowIfCancellationRequested()`.

3. **R4 Benchmark & Schema Inconsistencies:**
   - Active Revit instance PID 26264 is open on the developer machine; testing on active user models without isolation risks corrupting user state.
   - In `plans/261009-1118-hpdynamo-mcp-performance/benchmark-plan.md:30`:
     WL-01 assumes `get_dynamo_context` takes `includeActiveGraph: true`, but `ContextRequest.cs` and `ContextService.cs:22` only accept `bool includeSelection`.
   - In `benchmark-plan.md:36`:
     WL-02 claims wire detail inspection, but `get_active_graph_info/tool.json` only accepts `includeNodeList` and `includeErrors`, returning `connectorCount` rather than wire details.
   - In `plans/261009-1118-hpdynamo-mcp-performance/audit.md:40,44`:
     `< 1.5ms` transport and `15–45ms` queue latency are listed without empirical raw data files.
   - In `.agents/skills/hp-mcp-dynamo/SKILL.md:87`:
     States 24 tools (4 Core + 12 Seeds + 8 Registry), omitting `batch_edit_graph` (which brings the total to 25 tools).

---

## 2. Logic Chain

1. **Request-Scoped Cancellation (RV-04):**
   - Because `RevitBridgeClient` sends `{"id": id}` on cancellation, the request ID is already transmitted on the wire.
   - Because `IBridgeExecutor` is referenced by 10 production hosts (including .NET Framework 4.8 hosts like `HPNavis`), modifying `IBridgeExecutor` directly would break binary and source compatibility.
   - Introducing `ICorrelatedCancelExecutor : IBridgeExecutor` in `McpShared/HPRebar.McpBridge.Core` adds `ExecuteAsync(long requestId, ...)` and `Cancel(long? targetRequestId)` without changing `IBridgeExecutor`.
   - In `RequestDispatcher`, pattern-matching `if (_executor is ICorrelatedCancelExecutor correlated)` delegates to correlated execution and cancel, falling back to legacy behavior for the other 10 hosts.
   - In `DynamoBridgeCoordinator`, tracking `_activeRequestId` and checking `targetRequestId == _activeRequestId` guarantees that stale cancellation from an earlier request cannot cancel a newly started request.

2. **MirrorData & Dictionary Extraction (RV-07 & RV-08):**
   - Because ProtoCore stores array values with `mirrorData.Data == null` and `mirrorData.IsCollection == true`, checking `mirrorData.IsCollection` first and traversing `mirrorData.GetElements()` restores 100% list/array extraction.
   - Because `DesignScript.Builtin.Dictionary` exposes `.Keys` and `.ValueAtKey(key)` rather than `IDictionary`, explicit type checking `if (value is DesignScript.Builtin.Dictionary dsDict)` allows extracting full key-value maps.
   - Because reflection and `dynamic` are blocked by `ScriptGuard`, `ProtoCore.dll` and `DesignScriptBuiltin.dll` must be added to `DynamoCompilerReferences.All`, `SeedCompilationTests`, and `HostScriptContracts.DynamoImports`.
   - Adding breadth limits (100 items), depth limits (6 levels), and `ct.ThrowIfCancellationRequested()` prevents payload explosion and guarantees immediate cancellation.

3. **Benchmark, Raw Data & Skills (R4):**
   - Because PID 26264 contains the user's live session, benchmarking requires strict test model copy isolation.
   - Building `HPDynamo/tools/harness/run-benchmark.ps1` and storing `reports/baseline-vs-upgraded.json` provides reproducible empirical data.
   - Updating `SKILL.md` to 25 tools with `batch_edit_graph` ensures AI agents utilize single-roundtrip batch operations.

---

## 3. Caveats

1. **Revit PID 26264 Active Session:** No live graph was executed in the user's active Revit session during this survey, in accordance with the repository's safety rules. All findings are derived from decompiled binaries, static Roslyn AST constraints, contracts, and existing test suites.
2. **Headless Journal Execution:** Live verification of headless `.dyn` journal execution with `RunAndWait` requires a dedicated Revit test run on a cloned fixture.
3. **External Package Dictionaries:** Custom Python dictionaries inside Dynamo that do not map to `DesignScript.Builtin.Dictionary` or `IDictionary` will fall through to `ToString()`.

---

## 4. Conclusion

1. **RV-04** is fully designed with `ICorrelatedCancelExecutor` and `CancelParams`, delivering request-level cancel isolation with 0 breaking changes to any other host.
2. **RV-07 & RV-08** root causes are proven: `get_node_output_values/code.cs` must check `MirrorData.IsCollection` before `Data == null`, strongly type `DesignScript.Builtin.Dictionary`, and enforce breadth/depth/cancellation budgets.
3. **R4** benchmark harness, raw data format, document clarifications, and skill updates are fully specified and ready for implementation.

---

## 5. Verification Method

1. **Inspect Report:** View `.agents/teamwork/explorer_dynamo_survey_3/report.md`.
2. **Test Suite Verification:**
   - `dotnet test HPDynamo/HPDynamo.Mcp.Server.Tests` (99 passed)
   - `dotnet test HPDynamo/HPDynamo.McpBridge.Tests` (46 passed)
   - `dotnet test HPDynamo/HPDynamo.E2E.Tests` (201 passed)
   - `cd McpShared && dotnet test HPRebar.McpBridge.Core.Net48Tests` (115 passed)
   - `cd McpShared && dotnet test HPRebar.Mcp.Server.Core.Tests` (899 passed)
3. **Invalidation Conditions:**
   - If `IBridgeExecutor` definition is altered, breaking other hosts.
   - If `code.cs` attempts to use `System.Reflection` or `dynamic`, failing `ScriptGuard`.
