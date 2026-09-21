# Handoff Report: HPRobot MCP Server & Tool Registry Test Design

**Agent**: `explorer_m4_2`  
**Role**: Server Core & Tool Registry Test Design Explorer  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\explorer_m4_2\`  
**Target Milestone**: Milestone 4 — Automated Test Suites Design  
**Date**: 2026-09-21  

---

## 1. Observation

### 1.1 McpShared Integration & Robot Contracts
- File `McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs`:
  - Line 191: `public static readonly string[] RobotImports = { "RobotOM", "System", "System.Collections.Generic", "System.Linq", "HPRebar.McpBridge.Core.Scripting" };`
  - Line 202: `public static readonly string[] RobotGlobals = { "robot", "structure", "units", "ct", "log", "progress", "args" };`
  - Line 208: `public const int RobotHeavyMaxTimeoutSeconds = 300;`
- File `McpShared/HPRebar.Mcp.Contracts/PipeNaming.cs`:
  - Line 53: `public const string RobotHost = "robot";`
  - Line 78: `RobotHost => "hprobot-mcp-" + version,`
- File `McpShared/HPRebar.Mcp.Contracts/JsonRpc/JsonRpcMethods.cs`:
  - Line 43: `public const string RobotPrefix = "robot.";`
- File `McpShared/HPRebar.Mcp.Contracts/Messages/ContextMessages.cs`:
  - Line 45: `public RobotInfo? Robot { get; set; }`
  - Line 211: `public sealed record RobotInfo(bool IsAttached, int? AttachedPid, string? RobotVersion, string? StructureType, bool IsCalculated, bool HeavyOperationsEnabled, int NodeCount, int BarCount, int PanelCount, int LoadCaseCount);`
  - Line 251: `public sealed record BridgePingResult(bool Pong, string RevitVersion, bool ExecutionEnabled, bool Busy);`

### 1.2 Robot Host Profile & Server Implementation
- File `HPRobot/HPRobot.Mcp.Server/Hosts/Robot/RobotHostProfile.cs`:
  - Line 13: `public sealed class RobotHostProfile : IHostProfile`
  - Lines 15-20: `ExecuteToolName = "execute_robot_code"; ContextToolName = "get_robot_context"; Version = 2026; HeavyMaxTimeoutSeconds = 300;`
  - Line 22: `BridgeExecutable = "HPRobot.McpBridge.exe";`
  - Lines 36-38: `DefaultVersion => Version; ValidVersions => new[] { 2024, 2025, 2026 };`
  - Lines 48-51: `Categories => new[] { "Model", "Geometry", "Property", "Load", "Analysis", "Results", "Generic" };`
  - Lines 53-56: `CoreToolNames => new[] { ExecuteToolName, ContextToolName, "inspect_type", "cancel_execution" };`
  - Lines 75-79:
    ```csharp
    public string? BridgeNotConnectedHint =>
        $"Start {BridgeExecutable} beside Robot Structural Analysis Professional {Version}, click Attach and tick 'Allow AI code execution' (pipe {PipeNaming.For(PipeNaming.RobotHost, Version)}).";

    public string? TimeoutSemanticsHint =>
        "Robot Structural Analysis may still be running the call; changes made before the timeout persisted (no rollback) — check the snapshot named in the bridge window before retrying.";
    ```
- File `HPRobot/HPRobot.Mcp.Server/HPRobot.Mcp.Server.csproj`:
  - Lines 41-42:
    ```xml
    <Compile Remove="Registry\SeedLibrary\**\*.cs" />
    <EmbeddedResource Include="Registry\SeedLibrary\**\*" LogicalName="SeedLibrary/%(RecursiveDir)%(Filename)%(Extension)" />
    ```
  - Logical resource naming maps exactly to: `SeedLibrary/{Category}/{Name}/{file}`.

### 1.3 Embedded Seed Library (12 Seeds)
Discovered in `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/`:
1. `Analysis/run_calculations` (`tool.json`: `timeoutSeconds=300`, `destructive=true`, `tags=["analysis", "calculate", "fea", "solver", "destructive"]`)
2. `Geometry/assign_node_support` (`tool.json`: `transaction="auto"`, `destructive=true`)
3. `Geometry/draw_bar_by_coords` (`tool.json`: `transaction="auto"`, `destructive=true`)
4. `Geometry/get_coordinate_systems_and_grids` (`tool.json`: `transaction="none"`, `destructive=false`)
5. `Geometry/get_structural_objects` (`tool.json`: `transaction="none"`, `destructive=false`)
6. `Load/assign_bar_load` (`tool.json`: `transaction="auto"`, `destructive=true`)
7. `Load/get_load_definitions` (`tool.json`: `transaction="none"`, `destructive=false`)
8. `Model/get_model_info` (`tool.json`: `transaction="none"`, `destructive=false`)
9. `Property/assign_bar_section` (`tool.json`: `transaction="auto"`, `destructive=true`)
10. `Property/get_materials_and_sections` (`tool.json`: `transaction="none"`, `destructive=false`)
11. `Results/get_bar_forces` (`tool.json`: `transaction="none"`, `destructive=false`)
12. `Results/get_node_reactions` (`tool.json`: `transaction="none"`, `destructive=false`)

Every seed directory contains non-empty `tool.json`, `code.cs`, and `examples.json` ($\ge 2$ examples with `args`).

### 1.4 Sister Host Test Suite References
- `HPEtabs/HPEtabs.Mcp.Server.Tests`:
  - `HPEtabs.Mcp.Server.Tests.csproj` (TFM `net10.0`, MTP runner, linked `FakeRevitExecutor.cs`)
  - `EtabsHostProfileTests.cs` (169 lines: profile name, pipe, version, options binding, tool surface)
  - `SeedLibraryStructureTests.cs` (225 lines: manifest resource loading, schema validation, arg parity)
  - `EtabsToolsOverPipeTests.cs` (275 lines: pipe round-trip with `FakeRevitExecutor`)
  - `SeedLibraryCompileTests.cs` (168 lines: Roslyn compilation with graceful skip when ETABS is not installed)
- `McpShared/HPRebar.Mcp.Server.Core.Tests/Fakes/FakeRevitExecutor.cs`:
  - Single canonical fake executor implementing `IBridgeExecutor` for all MCP server tests.

---

## 2. Logic Chain

1. **Test Project Alignment**:
   - `HPRobot.Mcp.Server` runs on .NET 10 as an MCP stdio console application.
   - Per repository standards and `global.json`, `HPRobot.Mcp.Server.Tests` must be a `.NET 10.0` executable project (`<OutputType>Exe</OutputType>`) with `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>` and `xunit.v3`.
   - By linking `FakeRevitExecutor.cs` from `McpShared/HPRebar.Mcp.Server.Core.Tests/Fakes/FakeRevitExecutor.cs`, code duplication is avoided while enabling full in-memory pipe simulation.

2. **`RobotHostProfileTests.cs` Design**:
   - Observations 1.1 and 1.2 provide the exact invariants for `RobotHostProfile`.
   - Tests must assert identity (`"robot"`), default version (`2026`), method prefix (`"robot."`), script imports (5 namespaces including `HPRebar.McpBridge.Core.Scripting`), globals (7 names), timeout ceiling (300 s), category isolation (structural categories only), and sanitized hints without local paths.
   - The tool surface must be tested via `McpServerHost.CreateBuilder([], RobotHostProfile.Instance).Build()`, verifying exactly 12 static tools (4 core + 8 registry) and zero foreign tools.

3. **`SeedCatalogTests.cs` Design**:
   - In production, seeds are installed from assembly manifest resources via `SeedInstaller.LoadSeeds`.
   - Testing via manifest resources (`typeof(RobotHostProfile).Assembly.GetManifestResourceNames()`) guarantees that `HPRobot.Mcp.Server.csproj` embedded resource configuration (`LogicalName="SeedLibrary/%(RecursiveDir)%(Filename)%(Extension)"`) functions correctly.
   - The test iterates over all 12 seeds to assert:
     - `tool.json` schema validity, `host == "robot"`, `hostVersions` contains `2026`, `destructive` alignment (`run_calculations` has `destructive=true` and 300 s timeout; read-only tools have `destructive=false`).
     - `examples.json` contains $\ge 2$ examples with distinct `args` matching declared properties.
     - `code.cs` ends with top-level `return`, has zero `ScriptGuard` violations, reads exactly declared args, and contains no forbidden process/reflection calls.
     - Dynamic tool registry count: 4 core tools + 8 registry tools + 12 dynamically registered seeds = **exactly 24 tools**.

4. **`RobotToolsOverPipeTests.cs` (Pipe Round-Trip Tests) Design**:
   - By spinning up a real `PipeListener` on an ephemeral pipe name (`hprobot-mcp-test-{guid}`) with `RequestDispatcher` and `FakeRevitExecutor`, tests verify full JSON-RPC wire semantics:
     - `robot.ping`: returns `pong=true`, `2026`, reflects `ExecutionEnabled`.
     - `robot.context`: parses `RobotInfo` (`isAttached`, `attachedPid`, `structureType`, `barCount`, etc.), suppresses `revitVersion` / `isFamily`, and handles detached and busy states.
     - `robot.execute`: verifies parameter forwarding, returns `.rtd` snapshot name in JSON output, enforces 300 s timeout clamp, handles static preview (`PREVIEW` diagnostic) for `dryRun`/`none`, and handles disabled execution.
     - `robot.cancel`: invokes `_executor.Cancel()` and handles timeout-triggered cancellation with cooperative persistence warning ("persisted (no rollback)").

5. **`SeedCompilationTests.cs` Design**:
   - Provides companion Roslyn compilation against `Interop.RobotOM.dll` using `Assert.SkipWhen(...)` if the DLL is not present on the host machine.
   - Ensures that the seed scripts are syntax- and type-valid against the real Robot COM API without failing builds on generic build servers.

---

## 3. Caveats

1. **Out-of-Process COM Nature**:
   - Robot Structural Analysis uses out-of-process COM (`RobotOM.dll`).
   - Server unit tests run entirely in-memory using `FakeRevitExecutor` and do not require Robot to be installed or running, ensuring 100% reliability and sub-second execution during standard test runs.
2. **`Interop.RobotOM.dll` Dependency for Seed Compilation**:
   - `SeedCompilationTests` requires `Interop.RobotOM.dll` from `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\` or `HPROBOT_ROBOT_DIR`. If absent, the test skips cleanly via `Assert.SkipWhen(...)`.
3. **Execution Disabled Message Wording**:
   - In `BridgeEntry.cs`, the execution disabled message states: `"Code execution is disabled. Ask the user to tick 'Allow AI execution' in the HPRobot MCP Bridge window."`
   - In `RobotHostProfile.cs`, the hint states: `"...tick 'Allow AI code execution'..."`
   - Tests assert on the substring `"Allow AI"` to remain robust against minor phrasing variations.

---

## 4. Conclusion

The comprehensive test suite design for `HPRobot.Mcp.Server.Tests` is fully formulated and detailed in `analysis.md`:
1. `HPRobot.Mcp.Server.Tests.csproj`: Configured with .NET 10, MTP runner, linked `FakeRevitExecutor.cs`, and `HPRobot.slnx` entry.
2. `RobotHostProfileTests.cs`: 14 exhaustive tests covering profile identity, pipe naming, contracts, hints, options binding, and static tool surface isolation.
3. `SeedCatalogTests.cs`: Comprehensive suite covering manifest discovery of all 12 seeds across 6 categories, schema validation, examples validation, safety guard validation, and verification of the full 24-tool dynamic catalog.
4. `RobotToolsOverPipeTests.cs`: Live in-memory pipe round-trip suite covering `robot.ping`, `robot.context`, `robot.execute`, `robot.cancel`, static preview, and snapshot tracking.
5. `SeedCompilationTests.cs`: Roslyn compilation suite against `Interop.RobotOM.dll` with graceful skipping.

All specifications are ready for the implementation agent to execute without architectural ambiguity.

---

## 5. Verification Method

To verify the test suite once implemented:

1. **Build the Solution**:
   ```powershell
   dotnet build g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.slnx
   ```
   *Expected Result*: Build succeeds with 0 errors and 0 warnings.

2. **Run Server Unit Tests**:
   ```powershell
   dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.Mcp.Server.Tests\HPRobot.Mcp.Server.Tests.csproj"
   ```
   *Expected Result*:
   - `RobotHostProfileTests`: 14/14 tests PASS.
   - `SeedCatalogTests`: 1 manifest test + 12 schema tests + 12 example tests + 12 code/guard tests + 12 validator tests + 1 24-tool catalog test = 50/50 tests PASS.
   - `RobotToolsOverPipeTests`: 13/13 tests PASS.
   - `SeedCompilationTests`: 12 seed compile tests + 1 API misuse test PASS (or SKIPPED cleanly if `Interop.RobotOM.dll` not present).
   - Overall: 100% PASS rate.

3. **Verify Host Isolation**:
   Confirm that zero references to `HPRebar`, `HPAutoCad`, `HPEtabs`, `HPNavis`, `HPSap2000`, `HPPowerBi`, or `HPExcel` exist in `HPRobot.Mcp.Server.Tests`.

4. **Invalidation Conditions**:
   - Any test failure in `dotnet test`.
   - Tool count in dynamic catalog does not equal 24.
   - Path leakage (e.g. `C:\` or username) in error messages or hints.
   - Inability to build on a machine without Robot installed.
