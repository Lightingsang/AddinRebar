using System;
using System.Threading;
using System.Threading.Tasks;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Serilog;

namespace HPExcel.McpBridge.Host;

public sealed record ExcelAttachParams(int? Pid = null);

/// <summary>
///     Dispatches incoming JSON-RPC requests across both standard engine methods
///     and Excel-specific methods over the named pipe.
/// </summary>
public sealed class ExcelDispatcher
{
    private readonly ExcelBridgeExecutor _executor;
    private readonly BridgeSettings _settings;
    private readonly string _hostVersion;

    public ExcelDispatcher(
        ExcelBridgeExecutor executor,
        BridgeSettings settings,
        string hostVersion = "2026")
    {
        _executor = executor;
        _settings = settings;
        _hostVersion = hostVersion;
    }

    /// <summary>
    ///     Custom dispatcher hooked into McpBridgeHost. Returns null if method is not custom.
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
            "attach" => HandleAttach(id, request),
            "detach" => HandleDetach(id),
            _ => Task.FromResult<JsonRpcEnvelope?>(null)
        };
    }

    private Task<JsonRpcEnvelope?> HandleAttach(long id, JsonRpcEnvelope request)
    {
        try
        {
            var parameters = request.ParamsAs<ExcelAttachParams>();
            _executor.Attachment.Attach(parameters?.Pid);
            var result = new
            {
                success = true,
                isAttached = _executor.Attachment.IsAttached,
                pid = _executor.Attachment.AttachedPid,
                version = _executor.Attachment.ExcelVersion,
                activeWorkbook = _executor.Attachment.ActiveWorkbookName
            };

            return Task.FromResult<JsonRpcEnvelope?>(JsonRpcEnvelope.Success(id, result));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error handling excel.attach");
            return Task.FromResult<JsonRpcEnvelope?>(JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, ex.Message));
        }
    }

    private Task<JsonRpcEnvelope?> HandleDetach(long id)
    {
        try
        {
            _executor.Attachment.Detach("Client requested detach");
            var result = new
            {
                success = true,
                isAttached = false
            };

            return Task.FromResult<JsonRpcEnvelope?>(JsonRpcEnvelope.Success(id, result));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error handling excel.detach");
            return Task.FromResult<JsonRpcEnvelope?>(JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, ex.Message));
        }
    }
}
