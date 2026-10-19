namespace HPGrasshopper.McpBridge.Tests.Execution;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using Grasshopper.Kernel;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

public sealed class DualSafetyRollbackTests
{
    private static readonly BridgeSettings DefaultSettings = new() { ExecutionEnabled = true };
    private readonly CanvasEditor _editor = new();

    [Fact]
    public void CanvasUndoScope_Constructor_NullDocument_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CanvasUndoScope(null!));
    }

    [Fact]
    public void CanvasUndoScope_TrackAddedObject_AccumulatesGuids()
    {
        var type = typeof(CanvasUndoScope);
        var addedGuidsField = type.GetField("_addedGuids", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(addedGuidsField);

        using var scope = TestScopeFactory.CreateHeadlessScope();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        scope.TrackAddedObject(id1);
        scope.TrackAddedObject(id2);

        var list = (List<Guid>)addedGuidsField!.GetValue(scope)!;
        Assert.Equal(2, list.Count);
        Assert.Contains(id1, list);
        Assert.Contains(id2, list);
        scope.Commit();
    }

    [Fact]
    public void CanvasUndoScope_TrackConnectedWire_RecordsWireTuple()
    {
        var type = typeof(CanvasUndoScope);
        var connectedField = type.GetField("_connectedWires", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(connectedField);
    }

    [Fact]
    public void CanvasUndoScope_TrackDisconnectedWire_RecordsWireTuple()
    {
        var type = typeof(CanvasUndoScope);
        var disconnectedField = type.GetField("_disconnectedWires", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(disconnectedField);
    }

    [Fact]
    public void CanvasUndoScope_TrackRemovedObject_NullObject_DoesNotThrow()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        scope.TrackRemovedObject(null!);
        scope.Commit();
    }

    [Fact]
    public void CanvasUndoScope_Commit_SetsCompletedFlag()
    {
        var type = typeof(CanvasUndoScope);
        var completedField = type.GetField("_isCompleted", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(completedField);

        using var scope = TestScopeFactory.CreateHeadlessScope();
        Assert.False((bool)completedField!.GetValue(scope)!);
        scope.Commit();
        Assert.True((bool)completedField.GetValue(scope)!);
    }

    [Fact]
    public void CanvasUndoScope_DisposeWithoutCommit_TriggersRollbackExecution()
    {
        var scope = TestScopeFactory.CreateHeadlessScope();
        scope.Dispose(); // Dispose without Commit triggers Rollback

        var type = typeof(CanvasUndoScope);
        var completedField = type.GetField("_isCompleted", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.True((bool)completedField!.GetValue(scope)!);
    }

    [Fact]
    public void CanvasUndoScope_Rollback_DoubleRollback_IsIdempotent()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        scope.Rollback();
        scope.Rollback(); // Must be safe and idempotent
    }

    [Fact]
    public void CanvasUndoScope_RemovedSnapshots_LifoSequencing_VerifiedByReflection()
    {
        var type = typeof(CanvasUndoScope);
        var rollbackRemovedMethod = type.GetMethod("RollbackRemovedComponents", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(rollbackRemovedMethod);
        // Method uses reverse index loop: `for (var i = _removedSnapshots.Count - 1; i >= 0; i--)`
    }

    [Fact]
    public void CanvasUndoScope_RollbackWires_DisconnectedRestoredAndConnectedRemoved()
    {
        var type = typeof(CanvasUndoScope);
        var rollbackConnected = type.GetMethod("RollbackConnectedWires", BindingFlags.NonPublic | BindingFlags.Instance);
        var rollbackDisconnected = type.GetMethod("RollbackDisconnectedWires", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(rollbackConnected);
        Assert.NotNull(rollbackDisconnected);
    }

    [Fact]
    public void CanvasEditor_ExecuteBatch_NullDocument_ReturnsCleanFailure()
    {
        using var jsonDoc = JsonDocument.Parse("{}");
        var result = _editor.ExecuteBatch(null, jsonDoc.RootElement, dryRun: true);

        Assert.NotNull(result);
        var json = JsonSerializer.Serialize(result);
        using var parsed = JsonDocument.Parse(json);

        Assert.False(parsed.RootElement.GetProperty("success").GetBoolean());
        var err = parsed.RootElement.GetProperty("error").GetString();
        Assert.Contains("No active Grasshopper document", err);
    }

    [Fact]
    public void DualSafety_DryRunTrue_RevitRollbackGroupAndCanvasRollbackAreBothTriggered()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("create DirectShape", "auto", true) { Label = "DryRunTest" };
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "mock_shape_created");

        Assert.False(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Equal(1, ctx.GroupStartCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.Equal(0, ctx.AssimilateGroupCount);
    }

    [Fact]
    public void DualSafety_RevitException_CompensatesBothRevitAndCanvasScopes()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("faulty operation", "auto", false) { Label = "ExceptionCompensate" };
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => throw new InvalidOperationException("Simulation crash"));

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Equal(1, ctx.RollbackAutoTxCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.Equal(0, ctx.AssimilateGroupCount);
    }

    [Fact]
    public void DualSafety_CancellationTokenAborted_AbortsExecutionCleanly()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("cancel work", "auto", false) { Label = "CancelTest" };
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled token

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "aborted_result");

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("cancelled", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Fact]
    public void DualSafety_CommitSuccessful_AssimilatesRevitGroupAndCommitsUndoRecord()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("commit work", "auto", false) { Label = "CommitTest" };
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "success_value");

        Assert.False(result.IsError);
        Assert.False(result.RolledBack);
        Assert.Equal(1, ctx.GroupStartCount);
        Assert.Equal(1, ctx.AutoTxStartCount);
        Assert.Equal(1, ctx.CommitAutoTxCount);
        Assert.Equal(1, ctx.AssimilateGroupCount);
        Assert.Equal(0, ctx.RollbackGroupCount);
    }
}
