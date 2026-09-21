using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HPPowerBi.McpBridge.Cloud;
using HPPowerBi.McpBridge.Safety;
using HPPowerBi.McpBridge.Tabular;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;
using HPRebar.McpBridge.Core.Model;
using HPRebar.McpBridge.Core.Pipe;
using Serilog;
using BridgeErrorException = HPRebar.McpBridge.Core.Pipe.BridgeRequestException;

namespace HPPowerBi.McpBridge.Host;

#region Request Param DTOs

public sealed record DaxRequestParams(string? Query, int TopN = 100, string Format = "markdown");
public sealed record DaxFormatParams(string? Dax);
public sealed record SchemaRequestParams(string? TableName = null, bool IncludeColumns = true, bool IncludeMeasures = true, bool IncludeRelationships = true);
public sealed record MeasureUpsertParams(string? TableName, string? MeasureName, string? Expression, string? FormatString = null, string? DisplayFolder = null, string? Description = null);
public sealed record MeasureDeleteParams(string? TableName, string? MeasureName);
public sealed record RelationshipManageParams(string? Action, string? FromTable, string? FromColumn, string? ToTable, string? ToColumn, bool IsActive = true, string CrossFilteringBehavior = "OneDirection");
public sealed record CloudDatasetsParams(string? WorkspaceId = null);
public sealed record CloudRefreshParams(string? DatasetId, string? WorkspaceId = null, string? NotifyOption = "NoNotification");
public sealed record CloudDaxParams(string? DatasetId, string? Query, string? WorkspaceId = null);

#endregion

/// <summary>
///     Dispatches incoming JSON-RPC requests across both standard engine methods (ping, cancel, context, execute)
///     and high-level Power BI methods (dax, schema, measure, relationship, cloud).
/// </summary>
public sealed class PowerBiDispatcher
{
    private readonly PowerBiBridgeExecutor _executor;
    private readonly PowerBiCloudClient _cloudClient;
    private readonly RequestDispatcher _baseDispatcher;
    private readonly BridgeSettings _settings;

    public PowerBiDispatcher(
        PowerBiBridgeExecutor executor,
        PowerBiCloudClient cloudClient,
        BridgeSettings settings,
        string hostVersion = "2026")
    {
        _executor = executor;
        _cloudClient = cloudClient;
        _settings = settings;

        _baseDispatcher = new RequestDispatcher(
            executor,
            settings,
            hostVersion,
            hostName: PowerBiBridgeExecutor.HostName,
            executionDisabledMessage: PbiSafetyGuard.ExecutionDisabledMessage,
            customHandler: DispatchCustomAsync);
    }

    public RequestDispatcher BaseDispatcher => _baseDispatcher;

    public async Task HandleLineAsync(string line, NdjsonPipeWriter writer, CancellationToken cancellationToken)
    {
        JsonRpcEnvelope? envelope;
        try
        {
            envelope = BridgeJson.Deserialize<JsonRpcEnvelope>(line);
        }
        catch (JsonException ex)
        {
            Log.Warning(ex, "Power BI bridge received invalid JSON-RPC");
            await writer.WriteAsync(JsonRpcEnvelope.Failure(0, BridgeErrorCode.ParseError, "Line is not valid JSON-RPC."), cancellationToken).ConfigureAwait(false);
            return;
        }

        if (envelope is null || envelope.Kind != JsonRpcKind.Request || envelope.Id is not { } id)
        {
            Log.Debug("Power BI bridge ignored non-request line");
            return;
        }

        var customResponse = await DispatchCustomAsync(id, envelope, writer, cancellationToken).ConfigureAwait(false);
        if (customResponse is not null)
        {
            await writer.WriteAsync(customResponse, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Standard engine methods (ping, context, execute, cancel, inspect, analyze)
        await _baseDispatcher.HandleLineAsync(line, writer, cancellationToken).ConfigureAwait(false);
    }

    public async Task<JsonRpcEnvelope?> DispatchCustomAsync(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken ct)
    {
        var method = request.Method ?? string.Empty;
        var suffix = JsonRpcMethods.Suffix(method);

        return suffix switch
        {
            "dax" or "evaluate_dax" => await HandleDaxAsync(id, request, writer, ct).ConfigureAwait(false),
            "dax.format" or "format_dax" => await HandleFormatDaxAsync(id, request, writer, ct).ConfigureAwait(false),
            "schema" or "get_schema" => await HandleSchemaAsync(id, request, writer, ct).ConfigureAwait(false),
            "measure.upsert" => await HandleMeasureUpsertAsync(id, request, writer, ct).ConfigureAwait(false),
            "measure.delete" => await HandleMeasureDeleteAsync(id, request, writer, ct).ConfigureAwait(false),
            "relationship.manage" => await HandleRelationshipManageAsync(id, request, writer, ct).ConfigureAwait(false),
            "cloud.workspaces" => await HandleCloudWorkspacesAsync(id, request, writer, ct).ConfigureAwait(false),
            "cloud.datasets" => await HandleCloudDatasetsAsync(id, request, writer, ct).ConfigureAwait(false),
            "cloud.refresh" => await HandleCloudRefreshAsync(id, request, writer, ct).ConfigureAwait(false),
            "cloud.dax" => await HandleCloudDaxAsync(id, request, writer, ct).ConfigureAwait(false),
            _ => null,
        };
    }

    private async Task<JsonRpcEnvelope> HandleDaxAsync(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken ct)
    {
        try
        {
            _executor.Guard.EnsureExecutionAllowed();

            var parameters = request.ParamsAs<DaxRequestParams>() ?? new DaxRequestParams(string.Empty);
            if (!PbiSafetyGuard.ValidateDaxQuery(parameters.Query, out var validationError))
            {
                return JsonRpcEnvelope.Failure(id, BridgeErrorCode.InvalidRequest, validationError!);
            }

            if (!string.IsNullOrEmpty(parameters.Format) &&
                !string.Equals(parameters.Format, "markdown", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(parameters.Format, "json", StringComparison.OrdinalIgnoreCase))
            {
                throw new BridgeErrorException(BridgeErrorCode.InvalidRequest, $"Unsupported format '{parameters.Format}'. Supported formats: 'markdown', 'json'.");
            }

            if (!_executor.Connection.IsConnected || _executor.Connection.Adomd == null)
            {
                return JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, "Not connected to Power BI Desktop.");
            }

            var result = await PbiDaxExecutor.ExecuteDaxAsync(
                _executor.Connection.Adomd,
                parameters.Query!,
                parameters.TopN,
                ct).ConfigureAwait(false);

            object output = string.Equals(parameters.Format, "json", StringComparison.OrdinalIgnoreCase)
                ? result
                : PbiDaxExecutor.FormatAsMarkdown(result);

            return JsonRpcEnvelope.Success(id, output);
        }
        catch (BridgeRequestException ex)
        {
            return JsonRpcEnvelope.Failure(id, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            return JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, ex.Message);
        }
    }

    private Task<JsonRpcEnvelope> HandleFormatDaxAsync(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken ct)
    {
        var parameters = request.ParamsAs<DaxFormatParams>();
        var dax = parameters?.Dax ?? string.Empty;

        // Clean and format DAX with keyword capitalization and standard line breaks
        var formatted = FormatDaxString(dax);
        return Task.FromResult(JsonRpcEnvelope.Success(id, formatted));
    }

    private Task<JsonRpcEnvelope> HandleSchemaAsync(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken ct)
    {
        try
        {
            _executor.Guard.EnsureExecutionAllowed();

            if (!_executor.Connection.IsConnected || _executor.Connection.Model == null)
            {
                return Task.FromResult(JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, "Not connected to Power BI Desktop."));
            }

            var parameters = request.ParamsAs<SchemaRequestParams>() ?? new SchemaRequestParams();
            var schema = PbiSchemaReader.ExtractSchema(
                _executor.Connection.Model,
                parameters.TableName,
                parameters.IncludeColumns,
                parameters.IncludeMeasures,
                parameters.IncludeRelationships);

            return Task.FromResult(JsonRpcEnvelope.Success(id, schema));
        }
        catch (BridgeRequestException ex)
        {
            return Task.FromResult(JsonRpcEnvelope.Failure(id, ex.Code, ex.Message));
        }
        catch (Exception ex)
        {
            return Task.FromResult(JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, ex.Message));
        }
    }

    private Task<JsonRpcEnvelope> HandleMeasureUpsertAsync(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken ct)
    {
        try
        {
            _executor.Guard.EnsureMutationAllowed();

            if (!_executor.Connection.IsConnected || _executor.Connection.Model == null)
            {
                return Task.FromResult(JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, "Not connected to Power BI Desktop."));
            }

            var p = request.ParamsAs<MeasureUpsertParams>();
            if (p == null || string.IsNullOrWhiteSpace(p.TableName) || string.IsNullOrWhiteSpace(p.MeasureName) || string.IsNullOrWhiteSpace(p.Expression))
            {
                return Task.FromResult(JsonRpcEnvelope.Failure(id, BridgeErrorCode.InvalidRequest, "TableName, MeasureName, and Expression are required."));
            }

            // Layer 3: Capture snapshot before mutation
            string? snapshot = null;
            if (_executor.Connection.Database != null)
            {
                snapshot = _executor.Snapshots.CreateSnapshot(_executor.Connection.Database, $"upsert_{p.MeasureName}");
            }

            var result = PbiMeasureService.CreateOrUpdateMeasure(
                _executor.Connection.Model,
                p.TableName,
                p.MeasureName,
                p.Expression,
                p.FormatString,
                p.DisplayFolder,
                p.Description,
                snapshotName: snapshot);

            return Task.FromResult(JsonRpcEnvelope.Success(id, result));
        }
        catch (BridgeRequestException ex)
        {
            return Task.FromResult(JsonRpcEnvelope.Failure(id, ex.Code, ex.Message));
        }
        catch (Exception ex)
        {
            return Task.FromResult(JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, ex.Message));
        }
    }

    private Task<JsonRpcEnvelope> HandleMeasureDeleteAsync(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken ct)
    {
        try
        {
            _executor.Guard.EnsureMutationAllowed();

            if (!_executor.Connection.IsConnected || _executor.Connection.Model == null)
            {
                return Task.FromResult(JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, "Not connected to Power BI Desktop."));
            }

            var p = request.ParamsAs<MeasureDeleteParams>();
            if (p == null || string.IsNullOrWhiteSpace(p.TableName) || string.IsNullOrWhiteSpace(p.MeasureName))
            {
                return Task.FromResult(JsonRpcEnvelope.Failure(id, BridgeErrorCode.InvalidRequest, "TableName and MeasureName are required."));
            }

            string? snapshot = null;
            if (_executor.Connection.Database != null)
            {
                snapshot = _executor.Snapshots.CreateSnapshot(_executor.Connection.Database, $"delete_{p.MeasureName}");
            }

            var deleted = PbiMeasureService.DeleteMeasure(_executor.Connection.Model, p.TableName, p.MeasureName, snapshot);
            if (!deleted)
            {
                return Task.FromResult(JsonRpcEnvelope.Failure(id, BridgeErrorCode.InvalidRequest, $"Measure '{p.MeasureName}' was not found in table '{p.TableName}'."));
            }

            return Task.FromResult(JsonRpcEnvelope.Success(id, new { Deleted = true, TableName = p.TableName, MeasureName = p.MeasureName, Snapshot = snapshot }));
        }
        catch (BridgeRequestException ex)
        {
            return Task.FromResult(JsonRpcEnvelope.Failure(id, ex.Code, ex.Message));
        }
        catch (Exception ex)
        {
            return Task.FromResult(JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, ex.Message));
        }
    }

    private Task<JsonRpcEnvelope> HandleRelationshipManageAsync(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken ct)
    {
        try
        {
            _executor.Guard.EnsureMutationAllowed();

            if (!_executor.Connection.IsConnected || _executor.Connection.Model == null)
            {
                return Task.FromResult(JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, "Not connected to Power BI Desktop."));
            }

            var p = request.ParamsAs<RelationshipManageParams>();
            if (p == null || string.IsNullOrWhiteSpace(p.Action) || string.IsNullOrWhiteSpace(p.FromTable) ||
                string.IsNullOrWhiteSpace(p.FromColumn) || string.IsNullOrWhiteSpace(p.ToTable) || string.IsNullOrWhiteSpace(p.ToColumn))
            {
                return Task.FromResult(JsonRpcEnvelope.Failure(id, BridgeErrorCode.InvalidRequest, "Action, FromTable, FromColumn, ToTable, and ToColumn are required."));
            }

            string? snapshot = null;
            if (_executor.Connection.Database != null)
            {
                snapshot = _executor.Snapshots.CreateSnapshot(_executor.Connection.Database, $"relationship_{p.Action}");
            }

            var result = PbiRelationshipService.ManageRelationship(
                _executor.Connection.Model,
                p.Action,
                p.FromTable,
                p.FromColumn,
                p.ToTable,
                p.ToColumn,
                p.IsActive,
                p.CrossFilteringBehavior,
                snapshot);

            return Task.FromResult(JsonRpcEnvelope.Success(id, result));
        }
        catch (BridgeRequestException ex)
        {
            return Task.FromResult(JsonRpcEnvelope.Failure(id, ex.Code, ex.Message));
        }
        catch (Exception ex)
        {
            return Task.FromResult(JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, ex.Message));
        }
    }

    private async Task<JsonRpcEnvelope> HandleCloudWorkspacesAsync(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken ct)
    {
        try
        {
            var workspaces = await _cloudClient.ListWorkspacesAsync(100, ct).ConfigureAwait(false);
            return JsonRpcEnvelope.Success(id, workspaces);
        }
        catch (Exception ex)
        {
            return JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, ex.Message);
        }
    }

    private async Task<JsonRpcEnvelope> HandleCloudDatasetsAsync(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken ct)
    {
        try
        {
            var p = request.ParamsAs<CloudDatasetsParams>();
            var datasets = await _cloudClient.ListDatasetsAsync(p?.WorkspaceId, ct).ConfigureAwait(false);
            return JsonRpcEnvelope.Success(id, datasets);
        }
        catch (Exception ex)
        {
            return JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, ex.Message);
        }
    }

    private async Task<JsonRpcEnvelope> HandleCloudRefreshAsync(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken ct)
    {
        try
        {
            var p = request.ParamsAs<CloudRefreshParams>();
            if (p == null || string.IsNullOrWhiteSpace(p.DatasetId))
            {
                return JsonRpcEnvelope.Failure(id, BridgeErrorCode.InvalidRequest, "DatasetId is required.");
            }

            var result = await _cloudClient.TriggerRefreshAsync(p.DatasetId, p.WorkspaceId, p.NotifyOption ?? "NoNotification", ct).ConfigureAwait(false);
            return JsonRpcEnvelope.Success(id, result);
        }
        catch (Exception ex)
        {
            return JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, ex.Message);
        }
    }

    private async Task<JsonRpcEnvelope> HandleCloudDaxAsync(long id, JsonRpcEnvelope request, NdjsonPipeWriter writer, CancellationToken ct)
    {
        try
        {
            var p = request.ParamsAs<CloudDaxParams>();
            if (p == null || string.IsNullOrWhiteSpace(p.DatasetId) || string.IsNullOrWhiteSpace(p.Query))
            {
                return JsonRpcEnvelope.Failure(id, BridgeErrorCode.InvalidRequest, "DatasetId and Query are required.");
            }

            var result = await _cloudClient.ExecuteDaxAsync(p.DatasetId, p.Query, p.WorkspaceId, ct).ConfigureAwait(false);
            return JsonRpcEnvelope.Success(id, result);
        }
        catch (Exception ex)
        {
            return JsonRpcEnvelope.Failure(id, BridgeErrorCode.InternalError, ex.Message);
        }
    }

    private static string FormatDaxString(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var text = raw.Trim();
        // Capitalize primary DAX keywords
        var keywords = new[] { "EVALUATE", "DEFINE", "MEASURE", "VAR", "RETURN", "CALCULATE", "SUM", "AVERAGE", "COUNTROWS", "FILTER", "ALL", "RELATED", "DIVIDE" };
        foreach (var kw in keywords)
        {
            text = Regex.Replace(text, $@"\b{kw}\b", kw, RegexOptions.IgnoreCase);
        }

        return text;
    }
}
