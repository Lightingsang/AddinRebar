# Handoff Report: Registry Meta Tools Integration & Strict 24-Tool Surface Enforcement

**Agent**: `teamwork_preview_explorer_m2_3`  
**Date**: 2026-10-08  
**Topic**: F6 - 8 Registry Meta Tools Integration & 24-Tool Surface Enforcement (Milestone M2 Task 2.3)  
**Governing Documents**: `ORIGINAL_REQUEST.md` (lines 285–365), `PROJECT.md` (Feature F6, lines 15–21, 54, 108–129), `TEST_READY.md`, `Tier1_ContractsAndServerTests.cs` (TC_T1_SRV_01..12).

---

## 1. Observation

1. **HostProfile.Dynamo Core Tool Definition (`HostProfile.cs:38-63`)**:
   `HostProfile.Dynamo` in `McpShared/HPRebar.Mcp.Server.Core/Hosts/HostProfile.cs` (lines 48–53) defines:
   ```csharp
   ExecuteToolName = "execute_dynamo_code",
   ContextToolName = "get_dynamo_context",
   ResourceScheme = "dynamo",
   Categories = new[] { "Nodes", "Wire", "Workspace", "Geometry", "Revit", "Python", "Package", "Generic" },
   CoreToolNames = new[] { "execute_dynamo_code", "execute_designscript", "get_dynamo_context", "cancel_execution" },
   ```
   Direct observation: `CoreToolNames` contains 4 tools: `execute_dynamo_code`, `execute_designscript`, `get_dynamo_context`, and `cancel_execution`. `inspect_type` is absent.

2. **Server Bootstrap Engine Tool Registration (`McpServerHost.cs:66-84`)**:
   In `McpShared/HPRebar.Mcp.Server.Core/Bootstrap/McpServerHost.cs`:
   ```csharp
   builder.Services
       .AddMcpServer(options =>
       {
           options.ServerInfo = new Implementation { Name = profile.ServerName, Version = version };
           options.ToolCollection ??= [];
           options.Capabilities ??= new ServerCapabilities();
           options.Capabilities.Tools ??= new ToolsCapability();
           options.Capabilities.Tools.ListChanged = true;
       })
       .WithStdioServerTransport()
       .WithToolsFromAssembly(engine)
       .WithToolsFromAssembly(profile.HostAssembly)
       ...
   ```
   Where `engine = typeof(McpServerHost).Assembly` (`HPRebar.Mcp.Server.Core.dll`).

3. **Classes Decorated with `[McpServerToolType]` in Engine (`McpShared/HPRebar.Mcp.Server.Core`)**:
   Grep search across `HPRebar.Mcp.Server.Core` located exactly 6 tool classes:
   - `Tools/CancelExecutionTool.cs:11` -> `cancel_execution` (1 tool)
   - `Tools/InspectTypeTool.cs:14` -> `inspect_type` (1 tool)
   - `Tools/Registry/ToolRegistryQueryTools.cs:14` -> `search_tools`, `get_tool` (2 tools)
   - `Tools/Registry/RunToolTool.cs:16` -> `run_tool` (1 tool)
   - `Tools/Registry/RunHistoryTools.cs:13` -> `get_run` (1 tool)
   - `Tools/Registry/ToolLifecycleTools.cs:15` -> `propose_tool`, `test_tool`, `publish_tool`, `manage_tool` (4 tools)
   Total tools discovered from `engine`: 10 tools (8 Registry Meta Tools + `cancel_execution` + `inspect_type`).

4. **Dynamic Tool Sync for Published Seeds (`DynamicToolRegistrar.cs:141-186`, `RegistryStartup.cs:45-53`)**:
   `RegistryStartup.StartAsync` invokes `SeedInstaller.Install(_store, _logger, _profile.HostAssembly)` to copy 12 embedded seeds from `Registry/SeedLibrary/**` to `%AppData%\HPDynamo\McpServer\tools-library\`. When loaded, `DynamicToolRegistrar.Sync()` calls `collection.TryAdd(tool)` on `_serverOptions.Value.ToolCollection`.

5. **Tool Surface Count Discrepancy without Suppression**:
   If unsuppressed, the server yields:
   - 3 host tools: `execute_dynamo_code`, `execute_designscript`, `get_dynamo_context`
   - 10 engine tools: `cancel_execution`, `inspect_type`, and 8 registry meta tools
   - 12 published seeds: `run_dyn_file`, `get_active_graph_info`, `search_nodes_library`, `inspect_node_type`, `add_node_to_workspace`, `connect_node_ports`, `disconnect_node_ports`, `set_node_properties`, `get_node_output_values`, `export_graph_to_image`, `validate_dyn_file`, `create_code_block_node`
   Total: 3 + 10 + 12 = **25 tools**.

6. **Tier 1 E2E Invariant Test Expectations (`Tier1_ContractsAndServerTests.cs:77-130`)**:
   - `TC_T1_SRV_01_ToolSurface_Count_IsExactly24Tools`: `Assert.Equal(24, tools.Count)`
   - `TC_T1_SRV_02_ToolSurface_InspectType_IsSuppressed`: `Assert.DoesNotContain("inspect_type", tools)`
   - `TC_T1_SRV_03..06`: Assert presence of the 4 Core Tools: `execute_dynamo_code`, `execute_designscript`, `get_dynamo_context`, `cancel_execution`.
   - `TC_T1_SRV_07`: Asserts presence of all 8 Registry Meta Tools: `search_tools`, `get_tool`, `run_tool`, `get_run`, `propose_tool`, `test_tool`, `publish_tool`, `manage_tool`.
   - `TC_T1_SRV_08`: Asserts presence of all 12 Published Seeds.

7. **Empirical Behavior of ModelContextProtocol SDK 2.2.0**:
   Direct reflection and execution in net10.0 demonstrated:
   - `WithToolsFromAssembly(engine)` adds `ServiceDescriptor` entries for each tool method to `builder.Services` with factory target `McpServerBuilderExtensions+<>c__DisplayClass4_2` pointing to `toolMethod` (`InspectAsync`).
   - `WithToolsFromAssembly(engine)` populates `McpServerOptions.ToolCollection` via options configuration.
   - When `InspectAsync` descriptor is removed from `builder.Services` and `inspect_type` is removed from `McpServerOptions.ToolCollection` via `builder.Services.PostConfigure<McpServerOptions>`, static tools in `host.Services` drop from 10 to 9, and `ToolCollection` drops from 10 to 9. Combined with 3 host tools and 12 seeds, the surface count equals **exactly 24**.

---

## 2. Logic Chain

1. **Basis of Core Tools**: `ORIGINAL_REQUEST.md` (R2, line 303) and `PROJECT.md` (F5, line 15) specify 4 Core Tools: `execute_dynamo_code`, `execute_designscript`, `get_dynamo_context`, and `cancel_execution`. Unlike AutoCAD, Navisworks, or Civil 3D which have 2 host tools + 2 engine core tools (`cancel_execution`, `inspect_type`), Dynamo adds `execute_designscript` as an additional host execution tool.
2. **Preserving 24-Tool Total Budget**: The architecture enforces a strict total surface of 24 tools:
   $$\text{Total Tools} = \text{Core Tools (4)} + \text{Registry Tools (8)} + \text{Published Seeds (12)} = 24$$
   If `inspect_type` is retained, the count is 25, failing `TC_T1_SRV_01` and `TC_T1_SRV_02`.
3. **Redundancy of `inspect_type` in Dynamo**: Dynamo agents operate on nodes, wires, and DesignScript geometry. Node signature inspection is handled by Seed 4 (`inspect_node_type`). Retaining `inspect_type` (which reflects arbitrary host types) causes confusion between `inspect_type` and `inspect_node_type`.
4. **Origin of `inspect_type`**: `McpServerHost.CreateBuilder` calls `.WithToolsFromAssembly(engine)`. Because `HPRebar.Mcp.Server.Core` holds `InspectTypeTool`, the SDK automatically registers it.
5. **Two-Tier Suppression Strategy**:
   - *Tier A (DI Service Descriptors)*: Tests in `*.Mcp.Server.Tests` (such as `HostProfileTests` across hosts) inspect `host.Services.GetServices<McpServerTool>()`. To satisfy both runtime tool calls and DI tests, the `ServiceDescriptor` for `InspectAsync` must be removed from `builder.Services` prior to `builder.Build()`.
   - *Tier B (Options ToolCollection)*: Runtime stdio handling for `tools/list` enumerates `McpServerOptions.ToolCollection`. Adding a `PostConfigure<McpServerOptions>` hook that removes `inspect_type` from `ToolCollection` ensures the wire protocol response omits `inspect_type`.
6. **Automatic Wiring of 8 Registry Tools**: Calling `.WithToolsFromAssembly(engine)` in `McpServerHost.CreateBuilder` naturally registers all 8 registry meta tools (`ToolRegistryQueryTools`, `RunToolTool`, `RunHistoryTools`, `ToolLifecycleTools`). No custom or duplicate classes are needed in `HPDynamo.Mcp.Server`.

---

## 3. Caveats

1. **Category Set Expansion**: `HostProfile.Dynamo` currently defines:
   `Categories = new[] { "Nodes", "Wire", "Workspace", "Geometry", "Revit", "Python", "Package", "Generic" }`.
   However, `PROJECT.md` (lines 116–128) places seeds in folders `Graph`, `Workspace`, `Library`, `Node`, `Wire`, `Data`, `Export`, `CodeBlock`.
   To avoid `ToolValidator` category rejection (`category must be one of...`) during seed registration and proposals, `DynamoHostProfile.Instance.Categories` in `HPDynamo.Mcp.Server` must provide the comprehensive union:
   `"Graph", "Workspace", "Library", "Node", "Nodes", "Wire", "Data", "Export", "CodeBlock", "Geometry", "Revit", "Python", "Package", "Generic"`.
2. **Seed Resource Embedding Invariant**: Seed files (`tool.json`, `code.cs`, `examples.json`) must be embedded in `HPDynamo.Mcp.Server.csproj` with logical name `SeedLibrary/%(RecursiveDir)%(Filename)%(Extension)`. If this logical name mapping is omitted, `SeedInstaller.LoadSeeds` returns an empty collection, resulting in only 12 tools (4 Core + 8 Registry) instead of 24.
3. **Preservation of `cancel_execution`**: When filtering `engine` tools, only `inspect_type` must be removed. `cancel_execution` is required as Core Tool 4.

---

## 4. Conclusion & Implementation Guidance

The exact implementation for Milestone M2 (F6 - 8 Registry Meta Tools & 24 Tools Surface) requires two primary files in `HPDynamo.Mcp.Server`:
1. `Hosts/DynamoHostProfile.cs` (declares profile, comprehensive categories, and `EnforceToolSurface` filter).
2. `Program.cs` (bootstraps builder, calls `EnforceToolSurface`, handles CLI branch).

### Line-by-Line Code Specifications

#### A. `HPDynamo/HPDynamo.Mcp.Server/Hosts/DynamoHostProfile.cs`
```csharp
namespace HPDynamo.Mcp.Server.Hosts;

using System.Reflection;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Hosts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

/// <summary>
///     Dynamo host profile configuration for HPDynamo.Mcp.Server.
///     Enforces 4 Core Tools, 8 Registry Meta Tools, and suppresses inspect_type
///     to maintain the strict 24-tool surface invariant.
/// </summary>
public static class DynamoHostProfile
{
    public const string ExecuteCodeToolName = "execute_dynamo_code";
    public const string ExecuteDesignScriptToolName = "execute_designscript";
    public const string ContextToolName = "get_dynamo_context";
    public const string CancelToolName = "cancel_execution";
    public const string SuppressedEngineToolName = "inspect_type";

    public const int DefaultVersion = 2026;
    public const int HeavyMaxTimeoutSeconds = HostScriptContracts.DynamoHeavyMaxTimeoutSeconds; // 600

    public static readonly HostProfile Instance = new HostProfile
    {
        HostId = PipeNaming.DynamoHost,
        DisplayName = "Dynamo",
        ServerName = "HPDynamo MCP",
        ProductFolder = "HPDynamo",
        EnvPrefix = "HPDYNAMO_MCP_",
        DefaultVersion = DefaultVersion,
        ValidVersions = new[] { DefaultVersion },
        MethodPrefix = JsonRpcMethods.DynamoPrefix,
        ExecuteToolName = ExecuteCodeToolName,
        ContextToolName = ContextToolName,
        ResourceScheme = PipeNaming.DynamoHost,
        Categories = new[]
        {
            "Graph", "Workspace", "Library", "Node", "Nodes", "Wire", "Data", "Export",
            "CodeBlock", "Geometry", "Revit", "Python", "Package", "Generic"
        },
        CoreToolNames = new[]
        {
            ExecuteCodeToolName,
            ExecuteDesignScriptToolName,
            ContextToolName,
            CancelToolName
        },
        ScriptImports = HostScriptContracts.DynamoImports,
        ScriptContractSummary =
            "Globals: dynamo (DynamoModel/DynamoViewModel), workspace (WorkspaceModel), doc (Document), ct, log(string), progress(cur,total,msg), args. " +
            "Dual-host execution: headless via RevitDynamoModel or live canvas inspection/authoring. " +
            "transaction: auto when the code modifies Revit elements or Dynamo workspace; none when reading; manual if managing its own Transaction.",
        MaxTimeoutSeconds = HeavyMaxTimeoutSeconds,
        HostAssembly = typeof(DynamoHostProfile).Assembly,
        CliExecutable = "HPDynamo.Mcp.Server.exe",
        BridgeNotConnectedHint = "Open Revit 2026 with Dynamo and ensure HPDynamo MCP Bridge is running (pipe hpdynamo-mcp-2026).",
        TimeoutSemanticsHint = "The script or graph execution may still be running in Dynamo/Revit; check Dynamo status before retrying.",
    };

    /// <summary>
    /// Enforces the strict 24-tool surface by removing and suppressing inspect_type
    /// from both builder.Services and McpServerOptions.ToolCollection.
    /// </summary>
    public static void EnforceToolSurface(HostApplicationBuilder builder)
    {
        // 1. Remove inspect_type descriptor from builder.Services
        for (int i = builder.Services.Count - 1; i >= 0; i--)
        {
            var d = builder.Services[i];
            if (d.ServiceType == typeof(McpServerTool) && d.ImplementationFactory?.Target != null)
            {
                var target = d.ImplementationFactory.Target;
                var field = target.GetType().GetField("toolMethod", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var method = field?.GetValue(target) as MethodInfo;
                if (method?.Name == "InspectAsync" || method?.DeclaringType?.Name == "InspectTypeTool")
                {
                    builder.Services.RemoveAt(i);
                }
            }
        }

        // 2. Remove inspect_type from McpServerOptions.ToolCollection via PostConfigure
        builder.Services.PostConfigure<McpServerOptions>(options =>
        {
            if (options.ToolCollection != null)
            {
                var toRemove = options.ToolCollection.FirstOrDefault(t =>
                    string.Equals(t.ProtocolTool.Name, SuppressedEngineToolName, StringComparison.OrdinalIgnoreCase));
                if (toRemove != null)
                {
                    options.ToolCollection.Remove(toRemove);
                }
            }
        });
    }
}
```

#### B. `HPDynamo/HPDynamo.Mcp.Server/Program.cs`
```csharp
using HPDynamo.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Registry;

var builder = McpServerHost.CreateBuilder(args, DynamoHostProfile.Instance);

// Enforce strict 24-tool surface: suppress inspect_type from ToolCollection & DI
DynamoHostProfile.EnforceToolSurface(builder);

if (args.Length > 0 && string.Equals(args[0], McpServerHost.RegistryCommand, StringComparison.OrdinalIgnoreCase))
{
    using var cliHost = builder.Build();
    return await RegistryCli.RunAsync(cliHost.Services, args[1..]).ConfigureAwait(false);
}

await builder.Build().RunAsync().ConfigureAwait(false);
return 0;
```

#### C. `HPDynamo/HPDynamo.Mcp.Server/HPDynamo.Mcp.Server.csproj` Seed ItemGroup
```xml
    <ItemGroup>
        <Compile Remove="Registry\SeedLibrary\**\*.cs" />
        <EmbeddedResource Include="Registry\SeedLibrary\**\*" LogicalName="SeedLibrary/%(RecursiveDir)%(Filename)%(Extension)" />
    </ItemGroup>
```

#### D. Breakdown of the 24 Tool Catalog

| Group | # | Tool Name | Source | Description / Role |
|---|:---:|---|---|---|
| **Core** | 1 | `execute_dynamo_code` | `HPDynamo.Mcp.Server` | Execute C# Roslyn script against Dynamo & Revit |
| | 2 | `execute_designscript` | `HPDynamo.Mcp.Server` | Execute native DesignScript block against workspace |
| | 3 | `get_dynamo_context` | `HPDynamo.Mcp.Server` | Query active workspace, nodes count, live canvas status |
| | 4 | `cancel_execution` | `HPRebar.Mcp.Server.Core` | Signal cancellation token to active bridge execution |
| **Registry** | 5 | `search_tools` | `HPRebar.Mcp.Server.Core` | Full-text query over stored tools in library |
| | 6 | `get_tool` | `HPRebar.Mcp.Server.Core` | Retrieve full tool record, C# code, examples, stats |
| | 7 | `run_tool` | `HPRebar.Mcp.Server.Core` | Execute stored tool by name with arguments |
| | 8 | `get_run` | `HPRebar.Mcp.Server.Core` | Retrieve execution history and AST literal analysis |
| | 9 | `propose_tool` | `HPRebar.Mcp.Server.Core` | Package script into reusable tool candidate |
| | 10 | `test_tool` | `HPRebar.Mcp.Server.Core` | Dry-run execute tool test examples |
| | 11 | `publish_tool` | `HPRebar.Mcp.Server.Core` | Request tool publication (moves to published) |
| | 12 | `manage_tool` | `HPRebar.Mcp.Server.Core` | Deprecate, quarantine, or restore library tools |
| **Seeds** | 13 | `run_dyn_file` | `SeedLibrary/Graph` | Execute `.dyn` file with parameter dictionary |
| | 14 | `get_active_graph_info` | `SeedLibrary/Workspace` | Query active canvas workspace metadata |
| | 15 | `search_nodes_library` | `SeedLibrary/Library` | Search loaded node definitions across packages |
| | 16 | `inspect_node_type` | `SeedLibrary/Library` | Inspect node input/output ports and type signatures |
| | 17 | `add_node_to_workspace` | `SeedLibrary/Node` | Place node on canvas at (X, Y) coordinates |
| | 18 | `connect_node_ports` | `SeedLibrary/Wire` | Wire output port of Node A to input of Node B |
| | 19 | `disconnect_node_ports` | `SeedLibrary/Wire` | Disconnect wire between specified ports |
| | 20 | `set_node_properties` | `SeedLibrary/Node` | Configure node attributes (title, value, range) |
| | 21 | `get_node_output_values` | `SeedLibrary/Data` | Retrieve evaluated output values from node |
| | 22 | `export_graph_to_image` | `SeedLibrary/Export` | Render workspace to PNG image artifact |
| | 23 | `validate_dyn_file` | `SeedLibrary/Graph` | Validate `.dyn` JSON structure and wiring cycles |
| | 24 | `create_code_block_node` | `SeedLibrary/CodeBlock` | Generate DesignScript CodeBlock node on canvas |

---

## 5. Verification Method

### A. Independent Verification Commands
```powershell
# 1. Verify Acceptance Test Suite (Tier 1 Contracts & Server Surface)
powershell -ExecutionPolicy Bypass -File .\run-e2e-dynamo-tests.ps1 -Tier Tier1

# 2. Run McpShared Core Regression (zero regression across all 793 tests)
dotnet test McpShared\HPRebar.Mcp.Server.Core.Tests

# 3. Compile HPDynamo.E2E.Tests with zero warnings and zero errors
dotnet build HPDynamo\HPDynamo.E2E.Tests\HPDynamo.E2E.Tests.csproj
```

### B. Unit Verification in `HPDynamo.Mcp.Server.Tests` (Milestone M5)
```csharp
[Fact]
public void Server_Exposes_Exactly_12_Static_Tools_And_Omits_InspectType()
{
    var builder = McpServerHost.CreateBuilder([], DynamoHostProfile.Instance);
    DynamoHostProfile.EnforceToolSurface(builder);
    using var host = builder.Build();

    var staticTools = host.Services.GetServices<McpServerTool>()
        .Select(t => t.ProtocolTool.Name)
        .OrderBy(n => n, StringComparer.Ordinal)
        .ToArray();

    Assert.Equal(12, staticTools.Length);
    Assert.Contains("execute_dynamo_code", staticTools);
    Assert.Contains("execute_designscript", staticTools);
    Assert.Contains("get_dynamo_context", staticTools);
    Assert.Contains("cancel_execution", staticTools);
    Assert.DoesNotContain("inspect_type", staticTools);

    var registryTools = DynamoE2EContracts.RegistryMetaTools;
    foreach (var reg in registryTools)
    {
        Assert.Contains(reg, staticTools);
    }
}
```

### C. Invalidation Conditions
- Any occurrence of `"inspect_type"` in `tools/list` or `host.Services.GetServices<McpServerTool>()`.
- Total tool count differing from 24 (e.g., 25 if `inspect_type` leaks, or 12 if seeds fail to load).
- Failure of any test in `TC_T1_SRV_01..12`.
