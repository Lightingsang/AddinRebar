# Handoff Report: HPTekla MCP Server, 24 Tools Catalog & Test Suites Design

**Agent**: `teamwork_preview_explorer_survey_3`  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_3`  
**Date**: 2026-09-22  
**Recipient**: Orchestrator (`parent`, ID: `5d7560ee-5142-428f-a172-e73cf7738ac1`)

---

## 1. Observation

1. **Repository Layout & Standards (`AGENTS.md`)**:
   - The repository hosts nine CAD/BIM MCP deliverables (`HPRebar`, `HPAutoCad`, `HPNavis`, `HPEtabs`, `HPCivil3d`, `HPSap2000`, `HPPowerBi`, `HPExcel`, `HPRobot`) plus one shared library folder `McpShared/`.
   - Dependency direction is strictly one-way: Host MCP projects (`HPTekla/`) depend only on `McpShared/`. Cross-referencing other host projects is prohibited.
2. **Existing MCP Server Implementations**:
   - `HPRobot.Mcp.Server/Program.cs`: Runs in 8 lines via `return await McpServerHost.RunAsync(args, RobotHostProfile.Instance);`.
   - `HPNavis.Mcp.Server/Hosts/NavisHostProfile.cs`: Demonstrates `net48` bridge compatibility with a .NET 10 console server, where `IHostProfile` defines pipe naming (`hpnavis-mcp-2026`), method prefixes (`navis.`), categories, and script contracts.
   - `HPRebar.Mcp.Server.Core/Bootstrap/McpServerHost.cs` lines 76–83: Automatically binds 4 Core Tools and 8 Registry Meta Tools from two assemblies (`engine` and `profile.HostAssembly`).
   - `HPRobot.Mcp.Server.csproj` lines 41–42: Demonstrates removing `Registry\SeedLibrary\**\*.cs` from `<Compile>` and bundling them as embedded resources via `<EmbeddedResource Include="Registry\SeedLibrary\**\*" LogicalName="SeedLibrary/%(RecursiveDir)%(Filename)%(Extension)" />`.
3. **Tekla Structures 2025.0 Runtime Environment**:
   - Tekla Structures 2025.0 runs on Microsoft .NET Framework 4.8 (CLR v4.0.30319) with primary Open API assemblies located at `C:\Program Files\Tekla Structures\2025.0\bin` (`Tekla.Structures.dll`, `Tekla.Structures.Model.dll`, `Tekla.Structures.Catalogs.dll`, `Tekla.Structures.Drawing.dll`).
   - Tekla Open API is stateful and transactional via `model.CommitChanges()`. In-memory object creation (`Insert()`, `Modify()`) does not persist until committed, enabling clean `dryRun = true` rollback emulation by suppressing `model.CommitChanges()`.
4. **Existing Test Suite Architecture**:
   - `HPNavis.McpBridge.Tests` compiles against `net48` and tests the `net48` asset of `HPRebar.McpBridge.Core`.
   - `HPRobot.Mcp.Server.Tests` (`SeedCatalogTests.cs`, `SeedCompilationTests.cs`, `SeedExecutionTests.cs`) verifies 12 embedded seeds, schema compatibility, and Roslyn compilation against CAD APIs, gracefully skipping via `Assert.SkipWhen` when CAD software is not installed on the dev machine.
   - `McpShared/tools/harness_common.py` provides shared test bookkeeping (`Checklist`, `Server`, `parse_tool_result`, `ok`, `short`) for Python live verification harnesses across all host implementations.

---

## 2. Logic Chain

1. **Host-Neutral Architecture**:
   - Because `McpShared` encapsulates the MCP server bootstrap, named pipe communication, dynamic tool registrar, and SQLite database, `HPTekla.Mcp.Server` can be implemented as a clean .NET 10 console application without referencing any Tekla Open API binaries.
   - The connection to Tekla Structures 2025 occurs over Named Pipe `hptekla-mcp-2025` using the wire prefix `tekla.`.
2. **24 Tools Catalog Composition**:
   - `McpServerHost.CreateBuilder` automatically registers 4 Core Tools (`execute_tekla_code`, `get_tekla_context` from `HPTekla.Mcp.Server`; `inspect_type`, `cancel_execution` from `McpShared`) and 8 Registry Meta Tools (`search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`).
   - The 12 embedded seed tools cover core steel and reinforced concrete workflows:
     - Model: `get_model_info`, `select_objects`
     - Property: `get_part_properties`, `modify_user_properties`
     - Geometry: `create_beam`, `create_column`, `create_contour_plate`
     - Rebar: `create_rebar_group`, `create_single_rebar`, `get_reinforcement_info`
     - Drawing: `list_drawings`
     - Export: `export_ifc`
   - Combining 4 Core + 8 Registry Meta + 12 Published Seeds yields exactly **24 tools** in MCP `tools/list`.
3. **Safety and Transaction Control**:
   - In Tekla Open API, geometry modifications (`Insert`, `Modify`, `Delete`) exist in a local transaction buffer until `model.CommitChanges()` is called.
   - By denying `model.CommitChanges()` in `GuardProfile.Tekla` and having `HPTekla.McpBridge` conditionally call `model.CommitChanges()` only when `dryRun == false` and `transaction == "auto"`, write safety is guaranteed.
   - Heavy operations (`export_ifc`) are gated behind the bridge's `AllowHeavyOperations` toggle.
4. **Test & Live Verification Harness**:
   - `HPTekla.Mcp.Server.Tests` (.NET 10) validates profile invariants, tool schema completeness, Roslyn compilation of all 12 seeds, and simulated pipe execution via `FakeRevitExecutor`.
   - `HPTekla.McpBridge.Tests` (.NET Framework 4.8) validates the script guard, tier analyzer, and dry-run suppression on the .NET Framework CLR.
   - `HPTekla/tools/harness/live-verify.py` leverages `McpShared/tools/harness_common.py` to test end-to-end communication with an active Tekla Structures 2025 process.

---

## 3. Caveats

1. **Tekla Structures Installation Requirement for Live Testing**:
   - Unit tests (`HPTekla.Mcp.Server.Tests`) use `Assert.SkipWhen` to gracefully skip Roslyn compilation against Tekla Open API binaries if `Tekla.Structures.dll` is not found at `C:\Program Files\Tekla Structures\2025.0\bin`.
   - Live verification (`tools/harness/run-live-verify.ps1`) requires Tekla Structures 2025.0 to be running with an open model and `HPTekla.McpBridge` loaded.
2. **Drawing API Modality**:
   - `DrawingHandler.GetDrawings()` operates on the current drawing database. Drawing extraction requires that the model contains generated drawings; otherwise, `list_drawings` returns an empty array with count 0 (handled gracefully).
3. **No Direct Modifications Made in this Turn**:
   - In accordance with the Explorer archetype rules, this investigation is read-only. No project source files or McpShared files were modified. All designs and specifications are documented in `report.md`.

---

## 4. Conclusion

The architectural survey and complete technical specification for `HPTekla.Mcp.Server`, its 24 tools catalog, automated test suites, and live verification harness are fully designed and documented in:
`g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_3\report.md`

Key Deliverable Blueprint:
1. **McpShared Additive Layer**: Adds `PipeNaming.TeklaHost = "tekla"`, `JsonRpcMethods.TeklaPrefix = "tekla."`, `HostScriptContracts.TeklaImports`, `ContextResult.Tekla` (`TeklaInfo`), `GuardProfile.Tekla`, `AnalyzerProfile.Tekla`.
2. **`HPTekla.Mcp.Server` (.NET 10)**: Exposes 24 tools, registers `TeklaHostProfile`, connects over pipe `hptekla-mcp-2025`.
3. **12 Embedded Seeds**: Complete C# script implementations, schemas, and example JSONs designed for steel framing, concrete plates, reinforcement groups, single rebars, UDAs, drawing listing, and IFC export.
4. **Test Suites**: Comprehensive design for `HPTekla.Mcp.Server.Tests` (net10) and `HPTekla.McpBridge.Tests` (net48).
5. **Live Verification Harness**: Python harness in `HPTekla/tools/harness/` consuming `McpShared/tools/harness_common.py`.

---

## 5. Verification Method

To independently verify the findings and design artifacts:
1. **Inspect Report & Handoff**:
   - View `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_explorer_survey_3\report.md` to review the complete code blueprints, schemas, and test designs.
   - Check lines in `report.md` sections 3, 4, 5, and 6.
2. **Verify McpShared Baseline Tests**:
   - Run from repo root:
     ```powershell
     cd "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\McpShared"
     dotnet test HPRebar.Mcp.Server.Core.Tests
     dotnet test HPRebar.McpBridge.Core.Net48Tests
     ```
   - Confirms baseline of 164 server core tests and 62 net48 tests are currently 100% passing.
3. **Cross-Check Reference Implementations**:
   - Compare with `HPRobot/HPRobot.Mcp.Server/Hosts/Robot/RobotHostProfile.cs`
   - Compare with `HPNavis/HPNavis.Mcp.Server/Hosts/NavisHostProfile.cs`
   - Compare with `HPRobot/HPRobot.Mcp.Server.Tests/SeedCatalogTests.cs`
