using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using HPRebar.Mcp.Server.Tests.Fakes;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     The pipe half of the engine on .NET Framework, where <c>PipeOptions.CurrentUserOnly</c> does not
///     exist and the listener builds a <c>PipeSecurity</c> owned by the current user instead. A raw
///     NDJSON client stands in for the net10 server: connect, one line per request, one line per reply.
/// </summary>
public sealed class PipeListenerNet48Tests : IDisposable
{
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

    private readonly string _pipeName = "hpnavis-net48-test-" + Guid.NewGuid().ToString("N");
    private readonly FakeRevitExecutor _executor = new FakeRevitExecutor();
    private readonly BridgeSettings _settings = new BridgeSettings();
    private readonly PipeListener _listener;

    public PipeListenerNet48Tests()
    {
        _listener = new PipeListener(_pipeName, new RequestDispatcher(_executor, _settings, "2026", "Navisworks"));
        _listener.Start();
    }

    public void Dispose() => _listener.Dispose();

    [Fact]
    public async Task Ping_round_trips_over_a_pipe_the_current_user_owns()
    {
        using var client = await ConnectAsync();

        // The whole point of the net48 branch: the pipe is owned by the caller's Owner SID with a single FullControl
        // rule — what the net10 client's PipeOptions.CurrentUserOnly checks before it talks to us.
        using var identity = WindowsIdentity.GetCurrent();
        var acl = client.GetAccessControl();
        Assert.Equal(identity.Owner, acl.GetOwner(typeof(SecurityIdentifier)));
        var rule = Assert.Single(acl.GetAccessRules(true, false, typeof(SecurityIdentifier)).Cast<PipeAccessRule>());
        Assert.Equal(identity.Owner, rule.IdentityReference);
        Assert.Equal(PipeAccessRights.FullControl, rule.PipeAccessRights);
        Assert.Equal(AccessControlType.Allow, rule.AccessControlType);

        var reply = await CallAsync(client, 1, "navis.ping", null);

        Assert.Null(reply.Error);
        var pong = reply.ResultAs<BridgePingResult>();
        Assert.NotNull(pong);
        Assert.True(pong!.Pong);
        Assert.Equal("2026", pong.RevitVersion);
        Assert.False(pong.ExecutionEnabled);
    }

    [Fact]
    public async Task Execute_is_refused_with_the_actionable_code_until_the_user_opts_in()
    {
        using var client = await ConnectAsync();
        var request = new ExecuteRequest("return 1;", TransactionModes.None, false, 30, "probe");

        var refused = await CallAsync(client, 2, "navis.execute", request);

        Assert.NotNull(refused.Error);
        Assert.Equal(BridgeErrorCode.ExecutionDisabled, refused.Error!.Code);
        Assert.Contains("Navisworks", refused.Error.Message);
        Assert.Null(_executor.LastExecuteRequest);

        _settings.ExecutionEnabled = true;
        var accepted = await CallAsync(client, 3, "navis.execute", request);

        Assert.Null(accepted.Error);
        var result = accepted.ResultAs<ExecuteResult>();
        Assert.NotNull(result);
        Assert.False(result!.IsError);
        Assert.Equal(42, result.Value!.Value.GetInt32());
        Assert.Equal("probe", _executor.LastExecuteRequest!.Label);
    }

    [Fact]
    public async Task Context_reply_carries_the_host_neutral_fields()
    {
        using var client = await ConnectAsync();

        var reply = await CallAsync(client, 4, "navis.context", new ContextRequest(true));

        Assert.Null(reply.Error);
        var context = reply.ResultAs<ContextResult>();
        Assert.NotNull(context);
        Assert.Equal("Project1", context!.DocTitle);
        Assert.Single(context.Selection);
        Assert.True(_executor.LastContextIncludedSelection);
    }

    [Fact]
    public async Task Second_listener_on_the_same_name_faults_instead_of_sharing_it()
    {
        // Both accept loops would race to create the pipe; wait until the first one owns it, as the net10 suite does.
        var ready = DateTime.UtcNow.AddSeconds(5);
        while (!Directory.EnumerateFiles(@"\\.\pipe\").Any(f => f.EndsWith(_pipeName, StringComparison.Ordinal)) && DateTime.UtcNow < ready)
            await Task.Delay(25, TestContext.Current.CancellationToken);

        string? fault = null;
        using var second = new PipeListener(_pipeName, new RequestDispatcher(_executor, _settings, "2026", "Navisworks"));
        second.Faulted += reason => fault = reason;

        second.Start();
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (fault is null && DateTime.UtcNow < deadline) await Task.Delay(50, TestContext.Current.CancellationToken);

        Assert.NotNull(fault);
        Assert.Contains("Navisworks 2026", fault);
        Assert.False(second.IsListening);
    }

    private async Task<NamedPipeClientStream> ConnectAsync()
    {
        var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(5000, TestContext.Current.CancellationToken);
        return client;
    }

    private static async Task<JsonRpcEnvelope> CallAsync(NamedPipeClientStream client, long id, string method, object? parameters)
    {
        var line = BridgeJson.Serialize(JsonRpcEnvelope.Request(id, method, parameters)) + "\n";
        var bytes = Utf8NoBom.GetBytes(line);
        await client.WriteAsync(bytes, 0, bytes.Length, TestContext.Current.CancellationToken);
        await client.FlushAsync(TestContext.Current.CancellationToken);

        // Notifications (status, progress) may arrive before the reply: keep reading until the id matches.
        var reader = new StreamReader(client, Utf8NoBom, false, 65536, leaveOpen: true);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var text = await reader.ReadLineAsync();
            if (text is null) break;
            var envelope = BridgeJson.Deserialize<JsonRpcEnvelope>(text);
            if (envelope?.Id == id && envelope.Kind == JsonRpcKind.Response) return envelope;
        }

        throw new TimeoutException($"no reply to {method} #{id}");
    }
}
