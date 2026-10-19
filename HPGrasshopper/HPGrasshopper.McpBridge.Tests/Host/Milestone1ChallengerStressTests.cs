namespace HPGrasshopper.McpBridge.Tests.Host;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.McpBridge.Coordinator;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Host;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Xunit;

/// <summary>
///     Adversarial stress harness for Milestone M1 (F1 Native RPC Routing & F11 Error Dispatch).
///     Empirically validates:
///     1. Rapid sequential calls (50 calls) without busy leaks.
///     2. Concurrency isolation and Busy code (-32002) under contention.
///     3. Busy lock release guarantee when work delegate or hook throws exceptions.
///     4. Argument edge cases: null params, invalid types (array/string/number), empty paths.
///     5. Execution failure result mapping (F11) corner cases.
/// </summary>
public sealed class Milestone1ChallengerStressTests : IDisposable
{
    private sealed class ImmediateRaiser(GrasshopperBridgeCoordinator coordinator) : IExternalEventRaiser
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
    private readonly ImmediateRaiser _raiser;
    private readonly GrasshopperRpcDispatcher _dispatcher;

    public Milestone1ChallengerStressTests()
    {
        _raiser = new ImmediateRaiser(_coordinator);
        _coordinator.Initialize(_settings, _raiser);
        _coordinator.ExecutionHook = null;
        _dispatcher = new GrasshopperRpcDispatcher(_coordinator, _settings);
    }

    public void Dispose()
    {
        _coordinator.ExecutionHook = null;
        _coordinator.Dispose();
    }

    [Fact]
    public async Task RapidSequentialCalls_50Calls_SucceedWithoutBusyLeak()
    {
        for (int i = 1; i <= 50; i++)
        {
            var method = (i % 2 == 0)
                ? "grasshopper.read_grasshopper_canvas"
                : "grasshopper.export_grasshopper_canvas_image";

            var env = JsonRpcEnvelope.Request(i, method, new { zoom = 1.0, returnBase64 = false });
            var response = await _dispatcher.DispatchAsync(i, env, null, CancellationToken.None);

            Assert.NotNull(response);
            Assert.Equal(i, response!.Id);
            Assert.Null(response.Error);
            Assert.NotNull(response.Result);
            Assert.False(_coordinator.IsBusy, $"Coordinator leaked busy flag on iteration {i}");
        }
    }

    [Fact]
    public async Task ConcurrencyStress_Contention_ReturnsBusyAndRecovers()
    {
        var tcs = new TaskCompletionSource<bool>();
        _coordinator.ExecutionHook = async _ =>
        {
            await tcs.Task;
            return new ExecuteResult { Value = BridgeJson.ToElement(new { success = true }) };
        };

        var initialTask = _dispatcher.DispatchAsync(100, JsonRpcEnvelope.Request(100, "grasshopper.read_grasshopper_canvas", null), null, CancellationToken.None);
        await Task.Delay(20);

        Assert.True(_coordinator.IsBusy);

        var contendingTasks = Enumerable.Range(101, 10).Select(async id =>
        {
            var env = JsonRpcEnvelope.Request(id, "grasshopper.read_grasshopper_canvas", null);
            return await _dispatcher.DispatchAsync(id, env, null, CancellationToken.None);
        }).ToArray();

        var contendingResults = await Task.WhenAll(contendingTasks);
        foreach (var res in contendingResults)
        {
            Assert.NotNull(res);
            Assert.NotNull(res!.Error);
            Assert.Equal(BridgeErrorCode.Busy, res.Error!.Code);
        }

        tcs.SetResult(true);
        var initialResult = await initialTask;
        Assert.NotNull(initialResult);
        Assert.Null(initialResult!.Error);
        Assert.False(_coordinator.IsBusy);

        _coordinator.ExecutionHook = null;
        var recoveryRes = await _dispatcher.DispatchAsync(200, JsonRpcEnvelope.Request(200, "grasshopper.read_grasshopper_canvas", null), null, CancellationToken.None);
        Assert.NotNull(recoveryRes);
        Assert.Null(recoveryRes!.Error);
    }

    [Fact]
    public async Task ExceptionSafety_WhenHookThrowsException_BusyIsReleased()
    {
        _coordinator.ExecutionHook = _ => throw new InvalidOperationException("Simulated catastrophic delegate crash");

        var env = JsonRpcEnvelope.Request(301, "grasshopper.read_grasshopper_canvas", null);
        var response = await _dispatcher.DispatchAsync(301, env, null, CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response!.Error);
        Assert.Equal(BridgeErrorCode.InternalError, response.Error!.Code);
        Assert.Contains("InvalidOperationException", response.Error.Message);
        Assert.False(_coordinator.IsBusy, "Busy must be released even after thrown exception");

        _coordinator.ExecutionHook = null;
        var subsequentResponse = await _dispatcher.DispatchAsync(302, env, null, CancellationToken.None);
        Assert.NotNull(subsequentResponse);
        Assert.Null(subsequentResponse!.Error);
    }

    [Fact]
    public async Task ExceptionSafety_WhenOperationCanceled_BusyIsReleased()
    {
        _coordinator.ExecutionHook = _ => throw new OperationCanceledException();

        var env = JsonRpcEnvelope.Request(303, "grasshopper.read_grasshopper_canvas", null);
        var response = await _dispatcher.DispatchAsync(303, env, null, CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response!.Error);
        Assert.Equal(BridgeErrorCode.InternalError, response.Error!.Code);
        Assert.Contains("cancelled", response.Error.Message);
        Assert.False(_coordinator.IsBusy);

        _coordinator.ExecutionHook = null;
    }

    [Theory]
    [InlineData("[1, 2, 3]")]
    [InlineData("\"unexpected string payload\"")]
    [InlineData("12345")]
    [InlineData("true")]
    public async Task EdgeCaseArguments_InvalidJsonTypes_HandledSafelyWithoutBusyLeak(string rawJson)
    {
        var rawElement = JsonSerializer.Deserialize<JsonElement>(rawJson);
        var env = JsonRpcEnvelope.Request(401, "grasshopper.read_grasshopper_canvas", rawElement);

        var response = await _dispatcher.DispatchAsync(401, env, null, CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response!.Error);
        Assert.Equal(BridgeErrorCode.InternalError, response.Error!.Code);
        Assert.False(_coordinator.IsBusy, "Busy must be released on deserialization failure");

        var recovery = await _dispatcher.DispatchAsync(402, JsonRpcEnvelope.Request(402, "grasshopper.read_grasshopper_canvas", null), null, CancellationToken.None);
        Assert.NotNull(recovery);
        Assert.Null(recovery!.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public async Task EdgeCaseArguments_RunPlayer_BlankPaths_ReturnInvalidRequest(string? path)
    {
        var env = JsonRpcEnvelope.Request(501, "grasshopper.run_grasshopper_player", new { filePath = path });
        var response = await _dispatcher.DispatchAsync(501, env, null, CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotNull(response!.Error);
        Assert.Equal(BridgeErrorCode.InvalidRequest, response.Error!.Code);
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public async Task EdgeCaseArguments_BatchEdit_NullParams_DefaultsAndExecutes()
    {
        var env = JsonRpcEnvelope.Request(601, "grasshopper.batch_edit_grasshopper_canvas", null);
        var response = await _dispatcher.DispatchAsync(601, env, null, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Null(response!.Error);
        Assert.False(_coordinator.IsBusy);
    }

    [Theory]
    [InlineData("GRASSHOPPER.READ_GRASSHOPPER_CANVAS")]
    [InlineData("Grasshopper.Export_Grasshopper_Canvas_Image")]
    [InlineData("read_grasshopper_canvas")]
    [InlineData("export_grasshopper_canvas_image")]
    public async Task MethodNameCasing_CaseInsensitiveRouting_Succeeds(string methodName)
    {
        var env = JsonRpcEnvelope.Request(701, methodName, null);
        var response = await _dispatcher.DispatchAsync(701, env, null, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Null(response!.Error);
        Assert.False(_coordinator.IsBusy);
    }

    [Theory]
    [InlineData(null, true, "Execution returned null result", false)]
    [InlineData("{\"success\":false}", true, "Operation failed", false)]
    [InlineData("{\"success\":false,\"error\":\"Custom solver failure\",\"rolledBack\":true}", true, "Custom solver failure", true)]
    [InlineData("{\"success\":true,\"durationMs\":100}", false, null, false)]
    [InlineData("{\"otherProperty\":\"noSuccessField\"}", false, null, false)]
    public void F11_FailureMapping_AdversarialScenarios(string? json, bool expectError, string? expectedMsg, bool expectRolledBack)
    {
        object? input = json == null ? null : JsonSerializer.Deserialize<JsonElement>(json);
        var res = GrasshopperExecutionEngine.MapToExecuteResult(input);

        Assert.Equal(expectError, res.IsError);
        if (expectedMsg != null) Assert.Equal(expectedMsg, res.Message);
        Assert.Equal(expectRolledBack, res.RolledBack);
    }
}
