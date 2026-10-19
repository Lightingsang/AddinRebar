namespace HPBim.Client.Tests.ScriptGenerators;

using HPBim.Client.ScriptGenerators;
using HPBim.Core.Tests.Fixtures;
using Xunit;

public sealed class StageGeneratorTests
{
    private readonly HPBim.Core.Models.BimModel _model = NhaPho5x18mFixture.CreateModel();

    [Fact]
    public void Stage1LevelGenerator_GeneratesCorrectPayload()
    {
        var generator = new Stage1LevelGenerator();
        var payload = generator.Generate(_model);

        Assert.Equal(1, payload.StageIndex);
        Assert.Equal("Levels", payload.StageName);
        Assert.NotNull(payload.Args);

        var req = payload.ToExecuteRequest(dryRun: true);
        Assert.True(req.DryRun);
        Assert.Contains("Stage 1", req.Label);
        Assert.NotNull(req.Args);
    }

    [Fact]
    public void Stage2GridGenerator_GeneratesCorrectPayload()
    {
        var generator = new Stage2GridGenerator();
        var payload = generator.Generate(_model);

        Assert.Equal(2, payload.StageIndex);
        Assert.Equal("Grids", payload.StageName);
        Assert.NotNull(payload.Args);
    }

    [Fact]
    public void Stage3StructureGenerator_GeneratesColumnsBeamsAndSlabs()
    {
        var generator = new Stage3StructureGenerator();
        var payload = generator.Generate(_model);

        Assert.Equal(3, payload.StageIndex);
        Assert.Equal("Structure", payload.StageName);
        Assert.NotNull(payload.Args);
    }

    [Fact]
    public void Stage4WallGenerator_GeneratesWallsPayload()
    {
        var generator = new Stage4WallGenerator();
        var payload = generator.Generate(_model);

        Assert.Equal(4, payload.StageIndex);
        Assert.Equal("Walls", payload.StageName);
        Assert.NotNull(payload.Args);
    }

    [Fact]
    public void Stage5OpeningGenerator_GeneratesOpeningsPayload()
    {
        var generator = new Stage5OpeningGenerator();
        var payload = generator.Generate(_model);

        Assert.Equal(5, payload.StageIndex);
        Assert.Equal("Openings", payload.StageName);
        Assert.NotNull(payload.Args);
    }

    [Fact]
    public void Stage6RoomGenerator_GeneratesRoomsPayload()
    {
        var generator = new Stage6RoomGenerator();
        var payload = generator.Generate(_model);

        Assert.Equal(6, payload.StageIndex);
        Assert.Equal("Rooms", payload.StageName);
        Assert.NotNull(payload.Args);
    }

    [Fact]
    public void Stage7MepGenerator_GeneratesFixturesAndPipes()
    {
        var generator = new Stage7MepGenerator();
        var payload = generator.Generate(_model);

        Assert.Equal(7, payload.StageIndex);
        Assert.Equal("MEP", payload.StageName);
        Assert.NotNull(payload.Args);
    }

    [Fact]
    public void Stage8RebarGenerator_GeneratesRebarsPayload()
    {
        var generator = new Stage8RebarGenerator();
        var payload = generator.Generate(_model);

        Assert.Equal(8, payload.StageIndex);
        Assert.Equal("Rebars", payload.StageName);
        Assert.NotNull(payload.Args);
    }

    [Fact]
    public void CompositeBimGenerator_GeneratesCombinedPayload()
    {
        var generator = new CompositeBimGenerator();
        var payload = generator.Generate(_model);

        Assert.Equal(0, payload.StageIndex);
        Assert.Equal("CompositeBimPipeline", payload.StageName);
        Assert.NotNull(payload.Args);

        var req = payload.ToExecuteRequest(dryRun: true);
        Assert.True(req.DryRun);
        Assert.NotNull(req.Args);
    }

    [Fact]
    public void CompositeBimGenerator_IncludesAll8StagesInScriptAndArgs()
    {
        var generator = new CompositeBimGenerator();
        var payload = generator.Generate(_model);

        Assert.Contains("// --- STAGE 1: LEVELS ---", payload.ScriptCode);
        Assert.Contains("// --- STAGE 2: GRIDS ---", payload.ScriptCode);
        Assert.Contains("// --- STAGE 3A: COLUMNS ---", payload.ScriptCode);
        Assert.Contains("// --- STAGE 3B: BEAMS ---", payload.ScriptCode);
        Assert.Contains("// --- STAGE 3C: SLABS ---", payload.ScriptCode);
        Assert.Contains("// --- STAGE 4: WALLS ---", payload.ScriptCode);
        Assert.Contains("// --- STAGE 5: OPENINGS ---", payload.ScriptCode);
        Assert.Contains("// --- STAGE 6: ROOMS ---", payload.ScriptCode);
        Assert.Contains("// --- STAGE 7: MEP ---", payload.ScriptCode);
        Assert.Contains("// --- STAGE 8: REBARS ---", payload.ScriptCode);

        // Verify script return includes all 8 stages
        Assert.Contains("fixtures = countFixtures", payload.ScriptCode);
        Assert.Contains("pipes = countPipes", payload.ScriptCode);
        Assert.Contains("rebars = countRebars", payload.ScriptCode);

        // Verify combined args contain all entities
        var dict = (IDictionary<string, object>)payload.Args!;
        Assert.True(dict.ContainsKey("levels"));
        Assert.True(dict.ContainsKey("grids"));
        Assert.True(dict.ContainsKey("columns"));
        Assert.True(dict.ContainsKey("beams"));
        Assert.True(dict.ContainsKey("slabs"));
        Assert.True(dict.ContainsKey("walls"));
        Assert.True(dict.ContainsKey("openings"));
        Assert.True(dict.ContainsKey("rooms"));
        Assert.True(dict.ContainsKey("fixtures"));
        Assert.True(dict.ContainsKey("pipes"));
        Assert.True(dict.ContainsKey("rebars"));
    }
}
