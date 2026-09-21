# Handoff Report — Milestone 3 Challenger 2: Seed Schema & AST Guard Compliance

**Type**: Hard Handoff  
**Agent**: `teamwork_preview_challenger_m3_2` (Empirical Challenger & Adversarial Reviewer)  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m3_2`  
**Date**: 2026-09-22  
**Verdict**: **REQUEST_CHANGES**

---

## 1. Observation

1. **Schema & Examples Validation**:
   - Tested all 12 seed tools under `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/**`:
     - 12 `tool.json` files parsed cleanly against JSON Schema draft-07 and `ToolValidator`.
     - 25 examples across 12 `examples.json` files validated cleanly against `inputSchema` (no missing required fields, types match, at least 2 distinct examples per tool).
     - Parameter extraction consistency: All arguments accessed in `code.cs` via `args.Str`, `args.Double`, `args.Int`, `args.Bool`, `args.List`, `args.Obj` match declared properties in `tool.json` (0 undeclared argument reads).
     - Cross-host contamination check: Zero occurrences of `Autodesk.Revit`, `Autodesk.AutoCAD`, `Autodesk.Navisworks`, `ETABSv1`, `SAP2000`, `RobotOM`, `PowerBI`, or `Excel`.

2. **AST Guard & Syntax Compliance**:
   - Roslyn `CSharpSyntaxTree` parsing in script mode:
     - All 12 scripts end with a `ReturnStatementSyntax` returning structured result dictionaries (`return new { ... };`).
     - `ScriptGuard.Check(code, GuardProfile.Tekla)` returned 0 violations across all 12 tools.
     - Direct check on AST tokens and invocations found 0 occurrences of `MessageBox`, `System.Windows.Forms`, `Process.Start`, `RunMacro`, `ShowDialog`, `Picker`, `dynamic`, `await`, `async`, or `#r`/`#load` directives.
     - 0 direct calls to `CommitChanges` (mutation tools allow the bridge's `auto` transaction manager to handle commits and dryRun rollbacks).

3. **Empirical Compilation Against Installed Tekla Structures 2025.0 Assemblies (.NET Framework 4.8)**:
   - When compiled with `ScriptCompiler` using `HostScriptContracts.TeklaImports` and reference assemblies from `C:\Program Files\Tekla Structures\2025.0\bin\`:
     - **7 seeds compiled with 0 errors**:
       - `Geometry/create_beam`: PASS
       - `Geometry/create_column`: PASS
       - `Geometry/create_contour_plate`: PASS
       - `Property/get_part_properties`: PASS
       - `Property/modify_user_properties`: PASS
       - `Rebar/create_rebar_group`: PASS
       - `Rebar/create_single_rebar`: PASS
     - **5 seeds failed with fatal C# compilation errors**:
       - `Drawing/list_drawings/code.cs` (line 2):
         ```text
         Line 2, Col 18: error CS0117: 'Math' does not contain a definition for 'Clamp'
         ```
       - `Model/get_model_info/code.cs` (line 15):
         ```text
         Line 15, Col 28: error CS1061: 'ProjectInfo' does not contain a definition for 'ProjectName'
         ```
       - `Model/select_objects/code.cs` (lines 2, 9):
         ```text
         Line 2, Col 18: error CS0117: 'Math' does not contain a definition for 'Clamp'
         Line 9, Col 70: error CS0117: 'ModelObject.ModelObjectEnum' does not contain a definition for 'REBAR'
         ```
       - `Rebar/get_reinforcement_info/code.cs` (lines 2, 5, 29):
         ```text
         Line 2, Col 18: error CS0117: 'Math' does not contain a definition for 'Clamp'
         Line 5, Col 100: error CS0117: 'ModelObject.ModelObjectEnum' does not contain a definition for 'REBAR'
         Line 29, Col 26: error CS1061: 'Reinforcement' does not contain a definition for 'Size'
         ```
       - `Export/export_ifc/code.cs` (lines 14, 15, 21, 24):
         ```text
         Line 14, Col 73: error CS0117: 'Operation.IFCExportViewTypeEnum' does not contain a definition for 'IFC2X3_COORDINATION_VIEW'
         Line 15, Col 73: error CS0117: 'Operation.IFCExportViewTypeEnum' does not contain a definition for 'IFC4_DESIGN_TRANSFER_VIEW'
         Line 21, Col 65: error CS0117: 'Operation.ExportBasePoint' does not contain a definition for 'BasePointCurrentWorkPlane'
         Line 24, Col 64: error CS0117: 'Operation.IFCExportFlags' does not contain a definition for 'None'
         ```

---

## 2. Logic Chain

1. `HPTekla.McpBridge` runs inside `TeklaStructures.exe 2025.0` on the **.NET Framework 4.8** CLR (`v4.0.30319`).
2. `Math.Clamp` was introduced in .NET Core 2.1 / .NET Standard 2.1 and is absent in .NET Framework 4.8. Any script invoking `Math.Clamp` will fail to compile or throw `MissingMethodException` at runtime in the bridge.
3. In Trimble Tekla Open API (`Tekla.Structures.Model.dll`), reflection proves:
   - `ModelObject.ModelObjectEnum` has no member named `REBAR` (valid members are `SINGLEREBAR`, `REBARGROUP`, etc.).
   - `ProjectInfo` has a property `Name` (`string`), but no property `ProjectName`.
   - `Reinforcement` is an abstract base class with no `Size` property (`Size` is declared on `SingleRebar` and `RebarGroup`).
4. In `Tekla.Structures.Model.Operations.Operation`:
   - `IFCExportViewTypeEnum` contains `DESIGN_TRANSFER_VIEW` and `REFERENCE_VIEW`, but not `IFC4_DESIGN_TRANSFER_VIEW` or `IFC2X3_COORDINATION_VIEW`.
   - `ExportBasePoint` contains `WORK_PLANE` and `GLOBAL`, but not `BasePointCurrentWorkPlane`.
   - `IFCExportFlags` is a value type (`struct`) with no static `None` field.
5. Therefore, while these 5 scripts pass static JSON schema and regex validation on the .NET 10 console server, they will fail immediately when requested by an agent or user through `run_tool` or `execute_tekla_code`.

---

## 3. Caveats

- In-process UI interaction (e.g. WPF status window, Ribbon button invocation) was validated in Milestone 2.
- The 5 identified issues are purely in the C# script bodies (`code.cs`) of the seed library; the tool schemas (`tool.json`) and examples (`examples.json`) are valid and require no modifications.

---

## 4. Conclusion

**Verdict: REQUEST_CHANGES**

The 12 embedded seed tools pass all schema, example, and AST guard constraints, but 5 scripts contain fatal compilation errors against the Tekla Structures 2025.0 API on .NET Framework 4.8. 

### Required Remediations for Worker:
1. `Drawing/list_drawings/code.cs`: Replace `Math.Clamp(args.Int("limit", 50), 1, 200)` with `Math.Max(1, Math.Min(args.Int("limit", 50), 200))`.
2. `Model/get_model_info/code.cs`: Replace `proj.ProjectName` with `proj.Name`.
3. `Model/select_objects/code.cs`: Replace `Math.Clamp` with `Math.Max(1, Math.Min(..., 200))` and replace `ModelObjectEnum.REBAR` with `ModelObjectEnum.SINGLEREBAR` (or query all and filter `is Reinforcement`).
4. `Rebar/get_reinforcement_info/code.cs`: Replace `Math.Clamp`, query `GetAllObjects()`, and extract `Size` via `rebar is SingleRebar sr ? sr.Size : (rebar is RebarGroup rg ? rg.Size : "")`.
5. `Export/export_ifc/code.cs`: Use `Operation.IFCExportViewTypeEnum.DESIGN_TRANSFER_VIEW`, `Operation.ExportBasePoint.WORK_PLANE`, and `default` for flags.

---

## 5. Verification Method

To independently verify these findings:
1. Review the full empirical challenge report at:
   `G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork_preview_challenger_m3_2\report.md`
2. Compile the 12 seeds against Tekla 2025 assemblies on net48 using Roslyn `ScriptCompiler` with `HostScriptContracts.TeklaImports`.
3. Observe that 7 tools compile with 0 diagnostics, while the 5 tools above fail with CS0117 and CS1061.
