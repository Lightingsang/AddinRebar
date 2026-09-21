# Sister Host Test Architecture Analysis Report

## Executive Summary

This investigation analyzes how sister host MCP server test projects (`HPEtabs.Mcp.Server.Tests`, `HPSap2000.Mcp.Server.Tests`, `HPExcel.Mcp.Server.Tests`, and `HPPowerBi.Mcp.Server.Tests`) are architected in this repository. It provides the architectural blueprint, `.csproj` specification, and detailed test class designs for **`HPRobot.Mcp.Server.Tests`** (.NET 10.0, xUnit v3, Microsoft Testing Platform).

All sister host server test suites verify the stdio server out-of-process without requiring a live host CAD/FEM instance:
- **HPEtabs.Mcp.Server.Tests**: 81 tests passing (6.6s), covering profile, pipe round-trips via `FakeRevitExecutor`, embedded seed library structure, and Roslyn dynamic compilation against `ETABSv1.dll`.
- **HPSap2000.Mcp.Server.Tests**: 79 tests passing (6.7s), covering profile, pipe round-trips via `FakeRevitExecutor`, seed structure, and Roslyn dynamic compilation against `SAP2000v1.dll`.
- **HPExcel.Mcp.Server.Tests**: 90 tests passing (9.2s), covering catalog completeness (24 tools), FakeExecutor round-trips for all 12 seeds, adversarial testing, and Roslyn seed compilation.
- **HPPowerBi.Mcp.Server.Tests**: 96 tests passing (2.3s), covering profile, tools execution, and DAX schema round-trips.

For `HPRobot.Mcp.Server.Tests`, we formulate a robust, 4-class architecture that directly mirrors the repo's established COM out-of-process pattern (HPEtabs/HPSap2000) while incorporating HPExcel's per-seed round-trip rigor, fulfilling all acceptance criteria of Milestone 4.

---

## 1. Sister Host Project Comparison & Benchmarking

| Dimension | `HPEtabs.Mcp.Server.Tests` | `HPSap2000.Mcp.Server.Tests` | `HPExcel.Mcp.Server.Tests` | `HPRobot.Mcp.Server.Tests` (Recommended) |
|---|---|---|---|---|
| **Target Framework** | `net10.0` | `net10.0` | `net10.0` | `net10.0` |
| **Output Type** | `Exe` | `Exe` | `Exe` | `Exe` |
| **Test Runner** | Microsoft.Testing.Platform (`<UseMicrosoftTestingPlatformRunner>true`) | Microsoft.Testing.Platform (`<UseMicrosoftTestingPlatformRunner>true`) | Microsoft.Testing.Platform (`<UseMicrosoftTestingPlatformRunner>true`) | Microsoft.Testing.Platform (`<UseMicrosoftTestingPlatformRunner>true`) |
| **Pinned Runner in `global.json`** | `"test": { "runner": "Microsoft.Testing.Platform" }` | `"test": { "runner": "Microsoft.Testing.Platform" }` | `"test": { "runner": "Microsoft.Testing.Platform" }` | Pinned in `HPRobot/global.json` |
| **xUnit Version** | `xunit.v3` 3.1.0 | `xunit.v3` 3.1.0 | `xunit.v3` 3.1.0 | `xunit.v3` 3.1.0 |
| **VS Runner Version** | `xunit.runner.visualstudio` 3.1.5 | `xunit.runner.visualstudio` 3.1.5 | `xunit.runner.visualstudio` 3.1.5 | `xunit.runner.visualstudio` 3.1.5 |
| **Bcl.AsyncInterfaces** | `10.0.12` (silences MSB3277) | `10.0.12` (silences MSB3277) | `10.0.12` (silences MSB3277) | `10.0.12` (silences MSB3277) |
| **Host COM Dependency in csproj** | None (dynamic Roslyn metadata reference) | None (dynamic Roslyn metadata reference) | NuGet (`Microsoft.Office.Interop.Excel`) | None (dynamic Roslyn metadata reference) |
| **Linked Fake Executor** | `..\..\McpShared\HPRebar.Mcp.Server.Core.Tests\Fakes\FakeRevitExecutor.cs` | `..\..\McpShared\HPRebar.Mcp.Server.Core.Tests\Fakes\FakeRevitExecutor.cs` | `..\..\McpShared\HPRebar.Mcp.Server.Core.Tests\Fakes\FakeRevitExecutor.cs` | `..\..\McpShared\HPRebar.Mcp.Server.Core.Tests\Fakes\FakeRevitExecutor.cs` |
| **Test File Structure** | 4 files: Profile, ToolsOverPipe, SeedStructure, SeedCompile | 4 files: Profile, ToolsOverPipe, SeedStructure, SeedCompile | 6 files: CatalogCompleteness, ContextShape, SeedScriptRoslynCompilation, SeedToolsRoundTrip, Adversarial | 4 files: `RobotHostProfileTests`, `SeedCatalogTests`, `SeedExecutionTests`, `SeedCompilationTests` |
| **Total Test Count** | 81 tests | 79 tests | 90 tests | ~85–95 tests estimated |
| **Live Execution Time** | 6.6s | 6.7s | 9.2s | ~6–8s estimated |

---

## 2. Deep Dive: Architectural Standards Across Sister Hosts

### 2.1 Target Framework & Microsoft Testing Platform Runner
Every host server test project targets `net10.0`.
In .NET 10, the test project produces an executable:
```xml
<TargetFramework>net10.0</TargetFramework>
<OutputType>Exe</OutputType>
<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
<IsPackable>false</IsPackable>
```
The repository root and each deliverable root contains a `global.json` pinning:
```json
{
  "sdk": {
    "version": "10.0.300",
    "rollForward": "latestMinor",
    "allowPrerelease": true
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```
Because `xunit.v3` implements the Microsoft.Testing.Platform interface natively, `dotnet test` invokes the compiled executable directly without vstest overhead.

### 2.2 Package Reference Discipline & The MSB3277 Warning
In all sister projects, the package references are strictly:
1. `xunit.v3` (v3.1.0)
2. `xunit.runner.visualstudio` (v3.1.5)
3. `Microsoft.Bcl.AsyncInterfaces` (v10.0.12)

**Critical insight on `Microsoft.Bcl.AsyncInterfaces`**:
`xunit.v3.common` references `Microsoft.Bcl.AsyncInterfaces` 6.0.0, whereas `HPRebar.Mcp.Contracts` (compiled against `netstandard2.0` with `System.Text.Json` 10.x) references version 10.x. Explicitly declaring `<PackageReference Include="Microsoft.Bcl.AsyncInterfaces" Version="10.0.12" />` unifies the dependency, preventing the `MSB3277` assembly conflict warning during builds.

### 2.3 Project Reference Boundaries & Architectural Isolation
Following the rule of `AGENTS.md`, test projects reference strictly:
1. Sibling server project: `..\<Host>.Mcp.Server\<Host>.Mcp.Server.csproj`
2. Shared contracts: `..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj`
3. Shared bridge core: `..\..\McpShared\HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj`
4. Shared server core: `..\..\McpShared\HPRebar.Mcp.Server.Core\HPRebar.Mcp.Server.Core.csproj`

Test projects **NEVER** cross-reference other hosts (`HPEtabs`, `HPAutoCad`, `HPExcel`, etc.).

### 2.4 Shared Test Asset: FakeRevitExecutor
Named pipes and JSON-RPC dispatching are tested using the shared fake executor:
```xml
<ItemGroup>
    <Compile Include="..\..\McpShared\HPRebar.Mcp.Server.Core.Tests\Fakes\FakeRevitExecutor.cs" Link="Fakes\FakeRevitExecutor.cs" />
</ItemGroup>
```
`FakeRevitExecutor` implements `IBridgeExecutor`. It allows mocking:
- `ContextHandler`: returns mock `ContextResult` (with `RobotInfo`)
- `ExecuteHandler`: captures incoming `ExecuteRequest` (verifying `Code`, `Transaction`, `DryRun`, `TimeoutSeconds`, `Label`, `Args`) and returns mock `ExecuteResult` (including `Snapshot`, `Diagnostics`, `Changed`, `DurationMs`)
- `ExecuteFailure` / `ContextFailure`: simulates `BridgeRequestException` (e.g. `BridgeErrorCode.ExecutionDisabled`, `Busy`, `NoActiveDocument`)
- `ProgressSteps` / `ProgressDelayMs`: tests progress streaming across named pipes

### 2.5 Host COM Wrapper Handling: Why Dynamic Roslyn Compilation Wins
For out-of-process COM hosts (ETABS, SAP2000, Robot), the server project does not load the COM wrapper into memory; only Roslyn scripts run on the bridge side.
In `HPEtabs` and `HPSap2000`:
- The server test project does **not** add a compile-time `<Reference Include="ETABSv1">` in `.csproj`.
- Instead, `SeedLibraryCompileTests.cs` resolves the wrapper assembly path dynamically at runtime:
  1. Environment variable (e.g. `HPROBOT_ROBOT_DIR`)
  2. Windows Registry: `HKLM\SOFTWARE\Classes\CLSID\{F7870790-CDE5-11D1-8FF1-00A02447BAAE}\LocalServer32`
  3. Default Program Files path: `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll`
- If the wrapper is found, it creates a `MetadataReference.CreateFromFile(wrapper)` and tests Roslyn script compilation for all 12 seeds.
- If the wrapper is not found (e.g. on generic CI machines), it gracefully skips with:
  `Assert.SkipWhen(compiled is null, "Robot Structural Analysis Professional 2026 not installed (Interop.RobotOM.dll not found)")`.

This prevents build breaks while enabling 100% thorough verification on machines where the host is installed (such as the dev machine, where `Interop.RobotOM.dll` was verified to exist).

---

## 3. Test Organization Across Sister Hosts

Across the sister test suites, 4 distinct testing concerns are addressed:

### Concern 1: Profile & Host Identity Tests
Verifies that the server binary advertises the exact host contract:
- Host metadata: `HostId ("robot")`, `DisplayName ("Robot Structural Analysis")`, `ServerName ("HPRobot MCP")`, `ProductFolder ("HPRobot")`, `EnvPrefix ("HPROBOT_MCP_")`, `DefaultVersion (2026)`, `ValidVersions ([2024, 2025, 2026])`.
- Method prefix (`robot.`) and pipe naming (`hprobot-mcp-2026`).
- Tool names (`execute_robot_code`, `get_robot_context`, `inspect_type`, `cancel_execution`).
- Options binding via `McpServerHost.ConfigureOptions(...)`: verifies default seeding of version and pipe, library paths in `%AppData%\HPRobot\McpServer\`, and rejection of invalid host versions (e.g. 1997).
- Tool surface count: 4 core tools + 8 registry tools = 12 tools registered by the host engine.
- Tool metadata & safety annotations: `execute_robot_code` has `DestructiveHint = true`, `get_robot_context` has `ReadOnlyHint = true` and `IdempotentHint = true`.
- Description budget: descriptions stay under the 1800-character budget and explain units (m, kN, kN·m, MPa), 3 tiers, snapshots, and safety toggles.
- Resources (`robot://model/info`, `robot://selection`) and prompts (`robot_query_template`, `robot_modify_template`, `robot_analysis_template`, `toolify_run`).

### Concern 2: Tool Registration & Embedded Seed Catalog Tests
Verifies the embedded seed library packaged in assembly resources:
- Inventory: Exactly 12 seeds across 6 categories (`Model`, `Geometry`, `Property`, `Load`, `Analysis`, `Results`).
- Embedded resource manifests: `SeedLibrary/<Category>/<Name>/{tool.json, code.cs, examples.json}` exist for all 12 tools.
- Metadata conformance: valid JSON, `host == "robot"`, status `"published"`, author `"hprebar"`, valid description length (>= 20 chars).
- Transaction & destructive alignment:
  - `transaction: "none"` -> `destructive: false` (read-only queries)
  - `transaction: "auto"` -> `destructive: true` (mutating and destructive actions)
  - Destructive tool `run_calculations`: tagged with `"destructive"`, timeout = 300s, description starts with `"DESTRUCTIVE"`.
- Argument parity between schema and code:
  - Every property declared in `tool.json` `inputSchema` is read by `code.cs` via `args.Str()`, `args.Int()`, `args.Double()`, or `args.Bool()`.
  - Every argument read in `code.cs` is declared in `tool.json`.
- ScriptGuard check: `ScriptGuard.Check(seed.Code, GuardProfile.Robot)` produces 0 violations (no `Quit`, no `MessageBox`, no `Process`, no `#r`/`#load`).
- Script formatting: ends with top-level `return` statement, no double-escaped strings (`\\` or `\"`), at least 2 distinct valid examples.
- Engine validation: `ToolValidator.Validate(...)` passes 100% with 0 errors.

### Concern 3: FakeExecutor Pipe Round-Trip Integration Tests
Tests execution across real named pipes without requiring Robot to run:
- Pipe setup: In-memory `PipeListener` on unique GUID pipe name, `RequestDispatcher` with `FakeRevitExecutor` and `BridgeSettings`.
- Client setup: `RevitBridgeClient` pointing to the test pipe, configured with `RobotHostProfile.Instance`.
- Context tool:
  - Returns `robot` info block (`isAttached`, `attachedPid`, `robotVersion`, `structureType`, `isCalculated`, `heavyOperationsEnabled`, `nodeCount`, `barCount`, `panelCount`, `loadCaseCount`).
  - Hides fields from other hosts (`revitVersion`, `etabs`, `autocad`, `navis`).
  - Handles `includeSelection: true` vs `false`.
  - Handles detached state (`isAttached: false`).
- Resources: `ModelInfoAsync` and `SelectionAsync` return valid JSON snapshots.
- Execute tool:
  - Parameter pass-through: `Code`, `Transaction`, `DryRun`, `TimeoutSeconds`, `Label`, `Args`.
  - Snapshot pass-through: `ExecuteResult.Snapshot` (e.g. `"20260921-120000_model_draw_bar.rtd"`).
  - Timeout clamping: 300s allowed, >300s clamped to 300s (`RobotHostProfile.MaxTimeoutSeconds`).
  - Static preview: writing script with `dryRun=true` or `transaction="none"` returns `PREVIEW` diagnostic error.
  - Refusals:
    - Execution disabled: `ExecutionEnabled = false` returns clear error naming `HPRobot.McpBridge.exe` and "Allow AI code execution".
    - Destructive/Heavy disabled: error -32001 naming "Allow heavy/destructive operations".
  - Bridge not connected: error message explicitly names `HPRobot.McpBridge.exe` and `hprobot-mcp-2026`, without leaking user machine paths.
  - Timeout semantics: explains that calculation may still be running and changes persisted without rollback.

### Concern 4: Roslyn Seed Compilation Tests
Verifies C# code validity of all 12 seed scripts:
- Dynamically resolves `Interop.RobotOM.dll`.
- Assembles a synthetic `SeedHost` class with:
  - Usings: `HostScriptContracts.RobotImports` (`RobotOM`, `System`, `System.Collections.Generic`, `System.Linq`, `HPRebar.McpBridge.Core.Scripting`)
  - Globals:
    - `public RobotOM.IRobotApplication robot;`
    - `public RobotOM.IRobotStructure structure;`
    - `public RobotOM.IRobotUnitMngr units;`
    - `public CancellationToken ct;`
    - `public Action<string> log;`
    - `public Action<int, int?, string?> progress;`
    - `public ScriptArgs args;`
- Compiles via Roslyn `CSharpCompilation.Create(...)`.
- Asserts 0 compilation errors across all 12 seeds.
- Verifies rejection of invalid members (`CS1061`) and acceptance of real RobotOM members.
- Validates semantic tier alignment: checks that members called in the script match the declared safety tier.

---

## 4. Recommended Specification for `HPRobot.Mcp.Server.Tests`

### 4.1 Project Directory & File Layout
```
HPRobot/HPRobot.Mcp.Server.Tests/
├── HPRobot.Mcp.Server.Tests.csproj
├── RobotHostProfileTests.cs
├── SeedCatalogTests.cs
├── SeedExecutionTests.cs
└── SeedCompilationTests.cs
```

### 4.2 `.csproj` Specification
```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <LangVersion>latest</LangVersion>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <RootNamespace>HPRobot.Mcp.Server.Tests</RootNamespace>
        <Configurations>Debug;Release</Configurations>
        <IsPackable>false</IsPackable>
        <!-- global.json pins test.runner = Microsoft.Testing.Platform; xunit.v3 speaks it natively -->
        <OutputType>Exe</OutputType>
        <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="xunit.v3" Version="3.1.0" />
        <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
        <!-- Unify Microsoft.Bcl.AsyncInterfaces to 10.0.12 so MSB3277 stays quiet -->
        <PackageReference Include="Microsoft.Bcl.AsyncInterfaces" Version="10.0.12" />
    </ItemGroup>

    <ItemGroup>
        <!-- Robot MCP Server & shared host-neutral engine -->
        <ProjectReference Include="..\HPRobot.Mcp.Server\HPRobot.Mcp.Server.csproj" />
        <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Server.Core\HPRebar.Mcp.Server.Core.csproj" />
        <ProjectReference Include="..\..\McpShared\HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj" />
        <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj" />
    </ItemGroup>

    <ItemGroup>
        <!-- Linked shared fake executor for named pipe tests -->
        <Compile Include="..\..\McpShared\HPRebar.Mcp.Server.Core.Tests\Fakes\FakeRevitExecutor.cs" Link="Fakes\FakeRevitExecutor.cs" />
    </ItemGroup>

</Project>
```

### 4.3 Solution Registration (`HPRobot/HPRobot.slnx`)
Add the test project to `HPRobot.slnx`:
```xml
<Project Path="HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj" />
```

---

## 5. Detailed Test Classes Blueprint

### 5.1 `RobotHostProfileTests.cs`
**Purpose**: Validate `RobotHostProfile` properties, options binding, tool surface, prompts, and resources.

**Key Test Cases**:
1. `Profile_Names_The_Robot_Pipe_Prefix_Tools_Registry_Root_Ceiling_And_Hints()`:
   - `profile.HostId` == `"robot"`
   - `profile.DisplayName` == `"Robot Structural Analysis"`
   - `profile.ServerName` == `"HPRobot MCP"`
   - `profile.PipeName(2026)` == `"hprobot-mcp-2026"`
   - `profile.Method("execute")` == `"robot.execute"`
   - `profile.ExecuteToolName` == `"execute_robot_code"`
   - `profile.ContextToolName` == `"get_robot_context"`
   - `profile.ProductFolder` == `"HPRobot"`
   - `profile.EnvPrefix` == `"HPROBOT_MCP_"`
   - `profile.DefaultVersion` == `2026`
   - `profile.ValidVersions` == `[2024, 2025, 2026]`
   - `profile.MaxTimeoutSeconds` == `300`
   - `profile.ScriptImports` == `HostScriptContracts.RobotImports` (contains `RobotOM`, `HPRebar.McpBridge.Core.Scripting`, excludes `System.IO`, `System.Reflection`)
   - `profile.Categories` contains `"Model"`, `"Geometry"`, `"Property"`, `"Load"`, `"Analysis"`, `"Results"`, `"Generic"`
   - `profile.CoreToolNames` == `["execute_robot_code", "get_robot_context", "inspect_type", "cancel_execution"]`
   - `profile.BridgeNotConnectedHint` contains `"HPRobot.McpBridge.exe"`, `"2026"`, `"Attach"`, `"Allow AI code execution"`, `"hprobot-mcp-2026"`
   - `profile.TimeoutSemanticsHint` contains `"persisted"`, `"no rollback"`, `"snapshot"`
2. `Options_Bind_The_Registry_Root_And_Pipe_From_The_Profile_Even_Without_Configuration()`:
   - Evaluates `McpServerHost.ConfigureOptions(...)` with empty settings and with custom settings.
   - Verifies `bridge.HostVersion` == 2026, `bridge.PipeName` == `"hprobot-mcp-2026"`.
   - Verifies `registry.LibraryPath` and `registry.DbPath` contain `"HPRobot"`.
   - Asserts no strings from other hosts (`HPRebar`, `HPAutoCad`, `HPEtabs`, etc.) exist in paths.
3. `Invalid_HostVersion_Is_Refused()`:
   - Host version `1997` throws `OptionsValidationException`.
4. `Tool_Surface_Registers_Exactly_Four_Core_Tools_And_Eight_Registry_Tools()`:
   - Builds host: `using var host = McpServerHost.CreateBuilder([], RobotHostProfile.Instance).Build();`
   - Total registered tools: exactly 12 static tools.
   - Verifies core tools: `execute_robot_code`, `get_robot_context`, `inspect_type`, `cancel_execution`.
   - Verifies registry tools: `search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`.
   - Asserts no tools from other hosts (`revit`, `autocad`, `etabs`, `navis`, `excel`).
5. `Tool_Descriptions_And_Annotations_Are_Accurate()`:
   - `execute_robot_code`: `DestructiveHint == true`, `ReadOnlyHint == false`, length <= 1800 chars, covers units (`m, kN, kN·m, MPa`), tiers, snapshot, safety checkboxes.
   - `get_robot_context`: `ReadOnlyHint == true`, `IdempotentHint == true`, `DestructiveHint == false`, covers `isAttached`, `isCalculated`, `nodeCount`, `barCount`.
6. `Resources_And_Prompts_Use_Robot_Scheme_And_Names()`:
   - Resources: `robot://model/info`, `robot://selection`.
   - Prompts: `robot_query_template`, `robot_modify_template`, `robot_analysis_template`, `toolify_run`.
   - Asserts no prompt/resource starts with other host prefixes.
7. `Server_Name_In_Options_Is_HPRobot_MCP()`:
   - Verifies `options.ServerInfo.Name == "HPRobot MCP"`.

### 5.2 `SeedCatalogTests.cs`
**Purpose**: Validate embedded seed library manifest resources, schema validity, argument parity, and registry compliance.

**Key Test Cases**:
1. `All_Twelve_Seeds_Are_Embedded_Across_Six_Categories()`:
   - Discovers all embedded seeds via `assembly.GetManifestResourceNames()`.
   - Exactly 12 seeds in total.
   - Expected seeds:
     - `Model/get_model_info`
     - `Geometry/get_structural_objects`
     - `Property/get_materials_and_sections`
     - `Geometry/get_coordinate_systems_and_grids`
     - `Load/get_load_definitions`
     - `Geometry/draw_bar_by_coords`
     - `Geometry/assign_node_support`
     - `Property/assign_bar_section`
     - `Load/assign_bar_load`
     - `Analysis/run_calculations`
     - `Results/get_node_reactions`
     - `Results/get_bar_forces`
2. `Seed_Record_Is_Well_Formed_For_Robot_Host(string key)` (Theory over all 12 seeds):
   - `tool.json` has `host == "robot"`, `status == "published"`, `author == "hprebar"`.
   - Category is member of `RobotHostProfile.Categories`.
   - Name matches `^[a-z][a-z0-9_]{2,63}$` and is not reserved.
   - Timeout between 5s and 300s.
   - Code length < 32 KB, <= 120 lines.
   - Ends with top-level `return` statement.
   - No double-escaped strings (`\\` or `\"`).
   - At least 2 distinct examples with non-empty titles and valid arguments.
3. `Destructive_And_Writing_Seeds_Have_Required_Markers(string key)` (Theory):
   - If `transaction == "none"`: `destructive == false`, timeout <= 60s, description mentions read-only.
   - If `transaction == "auto"`: `destructive == true`, description mentions snapshot.
   - If `run_calculations`: tags include `"destructive"`, timeout == 300s, description starts with `"DESTRUCTIVE"`.
4. `Seed_Code_Passes_Robot_Guard_And_Reads_Only_Declared_Args(string key)` (Theory):
   - `ScriptGuard.Check(seed.Code, GuardProfile.Robot)` has 0 violations.
   - `ScriptAnalyzer.Analyze(seed.Code, AnalyzerProfile.Robot)` extracts argument keys.
   - Every key read matches `properties` in `inputSchema`.
   - Every property in `inputSchema` is read by code.
   - Does not contain forbidden calls: `Quit`, `ApplicationExit`, `Interactive`, `MessageBox`, `Process`, `#r`, `#load`.
5. `Seed_Record_Passes_ToolValidator_For_Robot_Profile(string key)` (Theory):
   - Deserializes `ToolRecord`, sets `Code` and `Examples`.
   - `ToolValidator.Validate(record, null, [], false, RobotHostProfile.Instance)` reports `report.IsValid == true`.

### 5.3 `SeedExecutionTests.cs`
**Purpose**: Named Pipe integration tests with `FakeRevitExecutor` verifying tool execution, argument forwarding, result formatting, snapshot handling, and error diagnostics.

**Key Test Cases**:
1. `Context_Tool_Returns_Robot_Block_And_Hides_Other_Host_Fields()`:
   - Sets mock `ContextResult` with `RobotInfo(IsAttached: true, AttachedPid: 1234, RobotVersion: "39.0", StructureType: "Frame3D", IsCalculated: true, HeavyOperationsEnabled: true, NodeCount: 10, BarCount: 15, PanelCount: 2, LoadCaseCount: 4)`.
   - Invokes `GetRobotContextTool.GetContextAsync(includeSelection: true)`.
   - Asserts result contains `robot`, `nodeCount: 10`, `barCount: 15`, `isCalculated: true`.
   - Asserts result suppresses `revitVersion`, `etabs`, `autocad`, `navis`.
2. `Context_Before_Attach_Reports_IsAttached_False()`:
   - Returns `RobotInfo(false, null, null, null, false, false, 0, 0, 0, 0)`.
   - Asserts `isAttached == false`, `isModifiable == false`.
3. `Resources_Return_Model_And_Selection_Snapshots()`:
   - Tests `RobotResourceProvider.ModelInfoAsync()` and `SelectionAsync()`.
   - Verifies JSON format and content.
4. `Execute_Tool_Dispatches_Over_Robot_Method_And_Returns_Snapshot()`:
   - Sets mock `ExecuteResult` with `Snapshot = "20260921-120000_draw_bar.rtd"`.
   - Executes with `transaction: "auto"`, `label: "draw_bar"`, `args: {"node1": 1, "node2": 2}`.
   - Verifies `LastExecuteRequest`: `Code`, `Transaction == "auto"`, `Label == "draw_bar"`, `Args`.
   - Verifies JSON output contains snapshot filename.
5. `Execute_Tool_Clamps_Timeout_To_300_Seconds()`:
   - 300s timeout accepted as 300s.
   - 900s timeout clamped to 300s.
6. `Static_Preview_Returns_Preview_Diagnostic_Error()`:
   - Writing script called with `dryRun: true` returns `PREVIEW` diagnostic error.
7. `Execution_Disabled_Refusal_Names_Bridge_Window()`:
   - Bridge setting `ExecutionEnabled = false` returns error naming `HPRobot.McpBridge.exe` and "Allow AI code execution".
8. `Heavy_Operations_Disabled_Refusal_Returns_32001()`:
   - `BridgeErrorCode.ExecutionDisabled` for heavy operations returns error naming "Allow heavy/destructive operations".
9. `Context_Tool_Surfaces_Busy_NotAttached_And_NoModel_Errors()`:
   - Tests `BridgeRequestException.Busy("Robot")`.
   - Tests `BridgeErrorCode.NoActiveDocument` ("Robot not attached — click Attach in the HPRobot MCP Bridge window").
   - Tests no active `.rtd` project.
10. `Bridge_Not_Connected_Error_Names_Exe_And_Pipe_Without_Leaking_Paths()`:
    - Points client to unconnected pipe.
    - Error message names `HPRobot.McpBridge.exe` and `hprobot-mcp-2026`.
    - Asserts no machine path (`C:\`) or username is leaked.
11. `Timeout_Explains_No_Rollback_Semantics()`:
    - Client sends request with short timeout.
    - `BridgeTimeoutException` message mentions "persisted (no rollback)" and references snapshot.
12. `Round_Trip_Dispatches_Payloads_For_All_Seeds()` (Theories or representative seed execution tests):
    - Tests parameter formatting and dispatch for key seeds (`get_model_info`, `draw_bar_by_coords`, `run_calculations`, `get_node_reactions`).

### 5.4 `SeedCompilationTests.cs`
**Purpose**: Validate Roslyn compilation of all 12 seed scripts against `Interop.RobotOM.dll` with graceful skip when not installed.

**Key Test Cases**:
1. `All_Twelve_Seeds_Compile_Against_RobotOM_Wrapper(string key)` (Theory over all 12 seeds):
   - Dynamically resolves `Interop.RobotOM.dll` path:
     - `Environment.GetEnvironmentVariable("HPROBOT_ROBOT_DIR")`
     - Registry `HKLM/HKCR\CLSID\{F7870790-CDE5-11D1-8FF1-00A02447BAAE}\LocalServer32`
     - Default Program Files path: `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll`
   - Uses `Assert.SkipWhen(compiled is null, "Robot Structural Analysis Professional 2026 not installed (Interop.RobotOM.dll not found)")`.
   - Emits `SeedHost` class with usings (`HostScriptContracts.RobotImports`), fields (`robot`, `structure`, `units`, `ct`, `log`, `progress`, `args`), and method `Run()` containing `seed.Code`.
   - Compiles via `CSharpCompilation.Create(...)`.
   - Asserts `errors.Length == 0`.
2. `Seed_Compilation_Rejects_Unknown_Members_And_Accepts_Valid_Members()`:
   - Unknown member `robot.NoSuchMember()` produces `CS1061`.
   - Valid member `structure.Nodes.GetAll().Count` compiles with 0 errors.
3. `Compilation_Verifies_Semantic_Tier_Alignment(string key)` (Theory):
   - Analyzes bound member accesses in compilation AST.
   - Binds against `RobotTierTable` / bridge tier classifications (`R`, `W`, `D`).
   - Asserts read-only seeds only call `R` members.
   - Asserts mutating seeds call `W` members.
   - Asserts only `run_calculations` calls `D` members (`Calculate`).

---

## 6. Implementation Checklist & Risks for Implementer Agent

| Item | Requirement | Risk / Pitfall | Mitigation |
|---|---|---|---|
| **1. MSB3277 Assembly Conflict** | Package `Microsoft.Bcl.AsyncInterfaces` 10.0.12 | `xunit.v3.common` references 6.0.0, causing build warning | Explicit `<PackageReference Include="Microsoft.Bcl.AsyncInterfaces" Version="10.0.12" />` in `.csproj` |
| **2. Test Runner Execution** | Microsoft.Testing.Platform | Running `dotnet test` from wrong directory or with `--nologo` can fail MTP runner | Run from `HPRobot/` directory where `global.json` is located; avoid unsupported vstest flags |
| **3. Fake Executor Linkage** | Link `FakeRevitExecutor.cs` | Duplicating fake file leads to maintenance drift | Use `<Compile Include="..." Link="..." />` identical to HPEtabs/HPSap2000 |
| **4. COM Dependency** | No hard dependency in `.csproj` | Referencing `Interop.RobotOM.dll` directly in test `.csproj` without condition will break CI builds | Use dynamic Roslyn metadata loading in `SeedCompilationTests.cs` with `Assert.SkipWhen(...)` |
| **5. Timeout Ceiling** | Robot profile max is 300s | Sister hosts have 600s (ETABS/SAP); testing against 600s will fail | Clamp assertions to 300s (`HostScriptContracts.RobotHeavyMaxTimeoutSeconds`) |
| **6. Units Enforcement** | Metric units (m, kN, kN·m, MPa) | Describing or testing imperial units | Enforce Metric units in prompt, description, and execution tests |
| **7. HPRobot.slnx Registration** | Update `HPRobot.slnx` | Solution build will not include test project | Add `<Project Path="HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj" />` |
