# Milestone 2 Handoff Report: Challenger Review of `HPTekla.McpBridge`

**Agent**: `teamwork_preview_challenger_m2_1`  
**Role**: Empirical Challenger (critic, specialist)  
**Target Milestone**: Milestone 2 (`HPTekla.McpBridge`)  
**Verdict**: **REQUEST_CHANGES**

---

## 1. Observation

1. **SavePoint & Rollback in `TeklaBridgeExecutor.cs`**:
   - Lines 176, 192-202:
     ```csharp
     // Set test save point for atomic dryRun or error rollback
     TeklaOperation.SetTestSavePoint();
     ...
     if (request.DryRun)
     {
         TeklaOperation.RollbackToTestSavePoint(resetSelection: true);
         rolledBack = true;
     }
     else if (tier >= TeklaTier.Write)
     {
         _model.CommitChanges(request.Label ?? "HPTekla AI Execution");
     }
     ```
   - Lines 206-219:
     ```csharp
     catch
     {
         try
         {
             TeklaOperation.RollbackToTestSavePoint(resetSelection: true);
             rolledBack = true;
         }
         catch (Exception rbEx)
         {
             Log.Error(rbEx, "Failed to rollback to test save point on exception");
         }
         throw;
     }
     ```
2. **Direct Commit Bypass Prevention via `ScriptGuard`**:
   - `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs` (lines 208, 215) defines `DeniedMembers = ["CommitChanges", ...]` and `DeniedMembersOnIdentifier["model"] = ["CommitChanges"]`.
   - Empirically executed `ScriptGuard.Check` with snippets:
     - `model.CommitChanges();` -> 2 violations (`.CommitChanges is not allowed`, `model.CommitChanges is not allowed`).
     - `var m = model; m.CommitChanges();` -> 1 violation (`.CommitChanges is not allowed`).
     - `((Tekla.Structures.Model.Model)model).CommitChanges();` -> 1 violation (`.CommitChanges is not allowed`).
3. **Snapshot Manager Shared File Access**:
   - `HPTekla/HPTekla.McpBridge/TeklaSnapshotManager.cs` (lines 125-127):
     ```csharp
     using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
     using var destStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None);
     sourceStream.CopyTo(destStream);
     ```
   - Empirically verified under an active `FileAccess.ReadWrite, FileShare.ReadWrite` lock simulating Tekla Structures: `CopyFileShared` read and copied the file without `IOException`.
4. **Snapshot Pruning**:
   - `TeklaSnapshotManager.PruneOldSnapshots`: Tested with 25 test directories; pruned down to exactly 20.
5. **3-Tier AST Classification**:
   - `TeklaTierAnalyzer.Analyze`:
     - `var info = model.GetInfo();` -> `HighestTier: Read`
     - `var part = enumerator.Current as Beam;` -> `HighestTier: Read`
     - `var beam = new Beam(); beam.Insert();` -> `HighestTier: Write`
     - `beam.Delete();` -> `HighestTier: Destructive`
     - `Tekla.Structures.Model.Operations.Operation.CreateIFC4ExportFromAll();` -> `HighestTier: Destructive`
6. **Defect Found — Missing Timeout CTS in `TeklaBridgeExecutor.cs`**:
   - Line 164:
     ```csharp
     using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
     _currentCancel = linkedCts;
     ```
     `request.TimeoutSeconds` is never used to create a timeout cancellation token. In contrast, `RobotBridgeExecutor.cs` line 176 and `ExcelBridgeExecutor.cs` line 187 use:
     ```csharp
     var timeoutSeconds = Math.Clamp(request.TimeoutSeconds, 5, HostScriptContracts.TeklaHeavyMaxTimeoutSeconds);
     using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
     using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
     ```
7. **Compilation & Regression**:
   - `dotnet build HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj -c Debug`: 0 errors, 0 warnings.
   - `dotnet build HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj -c Release`: 0 errors, 0 warnings.
   - `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests`: 113/113 passed (100%).
   - `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests`: 742/742 passed (100%).

---

## 2. Logic Chain

1. From Observation 1, when `request.DryRun == true`, `RollbackToTestSavePoint(resetSelection: true)` is executed in the `if (request.DryRun)` branch, and `_model.CommitChanges()` is on the mutually exclusive `else if` branch; therefore, `CommitChanges` is unreachable when `dryRun == true`.
2. From Observation 1, any exception in script execution jumps to the enclosing `catch` block which unconditionally invokes `TeklaOperation.RollbackToTestSavePoint(resetSelection: true)`, sets `rolledBack = true`, and rethrows; therefore, state rollback is guaranteed on error.
3. From Observation 2, direct calls to `CommitChanges` are intercepted and rejected by `ScriptGuard.Check` at Step 1 before compilation or dispatch; therefore, scripts cannot circumvent the bridge's transaction ownership.
4. From Observation 3 and 4, `TeklaSnapshotManager` correctly handles file locking via `FileShare.ReadWrite`, preserves path safety through `Sanitize`, and limits disk usage via `PruneOldSnapshots`.
5. From Observation 5, `TeklaTierAnalyzer` correctly classifies Read, Write, and Destructive tiers, and `TeklaBridgeExecutor` correctly rejects Destructive actions when `AllowHeavyOperations` is false.
6. From Observation 6, `linkedCts` in `TeklaBridgeExecutor` only wraps `cancellationToken` (the named pipe listener token). Because `request.TimeoutSeconds` is never passed to a `CancellationTokenSource`, scripts that hang or execute infinite loops cannot time out cooperatively. This violates the core host executor contract.

---

## 3. Caveats

- Interactive live execution testing inside a running `TeklaStructures.exe` process is deferred to Milestone 4/5 when the full end-to-end Python harness (`run-live-verify.ps1`) is exercised.
- In `TeklaSnapshotManager.cs`, if a secondary ephemeral database file is deleted by Tekla between directory enumeration and file copy, `CreateSnapshot` aborts the remaining database copies. This is a low-severity resilience caveat, not a blocker.

---

## 4. Conclusion

**Verdict: REQUEST_CHANGES**

`HPTekla.McpBridge` passes all architectural, transactional, snapshot, and safety classification requirements with flying colors. However, **Challenge 1 (Missing Timeout CTS)** must be resolved by the worker before Milestone 2 can be approved:
1. In `HPTekla/HPTekla.McpBridge/TeklaBridgeExecutor.cs`:
   Create a `timeoutCts` from `request.TimeoutSeconds` clamped between 5 and `HostScriptContracts.TeklaHeavyMaxTimeoutSeconds` (300s), and link it to `linkedCts`.

---

## 5. Verification Method

To independently verify the fix:
1. Inspect `HPTekla/HPTekla.McpBridge/TeklaBridgeExecutor.cs` around line 164:
   Ensure `timeoutCts` is created and linked into `_currentCancel`.
2. Recompile:
   `dotnet build HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj -c Debug`
   `dotnet build HPTekla/HPTekla.McpBridge/HPTekla.McpBridge.csproj -c Release`
3. Run regressions:
   `dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests`
   `dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests`
