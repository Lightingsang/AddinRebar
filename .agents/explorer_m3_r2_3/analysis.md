# Technical Investigation Analysis — Defect 3: Test Suite Integration & Verification Methodology

**Specialist**: explorer_m3_r2_3 (Test Suite Integration & Verification Specialist)  
**Parent Orchestrator**: orchestrator_7 (`b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Scope**: HPRobot MCP Subsystem — Defect 3 Root Cause, Test Mechanics, and Verification Procedure  
**Timestamp**: 2026-09-21T15:00:00Z  

---

## Executive Summary

This forensic investigation analyzes **Defect 3** (Test suite execution discovery and verification methodology) identified during the Milestone M3 Forensic Audit. The audit rejected the M3 delivery due to 15 test failures in `HPRobot.McpBridge.Tests` that were omitted from worker reporting.

Key conclusions established by empirical evidence:
1. **Seed Discovery Mechanism**: `SeedLibraryChallengerTests.cs` dynamically discovers seed tools by scanning the physical filesystem on disk (`Directory.GetDirectories` from `AppContext.BaseDirectory/../../../../HPRobot.Mcp.Server/Registry/SeedLibrary`), parsing raw `tool.json`, `code.cs`, and `examples.json` directly from source files. It does *not* read embedded assembly manifest resources.
2. **Test Count Dynamics (The 185 vs 197 Discrepancy)**:
   - The base bridge test suite contains **137 tests** across 9 test classes (all currently PASS).
   - `SeedLibraryChallengerTests.cs` contains **5 `[Theory]` test methods** running across **12 seed tools**, generating exactly **60 dynamic test cases** ($5 \times 12 = 60$).
   - Total test count executed by the runner is **197 tests** ($137 + 60 = 197$).
   - The number **185** cited in earlier reports originated from a formula assuming 4 assertions per seed ($4 \times 12 = 48$; $137 + 48 = 185$). When the 5th assertion (`Seed_ArgsRead_Match_DeclaredProperties`) was incorporated, the total became 197. In both counts, exactly **15 test cases fail** (3 Roslyn syntax/type errors + 12 schema violations).
3. **Execution Command Architecture**:
   - `HPRobot.McpBridge.Tests` is an executable (`<OutputType>Exe</OutputType>`) configured with `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>` and `xunit.v3`.
   - In the absence of a root-level `global.json`, executing `dotnet test` from repository root fails to activate MTP.
   - `dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj` invokes the MTP test executable directly and reliably across any environment or working directory.
4. **McpShared Zero-Regression Baseline**:
   - `McpShared/HPRebar.Mcp.Server.Core.Tests`: **613 tests** (.NET 10), 0 failed, 0 skipped.
   - `McpShared/HPRebar.McpBridge.Core.Net48Tests`: **72 tests** (.NET 4.8), 0 failed, 0 skipped.
   - Combined shared regression baseline: **685 tests**.

---

## 1. Deep Dive: `SeedLibraryChallengerTests.cs` Mechanics

### 1.1 Discovery Implementation
In `HPRobot/HPRobot.McpBridge.Tests/SeedLibraryChallengerTests.cs`:
```csharp
private static readonly string SeedLibraryDir = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "HPRobot.Mcp.Server", "Registry", "SeedLibrary"));

public static IEnumerable<object[]> GetAllSeeds()
{
    var dirs = Directory.GetDirectories(SeedLibraryDir, "*", SearchOption.AllDirectories)
        .Where(d => File.Exists(Path.Combine(d, "tool.json")))
        .OrderBy(d => d);

    foreach (var dir in dirs)
    {
        var cat = Path.GetFileName(Path.GetDirectoryName(dir))!;
        var name = Path.GetFileName(dir)!;
        yield return new object[] { cat, name };
    }
}
```

- **Runtime Path Resolution**:
  - `AppContext.BaseDirectory` resolves to:  
    `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.McpBridge.Tests\bin\Debug\net8.0-windows\`
  - Navigating 4 directory levels up reaches the subsystem root:  
    `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\`
  - Appending `HPRobot.Mcp.Server\Registry\SeedLibrary` points directly to the physical source directory containing the 12 seeds.
- **Disk vs Resource Comparison**:
  - **Runtime Server (`HPRobot.Mcp.Server.dll`)**: Embeds seed files as manifest resources via `<EmbeddedResource Include="Registry\SeedLibrary\**\*" />`. Seeds are compiled and validated upon registry initialization.
  - **Challenger Tests (`SeedLibraryChallengerTests`)**: Bypasses embedded resources and inspects source files directly from disk.
  - **Implication**: Any edits to `code.cs` or `examples.json` take effect immediately upon subsequent test runs without requiring `HPRobot.Mcp.Server.dll` to be rebuilt (when using `--no-build`).

### 1.2 Mathematical Breakdown of Generated Test Cases
The 12 discovered seed tools are:
1. `Analysis/run_calculations`
2. `Geometry/assign_node_support`
3. `Geometry/draw_bar_by_coords`
4. `Geometry/get_coordinate_systems_and_grids`
5. `Geometry/get_structural_objects`
6. `Load/assign_bar_load`
7. `Load/get_load_definitions`
8. `Model/get_model_info`
9. `Property/assign_bar_section`
10. `Property/get_materials_and_sections`
11. `Results/get_bar_forces`
12. `Results/get_node_reactions`

`SeedLibraryChallengerTests` defines 5 `[Theory]` methods taking `[MemberData(nameof(GetAllSeeds))]`:
1. `Seed_Code_CompilesCleanly_AgainstRobotOM` (12 invocations)
2. `Seed_Passes_SafetyGuard` (12 invocations)
3. `Seed_ToolJson_SchemaValidity` (12 invocations)
4. `Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples` (12 invocations)
5. `Seed_ArgsRead_Match_DeclaredProperties` (12 invocations)

**Test Counts**:
$$\text{Challenger Tests} = 5 \text{ assertions} \times 12 \text{ seeds} = 60 \text{ dynamic tests}$$
$$\text{Base Bridge Tests} = 137 \text{ unit tests across 9 test classes}$$
$$\text{Total Executed Tests} = 137 + 60 = 197 \text{ tests}$$

**Reconciliation with "185 Tests"**:
- When auditor initially evaluated the suite, only 4 assertions per seed were counted ($4 \times 12 = 48$), leading to $137 + 48 = 185$.
- With all 5 assertions present in `SeedLibraryChallengerTests.cs`, the test runner reports **197 tests**.
- Both counts isolate the exact same **15 failures**.

---

## 2. Exhaustive Catalog of the 15 Failing Test Cases

| # | Test Method | Category / Seed Name | Line/Col | Diagnostic & Failure Mode | Root Cause in RobotOM API / Schema |
|---|-------------|----------------------|----------|---------------------------|-----------------------------------|
| 1 | `Seed_Code_CompilesCleanly_AgainstRobotOM` | `Load/get_load_definitions` | L36, C35 | CS1061: `'IRobotCaseCombination' does not contain a definition for 'CaseComponents'` | Combination cases expose components via `CaseFactors` (`RobotCaseFactorMngr`), not `CaseComponents`. |
| 2 | `Seed_Code_CompilesCleanly_AgainstRobotOM` | `Model/get_model_info` | L22, C35/52/67/95 | CS1061: `'object' does not contain a definition for 'Number'` (and `Name`, `Type`, `Nature`) | `cCol.Get(i)` on `IRobotCaseCollection` returns `object`. Missing explicit cast to `(IRobotCase)`. |
| 3 | `Seed_Code_CompilesCleanly_AgainstRobotOM` | `Property/get_materials_and_sections` | L19, C31; L38-41, C39 | CS1061: `'IRobotMaterialData' does not contain a definition for 'UnitWeight'`. CS0103: `'IRobotBarSectionDataValueType' does not exist`. | Density in `IRobotMaterialData` is `RO`. Enum for section data constants is `IRobotBarSectionDataValue`. |
| 4 | `Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples` | `Analysis/run_calculations` | N/A | 1 example provided; uses `"input"` instead of `"args"`. | Schema convention violation: requires $\ge 2$ examples with `"args"`. |
| 5 | `Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples` | `Geometry/assign_node_support` | N/A | 1 example provided; uses `"input"` instead of `"args"`. Missing required `nodeNumber`. | Schema convention violation: requires $\ge 2$ examples with `"args"`. |
| 6 | `Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples` | `Geometry/draw_bar_by_coords` | N/A | 1 example provided; uses `"input"` instead of `"args"`. Missing required parameters. | Schema convention violation: requires $\ge 2$ examples with `"args"`. |
| 7 | `Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples` | `Geometry/get_coordinate_systems_and_grids` | N/A | 1 example provided; uses `"input"` instead of `"args"`. | Schema convention violation: requires $\ge 2$ examples with `"args"`. |
| 8 | `Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples` | `Geometry/get_structural_objects` | N/A | 1 example provided; uses `"input"` instead of `"args"`. | Schema convention violation: requires $\ge 2$ examples with `"args"`. |
| 9 | `Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples` | `Load/assign_bar_load` | N/A | 1 example provided; uses `"input"` instead of `"args"`. Missing required parameters. | Schema convention violation: requires $\ge 2$ examples with `"args"`. |
| 10 | `Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples` | `Load/get_load_definitions` | N/A | 1 example provided; uses `"input"` instead of `"args"`. | Schema convention violation: requires $\ge 2$ examples with `"args"`. |
| 11 | `Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples` | `Model/get_model_info` | N/A | 1 example provided; uses `"input"` instead of `"args"`. | Schema convention violation: requires $\ge 2$ examples with `"args"`. |
| 12 | `Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples` | `Property/assign_bar_section` | N/A | 1 example provided; uses `"input"` instead of `"args"`. Missing required parameters. | Schema convention violation: requires $\ge 2$ examples with `"args"`. |
| 13 | `Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples` | `Property/get_materials_and_sections` | N/A | 1 example provided; uses `"input"` instead of `"args"`. | Schema convention violation: requires $\ge 2$ examples with `"args"`. |
| 14 | `Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples` | `Results/get_bar_forces` | N/A | 1 example provided; uses `"input"` instead of `"args"`. Missing required parameters. | Schema convention violation: requires $\ge 2$ examples with `"args"`. |
| 15 | `Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples` | `Results/get_node_reactions` | N/A | 1 example provided; uses `"input"` instead of `"args"`. Missing required parameters. | Schema convention violation: requires $\ge 2$ examples with `"args"`. |

---

## 3. Test Runner Mechanics & Execution Architecture

### 3.1 Project Architecture
`HPRobot.McpBridge.Tests.csproj` is an xUnit v3 executable project:
- `<OutputType>Exe</OutputType>`
- `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>`
- Target Framework: `net8.0-windows` (WPF enabled)
- Package References: `xunit.v3` (3.1.0), `xunit.runner.visualstudio` (3.1.5), `NSubstitute` (5.3.0)

### 3.2 Why `dotnet run --project ...` Is Required
1. **Runner Independence**: The repository root does not have a `global.json`. `HPRobot/global.json` configures `"test": { "runner": "Microsoft.Testing.Platform" }`. When executing `dotnet test` from root, the CLI does not resolve `HPRobot/global.json` and fails to run MTP test executables without explicit flags.
2. **Direct Entry Point**: Invoking `dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj` starts the test executable directly, utilizing MTP native orchestration without dependency on working directory location.
3. **MTP Command Line Rules**:
   - Options passed after `--` are interpreted directly by MTP and xUnit v3 runner.
   - Example class filtering: `-- --filter-class "*SeedLibraryChallengerTests*"`
   - Exclusion filtering: `-- --filter-not-class "*SeedLibraryChallengerTests*"`
   - **Prohibited flag**: `--nologo` must NEVER be passed; MTP rejects it as an invalid argument.

---

## 4. Comprehensive Verification Checklist for Worker & Reviewers

The following 5-stage verification procedure must be executed sequentially to establish full compliance:

```
┌────────────────────────────────────────────────────────┐
│ Stage 1: Static Compilation Verification               │
│ - Debug & Release configurations                       │
│ - Zero warnings, zero errors                           │
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│ Stage 2: MCP Stdio Protocol Handshake                  │
│ - 24 tools, 3 resources, 4 prompts                     │
│ - UTF-8 console encoding enabled                       │
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│ Stage 3: HPRobot Test Suite Execution                  │
│ - 197/197 tests pass (or 185/185)                      │
│ - 0 failed, 0 skipped                                  │
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│ Stage 4: McpShared Regression Verification             │
│ - 613 net10 tests pass                                 │
│ - 72 net48 tests pass (685 total)                      │
└───────────────────────────┬────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────┐
│ Stage 5: Forensic Honesty & Artifact Validation        │
│ - Exact verbatim test summary reported                 │
│ - No suppressed or truncated test logs                 │
└────────────────────────────────────────────────────────┘
```

### Stage 1: Static Compilation Verification
```powershell
# 1.1 Clean build in Debug configuration
dotnet build HPRobot/HPRobot.slnx -c Debug

# 1.2 Clean build in Release configuration
dotnet build HPRobot/HPRobot.slnx -c Release
```
- **Success Criteria**: `0 Warning(s), 0 Error(s)` in both configurations.

### Stage 2: Stdio MCP Protocol Verification
```powershell
# Ensure UTF-8 output decoding for Python
python -X utf8 McpShared/tools/mcp-call.py HPRobot/HPRobot.Mcp.Server/bin/Debug/net10.0/HPRobot.Mcp.Server.exe tools/list

python -X utf8 McpShared/tools/mcp-call.py HPRobot/HPRobot.Mcp.Server/bin/Debug/net10.0/HPRobot.Mcp.Server.exe resources/list

python -X utf8 McpShared/tools/mcp-call.py HPRobot/HPRobot.Mcp.Server/bin/Debug/net10.0/HPRobot.Mcp.Server.exe prompts/list
```
- **Success Criteria**:
  - `tools/list`: Exactly 24 tools (4 core, 8 dynamic registry, 12 embedded seeds).
  - `resources/list`: Exactly 3 resources (`registry://tools`, `robot://model/info`, `robot://selection`).
  - `prompts/list`: Exactly 4 prompts (`robot_analysis_template`, `toolify_run`, `robot_query_template`, `robot_modify_template`).

### Stage 3: HPRobot Test Suite Execution
```powershell
# 3.1 Run challenger seed tests only
dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj --no-build -- --filter-class "*SeedLibraryChallengerTests*"

# 3.2 Run full bridge test suite
dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj --no-build
```
- **Success Criteria**:
  - `SeedLibraryChallengerTests`: 60 total, 60 succeeded, 0 failed, 0 skipped.
  - Complete Suite: 197 total, 197 succeeded, 0 failed, 0 skipped (or 185/185 if running with 4 assertions).

### Stage 4: McpShared Regression Baseline Verification
```powershell
# 4.1 Run .NET 10 shared server engine tests
dotnet run --project McpShared/HPRebar.Mcp.Server.Core.Tests/HPRebar.Mcp.Server.Core.Tests.csproj --no-build

# 4.2 Run .NET 4.8 shared bridge core tests
dotnet run --project McpShared/HPRebar.McpBridge.Core.Net48Tests/HPRebar.McpBridge.Core.Net48Tests.csproj --no-build
```
- **Success Criteria**:
  - `HPRebar.Mcp.Server.Core.Tests`: exactly 613 tests passed, 0 failed, 0 skipped.
  - `HPRebar.McpBridge.Core.Net48Tests`: exactly 72 tests passed, 0 failed, 0 skipped.
  - Total shared tests: 685 passed, 0 regressions.

### Stage 5: Forensic Honesty & Artifact Validation
1. Worker must include verbatim command line output blocks with execution durations and exact passed/failed counts.
2. Worker must not claim `137/137` tests passed when the actual test project discovers and runs 197 tests.
3. Reviewer must independently execute each command in clean state to verify matching test counts.
