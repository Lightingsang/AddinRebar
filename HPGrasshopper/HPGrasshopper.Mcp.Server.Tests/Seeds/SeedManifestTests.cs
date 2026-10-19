namespace HPGrasshopper.Mcp.Server.Tests.Seeds;

using System.Linq;
using HPGrasshopper.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Registry;
using Xunit;

public sealed class SeedManifestTests
{
    private static readonly string[] ExpectedSeedNames =
    [
        "create_levels_grids",
        "generate_columns_beams",
        "create_floors_slabs",
        "generate_rebar_curves",
        "generate_stirrup_distribution",
        "facade_panelization",
        "curtain_grid_layout",
        "create_wall_openings",
        "directshape_from_geometry",
        "query_revit_geometry",
        "evaluate_solar_radiation",
        "export_model_to_rhino"
    ];

    [Fact]
    public void SeedInstaller_ListSeeds_ReturnsAllTwelveSeeds()
    {
        var seeds = SeedInstaller.ListSeeds(typeof(GrasshopperHostProfile).Assembly);
        Assert.Equal(12, seeds.Count);

        var names = seeds.Select(s => s.Name).ToHashSet();
        foreach (var expected in ExpectedSeedNames)
        {
            Assert.Contains(expected, names);
        }
    }

    [Fact]
    public void SeedContent_ChecksumsAreDeterministicAndNonEmpty()
    {
        var seeds = SeedInstaller.LoadSeeds(typeof(GrasshopperHostProfile).Assembly);
        Assert.Equal(12, seeds.Count);

        foreach (var seed in seeds)
        {
            Assert.False(string.IsNullOrWhiteSpace(seed.Checksum));
            Assert.Equal(64, seed.Checksum.Length);
        }
    }
}
