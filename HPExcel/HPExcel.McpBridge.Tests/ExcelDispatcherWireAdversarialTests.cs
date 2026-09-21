using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPExcel.McpBridge.Com;
using HPExcel.McpBridge.Headless;
using HPExcel.McpBridge.Host;
using HPExcel.McpBridge.Safety;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Xunit;

namespace HPExcel.McpBridge.Tests;

/// <summary>
///     Adversarial stress-test harness for ExcelDispatcher, PipeListener, and JSON-RPC 2.0 wire protocol.
///     Probes broken pipes, mid-stream disconnects, rapid connect/disconnect churning,
///     malformed JSON payloads, notifications, invalid IDs, unknown methods, and stream reuse.
/// </summary>
public sealed class ExcelDispatcherWireAdversarialTests : IDisposable
{
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

    private readonly string _pipeName = "hpexcel-adv-" + Guid.NewGuid().ToString("N");
    private readonly ExcelAttachment _attachment;
    private readonly ExcelStaWorker _staWorker;
    private readonly ExcelSafetyGuard _guard;
    private readonly ExcelSnapshotManager _snapshots;
    private readonly ClosedXmlWorkbookService _closedXml;
    private readonly ExcelBridgeExecutor _executor;
    private readonly BridgeSettings _settings;
    private readonly ExcelDispatcher _dispatcher;
    private readonly RequestDispatcher _requestDispatcher;
    private readonly PipeListener _listener;

    public ExcelDispatcherWireAdversarialTests()
    {
        _attachment = new ExcelAttachment();
        _staWorker = new ExcelStaWorker();
        _guard = new ExcelSafetyGuard();
        _snapshots = new ExcelSnapshotManager();
        _closedXml = new ClosedXmlWorkbookService();

        _executor = new ExcelBridgeExecutor(_attachment, _staWorker, _guard, _snapshots, _closedXml, "2026");
        _settings = new BridgeSettings { ExecutionEnabled = true };
        _dispatcher = new ExcelDispatcher(_executor, _settings, "2026");

        _requestDispatcher = new RequestDispatcher(
            _executor,
            _settings,
            "2026",
            "Excel",
            ExcelSafetyGuard.ExecutionDisabledMessage,
            _dispatcher.DispatchCustomAsync);

        _listener = new PipeListener(_pipeName, _requestDispatcher);
        _listener.Start();
    }

    public void Dispose()
    {
        _listener.Dispose();
        _executor.Dispose();
        _staWorker.Dispose();
    }

    private async Task<NamedPipeClientStream> ConnectAsync(int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
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
        throw new TimeoutException($"Could not connect to pipe {_pipeName} within {timeoutMs}ms");
    }

    private async Task<JsonRpcEnvelope> SendSingleRequestAsync(string jsonRpcLine)
    {
        using var client = await ConnectAsync();
        var bytes = Utf8NoBom.GetBytes(jsonRpcLine + "\n");
        await client.WriteAsync(bytes, 0, bytes.Length);
        await client.FlushAsync();

        using var reader = new StreamReader(client, Utf8NoBom, false, 65536, leaveOpen: true);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            var text = await reader.ReadLineAsync();
            if (text is null) break;
            var envelope = BridgeJson.Deserialize<JsonRpcEnvelope>(text);
            if (envelope is not null) return envelope;
        }

        throw new TimeoutException($"No response received for request: {jsonRpcLine}");
    }

    #region 1. Broken Pipe & Disconnection Resilience

    [Fact]
    public async Task ClientDisconnects_MidStream_PipeListenerSurvives_AndNextClientSucceeds()
    {
        // 1. Client connects, writes partial/truncated JSON line, and abruptly disposes stream
        using (var brokenClient = await ConnectAsync())
        {
            var partialBytes = Utf8NoBom.GetBytes("{\"jsonrpc\":\"2.0\",\"id\":1,\"meth");
            await brokenClient.WriteAsync(partialBytes, 0, partialBytes.Length);
            await brokenClient.FlushAsync();
            // Abrupt disconnect
        }

        // Allow small buffer flush/cleanup delay
        await Task.Delay(100);

        // 2. Next client connects cleanly and performs ping
        var reply = await SendSingleRequestAsync("{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"excel.ping\"}");
        Assert.Equal(2, reply.Id);
        Assert.Null(reply.Error);
        var ping = reply.ResultAs<BridgePingResult>();
        Assert.NotNull(ping);
        Assert.True(ping.Pong);
    }

    [Fact]
    public async Task ClientDisconnects_BeforeReadingResponse_WriterHandlesSafely()
    {
        // Client writes full request, then immediately closes read end
        using (var client = await ConnectAsync())
        {
            var bytes = Utf8NoBom.GetBytes("{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"excel.ping\"}\n");
            await client.WriteAsync(bytes, 0, bytes.Length);
            await client.FlushAsync();
            // Close immediately without reading
        }

        await Task.Delay(100);

        // Verify listener is still intact and serves another client
        var reply = await SendSingleRequestAsync("{\"jsonrpc\":\"2.0\",\"id\":4,\"method\":\"excel.ping\"}");
        Assert.Equal(4, reply.Id);
        Assert.Null(reply.Error);
    }

    [Fact]
    public async Task RapidConnectDisconnectCycles_PipeRemainsResponsive()
    {
        // Open and close 5 connections rapidly without writing anything
        for (var i = 0; i < 5; i++)
        {
            using var client = await ConnectAsync();
            Assert.True(client.IsConnected);
        }

        // Listener must still accept new work
        var reply = await SendSingleRequestAsync("{\"jsonrpc\":\"2.0\",\"id\":5,\"method\":\"excel.ping\"}");
        Assert.Equal(5, reply.Id);
        Assert.True(reply.ResultAs<BridgePingResult>()!.Pong);
    }

    #endregion

    #region 2. Malformed JSON-RPC Payloads & Edge Cases

    [Fact]
    public async Task EmptyLines_AreIgnored_AndWhitespaceNonEmpty_ReturnsParseError()
    {
        using var client = await ConnectAsync();
        using var writer = new StreamWriter(client, Utf8NoBom, 1024, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };
        using var reader = new StreamReader(client, Utf8NoBom, false, 65536, leaveOpen: true);

        // 1. Completely empty lines (Length == 0) are skipped by PipeListener
        await writer.WriteLineAsync("");
        await writer.WriteLineAsync("");

        // 2. Whitespace-only line with spaces (Length > 0) is not valid JSON and returns ParseError (Code: -32700, Id: 0)
        await writer.WriteLineAsync("   ");

        var parseErrorLine = await reader.ReadLineAsync();
        Assert.NotNull(parseErrorLine);
        var parseErrorReply = BridgeJson.Deserialize<JsonRpcEnvelope>(parseErrorLine);
        Assert.NotNull(parseErrorReply);
        Assert.Equal(0, parseErrorReply.Id);
        Assert.Equal(BridgeErrorCode.ParseError, parseErrorReply.Error?.Code);

        // 3. Now send valid ping to verify pipe is still active and healthy
        await writer.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"id\":6,\"method\":\"excel.ping\"}");

        var pingLine = await reader.ReadLineAsync();
        Assert.NotNull(pingLine);
        var pingReply = BridgeJson.Deserialize<JsonRpcEnvelope>(pingLine);
        Assert.NotNull(pingReply);
        Assert.Equal(6, pingReply.Id);
        Assert.True(pingReply.ResultAs<BridgePingResult>()!.Pong);
    }

    [Fact]
    public async Task MalformedJson_UnclosedBrace_ReturnsParseError32700()
    {
        var reply = await SendSingleRequestAsync("{\"jsonrpc\":\"2.0\",\"id\":7,\"method\":\"excel.ping\"");
        Assert.Equal(0, reply.Id);
        Assert.NotNull(reply.Error);
        Assert.Equal(BridgeErrorCode.ParseError, reply.Error.Code);
        Assert.Equal(-32700, reply.Error.Code);
    }

    [Fact]
    public async Task ValidJson_ArrayNotObject_HandledSafely()
    {
        // Deserialization into JsonRpcEnvelope might fail (array is not an object) or produce null
        // Either ParseError (-32700) or ignored if null
        using var client = await ConnectAsync();
        using var writer = new StreamWriter(client, Utf8NoBom, 1024, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };
        using var reader = new StreamReader(client, Utf8NoBom, false, 65536, leaveOpen: true);

        await writer.WriteLineAsync("[1, 2, 3]");

        // Send valid ping right after to verify pipe is healthy
        await writer.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"id\":8,\"method\":\"excel.ping\"}");

        var replyLine = await reader.ReadLineAsync();
        Assert.NotNull(replyLine);
        var env = BridgeJson.Deserialize<JsonRpcEnvelope>(replyLine);
        Assert.NotNull(env);

        // If array produced ParseError, read the next line for ping
        if (env.Error?.Code == BridgeErrorCode.ParseError)
        {
            var pingLine = await reader.ReadLineAsync();
            Assert.NotNull(pingLine);
            var pingReply = BridgeJson.Deserialize<JsonRpcEnvelope>(pingLine);
            Assert.Equal(8, pingReply!.Id);
        }
        else
        {
            Assert.Equal(8, env.Id);
        }
    }

    [Fact]
    public async Task ValidJson_MissingId_IgnoredAsNotificationWithoutReply()
    {
        using var client = await ConnectAsync();
        using var writer = new StreamWriter(client, Utf8NoBom, 1024, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };
        using var reader = new StreamReader(client, Utf8NoBom, false, 65536, leaveOpen: true);

        // Notification has no "id"
        await writer.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"method\":\"excel.ping\"}");

        // Now send request with ID
        await writer.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"id\":9,\"method\":\"excel.ping\"}");

        // The first message should produce NO response; the response received must be for id: 9
        var line = await reader.ReadLineAsync();
        Assert.NotNull(line);
        var reply = BridgeJson.Deserialize<JsonRpcEnvelope>(line);
        Assert.NotNull(reply);
        Assert.Equal(9, reply.Id);
    }

    [Fact]
    public async Task MalformedJson_StringId_ReturnsParseError32700()
    {
        // In this architecture, Id is long?. A string "req-1" cannot be deserialized into long?, throwing JsonException -> -32700
        var reply = await SendSingleRequestAsync("{\"jsonrpc\":\"2.0\",\"id\":\"req-1\",\"method\":\"excel.ping\"}");
        Assert.Equal(0, reply.Id);
        Assert.NotNull(reply.Error);
        Assert.Equal(BridgeErrorCode.ParseError, reply.Error.Code);
    }

    [Fact]
    public async Task WireProtocol_Int64MaxId_EchoesMatchingIdInResponse()
    {
        var largeId = long.MaxValue;
        var reply = await SendSingleRequestAsync($"{{\"jsonrpc\":\"2.0\",\"id\":{largeId},\"method\":\"excel.ping\"}}");
        Assert.Equal(largeId, reply.Id);
        Assert.Null(reply.Error);
    }

    [Fact]
    public async Task WireProtocol_NegativeId_EchoesMatchingIdInResponse()
    {
        var reply = await SendSingleRequestAsync("{\"jsonrpc\":\"2.0\",\"id\":-123,\"method\":\"excel.ping\"}");
        Assert.Equal(-123, reply.Id);
        Assert.Null(reply.Error);
    }

    #endregion

    #region 3. Unknown Methods & Routing

    [Fact]
    public async Task UnknownMethod_EmptyString_ReturnsMethodNotFound32601()
    {
        var reply = await SendSingleRequestAsync("{\"jsonrpc\":\"2.0\",\"id\":10,\"method\":\"\"}");
        Assert.Equal(10, reply.Id);
        Assert.NotNull(reply.Error);
        Assert.Equal(BridgeErrorCode.MethodNotFound, reply.Error.Code);
        Assert.Equal(-32601, reply.Error.Code);
    }

    [Fact]
    public async Task UnknownMethod_OnlyPrefix_ReturnsMethodNotFound32601()
    {
        var reply = await SendSingleRequestAsync("{\"jsonrpc\":\"2.0\",\"id\":11,\"method\":\"excel.\"}");
        Assert.Equal(11, reply.Id);
        Assert.NotNull(reply.Error);
        Assert.Equal(BridgeErrorCode.MethodNotFound, reply.Error.Code);
    }

    [Fact]
    public async Task UnknownMethod_ArbitraryName_ReturnsMethodNotFound32601()
    {
        var reply = await SendSingleRequestAsync("{\"jsonrpc\":\"2.0\",\"id\":12,\"method\":\"excel.completely_fabricated_operation\"}");
        Assert.Equal(12, reply.Id);
        Assert.NotNull(reply.Error);
        Assert.Equal(BridgeErrorCode.MethodNotFound, reply.Error.Code);
        Assert.Contains("Method not found", reply.Error.Message);
    }

    [Fact]
    public async Task UnknownMethod_VeryLongMethodName_ReturnsMethodNotFound32601()
    {
        var longName = "excel." + new string('x', 2000);
        var reply = await SendSingleRequestAsync($"{{\"jsonrpc\":\"2.0\",\"id\":13,\"method\":\"{longName}\"}}");
        Assert.Equal(13, reply.Id);
        Assert.NotNull(reply.Error);
        Assert.Equal(BridgeErrorCode.MethodNotFound, reply.Error.Code);
    }

    #endregion

    #region 4. Stream Reuse & Sequential Protocol Operations

    [Fact]
    public async Task SinglePipeSession_ProcessesMultipleSequentialRequests()
    {
        using var client = await ConnectAsync();
        using var writer = new StreamWriter(client, Utf8NoBom, 1024, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };
        using var reader = new StreamReader(client, Utf8NoBom, false, 65536, leaveOpen: true);

        // 1. Ping
        await writer.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"id\":101,\"method\":\"excel.ping\"}");
        var line1 = await reader.ReadLineAsync();
        var reply1 = BridgeJson.Deserialize<JsonRpcEnvelope>(line1!);
        Assert.Equal(101, reply1!.Id);
        Assert.Null(reply1.Error);

        // 2. Cancel
        await writer.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"id\":102,\"method\":\"excel.cancel\"}");
        var line2 = await reader.ReadLineAsync();
        var reply2 = BridgeJson.Deserialize<JsonRpcEnvelope>(line2!);
        Assert.Equal(102, reply2!.Id);
        Assert.Null(reply2.Error);

        // 3. Context
        await writer.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"id\":103,\"method\":\"excel.context\",\"params\":{\"includeSelection\":false}}");
        var line3 = await reader.ReadLineAsync();
        var reply3 = BridgeJson.Deserialize<JsonRpcEnvelope>(line3!);
        Assert.Equal(103, reply3!.Id);
        Assert.Null(reply3.Error);
        var ctx = reply3.ResultAs<ContextResult>();
        Assert.NotNull(ctx);
        Assert.Equal("excel", ctx.Host);

        // 4. Detach
        await writer.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"id\":104,\"method\":\"excel.detach\"}");
        var line4 = await reader.ReadLineAsync();
        var reply4 = BridgeJson.Deserialize<JsonRpcEnvelope>(line4!);
        Assert.Equal(104, reply4!.Id);
        Assert.Null(reply4.Error);

        // 5. Another Ping
        await writer.WriteLineAsync("{\"jsonrpc\":\"2.0\",\"id\":105,\"method\":\"excel.ping\"}");
        var line5 = await reader.ReadLineAsync();
        var reply5 = BridgeJson.Deserialize<JsonRpcEnvelope>(line5!);
        Assert.Equal(105, reply5!.Id);
        Assert.Null(reply5.Error);
    }

    [Fact]
    public async Task Attach_WithMalformedPidParameter_ReturnsParseErrorOrInternalError()
    {
        // PID is int?, but we provide string "not_a_number"
        var reply = await SendSingleRequestAsync("{\"jsonrpc\":\"2.0\",\"id\":106,\"method\":\"excel.attach\",\"params\":{\"pid\":\"not_a_number\"}}");
        Assert.Equal(106, reply.Id);
        Assert.NotNull(reply.Error);
        Assert.Equal(BridgeErrorCode.InternalError, reply.Error.Code);
    }

    #endregion
}
