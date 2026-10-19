# DISPATCH — 2026-10-08T10:45:00Z

## Assigned Subagent
- Identity: teamwork_preview_explorer_m4_1
- Role: M4 Seed Architecture & Pattern Explorer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m4_1\

## Mission
Investigate sibling MCP servers (such as HPEtabs, HPAutoCad, HPNavis, HPRebar) to determine the exact pattern and plumbing for the Embedded Seed Library in HPDynamo.Mcp.Server:
1. Seed directory structure and embedding: How seeds are organized under `Registry/SeedLibrary/` or similar, how `.csproj` configures `<EmbeddedResource>`, resource naming conventions.
2. Metadata & Script pairing: Inspect how JSON schema/metadata files and `.cs` scripts are formatted, how `DynamicToolRegistrar` or `McpServerHost` discovers and registers them.
3. 24-tool surface validation: How the 12 seeds are registered as published seeds alongside the 4 core tools and 8 registry meta tools to reach exactly 24 tools.
4. Clean Code requirements: Each seed class/script <= 300 lines (Rule C2), 0 empty catch blocks, 0 commented-out code.

## References
- Authoritative Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- Project Spec: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md
- Sibling examples:
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPEtabs\HPEtabs.Mcp.Server\`
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPAutoCad\HPAutoCad.Mcp.Server\`
  - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPNavis\HPNavis.Mcp.Server\`

## Output
Write your comprehensive architectural report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m4_1\handoff.md` and send a message back to orchestrator_4.
