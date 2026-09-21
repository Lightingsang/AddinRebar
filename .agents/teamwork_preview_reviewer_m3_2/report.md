# Milestone 3 Review Report — 12 Embedded Seed Tools In-Depth Audit

**Reviewer**: `teamwork_preview_reviewer_m3_2`  
**Roles**: `reviewer`, `critic`  
**Target Subsystem**: `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/` (12 Embedded Seed Tools)  
**Host Environment**: Trimble Tekla Structures 2025.0 (.NET Framework 4.8 / CLR v4.0.30319)  
**Audit Date**: 2026-09-22  

---

## 1. Review Summary

**Verdict**: **`REQUEST_CHANGES`**

### Executive Verdict Summary
While the tool metadata (`tool.json`), JSON schemas, and usage examples (`examples.json`) are well-structured, and 7 of the 12 tools feature genuine and compilable Tekla Open API implementations, an adversarial compilation audit against the real **Trimble Tekla Structures 2025.0** assemblies (`Tekla.Structures.dll`, `Tekla.Structures.Model.dll`, `Tekla.Structures.Drawing.dll`, `Tekla.Structures.Catalogs.dll`) revealed that **5 out of 12 seed tools fail to compile and will crash when executed inside the Tekla Structures 2025 runtime**.

The root causes stem from:
1. Calling modern .NET Core / .NET Standard 2.1 APIs (`Math.Clamp`) that do not exist in **.NET Framework 4.8** (the runtime of Tekla Structures 2025).
2. Using non-existent Tekla Open API enums and properties (`ModelObjectEnum.REBAR`, `ProjectInfo.ProjectName`, `Reinforcement.Size`, `Operation.IFCExportViewTypeEnum.IFC2X3_COORDINATION_VIEW`, `Operation.IFCExportViewTypeEnum.IFC4_DESIGN_TRANSFER_VIEW`, `Operation.ExportBasePoint.BasePointCurrentWorkPlane`, `Operation.IFCExportFlags.None`).
3. Self-certifying code validity based on `dotnet build HPTekla.Mcp.Server.csproj` passing and `tools/list` returning 24 tools. Because seed scripts are embedded as raw text resources (`<Compile Remove=... /> <EmbeddedResource Include=... />`), `dotnet build` never validates the C# syntax or Tekla Open API bindings of `code.cs` files.

---

## 2. Findings

### [Critical] Finding 1: Compilation Failure in `Model/get_model_info` (`ProjectInfo.ProjectName`)
- **What**: Property `proj.ProjectName` does not exist on `Tekla.Structures.Model.ProjectInfo`, producing compiler error `CS1061: 'ProjectInfo' does not contain a definition for 'ProjectName'`.
- **Where**: `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Model/get_model_info/code.cs`, line 15.
- **Why**: In Tekla Open API 2025, the project name property on `ProjectInfo` is named `Name`, not `ProjectName`.
- **Suggestion**:
  ```csharp
  // Replace:
  projectName = proj.ProjectName,
  // With:
  projectName = proj.Name,
  ```

---

### [Critical] Finding 2: Compilation Failure in `Model/select_objects` (`Math.Clamp` & `ModelObjectEnum.REBAR`)
- **What**: 
  1. `Math.Clamp` is not available in .NET Framework 4.8 (`CS0117`).
  2. `ModelObject.ModelObjectEnum.REBAR` does not exist in `ModelObject.ModelObjectEnum` (`CS0117`).
- **Where**: `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Model/select_objects/code.cs`, lines 2 and 9.
- **Why**: 
  1. Tekla Structures 2025 runs on CLR v4.0.30319 (.NET Framework 4.8), where `Math.Clamp` was never added (introduced in .NET Core 2.0).
  2. In Tekla Open API, reinforcement entities are enumerated as `SINGLEREBAR`, `REBARGROUP`, `REBARMESH`, `REBARSTRAND`, etc., not a singular `REBAR`.
- **Suggestion**:
  ```csharp
  // Line 2: replace Math.Clamp with:
  int limit = Math.Max(1, Math.Min(200, args.Int("limit", 50)));

  // Line 6-13: map REBAR to REBARGROUP (or query all and check for Reinforcement):
  if (filter == "BEAM" || filter == "COLUMN") targetEnum = ModelObject.ModelObjectEnum.BEAM;
  else if (filter == "CONTOURPLATE") targetEnum = ModelObject.ModelObjectEnum.CONTOURPLATE;
  else if (filter == "REBAR") targetEnum = ModelObject.ModelObjectEnum.REBARGROUP;
  ```

---

### [Critical] Finding 3: Compilation Failure in `Rebar/get_reinforcement_info` (`Math.Clamp`, `ModelObjectEnum.REBAR`, `Reinforcement.Size`)
- **What**:
  1. `Math.Clamp` fails in .NET Framework 4.8 (`CS0117`).
  2. `ModelObject.ModelObjectEnum.REBAR` does not exist (`CS0117`).
  3. `rebar.Size` fails with `CS1061: 'Reinforcement' does not contain a definition for 'Size'`.
- **Where**: `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Rebar/get_reinforcement_info/code.cs`, lines 2, 5, and 29.
- **Why**:
  1. In .NET Framework 4.8, `Math.Clamp` does not exist.
  2. `ModelObjectEnum` has no `REBAR` member.
  3. The base class `Tekla.Structures.Model.Reinforcement` defines `Grade`, `Name`, `Class`, and `Father`, but does NOT declare `Size`. `Size` is declared on derived classes `SingleRebar` and `BaseRebarGroup` (which `RebarGroup` inherits from).
- **Suggestion**:
  ```csharp
  // Line 2:
  int limit = Math.Max(1, Math.Min(200, args.Int("limit", 50)));

  // Line 5: enumerate all objects and filter:
  var modelSelector = model.GetModelObjectSelector();
  ModelObjectEnumerator enumerator = modelSelector.GetAllObjects();

  // Line 20-35: extract size from derived classes:
  string size = "";
  int quantity = 1;
  if (rebar is SingleRebar sr)
  {
      size = sr.Size;
      quantity = 1;
  }
  else if (rebar is BaseRebarGroup group)
  {
      size = group.Size;
      if (group is RebarGroup rg)
      {
          quantity = rg.Polygons.Count > 0 ? (int)Math.Max(1, rg.GetNumberOfRebars()) : 1;
      }
  }
  ```

---

### [Critical] Finding 4: Compilation Failure in `Drawing/list_drawings` (`Math.Clamp`)
- **What**: `Math.Clamp` fails in .NET Framework 4.8 (`CS0117`).
- **Where**: `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Drawing/list_drawings/code.cs`, line 2.
- **Why**: Runtime target is .NET Framework 4.8.
- **Suggestion**:
  ```csharp
  int limit = Math.Max(1, Math.Min(200, args.Int("limit", 50)));
  ```

---

### [Critical] Finding 5: Compilation Failure in `Export/export_ifc` (Invalid Enums and Struct Usage)
- **What**:
  1. `CS0117: 'Operation.IFCExportViewTypeEnum' does not contain a definition for 'IFC2X3_COORDINATION_VIEW'`
  2. `CS0117: 'Operation.IFCExportViewTypeEnum' does not contain a definition for 'IFC4_DESIGN_TRANSFER_VIEW'`
  3. `CS0117: 'Operation.ExportBasePoint' does not contain a definition for 'BasePointCurrentWorkPlane'`
  4. `CS0117: 'Operation.IFCExportFlags' does not contain a definition for 'None'`
- **Where**: `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Export/export_ifc/code.cs`, lines 14, 15, 21, 24.
- **Why**:
  1. `IFCExportViewTypeEnum` values in Tekla Structures 2025 are `REFERENCE_VIEW`, `DESIGN_TRANSFER_VIEW`, `PRECAST_VIEW`, `MEP_REFERENCE_VIEW`, etc. (no `IFC2X3_...` or `IFC4_...` prefixes).
  2. `ExportBasePoint` values are `WORK_PLANE`, `GLOBAL`, `BASE_POINT`.
  3. `IFCExportFlags` is a class/struct of boolean switches (not an enum). Passing `None` causes a syntax error.
- **Suggestion**:
  ```csharp
  var exportView = format == "IFC2X3"
      ? Tekla.Structures.Model.Operations.Operation.IFCExportViewTypeEnum.REFERENCE_VIEW
      : Tekla.Structures.Model.Operations.Operation.IFCExportViewTypeEnum.DESIGN_TRANSFER_VIEW;

  bool ok = Tekla.Structures.Model.Operations.Operation.CreateIFC4ExportFromSelected(
      path,
      exportView,
      new List<string>(),
      Tekla.Structures.Model.Operations.Operation.ExportBasePoint.WORK_PLANE,
      "",
      "",
      new Tekla.Structures.Model.Operations.Operation.IFCExportFlags(),
      ""
  );
  ```

---

### [Major] Finding 6: Integrity & Verification Gap (Self-Certifying Untested Embedded Code)
- **What**: The worker report claimed: *"Every seed tool contains genuine C# code utilizing Tekla Open API idioms... All requirements of DISPATCH.md and ORIGINAL_REQUEST.md have been met."*
- **Where**: `teamwork_preview_worker_m3/handoff.md` Section 1, 2, 4.
- **Why**: Verification only checked `dotnet build` on `HPTekla.Mcp.Server.csproj` and ran `verify_stdio.py`. Because `HPTekla.Mcp.Server.csproj` explicitly removes `*.cs` from compilation (`<Compile Remove="Registry\SeedLibrary\**\*.cs" />`) and embeds them as strings, `dotnet build` gives a false sense of security. The C# scripts were never compiled against the installed Tekla 2025 assemblies prior to marking the task complete.

---

## 3. Seed Tool Status Matrix

| # | Category | Tool Name | Schema & tool.json | Examples | Tekla Open API C# Compilation | Verdict |
|---|---|---|---|---|---|---|
| 1 | `Model` | `get_model_info` | VALID | VALID (2) | **FAIL** (CS1061 `ProjectInfo.ProjectName`) | **REVISE** |
| 2 | `Model` | `select_objects` | VALID | VALID (2) | **FAIL** (CS0117 `Math.Clamp`, `REBAR`) | **REVISE** |
| 3 | `Property` | `get_part_properties` | VALID | VALID (2) | **PASS** (Clean Tekla Open API) | **ACCEPT** |
| 4 | `Geometry` | `create_beam` | VALID | VALID (2) | **PASS** (Clean Tekla Open API) | **ACCEPT** |
| 5 | `Geometry` | `create_column` | VALID | VALID (2) | **PASS** (Clean Tekla Open API) | **ACCEPT** |
| 6 | `Geometry` | `create_contour_plate` | VALID | VALID (2) | **PASS** (Clean Tekla Open API) | **ACCEPT** |
| 7 | `Rebar` | `create_rebar_group` | VALID | VALID (2) | **PASS** (Clean Tekla Open API) | **ACCEPT** |
| 8 | `Rebar` | `create_single_rebar` | VALID | VALID (2) | **PASS** (Clean Tekla Open API) | **ACCEPT** |
| 9 | `Property` | `modify_user_properties` | VALID | VALID (2) | **PASS** (Clean Tekla Open API) | **ACCEPT** |
| 10 | `Rebar` | `get_reinforcement_info` | VALID | VALID (2) | **FAIL** (CS0117 `Math.Clamp`, `REBAR`; CS1061 `Size`) | **REVISE** |
| 11 | `Drawing` | `list_drawings` | VALID | VALID (2) | **FAIL** (CS0117 `Math.Clamp`) | **REVISE** |
| 12 | `Export` | `export_ifc` | VALID | VALID (2) | **FAIL** (CS0117 `IFCExportViewTypeEnum`, `ExportBasePoint`, `IFCExportFlags`) | **REVISE** |

**Summary**:
- Total Seed Tools: **12**
- Fully Passing: **7** (58.3%)
- Requiring Fixes: **5** (41.7%)

---

## 4. Verified Claims

- `HPTekla.Mcp.Server.csproj` builds cleanly in Release and Debug modes (`dotnet build` exits with code 0).
- `HPTekla.Mcp.Server.exe` starts and successfully handles MCP stdio handshake (`initialize` -> `tools/list`).
- Tool surface exposed over MCP stdio is exactly **24 tools**:
  - 4 Core tools: `cancel_execution`, `execute_tekla_code`, `get_tekla_context`, `inspect_type`.
  - 8 Registry tools: `get_run`, `get_tool`, `manage_tool`, `propose_tool`, `publish_tool`, `run_tool`, `search_tools`, `test_tool`.
  - 12 Seed tools: `get_model_info`, `select_objects`, `get_part_properties`, `create_beam`, `create_column`, `create_contour_plate`, `create_rebar_group`, `create_single_rebar`, `modify_user_properties`, `get_reinforcement_info`, `list_drawings`, `export_ifc`.
- `resources/list` returns 3 resources: `registry://tools`, `tekla://selection`, `tekla://model/info`.
- `prompts/list` returns 4 prompts: `toolify_run`, `tekla_query_template`, `tekla_modify_template`, `tekla_rebar_template`.
- All 12 `tool.json` files contain required fields (`host: "tekla"`, `hostVersions: ["2025"]`, valid category, transaction mode, and timeout).
- All 12 `examples.json` files contain valid examples conforming to schema types and required properties.
- None of the 12 seeds use prohibited identifiers (`MessageBox`, `Process.Start`, `#r`, `#load`, direct `model.CommitChanges()`).
- The 5 fixes proposed above were verified independently against `C:\Program Files\Tekla Structures\2025.0\bin` using Roslyn compiler and compiled with 0 errors.

---

## 5. Coverage Gaps & Unverified Items

- **Live In-Process Execution**: Live script execution inside a running `tekla.exe` instance was not tested because Tekla Structures was not actively running during this audit. The live pipe execution will be verified during Milestone 5 live harness verification.

---

## 6. Required Actions Before Approval

1. Apply the 5 verified C# fixes to:
   - `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Model/get_model_info/code.cs`
   - `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Model/select_objects/code.cs`
   - `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Rebar/get_reinforcement_info/code.cs`
   - `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Drawing/list_drawings/code.cs`
   - `HPTekla/HPTekla.Mcp.Server/Registry/SeedLibrary/Export/export_ifc/code.cs`
2. Re-verify compilation of all 12 seeds against Tekla 2025 assemblies to achieve 100% compilation pass rate (12/12).
3. Re-run `HPTekla.Mcp.Server` build and stdio verification.
