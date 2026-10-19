# Dispatch: Worker M3.1 — Dual-Host In-Process Bridge Implementation

**Agent**: `teamwork_preview_worker_m3_1`
**Role**: In-Process Bridge Implementation Specialist
**Milestone**: M3 (Dual-Host In-Process Bridge `HPDynamo.McpBridge`)
**Parent**: `orchestrator_4` (`b20bb4a5-dd75-4c48-9f4d-621e02412393`)

## Mission
Implement Milestone 3 (`HPDynamo.McpBridge` in-process dual-lifecycle bridge) based on the comprehensive blueprints provided by the three Explorers:
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m3_1\handoff.md` (Dual Lifecycle & Manifests)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m3_2\handoff.md` (Execution Coordination & Rollback Engine)
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m3_3\handoff.md` (UI, Opt-In & Project Scaffolding)

Also reference:
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md`
- `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\TEST_READY.md`

## Scope of Work

1. **Project Scaffolding & Solution Registration**:
   - Create `HPDynamo/HPDynamo.McpBridge/HPDynamo.McpBridge.csproj`:
     - Target framework `net8.0-windows7.0`, `<UseWPF>true</UseWPF>`.
     - References to `McpShared/HPRebar.Mcp.Contracts` and `McpShared/HPRebar.McpBridge.Core`.
     - References to Revit 2026 (`RevitAPI`, `RevitAPIUI`) and Dynamo 3.7 (`DynamoCore`, `DynamoCoreWpf`, `DynamoServices`, `DynamoApplications`, `RevitNodes`, `DynamoRevitDS`, `RevitServices`, `DSRevitNodesUI`).
     - MANDATORY: Apply `<Private>false</Private>` on all host references AND include `<ItemDefinitionGroup><Reference><Private>false</Private></Reference></ItemDefinitionGroup>`.
     - Include `RepackMaterialDesign` ILRepack post-build target.
   - Update `HPDynamo/HPDynamo.slnx` to include `HPDynamo.McpBridge/HPDynamo.McpBridge.csproj`.

2. **Dual-Lifecycle Entry & Manifests**:
   - `Entry/DynamoAssemblyResolver.cs`: Proactive resolver hooked into `AppDomain.CurrentDomain.AssemblyResolve` for `AddIns\DynamoForRevit\`.
   - `Entry/DynamoRevitApp.cs`: `IExternalApplication` for Revit auto-start, starting named pipe `hpdynamo-mcp-2026` via `McpBridgeHost`.
   - `Entry/DynamoBridgeViewExtension.cs`: `IViewExtension` for Dynamo 3.7 live canvas hooks and view model binding.
   - `Manifests/HPDynamo.McpBridge.addin`: Revit add-in manifest.
   - `Manifests/HPDynamo_ViewExtensionDefinition.xml`: Dynamo view extension manifest.

3. **Execution Coordination & Rollback Engine**:
   - `Coordinator/DynamoBridgeCoordinator.cs`: Routes requests between Headless and Canvas modes, implements `IBridgeExecutor`.
   - `Coordinator/DynamoContextExtractor.cs`: Builds `ContextResult.Dynamo` (`DynamoInfo` record).
   - `Execution/RevitTransactionScope.cs`: Wraps executions in `TransactionGroup("MCP Dynamo: <label>")`, unconditional `RollBack()` on `dryRun = true`, `Assimilate()` on commit, suppresses modal error dialogs.
   - `Execution/HeadlessGraphRunner.cs`: Headless `.dyn` and DesignScript execution using `RevitDynamoModel`.
   - `Execution/CanvasGraphRunner.cs`: Live canvas graph and DesignScript execution via `DynamoViewModel`.
   - `Support/DynamoScriptGlobals.cs`: Globals passed to Roslyn C# scripts.
   - `Support/AutoDismissFailurePreprocessor.cs`: Auto-dismiss failure preprocessor.
   - `Support/DocumentChangeCounter.cs`: Document change tracking.

4. **UI, Theme Sync & Mandatory Opt-In**:
   - `View/DynamoBridgeStatusView.xaml` & `DynamoBridgeStatusView.xaml.cs`: Modeless WPF window using `MaterialDesignThemes 5.3.2`.
   - `Resources/Themes/DynamoHostTheme.cs`: Theme provider syncing with Revit theme.
   - `Resources/Themes/MaterialThemeBridge.cs`: Runtime Dark/Light theme swapper.
   - `Resources/Themes/DynamoTheme.xaml` & `MaterialBridge.xaml`: Design tokens, fonts (`Segoe UI`), button styles.
   - Opt-in toggle: Checkbox "Allow AI Dynamo execution" (default: false / OFF; code -32001 when disabled).

## Clean Code Governance (MANDATORY)
- Classes <= 300 lines (Rule C2).
- Methods <= 50 lines (Rule M3).
- 0 empty catch blocks, 0 commented-out code (Rules Q-B1, Q-B2, Q-B3).
- Pure logic / No cross-host referencing (McpShared only).

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Verification
- Run `dotnet build "HPDynamo/HPDynamo.slnx"` -> must succeed with 0 errors and 0 warnings.
- Run `powershell -ExecutionPolicy Bypass -File .\run-e2e-dynamo-tests.ps1` -> all 84 acceptance tests must pass.
- Verify bin directory cleanliness: no Revit or Dynamo DLLs copied to `HPDynamo.McpBridge/bin/`.
- Deliver full completion report to `handoff.md`.

## 2026-10-08T09:53:33Z
Received invocation from parent orchestrator_4 (b20bb4a5-dd75-4c48-9f4d-621e02412393) to implement Milestone 3 (Dual-Host In-Process Bridge HPDynamo.McpBridge).
