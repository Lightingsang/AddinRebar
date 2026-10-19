namespace HPGrasshopper.McpBridge.Tests.Execution;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Grasshopper.Kernel;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

public sealed class Milestone2CommitOrderingAndRollbackLedgerTests
{
    private static readonly BridgeSettings DefaultSettings = new() { ExecutionEnabled = true };

    private sealed class TrackingCanvasUndoScope : ICanvasUndoScope
    {
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }
        public bool ThrowOnRollback { get; set; }
        public bool IsCommitted { get; private set; }

        public void Commit()
        {
            CommitCount++;
            IsCommitted = true;
        }

        public CanvasRollbackResult Rollback()
        {
            RollbackCount++;
            if (ThrowOnRollback)
                throw new InvalidOperationException("Simulated canvas rollback exception");
            return CanvasRollbackResult.Clean();
        }

        public void Dispose()
        {
            if (!IsCommitted && RollbackCount == 0)
                Rollback();
        }
    }

    private class FailingParamProxy : System.Reflection.DispatchProxy
    {
        public Guid InstanceGuid { get; set; } = Guid.NewGuid();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null) return null;
            if (targetMethod.Name == "RemoveSource")
                throw new InvalidOperationException("Simulated wire disconnect failure");
            if (targetMethod.Name == "AddSource")
                throw new InvalidOperationException("Simulated wire add failure");
            if (targetMethod.Name == "get_InstanceGuid") return InstanceGuid;
            if (targetMethod.Name == "get_Sources") return new List<IGH_Param>();
            if (targetMethod.Name == "get_Recipients") return new List<IGH_Param>();
            return null;
        }

        public static IGH_Param Create() =>
            System.Reflection.DispatchProxy.Create<IGH_Param, FailingParamProxy>();
    }

    [Fact]
    public void Execute_WhenRevitAutoTxSucceeds_CommitsCanvasAfterRevitFinalOutcome()
    {
        using var ctx = new SimulatedTransactionContext { AutoTransactionCommitResult = true };
        var canvasScope = new TrackingCanvasUndoScope();
        var req = new ExecuteRequest("create Element", "auto", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "element_created", canvasScope: canvasScope);

        Assert.False(result.IsError);
        Assert.False(result.RolledBack);
        Assert.Equal(1, ctx.CommitAutoTxCount);
        Assert.Equal(1, canvasScope.CommitCount);
        Assert.Equal(0, canvasScope.RollbackCount);
    }

    [Fact]
    public void Execute_WhenRevitAutoTxFails_RollsBackCanvasAndNeverCommits()
    {
        using var ctx = new SimulatedTransactionContext { AutoTransactionCommitResult = false };
        var canvasScope = new TrackingCanvasUndoScope();
        var req = new ExecuteRequest("create Element", "auto", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "element_attempted", canvasScope: canvasScope);

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("Revit rejected the change", result.Message);
        Assert.Equal(0, canvasScope.CommitCount);
        Assert.Equal(1, canvasScope.RollbackCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Fact]
    public void Execute_WhenCancellationRequested_RollsBackCanvasAndNeverCommits()
    {
        using var ctx = new SimulatedTransactionContext();
        var canvasScope = new TrackingCanvasUndoScope();
        var req = new ExecuteRequest("cancel", "auto", false);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "never_runs", canvasScope: canvasScope);

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Equal(0, canvasScope.CommitCount);
        Assert.Equal(1, canvasScope.RollbackCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Fact]
    public void Execute_WhenActionThrows_RollsBackCanvasAndNeverCommits()
    {
        using var ctx = new SimulatedTransactionContext();
        var canvasScope = new TrackingCanvasUndoScope();
        var req = new ExecuteRequest("fault", "auto", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => throw new InvalidOperationException("Script fault"),
            canvasScope: canvasScope);

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("Script fault", result.Message);
        Assert.Equal(0, canvasScope.CommitCount);
        Assert.Equal(1, canvasScope.RollbackCount);
    }

    [Fact]
    public void Execute_WhenDryRunTrue_RollsBackBothRevitAndCanvas()
    {
        using var ctx = new SimulatedTransactionContext();
        var canvasScope = new TrackingCanvasUndoScope();
        var req = new ExecuteRequest("preview", "auto", true);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, log) => { log("dry-run"); return "preview_data"; },
            canvasScope: canvasScope);

        Assert.False(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Equal(0, canvasScope.CommitCount);
        Assert.Equal(1, canvasScope.RollbackCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Fact]
    public void Execute_WhenCanvasRollbackThrows_RevitRollbackStillExecutes()
    {
        using var ctx = new SimulatedTransactionContext();
        var canvasScope = new TrackingCanvasUndoScope { ThrowOnRollback = true };
        var req = new ExecuteRequest("fault", "auto", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => throw new InvalidOperationException("Initial failure"),
            canvasScope: canvasScope);

        Assert.True(result.IsError);
        Assert.False(result.RolledBack);
        Assert.Contains("Initial failure", result.Message);
        Assert.Contains("Additionally, canvas rollback failed", result.Message);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Fact]
    public void CanvasUndoScope_ObjectAddedAndRemovedInSameScope_DoesNotResurrectOnRollback()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var param = TestProxyFactory.CreateParam("tempObject", "TO");

        scope.TrackAddedObject(param.InstanceGuid);
        scope.TrackRemovedObject(param);

        var type = typeof(CanvasUndoScope);
        var removedField = type.GetField("_removedSnapshots", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(removedField);
        var removedList = (IList?)removedField!.GetValue(scope);
        Assert.NotNull(removedList);
        Assert.Empty(removedList);

        var rollbackResult = scope.Rollback();
        Assert.True(rollbackResult.Success);
        Assert.True(rollbackResult.CleanlyRolledBack);
        Assert.True(scope.RolledBackCleanly);
    }

    [Fact]
    public void CanvasUndoScope_BaselineObjectRemoved_TrackedInRemovedSnapshotsAndRestoredOnRollback()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var baselineParam = TestProxyFactory.CreateParam("baselineObject", "BO");

        scope.TrackRemovedObject(baselineParam);

        var type = typeof(CanvasUndoScope);
        var removedField = type.GetField("_removedSnapshots", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(removedField);
        var removedList = (IList?)removedField!.GetValue(scope);
        Assert.NotNull(removedList);
        Assert.Single(removedList);

        var rollbackResult = scope.Rollback();
        Assert.True(rollbackResult.Success);
        Assert.True(scope.RolledBackCleanly);
    }

    [Fact]
    public void CanvasUndoScope_WireTracking_RecordsPreMutationAndRollbackClean()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var target = TestProxyFactory.CreateParam("targetParam", "TP");
        var source = TestProxyFactory.CreateParam("sourceParam", "SP");

        scope.TrackConnectedWire(target, source);
        scope.TrackDisconnectedWire(target, source);

        var type = typeof(CanvasUndoScope);
        var connectedField = type.GetField("_connectedWires", BindingFlags.NonPublic | BindingFlags.Instance);
        var disconnectedField = type.GetField("_disconnectedWires", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(connectedField);
        Assert.NotNull(disconnectedField);

        var connList = (IList?)connectedField!.GetValue(scope);
        var disconnList = (IList?)disconnectedField!.GetValue(scope);

        Assert.Single(connList!);
        Assert.Single(disconnList!);

        var rollbackResult = scope.Rollback();
        Assert.True(rollbackResult.Success);
        Assert.True(scope.RolledBackCleanly);
    }

    [Fact]
    public void CanvasUndoScope_RollbackError_CollectsErrorsAndReturnsTruthfulStatus()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var failingTarget = FailingParamProxy.Create();
        var source = TestProxyFactory.CreateParam("sourceParam", "SP");

        scope.TrackConnectedWire(failingTarget, source);

        var rollbackResult = scope.Rollback();

        Assert.False(rollbackResult.Success);
        Assert.False(rollbackResult.CleanlyRolledBack);
        Assert.False(scope.RolledBackCleanly);
        Assert.NotEmpty(rollbackResult.Errors);
        Assert.Contains("Simulated wire disconnect failure", rollbackResult.Errors[0]);
    }

    [Fact]
    public void CanvasRollbackResult_CleanAndFailed_FactoryMethods()
    {
        var clean = CanvasRollbackResult.Clean();
        Assert.True(clean.Success);
        Assert.True(clean.CleanlyRolledBack);
        Assert.Empty(clean.Errors);

        var failed = CanvasRollbackResult.Failed(new[] { "error1", "error2" });
        Assert.False(failed.Success);
        Assert.False(failed.CleanlyRolledBack);
        Assert.Equal(2, failed.Errors.Count);
        Assert.Equal("error1", failed.Errors[0]);
    }
}
