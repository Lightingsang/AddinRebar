using HPAutoCad.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Xunit;

namespace HPAutoCad.Mcp.Server.Tests;

/// <summary>
///     What this exe registers: the AutoCAD pipe, prefix and registry root, and a tool surface that holds
///     the AutoCAD core tools plus the engine's registry tools — and nothing named after Revit.
/// </summary>
public sealed class HostProfileTests
{
    private static readonly string[] RegistryTools = ["search_tools", "get_tool", "run_tool", "get_run", "propose_tool", "test_tool", "publish_tool", "manage_tool"];

    [Fact]
    public void Profile_names_the_autocad_pipe_prefix_tools_and_registry_root()
    {
        var profile = AutocadHostProfile.Instance;

        Assert.Equal("autocad", profile.HostId);
        Assert.Equal("hpautocad-mcp-2026", profile.PipeName(2026));
        Assert.Equal("autocad.execute", profile.Method("execute"));
        Assert.Equal("execute_autocad_code", profile.ExecuteToolName);
        Assert.Equal("get_autocad_context", profile.ContextToolName);
        Assert.Equal("HPAutoCad", profile.ProductFolder);
        Assert.Equal("HPAUTOCAD_MCP_", profile.EnvPrefix);
        Assert.Equal([2026], profile.ValidVersions);
        Assert.Equal(HostScriptContracts.AutocadImports, profile.ScriptImports);
        Assert.Contains("tr", profile.ScriptContractSummary);
        Assert.Contains("never start a transaction", profile.ScriptContractSummary);
        Assert.Same(typeof(AutocadHostProfile).Assembly, profile.HostAssembly);
        Assert.DoesNotContain(profile.Categories, c => c is "Architecture" or "MEP");
    }

    [Fact]
    public void Options_bind_the_registry_root_and_pipe_from_the_profile()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("Registry:PublishPolicy", "manual"), new KeyValuePair<string, string?>("Bridge:HostVersion", "2026")])
            .Build();
        var services = new ServiceCollection();
        McpServerHost.ConfigureOptions(services, configuration, AutocadHostProfile.Instance);
        using var provider = services.BuildServiceProvider();

        var bridge = provider.GetRequiredService<IOptions<BridgeOptions>>().Value;
        var registry = provider.GetRequiredService<IOptions<RegistryOptions>>().Value;

        Assert.Equal("hpautocad-mcp-2026", bridge.PipeName);
        Assert.Contains(Path.Combine("HPAutoCad", "McpServer"), registry.LibraryPath);
        Assert.Contains(Path.Combine("HPAutoCad", "McpServer"), registry.DbPath);
        Assert.DoesNotContain("HPRebar", registry.LibraryPath);
    }

    [Fact]
    public void Tool_surface_is_the_four_autocad_core_tools_plus_the_registry_and_nothing_from_revit()
    {
        using var host = McpServerHost.CreateBuilder([], AutocadHostProfile.Instance).Build();

        var names = host.Services.GetServices<McpServerTool>().Select(t => t.ProtocolTool.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        Assert.Equal(12, names.Length);
        Assert.Contains("execute_autocad_code", names);
        Assert.Contains("get_autocad_context", names);
        Assert.Contains("inspect_type", names);
        Assert.Contains("cancel_execution", names);
        Assert.All(RegistryTools, tool => Assert.Contains(tool, names));
        Assert.DoesNotContain(names, n => n.Contains("revit", StringComparison.OrdinalIgnoreCase));

        var execute = host.Services.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "execute_autocad_code").ProtocolTool;
        Assert.True(execute.Annotations?.DestructiveHint);
        Assert.Contains("tr.AddNewlyCreatedDBObject", execute.Description);
        Assert.Contains("never call StartTransaction", execute.Description);
        Assert.Contains("HPMCPBRIDGE", execute.Description);
        Assert.True(execute.Description!.Length <= 2200, $"description is {execute.Description.Length} chars");
    }

    [Fact]
    public void Resources_and_prompts_use_the_autocad_scheme_and_names()
    {
        using var host = McpServerHost.CreateBuilder([], AutocadHostProfile.Instance).Build();

        var resources = host.Services.GetServices<McpServerResource>().Select(r => r.ProtocolResourceTemplate.UriTemplate).ToArray();
        var prompts = host.Services.GetServices<McpServerPrompt>().Select(p => p.ProtocolPrompt.Name).ToArray();

        Assert.Contains("autocad://document/info", resources);
        Assert.Contains("autocad://selection", resources);
        Assert.DoesNotContain(resources, r => r.StartsWith("revit://", StringComparison.Ordinal));
        Assert.Contains("autocad_query_template", prompts);
        Assert.Contains("autocad_modify_template", prompts);
        Assert.Contains("toolify_run", prompts);
        Assert.DoesNotContain(prompts, p => p.StartsWith("revit_", StringComparison.Ordinal));
    }
}
