# HPRobot MCP Subsystem — McpShared Architecture & Sibling COM Host Analysis

**Author:** Explorer 1 (`explorer_survey_robot_1`)  
**Parent:** Project Orchestrator (`orchestrator_7`, conversation ID: `b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Date:** 2026-09-21  
**Scope:** Architecture mapping of `McpShared`, comparative study of sibling COM hosts (`HPSap2000`, `HPEtabs`, `HPExcel`), exact integration specification for Robot Structural Analysis Professional 2026, and regression defense.

---

## Executive Summary

This investigation analyzes how the host-neutral MCP engine in `McpShared` can be extended to support **Robot Structural Analysis Professional 2026** (`HPRobot`) as the 9th host ecosystem, following the proven architectural patterns of `HPSap2000` and `HPEtabs`.

The live test baseline of `McpShared` was measured directly:
- `McpShared/HPRebar.Mcp.Server.Core.Tests` (net10.0): **385 tests passed, 0 failed, 0 skipped** (duration: 3.5s).
- `McpShared/HPRebar.McpBridge.Core.Net48Tests` (net48): **71 tests passed, 0 failed, 0 skipped** (duration: 3.0s).
- **Total McpShared baseline: 456 tests passed, 100% pass rate.**

On the host machine, Robot Structural Analysis Professional 2026 is installed:
- Executable: `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\robot.exe`
- COM Assembly: `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll` (Assembly `Interop.RobotOM`, v1.0.0.0, neutral)
- COM ProgID: `Robot.Application`
- COM CLSID: `{F7870790-CDE5-11D1-8FF1-00A02447BAAE}`

---

## 1. McpShared Architecture Mapping

`McpShared/` is the shared, host-neutral MCP engine powering all CAD/BIM/CAE host implementations (`Revit`, `AutoCAD`, `Navisworks`, `ETABS`, `Civil 3D`, `SAP2000`, `Power BI`, `Excel`).

```
McpShared/
├── HPRebar.Mcp.Contracts/         # netstandard2.0;net48 (Wire DTOs, JSON-RPC, PipeNaming)
├── HPRebar.McpBridge.Core/        # net8.0;net48 (PipeListener, Roslyn guard/compiler, Settings)
├── HPRebar.Mcp.Server.Core/       # net10.0 (Server bootstrap, bridge client, registry engine)
├── HPRebar.Mcp.Server.Core.Tests/ # net10.0 (385 xUnit v3 tests)
└── HPRebar.McpBridge.Core.Net48Tests/ # net48 (71 xUnit v3 tests)
```

### 1.1 `HPRebar.Mcp.Contracts`
Defines the wire contracts between server exes and bridges:

1. **`PipeNaming.cs`** (lines 10–76):
   - Maintains host constants (`RevitHost`, `AutocadHost`, `NavisHost`, `EtabsHost`, `Civil3dHost`, `Sap2000Host`, `PowerBiHost`, `ExcelHost`).
   - `For(string host, int version)` maps a host key and version to a canonical pipe name.
   - For Robot: `RobotHost = "robot"`; pipe format is `"hprobot-mcp-" + version` (e.g., `hprobot-mcp-2026`).

2. **`JsonRpc/JsonRpcMethods.cs`** (lines 10–70):
   - Defines host-neutral method suffixes (`ping`, `context`, `inspect`, `execute`, `cancel`, `analyze`, `progress`, `log`, `status`).
   - Defines host prefixes (`revit.`, `autocad.`, `navis.`, `etabs.`, `civil3d.`, `sap2000.`, `powerbi.`, `excel.`).
   - For Robot: Add `RobotPrefix = "robot."`. Suffix extraction (`JsonRpcMethods.Suffix`) guarantees any bridge responds to its suffix regardless of prefix.

3. **`HostScriptContracts.cs`** (lines 1–187):
   - Single source of truth for script `using` imports and global variables visible to Roslyn scripts.
   - Every host declares: `<Host>Imports`, `<Host>Globals`, and `<Host>HeavyMaxTimeoutSeconds`.
   - For Robot:
     - `RobotImports`: `System`, `System.Linq`, `System.Collections.Generic`, `RobotOM`, `HPRebar.McpBridge.Core.Scripting`.
     - `RobotGlobals`: `robot`, `structure`, `units`, `ct`, `log`, `progress`, `args`.
     - `RobotHeavyMaxTimeoutSeconds`: `300` (seconds).

4. **`Messages/ContextMessages.cs`** (lines 12–200):
   - `ContextResult` contains host-specific nested objects (`Autocad`, `Navis`, `Etabs`, `Civil3d`, `Sap2000`, `PowerBi`, `Excel`).
   - Serialization uses `BridgeJson.Options` with `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`.
   - When a host does not populate another host's property, that property is completely omitted from the JSON payload.
   - For Robot: Add `RobotInfo? Robot` to `ContextResult` and define the `RobotInfo` record.

### 1.2 `HPRebar.McpBridge.Core`
Host-free bridge engine:

1. **`Scripting/GuardProfile.cs`** (lines 9–214):
   - Static deny-list configuration layered on top of `ScriptGuard`'s base deny list (which unconditionally denies `System.IO`, `System.Reflection`, `System.Net`, `System.Diagnostics.Process`, `System.Threading.Tasks`, `System.Linq.Expressions`, `Marshal`, etc.).
   - Adds host-specific prohibited identifiers, members, and namespaces.
   - For Robot:
     - Prohibit UI modals (`MessageBox`).
     - Prohibit process termination (`Quit`, `ApplicationExit`).
     - Prohibit bridge internals (`HPRobot.McpBridge`, `HPRebar.McpBridge.Core.Host`).

2. **`Scripting/AnalyzerProfile.cs`** (lines 8–73):
   - Identifies whether a script explicitly manages transactions (`TransactionTypeNames`, `TransactionMethodNames`).
   - For COM hosts without native script transactions (`Etabs`, `Sap2000`, `Excel`, `PowerBi`, and `Robot`): empty sets (`Array.Empty<string>()`).

3. **`Scripting/ScriptUnits.cs`** (lines 11–43):
   - Host-neutral length converter: `Label`, `MmPerUnit`, `ToDrawing(mm)`, `ToMm(drawing)`.
   - For Robot: standardized Metric length is Meter (`MmPerUnit = 1000.0`).

4. **`Model/BridgeSettingsStore.cs`** (lines 13–83):
   - Manages `%AppData%\{vendorFolder}\{productFolder}\settings.json`.
   - For Robot: `new BridgeSettingsStore("HPRobot", "McpBridge")`.

### 1.3 `HPRebar.Mcp.Server.Core`
Host-free server engine:

1. **`Hosts/IHostProfile.cs` & `HostProfile.cs`**:
   - Defines host identity: `HostId`, `DisplayName`, `ServerName`, `ProductFolder`, `EnvPrefix`, `DefaultVersion`, `ValidVersions`, `MethodPrefix`, `ExecuteToolName`, `ContextToolName`, `ResourceScheme`, `Categories`, `CoreToolNames`, `ScriptImports`, `ScriptContractSummary`, `HostAssembly`, `CliExecutable`, `MaxTimeoutSeconds`, `BridgeNotConnectedHint`, `TimeoutSemanticsHint`.
   - Fully data-driven; no runtime switching.

2. **`Bootstrap/McpServerHost.cs`**:
   - Stdio transport, stderr logging, DI service registration, dynamic MCP tool registry with SQLite catalog, stdio tool discovery.
   - `ConfigureOptions`: dynamically binds `BridgeOptions` with `profile.HostId` and `profile.DefaultVersion`. Requires no changes for new hosts!

3. **`Services/ContextService.cs`**:
   - Automatically removes Revit-only legacy fields (`revitVersion`, `isFamily`) for all non-Revit hosts. Completely host-neutral.

---

## 2. Comparative Analysis of Sibling COM Host Subsystems

The repository contains three primary COM host subsystems:
1. **`HPSap2000/`** — CSI SAP2000 v27 (Structural Analysis FEA COM host, out-of-process COM wrapper `SAP2000v1.dll`). **Closest architectural twin to HPRobot.**
2. **`HPEtabs/`** — CSI ETABS v22 (Structural Building FEA COM host, out-of-process COM wrapper `ETABSv1.dll`).
3. **`HPExcel/`** — Microsoft Excel (Office COM Interop + ClosedXML headless).

### 2.1 Architectural Comparison Matrix

| Aspect | `HPSap2000` (CSI) | `HPEtabs` (CSI) | `HPRobot` (Autodesk Robot 2026) |
|---|---|---|---|
| **Host Process** | `SAP2000.exe` (x64) | `ETABS.exe` (x64) | `robot.exe` (x64) |
| **API Wrapper** | `SAP2000v1.dll` | `ETABSv1.dll` | `Interop.RobotOM.dll` |
| **Wrapper Source** | Installed product (`$(Sap2000InstallDir)SAP2000v1.dll`) | Installed product (`$(EtabsInstallDir)ETABSv1.dll`) | Installed product (`$(RobotInstallDir)Exe\Interop.RobotOM.dll`) |
| **ProgID** | `CSI.SAP2000.API.SapObject` | `CSI.ETABS.API.ETABSObject` | `Robot.Application` |
| **CLSID** | `{B6B21850-FB75-41DE-85EC-BC9DBEC69BD3}` | `{4AD3A3E2-5CF3-46C4-B1F7-C6E2A346CF87}` | `{F7870790-CDE5-11D1-8FF1-00A02447BAAE}` |
| **Attachment Mode** | COM Out-of-Process (`Marshal.GetActiveObject`) | COM Out-of-Process (`Marshal.GetActiveObject`) | COM Out-of-Process (`Marshal.GetActiveObject`) |
| **Root Objects** | `cSapModel sapModel`, `cOAPI sap` | `cSapModel sapModel`, `cOAPI etabs` | `IRobotApplication robot`, `IRobotStructure structure` |
| **Model File Ext** | `.SDB` | `.EDB` | `.rtd` |
| **Bridge Process** | Standalone WPF App (`HPSap2000.McpBridge.exe`) | Standalone WPF App (`HPEtabs.McpBridge.exe`) | Standalone WPF App (`HPRobot.McpBridge.exe`) |
| **Bridge Framework** | .NET 8.0-windows, MaterialDesignThemes 5.3.2 | .NET 8.0-windows, MaterialDesignThemes 5.3.2 | .NET 8.0-windows, MaterialDesignThemes 5.3.2 |
| **Server Process** | .NET 10.0 Stdio Console (`HPSap2000.Mcp.Server.exe`) | .NET 10.0 Stdio Console (`HPEtabs.Mcp.Server.exe`) | .NET 10.0 Stdio Console (`HPRobot.Mcp.Server.exe`) |
| **Pipe Name** | `hpsap2000-mcp-27` | `hpetabs-mcp-22` | `hprobot-mcp-2026` |
| **Method Prefix** | `sap2000.` | `etabs.` | `robot.` |
| **Units Policy** | Forced `eUnits.kN_m_C` (m, kN, kN·m, °C) | Forced `eUnits.kN_mm_C` (mm, kN, kN·mm, °C) | Standardized Metric (m, kN, kN·m, MPa) |
| **Safety Tiers** | 3-Tier: R (Read), W (Write), D (Destructive) | 3-Tier: R (Read), W (Write), D (Destructive) | 3-Tier: R (Read), W (Write), D (Delete / Heavy) |
| **Heavy / Timeout** | 600 seconds | 600 seconds | 300 seconds |
| **Total Tools** | 24 (4 core + 8 registry + 12 seeds) | 24 (4 core + 8 registry + 12 seeds) | 24 (4 core + 8 registry + 12 seeds) |

### 2.2 COM Attachment Pattern (`RobotAttachment.cs`)
Following `SapAttachment.cs`:
1. Check running process: `Process.GetProcessesByName("robot")`.
2. Retrieve active COM object:
   ```csharp
   [DllImport("oleaut32.dll", PreserveSig = false)]
   private static extern void GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);
   ```
   Or `Marshal.GetActiveObject("Robot.Application")`.
3. Cast to `RobotApplication` / `IRobotApplication`.
4. Obtain `IRobotStructure` from `robot.Project.Structure`.
5. Hook `Process.Exited` event to immediately mark `_attached = false` when Robot closes, avoiding dead COM proxies.
6. Check `MainWindowHandle` quiescence (ensure no blocking modal dialog like Print/Export is open).

### 2.3 3-Tier Safety & Snapshot Pattern
Because Robot Structural Analysis COM scripts operate directly on the live structure without native transaction undo:
1. **Tier R (Read-Only)**:
   - Reads project properties, coordinates, nodes, bars, panels, load definitions, reaction tables, internal forces.
   - Script runs directly with `transaction: none`. No disk snapshot created.
2. **Tier W (Write)**:
   - Modifications: creating nodes (`Nodes.Create`), drawing bars (`Bars.Create`), assigning sections/materials (`Labels.Create`, `ApplyTo`), adding loads (`Loads.Add`).
   - Prior to execution: validates model is locally saved (`EnsureSnapshotable`), triggers `robot.Project.Save()`, and creates a timestamped copy:
     `.hprobot_snapshots/<ModelName>/prerun/<yyyyMMdd-HHmmss>-<label>.rtd`.
   - Pruning keeps the last 10 pre-run snapshots and 5 pre-save snapshots.
3. **Tier D (Delete / Heavy)**:
   - Destructive operations: `CalcEngine.Calculate()` (FEA solver run), `Structure.Clear()`, `Objects.Delete()`, `Project.New()`, `Project.Open()`.
   - Blocked with error `-32001` unless the user has actively checked `AllowHeavyOperations` in the `HPRobot.McpBridge` WPF window.

### 2.4 Metric Units Policy Pattern (`RobotUnitsPolicy.cs`)
Robot allows project unit configurations via `robot.Project.Preferences.Units`:
- `IRobotUnitType.I_UT_STRUCTURE_DIMENSION` = 1 (Meter)
- `IRobotUnitType.I_UT_FORCE` = 7 (kN)
- `IRobotUnitType.I_UT_MOMENT` = 8 (kN·m)
- `IRobotUnitType.I_UT_STRESS` = 9 (MPa)

During script execution:
1. Snapshot current user unit names via `Units.Get(type).Name`.
2. Set standardized units for length, force, moment, stress.
3. Pass `ScriptUnits("Meters", 1000.0, "Metric standardized (m, kN, kN·m, MPa)")` to the script globals.
4. In `finally`, restore original user units.

---

## 3. Exact Changes Required in `McpShared/`

To integrate Robot Structural Analysis Professional 2026 into `McpShared`, the following 6 files in `McpShared/` require changes:

### 3.1 `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs`
- Add constant (around line 50):
  ```csharp
  /// <summary>
  ///     Autodesk Robot Structural Analysis Professional; pipe <c>hprobot-mcp-{version}</c> (e.g. 2026).
  /// </summary>
  public const string RobotHost = "robot";
  ```
- Update `For(string host, int version)` switch expression (around line 73):
  ```csharp
  RobotHost => "hprobot-mcp-" + version,
  ```

### 3.2 `McpShared/HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs`
- Add constant (around line 43):
  ```csharp
  public const string RobotPrefix = "robot.";
  ```

### 3.3 `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs`
- Add script contracts (around line 186):
  ```csharp
  /// <summary>
  ///     Default `using`s of a Robot Structural Analysis script. The API is the COM interop wrapper
  ///     <c>Interop.RobotOM.dll</c> (namespace <c>RobotOM</c>: <c>RobotApplication</c>, <c>RobotStructure</c>, …).
  /// </summary>
  public static readonly string[] RobotImports =
  {
      "System", "System.Linq", "System.Collections.Generic",
      "RobotOM",
      "HPRebar.McpBridge.Core.Scripting",
  };

  /// <summary>
  ///     Global names a Robot Structural Analysis script may use: `robot` is the attached <c>RobotApplication</c>,
  ///     `structure` the active <c>RobotStructure</c>, `units` reports the unit system the bridge standardizes
  ///     (Meter for length, kN for force, kN·m for moment, MPa for stress).
  /// </summary>
  public static readonly string[] RobotGlobals = { "robot", "structure", "units", "ct", "log", "progress", "args" };

  /// <summary>
  ///     Longest Robot Structural Analysis run once the user allowed heavy operations (e.g. structural FEA calculations
  ///     via CalcEngine.Calculate() or batch object deletions). The bridge clamps to it and the server profile advertises it.
  /// </summary>
  public const int RobotHeavyMaxTimeoutSeconds = 300;
  ```

### 3.4 `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs`
- Add property to `ContextResult` (around line 43):
  ```csharp
  /// <summary>Robot Structural Analysis-only facts; null for the other hosts (and omitted from the JSON).</summary>
  public RobotInfo? Robot { get; set; }
  ```
- Define `RobotInfo` record (around line 201):
  ```csharp
  /// <summary>
  ///     What a Robot Structural Analysis script needs to know that has no counterpart elsewhere. The bridge is a
  ///     separate desktop app attached over COM to Robot Structural Analysis Professional: whether attached,
  ///     attached PID, Robot version, project structure type, whether model is calculated,
  ///     heavy operations enabled, and coarse structural object counts (nodes, bars, panels, load cases).
  /// </summary>
  public sealed record RobotInfo(
      bool IsAttached,
      int AttachedPid,
      string? RobotVersion,
      string? StructureType,
      bool IsCalculated,
      bool HeavyOperationsEnabled,
      int NodeCount,
      int BarCount,
      int PanelCount,
      int LoadCaseCount);
  ```

### 3.5 `McpShared/HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`
- Add `GuardProfile.Robot` (around line 140):
  ```csharp
  /// <summary>
  ///     Robot Structural Analysis Professional (out-of-process COM through <c>Interop.RobotOM.dll</c>; the bridge is
  ///     a separate desktop app). Denied: Application Quit/Exit, modal UI, and bridge internals.
  ///     Base list already denies reflection, file I/O, processes, threads and memory marshalling.
  /// </summary>
  public static readonly GuardProfile Robot = new GuardProfile(
      "Robot Structural Analysis",
      deniedIdentifiers: new[] { "MessageBox" },
      deniedMembers: new[] { "Quit", "ApplicationExit" },
      deniedMembersOnIdentifier: new Dictionary<string, string[]>(StringComparer.Ordinal)
      {
          ["robot"] = new[] { "Quit" },
          ["app"] = new[] { "Quit" },
      },
      deniedNamespaces: new[] { "System.Windows.Forms", "HPRobot.McpBridge", "HPRebar.McpBridge.Core.Host" });
  ```

### 3.6 `McpShared/HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs`
- Add `AnalyzerProfile.Robot` (around line 47):
  ```csharp
  /// <summary>Robot Structural Analysis has no transaction of any kind in scripts, so no script can ever "manage" one; the profile exists so the Robot bridge never borrows Revit's.</summary>
  public static readonly AnalyzerProfile Robot = new AnalyzerProfile(
      transactionTypeNames: Array.Empty<string>(),
      transactionMethodNames: Array.Empty<string>());
  ```

### 3.7 Adaptations in `McpServerHost.ConfigureOptions` and `ContextService`
- **`McpServerHost.ConfigureOptions`**: Requires **zero code changes**. It dynamically binds options using `profile.HostId`, `profile.DefaultVersion`, and `profile.ValidVersions`.
- **`ContextService`**: Requires **zero code changes**. Non-Revit hosts automatically drop Revit-specific fields (`revitVersion`, `isFamily`), and `BridgeJson` ignores null properties, guaranteeing that `context.Robot` is emitted only for Robot and never leaks into other hosts.

---

## 4. Test Suite Baseline and Regression Defense

### 4.1 Measured Baseline
The test commands were run directly on the local machine:
```powershell
dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests
# Output: total: 385, failed: 0, succeeded: 385, skipped: 0

dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests
# Output: total: 71, failed: 0, succeeded: 71, skipped: 0
```

### 4.2 New Tests to Add in `HPRebar.Mcp.Server.Core.Tests`
To lock in the Robot integration and maintain test parity with SAP2000, ETABS, and Excel:

1. **`RobotTestProfile.cs`**:
   - Implements `HostProfile` fixture with `HostId = "robot"`, `MethodPrefix = "robot."`, `DefaultVersion = 2026`, `MaxTimeoutSeconds = 300`, and candidate tool records.

2. **`RobotProfileTests.cs`**:
   - `Robot_constants_produce_the_pipe_prefix_imports_and_globals`:
     Verifies pipe `hprobot-mcp-2026`, prefix `robot.`, imports contain `RobotOM`, globals contain `["robot", "structure", "units", "ct", "log", "progress", "args"]`, timeout ceiling is `300`.
   - `Robot_guard_profile_denies_lifecycle_bridge_internals_and_the_base_list`:
     Verifies guard rejects `robot.Quit()`, `MessageBox.Show()`, `HPRobot.McpBridge`, `System.IO.File`.
   - `Global_alias_does_not_bypass_the_robot_bridge_namespace_denial`:
     Verifies `global::HPRobot.McpBridge` is caught.
   - `Robot_guard_profile_lets_reads_writes_and_analysis_through`:
     Verifies valid RobotOM code passes guard.
   - `Robot_analyzer_profile_never_reports_a_transaction`:
     Verifies transactions are never reported.
   - `Validator_ceiling_follows_the_robot_profile`:
     Verifies tool validator enforces 300s upper limit.
   - `ConfigureOptions_seeds_HostVersion_from_the_profile`:
     Verifies options binding defaults to 2026.
   - `Wire_additions_are_invisible_when_unused`:
     Verifies serialized `ContextResult` without `Robot` does not contain `"robot"`.

3. **`ExcelMilestone1Challenger2Tests.cs` update**:
   - Add `PipeNaming.RobotHost` to `PipeNaming_host_constants_are_all_mutually_distinct`.
   - Add `JsonRpcMethods.RobotPrefix` to `JsonRpc_prefixes_are_all_distinct_and_end_with_period`.

---

## 5. Proposed HPRobot Solution & Project Layout

The complete solution `HPRobot/HPRobot.slnx` will mirror `HPSap2000`:

```
HPRobot/
├── HPRobot.slnx
├── Directory.Build.props            # Detects Robot 2026 install path & Interop.RobotOM.dll
├── HPRobot.McpBridge/               # Standalone WPF Desktop Bridge (net8.0-windows)
│   ├── App.xaml / App.xaml.cs
│   ├── BridgeEntry.cs               # Starts listener on hprobot-mcp-2026
│   ├── RobotExecutor.cs             # Implements IBridgeExecutor
│   ├── Model/
│   │   ├── RobotScriptGlobals.cs    # robot, structure, units, ct, log, progress, args
│   │   └── RobotScriptEnvironment.cs
│   ├── Service/
│   │   ├── RobotAttachment.cs       # Out-of-process COM to Robot.Application
│   │   ├── RobotContextReader.cs    # Extracts RobotInfo, structure counts, open RTD
│   │   ├── RobotTierAnalyzer.cs     # 3-tier Roslyn semantic analyzer
│   │   ├── RobotTierTable.cs        # Member classification table
│   │   ├── RobotSnapshotManager.cs  # .rtd pre-run & pre-save backup manager
│   │   ├── RobotUnitsPolicy.cs      # Metric standardization (m, kN, kN·m, MPa)
│   │   └── RobotAssemblyResolver.cs # Resolves Interop.RobotOM.dll at runtime
│   ├── View/MainWindow.xaml         # MaterialDesignThemes 5.3.2 Dark/Light UI
│   └── ViewModel/MainViewModel.cs   # Status, logs, AllowExecution & AllowHeavyOperations toggles
├── HPRobot.Mcp.Server/              # Stdio MCP Server Console (net10.0)
│   ├── Program.cs                   # 9 lines: McpServerHost.RunAsync(args, RobotHostProfile.Instance)
│   ├── Hosts/RobotHostProfile.cs    # IHostProfile implementation for Robot
│   ├── Tools/                       # execute_robot_code, get_robot_context
│   ├── Prompts/                     # Robot Roslyn C# scripting guidance
│   ├── Resources/                   # robot://document/info resources
│   └── Registry/SeedLibrary/        # 12 embedded seed tools
├── HPRobot.McpBridge.Tests/         # xUnit tests for Bridge (net8.0-windows)
│   ├── RobotTierAnalyzerTests.cs
│   ├── RobotSnapshotManagerTests.cs
│   ├── RobotUnitsPolicyTests.cs
│   └── RobotExecutorRefusalTests.cs
├── HPRobot.Mcp.Server.Tests/        # xUnit tests for Server (net10.0)
│   ├── RobotHostProfileTests.cs
│   ├── RobotToolsOverPipeTests.cs
│   ├── SeedLibraryStructureTests.cs
│   └── SeedLibraryCompileTests.cs   # Compiles 12 seeds against Interop.RobotOM.dll
└── tools/
    └── harness/                     # Unattended verification scripts
        ├── run-live-verify.ps1
        └── live-verify.py           # Inherits McpShared/tools/harness_common.py
```

---

## 6. Implementation Readiness & Sequence

1. **Phase 1: McpShared Host Integration**
   - Apply the exact changes to the 6 files in `McpShared/`.
   - Add `RobotTestProfile.cs` and `RobotProfileTests.cs` to `HPRebar.Mcp.Server.Core.Tests`.
   - Run `dotnet test` on both test projects; verify all 385 + 71 + new tests pass 100%.

2. **Phase 2: HPRobot Solution Scaffolding & Bridge Core**
   - Scaffold `HPRobot.slnx` and `Directory.Build.props`.
   - Implement `HPRobot.McpBridge` with `RobotAttachment`, `RobotUnitsPolicy`, `RobotTierAnalyzer`, and `RobotSnapshotManager`.
   - Implement `HPRobot.McpBridge.Tests`.

3. **Phase 3: MCP Server & 24 Tools Catalog**
   - Implement `HPRobot.Mcp.Server` with `RobotHostProfile`.
   - Implement 4 Core Tools + 8 Meta Tools + 12 Embedded Seed Tools.
   - Implement `HPRobot.Mcp.Server.Tests` with seed compile verification.

4. **Phase 4: Live Verification Harness & Documentation**
   - Create `tools/harness/` verification scripts.
   - Register skill `hp-mcp-robot` and update `AGENTS.md`.

---
*Report complete and verified against live codebase.*
