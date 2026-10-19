namespace ChallengerM1Harness;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.McpBridge.Coordinator;
using HPGrasshopper.McpBridge.Execution;
using HPGrasshopper.McpBridge.Host;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Xunit;

public sealed class ChallengerM1StressTests : IDisposable
{
    private sealed class AutoDispatchingEventRaiser(GrasshopperBridgeCoordinator coordinator) : IExternalEventRaiser
    {
        private static readonly MethodInfo? ExecuteMethod =
            typeof(GrasshopperBridgeCoordinator).GetMethod("Execute", BindingFlags.Instance | BindingFlags.Public);

        public bool Raise()
        {
            ExecuteMethod?.Invoke(coordinator, [null!]);
            return true;
        }

        public void Dispose() { }
    }

    private readonly GrasshopperBridgeCoordinator _coordinator = GrasshopperBridgeCoordinator.Instance;
    private readonly BridgeSettings _settings = new() { ExecutionEnabled = true };
    private readonly AutoDispatchingEventRaiser _raiser;
    private readonly GrasshopperRpcDispatcher _dispatcher;

    public ChallengerM1StressTests()
    {
        _raiser = new AutoDispatchingEventRaiser(_coordinator);
        _coordinator.Initialize(_settings, _raiser);
        SetExecutionHook(_coordinator, null);
        _dispatcher = new GrasshopperRpcDispatcher(_coordinator, _settings);
    }

    public void Dispose()
    {
        SetExecutionHook(_coordinator, null);
        _coordinator.Dispose();
        _settings.ExecutionEnabled = true;
    }

    private static bool TryAcquireBusy(GrasshopperBridgeCoordinator coordinator)
    {
        var m = typeof(GrasshopperBridgeCoordinator).GetMethod("TryAcquireBusy", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(m);
        return (bool)m!.Invoke(coordinator, null)!;
    }

    private static void ReleaseBusy(GrasshopperBridgeCoordinator coordinator)
    {
        var m = typeof(GrasshopperBridgeCoordinator).GetMethod("ReleaseBusy", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(m);
        m!.Invoke(coordinator, null);
    }

    private static void SetExecutionHook(GrasshopperBridgeCoordinator coordinator, Func<ExecuteRequest, Task<ExecuteResult>>? hook)
    {
        var p = typeof(GrasshopperBridgeCoordinator).GetProperty("ExecutionHook", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(p);
        p!.SetValue(coordinator, hook);
    }

    private static ExecuteResult InvokeMapToExecuteResult(object? data)
    {
        var method = typeof(GrasshopperExecutionEngine).GetMethod(
            "MapToExecuteResult",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return (ExecuteResult)method!.Invoke(null, [data])!;
    }

    #region Area 1: Routing & Method Resolution

    [Fact]
    public async Task Routing_MethodNullOrEmptyOrWhitespace_ReturnsNull()
    {
        var envNull = new JsonRpcEnvelope { Id = 100, Method = null };
        var resNull = await _dispatcher.DispatchAsync(100, envNull, null, CancellationToken.None);
        Assert.Null(resNull);

        var envEmpty = JsonRpcEnvelope.Request(101, "", null);
        var resEmpty = await _dispatcher.DispatchAsync(101, envEmpty, null, CancellationToken.None);
        Assert.Null(resEmpty);

        var envWhitespace = JsonRpcEnvelope.Request(102, "   \t\n  ", null);
        var resWhitespace = await _dispatcher.DispatchAsync(102, envWhitespace, null, CancellationToken.None);
        Assert.Null(resWhitespace);
    }

    [Theory]
    [InlineData("grasshopper.nonexistent_tool_123")]
    [InlineData("grasshopper.invalid_command")]
    [InlineData("grasshopper.undefined")]
    [InlineData("grasshopper.execute")]
    [InlineData("grasshopper.context")]
    [InlineData("grasshopper.ping")]
    public async Task Routing_UnknownGrasshopperMethod_ReturnsNull_ForDefaultDispatcherFallback(string unknownMethod)
    {
        var env = JsonRpcEnvelope.Request(200, unknownMethod, null);
        var res = await _dispatcher.DispatchAsync(200, env, null, CancellationToken.None);
        Assert.Null(res);
    }

    [Theory]
    [InlineData("revit.read_grasshopper_canvas")]
    [InlineData("autocad.read_grasshopper_canvas")]
    [InlineData("dynamo.run_grasshopper_player")]
    [InlineData("custom.tool_test")]
    public async Task Routing_ForeignHostPrefix_ReturnsNull(string foreignMethod)
    {
        var env = JsonRpcEnvelope.Request(300, foreignMethod, null);
        var res = await _dispatcher.DispatchAsync(300, env, null, CancellationToken.None);
        Assert.Null(res);
    }

    [Theory]
    [InlineData("read_grasshopper_canvas")]
    [InlineData("run_grasshopper_player")]
    [InlineData("batch_edit_grasshopper_canvas")]
    [InlineData("export_grasshopper_canvas_image")]
    public async Task Routing_UnprefixedNativeTools_AreRecognized(string nativeTool)
    {
        object? payload = nativeTool == "run_grasshopper_player"
            ? new { filePath = "test.gh", headless = true }
            : null;

        var env = JsonRpcEnvelope.Request(400, nativeTool, payload);
        var res = await _dispatcher.DispatchAsync(400, env, null, CancellationToken.None);
        Assert.NotNull(res);
        Assert.Null(res!.Error);
    }

    [Theory]
    [InlineData("GRASSHOPPER.READ_GRASSHOPPER_CANVAS")]
    [InlineData("Grasshopper.Run_Grasshopper_Player")]
    [InlineData("Grasshopper.Batch_Edit_Grasshopper_Canvas")]
    [InlineData("GRASSHOPPER.EXPORT_GRASSHOPPER_CANVAS_IMAGE")]
    public async Task Routing_CaseInsensitivity_Succeeds(string methodName)
    {
        object? payload = methodName.EndsWith("Player", StringComparison.OrdinalIgnoreCase)
            ? new { filePath = "test.gh", headless = true }
            : null;

        var env = JsonRpcEnvelope.Request(500, methodName, payload);
        var res = await _dispatcher.DispatchAsync(500, env, null, CancellationToken.None);
        Assert.NotNull(res);
        Assert.Null(res!.Error);
    }

    [Theory]
    [InlineData("grasshopper.show_window")]
    [InlineData("revit.show_window")]
    [InlineData("GRASSHOPPER.SHOW_WINDOW")]
    [InlineData("REVIT.SHOW_WINDOW")]
    public async Task Routing_ShowWindow_TriggersActionAndReturnsSuccess(string showMethod)
    {
        var callCount = 0;
        var dispatcher = new GrasshopperRpcDispatcher(
            _coordinator,
            _settings,
            showWindow: () => callCount++);

        var env = JsonRpcEnvelope.Request(600, showMethod, null);
        var res = await dispatcher.DispatchAsync(600, env, null, CancellationToken.None);

        Assert.NotNull(res);
        Assert.Null(res!.Error);
        Assert.Equal(1, callCount);
        var json = res.Result?.ToString();
        Assert.Contains("shown", json);
    }

    #endregion

    #region Area 2: Opt-In Safety Enforcement (F1)

    [Theory]
    [InlineData("grasshopper.run_grasshopper_player")]
    [InlineData("grasshopper.batch_edit_grasshopper_canvas")]
    [InlineData("run_grasshopper_player")]
    [InlineData("batch_edit_grasshopper_canvas")]
    public async Task OptIn_WriteTools_BlockedWithExecutionDisabled_WhenOptInFalse(string writeTool)
    {
        _settings.ExecutionEnabled = false;

        object? payload = writeTool.Contains("player", StringComparison.OrdinalIgnoreCase)
            ? new { filePath = "sample.gh" }
            : new { dryRun = true };

        var env = JsonRpcEnvelope.Request(700, writeTool, payload);
        var res = await _dispatcher.DispatchAsync(700, env, null, CancellationToken.None);

        Assert.NotNull(res);
        Assert.NotNull(res!.Error);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, res.Error!.Code);
        Assert.Equal(-32001, (int)res.Error.Code);
        Assert.Contains("Execution is disabled in bridge settings", res.Error.Message);
    }

    [Theory]
    [InlineData("grasshopper.read_grasshopper_canvas")]
    [InlineData("grasshopper.export_grasshopper_canvas_image")]
    [InlineData("read_grasshopper_canvas")]
    [InlineData("export_grasshopper_canvas_image")]
    public async Task OptIn_ReadTools_Allowed_WhenOptInFalse(string readTool)
    {
        _settings.ExecutionEnabled = false;

        var env = JsonRpcEnvelope.Request(800, readTool, null);
        var res = await _dispatcher.DispatchAsync(800, env, null, CancellationToken.None);

        Assert.NotNull(res);
        Assert.Null(res!.Error);
        Assert.NotNull(res.Result);
    }

    [Fact]
    public async Task OptIn_DynamicToggle_ReactsImmediatelyWithoutRestart()
    {
        var playerEnv = JsonRpcEnvelope.Request(900, "grasshopper.run_grasshopper_player", new { filePath = "test.gh" });

        // Phase 1: Disabled -> Fails
        _settings.ExecutionEnabled = false;
        var res1 = await _dispatcher.DispatchAsync(900, playerEnv, null, CancellationToken.None);
        Assert.NotNull(res1?.Error);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, res1!.Error!.Code);

        // Phase 2: Enabled -> Succeeds
        _settings.ExecutionEnabled = true;
        var res2 = await _dispatcher.DispatchAsync(901, playerEnv, null, CancellationToken.None);
        Assert.NotNull(res2);
        Assert.Null(res2!.Error);

        // Phase 3: Disabled again -> Fails
        _settings.ExecutionEnabled = false;
        var res3 = await _dispatcher.DispatchAsync(902, playerEnv, null, CancellationToken.None);
        Assert.NotNull(res3?.Error);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, res3!.Error!.Code);
    }

    #endregion

    #region Area 3: Concurrency & Busy Lock Contention (F1)

    [Theory]
    [InlineData("grasshopper.read_grasshopper_canvas")]
    [InlineData("grasshopper.run_grasshopper_player")]
    [InlineData("grasshopper.batch_edit_grasshopper_canvas")]
    [InlineData("grasshopper.export_grasshopper_canvas_image")]
    public async Task Concurrency_ReturnsBusy_WhenCoordinatorBusy(string toolMethod)
    {
        _settings.ExecutionEnabled = true;
        Assert.True(TryAcquireBusy(_coordinator));

        try
        {
            object? payload = toolMethod.Contains("player") ? new { filePath = "model.gh" } : null;
            var env = JsonRpcEnvelope.Request(1000, toolMethod, payload);
            var res = await _dispatcher.DispatchAsync(1000, env, null, CancellationToken.None);

            Assert.NotNull(res);
            Assert.NotNull(res!.Error);
            Assert.Equal(BridgeErrorCode.Busy, res.Error!.Code);
            Assert.Equal(-32002, (int)res.Error.Code);
            Assert.Contains("Another Grasshopper script or task is still running", res.Error.Message);
        }
        finally
        {
            ReleaseBusy(_coordinator);
        }

        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public async Task Concurrency_BusyLock_AlwaysReleased_WhenExecutionThrowsException()
    {
        _settings.ExecutionEnabled = true;
        SetExecutionHook(_coordinator, _ => throw new InvalidOperationException("Simulated unexpected crash in runner"));

        var env = JsonRpcEnvelope.Request(1100, "grasshopper.read_grasshopper_canvas", null);
        var res = await _dispatcher.DispatchAsync(1100, env, null, CancellationToken.None);

        Assert.NotNull(res);
        Assert.NotNull(res!.Error);
        Assert.Equal(BridgeErrorCode.InternalError, res.Error!.Code);
        Assert.Contains("InvalidOperationException", res.Error.Message);

        // CRITICAL CHECK: Lock MUST be released even after unexpected exception
        Assert.False(_coordinator.IsBusy);

        SetExecutionHook(_coordinator, null);
    }

    [Fact]
    public async Task Concurrency_HighContention_NoLockLeakOrDeadlock()
    {
        _settings.ExecutionEnabled = true;
        const int concurrentCount = 40;
        var tasks = new List<Task<JsonRpcEnvelope?>>();

        for (int i = 0; i < concurrentCount; i++)
        {
            var id = 2000 + i;
            var env = JsonRpcEnvelope.Request(id, "grasshopper.read_grasshopper_canvas", null);
            tasks.Add(Task.Run(() => _dispatcher.DispatchAsync(id, env, null, CancellationToken.None)));
        }

        var results = await Task.WhenAll(tasks);

        int successCount = 0;
        int busyCount = 0;

        foreach (var r in results)
        {
            Assert.NotNull(r);
            if (r!.Error == null)
            {
                successCount++;
            }
            else if (r.Error.Code == BridgeErrorCode.Busy)
            {
                busyCount++;
            }
            else
            {
                Assert.Fail($"Unexpected error code: {r.Error.Code} with message {r.Error.Message}");
            }
        }

        Assert.True(successCount >= 1, "At least one request should succeed");
        Assert.Equal(concurrentCount, successCount + busyCount);

        // After all concurrent requests complete, coordinator must NOT be busy
        Assert.False(_coordinator.IsBusy);
    }

    #endregion

    #region Area 4: Malformed Parameters & Boundary Edge Cases (F1)

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task Params_RunPlayer_EmptyOrWhitespaceFilePath_ReturnsInvalidRequest(string? invalidPath)
    {
        _settings.ExecutionEnabled = true;
        object? payload = invalidPath == null ? new { } : new { filePath = invalidPath };

        var env = JsonRpcEnvelope.Request(3000, "grasshopper.run_grasshopper_player", payload);
        var res = await _dispatcher.DispatchAsync(3000, env, null, CancellationToken.None);

        Assert.NotNull(res);
        Assert.NotNull(res!.Error);
        Assert.Equal(BridgeErrorCode.InvalidRequest, res.Error!.Code);
        Assert.Equal(-32600, (int)res.Error.Code);
        Assert.Contains("filePath is required", res.Error.Message);
    }

    [Fact]
    public async Task Params_RunPlayer_NullParams_ReturnsInvalidRequest()
    {
        _settings.ExecutionEnabled = true;
        var env = JsonRpcEnvelope.Request(3010, "grasshopper.run_grasshopper_player", null);
        var res = await _dispatcher.DispatchAsync(3010, env, null, CancellationToken.None);

        Assert.NotNull(res);
        Assert.NotNull(res!.Error);
        Assert.Equal(BridgeErrorCode.InvalidRequest, res.Error!.Code);
        Assert.Contains("filePath is required", res.Error.Message);
    }

    [Fact]
    public async Task Params_BatchEdit_NullOrPrimitiveParams_DoesNotCrash()
    {
        _settings.ExecutionEnabled = true;

        var envNull = JsonRpcEnvelope.Request(3020, "grasshopper.batch_edit_grasshopper_canvas", null);
        var resNull = await _dispatcher.DispatchAsync(3020, envNull, null, CancellationToken.None);
        Assert.NotNull(resNull);
        Assert.Null(resNull!.Error);

        var envInt = JsonRpcEnvelope.Request(3021, "grasshopper.batch_edit_grasshopper_canvas", 42);
        var resInt = await _dispatcher.DispatchAsync(3021, envInt, null, CancellationToken.None);
        Assert.NotNull(resInt);
        Assert.NotNull(resInt!.Error);
        Assert.Contains("JsonException", resInt.Error.Message);
        Assert.False(_coordinator.IsBusy);
    }

    [Fact]
    public async Task Params_ExportImage_NegativeZoomAndNullParams_HandlesCleanly()
    {
        _settings.ExecutionEnabled = true;

        var envNull = JsonRpcEnvelope.Request(3030, "grasshopper.export_grasshopper_canvas_image", null);
        var resNull = await _dispatcher.DispatchAsync(3030, envNull, null, CancellationToken.None);
        Assert.NotNull(resNull);
        Assert.Null(resNull!.Error);

        var envNegative = JsonRpcEnvelope.Request(3031, "grasshopper.export_grasshopper_canvas_image", new { zoom = -10.0, mode = "NonExistent" });
        var resNegative = await _dispatcher.DispatchAsync(3031, envNegative, null, CancellationToken.None);
        Assert.NotNull(resNegative);
        Assert.Null(resNegative!.Error);
    }

    #endregion

    #region Area 5: Execution Failure Mapping (F11 - MapToExecuteResult)

    [Fact]
    public void FailureMapping_NullData_ReturnsIsErrorTrue()
    {
        var res = InvokeMapToExecuteResult(null);
        Assert.True(res.IsError);
        Assert.Equal("Execution returned null result", res.Message);
    }

    [Fact]
    public void FailureMapping_SuccessFalse_WithExplicitErrorAndRolledBack_MapsProperly()
    {
        var payload = new
        {
            success = false,
            error = "Rhino geometry conversion failed on Curve 4",
            rolledBack = true
        };

        var res = InvokeMapToExecuteResult(payload);

        Assert.True(res.IsError);
        Assert.Equal("Rhino geometry conversion failed on Curve 4", res.Message);
        Assert.True(res.RolledBack);
        Assert.NotNull(res.Value);
    }

    [Fact]
    public void FailureMapping_SuccessFalse_WithoutErrorProperty_UsesFallbackMessage()
    {
        var payload = new { success = false };
        var res = InvokeMapToExecuteResult(payload);

        Assert.True(res.IsError);
        Assert.Equal("Operation failed", res.Message);
        Assert.False(res.RolledBack);
    }

    [Fact]
    public void FailureMapping_SuccessFalse_WithNullError_UsesFallbackMessage()
    {
        var json = "{\"success\": false, \"error\": null, \"rolledBack\": true}";
        using var doc = JsonDocument.Parse(json);
        var res = InvokeMapToExecuteResult(doc.RootElement);

        Assert.True(res.IsError);
        Assert.Equal("Operation failed", res.Message);
        Assert.True(res.RolledBack);
    }

    [Fact]
    public void FailureMapping_SuccessTrue_IsNotError()
    {
        var payload = new
        {
            success = true,
            durationMs = 25.5,
            inputsApplied = 3
        };

        var res = InvokeMapToExecuteResult(payload);

        Assert.False(res.IsError);
        Assert.Null(res.Message);
        Assert.NotNull(res.Value);
    }

    [Fact]
    public void FailureMapping_ObjectWithoutSuccessProperty_IsNotError()
    {
        var payload = new { componentCount = 15, wireCount = 20 };
        var res = InvokeMapToExecuteResult(payload);

        Assert.False(res.IsError);
        Assert.Null(res.Message);
    }

    [Fact]
    public void FailureMapping_PrimitivesAndArrays_AreNotErrors()
    {
        var resInt = InvokeMapToExecuteResult(42);
        Assert.False(resInt.IsError);

        var resString = InvokeMapToExecuteResult("Success string");
        Assert.False(resString.IsError);

        var resArray = InvokeMapToExecuteResult(new[] { "a", "b", "c" });
        Assert.False(resArray.IsError);
    }

    [Fact]
    public void FailureMapping_JsonElementDirect_MapsAccurately()
    {
        var json = "{\"success\":false,\"error\":\"Cannot find definition\",\"rolledBack\":false}";
        using var doc = JsonDocument.Parse(json);
        var res = InvokeMapToExecuteResult(doc.RootElement);

        Assert.True(res.IsError);
        Assert.Equal("Cannot find definition", res.Message);
        Assert.False(res.RolledBack);
    }

    #endregion

    #region Area 6: ID Correlation & Resiliency

    [Fact]
    public async Task Correlation_LargeId_PreservesExactValue()
    {
        const long largeId = 987654321012345678L;
        var env = JsonRpcEnvelope.Request(largeId, "grasshopper.read_grasshopper_canvas", null);
        var res = await _dispatcher.DispatchAsync(largeId, env, null, CancellationToken.None);

        Assert.NotNull(res);
        Assert.Equal(largeId, res!.Id);
    }

    [Fact]
    public async Task Cancellation_PreCancelledToken_DoesNotDeadlockOrLeakLock()
    {
        _settings.ExecutionEnabled = true;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var env = JsonRpcEnvelope.Request(4000, "grasshopper.read_grasshopper_canvas", null);
        var res = await _dispatcher.DispatchAsync(4000, env, null, cts.Token);

        Assert.NotNull(res);
        Assert.NotNull(res!.Error);
        Assert.Equal(BridgeErrorCode.InternalError, res.Error!.Code);
        Assert.Contains("cancelled", res.Error.Message, StringComparison.OrdinalIgnoreCase);

        Assert.False(_coordinator.IsBusy);
    }

    #endregion
}
