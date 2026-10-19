# DISPATCH — 2026-10-08T10:45:00Z

## Assigned Subagent
- Identity: teamwork_preview_explorer_m4_3
- Role: M4 Seeds 7-12 Specification Explorer
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m4_3\

## Mission
Design and blueprint the exact metadata (JSON) and C# scripts for Seeds 7 through 12 of the Dynamo MCP Seed Library:
7. `disconnect_node_ports`: Sever a wire/connector between specified node ports or disconnect all wires on a specific port.
8. `set_node_properties`: Update node parameters, name/caption, or custom inputs (e.g., number slider value, string input text).
9. `get_node_output_values`: Retrieve the computed execution values and mirror data cached on specified node output ports after graph run.
10. `export_graph_to_image`: Capture an image/screenshot of the active Dynamo canvas workspace or export graph layout to PNG/JPEG.
11. `validate_dyn_file`: Validate the JSON structure and node schema of a `.dyn` file on disk without executing it (missing packages, node version mismatches, syntax errors).
12. `create_code_block_node`: Create a DesignScript Code Block node with specified code string, position (x, y), and parse/validate syntax.

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
Write your comprehensive design blueprint and draft scripts to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m4_3\handoff.md` and send a message back to orchestrator_4.
