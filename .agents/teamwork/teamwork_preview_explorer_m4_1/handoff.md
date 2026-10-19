# Architecture Investigation Report: Embedded Seed Library for HPDynamo.Mcp.Server

**Agent**: teamwork_preview_explorer_m4_1  
**Milestone**: M4 (Embedded Seed Library)  
**Target Solution**: `HPDynamo/HPDynamo.slnx`  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\teamwork_preview_explorer_m4_1\`  
**Date**: 2026-10-08T10:55:00Z  

---

## 1. Observation

Direct observations from inspecting sibling servers (`HPEtabs`, `HPAutoCad`, `HPNavis`, `HPRebar`, `HPCivil3d`), the shared MCP engine (`McpShared`), and `HPDynamo`:

### 1.1 Sibling Seed Directory Structure & Embedding
- **Folder Structure**: Every sibling MCP server contains its seed tools under `<ServerProject>/Registry/SeedLibrary/<Category>/<ToolName>/`:
  - `HPEtabs/HPEtabs.Mcp.Server/Registry/SeedLibrary/` (e.g., `Model/get_model_info/`, `Analysis/run_analysis/`)
  - `HPNavis/HPNavis.Mcp.Server/Registry/SeedLibrary/` (e.g., `Clash/create_and_run_clash_test/`, `Model/get_model_info/`)
  - `HPRebar/HPRebar.Mcp.Server/Registry/SeedLibrary/`
  - `HPCivil3d/HPCivil3d.Mcp.Server/Registry/SeedLibrary/`
- **File Triad per Tool**: In all sibling implementations, every tool directory contains exactly 3 files:
  1. `tool.json`: Metadata, input schema, tags, execution mode, descriptions.
  2. `code.cs`: C# script source code executed at runtime.
  3. `examples.json`: Array of at least 2 representative usage examples with distinct arguments.
- **MSBuild Configuration in `.csproj`**:
  Observed in `HPEtabs/HPEtabs.Mcp.Server/HPEtabs.Mcp.Server.csproj` (lines 34-35) and `HPDynamo/HPDynamo.Mcp.Server/HPDynamo.Mcp.Server.csproj` (lines 33-34):
  ```xml
  <!-- Seed tools library embedded resources -->
  <Compile Remove="Registry\SeedLibrary\**\*.cs" />
  <EmbeddedResource Include="Registry\SeedLibrary\**\*" LogicalName="SeedLibrary/%(RecursiveDir)%(Filename)%(Extension)" />
  ```
  - `<Compile Remove>` ensures the .NET 10 console server build does not attempt to compile the script files as static classes against host assemblies.
  - `LogicalName="SeedLibrary/%(RecursiveDir)%(Filename)%(Extension)"` preserves forward-slash directory paths in the assembly's embedded manifest resource names.

### 1.2 Resource Discovery & Dynamic Registration in McpShared Engine
- **Resource Loading (`SeedInstaller.cs`, lines 17, 30-50)**:
  ```csharp
  public const string ResourcePrefix = "SeedLibrary/";
  public static IReadOnlyList<SeedContent> LoadSeeds(Assembly? assembly = null)
  {
      assembly ??= typeof(SeedInstaller).Assembly;
      var names = assembly.GetManifestResourceNames()
          .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal)).ToArray();
      ...
      return names
          .Select(n => n[ResourcePrefix.Length..].Replace('\\', '/').Split('/'))
          .Where(parts => parts.Length == 3)
          .GroupBy(parts => (parts[0], parts[1]))
          ...
  }
  ```
  - Splits embedded resource paths into 3 components: `parts[0]` = `Category`, `parts[1]` = `ToolName`, `parts[2]` = `FileName` (`tool.json`, `code.cs`, `examples.json`).
  - Ignores invalid formats; extracts and calculates SHA-256 checksums across `ToolJson + "\n" + Code + "\n" + ExamplesJson`.
- **Installation Lifecycle (`RegistryStartup.cs`, lines 45-53)**:
  ```csharp
  if (_options.InstallSeeds)
  {
      if (SeedInstaller.ListSeeds(_profile.HostAssembly).Count == 0)
          _logger.LogWarning("No seed tools embedded in {Assembly}; the library starts empty", _profile.HostAssembly.GetName().Name);
      SeedInstaller.Install(_store, _logger, _profile.HostAssembly);
  }
  _manager.Changed += () => _registrar.Sync();
  await _manager.LoadAllAsync(cancellationToken).ConfigureAwait(false);
  ```
  - `SeedInstaller.Install` unpackages the embedded resources into `_store.Root` on disk (default `%APPDATA%\HPDynamo\tools\SeedLibrary\...`).
  - `_manager.LoadAllAsync` reads all tools from disk.
- **Dynamic Tool Registration (`DynamicToolRegistrar.cs`, lines 145-174)**:
  ```csharp
  var desired = _manager.Tools.Where(t => t.IsPublished).ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
  ...
  var tool = McpServerTool.Create(new RegistryToolFunction(record, _manager, _formatter), new McpServerToolCreateOptions
  {
      Name = record.Name,
      Title = record.Title ?? record.Name,
      Destructive = record.Destructive,
      ReadOnly = string.Equals(record.Transaction, "none", StringComparison.OrdinalIgnoreCase),
      ...
  });
  collection.TryAdd(tool);
  ```
  - Published tools (`status == "published"`) are registered as `RegistryToolFunction` into `McpServerOptions.ToolCollection`.
  - For writing tools (`transaction != "none"`), `RegistryToolFunction.BuildSchema` automatically injects a `dryRun` boolean parameter.

### 1.3 24-Tool Surface Invariant in HPDynamo
- **Core Tools (4)** defined in `DynamoHostProfile.cs` (lines 45-51):
  - `execute_dynamo_code`
  - `execute_designscript`
  - `get_dynamo_context`
  - `cancel_execution`
- **Registry Meta Tools (8)** from `HPRebar.Mcp.Server.Core`:
  - `search_tools`
  - `get_tool`
  - `run_tool`
  - `get_run`
  - `propose_tool`
  - `test_tool`
  - `publish_tool`
  - `manage_tool`
- **Suppressed Engine Tool**: `InspectTypeTool.cs` (`inspect_type`) in `HPRebar.Mcp.Server.Core` is actively removed in `DynamoHostProfile.EnforceToolSurface(builder)` (lines 68-96) and `Program.cs` (lines 8-9):
  ```csharp
  DynamoHostProfile.EnforceToolSurface(builder);
  ```
- **Published Seeds (12)** specified in `PROJECT.md` (lines 116-128) and `ORIGINAL_REQUEST.md` (lines 321-335):
  1. `run_dyn_file` (`Graph`)
  2. `get_active_graph_info` (`Workspace`)
  3. `search_nodes_library` (`Library`)
  4. `inspect_node_type` (`Library`)
  5. `add_node_to_workspace` (`Node`)
  6. `connect_node_ports` (`Wire`)
  7. `disconnect_node_ports` (`Wire`)
  8. `set_node_properties` (`Node`)
  9. `get_node_output_values` (`Data`)
  10. `export_graph_to_image` (`Export`)
  11. `validate_dyn_file` (`Graph`)
  12. `create_code_block_node` (`CodeBlock`)
- **Total Tool Surface**: 4 Core + 8 Registry + 12 Published Seeds = **exactly 24 tools**.
  Verified in `HPDynamo.E2E.Tests/Tiers/Tier1_ContractsAndServerTests.cs` (line 81) and `Tier3_CrossFeatureInteractionTests.cs` (lines 68, 129, 134, 149):
  `Assert.Equal(24, tools.Count)`.

### 1.4 Clean Code Quality Enforcement
- **ScriptQuality AST Checks (`HPRebar.McpBridge.Core/Scripting/ScriptQuality.cs`)**:
  - `Q-B1` (Error): Commented-out code forbidden (`ScriptQualityComments.cs`).
  - `Q-B2` (Error): Empty catch blocks without explicit explanatory comment forbidden (`ScriptQualityCatches.cs`).
  - `Q-B3` (Error): Script line count cannot exceed 300 lines (`MaxScriptLines = 300`).
  - `Q-W1` (Warning): Blocks > 50 lines.
  - `Q-W2` (Warning): Nesting depth > 3 levels.
  - `Q-W3` (Warning): Vague variable/parameter names (`data`, `tmp`, `temp`, `obj`, `res`, `val`, `foo`, `bar`, `stuff`, `thing`).
  - `Q-W4` (Warning): Boolean parameter on local functions.
  - `Q-W5` (Warning): Swallowing `catch (Exception)`.
- **ScriptGuard Security Bounds (`ScriptGuard.cs`)**:
  - Forbidden namespaces: `System.IO`, `System.Net`, `System.Diagnostics.Process`, `System.Reflection`.
  - Forbids `#r` and `#load` compiler directives.
  - Disallows unapproved file access.

---

## 2. Logic Chain

1. **Embedding Architecture**:
   - Because `HPDynamo.Mcp.Server` compiles as a host-neutral .NET 10 console executable without referencing Revit or Dynamo binaries, script files in `Registry/SeedLibrary/` cannot be compiled directly by `csc`.
   - The `<Compile Remove>` directive excludes them from compilation, while `<EmbeddedResource>` packages them into the assembly manifest with forward-slash logical names `SeedLibrary/<Category>/<ToolName>/<file>`.
   - `SeedInstaller` in `McpShared` relies on this exact forward-slash naming convention to group and parse the triad (`tool.json`, `code.cs`, `examples.json`).

2. **Metadata & Script Synchronization**:
   - `tool.json` specifies tool identity (`name`), classification (`category`), host affinity (`host: "dynamo"`, `hostVersions: ["2026"]`), transactional behavior (`transaction: "none"|"auto"`), timeout limits, and JSON Schema (`inputSchema`).
   - `code.cs` accesses inputs via `args.*` methods (e.g. `args.String("key")`, `args.Int("key")`, `args.Bool("key")`).
   - The shared `ScriptAnalyzer` strictly validates **bidirectional argument parity**:
     - If `tool.json` defines a property in `inputSchema.properties`, `code.cs` must consume it.
     - If `code.cs` accesses an argument via `args.*`, it must be declared in `inputSchema.properties`.
     - Failure of either condition triggers test failures in `SeedLibraryStructureTests`.
   - Every script must conclude with a top-level `return` statement returning structured diagnostic and execution payloads.

3. **24-Tool Count Governance**:
   - The MCP protocol specification requires an accurate and deterministic tool surface advertised upon `tools/list`.
   - `McpServerHost.CreateBuilder` pulls in tools from both `engine` (`HPRebar.Mcp.Server.Core`) and `HostAssembly` (`HPDynamo.Mcp.Server`).
   - Without intervention, the engine includes `inspect_type` (which was intended for host reflection debugging).
   - In HPDynamo, `DynamoHostProfile.EnforceToolSurface` intercepts the DI container and `McpServerOptions.ToolCollection` to explicitly suppress `inspect_type`.
   - With 4 Core Tools, 8 Registry Meta Tools, and 12 Published Seeds, the total tool count equals exactly:
     $$4 + 8 + 12 = 24$$

4. **Quality & Security Rule Adherence**:
   - Rule C2 limits classes and scripts to $\le 300$ lines. In fact, sibling seeds average 40–80 lines.
   - Sibling unit test suites (`SeedLibraryStructureTests.cs`) enforce an even tighter limit of $\le 120$ lines for seed scripts to maintain extreme readability.
   - Scripts are validated against `ScriptGuard.Check(code, GuardProfile.Dynamo)` and `ScriptQuality.Find(code)`. Zero errors (`Q-B1`, `Q-B2`, `Q-B3`) are permitted.

---

## 3. Caveats

1. **Dynamo Execution Mode Availability**:
   - In-process execution can occur in two distinct modes depending on host state:
     - *Headless mode*: When Revit is running without the Dynamo UI open, executions flow through `RevitDynamoModel` / `DynamoRevitApp`.
     - *Canvas mode*: When Dynamo UI is open, executions hook into `DynamoViewModel` / `WorkspaceModel`.
   - The 12 seed scripts run through `execute_dynamo_code` (or respective bridge runners) and must gracefully handle whether `workspace` or `dynamo` is attached to a live canvas or headless model.
2. **File System Paths in Seeds**:
   - `run_dyn_file` and `validate_dyn_file` operate on `.dyn` files. Because `ScriptGuard` restricts arbitrary `System.IO` access in user scripts, these seeds must rely on safe parameter handling passed to bridge runner mechanisms rather than raw arbitrary file writing.
3. **No Unexplored Areas**: All four prompt objectives were verified directly against code and test fixtures across the repository.

---

## 4. Conclusion & Concrete Blueprint for M4

To successfully implement Milestone M4 in `HPDynamo.Mcp.Server`:

### 4.1 Required Directory Layout
Under `HPDynamo/HPDynamo.Mcp.Server/Registry/SeedLibrary/`, create 12 folders grouped by category:
```
Registry/SeedLibrary/
├── Graph/
│   ├── run_dyn_file/                  (tool.json, code.cs, examples.json)
│   └── validate_dyn_file/             (tool.json, code.cs, examples.json)
├── Workspace/
│   └── get_active_graph_info/         (tool.json, code.cs, examples.json)
├── Library/
│   ├── search_nodes_library/          (tool.json, code.cs, examples.json)
│   └── inspect_node_type/             (tool.json, code.cs, examples.json)
├── Node/
│   ├── add_node_to_workspace/         (tool.json, code.cs, examples.json)
│   └── set_node_properties/           (tool.json, code.cs, examples.json)
├── Wire/
│   ├── connect_node_ports/            (tool.json, code.cs, examples.json)
│   └── disconnect_node_ports/         (tool.json, code.cs, examples.json)
├── Data/
│   └── get_node_output_values/        (tool.json, code.cs, examples.json)
├── Export/
│   └── export_graph_to_image/         (tool.json, code.cs, examples.json)
└── CodeBlock/
    └── create_code_block_node/        (tool.json, code.cs, examples.json)
```

### 4.2 Seed Specification Table

| # | Seed Tool Name | Category | Transaction | Destructive | Key Parameters |
|---|----------------|----------|-------------|-------------|----------------|
| 1 | `run_dyn_file` | `Graph` | `auto` | `true` | `dynPath` (string), `parameters` (object) |
| 2 | `get_active_graph_info` | `Workspace` | `none` | `false` | `includeNodes` (bool), `includeErrors` (bool) |
| 3 | `search_nodes_library` | `Library` | `none` | `false` | `query` (string), `category` (string, optional), `limit` (int) |
| 4 | `inspect_node_type` | `Library` | `none` | `false` | `nodeType` (string) |
| 5 | `add_node_to_workspace` | `Node` | `auto` | `true` | `nodeType` (string), `x` (number), `y` (number), `customName` (string) |
| 6 | `connect_node_ports` | `Wire` | `auto` | `true` | `startNodeId` (string), `startPortIndex` (int), `endNodeId` (string), `endPortIndex` (int) |
| 7 | `disconnect_node_ports` | `Wire` | `auto` | `true` | `startNodeId` (string), `startPortIndex` (int), `endNodeId` (string), `endPortIndex` (int) |
| 8 | `set_node_properties` | `Node` | `auto` | `true` | `nodeId` (string), `properties` (object) |
| 9 | `get_node_output_values` | `Data` | `none` | `false` | `nodeIds` (array of strings) |
| 10 | `export_graph_to_image` | `Export` | `none` | `false` | `outputPath` (string, optional) |
| 11 | `validate_dyn_file` | `Graph` | `none` | `false` | `dynPath` (string) |
| 12 | `create_code_block_node` | `CodeBlock` | `auto` | `true` | `code` (string), `x` (number), `y` (number) |

### 4.3 Template Patterns for the Seed Triad

#### A. `tool.json` Blueprint
```json
{
  "name": "get_active_graph_info",
  "version": 1,
  "status": "published",
  "author": "hprebar",
  "category": "Workspace",
  "host": "dynamo",
  "hostVersions": [ "2026" ],
  "transaction": "none",
  "timeoutSeconds": 30,
  "destructive": false,
  "tags": [ "dynamo", "workspace", "graph", "query" ],
  "title": "Get active Dynamo graph info",
  "description": "Inspects the active Dynamo canvas workspace, including node count, connectors count, execution state, and evaluation error details. Read-only.",
  "notes": "Queries Dynamo.Graph.Workspaces.WorkspaceModel and HomeWorkspaceModel.",
  "inputSchema": {
    "type": "object",
    "properties": {
      "includeNodes": {
        "type": "boolean",
        "default": false,
        "description": "Whether to list individual node identifiers and states"
      },
      "includeErrors": {
        "type": "boolean",
        "default": true,
        "description": "Whether to include error details for failing nodes"
      }
    },
    "additionalProperties": false
  }
}
```

#### B. `code.cs` Blueprint
```csharp
bool includeNodes = args.Bool("includeNodes", false);
bool includeErrors = args.Bool("includeErrors", true);

if (workspace == null)
{
    return new
    {
        success = false,
        message = "No active Dynamo workspace is currently open.",
        nodesCount = 0,
        connectorsCount = 0
    };
}

var nodes = workspace.Nodes.ToList();
var connectors = workspace.Connectors.ToList();
var errorNodes = nodes.Where(n => n.State == ElementState.Error).ToList();

log($"Inspected workspace '{workspace.Name}': {nodes.Count} nodes, {connectors.Count} wires");

return new
{
    success = true,
    workspaceName = workspace.Name,
    fileName = workspace.FileName,
    nodesCount = nodes.Count,
    connectorsCount = connectors.Count,
    hasUnsavedChanges = workspace.HasUnsavedChanges,
    errorNodesCount = errorNodes.Count,
    errorDetails = includeErrors ? errorNodes.Select(n => new { id = n.GUID.ToString(), name = n.Name, error = n.ToolTipText }).ToList() : null,
    nodes = includeNodes ? nodes.Select(n => new { id = n.GUID.ToString(), name = n.Name, state = n.State.ToString() }).ToList() : null
};
```

#### C. `examples.json` Blueprint
```json
[
  {
    "title": "Summary overview",
    "args": {
      "includeNodes": false,
      "includeErrors": true
    }
  },
  {
    "title": "Detailed node listing",
    "args": {
      "includeNodes": true,
      "includeErrors": true
    }
  }
]
```

---

## 5. Verification Method

Once Milestone M4 creates the 12 seeds, the implementation can be independently verified using the following steps:

1. **Build Verification**:
   ```powershell
   dotnet build "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server\HPDynamo.Mcp.Server.csproj" -c Debug
   ```
   *Expected Result*: Compiles cleanly with 0 errors.

2. **Resource Embedding & Extraction Check**:
   Create and execute `SeedLibraryStructureTests` in `HPDynamo.Mcp.Server.Tests`:
   ```powershell
   dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.Mcp.Server.Tests\HPDynamo.Mcp.Server.Tests.csproj"
   ```
   *Assertions to include*:
   - `Assert.Equal(12, seeds.Count);`
   - `Assert.All(seeds, s => Assert.True(ScriptQuality.Find(s.Code).Count == 0));`
   - `Assert.All(seeds, s => Assert.True(s.Code.Split('\n').Length <= 120));`
   - `Assert.All(seeds, s => Assert.True(ScriptGuard.Check(s.Code, GuardProfile.Dynamo).Count == 0));`

3. **24-Tool Surface Validation via E2E Suite**:
   ```powershell
   dotnet test "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPDynamo\HPDynamo.E2E.Tests\HPDynamo.E2E.Tests.csproj" --filter "FullyQualifiedName~TC_T1_SRV_01"
   ```
   *Expected Result*: `TC_T1_SRV_01_ToolSurface_Count_IsExactly24Tools` passes with 100% green status.
