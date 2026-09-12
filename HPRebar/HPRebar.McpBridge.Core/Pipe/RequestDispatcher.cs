using System.Text.Json;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using Serilog;

namespace HPRebar.McpBridge.Core.Pipe;

/// <summary>
///     Turns one incoming line into one outgoing line. Cheap methods (ping, cancel, inspect) answer on
///     the pipe thread; context and execute go through <see cref="IRevitExecutor"/> and wait for Revit.
///     Every failure becomes a JSON-RPC error with a message the AI can read — never a dropped request.
/// </summary>
public sealed class RequestDispatcher
{
    private readonly IRevitExecutor _executor;
    private readonly BridgeSettings _settings;
    private readonly string _revitVersion;

    public RequestDispatcher(IRevitExecutor executor, BridgeSettings settings, string revitVersion)
    {
        _executor = executor;
        _settings = settings;
        _revitVersion = revitVersion;
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
        switch (request.Method)
        {
            case JsonRpcMethods.Ping:
                return JsonRpcEnvelope.Success(id, new BridgePingResult(true, _revitVersion, _settings.ExecutionEnabled, _executor.IsBusy));

            case JsonRpcMethods.Cancel:
                return JsonRpcEnvelope.Success(id, _executor.Cancel());

            case JsonRpcMethods.Inspect:
            {
                var parameters = request.ParamsAs<InspectRequest>();
                return parameters is null || string.IsNullOrWhiteSpace(parameters.TypeName)
                    ? JsonRpcEnvelope.Failure(id, BridgeErrorCode.InvalidRequest, "revit.inspect needs a typeName.")
                    : JsonRpcEnvelope.Success(id, _executor.Inspect(parameters));
            }

            case JsonRpcMethods.Analyze:
            {
                var parameters = request.ParamsAs<AnalyzeRequest>();
                return parameters is null || string.IsNullOrWhiteSpace(parameters.Code)
                    ? JsonRpcEnvelope.Failure(id, BridgeErrorCode.InvalidRequest, "revit.analyze needs a non-empty code.")
                    : JsonRpcEnvelope.Success(id, _executor.Analyze(parameters));
            }

            case JsonRpcMethods.Context:
            {
                var parameters = request.ParamsAs<ContextRequest>() ?? new ContextRequest();
                var context = await _executor.GetContextAsync(parameters.IncludeSelection, cancellationToken).ConfigureAwait(false);
                return JsonRpcEnvelope.Success(id, context);
            }

            case JsonRpcMethods.Execute:
                return await ExecuteAsync(id, request, writer, cancellationToken).ConfigureAwait(false);

            default:
                return JsonRpcEnvelope.Failure(id, BridgeErrorCode.MethodNotFound, $"Method not found: {request.Method}");
        }
    }

    private async Task<JsonRpcEnvelope> ExecuteAsync(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken cancellationToken)
    {
        if (!_settings.ExecutionEnabled)
            return JsonRpcEnvelope.Failure(id, BridgeErrorCode.ExecutionDisabled,
                "Code execution is disabled. Ask the user to tick 'Allow AI code execution' in the HPRebar MCP Bridge window inside Revit.");

        if (_executor.IsBusy)
            return JsonRpcEnvelope.Failure(id, BridgeErrorCode.Busy, "Another script is still running in Revit. Wait for it to finish or call cancel_execution.");

        var parameters = request.ParamsAs<ExecuteRequest>();
        if (parameters is null || string.IsNullOrWhiteSpace(parameters.Code))
            return JsonRpcEnvelope.Failure(id, BridgeErrorCode.InvalidRequest, "revit.execute needs a non-empty code.");

        // Progress callbacks arrive from the Revit thread; Post returns as soon as the line is queued behind the
        // write lock, and the synchronous forwarder keeps the notifications in the order the script raised them.
        var progress = new SynchronousProgress<ScriptProgress>(p =>
            writer.Post(JsonRpcEnvelope.Notification(JsonRpcMethods.ProgressNotification, new ProgressParams(id, p.Current, p.Total, p.Message))));

        var result = await _executor.ExecuteAsync(parameters, progress, cancellationToken).ConfigureAwait(false);

        return JsonRpcEnvelope.Success(id, result);
    }
}
