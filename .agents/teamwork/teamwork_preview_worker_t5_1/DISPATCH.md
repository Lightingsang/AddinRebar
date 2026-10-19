# Dispatch Instructions: teamwork_preview_worker_t5_1

## Identity
- **Agent**: `teamwork_preview_worker_t5_1`
- **Role**: Tier 5 Remediation & Hardening Worker
- **Orchestrator**: `orchestrator_4` (`b20bb4a5-dd75-4c48-9f4d-621e02412393`)
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_t5_1\`

## Context & Objectives
You are dispatched to implement the 4 production hardening improvements identified during Tier 5 adversarial analysis by `challenger_t5_1` (see `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_challenger_t5_1\handoff.md`), while ensuring all Tier 5 tests authored by `challenger_t5_2` pass 100% green.

## Mandatory Remediation Tasks

1. **RevitTransactionScope Cancellation Fix**:
   File: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge\Execution\RevitTransactionScope.cs`
   - In `ExecuteCore`, replace:
     ```csharp
     if (timeoutSource.IsCancellationRequested)
         throw new OperationCanceledException(timeoutSource.Token);
     ```
     with:
     ```csharp
     if (linked.Token.IsCancellationRequested)
         throw new OperationCanceledException(linked.Token);
     ```
     This ensures pre-action and post-action user cancellations via `cancel_execution` immediately abort before `FinalizeTransactions` commits.

2. **RevitTransactionScope Rollback Cascade Fix**:
   File: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge\Execution\RevitTransactionScope.cs`
   - In `SafeRollback`, split auto-transaction rollback and group rollback into separate defensive try/catch blocks:
     ```csharp
     private static bool SafeRollback(ITransactionContext txContext)
     {
         var autoSuccess = false;
         var groupSuccess = false;
         try
         {
             txContext.RollbackAutoTransaction();
             autoSuccess = true;
         }
         catch (Exception ex)
         {
             Log.Error(ex, "Exception encountered rolling back auto transaction");
         }

         try
         {
             txContext.RollbackGroup();
             groupSuccess = true;
         }
         catch (Exception ex)
         {
             Log.Error(ex, "Exception encountered rolling back transaction group");
         }

         return autoSuccess && groupSuccess;
     }
     ```

3. **connect_node_ports Cyclic Self-Connection Post-Resolution Check**:
   File: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\Registry\SeedLibrary\Wire\connect_node_ports\code.cs`
   - After resolving `upstreamSourceNode` and `downstreamTargetNode`, add:
     ```csharp
     if (upstreamSourceNode.GUID == downstreamTargetNode.GUID)
         throw new InvalidOperationException("Cyclic self-connection is not permitted: source and target node cannot be the same.");
     ```

4. **set_node_properties Partial Coordinate Update & NaN Validation**:
   File: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\Registry\SeedLibrary\Node\set_node_properties\code.cs`
   - Allow single-axis coordinate updates when either `x` or `y` is provided:
     ```csharp
     if (targetX.HasValue || targetY.HasValue)
     {
         if ((targetX.HasValue && (double.IsNaN(targetX.Value) || double.IsInfinity(targetX.Value))) ||
             (targetY.HasValue && (double.IsNaN(targetY.Value) || double.IsInfinity(targetY.Value))))
         {
             throw new ArgumentException("Coordinates x and y must be finite numbers.");
         }

         if (targetX.HasValue) targetNodeModel.X = targetX.Value;
         if (targetY.HasValue) targetNodeModel.Y = targetY.Value;
         targetNodeModel.ReportPosition();
         updatedPropertiesList.Add("position");
     }
     ```

5. **Clean Code Governance**:
   - Files $\le 300$ lines (Rule C2).
   - Methods $\le 50$ lines (Rule M3).
   - 0 unannotated empty catch blocks (Rule Q-B1).
   - 0 commented-out code (Rule Q-B2).
   - 0 host DLLs copied to test output bin directories (`<Private>false</Private>`).
   - Do NOT pass `--nologo` to MTP runners!

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Verification Commands
Execute and confirm 0 errors/warnings and 100% pass:
1. `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.slnx" -c Debug`
2. `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge.Tests\HPDynamo.McpBridge.Tests.csproj"`
3. `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server.Tests\HPDynamo.Mcp.Server.Tests.csproj"`
4. `powershell -ExecutionPolicy Bypass -File "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\run-e2e-dynamo-tests.ps1" -Tier All`
5. `powershell -ExecutionPolicy Bypass -File "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\run-e2e-dynamo-tests.ps1" -Tier Tier5`
6. McpShared regressions:
   `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj"`
   `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.McpBridge.Core.Net48Tests\HPRebar.McpBridge.Core.Net48Tests.csproj"`

## Output
Write your handoff report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_t5_1\handoff.md`
Then send a completion message back to `orchestrator_4`.


## 2026-10-08T15:27:43Z

[Message] timestamp=2026-10-08T15:27:43Z sender=b20bb4a5-dd75-4c48-9f4d-621e02412393 priority=MESSAGE_PRIORITY_HIGH content=You are teamwork_preview_worker_t5_1.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_t5_1\
Read your dispatch instructions in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_t5_1\DISPATCH.md
Read the authoritative request in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read the project specifications in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md
Read the Challenger 1 handoff report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_challenger_t5_1\handoff.md
Read the Challenger 2 handoff report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_challenger_t5_2\handoff.md

Implement the 4 production hardening improvements identified during Tier 5 adversarial analysis:
1. In RevitTransactionScope.cs: In ExecuteCore, use linked.Token.IsCancellationRequested instead of only timeoutSource.IsCancellationRequested so user cancellations via cancel_execution properly abort before FinalizeTransactions commits.
2. In RevitTransactionScope.cs: In SafeRollback, separate RollbackAutoTransaction and RollbackGroup into two independent try/catch blocks so auto-transaction failure does not orphan the Revit transaction group.
3. In connect_node_ports/code.cs: Add a post-resolution check if (upstreamSourceNode.GUID == downstreamTargetNode.GUID) to prevent cyclic self-connection when one parameter is passed as GUID and the other as Name.
4. In set_node_properties/code.cs: Allow single-axis coordinate updates (targetX.HasValue || targetY.HasValue), enforce finite number validation, and update each provided coordinate axis cleanly.
5. Clean Code governance: files <= 300 lines (Rule C2), methods <= 50 lines (Rule M3), 0 unannotated empty catch blocks (Rule Q-B1), 0 commented-out code (Rule Q-B2).

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Run verification commands:
dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.slnx" -c Debug
dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge.Tests\HPDynamo.McpBridge.Tests.csproj"
dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server.Tests\HPDynamo.Mcp.Server.Tests.csproj"
powershell -ExecutionPolicy Bypass -File "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\run-e2e-dynamo-tests.ps1" -Tier All
powershell -ExecutionPolicy Bypass -File "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\run-e2e-dynamo-tests.ps1" -Tier Tier5
dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj"
(Do NOT pass --nologo)
Verify all tests pass 100% green with 0 errors and 0 warnings.
Write your complete handoff report to:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_t5_1\handoff.md
When finished, send a message back to orchestrator_4.
