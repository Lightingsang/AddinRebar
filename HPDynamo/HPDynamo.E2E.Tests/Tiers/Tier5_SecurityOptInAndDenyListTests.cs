namespace HPDynamo.E2E.Tests.Tiers;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HPDynamo.E2E.Tests.Contracts;
using HPDynamo.E2E.Tests.Harness;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.McpBridge.Core.Scripting;
using Xunit;

/// <summary>
/// Tier 5: Adversarial Security Hardening Suite for HPDynamo MCP Subsystem.
/// Stress-tests opt-in gate bypass resistance, rapid state flipping, and ScriptGuard deny-list evasion.
/// </summary>
public sealed class Tier5_SecurityOptInAndDenyListTests
{
    private const string RefusalMessage =
        "Code execution is disabled. Ask the user to tick 'Allow AI Dynamo execution' in the HPDynamo MCP Bridge window inside Revit.";

    [Fact]
    public void TC_T5_SEC_01_MalformedJsonRpc_MissingVersionOrMethod_SafelyRejected()
    {
        var rawJsonMissingMethod = "{\"id\":1,\"params\":{}}";
        var env1 = BridgeJson.Deserialize<JsonRpcEnvelope>(rawJsonMissingMethod);
        Assert.NotNull(env1);
        Assert.Null(env1.Method);

        var rawJsonMalformed = "{ bad_json: true ";
        Assert.ThrowsAny<Exception>(() => BridgeJson.Deserialize<JsonRpcEnvelope>(rawJsonMalformed));
    }

    [Fact]
    public async Task TC_T5_SEC_02_ExecutionDisabled_DirectPipeRejection_ReturnsMinus32001()
    {
        var pipeName = "hpdynamo-t5-sec-" + Guid.NewGuid().ToString("N");
        var fakeExec = new SimulatedTestExecutor();
        var settings = new BridgeSettings { ExecutionEnabled = false };

        using var listener = new PipeListener(
            pipeName,
            new RequestDispatcher(fakeExec, settings, "2026", "Dynamo", RefusalMessage));
        listener.Start();

        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(3000, CancellationToken.None);

        var req = JsonRpcEnvelope.Request(1, "dynamo.execute", new ExecuteRequest("return 42;"));
        var payload = System.Text.Encoding.UTF8.GetBytes(BridgeJson.Serialize(req) + "\n");
        await client.WriteAsync(payload, 0, payload.Length);
        await client.FlushAsync();

        using var reader = new StreamReader(client, System.Text.Encoding.UTF8, false, 4096, leaveOpen: true);
        var replyLine = await reader.ReadLineAsync();

        Assert.NotNull(replyLine);
        var reply = BridgeJson.Deserialize<JsonRpcEnvelope>(replyLine);
        Assert.NotNull(reply?.Error);
        Assert.Equal(DynamoE2EContracts.OptInDisabledErrorCode, reply.Error.Code);
        Assert.Contains(DynamoE2EContracts.OptInDisabledMessageFragment, reply.Error.Message);

        await listener.StopAsync();
    }

    [Fact]
    public async Task TC_T5_SEC_03_NonExecutionMethods_PermittedWhenOptInIsDisabled()
    {
        var pipeName = "hpdynamo-t5-bypass-" + Guid.NewGuid().ToString("N");
        var fakeExec = new SimulatedTestExecutor();
        var settings = new BridgeSettings { ExecutionEnabled = false };

        using var listener = new PipeListener(
            pipeName,
            new RequestDispatcher(fakeExec, settings, "2026", "Dynamo", RefusalMessage));
        listener.Start();

        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(3000, CancellationToken.None);
        using var reader = new StreamReader(client, System.Text.Encoding.UTF8, false, 4096, leaveOpen: true);

        // 1. dynamo.ping is permitted
        var pingReq = JsonRpcEnvelope.Request(10, "dynamo.ping", null);
        var pingBytes = System.Text.Encoding.UTF8.GetBytes(BridgeJson.Serialize(pingReq) + "\n");
        await client.WriteAsync(pingBytes, 0, pingBytes.Length);
        await client.FlushAsync();
        var pingReply = BridgeJson.Deserialize<JsonRpcEnvelope>((await reader.ReadLineAsync())!);
        Assert.Null(pingReply?.Error);

        // 2. dynamo.context is permitted
        var ctxReq = JsonRpcEnvelope.Request(11, "dynamo.context", new ContextRequest(false));
        var ctxBytes = System.Text.Encoding.UTF8.GetBytes(BridgeJson.Serialize(ctxReq) + "\n");
        await client.WriteAsync(ctxBytes, 0, ctxBytes.Length);
        await client.FlushAsync();
        var ctxReply = BridgeJson.Deserialize<JsonRpcEnvelope>((await reader.ReadLineAsync())!);
        Assert.Null(ctxReply?.Error);

        // 3. dynamo.cancel is permitted
        var cncReq = JsonRpcEnvelope.Request(12, "dynamo.cancel", null);
        var cncBytes = System.Text.Encoding.UTF8.GetBytes(BridgeJson.Serialize(cncReq) + "\n");
        await client.WriteAsync(cncBytes, 0, cncBytes.Length);
        await client.FlushAsync();
        var cncReply = BridgeJson.Deserialize<JsonRpcEnvelope>((await reader.ReadLineAsync())!);
        Assert.Null(cncReply?.Error);

        await listener.StopAsync();
    }

    [Fact]
    public async Task TC_T5_SEC_04_RapidOptInStateFlipping_ConcurrentDispatches_ZeroLeakage()
    {
        var settings = new BridgeSettings { ExecutionEnabled = false };
        var fakeExec = new SimulatedTestExecutor();
        var pipeName = "hpdynamo-t5-flip-" + Guid.NewGuid().ToString("N");

        using var listener = new PipeListener(
            pipeName,
            new RequestDispatcher(fakeExec, settings, "2026", "Dynamo", RefusalMessage));
        listener.Start();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        var toggleTask = Task.Run(() =>
        {
            var state = false;
            while (!cts.Token.IsCancellationRequested)
            {
                settings.ExecutionEnabled = state;
                state = !state;
                Thread.Sleep(5);
            }
        });

        var results = new ConcurrentBag<JsonRpcEnvelope>();
        var clientTasks = Enumerable.Range(0, 5).Select(async clientId =>
        {
            try
            {
                using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                await client.ConnectAsync(3000, cts.Token);
                using var reader = new StreamReader(client, System.Text.Encoding.UTF8, false, 4096, leaveOpen: true);

                for (int i = 0; i < 10 && !cts.Token.IsCancellationRequested; i++)
                {
                    var req = JsonRpcEnvelope.Request(clientId * 100 + i, "dynamo.execute", new ExecuteRequest("return 1;"));
                    var payload = System.Text.Encoding.UTF8.GetBytes(BridgeJson.Serialize(req) + "\n");
                    await client.WriteAsync(payload, 0, payload.Length, cts.Token);
                    await client.FlushAsync(cts.Token);

                    var line = await reader.ReadLineAsync(cts.Token);
                    if (line != null)
                    {
                        var resp = BridgeJson.Deserialize<JsonRpcEnvelope>(line);
                        if (resp != null) results.Add(resp);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on timeout
            }
        }).ToArray();

        await Task.WhenAll(clientTasks.Concat(new[] { toggleTask }));
        await listener.StopAsync();

        Assert.NotEmpty(results);
        foreach (var r in results)
        {
            if (r.Error != null)
            {
                Assert.Equal(BridgeErrorCode.ExecutionDisabled, r.Error.Code);
            }
            else
            {
                Assert.NotNull(r.Result);
            }
        }
    }

    [Theory]
    [InlineData("dynamic d = 1; return d;")]
    [InlineData("var d = (dynamic)workspace; return d.Name;")]
    public void TC_T5_GRD_01_DynamicTypeEvasion_BlockedByScriptGuard(string script)
    {
        var diagnostics = ScriptGuard.Check(script, GuardProfile.Dynamo);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains("dynamic is not allowed"));
    }

    [Theory]
    [InlineData("global::System.IO.File.Delete(\"test.txt\"); return 1;")]
    [InlineData("global::System.Diagnostics.Process.Start(\"calc.exe\"); return 1;")]
    [InlineData("global::System.Net.Http.HttpClient c = null; return 1;")]
    [InlineData("global::System.Reflection.Assembly a = null; return 1;")]
    [InlineData("global::HPDynamo.McpBridge.Coordinator.DynamoBridgeCoordinator c = null; return 1;")]
    [InlineData("global::HPRebar.McpBridge.Core.Host.McpBridgeHost h = null; return 1;")]
    public void TC_T5_GRD_02_GlobalNamespacePrefix_BlockedByScriptGuard(string script)
    {
        var diagnostics = ScriptGuard.Check(script, GuardProfile.Dynamo);
        Assert.NotEmpty(diagnostics);
    }

    [Theory]
    [InlineData("#r \"System.Drawing.dll\"\nreturn 1;")]
    [InlineData("#load \"Helper.csx\"\nreturn 1;")]
    public void TC_T5_GRD_03_Directives_HashR_HashLoad_BlockedByScriptGuard(string script)
    {
        var diagnostics = ScriptGuard.Check(script, GuardProfile.Dynamo);
        Assert.NotEmpty(diagnostics);
        Assert.Contains(diagnostics, d => d.Message.Contains("is not allowed in Dynamo scripts"));
    }

    [Theory]
    [InlineData("#nullable enable\nint? x = null; return x ?? 0;")]
    [InlineData("#pragma warning disable CS0168\ntry { return 1; } catch (Exception ex) { /* ignored */ return 0; }")]
    public void TC_T5_GRD_04_BenignDirectives_AllowedByScriptGuard(string script)
    {
        var diagnostics = ScriptGuard.Check(script, GuardProfile.Dynamo);
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("System.Windows.Forms.MessageBox.Show(\"Hacked\"); return 1;")]
    [InlineData("Autodesk.Revit.UI.TaskDialog.Show(\"Revit\", \"Warning\"); return 1;")]
    [InlineData("app.Quit(); return 1;")]
    [InlineData("app.Exit(); return 1;")]
    public void TC_T5_GRD_05_ModalUIAndAppExit_BlockedByScriptGuard(string script)
    {
        var diagnostics = ScriptGuard.Check(script, GuardProfile.Dynamo);
        Assert.NotEmpty(diagnostics);
    }

    [Theory]
    [InlineData("unsafe { int* p = null; } return 1;")]
    [InlineData("Func<System.Threading.Tasks.Task> fn = async () => { }; return 1;")]
    public void TC_T5_GRD_06_UnsafeAndAsyncLambdas_BlockedByScriptGuard(string script)
    {
        var diagnostics = ScriptGuard.Check(script, GuardProfile.Dynamo);
        Assert.NotEmpty(diagnostics);
    }

    private sealed class SimulatedTestExecutor : IBridgeExecutor
    {
        public bool IsBusy => false;
        public int CompiledScriptCount => 0;
        public string? ActiveDocumentTitle => "Doc";
        public event Action? StateChanged { add { } remove { } }
        public event Action<LastRunInfo>? RunCompleted { add { } remove { } }

        public Task<ExecuteResult> ExecuteAsync(ExecuteRequest request, IProgress<ScriptProgress>? progress, CancellationToken ct) =>
            Task.FromResult(new ExecuteResult { Value = System.Text.Json.JsonSerializer.SerializeToElement("ok") });

        public Task<ContextResult> GetContextAsync(bool includeSelection, CancellationToken ct) =>
            Task.FromResult(new ContextResult { Host = "dynamo", ExecutionEnabled = false });

        public InspectResult Inspect(InspectRequest request) => new();
        public AnalyzeResult Analyze(AnalyzeRequest request) => new();
        public CancelResult Cancel() => new(false, false);
    }
}
