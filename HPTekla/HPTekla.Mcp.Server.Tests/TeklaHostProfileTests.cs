using System.ComponentModel;
using System.Reflection;
using HPTekla.Mcp.Server.Hosts.Tekla;
using HPTekla.Mcp.Server.Hosts.Tekla.Tools;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Xunit;

namespace HPTekla.Mcp.Server.Tests;

/// <summary>
///     What this exe registers: the Tekla pipe (version 2025), prefix, registry root, 600 s ceiling,
///     the two message hints that name the bridge plugin, and a tool surface that holds the Tekla core tools
///     plus the engine's registry tools — nothing named after other hosts.
/// </summary>
public sealed class TeklaHostProfileTests
{
    private static readonly string[] ExpectedRegistryTools =
    [
        "search_tools", "get_tool", "run_tool", "get_run", "propose_tool", "test_tool", "publish_tool", "manage_tool"
    ];

    [Fact]
    public void Profile_names_the_tekla_pipe_prefix_tools_registry_root_ceiling_and_hints()
    {
        var profile = TeklaHostProfile.Instance;

        Assert.Equal("tekla", profile.HostId);
        Assert.Equal("Tekla Structures", profile.DisplayName);
        Assert.Equal("HPTekla MCP", profile.ServerName);
        Assert.Equal("hptekla-mcp-2025", profile.PipeName(2025));
        Assert.Equal("tekla.execute", profile.Method("execute"));
        Assert.Equal("tekla.ping", profile.Method("ping"));
        Assert.Equal("tekla.context", profile.Method("context"));
        Assert.Equal("tekla.cancel", profile.Method("cancel"));
        Assert.Equal("execute_tekla_code", ((IHostProfile)profile).ExecuteToolName);
        Assert.Equal("get_tekla_context", ((IHostProfile)profile).ContextToolName);
        Assert.Equal("HPTekla", profile.ProductFolder);
        Assert.Equal("HPTEKLA_MCP_", profile.EnvPrefix);
        Assert.Equal(2025, profile.DefaultVersion);
        Assert.Equal([2025], profile.ValidVersions);
        Assert.Equal(600, profile.MaxTimeoutSeconds);
        Assert.Equal(HostScriptContracts.TeklaImports, profile.ScriptImports);
        Assert.Contains("Tekla.Structures", profile.ScriptImports);
        Assert.Contains("Tekla.Structures.Model", profile.ScriptImports);
        Assert.Contains("Tekla.Structures.Geometry3d", profile.ScriptImports);
        Assert.Contains("Tekla.Structures.Catalogs", profile.ScriptImports);
        Assert.Contains("HPRebar.McpBridge.Core.Scripting", profile.ScriptImports);
        Assert.Equal<string>(["model", "ct", "log", "progress", "args"], HostScriptContracts.TeklaGlobals);
        Assert.Same(typeof(TeklaHostProfile).Assembly, profile.HostAssembly);
        Assert.Equal("HPTekla.Mcp.Server.exe", profile.CliExecutable);

        Assert.Contains("Model", profile.Categories);
        Assert.Contains("Geometry", profile.Categories);
        Assert.Contains("Property", profile.Categories);
        Assert.Contains("Rebar", profile.Categories);
        Assert.Contains("Drawing", profile.Categories);
        Assert.Contains("Export", profile.Categories);
        Assert.Contains("Generic", profile.Categories);
        Assert.DoesNotContain(profile.Categories, c => c is "Layer" or "Block" or "Wall" or "Clash" or "Viewpoint");
        Assert.Equal(["execute_tekla_code", "get_tekla_context", "inspect_type", "cancel_execution"], profile.CoreToolNames);

        Assert.Contains("millimetres (mm)", profile.ScriptContractSummary);
        Assert.Contains("dryRun=true", profile.ScriptContractSummary);
        Assert.Contains("CommitChanges()", profile.ScriptContractSummary);
        Assert.Contains("Allow heavy operations", profile.ScriptContractSummary);

        Assert.Contains("Tekla Structures 2025", profile.BridgeNotConnectedHint);
        Assert.Contains("HPTekla MCP Bridge", profile.BridgeNotConnectedHint);
        Assert.Contains("hptekla-mcp-2025", profile.BridgeNotConnectedHint);
        Assert.Contains("dryRun=true", profile.TimeoutSemanticsHint);
        Assert.Contains("auto", profile.TimeoutSemanticsHint);
        Assert.DoesNotContain(@"C:\", profile.BridgeNotConnectedHint + profile.TimeoutSemanticsHint);
        Assert.DoesNotContain(Environment.UserName, profile.BridgeNotConnectedHint);
    }

    [Fact]
    public void Context_tool_description_names_the_tekla_properties()
    {
        var description = typeof(GetTeklaContextTool).GetMethod(nameof(GetTeklaContextTool.GetContextAsync))!
            .GetCustomAttributes(typeof(DescriptionAttribute), false)
            .Cast<DescriptionAttribute>()
            .Single().Description;

        Assert.Contains("hostVersion (2025)", description);
        Assert.Contains("modelName", description);
        Assert.Contains("modelPath", description);
        Assert.Contains("projectName", description);
        Assert.Contains("isModifiable", description);
        Assert.Contains("executionEnabled", description);
        Assert.Contains("heavyOperationsEnabled", description);
        Assert.Contains("partCount", description);
        Assert.Contains("rebarCount", description);
        Assert.Contains("drawingCount", description);
        Assert.Contains("includeSelection", description);
    }

    [Fact]
    public void Execute_tool_description_covers_tiers_save_and_preview_under_1800_characters()
    {
        var text = ExecuteTeklaCodeTool.ToolDescription;

        Assert.True(text.Length < 1800, $"description length {text.Length} exceeds the 1800-char budget");
        Assert.Contains("Tekla Structures 2025.0", text);
        Assert.Contains("model (Tekla.Structures.Model.Model)", text);
        Assert.Contains("selector (ModelObjectSelector)", text);
        Assert.Contains("millimetres (mm)", text);
        Assert.Contains("dryRun=true", text);
        Assert.Contains("CommitChanges()", text);
        Assert.Contains("Allow heavy operations", text);
        Assert.Contains("HPTekla MCP Bridge", text);
    }

    [Fact]
    public void Builder_seeds_tekla_profile_and_host_version_from_the_profile()
    {
        var builder = McpServerHost.CreateBuilder(["--host-version", "2025"], TeklaHostProfile.Instance);
        var host = builder.Build();

        var bridge = host.Services.GetRequiredService<IOptions<BridgeOptions>>().Value;
        Assert.Equal(2025, bridge.HostVersion);
        Assert.Equal("hptekla-mcp-2025", bridge.PipeName);

        var reg = host.Services.GetRequiredService<IOptions<RegistryOptions>>().Value;
        Assert.Equal("manual", reg.PublishPolicy);
        Assert.Contains(Path.Combine("HPTekla", "McpServer"), reg.LibraryPath);
    }

    [Fact]
    public void InvalidHostVersion_IsRefusedByValidation()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Bridge:HostVersion"] = "2020" })
            .Build();
        var services = new ServiceCollection();
        McpServerHost.ConfigureOptions(services, configuration, TeklaHostProfile.Instance);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<BridgeOptions>>().Value);
    }

    [Fact]
    public void ToolSurface_ContainsFourCoreTools_EightRegistryTools_NoForeignHostTools()
    {
        using var host = McpServerHost.CreateBuilder([], TeklaHostProfile.Instance).Build();

        var toolNames = host.Services.GetServices<McpServerTool>()
            .Select(t => t.ProtocolTool.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(12, toolNames.Length);
        Assert.Contains("execute_tekla_code", toolNames);
        Assert.Contains("get_tekla_context", toolNames);
        Assert.Contains("inspect_type", toolNames);
        Assert.Contains("cancel_execution", toolNames);
        Assert.All(ExpectedRegistryTools, regTool => Assert.Contains(regTool, toolNames));
        Assert.DoesNotContain(toolNames, n =>
            n.Contains("revit", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("autocad", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("etabs", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("sap2000", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("navis", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("excel", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("powerbi", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("robot", StringComparison.OrdinalIgnoreCase));

        var execTool = host.Services.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "execute_tekla_code").ProtocolTool;
        Assert.True(execTool.Annotations?.DestructiveHint);
        Assert.False(execTool.Annotations?.ReadOnlyHint);
        Assert.Equal(ExecuteTeklaCodeTool.ToolDescription, execTool.Description);

        var ctxTool = host.Services.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "get_tekla_context").ProtocolTool;
        Assert.True(ctxTool.Annotations?.ReadOnlyHint);
        Assert.True(ctxTool.Annotations?.IdempotentHint);
        Assert.Equal(GetTeklaContextTool.ToolDescription, ctxTool.Description);
    }

    [Fact]
    public void ResourcesAndPrompts_UseTeklaSchemeAndNames()
    {
        using var host = McpServerHost.CreateBuilder([], TeklaHostProfile.Instance).Build();

        var resources = host.Services.GetServices<McpServerResource>()
            .Select(r => r.ProtocolResourceTemplate.UriTemplate)
            .ToArray();
        var prompts = host.Services.GetServices<McpServerPrompt>()
            .Select(p => p.ProtocolPrompt.Name)
            .ToArray();

        Assert.Contains("tekla://model/info", resources);
        Assert.Contains("tekla://selection", resources);
        Assert.DoesNotContain(resources, r =>
            r.StartsWith("revit://", StringComparison.Ordinal) ||
            r.StartsWith("autocad://", StringComparison.Ordinal) ||
            r.StartsWith("etabs://", StringComparison.Ordinal) ||
            r.StartsWith("sap2000://", StringComparison.Ordinal) ||
            r.StartsWith("navis://", StringComparison.Ordinal) ||
            r.StartsWith("robot://", StringComparison.Ordinal));

        Assert.Contains("tekla_query_template", prompts);
        Assert.Contains("tekla_modify_template", prompts);
        Assert.Contains("tekla_rebar_template", prompts);
        Assert.Contains("toolify_run", prompts);
        Assert.DoesNotContain(prompts, p =>
            p.StartsWith("revit_", StringComparison.Ordinal) ||
            p.StartsWith("autocad_", StringComparison.Ordinal) ||
            p.StartsWith("etabs_", StringComparison.Ordinal) ||
            p.StartsWith("sap2000_", StringComparison.Ordinal) ||
            p.StartsWith("navis_", StringComparison.Ordinal) ||
            p.StartsWith("robot_", StringComparison.Ordinal));
    }

    [Fact]
    public void Server_name_is_the_tekla_one()
    {
        using var host = McpServerHost.CreateBuilder([], TeklaHostProfile.Instance).Build();

        var options = host.Services.GetRequiredService<IOptions<McpServerOptions>>().Value;

        Assert.Equal("HPTekla MCP", options.ServerInfo?.Name);
    }
}
