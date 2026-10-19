namespace HPDynamo.McpBridge.Tests.Security;

using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPDynamo.McpBridge.Tests.Fakes;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Xunit;

public sealed class OptInSecurityTests
{
    private const string CustomRefusalMessage =
        "Ask the user to tick 'Allow AI Dynamo execution' in the HPDynamo MCP Bridge window inside Revit.";

    [Fact]
    public async Task Wire_NamedPipe_RefusesExecutionOverWireWhenOptInIsOff()
    {
        var pipeName = "hpdynamo-sec-test-" + Guid.NewGuid().ToString("N");
        var fakeBridgeExecutor = new SimulatedBridgeExecutor();
        var settings = new BridgeSettings { ExecutionEnabled = false };
        using var listener = new PipeListener(
            pipeName,
            new RequestDispatcher(fakeBridgeExecutor, settings, "2026", "Dynamo", CustomRefusalMessage));
        listener.Start();

        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(3000, CancellationToken.None);

        var req = JsonRpcEnvelope.Request(1, "dynamo.execute", new ExecuteRequest("return 1;"));
        var payload = System.Text.Encoding.UTF8.GetBytes(BridgeJson.Serialize(req) + "\n");
        await client.WriteAsync(payload, 0, payload.Length);
        await client.FlushAsync();

        using var reader = new StreamReader(client, System.Text.Encoding.UTF8, false, 4096, leaveOpen: true);
        var responseLine = await reader.ReadLineAsync();

        Assert.NotNull(responseLine);
        var res = BridgeJson.Deserialize<JsonRpcEnvelope>(responseLine);
        Assert.NotNull(res);
        Assert.Equal(1, res.Id);
        Assert.NotNull(res.Error);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, res.Error.Code);
        Assert.Contains("Allow AI Dynamo execution", res.Error.Message);

        await listener.StopAsync();
    }

    [Fact]
    public async Task Wire_NamedPipe_PermitsPingAndContextWhenOptInIsOff()
    {
        var pipeName = "hpdynamo-sec-test-" + Guid.NewGuid().ToString("N");
        var fakeBridgeExecutor = new SimulatedBridgeExecutor();
        var settings = new BridgeSettings { ExecutionEnabled = false };
        using var listener = new PipeListener(
            pipeName,
            new RequestDispatcher(fakeBridgeExecutor, settings, "2026", "Dynamo", CustomRefusalMessage));
        listener.Start();

        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(3000, CancellationToken.None);

        var pingReq = JsonRpcEnvelope.Request(2, "dynamo.ping", null);
        var pingPayload = System.Text.Encoding.UTF8.GetBytes(BridgeJson.Serialize(pingReq) + "\n");
        await client.WriteAsync(pingPayload, 0, pingPayload.Length);
        await client.FlushAsync();

        using var reader = new StreamReader(client, System.Text.Encoding.UTF8, false, 4096, leaveOpen: true);
        var pingReplyLine = await reader.ReadLineAsync();

        Assert.NotNull(pingReplyLine);
        var pingReply = BridgeJson.Deserialize<JsonRpcEnvelope>(pingReplyLine);
        Assert.NotNull(pingReply);
        Assert.Null(pingReply.Error);
        Assert.Equal(2, pingReply.Id);

        await listener.StopAsync();
    }

    [Fact]
    public async Task OptInToggle_ThreadSafety_ConcurrentReadsAndWrites()
    {
        var settings = new BridgeSettings { ExecutionEnabled = false };
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        var writeTask = Task.Run(() =>
        {
            var flag = false;
            while (!cts.Token.IsCancellationRequested)
            {
                settings.ExecutionEnabled = flag;
                flag = !flag;
            }
        });

        var readTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                _ = settings.ExecutionEnabled;
            }
        });

        await Task.WhenAll(writeTask, readTask);
    }

    private sealed class SimulatedBridgeExecutor : IBridgeExecutor
    {
        public bool IsBusy => false;
        public int CompiledScriptCount => 0;
        public string? ActiveDocumentTitle => "Doc";
        public event Action? StateChanged { add { } remove { } }
        public event Action<LastRunInfo>? RunCompleted { add { } remove { } }

        public Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken cancellationToken) =>
            Task.FromResult(new ExecuteResult { Value = JsonSerializer.SerializeToElement("ok") });

        public Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken cancellationToken) =>
            Task.FromResult(new ContextResult { Host = "dynamo" });

        public InspectResult Inspect(InspectRequest request) => new();
        public AnalyzeResult Analyze(AnalyzeRequest request) => new();
        public CancelResult Cancel() => new(false, false);
    }
}
