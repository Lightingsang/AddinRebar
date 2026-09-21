# BRIEFING — 2026-09-21T06:13:40Z

## Mission
Investigate standalone MCP bridges and server implementations in this repo (HPEtabs, HPSap2000, HPNavis, McpShared) to extract the complete architectural contract for the new Power BI MCP subsystem (HPPowerBi).

## 🔒 My Identity
- Archetype: teamwork_preview_spec_miner
- Roles: Specification Miner
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\spec_miner_pbi_1
- Original parent: 4d88b310-8910-4f85-b5a8-50216392bc6b
- Milestone: M1_spec_mining

## 🔒 Key Constraints
- READ-ONLY investigation: Do NOT modify or create any source code or test files outside .agents/spec_miner_pbi_1/.
- Probe authoritative specifications in McpShared, HPEtabs, HPSap2000, HPNavis.
- Provide comprehensive tables: Features Discovered and Edge Cases.
- Deliver report.md, handoff.md, progress.md.

## Current Parent
- Conversation ID: 4d88b310-8910-4f85-b5a8-50216392bc6b
- Updated: not yet

## Task Summary
- **What to build**: Specification report and architectural blueprint for HPPowerBi subsystem based on existing standalone MCP implementations (HPEtabs, HPSap2000) and McpShared contracts.
- **Success criteria**: Complete specification covering McpShared wire contracts/interfaces, standalone WPF bridge patterns, MaterialDesign theming, safety opt-ins, C# Roslyn scripting guard/compiler requirements, and exact requirements for HPPowerBi.
- **Interface contracts**: McpShared (HPRebar.Mcp.Contracts, HPRebar.McpBridge.Core, HPRebar.Mcp.Server.Core).
- **Code layout**: HPPowerBi/ (HPPowerBi.McpBridge, HPPowerBi.Mcp.Server, tests, .slnx).

## Key Decisions Made
- Investigating McpShared first, then HPEtabs/HPSap2000 standalone WPF bridge architecture, then compiling requirements for HPPowerBi.

## Artifact Index
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\spec_miner_pbi_1\report.md — Architectural specification and contract report
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\spec_miner_pbi_1\handoff.md — 5-component self-contained handoff
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\spec_miner_pbi_1\progress.md — Liveness and progress tracker
