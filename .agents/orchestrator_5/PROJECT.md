# Project: HPPowerBi Subsystem (Power BI MCP)

## Architecture
- Standalone WPF Desktop Bridge (`HPPowerBi.McpBridge`, .NET 8.0-windows) connecting out-of-process to local Power BI Desktop instances via local Analysis Services (AMO-TOM / ADOMD.NET) and Power BI Service Cloud REST API via MSAL.
- Stdio MCP Server (`HPPowerBi.Mcp.Server`, .NET 10.0 console) implementing `PowerBiHostProfile` using `McpShared`, exposing 8 Core Local tools, 4 Cloud REST tools, and 8 Dynamic Registry meta-tools.
- Named pipe communication over `hppowerbi-mcp-2026` (`PipeNaming.PowerBiHost = "powerbi"`, `PipeNaming.For("powerbi", 2026)`).
- 3-Layer Safety: Dual UI opt-in switches, DAX AST validation / `GuardProfile.PowerBi`, and automatic pre-mutation TMSL JSON snapshots.
- Solution structure: `HPPowerBi/HPPowerBi.slnx` bundling Bridge, Server, and two test suites (`HPPowerBi.McpBridge.Tests` and `HPPowerBi.Mcp.Server.Tests`).

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Solution & Project Scaffolding | HPPowerBi.slnx, Directory.Build.props, global.json, 4 csproj files | M1 | Survey |
| 2 | PBIDesktop & Port Detection | Detect PBIDesktop.exe, read UTF-16LE msmdsrv.port.txt in AnalysisServicesWorkspaces | M1 | R1 |
| 3 | AMO-TOM & ADOMD Connection | Connect to local SSAS tabular engine via official NuGet packages | M1 | R1 |
| 4 | Tabular Schema Reading | Enumerate Model, Tables, Columns, Measures, Partitions, Relationships | M1 | R1/R2 |
| 5 | DAX Query Execution | Execute DAX queries via ADOMD.NET, serialize tabular JSON with rowcount/duration | M1 | R1/R2 |
| 6 | Measure & Relationship CRUD | Create/update/delete measures and manage relationships via TOM SaveChanges() | M1 | R1/R2 |
| 7 | 3-Layer Safety & Snapshot Manager | Dual UI opt-in, DAX validation, automatic pre-mutation TMSL JSON snapshot | M1 | R1 |
| 8 | External Tools Auto-Registration | Generate and register HPPowerBi.pbitool.json in Power BI Desktop External Tools | M1 | R1 |
| 9 | Cloud REST API Client | MSAL OAuth 2.0 / Service Principal auth for workspaces, datasets, refresh, DAX | M1 | R1/R2 |
| 10 | Bridge Executor Implementation | Implement IBridgeExecutor for McpBridgeHost | M1 | R1 |
| 11 | WPF MVVM MaterialDesign UI | MaterialDesign 5.3.2 UI, Power BI yellow brand palette, WindowsHostTheme, status window | M2 | R1 |
| 12 | Named Pipe Host Integration | BridgeEntry, App.xaml.cs, pipe listener on hppowerbi-mcp-2026 | M2 | R1 |
| 13 | PowerBiHostProfile | Implement IHostProfile for HPPowerBi.Mcp.Server with tools/resources/prompts | M3 | R2 |
| 14 | Core Local Tools (8 tools) | get_powerbi_context, execute_powerbi_code, get_schema, evaluate_dax, measure & relationship CRUD, format_dax | M3 | R2 |
| 15 | Cloud REST Tools (4 tools) | cloud_list_workspaces, cloud_list_datasets, cloud_trigger_refresh, cloud_execute_dax | M3 | R2 |
| 16 | Dynamic Tool Registry Integration | Integrate McpShared tool registry engine and 8 meta tools | M3 | R2 |
| 17 | Bridge & Server Automated Tests | HPPowerBi.McpBridge.Tests & HPPowerBi.Mcp.Server.Tests passing 100% | M4 | R3 |
| 18 | Skill & Documentation | hp-mcp-powerbi/SKILL.md and AGENTS.md registration | M4 | R4 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| 1 | M1: Bridge Core Engine & Solution Setup | HPPowerBi.slnx, props, global.json, McpBridge core services (TOM/ADOMD, port discovery, safety, snapshots, cloud, external tools) | none | DONE |
| 2 | M2: Bridge WPF UI & Pipe Host | MaterialDesign WPF UI, ViewModels, Themes, BridgeEntry, NamedPipe listener | M1 | DONE |
| 3 | M3: MCP Stdio Server & Tools | PowerBiHostProfile, 8 local tools, 4 cloud tools, dynamic registry, Program.cs | M1, M2 | DONE |
| 4 | M4: Tests, Docs & Verification | HPPowerBi.McpBridge.Tests & HPPowerBi.Mcp.Server.Tests, SKILL.md, AGENTS.md | M2, M3 | IN_PROGRESS |

## Interface Contracts
### McpBridge ↔ McpServer
- Named pipe: `hppowerbi-mcp-2026` (`PipeNaming.For("powerbi", 2026)`)
- Methods: `powerbi.ping`, `powerbi.context`, `powerbi.execute`, `powerbi.cancel`, `powerbi.inspect`, `powerbi.analyze`
- High-level methods: `powerbi.dax`, `powerbi.format_dax`, `powerbi.schema`, `powerbi.measure.upsert`, `powerbi.measure.delete`, `powerbi.relationship.manage`, `powerbi.cloud.workspaces`, `powerbi.cloud.datasets`, `powerbi.cloud.refresh`, `powerbi.cloud.dax`
- DTOs: `ContextResult.PowerBi` -> `PowerBiInfo` (`IsConnected`, `AttachedPid`, `LocalPort`, `DatabaseName`, `CompatibilityLevel`, `MutationEnabled`, `TableCount`, `MeasureCount`, `RelationshipCount`)
- Execution: `ExecuteRequest` -> `ExecuteResult` (`IsSuccess`, `StandardOutput`, `StandardError`, `ReturnValue`, `Snapshot`)
- C# Script Globals: `model` (`Microsoft.AnalysisServices.Tabular.Model`), `server` (`Microsoft.AnalysisServices.Tabular.Server`), `adomd` (`Microsoft.AnalysisServices.AdomdClient.AdomdConnection`), `ct`, `log`, `progress`, `args`

## Code Layout
- `HPPowerBi/`
  - `HPPowerBi.slnx`
  - `Directory.Build.props`
  - `global.json`
  - `HPPowerBi.McpBridge/` (Bridge application: Discovery/, Tabular/, Safety/, ExternalTools/, Cloud/, Host/, ViewModels/, Views/, Resources/Themes/)
  - `HPPowerBi.Mcp.Server/` (Server application: Hosts/PowerBi/, Tools/, Resources/, Prompts/)
  - `HPPowerBi.McpBridge.Tests/` (Unit tests for bridge services)
  - `HPPowerBi.Mcp.Server.Tests/` (Unit tests for server profile, tools, and pipe round-trips)
- `.agents/skills/hp-mcp-powerbi/SKILL.md`
- `AGENTS.md`
