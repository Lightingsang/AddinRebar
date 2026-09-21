using HPSap2000.Mcp.Server.Hosts;
using HPSap2000.Mcp.Server.Resources;
using HPSap2000.Mcp.Server.Tools;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Xunit;

namespace HPSap2000.Mcp.Server.Tests;

/// <summary>
///     What this exe registers: the SAP2000 pipe (version 27, CSI major release), prefix, registry root, 600 s ceiling,
///     the two message hints that name the bridge program, and a tool surface that holds the SAP2000 core tools
///     plus the engine's registry tools — nothing named after the other hosts.
/// </summary>
public sealed class Sap2000HostProfileTests
{
    private static readonly string[] RegistryTools = ["search_tools", "get_tool", "run_tool", "get_run", "propose_tool", "test_tool", "publish_tool", "manage_tool"];

    [Fact]
    public void Profile_names_the_sap2000_pipe_prefix_tools_registry_root_ceiling_and_hints()
    {
        var profile = Sap2000HostProfile.Instance;

        Assert.Equal("sap2000", profile.HostId);
        Assert.Equal("SAP2000", profile.DisplayName);
        Assert.Equal("HPSap2000 MCP", profile.ServerName);
        Assert.Equal("hpsap2000-mcp-27", profile.PipeName(27));
        Assert.Equal("sap2000.execute", profile.Method("execute"));
        Assert.Equal("execute_sap2000_code", profile.ExecuteToolName);
        Assert.Equal("get_sap2000_context", profile.ContextToolName);
        Assert.Equal("HPSap2000", profile.ProductFolder);
        Assert.Equal("HPSAP2000_MCP_", profile.EnvPrefix);
        Assert.Equal(27, profile.DefaultVersion);
        Assert.Equal([24, 25, 26, 27], profile.ValidVersions);
        Assert.Equal(600, profile.MaxTimeoutSeconds);
        Assert.Equal(HostScriptContracts.Sap2000Imports, profile.ScriptImports);
        Assert.Contains("kN_m_C", profile.ScriptContractSummary);
        Assert.Contains("no transaction", profile.ScriptContractSummary);
        Assert.Contains("Allow destructive operations", profile.ScriptContractSummary);
        Assert.Same(typeof(Sap2000HostProfile).Assembly, profile.HostAssembly);
        Assert.Equal("HPSap2000.Mcp.Server.exe", profile.CliExecutable);
        Assert.Contains("Analysis", profile.Categories);
        Assert.Contains("Results", profile.Categories);
        Assert.DoesNotContain(profile.Categories, c => c is "Layer" or "Block" or "Wall" or "Clash" or "Viewpoint");
        Assert.Equal(["execute_sap2000_code", "get_sap2000_context", "inspect_type", "cancel_execution"], profile.CoreToolNames);

        Assert.Contains("HPSap2000.McpBridge.exe", profile.BridgeNotConnectedHint);
        Assert.Contains("Attach", profile.BridgeNotConnectedHint);
        Assert.Contains("Allow AI code execution", profile.BridgeNotConnectedHint);
        Assert.Contains("hpsap2000-mcp-27", profile.BridgeNotConnectedHint);
        Assert.Contains("snapshot named in the bridge window", profile.TimeoutSemanticsHint);
    }

    [Fact]
    public void Context_tool_description_names_the_sap2000_properties()
    {
        var description = typeof(Sap2000ContextTool).GetMethod(nameof(Sap2000ContextTool.GetContextAsync))!
            .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
            .Cast<System.ComponentModel.DescriptionAttribute>()
            .Single().Description;

        Assert.Contains("hostVersion (27)", description);
        Assert.Contains("docTitle", description);
        Assert.Contains("docPath", description);
        Assert.Contains("isModifiable", description);
        Assert.Contains("units.length (m", description);
        Assert.Contains("sap2000 {isAttached", description);
        Assert.Contains("isLocked", description);
        Assert.Contains("presentUnits", description);
        Assert.Contains("databaseUnits", description);
        Assert.Contains("destructiveOperationsEnabled", description);
        Assert.Contains("pointCount, frameCount, areaCount", description);
        Assert.Contains("includeSelection", description);
    }

    [Fact]
    public void Execute_tool_description_covers_tiers_save_and_preview_under_1800_characters()
    {
        var text = ExecuteSap2000CodeTool.ToolDescription;

        Assert.True(text.Length < 1800, $"description length {text.Length} exceeds the 1800-char budget");
        Assert.Contains("SAP2000 27", text);
        Assert.Contains("sapModel (cSapModel)", text);
        Assert.Contains("sap (cOAPI)", text);
        Assert.Contains("units (forced to kN_m_C", text);
        Assert.Contains("No transaction or undo", text);
        Assert.Contains(".SDB snapshot", text);
        Assert.Contains("Allow destructive operations", text);
        Assert.Contains("PREVIEW", text);
        Assert.Contains("dryRun", text);
        Assert.Contains("HPSap2000 MCP Bridge", text);
    }

    [Fact]
    public void Builder_seeds_sap2000_profile_and_host_version_from_the_profile()
    {
        var builder = McpServerHost.CreateBuilder(["--host-version", "27"], Sap2000HostProfile.Instance);
        var host = builder.Build();

        var bridge = host.Services.GetRequiredService<IOptions<BridgeOptions>>().Value;
        Assert.Equal(27, bridge.HostVersion);

        var reg = host.Services.GetRequiredService<IOptions<RegistryOptions>>().Value;
        Assert.Equal("manual", reg.PublishPolicy);
    }

    [Fact]
    public void Server_registers_only_sap2000_and_registry_tools()
    {
        using var host = McpServerHost.CreateBuilder([], Sap2000HostProfile.Instance).Build();

        var names = host.Services.GetServices<McpServerTool>().Select(t => t.ProtocolTool.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        Assert.Equal(12, names.Length);
        Assert.Contains("execute_sap2000_code", names);
        Assert.Contains("get_sap2000_context", names);
        Assert.Contains("inspect_type", names);
        Assert.Contains("cancel_execution", names);
        Assert.All(RegistryTools, tool => Assert.Contains(tool, names));
        Assert.DoesNotContain(names, n => n.Contains("revit", StringComparison.OrdinalIgnoreCase) || n.Contains("autocad", StringComparison.OrdinalIgnoreCase) || n.Contains("navis", StringComparison.OrdinalIgnoreCase) || n.Contains("etabs", StringComparison.OrdinalIgnoreCase));

        var execute = host.Services.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "execute_sap2000_code").ProtocolTool;
        Assert.True(execute.Annotations?.DestructiveHint);
        var description = execute.Description!;
        Assert.Contains("kN_m_C", description);
        Assert.Contains("SAP2000 returned {ret}", description);
        Assert.Contains("ArgumentException", description);
        Assert.Contains("saves your model", description);
        Assert.Contains("snapshot", description);
        Assert.Contains("rolledBack:false", description);
        Assert.Contains("-32001", description);
        Assert.Contains("PREVIEW", description);
        Assert.Contains("Allow destructive operations", description);
        Assert.Contains("cannot interrupt", description);
        Assert.Contains("never UNC", description);
        Assert.Contains("Allow AI code execution", description);
        Assert.Contains("not inside SAP2000", description);
        Assert.True(description.Length <= 1800, $"description is {description.Length} chars");
        Assert.Equal(ExecuteSap2000CodeTool.ToolDescription, description);

        var context = host.Services.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "get_sap2000_context").ProtocolTool;
        Assert.True(context.Annotations?.ReadOnlyHint);
        Assert.Contains("destructiveOperationsEnabled", context.Description);
        Assert.Contains("isLocked", context.Description);
        Assert.Contains("not attached", context.Description);
    }

    [Fact]
    public void Resources_and_prompts_use_the_sap2000_scheme_and_names()
    {
        using var host = McpServerHost.CreateBuilder([], Sap2000HostProfile.Instance).Build();

        var resources = host.Services.GetServices<McpServerResource>().Select(r => r.ProtocolResourceTemplate.UriTemplate).ToArray();
        var prompts = host.Services.GetServices<McpServerPrompt>().Select(p => p.ProtocolPrompt.Name).ToArray();

        Assert.Contains("sap2000://model/info", resources);
        Assert.Contains("sap2000://selection", resources);
        Assert.DoesNotContain(resources, r => r.StartsWith("revit://", StringComparison.Ordinal) || r.StartsWith("autocad://", StringComparison.Ordinal) || r.StartsWith("navis://", StringComparison.Ordinal) || r.StartsWith("etabs://", StringComparison.Ordinal));
        Assert.Contains("sap2000_query_template", prompts);
        Assert.Contains("sap2000_modify_template", prompts);
        Assert.Contains("toolify_run", prompts);
        Assert.DoesNotContain(prompts, p => p.StartsWith("revit_", StringComparison.Ordinal) || p.StartsWith("autocad_", StringComparison.Ordinal) || p.StartsWith("navis_", StringComparison.Ordinal) || p.StartsWith("etabs_", StringComparison.Ordinal));
    }

    [Fact]
    public void Server_name_is_the_sap2000_one()
    {
        using var host = McpServerHost.CreateBuilder([], Sap2000HostProfile.Instance).Build();

        var options = host.Services.GetRequiredService<IOptions<McpServerOptions>>().Value;

        Assert.Equal("HPSap2000 MCP", options.ServerInfo?.Name);
    }
}
