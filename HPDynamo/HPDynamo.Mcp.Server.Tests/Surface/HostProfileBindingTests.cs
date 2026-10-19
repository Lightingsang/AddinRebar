namespace HPDynamo.Mcp.Server.Tests.Surface;

using System;
using System.Collections.Generic;
using System.IO;
using HPDynamo.Mcp.Server.Hosts;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Server.Bootstrap;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Registry.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

public sealed class HostProfileBindingTests
{
    [Fact]
    public void DynamoHostProfile_DeclaresRequiredMetadata()
    {
        var profile = DynamoHostProfile.Instance;

        Assert.Equal("dynamo", profile.HostId);
        Assert.Equal("Dynamo", profile.DisplayName);
        Assert.Equal("HPDynamo MCP", profile.ServerName);
        Assert.Equal("hpdynamo-mcp-2026", profile.PipeName(2026));
        Assert.Equal("dynamo.execute", profile.Method("execute"));
        Assert.Equal(2026, profile.DefaultVersion);
        Assert.Equal(HostScriptContracts.DynamoHeavyMaxTimeoutSeconds, profile.MaxTimeoutSeconds);
        Assert.Equal(HostScriptContracts.DynamoImports, profile.ScriptImports);
        Assert.Equal("HPDynamo.Mcp.Server.exe", profile.CliExecutable);
        Assert.Same(typeof(DynamoHostProfile).Assembly, profile.HostAssembly);
    }

    [Fact]
    public void ConfigureOptions_BindsPipeAndRegistryPaths()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Registry:PublishPolicy", "manual"),
                new KeyValuePair<string, string?>("Bridge:HostVersion", "2026")
            })
            .Build();

        var services = new ServiceCollection();
        McpServerHost.ConfigureOptions(services, configuration, DynamoHostProfile.Instance);
        using var provider = services.BuildServiceProvider();

        var bridge = provider.GetRequiredService<IOptions<BridgeOptions>>().Value;
        var registry = provider.GetRequiredService<IOptions<RegistryOptions>>().Value;

        Assert.Equal("hpdynamo-mcp-2026", bridge.PipeName);
        Assert.Contains(Path.Combine("HPDynamo", "McpServer"), registry.LibraryPath);
        Assert.Contains(Path.Combine("HPDynamo", "McpServer"), registry.DbPath);
        Assert.DoesNotContain("HPRebar", registry.LibraryPath);
        Assert.DoesNotContain("HPAutoCad", registry.LibraryPath);
    }

    [Fact]
    public void DynamoHostProfile_Categories_CoverAllRequiredDomains()
    {
        var profile = DynamoHostProfile.Instance;
        string[] requiredCategories =
        [
            "Nodes", "Wire", "Workspace", "Geometry", "Revit",
            "Python", "Package", "Generic", "CodeBlock", "Data",
            "Export", "Graph", "Library"
        ];

        foreach (var category in requiredCategories)
        {
            Assert.Contains(category, profile.Categories);
        }
    }
}
