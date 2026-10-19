namespace HPGrasshopper.McpBridge.Tests.Host;

using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using HPGrasshopper.McpBridge.Coordinator;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Host;
using HPGrasshopper.McpBridge.Tests.Fakes;
using HPGrasshopper.McpBridge.Tools;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Xunit;

public sealed class PublicRouteRegressionTests : IDisposable
{
    private sealed class AutoDispatchingEventRaiser(GrasshopperBridgeCoordinator coordinator) : IExternalEventRaiser
    {
        public bool Raise() { coordinator.Execute(null!); return true; }
        public void Dispose() { }
    }

    private readonly GrasshopperBridgeCoordinator _coordinator = GrasshopperBridgeCoordinator.Instance;
    private readonly BridgeSettings _settings = new() { ExecutionEnabled = true };
    private readonly GrasshopperRpcDispatcher _dispatcher;

    public PublicRouteRegressionTests()
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
    public async Task MissingFile_RunPlayer_ReturnsFailurePayloadWithDescriptiveError()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "MissingDef_" + Guid.NewGuid().ToString("N") + ".gh");
        var env = JsonRpcEnvelope.Request(101, "grasshopper.run_grasshopper_player", new
        {
            filePath = nonExistentPath,
            headless = true
        });

        var response = await _dispatcher.DispatchAsync(101, env, null, CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response!.Result);
        var elem = BridgeJson.ToElement(response.Result)!.Value;
        Assert.False(elem.GetProperty("success").GetBoolean());
        var error = elem.GetProperty("error").GetString();
        Assert.NotNull(error);
        Assert.Contains("Definition file not found", error);
    }

    [Fact]
    public async Task MissingFile_EmptyFilePath_RejectsWithInvalidRequestCode()
    {
        var env = JsonRpcEnvelope.Request(102, "grasshopper.run_grasshopper_player", new
        {
            filePath = string.Empty
        });

        var response = await _dispatcher.DispatchAsync(102, env, null, CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response!.Error);
        Assert.Equal(BridgeErrorCode.InvalidRequest, response.Error!.Code);
        Assert.Contains("filePath is required", response.Error.Message);
    }

    [Fact]
    public void PreflightBoundsRejection_InvalidPortBounds_CapturesDetailedDiagnostics()
    {
        using var jsonDoc = JsonDocument.Parse("""
        {
            "componentsToAdd": [
                { "type": "Addition", "tempId": "nodeA", "x": 100, "y": 100 }
            ],
            "wiresToConnect": [
                { "source": "nodeA", "sourceOutput": 999, "target": "nodeA", "targetInput": 0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(
            jsonDoc.RootElement,
            findObject: _ => null,
            canCreateObject: _ => true,
            getPortBounds: _ => (2, 1));

        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToConnect" && e.Reason.Contains("exceeds output port count"));
    }

    [Fact]
    public void PreflightConflictRejection_WireToRemovedComponent_FailsValidation()
    {
        var victimGuid = Guid.NewGuid();
        using var jsonDoc = JsonDocument.Parse($$"""
        {
            "componentsToRemove": [ "{{victimGuid}}" ],
            "wiresToConnect": [
                { "source": "{{victimGuid}}", "sourceOutput": 0, "target": "nodeB", "targetInput": 0 }
            ]
        }
        """);

        var preflight = CanvasBatchValidator.Preflight(jsonDoc.RootElement, findObject: _ => null);

        Assert.False(preflight.IsValid);
        Assert.Contains(preflight.Errors, e => e.Operation == "wiresToConnect" && e.Reason.Contains("marked for removal in componentsToRemove"));
    }

    [Fact]
    public async Task SolveError_DispatcherPropagatesErrorPayload()
    {
        _coordinator.ExecutionHook = _ =>
        {
            var simulatedFailure = new
            {
                success = false,
                durationMs = 25,
                error = "Solve failed with 1 runtime error(s).",
                errors = new[] { "[FAIL] Component execution failed (SolutionPhase.Failed)." }
            };
            return Task.FromResult(new ExecuteResult
            {
                IsError = true,
                Message = "Solve failed with 1 runtime error(s).",
                Value = BridgeJson.ToElement(simulatedFailure)
            });
        };

        var env = JsonRpcEnvelope.Request(103, "grasshopper.run_grasshopper_player", new { filePath = "mock_solve_error.gh" });
        var response = await _dispatcher.DispatchAsync(103, env, null, CancellationToken.None);

        Assert.NotNull(response?.Result);
        var elem = BridgeJson.ToElement(response!.Result)!.Value;
        Assert.False(elem.GetProperty("success").GetBoolean());
        Assert.Contains("Solve failed", elem.GetProperty("error").GetString());
        var errorsArray = elem.GetProperty("errors");
        Assert.True(errorsArray.GetArrayLength() > 0);
        Assert.Contains("SolutionPhase.Failed", errorsArray[0].GetString());
    }

    [Fact]
    public async Task DocumentIdentityMismatch_TargetDocTitle_FailsClosedAndRejectsExecution()
    {
        try
        {
            _coordinator.UpdateActiveSnapshot("ExpectedModel.rvt", @"C:\Models\ExpectedModel.rvt", Guid.NewGuid());

            var env = JsonRpcEnvelope.Request(104, "grasshopper.run_grasshopper_player", new
            {
                filePath = "dummy.gh",
                targetDocTitle = "WrongModel.rvt"
            });

            var response = await _dispatcher.DispatchAsync(104, env, null, CancellationToken.None);

            Assert.NotNull(response?.Error);
            Assert.Equal(BridgeErrorCode.InternalError, response!.Error!.Code);
            Assert.Contains("Document identity mismatch", response.Error.Message);
            Assert.Contains("WrongModel.rvt", response.Error.Message);
        }
        finally
        {
            _coordinator.UpdateActiveSnapshot(null, null, null);
        }
    }

    [Fact]
    public async Task DocumentIdentityMismatch_TargetDefId_FailsClosedAndRejectsExecution()
    {
        try
        {
            var activeDefId = Guid.NewGuid();
            _coordinator.UpdateActiveSnapshot(null, null, activeDefId);

            var wrongDefId = Guid.NewGuid();
            var env = JsonRpcEnvelope.Request(105, "grasshopper.batch_edit_grasshopper_canvas", new
            {
                dryRun = true,
                targetDefId = wrongDefId.ToString("D")
            });

            var response = await _dispatcher.DispatchAsync(105, env, null, CancellationToken.None);

            Assert.NotNull(response?.Error);
            Assert.Equal(BridgeErrorCode.InternalError, response!.Error!.Code);
            Assert.Contains("definition identity mismatch", response.Error.Message);
        }
        finally
        {
            _coordinator.UpdateActiveSnapshot(null, null, null);
        }
    }

    [Fact]
    public async Task Dispatcher_ParsesDryRunParamAndMapsRolledBackResult()
    {
        // Verifies dispatcher envelope extraction of dryRun=true and mapping to RolledBack in the result.
        // Genuine canvas rollback ledger and compensation mechanisms are verified in Tests 9 and 10.
        _coordinator.ExecutionHook = req =>
        {
            return Task.FromResult(new ExecuteResult
            {
                RolledBack = req.DryRun,
                Value = BridgeJson.ToElement(new
                {
                    success = true,
                    dryRun = req.DryRun,
                    rolledBack = req.DryRun,
                    added = 1,
                    removed = 0
                })
            });
        };

        var env = JsonRpcEnvelope.Request(106, "grasshopper.batch_edit_grasshopper_canvas", new
        {
            dryRun = true
        });

        var response = await _dispatcher.DispatchAsync(106, env, null, CancellationToken.None);

        Assert.NotNull(response?.Result);
        var elem = BridgeJson.ToElement(response!.Result)!.Value;
        Assert.True(elem.GetProperty("success").GetBoolean());
        Assert.True(elem.GetProperty("rolledBack").GetBoolean());
    }

    [Fact]
    public void AtomicRollback_TemporaryObjectAddedThenRemoved_LedgerDoesNotResurrect()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var tempGuid = Guid.NewGuid();

        scope.TrackAddedObject(tempGuid);

        var removedComp = TestProxyFactory.CreateComponent("TempNode", "TMP");
        var proxy = TestProxyFactory.GetProxy<TestComponentProxy>(removedComp);
        proxy.InstanceGuid = tempGuid;

        scope.TrackRemovedObject(removedComp);

        var rollbackResult = scope.Rollback();

        Assert.True(rollbackResult.Success);
        Assert.True(scope.RolledBackCleanly);
        Assert.Empty(scope.RollbackErrors);
    }

    [Fact]
    public void AtomicRollback_BatchException_TriggersCompensationAndReportsRolledBack()
    {
        using var scope = TestScopeFactory.CreateHeadlessScope();
        var handleMethod = typeof(CanvasEditor).GetMethod("HandleBatchException", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(handleMethod);

        var simulatedEx = new InvalidOperationException("Wire linkage failed during mutation");
        var result = handleMethod!.Invoke(null, new object[] { simulatedEx, scope });

        Assert.NotNull(result);
        var elem = BridgeJson.ToElement(result)!.Value;
        Assert.False(elem.GetProperty("success").GetBoolean());
        Assert.True(elem.GetProperty("rolledBack").GetBoolean());
        Assert.Contains("Wire linkage failed", elem.GetProperty("error").GetString());
    }
}
