using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Models;
using HPRebar.Mcp.Server.Services;
using HPRebar.Mcp.Server.Tests.Fakes;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HPRebar.Mcp.Server.Tests;

/// <summary>
///     The server's RevitBridgeClient talking to the bridge's PipeListener over a real Windows named
///     pipe, with a fake behind the dispatcher instead of Revit. Covers framing, correlation, error
///     codes and progress — everything except the Revit thread itself.
/// </summary>
public sealed class PipeRoundTripTests : IAsyncLifetime
{
    private readonly string _pipeName = "hprebar-mcp-test-" + Guid.NewGuid().ToString("N");
    private readonly FakeRevitExecutor _executor = new FakeRevitExecutor();
    private readonly BridgeSettings _settings = new BridgeSettings();
    private PipeListener _listener = null!;
    private RevitBridgeClient _client = null!;

    public ValueTask InitializeAsync()
    {
        _listener = new PipeListener(_pipeName, new RequestDispatcher(_executor, _settings, "2026"));
        _listener.Start();

        var options = Options.Create(new BridgeOptions { PipeName = _pipeName, ConnectTimeoutMs = 3000, PingIntervalSeconds = 60 });
        _client = new RevitBridgeClient(options, NullLogger<RevitBridgeClient>.Instance);

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        await _listener.StopAsync();
        _listener.Dispose();
    }

    private static TimeSpan Timeout => TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Ping_round_trips_with_bridge_state()
    {
        _settings.ExecutionEnabled = true;

        var pong = await _client.SendAsync<BridgePingResult>(JsonRpcMethods.Ping, null, Timeout, null, TestContext.Current.CancellationToken);

        Assert.True(pong.Pong);
        Assert.Equal("2026", pong.RevitVersion);
        Assert.True(pong.ExecutionEnabled);
        Assert.True(_client.IsConnected);
    }

    [Fact]
    public async Task Execute_is_refused_with_actionable_code_while_execution_is_disabled()
    {
        _settings.ExecutionEnabled = false;

        var error = await Assert.ThrowsAsync<BridgeErrorException>(() =>
            _client.SendAsync<ExecuteResult>(JsonRpcMethods.Execute, new ExecuteRequest("return 1;"), Timeout, null, TestContext.Current.CancellationToken));

        Assert.Equal(BridgeErrorCode.ExecutionDisabled, error.Code);
        Assert.True(BridgeErrorCode.IsActionable(error.Code));
        Assert.Null(_executor.LastExecuteRequest);
    }

    [Fact]
    public async Task A_request_exception_from_the_executor_keeps_its_own_error_code()
    {
        _executor.ContextFailure = BridgeRequestException.NoActiveDocument("AutoCAD", "drawing");

        var error = await Assert.ThrowsAsync<BridgeErrorException>(() =>
            _client.SendAsync<ContextResult>(JsonRpcMethods.Context, new ContextRequest(), Timeout, null, TestContext.Current.CancellationToken));

        Assert.Equal(BridgeErrorCode.NoActiveDocument, error.Code);
        Assert.True(BridgeErrorCode.IsActionable(error.Code));
        Assert.Contains("No drawing is open in AutoCAD", error.Message);

        _executor.ContextFailure = new InvalidOperationException(@"C:\secret\path.dwg exploded");
        error = await Assert.ThrowsAsync<BridgeErrorException>(() =>
            _client.SendAsync<ContextResult>(JsonRpcMethods.Context, new ContextRequest(), Timeout, null, TestContext.Current.CancellationToken));

        Assert.Equal(BridgeErrorCode.InternalError, error.Code);
        Assert.DoesNotContain("secret", error.Message);
    }

    [Fact]
    public async Task Execute_returns_result_and_forwards_progress_for_the_right_request()
    {
        _settings.ExecutionEnabled = true;
        _executor.ProgressSteps = 3;
        var seen = new List<ProgressParams>();
        var progress = new Progress<ProgressParams>(seen.Add);

        var result = await _client.SendAsync<ExecuteResult>(
            JsonRpcMethods.Execute,
            new ExecuteRequest("return 42;", TransactionModes.None, DryRun: true, TimeoutSeconds: 7, Label: "answer"),
            Timeout, progress, TestContext.Current.CancellationToken);

        Assert.False(result.IsError);
        Assert.Equal(42, result.Value!.Value.GetInt32());
        Assert.Equal("none", _executor.LastExecuteRequest!.Transaction);
        Assert.True(_executor.LastExecuteRequest.DryRun);
        Assert.Equal(7, _executor.LastExecuteRequest.TimeoutSeconds);

        // Progress<T> posts to the thread pool; give it a moment to drain.
        await Task.Delay(100, TestContext.Current.CancellationToken);
        Assert.Equal(3, seen.Count);
        Assert.All(seen, p => Assert.Equal(3, p.Total));
        Assert.Equal(seen.Select(p => p.Progress), [1, 2, 3]);
    }

    [Fact]
    public async Task Rapid_progress_reports_arrive_in_order()
    {
        _settings.ExecutionEnabled = true;
        _executor.ProgressSteps = 20;
        _executor.ProgressDelayMs = 0;
        var seen = new List<int>();
        var progress = new HPRebar.Mcp.Contracts.SynchronousProgress<ProgressParams>(p => { lock (seen) seen.Add(p.Progress); });

        await _client.SendAsync<ExecuteResult>(JsonRpcMethods.Execute, new ExecuteRequest("return 1;"), Timeout, progress, TestContext.Current.CancellationToken);

        // Notifications are written through one lock in call order and read by one loop: strictly ascending.
        await Task.Delay(100, TestContext.Current.CancellationToken);
        Assert.Equal(Enumerable.Range(1, 20), seen);
    }

    [Fact]
    public async Task Busy_bridge_answers_immediately_with_busy_code()
    {
        _settings.ExecutionEnabled = true;
        _executor.IsBusy = true;

        var error = await Assert.ThrowsAsync<BridgeErrorException>(() =>
            _client.SendAsync<ExecuteResult>(JsonRpcMethods.Execute, new ExecuteRequest("return 1;"), Timeout, null, TestContext.Current.CancellationToken));

        Assert.Equal(BridgeErrorCode.Busy, error.Code);
    }

    [Fact]
    public async Task Unknown_method_is_a_protocol_error_not_an_actionable_one()
    {
        var error = await Assert.ThrowsAsync<BridgeErrorException>(() =>
            _client.SendAsync<BridgePingResult>("revit.nope", null, Timeout, null, TestContext.Current.CancellationToken));

        Assert.Equal(BridgeErrorCode.MethodNotFound, error.Code);
        Assert.False(BridgeErrorCode.IsActionable(error.Code));
    }

    [Fact]
    public async Task Context_and_inspect_and_cancel_round_trip()
    {
        var context = await _client.SendAsync<ContextResult>(JsonRpcMethods.Context, new ContextRequest(true), Timeout, null, TestContext.Current.CancellationToken);
        Assert.Equal("Project1", context.DocTitle);
        Assert.Single(context.Selection);
        Assert.Equal(1001, context.Selection[0].Id);

        var inspect = await _client.SendAsync<InspectResult>(JsonRpcMethods.Inspect, new InspectRequest("Wall"), Timeout, null, TestContext.Current.CancellationToken);
        Assert.Equal("Autodesk.Revit.DB.Wall", inspect.FullName);
        Assert.Single(inspect.Members);

        var cancel = await _client.SendAsync<CancelResult>(JsonRpcMethods.Cancel, null, Timeout, null, TestContext.Current.CancellationToken);
        Assert.False(cancel.WasRunning);
        Assert.Equal(1, _executor.CancelCalls);
    }

    [Fact]
    public async Task Concurrent_requests_are_correlated_by_id()
    {
        _settings.ExecutionEnabled = true;

        var tasks = Enumerable.Range(0, 20).Select(i =>
            _client.SendAsync<InspectResult>(JsonRpcMethods.Inspect, new InspectRequest("Type" + i), Timeout, null, TestContext.Current.CancellationToken));

        var results = await Task.WhenAll(tasks);

        Assert.Equal(20, results.Length);
        for (var i = 0; i < 20; i++) Assert.Equal("Type" + i, results[i].TypeName);
    }

    [Fact]
    public async Task Execute_carries_args_beside_the_code()
    {
        _settings.ExecutionEnabled = true;
        _executor.ExecuteHandler = request => new ExecuteResult
        {
            Value = request.Args, // echo what crossed the pipe
            ValueType = "args",
        };
        var args = System.Text.Json.JsonSerializer.SerializeToElement(new { spacing = 150, names = new[] { "A", "B" } });

        var result = await _client.SendAsync<ExecuteResult>(JsonRpcMethods.Execute,
            new ExecuteRequest("return args.Double(\"spacing\");", Args: args), Timeout, null, TestContext.Current.CancellationToken);

        Assert.NotNull(_executor.LastExecuteRequest?.Args);
        Assert.Equal(150, _executor.LastExecuteRequest!.Args!.Value.GetProperty("spacing").GetInt32());
        Assert.Equal("B", result.Value!.Value.GetProperty("names")[1].GetString());
    }

    [Fact]
    public async Task Analyze_round_trips_without_execution_and_without_opt_in()
    {
        _settings.ExecutionEnabled = false;

        var analysis = await _client.SendAsync<AnalyzeResult>(JsonRpcMethods.Analyze,
            new AnalyzeRequest("double spacing = 150;\nreturn args.Int(\"count\");"), Timeout, null, TestContext.Current.CancellationToken);

        Assert.True(analysis.Compiles);
        Assert.Equal(2, analysis.LineCount);
        Assert.Equal("spacing", Assert.Single(analysis.Literals).BoundTo);
        Assert.Equal("count", Assert.Single(analysis.ArgKeys).Key);
        Assert.Null(_executor.LastExecuteRequest);

        var error = await Assert.ThrowsAsync<BridgeErrorException>(() =>
            _client.SendAsync<AnalyzeResult>(JsonRpcMethods.Analyze, new AnalyzeRequest(""), Timeout, null, TestContext.Current.CancellationToken));
        Assert.Equal(BridgeErrorCode.InvalidRequest, error.Code);
    }
}
