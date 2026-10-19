namespace HPGrasshopper.Mcp.Server.Tests.Protocol;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HPGrasshopper.Mcp.Server.Hosts;
using HPGrasshopper.Mcp.Server.Tests.Fixtures;
using HPGrasshopper.Mcp.Server.Tools;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;
using HPRebar.Mcp.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using Xunit;

public sealed class ProtocolAndStdioOptionTests
{
    [Fact]
    public void Profile_DeclaresCorrectGrasshopperTokens()
    {
        var profile = GrasshopperHostProfile.Instance;

        Assert.Equal("grasshopper", profile.HostId);
        Assert.Equal("Grasshopper", profile.DisplayName);
        Assert.Equal("HPGrasshopper MCP", profile.ServerName);
        Assert.Equal("hpgrasshopper-mcp-2026", profile.PipeName(2026));
        Assert.Equal("grasshopper.execute", profile.Method("execute"));
        Assert.Equal("execute_grasshopper_code", profile.ExecuteToolName);
        Assert.Equal("get_grasshopper_context", profile.ContextToolName);
        Assert.Equal("HPGrasshopper", profile.ProductFolder);
        Assert.Equal("HPGRASSHOPPER_MCP_", profile.EnvPrefix);
        Assert.Equal(2026, profile.DefaultVersion);
        Assert.Equal([2026], profile.ValidVersions);
        Assert.Equal(600, profile.MaxTimeoutSeconds);
        Assert.Equal(HostScriptContracts.GrasshopperImports, profile.ScriptImports);
        Assert.Same(typeof(GrasshopperHostProfile).Assembly, profile.HostAssembly);
        Assert.Equal("HPGrasshopper.Mcp.Server.exe", profile.CliExecutable);

        Assert.Contains("hpgrasshopper-mcp-2026", profile.BridgeNotConnectedHint);
        Assert.Contains("Grasshopper", profile.TimeoutSemanticsHint);
    }

    [Fact]
    public void Options_BindRegistryRootAndPipeFromProfile()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var services = new ServiceCollection();
        McpServerHost.ConfigureOptions(services, configuration, GrasshopperHostProfile.Instance);
        using var provider = services.BuildServiceProvider();

        var bridgeOptions = provider.GetRequiredService<IOptions<BridgeOptions>>().Value;
        var registryOptions = provider.GetRequiredService<IOptions<RegistryOptions>>().Value;

        Assert.Equal(2026, bridgeOptions.HostVersion);
        Assert.Equal("hpgrasshopper-mcp-2026", bridgeOptions.PipeName);
        Assert.Contains(Path.Combine("HPGrasshopper", "McpServer"), registryOptions.LibraryPath);
        Assert.Contains(Path.Combine("HPGrasshopper", "McpServer"), registryOptions.DbPath);
    }

    [Fact]
    public void InvalidHostVersion_IsRefusedByValidation()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Bridge:HostVersion"] = "2020" })
            .Build();
        var services = new ServiceCollection();
        McpServerHost.ConfigureOptions(services, configuration, GrasshopperHostProfile.Instance);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<BridgeOptions>>().Value);
    }

    [Fact]
    public async Task CoreTools_SendCorrectJsonRpcMethodsOverPipe()
    {
        await using var fixture = new GrasshopperRegistryFixture();
        var formatter = new ResultFormatter();
        var options = Microsoft.Extensions.Options.Options.Create(new BridgeOptions { HostVersion = 2026 });
        var executeService = new ExecuteCodeService(fixture.Bridge, formatter, options);
        var execTool = new ExecuteGrasshopperCodeTool(executeService);

        var result = await execTool.ExecuteAsync("return 42;", "auto", false, 30);
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Single(result.Content);
    }
}
