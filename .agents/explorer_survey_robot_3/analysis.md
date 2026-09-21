# HPRobot MCP Subsystem — Comprehensive Investigation & Engineering Specification

**Author**: Explorer 3 (Tool Catalog and Safety Engineer)  
**Date**: 2026-09-21  
**Target Environment**: Autodesk Robot Structural Analysis Professional 2026, .NET 8.0-windows (Bridge), .NET 10.0 (Server & Tests)  
**Scope**: 24-Tool Catalog Design, 3-Tier Safety System (`RobotTierAnalyzer`), RTD Snapshot Manager, Test Suite Architecture, Live Harness, Solution Topology  

---

## 1. Executive Summary & Problem Boundary

The HPRobot MCP subsystem exposes Autodesk Robot Structural Analysis Professional 2026 to AI coding agents via the Model Context Protocol (MCP 2.2.0, stdio) and named pipe communication (`hprobot-mcp-2026`). 

Following the established architecture of `HPSap2000`, `HPEtabs`, `HPExcel`, and `HPNavis`, Robot Structural Analysis Professional is controlled out-of-process through its COM API exposed by `Interop.RobotOM.dll` located at:
`C:\Program Files\Autodesk\Robot Structural Analysis Professional 2026\Exe\Interop.RobotOM.dll`.

### Key System Characteristics
1. **Architectural Isolation**: `HPRobot/` references `../McpShared/` only. It never cross-references `HPRebar`, `HPAutoCad`, `HPNavis`, `HPEtabs`, `HPCivil3d`, `HPSap2000`, `HPPowerBi`, or `HPExcel`.
2. **Process Separation**: `HPRobot.McpBridge` is a standalone WPF application (.NET 8.0-windows, MaterialDesignThemes 5.3.2) that attaches to the active Robot instance via COM ProgID `"Robot.Application"`. `HPRobot.Mcp.Server` is a lightweight .NET 10 console application launched by AI clients over stdio.
3. **Total Surface: 24 Tools**:
   - **4 Core Tools**: `execute_robot_code`, `get_robot_context`, `robot://` resources, Prompts.
   - **8 Registry Meta Tools**: `search_tools`, `propose_tool`, `test_tool`, `publish_tool`, `disable_tool`, `enable_tool`, `deprecate_tool`, `delete_tool` (inherited from `McpShared`).
   - **12 Embedded Seed Tools**: Covering model info, structural geometry, properties, grids, load definitions, element creation, support assignment, section assignment, bar loading, calculation execution, and result extraction (reactions & forces).
4. **3-Tier Safety & RTD Snapshot Engine**:
   - **Tier R (Read-Only)**: Model queries and result extraction. Allowed without snapshots.
   - **Tier W (Write / Mutation)**: Geometry/load/property additions or modifications. Requires `AllowExecution` toggle; triggers an automatic `.rtd` pre-run snapshot into `%LocalAppData%\HPRobot\McpBridge\snapshots\<model>\prerun\` (or `%TEMP%`).
   - **Tier D / Heavy (Destructive & Heavy Operations)**: Structural element deletion, project file management, and FEA calculations (`Calculate()`). Requires explicit user toggle `AllowHeavyOperations` on the Bridge UI.
5. **Standardized Working Units**: `RobotUnitsPolicy` forces metric units (`m`, `kN`, `kN·m`, `MPa`) for the duration of every script run and guarantees restoration in `finally`.

---

## 2. Complete 24-Tool Catalog Design

### 2.1 Tool Catalog Composition

| Category | Count | Tool Names | Notes |
|---|---|---|---|
| **Core Tools** | 4 | `execute_robot_code`<br>`get_robot_context`<br>`robot://` resources<br>Prompts (`robot-script`, `robot-model-query`, `robot-analysis-check`) | Primary entry points for dynamic execution and context discovery |
| **Registry Meta Tools** | 8 | `search_tools`<br>`propose_tool`<br>`test_tool`<br>`publish_tool`<br>`disable_tool`<br>`enable_tool`<br>`deprecate_tool`<br>`delete_tool` | Dynamic lifecycle management provided by `HPRebar.Mcp.Server.Core` |
| **Model Seeds** | 1 | `Model/get_model_info` | Read-only model overview, RTD path, lock/calc status, counts |
| **Geometry Seeds** | 4 | `Geometry/get_structural_objects`<br>`Geometry/get_coordinate_systems_and_grids`<br>`Geometry/draw_bar_by_coords`<br>`Geometry/assign_node_support` | Structural geometry querying, grid extraction, bar creation, support assignments |
| **Property Seeds** | 2 | `Property/get_materials_and_sections`<br>`Property/assign_bar_section` | Material and cross-section catalogs and assignments |
| **Load Seeds** | 2 | `Load/get_load_definitions`<br>`Load/assign_bar_load` | Load cases, combinations, and distributed/concentrated load application |
| **Analysis Seeds** | 1 | `Analysis/run_calculations` | FEA calculation execution (`Calculate()`), gated under Tier D / Heavy |
| **Results Seeds** | 2 | `Results/get_node_reactions`<br>`Results/get_bar_forces` | Support reactions and internal force diagram extraction (Fx, Fy, Fz, Mx, My, Mz) |
| **Total** | **24** | | |

---

### 2.2 Core Tools Specification

#### 2.2.1 `execute_robot_code`
- **Description**: Compiles and executes C# scripts against the active Robot Structural Analysis Professional 2026 instance via Roslyn and RobotOM COM API.
- **Input Schema**:
  ```json
  {
    "type": "object",
    "properties": {
      "code": {
        "type": "string",
        "description": "C# script body to execute against RobotOM. Available globals: robot (IRobotApplication), structure (IRobotStructure), project (IRobotProject), units (ScriptUnits), args (ScriptArgs), ct (CancellationToken), log (Action<string>), progress (Action<int, int, string>)."
      },
      "transaction": {
        "type": "string",
        "enum": ["auto", "manual", "none"],
        "default": "auto",
        "description": "Transaction mode. Use 'none' for read-only scripts. Use 'auto' for writing scripts."
      },
      "dryRun": {
        "type": "boolean",
        "default": false,
        "description": "If true, analyzes tiers and compiles without executing (returns PREVIEW diagnostics for writing operations)."
      },
      "timeoutSeconds": {
        "type": "integer",
        "default": 30,
        "maximum": 300,
        "description": "Execution timeout clamp in seconds. Max 300s for heavy operations."
      },
      "label": {
        "type": "string",
        "description": "Short human-readable label used in logs and snapshot filenames."
      },
      "args": {
        "type": "object",
        "description": "Optional dictionary of parameters accessible via args.Str(), args.Double(), args.Int(), args.Bool()."
      }
    },
    "required": ["code"]
  }
  ```

#### 2.2.2 `get_robot_context`
- **Description**: Returns active Robot model metadata, project type, file paths, node/bar/panel counts, load cases, calculation results readiness, and bridge status. Hides non-Robot fields.
- **Input Schema**:
  ```json
  {
    "type": "object",
    "properties": {
      "includeSelection": {
        "type": "boolean",
        "default": false,
        "description": "If true, enumerates currently selected nodes, bars, and panels in Robot."
      }
    },
    "additionalProperties": false
  }
  ```

#### 2.2.3 `robot://` Resources
Resources expose direct read-only views into the structural model without custom scripting:
1. `robot://model/summary`: Overall model summary (title, file path, project type, object counts, calculation status).
2. `robot://model/nodes`: List of nodes with coordinates and support conditions.
3. `robot://model/bars`: List of bars with connectivity, section names, and materials.
4. `robot://model/cases`: List of defined load cases and combinations.
5. `robot://results/reactions`: Summary of support reactions for active cases.
6. `robot://results/forces`: Extreme bar forces across the structure.

#### 2.2.4 Prompts
Standard prompt templates registered with MCP server:
1. `robot-script`: Generates safe Roslyn C# scripts targeting `RobotOM`, observing 3-tier rules, metric units, and proper error checking.
2. `robot-model-query`: Guides script generation for querying nodes, bars, panels, and boundary conditions.
3. `robot-analysis-check`: Assesses model readiness for analysis (unsupported nodes, free bars, missing sections, loads).

---

### 2.3 Embedded Seed Tools (Full Schemas & C# Roslyn Scripts)

#### Seed 1: `Model/get_model_info`
- **tool.json**:
  ```json
  {
    "name": "get_model_info",
    "version": 1,
    "status": "published",
    "author": "hprebar",
    "category": "Model",
    "host": "robot",
    "hostVersions": ["2023", "2024", "2025", "2026"],
    "transaction": "none",
    "timeoutSeconds": 30,
    "destructive": false,
    "tags": ["model", "query", "info", "units"],
    "title": "Get model info",
    "description": "Returns an overview of the active Robot Structural Analysis model: file path, project type, node/bar/panel counts, load case counts, calculation status, and active units. Read-only.",
    "inputSchema": {
      "type": "object",
      "properties": {
        "includeCases": {
          "type": "boolean",
          "default": true,
          "description": "Include summary list of load cases and combinations."
        }
      },
      "additionalProperties": false
    }
  }
  ```
- **code.cs**:
  ```csharp
  bool includeCases = args.Bool("includeCases", true);
  string filePath = project.FileName ?? "";
  bool hasFile = !string.IsNullOrWhiteSpace(filePath) && filePath.Contains("\\");
  string fileName = hasFile ? System.IO.Path.GetFileName(filePath) : "(unsaved model)";

  int nodeCount = structure.Nodes.GetAll().Count;
  int barCount = structure.Bars.GetAll().Count;
  int panelCount = structure.Objects.GetAll().Count;
  int caseCount = structure.Cases.GetAll().Count;
  bool resultsAvailable = structure.Results.Available != 0;
  string projectType = project.Type.ToString();

  object casesList = null;
  if (includeCases)
  {
      var cCol = structure.Cases.GetAll();
      var list = new List<object>();
      for (int i = 1; i <= cCol.Count; i++)
      {
          var c = cCol.Get(i);
          list.Add(new { number = c.Number, name = c.Name, type = c.Type.ToString(), nature = c.Nature.ToString() });
      }
      casesList = list;
  }

  log($"Model: {fileName} | Type: {projectType} | Nodes: {nodeCount}, Bars: {barCount}, Panels: {panelCount}, Cases: {caseCount}, ResultsAvailable: {resultsAvailable}");

  return new
  {
      success = true,
      hasModelFile = hasFile,
      modelName = fileName,
      modelPath = hasFile ? filePath : null,
      projectType = projectType,
      counts = new
      {
          nodes = nodeCount,
          bars = barCount,
          panels = panelCount,
          cases = caseCount
      },
      resultsAvailable = resultsAvailable,
      units = units.Label,
      cases = casesList,
      summary = $"{fileName} ({projectType}): {nodeCount} nodes, {barCount} bars, {panelCount} panels. Calculations: {(resultsAvailable ? "Available" : "Not Available")}."
  };
  ```
- **examples.json**:
  ```json
  [
    {
      "title": "Get basic model info",
      "input": { "includeCases": true }
    }
  ]
  ```

---

#### Seed 2: `Geometry/get_structural_objects`
- **tool.json**:
  ```json
  {
    "name": "get_structural_objects",
    "version": 1,
    "status": "published",
    "author": "hprebar",
    "category": "Geometry",
    "host": "robot",
    "hostVersions": ["2023", "2024", "2025", "2026"],
    "transaction": "none",
    "timeoutSeconds": 30,
    "destructive": false,
    "tags": ["geometry", "nodes", "bars", "panels"],
    "title": "Get structural objects",
    "description": "Extracts structural nodes, bars, and panels with coordinates, connectivity, assigned sections, and materials. Supports filtering by object type and limit.",
    "inputSchema": {
      "type": "object",
      "properties": {
        "objectType": {
          "type": "string",
          "enum": ["all", "nodes", "bars", "panels"],
          "default": "all",
          "description": "Category of structural elements to query."
        },
        "limit": {
          "type": "integer",
          "default": 100,
          "maximum": 1000,
          "description": "Maximum number of items to return per category."
        }
      },
      "additionalProperties": false
    }
  }
  ```
- **code.cs**:
  ```csharp
  string objType = args.Str("objectType", "all").ToLowerInvariant();
  int limit = Math.Clamp(args.Int("limit", 100), 1, 1000);

  object nodeList = null;
  if (objType == "all" || objType == "nodes")
  {
      var nCol = structure.Nodes.GetAll();
      var nodes = new List<object>();
      int nMax = Math.Min(nCol.Count, limit);
      for (int i = 1; i <= nMax; i++)
      {
          var node = (IRobotNode)nCol.Get(i);
          string support = node.HasLabel(IRobotLabelType.I_LT_SUPPORT) != 0 
              ? node.GetLabelName(IRobotLabelType.I_LT_SUPPORT) 
              : null;
          nodes.Add(new { id = node.Number, x = node.X, y = node.Y, z = node.Z, support });
      }
      nodeList = new { total = nCol.Count, returned = nodes.Count, items = nodes };
  }

  object barList = null;
  if (objType == "all" || objType == "bars")
  {
      var bCol = structure.Bars.GetAll();
      var bars = new List<object>();
      int bMax = Math.Min(bCol.Count, limit);
      for (int i = 1; i <= bMax; i++)
      {
          var bar = (IRobotBar)bCol.Get(i);
          string section = bar.HasLabel(IRobotLabelType.I_LT_BAR_SECTION) != 0 
              ? bar.GetLabelName(IRobotLabelType.I_LT_BAR_SECTION) 
              : null;
          string material = bar.HasLabel(IRobotLabelType.I_LT_MATERIAL) != 0 
              ? bar.GetLabelName(IRobotLabelType.I_LT_MATERIAL) 
              : null;
          bars.Add(new { id = bar.Number, startNode = bar.StartNode, endNode = bar.EndNode, length = bar.Length, section, material });
      }
      barList = new { total = bCol.Count, returned = bars.Count, items = bars };
  }

  object panelList = null;
  if (objType == "all" || objType == "panels")
  {
      var pCol = structure.Objects.GetAll();
      var panels = new List<object>();
      int pMax = Math.Min(pCol.Count, limit);
      for (int i = 1; i <= pMax; i++)
      {
          var obj = (IRobotObjObject)pCol.Get(i);
          string thickness = obj.HasLabel(IRobotLabelType.I_LT_PANEL_THICKNESS) != 0 
              ? obj.GetLabelName(IRobotLabelType.I_LT_PANEL_THICKNESS) 
              : null;
          panels.Add(new { id = obj.Number, thickness });
      }
      panelList = new { total = pCol.Count, returned = panels.Count, items = panels };
  }

  return new
  {
      success = true,
      nodes = nodeList,
      bars = barList,
      panels = panelList
  };
  ```
- **examples.json**:
  ```json
  [
    {
      "title": "Get first 50 nodes and bars",
      "input": { "objectType": "all", "limit": 50 }
    }
  ]
  ```

---

#### Seed 3: `Property/get_materials_and_sections`
- **tool.json**:
  ```json
  {
    "name": "get_materials_and_sections",
    "version": 1,
    "status": "published",
    "author": "hprebar",
    "category": "Property",
    "host": "robot",
    "hostVersions": ["2023", "2024", "2025", "2026"],
    "transaction": "none",
    "timeoutSeconds": 30,
    "destructive": false,
    "tags": ["property", "materials", "sections"],
    "title": "Get materials and sections",
    "description": "Lists all defined materials and bar cross-sections in the active Robot model with their engineering properties (E, G, density, area, moments of inertia).",
    "inputSchema": {
      "type": "object",
      "properties": {
        "includeMaterials": {
          "type": "boolean",
          "default": true,
          "description": "Include material definitions."
        },
        "includeSections": {
          "type": "boolean",
          "default": true,
          "description": "Include bar cross-section definitions."
        }
      },
      "additionalProperties": false
    }
  }
  ```
- **code.cs**:
  ```csharp
  bool incMat = args.Bool("includeMaterials", true);
  bool incSec = args.Bool("includeSections", true);

  var materials = new List<object>();
  if (incMat)
  {
      var matNames = structure.Labels.GetAvailableNames(IRobotLabelType.I_LT_MATERIAL);
      for (int i = 1; i <= matNames.Count; i++)
      {
          string name = matNames.Get(i);
          var lbl = structure.Labels.Get(IRobotLabelType.I_LT_MATERIAL, name);
          var data = (IRobotMaterialData)lbl.Data;
          materials.Add(new
          {
              name,
              type = data.Type.ToString(),
              e = data.E,
              nu = data.NU,
              unitWeight = data.UnitWeight
          });
      }
  }

  var sections = new List<object>();
  if (incSec)
  {
      var secNames = structure.Labels.GetAvailableNames(IRobotLabelType.I_LT_BAR_SECTION);
      for (int i = 1; i <= secNames.Count; i++)
      {
          string name = secNames.Get(i);
          var lbl = structure.Labels.Get(IRobotLabelType.I_LT_BAR_SECTION, name);
          var data = (IRobotBarSectionData)lbl.Data;
          sections.Add(new
          {
              name,
              material = data.MaterialName,
              type = data.Type.ToString(),
              ax = data.GetValue((short)IRobotBarSectionDataValueType.I_BSDV_AX),
              iy = data.GetValue((short)IRobotBarSectionDataValueType.I_BSDV_IY),
              iz = data.GetValue((short)IRobotBarSectionDataValueType.I_BSDV_IZ),
              ix = data.GetValue((short)IRobotBarSectionDataValueType.I_BSDV_IX)
          });
      }
  }

  return new
  {
      success = true,
      materialCount = materials.Count,
      sectionCount = sections.Count,
      materials,
      sections
  };
  ```
- **examples.json**:
  ```json
  [
    {
      "title": "Get all materials and sections",
      "input": { "includeMaterials": true, "includeSections": true }
    }
  ]
  ```

---

#### Seed 4: `Geometry/get_coordinate_systems_and_grids`
- **tool.json**:
  ```json
  {
    "name": "get_coordinate_systems_and_grids",
    "version": 1,
    "status": "published",
    "author": "hprebar",
    "category": "Geometry",
    "host": "robot",
    "hostVersions": ["2023", "2024", "2025", "2026"],
    "transaction": "none",
    "timeoutSeconds": 30,
    "destructive": false,
    "tags": ["geometry", "grid", "axes", "coordinates"],
    "title": "Get coordinate systems and structural grids",
    "description": "Reads defined structural axis grids (Cartesian, Cylindrical) including sequence coordinates, labels, and storey inclusions.",
    "inputSchema": {
      "type": "object",
      "properties": {},
      "additionalProperties": false
    }
  }
  ```
- **code.cs**:
  ```csharp
  var axisMngr = project.AxisMngr;
  int count = axisMngr.Count;
  var grids = new List<object>();

  for (int i = 1; i <= count; i++)
  {
      var grid = axisMngr.Get(i);
      string gridName = grid.Name;
      string gridType = grid.Type.ToString();

      if (grid is IRobotStructuralAxisGridCartesian cart)
      {
          var xAxes = new List<object>();
          for (int j = 1; j <= cart.X.AxisCount; j++)
          {
              xAxes.Add(new { position = cart.X.StartPosition });
          }
          grids.Add(new
          {
              name = gridName,
              type = gridType,
              xCount = cart.X.AxisCount,
              yCount = cart.Y.AxisCount,
              zCount = cart.Z.AxisCount,
              rotationAngle = cart.RotationAngle
          });
      }
      else
      {
          grids.Add(new { name = gridName, type = gridType });
      }
  }

  return new
  {
      success = true,
      gridCount = count,
      grids
  };
  ```
- **examples.json**:
  ```json
  [
    {
      "title": "Get structural grids",
      "input": {}
    }
  ]
  ```

---

#### Seed 5: `Load/get_load_definitions`
- **tool.json**:
  ```json
  {
    "name": "get_load_definitions",
    "version": 1,
    "status": "published",
    "author": "hprebar",
    "category": "Load",
    "host": "robot",
    "hostVersions": ["2023", "2024", "2025", "2026"],
    "transaction": "none",
    "timeoutSeconds": 30,
    "destructive": false,
    "tags": ["load", "cases", "combinations"],
    "title": "Get load definitions",
    "description": "Reads all load cases (simple cases, dead, live, wind, seismic) and combinations with applied records count and factor definitions.",
    "inputSchema": {
      "type": "object",
      "properties": {
        "includeRecords": {
          "type": "boolean",
          "default": false,
          "description": "If true, extracts details for each load record inside the cases."
        }
      },
      "additionalProperties": false
    }
  }
  ```
- **code.cs**:
  ```csharp
  bool incRec = args.Bool("includeRecords", false);
  var casesCol = structure.Cases.GetAll();
  var simpleCases = new List<object>();
  var combinations = new List<object>();

  for (int i = 1; i <= casesCol.Count; i++)
  {
      var c = casesCol.Get(i);
      if (c is IRobotSimpleCase sc)
      {
          var records = new List<object>();
          if (incRec)
          {
              for (int r = 1; r <= sc.Records.Count; r++)
              {
                  var rec = sc.Records.Get(r);
                  records.Add(new { index = r, type = rec.Type.ToString(), objects = rec.Objects.ToText() });
              }
          }
          simpleCases.Add(new
          {
              number = sc.Number,
              name = sc.Name,
              nature = sc.Nature.ToString(),
              recordCount = sc.Records.Count,
              records = incRec ? records : null
          });
      }
      else if (c is IRobotCaseCombination comb)
      {
          combinations.Add(new
          {
              number = comb.Number,
              name = comb.Name,
              type = comb.CombinationType.ToString(),
              caseComponents = comb.CaseComponents.Count
          });
      }
  }

  return new
  {
      success = true,
      simpleCaseCount = simpleCases.Count,
      combinationCount = combinations.Count,
      simpleCases,
      combinations
  };
  ```
- **examples.json**:
  ```json
  [
    {
      "title": "Get all load definitions",
      "input": { "includeRecords": false }
    }
  ]
  ```

---

#### Seed 6: `Geometry/draw_bar_by_coords`
- **tool.json**:
  ```json
  {
    "name": "draw_bar_by_coords",
    "version": 1,
    "status": "published",
    "author": "hprebar",
    "category": "Geometry",
    "host": "robot",
    "hostVersions": ["2023", "2024", "2025", "2026"],
    "transaction": "auto",
    "timeoutSeconds": 30,
    "destructive": false,
    "tags": ["geometry", "bar", "create", "model"],
    "title": "Draw bar by coordinates",
    "description": "Creates a structural bar between start and end 3D coordinates (meters). Automatically creates or connects existing nodes at the endpoints and optionally assigns a cross-section.",
    "inputSchema": {
      "type": "object",
      "properties": {
        "startX": { "type": "number", "description": "Start X coordinate (m)" },
        "startY": { "type": "number", "description": "Start Y coordinate (m)" },
        "startZ": { "type": "number", "description": "Start Z coordinate (m)" },
        "endX": { "type": "number", "description": "End X coordinate (m)" },
        "endY": { "type": "number", "description": "End Y coordinate (m)" },
        "endZ": { "type": "number", "description": "End Z coordinate (m)" },
        "sectionName": { "type": "string", "description": "Optional section name to assign (e.g. 'IPE 300')" },
        "barNumber": { "type": "integer", "description": "Optional bar number. If 0 or omitted, next free number is used." }
      },
      "required": ["startX", "startY", "startZ", "endX", "endY", "endZ"],
      "additionalProperties": false
    }
  }
  ```
- **code.cs**:
  ```csharp
  double x1 = args.Double("startX");
  double y1 = args.Double("startY");
  double z1 = args.Double("startZ");
  double x2 = args.Double("endX");
  double y2 = args.Double("endY");
  double z2 = args.Double("endZ");
  string section = args.Str("sectionName");
  int barNum = args.Int("barNumber", 0);

  double dx = x2 - x1, dy = y2 - y1, dz = z2 - z1;
  double len = Math.Sqrt(dx * dx + dy * dy + dz * dz);
  if (len < 0.001) throw new ArgumentException($"Start and end coordinates are coincident (distance {len:F4} m < 1 mm).");

  int n1 = structure.Nodes.FindXYZ(x1, y1, z1);
  if (n1 <= 0)
  {
      n1 = structure.Nodes.FreeNumber;
      structure.Nodes.Create(n1, x1, y1, z1);
  }

  int n2 = structure.Nodes.FindXYZ(x2, y2, z2);
  if (n2 <= 0)
  {
      n2 = structure.Nodes.FreeNumber;
      structure.Nodes.Create(n2, x2, y2, z2);
  }

  int bId = barNum > 0 ? barNum : structure.Bars.FreeNumber;
  structure.Bars.Create(bId, n1, n2);

  if (!string.IsNullOrWhiteSpace(section))
  {
      var sel = structure.Selections.Create(IRobotObjectType.I_OT_BAR);
      sel.AddOne(bId);
      structure.Bars.SetLabel(sel, IRobotLabelType.I_LT_BAR_SECTION, section);
  }

  log($"Created bar {bId} between nodes {n1} ({x1},{y1},{z1}) and {n2} ({x2},{y2},{z2}), length: {len:F3} m");

  return new
  {
      success = true,
      barNumber = bId,
      startNode = n1,
      endNode = n2,
      length = len,
      section = section
  };
  ```
- **examples.json**:
  ```json
  [
    {
      "title": "Draw column from (0,0,0) to (0,0,3.5)",
      "input": {
        "startX": 0.0, "startY": 0.0, "startZ": 0.0,
        "endX": 0.0, "endY": 0.0, "endZ": 3.5,
        "sectionName": "HEA 200"
      }
    }
  ]
  ```

---

#### Seed 7: `Geometry/assign_node_support`
- **tool.json**:
  ```json
  {
    "name": "assign_node_support",
    "version": 1,
    "status": "published",
    "author": "hprebar",
    "category": "Geometry",
    "host": "robot",
    "hostVersions": ["2023", "2024", "2025", "2026"],
    "transaction": "auto",
    "timeoutSeconds": 30,
    "destructive": false,
    "tags": ["geometry", "support", "boundary", "restraint"],
    "title": "Assign node support",
    "description": "Assigns support conditions (boundary restraints) to specified node numbers. Supports standard types (Fixed, Pinned, Roller) or named support labels.",
    "inputSchema": {
      "type": "object",
      "properties": {
        "nodeNumbers": {
          "type": "string",
          "description": "Node selection string, e.g. '1 2 3' or '1to5'."
        },
        "supportType": {
          "type": "string",
          "description": "Support name or standard type: 'Fixed', 'Pinned', 'Roller'."
        }
      },
      "required": ["nodeNumbers", "supportType"],
      "additionalProperties": false
    }
  }
  ```
- **code.cs**:
  ```csharp
  string nodeSelText = args.Require("nodeNumbers");
  string suppType = args.Require("supportType");

  var sel = structure.Selections.Create(IRobotObjectType.I_OT_NODE);
  sel.FromText(nodeSelText);
  if (sel.Count == 0) throw new ArgumentException($"No valid nodes found matching selection '{nodeSelText}'.");

  structure.Nodes.SetLabel(sel, IRobotLabelType.I_LT_SUPPORT, suppType);

  log($"Assigned support '{suppType}' to {sel.Count} nodes: {sel.ToText()}");

  return new
  {
      success = true,
      support = suppType,
      nodeCount = sel.Count,
      assignedNodes = sel.ToText()
  };
  ```
- **examples.json**:
  ```json
  [
    {
      "title": "Pin nodes 1 and 2",
      "input": {
        "nodeNumbers": "1 2",
        "supportType": "Pinned"
      }
    }
  ]
  ```

---

#### Seed 8: `Property/assign_bar_section`
- **tool.json**:
  ```json
  {
    "name": "assign_bar_section",
    "version": 1,
    "status": "published",
    "author": "hprebar",
    "category": "Property",
    "host": "robot",
    "hostVersions": ["2023", "2024", "2025", "2026"],
    "transaction": "auto",
    "timeoutSeconds": 30,
    "destructive": false,
    "tags": ["property", "section", "bar", "assign"],
    "title": "Assign bar section",
    "description": "Assigns a defined cross-section profile to specified bars in the Robot model.",
    "inputSchema": {
      "type": "object",
      "properties": {
        "barNumbers": {
          "type": "string",
          "description": "Bar selection text, e.g. '1 2 3' or '1to10'."
        },
        "sectionName": {
          "type": "string",
          "description": "Name of the section to assign (e.g. 'IPE 300', 'HEA 200')."
        }
      },
      "required": ["barNumbers", "sectionName"],
      "additionalProperties": false
    }
  }
  ```
- **code.cs**:
  ```csharp
  string barSelText = args.Require("barNumbers");
  string sectionName = args.Require("sectionName");

  var sel = structure.Selections.Create(IRobotObjectType.I_OT_BAR);
  sel.FromText(barSelText);
  if (sel.Count == 0) throw new ArgumentException($"No valid bars found matching selection '{barSelText}'.");

  structure.Bars.SetLabel(sel, IRobotLabelType.I_LT_BAR_SECTION, sectionName);

  log($"Assigned section '{sectionName}' to {sel.Count} bars: {sel.ToText()}");

  return new
  {
      success = true,
      section = sectionName,
      barCount = sel.Count,
      assignedBars = sel.ToText()
  };
  ```
- **examples.json**:
  ```json
  [
    {
      "title": "Assign IPE 300 to bars 1 to 5",
      "input": {
        "barNumbers": "1to5",
        "sectionName": "IPE 300"
      }
    }
  ]
  ```

---

#### Seed 9: `Load/assign_bar_load`
- **tool.json**:
  ```json
  {
    "name": "assign_bar_load",
    "version": 1,
    "status": "published",
    "author": "hprebar",
    "category": "Load",
    "host": "robot",
    "hostVersions": ["2023", "2024", "2025", "2026"],
    "transaction": "auto",
    "timeoutSeconds": 30,
    "destructive": false,
    "tags": ["load", "bar", "uniform", "force"],
    "title": "Assign bar load",
    "description": "Applies a uniform or concentrated force load to structural bars in a specified load case. Uses metric units (kN/m for uniform, kN for concentrated).",
    "inputSchema": {
      "type": "object",
      "properties": {
        "caseNumber": {
          "type": "integer",
          "description": "Target load case number."
        },
        "barNumbers": {
          "type": "string",
          "description": "Bar selection text, e.g. '1 2' or '1to4'."
        },
        "loadType": {
          "type": "string",
          "enum": ["uniform", "concentrated"],
          "default": "uniform",
          "description": "Type of load."
        },
        "pz": {
          "type": "number",
          "description": "Load intensity in Z direction (kN/m or kN). Negative value indicates downwards."
        },
        "px": {
          "type": "number",
          "default": 0.0,
          "description": "Load intensity in X direction."
        },
        "py": {
          "type": "number",
          "default": 0.0,
          "description": "Load intensity in Y direction."
        },
        "relativePosition": {
          "type": "number",
          "default": 0.5,
          "description": "Relative position along bar (0.0 to 1.0) for concentrated loads."
        },
        "isLocal": {
          "type": "boolean",
          "default": false,
          "description": "Whether coordinates are in local bar system instead of global."
        }
      },
      "required": ["caseNumber", "barNumbers", "pz"],
      "additionalProperties": false
    }
  }
  ```
- **code.cs**:
  ```csharp
  int caseNum = args.Int("caseNumber");
  string barSelText = args.Require("barNumbers");
  string loadType = args.Str("loadType", "uniform").ToLowerInvariant();
  double pz = args.Double("pz");
  double px = args.Double("px", 0.0);
  double py = args.Double("py", 0.0);
  double relPos = Math.Clamp(args.Double("relativePosition", 0.5), 0.0, 1.0);
  bool isLocal = args.Bool("isLocal", false);

  if (structure.Cases.Exist(caseNum) == 0)
      throw new ArgumentException($"Load case {caseNum} does not exist in the model.");

  var c = structure.Cases.Get(caseNum);
  if (c is not IRobotSimpleCase sc)
      throw new InvalidOperationException($"Case {caseNum} is not a simple load case (records cannot be added to combinations directly).");

  var barSel = structure.Selections.Create(IRobotObjectType.I_OT_BAR);
  barSel.FromText(barSelText);
  if (barSel.Count == 0) throw new ArgumentException($"No valid bars found for selection '{barSelText}'.");

  if (loadType == "uniform")
  {
      int recIdx = sc.Records.New(IRobotLoadRecordType.I_LRT_BAR_UNIFORM);
      var rec = sc.Records.Get(recIdx);
      rec.SetValue((short)IRobotUniformRecordValues.I_URV_PX, px);
      rec.SetValue((short)IRobotUniformRecordValues.I_URV_PY, py);
      rec.SetValue((short)IRobotUniformRecordValues.I_URV_PZ, pz);
      rec.SetValue((short)IRobotUniformRecordValues.I_URV_LOCAL_SYSTEM, isLocal ? 1.0 : 0.0);
      rec.Objects.FromText(barSel.ToText());
  }
  else
  {
      int recIdx = sc.Records.New(IRobotLoadRecordType.I_LRT_BAR_FORCE_CONCENTRATED);
      var rec = sc.Records.Get(recIdx);
      rec.SetValue((short)IRobotBarForceConcentrateRecordValues.I_BFCRV_FX, px);
      rec.SetValue((short)IRobotBarForceConcentrateRecordValues.I_BFCRV_FY, py);
      rec.SetValue((short)IRobotBarForceConcentrateRecordValues.I_BFCRV_FZ, pz);
      rec.SetValue((short)IRobotBarForceConcentrateRecordValues.I_BFCRV_X, relPos);
      rec.SetValue((short)IRobotBarForceConcentrateRecordValues.I_BFCRV_REL, 1.0);
      rec.SetValue((short)IRobotBarForceConcentrateRecordValues.I_BFCRV_LOC, isLocal ? 1.0 : 0.0);
      rec.Objects.FromText(barSel.ToText());
  }

  log($"Applied {loadType} load (Pz={pz} kN) to case {caseNum} on bars {barSel.ToText()}");

  return new
  {
      success = true,
      caseNumber = caseNum,
      bars = barSel.ToText(),
      loadType,
      pz, px, py,
      isLocal
  };
  ```
- **examples.json**:
  ```json
  [
    {
      "title": "Apply -15 kN/m uniform gravity load to bars 1 to 4 in case 1",
      "input": {
        "caseNumber": 1,
        "barNumbers": "1to4",
        "loadType": "uniform",
        "pz": -15.0
      }
    }
  ]
  ```

---

#### Seed 10: `Analysis/run_calculations`
- **tool.json**:
  ```json
  {
    "name": "run_calculations",
    "version": 1,
    "status": "published",
    "author": "hprebar",
    "category": "Analysis",
    "host": "robot",
    "hostVersions": ["2023", "2024", "2025", "2026"],
    "transaction": "auto",
    "timeoutSeconds": 300,
    "destructive": true,
    "tags": ["analysis", "calculate", "fea", "solver", "destructive"],
    "title": "Run calculations",
    "description": "Generates the computational FEA model and executes structural calculations. Heavy/Destructive operation requiring AllowHeavyOperations toggle.",
    "inputSchema": {
      "type": "object",
      "properties": {
        "autoGenerateModel": {
          "type": "boolean",
          "default": true,
          "description": "Automatically regenerate mesh/computational model before solver execution."
        }
      },
      "additionalProperties": false
    }
  }
  ```
- **code.cs**:
  ```csharp
  bool autoGen = args.Bool("autoGenerateModel", true);

  log("Starting structural calculation solver in Robot...");
  project.CalcEngine.AutoGenerateModel = autoGen;
  int ret = project.CalcEngine.Calculate();

  if (ret != 0)
      throw new InvalidOperationException($"Robot calculation solver returned error code {ret}. Inspect model geometry and boundary conditions.");

  bool available = structure.Results.Available != 0;
  string status = structure.Results.Status.ToString();

  log($"Calculations finished. Results available: {available}, Status: {status}");

  return new
  {
      success = true,
      returnCode = ret,
      resultsAvailable = available,
      status = status,
      summary = $"Calculation completed with status: {status}."
  };
  ```
- **examples.json**:
  ```json
  [
    {
      "title": "Run complete calculations",
      "input": { "autoGenerateModel": true }
    }
  ]
  ```

---

#### Seed 11: `Results/get_node_reactions`
- **tool.json**:
  ```json
  {
    "name": "get_node_reactions",
    "version": 1,
    "status": "published",
    "author": "hprebar",
    "category": "Results",
    "host": "robot",
    "hostVersions": ["2023", "2024", "2025", "2026"],
    "transaction": "none",
    "timeoutSeconds": 30,
    "destructive": false,
    "tags": ["results", "reactions", "nodes", "supports"],
    "title": "Get node reactions",
    "description": "Extracts support reaction forces (FX, FY, FZ) and moments (MX, MY, MZ) for specified nodes and load case/combination.",
    "inputSchema": {
      "type": "object",
      "properties": {
        "caseNumber": {
          "type": "integer",
          "description": "Load case or combination number."
        },
        "nodeNumbers": {
          "type": "string",
          "description": "Node selection text, e.g. '1 2' or 'all'."
        }
      },
      "required": ["caseNumber", "nodeNumbers"],
      "additionalProperties": false
    }
  }
  ```
- **code.cs**:
  ```csharp
  int caseNum = args.Int("caseNumber");
  string nodeSelText = args.Require("nodeNumbers");

  if (structure.Results.Available == 0)
      throw new InvalidOperationException("No calculation results are available in the model. Run calculations first.");

  var sel = structure.Selections.Create(IRobotObjectType.I_OT_NODE);
  if (nodeSelText.Equals("all", StringComparison.OrdinalIgnoreCase))
  {
      var nCol = structure.Nodes.GetAll();
      for (int i = 1; i <= nCol.Count; i++)
      {
          var n = (IRobotNode)nCol.Get(i);
          if (n.HasLabel(IRobotLabelType.I_LT_SUPPORT) != 0)
              sel.AddOne(n.Number);
      }
  }
  else
  {
      sel.FromText(nodeSelText);
  }

  if (sel.Count == 0) throw new ArgumentException($"No nodes found for selection '{nodeSelText}'.");

  var reactions = new List<object>();
  double sumFx = 0, sumFy = 0, sumFz = 0;

  for (int i = 1; i <= sel.Count; i++)
  {
      int nodeId = sel.Get(i);
      var rData = structure.Results.Nodes.Reactions.Value(nodeId, caseNum);
      sumFx += rData.FX; sumFy += rData.FY; sumFz += rData.FZ;
      reactions.Add(new
      {
          nodeId,
          fx = rData.FX, fy = rData.FY, fz = rData.FZ,
          mx = rData.MX, my = rData.MY, mz = rData.MZ
      });
  }

  return new
  {
      success = true,
      caseNumber = caseNum,
      count = reactions.Count,
      sum = new { fx = sumFx, fy = sumFy, fz = sumFz },
      reactions
  };
  ```
- **examples.json**:
  ```json
  [
    {
      "title": "Get reactions on all supported nodes for case 1",
      "input": {
        "caseNumber": 1,
        "nodeNumbers": "all"
      }
    }
  ]
  ```

---

#### Seed 12: `Results/get_bar_forces`
- **tool.json**:
  ```json
  {
    "name": "get_bar_forces",
    "version": 1,
    "status": "published",
    "author": "hprebar",
    "category": "Results",
    "host": "robot",
    "hostVersions": ["2023", "2024", "2025", "2026"],
    "transaction": "none",
    "timeoutSeconds": 30,
    "destructive": false,
    "tags": ["results", "forces", "bars", "moments", "diagrams"],
    "title": "Get bar internal forces",
    "description": "Extracts internal forces (FX, FY, FZ, MX, MY, MZ) along a specified bar at N relative points (0.0 = start, 1.0 = end).",
    "inputSchema": {
      "type": "object",
      "properties": {
        "barNumber": {
          "type": "integer",
          "description": "Bar number to extract forces for."
        },
        "caseNumber": {
          "type": "integer",
          "description": "Load case or combination number."
        },
        "pointsCount": {
          "type": "integer",
          "default": 5,
          "enum": [2, 3, 5, 11],
          "description": "Number of calculation points along the bar length."
        }
      },
      "required": ["barNumber", "caseNumber"],
      "additionalProperties": false
    }
  }
  ```
- **code.cs**:
  ```csharp
  int barNum = args.Int("barNumber");
  int caseNum = args.Int("caseNumber");
  int ptsCount = Math.Clamp(args.Int("pointsCount", 5), 2, 21);

  if (structure.Results.Available == 0)
      throw new InvalidOperationException("No calculation results are available in the model. Run calculations first.");

  if (structure.Bars.Exist(barNum) == 0)
      throw new ArgumentException($"Bar {barNum} does not exist in the model.");

  var bar = (IRobotBar)structure.Bars.Get(barNum);
  double len = bar.Length;
  var points = new List<object>();

  for (int i = 0; i < ptsCount; i++)
  {
      double relPt = (double)i / (ptsCount - 1);
      var fData = structure.Results.Bars.Forces.Value(barNum, caseNum, relPt);
      points.Add(new
      {
          relativePosition = relPt,
          distance = relPt * len,
          fx = fData.FX, fy = fData.FY, fz = fData.FZ,
          mx = fData.MX, my = fData.MY, mz = fData.MZ
      });
  }

  return new
  {
      success = true,
      barNumber = barNum,
      caseNumber = caseNum,
      length = len,
      pointsCount = ptsCount,
      points
  };
  ```
- **examples.json**:
  ```json
  [
    {
      "title": "Get internal forces at 5 points for bar 1 in case 1",
      "input": {
        "barNumber": 1,
        "caseNumber": 1,
        "pointsCount": 5
      }
    }
  ]
  ```

---

## 3. 3-Tier Safety System (`RobotTierAnalyzer`)

Robot Structural Analysis does not provide an atomic undo/redo API or rollback transaction scope for out-of-process COM scripts. Once an element is deleted or modified via `RobotOM`, changes take immediate effect in memory.

To ensure safety, the HPRobot MCP Subsystem implements a multi-layered safety architecture:
1. **Roslyn AST Semantic Pre-Execution Tier Classification** (`RobotTierAnalyzer`)
2. **Deterministic Member Allow-List Fixture** (`RobotTierTable` & `robot-oapi-tiers.txt`)
3. **Automated RTD Snapshot Engine** (`RobotSnapshotManager`)
4. **Metric Standardizer & Scope Guard** (`RobotUnitsPolicy`)

### 3.1 Operation Tiers Defined

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Tier R (Read-Only)                                                        │
│  - Methods: Get*, Is*, Has*, Count, Find*, Exist, Query, Preferences.Get*   │
│  - No modifications to model                                                │
│  - Allowed whenever AllowExecution is checked                               │
│  - ZERO snapshot overhead                                                   │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
┌──────────────────────────────────────▼──────────────────────────────────────┐
│  Tier W (Write / Mutation)                                                 │
│  - Methods: Create, Add*, SetLabel*, SetValue*, Store*, Update              │
│  - Adds or updates nodes, bars, materials, sections, loads, restraints      │
│  - Gated behind AllowExecution toggle                                       │
│  - AUTOMATIC .RTD SNAPSHOT taken before execution                           │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │
┌──────────────────────────────────────▼──────────────────────────────────────┐
│  Tier D / Heavy (Destructive & Calculations)                               │
│  - Methods: Delete, DeleteMany, Remove*, Clear*, Calculate*, GenerateModel  │
│  - File operations: project.New, project.Open, project.Close, SaveAs        │
│  - Gated behind BOTH AllowExecution AND AllowHeavyOperations toggles        │
│  - Rejection with -32001 if AllowHeavyOperations is false                   │
│  - AUTOMATIC .RTD SNAPSHOT taken before execution                           │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### 3.2 AST Classification Mechanics (`RobotTierAnalyzer`)

`RobotTierAnalyzer` examines the syntax trees and semantic model of a compiled Roslyn script (`Script<object>`):

1. **Visitor Pattern**:
   Inspects all `MemberAccessExpressionSyntax` and `MemberBindingExpressionSyntax` (`x?.Member`).
2. **Symbol Resolution**:
   Resolves the invoked member symbol (`model.GetSymbolInfo(node)`).
   Filters out non-`RobotOM` namespaces (e.g. `System.Linq`, `System.Collections`).
3. **Lookup in `RobotTierTable`**:
   - Matches `<InterfaceName>.<MemberName>`.
   - Returns assigned tier (`ReadOnly`, `Write`, `Destructive`).
   - **Fail-Closed Principle**: Any member of a `RobotOM` type that is NOT present in the table is automatically classified as `Destructive` (Tier D)!
4. **Path Screening (`RobotPathPolicy`)**:
   - For members marked with `path=i` in the tier fixture (e.g. `IRobotProject.Open(string)` or `SaveAs(string)`):
   - Enforces path literals or `args.Str("path")` expressions.
   - Forbids UNC network shares (`\\server\share`), directory traversal (`../`), system roots (`C:\Windows`).
5. **Static Preview Mode**:
   When `dryRun = true` or `transaction = "none"` on a script containing Tier W or Tier D calls:
   Returns an execution error with `PREVIEW` diagnostics:
   ```csharp
   new ScriptDiagnostic(line, col, "PREVIEW", $"{member} ({(tier == Destructive ? "D" : "W")})")
   ```

---

### 3.3 Snapshot Manager (`RobotSnapshotManager`)

Before any Tier W or Tier D script executes:
1. **Snapshotable Verification**:
   - The active model must have a valid rooted file path (`project.FileName`).
   - Unsaved new models (`""`) or UNC paths throw `BridgeRequestException(BridgeErrorCode.NoActiveDocument, "Save the model in Robot first; writing scripts need an existing .rtd file to snapshot.")`.
2. **Dual-Bucket Snapshot Process**:
   - **Presave Bucket**: If the model was modified by the user since the bridge's last operation, copies the current file to:
     `%LocalAppData%\HPRobot\McpBridge\snapshots\<model>\presave\<stamp>-<label>-presave.rtd`
     (retains last 5 copies).
   - **Model Save**: Invokes `project.Save()` to persist in-memory state.
   - **Prerun Bucket**: Copies the saved file to:
     `%LocalAppData%\HPRobot\McpBridge\snapshots\<model>\prerun\<stamp>-<label>.rtd`
     (retains last 10 copies).
3. **Response Integration**:
   The generated snapshot file name is returned in `ExecuteResult.Snapshot`. AI clients receive confirmation of the snapshot in the tool call response:
   ```json
   {
     "success": true,
     "snapshot": "20260921-143000-draw_bar.rtd"
   }
   ```

---

### 3.4 Metric Units Enforcement (`RobotUnitsPolicy`)

Because structural analysis calculations rely heavily on unit consistency, `RobotUnitsPolicy` wraps every script execution:
```csharp
public static class RobotUnitsPolicy
{
    public static ScriptUnits Units { get; } = new ScriptUnits(
        "m, kN, kN·m, MPa", 1.0,
        "lengths in m, forces in kN, moments in kN·m, stresses in MPa; user unit preferences are restored in finally");

    public static string? Run(IRobotApplication robot, List<string> logs, Action body)
    {
        var unitMngr = robot.Project.Preferences.Units;
        var savedDims = unitMngr.Get(IRobotUnitType.I_UT_STRUCTURE_DIMENSION);
        var savedForces = unitMngr.Get(IRobotUnitType.I_UT_FORCE);
        var savedMoments = unitMngr.Get(IRobotUnitType.I_UT_MOMENT);
        var savedStresses = unitMngr.Get(IRobotUnitType.I_UT_STRESS);

        try
        {
            // Force Metric working units
            unitMngr.UseMetricAsDefault = true;
            body();
        }
        finally
        {
            try
            {
                unitMngr.Set(IRobotUnitType.I_UT_STRUCTURE_DIMENSION, savedDims);
                unitMngr.Set(IRobotUnitType.I_UT_FORCE, savedForces);
                unitMngr.Set(IRobotUnitType.I_UT_MOMENT, savedMoments);
                unitMngr.Set(IRobotUnitType.I_UT_STRESS, savedStresses);
                unitMngr.Refresh();
            }
            catch (Exception ex)
            {
                logs.Add($"warning: restoring user units threw {ex.GetType().Name}.");
            }
        }
        return null;
    }
}
```

---

## 4. Test Suite Architecture & Live Harness

### 4.1 Test Project Layout

```
HPRobot/
├── HPRobot.Mcp.Server.Tests/           # .NET 10 xUnit v3 (Microsoft.Testing.Platform)
│   ├── RobotHostProfileTests.cs        # Validates profile properties, ceilings, tool lists
│   ├── RobotToolsOverPipeTests.cs      # Full named pipe round-trip against FakeRevitExecutor
│   ├── SeedLibraryStructureTests.cs    # Validates tool.json schema & examples for all 12 seeds
│   └── SeedLibraryCompileTests.cs      # Compiles all 12 seeds against Interop.RobotOM.dll
│
├── HPRobot.McpBridge.Tests/            # .NET 8.0-windows xUnit v3
│   ├── RobotTierAnalyzerTests.cs       # Tests R/W/D classification, preview diagnostics
│   ├── RobotTierFixtureTests.cs        # Validates robot-oapi-tiers.txt fixture integrity
│   ├── RobotSnapshotManagerTests.cs    # Unit tests for snapshot creation, pruning, errors
│   ├── RobotPathPolicyTests.cs         # Path traversal, UNC share rejection tests
│   ├── RobotUnitsPolicyTests.cs        # Metric unit setting and restoration tests
│   └── RobotExecutorRefusalTests.cs    # Rejection behavior when opt-ins are disabled
│
└── tools/harness/                      # Live verification against running Robot 2026
    ├── run-live-verify.ps1             # PowerShell unattended orchestrator
    └── live-verify.py                  # Python test suite inheriting McpShared/tools/harness_common.py
```

### 4.2 Detailed Test Matrix

#### `HPRobot.Mcp.Server.Tests` (.NET 10):
1. `Profile_advertises_robot_host_and_expected_tool_names`: Verifies `HostId == "robot"`, `MethodPrefix == "robot."`, `DefaultVersion == 2026`, and `MaxTimeoutSeconds == 300`.
2. `Context_tool_returns_robot_block_and_hides_other_hosts`: Verifies that `get_robot_context` populates the `robot` block and suppresses `revitVersion`, `autocad`, `navis`, `etabs`, `sap2000`, `excel`.
3. `Execute_tool_routes_over_pipe_and_captures_snapshot`: Simulates execution of writing code and verifies that the returned JSON contains the snapshot filename.
4. `Execute_tool_clamps_timeout_to_300s`: Verifies that requests with 600s are clamped to 300s.
5. `Static_preview_returns_preview_diagnostic`: Verifies that dry-run calls return `PREVIEW` diagnostic without executing.
6. `Seed_tools_structure_and_schema_validation`: Parametric test across all 12 seeds verifying valid `tool.json`, presence of `code.cs`, and valid `examples.json`.
7. `Seed_tools_compile_against_RobotOM`: Compiles all 12 C# seed codes using Roslyn in-memory compilation with `Interop.RobotOM.dll` reference. Asserts 0 diagnostic errors.
8. `Seed_tools_tier_matches_declared_transaction`: Verifies that read-only seeds use `transaction: "none"` and heavy seeds have `destructive: true`.

#### `HPRobot.McpBridge.Tests` (.NET 8.0-windows):
1. `TierAnalyzer_classifies_read_methods_as_ReadOnly`: Tests AST visitor with `structure.Nodes.GetAll()`, `structure.Results.Bars.Forces.Value()`.
2. `TierAnalyzer_classifies_creation_methods_as_Write`: Tests `structure.Nodes.Create()`, `structure.Bars.Create()`, `structure.Bars.SetLabel()`.
3. `TierAnalyzer_classifies_calculation_and_deletion_as_Destructive`: Tests `project.CalcEngine.Calculate()`, `structure.Nodes.Delete()`, `project.Open()`.
4. `TierAnalyzer_flags_unknown_members_as_Destructive`: Unlisted API methods fail closed.
5. `SnapshotManager_creates_prerun_copy_on_write`: Validates file creation and timestamp format.
6. `SnapshotManager_prunes_older_than_limit`: Verifies retention limit (10 prerun files).
7. `SnapshotManager_refuses_unsaved_or_unc_models`: Asserts `BridgeRequestException`.
8. `PathPolicy_rejects_directory_traversal_and_unc`: Asserts refusal on `..\..\secret.rtd` and `\\server\share\model.rtd`.

### 4.3 Live Verification Harness (`HPRobot/tools/harness/`)

The live harness connects to Robot Structural Analysis Professional 2026 unattended using `harness_common.py`:

```
Phase 1: disabled          - Execution opt-in OFF -> execute fails with -32001 naming bridge app.
Phase 2: detached          - Robot not running -> context returns isAttached=false.
Phase 3: bridge            - Attached to saved throw-away model, AllowHeavy=false:
                             * Read seeds (get_model_info, get_structural_objects) PASS.
                             * Write seed (draw_bar_by_coords) creates .rtd snapshot and executes.
                             * Run calculations is BLOCKED with -32001 (AllowHeavy required).
Phase 4: bridgedestructive - AllowHeavy=true enabled:
                             * run_calculations executes solver, generates FEA mesh.
                             * Results seeds (get_node_reactions, get_bar_forces) PASS.
Phase 5: registry          - Propose, test, publish, call dynamic tool in <5 seconds.
```

---

## 5. Solution Structure & Build System

### 5.1 Solution Topology (`HPRobot/HPRobot.slnx`)

```xml
<Solution>
  <Configurations>
    <BuildType Name="Debug" />
    <BuildType Name="Release" />
  </Configurations>
  <Folder Name="/Solution Items/">
    <File Path="global.json" />
    <File Path="Directory.Build.props" />
    <File Path="README.md" />
  </Folder>
  <Project Path="HPRobot.McpBridge/HPRobot.McpBridge.csproj" />
  <Project Path="HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj" />
  <Project Path="HPRobot.Mcp.Server/HPRobot.Mcp.Server.csproj" />
  <Project Path="HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj" />
  <Folder Name="/Shared/">
    <Project Path="../McpShared/HPRebar.Mcp.Contracts/HPRebar.Mcp.Contracts.csproj" />
    <Project Path="../McpShared/HPRebar.McpBridge.Core/HPRebar.McpBridge.Core.csproj" />
    <Project Path="../McpShared/HPRebar.Mcp.Server.Core/HPRebar.Mcp.Server.Core.csproj" />
  </Folder>
</Solution>
```

### 5.2 Build Configuration (`HPRobot/Directory.Build.props`)

```xml
<Project>
  <!--
    Locates Interop.RobotOM.dll on the dev machine.
    1. Env variable HPROBOT_ROBOT_DIR
    2. %ProgramW6432%\Autodesk\Robot Structural Analysis Professional 2026\Exe\
    3. Fallback to common Autodesk directory
  -->
  <PropertyGroup>
    <RobotMajor Condition="'$(RobotMajor)' == ''">2026</RobotMajor>
    <RobotInstallDir Condition="'$(RobotInstallDir)' == '' And '$(HPROBOT_ROBOT_DIR)' != ''">$(HPROBOT_ROBOT_DIR)</RobotInstallDir>
    <RobotInstallDir Condition="'$(RobotInstallDir)' == ''">$(ProgramW6432)\Autodesk\Robot Structural Analysis Professional $(RobotMajor)\Exe\</RobotInstallDir>
    <RobotInstallDir Condition="!HasTrailingSlash('$(RobotInstallDir)')">$(RobotInstallDir)\</RobotInstallDir>

    <RobotApiAvailable>false</RobotApiAvailable>
    <RobotApiAvailable Condition="Exists('$(RobotInstallDir)Interop.RobotOM.dll')">true</RobotApiAvailable>
  </PropertyGroup>
</Project>
```

---

## 6. McpShared Integration Touchpoints

To fully integrate Robot into `McpShared`, the following constants and contracts are established:
1. `PipeNaming.RobotHost = "robot"`; `PipeNaming.For("robot", 2026)` -> `"hprobot-mcp-2026"`.
2. `JsonRpcMethods.RobotPrefix = "robot."`.
3. `HostScriptContracts.RobotImports = new[] { "RobotOM", "System", "System.Collections.Generic", "System.Linq" }`.
4. `HostScriptContracts.RobotGlobals = "robot, structure, project, units, args, ct, log, progress"`.
5. `HostScriptContracts.RobotHeavyMaxTimeoutSeconds = 300`.
6. `GuardProfile.Robot` & `AnalyzerProfile.Robot` permitting `RobotOM` while rejecting `System.IO.File`, `System.Diagnostics.Process`, reflection, `#r`/`#load`.
7. `ContextResult.Robot`: `RobotInfo` DTO recording `IsAttached`, `AttachedPid`, `OapiVersion`, `ProjectType`, `ProjectPath`, `IsLocked`, `ResultsAvailable`, `NodeCount`, `BarCount`, `PanelCount`, `CaseCount`, `HeavyOperationsEnabled`.
8. Baseline preservation: McpShared existing 164 net10 tests + 62 net48 tests continue to pass 100%.

---

## 7. Next Steps for Implementation Team

1. **Implement McpShared touchpoints**: Add Robot profile, contracts, and DTOs. Verify no regression on existing tests.
2. **Scaffold `HPRobot/` Projects**: Create `HPRobot.McpBridge` and `HPRobot.Mcp.Server` with `HPRobot.slnx`.
3. **Embed 12 Seed Tools**: Place `tool.json`, `code.cs`, `examples.json` in `HPRobot.Mcp.Server/Registry/SeedLibrary/<Category>/<Name>/`.
4. **Implement Bridge Services**: Wire `RobotAttachment`, `RobotTierAnalyzer`, `RobotSnapshotManager`, `RobotUnitsPolicy`, and WPF MVVM UI.
5. **Run Automated Test Suites**: Run `dotnet test HPRobot.Mcp.Server.Tests` and `dotnet test HPRobot.McpBridge.Tests`.
6. **Execute Live Verification**: Run `run-live-verify.ps1` with Robot 2026 open.
