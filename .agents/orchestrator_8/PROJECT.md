# Project: Trimble Tekla Structures 2025.0 MCP Solution (HPTekla)

## Architecture
HPTekla connects AI Agents to Trimble Tekla Structures 2025.0 via Tekla Open API.
- **McpShared** (`McpShared/`): Shared, host-neutral engine multi-targeted for .NET Standard 2.0 / .NET Framework 4.8 / .NET 8 / .NET 10. Contains host definitions, script contracts, guard profiles, analyzers, pipe communication, dynamic tool registry, and SQLite persistence.
- **HPTekla.McpBridge** (`HPTekla/HPTekla.McpBridge/`): In-Process Plugin for Tekla Structures 2025.0 (.NET Framework 4.8 / CLR v4.0.30319). Listens on Named Pipe `hptekla-mcp-2025`. Marshals incoming calls to Tekla main thread via `MainThreadQueue` pumped on `ComponentDispatcher.ThreadIdle`. Enforces 3-tier safety (Read, Write, Destructive), native transaction savepoint rollback for `dryRun = true` (`Tekla.Structures.ModelInternal.Operation.SetTestSavePoint` and `RollbackToTestSavePoint`), automatic model database snapshotting (`.db1`/`.db2`), and custom assembly resolution via `PluginAssemblyResolver`. Hosts WPF Modeless Status Dialog and Tekla Ribbon button.
- **HPTekla.Mcp.Server** (`HPTekla/HPTekla.Mcp.Server/`): Stdio MCP Console Server (.NET 10.0). Connects over Named Pipe `hptekla-mcp-2025` to the bridge. Exposes standard Model Context Protocol (MCP 2.2.0) interface with 24 tools: 4 Core Tools, 8 Registry Meta Tools, and 12 Embedded Seed Tools for structural steel and concrete rebar.
- **Automated Test Suites**:
  - `HPTekla.Mcp.Server.Tests` (.NET 10.0): Validates `TeklaHostProfile`, 24 tools registry, seed schemas, and Roslyn seed script compilation against Tekla Open API assemblies.
  - `HPTekla.McpBridge.Tests` (.NET Framework 4.8): Validates script guard, 3-tier safety analyzer, dry-run suppression, and pipe communication with mock Tekla executor.
  - `HPRebar.Mcp.Server.Core.Tests` & `HPRebar.McpBridge.Core.Net48Tests`: Validates host neutrality and zero regressions across existing 9 hosts.
  - `HPTekla/tools/harness/`: Python live verification harness using `McpShared/tools/harness_common.py`.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | McpShared Host Contracts | Define `PipeNaming.TeklaHost = "tekla"`, pipe `hptekla-mcp-{version}`, prefix `tekla.` | M1 | Survey |
| 2 | McpShared Script Contracts | Define `HostScriptContracts.TeklaImports`, `TeklaGlobals`, timeout 600s | M1 | Survey |
| 3 | McpShared Guard & Analyzer | Define `GuardProfile.Tekla` and `AnalyzerProfile.Tekla` | M1 | Survey |
| 4 | McpShared Context DTOs | Define `ContextResult.Tekla` and `TeklaInfo` | M1 | Survey |
| 5 | McpShared Host Profile | Define `HostProfile.Tekla` with version 2025 | M1 | Survey |
| 6 | McpShared Neutrality Tests | Test host neutrality and Tekla profile in Server.Core.Tests & Net48Tests | M1 | Survey |
| 7 | In-Process Tekla Plugin | `[Plugin("HPTeklaBridge")]` targeting `net48`, loading inside TeklaStructures.exe | M2 | Survey |
| 8 | Assembly Isolation Resolver | `PluginAssemblyResolver` for reliable Roslyn compilation on .NET 4.8 | M2 | Survey |
| 9 | Thread Synchronization | `MainThreadQueue` pumped on `ComponentDispatcher.ThreadIdle` with `WM_NULL` wakeup | M2 | Survey |
| 10 | 3-Tier Safety System | Read, Write, Destructive categorization; `AllowHeavyOperations` gate | M2 | Survey |
| 11 | Native DryRun Rollback | Use `SetTestSavePoint()` and `RollbackToTestSavePoint()` when `dryRun == true` | M2 | Survey |
| 12 | Model Snapshot Engine | Auto-copy `.db1` and `.db2` to `.hptekla_snapshots/` before writes | M2 | Survey |
| 13 | WPF Status Dialog & Ribbon | Modeless window with dark/light theming, connection status, logs, toggles, Ribbon XML | M2 | Survey |
| 14 | Stdio Console Server | .NET 10 console executable running `McpServerHost.RunAsync` | M3 | Survey |
| 15 | Tekla Host Profile | `TeklaHostProfile` implementing `IHostProfile` with pipe `hptekla-mcp-2025` | M3 | Survey |
| 16 | 4 Core Tools | `execute_tekla_code`, `get_tekla_context`, `inspect_type`, `cancel_execution` | M3 | Survey |
| 17 | 8 Registry Meta Tools | `search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool` | M3 | Survey |
| 18 | 12 Embedded Seed Tools | Steel framing, concrete plates, reinforcement groups, rebars, UDAs, drawings, IFC | M3 | Survey |
| 19 | Server Test Suite | `HPTekla.Mcp.Server.Tests` net10 verifying profile, seeds, schemas, compilation | M4 | Survey |
| 20 | Bridge Test Suite | `HPTekla.McpBridge.Tests` net48 verifying guard, safety tiers, rollback, pipe | M4 | Survey |
| 21 | Live Verification Harness | Python harness in `HPTekla/tools/harness/` consuming `harness_common.py` | M4 | Survey |
| 22 | Solution Configuration | `HPTekla/HPTekla.slnx` with all 4 projects, clean build, isolated dependencies | M5 | Survey |
| 23 | Skill Documentation | `.agents/skills/hp-mcp-tekla/SKILL.md` documenting tools and safety guidelines | M5 | Survey |
| 24 | AGENTS.md Registration | Register HPTekla deliverable in repository architecture table in `AGENTS.md` | M5 | Survey |
| 25 | Final Verification & Victory Audit | Full regression build/test, forensic audit verification, victory reporting | M6 | Survey |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| 1 | McpShared Additive Integration | Additive Tekla profile, contracts, guards, analyzers, DTOs, tests in McpShared | none | DONE |
| 2 | HPTekla.McpBridge In-Process Plugin | .NET 4.8 Plugin, Thread Queue, 3-Tier Safety, SavePoint Rollback, Snapshot, WPF UI | M1 | DONE |
| 3 | HPTekla.Mcp.Server Stdio Console | .NET 10 Server, TeklaHostProfile, 24 tools (4 core, 8 meta, 12 seeds) | M1 | DONE |
| 4 | Automated Test Suites & Live Harness | Server.Tests (net10), Bridge.Tests (net48), Python live harness | M2, M3 | DONE |
| 5 | Solution Packaging & Ecosystem Docs | HPTekla.slnx, AGENTS.md registration, hp-mcp-tekla SKILL.md | M4 | IN_PROGRESS |
| 6 | Final Verification & Victory Audit | Full regression verification, forensic audit pass, Sentinel victory report | M5 | PLANNED |

## Interface Contracts
### AI Client ↔ HPTekla.Mcp.Server (stdio)
- Protocol: Model Context Protocol (MCP 2.2.0) over standard input/output.
- Standard requests: `initialize`, `tools/list` (reporting 24 tools), `tools/call`.

### HPTekla.Mcp.Server ↔ HPTekla.McpBridge (Named Pipe)
- Pipe Name: `hptekla-mcp-2025`
- Methods: `tekla.execute`, `tekla.context`, `tekla.ping`, `tekla.cancel`, `tekla.inspect`, `tekla.analyze`.
- Request Payload (`ExecuteRequest`): `Code` (string), `Args` (dictionary), `Transaction` ("auto"|"manual"|"none"), `DryRun` (bool), `TimeoutSeconds` (int).
- Response Payload (`ExecuteResult`): `Success` (bool), `Output` (string), `Value` (object), `Error` (string), `ExecutionTimeMs` (long), `Snapshot` (string).

### HPTekla.McpBridge ↔ Tekla Structures 2025.0 (In-Process)
- Assemblies: `Tekla.Structures.dll`, `Tekla.Structures.Model.dll`, `Tekla.Structures.Catalogs.dll`, `Tekla.Structures.Datatype.dll`, `Tekla.Structures.Drawing.dll`.
- Script Globals:
  - `model`: `Tekla.Structures.Model.Model`
  - `selector`: `Tekla.Structures.Model.UI.ModelObjectSelector`
  - `ct`: `CancellationToken`
  - `log`: `Action<string>`
  - `args`: `IReadOnlyDictionary<string, object>`

## Code Layout
- `McpShared/`
  - `HPRebar.Mcp.Contracts/`: Wire DTOs and host naming.
  - `HPRebar.McpBridge.Core/`: Guard profiles, AST analyzers, thread queues.
  - `HPRebar.Mcp.Server.Core/`: MCP engine, dynamic tool registrar.
  - `HPRebar.Mcp.Server.Core.Tests/` & `HPRebar.McpBridge.Core.Net48Tests/`: Host neutrality and engine tests.
- `HPTekla/`
  - `HPTekla.slnx`: XML solution file.
  - `Directory.Build.props`: Shared assembly info and Tekla bin paths.
  - `HPTekla.McpBridge/`: Plugin, Named Pipe listener, thread sync, WPF dialog, assembly resolver.
  - `HPTekla.Mcp.Server/`: Console stdio server, TeklaHostProfile, 12 embedded seeds.
  - `HPTekla.Mcp.Server.Tests/`: xUnit tests for Server on .NET 10.
  - `HPTekla.McpBridge.Tests/`: xUnit tests for Bridge on .NET Framework 4.8.
  - `tools/harness/`: Python live verification harness scripts.
