namespace HPBim.Client.Tests.Pipeline;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPBim.Client.Pipeline;
using HPBim.Client.Tests.Fakes;
using HPBim.Core.Models;
using HPBim.Core.Tests.Fixtures;
using HPRebar.Mcp.Contracts.Messages;
using Xunit;

public sealed class BimPipelineEngineAdversarialTests
{
    private readonly BimModel _model = NhaPho5x18mFixture.CreateModel();

    [Fact]
    public async Task ExecuteAsync_WhenStage1Fails_HaltsImmediatelyAndExecutesOnlyStage1()
    {
        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = req => ExecuteResult.Failure("Level elevation conflict")
        };

        var engine = new BimPipelineEngine(fakeClient);
        var result = await engine.ExecuteAsync(_model);

        Assert.False(result.Success);
        Assert.Single(fakeClient.ExecutedRequests);
        Assert.Single(result.StageResults);
        Assert.Equal(1, result.StageResults[0].StageIndex);
        Assert.Contains("Stage 1 (Levels) failed", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStage5Fails_HaltsDownstreamStages6Through8()
    {
        var executedLabels = new List<string>();
        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = req =>
            {
                executedLabels.Add(req.Label ?? string.Empty);
                if (req.Label?.Contains("Stage 5") == true)
                {
                    return ExecuteResult.Failure("Opening host wall not found");
                }
                return new ExecuteResult { IsError = false, Changed = new ChangedCounts(2, 0, 0) };
            }
        };

        var engine = new BimPipelineEngine(fakeClient);
        var result = await engine.ExecuteAsync(_model);

        Assert.False(result.Success);
        Assert.Equal(5, fakeClient.ExecutedRequests.Count);
        Assert.Equal(5, result.StageResults.Count);
        Assert.Equal(5, result.StageResults[4].StageIndex);
        Assert.Contains("Stage 5 (Openings) failed", result.ErrorMessage);
        Assert.DoesNotContain(executedLabels, l => l.Contains("Stage 6"));
        Assert.DoesNotContain(executedLabels, l => l.Contains("Stage 7"));
        Assert.DoesNotContain(executedLabels, l => l.Contains("Stage 8"));
    }

    [Fact]
    public async Task ExecuteAsync_SelectedStagesIsolateStages1And2_RunsOnlyStages1And2()
    {
        var fakeClient = new FakeRevitPipeClient();
        var engine = new BimPipelineEngine(fakeClient);

        var options = new BimPipelineOptions
        {
            SelectedStages = new[] { 1, 2 }
        };

        var result = await engine.ExecuteAsync(_model, options);

        Assert.True(result.Success);
        Assert.Equal(2, result.StageResults.Count);
        Assert.Equal(1, result.StageResults[0].StageIndex);
        Assert.Equal(2, result.StageResults[1].StageIndex);
        Assert.Equal(2, fakeClient.ExecutedRequests.Count);
    }

    [Fact]
    public async Task ExecuteAsync_SelectedStagesArbitrarySubset_RunsInCorrectNumericalOrder()
    {
        var fakeClient = new FakeRevitPipeClient();
        var engine = new BimPipelineEngine(fakeClient);

        var options = new BimPipelineOptions
        {
            SelectedStages = new[] { 7, 2, 4 } // Scrambled input order
        };

        var result = await engine.ExecuteAsync(_model, options);

        Assert.True(result.Success);
        Assert.Equal(3, result.StageResults.Count);
        Assert.Equal(2, result.StageResults[0].StageIndex);
        Assert.Equal(4, result.StageResults[1].StageIndex);
        Assert.Equal(7, result.StageResults[2].StageIndex);
    }

    [Fact]
    public async Task ExecuteAsync_SelectedStagesEmpty_CompletesWithZeroStages()
    {
        var fakeClient = new FakeRevitPipeClient();
        var engine = new BimPipelineEngine(fakeClient);

        var options = new BimPipelineOptions
        {
            SelectedStages = Array.Empty<int>()
        };

        var result = await engine.ExecuteAsync(_model, options);

        Assert.True(result.Success);
        Assert.Empty(result.StageResults);
        Assert.Empty(fakeClient.ExecutedRequests);
    }

    [Fact]
    public async Task ExecuteAsync_SelectedStagesNonExistent_CompletesWithZeroStages()
    {
        var fakeClient = new FakeRevitPipeClient();
        var engine = new BimPipelineEngine(fakeClient);

        var options = new BimPipelineOptions
        {
            SelectedStages = new[] { 99, -5 }
        };

        var result = await engine.ExecuteAsync(_model, options);

        Assert.True(result.Success);
        Assert.Empty(result.StageResults);
        Assert.Empty(fakeClient.ExecutedRequests);
    }

    [Fact]
    public async Task ExecuteAsync_AtomicDryRun_InvokesCompositeBimGeneratorInSingleStep()
    {
        var fakeClient = new FakeRevitPipeClient();
        var engine = new BimPipelineEngine(fakeClient);

        var options = new BimPipelineOptions
        {
            DryRun = true,
            AtomicDryRun = true
        };

        var result = await engine.ExecuteAsync(_model, options);

        Assert.True(result.Success);
        Assert.True(result.RolledBack);
        Assert.Single(result.StageResults);
        Assert.Equal(0, result.StageResults[0].StageIndex);
        Assert.Equal("CompositeDryRun", result.StageResults[0].StageName);
        Assert.Single(fakeClient.ExecutedRequests);
        Assert.True(fakeClient.ExecutedRequests[0].DryRun);
        Assert.Contains("Composite BIM Pipeline", fakeClient.ExecutedRequests[0].Label);
    }

    [Fact]
    public async Task ExecuteAsync_DryRunWithoutAtomic_ExecutesAll8StagesIndividuallyWithDryRunTrue()
    {
        var fakeClient = new FakeRevitPipeClient();
        var engine = new BimPipelineEngine(fakeClient);

        var options = new BimPipelineOptions
        {
            DryRun = true,
            AtomicDryRun = false
        };

        var result = await engine.ExecuteAsync(_model, options);

        Assert.True(result.Success);
        Assert.True(result.RolledBack);
        Assert.Equal(8, result.StageResults.Count);
        Assert.Equal(8, fakeClient.ExecutedRequests.Count);
        foreach (var req in fakeClient.ExecutedRequests)
        {
            Assert.True(req.DryRun);
        }
    }

    [Fact]
    public async Task ExecuteAsync_AtomicDryRunFails_ReportsFailureCorrectly()
    {
        var fakeClient = new FakeRevitPipeClient
        {
            CustomExecuteHandler = req => ExecuteResult.Failure("Composite transaction group failed to commit")
        };

        var engine = new BimPipelineEngine(fakeClient);
        var options = new BimPipelineOptions
        {
            DryRun = true,
            AtomicDryRun = true
        };

        var result = await engine.ExecuteAsync(_model, options);

        Assert.False(result.Success);
        Assert.Contains("Composite transaction group failed to commit", result.ErrorMessage);
        Assert.Single(result.StageResults);
        Assert.False(result.StageResults[0].Success);
    }

    [Fact]
    public async Task ExecuteAsync_AutoConnectFails_ThrowsRevitPipeException()
    {
        var fakeClient = new FakeRevitPipeClient
        {
            IsConnected = false,
            ShouldFailConnect = true
        };

        var engine = new BimPipelineEngine(fakeClient);

        await Assert.ThrowsAsync<RevitPipeException>(() => engine.ExecuteAsync(_model));
    }

    [Fact]
    public async Task ExecuteAsync_FiresStageCompletedEventForEachExecutedStage()
    {
        var fakeClient = new FakeRevitPipeClient();
        var engine = new BimPipelineEngine(fakeClient);
        var events = new List<StageResult>();
        engine.StageCompleted += res => events.Add(res);

        var options = new BimPipelineOptions
        {
            SelectedStages = new[] { 1, 3 }
        };

        await engine.ExecuteAsync(_model, options);

        Assert.Equal(2, events.Count);
        Assert.Equal(1, events[0].StageIndex);
        Assert.Equal(3, events[1].StageIndex);
    }
}
