namespace HPBim.Client.Tests.Pipeline;

using System.Collections.Generic;
using System.Threading.Tasks;
using HPBim.Client.Pipeline;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Tests.Fixtures;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class BimPipelineEngineTests
{
    private readonly HPBim.Core.Models.BimModel _model = NhaPho5x18mFixture.CreateModel();

    [Fact]
    public async Task ExecuteAsync_SequentialStages_RunsAll8StagesInOrder()
    {
        var fakeClient = new FakeRevitPipeClient();
        var engine = new BimPipelineEngine(fakeClient);
        var completedStages = new List<int>();
        engine.StageCompleted += res => completedStages.Add(res.StageIndex);

        var options = new BimPipelineOptions { DryRun = false };
        var result = await engine.ExecuteAsync(_model, options);

        Assert.True(result.Success);
        Assert.Equal(8, result.StageResults.Count);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }, completedStages);
        Assert.Equal(8, fakeClient.ExecutedRequests.Count);
    }

    [Fact]
    public async Task ExecuteAsync_AtomicDryRun_RunsCompositeInSingleCall()
    {
        var fakeClient = new FakeRevitPipeClient();
        var engine = new BimPipelineEngine(fakeClient);

        var options = new BimPipelineOptions { DryRun = true, AtomicDryRun = true };
        var result = await engine.ExecuteAsync(_model, options);

        Assert.True(result.Success);
        Assert.True(result.RolledBack);
        Assert.Single(result.StageResults);
        Assert.Single(fakeClient.ExecutedRequests);
        Assert.True(fakeClient.ExecutedRequests[0].DryRun);
    }

    [Fact]
    public async Task ExecuteAsync_WithSelectedStages_RunsOnlyRequestedStages()
    {
        var fakeClient = new FakeRevitPipeClient();
        var engine = new BimPipelineEngine(fakeClient);

        var options = new BimPipelineOptions
        {
            DryRun = false,
            SelectedStages = new[] { 1, 2, 4 }
        };
        var result = await engine.ExecuteAsync(_model, options);

        Assert.True(result.Success);
        Assert.Equal(3, result.StageResults.Count);
        Assert.Equal(1, result.StageResults[0].StageIndex);
        Assert.Equal(2, result.StageResults[1].StageIndex);
        Assert.Equal(4, result.StageResults[2].StageIndex);
        Assert.Equal(3, fakeClient.ExecutedRequests.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStageFails_HaltsDownstreamStages()
    {
        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = req =>
            {
                if (req.Label?.Contains("Stage 3") == true)
                {
                    return ExecuteResult.Failure("Simulated structure placement failure");
                }
                return new ExecuteResult { IsError = false, Changed = new ChangedCounts(1, 0, 0) };
            }
        };

        var engine = new BimPipelineEngine(fakeClient);
        var options = new BimPipelineOptions { DryRun = false };
        var result = await engine.ExecuteAsync(_model, options);

        Assert.False(result.Success);
        Assert.Contains("Stage 3", result.ErrorMessage);
        Assert.Equal(3, result.StageResults.Count);
        Assert.Equal(3, fakeClient.ExecutedRequests.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WhenClientNotConnected_ConnectsAutomatically()
    {
        var fakeClient = new FakeRevitPipeClient { IsConnected = false };
        var engine = new BimPipelineEngine(fakeClient);

        var options = new BimPipelineOptions { DryRun = false, SelectedStages = new[] { 1 } };
        var result = await engine.ExecuteAsync(_model, options);

        Assert.True(result.Success);
        Assert.True(fakeClient.IsConnected);
    }
}
