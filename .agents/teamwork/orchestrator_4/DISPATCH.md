# Dispatch: orchestrator_4 (Project Orchestrator)

## Mission
Build HPDynamo MCP (Model Context Protocol) subsystem for Dynamo in Revit 2026, enabling AI coding agents to inspect, author, and execute Dynamo graphs (.dyn) and DesignScript/C# code both headlessly and live on the active Revit canvas.

## Identity & Hierarchy
- Your identity: `orchestrator_4` (teamwork_preview_orchestrator).
- Your working directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\`.
- Your parent is: `60cd045e-fa41-429e-8f0d-4b61a2e4ed0f` (Use this conversation ID for all status reporting and communication via `send_message`).

## Reference Documents & Foundations
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (see timestamp `## 2026-10-08T08:01:51Z`)
- Clean Code Governance: `docs/clean-code/HP_CLEAN_CODE_CORE.md`, `docs/clean-code/host-appendix/revit.md`, `AGENTS.md`
- Existing Host Architecture Reference:
  - `McpShared/` (`HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`)
  - Sister in-process host bridges: `HPAutoCad/`, `HPCivil3d/`, `HPNavis/`, `HPTekla/`, and `HPRebar/HPRebar.McpBridge/`
  - Sister MCP servers: `HPRebar.Mcp.Server`, `HPAutoCad.Mcp.Server`, `HPEtabs.Mcp.Server`, `HPRobot.Mcp.Server`, etc.

## Requirements to Fulfill

### R1. McpShared Dynamo Profile & Pipe Contracts
Extend `McpShared` to define Dynamo host contracts without breaking existing hosts:
- Add `PipeNaming.DynamoHost = "hpdynamo-mcp-2026"`.
- Add `HostProfile.Dynamo` and Dynamo-specific `GuardProfile` / `AnalyzerProfile` with standard namespaces and safety rules.
- Maintain strict host-neutrality and wire contract compatibility in `McpShared/`.

### R2. HPDynamo Solution & MCP Server Executable (`HPDynamo.Mcp.Server`)
Create `HPDynamo/HPDynamo.slnx` and `HPDynamo.Mcp.Server` (.NET 10.0 console application, stdio transport):
- Implement `Program.cs` bootstrapping `McpServerHost.CreateBuilder(args, HostProfile.Dynamo)`.
- Implement Core Tools: `execute_dynamo_code`, `execute_designscript`, `get_dynamo_context`, `cancel_execution`.
- Wire 8 Registry Meta Tools (`search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`).
- Embed and auto-install the 12 embedded seed tools library.

### R3. Dual-Host In-Process Bridge (`HPDynamo.McpBridge`)
Create `HPDynamo.McpBridge` (.NET 8.0-windows7.0) running inside `Revit.exe`:
- **Dual-Host Lifecycle:**
  1. Revit Add-in (`IExternalApplication` + `.addin` manifest) that starts the Named Pipe listener `hpdynamo-mcp-2026` immediately upon Revit startup (allowing headless execution via `RevitDynamoModel` without Dynamo UI open).
  2. Dynamo ViewExtension (`IViewExtension` + XML manifest) that binds to the active `DynamoViewModel` / `WorkspaceModel` when Dynamo UI is opened, enabling live canvas node/wire authoring and inspection.
- **Dual-Level Safety & Rollback:**
  - Wrap graph executions modifying Revit documents in a Revit `TransactionGroup` ("MCP Dynamo: <label>"); rollback completely when `dryRun=true`.
  - Use Dynamo workspace snapshot / `UndoRecorder` for canvas-level changes.
- **ScriptGuard & Compiler:**
  - Roslyn compiler with `ScriptArgs`, denying forbidden namespaces (`System.IO`, `System.Net`, `Process`, unauthorized `Reflection`).
  - Direct DesignScript execution engine interacting with Dynamo geometry and Revit nodes.
- **Bridge UI:**
  - Modeless status window with MaterialDesignThemes 5.3.2, dark/light theme tracking, and mandatory "Allow AI Dynamo execution" opt-in toggle (default OFF).

### R4. Embedded Seed Library (12 Seeds)
Implement 12 verified seed tools under `HPDynamo.Mcp.Server/Registry/SeedLibrary/`:
1. `run_dyn_file`: Execute a `.dyn` file with input parameter dictionary overrides.
2. `get_active_graph_info`: Inspect active canvas workspace (nodes count, wires, run status, errors).
3. `search_nodes_library`: Search available node definitions in loaded Dynamo packages & core.
4. `inspect_node_type`: Get input/output ports, descriptions, and type signatures of a node.
5. `add_node_to_workspace`: Place a new node on canvas at coordinates (X, Y).
6. `connect_node_ports`: Wire an output port of Node A to an input port of Node B.
7. `disconnect_node_ports`: Disconnect wire between specified ports.
8. `set_node_properties`: Configure node attributes (title, slider range, boolean, text).
9. `get_node_output_values`: Retrieve evaluated output data from nodes after execution.
10. `export_graph_to_image`: Render canvas or graph to PNG image artifact.
11. `validate_dyn_file`: Inspect a `.dyn` JSON file for missing packages, cyclic wires, or errors.
12. `create_code_block_node`: Generate a DesignScript CodeBlock node with script content.

### R5. Test Suites & Quality Assurance
- `HPDynamo.Mcp.Server.Tests` (.NET 10.0, xUnit v3):
  - Registry lifecycle tests (draft -> tested -> pending_approval -> published).
  - Seed tool compilation tests against Dynamo and Revit reference assemblies.
  - ScriptGuard deny-list and AST analyzer tests.
- `HPDynamo.McpBridge.Tests` (.NET 8.0-windows7.0):
  - Dispatcher and named pipe protocol tests.
  - Headless graph parser and DesignScript runner tests.
- Compliance with `HP_CLEAN_CODE_CORE.md` (no forbidden namespaces, no commented-out code, structured exception logging).

## Acceptance Criteria
- [ ] `dotnet build HPDynamo/HPDynamo.slnx` succeeds with 0 errors and 0 warnings.
- [ ] `HPDynamo.Mcp.Server` publishes cleanly as a self-contained / win-x64 console exe.
- [ ] Deployment manifests (`HPDynamo.McpBridge.addin` and `HPDynamo_ViewExtensionDefinition.xml`) are correctly generated and positioned.
- [ ] Starting `HPDynamo.Mcp.Server.exe` and sending `tools/list` returns exactly 24 tools (4 Core + 8 Registry + 12 Seeds) with valid JSON Schema.
- [ ] Script proposals via `propose_tool` undergo AST validation and reject scripts exceeding 300 lines or containing forbidden namespaces.
- [ ] Named Pipe listener `hpdynamo-mcp-2026` handles requests with JSON-RPC 2.0 NDJSON.
- [ ] `dryRun=true` in `execute_dynamo_code` or `run_dyn_file` rolls back all created Revit elements via Revit `TransactionGroup`.
- [ ] Script execution is rejected when the bridge UI opt-in checkbox is OFF.
- [ ] All unit and integration tests in `HPDynamo.Mcp.Server.Tests` and `HPDynamo.McpBridge.Tests` pass.
- [ ] `McpShared` regression tests continue to pass without breaks (`HPRebar.Mcp.Server.Core.Tests`).

## Operational Protocol
1. Create `BRIEFING.md` and `progress.md` in your working directory immediately upon starting.
2. Establish a structured milestone plan (`plan.md`).
3. Coordinate specialists (Explorers, Workers, Reviewers, Challengers) adhering strictly to Clean Code standards.
4. Keep `progress.md` updated continuously so Sentinel crons track health and report to the user.
5. When complete, submit final handoff and completion notice to Sentinel via `send_message`.


## 2026-10-08T08:04:30Z
You are orchestrator_4, the Project Orchestrator for building the HPDynamo MCP subsystem in Revit 2026.
Your working directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\
Your parent Sentinel ID is: 60cd045e-fa41-429e-8f0d-4b61a2e4ed0f

Please read your complete dispatch specifications at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_4\DISPATCH.md
and the authoritative user request at:
g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (see ## 2026-10-08T08:01:51Z)

## 2026-10-08T14:32:29Z
Quota has reset. Please resume: complete Milestone M5 Gate 2 verification (or revive your gate verification subagents as needed), verify unanimous 5/5 consensus, and report completion back to Sentinel.

