# BRIEFING — 2026-10-08T11:00:00Z

## Mission
Design and blueprint the exact metadata (tool.json, examples.json) and C# scripts (code.cs) for Seeds 1 through 6 of the Dynamo MCP Seed Library (run_dyn_file, get_active_graph_info, search_nodes_library, inspect_node_type, add_node_to_workspace, connect_node_ports) adhering strictly to Clean Code governance and Dynamo/Revit 2026 APIs.

## 🔒 My Identity
- Archetype: Teamwork explorer
- Roles: M4 Seeds 1-6 Specification Explorer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m4_2\
- Original parent: b20bb4a5-dd75-4c48-9f4d-621e02412393
- Milestone: M4 - Embedded Seed Library (12 Seeds)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement directly in production src
- Classes <= 300 lines (Rule C2)
- Methods <= 50 lines (Rule M3)
- 0 empty catch blocks (Rule Q-B1)
- 0 commented-out code (Rule Q-B2)
- No vague names ("data", "tmp", "temp", "obj", "res", "val", "foo", "bar", "stuff", "thing")
- Block nesting <= 3 levels
- All scripts top-level Roslyn scripts targeting Dynamo 3.7 / Revit 2026 APIs
- Clean Code Governance and ADR-0007 compliance

## Current Parent
- Conversation ID: b20bb4a5-dd75-4c48-9f4d-621e02412393
- Updated: 2026-10-08T11:00:00Z

## Investigation State
- **Explored paths**: HPDynamo codebase, McpShared Dynamo profiles and contracts, E2E test suites (Tier 1-4), DynamoCore 3.7 assemblies in Revit 2026.
- **Key findings**: Complete API alignment for dual-lifecycle execution (canvas DynamoViewModel vs headless RevitDynamoModel), node creation commands, connection commands, search model entries, and node inspection.
- **Unexplored areas**: None for Seeds 1 through 6. All specifications, schemas, and implementations are fully drafted and verified against Clean Code rules.

## Key Decisions Made
- All 6 seeds structured as Category/seed_name/{tool.json, code.cs, examples.json}.
- Strict compliance with ScriptQuality: 0 vague names, lines <= 100 per script, blocks <= 35 lines, nesting <= 3, 0 empty catches, 0 commented-out code.
- Boundary conditions tested in E2E harness (IndexOutOfRangeException for negative/out-of-bounds port indices, ArgumentException for NaN/infinity coordinates, InvalidOperationException for cyclic self-loops) directly implemented in scripts.

## Artifact Index
- handoff.md — Blueprint and specification report for Seeds 1-6
