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

/// <summary>
///     Adversarial empirical challenge suite specifically stress-testing Finding R3:
///     Synchronous Dual Commit Ordering across Revit Transaction Scope and Canvas Undo Scope.
/// </summary>
public sealed class FindingR3CommitOrderingAdversarialTests
{
    private static readonly BridgeSettings DefaultSettings = new() { ExecutionEnabled = true };

    private sealed class ChronologicalCanvasScope(List<string> timeline) : ICanvasUndoScope
    {
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }
        public bool ThrowOnRollback { get; init; }
        public bool IsCommitted { get; private set; }
        public void Commit() { CommitCount++; IsCommitted = true; timeline.Add("Canvas:Commit"); }
        public CanvasRollbackResult Rollback()
        {
            RollbackCount++;
            timeline.Add("Canvas:Rollback");
            if (ThrowOnRollback) throw new InvalidOperationException("Adversarial rollback fault");
            return CanvasRollbackResult.Clean();
        }
        public void Dispose() { if (!IsCommitted && RollbackCount == 0) Rollback(); }
    }

    private sealed class ChronologicalTransactionContext(List<string> timeline) : ITransactionContext
    {
        public bool IsGroupStarted { get; private set; }
        public bool IsAutoTransactionStarted { get; private set; }
        public IReadOnlyList<string> Warnings => Array.Empty<string>();
        public bool AutoTransactionCommitResult { get; init; } = true;
        public int RollbackGroupCount { get; private set; }
        public void StartGroup(string label) { IsGroupStarted = true; timeline.Add("Revit:StartGroup"); }
        public void StartAutoTransaction(string label) { IsAutoTransactionStarted = true; timeline.Add("Revit:StartAutoTx"); }
        public bool CommitAutoTransaction()
        {
            IsAutoTransactionStarted = false;
            timeline.Add($"Revit:CommitAutoTx({AutoTransactionCommitResult})");
            return AutoTransactionCommitResult;
        }
        public void RollbackAutoTransaction() { IsAutoTransactionStarted = false; timeline.Add("Revit:RollbackAutoTx"); }
        public void RollbackGroup() { IsGroupStarted = false; RollbackGroupCount++; timeline.Add("Revit:RollbackGroup"); }
        public void AssimilateGroup() { IsGroupStarted = false; timeline.Add("Revit:AssimilateGroup"); }
        public void Dispose() { IsAutoTransactionStarted = false; IsGroupStarted = false; }
    }

    [Fact]
    public void FindingR3_CommitOrdering_CanvasCommitsStrictlyAfterRevitCommitAndAssimilate()
    {
        var timeline = new List<string>();
        using var ctx = new ChronologicalTransactionContext(timeline);
        var canvas = new ChronologicalCanvasScope(timeline);
        var req = new ExecuteRequest("create elements", "auto", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => { timeline.Add("Action:Execute"); return "ok"; }, canvasScope: canvas);

        Assert.False(result.IsError);
        Assert.False(result.RolledBack);
        Assert.Equal(1, canvas.CommitCount);
        Assert.Equal(0, canvas.RollbackCount);
        var idxCommitTx = timeline.IndexOf("Revit:CommitAutoTx(True)");
        var idxAssimilate = timeline.IndexOf("Revit:AssimilateGroup");
        var idxCanvasCommit = timeline.IndexOf("Canvas:Commit");
        Assert.True(idxCanvasCommit > idxCommitTx, "Canvas MUST commit strictly AFTER Revit transaction commits");
        Assert.True(idxCanvasCommit > idxAssimilate, "Canvas MUST commit strictly AFTER Revit group assimilates");
    }

    [Fact]
    public void FindingR3_RevitTxRejection_RollsBackCanvasAndNeverCommits()
    {
        var timeline = new List<string>();
        using var ctx = new ChronologicalTransactionContext(timeline) { AutoTransactionCommitResult = false };
        var canvas = new ChronologicalCanvasScope(timeline);
        var req = new ExecuteRequest("faulty element", "auto", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "attempted", canvasScope: canvas);

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("Revit rejected the change", result.Message);
        Assert.Equal(0, canvas.CommitCount);
        Assert.Equal(1, canvas.RollbackCount);
        Assert.Contains("Canvas:Rollback", timeline);
        Assert.DoesNotContain("Canvas:Commit", timeline);
    }

    [Fact]
    public void FindingR3_DryRunTrue_RollsBackBothAndCanvasNeverCommits()
    {
        var timeline = new List<string>();
        using var ctx = new ChronologicalTransactionContext(timeline);
        var canvas = new ChronologicalCanvasScope(timeline);
        var req = new ExecuteRequest("preview", "auto", true);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, log) => { log("dry-run"); return "preview"; }, canvasScope: canvas);

        Assert.False(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Equal(0, canvas.CommitCount);
        Assert.Equal(1, canvas.RollbackCount);
        Assert.Contains("Canvas:Rollback", timeline);
        Assert.Contains("Revit:RollbackGroup", timeline);
        Assert.DoesNotContain("Canvas:Commit", timeline);
        Assert.Contains(result.Logs, l => l.Contains("Canvas modifications rolled back cleanly"));
        Assert.Contains(result.Logs, l => l.Contains("All Revit document modifications rolled back cleanly"));
    }

    [Fact]
    public void FindingR3_Cancellation_RollsBackCanvasAndNeverCommits()
    {
        var timeline = new List<string>();
        using var ctx = new ChronologicalTransactionContext(timeline);
        var canvas = new ChronologicalCanvasScope(timeline);
        var req = new ExecuteRequest("cancelled task", "auto", false);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "aborted", canvasScope: canvas);

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("cancelled", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, canvas.CommitCount);
        Assert.Equal(1, canvas.RollbackCount);
        Assert.Contains("Canvas:Rollback", timeline);
        Assert.DoesNotContain("Canvas:Commit", timeline);
    }

    [Fact]
    public void FindingR3_TimeoutCancellation_RollsBackCanvasAndMarksTimedOut()
    {
        var timeline = new List<string>();
        using var ctx = new ChronologicalTransactionContext(timeline);
        var canvas = new ChronologicalCanvasScope(timeline);
        var req = new ExecuteRequest("timed out task", "auto", false) { TimeoutSeconds = 5 };
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (linkedToken, _) => throw new OperationCanceledException(linkedToken), canvasScope: canvas);

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Equal(0, canvas.CommitCount);
        Assert.Equal(1, canvas.RollbackCount);
        Assert.Contains("Canvas:Rollback", timeline);
        Assert.DoesNotContain("Canvas:Commit", timeline);
    }

    [Fact]
    public void FindingR3_ScriptThrowsException_RollsBackCanvasAndPreservesError()
    {
        var timeline = new List<string>();
        using var ctx = new ChronologicalTransactionContext(timeline);
        var canvas = new ChronologicalCanvasScope(timeline);
        var req = new ExecuteRequest("failing task", "auto", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => throw new InvalidOperationException("Severe simulation defect"), canvasScope: canvas);

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("Severe simulation defect", result.Message);
        Assert.Equal(0, canvas.CommitCount);
        Assert.Equal(1, canvas.RollbackCount);
        Assert.Contains("Canvas:Rollback", timeline);
        Assert.DoesNotContain("Canvas:Commit", timeline);
    }

    [Fact]
    public void FindingR3_ManualMode_ScriptFault_RollsBackBothRevitGroupAndCanvas()
    {
        var timeline = new List<string>();
        using var ctx = new ChronologicalTransactionContext(timeline);
        var canvas = new ChronologicalCanvasScope(timeline);
        var req = new ExecuteRequest("manual tx", "manual", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => throw new InvalidOperationException("Manual transaction failed to commit"),
            canvasScope: canvas);

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("Manual transaction failed to commit", result.Message);
        Assert.Equal(0, canvas.CommitCount);
        Assert.Equal(1, canvas.RollbackCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.Contains("Canvas:Rollback", timeline);
        Assert.Contains("Revit:RollbackGroup", timeline);
        Assert.DoesNotContain("Canvas:Commit", timeline);
    }

    [Fact]
    public void FindingR3_ModeNone_DocModifiableTrue_RollsBackCanvasAndNeverCommits()
    {
        var timeline = new List<string>();
        using var ctx = new ChronologicalTransactionContext(timeline);
        var canvas = new ChronologicalCanvasScope(timeline);
        var req = new ExecuteRequest("uncommitted tx", "none", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => "uncommitted", isDocModifiable: true, canvasScope: canvas);

        Assert.True(result.IsError);
        Assert.False(result.RolledBack);
        Assert.Contains("The script modified the document or left a transaction open", result.Message);
        Assert.Equal(0, canvas.CommitCount);
        Assert.Equal(1, canvas.RollbackCount);
        Assert.Contains("Canvas:Rollback", timeline);
        Assert.DoesNotContain("Canvas:Commit", timeline);
    }

    [Fact]
    public void FindingR3_DoubleFault_CanvasRollbackThrows_RevitRollbackStillExecutes()
    {
        var timeline = new List<string>();
        using var ctx = new ChronologicalTransactionContext(timeline);
        var canvas = new ChronologicalCanvasScope(timeline) { ThrowOnRollback = true };
        var req = new ExecuteRequest("double fault", "auto", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, _) => throw new InvalidOperationException("Original error"), canvasScope: canvas);

        Assert.True(result.IsError);
        Assert.False(result.RolledBack);
        Assert.Contains("Original error", result.Message);
        Assert.Contains("Additionally, canvas rollback failed", result.Message);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.Contains("Revit:RollbackGroup", timeline);
    }

    [Fact]
    public void FindingR3_DoubleFault_CanvasRollbackThrowsDuringDryRun_RevitRollbackSucceeds()
    {
        var timeline = new List<string>();
        using var ctx = new ChronologicalTransactionContext(timeline);
        var canvas = new ChronologicalCanvasScope(timeline) { ThrowOnRollback = true };
        var req = new ExecuteRequest("preview double fault", "auto", true);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, DefaultSettings, null, cts,
            (_, log) => { log("dry-run"); return "preview"; }, canvasScope: canvas);

        Assert.True(result.IsError);
        Assert.False(result.RolledBack);
        Assert.Contains("Canvas rollback failed", result.Message);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.Contains("Revit:RollbackGroup", timeline);
    }

    [Fact]
    public void FindingR3_CreateScriptWorkRequest_InspectSignatureAndCanvasScopeWiring()
    {
        var engineType = typeof(GrasshopperExecutionEngine);
        var method = engineType.GetMethod("CreateScriptWorkRequest", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var scopeInterface = typeof(ICanvasUndoScope);
        Assert.NotNull(scopeInterface.GetMethod("Commit"));
        Assert.NotNull(scopeInterface.GetMethod("Rollback"));
        Assert.NotNull(scopeInterface.GetProperty("IsCommitted"));
    }
}
