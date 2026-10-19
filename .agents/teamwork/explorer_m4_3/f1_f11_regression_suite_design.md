# F1–F11 Comprehensive Automated Regression Test Suite Design (F12)

## 1. Executive Summary

This design establishes a permanent, automated regression safety net covering findings **F1 through F11** (and **F13**) identified during the comprehensive audit of `HPGrasshopper`. 

The suite eliminates test execution exclusions (such as `--filter-not-class FixtureDefinitionTests`), bridges unit test gaps in `HPGrasshopper.McpBridge.Tests`, isolates test fixture generation to temporary storage, and locks in all critical runtime remediation behaviors.

All proposed test classes strictly comply with the repository's Clean Code governance:
- **Rule C2**: File length $\le 300$ lines.
- **Rule M3**: Method length $\le 50$ lines.
- **Rule Q-B1**: Zero commented-out dead code.
- **Rule Q-B2**: Zero unannotated empty catch blocks.
- **Host Neutrality**: Zero modifications to `McpShared/`.

---

## 2. Existing Test Coverage Catalog across Projects

The solution currently comprises three test projects totaling **732 tests**:
1. `HPGrasshopper.McpBridge.Tests`: **474 tests** (including 13 in `FixtureDefinitionTests`).
2. `HPGrasshopper.Mcp.Server.Tests`: **77 tests**.
3. `HPGrasshopper.E2E.Tests`: **181 tests**.

### 2.1 Test Distribution by Finding

| Finding ID | Finding Description | Current Test Locations | Current Tests | Coverage Assessment |
|---|---|---|:---:|---|
| **F1** | Native RPC routing, opt-in & busy checks | `Host/GrasshopperRpcDispatcherTests.cs`<br>`Host/Milestone1ChallengerStressTests.cs` | 21 | High in dispatcher; needs end-to-end server invocation over pipe |
| **F2** | Dual-layer rollback on cancellation/exceptions | `Execution/DualSafetyRollbackTests.cs`<br>`Execution/Milestone2RollbackAndUndoTests.cs` | 16 | Scope and context verified; needs explicit per-resource status assertion |
| **F3** | Mode "none" AST check on Transaction tokens | `Execution/TargetTypedNewEmpiricalChallengeTests.cs`<br>`Execution/AdversarialEdgeCaseEmpiricalTests.cs`<br>`Execution/LambdaAndModeNoneGateEmpiricalChallengeTests.cs`<br>`Execution/MultidimensionalAndNestedChallengeTests.cs`<br>`Execution/Milestone2RollbackAndUndoTests.cs` | 54 | Exhaustive AST check; needs consolidated regression lock-in fixture |
| **F4** | Canvas batch atomicity & native undo actions | `Execution/DualSafetyRollbackTests.cs`<br>`Execution/Milestone2RollbackAndUndoTests.cs`<br>`tools/CanvasEditorTests.cs`<br>`tools/CanvasPortAdversarialTests.cs` | 32 | High; needs solve-exception atomicity and native undo count lock-in |
| **F5** | Coordinator queue lifecycle & unblocking on cancellation | `Coordinator/Milestone2QueueAndContextTests.cs`<br>`Coordinator/QueueLifecycleAndIdentityAdversarialTests.cs`<br>`Coordinator/QueueLifecycleEmpiricalStressTests.cs`<br>`Coordinator/GrasshopperBridgeCoordinatorTests.cs` | 26 | Strong lifecycle coverage; needs coordinator dispose unblock check |
| **F6** | Document & definition identity verification | `Coordinator/Milestone2QueueAndContextTests.cs`<br>`Coordinator/QueueLifecycleAndIdentityAdversarialTests.cs` | 5 | Title/path mismatch tested; needs matching document pass-through and GUID checks |
| **F7** | Thread-safe UI context extraction & compiler invalidation | `Coordinator/Milestone2QueueAndContextTests.cs`<br>`Coordinator/QueueLifecycleAndIdentityAdversarialTests.cs` | 4 | Fallback & invalidation tested; needs UI thread context double check |
| **F8** | Seed contracts honest transaction modes & non-destructive flags | `Seeds/SeedSchemaValidationTests.cs`<br>`Seeds/AdversarialSeedStressTests.cs` (Server.Tests) | 7 | Schema verified; needs explicit unit assertions on `none` mode for 4 seeds |
| **F9** | Input parameter control: rebar diameter/normal, floor structural, directshape | `Seeds/AdversarialSeedStressTests.cs` (Server.Tests) | 3 | Parameter read verified; needs logic & AST branching assertions for 3 seeds |
| **F10** | Canvas batch preflight errors, ID mappings, multi-port wire disconnect | `tools/ChallengerEmpiricalM3Tests.cs`<br>`tools/CanvasEditorTests.cs`<br>`tools/CanvasPortAdversarialTests.cs` | 26 | Strong preflight tests; needs multi-component tempId mapping roundtrip |
| **F11** | Data Tree path retention, NickName disambiguation, solver error detection | `tools/PlayerDataTreeTests.cs`<br>`tools/PlayerDataTreeChallengerTests.cs`<br>`tools/PlayerRunnerTests.cs`<br>`tools/ChallengerEmpiricalM3Tests.cs`<br>`Host/GrasshopperRpcDispatcherTests.cs` | 44 | Strong data tree tests; needs solver error to `ExecuteResult.IsError` integration |
| **F13** | Image exporter zoom clamping, view modes, viewport preservation | `tools/ImageExporterTests.cs`<br>`tools/ImageExporterChallengerTests.cs`<br>`tools/ChallengerEmpiricalM3Tests.cs` | 20 | Zoom bounds tested; needs viewport preservation & mode boundary tests |
| **F12** | Test mocks vs production workload, Fixture isolation | `Fixtures/FixtureDefinitionTests.cs` (excluded filter removed) | 13 | Isolated fixture generation + 3 new dedicated regression fixtures |

---

## 3. Gap Analysis

1. **Gap G1 (F8 & F9 - Seed Contracts & Logic Validation)**:
   While `HPGrasshopper.Mcp.Server.Tests` has `SeedSchemaValidationTests`, it checks generic properties (e.g. modes $\in$ `["auto", "manual", "none"]`) across all seeds. There were no explicit unit tests verifying that the 4 remediated seeds (`facade_panelization`, `curtain_grid_layout`, `generate_stirrup_distribution`, `export_model_to_rhino`) strictly enforce:
   - `transaction == "none"`
   - `destructive == false`
   - Description and Title state "Calculate" / "Plan", not "Create" or "Export"
   - Zero `Transaction` or `TransactionGroup` tokens in their script code
   - No hardcoded dummy counts (`5/10`).
   Likewise, for F9, unit tests must lock in that `generate_rebar_curves` calculates cross-product normal and resolves `RebarBarType`, `create_floors_slabs` applies `FLOOR_PARAM_IS_STRUCTURAL`, and `directshape_from_geometry` branches on `Mesh` vs `Solid` while rejecting unsupported shapes.

2. **Gap G2 (F4 - Canvas Batch Solve Exception Atomicity)**:
   Tests verify `undoScope.Commit()` flag and reflection fields, but need an explicit empirical test demonstrating that if `doc.NewSolution(false)` throws an exception, `undoScope.Commit()` is never called, `undoScope.Rollback()` executes, and the batch response returns `success: false` and `rolledBack: true` without leaving partial modifications.

3. **Gap G3 (F1 & F11 - End-to-End Native RPC over Named Pipe)**:
   Existing tests test `GrasshopperRpcDispatcher.DispatchAsync` in isolation. A dedicated test should verify that when an RPC call passes through the full transport serialization/deserialization pipeline, solver errors and missing required arguments return the exact expected JSON-RPC error codes (-32001, -32002, -32601) and error result structures.

4. **Gap G4 (F5 & F6 - Queue Disposal & Document Identity Pass-through)**:
   Coordinator tests verify cancellation and raise denial, but need tests verifying:
   - Calling `coordinator.Dispose()` while items are queued immediately cancels all pending items with `OperationCanceledException` and resets `IsBusy = false`.
   - Matching document identity (`TargetDocTitle == activeDocTitle`) succeeds without throwing.

---

## 4. Architecture of Dedicated Regression Suites

To permanently lock in these behaviors without exceeding Rule C2 ($\le 300$ lines), the regression suite is partitioned into three dedicated test classes in `HPGrasshopper.McpBridge.Tests/Regression/`:

```
HPGrasshopper.McpBridge.Tests/
└── Regression/
    ├── F1_F7_CoreSafetyRegressionTests.cs         (~260 lines, 13 tests)
    ├── F4_F10_F13_CanvasToolRegressionTests.cs     (~250 lines, 8 tests)
    └── F8_F9_F11_SeedAndDataTreeRegressionTests.cs (~250 lines, 8 tests)
```

### 4.1 Specification: `F1_F7_CoreSafetyRegressionTests.cs`

**Target File**: `HPGrasshopper/HPGrasshopper.McpBridge.Tests/Regression/F1_F7_CoreSafetyRegressionTests.cs`
**Lines**: ~260 lines (Rule C2 compliant)
**Max Method Length**: ~30 lines (Rule M3 compliant)
**Coverage**: Findings F1, F2, F3, F5, F6, F7

**Test Matrix**:
1. `F1_NativeRpc_AllFourTools_RouteWithoutMethodNotFound`:
   Sends `read_grasshopper_canvas`, `export_grasshopper_canvas_image`, `batch_edit_grasshopper_canvas`, `run_grasshopper_player`. Asserts none return -32601.
2. `F1_NativeRpc_WriteTools_WhenExecutionDisabled_RejectWith32001`:
   Verifies write tools fail with `BridgeErrorCode.ExecutionDisabled` (-32001) when `ExecutionEnabled == false`.
3. `F1_NativeRpc_ReadTools_WhenExecutionDisabled_Succeed`:
   Verifies read tools succeed even when `ExecutionEnabled == false`.
4. `F1_NativeRpc_WhenCoordinatorBusy_RejectWith32002`:
   Verifies requests are rejected with `BridgeErrorCode.Busy` (-32002) when another task is active.
5. `F2_DualSafety_DryRun_RollsBackBothRevitAndCanvasScopes`:
   Executes with `dryRun = true`; verifies Revit `TransactionGroup` is rolled back and canvas undo scope executes rollback.
6. `F2_DualSafety_UnhandledException_RollsBackBothLayers`:
   Action throws `InvalidOperationException`; verifies both Revit and Canvas scopes are compensated.
7. `F2_DualSafety_Cancellation_AbortsBothLayersCleanly`:
   Pre-cancelled token aborts execution with `result.IsError == true`, `result.RolledBack == true`.
8. `F3_ModeNone_RejectsExplicitAndTargetTypedTransactions`:
   Tests `new Transaction()`, `new TransactionGroup()`, `new SubTransaction()`, and target-typed `new(doc)`; asserts pre-flight analyzer blocks execution with `"enforces read-only execution"`.
9. `F3_ModeNone_ValidReadOperations_ZeroFalsePositives`:
   Tests read-only query; verifies `analysis.UsesTransaction == false`.
10. `F5_CoordinatorQueue_CancellationWhileQueued_UnblocksImmediately`:
    Cancels queued item before host tick; asserts `OperationCanceledException` without host invocation.
11. `F5_CoordinatorQueue_RaiseDenied_ThrowsFastWithoutLeakingBusy`:
    When `Raise()` returns false, throws `InvalidOperationException` immediately and resets `IsBusy = false`.
12. `F6_DocumentIdentity_MatchingDocument_PassesValidation`:
    When document title matches expected title, passes without exception.
13. `F6_DocumentIdentity_MismatchedTitleOrPath_ThrowsInvalidOperationException`:
    When title or path mismatches, throws `InvalidOperationException` with `"Document identity mismatch"`.
14. `F7_CompilerInvalidation_ClearsAndRecreatesCompilerInstances`:
    Calls `InvalidateCompiler()`; asserts cached compiler is cleared and lazily recreated on next access.

---

### 4.2 Specification: `F4_F10_F13_CanvasToolRegressionTests.cs`

**Target File**: `HPGrasshopper/HPGrasshopper.McpBridge.Tests/Regression/F4_F10_F13_CanvasToolRegressionTests.cs`
**Lines**: ~250 lines (Rule C2 compliant)
**Max Method Length**: ~35 lines (Rule M3 compliant)
**Coverage**: Findings F4, F10, F13

**Test Matrix**:
1. `F4_BatchAtomicity_SolveException_TriggersUndoScopeRollback`:
   Simulates solve failure during batch execution; asserts `undoScope.Commit()` was NOT called, `undoScope.Rollback()` executed, response has `success = false`, `rolledBack = true`.
2. `F4_NativeUndoRecord_PopulatesUndoActionsForOperations`:
   Verifies `CanvasUndoScope` adds native `GH_UndoAction` objects into `GH_UndoRecord` for added/removed objects and modified wires.
3. `F10_BatchPreflight_CollectsDetailedErrorsPerOperation`:
   Submits batch with NaN coordinates and negative port index; verifies preflight collects `Operation`, `Index`, and `Reason`.
4. `F10_BatchPreflight_UnknownComponentTarget_FailsValidation`:
   Submits wire targeting ghost component; verifies preflight failure before canvas mutation.
5. `F10_CreatedComponentInfo_MapsTempIdToInstanceGuid`:
   Verifies `CreatedComponentInfo` records temp ID and valid parsed `InstanceGuid`.
6. `F10_DisconnectWires_MultiPortComponent_DisconnectsExactPortPair`:
   Disconnects port (1 -> 1); verifies port (0 -> 0) remains intact.
7. `F13_ImageExporter_CalculateDimensions_ClampsZoomToStrictBounds`:
   Tests zoom 0.05 (clamped to 0.1) and 10.0 (clamped to 5.0) against boundary dimensions.
8. `F13_ImageExporter_Export_NullCanvas_ReturnsCleanErrorWithoutThrowing`:
   Verifies null canvas returns `success = false` with informative error message.

---

### 4.3 Specification: `F8_F9_F11_SeedAndDataTreeRegressionTests.cs`

**Target File**: `HPGrasshopper/HPGrasshopper.McpBridge.Tests/Regression/F8_F9_F11_SeedAndDataTreeRegressionTests.cs`
**Lines**: ~250 lines (Rule C2 compliant)
**Max Method Length**: ~30 lines (Rule M3 compliant)
**Coverage**: Findings F8, F9, F11

**Test Matrix**:
1. `F8_SeedContractHonesty_TransactionModeNone_AllFourSeeds`:
   Loads tool manifests for `facade_panelization`, `curtain_grid_layout`, `generate_stirrup_distribution`, and `export_model_to_rhino`. Asserts `transaction == "none"`, `destructive == false`, and descriptions contain "Calculate" or "Plan".
2. `F8_SeedContractHonesty_CodeDoesNotContainFabricatedExportCounts`:
   Inspects `export_model_to_rhino/code.cs`; asserts no hardcoded counts (`5/10`) exist.
3. `F9_GenerateRebarCurves_ResolvesClosestBarDiameterAndNormal`:
   Inspects `generate_rebar_curves/code.cs`; asserts code reads `barDiameterMm`, matches `BarNominalDiameter`, and calculates cross-product normal vector.
4. `F9_CreateFloorsSlabs_AppliesStructuralParameter`:
   Inspects `create_floors_slabs/code.cs`; asserts code reads `structural` and sets `FLOOR_PARAM_IS_STRUCTURAL`.
5. `F9_DirectShapeFromGeometry_BranchesOnMeshAndSolid_RejectsUnsupported`:
   Inspects `directshape_from_geometry/code.cs`; asserts code branches on `isMesh` vs `isSolid` and rejects unsupported shapes.
6. `F11_DataTree_MultiBranchStructure_PreservesPathsAndTypes`:
   Extracts volatile data with branches `{0;0}`, `{0;1}`, `{1;0}`; verifies all branches, paths, and types are preserved.
7. `F11_PlayerRunner_ExtractOutputs_TriplicateNickNames_DisambiguatesWithoutCollisions`:
   Extracts outputs with 3 identical NickNames; asserts all 3 are disambiguated with GUID suffixes without collision.
8. `F11_ExecutionEngine_MapToExecuteResult_MapsSolveErrorsToIsError`:
   Verifies solver failure payload maps to `ExecuteResult.IsError = true` and `RolledBack = true`.

---

## 5. Verification Commands

To verify the test suite post-implementation:

```powershell
# 1. Build bridge and test project cleanly
dotnet build HPGrasshopper.McpBridge.Tests/HPGrasshopper.McpBridge.Tests.csproj -c Debug -p:DeployAddin=false

# 2. Run all tests in HPGrasshopper.McpBridge.Tests WITHOUT any filter (including FixtureDefinitionTests and new Regression suites)
& './HPGrasshopper.McpBridge.Tests/bin/Debug/net10.0-windows7.0/HPGrasshopper.McpBridge.Tests.exe' --no-progress --timeout 120s

# 3. Run Mcp.Server.Tests suite
dotnet test HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj -c Debug -p:DeployAddin=false

# 4. Run E2E.Tests suite
dotnet test HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj -c Debug -p:DeployAddin=false
```

Expected Outcome:
- `HPGrasshopper.McpBridge.Tests`: **503+ tests PASS** (474 baseline + 29 new regression tests), 0 failures, 0 exclusions.
- `HPGrasshopper.Mcp.Server.Tests`: **77 tests PASS**, 0 failures.
- `HPGrasshopper.E2E.Tests`: **181 tests PASS**, 0 failures.
- Total Solution: **761+ tests PASS** (100% green).
