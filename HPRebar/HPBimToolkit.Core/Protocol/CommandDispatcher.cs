using System.Text.Json;
using HPBimToolkit.Core.Contracts;

namespace HPBimToolkit.Core.Protocol;

/// <summary>Runs one command. The token is cancelled when the caller gives up or the command's timeout elapses.</summary>
public delegate Task<object?> BridgeCommandHandler(JsonElement? payload, CancellationToken cancellationToken);

/// <summary>
///     Routes requests to the handlers modules registered and turns every outcome — result, refusal, timeout or crash —
///     into a response, so the page always gets an answer it can settle its call with.
/// </summary>
public sealed class CommandDispatcher
{
    private readonly Dictionary<string, (BridgeCommandHandler Handler, TimeSpan Timeout)> _handlers = new(StringComparer.Ordinal);
    private readonly TimeSpan _defaultTimeout;
    private readonly Func<Exception, BridgeError?> _translateHostException;

    /// <param name="defaultTimeout">How long a request may take, queueing for Revit included, unless its command says otherwise.</param>
    /// <param name="translateHostException">Maps host exceptions Core cannot name (Revit API) to an error, or null to fall back to INTERNAL.</param>
    public CommandDispatcher(TimeSpan defaultTimeout, Func<Exception, BridgeError?>? translateHostException = null)
    {
        _defaultTimeout = RequirePositive(defaultTimeout, nameof(defaultTimeout));
        _translateHostException = translateHostException ?? (_ => null);
    }

    public IReadOnlyCollection<string> Commands => _handlers.Keys;

    /// <param name="timeout">Overrides the default timeout for a command known to run long (statistics on a large model).</param>
    public void Register(string command, BridgeCommandHandler handler, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout is { } value ? RequirePositive(value, nameof(timeout)) : _defaultTimeout;
        if (!_handlers.TryAdd(command, (handler, effectiveTimeout)))
        {
            throw new InvalidOperationException($"Command '{command}' is already registered.");
        }
    }

    /// <summary>Registers a handler whose payload is read as <typeparamref name="TRequest" /> (a missing payload reads as <c>{}</c>).</summary>
    public void Register<TRequest>(string command, Func<TRequest, CancellationToken, Task<object?>> handler, TimeSpan? timeout = null) =>
        Register(command, (payload, token) => handler(PayloadReader.Read<TRequest>(payload), token), timeout);

    public async Task<ResponseEnvelope> DispatchAsync(RequestEnvelope request, CancellationToken cancellationToken = default)
    {
        if (!_handlers.TryGetValue(request.Command, out var registration))
        {
            return ResponseEnvelope.Failure(request.Id,
                new BridgeError(BridgeErrorCodes.UnknownCommand, $"Unknown command '{request.Command}'."));
        }

        // One source for both the wait and the handler's token: when the wait gives up, the token is already cancelled,
        // so a request still queued for Revit is skipped instead of running after the page was told TIMEOUT.
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(registration.Timeout);
        try
        {
            var payload = await registration.Handler(request.Payload, timeoutSource.Token).WaitAsync(timeoutSource.Token);
            return ResponseEnvelope.Success(request.Id, payload);
        }
        catch (Exception exception)
        {
            return ResponseEnvelope.Failure(request.Id, Translate(exception, cancellationToken, registration.Timeout));
        }
    }

    private BridgeError Translate(Exception exception, CancellationToken callerToken, TimeSpan timeout) => exception switch
    {
        BridgeException bridge => new BridgeError(bridge.Code, bridge.Message),
        OperationCanceledException when callerToken.IsCancellationRequested =>
            new BridgeError(BridgeErrorCodes.Cancelled, "The request was cancelled."),
        OperationCanceledException or TimeoutException => new BridgeError(BridgeErrorCodes.Timeout,
            $"Revit did not answer within {timeout.TotalSeconds:0} s. Close any open Revit dialog and try again."),
        _ => _translateHostException(exception) ?? new BridgeError(BridgeErrorCodes.Internal, exception.Message),
    };

    private static TimeSpan RequirePositive(TimeSpan value, string name) =>
        value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(name, value, "Timeout must be positive.");
}
