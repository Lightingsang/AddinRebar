namespace HPGrasshopper.McpBridge.Tests.Regression;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Grasshopper.Kernel;
using HPGrasshopper.McpBridge.Coordinator;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tests.Fakes;
using Xunit;

/// <summary>
///     Adversarial stress-test suite for Findings N4 and N5:
///     - N4: Complex interleaved lifecycle sequences (multiple baseline/temporary components, multi-wire topology).
///     - N5: Strict edge cases for target identity, whitespace normalization, and GH-only validation.
/// </summary>
public sealed class ChallengerM1N4N5StressTests
{
    private static void RegisterBaseline(CanvasUndoScope scope, params IGH_Param[] components)
    {
        var baselineField = typeof(CanvasUndoScope).GetField("_baselineGuids", BindingFlags.NonPublic | BindingFlags.Instance);
        var baselineSet = (HashSet<Guid>)baselineField!.GetValue(scope)!;
        foreach (var comp in components) baselineSet.Add(comp.InstanceGuid);
    }

    private static void DisconnectAndTrack(CanvasUndoScope scope, IGH_Param target, IGH_Param source)
    {
        scope.TrackRemovedObject(target);
        scope.TrackDisconnectedWire(target, source);
        target.RemoveSource(source);
    }

    [Fact]
    public void N4_Stress_InterleavedBaselineAndTemporaryLifecycle_RestoresBaselinesAndDiscardsTemporaries()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var baselineComp1 = TestProxyFactory.CreateParam("baseComp1", "BC1");
        var baselineComp2 = TestProxyFactory.CreateParam("baseComp2", "BC2");
        var tempComp1 = TestProxyFactory.CreateParam("tempParam1", "TP1");
        var tempComp2 = TestProxyFactory.CreateParam("tempParam2", "TP2");
        var sourceNode = TestProxyFactory.CreateParam("sourceNode", "SN");

        baselineComp1.AddSource(sourceNode);
        baselineComp2.AddSource(sourceNode);
        RegisterBaseline(scope, baselineComp1, baselineComp2);

        // Interleaved operations
        DisconnectAndTrack(scope, baselineComp1, sourceNode);
        scope.TrackAddedObject(tempComp1.InstanceGuid);
        scope.TrackAddedObject(baselineComp1.InstanceGuid);
        scope.TrackRemovedObject(tempComp1);
        scope.TrackAddedObject(tempComp2.InstanceGuid);

        // Remove baseline 2 twice (remove -> add -> remove)
        DisconnectAndTrack(scope, baselineComp2, sourceNode);
        scope.TrackAddedObject(baselineComp2.InstanceGuid);
        scope.TrackRemovedObject(baselineComp2);

        var removedField = typeof(CanvasUndoScope).GetField("_removedSnapshots", BindingFlags.NonPublic | BindingFlags.Instance);
        var removedList = (IList)removedField!.GetValue(scope)!;
        Assert.Equal(2, removedList.Count);

        var rollbackResult = scope.Rollback();
        Assert.True(rollbackResult.Success && rollbackResult.CleanlyRolledBack);
        Assert.Contains(sourceNode, baselineComp1.Sources);
        Assert.Contains(sourceNode, baselineComp2.Sources);
    }

    [Fact]
    public void N4_Stress_BaselineWithMultiWireTopology_RestoresAllSeveredConnections()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var baselineTarget = TestProxyFactory.CreateParam("multiTarget", "MT");
        var sourceNodeA = TestProxyFactory.CreateParam("srcNodeA", "SNA");
        var sourceNodeB = TestProxyFactory.CreateParam("srcNodeB", "SNB");

        baselineTarget.AddSource(sourceNodeA);
        baselineTarget.AddSource(sourceNodeB);
        RegisterBaseline(scope, baselineTarget);

        scope.TrackRemovedObject(baselineTarget);
        scope.TrackDisconnectedWire(baselineTarget, sourceNodeA);
        scope.TrackDisconnectedWire(baselineTarget, sourceNodeB);
        baselineTarget.RemoveSource(sourceNodeA);
        baselineTarget.RemoveSource(sourceNodeB);

        scope.TrackAddedObject(baselineTarget.InstanceGuid);

        var rollbackResult = scope.Rollback();
        Assert.True(rollbackResult.Success && rollbackResult.CleanlyRolledBack);
        Assert.Equal(2, baselineTarget.Sources.Count);
        Assert.Contains(sourceNodeA, baselineTarget.Sources);
        Assert.Contains(sourceNodeB, baselineTarget.Sources);
    }

    [Fact]
    public void N5_Stress_WhitespaceAndEmptyTargetDocPath_NormalizesToNullConsistently()
    {
        var whitespaceCases = new[] { string.Empty, " ", "\t\t", "\r\n", "   \t \r\n   " };
        foreach (var candidatePath in whitespaceCases)
        {
            var workRequest = new McpBridgeWorkRequest(
                "WhitespaceCheck",
                (_, _) => null,
                CancellationToken.None,
                TargetDocTitle: "RevitProject.rvt",
                TargetDocPath: candidatePath);

            Assert.Null(workRequest.TargetDocPath);
            Assert.Equal("RevitProject.rvt", workRequest.TargetDocTitle);
        }
    }

    [Fact]
    public void N5_Stress_OnlyDefIdSpecified_EvaluatesGrasshopperWithoutRevitDependency()
    {
        var expectedGuid = Guid.NewGuid();
        var workRequest = new McpBridgeWorkRequest(
            "GhOnlyMutation",
            (_, _) => null,
            CancellationToken.None,
            TargetDocTitle: null,
            TargetDocPath: null,
            TargetDefId: expectedGuid,
            RequireTargetIdentity: true);

        var capturedException = Assert.Throws<InvalidOperationException>(() => workRequest.ValidateActiveDocument(null));
        Assert.Contains("Grasshopper definition identity mismatch", capturedException.Message);
        Assert.Contains(expectedGuid.ToString(), capturedException.Message);
        Assert.DoesNotContain("no document is open in Revit", capturedException.Message);
    }

    [Fact]
    public void N5_Stress_RequireTargetIdentityTrue_WithEmptyGuidAndWhitespaceTitle_FailsClosed()
    {
        var workRequest = new McpBridgeWorkRequest(
            "RejectInvalidIdentity",
            (_, _) => null,
            CancellationToken.None,
            TargetDocTitle: "   ",
            TargetDocPath: "\t\r\n",
            TargetDefId: Guid.Empty,
            RequireTargetIdentity: true);

        var capturedException = Assert.Throws<InvalidOperationException>(() => workRequest.ValidateActiveDocument(null));
        Assert.Contains("Write operation 'RejectInvalidIdentity' rejected", capturedException.Message);
        Assert.Contains("Target document or definition identity must be specified", capturedException.Message);
    }
}
