using System.Collections.Concurrent;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.Mcp.Server.Hosts;
using HPRebar.Mcp.Server.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HPRebar.Mcp.Server.Services;

/// <summary>
///     Request/response correlation over one <see cref="NdjsonPipeTransport"/>. Connects lazily on the
///     first call so the server starts instantly even when the host is closed, fails every in-flight call
///     the moment the pipe drops, and reconnects in the background with backoff so the next tool call
///     usually finds the bridge ready again.
/// </summary>
public sealed class RevitBridgeClient : IRevitBridgeClient, IAsyncDisposable
{
    private readonly BridgeOptions _options;
    private readonly ILogger<RevitBridgeClient> _logger;
    private readonly IHostProfile _profile;
    private readonly ConcurrentDictionary<long, PendingCall> _pending = new ConcurrentDictionary<long, PendingCall>();
    private readonly SemaphoreSlim _connectLock = new SemaphoreSlim(1, 1);
    private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

    private NdjsonPipeTransport? _transport;
    private CancellationTokenSource? _sessionCts;
    private long _nextId;

    /// <param name="profile">Names the pipe methods and the host in messages; defaults to the Revit profile.</param>
    public RevitBridgeClient(IOptions<BridgeOptions> options, ILogger<RevitBridgeClient> logger, IHostProfile? profile = null)
    {
        _options = options.Value;
        _logger = logger;
        _profile = profile ?? HostProfile.Revit;
    }

    public IHostProfile Profile => _profile;

    public bool IsConnected => _transport is { IsConnected: true };

    public string PipeName => _options.PipeName;

    public StatusParams? LastStatus { get; private set; }

    public async Task<T> SendAsync<T>(
        string method,
        object? parameters,
        TimeSpan timeout,
        IProgress<ProgressParams>? progress,
        CancellationToken cancellationToken)
    {
        var transport = await EnsureConnectedAsync(cancellationToken).ConfigureAwait(false);
        var id = Interlocked.Increment(ref _nextId);
        var pending = new PendingCall(progress);
        _pending[id] = pending;

        try
        {
            await transport.SendAsync(JsonRpcEnvelope.Request(id, method, parameters), cancellationToken).ConfigureAwait(false);

            var response = await WaitForResponseAsync(pending, id, timeout, cancellationToken).ConfigureAwait(false);

            if (response.Error is { } error) throw new BridgeErrorException(error.Code, error.Message);

            return response.ResultAs<T>()
                   ?? throw new BridgeErrorException(BridgeErrorCode.InternalError, $"Bridge returned an empty result for {method}.");
        }
        finally
        {
            _pending.TryRemove(id, out _);
        }
    }

    private async Task<JsonRpcEnvelope> WaitForResponseAsync(PendingCall pending, long id, TimeSpan timeout, CancellationToken cancellationToken)
    {
        try
        {
            return await pending.Completion.Task.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            TryCancelInRevit(id);
            throw new BridgeTimeoutException(
                $"{_profile.DisplayName} did not answer within {timeout.TotalSeconds:0}s. "
                + (_profile.TimeoutSemanticsHint
                   ?? $"The timeout is cooperative: {_profile.DisplayName} may still be finishing the script, and nothing has been committed until it does."));
        }
        catch (OperationCanceledException)
        {
            TryCancelInRevit(id);
            throw;
        }
    }

    /// <summary>Best effort: tells the bridge to cancel; the caller has already given up on the answer.</summary>
    private void TryCancelInRevit(long id)
    {
        _ = SendAsync<CancelResult>(_profile.Method(JsonRpcMethods.CancelSuffix), new { id }, TimeSpan.FromSeconds(5), null, CancellationToken.None)
            .ContinueWith(t => _logger.LogDebug(t.Exception, "Cancel after timeout failed"), TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task<NdjsonPipeTransport> EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_transport is { IsConnected: true } ready) return ready;

        await _connectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_transport is { IsConnected: true } readyNow) return readyNow;

            return await ConnectCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _connectLock.Release();
        }
    }

    /// <summary>Caller holds <see cref="_connectLock"/>.</summary>
    private async Task<NdjsonPipeTransport> ConnectCoreAsync(CancellationToken cancellationToken)
    {
        if (_transport is not null)
        {
            await _transport.DisposeAsync().ConfigureAwait(false);
            _transport = null;
        }

        var transport = new NdjsonPipeTransport(_options.PipeName, _options.MaxMessageBytes, _logger);
        transport.MessageReceived += OnMessage;
        transport.Disconnected += OnDisconnected;

        try
        {
            await transport.ConnectAsync(_options.ConnectTimeoutMs, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is TimeoutException or IOException or UnauthorizedAccessException)
        {
            await transport.DisposeAsync().ConfigureAwait(false);
            throw new BridgeUnavailableException(
                $"{_profile.DisplayName} bridge not connected. "
                + (_profile.BridgeNotConnectedHint
                   ?? $"Open {_profile.DisplayName} {_options.HostVersion} and enable the HP MCP Bridge (pipe {_options.PipeName})."),
                exception);
        }

        _transport = transport;
        _sessionCts?.Cancel();
        _sessionCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _ = PingLoopAsync(_sessionCts.Token);
        _logger.LogInformation("Connected to {Host} bridge on {Pipe}", _profile.DisplayName, _options.PipeName);

        return transport;
    }

    private void OnMessage(JsonRpcEnvelope envelope)
    {
        switch (envelope.Kind)
        {
            case JsonRpcKind.Response when envelope.Id is { } id && _pending.TryGetValue(id, out var call):
                call.Completion.TrySetResult(envelope);
                break;

            case JsonRpcKind.Notification when JsonRpcMethods.IsProgress(envelope.Method):
                var progress = envelope.ParamsAs<ProgressParams>();
                if (progress is not null && _pending.TryGetValue(progress.Id, out var target)) target.Progress?.Report(progress);
                break;

            case JsonRpcKind.Notification when JsonRpcMethods.IsStatus(envelope.Method):
                LastStatus = envelope.ParamsAs<StatusParams>();
                _logger.LogInformation("Bridge status: {@Status}", LastStatus);
                break;

            case JsonRpcKind.Notification when JsonRpcMethods.IsLog(envelope.Method):
                var log = envelope.ParamsAs<LogParams>();
                if (log is not null) _logger.LogInformation("[bridge:{Level}] {Message}", log.Level, log.Message);
                break;

            default:
                _logger.LogDebug("Ignoring unexpected message {Method} (id {Id})", envelope.Method, envelope.Id);
                break;
        }
    }

    private void OnDisconnected(Exception? failure)
    {
        _sessionCts?.Cancel();

        var reason = new BridgeUnavailableException($"{_profile.DisplayName} bridge disconnected while the request was running.", failure);
        foreach (var call in _pending.Values) call.Completion.TrySetException(reason);

        if (!_lifetime.IsCancellationRequested) _ = ReconnectWithBackoffAsync();
    }

    private async Task ReconnectWithBackoffAsync()
    {
        for (var attempt = 1; attempt <= _options.MaxReconnectAttempts; attempt++)
        {
            var delay = TimeSpan.FromMilliseconds(Math.Min(8000, 500 * Math.Pow(2, attempt - 1)));

            try
            {
                await Task.Delay(delay, _lifetime.Token).ConfigureAwait(false);
                await EnsureConnectedAsync(_lifetime.Token).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (BridgeUnavailableException)
            {
                _logger.LogDebug("Reconnect attempt {Attempt}/{Max} to {Pipe} failed", attempt, _options.MaxReconnectAttempts, _options.PipeName);
            }
        }
    }

    /// <summary>Keeps an idle connection honest: a dead bridge is noticed by the ping, not by the next tool call.</summary>
    private async Task PingLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PingIntervalSeconds));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!_pending.IsEmpty || !IsConnected) continue;

                try
                {
                    await SendAsync<BridgePingResult>(_profile.Method(JsonRpcMethods.PingSuffix), null, TimeSpan.FromSeconds(5), null, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    _logger.LogDebug(exception, "Ping failed; transport will report the disconnect");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // session ended
        }
    }

    public async ValueTask DisposeAsync()
    {
        _lifetime.Cancel();
        _sessionCts?.Cancel();

        if (_transport is not null) await _transport.DisposeAsync().ConfigureAwait(false);

        _connectLock.Dispose();
        _lifetime.Dispose();
    }

    private sealed class PendingCall(IProgress<ProgressParams>? progress)
    {
        public IProgress<ProgressParams>? Progress { get; } = progress;

        public TaskCompletionSource<JsonRpcEnvelope> Completion { get; } =
            new TaskCompletionSource<JsonRpcEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
