namespace HPDynamo.McpBridge.Tests.Coordinator;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dynamo.Models;
using HPDynamo.McpBridge.Coordinator;
using HPDynamo.McpBridge.Execution;
using HPDynamo.McpBridge.Support;
using HPDynamo.McpBridge.Tests.Fakes;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Xunit;

/// <summary>
///     Adversarial stress-test harness for Milestone 2 (RV-02, RV-03, RV-05).
///     Empirically stress-tests DynamoEvaluationSync, CanvasGraphRunner, linkedToken propagation,
///     and WorkItemTracker state machine concurrency.
/// </summary>
public sealed class Milestone2ChallengerTests
{
    private static void RaiseEvaluationCompleted(DynamoModel model, bool succeeded)
    {
        FieldInfo? field = null;
        for (var t = typeof(DynamoModel); t != null && field == null; t = t.BaseType)
        {
            field = t.GetField(nameof(DynamoModel.EvaluationCompleted), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null)
            {
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                    if (f.FieldType == typeof(EventHandler<EvaluationCompletedEventArgs>)) { field = f; break; }
            }
        }

        var handler = (EventHandler<EvaluationCompletedEventArgs>?)field?.GetValue(model);
        if (handler != null)
        {
            var args = succeeded
                ? new EvaluationCompletedEventArgs(true, null!)
                : new EvaluationCompletedEventArgs(true, new InvalidOperationException("Simulated Dynamo evaluation error"));
            handler.Invoke(model, args);
        }
    }

    // =========================================================================
    // 1. RV-02A: DynamoEvaluationSync Stress Testing
    // =========================================================================

    [Fact]
    public void EvaluationCompletedEventArgs_EvaluationSucceeded_ReflectsPresenceOfError()
    {
        var successArgs = new EvaluationCompletedEventArgs(true, null!);
        var failArgs = new EvaluationCompletedEventArgs(true, new InvalidOperationException("Eval failed"));
        Assert.True(successArgs.EvaluationSucceeded);
        Assert.False(failArgs.EvaluationSucceeded);
    }

    [Fact]
    public void DynamoEvaluationSync_WhenRunActionThrowsException_ReturnsFalseAndCleansUp()
    {
        var model = (DynamoModel)RuntimeHelpers.GetUninitializedObject(typeof(DynamoModel));
        bool result = DynamoEvaluationSync.RunAndWait(
            model, () => throw new InvalidOperationException("Immediate evaluation dispatch error"),
            TimeSpan.FromSeconds(5), CancellationToken.None, _ => { });
        Assert.False(result);
    }

    [Fact]
    public void DynamoEvaluationSync_WhenEvaluationSucceeds_ReturnsTrue()
    {
        var model = (DynamoModel)RuntimeHelpers.GetUninitializedObject(typeof(DynamoModel));
        bool result = DynamoEvaluationSync.RunAndWait(
            model, () => RaiseEvaluationCompleted(model, true),
            TimeSpan.FromSeconds(5), CancellationToken.None, _ => { });
        Assert.True(result);
    }

    [Fact]
    public void DynamoEvaluationSync_WhenEvaluationFails_ReturnsFalse()
    {
        var model = (DynamoModel)RuntimeHelpers.GetUninitializedObject(typeof(DynamoModel));
        bool result = DynamoEvaluationSync.RunAndWait(
            model, () => RaiseEvaluationCompleted(model, false),
            TimeSpan.FromSeconds(5), CancellationToken.None, _ => { });
        Assert.False(result);
    }

    [Fact]
    public void DynamoEvaluationSync_WhenTokenPreCancelled_ReturnsFalsePromptly()
    {
        var model = (DynamoModel)RuntimeHelpers.GetUninitializedObject(typeof(DynamoModel));
        var logs = new List<string>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        bool result = DynamoEvaluationSync.RunAndWait(
            model, () => { }, TimeSpan.FromSeconds(5), cts.Token, msg => { if (msg != null) logs.Add(msg); });

        Assert.False(result);
        Assert.Contains(logs, l => l.Contains("cancelled", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DynamoEvaluationSync_WhenTimeoutExpires_ReturnsFalse()
    {
        var model = (DynamoModel)RuntimeHelpers.GetUninitializedObject(typeof(DynamoModel));
        var logs = new List<string>();

        bool result = DynamoEvaluationSync.RunAndWait(
            model, () => { }, TimeSpan.FromMilliseconds(50), CancellationToken.None, msg => { if (msg != null) logs.Add(msg); });

        Assert.False(result);
        Assert.Contains(logs, l => l.Contains("timed out", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DynamoEvaluationSync_WhenCancellationTriggeredMidWait_ReturnsFalse()
    {
        var model = (DynamoModel)RuntimeHelpers.GetUninitializedObject(typeof(DynamoModel));
        var logs = new List<string>();
        using var cts = new CancellationTokenSource();
        Task.Delay(50).ContinueWith(_ => cts.Cancel());

        bool result = DynamoEvaluationSync.RunAndWait(
            model, () => { }, TimeSpan.FromSeconds(5), cts.Token, msg => { if (msg != null) logs.Add(msg); });

        Assert.False(result);
        Assert.Contains(logs, l => l.Contains("cancelled", StringComparison.OrdinalIgnoreCase));
    }

    // =========================================================================
    // 2. RV-02A: CanvasGraphRunner Error Throwing
    // =========================================================================

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t")]
    public void CanvasGraphRunner_ExecuteDesignScript_EmptyCode_ThrowsArgumentException(string code)
    {
        var runner = new CanvasGraphRunner();
        var vm = (Dynamo.ViewModels.DynamoViewModel)RuntimeHelpers.GetUninitializedObject(typeof(Dynamo.ViewModels.DynamoViewModel));
        Assert.Throws<ArgumentException>(() =>
            runner.ExecuteDesignScript(vm, code, null, false, CancellationToken.None, _ => { }));
    }

    [Fact]
    public void CanvasGraphRunner_RunDynFile_MissingFile_ThrowsFileNotFoundException()
    {
        var runner = new CanvasGraphRunner();
        var vm = (Dynamo.ViewModels.DynamoViewModel)RuntimeHelpers.GetUninitializedObject(typeof(Dynamo.ViewModels.DynamoViewModel));
        Assert.Throws<System.IO.FileNotFoundException>(() =>
            runner.RunDynFile(vm, "G:\\Missing\\NonExistentGraph.dyn", null, false, CancellationToken.None, _ => { }));
    }

    // =========================================================================
    // 3. RV-03: linkedToken & RevitTransactionScope Rollback
    // =========================================================================

    [Fact]
    public void RevitTransactionScope_LinkedToken_PropagatesCancellationFromCancelSource()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("return 1;", "auto");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, new BridgeSettings(), null, cts,
            (linkedToken, _) =>
            {
                Assert.True(linkedToken.IsCancellationRequested);
                throw new OperationCanceledException(linkedToken);
            });

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.False(result.TimedOut);
        Assert.Contains("cancelled", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Fact]
    public void RevitTransactionScope_WhenActionThrowsInvalidOperationException_TriggersSafeRollback()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("return 1;", "auto");
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, new BridgeSettings(), null, cts,
            (linkedToken, _) =>
            {
                throw new InvalidOperationException("Dynamo active graph evaluation completed with 2 node(s) in error state.");
            });

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("InvalidOperationException", result.Message);
        Assert.Equal(1, ctx.RollbackAutoTxCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Fact]
    public void RevitTransactionScope_DryRunTrue_AlwaysRollsBackGroupEvenOnSuccess()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("return 1;", "auto", true);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, new BridgeSettings(), null, cts,
            (linkedToken, log) =>
            {
                log("Node created successfully");
                return new { Created = true };
            });

        Assert.False(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    // =========================================================================
    // 4. RV-05: WorkItemTracker Multi-Threaded Invariants
    // =========================================================================

    [Fact]
    public void WorkItemTracker_ConcurrentTransitionAndCancel_MaintainsConsistentState()
    {
        for (int round = 0; round < 20; round++)
        {
            var tracker = new WorkItemTracker();
            var tcs = new TaskCompletionSource<object?>();
            using var cts = new CancellationTokenSource();
            bool runningWon = false;
            bool cancelWon = false;

            Parallel.Invoke(
                () => { if (tracker.TryTransitionToRunning()) runningWon = true; },
                () => { if (tracker.TryCancelIfQueued(tcs, cts.Token)) cancelWon = true; }
            );

            Assert.True(runningWon ^ cancelWon, $"Round {round}: exactly one operation must win.");
            if (runningWon)
            {
                Assert.Equal(WorkItemState.Running, tracker.State);
                Assert.False(tcs.Task.IsCanceled);
            }
            else
            {
                Assert.Equal(WorkItemState.Completed, tracker.State);
                Assert.True(tcs.Task.IsCanceled);
            }
        }
    }

    [Fact]
    public void WorkItemTracker_ConcurrentTransitionToRunning_ExactlyOneWins()
    {
        var tracker = new WorkItemTracker();
        int winCount = 0;

        Parallel.For(0, 50, _ =>
        {
            if (tracker.TryTransitionToRunning())
                Interlocked.Increment(ref winCount);
        });

        Assert.Equal(1, winCount);
        Assert.Equal(WorkItemState.Running, tracker.State);
    }
}
