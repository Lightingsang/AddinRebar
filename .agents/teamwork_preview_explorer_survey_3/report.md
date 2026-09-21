# Technical Architecture & Implementation Design: HPTekla MCP Server, 24 Tools Catalog & Test Suites

**Author**: Teamwork Preview Explorer Survey 3  
**Date**: 2026-09-22  
**Target Solution**: `HPTekla/HPTekla.slnx`  
**Host Application**: Trimble Tekla Structures 2025.0 (.NET Framework 4.8 / CLR v4.0.30319)  
**Deliverable Scope**: `HPTekla.Mcp.Server` (.NET 10 stdio), 24 Tools Catalog (4 Core + 8 Registry Meta + 12 Embedded Seeds), Test Suites (`HPTekla.Mcp.Server.Tests`, `HPTekla.McpBridge.Tests`), and Python Live Verification Harness.

---

## 1. Executive Summary & Architectural Overview

The HPTekla subsystem integrates Tekla Structures 2025 with AI agents (Claude Code, Antigravity, Gemini CLI) using the Model Context Protocol (MCP 2.2.0). Following the architectural pattern proven across `HPRebar` (Revit), `HPAutoCad` (AutoCAD), `HPNavis` (Navisworks), `HPEtabs` (ETABS), `HPCivil3d` (Civil 3D), `HPSap2000` (SAP2000), `HPPowerBi` (Power BI), `HPExcel` (Excel), and `HPRobot` (Robot Structural Analysis), HPTekla adheres to strict repository standards:

1. **Host-Neutral Separation via `McpShared`**:
   - `HPTekla.Mcp.Server` (.NET 10 console exe) provides the standard MCP `stdio` transport. It handles JSON-RPC framing, tool registry lifecycle, and parameter validation without referencing any Tekla Open API assemblies.
   - `HPTekla.McpBridge` (.NET Framework 4.8 in-process plugin) runs inside `tekla.exe`, listening on Named Pipe `hptekla-mcp-2025`. It executes Roslyn C# scripts on the Tekla main thread with thread synchronization and safety guards.
   - All shared contracts, pipe protocols, Roslyn guard bases, and registry databases reside in `McpShared/`. `HPTekla/` references `McpShared/` only, with zero coupling to sibling CAD/BIM host projects.

2. **Comparative Pattern with Other MCP Servers**:
   - Like **`HPNavis`**, the bridge runs in-process on **.NET Framework 4.8 (`net48`)** because Tekla Structures 2025.0 runs on CLR v4.0.30319.
   - Like **`HPAutoCad`** and **`HPRobot`**, execution employs a standard 3-tier safety classification (`Read`, `Write`, `Destructive/Heavy`), where write operations require active model connection and destructive/heavy tasks (such as IFC export or model deletion) require explicit user opt-in (`AllowHeavyOperations`).
   - Like **`HPCivil3d`** and **`HPRobot`**, the server exposes **exactly 24 tools**:
     - **4 Core Tools**: `execute_tekla_code`, `get_tekla_context`, `inspect_type`, `cancel_execution`.
     - **8 Registry Meta Tools**: `search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`.
     - **12 Embedded Seed Tools**: Covering structural steel and concrete rebar workflows.

```
+-------------------------------------------------------------------------+
|                              AI Agent Host                              |
|               (Claude Code, Antigravity, Gemini CLI, etc.)              |
+-------------------------------------------------------------------------+
                                    │
                             stdio (JSON-RPC)
                                    ▼
+-------------------------------------------------------------------------+
|                  HPTekla.Mcp.Server (.NET 10 Console)                   |
|  - Bootstrapped via McpServerHost (HPRebar.Mcp.Server.Core)             |
|  - Tool Surface (24 tools): 4 Core + 8 Registry Meta + 12 Seed Tools     |
|  - Dynamic Tool Registry (SQLite + tools-library)                       |
|  - TeklaHostProfile (Pipe: hptekla-mcp-2025, Prefix: tekla.)            |
+-------------------------------------------------------------------------+
                                    │
                     Named Pipe: hptekla-mcp-2025
                                    ▼
+-------------------------------------------------------------------------+
|              HPTekla.McpBridge (In-Process Tekla Plugin, net48)         |
|  - Tekla Structures 2025.0 Extension / Plugin Entry                     |
|  - Thread Synchronization Queue on Tekla UI / Model Thread             |
|  - Roslyn Script Compiler + Caching (Microsoft.CodeAnalysis)            |
|  - 3-Tier Safety Gate (Read / Write / Destructive) + DryRun Suppressor  |
|  - WPF Status Dialog / Ribbon UI with AllowExecution & AllowHeavy Checks|
+-------------------------------------------------------------------------+
                                    │
                          Tekla Open API 2025
                                    ▼
+-------------------------------------------------------------------------+
|               Trimble Tekla Structures 2025 Model Engine                |
|  (Tekla.Structures.Model.Model, Beam, ContourPlate, RebarGroup, etc.)   |
+-------------------------------------------------------------------------+
```

---

## 2. McpShared Additive Integration Design

To support Tekla Structures without modifying or breaking any of the 9 existing host profiles, the following additions in `McpShared/` are strictly additive:

### 2.1. `HPRebar.Mcp.Contracts`
1. **`PipeNaming.cs`**:
   ```csharp
   public const string TeklaHost = "tekla";
   ```
   `PipeNaming.For("tekla", 2025)` produces `"hptekla-mcp-2025"`.
2. **`JsonRpcMethods.cs`**:
   ```csharp
   public const string TeklaPrefix = "tekla.";
   ```
   Generates methods: `tekla.execute`, `tekla.context`, `tekla.ping`, `tekla.cancel`, `tekla.inspect`, `tekla.analyze`.
3. **`HostScriptContracts.cs`**:
   ```csharp
   public static readonly string[] TeklaImports =
   {
       "System", "System.Linq", "System.Collections.Generic",
       "Tekla.Structures",
       "Tekla.Structures.Model",
       "Tekla.Structures.Geometry3d",
       "Tekla.Structures.Catalogs",
       "HPRebar.McpBridge.Core.Scripting",
   };

   public static readonly string[] TeklaGlobals = { "model", "selector", "ct", "log", "progress", "args" };

   public const int TeklaHeavyMaxTimeoutSeconds = 300;
   ```
4. **`ContextMessages.cs`**:
   In `ContextResult`:
   ```csharp
   public TeklaInfo? Tekla { get; set; }
   ```
   Record definition:
   ```csharp
   public sealed record TeklaInfo(
       bool IsConnected,
       string? ModelName,
       string? ModelPath,
       string? ProjectName,
       string? TeklaVersion,
       int PartCount,
       int RebarCount,
       int AssemblyCount,
       int DrawingCount,
       int CurrentPhase,
       bool HeavyOperationsEnabled);
   ```

### 2.2. `HPRebar.McpBridge.Core`
1. **`GuardProfile.cs`**:
   ```csharp
   public static readonly GuardProfile Tekla = new GuardProfile(
       "Tekla Structures",
       deniedIdentifiers: new[] { "MessageBox" },
       deniedMembers: new[]
       {
           // Application exit / close
           "Exit", "Quit", "Close",
           // External macros and interactive dialogs that block threads
           "RunMacro", "ShowDialog",
       },
       deniedMembersOnIdentifier: new Dictionary<string, string[]>(StringComparer.Ordinal)
       {
           // model.CommitChanges() is owned strictly by the bridge's transaction manager
           ["model"] = new[] { "CommitChanges" }
       },
       deniedNamespaces: new[]
       {
           "System.Windows.Forms",
           "Tekla.Structures.Dialog",
           "HPTekla.McpBridge",
           "HPRebar.McpBridge.Core.Host"
       });
   ```
2. **`AnalyzerProfile.cs`**:
   ```csharp
   public static readonly AnalyzerProfile Tekla = new AnalyzerProfile(
       transactionTypeNames: Array.Empty<string>(),
       transactionMethodNames: new[] { "CommitChanges" });
   ```

---

## 3. HPTekla.Mcp.Server Architecture

### 3.1. Project File: `HPTekla.Mcp.Server.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net10.0</TargetFramework>
        <LangVersion>latest</LangVersion>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <RootNamespace>HPTekla.Mcp.Server</RootNamespace>
        <Configurations>Debug;Release</Configurations>
        <InvariantGlobalization>true</InvariantGlobalization>
        <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    </PropertyGroup>

    <ItemGroup>
        <InternalsVisibleTo Include="HPTekla.Mcp.Server.Tests" />
    </ItemGroup>

    <ItemGroup>
        <!-- Host-neutral MCP server engine -->
        <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Server.Core\HPRebar.Mcp.Server.Core.csproj" />
    </ItemGroup>

    <ItemGroup>
        <Content Include="appsettings.json">
            <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
        </Content>
        <!-- Seed library embedded resources -->
        <Compile Remove="Registry\SeedLibrary\**\*.cs" />
        <EmbeddedResource Include="Registry\SeedLibrary\**\*" LogicalName="SeedLibrary/%(RecursiveDir)%(Filename)%(Extension)" />
    </ItemGroup>

</Project>
```

### 3.2. `Program.cs`
```csharp
using HPTekla.Mcp.Server.Hosts.Tekla;
using HPRebar.Mcp.Server.Bootstrap;

// The Tekla Structures 2025 MCP server:
// host-neutral bootstrap, pipe client, registry engine, and meta tools live in HPRebar.Mcp.Server.Core.
// This exe supplies TeklaHostProfile, core tools, prompts, resources, and 12 embedded seeds.
return await McpServerHost.RunAsync(args, TeklaHostProfile.Instance);
```

### 3.3. `TeklaHostProfile.cs`
```csharp
using System.Reflection;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;

namespace HPTekla.Mcp.Server.Hosts.Tekla;

public sealed class TeklaHostProfile : IHostProfile
{
    public const string ExecuteToolName = "execute_tekla_code";
    public const string ContextToolName = "get_tekla_context";
    public const int Version = 2025;
    public const int HeavyMaxTimeoutSeconds = HostScriptContracts.TeklaHeavyMaxTimeoutSeconds;
    public const string BridgePluginName = "HPTekla MCP Bridge";

    public static readonly TeklaHostProfile Instance = new();

    public string HostId => PipeNaming.TeklaHost;
    public string DisplayName => "Tekla Structures";
    public string ServerName => "HPTekla MCP";
    public string ProductFolder => "HPTekla";
    public string EnvPrefix => "HPTEKLA_MCP_";
    public int DefaultVersion => Version;
    public IReadOnlyCollection<int> ValidVersions => new[] { 2025 };
    public string MethodPrefix => JsonRpcMethods.TeklaPrefix;

    string IHostProfile.ExecuteToolName => ExecuteToolName;
    string IHostProfile.ContextToolName => ContextToolName;
    public string ResourceScheme => PipeNaming.TeklaHost;

    public IReadOnlyCollection<string> Categories => new[]
    {
        "Model", "Geometry", "Property", "Rebar", "Drawing", "Export", "Generic"
    };

    public IReadOnlyCollection<string> CoreToolNames => new[]
    {
        ExecuteToolName, ContextToolName, "inspect_type", "cancel_execution"
    };

    public IReadOnlyCollection<string> ScriptImports => HostScriptContracts.TeklaImports;

    public string ScriptContractSummary =>
        "Globals: model (Tekla.Structures.Model.Model), selector (ModelObjectSelector), ct (CancellationToken), " +
        "log(string), progress(cur,total,msg), args (ScriptArgs). " +
        "Coordinates and lengths are in millimetres (mm). " +
        "Transaction control: transaction=auto (default for write operations) commits model changes via model.CommitChanges() upon successful completion; " +
        "dryRun=true runs the script logic in memory without calling CommitChanges(), ensuring zero persistence. " +
        "Heavy operations (e.g. IFC export, drawing numbering) require the 'Allow heavy operations' toggle in HPTekla MCP Bridge. " +
        "No MessageBox/Process/#r/#load. Scripts must end with return <value>;.";

    public Assembly HostAssembly => typeof(TeklaHostProfile).Assembly;
    public string CliExecutable => "HPTekla.Mcp.Server.exe";
    public int MaxTimeoutSeconds => HeavyMaxTimeoutSeconds;

    public string? BridgeNotConnectedHint =>
        $"Open Tekla Structures {Version} and ensure the HPTekla MCP Bridge plugin is loaded (Named Pipe: {PipeNaming.For(PipeNaming.TeklaHost, Version)}).";

    public string? TimeoutSemanticsHint =>
        "Tekla Structures script execution timed out; in dryRun=true mode no changes were committed, while in auto mode changes may have been discarded.";

    public string PipeName(int version) => PipeNaming.For(HostId, version);
    public string Method(string suffix) => JsonRpcMethods.For(MethodPrefix, suffix);
}
```

### 3.4. Resources and Prompts
- **`TeklaResourceProvider.cs`**:
  - `tekla://model/info`: Returns model summary JSON (`TeklaInfo`, file path, object counts).
  - `tekla://selection`: Returns currently selected objects in Tekla UI.
- **`TeklaPromptProvider.cs`**:
  - `tekla_query_template`: Prompt guidance for querying model entities and reinforcement.
  - `tekla_modify_template`: Prompt guidance for inserting steel/concrete framing and rebar groups with dryRun verification.
  - `toolify_run`: Automated prompt for turning successful scripts into reusable tools.

---

## 4. The 24 Tools Catalog Specification & Implementation

### 4.1. Core Tools (4 Tools)

#### 1. `execute_tekla_code`
- **Class**: `HPTekla.Mcp.Server.Hosts.Tekla.Tools.ExecuteTeklaCodeTool`
- **Description**: Executes C# script against Tekla Structures 2025.0 via Named Pipe.
- **Parameters**:
  - `code` (string, required): Script body (max 32 KB).
  - `transaction` (string, default `"auto"`): `"auto"` (commits on success), `"none"` (read-only), `"manual"`.
  - `dryRun` (bool, default `false`): Test execution in memory without committing changes.
  - `timeoutSeconds` (int, default `30`, 5–300s).
  - `label` (string?, optional): Identifier for audit logs.
  - `args` (JsonElement?, optional): Parameter values object.
- **Bridge-side Execution Logic**:
  ```csharp
  // Executed on Tekla Main Thread inside HPTekla.McpBridge
  var scriptArgs = new ScriptArgs(request.Args);
  var globals = new TeklaScriptGlobals(model, model.GetModelObjectSelector(), ct, logAction, progressAction, scriptArgs);
  var result = await compiler.ExecuteAsync(script, globals, ct);
  if (!request.DryRun && request.Transaction == TransactionModes.Auto && result.Success)
  {
      model.CommitChanges(); // Atomic commit
  }
  ```

#### 2. `get_tekla_context`
- **Class**: `HPTekla.Mcp.Server.Hosts.Tekla.Tools.GetTeklaContextTool`
- **Description**: Returns active Tekla session context, project info, model path, object counts, and selection.
- **Parameters**: `includeSelection` (bool, default `false`).

#### 3. `inspect_type`
- **Location**: Provided by `HPRebar.Mcp.Server.Core.Tools.InspectTypeTool`.
- **Description**: Reflects over Tekla Open API types (`Tekla.Structures.Model.*`, `Tekla.Structures.Geometry3d.*`, `Tekla.Structures.Catalogs.*`) to list properties, methods, and signatures.

#### 4. `cancel_execution`
- **Location**: Provided by `HPRebar.Mcp.Server.Core.Tools.CancelExecutionTool`.
- **Description**: Signals cooperative cancellation on the running script token.

---

### 4.2. Registry Meta Tools (8 Tools)

All 8 registry meta tools are provided directly by `HPRebar.Mcp.Server.Core` and configured via `IHostProfile`:

| MCP Tool Name | Engine Implementation | Role & Mapping |
|---|---|---|
| `search_tools` | `ToolRegistryQueryTools.Search` | Search published and draft tools in Tekla library |
| `get_tool` | `ToolRegistryQueryTools.Get` | Retrieve full schema, documentation, and code (`get_tool_schema`) |
| `run_tool` | `RunToolTool.Run` | Execute dynamic tool with parameter validation (`list_dynamic_tools` runner) |
| `get_run` | `RunHistoryTools.Get` | Inspect past execution run, literals, and AST analysis (`rollback_tool` aid) |
| `propose_tool` | `ToolLifecycleTools.Propose` | Package script into a new tool draft |
| `test_tool` | `ToolLifecycleTools.Test` | Test draft against example arguments with `dryRun` |
| `publish_tool` | `ToolLifecycleTools.Publish` | Publish tested tool to MCP tool surface |
| `manage_tool` | `ToolLifecycleTools.Manage` | Deprecate, quarantine, or restore a tool (`deprecate_tool`) |

---

### 4.3. Embedded Seed Tools (12 Tools)

All 12 seeds are embedded in `HPTekla.Mcp.Server/Registry/SeedLibrary/<Category>/<Name>/`.

```
SeedLibrary/
├── Model/
│   ├── get_model_info/           (Seed 1)
│   └── select_objects/           (Seed 2)
├── Property/
│   ├── get_part_properties/      (Seed 3)
│   └── modify_user_properties/   (Seed 9)
├── Geometry/
│   ├── create_beam/              (Seed 4)
│   ├── create_column/            (Seed 5)
│   └── create_contour_plate/     (Seed 6)
├── Rebar/
│   ├── create_rebar_group/       (Seed 7)
│   ├── create_single_rebar/      (Seed 8)
│   └── get_reinforcement_info/   (Seed 10)
├── Drawing/
│   └── list_drawings/            (Seed 11)
└── Export/
    └── export_ifc/               (Seed 12)
```

---

#### Seed 1: `Model/get_model_info`
- **Category**: `Model` | **Transaction**: `none` | **Destructive**: `false`
- **Description**: Returns overview of open Tekla model: name, path, project info, current phase, and connection status.
- **`tool.json` Input Schema**:
  ```json
  {
    "type": "object",
    "properties": {
      "includeProjectInfo": { "type": "boolean", "default": true, "description": "Include project info (project name, number, designer)." },
      "includePhaseInfo": { "type": "boolean", "default": true, "description": "Include current active phase details." }
    },
    "additionalProperties": false
  }
  ```
- **`code.cs` Implementation**:
  ```csharp
  bool includeProj = args.Bool("includeProjectInfo", true);
  bool includePhase = args.Bool("includePhaseInfo", true);

  var info = model.GetInfo();
  string modelName = info.ModelName ?? "";
  string modelPath = info.ModelPath ?? "";
  bool isConnected = model.GetConnectionStatus();

  object projData = null;
  if (includeProj)
  {
      var proj = model.GetProjectInfo();
      projData = new
      {
          projectName = proj.ProjectName,
          projectNumber = proj.ProjectNumber,
          designer = proj.Designer,
          builder = proj.Builder
      };
  }

  int currentPhase = 0;
  if (includePhase)
  {
      currentPhase = info.CurrentPhase;
  }

  log($"Tekla Model: {modelName} | Connected: {isConnected} | Phase: {currentPhase}");

  return new
  {
      success = true,
      isConnected = isConnected,
      modelName = modelName,
      modelPath = modelPath,
      currentPhase = currentPhase,
      project = projData,
      summary = $"Model '{modelName}' connected, current phase {currentPhase}."
  };
  ```

---

#### Seed 2: `Model/select_objects`
- **Category**: `Model` | **Transaction**: `none` | **Destructive**: `false`
- **Description**: Query or select objects in Tekla model by type or identifier.
- **`tool.json` Input Schema**:
  ```json
  {
    "type": "object",
    "properties": {
      "typeFilter": { "type": "string", "default": "ALL", "description": "Object type: ALL, BEAM, COLUMN, CONTOURPLATE, REBAR." },
      "limit": { "type": "integer", "default": 50, "description": "Maximum number of objects to return (1-200)." },
      "setSelection": { "type": "boolean", "default": false, "description": "Whether to highlight objects in Tekla active UI selection." }
    },
    "additionalProperties": false
  }
  ```
- **`code.cs` Implementation**:
  ```csharp
  string filter = args.Str("typeFilter", "ALL").ToUpperInvariant();
  int limit = Math.Clamp(args.Int("limit", 50), 1, 200);
  bool setSel = args.Bool("setSelection", false);

  ModelObject.ModelObjectEnum targetEnum = ModelObject.ModelObjectEnum.UNKNOWN;
  if (filter == "BEAM") targetEnum = ModelObject.ModelObjectEnum.BEAM;
  else if (filter == "COLUMN") targetEnum = ModelObject.ModelObjectEnum.BEAM; // Tekla columns are Beam objects
  else if (filter == "CONTOURPLATE") targetEnum = ModelObject.ModelObjectEnum.CONTOURPLATE;
  else if (filter == "REBAR") targetEnum = ModelObject.ModelObjectEnum.REBAR;

  ModelObjectEnumerator enumerator = targetEnum != ModelObject.ModelObjectEnum.UNKNOWN
      ? selector.GetAllObjectsWithType(targetEnum)
      : selector.GetAllObjects();

  var list = new List<object>();
  var selectedObjects = new System.Collections.ArrayList();

  while (enumerator.MoveNext() && list.Count < limit)
  {
      if (enumerator.Current is ModelObject mo)
      {
          string typeStr = mo.GetType().Name;
          string name = "";
          string profile = "";
          if (mo is Part part)
          {
              name = part.Name;
              profile = part.Profile.ProfileString;
          }

          list.Add(new
          {
              id = mo.Identifier.ID,
              guid = mo.Identifier.GUID.ToString(),
              type = typeStr,
              name = name,
              profile = profile
          });

          if (setSel) selectedObjects.Add(mo);
      }
  }

  if (setSel && selectedObjects.Count > 0)
  {
      var uiSelector = new Tekla.Structures.Model.UI.ModelObjectSelector();
      uiSelector.Select(selectedObjects);
  }

  return new
  {
      success = true,
      count = list.Count,
      filter = filter,
      objects = list
  };
  ```

---

#### Seed 3: `Property/get_part_properties`
- **Category**: `Property` | **Transaction**: `none` | **Destructive**: `false`
- **Description**: Returns detailed properties of a model part: profile, material, class, position, report values, and UDAs.
- **`tool.json` Input Schema**:
  ```json
  {
    "type": "object",
    "properties": {
      "objectId": { "type": "integer", "description": "Tekla identifier ID of the part." },
      "includeUserProperties": { "type": "boolean", "default": true, "description": "Include user-defined attributes (UDAs)." }
    },
    "required": ["objectId"],
    "additionalProperties": false
  }
  ```
- **`code.cs` Implementation**:
  ```csharp
  int id = args.Int("objectId");
  bool includeUda = args.Bool("includeUserProperties", true);

  var mo = model.SelectModelObject(new Identifier(id));
  if (mo == null) throw new ArgumentException($"Object ID {id} not found in model.");
  if (mo is not Part part) throw new ArgumentException($"Object ID {id} is not a Part ({mo.GetType().Name}).");

  double length = 0.0, weight = 0.0, volume = 0.0;
  part.GetReportProperty("LENGTH", ref length);
  part.GetReportProperty("WEIGHT", ref weight);
  part.GetReportProperty("VOLUME", ref volume);

  var udas = new Dictionary<string, object>();
  if (includeUda)
  {
      string comment = "", user1 = "";
      if (part.GetUserProperty("COMMENT", ref comment) && !string.IsNullOrWhiteSpace(comment)) udas["COMMENT"] = comment;
      if (part.GetUserProperty("USER_FIELD_1", ref user1) && !string.IsNullOrWhiteSpace(user1)) udas["USER_FIELD_1"] = user1;
  }

  return new
  {
      success = true,
      id = part.Identifier.ID,
      guid = part.Identifier.GUID.ToString(),
      type = part.GetType().Name,
      name = part.Name,
      profile = part.Profile.ProfileString,
      material = part.Material.MaterialString,
      partClass = part.Class,
      position = new
      {
          plane = part.Position.Plane.ToString(),
          depth = part.Position.Depth.ToString(),
          rotation = part.Position.Rotation.ToString()
      },
      measurements = new
      {
          lengthMm = length,
          weightKg = weight,
          volumeM3 = volume / 1e9
      },
      udas = udas
  };
  ```

---

#### Seed 4: `Geometry/create_beam`
- **Category**: `Geometry` | **Transaction**: `auto` | **Destructive**: `false`
- **Description**: Creates a new steel or concrete beam between start and end coordinates.
- **`tool.json` Input Schema**:
  ```json
  {
    "type": "object",
    "properties": {
      "startX": { "type": "number", "description": "Start X in mm." },
      "startY": { "type": "number", "description": "Start Y in mm." },
      "startZ": { "type": "number", "description": "Start Z in mm." },
      "endX": { "type": "number", "description": "End X in mm." },
      "endY": { "type": "number", "description": "End Y in mm." },
      "endZ": { "type": "number", "description": "End Z in mm." },
      "profile": { "type": "string", "default": "HEA300", "description": "Profile string (e.g. HEA300, 400*600)." },
      "material": { "type": "string", "default": "S235JR", "description": "Material string (e.g. S235JR, C30/37)." },
      "name": { "type": "string", "default": "BEAM", "description": "Part name." },
      "partClass": { "type": "string", "default": "1", "description": "Color class (1-14)." }
    },
    "required": ["startX", "startY", "startZ", "endX", "endY", "endZ"],
    "additionalProperties": false
  }
  ```
- **`code.cs` Implementation**:
  ```csharp
  double x1 = args.Double("startX"), y1 = args.Double("startY"), z1 = args.Double("startZ");
  double x2 = args.Double("endX"), y2 = args.Double("endY"), z2 = args.Double("endZ");
  string profile = args.Str("profile", "HEA300");
  string material = args.Str("material", "S235JR");
  string name = args.Str("name", "BEAM");
  string partClass = args.Str("partClass", "1");

  double dx = x2 - x1, dy = y2 - y1, dz = z2 - z1;
  double length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
  if (length < 1.0) throw new ArgumentException($"Start and end points are coincident (length {length:F2} mm < 1 mm).");

  var beam = new Beam
  {
      StartPoint = new Point(x1, y1, z1),
      EndPoint = new Point(x2, y2, z2),
      Name = name,
      Class = partClass
  };
  beam.Profile.ProfileString = profile;
  beam.Material.MaterialString = material;
  beam.Position.Plane = Position.PlaneEnum.MIDDLE;
  beam.Position.Depth = Position.DepthEnum.MIDDLE;

  bool ok = beam.Insert();
  if (!ok) throw new InvalidOperationException($"Failed to insert Beam '{name}' ({profile}) into Tekla model.");

  log($"Inserted Beam {beam.Identifier.ID} ({profile}, length: {length:F0} mm)");

  return new
  {
      success = true,
      id = beam.Identifier.ID,
      guid = beam.Identifier.GUID.ToString(),
      name = name,
      profile = profile,
      material = material,
      lengthMm = length
  };
  ```

---

#### Seed 5: `Geometry/create_column`
- **Category**: `Geometry` | **Transaction**: `auto` | **Destructive**: `false`
- **Description**: Creates a vertical structural column at coordinates (X, Y) from base elevation to top elevation.
- **`tool.json` Input Schema**:
  ```json
  {
    "type": "object",
    "properties": {
      "x": { "type": "number", "description": "Column insertion X coordinate in mm." },
      "y": { "type": "number", "description": "Column insertion Y coordinate in mm." },
      "baseZ": { "type": "number", "default": 0, "description": "Base elevation Z in mm." },
      "topZ": { "type": "number", "default": 3500, "description": "Top elevation Z in mm." },
      "profile": { "type": "string", "default": "HEB300", "description": "Profile string (e.g. HEB300, 500*500)." },
      "material": { "type": "string", "default": "S235JR", "description": "Material string." },
      "name": { "type": "string", "default": "COLUMN", "description": "Part name." },
      "partClass": { "type": "string", "default": "2", "description": "Color class." }
    },
    "required": ["x", "y"],
    "additionalProperties": false
  }
  ```
- **`code.cs` Implementation**:
  ```csharp
  double x = args.Double("x"), y = args.Double("y");
  double baseZ = args.Double("baseZ", 0);
  double topZ = args.Double("topZ", 3500);
  string profile = args.Str("profile", "HEB300");
  string material = args.Str("material", "S235JR");
  string name = args.Str("name", "COLUMN");
  string partClass = args.Str("partClass", "2");

  double height = topZ - baseZ;
  if (Math.Abs(height) < 1.0) throw new ArgumentException($"Column height cannot be 0 (baseZ={baseZ}, topZ={topZ}).");

  var col = new Beam
  {
      StartPoint = new Point(x, y, baseZ),
      EndPoint = new Point(x, y, topZ),
      Name = name,
      Class = partClass
  };
  col.Profile.ProfileString = profile;
  col.Material.MaterialString = material;
  col.Position.Plane = Position.PlaneEnum.MIDDLE;
  col.Position.Depth = Position.DepthEnum.MIDDLE;
  col.Position.Rotation = Position.RotationEnum.FRONT;

  bool ok = col.Insert();
  if (!ok) throw new InvalidOperationException($"Failed to insert Column '{name}' ({profile}) into Tekla model.");

  log($"Inserted Column {col.Identifier.ID} at ({x}, {y}), height: {height:F0} mm");

  return new
  {
      success = true,
      id = col.Identifier.ID,
      guid = col.Identifier.GUID.ToString(),
      name = name,
      profile = profile,
      material = material,
      heightMm = height
  };
  ```

---

#### Seed 6: `Geometry/create_contour_plate`
- **Category**: `Geometry` | **Transaction**: `auto` | **Destructive**: `false`
- **Description**: Creates a contour plate or concrete slab defined by a polygon of boundary points.
- **`tool.json` Input Schema**:
  ```json
  {
    "type": "object",
    "properties": {
      "points": {
        "type": "array",
        "description": "Polygon contour vertices [{x, y, z}]. Minimum 3 points.",
        "items": {
          "type": "object",
          "properties": {
            "x": { "type": "number" },
            "y": { "type": "number" },
            "z": { "type": "number" }
          },
          "required": ["x", "y", "z"]
        }
      },
      "thickness": { "type": "number", "default": 20, "description": "Plate thickness in mm." },
      "material": { "type": "string", "default": "S235JR", "description": "Material string." },
      "name": { "type": "string", "default": "PLATE", "description": "Part name." },
      "partClass": { "type": "string", "default": "3", "description": "Color class." }
    },
    "required": ["points"],
    "additionalProperties": false
  }
  ```
- **`code.cs` Implementation**:
  ```csharp
  var pts = args.Obj<List<Dictionary<string, double>>>("points");
  if (pts == null || pts.Count < 3) throw new ArgumentException("At least 3 points are required to define a contour plate.");
  double thickness = args.Double("thickness", 20);
  string material = args.Str("material", "S235JR");
  string name = args.Str("name", "PLATE");
  string partClass = args.Str("partClass", "3");

  var plate = new ContourPlate
  {
      Name = name,
      Class = partClass
  };
  plate.Profile.ProfileString = $"PL{thickness:F0}";
  plate.Material.MaterialString = material;

  foreach (var pt in pts)
  {
      plate.AddContourPoint(new ContourPoint(new Point(pt["x"], pt["y"], pt["z"]), null));
  }

  bool ok = plate.Insert();
  if (!ok) throw new InvalidOperationException("Failed to insert ContourPlate into Tekla model.");

  log($"Inserted ContourPlate {plate.Identifier.ID} (PL{thickness}, {pts.Count} vertices)");

  return new
  {
      success = true,
      id = plate.Identifier.ID,
      name = name,
      profile = plate.Profile.ProfileString,
      vertexCount = pts.Count
  };
  ```

---

#### Seed 7: `Rebar/create_rebar_group`
- **Category**: `Rebar` | **Transaction**: `auto` | **Destructive**: `false`
- **Description**: Creates a group of reinforcing bars (e.g. stirrups, distribution bars) hosted by a concrete member.
- **`tool.json` Input Schema**:
  ```json
  {
    "type": "object",
    "properties": {
      "fatherId": { "type": "integer", "description": "Identifier ID of the host concrete Part." },
      "shapePoints": {
        "type": "array",
        "description": "Polygon vertices [{x,y,z}] defining the bar cross-section curve.",
        "items": { "type": "object", "properties": { "x": {"type":"number"}, "y": {"type":"number"}, "z": {"type":"number"} }, "required": ["x","y","z"] }
      },
      "startX": { "type": "number", "description": "Distribution start X." },
      "startY": { "type": "number", "description": "Distribution start Y." },
      "startZ": { "type": "number", "description": "Distribution start Z." },
      "endX": { "type": "number", "description": "Distribution end X." },
      "endY": { "type": "number", "description": "Distribution end Y." },
      "endZ": { "type": "number", "description": "Distribution end Z." },
      "spacing": { "type": "number", "default": 150, "description": "Target spacing between bars in mm." },
      "size": { "type": "string", "default": "12", "description": "Bar diameter/size." },
      "grade": { "type": "string", "default": "B500B", "description": "Steel grade." },
      "name": { "type": "string", "default": "STIRRUP", "description": "Rebar group name." },
      "rebarClass": { "type": "integer", "default": 7, "description": "Color class." }
    },
    "required": ["fatherId", "shapePoints", "startX", "startY", "startZ", "endX", "endY", "endZ"],
    "additionalProperties": false
  }
  ```
- **`code.cs` Implementation**:
  ```csharp
  int fatherId = args.Int("fatherId");
  var shape = args.Obj<List<Dictionary<string, double>>>("shapePoints");
  if (shape == null || shape.Count < 2) throw new ArgumentException("Rebar shape requires at least 2 points.");

  double sx = args.Double("startX"), sy = args.Double("startY"), sz = args.Double("startZ");
  double ex = args.Double("endX"), ey = args.Double("endY"), ez = args.Double("endZ");
  double spacing = args.Double("spacing", 150);
  string size = args.Str("size", "12");
  string grade = args.Str("grade", "B500B");
  string name = args.Str("name", "STIRRUP");
  int rClass = args.Int("rebarClass", 7);

  var fatherPart = model.SelectModelObject(new Identifier(fatherId)) as Part;
  if (fatherPart == null) throw new ArgumentException($"Host Part {fatherId} not found in model.");

  var group = new RebarGroup
  {
      Father = fatherPart,
      Name = name,
      Class = rClass,
      Grade = grade,
      Size = size,
      StartPoint = new Point(sx, sy, sz),
      EndPoint = new Point(ex, ey, ez)
  };
  group.SpacingType = BaseRebarGroup.RebarGroupSpacingTypeEnum.SPACING_TYPE_TARGET_SPACE;
  group.Spacings.Add(spacing);

  var polygon = new Polygon();
  foreach (var pt in shape)
  {
      polygon.Points.Add(new Point(pt["x"], pt["y"], pt["z"]));
  }
  group.Polygons.Add(polygon);

  bool ok = group.Insert();
  if (!ok) throw new InvalidOperationException($"Failed to insert RebarGroup '{name}' for part {fatherId}.");

  log($"Inserted RebarGroup {group.Identifier.ID} (size: {size}, spacing: {spacing:F0} mm)");

  return new
  {
      success = true,
      id = group.Identifier.ID,
      fatherId = fatherId,
      name = name,
      size = size,
      grade = grade,
      spacing = spacing
  };
  ```

---

#### Seed 8: `Rebar/create_single_rebar`
- **Category**: `Rebar` | **Transaction**: `auto` | **Destructive**: `false`
- **Description**: Creates an individual reinforcing bar defined by a sequence of centerline coordinates.
- **`tool.json` Input Schema**:
  ```json
  {
    "type": "object",
    "properties": {
      "fatherId": { "type": "integer", "description": "Identifier ID of the host Part." },
      "points": {
        "type": "array",
        "description": "Centerline points [{x,y,z}] defining bar polyline.",
        "items": { "type": "object", "properties": { "x": {"type":"number"}, "y": {"type":"number"}, "z": {"type":"number"} }, "required": ["x","y","z"] }
      },
      "size": { "type": "string", "default": "20", "description": "Bar diameter." },
      "grade": { "type": "string", "default": "B500B", "description": "Steel grade." },
      "name": { "type": "string", "default": "MAIN_BAR", "description": "Bar name." },
      "rebarClass": { "type": "integer", "default": 6, "description": "Color class." }
    },
    "required": ["fatherId", "points"],
    "additionalProperties": false
  }
  ```
- **`code.cs` Implementation**:
  ```csharp
  int fatherId = args.Int("fatherId");
  var pts = args.Obj<List<Dictionary<string, double>>>("points");
  if (pts == null || pts.Count < 2) throw new ArgumentException("SingleRebar requires at least 2 centerline points.");

  string size = args.Str("size", "20");
  string grade = args.Str("grade", "B500B");
  string name = args.Str("name", "MAIN_BAR");
  int rClass = args.Int("rebarClass", 6);

  var fatherPart = model.SelectModelObject(new Identifier(fatherId)) as Part;
  if (fatherPart == null) throw new ArgumentException($"Host Part {fatherId} not found.");

  var bar = new SingleRebar
  {
      Father = fatherPart,
      Name = name,
      Class = rClass,
      Grade = grade,
      Size = size
  };

  var polygon = new Polygon();
  foreach (var pt in pts)
  {
      polygon.Points.Add(new Point(pt["x"], pt["y"], pt["z"]));
  }
  bar.Polygon = polygon;

  bool ok = bar.Insert();
  if (!ok) throw new InvalidOperationException($"Failed to insert SingleRebar '{name}' for part {fatherId}.");

  log($"Inserted SingleRebar {bar.Identifier.ID} (size: {size}, {pts.Count} points)");

  return new
  {
      success = true,
      id = bar.Identifier.ID,
      fatherId = fatherId,
      name = name,
      size = size,
      grade = grade,
      pointCount = pts.Count
  };
  ```

---

#### Seed 9: `Property/modify_user_properties`
- **Category**: `Property` | **Transaction**: `auto` | **Destructive**: `false`
- **Description**: Updates User-Defined Attributes (UDAs) for any model object.
- **`tool.json` Input Schema**:
  ```json
  {
    "type": "object",
    "properties": {
      "objectId": { "type": "integer", "description": "Identifier ID of the target object." },
      "properties": { "type": "object", "description": "Key-value dictionary of attributes to set." }
    },
    "required": ["objectId", "properties"],
    "additionalProperties": false
  }
  ```
- **`code.cs` Implementation**:
  ```csharp
  int id = args.Int("objectId");
  var props = args.Obj<Dictionary<string, object>>("properties");
  if (props == null || props.Count == 0) throw new ArgumentException("Properties dictionary cannot be empty.");

  var mo = model.SelectModelObject(new Identifier(id));
  if (mo == null) throw new ArgumentException($"ModelObject {id} not found.");

  int count = 0;
  foreach (var kvp in props)
  {
      string key = kvp.Key;
      object val = kvp.Value;
      if (val is string s)
      {
          mo.SetUserProperty(key, s);
          count++;
      }
      else if (val is int i)
      {
          mo.SetUserProperty(key, i);
          count++;
      }
      else if (val is double d)
      {
          mo.SetUserProperty(key, d);
          count++;
      }
      else if (val != null)
      {
          mo.SetUserProperty(key, val.ToString());
          count++;
      }
  }

  bool ok = mo.Modify();
  if (!ok) throw new InvalidOperationException($"Failed to modify user properties on object {id}.");

  log($"Modified {count} UDAs on object {id}");

  return new
  {
      success = true,
      objectId = id,
      modifiedCount = count
  };
  ```

---

#### Seed 10: `Rebar/get_reinforcement_info`
- **Category**: `Rebar` | **Transaction**: `none` | **Destructive**: `false`
- **Description**: Retrieves rebar schedules, bar marks, sizes, grades, lengths, and weights in model or for a host part.
- **`tool.json` Input Schema**:
  ```json
  {
    "type": "object",
    "properties": {
      "fatherId": { "type": "integer", "description": "Optional host part ID to filter rebars." },
      "limit": { "type": "integer", "default": 50, "description": "Maximum number of rebars to return." }
    },
    "additionalProperties": false
  }
  ```
- **`code.cs` Implementation**:
  ```csharp
  int fatherId = args.Int("fatherId", 0);
  int limit = Math.Clamp(args.Int("limit", 50), 1, 200);

  ModelObjectEnumerator enumerator = selector.GetAllObjectsWithType(ModelObject.ModelObjectEnum.REBAR);
  var list = new List<object>();

  while (enumerator.MoveNext() && list.Count < limit)
  {
      if (enumerator.Current is Reinforcement rebar)
      {
          if (fatherId > 0 && rebar.Father?.Identifier.ID != fatherId) continue;

          double length = 0.0, weight = 0.0;
          rebar.GetReportProperty("LENGTH", ref length);
          rebar.GetReportProperty("WEIGHT", ref weight);

          int quantity = 1;
          if (rebar is RebarGroup group)
          {
              quantity = group.Polygons.Count > 0 ? (int)Math.Max(1, group.GetNumberOfRebars()) : 1;
          }

          list.Add(new
          {
              id = rebar.Identifier.ID,
              fatherId = rebar.Father?.Identifier.ID,
              name = rebar.Name,
              size = rebar.Size,
              grade = rebar.Grade,
              quantity = quantity,
              lengthMm = length,
              weightKg = weight
          });
      }
  }

  return new
  {
      success = true,
      count = list.Count,
      rebars = list
  };
  ```

---

#### Seed 11: `Drawing/list_drawings`
- **Category**: `Drawing` | **Transaction**: `none` | **Destructive**: `false`
- **Description**: Enumerates and filters engineering drawings in Tekla model (GA, Assembly, Single Part, Cast Unit).
- **`tool.json` Input Schema**:
  ```json
  {
    "type": "object",
    "properties": {
      "drawingType": { "type": "string", "default": "ALL", "description": "Filter by drawing type: ALL, GA, ASSEMBLY, SINGLE_PART, CAST_UNIT." },
      "limit": { "type": "integer", "default": 50, "description": "Maximum number of drawings to return (1-200)." }
    },
    "additionalProperties": false
  }
  ```
- **`code.cs` Implementation**:
  ```csharp
  string filter = args.Str("drawingType", "ALL").ToUpperInvariant();
  int limit = Math.Clamp(args.Int("limit", 50), 1, 200);

  var dh = new Tekla.Structures.Drawing.DrawingHandler();
  var drawings = dh.GetDrawings();
  var list = new List<object>();

  while (drawings.MoveNext() && list.Count < limit)
  {
      var dwg = drawings.Current;
      string dwgTypeName = dwg.GetType().Name.Replace("Drawing", "").ToUpperInvariant();

      if (filter != "ALL" && !dwgTypeName.Contains(filter)) continue;

      list.Add(new
      {
          name = dwg.Name,
          title1 = dwg.Title1,
          title2 = dwg.Title2,
          type = dwgTypeName,
          mark = dwg.Mark,
          upToDate = dwg.UpToDateStatus.ToString()
      });
  }

  return new
  {
      success = true,
      count = list.Count,
      filter = filter,
      drawings = list
  };
  ```

---

#### Seed 12: `Export/export_ifc`
- **Category**: `Export` | **Transaction**: `auto` | **Destructive**: `true`
- **Tags**: `["export", "ifc", "destructive"]`
- **Description**: Exports the Tekla Structures model or selected objects to IFC format for BIM coordination.
- **`tool.json` Input Schema**:
  ```json
  {
    "type": "object",
    "properties": {
      "outputFilePath": { "type": "string", "description": "Absolute destination path for the .ifc file." },
      "format": { "type": "string", "default": "IFC4", "description": "IFC standard version: IFC4 or IFC2X3." },
      "exportSelectedOnly": { "type": "boolean", "default": false, "description": "Export only currently selected model objects." }
    },
    "required": ["outputFilePath"],
    "additionalProperties": false
  }
  ```
- **`code.cs` Implementation**:
  ```csharp
  string path = args.Str("outputFilePath");
  if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("outputFilePath is required.");
  string format = args.Str("format", "IFC4").ToUpperInvariant();
  bool selectedOnly = args.Bool("exportSelectedOnly", false);

  if (!path.EndsWith(".ifc", StringComparison.OrdinalIgnoreCase))
  {
      path += ".ifc";
  }

  // Ensure directory exists
  string dir = System.IO.Path.GetDirectoryName(path);
  if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
  {
      System.IO.Directory.CreateDirectory(dir);
  }

  log($"Exporting Tekla model to {path} (Format: {format}, SelectedOnly: {selectedOnly})...");

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

  if (!ok) throw new InvalidOperationException($"Tekla IFC export failed for output path: {path}");

  log($"IFC Export successfully created: {path}");

  return new
  {
      success = true,
      filePath = path,
      format = format,
      selectedOnly = selectedOnly,
      summary = $"Exported IFC file to {path}"
  };
  ```

---

## 5. Automated Test Strategy

### 5.1. `HPTekla.Mcp.Server.Tests` (.NET 10)

Targets `net10.0` with `Microsoft.Testing.Platform` and `xunit.v3`. Does not require Tekla Structures to be running:

1. **`TeklaHostProfileTests.cs`**:
   - Asserts profile invariants: `HostId == "tekla"`, `DefaultVersion == 2025`, `ValidVersions == [2025]`.
   - Asserts wire method prefix is `"tekla."` and pipe name is `"hptekla-mcp-2025"`.
   - Asserts exact tool surface on initialization: 4 core tools + 8 registry tools = 12 registered tools (seeds are published dynamically from the library).
   - Verifies zero cross-host naming leaks (no "revit", "autocad", "robot", etc.).
2. **`SeedCatalogTests.cs`**:
   - Discovers all 12 embedded seeds from assembly manifest resources.
   - Validates each seed's schema against `ToolValidator.Validate` under `TeklaHostProfile`.
   - Checks that all `args.X("key")` in `code.cs` match properties defined in `tool.json`.
   - Ensures scripts end with `return ...;` and do not violate AST guard rules.
3. **`SeedCompilationTests.cs`**:
   - Uses Roslyn `CSharpCompilation` with references to Tekla Open API assemblies (`Tekla.Structures.dll`, `Tekla.Structures.Model.dll`, `Tekla.Structures.Catalogs.dll`, `Tekla.Structures.Drawing.dll`) located via `C:\Program Files\Tekla Structures\2025.0\bin` or env var `HPTEKLA_DIR`.
   - Uses `Assert.SkipWhen(assembliesNotFound, "Tekla Structures 2025 not installed")` to ensure portable execution on non-CAD dev machines.
4. **`SeedExecutionTests.cs`**:
   - Links `FakeRevitExecutor.cs` to test full pipe JSON-RPC round-trips for all 12 seeds with sample input payloads.

### 5.2. `HPTekla.McpBridge.Tests` (.NET Framework 4.8)

Targets `net48` matching Tekla's internal CLR runtime:

1. **`TeklaRoslynCompilerTests.cs`**:
   - Verifies Roslyn compiler caching, syntax tree diagnostics, and script execution under .NET Framework 4.8.
2. **`TeklaGuardTests.cs`**:
   - Validates `GuardProfile.Tekla`: blocks `MessageBox`, `System.Windows.Forms`, `Process.Start`, `RunMacro`, and direct `model.CommitChanges()` calls inside script bodies.
3. **`TeklaTierAnalyzerTests.cs`**:
   - Asserts automatic classification:
     - `Read`: `get_model_info`, `get_part_properties`, `select_objects`, `get_reinforcement_info`, `list_drawings`.
     - `Write`: `create_beam`, `create_column`, `create_contour_plate`, `create_rebar_group`, `create_single_rebar`, `modify_user_properties`.
     - `Destructive`: `export_ifc`, object deletion.
4. **`TeklaDryRunSuppressionTests.cs`**:
   - Confirms that when `dryRun == true`, changes remain in volatile memory and no `model.CommitChanges()` is invoked.

---

## 6. Live Verification Harness (`HPTekla/tools/harness/`)

The live verification harness allows unattended and autonomous end-to-end verification against an active Tekla Structures 2025 session.

### 6.1. Shared Bookkeeping via `McpShared/tools/harness_common.py`
The harness imports `harness_common.py` by relative path:
```python
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "..", "McpShared", "tools"))
from harness_common import Checklist, Server, parse_tool_result, ok, short, utf8_console
```

### 6.2. `live-verify.py` Test Stages
The test script runs 6 verification stages:
1. **Stage A: Handshake & Stdio Pipe**:
   - Launches `HPTekla.Mcp.Server.exe`.
   - Sends `initialize` JSON-RPC handshake.
   - Asserts `tools/list` returns 24 tools.
2. **Stage B: Context & Resources**:
   - Invokes `get_tekla_context`.
   - Verifies active model name, path, and version 2025.
   - Reads `tekla://model/info` and `tekla://selection`.
3. **Stage C: Reflection & Metadata**:
   - Invokes `inspect_type` on `Tekla.Structures.Model.Beam` and `Tekla.Structures.Model.RebarGroup`.
   - Verifies member listings (`Profile`, `Material`, `Insert`, `Modify`).
4. **Stage D: Read-Only Seeds**:
   - Executes `get_model_info` and `select_objects`.
   - Executes `list_drawings`.
5. **Stage E: Dry-Run Write Verification**:
   - Calls `create_beam` and `create_column` with `dryRun = true`.
   - Confirms tool reports success in memory, but model state remains untouched.
6. **Stage F: Real Write & Rebar Placement**:
   - Calls `create_column` with `dryRun = false`.
   - Calls `create_rebar_group` hosted on the created column.
   - Queries `get_part_properties` and `get_reinforcement_info` to verify the physical insertion.

### 6.3. `run-live-verify.ps1`
PowerShell script orchestrating the run:
- Checks if `tekla.exe` is running.
- Checks if named pipe `\\.\pipe\hptekla-mcp-2025` is accessible.
- Builds `HPTekla.slnx`.
- Runs `python live-verify.py` and emits `summary.json`.

---

## 7. Implementation Recommendations & Next Steps

1. **Phase 0**: Implement Additive Integration in `McpShared`:
   - Add Tekla constants to `PipeNaming`, `JsonRpcMethods`, `HostScriptContracts`, `ContextResult`, `GuardProfile`, `AnalyzerProfile`, `HostProfile`.
   - Run `McpShared/HPRebar.Mcp.Server.Core.Tests` (164 tests) and `HPRebar.McpBridge.Core.Net48Tests` (62 tests) to ensure 100% pass rate with zero regressions.
2. **Phase 1**: Implement `HPTekla.Mcp.Server` (.NET 10) & 12 Embedded Seeds:
   - Create project structure and 12 embedded seed directories.
   - Build and verify `HPTekla.Mcp.Server.Tests`.
3. **Phase 2**: Implement `HPTekla.McpBridge` (.NET Framework 4.8) & Ribbon/WPF UI:
   - Build Tekla Open API bridge with pipe listener, thread dispatcher, and 3-tier safety gate.
   - Build and verify `HPTekla.McpBridge.Tests`.
4. **Phase 3**: Live Verification in Tekla Structures 2025:
   - Execute `run-live-verify.ps1` to achieve closed-loop verification.
