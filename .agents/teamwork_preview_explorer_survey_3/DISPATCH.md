# Dispatch for Survey Explorer 3: Server, Tool Catalog & Seed Implementations

## 2026-09-21T17:22:00Z

- Working Directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_3
- Authoritative Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\ORIGINAL_REQUEST.md (read header ## 2026-09-21T17:20:33Z)
- Repo Root: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar

Objective:
Investigate HPTekla.Mcp.Server architecture, 24 tools catalog, and test suites:
1. Examine existing MCP servers: `HPRobot.Mcp.Server`, `HPNavis.Mcp.Server`, `HPAutoCad.Mcp.Server`, `HPExcel.Mcp.Server`.
2. Detail the exact specification and implementation design for the 24 tools:
   - 4 Core Tools: `execute_tekla_code`, `get_tekla_context`, `inspect_type`, `cancel_execution`.
   - 8 Registry Meta Tools: `search_tools`, `get_tool_schema`, `propose_tool`, `test_tool`, `publish_tool`, `deprecate_tool`, `rollback_tool`, `list_dynamic_tools`.
   - 12 Embedded Seed Tools:
     1. `get_model_info`
     2. `select_objects`
     3. `get_part_properties`
     4. `create_beam`
     5. `create_column`
     6. `create_contour_plate`
     7. `create_rebar_group`
     8. `create_single_rebar`
     9. `modify_user_properties`
     10. `get_reinforcement_info`
     11. `list_drawings`
     12. `export_ifc`
3. Design test strategy for `HPTekla.Mcp.Server.Tests` (net10) and `HPTekla.McpBridge.Tests` (net48), as well as Python live verification harness in `HPTekla/tools/harness/`.
4. Output report to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_3\report.md and handoff.md.
