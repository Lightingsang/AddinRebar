# Project: HPDynamo MCP Subsystem in Revit 2026

## Architecture

HPDynamo is a full-fidelity Model Context Protocol (MCP) subsystem enabling AI coding agents to inspect, author, and execute Dynamo graphs (.dyn) and DesignScript/C# code both headlessly and live on the active Revit 2026 canvas.

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                                 AI Coding Agents                                 │
└────────────────────────────────────────┬─────────────────────────────────────────┘
                                         │ JSON-RPC 2.0 (stdio)
                                         ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│                 HPDynamo.Mcp.Server (.NET 10.0 Stdio Console Exe)                │
│  - 4 Core Tools: execute_dynamo_code, execute_designscript,                      │
│                  get_dynamo_context, cancel_execution                            │
│  - 8 Registry Meta Tools: search_tools, get_tool, run_tool, get_run,             │
│                           propose_tool, test_tool, publish_tool, manage_tool     │
│  - 12 Embedded Seeds (Published): run_dyn_file, get_active_graph_info, etc.      │
│  - Exactly 24 tools exposed on tools/list (inspect_type omitted)                 │
└────────────────────────────────────────┬─────────────────────────────────────────┘
                                         │ Named Pipe: \\.\pipe\hpdynamo-mcp-2026
                                         ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│               HPDynamo.McpBridge (.NET 8.0-windows7.0 In-Process)                │
│  - Dual-Host Lifecycle:                                                          │
│    • Revit Add-in (IExternalApplication, .addin) -> Pipe listener at RVT start   │
│    • Dynamo ViewExtension (IViewExtension, XML) -> Hooks live canvas/viewmodel  │
│  - Dual-Level Rollback:                                                          │
│    • Revit TransactionGroup("MCP Dynamo: <label>") -> full rollback on dryRun    │
│    • Dynamo UndoRedoRecorder / WorkspaceModel snapshot                           │
│  - Roslyn ScriptGuard + DesignScript Execution Engine                            │
│  - Modeless WPF Status Window: MaterialDesignThemes 5.3.2 + Opt-in Toggle        │
└────────────────────────────────────────┬─────────────────────────────────────────┘
                                         │
                                         ▼
┌──────────────────────────────────────────────────────────────────────────────────┐
│                     Autodesk Revit 2026 & Dynamo 3.7 Runtime                     │
│  - Revit.exe (.NET 8) / DynamoCore 3.7.0 / RevitDynamoModel / RevitNodes        │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

## Feature Inventory

| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | F1 - McpShared Dynamo Contracts & Wire DTOs | `PipeNaming.DynamoHost = "dynamo"`, `JsonRpcMethods.DynamoPrefix = "dynamo."`, `HostScriptContracts.DynamoImports`, `ContextResult.Dynamo` + `DynamoInfo` | M1 | survey |
| 2 | F2 - McpShared Dynamo Profiles | `HostProfile.Dynamo`, `GuardProfile.Dynamo`, `AnalyzerProfile.Dynamo` | M1 | survey |
| 3 | F3 - McpShared Tests Suite | `DynamoProfileTests.cs`, `DynamoTestProfile.cs`, zero regressions across 815 tests | M1 | survey |
| 4 | F4 - HPDynamo Solution Scaffolding | `HPDynamo/HPDynamo.slnx`, `Directory.Build.props`, `global.json` referencing McpShared | M2 | survey |
| 5 | F5 - HPDynamo.Mcp.Server Core Tools | Stdio server exe with `execute_dynamo_code`, `execute_designscript`, `get_dynamo_context`, `cancel_execution` | M2 | survey |
| 6 | F6 - 8 Registry Meta Tools & 24 Tools Surface | Dynamic tool registry, meta tools, strict 24 tool count enforcement | M2 | survey |
| 7 | F7 - Dual Lifecycle In-Process Bridge | `HPDynamo.McpBridge` (`IExternalApplication` + `IViewExtension`, manifests) | M3 | survey |
| 8 | F8 - Dual-Level Safety Rollback Engine | Revit `TransactionGroup` rollback on `dryRun=true` + Dynamo UndoRecorder | M3 | survey |
| 9 | F9 - Roslyn ScriptGuard & DesignScript Runner | ScriptArgs, deny-list AST checks, DesignScript execution | M3 | survey |
| 10 | F10 - Modeless WPF Status Window & Opt-In | MaterialDesignThemes 5.3.2, theme sync, mandatory "Allow AI Dynamo execution" toggle | M3 | survey |
| 11 | F11 - Embedded Seeds Library (Part 1: Seeds 1-6) | `run_dyn_file`, `get_active_graph_info`, `search_nodes_library`, `inspect_node_type`, `add_node_to_workspace`, `connect_node_ports` | M4 | survey |
| 12 | F12 - Embedded Seeds Library (Part 2: Seeds 7-12) | `disconnect_node_ports`, `set_node_properties`, `get_node_output_values`, `export_graph_to_image`, `validate_dyn_file`, `create_code_block_node` | M4 | survey |
| 13 | F13 - HPDynamo.Mcp.Server.Tests | Registry lifecycle, seed tool compilation checks, ScriptGuard tests | M5 | survey |
| 14 | F14 - HPDynamo.McpBridge.Tests | Named pipe protocol, parser, and runner tests | M5 | survey |
| 15 | F15 - Dual Track E2E Opaque-Box Acceptance Suite | Tiers 1-4 opaque-box acceptance test suite covering 24 tools, dryRun, opt-in rejection | E2E | survey |
| 16 | F16 - Final Milestone & Hardening | Pass 100% E2E test suite + Tier 5 adversarial stress testing + Forensic Audit verification | Final | survey |

---

## Milestones

| # | Name | Scope | Dependencies | Status |
|---|------|-------|--------------|--------|
| E2E | E2E Testing Track | Requirement-driven opaque-box test suite (Tiers 1-4), `TEST_INFRA.md`, publish `TEST_READY.md` | none | DONE |
| M1 | McpShared Dynamo Profile & Pipe Contracts | Extend `McpShared` with Dynamo contracts, profiles, and unit tests; 100% green regression | none | DONE |
| M2 | HPDynamo Solution & MCP Server Executable | Solution setup, `HPDynamo.Mcp.Server` with 4 Core + 8 Registry tools, exactly 24 tools | M1 | DONE |
| M3 | Dual-Host In-Process Bridge | `HPDynamo.McpBridge` dual lifecycle, `TransactionGroup` rollback, ScriptGuard, WPF UI & opt-in | M1 | DONE |
| M4 | Embedded Seed Library (12 Seeds) | 12 embedded seeds conforming to Clean Code (<300 lines each, examples, schemas) | M2, M3 | DONE |
| M5 | Test Suites & Server Verification | `HPDynamo.Mcp.Server.Tests` (94 tests), `HPDynamo.McpBridge.Tests` (43 tests), build verification | M4 | DONE |
| Final | Final Acceptance & Adversarial Hardening | Pass 100% E2E tests (Tiers 1-4) + Tier 5 adversarial hardening + Forensic Audit | E2E, M5 | DONE |

---

## Interface Contracts

### 1. Wire Protocol & Named Pipe
- Pipe Name: `\\.\pipe\hpdynamo-mcp-2026` (`PipeNaming.For("dynamo", 2026)`).
- Envelope: NDJSON JSON-RPC 2.0 messages (`JsonRpcRequest`, `JsonRpcResponse`).
- Method Prefix: `dynamo.` (`dynamo.execute`, `dynamo.context`, `dynamo.ping`, `dynamo.cancel`).

### 2. Dual-Lifecycle Bridge Interfaces
- `IExternalApplication.OnStartup`: Starts `McpBridgeHost` listening on named pipe. Installs `AssemblyResolve` hook for `DynamoForRevit`.
- `IViewExtension.Loaded(ViewLoadedParams)`: Captures `DynamoViewModel`, `WorkspaceModel`, and binds to `DynamoBridgeCoordinator`.
- `DynamoBridgeCoordinator`: Routes execution to live canvas if `DynamoViewModel` is available, or `RevitDynamoModel` if headless.

### 3. Safety & Rollback Contract
- `RevitTransactionScope`: Wraps execution in `TransactionGroup("MCP Dynamo: <label>")`. On `dryRun=true`, calls `RollBack()`.
- Opt-In Check: If `IsExecutionEnabled == false`, dispatcher rejects execution with code `-32001`.

---

## Code Layout

```
HPDynamo/
├── HPDynamo.slnx                                         (Solution format)
├── Directory.Build.props                                 (Pins net8.0-windows / net10.0, Revit 2026 Dynamo paths)
├── global.json                                           (Pins .NET SDK 10.0.300 + MTP test runner)
│
├── HPDynamo.Mcp.Server/                                  (net10.0, Stdio MCP Server Exe)
│   ├── Program.cs                                        (McpServerHost bootstrap with HostProfile.Dynamo)
│   ├── Hosts/
│   │   └── DynamoHostProfile.cs                          (Profile override, CoreToolNames, 24 tools filter)
│   ├── Tools/
│   │   ├── ExecuteDynamoCodeTool.cs                      (Core: execute_dynamo_code)
│   │   ├── ExecuteDesignScriptTool.cs                    (Core: execute_designscript)
│   │   └── DynamoContextTool.cs                          (Core: get_dynamo_context)
│   └── Registry/SeedLibrary/                             (12 Published Seeds)
│       ├── Graph/run_dyn_file/
│       ├── Workspace/get_active_graph_info/
│       ├── Library/search_nodes_library/
│       ├── Library/inspect_node_type/
│       ├── Node/add_node_to_workspace/
│       ├── Wire/connect_node_ports/
│       ├── Wire/disconnect_node_ports/
│       ├── Node/set_node_properties/
│       ├── Data/get_node_output_values/
│       ├── Export/export_graph_to_image/
│       ├── Graph/validate_dyn_file/
│       └── CodeBlock/create_code_block_node/
│
├── HPDynamo.McpBridge/                                   (net8.0-windows7.0, In-Process Bridge)
│   ├── Entry/
│   │   ├── DynamoRevitApp.cs                             (IExternalApplication, pipe listener)
│   │   └── DynamoBridgeViewExtension.cs                  (IViewExtension, live canvas hook)
│   ├── Coordinator/
│   │   └── DynamoBridgeCoordinator.cs                    (Dual lifecycle router)
│   ├── Execution/
│   │   ├── RevitTransactionScope.cs                      (TransactionGroup rollback engine)
│   │   ├── HeadlessGraphRunner.cs                        (RevitDynamoModel headless runner)
│   │   └── CanvasGraphRunner.cs                          (DynamoViewModel canvas runner)
│   ├── View/
│   │   ├── DynamoBridgeStatusView.xaml                   (WPF Modeless Dialog, MaterialDesign 5.3.2)
│   │   ├── DynamoBridgeStatusView.xaml.cs
│   │   └── MaterialThemeBridge.cs
│   └── Manifests/
│       ├── HPDynamo.McpBridge.addin
│       └── HPDynamo_ViewExtensionDefinition.xml
│
├── HPDynamo.Mcp.Server.Tests/                            (net10.0, xUnit v3 / MTP)
│   ├── Registry/
│   │   └── SeedValidationTests.cs
│   └── Scripting/
│       └── DynamoScriptGuardTests.cs
│
└── HPDynamo.McpBridge.Tests/                             (net8.0-windows7.0, xUnit v3 / MTP)
    ├── Protocol/
    │   └── DynamoDispatcherTests.cs
    └── Runner/
        └── GraphValidationTests.cs
```
