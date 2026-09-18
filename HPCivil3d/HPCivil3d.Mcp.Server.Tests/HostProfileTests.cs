using HPCivil3d.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Xunit;

namespace HPCivil3d.Mcp.Server.Tests;

/// <summary>
///     What this exe registers: the Civil 3D pipe, prefix and registry root, and a tool surface that holds the Civil
///     core tools plus the engine's registry tools — nothing named after Revit and nothing that would collide with the
///     AutoCAD server running beside it.
/// </summary>
public sealed class HostProfileTests
{
    private static readonly string[] RegistryTools = ["search_tools", "get_tool", "run_tool", "get_run", "propose_tool", "test_tool", "publish_tool", "manage_tool"];

    [Fact]
    public void Profile_names_the_civil3d_pipe_prefix_tools_and_registry_root()
    {
        var profile = Civil3dHostProfile.Instance;

        Assert.Equal("civil3d", profile.HostId);
        Assert.Equal("Civil 3D", profile.DisplayName);
        Assert.Equal("HPCivil3d MCP", profile.ServerName);
        Assert.Equal("hpcivil3d-mcp-2026", profile.PipeName(2026));
        Assert.Equal("civil3d.execute", profile.Method("execute"));
        Assert.Equal("execute_civil3d_code", profile.ExecuteToolName);
        Assert.Equal("get_civil3d_context", profile.ContextToolName);
        Assert.Equal("HPCivil3d", profile.ProductFolder);
        Assert.Equal("HPCIVIL3D_MCP_", profile.EnvPrefix);
        Assert.Equal(2026, profile.DefaultVersion);
        Assert.Equal([2026], profile.ValidVersions);
        Assert.Equal("civil3d", profile.ResourceScheme);
        Assert.Equal(["Document", "Alignment", "Profile", "Surface", "Corridor", "Pipe", "Parcel", "Point", "Data", "Generic"], profile.Categories);
        Assert.Equal(["execute_civil3d_code", "get_civil3d_context", "inspect_type", "cancel_execution"], profile.CoreToolNames);
        Assert.Equal(HostScriptContracts.Civil3dImports, profile.ScriptImports);
        Assert.Contains("civil (CivilDocument", profile.ScriptContractSummary);
        Assert.Contains("insunitsMismatch", profile.ScriptContractSummary);
        Assert.Contains("Rebuild", profile.ScriptContractSummary);
        Assert.Equal("HPCivil3d.Mcp.Server.exe", profile.CliExecutable);
        Assert.Contains("/product C3D", profile.BridgeNotConnectedHint);
        Assert.Contains("HPC3DMCPBRIDGE", profile.BridgeNotConnectedHint);
        Assert.Null(profile.TimeoutSemanticsHint);
        Assert.Equal(120, profile.MaxTimeoutSeconds);
        Assert.Same(typeof(Civil3dHostProfile).Assembly, profile.HostAssembly);
    }

    [Fact]
    public void Options_bind_the_registry_root_and_pipe_from_the_profile()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("Registry:PublishPolicy", "manual")])
            .Build();
        var services = new ServiceCollection();
        McpServerHost.ConfigureOptions(services, configuration, Civil3dHostProfile.Instance);
        using var provider = services.BuildServiceProvider();

        var bridge = provider.GetRequiredService<IOptions<BridgeOptions>>().Value;
        var registry = provider.GetRequiredService<IOptions<RegistryOptions>>().Value;

        // HostVersion is seeded from the profile's default, so the pipe is Civil's even with no configuration at all
        Assert.Equal("hpcivil3d-mcp-2026", bridge.PipeName);
        Assert.Contains(Path.Combine("HPCivil3d", "McpServer"), registry.LibraryPath);
        Assert.Contains(Path.Combine("HPCivil3d", "McpServer"), registry.DbPath);
        Assert.DoesNotContain("HPAutoCad", registry.LibraryPath);
        Assert.DoesNotContain("HPRebar", registry.LibraryPath);
    }

    [Fact]
    public void Tool_surface_is_the_four_civil_core_tools_plus_the_registry_and_nothing_from_revit_or_autocad()
    {
        using var host = McpServerHost.CreateBuilder([], Civil3dHostProfile.Instance).Build();

        var names = host.Services.GetServices<McpServerTool>().Select(t => t.ProtocolTool.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

        Assert.Equal(12, names.Length);
        Assert.Contains("execute_civil3d_code", names);
        Assert.Contains("get_civil3d_context", names);
        Assert.Contains("inspect_type", names);
        Assert.Contains("cancel_execution", names);
        Assert.All(RegistryTools, tool => Assert.Contains(tool, names));
        Assert.DoesNotContain(names, n => n.Contains("revit", StringComparison.OrdinalIgnoreCase) || n.Contains("autocad", StringComparison.OrdinalIgnoreCase));

        var execute = host.Services.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "execute_civil3d_code").ProtocolTool;
        Assert.True(execute.Annotations?.DestructiveHint);
        Assert.Contains("tr.AddNewlyCreatedDBObject", execute.Description);
        Assert.Contains("civil (CivilDocument", execute.Description);
        Assert.Contains("insunitsMismatch", execute.Description);
        Assert.Contains("Feet", execute.Description);
        Assert.Contains("Rebuild", execute.Description);
        Assert.Contains("HPC3DMCPBRIDGE", execute.Description);
        // Same budget as the AutoCAD host: the two-layer unit rule and the Civil deny list fit; anything more belongs in the prompts.
        Assert.True(execute.Description!.Length <= 1800, $"description is {execute.Description.Length} chars");

        var context = host.Services.GetServices<McpServerTool>().Single(t => t.ProtocolTool.Name == "get_civil3d_context").ProtocolTool;
        Assert.True(context.Annotations?.ReadOnlyHint);
        Assert.Contains("insunitsMismatch", context.Description);
        Assert.Contains("alignmentCount", context.Description);
    }

    [Fact]
    public void Resources_and_prompts_use_the_civil3d_scheme_and_names()
    {
        using var host = McpServerHost.CreateBuilder([], Civil3dHostProfile.Instance).Build();

        var resources = host.Services.GetServices<McpServerResource>().Select(r => r.ProtocolResourceTemplate.UriTemplate).ToArray();
        var prompts = host.Services.GetServices<McpServerPrompt>().Select(p => p.ProtocolPrompt.Name).ToArray();

        Assert.Contains("civil3d://document/info", resources);
        Assert.Contains("civil3d://selection", resources);
        Assert.DoesNotContain(resources, r => r.StartsWith("revit://", StringComparison.Ordinal) || r.StartsWith("autocad://", StringComparison.Ordinal));
        Assert.Contains("civil3d_query_template", prompts);
        Assert.Contains("civil3d_modify_template", prompts);
        Assert.Contains("toolify_run", prompts);
        Assert.DoesNotContain(prompts, p => p.StartsWith("revit_", StringComparison.Ordinal) || p.StartsWith("autocad_", StringComparison.Ordinal));
    }
}
