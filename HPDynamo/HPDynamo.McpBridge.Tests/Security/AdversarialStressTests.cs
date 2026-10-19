namespace HPDynamo.McpBridge.Tests.Security;

using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPDynamo.McpBridge.Coordinator;
using HPDynamo.McpBridge.Execution;
using HPDynamo.McpBridge.Tests.Fakes;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Xunit;

public sealed class AdversarialStressTests
{
    private const string RefusalMessage = "Ask the user to tick 'Allow AI Dynamo execution'.";

    [Fact]
    public async Task Wire_NamedPipe_PermitsContextOverWireWhenOptInIsOff()
    {
        var pipeName = "hpdynamo-adv-ctx-" + Guid.NewGuid().ToString("N");
        var fakeExecutor = new SimulatedBridgeExecutor();
        var settings = new BridgeSettings { ExecutionEnabled = false };
        using var listener = new PipeListener(
            pipeName,
            new RequestDispatcher(fakeExecutor, settings, "2026", "Dynamo", RefusalMessage));
        listener.Start();

        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(3000, CancellationToken.None);

        var ctxReq = JsonRpcEnvelope.Request(10, "dynamo.context", new ContextRequest(false));
        var payload = System.Text.Encoding.UTF8.GetBytes(BridgeJson.Serialize(ctxReq) + "\n");
        await client.WriteAsync(payload, 0, payload.Length);
        await client.FlushAsync();

        using var reader = new StreamReader(client, System.Text.Encoding.UTF8, false, 4096, leaveOpen: true);
        var replyLine = await reader.ReadLineAsync();

        Assert.NotNull(replyLine);
        var reply = BridgeJson.Deserialize<JsonRpcEnvelope>(replyLine);
        Assert.NotNull(reply);
        Assert.Null(reply.Error);
        Assert.Equal(10, reply.Id);

        var ctxResult = reply.ResultAs<ContextResult>();
        Assert.NotNull(ctxResult);
        Assert.Equal("dynamo", ctxResult.Host);

        await listener.StopAsync();
    }

    [Fact]
    public async Task Wire_NamedPipe_PermitsCancelOverWireWhenOptInIsOff()
    {
        var pipeName = "hpdynamo-adv-cnc-" + Guid.NewGuid().ToString("N");
        var fakeExecutor = new SimulatedBridgeExecutor();
        var settings = new BridgeSettings { ExecutionEnabled = false };
        using var listener = new PipeListener(
            pipeName,
            new RequestDispatcher(fakeExecutor, settings, "2026", "Dynamo", RefusalMessage));
        listener.Start();

        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(3000, CancellationToken.None);

        var cancelReq = JsonRpcEnvelope.Request(11, "dynamo.cancel", null);
        var payload = System.Text.Encoding.UTF8.GetBytes(BridgeJson.Serialize(cancelReq) + "\n");
        await client.WriteAsync(payload, 0, payload.Length);
        await client.FlushAsync();

        using var reader = new StreamReader(client, System.Text.Encoding.UTF8, false, 4096, leaveOpen: true);
        var replyLine = await reader.ReadLineAsync();

        Assert.NotNull(replyLine);
        var reply = BridgeJson.Deserialize<JsonRpcEnvelope>(replyLine);
        Assert.NotNull(reply);
        Assert.Null(reply.Error);
        Assert.Equal(11, reply.Id);

        await listener.StopAsync();
    }

    [Fact]
    public async Task Wire_NamedPipe_UnknownMethod_ReturnsMethodNotFound()
    {
        var pipeName = "hpdynamo-adv-unk-" + Guid.NewGuid().ToString("N");
        var fakeExecutor = new SimulatedBridgeExecutor();
        var settings = new BridgeSettings { ExecutionEnabled = true };
        using var listener = new PipeListener(
            pipeName,
            new RequestDispatcher(fakeExecutor, settings, "2026", "Dynamo", RefusalMessage));
        listener.Start();

        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(3000, CancellationToken.None);

        var unkReq = JsonRpcEnvelope.Request(12, "dynamo.non_existent_method", null);
        var payload = System.Text.Encoding.UTF8.GetBytes(BridgeJson.Serialize(unkReq) + "\n");
        await client.WriteAsync(payload, 0, payload.Length);
        await client.FlushAsync();

        using var reader = new StreamReader(client, System.Text.Encoding.UTF8, false, 4096, leaveOpen: true);
        var replyLine = await reader.ReadLineAsync();

        Assert.NotNull(replyLine);
        var reply = BridgeJson.Deserialize<JsonRpcEnvelope>(replyLine);
        Assert.NotNull(reply);
        Assert.NotNull(reply.Error);
        Assert.Equal(BridgeErrorCode.MethodNotFound, reply.Error.Code);

        await listener.StopAsync();
    }

    [Fact]
    public async Task Wire_NamedPipe_DynamicOptInToggle_TransitionsFromRefusalToExecution()
    {
        var pipeName = "hpdynamo-adv-tog-" + Guid.NewGuid().ToString("N");
        var fakeExecutor = new SimulatedBridgeExecutor();
        var settings = new BridgeSettings { ExecutionEnabled = false };
        using var listener = new PipeListener(
            pipeName,
            new RequestDispatcher(fakeExecutor, settings, "2026", "Dynamo", RefusalMessage));
        listener.Start();

        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(3000, CancellationToken.None);

        using var reader = new StreamReader(client, System.Text.Encoding.UTF8, false, 4096, leaveOpen: true);

        // 1. Rejected when ExecutionEnabled = false
        var req1 = JsonRpcEnvelope.Request(1, "dynamo.execute", new ExecuteRequest("return 1;"));
        var payload1 = System.Text.Encoding.UTF8.GetBytes(BridgeJson.Serialize(req1) + "\n");
        await client.WriteAsync(payload1, 0, payload1.Length);
        await client.FlushAsync();

        var reply1 = BridgeJson.Deserialize<JsonRpcEnvelope>((await reader.ReadLineAsync())!);
        Assert.NotNull(reply1?.Error);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, reply1.Error.Code);

        // 2. Toggle to true
        settings.ExecutionEnabled = true;

        // 3. Accepted when ExecutionEnabled = true
        var req2 = JsonRpcEnvelope.Request(2, "dynamo.execute", new ExecuteRequest("return 1;"));
        var payload2 = System.Text.Encoding.UTF8.GetBytes(BridgeJson.Serialize(req2) + "\n");
        await client.WriteAsync(payload2, 0, payload2.Length);
        await client.FlushAsync();

        var reply2 = BridgeJson.Deserialize<JsonRpcEnvelope>((await reader.ReadLineAsync())!);
        Assert.Null(reply2?.Error);
        Assert.Equal(2, reply2?.Id);

        await listener.StopAsync();
    }

    [Fact]
    public void FormatParameterOverrides_EmptyAndSpecialCharacters_HandlesSafely()
    {
        using var doc = JsonDocument.Parse("""
            {
                "empty": "",
                "quote": "text with \"quotes\"",
                "newline": "line1\nline2",
                "flag": true,
                "count": 0,
                "negative": -42.5
            }
            """);

        var json = HeadlessGraphRunner.FormatParameterOverrides(doc.RootElement);
        Assert.NotNull(json);

        using var result = JsonDocument.Parse(json);
        Assert.Equal(6, result.RootElement.GetArrayLength());
    }

    [Fact]
    public void SafeRollback_WhenRollbackAutoTransactionThrows_SurvivesAndReportsError()
    {
        var faultyCtx = new FaultyAutoTxContext();
        var req = new ExecuteRequest("throw new Exception();", "auto");
        using var cts = new CancellationTokenSource();

        var result = RevitTransactionScope.ExecuteWithContext(
            faultyCtx, req, new BridgeSettings { ExecutionEnabled = true }, null, cts,
            (_, _) => throw new InvalidOperationException("Action failed"));

        Assert.True(result.IsError);
        Assert.False(result.RolledBack);
        Assert.Contains("Action failed", result.Message);
    }

    private sealed class FaultyAutoTxContext : ITransactionContext
    {
        public bool IsGroupStarted => true;
        public bool IsAutoTransactionStarted => true;
        public System.Collections.Generic.IReadOnlyList<string> Warnings => Array.Empty<string>();

        public void StartGroup(string label) { }
        public void StartAutoTransaction(string label) { }
        public bool CommitAutoTransaction() => false;
        public void RollbackAutoTransaction() => throw new InvalidOperationException("Auto-tx rollback failed in host.");
        public void RollbackGroup() { }
        public void AssimilateGroup() { }
        public void Dispose() { }
    }

    private sealed class SimulatedBridgeExecutor : IBridgeExecutor
    {
        public bool IsBusy => false;
        public int CompiledScriptCount => 0;
        public string? ActiveDocumentTitle => "AdversarialDoc";
        public event Action? StateChanged { add { } remove { } }
        public event Action<LastRunInfo>? RunCompleted { add { } remove { } }

        public Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken cancellationToken) =>
            Task.FromResult(new ExecuteResult { Value = JsonSerializer.SerializeToElement("ok") });

        public Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken cancellationToken) =>
            Task.FromResult(new ContextResult { Host = "dynamo", ExecutionEnabled = false });

        public InspectResult Inspect(InspectRequest request) => new();
        public AnalyzeResult Analyze(AnalyzeRequest request) => new();
        public CancelResult Cancel() => new(false, false);
    }
}
