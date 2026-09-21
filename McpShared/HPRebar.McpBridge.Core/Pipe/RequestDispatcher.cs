using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Serilog;

namespace HPRebar.McpBridge.Core.Pipe;

/// <summary>
///     Turns one incoming line into one outgoing line. Cheap methods (ping, cancel, inspect) answer on
///     the pipe thread; context and execute go through <see cref="IBridgeExecutor"/> and wait for the
///     host's API thread. Methods are matched on the part after the host prefix (`revit.execute` and
///     `autocad.execute` are the same request), and every reply notification reuses the caller's prefix
///     so a server only ever sees the names it sent. Every failure becomes a JSON-RPC error with a
///     message the AI can read — never a dropped request.
/// </summary>
public sealed class RequestDispatcher
{
    /// <summary>"Revit", "AutoCAD" — for messages that name the host.</summary>
    public string HostName { get; }

    /// <summary>Major version of the host application, e.g. "2026".</summary>
    public string HostVersion { get; }

    private readonly IBridgeExecutor _executor;
    private readonly BridgeSettings _settings;
    private readonly string? _executionDisabledMessage;
    private readonly Func<long, JsonRpcEnvelope, NdjsonPipeWriter, CancellationToken, Task<JsonRpcEnvelope?>>? _customHandler;

    /// <param name="hostVersion">Major version of the host application, e.g. "2026".</param>
    /// <param name="hostName">Display name used in messages, e.g. "Revit" or "AutoCAD".</param>
    /// <param name="executionDisabledMessage">
    ///     Replaces the opt-in refusal text, which otherwise tells the user to look for the bridge window "inside"
    ///     the host — true for an add-in, wrong for a bridge that is a separate program. Null keeps the text.
    /// </param>
    /// <param name="customHandler">
    ///     Optional handler for custom host-specific JSON-RPC methods not covered by standard engine suffixes.
    /// </param>
    public RequestDispatcher(
        IBridgeExecutor executor,
        BridgeSettings settings,
        string hostVersion,
        string hostName = "Revit",
        string? executionDisabledMessage = null,
        Func<long, JsonRpcEnvelope, NdjsonPipeWriter, CancellationToken, Task<JsonRpcEnvelope?>>? customHandler = null)
    {
        _executor = executor;
        _settings = settings;
        HostVersion = hostVersion;
        HostName = hostName;
        _executionDisabledMessage = executionDisabledMessage;
        _customHandler = customHandler;
    }

    public async Task HandleLineAsync(string line, NdjsonPipeWriter writer, CancellationToken cancellationToken)
    {
        JsonRpcEnvelope? envelope;
        try
        {
            envelope = BridgeJson.Deserialize<JsonRpcEnvelope>(line);
        }
        catch (JsonException exception)
        {
            Log.Warning(exception, "MCP bridge received a line that is not JSON-RPC");
            await writer.WriteAsync(JsonRpcEnvelope.Failure(0, BridgeErrorCode.ParseError, "Line is not valid JSON-RPC."), cancellationToken).ConfigureAwait(false);
            return;
        }

        if (envelope is null || envelope.Kind != JsonRpcKind.Request || envelope.Id is not { } id)
        {
            // The server only ever sends requests; anything else is noise, and a notification has no id to answer to.
            Log.Debug("MCP bridge ignored a non-request message: {Method}", envelope?.Method);
            return;
        }

        JsonRpcEnvelope reply;
        try
        {
            reply = await DispatchAsync(id, envelope, writer, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            reply = JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, "Request cancelled because the bridge is shutting down.");
        }
        catch (BridgeRequestException exception)
        {
            // The executor chose the code (busy past the grace period, no document): actionable for the AI, not a bridge fault.
            Log.Information("MCP bridge refused {Method}: {Code} {Message}", envelope.Method, exception.Code, exception.Message);
            reply = JsonRpcEnvelope.Failure(id, exception.Code, exception.Message);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "MCP bridge failed to handle {Method}", envelope.Method);
            reply = JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError,
                SafeText.StripPaths($"Bridge failed to handle {envelope.Method}: {exception.GetType().Name}: {exception.Message}"));
        }

        await writer.WriteAsync(reply, cancellationToken).ConfigureAwait(false);
    }

    private async Task<JsonRpcEnvelope> DispatchAsync(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken cancellationToken)
    {
        var method = request.Method ?? string.Empty;

        switch (JsonRpcMethods.Suffix(method))
        {
            case JsonRpcMethods.PingSuffix:
                return JsonRpcEnvelope.Success(id, new BridgePingResult(true, HostVersion, _settings.ExecutionEnabled, _executor.IsBusy));

            case JsonRpcMethods.CancelSuffix:
                return JsonRpcEnvelope.Success(id, _executor.Cancel());

            case JsonRpcMethods.InspectSuffix:
            {
                var parameters = request.ParamsAs<InspectRequest>();
                return parameters is null || string.IsNullOrWhiteSpace(parameters.TypeName)
                    ? JsonRpcEnvelope.Failure(id, BridgeErrorCode.InvalidRequest, $"{method} needs a typeName.")
                    : JsonRpcEnvelope.Success(id, _executor.Inspect(parameters));
            }

            case JsonRpcMethods.AnalyzeSuffix:
            {
                var parameters = request.ParamsAs<AnalyzeRequest>();
                return parameters is null || string.IsNullOrWhiteSpace(parameters.Code)
                    ? JsonRpcEnvelope.Failure(id, BridgeErrorCode.InvalidRequest, $"{method} needs a non-empty code.")
                    : JsonRpcEnvelope.Success(id, _executor.Analyze(parameters));
            }

            case JsonRpcMethods.ContextSuffix:
            {
                var parameters = request.ParamsAs<ContextRequest>() ?? new ContextRequest();
                var context = await _executor.GetContextAsync(parameters.IncludeSelection, cancellationToken).ConfigureAwait(false);
                return JsonRpcEnvelope.Success(id, context);
            }

            case JsonRpcMethods.ExecuteSuffix:
                return await ExecuteAsync(id, request, writer, cancellationToken).ConfigureAwait(false);

            default:
                if (_customHandler is not null)
                {
                    var customResponse = await _customHandler(id, request, writer, cancellationToken).ConfigureAwait(false);
                    if (customResponse is not null)
                        return customResponse;
                }

                return JsonRpcEnvelope.Failure(id, BridgeErrorCode.MethodNotFound, $"Method not found: {method}");
        }
    }

    private async Task<JsonRpcEnvelope> ExecuteAsync(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken cancellationToken)
    {
        var method = request.Method ?? string.Empty;

        if (!_settings.ExecutionEnabled)
            return JsonRpcEnvelope.Failure(id, BridgeErrorCode.ExecutionDisabled,
                _executionDisabledMessage ?? $"Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HP MCP Bridge window inside {HostName}.");

        if (_executor.IsBusy)
            return JsonRpcEnvelope.Failure(id, BridgeErrorCode.Busy, $"Another script is still running in {HostName}. Wait for it to finish or call cancel_execution.");

        var parameters = request.ParamsAs<ExecuteRequest>();
        if (parameters is null || string.IsNullOrWhiteSpace(parameters.Code))
            return JsonRpcEnvelope.Failure(id, BridgeErrorCode.InvalidRequest, $"{method} needs a non-empty code.");

        // Progress callbacks arrive from the host's API thread; Post returns as soon as the line is queued behind the
        // write lock, and the synchronous forwarder keeps the notifications in the order the script raised them.
        // The notification carries the caller's prefix so the server matches it against the names it knows.
        var progressMethod = ProgressMethodFor(method);
        var progress = new SynchronousProgress<ScriptProgress>(p =>
            writer.Post(JsonRpcEnvelope.Notification(progressMethod, new ProgressParams(id, p.Current, p.Total, p.Message))));

        var result = await _executor.ExecuteAsync(parameters, progress, cancellationToken).ConfigureAwait(false);

        return JsonRpcEnvelope.Success(id, result);
    }

    /// <summary>`autocad.execute` → `autocad.progress`; a prefix-less request keeps the historical Revit name.</summary>
    private static string ProgressMethodFor(string method)
    {
        var dot = method.IndexOf('.');

        return dot < 0 ? JsonRpcMethods.ProgressNotification : method.Substring(0, dot + 1) + JsonRpcMethods.ProgressSuffix;
    }
}
