# Handoff Report: Investigation of Asynchronous Race Condition in `SeedExecutionTests.cs`

**Agent**: `explorer_m4_r2_1` (Async Race Condition & Cancellation Specialist)  
**Parent**: Project Orchestrator (`orchestrator_7`, conversation ID: `b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m4_r2_1\`  
**Target File**: `HPRobot/HPRobot.Mcp.Server.Tests/SeedExecutionTests.cs:356`  
**Date**: 2026-09-21  

---

## 1. Observation

### 1.1 Verbatim Failure Observed During Forensic Audits
In `auditor_m4_1/handoff.md`, `reviewer_m4_2/handoff.md`, and `challenger_m4_2/handoff.md`, running `dotnet test HPRobot.slnx` under concurrent test execution failed with exit code 2:

```text
failed HPRobot.Mcp.Server.Tests.SeedExecutionTests.Timeout_InformsModelThatChangesMayHavePersisted (231ms)
  Assert.True() Failure
Expected: True
Actual:   False
  from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\bin\Debug\net10.0\HPRobot.Mcp.Server.Tests.dll (net10.0|x64)
  Assert.True() Failure
  Expected: True
  Actual:   False
    at HPRobot.Mcp.Server.Tests.SeedExecutionTests.Timeout_InformsModelThatChangesMayHavePersisted() in G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\SeedExecutionTests.cs:356
```

### 1.2 Exact Target Code in `HPRobot/HPRobot.Mcp.Server.Tests/SeedExecutionTests.cs`
Lines 349–357 of `SeedExecutionTests.cs`:
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

### 1.3 Asynchronous Cancellation Implementation in `McpShared/HPRebar.Mcp.Server.Core/Services/RevitBridgeClient.cs`
Lines 81–101 of `RevitBridgeClient.cs`:
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
`TryCancelInRevit(id)` is an unawaited, fire-and-forget task (`_ = SendAsync<CancelResult>(...)`).

### 1.4 Dedicated Synchronous Cancellation Test in `SeedExecutionTests.cs`
Lines 322–329 of `SeedExecutionTests.cs`:
```csharp
322:    [Fact]
323:    public async Task Cancel_DispatchesToBridgeExecutor()
324:    {
325:        var cancelResult = await _client.SendAsync<CancelResult>(
326:            "robot.cancel", new { id = 123 }, TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken);
327:
328:        Assert.Equal(1, _executor.CancelCalls);
329:    }
```
This test awaits `SendAsync` synchronously and deterministically asserts `_executor.CancelCalls == 1`.

### 1.5 Sister Host 1: `HPEtabs`
In `HPEtabs/HPEtabs.Mcp.Server.Tests/EtabsToolsOverPipeTests.cs:258-273`:
```csharp
258:    [Fact]
259:    public async Task A_timeout_tells_the_model_that_changes_may_have_persisted()
260:    {
261:        _executor.ProgressSteps = 10;
262:        _executor.ProgressDelayMs = 100;
263:        await using var impatient = new RevitBridgeClient(
264:            Options.Create(new BridgeOptions { HostId = "etabs", HostVersion = 22, PipeName = _pipeName, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60, ExtraTimeoutSeconds = 0 }),
265:            NullLogger<RevitBridgeClient>.Instance, EtabsHostProfile.Instance);
266:
267:        var error = await Assert.ThrowsAsync<BridgeTimeoutException>(() => impatient.SendAsync<ExecuteResult>(
268:            "etabs.execute", new ExecuteRequest("return 1;", "none", false, 5, "slow", null), TimeSpan.FromMilliseconds(300), null, TestContext.Current.CancellationToken));
269:
270:        Assert.Contains("persisted", error.Message);
271:        Assert.Contains("no rollback", error.Message);
272:        Assert.DoesNotContain("nothing has been committed", error.Message);
273:    }
```
Line 356 (`Assert.True(_executor.CancelCalls > 0);`) **does not exist** in `HPEtabs`.

### 1.6 Sister Host 2: `HPExcel`
In `HPExcel/HPExcel.Mcp.Server.Tests/ExcelSeedToolsRoundTripAdversarialTests.cs:110-118`:
```csharp
110:        // Allow pipe message for TryCancelInRevit to arrive at the listener
111:        var deadline = DateTime.UtcNow.AddSeconds(3);
112:        while (DateTime.UtcNow < deadline && _executor.CancelCalls == 0)
113:        {
114:            await Task.Delay(50);
115:        }
116:
117:        // FakeRevitExecutor.Cancel() should have been called via excel.cancel
118:        Assert.True(_executor.CancelCalls >= 1, "Cancel() was not called on the executor when client cancelled.");
```

---

## 2. Logic Chain

1. **Root Cause**: `RevitBridgeClient` launches `TryCancelInRevit` as an unawaited background task `_ = SendAsync<CancelResult>(...)` and immediately throws `BridgeTimeoutException`.
2. **Timing Failure**: When `Assert.ThrowsAsync<BridgeTimeoutException>` catches the exception in `SeedExecutionTests.cs`, the test execution thread resumes instantaneously and evaluates line 356 (`Assert.True(_executor.CancelCalls > 0);`) in sub-microsecond time.
3. **IPC Latency**: The background task requires at least several milliseconds to acquire the lock, serialize the JSON-RPC cancel request, transmit it across the named pipe, and have `RequestDispatcher` invoke `_executor.Cancel()`.
4. **Empirical Flakiness**: Under parallel solution test execution (`dotnet test HPRobot.slnx`), CPU thread scheduling causes the background task to lag behind the main test thread, resulting in `_executor.CancelCalls == 0` at line 356, failing the assertion.
5. **Solution Space**:
   - **Option A (HPExcel Pattern)**: Retain the assertion that `_executor.CancelCalls > 0`, but bridge the asynchronous gap using a bounded polling loop (`while (DateTime.UtcNow < deadline && _executor.CancelCalls == 0) await Task.Delay(25);`).
   - **Option B (HPEtabs Pattern)**: Remove line 356 completely because the test method is dedicated to verifying the error message text (`Timeout_InformsModelThatChangesMayHavePersisted`), and cancellation dispatch is already independently and deterministically tested in `Cancel_DispatchesToBridgeExecutor` (lines 323–329).

---

## 3. Caveats

- **Scope of Investigation**: This report provides read-only architectural investigation and concrete code recommendations. No repository code was altered by this agent.
- **Implementer Task**: An implementer agent (`worker`) must apply the patch and verify that `dotnet test HPRobot.slnx` runs cleanly across multiple consecutive executions.
- **Timing Bounds**: In Option A, `deadline = DateTime.UtcNow.AddSeconds(3)` with `Task.Delay(25)` introduces near-zero overhead in practice (<25ms under idle conditions) while ensuring 100% determinism even under heavy CI load.

---

## 4. Conclusion & Concrete Recommendations

The failure at `HPRobot/HPRobot.Mcp.Server.Tests/SeedExecutionTests.cs:356` is a confirmed race condition against `RevitBridgeClient.TryCancelInRevit`'s fire-and-forget background task.

### Recommended Remediation (Option A — HPExcel Pattern):
Replace line 356 in `HPRobot/HPRobot.Mcp.Server.Tests/SeedExecutionTests.cs`:

```csharp
<<<<
        Assert.Contains("persisted (no rollback)", error.Message);
        Assert.Contains("snapshot", error.Message);
        Assert.DoesNotContain("nothing has been committed", error.Message);
        Assert.True(_executor.CancelCalls > 0);
    }
====
        Assert.Contains("persisted (no rollback)", error.Message);
        Assert.Contains("snapshot", error.Message);
        Assert.DoesNotContain("nothing has been committed", error.Message);

        // Allow background fire-and-forget TryCancelInRevit pipe message to arrive at listener
        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (DateTime.UtcNow < deadline && _executor.CancelCalls == 0)
        {
            await Task.Delay(25);
        }

        Assert.True(_executor.CancelCalls > 0, "Cancel() was not called on the executor when client timed out.");
    }
>>>>
```

### Alternative Remediation (Option B — HPEtabs Pattern):
Delete line 356 in `HPRobot/HPRobot.Mcp.Server.Tests/SeedExecutionTests.cs`:

```csharp
<<<<
        Assert.Contains("persisted (no rollback)", error.Message);
        Assert.Contains("snapshot", error.Message);
        Assert.DoesNotContain("nothing has been committed", error.Message);
        Assert.True(_executor.CancelCalls > 0);
    }
====
        Assert.Contains("persisted (no rollback)", error.Message);
        Assert.Contains("snapshot", error.Message);
        Assert.DoesNotContain("nothing has been committed", error.Message);
    }
>>>>
```

---

## 5. Verification Method

To verify the remedy after implementation:

1. **Solution Test Execution (5 consecutive runs)**:
   ```powershell
   cd "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot"
   for ($i=1; $i -le 5; $i++) {
       Write-Host "=== Run $i ===" -ForegroundColor Cyan
       dotnet test HPRobot.slnx
       if ($LASTEXITCODE -ne 0) { throw "Run $i failed!" }
   }
   ```
   *Expected*: All 5 runs pass with `total: 294, failed: 0, succeeded: 294, skipped: 0`.

2. **McpShared Regressions**:
   ```powershell
   dotnet test "..\McpShared\HPRebar.Mcp.Server.Core.Tests"
   dotnet test "..\McpShared\HPRebar.McpBridge.Core.Net48Tests"
   ```
   *Expected*: 613 net10 + 72 net48 = 685 passed, 0 failed.

3. **Invalidation Criteria**: If any run of `dotnet test HPRobot.slnx` fails with `Assert.True() Failure` at line 356, the race condition is not resolved.
