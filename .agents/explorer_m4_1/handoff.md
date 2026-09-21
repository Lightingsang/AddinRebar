# Handoff Report: Sister Host Test Architecture & HPRobot.Mcp.Server.Tests Blueprint

## 1. Observation

### 1.1 Sister Host Project Configurations
- **HPEtabs.Mcp.Server.Tests**:
  - File: `HPEtabs/HPEtabs.Mcp.Server.Tests/HPEtabs.Mcp.Server.Tests.csproj` (lines 4, 12–13, 17–21, 27–30, 35):
    ```xml
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    ...
    <PackageReference Include="xunit.v3" Version="3.1.0"/>
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5"/>
    <PackageReference Include="Microsoft.Bcl.AsyncInterfaces" Version="10.0.12"/>
    ...
    <ProjectReference Include="..\HPEtabs.Mcp.Server\HPEtabs.Mcp.Server.csproj"/>
    <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Server.Core\HPRebar.Mcp.Server.Core.csproj"/>
    <ProjectReference Include="..\..\McpShared\HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj"/>
    <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj"/>
    ...
    <Compile Include="..\..\McpShared\HPRebar.Mcp.Server.Core.Tests\Fakes\FakeRevitExecutor.cs" Link="Fakes\FakeRevitExecutor.cs"/>
    ```
  - Executed command: `dotnet test HPEtabs.Mcp.Server.Tests` (Cwd: `HPEtabs/`).
  - Result: `total: 81, failed: 0, succeeded: 81, skipped: 0, duration: 6s 636ms`.

- **HPSap2000.Mcp.Server.Tests**:
  - File: `HPSap2000/HPSap2000.Mcp.Server.Tests/HPSap2000.Mcp.Server.Tests.csproj` (lines 4, 12–13, 17–21, 27–30, 35):
    - Identical structure: `net10.0`, `Exe`, `UseMicrosoftTestingPlatformRunner=true`, `xunit.v3 3.1.0`, `Microsoft.Bcl.AsyncInterfaces 10.0.12`, linked `FakeRevitExecutor.cs`.
  - Executed command: `dotnet test HPSap2000.Mcp.Server.Tests` (Cwd: `HPSap2000/`).
  - Result: `total: 79, failed: 0, succeeded: 79, skipped: 0, duration: 6s 702ms`.

- **HPExcel.Mcp.Server.Tests**:
  - File: `HPExcel/HPExcel.Mcp.Server.Tests/HPExcel.Mcp.Server.Tests.csproj` (lines 4, 8–9, 13–15, 22–25, 29):
    - Targets `net10.0`, `Exe`, `UseMicrosoftTestingPlatformRunner=true`, links `FakeRevitExecutor.cs`.
  - Executed command: `dotnet test HPExcel.Mcp.Server.Tests` (Cwd: `HPExcel/`).
  - Result: `total: 90, failed: 0, succeeded: 90, skipped: 0, duration: 9s 255ms`.

- **HPRobot.McpBridge.Tests**:
  - File: `HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj`:
    - Targets `net8.0-windows`, `UseWPF=true`.
  - Executed command: `dotnet test HPRobot.McpBridge.Tests` (Cwd: `HPRobot/`).
  - Result: `total: 197, failed: 0, succeeded: 197, skipped: 0, duration: 9s 063ms`.

### 1.2 Host COM Wrapper Availability on Dev Machine
- Executed command: `powershell -NoProfile -Command "Test-Path 'C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll'"`
- Result: `True` (verified present on dev system).
- `HPRobot/Directory.Build.props` lines 14–27 sets `RobotMajor` to 2026, resolves `RobotInstallDir`, and evaluates `RobotApiAvailable Condition="Exists('$(RobotInstallDir)Interop.RobotOM.dll')"`.

### 1.3 Target Project State
- `HPRobot/HPRobot.Mcp.Server/HPRobot.Mcp.Server.csproj` compiles cleanly with 0 errors and 0 warnings.
- `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/` contains all 12 seed tool packages (`Analysis/run_calculations`, `Geometry/assign_node_support`, `Geometry/draw_bar_by_coords`, `Geometry/get_coordinate_systems_and_grids`, `Geometry/get_structural_objects`, `Load/assign_bar_load`, `Load/get_load_definitions`, `Model/get_model_info`, `Property/assign_bar_section`, `Property/get_materials_and_sections`, `Results/get_bar_forces`, `Results/get_node_reactions`).
- `HPRobot/HPRobot.slnx` currently contains `HPRobot.McpBridge`, `HPRobot.McpBridge.Tests`, and `HPRobot.Mcp.Server`. `HPRobot.Mcp.Server.Tests` is planned for Milestone 4 and is not yet created.

---

## 2. Logic Chain

1. **Test Runner Alignment**:
   - `global.json` pins `Microsoft.Testing.Platform` (Observation 1.1).
   - In .NET 10, running `xunit.v3` under Microsoft.Testing.Platform requires `<OutputType>Exe</OutputType>` and `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>` in the `.csproj`.
   - All three sister host test projects use this exact combination and successfully execute under `dotnet test` with 100% pass rates.
   - Therefore, `HPRobot.Mcp.Server.Tests.csproj` must use these exact settings.

2. **Dependency & Warning Suppression**:
   - `xunit.v3.common` references `Microsoft.Bcl.AsyncInterfaces` 6.0.0, while `HPRebar.Mcp.Contracts` targets `netstandard2.0` with `System.Text.Json` 10.x.
   - In all sister hosts, an explicit package reference `<PackageReference Include="Microsoft.Bcl.AsyncInterfaces" Version="10.0.12" />` silences compiler warning `MSB3277` and unifies the assembly binding.
   - Therefore, `HPRobot.Mcp.Server.Tests.csproj` must include this package reference.

3. **Decoupled COM Verification Pattern**:
   - `HPEtabs` and `HPSap2000` do not reference the host COM interop assembly directly in their test `.csproj` (Observation 1.1).
   - Instead, `SeedLibraryCompileTests` resolves the wrapper path at runtime via environment variable, registry, or default Program Files path, creating a `MetadataReference.CreateFromFile(wrapper)` dynamically with Roslyn.
   - If the wrapper is not found, tests skip gracefully using `Assert.SkipWhen(...)`.
   - On this dev system, `Interop.RobotOM.dll` exists (Observation 1.2), ensuring all 12 seed compilation tests will execute live and pass, while preserving build integrity in any environment where Robot is absent.
   - Therefore, `HPRobot.Mcp.Server.Tests` must adopt this dynamic Roslyn compilation pattern rather than a hard compile-time reference.

4. **Test Class Organization**:
   - The repository standardizes on 4 core test concerns:
     1. Host Profile, configuration binding, tool surface, prompts, and resources (`RobotHostProfileTests.cs`).
     2. Seed manifest resources, schema validity, argument parity, and registry validation (`SeedCatalogTests.cs`).
     3. Named pipe JSON-RPC integration via `FakeRevitExecutor` covering context, execution, timeout clamping, static preview, and refusals (`SeedExecutionTests.cs`).
     4. Roslyn dynamic compilation of all 12 seeds against `Interop.RobotOM.dll` (`SeedCompilationTests.cs`).
   - This 4-class architecture directly matches the orchestrator's blueprint in `PROJECT.md` lines 229–233.

---

## 3. Caveats

1. **Timeout Threshold**: Robot Structural Analysis profile timeout ceiling is 300 seconds (`HostScriptContracts.RobotHeavyMaxTimeoutSeconds = 300`), unlike ETABS and SAP2000 which have 600 seconds. Tests must assert clamping against 300 seconds, not 600 seconds.
2. **Units System**: Robot Structural Analysis script execution enforces Metric units (`m, kN, kN·m, MPa`) through `RobotUnitsPolicy` and `RobotHostProfile.ScriptContractSummary`. Tests should assert Metric units and avoid imperial assumptions.
3. **Execution Directory**: When executing tests with `dotnet test`, commands should be run from `HPRobot/` or specify the test project path to ensure `global.json` settings are applied correctly.

---

## 4. Conclusion

The recommended architecture for `HPRobot.Mcp.Server.Tests` is:
1. **Target**: `net10.0`, `Exe`, `UseMicrosoftTestingPlatformRunner = true`.
2. **Dependencies**: `xunit.v3` (3.1.0), `xunit.runner.visualstudio` (3.1.5), `Microsoft.Bcl.AsyncInterfaces` (10.0.12).
3. **References**: `HPRobot.Mcp.Server`, `HPRebar.Mcp.Server.Core`, `HPRebar.McpBridge.Core`, `HPRebar.Mcp.Contracts`.
4. **Shared Asset**: Linked `FakeRevitExecutor.cs`.
5. **Test Classes**:
   - `RobotHostProfileTests.cs`: 7+ facts validating profile properties, configuration, 12 static tools, prompts, resources.
   - `SeedCatalogTests.cs`: Facts & theories validating 12 embedded seeds, schema compliance, argument parity, and `ToolValidator`.
   - `SeedExecutionTests.cs`: Async pipe round-trip tests using `PipeListener` and `FakeRevitExecutor` verifying context, execute, static preview, and safety refusals.
   - `SeedCompilationTests.cs`: Theories compiling all 12 seeds against `Interop.RobotOM.dll` with Roslyn `CSharpCompilation`.

The complete specification, `.csproj` definition, and test class designs are documented in `analysis.md`.

---

## 5. Verification Method

To independently verify the findings and benchmark tests:
1. Inspect sister test projects:
   - `HPEtabs/HPEtabs.Mcp.Server.Tests/HPEtabs.Mcp.Server.Tests.csproj`
   - `HPSap2000/HPSap2000.Mcp.Server.Tests/HPSap2000.Mcp.Server.Tests.csproj`
   - `HPExcel/HPExcel.Mcp.Server.Tests/HPExcel.Mcp.Server.Tests.csproj`
2. Run sister test suites:
   ```powershell
   cd "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPEtabs"
   dotnet test HPEtabs.Mcp.Server.Tests
   cd "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPSap2000"
   dotnet test HPSap2000.Mcp.Server.Tests
   cd "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPExcel"
   dotnet test HPExcel.Mcp.Server.Tests
   ```
3. Run existing bridge tests for Robot:
   ```powershell
   cd "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot"
   dotnet test HPRobot.McpBridge.Tests
   ```
4. Verify Robot COM wrapper existence on dev machine:
   ```powershell
   Test-Path "C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll"
   ```
5. Invalidation conditions:
   - If `dotnet test` fails under .NET 10 without MTP runner.
   - If `xunit.v3` emits `MSB3277` warning due to missing `Microsoft.Bcl.AsyncInterfaces 10.0.12`.
   - If seed tools fail Roslyn compilation against `Interop.RobotOM.dll`.
