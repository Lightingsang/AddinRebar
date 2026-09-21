# Handoff Report — Milestone M3 Seed Tools Challenger

**Agent**: challenger_m3_1 (Empirical Challenger)  
**Parent**: orchestrator_7 (`b32c5a58-8b71-46dd-ba9a-5c9e4b6709de`)  
**Verdict**: **REQUEST_CHANGES**  
**Timestamp**: 2026-09-21T14:51:00Z  

---

## 1. Observation

Direct empirical tests were executed against all 12 embedded seed tools located under:
`HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/`
using an automated xUnit test suite `SeedLibraryChallengerTests.cs` executed on `HPRobot.McpBridge.Tests` compiling against the actual installed Autodesk Robot Structural Analysis Professional 2026 interop assembly:
`C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll`.

### 1.1 Test Suite Execution Result
Command executed:
```powershell
dotnet run --project HPRobot\HPRobot.McpBridge.Tests\HPRobot.McpBridge.Tests.csproj --no-build -- --filter-class "*SeedLibraryChallengerTests*"
```
Output:
```
Test run summary: Failed! - G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPRobot\HPRobot.McpBridge.Tests\bin\Debug\net8.0-windows\HPRobot.McpBridge.Tests.dll (net8.0|x64)
  total: 60
  failed: 15
  succeeded: 45
  skipped: 0
  duration: 9s 192ms
```

### 1.2 Seed Compilation Failures (3/12 seeds fail Roslyn compilation)

#### Finding 1: `Load/get_load_definitions/code.cs`
- **File**: `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Load/get_load_definitions/code.cs`
- **Line 36, Col 35**:
  ```csharp
  caseComponents = comb.CaseComponents.Count
  ```
- **Verbatim Error**:
  ```
  Line 36, Col 35: 'IRobotCaseCombination' does not contain a definition for 'CaseComponents' and no accessible extension method 'CaseComponents' accepting a first argument of type 'IRobotCaseCombination' could be found (are you missing a using directive or an assembly reference?)
  ```
- **Inspection of `Interop.RobotOM.dll`**: `[RobotOM.IRobotCaseCombination]` contains property `CaseFactors` (`RobotOM.RobotCaseFactorMngr` / `IRobotCaseFactorMngr`), which exposes `.Count`. There is no member named `CaseComponents`.

#### Finding 2: `Model/get_model_info/code.cs`
- **File**: `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Model/get_model_info/code.cs`
- **Lines 20–22**:
  ```csharp
  for (int i = 1; i <= cCol.Count; i++)
  {
      var c = cCol.Get(i);
      list.Add(new { number = c.Number, name = c.Name, type = c.Type.ToString(), nature = c.Nature.ToString() });
  }
  ```
- **Verbatim Errors**:
  ```
  Line 22, Col 35: 'object' does not contain a definition for 'Number' and no accessible extension method 'Number' accepting a first argument of type 'object' could be found
  Line 22, Col 52: 'object' does not contain a definition for 'Name' and no accessible extension method 'Name' accepting a first argument of type 'object' could be found
  Line 22, Col 67: 'object' does not contain a definition for 'Type' and no accessible extension method 'Type' accepting a first argument of type 'object' could be found
  Line 22, Col 95: 'object' does not contain a definition for 'Nature' and no accessible extension method 'Nature' accepting a first argument of type 'object' could be found
  ```
- **Inspection of `Interop.RobotOM.dll`**: `structure.Cases.GetAll().Get(i)` returns `System.Object`. Without casting `(IRobotCase)cCol.Get(i)` or pattern matching `if (cCol.Get(i) is IRobotCase c)`, C# static compilation fails.

#### Finding 3: `Property/get_materials_and_sections/code.cs`
- **File**: `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Property/get_materials_and_sections/code.cs`
- **Line 19, Col 31**:
  ```csharp
  unitWeight = data.UnitWeight
  ```
- **Lines 38–41, Col 39**:
  ```csharp
  ax = data.GetValue((short)IRobotBarSectionDataValueType.I_BSDV_AX),
  iy = data.GetValue((short)IRobotBarSectionDataValueType.I_BSDV_IY),
  iz = data.GetValue((short)IRobotBarSectionDataValueType.I_BSDV_IZ),
  ix = data.GetValue((short)IRobotBarSectionDataValueType.I_BSDV_IX)
  ```
- **Verbatim Errors**:
  ```
  Line 19, Col 31: 'IRobotMaterialData' does not contain a definition for 'UnitWeight' and no accessible extension method 'UnitWeight' accepting a first argument of type 'IRobotMaterialData' could be found
  Line 38, Col 39: The name 'IRobotBarSectionDataValueType' does not exist in the current context
  Line 39, Col 39: The name 'IRobotBarSectionDataValueType' does not exist in the current context
  Line 40, Col 39: The name 'IRobotBarSectionDataValueType' does not exist in the current context
  Line 41, Col 39: The name 'IRobotBarSectionDataValueType' does not exist in the current context
  ```
- **Inspection of `Interop.RobotOM.dll`**:
  - `IRobotMaterialData` represents density / unit weight via property `RO` (not `UnitWeight`).
  - Section value constants enum in `RobotOM` is `IRobotBarSectionDataValue` (e.g. `IRobotBarSectionDataValue.I_BSDV_AX`), not `IRobotBarSectionDataValueType`.

---

### 1.3 Examples Schema Conformance Failures (12/12 seeds fail)

Every one of the 12 seeds in `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/` fails schema conformance in `examples.json`:

1. **Non-Standard Property `"input"` instead of `"args"`**:
   All 12 seeds use:
   ```json
   [
     {
       "title": "...",
       "input": { ... }
     }
   ]
   ```
   In `HPRebar.Mcp.Server.Registry.Model.ToolExample` (and throughout all sister hosts `HPExcel`, `HPEtabs`, `HPSap2000`, `HPNavis`, `HPCivil3d`), the contract requires:
   ```json
   [
     {
       "title": "...",
       "args": { ... }
     }
   ]
   ```
   When deserialized with `RegistryJson.Deserialize<List<ToolExample>>()`, System.Text.Json ignores `"input"`. The `Args` property defaults to an empty object `{}`.

2. **Single Example Violation (`count == 1`)**:
   All 12 seeds supply exactly 1 example. Standard catalog requirements across all hosts dictate at least 2 distinct examples (`examples.Count >= 2` and distinct `args`).

3. **Validation Rejections on Required Parameters**:
   Because `args` defaults to `{}` due to the `"input"` key mismatch, `ToolValidator.Validate` flags missing required properties on every tool that defines required arguments (`draw_bar_by_coords`, `assign_node_support`, `assign_bar_section`, `assign_bar_load`, `get_bar_forces`, `get_node_reactions`).

---

### 1.4 Passing Checks (45/60 assertions passed)
- **`Seed_ToolJson_SchemaValidity` (12/12 PASS)**: All 12 `tool.json` files have valid name, category, host="robot", version, status="published", title, description, and valid `inputSchema.type == "object"`.
- **`Seed_Passes_SafetyGuard` (12/12 PASS)**: All 12 seeds pass `ScriptGuard.Check(code, GuardProfile.Robot)` with zero security violations.
- **`Seed_ArgsRead_Match_DeclaredProperties` (12/12 PASS)**: All 12 seeds read only arguments that are declared in `inputSchema.properties`, and all declared properties are referenced in the script code.
- **`Seed_Code_CompilesCleanly_AgainstRobotOM` (9/12 PASS)**: 9 seeds compile cleanly without any compilation errors.

---

## 2. Logic Chain

1. **Premise 1**: All embedded seed tools must be production-ready and executable by AI agents without compilation errors against the host API.
2. **Observation 1**: `Load/get_load_definitions`, `Model/get_model_info`, and `Property/get_materials_and_sections` fail Roslyn compilation when compiled against `Interop.RobotOM.dll` due to invalid property names (`CaseComponents`, `UnitWeight`), missing object-to-interface casts (`IRobotCase`), and invalid enum type names (`IRobotBarSectionDataValueType`).
3. **Inference 1**: If an agent attempts to execute or test these 3 tools, Roslyn compilation in `RobotBridgeExecutor` or `ExecuteCodeService` will immediately throw compilation diagnostics and abort execution.
4. **Premise 2**: Tool examples are consumed by the registry layer (`ToolExample`), `ToolValidator`, and AI users via MCP `tools/list`.
5. **Observation 2**: All 12 `examples.json` files use `"input"` rather than `"args"`, causing `ToolExample.Args` to deserialize as `{}`. Furthermore, each tool provides only 1 example instead of the mandated minimum of 2.
6. **Inference 2**: Tool examples are malformed and unusable by the dynamic tool registry, failing schema validation.
7. **Conclusion**: Milestone M3 cannot be approved in its current state. The 3 compilation bugs and 12 example schema issues must be corrected.

---

## 3. Caveats

- Runtime execution against a live Robot GUI process was not tested, as Robot GUI execution is handled out-of-process in Milestone M6 live harness. Static compilation against the official `Interop.RobotOM.dll` (v39.0.1.11984) and full Roslyn AST analysis provide 100% conclusive static evidence.
- A secondary finding was noted in `RobotTierTable.Classify` where standard C# `List.Add(...)` calls on local result lists trigger Tier W (Write) classification because `Add` is matched by simple name. This is an M2 subsystem classification nuance and did not block the M3 seeds themselves.

---

## 4. Conclusion & Required Changes

**Verdict**: **REQUEST_CHANGES**

The following concrete fixes are required for Milestone M3 acceptance:

1. **Fix `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Load/get_load_definitions/code.cs`**:
   - Change `comb.CaseComponents.Count` to `comb.CaseFactors.Count`.

2. **Fix `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Model/get_model_info/code.cs`**:
   - Cast `cCol.Get(i)` to `IRobotCase`:
     ```csharp
     for (int i = 1; i <= cCol.Count; i++)
     {
         if (cCol.Get(i) is IRobotCase c)
         {
             list.Add(new { number = c.Number, name = c.Name, type = c.Type.ToString(), nature = c.Nature.ToString() });
         }
     }
     ```

3. **Fix `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/Property/get_materials_and_sections/code.cs`**:
   - Change `data.UnitWeight` to `data.RO`.
   - Change `IRobotBarSectionDataValueType.I_BSDV_*` to `IRobotBarSectionDataValue.I_BSDV_*`.

4. **Fix All 12 `examples.json` Files in `HPRobot/HPRobot.Mcp.Server/Registry/SeedLibrary/**`**:
   - Replace key `"input"` with `"args"`.
   - Provide at least 2 distinct examples per tool with different argument sets.
   - Ensure all required arguments are populated in both examples.

---

## 5. Verification Method

To verify after fixes are applied:

```powershell
# 1. Rebuild the solution
dotnet build HPRobot/HPRobot.slnx -c Debug

# 2. Run the empirical challenger test suite
dotnet run --project HPRobot/HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj --no-build -- --filter-class "*SeedLibraryChallengerTests*"
```
Expected result upon remediation: **60/60 tests passing (0 failed)**.
