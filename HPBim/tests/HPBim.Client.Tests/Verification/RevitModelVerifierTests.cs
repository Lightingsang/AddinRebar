namespace HPBim.Client.Tests.Verification;

using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Client.Tests.Fakes;
using HPBim.Client.Verification;
using HPBim.Core.Models;
using HPBim.Core.Tests.Fixtures;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class RevitModelVerifierTests
{
    private readonly BimModel _model = NhaPho5x18mFixture.CreateModel();

    [Fact]
    public async Task VerifyAsync_WhenCountsMatchExactly_ReturnsIsMatchTrue()
    {
        int expDoors = _model.Walls.SelectMany(w => w.Openings).Count(o => o.Kind == OpeningKind.Door);
        int expWindows = _model.Walls.SelectMany(w => w.Openings).Count(o => o.Kind == OpeningKind.Window);

        var matchingData = new
        {
            levels = _model.Levels.Count,
            grids = _model.Grids.Count,
            columns = _model.Columns.Count,
            walls = _model.Walls.Count,
            doors = expDoors,
            windows = expWindows,
            rooms = _model.Rooms.Count,
            beams = _model.Beams.Count,
            slabs = _model.Slabs.Count,
            pipes = _model.Pipes.Count,
            rebars = _model.Rebars.Count
        };

        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = req => new ExecuteResult
            {
                IsError = false,
                Value = BridgeJson.ToElement(matchingData)
            }
        };

        var verifier = new RevitModelVerifier(fakeClient);
        var report = await verifier.VerifyAsync(_model);

        Assert.True(report.IsMatch);
        Assert.Empty(report.Discrepancies);
        Assert.Equal(_model.Levels.Count, report.ActualLevels);
        Assert.Equal(_model.Columns.Count, report.ActualColumns);
    }

    [Fact]
    public async Task VerifyAsync_WhenDiscrepanciesExist_ReturnsIsMatchFalseWithDetails()
    {
        var mismatchData = new
        {
            levels = _model.Levels.Count,
            grids = _model.Grids.Count,
            columns = _model.Columns.Count - 2, // 2 missing columns
            walls = _model.Walls.Count + 1,     // 1 extra wall
            doors = 0,
            windows = 0,
            rooms = _model.Rooms.Count,
            beams = _model.Beams.Count,
            slabs = _model.Slabs.Count,
            pipes = _model.Pipes.Count,
            rebars = _model.Rebars.Count
        };

        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = req => new ExecuteResult
            {
                IsError = false,
                Value = BridgeJson.ToElement(mismatchData)
            }
        };

        var verifier = new RevitModelVerifier(fakeClient);
        var report = await verifier.VerifyAsync(_model);

        Assert.False(report.IsMatch);
        Assert.NotEmpty(report.Discrepancies);
        Assert.Contains(report.Discrepancies, d => d.Contains("Columns"));
        Assert.Contains(report.Discrepancies, d => d.Contains("Walls"));
    }

    [Fact]
    public async Task VerifyAsync_WhenScriptExecutionFails_ThrowsRevitPipeException()
    {
        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = req => ExecuteResult.Failure("Query timeout")
        };

        var verifier = new RevitModelVerifier(fakeClient);

        await Assert.ThrowsAsync<RevitPipeException>(() => verifier.VerifyAsync(_model));
    }
}
