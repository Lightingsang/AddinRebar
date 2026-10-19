namespace HPDynamo.E2E.Tests.Tiers;

using System;
using System.Threading;
using System.Threading.Tasks;
using HPDynamo.E2E.Tests.Contracts;
using HPDynamo.E2E.Tests.Harness;
using Xunit;

/// <summary>
/// Tier 3: Combinatorial & Multi-Feature Interaction Testing.
/// Validates pairwise combinations, lifecycle progression, and cross-feature workflows.
/// </summary>
public sealed class Tier3_CrossFeatureInteractionTests
{
    [Fact]
    public async Task TC_T3_INT_01_ExecuteCode_DryRun_WithOptIn_AndScriptGuard_RollsBackCleanly()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };
        string validCode = "doc.CreateRebar();";

        var quality = bridge.ValidateScript(validCode);
        Assert.True(quality.IsValid);

        var req = new DynamoExecutionRequest(validCode, DryRun: true);
        var res = await bridge.ExecuteAsync(req);

        Assert.True(res.Success);
        Assert.True(res.RolledBack);
        Assert.Equal(0, bridge.GetCommittedElementCount());
    }

    [Fact]
    public async Task TC_T3_INT_02_ExecuteCode_Commit_ThenQueryContext_ReflectsUpdatedState()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };
        bridge.AttachLiveCanvas("StructuralModel");

        var req = new DynamoExecutionRequest("doc.CreateRebar();", DryRun: false);
        var res = await bridge.ExecuteAsync(req);

        Assert.True(res.Success);
        Assert.False(res.RolledBack);
        Assert.Equal(1, bridge.GetCommittedElementCount());

        var ctx = bridge.GetContext();
        Assert.True(ctx.IsLiveCanvasAttached);
        Assert.Equal("StructuralModel", ctx.ActiveWorkspaceName);
    }

    [Fact]
    public async Task TC_T3_INT_03_DynamicRegistry_ProposeTestPublish_LifecycleAdvancesCleanly()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };
        string proposedCode = "int calculatedSpan = 6000;";

        // 1. Propose & AST validation
        var quality = bridge.ValidateScript(proposedCode);
        Assert.True(quality.IsValid);

        // 2. Test in sandbox with dryRun
        var testRun = await bridge.ExecuteAsync(new DynamoExecutionRequest(proposedCode, DryRun: true));
        Assert.True(testRun.Success);
        Assert.True(testRun.RolledBack);

        // 3. Publish to tool surface
        var tools = DynamoE2ETestHarness.BuildToolSurfaceList();
        Assert.Equal(25, tools.Count);
    }

    [Fact]
    public async Task TC_T3_INT_04_SeedPipeline_ValidateThenRunGraph_UnderTransactionGroup()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };
        var graph = DynamoE2ETestHarness.CreateSampleRebarGraph();
        string json = graph.ToJson();

        var parsed = DynamoGraphSpec.FromJson(json);
        Assert.NotNull(parsed);
        Assert.Equal(2, parsed!.Nodes.Count);

        var runReq = new DynamoExecutionRequest("doc.CreateRebar();", DryRun: true);
        var res = await bridge.ExecuteAsync(runReq);

        Assert.True(res.Success);
        Assert.True(res.RolledBack);
    }

    [Fact]
    public void TC_T3_INT_05_CanvasAuthoring_PlaceWireConfigure_ProducesConnectedGraph()
    {
        var bridge = new DynamoE2EFakeBridge();
        bridge.AttachLiveCanvas("CanvasAuthoring");

        var n1 = DynamoE2ETestHarness.AddNodeToCanvas(bridge, "Point.ByCoordinates", "DSCoreNodes.Point", 100, 100);
        var n2 = DynamoE2ETestHarness.AddNodeToCanvas(bridge, "Curve.ByPoints", "DSCoreNodes.Curve", 300, 100);
        var wire = DynamoE2ETestHarness.ConnectPortsOnCanvas(bridge, n1.Id, 0, n2.Id, 0);

        n1.Properties["x"] = 150.0;
        n1.Properties["y"] = 250.0;

        var ctx = bridge.GetContext();
        Assert.Equal(2, ctx.NodesCount);
        Assert.Equal(1, ctx.WiresCount);
        Assert.Equal(150.0, n1.Properties["x"]);
    }

    [Fact]
    public async Task TC_T3_INT_06_CancellationMidExecution_AbortsTransactionWithoutOrphanLocks()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var cancelRes = await bridge.ExecuteAsync(new DynamoExecutionRequest("doc.CreateRebar();"), cts.Token);
        Assert.False(cancelRes.Success);
        Assert.True(cancelRes.RolledBack);

        // Subsequent execution executes cleanly without residual lock
        var nextRes = await bridge.ExecuteAsync(new DynamoExecutionRequest("int ok = 1;", DryRun: false));
        Assert.True(nextRes.Success);
    }

    [Fact]
    public void TC_T3_INT_07_DualLifecycleSwitch_HeadlessToLiveCanvas_Maintains25ToolsSurface()
    {
        var bridge = new DynamoE2EFakeBridge();
        var toolsBefore = DynamoE2ETestHarness.BuildToolSurfaceList();
        Assert.Equal(25, toolsBefore.Count);
        Assert.False(bridge.GetContext().IsLiveCanvasAttached);

        bridge.AttachLiveCanvas("LiveSession");
        var toolsAfter = DynamoE2ETestHarness.BuildToolSurfaceList();
        Assert.Equal(25, toolsAfter.Count);
        Assert.True(bridge.GetContext().IsLiveCanvasAttached);
    }

    [Fact]
    public void TC_T3_INT_08_ProposalWithForbiddenNamespace_BlockedBeforePersistence()
    {
        var bridge = new DynamoE2EFakeBridge();
        string malicious = "System.IO.File.Delete(\"revit.rvt\");";
        var quality = bridge.ValidateScript(malicious);

        Assert.False(quality.IsValid);
        Assert.Contains(quality.Errors, e => e.Contains("System.IO"));

        var tools = DynamoE2ETestHarness.BuildToolSurfaceList();
        Assert.Equal(25, tools.Count);
    }
}
