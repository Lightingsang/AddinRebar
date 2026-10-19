# DISPATCH — 2026-10-08T10:58:00Z

## Assigned Subagent
- Identity: teamwork_preview_worker_m4_1
- Role: M4 Seed Library Implementation Worker
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m4_1\

## Mission
Implement the complete Embedded Seed Library (12 Seeds) in `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/`:

1. Ensure `.csproj` configuration in `HPDynamo/HPDynamo.Mcp.Server/HPDynamo.Mcp.Server.csproj`:
   ```xml
   <Compile Remove="Registry\SeedLibrary\**\*.cs" />
   <EmbeddedResource Include="Registry\SeedLibrary\**\*" LogicalName="SeedLibrary/%(RecursiveDir)%(Filename)%(Extension)" />
   ```

2. Create all 12 seed tool folders, each containing the exact 3-file triad (`tool.json`, `code.cs`, `examples.json`):
   - `Graph/run_dyn_file/`
   - `Workspace/get_active_graph_info/`
   - `Library/search_nodes_library/`
   - `Library/inspect_node_type/`
   - `Node/add_node_to_workspace/`
   - `Wire/connect_node_ports/`
   - `Wire/disconnect_node_ports/`
   - `Node/set_node_properties/`
   - `Data/get_node_output_values/`
   - `Export/export_graph_to_image/`
   - `Graph/validate_dyn_file/`
   - `CodeBlock/create_code_block_node/`

3. Follow the detailed blueprints authored by the M4 Explorers:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m4_1\handoff.md` (Architecture, folder layout, logical embedding naming)
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m4_2\handoff.md` (Seeds 1–6 complete tool.json, code.cs, examples.json)
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m4_3\handoff.md` (Seeds 7–12 complete tool.json, code.cs, examples.json)

4. Clean Code Governance & Argument Parity:
   - Classes/scripts <= 300 lines (Rule C2).
   - Methods/blocks <= 50 lines (Rule M3).
   - 0 empty catch blocks (Rule Q-B1).
   - 0 commented-out code (Rule Q-B2).
   - Strict argument parity: every parameter declared in `tool.json` must be accessed via `args.*` in `code.cs`, and vice versa.
   - Zero forbidden namespaces (`System.IO`, `System.Net`, `Process`, `Reflection`).

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Verification
Execute the following verification commands:
1. `dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.slnx"` (0 warnings, 0 errors)
2. `powershell -ExecutionPolicy Bypass -File "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\run-e2e-dynamo-tests.ps1" -Tier Tier1` (36/36 pass)
3. `powershell -ExecutionPolicy Bypass -File "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\run-e2e-dynamo-tests.ps1" -Tier All` (84/84 pass)
4. `dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj"` (899/899 pass)
(Do NOT pass --nologo)

Write your complete handoff report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_worker_m4_1\handoff.md` and send a message back to orchestrator_4.
