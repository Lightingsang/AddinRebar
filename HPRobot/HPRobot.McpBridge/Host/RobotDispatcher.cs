using System;
using System.Threading;
using System.Threading.Tasks;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Serilog;

namespace HPRobot.McpBridge.Host;

public sealed record RobotAttachParams(int? Pid = null);

/// <summary>
///     Dispatches incoming JSON-RPC requests across both standard engine methods
///     and Robot-specific custom methods over the named pipe.
/// </summary>
public sealed class RobotDispatcher
{
    private readonly RobotBridgeExecutor _executor;
    private readonly BridgeSettings _settings;
    private readonly string _hostVersion;

    public RobotDispatcher(
        RobotBridgeExecutor executor,
        BridgeSettings settings,
        string hostVersion = "2026")
    {
        _executor = executor;
        _settings = settings;
        _hostVersion = hostVersion;
    }

    /// <summary>
    ///     Custom dispatcher hooked into McpBridgeHost. Returns null if method is handled by standard engine.
    /// </summary>
    public Task<JsonRpcEnvelope?> DispatchCustomAsync(
        long id,
        JsonRpcEnvelope request,
        NdjsonPipeWriter writer,
        CancellationToken ct)
    {
        var method = request.Method ?? string.Empty;
        var suffix = JsonRpcMethods.Suffix(method);

        return suffix switch
        {
            "attach" => HandleAttach(id),
            "detach" => HandleDetach(id),
            _ => Task.FromResult<JsonRpcEnvelope?>(null)
        };
    }

    private async Task<JsonRpcEnvelope?> HandleAttach(long id)
    {
        try
        {
            await _executor.StaWorker.RunOnControlLaneAsync("pipe-attach", () =>
            {
                _executor.Attachment.Attach();
            }).ConfigureAwait(false);

            var result = new
            {
                success = true,
                isAttached = _executor.Attachment.IsAttached,
                pid = _executor.Attachment.AttachedPid,
                version = _executor.Attachment.RobotVersion,
                activeModel = _executor.Attachment.ActiveModelFileName
            };

            return JsonRpcEnvelope.Success(id, result);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error handling robot.attach");
            return JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, ex.Message);
        }
    }

    private async Task<JsonRpcEnvelope?> HandleDetach(long id)
    {
        try
        {
            await _executor.StaWorker.RunOnControlLaneAsync("pipe-detach", () =>
            {
                _executor.Attachment.Detach("Client requested detach");
            }).ConfigureAwait(false);

            var result = new
            {
                success = true,
                isAttached = false
            };

            return JsonRpcEnvelope.Success(id, result);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error handling robot.detach");
            return JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, ex.Message);
        }
    }
}
