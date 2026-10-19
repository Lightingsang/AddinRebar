namespace HPDynamo.McpBridge.Tests.Coordinator;

using System; using System.Collections.Generic; using System.IO; using System.IO.Pipes; using System.Text; using System.Threading; using System.Threading.Tasks;
using HPDynamo.McpBridge.Coordinator; using HPDynamo.McpBridge.Execution; using HPDynamo.McpBridge.Tests.Fakes;
using HPRebar.Mcp.Contracts.JsonRpc; using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model; using HPRebar.McpBridge.Core.Pipe; using Xunit;

public sealed class MilestoneARound2EmpiricalChallengeTests
{
    private static DynamoBridgeCoordinator CreateCoordinator()
    {
        var coordinator = DynamoBridgeCoordinator.Instance;
        coordinator.Initialize(new BridgeSettings { ExecutionEnabled = true }, null);
        coordinator.UnregisterLiveCanvas();
        return coordinator;
    }

    [Fact]
    public async Task R204_LateCancel_AfterSuccessfulExecution_PreservesCommittedTruth()
    {
        var coordinator = CreateCoordinator();
        var execGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        coordinator.ExecutionHook = async _ =>
        {
            await execGate.Task;
            return new ExecuteResult { IsError = false, Message = "Committed to DB", RolledBack = false };
        };

        try
        {
            var execTask = coordinator.ExecuteAsync(3001L, new ExecuteRequest("doc.Commit();", "auto"), null, CancellationToken.None);
            while (!coordinator.IsBusy) await Task.Yield();

            execGate.SetResult(true);
            var cancelRes = coordinator.Cancel(3001L);

            var result = await execTask;
            Assert.False(result.IsError);
            Assert.False(result.RolledBack);
            Assert.Equal("Committed to DB", result.Message);
            Assert.False(coordinator.IsBusy);
        }
        finally
        {
            coordinator.ExecutionHook = null;
            execGate.TrySetResult(true);
        }
    }

    [Fact]
    public void R204_RevitTransactionScope_LateCancelAfterAssimilate_PreservesCommitTruth()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("return 1;", "auto", false);
        using var cts = new CancellationTokenSource();
        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, new BridgeSettings { ExecutionEnabled = true }, null, cts,
            (token, _) => 42);

        cts.Cancel();
        Assert.False(result.IsError);
        Assert.False(result.RolledBack);
        Assert.Equal(1, ctx.CommitAutoTxCount);
        Assert.Equal(1, ctx.AssimilateGroupCount);
        Assert.Equal(0, ctx.RollbackGroupCount);
    }

    [Fact]
    public void R205_TransactionScope_NodeEvaluationError_RollsBackCleanly()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("throw on error node", "auto", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, new BridgeSettings { ExecutionEnabled = true }, null, cts,
            (_, _) => throw new InvalidOperationException("Headless graph execution failed with 2 error node(s): Circle; Line"));

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("failed with 2 error node(s)", result.Message);
        Assert.Equal(1, ctx.RollbackAutoTxCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.Equal(0, ctx.CommitAutoTxCount);
        Assert.Equal(0, ctx.AssimilateGroupCount);
    }

    [Fact]
    public void R205_TransactionScope_EvaluationTimeoutOrFailure_RollsBackCleanly()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("timeout evaluation", "auto", false);
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            ctx, req, new BridgeSettings { ExecutionEnabled = true }, null, cts,
            (_, _) => throw new InvalidOperationException("Dynamo evaluation failed or timed out for graph 'test.dyn'."));

        Assert.True(result.IsError);
        Assert.True(result.RolledBack);
        Assert.Contains("failed or timed out", result.Message);
        Assert.Equal(1, ctx.RollbackAutoTxCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Fact]
    public async Task R206_SchedulerDrain_CoordinatorRetainsBusyUntilCompletion()
    {
        var coordinator = CreateCoordinator();
        var drainGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        coordinator.ExecutionHook = async _ =>
        {
            await drainGate.Task;
            return new ExecuteResult { IsError = false, Message = "Drained" };
        };

        try
        {
            var execTask = coordinator.ExecuteAsync(3002L, new ExecuteRequest("drain", "none"), null, CancellationToken.None);
            while (!coordinator.IsBusy) await Task.Yield();

            Assert.True(coordinator.IsBusy);

            var concurrentReq = coordinator.ExecuteAsync(3003L, new ExecuteRequest("drain2", "none"), null, CancellationToken.None);
            var rej = await concurrentReq;
            Assert.True(rej.IsError);
            Assert.Contains("Another Dynamo script or graph is still running", rej.Message);

            drainGate.SetResult(true);
            var finalRes = await execTask;
            Assert.False(finalRes.IsError);
            Assert.False(coordinator.IsBusy);
        }
        finally
        {
            coordinator.ExecutionHook = null;
            drainGate.TrySetResult(true);
        }
    }

    [Theory]
    [InlineData("// TransactionManager.Instance.EnsureInTransaction(doc);\ndoc.Create.NewWall(c, l, false);")]
    [InlineData("// new Transaction(doc);\ndoc.Create.NewFamilyInstance(xyz);")]
    [InlineData("/* TransactionManager.Instance */ doc.Create.NewWall(c, l, false);")]
    [InlineData("/* new Transaction(doc); */ doc.Create.NewWall(c, l, false);")]
    [InlineData("/* Revit.Elements.Wall */ doc.Create.NewWall(c, l, false);")]
    [InlineData("string msg = \"TransactionManager\"; doc.Delete(id);")]
    [InlineData("string msg = \"TransactionManager.Instance\"; doc.Delete(id);")]
    [InlineData("string msg = \"new Transaction(doc)\"; doc.Delete(id);")]
    [InlineData("var log = @\"new TransactionGroup(doc)\"; doc.Delete(id);")]
    [InlineData("var log = \"\"\"new SubTransaction(doc)\"\"\"; doc.Delete(id);")]
    [InlineData("int TransactionManagerIndex = 42; doc.Create.NewWall(c, l, false);")]
    [InlineData("string TransactionName = \"WallCreation\"; doc.Create.NewWall(c, l, false);")]
    public void R207_AstDetector_CommentsAndStrings_AllowAutoTransaction(string code)
    {
        Assert.True(DynamoExecutionEngine.ShouldStartAutoTransaction(code, "auto"));
    }

    [Theory]
    [InlineData("new Transaction(doc);")]
    [InlineData("new Autodesk.Revit.DB.Transaction(doc);")]
    [InlineData("new TransactionGroup(doc);")]
    [InlineData("new Autodesk.Revit.DB.TransactionGroup(doc);")]
    [InlineData("new SubTransaction(doc);")]
    [InlineData("new Autodesk.Revit.DB.SubTransaction(doc);")]
    [InlineData("TransactionManager.Instance.EnsureInTransaction(doc);")]
    [InlineData("Revit.Elements.Wall.ByCurveAndHeight(c, 100, l);")]
    [InlineData("Dynamo.Applications.DynamoRevit.RevitDynamoModel.ToString();")]
    [InlineData("ws.Run();")]
    [InlineData("homeWs.Run();")]
    [InlineData("activeCanvasWs.Run();")]
    [InlineData("ws?.Run();")]
    [InlineData("homeWs?.Run();")]
    [InlineData("activeCanvasWs?.Run();")]
    public void R207_AstDetector_RealTransactions_DisableAutoTransaction(string code)
    {
        Assert.False(DynamoExecutionEngine.ShouldStartAutoTransaction(code, "auto"));
    }

    [Fact]
    public void R207_AstDetector_MalformedSyntax_HandlesSafelyWithoutException()
    {
        const string brokenCode = "class Invalid { void Broken( { int x = ; }";
        Assert.True(DynamoExecutionEngine.ShouldStartAutoTransaction(brokenCode, "auto"));
    }

    [Fact]
    public async Task R209_SessionIsolation_SameRequestId_DifferentSession_NotCancelled()
    {
        var coordinator = CreateCoordinator();
        var hold = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        coordinator.ExecutionHook = async _ =>
        {
            await hold.Task;
            return new ExecuteResult { IsError = false, Message = "Session 1 Completed" };
        };

        try
        {
            var req = new ExecuteRequest("x = 10;", "none", SessionId: "sess-alpha");
            var execTask = coordinator.ExecuteAsync(9999L, req, null, CancellationToken.None);
            while (!coordinator.IsBusy) await Task.Yield();

            var foreignCancel = coordinator.Cancel(9999L, "sess-beta");
            Assert.False(foreignCancel.Cancelled);
            Assert.True(foreignCancel.WasRunning);
            Assert.True(coordinator.IsBusy);

            var ownCancel = coordinator.Cancel(9999L, "sess-alpha");
            Assert.True(ownCancel.Cancelled);
            Assert.True(ownCancel.WasRunning);

            hold.SetResult(true);
            await execTask;
        }
        finally
        {
            coordinator.ExecutionHook = null;
            hold.TrySetResult(true);
        }
    }

    [Fact]
    public async Task R209_RequestDispatcher_SelfCancelIsNoOp()
    {
        var coordinator = CreateCoordinator();
        var pipeName = "hpdynamo-selfcancel-" + Guid.NewGuid().ToString("N");
        var settings = new BridgeSettings { ExecutionEnabled = true };
        using var listener = new PipeListener(pipeName, new RequestDispatcher(coordinator, settings, "2026", "Dynamo"));
        listener.Start();

        try
        {
            using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await client.ConnectAsync(3000, CancellationToken.None);

            var selfCancelEnvelope = JsonRpcEnvelope.Request(42L, "dynamo.cancel", new CancelParams(42L));
            var payload = Encoding.UTF8.GetBytes(BridgeJson.Serialize(selfCancelEnvelope) + "\n");
            await client.WriteAsync(payload, 0, payload.Length);
            await client.FlushAsync();

            using var reader = new StreamReader(client, Encoding.UTF8, false, 4096, leaveOpen: true);
            var responseLine = await reader.ReadLineAsync();

            Assert.NotNull(responseLine);
            var reply = BridgeJson.Deserialize<JsonRpcEnvelope>(responseLine);
            Assert.NotNull(reply);
            Assert.Equal(42L, reply.Id);
            Assert.Null(reply.Error);

            var cancelRes = reply.ResultAs<CancelResult>();
            Assert.NotNull(cancelRes);
            Assert.False(cancelRes.Cancelled);
            Assert.False(cancelRes.WasRunning);
        }
        finally
        {
            await listener.StopAsync();
        }
    }

    [Fact]
    public async Task R209_ReentrantCancel_DoesNotDeadlockOrRecurse()
    {
        var coordinator = CreateCoordinator();
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        coordinator.ExecutionHook = async _ =>
        {
            await gate.Task;
            throw new OperationCanceledException();
        };

        try
        {
            var execTask = coordinator.ExecuteAsync(3004L, new ExecuteRequest("reentrant", "none"), null, CancellationToken.None);
            while (!coordinator.IsBusy) await Task.Yield();

            var outerCancel = coordinator.Cancel(3004L);
            Assert.True(outerCancel.Cancelled);

            var innerCancel = coordinator.Cancel(3004L);
            Assert.True(innerCancel.WasRunning);

            gate.SetResult(true);
            var res = await execTask;
            Assert.True(res.IsError);
        }
        finally
        {
            coordinator.ExecutionHook = null;
            gate.TrySetResult(true);
        }
    }
}
