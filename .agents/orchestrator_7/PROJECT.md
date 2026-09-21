# Project: HPRobot MCP Subsystem

## Architecture
The HPRobot deliverable provides an end-to-end Model Context Protocol (MCP) subsystem enabling AI coding agents and LLMs to interact safely, performantly, and expressively with Autodesk Robot Structural Analysis Professional 2026.

### 1. Architectural Isolation & Boundaries
- Reference Direction: `HPRobot/` references strictly `../McpShared/` projects (`HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Server.Core`).
- Absolute Isolation: `HPRobot/` NEVER cross-references sibling host projects (`HPRebar`, `HPAutoCad`, `HPNavis`, `HPEtabs`, `HPCivil3d`, `HPSap2000`, `HPPowerBi`, `HPExcel`).
- Shared Host Neutrality: All shared contracts, pipe naming rules, JSON-RPC prefixes, context shaping, and security guard profiles reside in `McpShared/` without adding external Autodesk/Robot dependencies to `McpShared`.

### 2. Component Architecture
```
┌────────────────────────────────────────────────────────┐
│               LLM / AI Client (Stdio)                  │
└───────────────────────────┬────────────────────────────┘
                            │ Stdio JSON-RPC
                            ▼
┌────────────────────────────────────────────────────────┐
│            HPRobot.Mcp.Server (.NET 10)                │
│   - RobotHostProfile (hprobot-mcp-2026)                │
│   - Core Tools (get_robot_context, execute_robot_code) │
│   - 12 Embedded Seed Tools Catalog                     │
│   - 8 Registry Meta Tools                              │
│   - Resources (robot://) & Prompts                     │
└───────────────────────────┬────────────────────────────┘
                            │ Named Pipe: hprobot-mcp-2026
                            ▼
┌────────────────────────────────────────────────────────┐
│          HPRobot.McpBridge (.NET 8 Windows WPF)        │
│   - MaterialDesign 5.3.2 UI with Auto Dark/Light Theme │
│   - PipeListener + RequestDispatcher                   │
│   - 3-Tier Safety Gating (R / W / D-Heavy)             │
│   - RobotTierAnalyzer (Roslyn AST semantic classifier) │
│   - RobotSnapshotManager (.hprobot_snapshots/)         │
│   - RobotUnitsPolicy (Metric: m, kN, kN·m, MPa)        │
└───────────────────────────┬────────────────────────────┘
                            │ Out-of-Process COM (RobotOM.dll)
                            ▼
┌────────────────────────────────────────────────────────┐
│   Autodesk Robot Structural Analysis Professional 2026 │
│   - robot.exe (x64, Build 39.0.1.11984)                │
│   - Interop.RobotOM.dll (3,078 exported types)         │
│   - IRobotApplication (ProgID: Robot.Application)      │
│   - CalcEngine & FEA Solver                            │
└────────────────────────────────────────────────────────┘
```

### 3. Safety & Snapshot Engine
- **Tier R (Read)**: `get_model_info`, `get_structural_objects`, `get_materials_and_sections`, `get_coordinate_systems_and_grids`, `get_load_definitions`, `get_node_reactions`, `get_bar_forces`, `get_robot_context`. Gated by `AllowExecution` toggle; no snapshot required.
- **Tier W (Write)**: `draw_bar_by_coords`, `assign_node_support`, `assign_bar_section`, `assign_bar_load`. Gated by `AllowExecution` toggle; triggers automatic `.rtd` snapshot to `.hprobot_snapshots/` or `%TEMP%`.
- **Tier D / Heavy (Destructive & FEA)**: `run_calculations` (`Calculate()`), structural object deletion, clear/new project. Gated by `AllowHeavyOperations` toggle (off by default); triggers automatic `.rtd` snapshot.
- **Snapshot Storage**: Saved to `.hprobot_snapshots/` adjacent to the active `.rtd` model (fallback to `%TEMP%\.hprobot_snapshots\`). Retains newest 20 snapshots. Snapshot filename returned in `ExecuteResult.Snapshot`.
- **Units Policy**: Standardizes internal unit manager to Metric (`m`, `kN`, `kN·m`, `MPa`) during script execution and restores previous user preferences in a `finally` block.

---

## Feature Inventory

Every feature from requirements and survey is cataloged and assigned to a milestone below:

| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | PipeNaming.RobotHost | Add RobotHost ("robot") and mapping to "hprobot-mcp-{version}" in McpShared | M1 | Survey 1 |
| 2 | JsonRpcMethods.RobotPrefix | Add RobotPrefix ("robot.") in McpShared | M1 | Survey 1 |
| 3 | HostScriptContracts.Robot | Add RobotImports, RobotGlobals, and RobotHeavyMaxTimeoutSeconds (300s) | M1 | Survey 1 |
| 4 | ContextMessages.RobotInfo | Add RobotInfo DTO and ContextResult.Robot property in McpShared | M1 | Survey 1 |
| 5 | GuardProfile.Robot & AnalyzerProfile.Robot | Add Robot safety profile (blocking Quit, dialogs, bridge internals) & empty analyzer | M1 | Survey 1 |
| 6 | McpShared Robot Unit Tests | Add RobotTestProfile and RobotProfileTests in Mcp.Server.Core.Tests | M1 | Survey 1 |
| 7 | HPRobot Solution Configuration | Create HPRobot.slnx, Directory.Build.props, global.json, README.md | M2 | Survey 1/2 |
| 8 | McpBridge Project Skeleton | Setup HPRobot.McpBridge.csproj (net8.0-windows, WPF, MaterialDesign 5.3.2, Interop.RobotOM) | M2 | Survey 2/3 |
| 9 | COM Attachment & Lifecycle | RobotAttachment, ComInteropHelper, oleaut32!GetActiveObject, ROT management | M2 | Survey 2 |
| 10 | RobotUnitsPolicy | Standardize units to Meter, kN, kN·m, MPa during execution; restore user units in finally | M2 | Survey 2 |
| 11 | 3-Tier Safety Engine | RobotSafetyGuard, RobotTierTable, RobotTierAnalyzer (Roslyn AST semantic classifier) | M2 | Survey 3 |
| 12 | Automatic Snapshot Engine | RobotSnapshotManager (.hprobot_snapshots/), SaveAs backup, retention pruning | M2 | Survey 2/3 |
| 13 | MaterialDesign 5.3.2 UI | MainWindow.xaml, MainWindowViewModel.cs, MaterialThemeBridge, Dark/Light theme, checkboxes | M2 | Survey 2 |
| 14 | Bridge Pipe Listener & Dispatcher | PipeListener on hprobot-mcp-2026, RobotDispatcher, RobotBridgeExecutor | M2 | Survey 2 |
| 15 | Mcp.Server Project Skeleton | HPRobot.Mcp.Server.csproj (.NET 10 console), Program.cs, appsettings.json, RobotHostProfile | M3 | Survey 1/3 |
| 16 | Core Tool: get_robot_context | Context tool returning active model name, path, mode, node/bar/panel count, calc state | M3 | Survey 3 |
| 17 | Core Tool: execute_robot_code | Roslyn C# script tool with robot, structure, units, args, log, progress, ct | M3 | Survey 3 |
| 18 | Registry Meta Tools | 8 dynamic tool registry meta tools integrated from McpShared | M3 | Survey 1/3 |
| 19 | Seed Tool: get_model_info | Read general model info (version, project type, file path, units) | M3 | Survey 3 |
| 20 | Seed Tool: get_structural_objects | Read structural elements (Nodes, Bars, Panels) with coordinates & numbers | M3 | Survey 3 |
| 21 | Seed Tool: get_materials_and_sections | Read material and bar/panel section properties | M3 | Survey 3 |
| 22 | Seed Tool: get_coordinate_systems_and_grids | Read structural axes and grid lines | M3 | Survey 3 |
| 23 | Seed Tool: get_load_definitions | Read load cases, combinations, and nature | M3 | Survey 3 |
| 24 | Seed Tool: draw_bar_by_coords | Create bar element by end coordinates with section assignment (Tier W) | M3 | Survey 3 |
| 25 | Seed Tool: assign_node_support | Assign support/boundary condition to node (Tier W) | M3 | Survey 3 |
| 26 | Seed Tool: assign_bar_section | Assign section label to bar element (Tier W) | M3 | Survey 3 |
| 27 | Seed Tool: assign_bar_load | Apply uniform/concentrated load to bar (Tier W) | M3 | Survey 3 |
| 28 | Seed Tool: run_calculations | Run FEA structural calculations via CalcEngine.Calculate() (Tier D/Heavy) | M3 | Survey 3 |
| 29 | Seed Tool: get_node_reactions | Read nodal support reactions (FX, FY, FZ, MX, MY, MZ) | M3 | Survey 3 |
| 30 | Seed Tool: get_bar_forces | Read internal bar forces along chord points | M3 | Survey 3 |
| 31 | Test Suite: Mcp.Server.Tests | 24 tools validation, schema tests, fake pipe round-trip, Roslyn compile of 12 seeds | M4 | Survey 3 |
| 32 | Test Suite: McpBridge.Tests | Safety tier classification, snapshot manager backup/restore, units policy tests | M4 | Survey 3 |
| 33 | Ecosystem Documentation | .agents/skills/hp-mcp-robot/SKILL.md documenting tools, schemas, workflows | M5 | Context |
| 34 | AGENTS.md Registration | Register HPRobot deliverable in repo AGENTS.md table and guidelines | M5 | Context |
| 35 | E2E Dual Track Final Verification | Pass 100% of automated tests and clean solution build of HPRobot.slnx | M6 | Context |

---

## Milestones

| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | McpShared Host Integration | Register Robot host in McpShared (PipeNaming, JsonRpcMethods, HostScriptContracts, ContextMessages, GuardProfile, AnalyzerProfile, RobotProfileTests) | none | DONE |
| M2 | HPRobot McpBridge & Safety | Build HPRobot.McpBridge: Solution setup, COM Interop, 3-tier safety engine, snapshot backup manager, units policy, MaterialDesign 5.3.2 WPF UI | M1 | DONE |
| M3 | HPRobot Stdio Server & Tools | Build HPRobot.Mcp.Server: RobotHostProfile, Core Tools (`get_robot_context`, `execute_robot_code`), 12 embedded seed tools with schemas, examples, and Roslyn scripts | M1 | DONE |
| M4 | Automated Test Suites | Build and verify HPRobot.Mcp.Server.Tests (.NET 10) and HPRobot.McpBridge.Tests (.NET 8 Windows) | M2, M3 | DONE |
| M5 | Skill & Repo Documentation | Create `.agents/skills/hp-mcp-robot/SKILL.md` and update `AGENTS.md` | M3, M4 | PLANNED |
| M6 | Final Verification & E2E Track | Execute clean solution build of `HPRobot.slnx` (0 errors), run all test suites (100% pass), and verify dual-track compliance with live harness | M1, M2, M3, M4, M5 | PLANNED |

---

## Interface Contracts

### 1. Server ↔ Bridge Named Pipe Wire Contract
- Named Pipe: `hprobot-mcp-2026` (via `PipeNaming.For(PipeNaming.RobotHost, 2026)`)
- Wire Methods:
  - `robot.ping` -> `PingResult`
  - `robot.context` -> `ContextResult` (with `RobotInfo`)
  - `robot.execute` -> `ExecuteResult` (with `Snapshot` filename)
  - `robot.cancel` -> `CancelResult`
  - `robot.analyze` -> `AnalyzeResult` (with AST tier classification)

### 2. Context Service Contract (`ContextResult.Robot` / `RobotInfo`)
```csharp
public sealed record RobotInfo(
    bool IsAttached,
    int? AttachedPid,
    string? RobotVersion,
    string? StructureType,
    bool IsCalculated,
    bool HeavyOperationsEnabled,
    int NodeCount,
    int BarCount,
    int PanelCount,
    int LoadCaseCount);
```

### 3. Roslyn Script Environment Contract (`execute_robot_code`)
- Globals:
  - `robot`: `RobotOM.IRobotApplication`
  - `structure`: `RobotOM.IRobotStructure`
  - `units`: `RobotOM.IRobotUnitMngr`
  - `args`: `ScriptArgs`
  - `log`: `Action<string>`
  - `progress`: `Action<int, int?, string?>`
  - `ct`: `CancellationToken`
- Default Usings: `System`, `System.Linq`, `System.Collections.Generic`, `RobotOM`, `HPRebar.McpBridge.Core.Scripting`
- Guard Deny-List: `Quit`, `ApplicationExit`, `Interactive`, `MessageBox`, `System.Diagnostics.Process`, `#r`/`#load` directives, reflection, threading.

---

## Code Layout

```
HPRobot/
├── HPRobot.slnx
├── Directory.Build.props
├── global.json
├── README.md
├── HPRobot.McpBridge/
│   ├── HPRobot.McpBridge.csproj
│   ├── Program.cs
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── BridgeEntry.cs
│   ├── Com/
│   │   ├── RobotAttachment.cs
│   │   ├── ComInteropHelper.cs
│   │   └── RobotAssemblyResolver.cs
│   ├── Units/
│   │   └── RobotUnitsPolicy.cs
│   ├── Safety/
│   │   ├── RobotSafetyGuard.cs
│   │   ├── RobotTier.cs
│   │   ├── RobotTierTable.cs
│   │   ├── RobotTierAnalyzer.cs
│   │   └── RobotSnapshotManager.cs
│   ├── Host/
│   │   ├── RobotBridgeExecutor.cs
│   │   ├── RobotDispatcher.cs
│   │   └── RobotScriptGlobals.cs
│   ├── ViewModels/
│   │   └── MainWindowViewModel.cs
│   ├── Views/
│   │   ├── MainWindow.xaml
│   │   └── MainWindow.xaml.cs
│   └── Resources/Themes/
│       ├── ThemeInfo.cs
│       ├── IHostTheme.cs
│       ├── WindowsHostTheme.cs
│       ├── MaterialThemeBridge.cs
│       ├── MaterialBridge.xaml
│       ├── ThemeLight.xaml
│       ├── ThemeDark.xaml
│       └── RobotTheme.xaml
├── HPRobot.McpBridge.Tests/
│   ├── HPRobot.McpBridge.Tests.csproj
│   ├── SafetyGatingTests.cs
│   ├── RobotTierAnalyzerTests.cs
│   ├── RobotSnapshotManagerTests.cs
│   ├── RobotUnitsPolicyTests.cs
│   └── RobotDispatcherTests.cs
├── HPRobot.Mcp.Server/
│   ├── HPRobot.Mcp.Server.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Hosts/Robot/
│   │   ├── RobotHostProfile.cs
│   │   ├── RobotContextService.cs
│   │   └── Tools/
│   │       ├── GetRobotContextTool.cs
│   │       └── ExecuteRobotCodeTool.cs
│   └── Registry/SeedLibrary/
│       ├── get_model_info/
│       ├── get_structural_objects/
│       ├── get_materials_and_sections/
│       ├── get_coordinate_systems_and_grids/
│       ├── get_load_definitions/
│       ├── draw_bar_by_coords/
│       ├── assign_node_support/
│       ├── assign_bar_section/
│       ├── assign_bar_load/
│       ├── run_calculations/
│       ├── get_node_reactions/
│       └── get_bar_forces/
├── HPRobot.Mcp.Server.Tests/
│   ├── HPRobot.Mcp.Server.Tests.csproj
│   ├── RobotHostProfileTests.cs
│   ├── SeedCatalogTests.cs
│   ├── SeedExecutionTests.cs
│   └── SeedCompilationTests.cs
└── tools/
    └── harness/
        ├── run-live-verify.ps1
        └── live-verify.py
```
