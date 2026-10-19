# Empirical Challenger Report — Milestone M1 (Findings N1, N2, N3)

**Verdict**: **APPROVE**

---

## 1. Observation

1. **Finding N1 (Rebar Seed Batch Atomicity & Rollback)**:
   - File: `HPGrasshopper/HPGrasshopper.Mcp.Server/Registry/SeedLibrary/Revit/generate_rebar_curves/code.cs` (201 lines).
   - In lines 115–160, curve validation is executed in a dedicated pre-validation loop prior to any element creation:
     ```csharp
     for (int setIndex = 0; setIndex < curveSetElements.Count; setIndex++)
     {
         ...
         var curveSegments = ParseSegments(rawItem.Value, feetPerMeter);
         ...
         if (!CheckCoplanar(curveSegments, tangents, normalVector))
         {
             return new { success = false, message = $"Rebar curve set {setIndex} is non-planar; all segments must lie in a single plane." };
         }
         validatedSets.Add((normalVector, curveSegments));
     }
     ```
   - In lines 162–191, creation of rebars is executed only over `validatedSets`. If any creation step throws or fails:
     ```csharp
     catch (Exception ex)
     {
         throw new InvalidOperationException($"Failed creating rebar for curve set {setIndex}: {ex.Message}", ex);
     }
     ```
   - When tested with mixed valid (Set 0 planar) and invalid (Set 1 non-planar / disconnected), the method returns `success: false` at Phase 1 before calling `Rebar.CreateFromCurves`, ensuring exactly 0 rebars are created (`N1_MixedValidAndNonPlanarSets_RejectsBeforeCreation_ZeroRebarsCreated` in `ChallengerM1N1N2AdversarialTests.cs:48-60`).
   - When creation fails on curve set 1 after curve set 0 succeeded, `InvalidOperationException` is thrown, caught by `RevitTransactionScope`, which executes `txContext.RollbackGroup()` and `txContext.RollbackAutoTx()`. Exactly 0 rebars remain committed (`N1_CreationPhaseFailure_TriggersRevitTransactionRollback_ZeroRebarsCommitted` in `ChallengerM1N1N2AdversarialTests.cs:83-100`).

2. **Finding N2 (Truthful Rollback Reporting Under Double-Faults)**:
   - File: `HPGrasshopper/HPGrasshopper.McpBridge/Execution/RevitTransactionScope.cs` (294 lines).
   - Lines 180–189:
     ```csharp
     private static CanvasRollbackResult SafeRollbackCanvas(ICanvasUndoScope? canvasScope)
     {
         if (canvasScope == null) return CanvasRollbackResult.Clean();
         try { return canvasScope.Rollback(); }
         catch (Exception ex)
         {
             Log.Error(ex, "Exception encountered rolling back canvas undo scope");
             return CanvasRollbackResult.Failed(new[] { $"Canvas rollback threw {ex.GetType().Name}: {ex.Message}" });
         }
     }
     ```
   - Lines 190–204:
     ```csharp
     var canvasRollback = SafeRollbackCanvas(canvasScope);
     var revitRolledBack = SafeRollback(txContext);
     ...
     bool cleanlyRolledBack = revitRolledBack && (canvasRollback == null || canvasRollback.Success);
     return new ExecutionOutcome(null, combinedError, cleanlyRolledBack, timedOut, canvasRollback);
     ```
   - Lines 146–150 (DryRun path):
     ```csharp
     if (request.DryRun && canvasRollback is { Success: false })
     {
         var error = $"Canvas rollback failed with {canvasRollback.Errors.Count} error(s): {string.Join("; ", canvasRollback.Errors)}";
         return new ExecutionOutcome(value, error, false, false, canvasRollback);
     }
     ```
   - Empirical verification tests in `ChallengerM1N1N2AdversarialTests.cs`:
     - `N2_DoubleFault_ActionThrows_CanvasRollbackFails_ReturnsRolledBackFalse` (lines 102–123): `IsError == true`, `RolledBack == false`, error message contains `"Additionally, canvas rollback failed: Port unbind error; Stale wire lock"`.
     - `N2_DoubleFault_ActionThrows_CanvasRollbackThrows_TrappedSafelyWithRolledBackFalse` (lines 125–145): `IsError == true`, `RolledBack == false`, error message contains `"Canvas rollback threw InvalidOperationException"`.
     - `N2_DoubleFault_DryRun_CanvasRollbackFails_ReturnsRolledBackFalse` (lines 147–168): `IsError == true`, `RolledBack == false`, error message contains `"Canvas rollback failed with 1 error(s): Corrupted undo stack"`.
     - `N2_DoubleFault_DryRun_CanvasRollbackThrows_ReturnsRolledBackFalse` (lines 170–187): `IsError == true`, `RolledBack == false`, error message contains `"Canvas rollback threw InvalidOperationException"`.
     - `N2_DoubleFault_Cancellation_CanvasRollbackFails_ReturnsRolledBackFalse` (lines 189–213): `IsError == true`, `RolledBack == false`.

3. **Finding N3 (Batch Solve Boundaries, Cancellation Checkpoints & Deferred Commit)**:
   - File: `HPGrasshopper/HPGrasshopper.McpBridge/Tools/CanvasEditor.cs` (294 lines).
   - Checkpoints verified:
     - Checkpoint 1 (line 21): `cancellationToken.ThrowIfCancellationRequested();` (before preflight)
     - Checkpoint 2 (line 26): `cancellationToken.ThrowIfCancellationRequested();` (before mutations)
     - Checkpoint 3 (lines 80, 83, 85, 87 in `ApplyMutations`): `ct.ThrowIfCancellationRequested();` (between add, remove, connect, disconnect operations)
     - Checkpoint 4 (line 40): `cancellationToken.ThrowIfCancellationRequested();` (before `doc.NewSolution(false)`)
     - Checkpoint 5 (line 42): `cancellationToken.ThrowIfCancellationRequested();` (after `doc.NewSolution(false)`)
     - Deferred Commit (line 43): `if (!isExternalScope) scope.Commit();` (external scope commit is deferred to caller)
     - Catch & Rollback (lines 46–50):
       ```csharp
       catch (OperationCanceledException)
       {
           var rb = scope.Rollback();
           return new { success = false, error = "Operation was cancelled.", rolledBack = rb.Success, rollbackErrors = rb.Errors };
       }
       ```
   - File: `HPGrasshopper/HPGrasshopper.McpBridge/Execution/GrasshopperExecutionEngine.cs`:
     - Lines 135–148 in `RunBatchEditAsync`: passes `scope` as `canvasScope` to `RevitTransactionScope.Execute`. If cancellation or failure occurs, `RevitTransactionScope` rolls back Revit transactions and invokes `SafeRollbackCanvas(canvasScope)`.
   - Empirical verification tests in `ChallengerM1N3AdversarialTests.cs`:
     - `N3_CancellationBeforePreflight_ThrowsOperationCanceledException_NoCommit` (lines 39–49)
     - `N3_CancellationBeforeMutation_ThrowsOperationCanceledException_NoCommit` (lines 51–63)
     - `N3_CancellationDuringBatch_InvokesRollbackAndPreventsCommit` (lines 65–77)
     - `N3_CancellationBeforeSolve_RollsBackScopeAndPreventsCommit` (lines 79–91)
     - `N3_ExecuteBatch_ExternalScope_DefersCommitToCaller` (lines 93–107)
     - `N3_RevitTransactionScope_Cancellation_RollsBackBothRevitAndCanvas` (lines 109–133)
     - `N3_TimeoutExceeded_RollsBackBothRevitAndCanvasTruthfully` (lines 135–158)

4. **Empirical Test Suite Execution Results**:
   - `HPGrasshopper.McpBridge.Tests`: `Passed! - Failed: 0, Passed: 656, Skipped: 0, Total: 656, Duration: 22s 679ms`
   - `HPGrasshopper.Mcp.Server.Tests`: `Passed! - Failed: 0, Passed: 118, Skipped: 0, Total: 118, Duration: 5s 114ms`
   - `HPGrasshopper.E2E.Tests`: `Passed! - Failed: 0, Passed: 181, Skipped: 0, Total: 181, Duration: 8s 789ms`
   - `HPRebar.Mcp.Server.Core.Tests` (McpShared): `Passed! - total: 1047, failed: 0, succeeded: 1047, skipped: 0`
   - `HPRebar.McpBridge.Core.Net48Tests` (McpShared): `Passed! - total: 115, failed: 0, succeeded: 115, skipped: 0`
   - Total HPGrasshopper tests: **955 passed, 0 failed, 0 skipped (100% green)**.
   - Total McpShared tests: **1162 passed, 0 failed, 0 skipped (100% green)**.

5. **Clean Code Rule Compliance**:
   - Rule C2 (File $\le 300$ lines): Verified 100% of C# files in HPGrasshopper are $\le 300$ lines (0 violations).
   - Rule M3 (Method $\le 50$ lines): Verified 100% of methods are $\le 50$ lines.
   - Rule Q-B1 (0 commented-out code): Verified 0 lines of dead/commented code.
   - Rule Q-B2 (0 unannotated empty catches): Verified 0 empty catches.
   - ADR-0007: 12/12 embedded seeds verified against quality rules.
   - `McpShared` immutability: 0 edits introduced in M1.

---

## 2. Logic Chain

1. **From Observation 1**: Because `generate_rebar_curves/code.cs` splits execution into Phase 1 (full geometric pre-validation across all curve sets) and Phase 2 (atomic creation), any non-planar, disconnected, or zero-length curve set causes an immediate exit before `Rebar.CreateFromCurves` is called. If Revit throws during element creation, the script throws an `InvalidOperationException`, which is trapped by `RevitTransactionScope`, rolling back the outer transaction group. Therefore, mixed valid and invalid curve sets guarantee 0 rebars remain committed in the Revit document.
2. **From Observation 2**: Because `RevitTransactionScope` evaluates `cleanlyRolledBack = revitRolledBack && (canvasRollback == null || canvasRollback.Success)`, double-fault conditions where canvas rollback fails or throws while Revit rolls back evaluate to `cleanlyRolledBack = false`. Consequently, `RolledBack` is strictly `false`, `IsError` is `true`, and the specific canvas rollback errors are concatenated into `result.Message`. The bridge never falsely claims clean rollback when one of the resources was not restored.
3. **From Observation 3**: Because `CanvasEditor.ExecuteBatch` checks the cancellation token at checkpoints before preflight, before mutations, between mutation types, before solve, and after solve, and defers scope commit when an external scope is passed, cancellation stops the pipeline safely. If cancelled, `scope.Rollback()` is invoked, `scope.Commit()` is suppressed, and under `RevitTransactionScope`, both Revit and Canvas resources are rolled back.
4. **From Observation 4 and 5**: Because all 955 tests in HPGrasshopper and all 1162 tests in McpShared execute and pass 100% filter-free, and all modified files conform strictly to repo governance (Rule C2 $\le 300$ lines, Rule M3 $\le 50$ lines), the implementation is verified to be stable and robust under adversarial conditions.

---

## 3. Caveats

1. **In-Process Revit UI Session**: The empirical tests run via the headless test runner (using mock transaction contexts and headless document doubles) rather than an interactive live Revit 2026 GUI session. However, all Revit API geometry classes (`XYZ`, `Line`, `Curve`, `ArgumentsInconsistentException`) are bound against the authentic installed `RevitAPI.dll` assembly.
2. **Rhino.Inside Live Canvas**: Live rendering of components on an open Grasshopper viewport was validated via headless canvas undo scope doubles, as live Rhino UI cannot be initialized without a desktop display session.

---

## 4. Conclusion

The implementation of Findings N1, N2, and N3 in HPGrasshopper has been adversarially challenged and empirically verified:
- **N1**: Validated upfront geometric filtering and atomic transaction group rollback. 0 rebars remain committed on mixed valid/invalid sets.
- **N2**: Validated truthful rollback reporting across all double-fault scenarios (exceptions, failed rollbacks, dry-run rollbacks). `IsError = true` and `RolledBack = false` are consistently reported.
- **N3**: Validated cancellation checkpoints before preflight, before mutation, during mutations, before solve, and after solve. Canvas commit is strictly deferred and mutations are rolled back on cancellation.
- **Quality**: 955 HPGrasshopper tests + 1162 McpShared tests pass 100% green. 100% clean code compliance.

**Verdict**: **APPROVE**

---

## 5. Verification Method

To independently reproduce this verification:

1. **Run McpBridge Tests (including adversarial M1 suites)**:
   ```powershell
   dotnet test HPGrasshopper/HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug -p:DeployAddin=false
   # Expected: Passed! - Failed: 0, Passed: 656, Skipped: 0, Total: 656
   ```

2. **Run Mcp Server Tests (including ADR-0007 quality walker)**:
   ```powershell
   dotnet test HPGrasshopper/HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj -c Debug -p:DeployAddin=false
   # Expected: Passed! - Failed: 0, Passed: 118, Skipped: 0, Total: 118
   ```

3. **Run E2E Tests**:
   ```powershell
   dotnet test HPGrasshopper/HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj -c Debug -p:DeployAddin=false
   # Expected: Passed! - Failed: 0, Passed: 181, Skipped: 0, Total: 181
   ```

4. **Verify Clean Code Rule C2 (All C# files $\le 300$ lines)**:
   ```powershell
   python -c "
   import os
   v = [os.path.join(r, f) for r, _, fs in os.walk('HPGrasshopper') for f in fs if f.endswith('.cs') and len(open(os.path.join(r, f), encoding='utf-8').readlines()) > 300]
   print('Violations:', len(v))
   assert len(v) == 0, f'Found {len(v)} files > 300 lines'
   "
   ```
