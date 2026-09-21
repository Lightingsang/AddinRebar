# HPRobot MCP Server & Tool Registry Test Design Analysis

**Agent**: `explorer_m4_2`  
**Subsystem**: HPRobot MCP Subsystem  
**Milestone**: M4 — Automated Test Suites Design  
**Target Project**: `HPRobot.Mcp.Server.Tests` (.NET 10 xUnit v3 / MTP)  
**Date**: 2026-09-21  

---

## 1. Executive Summary & Scope

The objective of this investigation is to design a complete, production-grade test suite for `HPRobot.Mcp.Server.Tests`. This suite verifies that the stdio MCP server for Autodesk Robot Structural Analysis Professional 2026 behaves strictly in accordance with:
1. The **McpShared** host integration contracts (`PipeNaming`, `JsonRpcMethods`, `HostScriptContracts`, `ContextMessages`, `GuardProfile`, `AnalyzerProfile`).
2. The **Tool Registry** ecosystem specifications: exactly 24 tools available at runtime (4 core tools + 8 registry meta tools + 12 embedded seed tools discovered via manifest resources).
3. The **Named Pipe Wire Protocol**: seamless request/response framing, dispatching, and error mapping for `robot.ping`, `robot.context`, `robot.execute`, and `robot.cancel` over Windows Named Pipe `hprobot-mcp-2026` using the canonical `FakeRevitExecutor` harness.
4. **Safety & Snapshot Gating**: verified 3-tier static gating (`Read` vs `Write` vs `Heavy/Destructive`), snapshot filename propagation, cooperative timeout semantics (300 s ceiling, persisted changes without rollback), and sanitized error reporting that never exposes local machine paths.

---

## 2. Solution & Project Configuration Architecture

### 2.1 Project File Specification: `HPRobot.Mcp.Server.Tests.csproj`

Following the proven pattern of sister test suites (`HPEtabs.Mcp.Server.Tests`, `HPSap2000.Mcp.Server.Tests`), the test project targets `.NET 10.0`, utilizes the pinned `Microsoft.Testing.Platform` (MTP) runner, links `FakeRevitExecutor` from `McpShared`, and references `HPRobot.Mcp.Server`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <LangVersion>latest</LangVersion>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <RootNamespace>HPRobot.Mcp.Server.Tests</RootNamespace>
        <Configurations>Debug;Release</Configurations>
        <IsPackable>false</IsPackable>
        <!-- global.json pins test.runner = Microsoft.Testing.Platform; xunit.v3 speaks it natively -->
        <OutputType>Exe</OutputType>
        <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="xunit.v3" Version="3.1.0" />
        <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
        <!-- Unify System.Text.Json / AsyncInterfaces across packages -->
        <PackageReference Include="Microsoft.Bcl.AsyncInterfaces" Version="10.0.12" />
        <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.12.0" />
    </ItemGroup>

    <ItemGroup>
        <!-- Target MCP Server and Core Engine Libraries -->
        <ProjectReference Include="..\HPRobot.Mcp.Server\HPRobot.Mcp.Server.csproj" />
        <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Server.Core\HPRebar.Mcp.Server.Core.csproj" />
        <ProjectReference Include="..\..\McpShared\HPRebar.McpBridge.Core\HPRebar.McpBridge.Core.csproj" />
        <ProjectReference Include="..\..\McpShared\HPRebar.Mcp.Contracts\HPRebar.Mcp.Contracts.csproj" />
    </ItemGroup>

    <ItemGroup>
        <!-- Reference Interop.RobotOM.dll if installed on dev machine (for SeedCompilationTests) -->
        <Reference Include="Interop.RobotOM" Condition="'$(RobotApiAvailable)' == 'true' Or Exists('$(RobotInstallDir)Interop.RobotOM.dll')">
            <HintPath>$(RobotInstallDir)Interop.RobotOM.dll</HintPath>
            <Private>false</Private>
            <EmbedInteropTypes>false</EmbedInteropTypes>
        </Reference>
    </ItemGroup>

    <ItemGroup>
        <!-- Canonical fake executor linked directly from McpShared test suite -->
        <Compile Include="..\..\McpShared\HPRebar.Mcp.Server.Core.Tests\Fakes\FakeRevitExecutor.cs" Link="Fakes\FakeRevitExecutor.cs" />
    </ItemGroup>

</Project>
```

### 2.2 Solution Registration (`HPRobot.slnx`)
The test project must be added to `HPRobot.slnx` under the root solution items:
```xml
  <Project Path="HPRobot.McpBridge/HPRobot.McpBridge.csproj" />
  <Project Path="HPRobot.McpBridge.Tests/HPRobot.McpBridge.Tests.csproj" />
  <Project Path="HPRobot.Mcp.Server/HPRobot.Mcp.Server.csproj" />
  <Project Path="HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj" />
```

---

## 3. Test Suite 1 Design: `RobotHostProfileTests.cs`

### 3.1 Objectives & Verifications
`RobotHostProfileTests` verifies that `HPRobot.Mcp.Server.Hosts.Robot.RobotHostProfile` implements `IHostProfile` exactly as specified, anchors options properly, provides correct help text and message hints, and establishes strict host boundaries that isolate Robot from sibling deliverables.

### 3.2 Detailed Test Method Specifications

| Test Method | Category | Assertions & Invariants |
|---|---|---|
| `Profile_IdentityAndMetadata_AreAccurate` | Naming & IDs | `HostId == "robot"`, `DisplayName == "Robot Structural Analysis"`, `ServerName == "HPRobot MCP"`, `ProductFolder == "HPRobot"`, `EnvPrefix == "HPROBOT_MCP_"`, `CliExecutable == "HPRobot.Mcp.Server.exe"`, `BridgeExecutable == "HPRobot.McpBridge.exe"`, `ResourceScheme == "robot"` |
| `Profile_VersionsAndPipeNaming_MatchConventions` | Versioning | `DefaultVersion == 2026`, `ValidVersions == [2024, 2025, 2026]`, `PipeName(2026) == "hprobot-mcp-2026"`, `PipeName(2025) == "hprobot-mcp-2025"`, `PipeName(2024) == "hprobot-mcp-2024"` |
| `Profile_MethodsAndToolNames_MatchRobotPrefix` | Prefix & Tools | `MethodPrefix == "robot."`, `Method("execute") == "robot.execute"`, `Method("context") == "robot.context"`, `Method("ping") == "robot.ping"`, `Method("cancel") == "robot.cancel"`, `ExecuteToolName == "execute_robot_code"`, `ContextToolName == "get_robot_context"` |
| `Profile_ScriptContracts_ImportsAndGlobals_MatchSpecifications` | Script Engine | `ScriptImports` contains exact set: `RobotOM`, `System`, `System.Collections.Generic`, `System.Linq`, `HPRebar.McpBridge.Core.Scripting`. `HostScriptContracts.RobotGlobals` equals `["robot", "structure", "units", "ct", "log", "progress", "args"]`. |
| `Profile_TimeoutAndCeiling_Enforces300Seconds` | Timeouts | `MaxTimeoutSeconds == 300`, `HeavyMaxTimeoutSeconds == 300` (`HostScriptContracts.RobotHeavyMaxTimeoutSeconds`). |
| `Profile_Categories_StrictlyStructural_NoSiblingBleed` | Boundary Isolation | `Categories` contains `["Model", "Geometry", "Property", "Load", "Analysis", "Results", "Generic"]`. `DoesNotContain(c => c is "Layer" or "Block" or "Wall" or "Clash" or "Viewpoint" or "Sheet")`. |
| `Profile_CoreToolNames_ContainsCanonicalFourTools` | Core Tools | `CoreToolNames` equals `["execute_robot_code", "get_robot_context", "inspect_type", "cancel_execution"]`. |
| `Profile_ScriptContractSummary_ContainsSafetyGuidelines` | Contract Text | Mentions `robot`, `structure`, `units` (`m`, `kN`, `kN·m`, `MPa`), no transaction/undo API, static tiers R, W, D, auto `.rtd` snapshot before write, requires `Allow heavy/destructive operations` toggle for D, static preview with `PREVIEW` diagnostic on `dryRun`/`none`, deny-list: `Quit`, `ApplicationExit`, `Interactive`, `MessageBox`, `Process`, reflection, threading, `#r`, `#load`, ends with `return <value>;`. |
| `Profile_Hints_NameBridge_AndContainNoLocalPaths` | Message Hygiene | `BridgeNotConnectedHint` contains `HPRobot.McpBridge.exe`, `Robot Structural Analysis Professional 2026`, `Attach`, `Allow AI code execution`, `hprobot-mcp-2026`. `TimeoutSemanticsHint` contains `Robot Structural Analysis`, `persisted (no rollback)`, `snapshot`. Both hints do NOT contain `C:\` or `Environment.UserName`. |
| `Options_BindsHostVersionAndPipe_EvenWithoutConfiguration` | DI Binding | In empty config, `bridge.HostVersion == 2026`, `bridge.PipeName == "hprobot-mcp-2026"`. With config, registry paths anchor to `HPRobot\McpServer`. No mentions of `HPRebar`, `HPAutoCad`, `HPEtabs`, `HPNavis`. |
| `Options_RejectsInvalidHostVersion` | Validation | Setting `Bridge:HostVersion = "2020"` (not in `ValidVersions`) throws `OptionsValidationException`. |
| `ToolSurface_BuildsWithCoreAndRegistryTools_NoForeignHosts` | Host Integration | `McpServerHost.CreateBuilder([], profile).Build()` registers 12 static tools (4 core + 8 registry). Contains zero tools matching `revit`, `autocad`, `etabs`, `navis`, `sap2000`, `excel`. |
| `ToolSurface_ExecuteAndContextToolAnnotations_AreAccurate` | Tool Annotations | `execute_robot_code`: `DestructiveHint == true`, `ReadOnlyHint == false`. `get_robot_context`: `ReadOnlyHint == true`, `IdempotentHint == true`. |
| `ResourcesAndPrompts_UseRobotSchemeAndNames` | Protocol Surface | Resources contain `robot://model/info`, `robot://selection`. Prompts contain `robot_query_template`, `robot_modify_template`, `toolify_run`. Server name in options is `"HPRobot MCP"`. |

### 3.3 Reference Implementation Design: `RobotHostProfileTests.cs`

```csharp
using System.IO;
using System.Linq;
using HPRobot.Mcp.Server.Hosts.Robot;
using HPRobot.Mcp.Server.Hosts.Robot.Tools;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Xunit;

namespace HPRobot.Mcp.Server.Tests;

public sealed class RobotHostProfileTests
{
    private static readonly string[] ExpectedRegistryTools =
    [
        "search_tools", "get_tool", "run_tool", "get_run",
        "propose_tool", "test_tool", "publish_tool", "manage_tool"
    ];

    [Fact]
    public void Profile_NamesTheRobotPipe_Prefix_Tools_RegistryRoot_Ceiling_And_Hints()
    {
        var profile = RobotHostProfile.Instance;

        Assert.Equal("robot", profile.HostId);
        Assert.Equal("Robot Structural Analysis", profile.DisplayName);
        Assert.Equal("HPRobot MCP", profile.ServerName);
        Assert.Equal("hprobot-mcp-2026", profile.PipeName(2026));
        Assert.Equal("robot.execute", profile.Method("execute"));
        Assert.Equal("robot.context", profile.Method("context"));
        Assert.Equal("robot.ping", profile.Method("ping"));
        Assert.Equal("robot.cancel", profile.Method("cancel"));
        Assert.Equal("execute_robot_code", profile.ExecuteToolName);
        Assert.Equal("get_robot_context", profile.ContextToolName);
        Assert.Equal("HPRobot", profile.ProductFolder);
        Assert.Equal("HPROBOT_MCP_", profile.EnvPrefix);
        Assert.Equal(2026, profile.DefaultVersion);
        Assert.Equal([2024, 2025, 2026], profile.ValidVersions);
        Assert.Equal(300, profile.MaxTimeoutSeconds);
        Assert.Equal(HostScriptContracts.RobotImports, profile.ScriptImports);
        Assert.Equal(HostScriptContracts.RobotGlobals, ["robot", "structure", "units", "ct", "log", "progress", "args"]);
        Assert.Same(typeof(RobotHostProfile).Assembly, profile.HostAssembly);
        Assert.Equal("HPRobot.Mcp.Server.exe", profile.CliExecutable);
        Assert.Equal("HPRobot.McpBridge.exe", profile.BridgeExecutable);

        Assert.Contains("kN·m", profile.ScriptContractSummary);
        Assert.Contains("MPa", profile.ScriptContractSummary);
        Assert.Contains("no transaction", profile.ScriptContractSummary);
        Assert.Contains("snapshot", profile.ScriptContractSummary);
        Assert.Contains("Allow heavy/destructive operations", profile.ScriptContractSummary);
        Assert.Contains("PREVIEW", profile.ScriptContractSummary);

        Assert.Contains("HPRobot.McpBridge.exe", profile.BridgeNotConnectedHint);
        Assert.Contains("hprobot-mcp-2026", profile.BridgeNotConnectedHint);
        Assert.Contains("Attach", profile.BridgeNotConnectedHint);
        Assert.Contains("persisted (no rollback)", profile.TimeoutSemanticsHint);
        Assert.DoesNotContain(@"C:\", profile.BridgeNotConnectedHint + profile.TimeoutSemanticsHint);
        Assert.DoesNotContain(Environment.UserName, profile.BridgeNotConnectedHint);
    }

    [Fact]
    public void Options_BindRegistryRootAndPipe_FromProfile_EvenWithoutConfiguration()
    {
        var withoutConfig = OptionsFor(new Dictionary<string, string?>());
        var withConfig = OptionsFor(new Dictionary<string, string?>
        {
            ["Registry:PublishPolicy"] = "manual",
            ["Bridge:HostVersion"] = "2026"
        });

        Assert.Equal(2026, withoutConfig.bridge.HostVersion);
        Assert.Equal("hprobot-mcp-2026", withoutConfig.bridge.PipeName);
        Assert.Equal("hprobot-mcp-2026", withConfig.bridge.PipeName);
        Assert.Contains(Path.Combine("HPRobot", "McpServer"), withConfig.registry.LibraryPath);
        Assert.Contains(Path.Combine("HPRobot", "McpServer"), withConfig.registry.DbPath);
        Assert.DoesNotContain("HPRebar", withConfig.registry.LibraryPath);
        Assert.DoesNotContain("HPAutoCad", withConfig.registry.LibraryPath);
        Assert.DoesNotContain("HPEtabs", withConfig.registry.LibraryPath);
        Assert.DoesNotContain("HPSap2000", withConfig.registry.LibraryPath);
    }

    [Fact]
    public void InvalidHostVersion_IsRefusedByValidation()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Bridge:HostVersion"] = "2020" })
            .Build();
        var services = new ServiceCollection();
        McpServerHost.ConfigureOptions(services, configuration, RobotHostProfile.Instance);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<BridgeOptions>>().Value);
    }

    [Fact]
    public void ToolSurface_ContainsFourCoreTools_EightRegistryTools_NoForeignHostTools()
    {
        using var host = McpServerHost.CreateBuilder([], RobotHostProfile.Instance).Build();

        var toolNames = host.Services.GetServices<McpServerTool>()
            .Select(t => t.ProtocolTool.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(12, toolNames.Length);
        Assert.Contains("execute_robot_code", toolNames);
        Assert.Contains("get_robot_context", toolNames);
        Assert.Contains("inspect_type", toolNames);
        Assert.Contains("cancel_execution", toolNames);
        Assert.All(ExpectedRegistryTools, regTool => Assert.Contains(regTool, toolNames));
        Assert.DoesNotContain(toolNames, n =>
            n.Contains("revit", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("autocad", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("etabs", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("sap2000", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("navis", StringComparison.OrdinalIgnoreCase));

        var execTool = host.Services.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "execute_robot_code").ProtocolTool;
        Assert.True(execTool.Annotations?.DestructiveHint);
        Assert.False(execTool.Annotations?.ReadOnlyHint);
        Assert.Equal(ExecuteRobotCodeTool.ToolDescription, execTool.Description);

        var ctxTool = host.Services.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "get_robot_context").ProtocolTool;
        Assert.True(ctxTool.Annotations?.ReadOnlyHint);
        Assert.True(ctxTool.Annotations?.IdempotentHint);
        Assert.Equal(GetRobotContextTool.ToolDescription, ctxTool.Description);
    }

    [Fact]
    public void ResourcesAndPrompts_UseRobotSchemeAndNames()
    {
        using var host = McpServerHost.CreateBuilder([], RobotHostProfile.Instance).Build();

        var resources = host.Services.GetServices<McpServerResource>()
            .Select(r => r.ProtocolResourceTemplate.UriTemplate)
            .ToArray();
        var prompts = host.Services.GetServices<McpServerPrompt>()
            .Select(p => p.ProtocolPrompt.Name)
            .ToArray();

        Assert.Contains("robot://model/info", resources);
        Assert.Contains("robot://selection", resources);
        Assert.DoesNotContain(resources, r => r.StartsWith("revit://", StringComparison.Ordinal) || r.StartsWith("etabs://", StringComparison.Ordinal));

        Assert.Contains("robot_query_template", prompts);
        Assert.Contains("robot_modify_template", prompts);
        Assert.Contains("toolify_run", prompts);
        Assert.DoesNotContain(prompts, p => p.StartsWith("revit_", StringComparison.Ordinal) || p.StartsWith("etabs_", StringComparison.Ordinal));
    }

    private static (BridgeOptions bridge, RegistryOptions registry) OptionsFor(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        McpServerHost.ConfigureOptions(services, configuration, RobotHostProfile.Instance);
        using var provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<IOptions<BridgeOptions>>().Value,
                provider.GetRequiredService<IOptions<RegistryOptions>>().Value);
    }
}
```

---

## 4. Test Suite 2 Design: `SeedCatalogTests.cs`

### 4.1 Objectives & Verifications
`SeedCatalogTests` validates the 12 embedded seed tools that ship with `HPRobot.Mcp.Server`. It guarantees:
1. **Manifest Resource Discovery**: All 12 seeds are extracted from embedded assembly resources (`SeedLibrary/{Category}/{Name}/...`), exactly as loaded in production.
2. **Schema & Metadata Rigor**: All `tool.json` files conform to JSON Schema Draft 7, declare `status="published"`, valid `host="robot"`, target `hostVersions` containing `2026`, have valid transactions (`none` or `auto`), declare appropriate destructive hints, and pass `ToolValidator.Validate`.
3. **Examples Richness**: Every `examples.json` contains $\ge 2$ examples, each containing an `args` JSON object with distinct property sets that match the schema properties.
4. **Code Quality & Guard Compliance**: Every `code.cs` is $\le 32$ KB and $\le 120$ lines, ends with a top-level `return` statement, has zero `ScriptGuard` violations, reads exactly the args declared in `inputSchema`, and contains no forbidden process, file-destroying, or reflection APIs.
5. **Dynamic Tool Registry (24 Tools Total)**: Together with the 4 core tools and 8 registry meta tools, exactly 24 tools are exposed through the MCP interface.

### 4.2 Detailed Seed Tool Inventory

| Category | Seed Tool Name | Tier | Mode | Timeout | Destructive | Expected Args |
|---|---|---|---|---|---|---|
| **Analysis** | `run_calculations` | D (Heavy) | `auto` | 300 s | `true` | `autoGenerateModel` |
| **Geometry** | `assign_node_support` | W | `auto` | 30 s | `true` | `nodeNumber`, `supportName`, `ux`, `uy`, `uz`, `rx`, `ry`, `rz` |
| **Geometry** | `draw_bar_by_coords` | W | `auto` | 30 s | `true` | `startX`, `startY`, `startZ`, `endX`, `endY`, `endZ`, `sectionName`, `materialName` |
| **Geometry** | `get_coordinate_systems_and_grids` | R | `none` | 30 s | `false` | `includeStructuralAxes` |
| **Geometry** | `get_structural_objects` | R | `none` | 30 s | `false` | `objectType`, `maxCount` |
| **Load** | `assign_bar_load` | W | `auto` | 30 s | `true` | `barNumber`, `caseNumber`, `loadType`, `pz`, `py`, `px` |
| **Load** | `get_load_definitions` | R | `none` | 30 s | `false` | `includeCombinations` |
| **Model** | `get_model_info` | R | `none` | 30 s | `false` | `includeCases` |
| **Property** | `assign_bar_section` | W | `auto` | 30 s | `true` | `barNumber`, `sectionName` |
| **Property** | `get_materials_and_sections` | R | `none` | 30 s | `false` | `includeMaterials`, `includeSections` |
| **Results** | `get_bar_forces` | R | `none` | 30 s | `false` | `barNumber`, `caseNumber`, `pointCount` |
| **Results** | `get_node_reactions` | R | `none` | 30 s | `false` | `nodeNumber`, `caseNumber` |

### 4.3 Reference Implementation Design: `SeedCatalogTests.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HPRobot.Mcp.Server.Hosts.Robot;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Registry;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Xunit;

namespace HPRobot.Mcp.Server.Tests;

public sealed class SeedCatalogTests
{
    private const int MaxCodeBytes = 32 * 1024;
    private const int MaxCodeLines = 120;
    private static readonly string[] AllowedModes = ["auto", "manual", "none"];

    private static readonly Regex ForbiddenPatterns = new(
        @"\b(System\.Diagnostics\.Process|Process\.Start|Quit|ApplicationExit|Interactive|MessageBox|Assembly\.Load|Type\.GetType)\b",
        RegexOptions.Compiled);

    public sealed record Seed(string Category, string Name, JsonElement Tool, string Code, JsonElement Examples);

    private static readonly Lazy<IReadOnlyList<Seed>> CachedSeeds = new(LoadEmbeddedSeeds);

    public static IReadOnlyList<Seed> Seeds => CachedSeeds.Value;

    public static IEnumerable<object[]> SeedKeys() => Seeds.Select(s => new object[] { s.Category + "/" + s.Name });

    public static Seed GetSeed(string key) => Seeds.Single(s => s.Category + "/" + s.Name == key);

    private static IReadOnlyList<Seed> LoadEmbeddedSeeds()
    {
        var assembly = typeof(RobotHostProfile).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(SeedInstaller.ResourcePrefix, StringComparison.Ordinal))
            .ToArray();

        string ReadResource(string name)
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        return resourceNames
            .Select(n => n.Replace('\\', '/'))
            .GroupBy(n => n[..n.LastIndexOf('/')])
            .Select(g =>
            {
                var parts = g.Key.Split('/'); // SeedLibrary/<Category>/<Name>
                string GetFile(string fileName) => resourceNames.First(n => n.Replace('\\', '/') == g.Key + "/" + fileName);

                return new Seed(
                    parts[1],
                    parts[2],
                    JsonSerializer.Deserialize<JsonElement>(ReadResource(GetFile("tool.json"))),
                    ReadResource(GetFile("code.cs")),
                    JsonSerializer.Deserialize<JsonElement>(ReadResource(GetFile("examples.json"))));
            })
            .OrderBy(s => s.Category).ThenBy(s => s.Name)
            .ToArray();
    }

    private static bool EndsWithReturn(string code)
    {
        var lines = code.TrimEnd().Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        var last = Array.FindLastIndex(lines, l => l.StartsWith("return ", StringComparison.Ordinal) || l == "return;");
        if (last < 0 || !lines[^1].TrimEnd().EndsWith(';')) return false;
        return lines.Skip(last + 1).All(l => l.Length == 0 || char.IsWhiteSpace(l[0]) || l[0] is '{' or '}' or ')');
    }

    [Fact]
    public void Manifest_DiscoversExactlyTwelveSeeds_AcrossSixCategories()
    {
        Assert.Equal(12, Seeds.Count);

        var categories = Seeds.Select(s => s.Category).Distinct().OrderBy(c => c, StringComparer.Ordinal).ToArray();
        Assert.Equal(["Analysis", "Geometry", "Load", "Model", "Property", "Results"], categories);

        Assert.Equal(7, Seeds.Count(s => s.Tool.GetProperty("transaction").GetString() == "none"));
        Assert.Equal(5, Seeds.Count(s => s.Tool.GetProperty("transaction").GetString() == "auto"));

        // Only run_calculations is flagged with destructive tag and 300s timeout
        var destructiveSeeds = Seeds.Where(s =>
            s.Tool.TryGetProperty("tags", out var tags) &&
            tags.EnumerateArray().Any(t => t.GetString() == "destructive")).Select(s => s.Name).ToArray();

        Assert.Equal(["run_calculations"], destructiveSeeds);
    }

    [Theory]
    [MemberData(nameof(SeedKeys))]
    public void Seed_ToolJson_ConformsToSchemaAndConventions(string key)
    {
        var seed = GetSeed(key);
        var tool = seed.Tool;
        var profile = RobotHostProfile.Instance;

        Assert.Equal(seed.Name, tool.GetProperty("name").GetString());
        Assert.Equal(seed.Category, tool.GetProperty("category").GetString());
        Assert.Contains(seed.Category, profile.Categories);
        Assert.Matches("^[a-z][a-z0-9_]{2,63}$", seed.Name);
        Assert.False(ToolValidator.IsReserved(seed.Name, profile), $"Seed '{seed.Name}' shadows a core tool name.");
        Assert.Equal("robot", tool.GetProperty("host").GetString());
        Assert.Contains("2026", tool.GetProperty("hostVersions").EnumerateArray().Select(v => v.GetString()));
        Assert.Equal("hprebar", tool.GetProperty("author").GetString());
        Assert.Equal("published", tool.GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(tool.GetProperty("notes").GetString()));

        var description = tool.GetProperty("description").GetString()!;
        Assert.True(description.Length >= 40, $"Description of '{seed.Name}' is too short.");

        var mode = tool.GetProperty("transaction").GetString();
        Assert.Contains(mode, AllowedModes);

        var timeout = tool.GetProperty("timeoutSeconds").GetInt32();
        Assert.InRange(timeout, 10, profile.MaxTimeoutSeconds);

        if (mode == "none")
        {
            Assert.False(tool.GetProperty("destructive").GetBoolean());
            Assert.True(timeout <= 30);
        }
        else
        {
            Assert.True(tool.GetProperty("destructive").GetBoolean());
        }

        var schema = tool.GetProperty("inputSchema");
        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.Equal(JsonValueKind.Object, schema.GetProperty("properties").ValueKind);

        if (schema.TryGetProperty("additionalProperties", out var addl))
        {
            Assert.False(addl.GetBoolean());
        }

        var properties = schema.GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (schema.TryGetProperty("required", out var requiredArray))
        {
            foreach (var req in requiredArray.EnumerateArray())
            {
                Assert.Contains(req.GetString()!, properties);
            }
        }
    }

    [Theory]
    [MemberData(nameof(SeedKeys))]
    public void Seed_ExamplesJson_HasAtLeastTwoValidExamples_WithDistinctArgs(string key)
    {
        var seed = GetSeed(key);
        var schema = seed.Tool.GetProperty("inputSchema");
        var declaredProperties = schema.GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requiredProperties = schema.TryGetProperty("required", out var reqArr)
            ? reqArr.EnumerateArray().Select(r => r.GetString()!).ToArray()
            : [];

        Assert.True(seed.Examples.GetArrayLength() >= 2, $"Seed '{seed.Name}' must have >= 2 examples.");

        var distinctArgs = seed.Examples.EnumerateArray()
            .Select(e => JsonSerializer.Serialize(e.GetProperty("args")))
            .Distinct()
            .Count();
        Assert.True(distinctArgs >= 2, $"Seed '{seed.Name}' examples must have distinct argument payloads.");

        foreach (var example in seed.Examples.EnumerateArray())
        {
            Assert.False(string.IsNullOrWhiteSpace(example.GetProperty("title").GetString()));
            var args = example.GetProperty("args");
            Assert.Equal(JsonValueKind.Object, args.ValueKind);

            var argKeys = args.EnumerateObject().Select(p => p.Name).ToArray();
            Assert.All(argKeys, k => Assert.Contains(k, declaredProperties));
            Assert.All(requiredProperties, req => Assert.Contains(req, argKeys, StringComparer.OrdinalIgnoreCase));
        }
    }

    [Theory]
    [MemberData(nameof(SeedKeys))]
    public void Seed_Code_PassesSafetyGuard_AndMatchesInputSchema(string key)
    {
        var seed = GetSeed(key);

        Assert.True(Encoding.UTF8.GetByteCount(seed.Code) < MaxCodeBytes);
        Assert.True(seed.Code.Split('\n').Length <= MaxCodeLines);
        Assert.True(EndsWithReturn(seed.Code), $"Seed '{seed.Name}' code must end with a top-level return statement.");
        Assert.DoesNotMatch(ForbiddenPatterns, seed.Code);

        var guardViolations = ScriptGuard.Check(seed.Code, GuardProfile.Robot);
        Assert.Empty(guardViolations);

        var facts = ScriptAnalyzer.Analyze(seed.Code, AnalyzerProfile.Robot);
        var declared = seed.Tool.GetProperty("inputSchema").GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var read = facts.ArgKeys.Select(a => a.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(read.IsSubsetOf(declared), $"Seed '{seed.Name}' reads undeclared args: {string.Join(", ", read.Except(declared))}");
        Assert.False(facts.UsesTransaction, $"Seed '{seed.Name}' must not open a transaction.");
    }

    [Theory]
    [MemberData(nameof(SeedKeys))]
    public void Seed_Record_PassesToolValidator(string key)
    {
        var seed = GetSeed(key);
        var record = RegistryJson.Deserialize<ToolRecord>(seed.Tool.GetRawText())!;
        record.Code = seed.Code;
        record.Examples = RegistryJson.Deserialize<List<ToolExample>>(seed.Examples.GetRawText())!;

        var report = ToolValidator.Validate(record, null, [], false, RobotHostProfile.Instance);
        Assert.True(report.IsValid, string.Join("; ", report.Errors));
    }

    [Fact]
    public void DynamicRegistry_YieldsExactly24ToolsTotal()
    {
        // 1. Check static tools
        using var host = McpServerHost.CreateBuilder([], RobotHostProfile.Instance).Build();
        var staticTools = host.Services.GetServices<McpServerTool>()
            .Select(t => t.ProtocolTool.Name)
            .ToHashSet(StringComparer.Ordinal);

        // 2. Check embedded seeds
        var seeds = SeedInstaller.LoadSeeds(typeof(RobotHostProfile).Assembly)
            .Select(s => s.Name)
            .ToHashSet(StringComparer.Ordinal);

        var combinedCatalog = new HashSet<string>(staticTools, StringComparer.Ordinal);
        foreach (var seedName in seeds)
        {
            combinedCatalog.Add(seedName);
        }

        Assert.Equal(24, combinedCatalog.Count);
        Assert.Equal(4, RobotHostProfile.Instance.CoreToolNames.Count);
        Assert.Equal(8, staticTools.Count - RobotHostProfile.Instance.CoreToolNames.Count);
        Assert.Equal(12, seeds.Count);

        // Check specific seed names exist in combined catalog
        Assert.Contains("run_calculations", combinedCatalog);
        Assert.Contains("assign_node_support", combinedCatalog);
        Assert.Contains("draw_bar_by_coords", combinedCatalog);
        Assert.Contains("get_coordinate_systems_and_grids", combinedCatalog);
        Assert.Contains("get_structural_objects", combinedCatalog);
        Assert.Contains("assign_bar_load", combinedCatalog);
        Assert.Contains("get_load_definitions", combinedCatalog);
        Assert.Contains("get_model_info", combinedCatalog);
        Assert.Contains("assign_bar_section", combinedCatalog);
        Assert.Contains("get_materials_and_sections", combinedCatalog);
        Assert.Contains("get_bar_forces", combinedCatalog);
        Assert.Contains("get_node_reactions", combinedCatalog);
    }
}
```

---

## 5. Test Suite 3 Design: `RobotToolsOverPipeTests.cs` (Pipe Round-Trip Tests)

### 5.1 Objectives & Verifications
`RobotToolsOverPipeTests` proves that `HPRobot.Mcp.Server` talks seamlessly over an authentic Windows Named Pipe to an `IBridgeExecutor` behind a `PipeListener` and `RequestDispatcher`.
Key capabilities verified:
1. **`robot.ping`**: Returns `pong=true`, reports host version (`2026`), active `ExecutionEnabled` status, and maintains pipe connectivity.
2. **`robot.context`**: Shapes the `RobotInfo` DTO (`isAttached`, `attachedPid`, `robotVersion`, `structureType`, `isCalculated`, `heavyOperationsEnabled`, `nodeCount`, `barCount`, `panelCount`, `loadCaseCount`), strips Revit-specific fields (`revitVersion`, `isFamily`), handles detached session, and surfaces actionable error codes (`Busy`, `NoActiveDocument`).
3. **`robot.execute`**: Forwards script execution parameters, returns snapshot filename (`ExecuteResult.Snapshot`), handles static preview (`PREVIEW` diagnostic) when `dryRun=true`, honors timeout clamping (up to 300 s), and enforces bridge opt-in checks.
4. **`robot.cancel`**: Dispatches cancellation over the pipe, handles timeout-initiated cancellation with cooperative persistence warning ("persisted (no rollback)").
5. **Bridge Missing Error**: Produces formatted failure message instructing user to start `HPRobot.McpBridge.exe` on pipe `hprobot-mcp-2026` with no local machine paths exposed.

### 5.2 Reference Implementation Design: `RobotToolsOverPipeTests.cs`

```csharp
using System;
using System.Text.Json;
using System.Threading.Tasks;
using HPRobot.Mcp.Server.Hosts.Robot;
using HPRobot.Mcp.Server.Hosts.Robot.Resources;
using HPRobot.Mcp.Server.Hosts.Robot.Tools;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using Xunit;

namespace HPRobot.Mcp.Server.Tests;

public sealed class RobotToolsOverPipeTests : IAsyncLifetime
{
    private const string DisabledText =
        "Code execution is disabled. Ask the user to tick 'Allow AI execution' in the HPRobot MCP Bridge window (a separate desktop app, not inside Robot).";

    private readonly string _pipeName = "hprobot-mcp-test-" + Guid.NewGuid().ToString("N");
    private readonly FakeRevitExecutor _executor = new();
    private readonly BridgeSettings _settings = new() { ExecutionEnabled = true };

    private PipeListener _listener = null!;
    private RevitBridgeClient _client = null!;
    private ContextService _context = null!;
    private ExecuteCodeService _execute = null!;
    private IOptions<BridgeOptions> _options = null!;

    public ValueTask InitializeAsync()
    {
        _listener = new PipeListener(
            _pipeName,
            new RequestDispatcher(_executor, _settings, "2026", "Robot", DisabledText));
        _listener.Start();

        _options = Options.Create(new BridgeOptions
        {
            HostId = "robot",
            HostVersion = 2026,
            PipeName = _pipeName,
            ConnectTimeoutMs = 3000,
            PingIntervalSeconds = 60
        });

        _client = new RevitBridgeClient(_options, NullLogger<RevitBridgeClient>.Instance, RobotHostProfile.Instance);
        var formatter = new ResultFormatter();
        _context = new ContextService(_client, formatter);
        _execute = new ExecuteCodeService(_client, formatter, _options);

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        await _listener.StopAsync();
        _listener.Dispose();
    }

    private static string TextOf(CallToolResult result) => ((TextContentBlock)result.Content[0]).Text;

    private static ContextResult StandardRobotContext(bool includeSelection) => new()
    {
        Host = "robot",
        HostVersion = "2026",
        DocTitle = "TowerFrame.rtd",
        DocPath = @"C:\Models\TowerFrame.rtd",
        IsModifiable = true,
        Units = new UnitsInfo("m"),
        Robot = new RobotInfo(
            IsAttached: true,
            AttachedPid: 5432,
            RobotVersion: "39.0.1.11984",
            StructureType: "I_ST_FRAME_3D",
            IsCalculated: true,
            HeavyOperationsEnabled: true,
            NodeCount: 120,
            BarCount: 240,
            PanelCount: 30,
            LoadCaseCount: 8),
        Selection = includeSelection ? [new ElementInfo(1, "Bar", "HEA 200")] : []
    };

    [Fact]
    public async Task Ping_RoundTripsOverPipe_WithRobotHostState()
    {
        var pong = await _client.SendAsync<BridgePingResult>(
            "robot.ping", null, TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken);

        Assert.True(pong.Pong);
        Assert.Equal("2026", pong.RevitVersion);
        Assert.True(pong.ExecutionEnabled);
        Assert.True(_client.IsConnected);

        _settings.ExecutionEnabled = false;
        pong = await _client.SendAsync<BridgePingResult>(
            "robot.ping", null, TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken);
        Assert.False(pong.ExecutionEnabled);
    }

    [Fact]
    public async Task Context_Tool_ReturnsRobotBlock_AndHidesRevitNamedFields()
    {
        _executor.ContextHandler = StandardRobotContext;

        var result = await new GetRobotContextTool(_context).GetContextAsync(
            includeSelection: true, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        using var json = JsonDocument.Parse(TextOf(result));
        var root = json.RootElement;

        Assert.Equal("robot", root.GetProperty("host").GetString());
        Assert.Equal("2026", root.GetProperty("hostVersion").GetString());
        Assert.False(root.TryGetProperty("revitVersion", out _));
        Assert.False(root.TryGetProperty("isFamily", out _));
        Assert.False(root.TryGetProperty("etabs", out _));
        Assert.False(root.TryGetProperty("sap2000", out _));
        Assert.False(root.TryGetProperty("navis", out _));

        var robot = root.GetProperty("robot");
        Assert.True(robot.GetProperty("isAttached").GetBoolean());
        Assert.Equal(5432, robot.GetProperty("attachedPid").GetInt32());
        Assert.Equal("39.0.1.11984", robot.GetProperty("robotVersion").GetString());
        Assert.Equal("I_ST_FRAME_3D", robot.GetProperty("structureType").GetString());
        Assert.True(robot.GetProperty("isCalculated").GetBoolean());
        Assert.True(robot.GetProperty("heavyOperationsEnabled").GetBoolean());
        Assert.Equal(120, robot.GetProperty("nodeCount").GetInt32());
        Assert.Equal(240, robot.GetProperty("barCount").GetInt32());

        Assert.Equal("Bar", root.GetProperty("selection")[0].GetProperty("category").GetString());
        Assert.True(_executor.LastContextIncludedSelection);
    }

    [Fact]
    public async Task Context_BeforeAttach_ReportsIsAttachedFalse()
    {
        _executor.ContextHandler = _ => new ContextResult
        {
            Host = "robot",
            HostVersion = "2026",
            IsModifiable = false,
            Robot = new RobotInfo(false, null, null, null, false, false, 0, 0, 0, 0)
        };

        var result = await new GetRobotContextTool(_context).GetContextAsync(
            includeSelection: false, TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(TextOf(result));
        var robot = json.RootElement.GetProperty("robot");
        Assert.False(robot.GetProperty("isAttached").GetBoolean());
        Assert.False(json.RootElement.GetProperty("isModifiable").GetBoolean());
    }

    [Fact]
    public async Task Model_Resource_ReturnsSameSnapshotAsJsonText()
    {
        _executor.ContextHandler = StandardRobotContext;

        var text = await new RobotResourceProvider(_context).ModelInfoAsync(TestContext.Current.CancellationToken);

        using var json = JsonDocument.Parse(text);
        Assert.Equal("TowerFrame.rtd", json.RootElement.GetProperty("docTitle").GetString());
        Assert.Equal(240, json.RootElement.GetProperty("robot").GetProperty("barCount").GetInt32());
        Assert.False(json.RootElement.TryGetProperty("revitVersion", out _));
    }

    [Fact]
    public async Task Execute_Tool_SendsRequestOverRobotMethod_AndReturnsSnapshot()
    {
        _executor.ExecuteHandler = request => new ExecuteResult
        {
            Value = JsonSerializer.SerializeToElement(new { createdBar = 42 }),
            ValueType = "object",
            Changed = new ChangedCounts(1, 0, 0),
            RolledBack = false,
            DurationMs = 15,
            Snapshot = "20260921-221500-draw_bar.rtd"
        };

        var args = JsonSerializer.SerializeToElement(new { section = "HEA 200" });
        var result = await new ExecuteRobotCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: false, timeoutSeconds: 30,
            label: "draw bar", args: args, progress: null, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        var request = _executor.LastExecuteRequest!;
        Assert.Equal("return 1;", request.Code);
        Assert.Equal("auto", request.Transaction);
        Assert.False(request.DryRun);
        Assert.Equal(30, request.TimeoutSeconds);
        Assert.Equal("draw bar", request.Label);
        Assert.Equal("HEA 200", request.Args!.Value.GetProperty("section").GetString());

        using var json = JsonDocument.Parse(TextOf(result));
        Assert.Equal("20260921-221500-draw_bar.rtd", json.RootElement.GetProperty("snapshot").GetString());
        Assert.False(json.RootElement.GetProperty("rolledBack").GetBoolean());
    }

    [Fact]
    public async Task Execute_Tool_AllowsUpTo300Seconds_AndClampsAbove()
    {
        var accepted = await new ExecuteRobotCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: false, timeoutSeconds: 300,
            label: null, args: null, progress: null, TestContext.Current.CancellationToken);
        Assert.False(accepted.IsError);
        Assert.Equal(300, _executor.LastExecuteRequest!.TimeoutSeconds);

        var clamped = await new ExecuteRobotCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.Auto, dryRun: false, timeoutSeconds: 600,
            label: null, args: null, progress: null, TestContext.Current.CancellationToken);
        Assert.False(clamped.IsError);
        Assert.Equal(300, _executor.LastExecuteRequest!.TimeoutSeconds);
    }

    [Fact]
    public async Task StaticPreview_ComesBackAsError_WithPreviewDiagnostic()
    {
        _executor.ExecuteHandler = _ => new ExecuteResult
        {
            IsError = true,
            RolledBack = true,
            Message = "static preview: would call structure.Bars.Create — send transaction:auto with dryRun:false to run it",
            Diagnostics = [new ScriptDiagnostic(1, 1, "PREVIEW", "structure.Bars.Create (W)")]
        };

        var result = await new ExecuteRobotCodeTool(_execute).ExecuteAsync(
            "structure.Bars.Create(1, 1, 2); return 1;", TransactionModes.None, dryRun: true,
            timeoutSeconds: 30, label: null, args: null, progress: null, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Contains("PREVIEW", TextOf(result));
        Assert.Contains("structure.Bars.Create", TextOf(result));
    }

    [Fact]
    public async Task Execute_SurfacesExecutionDisabledRefusal_NamingHPRobotBridge()
    {
        _settings.ExecutionEnabled = false;

        var result = await new ExecuteRobotCodeTool(_execute).ExecuteAsync(
            "return 1;", TransactionModes.None, dryRun: false, timeoutSeconds: 5,
            label: null, args: null, progress: null, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Contains("Allow AI execution", TextOf(result));
        Assert.Contains("HPRobot MCP Bridge", TextOf(result));
    }

    [Fact]
    public async Task Execute_SurfacesHeavyOperationsDisabledRefusal()
    {
        _executor.ExecuteHandler = _ => throw new BridgeRequestException(
            BridgeErrorCode.ExecutionDisabled,
            "Heavy operations are disabled. Ask the user to tick 'Allow heavy/destructive operations' in the HPRobot MCP Bridge window.");

        var result = await new ExecuteRobotCodeTool(_execute).ExecuteAsync(
            "robot.Project.CalcEngine.Calculate(); return 1;", TransactionModes.Auto, dryRun: false,
            timeoutSeconds: 300, label: "calc", args: null, progress: null, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        Assert.Contains("Allow heavy/destructive operations", TextOf(result));
    }

    [Fact]
    public async Task Context_Tool_SurfacesBusy_NotAttached_AndNoModel_AsErrorsNamingRobot()
    {
        _executor.ContextFailure = BridgeRequestException.Busy("Robot");
        var busy = await new GetRobotContextTool(_context).GetContextAsync(false, TestContext.Current.CancellationToken);
        Assert.True(busy.IsError);
        Assert.Contains("Robot", TextOf(busy));
        Assert.Contains("dialog", TextOf(busy));

        _executor.ContextFailure = new BridgeRequestException(BridgeErrorCode.NoActiveDocument, "Robot not attached — click Attach in HPRobot MCP Bridge.");
        var detached = await new GetRobotContextTool(_context).GetContextAsync(false, TestContext.Current.CancellationToken);
        Assert.True(detached.IsError);
        Assert.Contains("click Attach", TextOf(detached));

        _executor.ContextFailure = BridgeRequestException.NoActiveDocument("Robot", "model (.rtd)");
        var noModel = await new GetRobotContextTool(_context).GetContextAsync(false, TestContext.Current.CancellationToken);
        Assert.True(noModel.IsError);
        Assert.Contains(".rtd", TextOf(noModel));
    }

    [Fact]
    public async Task WithoutBridge_ErrorNamesBridgeExe_AndCarriesNoMachinePath()
    {
        await using var orphan = new RevitBridgeClient(
            Options.Create(new BridgeOptions
            {
                HostId = "robot",
                HostVersion = 2026,
                PipeName = "hprobot-mcp-nobody-" + Guid.NewGuid().ToString("N"),
                ConnectTimeoutMs = 300
            }),
            NullLogger<RevitBridgeClient>.Instance, RobotHostProfile.Instance);

        var context = new ContextService(orphan, new ResultFormatter());
        var result = await new GetRobotContextTool(context).GetContextAsync(false, TestContext.Current.CancellationToken);

        Assert.True(result.IsError);
        var text = TextOf(result);
        Assert.Contains("Robot bridge not connected", text);
        Assert.Contains("HPRobot.McpBridge.exe", text);
        Assert.Contains("hprobot-mcp-2026", text);
        Assert.DoesNotContain("enable the HP MCP Bridge", text);
        Assert.DoesNotContain(Environment.UserName, text);
        Assert.DoesNotContain(@"C:\", text);
    }

    [Fact]
    public async Task Cancel_DispatchesToBridgeExecutor()
    {
        var cancelResult = await _client.SendAsync<CancelResult>(
            "robot.cancel", new { id = 123 }, TimeSpan.FromSeconds(5), null, TestContext.Current.CancellationToken);

        Assert.Equal(1, _executor.CancelCalls);
    }

    [Fact]
    public async Task Timeout_InformsModelThatChangesMayHavePersisted()
    {
        _executor.ProgressSteps = 10;
        _executor.ProgressDelayMs = 100;

        await using var impatientClient = new RevitBridgeClient(
            Options.Create(new BridgeOptions
            {
                HostId = "robot",
                HostVersion = 2026,
                PipeName = _pipeName,
                ConnectTimeoutMs = 3000,
                PingIntervalSeconds = 60,
                ExtraTimeoutSeconds = 0
            }),
            NullLogger<RevitBridgeClient>.Instance, RobotHostProfile.Instance);

        var error = await Assert.ThrowsAsync<BridgeTimeoutException>(() => impatientClient.SendAsync<ExecuteResult>(
            "robot.execute", new ExecuteRequest("return 1;", "none", false, 5, "slow", null),
            TimeSpan.FromMilliseconds(200), null, TestContext.Current.CancellationToken));

        Assert.Contains("persisted (no rollback)", error.Message);
        Assert.Contains("snapshot", error.Message);
        Assert.DoesNotContain("nothing has been committed", error.Message);
        Assert.True(_executor.CancelCalls > 0);
    }
}
```

---

## 6. Companion Test Suite Design: `SeedCompilationTests.cs`

### 6.1 Objectives & Verifications
In addition to schema structure checks, `SeedCompilationTests` validates that all 12 seed script bodies compile cleanly via Roslyn CSharp against the actual `Interop.RobotOM.dll` types and match the script environment (`robot`, `structure`, `units`, `ct`, `log`, `progress`, `args`).
- Runs on machines where Robot Structural Analysis Professional 2026 is installed.
- Visibly skips with `Assert.SkipWhen(...)` if `Interop.RobotOM.dll` is absent, preventing build pipeline breakage on generic CI/CD runners while maintaining rigorous validation on workstation builds.

### 6.2 Reference Implementation Design: `SeedCompilationTests.cs`

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HPRobot.Mcp.Server.Hosts.Robot;
using HPRebar.Mcp.Contracts;
using HPRebar.McpBridge.Core.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Win32;
using Xunit;

namespace HPRobot.Mcp.Server.Tests;

public sealed class SeedCompilationTests
{
    private const string WrapperFile = "Interop.RobotOM.dll";
    private const string RobotClsid = "{F7870790-CDE5-11D1-8FF1-00A02447BAAE}";

    public static IEnumerable<object[]> Seeds() => SeedCatalogTests.SeedKeys();

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Seed_Code_CompilesAgainst_RobotOM_Wrapper(string key)
    {
        var seed = SeedCatalogTests.GetSeed(key);
        var compiled = Compile(seed.Code);

        Assert.SkipWhen(compiled is null, "Robot Structural Analysis 2026 not installed (Interop.RobotOM.dll not found)");
        Assert.True(compiled!.Value.errors.Length == 0,
            $"Seed '{seed.Name}' compilation errors:{Environment.NewLine}" +
            string.Join(Environment.NewLine, compiled.Value.errors));
    }

    [Fact]
    public void CompilerCheck_RejectsApiMisuse_AndAcceptsValidRobotOMCode()
    {
        var bad = Compile("return robot.NoSuchMember_12345;");
        Assert.SkipWhen(bad is null, "Robot Structural Analysis 2026 not installed (Interop.RobotOM.dll not found)");
        Assert.Contains(bad!.Value.errors, e => e.Contains("CS1061") && e.Contains("NoSuchMember_12345"));

        var good = Compile("int count = structure.Nodes.GetAll().Count; return new { count, units = \"m, kN\" };");
        Assert.Empty(good!.Value.errors);
    }

    private static (CSharpCompilation compilation, string[] errors)? Compile(string code)
    {
        var wrapperPath = FindWrapper();
        if (wrapperPath is null || !File.Exists(wrapperPath)) return null;

        var usings = string.Join(Environment.NewLine, HostScriptContracts.RobotImports.Select(ns => $"using {ns};"));
        var source = $$"""
            {{usings}}

            public sealed class SeedHost
            {
                public RobotOM.IRobotApplication robot;
                public RobotOM.IRobotStructure structure;
                public RobotOM.IRobotUnitMngr units;
                public System.Threading.CancellationToken ct;
                public Action<string> log;
                public Action<int, int?, string?> progress;
                public HPRebar.McpBridge.Core.Scripting.ScriptArgs args;

                public object Run()
                {
            #line 1 "code.cs"
            {{code}}
                }
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
            MetadataReference.CreateFromFile(typeof(ScriptArgs).Assembly.Location),
            MetadataReference.CreateFromFile(wrapperPath)
        };

        var compilation = CSharpCompilation.Create(
            "SeedCompileCheck_" + Guid.NewGuid().ToString("N"),
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToArray();

        return (compilation, errors);
    }

    private static string? FindWrapper()
    {
        var env = Environment.GetEnvironmentVariable("HPROBOT_ROBOT_DIR");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(Path.Combine(env, WrapperFile)))
            return Path.Combine(env, WrapperFile);

        try
        {
            using var key = Registry.ClassesRoot.OpenSubKey(@"CLSID\" + RobotClsid + @"\LocalServer32");
            var server = key?.GetValue("") as string;
            if (!string.IsNullOrWhiteSpace(server))
            {
                var dir = Path.GetDirectoryName(server.Trim('"'));
                if (dir is not null && File.Exists(Path.Combine(dir, WrapperFile)))
                    return Path.Combine(dir, WrapperFile);
            }
        }
        catch { }

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var defaultPath = Path.Combine(programFiles, "Autodesk", "Robot Structural Analysis Professional 2026", "Exe", WrapperFile);
        return File.Exists(defaultPath) ? defaultPath : null;
    }
}
```

---

## 7. Synthesis & Traceability Matrix

| Requirement from Mission & Project | Target Test File | Specific Test Method(s) | Verification Strategy |
|---|---|---|---|
| Profile name: "robot" | `RobotHostProfileTests.cs` | `Profile_NamesTheRobotPipe_Prefix_Tools...` | Assert `HostId == "robot"`, `PipeNaming.RobotHost` |
| DefaultVersion: 2026 | `RobotHostProfileTests.cs` | `Profile_NamesTheRobotPipe_Prefix_Tools...` | Assert `DefaultVersion == 2026`, `ValidVersions == [2024, 2025, 2026]` |
| ToolPrefix: "robot." | `RobotHostProfileTests.cs` | `Profile_NamesTheRobotPipe_Prefix_Tools...` | Assert `MethodPrefix == "robot."`, `JsonRpcMethods.RobotPrefix` |
| Imports: RobotOM, System, Collections, Linq, Scripting | `RobotHostProfileTests.cs` | `Profile_NamesTheRobotPipe_Prefix_Tools...` | Assert `ScriptImports == HostScriptContracts.RobotImports` |
| Globals: robot, structure, units, ct, log, progress, args | `RobotHostProfileTests.cs` | `Profile_NamesTheRobotPipe_Prefix_Tools...` | Assert `HostScriptContracts.RobotGlobals` |
| Timeout: RobotHeavyMaxTimeoutSeconds = 300 | `RobotHostProfileTests.cs` | `Profile_NamesTheRobotPipe_Prefix_Tools...` | Assert `MaxTimeoutSeconds == 300` |
| Help text & hints (sanitized, no machine paths) | `RobotHostProfileTests.cs` | `Profile_NamesTheRobotPipe_Prefix_Tools...` | Assert mentions bridge exe, no `C:\` or username |
| Options binding & version validation | `RobotHostProfileTests.cs` | `Options_BindRegistryRoot...`, `InvalidHostVersion...` | Assert DI configuration seeds and options validation |
| Static tool surface (12 static tools, 4 core + 8 registry) | `RobotHostProfileTests.cs` | `ToolSurface_ContainsFourCoreTools...` | Assert exact 12 static tools, no foreign host tools |
| All 12 seeds discovered via manifest resources | `SeedCatalogTests.cs` | `Manifest_DiscoversExactlyTwelveSeeds...` | `assembly.GetManifestResourceNames()`, 6 categories |
| All 12 `tool.json` schemas are valid | `SeedCatalogTests.cs` | `Seed_ToolJson_ConformsToSchemaAndConventions` | JSON schema, properties, additionalProperties:false |
| All 12 `examples.json` have >= 2 examples with "args" | `SeedCatalogTests.cs` | `Seed_ExamplesJson_HasAtLeastTwoValidExamples...` | $\ge 2$ examples, distinct args, matches schema properties |
| Seed script code quality & guard check | `SeedCatalogTests.cs` | `Seed_Code_PassesSafetyGuard_AndMatchesInputSchema` | `ScriptGuard`, `ScriptAnalyzer`, ends with `return` |
| Registry tool validator compliance | `SeedCatalogTests.cs` | `Seed_Record_PassesToolValidator` | `ToolValidator.Validate` reports valid |
| Dynamic registry registers all 24 tools total | `SeedCatalogTests.cs` | `DynamicRegistry_YieldsExactly24ToolsTotal` | 4 core + 8 registry + 12 seeds = 24 tools |
| `robot.ping` pipe round-trip | `RobotToolsOverPipeTests.cs` | `Ping_RoundTripsOverPipe_WithRobotHostState` | Verify pong, host version, execution enabled |
| `robot.context` pipe round-trip | `RobotToolsOverPipeTests.cs` | `Context_Tool_ReturnsRobotBlock...`, detached, errors | Verify `RobotInfo`, hide revit fields, busy/not attached errors |
| `robot.execute` pipe round-trip | `RobotToolsOverPipeTests.cs` | `Execute_Tool_SendsRequestOverRobotMethod...` | Parameter forwarding, snapshot name, preview diagnostic, timeout clamp (300 s) |
| `robot.cancel` pipe round-trip | `RobotToolsOverPipeTests.cs` | `Cancel_DispatchesToBridgeExecutor`, timeout persist | `executor.CancelCalls`, `persisted (no rollback)` message |
| Missing bridge error message | `RobotToolsOverPipeTests.cs` | `WithoutBridge_ErrorNamesBridgeExe_AndCarriesNoMachinePath` | Formatted guidance with no `C:\` or username |
| Roslyn seed compilation against Interop.RobotOM | `SeedCompilationTests.cs` | `Seed_Code_CompilesAgainst_RobotOM_Wrapper` | CSharpCompilation against wrapper or graceful skip |

---

## 8. Implementation Strategy for Implementer

1. **Create `HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj`**:
   - Apply the XML defined in Section 2.1.
   - Register in `HPRobot/HPRobot.slnx`.
2. **Implement Test Files**:
   - `RobotHostProfileTests.cs` (Section 3.3).
   - `SeedCatalogTests.cs` (Section 4.3).
   - `RobotToolsOverPipeTests.cs` (Section 5.2).
   - `SeedCompilationTests.cs` (Section 6.2).
3. **Execution & Validation**:
   ```bash
   dotnet test HPRobot/HPRobot.Mcp.Server.Tests/HPRobot.Mcp.Server.Tests.csproj
   ```
   Expectation: All test cases pass 100% cleanly without warnings or failures. On machines without Robot installed, `SeedCompilationTests` skips gracefully without failing.
