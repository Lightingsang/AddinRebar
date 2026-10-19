namespace HPGrasshopper.McpBridge.Host;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPGrasshopper.McpBridge.Coordinator;
using HPGrasshopper.McpBridge.Tools;
using HPGrasshopper.McpBridge.UI;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Serilog;

/// <summary>
///     Dispatches custom Grasshopper JSON-RPC requests from the named pipe listener
///     to native tool handlers running safely on the Revit UI thread.
/// </summary>
public sealed class GrasshopperRpcDispatcher
{
    private readonly GrasshopperBridgeCoordinator _coordinator;
    private readonly BridgeSettings? _settings;
    private readonly Action _showWindow;

    public GrasshopperRpcDispatcher(
        GrasshopperBridgeCoordinator? coordinator = null,
        BridgeSettings? settings = null,
        CanvasReader? canvasReader = null,
        PlayerRunner? playerRunner = null,
        CanvasEditor? canvasEditor = null,
        ImageExporter? imageExporter = null,
        Action? showWindow = null)
    {
        _coordinator = coordinator ?? GrasshopperBridgeCoordinator.Instance;
        _settings = settings;
        _showWindow = showWindow ?? OpenStatusWindow;
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void OpenStatusWindow() => ShowStatusDialogCommand.ShowWindow();

    private BridgeSettings EffectiveSettings => _settings ?? _coordinator.Settings ?? new BridgeSettings();

    public async Task<JsonRpcEnvelope?> DispatchAsync(
        long id,
        JsonRpcEnvelope envelope,
        NdjsonPipeWriter? writer,
        CancellationToken cancellationToken)
    {
        var method = envelope.Method;
        if (string.IsNullOrEmpty(method)) return null;

        if (method.Equals("grasshopper.show_window", StringComparison.OrdinalIgnoreCase) ||
            method.Equals("revit.show_window", StringComparison.OrdinalIgnoreCase))
        {
            _showWindow();
            return JsonRpcEnvelope.Success(id, new { shown = true });
        }

        if (!TryParseNativeMethod(method, out var suffix, out var isWrite))
            return null;

        if (isWrite && !EffectiveSettings.ExecutionEnabled)
        {
            return JsonRpcEnvelope.Failure(
                id,
                BridgeErrorCode.ExecutionDisabled,
                "Execution is disabled in bridge settings. Please enable 'Allow AI Grasshopper execution' in the Grasshopper MCP status window.");
        }

        if (suffix.Equals("run_grasshopper_player", StringComparison.OrdinalIgnoreCase))
        {
            var p = envelope.ParamsAs<RunPlayerParams>();
            if (string.IsNullOrWhiteSpace(p?.FilePath))
            {
                return JsonRpcEnvelope.Failure(
                    id,
                    BridgeErrorCode.InvalidRequest,
                    "filePath is required for run_grasshopper_player.");
            }
        }

        return await ExecuteAndMapAsync(id, suffix, envelope, writer, isWrite, cancellationToken).ConfigureAwait(false);
    }

    private async Task<JsonRpcEnvelope> ExecuteAndMapAsync(
        long id,
        string suffix,
        JsonRpcEnvelope envelope,
        NdjsonPipeWriter? writer,
        bool isWrite,
        CancellationToken cancellationToken)
    {
        try
        {
            var executeRequest = CreateExecuteRequest(suffix, envelope, writer?.SessionId, isWrite);
            var execResult = await _coordinator.ExecuteAsync(id, executeRequest, null, cancellationToken).ConfigureAwait(false);

            if (execResult.IsError)
            {
                if (string.Equals(execResult.Message, "Another Grasshopper script or task is still running.", StringComparison.Ordinal))
                    return JsonRpcEnvelope.Failure(id, BridgeErrorCode.Busy, execResult.Message!);

                if (execResult.Message?.Contains("Execution is disabled in bridge settings") == true)
                    return JsonRpcEnvelope.Failure(id, BridgeErrorCode.ExecutionDisabled, execResult.Message!);

                if (execResult.Value != null)
                    return JsonRpcEnvelope.Success(id, execResult.Value);

                return JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, execResult.Message ?? "Execution failed");
            }

            return JsonRpcEnvelope.Success(id, execResult.Value ?? BridgeJson.ToElement(execResult));
        }
        catch (OperationCanceledException)
        {
            return JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, "Operation was cancelled.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GrasshopperRpcDispatcher: Error executing {Suffix}", suffix);
            return JsonRpcEnvelope.Failure(
                id,
                BridgeErrorCode.InternalError,
                SafeText.StripPaths($"{ex.GetType().Name}: {ex.Message}"));
        }
    }

    private static bool TryParseNativeMethod(string method, out string suffix, out bool isWrite)
    {
        suffix = JsonRpcMethods.Suffix(method);
        isWrite = false;

        var isGrasshopper = method.StartsWith(JsonRpcMethods.GrasshopperPrefix, StringComparison.OrdinalIgnoreCase) ||
                            method.IndexOf('.') < 0;
        if (!isGrasshopper) return false;

        isWrite = suffix.Equals("run_grasshopper_player", StringComparison.OrdinalIgnoreCase) ||
                  suffix.Equals("batch_edit_grasshopper_canvas", StringComparison.OrdinalIgnoreCase);

        return isWrite ||
               suffix.Equals("read_grasshopper_canvas", StringComparison.OrdinalIgnoreCase) ||
               suffix.Equals("export_grasshopper_canvas_image", StringComparison.OrdinalIgnoreCase);
    }

    private static ExecuteRequest CreateExecuteRequest(
        string suffix,
        JsonRpcEnvelope envelope,
        string? sessionId,
        bool isWrite)
    {
        var filePath = string.Empty;
        var timeoutSeconds = 60;

        if (suffix.Equals("run_grasshopper_player", StringComparison.OrdinalIgnoreCase))
        {
            if (envelope.Params.HasValue && envelope.Params.Value.TryGetProperty("filePath", out var fp))
                filePath = fp.GetString() ?? string.Empty;
            timeoutSeconds = 120;
        }
        else if (suffix.Equals("read_grasshopper_canvas", StringComparison.OrdinalIgnoreCase) ||
                 suffix.Equals("export_grasshopper_canvas_image", StringComparison.OrdinalIgnoreCase))
        {
            timeoutSeconds = 30;
        }

        var dryRun = false;
        if (envelope.Params.HasValue && envelope.Params.Value.TryGetProperty("dryRun", out var dr) && dr.ValueKind == JsonValueKind.True)
            dryRun = true;

        var transaction = isWrite ? TransactionModes.Auto : TransactionModes.None;

        return new ExecuteRequest(filePath, transaction, dryRun, timeoutSeconds)
        {
            Label = suffix,
            Args = envelope.Params,
            SessionId = sessionId
        };
    }

    private sealed record RunPlayerParams(string? FilePath = null);
}
