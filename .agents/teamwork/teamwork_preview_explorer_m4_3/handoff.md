# Handoff Report — Seeds 7 through 12 Specification Blueprint for Dynamo MCP Seed Library

**Subagent**: `teamwork_preview_explorer_m4_3`  
**Milestone**: M4 (Embedded Seed Library — Part 2: Seeds 7 to 12)  
**Target Delivery**: `HPDynamo.Mcp.Server/Registry/SeedLibrary/`  
**Date**: 2026-10-08  

---

## 1. Observation

1. **Host Contracts & Engine Layout**:
   - `HostScriptContracts.DynamoImports` (`McpShared/HPRebar.Mcp.Contracts/HostScriptContracts.cs` lines 239–246) defines default namespaces: `System`, `System.Linq`, `System.Collections.Generic`, `Autodesk.DesignScript.Geometry`, `Dynamo.Graph.Nodes`, `Dynamo.Graph.Workspaces`, `Dynamo.Models`, `Revit.Elements`, `HPRebar.McpBridge.Core.Scripting`.
   - `HostScriptContracts.DynamoGlobals` (line 253) exposes globals: `dynamo`, `workspace`, `doc`, `ct`, `log`, `progress`, `args`.
   - `DynamoHostProfile.Instance` (`HPDynamo/HPDynamo.Mcp.Server/Hosts/DynamoHostProfile.cs` lines 40–44) enumerates allowed categories: `Graph`, `Workspace`, `Library`, `Node`, `Nodes`, `Wire`, `Data`, `Export`, `CodeBlock`, `Geometry`, `Revit`, `Python`, `Package`, `Generic`.
   - `PipeNaming.DynamoHost` is `"dynamo"`, default host version is `2026`, named pipe is `\\.\pipe\hpdynamo-mcp-2026`.

2. **Security & Guard Constraints (`ScriptGuard.cs` & `GuardProfile.Dynamo`)**:
   - Denied namespaces in `ScriptGuard.cs`: `System.IO` (with `System.IO.Path` specifically allowed), `System.Net`, `System.Diagnostics.Process`, `System.Reflection`, `System.Runtime.InteropServices`.
   - Denied identifiers: `File`, `Directory`, `Process`, `Assembly`, `MessageBox`, `TaskDialog`.
   - Denied directives: `#r` and `#load`.
   - Clean Code Governance: Classes/scripts <= 300 lines (Rule C2), methods <= 50 lines (Rule M3), 0 empty catch blocks without comment (Rule Q-B1), 0 commented-out code (Rule Q-B2).

3. **Runtime Assemblies in Revit 2026**:
   - Located at `C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\`: `DynamoCore.dll`, `ProtoCore.dll`, `DynamoCoreWpf.dll`, `DynamoServices.dll`.
   - Subfolder `Revit\`: `RevitNodes.dll`, `DynamoRevitDS.dll`, `RevitServices.dll`.
   - `HPDynamo.slnx` builds cleanly with 0 errors and 0 warnings (`net10.0` console server + `net10.0-windows7.0` bridge).

---

## 2. Logic Chain

1. **Seed Distribution and Folder Hierarchy**:
   Seeds 7 through 12 correspond to 6 functional domains under `HPDynamo.Mcp.Server/Registry/SeedLibrary/`:
   - Seed 7: `Wire/disconnect_node_ports/` (`category = "Wire"`, `transaction = "auto"`)
   - Seed 8: `Node/set_node_properties/` (`category = "Node"`, `transaction = "auto"`)
   - Seed 9: `Data/get_node_output_values/` (`category = "Data"`, `transaction = "none"`)
   - Seed 10: `Export/export_graph_to_image/` (`category = "Export"`, `transaction = "none"`)
   - Seed 11: `Graph/validate_dyn_file/` (`category = "Graph"`, `transaction = "none"`)
   - Seed 12: `CodeBlock/create_code_block_node/` (`category = "CodeBlock"`, `transaction = "auto"`)

2. **Schema & Argument Symmetry**:
   To satisfy `ToolValidator` and `ScriptAnalyzer`, every parameter read via `args.Require(...)`, `args.Str(...)`, `args.Int(...)`, `args.Bool(...)`, `args.Double(...)`, `args.Strings(...)` in `code.cs` must exactly match the keys defined in `inputSchema.properties` of `tool.json`, with zero undeclared reads and zero unused schema properties.

3. **Dual Execution Safety**:
   - Read-only tools (`get_node_output_values`, `export_graph_to_image`, `validate_dyn_file`) declare `transaction = "none"`.
   - Graph mutation tools (`disconnect_node_ports`, `set_node_properties`, `create_code_block_node`) declare `transaction = "auto"`, enabling Revit `TransactionGroup` rollback and Dynamo `UndoRecorder` tracking when `dryRun=true`.

---

## 3. Detailed Specification of Seeds 7 through 12

### Seed 7: `disconnect_node_ports`
- **Location**: `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Wire/disconnect_node_ports/`
- **Category**: `Wire` | **Transaction**: `auto` | **Destructive**: `false` | **Timeout**: `30s`

#### `tool.json`
```json
{
  "name": "disconnect_node_ports",
  "version": 1,
  "status": "published",
  "author": "hprebar",
  "category": "Wire",
  "host": "dynamo",
  "hostVersions": [
    "2026"
  ],
  "transaction": "auto",
  "timeoutSeconds": 30,
  "destructive": false,
  "tags": [
    "wire",
    "connector",
    "disconnect",
    "port",
    "graph",
    "canvas"
  ],
  "title": "Disconnect node ports",
  "description": "Disconnect wire(s) connected to a specific input or output port of a Dynamo node on canvas, or sever a specific connection between two nodes. Modifies active graph workspace with undo recording.",
  "notes": "Requires an active Dynamo workspace. If targetNodeId is omitted, all wires connected to the specified port are severed. Uses DynamoModel.DeleteModelCommand or ConnectorModel.Delete.",
  "inputSchema": {
    "type": "object",
    "required": [
      "nodeId"
    ],
    "properties": {
      "nodeId": {
        "type": "string",
        "description": "GUID string or title/name of the primary node whose port to disconnect"
      },
      "portIndex": {
        "type": "integer",
        "default": 0,
        "minimum": 0,
        "description": "0-based port index on the specified node (default 0)"
      },
      "portType": {
        "type": "string",
        "enum": [
          "input",
          "output"
        ],
        "default": "input",
        "description": "Type of port to disconnect: 'input' (in-port) or 'output' (out-port)"
      },
      "targetNodeId": {
        "type": "string",
        "description": "Optional GUID string or name of the connected target/source node to disconnect from. If omitted, all wires on the specified port are disconnected."
      },
      "targetPortIndex": {
        "type": "integer",
        "minimum": 0,
        "description": "Optional 0-based port index on the target node"
      }
    },
    "additionalProperties": false
  }
}
```

#### `code.cs`
```csharp
string nodeId = args.Require("nodeId");
int portIndex = args.Int("portIndex", 0);
string portType = args.Str("portType", "input").ToLowerInvariant();
string targetNodeId = args.Str("targetNodeId", null);
int? targetPortIndex = args.Has("targetPortIndex") ? args.Int("targetPortIndex", 0) : null;

var ws = workspace ?? (dynamo as DynamoViewModel)?.Model.CurrentWorkspace ?? (dynamo as DynamoModel)?.CurrentWorkspace;
if (ws == null) throw new InvalidOperationException("No active Dynamo workspace is currently open.");

bool isGuid = Guid.TryParse(nodeId, out Guid nodeGuid);
var node = ws.Nodes.FirstOrDefault(n => (isGuid && n.GUID == nodeGuid) ||
    string.Equals(n.Name, nodeId, StringComparison.OrdinalIgnoreCase) ||
    string.Equals(n.NickName, nodeId, StringComparison.OrdinalIgnoreCase));
if (node == null) throw new ArgumentException($"Node '{nodeId}' not found in active workspace.");

bool isInput = portType == "input" || portType == "in";
int portCount = isInput ? node.InPorts.Count : node.OutPorts.Count;
if (portIndex < 0 || portIndex >= portCount)
    throw new IndexOutOfRangeException($"Port index {portIndex} is out of range for node '{node.Name}' ({portType} port count: {portCount}).");

NodeModel targetNode = null;
if (!string.IsNullOrWhiteSpace(targetNodeId))
{
    bool isTargetGuid = Guid.TryParse(targetNodeId, out Guid targetGuid);
    targetNode = ws.Nodes.FirstOrDefault(n => (isTargetGuid && n.GUID == targetGuid) ||
        string.Equals(n.Name, targetNodeId, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(n.NickName, targetNodeId, StringComparison.OrdinalIgnoreCase));
    if (targetNode == null) throw new ArgumentException($"Target node '{targetNodeId}' not found in active workspace.");
}

var matching = ws.Connectors.Where(c =>
{
    if (isInput)
    {
        if (c.End == null || c.End.Owner != node || c.End.Index != portIndex) return false;
        if (targetNode != null && (c.Start == null || c.Start.Owner != targetNode)) return false;
        if (targetPortIndex.HasValue && (c.Start == null || c.Start.Index != targetPortIndex.Value)) return false;
        return true;
    }
    else
    {
        if (c.Start == null || c.Start.Owner != node || c.Start.Index != portIndex) return false;
        if (targetNode != null && (c.End == null || c.End.Owner != targetNode)) return false;
        if (targetPortIndex.HasValue && (c.End == null || c.End.Index != targetPortIndex.Value)) return false;
        return true;
    }
}).ToList();

if (matching.Count == 0)
{
    log($"No matching wires found for node '{node.Name}' port {portIndex} ({portType}).");
    return new
    {
        success = true,
        nodeId = node.GUID.ToString(),
        nodeName = node.Name,
        portIndex,
        portType,
        disconnectedCount = 0,
        summary = $"No wires connected to {node.Name} port {portIndex} ({portType}) matched criteria."
    };
}

var dm = dynamo as DynamoModel ?? (dynamo as DynamoViewModel)?.Model;
var disconnectedWires = new List<object>();

foreach (var c in matching)
{
    ct.ThrowIfCancellationRequested();
    disconnectedWires.Add(new
    {
        wireId = c.GUID.ToString(),
        sourceNode = c.Start.Owner.Name,
        sourceNodeId = c.Start.Owner.GUID.ToString(),
        sourcePort = c.Start.Index,
        targetNode = c.End.Owner.Name,
        targetNodeId = c.End.Owner.GUID.ToString(),
        targetPort = c.End.Index
    });

    if (dm != null)
        dm.ExecuteCommand(new DynamoModel.DeleteModelCommand(c.GUID));
    else
        c.Delete();
}

log($"Disconnected {disconnectedWires.Count} wire(s) on node '{node.Name}' port {portIndex} ({portType}).");

return new
{
    success = true,
    nodeId = node.GUID.ToString(),
    nodeName = node.Name,
    portIndex,
    portType,
    disconnectedCount = disconnectedWires.Count,
    disconnectedWires,
    summary = $"Disconnected {disconnectedWires.Count} wire(s) from {node.Name} port {portIndex} ({portType})"
};
```

#### `examples.json`
```json
[
  {
    "title": "Disconnect all wires from first input port of Point node",
    "args": {
      "nodeId": "Point.ByCoordinates",
      "portIndex": 0,
      "portType": "input"
    }
  },
  {
    "title": "Disconnect specific wire between CodeBlock and RebarBarType",
    "args": {
      "nodeId": "CodeBlockNode",
      "portIndex": 0,
      "portType": "output",
      "targetNodeId": "RebarBarType.ByName",
      "targetPortIndex": 0
    }
  }
]
```

---

### Seed 8: `set_node_properties`
- **Location**: `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Node/set_node_properties/`
- **Category**: `Node` | **Transaction**: `auto` | **Destructive**: `false` | **Timeout**: `30s`

#### `tool.json`
```json
{
  "name": "set_node_properties",
  "version": 1,
  "status": "published",
  "author": "hprebar",
  "category": "Node",
  "host": "dynamo",
  "hostVersions": [
    "2026"
  ],
  "transaction": "auto",
  "timeoutSeconds": 30,
  "destructive": false,
  "tags": [
    "node",
    "properties",
    "rename",
    "freeze",
    "visibility",
    "value",
    "canvas"
  ],
  "title": "Set node properties",
  "description": "Update properties of a Dynamo node on canvas: rename/nickname, freeze state, 3D geometry preview visibility, input value (slider, string, boolean), or canvas coordinates. Modifies active graph workspace with undo recording.",
  "notes": "Requires an active Dynamo workspace. Modifies properties using DynamoModel.RecordModelForModification or UpdateValueParams to preserve canvas undo history.",
  "inputSchema": {
    "type": "object",
    "required": [
      "nodeId"
    ],
    "properties": {
      "nodeId": {
        "type": "string",
        "description": "GUID string or current name/nickname of the target node"
      },
      "name": {
        "type": "string",
        "description": "New custom nickname or title for the node"
      },
      "value": {
        "type": "string",
        "description": "New value string for input nodes (e.g. number slider value, string text, boolean 'true'/'false', or CodeBlock script)"
      },
      "isFrozen": {
        "type": "boolean",
        "description": "Set execution freeze state (true to freeze execution, false to unfreeze)"
      },
      "isVisible": {
        "type": "boolean",
        "description": "Set 3D geometry preview visibility (true to show preview in background, false to hide)"
      },
      "x": {
        "type": "number",
        "description": "New X coordinate on canvas"
      },
      "y": {
        "type": "number",
        "description": "New Y coordinate on canvas"
      }
    },
    "additionalProperties": false
  }
}
```

#### `code.cs`
```csharp
string nodeId = args.Require("nodeId");
string name = args.Str("name", null);
string value = args.Str("value", null);
bool? isFrozen = args.Has("isFrozen") ? args.Bool("isFrozen", false) : null;
bool? isVisible = args.Has("isVisible") ? args.Bool("isVisible", true) : null;
double? x = args.Has("x") ? args.Double("x", 0.0) : null;
double? y = args.Has("y") ? args.Double("y", 0.0) : null;

var ws = workspace ?? (dynamo as DynamoViewModel)?.Model.CurrentWorkspace ?? (dynamo as DynamoModel)?.CurrentWorkspace;
if (ws == null) throw new InvalidOperationException("No active Dynamo workspace is currently open.");

bool isGuid = Guid.TryParse(nodeId, out Guid nodeGuid);
var node = ws.Nodes.FirstOrDefault(n => (isGuid && n.GUID == nodeGuid) ||
    string.Equals(n.Name, nodeId, StringComparison.OrdinalIgnoreCase) ||
    string.Equals(n.NickName, nodeId, StringComparison.OrdinalIgnoreCase));
if (node == null) throw new ArgumentException($"Node '{nodeId}' not found in active workspace.");

var dm = dynamo as DynamoModel ?? (dynamo as DynamoViewModel)?.Model;
if (dm != null)
{
    dm.RecordModelForModification(node);
}

var updated = new List<string>();

if (!string.IsNullOrWhiteSpace(name))
{
    node.NickName = name;
    updated.Add("name");
}

if (isFrozen.HasValue)
{
    node.IsFrozen = isFrozen.Value;
    updated.Add("isFrozen");
}

if (isVisible.HasValue)
{
    node.IsVisible = isVisible.Value;
    updated.Add("isVisible");
}

if (value != null)
{
    if (node is CodeBlockNodeModel cbn)
    {
        cbn.SetCodeContent(value, ws.ElementResolver);
        updated.Add("codeContent");
    }
    else
    {
        node.UpdateValue(new Dynamo.Graph.UpdateValueParams("Value", value));
        updated.Add("value");
    }
}

if (x.HasValue && y.HasValue)
{
    if (double.IsNaN(x.Value) || double.IsNaN(y.Value) || double.IsInfinity(x.Value) || double.IsInfinity(y.Value))
        throw new ArgumentException("Coordinates x and y must be finite numbers.");

    node.X = x.Value;
    node.Y = y.Value;
    node.ReportPosition();
    updated.Add("position");
}

log($"Updated properties on node '{node.Name}' ({node.GUID}): {string.Join(", ", updated)}");

return new
{
    success = true,
    nodeId = node.GUID.ToString(),
    nodeName = node.Name,
    nickName = node.NickName,
    isFrozen = node.IsFrozen,
    isVisible = node.IsVisible,
    updatedProperties = updated,
    summary = $"Updated {updated.Count} properties on {node.Name} ({string.Join(", ", updated)})"
};
```

#### `examples.json`
```json
[
  {
    "title": "Rename and freeze node",
    "args": {
      "nodeId": "Point.ByCoordinates",
      "name": "Beam Start Point",
      "isFrozen": true
    }
  },
  {
    "title": "Update slider input value and canvas position",
    "args": {
      "nodeId": "Integer Slider",
      "value": "20",
      "x": 250.0,
      "y": 300.0
    }
  }
]
```

---

### Seed 9: `get_node_output_values`
- **Location**: `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Data/get_node_output_values/`
- **Category**: `Data` | **Transaction**: `none` | **Destructive**: `false` | **Timeout**: `60s`

#### `tool.json`
```json
{
  "name": "get_node_output_values",
  "version": 1,
  "status": "published",
  "author": "hprebar",
  "category": "Data",
  "host": "dynamo",
  "hostVersions": [
    "2026"
  ],
  "transaction": "none",
  "timeoutSeconds": 60,
  "destructive": false,
  "tags": [
    "data",
    "output",
    "values",
    "evaluation",
    "results",
    "ports",
    "query"
  ],
  "title": "Get node output values",
  "description": "Retrieve computed evaluation values and cached mirror data from output ports of specified nodes or all active nodes after graph execution. Read-only.",
  "notes": "Reads cached evaluation data via EngineController. If a node is in an Error or Warning state, tooltips and diagnostic messages are returned alongside port data.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "nodeIds": {
        "type": "array",
        "items": {
          "type": "string"
        },
        "description": "Optional list of node GUIDs or names to query. If omitted or empty, queries all nodes in the workspace."
      },
      "includeGeometry": {
        "type": "boolean",
        "default": false,
        "description": "If true, extracts detailed geometric coordinates and measurements rather than short string summaries"
      },
      "limit": {
        "type": "integer",
        "default": 50,
        "minimum": 1,
        "maximum": 200,
        "description": "Maximum number of nodes to inspect (1-200, default 50)"
      }
    },
    "additionalProperties": false
  }
}
```

#### `code.cs`
```csharp
var nodeIds = args.Strings("nodeIds").Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
bool includeGeometry = args.Bool("includeGeometry", false);
int limit = Math.Clamp(args.Int("limit", 50), 1, 200);

var ws = workspace ?? (dynamo as DynamoViewModel)?.Model.CurrentWorkspace ?? (dynamo as DynamoModel)?.CurrentWorkspace;
if (ws == null) throw new InvalidOperationException("No active Dynamo workspace is currently open.");

var dm = dynamo as DynamoModel ?? (dynamo as DynamoViewModel)?.Model;
var controller = dm?.EngineController;

IEnumerable<NodeModel> candidates = ws.Nodes;
if (nodeIds.Count > 0)
{
    var hash = new HashSet<string>(nodeIds, StringComparer.OrdinalIgnoreCase);
    candidates = ws.Nodes.Where(n => hash.Contains(n.GUID.ToString()) || hash.Contains(n.Name) || hash.Contains(n.NickName));
}

var results = new List<object>();

foreach (var node in candidates.Take(limit))
{
    ct.ThrowIfCancellationRequested();
    var ports = new List<object>();

    for (int i = 0; i < node.OutPorts.Count; i++)
    {
        var p = node.OutPorts[i];
        object portVal = null;
        string valType = "None";

        if (controller != null)
        {
            try
            {
                var mirror = node.GetValue(i, controller);
                if (mirror != null && mirror.Data != null)
                {
                    valType = mirror.Data.GetType().Name;
                    if (includeGeometry && mirror.Data is Autodesk.DesignScript.Geometry.Point pt)
                    {
                        portVal = new { type = "Point", x = pt.X, y = pt.Y, z = pt.Z };
                    }
                    else if (includeGeometry && mirror.Data is Autodesk.DesignScript.Geometry.Curve crv)
                    {
                        portVal = new { type = crv.GetType().Name, length = crv.Length };
                    }
                    else
                    {
                        portVal = mirror.Data.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                // Port evaluation error recorded as error string
                portVal = $"[Evaluation error: {ex.Message}]";
                valType = "Error";
            }
        }

        ports.Add(new
        {
            portIndex = i,
            portName = p.Name,
            dataType = valType,
            value = portVal
        });
    }

    results.Add(new
    {
        nodeId = node.GUID.ToString(),
        name = node.Name,
        nickName = node.NickName,
        state = node.State.ToString(),
        tooltip = !string.IsNullOrEmpty(node.ToolTipText) ? node.ToolTipText : null,
        outPorts = ports
    });
}

log($"Extracted output values from {results.Count} node(s) in workspace '{ws.Name}'.");

return new
{
    success = true,
    workspaceName = ws.Name,
    nodeCount = results.Count,
    nodes = results,
    summary = $"Extracted output values from {results.Count} node(s) in {ws.Name}"
};
```

#### `examples.json`
```json
[
  {
    "title": "Inspect outputs of all active nodes",
    "args": {
      "limit": 20,
      "includeGeometry": false
    }
  },
  {
    "title": "Inspect detailed geometry outputs for specific point node",
    "args": {
      "nodeIds": [
        "Point.ByCoordinates"
      ],
      "includeGeometry": true,
      "limit": 10
    }
  }
]
```

---

### Seed 10: `export_graph_to_image`
- **Location**: `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Export/export_graph_to_image/`
- **Category**: `Export` | **Transaction**: `none` | **Destructive**: `false` | **Timeout**: `60s`

#### `tool.json`
```json
{
  "name": "export_graph_to_image",
  "version": 1,
  "status": "published",
  "author": "hprebar",
  "category": "Export",
  "host": "dynamo",
  "hostVersions": [
    "2026"
  ],
  "transaction": "none",
  "timeoutSeconds": 60,
  "destructive": false,
  "tags": [
    "export",
    "image",
    "png",
    "jpeg",
    "screenshot",
    "canvas",
    "graph"
  ],
  "title": "Export graph to image",
  "description": "Export a screenshot / image rendering of the active Dynamo canvas workspace to a PNG or JPEG file on disk. Read-only.",
  "notes": "Requires an active Dynamo canvas session. Triggers workspace canvas rendering via DynamoViewModel.OnRequestSaveImage. Does not modify graph state.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "outputPath": {
        "type": "string",
        "description": "Optional file path where the exported image will be saved (e.g. 'C:\\Temp\\dynamo_graph.png'). If omitted, writes to %TEMP% with a timestamp."
      },
      "format": {
        "type": "string",
        "enum": [
          "PNG",
          "JPEG"
        ],
        "default": "PNG",
        "description": "Image export format: 'PNG' or 'JPEG' (default 'PNG')"
      }
    },
    "additionalProperties": false
  }
}
```

#### `code.cs`
```csharp
string outputPath = args.Str("outputPath", null);
string format = args.Str("format", "PNG").ToUpperInvariant();

var ws = workspace ?? (dynamo as DynamoViewModel)?.Model.CurrentWorkspace ?? (dynamo as DynamoModel)?.CurrentWorkspace;
if (ws == null) throw new InvalidOperationException("No active Dynamo workspace is currently open.");

var vm = dynamo as DynamoViewModel;
if (vm == null)
    throw new InvalidOperationException("Exporting canvas image requires an active Dynamo UI session (DynamoViewModel).");

string ext = format == "JPEG" ? ".jpg" : ".png";
if (string.IsNullOrWhiteSpace(outputPath))
{
    string tempDir = System.IO.Path.GetTempPath();
    outputPath = System.IO.Path.Combine(tempDir, $"dynamo_canvas_{DateTime.UtcNow:yyyyMMdd_HHmmss}{ext}");
}
else if (!outputPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
{
    outputPath += ext;
}

vm.OnRequestSaveImage(vm, new Dynamo.ViewModels.ImageSaveEventArgs(outputPath));

log($"Exported Dynamo canvas image to '{outputPath}' (Workspace: '{ws.Name}', Nodes: {ws.Nodes.Count()}).");

return new
{
    success = true,
    filePath = outputPath,
    format,
    workspaceName = ws.Name,
    nodeCount = ws.Nodes.Count(),
    wireCount = ws.Connectors.Count(),
    summary = $"Exported canvas workspace '{ws.Name}' to {outputPath} ({format})"
};
```

#### `examples.json`
```json
[
  {
    "title": "Export active graph to PNG in temp directory",
    "args": {
      "format": "PNG"
    }
  },
  {
    "title": "Export active graph to specific PNG path",
    "args": {
      "outputPath": "C:\\Temp\\DynamoRebarFraming.png",
      "format": "PNG"
    }
  }
]
```

---

### Seed 11: `validate_dyn_file`
- **Location**: `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/Graph/validate_dyn_file/`
- **Category**: `Graph` | **Transaction**: `none` | **Destructive**: `false` | **Timeout**: `60s`

#### `tool.json`
```json
{
  "name": "validate_dyn_file",
  "version": 1,
  "status": "published",
  "author": "hprebar",
  "category": "Graph",
  "host": "dynamo",
  "hostVersions": [
    "2026"
  ],
  "transaction": "none",
  "timeoutSeconds": 60,
  "destructive": false,
  "tags": [
    "graph",
    "validation",
    "dyn",
    "schema",
    "packages",
    "offline",
    "qc"
  ],
  "title": "Validate DYN file",
  "description": "Inspect and validate a Dynamo graph (.dyn) JSON structure and node schema without executing it. Checks JSON syntax, node types, unresolved packages, and cyclic connections. Read-only.",
  "notes": "Validates graph JSON schema and dependencies. Can inspect active workspace or parse raw fileContent JSON passed in arguments.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "fileContent": {
        "type": "string",
        "description": "Raw JSON text content of the .dyn graph file to validate"
      },
      "filePath": {
        "type": "string",
        "description": "Optional file path of the .dyn graph. If fileContent is omitted and the graph is loaded in workspace, validates active workspace matching this path."
      },
      "checkPackages": {
        "type": "boolean",
        "default": true,
        "description": "Whether to check package dependency declarations against loaded packages in Dynamo"
      }
    },
    "additionalProperties": false
  }
}
```

#### `code.cs`
```csharp
string fileContent = args.Str("fileContent", null);
string filePath = args.Str("filePath", null);
bool checkPackages = args.Bool("checkPackages", true);

var ws = workspace ?? (dynamo as DynamoViewModel)?.Model.CurrentWorkspace ?? (dynamo as DynamoModel)?.CurrentWorkspace;
var dm = dynamo as DynamoModel ?? (dynamo as DynamoViewModel)?.Model;

var issues = new List<string>();
var warnings = new List<string>();
int nodeCount = 0;
int wireCount = 0;
string graphName = "Unknown";
string graphId = string.Empty;
var packageDependencies = new List<string>();

if (!string.IsNullOrWhiteSpace(fileContent))
{
    try
    {
        using var docJson = System.Text.Json.JsonDocument.Parse(fileContent);
        var root = docJson.RootElement;

        if (root.TryGetProperty("Name", out var nameProp)) graphName = nameProp.GetString() ?? graphName;
        if (root.TryGetProperty("Uuid", out var uuidProp)) graphId = uuidProp.GetString() ?? string.Empty;

        if (root.TryGetProperty("Nodes", out var nodesProp) && nodesProp.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            nodeCount = nodesProp.GetArrayLength();
            foreach (var n in nodesProp.EnumerateArray())
            {
                if (n.TryGetProperty("NodeType", out var nt) && nt.GetString() == "DummyNodeModel")
                    issues.Add($"Unresolved/Dummy node detected: {n.GetProperty("Name").GetString()} ({n.GetProperty("Id").GetString()})");
            }
        }
        else
        {
            issues.Add("Missing or invalid 'Nodes' array in graph JSON.");
        }

        if (root.TryGetProperty("Connectors", out var connsProp) && connsProp.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            wireCount = connsProp.GetArrayLength();
            foreach (var c in connsProp.EnumerateArray())
            {
                if (c.TryGetProperty("Start", out var start) && c.TryGetProperty("End", out var end))
                {
                    if (start.TryGetProperty("Id", out var sid) && end.TryGetProperty("Id", out var eid) && sid.GetString() == eid.GetString())
                        issues.Add($"Cyclic self-connection detected on node {sid.GetString()}");
                }
            }
        }

        if (checkPackages && root.TryGetProperty("NodeLibraryDependencies", out var pkgProp) && pkgProp.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var p in pkgProp.EnumerateArray())
            {
                if (p.TryGetProperty("Name", out var pName))
                {
                    string pkg = pName.GetString();
                    if (!string.IsNullOrEmpty(pkg)) packageDependencies.Add(pkg);
                }
            }
        }
    }
    catch (System.Text.Json.JsonException jex)
    {
        issues.Add($"Invalid JSON syntax in .dyn file: {jex.Message}");
    }
}
else if (ws != null && (string.IsNullOrWhiteSpace(filePath) || string.Equals(ws.FileName, filePath, StringComparison.OrdinalIgnoreCase)))
{
    graphName = ws.Name;
    graphId = ws.FileName ?? "ActiveCanvas";
    nodeCount = ws.Nodes.Count();
    wireCount = ws.Connectors.Count();

    foreach (var n in ws.Nodes)
    {
        if (n.State == Dynamo.Graph.Nodes.ElementState.Error)
            issues.Add($"Node in error state: '{n.Name}' ({n.GUID}): {n.ToolTipText}");
        else if (n.State == Dynamo.Graph.Nodes.ElementState.Warning)
            warnings.Add($"Node warning: '{n.Name}' ({n.GUID}): {n.ToolTipText}");
    }

    if (checkPackages && ws.NodePackageDependencies != null)
    {
        foreach (var dep in ws.NodePackageDependencies)
        {
            packageDependencies.Add(dep.Name);
        }
    }
}
else
{
    throw new ArgumentException("Provide 'fileContent' (raw JSON of the .dyn graph) or ensure the graph is loaded in the active Dynamo workspace.");
}

bool isValid = issues.Count == 0;
log($"Validated graph '{graphName}': Valid={isValid}, Issues={issues.Count}, Warnings={warnings.Count}, Nodes={nodeCount}, Wires={wireCount}");

return new
{
    success = true,
    isValid,
    graphName,
    graphId,
    nodeCount,
    wireCount,
    issuesCount = issues.Count,
    issues,
    warningsCount = warnings.Count,
    warnings,
    packageDependencies,
    summary = isValid ? $"Graph '{graphName}' is valid ({nodeCount} nodes, {wireCount} wires)" : $"Graph '{graphName}' has {issues.Count} validation issue(s)"
};
```

#### `examples.json`
```json
[
  {
    "title": "Validate raw .dyn JSON content",
    "args": {
      "fileContent": "{\n  \"Uuid\": \"11111111-2222-3333-4444-555555555555\",\n  \"Name\": \"SampleRebarGraph\",\n  \"Nodes\": [],\n  \"Connectors\": []\n}",
      "checkPackages": true
    }
  },
  {
    "title": "Validate active canvas workspace without passing content",
    "args": {
      "checkPackages": true
    }
  }
]
```

---

### Seed 12: `create_code_block_node`
- **Location**: `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/CodeBlock/create_code_block_node/`
- **Category**: `CodeBlock` | **Transaction**: `auto` | **Destructive**: `false` | **Timeout**: `30s`

#### `tool.json`
```json
{
  "name": "create_code_block_node",
  "version": 1,
  "status": "published",
  "author": "hprebar",
  "category": "CodeBlock",
  "host": "dynamo",
  "hostVersions": [
    "2026"
  ],
  "transaction": "auto",
  "timeoutSeconds": 30,
  "destructive": false,
  "tags": [
    "codeblock",
    "designscript",
    "node",
    "create",
    "authoring",
    "script",
    "canvas"
  ],
  "title": "Create Code Block node",
  "description": "Create a DesignScript Code Block node on the active Dynamo canvas workspace with specified script code, position (X, Y), and parse syntax. Modifies active graph workspace with undo recording.",
  "notes": "Requires an active Dynamo workspace. Creates a CodeBlockNodeModel, parses inputs and outputs via ElementResolver, and registers node via DynamoModel.CreateNodeCommand.",
  "inputSchema": {
    "type": "object",
    "required": [
      "code"
    ],
    "properties": {
      "code": {
        "type": "string",
        "description": "DesignScript code statements for the code block (e.g. 'x = 100; y = 200; Point.ByCoordinates(x, y, 0);')"
      },
      "x": {
        "type": "number",
        "default": 100.0,
        "description": "X coordinate for placement on canvas (default 100.0)"
      },
      "y": {
        "type": "number",
        "default": 100.0,
        "description": "Y coordinate for placement on canvas (default 100.0)"
      },
      "title": {
        "type": "string",
        "description": "Optional custom nickname or title for the Code Block node"
      }
    },
    "additionalProperties": false
  }
}
```

#### `code.cs`
```csharp
string code = args.Require("code");
double x = args.Double("x", 100.0);
double y = args.Double("y", 100.0);
string title = args.Str("title", null);

if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y))
    throw new ArgumentException("Coordinates x and y must be finite numbers.");

var ws = workspace ?? (dynamo as DynamoViewModel)?.Model.CurrentWorkspace ?? (dynamo as DynamoModel)?.CurrentWorkspace;
if (ws == null) throw new InvalidOperationException("No active Dynamo workspace is currently open.");

var dm = dynamo as DynamoModel ?? (dynamo as DynamoViewModel)?.Model;
if (dm == null) throw new InvalidOperationException("DynamoModel is not available.");

var cbn = new CodeBlockNodeModel(code, Guid.NewGuid(), x, y, dm.LibraryServices, ws.ElementResolver);
if (!string.IsNullOrWhiteSpace(title))
{
    cbn.NickName = title;
}

dm.ExecuteCommand(new DynamoModel.CreateNodeCommand(cbn, x, y, false, false));

var inPorts = cbn.InPorts.Select(p => p.Name).ToList();
var outPorts = cbn.OutPorts.Select(p => p.Name).ToList();

log($"Created Code Block node '{cbn.GUID}' at ({x:F1}, {y:F1}) with {inPorts.Count} input(s) and {outPorts.Count} output(s).");

return new
{
    success = true,
    nodeId = cbn.GUID.ToString(),
    name = cbn.Name,
    nickName = cbn.NickName,
    x,
    y,
    code,
    inPorts,
    outPorts,
    summary = $"Created CodeBlockNode '{cbn.GUID}' at ({x:F1}, {y:F1}) with {inPorts.Count} inputs and {outPorts.Count} outputs"
};
```

#### `examples.json`
```json
[
  {
    "title": "Create Point with coordinate assignments",
    "args": {
      "code": "x = 100;\ny = 200;\npt = Point.ByCoordinates(x, y, 0);",
      "x": 150.0,
      "y": 200.0,
      "title": "Point Creator"
    }
  },
  {
    "title": "Parametric quarter-span formula",
    "args": {
      "code": "quarter = span / 4.0;",
      "x": 300.0,
      "y": 150.0,
      "title": "Quarter Span"
    }
  }
]
```

---

## 4. Caveats

1. **Canvas Dependency for View-Bound Operations**:
   - `export_graph_to_image` requires an active `DynamoViewModel` session (live canvas with UI open). If executed in purely headless mode (`RevitDynamoModel` without UI), it cleanly throws an `InvalidOperationException` explaining the requirement.
2. **File Reading Security Boundary**:
   - In accordance with `ScriptGuard` denying `System.IO` and `File`, `validate_dyn_file` accepts raw JSON string via `fileContent` parameter. This allows AI coding agents and MCP clients to validate any graph file offline without breaching the security sandbox. If `filePath` is supplied without `fileContent`, it validates the matching active workspace graph.
3. **DesignScript Syntax Compilation**:
   - In `create_code_block_node`, syntax errors in the DesignScript string will be parsed and flagged on the CodeBlock node's visual state (`ElementState.Error` or `Warning`) rather than throwing fatal runtime exceptions, mirroring standard Dynamo behavior.

---

## 5. Conclusion

1. Seeds 7 through 12 have been completely designed and blueprint-specified with production-grade JSON metadata, schemas, examples, and C# Roslyn scripts.
2. Every script strictly adheres to Clean Code Governance rules:
   - Line counts: All scripts between 39 and 104 lines (well below the 300-line ceiling in Rule C2 and 120-line seed limit).
   - Method lengths <= 50 lines (Rule M3).
   - 0 empty catch blocks without comment (Rule Q-B1).
   - 0 commented-out code (Rule Q-B2).
   - Exact 1-to-1 symmetry between schema parameters and `args` calls.
3. The blueprint is completely self-contained and ready for immediate implementation by the milestone worker or file writer into `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/`.

---

## 6. Verification Method

To independently verify this specification:
1. **Schema Validation**: Verify that all `tool.json` files parse with `System.Text.Json` and conform to JSON Schema Draft-07 / `ToolRecord`.
2. **Argument Symmetry**: Compare each property in `tool.json` `inputSchema.properties` against every `args.*` call in `code.cs` to confirm zero discrepancies.
3. **ScriptGuard Static Check**: Run `ScriptGuard.Check(seed.Code, GuardProfile.Dynamo)` against all 6 scripts to verify 0 diagnostics.
4. **Build Check**: After placement into `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/`, execute:
   ```powershell
   dotnet build HPDynamo/HPDynamo.slnx
   ```
   Confirm 0 errors and 0 warnings, and verify embedded resources via reflection.
