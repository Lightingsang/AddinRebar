using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     The profile decides the registry root and the pipe name, and configuration may override them.
///     Binding goes through the real options pipeline (Configure → Bind → PostConfigure) with a non-empty
///     section, because the binder writes computed getter values back into their setters: an ordering
///     mistake pins the Revit defaults before the profile is applied.
/// </summary>
public sealed class ProfileOptionsBindingTests
{
    private static readonly HostProfile Autocad = new HostProfile
    {
        HostId = "autocad", DisplayName = "AutoCAD", ServerName = "test", ProductFolder = "HPAutoCadTest", EnvPrefix = "X_",
        DefaultVersion = 2026, ValidVersions = new[] { 2026 }, MethodPrefix = JsonRpcMethods.AutocadPrefix,
        ExecuteToolName = "execute_autocad_code", ContextToolName = "get_autocad_context", ResourceScheme = "autocad",
        Categories = new[] { "Generic" }, CoreToolNames = new[] { "execute_autocad_code" }, ScriptImports = Array.Empty<string>(),
        ScriptContractSummary = "test", HostAssembly = typeof(ProfileOptionsBindingTests).Assembly,
    };

    private static (BridgeOptions Bridge, RegistryOptions Registry) Bind(IHostProfile profile, params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(s => new KeyValuePair<string, string?>(s.Key, s.Value)))
            .Build();

        var services = new ServiceCollection();
        McpServerHost.ConfigureOptions(services, configuration, profile);
        using var provider = services.BuildServiceProvider();

        return (provider.GetRequiredService<IOptions<BridgeOptions>>().Value, provider.GetRequiredService<IOptions<RegistryOptions>>().Value);
    }

    [Fact]
    public void Non_empty_sections_still_take_the_registry_root_and_pipe_from_the_profile()
    {
        var (bridge, registry) = Bind(Autocad, ("Registry:PublishPolicy", "manual"), ("Bridge:ConnectTimeoutMs", "5000"));

        Assert.Contains(Path.Combine("HPAutoCadTest", "McpServer"), registry.LibraryPath);
        Assert.Contains(Path.Combine("HPAutoCadTest", "McpServer"), registry.DbPath);
        Assert.Equal("hpautocad-mcp-2026", bridge.PipeName);
        Assert.Equal(5000, bridge.ConnectTimeoutMs);
    }

    [Fact]
    public void Explicit_configuration_wins_over_the_profile_defaults()
    {
        var (bridge, registry) = Bind(Autocad, ("Registry:LibraryPath", @"C:\lib"), ("Bridge:PipeName", "custom-pipe"), ("Bridge:HostVersion", "2026"));

        Assert.Equal(@"C:\lib", registry.LibraryPath);
        Assert.Contains(Path.Combine("HPAutoCadTest", "McpServer"), registry.DbPath);
        Assert.Equal("custom-pipe", bridge.PipeName);
    }

    [Fact]
    public void Revit_profile_keeps_the_original_paths_and_the_RevitVersion_alias()
    {
        var (bridge, registry) = Bind(HostProfile.Revit, ("Bridge:RevitVersion", "2025"), ("Registry:SearchTopK", "7"));

        Assert.Contains(Path.Combine("HPRebar", "McpServer", "tools-library"), registry.LibraryPath);
        Assert.Equal("hprebar-mcp-r2025", bridge.PipeName);
        Assert.Equal(2025, bridge.HostVersion);
        Assert.Equal(7, registry.SearchTopK);
    }

    [Fact]
    public void A_configured_host_id_cannot_redirect_the_exe_to_another_pipe()
    {
        var (bridge, _) = Bind(Autocad, ("Bridge:HostId", "revit"));

        Assert.Equal("hpautocad-mcp-2026", bridge.PipeName);
    }
}
