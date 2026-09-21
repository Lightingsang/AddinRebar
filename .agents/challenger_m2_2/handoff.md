# Empirical Challenge Report — Milestone M2 (RobotUnitsPolicy, RobotStaWorker, RobotDispatcher)

**Challenger:** `challenger_m2_2` (critic, specialist)  
**Parent:** Project Orchestrator (`orchestrator_7`, conversation ID: `b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Verdict:** **APPROVE**  
**Date:** 2026-09-21  

---

## 1. Observation

1. **Test Infrastructure & Solution Assembly**:
   - `HPRobot/HPRobot.slnx` was updated to incorporate `HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj` (`net8.0-windows` with `xunit.v3` and `Microsoft.Testing.Platform`).
   - Four dedicated adversarial test suites were implemented and executed directly:
     - `HPRobot/HPRobot.McpBridge.Tests/RobotUnitsPolicyChallengerTests.cs` (6 empirical stress tests)
     - `HPRobot/HPRobot.McpBridge.Tests/RobotStaWorkerChallengerTests.cs` (5 empirical concurrency and apartment tests)
     - `HPRobot/HPRobot.McpBridge.Tests/RobotOleMessageFilterChallengerTests.cs` (13 empirical OLE retry and rejection tests)
     - `HPRobot/HPRobot.McpBridge.Tests/RobotDispatcherWireChallengerTests.cs` (8 empirical named pipe wire round-trip tests)

2. **Test Execution Results**:
   - Command:
     ```powershell
     dotnet run --project "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.McpBridge.Tests\HPRobot.McpBridge.Tests.csproj"
     ```
   - Verbatim runner output:
     ```
     xUnit.net v3 Microsoft.Testing.Platform Runner v3.1.0+03a071627b (64-bit .NET 8.0.30)
     Test run summary: Passed! - G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.McpBridge.Tests\bin\Debug\net8.0-windows\HPRobot.McpBridge.Tests.dll (net8.0|x64)
       total: 137
       failed: 0
       succeeded: 137
       skipped: 0
       duration: 2s 519ms
     ```

3. **Solution Build Verifications**:
   - Command: `dotnet build HPRobot\HPRobot.slnx -c Debug`
     - Result: `Build succeeded. 0 Warning(s), 0 Error(s). Time Elapsed 00:00:02.58`
   - Command: `dotnet build HPRobot\HPRobot.slnx -c Release`
     - Result: `Build succeeded. 0 Warning(s), 0 Error(s). Time Elapsed 00:00:05.26`

4. **Shared Baseline Regression Verifications**:
   - Command: `dotnet run --project McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj`
     - Result: `Passed! total: 613, failed: 0, succeeded: 613, skipped: 0, duration: 2s 836ms`
   - Command: `dotnet run --project McpShared\HPRebar.McpBridge.Core.Net48Tests\HPRebar.McpBridge.Core.Net48Tests.csproj`
     - Result: `Passed! total: 72, failed: 0, succeeded: 72, skipped: 0, duration: 2s 118ms`

---

## 2. Logic Chain

### 2.1 Challenge Area 1: `RobotUnitsPolicy` Behavior
1. **Pre-Execution Metric Enforcement**:
   - In `RobotUnitsPolicy.cs` lines 59-66, the policy sets `unitMngr.UseMetricAsDefault = true;`, dimensions to `"m"`, force to `"kN"`, moment to `"kN*m"`, and stress to `"MPa"`, followed by `unitMngr.Refresh()`.
   - Empirically confirmed by `RobotUnitsPolicyChallengerTests.Run_EnforcesMetricUnits_AndRestoresOriginalImperialUnits_OnSuccessfulScript`: when called with an initial imperial configuration (`ft`, `kip`, `kip*ft`, `ksi`, `UseMetricAsDefault = false`), state captured during script execution verified that all four unit dimensions were standardized to metric values.
2. **State Restoration on Success and Unhandled Failure**:
   - In `RobotUnitsPolicy.cs` lines 78-112, the `finally` block restores original user preferences: `unitMngr.UseMetricAsDefault = savedMetricDefault;`, restores saved dimensions, forces, moments, and stresses, and invokes `unitMngr.Refresh()`.
   - Empirically confirmed by `RobotUnitsPolicyChallengerTests.Run_RestoresOriginalUnits_EvenWhenScriptThrowsUnhandledException`: when the script body threw `ApplicationException("Fatal FEA simulation crash in script")`, the `finally` block successfully executed, restoring original units (`in`, `lbf`, `lbf*in`, `psi`, `UseMetricAsDefault = false`), while allowing the unhandled exception to propagate without being suppressed.
3. **Fault Tolerance & Degraded Logging**:
   - In `RobotUnitsPolicyChallengerTests.Run_WhenPreExecutionUnitSetupThrows_LogsWarningAndStillExecutesScriptAndFinally`: simulated COM exception during pre-execution unit acquisition logged a descriptive warning into the script execution log (`"warning: standardizing units to Metric threw COMException"`) and did not abort script execution.
   - In `RobotUnitsPolicyChallengerTests.Run_WhenPostExecutionRestorationThrows_LogsWarningWithoutMaskingOriginalException`: simulated COM failure during restoration logged a warning (`"warning: restoring user units threw COMException"`) without swallowing the original script business error.

### 2.2 Challenge Area 2: STA Threading & Concurrency
1. **Dedicated STA Apartment Enforcement**:
   - In `RobotStaWorker.cs` line 30, `_thread.SetApartmentState(ApartmentState.STA)` is configured.
   - Empirically confirmed by `RobotStaWorkerChallengerTests.Worker_ExecutesTasksStrictlyInStaApartmentState`: verified that both `RunAsync` and `RunOnControlLaneAsync` execute on a thread where `Thread.CurrentThread.GetApartmentState() == ApartmentState.STA`, and that the managed thread ID is constant and distinct from the caller's threadpool thread.
2. **Sequential Concurrency & Thread Isolation**:
   - Empirically confirmed by `RobotStaWorkerChallengerTests.Worker_SerializesConcurrentExecutionsSequentially`: when 10 tasks were simultaneously dispatched from separate threadpool threads, `activeConcurrency` never exceeded 1, and all 10 tasks completed sequentially on the single STA thread.
3. **Priority Queue Ordering (Control Lane Precedence)**:
   - In `RobotStaWorker.cs` lines 116-122, `DrainQueue(_controlLane)` runs ahead of dequeuing each script task from `_scriptLane`.
   - Empirically confirmed by `RobotStaWorkerChallengerTests.ControlLane_PrioritizedAheadOfQueuedScriptLaneTasks`: when 3 script tasks and 2 control tasks were enqueued while the worker was blocked, the control tasks jumped ahead and executed before the queued script tasks once unblocked.
4. **Worker Resilience**:
   - Empirically confirmed by `RobotStaWorkerChallengerTests.Worker_SurvivesUnhandledExceptionsInTasks_AndProcessesSubsequentTasks`: when tasks threw `InvalidOperationException` and `ArgumentException`, the corresponding TaskCompletionSources faulted cleanly while the underlying STA thread survived and continued processing subsequent tasks.
5. **Native `IOleMessageFilter` Implementation**:
   - In `ComInteropHelper.cs` lines 106-133:
     - `RetryRejectedCall` returns 250ms when `dwRejectType == SERVERCALL_RETRYLATER` (2) and `dwTickCount < 30000`.
     - `RetryRejectedCall` returns -1 (cancel) when `dwTickCount >= 30000` or for other reject types (`SERVERCALL_REJECTED` = 1).
     - `HandleInComingCall` returns 0 (`SERVERCALL_ISHANDLED`).
     - `MessagePending` returns 2 (`PENDINGMSG_WAITDEFPROCESS`).
   - Empirically confirmed across 13 test cases in `RobotOleMessageFilterChallengerTests`.

### 2.3 Challenge Area 3: Named Pipe Wire Protocol & Dispatching
1. **Named Pipe Registration**:
   - `PipeNaming.For(PipeNaming.RobotHost, 2026)` resolves to `"hprobot-mcp-2026"`.
2. **Live Pipe Communication & JSON-RPC Dispatching**:
   - Tested over live Windows Named Pipe streams (`NamedPipeClientStream` <-> `PipeListener`) in `RobotDispatcherWireChallengerTests`:
     - `robot.ping`: verified response with `pong: true`, `revitVersion: "2026"`, `executionEnabled: true`.
     - `robot.context`: verified response returning `ContextResult` containing `robot` (`RobotInfo` with `isAttached`, `nodeCount`, `barCount`, `panelCount`, etc.).
     - `robot.execute` (Gated): when `ExecutionEnabled = false`, returned JSON-RPC error -32001 (`ExecutionDisabled`) with message `"Code execution is disabled. Ask the user to tick 'Allow AI execution' in the HPRobot MCP Bridge window."`.
     - `robot.execute` (Execution): when `ExecutionEnabled = true`, compiled and executed script, returning result `100`.
     - `robot.attach` & `robot.detach`: dispatched via `RobotDispatcher.DispatchCustomAsync`, returning attachment state or diagnostic error (-32000) when Robot is not in the Windows ROT.
     - `robot.cancel`: returned `CancelResult`.
     - Method not found: unknown method returned code -32601 (`MethodNotFound`).
     - Parse error: malformed input returned code -32700 (`ParseError`).

---

## 3. Caveats

1. **Running Object Table Elevation Boundary**:
   - If `robot.exe` is launched elevated (as Administrator) and `HPRobot.McpBridge.exe` is non-elevated (or vice-versa), `Marshal2.GetActiveObject` cannot access the ROT across session integrity levels. `RobotAttachment.Attach()` detects running `robot.exe` processes and returns an explicit diagnostic message explaining the administrator elevation mismatch.
2. **No Robot Process in Headless CI**:
   - Live interaction with actual FEA mesh generation and solver calculation requires Autodesk Robot Structural Analysis Professional 2026 installed and running. All test cases in `HPRobot.McpBridge.Tests` are designed to execute headlessly and deterministically without requiring an active robot model.

---

## 4. Conclusion

**Verdict: APPROVE.**

`RobotUnitsPolicy`, `RobotStaWorker`, `ComInteropHelper` (IOleMessageFilter), `RobotDispatcher`, and `RobotBridgeExecutor` satisfy all requirements and contracts:
- Units policy guarantees Metric normalization (`m`, `kN`, `kN*m`, `MPa`) before execution and reliably restores user units in `finally` blocks, preserving script exceptions.
- `RobotStaWorker` strictly enforces STA apartment execution on a single dedicated thread, serializes concurrent calls, and prioritizes control lane operations over script executions.
- `RobotOleMessageFilter` correctly manages COM busy/retry states up to the 30-second ceiling.
- `RobotDispatcher` and named pipe listeners adhere strictly to the `hprobot-mcp-2026` wire contract.

All 137 tests in `HPRobot.McpBridge.Tests` pass with 0 failures, and the 685 baseline tests in `McpShared` pass with 0 regressions.

---

## 5. Verification Method

To independently reproduce and verify this challenge report:

1. **Run HPRobot Bridge Unit & Challenger Tests**:
   ```powershell
   dotnet run --project "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.McpBridge.Tests\HPRobot.McpBridge.Tests.csproj"
   ```
   *Expected:* 137 tests passed, 0 failed, 0 skipped.

2. **Run McpShared Regression Test Baselines**:
   ```powershell
   dotnet run --project "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj"
   dotnet run --project "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.McpBridge.Core.Net48Tests\HPRebar.McpBridge.Core.Net48Tests.csproj"
   ```
   *Expected:* 613 tests passed (.NET 10) and 72 tests passed (.NET 4.8), 0 failed.

3. **Build Full HPRobot Solution**:
   ```powershell
   dotnet build "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.slnx" -c Debug
   dotnet build "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.slnx" -c Release
   ```
   *Expected:* Both exit with code 0, reporting 0 warnings and 0 errors.
