namespace HPBim.Client.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HPBim.Core.Coordination;
using HPBim.Core.Execution;
using HPRebar.Mcp.Contracts.Messages;

public sealed partial class BimPipelineEngine
{
    private static IReadOnlyList<ElementExecutionRecord> ParseElementRecords(ExecuteResult execRes, BimExecutionPlan plan)
    {
        if (execRes.Truncated)
        {
            return CreateBatchUniformRecords(plan, ElementExecutionStatus.OutcomeUnknown, "ERR_RESPONSE_TRUNCATED",
                "Revit response was truncated (>64KB / output limit). Outcome cannot be confirmed without reconciliation.");
        }

        bool isFailedRollback = (execRes.IsError && execRes.RolledBack) || (!plan.Options.DryRun && execRes.RolledBack);
        var returned = ExtractReturnedRecords(execRes);
        if (returned == null)
        {
            var status = isFailedRollback ? ElementExecutionStatus.Failed : ElementExecutionStatus.OutcomeUnknown;
            var code = isFailedRollback ? "ERR_TRANSACTION_ROLLED_BACK" : (execRes.IsError ? "ERR_EXECUTION_ERROR" : "ERR_NO_RECORDS");
            var reason = isFailedRollback
                ? (execRes.Message ?? "Transaction rolled back by Revit bridge.")
                : (execRes.IsError ? (execRes.Message ?? "Revit execution failed without records.") : "Response lacked element execution records array.");
            return CreateBatchUniformRecords(plan, status, code, reason);
        }

        return ReconcileRecordsStrict1To1(plan, returned, isFailedRollback, execRes.RolledBack);
    }

    private static List<ElementExecutionRecord>? ExtractReturnedRecords(ExecuteResult execRes)
    {
        if (execRes.Value != null && execRes.Value.Value.ValueKind == JsonValueKind.Object)
        {
            var root = execRes.Value.Value;
            if (root.TryGetProperty("records", out var recordsProp) && recordsProp.ValueKind == JsonValueKind.Array)
            {
                return recordsProp.EnumerateArray().Select(ParseSingleElementRecord).ToList();
            }
        }
        else if (execRes.IsError && !string.IsNullOrEmpty(execRes.Message) && execRes.Message.Contains("||PAYLOAD:"))
        {
            return TryExtractPayloadRecords(execRes.Message);
        }
        return null;
    }

    private static List<ElementExecutionRecord>? TryExtractPayloadRecords(string message)
    {
        try
        {
            int payloadIdx = message.IndexOf("||PAYLOAD:", StringComparison.Ordinal) + 10;
            string payloadJson = message.Substring(payloadIdx).Trim();
            using var payloadDoc = JsonDocument.Parse(payloadJson);
            if (payloadDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                return payloadDoc.RootElement.EnumerateArray().Select(ParseSingleElementRecord).ToList();
            }
        }
        catch (JsonException ex)
        {
            _ = ex.Message;
            return null;
        }
        return null;
    }

    private static IReadOnlyList<ElementExecutionRecord> CreateBatchUniformRecords(
        BimExecutionPlan plan, ElementExecutionStatus status, string errorCode, string errorReason) =>
        plan.Items.Select(item => new ElementExecutionRecord
        {
            SourceKey = item.SourceKey, Category = item.Category, Mark = item.Mark,
            Status = status, ErrorCode = errorCode, ErrorReason = errorReason
        }).ToList();

    private static IReadOnlyList<ElementExecutionRecord> ReconcileRecordsStrict1To1(
        BimExecutionPlan plan,
        IReadOnlyList<ElementExecutionRecord> returned,
        bool isFailedRollback,
        bool hostRolledBack)
    {
        var planKeyCounts = plan.Items.GroupBy(i => i.SourceKey).ToDictionary(g => g.Key, g => g.Count());
        var recordMap = GroupRecordsBySourceKey(returned);

        var foreignRecords = returned.Where(r => !planKeyCounts.ContainsKey(r.SourceKey)).ToList();
        var duplicateKeys = recordMap
            .Where(kvp => kvp.Value.Count > (planKeyCounts.TryGetValue(kvp.Key, out int exp) ? exp : 0))
            .Select(kvp => kvp.Key).ToList();
        bool hasIntegrityDefect = foreignRecords.Count > 0 || duplicateKeys.Count > 0;

        var reconciled = new List<ElementExecutionRecord>(plan.Items.Count);
        foreach (var item in plan.Items)
        {
            recordMap.TryGetValue(item.SourceKey, out var queue);
            var rec = ReconcileSingleItem(item, queue, isFailedRollback);
            if (IsIntegrityDefect(rec)) hasIntegrityDefect = true;
            reconciled.Add(rec);
        }

        if (hasIntegrityDefect || isFailedRollback)
        {
            return ApplyBatchDefectResolution(reconciled, foreignRecords, duplicateKeys, hostRolledBack);
        }

        return reconciled;
    }

    private static Dictionary<CompositeSourceKey, Queue<ElementExecutionRecord>> GroupRecordsBySourceKey(
        IReadOnlyList<ElementExecutionRecord> returned)
    {
        var recordMap = new Dictionary<CompositeSourceKey, Queue<ElementExecutionRecord>>();
        foreach (var rec in returned)
        {
            if (!recordMap.TryGetValue(rec.SourceKey, out var q))
            {
                q = new Queue<ElementExecutionRecord>();
                recordMap[rec.SourceKey] = q;
            }
            q.Enqueue(rec);
        }
        return recordMap;
    }

    private static ElementExecutionRecord ReconcileSingleItem(
        ElementPlanItem item, Queue<ElementExecutionRecord>? queue, bool isFailedRollback) =>
        queue != null && queue.Count > 0
            ? ValidateReturnedRecord(item, queue.Dequeue(), isFailedRollback)
            : CreateMissingItemRecord(item, isFailedRollback);

    private static ElementExecutionRecord ParseSingleElementRecord(JsonElement elem)
    {
        string rawSourceKey = GetOptionalString(elem, "sourceKey") ?? string.Empty;
        string mark = GetOptionalString(elem, "mark") ?? string.Empty;
        Enum.TryParse<CoordinationCategory>(GetOptionalString(elem, "category") ?? "Column", true, out var cat);

        var (status, errCode, errReason) = ParseStatus(elem);
        long? elemId = elem.TryGetProperty("targetElementId", out var idProp) && idProp.ValueKind == JsonValueKind.Number ? idProp.GetInt64() : null;
        string? targetUid = elem.TryGetProperty("targetUniqueId", out var p)
            ? (p.ValueKind == JsonValueKind.String ? p.GetString() : string.Empty) : null;

        return new ElementExecutionRecord
        {
            SourceKey = CompositeSourceKey.TryParse(rawSourceKey, out var parsedSk) ? parsedSk! : new CompositeSourceKey(string.Empty, cat, mark),
            Category = cat, Mark = mark, Status = status, TargetElementId = elemId, TargetUniqueId = targetUid,
            ErrorCode = errCode, ErrorReason = errReason, Warnings = ExtractWarnings(elem)
        };
    }

    private static (ElementExecutionStatus Status, string? Code, string? Reason) ParseStatus(JsonElement elem)
    {
        string statusStr = GetOptionalString(elem, "status") ?? "Failed";
        bool valid = Enum.TryParse<ElementExecutionStatus>(statusStr, true, out var status)
                     && Enum.IsDefined(typeof(ElementExecutionStatus), status) && status != 0;
        if (!valid)
        {
            return (ElementExecutionStatus.Failed, "ERR_INVALID_STATUS_ENUM", $"Invalid ElementExecutionStatus '{statusStr}'.");
        }
        return (status, GetOptionalString(elem, "errorCode"), GetOptionalString(elem, "errorReason"));
    }

    private static List<string> ExtractWarnings(JsonElement elem)
    {
        var list = new List<string>();
        if (elem.TryGetProperty("warnings", out var w) && w.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in w.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String && item.GetString() is { } s) list.Add(s);
        }
        return list;
    }

    private static string? GetOptionalString(JsonElement elem, string prop) =>
        elem.TryGetProperty(prop, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
}
