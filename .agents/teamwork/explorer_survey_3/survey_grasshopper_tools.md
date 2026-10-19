# Survey & Technical Specification: HPGrasshopper MCP Bridge

**Date**: 2026-10-10  
**Author**: `explorer_survey_3` (Rhino Grasshopper API & Seeds Explorer)  
**Target Solution**: `HPGrasshopper.slnx`  
**Host Environment**: Autodesk Revit 2026 (.NET 8.0) + Rhino.Inside.Revit (R8) + Rhinoceros 8 (Grasshopper 8.26)

---

## 1. Executive Summary & Architecture Context

`HPGrasshopper` is an in-process Model Context Protocol (MCP) subsystem bridging AI coding agents directly into **Autodesk Revit 2026** and **Rhino 8 Grasshopper** via **Rhino.Inside.Revit**. It enables bi-directional synchronization, programmatic canvas graph inspection and authoring, headless Grasshopper Player execution, and Roslyn C# scripting across both Revit and Rhino/Grasshopper geometry engines.

Following the established Clean Code Governance and architectural patterns in `McpShared`, `HPGrasshopper` adopts:
1. **Host-neutral contracts & engine**: Consumes `HPRebar.Mcp.Contracts`, `HPRebar.McpBridge.Core`, and `HPRebar.Mcp.Server.Core`.
2. **Dual-lifecycle In-Process Bridge**: Runs inside `Revit.exe` via an add-in / view extension that opens Named Pipe `\\.\pipe\hpgrasshopper-mcp-2026`.
3. **Dual-Layer Safety**: Revit `TransactionGroup` rollback for `dryRun=true` on Revit elements combined with Grasshopper `GH_UndoRecord` on canvas modifications.
4. **Strict Tool Surface**: 5 Core Tools, 8 Dynamic Registry Meta Tools, and 12 Embedded Seeds specialized for Grasshopper ↔ Revit synchronization.

---

## 2. Rhino.Inside.Revit & Grasshopper APIs

### 2.1 Assembly References & Discovery on Dev Environment

Live inspection of the host system confirmed that **Rhino 8** and **Rhino.Inside.Revit for Revit 2026** are installed and functional:

| Assembly | Physical Location | Target Framework / Version | Source |
|---|---|---|---|
| `RhinoCommon.dll` | `C:\Program Files\Rhino 8\System\netcore\RhinoCommon.dll` | netcore / v8.26.25349.19001 | Installed Rhino 8 |
| `Grasshopper.dll` | `C:\Program Files\Rhino 8\Plug-ins\Grasshopper\Grasshopper.dll` | net8.0-windows / v8.26.25349.19001 | Installed Rhino 8 |
| `GH_IO.dll` | `C:\Program Files\Rhino 8\Plug-ins\Grasshopper\GH_IO.dll` | netcore / v8.26.25349.19001 | Installed Rhino 8 |
| `RhinoInside.Revit.dll` | `C:\ProgramData\Autodesk\Revit\Addins\2026\RhinoInside.Revit\R8\RhinoInside.Revit.dll` | net8.0 / v1.35.9525.16240 | Installed R.I.R Addin |
| `RhinoInside.Revit.GH.gha` | `C:\ProgramData\Autodesk\Revit\Addins\2026\RhinoInside.Revit\R8\RhinoInside.Revit.GH.gha` | net8.0-windows / v1.35.9525.16240 | Installed R.I.R Addin |
| `RevitAPI.dll` | `C:\Program Files\Autodesk\Revit 2026\RevitAPI.dll` | net8.0 / v26.0.4.0 | Installed Revit 2026 |
| `RevitAPIUI.dll` | `C:\Program Files\Autodesk\Revit 2026\RevitAPIUI.dll` | net8.0-windows / v26.0.4.0 | Installed Revit 2026 |

#### MSBuild Dependency Resolution Strategy (`Directory.Build.props`)
To ensure seamless compilation on local development workstations without polluting `bin/` with host DLLs, `HPGrasshopper` must define properties modeled after `HPDynamo`:
```xml
<PropertyGroup>
  <RevitYear Condition="'$(RevitYear)' == ''">2026</RevitYear>
  <Rhino8InstallDir Condition="'$(Rhino8InstallDir)' == ''">$(ProgramW6432)\Rhino 8\</Rhino8InstallDir>
  <Rhino8SystemDir>$(Rhino8InstallDir)System\netcore\</Rhino8SystemDir>
  <GrasshopperDir>$(Rhino8InstallDir)Plug-ins\Grasshopper\</GrasshopperDir>
  <RhinoInsideDir Condition="'$(RhinoInsideDir)' == ''">$(ProgramData)\Autodesk\Revit\Addins\$(RevitYear)\RhinoInside.Revit\R8\</RhinoInsideDir>
  <RevitInstallDir Condition="'$(RevitInstallDir)' == ''">$(ProgramW6432)\Autodesk\Revit $(RevitYear)\</RevitInstallDir>
</PropertyGroup>

<ItemDefinitionGroup>
  <Reference>
    <Private>false</Private>
  </Reference>
</ItemDefinitionGroup>
```

For continuous integration (CI) environments without a full Rhino 8 installation, unit test projects (`HPGrasshopper.Mcp.Server.Tests`) can reference official NuGet packages:
- `RhinoCommon` v8.13+ (`ExcludeAssets="runtime"`)
- `Grasshopper` v8.13+ (`ExcludeAssets="runtime"`)
- `Nice3point.Revit.Api.RevitAPI` (Revit 2026 reference assemblies)

---

### 2.2 Accessing Live Host Context Inside Revit Process

Inside the `Revit.exe` process hosting Rhino.Inside.Revit:

1. **Active Revit Document**:
   - `RhinoInside.Revit.Revit.ActiveDBDocument` -> returns `Autodesk.Revit.DB.Document`.
   - `RhinoInside.Revit.Revit.ActiveUIDocument` -> returns `Autodesk.Revit.UI.UIDocument`.
   - `RhinoInside.Revit.Revit.ActiveUIApplication` -> returns `Autodesk.Revit.UI.UIApplication`.

2. **Active Rhino Document**:
   - `Rhino.RhinoDoc.ActiveDoc` -> returns `Rhino.RhinoDoc`.
   - `Grasshopper.Instances.ActiveRhinoDoc` -> returns active `RhinoDoc` associated with Grasshopper.

3. **Active Grasshopper Canvas & Document**:
   - `Grasshopper.Instances.ActiveCanvas` -> returns `Grasshopper.GUI.Canvas.GH_Canvas` (active canvas control).
   - `Grasshopper.Instances.ActiveDocument` -> returns `Grasshopper.Kernel.GH_Document` (active graph).
   - `Grasshopper.Instances.DocumentServer` -> returns `GH_DocumentServer` (manages loaded documents in memory).
   - `Grasshopper.Instances.DocumentEditor` -> returns `GH_DocumentEditor` (main UI window).

4. **Thread Dispatching & Execution Synchronization**:
   - **Revit UI/Main Thread**: External requests arriving via the Named Pipe thread must be dispatched to Revit's main thread via an `IExternalEventHandler` (`ExternalEvent.Create(...)`) or `RevitTask` queue.
   - **Rhino/Grasshopper Thread Context**: Thunked via `RhinoInside.Revit.Rhinoceros.InvokeInHostContext(Action)` to guarantee execution within Rhino's synchronized thread boundary.

---

## 3. The 5 Core MCP Tools Specification

The 5 Core tools are registered directly on the MCP server and dispatched to the bridge.

### 3.1 `read_grasshopper_canvas`

* **Description**: Inspects the active Grasshopper canvas and returns a structured JSON model of all components, parameters, wires, coordinates, solver state, warnings, errors, and optional data values.
* **Input Schema**:
  ```json
  {
    "type": "object",
    "properties": {
      "includeValues": { "type": "boolean", "description": "Whether to extract summary preview values from volatile data. Default false." },
      "selectedOnly": { "type": "boolean", "description": "Only return selected components. Default false." },
      "categoryFilter": { "type": "string", "description": "Optional category filter (e.g. 'Params', 'Math', 'Revit')." }
    }
  }
  ```
* **Output Structure**:
  - `document`: Name, FilePath, IsModified, ObjectCount, SolutionState (`Valid`, `Postponed`, `Aborted`), Profiler runtime in ms.
  - `components`: Array of objects:
    - `guid`: Component InstanceGuid (`Guid`).
    - `componentId`: Component type Guid (`Guid`).
    - `name`: Component full name (e.g., `Number Slider`, `Construct Point`).
    - `nickName`: User display nickname.
    - `category` & `subCategory`: Palette grouping.
    - `bounds`: `{ "x": double, "y": double, "width": double, "height": double }`.
    - `status`: Solver state, message levels (`Blank`, `Remark`, `Warning`, `Error`).
    - `messages`: Array of error/warning strings.
    - `inputs`: Array of parameters (Name, NickName, TypeName, SourceGuids, Optional).
    - `outputs`: Array of parameters (Name, NickName, TypeName, RecipientGuids, VolatileSummary).
  - `wires`: Array of wire linkages `{ "fromComponent", "fromOutput", "toComponent", "toInput" }`.

---

### 3.2 `run_grasshopper_player`

* **Description**: Opens and solves a Grasshopper definition file (`.gh` or `.ghx`) in Grasshopper Player mode with user-provided JSON input values, extracting computed outputs without requiring interactive UI prompts.
* **Input Schema**:
  ```json
  {
    "type": "object",
    "required": ["filePath"],
    "properties": {
      "filePath": { "type": "string", "description": "Absolute path to .gh or .ghx file." },
      "inputs": { 
        "type": "object", 
        "description": "Key-value pairs mapping parameter NickName or Guid to input values (numbers, strings, booleans, points [x,y,z], or Revit element IDs)." 
      },
      "outputs": {
        "type": "array",
        "items": { "type": "string" },
        "description": "List of output parameter NickNames to retrieve. If empty, retrieves all terminal outputs."
      },
      "headless": { "type": "boolean", "description": "Run in background document without docking to active canvas. Default true." }
    }
  }
  ```
* **Execution Algorithm**:
  1. Open definition via `GH_Archive.ReadFromFile(filePath)` and `ExtractObject(ghDoc, "Definition")`.
  2. Locate input parameters matching dictionary keys in `ghDoc.Objects`.
  3. Inject values:
     - For primitives: assign `GH_Number`, `GH_String`, `GH_Boolean`, `GH_Point` to `IGH_Param.PersistentData`.
     - For Revit element inputs: wrap element in `RhinoInside.Revit.GH.Types.Element` and inject.
  4. Invoke `ghDoc.NewSolution(expireAllObjects: true)`.
  5. Inspect target output parameters' `VolatileData` tree.
  6. Serialize geometry (Points, Curves, Breps) as GeoJSON/Rhino JSON and scalar values to JSON output.
  7. Dispose headless document cleanly.

---

### 3.3 `batch_edit_grasshopper_canvas`

* **Description**: Performs batch atomic edits on the active Grasshopper canvas (adding/removing components, wiring/unwiring ports, updating parameters and positions) backed by `GH_UndoRecord` for full undo support.
* **Input Schema**:
  ```json
  {
    "type": "object",
    "properties": {
      "componentsToAdd": {
        "type": "array",
        "items": {
          "type": "object",
          "required": ["type", "x", "y"],
          "properties": {
            "type": { "type": "string", "description": "Component Guid or Name (e.g. 'Construct Point', 'Number Slider')." },
            "x": { "type": "number" },
            "y": { "type": "number" },
            "nickName": { "type": "string" },
            "tempId": { "type": "string", "description": "Local ID used for wiring in the same batch." }
          }
        }
      },
      "componentsToRemove": { "type": "array", "items": { "type": "string" } },
      "wiresToConnect": {
        "type": "array",
        "items": {
          "type": "object",
          "required": ["source", "target"],
          "properties": {
            "source": { "type": "string", "description": "Component Guid or tempId." },
            "sourceOutput": { "type": "integer", "default": 0 },
            "target": { "type": "string", "description": "Component Guid or tempId." },
            "targetInput": { "type": "integer", "default": 0 }
          }
        }
      },
      "wiresToDisconnect": {
        "type": "array",
        "items": {
          "type": "object",
          "required": ["source", "target"],
          "properties": {
            "source": { "type": "string" },
            "target": { "type": "string" }
          }
        }
      },
      "dryRun": { "type": "boolean", "description": "If true, simulates changes and immediately rolls back via GH_UndoRecord. Default false." }
    }
  }
  ```
* **Undo & Safety Mechanism**:
  ```csharp
  var undoRecord = new GH_UndoRecord("Batch Edit MCP Canvas");
  doc.UndoServer.PushUndoRecord(undoRecord);
  // Perform additions, connections, deletions
  if (dryRun)
  {
      doc.Undo();
      return BatchEditResult.RolledBack();
  }
  ```

---

### 3.4 `execute_grasshopper_code`

* **Description**: Flexible Roslyn C# script executor with multi-engine bindings (`doc` for GH_Document, `canvas` for GH_Canvas, `rhinoDoc` for RhinoDoc, `revitDoc` for Revit Document), transaction tiers (None, Read, Write), and dryRun rollback.
* **Input Schema**:
  ```json
  {
    "type": "object",
    "required": ["code"],
    "properties": {
      "code": { "type": "string", "description": "C# script body to execute." },
      "args": { "type": "object", "description": "Optional parameters accessible via args dictionary." },
      "transaction": { "type": "string", "enum": ["none", "auto", "manual"], "default": "auto" },
      "dryRun": { "type": "boolean", "default": false },
      "timeoutSeconds": { "type": "integer", "default": 60 }
    }
  }
  ```
* **Global Script Variables**:
  - `doc`: `Grasshopper.Kernel.GH_Document` (Active canvas document or execution context)
  - `canvas`: `Grasshopper.GUI.Canvas.GH_Canvas` (Canvas viewport and painter)
  - `rhinoDoc`: `Rhino.RhinoDoc` (Rhino model document)
  - `revitDoc`: `Autodesk.Revit.DB.Document` (Revit active database document)
  - `uiApp`: `Autodesk.Revit.UI.UIApplication`
  - `args`: `HPRebar.McpBridge.Core.Scripting.ScriptArgs`
  - `ct`: `System.Threading.CancellationToken`
  - `log`: `System.Action<string>`
* **Dual-Layer Transaction & Rollback**:
  - If `transaction == "auto"` and `revitDoc` elements are modified: enclosed in Revit `TransactionGroup` / `Transaction`.
  - If `dryRun == true`:
    1. Revit `TransactionGroup.RollBack()` is called.
    2. Grasshopper canvas changes are rolled back via `doc.Undo()`.
    3. Guarantees zero residual state in both Revit and Rhino models.

---

### 3.5 `export_grasshopper_canvas_image`

* **Description**: Renders a high-resolution snapshot of the Grasshopper canvas into PNG or Base64 data URL.
* **Input Schema**:
  ```json
  {
    "type": "object",
    "properties": {
      "mode": { "type": "string", "enum": ["FullCanvas", "Viewport", "Selection"], "default": "FullCanvas" },
      "zoom": { "type": "number", "default": 1.0 },
      "outputPath": { "type": "string", "description": "Optional file path to save PNG on disk." },
      "returnBase64": { "type": "boolean", "default": true }
    }
  }
  ```
* **Implementation**:
  Uses `canvas.GenerateHiResImage` or renders canvas objects into an off-screen `System.Drawing.Bitmap` with `canvas.Painter` and GDI+ rendering.

---

## 4. Dynamic Registry (8 Tools) & 12 Concrete Embedded Seeds

### 4.1 The 8 Dynamic Registry Meta Tools
Provided by `HPRebar.Mcp.Server.Core.Registry`:
1. `search_tools`: Searches available seeds and published user tools by name, tag, or description.
2. `propose_tool`: Analyzes syntax, validates ADR-0007 quality rules, compiles script, and stages for testing.
3. `test_tool`: Executes a proposed tool in sandbox mode with `dryRun=true` or supplied test inputs.
4. `publish_tool`: Promotes tested tool into user's live active tool catalog.
5. `list_tools`: Returns all active, proposed, and retired tools with schema metadata.
6. `get_tool_source`: Inspects C# Roslyn script and schema for an existing tool.
7. `retire_tool`: Removes a tool from active registry.
8. `view_tool_docs`: Renders tool usage, parameter docs, and examples.

---

### 4.2 The 12 Concrete Embedded Seeds (Grasshopper ↔ Revit)

The 12 embedded seeds are stored in `SeedLibrary/<Category>/<name>/` within `HPGrasshopper.Mcp.Server`:

```
SeedLibrary/
├── Grids/
│   ├── sync_rhino_curves_to_revit_grids/
│   └── create_parametric_levels/
├── Framing/
│   ├── sync_rhino_lines_to_structural_columns/
│   └── sync_rhino_curves_to_structural_beams/
├── Floors/
│   └── sync_rhino_surfaces_to_revit_floors/
├── Rebar/
│   ├── generate_rebar_from_curves/
│   └── distribute_stirrups_along_beam_curve/
├── Facade/
│   ├── generate_facade_adaptive_panels/
│   └── sync_curves_to_curtain_grid/
├── Openings/
│   └── create_parametric_wall_openings/
└── DirectShape/
    ├── query_revit_geometry_to_grasshopper/
    └── bake_grasshopper_preview_to_revit_directshape/
```

#### Detailed Specification of the 12 Seeds:

| # | Seed Tool Name | Category | Primary Purpose & Input | Revit / Rhino API Used |
|---|---|---|---|---|
| 1 | `sync_rhino_curves_to_revit_grids` | Grids | Takes 2D Rhino curves (lines or arcs) and creates/updates native Revit `Grid` elements with naming and bubble offsets. | `Rhino.Geometry.Curve` -> `Revit.DB.Grid.Create(revitDoc, line)` |
| 2 | `create_parametric_levels` | Grids | Generates an array of Revit `Level` elements from Z-elevation values or horizontal planes generated in Grasshopper. | `Revit.DB.Level.Create(revitDoc, elevation)` |
| 3 | `sync_rhino_lines_to_structural_columns` | Framing | Converts vertical Rhino line axes into Revit `FamilyInstance` Structural Columns with Level bindings and FamilySymbol resolution. | `revitDoc.Create.NewFamilyInstance(point, symbol, level, StructuralType.Column)` |
| 4 | `sync_rhino_curves_to_structural_beams` | Framing | Converts 3D space curves into Revit Structural Framing beams (`StructuralType.Beam`) with cross-section rotation. | `revitDoc.Create.NewFamilyInstance(curve, symbol, level, StructuralType.Beam)` |
| 5 | `sync_rhino_surfaces_to_revit_floors` | Floors | Converts planar Rhino Brep faces into Revit `Floor` elements by extracting boundary curve loops and assigning FloorType. | `CurveLoop` -> `Floor.Create(revitDoc, curveLoops, floorTypeId, levelId)` |
| 6 | `generate_rebar_from_curves` | Rebar | Generates native 3D `Rebar` elements from Grasshopper center-line curves inside a host concrete element (`Rebar.CreateFromCurves`). | `Rebar.CreateFromCurves(revitDoc, RebarStyle.Standard, barType, hook, hook, host, normal, curves, ...)` |
| 7 | `distribute_stirrups_along_beam_curve` | Rebar | Parametrically calculates dense and sparse stirrup spacings along a structural beam axis curve and generates Rebar sets. | `Rebar.CreateFromCurvesAndShape(...)` / `RebarContainer` |
| 8 | `generate_facade_adaptive_panels` | Facade | Subdivides a freeform Rhino surface into UV quadrilateral/triangular mesh and places Revit Adaptive Component panels on points. | `AdaptiveComponentInstanceUtils.CreateAdaptiveComponentInstance(...)` |
| 9 | `sync_curves_to_curtain_grid` | Facade | Transfers parametric curve divisions from Grasshopper into U/V Grid lines on a Revit Curtain Wall host. | `CurtainGrid.AddGridLine(...)` |
| 10 | `create_parametric_wall_openings` | Openings | Projects Grasshopper boundary curves onto host walls or floor slabs to generate native Revit `Opening` elements. | `revitDoc.Create.NewOpening(wall, p1, p2)` or `NewOpening(floor, curveArray, ...)` |
| 11 | `query_revit_geometry_to_grasshopper` | DirectShape | Queries Revit element solids (GeometryElement/Solid) and streams them as Rhino Breps/Meshes into Grasshopper. | `RhinoInside.Revit.Convert.Geometry.GeometryEncoder.ToBrep(solid)` |
| 12 | `bake_grasshopper_preview_to_revit_directshape` | DirectShape | Bakes Grasshopper geometric outputs (Breps, Meshes, SubDs) directly into Revit as native `DirectShape` elements with Category/Material. | `DirectShape.CreateElement(revitDoc, categoryId)` -> `SetShape(...)` |

---

## 5. ADR-0007 Script Quality Rules Compliance

All 12 embedded seeds and run-time proposed scripts must strictly adhere to the repository's clean-code quality rules enforced by `ScriptQuality.Find(code)`:

### Blocking Errors (Reject Proposal / Fail Seed Tests)
- **Q-B1**: **No commented-out code**. Any comment containing C# keywords (`if (`, `var `, `// return`, etc.) is flagged as an error.
- **Q-B2**: **No empty catch blocks**. Every `catch { ... }` must contain an explanatory comment stating why the exception is ignored (e.g. `catch { /* Output preview unavailable */ }`).
- **Q-B3**: **Maximum script length: 300 lines**. Scripts exceeding 300 lines must be split or moved into engine logic.

### Warnings (Surface to AI Agent)
- **Q-W1**: Block length > 50 lines -> extract into local functions.
- **Q-W2**: Nesting depth > 3 levels -> early return or refactor into functions.
- **Q-W3**: Vague variable names: forbidden names include `data`, `tmp`, `temp`, `obj`, `res`, `val`, `foo`, `bar`, `stuff`, `thing`.
- **Q-W4**: Boolean parameter on local functions -> split into two descriptive functions.
- **Q-W5**: Swallowing catch-all: `catch (Exception)` without specific handling.

---

## 6. Sibling Architecture Parity & Tool Count Calculation

Across the repo, each MCP server targets a strict tool surface invariant:
- **AutoCAD / Navisworks / Excel**:
  - 4 Core tools (`execute_*_code`, `get_*_context`, `inspect_type`, `cancel_execution`)
  - 8 Registry Meta tools
  - 12 Embedded Seeds
  - **Total: 24 tools**
- **Dynamo**:
  - 4 Core tools (`execute_dynamo_code`, `execute_designscript`, `get_dynamo_context`, `cancel_execution`) with `inspect_type` suppressed
  - 8 Registry Meta tools
  - 12 Embedded Seeds (graph tools like `batch_edit_graph`, `run_dyn_file`, `export_graph_to_image` were implemented as embedded seeds)
  - **Total: 24 tools**
- **HPGrasshopper MCP Surface Formulation**:
  Under `ORIGINAL_REQUEST.md`, R2 specifies 5 Core Tools (`read_grasshopper_canvas`, `run_grasshopper_player`, `batch_edit_grasshopper_canvas`, `execute_grasshopper_code`, `export_grasshopper_canvas_image`) while R4 mentions 24 Tools with 12 Embedded Seeds.
  
  **Harmonized Formulation**:
  1. The 5 Core Tools are registered as the standard primary tools for Grasshopper canvas/player interaction.
  2. The 8 Dynamic Registry tools provide runtime extensibility.
  3. The 12 Embedded Seeds provide concrete parametric Revit ↔ Grasshopper BIM synchronization.
  4. With `inspect_type` suppressed (as done in HPDynamo) or by registering the specialized canvas tools via the profile tool collection, the catalog cleanly exposes 25 tools (or 24 if canvas tools are packaged as high-priority seeds / core tool names). The test suite will enforce the exact tool surface matching the orchestrator's contract.

---

## 7. Verification Method & Test Plan

1. **Unit Tests (`HPGrasshopper.McpBridge.Tests`)**:
   - Serialization of `GH_Document` to canvas JSON.
   - Parsing of Grasshopper player inputs (JSON to GH types).
   - `GH_UndoRecord` push and rollback verification.
2. **Unit Tests (`HPGrasshopper.Mcp.Server.Tests`)**:
   - `SeedQualityTests`: Asserts 12 embedded seeds have 0 errors and 0 warnings under `ScriptQuality.Find()`.
   - `SeedManifestTests`: Validates `_seeds.json` manifest hashing.
   - `SeedCompilationTests`: Compiles all 12 seeds against RhinoCommon and Grasshopper reference assemblies.
   - `ToolSurfaceTests`: Verifies `tools/list` response format and schema conformity.
3. **Automated Live Harness (`tools/harness/run-live-verify.ps1`)**:
   - Connects to Named Pipe `hpgrasshopper-mcp-2026`.
   - Tests `read_grasshopper_canvas`, `batch_edit_grasshopper_canvas`, `execute_grasshopper_code`, and `export_grasshopper_canvas_image`.
