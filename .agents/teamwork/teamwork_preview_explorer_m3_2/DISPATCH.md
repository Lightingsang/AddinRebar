# Dispatch: Explorer M3.2 — Execution, Rollback & Coordinators Explorer

**Agent**: `teamwork_preview_explorer_m3_2`
**Role**: Execution Engine & Rollback Specialist Explorer
**Milestone**: M3 (Dual-Host In-Process Bridge `HPDynamo.McpBridge`)
**Parent**: `orchestrator_4` (`b20bb4a5-dd75-4c48-9f4d-621e02412393`)

## Mission
Investigate and design the execution coordination and rollback engine for `HPDynamo.McpBridge`:
1. `DynamoBridgeCoordinator.cs`:
   - State management: track whether Dynamo is active in Headless mode (via `RevitDynamoModel`) or Live Canvas mode (via `DynamoViewModel`).
   - Dynamic routing of execution requests (`execute_dynamo_code`, `execute_designscript`, `get_dynamo_context`).
2. Rollback & Safety Engine (`RevitTransactionScope.cs`):
   - Wrapping document execution inside `TransactionGroup("MCP Dynamo: <label>")`.
   - On `dryRun = true`, rolling back all changes cleanly via `TransactionGroup.RollBack()`.
   - On `dryRun = false`, assimilating or committing transactions.
3. Graph & Code Runners:
   - `HeadlessGraphRunner.cs`: Headless execution using `RevitDynamoModel`.
   - `CanvasGraphRunner.cs`: Live canvas execution using `DynamoViewModel` and `WorkspaceModel`.
   - DesignScript runner integration: creating temporary code blocks or evaluating via `DynamoEngine`.
4. Context Extractor (`DynamoContextExtractor.cs`):
   - Extracting active workspace info, nodes count, selection, and document details for `get_dynamo_context`.
5. Reference:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_2\handoff.md`
   - Clean Code limits (classes <= 300 lines, methods <= 50 lines).
## 2026-10-08T09:42:47Z
Investigate and design the execution coordination and rollback engine for HPDynamo.McpBridge:
1. DynamoBridgeCoordinator.cs routing between headless and canvas modes.
2. RevitTransactionScope.cs for TransactionGroup rollback on dryRun=true.
3. HeadlessGraphRunner.cs and CanvasGraphRunner.cs for graph/script execution.
4. Context extraction for get_dynamo_context.
5. Clean Code compliance (classes <= 300 lines, methods <= 50 lines).
Output your complete blueprint to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m3_2\handoff.md and send a message back to orchestrator_4.
