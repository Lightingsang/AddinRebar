namespace HPBim.Client.Recovery;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPBim.Client.Connection;
using HPRebar.Mcp.Contracts;
using HPRebar.Mcp.Contracts.JsonRpc;
using HPRebar.Mcp.Contracts.Messages;

/// <summary>
/// Service reading coordination receipts from Revit Extensible Storage over Named Pipe.
/// </summary>
public sealed class ExtensibleStorageReceiptReader : IExtensibleStorageReceiptReader
{
    private readonly IRevitPipeClient _pipeClient;

    public ExtensibleStorageReceiptReader(IRevitPipeClient pipeClient)
    {
        _pipeClient = pipeClient ?? throw new ArgumentNullException(nameof(pipeClient));
    }

    public async Task<CoordinationReceiptRecord?> FindReceiptByOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken = default)
    {
        var receipts = await QueryReceiptsAsync(
            targetOperationId: operationId.ToString("D"),
            targetPlanHash: null,
            cancellationToken).ConfigureAwait(false);

        return receipts.FirstOrDefault();
    }

    public async Task<CoordinationReceiptRecord?> FindReceiptByPlanHashAsync(
        string planHash,
        CancellationToken cancellationToken = default)
    {
        var receipts = await QueryReceiptsAsync(
            targetOperationId: null,
            targetPlanHash: planHash,
            cancellationToken).ConfigureAwait(false);

        return receipts
            .OrderByDescending(r => r.CommittedAtUtc)
            .FirstOrDefault();
    }

    public async Task<CoordinationReceiptRecord?> FindReceiptAsync(
        Guid operationId,
        string? planHash = null,
        CancellationToken cancellationToken = default)
    {
        var receipt = await FindReceiptByOperationIdAsync(operationId, cancellationToken).ConfigureAwait(false);
        if (receipt != null) return receipt;

        if (!string.IsNullOrEmpty(planHash))
        {
            return await FindReceiptByPlanHashAsync(planHash!, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    public async Task<IReadOnlyList<CoordinationReceiptRecord>> GetAllReceiptsAsync(
        CancellationToken cancellationToken = default)
    {
        return await QueryReceiptsAsync(null, null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<CoordinationReceiptRecord>> QueryReceiptsAsync(
        string? targetOperationId,
        string? targetPlanHash,
        CancellationToken cancellationToken)
    {
        if (!_pipeClient.IsConnected)
        {
            await _pipeClient.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        var argsDict = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(targetOperationId)) argsDict["targetOperationId"] = targetOperationId!;
        if (!string.IsNullOrEmpty(targetPlanHash)) argsDict["targetPlanHash"] = targetPlanHash!;

        var request = new ExecuteRequest(
            Code: ExtensibleStorageQueryScriptTemplate.GetQueryScript(),
            Transaction: TransactionModes.None,
            DryRun: false,
            TimeoutSeconds: 30,
            Label: "Query Extensible Storage Receipts",
            Args: BridgeJson.ToElement(argsDict));

        var execRes = await _pipeClient.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        if (execRes.IsError)
        {
            throw new RevitPipeException($"Failed to query extensible storage receipts: {execRes.Message}");
        }

        return ParseReceipts(execRes.Value);
    }

    public static IReadOnlyList<CoordinationReceiptRecord> ParseReceipts(JsonElement? root)
    {
        if (root == null || root.Value.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<CoordinationReceiptRecord>();
        }

        try
        {
            var elem = root.Value;
            if (!elem.TryGetProperty("receipts", out var receiptsProp) || receiptsProp.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<CoordinationReceiptRecord>();
            }

            var results = new List<CoordinationReceiptRecord>();
            foreach (var item in receiptsProp.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                results.Add(ParseSingleReceipt(item));
            }

            return results;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException)
        {
            // Damaged JSON payload from Revit Extensible Storage query response
            return Array.Empty<CoordinationReceiptRecord>();
        }
    }

    private static string? TryGetStringProperty(JsonElement item, string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var prop))
        {
            return null;
        }

        if (prop.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        try
        {
            return prop.GetString();
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            // Incomplete UTF-16 surrogates or unescapable string tokens in property value
            return null;
        }
    }

    private static CoordinationReceiptRecord ParseSingleReceipt(JsonElement item)
    {
        string opIdStr = TryGetStringProperty(item, "operationId") ?? string.Empty;
        string planHash = TryGetStringProperty(item, "planHash") ?? string.Empty;
        string targetDocId = TryGetStringProperty(item, "targetDocId") ?? string.Empty;
        string targetDocTitle = TryGetStringProperty(item, "targetDocTitle") ?? targetDocId;
        string targetDocPath = TryGetStringProperty(item, "targetDocPath") ?? string.Empty;
        string targetVersionGuid = TryGetStringProperty(item, "targetVersionGuid") ?? string.Empty;
        string targetCentralGuid = TryGetStringProperty(item, "targetCentralGuid") ?? string.Empty;
        string committedAt = TryGetStringProperty(item, "committedAtUtc") ?? string.Empty;
        string? elemMapJson = TryGetStringProperty(item, "elementMapJson");

        return new CoordinationReceiptRecord
        {
            OperationId = Guid.TryParse(opIdStr, out var parsed) ? parsed : Guid.Empty,
            PlanHash = planHash,
            TargetDocId = targetDocId,
            TargetDocTitle = targetDocTitle,
            TargetDocPath = targetDocPath,
            TargetVersionGuid = targetVersionGuid,
            TargetCentralGuid = targetCentralGuid,
            CommittedAtUtc = committedAt,
            ElementMap = elemMapJson != null ? ParseElementMap(elemMapJson) : null
        };
    }

    public static IReadOnlyDictionary<string, string>? ParseElementMap(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        if (json.Trim() == "{}")
        {
            return new Dictionary<string, string>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException)
        {
            // Corrupt, truncated JSON or invalid UTF-16 surrogate pairs in storage must not be masked as valid empty map
            return null;
        }
    }
}
