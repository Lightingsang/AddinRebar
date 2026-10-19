namespace HPGrasshopper.McpBridge.Tests.Fixtures;

using System;
using System.IO;
using GH_IO.Serialization;
using Xunit;

public sealed class FixtureDefinitionTests : IClassFixture<IsolatedFixtureDirectory>
{
    private readonly string _fixturesDir;

    public FixtureDefinitionTests(IsolatedFixtureDirectory fixture)
    {
        _fixturesDir = fixture.DirectoryPath;
    }


    [Fact]
    public void FixturesInfrastructure_AllGoldenFiles_ExistOnDisk()
    {
        var expectedFiles = new[]
        {
            "MathSlider.ghx", "MathSlider.gh",
            "CurveGenerator.ghx", "CurveGenerator.gh",
            "RevitDirectShapeSyncMock.ghx", "RevitDirectShapeSyncMock.gh"
        };

        foreach (var file in expectedFiles)
        {
            var fullPath = Path.Combine(_fixturesDir, file);
            Assert.True(File.Exists(fullPath), $"Expected fixture file {file} must exist at {fullPath}");
            var info = new FileInfo(fullPath);
            Assert.True(info.Length > 0, $"Fixture file {file} must not be empty");
        }
    }

    [Fact]
    public void MathSliderFixture_Ghx_LoadsCleanlyFromArchive()
    {
        var path = Path.Combine(_fixturesDir, "MathSlider.ghx");
        var archive = FixtureBuilder.LoadArchive(path);
        Assert.NotNull(archive);

        var inspection = FixtureBuilder.InspectDefinition(archive);
        Assert.Equal("MathSlider", inspection.Name);
        Assert.Equal(4, inspection.ObjectCount);
    }

    [Fact]
    public void MathSliderFixture_Gh_Binary_LoadsCleanlyFromArchive()
    {
        var path = Path.Combine(_fixturesDir, "MathSlider.gh");
        var archive = FixtureBuilder.LoadArchive(path);
        Assert.NotNull(archive);

        var inspection = FixtureBuilder.InspectDefinition(archive);
        Assert.Equal("MathSlider", inspection.Name);
        Assert.Equal(4, inspection.ObjectCount);
    }

    [Fact]
    public void MathSliderFixture_ContainsDefinitionChunkAndProperties()
    {
        var path = Path.Combine(_fixturesDir, "MathSlider.ghx");
        var archive = FixtureBuilder.LoadArchive(path);
        var def = FixtureBuilder.FindDefinitionChunk(archive);
        Assert.NotNull(def);

        var props = def!.FindChunk("DefinitionProperties") as GH_Chunk;
        Assert.NotNull(props);
        Assert.Equal("MathSlider", props!.GetString("Name"));
        Assert.NotEmpty(props.GetString("Description"));
    }

    [Fact]
    public void MathSliderFixture_ContainsInputParametersAAndB()
    {
        var path = Path.Combine(_fixturesDir, "MathSlider.ghx");
        var archive = FixtureBuilder.LoadArchive(path);
        var inspection = FixtureBuilder.InspectDefinition(archive);

        Assert.Contains("A", inspection.NickNames);
        Assert.Contains("B", inspection.NickNames);
        Assert.Contains("Result", inspection.NickNames);
    }

    [Fact]
    public void CurveGeneratorFixture_Ghx_LoadsCleanlyFromArchive()
    {
        var path = Path.Combine(_fixturesDir, "CurveGenerator.ghx");
        var archive = FixtureBuilder.LoadArchive(path);
        var inspection = FixtureBuilder.InspectDefinition(archive);

        Assert.Equal("CurveGenerator", inspection.Name);
        Assert.Equal(5, inspection.ObjectCount);
    }

    [Fact]
    public void CurveGeneratorFixture_Gh_Binary_LoadsCleanlyFromArchive()
    {
        var path = Path.Combine(_fixturesDir, "CurveGenerator.gh");
        var archive = FixtureBuilder.LoadArchive(path);
        var inspection = FixtureBuilder.InspectDefinition(archive);

        Assert.Equal("CurveGenerator", inspection.Name);
        Assert.Equal(5, inspection.ObjectCount);
    }

    [Fact]
    public void CurveGeneratorFixture_ContainsExpectedInputAndOutputParameters()
    {
        var path = Path.Combine(_fixturesDir, "CurveGenerator.ghx");
        var archive = FixtureBuilder.LoadArchive(path);
        var inspection = FixtureBuilder.InspectDefinition(archive);

        Assert.Contains("Radius", inspection.NickNames);
        Assert.Contains("PointCount", inspection.NickNames);
        Assert.Contains("Closed", inspection.NickNames);
        Assert.Contains("GeneratedCurve", inspection.NickNames);
        Assert.Contains("CurveLength", inspection.NickNames);
    }

    [Fact]
    public void RevitDirectShapeSyncMockFixture_Ghx_LoadsCleanlyFromArchive()
    {
        var path = Path.Combine(_fixturesDir, "RevitDirectShapeSyncMock.ghx");
        var archive = FixtureBuilder.LoadArchive(path);
        var inspection = FixtureBuilder.InspectDefinition(archive);

        Assert.Equal("RevitDirectShapeSyncMock", inspection.Name);
        Assert.Equal(7, inspection.ObjectCount);
        Assert.Contains("CategoryName", inspection.NickNames);
        Assert.Contains("BoxWidth", inspection.NickNames);
        Assert.Contains("BoxHeight", inspection.NickNames);
        Assert.Contains("SolidSummary", inspection.NickNames);
        Assert.Contains("Volume", inspection.NickNames);
        Assert.Contains("SyncStatus", inspection.NickNames);
    }

    [Fact]
    public void RevitDirectShapeSyncMockFixture_Gh_Binary_RoundTripSerializationMaintainsIntegrity()
    {
        var path = Path.Combine(_fixturesDir, "RevitDirectShapeSyncMock.gh");
        var archive = FixtureBuilder.LoadArchive(path);
        var originalInspection = FixtureBuilder.InspectDefinition(archive);

        var bytes = archive.Serialize_Binary();
        var roundTripArchive = new GH_Archive();
        var success = roundTripArchive.Deserialize_Binary(bytes);
        Assert.True(success);

        var roundTripInspection = FixtureBuilder.InspectDefinition(roundTripArchive);
        Assert.Equal(originalInspection.Name, roundTripInspection.Name);
        Assert.Equal(originalInspection.ObjectCount, roundTripInspection.ObjectCount);
        Assert.Equal(originalInspection.NickNames.Count, roundTripInspection.NickNames.Count);
    }

    [Fact]
    public void ContinuousBeamRenumbering_Ghx_LoadsCleanlyFromArchive()
    {
        var path = Path.Combine(_fixturesDir, "ContinuousBeamRenumbering.ghx");
        var archive = FixtureBuilder.LoadArchive(path);
        var inspection = FixtureBuilder.InspectDefinition(archive);

        Assert.Equal("ContinuousBeamRenumbering", inspection.Name);
        Assert.Equal(7, inspection.ObjectCount);
        Assert.Contains("ViewName", inspection.NickNames);
        Assert.Contains("FloorSuffix", inspection.NickNames);
        Assert.Contains("MinWidthMm", inspection.NickNames);
        Assert.Contains("ToleranceMm", inspection.NickNames);
        Assert.Contains("DryRun", inspection.NickNames);
        Assert.Contains("RenumberScript", inspection.NickNames);
        Assert.Contains("RenumberSummary", inspection.NickNames);
    }

    [Fact]
    public void ContinuousBeamRenumbering_Gh_Binary_LoadsCleanlyFromArchive()
    {
        var path = Path.Combine(_fixturesDir, "ContinuousBeamRenumbering.gh");
        var archive = FixtureBuilder.LoadArchive(path);
        var inspection = FixtureBuilder.InspectDefinition(archive);

        Assert.Equal("ContinuousBeamRenumbering", inspection.Name);
        Assert.Equal(7, inspection.ObjectCount);
    }

    [Fact]
    public void ContinuousBeamRenumbering_RootArchive_HasDefinitionChunkAsDirectChild()
    {
        var path = Path.Combine(_fixturesDir, "ContinuousBeamRenumbering.ghx");
        var archive = FixtureBuilder.LoadArchive(path);
        var root = archive.GetRootNode;
        Assert.NotNull(root);

        // Crucial: Definition must be a direct child of root, NOT wrapped inside a "Root" chunk!
        var directDef = root!.FindChunk("Definition");
        Assert.NotNull(directDef);
    }
}


