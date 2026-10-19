# Dispatch Instructions: teamwork_preview_reviewer_t5_2

## Identity
- **Agent**: `teamwork_preview_reviewer_t5_2`
- **Role**: Tier 5 Architecture & Security Reviewer
- **Orchestrator**: `orchestrator_4` (`b20bb4a5-dd75-4c48-9f4d-621e02412393`)
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_t5_2\`

## Objectives & Review Scope
Conduct an architectural, security boundary, and system-level isolation review for the Tier 5 Adversarial Hardening Phase of HPDynamo:
1. Verify Security & Boundary Isolation:
   - Opt-in gate verification (`-32001` fast rejection on `ExecutionEnabled = false`).
   - ScriptGuard AST defense verification (`dynamic`, `global::` namespace aliases, `#r`/`#load`, modal dialogs, unsafe blocks).
   - Cyclic wire prevention and coordinate boundary validation across the seed tools.
   - Elimination of test runner concurrency race conditions on static singleton `DynamoBridgeCoordinator.Instance`.
2. Verify 24-Tool Surface Invariant:
   - Exactly 24 tools exposed on stdio `tools/list` (4 Core + 8 Registry Meta + 12 Published Seeds; `inspect_type` suppressed).
   - Zero host DLLs leaking into output test bin directories (`<Private>false</Private>`).
3. Run verification tests (Do NOT pass `--nologo`):
   - `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.slnx" -c Debug`
   - `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge.Tests\HPDynamo.McpBridge.Tests.csproj"`
   - `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server.Tests\HPDynamo.Mcp.Server.Tests.csproj"`
   - `powershell -ExecutionPolicy Bypass -File "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\run-e2e-dynamo-tests.ps1" -Tier All`
   - `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj"`
4. Issue an explicit verdict: **APPROVE** or **REQUEST_CHANGES**.

Write your handoff report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_t5_2\handoff.md`
Then send a completion message to `orchestrator_4`.

## 2026-10-08T15:39:57Z
You are teamwork_preview_reviewer_t5_2.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_t5_2\
Read your dispatch instructions in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_t5_2\DISPATCH.md
Read the authoritative request in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
Read the project specifications in: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md
Read the worker handoff report: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_t5_1\handoff.md

Review Tier 5 Architecture & Security:
1. Verify security boundaries, opt-in gate, ScriptGuard AST defense, and 24-tool surface invariant.
2. Verify host assembly isolation: 0 host DLLs (RevitAPI*.dll, Dynamo*.dll) in test bins.
3. Run verification tests (Do NOT pass --nologo):
   dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.slnx" -c Debug
   dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge.Tests\HPDynamo.McpBridge.Tests.csproj"
   dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server.Tests\HPDynamo.Mcp.Server.Tests.csproj"
   powershell -ExecutionPolicy Bypass -File "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\run-e2e-dynamo-tests.ps1" -Tier All
   dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj"
4. Deliver your handoff report with explicit verdict APPROVE or REQUEST_CHANGES to:
   g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_reviewer_t5_2\handoff.md
When finished, send a message back to orchestrator_4.
