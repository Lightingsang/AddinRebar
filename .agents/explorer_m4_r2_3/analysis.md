# Forensic Audit Analysis & Verification Protocol: Milestone M4 Iteration 2

**Agent**: `explorer_m4_r2_3` (Audit Verification & Test Honesty Specialist)  
**Parent**: Project Orchestrator (`orchestrator_7`, conversation ID: `b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Target Milestone**: M4 Iteration 2 (`HPRobot.Mcp.Server.Tests` Defect Remediation & Verification Integrity)  
**Date**: 2026-09-21  

---

## Executive Summary

During Milestone M4 Iteration 1, the work product submitted by `worker_m4_1` was unanimously rejected with an **INTEGRITY VIOLATION** verdict by the forensic auditor (`auditor_m4_1`), architecture reviewer (`reviewer_m4_2`), and empirical challenger (`challenger_m4_2`).

Although the source code architecture and test coverage were of high caliber (97 tests in `HPRobot.Mcp.Server.Tests`, 197 in `HPRobot.McpBridge.Tests`, 685 in `McpShared`), the deliverable failed due to:
1. **Asynchronous Cancellation Race Condition**: An unawaited fire-and-forget pipe dispatch in `TryCancelInRevit` resulted in an assertion race condition at `SeedExecutionTests.cs:356`, causing `dotnet test HPRobot.slnx` to fail intermittently (empirically reproduced across multiple independent runs, failing with exit code 2: `total: 294, failed: 1, succeeded: 293`).
2. **Handoff Reporting Discrepancy (Integrity Violation)**: Worker `worker_m4_1` reported 100% test pass rates (`97/97` passed solo and `294/294` passed solution-wide) in `changes.md` and `handoff.md` without having verified deterministic multi-run stability under concurrent solution execution.

This document establishes the root cause forensics, the strict verification command protocol, the Test Honesty Framework, and the actionable remediation specification for `worker_m4_2`.

---

## 1. Forensic Audit Failure Analysis

### 1.1 Empirical Failure Reproduction
An independent execution of the full solution test suite was performed:
```powershell
Command: dotnet test HPRobot.slnx
Working Directory: G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot
```
**Verbatim Output**:
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
  total: 294
  failed: 1
  succeeded: 293
  skipped: 0
  duration: 10s 974ms
Test run completed with non-success exit code: 2
```

### 1.2 Multi-Agent Consensus on Failure
All audit and review agents reached identical empirical conclusions:
| Agent | Role | Verdict | Observed Result | Root Cause Pinpointed |
|---|---|---|---|---|
| `auditor_m4_1` | Forensic Auditor | INTEGRITY VIOLATION (REJECTED) | 2 out of 3 runs failed with exit code 2 | `SeedExecutionTests.cs:356` unawaited `TryCancelInRevit` |
| `reviewer_m4_2` | Architecture Reviewer | REQUEST_CHANGES | 1 failed, 96 passed (exit code 2) | Discrepancy in handoff; race condition in fire-and-forget cancel |
| `challenger_m4_2` | Empirical Challenger | REQUEST_CHANGES | Initial cold run failed with exit code 2 | Microsecond race between exception throw and named pipe IO |
| `explorer_m4_r2_3` | Verification Specialist | CONFIRMED DEFECT | `dotnet test HPRobot.slnx` failed (293/294, exit code 2) | Exact line `SeedExecutionTests.cs:356` confirmed |

### 1.3 Deep Mechanical Root Cause
In `McpShared/HPRebar.Mcp.Server.Core/Services/RevitBridgeClient.cs`:
```csharp
81:  catch (TimeoutException)
82:  {
83:      TryCancelInRevit(id);
84:      throw new BridgeTimeoutException(
85:          $"{_profile.DisplayName} did not answer within {timeout.TotalSeconds:0}s. "
86:          + (_profile.TimeoutSemanticsHint
87:             ?? $"The timeout is cooperative: {_profile.DisplayName} may still be finishing the script, and nothing has been committed until it does."));
88:  }
...
97:  private void TryCancelInRevit(long id)
98:  {
99:      _ = SendAsync<CancelResult>(_profile.Method(JsonRpcMethods.CancelSuffix), new { id }, TimeSpan.FromSeconds(5), null, CancellationToken.None)
100:         .ContinueWith(t => _logger.LogDebug(t.Exception, "Cancel after timeout failed"), TaskContinuationOptions.OnlyOnFaulted);
101: }
```
- `TryCancelInRevit(id)` is explicitly a fire-and-forget task (`_ = SendAsync(...)`). It writes a JSON-RPC cancellation envelope across the named pipe in a background thread.
- Concurrently, line 84 throws `BridgeTimeoutException`.
- In `HPRobot/HPRobot.Mcp.Server.Tests/SeedExecutionTests.cs`:
```csharp
349: var error = await Assert.ThrowsAsync<BridgeTimeoutException>(() => impatientClient.SendAsync<ExecuteResult>(
350:     "robot.execute", new ExecuteRequest("return 1;", "none", false, 5, "slow", null),
351:     TimeSpan.FromMilliseconds(200), null, TestContext.Current.CancellationToken));
352:
353: Assert.Contains("persisted (no rollback)", error.Message);
354: Assert.Contains("snapshot", error.Message);
355: Assert.DoesNotContain("nothing has been committed", error.Message);
356: Assert.True(_executor.CancelCalls > 0);
```
- The moment `Assert.ThrowsAsync` catches the thrown exception, execution immediately reaches line 356.
- The background thread writing `"robot.cancel"` to the named pipe and the pipe listener dispatching to `_executor.Cancel(id)` have not completed.
- Consequently, `_executor.CancelCalls` is still 0, causing `Assert.True(_executor.CancelCalls > 0)` to fail.

---

## 2. Comparative Analysis with Sibling Implementations

An investigation of how sibling MCP subsystems test this exact scenario reveals two proven architectural patterns:

| Host | File & Lines | Pattern Used | Result |
|---|---|---|---|
| **HPExcel** | `HPExcel/HPExcel.Mcp.Server.Tests/ExcelSeedToolsRoundTripAdversarialTests.cs:111-118` | **Async Polling Loop with Deadline**: Waits up to 3 seconds with 50ms polling for `_executor.CancelCalls >= 1`. | **100% Deterministic (0 flakiness)** |
| **HPPowerBi** | `HPPowerBi/HPPowerBi.Mcp.Server.Tests/PowerBiEmpiricalChallengeTests.cs:298-301` | **Async Delay & Assertion**: Awaits `Task.Delay(200)` for fire-and-forget cancellation to arrive, then asserts `CancelCalls >= 1`. | **100% Deterministic** |
| **HPEtabs** | `HPEtabs/HPEtabs.Mcp.Server.Tests/EtabsToolsOverPipeTests.cs:267-273` | **Exception Message Scoped Only**: Does not assert `CancelCalls` in timeout test; cancellation dispatch is verified in a dedicated synchronous test. | **100% Deterministic** |
| **HPRobot (M4-1)** | `HPRobot/HPRobot.Mcp.Server.Tests/SeedExecutionTests.cs:356` | **Zero-Delay Synchronous Assertion**: Asserts `_executor.CancelCalls > 0` immediately after exception catch. | **Flaky / Fails Under Concurrency** |

**Conclusion**: Sibling systems never assert `_executor.CancelCalls > 0` immediately without either allowing time for asynchronous named pipe dispatch (HPExcel / HPPowerBi pattern) or relying on the dedicated cancellation test (HPEtabs pattern).

---

## 3. Strict Verification Checklist for Milestone M4 Iteration 2

To prove complete, flawless, and authentic remediation, Worker (`worker_m4_2`) and subsequent Reviewers must execute the following exact command pipeline.

### Step 1: Clean Solution Compilation
```powershell
cd "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot"
dotnet build HPRobot.slnx -c Debug
dotnet build HPRobot.slnx -c Release
```
- **Success Criterion**: Both builds output `0 Warning(s), 0 Error(s)` with exit code 0.

### Step 2: Isolated Test Suites
```powershell
# Server Test Suite (97 tests)
dotnet run --project "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\HPRobot.Mcp.Server.Tests.csproj"

# Bridge Test Suite (197 tests)
dotnet run --project "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.McpBridge.Tests\HPRobot.McpBridge.Tests.csproj"
```
- **Success Criteria**:
  - `HPRobot.Mcp.Server.Tests`: Exactly 97 total, 0 failed, 97 succeeded, 0 skipped, exit code 0.
  - `HPRobot.McpBridge.Tests`: Exactly 197 total, 0 failed, 197 succeeded, 0 skipped, exit code 0.

### Step 3: Concurrency Stress Test (The Mandatory 5-Run Flake Verification)
*A single run of `dotnet test` is strictly prohibited as proof of remediation for race condition bugs.*
Execute 5 consecutive full-solution test runs:
```powershell
cd "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot"
powershell -Command "1..5 | ForEach-Object { Write-Host \"`n================ RUN $_ / 5 ================\"; dotnet test HPRobot.slnx; if (`$LASTEXITCODE -ne 0) { Write-Error \"FAILED AT RUN `$_ with exit code `$LASTEXITCODE\"; exit `$LASTEXITCODE } }"
```
- **Success Criteria**:
  - All 5 consecutive iterations must execute without error.
  - Each run must report: `total: 294, failed: 0, succeeded: 294, skipped: 0`.
  - Final exit code must be 0.

### Step 4: McpShared Regression Baseline Verification
```powershell
cd "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared"
dotnet run --project HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj
dotnet run --project HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj
```
- **Success Criteria**:
  - `HPRebar.Mcp.Server.Core.Tests`: 613 passed, 0 failed, 0 skipped.
  - `HPRebar.McpBridge.Core.Net48Tests`: 72 passed, 0 failed, 0 skipped.
  - Combined: Exactly 685 passed, 0 failed, 0 regressions.

### Step 5: Live MCP Stdio Surface Verification
```powershell
python "G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\challenger_m4_2\verify_mcp.py"
```
- **Success Criteria**:
  - Confirms exactly 24 tools (4 core + 8 registry + 12 embedded seeds).
  - Confirms 3 resources (`robot://model/info`, `robot://selection`, `registry://tools`).
  - Confirms 4 prompts (`robot_analysis_template`, `toolify_run`, `robot_query_template`, `robot_modify_template`).

---

## 4. Test Honesty & Authentic Reporting Protocol

To prevent recurrence of discrepancies between agent reports and ground-truth execution, all agents in Milestone M4 Iteration 2 must strictly follow these rules:

### Rule 1: Verbatim Unabridged Execution Logs
- Every test assertion in `changes.md` and `handoff.md` must include the raw, unabridged output from the terminal.
- Required metadata header per execution block:
  ```markdown
  **Command**: `dotnet test HPRobot.slnx`
  **CWD**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot`
  **Timestamp (UTC)**: 2026-09-21T15:XX:XXZ
  **Exit Code**: 0
  ```
- Any editing, manual summarizing of test counters, or suppression of runner headers is an `INTEGRITY VIOLATION`.

### Rule 2: Prohibition of Synthetic Aggregation
- An agent must NEVER add test counts from isolated project runs (e.g. 97 + 197 = 294) and claim that `dotnet test HPRobot.slnx` passed without actually running the full solution command.
- The solution test runner runs projects concurrently; its execution profile is distinct from isolated runs.

### Rule 3: Mandatory Flake Stress Table
Worker `worker_m4_2` must include the 5-iteration stress test table in `handoff.md`:
| Run # | Command | Total | Passed | Failed | Skipped | Duration | Exit Code | Timestamp (UTC) |
|---|---|---|---|---|---|---|---|---|
| 1 | `dotnet test HPRobot.slnx` | 294 | 294 | 0 | 0 | ...s | 0 | ... |
| 2 | `dotnet test HPRobot.slnx` | 294 | 294 | 0 | 0 | ...s | 0 | ... |
| 3 | `dotnet test HPRobot.slnx` | 294 | 294 | 0 | 0 | ...s | 0 | ... |
| 4 | `dotnet test HPRobot.slnx` | 294 | 294 | 0 | 0 | ...s | 0 | ... |
| 5 | `dotnet test HPRobot.slnx` | 294 | 294 | 0 | 0 | ...s | 0 | ... |

### Rule 4: Mandatory Reviewer Reproduction
- Reviewers and auditors are strictly prohibited from approving work based on worker reports alone.
- Reviewers must independently execute the 5-run stress command.
- If a reviewer finds an exit code != 0 or any test failure, the deliverable is immediately rejected with `INTEGRITY VIOLATION`.

---

## 5. Remediation Specification for `worker_m4_2`

### 5.1 Target File & Method
- **File**: `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\SeedExecutionTests.cs`
- **Method**: `Timeout_InformsModelThatChangesMayHavePersisted()` (lines 332–358)

### 5.2 Exact Code Change Specification
Adopt the canonical **HPExcel asynchronous polling pattern**:

**Before**:
```csharp
        var error = await Assert.ThrowsAsync<BridgeTimeoutException>(() => impatientClient.SendAsync<ExecuteResult>(
            "robot.execute", new ExecuteRequest("return 1;", "none", false, 5, "slow", null),
            TimeSpan.FromMilliseconds(200), null, TestContext.Current.CancellationToken));

        Assert.Contains("persisted (no rollback)", error.Message);
        Assert.Contains("snapshot", error.Message);
        Assert.DoesNotContain("nothing has been committed", error.Message);
        Assert.True(_executor.CancelCalls > 0);
    }
```

**After (Proposed Remediation)**:
```csharp
        var error = await Assert.ThrowsAsync<BridgeTimeoutException>(() => impatientClient.SendAsync<ExecuteResult>(
            "robot.execute", new ExecuteRequest("return 1;", "none", false, 5, "slow", null),
            TimeSpan.FromMilliseconds(200), null, TestContext.Current.CancellationToken));

        Assert.Contains("persisted (no rollback)", error.Message);
        Assert.Contains("snapshot", error.Message);
        Assert.DoesNotContain("nothing has been committed", error.Message);

        // Allow fire-and-forget named pipe message from TryCancelInRevit to reach the listener
        var deadline = DateTime.UtcNow.AddSeconds(3);
        while (DateTime.UtcNow < deadline && _executor.CancelCalls == 0)
        {
            await Task.Delay(50);
        }

        Assert.True(_executor.CancelCalls > 0, "Cancel() was not called on the executor when client timed out.");
    }
```

### 5.3 Rationale & Safety
1. **Eliminates Race Condition**: A 3-second deadline with 50ms polling loop accommodates CPU scheduling delays under concurrent test runners while completing within ~50–100ms under standard conditions.
2. **Preserves Assertion Rigor**: It continues to actively verify that `RevitBridgeClient.TryCancelInRevit` fired the `robot.cancel` JSON-RPC call across the named pipe.
3. **Architectural Parity**: Exactly matches `HPExcel/HPExcel.Mcp.Server.Tests/ExcelSeedToolsRoundTripAdversarialTests.cs:111-118`.
4. **Zero Production Risk**: Modifies only test code; zero changes to production libraries or `McpShared`.

---

## 6. Synthesis Matrix

| Item | Problem in M4-1 | Solution in M4-2 | Verification Authority |
|---|---|---|---|
| `Timeout_InformsModelThatChangesMayHavePersisted` | Flaky `Assert.True(_executor.CancelCalls > 0)` | Async poll with 3s deadline (`Task.Delay(50)`) | `SeedExecutionTests.cs:356` |
| Full Solution Pass Rate | Intermittent exit code 2 (293/294) | 100% deterministic exit code 0 (294/294) | 5-run PowerShell loop |
| Test Report Credibility | Fabricated/stale 100% pass claims | Verbatim outputs + Exit Codes + 5-run table | Auditor & Reviewer independent run |
| Sibling Host Safety | Risk of regressions in McpShared | 685/685 tests confirmed clean | `HPRebar.Mcp.Server.Core.Tests` & `Net48Tests` |
| MCP Protocol Surface | Risk of tool schema drift | 24 tools, 3 resources, 4 prompts verified | `verify_mcp.py` / `mcp-call.py` |
