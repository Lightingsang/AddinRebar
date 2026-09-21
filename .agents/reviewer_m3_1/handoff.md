# Review & Handoff Report — Milestone M3: HPRobot Stdio Server & 24 Tools Catalog

**Reviewer**: `reviewer_m3_1` (M3 Tool Completeness Reviewer)  
**Recipient**: `orchestrator_7` (Project Orchestrator, conversation ID: `b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Worker Under Review**: `worker_m3_1`  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\reviewer_m3_1\`  
**Timestamp**: 2026-09-21T14:52:00Z  
**Verdict**: **REQUEST_CHANGES**

---

## Review Summary

**Verdict**: **REQUEST_CHANGES**

`HPRobot.Mcp.Server` successfully boots over stdio and advertises the complete 24-tool surface (4 core, 8 meta, 12 seeds), 3 resources, and 4 prompts. `HPRobot.slnx` builds with 0 errors and 0 warnings in both Debug and Release configurations. `RobotHostProfile.cs` strictly meets all contract requirements.

However, independent test execution and Roslyn compilation verification revealed **15 test failures** in `HPRobot.McpBridge.Tests`:
1. **3 Seed Tools Fail Roslyn Compilation** against `Interop.RobotOM.dll` due to invalid API member usage and missing type casts (`Load/get_load_definitions`, `Model/get_model_info`, `Property/get_materials_and_sections`).
2. **All 12 Seed Tools Violate Examples Schema Conventions**: every seed's `examples.json` uses `"input"` instead of the standard `"args"` object, and provides only 1 example instead of the required minimum of 2 distinct examples.

---

## Findings

### [Critical] Finding 1: 3 Seed Tools Fail Roslyn Compilation Against RobotOM API

- **What**: Three seed tool C# scripts fail compilation when analyzed against Autodesk Robot Structural Analysis Professional 2026 `Interop.RobotOM.dll`.
- **Where**:
  1. `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Load/get_load_definitions/code.cs`: line 36
  2. `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Model/get_model_info/code.cs`: lines 20–24
  3. `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Property/get_materials_and_sections/code.cs`: lines 19, 38–41
- **Why**:
  - In `Load/get_load_definitions/code.cs:36`:
    ```csharp
    caseComponents = comb.CaseComponents.Count
    ```
    *Error*: `'IRobotCaseCombination' does not contain a definition for 'CaseComponents'`. RobotOM combinations manage components through `CaseFactors` (`RobotCaseFactorMngr`).
  - In `Model/get_model_info/code.cs:22`:
    ```csharp
    var c = cCol.Get(i);
    list.Add(new { number = c.Number, name = c.Name, type = c.Type.ToString(), nature = c.Nature.ToString() });
    ```
    *Error*: `cCol.Get(i)` returns `object`. Calling `.Number`, `.Name`, `.Type`, `.Nature` fails with CS1061 (`'object' does not contain a definition for...`).
  - In `Property/get_materials_and_sections/code.cs`:
    ```csharp
    unitWeight = data.UnitWeight  // Line 19
    ax = data.GetValue((short)IRobotBarSectionDataValueType.I_BSDV_AX) // Lines 38-41
    ```
    *Error*: `IRobotMaterialData` does not define `UnitWeight` (the RobotOM property is `RO`). `IRobotBarSectionDataValueType` does not exist in RobotOM (the enum name is `IRobotBarSectionDataValue`).
- **Suggestion**:
  1. In `Load/get_load_definitions/code.cs`: change `comb.CaseComponents.Count` to `comb.CaseFactors.Count`.
  2. In `Model/get_model_info/code.cs`: cast `var c = (IRobotCase)cCol.Get(i);`.
  3. In `Property/get_materials_and_sections/code.cs`: change `data.UnitWeight` to `data.RO`, and change `(short)IRobotBarSectionDataValueType.I_BSDV_*` to `(short)IRobotBarSectionDataValue.I_BSDV_*` or `(IRobotBarSectionDataValue)IRobotBarSectionDataValue.I_BSDV_*`.

---

### [Major] Finding 2: All 12 Seed Packages Violate Examples Schema Convention

- **What**: Every `examples.json` file in `Registry/SeedLibrary/**` uses the non-standard key `"input"` instead of `"args"`, and provides only 1 example instead of at least 2 distinct examples.
- **Where**: All 12 files:
  - `Registry/SeedLibrary/Analysis/run_calculations/examples.json`
  - `Registry/SeedLibrary/Geometry/assign_node_support/examples.json`
  - `Registry/SeedLibrary/Geometry/draw_bar_by_coords/examples.json`
  - `Registry/SeedLibrary/Geometry/get_coordinate_systems_and_grids/examples.json`
  - `Registry/SeedLibrary/Geometry/get_structural_objects/examples.json`
  - `Registry/SeedLibrary/Load/assign_bar_load/examples.json`
  - `Registry/SeedLibrary/Load/get_load_definitions/examples.json`
  - `Registry/SeedLibrary/Model/get_model_info/examples.json`
  - `Registry/SeedLibrary/Property/assign_bar_section/examples.json`
  - `Registry/SeedLibrary/Property/get_materials_and_sections/examples.json`
  - `Registry/SeedLibrary/Results/get_bar_forces/examples.json`
  - `Registry/SeedLibrary/Results/get_node_reactions/examples.json`
- **Why**:
  Across all HP MCP subsystems (`HPEtabs`, `HPExcel`, `HPSap2000`), registry seed tools follow the standard format:
  ```json
  [
    {
      "title": "Example 1 description",
      "args": { ... }
    },
    {
      "title": "Example 2 description",
      "args": { ... }
    }
  ]
  ```
  Using `"input"` causes downstream clients and `SeedLibraryChallengerTests.Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples` to fail on every single seed tool.
- **Suggestion**:
  Update all 12 `examples.json` files: replace `"input"` with `"args"`, and add a second realistic example scenario for each seed.

---

## 1. Observation

1. **Solution Compilation**:
   Executed independent builds:
   - `dotnet build HPRobot/HPRobot.slnx -c Debug` -> **PASS** (`0 Warning(s), 0 Error(s), Time Elapsed 00:00:03.06`).
   - `dotnet build HPRobot/HPRobot.slnx -c Release` -> **PASS** (`0 Warning(s), 0 Error(s), Time Elapsed 00:00:05.51`).

2. **Stdio MCP Protocol Verification**:
   Executed via `McpShared/tools/mcp-call.py` with `HPRobot.Mcp.Server.exe`:
   - `tools/list`: Total tool count returned is **24**:
     - **4 Core Tools**: `execute_robot_code`, `get_robot_context`, `inspect_type`, `cancel_execution`.
     - **8 Registry Meta Tools**: `manage_tool`, `get_run`, `publish_tool`, `search_tools`, `get_tool`, `run_tool`, `propose_tool`, `test_tool`.
     - **12 Embedded Seeds**: `get_model_info`, `get_structural_objects`, `get_materials_and_sections`, `get_coordinate_systems_and_grids`, `get_load_definitions`, `draw_bar_by_coords`, `assign_node_support`, `assign_bar_section`, `assign_bar_load`, `run_calculations`, `get_node_reactions`, `get_bar_forces`.
   - `resources/list`: Returned **3 resources**: `registry://tools`, `robot://selection`, `robot://model/info`.
   - `prompts/list`: Returned **4 prompts**: `robot_analysis_template`, `toolify_run`, `robot_query_template`, `robot_modify_template`.

3. **RobotHostProfile Verification**:
   Inspected `HPRobot/HPRobot.Mcp.Server/Hosts/Robot/RobotHostProfile.cs`:
   - `HostId`: `PipeNaming.RobotHost` (`"robot"`).
   - `PipeName(2026)`: `PipeNaming.For("robot", 2026)` (`"hprobot-mcp-2026"`).
   - `MethodPrefix`: `JsonRpcMethods.RobotPrefix` (`"robot."`).
   - `MaxTimeoutSeconds`: `HostScriptContracts.RobotHeavyMaxTimeoutSeconds` (`300`).
   - `CoreToolNames`: `["execute_robot_code", "get_robot_context", "inspect_type", "cancel_execution"]`.
   - All properties match specifications.

4. **Automated Unit & Challenger Test Execution**:
   Executed `dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj`:
   ```
   Test run summary: Failed! - G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.McpBridge.Tests\bin\Debug\net8.0-windows\HPRobot.McpBridge.Tests.dll (net8.0|x64)
     total: 197
     failed: 15
     succeeded: 182
     skipped: 0
     duration: 9s 541ms
   ```
   Verbatim failures:
   - `failed Seed_Code_CompilesCleanly_AgainstRobotOM(category: "Load", name: "get_load_definitions")`: `'IRobotCaseCombination' does not contain a definition for 'CaseComponents'`
   - `failed Seed_Code_CompilesCleanly_AgainstRobotOM(category: "Model", name: "get_model_info")`: `'object' does not contain a definition for 'Number'`
   - `failed Seed_Code_CompilesCleanly_AgainstRobotOM(category: "Property", name: "get_materials_and_sections")`: `'IRobotMaterialData' does not contain a definition for 'UnitWeight'`; `The name 'IRobotBarSectionDataValueType' does not exist in the current context`
   - `failed Seed_ExamplesJson_HasStandardArgsProperty_AndAtLeastTwoExamples` on all 12 seeds: `uses non-standard key 'input' instead of 'args'` and `has 1 example(s). Expected at least 2 distinct examples`.

---

## 2. Logic Chain

1. **Step 1**: The build of `HPRobot.slnx` succeeds in both configurations because seed tool C# files are marked with `<Compile Remove="Registry\SeedLibrary\**\*.cs" />` and embedded as resources; they are compiled by Roslyn at runtime rather than during project compilation.
2. **Step 2**: Because `HPRobot.Mcp.Server` embeds these scripts without build-time compilation, runtime Roslyn compilation against `Interop.RobotOM.dll` is the only way to detect type errors in the seed library.
3. **Step 3**: Reflection against `C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll` confirms:
   - `IRobotCaseCombination` contains `CaseFactors`, not `CaseComponents`.
   - `IRobotMaterialData` contains `RO`, not `UnitWeight`.
   - `IRobotBarSectionData.GetValue` expects `IRobotBarSectionDataValue`, not `IRobotBarSectionDataValueType`.
   - `IRobotCaseCollection.Get(i)` returns `object`, requiring an explicit cast to `IRobotCase`.
4. **Step 4**: Conformance with the rest of the repository (`HPEtabs`, `HPExcel`, `HPSap2000`) and `SeedLibraryChallengerTests` requires that `examples.json` provide at least 2 examples using the `"args"` key.
5. **Step 5**: Because 3 seeds fail Roslyn compilation and all 12 fail examples validation (15 test failures total), Requirement 2 ("Verify that each of the 12 seeds in `Registry/SeedLibrary/` contains valid `tool.json`, `code.cs`, and `examples.json`") is not satisfied. Therefore, the required verdict is **REQUEST_CHANGES**.

---

## 3. Caveats

- Out-of-process COM execution against a live, running `robot.exe` instance with UI interaction was not executed in this turn (requires an active Robot 2026 process and is covered in Milestone M6 live harness).
- The remaining 9 seeds (`run_calculations`, `assign_node_support`, `draw_bar_by_coords`, `get_coordinate_systems_and_grids`, `get_structural_objects`, `assign_bar_load`, `assign_bar_section`, `get_bar_forces`, `get_node_reactions`) compile cleanly and pass AST safety checks.

---

## 4. Conclusion

**Verdict: REQUEST_CHANGES**

`worker_m3_1` must address the following items:
1. Fix the 3 failing seed scripts:
   - `Load/get_load_definitions/code.cs`: replace `comb.CaseComponents.Count` with `comb.CaseFactors.Count`.
   - `Model/get_model_info/code.cs`: cast `c` to `(IRobotCase)cCol.Get(i)`.
   - `Property/get_materials_and_sections/code.cs`: replace `data.UnitWeight` with `data.RO`; replace `IRobotBarSectionDataValueType` with `IRobotBarSectionDataValue`.
2. Update all 12 `examples.json` files:
   - Replace `"input"` key with `"args"`.
   - Supply at least 2 distinct, valid examples per seed file.
3. Ensure `dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj` passes 100% (197/197 tests).

---

## 5. Verification Method

To independently verify after changes are applied:

1. **Run Full Test Suite**:
   ```powershell
   dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj
   ```
   *Expected*: Total 197, Passed 197, Failed 0, Skipped 0.

2. **Verify Solution Build**:
   ```powershell
   dotnet build HPRobot/HPRobot.slnx -c Debug
   dotnet build HPRobot/HPRobot.slnx -c Release
   ```
   *Expected*: 0 warnings, 0 errors.

3. **Verify Stdio Tools List**:
   ```powershell
   $env:PYTHONUTF8=1
   (python McpShared/tools/mcp-call.py "HPRobot/HPRobot.Mcp.Server/bin/Debug/net10.0/HPRobot.Mcp.Server.exe" tools/list | ConvertFrom-Json).result.tools.Count
   ```
   *Expected*: 24.
