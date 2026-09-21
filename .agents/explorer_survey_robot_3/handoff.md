# Handoff Report — HPRobot Tool Catalog and Safety Engineer (Explorer 3)

**Agent**: Explorer 3 (Tool Catalog and Safety Engineer)  
**Parent**: Project Orchestrator (`orchestrator_7`, conversation ID: `b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_3\`  
**Date**: 2026-09-21T13:25:00Z  

---

## 1. Observation

1. **Host Environment & COM Wrapper**:
   - Autodesk Robot Structural Analysis Professional 2026 is installed on the dev machine.
   - Exact assembly path verified: `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll`.
   - Assembly inspection via reflection confirmed 3,085 types in namespace `RobotOM`.
   - Primary COM interfaces identified:
     - `IRobotApplication` (prog ID `"Robot.Application"`, `Visible`, `Project`).
     - `IRobotProject` (`FileName`, `Type`, `Structure`, `CalcEngine`, `Preferences`, `AxisMngr`).
     - `IRobotStructure` (`Nodes`, `Bars`, `Objects`, `Cases`, `Labels`, `Results`).
     - `IRobotCalcEngine` (`Calculate()`, `GenerateModel()`, `AutoGenerateModel`).
     - `IRobotNodeServer` (`Create(num, x, y, z)`, `FindXYZ(x, y, z)`, `FreeNumber`, `SetLabel(...)`).
     - `IRobotBarServer` (`Create(num, startNode, endNode)`, `FreeNumber`, `SetLabel(...)`).
     - `IRobotResultServer` (`Nodes.Reactions.Value(node, case)`, `Bars.Forces.Value(bar, case, pt)`).
     - `IRobotUnitMngr` (`UseMetricAsDefault`, `Get(unitType)`, `Set(unitType, data)`).
2. **Prior Art Architecture in Repo**:
   - `HPSap2000` (`HPSap2000/HPSap2000.slnx`), `HPEtabs` (`HPEtabs/HPEtabs.slnx`), `HPExcel` (`HPExcel/HPExcel.slnx`), and `HPNavis` (`HPNavis/HPNavis.slnx`) establish the pattern for out-of-process COM / native host MCP bridges.
   - Shared engine `McpShared/` contains host-neutral interfaces (`IHostProfile`, `IBridgeExecutor`, `RequestDispatcher`, `PipeListener`, `McpServerHost`).
   - Host isolation is strictly enforced: `HPRobot/` references `../McpShared/` only.
3. **Repository Build Configuration**:
   - `global.json` pins .NET SDK `10.0.300` and Microsoft Testing Platform runner.
   - McpShared test suite has 164 tests in `HPRebar.Mcp.Server.Core.Tests` (.NET 10) and 62 tests in `HPRebar.McpBridge.Core.Net48Tests` (.NET 4.8).

---

## 2. Logic Chain

1. **Out-of-Process COM Architecture**:
   - Robot Structural Analysis Professional 2026 provides a full COM automation object model (`RobotOM`) through `Interop.RobotOM.dll`.
   - Like SAP2000 and ETABS, Robot requires no in-process add-in inside `robot.exe` to expose full API access.
   - Therefore, `HPRobot.McpBridge` must be designed as a standalone WPF desktop application (.NET 8.0-windows) running beside Robot, holding the COM connection, Named Pipe listener (`hprobot-mcp-2026`), snapshot manager, and safety checkboxes (`AllowExecution`, `AllowHeavyOperations`).
2. **24-Tool Catalog Design**:
   - Standard HP MCP surface requires 24 tools:
     - 4 Core Tools: `execute_robot_code`, `get_robot_context`, `robot://` resources, Prompts.
     - 8 Registry Meta Tools: Inherited from `HPRebar.Mcp.Server.Core`.
     - 12 Embedded Seed Tools: Distributed across `Model`, `Geometry`, `Property`, `Load`, `Analysis`, and `Results`.
   - Every seed tool requires a full `tool.json` schema, production-ready `code.cs` Roslyn script, and test inputs `examples.json`.
3. **3-Tier Safety & RTD Snapshot Engine**:
   - Robot has no transaction rollback mechanism for COM scripts; element deletions and modifications are immediate.
   - Consequently, pre-execution Roslyn semantic analysis (`RobotTierAnalyzer`) is required:
     - Tier R (Read-Only): `transaction: "none"`, zero snapshot overhead.
     - Tier W (Write): `transaction: "auto"`, requires `AllowExecution` toggle; creates timestamped `.rtd` snapshot into `%LocalAppData%\HPRobot\McpBridge\snapshots\<model>\prerun\` before execution.
     - Tier D / Heavy (Destructive): Element deletions, file operations, and solver execution (`Calculate()`); requires explicit `AllowHeavyOperations` toggle, otherwise rejected with -32001.
4. **Metric Standardizer (`RobotUnitsPolicy`)**:
   - Structural FEA requires strict unit consistency.
   - `RobotUnitsPolicy` forces metric units (`m`, `kN`, `kN·m`, `MPa`) via `robot.Project.Preferences.Units` for the duration of the run and restores user preferences in `finally`.
5. **Test Architecture**:
   - `HPRobot.Mcp.Server.Tests` (.NET 10 xUnit) verifies tool metadata, schema validation, pipe round-trips with `FakeRevitExecutor`, and compiles all 12 seed codes in-memory against `Interop.RobotOM.dll`.
   - `HPRobot.McpBridge.Tests` (.NET 8.0-windows xUnit) validates AST tier classification, snapshot retention, path policies, and refusal handling.
   - `tools/harness/run-live-verify.ps1` and `live-verify.py` inherit `McpShared/tools/harness_common.py` for automated end-to-end verification against live Robot 2026.

---

## 3. Caveats

1. **Active Document Prerequisite for Snapshots**:
   Writing scripts require an already-saved `.rtd` model on local disk. New, unsaved models (`project.FileName == ""`) or network UNC paths are refused with `BridgeErrorCode.NoActiveDocument` because the snapshot engine cannot copy an unsaved file.
2. **Solver Execution Time**:
   Complex structural models with large meshes may exceed normal timeouts during `run_calculations`. The maximum timeout ceiling for Robot is therefore set to 300 seconds (`HostScriptContracts.RobotHeavyMaxTimeoutSeconds = 300`).
3. **Machine Dependency for Seeds Compilation**:
   Seed compilation tests in `HPRobot.Mcp.Server.Tests` require `Interop.RobotOM.dll` to be present on the build machine. If missing, tests will skip visibly (`Assert.SkipWhen(...)`), preserving CI portability.

---

## 4. Conclusion

The complete 24-tool catalog, 3-tier safety system (`RobotTierAnalyzer`), RTD snapshot manager, unit standardizer (`RobotUnitsPolicy`), test suite architecture, and live harness for the HPRobot MCP subsystem have been designed and documented in detail in:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_3\analysis.md`.

All 12 seed tools have been fully specified with production-grade C# Roslyn scripts and JSON schemas mapped directly to verified `RobotOM` COM API types.

---

## 5. Verification Method

1. **Verify Documentation & Specification**:
   - Inspect `analysis.md` for complete schemas and scripts:
     `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_survey_robot_3\analysis.md`
2. **Verify COM Assembly Presence**:
   ```powershell
   Test-Path "C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll"
   ```
3. **Verify McpShared Baselines**:
   ```bash
   dotnet test McpShared/HPRebar.Mcp.Server.Core.Tests
   dotnet test McpShared/HPRebar.McpBridge.Core.Net48Tests
   ```
4. **Post-Implementation Verification (by implementer)**:
   ```bash
   dotnet build HPRobot/HPRobot.slnx
   dotnet test HPRobot/HPRobot.Mcp.Server.Tests
   dotnet test HPRobot/HPRobot.McpBridge.Tests
   powershell -File HPRobot/tools/harness/run-live-verify.ps1
   ```
