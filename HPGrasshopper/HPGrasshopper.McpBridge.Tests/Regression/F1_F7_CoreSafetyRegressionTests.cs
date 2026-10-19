namespace HPGrasshopper.McpBridge.Tests.Regression;

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.McpBridge.Coordinator;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Host;
using HPGrasshopper.McpBridge.Tests.Coordinator;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using ScriptAnalyzer = HPGrasshopper.McpBridge.Execution.ScriptAnalyzer;
using Xunit;

public sealed class F1_F7_CoreSafetyRegressionTests : IDisposable
{
    private sealed class AutoDispatchingEventRaiser(GrasshopperBridgeCoordinator coordinator) : IExternalEventRaiser
    {
        public bool Raise()
        {
            coordinator.Execute(null!);
            return true;
        }

        public void Dispose() { }
    }

    private readonly GrasshopperBridgeCoordinator _coordinator = GrasshopperBridgeCoordinator.Instance;
    private readonly BridgeSettings _settings = new() { ExecutionEnabled = true };
    private readonly GrasshopperRpcDispatcher _dispatcher;

    public F1_F7_CoreSafetyRegressionTests()
    {
        _coordinator.Initialize(_settings, new AutoDispatchingEventRaiser(_coordinator));
        _coordinator.ExecutionHook = null;
        _dispatcher = new GrasshopperRpcDispatcher(_coordinator, _settings);
    }

    public void Dispose()
    {
        _coordinator.ExecutionHook = null;
        _coordinator.Dispose();
    }

    [Theory]
    [InlineData("read_grasshopper_canvas")]
    [InlineData("export_grasshopper_canvas_image")]
    [InlineData("batch_edit_grasshopper_canvas")]
    [InlineData("run_grasshopper_player")]
    public async Task F1_NativeRpc_AllFourTools_RouteWithoutMethodNotFound(string toolName)
    {
        var env = JsonRpcEnvelope.Request(1, toolName, null);
        var result = await _dispatcher.DispatchAsync(1, env, null, CancellationToken.None);

        Assert.NotNull(result);
        if (result!.Error != null)
        {
            Assert.NotEqual(-32601, result.Error.Code);
        }
    }

    [Fact]
    public async Task F1_NativeRpc_WriteTools_WhenExecutionDisabled_RejectWith32001()
    {
        _settings.ExecutionEnabled = false;

        var batchEnv = JsonRpcEnvelope.Request(2, "batch_edit_grasshopper_canvas", null);
        var batchRes = await _dispatcher.DispatchAsync(2, batchEnv, null, CancellationToken.None);
        Assert.NotNull(batchRes?.Error);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, batchRes!.Error!.Code);

        var playerEnv = JsonRpcEnvelope.Request(3, "run_grasshopper_player", null);
        var playerRes = await _dispatcher.DispatchAsync(3, playerEnv, null, CancellationToken.None);
        Assert.NotNull(playerRes?.Error);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, playerRes!.Error!.Code);
    }

    [Fact]
    public async Task F1_NativeRpc_ReadTools_WhenExecutionDisabled_Succeed()
    {
        _settings.ExecutionEnabled = false;
        var env = JsonRpcEnvelope.Request(4, "read_grasshopper_canvas", null);
        var res = await _dispatcher.DispatchAsync(4, env, null, CancellationToken.None);

        Assert.NotNull(res);
        Assert.Null(res!.Error);
        Assert.NotNull(res.Result);
    }

    [Fact]
    public async Task F1_NativeRpc_WhenCoordinatorBusy_RejectWith32002()
    {
        Assert.True(_coordinator.TryAcquireBusy());
        try
        {
            var env = JsonRpcEnvelope.Request(5, "read_grasshopper_canvas", null);
            var res = await _dispatcher.DispatchAsync(5, env, null, CancellationToken.None);

            Assert.NotNull(res?.Error);
            Assert.Equal(BridgeErrorCode.Busy, res!.Error!.Code);
        }
        finally
        {
            _coordinator.ReleaseBusy();
        }
    }

    [Fact]
    public void F2_DualSafety_DryRun_RollsBackBothRevitAndCanvasScopes()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("create elements", "auto", true);
        using var cts = new CancellationTokenSource();

        var res = RevitTransactionScope.ExecuteWithContext(
            ctx, req, _settings, null, cts, (_, _) => "ok");

        Assert.False(res.IsError);
        Assert.True(res.RolledBack);
        Assert.Equal(1, ctx.GroupStartCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
        Assert.Equal(0, ctx.AssimilateGroupCount);
    }

    [Fact]
    public void F2_DualSafety_UnhandledException_RollsBackBothLayers()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("fail elements", "auto", false);
        using var cts = new CancellationTokenSource();

        var res = RevitTransactionScope.ExecuteWithContext(
            ctx, req, _settings, null, cts,
            (_, _) => throw new InvalidOperationException("Simulation crash"));

        Assert.True(res.IsError);
        Assert.True(res.RolledBack);
        Assert.Equal(1, ctx.RollbackAutoTxCount);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Fact]
    public void F2_DualSafety_Cancellation_AbortsBothLayersCleanly()
    {
        using var ctx = new SimulatedTransactionContext();
        var req = new ExecuteRequest("cancelled task", "auto", false);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var res = RevitTransactionScope.ExecuteWithContext(
            ctx, req, _settings, null, cts, (_, _) => "ok");

        Assert.True(res.IsError);
        Assert.True(res.RolledBack);
        Assert.Equal(1, ctx.RollbackGroupCount);
    }

    [Theory]
    [InlineData("using var t = new Transaction(doc, \"T\");")]
    [InlineData("Transaction t = new(doc, \"T\");")]
    [InlineData("SubTransaction st = new(doc);")]
    public void F3_ModeNone_RejectsExplicitAndTargetTypedTransactions(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, AnalyzerProfile.Grasshopper);
        Assert.True(analysis.UsesTransaction);
    }

    [Theory]
    [InlineData("var x = 42;")]
    [InlineData("List<string> items = new();")]
    [InlineData("double area = Math.PI * 100;")]
    public void F3_ModeNone_ValidReadOperations_ZeroFalsePositives(string code)
    {
        var analysis = ScriptAnalyzer.Analyze(code, AnalyzerProfile.Grasshopper);
        Assert.False(analysis.UsesTransaction);
    }

    [Fact]
    public async Task F5_CoordinatorQueue_CancellationWhileQueued_UnblocksImmediately()
    {
        var raiser = new FakeExternalEventRaiser { ReturnValue = true };
        _coordinator.Initialize(_settings, raiser);
        using var cts = new CancellationTokenSource();
        var work = new McpBridgeWorkRequest("QueuedTest", (_, _) => "ok", cts.Token);

        var task = _coordinator.RunOnRevitThreadAsync(work);
        Assert.Equal(WorkItemState.Queued, _coordinator.GetWorkItemState(work));

        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Equal(WorkItemState.Completed, _coordinator.GetWorkItemState(work));
    }

    [Fact]
    public async Task F5_CoordinatorQueue_RaiseDenied_ThrowsFastWithoutLeakingBusy()
    {
        var raiser = new FakeExternalEventRaiser { ReturnValue = false };
        _coordinator.Initialize(_settings, raiser);
        var work = new McpBridgeWorkRequest("DeniedTest", (_, _) => "ok", CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _coordinator.RunOnRevitThreadAsync(work));
        Assert.True(_coordinator.TryAcquireBusy());
        _coordinator.ReleaseBusy();
    }

    [Fact]
    public void F6_DocumentIdentity_MatchingDocument_PassesValidation()
    {
        var work = new McpBridgeWorkRequest("MatchTest", (_, _) => null, CancellationToken.None);
        work.ValidateActiveDocument(null);
    }

    [Fact]
    public void F6_DocumentIdentity_MismatchedTitleOrPath_ThrowsInvalidOperationException()
    {
        var work = new McpBridgeWorkRequest("MismatchTest", (_, _) => null, CancellationToken.None)
        {
            TargetDocTitle = "ProjectAlpha.rvt"
        };
        var ex = Assert.Throws<InvalidOperationException>(() => work.ValidateActiveDocument(null));
        Assert.Contains("Document identity mismatch", ex.Message);
    }

    [Fact]
    public void F7_CompilerInvalidation_ClearsAndRecreatesCompilerInstances()
    {
        var c1 = _coordinator.Compiler;
        Assert.NotNull(c1);

        _coordinator.InvalidateCompiler();

        var type = typeof(GrasshopperBridgeCoordinator);
        var compField = type.GetField("_compiler", BindingFlags.NonPublic | BindingFlags.Instance);
        var inspField = type.GetField("_inspector", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.Null(compField!.GetValue(_coordinator));
        Assert.Null(inspField!.GetValue(_coordinator));

        var c2 = _coordinator.Compiler;
        Assert.NotNull(c2);
    }
}
