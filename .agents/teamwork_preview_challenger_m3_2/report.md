# Empirical Adversarial Challenge Report — Milestone 3: Embedded Seed Tools

- **Author**: Challenger 2 (`teamwork_preview_challenger_m3_2`) — Empirical Challenger & Adversarial Reviewer
- **Target**: Milestone 3 (`HPTekla.Mcp.Server/Registry/SeedLibrary/**`)
- **Repo Root**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar`
- **Execution Runtimes Tested**: 
  - .NET 10.0.300 (`net10.0`) via Roslyn `Microsoft.CodeAnalysis.CSharp` 4.12.0
  - Desktop CLR `v4.0.30319` (.NET Framework 4.8 / Tekla Structures 2025.0 assemblies from `C:\Program Files\Tekla Structures\2025.0\bin\`)
- **Date**: 2026-09-22
- **Verdict**: **REQUEST_CHANGES**

---

## 1. Executive Summary & Verdict

**Overall Risk Assessment**: **CRITICAL**  
**Verdict**: **REQUEST_CHANGES** (5 fatal compile-time / runtime defects in 5 seed scripts when executing against the Tekla Structures 2025.0 .NET Framework 4.8 Open API)

Milestone 3 delivered 12 embedded seed tools under `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/`.
Our adversarial challenge verified that at the static metadata and server registry level, the implementation is well-structured:
- All 12 `tool.json` files parse cleanly against JSON Schema draft-07 and `ToolValidator`.
- All 25 examples across 12 `examples.json` validate cleanly against `inputSchema` (no missing required fields, types match, at least 2 distinct examples per tool).
- All 12 `code.cs` files pass `ScriptGuard.Check` with `GuardProfile.Tekla` (zero occurrences of `MessageBox`, `System.Windows.Forms`, `Process.Start`, `RunMacro`, `ShowDialog`, `CommitChanges`, `#r`, or `#load`).
- All 12 scripts terminate with explicit return statements (`return new { ... };`).
- Zero cross-host contamination (`Autodesk.Revit`, `Autodesk.AutoCAD`, `Autodesk.Navisworks`, `ETABSv1`, `SAP2000`, `Robot`, `PowerBI`, `Excel`).
- Parameter extraction consistency is strictly maintained (0 undeclared argument reads).

**However**, when the seed scripts were empirically compiled against the actual installed Tekla Structures 2025.0 assemblies on .NET Framework 4.8 (the runtime environment inside `TeklaStructures.exe` and `HPTekla.McpBridge`), **5 out of the 12 seed tools failed compilation with fatal C# errors**:

| Tool | Category | Compile Result | Root Cause |
|---|---|---|---|
| `Drawing/list_drawings` | Drawing | **FAIL** | `Math.Clamp` does not exist in .NET Framework 4.8 (`CS0117`). |
| `Export/export_ifc` | Export | **FAIL** | Hallucinated enum values on `Operation.IFCExportViewTypeEnum`, `ExportBasePoint`, and invalid struct usage `IFCExportFlags.None` (`CS0117`). |
| `Geometry/create_beam` | Geometry | **PASS** | Compiles cleanly against `Tekla.Structures.Model.Beam`. |
| `Geometry/create_column` | Geometry | **PASS** | Compiles cleanly against `Tekla.Structures.Model.Beam`. |
| `Geometry/create_contour_plate` | Geometry | **PASS** | Compiles cleanly against `Tekla.Structures.Model.ContourPlate`. |
| `Model/get_model_info` | Model | **FAIL** | Property `ProjectInfo.ProjectName` does not exist in Tekla Open API; correct property is `proj.Name` (`CS1061`). |
| `Model/select_objects` | Model | **FAIL** | `Math.Clamp` does not exist in net48; `ModelObject.ModelObjectEnum.REBAR` does not exist in Tekla Open API (`CS0117`). |
| `Property/get_part_properties` | Property | **PASS** | Compiles cleanly against `Tekla.Structures.Model.Part`. |
| `Property/modify_user_properties` | Property | **PASS** | Compiles cleanly against `Tekla.Structures.Model.ModelObject`. |
| `Rebar/create_rebar_group` | Rebar | **PASS** | Compiles cleanly against `Tekla.Structures.Model.RebarGroup`. |
| `Rebar/create_single_rebar` | Rebar | **PASS** | Compiles cleanly against `Tekla.Structures.Model.SingleRebar`. |
| `Rebar/get_reinforcement_info` | Rebar | **FAIL** | `Math.Clamp` does not exist in net48; `ModelObjectEnum.REBAR` does not exist; base `Reinforcement` has no `Size` property (`CS0117`, `CS1061`). |

---

## 2. Empirical Verification Test Harness & Methodology

The challenge was executed empirically using two dedicated test harnesses:
1. **Host-Neutral Schema & AST Guard Verifier** (.NET 10.0.300):
   - Evaluated `SeedInstaller.LoadSeeds` (36 embedded manifest resources).
   - Validated JSON schemas and payloads against JSON Schema draft-07 and `ToolValidator`.
   - Analyzed Roslyn syntax trees for directives (`#r`, `#load`), terminal `ReturnStatementSyntax`, and forbidden tokens from `GuardProfile.Tekla`.
   - Verified parameter extraction consistency using `ScriptAnalyzer.Analyze(code, AnalyzerProfile.Tekla)`.
2. **Tekla Structures 2025.0 .NET Framework 4.8 Compilation Harness** (Desktop CLR 4.8.9181.0):
   - Configured `ScriptCompiler` using `HostScriptContracts.TeklaImports` and reference assemblies loaded from `C:\Program Files\Tekla Structures\2025.0\bin\`:
     - `Tekla.Structures.dll`
     - `Tekla.Structures.Model.dll`
     - `Tekla.Structures.Drawing.dll`
     - `Tekla.Structures.Catalogs.dll`
     - `HPRebar.McpBridge.Core.dll`
     - `HPRebar.Mcp.Contracts.dll`
   - Globals: `TeklaScriptGlobals` (`model`, `selector`, `ct`, `log`, `progress`, `args`).

---

## 3. Detailed Challenge Findings (Defects)

### [Critical] Challenge 1: `Math.Clamp` Incompatibility with .NET Framework 4.8

- **Affected Tools**:
  - `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Drawing/list_drawings/code.cs` (line 2)
  - `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Model/select_objects/code.cs` (line 2)
  - `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Rebar/get_reinforcement_info/code.cs` (line 2)
- **Code snippet**:
  ```csharp
  int limit = Math.Clamp(args.Int("limit", 50), 1, 200);
  ```
- **Observed Error**:
  ```text
  Line 2, Col 18: error CS0117: 'Math' does not contain a definition for 'Clamp'
  ```
- **Root Cause**:
  `Math.Clamp` was added in .NET Core 2.1 / .NET Standard 2.1. Tekla Structures 2025.0 and `HPTekla.McpBridge` run on **.NET Framework 4.8** (`net48`), where `Math.Clamp` does not exist.
- **Remediation**:
  Replace `Math.Clamp(val, 1, 200)` with:
  ```csharp
  int rawLimit = args.Int("limit", 50);
  int limit = rawLimit < 1 ? 1 : (rawLimit > 200 ? 200 : rawLimit);
  ```
  or `Math.Max(1, Math.Min(args.Int("limit", 50), 200));`.

---

### [Critical] Challenge 2: Non-Existent Enum `ModelObject.ModelObjectEnum.REBAR`

- **Affected Tools**:
  - `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Model/select_objects/code.cs` (line 9)
  - `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Rebar/get_reinforcement_info/code.cs` (line 5)
- **Code snippet**:
  ```csharp
  // in select_objects:
  else if (filter == "REBAR") targetEnum = ModelObject.ModelObjectEnum.REBAR;

  // in get_reinforcement_info:
  ModelObjectEnumerator enumerator = modelSelector.GetAllObjectsWithType(ModelObject.ModelObjectEnum.REBAR);
  ```
- **Observed Error**:
  ```text
  error CS0117: 'ModelObject.ModelObjectEnum' does not contain a definition for 'REBAR'
  ```
- **Root Cause**:
  Reflection over `Tekla.Structures.Model.ModelObject.ModelObjectEnum` confirms that there is no `REBAR` member. Tekla defines granular reinforcement enums:
  - `SINGLEREBAR`
  - `REBARGROUP`
  - `REBARMESH`
  - `REBARSTRAND`
  - `REBAR_SPLICE`
  - `CIRCLEREBAR`
  - `CIRCLE_REBARGROUP`
  - `CURVED_REBARGROUP`
  - `REBAR_SET`
- **Remediation**:
  - In `select_objects`: query `ModelObject.ModelObjectEnum.UNKNOWN` or check both `SINGLEREBAR` and `REBARGROUP`, or filter `GetAllObjects()` using `mo is Reinforcement`.
  - In `get_reinforcement_info`: Use `modelSelector.GetAllObjects()` and filter `if (enumerator.Current is Reinforcement rebar)`.

---

### [Critical] Challenge 3: Non-Existent Property `ProjectInfo.ProjectName`

- **Affected Tool**:
  - `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Model/get_model_info/code.cs` (line 15)
- **Code snippet**:
  ```csharp
  var proj = model.GetProjectInfo();
  projData = new
  {
      projectName = proj.ProjectName,
      projectNumber = proj.ProjectNumber,
      designer = proj.Designer,
      builder = proj.Builder
  };
  ```
- **Observed Error**:
  ```text
  Line 15, Col 28: error CS1061: 'ProjectInfo' does not contain a definition for 'ProjectName' and no accessible extension method 'ProjectName' accepting a first argument of type 'ProjectInfo' could be found
  ```
- **Root Cause**:
  In `Tekla.Structures.Model.ProjectInfo`, the project name property is named `Name`, NOT `ProjectName`.
  Properties on `ProjectInfo`:
  `Name`, `ProjectNumber`, `Description`, `Designer`, `Builder`, `Location`, `Address`, `Town`, `PostalCode`, `Country`, `StartDate`, `EndDate`.
- **Remediation**:
  Change line 15 to:
  ```csharp
  projectName = proj.Name,
  ```

---

### [Critical] Challenge 4: Non-Existent Base Class Property `Reinforcement.Size`

- **Affected Tool**:
  - `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Rebar/get_reinforcement_info/code.cs` (line 29)
- **Code snippet**:
  ```csharp
  if (enumerator.Current is Reinforcement rebar)
  {
      ...
      size = rebar.Size,
  ```
- **Observed Error**:
  ```text
  Line 29, Col 26: error CS1061: 'Reinforcement' does not contain a definition for 'Size' and no accessible extension method 'Size' accepting a first argument of type 'Reinforcement' could be found
  ```
- **Root Cause**:
  In Tekla Open API, `Tekla.Structures.Model.Reinforcement` is an abstract base class. It defines `Grade`, `Name`, `Class`, and `Father`, but does **not** define `Size`.
  `Size` is defined on derived classes:
  - `SingleRebar.Size` (`string`)
  - `RebarGroup.Size` (inherited from `BaseRebarGroup.Size`, `string`)
- **Remediation**:
  Extract the bar size conditionally:
  ```csharp
  string size = rebar is SingleRebar sr ? sr.Size : (rebar is RebarGroup rg ? rg.Size : "");
  ```

---

### [Critical] Challenge 5: Invalid Enums and Struct Usage in `export_ifc`

- **Affected Tool**:
  - `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Export/export_ifc/code.cs` (lines 13–24)
- **Code snippet**:
  ```csharp
  var exportView = format == "IFC2X3"
      ? Tekla.Structures.Model.Operations.Operation.IFCExportViewTypeEnum.IFC2X3_COORDINATION_VIEW
      : Tekla.Structures.Model.Operations.Operation.IFCExportViewTypeEnum.IFC4_DESIGN_TRANSFER_VIEW;

  bool ok = Tekla.Structures.Model.Operations.Operation.CreateIFC4ExportFromSelected(
      path,
      exportView,
      new List<string>(),
      Tekla.Structures.Model.Operations.Operation.ExportBasePoint.BasePointCurrentWorkPlane,
      "",
      "",
      Tekla.Structures.Model.Operations.Operation.IFCExportFlags.None,
      ""
  );
  ```
- **Observed Errors**:
  ```text
  Line 14, Col 73: error CS0117: 'Operation.IFCExportViewTypeEnum' does not contain a definition for 'IFC2X3_COORDINATION_VIEW'
  Line 15, Col 73: error CS0117: 'Operation.IFCExportViewTypeEnum' does not contain a definition for 'IFC4_DESIGN_TRANSFER_VIEW'
  Line 21, Col 65: error CS0117: 'Operation.ExportBasePoint' does not contain a definition for 'BasePointCurrentWorkPlane'
  Line 24, Col 64: error CS0117: 'Operation.IFCExportFlags' does not contain a definition for 'None'
  ```
- **Root Cause**:
  Inspection of `Tekla.Structures.Model.Operations.Operation` reveals:
  1. `IFCExportViewTypeEnum` values are: `UNDEFINED`, `REFERENCE_VIEW`, `DESIGN_TRANSFER_VIEW`, `PRECAST_VIEW`, `MEP_REFERENCE_VIEW`, `MEP_DESIGN_TRANSFER_VIEW`, `BRIDGE_VIEW`.
  2. `ExportBasePoint` values are: `GLOBAL`, `WORK_PLANE`, `BASE_POINT`.
  3. `IFCExportFlags` is a value type (`struct`), not an enum. It does not have a `None` field; default value is `default` or `new Tekla.Structures.Model.Operations.Operation.IFCExportFlags()`.
- **Remediation**:
  Update lines 13–25 to:
  ```csharp
  var exportView = Tekla.Structures.Model.Operations.Operation.IFCExportViewTypeEnum.DESIGN_TRANSFER_VIEW;

  bool ok = Tekla.Structures.Model.Operations.Operation.CreateIFC4ExportFromSelected(
      path,
      exportView,
      new List<string>(),
      Tekla.Structures.Model.Operations.Operation.ExportBasePoint.WORK_PLANE,
      "",
      "",
      default,
      ""
  );
  ```

---

## 4. Verification Table of Passed Seed Checks

| Seed Tool | Schema (draft-07) | Examples Valid | Return Stmt | GuardProfile.Tekla | Cross-Host Clean | ArgKeys Match |
|---|:---:|:---:|:---:|:---:|:---:|:---:|
| `Drawing/list_drawings` | PASS | PASS (2) | PASS | PASS | PASS | PASS |
| `Export/export_ifc` | PASS | PASS (2) | PASS | PASS | PASS | PASS |
| `Geometry/create_beam` | PASS | PASS (2) | PASS | PASS | PASS | PASS |
| `Geometry/create_column` | PASS | PASS (2) | PASS | PASS | PASS | PASS |
| `Geometry/create_contour_plate` | PASS | PASS (2) | PASS | PASS | PASS | PASS |
| `Model/get_model_info` | PASS | PASS (2) | PASS | PASS | PASS | PASS |
| `Model/select_objects` | PASS | PASS (2) | PASS | PASS | PASS | PASS |
| `Property/get_part_properties` | PASS | PASS (2) | PASS | PASS | PASS | PASS |
| `Property/modify_user_properties` | PASS | PASS (2) | PASS | PASS | PASS | PASS |
| `Rebar/create_rebar_group` | PASS | PASS (2) | PASS | PASS | PASS | PASS |
| `Rebar/create_single_rebar` | PASS | PASS (2) | PASS | PASS | PASS | PASS |
| `Rebar/get_reinforcement_info` | PASS | PASS (2) | PASS | PASS | PASS | PASS |

---

## 5. Unchallenged Areas

- **In-process GUI execution in running TeklaStructures.exe process**:
  Per project roadmap, live UI testing inside Tekla Structures is designated for the unattended live test harness (M4 / Milestone 4). Static and out-of-process compilation against Tekla 2025.0 Open API assemblies is 100% complete.

---

## 6. Actionable Fix Recommendations for Worker

The worker should apply the following non-breaking fixes to the 5 failing `code.cs` files:

1. **`Drawing/list_drawings/code.cs`**:
   Replace line 2 with:
   ```csharp
   int rawLimit = args.Int("limit", 50);
   int limit = Math.Max(1, Math.Min(rawLimit, 200));
   ```

2. **`Model/get_model_info/code.cs`**:
   Replace line 15 with:
   ```csharp
   projectName = proj.Name,
   ```

3. **`Model/select_objects/code.cs`**:
   Replace line 2 with:
   ```csharp
   int rawLimit = args.Int("limit", 50);
   int limit = Math.Max(1, Math.Min(rawLimit, 200));
   ```
   Replace line 9 with:
   ```csharp
   else if (filter == "REBAR") targetEnum = ModelObject.ModelObjectEnum.SINGLEREBAR;
   ```
   (or query `GetAllObjects()` with `is Reinforcement`).

4. **`Rebar/get_reinforcement_info/code.cs`**:
   Replace line 2 with:
   ```csharp
   int rawLimit = args.Int("limit", 50);
   int limit = Math.Max(1, Math.Min(rawLimit, 200));
   ```
   Replace line 5 with:
   ```csharp
   ModelObjectEnumerator enumerator = modelSelector.GetAllObjects();
   ```
   Replace line 29 with:
   ```csharp
   string size = rebar is SingleRebar sr ? sr.Size : (rebar is RebarGroup rg ? rg.Size : "");
   ```

5. **`Export/export_ifc/code.cs`**:
   Update lines 13–26 to use:
   ```csharp
   var exportView = Tekla.Structures.Model.Operations.Operation.IFCExportViewTypeEnum.DESIGN_TRANSFER_VIEW;

   bool ok = Tekla.Structures.Model.Operations.Operation.CreateIFC4ExportFromSelected(
       path,
       exportView,
       new List<string>(),
       Tekla.Structures.Model.Operations.Operation.ExportBasePoint.WORK_PLANE,
       "",
       "",
       default,
       ""
   );
   ```

---

## 7. Conclusion

Because 5 out of the 12 embedded seed scripts contain fatal C# compilation errors when targeted against the Tekla Structures 2025.0 API runtime, the work product cannot execute in production without modifications.

**Verdict**: **REQUEST_CHANGES**
