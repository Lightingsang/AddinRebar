using HPEtabs.Mcp.Server.Hosts;
using HPEtabs.Mcp.Server.Tools;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Xunit;

namespace HPEtabs.Mcp.Server.Tests;

/// <summary>
///     What this exe registers: the ETABS pipe (version 22, not a year), prefix, registry root, 600 s ceiling,
///     the two message hints that name the bridge program, and a tool surface that holds the ETABS core tools
///     plus the engine's registry tools — nothing named after the other hosts.
/// </summary>
public sealed class EtabsHostProfileTests
{
    private static readonly string[] RegistryTools = ["search_tools", "get_tool", "run_tool", "get_run", "propose_tool", "test_tool", "publish_tool", "manage_tool"];

    [Fact]
    public void Profile_names_the_etabs_pipe_prefix_tools_registry_root_ceiling_and_hints()
    {
        var profile = EtabsHostProfile.Instance;

        Assert.Equal("etabs", profile.HostId);
        Assert.Equal("ETABS", profile.DisplayName);
        Assert.Equal("HPEtabs MCP", profile.ServerName);
        Assert.Equal("hpetabs-mcp-22", profile.PipeName(22));
        Assert.Equal("etabs.execute", profile.Method("execute"));
        Assert.Equal("execute_etabs_code", profile.ExecuteToolName);
        Assert.Equal("get_etabs_context", profile.ContextToolName);
        Assert.Equal("HPEtabs", profile.ProductFolder);
        Assert.Equal("HPETABS_MCP_", profile.EnvPrefix);
        Assert.Equal(22, profile.DefaultVersion);
        Assert.Equal([22], profile.ValidVersions);
        Assert.Equal(600, profile.MaxTimeoutSeconds);
        Assert.Equal(HostScriptContracts.EtabsImports, profile.ScriptImports);
        Assert.Contains("kN_mm_C", profile.ScriptContractSummary);
        Assert.Contains("no transaction", profile.ScriptContractSummary);
        Assert.Contains("Allow destructive operations", profile.ScriptContractSummary);
        Assert.Same(typeof(EtabsHostProfile).Assembly, profile.HostAssembly);
        Assert.Equal("HPEtabs.Mcp.Server.exe", profile.CliExecutable);
        Assert.Contains("Analysis", profile.Categories);
        Assert.Contains("Results", profile.Categories);
        Assert.DoesNotContain(profile.Categories, c => c is "Layer" or "Block" or "Wall" or "Clash" or "Viewpoint");
        Assert.Equal(["execute_etabs_code", "get_etabs_context", "connect_etabs", "inspect_type", "cancel_execution"], profile.CoreToolNames);

        // The bridge is a program the user starts, not an add-in: both hints say so and neither carries a machine path.
        Assert.Contains("HPEtabs.McpBridge.exe", profile.BridgeNotConnectedHint);
        Assert.Contains("hpetabs-mcp-22", profile.BridgeNotConnectedHint);
        Assert.Contains("Attach", profile.BridgeNotConnectedHint);
        Assert.Contains("no rollback", profile.TimeoutSemanticsHint);
        Assert.DoesNotContain(@"C:\", profile.BridgeNotConnectedHint + profile.TimeoutSemanticsHint);
    }

    [Fact]
    public void Options_bind_the_registry_root_and_pipe_from_the_profile_even_without_configuration()
    {
        var withoutConfig = OptionsFor(new Dictionary<string, string?>());
        var withConfig = OptionsFor(new Dictionary<string, string?> { ["Registry:PublishPolicy"] = "manual", ["Bridge:HostVersion"] = "22" });

        // The engine seeds HostVersion from the profile: an exe started without appsettings.json beside it still validates.
        Assert.Equal(22, withoutConfig.bridge.HostVersion);
        Assert.Equal("hpetabs-mcp-22", withoutConfig.bridge.PipeName);
        Assert.Equal("hpetabs-mcp-22", withConfig.bridge.PipeName);
        Assert.Contains(Path.Combine("HPEtabs", "McpServer"), withConfig.registry.LibraryPath);
        Assert.Contains(Path.Combine("HPEtabs", "McpServer"), withConfig.registry.DbPath);
        Assert.DoesNotContain("HPRebar", withConfig.registry.LibraryPath);
        Assert.DoesNotContain("HPAutoCad", withConfig.registry.LibraryPath);
        Assert.DoesNotContain("HPNavis", withConfig.registry.LibraryPath);
    }

    private static (BridgeOptions bridge, RegistryOptions registry) OptionsFor(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        McpServerHost.ConfigureOptions(services, configuration, EtabsHostProfile.Instance);
        using var provider = services.BuildServiceProvider();
        return (provider.GetRequiredService<IOptions<BridgeOptions>>().Value, provider.GetRequiredService<IOptions<RegistryOptions>>().Value);
    }

    [Fact]
    public void A_year_instead_of_the_csi_version_is_refused()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Bridge:HostVersion"] = "2026" }).Build();
        var services = new ServiceCollection();
        McpServerHost.ConfigureOptions(services, configuration, EtabsHostProfile.Instance);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<BridgeOptions>>().Value);
    }

    [Fact]
    public void Tool_surface_is_the_five_etabs_core_tools_plus_the_registry_and_nothing_from_the_other_hosts()
    {
        using var host = McpServerHost.CreateBuilder([], EtabsHostProfile.Instance).Build();

        var names = host.Services.GetServices<McpServerTool>().Select(t => t.ProtocolTool.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        Assert.Equal(13, names.Length);
        Assert.Contains("execute_etabs_code", names);
        Assert.Contains("get_etabs_context", names);
        Assert.Contains("connect_etabs", names);
        Assert.Contains("inspect_type", names);
        Assert.Contains("cancel_execution", names);
        Assert.All(RegistryTools, tool => Assert.Contains(tool, names));
        Assert.DoesNotContain(names, n => n.Contains("revit", StringComparison.OrdinalIgnoreCase) || n.Contains("autocad", StringComparison.OrdinalIgnoreCase) || n.Contains("navis", StringComparison.OrdinalIgnoreCase));

        var execute = host.Services.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "execute_etabs_code").ProtocolTool;
        Assert.True(execute.Annotations?.DestructiveHint);
        var description = execute.Description!;
        // the contract the model must know before its first script: globals and units, the ret convention, the three tiers,
        // the forced save + snapshot, the two refusal mechanisms, what cancel cannot do, and the opt-in that lives outside ETABS
        Assert.Contains("kN_mm_C", description);
        Assert.Contains("ETABS returned {ret}", description);
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
        Assert.Contains("not inside ETABS", description);
        Assert.DoesNotContain("\\\\", description); // the path policy is stated, no UNC example a model could copy
        // ETABS has as much to say as Navisworks (2 000) — tiers, forced save, two refusal codes; anything beyond belongs in the prompts.
        Assert.True(description.Length <= 1800, $"description is {description.Length} chars");
        Assert.Equal(ExecuteEtabsCodeTool.ToolDescription, description);

        var context = host.Services.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "get_etabs_context").ProtocolTool;
        Assert.True(context.Annotations?.ReadOnlyHint);
        Assert.Contains("destructiveOperationsEnabled", context.Description);
        Assert.Contains("isLocked", context.Description);
        Assert.Contains("not attached", context.Description);
    }

    [Fact]
    public void Resources_and_prompts_use_the_etabs_scheme_and_names()
    {
        using var host = McpServerHost.CreateBuilder([], EtabsHostProfile.Instance).Build();

        var resources = host.Services.GetServices<McpServerResource>().Select(r => r.ProtocolResourceTemplate.UriTemplate).ToArray();
        var prompts = host.Services.GetServices<McpServerPrompt>().Select(p => p.ProtocolPrompt.Name).ToArray();

        Assert.Contains("etabs://model/info", resources);
        Assert.Contains("etabs://selection", resources);
        Assert.DoesNotContain(resources, r => r.StartsWith("revit://", StringComparison.Ordinal) || r.StartsWith("autocad://", StringComparison.Ordinal) || r.StartsWith("navis://", StringComparison.Ordinal));
        Assert.Contains("etabs_query_template", prompts);
        Assert.Contains("etabs_modify_template", prompts);
        Assert.Contains("toolify_run", prompts);
        Assert.DoesNotContain(prompts, p => p.StartsWith("revit_", StringComparison.Ordinal) || p.StartsWith("autocad_", StringComparison.Ordinal) || p.StartsWith("navis_", StringComparison.Ordinal));
    }

    [Fact]
    public void Server_name_is_the_etabs_one()
    {
        using var host = McpServerHost.CreateBuilder([], EtabsHostProfile.Instance).Build();

        var options = host.Services.GetRequiredService<IOptions<McpServerOptions>>().Value;

        Assert.Equal("HPEtabs MCP", options.ServerInfo?.Name);
    }
}
