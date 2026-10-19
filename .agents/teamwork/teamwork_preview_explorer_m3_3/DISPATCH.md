# Dispatch: Explorer M3.3 — UI, Opt-In & Project Scaffolding Explorer

**Agent**: `teamwork_preview_explorer_m3_3`
**Role**: UI, Opt-In & Project Scaffolding Explorer
**Milestone**: M3 (Dual-Host In-Process Bridge `HPDynamo.McpBridge`)
**Parent**: `orchestrator_4` (`b20bb4a5-dd75-4c48-9f4d-621e02412393`)

## Mission
Investigate and design the UI, opt-in mechanism, and project scaffolding for `HPDynamo.McpBridge`:
1. `HPDynamo.McpBridge.csproj`:
   - Target framework `net8.0-windows7.0` (matching Revit 2026 .NET 8 runtime).
   - Use WPF (`<UseWPF>true</UseWPF>`).
   - ProjectReference to `McpShared/HPRebar.McpBridge.Core` and `McpShared/HPRebar.Mcp.Contracts`.
   - Reference Revit 2026 API (`RevitAPI.dll`, `RevitAPIUI.dll`) and Dynamo 3.7 API (`DynamoCore.dll`, `DynamoServices.dll`, `RevitNodes.dll`, `DynamoCoreWpf.dll`, `DynamoRevitDS.dll`).
   - MANDATORY: Apply `<Private>false</Private>` on all host API references to prevent bin/output pollution.
2. Modeless WPF Status Window:
   - `DynamoBridgeStatusView.xaml` using `MaterialDesignThemes 5.3.2`.
   - `MaterialThemeBridge.cs` syncing with Revit/Dynamo theme.
   - Displays connection status, active pipe, execution count, and last executed script.
3. Mandatory Opt-In Toggle:
   - Toggle: "Allow AI Dynamo execution" (default: OFF / false).
   - Wire into `McpBridgeHost` / `IBridgeExecutor` so that any write/destructive execution returns error code `-32001` when toggle is OFF.
4. Reference sister bridges:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_survey_3\handoff.md`
   - Reviewer 2 Finding 1 from Milestone M2 regarding `<Private>false</Private>`.
Output full blueprint to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m3_3\handoff.md`.


## 2026-10-08T09:42:47Z
Investigate and design the UI, opt-in mechanism, and project scaffolding for HPDynamo.McpBridge:
1. HPDynamo.McpBridge.csproj (net8.0-windows7.0, WPF, McpShared refs, Revit/Dynamo refs with <Private>false</Private>).
2. Modeless WPF Status View (DynamoBridgeStatusView.xaml, MaterialDesignThemes 5.3.2, MaterialThemeBridge.cs).
3. Mandatory Opt-In toggle: "Allow AI Dynamo execution" (default: false / OFF; code -32001 when disabled).
4. Clean Code compliance (classes <= 300 lines, methods <= 50 lines).
Output your complete blueprint to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m3_3\handoff.md and send a message back to orchestrator_4.
