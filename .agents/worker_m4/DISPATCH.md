## 2026-09-21T07:49:52Z

Implement Milestone 4: End-to-End Test Suite Verification, Skill Documentation (.agents/skills/hp-mcp-powerbi/SKILL.md), and Repository Registration (AGENTS.md).

Tasks:
1. Create Skill Documentation:
   File: .agents/skills/hp-mcp-powerbi/SKILL.md
   Model after .agents/skills/hp-mcp-sap2000/SKILL.md and .agents/skills/hp-mcp-etabs/SKILL.md:
   - YAML frontmatter with `name: hp-mcp-powerbi`, author: hoang, version: 1.0.0, mcp-server: hprebar-powerbi, description with triggers (Power BI, PBIDesktop, DAX, AMO-TOM, ADOMD, measure, relationship, TMSL, Power BI Service, workspaces, datasets, refresh, hppowerbi-mcp-2026, etc.).
   - Portable host contract comments.
   - Comprehensive documentation covering:
     * Overview & Architecture: Claude -> HPPowerBi.Mcp.Server (stdio, .NET 10) -> named pipe `hppowerbi-mcp-2026` -> HPPowerBi.McpBridge.exe (WPF, .NET 8) -> local Analysis Services (TOM/ADOMD) & Power BI Service Cloud REST API (MSAL).
     * Connection checklist: opening PBIDesktop, starting Bridge, opt-in toggles.
     * Full 12 tools catalog: 8 local tools (get_powerbi_context, execute_powerbi_code, powerbi_get_schema, powerbi_evaluate_dax, powerbi_create_or_update_measure, powerbi_delete_measure, powerbi_manage_relationship, powerbi_format_dax) + 4 cloud tools (powerbi_cloud_list_workspaces, powerbi_cloud_list_datasets, powerbi_cloud_trigger_refresh, powerbi_cloud_execute_dax).
     * 3-Layer Safety: Dual opt-in UI toggles, DAX query validation, automatic pre-mutation TMSL JSON snapshots with rollback.
     * Resources (`powerbi://schema`, `powerbi://document/info`) and prompts (`powerbi_dax_optimize`).
     * External Tools integration: HPPowerBi.pbitool.json.
     * Troubleshooting and error codes.

2. Register HPPowerBi in AGENTS.md:
   - In the Repository Layout table, add `HPPowerBi/` as the 7th deliverable:
     | `HPPowerBi/` | The **Power BI MCP** (standalone WPF bridge `HPPowerBi.McpBridge` net8.0-windows + stdio server `HPPowerBi.Mcp.Server` net10; connects to local PBIDesktop Analysis Services via AMO-TOM / ADOMD.NET and Power BI Service Cloud REST API via MSAL; 3-layer safety with TMSL snapshots, MaterialDesign 5.3.2 UI, External Tools auto-registration; pipe `hppowerbi-mcp-2026`). Own `HPPowerBi.slnx` + `global.json` + `Directory.Build.props`; references `../McpShared/` only. Tests `HPPowerBi.McpBridge.Tests` (203) + `HPPowerBi.Mcp.Server.Tests` (96). | C# / net8.0-windows · net10 / Microsoft.AnalysisServices (AMO-TOM / ADOMD) + Microsoft.Identity.Client |
   - Add a dedicated section `## HPPowerBi — Build, Run, Debug` (similar to HPEtabs and HPSap2000 sections) detailing build commands, test commands, architecture notes, safety model, and tool catalog.

3. Complete Build & Verification:
   Run and verify:
   - `dotnet build HPPowerBi/HPPowerBi.slnx -c Debug` (MUST be 0 errors, 0 warnings)
   - `dotnet run --project HPPowerBi/HPPowerBi.Mcp.Server.Tests/HPPowerBi.Mcp.Server.Tests.csproj` (100% pass)
   - `dotnet run --project HPPowerBi/HPPowerBi.McpBridge.Tests/HPPowerBi.McpBridge.Tests.csproj` (100% pass)
   - `dotnet run --project McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj` (100% pass, zero regression)
   - `dotnet run --project McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj` (100% pass)
   - `dotnet test HPRebar/HPRebar.Core.Tests/HPRebar.Core.Tests.csproj` (100% pass)
   - `dotnet test HPRebar/HPRebar.Mcp.Server.Tests/HPRebar.Mcp.Server.Tests.csproj` (100% pass)
