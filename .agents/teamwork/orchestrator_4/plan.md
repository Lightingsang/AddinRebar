# HPDynamo MCP Architectural Plan & Milestones

## Mission & Scope
Build the HPDynamo MCP subsystem in Revit 2026, enabling AI coding agents to inspect, author, and execute Dynamo graphs (.dyn) and DesignScript/C# code both headlessly and live on the active Revit canvas.

## Milestones Breakdown

### Phase 0: Survey & Discovery
- **Explorer 1 (McpShared)**: Survey `McpShared/` contracts, `HostProfile`, `PipeNaming`, `GuardProfile`, `AnalyzerProfile`, `HostScriptContracts`, and identify how new hosts (e.g. Navisworks, ETABS, AutoCAD) were plugged in without breaking existing suites.
- **Explorer 2 (Dynamo & Revit Runtime)**: Survey Revit 2026 installation for Dynamo assemblies (`DynamoCore.dll`, `DynamoServices.dll`, `DynamoRevitVersionSelector.dll`, `RevitNodes.dll`, `DynamoCoreWpf.dll`, `DesignScriptBuiltin.dll`), view extension interfaces (`IViewExtension`, `ViewStartupParams`, `ViewLoadedParams`), headless model (`RevitDynamoModel`), and manifest formats.
- **Explorer 3 (Sister Bridges & Seed Tool Patterns)**: Survey existing seed tools in `HPAutoCad.Mcp.Server`, `HPNavis.Mcp.Server`, `HPEtabs.Mcp.Server`, `HPRebar.Mcp.Server`, and bridge lifecycle patterns to ensure 100% adherence to clean code rules and tool registry structure.

### Milestone 1 (M1): McpShared Dynamo Profile & Pipe Contracts
- Add `PipeNaming.DynamoHost = "hpdynamo-mcp-2026"`.
- Add `HostProfile.Dynamo` and Dynamo-specific `GuardProfile` / `AnalyzerProfile` / `HostScriptContracts.DynamoImports` / `HostScriptContracts.DynamoGlobals`.
- Ensure zero breakage across all existing hosts; verify against `HPRebar.Mcp.Server.Core.Tests`.

### Milestone 2 (M2): HPDynamo Solution & MCP Server Executable (`HPDynamo.Mcp.Server`)
- Create `HPDynamo/HPDynamo.slnx`, `Directory.Build.props`, and `global.json`.
- Implement `HPDynamo.Mcp.Server` (.NET 10.0 console application, stdio transport).
- Bootstrap `McpServerHost.CreateBuilder(args, HostProfile.Dynamo)`.
- Implement 4 Core tools (`execute_dynamo_code`, `execute_designscript`, `get_dynamo_context`, `cancel_execution`).
- Wire 8 Registry Meta tools (`search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`).

### Milestone 3 (M3): Dual-Host In-Process Bridge (`HPDynamo.McpBridge`)
- Create `HPDynamo.McpBridge` (.NET 8.0-windows7.0) running inside `Revit.exe`.
- Implement dual lifecycle:
  1. Revit Add-in (`IExternalApplication` + `.addin` manifest) for immediate Named Pipe listener `hpdynamo-mcp-2026` upon Revit startup (allowing headless execution).
  2. Dynamo ViewExtension (`IViewExtension` + `HPDynamo_ViewExtensionDefinition.xml`) binding to `DynamoViewModel`/`WorkspaceModel` for live canvas interaction.
- Dual-level safety & rollback: Revit `TransactionGroup` ("MCP Dynamo: <label>") rollback on `dryRun=true`, and Dynamo workspace undo/snapshot.
- Roslyn compiler with `ScriptArgs` & `GuardProfile.Dynamo` + DesignScript execution runner.
- Modeless WPF status window with MaterialDesignThemes 5.3.2 and mandatory execution opt-in toggle.

### Milestone 4 (M4): Embedded Seed Library (12 Seeds)
- Implement and verify 12 seeds in `HPDynamo.Mcp.Server/Registry/SeedLibrary/`:
  1. `run_dyn_file`
  2. `get_active_graph_info`
  3. `search_nodes_library`
  4. `inspect_node_type`
  5. `add_node_to_workspace`
  6. `connect_node_ports`
  7. `disconnect_node_ports`
  8. `set_node_properties`
  9. `get_node_output_values`
  10. `export_graph_to_image`
  11. `validate_dyn_file`
  12. `create_code_block_node`

### Milestone 5 (M5): Test Suites & Final Gate Verification
- Implement `HPDynamo.Mcp.Server.Tests` (.NET 10.0 xUnit v3 / MTP): registry lifecycle, seed tool compilation checks, ScriptGuard tests.
- Implement `HPDynamo.McpBridge.Tests` (.NET 8.0-windows7.0): protocol, headless graph parser, and runner tests.
- Full verification: `dotnet build HPDynamo/HPDynamo.slnx`, `dotnet test`, `tools/list` returns 24 tools, regression tests pass 100%.
- Reviewers, Challengers, and Forensic Auditor verification.
