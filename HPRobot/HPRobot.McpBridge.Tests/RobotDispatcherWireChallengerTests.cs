using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPRobot.McpBridge.Com;
using HPRobot.McpBridge.Host;
using HPRobot.McpBridge.Safety;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Xunit;

namespace HPRobot.McpBridge.Tests;

/// <summary>
///     Empirical adversarial tests challenging the HPRobot Named Pipe wire protocol:
///     1. Pipe naming registration verification for "hprobot-mcp-2026".
///     2. Real Windows Named Pipe client-server communication.
///     3. JSON-RPC dispatching of robot.ping, robot.context, robot.execute.
///     4. Custom dispatching of robot.attach and robot.detach via RobotDispatcher.
///     5. Permission gating enforcement over wire (code -32001).
///     6. Standard error contracts: MethodNotFound (-32601) and ParseError (-32700).
/// </summary>
public sealed class RobotDispatcherWireChallengerTests : IDisposable
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private readonly string _pipeName = "hprobot-test-" + Guid.NewGuid().ToString("N");
    private readonly RobotAttachment _attachment;
    private readonly RobotStaWorker _staWorker;
    private readonly RobotSafetyGuard _guard;
    private readonly RobotSnapshotManager _snapshots;
    private readonly RobotBridgeExecutor _executor;
    private readonly BridgeSettings _settings;
    private readonly RobotDispatcher _dispatcher;
    private readonly RequestDispatcher _requestDispatcher;
    private readonly PipeListener _listener;
    private readonly string _tempDir;

    public RobotDispatcherWireChallengerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hprobot_wire_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _attachment = new RobotAttachment();
        _staWorker = new RobotStaWorker();
        _staWorker.Start();

        _guard = new RobotSafetyGuard();
        _snapshots = new RobotSnapshotManager(Path.Combine(_tempDir, "snaps"));

        _executor = new RobotBridgeExecutor(_attachment, _staWorker, _guard, _snapshots, "2026");
        _settings = new BridgeSettings { ExecutionEnabled = true };
        _dispatcher = new RobotDispatcher(_executor, _settings, "2026");

        _requestDispatcher = new RequestDispatcher(
            _executor,
            _settings,
            "2026",
            "Robot",
            BridgeEntry.ExecutionDisabledMessage,
            _dispatcher.DispatchCustomAsync);

        _listener = new PipeListener(_pipeName, _requestDispatcher);
        _listener.Start();
    }

    public void Dispose()
    {
        _listener.Dispose();
        _executor.Dispose();
        _staWorker.Dispose();
        _attachment.Dispose();

        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }

    private async Task<NamedPipeClientStream> ConnectAsync()
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await client.ConnectAsync(500);
                return client;
            }
            catch
            {
                await Task.Delay(50);
            }
        }
        throw new TimeoutException($"Could not connect to named pipe {_pipeName}");
    }

    private async Task<JsonRpcEnvelope> SendLineAsync(string jsonRpcLine)
    {
        using var client = await ConnectAsync();
        var bytes = Utf8NoBom.GetBytes(jsonRpcLine + "\n");
        await client.WriteAsync(bytes, 0, bytes.Length);
        await client.FlushAsync();

        using var reader = new StreamReader(client, Utf8NoBom, false, 65536, leaveOpen: true);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var line = await reader.ReadLineAsync(cts.Token);
        Assert.NotNull(line);

        var envelope = BridgeJson.Deserialize<JsonRpcEnvelope>(line);
        Assert.NotNull(envelope);
        return envelope!;
    }

    // =========================================================================
    // 1. Pipe Naming and Protocol Constants
    // =========================================================================

    [Fact]
    public void PipeNaming_Resolves_hprobot_mcp_2026()
    {
        var pipe = PipeNaming.For(PipeNaming.RobotHost, 2026);
        Assert.Equal("hprobot-mcp-2026", pipe);
    }

    // =========================================================================
    // 2. robot.ping Wire Handling
    // =========================================================================

    [Fact]
    public async Task Wire_Handles_robot_ping_Successfully()
    {
        var line = "{\"jsonrpc\":\"2.0\",\"id\":10,\"method\":\"robot.ping\",\"params\":{}}";
        var reply = await SendLineAsync(line);

        Assert.Equal(10, reply.Id);
        Assert.Equal(JsonRpcKind.Response, reply.Kind);
        Assert.Null(reply.Error);

        var ping = reply.ResultAs<BridgePingResult>();
        Assert.NotNull(ping);
        Assert.True(ping.Pong);
        Assert.Equal("2026", ping.RevitVersion);
        Assert.True(ping.ExecutionEnabled);
    }

    // =========================================================================
    // 3. robot.context Wire Handling
    // =========================================================================

    [Fact]
    public async Task Wire_Handles_robot_context_ReturningRobotInfo()
    {
        var line = "{\"jsonrpc\":\"2.0\",\"id\":20,\"method\":\"robot.context\",\"params\":{\"includeSelection\":false}}";
        var reply = await SendLineAsync(line);

        Assert.Equal(20, reply.Id);
        Assert.Null(reply.Error);

        var context = reply.ResultAs<ContextResult>();
        Assert.NotNull(context);
        Assert.Equal("robot", context.Host);
        Assert.Equal("2026", context.HostVersion);
        Assert.NotNull(context.Robot);
        Assert.Equal(_attachment.IsAttached, context.Robot.IsAttached);
    }

    // =========================================================================
    // 4. robot.execute Gating and Execution Wire Handling
    // =========================================================================

    [Fact]
    public async Task Wire_Handles_robot_execute_RefusedWhenExecutionDisabled()
    {
        // Disable execution in bridge settings
        _settings.ExecutionEnabled = false;
        _guard.IsExecutionEnabled = false;

        var line = "{\"jsonrpc\":\"2.0\",\"id\":30,\"method\":\"robot.execute\",\"params\":{\"code\":\"return 42;\"}}";
        var reply = await SendLineAsync(line);

        Assert.Equal(30, reply.Id);
        Assert.NotNull(reply.Error);
        Assert.Equal((int)BridgeErrorCode.ExecutionDisabled, reply.Error.Code);
        Assert.Contains("Code execution is disabled", reply.Error.Message);
    }

    [Fact]
    public async Task Wire_Handles_robot_execute_SucceedsWhenExecutionEnabled()
    {
        _settings.ExecutionEnabled = true;
        _guard.IsExecutionEnabled = true;

        var line = "{\"jsonrpc\":\"2.0\",\"id\":31,\"method\":\"robot.execute\",\"params\":{\"code\":\"return 25 * 4;\",\"transaction\":\"none\"}}";
        var reply = await SendLineAsync(line);

        Assert.Equal(31, reply.Id);
        Assert.Null(reply.Error);

        var result = reply.ResultAs<ExecuteResult>();
        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.NotNull(result.Value);
        Assert.Equal(100, result.Value.Value.GetInt32());
    }

    // =========================================================================
    // 5. Custom Methods: robot.attach and robot.detach
    // =========================================================================

    [Fact]
    public async Task Wire_Handles_robot_attach_ViaCustomDispatcher()
    {
        var line = "{\"jsonrpc\":\"2.0\",\"id\":40,\"method\":\"robot.attach\",\"params\":{}}";
        var reply = await SendLineAsync(line);

        Assert.Equal(40, reply.Id);

        // Either attaches successfully (when Robot ROT available) or returns descriptive error (-32000)
        if (reply.Error != null)
        {
            Assert.Equal((int)BridgeErrorCode.InternalError, reply.Error.Code);
            Assert.Contains("Robot", reply.Error.Message);
        }
        else
        {
            using var doc = JsonDocument.Parse(reply.Result?.GetRawText() ?? "{}");
            var root = doc.RootElement;
            Assert.True(root.GetProperty("success").GetBoolean());
            Assert.True(root.TryGetProperty("isAttached", out _));
        }
    }

    [Fact]
    public async Task Wire_Handles_robot_detach_ViaCustomDispatcher()
    {
        var line = "{\"jsonrpc\":\"2.0\",\"id\":50,\"method\":\"robot.detach\",\"params\":{}}";
        var reply = await SendLineAsync(line);

        Assert.Equal(50, reply.Id);
        Assert.Null(reply.Error);

        using var doc = JsonDocument.Parse(reply.Result?.GetRawText() ?? "{}");
        var root = doc.RootElement;
        Assert.True(root.GetProperty("success").GetBoolean());
        Assert.False(root.GetProperty("isAttached").GetBoolean());
    }

    // =========================================================================
    // 6. Error Handling: MethodNotFound and ParseError
    // =========================================================================

    [Fact]
    public async Task Wire_Handles_UnknownMethod_ReturnsMethodNotFound()
    {
        var line = "{\"jsonrpc\":\"2.0\",\"id\":60,\"method\":\"robot.nonexistent_method\",\"params\":{}}";
        var reply = await SendLineAsync(line);

        Assert.Equal(60, reply.Id);
        Assert.NotNull(reply.Error);
        Assert.Equal((int)BridgeErrorCode.MethodNotFound, reply.Error.Code);
        Assert.Contains("Method not found: robot.nonexistent_method", reply.Error.Message);
    }

    [Fact]
    public async Task Wire_Handles_MalformedJson_ReturnsParseError()
    {
        var line = "MALFORMED_NON_JSON_LINE";
        var reply = await SendLineAsync(line);

        Assert.Equal(0, reply.Id);
        Assert.NotNull(reply.Error);
        Assert.Equal((int)BridgeErrorCode.ParseError, reply.Error.Code);
        Assert.Contains("Line is not valid JSON-RPC", reply.Error.Message);
    }

    [Fact]
    public async Task Wire_Handles_robot_cancel_ReturnsCancelResult()
    {
        var line = "{\"jsonrpc\":\"2.0\",\"id\":70,\"method\":\"robot.cancel\",\"params\":{}}";
        var reply = await SendLineAsync(line);

        Assert.Equal(70, reply.Id);
        Assert.Null(reply.Error);

        var cancel = reply.ResultAs<CancelResult>();
        Assert.NotNull(cancel);
        Assert.False(cancel.WasRunning);
    }
}
