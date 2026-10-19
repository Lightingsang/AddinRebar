# Dispatch Instructions: teamwork_preview_auditor_final_1

## Identity
- **Agent**: `teamwork_preview_auditor_final_1`
- **Role**: Final Milestone Forensic Integrity Auditor
- **Orchestrator**: `orchestrator_4` (`b20bb4a5-dd75-4c48-9f4d-621e02412393`)
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_auditor_final_1\`

## Objectives & Audit Scope
Conduct the comprehensive Final Forensic Integrity Audit on the entire HPDynamo MCP Subsystem across all milestones (M1–M5 + Final Tier 5 Hardening):
1. **Authenticity & Integrity Verification**:
   - Inspect production code across `HPDynamo.Mcp.Server/` and `HPDynamo.McpBridge/`.
   - Inspect all 12 embedded seeds in `HPDynamo.Mcp.Server/Registry/SeedLibrary/`.
   - Inspect all test suites in `HPDynamo.Mcp.Server.Tests/`, `HPDynamo.McpBridge.Tests/`, and `HPDynamo.E2E.Tests/`.
   - Confirm 0 hardcoded test results, 0 dummy/facade implementations, 0 self-certifying tests, 0 fabricated logs or artifacts.
2. **Clean Code Governance Audit**:
   - Every C# class/script $\le 300$ lines (Rule C2).
   - Every method $\le 50$ lines (or justified linear method like `ExecuteCore` $\le 100$ lines) (Rule M3).
   - 0 unannotated empty catch blocks (Rule Q-B1).
   - 0 commented-out code (Rule Q-B2).
   - 0 forbidden namespaces (`System.IO`, `System.Net`, `System.Diagnostics.Process`, `System.Reflection`).
3. **Tool Surface & Host Isolation**:
   - Verify that stdio server exposes exactly 24 tools on `tools/list` (`inspect_type` suppressed).
   - Verify 0 host DLLs (`RevitAPI*.dll`, `Dynamo*.dll`) in test bin output directories (`<Private>false</Private>`).
4. **Live Verification Commands (Do NOT pass `--nologo`)**:
   - `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.slnx" -c Debug`
   - `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.McpBridge.Tests\HPDynamo.McpBridge.Tests.csproj"`
   - `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server.Tests\HPDynamo.Mcp.Server.Tests.csproj"`
   - `powershell -ExecutionPolicy Bypass -File "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\run-e2e-dynamo-tests.ps1" -Tier All`
   - `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj"`
   - `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.McpBridge.Core.Net48Tests\HPRebar.McpBridge.Core.Net48Tests.csproj"`
5. **Binary Verdict**:
   Issue an explicit binary audit verdict: **CLEAN** or **INTEGRITY VIOLATION**.

Write your handoff report to:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_auditor_final_1\handoff.md`
Then send a completion message to `orchestrator_4`.


## 2026-10-08T15:53:58Z
Received dispatch from orchestrator_4 (b20bb4a5-dd75-4c48-9f4d-621e02412393) to conduct Final Milestone Forensic Integrity Audit on HPDynamo MCP Subsystem.
