# Dispatch Instructions: teamwork_preview_reviewer_t5_1

## Identity
- **Agent**: `teamwork_preview_reviewer_t5_1`
- **Role**: Tier 5 Code & Standards Reviewer
- **Orchestrator**: `orchestrator_4` (`b20bb4a5-dd75-4c48-9f4d-621e02412393`)
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_t5_1\`

## Objectives & Review Scope
Conduct an independent code quality, Clean Code compliance, and regression review for the Tier 5 Adversarial Hardening Phase of HPDynamo:
1. Review the changes made by `worker_t5_1`:
   - `HPDynamo/HPDynamo.McpBridge/Execution/RevitTransactionScope.cs` (cancellation check and isolated rollback cascade)
   - `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Wire/connect_node_ports/code.cs` (post-resolution self-connection check)
   - `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Node/set_node_properties/code.cs` (partial coordinates and finite number validation)
   - `HPDynamo/HPDynamo.McpBridge.Tests/Execution/TransactionScopeTests.cs` (new rollback and cancellation unit tests)
   - `HPDynamo/HPDynamo.E2E.Tests/Tiers/` (`Tier5_SecurityOptInAndDenyListTests.cs`, `Tier5_SeedParameterFuzzingTests.cs`, `Tier5_RoslynAstMutationTests.cs`)
2. Clean Code Rules Verification:
   - All classes $\le 300$ lines (Rule C2).
   - All methods $\le 50$ lines (Rule M3).
   - 0 unannotated empty catch blocks (Rule Q-B1).
   - 0 commented-out code (Rule Q-B2).
   - 0 host DLLs in test bins (`<Private>false</Private>`).
3. Run verification commands (Do NOT pass `--nologo`):
   - `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.slnx" -c Debug`
   - `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge.Tests\HPDynamo.McpBridge.Tests.csproj"`
   - `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server.Tests\HPDynamo.Mcp.Server.Tests.csproj"`
   - `powershell -ExecutionPolicy Bypass -File "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\run-e2e-dynamo-tests.ps1" -Tier All`
4. Issue an explicit verdict: **APPROVE** or **REQUEST_CHANGES**.

Write your handoff report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_t5_1\handoff.md`
Then send a completion message to `orchestrator_4`.


## 2026-10-08T15:39:57Z
You are teamwork_preview_reviewer_t5_1.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_t5_1\
Read your dispatch instructions in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_t5_1\DISPATCH.md
Read the authoritative request in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read the project specifications in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md
Read the worker handoff report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_t5_1\handoff.md

Review Tier 5 Hardening & Remediation:
1. Verify the 4 production hardening changes in RevitTransactionScope.cs, connect_node_ports/code.cs, set_node_properties/code.cs.
2. Verify Clean Code rules (classes <= 300 lines, methods <= 50 lines, 0 empty catch, 0 commented code).
3. Run verification commands (Do NOT pass --nologo):
   dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.slnx" -c Debug
   dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge.Tests\HPDynamo.McpBridge.Tests.csproj"
   dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server.Tests\HPDynamo.Mcp.Server.Tests.csproj"
   powershell -ExecutionPolicy Bypass -File "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\run-e2e-dynamo-tests.ps1" -Tier All
4. Deliver your handoff report with explicit verdict APPROVE or REQUEST_CHANGES to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_t5_1\handoff.md
When finished, send a message back to orchestrator_4.
