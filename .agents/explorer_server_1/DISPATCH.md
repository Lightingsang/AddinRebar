# Dispatch: explorer_server_1
Role: teamwork_preview_explorer
Target: Investigate HPPowerBi.Mcp.Server (.NET 10 Stdio), PowerBiHostProfile, core & cloud tools, dynamic registry, test mocking strategies, and documentation requirements.
Original Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md
Output: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_server_1\report.md

## 2026-09-21T06:13:02Z
Objective:
Investigate the technical specifications and implementation details for HPPowerBi.Mcp.Server (R2), Automated Tests (R3), and Skill/Docs (R4).

Tasks:
1. HPPowerBi.Mcp.Server (.NET 10 Stdio Console):
   - Setup of PowerBiHostProfile implementing IHostProfile from McpShared.
   - Specification and parameter contracts for Core Local Tools:
     * get_powerbi_context
     * execute_powerbi_code (Roslyn C# script with Tabular Model & AdomdConnection globals)
     * powerbi_get_schema
     * powerbi_evaluate_dax
     * powerbi_create_or_update_measure
     * powerbi_delete_measure
     * powerbi_manage_relationship
     * powerbi_format_dax (DAX formatter logic or offline formatting parser)
   - Specification and parameter contracts for Cloud REST Tools:
     * powerbi_cloud_list_workspaces
     * powerbi_cloud_list_datasets
     * powerbi_cloud_trigger_refresh
     * powerbi_cloud_execute_dax
   - Integration of Dynamic Tool Registry and meta tools from McpShared.
   - Power BI resources (e.g. powerbi://schema, powerbi://instances) and prompts.
2. Automated Test Strategy (R3):
   - HPPowerBi.McpBridge.Tests: Unit tests for port discovery parser, DAX serialization, snapshot manager, safety gating, external tool JSON generator.
   - HPPowerBi.Mcp.Server.Tests: Unit tests for PowerBiHostProfile, tool catalog registration, tool execution routing via fake/mock pipe executor, argument validation.
   - How to test 100% reliably in CI/offline without requiring real PBIDesktop running or real Azure credentials.
3. Skill and Documentation (R4):
   - Structure and requirements for .agents/skills/hp-mcp-powerbi/SKILL.md following repository skill standards.
   - Registration entry in AGENTS.md for HPPowerBi deliverable.
