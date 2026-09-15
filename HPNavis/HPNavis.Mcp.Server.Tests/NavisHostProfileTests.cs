using HPNavis.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Xunit;

namespace HPNavis.Mcp.Server.Tests;

/// <summary>
///     What this exe registers: the Navisworks pipe, prefix, registry root and 600 s ceiling, and a tool
///     surface that holds the Navisworks core tools plus the engine's registry tools — nothing named after
///     Revit or AutoCAD.
/// </summary>
public sealed class NavisHostProfileTests
{
    private static readonly string[] RegistryTools = ["search_tools", "get_tool", "run_tool", "get_run", "propose_tool", "test_tool", "publish_tool", "manage_tool"];

    [Fact]
    public void Profile_names_the_navis_pipe_prefix_tools_registry_root_and_ceiling()
    {
        var profile = NavisHostProfile.Instance;

        Assert.Equal("navis", profile.HostId);
        Assert.Equal("Navisworks", profile.DisplayName);
        Assert.Equal("HPNavis MCP", profile.ServerName);
        Assert.Equal("hpnavis-mcp-2026", profile.PipeName(2026));
        Assert.Equal("navis.execute", profile.Method("execute"));
        Assert.Equal("execute_navis_code", profile.ExecuteToolName);
        Assert.Equal("get_navis_context", profile.ContextToolName);
        Assert.Equal("HPNavis", profile.ProductFolder);
        Assert.Equal("HPNAVIS_MCP_", profile.EnvPrefix);
        Assert.Equal([2026], profile.ValidVersions);
        Assert.Equal(600, profile.MaxTimeoutSeconds);
        Assert.Equal(HostScriptContracts.NavisImports, profile.ScriptImports);
        Assert.Contains("Never open a Transaction", profile.ScriptContractSummary);
        Assert.Contains("heavy", profile.ScriptContractSummary);
        Assert.Same(typeof(NavisHostProfile).Assembly, profile.HostAssembly);
        Assert.Equal("HPNavis.Mcp.Server.exe", profile.CliExecutable);
        Assert.Contains("Clash", profile.Categories);
        Assert.Contains("Timeliner", profile.Categories);
        Assert.DoesNotContain(profile.Categories, c => c is "Layer" or "Block" or "Wall");
    }

    [Fact]
    public void Options_bind_the_registry_root_and_pipe_from_the_profile()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("Registry:PublishPolicy", "manual"), new KeyValuePair<string, string?>("Bridge:HostVersion", "2026")])
            .Build();
        var services = new ServiceCollection();
        McpServerHost.ConfigureOptions(services, configuration, NavisHostProfile.Instance);
        using var provider = services.BuildServiceProvider();

        var bridge = provider.GetRequiredService<IOptions<BridgeOptions>>().Value;
        var registry = provider.GetRequiredService<IOptions<RegistryOptions>>().Value;

        Assert.Equal("hpnavis-mcp-2026", bridge.PipeName);
        Assert.Contains(Path.Combine("HPNavis", "McpServer"), registry.LibraryPath);
        Assert.Contains(Path.Combine("HPNavis", "McpServer"), registry.DbPath);
        Assert.DoesNotContain("HPRebar", registry.LibraryPath);
        Assert.DoesNotContain("HPAutoCad", registry.LibraryPath);
    }

    [Fact]
    public void Tool_surface_is_the_four_navis_core_tools_plus_the_registry_and_nothing_from_the_other_hosts()
    {
        using var host = McpServerHost.CreateBuilder([], NavisHostProfile.Instance).Build();

        var names = host.Services.GetServices<McpServerTool>().Select(t => t.ProtocolTool.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        Assert.Equal(12, names.Length);
        Assert.Contains("execute_navis_code", names);
        Assert.Contains("get_navis_context", names);
        Assert.Contains("inspect_type", names);
        Assert.Contains("cancel_execution", names);
        Assert.All(RegistryTools, tool => Assert.Contains(tool, names));
        Assert.DoesNotContain(names, n => n.Contains("revit", StringComparison.OrdinalIgnoreCase) || n.Contains("autocad", StringComparison.OrdinalIgnoreCase));

        var execute = host.Services.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "execute_navis_code").ProtocolTool;
        Assert.True(execute.Annotations?.DestructiveHint);
        var description = execute.Description!;
        // the contract the model must know before its first script: runtime, globals, the four writability classes, heavy, dryRun semantics, the opt-in
        Assert.Contains(".NET Framework 4.8", description);
        Assert.Contains("units.ToMm", description);
        Assert.Contains("geometry is read-only", description);
        Assert.Contains("Allow heavy operations", description);
        Assert.Contains("HEAVY", description);
        Assert.Contains("rolledBack:false", description);
        Assert.Contains("fingerprint", description);
        Assert.Contains("Never open a Transaction", description);
        Assert.Contains("Allow AI code execution", description);
        Assert.DoesNotContain("\\\\", description); // the path policy is stated, no UNC example that a model could copy
        // Navisworks has more to say than Revit (1 270) or AutoCAD (≤ 1 800): runtime limits and the writability classes; anything beyond belongs in the prompts.
        Assert.True(description.Length <= 2000, $"description is {description.Length} chars");
        Assert.Contains("RemoveFile", description); // the heavy list is complete, not a sample

        var context = host.Services.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "get_navis_context").ProtocolTool;
        Assert.True(context.Annotations?.ReadOnlyHint);
        Assert.Contains("heavyOperationsEnabled", context.Description);
        Assert.Contains("hasClashModule", context.Description);
    }

    [Fact]
    public void Resources_and_prompts_use_the_navis_scheme_and_names()
    {
        using var host = McpServerHost.CreateBuilder([], NavisHostProfile.Instance).Build();

        var resources = host.Services.GetServices<McpServerResource>().Select(r => r.ProtocolResourceTemplate.UriTemplate).ToArray();
        var prompts = host.Services.GetServices<McpServerPrompt>().Select(p => p.ProtocolPrompt.Name).ToArray();

        Assert.Contains("navis://document/info", resources);
        Assert.Contains("navis://selection", resources);
        Assert.DoesNotContain(resources, r => r.StartsWith("revit://", StringComparison.Ordinal) || r.StartsWith("autocad://", StringComparison.Ordinal));
        Assert.Contains("navis_query_template", prompts);
        Assert.Contains("navis_review_template", prompts);
        Assert.Contains("toolify_run", prompts);
        Assert.DoesNotContain(prompts, p => p.StartsWith("revit_", StringComparison.Ordinal) || p.StartsWith("autocad_", StringComparison.Ordinal));
    }

    [Fact]
    public void Server_name_is_the_navis_one()
    {
        using var host = McpServerHost.CreateBuilder([], NavisHostProfile.Instance).Build();

        var options = host.Services.GetRequiredService<IOptions<McpServerOptions>>().Value;

        Assert.Equal("HPNavis MCP", options.ServerInfo?.Name);
    }
}
