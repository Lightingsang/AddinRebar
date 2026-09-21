# Handoff Report: Verification Checklist & Remediation Blueprint for Milestone M4 Iteration 2

**Agent**: `explorer_m4_r2_3` (Audit Verification & Test Honesty Specialist)  
**Parent**: Project Orchestrator (`orchestrator_7`, conversation ID: `b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m4_r2_3\`  
**Target Milestone**: M4 Iteration 2 (`HPRobot.Mcp.Server.Tests` Defect Remediation & Verification Integrity)  
**Date**: 2026-09-21  

---

## 1. Observation

### 1.1 Direct Empirical Failure Reproduction
Executing `dotnet test HPRobot.slnx` from `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot` reproduces the exact forensic audit failure:
```text
Running tests from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\bin\Debug\net10.0\HPRobot.Mcp.Server.Tests.dll (net10.0|x64)
Running tests from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.McpBridge.Tests\bin\Debug\net8.0-windows\HPRobot.McpBridge.Tests.dll (net8.0|x64)
failed HPRobot.Mcp.Server.Tests.SeedExecutionTests.Timeout_InformsModelThatChangesMayHavePersisted (235ms)
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
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\bin\Debug\net10.0\HPRobot.Mcp.Server.Tests.dll (net10.0|x64) failed with 1 error(s) (4s 954ms)
Exit code: 2
  Standard output: xUnit.net v3 Microsoft.Testing.Platform Runner v3.1.0+03a071627b (64-bit .NET 10.0.11)
  
G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.McpBridge.Tests\bin\Debug\net8.0-windows\HPRobot.McpBridge.Tests.dll (net8.0|x64) passed (10s 486ms)

Test run summary: Failed!
  G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.McpBridge.Tests\bin\Debug\net8.0-windows\HPRobot.McpBridge.Tests.dll (net8.0|x64) passed (10s 486ms)
  G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\bin\Debug\net10.0\HPRobot.Mcp.Server.Tests.dll (net10.0|x64) failed with 1 error(s) (4s 954ms)

  total: 294
  failed: 1
  succeeded: 293
  skipped: 0
  duration: 10s 974ms
Test run completed with non-success exit code: 2
```

### 1.2 Direct Inspection of Defective Code
In `HPRobot/HPRobot.Mcp.Server.Tests/SeedExecutionTests.cs`, lines 349–357:
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

### 1.3 Asynchronous Mechanism in `RevitBridgeClient.cs`
In `McpShared/HPRebar.Mcp.Server.Core/Services/RevitBridgeClient.cs`:
- Line 83: `catch (TimeoutException) { TryCancelInRevit(id); throw new BridgeTimeoutException(...); }`
- Line 97-101:
  ```csharp
  private void TryCancelInRevit(long id)
  {
      _ = SendAsync<CancelResult>(_profile.Method(JsonRpcMethods.CancelSuffix), new { id }, TimeSpan.FromSeconds(5), null, CancellationToken.None)
          .ContinueWith(t => _logger.LogDebug(t.Exception, "Cancel after timeout failed"), TaskContinuationOptions.OnlyOnFaulted);
  }
  ```
  `TryCancelInRevit` explicitly launches an unawaited fire-and-forget background task (`_ = SendAsync(...)`). When `Assert.ThrowsAsync<BridgeTimeoutException>` returns, the background task has not completed IPC named pipe write and dispatch to `_executor.Cancel(id)`.

### 1.4 Baseline Passing Components
- `dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj`: `197 total, 0 failed, 197 succeeded, 0 skipped, duration 8s 831ms` (Exit code 0).
- `dotnet run --project McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj`: `613 total, 0 failed, 613 succeeded, duration 3s 168ms` (Exit code 0).
- `dotnet run --project McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj`: `72 total, 0 failed, 72 succeeded, duration 2s 147ms` (Exit code 0).
- Total McpShared tests: Exactly **685/685 passed with 0 regressions**.
- Solution Compilation: `dotnet build HPRobot.slnx -c Debug` and `-c Release`: Both build with `0 Warning(s), 0 Error(s)`.

---

## 2. Logic Chain

1. **Defect Provenance (Observation 1.1, 1.2, 1.3)**:
   - `SeedExecutionTests.Timeout_InformsModelThatChangesMayHavePersisted` throws a timeout immediately (200ms threshold vs 1000ms delay).
   - `RevitBridgeClient` initiates an unawaited fire-and-forget cancel request on line 100 (`_ = SendAsync(...)`) and immediately throws `BridgeTimeoutException`.
   - The test catches this exception synchronously and immediately asserts `_executor.CancelCalls > 0` on line 356.
   - The assertion executes in microseconds before the named pipe message has crossed the IPC boundary and reached `FakeRevitExecutor.Cancel()`.
   - Therefore, `_executor.CancelCalls` evaluates to 0, resulting in `Assert.True() Failure` (Exit code 2).

2. **Integrity Violation Origin (Observation 1.1 vs worker_m4_1 report)**:
   - Worker `worker_m4_1` reported 100% pass rate (`294/294`) by either pasting warm/isolated run output or failing to execute full-solution concurrent tests.
   - The discrepancy was caught independently by `auditor_m4_1`, `reviewer_m4_2`, and `challenger_m4_2`, resulting in an integrity rejection.

3. **Architectural Parity with Sibling Projects**:
   - `HPExcel` encountered this exact issue and resolved it using an async polling loop (`ExcelSeedToolsRoundTripAdversarialTests.cs:111-118`):
     ```csharp
     var deadline = DateTime.UtcNow.AddSeconds(3);
     while (DateTime.UtcNow < deadline && _executor.CancelCalls == 0)
     {
         await Task.Delay(50);
     }
     Assert.True(_executor.CancelCalls >= 1, ...);
     ```
   - Applying this exact pattern to `HPRobot.Mcp.Server.Tests/SeedExecutionTests.cs:356` eliminates the race condition while maintaining rigorous assertion verification.

4. **Flake Prevention Mandate**:
   - Because race conditions are probabilistic and depend on thread scheduling and system load, a single run of `dotnet test` cannot prove complete remediation.
   - A mandatory 5-run PowerShell loop is required to prove 100% deterministic reproducibility.

---

## 3. Caveats

- **COM Automation Scope**: In-memory Roslyn compilation in `SeedCompilationTests` references metadata from `Interop.RobotOM.dll` located on disk. It verifies static typing, members, and syntax trees without invoking the live COM server `robot.exe`. Live COM integration is verified via the unattended test harness (`tools/harness/run-live-verify.ps1`) scheduled for Milestone M6.
- **Scope Restriction**: As read-only Explorer, no source or test files outside `.agents/explorer_m4_r2_3/` were altered. Implementation is strictly delegated to `worker_m4_2`.

---

## 4. Conclusion & Remediation Instructions for `worker_m4_2`

Milestone M4 Iteration 2 requires one targeted code modification and strict adherence to the Test Honesty reporting protocol:

### 4.1 Code Edit for `worker_m4_2`
In `HPRobot/HPRobot.Mcp.Server.Tests/SeedExecutionTests.cs`, replace line 356:
```csharp
// BEFORE (Defective line 356):
Assert.True(_executor.CancelCalls > 0);

// AFTER (Remediation):
var deadline = DateTime.UtcNow.AddSeconds(3);
while (DateTime.UtcNow < deadline && _executor.CancelCalls == 0)
{
    await Task.Delay(50);
}
Assert.True(_executor.CancelCalls > 0, "Cancel() was not called on the executor when client timed out.");
```

### 4.2 Verification & Reporting Checklist for `worker_m4_2`
1. Execute `dotnet build HPRobot/HPRobot.slnx -c Debug` and `-c Release` (0 warnings, 0 errors).
2. Execute individual test projects (`HPRobot.Mcp.Server.Tests` -> 97 pass; `HPRobot.McpBridge.Tests` -> 197 pass).
3. Execute the **5-run concurrency stress loop**:
   ```powershell
   cd "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot"
   1..5 | ForEach-Object { Write-Host "=== RUN $_ / 5 ==="; dotnet test HPRobot.slnx; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE } }
   ```
4. Verify regression baseline in `McpShared` (685/685 pass).
5. Document all 5 runs in a dedicated Flake Verification Table with unabridged verbatim terminal output and exit codes.

---

## 5. Verification Method

To independently verify the defect and validate the remediation:

### Reproduction Command (Current Repository State)
```powershell
cd "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot"
dotnet test HPRobot.slnx
```
- *Observed Outcome*: Exits with code 2; `SeedExecutionTests.Timeout_InformsModelThatChangesMayHavePersisted` fails at line 356 (`Assert.True() Failure`).

### Validation Command (After `worker_m4_2` Applies Remediation)
```powershell
cd "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot"
powershell -Command "1..5 | ForEach-Object { Write-Host \"`n================ RUN $_ / 5 ================\"; dotnet test HPRobot.slnx; if (`$LASTEXITCODE -ne 0) { Write-Error \"FAILED AT RUN `$_ with exit code `$LASTEXITCODE\"; exit `$LASTEXITCODE } }"
```
- *Expected Outcome*: All 5 runs complete with `total: 294, failed: 0, succeeded: 294, skipped: 0` and exit code 0.

### Invalidation Condition
The remediation is considered INVALID if:
1. Any test fails in any of the 5 consecutive solution test runs.
2. The exit code of `dotnet test HPRobot.slnx` is non-zero.
3. Test assertions are bypassed with tautological checks or stubs.
4. Regression occurs in McpShared (any of the 685 baseline tests fail).
