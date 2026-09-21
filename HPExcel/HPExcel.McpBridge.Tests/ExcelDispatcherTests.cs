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
///     Comprehensive test suite for ExcelDispatcher and RequestDispatcher JSON-RPC routing.
///     Verifies custom methods (excel.attach, excel.detach), standard methods (ping, context, execute, cancel),
///     error handling (invalid requests, parse errors, missing methods), and execution gating over named pipe.
/// </summary>
public sealed class ExcelDispatcherTests : IDisposable
{
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

    private readonly string _pipeName = "hpexcel-disp-" + Guid.NewGuid().ToString("N");
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

    public ExcelDispatcherTests()
    {
        _attachment = new ExcelAttachment();
        _staWorker = new ExcelStaWorker();
        // Do not call _staWorker.Start() in unit tests to avoid COM thread overhead
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
        throw new TimeoutException($"Could not connect to pipe {_pipeName}");
    }

    private async Task<JsonRpcEnvelope> ProcessRequestAsync(string jsonRpcLine)
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

    [Fact]
    public async Task Direct_DispatchCustomAsync_Attach_InvokesAttachmentAndReturnsProperResponse()
    {
        var req = JsonRpcEnvelope.Request(101, "excel.attach", new ExcelAttachParams(9999));
        var res = await _dispatcher.DispatchCustomAsync(101, req, null!, CancellationToken.None);

        Assert.NotNull(res);
        Assert.Equal(101, res.Id);

        if (_attachment.IsAttached)
        {
            Assert.Null(res.Error);
            Assert.NotNull(res.Result);
            var doc = JsonDocument.Parse(res.Result!.Value.GetRawText());
            Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        }
        else
        {
            // In headless/test runner when Excel is not running or not in ROT
            Assert.NotNull(res.Error);
            Assert.Equal(BridgeErrorCode.InternalError, res.Error.Code);
            Assert.True(res.Error.Message.Contains("Microsoft Excel is not running") || res.Error.Message.Contains("Running Object Table"));
        }
    }

    [Fact]
    public async Task Direct_DispatchCustomAsync_Detach_ReturnsSuccess()
    {
        var req = JsonRpcEnvelope.Request(102, "excel.detach", null);
        var res = await _dispatcher.DispatchCustomAsync(102, req, null!, CancellationToken.None);

        Assert.NotNull(res);
        Assert.Equal(102, res.Id);
        Assert.Null(res.Error);
        Assert.NotNull(res.Result);

        var doc = JsonDocument.Parse(res.Result!.Value.GetRawText());
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("isAttached").GetBoolean());
    }

    [Fact]
    public async Task Direct_DispatchCustomAsync_UnknownCustomMethod_ReturnsNull()
    {
        var req = JsonRpcEnvelope.Request(103, "excel.nonexistent", null);
        var res = await _dispatcher.DispatchCustomAsync(103, req, null!, CancellationToken.None);

        Assert.Null(res);
    }

    [Fact]
    public async Task Dispatcher_Ping_ReturnsExpectedHostDetails()
    {
        var requestJson = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"excel.ping\"}";
        var reply = await ProcessRequestAsync(requestJson);

        Assert.Equal(1, reply.Id);
        Assert.Null(reply.Error);
        Assert.NotNull(reply.Result);

        var pingResult = reply.ResultAs<BridgePingResult>();
        Assert.NotNull(pingResult);
        Assert.True(pingResult.Pong);
        Assert.Equal("2026", pingResult.RevitVersion);
        Assert.True(pingResult.ExecutionEnabled);
        Assert.False(pingResult.Busy);
    }

    [Fact]
    public async Task Dispatcher_Attach_InvokesAttachmentAndReturnsResult()
    {
        var requestJson = "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"excel.attach\",\"params\":{\"pid\":1234}}";
        var reply = await ProcessRequestAsync(requestJson);

        Assert.Equal(2, reply.Id);
        if (_attachment.IsAttached)
        {
            Assert.Null(reply.Error);
            Assert.NotNull(reply.Result);
            var doc = JsonDocument.Parse(reply.Result!.Value.GetRawText());
            Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        }
        else
        {
            Assert.NotNull(reply.Error);
            Assert.Equal(BridgeErrorCode.InternalError, reply.Error.Code);
            Assert.True(reply.Error.Message.Contains("Microsoft Excel is not running") || reply.Error.Message.Contains("Running Object Table"));
        }
    }

    [Fact]
    public async Task Dispatcher_Detach_DetachesAndReturnsCleanSuccess()
    {
        var requestJson = "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"excel.detach\"}";
        var reply = await ProcessRequestAsync(requestJson);

        Assert.Equal(3, reply.Id);
        Assert.Null(reply.Error);
        Assert.NotNull(reply.Result);

        var doc = JsonDocument.Parse(reply.Result!.Value.GetRawText());
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("isAttached").GetBoolean());
    }

    [Fact]
    public async Task Dispatcher_Cancel_ReturnsCancelResult()
    {
        var requestJson = "{\"jsonrpc\":\"2.0\",\"id\":4,\"method\":\"excel.cancel\"}";
        var reply = await ProcessRequestAsync(requestJson);

        Assert.Equal(4, reply.Id);
        Assert.Null(reply.Error);
        Assert.NotNull(reply.Result);

        var cancelResult = reply.ResultAs<CancelResult>();
        Assert.NotNull(cancelResult);
        Assert.False(cancelResult.WasRunning);
        Assert.False(cancelResult.Cancelled);
    }

    [Fact]
    public async Task Dispatcher_Context_ReturnsExcelContextResult()
    {
        var requestJson = "{\"jsonrpc\":\"2.0\",\"id\":5,\"method\":\"excel.context\",\"params\":{\"includeSelection\":false}}";
        var reply = await ProcessRequestAsync(requestJson);

        Assert.Equal(5, reply.Id);
        Assert.Null(reply.Error);
        Assert.NotNull(reply.Result);

        var context = reply.ResultAs<ContextResult>();
        Assert.NotNull(context);
        Assert.Equal("excel", context.Host);
        Assert.Equal("2026", context.HostVersion);
        Assert.NotNull(context.Excel);
    }

    [Fact]
    public async Task Dispatcher_Execute_WhenExecutionDisabled_ReturnsErrorCode32001()
    {
        _settings.ExecutionEnabled = false;
        try
        {
            var requestJson = "{\"jsonrpc\":\"2.0\",\"id\":6,\"method\":\"excel.execute\",\"params\":{\"code\":\"return 1;\"}}";
            var reply = await ProcessRequestAsync(requestJson);

            Assert.Equal(6, reply.Id);
            Assert.NotNull(reply.Error);
            Assert.Equal(BridgeErrorCode.ExecutionDisabled, reply.Error.Code);
            Assert.Equal(-32001, reply.Error.Code);
            Assert.Contains("Allow AI execution", reply.Error.Message);
        }
        finally
        {
            _settings.ExecutionEnabled = true;
        }
    }

    [Fact]
    public async Task Dispatcher_Execute_WithEmptyCode_ReturnsInvalidRequest()
    {
        var requestJson = "{\"jsonrpc\":\"2.0\",\"id\":7,\"method\":\"excel.execute\",\"params\":{\"code\":\"   \"}}";
        var reply = await ProcessRequestAsync(requestJson);

        Assert.Equal(7, reply.Id);
        Assert.NotNull(reply.Error);
        Assert.Equal(BridgeErrorCode.InvalidRequest, reply.Error.Code);
        Assert.Equal(-32600, reply.Error.Code);
        Assert.Contains("non-empty code", reply.Error.Message);
    }

    [Fact]
    public async Task Dispatcher_Inspect_WithEmptyTypeName_ReturnsInvalidRequest()
    {
        var requestJson = "{\"jsonrpc\":\"2.0\",\"id\":8,\"method\":\"excel.inspect\",\"params\":{\"typeName\":\"\"}}";
        var reply = await ProcessRequestAsync(requestJson);

        Assert.Equal(8, reply.Id);
        Assert.NotNull(reply.Error);
        Assert.Equal(BridgeErrorCode.InvalidRequest, reply.Error.Code);
        Assert.Equal(-32600, reply.Error.Code);
    }

    [Fact]
    public async Task Dispatcher_Analyze_WithEmptyCode_ReturnsInvalidRequest()
    {
        var requestJson = "{\"jsonrpc\":\"2.0\",\"id\":9,\"method\":\"excel.analyze\",\"params\":{\"code\":\"\"}}";
        var reply = await ProcessRequestAsync(requestJson);

        Assert.Equal(9, reply.Id);
        Assert.NotNull(reply.Error);
        Assert.Equal(BridgeErrorCode.InvalidRequest, reply.Error.Code);
        Assert.Equal(-32600, reply.Error.Code);
    }

    [Fact]
    public async Task Dispatcher_MalformedJson_ReturnsParseError()
    {
        var malformedLine = "{ this is not valid JSON! }";
        var reply = await ProcessRequestAsync(malformedLine);

        Assert.Equal(0, reply.Id);
        Assert.NotNull(reply.Error);
        Assert.Equal(BridgeErrorCode.ParseError, reply.Error.Code);
        Assert.Equal(-32700, reply.Error.Code);
    }

    [Fact]
    public async Task Dispatcher_UnknownMethod_ReturnsMethodNotFound()
    {
        var unknownMethod = "{\"jsonrpc\":\"2.0\",\"id\":10,\"method\":\"excel.unknown_action\"}";
        var reply = await ProcessRequestAsync(unknownMethod);

        Assert.Equal(10, reply.Id);
        Assert.NotNull(reply.Error);
        Assert.Equal(BridgeErrorCode.MethodNotFound, reply.Error.Code);
        Assert.Equal(-32601, reply.Error.Code);
        Assert.Contains("Method not found", reply.Error.Message);
    }
}
