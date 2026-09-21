# Handoff Report — HPRobot McpShared Architecture & Sibling Patterns

**Agent:** Explorer 1 (`explorer_survey_robot_1`)  
**Parent:** Project Orchestrator (`orchestrator_7`, conversation ID: `b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Date:** 2026-09-21  
**Status:** Hard Handoff (Investigation Complete)

---

## 1. Observation

### 1.1 McpShared Live Test Baseline
Ran `dotnet test` directly on the local machine:
- Command: `dotnet test McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj`
  Result:
  ```
  Running tests from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Server.Core.Tests\bin\Debug\net10.0\HPRebar.Mcp.Server.Core.Tests.dll (net10.0|x64)
  Test run summary: Passed!
    total: 385
    failed: 0
    succeeded: 385
    skipped: 0
    duration: 4s 139ms
  ```
- Command: `dotnet test McpShared\HPRebar.McpBridge.Core.Net48Tests\HPRebar.McpBridge.Core.Net48Tests.csproj`
  Result:
  ```
  Running tests from G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.McpBridge.Core.Net48Tests\bin\Debug\net48\HPRebar.McpBridge.Core.Net48Tests.exe (net48|x64)
  Test run summary: Passed!
    total: 71
    failed: 0
    succeeded: 71
    skipped: 0
    duration: 3s 454ms
  ```
- Total baseline: **456 tests passed, 0 failed, 0 skipped**.

### 1.2 Robot Structural Analysis Professional 2026 Environment on Host
- Path inspection:
  `Test-Path "C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll"` returned `True`.
- COM Registry inspection:
  `HKCR\Robot.Application\CLSID` returned `{F7870790-CDE5-11D1-8FF1-00A02447BAAE}`.
  `HKCR\CLSID\{F7870790-CDE5-11D1-8FF1-00A02447BAAE}\LocalServer32` returned `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\robot.exe`.
- Assembly reflection:
  Assembly identity: `Interop.RobotOM, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null`.
  Key types:
  - Application: `RobotOM.RobotApplication`, `RobotOM.IRobotApplication`.
  - Structure: `RobotOM.RobotStructure`, `RobotOM.IRobotStructure`.
  - Units: `RobotOM.IRobotUnitMngr`, `RobotOM.IRobotUnitData`, enum `RobotOM.IRobotUnitType` (`I_UT_STRUCTURE_DIMENSION = 1`, `I_UT_FORCE = 7`, `I_UT_MOMENT = 8`, `I_UT_STRESS = 9`).
  - Preferences: `RobotOM.IRobotProjectPreferences.Units`.
  - CalcEngine: `RobotOM.IRobotCalcEngine.Calculate()`.
  - Results: `RobotOM.IRobotResultServer.Available`.

### 1.3 McpShared Integration Touchpoints
Directly inspected in `McpShared/`:
1. `HPRebar.Mcp.Contracts/PipeNaming.cs`: Lines 10–76. Contains `RevitHost`, `AutocadHost`, `NavisHost`, `EtabsHost`, `Civil3dHost`, `Sap2000Host`, `PowerBiHost`, `ExcelHost`.
2. `HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs`: Lines 35–43. Contains host prefixes `revit.`, `autocad.`, `navis.`, `etabs.`, `civil3d.`, `sap2000.`, `powerbi.`, `excel.`.
3. `HPRebar.Mcp.Contracts/HostScriptContracts.cs`: Lines 122–143 (`Sap2000Imports`, `Sap2000Globals`, `Sap2000HeavyMaxTimeoutSeconds`).
4. `HPRebar.Mcp.Contracts/Messages/ContextMessages.cs`: Lines 12–43 (`ContextResult` host properties: `Autocad`, `Navis`, `Etabs`, `Civil3d`, `Sap2000`, `PowerBi`, `Excel`).
5. `HPRebar.McpBridge.Core/Scripting/GuardProfile.cs`: Lines 98–105 (`GuardProfile.Sap2000`).
6. `HPRebar.McpBridge.Core/Scripting/AnalyzerProfile.cs`: Lines 32–36 (`AnalyzerProfile.Sap2000`).
7. `HPRebar.Mcp.Server.Core/Bootstrap/McpServerHost.cs`: Lines 98–111 (`ConfigureOptions` is fully dynamic and needs no host-specific code).
8. `HPRebar.Mcp.Server.Core/Services/ContextService.cs`: Lines 48–59 (drops Revit-only fields for all non-Revit hosts; completely host-neutral).

### 1.4 Sibling Subsystem Architecture
Examined `HPSap2000/` and `HPEtabs/`:
- `Directory.Build.props`: Resolves API install directory via registry `LocalServer32`, sets `<Sap2000ApiAvailable>` condition.
- `HPSap2000.McpBridge`: Standalone WPF app (.NET 8.0-windows) with `MaterialDesignThemes` 5.3.2, connects out-of-process via COM, implements `SapAttachment`, `SapUnitsPolicy` (forces metric units, restores in finally), `SapTierAnalyzer` (Read, Write with `.SDB` snapshot, Destructive gated by UI toggle), `SapSnapshotManager`.
- `HPSap2000.Mcp.Server`: Console stdio (.NET 10.0), 9-line `Program.cs`, 24 tools (4 core + 8 meta + 12 embedded seeds).
- `HPSap2000.Mcp.Server.Tests`: Tests 24 tools registration and compiles 12 seeds against `SAP2000v1.dll`.

---

## 2. Logic Chain

1. **Host-Neutral Extensibility**:
   - `McpShared` was designed to accommodate new hosts without modifying core pipeline orchestration (`McpServerHost`, `RequestDispatcher`, `ContextService`, `ToolLifecycleService`).
   - Every host is fully defined through contracts (`PipeNaming`, `JsonRpcMethods`, `HostScriptContracts`, `ContextResult`), guard/analyzer profiles (`GuardProfile`, `AnalyzerProfile`), and server profile (`IHostProfile`).
   - Therefore, adding Robot Structural Analysis requires only additive entries in these 6 exact files in `McpShared/`.

2. **COM Host Sibling Pattern Fidelity**:
   - Robot Structural Analysis Professional 2026 is an out-of-process COM host (`robot.exe` and `Interop.RobotOM.dll`), identical in runtime topology to CSI SAP2000 (`SAP2000.exe` and `SAP2000v1.dll`) and CSI ETABS (`ETABS.exe` and `ETABSv1.dll`).
   - Like SAP2000 and ETABS, Robot scripts have no native database transaction rollback in COM.
   - Therefore, safety must be guaranteed via:
     - 3-Tier semantic AST analysis (Read / Write / Delete-Heavy).
     - Automated pre-run model snapshots (`.rtd` file backup).
     - UI opt-in checkbox (`AllowHeavyOperations`) for destructive actions (`Calculate()`, `Clear()`).
     - Standardized Metric units policy (Meter, kN, kN·m, MPa) during script execution with state restoration in `finally`.

3. **Zero Regression Assurance**:
   - `BridgeJson` serializes with `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`. When `context.Robot` is null, no `"robot"` property is emitted for any existing host.
   - All existing host profile tests (`Revit`, `Autocad`, `Navis`, `Etabs`, `Civil3d`, `Sap2000`, `PowerBi`, `Excel`) continue to execute against their dedicated profiles.
   - Adding `RobotProfileTests` in `McpShared/HPRebar.Mcp.Server.Core.Tests` will lock in Robot behavior without altering any existing test expectations.

---

## 3. Caveats

1. **No Live Robot Process Running During Test Execution**:
   - Robot Structural Analysis Professional 2026 is installed on disk, but was not actively running during this investigation. Local unit tests and seed compile tests execute against `Interop.RobotOM.dll` metadata without requiring a live instance. Live end-to-end execution will be validated in later phases via `tools/harness/run-live-verify.ps1`.
2. **Dynamic UI COM Quiescence**:
   - Unlike AutoCAD which provides `Editor.IsQuiescent`, Robot COM exposes window handles and modal dialog states. Quiescence checks should monitor `MainWindowHandle` and ensure no modal calculation/export dialog is blocking the main thread.

---

## 4. Conclusion

1. The exact set of modifications for `McpShared/` has been fully identified across 6 files (`PipeNaming.cs`, `JsonRpcMethods.cs`, `HostScriptContracts.cs`, `ContextMessages.cs`, `GuardProfile.cs`, `AnalyzerProfile.cs`).
2. `McpServerHost.ConfigureOptions` and `ContextService` require **zero modifications**.
3. `HPSap2000` provides an exact 1:1 blueprint for `HPRobot`:
   - `HPRobot.McpBridge` (.NET 8.0-windows WPF) connecting to `Robot.Application` via COM.
   - `HPRobot.Mcp.Server` (.NET 10.0 console stdio) serving 24 tools.
   - 3-tier safety with `.rtd` snapshots and Metric units policy.
4. Baseline test stability is confirmed at **456/456 tests passing** (385 in `Server.Core.Tests` + 71 in `Net48Tests`).

Detailed investigation report is available in:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_1\analysis.md`

---

## 5. Verification Method

To independently verify all findings:
1. **Run McpShared Baseline Tests**:
   ```powershell
   dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.Mcp.Server.Core.Tests\HPRebar.Mcp.Server.Core.Tests.csproj"
   dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared\HPRebar.McpBridge.Core.Net48Tests\HPRebar.McpBridge.Core.Net48Tests.csproj"
   ```
   Both must pass with 0 failures.
2. **Verify Robot 2026 COM Assembly**:
   ```powershell
   Test-Path "C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll"
   ```
   Must return `True`.
3. **Verify Sibling COM Architecture**:
   Inspect `HPSap2000/HPSap2000.McpBridge/HPSap2000.McpBridge.csproj` and `HPSap2000/Directory.Build.props` to confirm COM referencing and 3-tier safety structures.
