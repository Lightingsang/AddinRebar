# DISPATCH — teamwork_preview_explorer_m5_2

## Task Assignment
**Role**: Bridge Tests Architecture Explorer (teamwork_preview_explorer)
**Mission**: Investigate sibling bridge test suites (`HPNavis.McpBridge.Tests`, `HPEtabs.McpBridge.Tests`, `HPAutoCad.Aec.Tests`) and design the full test architecture for `HPDynamo.McpBridge.Tests`.

## Required Reading
1. `ORIGINAL_REQUEST.md`: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md`
2. Project Specifications: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md`
3. Sibling projects:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPNavis\HPNavis.McpBridge.Tests\`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPEtabs\HPEtabs.McpBridge.Tests\`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge\`

## Scope of Investigation
1. **Test Project Configuration**: Target framework (`net10.0-windows7.0`), WPF support (`<UseWPF>true</UseWPF>`), dependencies on `McpShared`, zero host API assembly copy (`<Private>false</Private>`), reference to `HPDynamo.McpBridge`.
2. **Bridge Isolation & Headless Mocking**:
   - How tests can run headlessly without Revit.exe or Dynamo.exe running.
   - Use of mock scopes, simulated Revit transactions, and fake view extensions.
3. **Test Suites to Design**:
   - `CoordinatorTests`: `DynamoBridgeCoordinator` routing between canvas mode (`DynamoViewModel` present) and headless mode (`RevitDynamoModel` / simulated engine). Verify `ContextResult.Dynamo` structure and properties.
   - `TransactionScopeTests`: `RevitTransactionScope` behavior on `dryRun=true` (TransactionGroup rollback) vs `dryRun=false` (commit/assimilate). Verify exception handling and rollback guarantees.
   - `OptInSecurityTests`: Verify that when `IsExecutionEnabled` is false, execution requests are immediately rejected with error code `-32001` and descriptive error message.
   - `DesignScriptRunnerTests`: Verify DesignScript argument parsing, AST statement validation, variable binding, and error extraction.
   - `ViewModelBindingTests`: Verify `DynamoStatusViewModel` state transitions, theme syncing, toggle switches, and connection counters.
4. **Clean Code Rules**: Files <= 300 lines, methods <= 50 lines.

## Deliverable
Write your comprehensive architectural report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m5_2\handoff.md`
When complete, send a message back to orchestrator_4.


## 2026-10-08T11:44:48Z
[Message] timestamp=2026-10-08T11:44:48Z sender=b20bb4a5-dd75-4c48-9f4d-621e02412393 priority=MESSAGE_PRIORITY_HIGH content=You are teamwork_preview_explorer_m5_2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m5_2\
Read your dispatch instructions in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m5_2\DISPATCH.md
Read the authoritative request in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read the project specifications in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md

Investigate sibling bridge test projects (e.g., HPNavis.McpBridge.Tests, HPEtabs.McpBridge.Tests) to design the complete test architecture for HPDynamo.McpBridge.Tests:
1. Target framework (net10.0-windows7.0), UseWPF=true, references (HPDynamo.McpBridge, McpShared), <Private>false</Private> on any host references.
2. Headless execution architecture: How to test coordinator routing, transaction rollback, opt-in gate, and status view model without requiring a live Revit or Dynamo process.
3. Test suite design:
   - CoordinatorTests: routing between canvas and headless, ContextResult.Dynamo structure.
   - TransactionScopeTests: RevitTransactionScope rollback on dryRun=true, assimilation on dryRun=false, exception rollback.
   - OptInSecurityTests: code -32001 fast rejection when ExecutionEnabled=false.
   - DesignScriptRunnerTests: parsing, parameter binding, statement execution.
   - ViewModelBindingTests: DynamoStatusViewModel toggle switches, theme sync, counters.
4. Clean Code compliance for test classes and methods.
Write your comprehensive architectural blueprint to:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m5_2\handoff.md
When finished, send a message back to orchestrator_4.
