namespace HPGrasshopper.McpBridge.Tests.Host;

using System;
using System.IO;
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

public sealed class GrasshopperRpcDispatcherTests : IDisposable
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
    private readonly AutoDispatchingEventRaiser _raiser;
    private readonly GrasshopperRpcDispatcher _dispatcher;

    public GrasshopperRpcDispatcherTests()
    {
        _raiser = new AutoDispatchingEventRaiser(_coordinator);
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
    public async Task DispatchAsync_EmptyMethod_ReturnsNull()
    {
        var env = JsonRpcEnvelope.Request(1, "", null);
        var result = await _dispatcher.DispatchAsync(1, env, null, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task DispatchAsync_UnknownMethod_ReturnsNull()
    {
        var env = JsonRpcEnvelope.Request(2, "grasshopper.unknown_tool_xyz", null);
        var result = await _dispatcher.DispatchAsync(2, env, null, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task DispatchAsync_NonGrasshopperPrefix_ReturnsNull()
    {
        var env = JsonRpcEnvelope.Request(3, "revit.read_grasshopper_canvas", null);
        var result = await _dispatcher.DispatchAsync(3, env, null, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task DispatchAsync_ShowWindow_ReturnsSuccess()
    {
        var env = JsonRpcEnvelope.Request(4, "grasshopper.show_window", null);
        var result = await _dispatcher.DispatchAsync(4, env, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result!.Error);
        Assert.NotNull(result.Result);
        var json = result.Result.ToString();
        Assert.Contains("shown", json);
    }

    [Fact]
    public async Task DispatchAsync_ReadCanvas_RoutesAndSucceeds()
    {
        var env = JsonRpcEnvelope.Request(5, "grasshopper.read_grasshopper_canvas", new
        {
            includeValues = true,
            selectedOnly = false,
            categoryFilter = "Params"
        });

        var result = await _dispatcher.DispatchAsync(5, env, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result!.Error);
        Assert.NotNull(result.Result);
        Assert.Equal(5, result.Id);
    }

    [Fact]
    public async Task DispatchAsync_ReadCanvas_WithoutPrefix_RoutesAndSucceeds()
    {
        var env = JsonRpcEnvelope.Request(6, "read_grasshopper_canvas", null);
        var result = await _dispatcher.DispatchAsync(6, env, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result!.Error);
        Assert.NotNull(result.Result);
    }

    [Fact]
    public async Task DispatchAsync_ExportImage_RoutesAndSucceeds()
    {
        var env = JsonRpcEnvelope.Request(7, "grasshopper.export_grasshopper_canvas_image", new
        {
            mode = "FullCanvas",
            zoom = 1.0,
            returnBase64 = false
        });

        var result = await _dispatcher.DispatchAsync(7, env, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result!.Error);
        Assert.NotNull(result.Result);
    }

    [Fact]
    public async Task DispatchAsync_WriteTools_WhenExecutionDisabled_ReturnsExecutionDisabled()
    {
        _settings.ExecutionEnabled = false;

        var playerEnv = JsonRpcEnvelope.Request(8, "grasshopper.run_grasshopper_player", new
        {
            filePath = "dummy.gh",
            headless = true
        });

        var playerRes = await _dispatcher.DispatchAsync(8, playerEnv, null, CancellationToken.None);
        Assert.NotNull(playerRes);
        Assert.NotNull(playerRes!.Error);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, playerRes.Error!.Code);
        Assert.Contains("Execution is disabled in bridge settings", playerRes.Error.Message);

        var batchEnv = JsonRpcEnvelope.Request(9, "grasshopper.batch_edit_grasshopper_canvas", new
        {
            dryRun = true
        });

        var batchRes = await _dispatcher.DispatchAsync(9, batchEnv, null, CancellationToken.None);
        Assert.NotNull(batchRes);
        Assert.NotNull(batchRes!.Error);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, batchRes.Error!.Code);
    }

    [Fact]
    public async Task DispatchAsync_ReadTools_WhenExecutionDisabled_Succeeds()
    {
        _settings.ExecutionEnabled = false;

        var readEnv = JsonRpcEnvelope.Request(10, "grasshopper.read_grasshopper_canvas", null);
        var readRes = await _dispatcher.DispatchAsync(10, readEnv, null, CancellationToken.None);

        Assert.NotNull(readRes);
        Assert.Null(readRes!.Error);
        Assert.NotNull(readRes.Result);
    }

    [Fact]
    public async Task DispatchAsync_WhenCoordinatorIsBusy_ReturnsBusy()
    {
        Assert.True(_coordinator.TryAcquireBusy());
        try
        {
            var env = JsonRpcEnvelope.Request(11, "grasshopper.read_grasshopper_canvas", null);
            var result = await _dispatcher.DispatchAsync(11, env, null, CancellationToken.None);

            Assert.NotNull(result);
            Assert.NotNull(result!.Error);
            Assert.Equal(BridgeErrorCode.Busy, result.Error!.Code);
            Assert.Contains("Another Grasshopper script or task is still running", result.Error.Message);
        }
        finally
        {
            _coordinator.ReleaseBusy();
        }
    }

    [Fact]
    public async Task DispatchAsync_RunPlayer_MissingFilePath_ReturnsInvalidRequest()
    {
        _settings.ExecutionEnabled = true;

        var env = JsonRpcEnvelope.Request(12, "grasshopper.run_grasshopper_player", new
        {
            filePath = ""
        });

        var result = await _dispatcher.DispatchAsync(12, env, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result!.Error);
        Assert.Equal(BridgeErrorCode.InvalidRequest, result.Error!.Code);
        Assert.Contains("filePath is required", result.Error.Message);
    }

    [Fact]
    public async Task DispatchAsync_RunPlayer_ValidFilePath_Routes()
    {
        _settings.ExecutionEnabled = true;

        var env = JsonRpcEnvelope.Request(13, "grasshopper.run_grasshopper_player", new
        {
            filePath = "nonexistent_definition.gh",
            headless = true
        });

        var result = await _dispatcher.DispatchAsync(13, env, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result!.Error);
        Assert.NotNull(result.Result);
        var json = result.Result?.ToString() ?? string.Empty;
        Assert.Contains("\"success\":false", json.Replace(" ", ""));
        Assert.Contains("Definition file not found", json);
    }

    [Fact]
    public async Task DispatchAsync_BatchEdit_Routes()
    {
        _settings.ExecutionEnabled = true;

        var env = JsonRpcEnvelope.Request(14, "grasshopper.batch_edit_grasshopper_canvas", new
        {
            dryRun = true
        });

        var result = await _dispatcher.DispatchAsync(14, env, null, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Null(result!.Error);
        Assert.NotNull(result.Result);
        var json = result.Result.ToString();
        Assert.Contains("success", json);
    }

    [Fact]
    public async Task ExecutionEngine_MapToExecuteResult_MapsFailuresCorrectly()
    {
        var failureObj = new { success = false, error = "Failed to solve", rolledBack = true };
        var execRes = GrasshopperExecutionEngine.MapToExecuteResult(failureObj);

        Assert.True(execRes.IsError);
        Assert.Equal("Failed to solve", execRes.Message);
        Assert.True(execRes.RolledBack);

        var successObj = new { success = true, durationMs = 45 };
        var successRes = GrasshopperExecutionEngine.MapToExecuteResult(successObj);

        Assert.False(successRes.IsError);
        Assert.Null(successRes.Message);

        var nullRes = GrasshopperExecutionEngine.MapToExecuteResult(null);
        Assert.True(nullRes.IsError);
    }
}
