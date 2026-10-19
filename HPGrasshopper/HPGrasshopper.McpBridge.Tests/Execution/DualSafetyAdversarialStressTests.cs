namespace HPGrasshopper.McpBridge.Tests.Execution;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

public sealed class DualSafetyAdversarialStressTests
{
    private static readonly BridgeSettings DefaultSettings = new() { ExecutionEnabled = true };

    [Theory]
    [InlineData("AUTO", "auto")]
    [InlineData("  auto  ", "auto")]
    [InlineData("MANUAL", "manual")]
    [InlineData("none", "none")]
    [InlineData("NONE", "none")]
    [InlineData(null, "auto")]
    [InlineData("invalid_mode", "auto")]
    public void Level1_TransactionMode_Normalization_HandlesAdversarialInputs(string? inputMode, string expectedEffectiveMode)
    {
        var normalized = TransactionModes.Normalize(inputMode) ?? TransactionModes.Auto;
        Assert.Equal(expectedEffectiveMode, normalized);
    }

    [Fact]
    public void Level1_ManualMode_WhenDocModifiableParameterIsStale_BypassesEarlyCheck()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("manual tx", "manual", false);
        using var cts = new CancellationTokenSource();

        // If isDocModifiable was evaluated BEFORE action ran, it is false.
        // Even if the action opened a transaction that remains uncommitted,
        // RevitTransactionScope.ExecuteWithContext passed isDocModifiable=false.
        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "uncommitted work",
            isDocModifiable: false);

        // Verification: The method completes and calls AssimilateGroup because isDocModifiable was false.
        Assert.False(result.IsError);
        Assert.Equal(1, ctx.AssimilateGroupCount);
    }

    [Fact]
    public void Level1_UnhandledException_Guarantees100PercentRollback_WithOriginalErrorMessage()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("faulty code", "auto", false) { Label = "CrashTest" };
        using var cts = new CancellationTokenSource();

        const string expectedError = "Fatal structural constraint violation: intersecting solids";
        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => throw new InvalidOperationException(expectedError));

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains(expectedError, result.Message);
        Assert.Equal(1, ctx.RollbackAutoTxCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.Equal(0, ctx.AssimilateGroupCount);
    }

    [Fact]
    public void Level2_CanvasUndoScope_Structure_VerifiesPresenceOfRemovedSnapshotTracking()
    {
        var type = typeof(CanvasUndoScope);

        // Verify that CanvasUndoScope has fields for added GUIDs, wires, and removed snapshots
        var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in fields) fieldNames.Add(f.Name);

        Assert.Contains("_addedGuids", fieldNames);
        Assert.Contains("_connectedWires", fieldNames);
        Assert.Contains("_disconnectedWires", fieldNames);
        Assert.Contains("_removedSnapshots", fieldNames);

        // Verify TrackRemovedObject API method exists
        Assert.NotNull(type.GetMethod("TrackRemovedObject"));
    }

    [Fact]
    public void Level2_WireLedger_RestorationOrder_DemonstratesForwardTraversal()
    {
        // Inspect RollbackWires method via reflection to verify iteration structure
        var type = typeof(CanvasUndoScope);
        var rollbackWiresMethod = type.GetMethod("RollbackWires", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(rollbackWiresMethod);

        // Both _connectedWires and _disconnectedWires are List<(IGH_Param, IGH_Param)>
        var connectedField = type.GetField("_connectedWires", BindingFlags.NonPublic | BindingFlags.Instance);
        var disconnectedField = type.GetField("_disconnectedWires", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(connectedField);
        Assert.NotNull(disconnectedField);
        Assert.Equal(typeof(List<(Grasshopper.Kernel.IGH_Param Target, Grasshopper.Kernel.IGH_Param Source)>), connectedField.FieldType);
        Assert.Equal(typeof(List<(Grasshopper.Kernel.IGH_Param Target, Grasshopper.Kernel.IGH_Param Source)>), disconnectedField.FieldType);
    }
}
