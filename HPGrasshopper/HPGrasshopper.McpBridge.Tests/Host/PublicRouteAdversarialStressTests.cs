namespace HPGrasshopper.McpBridge.Tests.Host;

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.McpBridge.Coordinator;
using HPGrasshopper.McpBridge.Host;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Xunit;

public sealed class PublicRouteAdversarialStressTests : IDisposable
{
    private sealed class AutoDispatchingEventRaiser(GrasshopperBridgeCoordinator coordinator) : IExternalEventRaiser
    {
        public bool Raise() { coordinator.Execute(null!); return true; }
        public void Dispose() { }
    }

    private readonly GrasshopperBridgeCoordinator _coordinator = GrasshopperBridgeCoordinator.Instance;
    private readonly BridgeSettings _settings = new() { ExecutionEnabled = true };
    private readonly GrasshopperRpcDispatcher _dispatcher;

    public PublicRouteAdversarialStressTests()
    {
        _coordinator.UpdateActiveSnapshot(null, null, null);
        _coordinator.Initialize(_settings, new AutoDispatchingEventRaiser(_coordinator));
        _coordinator.ExecutionHook = null;
        _dispatcher = new GrasshopperRpcDispatcher(_coordinator, _settings);
    }

    public void Dispose()
    {
        _coordinator.ExecutionHook = null;
        _coordinator.UpdateActiveSnapshot(null, null, null);
        _coordinator.Dispose();
    }

    [Fact]
    public async Task DocumentIdentityMismatch_RapidConsecutiveCalls_NeverCorruptsCoordinatorSnapshot()
    {
        var expectedGuid = Guid.NewGuid();
        _coordinator.UpdateActiveSnapshot("Hospital_Tower.rvt", @"D:\Bim\Hospital_Tower.rvt", expectedGuid);

        try
        {
            _coordinator.ExecutionHook = null;

            for (var i = 0; i < 20; i++)
            {
                var badEnv = JsonRpcEnvelope.Request(i * 2 + 1, "grasshopper.run_grasshopper_player", new
                {
                    filePath = "model.gh",
                    targetDocTitle = "Malicious_Override.rvt"
                });
                var badResp = await _dispatcher.DispatchAsync(i * 2 + 1, badEnv, null, CancellationToken.None);

                Assert.NotNull(badResp?.Error);
                Assert.Equal(BridgeErrorCode.InternalError, badResp!.Error!.Code);
                Assert.Contains("Document identity mismatch", badResp.Error.Message);

                Assert.Equal("Hospital_Tower.rvt", _coordinator.ActiveDocumentTitle);
                Assert.Equal(@"D:\Bim\Hospital_Tower.rvt", _coordinator.ActiveDocumentPath);
                Assert.Equal(expectedGuid, _coordinator.ActiveDefinitionId);
                Assert.False(_coordinator.IsBusy);
            }
        }
        finally
        {
            _coordinator.UpdateActiveSnapshot(null, null, null);
        }
    }

    [Fact]
    public async Task PublicRoute_CancellationMidFlight_ReleasesCoordinatorBusyFlagImmediately()
    {
        var startedTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var holdTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _coordinator.ExecutionHook = async _ =>
        {
            startedTcs.TrySetResult(true);
            await holdTcs.Task.ConfigureAwait(false);
            return new ExecuteResult { Value = BridgeJson.ToElement(new { success = true }) };
        };

        using var cts = new CancellationTokenSource();
        var env = JsonRpcEnvelope.Request(201, "grasshopper.batch_edit_grasshopper_canvas", new { dryRun = true });

        var dispatchTask = _dispatcher.DispatchAsync(201, env, null, cts.Token);
        await startedTcs.Task.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.True(_coordinator.IsBusy);

        cts.Cancel();
        holdTcs.TrySetCanceled();

        var response = await dispatchTask;

        Assert.NotNull(response);
        Assert.False(_coordinator.IsBusy);

        _coordinator.ExecutionHook = _ => Task.FromResult(new ExecuteResult
        {
            Value = BridgeJson.ToElement(new { success = true, recovered = true })
        });

        var nextEnv = JsonRpcEnvelope.Request(202, "grasshopper.read_grasshopper_canvas", new { });
        var nextResp = await _dispatcher.DispatchAsync(202, nextEnv, null, CancellationToken.None);

        Assert.NotNull(nextResp?.Result);
        var elem = BridgeJson.ToElement(nextResp!.Result)!.Value;
        Assert.True(elem.GetProperty("recovered").GetBoolean());
    }

    [Fact]
    public async Task PublicRoute_ExecutionDisabled_WriteMethodsRejectWithoutAcquiringBusy()
    {
        _settings.ExecutionEnabled = false;

        var playerEnv = JsonRpcEnvelope.Request(301, "grasshopper.run_grasshopper_player", new { filePath = "test.gh" });
        var playerResp = await _dispatcher.DispatchAsync(301, playerEnv, null, CancellationToken.None);

        Assert.NotNull(playerResp?.Error);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, playerResp!.Error!.Code);
        Assert.Contains("Execution is disabled", playerResp.Error.Message);
        Assert.False(_coordinator.IsBusy);

        var batchEnv = JsonRpcEnvelope.Request(302, "grasshopper.batch_edit_grasshopper_canvas", new { dryRun = false });
        var batchResp = await _dispatcher.DispatchAsync(302, batchEnv, null, CancellationToken.None);

        Assert.NotNull(batchResp?.Error);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, batchResp!.Error!.Code);
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public async Task PublicRoute_InvalidAndMalformedEnvelopes_ConsistentlyReturnSafeErrorEnvelopes()
    {
        var emptyMethodEnv = JsonRpcEnvelope.Request(401, string.Empty, new { });
        var emptyResp = await _dispatcher.DispatchAsync(401, emptyMethodEnv, null, CancellationToken.None);
        Assert.Null(emptyResp);

        var unknownEnv = JsonRpcEnvelope.Request(402, "grasshopper.non_existent_tool", new { });
        var unknownResp = await _dispatcher.DispatchAsync(402, unknownEnv, null, CancellationToken.None);
        Assert.Null(unknownResp);

        var emptyFileEnv = JsonRpcEnvelope.Request(403, "grasshopper.run_grasshopper_player", new { filePath = "   " });
        var emptyFileResp = await _dispatcher.DispatchAsync(403, emptyFileEnv, null, CancellationToken.None);
        Assert.NotNull(emptyFileResp?.Error);
        Assert.Equal(BridgeErrorCode.InvalidRequest, emptyFileResp!.Error!.Code);

        var missingPath = Path.Combine(Path.GetTempPath(), "AbsentDef_" + Guid.NewGuid().ToString("N") + ".gh");
        var missingFileEnv = JsonRpcEnvelope.Request(404, "grasshopper.run_grasshopper_player", new { filePath = missingPath });
        var missingFileResp = await _dispatcher.DispatchAsync(404, missingFileEnv, null, CancellationToken.None);
        Assert.NotNull(missingFileResp?.Result);
        var elem = BridgeJson.ToElement(missingFileResp!.Result)!.Value;
        Assert.False(elem.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task PublicRoute_TargetDefIdMismatch_FailsClosedUnderConsecutiveAttempts()
    {
        var targetDefId = Guid.NewGuid();
        _coordinator.UpdateActiveSnapshot(null, null, targetDefId);

        try
        {
            for (var i = 0; i < 10; i++)
            {
                var alienGuid = Guid.NewGuid();
                var env = JsonRpcEnvelope.Request(500 + i, "grasshopper.batch_edit_grasshopper_canvas", new
                {
                    dryRun = true,
                    targetDefId = alienGuid.ToString("D")
                });

                var resp = await _dispatcher.DispatchAsync(500 + i, env, null, CancellationToken.None);

                Assert.NotNull(resp?.Error);
                Assert.Equal(BridgeErrorCode.InternalError, resp!.Error!.Code);
                Assert.Contains("definition identity mismatch", resp.Error.Message);
                Assert.False(_coordinator.IsBusy);
            }
        }
        finally
        {
            _coordinator.UpdateActiveSnapshot(null, null, null);
        }
    }
}
