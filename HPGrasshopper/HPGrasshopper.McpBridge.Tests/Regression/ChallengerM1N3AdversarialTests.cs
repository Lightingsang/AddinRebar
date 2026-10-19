namespace HPGrasshopper.McpBridge.Tests.Regression;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

/// <summary>
///     Adversarial empirical challenge tests for Finding N3 (Cancellation, Deferred Commit, Safe Scope).
/// </summary>
public sealed class ChallengerM1N3AdversarialTests
{
    private static readonly BridgeSettings DefaultSettings = new() { ExecutionEnabled = true };

    private sealed class TrackingCanvasScope : ICanvasUndoScope
    {
        public bool IsCommitted { get; private set; }
        public bool RollbackCalled { get; private set; }

        public void Commit() => IsCommitted = true;

        public CanvasRollbackResult Rollback()
        {
            RollbackCalled = true;
            return CanvasRollbackResult.Clean();
        }

        public void Dispose()
        {
            if (!IsCommitted && !RollbackCalled) Rollback();
        }
    }

    [Fact]
    public void N3_CancellationBeforePreflight_ThrowsOperationCanceledException_NoCommit()
    {
        var editor = new CanvasEditor();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        using var doc = JsonDocument.Parse("{\"componentsToAdd\": []}");

        Assert.Throws<OperationCanceledException>(() =>
            editor.ExecuteBatch(null, doc.RootElement, false, cts.Token));
    }

    [Fact]
    public void N3_CancellationBeforeMutation_ThrowsOperationCanceledException_NoCommit()
    {
        var editor = new CanvasEditor();
        using var cts = new CancellationTokenSource();
        using var doc = JsonDocument.Parse("{\"componentsToAdd\": []}");
        using var scope = TestScopeFactory.CreateHeadlessScope();

        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            editor.ExecuteBatch(null, doc.RootElement, false, cts.Token, scope));

        Assert.False(scope.IsCommitted);
    }

    [Fact]
    public void N3_CancellationDuringBatch_InvokesRollbackAndPreventsCommit()
    {
        var editor = new CanvasEditor();
        using var cts = new CancellationTokenSource();
        using var doc = JsonDocument.Parse("{\"componentsToAdd\": []}");
        using var scope = TestScopeFactory.CreateHeadlessScope();

        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            editor.ExecuteBatch(null, doc.RootElement, false, cts.Token, scope));

        Assert.False(scope.IsCommitted);
    }

    [Fact]
    public void N3_CancellationBeforeSolve_RollsBackScopeAndPreventsCommit()
    {
        var editor = new CanvasEditor();
        using var cts = new CancellationTokenSource();
        using var doc = JsonDocument.Parse("{\"componentsToAdd\": []}");
        var canvas = new TrackingCanvasScope();

        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            editor.ExecuteBatch(null, doc.RootElement, false, cts.Token, undoScope: null));

        Assert.False(canvas.IsCommitted);
    }

    [Fact]
    public void N3_ExecuteBatch_ExternalScope_DefersCommitToCaller()
    {
        var editor = new CanvasEditor();
        using var scope = TestScopeFactory.CreateHeadlessScope();
        using var cts = new CancellationTokenSource();
        using var doc = JsonDocument.Parse("{\"componentsToAdd\": []}");

        var result = editor.ExecuteBatch(null, doc.RootElement, false, cts.Token, scope);

        Assert.NotNull(result);
        Assert.False(scope.IsCommitted);

        scope.Commit();
        Assert.True(scope.IsCommitted);
    }

    [Fact]
    public void N3_RevitTransactionScope_Cancellation_RollsBackBothRevitAndCanvas()
    {
        using var ctx = new SimulatedTransactionContext();
        var canvas = new TrackingCanvasScope();
        var req = new ExecuteRequest("cancel_midway", "auto", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (ct, _) =>
            {
                cts.Cancel();
                ct.ThrowIfCancellationRequested();
                return null;
            },
            canvasScope: canvas);

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.True(canvas.RollbackCalled);
        Assert.False(canvas.IsCommitted);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.Equal(1, ctx.RollbackAutoTxCount);
        Assert.Contains("Execution was cancelled. Nothing was committed.", result.Message);
    }

    [Fact]
    public void N3_TimeoutExceeded_RollsBackBothRevitAndCanvasTruthfully()
    {
        using var ctx = new SimulatedTransactionContext();
        var canvas = new TrackingCanvasScope();
        var req = new ExecuteRequest("timeout_op", "auto", false, TimeoutSeconds: 5);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (ct, _) =>
            {
                using var timeoutSimulator = new CancellationTokenSource();
                timeoutSimulator.Cancel();
                timeoutSimulator.Token.ThrowIfCancellationRequested();
                return null;
            },
            canvasScope: canvas);

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.True(canvas.RollbackCalled);
        Assert.False(canvas.IsCommitted);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }
}
