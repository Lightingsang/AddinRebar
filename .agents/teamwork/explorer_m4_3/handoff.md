# Milestone M4: F1–F11 Comprehensive Automated Regression Suite Design (F12) Handoff Report

## 1. Observation

1. **Current Solution Test Execution**:
   - `HPGrasshopper.Mcp.Server.Tests`:
     Command: `dotnet test HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj -c Debug -p:DeployAddin=false`
     Result: **77 passed**, 0 failed, 0 skipped in 4.522s.
   - `HPGrasshopper.E2E.Tests`:
     Command: `dotnet test HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj -c Debug -p:DeployAddin=false`
     Result: **181 passed**, 0 failed, 0 skipped in 9.284s.
   - `HPGrasshopper.McpBridge.Tests` (with exclusion filter `--filter-not-class HPGrasshopper.McpBridge.Tests.Fixtures.FixtureDefinitionTests`):
     Result: **461 passed**, 0 failed, 0 skipped in 16.800s.
   - `HPGrasshopper.McpBridge.Tests` (WITHOUT filter, running all test classes):
     Command: `& './HPGrasshopper.McpBridge.Tests/bin/Debug/net10.0-windows7.0/HPGrasshopper.McpBridge.Tests.exe' --no-progress --timeout 120s`
     Result: **474 passed**, 0 failed, 0 skipped in 16.633s. The 13 tests in `FixtureDefinitionTests` passed cleanly when RhinoCommon/GH_IO assemblies are available.
   - Total current test count: **732 tests** (474 Bridge + 77 Server + 181 E2E).

2. **Source Code Length Audit (Rule C2 Compliance)**:
   PowerShell query measuring line counts across all non-obj/bin `*.cs` files in `HPGrasshopper`:
   - `CanvasUndoScope.cs`: 267 lines.
   - `RevitTransactionScope.cs`: 262 lines.
   - `ContinuousBeamRenumbering.cs`: 260 lines.
   - `GrasshopperBridgeCoordinator.cs`: 259 lines.
   - `Tier5_SeedParameterFuzzingTests.cs`: 253 lines.
   Every single file in `HPGrasshopper` is $\le 267$ lines, strictly complying with Rule C2 ($\le 300$ lines).

3. **Current Coverage Matrix for Audit Findings F1–F11 & F13**:
   - **F1 (Native RPC Routing & Opt-In)**: Covered by 21 tests in `Host/GrasshopperRpcDispatcherTests.cs` (lines 49–249) and `Host/Milestone1ChallengerStressTests.cs` (lines 59–220).
   - **F2 (Dual-Layer Rollback & Player Transaction Boundary)**: Covered by 16 tests in `Execution/DualSafetyRollbackTests.cs` (lines 21–211) and `Execution/Milestone2RollbackAndUndoTests.cs` (lines 24–108).
   - **F3 (Mode "none" Read-Only AST Enforcement)**: Covered by 54 tests across `TargetTypedNewEmpiricalChallengeTests.cs`, `AdversarialEdgeCaseEmpiricalTests.cs`, `LambdaAndModeNoneGateEmpiricalChallengeTests.cs`, and `MultidimensionalAndNestedChallengeTests.cs`.
   - **F4 (Canvas Batch Atomicity & Native Undo Actions)**: Covered by 32 tests across `DualSafetyRollbackTests.cs`, `Milestone2RollbackAndUndoTests.cs`, `CanvasEditorTests.cs`, and `CanvasPortAdversarialTests.cs`.
   - **F5 (Coordinator Queue Lifecycle & Unblocking on Cancellation)**: Covered by 26 tests in `Milestone2QueueAndContextTests.cs`, `QueueLifecycleAndIdentityAdversarialTests.cs`, and `QueueLifecycleEmpiricalStressTests.cs`.
   - **F6 (Document & Definition Identity Verification)**: Covered by 5 tests in `Milestone2QueueAndContextTests.cs` and `QueueLifecycleAndIdentityAdversarialTests.cs`.
   - **F7 (Thread-Safe Context Extraction & Compiler Invalidation)**: Covered by 4 tests in `Milestone2QueueAndContextTests.cs` and `QueueLifecycleAndIdentityAdversarialTests.cs`.
   - **F8 (Seed Contract Honesty)**: Covered by 7 generic schema tests in `HPGrasshopper.Mcp.Server.Tests/Seeds/SeedSchemaValidationTests.cs` (lines 58–130) and `AdversarialSeedStressTests.cs`.
   - **F9 (Input Parameter Control: Rebar Diameter/Normal, Structural Floor, DirectShape)**: Covered by 3 generic ScriptArgs binding tests in `AdversarialSeedStressTests.cs` (lines 36–66).
   - **F10 (Canvas Batch Preflight Errors, ID Mappings, Multi-Port Wire Disconnect)**: Covered by 26 tests in `tools/ChallengerEmpiricalM3Tests.cs`, `CanvasEditorTests.cs`, and `CanvasPortAdversarialTests.cs`.
   - **F11 (Data Tree Path Retention, NickName Disambiguation, Solver Error Mapping)**: Covered by 44 tests across `tools/PlayerDataTreeTests.cs`, `tools/PlayerDataTreeChallengerTests.cs`, `tools/PlayerRunnerTests.cs`, and `Host/GrasshopperRpcDispatcherTests.cs`.
   - **F13 (Image Exporter Zoom Clamping, View Modes, Viewport Preservation)**: Covered by 20 tests in `tools/ImageExporterTests.cs`, `tools/ImageExporterChallengerTests.cs`, and `tools/ChallengerEmpiricalM3Tests.cs`.

4. **Identified Test Gaps**:
   - **Gap 1 (F8 & F9 - Specific Seed Contract & Branching Logic Assertions)**: Existing tests in `HPGrasshopper.Mcp.Server.Tests` validate schema properties generically across all 12 seeds, but lack explicit assertions that the 4 remediated seeds (`facade_panelization`, `curtain_grid_layout`, `generate_stirrup_distribution`, `export_model_to_rhino`) specify `"transaction": "none"`, `"destructive": false`, and contain 0 transaction tokens in their code. Similarly, F9 lacks unit assertions confirming `generate_rebar_curves` calculates cross-product normal and resolves `RebarBarType`, `create_floors_slabs` sets `FLOOR_PARAM_IS_STRUCTURAL`, and `directshape_from_geometry` branches on `Mesh` vs `Solid` while rejecting unsupported shapes.
   - **Gap 2 (F4 - Canvas Batch Solve Failure Atomicity)**: Tests check `CanvasUndoScope` fields via reflection, but lack an empirical execution test proving that when `doc.NewSolution(false)` throws during `CanvasEditor.ExecuteBatch`, `undoScope.Commit()` is NOT invoked, `undoScope.Rollback()` triggers, and the batch result returns `success = false`, `rolledBack = true`.
   - **Gap 3 (F6 & F5 - Document Identity Pass-through & Coordinator Dispose)**: Missing explicit test confirming matching document title (`TargetDocTitle == activeDocTitle`) succeeds without throwing, and coordinator dispose immediately cancels queued items and resets `IsBusy = false`.

---

## 2. Logic Chain

1. **Premise 1**: The dispatch instructs us to investigate and design a comprehensive automated regression test suite covering findings F1 through F11 (and F13), audit existing coverage, identify gaps, and design dedicated regression suites adhering to Clean Code rules (C2 $\le 300$ lines, M3 $\le 50$ lines).
2. **Premise 2**: Observation 1 shows that while the baseline suite has grown to 732 tests through Milestones M1–M3, test coverage is currently dispersed across many ad-hoc challenger and empirical test files.
3. **Premise 3**: Observation 4 identifies specific regression risks (Gaps 1–3) where regressions could slip through if not permanently guarded by dedicated fixtures:
   - Modifying seed manifests could inadvertently re-introduce `transaction: "auto"` or fabricated counts to seeds without breaking existing generic schema tests.
   - Refactoring `CanvasEditor` could move `undoScope.Commit()` back before `NewSolution()`, re-introducing non-atomic solve failures.
   - Modifying `GrasshopperBridgeCoordinator` could cause queue deadlocks or document mismatch bypasses.
4. **Premise 4**: By designing three dedicated regression test classes partitioned into `HPGrasshopper.McpBridge.Tests/Regression/` (each $\le 260$ lines to satisfy Rule C2, methods $\le 35$ lines to satisfy Rule M3), all 12 findings (F1–F11, F13) receive unified, permanent test coverage without touching `McpShared/`.
5. **Premise 5**: Adding these 29 targeted regression tests (13 in `F1_F7_CoreSafetyRegressionTests`, 8 in `F4_F10_F13_CanvasToolRegressionTests`, and 8 in `F8_F9_F11_SeedAndDataTreeRegressionTests`) will expand `HPGrasshopper.McpBridge.Tests` from 474 to **503 tests**, bringing total solution coverage to **761 tests** with 0 exclusions.

---

## 3. Caveats

1. **Host-Specific Execution**: While `HPGrasshopper.McpBridge.Tests` executes headlessly in-process using `SimulatedTransactionContext` and `DispatchProxy` doubles (`TestParamProxy`, `TestComponentProxy`), actual geometric element baking inside Revit requires an attached Revit 2026 process. This is covered by the PowerShell harness (`run-live-verify.ps1`) rather than unit tests.
2. **Rhino Native Dependencies**: `TestInitializer` adds `C:\Program Files\Rhino 8\System` to `PATH`. If running on a machine without Rhino 8 installed, test doubles and simulated scopes allow all regression tests to run cleanly.
3. **McpShared Boundary**: All regression tests reside strictly within `HPGrasshopper/`. Zero tests or contracts in `McpShared/` were modified, preserving host neutrality.

---

## 4. Conclusion

The audit and design for Milestone M4 (Finding F12 Regression Test Suite) is complete.
A complete, actionable design document has been written to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_3\f1_f11_regression_suite_design.md`

### Proposed Regression Test Architecture:

```
HPGrasshopper.McpBridge.Tests/
└── Regression/
    ├── F1_F7_CoreSafetyRegressionTests.cs         (~260 lines, 13 tests)
    │   ├── F1: Native RPC routing for all 4 tools (read, player, batch, image)
    │   ├── F1: Opt-in write rejection (-32001) & read pass-through
    │   ├── F1: Coordinator busy rejection (-32002)
    │   ├── F2: Dual-level rollback (Revit & Canvas) on dryRun: true
    │   ├── F2: Unhandled exception rollback compensation
    │   ├── F2: CancellationToken abort of both layers
    │   ├── F3: Mode "none" rejection of new Transaction / target-typed new(doc)
    │   ├── F3: Mode "none" zero false positives on read-only collector queries
    │   ├── F5: Cancellation while queued unblocks immediately without host tick
    │   ├── F5: ExternalEvent raise denial fast-fails and resets busy
    │   ├── F6: Document title/path matching succeeds
    │   ├── F6: Document title/path mismatch throws InvalidOperationException
    │   └── F7: Compiler invalidation clears cached compiler and inspector
    │
    ├── F4_F10_F13_CanvasToolRegressionTests.cs     (~250 lines, 8 tests)
    │   ├── F4: Batch atomicity: solve exception triggers clean undoScope.Rollback()
    │   ├── F4: Native undo record populates GH_UndoAction items
    │   ├── F10: Batch preflight collects detailed errors per operation and index
    │   ├── F10: Preflight rejects wire targeting nonexistent component
    │   ├── F10: CreatedComponentInfo preserves tempId -> InstanceGuid mappings
    │   ├── F10: Multi-port wire disconnect disconnects exact port pair (1 -> 1)
    │   ├── F13: Image exporter clamps zoom to [0.1, 5.0]
    │   └── F13: Image exporter null canvas returns clean error
    │
    └── F8_F9_F11_SeedAndDataTreeRegressionTests.cs (~250 lines, 8 tests)
        ├── F8: Seed contract honesty: mode "none" and non-destructive on 4 seeds
        ├── F8: Seed contract honesty: no fabricated counts in export_model_to_rhino
        ├── F9: generate_rebar_curves resolves BarNominalDiameter & cross-product normal
        ├── F9: create_floors_slabs applies FLOOR_PARAM_IS_STRUCTURAL
        ├── F9: directshape_from_geometry branches on Mesh vs Solid and rejects invalid
        ├── F11: Data Tree multi-branch extraction preserves {0;0}, {0;1}, {1;0}
        ├── F11: PlayerRunner output disambiguation handles duplicate NickNames
        └── F11: ExecutionEngine maps solver errors to ExecuteResult.IsError = true
```

---

## 5. Verification Method

1. **Verify Design Document**:
   Read `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_m4_3\f1_f11_regression_suite_design.md` to review the detailed test matrices and specifications.

2. **Verify Baseline Test Execution (All 732 tests PASS)**:
   ```powershell
   # 1. Run Mcp.Server.Tests (77 tests)
   dotnet test HPGrasshopper.Mcp.Server.Tests/HPGrasshopper.Mcp.Server.Tests.csproj -c Debug -p:DeployAddin=false

   # 2. Run E2E.Tests (181 tests)
   dotnet test HPGrasshopper.E2E.Tests/HPGrasshopper.E2E.Tests.csproj -c Debug -p:DeployAddin=false

   # 3. Run McpBridge.Tests WITHOUT filter (474 tests)
   & './HPGrasshopper.McpBridge.Tests/bin/Debug/net10.0-windows7.0/HPGrasshopper.McpBridge.Tests.exe' --no-progress --timeout 120s
   ```

3. **Post-Implementation Regression Verification**:
   After the worker creates the three test classes in `HPGrasshopper.McpBridge.Tests/Regression/`:
   ```powershell
   & './HPGrasshopper.McpBridge.Tests/bin/Debug/net10.0-windows7.0/HPGrasshopper.McpBridge.Tests.exe' --no-progress --timeout 120s
   ```
   **Expected Outcome**: Total tests in `HPGrasshopper.McpBridge.Tests` increases to **503 tests** (all PASS, 0 failures, 0 skipped, 0 excluded).
   Total solution tests: **761 tests PASS**.
