# Empirical Challenge & Stress-Test Report: Milestone 2 `HPTekla.McpBridge`

**Date**: 2026-09-22T01:10:00Z  
**Agent**: `teamwork_preview_challenger_m2_1` (EMPIRICAL CHALLENGER: critic, specialist)  
**Target Subsystem**: `HPTekla/HPTekla.McpBridge` (.NET Framework 4.8 In-Process Plugin)  
**Target Host**: Trimble Tekla Structures 2025.0 (`TeklaStructures.exe`)  
**Verdict**: **REQUEST_CHANGES**

---

## 1. Executive Summary

We conducted comprehensive, adversarial empirical stress-testing on the Milestone 2 deliverable `HPTekla.McpBridge`.
The core architecture, atomic dryRun rollback, snapshot backup engine, and 3-tier AST safety classification are well-designed and rigorously implemented.
However, an empirical inspection of execution cancellation and timeout controls uncovered a **high-severity defect**: `request.TimeoutSeconds` is completely ignored in `TeklaBridgeExecutor.cs`, creating no timeout cancellation token. Consequently, infinite loops or hanging operations inside Tekla will never automatically time out.

---

## 2. Challenge Summary

**Overall risk assessment**: **MEDIUM** (High functional defect in timeout handling, but core transaction and rollback semantics are rock-solid).

---

## 3. Detailed Challenges & Findings

### [High] Challenge 1: Missing Per-Request Timeout CTS in `TeklaBridgeExecutor.cs`

- **Assumption challenged**: That AI scripts respect `request.TimeoutSeconds` and abort automatically when exceeding the timeout limit.
- **Attack scenario**: An AI client or script enters a long-running iteration or infinite loop (e.g. `while (true) {}` or querying millions of points without an exit condition) with `timeoutSeconds = 30`.
- **Observation**:
  In `HPTekla/HPTekla.McpBridge/TeklaBridgeExecutor.cs` (line 164):
  ```csharp
  using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
  _currentCancel = linkedCts;
  ```
  `cancellationToken` received from `RequestDispatcher` is only the named pipe session lifetime token. `request.TimeoutSeconds` is never inspected, clamped, or used to instantiate a `timeoutCts`.
  Across all other bridges in the repository (`HPNavis`, `HPExcel`, `HPPowerBi`, `HPRobot`), timeout is enforced via:
  ```csharp
  var timeoutSeconds = Math.Clamp(request.TimeoutSeconds, 5, HostScriptContracts.TeklaHeavyMaxTimeoutSeconds);
  using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
  using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
  ```
- **Blast radius**: If a script hangs or loops infinitely, Tekla Structures' UI/Model thread remains permanently occupied. The bridge becomes unresponsive (`IsBusy == true`) until the user manually invokes `cancel_execution` or closes the pipe client.
- **Mitigation**: Update `TeklaBridgeExecutor.cs` to instantiate a `timeoutCts` from `request.TimeoutSeconds` clamped between 5 and 300 seconds, and link it into `linkedCts`.

---

### [Low] Challenge 2: Snapshot Resilience on Ephemeral/Transient Database Files

- **Assumption challenged**: That all enumerated database files remain accessible and locked throughout the copy loop.
- **Attack scenario**: During `Directory.EnumerateFiles(modelPath, pattern)`, Tekla Structures deletes or swaps an ephemeral `.db2` transaction log file before `CopyFileShared` opens it.
- **Observation**:
  In `HPTekla/HPTekla.McpBridge/TeklaSnapshotManager.cs` (lines 94-104):
  ```csharp
  var extensionsToCopy = new[] { "*.db1", "*.db2", "environment.db", "options_model.db" };
  foreach (var pattern in extensionsToCopy)
  {
      foreach (var sourceFile in Directory.EnumerateFiles(modelPath, pattern, SearchOption.TopDirectoryOnly))
      {
          var fileName = Path.GetFileName(sourceFile);
          var destFile = Path.Combine(snapshotSubDir, fileName);
          CopyFileShared(sourceFile, destFile);
      }
  }
  ```
  If any enumerated file is deleted between enumeration and `CopyFileShared`, `CopyFileShared` throws `FileNotFoundException`. The outer `catch (Exception ex)` catches it, logs an error, and aborts the loop, leaving subsequent database files (such as `options_model.db` or `environment.db`) uncopied.
- **Blast radius**: A non-critical missing ephemeral file could cause an incomplete backup of the model.
- **Mitigation**: Add a per-file `try / catch` inside the `foreach (var sourceFile ...)` loop or check `if (!File.Exists(sourceFile)) continue;` so one missing secondary file does not abort the entire snapshot pass.

---

## 4. Empirical Stress Test Results

| Test Scenario | Target Component | Expected Behavior | Actual Behavior | Verdict |
|---|---|---|---|---|
| **SavePoint & Rollback on dryRun** | `TeklaBridgeExecutor.cs` (lines 192-197) | `TeklaOperation.RollbackToTestSavePoint(true)` is unconditionally called. `model.CommitChanges()` is never reached. | When `request.DryRun == true`, `RollbackToTestSavePoint(resetSelection: true)` executes. `CommitChanges` is inside `else if` and is skipped entirely. | **PASS** |
| **SavePoint & Rollback on Exception** | `TeklaBridgeExecutor.cs` (lines 206-218) | On script error, `RollbackToTestSavePoint(true)` is called before rethrowing. | `catch` block unconditionally invokes `TeklaOperation.RollbackToTestSavePoint(resetSelection: true)`, sets `rolledBack = true`, and rethrows. Outer catch preserves `rolledBack: true`. | **PASS** |
| **Direct Commit Bypass Prevention** | `ScriptGuard.Check` + `GuardProfile.Tekla` | Prevent user scripts from calling `model.CommitChanges()` directly. | Empirically verified with `model.CommitChanges()`, `((Model)model).CommitChanges()`, and `var m = model; m.CommitChanges()`. All 3 variants rejected by `ScriptGuard` with diagnostic `GUARD`. | **PASS** |
| **Shared File Copy under Active Lock** | `TeklaSnapshotManager.CopyFileShared` | Copy database files opened by Tekla with `FileShare.ReadWrite` without `IOException`. | Tested empirically with an active `FileAccess.ReadWrite` / `FileShare.ReadWrite` lock. `CopyFileShared` read and copied the file content perfectly. | **PASS** |
| **Snapshot Naming & Sanitization** | `TeklaSnapshotManager.Sanitize` | Sanitize illegal characters (`/`, `\`, `:`, `*`, `?`, `"`, `<`, `>`, `|`) with `_`. | Tested with `My/Model:Name*With?Bad<Chars>`. Output: `My_Model_Name_With_Bad_Chars_`. Matches specification. | **PASS** |
| **Snapshot Pruning Retention** | `TeklaSnapshotManager.PruneOldSnapshots` | Retain newest 20 snapshots and delete older ones chronologically. | Created 25 test directories. `PruneOldSnapshots(dir, 20)` reduced directory count from 25 to exactly 20. | **PASS** |
| **3-Tier Analysis: Read Queries** | `TeklaTierAnalyzer.Analyze` | `model.GetInfo()`, `enumerator.Current as Beam` classify as Tier 0 (Read). | Verified: `HighestTier == TeklaTier.Read` (0 write hits, 0 destructive hits). | **PASS** |
| **3-Tier Analysis: Object Creation & Mutations** | `TeklaTierAnalyzer.Analyze` | `new Beam()`, `beam.Insert()`, `beam.Modify()` classify as Tier 1 (Write). | Verified: `HighestTier == TeklaTier.Write` (Write hits recorded). | **PASS** |
| **3-Tier Analysis: Destructive Operations** | `TeklaTierAnalyzer.Analyze` | `beam.Delete()`, `CreateIFC4ExportFromAll()`, `CreateNCFiles()` classify as Tier 2 (Destructive). | Verified: `HighestTier == TeklaTier.Destructive`. Tested on direct, chained, and lambda expressions. | **PASS** |
| **Destructive Gating on AllowHeavyOperations** | `TeklaBridgeExecutor.ExecuteAsync` | Block `Destructive` script if `AllowHeavyOperations == false`. | Verified: Blocked at step 2 with `ScriptDiagnostic` code `HEAVY`. Never reaches compiler or main thread queue. | **PASS** |
| **Timeout Enforcement** | `TeklaBridgeExecutor.ExecuteAsync` (line 164) | Create `timeoutCts` from `request.TimeoutSeconds` and trigger cooperative cancellation. | **Failed**: `request.TimeoutSeconds` is ignored. `linkedCts` only wraps pipe cancellation token. | **FAIL** |
| **Solution Compilation** | `dotnet build HPTekla.McpBridge.csproj` | Compile cleanly in Debug and Release. | Both Debug and Release build with **0 errors and 0 warnings**. | **PASS** |
| **Engine Regression Suite** | `HPRebar.McpBridge.Core.Net48Tests` & `HPRebar.Mcp.Server.Core.Tests` | 100% tests pass without regressions. | `Net48Tests`: **113/113 passed (100%)**. `Server.Core.Tests`: **742/742 passed (100%)**. | **PASS** |

---

## 5. Unchallenged Areas

- **Live In-Process Execution Inside Running TeklaStructures.exe**:
  Full live interactive execution against a running Tekla Structures 2025 instance is planned for Milestone 4/5 via the automated live harness (`tools/harness/run-live-verify.ps1`). The in-process COM/CLR environment of TeklaStructures.exe was validated statically and via unit tests against the real installed Tekla Open API assemblies at `C:\Program Files\Tekla Structures\2025.0\bin\`.

---

## 6. Required Changes

1. **In `HPTekla/HPTekla.McpBridge/TeklaBridgeExecutor.cs`**:
   Add timeout token management to `ExecuteAsync`:
   ```csharp
   var timeoutSeconds = Math.Clamp(request.TimeoutSeconds, 5, HostScriptContracts.TeklaHeavyMaxTimeoutSeconds);
   using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
   using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
   _currentCancel = linkedCts;
   ```
2. *(Optional recommendation)* In `HPTekla/HPTekla.McpBridge/TeklaSnapshotManager.cs`:
   Wrap individual file copy in `CreateSnapshot` in a `try / catch` block so a transient missing secondary file does not abort the entire snapshot pass.
