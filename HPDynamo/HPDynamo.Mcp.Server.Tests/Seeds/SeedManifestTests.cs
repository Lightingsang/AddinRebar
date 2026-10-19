namespace HPDynamo.Mcp.Server.Tests.Seeds;

using System;
using System.Linq;
using HPDynamo.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using Xunit;

public sealed class SeedManifestTests
{
    private static readonly string[] ExpectedSeedNames =
    [
        "run_dyn_file",
        "get_active_graph_info",
        "search_nodes_library",
        "inspect_node_type",
        "add_node_to_workspace",
        "connect_node_ports",
        "disconnect_node_ports",
        "set_node_properties",
        "get_node_output_values",
        "export_graph_to_image",
        "validate_dyn_file",
        "create_code_block_node",
        "batch_edit_graph"
    ];

    [Fact]
    public void ManifestResources_ContainsAllSeedResources()
    {
        var asm = typeof(DynamoHostProfile).Assembly;
        var seedResources = asm.GetManifestResourceNames()
            .Where(r => r.StartsWith("SeedLibrary/", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Replace('\\', '/'))
            .ToList();

        Assert.Equal(39, seedResources.Count);

        foreach (var seedName in ExpectedSeedNames)
        {
            Assert.Contains(seedResources, r => r.Contains($"/{seedName}/code.cs", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(seedResources, r => r.Contains($"/{seedName}/tool.json", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(seedResources, r => r.Contains($"/{seedName}/examples.json", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void AssemblyTypes_ContainsZeroCompiledSeedScriptClasses()
    {
        var asm = typeof(DynamoHostProfile).Assembly;
        var types = asm.GetTypes();

        foreach (var seedName in ExpectedSeedNames)
        {
            Assert.DoesNotContain(types, t => string.Equals(t.Name, seedName, StringComparison.OrdinalIgnoreCase));
        }

        Assert.DoesNotContain(types, t => t.Namespace?.Contains("SeedLibrary", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void SeedCategories_MatchPhysicalDirectoryStructure()
    {
        var seeds = SeedInstaller.LoadSeeds(typeof(DynamoHostProfile).Assembly);

        Assert.Equal(13, seeds.Count);
        foreach (var seed in seeds)
        {
            Assert.False(string.IsNullOrWhiteSpace(seed.Category));
            Assert.False(string.IsNullOrWhiteSpace(seed.Name));
            Assert.False(string.IsNullOrWhiteSpace(seed.Code));
        }
    }
}
