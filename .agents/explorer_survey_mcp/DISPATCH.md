## 2026-10-05T23:27:52Z
You are teamwork_preview_explorer assigned to survey the Revit MCP Named Pipe infrastructure and McpShared for HPBim.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_mcp

Read the authoritative request at: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (specifically under ## 2026-10-05T23:21:03Z).

Your mission is a thorough technical survey of:
1. McpShared/ (HPRebar.Mcp.Contracts, HPRebar.McpBridge.Core, HPRebar.Mcp.Server.Core):
   - Named Pipe communication protocol, client connection mechanisms, JSON-RPC envelope and methods.
   - How BridgeClient or pipe clients send requests to Revit MCP (pipe name \\.\pipe\hprebar-mcp-r2026).
2. HPRebar/ and Revit 2026 MCP execution:
   - Script execution engine (Roslyn scripting inside Revit process, global variables `doc`, `app`, `output`).
   - Transaction handling: Transaction vs TransactionGroup, dryRun rollback pattern (rolling back transaction group without dirtying document).
3. Revit API 2026 BIM Creation patterns needed for HPBim:
   - Trật tự dựng hình: Levels -> Grids -> Columns/Beams/Floors -> Walls -> Doors/Windows -> Rooms -> MEP.
   - Dynamic Family/Type resolution & duplication in Revit: How to detect missing types (e.g. Column 220x220, Wall 110mm, Door 900x2200), duplicate an existing symbol, and set dimension parameters (WIDTH/HEIGHT or CompoundStructure thickness) via Roslyn script.
   - Element verification: How to query back created elements (FilteredElementCollector) to verify element counts and properties.

Deliverables:
- Write your comprehensive survey report to g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_mcp\handoff.md.
- Update g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_mcp\progress.md.
- Send a message to parent with summary and file path.
