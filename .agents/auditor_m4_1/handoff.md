# Forensic Audit Report & Handoff: Milestone M4 (HPRobot.Mcp.Server.Tests)

**Auditor**: `auditor_m4_1` (M4 Forensic Auditor)  
**Parent**: Project Orchestrator (`orchestrator_7`, conversation ID: `b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\auditor_m4_1\`  
**Target Milestone**: M4 (`HPRobot.Mcp.Server.Tests` & full solution test verification)  
**Date**: 2026-09-21  

---

## Forensic Audit Report

**Work Product**: Milestone M4 (`HPRobot.Mcp.Server.Tests` & Full Solution Test Execution)  
**Profile**: General Project  
**Verdict**: **INTEGRITY VIOLATION (REJECTED)**  

### Phase Results
- **Phase 1 — Source Code Inspection**: **PASS**
  - No hardcoded test result bypasses or dummy stubs found.
  - No `Assert.True(true)`, `Assert.False(false)`, or tautological assertions across `RobotHostProfileTests.cs`, `SeedCatalogTests.cs`, `SeedExecutionTests.cs`, and `SeedCompilationTests.cs`.
  - All test methods perform genuine schema validations, AST analysis, ScriptGuard security scans, in-memory Roslyn dynamic compilation against `Interop.RobotOM.dll`, and named pipe IPC round-trips.
- **Phase 2 — Behavioral Verification**: **FAIL**
  - Build Debug: **PASS** (`0 Warning(s), 0 Error(s)`).
  - Build Release: **PASS** (`0 Warning(s), 0 Error(s)` after stale build-server shutdown).
  - Test Suite `HPRobot.Mcp.Server.Tests` (solo): **PASS** (97 passed, 0 failed, 0 skipped).
  - Test Suite `HPRobot.McpBridge.Tests` (solo): **PASS** (197 passed, 0 failed, 0 skipped).
  - McpShared Regression Tests: **PASS** (685 passed, 0 failed).
  - Solution Test Suite `dotnet test HPRobot/HPRobot.slnx`: **FAIL**
    - Out of 3 independent runs of `dotnet test HPRobot.slnx`, 2 runs failed with exit code 2 and 1 test failure (`293 succeeded, 1 failed, 0 skipped`).
    - Flaky failure: `HPRobot.Mcp.Server.Tests.SeedExecutionTests.Timeout_InformsModelThatChangesMayHavePersisted` at line 356 (`Assert.True(_executor.CancelCalls > 0);`).
- **Phase 3 — Claim Honesty Verification**: **FAIL**
  - Worker claimed: `All Solution Tests: 294/294 tests passed (100% success rate, 0 failed, 0 skipped)`.
  - Empirical finding: `dotnet test HPRobot.slnx` fails intermittently due to a race condition in `Timeout_InformsModelThatChangesMayHavePersisted`. The worker's claim of a 100% reproducible pass rate is invalidated under concurrent solution test execution.

---

## 1. Observation

### 1.1 Verbatim Failure in `dotnet test HPRobot.slnx` (Run 1 — task-66)
```text
Running tests from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.McpBridge.Tests\bin\Debug\net8.0-windows\HPRobot.McpBridge.Tests.dll (net8.0|x64)
Running tests from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\bin\Debug\net10.0\HPRobot.Mcp.Server.Tests.dll (net10.0|x64)
failed HPRobot.Mcp.Server.Tests.SeedExecutionTests.Timeout_InformsModelThatChangesMayHavePersisted (231ms)
  Assert.True() Failure
Expected: True
Actual:   False
  from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\bin\Debug\net10.0\HPRobot.Mcp.Server.Tests.dll (net10.0|x64)
  Assert.True() Failure
  Expected: True
  Actual:   False
    at HPRobot.Mcp.Server.Tests.SeedExecutionTests.Timeout_InformsModelThatChangesMayHavePersisted() in G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\SeedExecutionTests.cs:356
    at HPRobot.Mcp.Server.Tests.SeedExecutionTests.Timeout_InformsModelThatChangesMayHavePersisted() in G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\SeedExecutionTests.cs:356
    --- End of stack trace from previous location ---
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\bin\Debug\net10.0\HPRobot.Mcp.Server.Tests.dll (net10.0|x64) failed with 1 error(s) (6s 764ms)
Exit code: 2
  Standard output: xUnit.net v3 Microsoft.Testing.Platform Runner v3.1.0+03a071627b (64-bit .NET 10.0.11)
  
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.McpBridge.Tests\bin\Debug\net8.0-windows\HPRobot.McpBridge.Tests.dll (net8.0|x64) passed (13s 686ms)

Test run summary: Failed!
  G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\bin\Debug\net10.0\HPRobot.Mcp.Server.Tests.dll (net10.0|x64) failed with 1 error(s) (6s 764ms)
  G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.McpBridge.Tests\bin\Debug\net8.0-windows\HPRobot.McpBridge.Tests.dll (net8.0|x64) passed (13s 686ms)

  total: 294
  failed: 1
  succeeded: 293
  skipped: 0
  duration: 14s 182ms
Test run completed with non-success exit code: 2 (see: https://aka.ms/testingplatform/exitcodes)
```

### 1.2 Verbatim Failure in `dotnet test HPRobot.slnx` (Run 3 — task-100)
```text
Running tests from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.McpBridge.Tests\bin\Debug\net8.0-windows\HPRobot.McpBridge.Tests.dll (net8.0|x64)
Running tests from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\bin\Debug\net10.0\HPRobot.Mcp.Server.Tests.dll (net10.0|x64)
failed HPRobot.Mcp.Server.Tests.SeedExecutionTests.Timeout_InformsModelThatChangesMayHavePersisted (233ms)
  Assert.True() Failure
Expected: True
Actual:   False
  from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\bin\Debug\net10.0\HPRobot.Mcp.Server.Tests.dll (net10.0|x64)
  Assert.True() Failure
  Expected: True
  Actual:   False
    at HPRobot.Mcp.Server.Tests.SeedExecutionTests.Timeout_InformsModelThatChangesMayHavePersisted() in G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\SeedExecutionTests.cs:356
    at HPRobot.Mcp.Server.Tests.SeedExecutionTests.Timeout_InformsModelThatChangesMayHavePersisted() in G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\SeedExecutionTests.cs:356
    --- End of stack trace from previous location ---
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\bin\Debug\net10.0\HPRobot.Mcp.Server.Tests.dll (net10.0|x64) failed with 1 error(s) (5s 357ms)
Exit code: 2
  Standard output: xUnit.net v3 Microsoft.Testing.Platform Runner v3.1.0+03a071627b (64-bit .NET 10.0.11)
  
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.McpBridge.Tests\bin\Debug\net8.0-windows\HPRobot.McpBridge.Tests.dll (net8.0|x64) passed (13s 188ms)

Test run summary: Failed!
  total: 294
  failed: 1
  succeeded: 293
  skipped: 0
  duration: 13s 651ms
Test run completed with non-success exit code: 2
```

### 1.3 Code Inspection: The Root Cause Race Condition
In `HPRobot/HPRobot.Mcp.Server.Tests/SeedExecutionTests.cs`, lines 349-357:
```csharp
349:        var error = await Assert.ThrowsAsync<BridgeTimeoutException>(() => impatientClient.SendAsync<ExecuteResult>(
350:            "robot.execute", new ExecuteRequest("return 1;", "none", false, 5, "slow", null),
351:            TimeSpan.FromMilliseconds(200), null, TestContext.Current.CancellationToken));
352:
353:        Assert.Contains("persisted (no rollback)", error.Message);
354:        Assert.Contains("snapshot", error.Message);
355:        Assert.DoesNotContain("nothing has been committed", error.Message);
356:        Assert.True(_executor.CancelCalls > 0);
357:    }
```

In `McpShared/HPRebar.Mcp.Server.Core/Services/RevitBridgeClient.cs`, lines 81-88 and 97-101:
```csharp
81:         catch (TimeoutException)
82:         {
83:             TryCancelInRevit(id);
84:             throw new BridgeTimeoutException(
85:                 $"{_profile.DisplayName} did not answer within {timeout.TotalSeconds:0}s. "
86:                 + (_profile.TimeoutSemanticsHint
87:                    ?? $"The timeout is cooperative: {_profile.DisplayName} may still be finishing the script, and nothing has been committed until it does."));
88:         }
...
97:     /// <summary>Best effort: tells the bridge to cancel; the caller has already given up on the answer.</summary>
98:     private void TryCancelInRevit(long id)
99:     {
100:        _ = SendAsync<CancelResult>(_profile.Method(JsonRpcMethods.CancelSuffix), new { id }, TimeSpan.FromSeconds(5), null, CancellationToken.None)
101:            .ContinueWith(t => _logger.LogDebug(t.Exception, "Cancel after timeout failed"), TaskContinuationOptions.OnlyOnFaulted);
102:    }
```
`TryCancelInRevit(id)` explicitly launches `SendAsync<CancelResult>` as an unawaited, fire-and-forget background task.
When `Assert.ThrowsAsync<BridgeTimeoutException>` returns, the background task has just been scheduled or is in the middle of IPC named pipe transmission.
Calling `Assert.True(_executor.CancelCalls > 0)` synchronously on line 356 executes before the named pipe listener has deserialized the JSON-RPC packet and called `_executor.Cancel(id)`.
Under machine load (such as during parallel solution test runs), this causes immediate assertion failure.

In contrast, the sibling implementation in `HPEtabs` (`HPEtabs.Mcp.Server.Tests/EtabsToolsOverPipeTests.cs:267-273`) tests only the exception message and does NOT assert `_executor.CancelCalls > 0`:
```csharp
        var error = await Assert.ThrowsAsync<BridgeTimeoutException>(() => impatient.SendAsync<ExecuteResult>(
            "etabs.execute", new ExecuteRequest("return 1;", "none", false, 5, "slow", null), TimeSpan.FromMilliseconds(300), null, TestContext.Current.CancellationToken));

        Assert.Contains("persisted", error.Message);
        Assert.Contains("no rollback", error.Message);
        Assert.DoesNotContain("nothing has been committed", error.Message);
```
Furthermore, `robot.cancel` dispatch is already independently and deterministically tested in `SeedExecutionTests.Cancel_DispatchesToBridgeExecutor` (lines 323-329).

### 1.4 Passing Checks (Empirical Verification)
- **HPRobot.Mcp.Server.Tests (solo run)**:
  `dotnet run --project HPRobot/HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj`
  `total: 97, failed: 0, succeeded: 97, skipped: 0, duration: 3s 713ms`
- **HPRobot.McpBridge.Tests (solo run)**:
  `dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj`
  `total: 197, failed: 0, succeeded: 197, skipped: 0, duration: 10s 775ms`
- **McpShared Regressions**:
  - `HPRebar.Mcp.Server.Core.Tests`: `total: 613, failed: 0, succeeded: 613, skipped: 0, duration: 3s 474ms`
  - `HPRebar.McpBridge.Core.Net48Tests`: `total: 72, failed: 0, succeeded: 72, skipped: 0, duration: 4s 819ms`
  - Total McpShared: `685 passed, 0 failed` (100% clean).
- **Compilation**:
  - `dotnet build HPRobot.slnx -c Debug`: `0 Warning(s), 0 Error(s)`
  - `dotnet build HPRobot.slnx -c Release`: `0 Warning(s), 0 Error(s)` (after clearing stale msbuild server daemon via `dotnet build-server shutdown`).

---

## 2. Logic Chain

1. **Mandatory Audit Rule**: Under the Integrity Forensics protocol and User Mission instructions, "If ANY check fails, your verdict is INTEGRITY VIOLATION and you MUST reject the work product."
2. **Behavioral Test Execution Failure**: Mission requirement #3 dictates: "Run full solution dotnet test HPRobot/HPRobot.slnx (verify 294 passing tests)."
3. **Reproducibility of Flakiness**:
   - Run 1 (task-66): 293 passed, 1 failed (`Timeout_InformsModelThatChangesMayHavePersisted`).
   - Run 2 (task-88): 294 passed, 0 failed.
   - Run 3 (task-100): 293 passed, 1 failed (`Timeout_InformsModelThatChangesMayHavePersisted`).
   Two out of three runs failed under full solution execution.
4. **Defect Proven**: The assertion `Assert.True(_executor.CancelCalls > 0);` at `SeedExecutionTests.cs:356` is flawed due to an unawaited race condition against `TryCancelInRevit`.
5. **Auditor Constraint**: As forensic auditor, I am strictly forbidden from modifying implementation or test code (`Audit-only — do NOT modify implementation code`).
6. **Conclusion**: Milestone M4 must be rejected with verdict **INTEGRITY VIOLATION** and returned to worker `worker_m4_1` for immediate rectification.

---

## 3. Caveats

- **Test Logic Validity**: The 97 tests created in `HPRobot.Mcp.Server.Tests` are genuinely written with high fidelity, rich schema assertions, Roslyn script compilation against `Interop.RobotOM.dll`, and valid named-pipe integration. The failure is not caused by malicious facades or dummy stubs, but by a timing race condition in a fire-and-forget cancellation test.
- **Standalone Execution**: When executed in isolation via `dotnet run --project HPRobot.Mcp.Server.Tests.csproj`, the test passes because system load is low and the IPC message arrives before the assertion. The failure surfaces specifically during parallel test runs under `dotnet test HPRobot.slnx`.

---

## 4. Conclusion & Actionable Fix for `worker_m4_1`

**Verdict**: **INTEGRITY VIOLATION (REJECTED)**  

The work product fails Acceptance Criterion 3 (full solution `dotnet test HPRobot.slnx` must pass 294 tests deterministically with 0 failures).

### Actionable Remediation for `worker_m4_1`:
In `HPRobot/HPRobot.Mcp.Server.Tests/SeedExecutionTests.cs`, line 356 of `Timeout_InformsModelThatChangesMayHavePersisted()`:
Either:
1. **Remove line 356 (`Assert.True(_executor.CancelCalls > 0);`)**:
   This matches the standard pattern in `HPEtabs` (`HPEtabs.Mcp.Server.Tests/EtabsToolsOverPipeTests.cs:267-273`). Cancellation dispatch over the pipe is already explicitly and deterministically verified in `Cancel_DispatchesToBridgeExecutor()` (lines 323-329).
2. **OR add an async polling wait loop**:
   ```csharp
   var sw = System.Diagnostics.Stopwatch.StartNew();
   while (_executor.CancelCalls == 0 && sw.ElapsedMilliseconds < 2000)
   {
       await Task.Delay(25);
   }
   Assert.True(_executor.CancelCalls > 0);
   ```
After applying the fix, verify that `dotnet test HPRobot/HPRobot.slnx` passes 294/294 tests across 5 consecutive runs with zero failures.

---

## 5. Verification Method

To reproduce and verify the failure:
1. Run full solution test suite multiple times:
   ```powershell
   cd "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot"
   dotnet test HPRobot.slnx
   ```
   Observe the intermittent failure on `SeedExecutionTests.Timeout_InformsModelThatChangesMayHavePersisted` (line 356: `Assert.True(_executor.CancelCalls > 0)`).
2. Invalidation Condition: The rejection is resolved when the race condition at line 356 is eliminated and `dotnet test HPRobot.slnx` passes 294/294 tests 100% of the time across multiple consecutive runs.
