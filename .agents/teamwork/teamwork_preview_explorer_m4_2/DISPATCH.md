# DISPATCH — 2026-10-08T10:45:00Z

## Assigned Subagent
- Identity: teamwork_preview_explorer_m4_2
- Role: M4 Seeds 1-6 Specification Explorer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m4_2\

## Mission
Design and blueprint the exact metadata (JSON) and C# scripts for Seeds 1 through 6 of the Dynamo MCP Seed Library:
1. `run_dyn_file`: Execute a `.dyn` graph file headlessly or on canvas, with input parameters dictionary and output node extraction.
2. `get_active_graph_info`: Inspect the currently open Dynamo graph (file path, nodes count, connectors count, custom nodes, execution mode, isModified).
3. `search_nodes_library`: Query available node classes/types in Dynamo's loaded library/package managers by keyword or category.
4. `inspect_node_type`: Detailed inspection of a specific node type (input ports, output ports, default values, assembly, description).
5. `add_node_to_workspace`: Place a new node into the active workspace at specified (x, y) coordinates.
6. `connect_node_ports`: Create a wire/connector between an output port of an upstream node and an input port of a downstream node.

## Requirements
- Each seed must have:
  - Tool name, description, parameters JSON schema (types, required, descriptions), returns format.
  - C# script implementation using Dynamo and Revit APIs in accordance with Clean Code governance:
    - Classes <= 300 lines (Rule C2).
    - Methods <= 50 lines (Rule M3).
    - 0 empty catch blocks (Rule Q-B1).
    - 0 commented-out code (Rule Q-B2).
    - Structured exception handling and logging.
- Reference Dynamo API assemblies in Revit 2026: `DynamoCore.dll`, `DynamoServices.dll`, `RevitNodes.dll`, `DynamoRevitDS.dll`.

## References
- Authoritative Request: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md
- Project Spec: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\PROJECT.md
- Sibling seed implementations in `HPEtabs/HPEtabs.Mcp.Server/Registry/SeedLibrary/` or `HPAutoCad/HPAutoCad.Mcp.Server/Registry/SeedLibrary/`.

## Output
Write your comprehensive design blueprint and draft scripts to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m4_2\handoff.md` and send a message back to orchestrator_4.
