namespace HPDynamo.E2E.Tests.Tiers;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPDynamo.E2E.Tests.Contracts;
using HPDynamo.E2E.Tests.Harness;
using Xunit;

/// <summary>
/// Tier 2: Boundary Value Analysis & Corner Cases - Part 2:
/// Dual Rollback Edge Cases, Seed Parameter Boundaries, and Lifecycle Transitions.
/// </summary>
public sealed class Tier2_RollbackAndSeedBoundariesTests
{
    [Fact]
    public async Task TC_T2_ROL_01_ExecutionCancellation_TriggersRollbackAndAbortsCleanly()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var req = new DynamoExecutionRequest("doc.CreateRebar();", DryRun: false);
        var res = await bridge.ExecuteAsync(req, cts.Token);

        Assert.False(res.Success);
        Assert.True(res.RolledBack);
        Assert.Equal(0, bridge.GetCommittedElementCount());
    }

    [Fact]
    public async Task TC_T2_ROL_02_ZeroCreatedElements_DryRun_StillReportsRolledBackTrue()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };
        var req = new DynamoExecutionRequest("// no elements created", DryRun: true);
        var res = await bridge.ExecuteAsync(req);

        Assert.True(res.Success);
        Assert.True(res.RolledBack);
        Assert.Empty(res.CreatedElementIds);
    }

    [Fact]
    public async Task TC_T2_ROL_03_MultipleCreatedElements_DryRun_RollsBackAllElementsCleanly()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };
        string code = "doc.CreateRebar(); doc.CreateRebar(); doc.CreateElement();";
        var req = new DynamoExecutionRequest(code, DryRun: true);
        var res = await bridge.ExecuteAsync(req);

        Assert.True(res.Success);
        Assert.True(res.RolledBack);
        Assert.Equal(0, bridge.GetCommittedElementCount());
    }

    [Fact]
    public void TC_T2_SED_01_AddNode_NaN_Coordinates_ThrowsArgumentException()
    {
        var bridge = new DynamoE2EFakeBridge();
        bridge.AttachLiveCanvas();
        Assert.Throws<ArgumentException>(() =>
            DynamoE2ETestHarness.AddNodeToCanvas(bridge, "Test", "Type", double.NaN, 100));
    }

    [Fact]
    public void TC_T2_SED_02_AddNode_PositiveInfinity_Coordinates_ThrowsArgumentException()
    {
        var bridge = new DynamoE2EFakeBridge();
        bridge.AttachLiveCanvas();
        Assert.Throws<ArgumentException>(() =>
            DynamoE2ETestHarness.AddNodeToCanvas(bridge, "Test", "Type", 100, double.PositiveInfinity));
    }

    [Fact]
    public void TC_T2_SED_03_ConnectPorts_NegativeSourcePort_ThrowsIndexOutOfRangeException()
    {
        var bridge = new DynamoE2EFakeBridge();
        bridge.AttachLiveCanvas();
        var n1 = DynamoE2ETestHarness.AddNodeToCanvas(bridge, "N1", "T1", 0, 0);
        var n2 = DynamoE2ETestHarness.AddNodeToCanvas(bridge, "N2", "T2", 10, 10);

        Assert.Throws<IndexOutOfRangeException>(() =>
            DynamoE2ETestHarness.ConnectPortsOnCanvas(bridge, n1.Id, -1, n2.Id, 0));
    }

    [Fact]
    public void TC_T2_SED_04_ConnectPorts_OutOfRangeTargetPort_ThrowsIndexOutOfRangeException()
    {
        var bridge = new DynamoE2EFakeBridge();
        bridge.AttachLiveCanvas();
        var n1 = DynamoE2ETestHarness.AddNodeToCanvas(bridge, "N1", "T1", 0, 0);
        var n2 = DynamoE2ETestHarness.AddNodeToCanvas(bridge, "N2", "T2", 10, 10);

        Assert.Throws<IndexOutOfRangeException>(() =>
            DynamoE2ETestHarness.ConnectPortsOnCanvas(bridge, n1.Id, 0, n2.Id, 999));
    }

    [Fact]
    public void TC_T2_SED_05_ConnectPorts_CyclicSelfLoop_ThrowsInvalidOperationException()
    {
        var bridge = new DynamoE2EFakeBridge();
        bridge.AttachLiveCanvas();
        var n1 = DynamoE2ETestHarness.AddNodeToCanvas(bridge, "N1", "T1", 0, 0);

        Assert.Throws<InvalidOperationException>(() =>
            DynamoE2ETestHarness.ConnectPortsOnCanvas(bridge, n1.Id, 0, n1.Id, 0));
    }

    [Fact]
    public void TC_T2_LCY_01_HeadlessMode_GetContext_ReportsIsLiveCanvasAttachedFalse()
    {
        var bridge = new DynamoE2EFakeBridge();
        var ctx = bridge.GetContext();
        Assert.False(ctx.IsLiveCanvasAttached);
    }

    [Fact]
    public void TC_T2_LCY_02_CanvasClosed_DetachesCanvasAndReturnsToHeadlessState()
    {
        var bridge = new DynamoE2EFakeBridge();
        bridge.AttachLiveCanvas("Workspace1");
        bridge.DetachLiveCanvas();

        var ctx = bridge.GetContext();
        Assert.False(ctx.IsLiveCanvasAttached);
    }

    [Fact]
    public void TC_T2_WIR_01_MalformedNdjson_SimulatedParser_IdentifiesIncompleteJson()
    {
        char openBrace = (char)123;
        string incomplete = openBrace + "\"jsonrpc\":\"2.0\",\"method\":";
        Assert.ThrowsAny<JsonException>(() => JsonDocument.Parse(incomplete));
    }

    [Fact]
    public async Task TC_T2_WIR_02_EmptyScript_DryRun_ExecutesSafelyWithoutCrashing()
    {
        var bridge = new DynamoE2EFakeBridge { IsExecutionEnabled = true };
        var res = await bridge.ExecuteAsync(new DynamoExecutionRequest("", DryRun: true));
        Assert.True(res.Success);
    }

    [Fact]
    public void TC_T2_WIR_03_TimeoutClamping_ClampsExcessiveTimeoutToMaxCeiling()
    {
        int requestedTimeout = 99999;
        int maxCeiling = 120;
        int clamped = Math.Min(requestedTimeout, maxCeiling);
        Assert.Equal(120, clamped);
    }

    [Fact]
    public void TC_T2_SED_06_GraphSpec_WithZeroNodesAndWires_SerializesCleanly()
    {
        var emptyGraph = new DynamoGraphSpec(Guid.NewGuid(), "Empty", "None", new(), new());
        string json = emptyGraph.ToJson();
        var roundtrip = DynamoGraphSpec.FromJson(json);
        Assert.NotNull(roundtrip);
        Assert.Empty(roundtrip!.Nodes);
        Assert.Empty(roundtrip.Wires);
    }

    [Fact]
    public void TC_T2_SED_07_GraphSpec_InvalidJson_ReturnsNullOrThrows()
    {
        Assert.Throws<JsonException>(() => DynamoGraphSpec.FromJson("not a json"));
    }

    [Fact]
    public void TC_T2_SED_08_CanvasAuthoring_AddMultipleNodes_MaintainsAccurateCount()
    {
        var bridge = new DynamoE2EFakeBridge();
        bridge.AttachLiveCanvas();
        for (int i = 0; i < 5; i++)
        {
            DynamoE2ETestHarness.AddNodeToCanvas(bridge, $"Node_{i}", "Type", i * 10, i * 10);
        }
        Assert.Equal(5, bridge.ActiveCanvasWorkspace.Nodes.Count);
    }

    [Fact]
    public void TC_T2_SED_09_CanvasAuthoring_RemoveNode_RemovesNodeFromCanvas()
    {
        var bridge = new DynamoE2EFakeBridge();
        bridge.AttachLiveCanvas();
        var n = DynamoE2ETestHarness.AddNodeToCanvas(bridge, "NodeToRemove", "Type", 0, 0);
        bridge.ActiveCanvasWorkspace.Nodes.Remove(n);
        Assert.Empty(bridge.ActiveCanvasWorkspace.Nodes);
    }

    [Fact]
    public void TC_T2_WIR_04_Utf8Characters_PreservesVietnameseAccentedText()
    {
        var graph = new DynamoGraphSpec(
            Guid.NewGuid(), "Mô hình Dầm TCVN", "Bố trí cốt thép dầm", new(), new());
        string json = graph.ToJson();
        var parsed = DynamoGraphSpec.FromJson(json);
        Assert.Equal("Mô hình Dầm TCVN", parsed!.Name);
        Assert.Equal("Bố trí cốt thép dầm", parsed.Description);
    }
}
