using System.Text.Json;
using HPWebHost.Protocol;
using Xunit;

namespace HPWebHost.Tests.Protocol;

public sealed class CommandDispatcherTests
{
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(100);

    [Fact]
    public async Task DispatchAsync_RegisteredCommand_ReturnsSuccessWithPayload()
    {
        var dispatcher = new CommandDispatcher(TimeSpan.FromSeconds(5));
        dispatcher.Register("echo", (payload, _) => Task.FromResult<object?>(payload?.GetProperty("text").GetString()));

        var response = await dispatcher.DispatchAsync(Request("echo", """{"text":"hi"}"""));

        Assert.True(response.Ok);
        Assert.Equal("r1", response.Id);
        Assert.Equal("hi", response.Payload);
    }

    [Fact]
    public async Task DispatchAsync_UnknownCommand_ReturnsUnknownCommand()
    {
        var dispatcher = new CommandDispatcher(TimeSpan.FromSeconds(5));

        var response = await dispatcher.DispatchAsync(Request("nope"));

        Assert.False(response.Ok);
        Assert.Equal(BridgeErrorCodes.UnknownCommand, response.Error!.Code);
    }

    [Fact]
    public async Task DispatchAsync_HandlerSlowerThanTimeout_ReturnsTimeoutAndCancelsToken()
    {
        var dispatcher = new CommandDispatcher(ShortTimeout);
        var observed = new TaskCompletionSource<bool>();
        dispatcher.Register("slow", async (_, token) =>
        {
            token.Register(() => observed.TrySetResult(true));
            await Task.Delay(Timeout.Infinite, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
            return null;
        });

        var response = await dispatcher.DispatchAsync(Request("slow"));

        Assert.Equal(BridgeErrorCodes.Timeout, response.Error!.Code);
        Assert.True(await observed.Task.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task DispatchAsync_CallerCancels_ReturnsCancelled()
    {
        var dispatcher = new CommandDispatcher(TimeSpan.FromSeconds(5));
        dispatcher.Register("wait", async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return null;
        });
        using var caller = new CancellationTokenSource(ShortTimeout);

        var response = await dispatcher.DispatchAsync(Request("wait"), caller.Token);

        Assert.Equal(BridgeErrorCodes.Cancelled, response.Error!.Code);
    }

    [Theory]
    [InlineData(BridgeErrorCodes.NoDocument)]
    [InlineData(BridgeErrorCodes.Busy)]
    public async Task DispatchAsync_HandlerThrowsBridgeException_ReturnsItsCode(string code)
    {
        var dispatcher = new CommandDispatcher(TimeSpan.FromSeconds(5));
        dispatcher.Register("fail", (_, _) => throw new BridgeException(code, "refused"));

        var response = await dispatcher.DispatchAsync(Request("fail"));

        Assert.Equal(code, response.Error!.Code);
        Assert.Equal("refused", response.Error.Message);
    }

    [Fact]
    public async Task DispatchAsync_HandlerThrowsArgumentException_ReturnsInternalBecauseItIsAHostBug()
    {
        var dispatcher = new CommandDispatcher(TimeSpan.FromSeconds(5));
        dispatcher.Register("bug", (_, _) => throw new ArgumentException("value out of range"));

        var response = await dispatcher.DispatchAsync(Request("bug"));

        Assert.Equal(BridgeErrorCodes.Internal, response.Error!.Code);
    }

    [Fact]
    public async Task DispatchAsync_Timeout_CancelsTheHandlerTokenBeforeAnswering()
    {
        var dispatcher = new CommandDispatcher(ShortTimeout);
        var queued = new TaskCompletionSource<object?>();
        CancellationToken handlerToken = default;
        dispatcher.Register("queued", (_, token) =>
        {
            handlerToken = token;
            return queued.Task;
        });

        var response = await dispatcher.DispatchAsync(Request("queued"));

        Assert.Equal(BridgeErrorCodes.Timeout, response.Error!.Code);
        Assert.True(handlerToken.IsCancellationRequested);
    }

    [Fact]
    public async Task DispatchAsync_CommandWithLongerTimeout_OutlivesTheDefault()
    {
        var dispatcher = new CommandDispatcher(ShortTimeout);
        dispatcher.Register("long", async (_, token) =>
        {
            await Task.Delay(ShortTimeout * 3, token);
            return "done";
        }, timeout: TimeSpan.FromSeconds(5));

        var response = await dispatcher.DispatchAsync(Request("long"));

        Assert.True(response.Ok);
        Assert.Equal("done", response.Payload);
    }

    [Fact]
    public async Task DispatchAsync_UnmappedException_UsesHostTranslatorThenInternal()
    {
        var translated = new CommandDispatcher(TimeSpan.FromSeconds(5),
            exception => exception is InvalidOperationException ? new BridgeError(BridgeErrorCodes.RevitApiError, "api") : null);
        translated.Register("api", (_, _) => throw new InvalidOperationException("element deleted"));
        translated.Register("crash", (_, _) => throw new NullReferenceException("boom"));

        var api = await translated.DispatchAsync(Request("api"));
        var crash = await translated.DispatchAsync(Request("crash"));

        Assert.Equal(BridgeErrorCodes.RevitApiError, api.Error!.Code);
        Assert.Equal(BridgeErrorCodes.Internal, crash.Error!.Code);
    }

    [Fact]
    public async Task DispatchAsync_TypedPayloadDoesNotFit_ReturnsBadRequest()
    {
        var dispatcher = new CommandDispatcher(TimeSpan.FromSeconds(5));
        dispatcher.Register<ScopedRequest>("scoped", (request, _) => Task.FromResult<object?>(request.Scope));

        var good = await dispatcher.DispatchAsync(Request("scoped", """{"scope":"activeView"}"""));
        var missing = await dispatcher.DispatchAsync(Request("scoped"));
        var bad = await dispatcher.DispatchAsync(Request("scoped", """{"scope":"everything"}"""));

        Assert.Equal(Scope.ActiveView, good.Payload);
        Assert.Equal(Scope.Model, missing.Payload);
        Assert.Equal(BridgeErrorCodes.BadRequest, bad.Error!.Code);
    }

    [Fact]
    public void Register_SameCommandTwice_Throws()
    {
        var dispatcher = new CommandDispatcher(TimeSpan.FromSeconds(5));
        dispatcher.Register("once", (_, _) => Task.FromResult<object?>(null));

        Assert.Throws<InvalidOperationException>(() => dispatcher.Register("once", (_, _) => Task.FromResult<object?>(null)));
    }

    public enum Scope
    {
        Model,
        ActiveView,
    }

    public sealed record ScopedRequest(Scope Scope = Scope.Model);

    private static RequestEnvelope Request(string command, string? payloadJson = null) =>
        new("r1", command, payloadJson is null ? null : JsonDocument.Parse(payloadJson).RootElement.Clone());
}
